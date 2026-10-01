using Lair.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Tests.Battle
{
    //# 게임플레이 값 회귀 — Battle.unity 직렬화 값. 기획서 scene-2d-conversion §10 · G5.
    public class BattleSceneGameplayValuesTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void OpenScene()
        {
            _scene = EditorSceneManager.OpenScene("Assets/_Lair/Scenes/Battle.unity", OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        private T Find<T>() where T : Component
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                T c = root.GetComponentInChildren<T>(true);
                if (c != null)
                    return c;
            }
            return null;
        }

        [Test]
        public void 스포너_링_반축은_8_32와_6_3149다()
        {
            SerializedObject so = new SerializedObject(Find<CircularSpawnerArranger>());
            Assert.AreEqual(8.32f, so.FindProperty("_radius").floatValue, 1e-4f);
            Assert.AreEqual(6.3149f, so.FindProperty("_radiusZ").floatValue, 1e-4f);
        }

        [Test]
        public void BattleZone_클램프는_10과_5다()
        {
            SerializedObject so = new SerializedObject(Find<BattleZone>());
            Assert.AreEqual(10f, so.FindProperty("_clampHalfExtentX").floatValue, 1e-4f);
            Assert.AreEqual(5f, so.FindProperty("_clampHalfExtentZ").floatValue, 1e-4f);
        }

        [Test]
        public void 영웅_진입점은_로컬_마이너스7이다()
        {
            BattleZone zone = Find<BattleZone>();
            Transform entry = zone.transform.Find("HeroEntryPoint");
            Assert.IsNotNull(entry);
            Assert.AreEqual(-7f, entry.localPosition.x, 1e-4f);
            Assert.AreEqual(0f, entry.localPosition.z, 1e-4f);
        }

        [Test]
        public void 스포너_6개가_타원_위에_있다()
        {
            Spawner[] spawners = Object.FindObjectsByType<Spawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            foreach (Spawner s in spawners)
            {
                if (s.gameObject.scene != _scene)
                    continue;
                n++;
                Vector3 rel = s.transform.position - new Vector3(0f, 0f, -2f);
                float v = (rel.x * rel.x) / (8.32f * 8.32f) + (rel.z * rel.z) / (6.3149f * 6.3149f);
                Assert.AreEqual(1f, v, 0.002f, s.name);
            }
            Assert.AreEqual(6, n);
        }
    }
}
