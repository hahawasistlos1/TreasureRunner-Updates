"""Inspect public creator download links; no account cookies or private credentials."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urljoin,urlparse
import concurrent.futures,hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Downloads/AssetExpansion';OUT.mkdir(parents=True,exist_ok=True)
PACKS=[
('kenney-town','Kenney','Fantasy Town Kit','https://kenney.nl/assets/fantasy-town-kit','CC0-1.0','models'),
('kenney-castle','Kenney','Castle Kit','https://kenney.nl/assets/castle-kit','CC0-1.0','models'),
('kenney-cave','Kenney','Modular Cave Kit','https://kenney.nl/assets/modular-cave-kit','CC0-1.0','models'),
('kenney-pirate','Kenney','Pirate Kit','https://kenney.nl/assets/pirate-kit','CC0-1.0','models'),
('kenney-platformer2d','Kenney','New Platformer Pack','https://kenney.nl/assets/new-platformer-pack','CC0-1.0','sprites'),
('kenney-smoke','Kenney','Smoke Particles','https://kenney.nl/assets/smoke-particles','CC0-1.0','effects'),
('kenney-fantasy-ui','Kenney','Fantasy UI Borders','https://kenney.nl/assets/fantasy-ui-borders','CC0-1.0','ui'),
('kenney-rpg-ui','Kenney','UI Pack RPG Expansion','https://kenney.nl/assets/ui-pack-rpg-expansion','CC0-1.0','ui'),
('kenney-controls','Kenney','Mobile Controls','https://kenney.nl/assets/mobile-controls','CC0-1.0','ui'),
('kenney-rpg-audio','Kenney','RPG Audio','https://kenney.nl/assets/rpg-audio','CC0-1.0','audio'),
('kenney-impact','Kenney','Impact Sounds','https://kenney.nl/assets/impact-sounds','CC0-1.0','audio'),
('kenney-interface','Kenney','Interface Sounds','https://kenney.nl/assets/interface-sounds','CC0-1.0','audio'),
('kay-forest','Kay Lousberg','Forest Nature FREE','https://kaylousberg.itch.io/kaykit-forest','CC0-1.0','models'),
('kay-adventurers','Kay Lousberg','Adventurers FREE','https://kaylousberg.itch.io/kaykit-adventurers','CC0-1.0','characters'),
('kay-weapons','Kay Lousberg','Fantasy Weapons Bits FREE','https://kaylousberg.itch.io/fantasy-weapons-bits','CC0-1.0','weapons'),
('kay-platformer','Kay Lousberg','Platformer Pack FREE','https://kaylousberg.itch.io/kaykit-platformer','CC0-1.0','models'),
('quater-monsters','Quaternius','Ultimate Monsters','https://quaternius.com/packs/ultimatemonsters.html','verify-download-license','characters'),
('quater-nature','Quaternius','Ultimate Stylized Nature','https://quaternius.com/packs/ultimatestylizednature.html','verify-download-license','models'),
('quater-ruins','Quaternius','Ultimate Modular Ruins','https://quaternius.com/packs/ultimatemodularruins.html','verify-download-license','models'),
('quater-props','Quaternius','Fantasy Props Standard','https://quaternius.itch.io/fantasy-props-megakit','verify-download-license','models'),
('quater-animation1','Quaternius','Universal Animation Library Standard','https://quaternius.itch.io/universal-animation-library','verify-download-license','animations'),
('quater-animation2','Quaternius','Universal Animation Library 2 Standard','https://quaternius.itch.io/universal-animation-library-2','verify-download-license','animations'),
('abstraction-music','Abstraction / Tallbeard Studios','FREE Music Loop Bundle','https://tallbeard.itch.io/music-loop-bundle','CC0-1.0','music'),
]

class Links(HTMLParser):
    def __init__(self):super().__init__();self.links=[];self.current=None
    def handle_starttag(self,tag,attrs):
        values=dict(attrs)
        if tag=='a' and values.get('href'):self.current={'href':values['href'],'text':''};self.links.append(self.current)
    def handle_data(self,text):
        if self.current:self.current['text']+=text
    def handle_endtag(self,tag):
        if tag=='a':self.current=None

def fetch(pack):
    key,author,title,url,license,kind=pack
    p=OUT/(key+'.html')
    if not p.exists():
        result=subprocess.run(['curl.exe','--fail','--silent','--show-error','--location','--compressed','--connect-timeout','30','--max-time','120','--retry','2','--retry-all-errors','--max-filesize','5242880',url],capture_output=True)
        if result.returncode:return {'id':key,'title':title,'page':url,'error':'HTTP transport '+str(result.returncode)}
        p.write_bytes(result.stdout)
    parser=Links();parser.feed(p.read_text(encoding='utf-8',errors='replace'))
    links=[]
    for link in parser.links:
        absolute=urljoin(url,link['href'])
        if urlparse(absolute).scheme!='https':continue
        if any(s in absolute.lower() for s in ['.zip','.fbx','drive.google.com','/purchase','/download','github.com','/license']):
            links.append({'url':absolute,'title':' '.join(link['text'].split())[:100]})
    print(key+': '+str(len(links))+' candidate links',flush=True)
    return {'id':key,'author':author,'title':title,'page':url,'license':license,'kind':kind,'links':links,'pageSha256':hashlib.sha256(p.read_bytes()).hexdigest()}

if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:results=list(pool.map(fetch,PACKS))
    (OUT/'download-plan.json').write_text(json.dumps({'packs':results},indent=2)+'\n',encoding='utf-8')
    for p in results:
        print(p['id']+': '+json.dumps(p.get('links',p.get('error')),ensure_ascii=False),flush=True)
