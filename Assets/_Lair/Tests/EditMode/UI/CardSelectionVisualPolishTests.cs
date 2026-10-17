using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 카드 선택 팝업 비주얼 폴리시 스위트(card-selection-visual-polish §8.1) — 프리팹 연결·Missing Script·좌표 회귀·레이캐스트·연출 상태·팝업 전파·스프라이트 임포트.
    //# 기존 CardSelectPolishTests(합성 CardView 최소 검증)와 달리 실제 프리팹 인스턴스·엣지·회귀를 다룬다.
    public class CardSelectionVisualPolishTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string PopupPath = "Assets/_Lair/Art/UI/CardSelectionPopup.prefab";

        private static readonly string[] KindTintNames =
        {
            "DecoBand", "DecoBandGlow", "DecoArtRing", "DecoNameLine", "DecoKindLabelGlow", "DecoKindLabelFrame", "DecoPickGlow",
        };

        private static readonly string[] PopupKindTintNames =
        {
            "DecoTitleGlow", "DecoSubtitleGlow", "DecoSubtitlePill", "DecoSubtitleDiamondL", "DecoSubtitleDiamondR",
        };

        private static readonly string[] CardOrder =
        {
            "DecoHoverGlow", "Bg", "DecoFaceGradient", "DecoFaceGloss", "Border", "KindRing", "DecoFrameGold", "DecoBandGlow", "DecoBand",
            "DecoArtRing", "ArtSlot", "CardArt", "DecoArtVignette", "Scrim", "NameText", "DecoNameLine", "DecoDescFill", "DecoDescTop",
            "DecoDescFrame", "DescText", "DecoCornerTL", "DecoCornerTR", "DecoCornerBL", "DecoCornerBR", "CountBadgeBg", "CountBadge",
            "DecoKindLabelGlow", "KindLabelBg", "DecoKindLabelFrame", "KindLabel", "PickButton", "DecoAxisGlow", "AxisIcon",
            "DecoAxisGem1", "DecoAxisGem2", "DecoAxisGem3", "DecoDimOverlay",
        };

        private static readonly string[] PopupOrder =
        {
            "Dim", "DecoRuneL", "DecoRuneR", "DecoTitleGlow", "DecoTitlePlaque", "Title", "DecoSubtitleGlow", "DecoSubtitlePill",
            "DecoSubtitleDiamondL", "DecoSubtitleDiamondR", "Subtitle", "CardsLayout", "DecoCaptionDiamondL", "DecoCaptionDiamondR",
            "SynergyCaption", "SynergyChip_0", "SynergyChip_1", "SynergyChip_2", "SynergyChip_3",
        };

        private static readonly string[] LegacyCardFields =
        {
            "_nameText", "_descText", "_border", "_artImage", "_pickButton", "_countBadge", "_kindRing", "_kindLabel",
            "_pickButtonImage", "_passiveButtonSprite", "_activeButtonSprite", "_countBadgeBg", "_axisIconRoot", "_axisIconBorder", "_axisIconImage",
        };

        private static readonly Color RestFrame = new Color32(0x8A, 0x6A, 0x2C, 0xFF);
        private static readonly Color HoverFrame = new Color32(0xFF, 0xD6, 0x66, 0xFF);
        private static readonly Color PressFrame = new Color32(0xFF, 0xF0, 0xA8, 0xFF);
        private static readonly Color GemOff = new Color32(0x3A, 0x47, 0x64, 0xFF);

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
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                    return field;
            }
            Assert.Fail($"{target.GetType().Name}.{name} 필드 없음 — 시그니처 변경 감지");
            return null;
        }

        private static object Field(object target, string name)
        {
            return FieldOf(target, name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldOf(target, name).SetValue(target, value);
        }

        //# 리플렉션 값이 null 이면 NRE 대신 Assert.NotNull 로 먼저 실패시킨다.
        private static T Required<T>(object target, string name) where T : class
        {
            object value = Field(target, name);
            if (value is UnityEngine.Object unityObj)
            {
                Assert.IsTrue(unityObj != null, $"{target.GetType().Name}.{name} 비어 있음");
            }
            else
            {
                Assert.IsNotNull(value, $"{target.GetType().Name}.{name} 비어 있음");
            }
            return (T)value;
        }

        private static void Call(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{method} 메서드 없음");
            info.Invoke(target, null);
        }

        //# EditMode 에서는 OnEnable/OnDisable 이 자동 호출되지 않으므로 리플렉션으로 직접 호출한다(중복 구독 방지로 Disable 먼저).
        private static void Reenable(CardView view)
        {
            Call(view, "OnDisable");
            Call(view, "OnEnable");
        }

        private static GameObject LoadPrefabAsset()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
            Assert.IsNotNull(root, PopupPath);
            return root;
        }

        private static CardView[] PrefabCards()
        {
            CardView[] cards = LoadPrefabAsset().GetComponentsInChildren<CardView>(true);
            Assert.AreEqual(3, cards.Length, "프리팹 CardView 수");
            return cards;
        }

        private GameObject NewInstance()
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(LoadPrefabAsset()) as GameObject;
            Assert.IsNotNull(instance, "프리팹 인스턴스");
            _spawned.Add(instance);
            return instance;
        }

        private static Transform Child(Component parent, string name)
        {
            Transform t = parent.transform.Find(name);
            Assert.IsTrue(t != null, $"{parent.name}/{name} 없음");
            return t;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            Assert.Fail($"{root.name} 하위에 {name} 없음");
            return null;
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.01f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.01f, message + " b");
        }

        private static void AssertOrder(Transform parent, string[] expected, string label)
        {
            int previous = -1;
            foreach (string name in expected)
            {
                Transform child = parent.Find(name);
                Assert.IsTrue(child != null, $"{label}: {name} 없음");
                int index = child.GetSiblingIndex();
                Assert.Greater(index, previous, $"{label}: {name} 의 형제 순서가 앞 요소보다 뒤여야 한다");
                previous = index;
            }
        }

        private static float Alpha(Image image)
        {
            return image.color.a;
        }

        private CardData MakeCard(EBuildAxis axis)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            SetField(card, "_axis", axis);
            SetField(card, "_displayName", "테스트 카드");
            SetField(card, "_description", "테스트 설명");
            _spawned.Add(card);
            return card;
        }

        private Sprite NewSprite()
        {
            Texture2D tex = new Texture2D(12, 12);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f));
            _spawned.Add(sprite);
            _spawned.Add(tex);
            return sprite;
        }

        private SynergyVisualConfig MakeConfig(params EBuildAxis[] axes)
        {
            SynergyVisualConfig config = ScriptableObject.CreateInstance<SynergyVisualConfig>();
            SynergyVisualConfig.Entry[] entries = new SynergyVisualConfig.Entry[axes.Length];
            for (int i = 0; i < axes.Length; ++i)
            {
                entries[i] = new SynergyVisualConfig.Entry { Axis = axes[i], Icon = NewSprite() };
            }
            SetField(config, "_entries", entries);
            _spawned.Add(config);
            return config;
        }

        //# --- 합성 CardView (연출 상태 단위 검증용) ---

        private class Parts
        {
            public CardView View;
            public RectTransform Rt;
            public CardPointerRelay Relay;
            public Image Frame;
            public Image Glow;
            public Image Bright;
            public Image Dim;
            public GameObject Guard;
        }

        private GameObject NewGo(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Parts MakeSynthetic()
        {
            GameObject root = new GameObject("Card", typeof(RectTransform));
            _spawned.Add(root);
            Parts p = new Parts { View = root.AddComponent<CardView>(), Rt = (RectTransform)root.transform };
            p.Rt.anchoredPosition = new Vector2(12f, 100f);
            p.Frame = NewGo("Frame", root.transform).AddComponent<Image>();
            p.Frame.color = RestFrame;
            p.Glow = NewGo("Glow", root.transform).AddComponent<Image>();
            p.Glow.color = new Color(0.97f, 0.78f, 0.29f, 0f);
            p.Bright = NewGo("Bright", root.transform).AddComponent<Image>();
            p.Bright.color = new Color(1f, 1f, 1f, 0f);
            p.Dim = NewGo("Dim", root.transform).AddComponent<Image>();
            p.Dim.color = new Color(0.02f, 0.03f, 0.05f, 0f);
            p.Guard = NewGo("Guard", root.transform);
            p.Guard.SetActive(false);
            p.Relay = NewGo("Pick", root.transform).AddComponent<CardPointerRelay>();

            SetField(p.View, "_frameGold", p.Frame);
            SetField(p.View, "_hoverGlow", p.Glow);
            SetField(p.View, "_pickBright", p.Bright);
            SetField(p.View, "_dimOverlay", p.Dim);
            SetField(p.View, "_hoverGuard", p.Guard);
            SetField(p.View, "_pointerRelay", p.Relay);
            SetField(p.View, "_tweenSeconds", 0f);
            Reenable(p.View);
            return p;
        }

        private static void AssertRest(Parts p, float baseY, string label)
        {
            AssertRgb(RestFrame, p.Frame.color, label + " 프레임");
            Assert.AreEqual(0f, Alpha(p.Glow), 0.001f, label + " 글로우");
            Assert.AreEqual(0f, Alpha(p.Bright), 0.001f, label + " 밝기");
            Assert.AreEqual(0f, Alpha(p.Dim), 0.001f, label + " 어둠");
            Assert.AreEqual(1f, p.View.transform.localScale.x, 0.001f, label + " 스케일");
            Assert.AreEqual(baseY, p.Rt.anchoredPosition.y, 0.001f, label + " y");
            Assert.IsFalse(p.Guard.activeSelf, label + " 보호 영역");
        }

        //# --- ① 프리팹 연결 ---

        [Test]
        public void 카드_3장_신규_배열_필드의_길이와_순서가_기획서와_일치한다()
        {
            foreach (CardView card in PrefabCards())
            {
                Image[] kind = Required<Image[]>(card, "_kindTintImages");
                Assert.AreEqual(KindTintNames.Length, kind.Length, card.name + " _kindTintImages 길이");
                for (int i = 0; i < kind.Length; ++i)
                {
                    Assert.IsTrue(kind[i] != null, $"{card.name} _kindTintImages[{i}] 비어 있음");
                    Assert.AreEqual(KindTintNames[i], kind[i].name, $"{card.name} _kindTintImages[{i}]");
                }

                Image[] axis = Required<Image[]>(card, "_axisTintImages");
                Assert.AreEqual(1, axis.Length, card.name + " _axisTintImages 길이");
                Assert.IsTrue(axis[0] != null);
                Assert.AreEqual("DecoAxisGlow", axis[0].name);

                Image[] gems = Required<Image[]>(card, "_axisGems");
                Assert.AreEqual(3, gems.Length, card.name + " _axisGems 길이");
                for (int i = 0; i < gems.Length; ++i)
                {
                    Assert.IsTrue(gems[i] != null, $"{card.name} _axisGems[{i}] 비어 있음");
                    Assert.AreEqual("DecoAxisGem" + (i + 1), gems[i].name);
                    Assert.IsNotNull(gems[i].sprite, gems[i].name + " 스프라이트");
                    Assert.AreEqual("VillageHudGem", gems[i].sprite.name, gems[i].name + " 스프라이트");
                }
            }
        }

        [Test]
        public void 카드_3장_신규_단일_필드가_모두_연결되고_이름이_일치한다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.AreEqual("DecoFrameGold", Required<Image>(card, "_frameGold").name, card.name);
                Assert.AreEqual("DecoHoverGlow", Required<Image>(card, "_hoverGlow").name, card.name);
                Assert.AreEqual("DecoPickBright", Required<Image>(card, "_pickBright").name, card.name);
                Assert.AreEqual("DecoDimOverlay", Required<Image>(card, "_dimOverlay").name, card.name);
                Assert.AreEqual("DecoHoverGuard", Required<GameObject>(card, "_hoverGuard").name, card.name);
                Assert.IsNotNull(Required<CardPointerRelay>(card, "_pointerRelay"), card.name);
            }
        }

        [Test]
        public void 카드_3장_tweenSeconds_기본값은_0_16이다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.AreEqual(0.16f, (float)Field(card, "_tweenSeconds"), 0.0001f, card.name);
            }
        }

        [Test]
        public void 카드_3장_기존_필드_15개가_유지된다()
        {
            foreach (CardView card in PrefabCards())
            {
                foreach (string name in LegacyCardFields)
                {
                    Required<UnityEngine.Object>(card, name);
                }
            }
        }

        [Test]
        public void 팝업_신규_kindTintImages_5개와_기존_필드_3개가_연결된다()
        {
            CardSelectionPopup popup = LoadPrefabAsset().GetComponent<CardSelectionPopup>();
            Assert.IsTrue(popup != null, "CardSelectionPopup 컴포넌트");

            Image[] tint = Required<Image[]>(popup, "_kindTintImages");
            Assert.AreEqual(PopupKindTintNames.Length, tint.Length);
            for (int i = 0; i < tint.Length; ++i)
            {
                Assert.IsTrue(tint[i] != null, $"_kindTintImages[{i}] 비어 있음");
                Assert.AreEqual(PopupKindTintNames[i], tint[i].name, $"_kindTintImages[{i}]");
            }

            CardView[] slots = Required<CardView[]>(popup, "_slots");
            Assert.AreEqual(3, slots.Length);
            foreach (CardView slot in slots)
            {
                Assert.IsTrue(slot != null, "_slots 원소");
            }
            Required<UnityEngine.Object>(popup, "_subtitleText");
            BuildSynergyCell[] chips = Required<BuildSynergyCell[]>(popup, "_synergyChips");
            Assert.AreEqual(4, chips.Length);
            foreach (BuildSynergyCell chip in chips)
            {
                Assert.IsTrue(chip != null, "_synergyChips 원소");
            }
        }

        [Test]
        public void 프리팹_텍스트에_Missing_Script_참조가_없다()
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PopupPath));
            string yaml = File.ReadAllText(full);
            MatchCollection missing = Regex.Matches(yaml, @"m_Script:\s*\{fileID:\s*0\}");
            Assert.AreEqual(0, missing.Count, "프리팹 YAML 의 m_Script fileID 0 (Missing Script)");
        }

        [Test]
        public void 프리팹_로드_시_null_컴포넌트와_Missing_Script가_없다()
        {
            GameObject root = LoadPrefabAsset();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), t.name + " Missing Script 수");
                foreach (Component c in t.GetComponents<Component>())
                {
                    Assert.IsTrue(c != null, t.name + " 에 null 컴포넌트");
                }
            }
        }

        [Test]
        public void 릴레이는_PickButton에_붙고_CardView가_그_컴포넌트를_참조한다()
        {
            foreach (CardView card in PrefabCards())
            {
                CardPointerRelay relay = Child(card, "PickButton").GetComponent<CardPointerRelay>();
                Assert.IsNotNull(relay, card.name + " PickButton 릴레이");
                Assert.AreSame(relay, Required<CardPointerRelay>(card, "_pointerRelay"), card.name);
            }
        }

        [Test]
        public void 스프라이트_연결이_기획서와_일치한다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.AreEqual("CardSelectKindInset", Required<Image>(card, "_kindRing").sprite.name, card.name + " KindRing");
                Assert.AreEqual("CardSelectFrame", Required<Image>(card, "_frameGold").sprite.name, card.name + " DecoFrameGold");
                Assert.AreEqual("CardSelectPickBtnSoul", Required<Sprite>(card, "_passiveButtonSprite").name, card.name);
                Assert.AreEqual("CardSelectPickBtnGold", Required<Sprite>(card, "_activeButtonSprite").name, card.name);
                Assert.AreEqual("SynergyModalHalo", Required<Image>(card, "_axisIconBorder").sprite.name, card.name + " AxisIcon");
                Assert.AreEqual("SynergyModalBadge", Child(card, "CountBadgeBg").GetComponent<Image>().sprite.name, card.name + " CountBadgeBg");
                Assert.AreEqual("WorldMapGradientV", Child(card, "Scrim").GetComponent<Image>().sprite.name, card.name + " Scrim");
                Assert.AreSame(Child(card, "CountBadgeBg"), Child(card, "CountBadgeBg/DecoCountGlow").parent, card.name);
            }
        }

        //# --- ② Scrim 보정 회귀 · Border 비활성 ---

        [Test]
        public void Scrim_3곳_보정값이_유지된다()
        {
            foreach (CardView card in PrefabCards())
            {
                RectTransform scrim = (RectTransform)Child(card, "Scrim");
                Assert.AreEqual(0.5f, scrim.pivot.x, 0.0001f, card.name + " 피벗 x");
                Assert.AreEqual(0.5f, scrim.pivot.y, 0.0001f, card.name + " 피벗 y");
                Assert.AreEqual(0f, scrim.anchoredPosition.x, 0.001f, card.name + " pos x");
                Assert.AreEqual(-210f, scrim.anchoredPosition.y, 0.001f, card.name + " pos y");
                Assert.AreEqual(1f, scrim.localScale.x, 0.0001f, card.name + " scale x");
                Assert.AreEqual(-1f, scrim.localScale.y, 0.0001f, card.name + " scale y");
                Assert.AreEqual(-36f, scrim.sizeDelta.x, 0.001f, card.name + " size x");
                Assert.AreEqual(100f, scrim.sizeDelta.y, 0.001f, card.name + " size y");
            }
        }

        [Test]
        public void Border_3곳이_비활성이다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.IsFalse(Child(card, "Border").gameObject.activeSelf, card.name + " Border");
            }
        }

        //# --- ③ 형제 순서 · 레이캐스트 ---

        [Test]
        public void 카드_자식_형제_순서가_기획서와_일치한다()
        {
            foreach (CardView card in PrefabCards())
            {
                AssertOrder(card.transform, CardOrder, card.name);
            }
        }

        [Test]
        public void 팝업_루트_자식_형제_순서가_기획서와_일치한다()
        {
            AssertOrder(LoadPrefabAsset().transform, PopupOrder, "CardSelectionPopup");
        }

        [Test]
        public void Deco_Image는_raycastTarget이_꺼져_있고_HoverGuard만_예외다()
        {
            int decoCount = 0;
            foreach (Image image in LoadPrefabAsset().GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("Deco") == false)
                    continue;

                ++decoCount;
                if (image.name == "DecoHoverGuard")
                {
                    Assert.IsTrue(image.raycastTarget, image.transform.parent.parent.name + "/DecoHoverGuard raycast");
                    Assert.IsFalse(image.gameObject.activeSelf, "DecoHoverGuard 기본 비활성");
                    Assert.AreEqual("PickButton", image.transform.parent.name, "DecoHoverGuard 부모");
                }
                else
                {
                    Assert.IsFalse(image.raycastTarget, image.name + " raycastTarget");
                }
            }
            Assert.Greater(decoCount, 40, "Deco Image 수집 확인");
        }

        [Test]
        public void PickButton_Image는_raycastTarget이_켜져_있다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.IsTrue(Child(card, "PickButton").GetComponent<Image>().raycastTarget, card.name);
            }
        }

        //# --- ④ 휴지 기준값 · 연출 · 리셋 ---

        [Test]
        public void 프리팹_휴지_기준값이_기획서와_일치한다()
        {
            foreach (CardView card in PrefabCards())
            {
                Assert.AreEqual(0f, Alpha(Required<Image>(card, "_hoverGlow")), 0.001f, card.name + " 글로우");
                Assert.AreEqual(0f, Alpha(Required<Image>(card, "_pickBright")), 0.001f, card.name + " 밝기");
                Assert.AreEqual(0f, Alpha(Required<Image>(card, "_dimOverlay")), 0.001f, card.name + " 어둠");
                AssertRgb(RestFrame, Required<Image>(card, "_frameGold").color, card.name + " 프레임");
                Assert.IsFalse(Required<GameObject>(card, "_hoverGuard").activeSelf, card.name + " 보호 영역");
            }
        }

        [Test]
        public void 호버_눌림_단계별_표시값이_기획서와_일치한다()
        {
            Parts p = MakeSynthetic();

            p.Relay.OnPointerEnter(null);
            Assert.AreEqual(106f, p.Rt.anchoredPosition.y, 0.001f, "호버 리프트");
            Assert.AreEqual(12f, p.Rt.anchoredPosition.x, 0.001f, "x 불변");
            Assert.AreEqual(0.8f, Alpha(p.Glow), 0.001f);
            Assert.AreEqual(0.15f, Alpha(p.Bright), 0.001f);
            AssertRgb(HoverFrame, p.Frame.color, "호버 프레임");
            Assert.AreEqual(1f, p.View.transform.localScale.x, 0.001f);
            Assert.IsTrue(p.Guard.activeSelf);

            p.Relay.OnPointerDown(null);
            Assert.IsTrue(p.View.IsPressed);
            Assert.AreEqual(1.03f, p.View.transform.localScale.x, 0.001f);
            Assert.AreEqual(1f, Alpha(p.Glow), 0.001f);
            Assert.AreEqual(0.3f, Alpha(p.Bright), 0.001f);
            AssertRgb(PressFrame, p.Frame.color, "눌림 프레임");
            Assert.AreEqual(106f, p.Rt.anchoredPosition.y, 0.001f);

            p.Relay.OnPointerUp(null);
            Assert.IsFalse(p.View.IsPressed);
            Assert.AreEqual(0.8f, Alpha(p.Glow), 0.001f, "눌림 해제 후 호버 표시 복귀");
            AssertRgb(HoverFrame, p.Frame.color, "해제 후 프레임");
            Assert.AreEqual(1f, p.View.transform.localScale.x, 0.001f);
            Assert.IsTrue(p.Guard.activeSelf);

            p.Relay.OnPointerExit(null);
            AssertRest(p, 100f, "Exit");
        }

        [Test]
        public void 눌린_채_Exit하면_눌림이_해제되고_휴지로_복귀한다()
        {
            Parts p = MakeSynthetic();
            p.Relay.OnPointerEnter(null);
            p.Relay.OnPointerDown(null);

            p.Relay.OnPointerExit(null);

            Assert.IsFalse(p.View.IsPressed);
            AssertRest(p, 100f, "눌린 채 Exit");
        }

        [Test]
        public void SetDimmed_어두움_알파는_0_5이고_눌림_카드는_무시한다()
        {
            Parts p = MakeSynthetic();

            p.View.SetDimmed(true);
            Assert.AreEqual(0.5f, Alpha(p.Dim), 0.001f);
            Assert.AreEqual(100f, p.Rt.anchoredPosition.y, 0.001f, "어두움은 위치 불변");

            p.Relay.OnPointerDown(null);
            Assert.AreEqual(0f, Alpha(p.Dim), 0.001f, "눌림이면 어두움 무시");

            p.Relay.OnPointerUp(null);
            Assert.AreEqual(0.5f, Alpha(p.Dim), 0.001f, "눌림 해제 후 어두움 복귀");

            p.View.SetDimmed(false);
            Assert.AreEqual(0f, Alpha(p.Dim), 0.001f);
        }

        [Test]
        public void 호버_중_레이아웃이_y를_되돌려도_Update가_리프트를_복구한다()
        {
            Parts p = MakeSynthetic();
            p.Relay.OnPointerEnter(null);
            p.Rt.anchoredPosition = new Vector2(12f, 100f);

            Call(p.View, "Update");

            Assert.AreEqual(106f, p.Rt.anchoredPosition.y, 0.001f);
        }

        [Test]
        public void 이미_휴지_상태면_Update는_위치를_건드리지_않는다()
        {
            Parts p = MakeSynthetic();
            p.Rt.anchoredPosition = new Vector2(33f, 77f);

            Call(p.View, "Update");

            Assert.AreEqual(77f, p.Rt.anchoredPosition.y, 0.001f);
            Assert.AreEqual(33f, p.Rt.anchoredPosition.x, 0.001f);
        }

        [Test]
        public void 풀_재오픈_시_눌림_호버_어두움_상태가_휴지로_리셋된다()
        {
            Parts p = MakeSynthetic();
            p.Relay.OnPointerEnter(null);
            p.Relay.OnPointerDown(null);
            p.View.SetDimmed(true);

            //# SetActive(false)/(true) 에 해당하는 라이프사이클 — EditMode 에선 자동 호출 안 되므로 직접 호출.
            Call(p.View, "OnDisable");
            Call(p.View, "OnEnable");

            Assert.IsFalse(p.View.IsPressed);
            AssertRest(p, 100f, "재오픈");
        }

        [Test]
        public void 재오픈_후에는_릴레이_이벤트가_다시_정상_반응한다()
        {
            Parts p = MakeSynthetic();
            p.Relay.OnPointerEnter(null);
            Call(p.View, "OnDisable");
            Call(p.View, "OnEnable");

            p.Relay.OnPointerEnter(null);

            Assert.AreEqual(0.8f, Alpha(p.Glow), 0.001f);
            Assert.AreEqual(106f, p.Rt.anchoredPosition.y, 0.001f);
        }

        [Test]
        public void OnDisable_후에는_릴레이_이벤트에_반응하지_않는다()
        {
            Parts p = MakeSynthetic();
            Call(p.View, "OnDisable");

            p.Relay.OnPointerEnter(null);
            p.Relay.OnPointerDown(null);

            Assert.IsFalse(p.View.IsPressed);
            Assert.AreEqual(0f, Alpha(p.Glow), 0.001f);
        }

        [Test]
        public void 눌림_중_비활성화되면_PressChanged_false가_통지된다()
        {
            Parts p = MakeSynthetic();
            List<bool> events = new List<bool>();
            p.View.PressChanged += (view, pressed) => events.Add(pressed);

            p.Relay.OnPointerDown(null);
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0]);

            Call(p.View, "OnDisable");

            Assert.AreEqual(2, events.Count, "비활성화 통지 1회");
            Assert.IsFalse(events[1]);
            Assert.IsFalse(p.View.IsPressed);
        }

        [Test]
        public void 눌리지_않은_채_비활성화되면_PressChanged가_통지되지_않는다()
        {
            Parts p = MakeSynthetic();
            int count = 0;
            p.View.PressChanged += (view, pressed) => ++count;

            p.Relay.OnPointerEnter(null);
            Call(p.View, "OnDisable");

            Assert.AreEqual(0, count);
        }

        [Test]
        public void 같은_눌림_상태_중복_입력은_PressChanged를_한_번만_발화한다()
        {
            Parts p = MakeSynthetic();
            int count = 0;
            p.View.PressChanged += (view, pressed) => ++count;

            p.Relay.OnPointerDown(null);
            p.Relay.OnPointerDown(null);
            p.Relay.OnPointerUp(null);
            p.Relay.OnPointerUp(null);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void 트윈_시간이_양수면_상태_전환이_즉시_완료되지_않고_Update로_진행된다()
        {
            Parts p = MakeSynthetic();
            SetField(p.View, "_tweenSeconds", 0.16f);

            p.Relay.OnPointerEnter(null);
            Assert.AreEqual(0f, Alpha(p.Glow), 0.001f, "Update 전에는 휴지 표시");
            Assert.IsTrue(p.Guard.activeSelf, "보호 영역은 상태 변경 즉시 활성");

            Call(p.View, "Update");
            Assert.GreaterOrEqual(Alpha(p.Glow), 0f);
            Assert.LessOrEqual(Alpha(p.Glow), 0.8f + 0.001f);
        }

        //# --- ⑤ 팝업 전파 · Tier ---

        private CardSelectionPopup OpenPopup(int cardCount, bool passive, Func<EBuildAxis, int> counts, SynergyVisualConfig config, EBuildAxis axis)
        {
            GameObject instance = NewInstance();
            CardSelectionPopup popup = instance.GetComponent<CardSelectionPopup>();
            Assert.IsTrue(popup != null, "CardSelectionPopup 인스턴스");

            CardView[] slots = Required<CardView[]>(popup, "_slots");
            foreach (CardView slot in slots)
            {
                SetField(slot, "_tweenSeconds", 0f);
            }

            List<CardData> choices = new List<CardData>();
            for (int i = 0; i < cardCount; ++i)
            {
                choices.Add(MakeCard(axis));
            }

            popup.InitUI(new CardSelectionArg
            {
                Choices = choices,
                IsPassive = passive,
                SynergyVisual = config,
                BuildCountOf = counts,
                OnPicked = card => { },
            });

            foreach (CardView slot in slots)
            {
                Reenable(slot);
            }
            return popup;
        }

        private static CardPointerRelay RelayOf(CardView slot)
        {
            return Required<CardPointerRelay>(slot, "_pointerRelay");
        }

        private static float DimAlphaOf(CardView slot)
        {
            return Alpha(Required<Image>(slot, "_dimOverlay"));
        }

        [Test]
        public void 팝업_한_카드를_누르면_나머지_두_카드가_어두워지고_해제하면_복귀한다()
        {
            CardSelectionPopup popup = OpenPopup(3, true, null, null, EBuildAxis.Tank);
            CardView[] slots = Required<CardView[]>(popup, "_slots");

            RelayOf(slots[1]).OnPointerDown(null);
            Assert.AreEqual(0.5f, DimAlphaOf(slots[0]), 0.001f, "슬롯0 어두움");
            Assert.AreEqual(0f, DimAlphaOf(slots[1]), 0.001f, "눌린 슬롯 어둡지 않음");
            Assert.AreEqual(0.5f, DimAlphaOf(slots[2]), 0.001f, "슬롯2 어두움");

            RelayOf(slots[1]).OnPointerUp(null);
            for (int i = 0; i < slots.Length; ++i)
            {
                Assert.AreEqual(0f, DimAlphaOf(slots[i]), 0.001f, $"슬롯{i} 해제");
            }
        }

        [Test]
        public void 팝업_멀티터치로_두_카드가_눌리면_눌린_카드만_밝고_하나_해제돼도_나머지_눌림은_유지된다()
        {
            CardSelectionPopup popup = OpenPopup(3, false, null, null, EBuildAxis.Dps);
            CardView[] slots = Required<CardView[]>(popup, "_slots");

            RelayOf(slots[0]).OnPointerDown(null);
            RelayOf(slots[1]).OnPointerDown(null);
            Assert.AreEqual(0f, DimAlphaOf(slots[0]), 0.001f);
            Assert.AreEqual(0f, DimAlphaOf(slots[1]), 0.001f);
            Assert.AreEqual(0.5f, DimAlphaOf(slots[2]), 0.001f);

            RelayOf(slots[0]).OnPointerUp(null);
            Assert.AreEqual(0.5f, DimAlphaOf(slots[0]), 0.001f, "해제된 카드는 다시 어두움");
            Assert.AreEqual(0f, DimAlphaOf(slots[1]), 0.001f);
            Assert.AreEqual(0.5f, DimAlphaOf(slots[2]), 0.001f);
        }

        [Test]
        public void 팝업_눌린_카드가_비활성화되면_나머지_어두움이_해제된다()
        {
            CardSelectionPopup popup = OpenPopup(3, true, null, null, EBuildAxis.Tank);
            CardView[] slots = Required<CardView[]>(popup, "_slots");
            RelayOf(slots[0]).OnPointerDown(null);
            Assert.AreEqual(0.5f, DimAlphaOf(slots[1]), 0.001f);

            Call(slots[0], "OnDisable");

            Assert.AreEqual(0f, DimAlphaOf(slots[1]), 0.001f);
            Assert.AreEqual(0f, DimAlphaOf(slots[2]), 0.001f);
        }

        [Test]
        public void 팝업_후보가_1장이면_눌림에도_예외없이_눌린_슬롯은_어둡지_않다()
        {
            CardSelectionPopup popup = OpenPopup(1, true, null, null, EBuildAxis.Tank);
            CardView[] slots = Required<CardView[]>(popup, "_slots");
            Assert.IsTrue(slots[0].gameObject.activeSelf);
            Assert.IsFalse(slots[1].gameObject.activeSelf);
            Assert.IsFalse(slots[2].gameObject.activeSelf);

            Assert.DoesNotThrow(() =>
            {
                RelayOf(slots[0]).OnPointerDown(null);
                RelayOf(slots[0]).OnPointerUp(null);
            });
            Assert.AreEqual(0f, DimAlphaOf(slots[0]), 0.001f);
        }

        private static int LitGemCount(CardView slot, Color axisColor)
        {
            Image[] gems = Required<Image[]>(slot, "_axisGems");
            int lit = 0;
            foreach (Image gem in gems)
            {
                if (Mathf.Abs(gem.color.r - axisColor.r) < 0.01f && Mathf.Abs(gem.color.g - axisColor.g) < 0.01f && Mathf.Abs(gem.color.b - axisColor.b) < 0.01f)
                {
                    ++lit;
                }
                else
                {
                    AssertRgb(GemOff, gem.color, gem.name + " 꺼짐색");
                }
            }
            return lit;
        }

        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(5, 2)]
        [TestCase(7, 3)]
        [TestCase(12, 3)]
        public void 팝업_축_장수에_따라_켜진_Tier_보석_수가_결정된다(int buildCount, int expectedTier)
        {
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm);
            CardSelectionPopup popup = OpenPopup(3, true, axis => buildCount, config, EBuildAxis.Tank);
            CardView[] slots = Required<CardView[]>(popup, "_slots");

            foreach (CardView slot in slots)
            {
                Assert.AreEqual(expectedTier, LitGemCount(slot, BuildSynergyPanel.AxisColor[EBuildAxis.Tank]), "켜진 보석 수");
            }
        }

        [Test]
        public void 팝업_BuildCountOf가_null이면_Tier_0이다()
        {
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm);
            CardSelectionPopup popup = OpenPopup(3, true, null, config, EBuildAxis.Tank);

            foreach (CardView slot in Required<CardView[]>(popup, "_slots"))
            {
                Assert.AreEqual(0, LitGemCount(slot, BuildSynergyPanel.AxisColor[EBuildAxis.Tank]));
            }
        }

        [Test]
        public void 팝업_축_아이콘이_없으면_보석과_후광이_비활성이고_Tier가_켜지지_않는다()
        {
            CardSelectionPopup popup = OpenPopup(3, true, axis => 7, null, EBuildAxis.Tank);

            foreach (CardView slot in Required<CardView[]>(popup, "_slots"))
            {
                foreach (Image gem in Required<Image[]>(slot, "_axisGems"))
                {
                    Assert.IsFalse(gem.gameObject.activeSelf, gem.name);
                }
                foreach (Image glow in Required<Image[]>(slot, "_axisTintImages"))
                {
                    Assert.IsFalse(glow.gameObject.activeSelf, glow.name);
                }
            }
        }

        [Test]
        public void 팝업_종류_틴트는_패시브_액티브_색으로_칠하고_알파를_보존한다()
        {
            CardSelectionPopup popup = OpenPopup(3, true, null, null, EBuildAxis.Tank);
            Image[] tint = Required<Image[]>(popup, "_kindTintImages");
            float[] alphas = new float[tint.Length];
            for (int i = 0; i < tint.Length; ++i)
            {
                alphas[i] = tint[i].color.a;
                AssertRgb(UiDotPalette.CardKind(true), tint[i].color, tint[i].name + " 패시브");
            }

            popup.InitUI(new CardSelectionArg { Choices = new List<CardData> { MakeCard(EBuildAxis.Tank) }, IsPassive = false });

            for (int i = 0; i < tint.Length; ++i)
            {
                AssertRgb(UiDotPalette.CardKind(false), tint[i].color, tint[i].name + " 액티브");
                Assert.AreEqual(alphas[i], tint[i].color.a, 0.001f, tint[i].name + " 알파 보존");
            }
        }

        [Test]
        public void 카드_종류_틴트는_알파를_보존하고_반대_종류_재호출에도_유지된다()
        {
            CardView view = NewInstance().GetComponentsInChildren<CardView>(true)[0];
            Image[] tint = Required<Image[]>(view, "_kindTintImages");
            float[] alphas = new float[tint.Length];
            for (int i = 0; i < tint.Length; ++i)
            {
                alphas[i] = tint[i].color.a;
            }

            view.SetKind(true);
            for (int i = 0; i < tint.Length; ++i)
            {
                AssertRgb(UiDotPalette.CardKind(true), tint[i].color, tint[i].name);
                Assert.AreEqual(alphas[i], tint[i].color.a, 0.001f, tint[i].name + " 알파");
            }
            view.SetKind(false);
            for (int i = 0; i < tint.Length; ++i)
            {
                AssertRgb(UiDotPalette.CardKind(false), tint[i].color, tint[i].name);
                Assert.AreEqual(alphas[i], tint[i].color.a, 0.001f, tint[i].name + " 알파");
            }
        }

        //# --- ⑥ OnPicked 1회 ---

        [Test]
        public void 릴레이_입력을_거친_뒤에도_Button_클릭은_OnPicked를_정확히_1회_발화한다()
        {
            GameObject instance = NewInstance();
            CardView view = instance.GetComponentsInChildren<CardView>(true)[0];
            SetField(view, "_tweenSeconds", 0f);
            Reenable(view);

            int clicks = 0;
            view.Bind(MakeCard(EBuildAxis.Tank), () => ++clicks, 0);

            CardPointerRelay relay = RelayOf(view);
            relay.OnPointerEnter(null);
            relay.OnPointerDown(null);
            relay.OnPointerUp(null);
            Assert.AreEqual(0, clicks, "릴레이는 클릭을 발화하지 않는다");

            Button button = relay.GetComponent<Button>();
            Assert.IsNotNull(button, "PickButton Button");
            button.onClick.Invoke();

            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void 릴레이_이벤트만으로는_클릭_콜백이_호출되지_않는다()
        {
            GameObject instance = NewInstance();
            CardView view = instance.GetComponentsInChildren<CardView>(true)[0];
            Reenable(view);
            int clicks = 0;
            view.Bind(MakeCard(EBuildAxis.Tank), () => ++clicks, 1);

            CardPointerRelay relay = RelayOf(view);
            relay.OnPointerEnter(null);
            relay.OnPointerDown(null);
            relay.OnPointerExit(null);

            Assert.AreEqual(0, clicks);
        }

        //# --- ⑦ 신규 스프라이트 임포트 ---

        private static TextureImporter Importer(string name)
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name + " 임포터");
            return importer;
        }

        private static Texture2D ReadPixels(string name)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SpriteDir + name + ".png"));
            Assert.IsTrue(File.Exists(full), full);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(tex, File.ReadAllBytes(full)), name + " 디코드");
            return tex;
        }

        private static Color32 Px(Texture2D tex, int x, int y)
        {
            return (Color32)tex.GetPixel(x, y);
        }

        [TestCase("CardSelectFrame", 16, 16, 7, 7, 7, 7, FilterMode.Point)]
        [TestCase("CardSelectKindInset", 20, 20, 9, 9, 9, 9, FilterMode.Point)]
        [TestCase("CardSelectArtRing", 12, 12, 3, 3, 3, 3, FilterMode.Point)]
        [TestCase("CardSelectChip", 16, 16, 4, 4, 4, 4, FilterMode.Point)]
        [TestCase("CardSelectPickBtnSoul", 16, 32, 3, 7, 3, 5, FilterMode.Bilinear)]
        [TestCase("CardSelectPickBtnGold", 16, 32, 3, 7, 3, 5, FilterMode.Bilinear)]
        [TestCase("CardSelectFadeLine", 64, 4, 0, 0, 0, 0, FilterMode.Bilinear)]
        public void 신규_스프라이트_임포트_설정이_기획서와_일치한다(string name, int width, int height, int left, int bottom, int right, int top, FilterMode filter)
        {
            TextureImporter importer = Importer(name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name + " 타입");
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, name + " 모드");
            Assert.AreEqual(100f, importer.spritePixelsPerUnit, 0.001f, name + " PPU");
            Assert.IsFalse(importer.mipmapEnabled, name + " 밉맵");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, name + " 압축");
            Assert.IsTrue(importer.sRGBTexture, name + " sRGB");
            Assert.IsTrue(importer.alphaIsTransparency, name + " Alpha Is Transparency");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, name + " Wrap");
            Assert.AreEqual(filter, importer.filterMode, name + " Filter");
            Assert.AreEqual(new Vector4(left, bottom, right, top), importer.spriteBorder, name + " border(좌,하,우,상)");

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, name + " Mesh Type");

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            Assert.IsNotNull(sprite, name + " 스프라이트 로드");
            Assert.AreEqual(width, (int)sprite.rect.width, name + " 폭");
            Assert.AreEqual(height, (int)sprite.rect.height, name + " 높이");
            Assert.AreEqual(0.5f, sprite.pivot.x / sprite.rect.width, 0.001f, name + " 피벗 x");
            Assert.AreEqual(0.5f, sprite.pivot.y / sprite.rect.height, 0.001f, name + " 피벗 y");
        }

        [Test]
        public void KindInset_링_픽셀_알파는_166이고_바깥과_안쪽은_투명하다()
        {
            Texture2D tex = ReadPixels("CardSelectKindInset");
            _spawned.Add(tex);

            for (int y = 9; y < 11; ++y)
            {
                Assert.AreEqual(0, Px(tex, 0, y).a, $"바깥 투명 (0,{y})");
                Assert.AreEqual(0, Px(tex, 6, y).a, $"바깥 투명 (6,{y})");
                Assert.AreEqual(166, Px(tex, 7, y).a, $"링 (7,{y})");
                Assert.AreEqual(166, Px(tex, 8, y).a, $"링 (8,{y})");
                Assert.AreEqual(0, Px(tex, 9, y).a, $"안쪽 투명 (9,{y})");
                Assert.AreEqual(255, Px(tex, 8, y).r, $"링 흰색 (8,{y})");
            }
        }

        [Test]
        public void Frame_픽셀은_바깥_먹색_금테_안쪽_먹색_중앙_투명_구조다()
        {
            Texture2D tex = ReadPixels("CardSelectFrame");
            _spawned.Add(tex);
            int y = 8;

            for (int x = 0; x < 3; ++x)
            {
                Color32 c = Px(tex, x, y);
                Assert.AreEqual(255, c.a, $"바깥 불투명 x={x}");
                Assert.AreEqual(0x07, c.r, $"바깥 먹색 x={x}");
            }
            for (int x = 3; x < 5; ++x)
            {
                Color32 c = Px(tex, x, y);
                Assert.AreEqual(255, c.a, $"금테 불투명 x={x}");
                Assert.AreEqual(255, c.r, $"금테 흰색 x={x}");
                Assert.AreEqual(255, c.g, $"금테 흰색 x={x}");
            }
            for (int x = 5; x < 7; ++x)
            {
                Assert.AreEqual(255, Px(tex, x, y).a, $"안쪽 먹색 불투명 x={x}");
                Assert.AreEqual(0x07, Px(tex, x, y).r, $"안쪽 먹색 x={x}");
            }
            Assert.AreEqual(0, Px(tex, 8, y).a, "중앙 투명");
        }

        [Test]
        public void FadeLine_알파는_양끝이_0에_가깝고_가운데가_255다()
        {
            Texture2D tex = ReadPixels("CardSelectFadeLine");
            _spawned.Add(tex);

            Assert.AreEqual(6, Px(tex, 0, 0).a, "열 0");
            Assert.AreEqual(6, Px(tex, 63, 3).a, "열 63");
            Assert.AreEqual(255, Px(tex, 31, 1).a, "열 31");
            Assert.AreEqual(255, Px(tex, 32, 2).a, "열 32");
        }

        //# --- ⑧ 일회용 생성 스크립트 없음 ---

        [Test]
        public void 일회용_스프라이트_생성_스크립트가_레포에_없다()
        {
            string editorDir = Path.GetFullPath(Path.Combine(Application.dataPath, "_Lair", "Editor"));
            Assert.IsFalse(File.Exists(Path.Combine(editorDir, "CardSelectPolishSpriteGenerator.cs")), "CardSelectPolishSpriteGenerator.cs 잔존");

            string[] leftovers = Directory.GetFiles(editorDir, "CardSelect*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, leftovers.Length, "CardSelect* 에디터 스크립트 잔존: " + string.Join(", ", leftovers));
        }

        //# --- ⑨ 필드 비어 있어도 예외 없음 ---

        [Test]
        public void 신규_필드를_모두_비운_CardView에서_전_공개_경로가_예외없이_동작한다()
        {
            GameObject go = new GameObject("EmptyCard", typeof(RectTransform));
            _spawned.Add(go);
            CardView view = go.AddComponent<CardView>();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);

            Assert.DoesNotThrow(() =>
            {
                Call(view, "OnEnable");
                view.SetKind(true);
                view.SetKind(false);
                view.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);
                view.SetAxisTier(0);
                view.SetAxisTier(3);
                view.SetDimmed(true);
                view.SetDimmed(false);
                Call(view, "Update");
                Call(view, "OnDisable");
            });
            Assert.IsFalse(view.IsPressed);
        }

        [Test]
        public void 배열_길이가_3이_아니거나_원소가_null이어도_SetAxisTier와_틴트가_예외없다()
        {
            Parts p = MakeSynthetic();
            Image gem = NewGo("Gem", p.View.transform).AddComponent<Image>();
            SetField(p.View, "_axisGems", new Image[] { gem, null });
            SetField(p.View, "_kindTintImages", new Image[] { null, gem });
            SetField(p.View, "_axisIconRoot", NewGo("AxisRoot", p.View.transform));
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);

            Assert.DoesNotThrow(() =>
            {
                p.View.SetKind(true);
                p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);
                p.View.SetAxisTier(5);
                p.View.SetAxisTier(-1);
            });
            AssertRgb(GemOff, gem.color, "음수 Tier 는 모두 꺼짐");
        }

        [Test]
        public void 신규_필드를_모두_비운_팝업의_InitUI가_예외없다()
        {
            GameObject go = new GameObject("EmptyPopup", typeof(RectTransform));
            _spawned.Add(go);
            CardSelectionPopup popup = go.AddComponent<CardSelectionPopup>();
            SetField(popup, "_slots", new CardView[3]);
            SetField(popup, "_synergyChips", new BuildSynergyCell[4]);

            Assert.DoesNotThrow(() => popup.InitUI(new CardSelectionArg { Choices = new List<CardData>(), IsPassive = true }));
        }
    }
}
