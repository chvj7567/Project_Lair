using System.Collections.Generic;
using Lair.Data;
using Lair.EditorTools;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# Meta Editor 윈도우의 미리보기/검증 계산 자체 테스트 — 기획서 village-meta-hub.md §3.5 가격표 기준.
    //# 본격 엣지/회귀 스위트는 test-engineer 영역 (정상 1 + 엣지 1 만 박제).
    public class MetaEditorCalcTests
    {
        [Test]
        public void 가격표_미리보기가_기획서_3_5절_BasePrice80_행과_일치한다()
        {
            ShopItemDef def = new ShopItemDef
            {
                Id = "MonsterHpUp",
                BasePrice = 80,
                PriceGrowth = 1.6f,
                MaxLevel = 5,
            };

            int[] rows = MetaEditorCalc.PriceRows(def);

            CollectionAssert.AreEqual(new int[] { 80, 128, 204, 327, 524 }, rows,
                "레벨별 가격 = floor(BasePrice × PriceGrowth^Lv) — ShopService.PriceOf 와 동일해야 함");
            Assert.AreEqual(1263, MetaEditorCalc.CumulativeMaxCost(def), "만렙 누적 비용 (§3.5)");
        }

        [Test]
        public void 공백_Id는_중복_검출에서_제외되고_비공백_중복만_보고된다()
        {
            List<string> ids = new List<string> { "A", "A", "", "  ", null, "B" };

            List<string> duplicates = MetaEditorCalc.FindDuplicateIds(ids);

            CollectionAssert.AreEqual(new List<string> { "A" }, duplicates,
                "공백/빈 Id 는 '공백 경고' 전용 — 중복 목록에 섞이면 안 됨");
        }

        [Test]
        public void 앞뒤_공백만_다른_Id는_Trim_비교로_중복_검출된다()
        {
            //# Id 는 MetaProfile 세이브 키 — "A" 와 "A " 는 사실상 같은 키 의도라 중복으로 보고.
            List<string> ids = new List<string> { "A", "A " };

            List<string> duplicates = MetaEditorCalc.FindDuplicateIds(ids);

            CollectionAssert.AreEqual(new List<string> { "A" }, duplicates,
                "Trim 비교 — 공백 패딩 차이는 중복으로 검출돼야 함");
        }

        [Test]
        public void 역순_레벨은_최고치_기준으로_뒤따르는_항목_전부_검출된다()
        {
            //# [5,3,4] — 3·4 둘 다 running max(5)보다 낮음. 인접 비교면 4 를 놓친다.
            List<LordRewardDef> rewards = new List<LordRewardDef>
            {
                new LordRewardDef { Level = 5 },
                new LordRewardDef { Level = 3 },
                new LordRewardDef { Level = 4 },
            };

            List<string> warnings = MetaEditorCalc.LordLevelWarnings(rewards);

            Assert.AreEqual(2, warnings.Count,
                "running max 비교 — #2(Lv3)·#3(Lv4) 둘 다 역순 경고");
        }
    }
}
