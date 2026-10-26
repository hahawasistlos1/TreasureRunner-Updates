"""Scoped A55 checks for TreasureRunner only; no embedded device ID or account data."""
from pathlib import Path
import hashlib,json,re,subprocess,sys,time
ROOT=Path(__file__).resolve().parents[1]
ADB=Path('C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe')
APP='com.treasurerunner.game';OUT=ROOT/'Logs/Device09';OUT.mkdir(parents=True,exist_ok=True)
devices=subprocess.check_output([str(ADB),'devices','-l'],text=True)
phones=[line.split()[0] for line in devices.splitlines() if 'model:SM_A556B' in line and re.search(r'\sdevice\s',line)]
if len(phones)!=1:raise RuntimeError('Expected one connected authorized A55')
def adb(*args):return subprocess.check_output([str(ADB),'-s',phones[0],*map(str,args)],timeout=45)
def foreground():
    text=adb('shell','dumpsys','window').decode(errors='replace')
    return any('mCurrentFocus=' in line and APP+'/' in line for line in text.splitlines())
def guard():
    if not foreground():raise RuntimeError('TreasureRunner is not foreground; no input or screenshot performed')
def shot(name):
    guard()
    if not re.fullmatch('[A-Za-z0-9_-]+',name):raise ValueError('Invalid evidence name')
    data=adb('exec-out','screencap','-p');assert data.startswith(b'\x89PNG')
    path=OUT/(name+'.png');path.write_bytes(data);print(path)
mode,*args=sys.argv[1:]
if mode=='launch':
    adb('shell','am','start','-n',APP+'/com.unity3d.player.UnityPlayerGameActivity');print('TreasureRunner opened')
elif mode=='shot':shot(args[0])
elif mode=='tap':
    guard();x,y=map(int,args[:2]);assert 0<=x<3000 and 0<=y<3000
    adb('shell','input','tap',x,y);time.sleep(.7)
    if len(args)>2:shot(args[2])
elif mode=='swipe':
    guard();values=list(map(int,args[:5]));assert all(0<=n<3000 for n in values[:4]) and 50<=values[4]<=1500
    adb('shell','input','swipe',*values);time.sleep(.7)
    if len(args)>5:shot(args[5])
elif mode=='status':print(json.dumps({'gameForeground':foreground()}))
elif mode=='restart':
    guard();adb('shell','am','force-stop',APP);adb('shell','am','start','-n',APP+'/com.unity3d.player.UnityPlayerGameActivity');print('TreasureRunner restarted')
elif mode=='logs':
    pid=adb('shell','pidof',APP).decode().strip();assert re.fullmatch(r'\d+',pid)
    data=adb('logcat','-d','--pid='+pid,'-v','time','Unity:I','AndroidRuntime:E','*:S')
    (OUT/'Runtime.log').write_bytes(data);print('Game-only runtime log saved')
elif mode=='verify-levels':
    destination=ROOT/'Backups/Device09/AfterTest';destination.mkdir(parents=True,exist_ok=True)
    adb('pull','/sdcard/Android/data/'+APP+'/files/my-levels',destination)
    before=ROOT/'Backups/Device09/BeforeInstall/my-levels';after=destination/'my-levels'
    hashes=lambda folder:{p.relative_to(folder).as_posix():hashlib.file_digest(p.open('rb'),'sha256').hexdigest() for p in folder.rglob('*') if p.is_file()}
    original=hashes(before);current=hashes(after);assert original and original==current,'Custom level files changed'
    (OUT/'LevelPreservation.json').write_text(json.dumps({'unchanged':True,'files':current},indent=2)+'\n')
    print('All '+str(len(original))+' custom level files preserved byte-for-byte')
else:raise ValueError('Unsupported check')
