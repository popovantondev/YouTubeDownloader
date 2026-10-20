// Unicode-safe current-user registration and exact rollback, including on non-English profiles.
using System;using System.IO;using System.Text;using System.Text.RegularExpressions;using Microsoft.Win32;
internal static class IntegrationRegistration
{
 private static readonly string[] browsers={"Microsoft\\Edge","Google\\Chrome","Chromium","Yandex\\YandexBrowser","Yandex\\YandexBrowserStable"};
 internal static int Run(string[] args)
 {
  try{
   if(args.Length!=5)throw new ArgumentException("Invalid integration arguments.");
   string mode=args[1],root=Path.GetFullPath(args[2]).TrimEnd('\\'),snapshot=args[3],prefix=args[4];
   if(!String.Equals(Path.GetFileName(root),"YTWin7_360_MP3",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Invalid install root.");
   if(prefix.Length>0&&!Regex.IsMatch(prefix,@"^Software\\popovantondev\\YouTubeDownloader\\Tests\\[A-Za-z0-9_-]+\\$"))throw new ArgumentException("Invalid test registry namespace.");
   if(prefix.Length==0)prefix="Software\\";
   string manifest=Path.Combine(root,"com.ytwin7.sabr.json");string[] saved=mode=="restore"?File.ReadAllLines(snapshot,Encoding.UTF8):new string[5];
   if(saved.Length!=5)throw new InvalidDataException("Incomplete registration snapshot.");
   for(int i=0;i<browsers.Length;i++){
    string key=prefix+browsers[i]+"\\NativeMessagingHosts\\com.ytwin7.sabr";
    using(RegistryKey current=Registry.CurrentUser.OpenSubKey(key)){
     object old=current==null?null:current.GetValue("",null,RegistryValueOptions.DoNotExpandEnvironmentNames);
     if(mode=="snapshot"){saved[i]=old==null?"-":Convert.ToBase64String(Encoding.UTF8.GetBytes(Convert.ToString(old)));continue;}
     if(mode=="verify"){if(!String.Equals(Convert.ToString(old),manifest,StringComparison.OrdinalIgnoreCase))throw new IOException("Native registration does not match the install path.");continue;}
     if(mode=="remove"&&(old==null||!String.Equals(Convert.ToString(old),manifest,StringComparison.OrdinalIgnoreCase)))continue;
    }
    if(mode=="remove"||mode=="restore"&&saved[i]=="-")Registry.CurrentUser.DeleteSubKey(key,false);
    else if(mode=="register"||mode=="restore")using(RegistryKey target=Registry.CurrentUser.CreateSubKey(key)){target.SetValue("",mode=="register"?manifest:Encoding.UTF8.GetString(Convert.FromBase64String(saved[i])),RegistryValueKind.String);}
    else throw new ArgumentException("Unknown integration operation.");
   }
   if(mode=="snapshot")File.WriteAllLines(snapshot,saved,Encoding.UTF8);
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
 }
}
