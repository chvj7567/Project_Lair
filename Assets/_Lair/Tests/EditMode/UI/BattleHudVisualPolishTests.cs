using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ChvjUnityInfra;
using Lair.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 배틀 HUD 비주얼 폴리시 스위트(battle-hud-visual-polish §8.2) — 프리팹 연결·판정·움직임·장식 레이캐스트·스프라이트·View 전환.
    public class BattleHudVisualPolishTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string GeneratorPath = "Assets/_Lair/Editor/BattleHudSpriteGenerator.cs";

        private static readonly string[] PolishPrefabs =
        {
            "BattleHud", "BuildSynergyPanel", "BuildSynergyCell", "BuildIconCell", "SpawnerStatusCell",
        };

        private static readonly string[] AllSprites =
        {
            "BattleHudBossFill", "BattleHudBossFillLow", "BattleHudBossFrame", "BattleHudSmallFrame",
            "BattleHudTrackFrame", "BattleHudMiniFill", "BattleHudGaugeFill", "BattleHudChipBar",
        };

        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void 생성한_오브젝트를_정리한다()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _spawned.Clear();
        }

        //# --- 공용 헬퍼 ---

        private static FieldInfo FieldOf(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재 — 시그니처 변경 감지");
            return field;
        }

        private static object Field(object target, string name)
        {
            return FieldOf(target, name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldOf(target, name).SetValue(target, value);
        }

        private static GameObject LoadPrefab(string name)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + name + ".prefab");
            Assert.IsNotNull(root, name);
            return root;
        }

        private static T LoadComponent<T>(string prefabName) where T : Component
        {
            T comp = LoadPrefab(prefabName).GetComponent<T>();
            Assert.IsNotNull(comp, $"{prefabName}.{typeof(T).Name}");
            return comp;
        }

        //# 단일 위젯이면 비어 있지 않음, 배열이면 길이 기대치와 모든 원소 비어 있지 않음.
        private static void AssertConnected(object owner, string name, int expectedLength = -1)
        {
            object value = Field(owner, name);
            string label = $"{owner.GetType().Name}.{name}";
            Array array = value as Array;
            if (array != null)
            {
                Assert.Greater(array.Length, 0, label + " 길이");
                if (expectedLength >= 0)
                    Assert.AreEqual(expectedLength, array.Length, label + " 길이");
                for (int i = 0; i < array.Length; ++i)
                {
                    UnityEngine.Object element = array.GetValue(i) as UnityEngine.Object;
                    Assert.IsTrue(element != null, $"{label}[{i}] 비어 있음");
                }
                return;
            }
            UnityEngine.Object obj = value as UnityEngine.Object;
            Assert.IsTrue(obj != null, label + " 비어 있음");
        }

        private static void AssertAllConnected(object owner, params string[] names)
        {
            foreach (string name in names)
            {
                AssertConnected(owner, name);
            }
        }

        private static TextureImporter Importer(string name)
        {
            return AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
        }

        //# EditMode 에서는 [ExecuteAlways] 아닌 MonoBehaviour 의 OnEnable 이 자동 호출되지 않아 직접 호출한다.
        private static void InvokeOnEnable(object target)
        {
            MethodInfo method = target.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, target.GetType().Name + ".OnEnable");
            method.Invoke(target, null);
        }

        private static GameObject GameObjectOf(object widget)
        {
            GameObject go = widget as GameObject;
            if (go != null)
                return go;
            Component comp = widget as Component;
            return comp != null ? comp.gameObject : null;
        }

        private static Sprite MakeSprite(string name)
        {
            Texture2D tex = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            return sprite;
        }

        private GameObject NewGo(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            _spawned.Add(go);
            return go;
        }

        private CHText NewText(string name, out TextMeshProUGUI tmp)
        {
            GameObject go = NewGo(name);
            tmp = go.AddComponent<TextMeshProUGUI>();
            return go.AddComponent<CHText>();
        }

        //# --- ① 신규 SerializeField 8개 프리팹 연결 ---

        [Test]
        public void BattleHud_신규_타이머_경고_필드가_연결되고_이름과_소유자가_기획서와_같다()
        {
            BattleHud hud = LoadComponent<BattleHud>("BattleHud");
            GameObject glow = (GameObject)Field(hud, "_timerWarnGlow");
            UILoopMotion motion = (UILoopMotion)Field(hud, "_timerWarnTextMotion");
            CHText timerText = (CHText)Field(hud, "_timerText");

            Assert.IsTrue(glow != null, "_timerWarnGlow");
            Assert.IsTrue(motion != null, "_timerWarnTextMotion");
            Assert.AreEqual("DecoTimerWarnGlow", glow.name);
            Assert.AreSame(timerText.gameObject, motion.gameObject, "맥동은 TimerText 소유");
            Assert.AreEqual("TimerText", motion.gameObject.name);
        }

        [Test]
        public void ActiveCountdownView_신규_임박_글로우가_연결되고_이름이_기획서와_같다()
        {
            ActiveCountdownView view = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<ActiveCountdownView>(true);
            Assert.IsTrue(view != null, "ActiveCountdownView");

            GameObject glow = (GameObject)Field(view, "_imminentGlow");
            Assert.IsTrue(glow != null, "_imminentGlow");
            Assert.AreEqual("DecoCountdownGlow", glow.name);
        }

        [Test]
        public void BossHpBarView_신규_fill_스프라이트_2종이_연결되고_이름이_기획서와_같다()
        {
            BossHpBarView bar = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<BossHpBarView>(true);
            Assert.IsTrue(bar != null, "BossHpBarView");

            Sprite normal = (Sprite)Field(bar, "_fillNormalSprite");
            Sprite low = (Sprite)Field(bar, "_fillLowSprite");
            Assert.IsTrue(normal != null, "_fillNormalSprite");
            Assert.IsTrue(low != null, "_fillLowSprite");
            Assert.AreEqual("BattleHudBossFill", normal.name);
            Assert.AreEqual("BattleHudBossFillLow", low.name);
        }

        [Test]
        public void BuildIconCell_신규_kindGlow가_연결되고_이름이_기획서와_같다()
        {
            BuildIconCell cell = LoadComponent<BuildIconCell>("BuildIconCell");
            Image glow = (Image)Field(cell, "_kindGlow");

            Assert.IsTrue(glow != null, "_kindGlow");
            Assert.AreEqual("DecoKindGlow", glow.name);
        }

        [Test]
        public void BuildSynergyCell_신규_activeGlows는_길이2이고_순서가_행_트랙이다()
        {
            BuildSynergyCell cell = LoadComponent<BuildSynergyCell>("BuildSynergyCell");
            Image[] glows = (Image[])Field(cell, "_activeGlows");

            Assert.IsNotNull(glows, "_activeGlows");
            Assert.AreEqual(2, glows.Length);
            Assert.IsTrue(glows[0] != null, "_activeGlows[0]");
            Assert.IsTrue(glows[1] != null, "_activeGlows[1]");
            Assert.AreEqual("DecoRowGlow", glows[0].name);
            Assert.AreEqual("DecoTrackGlow", glows[1].name);
        }

        //# --- ① 기존 연결 보존 (재직렬화 클로버 가드) ---

        [Test]
        public void BattleHud_기존_필드가_모두_연결돼_있다()
        {
            BattleHud hud = LoadComponent<BattleHud>("BattleHud");
            AssertAllConnected(hud, "_timerText", "_bossBar", "_activeCountdown", "_buildPanel", "_spawnerStatusPanel", "_synergyPanel");
        }

        [Test]
        public void BossHpBarView_기존_필드와_배열_길이가_보존된다()
        {
            BossHpBarView bar = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<BossHpBarView>(true);
            AssertAllConnected(bar, "_title", "_hpText", "_fill", "_lag", "_statusIconRow");
            AssertConnected(bar, "_ticks", 9);
            AssertConnected(bar, "_tickGems", 9);
            AssertConnected(bar, "_iconSlots", 8);
        }

        [Test]
        public void ActiveCountdownView_기존_필드가_연결돼_있다()
        {
            ActiveCountdownView view = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<ActiveCountdownView>(true);
            AssertAllConnected(view, "_value", "_miniBar");
        }

        [Test]
        public void BuildPanel_기존_필드가_연결돼_있다()
        {
            BuildPanel panel = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<BuildPanel>(true);
            Assert.IsTrue(panel != null, "BuildPanel");
            AssertAllConnected(panel,
                "_passiveScrollView", "_activeScrollView", "_rootButton", "_body", "_foldedTab", "_collapseButton", "_expandButton",
                "_passiveCountText", "_activeCountText", "_foldedPassiveText", "_foldedActiveText");
        }

        [Test]
        public void BuildSynergyPanel_기존_필드가_연결돼_있다()
        {
            BuildSynergyPanel panel = LoadComponent<BuildSynergyPanel>("BuildSynergyPanel");
            AssertAllConnected(panel, "_scrollView", "_rootButton", "_body", "_foldedTab", "_collapseButton", "_expandButton");
            AssertConnected(panel, "_foldedDots", 4);
        }

        [Test]
        public void BuildSynergyCell_기존_필드가_연결돼_있다()
        {
            BuildSynergyCell cell = LoadComponent<BuildSynergyCell>("BuildSynergyCell");
            //# _tierMarkers 는 변경 전부터 빈 배열(리디자인 모드는 마커 미사용)이라 연결 필수 목록에서 제외.
            Assert.IsNotNull(Field(cell, "_tierMarkers"), "_tierMarkers 배열 자체는 직렬화돼 있다");
            AssertAllConnected(cell, "_background", "_text", "_axisIcon", "_axisStrip", "_group", "_countText", "_track");
        }

        [Test]
        public void BuildIconCell_기존_필드가_연결돼_있다()
        {
            BuildIconCell cell = LoadComponent<BuildIconCell>("BuildIconCell");
            AssertAllConnected(cell, "_iconImage", "_frameImage", "_countText", "_button", "_kindRing", "_emptyHatch", "_countBg");
        }

        [Test]
        public void SpawnerStatusCell_기존_필드가_연결돼_있다()
        {
            SpawnerStatusCell cell = LoadComponent<SpawnerStatusCell>("SpawnerStatusCell");
            //# _border 는 변경 전(7fb73e8~1)부터 의도적 null 이라 연결 필수 목록에서 제외.
            AssertAllConnected(cell,
                "_colorChip", "_icon", "_speciesText", "_countText", "_progressFill", "_periodText", "_button",
                "_glowOverlay", "_levelBadge", "_levelBadgeRoot", "_iconRect", "_maxLevelRing");
        }

        //# --- ② 정적 순수 판정 ---

        [TestCase(1.0f, false)]
        [TestCase(0.31f, false)]
        [TestCase(0.30f, true)]
        [TestCase(0.29f, true)]
        [TestCase(0.0f, true)]
        public void IsLowHp는_30퍼센트_이하에서_참이다(float ratio, bool expected)
        {
            Assert.AreEqual(expected, BossHpBarView.IsLowHp(ratio));
        }

        [TestCase(true, 5.0f, true)]
        [TestCase(true, 5.01f, false)]
        [TestCase(true, 0.5f, true)]
        [TestCase(true, 0.0f, true)]
        [TestCase(true, 30.0f, false)]
        [TestCase(false, 0.0f, false)]
        [TestCase(false, 3.0f, false)]
        public void IsImminent는_다음_카드가_있고_5초_이하일_때만_참이다(bool hasNext, float remain, bool expected)
        {
            ActiveCountdown c = new ActiveCountdown { HasNext = hasNext, RemainingSeconds = remain };
            Assert.AreEqual(expected, ActiveCountdownView.IsImminent(c));
        }

        [Test]
        public void 판정_상수는_기획서_값이다()
        {
            Assert.AreEqual(0.3f, BossHpBarView.LowHpRatio, 0.0001f);
            Assert.AreEqual(5f, ActiveCountdownView.ImminentRemainSeconds, 0.0001f);
        }

        //# --- ③ UILoopMotion / 글로우 기본 비활성 ---

        [Test]
        public void UILoopMotion은_BattleHud에_정확히_3개이고_대상이_기획서와_같다()
        {
            UILoopMotion[] motions = LoadPrefab("BattleHud").GetComponentsInChildren<UILoopMotion>(true);

            Assert.AreEqual(3, motions.Length);
            List<string> names = new List<string>();
            foreach (UILoopMotion motion in motions)
            {
                names.Add(motion.gameObject.name);
            }
            CollectionAssert.AreEquivalent(new[] { "DecoTimerWarnGlow", "TimerText", "DecoCountdownGlow" }, names);
        }

        private static UILoopMotion MotionOn(string objectName)
        {
            foreach (UILoopMotion motion in LoadPrefab("BattleHud").GetComponentsInChildren<UILoopMotion>(true))
            {
                if (motion.gameObject.name == objectName)
                    return motion;
            }
            Assert.Fail(objectName + " 에 UILoopMotion 없음");
            return null;
        }

        private static void AssertPulse(UILoopMotion motion, float period, float alphaFrom, float alphaTo, float scaleFrom, float scaleTo)
        {
            Assert.AreEqual("Pulse", Field(motion, "_kind").ToString());
            Assert.AreEqual(period, (float)Field(motion, "_period"), 0.0001f, "_period");
            Assert.AreEqual(alphaFrom, (float)Field(motion, "_alphaFrom"), 0.0001f, "_alphaFrom");
            Assert.AreEqual(alphaTo, (float)Field(motion, "_alphaTo"), 0.0001f, "_alphaTo");
            Assert.AreEqual(scaleFrom, (float)Field(motion, "_scaleFrom"), 0.0001f, "_scaleFrom");
            Assert.AreEqual(scaleTo, (float)Field(motion, "_scaleTo"), 0.0001f, "_scaleTo");
        }

        [Test]
        public void DecoTimerWarnGlow_Pulse는_주기10_알파045에서085_스케일불변이고_자기_Image를_쓴다()
        {
            UILoopMotion motion = MotionOn("DecoTimerWarnGlow");

            AssertPulse(motion, 1.0f, 0.45f, 0.85f, 1.00f, 1.00f);
            Assert.AreSame(motion.GetComponent<Image>(), Field(motion, "_graphic"), "_graphic = 자기 Image");
        }

        [Test]
        public void TimerText_Pulse는_주기10_스케일100에서107이고_graphic을_비운다()
        {
            UILoopMotion motion = MotionOn("TimerText");

            Assert.AreEqual("Pulse", Field(motion, "_kind").ToString());
            Assert.AreEqual(1.0f, (float)Field(motion, "_period"), 0.0001f);
            Assert.AreEqual(1.00f, (float)Field(motion, "_scaleFrom"), 0.0001f);
            Assert.AreEqual(1.07f, (float)Field(motion, "_scaleTo"), 0.0001f);
            Assert.IsFalse((UnityEngine.Object)Field(motion, "_graphic") != null, "글자 색은 SetColor 가 관리 — _graphic 비움");
        }

        [Test]
        public void DecoCountdownGlow_Pulse는_주기11_알파045에서090_스케일불변이고_자기_Image를_쓴다()
        {
            UILoopMotion motion = MotionOn("DecoCountdownGlow");

            AssertPulse(motion, 1.1f, 0.45f, 0.90f, 1.00f, 1.00f);
            Assert.AreSame(motion.GetComponent<Image>(), Field(motion, "_graphic"), "_graphic = 자기 Image");
        }

        [Test]
        public void 경고_글로우와_임박_글로우는_프리팹_기본_비활성이다()
        {
            BattleHud hud = LoadComponent<BattleHud>("BattleHud");
            GameObject timerGlow = (GameObject)Field(hud, "_timerWarnGlow");
            ActiveCountdownView view = hud.GetComponentInChildren<ActiveCountdownView>(true);
            GameObject countdownGlow = (GameObject)Field(view, "_imminentGlow");

            Assert.IsFalse(timerGlow.activeSelf, "DecoTimerWarnGlow");
            Assert.IsFalse(countdownGlow.activeSelf, "DecoCountdownGlow");
        }

        //# --- ④ 장식 레이캐스트 ---

        [TestCase("BattleHud")]
        [TestCase("BuildSynergyPanel")]
        [TestCase("BuildSynergyCell")]
        [TestCase("BuildIconCell")]
        [TestCase("SpawnerStatusCell")]
        public void Deco_접두어_오브젝트의_모든_Graphic은_raycastTarget이_꺼져_있다(string prefabName)
        {
            int decoCount = 0;
            int graphicCount = 0;
            foreach (Transform t in LoadPrefab(prefabName).GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Deco") == false)
                    continue;
                ++decoCount;
                foreach (Graphic graphic in t.GetComponentsInChildren<Graphic>(true))
                {
                    ++graphicCount;
                    Assert.IsFalse(graphic.raycastTarget, $"{prefabName}/{t.name}/{graphic.name} raycastTarget");
                }
            }

            Assert.Greater(decoCount, 0, prefabName + " Deco* 오브젝트 없음");
            Assert.Greater(graphicCount, 0, prefabName + " Deco* Graphic 없음");
        }

        [Test]
        public void 장식_검사_대상_프리팹은_5종이다()
        {
            Assert.AreEqual(5, PolishPrefabs.Length);
            foreach (string name in PolishPrefabs)
            {
                Assert.IsTrue(File.Exists(UiDir + name + ".prefab"), name);
            }
        }

        //# --- ⑤ 스프라이트 임포트 ---

        [Test]
        public void 신규_스프라이트_8종이_존재하고_공통_임포트_설정을_따른다()
        {
            Assert.AreEqual(8, AllSprites.Length);
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

        [TestCase("BattleHudBossFrame", 5, 5, 5, 5)]
        [TestCase("BattleHudSmallFrame", 3, 3, 3, 3)]
        [TestCase("BattleHudTrackFrame", 2, 2, 2, 2)]
        public void 슬라이스9_스프라이트의_border가_기획서_표와_같다(string name, int left, int bottom, int right, int top)
        {
            Assert.AreEqual(new Vector4(left, bottom, right, top), Importer(name).spriteBorder, name);
        }

        [TestCase("BattleHudBossFill")]
        [TestCase("BattleHudBossFillLow")]
        [TestCase("BattleHudMiniFill")]
        [TestCase("BattleHudGaugeFill")]
        [TestCase("BattleHudChipBar")]
        public void 슬라이스_대상이_아닌_스프라이트는_border가_0이다(string name)
        {
            Assert.AreEqual(Vector4.zero, Importer(name).spriteBorder, name);
        }

        [TestCase("BattleHudBossFrame", FilterMode.Point)]
        [TestCase("BattleHudSmallFrame", FilterMode.Point)]
        [TestCase("BattleHudTrackFrame", FilterMode.Point)]
        [TestCase("BattleHudBossFill", FilterMode.Bilinear)]
        [TestCase("BattleHudBossFillLow", FilterMode.Bilinear)]
        [TestCase("BattleHudMiniFill", FilterMode.Bilinear)]
        [TestCase("BattleHudGaugeFill", FilterMode.Bilinear)]
        [TestCase("BattleHudChipBar", FilterMode.Bilinear)]
        public void 필터_모드가_기획서_표와_같다(string name, FilterMode expected)
        {
            Assert.AreEqual(expected, Importer(name).filterMode, name);
        }

        [TestCase("BattleHudBossFill", 4, 20)]
        [TestCase("BattleHudBossFillLow", 4, 20)]
        [TestCase("BattleHudBossFrame", 16, 16)]
        [TestCase("BattleHudSmallFrame", 16, 16)]
        [TestCase("BattleHudTrackFrame", 8, 8)]
        [TestCase("BattleHudMiniFill", 4, 5)]
        [TestCase("BattleHudGaugeFill", 4, 6)]
        [TestCase("BattleHudChipBar", 10, 62)]
        public void 스프라이트_픽셀_크기가_기획서_표와_같다(string name, int width, int height)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");

            Assert.IsNotNull(sprite, name);
            Assert.AreEqual(width, (int)sprite.rect.width, $"{name} 폭");
            Assert.AreEqual(height, (int)sprite.rect.height, $"{name} 높이");
        }

        //# --- ⑥ 일회용 생성기 삭제 (Rule 04 §3) ---

        [Test]
        public void 일회용_스프라이트_생성기_스크립트가_레포에_없다()
        {
            Assert.IsFalse(File.Exists(GeneratorPath), GeneratorPath);
            Assert.IsFalse(File.Exists(GeneratorPath + ".meta"), GeneratorPath + ".meta");
            foreach (string file in Directory.GetFiles("Assets/_Lair/Editor", "*.cs", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("BattleHudSpriteGenerator", Path.GetFileName(file), file);
            }
        }

        //# --- ⑦ View 동작: 보스 fill 전환 ---

        private BossHpBarView MakeBossBar(out Image fill, out Sprite normal, out Sprite low)
        {
            GameObject go = NewGo("BossBarTest");
            BossHpBarView bar = go.AddComponent<BossHpBarView>();
            fill = NewGo("Fill").AddComponent<Image>();
            fill.transform.SetParent(go.transform, false);
            normal = MakeSprite("N");
            low = MakeSprite("L");
            _spawned.Add(normal.texture);
            _spawned.Add(normal);
            _spawned.Add(low.texture);
            _spawned.Add(low);
            SetField(bar, "_fill", fill);
            SetField(bar, "_fillNormalSprite", normal);
            SetField(bar, "_fillLowSprite", low);
            return bar;
        }

        [Test]
        public void SetHp는_30퍼센트_이하에서만_낮음_스프라이트로_바꾸고_회복하면_되돌린다()
        {
            Image fill;
            Sprite normal;
            Sprite low;
            BossHpBarView bar = MakeBossBar(out fill, out normal, out low);

            bar.SetHp(31, 100);
            Assert.AreSame(normal, fill.sprite, "31%");
            bar.SetHp(30, 100);
            Assert.AreSame(low, fill.sprite, "30%");
            bar.SetHp(0, 100);
            Assert.AreSame(low, fill.sprite, "0%");
            bar.SetHp(60, 100);
            Assert.AreSame(normal, fill.sprite, "60% 회복");
        }

        [Test]
        public void SetHp는_max가_0이면_낮음_상태로_처리하고_예외가_없다()
        {
            Image fill;
            Sprite normal;
            Sprite low;
            BossHpBarView bar = MakeBossBar(out fill, out normal, out low);

            Assert.DoesNotThrow(() => bar.SetHp(0, 0));
            Assert.AreSame(low, fill.sprite);
        }

        [Test]
        public void SetHp는_재활성화하면_상태_캐시가_리셋돼_평상시_스프라이트를_다시_적용한다()
        {
            Image fill;
            Sprite normal;
            Sprite low;
            BossHpBarView bar = MakeBossBar(out fill, out normal, out low);
            bar.SetHp(10, 100);
            Assert.AreSame(low, fill.sprite);

            InvokeOnEnable(bar);
            fill.sprite = null;
            bar.SetHp(10, 100);

            Assert.AreSame(low, fill.sprite, "캐시 리셋 후 같은 상태도 다시 적용");
        }

        [Test]
        public void SetHp는_필드가_비어_있어도_예외가_없다()
        {
            BossHpBarView empty = NewGo("EmptyBar").AddComponent<BossHpBarView>();
            Assert.DoesNotThrow(() => empty.SetHp(10, 100));

            Image fill;
            Sprite normal;
            Sprite low;
            BossHpBarView noSprites = MakeBossBar(out fill, out normal, out low);
            SetField(noSprites, "_fillNormalSprite", null);
            SetField(noSprites, "_fillLowSprite", null);
            Sprite before = fill.sprite;
            Assert.DoesNotThrow(() => noSprites.SetHp(10, 100));
            Assert.AreSame(before, fill.sprite, "스프라이트 미연결이면 건드리지 않음");
        }

        //# --- ⑦⑧ View 동작: 카운트다운 임박 전환 ---

        private static readonly Color BoneColor = new Color32(0xE8, 0xE1, 0xCF, 0xFF);

        private ActiveCountdownView MakeCountdown(out GameObject glow, out TextMeshProUGUI tmp, Color normal)
        {
            GameObject go = NewGo("CountdownTest");
            ActiveCountdownView view = go.AddComponent<ActiveCountdownView>();
            glow = NewGo("DecoCountdownGlow");
            glow.SetActive(false);
            CHText value = NewText("Value", out tmp);
            value.transform.SetParent(go.transform, false);
            SetField(view, "_imminentGlow", glow);
            SetField(view, "_value", value);
            SetField(view, "_valueNormalColor", normal);
            return view;
        }

        [Test]
        public void Show는_임박_진입과_이탈에서_글로우와_숫자_색을_전환한다()
        {
            GameObject glow;
            TextMeshProUGUI tmp;
            ActiveCountdownView view = MakeCountdown(out glow, out tmp, BoneColor);

            view.Show(new ActiveCountdown { HasNext = true, RemainingSeconds = 30f, Progress = 0f });
            Assert.IsFalse(glow.activeSelf, "30초 — 글로우 꺼짐");
            Assert.AreEqual(BoneColor, tmp.color, "30초 — 평상시 색");

            view.Show(new ActiveCountdown { HasNext = true, RemainingSeconds = 4.9f, Progress = 0.9f });
            Assert.IsTrue(glow.activeSelf, "4.9초 — 글로우 켜짐");
            Assert.AreEqual(UiDotPalette.Gold, tmp.color, "4.9초 — 금색");

            view.Show(new ActiveCountdown { HasNext = true, RemainingSeconds = 30f, Progress = 0f });
            Assert.IsFalse(glow.activeSelf, "복귀 — 글로우 꺼짐");
            Assert.AreEqual(BoneColor, tmp.color, "복귀 — 평상시 색");
        }

        [Test]
        public void Show는_다음_카드가_없으면_평상시로_처리한다()
        {
            GameObject glow;
            TextMeshProUGUI tmp;
            ActiveCountdownView view = MakeCountdown(out glow, out tmp, BoneColor);
            view.Show(new ActiveCountdown { HasNext = true, RemainingSeconds = 2f, Progress = 0.9f });
            Assert.IsTrue(glow.activeSelf);

            view.Show(new ActiveCountdown { HasNext = false, RemainingSeconds = 0f, Progress = 0f });

            Assert.IsFalse(glow.activeSelf);
            Assert.AreEqual(BoneColor, tmp.color);
        }

        [Test]
        public void Show는_필드가_비어_있어도_예외가_없다()
        {
            ActiveCountdownView empty = NewGo("EmptyCountdown").AddComponent<ActiveCountdownView>();

            Assert.DoesNotThrow(() => empty.Show(new ActiveCountdown { HasNext = true, RemainingSeconds = 3f }));
        }

        [Test]
        public void ActiveCountdown_프리팹의_valueNormalColor는_뼈색_E8E1CF다()
        {
            ActiveCountdownView view = LoadComponent<BattleHud>("BattleHud").GetComponentInChildren<ActiveCountdownView>(true);
            Color color = (Color)Field(view, "_valueNormalColor");
            Color expected = new Color32(0xE8, 0xE1, 0xCF, 0xFF);

            Assert.AreEqual(expected.r, color.r, 0.005f, "r");
            Assert.AreEqual(expected.g, color.g, 0.005f, "g");
            Assert.AreEqual(expected.b, color.b, 0.005f, "b");
            Assert.AreEqual(1f, color.a, 0.005f, "a");
        }

        //# --- ⑦ View 동작: 타이머 경고 전환 ---

        private static void InvokeHandleTimer(BattleHud hud, float elapsed, float total)
        {
            MethodInfo method = typeof(BattleHud).GetMethod("HandleTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "BattleHud.HandleTimer");
            method.Invoke(hud, new object[] { elapsed, total });
        }

        [Test]
        public void HandleTimer는_30초_경계에서_경고_글로우와_숫자_맥동을_전환한다()
        {
            GameObject hudGo = NewGo("BattleHudTest");
            BattleHud hud = hudGo.AddComponent<BattleHud>();
            TextMeshProUGUI tmp;
            CHText timer = NewText("TimerText", out tmp);
            timer.transform.SetParent(hudGo.transform, false);
            UILoopMotion motion = timer.gameObject.AddComponent<UILoopMotion>();
            GameObject glow = NewGo("DecoTimerWarnGlow");
            glow.SetActive(false);
            SetField(hud, "_timerText", timer);
            SetField(hud, "_timerWarnGlow", glow);
            SetField(hud, "_timerWarnTextMotion", motion);

            InvokeHandleTimer(hud, 269f, 300f);
            Assert.IsFalse(glow.activeSelf, "31초 — 글로우 꺼짐");
            Assert.IsFalse((bool)Field(motion, "_running"), "31초 — 맥동 정지");

            InvokeHandleTimer(hud, 270f, 300f);
            Assert.IsTrue(glow.activeSelf, "30초 — 글로우 켜짐");
            Assert.IsTrue((bool)Field(motion, "_running"), "30초 — 맥동 실행");
            Assert.AreEqual(UiDotPalette.TimerWarn, tmp.color, "30초 — 경고 색");

            InvokeHandleTimer(hud, 100f, 300f);
            Assert.IsFalse(glow.activeSelf, "복구 — 글로우 꺼짐");
            Assert.IsFalse((bool)Field(motion, "_running"), "복구 — 맥동 정지");
        }

        [Test]
        public void HandleTimer는_신규_필드가_비어_있어도_예외가_없다()
        {
            GameObject hudGo = NewGo("BattleHudBare");
            BattleHud hud = hudGo.AddComponent<BattleHud>();
            TextMeshProUGUI tmp;
            CHText timer = NewText("TimerText", out tmp);
            SetField(hud, "_timerText", timer);

            Assert.DoesNotThrow(() => InvokeHandleTimer(hud, 270f, 300f));
            Assert.DoesNotThrow(() => InvokeHandleTimer(hud, 0f, 300f));

            BattleHud noTimer = NewGo("BattleHudNoTimer").AddComponent<BattleHud>();
            Assert.DoesNotThrow(() => InvokeHandleTimer(noTimer, 270f, 300f));
        }

        //# --- ⑦ View 동작: 빌드 슬롯 ---

        [Test]
        public void BuildIconCell은_SetKind에서_종류색_발광을_켜고_SetEmpty에서_끄며_링을_어둡게_칠한다()
        {
            GameObject go = NewGo("BuildIconCellTest");
            BuildIconCell cell = go.AddComponent<BuildIconCell>();
            Image glow = NewGo("DecoKindGlow").AddComponent<Image>();
            Image ring = NewGo("KindRing").AddComponent<Image>();
            glow.transform.SetParent(go.transform, false);
            ring.transform.SetParent(go.transform, false);
            SetField(cell, "_kindGlow", glow);
            SetField(cell, "_kindRing", ring);

            cell.SetKind(true);
            Color soul = UiDotPalette.CardKind(true);
            Assert.IsTrue(glow.gameObject.activeSelf, "패시브 — 발광 켜짐");
            Assert.AreEqual(soul.r, glow.color.r, 0.005f);
            Assert.AreEqual(soul.g, glow.color.g, 0.005f);
            Assert.AreEqual(soul.b, glow.color.b, 0.005f);
            Assert.AreEqual(0.55f, glow.color.a, 0.005f);

            cell.SetKind(false);
            Color gold = UiDotPalette.CardKind(false);
            Assert.AreEqual(gold.r, glow.color.r, 0.005f, "액티브 r");
            Assert.AreEqual(gold.g, glow.color.g, 0.005f, "액티브 g");

            cell.SetEmpty();
            Color expected = new Color32(0x26, 0x2D, 0x40, 0xFF);
            Assert.IsFalse(glow.gameObject.activeSelf, "빈 슬롯 — 발광 꺼짐");
            Assert.AreEqual(expected.r, ring.color.r, 0.005f, "링 r");
            Assert.AreEqual(expected.g, ring.color.g, 0.005f, "링 g");
            Assert.AreEqual(expected.b, ring.color.b, 0.005f, "링 b");
        }

        [Test]
        public void BuildIconCell은_풀_재활성화_시_발광을_비활성화한다()
        {
            GameObject go = NewGo("BuildIconCellPool");
            BuildIconCell cell = go.AddComponent<BuildIconCell>();
            Image glow = NewGo("DecoKindGlow").AddComponent<Image>();
            glow.transform.SetParent(go.transform, false);
            SetField(cell, "_kindGlow", glow);
            cell.SetKind(true);
            Assert.IsTrue(glow.gameObject.activeSelf);

            InvokeOnEnable(cell);

            Assert.IsFalse(glow.gameObject.activeSelf);
        }

        [Test]
        public void BuildIconCell은_신규_필드가_비어_있어도_예외가_없다()
        {
            BuildIconCell cell = NewGo("BuildIconCellBare").AddComponent<BuildIconCell>();

            Assert.DoesNotThrow(() => cell.SetKind(true));
            Assert.DoesNotThrow(() => cell.SetEmpty());
            Assert.DoesNotThrow(() => InvokeOnEnable(cell));
        }

        //# --- ⑦ View 동작: 시너지 셀 (필드 비어 있음) ---

        [Test]
        public void BuildSynergyCell은_activeGlows가_비어_있어도_Bind에서_예외가_없다()
        {
            BuildSynergyCell cell = NewGo("SynergyCellBare").AddComponent<BuildSynergyCell>();
            Assert.DoesNotThrow(() => cell.Bind(new BuildSynergyCellData()));

            BuildSynergyCell redesign = NewGo("SynergyCellRedesign").AddComponent<BuildSynergyCell>();
            SetField(redesign, "_axisIcon", NewGo("AxisIcon").AddComponent<Image>());
            Assert.DoesNotThrow(() => redesign.Bind(new BuildSynergyCellData { Count = 2, Color = Color.red }));

            SetField(redesign, "_activeGlows", new Image[] { null, null });
            Assert.DoesNotThrow(() => redesign.Bind(new BuildSynergyCellData { Count = 2, Color = Color.red }));
        }

        [Test]
        public void BuildSynergyCell은_장수에_따라_활성_글로우를_토글하고_축_색에_프리팹_알파를_유지한다()
        {
            BuildSynergyCell cell = NewGo("SynergyCellGlow").AddComponent<BuildSynergyCell>();
            SetField(cell, "_axisIcon", NewGo("AxisIcon").AddComponent<Image>());
            Image row = NewGo("DecoRowGlow").AddComponent<Image>();
            Image track = NewGo("DecoTrackGlow").AddComponent<Image>();
            row.color = new Color(1f, 1f, 1f, 0.40f);
            track.color = new Color(1f, 1f, 1f, 0.55f);
            SetField(cell, "_activeGlows", new[] { row, track });
            Color axis = new Color(0.2f, 0.6f, 0.9f, 1f);

            cell.Bind(new BuildSynergyCellData { Count = 0, Color = axis });
            Assert.IsFalse(row.gameObject.activeSelf, "0장 — 행 글로우 꺼짐");
            Assert.IsFalse(track.gameObject.activeSelf, "0장 — 트랙 글로우 꺼짐");

            cell.Bind(new BuildSynergyCellData { Count = 3, Color = axis });
            Assert.IsTrue(row.gameObject.activeSelf, "3장 — 행 글로우 켜짐");
            Assert.IsTrue(track.gameObject.activeSelf, "3장 — 트랙 글로우 켜짐");
            Assert.AreEqual(axis.r, row.color.r, 0.005f);
            Assert.AreEqual(axis.g, row.color.g, 0.005f);
            Assert.AreEqual(axis.b, row.color.b, 0.005f);
            Assert.AreEqual(0.40f, row.color.a, 0.005f, "행 알파 유지");
            Assert.AreEqual(0.55f, track.color.a, 0.005f, "트랙 알파 유지");

            cell.Bind(new BuildSynergyCellData { Count = 0, Color = axis });
            Assert.IsFalse(row.gameObject.activeSelf, "다시 0장 — 꺼짐");
        }
    }
}
