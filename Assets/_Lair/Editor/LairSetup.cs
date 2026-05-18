using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Slice A 셋업 자동화. Build Settings 등록, Addressables 그룹/라벨 생성 등 1회성 작업.
    //# UnityMCP editor_invoke_method 또는 메뉴(Lair/Setup/*)에서 호출.
    public static class LairSetup
    {
        public const string BattleScenePath = "Assets/_Lair/Scenes/Battle.unity";
        public const string ResourceGroupName = "Resource";
        public const string ResourceLabel = "Resource";

        [MenuItem("Lair/Setup/M1 - Ensure Battle Scene In Build Settings")]
        public static void EnsureBattleSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            //# Battle.unity 가 Index 0 이 되도록 보장 — 다른 위치에 있으면 제거 후 맨 앞 삽입
            scenes.RemoveAll(s => s.path == BattleScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(BattleScenePath, true));

            //# Slice A 한정: SampleScene 은 비활성화 (삭제 X)
            for (int i = 0; i < scenes.Count; ++i)
            {
                if (scenes[i].path != BattleScenePath && scenes[i].path.Contains("SampleScene"))
                {
                    scenes[i] = new EditorBuildSettingsScene(scenes[i].path, false);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[LairSetup] Build Settings 갱신: Battle@0, 총 {scenes.Count} 씬");
        }

        [MenuItem("Lair/Setup/M1 - Ensure Addressables Setup")]
        public static void EnsureAddressablesSetup()
        {
            //# 1) AddressableAssetSettings 생성 (없으면)
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                Debug.Log("[LairSetup] AddressableAssetSettings 신규 생성");
            }

            //# 2) "Resource" 그룹 생성 (이미 있으면 스킵)
            var group = settings.FindGroup(ResourceGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    ResourceGroupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema)
                );
                Debug.Log("[LairSetup] 'Resource' 그룹 생성");
            }
            else
            {
                Debug.Log("[LairSetup] 'Resource' 그룹 이미 존재");
            }

            //# 3) "Resource" 라벨 추가 (이미 있으면 스킵)
            if (settings.GetLabels().Contains(ResourceLabel) == false)
            {
                settings.AddLabel(ResourceLabel);
                Debug.Log("[LairSetup] 'Resource' 라벨 추가");
            }
            else
            {
                Debug.Log("[LairSetup] 'Resource' 라벨 이미 존재");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Lair/Setup/M1 - Run All")]
        public static void RunAllM1Setup()
        {
            EnsureBattleSceneInBuildSettings();
            EnsureAddressablesSetup();
            Debug.Log("[LairSetup] M1 셋업 완료");
        }
    }
}
