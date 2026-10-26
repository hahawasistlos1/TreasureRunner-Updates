#if UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
// Used only during a later explicitly requested Android build. Does not start a build itself.
public class TreasureRunnerIdentityAndroidSetup:IPostGenerateGradleAndroidProject
{
    public int callbackOrder=>200;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var root=Directory.GetParent(path).FullName;var properties=Path.Combine(root,"gradle.properties");
        if(!File.Exists(properties))throw new FileNotFoundException("Generated Android Gradle properties missing",properties);
        var lines=File.ReadAllLines(properties);bool found=false;
        for(int i=0;i<lines.Length;i++)if(lines[i].TrimStart().StartsWith("android.useAndroidX=")){lines[i]="android.useAndroidX=true";found=true;}
        File.WriteAllText(properties,string.Join("\n",lines)+(found ? "\n" : "\nandroid.useAndroidX=true\n"));
    }
}
#endif
