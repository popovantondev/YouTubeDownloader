using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace PeakMp3Win7
{
 // One per-pixel-alpha top-level window: a Region cannot represent smooth edge coverage.
 internal sealed class LayeredSkin : IDisposable
 {
  [StructLayout(LayoutKind.Sequential)]private struct XY{internal int X,Y;internal XY(int x,int y){X=x;Y=y;}}
  [StructLayout(LayoutKind.Sequential,Pack=1)]private struct Blend{internal byte Op,Flags,Alpha,Format;}
  [DllImport("user32.dll",SetLastError=true)]private static extern bool UpdateLayeredWindow(IntPtr w,IntPtr screen,ref XY at,ref XY size,IntPtr source,ref XY from,int key,ref Blend blend,int flags);
  [DllImport("user32.dll")]private static extern IntPtr GetDC(IntPtr h);
  [DllImport("user32.dll")]private static extern int ReleaseDC(IntPtr h,IntPtr dc);
  [DllImport("gdi32.dll")]private static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]private static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")]private static extern bool DeleteDC(IntPtr dc);
  private Bitmap frame;private int[] edgeOffsets;private byte[] edgeAlpha;private Size size;
  internal static GraphicsPath Outline(Rectangle r)
  {float x=r.X,y=r.Y,w=r.Width,h=r.Height,t=32*w/973f,b=38*w/973f;GraphicsPath p=new GraphicsPath();p.AddArc(x,y,t*2,t*2,180,90);p.AddArc(x+w-t*2,y,t*2,t*2,270,90);p.AddArc(x+w-b*2,y+h-b*2,b*2,b*2,0,90);p.AddArc(x,y+h-b*2,b*2,b*2,90,90);p.CloseFigure();return p;}
  private void Prepare(Size next,Rectangle art)
  {
   if(frame!=null)frame.Dispose();size=next;frame=new Bitmap(next.Width,next.Height,PixelFormat.Format32bppPArgb);
   System.Collections.Generic.List<int> offsets=new System.Collections.Generic.List<int>();System.Collections.Generic.List<byte> values=new System.Collections.Generic.List<byte>();
   using(Bitmap mask=new Bitmap(next.Width,next.Height,PixelFormat.Format32bppArgb))using(Graphics g=Graphics.FromImage(mask))using(GraphicsPath p=Outline(art))
   {g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.FillPath(Brushes.White,p);
    for(int y=0;y<next.Height;y++)for(int x=0;x<next.Width;x++){byte a=mask.GetPixel(x,y).A;if(a<255){offsets.Add((y*next.Width+x)*4);values.Add(a);}}}
   edgeOffsets=offsets.ToArray();edgeAlpha=values.ToArray();
  }
  internal Bitmap Render(VintageSurface surface)
  {
   if(frame==null||size!=surface.ClientSize)Prepare(surface.ClientSize,surface.ArtBounds);
   surface.DrawToBitmap(frame,new Rectangle(Point.Empty,size));BitmapData data=frame.LockBits(new Rectangle(Point.Empty,size),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);
   try{for(int n=0;n<edgeOffsets.Length;n++){int index=edgeOffsets[n],a=edgeAlpha[n];int y=index/(size.Width*4),x=index%(size.Width*4);IntPtr pixel=new IntPtr(data.Scan0.ToInt64()+y*data.Stride+x);for(int c=0;c<4;c++)Marshal.WriteByte(pixel,c,(byte)(Marshal.ReadByte(pixel,c)*a/255));}}finally{frame.UnlockBits(data);}return frame;
  }
  internal void Present(Form form,VintageSurface surface)
  {
   if(!form.Visible||form.WindowState==FormWindowState.Minimized||surface.Width<4||surface.Height<4)return;
   Bitmap bitmap=Render(surface);IntPtr screen=GetDC(IntPtr.Zero),memory=CreateCompatibleDC(screen),dib=IntPtr.Zero,old=IntPtr.Zero;
   try{dib=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,dib);XY at=new XY(form.Left,form.Top),dimensions=new XY(bitmap.Width,bitmap.Height),origin=new XY(0,0);Blend blend=new Blend();blend.Alpha=255;blend.Format=1;if(!UpdateLayeredWindow(form.Handle,screen,ref at,ref dimensions,memory,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
   finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(dib!=IntPtr.Zero)DeleteObject(dib);DeleteDC(memory);ReleaseDC(IntPtr.Zero,screen);}
  }
  public void Dispose(){if(frame!=null){frame.Dispose();frame=null;}}
 }
 internal sealed partial class ApprovedWindow
 {
  private readonly LayeredSkin skin=new LayeredSkin();
  protected override CreateParams CreateParams{get{CreateParams p=base.CreateParams;p.ExStyle|=0x80000;return p;}}
  protected override void OnShown(EventArgs e){base.OnShown(e);RefreshVintage();}
 }
}
