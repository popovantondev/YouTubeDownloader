// Windows Script Host JScript. No administrative access required.
var sh=WScript.CreateObject('WScript.Shell');
var fso=WScript.CreateObject('Scripting.FileSystemObject');
var root=sh.ExpandEnvironmentStrings('%LOCALAPPDATA%')+'\\YTWin7_360_MP3';
var exe=root+'\\YTWin7SabrHost.exe';
var dest=root+'\\com.ytwin7.sabr.json';
function esc(s){var out='';for(var n=0;n<s.length;n++){var c=s.charAt(n),code=s.charCodeAt(n);if(c==='\\'||c==='"')out+='\\'+c;else if(code<32||code>126)out+='\\u'+('0000'+code.toString(16)).slice(-4);else out+=c;}return out;}

var id='eejgbkimfjihhfjmemjgmdjdpmipiohm';
var f=fso.CreateTextFile(dest,true,false);
f.Write('{"name":"com.ytwin7.sabr","description":"SABR local media assembly","path":"'+esc(exe)+'","type":"stdio","allowed_origins":["chrome-extension://'+id+'/"]}');
f.Close();
var paths=['Microsoft\\Edge','Google\\Chrome','Chromium','Yandex\\YandexBrowser','Yandex\\YandexBrowserStable'];
var testPrefix=sh.Environment('PROCESS')('YTD_INSTALL_TEST_REGISTRY');
if(testPrefix&&!/^Software\\popovantondev\\YouTubeDownloader\\Tests\\[A-Za-z0-9_-]+\\$/.test(testPrefix))throw Error('Invalid isolated test registry.');
for(var i=0;i<paths.length;i++){
 try{sh.RegWrite('HKCU\\'+(testPrefix||'Software\\')+paths[i]+'\\NativeMessagingHosts\\com.ytwin7.sabr\\',dest,'REG_SZ');}catch(e){}
}
WScript.Echo('SABR native host registered for extension '+id);
