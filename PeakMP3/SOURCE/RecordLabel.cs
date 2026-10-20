using System;using System.IO;using System.Text;using System.Text.RegularExpressions;using System.Globalization;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;
namespace PeakMp3Win7
{
 internal sealed class RecordName
 {
  internal readonly string Artist,Title;
  internal RecordName(string artist,string title){Artist=Clean(artist);Title=Clean(title);}
  internal string Full {get{return Artist.Length==0?Title:Artist+" — "+Title;}}
  internal static string Clean(string text)
  {
   if(string.IsNullOrEmpty(text))return "";if(text.Length>4096)text=text.Substring(0,4096);
   text=Regex.Replace(text,@"&#(x[0-9a-f]+|[0-9]+);",delegate(Match m){try{int n=Convert.ToInt32(m.Groups[1].Value.TrimStart('x','X'),m.Groups[1].Value.StartsWith("x",StringComparison.OrdinalIgnoreCase)?16:10);return n>0&&n<=0x10ffff&&!(n>=0xd800&&n<=0xdfff)?char.ConvertFromUtf32(n):"";}catch{return "";}},RegexOptions.IgnoreCase);
   text=text.Replace("&quot;","\"").Replace("&apos;","'").Replace("&lt;","<").Replace("&gt;",">").Replace("&amp;","&");
   StringBuilder b=new StringBuilder();foreach(char c in text){UnicodeCategory cat=char.GetUnicodeCategory(c);if(char.IsWhiteSpace(c)||char.IsControl(c))b.Append(' ');else if(cat!=UnicodeCategory.Format&&cat!=UnicodeCategory.OtherNotAssigned)b.Append(c);}
   text=Regex.Replace(b.ToString(),@"\s+"," ").Trim();
   // Remove only recognized download annotations. Song parentheses and unknown brackets survive.
   text=Regex.Replace(text,@"\s*\[(?:MP3\s*(?:320|256|192|128)(?:\s*k(?:b(?:it)?/?s)?)?|AAC\s*(?:original|исходный)?|(?:144|240|360|480|720|1080|1440|2160)p(?:\s*(?:H\.?264|AVC|VP9|AV1|H\.?265|HEVC|HDR|60fps))*|PeakMP3\s*[-+]?\d+)\]", "",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
   return Regex.Replace(text,@"\s+"," ").Trim();
  }
  internal static RecordName Resolve(string path,string artist,string title)
  {
   string stem=Clean(Path.GetFileNameWithoutExtension(path));artist=Clean(artist);title=Clean(title);
   if(Regex.IsMatch(artist,@"^(unknown(?: artist)?|неизвест(?:ный исполнитель|но)|unbekannt(?:er interpret)?)$",RegexOptions.IgnoreCase))artist="";
   if(title.Length>0&&!Regex.IsMatch(title,@"^(?:https?://|www\.)",RegexOptions.IgnoreCase))return new RecordName(artist,title);
   Match split=Regex.Match(stem,@"^(.{1,160}?)\s+[-–—]\s+(.+)$");
   if(split.Success){string first=split.Groups[1].Value;if(!Regex.IsMatch(first,@"^\d{1,3}$")){if(artist.Length==0)artist=first;stem=split.Groups[2].Value;}}
   if(artist.Length>0&&stem.StartsWith(artist+" - ",StringComparison.OrdinalIgnoreCase))stem=stem.Substring(artist.Length+3);
   return new RecordName(artist,stem.Length==0?"PeakMP3":stem);
  }
  internal static RecordName FromDiagnostic(string path,string diagnostic)
  {
   int end=diagnostic.IndexOf("Duration:",StringComparison.Ordinal);string root=end<0?"":diagnostic.Substring(0,end);string artist="",albumArtist="",title="";
   foreach(Match m in Regex.Matches(root,@"^\s*(artist|album_artist|title)\s*:\s*(.*?)\r?$",RegexOptions.Multiline|RegexOptions.IgnoreCase)){string key=m.Groups[1].Value.ToLowerInvariant(),v=m.Groups[2].Value;if(key=="artist"&&artist.Length==0)artist=v;else if(key=="title"&&title.Length==0)title=v;else if(key=="album_artist")albumArtist=v;}
   return Resolve(path,artist.Length>0?artist:albumArtist,title);
  }
 }
 internal static class RecordLabel
 {
  internal static string Shorten(string s,int count){StringInfo elements=new StringInfo(s);return elements.LengthInTextElements<=count?s:elements.SubstringByTextElements(0,Math.Max(0,count-1))+"…";}
  internal static int Variant(RecordName name)
  {using(Bitmap b=new Bitmap(1,1))using(Graphics g=Graphics.FromImage(b))using(Font title=new Font("Tahoma",20,FontStyle.Bold,GraphicsUnit.Pixel))using(Font artist=new Font("Tahoma",30,FontStyle.Bold,GraphicsUnit.Pixel))return g.MeasureString(name.Title,title).Width<=390&&g.MeasureString(name.Artist,artist).Width<=210?2:3;}
  private static void Text(Graphics g,string s,RectangleF r,float size,bool bold)
  {using(Font f=new Font("Tahoma",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(Brush ink=new SolidBrush(Color.FromArgb(48,27,12)))using(StringFormat format=new StringFormat()){format.Alignment=StringAlignment.Center;format.LineAlignment=StringAlignment.Center;format.Trimming=StringTrimming.EllipsisWord;g.DrawString(s,f,ink,r,format);}}
  internal static Bitmap Create(Bitmap texture,RecordName name)
  {
   Bitmap result=new Bitmap(256,256,PixelFormat.Format32bppPArgb);using(Graphics g=Graphics.FromImage(result))
   {
    g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    using(Brush orange=new SolidBrush(Color.FromArgb(245,137,35)))g.FillEllipse(orange,1,1,254,254);
    // A plain paper label, without concentric cut/groove lines or rotating highlights.
    if(Variant(name)==2)
    {Text(g,name.Artist.Length==0?"PeakMP3":Shorten(name.Artist,40),new RectangleF(29,37,198,46),30,true);Text(g,Shorten(name.Title,70),new RectangleF(28,150,200,60),20,true);Text(g,"PeakMP3",new RectangleF(43,218,170,20),14,false);}
    else
    {
     const string brand="PeakMP3";for(int i=0;i<brand.Length;i++){GraphicsState state=g.Save();g.TranslateTransform(128,128);g.RotateTransform(-51+i*17);g.TranslateTransform(0,-86);Text(g,brand[i].ToString(),new RectangleF(-17,-20,34,40),31,true);g.Restore(state);}
     Text(g,Shorten(name.Artist,26),new RectangleF(34,151,188,26),20,true);Text(g,Shorten(name.Title,64),new RectangleF(34,179,188,49),18,false);
    }
   }return result;
  }
 }
}
