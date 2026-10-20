using System;using System.IO;using System.Drawing;using System.Windows.Forms;
namespace PeakMp3Win7
{
 internal static class DesignReviewQa
 {
  private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS: "+message);}
  [STAThread]private static int Main()
  {
   try{
    RecordName a=RecordName.Resolve("ugly download [MP3 320].mp3","Björk","Jóga (Live)");Check(a.Artist=="Björk"&&a.Title=="Jóga (Live)","Unicode tags take precedence; meaningful parentheses preserved");
    a=RecordName.Resolve("ABBA - Gimme! Gimme! Gimme! (A Man After Midnight) [MP3 320].mp3","","");Check(a.Artist=="ABBA"&&a.Title=="Gimme! Gimme! Gimme! (A Man After Midnight)","fallback filename strips only technical annotation");
    a=RecordName.Resolve("A - Song [Live] [Remastered].mp3","","");Check(a.Title=="Song [Live] [Remastered]","unknown brackets survive");
    Check(RecordName.Clean("  Tom\u200b &amp; Jerry\r\n  Live  ")=="Tom & Jerry Live","invisible formatting and whitespace cleaned, entities decoded");
    a=RecordName.FromDiagnostic("fallback.mp3","Metadata:\n  artist : Земфира\n  title : Искала\n Duration: 00:01:00\n Stream:\n Metadata:\n  title : ignored stream title\n");Check(a.Artist=="Земфира"&&a.Title=="Искала","root tags only; stream title cannot overwrite song");
    Check(RecordLabel.Variant(new RecordName("ABBA","SOS"))==2,"short title gets label2");Check(RecordLabel.Variant(new RecordName("Artist",new string('W',200)))==3,"long title gets label3");
    for(int n=0;n<10;n++){Check(VintageSurface.LampOn(100,n),"all lamps on at100 row"+n);Check(!VintageSurface.LampOn(0,n),"all lamps off at0 row"+n);}
    for(int v=0;v<=100;v++){RectangleF f=VintageSurface.FaderThumb(v);Check(Math.Abs(f.Y+f.Height/2-VintageSurface.FaderY(v))<.01,"central fader anchor "+v);}
    Application.EnableVisualStyles();Directory.CreateDirectory("TESTS/RESULTS/DESIGN_REVIEW_2");
    using(VintageSurface surface=new VintageSurface(Path.Combine(Application.StartupPath,"ASSETS/Vintage/Runtime")))using(LayeredSkin skin=new LayeredSkin())
    {surface.Size=new Size(435,722);VintageState state=new VintageState();state.HasFile=true;state.FileName="ABBA — The Day Before You Came (Video)";state.Metadata="MP3 · 13.23 MB · 44100 Hz · 320 kb/s";state.Label=new RecordName("ABBA","The Day Before You Came (Video)");state.Wave=new float[]{.3f,.5f,.8f,.7f,.4f,.6f};state.Duration=346;state.Position=139;state.QueueCount=3;state.QueueIndex=1;state.Density=8;
     for(int lang=0;lang<3;lang++)foreach(int volume in new int[]{0,50,100}){state.Language=lang;state.Volume=volume;surface.Bind(state);Bitmap b=skin.Render(surface);b.Save("TESTS/RESULTS/DESIGN_REVIEW_2/UI-"+lang+"-"+volume+".png");Check(b.GetPixel(0,0).A==0,"transparent corner");int partial=0;for(int y=0;y<18;y++)for(int x=0;x<18;x++){int alpha=b.GetPixel(x,y).A;if(alpha>0&&alpha<255)partial++;}Check(partial>0,"antialiased corner coverage");}
    }
    return 0;
   }catch(Exception e){Console.Error.WriteLine(e);return 1;}
  }
 }
}
