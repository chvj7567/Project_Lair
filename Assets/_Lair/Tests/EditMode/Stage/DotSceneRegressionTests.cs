using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Stage;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Lair.Tests.EditMode
{
    //# 세 씬의 2D 도트 전환 회귀 — 카메라·정렬 레이어·3D 잔재·삭제 에셋 참조·와이드 화면 채움. 기획서 scene-2d-conversion §1.2 · §1.3 · §9.2 · §10.
    public static class DotSceneRules
    {
        //# 삭제된 3D 바닥·벽·스포너 원통 에셋 GUID(커밋 83e46b9) — 씬·프리팹에 남아 있으면 안 된다.
        public static readonly string[] DeletedGuids =
        {
            "d573703d85da0c14a9cb18dc0a75d990", "33cc4f5139f01364bb35f852ed812921", "4881ff982fb61c34599f71953bffc53c",
            "e8298b4eda9a138459dc97b04ab3801c", "04da5009a8308544a982b0f8530c7eac", "ae7972884b8873249a991db4aab1f744",
            "ecc50113e066405429aabbeea5c4a259", "c6967b87384ed2844a5703cc18fad30b", "c6d7754f580c4da4b925eff01ce41f14",
            "01731357956957d4f8b6f8a7e1601e36", "06cedec69a368d24ea47bc623c808ea4", "b3b8720be4e6c844a952077494cbf6a5",
            "1d33f59f9d386cf479cbc4dc548d62db",
        };

        public static readonly string[] DeletedPaths =
        {
            "Assets/_Lair/Art/Materials/Mat_Floor.mat", "Assets/_Lair/Art/Materials/Mat_Spawner_Hex.mat",
            "Assets/_Lair/Art/Materials/Mat_Spawner_Phantom.mat", "Assets/_Lair/Art/Materials/Mat_Spawner_Plague.mat",
            "Assets/_Lair/Art/Materials/Mat_Spawner_Reaper.mat", "Assets/_Lair/Art/Materials/Mat_Spawner_Wisp.mat",
            "Assets/_Lair/Art/Materials/Mat_Spawner_Wraith.mat", "Assets/_Lair/Art/Materials/Mat_VillageGround.mat",
            "Assets/_Lair/Art/Materials/Mat_VillageWall.mat", "Assets/_Lair/Art/Sprites/Map_Arena_1280x720.png",
            "Assets/_Lair/Art/Sprites/Village_Floor.png", "Assets/_Lair/Art/Sprites/Village_Wall.png",
            "Assets/_Lair/Scripts/Battle/SpawnerBody.cs",
        };

        public static int RendererIndexOf(Camera cam)
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            SerializedObject so = new SerializedObject(data);
            return so.FindProperty("m_RendererIndex").intValue;
        }
    }

    //# 씬·프리팹 파일 전체에 대한 정적 검사.
    public class DotAssetReferenceRegressionTests
    {
        private static IEnumerable<string> SceneAndPrefabFiles()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets/_Lair" }))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        [Test]
        public void 씬과_프리팹에_삭제된_3D_에셋_GUID_참조가_없다()
        {
            List<string> hits = new List<string>();
            foreach (string path in SceneAndPrefabFiles())
            {
                string text = File.ReadAllText(path);
                foreach (string guid in DotSceneRules.DeletedGuids)
                {
                    if (text.Contains(guid))
                    {
                        hits.Add(path + " -> " + guid);
                    }
                }
            }
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }

        [Test]
        public void 삭제된_3D_에셋_파일이_디스크에_다시_존재하지_않는다()
        {
            //# AssetDatabase 캐시는 외부 삭제 직후 stale 일 수 있어 디스크 기준으로 검사
            foreach (string path in DotSceneRules.DeletedPaths)
            {
                Assert.IsFalse(File.Exists(path), path);
            }
        }

        [Test]
        public void 삭제된_스포너_원통_클래스가_남아있지_않다()
        {
            Assert.IsNull(typeof(Spawner).Assembly.GetType("Lair.Battle.SpawnerBody"));
        }

        [Test]
        public void 정렬_레이어는_Backdrop_Default_Emissive_WorldUI_UI_순서다()
        {
            int backdrop = SortingLayer.GetLayerValueFromName("Backdrop");
            int def = SortingLayer.GetLayerValueFromName("Default");
            int emissive = SortingLayer.GetLayerValueFromName("Emissive");
            int worldUi = SortingLayer.GetLayerValueFromName("WorldUI");
            int ui = SortingLayer.GetLayerValueFromName("UI");
            Assert.Less(backdrop, def);
            Assert.Less(def, emissive);
            Assert.Less(emissive, worldUi);
            Assert.Less(worldUi, ui);
        }

        [Test]
        public void 정렬_레이어_5개가_모두_존재한다()
        {
            foreach (string n in new[] { "Backdrop", "Default", "Emissive", "WorldUI", "UI" })
            {
                Assert.IsTrue(SortingLayer.IsValid(SortingLayer.NameToID(n)), n);
            }
        }

        [TestCase("Wisp")]
        [TestCase("Wraith")]
        [TestCase("Reaper")]
        [TestCase("Hex")]
        [TestCase("Plague")]
        [TestCase("Phantom")]
        [TestCase("Knight")]
        public void 캐릭터_프리팹_루트에_SortingGroup이_있다(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/" + name + ".prefab");
            Assert.IsNotNull(prefab, name);
            UnityEngine.Rendering.SortingGroup group = prefab.GetComponent<UnityEngine.Rendering.SortingGroup>();
            Assert.IsNotNull(group, name);
            Assert.AreEqual("Default", group.sortingLayerName, name);
        }

        [Test]
        public void 제단_프리팹은_SpawnerAltar2D_루트_파사드를_가진다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/SpawnerAltar2D.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<SpawnerAltar2D>());
        }

        [Test]
        public void 제단_링_결정_반짝은_Emissive_레이어_본체는_Default다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/SpawnerAltar2D.prefab");
            foreach (SpriteRenderer sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name == "Ring" || sr.name == "Crystal" || sr.name == "CrystalCore" || sr.name == "Sparkle")
                {
                    Assert.AreEqual("Emissive", sr.sortingLayerName, sr.name);
                }
                if (sr.name == "Base" || sr.name == "RingDim")
                {
                    Assert.AreEqual("Default", sr.sortingLayerName, sr.name);
                }
            }
        }
    }

    public class BattleSceneDotRegressionTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = DotSceneTestUtil.Open(DotSceneTestUtil.BattleScene);
        }

        [OneTimeTearDown]
        public void Close()
        {
            DotSceneTestUtil.Close(_scene);
        }

        [Test]
        public void 메인_카메라는_직교_크기_7_피치_50도다()
        {
            Camera cam = DotSceneTestUtil.MainCamera(_scene);
            Assert.IsNotNull(cam);
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(7f, cam.orthographicSize, 1e-4f);
            Assert.AreEqual(50f, cam.transform.eulerAngles.x, 0.01f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, cam.transform.eulerAngles.y), 0.01f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, cam.transform.eulerAngles.z), 0.01f);
        }

        [Test]
        public void 메인_카메라는_2D_렌더러_인덱스_1을_쓴다()
        {
            Assert.AreEqual(1, DotSceneRules.RendererIndexOf(DotSceneTestUtil.MainCamera(_scene)));
        }

        [Test]
        public void 메인_카메라_배경색은_05060A다()
        {
            Color c = DotSceneTestUtil.MainCamera(_scene).backgroundColor;
            Assert.AreEqual(5f / 255f, c.r, 0.003f);
            Assert.AreEqual(6f / 255f, c.g, 0.003f);
            Assert.AreEqual(10f / 255f, c.b, 0.003f);
        }

        [Test]
        public void 휠_줌_거리는_15로_고정이다()
        {
            BattleCamera bc = DotSceneTestUtil.Find<BattleCamera>(_scene);
            SerializedObject so = new SerializedObject(bc);
            Assert.AreEqual(15f, so.FindProperty("_minZoomDistance").floatValue, 1e-4f);
            Assert.AreEqual(15f, so.FindProperty("_maxZoomDistance").floatValue, 1e-4f);
        }

        [Test]
        public void 레터박스_띠에도_월드를_이어_그린다()
        {
            LetterboxFramer lf = DotSceneTestUtil.Find<LetterboxFramer>(_scene);
            SerializedObject so = new SerializedObject(lf);
            Assert.IsTrue(so.FindProperty("_renderWorldInBars").boolValue);
            Assert.AreEqual(16f / 9f, so.FindProperty("_targetAspect").floatValue, 1e-4f);
        }

        [Test]
        public void 조명_격자_원점은_배틀_화면_중앙_614_352다()
        {
            DotLightingSettings s = DotSceneTestUtil.Find<DotLightingSettings>(_scene);
            Assert.IsNotNull(s);
            Vector2 o = new SerializedObject(s).FindProperty("_gridOrigin").vector2Value;
            Assert.AreEqual(614f, o.x, 1e-4f);
            Assert.AreEqual(352f, o.y, 1e-4f);
        }

        [Test]
        public void 입체_바닥과_조명_잔재가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreNotEqual("Floor", go.name);
                Assert.AreNotEqual("MapBackground", go.name);
                Assert.AreNotEqual("Directional Light", go.name);
            }
            foreach (Light l in DotSceneTestUtil.FindAll<Light>(_scene))
            {
                Assert.Fail("3D Light 잔재: " + l.name);
            }
        }

        [Test]
        public void 배경_스프라이트는_Backdrop_레이어이고_흔들림_여백까지_화면을_덮는다()
        {
            SpriteRenderer backdrop = null;
            foreach (SpriteRenderer sr in DotSceneTestUtil.FindAll<SpriteRenderer>(_scene))
            {
                if (sr.sprite != null && sr.sprite.name.StartsWith("Battle_Backdrop"))
                {
                    backdrop = sr;
                }
            }
            Assert.IsNotNull(backdrop, "Battle_Backdrop");
            Assert.AreEqual("Backdrop", backdrop.sortingLayerName);
            //# 16:9 화면 1194.67×672 도트 + 카메라 흔들림 최대 0.3u = 14.4도트 × 양쪽
            Assert.GreaterOrEqual(backdrop.sprite.rect.width, 1194.67f + 28.8f);
            Assert.GreaterOrEqual(backdrop.sprite.rect.height, 672f + 28.8f);
            Assert.AreEqual(48f, backdrop.sprite.pixelsPerUnit, 1e-4f);
        }

        [Test]
        public void 배경은_카메라_정면_평면에서_스케일_1이다()
        {
            foreach (SpriteRenderer sr in DotSceneTestUtil.FindAll<SpriteRenderer>(_scene))
            {
                if (sr.sprite == null || sr.sprite.name.StartsWith("Battle_Backdrop") == false)
                    continue;
                Assert.AreEqual(1f, sr.transform.lossyScale.x, 1e-4f);
                Assert.AreEqual(1f, sr.transform.lossyScale.y, 1e-4f);
                Assert.AreEqual(50f, sr.transform.eulerAngles.x, 0.01f);
            }
        }

        [Test]
        public void HUD_캔버스는_UI_정렬_레이어다()
        {
            bool found = false;
            foreach (Canvas c in DotSceneTestUtil.FindAll<Canvas>(_scene))
            {
                if (c.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;
                found = true;
                Assert.AreEqual("UI", c.sortingLayerName, c.name);
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void 글로벌_2D_조명은_정확히_1개다()
        {
            int global = 0;
            foreach (Light2D l in DotSceneTestUtil.FindAll<Light2D>(_scene))
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    global++;
                }
            }
            Assert.AreEqual(1, global);
        }

        [Test]
        public void 발광_머티리얼_스프라이트는_Emissive_레이어다()
        {
            foreach (SpriteRenderer sr in DotSceneTestUtil.FindAll<SpriteRenderer>(_scene))
            {
                if (sr.sharedMaterial != null && sr.sharedMaterial.name == "Mat_DotEmissive")
                {
                    Assert.AreEqual("Emissive", sr.sortingLayerName, sr.name);
                }
            }
        }

        [Test]
        public void 씬에_깨진_스크립트_참조가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), go.name);
            }
        }
    }

    public class VillageSceneDotRegressionTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = DotSceneTestUtil.Open(DotSceneTestUtil.VillageScene);
        }

        [OneTimeTearDown]
        public void Close()
        {
            DotSceneTestUtil.Close(_scene);
        }

        [Test]
        public void 메인_카메라는_직교_피치_16도_2D_렌더러다()
        {
            Camera cam = DotSceneTestUtil.MainCamera(_scene);
            Assert.IsNotNull(cam);
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(16f, cam.transform.eulerAngles.x, 0.01f);
            Assert.AreEqual(1, DotSceneRules.RendererIndexOf(cam));
        }

        [Test]
        public void 입체_면과_조명_잔재가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreNotEqual("Ground", go.name);
                Assert.AreNotEqual("Wall_Back", go.name);
                Assert.AreNotEqual("Wall_Left", go.name);
                Assert.AreNotEqual("Wall_Right", go.name);
                Assert.AreNotEqual("Directional Light", go.name);
            }
            Assert.AreEqual(0, DotSceneTestUtil.FindAll<Light>(_scene).Count);
        }

        [Test]
        public void 조명_격자_원점은_마을_화면_중앙_240_135다()
        {
            Vector2 o = new SerializedObject(DotSceneTestUtil.Find<DotLightingSettings>(_scene)).FindProperty("_gridOrigin").vector2Value;
            Assert.AreEqual(240f, o.x, 1e-4f);
            Assert.AreEqual(135f, o.y, 1e-4f);
        }

        [Test]
        public void 배경은_640x270_이상이고_Backdrop_레이어다()
        {
            SpriteRenderer backdrop = null;
            foreach (SpriteRenderer sr in DotSceneTestUtil.FindAll<SpriteRenderer>(_scene))
            {
                if (sr.sprite != null && sr.sprite.name.StartsWith("Village_Backdrop"))
                {
                    backdrop = sr;
                }
            }
            Assert.IsNotNull(backdrop);
            Assert.AreEqual("Backdrop", backdrop.sortingLayerName);
            Assert.AreEqual(640f, backdrop.sprite.rect.width);
            Assert.AreEqual(270f, backdrop.sprite.rect.height);
        }

        [Test]
        public void 커버핏_기준_도트는_배경_크기와_같다()
        {
            DotStageCoverFit fit = DotSceneTestUtil.Find<DotStageCoverFit>(_scene);
            Assert.IsNotNull(fit);
            Vector2 reference = new SerializedObject(fit).FindProperty("_referenceDots").vector2Value;
            Assert.AreEqual(640f, reference.x);
            Assert.AreEqual(270f, reference.y);
        }

        [TestCase(16f / 9f)]
        [TestCase(2.15f)]
        [TestCase(21f / 9f)]
        [TestCase(2.4f)]
        [TestCase(4f / 3f)]
        public void 마을_배경은_해당_종횡비에서_화면을_빈틈없이_채운다(float aspect)
        {
            Vector2 reference = new SerializedObject(DotSceneTestUtil.Find<DotStageCoverFit>(_scene)).FindProperty("_referenceDots").vector2Value;
            float size = DotStageCoverFit.OrthoSizeForCover(reference, aspect, 48f);
            float visibleW = 2f * size * aspect * 48f;
            float visibleH = 2f * size * 48f;
            Assert.LessOrEqual(visibleW, reference.x + 0.01f, "가로가 배경을 넘음");
            Assert.LessOrEqual(visibleH, reference.y + 0.01f, "세로가 배경을 넘음");
        }

        [Test]
        public void 마을_16대9에서는_기준_크기_2_8125를_그대로_쓴다()
        {
            Vector2 reference = new SerializedObject(DotSceneTestUtil.Find<DotStageCoverFit>(_scene)).FindProperty("_referenceDots").vector2Value;
            Assert.AreEqual(2.8125f, DotStageCoverFit.OrthoSizeForCover(reference, 16f / 9f, 48f), 1e-4f);
            Assert.AreEqual(2.8125f, DotStageCoverFit.OrthoSizeForCover(reference, 2.15f, 48f), 1e-4f);
        }

        [Test]
        public void 마을_배회자_프리팹_인스턴스는_6개다()
        {
            Assert.AreEqual(6, DotSceneTestUtil.FindAll<VillageWanderer>(_scene).Count);
        }

        [Test]
        public void 씬에_깨진_스크립트_참조가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), go.name);
            }
        }
    }

    public class LoadingSceneDotRegressionTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = DotSceneTestUtil.Open(DotSceneTestUtil.LoadingScene);
        }

        [OneTimeTearDown]
        public void Close()
        {
            DotSceneTestUtil.Close(_scene);
        }

        [Test]
        public void 메인_카메라는_직교_크기_2_8125_정면_2D_렌더러다()
        {
            Camera cam = DotSceneTestUtil.MainCamera(_scene);
            Assert.IsNotNull(cam);
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(2.8125f, cam.orthographicSize, 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, cam.transform.rotation), 0.01f);
            Assert.AreEqual(1, DotSceneRules.RendererIndexOf(cam));
        }

        [Test]
        public void 로딩_캔버스는_UI_레이어다()
        {
            bool found = false;
            foreach (Canvas c in DotSceneTestUtil.FindAll<Canvas>(_scene))
            {
                if (c.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;
                found = true;
                Assert.AreEqual("UI", c.sortingLayerName, c.name);
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void 레터박스_띠에도_월드를_이어_그린다()
        {
            LetterboxFramer lf = DotSceneTestUtil.Find<LetterboxFramer>(_scene);
            Assert.IsNotNull(lf);
            Assert.IsTrue(new SerializedObject(lf).FindProperty("_renderWorldInBars").boolValue);
        }

        [Test]
        public void 조명_격자_원점은_로딩_화면_중앙_240_135다()
        {
            Vector2 o = new SerializedObject(DotSceneTestUtil.Find<DotLightingSettings>(_scene)).FindProperty("_gridOrigin").vector2Value;
            Assert.AreEqual(240f, o.x, 1e-4f);
            Assert.AreEqual(135f, o.y, 1e-4f);
        }

        [Test]
        public void 배경은_640x270이고_화면_가로를_덮는다()
        {
            SpriteRenderer backdrop = null;
            foreach (SpriteRenderer sr in DotSceneTestUtil.FindAll<SpriteRenderer>(_scene))
            {
                if (sr.sprite != null && sr.sprite.name.StartsWith("Loading_Backdrop"))
                {
                    backdrop = sr;
                }
            }
            Assert.IsNotNull(backdrop);
            Assert.AreEqual("Backdrop", backdrop.sortingLayerName);
            Assert.AreEqual(640f, backdrop.sprite.rect.width);
            Assert.AreEqual(270f, backdrop.sprite.rect.height);
        }

        [Test]
        public void 입체_조명_잔재가_없다()
        {
            Assert.AreEqual(0, DotSceneTestUtil.FindAll<Light>(_scene).Count);
        }

        [Test]
        public void 씬에_깨진_스크립트_참조가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), go.name);
            }
        }
    }
}
