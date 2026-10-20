// PeakMP3 0.9: float WinMM preview and sample-clock aligned metering.
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PeakMp3Win7
{
    internal sealed class PreviewPlayer : IDisposable
    {
        private readonly string ffmpeg, file;
        private readonly bool normalized, dense;
        private int strength;
        private readonly object guard = new object();
        private Thread background;
        private Process process;
        private ProcessingEngine analyzer;
        private volatile bool stopping;
        private bool playing,ended;
        internal bool Ended {get{lock(guard)return ended;}}
        private volatile float outputVolume=1f;
        internal float Volume{get{return outputVolume;}set{outputVolume=Math.Max(0,Math.Min(1,value));}}
        private string filter, error;
        private ProcessResult measured;
        private readonly Action<ProcessResult> measuredReady;
        private readonly List<MeterFrame> meterFrames = new List<MeterFrame>();
        private long completedFrames, queuedFrames;
        private double lastClock;
        internal bool FloatOutput { get; private set; }
        private IntPtr soundDevice = IntPtr.Zero;
        private volatile bool paused;
        private double left = -35, right = -35, position, playFrom;

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WaveFormat
        {
            public short formatTag;
            public short channels;
            public int samplesPerSec;
            public int avgBytesPerSec;
            public short blockAlign;
            public short bitsPerSample;
            public short cbSize;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr lpData;
            public int dwBufferLength;
            public int dwBytesRecorded;
            public IntPtr dwUser;
            public int dwFlags;
            public int dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }
        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr handle, int deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, int flags);
        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr handle, IntPtr header, int bytes);
        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr handle, IntPtr header, int bytes);
        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr header, int bytes);
        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr handle);
        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr handle);
        [DllImport("winmm.dll")]
        private static extern int waveOutPause(IntPtr handle);
        [DllImport("winmm.dll")]
        private static extern int waveOutRestart(IntPtr handle);
        [StructLayout(LayoutKind.Sequential)]
        private struct MmTime { public int wType; public uint value; public uint reserved; }
        [DllImport("winmm.dll")]
        private static extern int waveOutGetPosition(IntPtr handle, ref MmTime position, uint cbmmt);

        private sealed class AudioSlot
        {
            public IntPtr Data;
            public IntPtr Header;
            public bool Queued;
            public int Length;
        }

        private void DrainSlot(IntPtr device, AudioSlot slot)
        {
            if (!slot.Queued) return;
            int size = Marshal.SizeOf(typeof(WaveHeader));
            while (!stopping)
            {
                WaveHeader info = (WaveHeader)Marshal.PtrToStructure(slot.Header, typeof(WaveHeader));
                if ((info.dwFlags & 1) != 0) break;
                Thread.Sleep(3);
            }
            lock(guard){if(stopping)waveOutReset(device);waveOutUnprepareHeader(device,slot.Header,size);}slot.Queued=false;
            if (!stopping) lock (guard) completedFrames += slot.Length;
        }
        public PreviewPlayer(string binary, string input, bool norm, bool isDense, int density)
            : this(binary, input, norm, isDense, density, null, null) { }
        internal PreviewPlayer(string binary, string input, bool norm, bool isDense, int density,
            ProcessResult knownAnalysis, Action<ProcessResult> onMeasured)
        {
            ffmpeg = binary; file = input; normalized = norm; dense = isDense; strength = density;
            measured = knownAnalysis; measuredReady = onMeasured;
            if (ProcessingEngine.FilterReady(measured,dense,strength)) filter = ProcessingEngine.FilterForOptions(measured, normalized, dense, strength);
        }
        private static string Quote(string s) { return "\"" + s.Replace("\"", "") + "\""; }
        public static double GetDuration(string ffmpegPath, string input)
        {
            if (!File.Exists(ffmpegPath)) return 0;
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = ffmpegPath;
                p.StartInfo.Arguments = "-hide_banner -nostdin -i " + Quote(input);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                string data = p.StandardError.ReadToEnd();
                p.WaitForExit();
                p.Close();
                Match m = Regex.Match(data, @"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);
                if (!m.Success) return 0;
                return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 3600 +
                    double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) * 60 +
                    double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
        public bool IsPlaying { get { lock (guard) return playing; } }
        public bool IsPaused { get { return paused; } }
        public void Pause()
        {
            paused = true;
            lock(guard)if(soundDevice!=IntPtr.Zero)waveOutPause(soundDevice);
        }
        public void Resume()
        {
            paused = false;
            lock(guard)if(soundDevice!=IntPtr.Zero)waveOutRestart(soundDevice);
        }
        public void SetDensity(int value)
        {

            strength = value;
            filter = !ProcessingEngine.FilterReady(measured,dense,value) ? null : ProcessingEngine.FilterForOptions(measured, normalized, dense, value);
        }
        public void Snapshot(out double dbLeft, out double dbRight, out double seconds, out bool active, out string err)
        {
            lock (guard)
            {
                double clock = completedFrames;
                if (playing && soundDevice != IntPtr.Zero)
                {
                    MmTime stamp = new MmTime(); stamp.wType = 2; // TIME_SAMPLES, not bytes or wall time
                    try
                    {
                        if (waveOutGetPosition(soundDevice, ref stamp, (uint)Marshal.SizeOf(typeof(MmTime))) == 0 && stamp.wType == 2)
                            clock = stamp.value;
                    }
                    catch { }
                }
                clock = Math.Max(lastClock, Math.Min(queuedFrames, clock)); lastClock = clock;
                seconds = playFrom + clock / 44100.0; position = seconds;
                while (meterFrames.Count > 1 && meterFrames[0].End <= clock) meterFrames.RemoveAt(0);
                if (meterFrames.Count > 0)
                {
                    float peakLeft=0,peakRight=0;
                    int remaining=ReferencePeakWindow.SampleCount;
                    long cursor=(long)clock;
                    foreach(MeterFrame f in meterFrames)
                    {
                        int offset=(int)Math.Max(0,cursor-f.Start);
                        int take=Math.Min(remaining,Math.Max(0,(int)(f.End-f.Start)-offset));
                        ReferencePeakWindow.Measure(f.Samples,offset,take,ref peakLeft,ref peakRight);
                        cursor+=take;remaining-=take;if(remaining==0)break;
                    }
                    left = MeterEnvelope.Db(peakLeft);
                    right = MeterEnvelope.Db(peakRight);
                }
                dbLeft = left; dbRight = right; active = playing; err = error;
            }
        }
        public void ClearError() { lock (guard) error = null; }
        public void Play(double startAt)
        {
            Stop();
            stopping = false;
            paused = false;
            lock (guard) { playing = true; ended=false; position = startAt; playFrom = startAt; error = null; left = -35; right = -35; meterFrames.Clear(); completedFrames = queuedFrames = 0; lastClock = 0; }
            background = new Thread(delegate() { StreamSound(startAt); });
            background.IsBackground = true;
            background.Name = "PeakMP3 WinMM preview";
            background.Start();
        }
        public void Stop()
        {
            stopping = true;
            lock(guard)if(soundDevice!=IntPtr.Zero){try{waveOutReset(soundDevice);}catch{}}
            try { if (analyzer != null) analyzer.Cancel(); } catch { }
            lock (guard)
            {
                try { if (process != null && !process.HasExited) process.Kill(); }
                catch { }
            }
            if (background != null && background != Thread.CurrentThread)
            {
                if (!background.Join(3000)) throw new InvalidOperationException("Preview worker did not stop");
                background = null;
            }
            lock (guard) { playing = false; left = -35; right = -35; }
            paused = false;
        }
        public void Dispose() { Stop(); }

        private void StreamSound(double startAt)
        {
            IntPtr device = IntPtr.Zero;
            AudioSlot[] slots = null;
            try
            {
                if (filter == null)
                {
                    analyzer = new ProcessingEngine(ffmpeg, new System.ComponentModel.BackgroundWorker(),
                        delegate(string key, string value) { });
                    if(measured==null)measured = analyzer.AnalyzeForPreview(file, normalized, dense);
                    if(dense)analyzer.PrepareDenseOutput(file,measured,strength);
                    if (measuredReady != null) measuredReady(measured);
                    if (stopping) return;
                    filter = ProcessingEngine.FilterForOptions(measured, normalized, dense, strength);
                    analyzer = null;
                }
                WaveFormat fmt = new WaveFormat();
                fmt.formatTag = 3; fmt.channels = 2; fmt.samplesPerSec = 44100;
                fmt.bitsPerSample = 32; fmt.blockAlign = 8; fmt.avgBytesPerSec = 352800;
                int result = waveOutOpen(out device, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
                FloatOutput = result == 0;
                if (result != 0) // old WinMM drivers may only accept PCM16
                {
                    fmt.formatTag = 1; fmt.bitsPerSample = 16; fmt.blockAlign = 4; fmt.avgBytesPerSec = 176400;
                    result = waveOutOpen(out device, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
                }
                if (result != 0) throw new InvalidOperationException("waveOutOpen WinMM: " + result);
                lock (guard) soundDevice = device;
                lock(guard)if(paused)waveOutPause(device);
                Process p = new Process();
                p.StartInfo.FileName = ffmpeg;
                p.StartInfo.Arguments = "-hide_banner -nostdin -loglevel error -xerror -ss " +
                    startAt.ToString("0.000", CultureInfo.InvariantCulture) + " -i " + Quote(file) +
                    " -map 0:a:0 -vn -sn -dn -af " + Quote(filter) +
                    " -ac 2 -ar 44100 -c:a pcm_f32le -f f32le pipe:1";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                string diagnostic = "";
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) diagnostic = e.Data; };
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start(); p.BeginErrorReadLine();
                lock (guard) process = p;
                byte[] bytes = new byte[1379*8]; // 32 reference frames/s, alternating 1378/1379 samples.
                byte[] pcm16 = new byte[1379*4];
                slots = new AudioSlot[5]; // about 156ms, bounded output queue.
                int headerSize = Marshal.SizeOf(typeof(WaveHeader));
                for (int k = 0; k < slots.Length; k++)
                {
                    slots[k] = new AudioSlot();
                    slots[k].Data = Marshal.AllocHGlobal(bytes.Length);
                    slots[k].Header = Marshal.AllocHGlobal(headerSize);
                }
                Stream stream = p.StandardOutput.BaseStream;
                int count = 0;
                while (!stopping)
                {
                    AudioSlot slot = slots[count % slots.Length];
                    if (slot.Queued) DrainSlot(device, slot);
                    if (stopping) break;
                    int read = 0, wanted=MeterEnvelope.SamplesInFrame(count)*8;
                    while (read < wanted && !stopping)
                    {
                        int n = stream.Read(bytes, read, wanted - read);
                        if (n == 0) break;
                        read += n;
                    }
                    read -= read % 8;
                    if (read == 0 || stopping) break;
                    float[] samples=new float[read/4];
                    Buffer.BlockCopy(bytes,0,samples,0,read);
                    float gain=outputVolume;
                    for (int j = 0; j < read; j += 8)
                    {
                        double l = BitConverter.ToSingle(bytes, j), rr = BitConverter.ToSingle(bytes, j + 4);
                        if (double.IsNaN(l) || double.IsInfinity(l) || double.IsNaN(rr) || double.IsInfinity(rr))
                            throw new InvalidOperationException("Invalid PCM sample");
                        if (!FloatOutput)
                        {
                            short sl = (short)Math.Max(-32768, Math.Min(32767, Math.Round(l * gain * 32768)));
                            short sr = (short)Math.Max(-32768, Math.Min(32767, Math.Round(rr * gain * 32768)));
                            int o = j / 2; pcm16[o] = (byte)sl; pcm16[o + 1] = (byte)(sl >> 8);
                            pcm16[o + 2] = (byte)sr; pcm16[o + 3] = (byte)(sr >> 8);
                        }
                    }
                    // Meter frames retain the unscaled float samples. Volume affects only this
                    // player's output buffers; it never changes the export graph or system mixer.
                    if(FloatOutput&&gain!=1)
                    {float[] audible=new float[samples.Length];for(int j=0;j<samples.Length;j++)audible[j]=samples[j]*gain;Buffer.BlockCopy(audible,0,bytes,0,read);}
                    int frames = read / 8;
                    lock (guard)
                    {
                        MeterFrame f = new MeterFrame(); f.Start = queuedFrames; f.End = queuedFrames + frames;
                        f.Samples=samples;meterFrames.Add(f);queuedFrames+=frames;
                        // GUI can be minimized: keep only the bounded output queue plus last completed frame.
                        while (meterFrames.Count > 10) meterFrames.RemoveAt(0);
                    }
                    slot.Length = frames;
                    int outputBytes = FloatOutput ? read : read / 2;
                    Marshal.Copy(FloatOutput ? bytes : pcm16, 0, slot.Data, outputBytes);
                    WaveHeader h = new WaveHeader();
                    h.lpData = slot.Data;
                    h.dwBufferLength = outputBytes;
                    Marshal.StructureToPtr(h, slot.Header, false);
                    lock(guard)
                    {
                        if(waveOutPrepareHeader(device,slot.Header,headerSize)!=0)throw new InvalidOperationException("waveOutPrepareHeader failed");
                        slot.Queued=true;
                        if(waveOutWrite(device,slot.Header,headerSize)!=0)throw new InvalidOperationException("waveOutWrite failed");
                    }
                    count++;
                }
                if (!stopping)
                {
                    for (int k = Math.Max(0, count - slots.Length); k < count; k++)
                        DrainSlot(device, slots[k % slots.Length]);
                }
                if (!stopping)
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0) throw new InvalidOperationException("FFmpeg error " + p.ExitCode + ": " + diagnostic);
                }
                lock (guard) if (Object.ReferenceEquals(process, p)) process = null;
                p.Close();
            }
            catch (Exception ex)
            {
                if (!stopping) lock (guard) error = ex.Message;
            }
            finally
            {
                lock(guard)if(device!=IntPtr.Zero)waveOutReset(device);
                if (slots != null)
                {
                    for (int k = 0; k < slots.Length; k++)
                    {
                        AudioSlot slot = slots[k];
                        if (slot == null) continue;
                        if(slot.Queued&&device!=IntPtr.Zero)lock(guard)waveOutUnprepareHeader(device,slot.Header,Marshal.SizeOf(typeof(WaveHeader)));
                        if (slot.Header != IntPtr.Zero) Marshal.FreeHGlobal(slot.Header);
                        if (slot.Data != IntPtr.Zero) Marshal.FreeHGlobal(slot.Data);
                    }
                }
                // Snapshot/Pause/Resume/Stop use the same guard. Unpublish the handle
                // before closing it so a UI tick cannot query a freed driver object.
                lock(guard)
                {if(soundDevice==device)soundDevice=IntPtr.Zero;if(device!=IntPtr.Zero)waveOutClose(device);}
                lock (guard)
                {
                    if (process != null)
                    {
                        try { if (!process.HasExited) process.Kill(); } catch { }
                        process.Close(); process = null;
                    }
                    ended=!stopping&&error==null; playing = false; left = -35; right = -35;
                }
            }
        }
    }
}
