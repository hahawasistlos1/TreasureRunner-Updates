using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
namespace TreasureRunner
{
    public static class BackgroundUpdateNotifications
    {
        public const string OpenUrl="treasurerunner://updates";
        public const string PermissionName="android.permission.POST_NOTIFICATIONS";
        static string problem;
        public static bool Supported=>Application.platform==RuntimePlatform.Android && !Application.isEditor;
        public static bool IsUpdateLink(string url)=>string.Equals(url,OpenUrl,StringComparison.OrdinalIgnoreCase);
        public static void Initialize(UpdateChannel channel)
        {
            bool allowed=channel!=null && channel.enabled && channel.owner=="hahawasistlos1" && channel.repository=="TreasureRunner-Updates";
            Call((bridge,activity)=>{bridge.CallStatic("configure",activity,allowed);return true;},false);
            SetVisible(Application.isFocused);
        }
        public static void SetVisible(bool visible)=>Call((bridge,activity)=>{bridge.CallStatic("setVisible",visible);if(visible)bridge.CallStatic("ensureScheduled",activity);return true;},false);
        public static bool Enabled=>Call((bridge,activity)=>bridge.CallStatic<bool>("enabled",activity),false);
        public static bool Permitted=>Call((bridge,activity)=>bridge.CallStatic<bool>("permitted",activity),false);
        public static string Status=>!Supported ? "Benachrichtigungen bei geschlossenem Spiel stehen auf Android zur Verfügung." :
            Call((bridge,activity)=>bridge.CallStatic<string>("status",activity),problem??"Android-Updatebenachrichtigungen konnten nicht geladen werden.");
        public static void Enable()
        {
            if(!Supported)return;
            Call((bridge,activity)=>{bridge.CallStatic("setEnabled",activity,true);return true;},false);
#if UNITY_ANDROID && !UNITY_EDITOR
            int sdk=0;using(var version=new AndroidJavaClass("android.os.Build$VERSION"))sdk=version.GetStatic<int>("SDK_INT");
            if(sdk>=33 && !Permission.HasUserAuthorizedPermission(PermissionName))
            {
                var callbacks=new PermissionCallbacks();
                callbacks.PermissionGranted+=permission=>EnsureAndCheck();
                callbacks.PermissionDenied+=permission=>EnsureScheduled();
                Permission.RequestUserPermission(PermissionName,callbacks);
            }
            else EnsureAndCheck();
#endif
        }
        public static void Disable()=>Call((bridge,activity)=>{bridge.CallStatic("setEnabled",activity,false);return true;},false);
        public static void EnsureScheduled()=>Call((bridge,activity)=>{bridge.CallStatic("ensureScheduled",activity);return true;},false);
        static void EnsureAndCheck()=>Call((bridge,activity)=>{bridge.CallStatic("ensureScheduled",activity);bridge.CallStatic("scheduleFirst",activity);return true;},false);
        public static void OpenSettings()=>Call((bridge,activity)=>{bridge.CallStatic("openSettings",activity);return true;},false);
        public static void Acknowledge(string version)=>Call((bridge,activity)=>{bridge.CallStatic("acknowledge",activity,version);return true;},false);
        static T Call<T>(Func<AndroidJavaClass,AndroidJavaObject,T> action,T fallback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var bridge=new AndroidJavaClass("com.treasurerunner.updates.UpdateNotifications"))
                {var result=action(bridge,activity);problem=null;return result;}
            }
            catch(AndroidJavaException error){problem="Android-Updatebenachrichtigungen sind momentan nicht verfügbar.";Debug.LogWarning("Background update notices: "+error.GetType().Name);}
#endif
            return fallback;
        }
    }
}
