using Lair.Character;
using Lair.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.EditorTools
{
    //# M3 시각 검증용 — 영웅 1 + 몬스터 3 을 씬에 임시 배치 + 카메라/조명/바닥 셋업.
    //# 사용자가 Play 버튼만 누르면 자동전투를 4 대 1 로 관찰 가능.
    //# M5 본격 씬 구성 진입 시 [Clear] 로 정리.
    public static class LairManualVerify
    {
        private const string VerifyRootName = "@M3Verify";

        [MenuItem("Lair/Verify/M3 - Setup Manual Verify (Hero vs 3 Monsters)")]
        public static void SetupVerifyScene()
        {
            //# 0) 현재 활성 씬 확인 — Battle.unity 가 아니면 경고
            var active = SceneManager.GetActiveScene();
            if (active.path != LairSetup.BattleScenePath)
            {
                Debug.LogWarning($"[ManualVerify] 활성 씬이 Battle.unity 가 아님: {active.path}. 그래도 진행합니다.");
            }

            //# 1) 기존 @M3Verify 있으면 제거
            ClearVerifyRoot();

            //# 2) 검증 루트
            var root = new GameObject(VerifyRootName);

            //# 3) 카메라 — 45° 비스듬, 기획서 §11.4
            SetupCamera();

            //# 4) 조명
            SetupLight();

            //# 5) 바닥 (Plane 30×30, 어두운 회색)
            SetupFloor(root);

            //# 6) 캐릭터 배치 (대칭 1:3)
            SpawnPrefab(root, EHero.Knight.ToString(),    new Vector3(0,  0, -5));
            SpawnPrefab(root, EMonster.Slime.ToString(),  new Vector3(-3, 0,  5));
            SpawnPrefab(root, EMonster.Golem.ToString(),  new Vector3(0,  0,  6));
            SpawnPrefab(root, EMonster.Orc.ToString(),    new Vector3(3,  0,  5));

            //# 7) 씬 저장
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);

            Debug.Log("[ManualVerify] 셋업 완료 — Play 버튼을 눌러 자동전투를 관찰하세요.");
        }

        [MenuItem("Lair/Verify/M3 - Clear Manual Verify")]
        public static void ClearVerifyScene()
        {
            ClearVerifyRoot();
            var active = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log("[ManualVerify] @M3Verify 제거 완료");
        }

        private static void ClearVerifyRoot()
        {
            var existing = GameObject.Find(VerifyRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(0, 12, -8);
            cam.transform.rotation = Quaternion.Euler(50, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);   //# #1F2937
            cam.fieldOfView = 60f;
        }

        private static void SetupLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.rotation = Quaternion.Euler(50, -30, 0);
                    l.intensity = 1f;
                    return;
                }
            }
            //# 없으면 생성
            var go = new GameObject("Directional Light");
            var nl = go.AddComponent<Light>();
            nl.type = LightType.Directional;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static void SetupFloor(GameObject parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform, worldPositionStays: false);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(3, 1, 3);  //# 10 * 3 = 30 유닛

            var col = floor.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 회색 머티리얼
            var matPath = "Assets/_Lair/Prefabs/Mat_Floor.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var c = new Color(0.22f, 0.25f, 0.32f, 1f);   //# #374151
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            floor.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SpawnPrefab(GameObject parent, string assetName, Vector3 pos)
        {
            var path = $"Assets/_Lair/Prefabs/Characters/{assetName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[ManualVerify] 프리팹 미발견: {path}");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.position = pos;

            //# 검증 한정 — 동적으로 CombatLogger 부착해 [Combat] 로그 출력
            if (instance.GetComponent<CombatLogger>() == null)
            {
                instance.AddComponent<CombatLogger>();
            }
        }
    }
}
