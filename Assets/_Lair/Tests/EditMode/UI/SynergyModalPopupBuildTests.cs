using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.UI
{
    //# 시너지 모달 행 평탄화(BuildRows) 순수 로직 검증. VM/Prefab 없이 EditMode.
    public class SynergyModalPopupBuildTests
    {
        private static Func<EBuildAxis, int> Counts(int tank, int dps, int debuff, int swarm)
        {
            Dictionary<EBuildAxis, int> map = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, tank }, { EBuildAxis.Dps, dps },
                { EBuildAxis.Debuff, debuff }, { EBuildAxis.Swarm, swarm },
            };
            return a => map[a];
        }

        [Test]
        public void 활성_티어_0개면_빈_리스트()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(2, 0, 1, 0));
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void Tank5_Dps3_헤더와_효과행_수_검증()
        {
            //# Tank 5 → 헤더1 + 효과2(Tier1,2), Dps 3 → 헤더1 + 효과1(Tier1) = 5행
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(5, 3, 0, 0));
            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.AreEqual("TANK (5장)", rows[0].Label);
            Assert.AreEqual(SynergyModalCellData.Kind.Effect, rows[1].RowKind);
            Assert.IsTrue(rows[1].Label.StartsWith("Tier1"));
            Assert.IsTrue(rows[2].Label.StartsWith("Tier2"));
            Assert.AreEqual("DPS (3장)", rows[3].Label);
        }

        [Test]
        public void Tank7_이상이면_Tier3까지_3행()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(7, 0, 0, 0));
            //# 헤더1 + 효과3
            Assert.AreEqual(4, rows.Count);
            Assert.IsTrue(rows[3].Label.StartsWith("Tier3"));
        }

        [Test]
        public void 축_순서는_Tank_Dps_Debuff_Swarm()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(3, 3, 3, 3));
            //# 각 축 헤더1+효과1 = 8행, 헤더 라벨 순서 확인
            Assert.AreEqual("TANK (3장)", rows[0].Label);
            Assert.AreEqual("DPS (3장)", rows[2].Label);
            Assert.AreEqual("DEBUFF (3장)", rows[4].Label);
            Assert.AreEqual("SWARM (3장)", rows[6].Label);
        }

        [Test]
        public void TierDesc_12개_키_전부_채워짐()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7));
            foreach (SynergyModalCellData r in rows)
                if (r.RowKind == SynergyModalCellData.Kind.Effect)
                    Assert.IsFalse(r.Label.EndsWith("  "), $"빈 설명: {r.Label}");
        }
    }
}
