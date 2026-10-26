from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
def replace(path, old, new):
    p=root/path
    text=p.read_text(encoding='utf-8-sig')
    assert old in text, path
    p.write_text(text.replace(old,new),encoding='utf-8')
(root/'Logs/Release09').mkdir(parents=True,exist_ok=True)
replace('Assets/Scripts/Diagnostics/ToolsChecks.cs','string empty="{\\"version\\":1,\\"levels\\":[]}"','string empty=LevelModeration.Empty')
replace('Assets/Scripts/Diagnostics/ToolsChecks.cs','Changed revision invalidates test proof','Changed revision invalidates the approval snapshot')
replace('Assets/Scripts/Diagnostics/ToolsChecks.cs','Higher tested revision replaces old entry instead of duplicating it','Higher approved revision replaces old entry instead of duplicating it')
replace('Assets/Scripts/Diagnostics/FantasyMenuChecks.cs','"Importieren","Freigeben"','"Importieren","Einreichen"')
replace('Assets/Scripts/Diagnostics/NextDevChecks.cs','LevelModeration.PathFor(LevelEditorScreen.Working)==LevelModeration.NextPath','LevelModeration.PathFor(LevelEditorScreen.Working)==LevelModeration.CurrentPath')
replace('Assets/Scripts/Diagnostics/BuilderChecks.cs','new CommunityCatalog{levels=','new CommunityCatalog{moderationProtocol=LevelModeration.Protocol,gameVersion=ReleaseInfo.Version,levels=')
replace('Assets/Scripts/Diagnostics/Revision06Checks.cs','"Untested new draft cannot be submitted"','"New draft has no optional completion proof"')
replace('Assets/Scripts/Diagnostics/Revision06Checks.cs','Check(GameObject.Find("Dialog")==null,"Submit action is blocked until a successful test")','Check(GameObject.Find("Dialog")!=null,"Untested valid draft opens submission dialog")')
(root/'Assets/Resources/CommunityStarter.json').write_text(json.dumps({'version':1,'moderationProtocol':4,'gameVersion':'0.9.0','levels':[]})+'\n',encoding='utf-8')
p=root/'Documentation/Arbeitsregeln.md'
text=p.read_text(encoding='utf-8-sig')
header='Am 26.10.2026 ausdrücklich neu autorisiert: Quellen als Git-Dateien statt Source-ZIP veröffentlichen; öffentliches Windows-Paket ausschließlich Welteneditor, Verwaltung nur lokal beim Besitzer; Durchspielen keine Pflicht für Einreichung/Freigabe; neue APK verwendet einen eigenen versionsgebundenen Katalog, den alte Verwaltungsprogramme nicht beschreiben. Nach Umsetzung alles neu veröffentlichen. Damit sind neue Builds, Versionsstand 0.9.0 / Android-Code 10 und gemeinsamer Release autorisiert. Alte Veröffentlichungsartefakte bleiben unverändert. Google-Login bleibt bis zur fehlenden Besitzer-Konfiguration deaktiviert; kein echter Login behauptet.\n\n'
p.write_text(text.replace('# Verbindliche Nutzerregel für TreasureRunner\n\n','# Verbindliche Nutzerregel für TreasureRunner\n\n'+header,1),encoding='utf-8')
print('Release09 policy and regression expectations prepared; no build started.')
