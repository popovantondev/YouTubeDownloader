// Lightweight streaming waveform thumbnail. No audio decoder libraries besides FFmpeg.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PeakMp3Win7
{
    internal delegate void SeekHandler(double seconds);

    internal sealed class WaveformView : Control
    {
        public event SeekHandler SeekRequested;
        private const int BinCount = 2048;
        private readonly object sync = new object();
        private float[] peaks;
        private string source;
        private double duration, position;
        private int revision;
        private Process running;
        private bool pending, dragging;
        private string error;
        public string LoadingText = "Generating waveform…";
        public string EmptyText = "Add an audio file";

        public WaveformView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(247, 243, 235);
            ForeColor = Color.FromArgb(246, 110, 25);
        }
        public bool IsScrubbing { get { return dragging; } }
        internal float[] PreviewPeaks{get{return peaks;}}
        internal void EnsurePreviewHandle(){if(!IsHandleCreated)CreateHandle();}
        public double PositionSeconds
        {
            get { return position; }
            set
            {
                if (!dragging && Math.Abs(value - position) >= 0.02) { position = value; Invalidate(); }
            }
        }
        public void Clear()
        {
            CancelLoad();
            duration = 0; source = null; position = 0; peaks = null; pending = false; error = null;
            Invalidate();
        }
        public void CancelLoad()
        {
            Interlocked.Increment(ref revision);
            lock (sync)
            {
                if (running != null)
                {
                    try { if (!running.HasExited) running.Kill(); } catch { }
                }
            }
        }
        public void LoadWaveform(string ffmpeg, string file, double total)
        {
            CancelLoad();
            duration = total; source = file; position = 0; peaks = null;
            error = null; pending = true; Invalidate();
            int ticket = revision;
            Thread t = new Thread(delegate() { DecodeWaveform(ffmpeg, file, total, ticket); });
            t.Name = "PeakMP3 waveform thumbnail";
            t.IsBackground = true;
            t.Start();
        }
        private static string Quote(string s) { return "\"" + s.Replace("\"", "") + "\""; }
        private void DecodeWaveform(string ffmpeg, string file, double total, int ticket)
        {
            Process p = null;
            string failure = null;
            float[] data = new float[BinCount];
            try
            {
                if (total <= 0) throw new InvalidOperationException("Unknown duration");
                p = new Process();
                p.StartInfo.FileName = ffmpeg;
                p.StartInfo.Arguments = "-hide_banner -nostdin -loglevel fatal -xerror -i " + Quote(file) +
                    " -map 0:a:0 -vn -sn -dn -ar 8000 -ac 2 -c:a pcm_s16le -f s16le pipe:1";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                lock (sync)
                {
                    if (ticket != revision) return;
                    p.Start(); running = p;
                }
                Stream stream = p.StandardOutput.BaseStream;
                byte[] bytes = new byte[32768];
                long sample = 0;
                double samplesPerBin = Math.Max(1.0, total * 8000.0 / BinCount);
                int got, carry=0;
                while (ticket == revision && (got = stream.Read(bytes, carry, bytes.Length-carry)) > 0)
                {
                    got += carry; carry=got%4; got-=carry;
                    for (int i = 0; i < got; i += 4)
                    {
                        int channelA = (short)(bytes[i] | (bytes[i + 1] << 8));
                        int channelB = (short)(bytes[i + 2] | (bytes[i + 3] << 8));
                        float level = (float)(Math.Max(Math.Abs((double)channelA), Math.Abs((double)channelB)) / 32768.0);
                        int index = (int)Math.Min(BinCount - 1, sample / samplesPerBin);
                        if (level > data[index]) data[index] = level;
                        ++sample;
                    }
                    if(carry>0)Array.Copy(bytes,got,bytes,0,carry);
                }
                if (ticket == revision)
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0) failure = "FFmpeg waveform exit: " + p.ExitCode;
                }
            }
            catch (Exception ex) { failure = ex.Message; }
            finally
            {
                lock (sync) { if (Object.ReferenceEquals(running, p)) running = null; }
                if (p != null) p.Close();
            }
            if (ticket == revision && IsHandleCreated && !IsDisposed)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (ticket != revision || IsDisposed) return;
                        peaks = failure == null ? data : null;
                        error = failure; pending = false; Invalidate();
                    });
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
        }
        private void SeekAt(int mouseX, bool notify)
        {
            if (duration <= 0) return;
            position = Math.Max(0, Math.Min(duration, (mouseX - 8.0) / Math.Max(1, Width - 16) * duration));
            Invalidate();
            if (notify && SeekRequested != null) SeekRequested(position);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { dragging = true; Capture = true; SeekAt(e.X, false); } }
        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); if (dragging) SeekAt(e.X, false); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (dragging) { dragging = false; SeekAt(e.X, true); Capture = false; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            StudioHardware.ParentBackground(this,g);
            using(GraphicsPath inset=StudioPalette.RoundRect(new Rectangle(0,0,Width-1,Height-1),7))
            using(LinearGradientBrush finish=new LinearGradientBrush(ClientRectangle,Color.FromArgb(244,239,224),Color.FromArgb(225,220,204),90))
            using(Pen edge=new Pen(Color.FromArgb(189,183,165),1))
            {g.FillPath(finish,inset);g.DrawPath(edge,inset);}
            int w = Width - 16, h = Height - 31, x0 = 8, y0 = 3;
            if (w < 1 || h < 1) return;
            int middle = y0 + h / 2;
            using (Pen center = new Pen(Color.FromArgb(218, 205, 188)))
                g.DrawLine(center, x0, middle, x0 + w, middle);
            if (peaks != null)
            {
                // Approved design: already-played waveform in warm orange,
                // upcoming waveform in muted silver. Scrubbing remains exact.
                float maximum=.001f;foreach(float amplitude in peaks)maximum=Math.Max(maximum,amplitude);
                int passed = duration > 0 ? (int)(w * Math.Max(0, Math.Min(duration, position)) / duration) : 0;
                using (Pen played = new Pen(ForeColor, 1f))
                using (Pen upcoming = new Pen(Color.FromArgb(172, 168, 164), 1f))
                {
                    for (int x = 0; x < w; x++)
                    {
                        int from = (int)((long)x * BinCount / w);
                        int to = Math.Min(BinCount, (int)((long)(x + 1) * BinCount / w) + 1);
                        float peak = 0;
                        for (int i = from; i < to; i++) if (peaks[i] > peak) peak = peaks[i];
                        int amp = (int)(peak / maximum * (h / 2 - 3));
                        if (amp < 1) amp = 1;
                        g.DrawLine(x <= passed ? played : upcoming, x0 + x, middle - amp, x0 + x, middle + amp);
                    }
                }
            }
            using (Pen ticks = new Pen(Color.FromArgb(176, 160, 140)))
            using (Font font = new Font("Tahoma", 8f))
            using (Brush brush = new SolidBrush(Color.FromArgb(115, 104, 92)))
            {
                int now=(int)Math.Max(0,position),end=(int)Math.Max(0,duration);
                string first=(now/60)+":"+(now%60).ToString("00"),last=(end/60)+":"+(end%60).ToString("00");
                g.DrawString(first,font,brush,x0,Height-18);
                SizeF lastSize=g.MeasureString(last,font);g.DrawString(last,font,brush,Width-x0-lastSize.Width,Height-18);
                if (pending || error != null || source == null)
                    g.DrawString(pending ? LoadingText : error != null ? error : EmptyText, font, brush, 18, 14);
            }
            if (duration > 0)
            {
                int playX = x0 + (int)(Math.Min(duration, Math.Max(0, position)) / duration * w);
                using (Pen line = new Pen(Color.FromArgb(247, 114, 24), 2f))
                    g.DrawLine(line, playX, y0, playX, y0 + h);
            }
        }
    }
}
