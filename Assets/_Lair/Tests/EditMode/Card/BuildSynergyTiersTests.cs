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
    public class BuildSynergyTiersTests
    {
        private class FakeCtx : IBattleContext
        {
            public readonly List<(EMonster, EMonsterStatKind, float)> Buffs = new();
            public readonly List<float> SpawnerPeriods = new();
            public readonly List<int> OutputDeltas = new();
            public readonly List<IHeroAura> Auras = new();

            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
                => Buffs.Add((type, stat, multiplier));
            public void ScaleAllSpawnerPeriods(float mul) => SpawnerPeriods.Add(mul);
            public void IncrementAllSpawnerOutputs(int delta) => OutputDeltas.Add(delta);
            public void ScaleSpawnerPeriodForType(EMonster type, float mul) { }
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

        //# 캡 제거 리뉴얼 — 구 IncrementGlobalMonsterCap(6) → Wisp·Wraith HP ×1.4 등록 (기획서 §2).
        [Test]
        public void TankTier3_Wisp_Wraith_Hp_1점4_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier3().Apply(ctx);
            Assert.AreEqual(2, ctx.Buffs.Count);
            Assert.Contains((EMonster.Wisp,   EMonsterStatKind.Hp, 1.4f), ctx.Buffs);
            Assert.Contains((EMonster.Wraith, EMonsterStatKind.Hp, 1.4f), ctx.Buffs);
        }

        //# 회귀 (고가치) — 구 효과 잔존 차단: Tier3 가 스포너 출력/주기 표면을 건드리지 않는다.
        //# 구 캡 +6 이 IncrementGlobalMonsterCap → (인터페이스 삭제) 였고, 잘못 재구현 시 출력 표면을 칠 위험.
        [Test]
        public void TankTier3_스포너_표면_미호출_HP_표면만_사용()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier3().Apply(ctx);
            Assert.AreEqual(0, ctx.SpawnerPeriods.Count, "Tier3 는 주기 표면 미호출");
            Assert.AreEqual(0, ctx.OutputDeltas.Count, "Tier3 는 출력 표면 미호출 (구 캡 효과 잔존 없음)");
            Assert.AreEqual(0, ctx.Auras.Count, "Tier3 는 영웅 오라 미호출");
        }

        //# 엣지 — Tier3 는 Wisp·Wraith 외 종(Reaper 등)에 버프를 등록하지 않는다.
        [Test]
        public void TankTier3_Wisp_Wraith_외_종은_미등록()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier3().Apply(ctx);
            foreach ((EMonster type, EMonsterStatKind _, float _) in ctx.Buffs)
                Assert.IsTrue(type == EMonster.Wisp || type == EMonster.Wraith,
                    $"Tier3 가 대상 외 종 {type} 에 버프 등록 — Wisp·Wraith 만 허용");
        }

        //# 엣지 — Tier3 단독 반복 호출도 호출 표면이 일정 (멱등 호출 — 누적은 ctx 구현 책임).
        //# 본 FakeCtx 는 호출만 기록하므로 2회 Apply 면 4건. 누적 곱연산은 PlayMode 실 _typeModifiers 가 검증.
        [Test]
        public void TankTier3_2회_Apply_시_각_호출이_HP_표면으로_누적_기록()
        {
            FakeCtx ctx = new FakeCtx();
            new TankSynergyTier3().Apply(ctx);
            new TankSynergyTier3().Apply(ctx);
            Assert.AreEqual(4, ctx.Buffs.Count, "2회 Apply — Wisp·Wraith 각 2건씩 표면 호출");
            foreach ((EMonster _, EMonsterStatKind stat, float mul) in ctx.Buffs)
            {
                Assert.AreEqual(EMonsterStatKind.Hp, stat, "모든 호출이 HP 스탯");
                Assert.AreEqual(1.4f, mul, 0.0001f, "모든 호출 배율 ×1.4");
            }
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
