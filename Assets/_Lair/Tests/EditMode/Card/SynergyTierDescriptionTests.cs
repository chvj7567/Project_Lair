using Lair.Card;
using NUnit.Framework;

namespace Lair.Tests.Card
{
    //# 시너지 티어 설명 계약 스모크 — 각 tier 가 spec §6 표대로 DescriptionStringId·DescriptionArgs 를
    //# 자기 const 에서 조립하는지 검증. 파생 표기(Dps2 공속%, Debuff3 %/s)·불필요 0 제거 포함.
    public class SynergyTierDescriptionTests
    {
        //# spec §6 — (tier 인스턴스, 기대 id, 기대 arg[0]).
        [TestCase(200, "1.3")]  //# Tank1 HpMul 1.3
        public void Tank1_설명계약(int id, string arg) => Assert(new TankSynergyTier1(), id, arg);
        [TestCase(201, "1.2")]  //# Tank2 PowerMul 1.2
        public void Tank2_설명계약(int id, string arg) => Assert(new TankSynergyTier2(), id, arg);
        [TestCase(202, "1.4")]  //# Tank3 HpMul 1.4 (구 "필드 캡 +6" stale 표시 교정)
        public void Tank3_설명계약(int id, string arg) => Assert(new TankSynergyTier3(), id, arg);
        [TestCase(203, "1.3")]  //# Dps1 PowerMul 1.3
        public void Dps1_설명계약(int id, string arg) => Assert(new DpsSynergyTier1(), id, arg);
        [TestCase(204, "25")]   //# Dps2 CooldownMul 0.8 → 공속 +25%
        public void Dps2_설명계약(int id, string arg) => Assert(new DpsSynergyTier2(), id, arg);
        [TestCase(205, "1.3")]  //# Dps3 RangeMul 1.3
        public void Dps3_설명계약(int id, string arg) => Assert(new DpsSynergyTier3(), id, arg);
        [TestCase(206, "0.8")]  //# Debuff1 SlowMul 0.8
        public void Debuff1_설명계약(int id, string arg) => Assert(new DebuffSynergyTier1(), id, arg);
        [TestCase(207, "0.85")] //# Debuff2 Factor 0.85
        public void Debuff2_설명계약(int id, string arg) => Assert(new DebuffSynergyTier2(), id, arg);
        [TestCase(208, "1")]    //# Debuff3 Ratio 0.01 → -1%/s
        public void Debuff3_설명계약(int id, string arg) => Assert(new DebuffSynergyTier3(), id, arg);
        [TestCase(209, "1.3")]  //# Swarm1 MoveMul 1.3
        public void Swarm1_설명계약(int id, string arg) => Assert(new SwarmSynergyTier1(), id, arg);
        [TestCase(210, "0.85")] //# Swarm2 PeriodMul 0.85
        public void Swarm2_설명계약(int id, string arg) => Assert(new SwarmSynergyTier2(), id, arg);
        [TestCase(211, "1")]    //# Swarm3 OutputDelta 1
        public void Swarm3_설명계약(int id, string arg) => Assert(new SwarmSynergyTier3(), id, arg);

        private static void Assert(IBuildSynergyTier tier, int expectedId, string expectedArg)
        {
            NUnit.Framework.Assert.AreEqual(expectedId, tier.DescriptionStringId, "DescriptionStringId");
            NUnit.Framework.Assert.IsNotNull(tier.DescriptionArgs, "DescriptionArgs 는 null 아님");
            NUnit.Framework.Assert.AreEqual(1, tier.DescriptionArgs.Length, "arg 1개");
            NUnit.Framework.Assert.AreEqual(expectedArg, tier.DescriptionArgs[0], "DescriptionArgs[0]");
        }

        //# 엣지 — 12 tier 의 DescriptionStringId 가 200~211 로 전부 유일 (중복 id 회귀 방지).
        [Test]
        public void id는_200_211_범위에서_전부_유일()
        {
            IBuildSynergyTier[] tiers =
            {
                new TankSynergyTier1(), new TankSynergyTier2(), new TankSynergyTier3(),
                new DpsSynergyTier1(),  new DpsSynergyTier2(),  new DpsSynergyTier3(),
                new DebuffSynergyTier1(), new DebuffSynergyTier2(), new DebuffSynergyTier3(),
                new SwarmSynergyTier1(), new SwarmSynergyTier2(), new SwarmSynergyTier3(),
            };
            System.Collections.Generic.HashSet<int> seen = new System.Collections.Generic.HashSet<int>();
            foreach (IBuildSynergyTier t in tiers)
            {
                NUnit.Framework.Assert.GreaterOrEqual(t.DescriptionStringId, 200);
                NUnit.Framework.Assert.LessOrEqual(t.DescriptionStringId, 211);
                NUnit.Framework.Assert.IsTrue(seen.Add(t.DescriptionStringId),
                    $"id {t.DescriptionStringId} 중복");
            }
            NUnit.Framework.Assert.AreEqual(12, seen.Count, "12개 유일 id");
        }
    }
}
