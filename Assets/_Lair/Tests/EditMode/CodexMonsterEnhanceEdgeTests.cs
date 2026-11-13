using System;
using System.Collections.Generic;
using System.Reflection;
using Lair.Card;
using Lair.Data;
using Lair.Meta;
using Lair.Tests.Helpers;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 도감 카드 셀 데이터 계약 — 몬스터 탭은 monster-codex 개편(CodexViewModel)으로 이관돼 여기선 카드 셀 격리만 본다.
    //# 레벨→시각 매핑·뷰 클램프(CodexCell)는 아래 클래스들이 계속 담당.
    public class CodexBuildMonsterCellDataTests
    {
        private static MetaConfig Cfg()
        {
            return new MetaConfig();
        }

        //# 격리 — 카드 셀에는 몬스터 필드가 새지 않는다(Species null, EnhanceLevel 0). 잠금 더미는 더 이상 붙지 않는다(D7).
        [Test]
        public void 카드셀은_몬스터필드를_담지_않고_더미가_붙지_않는다()
        {
            MetaProfile p = new MetaProfile();
            CardData c1 = FakeCardData.Create(ECardId.Frenzy);
            CardData c2 = FakeCardData.Create(ECardId.Berserk);
            p.PickedCards.Add(c1.Id.ToString());
            List<CardData> cards = new List<CardData> { c1, c2 };

            List<CodexCellData> list = CodexPopup.BuildCardCellData(p, Cfg(), cards);

            Assert.AreEqual(2, list.Count);
            foreach (CodexCellData cell in list)
            {
                Assert.IsFalse(cell.Species.HasValue, "카드 셀 Species=null");
                Assert.AreEqual(0, cell.EnhanceLevel, "카드 셀 EnhanceLevel=0");
                Assert.IsFalse(cell.IsLockedDummy);
            }
        }
    }

    //# 레벨→시각 매핑 배열 — 구조 불변식(정확값 핀은 기존 테스트가 담당, 여기선 다른 실패모드).
    //# 단조 비감소·Lv0 항등·Lv5 상한: SoT 재튜닝을 살아남되 잘못된 밸런스 편집(예: Lv3<Lv2)을 잡는다.
    public class CodexEnhanceMappingArrayTests
    {
        private const int ExpectedLen = 6;   //# Lv0~Lv5

        private static void AssertMonotonicNonDecreasing(float[] arr, string name)
        {
            for (int i = 1; i < arr.Length; ++i)
            {
                Assert.GreaterOrEqual(arr[i], arr[i - 1], $"{name}[{i}] 는 [{i - 1}] 이상(비감소)");
            }
        }

        [Test]
        public void 세_매핑배열은_모두_길이6이다()
        {
            Assert.AreEqual(ExpectedLen, EnhanceLevelVisual.IconTintByLevel.Length);
            Assert.AreEqual(ExpectedLen, EnhanceLevelVisual.GlowOverlayAlphaByLevel.Length);
            Assert.AreEqual(ExpectedLen, EnhanceLevelVisual.ScaleByLevel.Length);
        }

        [Test]
        public void 세_매핑배열은_레벨에_따라_단조_비감소한다()
        {
            AssertMonotonicNonDecreasing(EnhanceLevelVisual.IconTintByLevel, "IconTint");
            AssertMonotonicNonDecreasing(EnhanceLevelVisual.GlowOverlayAlphaByLevel, "GlowAlpha");
            AssertMonotonicNonDecreasing(EnhanceLevelVisual.ScaleByLevel, "Scale");
        }

        //# Lv0 = 강화 없음 항등: 틴트 0(원본색), 발광 0(불투명 오버레이 없음), 스케일 1(원본 크기).
        [Test]
        public void Lv0은_강화없음_항등값이다()
        {
            Assert.AreEqual(0f, EnhanceLevelVisual.IconTintByLevel[0], 1e-4f, "Lv0 틴트=0(원본색)");
            Assert.AreEqual(0f, EnhanceLevelVisual.GlowOverlayAlphaByLevel[0], 1e-4f, "Lv0 발광α=0");
            Assert.AreEqual(1f, EnhanceLevelVisual.ScaleByLevel[0], 1e-4f, "Lv0 스케일=1(원본)");
        }

        //# Lv5 = 상한: 각 배열의 마지막 원소가 곧 최댓값(만렙이 가장 강한 표현).
        [Test]
        public void Lv5는_각_배열의_상한_최댓값이다()
        {
            AssertLastIsMax(EnhanceLevelVisual.IconTintByLevel, "IconTint");
            AssertLastIsMax(EnhanceLevelVisual.GlowOverlayAlphaByLevel, "GlowAlpha");
            AssertLastIsMax(EnhanceLevelVisual.ScaleByLevel, "Scale");
        }

        private static void AssertLastIsMax(float[] arr, string name)
        {
            float last = arr[arr.Length - 1];
            foreach (float v in arr)
            {
                Assert.LessOrEqual(v, last, $"{name} 마지막 원소가 최댓값(만렙=상한)");
            }
        }
    }

    //# 뷰 클램프 — 변조 세이브가 만렙 초과/음수 레벨을 실어도 CodexCell 이 배열 인덱싱 전에 Clamp.
    //# GetShopLevel/SetShopLevel 은 상한 없음 → 입력 도달 가능, 클램프는 load-bearing.
    //# _iconRect 만 배선: 스케일 라인은 _iconRect!=null 만 가드 → 클램프 제거 시 ScaleByLevel[99]가 여기서 throw.
    public class CodexCellClampTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        private CodexCell MakeCellWithIconRect(out RectTransform iconRect)
        {
            _root = new GameObject("CodexCellTest");
            CodexCell cell = _root.AddComponent<CodexCell>();

            GameObject iconGo = new GameObject("icon", typeof(RectTransform));
            iconGo.transform.SetParent(_root.transform, false);
            iconRect = iconGo.GetComponent<RectTransform>();

            FieldInfo field = typeof(CodexCell).GetField("_iconRect", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "_iconRect 필드 존재(리네임 시 이 테스트가 붉게 알림)");
            field.SetValue(cell, iconRect);
            return cell;
        }

        //# 만렙 초과(99) → Clamp→5 → 스케일 상한 1.10, IndexOutOfRange 없음.
        [Test]
        public void 만렙초과_레벨은_클램프되어_스케일_상한을_넘지_않는다()
        {
            RectTransform iconRect;
            CodexCell cell = MakeCellWithIconRect(out iconRect);

            Assert.DoesNotThrow(() => cell.Bind(new CodexCellData
            {
                DisplayName = "도깨비불",
                Unlocked = true,
                Species = EMonster.Wisp,
                EnhanceLevel = 99,
            }));

            Assert.AreEqual(EnhanceLevelVisual.ScaleByLevel[5], iconRect.localScale.x, 1e-4f, "Lv99 → Clamp→5 스케일 상한");
        }

        //# 음수 레벨 → Clamp→0 → 스케일 항등 1.0, throw 없음.
        [Test]
        public void 음수_레벨은_클램프되어_스케일_항등값이_된다()
        {
            RectTransform iconRect;
            CodexCell cell = MakeCellWithIconRect(out iconRect);

            Assert.DoesNotThrow(() => cell.Bind(new CodexCellData
            {
                DisplayName = "저주술사",
                Unlocked = true,
                Species = EMonster.Hex,
                EnhanceLevel = -3,
            }));

            Assert.AreEqual(EnhanceLevelVisual.ScaleByLevel[0], iconRect.localScale.x, 1e-4f, "음수 → Clamp→0 항등 스케일");
        }
    }
}
