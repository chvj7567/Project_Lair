using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    //# 빌드 상세 모달 비주얼 폴리시 스위트(build-modal-visual-polish §8.2) — 프리팹 연결·칩 텍스트·종류색 틴트·×N 배지·레이캐스트·스프라이트 임포트.
    //# BuildModalCardCellTests 의 합성 셀 기본형(rgb 칠하기·배지 토글)과 달리 여기서는 실제 프리팹 인스턴스로 검증한다.
    public class BuildModalVisualPolishTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string CellPath = UiDir + "BuildModalCardCell.prefab";
        private const string PopupPath = UiDir + "BuildModalPopup.prefab";
        private const string GeneratorPath = "Assets/_Lair/Editor/BuildModalSpriteGenerator.cs";

        private static readonly string[] KindTintedNames = { "DecoKindStrip", "DecoFrameGlow", "DecoFrameBloom", "DecoFrameGem" };
        private static readonly float[] KindTintedAlphas = { 1.0f, 0.65f, 0.22f, 1.0f };

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

        //# 상속 private 필드(CHPoolingScrollView._origin 등)까지 찾도록 계층을 거슬러 올라간다.
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

        //# 리뷰 권고 — 리플렉션 값이 null 이면 NRE 대신 Assert.NotNull 로 먼저 실패시킨다.
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

        private static GameObject LoadPrefabAsset(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(root, path);
            return root;
        }

        private GameObject NewInstance(string path)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(LoadPrefabAsset(path)) as GameObject;
            Assert.IsNotNull(instance, path + " 인스턴스");
            _spawned.Add(instance);
            return instance;
        }

        private BuildModalCardCell NewCellInstance()
        {
            BuildModalCardCell cell = NewInstance(CellPath).GetComponent<BuildModalCardCell>();
            Assert.IsTrue(cell != null, "BuildModalCardCell 컴포넌트");
            return cell;
        }

        private BuildModalPopup NewPopupInstance()
        {
            BuildModalPopup popup = NewInstance(PopupPath).GetComponent<BuildModalPopup>();
            Assert.IsTrue(popup != null, "BuildModalPopup 컴포넌트");
            return popup;
        }

        private static Transform FindChild(GameObject root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            Assert.Fail($"{root.name} 하위에 {name} 없음");
            return null;
        }

        private static Transform FindPath(GameObject root, string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsTrue(t != null, $"{root.name}/{path} 없음");
            return t;
        }

        private CardData MakeCard()
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            _spawned.Add(card);
            return card;
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.005f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.005f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.005f, message + " b");
        }

        private static Image[] KindTinted(BuildModalCardCell cell)
        {
            return Required<Image[]>(cell, "_kindTinted");
        }

        private static TextureImporter Importer(string name)
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name + " 임포터");
            return importer;
        }

        private static SpriteMeshType ReadMeshType(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType;
        }

        //# --- ① 팝업 프리팹 연결 ---

        [Test]
        public void 팝업_프리팹_기존_필드_8개가_연결돼_있다()
        {
            BuildModalPopup popup = LoadPrefabAsset(PopupPath).GetComponent<BuildModalPopup>();
            string[] names =
            {
                "_dimButton", "_closeButton", "_passiveScrollView", "_activeScrollView",
                "_passiveEmptyText", "_activeEmptyText", "_passiveCountText", "_activeCountText",
            };
            foreach (string name in names)
            {
                Required<UnityEngine.Object>(popup, name);
            }
        }

        [Test]
        public void 팝업_프리팹_두_스크롤뷰의_origin은_서로_다른_셀_인스턴스다()
        {
            BuildModalPopup popup = LoadPrefabAsset(PopupPath).GetComponent<BuildModalPopup>();
            GameObject passiveOrigin = Required<GameObject>(Field(popup, "_passiveScrollView"), "_origin");
            GameObject activeOrigin = Required<GameObject>(Field(popup, "_activeScrollView"), "_origin");

            Assert.AreNotSame(passiveOrigin, activeOrigin, "두 섹션은 각자 origin 인스턴스를 가진다");
            Assert.IsTrue(passiveOrigin.GetComponent<BuildModalCardCell>() != null, "패시브 origin 은 셀");
            Assert.IsTrue(activeOrigin.GetComponent<BuildModalCardCell>() != null, "액티브 origin 은 셀");
        }

        [Test]
        public void 팝업_프리팹_신규_칩_필드가_경로의_ChipText_CHText에_연결돼_있다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            BuildModalPopup popup = root.GetComponent<BuildModalPopup>();

            CHText passive = Required<CHText>(popup, "_passiveCountText");
            CHText active = Required<CHText>(popup, "_activeCountText");

            Assert.AreSame(FindPath(root, "ModalBody/PassiveSection/PassiveCountChip/ChipText").gameObject, passive.gameObject);
            Assert.AreSame(FindPath(root, "ModalBody/ActiveSection/ActiveCountChip/ChipText").gameObject, active.gameObject);
        }

        [Test]
        public void 팝업_프리팹_칩_CHText의_stringID는_마이너스1이다()
        {
            BuildModalPopup popup = LoadPrefabAsset(PopupPath).GetComponent<BuildModalPopup>();

            Assert.AreEqual(-1, (int)Field(Required<CHText>(popup, "_passiveCountText"), "_stringID"), "패시브 칩");
            Assert.AreEqual(-1, (int)Field(Required<CHText>(popup, "_activeCountText"), "_stringID"), "액티브 칩");
        }

        //# --- ① 셀 프리팹 연결 ---

        [Test]
        public void 셀_프리팹_기존_필드_6개가_연결돼_있다()
        {
            BuildModalCardCell cell = LoadPrefabAsset(CellPath).GetComponent<BuildModalCardCell>();
            string[] names = { "_frame", "_icon", "_nameText", "_countText", "_descText", "_kindRing" };
            foreach (string name in names)
            {
                Required<UnityEngine.Object>(cell, name);
            }
        }

        [Test]
        public void 셀_프리팹_kindTinted는_길이4고_이름이_기획서_순서다()
        {
            BuildModalCardCell cell = LoadPrefabAsset(CellPath).GetComponent<BuildModalCardCell>();
            Image[] tinted = KindTinted(cell);

            Assert.AreEqual(4, tinted.Length);
            for (int i = 0; i < tinted.Length; ++i)
            {
                Assert.IsTrue(tinted[i] != null, $"_kindTinted[{i}] 비어 있음");
                Assert.AreEqual(KindTintedNames[i], tinted[i].name, $"_kindTinted[{i}] 이름");
            }
        }

        [Test]
        public void 셀_프리팹_countBadgeDecor는_DecoCountBadge다()
        {
            BuildModalCardCell cell = LoadPrefabAsset(CellPath).GetComponent<BuildModalCardCell>();
            GameObject badge = Required<GameObject>(cell, "_countBadgeDecor");

            Assert.AreEqual("DecoCountBadge", badge.name);
        }

        [Test]
        public void 셀_프리팹_kindTinted_알파_사전값이_기획서와_같다()
        {
            BuildModalCardCell cell = LoadPrefabAsset(CellPath).GetComponent<BuildModalCardCell>();
            Image[] tinted = KindTinted(cell);

            Assert.AreEqual(KindTintedAlphas.Length, tinted.Length);
            for (int i = 0; i < tinted.Length; ++i)
            {
                Assert.AreEqual(KindTintedAlphas[i], tinted[i].color.a, 0.005f, $"{KindTintedNames[i]} 프리팹 알파");
            }
        }

        [Test]
        public void 셀_프리팹_형제_순서가_기획서_2_3의_핵심_순서를_지킨다()
        {
            GameObject root = LoadPrefabAsset(CellPath);
            List<string> order = new List<string>();
            for (int i = 0; i < root.transform.childCount; ++i)
            {
                order.Add(root.transform.GetChild(i).name);
            }

            Assert.Less(order.IndexOf("DecoDescInset"), order.IndexOf("DescText"), "DecoDescInset < DescText");
            Assert.Less(order.IndexOf("DecoCountBadge"), order.IndexOf("CountText"), "DecoCountBadge < CountText");
            Assert.GreaterOrEqual(order.IndexOf("DecoDescInset"), 0);
            Assert.GreaterOrEqual(order.IndexOf("DecoCountBadge"), 0);
        }

        //# --- ② 칩 텍스트 ---

        private BattleViewModel NewViewModel(int passiveMax, int activeMax)
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            vm.BindTriggerThresholds(new float[activeMax], new float[passiveMax]);
            return vm;
        }

        //# 칩 갱신만 보면 되므로 스크롤뷰는 비운다(EditMode 에선 풀링 스크롤 구동 불가) — 칩 텍스트는 프리팹 값 그대로.
        private BuildModalPopup NewPopupForChips(BattleViewModel vm)
        {
            BuildModalPopup popup = NewPopupInstance();
            SetField(popup, "_passiveScrollView", null);
            SetField(popup, "_activeScrollView", null);
            SetField(popup, "_vm", vm);
            return popup;
        }

        private static void RefreshBuild(BuildModalPopup popup)
        {
            MethodInfo handle = typeof(BuildModalPopup).GetMethod("HandleBuildChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handle, "HandleBuildChanged");
            handle.Invoke(popup, null);
        }

        private static string ChipOf(BuildModalPopup popup, string field)
        {
            return Required<CHText>(popup, field).GetString();
        }

        [Test]
        public void 칩_텍스트_픽_0장은_0_9와_0_5다()
        {
            BattleViewModel vm = NewViewModel(9, 5);
            BuildModalPopup popup = NewPopupForChips(vm);

            RefreshBuild(popup);

            Assert.AreEqual("0/9", ChipOf(popup, "_passiveCountText"));
            Assert.AreEqual("0/5", ChipOf(popup, "_activeCountText"));
        }

        [Test]
        public void 칩_텍스트_픽_추가_후_재호출하면_VM_프로퍼티_값으로_갱신된다()
        {
            BattleViewModel vm = NewViewModel(9, 5);
            BuildModalPopup popup = NewPopupForChips(vm);
            CardData passiveCard = MakeCard();
            CardData activeCard = MakeCard();
            RefreshBuild(popup);

            for (int i = 0; i < 5; ++i)
            {
                vm.AddPick(passiveCard, true);
            }
            for (int i = 0; i < 3; ++i)
            {
                vm.AddPick(activeCard, false);
            }
            RefreshBuild(popup);

            Assert.AreEqual($"{vm.PassivePickCount}/{vm.PassiveTriggerTotal}", ChipOf(popup, "_passiveCountText"));
            Assert.AreEqual($"{vm.ActivePickCount}/{vm.ActiveTriggerTotal}", ChipOf(popup, "_activeCountText"));
            Assert.AreEqual("5/9", ChipOf(popup, "_passiveCountText"));
            Assert.AreEqual("3/5", ChipOf(popup, "_activeCountText"));
        }

        [Test]
        public void 칩_텍스트_가득_찬_상태는_9_9와_5_5다()
        {
            BattleViewModel vm = NewViewModel(9, 5);
            BuildModalPopup popup = NewPopupForChips(vm);
            CardData passiveCard = MakeCard();
            CardData activeCard = MakeCard();
            for (int i = 0; i < 9; ++i)
            {
                vm.AddPick(passiveCard, true);
            }
            for (int i = 0; i < 5; ++i)
            {
                vm.AddPick(activeCard, false);
            }

            RefreshBuild(popup);

            Assert.AreEqual("9/9", ChipOf(popup, "_passiveCountText"));
            Assert.AreEqual("5/5", ChipOf(popup, "_activeCountText"));
        }

        [Test]
        public void 칩_텍스트_최대값은_VM_트리거_배열_길이를_따른다()
        {
            BattleViewModel vm = NewViewModel(4, 2);
            BuildModalPopup popup = NewPopupForChips(vm);

            RefreshBuild(popup);

            Assert.AreEqual("0/4", ChipOf(popup, "_passiveCountText"));
            Assert.AreEqual("0/2", ChipOf(popup, "_activeCountText"));
        }

        //# --- ③ 종류색 틴트 ---

        [Test]
        public void Bind_패시브는_kindTinted_4개를_패시브_종류색으로_칠하고_알파는_사전값이다()
        {
            BuildModalCardCell cell = NewCellInstance();
            Image[] tinted = KindTinted(cell);

            cell.Bind(MakeCard(), 1, true);

            for (int i = 0; i < tinted.Length; ++i)
            {
                AssertRgb(UiDotPalette.CardKind(true), tinted[i].color, KindTintedNames[i]);
                Assert.AreEqual(KindTintedAlphas[i], tinted[i].color.a, 0.005f, KindTintedNames[i] + " 알파");
            }
        }

        [Test]
        public void Bind_액티브로_재_Bind하면_액티브_종류색으로_바뀌고_알파가_유지된다()
        {
            BuildModalCardCell cell = NewCellInstance();
            Image[] tinted = KindTinted(cell);
            CardData card = MakeCard();

            cell.Bind(card, 1, true);
            cell.Bind(card, 1, false);

            for (int i = 0; i < tinted.Length; ++i)
            {
                AssertRgb(UiDotPalette.CardKind(false), tinted[i].color, KindTintedNames[i]);
                Assert.AreEqual(KindTintedAlphas[i], tinted[i].color.a, 0.005f, KindTintedNames[i] + " 알파");
            }
        }

        [Test]
        public void Bind_패시브_액티브를_번갈아_재_Bind해도_이전_종류색_잔상이_없다()
        {
            BuildModalCardCell cell = NewCellInstance();
            Image[] tinted = KindTinted(cell);
            Image ring = Required<Image>(cell, "_kindRing");
            CardData card = MakeCard();

            for (int round = 0; round < 3; ++round)
            {
                bool isPassive = round % 2 == 0;
                cell.Bind(card, 1, isPassive);
                AssertRgb(UiDotPalette.CardKind(isPassive), ring.color, $"라운드{round} KindRing");
                for (int i = 0; i < tinted.Length; ++i)
                {
                    AssertRgb(UiDotPalette.CardKind(isPassive), tinted[i].color, $"라운드{round} {KindTintedNames[i]}");
                    Assert.AreEqual(KindTintedAlphas[i], tinted[i].color.a, 0.005f, $"라운드{round} {KindTintedNames[i]} 알파");
                }
            }
        }

        //# --- ③ ×N 배지 ---

        [Test]
        public void Bind_배지는_count_1_2_1_순서로_꺼짐_켜짐_꺼짐이다()
        {
            BuildModalCardCell cell = NewCellInstance();
            GameObject badge = Required<GameObject>(cell, "_countBadgeDecor");
            CHText countText = Required<CHText>(cell, "_countText");
            CardData card = MakeCard();

            cell.Bind(card, 1, true);
            Assert.IsFalse(badge.activeSelf, "count 1");
            Assert.IsFalse(countText.gameObject.activeSelf, "count 1 글자");

            cell.Bind(card, 2, true);
            Assert.IsTrue(badge.activeSelf, "count 2");
            Assert.IsTrue(countText.gameObject.activeSelf, "count 2 글자");
            Assert.AreEqual("×2", countText.GetString());

            cell.Bind(card, 1, true);
            Assert.IsFalse(badge.activeSelf, "풀 재사용 count 1");
            Assert.IsFalse(countText.gameObject.activeSelf, "풀 재사용 글자");
        }

        [Test]
        public void Bind_card_null이면_배지_상태가_바뀌지_않는다()
        {
            BuildModalCardCell cell = NewCellInstance();
            GameObject badge = Required<GameObject>(cell, "_countBadgeDecor");
            CardData card = MakeCard();
            cell.Bind(card, 3, true);

            Assert.DoesNotThrow(() => cell.Bind((CardData)null, 1, true));

            Assert.IsTrue(badge.activeSelf, "직전 상태 유지");
        }

        //# --- ④ 레이캐스트 ---

        private static bool HasDecoAncestorOrSelf(Transform t, Transform stop)
        {
            for (Transform cur = t; cur != null && cur != stop.parent; cur = cur.parent)
            {
                if (cur.name.StartsWith("Deco"))
                    return true;
            }
            return false;
        }

        private static void AssertDecoGraphicsRaycastOff(GameObject root, string label)
        {
            int count = 0;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (HasDecoAncestorOrSelf(graphic.transform, root.transform) == false)
                    continue;
                ++count;
                Assert.IsFalse(graphic.raycastTarget, $"{label}/{graphic.name} raycastTarget");
            }
            Assert.Greater(count, 0, label + " Deco Graphic 수");
        }

        [Test]
        public void 셀_프리팹의_Deco_Graphic은_모두_raycastTarget이_꺼져_있다()
        {
            AssertDecoGraphicsRaycastOff(LoadPrefabAsset(CellPath), "BuildModalCardCell");
        }

        [Test]
        public void 팝업_프리팹의_Deco_Graphic은_모두_raycastTarget이_꺼져_있다()
        {
            AssertDecoGraphicsRaycastOff(LoadPrefabAsset(PopupPath), "BuildModalPopup");
        }

        [Test]
        public void 팝업_프리팹의_칩_글자는_raycastTarget이_꺼져_있다()
        {
            BuildModalPopup popup = LoadPrefabAsset(PopupPath).GetComponent<BuildModalPopup>();

            Assert.IsFalse(Required<CHText>(popup, "_passiveCountText").GetComponent<Graphic>().raycastTarget, "패시브 칩 글자");
            Assert.IsFalse(Required<CHText>(popup, "_activeCountText").GetComponent<Graphic>().raycastTarget, "액티브 칩 글자");
        }

        [TestCase("Dim")]
        [TestCase("CloseButton")]
        [TestCase("ModalBody")]
        [TestCase("Scrollbar")]
        [TestCase("Handle")]
        [TestCase("Viewport")]
        public void 팝업_프리팹의_클릭_대상은_raycastTarget이_켜져_있다(string name)
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            int found = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name)
                    continue;
                Graphic graphic = t.GetComponent<Graphic>();
                Assert.IsTrue(graphic != null, $"{name} Graphic");
                Assert.IsTrue(graphic.raycastTarget, $"{name} raycastTarget");
                ++found;
            }
            Assert.Greater(found, 0, name + " 존재");
        }

        //# --- ⑤ 신규 스프라이트 임포트 (§3.3) ---

        [TestCase("BuildModalChipFrame", 8, 8)]
        [TestCase("BuildModalScrollThumb", 4, 4)]
        [TestCase("BuildModalCloseGlyph", 12, 12)]
        public void 신규_스프라이트가_존재하고_크기와_공통_임포트_설정이_기획서와_같다(string name, int width, int height)
        {
            Assert.IsTrue(File.Exists(SpriteDir + name + ".png"), $"{name}.png 없음");
            TextureImporter importer = Importer(name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, "textureType");
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, "spriteMode");
            Assert.AreEqual(100f, importer.spritePixelsPerUnit, "PPU");
            Assert.AreEqual(SpriteMeshType.FullRect, ReadMeshType(importer), "meshType");
            Assert.IsFalse(importer.mipmapEnabled, "mipmap");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "compression");
            Assert.IsTrue(importer.sRGBTexture, "sRGB");
            Assert.IsTrue(importer.alphaIsTransparency, "alphaIsTransparency");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, "wrap");

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            Assert.IsNotNull(sprite, name + " Sprite 로드");
            Assert.AreEqual(width, (int)sprite.rect.width, "폭");
            Assert.AreEqual(height, (int)sprite.rect.height, "높이");
        }

        //# border 는 Unity 순서(좌,하,우,상).
        [TestCase("BuildModalChipFrame", 3, 3, 3, 3)]
        [TestCase("BuildModalScrollThumb", 0, 0, 0, 0)]
        [TestCase("BuildModalCloseGlyph", 0, 0, 0, 0)]
        public void 신규_스프라이트_border가_기획서_표와_같다(string name, int left, int bottom, int right, int top)
        {
            Assert.AreEqual(new Vector4(left, bottom, right, top), Importer(name).spriteBorder, name);
        }

        [TestCase("BuildModalChipFrame", FilterMode.Point)]
        [TestCase("BuildModalScrollThumb", FilterMode.Bilinear)]
        [TestCase("BuildModalCloseGlyph", FilterMode.Bilinear)]
        public void 신규_스프라이트_필터_모드가_기획서_표와_같다(string name, FilterMode expected)
        {
            Assert.AreEqual(expected, Importer(name).filterMode, name);
        }

        //# --- ⑥ 일회용 생성기 삭제 (Rule 04 §3) ---

        [Test]
        public void 일회용_스프라이트_생성기_스크립트가_레포에_없다()
        {
            Assert.IsFalse(File.Exists(GeneratorPath), GeneratorPath);
            Assert.IsFalse(File.Exists(GeneratorPath + ".meta"), GeneratorPath + ".meta");
            foreach (string file in Directory.GetFiles("Assets/_Lair/Editor", "*.cs", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("BuildModalSprite", Path.GetFileName(file), file);
            }
        }

        //# --- ⑦ 빈 필드 안전 ---

        [Test]
        public void 신규_필드가_비어_있는_팝업도_Build에서_예외가_없다()
        {
            GameObject go = new GameObject("EmptyPopup", typeof(RectTransform));
            _spawned.Add(go);
            BuildModalPopup popup = go.AddComponent<BuildModalPopup>();
            SetField(popup, "_vm", NewViewModel(9, 5));

            Assert.DoesNotThrow(() => RefreshBuild(popup));
        }

        [Test]
        public void 신규_필드가_비어_있는_셀도_Bind에서_예외가_없다()
        {
            GameObject go = new GameObject("EmptyCell", typeof(RectTransform));
            _spawned.Add(go);
            BuildModalCardCell cell = go.AddComponent<BuildModalCardCell>();
            CardData card = MakeCard();

            Assert.DoesNotThrow(() => cell.Bind(card, 1, true));
            Assert.DoesNotThrow(() => cell.Bind(card, 2, false));
        }

        [Test]
        public void kindTinted_원소가_null이거나_빈_배열이어도_Bind에서_예외가_없다()
        {
            GameObject go = new GameObject("PartialCell", typeof(RectTransform));
            _spawned.Add(go);
            BuildModalCardCell cell = go.AddComponent<BuildModalCardCell>();
            CardData card = MakeCard();

            SetField(cell, "_kindTinted", new Image[] { null, null });
            Assert.DoesNotThrow(() => cell.Bind(card, 1, true));

            SetField(cell, "_kindTinted", new Image[0]);
            Assert.DoesNotThrow(() => cell.Bind(card, 1, true));
        }
    }
}
