using System;using System.IO;using System.Drawing;using System.Collections.Generic;using System.Reflection;using System.Windows.Forms;using System.Diagnostics;using System.Threading;
namespace PeakMp3Win7
{
 internal static class VintageQa
 {
  static T Field<T>(object x,string name){return (T)x.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(x);}
  static object Call(object x,string name,params object[] args){return x.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(x,args);}
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS: "+label);}
  static void Pump(int ms){Stopwatch clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<ms){Application.DoEvents();Thread.Sleep(5);}}
  [STAThread]static int Main(string[] args)
  {
   try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args[0]=="stress")Stress(Path.GetFullPath(args[1]));else Run(Path.GetFullPath(args[0]),Path.GetFullPath(args[1]));return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  }
  static void Run(string file,string output)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.StartPosition=FormStartPosition.Manual;w.Location=new Point(-20000,-20000);w.Show();Pump(100);
    VintageSurface skin=Field<VintageSurface>(w,"vintage");Check(skin.Visible&&!skin.TabStop,"bitmap interface visible without Tab focus");
    Check(!Field<Panel>(w,"header").Visible&&!Field<Panel>(w,"viewport").Visible,"legacy rendering hidden; existing processing model retained");
    Check(VintageSurface.Hit(new PointF(480,435))=="calibrate","calibration screw remains an actual action");
    Check(VintageSurface.NeedleForDb(0)==new PointF(604,234),"0 dBFS needle endpoint exactly matches the artwork's zero tick");
    Check(VintageSurface.Hit(new PointF(881,895))=="file","removed folder button leaves clickable file card");
    Check(VintageSurface.Hit(new PointF(500,991))=="seek","waveform itself is seek target");
    for(int volume=0;volume<=100;volume++)
    {
     RectangleF thumb=VintageSurface.FaderThumb(volume);Check(Math.Abs(thumb.X+thumb.Width/2-883.5)<.01,"fader centered on slot at "+volume);
     for(int lamp=0;lamp<10;lamp++)Check(!VintageSurface.LampOn(volume,lamp)||1306+lamp*16>=VintageSurface.FaderY(volume),"no lit lamp above fader "+volume+"/"+lamp);
    }
    Check(!VintageSurface.LampOn(0,9)&&VintageSurface.LampOn(100,0),"volume0 has no lamps, volume100 reaches top lens");
    for(int level=0;level<11;level++){double angle=(150+level*24)*Math.PI/180;PointF pt=new PointF((float)(481+120*Math.Cos(angle)),(float)(663+120*Math.Sin(angle)));Check(VintageSurface.DensityAt(pt)==level,"artwork knob detent "+level);}
    Call(w,"AddFiles",(object)new string[]{file});Stopwatch wait=Stopwatch.StartNew();while(Field<WaveformView>(w,"wave").PreviewPeaks==null&&wait.ElapsedMilliseconds<15000)Pump(25);
    Check(Field<WaveformView>(w,"wave").PreviewPeaks!=null,"real waveform published from the decoder through the retained hidden handle");
    Call(w,"VintageCommand","compare",-1.0);bool beforeToggle=Field<StudioABSwitch>(w,"comparison").Processed;
    RectangleF currentThumb=VintageSurface.ComparisonThumb(beforeToggle);Point capPoint=skin.FromArt(currentThumb.X+22,currentThumb.Y+22);
    Call(skin,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,capPoint.X,capPoint.Y,0));Call(skin,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,capPoint.X,capPoint.Y,0));
    Check(Field<StudioABSwitch>(w,"comparison").Processed!=beforeToggle,"click directly on round A/B thumb toggles mode");
    Field<StudioKnob>(w,"knob").Value=10;
    for(int language=0;language<3;language++)foreach(int width in new int[]{435,600,800})
    {
     Call(w,"ChangeLanguage",language);w.ClientSize=new Size(width,(int)Math.Round(width*1616.0/973));Call(w,"RefreshVintage");Pump(40);
     Rectangle fit=skin.ArtBounds;Check(Math.Abs(fit.Width*1616.0/973-fit.Height)<2,"reference aspect ratio retained "+language+"/"+width);
     Point p=skin.FromArt(785,577);Check(VintageSurface.Hit(skin.ToArt(p))=="loud","scaled hit coordinates stay aligned "+language+"/"+width);
     using(Bitmap b=new Bitmap(w.ClientSize.Width,w.ClientSize.Height)){w.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(output,"skin-"+language+"-"+width+".png"));}
    }
    Call(w,"VintageCommand","volume",37.0);Check(Field<int>(w,"previewVolume")==37&&UserPrefs.Load().PreviewVolume==37,"preview volume persisted per user");
    Call(w,"VintageCommand","max",0.0);Check(Field<StudioKnob>(w,"knob").Value==10,"physical Maximum controls the existing density model");
    Call(w,"VintageCommand","balance",0.0);Check(Field<StudioKnob>(w,"knob").Value==5,"physical Balanced controls the existing density model");
    Call(w,"VintageCommand","compare",1.0);Check(Field<StudioABSwitch>(w,"comparison").Processed,"A/B hardware slider selects processed source");
    Call(w,"VintageCommand","compare",0.0);Check(!Field<StudioABSwitch>(w,"comparison").Processed,"A/B hardware slider selects original source");
    Call(w,"VintageCommand","volume",0.0);Call(w,"VintageCommand","play",0.0);Call(w,"Seek",155.0);
    PreviewPlayer muted=Field<PreviewPlayer>(w,"player");double ml,mr,mt;bool ma;string me;Stopwatch audioWait=Stopwatch.StartNew();
    do{Pump(20);muted.Snapshot(out ml,out mr,out mt,out ma,out me);if(me!=null)throw new Exception(me);}while(mt<155.25&&audioWait.ElapsedMilliseconds<10000);
    Check(ma&&mt>155.2&&muted.Volume==0,"actual WinMM preview advances at zero output gain");
    Check(Math.Max(ml,mr)>-20,"mute retains real unscaled source peaks for VU");
    Call(w,"VintageCommand","play",0.0);Check(muted.IsPaused,"physical play button pauses the real player");
    double pausedClock=mt;Pump(80);muted.Snapshot(out ml,out mr,out mt,out ma,out me);pausedClock=mt;Pump(120);muted.Snapshot(out ml,out mr,out mt,out ma,out me);Check(Math.Abs(mt-pausedClock)<.025,"hardware clock frozen while record is paused");
    Call(w,"VintageCommand","stop",0.0);Check(Field<PreviewPlayer>(w,"player")==null,"physical stop disposes the real preview");
    VintageState original=skin.State;VintageState playing=new VintageState();playing.Playing=true;playing.HasFile=true;
    skin.Bind(playing);Pump(80);skin.Bind(playing);double angleBefore=skin.Rotation;Check(angleBefore>0,"record animation advances on play");
    playing.Paused=true;skin.Bind(playing);double paused=skin.Rotation;Pump(100);skin.Bind(playing);Check(skin.Rotation==paused,"record freezes on pause");
    playing.Playing=false;playing.Paused=false;skin.Bind(playing);double stopped=skin.Rotation;Pump(100);skin.Bind(playing);Check(skin.Rotation==stopped,"record remains still on stop");
    Call(w,"RefreshVintage");Call(w,"VintageCommand","volume",100.0);
    w.ClientSize=new Size(435,722);Call(w,"ChangeLanguage",0);Call(w,"RefreshVintage");Pump(30);
    Process process=Process.GetCurrentProcess();Console.WriteLine("Private bytes after assets/rendering: "+process.PrivateMemorySize64);
    w.Close();
   }
  }
  static void Stress(string silence)
  {
   int snapshots=0;
   for(int i=0;i<40;i++)using(PreviewPlayer player=new PreviewPlayer(Path.Combine(Application.StartupPath,"ffmpeg.exe"),silence,false,false,0))
   using(ManualResetEvent finish=new ManualResetEvent(false))
   {
    Exception failure=null;player.Volume=0;
    Thread reader=new Thread(delegate(){try{while(!finish.WaitOne(0,false)){double l,r,t;bool active;string error;player.Snapshot(out l,out r,out t,out active,out error);if(error!=null)throw new Exception(error);Interlocked.Increment(ref snapshots);Thread.Sleep(1);}}catch(Exception ex){failure=ex;}});
    player.Play(5.85);reader.Start();Thread.Sleep(45);player.Pause();Thread.Sleep(5);player.Resume();
    if(i%2==0){Thread.Sleep(35);player.Stop();}
    else{Stopwatch watch=Stopwatch.StartNew();while(player.IsPlaying&&watch.ElapsedMilliseconds<4000)Thread.Sleep(2);Check(!player.IsPlaying,"natural EOF releases device "+i);}
    player.Pause();player.Resume();player.Stop();finish.Set();Check(reader.Join(4000),"snapshot reader ends "+i);if(failure!=null)throw failure;
   }
   Check(snapshots>100,"parallel snapshots across 40 stop/EOF/pause/resume device lifetimes: "+snapshots);
  }
 }
}
