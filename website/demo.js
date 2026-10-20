/* Silent interface demonstration; no audio processing or signal measurement. */
(async () => {
  const canvas = document.querySelector('#peak-demo');
  if (!canvas) return;
  const lang = document.body.dataset.lang;
  const strings = (await (await fetch('website/strings.json')).json())[lang];
  const load = src => new Promise((resolve, reject) => { const im = new Image(); im.onload=()=>resolve(im); im.onerror=reject; im.src='assets/site/'+src; });
  const [screen, plate, knob] = await Promise.all([load('peakmp3-'+lang+'.webp'), load('turntable_plate.png'), load('knob_states.png')]);
  const ctx=canvas.getContext('2d'), range=document.querySelector('#density-range');
  let density=7, playing=false, mode='b', angle=0, speed=0, last=0, frame=0, drag=null;
  const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;
  const play=document.querySelector('#demo-play'), state=document.querySelector('#demo-state');
  function update() {
    range.value=density;
    document.querySelector('#density-number').value=density;
    document.querySelectorAll('[data-compare]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.compare===mode)));
    play.setAttribute('aria-pressed',String(playing));
    play.textContent=(playing?'Ⅱ ':'▶ ')+(playing?strings.pause:strings.play);
    state.textContent=playing?strings.demoPlaying:strings.demoPaused;
    draw(performance.now());
    schedule();
  }
  function schedule(){if(!frame && (playing || speed>.005) && !document.hidden)frame=requestAnimationFrame(tick);}
  function tick(now){frame=0; const dt=Math.min(.06,(now-(last||now))/1000);last=now;speed+=(Number(playing)-speed)*Math.min(1,dt*5);if(!reduced)angle+=dt*speed*1.3;draw(now);schedule();}
  function draw(now){
    ctx.setTransform(canvas.width/973,0,0,canvas.height/1616,0,0);
    ctx.drawImage(screen,0,0,973,1616);
    // Restore the real instrument face, then draw its moving needle underneath the fixed hub.
    ctx.drawImage(plate,57,126,854,326,57,126,854,326);
    const level=playing ? (mode==='a'?-9:-10+density*.8)+Math.sin(now*.008)*3.6+Math.sin(now*.019)*1.6 : -20;
    const a=(-159+(Math.max(-20,Math.min(3,level))+20)*5.25)*Math.PI/180;
    ctx.save();ctx.lineCap='round';ctx.strokeStyle='#c33d23';ctx.lineWidth=4;
    ctx.beginPath();ctx.moveTo(481,435);ctx.lineTo(481+350*Math.cos(a),435+350*Math.sin(a));ctx.stroke();ctx.restore();
    ctx.drawImage(plate,355,399,253,66,355,399,253,66);
    const size=knob.height, k=Math.min(10,Math.round(density));
    ctx.drawImage(knob,k*size,0,size,size,342,524,278,278);
    // Keep the face lighting and central shaft fixed while the indicator moves.
    ctx.save();ctx.beginPath();ctx.arc(481,663,116,0,Math.PI*2);ctx.clip();ctx.drawImage(knob,23,23,232,232,365,547,232,232);ctx.restore();
    const ka=(150+density*24)*Math.PI/180;
    ctx.save();ctx.lineCap='round';ctx.lineWidth=10;ctx.strokeStyle='#69532e';ctx.beginPath();ctx.moveTo(481+82*Math.cos(ka),663+82*Math.sin(ka));ctx.lineTo(481+104*Math.cos(ka),663+104*Math.sin(ka));ctx.stroke();ctx.lineWidth=5;ctx.strokeStyle='#ffe19a';ctx.stroke();ctx.restore();
    ctx.fillStyle='#ece8dd';ctx.fillRect(330,802,310,36);ctx.textAlign='center';ctx.fillStyle='#27231e';ctx.font='bold 28px Studio, sans-serif';ctx.fillText(strings.density+' · '+density,481,831);
    // Only the orange record label rotates; the vinyl reflections and spindle stay fixed.
    ctx.save();ctx.translate(471,1250);ctx.scale(1,.73);ctx.rotate(angle);ctx.fillStyle='#fb8a1a';ctx.beginPath();ctx.arc(0,0,84,0,Math.PI*2);ctx.fill();ctx.fillStyle='#302214';ctx.font='bold 19px Studio, sans-serif';ctx.fillText('PeakMP3',0,-36);ctx.font='14px Studio, sans-serif';ctx.fillText('DEMO',0,38);ctx.restore();
    ctx.drawImage(screen,458,1233,27,38,458,1233,27,38);
    if(playing){ctx.fillStyle='#33281a';ctx.beginPath();ctx.arc(115,1143,40,0,Math.PI*2);ctx.fill();ctx.fillStyle='#fff2ca';ctx.fillRect(102,1124,9,36);ctx.fillRect(119,1124,9,36);}
  }
  range.addEventListener('input',()=>{density=+range.value;update();});
  document.querySelectorAll('[data-compare]').forEach(b=>b.addEventListener('click',()=>{mode=b.dataset.compare;update();}));
  play.addEventListener('click',()=>{playing=!playing;update();});
  document.querySelector('#demo-stop').addEventListener('click',()=>{playing=false;angle=0;update();});
  const point=e=>{const r=canvas.getBoundingClientRect();return {x:(e.clientX-r.left)*973/r.width,y:(e.clientY-r.top)*1616/r.height};};
  canvas.addEventListener('pointerdown',e=>{const p=point(e);if(Math.hypot(p.x-481,p.y-663)<150){drag={y:e.clientY,value:density};canvas.setPointerCapture(e.pointerId);e.preventDefault();}else if(Math.hypot(p.x-115,p.y-1143)<65){playing=!playing;update();}else if(Math.hypot(p.x-115,p.y-1247)<55){playing=false;update();}else if(p.x<250&&p.y>1420&&p.y<1495){mode=mode==='a'?'b':'a';update();}});
  canvas.addEventListener('pointermove',e=>{if(drag){density=Math.max(0,Math.min(10,Math.round(drag.value+(drag.y-e.clientY)/9)));update();}});
  canvas.addEventListener('pointerup',()=>{drag=null;});canvas.addEventListener('pointercancel',()=>{drag=null;});
  canvas.addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight',' ','Escape'].includes(e.key)){e.preventDefault();if(e.key===' ')playing=!playing;else if(e.key==='Escape')playing=false;else density=Math.max(0,Math.min(10,density+(e.key==='ArrowRight'?1:-1)));update();}});
  document.addEventListener('visibilitychange',()=>{last=0;schedule();});
  update();
})().catch(error=>{console.error('Demo unavailable',error);const c=document.querySelector('#peak-demo');if(c)c.replaceWith(Object.assign(new Image(),{src:'assets/site/peakmp3-'+document.body.dataset.lang+'.webp',alt:'PeakMP3'}));});
