using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace PeakMp3Win7
{
 internal static class Qa
 {
  static void Check(bool condition,string text){if(!condition)throw new Exception(text);Console.WriteLine("PASS: "+text);}
  static T Field<T>(object target,string name){return (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
  static object Call(object target,string name,params object[] args){return target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
  static void Pump(int ms){Stopwatch w=Stopwatch.StartNew();while(w.ElapsedMilliseconds<ms){Application.DoEvents();Thread.Sleep(8);}}
  static void NoTab(Control c){Check(!c.TabStop,"Tab disabled: "+c.GetType().Name);foreach(Control child in c.Controls)NoTab(child);}
  [DllImport("winmm.dll")]static extern uint waveOutGetNumDevs();
  [STAThread]static int Main(string[] args)
  {
   try
   {
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    if(args[0]=="unit")Unit();
    else if(args[0]=="render")Render(args[1],args[2]);
    else if(args[0]=="convert")Convert(args[1],int.Parse(args[2]),args[3]);
    else if(args[0]=="meter")Meter(args[1],args[2]);
    else if(args[0]=="playback")Playback(args[1],args[2]);
    else if(args[0]=="integration")Integration(args[1],args[2],args[3]);
    else if(args[0]=="assembly")AssemblySmoke(args[1],args[2]);
    else if(args[0]=="parity")Parity(args[1]);
    else if(args[0]=="provider")ProviderParity(args[1]);
    else if(args[0]=="hardware")Hardware(args[1]);
    else if(args[0]=="desktop")DesktopExport(args[1],args[2]);
    else if(args[0]=="lowdensity")LowDensity(args[1],args[2]);
    return 0;
   }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
  }
  static void Unit()
  {
   UserPrefs.TestProfile=Path.Combine(Path.GetTempPath(),"PeakMP3-QA-"+Guid.NewGuid().ToString("N"));
   UserPrefs p=UserPrefs.Load();Check(p.VuReference==0,"default reference 0 dBFS");Check(p.DenseStrength==7,"density default retained");
   Check(!typeof(Button).IsAssignableFrom(typeof(StudioButton)),"painted controls have no native BUTTON background/text renderer");
   Check(StudioButton.PresetBars("presetMax")>StudioButton.PresetBars("presetLoud"),"maximum has more bars than loud");
   using(StudioSurface surface=new StudioSurface())using(StudioButton button=new StudioButton())
   {
    surface.Size=new Size(240,160);surface.Controls.Add(button);button.SetBounds(20,20,180,100);button.BackColor=Color.FromArgb(248,247,244);
    int clicks=0;button.Click+=delegate{clicks++;};button.PerformClick();button.Enabled=false;button.PerformClick();button.Enabled=true;
    Check(clicks==1,"custom button activation respects enabled state");
    Call(button,"OnKeyDown",new KeyEventArgs(Keys.Enter));Call(button,"OnKeyUp",new KeyEventArgs(Keys.Space));
    Check(clicks==3,"custom buttons retain Enter and Space activation");
    foreach(string role in new string[]{"","gear","minimize","closeWindow","addFiles","removeFile","presetSoft","export","play","log"})
    {
     button.Tag=role;button.Text="Test";
     using(Bitmap image=new Bitmap(button.Width,button.Height))using(Graphics g=Graphics.FromImage(image))
     {g.Clear(Color.Magenta);PaintEventArgs paint=new PaintEventArgs(g,new Rectangle(Point.Empty,image.Size));Call(button,"OnPaintBackground",paint);Call(button,"OnPaint",paint);
      Check(image.GetPixel(0,0).R>150&&image.GetPixel(button.Width-1,button.Height-1).R>150,"all button margins painted: "+role);}
    }
    using(Form dialog=new Form()){dialog.Controls.Add(button);button.DialogResult=DialogResult.OK;dialog.AcceptButton=button;((IButtonControl)button).PerformClick();Check(dialog.DialogResult==DialogResult.OK,"settings OK supports IButtonControl and DialogResult");}
   }
   float[] samples=new float[3000];samples[0]=.7f;samples[1022]=-.9f;samples[1023]=1.1f;samples[1024]=1.4f;
   float pl=0,pr=0;ReferencePeakWindow.Measure(samples,0,512,ref pl,ref pr);
   Check(pl==.9f&&pr==1.1f,"512-sample absolute peaks include last frame and exclude the next");
   using(PreviewPlayer window=new PreviewPlayer("unused","unused",false,false,0))
   {
    List<MeterFrame> frames=Field<List<MeterFrame>>(window,"meterFrames");
    float[] all=new float[6000];for(int i=0;i<all.Length;i++)all[i]=(float)(.7*Math.Sin(i*.13));
    all[2799]=1.2f;all[2800]=-1.3f;
    for(int k=0;k<3;k++){MeterFrame f=new MeterFrame();f.Start=k*1000;f.End=f.Start+1000;f.Samples=new float[2000];Array.Copy(all,k*2000,f.Samples,0,2000);frames.Add(f);}
    typeof(PreviewPlayer).GetField("queuedFrames",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,(long)3000);
    typeof(PreviewPlayer).GetField("completedFrames",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,(long)950);
    double l,r,t;bool active;string error;window.Snapshot(out l,out r,out t,out active,out error);
    pl=pr=0;ReferencePeakWindow.Measure(all,950,512,ref pl,ref pr);
    Check(l==MeterEnvelope.Db(pl)&&r==MeterEnvelope.Db(pr),"hardware-position peak window spans queued buffer boundaries");
   }
   p.DenseStrength=10;p.VuReference=0;p.Language=1;p.Save();p=UserPrefs.Load();Check(p.DenseStrength==10&&p.VuReference==0&&p.Language==1,"settings persisted in isolated profile");
   Check(p.OutputFolder==Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"output is always the actual desktop");
   string legacy=Path.Combine(Path.Combine(UserPrefs.TestProfile,"PeakMp3Win7"),"settings.ini");File.AppendAllText(legacy,"\noutputFolder=C:\\ignored-legacy-folder\n");
   Check(UserPrefs.Load().OutputFolder==Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"legacy custom output folder is ignored");
   for(int kind=0;kind<3;kind++){byte[] sound=UiSounds.Wave(kind);Check(System.Text.Encoding.ASCII.GetString(sound,0,4)=="RIFF"&&BitConverter.ToInt32(sound,24)==22050&&sound.Length>800,"short generated tactile sound "+kind);}
   using(ApprovedVu dial=new ApprovedVu()){dial.Size=new Size(400,180);int fired=0;dial.CalibrationRequested+=delegate{fired++;};Call(dial,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,200,169,0));Check(fired==1,"trimmer screw opens calibration action");}
   using(StudioCalibrationDialog dialog=new StudioCalibrationDialog(0,0)){Call(dialog,"Adjust",-1);Check(dialog.Reference==-1,"styled calibration steps down");Call(dialog,"Adjust",1);Check(dialog.Reference==0,"calibration can return to zero");}
   MeterEnvelope m=new MeterEnvelope();for(int i=0;i<30;i++)m.Push(1,0,1,0,.02);Check(Math.Abs(MeterEnvelope.Db(m.Left))<1e-6,"constant full-scale signal settles at 0 dBFS");
   for(int i=0;i<30;i++)m.Push(1.1,0,1.1,0,.02);Check(MeterEnvelope.Db(m.Left)>.82,"above-zero decoded samples retained");
   m=new MeterEnvelope();for(int i=0;i<30;i++)m.Push(.5,0,.5,0,.02);double withoutRms=m.Left;
   m=new MeterEnvelope();for(int i=0;i<30;i++)m.Push(.5,100,.5,100,.02);Check(m.Left==withoutRms,"needle driven by peaks only, independent of RMS");
   for(int i=0;i<40;i++)m.Push(0,0,0,0,.02);Check(m.Left<=.100001,"silence returns to the lower end of the scale");
   m=new MeterEnvelope();for(int i=0;i<500;i++)m.Push(.99,.25,.99,.25,.02);Check(MeterEnvelope.Db(m.Left)<0,"below-zero source never fabricates zero hits");
   using(ApprovedVu vu=new ApprovedVu()){for(int i=0;i<30;i++)vu.SetLevels(-2,.2);Check(Math.Abs(vu.DisplayDb-.2)<1e-5,"single meter chooses higher channel");Check(ApprovedVu.AngleFor(-1)>ApprovedVu.AngleFor(0)&&ApprovedVu.AngleFor(0)>ApprovedVu.AngleFor(1),"zero and positive markings monotonic");}
   using(StudioKnob k=new StudioKnob())
   {k.Size=new Size(400,370);for(int v=0;v<=10;v++){double a=(210-v*24)*Math.PI/180;Point pt=new Point((int)Math.Round(200+170*Math.Cos(a)),(int)Math.Round(204-170*Math.Sin(a)));Call(k,"SelectAt",pt);Check(k.Value==v,"exact knob mouse selection "+v);}}
   using(ApprovedWindow w=new ApprovedWindow()){using(Graphics screen=Graphics.FromHwnd(IntPtr.Zero))Check(w.ClientSize.Width<500*screen.DpiX/96,"default window is about one third narrower");w.Show();Pump(80);Check(!Field<StudioButton>(w,"settings").Visible,"header gear removed");Check(!Field<StudioButton>(w,"buttonLog").Visible,"journal button removed");Check(Field<StudioButton>(w,"buttonPlay").Left<Field<StudioButton>(w,"buttonCreate").Left,"listen is left and create is right");foreach(Control c in w.Controls)NoTab(c);bool tab=(bool)typeof(ApprovedWindow).GetMethod("ProcessDialogKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,new object[]{Keys.Tab});Check(tab,"Tab navigation swallowed");w.Close();}
   Console.WriteLine("Runtime "+Environment.Version+"; OS "+Environment.OSVersion);
  }
  static void Convert(string file,int strength,string output)
  {
   JobOptions job=new JobOptions();job.Inputs=new string[]{file};job.OutputFolder=output;job.DenseMode=true;job.DenseStrength=strength;
   new ProcessingEngine(Path.Combine(Application.StartupPath,"ffmpeg.exe"),new BackgroundWorker(),delegate(string k,string v){Console.WriteLine(k+" "+v);}).Convert(job);
  }
  static void LowDensity(string file,string output)
  {
   Directory.CreateDirectory(output);string binary=Path.Combine(Application.StartupPath,"ffmpeg.exe");int analyses=0;
   ProcessingEngine engine=new ProcessingEngine(binary,new BackgroundWorker(),delegate(string key,string value){if(key=="peakAnalysis")analyses++;});
   ProcessResult measured=engine.AnalyzeForPreview(file,false,true);
   Check(ProcessingEngine.FilterReady(null,true,0),"original endpoint needs no analysis");
   for(int level=0;level<11;level++){engine.PrepareDenseOutput(file,measured,level);Check(ProcessingEngine.FilterReady(measured,true,level),"shared Maximum analysis ready for blend "+level);string graph=ProcessingEngine.FilterForOptions(measured,false,true,level);Check(!graph.Contains("agate="),"no gate in blend "+level);}
   Check(analyses==0,"no extra whole-track peak passes for blend changes");
   Check(ProcessingEngine.FilterForOptions(measured,false,true,0)=="aresample=44100","density0 original");
   Check(ProcessingEngine.FilterForOptions(measured,false,true,10)==ProcessingEngine.DenseFilter(measured,10),"density10 unchanged Maximum");
  }
  static void Hardware(string output)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   for(int kind=0;kind<3;kind++)File.WriteAllBytes(Path.Combine(output,"tactile-"+kind+".wav"),UiSounds.Wave(kind));
   using(System.Media.SoundPlayer player=new System.Media.SoundPlayer(Path.Combine(output,"tactile-1.wav"))){player.Load();player.PlaySync();Check(true,"Windows SoundPlayer accepts and plays the tactile tick");}
   for(int language=0;language<3;language++)using(StudioCalibrationDialog dialog=new StudioCalibrationDialog(language,0))
   {
    dialog.Location=new Point(-20000,-20000);dialog.StartPosition=FormStartPosition.Manual;dialog.Show();Pump(80);
    using(Bitmap image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(output,"calibration-"+language+".png"));}
    dialog.Close();
   }
   Check(true,"tactile WAV files and three styled calibration dialogs rendered");
  }
  static void DesktopExport(string silence,string output)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   string input=Path.Combine(Path.GetTempPath(),"PeakMP3_Desktop_Test_"+Guid.NewGuid().ToString("N")+".wav");File.Copy(silence,input);
   string exported=null;
   try
   {
    using(ApprovedWindow w=new ApprovedWindow())
    {
     w.Location=new Point(-20000,-20000);w.Show();Call(w,"AddFiles",(object)new string[]{input});Call(w,"StartJob");
     BackgroundWorker worker=Field<BackgroundWorker>(w,"worker");Stopwatch clock=Stopwatch.StartNew();while(worker.IsBusy&&clock.ElapsedMilliseconds<20000)Pump(20);Pump(150);
     Check(!worker.IsBusy,"desktop export completes");Dictionary<string,string> outputs=Field<Dictionary<string,string>>(w,"outputs");
     Check(outputs.TryGetValue(input,out exported)&&File.Exists(exported),"actual UI job publishes its MP3");
     Check(Path.GetFullPath(Path.GetDirectoryName(exported))==Path.GetFullPath(UserPrefs.DesktopOutputFolder),"actual MP3 lands directly on desktop");
     Process p=new Process();p.StartInfo.FileName=Path.Combine(Application.StartupPath,"ffmpeg.exe");p.StartInfo.Arguments="-nostdin -hide_banner -loglevel error -xerror -i \""+exported+"\" -f null -";p.StartInfo.UseShellExecute=false;p.StartInfo.CreateNoWindow=true;p.Start();p.WaitForExit();Check(p.ExitCode==0,"desktop MP3 fully decodes");p.Dispose();w.Close();
    }
   }
   finally
   {
    if(exported!=null&&Path.GetFileName(exported).StartsWith("PeakMP3_Desktop_Test_",StringComparison.Ordinal)&&Path.GetFullPath(Path.GetDirectoryName(exported))==Path.GetFullPath(UserPrefs.DesktopOutputFolder))File.Delete(exported);
    File.Delete(input);
   }
  }
  static void Render(string output,string file)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.StartPosition=FormStartPosition.Manual;w.Location=new Point(-20000,-20000);w.ClientSize=new Size(846,1465);w.Show();Pump(80);Call(w,"AddFiles",(object)new string[]{file});Pump(2000);
    Field<StudioKnob>(w,"knob").Value=10;for(int meterFrame=0;meterFrame<40;meterFrame++)Field<ApprovedVu>(w,"meter").SetLevels(-1.2,-2);
    Field<WaveformView>(w,"wave").PositionSeconds=155;
    for(int lang=0;lang<3;lang++)for(int scale=0;scale<4;scale++)
    {
     int[] widths={435,520,652,815};w.ClientSize=new Size(widths[scale],1000);Call(w,"ChangeLanguage",lang);Pump(100);
     Check(w.Region!=null&&w.Region.IsVisible(w.Width/2,w.Height/2)&&!w.Region.IsVisible(0,0),"rounded casing retains content "+lang+"/"+scale);
     Panel header=Field<Panel>(w,"header"),body=Field<Panel>(w,"panel");for(int meterFrame=0;meterFrame<40;meterFrame++)Field<ApprovedVu>(w,"meter").SetLevels(-1.2,-2);
     Label density=Field<Label>(w,"densityCaption");StudioKnob knob=Field<StudioKnob>(w,"knob");Panel fileCard=Field<Panel>(w,"fileCard");
     Check(knob.Bottom<=density.Top&&density.Bottom<=fileCard.Top,"density label cannot be obscured by knob or file card "+lang+"/"+scale);
     Check(!Field<Panel>(w,"viewport").HorizontalScroll.Visible,"no horizontal scrollbar "+lang+"/"+scale);
     using(Bitmap image=new Bitmap(Math.Max(header.Width,body.Width),header.Height+body.Height))
     {header.DrawToBitmap(image,new Rectangle(0,0,header.Width,header.Height));body.DrawToBitmap(image,new Rectangle(0,header.Height,body.Width,body.Height));image.Save(Path.Combine(output,"UI-"+lang+"-"+scale+".png"));}
     foreach(Control c in body.Controls)if(c.Visible&&c!=Field<StudioButton>(w,"buttonStop"))Check(c.Left>=0&&c.Right<=body.Width+1,"horizontal layout "+lang+"/"+scale+" "+c.GetType().Name);
    }
    Call(w,"ChangeLanguage",0);w.Close();
   }
  }
  static void Playback(string file,string output)
  {
   Directory.CreateDirectory(output);Console.WriteLine("WinMM output devices: "+waveOutGetNumDevs());
   using(PreviewPlayer p=new PreviewPlayer(Path.Combine(Application.StartupPath,"ffmpeg.exe"),file,false,false,0))
   {
    p.Play(0);double l,r,t;bool active;string error;Stopwatch clock=Stopwatch.StartNew();
    do{Thread.Sleep(30);p.Snapshot(out l,out r,out t,out active,out error);if(error!=null)throw new Exception(error);}while(t<.15&&clock.ElapsedMilliseconds<7000);
    Check(t>.1&&active,"real WinMM playback clock advances");Console.WriteLine("Float output: "+p.FloatOutput);
    p.Pause();Thread.Sleep(70);p.Snapshot(out l,out r,out t,out active,out error);double before=t;Thread.Sleep(250);p.Snapshot(out l,out r,out t,out active,out error);Check(Math.Abs(t-before)<.025,"pause freezes hardware position");
    p.Resume();Thread.Sleep(200);p.Snapshot(out l,out r,out t,out active,out error);Check(t>before+.08,"resume continues hardware position");
    p.Play(2);Thread.Sleep(250);p.Snapshot(out l,out r,out t,out active,out error);Check(t>=2&&t<2.8,"seek resets hardware clock to requested offset");p.Stop();Check(!p.IsPlaying,"stop closes playback");
   }
  }
  static void Parity(string folder)
  {
   int count=0;
   foreach(string file in Directory.GetFiles(folder,"*.csv"))
   {
    AnalogMeterChannel l=new AnalogMeterChannel(),r=new AnalogMeterChannel();int n=0;
    ApprovedVu display=new ApprovedVu();
    foreach(string line in File.ReadAllLines(file))
    {
     if(line.StartsWith("frame,"))continue;string[] a=line.Split(',');float[] expected=new float[a.Length];
     for(int j=0;j<a.Length;j++)expected[j]=float.Parse(a[j],System.Globalization.CultureInfo.InvariantCulture);
     l.Tick(expected[1]);r.Tick(expected[2]);
     display.SetLevels(MeterEnvelope.Db(expected[1]),MeterEnvelope.Db(expected[2]));
     double expectedDb=Math.Max(-20,Math.Min(3,MeterEnvelope.Db(Math.Max(l.Level,r.Level))));
     if(Math.Abs(display.DisplayDb-expectedDb)>.000001)throw new Exception("Actual UI meter differs from native reference at frame "+n);
     float[] actual={l.Angle,r.Angle,l.Level,r.Level,l.Delta,l.Target};
     for(int j=0;j<actual.Length;j++)if(BitConverter.ToInt32(BitConverter.GetBytes(actual[j]),0)!=BitConverter.ToInt32(BitConverter.GetBytes(expected[j+3]),0))throw new Exception("Reference mismatch "+Path.GetFileName(file)+" frame "+n+" field "+j);
     double angle=ApprovedVu.AngleFor(MeterEnvelope.Db(Math.Max(l.Level,r.Level)));
     if(Math.Abs(angle-(90-Math.Max(l.Angle,r.Angle)))>.00003)throw new Exception("Rendered angle mapping differs from reference");
     n++;count++;
    }
    display.Dispose();
    Check(n>1000,"native AIMP float32 parity and rendered-angle mapping: "+Path.GetFileName(file)+" ("+n+" frames)");
   }
   Check(count>=11520,"all eight reference songs before and after processing");
  }
  static uint Next(ref uint state){state^=state<<13;state^=state>>17;state^=state<<5;return state;}
  static void ProviderParity(string file)
  {
   int n=0;
   foreach(string line in File.ReadAllLines(file))
   {
    if(line.StartsWith("seed,"))continue;string[] values=line.Split(',');uint seed=uint.Parse(values[0]);
    float[] samples=new float[1024];for(int i=0;i<samples.Length;i++)samples[i]=((int)(Next(ref seed)&0xffffff)-8388608)/8388608f;
    float left=0,right=0;ReferencePeakWindow.Measure(samples,0,512,ref left,ref right);
    float el=float.Parse(values[1],System.Globalization.CultureInfo.InvariantCulture),er=float.Parse(values[2],System.Globalization.CultureInfo.InvariantCulture);
    Check(left==el&&right==er,"native 512-sample provider parity "+n++);
   }
   Check(n>=128,"all native provider fixtures compared");
  }
  static void AssemblySmoke(string path,string output)
  {
   Directory.CreateDirectory(output);Assembly app=Assembly.LoadFile(Path.GetFullPath(path));Console.WriteLine(app.GetName().Name+" metadata "+app.ImageRuntimeVersion);
   app.GetType("PeakMp3Win7.UserPrefs").GetField("TestProfile",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Path.Combine(output,"profile"));
   using(Form form=(Form)Activator.CreateInstance(app.GetType("PeakMp3Win7.ApprovedWindow"),true))
   {form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Pump(100);
    Check(form.Region!=null&&form.Region.IsVisible(form.Width/2,form.Height/2),"compiled application has rounded casing and visible center");Check(form.Visible,"compiled form created and shown");
    using(Bitmap bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(output,"compiled-window.png"));}
    form.Close();}
   using(Icon icon=Icon.ExtractAssociatedIcon(path))Check(icon!=null,"application icon embedded");
  }
  static void WaitPlayer(PreviewPlayer p)
  {double l,r,t;bool a;string e;Stopwatch clock=Stopwatch.StartNew();do{long before=clock.ElapsedMilliseconds;Pump(20);p.Snapshot(out l,out r,out t,out a,out e);Console.WriteLine("wait: elapsed="+clock.ElapsedMilliseconds+" pump="+(clock.ElapsedMilliseconds-before)+" position="+t+" active="+a);if(e!=null)throw new Exception(e);}while(t<.1&&clock.ElapsedMilliseconds<6000);Check(p.IsPlaying,"preview active");}
  static void Integration(string silence,string music,string output)
  {
   Directory.CreateDirectory(output);UserPrefs.TestProfile=Path.Combine(output,"profile");
   using(ApprovedWindow w=new ApprovedWindow())
   {
    w.Location=new Point(-20000,-20000);w.Show();Call(w,"AddFiles",(object)new string[]{silence,silence});Check(Field<List<string>>(w,"files").Count==1,"duplicate queue input ignored");
    Call(w,"StartPlayback");try{WaitPlayer(Field<PreviewPlayer>(w,"player"));}catch{Console.WriteLine(Field<TextBox>(w,"logData").Text);throw;}
    PreviewPlayer a=Field<PreviewPlayer>(w,"player");a.Pause();Call(w,"Seek",2.0);Pump(100);
    Check(Field<PreviewPlayer>(w,"player").IsPaused,"seek retains pause");
    Field<StudioABSwitch>(w,"comparison").Processed=true;Pump(350);
    PreviewPlayer b=Field<PreviewPlayer>(w,"player");Check(b.IsPaused,"A/B retains pause");
    var cache=Field<Dictionary<string,ProcessResult>>(w,"analysisCache");Stopwatch wait=Stopwatch.StartNew();while(cache.Count==0&&wait.ElapsedMilliseconds<6000)Pump(20);
    if(cache.Count!=1){double dl,dr,dt;bool da;string de;b.Snapshot(out dl,out dr,out dt,out da,out de);Console.WriteLine(Field<TextBox>(w,"logData").Text);Console.WriteLine("B cache diagnostic: active="+da+" time="+dt+" error="+de);ProcessResult dm=Field<ProcessResult>(b,"measured");Console.WriteLine("Measured="+(dm==null?"null":("peak="+dm.HasPeak+" rms="+dm.HasRms+" silent="+dm.IsSilent)));}Check(cache.Count==1,"B analysis cached once");ProcessResult known=null;foreach(ProcessResult r in cache.Values)known=r;
    Field<StudioKnob>(w,"knob").Value=9;Pump(650);Check(Field<PreviewPlayer>(w,"player").IsPaused,"live density retains pause");Check(cache.Count==1,"live density shares analysis cache");
    foreach(ProcessResult r in cache.Values)Check(Object.ReferenceEquals(r,known),"cached analysis reused without replacement");
    w.Close();
   }
   string cancelled=Path.Combine(output,"cancel");Directory.CreateDirectory(cancelled);
   BackgroundWorker worker=new BackgroundWorker();worker.WorkerSupportsCancellation=true;ManualResetEvent encoding=new ManualResetEvent(false);Exception failure=null;
   ProcessingEngine engine=new ProcessingEngine(Path.Combine(Application.StartupPath,"ffmpeg.exe"),worker,delegate(string key,string value){if(key=="encode")encoding.Set();});
   JobOptions job=new JobOptions();job.Inputs=new string[]{music};job.OutputFolder=cancelled;job.DenseMode=true;job.DenseStrength=10;
   Thread thread=new Thread(delegate(){try{engine.Convert(job);}catch(Exception e){failure=e;}});thread.Start();Check(encoding.WaitOne(20000,false),"cancellation reaches encoding");Thread.Sleep(130);worker.CancelAsync();engine.Cancel();Check(thread.Join(10000),"cancel worker finishes");Check(failure is OperationCanceledException,"cancel reports cancellation");Check(Directory.GetFiles(cancelled).Length==0,"cancel removes partial and publishes no MP3");
   string bad=Path.Combine(output,"invalid.mp3");File.WriteAllText(bad,"not audio");job.Inputs=new string[]{bad};job.OutputFolder=Path.Combine(output,"failure");failure=null;
   try{new ProcessingEngine(Path.Combine(Application.StartupPath,"ffmpeg.exe"),new BackgroundWorker(),delegate(string k,string v){}).Convert(job);}catch(Exception e){failure=e;}
   Check(failure!=null,"invalid audio fails clearly");Check(Directory.GetFiles(job.OutputFolder).Length==0,"failed conversion leaves no partial output");
  }
  static void Meter(string file,string output)
  {
   Process p=new Process();p.StartInfo.FileName=Path.Combine(Application.StartupPath,"ffmpeg.exe");p.StartInfo.Arguments="-nostdin -hide_banner -loglevel error -ss 155 -i \""+file+"\" -t 45 -ar 44100 -ac 2 -c:a pcm_f32le -f f32le pipe:1";p.StartInfo.UseShellExecute=false;p.StartInfo.CreateNoWindow=true;p.StartInfo.RedirectStandardOutput=true;p.Start();
   MeterEnvelope floating=new MeterEnvelope(),clipped=new MeterEnvelope();List<double> values=new List<double>(),old=new List<double>();byte[] bytes=new byte[1379*8];int block=0;double peak=0;
   using(StreamWriter csv=new StreamWriter(output))
   {
    csv.WriteLine("seconds,float_meter_db,pcm16_meter_db,decoded_sample_peak_db");
    for(;;){int read=0,n,wanted=MeterEnvelope.SamplesInFrame(block)*8;while(read<wanted&&(n=p.StandardOutput.BaseStream.Read(bytes,read,wanted-read))>0)read+=n;if(read<8)break;read-=read%8;double pl=0,pr=0,el=0,er=0,cl=0,cr=0,cel=0,cer=0;
     for(int i=0;i<Math.Min(read,ReferencePeakWindow.SampleCount*8);i+=8){double l=BitConverter.ToSingle(bytes,i),r=BitConverter.ToSingle(bytes,i+4);pl=Math.Max(pl,Math.Abs(l));pr=Math.Max(pr,Math.Abs(r));l=Math.Max(-1,Math.Min(32767/32768.0,l));r=Math.Max(-1,Math.Min(32767/32768.0,r));cl=Math.Max(cl,Math.Abs(l));cr=Math.Max(cr,Math.Abs(r));}
     int frames=read/8;floating.Push(pl,el/frames,pr,er/frames,frames/44100.0);clipped.Push(cl,cel/frames,cr,cer/frames,frames/44100.0);double value=MeterEnvelope.Db(Math.Max(floating.Left,floating.Right)),vold=MeterEnvelope.Db(Math.Max(clipped.Left,clipped.Right));values.Add(value);old.Add(vold);peak=Math.Max(peak,Math.Max(pl,pr));csv.WriteLine((155+block/32.0).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+value.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+vold.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+MeterEnvelope.Db(Math.Max(pl,pr)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));block++;}
   }
   p.WaitForExit();Check(p.ExitCode==0,"reference MP3 float decoding");values.Sort();old.Sort();Console.WriteLine("frames="+values.Count+" median="+values[values.Count/2]+" pcm16median="+old[old.Count/2]+" p10="+values[values.Count/10]+" p90="+values[values.Count*9/10]+" max="+values[values.Count-1]+" samplepeak="+MeterEnvelope.Db(peak));
  }
 }
}
