using System;using System.IO;using System.Diagnostics;
namespace PeakMp3Win7
{
 internal static class AutoQa
 {
  static int Main(string[] args){try{Stopwatch clock=Stopwatch.StartNew();AutoDensityResult r=new AutoDensityPicker(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe")).Pick(Path.GetFullPath(args[0]),null);for(int i=0;i<11;i++)Console.WriteLine(i+" regular="+r.Scores[i].Regular+" "+r.Scores[i].Description);Console.WriteLine("SELECTED="+r.Strength+" reachesZero="+r.ReachesZero+" "+r.Detail+" elapsed="+clock.Elapsed);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
