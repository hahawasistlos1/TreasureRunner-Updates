const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const path=require('node:path');
const root=path.resolve(__dirname,'../Web/InviteSite');
const html=fs.readFileSync(path.join(root,'treasurerunner/invite/index.html'),'utf8');
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
let count=0;const report=[];
function check(ok,name){assert(ok,name);report.push('PASS: '+name);count++;}
function render(hash,clipboardFails=false,userAgent='Desktop Test'){
  const nodes={};let copied;
  const document={getElementById(id){return nodes[id]??=( {hidden:['identity','open','copy'].includes(id),textContent:'',listeners:{},addEventListener(type,fn){this.listeners[type]=fn;}});}};
  vm.runInNewContext(script,{location:{hash},document,navigator:{userAgent,clipboard:{async writeText(text){if(clipboardFails)throw Error('Denied');copied=text;}}}});
  return {nodes,copy:()=>copied};
}
(async()=>{
  const id='0123456789abcdef0123456789abcdef',payload='v=1&id='+id+'&name=Waldl%C3%A4ufer%20Mia';
  const page=render('#'+payload),n=page.nodes;
  check(n.title.textContent==='Waldläufer Mia lädt dich ein!','German sender name renders correctly');
  check(n.identity.hidden===false && n.open.hidden===false,'Complete valid invitation exposes identity and open button');
  check(n.open.href==='treasurerunner://invite#'+payload,'App fallback keeps complete, encoded invitation');
  await n.copy.listeners.click();check(page.copy()==='https://hahawasistlos1.github.io/treasurerunner/invite/#'+payload,'Copy button preserves canonical HTTPS invitation');
  n.open.listeners.click();check(n.status.textContent.includes('kein Spiel'),'Browser fallback explains unsupported or missing installation');
  const apk='https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/latest/download/TreasureRunner.apk';
  const androidPage=render('#'+payload,false,'Mozilla/5.0 (Linux; Android 16) Chrome/140 Mobile'),a=androidPage.nodes;
  check(a.open.href==='intent://invite#'+payload+'#Intent;scheme=treasurerunner;package=com.treasurerunner.game;S.browser_fallback_url='+encodeURIComponent(apk)+';end','Android button opens existing game or downloads official latest APK');
  const marker=a.open.href.lastIndexOf('#Intent;'),data=a.open.href.slice(0,marker).replace(/^intent:/,'treasurerunner:');
  check(data==='treasurerunner://invite#'+payload,'Android intent data retains exact original invite fragment');
  check(decodeURIComponent(a.open.href.match(/S\.browser_fallback_url=([^;]+);/)[1])===apk,'Missing-app fallback is direct official APK, without contact data');
  check(a.open.textContent.includes('APK herunterladen'),'Android opening button explains its download fallback');
  a.open.listeners.click();check(a.status.textContent.includes('APK-Download') && a.status.textContent.includes('Installation'),'Android click explains download and post-install invitation');
  await a.copy.listeners.click();check(androidPage.copy()==='https://hahawasistlos1.github.io/treasurerunner/invite/#'+payload,'Post-install link remains canonical HTTPS invitation, never an intent or APK URL');
  const denied=render('#'+payload,true);await denied.nodes.copy.listeners.click();check(denied.nodes.status.textContent.includes('Adresszeile'),'Denied clipboard permission has manual fallback');
  for(const bad of ['',payload+'&id='+id,payload+'&foo=1',payload.replace('v=1','v=2'),payload.replace(id,'short'),payload.replace(id,id.toUpperCase()),payload.replace('name=','name=%00'),payload.replace('name=','name=%0A'),payload.replace('name=','name=%C0%AF'),payload.replace('name=','name=%oops'),payload.replace('name=','name=%25'),payload.replace('name=','name='+ 'a'.repeat(81)),payload+'a'.repeat(1025)]){
    const broken=render('#'+bad).nodes;check(!broken.open || broken.open.hidden,'Reject malformed or missing invitation '+count);
  }
  const hostile=render('#v=1&id='+id+'&name='+encodeURIComponent('<img src=x onerror=alert(1)>')).nodes;
  check(!hostile.title.textContent.includes('<') && !hostile.title.textContent.includes('>'),'Untrusted sender is displayed as sanitized text, never HTML');
  check(html.includes('ab TreasureRunner 0.8.0') && html.includes('älteren Spielversion'),'Public page states invitations require version 0.8 or newer');
  check(html.includes('id="download" href="'+apk+'"'),'Download entry points directly to latest APK rather than release overview');
  check(!/location\.(assign|replace)|setTimeout|\.click\(\)/.test(script),'No timed app probe or unwanted automatic download for installed/browser-blocked games');
  check(!/fetch\(|XMLHttpRequest|sendBeacon|localStorage|<script[^>]+src=/.test(html),'No contact upload, external script, analytics or browser storage');
  check(html.includes('name="referrer" content="no-referrer"'),'Invite fragment is not exposed through referrer');
  const links=JSON.parse(fs.readFileSync(path.join(root,'.well-known/assetlinks.json'),'utf8'));
  const digest=fs.readFileSync(path.resolve(__dirname,'../Logs/SignatureVerification07.txt'),'utf8').match(/digest: ([a-f0-9]{64})/)[1].match(/../g).join(':').toUpperCase();
  check(links[0].target.sha256_cert_fingerprints[0]===digest,'App Links use the exact existing APK public signing certificate');
  check(links[0].target.package_name==='com.treasurerunner.game' && links[0].relation.includes('delegate_permission/common.handle_all_urls'),'Asset association uses the existing game identity');
  check(fs.existsSync(path.join(root,'.nojekyll')),'Well-known association is included without Jekyll filtering');
  report.push(`ALL ${count} INVITE WEBSITE TESTS PASSED`);fs.writeFileSync(path.resolve(__dirname,'../Logs/InviteWebsiteTests.txt'),report.join('\n')+'\n');console.log(report.at(-1));
})().catch(e=>{console.error(e);process.exitCode=1;});
