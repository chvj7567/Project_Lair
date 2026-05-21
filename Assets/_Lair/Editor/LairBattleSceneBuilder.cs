using Lair.Battle;
using Lair.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.EditorTools
{
    //# M5 — Battle.unity 영구 셋업.
    //# 카메라/조명/바닥 + @Battle/BattleController + 스폰 포인트 4개 + 인스펙터 바인딩까지 자동화.
    //# Idempotent: 재실행 시 자식만 갱신, 중복 생성 X.
    public static class LairBattleSceneBuilder
    {
        private const string VerifyRootName = "@M3Verify";

        private const string CameraGroupName = "@Camera";
        private const string LightGroupName  = "@Light";
        private const string StageGroupName  = "@Stage";
        private const string BattleGroupName = "@Battle";

        private const string FloorName              = "Floor";
        private const string BattleControllerName   = "BattleController";
        private const string HeroSpawnName          = "HeroSpawn";
        private const string MonsterSpawn1Name      = "MonsterSpawn_01";
        private const string MonsterSpawn2Name      = "MonsterSpawn_02";
        private const string MonsterSpawn3Name      = "MonsterSpawn_03";

        private const string FloorMaterialPath = "Assets/_Lair/Art/Materials/Mat_Floor.mat";

        [MenuItem("Lair/Setup/M5 - Build Battle Scene")]
        public static void BuildBattleScene()
        {
            //# 0) 활성 씬 확인
            var active = SceneManager.GetActiveScene();
            if (active.path != LairSetup.BattleScenePath)
            {
                Debug.LogWarning($"[BattleSceneBuilder] 활성 씬이 Battle.unity 가 아님: {active.path}. 그래도 진행합니다.");
            }

            //# 1) @M3Verify 검증 잔재 제거
            ClearVerifyRoot();

            //# 2) 카메라/조명/바닥 그룹
            var cameraGroup = EnsureGroup(CameraGroupName);
            var lightGroup  = EnsureGroup(LightGroupName);
            var stageGroup  = EnsureGroup(StageGroupName);

            //# 3) 카메라
            SetupCamera(cameraGroup);
            Debug.Log("[BattleSceneBuilder] 카메라 셋업 완료");

            //# 4) 조명
            SetupLight(lightGroup);
            Debug.Log("[BattleSceneBuilder] 조명 셋업 완료");

            //# 5) 바닥
            SetupFloor(stageGroup);
            Debug.Log("[BattleSceneBuilder] 바닥 셋업 완료");

            //# 6) @Battle / BattleController / 스폰 포인트
            var battleGroup = EnsureGroup(BattleGroupName);
            var controllerGo = EnsureChild(battleGroup.transform, BattleControllerName);
            var controller = controllerGo.GetComponent<BattleController>();
            if (controller == null)
            {
                controller = controllerGo.AddComponent<BattleController>();
                Debug.Log("[BattleSceneBuilder] BattleController 컴포넌트 추가");
            }

            //# 스폰 포인트 (BattleController 의 자식)
            var heroSpawn = EnsureChild(controllerGo.transform, HeroSpawnName);
            heroSpawn.transform.localPosition = Vector3.zero;
            heroSpawn.transform.position = new Vector3(0, 0, -8);

            var ms1 = EnsureChild(controllerGo.transform, MonsterSpawn1Name);
            ms1.transform.position = new Vector3(-3, 0, 5);

            var ms2 = EnsureChild(controllerGo.transform, MonsterSpawn2Name);
            ms2.transform.position = new Vector3(0, 0, 6);

            var ms3 = EnsureChild(controllerGo.transform, MonsterSpawn3Name);
            ms3.transform.position = new Vector3(3, 0, 5);

            Debug.Log("[BattleSceneBuilder] 스폰 포인트 4개 셋업 완료");

            //# 7) BattleController 인스펙터 바인딩
            BindBattleController(controller, heroSpawn.transform, ms1.transform, ms2.transform, ms3.transform);
            Debug.Log("[BattleSceneBuilder] BattleController 인스펙터 바인딩 완료");

            //# 8) 씬 저장
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log("[BattleSceneBuilder] 씬 저장 완료");
        }

        private static void ClearVerifyRoot()
        {
            var existing = GameObject.Find(VerifyRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
                Debug.Log($"[BattleSceneBuilder] {VerifyRootName} 제거");
            }
        }

        //# 씬 루트의 그룹 GameObject 를 찾아 반환. 없으면 생성.
        private static GameObject EnsureGroup(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }
            return go;
        }

        //# 부모의 직계 자식을 이름으로 찾아 반환. 없으면 생성.
        private static GameObject EnsureChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static void SetupCamera(GameObject group)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            //# 카메라를 @Camera 그룹의 자식으로 이동
            if (cam.transform.parent != group.transform)
            {
                cam.transform.SetParent(group.transform, worldPositionStays: true);
            }
            cam.transform.position = new Vector3(0, 12, -8);
            cam.transform.rotation = Quaternion.Euler(50, 0, 0);
            cam.orthographic = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);   //# #1F2937
            cam.fieldOfView = 60f;
        }

        private static void SetupLight(GameObject group)
        {
            Light directional = null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directional = l;
                    break;
                }
            }
            if (directional == null)
            {
                var go = new GameObject("Directional Light");
                directional = go.AddComponent<Light>();
                directional.type = LightType.Directional;
            }
            //# @Light 그룹의 자식으로 이동
            if (directional.transform.parent != group.transform)
            {
                directional.transform.SetParent(group.transform, worldPositionStays: true);
            }
            directional.transform.rotation = Quaternion.Euler(50, -30, 0);
            directional.intensity = 1f;
        }

        private static void SetupFloor(GameObject stageGroup)
        {
            //# 기존 Floor 자식 찾기
            var existing = stageGroup.transform.Find(FloorName);
            GameObject floor;
            if (existing != null)
            {
                floor = existing.gameObject;
            }
            else
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = FloorName;
                floor.transform.SetParent(stageGroup.transform, worldPositionStays: false);
            }
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(3, 1, 3);

            //# 콜라이더 제거 (검증 패턴 동일)
            var col = floor.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 머티리얼
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var c = new Color(0.22f, 0.25f, 0.32f, 1f);   //# #374151
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, FloorMaterialPath);
            }
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }

        private static void BindBattleController(
            BattleController controller,
            Transform heroSpawn,
            Transform ms1, Transform ms2, Transform ms3)
        {
            var so = new SerializedObject(controller);
            so.Update();

            so.FindProperty("_heroSpawn").objectReferenceValue = heroSpawn;

            var arr = so.FindProperty("_monsterSpawns");
            arr.arraySize = 3;

            var e0 = arr.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("Point").objectReferenceValue = ms1;
            e0.FindPropertyRelative("Key").intValue = (int)EMonster.Slime;

            var e1 = arr.GetArrayElementAtIndex(1);
            e1.FindPropertyRelative("Point").objectReferenceValue = ms2;
            e1.FindPropertyRelative("Key").intValue = (int)EMonster.Golem;

            var e2 = arr.GetArrayElementAtIndex(2);
            e2.FindPropertyRelative("Point").objectReferenceValue = ms3;
            e2.FindPropertyRelative("Key").intValue = (int)EMonster.Orc;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }
}
