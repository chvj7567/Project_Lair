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
    }
}
