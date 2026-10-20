// PeakMP3 Win7 standalone. .NET Framework 3.5 / CLR 2.0. No external .NET libraries.
// Bundled FFmpeg/LAME notices and sources are in the package licenses/encoder folders.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace PeakMp3Win7
{
    internal sealed class ProcessResult
    {
        public int ExitCode;
        public bool SawOverall;
        public bool HasPeak;
        public bool HasRms;
        public double RmsDb;
        public double PeakDb;
        public bool IsSilent;
        public string Diagnostic;
        internal readonly object PeakGainLock=new object();
        internal readonly bool[] PeakGainKnown=new bool[9];
        internal readonly double[] PeakGainDb=new double[9];
    }

    internal sealed class JobOptions
    {
        public string[] Inputs;
        public string OutputFolder;
        public bool NormalizeMp3Peaks;
        public bool DenseMode;
        public int DenseStrength;
        public int[] DenseStrengths;
        public bool[] AutoSelectDensities;
        public ProcessResult[] KnownAnalyses;
    }

    internal sealed class ProcessingEngine
    {
        private readonly string ffmpegPath;
        private readonly object processLock = new object();
        private Process current;
        private volatile bool cancelRequested;
        internal DensityScore LastVerification;
        private double progressDuration;
        private int progressStart,progressEnd;
        private readonly BackgroundWorker worker;
        private readonly Action<string, string> report;
        private static readonly Regex PeakLine = new Regex(@"\bPeak level dB:\s*(\S+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex RmsLine = new Regex(@"\bRMS level dB:\s*(\S+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ProcessingEngine(string binary, BackgroundWorker background, Action<string, string> reporter)
        {
            ffmpegPath = binary;
            worker = background;
            report = reporter;
        }

        public void Cancel()
        {
            cancelRequested = true;
            lock (processLock)
            {
                try { if (current != null && !current.HasExited) current.Kill(); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }

        private void ThrowIfCancelled()
        {
            if (cancelRequested || worker.CancellationPending) throw new OperationCanceledException();
        }

        private static string Q(string value)
        {
            if (value.IndexOf('"') >= 0) throw new ArgumentException("Invalid double quote in path.");
            return "\"" + value + "\"";
        }

        private ProcessResult Run(string arguments, bool getPeak, bool getRms)
        {
            ThrowIfCancelled();
            ProcessResult result = new ProcessResult();
            Queue<string> recent = new Queue<string>();
            Process p = new Process();
            p.StartInfo.FileName = ffmpegPath;
            p.StartInfo.Arguments = "-hide_banner -nostdin -threads 1 -filter_threads 1 -filter_complex_threads 1 -nostats -progress pipe:2 -stats_period 0.25 " + arguments;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ffmpegPath);
            try
            {
                lock (processLock)
                {
                    ThrowIfCancelled();
                    current = p;
                    p.Start();
                    try{p.PriorityClass=ProcessPriorityClass.BelowNormal;}catch{}
                }
                string line;
                while ((line = p.StandardError.ReadLine()) != null)
                {
                    if(line.StartsWith("out_time_us=",StringComparison.Ordinal)&&progressDuration>0)
                    {double us;if(double.TryParse(line.Substring(12),NumberStyles.Float,CultureInfo.InvariantCulture,out us))report("stageProgress",Math.Max(progressStart,Math.Min(progressEnd,progressStart+(int)((progressEnd-progressStart)*us/1000000/progressDuration))).ToString(CultureInfo.InvariantCulture));}
                    if (recent.Count >= 18) recent.Dequeue();
                    recent.Enqueue(line);
                    if (getPeak || getRms)
                    {
                        // Parse only the Overall block: Peak_level, and RMS_level in dense mode.
                        if (line.IndexOf("Overall", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            line.IndexOf("[Parsed_astats", StringComparison.OrdinalIgnoreCase) >= 0)
                            result.SawOverall = true;
                        else if (result.SawOverall)
                        {
                            Match match = PeakLine.Match(line);
                            if (match.Success)
                            {
                                string token = match.Groups[1].Value;
                                if (string.Equals(token, "-inf", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.HasPeak = true;
                                    result.IsSilent = true;
                                    result.PeakDb = double.NegativeInfinity;
                                }
                                else
                                {
                                    double peak;
                                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out peak)
                                        && !double.IsInfinity(peak) && !double.IsNaN(peak))
                                    {
                                        result.PeakDb = peak;
                                        result.HasPeak = true;
                                    }
                                }
                            }
                            if (getRms && !result.HasRms)
                            {
                                Match rms = RmsLine.Match(line);
                                if (rms.Success)
                                {
                                    double value;
                                    string token = rms.Groups[1].Value;
                                    if (string.Equals(token, "-inf", StringComparison.OrdinalIgnoreCase))
                                    {
                                        result.RmsDb = double.NegativeInfinity;
                                        result.HasRms = true;
                                    }
                                    else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                                        && !double.IsInfinity(value) && !double.IsNaN(value))
                                    {
                                        result.RmsDb = value;
                                        result.HasRms = true;
                                    }
                                }
                            }
                        }
                    }
                }
                p.WaitForExit();
                result.ExitCode = p.ExitCode;
                StringBuilder sb = new StringBuilder();
                foreach (string x in recent) sb.AppendLine(x);
                result.Diagnostic = sb.ToString().Trim();
            }
            finally
            {
                lock (processLock)
                {
                    if (object.ReferenceEquals(current, p)) current = null;
                }
                p.Close();
            }
            ThrowIfCancelled();
            if (result.ExitCode != 0)
                throw new InvalidOperationException("ffmpeg_failed:" + result.ExitCode + ". " + result.Diagnostic);
            if (getPeak && (!result.SawOverall || !result.HasPeak))
                throw new InvalidOperationException("invalid_peak:" + result.Diagnostic);
            if (getRms && (!result.SawOverall || !result.HasRms))
                throw new InvalidOperationException("invalid_rms:" + result.Diagnostic);
            return result;
        }

        private string ReadToolListing(string args)
        {
            ThrowIfCancelled();
            Process p = new Process();
            p.StartInfo.FileName = ffmpegPath;
            p.StartInfo.Arguments = "-hide_banner -nostdin " + args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = false;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ffmpegPath);
            try
            {
                lock (processLock)
                {
                    ThrowIfCancelled();
                    current = p;
                    p.Start();
                    try{p.PriorityClass=ProcessPriorityClass.BelowNormal;}catch{}
                }
                string listing = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                ThrowIfCancelled();
                if (p.ExitCode != 0) throw new InvalidOperationException("FFmpeg " + args + " failed: " + p.ExitCode);
                return listing;
            }
            finally
            {
                lock (processLock)
                {
                    if (object.ReferenceEquals(current, p)) current = null;
                }
                p.Close();
            }
        }

        private static bool HasListedName(string listing, string name)
        {
            // Match the separate FFmpeg filter/encoder name column, not a description.
            return Regex.IsMatch(listing, @"(?m)^\s*[A-Z\.]{3,7}\s+" + Regex.Escape(name) + @"\s", RegexOptions.CultureInvariant);
        }

        public void VerifyCapabilities(bool normalize, bool dense)
        {
            if (!File.Exists(ffmpegPath)) throw new FileNotFoundException("missing_ffmpeg", ffmpegPath);
            if (normalize || dense)
            {
                string filters = ReadToolListing("-filters");
                string[] needed = dense ?
                    new string[] { "astats", "volume", "aresample", "acompressor", "alimiter", "asplit", "amix" } :
                    new string[] { "astats", "volume", "aresample" };
                foreach (string filter in needed)
                    if (!HasListedName(filters, filter)) throw new InvalidOperationException("missing_filter:" + filter);
            }
            string encoders = ReadToolListing("-encoders");
            if (!HasListedName(encoders, "libmp3lame")) throw new InvalidOperationException("missing_encoder");
        }

        private static string CreateName(string source, bool normalized, bool dense)
        {
            string name = Path.GetFileNameWithoutExtension(source).Trim();
            string a = "[MP3 320 normalized]";
            string d = "[MP3 320 dense]";
            string b = "[MP3 320]";
            if (name.EndsWith(a, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - a.Length).TrimEnd();
            else if (name.EndsWith(d, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - d.Length).TrimEnd();
            else if (name.EndsWith(b, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - b.Length).TrimEnd();
            return name + (dense ? " [MP3 320 dense]" : normalized ? " [MP3 320 normalized]" : " [MP3 320]") + ".mp3";
        }

        private static string MakeUniqueDestination(string folder, string source, bool normalized, bool dense)
        {
            string proposed = CreateName(source, normalized, dense);
            string name = Path.GetFileNameWithoutExtension(proposed);
            for (int i = 1; i < 100000; i++)
            {
                string destination = Path.Combine(folder, (i == 1 ? name : name + " (" + i.ToString(CultureInfo.InvariantCulture) + ")") + ".mp3");
                if (!File.Exists(destination) && !string.Equals(Path.GetFullPath(destination), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase))
                    return destination;
            }
            throw new IOException("Cannot find a free output filename.");
        }

        private static double Clamp(double value, double low, double high)
        {
            return Math.Max(low, Math.Min(high, value));
        }

        // Original Maximum compressor/limiter parameters are preserved.
        // UI density controls dry/Maximum mix, never separate low-density gating.
        // User-adjustable strength is frozen at job start.
        internal static double Blend(double a, double b, double fraction)
        {
            return a + (b - a) * fraction;
        }

        internal static void CalculateDense(ProcessResult analysis, int strength,
            out double preGain, out double attack, out double release, out double ratio, out double threshold)
        {
            double t = Clamp(strength / 5.0, 0.0, 1.0);
            double crest = Clamp(analysis.PeakDb - analysis.RmsDb, 6.0, 26.0);
            double oldA = Clamp(20.0 + 0.4 * crest, 22.0, 33.0);
            double newA = Clamp(18.0 + 0.5 * crest, 23.0, 34.0);
            double oldR = Clamp(125.0 + 9.0 * crest, 180.0, 360.0);
            double newR = Clamp(110.0 + 10.0 * crest, 190.0, 410.0);
            attack = Blend(oldA, newA, t);
            release = Blend(oldR, newR, t);
            ratio = Blend(1.65, 1.8, t);
            // 9 adds compression, 10 uses stronger limiting with 12 dB pre-limiter drive.
            if (strength == 9) ratio = 2.05;
            threshold = Math.Pow(10.0, (strength == 9 ? -18.0 : Blend(-14.0, -16.0, t)) / 20.0);
            // Peak-aware guard: a rare loud transient must not trigger extra huge gain.
            // Only unusually low *peak* tracks can extend the old +22 dB makeup ceiling.
            // This leaves the successful density 9/10 ABBA presets unchanged.
            double maxGain = Blend(14.0, 22.0, t);
            if (strength >= 9 && analysis.PeakDb < -18.0)
                maxGain = Math.Min(36.0, maxGain + (-18.0 - analysis.PeakDb));
            preGain = Clamp(Blend(-12.0, -5.0, t) - analysis.RmsDb,
                -4.0, maxGain);
        }

        internal static string DenseFilter(ProcessResult analysis, int strength)
        {
            double preGain, attack, release, ratio, threshold;
            CalculateDense(analysis, strength, out preGain, out attack, out release, out ratio, out threshold);
            return "aresample=44100,volume=" + preGain.ToString("0.000000", CultureInfo.InvariantCulture) + "dB" +
                ",acompressor=threshold=" + threshold.ToString("0.00000000", CultureInfo.InvariantCulture) +
                ":ratio=" + ratio.ToString("0.000", CultureInfo.InvariantCulture) +
                ":attack=" + attack.ToString("0.0", CultureInfo.InvariantCulture) +
                ":release=" + release.ToString("0.0", CultureInfo.InvariantCulture) +
                ":knee=2.5:link=maximum:detection=rms:makeup=1" +
                ",volume=" + (strength == 10 ? 12 : strength).ToString(CultureInfo.InvariantCulture) + "dB" +
                ",alimiter=limit=" + "0.98855309" + ":attack=5:release=" + Blend(100.0, 130.0, Clamp(strength / 5.0, 0.0, 1.0)).ToString("0", CultureInfo.InvariantCulture) +
                ":asc=1:asc_level=0.5:level=0:latency=1";
        }

        // Shared DSP graph for exporting and previewing. No intermediate compressed media.
        internal static string FilterForOptions(ProcessResult analysis, bool normalize, bool dense, int strength)
        {
            if (dense)
            {
                // A convex, time-aligned dry/Maximum blend. Endpoints bypass amix.
                if(strength<=0)return strength==0?"aresample=44100":"aresample=44100,volume="+Math.Pow(10,strength/20.0).ToString("R",CultureInfo.InvariantCulture);
                if(analysis==null)throw new InvalidOperationException("analysis_not_ready");
                if(analysis.IsSilent)return "aresample=44100";
                string maximum=DenseFilter(analysis,10);
                if(strength==10)return maximum;
                if(strength>10)return maximum+",volume="+(1+(strength-10)/10.0).ToString("R",CultureInfo.InvariantCulture)+":precision=double,alimiter=limit=1:attack=5:release=130:asc=1:asc_level=0.5:level=0:latency=1";
                double wet=strength/10.0;
                return "aresample=44100,asplit=2[dry][wet];[wet]"+maximum+
                    "[maximum];[dry][maximum]amix=inputs=2:weights='"+
                    (1-wet).ToString("0.0",CultureInfo.InvariantCulture)+" "+
                    wet.ToString("0.0",CultureInfo.InvariantCulture)+
                    "':normalize=0:duration=first:dropout_transition=0";
            }
            if (normalize)
            {
                double gain = !analysis.IsSilent && analysis.PeakDb < -0.1 ? -analysis.PeakDb : 0.0;
                return "aresample=44100,volume=" + gain.ToString("0.000000", CultureInfo.InvariantCulture) + "dB";
            }
            return "aresample=44100";
        }

        internal static bool FilterReady(ProcessResult analysis,bool dense,int strength)
        {
            if(dense&&strength<=0)return true;
            return analysis!=null;
        }
        internal void PrepareDenseOutput(string input,ProcessResult analysis,int strength)
        {
            // The Maximum analysis is shared; blending requires no extra peak pass.
            ThrowIfCancelled();

        }

        // Cached once per selected track. Density changes only rebuild the filter graph.
        internal ProcessResult AnalyzeForPreview(string input, bool normalize, bool dense)
        {
            if (!normalize && !dense) return new ProcessResult();
            VerifyCapabilities(normalize, dense);
            string measures = dense ? "Peak_level+RMS_level" : "Peak_level";
            return Run("-loglevel info -xerror -i " + Q(input) +
                " -map 0:a:0 -vn -sn -dn -af " + Q("aresample=44100,astats=metadata=0:reset=0:measure_perchannel=none:measure_overall=" + measures) +
                " -c:a pcm_f32le -f null -", true, dense);
        }

        public void Convert(JobOptions job)
        {
            if (job.NormalizeMp3Peaks && job.DenseMode) throw new InvalidOperationException("conflicting_modes");

            VerifyCapabilities(job.NormalizeMp3Peaks, job.DenseMode);
            Directory.CreateDirectory(job.OutputFolder);
            for (int i = 0; i < job.Inputs.Length; ++i)
            {
                ThrowIfCancelled();
                if(job.DenseStrengths!=null){job.DenseStrength=job.DenseStrengths[i];}
                report("densitySelected",job.DenseStrength.ToString(CultureInfo.InvariantCulture));
                string input = job.Inputs[i];
                if (!File.Exists(input)) throw new FileNotFoundException("source_missing", input);
                string displayName = Path.GetFileName(input);
                report("item", (i + 1) + "/" + job.Inputs.Length + "  " + displayName);
                progressDuration=PreviewPlayer.GetDuration(ffmpegPath,input);progressStart=0;progressEnd=25;
                double gain = 0.0;
                ProcessResult analysis = null;
                if (job.NormalizeMp3Peaks || (job.DenseMode && job.DenseStrength>0))
                {
                    report(job.DenseMode ? "denseAnalysis" : "analysis", displayName);
                    // Floating-point null output avoids 16-bit clipping during Peak_level analysis.
                    string measures = job.DenseMode ? "Peak_level+RMS_level" : "Peak_level";
                    analysis = job.KnownAnalyses!=null&&job.KnownAnalyses[i]!=null?job.KnownAnalyses[i]:Run("-loglevel info -xerror -i " + Q(input) +
                        " -map 0:a:0 -vn -sn -dn -af " + Q("aresample=44100,astats=metadata=0:reset=0:measure_perchannel=none:measure_overall=" + measures) +
                        " -c:a pcm_f32le -f null -", true, job.DenseMode);
                    if (job.NormalizeMp3Peaks)
                    {
                        if (!analysis.IsSilent && analysis.PeakDb < -0.1) gain = -analysis.PeakDb;
                        report("gain", (analysis.IsSilent ? "-inf" : analysis.PeakDb.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture)) +
                            " dBFS -> " + gain.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture) + " dB");
                    }
                    else
                    {
                        // Whole-track RMS is only used in the explicitly selected dense mode.
                        // Bounded makeup: silence remains silent, quiet material is not boosted without limit.
                        double attack = 0.0, release = 0.0, ratio = 0.0, threshold = 0.0;
                        if (!analysis.IsSilent && !double.IsNegativeInfinity(analysis.RmsDb))
                              CalculateDense(analysis, 10, out gain, out attack, out release, out ratio, out threshold);
                        report("denseGain", "RMS " + analysis.RmsDb.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) +
                            " dBFS; pre " + gain.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) +
                              " dB; boost +" + (analysis.IsSilent ? 0 : 12).ToString(CultureInfo.InvariantCulture) +
                            " dB; A " + attack.ToString("0", CultureInfo.InvariantCulture) +
                            " ms; R " + release.ToString("0", CultureInfo.InvariantCulture) + " ms");
                    }
                }
                if(job.DenseMode)PrepareDenseOutput(input,analysis,job.DenseStrength);
                ThrowIfCancelled();
                string target = MakeUniqueDestination(job.OutputFolder, input, job.NormalizeMp3Peaks, job.DenseMode);
                string scratch = Path.Combine(job.OutputFolder, ".peakmp3-" + Guid.NewGuid().ToString("N") + ".partial");
                try
                {
                    progressStart=25;progressEnd=90;
                    report("encode", Path.GetFileName(target));
                    // 2nd full decode. Gain before libmp3lame. No intermediate AAC/MP3.
                    string audioFilter = (job.DenseMode || job.NormalizeMp3Peaks) ?
                        " -af " + Q(FilterForOptions(analysis, job.NormalizeMp3Peaks, job.DenseMode, job.DenseStrength)) : "";
                    Run("-loglevel error -xerror -i " + Q(input) +
                        " -map 0:a:0 -vn -sn -dn" + audioFilter +
                        " -ar 44100 -c:a libmp3lame -b:a 320k -id3v2_version 3 -f mp3 " + Q(scratch), false, false);
                    ThrowIfCancelled();
                    FileInfo file = new FileInfo(scratch);
                    if (!file.Exists || file.Length == 0) throw new InvalidOperationException("empty_mp3");
                    report("verify", Path.GetFileName(target));
                    // End-to-end decode validation before publishing the result.
                    AutoDensityPicker verifier=new AutoDensityPicker(ffmpegPath);
                    DensityScore verified=verifier.Measure(scratch,delegate(int seconds){ThrowIfCancelled();if(progressDuration>0)report("stageProgress",Math.Min(99,90+(int)(9*seconds/progressDuration)).ToString(CultureInfo.InvariantCulture));});
                    LastVerification=verified;report("vuResult",verified.Description);
                    ThrowIfCancelled();
                    File.Move(scratch, target); // atomic rename in the same directory
                    report("saved", target);
                }
                finally
                {
                    try { if (File.Exists(scratch)) File.Delete(scratch); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
    }
}
