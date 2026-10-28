from pathlib import Path
from collections import Counter
import zipfile,json
root=Path(__file__).resolve().parents[2]
for key in ['kay-adventurers','kay-skeletons-full','kay-dungeon-full']:
    p=root/'Downloads/AssetExpansion'/(key+'.zip')
    with zipfile.ZipFile(p) as z:
        names=[n for n in z.namelist() if not n.endswith('/')]
        print(key,json.dumps(dict(Counter(Path(n).suffix.lower() for n in names))))
        print('\n'.join(names[:12]))
