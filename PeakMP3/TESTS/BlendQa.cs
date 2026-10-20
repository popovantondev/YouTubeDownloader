using System;using System.IO;using System.ComponentModel;using System.Diagnostics;using System.Globalization;using System.Security.Cryptography;
namespace PeakMp3Win7
{
 internal static class BlendQa
 {
  static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
  static string Q(string x){return "\""+x.Replace("\"","")+"\"";}
  static float[] Render(string file,string graph)
  {
   Process p=new Process();p.StartInfo.FileName=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe");p.StartInfo.Arguments="-hide_banner -nostdin -loglevel error -ss 120 -i "+Q(file)+" -t 12 -map 0:a:0 -af "+Q(graph)+" -ac 2 -ar 44100 -c:a pcm_f32le -f f32le pipe:1";
   p.StartInfo.UseShellExecute=false;p.StartInfo.CreateNoWindow=true;p.StartInfo.RedirectStandardOutput=true;p.StartInfo.RedirectStandardError=true;string error="";p.ErrorDataReceived+=delegate(object o,DataReceivedEventArgs e){if(e.Data!=null)error+=e.Data+"\n";};p.Start();p.BeginErrorReadLine();
   byte[] data;using(MemoryStream m=new MemoryStream()){byte[] block=new byte[32768];int n;while((n=p.StandardOutput.BaseStream.Read(block,0,block.Length))>0)m.Write(block,0,n);data=m.ToArray();}p.WaitForExit();int code=p.ExitCode;p.Dispose();Check(code==0,"FFmpeg float render: "+graph);if(code!=0)throw new Exception(error);float[] samples=new float[data.Length/4];Buffer.BlockCopy(data,0,samples,0,data.Length);return samples;
  }
  static string Hash(string path){using(SHA256 h=SHA256.Create())using(FileStream f=File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  static int Main(string[] args)
  {
   try
   {
    string file=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);string binary=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe");
    ProcessingEngine engine=new ProcessingEngine(binary,new BackgroundWorker(),delegate(string k,string v){});
    Check(ProcessingEngine.FilterReady(null,true,0),"original endpoint needs no processing analysis");
    Check(ProcessingEngine.FilterForOptions(null,false,true,0)=="aresample=44100","density0 is original PCM without gain, compression or limiting");
    ProcessResult analysis=engine.AnalyzeForPreview(file,false,true);
    string maximumGraph=ProcessingEngine.DenseFilter(analysis,10);
    Check(ProcessingEngine.FilterForOptions(analysis,false,true,10)==maximumGraph,"Maximum DSP graph remains exactly unchanged");
    float[] dry=Render(file,"aresample=44100"),maximum=Render(file,maximumGraph);
    Check(dry.Length==maximum.Length,"Maximum lookahead compensation retains original timeline and sample count");
    for(int level=1;level<10;level++)
    {
     string graph=ProcessingEngine.FilterForOptions(analysis,false,true,level);Check(!graph.Contains("agate="),"no former gate/expander at density"+level);
     float[] mixed=Render(file,graph);Check(mixed.Length==dry.Length,"parallel blend has full aligned duration at density"+level);
     double error=0,wet=level/10.0,delta=0;
     for(int i=0;i<mixed.Length;i++){double expected=dry[i]*(1-wet)+maximum[i]*wet;error=Math.Max(error,Math.Abs(mixed[i]-expected));delta+=Math.Abs(mixed[i]-dry[i]);}
     Check(error<.000002,"density"+level+" = "+(10-level)+"0% original + "+level+"0% Maximum; maximum sample error "+error.ToString("G9",CultureInfo.InvariantCulture));Check(delta>1,"blend changes actual audio samples at density"+level);
    }
    if(args.Length>2&&args[2]=="exports")
    {
     for(int level=0;level<11;level++)
     {
      string folder=Path.Combine(output,"LEVEL"+level);JobOptions job=new JobOptions();job.Inputs=new string[]{file};job.OutputFolder=folder;job.DenseMode=true;job.DenseStrength=level;
      engine.Convert(job);string[] files=Directory.GetFiles(folder,"*.mp3");Check(files.Length==1,"complete MP3320 density"+level+" exported and fully decoded");Console.WriteLine("SHA256 density"+level+" "+Hash(files[0]));
      if(level==10&&args.Length>3)Check(Hash(files[0])==Hash(args[3]),"fresh full Maximum MP3 byte-identical to0.12");
     }
    }
    return 0;
   }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  }
 }
}
