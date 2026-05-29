using Lair.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.EditorTools
{
    //# Battle 씬에 BattleZone GameObject 와 자식(spawn point 12 + hero entry 1) 일괄 셋업.
    //# 기획서 §2.3 (zone size 24) · §3.2 / §4.2 (spawn point ±14.4) · §5.1 (hero entry -15) 좌표 박제.
    //# 인스펙터 와이어링 (_zoneTrigger / _spawnPoints / _heroEntryPoint) 도 함께 처리.
    //# 마지막으로 BattleController._zone 도 자동 와이어링.
    public static class LairBattleZoneBuilder
    {
        [MenuItem("Lair/Setup/Build BattleZone (Scene)")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path.EndsWith("Battle.unity") == false)
            {
                EditorUtility.DisplayDialog("BattleZone Builder",
                    "Battle.unity 씬을 먼저 연 뒤 실행하세요.\n현재: " + scene.path, "OK");
                return;
            }

            //# 기존 BattleZone 제거 (재실행 안전).
            GameObject existing = GameObject.Find("BattleZone");
            if (existing != null) Object.DestroyImmediate(existing);

            //# 본체 GameObject + BoxCollider(trigger) + BattleZone 컴포넌트.
            GameObject root = new GameObject("BattleZone");
            root.transform.position = Vector3.zero;
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(24f, 1f, 24f);
            BattleZone zone = root.AddComponent<BattleZone>();

            //# Spawn points 12개 — 4 edge × 3 (기획서 §4.2).
            Transform[] spawns = new Transform[12];
            spawns[0]  = CreateChild(root.transform, "SpawnPoint_N1", new Vector3(-6f,    0f,  14.4f));
            spawns[1]  = CreateChild(root.transform, "SpawnPoint_N2", new Vector3( 0f,    0f,  14.4f));
            spawns[2]  = CreateChild(root.transform, "SpawnPoint_N3", new Vector3( 6f,    0f,  14.4f));
            spawns[3]  = CreateChild(root.transform, "SpawnPoint_S1", new Vector3(-6f,    0f, -14.4f));
            spawns[4]  = CreateChild(root.transform, "SpawnPoint_S2", new Vector3( 0f,    0f, -14.4f));
            spawns[5]  = CreateChild(root.transform, "SpawnPoint_S3", new Vector3( 6f,    0f, -14.4f));
            spawns[6]  = CreateChild(root.transform, "SpawnPoint_E1", new Vector3( 14.4f, 0f, -6f));
            spawns[7]  = CreateChild(root.transform, "SpawnPoint_E2", new Vector3( 14.4f, 0f,  0f));
            spawns[8]  = CreateChild(root.transform, "SpawnPoint_E3", new Vector3( 14.4f, 0f,  6f));
            spawns[9]  = CreateChild(root.transform, "SpawnPoint_W1", new Vector3(-14.4f, 0f, -6f));
            spawns[10] = CreateChild(root.transform, "SpawnPoint_W2", new Vector3(-14.4f, 0f,  0f));
            spawns[11] = CreateChild(root.transform, "SpawnPoint_W3", new Vector3(-14.4f, 0f,  6f));

            //# Hero entry point (기획서 §5.1) — 영웅 moveSpeed 3 × 5초 = 15m 행진.
            Transform heroEntry = CreateChild(root.transform, "HeroEntryPoint", new Vector3(-15f, 0f, 0f));

            //# 인스펙터 와이어링 — SerializeField private 라 SerializedObject 사용.
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("_zoneTrigger").objectReferenceValue = trigger;
            SerializedProperty spawnsArr = so.FindProperty("_spawnPoints");
            spawnsArr.arraySize = 12;
            for (int i = 0; i < 12; ++i)
                spawnsArr.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.FindProperty("_heroEntryPoint").objectReferenceValue = heroEntry;
            so.ApplyModifiedPropertiesWithoutUndo();

            //# BattleController._zone 자동 와이어링 — 씬에 있는 첫 BattleController 찾아 주입.
            BattleController bc = Object.FindFirstObjectByType<BattleController>();
            if (bc != null)
            {
                SerializedObject bcSo = new SerializedObject(bc);
                bcSo.FindProperty("_zone").objectReferenceValue = zone;
                bcSo.ApplyModifiedPropertiesWithoutUndo();
            }

            //# 씬 dirty + 저장.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LairBattleZoneBuilder] BattleZone 셋업 완료 — spawn 12 + hero entry 1, BattleController._zone 와이어링" + (bc == null ? " 실패 (BattleController 없음)" : " 완료"));
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            return go.transform;
        }
    }
}
