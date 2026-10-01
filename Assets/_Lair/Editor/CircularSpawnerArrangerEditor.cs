using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lair.EditorTools
{
    //# CircularSpawnerArranger 커스텀 인스펙터 — "Rebuild" 버튼이 스포너 생성/배치/색상/재와이어링 수행.
    //# GameObject 생성·CreatePrimitive 는 Editor asmdef 안에만 둔다 (Rule 03 §4).
    [CustomEditor(typeof(CircularSpawnerArranger))]
    public class CircularSpawnerArrangerEditor : UnityEditor.Editor
    {
        //# 관리 스포너 자식 식별 prefix — Rebuild 시 이 prefix 자식만 전면 제거.
        private const string SpawnerNamePrefix = "Spawner_";
        //# 스포너 제단 프리팹(scene-2d-conversion §3.2) — 기존 SpawnerBody 실린더 대체
        public const string AltarPrefabPath = "Assets/_Lair/Art/Characters/SpawnerAltar2D.prefab";
        private const string AltarChildName = "SpawnerAltar2D";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CircularSpawnerArranger arranger = (CircularSpawnerArranger)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild"))
                RebuildArranger(arranger);
        }

        //# 관리 스포너 전면 교체 → 몬스터별 Spawner 생성·배치·색상 → BattleController 재와이어링 → 씬 저장.
        //# 1회성 Setup 메뉴는 제거 — 인스펙터 Rebuild 버튼만 진입점 (실수 실행 방지).
        public static void RebuildArranger(CircularSpawnerArranger arranger)
        {
            //# 1) 이전 관리 스포너 자식 전부 제거 (전면 교체, idempotent).
            RemoveManagedSpawners(arranger.transform);

            IReadOnlyList<EMonster> monsters = arranger.Monsters;
            int count = monsters.Count;
            Vector3[] positions = CircularSpawnerArranger.ComputeEllipsePositions(
                arranger.transform.position, arranger.Radius, arranger.RadiusZ, count, arranger.StartAngleDeg);

            //# 2) _monsters[i] 마다 Spawner 생성·배치·색상.
            List<Spawner> created = new List<Spawner>(count);
            for (int i = 0; i < count; ++i)
            {
                EMonster type = monsters[i];
                Spawner spawner = CreateSpawner(arranger.transform, type, i, positions[i]);
                created.Add(spawner);
            }

            //# 3) BattleController._spawners 를 새 배열로 재와이어링 — 누락 시 스포너 Tick 안 됨.
            RewireBattleController(created);

            //# 4) 씬 dirty + 저장.
            EditorSceneManager.MarkSceneDirty(arranger.gameObject.scene);
            EditorUtility.SetDirty(arranger.gameObject);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(arranger.gameObject.scene);

            Debug.Log($"[CircularSpawnerArrangerEditor] Rebuild 완료 — 스포너 {created.Count}개 배치");
        }

        //# 관리 prefix 자식 스포너 제거 — 역순 순회로 인덱스 안전.
        private static void RemoveManagedSpawners(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; --i)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith(SpawnerNamePrefix) == false)
                    continue;

                Object.DestroyImmediate(child.gameObject);
            }
        }

        //# Spawner GameObject 생성 + _outputType 설정 + 위치 배치 + 제단 프리팹 부착.
        private static Spawner CreateSpawner(Transform parent, EMonster type, int index, Vector3 worldPos)
        {
            GameObject go = new GameObject($"{SpawnerNamePrefix}{type}_{index}");
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;

            Spawner spawner = go.AddComponent<Spawner>();

            //# _outputType 설정 (SerializedObject) — enum 프로퍼티는 enumValueIndex 로 써야
            //# EnsureSpawnerBody 의 GetOutputTypeIndex(enumValueIndex 읽기)와 정합한다.
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty prop = so.FindProperty("_outputType");
            if (prop != null)
            {
                prop.enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureAltar(spawner, index);

            return spawner;
        }

        //# 스포너 자식 제단 — 기존 SpawnerBody 를 제거하고 SpawnerAltar2D 프리팹 인스턴스를 로컬 원점에 둔다. 위상 인덱스 = 스포너 순번.
        public static void EnsureAltar(Spawner spawner, int index)
        {
            Transform body = spawner.transform.Find("SpawnerBody");
            if (body != null)
                Object.DestroyImmediate(body.gameObject);

            Transform altar = spawner.transform.Find(AltarChildName);
            if (altar == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AltarPrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning("[CircularSpawnerArrangerEditor] 제단 프리팹 없음: " + AltarPrefabPath);
                    return;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, spawner.transform);
                go.name = AltarChildName;
                altar = go.transform;
            }
            altar.localPosition = Vector3.zero;
            altar.localRotation = Quaternion.identity;

            SerializedObject so = new SerializedObject(altar.GetComponent<SpawnerAltar2D>());
            so.FindProperty("_phaseIndex").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        //# 씬의 BattleController._spawners 를 새 배열로 교체 (SerializedObject).
        private static void RewireBattleController(List<Spawner> spawners)
        {
            BattleController controller = Object.FindFirstObjectByType<BattleController>();
            if (controller == null)
            {
                Debug.LogWarning("[CircularSpawnerArrangerEditor] 씬에 BattleController 없음 — _spawners 재와이어링 생략");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty prop = so.FindProperty("_spawners");
            if (prop == null)
            {
                Debug.LogWarning("[CircularSpawnerArrangerEditor] BattleController._spawners 필드 미발견");
                return;
            }

            prop.arraySize = spawners.Count;
            for (int i = 0; i < spawners.Count; ++i)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = spawners[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }
}
