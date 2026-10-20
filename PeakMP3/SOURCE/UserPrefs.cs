using System;
using System.IO;
using System.Text;

namespace PeakMp3Win7
{
    internal sealed class UserPrefs
    {
        internal static string TestProfile = null;
        public bool NormalizeMp3Peaks;
        public bool DenseMode;
        public int DenseStrength;
        public int ExtendedDensity=7;
        public int Language;
        public int VuReference;
        public int PreviewVolume=100;
        public string OutputFolder { get { return DesktopOutputFolder; } }
        internal static string DesktopOutputFolder
        {get {return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);}}

        public UserPrefs()
        {
            NormalizeMp3Peaks = false;
            DenseMode = true;
            DenseStrength = 7;
            Language = 0;
            VuReference = 0;

        }

        private static string ConfigFile
        {
            get
            {
                string home = TestProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(Path.Combine(home, "PeakMp3Win7"), "settings.ini");
            }
        }

        public static UserPrefs Load()
        {
            UserPrefs prefs = new UserPrefs();
            bool hasNewMeterProfile = false,hasExtended=false;
            try
            {
                if (!File.Exists(ConfigFile)) return prefs;
                foreach (string line in File.ReadAllLines(ConfigFile, Encoding.UTF8))
                {
                    int equal = line.IndexOf('=');
                    if (equal < 0) continue;
                    string key = line.Substring(0, equal).Trim();
                    string val = line.Substring(equal + 1).Trim();
                    if (key == "meterProfileV2") hasNewMeterProfile = val == "true";
                    else if (key == "normalizeMp3Peaks") prefs.NormalizeMp3Peaks = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                    else if (key == "denseMode") prefs.DenseMode = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                    else if (key == "denseStrength")
                    {
                        int n;
                        if (int.TryParse(val, out n)) prefs.DenseStrength = Math.Max(0, Math.Min(10, n));
                    }
                    else if(key=="extendedDensity"){int n;if(int.TryParse(val,out n)){prefs.ExtendedDensity=n;hasExtended=true;}}
                    else if (key == "previewVolume")
                    {int n;if(int.TryParse(val,out n))prefs.PreviewVolume=Math.Max(0,Math.Min(100,n));}
                    else if (key == "vuReference")
                    {
                        int db;
                        if (int.TryParse(val, out db)) prefs.VuReference = Math.Max(-24, Math.Min(3, db));
                    }
                    else if (key == "language")
                    {
                        if (val == "de") prefs.Language = 1;
                        else if (val == "en") prefs.Language = 2;
                        else prefs.Language = 0;
                    }
                    // Historical outputFolder is intentionally ignored: desktop is fixed.
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            if(!hasExtended)prefs.ExtendedDensity=prefs.DenseStrength;
            if (!hasNewMeterProfile) prefs.VuReference = 0; // migrate old default -9 once
            if (prefs.DenseMode && prefs.NormalizeMp3Peaks) prefs.NormalizeMp3Peaks = false;
            return prefs;
        }

        public void Save()
        {
            string file = ConfigFile;
            string dir = Path.GetDirectoryName(file);
            Directory.CreateDirectory(dir);
            string tmp = file + ".new";
            StringBuilder content = new StringBuilder();
            content.AppendLine("normalizeMp3Peaks=" + (NormalizeMp3Peaks ? "true" : "false"));
            content.AppendLine("denseMode=" + (DenseMode ? "true" : "false"));
            content.AppendLine("denseStrength=" + DenseStrength);
            content.AppendLine("extendedDensity="+ExtendedDensity);
            content.AppendLine("vuReference=" + VuReference);
            content.AppendLine("previewVolume=" + PreviewVolume);
            content.AppendLine("meterProfileV2=true");
            content.AppendLine("language=" + (Language == 1 ? "de" : Language == 2 ? "en" : "ru"));

            File.WriteAllText(tmp, content.ToString(), Encoding.UTF8);
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        }
    }
}
