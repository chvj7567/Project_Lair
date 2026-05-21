using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 시각 이펙트 프리팹 자동 생성 (Rule 12 — CHMPool 사용 대상).
    //# PoisonAura (영웅 발 밑 연두 디스크) + 영웅 디버프 상태 표시 6종.
    public static class LairVisualPrefabBuilder
    {
        public const string PrefabDir     = "Assets/_Lair/Art/FX";
        public const string MaterialDir   = "Assets/_Lair/Art/Materials";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        //# 일반 visual 빌드 스펙 — 균일 스케일 부착물.
        public class VisualSpec
        {
            public EVisual Key;
            public PrimitiveType Mesh;
            public string ColorHex;
            public float Alpha;
            public float Scale;
        }

        //# 영웅 디버프 상태 표시 6종 (설계서 §3 표).
        public static readonly VisualSpec[] StatusSpecs = new[]
        {
            new VisualSpec { Key = EVisual.SlowStatus,       Mesh = PrimitiveType.Sphere, ColorHex = "#0EA5E9", Alpha = 0.5f, Scale = 0.4f  },
            new VisualSpec { Key = EVisual.FearStatus,       Mesh = PrimitiveType.Cube,   ColorHex = "#A855F7", Alpha = 1.0f, Scale = 0.3f  },
            new VisualSpec { Key = EVisual.WeakenStatus,     Mesh = PrimitiveType.Cube,   ColorHex = "#6B7280", Alpha = 1.0f, Scale = 0.3f  },
            new VisualSpec { Key = EVisual.AttackDownStatus, Mesh = PrimitiveType.Cube,   ColorHex = "#7F1D1D", Alpha = 1.0f, Scale = 0.25f },
            new VisualSpec { Key = EVisual.TimeStopStatus,   Mesh = PrimitiveType.Sphere, ColorHex = "#E5E7EB", Alpha = 0.3f, Scale = 1.5f  },
            new VisualSpec { Key = EVisual.BleedStatus,      Mesh = PrimitiveType.Sphere, ColorHex = "#DC2626", Alpha = 1.0f, Scale = 0.25f },
        };

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

            //# PoisonAura — 비균일 스케일(디스크)이라 special-case 유지.
            BuildPoisonAura(settings, group);

            //# 상태 표시 6종 — 일반 BuildVisual.
            foreach (var spec in StatusSpecs)
                BuildVisual(spec, settings, group);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairVisualPrefabBuilder] Visual 프리팹 빌드 완료 (PoisonAura + 상태 6종)");
        }

        //# 균일 스케일 부착물 visual 1종 생성.
        private static void BuildVisual(VisualSpec spec, AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            string prefabName = spec.Key.ToString();

            var go = GameObject.CreatePrimitive(spec.Mesh);
            go.name = prefabName;
            go.transform.localScale = Vector3.one * spec.Scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var matPath = $"{MaterialDir}/Mat_{prefabName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                ColorUtility.TryParseHtmlString(spec.ColorHex, out var c);
                c.a = spec.Alpha;

                //# 반투명이면 URP Lit Transparent Surface 셋업.
                if (spec.Alpha < 1f)
                {
                    mat.SetFloat("_Surface", 1f);   //# 0=Opaque, 1=Transparent
                    mat.SetFloat("_Blend", 0f);     //# 0=Alpha
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                }

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairVisualPrefabBuilder] {prefabName} 프리팹 생성 + Addressables 등록");
        }

        private static void BuildPoisonAura(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = nameof(EVisual.PoisonAura);

            //# Cylinder 디스크 — 직경 2.5, 두께 0.1
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = PrefabName;
            go.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var matPath = $"{MaterialDir}/Mat_PoisonAura.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                var c = new Color(0.518f, 0.8f, 0.086f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

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
