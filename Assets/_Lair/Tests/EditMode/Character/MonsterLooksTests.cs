using System.Collections.Generic;
using NUnit.Framework;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# 전투·쇼케이스 공유 외형 계산(village-monster-showcase §2, 게이트 T-S2) — 시드: gameplay-programmer. 정상(등급·티어 산출) + 엣지(편성 없음 → 기준 6종, 티어 0 은 로드 대상 제외).
    public class MonsterLooksTests
    {
        [Test]
        public void 등급_몬스터와_강화_레벨에서_기준_등급_티어를_계산한다()
        {
            EMonster[] formation = { EMonster.WispG3, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            Dictionary<EMonster, int> levels = new Dictionary<EMonster, int> { { EMonster.Wisp, 5 }, { EMonster.Hex, 2 } };

            MonsterLook[] looks = MonsterLooks.Compute(formation, levels);

            Assert.AreEqual(EMonster.Wisp, looks[0].Base);
            Assert.AreEqual(3, looks[0].Grade);
            Assert.AreEqual(3, looks[0].Tier, "Lv5 → T3");
            Assert.AreEqual(1, looks[3].Tier, "Lv2 → T1");
            Assert.AreEqual(0, looks[1].Tier, "표에 없으면 Lv0");
        }

        [Test]
        public void 편성이_없으면_기준_6종이고_티어0은_로드_대상에서_빠진다()
        {
            MonsterLook[] looks = MonsterLooks.Compute(null, new Dictionary<EMonster, int> { { EMonster.Reaper, 3 } });

            Assert.AreEqual(6, looks.Length);
            Assert.AreEqual(1, looks[2].Grade);
            List<(EMonster, int)> needs = MonsterLooks.TierNeeds(looks);
            Assert.AreEqual(1, needs.Count);
            Assert.AreEqual((EMonster.Reaper, 2), needs[0]);
        }

        [Test]
        public void 로딩_쇼케이스는_공개_후_6종_모두_군주_Lv5_T3다()
        {
            MonsterLook[] looks = MonsterLooks.LoadingShowcase(null, true);

            Assert.AreEqual(6, looks.Length);
            foreach (MonsterLook look in looks)
            {
                Assert.AreEqual(MonsterGrades.MaxGrade, look.Grade);
                Assert.AreEqual(MonsterGrades.Of(look.Base, MonsterGrades.MaxGrade), look.Monster);
                Assert.AreEqual(5, look.Level);
                Assert.AreEqual(3, look.Tier);
            }
        }

        [Test]
        public void 로딩_쇼케이스는_공개_전_군주_등급이되_Lv0_티어0이라_시트_로드가_없다()
        {
            MonsterLook[] looks = MonsterLooks.LoadingShowcase(null, false);

            Assert.AreEqual(EMonster.WispG5, looks[0].Monster);
            Assert.AreEqual(0, looks[0].Level);
            Assert.AreEqual(0, MonsterLooks.TierNeeds(looks).Count);
        }
    }
}
