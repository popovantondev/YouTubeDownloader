// User-approved PeakMP3 studio panel. Windows Forms UI over the existing audio engine.
// Shared controller/calibration; the vintage image skin is implemented in VintageSurface.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace PeakMp3Win7
{
    internal sealed partial class ApprovedWindow : Form
    {
        private readonly UserPrefs prefs;
        private readonly BackgroundWorker worker;
        private readonly string ffmpeg;
        private readonly List<string> files = new List<string>();
        private readonly Dictionary<string, string> outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> outputStrengths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private int exportedStrength;
        private readonly List<string> jobFiles = new List<string>();
        private int jobIndex = -1;
        private ProcessingEngine engine;
        private PreviewPlayer player;
        private bool initializing = true, closingAfterCancel;
        private string activeFile;
        private RecordName recordName=new RecordName("","PeakMP3");private int metadataGeneration;
        private double duration, position;
        
        private float uiScale = 1;
        private Panel header;
        private StudioButton minimize, closeWindow;
        private readonly object analysisGuard = new object();
        private readonly Dictionary<string, ProcessResult> analysisCache = new Dictionary<string, ProcessResult>();
        private readonly System.Windows.Forms.Timer playbackTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer applyTimer = new System.Windows.Forms.Timer();
        private Panel viewport, panel, fileCard, dropPanel, queueCard;
        private Control fileIcon;
        private readonly Dictionary<Control,Font> ownedFonts=new Dictionary<Control,Font>();
        private StudioLogo logo;
        private StudioTitle title;
        private Label densityCaption, fileName, fileMeta, timeLabel, status, queueCount, queueHeading;
        private StudioButton langRU, langDE, langEN;
        private ApprovedVu meter;
        private StudioKnob knob;
        private WaveformView wave;
        private StudioButton settings, soft, balanced, loud, maxed, removeFile, buttonPlay, buttonStop,
            buttonCreate, buttonLog, buttonAdd, buttonCancel, buttonQueue;
        private StudioABSwitch comparison;
        private ListBox queue;
        private ProgressBar itemProgress, totalProgress;
        private TextBox logData;
        
        private const int PanelHeight = 1425;

        public ApprovedWindow()
        {
            prefs = UserPrefs.Load();
            actualDensity=prefs.ExtendedDensity;
            prefs.NormalizeMp3Peaks = false; prefs.DenseMode = true;
            ffmpeg = Path.Combine(Application.StartupPath, "ffmpeg.exe");
            Font = new Font("Tahoma", 9.0f);
            Text = "PeakMP3";
            BackColor = StudioPalette.Silver;
            ClientSize = new Size(696, 945);
            MinimumSize = new Size(380, 480);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) uiScale = screen.DpiX / 96f * .515f;
            ClientSize = new Size((int)(846*uiScale), Math.Min((int)(PanelHeight*uiScale), Screen.PrimaryScreen.WorkingArea.Height-36));
            MinimumSize = new Size(380,Math.Min(480,Screen.PrimaryScreen.WorkingArea.Height-36));
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            worker = new BackgroundWorker(); worker.WorkerSupportsCancellation = true; worker.WorkerReportsProgress = true;
            worker.DoWork += Work; worker.ProgressChanged += WorkProgress; worker.RunWorkerCompleted += WorkComplete;
            header = new StudioSurface(); ((StudioSurface)header).BorderVisible=false; header.Dock = DockStyle.Top; Controls.Add(header);
            viewport = new Panel(); viewport.Dock = DockStyle.Fill; viewport.AutoScroll = true;
            Controls.Add(viewport);
            viewport.BringToFront();
            panel = new StudioSurface(); ((StudioSurface)panel).BorderVisible=false; panel.Height = PanelHeight; panel.BackColor = StudioPalette.Silver;
            viewport.Controls.Add(panel);
            panel.AllowDrop = true; panel.DragEnter += DragEnterFiles; panel.DragDrop += DropFiles;
            viewport.AllowDrop = true; viewport.DragEnter += DragEnterFiles; viewport.DragDrop += DropFiles;
            CreateControls();
            knob.Value = prefs.DenseStrength;
            UpdateTexts();
            initializing = false;
            Resize += delegate { LayoutAll(); };
            LayoutAll();
            playbackTimer.Interval = 31; playbackTimer.Tick += UpdatePlayback; playbackTimer.Start();
            applyTimer.Interval = 260; applyTimer.Tick += delegate { applyTimer.Stop(); ChangeLiveDensity(); };
            FormClosing += OnClosing;
            FormClosed+=delegate{foreach(Font f in ownedFonts.Values)f.Dispose();};
            SetupVintage();
        }
        private string T(string key) { return L10n.Get(key, prefs.Language); }
        private string DescribeError(Exception error)
        {
            string message=error.Message;
            string[] codes={"missing_ffmpeg","missing_filter:","missing_encoder","invalid_peak:","invalid_rms:","conflicting_modes","empty_mp3","source_missing","ffmpeg_failed:"};
            string[] keys={"missingFFmpeg","missingFilter","missingEncoder","invalidPeak","invalidRms","conflictingModes","emptyMp3","sourceMissing","ffmpegFailed"};
            for(int i=0;i<codes.Length;i++)if(message.StartsWith(codes[i],StringComparison.Ordinal))
            {
                string detail=message.Substring(codes[i].Length);
                FileNotFoundException missing=error as FileNotFoundException;
                if(missing!=null&&missing.FileName!=null)detail+=" "+missing.FileName;
                return T(keys[i])+(detail.Length>0?" "+detail:"");
            }
            return message;
        }
        private Label MakeLabel(Control parent, bool bold)
        {
            Label label = new Label(); label.BackColor = Color.Transparent;
            label.AutoEllipsis = true; label.TextAlign = ContentAlignment.MiddleLeft;
            if (bold) label.Font = new Font(Font, FontStyle.Bold);
            parent.Controls.Add(label); return label;
        }
        private StudioButton MakeButton(Control parent, EventHandler click, bool orange)
        {
            StudioButton button = new StudioButton();
            button.BackColor = orange ? StudioPalette.Orange : Color.FromArgb(248, 247, 244);
            button.ForeColor = orange ? Color.White : StudioPalette.Ink;
            button.Font = new Font("Tahoma", 9, orange ? FontStyle.Bold : FontStyle.Regular);
            button.Click += click; parent.Controls.Add(button); return button;
        }
        private Panel MakePanel(int h)
        {
            Panel p = new StudioSurface(); p.Height = h; p.BackColor = Color.FromArgb(250, 249, 247);
            p.BorderStyle = BorderStyle.None; panel.Controls.Add(p); return p;
        }
        private void CreateControls()
        {
            logo = new StudioLogo(); panel.Controls.Add(logo);
            title = new StudioTitle(); panel.Controls.Add(title);
            langRU = MakeButton(panel, delegate { ChangeLanguage(0); }, false);
            langDE = MakeButton(panel, delegate { ChangeLanguage(1); }, false);
            langEN = MakeButton(panel, delegate { ChangeLanguage(2); }, false);
            settings = MakeButton(panel, delegate { OpenSettings(); }, false); settings.Tag = "gear";settings.Visible=false;
            minimize = MakeButton(header, delegate { WindowState=FormWindowState.Minimized; }, false); minimize.Tag="minimize";
            closeWindow = MakeButton(header, delegate { Close(); }, false); closeWindow.Tag="closeWindow";
            foreach(Control c in new Control[]{logo,title,langRU,langDE,langEN,settings})header.Controls.Add(c);
            foreach(Control c in new Control[]{header,logo,title})c.MouseDown+=DragWindow;
            
            meter = new ApprovedVu();meter.CalibrationRequested+=delegate{OpenSettings();}; panel.Controls.Add(meter);
            knob = new StudioKnob(); knob.ValueChanged += delegate { if (!initializing) DensityChanged(); };
            panel.Controls.Add(knob);
            densityCaption = MakeLabel(panel, true); densityCaption.TextAlign = ContentAlignment.MiddleCenter;
            soft = MakeButton(panel, delegate { knob.Value = 2; }, false); soft.Tag = "presetSoft";
            balanced = MakeButton(panel, delegate { knob.Value = 5; }, false); balanced.Tag = "presetBalance";
            loud = MakeButton(panel, delegate { knob.Value = 8; }, false); loud.Tag = "presetLoud";
            maxed = MakeButton(panel, delegate { knob.Value = 10; }, false); maxed.Tag = "presetMax";
            fileCard = MakePanel(130);
            fileIcon = new StudioFileIcon();fileCard.Controls.Add(fileIcon);
            fileName = MakeLabel(fileCard, true); fileName.Font = new Font("Tahoma", 10, FontStyle.Bold);
            fileMeta = MakeLabel(fileCard, false); fileMeta.ForeColor = Color.FromArgb(117, 109, 102);
            removeFile = MakeButton(fileCard, delegate { RemoveCurrent(); }, false);removeFile.Tag="removeFile";
            wave = new WaveformView(); wave.SeekRequested += delegate(double sec) { Seek(sec); };
            fileCard.Controls.Add(wave);
            timeLabel = MakeLabel(fileCard, false); timeLabel.TextAlign = ContentAlignment.MiddleRight;
            comparison = new StudioABSwitch(); panel.Controls.Add(comparison);
            comparison.ModeChanged += delegate { SwitchPreviewSource(); };
            buttonCreate = MakeButton(panel, delegate { StartJob(); }, true);buttonCreate.Tag="export";
            buttonPlay = MakeButton(panel, delegate { RequestPlayback(); }, false);buttonPlay.Tag="play";
            buttonStop = MakeButton(panel, delegate { StopPlayback(true); UpdatePosition(); }, false); buttonStop.Visible=false;
            ContextMenuStrip transport=new ContextMenuStrip(); transport.Items.Add(T("previewStop"),null,delegate{StopPlayback(true);UpdatePosition();});transport.Opening+=delegate{transport.Items[0].Text=T("previewStop");};buttonPlay.ContextMenuStrip=transport;
            buttonLog = MakeButton(panel, delegate { ShowLog(); }, false);buttonLog.Tag="log";
            dropPanel = new StudioDropZone(); panel.Controls.Add(dropPanel);
            dropPanel.AllowDrop = true; dropPanel.DragEnter += DragEnterFiles; dropPanel.DragDrop += DropFiles;
            buttonAdd = MakeButton(dropPanel, delegate { AddDialog(); }, false);
            queueCount = MakeLabel(dropPanel, false); queueCount.AllowDrop = true;
            queueCount.DragEnter += DragEnterFiles; queueCount.DragDrop += DropFiles;
            buttonQueue = MakeButton(panel, delegate { queueCard.Visible = !queueCard.Visible; LayoutAll(); UpdateTexts(); }, false);
            queueCard = MakePanel(90);
            queueHeading = MakeLabel(queueCard, true);
            queue = new NoFocusListBox(); queue.HorizontalScrollbar = true;
            queue.SelectedIndexChanged += delegate { SelectQueueItem(); };
            queueCard.Controls.Add(queue); queueCard.Visible = false;
            status = MakeLabel(panel, false); status.ForeColor = Color.FromArgb(96, 76, 55);
            buttonCancel = MakeButton(panel, delegate { CancelJob(); }, false); buttonCancel.Enabled = false;
            itemProgress = new ProgressBar(); panel.Controls.Add(itemProgress);
            totalProgress = new ProgressBar(); panel.Controls.Add(totalProgress);
            logData = new TextBox(); logData.Multiline = true; logData.ScrollBars = ScrollBars.Vertical;
            queueCount.Cursor=Cursors.Hand;queueCount.Click+=delegate{AddDialog();};
            logData.ReadOnly = true; logData.BackColor = Color.White;
            Log("PeakMP3 desktop panel initialized");
        }
        [DllImport("user32.dll")]private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        private void DragWindow(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}}
        protected override bool ProcessDialogKey(Keys keyData)
        { if((keyData&Keys.KeyCode)==Keys.Tab)return true;return base.ProcessDialogKey(keyData); }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x84 && WindowState==FormWindowState.Normal)
            {
                Point q=PointToClient(new Point((short)((long)m.LParam&65535),(short)(((long)m.LParam>>16)&65535)));int b=6;
                bool l=q.X<b,r=q.X>ClientSize.Width-b,t=q.Y<b,bt=q.Y>ClientSize.Height-b;
                if(l||r||t||bt){m.Result=new IntPtr(t?(l?13:r?14:12):bt?(l?16:r?17:15):l?10:11);return;}
            }
            base.WndProc(ref m);
        }
        private void Place(Control c, int x, int y, int w, int h)
        {
            if(c.Parent==panel)y-=84;
            c.SetBounds((int)Math.Round(x*uiScale),(int)Math.Round(y*uiScale),Math.Max(4,(int)Math.Round(w*uiScale)),Math.Max(4,(int)Math.Round(h*uiScale)));
            c.TabStop=false;
        }
        private bool layingOut;
        private void LayoutAll()
        {
            if(vintage!=null){RoundVintageWindow();vintage.InvalidateCache();return;}
            if(panel==null||queueCard==null||layingOut)return;
            layingOut=true;
            try
            {
            header.SuspendLayout();viewport.SuspendLayout();panel.SuspendLayout();
            // Coordinates follow the approved 846 x 1465 window, with a pinned header.
            uiScale=Math.Max(.42f,(ClientSize.Width-4)/846f);
            header.Height=(int)(84*uiScale);
            // Reserve the vertical scrollbar width before laying out tall content.
            // Otherwise its appearance makes the body wider than the viewport.
            int bodyWidth=Math.Max(1,viewport.Width-SystemInformation.VerticalScrollBarWidth);
            panel.Width=bodyWidth;
            float bodyScale=panel.Width/846f;
            Place(logo,34,17,64,57);Place(title,116,15,235,56);
            Place(langRU,390,17,73,56);Place(langDE,465,17,73,56);Place(langEN,540,17,73,56);
            settings.Visible=false;Place(minimize,710,22,52,45);Place(closeWindow,780,22,52,45);
            uiScale=bodyScale;
            Place(meter,29,97,790,360);
            Place(knob,223,451,400,370);
            Place(soft,48,526,165,96);Place(balanced,48,674,165,96);Place(loud,633,526,165,96);Place(maxed,633,674,165,96);
            Place(densityCaption,277,821,292,36);
            Place(fileCard,29,865,790,227);
            Place(fileIcon,22,20,84,85);Place(fileName,132,19,565,41);Place(fileMeta,132,64,565,32);Place(removeFile,720,33,49,49);
            Place(wave,20,113,750,104);timeLabel.Visible=false;
            Place(comparison,29,1111,790,74);
            Place(buttonPlay,29,1198,280,94);Place(buttonCreate,326,1198,493,94);buttonStop.Visible=false;
            Place(dropPanel,29,1320,790,86);buttonLog.Visible=false;
            Place(buttonAdd,12,8,72,70);Place(queueCount,100,12,660,62);
            Place(buttonQueue,419,1414,380,32);buttonQueue.Visible=files.Count>1;
            int y=files.Count>1?1465:PanelHeight;
            if(queueCard.Visible){Place(queueCard,29,y,790,120);Place(queueHeading,12,3,766,24);Place(queue,12,29,766,78);y+=128;}
            bool showProgress=worker.IsBusy||status.Tag!=null;
            status.Visible=itemProgress.Visible=totalProgress.Visible=showProgress;buttonCancel.Visible=worker.IsBusy;
            if(showProgress){Place(status,29,y,640,37);Place(buttonCancel,681,y,138,37);Place(itemProgress,29,y+43,790,7);Place(totalProgress,29,y+56,790,5);y+=76;}
            panel.Height=(int)((y-84)*uiScale);viewport.AutoScrollMinSize=new Size(0,panel.Height+Math.Max(6,(int)(10*uiScale)));
            SetUiFonts(panel);SetUiFonts(header);
            foreach(Control child in header.Controls)child.Invalidate();
            panel.Invalidate(true);header.Invalidate(true);
            // Round the outer casing only; all header and footer controls stay inside its inset.
            StudioHardware.RoundWindow(this,12);
            }
            finally{panel.ResumeLayout(false);viewport.ResumeLayout(true);header.ResumeLayout(false);layingOut=false;}
        }
        private void SetUiFonts(Control root)
        {
            foreach(Control c in root.Controls)
            {
                float size=18;FontStyle style=FontStyle.Regular;
                if(c==fileName){size=25;style=FontStyle.Bold;}
                if(c==fileMeta)size=20;
                if(c==densityCaption){size=28;style=FontStyle.Bold;}
                if(c==buttonCreate){size=32;style=FontStyle.Bold;}
                if(c==buttonPlay||c==buttonLog)size=27;
                if(c==soft||c==balanced||c==loud||c==maxed)size=26;
                if(c==langRU||c==langDE||c==langEN)size=22;
                if(c==queueCount)size=18;
                Font prior;ownedFonts.TryGetValue(c,out prior);
                if(c!=knob&&c!=meter&&c!=title&&c!=logo&&c!=wave)
                    {
                        float pixels=Math.Max(9,size*uiScale);
                        // WinForms may retain an equal Font instance. Never dispose an active font.
                        if(prior==null||Math.Abs(prior.Size-pixels)>.01f||prior.Style!=style)
                        {Font next=new Font("Tahoma",pixels,style,GraphicsUnit.Pixel);c.Font=next;ownedFonts[c]=next;if(prior!=null)prior.Dispose();}
                    }
                c.TabStop=false;
                if(c.Controls.Count>0)SetUiFonts(c);
            }
        }
        private void UpdateTexts()
        {
            Text = "PeakMP3 — " + T("dense")+(Path.GetFileName(Application.ExecutablePath).IndexOf("DesignPreview",StringComparison.OrdinalIgnoreCase)>=0?" · Design "+Regex.Match(Path.GetFileNameWithoutExtension(Application.ExecutablePath),@"\d+$").Value:"");
            title.Text = "PeakMP3";
            settings.Text = "";
            langRU.Text = "RU"; langDE.Text = "DE"; langEN.Text = "EN";
            StudioButton[] langs = { langRU, langDE, langEN };
            for (int i = 0; i < langs.Length; i++)
                langs[i].BackColor = prefs.Language == i ? Color.FromArgb(255, 220, 184) : Color.FromArgb(248, 247, 244);
            soft.Text = T("presetSoft"); balanced.Text = T("presetBalance");
            loud.Text = T("presetLoud"); maxed.Text = T("presetMax");
            densityCaption.Text = T("strength");meter.AccessibleName=T("calibrationTitle");meter.AccessibleDescription=T("calibrationHint");
            fileName.Text = activeFile == null ? T("noActiveFile") : Path.GetFileName(activeFile);
            if (activeFile == null) fileMeta.Text = "MP3 / AAC / WAV / FLAC";
            removeFile.Text = "×";
            comparison.Caption = T("compare");
            comparison.Original = "A — " + T("compOriginal");
            comparison.ProcessedText = "B — " + T("compProcessed");
            comparison.Invalidate();
            buttonCreate.Text = T("start"); buttonPlay.Text = player != null && player.IsPlaying && !player.IsPaused ? T("pause") : T("listen");
            buttonStop.Text = "■"; buttonLog.Text = T("log");
            buttonAdd.Text = "⇧"; buttonAdd.Tag="addFiles"; buttonQueue.Text = (queueCard.Visible ? "▴ " : "▾ ") + T("queue") + " (" + files.Count + ")";
            queueCount.Text = T("dropQueue")+"\n"+T("multipleFiles"); queueHeading.Text = T("queue") + " — " + files.Count;
            buttonCancel.Text = T("cancel");
            if (string.IsNullOrEmpty(status.Text)) status.Text = T("ready");
            wave.LoadingText = T("waveLoading"); wave.EmptyText = T("waveEmpty");
            wave.Invalidate();
            comparison.Enabled = activeFile != null;
            buttonCreate.Enabled = files.Count > 0 && !worker.IsBusy;
            removeFile.Enabled = activeFile != null && !worker.IsBusy;
            buttonPlay.Enabled = activeFile != null;
            HighlightPreset();
        }
        private void ChangeLanguage(int value)
        {
            prefs.Language = value; status.Text=T("ready"); Save(); UpdateTexts(); LayoutAll();
        }
        private void HighlightPreset()
        {
            int v = knob.Value;
            StudioButton[] all = { soft, balanced, loud, maxed };
            int[] values = { 2, 5, 8, 10 };
            for (int i = 0; i < all.Length; i++)
                all[i].BackColor = v == values[i] ? Color.FromArgb(255, 218, 183) : Color.FromArgb(248, 247, 244);
        }
        private void Save()
        {
            try { prefs.Save(); } catch (Exception ex) { Log(ex.Message); }
        }
        private static bool Supported(string file)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            return ext == ".mp3" || ext == ".aac" || ext == ".m4a" || ext == ".mp4" || ext == ".wav" || ext == ".flac";
        }
        private void AddFiles(string[] incoming)
        {
            if (worker.IsBusy) return;
            foreach (string path in incoming)
                if (File.Exists(path) && Supported(path) && !files.Contains(path)) files.Add(path);
            FillQueue();
            if (activeFile == null && files.Count > 0) SetCurrent(files[0]);
            UpdateTexts();
        }
        private void FillQueue()
        {
            int idx = activeFile == null ? -1 : files.IndexOf(activeFile);
            queue.BeginUpdate(); queue.Items.Clear();
            foreach (string f in files) queue.Items.Add(Path.GetFileName(f));
            if (idx >= 0) queue.SelectedIndex = idx;
            queue.EndUpdate();
        }
        private void SelectQueueItem()
        {
            if (worker.IsBusy) return;
            int idx = queue.SelectedIndex;
            if (idx >= 0 && idx < files.Count && files[idx] != activeFile) SetCurrent(files[idx]);
        }
        private void SetCurrent(string input)
        {
            StopPlayback(true);
            activeFile = input; position = 0;metadataGeneration++;recordName=IncomingName(input,RecordName.Resolve(input,"",""));
            duration = PreviewPlayer.GetDuration(ffmpeg, input);
            wave.LoadWaveform(ffmpeg, input, duration);
            if (queue.Items.Count > 0) { int idx = files.IndexOf(input); if (idx >= 0 && queue.SelectedIndex != idx) queue.SelectedIndex = idx; }
            fileMeta.Text = Path.GetExtension(input).TrimStart('.').ToUpperInvariant() + "  •  " +
                (new FileInfo(input).Length / 1048576.0).ToString("0.00") + " MB  •  " + T("readingMeta");
            ProbeMetadata(input);
            comparison.Processed = false;
            PrepareSelectedFile();
            UpdateTexts();
        }
        private void ProbeMetadata(string input)
        {
            int generation=metadataGeneration;long inputLength = 0;
            try { inputLength = new FileInfo(input).Length; } catch (IOException) { }
            System.Threading.Thread thread = new System.Threading.Thread(delegate()
            {
                string data = "";
                try
                {
                    Process p = new Process(); p.StartInfo.FileName = ffmpeg;
                    p.StartInfo.Arguments = "-hide_banner -nostdin -i \"" + input.Replace("\"", "") + "\"";
                    p.StartInfo.UseShellExecute = false; p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;p.StartInfo.StandardErrorEncoding=System.Text.Encoding.UTF8;
                    p.Start(); data = p.StandardError.ReadToEnd(); p.WaitForExit(); p.Close();
                }
                catch { }
                RecordName parsed=RecordName.FromDiagnostic(input,data);
                Match freq = Regex.Match(data, @"(\d+) Hz");
                Match rate = Regex.Match(data, @"(\d+) kb/s");
                string info = Path.GetExtension(input).TrimStart('.').ToUpperInvariant() + "  •  " +
                    (inputLength / 1048576.0).ToString("0.00") + " MB" +
                    (freq.Success ? "  •  " + freq.Groups[1].Value + " Hz" : "") +
                    (rate.Success ? "  •  " + rate.Groups[1].Value + " kb/s" : "");
                if (!IsHandleCreated || IsDisposed) return;
                try { BeginInvoke((MethodInvoker)delegate { if (input == activeFile&&generation==metadataGeneration){fileMeta.Text = info;recordName=IncomingName(input,parsed);RefreshVintage();} }); }
                catch (InvalidOperationException) { }
            });
            thread.IsBackground = true; thread.Start();
        }
        private void AddDialog()
        {
            using (OpenFileDialog picker = new OpenFileDialog())
            { picker.Multiselect = true; picker.Filter = T("inputfilter"); if (picker.ShowDialog(this) == DialogResult.OK) AddFiles(picker.FileNames); }
        }
        private void RemoveCurrent()
        {
            if (activeFile == null || worker.IsBusy) return;
            string old = activeFile; StopPlayback(true); files.Remove(old);
            activeFile = null; wave.Clear(); duration = position = 0;
            FillQueue();
            if (files.Count > 0) SetCurrent(files[0]); else { fileMeta.Text = ""; UpdateTexts(); }
        }
        private void DragEnterFiles(object sender, DragEventArgs e)
        { e.Effect = !worker.IsBusy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; }
        private void DropFiles(object sender, DragEventArgs e)
        { if (!worker.IsBusy) { string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[]; if (paths != null) AddFiles(paths); } }
        private bool PreviewUsesReady()
        {
            if (activeFile == null || !comparison.Processed) return false;
            string path; int strength;
            return outputs.TryGetValue(activeFile, out path) && File.Exists(path) &&
                outputStrengths.TryGetValue(activeFile, out strength) && strength == CurrentDensity;
        }
        private string PreviewFile()
        {
            if (activeFile == null) return null;
            return PreviewUsesReady() ? outputs[activeFile] : activeFile;
        }
        private bool PreviewUsesLive() { return comparison.Processed && !PreviewUsesReady(); }
        private void SwitchPreviewSource()
        {
            if (activeFile == null || player == null || !player.IsPlaying) return;
            double l, r, sec; bool active; string err;
            player.Snapshot(out l, out r, out sec, out active, out err);
            double at = Math.Max(0, Math.Min(duration, sec));
            bool wasPaused = player.IsPaused;
            StopPlayback(false); position = at;
            StartPlayback(); if (wasPaused) { player.Pause();  }
        }
        private void StartPlayback()
        {
            if (activeFile == null) return;
            headline="";transportState=1;
            if (!File.Exists(ffmpeg)) { MessageBox.Show(T("missingFFmpeg")); return; }
            try
            {
                string input = PreviewFile();
                if (player != null && !player.IsPlaying) { player.Dispose(); player = null; }
                if (player == null)
                {
                    string key=input+"|"+new FileInfo(input).Length+"|"+File.GetLastWriteTimeUtc(input).Ticks;
                    ProcessResult known; lock(analysisGuard)analysisCache.TryGetValue(key,out known);
                    player=new PreviewPlayer(ffmpeg,input,false,PreviewUsesLive(),CurrentDensity,known,
                        delegate(ProcessResult measured){if(measured.HasPeak&&measured.HasRms)lock(analysisGuard)analysisCache[key]=measured;});
                }
                player.Volume=previewVolume/100f;
                player.Play(position); 
                status.Text = T("previewAnalyzing"); UpdateTexts();
            }
            catch (Exception ex) { status.Text = T("previewError") + ": " + ex.Message;headline=status.Text;Log(status.Text); }
        }
        private void DirectPlayPause()
        {
            if (player == null || !player.IsPlaying) StartPlayback();
            else if (player.IsPaused) { player.Resume();  }
            else { player.Pause();  }
            UpdateTexts();
        }
        private void StopPlayback(bool reset)
        {
            if (player != null) { player.Dispose(); player = null; }
            meter.SetLevels(-20, -20);
            if (reset) position = 0;
             wave.PositionSeconds = position;
            if (buttonPlay != null) buttonPlay.Text = T("listen");
        }
        private void Seek(double sec)
        {
            if (activeFile == null) return;
            position = Math.Max(0, Math.Min(duration, sec));
            if (player != null && player.IsPlaying)
            {
                bool pause = player.IsPaused;
                player.Play(position);player.Pause();
                if(!pause){pendingPlay=pendingResume=true;transportState=1;if(vintage!=null)vintage.Mechanics.BeginSeek();}
            }
            UpdatePosition();
        }
        private void UpdatePosition()
        {
            timeLabel.Text = ToTime(position) + " / " + ToTime(duration);
            if (!wave.IsScrubbing) wave.PositionSeconds = position;
        }
        private static string ToTime(double seconds)
        {
            int s = (int)Math.Max(0, seconds);
            return (s / 60).ToString() + ":" + (s % 60).ToString("00");
        }
        private void DensityChanged()
        {
            if(tuningKnob)return;
            ManualDensity();
            prefs.DenseStrength = knob.Value; Save();
            densityCaption.Text = T("strength");meter.AccessibleName=T("calibrationTitle");meter.AccessibleDescription=T("calibrationHint");
            HighlightPreset();
            if (player != null && player.IsPlaying)
            {
                // A/B stays a true two-state switch. Updating density selects B's
                // live filter; existing ready MP3 remains available at its density.
                if (!comparison.Processed) { comparison.Processed = true; return; }
                applyTimer.Stop(); applyTimer.Start();
            }
        }
        private void ChangeLiveDensity()
        {
            if (player == null || !comparison.Processed) return;
            double a, b, at; bool active; string error;
            player.Snapshot(out a, out b, out at, out active, out error);
            bool paused = player.IsPaused;
            position = Math.Max(0, Math.Min(duration, at));
            // A finished MP3 cannot be retuned: switch to a new PCM Live graph
            // whenever density differs from the exported setting.
            StopPlayback(false);
            if (active) { StartPlayback(); if (paused && player != null) player.Pause(); }
            status.Text = T("previewSync");
        }
        private void UpdatePlayback(object sender, EventArgs e)
        {
            SmartTick();
            playbackTimer.Interval = Visible && WindowState != FormWindowState.Minimized ? (player!=null&&player.IsPlaying||pendingPlay||vintage!=null&&vintage.Mechanics.Animating?31:100) : 200;
            if (player == null) {meter.SetLevels(-20,-20);RefreshVintage();return;}
            double l, r, sec; bool isPlaying; string error;
            player.Snapshot(out l, out r, out sec, out isPlaying, out error);
            if (isPlaying)
            {
                if (status.Text == T("previewAnalyzing") && sec > .25) status.Text = T("previewPlaying");
                position = Math.Max(0, Math.Min(duration, sec));
                // 0 VU references the configured digital level; use the higher channel.
                meter.SetLevels(player.IsPaused ? -20 : l - prefs.VuReference, player.IsPaused ? -20 : r - prefs.VuReference);
                UpdatePosition();
            }
            else meter.SetLevels(-20, -20);
            if (error != null) { pendingPlay=pendingResume=false;transportState=3;status.Text = T("previewError") + ": " + error; headline=status.Text;Log(status.Text); player.ClearError(); }
            RefreshVintage();
        }
        private void StartJob()
        {
            if (worker.IsBusy) return;
            if (files.Count == 0) { MessageBox.Show(T("select")); return; }
            if (!File.Exists(ffmpeg)) { MessageBox.Show(T("missingFFmpeg")); return; }
            string destination;
            using(FolderBrowserDialog dialog=new FolderBrowserDialog())
            {
                dialog.Description=T("outputFolderPrompt");
                dialog.RootFolder=Environment.SpecialFolder.Desktop;
                dialog.SelectedPath=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                dialog.ShowNewFolderButton=true;
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                destination=dialog.SelectedPath;
            }
            prefs.DenseMode = true; prefs.NormalizeMp3Peaks = false; Save();
            JobOptions job = new JobOptions(); job.Inputs = files.ToArray(); job.OutputFolder = destination;
            job.NormalizeMp3Peaks = false; job.DenseMode = true; job.DenseStrength = CurrentDensity;
            SnapshotJobDensities(job);
            exportedStrength = job.DenseStrength;
            jobFiles.Clear(); jobFiles.AddRange(job.Inputs); jobIndex = -1;
            engine = new ProcessingEngine(ffmpeg, worker, delegate(string key, string details)
            { worker.ReportProgress(0, new string[] { key, details }); });
            SetBusy(true); itemProgress.Value = 0; totalProgress.Value = 0;
            status.Text = T("working"); worker.RunWorkerAsync(job);
        }
        private void Work(object sender, DoWorkEventArgs e)
        { try { JobOptions job=(JobOptions)e.Argument;ResolveJobDensities(job);engine.Convert(job); } catch (OperationCanceledException) { e.Cancel = true; } }
        private bool progressKnown;
        private void WorkProgress(object sender, ProgressChangedEventArgs e)
        {
            string[] lines = e.UserState as string[]; if (lines == null || lines.Length < 2) return;
            string key = lines[0]; string msg = lines[1];
            status.Text = T(key) + ": " + msg; Log(status.Text);
            if (key == "item") { jobIndex++; itemProgress.Value = 0; }
            if(key=="autoSelecting"){itemProgress.Value=0;progressKnown=false;}
            if(key=="stageProgress"){int value;if(int.TryParse(msg,out value)){itemProgress.Value=Math.Max(0,Math.Min(99,value));progressKnown=true;}status.Text=T("working");}
            if(key=="densitySelected"){int value;if(int.TryParse(msg,out value))exportedStrength=value;}
            if(key=="vuResult")Log(msg);
            if (key == "saved")
            {
                itemProgress.Value = 100;
                if (jobIndex >= 0 && jobIndex < jobFiles.Count)
                {
                    outputs[jobFiles[jobIndex]] = msg;
                    outputStrengths[jobFiles[jobIndex]] = exportedStrength;
                }
            }
            if (jobFiles.Count > 0) totalProgress.Value = Math.Min(100, Math.Max(0,
                ((Math.Max(0, jobIndex) * 100) + itemProgress.Value) / jobFiles.Count));
        }
        private void WorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBusy(false);
            if (e.Cancelled) status.Text = T("cancelled");
            else if (e.Error != null) { status.Text = T("failed") + " " + DescribeError(e.Error); MessageBox.Show(status.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else { status.Text = T("complete"); itemProgress.Value = 100; totalProgress.Value = 100; }
            headline=status.Text;Log(status.Text); UpdateTexts();RefreshVintage();
            if(resumeAfterExport&&!closingAfterCancel){resumeAfterExport=false;RequestPlayback();}
            if (closingAfterCancel) { closingAfterCancel = false; Close(); }
        }
        private void SetBusy(bool busy)
        {
            buttonCreate.Enabled = !busy && files.Count > 0;
            buttonCancel.Enabled = busy; buttonCancel.Visible=busy; status.Tag=true; LayoutAll(); buttonAdd.Enabled = !busy; removeFile.Enabled = !busy;
            queue.Enabled = !busy;
        }
        private void CancelJob()
        {
            if (!worker.IsBusy) return;
            worker.CancelAsync(); buttonCancel.Enabled = false;
            if (engine != null) engine.Cancel();
            if(exportPicker!=null)exportPicker.Cancel();
        }
        private void OpenSettings()
        {
            using(StudioCalibrationDialog dialog=new StudioCalibrationDialog(prefs.Language,prefs.VuReference))
            {
                if(dialog.ShowDialog(this)==DialogResult.OK){prefs.VuReference=dialog.Reference;Save();}
            }
        }
        private void ShowLog()
        {
            using (Form dialog = new NoTabDialog())
            {
                dialog.Text = T("log"); dialog.ClientSize = new Size(690, 370);
                TextBox area = new TextBox(); area.Dock = DockStyle.Fill; area.Multiline = true;
                area.ScrollBars = ScrollBars.Both; area.ReadOnly = true; area.WordWrap = false;
                area.Text = logData.Text; dialog.Controls.Add(area); dialog.ShowDialog(this);
            }
        }
        private void Log(string message)
        { if (logData != null) logData.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine); }
        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (worker.IsBusy) { e.Cancel = true; closingAfterCancel = true; CancelJob(); return; }
            CancelAuto();CloseIncomingLeases();
            playbackTimer.Stop(); applyTimer.Stop(); wave.CancelLoad(); StopPlayback(false);
            playbackTimer.Dispose(); applyTimer.Dispose();skin.Dispose();
        }
    }
}
