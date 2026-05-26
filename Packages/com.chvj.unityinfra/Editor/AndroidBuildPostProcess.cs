#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    /// <summary>
    /// Gradle 프로젝트 생성 후 AndroidManifest.xml에 광고 ID 권한(com.google.android.gms.permission.AD_ID)을 추가.
    /// GoogleMobileAds SDK 자체 매니페스트에 이미 있을 수 있으나, Manifest Merger가 중복을 제거하므로 안전.
    /// 키즈 앱은 별도 매니페스트로 tools:node="remove" 처리 필요.
    /// </summary>
    public class AndroidBuildPostProcess : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNS = "http://schemas.android.com/apk/res/android";
        private const string Permission = "com.google.android.gms.permission.AD_ID";

        public int callbackOrder => 900;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath)) return;

            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("android", AndroidNS);

            var existing = doc.SelectSingleNode(
                $"/manifest/uses-permission[@android:name='{Permission}']", nsMgr);
            if (existing != null) return;

            var manifest = doc.SelectSingleNode("/manifest");
            if (manifest == null) return;

            var elem = doc.CreateElement("uses-permission");
            var attr = doc.CreateAttribute("android", "name", AndroidNS);
            attr.Value = Permission;
            elem.Attributes.Append(attr);
            manifest.AppendChild(elem);

            doc.Save(manifestPath);
            Debug.Log($"[CHM] AndroidManifest.xml에 {Permission} 권한 추가됨");
        }
    }
}
#endif
