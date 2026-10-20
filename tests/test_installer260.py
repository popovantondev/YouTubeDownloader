"""Isolated current-user install/upgrade/rollback/uninstall checks on Windows."""
from pathlib import Path
import os,sys,subprocess,shutil,hashlib,json,uuid,winreg
R=Path(__file__).resolve().parents[1]
scope='Install_'+uuid.uuid4().hex
T=R/'.test-artifacts'/scope;local=T/'Профиль';base=local/'YTWin7_360_MP3';data=local/'YTWin7_WatchCapture'
prefix='Software\\popovantondev\\YouTubeDownloader\\Tests\\'+scope+'\\'
env=dict(os.environ,LOCALAPPDATA=str(local),YTD_INSTALL_TEST_REGISTRY=prefix)
def run(args,ok=True):
 p=subprocess.run([str(x) for x in args],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=90)
 if (p.returncode==0)!=ok:raise AssertionError(str(args)+'\n'+p.stdout.decode(errors='replace'))
 return p
def script(folder,mode,ok=True):return run([folder/'InstallerEngine.exe',mode,'en'],ok)
def check(value,label):
 if not value:raise AssertionError(label)
 print('PASS:',label,flush=True)
def hash(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def regvalue(path):
 try:
  with winreg.OpenKey(winreg.HKEY_CURRENT_USER,prefix+path+'\\NativeMessagingHosts\\com.ytwin7.sabr') as k:return winreg.QueryValueEx(k,'')[0]
 except FileNotFoundError:return None
def remove_test_key(path):
 try:
  with winreg.OpenKey(winreg.HKEY_CURRENT_USER,path,0,winreg.KEY_READ|winreg.KEY_WRITE) as k:
   while True:
    try:child=winreg.EnumKey(k,0)
    except OSError:break
    remove_test_key(path+'\\'+child)
  winreg.DeleteKey(winreg.HKEY_CURRENT_USER,path)
 except FileNotFoundError:pass
try:
 base.mkdir(parents=True);data.mkdir();(base/'user-file.keep').write_text('retained');(data/'cache.keep').write_text('retained')
 script(R/'.payload','install')
 check((base/'PeakMP3/PeakMP3.exe').exists(),'clean per-user script install includes PeakMP3')
 check(all(regvalue(p)==str(base/'com.ytwin7.sabr.json') for p in ['Microsoft\\Edge','Google\\Chrome','Chromium','Yandex\\YandexBrowser','Yandex\\YandexBrowserStable']),'all five isolated browser registrations point at the Unicode install path')
 manifest=json.loads((base/'com.ytwin7.sabr.json').read_text(encoding='utf-8'));check(manifest['allowed_origins']==['chrome-extension://eejgbkimfjihhfjmemjgmdjdpmipiohm/'],'legacy extension ID is preserved in native manifest')
 # Force a failure after copying, not a preflight rejection. Rollback must restore bytes/keys.
 before=hash(base/'YTWin7SabrHost.exe');bad=T/'bad-payload';shutil.copytree(R/'.payload',bad)
 with (bad/'ffprobe.exe').open('ab') as f:f.write(b'TAMPER-CHECK')
 script(bad,'install',False)
 check(hash(base/'YTWin7SabrHost.exe')==before,'tampered payload is rejected before installed files change')
 shutil.copyfile(R/'.payload/ffprobe.exe',bad/'ffprobe.exe')
 env['YTD_INSTALL_TEST_FAIL']='after-copy'
 script(bad,'install',False)
 env.pop('YTD_INSTALL_TEST_FAIL')
 check(hash(base/'YTWin7SabrHost.exe')==before,'post-copy registration failure restores previous program bytes')
 check(regvalue('Microsoft\\Edge')==str(base/'com.ytwin7.sabr.json'),'rollback restores prior native registration')
 script(base,'uninstall-app')
 check(not (base/'PeakMP3/PeakMP3.exe').exists() and not (base/'com.ytwin7.sabr.json').exists(),'script uninstall removes owned runtime and generated native manifest')
 check((base/'user-file.keep').exists() and (data/'cache.keep').exists(),'uninstall retains user files and cache')
 # Optional actual baseline, provided explicitly; never use the user's installed folder.
 if len(sys.argv)>1:
  old=Path(sys.argv[1]).resolve()
  for name in ['YTWin7SabrHost.exe','YTWin7SabrHost.exe.config','ffmpeg.exe','ffprobe.exe']:
   shutil.copyfile(old/name,base/name)
  (base/'extension').mkdir(exist_ok=True);shutil.copyfile(old/'extension/manifest.json',base/'extension/manifest.json')
  # Remove only this test's first rollback backup so the baseline snapshot is unambiguous.
  backup=data/'BACKUP_PRE_260'
  assert backup.resolve().parent==data.resolve()
  if backup.exists():shutil.rmtree(backup)
  original=hash(base/'YTWin7SabrHost.exe');script(R/'.payload','install');check(hash(base/'YTWin7SabrHost.exe')!=original,'actual 2.5.0 host is upgraded')
  script(base,'restore');check(hash(base/'YTWin7SabrHost.exe')==original,'explicit restore returns exact prior 2.5.0 host bytes')
 setup=R/'dist/YouTubeDownloader-PeakMP3-2.6.0-TEST-Setup.exe'
 log=T/'inno-install.log'
 prior=hash(base/'YTWin7SabrHost.exe')
 env['YTD_INSTALL_TEST_FAIL']='after-copy'
 run([setup,'/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/LANG=en','/DIR='+str(base),'/LOG='+str(T/'inno-forced-failure.log')],False)
 env.pop('YTD_INSTALL_TEST_FAIL')
 check(hash(base/'YTWin7SabrHost.exe')==prior and (base/'user-file.keep').exists(),'compiled Setup failure restores prior bytes and preserves user files')
 run([setup,'/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/LANG=en','/DIR='+str(base),'/LOG='+str(log)])
 check((base/'unins000.exe').exists() and (base/'BrowserSetup.exe').exists(),'compiled Setup.exe completes in isolated per-user target')
 run([base/'unins000.exe','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/LOG='+str(T/'inno-uninstall.log')])
 check(not (base/'PeakMP3/PeakMP3.exe').exists() and regvalue('Microsoft\\Edge') is None,'compiled uninstaller removes program and owned browser registration')
 check((base/'user-file.keep').exists() and (data/'cache.keep').exists(),'compiled uninstaller retains unknown user file and cache')
 print('ALL_ISOLATED_INSTALLER_CHECKS_PASSED',flush=True)
finally:remove_test_key(prefix.rstrip('\\'))
