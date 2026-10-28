"""Scoped GitHub transport. Existing owner credential stays in process memory."""
from pathlib import Path
import json,os,subprocess,time

ROOT=Path(__file__).resolve().parents[1]
GIT=Path('C:/Users/Jir3n/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/cmd/git.exe')
ENV=dict(os.environ,GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='never')
BASE='https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates'
class Client:
    def __init__(self):
        result=subprocess.run([str(GIT),'credential','fill'],input='protocol=https\nhost=github.com\n\n',capture_output=True,text=True,env=ENV)
        if result.returncode:raise RuntimeError('Existing GitHub login unavailable; credential output omitted')
        fields=dict(line.split('=',1) for line in result.stdout.splitlines() if '=' in line)
        self.token=fields['password'];fields=None;result=None
        assert self.request('GET','https://api.github.com/user')['login'].lower()=='hahawasistlos1','Wrong owner account'
    def request(self,method,url,payload=None,upload_file=None):
        assert url.startswith(BASE+'/') or url==BASE or url=='https://api.github.com/user' or url.startswith('https://uploads.github.com/repos/hahawasistlos1/TreasureRunner-Updates/')
        headers={'Authorization':'Bearer '+self.token,'User-Agent':'TreasureRunner-authorized-release10','Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','Cache-Control':'no-cache'}
        if method=='GET':url+=('&' if '?' in url else '?')+'verification='+str(time.time_ns())
        config=['url = '+json.dumps(url),'request = '+json.dumps(method)]+['header = '+json.dumps(k+': '+v) for k,v in headers.items()]
        if payload is not None:
            config+=['header = "Content-Type: application/json"','data-binary = '+json.dumps(json.dumps(payload))]
        if upload_file is not None:
            upload_file=Path(upload_file).resolve();out=(ROOT/'Builds/Publish-0.10.0').resolve()
            assert upload_file.parent==out and upload_file.name in {'TreasureRunner.apk','TreasureRunner-Windows-Editor.zip'}
            content_type='application/vnd.android.package-archive' if upload_file.suffix=='.apk' else 'application/zip'
            config+=['header = '+json.dumps('Content-Type: '+content_type),'data-binary = '+json.dumps('@'+upload_file.as_posix())]
        command=['curl.exe','--silent','--show-error','--connect-timeout','45','--max-time','900' if upload_file else '90','--write-out','\n%{http_code}','--config','-']
        if method=='GET':command+=['--retry','2','--retry-delay','2','--retry-all-errors']
        result=subprocess.run(command,input='\n'.join(config)+'\n',capture_output=True,text=True,encoding='utf-8')
        if result.returncode:raise RuntimeError('GitHub transport failed ('+str(result.returncode)+'); credential output omitted')
        body,code=result.stdout.rsplit('\n',1)
        if code=='404':return None
        if not code.startswith('2'):raise RuntimeError('GitHub HTTP '+code+' for '+method+'; response omitted')
        return json.loads(body) if body.strip() else None
    def close(self):self.token=None

if __name__=='__main__':
    client=Client();repo=client.request('GET',BASE);latest=client.request('GET',BASE+'/releases/latest');new=client.request('GET',BASE+'/releases/tags/v0.10.0')
    report={'owner':'hahawasistlos1','defaultBranch':repo['default_branch'],'latest':latest['tag_name'],'newRelease':None if new is None else {'id':new['id'],'draft':new['draft'],'tag':new['tag_name']}}
    client.close();(ROOT/'Logs/Release10/GitHubPreflight.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
