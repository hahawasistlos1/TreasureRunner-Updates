from pathlib import Path
# Preserve explicit document entry points in retained test/preview sources.
for folder in ['Assets/Editor','Assets/Scripts/Diagnostics']:
 for p in Path(folder).glob('*.cs'):
  s=p.read_text(encoding='utf-8-sig')
  import re
  s2=re.sub(r'LevelEditorScreen\.Working=([^;\n]+);LevelEditorScreen\.Open\(\);',r'LevelEditorScreen.OpenDocument(\1);',s)
  if s2!=s:p.write_text(s2,encoding='utf-8')
p=Path('Assets/Editor/TreasureRunnerLiveEditorTests.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('Button Button(string text)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==text);','Button Button(string text)=>BuilderTestNavigation.Button(text);');p.write_text(s,encoding='utf-8')
p=Path('Assets/Editor/TreasureRunnerEditorCommunityTests.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Level einrichten")','BuilderTestNavigation.Button("Level einrichten")')
s=s.replace('editor.LoadDocument(live);scene.Zoom(1.9f);','editor.LoadDocument(live);scene.SetSimulation(true);scene.Zoom(1.9f);')
s=s.replace('Check(new[]{"Speichern","Testspielen","Simulation: an","Level einrichten","Bauteil wählen","Bauplatz"}.All(label=>buttons.Any(b=>b.GetComponentInChildren<Text>()?.text==label)),"Structured editor exposes separate document, build, simulation and navigation tools");','Check(buttons.Length==2 && GameObject.Find("Bauteile")!=null && GameObject.Find("Funktionen")!=null,"Full-screen editor has exactly two floating menu buttons");')
s=s.replace('Check(room.IsInside,"Door re-arms for a deliberate later visit");run.Menu();','''Check(room.IsInside,"Door re-arms for a deliberate later visit without taking treasure");
        room.Interact(2,player);room.Interact(3,player);yield return new WaitForFixedUpdate();
        Check(room.Claimed && room.DoorRetired && !door.enabled && !door.gameObject.activeInHierarchy,"Taking treasure and leaving removes door and entrance trigger for the run");
        room.Interact(1,player);Check(!room.IsInside,"Retired door ignores stale or repeated entry callbacks");
        run.Menu();''')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/Tools/DesktopToolsSmoke.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('Check(live.Simulation,"Native Windows editor starts with live preview enabled");','Check(!live.Simulation,"Native Windows editor starts in precise build mode");')
s=s.replace('var submit=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Einreichen");','var submit=BuilderTestNavigation.Button("Einreichen");')
s=s.replace('var palette=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="Bauteil wählen");','var palette=GameObject.Find("Bauteile").GetComponent<Button>();')
p.write_text(s,encoding='utf-8')
