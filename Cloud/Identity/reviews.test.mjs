import { readFile,writeFile } from 'node:fs/promises';
import { initializeTestEnvironment,assertFails,assertSucceeds } from '@firebase/rules-unit-testing';
import { doc,setDoc,getDoc,getDocs,deleteDoc,collection,query,limit,writeBatch,serverTimestamp,Timestamp,setLogLevel } from 'firebase/firestore';
if(process.env.GCLOUD_PROJECT!=='demo-treasurerunner' || process.env.FIRESTORE_EMULATOR_HOST!=='127.0.0.1:8187')throw new Error('Local demo emulator required');
setLogLevel('silent');const report=[];
const env=await initializeTestEnvironment({projectId:'demo-treasurerunner',firestore:{host:'127.0.0.1',port:8187,rules:await readFile(new URL('firestore.rules',import.meta.url),'utf8')}});
const db=env.authenticatedContext('reviewer-a').firestore(),other=env.authenticatedContext('reviewer-b').firestore(),guest=env.unauthenticatedContext().firestore();
const key='a'.repeat(64),second='b'.repeat(64),path=`levelReviews/${key}/entries/reviewer-a`;
const fields=()=>({nickname:'Held',stars:1,comment:'Zu schwer!\nDie Fallen sind aber gut.',revision:2,updatedAt:serverTimestamp()});
async function check(label,operation){await operation();report.push('PASS: '+label);}
function commit(store=db,{data=fields(),uid='reviewer-a',level=key,clock=true}={}){
  const batch=writeBatch(store);if(clock)batch.set(doc(store,`reviewWriters/${uid}`),{at:serverTimestamp(),level});batch.set(doc(store,`levelReviews/${level}/entries/${uid}`),data);return batch.commit();
}
async function resetClock(){await env.withSecurityRulesDisabled(async c=>deleteDoc(doc(c.firestore(),'reviewWriters/reviewer-a')));}
try{
  await env.clearFirestore();await setDoc(doc(db,'players/reviewer-a'),{schema:1,nickname:'Held'});await setDoc(doc(other,'players/reviewer-b'),{schema:1,nickname:'Andere'});
  await check('Negative star rating and multiline public comment saved atomically',()=>assertSucceeds(commit()));
  await check('Guest can read a public review without Google profile access',()=>assertSucceeds(getDoc(doc(guest,path))));
  await check('Guest can read a bounded page of reviews',()=>assertSucceeds(getDocs(query(collection(guest,`levelReviews/${key}/entries`),limit(20)))));
  await check('Unbounded enumeration is denied',()=>assertFails(getDocs(collection(guest,`levelReviews/${key}/entries`))));
  await check('Oversized page is denied',()=>assertFails(getDocs(query(collection(guest,`levelReviews/${key}/entries`),limit(21)))));
  await check('Immediate edits cannot bypass one minute cooldown',()=>assertFails(commit()));
  await check('Cooldown applies across levels',()=>assertFails(commit(db,{level:second})));
  await check('Clock cannot be erased to bypass cooldown',()=>assertFails(deleteDoc(doc(db,'reviewWriters/reviewer-a'))));
  await check('Another account cannot modify a review',()=>assertFails(commit(other)));
  await check('Guest cannot post',()=>assertFails(commit(guest)));
  await check('Another account cannot delete a review',()=>assertFails(deleteDoc(doc(other,path))));
  await check('Guest cannot read nickname profiles',()=>assertFails(getDoc(doc(guest,'players/reviewer-a'))));
  await resetClock();
  await check('Review without atomic clock is denied',()=>assertFails(commit(db,{clock:false})));
  await check('Clock without simultaneous review is denied',()=>assertFails(setDoc(doc(db,'reviewWriters/reviewer-a'),{at:serverTimestamp(),level:key})));
  for(const stars of [0,6,1.5,'5',null])await check('Reject invalid star value '+stars,()=>assertFails(commit(db,{data:{...fields(),stars}})));
  for(const comment of ['x'.repeat(401),'bad\u0000text','bad\ttext','bad\u202etext',42,null])await check('Reject invalid comment '+JSON.stringify(comment).slice(0,30),()=>assertFails(commit(db,{data:{...fields(),comment}})));
  for(const extra of ['email','displayName','googleToken','uid','photoUrl'])await check('Reject private/provider field '+extra,()=>assertFails(commit(db,{data:{...fields(),[extra]:'private'}})));
  await check('Reject spoofed author nickname',()=>assertFails(commit(db,{data:{...fields(),nickname:'Andere'}})));
  await check('Reject arbitrary timestamp',()=>assertFails(commit(db,{data:{...fields(),updatedAt:Timestamp.fromMillis(0)}})));
  await check('Reject invalid level namespace',()=>assertFails(commit(db,{level:'not-an-approved-id'})));
  await check('Valid later edit replaces single account rating',()=>assertSucceeds(commit(db,{data:{...fields(),stars:5,comment:''}})));
  await check('Owner can remove their own review immediately',()=>assertSucceeds(deleteDoc(doc(db,path))));
  await check('Delete and repost does not bypass global cooldown',()=>assertFails(commit()));
  await check('Deleting clock still forbidden after review removal',()=>assertFails(deleteDoc(doc(db,'reviewWriters/reviewer-a'))));
  await resetClock();await commit();
  await env.withSecurityRulesDisabled(async c=>setDoc(doc(c.firestore(),'reviewWriters/reviewer-a'),{at:Timestamp.fromMillis(Date.now()-120000),level:key}));
  await check('Later update after cooldown succeeds',()=>assertSucceeds(commit(db,{data:{...fields(),stars:3}})));
  const stored=(await getDoc(doc(guest,path))).data();if(stored.stars!==3 || stored.nickname!=='Held' || Object.keys(stored).length!==5)throw new Error('Stored fields unexpected');
  report.push('PASS: Public record contains chosen nickname, stars, comment, revision and time only');
  report.push('ALL COMMUNITY REVIEW RULES CHECKS PASSED');
}catch(error){report.push('FAIL: '+error.message);process.exitCode=1;}
finally{await env.cleanup();await writeFile(new URL('../../Logs/EditorCommunity/ReviewRulesChecks.txt',import.meta.url),report.join('\n')+'\n');console.log(report.join('\n'));}
