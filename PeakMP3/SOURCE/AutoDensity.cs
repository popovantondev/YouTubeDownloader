using System;using System.IO;using System.Diagnostics;using System.ComponentModel;using System.Globalization;using System.Collections.Generic;
namespace PeakMp3Win7
{
 internal sealed class DensityScore
 {
  internal int Active,Near,Zero,Frames;
  internal double Maximum=-180;
  internal readonly HashSet<int> ActiveBins=new HashSet<int>(),NearBins=new HashSet<int>(),ZeroBins=new HashSet<int>();
  private readonly MeterEnvelope meter=new MeterEnvelope();
  internal void Push(float l,float r,float sourcePeak)
  {
   meter.Push(l,0,r,0,1.0/32);double db=MeterEnvelope.Db(Math.Max(meter.Left,meter.Right));Maximum=Math.Max(Maximum,db);
   if(sourcePeak>.005){Active++;ActiveBins.Add(Frames/160);if(db>=-.15){Near++;NearBins.Add(Frames/160);}if(db>=0){Zero++;ZeroBins.Add(Frames/160);}}Frames++;
  }
  internal bool Regular {get{return Active>=32&&Near>=Math.Max(3,(int)Math.Ceiling(Active*.08))&&NearBins.Count>=Math.Max(1,(int)Math.Ceiling(ActiveBins.Count*.35));}}
  internal bool VerifiedZero {get{return Regular&&Zero>=Math.Max(2,(int)Math.Ceiling(Active*.04))&&ZeroBins.Count>=Math.Max(1,(int)Math.Ceiling(ActiveBins.Count*.35));}}
  internal string Description {get{return "peakVU="+Maximum.ToString("0.000",CultureInfo.InvariantCulture)+"dB; near="+Near+"/"+Active+"; zero="+Zero+"; bins="+NearBins.Count+"/"+ActiveBins.Count;}}
 }
 internal sealed class AutoDensityResult
 {internal int Strength;internal bool ReachesZero;internal string Detail;internal ProcessResult Analysis;internal DensityScore[] Scores;internal int[] Candidates;}
 internal sealed class AutoDensityPicker
 {
  private readonly string binary;private readonly object guard=new object();private Process current;private volatile ProcessingEngine engine;private volatile bool cancelled;
  internal AutoDensityPicker(string ffmpeg){binary=ffmpeg;}
  internal void Cancel(){cancelled=true;if(engine!=null)engine.Cancel();lock(guard){try{if(current!=null&&!current.HasExited)current.Kill();}catch(InvalidOperationException){}}}
  private void Check(){if(cancelled)throw new OperationCanceledException();}
  private static string Q(string s){return "\""+s.Replace("\"","")+"\"";}
  internal AutoDensityResult Pick(string file,Action<int> progress)
  {
   Check();BackgroundWorker worker=new BackgroundWorker();engine=new ProcessingEngine(binary,worker,delegate(string k,string v){});
   Check();ProcessResult analysis=engine.AnalyzeForPreview(file,false,true);Check();
   string max=ProcessingEngine.DenseFilter(analysis,10);
   string graph=analysis.IsSilent?"aresample=44100,aformat=channel_layouts=stereo,asplit=2[d][m];[d][m]amerge=inputs=2":
    "aresample=44100,aformat=channel_layouts=stereo,asplit=2[d][w];[w]"+max+",aformat=channel_layouts=stereo[m];[d][m]amerge=inputs=2";
   int[] candidates={0,1,2,3,4,5,6,7,8,9,10,11,12,14,16,20,30,50,100,200,500,1000,10000,1000000,int.MaxValue};
   DensityScore[] scores=new DensityScore[candidates.Length];for(int i=0;i<scores.Length;i++)scores[i]=new DensityScore();
   Decode(file,graph,4,delegate(float[] samples,int count,int frame)
   {
    Check();float src=0,drPeak=0,wlPeak=0,wrPeak=0;
    for(int j=0;j<Math.Min(count,512);j++)
    {int o=j*4;src=Math.Max(src,Math.Abs(samples[o]));drPeak=Math.Max(drPeak,Math.Abs(samples[o+1]));wlPeak=Math.Max(wlPeak,Math.Abs(samples[o+2]));wrPeak=Math.Max(wrPeak,Math.Abs(samples[o+3]));}
    // Cheap upper-bound screening from four window peaks, never used as the actual meter.
    // The proposed density is always validated with real mixed, encoded and decoded audio.
    for(int k=0;k<scores.Length;k++){int strength=candidates[k];double wet=strength/10.0,l,r;if(strength<=10){l=src*(1-wet)+wlPeak*wet;r=drPeak*(1-wet)+wrPeak*wet;}else{double gain=1+(strength-10)/10.0;l=Math.Min(1,wlPeak*gain);r=Math.Min(1,wrPeak*gain);}scores[k].Push((float)l,(float)r,(float)Math.Max(l,r));}
    if(progress!=null&&frame%32==0)progress(frame/32);
   });
   int chosenIndex=10;int[] coarse={0,3,6,9,10,12,14,16,18,20,22,24};int previous=0;
   foreach(int probe in coarse){if(scores[probe].Regular){chosenIndex=probe;for(int fine=previous;fine<probe;fine++)if(scores[fine].Regular){chosenIndex=fine;break;}break;}previous=probe+1;}int chosen=candidates[chosenIndex];
   if(analysis.IsSilent){AutoDensityResult silent=new AutoDensityResult();silent.Strength=0;silent.Detail="silent";silent.Scores=scores;silent.Candidates=candidates;silent.Analysis=analysis;return silent;}
   AutoDensityResult result=new AutoDensityResult();result.Strength=chosen;result.Scores=scores;result.Candidates=candidates;result.Analysis=analysis;
   string temp=Path.Combine(Path.GetTempPath(),"PeakMP3-auto-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
   try
   {
    // Screening above10 estimates clipped Maximum; final decisions use real encoded/decoded audio.
    // At most four proposed candidates are encoded, cached per file, never on every Play.
    // No eleven full encodes on Play. Both temporary candidates are fully decoded.
    DensityScore verified=VerifyCandidate(file,temp,chosen,analysis);Check();
    for(int attempt=1;!verified.VerifiedZero&&attempt<4;attempt++)
    {
     int next;
     if(chosen<10)next=Math.Min(10,chosen+3);
     else{double gain=1+(chosen-10)/10.0,ratio=Math.Pow(10,Math.Max(.2,.2-verified.Maximum)/20.0);double target=10+10*(gain*ratio-1);next=(int)Math.Min(int.MaxValue,Math.Max((double)chosen+1,Math.Ceiling(target)));}
     if(next<=chosen)break;chosen=next;verified=VerifyCandidate(file,temp,chosen,analysis);
    }
    result.Strength=chosen;result.ReachesZero=verified.VerifiedZero;result.Detail=verified.Description;
   }
   finally{try{foreach(string p in Directory.GetFiles(temp))File.Delete(p);Directory.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}
   Check();return result;
  }
  private DensityScore VerifyCandidate(string file,string temp,int strength,ProcessResult analysis)
  {
   Check();JobOptions job=new JobOptions();job.Inputs=new string[]{file};job.DenseMode=true;job.DenseStrength=strength;job.OutputFolder=temp;job.KnownAnalyses=new ProcessResult[]{analysis};
   engine=new ProcessingEngine(binary,new BackgroundWorker(),delegate(string k,string v){Check();});Check();engine.Convert(job);Check();
   string[] outputs=Directory.GetFiles(temp,"*.mp3");string path=outputs[outputs.Length-1];
   DensityScore result=engine.LastVerification;Check();File.Delete(path);return result;
  }
  internal DensityScore Measure(string file,Action<int> seconds)
  {
   DensityScore score=new DensityScore();Decode(file,"aresample=44100,aformat=channel_layouts=stereo",2,delegate(float[] samples,int count,int frame)
   {Check();float l=0,r=0;ReferencePeakWindow.Measure(samples,0,Math.Min(512,count),ref l,ref r);score.Push(l,r,Math.Max(l,r));if(seconds!=null&&frame%8==0)seconds(frame/32);});return score;
  }
  private void Decode(string file,string graph,int channels,Action<float[],int,int> consume)
  {
   Check();Process p=new Process();p.StartInfo.FileName=binary;p.StartInfo.Arguments="-hide_banner -nostdin -loglevel error -xerror -i "+Q(file)+" -map 0:a:0 -vn -sn -dn -af "+Q(graph)+" -c:a pcm_f32le -f f32le pipe:1";
   p.StartInfo.UseShellExecute=false;p.StartInfo.CreateNoWindow=true;p.StartInfo.RedirectStandardOutput=true;p.StartInfo.RedirectStandardError=true;string error="";p.ErrorDataReceived+=delegate(object o,DataReceivedEventArgs e){if(e.Data!=null)error=e.Data;};
   try
   {
    lock(guard){Check();current=p;p.Start();}p.BeginErrorReadLine();
    try{p.PriorityClass=ProcessPriorityClass.BelowNormal;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
    byte[] bytes=new byte[1379*channels*4];float[] values=new float[1379*channels];int frame=0;
    for(;;){Check();int wanted=MeterEnvelope.SamplesInFrame(frame)*channels*4,read=0,n;while(read<wanted&&(n=p.StandardOutput.BaseStream.Read(bytes,read,wanted-read))>0){read+=n;Check();}int count=read/(channels*4);if(count==0)break;Buffer.BlockCopy(bytes,0,values,0,count*channels*4);consume(values,count,frame++);}
    p.WaitForExit();Check();if(p.ExitCode!=0)throw new InvalidOperationException("ffmpeg_failed:"+error);
   }
   finally{lock(guard){current=null;try{if(!p.HasExited)p.Kill();}catch(InvalidOperationException){}p.Dispose();}}
  }
 }
}
