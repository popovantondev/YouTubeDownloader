"""Optional assembly of supplied sprites. Runtime needs no Python; Pillow for preparation only."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter,ImageEnhance
import json
r=Path(__file__).resolve().parent.parent;incoming=r/'ASSETS/Turntable';out=r/'ASSETS/Vintage/Runtime'
plate=Image.open(out/'background.png').convert('RGBA')
def metal_repair(box):
 x0,y0,x1,y1=box;w,h=x1-x0,y1-y0
 corners=[plate.getpixel((x0,y0)),plate.getpixel((x1-1,y0)),plate.getpixel((x0,y1-1)),plate.getpixel((x1-1,y1-1))]
 patch=Image.new('RGBA',(w,h));pixels=patch.load();mask=Image.new('L',(w,h));coverage=mask.load()
 for y in range(h):
  for x in range(w):
   u=x/max(1,w-1);v=y/max(1,h-1);grain=((y*37+11)%7-3)*.22
   pixels[x,y]=tuple(round(max(0,min(255,corners[0][k]*(1-u)*(1-v)+corners[1][k]*u*(1-v)+corners[2][k]*(1-u)*v+corners[3][k]*u*v+grain))) for k in range(3))+(255,)
   coverage[x,y]=round(255*min(1,min(x,y,w-1-x,h-1-y)/5))
 plate.paste(patch,(x0,y0),mask)
metal_repair((73,1405,222,1504))
metal_repair((864,1493,917,1532))
plate.save(out/'turntable_plate.png')
for target,name in {'ab-frame':'01_AB_рамка_пустая.png','ab-normal':'01_AB_ползунок_обычный.png','ab-pressed':'01_AB_ползунок_нажатый.png'}.items():
 im=Image.open(incoming/name).convert('RGBA')
 if target!='ab-frame':im=im.crop((260,20,540,300))
 im.save(out/(target+'.png'))
for name in ['moving','base']:
 Image.open(incoming/('arm-'+name+'-source.png')).convert('RGBA').resize((378,372),Image.Resampling.LANCZOS).save(out/('arm-'+name+'.png'))
# The replacement is already a frontal view: no quarter-turn of an oblique photograph.
cap=Image.open(incoming/'fader-front-source.png').convert('RGBA')
alpha=cap.getchannel('A').point(lambda a:255 if a>200 else 0)
cap=cap.crop(alpha.getbbox())
cap.resize((120,192),Image.Resampling.LANCZOS).save(out/'fader-normal.png')
ImageEnhance.Brightness(cap).enhance(.84).resize((120,192),Image.Resampling.LANCZOS).save(out/'fader-pressed.png')
# Text is composed at runtime, in an orange-only circle, not baked into black vinyl.
Image.open(incoming/'03_Пластинка_яблочко.png').convert('RGBA').save(out/'label-paper.png')
pin=plate.crop((458,1211,486,1247));mask=Image.new('L',(28,36));ImageDraw.Draw(mask).polygon([(13,4),(20,11),(24,25),(27,30),(18,35),(3,32),(0,27),(7,16)],fill=255);pin.putalpha(mask);pin.save(out/'spindle.png')
(out/'turntable_geometry.json').write_text(json.dumps({'label_center':[471.5,1233.5],'label_size':[177.4,130.389],'vinyl_dots_light':'stationary','spindle':'stationary','arm_pivot':[839,1180],'arm_base':[559,1086,378,372],'fader_line_y':[1324,1443]},indent=2),encoding='utf-8')
print('Prepared stationary vinyl/dots, orange-only label, centered fader and physical switch')
