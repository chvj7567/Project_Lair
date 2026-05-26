using System.IO;
using Lair.Battle;
using Lair.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# 씬에 배치된 Spawner(6개)에 SpawnerBody 자식(Cylinder 디스크) + CooldownBarWrapper 자식을 부착.
    //# LairCharacterPrefabBuilder.AttachMonsterHpBar 패턴을 따름 (기획서 §6).
    //# Rule 12 예외: CreatePrimitive 는 에디터 빌더에서만 허용.
    //# 이미 존재하는 자식은 스킵 (idempotent).
    public static class LairSpawnerVisualBuilder
    {
        private const string MaterialDir   = "Assets/_Lair/Art/Materials";
        private const string HpBarPrefabPath = "Assets/_Lair/Art/UI/HpBar.prefab";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        //# EMonster 순서(0=Wisp, 1=Wraith, 2=Reaper, 3=Hex, 4=Plague, 5=Phantom)와 1:1 대응.
        private static readonly (EMonster Type, string ColorHex)[] SpawnerColorTable = new[]
        {
            (EMonster.Wisp,    "#22C55E"),
            (EMonster.Wraith,  "#6B7280"),
            (EMonster.Reaper,  "#EF4444"),
            (EMonster.Hex,     "#EAB308"),
            (EMonster.Plague,  "#A855F7"),
            (EMonster.Phantom, "#1F2937"),
        };

        [MenuItem("Lair/Setup/S1 - Attach Spawner Visuals")]
        public static void AttachSpawnerVisuals()
        {
            //# 씬의 모든 Spawner (disabled 포함) 탐색.
            var spawners = Object.FindObjectsOfType<Spawner>(includeInactive: true);
            if (spawners.Length == 0)
            {
                Debug.LogWarning("[LairSpawnerVisualBuilder] 씬에 Spawner 를 찾을 수 없음");
                return;
            }

            //# 6종 머티리얼을 미리 생성/로드해 두어 반복 참조.
            var mats = EnsureSpawnerMaterials();

            var hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath);
            if (hpBarPrefab == null)
            {
                Debug.LogError("[LairSpawnerVisualBuilder] HpBar.prefab 미발견: " + HpBarPrefabPath);
                return;
            }

            int processed = 0;
            foreach (var spawner in spawners)
            {
                bool changed = false;
                changed |= EnsureSpawnerBody(spawner, mats);
                changed |= EnsureCooldownBarWrapper(spawner, hpBarPrefab);

                if (changed)
                {
                    EditorUtility.SetDirty(spawner.gameObject);
                    EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                    processed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LairSpawnerVisualBuilder] {spawners.Length}개 Spawner 중 {processed}개 수정 완료");
        }

        //# SpawnerBody 자식 생성 — 이미 있으면 false 반환(스킵).
        private static bool EnsureSpawnerBody(Spawner spawner, Material[] mats)
        {
            if (spawner.transform.Find("SpawnerBody") != null) return false;

            //# Cylinder 납작 디스크 생성 (Rule 12 에디터 예외).
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "SpawnerBody";
            body.transform.SetParent(spawner.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(2.0f, 0.05f, 2.0f);

            //# Collider 제거 — 전투 충돌 영향 없도록 (기획서 §2.1).
            var col = body.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# _currentType 초기 읽기 — Spawner._outputType 의 기본값(Wisp) 이 직렬화에 반영됨.
            //# SerializedObject 로 직렬화 필드를 읽어 초기 머티리얼 인덱스 결정.
            int initIndex = GetOutputTypeIndex(spawner);
            var renderer = body.GetComponent<Renderer>();
            if (initIndex >= 0 && initIndex < mats.Length && mats[initIndex] != null)
                renderer.sharedMaterial = mats[initIndex];

            //# SpawnerBody 컴포넌트 부착 — _renderer + _materials 주입.
            var bodyComp = body.AddComponent<SpawnerBody>();
            SetPrivateField(bodyComp, "_renderer", renderer);
            SetPrivateField(bodyComp, "_materials", mats);

            return true;
        }

        //# CooldownBarWrapper 자식 생성 — 이미 있으면 false 반환(스킵).
        private static bool EnsureCooldownBarWrapper(Spawner spawner, GameObject hpBarPrefab)
        {
            if (spawner.transform.Find("CooldownBarWrapper") != null) return false;

            const float BarPixelW    = 120f;
            const float TargetWorldW = 1.2f;
            const float SpawnerScale = 1.0f;   //# Spawner 씬 배치 스케일 = 1 고정.
            const float LocalY       = 0.5f;   //# 기획서 §3.4 — 디스크 살짝 위.

            //# 래퍼 — WorldSpace Canvas + SpawnerCooldownBar.
            var wrapper = new GameObject("CooldownBarWrapper", typeof(RectTransform), typeof(Canvas));
            wrapper.transform.SetParent(spawner.transform, worldPositionStays: false);
            var wrapperCanvas = wrapper.GetComponent<Canvas>();
            wrapperCanvas.renderMode = RenderMode.WorldSpace;
            var wrapperRt = (RectTransform)wrapper.transform;
            wrapperRt.sizeDelta = new Vector2(BarPixelW, 20f);
            wrapperRt.localScale = Vector3.one * (TargetWorldW / BarPixelW / SpawnerScale);
            wrapperRt.localRotation = Quaternion.identity;
            wrapperRt.localPosition = new Vector3(0f, LocalY, 0f);

            //# HpBar.prefab 인스턴스 — 래퍼 자식 full-stretch.
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab, wrapper.transform);
            var instRt = (RectTransform)inst.transform;
            instRt.localScale    = Vector3.one;
            instRt.localRotation = Quaternion.identity;
            instRt.localPosition = Vector3.zero;
            instRt.anchorMin  = Vector2.zero;
            instRt.anchorMax  = Vector2.one;
            instRt.offsetMin  = Vector2.zero;
            instRt.offsetMax  = Vector2.zero;

            //# Fill Image 탐색 — 결정론적 경로 Background/Fill.
            var fillTf  = inst.transform.Find("Background/Fill");
            var fillImg = fillTf != null ? fillTf.GetComponent<Image>() : null;
            if (fillImg == null)
                Debug.LogWarning("[LairSpawnerVisualBuilder] HpBar.prefab 내 Background/Fill Image 미발견");

            //# SpawnerCooldownBar 부착 + _fill 주입.
            var bar = wrapper.AddComponent<SpawnerCooldownBar>();
            SetPrivateField(bar, "_fill", fillImg);

            return true;
        }

        //# 6종 머티리얼 생성 또는 로드 — EMonster 순서 인덱스 배열로 반환.
        private static Material[] EnsureSpawnerMaterials()
        {
            if (!Directory.Exists(MaterialDir))
            {
                Directory.CreateDirectory(MaterialDir);
                AssetDatabase.Refresh();
            }

            var mats = new Material[SpawnerColorTable.Length];
            for (int i = 0; i < SpawnerColorTable.Length; i++)
            {
                var (type, hex) = SpawnerColorTable[i];
                string matPath = $"{MaterialDir}/Mat_Spawner_{type}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find(UrpLitShaderName));
                    if (ColorUtility.TryParseHtmlString(hex, out var color))
                    {
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        mat.color = color;
                    }
                    AssetDatabase.CreateAsset(mat, matPath);
                    Debug.Log($"[LairSpawnerVisualBuilder] 머티리얼 생성: {matPath}");
                }
                mats[i] = mat;
            }
            return mats;
        }

        //# Spawner 의 직렬화 _outputType 필드를 읽어 EMonster 인덱스 반환.
        private static int GetOutputTypeIndex(Spawner spawner)
        {
            var so = new SerializedObject(spawner);
            var prop = so.FindProperty("_outputType");
            if (prop == null) return 0;
            return prop.enumValueIndex;
        }

        //# SerializedObject 로 private 필드 주입 (LairCharacterPrefabBuilder 와 동일 패턴).
        private static void SetPrivateField(Component target, string fieldName, object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairSpawnerVisualBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            switch (value)
            {
                case int i:        prop.intValue            = i; break;
                case float f:      prop.floatValue          = f; break;
                case bool b:       prop.boolValue           = b; break;
                case string s:     prop.stringValue         = s; break;
                case Material[] a:
                    //# Material 배열 — 크기 설정 후 각 원소 주입.
                    prop.arraySize = a.Length;
                    for (int idx = 0; idx < a.Length; idx++)
                        prop.GetArrayElementAtIndex(idx).objectReferenceValue = a[idx];
                    break;
                default:           prop.objectReferenceValue = value as Object; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
