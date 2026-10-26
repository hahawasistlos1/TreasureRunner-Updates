using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
[assembly:AssemblyVersion("0.9.0.0")]
[assembly:AssemblyFileVersion("0.9.0.0")]
[assembly:AssemblyProduct("TreasureRunner Windows-Werkzeuge")]
static class Launcher
{
    [STAThread] static void Main()
    {
        string root=AppDomain.CurrentDomain.BaseDirectory;
        string runtime=Path.Combine(root,"Runtime","TreasureRunnerTools.exe");
        if(!File.Exists(runtime)){MessageBox.Show("Der Ordner Runtime fehlt. Bitte das vollständige Werkzeug-Paket entpacken.","TreasureRunner");return;}
        string mode=Path.GetFileNameWithoutExtension(Application.ExecutablePath).Contains("Verwaltung") ? "--level-admin" : "--level-editor";
        Process.Start(new ProcessStartInfo(runtime,mode){WorkingDirectory=root,UseShellExecute=false});
    }
}
