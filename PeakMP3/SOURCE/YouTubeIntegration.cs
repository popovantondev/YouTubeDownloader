using System;using System.IO;using System.IO.Pipes;using System.Text;using System.Threading;using System.Security.AccessControl;using System.Security.Principal;using System.Collections.Generic;using System.Windows.Forms;
namespace PeakMp3Win7
{
 internal sealed class DesktopInstance : IDisposable
 {
  private readonly Mutex mutex;private readonly string pipeName;internal readonly bool Owner;private NamedPipeServerStream server;private volatile bool closing;
  internal DesktopInstance():this(""){}
  internal DesktopInstance(string isolatedTest)
  {if(isolatedTest.Length>0&&!System.Text.RegularExpressions.Regex.IsMatch(isolatedTest,@"^Test_[a-f0-9]{32}$"))throw new ArgumentException("Invalid instance namespace.");string user=WindowsIdentity.GetCurrent().User.Value;pipeName="PeakMP3.Desktop."+user+(isolatedTest.Length>0?"."+isolatedTest:"");bool fresh;mutex=new Mutex(true,"Local\\"+pipeName,out fresh);Owner=fresh;}
  internal void Forward(string job)
  {using(NamedPipeClientStream client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut)){client.Connect(10000);using(StreamReader reader=new StreamReader(client,Encoding.UTF8))using(StreamWriter writer=new StreamWriter(client,new UTF8Encoding(false),1024)){writer.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(job??"")));writer.Flush();string reply=reader.ReadLine();if(reply!="OK")throw new IOException(reply??"PeakMP3 did not acknowledge the request.");}}}
  internal void Listen(ApprovedWindow window)
  {
   Thread thread=new Thread(delegate(){while(!closing)try{
    PipeSecurity security=new PipeSecurity();security.SetAccessRuleProtection(true,false);security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
    using(NamedPipeServerStream next=new NamedPipeServerStream(pipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.None,4096,4096,security))
    {server=next;next.WaitForConnection();using(StreamReader reader=new StreamReader(next,Encoding.UTF8))using(StreamWriter writer=new StreamWriter(next,new UTF8Encoding(false),1024))
     {StringBuilder line=new StringBuilder();int c;while((c=reader.Read())>=0&&c!='\n'){if(line.Length>=8192)throw new InvalidDataException("Request too large.");if(c!='\r')line.Append((char)c);}string job=Encoding.UTF8.GetString(Convert.FromBase64String(line.ToString()));string error=null;
      using(ManualResetEvent completed=new ManualResetEvent(false)){window.BeginInvoke((MethodInvoker)delegate{try{if(job.Length>0)window.AcceptYouTubeJob(job);else window.RevealDesktop();}catch(Exception e){error=e.Message;}finally{try{completed.Set();}catch(ObjectDisposedException){}}});if(!completed.WaitOne(30000,false))error="PeakMP3 is busy.";writer.WriteLine(error==null?"OK":error);writer.Flush();}
     }}server=null;
   }catch{if(closing)return;Thread.Sleep(100);}});thread.IsBackground=true;thread.Start();
  }
  public void Dispose(){closing=true;try{if(server!=null)server.Dispose();}catch{}if(Owner){try{mutex.ReleaseMutex();}catch(ApplicationException){}}mutex.Close();}
 }
 internal sealed partial class ApprovedWindow
 {
  private readonly Dictionary<string,FileStream> incomingLeases=new Dictionary<string,FileStream>();
  private readonly HashSet<string> incomingHashes=new HashSet<string>();
  private readonly Dictionary<string,string> incomingTitles=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  internal void SetFirstLanguage(string language){prefs.Language=language=="ru"?0:language=="de"?1:2;Save();UpdateTexts();RefreshVintage();}
  private string IncomingTitle(string source,string fallback){string title;return incomingTitles.TryGetValue(source,out title)?RecordName.Clean(title):fallback;}
  private RecordName IncomingName(string source,RecordName fallback)
  {string title;if(!incomingTitles.TryGetValue(source,out title))return fallback;foreach(char c in Path.GetInvalidFileNameChars())title=title.Replace(c,'_');return RecordName.Resolve(title+".m4a","","");}
  internal void RevealDesktop(){if(UserPrefs.TestProfile!=null)return;if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Show();Activate();}
  protected override bool ShowWithoutActivation{get{return UserPrefs.TestProfile!=null;}}
  internal void AcceptYouTubeJob(string manifest)
  {
   string data=DesktopHandoff.DataRoot;
   #if TESTING
   if(UserPrefs.TestProfile!=null)data=Path.Combine(UserPrefs.TestProfile,"IntegrationData");
   #endif
   YouTubeJob job=DesktopHandoff.Load(manifest,data);string fingerprint=job.VideoId+"|"+job.Hash;
   if(incomingHashes.Contains(fingerprint)){DesktopHandoff.Accept(job);RevealDesktop();return;}
   FileStream lease=new FileStream(Path.Combine(job.DirectoryPath,"claim.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
   try{
    incomingTitles[job.Source]=job.Title;incomingLeases.Add(job.JobId,lease);incomingHashes.Add(fingerprint);
    bool select=activeFile==null||!worker.IsBusy&&(player==null||!player.IsPlaying);
    if(!files.Contains(job.Source))files.Add(job.Source);FillQueue();if(select)SetCurrent(job.Source);
    DesktopHandoff.Accept(job);Log(T("receivedYouTube")+" "+job.Title);UpdateTexts();RefreshVintage();RevealDesktop();
   }catch{incomingTitles.Remove(job.Source);incomingHashes.Remove(fingerprint);incomingLeases.Remove(job.JobId);lease.Dispose();throw;}
  }
  private void CloseIncomingLeases(){foreach(FileStream lease in incomingLeases.Values)lease.Dispose();incomingLeases.Clear();}
 }
}
