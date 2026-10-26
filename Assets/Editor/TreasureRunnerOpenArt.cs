#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class TreasureRunnerOpenArt
{
    public static void Prepare()
    {
        string root="Assets/Resources/OpenArt";Directory.CreateDirectory(root+"/Prefabs");Directory.CreateDirectory(root+"/Materials");
        foreach(var group in new[]{"Nature","Dungeon","Skeletons","Platformer"})
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/"+group+"/"+(group=="Dungeon" ? "dungeon_texture" : group=="Platformer" ? "colormap" : "skeleton_texture")+".png");
            Material shared=null;
            if(group!="Nature")
            {
                string path=root+"/Materials/"+group+".mat";shared=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(shared==null){shared=new Material(TreasureRunner.ComicPalette.Shader);AssetDatabase.CreateAsset(shared,path);}
                shared.SetColor("_BaseColor",Color.white);shared.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(shared);
            }
            foreach(string modelPath in Directory.GetFiles(root+"/"+group,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                if(group=="Skeletons" && Path.GetFileName(modelPath).StartsWith("Skeleton_") && new FileInfo(modelPath).Length>1000000)
                {
                    importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;
                    var available=importer.defaultClipAnimations;
                    File.AppendAllText("Logs/OpenArtAnimations.txt",Path.GetFileName(modelPath)+": "+string.Join(", ",available.Select(c=>c.name))+"\n");
                    var selected=new System.Collections.Generic.HashSet<string>{"Idle","Walking_A","Hit_A","Death_A","1H_Melee_Attack_Slice_Horizontal","1H_Ranged_Shoot","Spellcast_Shoot"};
                    var chosen=available.Where(c=>selected.Contains(c.name)).ToArray();
                    foreach(var c in chosen){c.loopTime=c.name.Contains("Idle")||c.name.Contains("Walking");c.wrapMode=c.loopTime ? WrapMode.Loop : WrapMode.Once;}
                    if(chosen.Length>0)importer.clipAnimations=chosen;
                    importer.animationCompression=ModelImporterAnimationCompression.Optimal;
                }
                else importer.importAnimation=false;
                importer.isReadable=true;importer.SaveAndReimport();
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);if(source==null)throw new Exception("Missing model "+modelPath);
                var instance=UnityEngine.Object.Instantiate(source);instance.name=(group=="Platformer" ? "Platformer_" : "")+Path.GetFileNameWithoutExtension(modelPath);
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        if(shared!=null)slots[i]=shared;
                        else
                        {
                            var original=slots[i];string name=original!=null ? original.name : "Nature";
                            string materialPath=root+"/Materials/Nature-"+name.Replace('/','_')+".mat";
                            var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                            if(mat==null){mat=new Material(TreasureRunner.ComicPalette.Shader);mat.SetColor("_BaseColor",original!=null ? original.color : Color.green);AssetDatabase.CreateAsset(mat,materialPath);}
                            slots[i]=mat;
                        }
                    }
                    renderer.sharedMaterials=slots;
                }
                foreach(var collider in instance.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                PrefabUtility.SaveAsPrefabAsset(instance,root+"/Prefabs/"+instance.name+".prefab");UnityEngine.Object.DestroyImmediate(instance);
            }
        }
        foreach(string p in Directory.GetFiles(root+"/Particles","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.SaveAndReimport();
        }
        string particlePath=root+"/Materials/Particle.mat";
        var particle=AssetDatabase.LoadAssetAtPath<Material>(particlePath);
        if(particle==null){particle=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(particle,particlePath);}
        particle.SetFloat("_Surface",1);particle.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);particle.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);particle.SetFloat("_ZWrite",0);particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particle.renderQueue=3000;EditorUtility.SetDirty(particle);
        AssetDatabase.SaveAssets();Debug.Log("OpenArt prepared without changing game version or creating APK.");
    }
}
#endif
