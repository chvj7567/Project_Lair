using Lair.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.EditorTools
{
    //# Battle 씬에 BattleZone GameObject 와 자식(hero entry 1) 일괄 셋업.
    //# 기획서 §2.3 (zone size 24, 경계 ±12) · §5.1 (hero entry -15) 좌표 박제.
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

            //# Hero entry point (기획서 §5.1) — 영웅 moveSpeed 3 × 5초 = 15m 행진.
            Transform heroEntry = CreateChild(root.transform, "HeroEntryPoint", new Vector3(-15f, 0f, 0f));

            //# 인스펙터 와이어링 — SerializeField private 라 SerializedObject 사용.
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("_zoneTrigger").objectReferenceValue = trigger;
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
            Debug.Log("[LairBattleZoneBuilder] BattleZone 셋업 완료 — hero entry 1, BattleController._zone 와이어링" + (bc == null ? " 실패 (BattleController 없음)" : " 완료"));
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
