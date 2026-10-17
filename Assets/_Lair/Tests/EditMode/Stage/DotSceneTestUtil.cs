using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Tests.EditMode
{
    //# 도트 무대 씬 회귀 테스트 공용 헬퍼 — 씬을 Additive 로 열고 닫는다(열려 있는 씬 상태를 건드리지 않음).
    public static class DotSceneTestUtil
    {
        public const string BattleScene = "Assets/_Lair/Scenes/Battle.unity";
        public const string VillageScene = "Assets/_Lair/Scenes/Village.unity";
        public const string LoadingScene = "Assets/_Lair/Scenes/Loading.unity";

        public static Scene Open(string path)
        {
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        public static void Close(Scene scene)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        public static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T c = root.GetComponentInChildren<T>(true);
                if (c != null)
                    return c;
            }
            return null;
        }

        public static List<T> FindAll<T>(Scene scene) where T : Component
        {
            List<T> list = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return list;
        }

        public static List<GameObject> AllObjects(Scene scene)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (Transform t in FindAll<Transform>(scene))
            {
                list.Add(t.gameObject);
            }
            return list;
        }

        //# MainCamera 태그 카메라 — 씬당 1개.
        public static Camera MainCamera(Scene scene)
        {
            foreach (Camera cam in FindAll<Camera>(scene))
            {
                if (cam.CompareTag("MainCamera"))
                    return cam;
            }
            return null;
        }
    }
}
