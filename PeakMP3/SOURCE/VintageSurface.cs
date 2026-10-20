// Bitmap studio skin over the existing processing and native-reference VU state.
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;

namespace PeakMp3Win7
{
    internal sealed class VintageState
    {
        internal int Language,Density,Volume=100,Progress,QueueCount,QueueIndex,JobIndex,JobCount,Transport;
        internal bool HasFile,Playing,Paused,Busy,Processed,Requested;
        internal string FileName="",Metadata="",Status="",Headline="",AutoInfo="",TrackKey="";
        internal double Position,Duration,Vu=-20;
        internal float[] Wave;internal RecordName Label=new RecordName("","PeakMP3");
    }
    internal sealed class VintageSurface : Control
    {
        internal event Action<string,double> Command;
        private readonly Bitmap background,arm,knob,armBase,spindle,abFrame,abNormal,abPressed,faderNormal,faderPressed;
        internal readonly TurntableMotion Mechanics=new TurntableMotion();
        private readonly Bitmap labelPaper;private Bitmap labelBitmap;private string labelKey="";

        private readonly Bitmap hub;
        private Bitmap displayArm,displayHub,displayPlate;
        private Size displaySize;
        private readonly Bitmap[] buttons=new Bitmap[3];
        private Bitmap cache;
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly Bitmap needleLayer=new Bitmap(1682,554,PixelFormat.Format32bppPArgb);
        private double lastNeedleDb=double.NaN;private bool labelTip;
        private double lastTime,rotation;
        private int recordFrame;
        private int tileVolume=-1;
        private string cacheKey,pressed="",drag="",hover="";
        private double seekCandidate;
        private bool dirty=true;
        internal VintageState State=new VintageState();
        internal int CurrentRecordFrame{get{return recordFrame;}}
        internal double Rotation{get{return rotation;}}
        internal static readonly Rectangle RecordBounds=new Rectangle(175,1060,595,435);
        internal static readonly Rectangle ArmBounds=new Rectangle(559,1086,378,372);
        private readonly ToolTip tips=new ToolTip();
        internal VintageSurface(string directory)
        {
            background=Load(directory,"turntable_plate.png");
            armBase=Load(directory,"arm-base.png");spindle=Load(directory,"spindle.png");abFrame=Load(directory,"ab-frame.png");abNormal=Load(directory,"ab-normal.png");abPressed=Load(directory,"ab-pressed.png");faderNormal=Load(directory,"fader-normal.png");faderPressed=Load(directory,"fader-pressed.png");
            labelPaper=Load(directory,"label-paper.png");
            arm=Load(directory,"arm-moving.png");knob=Load(directory,"knob_states.png");
            hub=new Bitmap(255,66,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(hub))using(GraphicsPath cover=new GraphicsPath())
            {g.TranslateTransform(-355,-399);cover.AddBezier(355,465,415,377,550,377,610,465);cover.CloseFigure();g.SetClip(cover);g.DrawImageUnscaled(background,0,0);}
            for(int i=0;i<3;i++)buttons[i]=Load(directory,"button"+i+".png");
            DoubleBuffered=true;ResizeRedraw=true;TabStop=false;AllowDrop=true;
            BackColor=Color.FromArgb(33,29,23);Cursor=Cursors.Default;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);
        }
        private static Bitmap Load(string directory,string name)
        {using(Image im=Image.FromFile(Path.Combine(directory,name))){Bitmap b=new Bitmap(im.Width,im.Height,PixelFormat.Format32bppPArgb);using(Graphics g=Graphics.FromImage(b))g.DrawImageUnscaled(im,0,0);return b;}}
        internal Rectangle ArtBounds
        {
            get{double scale=Math.Min(Width/973.0,Height/1616.0);int w=Math.Max(1,(int)Math.Round(973*scale)),h=Math.Max(1,(int)Math.Round(1616*scale));return new Rectangle((Width-w)/2,(Height-h)/2,w,h);}
        }
        internal PointF ToArt(Point point)
        {Rectangle r=ArtBounds;return new PointF((point.X-r.X)*973f/r.Width,(point.Y-r.Y)*1616f/r.Height);}
        internal Point FromArt(float x,float y)
        {Rectangle r=ArtBounds;return new Point((int)Math.Round(r.X+x*r.Width/973),(int)Math.Round(r.Y+y*r.Height/1616));}
        private PointF ScreenPoint(float x,float y)
        {Rectangle r=ArtBounds;return new PointF(r.X+x*r.Width/973f,r.Y+y*r.Height/1616f);}
        private Rectangle View(Rectangle r)
        {Point a=FromArt(r.X,r.Y),b=FromArt(r.Right,r.Bottom);return Rectangle.FromLTRB(a.X,a.Y,b.X,b.Y);}
        internal void Bind(VintageState next)
        {
            double now=clock.Elapsed.TotalSeconds,dt=Math.Max(0,Math.Min(.25,now-lastTime));lastTime=now;
            Mechanics.Advance(dt,next.Requested,next.Duration>0?next.Position/next.Duration:0,next.TrackKey);rotation=Mechanics.Rotation;
            int frame=(int)(rotation/360*96)%96;
            double oldVu=State.Vu,oldPosition=State.Position;int oldProgress=State.Progress;State=next;
            if(tileVolume!=next.Volume){tileVolume=next.Volume;Invalidate(View(new Rectangle(820,1250,130,260)));}
            string key=next.Language+"|"+next.Density+"|"+next.FileName+"|"+next.Metadata+"|"+next.Status+"|"+next.HasFile+"|"+next.Playing+"|"+next.Paused+"|"+next.Busy+"|"+next.Processed+"|"+next.Headline+"|"+next.AutoInfo+"|"+next.Transport+"|"+next.QueueIndex+"|"+next.Label.Full+"|"+next.QueueCount+"|"+(next.Wave==null?0:next.Wave.GetHashCode());
            if(labelKey!=next.Label.Full){labelKey=next.Label.Full;if(labelBitmap!=null)labelBitmap.Dispose();labelBitmap=RecordLabel.Create(labelPaper,next.Label);if(labelTip)tips.SetToolTip(this,next.Label.Full);}
            if(key!=cacheKey){cacheKey=key;InvalidateCache();}
            else if(Math.Abs(oldVu-next.Vu)>.0001)Invalidate(View(new Rectangle(65,190,841,273)));
            if(Math.Abs(oldPosition-next.Position)>.015||oldProgress!=next.Progress)Invalidate(View(new Rectangle(55,954,863,99)));
            if(frame!=recordFrame||Mechanics.Animating){recordFrame=frame;Invalidate(View(new Rectangle(160,1000,813,505)));}
        }
        internal void InvalidateCache(){dirty=true;Invalidate();}
        protected override bool ShowFocusCues{get{return false;}}
        private static bool In(PointF p,int x,int y,int w,int h){return p.X>=x&&p.X<=x+w&&p.Y>=y&&p.Y<=y+h;}
        internal static string Hit(PointF p)
        {
            if(In(p,438,18,80,65))return "ru";if(In(p,527,18,80,65))return "de";if(In(p,614,18,80,65))return "en";
            if(In(p,786,12,79,70))return "minimize";if(In(p,872,12,79,70))return "close";
            if(In(p,456,411,49,47))return "calibrate";
            double dx=p.X-481,dy=p.Y-663;if(dx*dx+dy*dy<140*140)return "density";
            if(In(p,49,528,196,104))return "soft";if(In(p,49,667,196,104))return "balance";
            if(In(p,726,528,196,104))return "loud";if(In(p,726,667,196,104))return "max";
            if(In(p,55,855,870,95))return "file";
            
            if(In(p,55,954,863,99))return "seek";
            if(In(p,60,1088,112,108))return "play";if(In(p,69,1203,87,86))return "stop";
            if(In(p,41,1301,72,74))return "previous";if(In(p,114,1301,72,74))return "next";
            if(In(p,77,1413,143,77))return "compare";
            if(In(p,347,1496,280,62))return "export";
            if(In(p,837,1258,89,238))return "volume";
            double radius=RecordRadius(p);if(radius<=1.04)return "recordSeek";
            if(In(p,0,0,973,91))return "drag";
            return "";
        }
        internal static double RecordRadius(PointF p)
        {double c=-.0860215054,a=1435*(1+c)-1233.5,v=(1233.5-p.Y)/(p.Y*c-a),u=(p.X-471.5)*(c*v+1)/261.0;return Math.Sqrt(u*u+v*v);}
        internal static double RecordPosition(PointF p){return Math.Max(0,Math.Min(1,(1-RecordRadius(p))/(1-.35)));}
        internal static int DensityAt(PointF p)
        {double a=Math.Atan2(p.Y-663,p.X-481)*180/Math.PI;if(a<0)a+=360;if(a>30&&a<150)return a<90?10:0;if(a<150)a+=360;return Math.Max(0,Math.Min(10,(int)Math.Round((a-150)/24)));}
        private void Emit(string name,double value){if(Command!=null)Command(name,value);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);PointF p=ToArt(e.Location);string hit=Hit(p);
            if(e.Button==MouseButtons.Right){if(hit=="file"||hit=="add")Emit("menu",0);return;}
            if(e.Button!=MouseButtons.Left)return;
            Focus();if((hit=="seek"||hit=="recordSeek")&&State.Busy)return;pressed=hit;
            if(hit=="drag"){Emit("drag",0);pressed="";return;}
            if(hit=="density"||hit=="volume"||hit=="seek"){drag=hit;Capture=true;Continuous(p);}
            InvalidateCache();
        }
        private void Continuous(PointF p)
        {
            if(drag=="density"&&((p.X-481)*(p.X-481)+(p.Y-663)*(p.Y-663)>2500))Emit("density",DensityAt(p));
            if(drag=="volume")Emit("volume",Math.Max(0,Math.Min(100,100*(1443-p.Y)/119)));
            if(drag=="seek"){seekCandidate=Math.Max(0,Math.Min(1,(p.X-77)/820));Invalidate(View(new Rectangle(55,954,863,99)));}
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {base.OnMouseMove(e);PointF p=ToArt(e.Location);string hit=Hit(p);bool onLabel=State.HasFile&&RecordRadius(p)<.35;if(onLabel!=labelTip){labelTip=onLabel;tips.SetToolTip(this,onLabel?State.Label.Full:"");}if(hover!=hit){string old=hover;hover=hit;if(old=="export"||hit=="export")InvalidateCache();}if(drag!="")Continuous(p);else Cursor=hit==""||hit=="drag"||hit=="seek"&&State.Busy?Cursors.Default:Cursors.Hand;}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);labelTip=false;tips.SetToolTip(this,"");if(hover!=""){hover="";InvalidateCache();}}
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;
            PointF p=ToArt(e.Location);string hit=Hit(p),was=pressed;
            bool seeking=drag=="seek";drag="";Capture=false;pressed="";
            if(seeking)Emit("seek",seekCandidate);
            else if(hit==was&&hit=="recordSeek"&&!State.Busy)Emit("recordSeek",RecordPosition(p));
            else if(hit==was&&hit!="density"&&hit!="volume"&&hit!="")
            {UiSounds.Button();Emit(hit,hit=="compare"?-1:0);}
            InvalidateCache();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(e.Button==MouseButtons.Left&&Hit(ToArt(e.Location))=="density"){drag=pressed="";Capture=false;Emit("densityInput",0);}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){pressed="";drag="";InvalidateCache();}}
        protected override void OnMouseWheel(MouseEventArgs e)
        {base.OnMouseWheel(e);if(Hit(ToArt(PointToClient(System.Windows.Forms.Cursor.Position)))=="volume")Emit("volume",State.Volume+Math.Sign(e.Delta)*5);else Emit("density",(double)State.Density+Math.Sign(e.Delta));}
        protected override bool IsInputKey(Keys keys){Keys k=keys&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||k==Keys.Home||k==Keys.End||base.IsInputKey(keys);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.Control&&e.KeyCode==Keys.O)Emit("add",0);
            else if(e.KeyCode==Keys.Space)Emit("play",0);
            else if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Emit("density",(double)State.Density-1);
            else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Emit("density",(double)State.Density+1);
            else if(e.KeyCode==Keys.Home)Emit("density",0);else if(e.KeyCode==Keys.End)Emit("density",10);else return;
            e.Handled=e.SuppressKeyPress=true;
        }
        private string T(string key){return L10n.Get(key,State.Language);}
        private static Font FontAt(float size,bool bold){return new Font("Tahoma",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
        private static void DrawText(Graphics g,string s,RectangleF box,float size,bool bold,Color ink,bool centered)
        {
            using(Font f=FontAt(size,bold))using(Brush b=new SolidBrush(ink))using(StringFormat format=new StringFormat())
            {format.LineAlignment=StringAlignment.Center;format.Alignment=centered?StringAlignment.Center:StringAlignment.Near;format.Trimming=StringTrimming.EllipsisCharacter;format.FormatFlags=StringFormatFlags.NoWrap;g.DrawString(s,f,b,box,format);}
        }
        private void Preset(Graphics g,string name,Rectangle bounds,int value,string caption,int bars)
        {
            int state=State.Density==value?2:pressed==name?1:0;g.DrawImage(buttons[state],bounds);
            Color ink=state==2?Color.FromArgb(111,43,8):Color.FromArgb(30,29,25);
            DrawText(g,caption,new RectangleF(bounds.X+8,bounds.Y+49,bounds.Width-16,41),27,false,Color.FromArgb(27,25,21),true);
            using(Pen p=new Pen(ink,5)){p.StartCap=p.EndCap=LineCap.Round;
                if(name=="soft"){PointF[] wave=new PointF[31];for(int i=0;i<wave.Length;i++)wave[i]=new PointF(bounds.X+62+i*2.3f,bounds.Y+32-(float)Math.Sin(i*Math.PI/15)*7);g.DrawCurve(p,wave);}
                else if(name=="balance"){g.DrawLine(p,bounds.X+70,bounds.Y+32,bounds.X+87,bounds.Y+32);g.DrawLine(p,bounds.X+97,bounds.Y+23,bounds.X+97,bounds.Y+42);g.DrawLine(p,bounds.X+107,bounds.Y+32,bounds.X+125,bounds.Y+32);}
                else for(int i=0;i<bars;i++){float x=bounds.X+bounds.Width/2f+(i-(bars-1)/2f)*14;g.DrawLine(p,x,bounds.Y+42,x,bounds.Y+36-i*3.3f);}
            }
        }
        private void BuildCache()
        {
            Rectangle fit=ArtBounds;
            if(cache==null||cache.Width!=fit.Width||cache.Height!=fit.Height){if(cache!=null)cache.Dispose();cache=new Bitmap(fit.Width,fit.Height,PixelFormat.Format32bppPArgb);}
            using(Graphics g=Graphics.FromImage(cache))
            {
                g.Clear(BackColor);g.DrawImageUnscaled(displayPlate,0,0);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.ScaleTransform(fit.Width/973f,fit.Height/1616f);
                int[] headerX={439,529,616};Rectangle language=new Rectangle(headerX[Math.Max(0,Math.Min(2,State.Language))],23,78,58);
                using(ImageAttributes tint=new ImageAttributes())
                {tint.SetColorMatrix(new ColorMatrix(new float[][]{new float[]{1.12f,0,0,0,0},new float[]{0,.91f,0,0,0},new float[]{0,0,.55f,0,0},new float[]{0,0,0,1,0},new float[]{0,0,0,0,1}}));g.DrawImage(background,language,language.X,language.Y,language.Width,language.Height,GraphicsUnit.Pixel,tint);}
                using(Pen glow=new Pen(Color.FromArgb(225,249,182,71),2))using(GraphicsPath border=StudioPalette.RoundRect(Rectangle.Inflate(language,-8,-8),6))g.DrawPath(glow,border);
                Preset(g,"soft",new Rectangle(50,529,194,102),2,T("presetSoft"),0);
                Preset(g,"balance",new Rectangle(50,667,194,102),5,T("presetBalance"),0);
                Preset(g,"loud",new Rectangle(726,529,194,102),8,T("presetLoud"),4);
                Preset(g,"max",new Rectangle(726,667,194,102),10,T("presetMax"),6);
                DrawDensityKnob(g,State.Density);
                DrawText(g,T("strength")+" · "+State.Density,new RectangleF(280,799,403,42),32,true,Color.FromArgb(22,23,22),true);
                string name=State.HasFile?(State.Busy?State.Status:State.Headline!=""?State.Headline:State.FileName):T("dropQueue");
                DrawText(g,name,new RectangleF(176,859,726,48),28,true,Color.FromArgb(24,24,21),false);
                string meta=State.HasFile?State.Metadata:"MP3 / AAC / WAV / FLAC";
                if(State.AutoInfo!=""&&!State.Busy)meta=State.AutoInfo+" · "+meta;
                DrawText(g,meta,new RectangleF(177,910,726,34),22,false,Color.FromArgb(116,109,94),false);
                DrawExport(g);
                if(State.QueueCount>0)DrawText(g,string.Format(T("queuePosition"),Math.Max(1,State.QueueIndex+1),State.QueueCount),new RectangleF(638,1509,218,44),25,false,Color.FromArgb(71,49,27),true);
                using(GraphicsPath outline=LayeredSkin.Outline(new Rectangle(2,2,969,1612)))using(Pen edge=new Pen(Color.FromArgb(135,116,94),1.4f))g.DrawPath(edge,outline);
                DrawTransport(g);
                DrawComparison(g);
                if(!State.HasFile)
                {using(Brush shade=new SolidBrush(Color.FromArgb(70,0,0,0))){g.FillEllipse(shade,63,1092,105,104);g.FillEllipse(shade,70,1207,85,81);g.FillRectangle(shade,357,1504,260,43);}}
                if(pressed=="play"||pressed=="stop"||pressed=="previous"||pressed=="next")
                {Rectangle cap=pressed=="play"?new Rectangle(74,1101,83,83):pressed=="stop"?new Rectangle(79,1212,66,66):pressed=="previous"?new Rectangle(49,1310,55,55):new Rectangle(122,1310,55,55);using(Brush shade=new SolidBrush(Color.FromArgb(85,0,0,0)))g.FillEllipse(shade,cap);}
            }
            dirty=false;
        }
        private static Bitmap Scaled(Bitmap image,Size size)
        {Bitmap b=new Bitmap(Math.Max(1,size.Width),Math.Max(1,size.Height),PixelFormat.Format32bppPArgb);using(Graphics g=Graphics.FromImage(b)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(image,new Rectangle(Point.Empty,b.Size));}return b;}
        private void EnsureDisplaySprites()
        {
            Size size=ArtBounds.Size;if(size==displaySize)return;displaySize=size;

            if(displayArm!=null)displayArm.Dispose();if(displayHub!=null)displayHub.Dispose();
            if(displayPlate!=null)displayPlate.Dispose();displayPlate=Scaled(background,size);
            displayArm=Scaled(arm,View(ArmBounds).Size);displayHub=Scaled(hub,View(new Rectangle(355,399,255,66)).Size);
        }
        private static string Time(double seconds){int value=(int)Math.Max(0,seconds);return value/60+":"+(value%60).ToString("00");}
        private void DrawWave(Graphics g)
        {
            double fraction=State.Busy?Math.Max(0,State.Progress)/100.0:drag=="seek"?seekCandidate:State.Duration>0?Math.Max(0,Math.Min(1,State.Position/State.Duration)):0;
            if(State.Wave!=null&&State.Wave.Length>0)
            {
                float max=.001f;foreach(float sample in State.Wave)max=Math.Max(max,sample);
                using(Pen played=new Pen(Color.FromArgb(203,119,47),3))using(Pen upcoming=new Pen(Color.FromArgb(145,136,117),3))
                {played.StartCap=played.EndCap=upcoming.StartCap=upcoming.EndCap=LineCap.Round;
                    for(int x=0;x<820;x+=5){int begin=x*State.Wave.Length/820,end=Math.Min(State.Wave.Length,(x+5)*State.Wave.Length/820+1);float peak=0;for(int i=begin;i<end;i++)peak=Math.Max(peak,State.Wave[i]);float h=Math.Max(1,peak/max*25);g.DrawLine(x<820*fraction?played:upcoming,77+x,988-h,77+x,988+h);}}
            }
            if(State.HasFile&&!State.Busy){float x=77+(float)(820*fraction);using(Pen cursor=new Pen(Color.FromArgb(227,126,33),2))g.DrawLine(cursor,x,962,x,1014);}
            if(State.Busy&&State.Wave==null)using(Pen progress=new Pen(Color.FromArgb(239,142,42),3))g.DrawLine(progress,77,1015,77+(float)(820*State.Progress/100.0),1015);
            if(State.Busy)
            {
                DrawText(g,State.Progress<0?"…":State.Progress+"%",new RectangleF(73,1015,130,34),24,false,Color.FromArgb(125,119,102),false);
                DrawText(g,Math.Max(1,State.JobIndex+1)+" / "+Math.Max(1,State.JobCount),new RectangleF(770,1015,140,34),24,false,Color.FromArgb(125,119,102),false);
            }
            else
            {
                DrawText(g,Time(drag=="seek"?seekCandidate*State.Duration:State.Position),new RectangleF(73,1015,120,34),24,false,Color.FromArgb(125,119,102),false);
                DrawText(g,Time(State.Duration),new RectangleF(813,1015,100,34),24,false,Color.FromArgb(125,119,102),false);
            }
        }
        private void DrawExport(Graphics g)
        {
            Rectangle r=new Rectangle(355,1502,264,49);bool enabled=State.HasFile||State.Busy;
            using(GraphicsPath shape=StudioPalette.RoundRect(r,8))
            {
                if(!enabled)using(Brush dim=new SolidBrush(Color.FromArgb(90,0,0,0)))g.FillPath(dim,shape);
                else
                {using(LinearGradientBrush glow=new LinearGradientBrush(r,Color.FromArgb(pressed=="export"?155:hover=="export"?140:78,255,199,76),Color.FromArgb(hover=="export"?88:48,238,107,16),90))g.FillPath(glow,shape);using(Pen edge=new Pen(Color.FromArgb(255,208,118),2))g.DrawPath(edge,shape);}
            }
            DrawFitted(g,State.Busy?T("cancel"):T("start"),new RectangleF(360,1504,254,43),27,enabled?Color.FromArgb(255,239,207):Color.FromArgb(160,151,131));
        }
        private static void DrawFitted(Graphics g,string text,RectangleF box,float maximum,Color color)
        {float size=maximum;for(;size>14;size-=.5f)using(Font f=FontAt(size,true)){if(g.MeasureString(text,f).Width<=box.Width-4)break;}DrawText(g,text,box,size,true,color,true);}
        private static void DrawTransportCap(Graphics g,float cx,float cy,float radius,bool selected)
        {
            RectangleF r=new RectangleF(cx-radius,cy-radius,2*radius,2*radius);
            using(Brush shadow=new SolidBrush(Color.FromArgb(100,0,0,0)))g.FillEllipse(shadow,r.X,r.Y+3,r.Width,r.Height);
            using(LinearGradientBrush gold=new LinearGradientBrush(r,Color.FromArgb(249,225,170),Color.FromArgb(83,48,17),85))g.FillEllipse(gold,r);
            using(Pen edge=new Pen(Color.FromArgb(45,31,18),2))g.DrawEllipse(edge,r);
            if(selected)using(Pen glow=new Pen(Color.FromArgb(255,164,26),4))g.DrawEllipse(glow,RectangleF.Inflate(r,-4,-4));
            RectangleF cap=RectangleF.Inflate(r,-8,-8);using(LinearGradientBrush face=new LinearGradientBrush(cap,Color.FromArgb(66,48,29),Color.FromArgb(12,11,9),90))g.FillEllipse(face,cap);
            using(Pen rim=new Pen(Color.FromArgb(166,118,54),1))g.DrawEllipse(rim,cap);
            using(Pen shine=new Pen(Color.FromArgb(75,255,243,211),1.2f))g.DrawArc(shine,RectangleF.Inflate(cap,-3,-3),205,110);
        }
        private void DrawTransport(Graphics g)
        {
            DrawTransportCap(g,115,1143,53,State.Requested);
            DrawTransportCap(g,115,1247,44,State.Transport==3);
            using(Brush ink=new SolidBrush(Color.FromArgb(251,243,220)))
            {
                g.FillRectangle(ink,101.5f,1233.5f,27,27);
                if(State.Requested){g.FillRectangle(ink,101.5f,1125.5f,9,35);g.FillRectangle(ink,119.5f,1125.5f,9,35);}
                else g.FillPolygon(ink,new PointF[]{new PointF(105,1125.5f),new PointF(105,1160.5f),new PointF(135,1143)});
            }
        }
        private static void DrawMetalCap(Graphics g,RectangleF r)
        {
            using(Brush shadow=new SolidBrush(Color.FromArgb(105,0,0,0)))g.FillEllipse(shadow,r.X,r.Y+2,r.Width,r.Height);
            for(int i=0;i<360;i++)
            {double a=(i-35)*Math.PI/180,shine=Math.Cos(a*2)*.62+Math.Cos(a*4)*.15;int v=(int)Math.Max(65,Math.Min(252,171+shine*104));using(Brush sector=new SolidBrush(Color.FromArgb(v,Math.Min(255,v+1),Math.Max(0,v-6))))g.FillPie(sector,r.X,r.Y,r.Width,r.Height,i,1.8f);}
            for(float inset=1;inset<r.Width/2;inset+=1.2f)using(Pen groove=new Pen(Color.FromArgb(22,255,255,247),.55f))g.DrawEllipse(groove,RectangleF.Inflate(r,-inset,-inset));
            using(Pen dark=new Pen(Color.FromArgb(61,58,48),1.2f))using(Pen bright=new Pen(Color.FromArgb(238,237,222),1))
            {g.DrawEllipse(dark,r);g.DrawArc(bright,RectangleF.Inflate(r,-1.4f,-1.4f),190,150);}
        }
        private void DrawDensityKnob(Graphics g,int density)
        {
            const float cx=481,cy=663;
            int frame=Math.Max(0,Math.Min(10,density));
            g.DrawImage(knob,new Rectangle(342,524,278,278),new Rectangle(frame*278,0,278,278),GraphicsUnit.Pixel);
            // Keep the approved photographic metal; recenter its face onto the same shaft as the grip.
            GraphicsState faceState=g.Save();using(GraphicsPath face=new GraphicsPath())
            {face.AddEllipse(cx-116,cy-116,232,232);g.SetClip(face,CombineMode.Intersect);g.DrawImage(knob,new RectangleF(cx-116,cy-116,232,232),new RectangleF(22.5f,21,232,232),GraphicsUnit.Pixel);}g.Restore(faceState);
            using(Pen faceRim=new Pen(Color.FromArgb(105,74,62,42),1))g.DrawEllipse(faceRim,cx-116,cy-116,232,232);
            double marker=(150+Math.Max(0,Math.Min(10,density))*24)*Math.PI/180;
            PointF inner=new PointF(cx+(float)Math.Cos(marker)*83,cy+(float)Math.Sin(marker)*83),outer=new PointF(cx+(float)Math.Cos(marker)*105,cy+(float)Math.Sin(marker)*105);
            using(Pen well=new Pen(Color.FromArgb(56,43,24),11))using(Pen line=new Pen(Color.FromArgb(241,207,127),6))
            {well.StartCap=well.EndCap=LineCap.Round;g.DrawLine(well,inner,outer);g.DrawLine(line,inner,outer);}
            DrawMetalCap(g,new RectangleF(cx-21,cy-21,42,42));
        }
        private void DrawComparison(Graphics g)
        {
            g.DrawImage(abFrame,new Rectangle(77,1413,143,58));
            RectangleF thumb=ComparisonThumb(State.Processed);if(pressed=="compare"){thumb.Offset(0,1);}DrawMetalCap(g,thumb);
            using(Brush dark=new SolidBrush(Color.FromArgb(75,51,25)))g.FillEllipse(dark,54,1467,14,14);
            using(Brush dark=new SolidBrush(Color.FromArgb(75,51,25)))g.FillEllipse(dark,224,1467,14,14);
            using(Brush light=new SolidBrush(Color.FromArgb(255,195,68)))using(Pen glow=new Pen(Color.FromArgb(255,130,18),3))
            {Rectangle r=new Rectangle(State.Processed?224:54,1467,14,14);g.FillEllipse(light,r);g.DrawEllipse(glow,r);}
        }
        internal static RectangleF ComparisonThumb(bool processed){return new RectangleF((processed?195:102)-29,1442-29,58,58);}
        internal static float FaderY(int volume){return 1443-Math.Max(0,Math.Min(100,volume))*119f/100;}
        internal static bool LampOn(int volume,int index){return volume>0&&1324+index*(119f/9)>=FaderY(volume)-.01f;}
        internal static RectangleF FaderThumb(int volume){return new RectangleF(883.5f-24,FaderY(volume)-39,48,78);}
        private void DrawVolume(Graphics g)
        {
            // The original always-on lamps have been removed from the plate.
            Rectangle lenses=new Rectangle(839,1280,23,197);
            using(LinearGradientBrush strip=new LinearGradientBrush(lenses,Color.FromArgb(128,110,80),Color.FromArgb(154,130,91),90))g.FillRectangle(strip,lenses);
            for(int i=0;i<10;i++)
            {
                float y=1324+i*(119f/9);RectangleF lens=new RectangleF(844,y-3,13,6);
                using(Brush dark=new SolidBrush(Color.FromArgb(48,34,19)))g.FillRectangle(dark,lens);
                if(LampOn(State.Volume,i))
                {using(Brush halo=new SolidBrush(Color.FromArgb(65,255,141,17)))g.FillRectangle(halo,841,y-5,19,10);using(Brush light=new SolidBrush(Color.FromArgb(255,206,82)))g.FillRectangle(light,845,y-2,11,4);}
            }
            g.DrawImage(pressed=="volume"?faderPressed:faderNormal,FaderThumb(State.Volume));
            DrawSpeaker(g);

        }
        private void DrawSpeaker(Graphics g)
        {
            float x=871,y=1511;
            using(Brush ink=new SolidBrush(Color.FromArgb(35,31,24)))g.FillPolygon(ink,new PointF[]{new PointF(x-4,y-7),new PointF(x+1,y-7),new PointF(x+9,y-13),new PointF(x+9,y+13),new PointF(x+1,y+7),new PointF(x-4,y+7)});
            using(Pen p=new Pen(Color.FromArgb(35,31,24),2.4f))
            {p.StartCap=p.EndCap=LineCap.Round;if(State.Volume==0){g.DrawLine(p,x+15,y-5,x+23,y+5);g.DrawLine(p,x+15,y+5,x+23,y-5);}else{int waves=State.Volume<34?1:State.Volume<67?2:3;for(int i=0;i<waves;i++){float radius=11+i*7;g.DrawArc(p,x+4-radius,y-radius,radius*2,radius*2,-49,98);}}}
        }
        // Artwork labels are irregularly spaced. Calibrate drawing to their exact ticks;
        // the input remains the unmodified native-reference level in dBFS.
        internal static PointF NeedleForDb(double db)
        {
            double[] labels={-20,-10,-7,-5,-3,-2,-1,0,1,2,3};
            PointF[] points={new PointF(155,310),new PointF(232,258),new PointF(307,234),new PointF(367,218),new PointF(447,210),new PointF(489,213),new PointF(543,219),new PointF(604,234),new PointF(663,260),new PointF(720,290),new PointF(783,322)};
            db=Math.Max(-20,Math.Min(3,db));int i=0;while(i<labels.Length-2&&db>labels[i+1])i++;
            double a=Math.Pow(10,db/20),lo=Math.Pow(10,labels[i]/20),hi=Math.Pow(10,labels[i+1]/20),f=(a-lo)/(hi-lo);
            return new PointF((float)(points[i].X+(points[i+1].X-points[i].X)*f),(float)(points[i].Y+(points[i+1].Y-points[i].Y)*f));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);if(Width<4||Height<4)return;Rectangle fit=ArtBounds;EnsureDisplaySprites();if(dirty||cache==null||cache.Size!=fit.Size)BuildCache();
            Graphics g=e.Graphics;g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(cache,fit.X,fit.Y);g.CompositingMode=CompositingMode.SourceOver;g.SmoothingMode=SmoothingMode.AntiAlias;
            GraphicsState waveTransform=g.Save();g.TranslateTransform(fit.X,fit.Y);g.ScaleTransform(fit.Width/973f,fit.Height/1616f);DrawWave(g);g.Restore(waveTransform);
            PointF end=NeedleForDb(State.Vu);
            float scale=fit.Width/973f;
            if(lastNeedleDb!=State.Vu)using(Graphics n=Graphics.FromImage(needleLayer))
            {n.Clear(Color.Transparent);n.SmoothingMode=SmoothingMode.HighQuality;n.ScaleTransform(2,2);n.TranslateTransform(-65,-190);
             using(Pen shadow=new Pen(Color.FromArgb(65,92,44,25),6))using(Pen needle=new Pen(Color.FromArgb(224,66,37),3.8f))using(Pen light=new Pen(Color.FromArgb(255,177,145),1))
             {shadow.StartCap=needle.StartCap=light.StartCap=LineCap.Round;n.DrawLine(shadow,new PointF(482,437),new PointF(end.X+1,end.Y+2));n.DrawLine(needle,new PointF(481,435),end);n.DrawLine(light,new PointF(480.7f,434.7f),new PointF(end.X-.3f,end.Y-.3f));}
             lastNeedleDb=State.Vu;}
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(needleLayer,View(new Rectangle(65,190,841,277)));
            Rectangle cover=View(new Rectangle(355,399,255,66));g.DrawImageUnscaled(displayHub,cover.Location);
            // Only the orange paper and its lettering rotate, never black vinyl, dots or light.
            if(labelBitmap==null)labelBitmap=RecordLabel.Create(labelPaper,State.Label);
            GraphicsState paperTransform=g.Save();PointF paperCenter=ScreenPoint(471.5f,1233.5f);g.TranslateTransform(paperCenter.X,paperCenter.Y);g.ScaleTransform(scale,scale*.735f);g.RotateTransform((float)rotation);g.DrawImage(labelBitmap,new RectangleF(-88.7f,-88.7f,177.4f,177.4f));g.Restore(paperTransform);
            g.DrawImage(spindle,View(new Rectangle(458,1211,28,36)));
            Rectangle pedestal=View(ArmBounds);g.DrawImage(armBase,pedestal);
            GraphicsState armTransform=g.Save();PointF pivot=ScreenPoint(839,1180);double radians=Mechanics.ArmAngle*Math.PI/180,tipY=1180-260*Math.Sin(radians)+254*Math.Cos(radians);float contactRock=(float)((tipY-1233.5)*Mechanics.Rock/240*(1-Mechanics.Lift));g.TranslateTransform(pivot.X,pivot.Y+(contactRock-(float)Mechanics.Lift*7)*scale);g.RotateTransform((float)Mechanics.ArmAngle);
            Rectangle armBox=View(ArmBounds);g.DrawImage(displayArm,new RectangleF(armBox.X-pivot.X,armBox.Y-pivot.Y,armBox.Width,armBox.Height));g.Restore(armTransform);
            GraphicsState volumeTransform=g.Save();g.TranslateTransform(fit.X,fit.Y);g.ScaleTransform(fit.Width/973f,fit.Height/1616f);DrawVolume(g);g.Restore(volumeTransform);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){if(cache!=null)cache.Dispose();needleLayer.Dispose();background.Dispose();labelPaper.Dispose();if(labelBitmap!=null)labelBitmap.Dispose();hub.Dispose();if(displayArm!=null)displayArm.Dispose();if(displayHub!=null)displayHub.Dispose();if(displayPlate!=null)displayPlate.Dispose();arm.Dispose();knob.Dispose();armBase.Dispose();spindle.Dispose();abFrame.Dispose();abNormal.Dispose();abPressed.Dispose();faderNormal.Dispose();faderPressed.Dispose();foreach(Bitmap b in buttons)b.Dispose();tips.Dispose();}
            base.Dispose(disposing);
        }
    }
}
