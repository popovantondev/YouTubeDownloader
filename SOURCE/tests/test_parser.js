const fs=require('fs'),vm=require('vm'),assert=require('assert');
const file=require('path').join(__dirname,'../../extension/sabr_capture.js');
const source=fs.readFileSync(file,'utf8').replace(/\}\)\(\);\s*$/, 'globalThis.test={Parser,uvar,mediaHeader};})();');
const messages=[],listeners={};
const ctx={Uint8Array,ArrayBuffer,TextDecoder,URL,Number,Set,JSON,Math,setTimeout,clearTimeout,location:{href:'https://www.youtube.com/watch?v=testVideo01',origin:'https://www.youtube.com'},sessionStorage:{getItem:()=>JSON.stringify({videoId:'testVideo01',session:'testVideo01-test-session'})},document:{getElementById:()=>null}};
ctx.window={addEventListener:(type,fn)=>listeners[type]=fn,postMessage:m=>messages.push(m)};vm.createContext(ctx);vm.runInContext(source,ctx);
listeners.message({source:ctx.window,origin:ctx.location.origin,data:{tag:'YTWIN7_SABR_CAPTURE_11_CONTROL',type:'start',session:'testVideo01-test-session'}});messages.length=0;
function uvar(n){if(n<128)return [n];if(n<16384)return [128|(n&63),n>>6];if(n<2097152)return [192|(n&31),(n>>5)&255,n>>13];return [224|(n&15),(n>>4)&255,(n>>12)&255,n>>20];}
function proto(n){const out=[];do{let b=n%128;n=Math.floor(n/128);if(n)b|=128;out.push(b);}while(n);return out;}
function value(field,n){return [...proto(field*8),...proto(n)];}
function text(field,s){const data=Buffer.from(s);return [...proto(field*8+2),...proto(data.length),...data];}
function part(type,bytes){return new Uint8Array([...uvar(type),...uvar(bytes.length),...bytes]);}
function record(hid,tag,seq,bytes,videoId='testVideo01',offset=0){const header=[...value(1,hid),...text(2,videoId),...value(3,tag),...value(8,seq<0?1:0),...(seq>=0?value(9,seq):[]),...value(11,Math.max(seq,0)*2000),...value(12,2000),...value(14,bytes.length),...value(17,offset)];return Buffer.concat([part(20,header),part(21,[...uvar(hid),...bytes]),part(22,uvar(hid))]);}
const payload=Buffer.alloc(30000);for(let i=0;i<payload.length;i++)payload[i]=i%251;
const packet=record(200,136,0,payload);
for(const step of [1,2,3,7,127,8192,40000]){messages.length=0;const parser=new ctx.test.Parser(1);for(let p=0;p<packet.length;p+=step)parser.feed(new Uint8Array(packet.subarray(p,p+step)));assert.strictEqual(parser.remain,0);assert.strictEqual(parser.buf.length,0);assert.strictEqual(messages[0].type,'header');assert.strictEqual(messages.at(-1).type,'end');const chunks=messages.filter(m=>m.type==='chunk').map(m=>Buffer.from(m.bytes));assert.deepStrictEqual(Buffer.concat(chunks),payload,'Lost media bytes, step='+step);assert(messages[0].length===30000&&messages[0].duration===2000);}
console.log('PASS: UMP headers, 2-byte header IDs, payload and MEDIA_END survive arbitrary 1..40000-byte read boundaries.');
messages.length=0;new ctx.test.Parser(2).feed(new Uint8Array(record(1,136,-1,Buffer.from('init'))));assert.strictEqual(messages[0].seg,-1);assert.strictEqual(messages.at(-1).seg,-1);console.log('PASS: initialization header/end use same -1 sequence as payload chunks.');
messages.length=0;new ctx.test.Parser(3).feed(new Uint8Array(record(1,136,0,payload,'otherVideo1')));assert.strictEqual(messages.length,0);console.log('PASS: another video/ad ID is filtered.');
listeners.message({source:ctx.window,origin:ctx.location.origin,data:{tag:'YTWIN7_SABR_CAPTURE_11_CONTROL',type:'start',session:'testVideo01-test-session',audioCached:true}});messages.length=0;new ctx.test.Parser(4).feed(new Uint8Array(record(1,140,0,payload)));assert.strictEqual(messages.length,0);console.log('PASS: cached audio is not resent to the native host.');
listeners.message({source:ctx.window,origin:ctx.location.origin,data:{tag:'YTWIN7_SABR_CAPTURE_11_CONTROL',type:'start',session:'testVideo01-test-session',tagList:'135,244,397',audioCached:false}});messages.length=0;
new ctx.test.Parser(5).feed(new Uint8Array(record(1,398,0,payload)));assert.strictEqual(messages.length,0,'unselected 720p caused disk/native traffic during 480p capture');
new ctx.test.Parser(6).feed(new Uint8Array(record(1,397,-1,Buffer.from('init480'))));assert(messages.some(m=>m.type==='header'&&m.itag===397));
messages.length=0;new ctx.test.Parser(7).feed(new Uint8Array(record(1,140,0,payload)));assert(messages.some(m=>m.type==='chunk'),'required uncached audio was filtered out');
console.log('PASS: selected quality filters unwanted video traffic while keeping its init and uncached audio.');
// Exercise production fetch/drain logic with an in-flight response.
(async()=>{const emit=[],events={};let release;
const block=new Promise(resolve=>release=resolve);let readCount=0;
const win={addEventListener:(type,fn)=>events[type]=fn,postMessage:m=>emit.push(m),fetch:async()=>({ok:true,headers:{get:()=> 'application/vnd.yt-ump'},clone:()=>({body:{getReader:()=>({read:async()=>{if(readCount++===0)return {value:new Uint8Array(packet),done:false};await block;return {done:true};},cancel:async()=>{}})}})})};
const env={...ctx,window:win,setTimeout,clearTimeout};vm.createContext(env);vm.runInContext(fs.readFileSync(file,'utf8'),env);
await win.fetch('https://test.googlevideo.com/videoplayback?sabr=1');await new Promise(r=>setTimeout(r,10));assert.strictEqual(emit.length,0,'startup bytes must wait for the isolated receiver');events.message({source:win,origin:env.location.origin,data:{tag:'YTWIN7_SABR_CAPTURE_11_CONTROL',type:'start',session:'testVideo01-test-session'}});assert.deepStrictEqual(Buffer.concat(emit.filter(m=>m.type==='chunk').map(m=>Buffer.from(m.bytes))),payload);console.log('PASS: early media bytes wait for the native/isolated handshake and flush without loss.');await new Promise(r=>setTimeout(r,10));
events.message({source:win,origin:env.location.origin,data:{tag:'YTWIN7_SABR_CAPTURE_11_CONTROL',type:'drain',session:'testVideo01-test-session'}});
await new Promise(r=>setTimeout(r,350));assert(!emit.some(m=>m.type==='drained'),'Finished before last response');
release();await new Promise(r=>setTimeout(r,30));assert(emit.some(m=>m.type==='drained'));assert.strictEqual(emit.at(-1).type,'drained');console.log('PASS: finalization waits for the last response reader instead of cutting it off.');})().catch(e=>{console.error(e);process.exitCode=1;});
