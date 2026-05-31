using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 2 Task 11 — 12개 IBuildSynergyTier 단위 테스트.
    //# 각 Tier 의 Apply 가 IBattleContext 의 어느 표면을 어떤 인자로 호출하는지만 검증.
    //# 본격 행동 검증 (시너지 발화 후 실제 몬스터 강화) 은 test-engineer 영역.
    public class BuildSynergyTiersTests
    {
        private class FakeCtx : IBattleContext
        {
            public readonly List<(EMonster, EMonsterStatKind, float)> Buffs = new();
            public readonly List<int> CapDeltas = new();
            public readonly List<float> SpawnerPeriods = new();
            public readonly List<int> OutputDeltas = new();
            public readonly List<IHeroAura> Auras = new();

            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
                => Buffs.Add((type, stat, multiplier));
            public void IncrementGlobalMonsterCap(int delta) => CapDeltas.Add(delta);
            public void ScaleAllSpawnerPeriods(float mul) => SpawnerPeriods.Add(mul);
            public void IncrementAllSpawnerOutputs(int delta) => OutputDeltas.Add(delta);
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) => Auras.Add(aura);

            //# no-op stubs
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public Transform GetHeroTransform() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public float DeltaTime => 0f;
        }

        //# ===== Tank =====
        [Test]
        public void TankTier1_Wisp_Wraith_Hp_1점3_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier1().Apply(ctx);
            Assert.AreEqual(2, ctx.Buffs.Count);
            Assert.Contains((EMonster.Wisp,   EMonsterStatKind.Hp, 1.3f), ctx.Buffs);
            Assert.Contains((EMonster.Wraith, EMonsterStatKind.Hp, 1.3f), ctx.Buffs);
        }

        [Test]
        public void TankTier2_Wisp_Wraith_Power_1점2_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier2().Apply(ctx);
            Assert.AreEqual(2, ctx.Buffs.Count);
            Assert.Contains((EMonster.Wisp,   EMonsterStatKind.Power, 1.2f), ctx.Buffs);
            Assert.Contains((EMonster.Wraith, EMonsterStatKind.Power, 1.2f), ctx.Buffs);
        }

        [Test]
        public void TankTier3_IncrementGlobalMonsterCap_6_호출()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier3().Apply(ctx);
            Assert.AreEqual(1, ctx.CapDeltas.Count);
            Assert.AreEqual(6, ctx.CapDeltas[0]);
        }

        //# ===== Dps =====
        [Test]
        public void DpsTier1_Reaper_Hex_Power_1점3_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new DpsSynergyTier1().Apply(ctx);
            Assert.Contains((EMonster.Reaper, EMonsterStatKind.Power, 1.3f), ctx.Buffs);
            Assert.Contains((EMonster.Hex,    EMonsterStatKind.Power, 1.3f), ctx.Buffs);
        }

        [Test]
        public void DpsTier2_Reaper_Hex_Cooldown_0점8_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new DpsSynergyTier2().Apply(ctx);
            Assert.Contains((EMonster.Reaper, EMonsterStatKind.Cooldown, 0.8f), ctx.Buffs);
            Assert.Contains((EMonster.Hex,    EMonsterStatKind.Cooldown, 0.8f), ctx.Buffs);
        }

        [Test]
        public void DpsTier3_Reaper_Hex_Range_1점3_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new DpsSynergyTier3().Apply(ctx);
            Assert.Contains((EMonster.Reaper, EMonsterStatKind.Range, 1.3f), ctx.Buffs);
            Assert.Contains((EMonster.Hex,    EMonsterStatKind.Range, 1.3f), ctx.Buffs);
        }

        //# ===== Debuff =====
        [Test]
        public void DebuffTier1_Plague_SlowFactor_0점8_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new DebuffSynergyTier1().Apply(ctx);
            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual((EMonster.Plague, EMonsterStatKind.SlowFactor, 0.8f), ctx.Buffs[0]);
        }

        //# Debuff Tier2 — 영웅 transform 미존재 시 no-op (자체 검증 엣지). 본격 시너지 적용 검증은 test-engineer.
        [Test]
        public void DebuffTier2_Hero_없으면_noop()
        {
            FakeCtx ctx = new FakeCtx();
            Assert.DoesNotThrow(() => new DebuffSynergyTier2().Apply(ctx));
            Assert.AreEqual(0, ctx.Auras.Count);
        }

        //# Debuff Tier3 — Hero mover 미존재 시 no-op.
        [Test]
        public void DebuffTier3_Hero_mover_없으면_noop()
        {
            FakeCtx ctx = new FakeCtx();
            Assert.DoesNotThrow(() => new DebuffSynergyTier3().Apply(ctx));
            Assert.AreEqual(0, ctx.Auras.Count);
        }

        //# ===== Swarm =====
        [Test]
        public void SwarmTier1_Phantom_Wisp_MoveSpeed_1점3_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new SwarmSynergyTier1().Apply(ctx);
            Assert.Contains((EMonster.Phantom, EMonsterStatKind.MoveSpeed, 1.3f), ctx.Buffs);
            Assert.Contains((EMonster.Wisp,    EMonsterStatKind.MoveSpeed, 1.3f), ctx.Buffs);
        }

        [Test]
        public void SwarmTier2_ScaleAllSpawnerPeriods_0점85_호출()
        {
            FakeCtx ctx = new FakeCtx();
            new SwarmSynergyTier2().Apply(ctx);
            Assert.AreEqual(1, ctx.SpawnerPeriods.Count);
            Assert.AreEqual(0.85f, ctx.SpawnerPeriods[0], 0.0001f);
        }

        [Test]
        public void SwarmTier3_IncrementAllSpawnerOutputs_1_호출()
        {
            FakeCtx ctx = new FakeCtx();
            new SwarmSynergyTier3().Apply(ctx);
            Assert.AreEqual(1, ctx.OutputDeltas.Count);
            Assert.AreEqual(1, ctx.OutputDeltas[0]);
        }
    }
}
