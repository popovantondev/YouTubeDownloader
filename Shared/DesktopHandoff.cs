// Local current-user handoff. No network requests or audio processing occur here.
using System;using System.IO;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;using System.Security.Cryptography;
internal sealed class YouTubeJob
{
 internal string JobId,VideoId,Title,Language,Source,DirectoryPath,Manifest,Hash;
}
internal static class DesktopHandoff
{
 internal static string DataRoot {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YTWin7_WatchCapture");}}
 internal static string Inbox(string data){return Path.Combine(data,"PEAKMP3_INBOX");}
 internal static string Hash(string path){using(SHA256 h=SHA256.Create())using(FileStream f=File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
 private static string Value(Dictionary<string,object> d,string key){object v;return d.TryGetValue(key,out v)?Convert.ToString(v):"";}
 private static string SafeLeaf(string value)
 {foreach(char c in Path.GetInvalidFileNameChars())value=value.Replace(c,'_');value=Regex.Replace(value,@"\s+"," ").Trim(' ','.');if(value.Length>64)value=value.Substring(0,64).TrimEnd();if(value.Length==0||Regex.IsMatch(value,@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)",RegexOptions.IgnoreCase))value="YouTube-"+value;return value;}
 internal static string Create(string data,string source,string videoId,string title,string language)
 {
  if(!Regex.IsMatch(videoId,@"^[A-Za-z0-9_-]{11}$"))throw new InvalidDataException("Invalid video identifier.");
  string id=Guid.NewGuid().ToString("N"),folder=Path.Combine(Inbox(data),id);Directory.CreateDirectory(folder);
  string leaf=SafeLeaf(title),path=Path.Combine(folder,leaf+".m4a");if(path.Length>240){int available=240-folder.Length-6;if(available<12)throw new IOException("The user profile path is too long.");leaf=leaf.Substring(0,Math.Min(leaf.Length,available));path=Path.Combine(folder,leaf+".m4a");}
  try{File.Copy(source,path,false);string manifest=Path.Combine(folder,"job.json"),pending=manifest+".part";MiniJson json=new MiniJson();
   File.WriteAllText(pending,json.Serialize(new {schema=1,jobId=id,videoId=videoId,title=title.Length>512?title.Substring(0,512):title,language=Regex.IsMatch(language,@"^(ru|de|en)$")?language:"en",source=Path.GetFileName(path),sha256=Hash(path)}),new UTF8Encoding(false));File.Move(pending,manifest);return manifest;
  }catch{foreach(string f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder);throw;}
 }
 internal static YouTubeJob Load(string manifest,string data)
 {
  string inbox=Path.GetFullPath(Inbox(data)).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  manifest=Path.GetFullPath(manifest);string folder=Path.GetDirectoryName(manifest),id=Path.GetFileName(folder);
  if(!manifest.StartsWith(inbox,StringComparison.OrdinalIgnoreCase)||!String.Equals(Path.GetDirectoryName(folder)+Path.DirectorySeparatorChar,inbox,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(manifest)!="job.json"||!Regex.IsMatch(id,@"^[a-f0-9]{32}$"))throw new InvalidDataException("The YouTube job is outside the private inbox.");
  if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0||new FileInfo(manifest).Length>16384)throw new InvalidDataException("Invalid job directory or size.");
  Dictionary<string,object> d=new MiniJson().Deserialize(File.ReadAllText(manifest,Encoding.UTF8));string leaf=Value(d,"source");
  if(Value(d,"schema")!="1"||Value(d,"jobId")!=id||!Regex.IsMatch(Value(d,"videoId"),@"^[A-Za-z0-9_-]{11}$")||leaf.Length==0||leaf.IndexOfAny(new char[]{'\\','/',':'})>=0||!leaf.EndsWith(".m4a",StringComparison.OrdinalIgnoreCase)||leaf=="..")throw new InvalidDataException("Invalid YouTube job data.");
  string source=Path.Combine(folder,leaf);if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0||!File.Exists(source)||Hash(source)!=Value(d,"sha256"))throw new InvalidDataException("The downloaded AAC is missing or changed.");
  YouTubeJob job=new YouTubeJob();job.JobId=id;job.VideoId=Value(d,"videoId");job.Title=Value(d,"title");job.Language=Value(d,"language");job.Source=source;job.DirectoryPath=folder;job.Manifest=manifest;job.Hash=Value(d,"sha256");return job;
 }
 internal static void Accept(YouTubeJob job){File.WriteAllText(Path.Combine(job.DirectoryPath,"accepted"),job.JobId,new UTF8Encoding(false));}
}
