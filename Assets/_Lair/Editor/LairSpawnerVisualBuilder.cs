using Lair.Battle;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Spawner 에 SpawnerBody 자식(Cylinder 디스크)을 부착하는 공유 헬퍼.
    //# 1회성 Setup 메뉴(S1)는 제거 — 호출자는 CircularSpawnerArrangerEditor 인스펙터 Rebuild 뿐.
    public static class LairSpawnerVisualBuilder
    {
        //# SpawnerBody 자식 생성 — 이미 있으면 false 반환(스킵). CircularSpawnerArrangerEditor 가 사용.
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
