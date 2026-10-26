#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using TreasureRunner;
public static class TreasureRunnerPatchExporter
{
    [MenuItem("TreasureRunner/Spielpatch/Entwurf prüfen und exportieren")]
    public static void Export()
    {
        var patch=JsonUtility.FromJson<DataPatch>(File.ReadAllText("Patches/Draft.json"));
        if(!PatchCodec.Validate(patch,Application.version,out string reason))throw new InvalidOperationException(reason);
        var note=JsonUtility.FromJson<PatchManifest>(File.ReadAllText("Patches/Notes.json"));
        var bytes=new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(patch,true));
        note.schema=1;note.revision=patch.revision;note.appVersion=patch.appVersion;note.payloadFile="patch-"+patch.revision.ToString("0000")+".json";note.payloadBytes=bytes.Length;note.sha256=PatchCodec.Hash(bytes);
        if(!PatchCodec.Manifest(note,Application.version,out reason))throw new InvalidOperationException(reason);
        if(!PatchCodec.Package(new PatchPackage{manifest=note,payload=Encoding.UTF8.GetString(bytes)},Application.version,out _,out reason))throw new InvalidOperationException(reason);
        string directory=Path.Combine("Builds/DataPatch",Application.version);Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,note.payloadFile),bytes);File.WriteAllText(Path.Combine(directory,"manifest.json"),JsonUtility.ToJson(note,true),new UTF8Encoding(false));
        Debug.Log("Spielpatch nur lokal exportiert: "+directory+"/"+note.payloadFile+" · "+bytes.Length+" Bytes · "+note.sha256+". Keine Veröffentlichung.");
    }
}
#endif
