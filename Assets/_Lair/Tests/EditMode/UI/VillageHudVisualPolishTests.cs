using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lair.Battle;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 마을 HUD 비주얼 폴리시 스위트(village-hud-visual-polish §8.2) — 프리팹 연결·스프라이트 임포트·장식 레이캐스트·움직임·글로우 토글.
    public class VillageHudVisualPolishTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string HudPath = "Assets/_Lair/Art/UI/VillageHud.prefab";
        private const string GeneratorPath = "Assets/_Lair/Editor/VillageHudSpriteGenerator.cs";

        private static readonly string[] ExistingFields =
        {
            "_displayNameText", "_soulText", "_lordLevelText", "_lordXpFill",
            "_shopButton", "_codexButton", "_recordsButton", "_heroButton", "_questButton", "_lordButton", "_rankingButton",
            "_cloudButton", "_cloudConflictDot", "_sortieButton",
            "_stagePrevButton", "_stageNextButton", "_stageIndicatorText", "_stageThreatText",
            "_stageLockOverlay", "_stageLockDim", "_stageLockLabel", "_stageLockHintText",
            "_stageHeroPortrait", "_recordIntruderText", "_recordBestText", "_recordWinsText",
        };

        private static readonly string[] AllSprites =
        {
            "VillageHudAvatarRing", "VillageHudXpFill", "VillageHudBarFrame", "VillageHudChip", "VillageHudBtn",
            "VillageHudBtnOff", "VillageHudCta", "VillageHudGem", "VillageHudPaperInset",
        };

        private int _prevChapterCount;

        [SetUp]
        public void 챕터_수를_출격_가능_챕터_수로_맞춘다()
        {
            _prevChapterCount = StageProgress.ChapterCount;
            StageProgress.ConfigureChapterCount(ChapterGate.PlayableChapterCount);
        }

        [TearDown]
        public void 챕터_수를_원복한다()
        {
            StageProgress.ConfigureChapterCount(_prevChapterCount);
        }

        private static object Field(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재 — 시그니처 변경 감지");
            return field.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재");
            field.SetValue(target, value);
        }

        private static bool IsAssigned(object value)
        {
            UnityEngine.Object obj = value as UnityEngine.Object;
            return obj != null;
        }

        private static GameObject LoadHudRoot()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            Assert.IsNotNull(root, HudPath);
            return root;
        }

        private static VillageHud LoadHud()
        {
            VillageHud hud = LoadHudRoot().GetComponent<VillageHud>();
            Assert.IsNotNull(hud, "VillageHud 컴포넌트");
            return hud;
        }

        private static TextureImporter Importer(string name)
        {
            return AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
        }

        private static GameObject GameObjectOf(object widget)
        {
            GameObject go = widget as GameObject;
            if (go != null)
                return go;
            Component comp = widget as Component;
            return comp != null ? comp.gameObject : null;
        }

        //# --- ① 프리팹 필드 연결 ---

        [Test]
        public void 기존_SerializeField_26개가_모두_연결돼_있다()
        {
            Assert.AreEqual(26, ExistingFields.Length);
            VillageHud hud = LoadHud();
            foreach (string name in ExistingFields)
            {
                Assert.IsTrue(IsAssigned(Field(hud, name)), $"VillageHud.{name} 비어 있음");
            }
        }

        [Test]
        public void stageDots는_길이5이고_원소가_모두_채워져_있다()
        {
            Image[] dots = (Image[])Field(LoadHud(), "_stageDots");

            Assert.AreEqual(5, dots.Length);
            for (int i = 0; i < dots.Length; ++i)
            {
                Assert.IsTrue(IsAssigned(dots[i]), $"_stageDots[{i}] 비어 있음");
            }
        }

        [Test]
        public void 신규_sortieGlow_sortieGloss가_연결되고_이름이_기획서와_같다()
        {
            VillageHud hud = LoadHud();
            GameObject glow = (GameObject)Field(hud, "_sortieGlow");
            GameObject gloss = (GameObject)Field(hud, "_sortieGloss");

            Assert.IsTrue(IsAssigned(glow), "_sortieGlow");
            Assert.IsTrue(IsAssigned(gloss), "_sortieGloss");
            Assert.AreEqual("DecoSortieGlow", glow.name);
            Assert.AreEqual("DecoSortieGloss", gloss.name);
        }

        [Test]
        public void 방어하기_글로우는_버튼_뒤_형제이고_광택은_버튼의_자식이다()
        {
            VillageHud hud = LoadHud();
            Transform sortie = ((Component)Field(hud, "_sortieButton")).transform;
            Transform glow = ((GameObject)Field(hud, "_sortieGlow")).transform;
            Transform gloss = ((GameObject)Field(hud, "_sortieGloss")).transform;

            Assert.AreSame(sortie.parent, glow.parent, "글로우는 버튼과 같은 부모");
            Assert.Less(glow.GetSiblingIndex(), sortie.GetSiblingIndex(), "글로우는 버튼 뒤(앞 형제 순서)");
            Assert.AreSame(sortie, gloss.parent, "광택은 버튼의 자식");
        }

        //# --- ② 보석 점 / 비활성 스프라이트 ---

        [Test]
        public void stageDots_5개는_모두_VillageHudGem_스프라이트를_쓴다()
        {
            Image[] dots = (Image[])Field(LoadHud(), "_stageDots");
            for (int i = 0; i < dots.Length; ++i)
            {
                Assert.IsNotNull(dots[i].sprite, $"_stageDots[{i}] 스프라이트");
                Assert.AreEqual("VillageHudGem", dots[i].sprite.name, $"_stageDots[{i}]");
            }
        }

        [TestCase("_sortieButton")]
        [TestCase("_stagePrevButton")]
        [TestCase("_stageNextButton")]
        public void 비활성_스프라이트는_SpriteSwap과_VillageHudBtnOff다(string fieldName)
        {
            Component button = (Component)Field(LoadHud(), fieldName);
            Selectable selectable = button.GetComponent<Selectable>();

            Assert.IsNotNull(selectable, fieldName);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, selectable.transition, fieldName);
            Assert.IsNotNull(selectable.spriteState.disabledSprite, $"{fieldName} disabledSprite");
            Assert.AreEqual("VillageHudBtnOff", selectable.spriteState.disabledSprite.name, fieldName);
        }

        [Test]
        public void 방어하기_버튼은_VillageHudCta_배경을_쓴다()
        {
            Component button = (Component)Field(LoadHud(), "_sortieButton");
            Image image = button.GetComponent<Image>();

            Assert.IsNotNull(image.sprite);
            Assert.AreEqual("VillageHudCta", image.sprite.name);
        }

        //# --- ③ 장식 레이캐스트 ---

        [Test]
        public void Deco_접두어_오브젝트가_존재하고_모든_Graphic은_raycastTarget이_꺼져_있다()
        {
            int decoCount = 0;
            int graphicCount = 0;
            foreach (Transform t in LoadHudRoot().GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Deco") == false)
                    continue;
                ++decoCount;
                foreach (Graphic graphic in t.GetComponentsInChildren<Graphic>(true))
                {
                    ++graphicCount;
                    Assert.IsFalse(graphic.raycastTarget, $"{t.name}/{graphic.name} raycastTarget");
                }
            }

            Assert.Greater(decoCount, 0, "Deco* 오브젝트 없음");
            Assert.Greater(graphicCount, 0, "Deco* Graphic 없음");
        }

        //# --- ④ UILoopMotion ---

        [Test]
        public void UILoopMotion은_정확히_2개이고_RedDot과_DecoSortieGlow에_붙어_있다()
        {
            UILoopMotion[] motions = LoadHudRoot().GetComponentsInChildren<UILoopMotion>(true);

            Assert.AreEqual(2, motions.Length);
            List<string> names = new List<string>();
            foreach (UILoopMotion motion in motions)
            {
                names.Add(motion.gameObject.name);
            }
            CollectionAssert.AreEquivalent(new[] { "RedDot", "DecoSortieGlow" }, names);
        }

        private static UILoopMotion MotionOf(GameObject target)
        {
            UILoopMotion motion = target.GetComponent<UILoopMotion>();
            Assert.IsNotNull(motion, target.name);
            return motion;
        }

        [Test]
        public void RedDot_Pulse는_주기14_스케일100에서118_알파_변경없음이다()
        {
            VillageHud hud = LoadHud();
            GameObject dot = (GameObject)Field(hud, "_cloudConflictDot");
            Assert.AreEqual("RedDot", dot.name);
            UILoopMotion motion = MotionOf(dot);

            Assert.AreEqual("Pulse", Field(motion, "_kind").ToString());
            Assert.AreEqual(1.4f, (float)Field(motion, "_period"), 0.0001f);
            Assert.AreEqual(1.00f, (float)Field(motion, "_scaleFrom"), 0.0001f);
            Assert.AreEqual(1.18f, (float)Field(motion, "_scaleTo"), 0.0001f);
            Assert.IsFalse(IsAssigned(Field(motion, "_graphic")), "알파 변경 없음 — _graphic 비움");
            Assert.AreEqual(Field(motion, "_alphaFrom"), Field(motion, "_alphaTo"), "알파 불변");
        }

        [Test]
        public void DecoSortieGlow_Pulse는_주기22_알파047에서080_스케일불변이고_자기_Image를_쓴다()
        {
            VillageHud hud = LoadHud();
            GameObject glow = (GameObject)Field(hud, "_sortieGlow");
            UILoopMotion motion = MotionOf(glow);

            Assert.AreEqual("Pulse", Field(motion, "_kind").ToString());
            Assert.AreEqual(2.2f, (float)Field(motion, "_period"), 0.0001f);
            Assert.AreEqual(0.47f, (float)Field(motion, "_alphaFrom"), 0.0001f);
            Assert.AreEqual(0.80f, (float)Field(motion, "_alphaTo"), 0.0001f);
            Assert.AreEqual(1.00f, (float)Field(motion, "_scaleFrom"), 0.0001f);
            Assert.AreEqual(1.00f, (float)Field(motion, "_scaleTo"), 0.0001f);
            Assert.AreSame(glow.GetComponent<Image>(), Field(motion, "_graphic"), "_graphic = 자기 Image");
        }

        //# --- ⑤ 잠금 오버레이 ---

        [Test]
        public void StageLockDim_알파는_072다()
        {
            Image dim = (Image)Field(LoadHud(), "_stageLockDim");

            Assert.AreEqual(0.72f, dim.color.a, 0.005f);
        }

        //# --- ⑥ 스프라이트 임포트 ---

        [Test]
        public void 신규_스프라이트_9종이_존재하고_공통_임포트_설정을_따른다()
        {
            Assert.AreEqual(9, AllSprites.Length);
            foreach (string name in AllSprites)
            {
                Assert.IsTrue(File.Exists(SpriteDir + name + ".png"), $"{name}.png 없음");
                TextureImporter importer = Importer(name);
                Assert.IsNotNull(importer, $"{name} 임포터");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{name} textureType");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{name} spriteMode");
                Assert.AreEqual(100f, importer.spritePixelsPerUnit, $"{name} PPU");
                Assert.AreEqual(SpriteMeshType.FullRect, ReadMeshType(importer), $"{name} meshType");
                Assert.IsFalse(importer.mipmapEnabled, $"{name} mipmap");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{name} compression");
                Assert.IsTrue(importer.sRGBTexture, $"{name} sRGB");
                Assert.IsTrue(importer.alphaIsTransparency, $"{name} alphaIsTransparency");
                Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, $"{name} wrap");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png"), $"{name} Sprite 로드");
            }
        }

        private static SpriteMeshType ReadMeshType(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType;
        }

        [TestCase("VillageHudBarFrame", 3, 3, 3, 3)]
        [TestCase("VillageHudChip", 5, 5, 5, 5)]
        [TestCase("VillageHudBtn", 6, 7, 6, 6)]
        [TestCase("VillageHudBtnOff", 6, 7, 6, 6)]
        [TestCase("VillageHudCta", 6, 8, 6, 6)]
        [TestCase("VillageHudPaperInset", 12, 12, 12, 12)]
        public void 슬라이스9_스프라이트의_border가_기획서_표와_같다(string name, int left, int bottom, int right, int top)
        {
            Assert.AreEqual(new Vector4(left, bottom, right, top), Importer(name).spriteBorder, name);
        }

        [TestCase("VillageHudAvatarRing")]
        [TestCase("VillageHudXpFill")]
        [TestCase("VillageHudGem")]
        public void 슬라이스_대상이_아닌_스프라이트는_border가_0이다(string name)
        {
            Assert.AreEqual(Vector4.zero, Importer(name).spriteBorder, name);
        }

        [TestCase("VillageHudBtn", FilterMode.Point)]
        [TestCase("VillageHudBtnOff", FilterMode.Point)]
        [TestCase("VillageHudCta", FilterMode.Point)]
        [TestCase("VillageHudChip", FilterMode.Point)]
        [TestCase("VillageHudBarFrame", FilterMode.Point)]
        [TestCase("VillageHudPaperInset", FilterMode.Point)]
        [TestCase("VillageHudAvatarRing", FilterMode.Bilinear)]
        [TestCase("VillageHudXpFill", FilterMode.Bilinear)]
        [TestCase("VillageHudGem", FilterMode.Bilinear)]
        public void 필터_모드가_기획서_표와_같다(string name, FilterMode expected)
        {
            Assert.AreEqual(expected, Importer(name).filterMode, name);
        }

        [TestCase("VillageHudAvatarRing", 72, 72)]
        [TestCase("VillageHudXpFill", 4, 16)]
        [TestCase("VillageHudBarFrame", 16, 16)]
        [TestCase("VillageHudChip", 16, 16)]
        [TestCase("VillageHudBtn", 24, 24)]
        [TestCase("VillageHudBtnOff", 24, 24)]
        [TestCase("VillageHudCta", 24, 24)]
        [TestCase("VillageHudGem", 36, 36)]
        [TestCase("VillageHudPaperInset", 32, 32)]
        public void 스프라이트_픽셀_크기가_기획서_표와_같다(string name, int width, int height)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");

            Assert.IsNotNull(sprite, name);
            Assert.AreEqual(width, (int)sprite.rect.width, $"{name} 폭");
            Assert.AreEqual(height, (int)sprite.rect.height, $"{name} 높이");
        }

        //# --- ⑦ 일회용 생성기 삭제 (Rule 04 §3) ---

        [Test]
        public void 일회용_스프라이트_생성기_스크립트가_레포에_없다()
        {
            Assert.IsFalse(File.Exists(GeneratorPath), GeneratorPath);
            Assert.IsFalse(File.Exists(GeneratorPath + ".meta"), GeneratorPath + ".meta");
            foreach (string file in Directory.GetFiles("Assets/_Lair/Editor", "*.cs", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("VillageHudSpriteGenerator", Path.GetFileName(file), file);
            }
        }

        //# --- ⑧ RefreshStage 글로우 토글 ---

        private static VillageHud MakeBareHud(out GameObject root)
        {
            root = new GameObject("VillageHudPolishTest");
            return root.AddComponent<VillageHud>();
        }

        private static VillageViewModel MakeVm(int selectedStage, int clearedStage)
        {
            VillageViewModel vm = new VillageViewModel(new MetaProfile { ClearedStage = clearedStage }, null);
            vm.MoveStage(selectedStage - vm.SelectedStage);
            return vm;
        }

        [Test]
        public void InitUI는_신규_필드가_비어_있어도_예외가_없다()
        {
            GameObject root;
            VillageHud hud = MakeBareHud(out root);
            try
            {
                VillageViewModel vm = MakeVm(1, 0);

                Assert.DoesNotThrow(() => hud.InitUI(new VillageHudArg { Vm = vm }));
                Assert.DoesNotThrow(() => vm.MoveStage(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshStage는_잠금_여부에_따라_글로우와_광택을_토글한다()
        {
            GameObject root;
            VillageHud hud = MakeBareHud(out root);
            GameObject glow = new GameObject("DecoSortieGlow");
            GameObject gloss = new GameObject("DecoSortieGloss");
            try
            {
                SetField(hud, "_sortieGlow", glow);
                SetField(hud, "_sortieGloss", gloss);
                glow.SetActive(false);
                gloss.SetActive(false);

                //# 클리어 0 → 스테이지 1 해금, 스테이지 5 는 잠금
                VillageViewModel vm = MakeVm(1, 0);
                Assert.IsTrue(vm.IsCurrentStageUnlocked);
                hud.InitUI(new VillageHudArg { Vm = vm });
                Assert.IsTrue(glow.activeSelf, "해금 — 글로우 켜짐");
                Assert.IsTrue(gloss.activeSelf, "해금 — 광택 켜짐");

                vm.MoveStage(4);
                Assert.IsFalse(vm.IsCurrentStageUnlocked);
                Assert.IsFalse(glow.activeSelf, "잠금 — 글로우 꺼짐");
                Assert.IsFalse(gloss.activeSelf, "잠금 — 광택 꺼짐");

                vm.MoveStage(-4);
                Assert.IsTrue(vm.IsCurrentStageUnlocked);
                Assert.IsTrue(glow.activeSelf, "복귀 — 글로우 켜짐");
                Assert.IsTrue(gloss.activeSelf, "복귀 — 광택 켜짐");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(glow);
                UnityEngine.Object.DestroyImmediate(gloss);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshStage는_글로우만_연결돼도_광택_null에서_예외가_없다()
        {
            GameObject root;
            VillageHud hud = MakeBareHud(out root);
            GameObject glow = new GameObject("DecoSortieGlow");
            try
            {
                SetField(hud, "_sortieGlow", glow);
                VillageViewModel vm = MakeVm(1, 0);

                Assert.DoesNotThrow(() => hud.InitUI(new VillageHudArg { Vm = vm }));
                Assert.IsTrue(glow.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(glow);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
