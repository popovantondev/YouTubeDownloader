using System;using System.IO;using System.Reflection;using System.ComponentModel;using System.Collections.Generic;using System.Diagnostics;using System.Threading;using System.Windows.Forms;using System.Security.Cryptography;
namespace PeakMp3Win7
{
 internal static class ExtendedQa
 {
  static void Check(bool yes,string name){if(!yes)throw new Exception(name);Console.WriteLine("PASS: "+name);}
  static string Hash(string p){using(SHA256 h=SHA256.Create())using(FileStream f=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  static T Field<T>(object o,string k){return (T)o.GetType().GetField(k,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
  static object Call(object o,string k,params object[] a){return o.GetType().GetMethod(k,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,a);}
  static void Pump(int ms){Stopwatch c=Stopwatch.StartNew();while(c.ElapsedMilliseconds<ms){Application.DoEvents();Thread.Sleep(5);}}
  static void Graphs(string previous)
  {
   Assembly old=Assembly.LoadFile(Path.GetFullPath(previous));Type analysisType=old.GetType("PeakMp3Win7.ProcessResult");MethodInfo filter=old.GetType("PeakMp3Win7.ProcessingEngine").GetMethod("FilterForOptions",BindingFlags.NonPublic|BindingFlags.Static);
   foreach(double rms in new double[]{-6,-16,-35,-80})foreach(bool silent in new bool[]{false,true})
   {
    ProcessResult now=new ProcessResult();now.RmsDb=rms;now.PeakDb=rms+4;now.HasPeak=now.HasRms=true;now.IsSilent=silent;object before=Activator.CreateInstance(analysisType,true);
    foreach(string key in new string[]{"RmsDb","PeakDb","HasPeak","HasRms","IsSilent"})analysisType.GetField(key).SetValue(before,typeof(ProcessResult).GetField(key).GetValue(now));
    for(int d=0;d<11;d++)Check((string)filter.Invoke(null,new object[]{before,false,true,d})==ProcessingEngine.FilterForOptions(now,false,true,d),"0.13 audio graph identical d="+d+" rms="+rms+" silent="+silent);
   }
   Check(ProcessingEngine.FilterForOptions(null,false,true,-6).Contains("0.501187"),"negative density applies actual -6dB amplitude to original");
   Check(ProcessingEngine.FilterForOptions(null,false,true,int.MinValue).EndsWith("volume=0"),"minimum signed integer safely underflows to silence");
   ProcessResult measured=new ProcessResult();measured.RmsDb=-16;measured.PeakDb=-10;
   Check(ProcessingEngine.FilterForOptions(measured,false,true,int.MaxValue).Contains("alimiter=limit=1"),"maximum signed integer uses finite drive and output limiter");
  }
  static void Exports(string music,string test,string output)
  {
   string ffmpeg=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe");
   foreach(int d in new int[]{-6,0,11,100,int.MinValue,int.MaxValue})
   {
    string folder=Path.Combine(output,"SIGNED_"+d);JobOptions job=new JobOptions();job.Inputs=new string[]{test};job.OutputFolder=folder;job.DenseMode=true;job.DenseStrength=d;int last=-1,updates=0;DensityScore actual=null;
    ProcessingEngine engine=new ProcessingEngine(ffmpeg,new BackgroundWorker(),delegate(string k,string v){if(k=="stageProgress"){int n=int.Parse(v);Check(n>=last&&n<=99,"measured export progress monotonic d="+d+" p="+n);last=n;updates++;}});
    engine.Convert(job);actual=engine.LastVerification;Check(Directory.GetFiles(folder,"*.mp3").Length==1&&actual!=null&&actual.Frames>0,"signed export d="+d+" fully decoded and measured");Check(updates>0,"export progress comes from decoded audio timestamps d="+d);
   }
   JobOptions maximum=new JobOptions();maximum.Inputs=new string[]{music};maximum.OutputFolder=Path.Combine(output,"MAXIMUM10");maximum.DenseMode=true;maximum.DenseStrength=10;
   ProcessingEngine full=new ProcessingEngine(ffmpeg,new BackgroundWorker(),delegate(string k,string v){});full.Convert(maximum);string path=Directory.GetFiles(maximum.OutputFolder,"*.mp3")[0];string hash=Hash(path);
   Check(hash=="ff080e007d0dafb3e1931c759d2707c791e53a61f22c3c542a6796ad7686ff06","fresh complete Maximum MP3 byte-identical0.13 after new validation pipeline");Console.WriteLine("SHA256 Maximum10 "+hash+" "+full.LastVerification.Description);
  }
  static void Controller(string signal,string output)
  {
   UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.StartPosition=FormStartPosition.Manual;w.Location=new System.Drawing.Point(-20000,-20000);w.Show();Call(w,"AddFiles",(object)new string[]{signal});Stopwatch wait=Stopwatch.StartNew();
    while(Field<bool>(w,"autoBusy")&&wait.ElapsedMilliseconds<20000)Pump(20);Check(!Field<bool>(w,"autoBusy"),"real asynchronous auto selection completes");Check(Field<Dictionary<string,AutoDensityResult>>(w,"autoResults").Count==1,"per-file auto result is cached");
    Call(w,"SetManualDensity",-6);Check(Field<int>(w,"actualDensity")==-6,"manual negative parameter selected");Call(w,"PrepareSelectedFile");Check(Field<int>(w,"actualDensity")==-6&&!Field<bool>(w,"autoBusy"),"manual override outranks auto reload");
    JobOptions snapshot=new JobOptions();snapshot.Inputs=new string[]{signal};Call(w,"SnapshotJobDensities",snapshot);Check(snapshot.DenseStrengths[0]==-6&&!snapshot.AutoSelectDensities[0],"negative manual value is not mistaken for unknown auto sentinel");w.Close();
   }
   AutoDensityPicker picker=new AutoDensityPicker(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe"));Exception caught=null;Thread thread=new Thread(delegate(){try{picker.Pick(signal,null);}catch(Exception e){caught=e;}});thread.Start();Thread.Sleep(35);picker.Cancel();Check(thread.Join(5000)&&caught is OperationCanceledException,"auto analysis cancellation terminates owned subprocesses promptly");
  }
  [STAThread]static int Main(string[] a){try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Directory.CreateDirectory(a[3]);Graphs(a[0]);Controller(Path.GetFullPath(a[2]),Path.GetFullPath(a[3]));Exports(Path.GetFullPath(a[1]),Path.GetFullPath(a[2]),Path.GetFullPath(a[3]));return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
