#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TreasureRunner;

public class TreasureRunnerLibraryPreprocessor:AssetPostprocessor
{
    bool Library=>assetPath.StartsWith("Assets/Art/FreeLibrary/") || assetPath.StartsWith("Assets/Resources/AssetLibrary/");
    void OnPreprocessTexture()
    {
        if(!Library)return;
        var t=(TextureImporter)assetImporter;t.maxTextureSize=1024;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.isReadable=false;t.anisoLevel=0;t.filterMode=FilterMode.Bilinear;
    }
    void OnPreprocessModel()
    {
        if(!Library)return;
        var m=(ModelImporter)assetImporter;m.isReadable=true;m.importCameras=false;m.importLights=false;
        bool animated=assetPath.Contains("adventurers") || assetPath.Contains("skeletons") || assetPath.Contains("monsters") || assetPath.Contains("animation");
        m.importAnimation=animated;m.animationType=animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
        m.animationCompression=ModelImporterAnimationCompression.Optimal;
    }
    void OnPreprocessAudio()
    {
        if(!Library)return;
        var a=(AudioImporter)assetImporter;var s=a.defaultSampleSettings;s.loadType=assetPath.Contains("music") ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
        s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.5f;s.sampleRateSetting=AudioSampleRateSetting.OptimizeSampleRate;a.defaultSampleSettings=s;
    }
}
public static class TreasureRunnerAssetLibraryImport
{
    [Serializable] class PlannedPack:LibraryPack {public string[] licenses;}
    [Serializable] class PlannedEntry:LibraryEntry {public string source;public string[] animationSources;}
    [Serializable] class PlannedImage:LibraryImage {public string source;}
    [Serializable] class PlannedAudio:LibraryAudio {public string source;}
    [Serializable] class Plan {public PlannedPack[] packs;public PlannedEntry[] assets;public PlannedImage[] images;public PlannedAudio[] audio;public LibraryCharacter[] characters;}
    const string Output="Assets/Resources/AssetLibrary";
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,string> atlases=new Dictionary<string,string>();
    static string PathFor(string resource,string extension)=>"Assets/Resources/"+resource+extension;
    static void Parent(string path)=>Directory.CreateDirectory(Path.GetDirectoryName(path));
    public static void Prepare()
    {
        Directory.CreateDirectory("Logs/AssetExpansion");Directory.CreateDirectory(Output);
        var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Downloads/AssetExpansion/runtime-plan.json"));
        File.WriteAllText("Logs/AssetExpansion/Animation-Clips.txt","");
        foreach(var p in plan.packs)
        {
            string path=PathFor(p.licenseResource,".txt");Parent(path);
            File.WriteAllText(path,string.Join("\n\n",p.licenses.Select(f=>"SOURCE FILE: "+f+"\n"+ReadLicense(f))));
        }
        foreach(var image in plan.images)CopyData(image.source,PathFor(image.resource,Path.GetExtension(image.source)));
        foreach(var audio in plan.audio)CopyData(audio.source,PathFor(audio.resource,Path.GetExtension(audio.source)));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var imported=new List<LibraryEntry>();int count=0;
        AssetDatabase.StartAssetEditing();try{foreach(var entry in plan.assets)
        {
            GameObject instance=null;
            try
            {
                if(Path.GetExtension(entry.source).Equals(".fbx",StringComparison.OrdinalIgnoreCase))
                {
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(entry.source);if(model==null)throw new Exception("Model not imported: "+entry.source);
                    if(entry.animated)File.AppendAllText("Logs/AssetExpansion/Animation-Clips.txt",entry.source+": "+string.Join(", ",AssetDatabase.LoadAllAssetsAtPath(entry.source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>c.name))+"\n");
                    instance=UnityEngine.Object.Instantiate(model);instance.name=entry.title;
                    if(entry.animated && entry.role==(int)BuilderRole.Enemy && entry.source.Contains("/Converted/"))
                    {
                        string body=Path.GetFileNameWithoutExtension(entry.source).Replace("_Hooded","")+"_";
                        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))if(!renderer.name.StartsWith(body,StringComparison.Ordinal))renderer.gameObject.SetActive(false);
                    }
                    if(entry.animationSources!=null && entry.animationSources.Length>0)
                    {
                        var animation=instance.GetComponentInChildren<Animation>()??instance.AddComponent<Animation>();animation.playAutomatically=false;
                        foreach(string animationSource in entry.animationSources)foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(animationSource).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))
                        {animation.AddClip(clip,clip.name);File.AppendAllText("Logs/AssetExpansion/Animation-Clips.txt",entry.source+" + "+animationSource+": "+clip.name+"\n");}
                    }
                    foreach(var collider in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                    foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        var slots=renderer.sharedMaterials;
                        for(int i=0;i<slots.Length;i++)slots[i]=MaterialFor(entry,slots[i]);
                        renderer.sharedMaterials=slots;
                    }
                }
                else
                {
                    instance=GameObject.CreatePrimitive(PrimitiveType.Quad);instance.name=entry.title;UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
                    var image=plan.images.First(i=>i.source==entry.source);var texture=Resources.Load<Texture2D>(image.resource);
                    var materialPath=Output+"/Materials/"+image.id+".mat";Parent(materialPath);
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(mat==null){mat=new Material(Shader.Find("TreasureRunner/Library Sprite"));AssetDatabase.CreateAsset(mat,materialPath);}
                    mat.mainTexture=texture;EditorUtility.SetDirty(mat);instance.GetComponent<Renderer>().sharedMaterial=mat;
                    instance.transform.localScale=new Vector3(texture.width/(float)Mathf.Max(1,texture.height),1,1);
                    instance.AddComponent<LibraryPlaneVisual>().frames=entry.frames;
                    if(entry.role==(int)BuilderRole.Block)
                    {
                        var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);readable.LoadImage(File.ReadAllBytes(entry.source));
                        var shape=TileSilhouette(readable);UnityEngine.Object.DestroyImmediate(readable);
                        string shapePath=Output+"/Collision/"+image.id+".asset";Parent(shapePath);
                        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(shapePath);
                        if(saved==null){AssetDatabase.CreateAsset(shape,shapePath);saved=shape;}else{EditorUtility.CopySerialized(shape,saved);UnityEngine.Object.DestroyImmediate(shape);EditorUtility.SetDirty(saved);}
                        instance.AddComponent<LibraryTileCollision>().shape=saved;
                    }
                }
                string prefab=PathFor(entry.resource,".prefab");Parent(prefab);PrefabUtility.SaveAsPrefabAsset(instance,prefab);imported.Add(entry);
                if(++count%100==0)Debug.Log("Library prefabs prepared: "+count+"/"+plan.assets.Length);
            }
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);}
        }}finally{AssetDatabase.StopAssetEditing();}
        var catalog=new LibraryData{packs=plan.packs.Cast<LibraryPack>().ToArray(),entries=imported.ToArray(),images=plan.images.Cast<LibraryImage>().ToArray(),audio=plan.audio.Cast<LibraryAudio>().ToArray(),characters=plan.characters??Array.Empty<LibraryCharacter>()};
        File.WriteAllText(Output+"/catalog.json",JsonUtility.ToJson(catalog,true));AssetDatabase.Refresh();AssetDatabase.SaveAssets();AssetLibrary.Refresh();
        File.WriteAllText("Logs/AssetExpansion/Prepared.json",JsonUtility.ToJson(catalog,true));
        Debug.Log("ASSET LIBRARY PREPARED: "+catalog.packs.Length+" packs, "+catalog.entries.Length+" models/sprites, "+catalog.audio.Length+" audio clips. No APK built, no version changed.");
    }
    static void CopyData(string source,string target)
    {Parent(target);if(!File.Exists(target) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target)))File.Copy(source,target,true);}
    static string ReadLicense(string path)
    {var bytes=File.ReadAllBytes(path);try{return new System.Text.UTF8Encoding(false,true).GetString(bytes);}catch(System.Text.DecoderFallbackException){return System.Text.Encoding.GetEncoding(1252).GetString(bytes);}}
    static Mesh TileSilhouette(Texture2D image)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();const int columns=32;
        for(int column=0;column<columns;column++)
        {
            int min=image.height,max=-1,start=column*image.width/columns,end=Mathf.Max(start+1,(column+1)*image.width/columns);
            for(int x=start;x<end;x++)for(int y=0;y<image.height;y++)if(image.GetPixel(x,y).a>.4f){min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
            if(max<min)continue;float l=column/(float)columns-.5f,r=(column+1f)/columns-.5f,b=min/(float)image.height-.5f,t=(max+1f)/image.height-.5f;
            Quad(new Vector3(l,b,-.5f),new Vector3(l,t,-.5f),new Vector3(r,t,-.5f),new Vector3(r,b,-.5f));
            Quad(new Vector3(r,b,.5f),new Vector3(r,t,.5f),new Vector3(l,t,.5f),new Vector3(l,b,.5f));
            Quad(new Vector3(l,t,-.5f),new Vector3(l,t,.5f),new Vector3(r,t,.5f),new Vector3(r,t,-.5f));
            Quad(new Vector3(l,b,.5f),new Vector3(l,b,-.5f),new Vector3(r,b,-.5f),new Vector3(r,b,.5f));
            Quad(new Vector3(l,b,.5f),new Vector3(l,t,.5f),new Vector3(l,t,-.5f),new Vector3(l,b,-.5f));
            Quad(new Vector3(r,b,-.5f),new Vector3(r,t,-.5f),new Vector3(r,t,.5f),new Vector3(r,b,.5f));
        }
        var mesh=new Mesh{name="Solid tile silhouette"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
    }
    static Material MaterialFor(PlannedEntry entry,Material original)
    {
        string packRoot="Assets/Art/FreeLibrary/"+entry.pack;
        if(!atlases.TryGetValue(entry.pack,out string atlas))
        {atlas=Directory.GetFiles(packRoot,"*.png",SearchOption.AllDirectories).Where(p=>p.Replace('\\','/').Contains("FBX format/Textures/")).OrderBy(p=>p).FirstOrDefault();atlases[entry.pack]=atlas;}
        var originalTexture=original!=null ? original.mainTexture : null;
        Color originalColor=original!=null && original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original!=null && original.HasProperty("_Color") ? original.color : Color.white;
        string key=entry.pack+"-"+(atlas!=null ? "atlas" : Hash128.Compute((original!=null ? original.name : "default")+"/"+AssetDatabase.GetAssetPath(originalTexture)+"/"+ColorUtility.ToHtmlStringRGBA(originalColor)).ToString());
        if(materials.TryGetValue(key,out var cached))return cached;
        string path=Output+"/Materials/"+key+".mat";Parent(path);var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(ComicPalette.Shader);AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",atlas!=null ? Color.white : originalColor);
        var texture=atlas!=null ? AssetDatabase.LoadAssetAtPath<Texture2D>(atlas.Replace('\\','/')) : original!=null ? original.mainTexture : null;
        if(texture!=null)mat.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(mat);materials[key]=mat;return mat;
    }
}
#endif
