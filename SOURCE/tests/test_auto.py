from pathlib import Path
import sys,json,subprocess
test=Path(__file__).with_name('test_native.py').resolve();sys.argv=[str(test),sys.argv[1],sys.argv[2]]
ns={'__file__':str(test)};source=test.read_text('utf-8');exec(source[:source.index('results=[]')],ns)
protocol,stream,identity,sid=ns['protocol'],ns['stream'],ns['identity'],ns['sid']
av1=ns['work']/'auto_av1.mp4'
ns['run'](['-f','lavfi','-i','color=c=blue:s=854x480:r=5','-t','6','-an','-c:v','libaom-av1','-cpu-used','8','-g','10','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000',str(av1)])
messages=[dict(action='start',session=sid,height=480,sourceHeight=480,itag=0,codec='auto',tagList='135,244,397',rate=4,**identity)]+stream(av1,397,1)+stream(ns['audio'],140,2)+[dict(action='finish',session=sid)]
out=protocol(messages,'auto-av1');assert out[-1]['ok'] and out[-1]['codec']=='av1',out
dest=Path(out[-1]['path']);assert ns['packet_hash'](av1,'v')==ns['packet_hash'](dest,'v')
meta=ns['metadata'](dest);assert next(s for s in meta['streams'] if s['codec_type']=='video')['avg_frame_rate']=='5/1';assert abs(float(meta['format']['duration'])-6)<0.1
report='PASS: AUTO 480p saves AV1 when H.264/VP9 are advertised but not received; output codec correctly reported as AV1.\nPASS: capture-speed setting does not change the stored video frame rate, duration, or encoded packets.\n'
(ns['work']/'auto_results.txt').write_text(report,'utf-8');print(report)
