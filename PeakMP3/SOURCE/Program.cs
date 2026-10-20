// PeakMP3 0.16 — vintage bitmap studio panel over the existing audio engine.
using System;
using System.Windows.Forms;
[assembly:System.Reflection.AssemblyTitle("PeakMP3")]
[assembly:System.Reflection.AssemblyVersion("0.16.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("0.16.0.0")]
namespace PeakMp3Win7
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string job=null,language=null;
            try{
             for(int i=0;i<args.Length;i++){
              if(args[i]=="--version"){Console.WriteLine("PeakMP3 0.16.0; x86 CLR2 / .NET3.5");return 0;}
              if(args[i]=="--youtube-job"&&i+1<args.Length){job=args[++i];continue;}
              if(args[i]=="--language"&&i+1<args.Length){language=args[++i];continue;}
              throw new ArgumentException("Invalid PeakMP3 command line.");
             }
             Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
             using(DesktopInstance instance=new DesktopInstance()){
              if(!instance.Owner){instance.Forward(job);return 0;}
              using(ApprovedWindow window=new ApprovedWindow()){
               if(!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PeakMp3Win7"),"settings.ini"))){string first=language??System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;window.SetFirstLanguage(first);}
               window.Shown+=delegate{instance.Listen(window);if(job!=null)try{window.AcceptYouTubeJob(job);}catch(Exception e){MessageBox.Show(e.Message,"PeakMP3",MessageBoxButtons.OK,MessageBoxIcon.Error);}};
               Application.Run(window);
              }
             }return 0;
            }catch(Exception e){MessageBox.Show(e.Message,"PeakMP3",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
        }
    }
}
