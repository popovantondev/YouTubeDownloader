using System;using System.IO;using System.Diagnostics;using System.Collections.Generic;using System.Threading;using System.Text.RegularExpressions;
partial class Host
{
 static string PeakExecutable(){return Path.Combine(Path.Combine(root,"PeakMP3"),"PeakMP3.exe");}
 static bool DesktopCommand(Dictionary<string,object> m)
 {
  string action=S(m,"action");if(action!="desktopStatus"&&action!="openDesktop")return false;string peak=PeakExecutable();bool installed=File.Exists(peak)&&File.Exists(peak+".config");
  if(action=="desktopStatus"){Send(new {action="result",ok=true,installed=installed,hostVersion="2.6.0",peakVersion=installed?FileVersionInfo.GetVersionInfo(peak).FileVersion:"",protocolVersion=1});return true;}
  if(!installed)throw new Exception("PeakMP3 не установлен. Запустите общий установщик.");
  LaunchPeak(null,S(m,"language"));Send(new {action="result",ok=true,opened=true,message="PeakMP3 открыт."});return true;
 }
 static Process LaunchPeak(string job,string language)
 {
  string peak=PeakExecutable();if(!File.Exists(peak))throw new Exception("PeakMP3 не установлен. Запустите общий установщик.");
  string args=job==null?"":"--youtube-job "+Q(job);if(Regex.IsMatch(language,@"^(ru|de|en)$"))args+=" --language "+language;
  ProcessStartInfo info=new ProcessStartInfo(peak,args);info.WorkingDirectory=Path.GetDirectoryName(peak);info.UseShellExecute=false;return Process.Start(info);
 }
 static void HandOffPeak(Dictionary<string,object> m)
 {
  Identity(m);if(!HasAudio()||!Regex.IsMatch(Run("-i "+Q(Audio()),false),@"Audio:\s*aac\b"))throw new Exception("Для PeakMP3 нужен исходный AAC. Загрузите AAC ещё раз.");
  if(!File.Exists(PeakExecutable()))throw new Exception("PeakMP3 не установлен. Запустите общий установщик.");
  Status(87,"Проверяю AAC перед передачей в PeakMP3…");Verify(Audio(),false);Run("-loglevel error -xerror -i "+Q(Audio())+" -map 0:a:0 -vn -c:a pcm_f32le -f null -",true);
  string name=S(m,"displayTitle");if(name.Length==0)name=title;
  CheckMediaCancel();string manifest=DesktopHandoff.Create(data,Audio(),id,name,S(m,"language"));Status(96,"Открываю AAC в PeakMP3…");
  using(Process process=LaunchPeak(manifest,S(m,"language")))
  {Stopwatch clock=Stopwatch.StartNew();string accepted=Path.Combine(Path.GetDirectoryName(manifest),"accepted");
   while(!File.Exists(accepted)&&clock.ElapsedMilliseconds<30000){if(process.HasExited&&process.ExitCode!=0)throw new Exception("PeakMP3 не смог открыть AAC. Проверьте установку.");Thread.Sleep(80);}
   if(!File.Exists(accepted))throw new Exception("PeakMP3 не подтвердил приём. AAC сохранён для повторной попытки.");
  }
  Send(new {action="result",ok=true,handedOff=true,jobId=Path.GetFileName(Path.GetDirectoryName(manifest)),progress=100,message="AAC передан в PeakMP3. Проверьте плотность и сохраните MP3 в программе."});
 }
}
