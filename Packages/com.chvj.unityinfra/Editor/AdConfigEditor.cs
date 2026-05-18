#if UNITY_INFRA_ADS
using UnityEditor;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    [CustomEditor(typeof(AdConfig))]
    public class AdConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "에디터 빌드에서는 항상 테스트 광고가 사용됩니다.\n" +
                "디바이스 빌드에서만 프로덕션 ID와 UseTestAds 옵션이 반영됩니다.\n" +
                "프로덕션 BannerAdUnitId가 비어있으면 디바이스 빌드에서도 자동 테스트 광고로 fallback.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
#endif
