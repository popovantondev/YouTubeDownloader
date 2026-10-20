"""Native peak command launches the actual desktop import code in an isolated harness."""
from pathlib import Path
import os,shutil,uuid,json,time,base64
from test_desktop_media import Native,identity,check,root
t=root/'.test-artifacts'/('bridge-'+uuid.uuid4().hex);runtime=t/'runtime';profile=t/'profile';data=profile/'IntegrationData'
runtime.mkdir(parents=True);data.mkdir(parents=True)
for name in ['YTWin7SabrHost.exe','YTWin7SabrHost.exe.config','ffmpeg.exe','ffprobe.exe']:shutil.copyfile(root/name,runtime/name)
peak=runtime/'PeakMP3';peak.mkdir();shutil.copyfile(root/'PeakMP3/BridgeHarness.exe',peak/'PeakMP3.exe');shutil.copyfile(root/'PeakMP3/PeakMP3.exe.config',peak/'PeakMP3.exe.config');shutil.copyfile(root/'PeakMP3/ffmpeg.exe',peak/'ffmpeg.exe')
shutil.copytree(root/'PeakMP3/ASSETS/Vintage/Runtime',peak/'ASSETS/Vintage/Runtime',ignore=shutil.ignore_patterns('record_frames.png','turntable_frames.png'))
cache=data/'AUDIO_CACHE';cache.mkdir();source=root/'.test-artifacts/native/input.m4a';shutil.copyfile(source,cache/'YtdTest0001.m4a')
os.environ['PEAKMP3_BRIDGE_TEST_PROFILE']=str(profile)
try:
 n=Native(runtime,data);n.send(action='peakstream',**identity);n.until('ready');payload=source.read_bytes()
 for offset in range(0,len(payload),12288):n.send(action='mediaChunk',data=base64.b64encode(payload[offset:offset+12288]).decode());n.until('mediaAck')
 n.send(action='mediaFinish');result=n.until('result',35);n.close();check(result.get('handedOff') and not result.get('path'),'native peak command acknowledges desktop import, not a finished MP3')
 jobs=list((data/'PEAKMP3_INBOX').glob('*/job.json'));check(len(jobs)==1,'one private AAC job created')
 job=json.loads(jobs[0].read_text(encoding='utf-8'));check(job['title']==identity['displayTitle'] and job['language']=='ru','full title and language survive end-to-end handoff')
 check((jobs[0].parent/'accepted').exists(),'actual desktop window wrote the acceptance marker')
 time.sleep(3)
 print('ALL_NATIVE_TO_DESKTOP_HANDOFF_CHECKS_PASSED')
finally:os.environ.pop('PEAKMP3_BRIDGE_TEST_PROFILE',None)
