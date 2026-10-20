// PeakMP3 0.9 - original vector controls recreating the approved studio composition.
// GDI+ / System.Windows.Forms only, compatible with .NET Framework 3.5.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PeakMp3Win7
{
    internal sealed class NoFocusListBox : ListBox
    { protected override bool ShowFocusCues { get { return false; } } }
    internal class NoTabDialog : Form
    {
        protected override bool ProcessDialogKey(Keys keys){return (keys&Keys.KeyCode)==Keys.Tab||base.ProcessDialogKey(keys);}
        protected override void OnShown(EventArgs e){DisableTab(this);base.OnShown(e);}
        private static void DisableTab(Control root){foreach(Control c in root.Controls){c.TabStop=false;DisableTab(c);}}
    }
    internal static class StudioPalette
    {
        public static readonly Color Orange = Color.FromArgb(250, 106, 14);
        public static readonly Color Ink = Color.FromArgb(45, 44, 42);
        public static readonly Color Silver = Color.FromArgb(229, 227, 222);
        public static readonly Color Face = Color.FromArgb(248, 244, 237);
        public static readonly Color Frame = Color.FromArgb(170, 166, 161);
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Min(Math.Min(r.Width, r.Height), Math.Max(2, radius * 2));
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
        public static PointF Polar(float cx, float cy, float radius, double degrees)
        {
            double a = degrees * Math.PI / 180.0;
            return new PointF(cx + radius * (float)Math.Cos(a), cy - radius * (float)Math.Sin(a));
        }
        public static void CenterString(Graphics g, string text, Font font, Brush brush, PointF center)
        {
            SizeF s = g.MeasureString(text, font);
            g.DrawString(text, font, brush, center.X - s.Width / 2f, center.Y - s.Height / 2f);
        }
    }

    internal sealed class StudioLogo : Control
    {
        public StudioLogo() { DoubleBuffered = true; ResizeRedraw = true; SetStyle(ControlStyles.SupportsTransparentBackColor,true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen p = new Pen(StudioPalette.Orange, Math.Max(3f, Width / 10f)))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                int[] heights = { 12, 27, 41, 27, 12 };
                for (int i = 0; i < 5; i++)
                {
                    float x = Width / 2f + (i - 2) * Width / 5.8f;
                    float h = heights[i] * Height / 45f;
                    g.DrawLine(p, x, Height / 2f - h / 2f, x, Height / 2f + h / 2f);
                }
            }
        }
    }

    // Glossy rounded buttons matching the user's approved visual hierarchy.
    // The Click/keyboard/drag behavior remains the standard WinForms behavior.
    internal sealed class StudioButton : Control, IButtonControl
    {
        private bool isPressed;
        public DialogResult DialogResult { get; set; }
        public void NotifyDefault(bool value) { }
        public void PerformClick(){if(Enabled)OnClick(EventArgs.Empty);}
        public StudioButton()
        {
            Cursor=Cursors.Hand;TabStop=false;AccessibleRole=AccessibleRole.PushButton;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.StandardClick|ControlStyles.Selectable,true);
        }
        protected override bool ShowFocusCues{get{return false;}}
        protected override void OnClick(EventArgs e)
        {if(!Enabled)return;UiSounds.Button();if(DialogResult!=DialogResult.None){Form f=FindForm();if(f!=null)f.DialogResult=DialogResult;}base.OnClick(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter){PerformClick();e.Handled=e.SuppressKeyPress=true;}base.OnKeyDown(e);}
        protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Space){PerformClick();e.Handled=true;}base.OnKeyUp(e);}
        protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);Invalidate();}
        protected override void OnMouseDown(MouseEventArgs e){if(Enabled&&e.Button==MouseButtons.Left){isPressed=true;Invalidate();}base.OnMouseDown(e);}
        protected override void OnMouseUp(MouseEventArgs e){isPressed=false;Invalidate();base.OnMouseUp(e);}
        protected override void OnMouseCaptureChanged(EventArgs e){isPressed=false;Invalidate();base.OnMouseCaptureChanged(e);}
        protected override void OnPaintBackground(PaintEventArgs e){StudioHardware.ParentBackground(this,e.Graphics);}
        internal static int PresetBars(string role){return role=="presetBalance"?3:role=="presetLoud"?4:role=="presetMax"?6:0;}
        private Rectangle PaintCap(Graphics g,bool dark,bool selected)
        {
            bool down=isPressed||selected;Rectangle well=new Rectangle(2,2,Width-5,Height-6);
            if(well.Width<6||well.Height<6)return ClientRectangle;
            using(GraphicsPath outer=StudioPalette.RoundRect(well,4))using(LinearGradientBrush steel=new LinearGradientBrush(well,Color.FromArgb(50,48,42),Color.FromArgb(143,139,123),90))using(Pen rim=new Pen(Color.FromArgb(50,48,41),1))
            {GraphicsState state=g.Save();g.TranslateTransform(0,2);using(Brush shadow=new SolidBrush(Color.FromArgb(85,0,0,0)))g.FillPath(shadow,outer);g.Restore(state);g.FillPath(steel,outer);g.DrawPath(rim,outer);}
            Rectangle cap=Rectangle.Inflate(well,-3,-3);cap.Y+=down?2:0;cap.Height-=down?1:2;
            if(cap.Width<2||cap.Height<2)return well;
            Color top=dark?Color.FromArgb(100,98,85):selected?Color.FromArgb(183,139,74):Color.FromArgb(235,228,207);
            Color bottom=dark?Color.FromArgb(35,34,29):selected?Color.FromArgb(224,188,126):Color.FromArgb(159,156,139);
            using(GraphicsPath face=StudioPalette.RoundRect(cap,3))
            {
                using(LinearGradientBrush body=new LinearGradientBrush(cap,top,bottom,90))g.FillPath(body,face);
                GraphicsState clip=g.Save();g.SetClip(face,CombineMode.Intersect);
                Rectangle gloss=new Rectangle(cap.X,cap.Y,cap.Width,Math.Max(2,cap.Height*2/3));
                using(LinearGradientBrush polish=new LinearGradientBrush(gloss,Color.FromArgb(down?35:95,255,255,241),Color.FromArgb(0,255,255,241),90))g.FillRectangle(polish,gloss);
                if(down)using(LinearGradientBrush inset=new LinearGradientBrush(new Rectangle(cap.X,cap.Y,cap.Width,Math.Min(7,cap.Height)),Color.FromArgb(110,18,15,9),Color.FromArgb(0,18,15,9),90))g.FillRectangle(inset,cap.X,cap.Y,cap.Width,Math.Min(7,cap.Height));
                g.Restore(clip);using(Pen edge=new Pen(down?Color.FromArgb(82,64,37):Color.FromArgb(130,126,109),1))g.DrawPath(edge,face);
            }
            return cap;
        }
        private void Caption(Graphics g,RectangleF box,Color color)
        {
            using(StringFormat f=new StringFormat())using(Brush b=new SolidBrush(color))
            {f.Alignment=StringAlignment.Center;f.LineAlignment=StringAlignment.Center;f.Trimming=StringTrimming.EllipsisCharacter;f.FormatFlags=StringFormatFlags.NoWrap;g.DrawString(Text,Font,b,box,f);}
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Width<8||Height<8)return;
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            string role=Tag as string??"";bool preset=role.StartsWith("preset",StringComparison.Ordinal);
            bool selected=BackColor.R>240&&BackColor.G>=170&&BackColor.G<=228&&BackColor.B<206;
            bool dark=role=="play"||role=="export";Rectangle cap=PaintCap(g,dark,selected);
            RectangleF content=RectangleF.Inflate(cap,-2,-2);GraphicsState state=g.Save();g.SetClip(content,CombineMode.Intersect);
            Color ink=!Enabled?Color.FromArgb(136,132,116):dark?Color.FromArgb(246,236,203):Color.FromArgb(35,32,24);
            if(preset)
            {
                float captionHeight=Math.Min(content.Height*.46f,Font.GetHeight(g)+2);
                RectangleF caption=new RectangleF(content.X,content.Bottom-captionHeight,content.Width,captionHeight);
                RectangleF icon=new RectangleF(content.X+content.Width*.25f,content.Y+1,content.Width*.5f,Math.Max(3,caption.Top-content.Y-3));
                using(Pen line=new Pen(selected?Color.FromArgb(146,65,17):ink,Math.Max(1.3f,Math.Min(2.5f,icon.Height*.14f))))
                {
                    line.StartCap=line.EndCap=LineCap.Round;float pad=line.Width/2+1;icon.Inflate(-pad,-pad);
                    if(role=="presetSoft")
                    {PointF[] wave=new PointF[31];for(int i=0;i<31;i++)wave[i]=new PointF(icon.Left+icon.Width*i/30,icon.Top+icon.Height*.5f-(float)Math.Sin(i*Math.PI*2/30)*icon.Height*.30f);g.DrawCurve(line,wave);}
                    else
                    {int count=PresetBars(role);for(int i=0;i<count;i++){float h=icon.Height*(.30f+.70f*i/Math.Max(1,count-1));float x=icon.Left+icon.Width*i/Math.Max(1,count-1);g.DrawLine(line,x,icon.Bottom-h,x,icon.Bottom);}}
                }
                Caption(g,caption,ink);
            }
            else if(role=="minimize"||role=="closeWindow"||role=="removeFile")
            {
                float cx=content.X+content.Width/2,cy=content.Y+content.Height/2,d=Math.Min(content.Width,content.Height)*.27f;
                using(Pen line=new Pen(ink,Math.Max(1.2f,d*.18f)))
                {if(role=="minimize")g.DrawLine(line,cx-d,cy,cx+d,cy);else{g.DrawLine(line,cx-d,cy-d,cx+d,cy+d);g.DrawLine(line,cx+d,cy-d,cx-d,cy+d);}}
            }
            else if(role=="addFiles")
            {
                float cx=content.X+content.Width/2,cy=content.Y+content.Height/2,r=Math.Min(content.Width,content.Height)*.39f;
                using(LinearGradientBrush rim=new LinearGradientBrush(new RectangleF(cx-r,cy-r,2*r,2*r),Color.FromArgb(204,200,178),Color.FromArgb(67,65,54),50))using(Pen edge=new Pen(ink,1))
                {g.FillEllipse(rim,cx-r,cy-r,2*r,2*r);g.DrawEllipse(edge,cx-r,cy-r,2*r,2*r);}
                using(Brush hole=new SolidBrush(Color.FromArgb(42,39,30)))g.FillEllipse(hole,cx-r*.66f,cy-r*.66f,r*1.32f,r*1.32f);
                using(Pen plus=new Pen(Color.FromArgb(232,221,183),Math.Max(1,r*.16f))){g.DrawLine(plus,cx-r*.35f,cy,cx+r*.35f,cy);g.DrawLine(plus,cx,cy-r*.35f,cx,cy+r*.35f);}
            }
            else
            {
                RectangleF text=content;
                if(role=="play"||role=="export")
                {
                    float cx=content.X+content.Width*.12f,cy=content.Y+content.Height/2,d=Math.Min(content.Height*.25f,content.Width*.045f);
                    using(Brush b=new SolidBrush(ink))using(Pen line=new Pen(ink,Math.Max(1,d*.2f)))
                    {if(role=="play")g.FillPolygon(b,new PointF[]{new PointF(cx-d*.6f,cy-d),new PointF(cx+d,cy),new PointF(cx-d*.6f,cy+d)});else{g.DrawRectangle(line,cx-d,cy-d*.7f,2*d,d*1.4f);g.DrawLine(line,cx-d*.6f,cy,cx+d*.6f,cy);}}
                    text.X+=content.Width*.21f;text.Width-=content.Width*.23f;
                }
                Caption(g,text,ink);
            }
            g.Restore(state);
        }
    }

    internal sealed class StudioDropZone : Panel
    {
        public StudioDropZone()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(250, 248, 246);
        }
        protected override void OnPaintBackground(PaintEventArgs e){StudioHardware.ParentBackground(this,e.Graphics);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath rr = StudioPalette.RoundRect(bounds, 8))
            using (Pen dash = new Pen(Color.FromArgb(167, 164, 160), 1.3f))
            {
                dash.DashStyle = DashStyle.Dash;
                e.Graphics.DrawPath(dash, rr);
            }
        }
    }

    internal sealed class StudioTitle : Control
    {
        public StudioTitle() { DoubleBuffered = true; ResizeRedraw = true; SetStyle(ControlStyles.SupportsTransparentBackColor,true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.ScaleTransform(Width/235f,Height/56f);
            using(Font f=new Font("Tahoma",42,FontStyle.Bold,GraphicsUnit.Pixel))using(Brush ink=new SolidBrush(StudioPalette.Ink))using(Brush orange=new SolidBrush(StudioPalette.Orange))
            {g.DrawString("Peak",f,ink,0,0,StringFormat.GenericTypographic);float x=g.MeasureString("Peak",f,1000,StringFormat.GenericTypographic).Width-1;g.DrawString("MP3",f,orange,x,0,StringFormat.GenericTypographic);}
        }
    }

    // Approved two-position A/B comparison. B plays a finished MP3 if the
    // matching density has been exported, otherwise a live processed preview.
    internal sealed class StudioABSwitch : Control
    {
        private bool processed;
        public event EventHandler ModeChanged;
        public string Caption = "Сравнение:";
        public string Original = "A — оригинал";
        public string ProcessedText = "B — после обработки";
        public bool Processed
        {
            get { return processed; }
            set { if (processed == value) return; processed = value; Invalidate();
                if (ModeChanged != null) ModeChanged(this, EventArgs.Empty); }
        }
        public StudioABSwitch()
        {
            DoubleBuffered = true; ResizeRedraw = true; Cursor = Cursors.Hand;
            SetStyle(ControlStyles.Selectable, true); TabStop = false;
            BackColor = Color.FromArgb(250, 249, 247);
        }
        protected override void OnClick(EventArgs e)
        { base.OnClick(e); Focus();UiSounds.Toggle(); Processed = !Processed; }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {UiSounds.Toggle(); Processed = e.KeyCode == Keys.Left ? false : e.KeyCode == Keys.Right ? true : !Processed; e.Handled = true; }
            base.OnKeyDown(e);
        }
        protected override void OnPaintBackground(PaintEventArgs e){StudioHardware.ParentBackground(this,e.Graphics);}
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Width/790f,Height/74f);
            using(GraphicsPath border=StudioPalette.RoundRect(new Rectangle(1,1,787,71),10))
            using(LinearGradientBrush face=new LinearGradientBrush(new Rectangle(1,1,787,71),Color.FromArgb(245,242,232),Color.FromArgb(210,208,197),90))
            using(Pen edge=new Pen(Color.FromArgb(153,149,137),1.4f)){g.FillPath(face,border);g.DrawPath(edge,border);}
            using(Font strong=new Font("Tahoma",24,FontStyle.Regular,GraphicsUnit.Pixel))using(Font small=new Font("Tahoma",19,FontStyle.Regular,GraphicsUnit.Pixel))
            using(Brush ink=new SolidBrush(StudioPalette.Ink))using(Brush sub=new SolidBrush(Color.FromArgb(106,102,92)))
            {
                g.DrawString(Caption,strong,ink,26,24);g.DrawString("A",strong,ink,309,24);g.DrawString("B",strong,ink,463,24);
                Rectangle slot=new Rectangle(343,16,106,44);
                using(GraphicsPath rim=StudioPalette.RoundRect(slot,7))using(LinearGradientBrush steel=new LinearGradientBrush(slot,Color.FromArgb(95,91,76),Color.FromArgb(202,195,173),90))using(Pen edge=new Pen(Color.FromArgb(72,67,51),1.5f)){g.FillPath(steel,rim);g.DrawPath(edge,rim);}
                using(Brush dark=new SolidBrush(Color.FromArgb(40,35,24)))g.FillRectangle(dark,356,31,81,15);
                float cx=processed?426:367,cy=37;
                using(Brush shadow=new SolidBrush(Color.FromArgb(100,0,0,0)))g.FillEllipse(shadow,cx-19,cy-15,40,40);
                RectangleF knob=new RectangleF(cx-18,cy-18,36,36);
                using(LinearGradientBrush metal=new LinearGradientBrush(knob,Color.FromArgb(237,231,205),Color.FromArgb(89,85,67),50))using(Pen edge=new Pen(Color.FromArgb(52,46,30),2)){g.FillEllipse(metal,knob);g.DrawEllipse(edge,knob);}
                using(Pen ridge=new Pen(Color.FromArgb(103,96,73),1.2f))for(int j=-2;j<=2;j++)g.DrawLine(ridge,cx+j*4,cy-8,cx+j*4,cy+8);
                using(Brush polish=new SolidBrush(Color.FromArgb(85,255,255,237)))g.FillEllipse(polish,cx-12,cy-14,19,10);
                using(Pen line=new Pen(Color.FromArgb(162,157,145),1.4f))g.DrawLine(line,508,13,508,61);
                g.DrawString(Original,small,sub,537,12);g.DrawString(ProcessedText,small,sub,537,38);
            }
        }
    }

    // Static dial face is cached. Only the needle and its hub are repainted in playback.
    internal sealed class ApprovedVu : Control
    {
        internal static readonly int[] Labels = { -20, -10, -7, -5, -3, -2, -1, 0, 1, 2, 3 };
        private static readonly double[] Angles = CalibratedAngles();
        private static double[] CalibratedAngles()
        {double[] result=new double[Labels.Length];for(int i=0;i<result.Length;i++)result[i]=AngleFor(Labels[i]);return result;}
        private double current = -20;
        private readonly MeterEnvelope movement=new MeterEnvelope();
        private Bitmap face,glass;
        internal event EventHandler CalibrationRequested;
        public ApprovedVu() { DoubleBuffered = true; ResizeRedraw = true; SetStyle(ControlStyles.SupportsTransparentBackColor,true); BackColor = Color.Transparent; }
        internal static double AngleFor(double db)
        {
            double amplitude=Math.Pow(10,Math.Max(-20,Math.Min(3,db))/20.0);
            double a=amplitude<1.0?-52.0+72.0*(amplitude-(double)AnalogMeterChannel.MinLevel)/(1.0-(double)AnalogMeterChannel.MinLevel)
                :20.0+32.0*(amplitude-1.0)/((double)AnalogMeterChannel.MaxLevel-1.0);
            return 90.0-a;
        }
        private static PointF Oval(float rx, float ry, double angle)
        { double a = angle * Math.PI / 180; return new PointF(500 + rx * (float)Math.Cos(a), 440 - ry * (float)Math.Sin(a)); }
        internal double DisplayDb { get { return current; } }
        public void SetLevels(double left, double right)
        {
            double l=double.IsNaN(left)||double.IsInfinity(left)?0:Math.Pow(10,left/20.0);
            double r=double.IsNaN(right)||double.IsInfinity(right)?0:Math.Pow(10,right/20.0);
            movement.Push(l,0,r,0,1.0/MeterEnvelope.FramesPerSecond);
            double value=Math.Max(-20,Math.Min(3,MeterEnvelope.Db(Math.Max(movement.Left,movement.Right))));
            bool repaint=Math.Abs(current-value)>.0001;current=value;
            if(repaint)Invalidate();
        }
        protected override void OnSizeChanged(EventArgs e)
        { if (face != null) { face.Dispose(); face = null; }if(glass!=null){glass.Dispose();glass=null;} base.OnSizeChanged(e); }
        protected override void Dispose(bool disposing)
        { if (disposing && face != null) { face.Dispose(); face = null; }if(disposing&&glass!=null){glass.Dispose();glass=null;} base.Dispose(disposing); }
        private void DrawFace(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.ScaleTransform(Width / 1000f, Height / 450f);
            Rectangle[] rings = { new Rectangle(2,2,996,446),new Rectangle(9,9,982,432),new Rectangle(18,18,964,414),new Rectangle(25,25,950,400) };
            Color[] tops = { Color.FromArgb(90,87,81),Color.FromArgb(245,241,232),Color.FromArgb(106,102,93),Color.FromArgb(242,230,211) };
            Color[] bottoms = { Color.FromArgb(222,219,211),Color.FromArgb(139,132,119),Color.FromArgb(236,231,218),Color.FromArgb(252,249,242) };
            for (int i=0;i<rings.Length;i++) using(GraphicsPath path=StudioPalette.RoundRect(rings[i],18-i*3))
            using(LinearGradientBrush fill=new LinearGradientBrush(rings[i],tops[i],bottoms[i],90))
            using(Pen edge=new Pen(i==3?Color.FromArgb(163,151,132):Color.FromArgb(115,111,101),1.6f))
            {g.FillPath(fill,path);g.DrawPath(edge,path);}
            // Warm diffused illumination is cached with the plate; no per-frame texture work.
            using(GraphicsPath glow=new GraphicsPath())
            {
                glow.AddEllipse(140,15,720,500);
                using(PathGradientBrush light=new PathGradientBrush(glow))
                {light.CenterColor=Color.FromArgb(120,255,255,250);light.SurroundColors=new Color[]{Color.FromArgb(0,255,255,250)};g.FillPath(light,glow);}
            }
            // Warm face illumination and a subtle brushed plate, not a raster skin.
            using(Pen light=new Pen(Color.FromArgb(190,255,255,255),2))
            {g.DrawLine(light,47,31,950,31);g.DrawLine(light,47,416,950,416);}
            using(Pen arc=new Pen(Color.FromArgb(25,24,22),3))
            using(Pen hot=new Pen(Color.FromArgb(234,66,32),15))
            using(Pen tick=new Pen(Color.FromArgb(25,24,22),2.6f))
            using(Brush ink=new SolidBrush(Color.FromArgb(25,24,22)))
            using(Font labels=new Font("Arial",36,FontStyle.Bold,GraphicsUnit.Pixel))
            using(Font title=new Font("Arial",50,FontStyle.Bold,GraphicsUnit.Pixel))
            {
                PointF[] outline=new PointF[241];for(int i=0;i<outline.Length;i++)outline[i]=Oval(440,317,Angles[0]+(Angles[10]-Angles[0])*i/240.0);g.DrawLines(arc,outline);
                using(GraphicsPath red=new GraphicsPath())
                {
                    PointF[] band=new PointF[162];for(int i=0;i<=80;i++){double angle=Angles[7]+(Angles[10]-Angles[7])*i/80.0;band[i]=Oval(446,325,angle);band[161-i]=Oval(434,309,angle);}red.AddPolygon(band);
                    using(Brush redFill=new SolidBrush(Color.FromArgb(235,66,33)))g.FillPath(redFill,red);
                }
                for(int i=0;i<Labels.Length;i++)
                {g.DrawLine(tick,Oval(424,301,Angles[i]),Oval(453,335,Angles[i]));
                 StudioPalette.CenterString(g,Labels[i]>0?"+"+Labels[i]:Labels[i].ToString(),labels,ink,Oval(480,365,Angles[i]));}
                StudioPalette.CenterString(g,"VU",title,ink,new PointF(500,280));
            }
            float[] xs={47,953};float[] ys={50,397};
            foreach(float x in xs)foreach(float y in ys)StudioHardware.Screw(g,new RectangleF(x-21,y-21,42,42),true);
        }
        private bool IsTrimmer(Point p)
        {double x=p.X*1000.0/Math.Max(1,Width)-500,y=p.Y*450.0/Math.Max(1,Height)-422;return x*x+y*y<45*45;}
        protected override void OnMouseMove(MouseEventArgs e){Cursor=IsTrimmer(e.Location)?Cursors.Hand:Cursors.Default;base.OnMouseMove(e);}
        protected override void OnMouseUp(MouseEventArgs e)
        {base.OnMouseUp(e);if(e.Button==MouseButtons.Left&&IsTrimmer(e.Location)){UiSounds.Toggle();if(CalibrationRequested!=null)CalibrationRequested(this,EventArgs.Empty);}}
        private void DrawGlass(Graphics g)
        {
            g.ScaleTransform(Width/1000f,Height/450f);g.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath clip=StudioPalette.RoundRect(new Rectangle(26,26,948,398),9))
            {
                g.SetClip(clip);
                using(LinearGradientBrush light=new LinearGradientBrush(new Rectangle(27,27,946,390),Color.FromArgb(55,255,255,250),Color.FromArgb(0,255,255,255),90))g.FillRectangle(light,27,27,946,390);
                using(Brush reflection=new SolidBrush(Color.FromArgb(52,255,255,255)))g.FillPolygon(reflection,new PointF[]{new PointF(40,56),new PointF(952,29),new PointF(952,130),new PointF(40,295)});
                using(Pen edge=new Pen(Color.FromArgb(125,255,255,255),2))g.DrawLine(edge,45,29,951,29);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Width<2||Height<2)return;
            if(face==null){face=new Bitmap(Width,Height);using(Graphics f=Graphics.FromImage(face)){f.Clear(BackColor);DrawFace(f);}}
            Graphics g=e.Graphics;g.DrawImageUnscaled(face,0,0);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Width/1000f,Height/450f);
            PointF end=Oval(457,331,AngleFor(current));
            using(Pen shade=new Pen(Color.FromArgb(90,52,37,24),8))using(Pen hand=new Pen(Color.FromArgb(235,66,33),5.5f))
            using(Pen shine=new Pen(Color.FromArgb(255,150,126),1.4f))
            {g.DrawLine(shade,502,442,end.X+3,end.Y+3);g.DrawLine(hand,500,440,end.X,end.Y);g.DrawLine(shine,499,439,end.X-1,end.Y-1);}
            if(glass==null){glass=new Bitmap(Width,Height);using(Graphics light=Graphics.FromImage(glass)){light.Clear(Color.Transparent);DrawGlass(light);}}
            GraphicsState transform=g.Save();g.ResetTransform();g.DrawImageUnscaled(glass,0,0);g.Restore(transform);
            Rectangle cover=new Rectangle(354,385,292,149);
            using(LinearGradientBrush black=new LinearGradientBrush(cover,Color.FromArgb(61,59,54),Color.FromArgb(10,10,9),90))
            using(Pen rim=new Pen(Color.FromArgb(143,139,127),3)){g.FillEllipse(black,cover);g.DrawEllipse(rim,cover);}
            StudioHardware.Screw(g,new RectangleF(472,394,56,56),false);
        }
    }

    internal sealed class StudioKnob : Control
    {
        private int value = 7;
        private bool moving;
        public event EventHandler ValueChanged;
        public int Value
        {
            get { return value; }
            set
            {
                int v = Math.Max(0, Math.Min(10, value));
                if (v == this.value) return;
                this.value = v; Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }
        public StudioKnob()
        {
            DoubleBuffered = true; ResizeRedraw = true; TabStop = false;
            SetStyle(ControlStyles.SupportsTransparentBackColor,true); BackColor = Color.Transparent;
        }
        private void UserValue(int next){int old=value;Value=next;if(value!=old)UiSounds.Tick();}
        private void SelectAt(Point p)
        {
            float cx = Width / 2f, cy = Height * 204f / 370f;
            double a = Math.Atan2(cy - p.Y, p.X - cx) * 180 / Math.PI;
            if (a < -30 && a > -120) a = -30;
            else if (a <= -120) a += 360;
            a = Math.Max(-30, Math.Min(210, a));
            UserValue((int)Math.Round((210 - a) / 24.0));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); moving = true; Capture = true; SelectAt(e.Location); } }
        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); if (moving) SelectAt(e.Location); }
        protected override void OnMouseUp(MouseEventArgs e)
        { base.OnMouseUp(e); moving = false; Capture = false; }
        protected override void OnMouseWheel(MouseEventArgs e)
        { UserValue(Value+Math.Sign(e.Delta)); base.OnMouseWheel(e); }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || key==Keys.Home || key==Keys.End || base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { UserValue(Value-1); e.Handled = true; }
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { UserValue(Value+1); e.Handled = true; }
            if (e.KeyCode == Keys.Home) { UserValue(0); e.Handled = true; }
            if (e.KeyCode == Keys.End) { UserValue(10); e.Handled = true; }
            base.OnKeyDown(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Width/400f,Height/370f);
            const float cx=200,cy=204,r=130;
            using(Font f=new Font("Arial",25,FontStyle.Regular,GraphicsUnit.Pixel))using(Brush ink=new SolidBrush(StudioPalette.Ink))using(Pen tick=new Pen(Color.FromArgb(47,43,31),1.8f))
            for(int i=0;i<=10;i++){double a=210-24*i;g.DrawLine(tick,StudioPalette.Polar(cx,cy,r+13,a),StudioPalette.Polar(cx,cy,r+25,a));StudioPalette.CenterString(g,i.ToString(),f,ink,StudioPalette.Polar(cx,cy,r+47,a));}
            using(Brush shadow=new SolidBrush(Color.FromArgb(100,0,0,0)))g.FillEllipse(shadow,cx-r-3,cy-r+5,2*r+6,2*r+6);
            RectangleF washer=new RectangleF(cx-r-3,cy-r-3,2*r+6,2*r+6);
            using(LinearGradientBrush steel=new LinearGradientBrush(washer,Color.FromArgb(203,198,177),Color.FromArgb(72,70,60),50))using(Pen edge=new Pen(Color.FromArgb(49,45,34),2)){g.FillEllipse(steel,washer);g.DrawEllipse(edge,washer);}
            PointF[] grip=new PointF[128];for(int i=0;i<grip.Length;i++)grip[i]=StudioPalette.Polar(cx,cy,r*(i%4<2?1:.965f),i*360.0/grip.Length);
            using(GraphicsPath path=new GraphicsPath())
            {path.AddPolygon(grip);using(PathGradientBrush rubber=new PathGradientBrush(path)){rubber.CenterColor=Color.FromArgb(89,85,71);rubber.SurroundColors=new Color[]{Color.FromArgb(16,15,12)};g.FillPath(rubber,path);}}
            using(Pen ridge=new Pen(Color.FromArgb(84,80,65),1.4f))for(int j=0;j<64;j++){double a=j*360.0/64;g.DrawLine(ridge,StudioPalette.Polar(cx,cy,r-15,a),StudioPalette.Polar(cx,cy,r-3,a));}
            RectangleF face=new RectangleF(cx-r+18,cy-r+18,2*r-36,2*r-36);
            using(LinearGradientBrush cap=new LinearGradientBrush(face,Color.FromArgb(68,65,53),Color.FromArgb(24,22,17),95))using(Pen rim=new Pen(Color.FromArgb(92,85,65),2)){g.FillEllipse(cap,face);g.DrawEllipse(rim,face);}
            using(Pen gloss=new Pen(Color.FromArgb(110,167,159,132),3))g.DrawArc(gloss,cx-r+23,cy-r+23,2*r-46,2*r-46,205,105);
            using(Pen marker=new Pen(Color.FromArgb(238,225,185),8)){marker.StartCap=marker.EndCap=LineCap.Round;g.DrawLine(marker,StudioPalette.Polar(cx,cy,r*.46f,210-24*value),StudioPalette.Polar(cx,cy,r*.82f,210-24*value));}
            RectangleF hub=new RectangleF(cx-17,cy-17,34,34);using(LinearGradientBrush pin=new LinearGradientBrush(hub,Color.FromArgb(175,169,147),Color.FromArgb(75,71,57),50))using(Pen edge=new Pen(Color.FromArgb(21,19,13),2)){g.FillEllipse(pin,hub);g.DrawEllipse(edge,hub);}
        }
    }

    internal sealed class StudioFileIcon : Control
    {
        public StudioFileIcon(){DoubleBuffered=true;ResizeRedraw=true;}
        protected override void OnPaintBackground(PaintEventArgs e){StudioHardware.ParentBackground(this,e.Graphics);}
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Width/84f,Height/85f);
            Rectangle r=new Rectangle(2,3,79,79);using(GraphicsPath path=StudioPalette.RoundRect(r,6))using(LinearGradientBrush metal=new LinearGradientBrush(r,Color.FromArgb(191,184,157),Color.FromArgb(95,90,69),50))using(Pen edge=new Pen(Color.FromArgb(58,51,31),2)){g.FillPath(metal,path);g.DrawPath(edge,path);}
            using(Brush panel=new SolidBrush(Color.FromArgb(42,37,25)))g.FillRectangle(panel,10,17,64,49);
            foreach(float x in new float[]{27,57})
            {using(Brush rim=new SolidBrush(Color.FromArgb(189,177,136)))g.FillEllipse(rim,x-12,27,24,24);using(Brush hole=new SolidBrush(Color.FromArgb(42,37,25))){g.FillEllipse(hole,x-4,35,8,8);for(int j=0;j<3;j++){PointF p=StudioPalette.Polar(x,39,8,j*120);g.FillEllipse(hole,p.X-2,p.Y-2,4,4);}}}
            using(Pen tape=new Pen(Color.FromArgb(203,185,130),2))g.DrawLine(tape,25,55,59,55);
        }
    }
    internal sealed class StudioSurface : Panel
    {
        internal bool BorderVisible=true;
        public StudioSurface(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(247,246,244);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);PaintSurface(e.Graphics);
        }
        protected override void OnPaintBackground(PaintEventArgs e){StudioHardware.ParentBackground(this,e.Graphics);}
        internal void PaintSurface(Graphics graphics)
        {
            graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Rectangle r=new Rectangle(1,1,Width-3,Height-3);if(r.Width<2||r.Height<2)return;
            if(!BorderVisible){using(LinearGradientBrush body=new LinearGradientBrush(ClientRectangle,Color.FromArgb(245,244,239),Color.FromArgb(220,218,211),90))graphics.FillRectangle(body,ClientRectangle);return;}
            using(GraphicsPath path=StudioPalette.RoundRect(r,11))using(LinearGradientBrush fill=new LinearGradientBrush(r,Color.FromArgb(247,245,236),Color.FromArgb(225,222,210),90))
            using(Pen edge=new Pen(Color.FromArgb(194,191,185),1)){graphics.FillPath(fill,path);if(BorderVisible)graphics.DrawPath(edge,path);}
        }
    }
}
