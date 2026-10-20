using System;using System.Collections.Generic;using System.Diagnostics;using System.Threading;
partial class Host
{
 static volatile bool mediaCancelled;static readonly object mediaProcessLock=new object();static Process mediaProcess;
 static void CheckMediaCancel(){if(mediaCancelled)throw new OperationCanceledException("Отменено");}
 static void WatchMediaCancel()
 {
  Thread watcher=new Thread(delegate(){try{Dictionary<string,object> message;while((message=Read())!=null)if(S(message,"action")=="cancel"){mediaCancelled=true;lock(mediaProcessLock){try{if(mediaProcess!=null&&!mediaProcess.HasExited)mediaProcess.Kill();}catch(InvalidOperationException){}}return;}}catch{}});watcher.IsBackground=true;watcher.Start();
 }
}
