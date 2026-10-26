using UnityEngine;
namespace TreasureRunner
{
    public static class ComicPalette
    {
        public static readonly Color[] Sky={Hex(0x79DDF4),Hex(0x9CB7FA),Hex(0xFFC782),Hex(0x6D4182),Hex(0x454181),Hex(0x344B83),Hex(0x99C7ED),Hex(0xB897EF)};
        public static readonly Color[] Ground={Hex(0x8AD74E),Hex(0xB5F4FC),Hex(0xF7CC6F),Hex(0xA35873),Hex(0x8982C7),Hex(0x51B5A0),Hex(0xA9A6D1),Hex(0xE098E9)};
        public static readonly Color[] Earth={Hex(0xA56042),Hex(0x478FCC),Hex(0xCA814B),Hex(0x563B68),Hex(0x554B88),Hex(0x545887),Hex(0x6E6BA4),Hex(0x7764B4)};
        public static readonly Color[] Accent={Hex(0xFF85AF),Hex(0xF5E7FF),Hex(0x46C9C0),Hex(0xFFBC49),Hex(0x53EED9),Hex(0xFFA3E3),Hex(0xFF6188),Hex(0x78F0E8)};
        public static Color Hex(int rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);
        public static Shader Shader=>Resources.Load<Shader>("ComicSurface") ?? UnityEngine.Shader.Find("Universal Render Pipeline/Simple Lit");
        public static Material Material(string name,Color color)
        {var m=new Material(Shader){name=name};m.SetColor("_BaseColor",color);return m;}
    }
}
