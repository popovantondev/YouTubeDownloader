from pathlib import Path
import sys,base64
test=Path(__file__).with_name('test_native.py').resolve()
sys.argv=[str(test),sys.argv[1],sys.argv[2]]
ns={'__file__':str(test)};source=test.read_text('utf-8');exec(source[:source.index('results=[]')],ns)
init,_=ns['fragments'](ns['video']);half=len(init)//2
start=dict(action='start',session=ns['sid'],height=1080,codec='auto',tagList='299,335,699',**ns['identity'])
def part(data,offset,req):
    head=dict(session=ns['sid'],itag=699,segment=-1,request=req,headerId=req,init=True,length=len(init),offset=offset)
    return [dict(action='header',**head),dict(action='chunk',data=base64.b64encode(data).decode(),**head),dict(action='end',**head)]
stop=dict(action='cancel',session=ns['sid'])
first=part(init[:half],0,1)
incomplete=ns['protocol']([start]+first+[stop],'init-partial')
assert not any(m['action']=='initReady' for m in incomplete)
complete=ns['protocol']([start]+first+part(init[half:],half,2)+[stop],'init-complete')
assert any(m['action']=='initReady' and m['itag']==699 for m in complete)
hole=ns['protocol']([start]+first+part(init[half+1:],half+1,2)+[stop],'init-hole')
assert not any(m['action']=='initReady' for m in hole)
report='PASS: initialization ACK requires complete native byte coverage; split init is accepted, partial init and byte holes are not.\nPASS: advertised tag 699 passes the generic native path without pretending it is a tested live 1080p stream.\n'
(ns['work']/'init_results.txt').write_text(report,'utf-8');print(report)
