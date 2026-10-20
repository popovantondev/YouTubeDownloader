using System;using System.IO;using System.Drawing;using System.Reflection;using System.Windows.Forms;using System.Diagnostics;using System.Threading;using System.Collections.Generic;using System.ComponentModel;using System.Security.Cryptography;
namespace PeakMp3Win7
{
 internal static class TurntableQa
 {
  static T Field<T>(object o,string key){return (T)o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
  static object Call(object o,string key,params object[] args){return o.GetType().GetMethod(key,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
  static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS: "+name);}
  static void Pump(int ms){Stopwatch clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<ms){Application.DoEvents();Thread.Sleep(5);}}
  static void Wait(Func<bool> predicate,int timeout,string name){Stopwatch clock=Stopwatch.StartNew();while(!predicate()&&clock.ElapsedMilliseconds<timeout)Pump(20);Check(predicate(),name);}
  static void Shot(Form w,string path){using(Bitmap b=new Bitmap(w.ClientSize.Width,w.ClientSize.Height)){w.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(path);}}
  static string Key(string p){return p+"|"+new FileInfo(p).Length+"|"+File.GetLastWriteTimeUtc(p).Ticks;}
  static double Position(PreviewPlayer p){double l,r,t;bool a;string e;p.Snapshot(out l,out r,out t,out a,out e);if(e!=null)throw new Exception(e);return t;}
  static void Wav(string path,double seconds)
  {using(BinaryWriter w=new BinaryWriter(File.Create(path))){int n=(int)(seconds*44100);w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*4);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)2);w.Write(44100);w.Write(176400);w.Write((short)4);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(n*4);for(int i=0;i<n;i++){short s=(short)(Math.Sin(i*2*Math.PI*440/44100)*7000);w.Write(s);w.Write(s);}}}
  static void Motion()
  {
   TurntableMotion m=new TurntableMotion();for(int i=0;i<80;i++)m.Advance(1.0/32,true,0,"one");Check(m.ReadyForAudio&&m.Speed==200&&m.ArmAngle==5,"head reaches outer groove before audio readiness");
   double angle=m.Rotation;m.Advance(1.0/32,false,0,"one");Check(m.Speed>0&&m.Speed<200&&m.Rotation!=angle,"pause begins a real coast rather than freezing immediately");
   for(int i=0;i<80;i++)m.Advance(1.0/32,false,0,"one");Check(m.Phase=="parked"&&m.Lift==1&&m.ArmAngle==-16&&m.Speed==0,"paused/stopped head lifted and parked, platter stopped");
   angle=m.Rotation;for(int i=0;i<10;i++)m.Advance(1.0/32,false,.6,"one");Check(m.Rotation==angle,"fully stopped platter remains still");
   for(int i=0;i<80;i++)m.Advance(1.0/32,true,.6,"one");Check(m.ReadyForAudio&&Math.Abs(m.ArmAngle-16.4)<.001,"resume returns to saved radial position, not outer groove");
   m.BeginSeek();Check(!m.ReadyForAudio,"seek lifts the head and gates audio");for(int i=0;i<60;i++)m.Advance(1.0/32,true,.2,"one");Check(m.ReadyForAudio&&Math.Abs(m.ArmAngle-8.8)<.001,"seek lands at timeline-derived radius");
   m.Advance(1.0/32,true,0,"two");Check(!m.ReadyForAudio,"new queue track cannot start under old groove position");for(int i=0;i<90;i++)m.Advance(1.0/32,true,0,"two");Check(m.ReadyForAudio&&m.ArmAngle==5,"next queue track returns to outer groove");
   for(int i=0;i<1000;i++){m.Advance(1.0/32,i%13<7,(i%100)/100.0,"two");Check(m.Lift>=0&&m.Lift<=1&&m.Speed>=0&&m.Speed<=200&&!double.IsNaN(m.ArmAngle),"bounded mechanics rapid input "+i);}
  }
  static void Ui(string music,string output)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.StartPosition=FormStartPosition.Manual;w.Location=new Point(-20000,-20000);w.Show();Pump(80);VintageSurface skin=Field<VintageSurface>(w,"vintage");
    Field<Dictionary<string,int>>(w,"manualSelections")[Key(music)]=11;Call(w,"AddFiles",(object)new string[]{music});
    Wait(delegate{return Field<WaveformView>(w,"wave").PreviewPeaks!=null;},15000,"real song waveform ready");
    Check(Field<int>(w,"actualDensity")==11,"density above visual scale retained");Call(w,"SetManualDensity",-6);Check(Field<int>(w,"actualDensity")==-6&&UserPrefs.Load().ExtendedDensity==-6,"negative density persists without clamping to dial");Call(w,"SetManualDensity",11);
    for(int lang=0;lang<3;lang++)foreach(int width in new int[]{435,600,800})
    {Call(w,"ChangeLanguage",lang);w.ClientSize=new Size(width,(int)Math.Round(width*1616.0/973));Call(w,"RefreshVintage");Pump(50);Shot(w,Path.Combine(output,"UI-"+lang+"-"+width+".png"));}
    w.ClientSize=new Size(435,722);Call(w,"ChangeLanguage",0);Call(w,"RefreshVintage");Pump(50);
    foreach(int volume in new int[]{0,37,100,0}){Call(w,"VintageCommand","volume",(double)volume);Shot(w,Path.Combine(output,"fader-"+volume+".png"));}
    Point hover=skin.FromArt(480,1525);Call(skin,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,hover.X,hover.Y,0));Shot(w,Path.Combine(output,"export-hover.png"));Check(Field<string>(skin,"hover")=="export","export hover state entered");Call(skin,"OnMouseLeave",EventArgs.Empty);Check(Field<string>(skin,"hover")=="","hover state clears on leave");
    Call(w,"VintageCommand","compare",-1.0);Check(Field<StudioABSwitch>(w,"comparison").Processed,"supplied round AB control routes processed source");
    VintageState state=skin.State;state.Busy=true;state.Status="Создаю MP3";state.Progress=50;state.JobIndex=0;state.JobCount=2;skin.Bind(state);Shot(w,Path.Combine(output,"progress-50.png"));state.Progress=100;skin.Bind(state);Shot(w,Path.Combine(output,"progress-100.png"));Call(w,"RefreshVintage");
    for(int lang=0;lang<3;lang++)using(DensityInput d=new DensityInput(-6,lang)){d.StartPosition=FormStartPosition.Manual;d.Location=new Point(-20000,-20000);d.Show();Pump(30);Shot(d,Path.Combine(output,"integer-dialog-"+lang+".png"));d.Close();}
    Call(w,"VintageCommand","volume",100.0);w.Close();
   }
  }
  static void Queue(string output)
  {
   Directory.CreateDirectory(output);string first=Path.Combine(output,"first.wav"),second=Path.Combine(output,"second.wav");Wav(first,3.2);Wav(second,2.2);UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.StartPosition=FormStartPosition.Manual;w.Location=new Point(-20000,-20000);w.Show();Pump(80);var manual=Field<Dictionary<string,int>>(w,"manualSelections");manual[Key(first)]=manual[Key(second)]=0;
    Call(w,"AddFiles",(object)new string[]{first,second});Call(w,"VintageCommand","volume",0.0);Call(w,"VintageCommand","play",0.0);
    Wait(delegate{PreviewPlayer p=Field<PreviewPlayer>(w,"player");return p!=null&&p.IsPlaying&&!p.IsPaused&&Position(p)>.35;},8000,"pending play starts only after head landing");
    Call(w,"VintageCommand","play",0.0);PreviewPlayer paused=Field<PreviewPlayer>(w,"player");Check(paused.IsPaused,"pause pauses actual WinMM immediately");double saved=Position(paused);Pump(1100);Check(Math.Abs(Position(paused)-saved)<.03,"pause parks head without advancing actual sound");Check(Field<VintageSurface>(w,"vintage").Mechanics.Phase=="parked","pause removes head from record");
    Call(w,"VintageCommand","play",0.0);Wait(delegate{return !paused.IsPaused;},5000,"resume waits for head return");Check(Position(paused)>=saved&&Position(paused)<saved+.4,"resume sound clock returns to same place");
    Call(w,"Seek",2.95);Wait(delegate{return Field<string>(w,"activeFile")==second;},8000,"natural EOF advances queue once");
    Wait(delegate{PreviewPlayer p=Field<PreviewPlayer>(w,"player");return p!=null&&p.IsPlaying&&!p.IsPaused&&Position(p)>.05;},5000,"second track starts after returning to outer groove");Check(Field<VintageSurface>(w,"vintage").Mechanics.ArmAngle<8,"second track begins near outer groove");
    Wait(delegate{return Field<PreviewPlayer>(w,"player")==null&&!Field<bool>(w,"pendingPlay");},7000,"last EOF stops without looping queue");Pump(1000);Check(Field<VintageSurface>(w,"vintage").Mechanics.Phase=="parked","end of queue parks head");
    Call(w,"VintageCommand","play",0.0);Call(w,"VintageCommand","stop",0.0);Pump(1200);Check(Field<PreviewPlayer>(w,"player")==null&&Field<double>(w,"position")==0,"stop during pending motion cannot start later");w.Close();
   }
  }
  [STAThread]static int Main(string[] args)
  {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Motion();Ui(Path.GetFullPath(args[0]),Path.GetFullPath(args[1]));Queue(Path.Combine(Path.GetFullPath(args[1]),"queue"));return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
