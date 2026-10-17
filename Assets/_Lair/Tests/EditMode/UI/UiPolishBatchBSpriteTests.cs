using System;
using System.Collections.Generic;
using System.Reflection;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 비주얼 폴리시 묶음 B 스프라이트 연결 보강(ui-polish-batch-b §9.2 스프라이트 연결 · 스크롤바) —
    //# 닫기 버튼 몸체 · 구매 버튼 스킨 슬롯과 상태 전환 · 스크롤바 AutoHide · 도감/기록 셀 상태별 배경 전환.
    public class UiPolishBatchBSpriteTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string ShopPopupPath = UiDir + "ShopPopup.prefab";
        private const string ShopCellPath = UiDir + "ShopItemCell.prefab";
        private const string CodexPopupPath = UiDir + "CodexPopup.prefab";
        private const string CodexCellPath = UiDir + "CodexCell.prefab";
        private const string RecordsPopupPath = UiDir + "RecordsPopup.prefab";
        private const string RecordsCellPath = UiDir + "RecordsStageCell.prefab";

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

        private static T Required<T>(object target, string name) where T : class
        {
            object value = FieldOf(target, name).GetValue(target);
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

        private static string SpriteName(Image image, string label)
        {
            Assert.IsTrue(image != null, label + " Image 없음");
            Assert.IsTrue(image.sprite != null, label + " 스프라이트 비어 있음");
            return image.sprite.name;
        }

        private static string SpriteName(Sprite sprite, string label)
        {
            Assert.IsTrue(sprite != null, label + " 스프라이트 비어 있음");
            return sprite.name;
        }

        //# --- 닫기 버튼 몸체 = VillageHudCta (3곳) ---

        [TestCase(ShopPopupPath)]
        [TestCase(CodexPopupPath)]
        [TestCase(RecordsPopupPath)]
        public void 닫기_버튼_Image_스프라이트는_VillageHudCta다(string path)
        {
            int closes = 0;
            foreach (Image image in LoadPrefabAsset(path).GetComponentsInChildren<Image>(true))
            {
                if (image.name != "CloseButton")
                    continue;

                ++closes;
                Assert.AreEqual("VillageHudCta", SpriteName(image, path + " CloseButton"), path + " CloseButton 스프라이트");
            }
            Assert.AreEqual(1, closes, path + " CloseButton Image 수");
        }

        //# --- 구매 버튼 스킨 슬롯 유지 + 상태별 전환 ---

        [Test]
        public void 상점_셀의_구매_버튼_스킨_슬롯은_Px_BtnSoul_Off_Gold를_유지한다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));

            Assert.AreEqual("Px_BtnSoul", SpriteName(Required<Sprite>(cell, "_buySoulSprite"), "_buySoulSprite"));
            Assert.AreEqual("Px_BtnOff", SpriteName(Required<Sprite>(cell, "_buyOffSprite"), "_buyOffSprite"));
            Assert.AreEqual("Px_BtnGold", SpriteName(Required<Sprite>(cell, "_buyGoldSprite"), "_buyGoldSprite"));
        }

        [Test]
        public void 상점_셀_Bind는_상태별로_구매_버튼_스프라이트를_Soul_Off_Gold로_전환한다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(NewInstance(ShopCellPath));
            Image buttonImage = Required<Image>(cell, "_buyButtonImage");

            cell.Bind(ItemData(true, false));
            Assert.AreEqual("Px_BtnSoul", SpriteName(buttonImage, "구매 가능"));
            cell.Bind(ItemData(false, false));
            Assert.AreEqual("Px_BtnOff", SpriteName(buttonImage, "소울 부족"));
            cell.Bind(ItemData(false, true));
            Assert.AreEqual("Px_BtnGold", SpriteName(buttonImage, "만렙"));
            cell.Bind(ItemData(true, false));
            Assert.AreEqual("Px_BtnSoul", SpriteName(buttonImage, "재 Bind 구매 가능"));
        }

        [Test]
        public void 상점_셀의_구매_버튼은_Image가_버튼_타깃_그래픽이고_raycastTarget이_켜져_있다()
        {
            ShopItemCell cell = Comp<ShopItemCell>(LoadPrefabAsset(ShopCellPath));
            Image buttonImage = Required<Image>(cell, "_buyButtonImage");
            Component buyButton = Required<Component>(cell, "_buyButton");

            Assert.IsTrue(buttonImage.raycastTarget, "구매 버튼 raycastTarget");
            Assert.IsTrue(buttonImage.transform.IsChildOf(buyButton.transform), "구매 버튼 Image 는 버튼 하위");
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

        //# --- 스크롤바 AutoHide ---

        [TestCase(ShopPopupPath, 1)]
        [TestCase(CodexPopupPath, 2)]
        [TestCase(RecordsPopupPath, 1)]
        public void ScrollRect의_세로_스크롤바가_연결되고_AutoHide다(string path, int scrollRectCount)
        {
            ScrollRect[] rects = LoadPrefabAsset(path).GetComponentsInChildren<ScrollRect>(true);

            Assert.AreEqual(scrollRectCount, rects.Length, path + " ScrollRect 수");
            foreach (ScrollRect rect in rects)
            {
                Assert.IsTrue(rect.verticalScrollbar != null, $"{path} {rect.name} verticalScrollbar 비어 있음");
                Assert.AreEqual(ScrollRect.ScrollbarVisibility.AutoHide, rect.verticalScrollbarVisibility, $"{path} {rect.name} Visibility");
            }
        }

        //# --- 도감 셀 상태별 배경 ---

        [Test]
        public void 도감_셀_스킨_슬롯은_SynergyModalCellBg와_VillageHudBtnOff다()
        {
            CodexCell cell = Comp<CodexCell>(LoadPrefabAsset(CodexCellPath));

            Assert.AreEqual("SynergyModalCellBg", SpriteName(Required<Sprite>(cell, "_normalSprite"), "_normalSprite"));
            Assert.AreEqual("VillageHudBtnOff", SpriteName(Required<Sprite>(cell, "_dummySprite"), "_dummySprite"));
        }

        [Test]
        public void 도감_셀_Bind는_잠금_더미와_해금_사이에서_배경_스프라이트를_전환하고_아이콘을_토글한다()
        {
            CodexCell cell = Comp<CodexCell>(NewInstance(CodexCellPath));
            Image background = Required<Image>(cell, "_background");
            Image icon = Required<Image>(cell, "_icon");

            cell.Bind(new CodexCellData { DisplayName = "???", IsLockedDummy = true });
            Assert.AreEqual("VillageHudBtnOff", SpriteName(background, "잠금 더미"));
            Assert.IsFalse(icon.gameObject.activeSelf, "잠금 더미 아이콘 숨김");

            cell.Bind(new CodexCellData { DisplayName = "해금", Unlocked = true, TintColor = Color.green });
            Assert.AreEqual("SynergyModalCellBg", SpriteName(background, "해금"));
            Assert.IsTrue(icon.gameObject.activeSelf, "해금 아이콘 표시");

            cell.Bind(new CodexCellData { DisplayName = "???", IsLockedDummy = true });
            Assert.AreEqual("VillageHudBtnOff", SpriteName(background, "재 Bind 잠금 더미"));
            Assert.IsFalse(icon.gameObject.activeSelf, "재 Bind 아이콘 숨김");
        }

        [Test]
        public void 도감_셀_미조우_비더미는_일반_배경을_쓴다()
        {
            CodexCell cell = Comp<CodexCell>(NewInstance(CodexCellPath));
            Image background = Required<Image>(cell, "_background");

            cell.Bind(new CodexCellData { DisplayName = "미조우", Unlocked = false, IsLockedDummy = false });

            Assert.AreEqual("SynergyModalCellBg", SpriteName(background, "미조우"));
        }

        //# --- 기록 셀 상태별 배경 ---

        [Test]
        public void 기록_셀_스킨_슬롯은_SynergyModalCellBg와_VillageHudBtnOff다()
        {
            RecordsStageCell cell = Comp<RecordsStageCell>(LoadPrefabAsset(RecordsCellPath));

            Assert.AreEqual("SynergyModalCellBg", SpriteName(Required<Sprite>(cell, "_normalSprite"), "_normalSprite"));
            Assert.AreEqual("VillageHudBtnOff", SpriteName(Required<Sprite>(cell, "_lockedSprite"), "_lockedSprite"));
        }

        [Test]
        public void 기록_셀_Bind는_잠금_해금_선택_상태별로_배경과_링_배지를_전환한다()
        {
            RecordsStageCell cell = Comp<RecordsStageCell>(NewInstance(RecordsCellPath));
            Image background = Required<Image>(cell, "_background");
            Image ring = Required<Image>(cell, "_selectedRing");
            GameObject badge = Required<GameObject>(cell, "_selectedBadge");

            cell.Bind(new RecordsStageCellData { IsLocked = true, PortraitTint = Color.white });
            Assert.AreEqual("VillageHudBtnOff", SpriteName(background, "잠금"));
            Assert.IsFalse(ring.gameObject.activeSelf, "잠금 링");
            Assert.IsFalse(badge.activeSelf, "잠금 배지");

            cell.Bind(new RecordsStageCellData { IsLocked = false, IsSelected = true, PortraitTint = Color.white });
            Assert.AreEqual("SynergyModalCellBg", SpriteName(background, "해금 선택"));
            Assert.IsTrue(ring.gameObject.activeSelf, "선택 링");
            Assert.IsTrue(badge.activeSelf, "선택 배지");

            cell.Bind(new RecordsStageCellData { IsLocked = false, IsSelected = false, PortraitTint = Color.white });
            Assert.AreEqual("SynergyModalCellBg", SpriteName(background, "해금 비선택"));
            Assert.IsFalse(ring.gameObject.activeSelf, "비선택 링");
            Assert.IsFalse(badge.activeSelf, "비선택 배지");
        }

        [Test]
        public void 기록_셀_Portrait가_null이면_초상_이미지가_숨겨진다()
        {
            RecordsStageCell cell = Comp<RecordsStageCell>(NewInstance(RecordsCellPath));
            Image portrait = Required<Image>(cell, "_portrait");

            cell.Bind(new RecordsStageCellData { Portrait = null, PortraitTint = Color.white });

            Assert.IsFalse(portrait.gameObject.activeSelf, "Portrait==null 숨김");
        }
    }
}
