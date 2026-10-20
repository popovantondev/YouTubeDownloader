// Test-only entry point; the shipped program uses Program.Main.
using System;using System.IO;using System.Collections.Generic;using System.Reflection;using System.Windows.Forms;using System.Drawing;
namespace PeakMp3Win7{
 internal static class BridgeHarness{
 [STAThread]static int Main(string[] args){try{
  string profile=Environment.GetEnvironmentVariable("PEAKMP3_BRIDGE_TEST_PROFILE");if(String.IsNullOrEmpty(profile))throw new Exception("No isolated test profile");UserPrefs.TestProfile=profile;
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string manifest=args[1];YouTubeJob job=DesktopHandoff.Load(manifest,Path.Combine(profile,"IntegrationData"));
  using(ApprovedWindow window=new ApprovedWindow()){
   string key=job.Source+"|"+new FileInfo(job.Source).Length+"|"+File.GetLastWriteTimeUtc(job.Source).Ticks;
   ((Dictionary<string,int>)typeof(ApprovedWindow).GetField("manualSelections",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window))[key]=0;
   window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-20000,-20000);window.ShowInTaskbar=false;
   Timer exit=new Timer();exit.Interval=2000;exit.Tick+=delegate{exit.Stop();window.Close();};
   window.Shown+=delegate{window.AcceptYouTubeJob(manifest);exit.Start();};Application.Run(window);exit.Dispose();
  }return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
