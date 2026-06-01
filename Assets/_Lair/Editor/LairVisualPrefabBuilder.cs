using System.IO;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using TMPro;
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
            //# 출혈 — #A01346 (= HitFeedbackPalette.Bleed, 데미지 숫자색과 동일. 기획서 §1.1/§8.5).
            new VisualSpec { Key = EVisual.BleedStatus,      Mesh = PrimitiveType.Sphere, ColorHex = "#A01346", Alpha = 1.0f, Scale = 0.25f },
        };

        [MenuItem("Lair/Setup/B1 - Build Visual Prefabs")]
        public static void BuildAllVisuals()
        {
            EnsureDir(PrefabDir);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairVisualPrefabBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);

            //# PoisonAura — 비균일 스케일(디스크)이라 special-case 유지.
            BuildPoisonAura(settings, group);

            //# 상태 표시 6종 — 일반 BuildVisual.
            foreach (VisualSpec spec in StatusSpecs)
                BuildVisual(spec, settings, group);

            //# 타격 피드백 FX 2종 (2026-06-01).
            BuildHitImpact(settings, group);
            BuildDamagePopup(settings, group);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairVisualPrefabBuilder] Visual 프리팹 빌드 완료 (PoisonAura + 상태 6종 + HitImpact/DamagePopup)");
        }

        //# 균일 스케일 부착물 visual 1종 생성.
        private static void BuildVisual(VisualSpec spec, AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            string prefabName = spec.Key.ToString();

            GameObject go = GameObject.CreatePrimitive(spec.Mesh);
            go.name = prefabName;
            go.transform.localScale = Vector3.one * spec.Scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            ColorUtility.TryParseHtmlString(spec.ColorHex, out Color c);
            c.a = spec.Alpha;

            string matPath = $"{MaterialDir}/Mat_{prefabName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool created = mat == null;
            if (created)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));

                //# 반투명이면 URP Lit Transparent Surface 셋업 (생성 시 1회).
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
            }

            //# 색은 항상 덮어쓴다 — 기존 .mat 도 신규 hex 로 강제 갱신(기획서 §8.5 NIT).
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            if (created)
                AssetDatabase.CreateAsset(mat, matPath);
            else
                EditorUtility.SetDirty(mat);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairVisualPrefabBuilder] {prefabName} 프리팹 생성 + Addressables 등록");
        }

        private static void BuildPoisonAura(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = nameof(EVisual.PoisonAura);

            //# Cylinder 디스크 — 직경 2.5, 두께 0.1
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = PrefabName;
            go.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 독 — #0B5B4A (= HitFeedbackPalette.Poison, 데미지 숫자색과 동일. 기획서 §1.1/§8.5).
            Color c = new Color(0.043f, 0.357f, 0.290f, 1f);

            string matPath = $"{MaterialDir}/Mat_PoisonAura.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool created = mat == null;
            if (created)
                mat = new Material(Shader.Find(UrpLitShaderName));

            //# 색은 항상 덮어쓴다 — 기존 .mat(#84CC16) 강제 교체(기획서 §8.5 NIT).
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            if (created)
                AssetDatabase.CreateAsset(mat, matPath);
            else
                EditorUtility.SetDirty(mat);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            string prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = PrefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairVisualPrefabBuilder] {PrefabName} 프리팹 생성 + Addressables 등록");
        }

        //# HitImpact — 텍스처 없는 Cube 메시 버스트 파티클 (기획서 §5). 색은 런타임 스탬프.
        private static void BuildHitImpact(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = nameof(EVisual.HitImpact);

            GameObject go = new GameObject(PrefabName);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            //# 모듈 셋업 — 1회 6버스트, 수명 0.35초, 구면 방향, 중력 0.
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 1.5f;
            main.startSize = 0.12f;
            main.gravityModifier = 0f;
            main.maxParticles = 32;

            //# 0개 지속 방출 + 6개 단일 버스트.
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            //# Mesh 렌더 — 텍스처 없는 프리미티브 Cube.
            ParticleSystemRenderer rd = go.GetComponent<ParticleSystemRenderer>();
            rd.renderMode = ParticleSystemRenderMode.Mesh;
            rd.mesh = BuiltinCubeMesh();
            Material mat = EnsureParticleMaterial();
            rd.sharedMaterial = mat;

            //# 자동 풀 반환 — 수명 0.35 + 여유 0.1 = 0.45 (CHPoolable + ReturnToPoolAfter).
            go.AddComponent<CHPoolable>();
            go.AddComponent<ReturnToPoolAfter>();

            string prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            RegisterAddressable(settings, group, prefabPath, PrefabName);
            Debug.Log($"[LairVisualPrefabBuilder] {PrefabName} 프리팹 생성 + Addressables 등록");
        }

        //# DamagePopup — 월드스페이스 TMP(3D) + CHText + DamagePopup + CHPoolable (기획서 §4).
        private static void BuildDamagePopup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = nameof(EVisual.DamagePopup);

            GameObject go = new GameObject(PrefabName);

            //# 자식 TMP(3D) — fontSize 4, outlineWidth 0.1 (정규화 SDF). 외곽선 색은 런타임 명도 분기.
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = "0";
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            //# 전용 머티리얼 에셋 — 공유 기본 폰트 머티리얼에 outline 을 쓰면 전 게임 TMP 가 오염된다.
            //# 전용 .mat 을 먼저 배정한 뒤 outline 속성을 설정 (순서 중요).
            //# outline 은 TMP 프로퍼티 세터 대신 공유 머티리얼 에셋에 직접 쓴다.
            //# 세터는 edit mode 에서 renderer.material 인스턴스를 생성해 머티리얼을 누수시킨다(기획서 §4.2).
            Material popupMat = EnsureDamagePopupFontMaterial(tmp.fontSharedMaterial);
            if (popupMat != null)
            {
                tmp.fontSharedMaterial = popupMat;
                popupMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.1f);   //# TMP 정규화 SDF 단위
                popupMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.102f, 0.102f, 0.102f, 1f));   //# #1A1A1A 기본 (런타임 명도 분기로 갱신)
                popupMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                EditorUtility.SetDirty(popupMat);
            }
            RectTransform tmpRt = textGo.GetComponent<RectTransform>();
            if (tmpRt != null)
                tmpRt.sizeDelta = new Vector2(4f, 2f);
            textGo.AddComponent<CHText>();

            DamagePopup popup = go.AddComponent<DamagePopup>();
            SetObjectField(popup, "_text", tmp);
            go.AddComponent<CHPoolable>();

            string prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            RegisterAddressable(settings, group, prefabPath, PrefabName);
            Debug.Log($"[LairVisualPrefabBuilder] {PrefabName} 프리팹 생성 + Addressables 등록");
        }

        //# DamagePopup 전용 폰트 머티리얼 — 기본 폰트 공유 머티리얼 복제본.
        //# 공유 머티리얼에 outline 을 쓰면 전 게임 TMP 가 오염되므로 전용 에셋으로 격리.
        private static Material EnsureDamagePopupFontMaterial(Material sourceShared)
        {
            const string MatPath = MaterialDir + "/Mat_DamagePopupFont.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                if (sourceShared == null)
                    return null;
                mat = new Material(sourceShared);
                mat.name = "Mat_DamagePopupFont";
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            return mat;
        }

        //# 파티클용 단색 URP Lit 머티리얼 (텍스처 없음). 색은 런타임 _BaseColor 스탬프로 덮임.
        private static Material EnsureParticleMaterial()
        {
            const string MatPath = MaterialDir + "/Mat_HitImpact.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                Color c = Color.white;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            return mat;
        }

        //# 빌트인 Cube 메시 핸들 — 임시 프리미티브 생성 후 메시만 채취.
        private static Mesh BuiltinCubeMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        private static void RegisterAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string prefabPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);
        }

        //# SerializedObject 로 private [SerializeField] 참조 주입.
        private static void SetObjectField(Component target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVisualPrefabBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDir(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
