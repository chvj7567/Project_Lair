using System.IO;
using System.Reflection;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 침입 지도 비주얼 폴리시 본격 스위트(worldmap-popup-visual-polish §8.1) — 팔레트·곡선·스프라이트 임포트·프리팹 연결·고아 에셋.
    public class WorldMapVisualPolishSuiteTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string PopupPath = "Assets/_Lair/Art/UI/WorldMapPopup.prefab";
        private const string NodePath = "Assets/_Lair/Art/UI/WorldMapNode.prefab";
        private const string TabPath = "Assets/_Lair/Art/UI/WorldMapChapterTab.prefab";
        private const float Tol = 0.002f;

        private static readonly Color Neutral = new Color32(0x2c, 0x37, 0x50, 0xff);
        private static readonly Color NeutralMap = new Color32(0x0d, 0x11, 0x19, 0xff);

        private static readonly string[] AllSprites =
        {
            "WorldMapFrame", "WorldMapCornerGold", "WorldMapGradientV", "WorldMapGradientH", "WorldMapInset",
            "WorldMapBadge", "WorldMapChipGem", "WorldMapPointer", "WorldMapMedalOpen", "WorldMapMedalBoss",
            "WorldMapMedalCleared", "WorldMapMedalLocked", "WorldMapNodeSelRing", "WorldMapStarBadge",
            "WorldMapNamePlate", "WorldMapPathDot", "WorldMapPathGlow", "WorldMapSilhouetteCh1",
            "WorldMapSilhouetteCh2", "WorldMapSilhouetteCh3", "WorldMapMoon", "WorldMapFog", "WorldMapVignette",
        };

        private static void AssertColor(Color expected, Color actual, float tol, string label)
        {
            Assert.AreEqual(expected.r, actual.r, tol, label + ".r");
            Assert.AreEqual(expected.g, actual.g, tol, label + ".g");
            Assert.AreEqual(expected.b, actual.b, tol, label + ".b");
            Assert.AreEqual(expected.a, actual.a, tol, label + ".a");
        }

        //# --- ① 팔레트 ---

        [TestCase(1, 0.503f, 0.605f, 0.753f)]
        [TestCase(2, 0.429f, 0.679f, 0.561f)]
        [TestCase(3, 0.635f, 0.533f, 0.849f)]
        public void NodeOpenTint_챕터1에서3은_기획서_표_값이다(int chapter, float r, float g, float b)
        {
            AssertColor(new Color(r, g, b, 1f), WorldMapChapterPalette.NodeOpenTint(chapter), Tol, $"챕터 {chapter}");
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(99)]
        [TestCase(-1)]
        public void NodeOpenTint_범위_밖_챕터는_중립색과_흰색의_30퍼센트_보간이다(int chapter)
        {
            Color expected = Color.Lerp(Neutral, Color.white, 0.30f);
            expected.a = 1f;

            AssertColor(expected, WorldMapChapterPalette.NodeOpenTint(chapter), 0.0001f, $"챕터 {chapter}");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(99)]
        public void NodeGlow_rgb는_Accent와_같고_알파는_045다(int chapter)
        {
            Color accent = WorldMapChapterPalette.Accent(chapter);
            Color glow = WorldMapChapterPalette.NodeGlow(chapter);

            Assert.AreEqual(accent.r, glow.r, 0.0001f);
            Assert.AreEqual(accent.g, glow.g, 0.0001f);
            Assert.AreEqual(accent.b, glow.b, 0.0001f);
            Assert.AreEqual(0.45f, glow.a, 0.0001f);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Silhouette_챕터1에서3은_지도배경_곱_030이고_알파_080이다(int chapter)
        {
            Color bg = WorldMapChapterPalette.MapBackground(chapter);
            Color expected = new Color(bg.r * 0.3f, bg.g * 0.3f, bg.b * 0.3f, 0.80f);

            AssertColor(expected, WorldMapChapterPalette.Silhouette(chapter), 0.0001f, $"챕터 {chapter}");
        }

        [Test]
        public void Silhouette_챕터1에서3_기획서_검산값과_일치한다()
        {
            AssertColor(new Color(0.0235f, 0.0318f, 0.0518f, 0.8f), WorldMapChapterPalette.Silhouette(1), 0.001f, "1");
            AssertColor(new Color(0.0165f, 0.0400f, 0.0259f, 0.8f), WorldMapChapterPalette.Silhouette(2), 0.001f, "2");
            AssertColor(new Color(0.0318f, 0.0200f, 0.0529f, 0.8f), WorldMapChapterPalette.Silhouette(3), 0.001f, "3");
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(99)]
        public void Silhouette_범위_밖_챕터는_중립_지도배경_곱_030이다(int chapter)
        {
            Color expected = new Color(NeutralMap.r * 0.3f, NeutralMap.g * 0.3f, NeutralMap.b * 0.3f, 0.80f);

            AssertColor(expected, WorldMapChapterPalette.Silhouette(chapter), 0.0001f, $"챕터 {chapter}");
        }

        [Test]
        public void 팔레트_상수가_기획서_값이다()
        {
            Assert.AreEqual(0.30f, WorldMapChapterPalette.NodeOpenLightness);
            Assert.AreEqual(0.45f, WorldMapChapterPalette.NodeGlowAlpha);
            Assert.AreEqual(0.30f, WorldMapChapterPalette.SilhouetteDarken);
            Assert.AreEqual(0.80f, WorldMapChapterPalette.SilhouetteAlpha);
        }

        [Test]
        public void 신규_팔레트_조회는_기존_Accent와_MapBackground를_변경하지_않는다()
        {
            Color accentBefore = WorldMapChapterPalette.Accent(1);
            Color mapBefore = WorldMapChapterPalette.MapBackground(1);

            WorldMapChapterPalette.NodeOpenTint(1);
            WorldMapChapterPalette.NodeGlow(1);
            WorldMapChapterPalette.Silhouette(1);

            Assert.AreEqual(accentBefore, WorldMapChapterPalette.Accent(1));
            Assert.AreEqual(mapBefore, WorldMapChapterPalette.MapBackground(1));
        }

        //# --- ② UILoopMotion 곡선 ---

        [Test]
        public void DriftFactor_시작_편도_왕복_끝_값과_반복()
        {
            Assert.AreEqual(0f, UILoopMotion.DriftFactor(0f, 14f), 0.0001f);
            Assert.AreEqual(0.5f, UILoopMotion.DriftFactor(7f, 14f), 0.0001f);
            Assert.AreEqual(1f, UILoopMotion.DriftFactor(14f, 14f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.DriftFactor(28f, 14f), 0.0001f);
            Assert.AreEqual(UILoopMotion.DriftFactor(3f, 14f), UILoopMotion.DriftFactor(31f, 14f), 0.0001f);
        }

        [Test]
        public void DriftFactor_편도시간이_0이하면_0이다()
        {
            Assert.AreEqual(0f, UILoopMotion.DriftFactor(5f, 0f));
            Assert.AreEqual(0f, UILoopMotion.DriftFactor(5f, -1f));
        }

        [Test]
        public void RotateAngle_8초에_마이너스_90도이고_0초는_0이다()
        {
            Assert.AreEqual(0f, UILoopMotion.RotateAngle(0f, -11.25f), 0.0001f);
            Assert.AreEqual(-90f, UILoopMotion.RotateAngle(8f, -11.25f), 0.0001f);
        }

        [Test]
        public void NudgeFactor_절반_경계와_주기_복귀()
        {
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(0f, 1f));
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(0.49f, 1f));
            Assert.AreEqual(1f, UILoopMotion.NudgeFactor(0.51f, 1f));
            Assert.AreEqual(1f, UILoopMotion.NudgeFactor(0.99f, 1f));
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(1.0f, 1f));
            Assert.AreEqual(1f, UILoopMotion.NudgeFactor(1.51f, 1f));
        }

        [Test]
        public void NudgeFactor_주기가_0이하면_0이다()
        {
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(0.7f, 0f));
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(0.7f, -1f));
        }

        [Test]
        public void PulseFactor_시작_정점_복귀와_주기_대칭()
        {
            Assert.AreEqual(0f, UILoopMotion.PulseFactor(0f, 2f), 0.0001f);
            Assert.AreEqual(0.5f, UILoopMotion.PulseFactor(0.5f, 2f), 0.0001f);
            Assert.AreEqual(1f, UILoopMotion.PulseFactor(1f, 2f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.PulseFactor(2f, 2f), 0.0001f);
            Assert.AreEqual(UILoopMotion.PulseFactor(0.3f, 2f), UILoopMotion.PulseFactor(1.7f, 2f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.PulseFactor(1f, 0f));
        }

        [Test]
        public void 노드_프리팹_글로우_Pulse_파라미터가_시작_알파067_스케일100_정점_알파100_스케일115다()
        {
            GameObject nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePath);
            UILoopMotion motion = (UILoopMotion)Field(nodePrefab.GetComponent<WorldMapNodeView>(), "_glowMotion");
            Assert.IsNotNull(motion, "_glowMotion");

            float period = (float)Field(motion, "_period");
            float alphaFrom = (float)Field(motion, "_alphaFrom");
            float alphaTo = (float)Field(motion, "_alphaTo");
            float scaleFrom = (float)Field(motion, "_scaleFrom");
            float scaleTo = (float)Field(motion, "_scaleTo");

            Assert.AreEqual(2f, period, 0.0001f);
            Assert.AreEqual(0.67f, Mathf.Lerp(alphaFrom, alphaTo, UILoopMotion.PulseFactor(0f, period)), 0.001f);
            Assert.AreEqual(1.00f, Mathf.Lerp(scaleFrom, scaleTo, UILoopMotion.PulseFactor(0f, period)), 0.001f);
            Assert.AreEqual(1.00f, Mathf.Lerp(alphaFrom, alphaTo, UILoopMotion.PulseFactor(1f, period)), 0.001f);
            Assert.AreEqual(1.15f, Mathf.Lerp(scaleFrom, scaleTo, UILoopMotion.PulseFactor(1f, period)), 0.001f);
            Assert.AreEqual(0.67f, Mathf.Lerp(alphaFrom, alphaTo, UILoopMotion.PulseFactor(2f, period)), 0.001f);
        }

        //# --- ③ 스프라이트 임포트 ---

        private static TextureImporter Importer(string name)
        {
            return AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
        }

        [Test]
        public void 신규_스프라이트_23종이_존재하고_Sprite_타입_공통_설정을_따른다()
        {
            Assert.AreEqual(23, AllSprites.Length);
            foreach (string name in AllSprites)
            {
                Assert.IsTrue(File.Exists(SpriteDir + name + ".png"), $"{name}.png 없음");
                TextureImporter importer = Importer(name);
                Assert.IsNotNull(importer, $"{name} 임포터");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{name} textureType");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{name} spriteMode");
                Assert.AreEqual(100f, importer.spritePixelsPerUnit, $"{name} PPU");
                Assert.IsFalse(importer.mipmapEnabled, $"{name} mipmap");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{name} compression");
                Assert.IsTrue(importer.alphaIsTransparency, $"{name} alphaIsTransparency");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png"), $"{name} Sprite 로드");
            }
        }

        [TestCase("WorldMapFrame", 9, 9, 9, 9)]
        [TestCase("WorldMapInset", 5, 5, 5, 5)]
        [TestCase("WorldMapBadge", 0, 3, 3, 2)]
        [TestCase("WorldMapNamePlate", 2, 2, 2, 2)]
        [TestCase("WorldMapPathGlow", 12, 12, 12, 12)]
        public void 슬라이스9_스프라이트의_border가_기획서_표와_같다(string name, int left, int bottom, int right, int top)
        {
            Vector4 border = Importer(name).spriteBorder;

            Assert.AreEqual(new Vector4(left, bottom, right, top), border, name);
        }

        [Test]
        public void 슬라이스9_대상이_아닌_스프라이트는_border가_0이다()
        {
            string[] sliced = { "WorldMapFrame", "WorldMapInset", "WorldMapBadge", "WorldMapNamePlate", "WorldMapPathGlow" };
            foreach (string name in AllSprites)
            {
                if (System.Array.IndexOf(sliced, name) >= 0)
                    continue;
                Assert.AreEqual(Vector4.zero, Importer(name).spriteBorder, name);
            }
        }

        [Test]
        public void PathDot은_Repeat_Point이고_ChipGem은_Point이며_나머지는_Clamp_Bilinear다()
        {
            Assert.AreEqual(TextureWrapMode.Repeat, Importer("WorldMapPathDot").wrapMode);
            Assert.AreEqual(FilterMode.Point, Importer("WorldMapPathDot").filterMode);
            Assert.AreEqual(FilterMode.Point, Importer("WorldMapChipGem").filterMode);

            foreach (string name in AllSprites)
            {
                if (name != "WorldMapPathDot")
                {
                    Assert.AreEqual(TextureWrapMode.Clamp, Importer(name).wrapMode, $"{name} wrap");
                }
                if (name != "WorldMapPathDot" && name != "WorldMapChipGem")
                {
                    Assert.AreEqual(FilterMode.Bilinear, Importer(name).filterMode, $"{name} filter");
                }
            }
        }

        [TestCase("WorldMapFrame", 32, 32)]
        [TestCase("WorldMapCornerGold", 28, 28)]
        [TestCase("WorldMapGradientV", 4, 64)]
        [TestCase("WorldMapGradientH", 64, 4)]
        [TestCase("WorldMapInset", 16, 16)]
        [TestCase("WorldMapBadge", 16, 16)]
        [TestCase("WorldMapChipGem", 30, 18)]
        [TestCase("WorldMapPointer", 24, 24)]
        [TestCase("WorldMapMedalOpen", 128, 128)]
        [TestCase("WorldMapMedalBoss", 128, 128)]
        [TestCase("WorldMapMedalCleared", 128, 128)]
        [TestCase("WorldMapMedalLocked", 128, 128)]
        [TestCase("WorldMapNodeSelRing", 256, 256)]
        [TestCase("WorldMapStarBadge", 48, 48)]
        [TestCase("WorldMapNamePlate", 8, 8)]
        [TestCase("WorldMapPathDot", 12, 4)]
        [TestCase("WorldMapPathGlow", 32, 32)]
        [TestCase("WorldMapSilhouetteCh1", 520, 260)]
        [TestCase("WorldMapSilhouetteCh2", 520, 260)]
        [TestCase("WorldMapSilhouetteCh3", 520, 260)]
        [TestCase("WorldMapMoon", 64, 64)]
        [TestCase("WorldMapFog", 4, 64)]
        [TestCase("WorldMapVignette", 128, 128)]
        public void 스프라이트_픽셀_크기가_기획서_표와_같다(string name, int width, int height)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");

            Assert.IsNotNull(sprite, name);
            Assert.AreEqual(width, (int)sprite.rect.width, $"{name} 폭");
            Assert.AreEqual(height, (int)sprite.rect.height, $"{name} 높이");
        }

        //# --- ④ 프리팹 연결 ---

        private static object Field(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재 — 시그니처 변경 감지");
            return field.GetValue(target);
        }

        private static bool IsAssigned(object value)
        {
            UnityEngine.Object obj = value as UnityEngine.Object;
            return obj != null;
        }

        [Test]
        public void 팝업_프리팹_신규_SerializeField가_모두_연결돼_있다()
        {
            WorldMapPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath).GetComponent<WorldMapPopup>();

            string[] names = { "_mapSilhouette", "_portraitGlow", "_selectGlow", "_selectGloss" };
            foreach (string name in names)
            {
                Assert.IsTrue(IsAssigned(Field(popup, name)), $"WorldMapPopup.{name} 비어 있음");
            }
        }

        [Test]
        public void 팝업_silhouetteSprites는_길이3이고_원소가_모두_채워지며_챕터순_스프라이트다()
        {
            WorldMapPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath).GetComponent<WorldMapPopup>();
            Sprite[] sprites = (Sprite[])Field(popup, "_silhouetteSprites");

            Assert.AreEqual(3, sprites.Length);
            for (int i = 0; i < sprites.Length; ++i)
            {
                Assert.IsTrue(IsAssigned(sprites[i]), $"_silhouetteSprites[{i}] 비어 있음");
                Assert.AreEqual($"WorldMapSilhouetteCh{i + 1}", sprites[i].name, $"인덱스 {i} = 챕터 {i + 1}");
            }
        }

        private static void AssertNodeMedals(WorldMapNodeView node, string label)
        {
            Assert.AreEqual("WorldMapMedalOpen", ((Sprite)Field(node, "_openSprite")).name, label);
            Assert.AreEqual("WorldMapMedalBoss", ((Sprite)Field(node, "_bossSprite")).name, label);
            Assert.AreEqual("WorldMapMedalCleared", ((Sprite)Field(node, "_clearedSprite")).name, label);
            Assert.AreEqual("WorldMapMedalLocked", ((Sprite)Field(node, "_lockedSprite")).name, label);
        }

        [Test]
        public void 노드_프리팹_메달_슬롯_4종이_각각_해당_스프라이트를_가리킨다()
        {
            AssertNodeMedals(AssetDatabase.LoadAssetAtPath<GameObject>(NodePath).GetComponent<WorldMapNodeView>(), "WorldMapNode.prefab");
        }

        [Test]
        public void 팝업_프리팹에_배치된_노드_5개도_메달_슬롯과_신규_위젯이_연결돼_있다()
        {
            WorldMapPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath).GetComponent<WorldMapPopup>();
            WorldMapNodeView[] nodes = (WorldMapNodeView[])Field(popup, "_nodes");

            Assert.AreEqual(5, nodes.Length);
            for (int i = 0; i < nodes.Length; ++i)
            {
                AssertNodeMedals(nodes[i], $"_nodes[{i}]");
                Assert.IsTrue(IsAssigned(Field(nodes[i], "_glow")), $"_nodes[{i}]._glow");
                Assert.IsTrue(IsAssigned(Field(nodes[i], "_glowMotion")), $"_nodes[{i}]._glowMotion");
                Assert.IsTrue(IsAssigned(Field(nodes[i], "_starBadge")), $"_nodes[{i}]._starBadge");
            }
        }

        [Test]
        public void 노드_프리팹_신규_위젯_glow_glowMotion_starBadge가_연결돼_있다()
        {
            WorldMapNodeView node = AssetDatabase.LoadAssetAtPath<GameObject>(NodePath).GetComponent<WorldMapNodeView>();

            Assert.IsTrue(IsAssigned(Field(node, "_glow")));
            Assert.IsTrue(IsAssigned(Field(node, "_glowMotion")));
            Assert.IsTrue(IsAssigned(Field(node, "_starBadge")));
        }

        [Test]
        public void 노드_글로우와_장식_Image는_raycastTarget이_꺼져_있다()
        {
            WorldMapNodeView node = AssetDatabase.LoadAssetAtPath<GameObject>(NodePath).GetComponent<WorldMapNodeView>();

            Assert.IsFalse(((Image)Field(node, "_glow")).raycastTarget, "_glow");
            GameObject star = (GameObject)Field(node, "_starBadge");
            foreach (Image image in star.GetComponentsInChildren<Image>(true))
            {
                Assert.IsFalse(image.raycastTarget, $"_starBadge/{image.name}");
            }
        }

        [Test]
        public void 탭_프리팹과_배치된_탭_5개의_selectedDecor_selectedPointer_chips가_연결돼_있다()
        {
            WorldMapChapterTabView prefabTab = AssetDatabase.LoadAssetAtPath<GameObject>(TabPath).GetComponent<WorldMapChapterTabView>();
            WorldMapPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath).GetComponent<WorldMapPopup>();
            WorldMapChapterTabView[] placed = (WorldMapChapterTabView[])Field(popup, "_tabs");

            Assert.AreEqual(5, placed.Length);
            AssertTab(prefabTab, "WorldMapChapterTab.prefab");
            for (int i = 0; i < placed.Length; ++i)
            {
                AssertTab(placed[i], $"_tabs[{i}]");
            }
        }

        private static void AssertTab(WorldMapChapterTabView tab, string label)
        {
            Assert.IsTrue(IsAssigned(Field(tab, "_selectedDecor")), $"{label}._selectedDecor");
            Assert.IsTrue(IsAssigned(Field(tab, "_selectedPointer")), $"{label}._selectedPointer");

            Image[] chips = (Image[])Field(tab, "_chips");
            Assert.AreEqual(5, chips.Length, $"{label}._chips 길이");
            for (int i = 0; i < chips.Length; ++i)
            {
                Assert.IsTrue(IsAssigned(chips[i]), $"{label}._chips[{i}]");
                Assert.IsNotNull(chips[i].sprite, $"{label}._chips[{i}] 스프라이트");
                Assert.AreEqual("WorldMapChipGem", chips[i].sprite.name, $"{label}._chips[{i}]");
            }
        }

        [Test]
        public void 탭_선택_장식_하위_Image는_raycastTarget이_꺼져_있다()
        {
            WorldMapChapterTabView tab = AssetDatabase.LoadAssetAtPath<GameObject>(TabPath).GetComponent<WorldMapChapterTabView>();

            foreach (string name in new[] { "_selectedDecor", "_selectedPointer" })
            {
                GameObject root = (GameObject)Field(tab, name);
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    Assert.IsFalse(image.raycastTarget, $"{name}/{image.name}");
                }
            }
        }

        [Test]
        public void 팝업_연결선_점선은_PathDot_스프라이트를_쓴다()
        {
            WorldMapPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath).GetComponent<WorldMapPopup>();
            Image[] dashed = (Image[])Field(popup, "_pathDashed");

            Assert.AreEqual(4, dashed.Length);
            for (int i = 0; i < dashed.Length; ++i)
            {
                Assert.IsNotNull(dashed[i].sprite, $"_pathDashed[{i}]");
                Assert.AreEqual("WorldMapPathDot", dashed[i].sprite.name, $"_pathDashed[{i}]");
            }
        }

        //# --- ⑤ 삭제된 WorldMapPathDash ---

        [Test]
        public void WorldMapPathDash_png와_meta가_존재하지_않는다()
        {
            Assert.IsFalse(File.Exists(SpriteDir + "WorldMapPathDash.png"));
            Assert.IsFalse(File.Exists(SpriteDir + "WorldMapPathDash.png.meta"));
            Assert.IsEmpty(AssetDatabase.FindAssets("WorldMapPathDash"));
        }

        [Test]
        public void WorldMapPathDash_GUID를_참조하는_프리팹_씬_에셋이_없다()
        {
            foreach (string file in Directory.GetFiles("Assets/_Lair", "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".prefab") == false && file.EndsWith(".unity") == false && file.EndsWith(".asset") == false)
                    continue;
                StringAssert.DoesNotContain("WorldMapPathDash", File.ReadAllText(file), file);
            }
        }

        [Test]
        public void 코드와_테스트_소스에_WorldMapPathDash_참조가_없다()
        {
            foreach (string file in Directory.GetFiles("Assets/_Lair/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("WorldMapPathDash", File.ReadAllText(file), file);
            }
        }
    }
}
