using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# B1 — 시각 이펙트 프리팹 자동 생성 (Rule 12 — CHMPool 사용 대상).
    //# 현재: PoisonAura (영웅 발 밑 연두 디스크).
    public static class LairVisualPrefabBuilder
    {
        public const string PrefabDir     = "Assets/_Lair/Prefabs/FX";
        public const string MaterialDir   = "Assets/_Lair/Prefabs/FX";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        [MenuItem("Lair/Setup/B1 - Build Visual Prefabs")]
        public static void BuildAllVisuals()
        {
            EnsureDir(PrefabDir);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                LairSetup.EnsureAddressablesSetup();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            var group = settings.FindGroup(ResourceGroup);

            BuildPoisonAura(settings, group);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairVisualPrefabBuilder] Visual 프리팹 빌드 완료");
        }

        private static void BuildPoisonAura(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = nameof(EVisual.PoisonAura);

            //# Cylinder 디스크 — 직경 2.5, 두께 0.1
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = PrefabName;
            go.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);

            //# Collider 제거 — Slice A 의 충돌 미사용 정책
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 머티리얼 — URP Lit 연두 #84CC16
            var matPath = $"{MaterialDir}/Mat_PoisonAura.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var c = new Color(0.518f, 0.8f, 0.086f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            //# 프리팹 저장
            var prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            //# Addressables 등록 — address = Enum 값명 (Rule 08)
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = PrefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairVisualPrefabBuilder] {PrefabName} 프리팹 생성 + Addressables 등록");
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
