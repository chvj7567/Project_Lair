#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    /// <summary>
    /// iOS 빌드 후 Info.plist에 ATT(NSUserTrackingUsageDescription)를 자동 추가.
    /// Player Settings 등에서 이미 키가 설정된 경우 덮어쓰지 않는다.
    /// </summary>
    public static class IOSBuildPostProcess
    {
        // 기본 ATT 사용 설명. 게임 측에서 다른 메시지를 원하면 Player Settings의 iOSUserTrackingUsageDescription을 채워두면 우선 적용됨.
        private const string DefaultATTUsageDescription =
            "광고 개인화 및 측정을 위해 광고 식별자 사용을 허용해주세요.";

        [PostProcessBuild(900)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath)) return;

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            const string key = "NSUserTrackingUsageDescription";
            if (!plist.root.values.ContainsKey(key))
            {
                plist.root.SetString(key, DefaultATTUsageDescription);
                Debug.Log($"[CHM] Info.plist에 {key} 추가됨");
                plist.WriteToFile(plistPath);
            }
        }
    }
}
#endif
