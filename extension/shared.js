(() => {
 'use strict';
 const finite=(v,fallback=0)=>v!==null&&v!==undefined&&v!==''&&Number.isFinite(Number(v))?Number(v):fallback;
 function percent(time,duration,previous=0){const total=finite(duration),t=finite(time,-1);return total>0&&t>=0?Math.max(finite(previous),Math.min(89,Math.floor(t/total*90))):finite(previous);}
 function videoIcon(){const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');svg.setAttribute('viewBox','0 0 24 24');svg.setAttribute('width','18');svg.setAttribute('height','18');svg.setAttribute('aria-hidden','true');svg.style.cssText='flex:none;vertical-align:middle';const rect=document.createElementNS(svg.namespaceURI,'rect');for(const [key,value] of Object.entries({x:2,y:4,width:15,height:16,rx:3,fill:'none',stroke:'currentColor','stroke-width':1.8}))rect.setAttribute(key,value);const path=document.createElementNS(svg.namespaceURI,'path');path.setAttribute('d','M8 9l5 3-5 3V9zm10-1l4-2v12l-4-2');path.setAttribute('fill','currentColor');svg.append(rect,path);return svg;}
 function decorate(button,label){button.style.display='inline-flex';button.style.alignItems='center';button.style.gap='6px';button.appendChild(videoIcon());const text=document.createElement('span');text.textContent=label;button.appendChild(text);}
 function byHeight(formats){const groups=new Map();for(const f of formats){let group=groups.get(f.height);if(!group){group={...f,codec:'auto',itag:0,tagList:'',tags:[]};groups.set(f.height,group);}if(!group.tags.includes(f.itag))group.tags.push(f.itag);}return [...groups.values()].sort((a,b)=>a.height-b.height).map(f=>({...f,tagList:f.tags.join(',')}));}
 window.YTW7={finite,percent,videoIcon,decorate,byHeight};
})();
