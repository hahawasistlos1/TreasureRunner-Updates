using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace TreasureRunner
{
    public static class EditorTestSession
    {
        static readonly HashSet<string> passed=new HashSet<string>();
        static string active;
        public static bool IsTest=>active!=null;
        public static bool ReturnToEditor;
        public static string Fingerprint(CommunityLevel level)
        {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(level)))).Replace("-","");
        }
        public static bool Passed(CommunityLevel level)
        {
            string hash=Fingerprint(level);return passed.Contains(hash) || (!Application.isEditor && !DesktopTools.Smoke && PlayerPrefs.GetInt("TR.LevelTest."+hash,0)==1);
        }
        public static void Begin(CommunityLevel level){active=Fingerprint(level);}
        public static bool Complete(CommunityLevel level)
        {
            if(active==null || active!=Fingerprint(level))return false;
            passed.Add(active);if(!Application.isEditor && !DesktopTools.Smoke){PlayerPrefs.SetInt("TR.LevelTest."+active,1);PlayerPrefs.Save();}return true;
        }
        public static void Clear(){active=null;}
    }
}
