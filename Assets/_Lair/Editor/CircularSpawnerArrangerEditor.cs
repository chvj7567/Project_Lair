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

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CircularSpawnerArranger arranger = (CircularSpawnerArranger)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild"))
                RebuildArranger(arranger);
        }

        //# 씬의 Arranger 를 찾아 Rebuild — 인스펙터 버튼 없이 메뉴/MCP 에서 실행 가능하게 한다.
        [MenuItem("Lair/Setup/Rebuild Circular Spawners")]
        public static void RebuildFromMenu()
        {
            CircularSpawnerArranger arranger = Object.FindFirstObjectByType<CircularSpawnerArranger>();
            if (arranger == null)
            {
                Debug.LogWarning("[CircularSpawnerArrangerEditor] 씬에 CircularSpawnerArranger 없음 — Rebuild 생략");
                return;
            }

            CircularSpawnerArranger[] all = Object.FindObjectsByType<CircularSpawnerArranger>(FindObjectsSortMode.InstanceID);
            if (all.Length > 1)
                Debug.LogWarning($"[CircularSpawnerArrangerEditor] CircularSpawnerArranger {all.Length}개 발견 — 첫 번째 사용");

            RebuildArranger(arranger);
        }

        //# 관리 스포너 전면 교체 → 몬스터별 Spawner 생성·배치·색상 → BattleController 재와이어링 → 씬 저장.
        //# 인스펙터 버튼·메뉴가 공유하는 단일 진실 (중복 로직 0건).
        public static void RebuildArranger(CircularSpawnerArranger arranger)
        {
            //# 1) 이전 관리 스포너 자식 전부 제거 (전면 교체, idempotent).
            RemoveManagedSpawners(arranger.transform);

            //# 6종 머티리얼 — 공유 팔레트에서 준비 (색상표 단일 진실).
            Material[] mats = SpawnerColorPalette.EnsureSpawnerMaterials();

            IReadOnlyList<EMonster> monsters = arranger.Monsters;
            int count = monsters.Count;
            Vector3[] positions = CircularSpawnerArranger.ComputePositions(
                arranger.transform.position, arranger.Radius, count, arranger.StartAngleDeg);

            //# 2) _monsters[i] 마다 Spawner 생성·배치·색상.
            List<Spawner> created = new List<Spawner>(count);
            for (int i = 0; i < count; ++i)
            {
                EMonster type = monsters[i];
                Spawner spawner = CreateSpawner(arranger.transform, type, i, positions[i], mats);
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

        //# Spawner GameObject 생성 + _outputType 설정 + 위치 배치 + SpawnerBody 색 디스크 부착.
        private static Spawner CreateSpawner(Transform parent, EMonster type, int index, Vector3 worldPos, Material[] mats)
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

            //# SpawnerBody 색 디스크 부착 — 기존 빌더 패턴 재사용 (Cylinder·머티리얼 주입).
            LairSpawnerVisualBuilder.EnsureSpawnerBody(spawner, mats);

            return spawner;
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
