// Use the Firebase CLI's existing account in memory; never print/persist credentials.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),cli=path.join(root,'Cloud/Identity/node_modules/firebase-tools/lib');
const auth=require(path.join(cli,'auth.js')),rules=require(path.join(cli,'gcp/rules.js'));
const mode=process.argv[2];
(async()=>{
  if(!['before','after'].includes(mode))throw Error('Use before or after');
  const account=auth.getGlobalDefaultAccount();if(!account)throw Error('Firebase CLI has no signed-in account');
  const options={project:'treasurerunner-2689b',nonInteractive:true};auth.setActiveAccount(options,account);
  await require(path.join(cli,'requireAuth.js')).requireAuth(options,true);
  const releases=await rules.listAllReleases(options.project),name=await rules.getLatestRulesetName(options.project,'cloud.firestore',releases);
  if(!name)throw Error('No existing Firestore rules release');
  const files=await rules.getRulesetContent(name);if(files.length!==1)throw Error('Unexpected multiple rules files');
  const text=files[0].content;
  const expected=fs.readFileSync(path.join(root,mode==='before'?'Backups/EditorCommunity/firestore-before-reviews.rules':'Cloud/Identity/firestore.rules'),'utf8').replace(/^\uFEFF/,'');
  const canonical=s=>s.replace(/\r\n/g,'\n').trim();
  if(canonical(text)!==canonical(expected))throw Error('Active cloud rules differ from expected '+mode+' state; do not overwrite');
  const out=path.join(root,'Backups/EditorCommunity/firestore-active-'+mode+'.rules');
  if(!fs.existsSync(out))fs.writeFileSync(out,text,{flag:'wx'});
  const report={project:options.project,phase:mode,ruleset:name,sha256:crypto.createHash('sha256').update(text).digest('hex'),matchesExpected:true,credentialsRecorded:false};
  fs.writeFileSync(path.join(root,'Logs/EditorCommunity/CloudRules-'+mode+'.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
})().catch(e=>{console.error('Cloud verification failed: '+e.message);process.exitCode=1});
