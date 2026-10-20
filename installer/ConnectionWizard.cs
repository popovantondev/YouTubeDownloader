using System;using System.IO;using System.Drawing;using System.Diagnostics;using System.Collections.Generic;using System.Globalization;using System.Windows.Forms;using Microsoft.Win32;
[assembly:System.Reflection.AssemblyTitle("YouTube Downloader Browser Setup")]
[assembly:System.Reflection.AssemblyVersion("2.6.0.0")]
internal sealed class ConnectionWizard : Form
{
 private readonly string root=AppDomain.CurrentDomain.BaseDirectory;private readonly ComboBox languages=new ComboBox(),browsers=new ComboBox();private readonly Label intro=new Label(),steps=new Label(),state=new Label();private readonly TextBox folder=new TextBox();private readonly Button open=new Button(),copy=new Button(),peak=new Button(),check=new Button();private readonly List<Browser> found=new List<Browser>();
 #if TESTING
 protected override bool ShowWithoutActivation{get{return true;}}
 #endif
 private int lang;
 private sealed class Browser{internal string Name,Exe,Url;public override string ToString(){return Name;}}
 private string T(string ru,string de,string en){return lang==0?ru:lang==1?de:en;}
 private static string RegistryPath(string name)
 {foreach(RegistryKey hive in new RegistryKey[]{Registry.CurrentUser,Registry.LocalMachine})foreach(string prefix in new string[]{"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\","Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\App Paths\\"})try{using(RegistryKey key=hive.OpenSubKey(prefix+name)){if(key!=null){string path=Convert.ToString(key.GetValue("")).Trim('"');if(File.Exists(path))return path;}}}catch{}return null;}
 private void AddBrowser(string name,string file,string url,string[] candidates)
 {string exe=RegistryPath(file);if(exe==null)foreach(string path in candidates)if(File.Exists(path)){exe=path;break;}if(exe!=null)found.Add(new Browser{Name=name,Exe=exe,Url=url});}
 internal ConnectionWizard(string language)
 {
  lang=language=="ru"?0:language=="de"?1:2;Text="YouTube Downloader + PeakMP3";StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(650,500);MinimumSize=MaximumSize=Size;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;Font=new Font("Tahoma",10);BackColor=Color.FromArgb(245,249,246);
  try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  Panel header=new Panel();header.SetBounds(0,0,650,82);header.BackColor=Color.FromArgb(20,123,69);Controls.Add(header);
  PictureBox icon=new PictureBox();icon.SetBounds(20,14,54,54);icon.SizeMode=PictureBoxSizeMode.Zoom;try{icon.Image=Image.FromFile(Path.Combine(root,"extension\\icons\\icon128.png"));}catch{}header.Controls.Add(icon);
  Label title=new Label();title.Text="YouTube Downloader + PeakMP3";title.SetBounds(90,24,540,40);title.Font=new Font("Tahoma",16,FontStyle.Bold);title.ForeColor=Color.White;header.Controls.Add(title);
  languages.DropDownStyle=ComboBoxStyle.DropDownList;languages.Items.AddRange(new object[]{"Русский","Deutsch","English"});languages.SelectedIndex=lang;languages.SetBounds(462,97,164,29);languages.SelectedIndexChanged+=delegate{lang=languages.SelectedIndex;Render();};Controls.Add(languages);
  intro.SetBounds(24,96,418,54);Controls.Add(intro);browsers.DropDownStyle=ComboBoxStyle.DropDownList;browsers.SetBounds(24,158,240,30);Controls.Add(browsers);
  string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),pf=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),pf64=Environment.GetEnvironmentVariable("ProgramW6432")??pf;
  AddBrowser("Google Chrome","chrome.exe","chrome://extensions/",new string[]{Path.Combine(local,"Google\\Chrome\\Application\\chrome.exe"),Path.Combine(pf,"Google\\Chrome\\Application\\chrome.exe"),Path.Combine(pf64,"Google\\Chrome\\Application\\chrome.exe")});
  AddBrowser("Microsoft Edge","msedge.exe","edge://extensions/",new string[]{Path.Combine(pf,"Microsoft\\Edge\\Application\\msedge.exe"),Path.Combine(pf64,"Microsoft\\Edge\\Application\\msedge.exe")});
  AddBrowser("Yandex Browser","browser.exe","browser://extensions/",new string[]{Path.Combine(local,"Yandex\\YandexBrowser\\Application\\browser.exe")});foreach(Browser browser in found)browsers.Items.Add(browser);if(found.Count>0)browsers.SelectedIndex=0;
  open.SetBounds(280,157,346,33);open.Click+=delegate{if(browsers.SelectedIndex>=0){Browser b=found[browsers.SelectedIndex];Process.Start(new ProcessStartInfo(b.Exe,b.Url){UseShellExecute=true});}};Controls.Add(open);
  steps.SetBounds(24,207,602,92);Controls.Add(steps);folder.ReadOnly=true;folder.Text=Path.Combine(root,"extension");folder.SetBounds(24,304,602,30);Controls.Add(folder);
  copy.SetBounds(24,347,290,35);copy.Click+=delegate{Clipboard.SetText(folder.Text);};Controls.Add(copy);
  check.SetBounds(332,347,294,35);check.Click+=delegate{Check();};Controls.Add(check);
  state.SetBounds(24,393,602,48);state.ForeColor=Color.FromArgb(18,91,47);Controls.Add(state);
  peak.SetBounds(24,445,602,34);peak.Click+=delegate{string exe=Path.Combine(root,"PeakMP3\\PeakMP3.exe");if(File.Exists(exe))Process.Start(exe);};Controls.Add(peak);Render();Check();
 }
 private void Render()
 {
  intro.Text=T("Программы установлены. Осталось один раз подключить расширение в нужном профиле браузера.","Programme installiert. Die Erweiterung einmal im gewünschten Browserprofil verbinden.","Applications installed. Connect the extension once in the desired browser profile.");
  open.Text=T("Открыть расширения браузера","Browser-Erweiterungen öffnen","Open browser extensions");open.Enabled=found.Count>0;
  steps.Text=T("1. Откройте страницу расширений выбранного браузера.\n2. Включите «Режим разработчика».\n3. Нажмите «Загрузить распакованное» и выберите папку ниже.\n4. После обновления перезагрузите расширение. Закрепите стрелку и обновите YouTube.","1. Erweiterungsseite des Browsers öffnen.\n2. Entwicklermodus einschalten.\n3. «Entpackte Erweiterung laden» wählen, Ordner unten auswählen.\n4. Nach einem Update Erweiterung neu laden. Grünen Pfeil anheften, YouTube neu laden.","1. Open the selected browser's extensions page.\n2. Enable Developer mode.\n3. Choose Load unpacked and select the folder below.\n4. After an update, reload the extension. Pin the arrow and reload YouTube.");
  copy.Text=T("Скопировать путь к расширению","Erweiterungspfad kopieren","Copy extension folder");check.Text=T("Проверить локальную установку","Lokale Installation prüfen","Check local installation");peak.Text=T("Открыть PeakMP3","PeakMP3 öffnen","Open PeakMP3");
 }
 private void Check()
 {
  bool files=File.Exists(Path.Combine(root,"YTWin7SabrHost.exe"))&&File.Exists(Path.Combine(root,"ffmpeg.exe"))&&File.Exists(Path.Combine(root,"PeakMP3\\PeakMP3.exe"));bool registration=false;
  foreach(string path in new string[]{"Microsoft\\Edge","Google\\Chrome","Yandex\\YandexBrowser","Yandex\\YandexBrowserStable"})try{using(RegistryKey key=Registry.CurrentUser.OpenSubKey("Software\\"+path+"\\NativeMessagingHosts\\com.ytwin7.sabr")){registration|=key!=null&&string.Equals(Convert.ToString(key.GetValue("")),Path.Combine(root,"com.ytwin7.sabr.json"),StringComparison.OrdinalIgnoreCase);}}catch{}
  state.Text=files&&registration?T("Локальные компоненты готовы. Связь с конкретным профилем проверяется зелёной стрелкой в браузере.","Lokale Komponenten bereit. Verbindung im jeweiligen Profil über den grünen Pfeil prüfen.","Local components are ready. Check the connection in each profile using the green browser arrow."):T("Установка неполная. Повторно запустите общий установщик.","Installation unvollständig. Gemeinsames Setup erneut starten.","Installation is incomplete. Run the combined installer again.");
 }
 [STAThread]private static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string lang=args.Length==1?args[0]:CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;Application.Run(new ConnectionWizard(lang));}
}
