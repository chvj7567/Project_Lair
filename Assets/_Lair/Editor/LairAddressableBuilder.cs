using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Addressables 콘텐츠 빌드 진입점. 메인 오케스트레이터가 UnityMCP 메뉴 실행으로 호출.
    //# 빌드 결과/에러를 콘솔에 명시 로깅 — 리플렉션 직접 호출 시 가려지는 내부 예외를 드러냄.
    public static class LairAddressableBuilder
    {
        [MenuItem("Lair/Build Addressables")]
        public static void BuildAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairAddressableBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }

            Debug.Log($"[LairAddressableBuilder] 빌드 시작 — profile={settings.activeProfileId}, groups={settings.groups.Count}");

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            if (result != null && string.IsNullOrEmpty(result.Error) == false)
            {
                Debug.LogError($"[LairAddressableBuilder] 빌드 실패: {result.Error}");
                return;
            }

            string outputPath = result != null ? result.OutputPath : "(unknown)";
            double seconds = result != null ? result.Duration : 0;
            Debug.Log($"[LairAddressableBuilder] 빌드 완료 — output={outputPath}, {seconds:F1}s");
        }
    }
}
