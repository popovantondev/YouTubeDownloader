// Render the existing desktop control with explicitly labelled demonstration data.
using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
namespace PeakMp3Win7 {
 internal static class RenderSiteScreens {
  [STAThread]static int Main(string[] args){try{
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Directory.CreateDirectory(args[1]);
   string[] langs={"ru","de","en"};string[] labels={"Демонстрационная запись","Demoaufnahme","Demonstration track"};
   for(int i=0;i<3;i++)using(VintageSurface panel=new VintageSurface(args[0])){
    panel.Size=new Size(973,1616);panel.CreateControl();VintageState state=new VintageState();state.Language=i;state.HasFile=true;state.Density=8;state.Volume=80;state.Vu=-3.4;state.Duration=213;state.Position=73;state.QueueCount=1;state.FileName=labels[i];state.Metadata="MP3 · 44.1 kHz · 320 kbit/s";state.Label=new RecordName("PeakMP3",labels[i]);state.Wave=new float[300];for(int j=0;j<300;j++)state.Wave[j]=(float)(.1+.7*Math.Abs(Math.Sin(j*.31)*Math.Sin(j*.069)));
    panel.Bind(state);using(Bitmap b=new Bitmap(973,1616)){panel.DrawToBitmap(b,new Rectangle(0,0,973,1616));b.Save(Path.Combine(args[1],"peakmp3-"+langs[i]+".png"),ImageFormat.Png);}
   }return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
