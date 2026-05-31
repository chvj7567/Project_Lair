using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 1 Task 2·3 — BuildSynergyService 단위 테스트.
    //# Task 2: 카운트 누적 / Reset.
    //# Task 3: Tier 임계(3장) 도달 시 1회 Apply 발화 + 재발화 없음.
    public class BuildSynergyServiceTests
    {
        [Test]
        public void RegisterPick_같은축_카운트_누적()
        {
            BuildSynergyService sut = new BuildSynergyService();
            sut.RegisterPick(EBuildAxis.Tank);
            sut.RegisterPick(EBuildAxis.Tank);
            sut.RegisterPick(EBuildAxis.Dps);

            Assert.AreEqual(2, sut.GetCount(EBuildAxis.Tank));
            Assert.AreEqual(1, sut.GetCount(EBuildAxis.Dps));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Debuff));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Swarm));
        }

        [Test]
        public void Reset_모든_축_카운트_0()
        {
            BuildSynergyService sut = new BuildSynergyService();
            sut.RegisterPick(EBuildAxis.Tank);
            sut.RegisterPick(EBuildAxis.Dps);
            sut.RegisterPick(EBuildAxis.Swarm);

            sut.Reset();

            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Tank));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Dps));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Debuff));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Swarm));
        }

        [Test]
        public void RegisterPick_3장_도달_시_Tier1_1회_Apply()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankTier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, threshold: 3, tier: tankTier1);

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(0, tankTier1.AppliedCount, "2장 — 아직 임계 미달");

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tankTier1.AppliedCount, "3장 — 임계 도달, 1회 발화");

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tankTier1.AppliedCount, "4장 — 같은 임계 재발화 없음");
        }

        [Test]
        public void RegisterPick_다른_축_Tier_침범_없음()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankTier1 = new FakeTier();
            FakeTier dpsTier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tankTier1);
            sut.BindTier(EBuildAxis.Dps, 3, dpsTier1);

            //# Dps 만 3장 픽 — Tank Tier 영향 없음.
            sut.RegisterPick(EBuildAxis.Dps, ctx);
            sut.RegisterPick(EBuildAxis.Dps, ctx);
            sut.RegisterPick(EBuildAxis.Dps, ctx);

            Assert.AreEqual(1, dpsTier1.AppliedCount);
            Assert.AreEqual(0, tankTier1.AppliedCount);
        }

        //# Tier 효과 Apply 호출만 검증하는 더미.
        private class FakeTier : IBuildSynergyTier
        {
            public int AppliedCount { get; private set; }
            public void Apply(IBattleContext ctx)
            {
                ++AppliedCount;
            }
        }

        //# IBattleContext stub — 모든 메서드 빈 구현. 본 테스트는 ctx 전달 자체만 확인.
        private class FakeBattleContext : IBattleContext
        {
            public float DeltaTime => 0f;
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public Transform GetHeroTransform() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void IncrementGlobalMonsterCap(int delta) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
        }
    }
}
