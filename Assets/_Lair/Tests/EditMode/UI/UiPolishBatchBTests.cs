using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 비주얼 폴리시 묶음 B(ShopPopup · CodexPopup · RecordsPopup + 셀 3종) 스위트(ui-polish-batch-b §9.2) —
    //# Missing Script · 신규 필드 연결 · 상점 셀 상태 틴트 · origin 보존 · 레이캐스트 · 기록 셀 폭 · 도감 탭 링 · 일회용 빌더 부재.
    public class UiPolishBatchBTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string ShopPopupPath = UiDir + "ShopPopup.prefab";
        private const string ShopCellPath = UiDir + "ShopItemCell.prefab";
        private const string CodexPopupPath = UiDir + "CodexPopup.prefab";
        private const string CodexCellPath = UiDir + "CodexCell.prefab";
        private const string RecordsPopupPath = UiDir + "RecordsPopup.prefab";
        private const string RecordsCellPath = UiDir + "RecordsStageCell.prefab";

        private static readonly float[] StateTintAlphas = { 0.40f, 1.0f };

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

        //# 리플렉션 값이 null 이면 NRE 대신 Assert 로 먼저 실패시킨다.
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
            Assert.IsNotNull(instance, "프리팹 인스턴스 " + path);
            _spawned.Add(instance);
            return instance;
        }

        private static T Comp<T>(GameObject root) where T : Component
        {
            T comp = root.GetComponentInChildren<T>(true);
            Assert.IsTrue(comp != null, $"{root.name} 에 {typeof(T).Name} 없음");
            return comp;
        }

        private ShopItemCell NewShopCell()
        {
            return Comp<ShopItemCell>(NewInstance(ShopCellPath));
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.01f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.01f, message + " b");
        }

        private static string FullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static ShopItemCellData ItemData(bool canBuy, bool isMax)
        {
            return new ShopItemCellData
            {
                RowKind = ShopPopup.ShopRowKind.Item,
                Id = "test_item",
                DisplayName = "테스트 항목",
                Description = "설명",
                LevelText = "Lv 1/5",
                Price = 100,
                IsMax = isMax,
                CanBuy = canBuy,
                Level = isMax ? 5 : 1,
                MaxLevel = 5,
            };
        }

        private static ShopItemCellData HeaderData()
        {
            return new ShopItemCellData
            {
                RowKind = ShopPopup.ShopRowKind.SectionHeader,
                HeaderText = "스탯 강화",
            };
        }

        //# 팝업 프리팹의 CHPoolingScrollView 필드 → _origin(GameObject) 조회.
        private static GameObject OriginOf(Component popup, string scrollViewField)
        {
            Component scrollView = Required<Component>(popup, scrollViewField);
            return Required<GameObject>(scrollView, "_origin");
        }

        private static string SourcePrefabPath(GameObject originInstance)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(originInstance);
            Assert.IsTrue(source != null, originInstance.name + " 은 프리팹 인스턴스가 아님");
            return AssetDatabase.GetAssetPath(source);
        }

        //# --- ① Missing Script ---

        [TestCase(ShopPopupPath)]
        [TestCase(ShopCellPath)]
        [TestCase(CodexPopupPath)]
        [TestCase(CodexCellPath)]
        [TestCase(RecordsPopupPath)]
        [TestCase(RecordsCellPath)]
        public void 프리팹_YAML에_Missing_Script_참조가_없다(string path)
        {
            string full = FullPath(path);
            Assert.IsTrue(File.Exists(full), full);
            MatchCollection missing = Regex.Matches(File.ReadAllText(full), @"m_Script:\s*\{fileID:\s*0\}");
            Assert.AreEqual(0, missing.Count, path + " 의 m_Script fileID 0 (Missing Script)");
        }

        [TestCase(ShopPopupPath)]
        [TestCase(ShopCellPath)]
        [TestCase(CodexPopupPath)]
        [TestCase(CodexCellPath)]
        [TestCase(RecordsPopupPath)]
        [TestCase(RecordsCellPath)]
        public void 프리팹_모든_오브젝트에_Missing_Script와_null_컴포넌트가_없다(string path)
        {
            foreach (Transform t in LoadPrefabAsset(path).GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), $"{path} {t.name} Missing Script 수");
                foreach (Component c in t.GetComponents<Component>())
                {
                    Assert.IsTrue(c != null, $"{path} {t.name} 에 null 컴포넌트");
                }
            }
        }

        //# --- ② ShopItemCell 신규 필드 연결 · 기존 필드 유지 ---

        [Test]
        public void ShopItemCell_itemDecor가_DecoItemGroup으로_연결된다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));

            GameObject decor = Required<GameObject>(cell, "_itemDecor");
            Assert.AreEqual("DecoItemGroup", decor.name);
        }

        [Test]
        public void ShopItemCell_stateTinted가_길이_2와_Glow_Strip_순서_프리팹_알파로_연결된다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));

            Image[] tinted = Required<Image[]>(cell, "_stateTinted");
            Assert.AreEqual(2, tinted.Length, "_stateTinted 길이");
            string[] names = { "DecoStateGlow", "DecoStateStrip" };
            for (int i = 0; i < tinted.Length; ++i)
            {
                Assert.IsTrue(tinted[i] != null, $"_stateTinted[{i}] 비어 있음");
                Assert.AreEqual(names[i], tinted[i].name, $"_stateTinted[{i}]");
                Assert.AreEqual(StateTintAlphas[i], tinted[i].color.a, 0.01f, tinted[i].name + " 프리팹 알파");
            }
        }

        [Test]
        public void ShopItemCell_stateTinted_두_장식은_StateRing의_자식이다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));
            Image ring = Required<Image>(cell, "_stateRing");
            Image[] tinted = Required<Image[]>(cell, "_stateTinted");

            foreach (Image image in tinted)
            {
                Assert.IsTrue(image != null, "_stateTinted 원소 비어 있음");
                Assert.IsTrue(image.transform.IsChildOf(ring.transform), image.name + " 은 StateRing 하위여야 한다");
                Assert.AreNotSame(ring.transform, image.transform, image.name);
            }
        }

        [Test]
        public void ShopItemCell_기존_위젯_필드가_유지된다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));
            string[] names =
            {
                "_nameText", "_levelText", "_descText", "_priceText", "_buyButton", "_buyLabel",
                "_iconImage", "_glowFrame", "_glowHintRing", "_headerText", "_headerBg", "_headerAccent",
                "_stateRing", "_iconSlot", "_buyButtonImage", "_buySoulSprite", "_buyOffSprite", "_buyGoldSprite",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(cell, name);
            }
            Image[] pips = Required<Image[]>(cell, "_levelPips");
            Assert.AreEqual(5, pips.Length, "_levelPips 길이");
            RectTransform[] textBlock = Required<RectTransform[]>(cell, "_textBlock");
            Assert.Greater(textBlock.Length, 0, "_textBlock 길이");
        }

        //# --- ③ ShopItemCell 동작 ---

        [Test]
        public void ShopItemCell_헤더와_항목_행을_반복_Bind해도_itemDecor_토글_잔상이_없다()
        {
            ShopItemCell cell = NewShopCell();
            GameObject decor = Required<GameObject>(cell, "_itemDecor");

            cell.Bind(ItemData(true, false));
            Assert.IsTrue(decor.activeSelf, "1차 항목");
            cell.Bind(HeaderData());
            Assert.IsFalse(decor.activeSelf, "2차 헤더");
            cell.Bind(ItemData(false, false));
            Assert.IsTrue(decor.activeSelf, "3차 항목");
            cell.Bind(HeaderData());
            cell.Bind(HeaderData());
            Assert.IsFalse(decor.activeSelf, "4~5차 헤더 연속");
            cell.Bind(ItemData(false, true));
            Assert.IsTrue(decor.activeSelf, "6차 항목");
        }

        [Test]
        public void ShopItemCell_헤더_행_Bind는_StateRing도_함께_꺼서_상태_장식_잔상이_없다()
        {
            ShopItemCell cell = NewShopCell();
            Image ring = Required<Image>(cell, "_stateRing");

            cell.Bind(ItemData(true, false));
            Assert.IsTrue(ring.gameObject.activeSelf, "구매 가능 항목");
            cell.Bind(HeaderData());

            Assert.IsFalse(ring.gameObject.activeSelf, "헤더 행에서 링 꺼짐");
        }

        [Test]
        public void ShopItemCell_구매_가능_Soul_상태는_링을_켜고_틴트를_Soul로_칠하며_알파를_보존한다()
        {
            ShopItemCell cell = NewShopCell();
            Image ring = Required<Image>(cell, "_stateRing");
            Image[] tinted = Required<Image[]>(cell, "_stateTinted");

            cell.Bind(ItemData(true, false));

            Assert.IsTrue(ring.gameObject.activeSelf, "StateRing");
            AssertRgb(UiDotPalette.Soul, ring.color, "링");
            for (int i = 0; i < tinted.Length; ++i)
            {
                AssertRgb(UiDotPalette.Soul, tinted[i].color, tinted[i].name);
                Assert.AreEqual(StateTintAlphas[i], tinted[i].color.a, 0.001f, tinted[i].name + " 알파 보존");
            }
        }

        [Test]
        public void ShopItemCell_만렙_Gold_상태는_링을_켜고_틴트를_Gold로_칠하며_알파를_보존한다()
        {
            ShopItemCell cell = NewShopCell();
            Image ring = Required<Image>(cell, "_stateRing");
            Image[] tinted = Required<Image[]>(cell, "_stateTinted");

            cell.Bind(ItemData(false, true));

            Assert.IsTrue(ring.gameObject.activeSelf, "StateRing");
            AssertRgb(UiDotPalette.Gold, ring.color, "링");
            for (int i = 0; i < tinted.Length; ++i)
            {
                AssertRgb(UiDotPalette.Gold, tinted[i].color, tinted[i].name);
                Assert.AreEqual(StateTintAlphas[i], tinted[i].color.a, 0.001f, tinted[i].name + " 알파 보존");
            }
        }

        [Test]
        public void ShopItemCell_소울_부족_상태는_StateRing이_꺼진다()
        {
            ShopItemCell cell = NewShopCell();
            Image ring = Required<Image>(cell, "_stateRing");

            cell.Bind(ItemData(true, false));
            Assert.IsTrue(ring.gameObject.activeSelf, "사전: 구매 가능");
            cell.Bind(ItemData(false, false));

            Assert.IsFalse(ring.gameObject.activeSelf, "소울 부족 시 링 꺼짐");
        }

        [Test]
        public void ShopItemCell_상태를_반복_전환해도_틴트_알파가_변하지_않고_rgb는_마지막_상태를_따른다()
        {
            ShopItemCell cell = NewShopCell();
            Image[] tinted = Required<Image[]>(cell, "_stateTinted");

            bool[][] sequence =
            {
                new[] { true, false }, new[] { false, true }, new[] { false, false },
                new[] { true, false }, new[] { false, true }, new[] { false, true },
            };
            foreach (bool[] step in sequence)
            {
                bool canBuy = step[0];
                bool isMax = step[1];
                cell.Bind(ItemData(canBuy, isMax));
                Color expected = isMax ? UiDotPalette.Gold : UiDotPalette.Soul;
                for (int i = 0; i < tinted.Length; ++i)
                {
                    AssertRgb(expected, tinted[i].color, $"canBuy={canBuy} isMax={isMax} {tinted[i].name}");
                    Assert.AreEqual(StateTintAlphas[i], tinted[i].color.a, 0.001f, $"{tinted[i].name} 알파");
                }
            }
        }

        [Test]
        public void ShopItemCell_stateTinted가_null이거나_원소가_null이어도_Bind가_예외없다()
        {
            ShopItemCell cell = NewShopCell();
            Image[] original = Required<Image[]>(cell, "_stateTinted");

            SetField(cell, "_stateTinted", null);
            Assert.DoesNotThrow(() => cell.Bind(ItemData(true, false)), "배열 null");

            SetField(cell, "_stateTinted", new Image[0]);
            Assert.DoesNotThrow(() => cell.Bind(ItemData(false, true)), "빈 배열");

            SetField(cell, "_stateTinted", new Image[] { null, original[1] });
            Assert.DoesNotThrow(() => cell.Bind(ItemData(true, false)), "원소 null");
            AssertRgb(UiDotPalette.Soul, original[1].color, "null 원소를 건너뛰고 나머지는 칠함");
        }

        [Test]
        public void ShopItemCell_itemDecor가_비어_있어도_헤더_항목_Bind가_예외없다()
        {
            ShopItemCell cell = NewShopCell();
            SetField(cell, "_itemDecor", null);

            Assert.DoesNotThrow(() => cell.Bind(HeaderData()));
            Assert.DoesNotThrow(() => cell.Bind(ItemData(true, false)));
        }

        [Test]
        public void ShopItemCell_위젯_필드가_전부_비어_있어도_Bind가_예외없다()
        {
            //# EditMode 에서는 OnEnable/Awake 가 자동 호출되지 않으므로 필드 초기값 그대로의 빈 컴포넌트를 쓴다.
            GameObject go = new GameObject("EmptyShopCell");
            _spawned.Add(go);
            ShopItemCell cell = go.AddComponent<ShopItemCell>();

            Assert.DoesNotThrow(() => cell.Bind(HeaderData()), "헤더");
            Assert.DoesNotThrow(() => cell.Bind(ItemData(true, false)), "구매 가능");
            Assert.DoesNotThrow(() => cell.Bind(ItemData(false, true)), "만렙");
            Assert.DoesNotThrow(() => cell.Bind(ItemData(false, false)), "소울 부족");
            Assert.DoesNotThrow(() => cell.Bind(null), "null 데이터");
        }

        //# --- ④ CHPoolingScrollView _origin 보존 · m_RemovedComponents ---

        [Test]
        public void ShopPopup_origin이_ShopItemCell_프리팹_인스턴스를_가리킨다()
        {
            ShopPopup popup = Comp<ShopPopup>(LoadPrefabAsset(ShopPopupPath));

            GameObject origin = OriginOf(popup, "_scrollView");

            Assert.IsTrue(origin.GetComponent<ShopItemCell>() != null, "origin 에 ShopItemCell");
            Assert.AreEqual(ShopCellPath, SourcePrefabPath(origin), "origin 원본 프리팹");
        }

        [Test]
        public void CodexPopup_origin_2개가_CodexCell_프리팹의_서로_다른_인스턴스다()
        {
            CodexPopup popup = Comp<CodexPopup>(LoadPrefabAsset(CodexPopupPath));

            GameObject monster = OriginOf(popup, "_monsterScrollView");
            GameObject card = OriginOf(popup, "_cardScrollView");

            Assert.IsTrue(monster.GetComponent<CodexCell>() != null, "몬스터 origin 에 CodexCell");
            Assert.IsTrue(card.GetComponent<CodexCell>() != null, "카드 origin 에 CodexCell");
            Assert.AreEqual(CodexCellPath, SourcePrefabPath(monster), "몬스터 origin 원본");
            Assert.AreEqual(CodexCellPath, SourcePrefabPath(card), "카드 origin 원본");
            Assert.AreNotSame(monster, card, "두 origin 은 서로 다른 인스턴스");
            Assert.AreNotSame(monster.transform.parent, card.transform.parent, "서로 다른 Content 아래");
        }

        [Test]
        public void RecordsPopup_origin이_RecordsStageCell_프리팹_인스턴스를_가리킨다()
        {
            RecordsPopup popup = Comp<RecordsPopup>(LoadPrefabAsset(RecordsPopupPath));

            GameObject origin = OriginOf(popup, "_scrollView");

            Assert.IsTrue(origin.GetComponent<RecordsStageCell>() != null, "origin 에 RecordsStageCell");
            Assert.AreEqual(RecordsCellPath, SourcePrefabPath(origin), "origin 원본 프리팹");
        }

        [TestCase(ShopPopupPath, 1)]
        [TestCase(CodexPopupPath, 2)]
        [TestCase(RecordsPopupPath, 1)]
        public void 팝업_프리팹의_셀_인스턴스는_m_RemovedComponents가_비어_있다(string path, int instanceCount)
        {
            string yaml = File.ReadAllText(FullPath(path));

            Assert.AreEqual(instanceCount, Regex.Matches(yaml, @"^PrefabInstance:", RegexOptions.Multiline).Count, path + " PrefabInstance 수");
            Assert.AreEqual(instanceCount, Regex.Matches(yaml, @"m_RemovedComponents:").Count, path + " m_RemovedComponents 키 수");
            Assert.AreEqual(0, Regex.Matches(yaml, @"m_RemovedComponents:\s*\r?\n\s*-").Count, path + " 제거된 컴포넌트 존재(참조 null 위험)");
        }

        //# --- ⑤ Deco raycastTarget ---

        [TestCase(ShopPopupPath)]
        [TestCase(ShopCellPath)]
        [TestCase(CodexPopupPath)]
        [TestCase(CodexCellPath)]
        [TestCase(RecordsPopupPath)]
        [TestCase(RecordsCellPath)]
        public void Deco로_시작하는_오브젝트의_Graphic은_raycastTarget이_꺼져_있다(string path)
        {
            int decoCount = 0;
            foreach (Graphic graphic in LoadPrefabAsset(path).GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.name.StartsWith("Deco") == false)
                    continue;

                ++decoCount;
                Assert.IsFalse(graphic.raycastTarget, $"{path} {graphic.name} raycastTarget");
            }
            Assert.Greater(decoCount, 0, path + " Deco Graphic 수집 확인");
        }

        [TestCase(ShopPopupPath, 1)]
        [TestCase(CodexPopupPath, 2)]
        [TestCase(RecordsPopupPath, 1)]
        public void 팝업의_Scrollbar와_Handle과_Dim_닫기버튼은_raycastTarget이_켜져_있다(string path, int scrollbarCount)
        {
            int scrollbars = 0;
            int handles = 0;
            int dims = 0;
            int closes = 0;
            foreach (Graphic graphic in LoadPrefabAsset(path).GetComponentsInChildren<Graphic>(true))
            {
                switch (graphic.name)
                {
                    case "Scrollbar":
                        ++scrollbars;
                        Assert.IsTrue(graphic.raycastTarget, path + " Scrollbar raycastTarget");
                        break;
                    case "Handle":
                        ++handles;
                        Assert.IsTrue(graphic.raycastTarget, path + " Handle raycastTarget");
                        break;
                    case "Dim":
                        ++dims;
                        Assert.IsTrue(graphic.raycastTarget, path + " Dim raycastTarget");
                        break;
                    case "CloseButton":
                        ++closes;
                        Assert.IsTrue(graphic.raycastTarget, path + " CloseButton raycastTarget");
                        break;
                }
            }
            Assert.AreEqual(scrollbarCount, scrollbars, "Scrollbar Graphic 수");
            Assert.AreEqual(scrollbarCount, handles, "Handle Graphic 수");
            Assert.AreEqual(1, dims, "Dim Graphic 수");
            Assert.AreEqual(1, closes, "CloseButton Graphic 수");
        }

        //# --- ⑥ 기록 셀 폭 424 ---

        [Test]
        public void RecordsStageCell_프리팹_루트_크기는_424x84다()
        {
            RectTransform root = (RectTransform)LoadPrefabAsset(RecordsCellPath).transform;

            Assert.AreEqual(424f, root.sizeDelta.x, 0.01f, "셀 루트 폭");
            Assert.AreEqual(84f, root.sizeDelta.y, 0.01f, "셀 루트 높이");
        }

        [Test]
        public void RecordsPopup_origin_오버라이드_폭도_424다()
        {
            RecordsPopup popup = Comp<RecordsPopup>(LoadPrefabAsset(RecordsPopupPath));
            GameObject origin = OriginOf(popup, "_scrollView");

            RectTransform rect = origin.GetComponent<RectTransform>();
            Assert.AreEqual(424f, rect.sizeDelta.x, 0.01f, "origin 폭");
            Assert.AreEqual(84f, rect.sizeDelta.y, 0.01f, "origin 높이");
        }

        //# --- ⑦ 도감 탭 Checkmark 금 링 ---

        [TestCase("_monsterTab")]
        [TestCase("_cardTab")]
        public void 도감_탭의_Checkmark는_금색_CardSelectFrame_링이다(string tabField)
        {
            CodexPopup popup = Comp<CodexPopup>(LoadPrefabAsset(CodexPopupPath));
            Toggle tab = Required<Toggle>(popup, tabField);

            Image checkmark = tab.graphic as Image;
            Assert.IsTrue(checkmark != null, tabField + " Toggle.graphic 이 Image 가 아니거나 비어 있음");
            Assert.AreEqual("Checkmark", checkmark.name, "graphic 오브젝트 이름");
            Assert.IsTrue(checkmark.sprite != null, "Checkmark 스프라이트 비어 있음");
            Assert.AreEqual("CardSelectFrame", checkmark.sprite.name, "Checkmark 스프라이트");
            Assert.AreEqual(Image.Type.Sliced, checkmark.type, "Checkmark 9-slice");
            Assert.IsFalse(checkmark.fillCenter, "링이므로 fillCenter 꺼짐");
            AssertRgb(new Color32(0xF7, 0xC6, 0x4A, 0xFF), checkmark.color, "Checkmark 금색 #f7c64a");
            Assert.IsTrue(checkmark.transform.IsChildOf(tab.transform), "Checkmark 는 탭의 자식");
        }

        [Test]
        public void 도감_두_탭의_Checkmark는_서로_다른_오브젝트이고_초기_선택은_몬스터_탭이다()
        {
            CodexPopup popup = Comp<CodexPopup>(LoadPrefabAsset(CodexPopupPath));
            Toggle monster = Required<Toggle>(popup, "_monsterTab");
            Toggle card = Required<Toggle>(popup, "_cardTab");

            Assert.IsTrue(monster.graphic != null && card.graphic != null, "graphic 연결");
            Assert.AreNotSame(monster.graphic, card.graphic, "탭마다 별도 Checkmark");
            Assert.IsTrue(monster.isOn, "MonsterTab 초기 isOn");
            Assert.IsFalse(card.isOn, "CardTab 초기 isOn");
        }

        //# --- ⑧ 일회용 빌더 없음 ---

        [Test]
        public void 일회용_장식_빌더_스크립트가_레포에_없다()
        {
            string editorDir = Path.GetFullPath(Path.Combine(Application.dataPath, "_Lair", "Editor"));
            Assert.IsTrue(Directory.Exists(editorDir), editorDir);

            string[] batch = Directory.GetFiles(editorDir, "*Batch*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, batch.Length, "Batch 에디터 스크립트 잔존: " + string.Join(", ", batch));
            string[] decoBuilders = Directory.GetFiles(editorDir, "*Deco*Builder*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, decoBuilders.Length, "Deco 빌더 잔존: " + string.Join(", ", decoBuilders));
        }
    }
}
