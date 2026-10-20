// Physical UI helpers and local tactile sounds. Does not process exported audio.
using System;
using System.IO;
using System.Text;
using System.Media;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
namespace PeakMp3Win7
{
    internal static class StudioHardware
    {
        internal static void RoundWindow(Form form,int radius)
        {
            if(form.ClientSize.Width<2||form.ClientSize.Height<2)return;
            using(GraphicsPath shape=StudioPalette.RoundRect(new Rectangle(0,0,form.ClientSize.Width,form.ClientSize.Height),radius))
            {Region previous=form.Region;form.Region=new Region(shape);if(previous!=null)previous.Dispose();}
        }
        internal static void ParentBackground(Control control,Graphics g)
        {
            g.Clear(control.Parent==null?StudioPalette.Silver:control.Parent.BackColor);
            StudioSurface surface=control.Parent as StudioSurface;
            if(surface!=null){GraphicsState state=g.Save();g.TranslateTransform(-control.Left,-control.Top);surface.PaintSurface(g);g.Restore(state);}
        }
        internal static void Screw(Graphics g,RectangleF r,bool cross)
        {
            using(Brush shadow=new SolidBrush(Color.FromArgb(90,0,0,0)))g.FillEllipse(shadow,r.X+1,r.Y+2,r.Width,r.Height);
            using(LinearGradientBrush steel=new LinearGradientBrush(r,Color.FromArgb(247,246,236),Color.FromArgb(65,65,60),48))
            using(Pen rim=new Pen(Color.FromArgb(44,43,38),Math.Max(1,r.Width*.055f)))
            {g.FillEllipse(steel,r);g.DrawEllipse(rim,r);}
            float cx=r.X+r.Width/2,cy=r.Y+r.Height/2,d=r.Width*.30f;
            using(Pen shine=new Pen(Color.FromArgb(205,255,255,248),r.Width*.12f))g.DrawLine(shine,cx-d,cy+d*.5f-1,cx+d,cy-d*.5f-1);
            using(Pen groove=new Pen(Color.FromArgb(35,33,28),r.Width*.105f))
            {groove.StartCap=groove.EndCap=LineCap.Round;g.DrawLine(groove,cx-d,cy+d*.5f,cx+d,cy-d*.5f);if(cross)g.DrawLine(groove,cx-d*.5f,cy-d,cx+d*.5f,cy+d);}
        }
    }
    internal static class UiSounds
    {
        private static readonly Stopwatch clock=Stopwatch.StartNew();
        private static long lastTick=-100;
        private static readonly SoundPlayer button=Player(0),knob=Player(1),toggle=Player(2);
        private static SoundPlayer Player(int kind)
        {SoundPlayer p=new SoundPlayer(new MemoryStream(Wave(kind)));try{p.Load();}catch{}return p;}
        internal static byte[] Wave(int kind)
        {
            const int rate=22050;int count=kind==1?420:kind==2?990:790;
            MemoryStream stream=new MemoryStream();BinaryWriter writer=new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*2);uint state=724193;
            for(int i=0;i<count;i++)
            {
                state^=state<<13;state^=state>>17;state^=state<<5;
                double t=i/(double)rate,noise=((state&65535)/32768.0-1),decay=Math.Exp(-t*(kind==1?420:210));
                double pulse=(.55*noise+.45*Math.Sin(2*Math.PI*(kind==1?1800:kind==2?1050:1400)*t))*decay;
                if(kind==2&&i>330)pulse+=.3*Math.Sin(2*Math.PI*700*t)*Math.Exp(-(i-330)/110.0);
                writer.Write((short)(Math.Max(-1,Math.Min(1,pulse))*(kind==1?1900:2600)));
            }
            writer.Flush();return stream.ToArray();
        }
        private static bool Allowed {get{return UserPrefs.TestProfile==null;}}
        internal static void Button(){if(Allowed)try{button.Play();}catch{}}
        internal static void Toggle(){if(Allowed)try{toggle.Play();}catch{}}
        internal static void Tick(){if(!Allowed||clock.ElapsedMilliseconds-lastTick<24)return;lastTick=clock.ElapsedMilliseconds;try{knob.Play();}catch{}}
    }
    internal sealed class StudioCalibrationDialog : NoTabDialog
    {
        internal int Reference {get;private set;}
        private readonly Label value;
        private readonly StudioSurface surface;
        internal StudioCalibrationDialog(int language,int reference)
        {
            Reference=reference;Text=L10n.Get("calibrationTitle",language);FormBorderStyle=FormBorderStyle.None;
            StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(400,246);BackColor=StudioPalette.Silver;
            Font=new Font("Tahoma",9);DoubleBuffered=true;
            surface=new StudioSurface();surface.BorderVisible=true;surface.Dock=DockStyle.Fill;Controls.Add(surface);
            Label title=new Label();title.Text=Text;title.BackColor=Color.Transparent;title.Font=new Font("Tahoma",13,FontStyle.Bold);
            title.SetBounds(20,16,320,30);surface.Controls.Add(title);
            StudioButton close=Button(surface,"",new Rectangle(351,11,35,32),delegate{DialogResult=DialogResult.Cancel;});close.Tag="closeWindow";
            value=new Label();value.BackColor=Color.Transparent;value.TextAlign=ContentAlignment.MiddleCenter;value.Font=new Font("Tahoma",22,FontStyle.Bold);
            value.SetBounds(74,65,252,47);surface.Controls.Add(value);
            Button(surface,"−",new Rectangle(22,67,46,43),delegate{Adjust(-1);});
            Button(surface,"+",new Rectangle(332,67,46,43),delegate{Adjust(1);});
            Label hint=new Label();hint.Text=L10n.Get("calibrationHint",language);hint.BackColor=Color.Transparent;
            hint.TextAlign=ContentAlignment.MiddleCenter;hint.SetBounds(20,126,360,46);surface.Controls.Add(hint);
            Button(surface,"0 dBFS",new Rectangle(22,190,145,39),delegate{Reference=0;RefreshValue();});
            StudioButton ok=Button(surface,"OK",new Rectangle(231,190,147,39),delegate{DialogResult=DialogResult.OK;});ok.BackColor=StudioPalette.Orange;AcceptButton=ok;
            RefreshValue();StudioHardware.RoundWindow(this,12);
        }
        private void Adjust(int change){Reference=Math.Max(-24,Math.Min(3,Reference+change));RefreshValue();}
        private void RefreshValue(){value.Text=Reference.ToString("+0;-0;0")+" dBFS";}
        private static StudioButton Button(Control parent,string text,Rectangle bounds,EventHandler click)
        {StudioButton b=new StudioButton();b.Text=text;b.Bounds=bounds;b.BackColor=Color.FromArgb(239,235,224);b.Click+=click;parent.Controls.Add(b);return b;}
    }
}
