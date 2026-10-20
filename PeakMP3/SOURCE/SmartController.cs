using System;using System.IO;using System.Threading;using System.Collections.Generic;using System.Security.Cryptography;using System.Text;
namespace PeakMp3Win7
{
 internal sealed partial class ApprovedWindow
 {
  private bool pendingPlay,pendingResume,autoApplying,autoBusy,resumeAfterExport;
  private int transportState,autoGeneration,actualDensity,autoStatus;
  private bool tuningKnob;
  private int CurrentDensity {get{return actualDensity;}}
  private string headline="",autoInfo="";
  private AutoDensityPicker autoPicker;private volatile AutoDensityPicker exportPicker;
  private readonly Dictionary<string,AutoDensityResult> autoResults=new Dictionary<string,AutoDensityResult>();
  private readonly Dictionary<string,int> manualSelections=new Dictionary<string,int>();
  private static string FileKey(string path){return path+"|"+new FileInfo(path).Length+"|"+File.GetLastWriteTimeUtc(path).Ticks;}
  private void ManualDensity()
  {if(autoApplying||tuningKnob)return;actualDensity=knob.Value;prefs.ExtendedDensity=actualDensity;if(activeFile==null)return;manualSelections[FileKey(activeFile)]=actualDensity;CancelAuto();autoInfo=T("manualDensity");autoStatus=4;}
  private void CancelAuto(){autoGeneration++;autoBusy=false;if(autoPicker!=null){autoPicker.Cancel();autoPicker=null;}}
  private void SelectDensity(int value)
  {
   int previous=actualDensity;actualDensity=value;autoApplying=tuningKnob=true;
   try{knob.Value=Math.Max(0,Math.Min(10,value));prefs.DenseStrength=knob.Value;prefs.ExtendedDensity=value;Save();}
   finally{autoApplying=tuningKnob=false;}
   if(previous!=value&&player!=null&&player.IsPlaying){if(!comparison.Processed)comparison.Processed=true;else{applyTimer.Stop();applyTimer.Start();}}
  }
  private void SetManualDensity(int value)
  {SelectDensity(value);if(activeFile!=null)manualSelections[FileKey(activeFile)]=value;CancelAuto();autoInfo=T("manualDensity");autoStatus=4;}
  private void EditDensity()
  {using(DensityInput input=new DensityInput(CurrentDensity,prefs.Language)){if(input.ShowDialog(this)==System.Windows.Forms.DialogResult.OK)SetManualDensity(input.Value);}}
  private void PrepareSelectedFile()
  {
   pendingPlay=pendingResume=false;transportState=0;headline="";autoInfo="";autoStatus=0;CancelAuto();if(activeFile==null)return;
   string key=FileKey(activeFile);int value;
   if(manualSelections.TryGetValue(key,out value)){SelectDensity(value);autoInfo=T("manualDensity");autoStatus=4;return;}
   AutoDensityResult known;if(autoResults.TryGetValue(key,out known)||ReadAutoCache(key,out known)){autoResults[key]=known;ApplyAuto(known);return;}
   string file=activeFile;int generation=++autoGeneration;autoBusy=true;autoStatus=1;autoInfo=T("autoSelecting");
   AutoDensityPicker picker=new AutoDensityPicker(ffmpeg);autoPicker=picker;
   Thread thread=new Thread(delegate()
   {
    AutoDensityResult result=null;string error=null;try{result=picker.Pick(file,null);}catch(OperationCanceledException){return;}catch(Exception e){error=e.Message;}
    if(IsDisposed||!IsHandleCreated)return;
    try{BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
    {
     if(generation!=autoGeneration||activeFile!=file)return;autoBusy=false;autoPicker=null;
     if(result!=null){autoResults[key]=result;WriteAutoCache(key,result);if(!manualSelections.ContainsKey(key))ApplyAuto(result);}
     else{pendingPlay=pendingResume=false;autoStatus=5;autoInfo=T("autoFailed");headline=T("autoFailed");Log(error??"");}
     RefreshVintage();
    });}catch(InvalidOperationException){}
   });thread.IsBackground=true;thread.Priority=ThreadPriority.BelowNormal;thread.Start();
  }
  private void ApplyAuto(AutoDensityResult result)
  {
   SelectDensity(result.Strength);comparison.Processed=true;
   if(result.Analysis!=null&&activeFile!=null)lock(analysisGuard)analysisCache[FileKey(activeFile)]=result.Analysis;
   if(!result.ReachesZero)headline=T("autoNoZero");
   autoStatus=result.ReachesZero?2:3;
   autoInfo=T("autoSelected")+" "+result.Strength+"/10"+(result.ReachesZero?"":" · "+T("autoNoZero"));
   Log(autoInfo+"; "+result.Detail);
  }
  private void RequestPlayback()
  {
   if(worker.IsBusy)return;
   headline="";
   if(pendingPlay){pendingPlay=false;pendingResume=false;transportState=2;return;}
   if(player!=null&&player.IsPlaying&&!player.IsPaused){player.Pause();transportState=2;return;}
   if(activeFile==null)return;
   if(duration>0&&position>=duration-.02)position=0;
   pendingResume=player!=null&&player.IsPlaying&&player.IsPaused;pendingPlay=true;transportState=1;
  }
  private void SmartStop()
  {resumeAfterExport=false;pendingPlay=pendingResume=false;transportState=3;headline="";StopPlayback(true);UpdatePosition();}
  private void SmartTick()
  {
   if(pendingPlay&&!autoBusy&&vintage!=null&&vintage.Mechanics.ReadyForAudio)
   {
    bool resume=pendingResume;pendingPlay=pendingResume=false;
    if(resume&&player!=null&&player.IsPlaying)player.Resume();else StartPlayback();
   }
   if(player!=null&&player.Ended)
   {
    StopPlayback(false);int index=files.IndexOf(activeFile);position=duration;transportState=3;
    if(!worker.IsBusy&&index>=0&&index+1<files.Count){SetCurrent(files[index+1]);pendingPlay=true;pendingResume=false;transportState=1;}
   }
  }
  private void SnapshotJobDensities(JobOptions job)
  {
   resumeAfterExport=player!=null&&player.IsPlaying&&!player.IsPaused;
   if(resumeAfterExport)player.Pause();pendingPlay=pendingResume=false;transportState=2;progressKnown=false;
   CancelAuto();job.DenseStrengths=new int[job.Inputs.Length];job.AutoSelectDensities=new bool[job.Inputs.Length];
   for(int i=0;i<job.Inputs.Length;i++){string key=FileKey(job.Inputs[i]);int value;AutoDensityResult known;
    if(manualSelections.TryGetValue(key,out value))job.DenseStrengths[i]=value;else if(autoResults.TryGetValue(key,out known))job.DenseStrengths[i]=known.Strength;else job.AutoSelectDensities[i]=true;}
  }
  private void ResolveJobDensities(JobOptions job)
  {
   for(int i=0;i<job.Inputs.Length;i++)
   {
    if(worker.CancellationPending)throw new OperationCanceledException();if(!job.AutoSelectDensities[i])continue;
    worker.ReportProgress(0,new string[]{"autoSelecting",Path.GetFileName(job.Inputs[i])});
    exportPicker=new AutoDensityPicker(ffmpeg);if(worker.CancellationPending){exportPicker.Cancel();throw new OperationCanceledException();}AutoDensityResult result=exportPicker.Pick(job.Inputs[i],delegate(int seconds){if(worker.CancellationPending){exportPicker.Cancel();throw new OperationCanceledException();}});exportPicker=null;
    job.DenseStrengths[i]=result.Strength;WriteAutoCache(FileKey(job.Inputs[i]),result);
   }
  }
  private string AutoCaption()
  {if(autoStatus==1)return T("autoSelecting");if(autoStatus==4)return T("manualDensity");if(autoStatus==5)return T("autoFailed");if(autoStatus==2||autoStatus==3)return T("autoSelected")+" "+CurrentDensity+(autoStatus==3?" · "+T("autoNoZero"):"");return "";}
  private string LocalizeHeadline(string text)
  {foreach(string key in new string[]{"complete","cancelled","autoNoZero","autoFailed","working","failed","previewError"})for(int lang=0;lang<3;lang++){string old=L10n.Get(key,lang);if(text.StartsWith(old,StringComparison.Ordinal))return T(key)+text.Substring(old.Length);}return text;}
  private static readonly object autoCacheGuard=new object();
  private static string CacheFile {get{return Path.Combine(UserPrefs.TestProfile??Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PeakMp3Win7\\auto-density-v1.cache");}}
  private static string CacheKey(string key){using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes("criterion2-extended|"+key))).Replace("-","");}
  private static bool ReadAutoCache(string key,out AutoDensityResult result)
  {
   result=null;try{lock(autoCacheGuard){if(!File.Exists(CacheFile))return false;string wanted=CacheKey(key);foreach(string line in File.ReadAllLines(CacheFile)){string[] x=line.Split('|');int value;if(x.Length==4&&x[0]==wanted&&int.TryParse(x[1],out value)&&value>=0){result=new AutoDensityResult();result.Strength=value;result.ReachesZero=x[2]=="1";result.Detail=Encoding.UTF8.GetString(Convert.FromBase64String(x[3]));return true;}}}}catch(IOException){}catch(UnauthorizedAccessException){}catch(FormatException){}return false;
  }
  private static void WriteAutoCache(string key,AutoDensityResult result)
  {
   try{lock(autoCacheGuard){string path=CacheFile;Directory.CreateDirectory(Path.GetDirectoryName(path));List<string> lines=new List<string>();string hash=CacheKey(key);if(File.Exists(path))foreach(string line in File.ReadAllLines(path))if(!line.StartsWith(hash+"|",StringComparison.Ordinal))lines.Add(line);while(lines.Count>=100)lines.RemoveAt(0);lines.Add(hash+"|"+result.Strength+"|"+(result.ReachesZero?"1":"0")+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(result.Detail??"")));File.WriteAllLines(path,lines.ToArray());}}catch(IOException){}catch(UnauthorizedAccessException){}
  }
 }
}
