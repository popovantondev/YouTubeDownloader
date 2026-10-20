"""Optional deterministic sprite preparation. Ready PNGs are shipped with the app.
Run with Python, Pillow and NumPy from the application directory.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json, shutil

root=Path(__file__).resolve().parent.parent
source=root/'ASSETS/Vintage';out=source/'Runtime';out.mkdir(exist_ok=True)
plate=Image.open(source/'01_background_plate_v1.png').convert('RGBA')
assert plate.size==(973,1616)
clean=Image.open(source/'06_clean_plate.png').convert('RGBA').resize(plate.size,Image.Resampling.LANCZOS)
# Import only requested clean regions; the rest of the approved art is unchanged.
for box in [(836,851,931,944),(140,1016,830,1034),(835,1276,865,1481),(864,1494,907,1528)]:
    plate.paste(clean.crop(box),box[:2])
# The knob face and its lighting are static; its marker is rendered by the app.
plate.paste(clean.crop((523,683,583,739)),(523,683))
# Move a smaller physical export bezel down, with more clearance from the record.
bezel=plate.crop((337,1482,638,1555)).resize((280,62),Image.Resampling.LANCZOS)
metal=plate.crop((664,1482,815,1557)).resize((311,79),Image.Resampling.BICUBIC)
plate.paste(metal,(332,1479));plate.paste(bezel,(347,1496))
# Smaller music-file button, preserving its physical border and surrounding card.
note=plate.crop((53,861,148,949)).resize((78,73),Image.Resampling.LANCZOS)
plate.paste(plate.crop((153,861,174,949)).resize((95,88),Image.Resampling.BICUBIC),(53,861))
plate.paste(note,(61,868))
# Seamless repair of the two resized-control surroundings, feathered into the plate.
repair=Image.open(source/'07_seam_repair.png').convert('RGBA').resize(plate.size,Image.Resampling.LANCZOS)
for box in [(320,1476,654,1559),(49,937,154,954)]:
    x0,y0,x1,y1=box;yy,xx=np.mgrid[y0:y1,x0:x1]
    distance=np.minimum.reduce([xx-x0,x1-1-xx,yy-y0,y1-1-yy])
    alpha=Image.fromarray(np.clip(distance/6,0,1).__mul__(255).astype('uint8'))
    plate.paste(repair.crop(box),box[:2],alpha)
plate.save(out/'background.png')

# Crop and normalize the three physical cap states to the same reference bezel.
atlas=Image.open(source/'04_preset_button_states.png').convert('RGBA')
for row in range(3):
    cell=atlas.crop((0,row*512,1024,(row+1)*512));a=np.array(cell)[:,:,3]
    y,x=np.where(a>100)
    box=(max(0,int(x.min())-3),max(0,int(y.min())-3),min(1024,int(x.max())+4),min(512,int(y.max())+4))
    cell.crop(box).resize((194,102),Image.Resampling.LANCZOS).save(out/f'button{row}.png')

original=Image.open(root/'DESIGN/vintage_reference.png').convert('RGBA')
knob=plate.crop((342,524,620,802));mask=Image.new('L',knob.size)
ImageDraw.Draw(mask).ellipse((2,2,275,275),fill=255);knob.putalpha(mask)
knob.save(out/'knob.png')

# True-alpha tonearm, with invisible background noise removed before cropping.
arm=Image.open(source/'05_tonearm_clean.png').convert('RGBA')
if arm.size!=plate.size:arm=arm.resize(plate.size,Image.Resampling.LANCZOS)
arm_data=np.array(arm);arm_data[arm_data[:,:,3]<=4]=0
arm=Image.fromarray(arm_data);bounds=arm.getbbox();arm.crop(bounds).save(out/'tonearm.png')

# A perspective plane keeps the spindle fixed while the outer ellipse is asymmetric.
# Split diffuse/slow angular illumination from moving fine groove texture.
# Neither the highlight nor spindle moves. Ninety-six prerendered frames reduce stepping.
vinyl=np.array(Image.open(source/'02_vinyl.png').convert('RGBA'),dtype=np.float32)
left,top,width,height=198,1052,554,394
yy,xx=np.mgrid[top:top+height,left:left+width].astype(np.float32)
cx,cy,sx,c=471.5,1233.5,261.0,-.0860215054
a=1435.0*(1+c)-cy
v=(cy-yy)/(yy*c-a);u=(xx-cx)*(c*v+1)/sx
inside=(u*u+v*v)<=1
def sample(data,px,py):
    px=np.clip(px,0,data.shape[1]-1.001);py=np.clip(py,0,data.shape[0]-1.001)
    ix=px.astype(int);iy=py.astype(int);dx=(px-ix)[:,:,None];dy=(py-iy)[:,:,None]
    return data[iy,ix]*(1-dx)*(1-dy)+data[iy,ix+1]*dx*(1-dy)+data[iy+1,ix]*(1-dx)*dy+data[iy+1,ix+1]*dx*dy
radial=np.linspace(0,503,640)[:,None];angles=np.linspace(0,2*np.pi,1440,endpoint=False)[None,:]
polar=sample(vinyl,511.5+radial*np.cos(angles),511.5+radial*np.sin(angles))
spectrum=np.fft.rfft(polar,axis=1);spectrum[:,7:]=0
illum=np.fft.irfft(spectrum,n=1440,axis=1).astype(np.float32)
illum=np.concatenate([illum,illum[:,:1]],axis=1)
rr=np.sqrt(u*u+v*v);angle=np.mod(np.arctan2(v,u),2*np.pi)
fixed=sample(illum,angle/(2*np.pi)*1440,rr*639)
base=sample(vinyl,511.5+u*503,511.5+v*503)
sheet=Image.new('RGBA',(width*12,height*8))
for frame in range(96):
    theta=frame*2*np.pi/96;co,si=np.cos(theta),np.sin(theta)
    rotating=sample(vinyl,(u*co+v*si)*503+511.5,(-u*si+v*co)*503+511.5)
    moving_light=sample(illum,np.mod(angle-theta,2*np.pi)/(2*np.pi)*1440,rr*639)
    sampled=fixed+(rotating-moving_light)
    # The center pin is a fixed reflective object above the rotating record.
    sampled[rr<.045]=base[rr<.045]
    sampled[:,:,3]=base[:,:,3]
    sampled[~inside]=0
    image=Image.fromarray(np.clip(sampled,0,255).astype('uint8'))
    sheet.paste(image,((frame%12)*width,(frame//12)*height))
sheet.save(out/'record_frames.png')
# Rotate the teeth texture while the hub/face and diffuse light stay fixed.
cap=np.array(knob,dtype=np.float32)
ky,kx=np.mgrid[0:278,0:278].astype(np.float32);ku=kx-139;kv=ky-139
kr=np.sqrt(ku*ku+kv*kv);ka=np.mod(np.arctan2(kv,ku),2*np.pi)
rad=np.linspace(0,141,180)[:,None];ang=np.linspace(0,2*np.pi,720,endpoint=False)[None,:]
kp=sample(cap,139+rad*np.cos(ang),139+rad*np.sin(ang))
kf=np.fft.rfft(kp,axis=1);kf[:,6:]=0
kl=np.fft.irfft(kf,n=720,axis=1).astype(np.float32);kl=np.concatenate([kl,kl[:,:1]],axis=1)
fixed_k=sample(kl,ka/(2*np.pi)*720,kr/141*179)
ring=np.clip((kr-122)/4,0,1)*np.clip((139-kr)/2,0,1)
knobs=Image.new('RGBA',(278*11,278))
for level in range(11):
    t=(level-10)*24*np.pi/180;co,si=np.cos(t),np.sin(t)
    spun=sample(cap,139+ku*co+kv*si,139-ku*si+kv*co)
    light=sample(kl,np.mod(ka-t,2*np.pi)/(2*np.pi)*720,kr/141*179)
    value=cap*(1-ring[:,:,None])+(fixed_k+spun-light)*ring[:,:,None]
    value[:,:,3]=cap[:,:,3]
    knobs.paste(Image.fromarray(np.clip(value,0,255).astype('uint8')),(level*278,0))
knobs.save(out/'knob_states.png')
geometry={'canvas':[973,1616],'record':[left,top,width,height],'record_frames':96,'record_columns':12,'tonearm':list(bounds),'knob':[342,524,278,278],'lighting':'stationary; moving high-frequency grooves only','fader_center_x':883.5}
(out/'geometry.json').write_text(json.dumps(geometry,indent=2),encoding='utf-8')
print(json.dumps(geometry))
