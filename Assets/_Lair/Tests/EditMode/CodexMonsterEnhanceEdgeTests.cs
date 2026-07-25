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
    //# 도감 몬스터 강화 — 엣지·회귀 보강. 기존 CodexMonsterEnhanceLevelTests(기본 4건)와 중복 없이:
    //#  1) BuildMonsterCellData 6종 전수·계약, 카드/더미 필드 격리, 가드
    //#  2) 레벨→시각 매핑 배열 구조 불변식(단조·Lv0 항등·Lv5 상한) — 기존 "정확값 핀"과 다른 실패모드
    //#  3) 뷰 클램프(CodexCell) — 변조 레벨(99/음수)에서 배열 인덱싱 안전(§9)
    //# 실제 픽셀/발광 렌더는 프리팹·육안(§7.2) 소관 — 본 스위트는 데이터·매핑 상수·계약만.
    public class CodexBuildMonsterCellDataTests
    {
        //# 다른 클래스의 Cfg()는 private — 본 파일 전용 헬퍼를 둔다(격리·이식성).
        private static MetaConfig Cfg(int lockedSlots = 0)
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.CodexLockedSlots = lockedSlots;
            return cfg;
        }

        private static MetaProfile AllSeen()
        {
            MetaProfile p = new MetaProfile();
            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                p.SeenMonsters.Add(type.ToString());
            }
            return p;
        }

        //# 전수 — 몬스터 셀은 enum 순서대로 6개, 각 Species 가 정확히 매핑.
        [Test]
        public void 몬스터셀은_enum순서대로_6종_전수_매핑된다()
        {
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(AllSeen(), Cfg(), _ => null);

            EMonster[] all = (EMonster[])Enum.GetValues(typeof(EMonster));
            Assert.AreEqual(all.Length, list.Count, "잠금 더미 0일 때 몬스터 셀 수 = 종족 수");
            for (int i = 0; i < all.Length; ++i)
            {
                Assert.AreEqual(all[i], list[i].Species, $"index {i} Species 매핑·순서");
            }
        }

        //# 회귀 — 해금 종족 DisplayName = SpeciesVisual.SpeciesName(한글 SoT). 6종 전수.
        [Test]
        public void 해금_종족_표시명은_SpeciesName_한글과_일치한다()
        {
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(AllSeen(), Cfg(), _ => null);

            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                CodexCellData cell = list.Find(c => c.Species == type);
                Assert.IsNotNull(cell);
                Assert.AreEqual(SpeciesVisual.SpeciesName(type), cell.DisplayName, $"{type} 표시명");
            }
        }

        //# 회귀 — TintColor = SpawnerStatusCell.SpeciesColor(종 색 SoT). 6종 전수.
        [Test]
        public void 몬스터셀_틴트색은_SpeciesColor_SoT와_일치한다()
        {
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(AllSeen(), Cfg(), _ => null);

            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                CodexCellData cell = list.Find(c => c.Species == type);
                Assert.IsNotNull(cell);
                Assert.AreEqual(SpawnerStatusCell.SpeciesColor(type), cell.TintColor, $"{type} 틴트");
            }
        }

        //# 회귀 — 미해금 종족은 표시명 ???·Unlocked false, 해금 종족만 true (부분 해금 혼재).
        [Test]
        public void 부분해금시_해금여부가_SeenMonsters를_반영한다()
        {
            MetaProfile p = new MetaProfile();
            p.SeenMonsters.Add(EMonster.Wisp.ToString());
            p.SeenMonsters.Add(EMonster.Reaper.ToString());

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);

            Assert.IsTrue(list.Find(c => c.Species == EMonster.Wisp).Unlocked);
            Assert.IsTrue(list.Find(c => c.Species == EMonster.Reaper).Unlocked);
            CodexCellData wraith = list.Find(c => c.Species == EMonster.Wraith);
            Assert.IsFalse(wraith.Unlocked);
            Assert.AreEqual("???", wraith.DisplayName);
        }

        //# 데이터 계약 — BuildMonsterCellData 는 레벨을 raw 로 저장(클램프 안 함, 뷰가 클램프). 만렙 초과.
        [Test]
        public void 데이터층은_만렙초과_레벨을_클램프없이_그대로_읽는다()
        {
            MetaProfile p = AllSeen();
            p.SetShopLevel("Enhance_Wisp", 99);

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);

            Assert.AreEqual(99, list.Find(c => c.Species == EMonster.Wisp).EnhanceLevel,
                "데이터층은 raw 저장 — 인덱싱 안전은 뷰 클램프(CodexCellClampTests) 책임");
        }

        //# 데이터 계약 — 음수 레벨도 raw 통과(변조 세이브 대비). 뷰에서 Clamp→0.
        [Test]
        public void 데이터층은_음수_레벨도_그대로_읽는다()
        {
            MetaProfile p = AllSeen();
            p.SetShopLevel("Enhance_Hex", -3);

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);

            Assert.AreEqual(-3, list.Find(c => c.Species == EMonster.Hex).EnhanceLevel);
        }

        //# 격리 — 카드 셀에는 몬스터 필드가 새지 않는다(Species null, EnhanceLevel 0).
        [Test]
        public void 카드셀은_몬스터필드를_담지_않는다()
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
            }
        }

        //# 격리 — 잠금 더미도 몬스터 필드가 없다(Species null, EnhanceLevel 0, IsLockedDummy).
        [Test]
        public void 잠금더미셀은_몬스터필드없이_더미플래그만_든다()
        {
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(AllSeen(), Cfg(lockedSlots: 2), _ => null);

            List<CodexCellData> dummies = list.FindAll(c => c.IsLockedDummy);
            Assert.AreEqual(2, dummies.Count, "CodexLockedSlots=2 → 더미 2개 append");
            foreach (CodexCellData d in dummies)
            {
                Assert.IsFalse(d.Species.HasValue, "더미 Species=null");
                Assert.AreEqual(0, d.EnhanceLevel, "더미 EnhanceLevel=0");
            }
        }

        //# 가드 — null profile / null cfg 는 빈 리스트(NRE 없음).
        [Test]
        public void null_profile나_config면_빈_리스트를_반환한다()
        {
            Assert.AreEqual(0, CodexPopup.BuildMonsterCellData(null, Cfg(), _ => null).Count);
            Assert.AreEqual(0, CodexPopup.BuildMonsterCellData(AllSeen(), null, _ => null).Count);
        }

        //# 가드 — iconResolver null 이어도 NRE 없이 Icon=null (resolver 미배선 대비).
        [Test]
        public void iconResolver_null이면_Icon은_null이고_NRE_없다()
        {
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(AllSeen(), Cfg(), null);

            Assert.AreEqual(6, list.Count);
            foreach (CodexCellData cell in list)
            {
                Assert.IsNull(cell.Icon);
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
            Assert.AreEqual(ExpectedLen, CodexCell.IconTintByLevel.Length);
            Assert.AreEqual(ExpectedLen, CodexCell.GlowOverlayAlphaByLevel.Length);
            Assert.AreEqual(ExpectedLen, CodexCell.ScaleByLevel.Length);
        }

        [Test]
        public void 세_매핑배열은_레벨에_따라_단조_비감소한다()
        {
            AssertMonotonicNonDecreasing(CodexCell.IconTintByLevel, "IconTint");
            AssertMonotonicNonDecreasing(CodexCell.GlowOverlayAlphaByLevel, "GlowAlpha");
            AssertMonotonicNonDecreasing(CodexCell.ScaleByLevel, "Scale");
        }

        //# Lv0 = 강화 없음 항등: 틴트 0(원본색), 발광 0(불투명 오버레이 없음), 스케일 1(원본 크기).
        [Test]
        public void Lv0은_강화없음_항등값이다()
        {
            Assert.AreEqual(0f, CodexCell.IconTintByLevel[0], 1e-4f, "Lv0 틴트=0(원본색)");
            Assert.AreEqual(0f, CodexCell.GlowOverlayAlphaByLevel[0], 1e-4f, "Lv0 발광α=0");
            Assert.AreEqual(1f, CodexCell.ScaleByLevel[0], 1e-4f, "Lv0 스케일=1(원본)");
        }

        //# Lv5 = 상한: 각 배열의 마지막 원소가 곧 최댓값(만렙이 가장 강한 표현).
        [Test]
        public void Lv5는_각_배열의_상한_최댓값이다()
        {
            AssertLastIsMax(CodexCell.IconTintByLevel, "IconTint");
            AssertLastIsMax(CodexCell.GlowOverlayAlphaByLevel, "GlowAlpha");
            AssertLastIsMax(CodexCell.ScaleByLevel, "Scale");
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

            Assert.AreEqual(CodexCell.ScaleByLevel[5], iconRect.localScale.x, 1e-4f, "Lv99 → Clamp→5 스케일 상한");
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

            Assert.AreEqual(CodexCell.ScaleByLevel[0], iconRect.localScale.x, 1e-4f, "음수 → Clamp→0 항등 스케일");
        }
    }
}
