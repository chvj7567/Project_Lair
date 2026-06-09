using Lair.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 씬에 배치된 Spawner(6개)에 SpawnerBody 자식(Cylinder 디스크)을 부착.
    //# 스포너 상태 UI — World-space 진행 바(CooldownBarWrapper) 빌드 스텝 제거 (기획서 §4.11).
    public static class LairSpawnerVisualBuilder
    {
        [MenuItem("Lair/Setup/S1 - Attach Spawner Visuals")]
        public static void AttachSpawnerVisuals()
        {
            //# 씬의 모든 Spawner (disabled 포함) 탐색.
            Spawner[] spawners = Object.FindObjectsOfType<Spawner>(includeInactive: true);
            if (spawners.Length == 0)
            {
                Debug.LogWarning("[LairSpawnerVisualBuilder] 씬에 Spawner 를 찾을 수 없음");
                return;
            }

            //# 6종 머티리얼을 미리 생성/로드해 두어 반복 참조 (공유 팔레트).
            Material[] mats = SpawnerColorPalette.EnsureSpawnerMaterials();

            int processed = 0;
            foreach (Spawner spawner in spawners)
            {
                bool changed = false;
                changed |= EnsureSpawnerBody(spawner, mats);
                //# 스포너 상태 UI — 기존 CooldownBarWrapper 자식이 있다면 제거 (씬 마이그레이션, 기획서 §4.11).
                changed |= RemoveCooldownBarWrapperIfExists(spawner);

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

        //# 씬에 잔존하는 CooldownBarWrapper 자식 제거 (idempotent). 신규 진행 바는 BattleHud 6셀.
        private static bool RemoveCooldownBarWrapperIfExists(Spawner spawner)
        {
            Transform wrapper = spawner.transform.Find("CooldownBarWrapper");
            if (wrapper == null) return false;
            Object.DestroyImmediate(wrapper.gameObject);
            return true;
        }

        //# SpawnerBody 자식 생성 — 이미 있으면 false 반환(스킵). 다른 빌더(CircularSpawnerArrangerEditor)도 재사용.
        internal static bool EnsureSpawnerBody(Spawner spawner, Material[] mats)
        {
            if (spawner.transform.Find("SpawnerBody") != null) return false;

            //# Cylinder 납작 디스크 생성 (Rule 12 에디터 예외).
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "SpawnerBody";
            body.transform.SetParent(spawner.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(2.0f, 0.05f, 2.0f);

            //# Collider 제거 — 전투 충돌 영향 없도록 (기획서 §2.1).
            Collider col = body.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# _currentType 초기 읽기 — Spawner._outputType 의 기본값(Wisp) 이 직렬화에 반영됨.
            //# SerializedObject 로 직렬화 필드를 읽어 초기 머티리얼 인덱스 결정.
            int initIndex = GetOutputTypeIndex(spawner);
            Renderer renderer = body.GetComponent<Renderer>();
            if (initIndex >= 0 && initIndex < mats.Length && mats[initIndex] != null)
                renderer.sharedMaterial = mats[initIndex];

            //# SpawnerBody 컴포넌트 부착 — _renderer + _materials 주입.
            SpawnerBody bodyComp = body.AddComponent<SpawnerBody>();
            SetPrivateField(bodyComp, "_renderer", renderer);
            SetPrivateField(bodyComp, "_materials", mats);

            return true;
        }

        //# Spawner 의 직렬화 _outputType 필드를 읽어 EMonster 인덱스 반환.
        private static int GetOutputTypeIndex(Spawner spawner)
        {
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty prop = so.FindProperty("_outputType");
            if (prop == null) return 0;
            return prop.enumValueIndex;
        }

        //# SerializedObject 로 private 필드 주입 (LairCharacterPrefabBuilder 와 동일 패턴).
        private static void SetPrivateField(Component target, string fieldName, object value)
        {
            SerializedObject so   = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
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
