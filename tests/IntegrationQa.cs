using System;using System.IO;using System.Reflection;using System.Collections.Generic;using System.Drawing;using System.Threading;using System.Windows.Forms;
namespace PeakMp3Win7
{
 internal static class IntegrationQa
 {
  private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS: "+message);}
  private static T Field<T>(object obj,string name){return (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);}
  private static void Pump(int ms){DateTime end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(5);}}
  [STAThread]private static int Main(string[] args)
  {
   try{
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    string profile=Path.GetFullPath(args[1]);Directory.CreateDirectory(profile);UserPrefs.TestProfile=profile;string data=Path.Combine(profile,"IntegrationData");
    string manifest=DesktopHandoff.Create(data,Path.GetFullPath(args[0]),"YtdTest0001","ABBA — Song (Live) & test","en");YouTubeJob job=DesktopHandoff.Load(manifest,data);Check(File.Exists(job.Source)&&job.Title=="ABBA — Song (Live) & test","flat job preserves title and AAC bytes");
    bool rejected=false;try{DesktopHandoff.Load(manifest,Path.Combine(profile,"another-root"));}catch{rejected=true;}Check(rejected,"outside-inbox handoff rejected");
    using(ApprovedWindow window=new ApprovedWindow())
    {
     window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-20000,-20000);window.Show();
     // Register the already-selected original mode: this transport test must not time a full auto search.
     string key=job.Source+"|"+new FileInfo(job.Source).Length+"|"+File.GetLastWriteTimeUtc(job.Source).Ticks;
     Field<Dictionary<string,int>>(window,"manualSelections")[key]=0;
     window.AcceptYouTubeJob(manifest);Pump(150);Check(Field<List<string>>(window,"files").Count==1,"actual window imports one source into queue");Check(File.Exists(Path.Combine(job.DirectoryPath,"accepted")),"ack written only after GUI import");Check(Field<PreviewPlayer>(window,"player")==null,"YouTube import does not autoplay");
     window.AcceptYouTubeJob(manifest);Check(Field<List<string>>(window,"files").Count==1,"repeat request cannot duplicate current queue entry");
     string scope="Test_"+Guid.NewGuid().ToString("N");
     using(DesktopInstance first=new DesktopInstance(scope))using(DesktopInstance second=new DesktopInstance(scope))
     {
      Check(first.Owner&&!second.Owner,"single-instance ownership is exclusive");first.Listen(window);Exception forwardError=null;bool finished=false;
      Thread sender=new Thread(delegate(){try{second.Forward(manifest);}catch(Exception e){forwardError=e;}finally{finished=true;}});sender.IsBackground=true;sender.Start();
      DateTime until=DateTime.UtcNow.AddSeconds(12);while(!finished&&DateTime.UtcNow<until)Pump(20);
      if(forwardError!=null)Console.Error.WriteLine(forwardError);Check(finished&&forwardError==null,"existing window acknowledges job through the actual user-only pipe");Check(Field<List<string>>(window,"files").Count==1,"IPC delivery keeps repeat import deduplicated");
     }
     window.Close();
    }
    string bytes=File.ReadAllText(manifest);File.WriteAllText(manifest,bytes.Replace(Path.GetFileName(job.Source),"../escape.m4a"));rejected=false;try{DesktopHandoff.Load(manifest,data);}catch{rejected=true;}Check(rejected,"relative path traversal rejected");
    Console.WriteLine("ALL_DESKTOP_IMPORT_CHECKS_PASSED");return 0;
   }catch(Exception e){Console.Error.WriteLine(e);return 1;}
  }
 }
}
