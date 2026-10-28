#if TREASURERUNNER_ADMIN || UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
namespace TreasureRunner
{
    // Windows stores the token for this account. It is never included in level files,
    // PlayerPrefs, source control or public tool packages.
    public static class ModeratorCredential
    {
        const string Target="TreasureRunner/CommunityModeration";
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Credential
        {
            public uint flags,type;public string target,comment;public System.Runtime.InteropServices.ComTypes.FILETIME written;
            public uint size;public IntPtr blob;public uint persist,attributeCount;public IntPtr attributes;public string alias,user;
        }
        [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool Read(string target,uint type,uint flags,out IntPtr result);
        [DllImport("advapi32.dll",EntryPoint="CredWriteW",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool Write(ref Credential credential,uint flags);
        [DllImport("advapi32.dll",EntryPoint="CredDeleteW",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool Delete(string target,uint type,uint flags);
        [DllImport("advapi32.dll")] static extern void CredFree(IntPtr value);
        public static string Load()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(DesktopTools.Smoke)return "";
            try{if(!Read(Target,1,0,out var ptr))return "";try{var c=Marshal.PtrToStructure<Credential>(ptr);return c.size<=1200 ? Marshal.PtrToStringUni(c.blob,(int)c.size/2) : "";}finally{CredFree(ptr);}}catch{return "";}
#else
            return "";
#endif
        }
        public static bool Store(string token)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(DesktopTools.Smoke)return false;
            IntPtr blob=IntPtr.Zero;
            try{if(string.IsNullOrEmpty(token)){return Delete(Target,1,0) || Marshal.GetLastWin32Error()==1168;}if(token.Length>300)return false;
                blob=Marshal.StringToCoTaskMemUni(token);var c=new Credential{type=1,target=Target,user="TreasureRunner",size=(uint)(token.Length*2),blob=blob,persist=2};return Write(ref c,0);
            }catch{return false;}finally{if(blob!=IntPtr.Zero)Marshal.ZeroFreeCoTaskMemUnicode(blob);}
#else
            return false;
#endif
        }
    }
}
#endif
