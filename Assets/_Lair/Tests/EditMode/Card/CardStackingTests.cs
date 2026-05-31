using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — Layer 2 카드 중첩 누적 검증.
    //# 기획서 §7.1·§7.2: 같은 카드 K번 픽 시 곱연산/가산/지속시간 누적이 IBattleContext 표면을 K번 재호출.
    //#  - WispHpBoost 5픽 → RegisterMonsterTypeBuff(Wisp, Hp, 1.5) 5회 호출 (곱연산 누적)
    //#  - SpawnerHaste 다중 픽 → ScaleAllSpawnerPeriods(0.8) K회 호출 (곱연산 누적)
    //#  - HeroAttackDownEffect 다중 픽 → 같은 factor → ShouldStackAsNew=false → 첫 픽만 적용
    //#  - Fear / SpawnerHaste 의 호출 카운트는 효과 클래스가 표면을 정확히 K번 호출하는지만 확인.
    //# 본 단위 테스트는 *카드 효과 클래스가 IBattleContext 표면을 정확히 K회 호출* 하는지를 검증.
    //# 실제 곱연산 누적(예: 1.5×1.5=2.25)은 BattleController 의 dict 누적 책임이며
    //# ContinuousSpawnIntegrationTest.ApplyMonsterStats_강화_2픽_곱연산_누적 이 이미 회귀로 박제.
    public class CardStackingTests
    {
        //# ===== 1. WispHpBoost 5픽 → RegisterMonsterTypeBuff 5회 호출 (Layer 2 곱연산 누적의 표면 검증) =====

        [Test]
        public void WispHpBoost_5번_Apply_RegisterMonsterTypeBuff_5회_호출_누적()
        {
            FakeCtx ctx = new FakeCtx();
            WispHpBoostEffect effect = new WispHpBoostEffect();

            for (int i = 0; i < 5; ++i) effect.Apply(ctx);

            Assert.AreEqual(5, ctx.Buffs.Count, "5픽 → 표면 5회 호출 (곱연산 누적은 BattleController dict 책임)");
            foreach ((EMonster m, EMonsterStatKind s, float mul) in ctx.Buffs)
            {
                Assert.AreEqual(EMonster.Wisp, m, "Wisp 종");
                Assert.AreEqual(EMonsterStatKind.Hp, s, "Hp 스탯");
                Assert.AreEqual(1.5f, mul, 0.0001f, "_hpMul = 1.5 — 매 픽 동일 인자");
            }
        }

        //# ===== 2. SpawnerHaste 다중 픽 → ScaleAllSpawnerPeriods 곱연산 누적 (호출 카운트) =====

        //# 기획서 §9.6: SpawnerHaste 3픽 곱연산 = ×0.8³ = ×0.512. 본 테스트는 *효과 클래스가
        //# 표면을 정확히 3회 호출* 하는지 검증 (실제 곱연산은 BattleController._spawners 측 책임).
        [Test]
        public void SpawnerHaste_3번_Apply_ScaleAllSpawnerPeriods_3회_호출()
        {
            FakeCtx ctx = new FakeCtx();
            SpawnerHasteEffect effect = new SpawnerHasteEffect();

            for (int i = 0; i < 3; ++i) effect.Apply(ctx);

            Assert.AreEqual(3, ctx.SpawnerPeriods.Count, "3픽 → 3회 호출");
            foreach (float mul in ctx.SpawnerPeriods)
                Assert.AreEqual(0.8f, mul, 0.0001f, "_periodMul = 0.8 — 매 픽 동일 인자");
        }

        //# ===== 3. HeroAttackDown 다중 픽 — 같은 factor 재부착 시 첫 인스턴스만 적용 =====

        //# HeroAttackDownEffect 가 호출하는 Aura factor 기본값 (HeroAttackDownAura(_attacker) → 0.75 기본).
        //# Effect 자체는 매 Apply 마다 ApplyHeroAura 호출 — 표면 호출 카운트는 K회.
        //# 다만 HeroAuraRunner 가 같은 factor 의 IDistinctHeroAura 를 ShouldStackAsNew=false 로 가드하므로
        //# 같은 카드 다중 픽 시 PowerScale 은 1회만 곱연산 (단일 인스턴스 정책).
        //# B3 회귀 EffectsRenewal2026Tests.HeroAttackDown_같은_factor_재부착은_PowerScale_재곱연산_안됨 의 보강.
        [Test]
        public void HeroAttackDown_같은카드_5번_픽_같은_factor_PowerScale_1회만_곱연산()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                heroGo.AddComponent<Health>();
                MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
                atk.PowerScale = 1f;
                HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

                //# HeroAttackDownAura 기본 factor=0.75 동일 인스턴스를 5번 Attach.
                for (int i = 0; i < 5; ++i)
                    runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);

                Assert.AreEqual(0.75f, atk.PowerScale, 0.0001f,
                    "같은 factor 다중 부착 — PowerScale 1회만 곱연산 (× 0.75 ¹)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# ===== 4. HeroAttackDownAura — 카드 1픽 + Tier2 (다른 factor) 의 곱연산 누적 =====

        //# 기획서 §4.5 누적 정책: 카드 픽 (×0.75) + Debuff Tier2 (×0.85) + 카드 2픽 누적 가능 시 ×0.75 × ×0.85.
        //# 같은 factor 재부착은 1회만 적용되므로 *카드 픽 1회 (×0.75) + Tier2 (×0.85) = ×0.6375* 가
        //# 본 시스템의 최대 누적. 카드 픽 2회 시 두 번째 ×0.75 는 첫 ×0.75 와 ShouldStackAsNew=false 가드.
        [Test]
        public void HeroAttackDown_카드픽_2회_더하기_Tier2_1회_누적_PowerScale_0점6375()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                heroGo.AddComponent<Health>();
                MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
                atk.PowerScale = 1f;
                HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

                runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
                runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);   //# 가드로 무시
                runner.Attach(new HeroAttackDownAura(atk, 0.85f), duration: -1f);   //# Tier2 — 신규 인스턴스

                Assert.AreEqual(0.75f * 0.85f, atk.PowerScale, 0.0001f,
                    "카드 2픽 (1회만 실효) × Tier2 = ×0.75 × ×0.85 = ×0.6375");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# ===== 5. 지속시간 누적 정책 — Fear / IronWill 효과 진행 중 재픽 시 잔여+duration =====

        //# Fear 효과는 영웅 AutoCombatAI 가 있어야 작동 — 본 단위 테스트에선 무관, 표면 호출 카운트만 검증.
        //# 기획서 §7.2: "지속시간 누적 — 효과 진행 중 재픽 시 잔여 + duration".
        //# Effect.Apply 가 매번 ApplyHeroAura 호출 = K픽 시 K번 호출. 실제 잔여+duration 누적은
        //# HeroAuraRunner.Attach 의 "existing.Remain += duration" 경로가 담당.
        [Test]
        public void Fear_5번_Apply_ApplyHeroAura_호출표면_보호()
        {
            //# Fear 는 hero AutoCombatAI 가 없으면 no-op (early return).
            //# 본 케이스는 effect 의 early-return 가드가 정상 동작하는지 확인 (예외 없음).
            FakeCtx ctx = new FakeCtx();
            FearEffect effect = new FearEffect();

            for (int i = 0; i < 5; ++i)
                Assert.DoesNotThrow(() => effect.Apply(ctx), $"Fear Hero 없는 컨텍스트 — 예외 없음 (i={i})");

            Assert.AreEqual(0, ctx.Auras.Count, "Hero 없음 → ApplyHeroAura 호출 안 됨 (early return)");
        }

        //# ===== 6. 신규 효과 5개 (Layer 2 호출 표면 회귀) =====

        //# WallOfWisps 2픽 → SpawnMonster(Wisp) 8회 호출 (가산 누적, 캡 truncate 는 SpawnMonster 내부).
        [Test]
        public void WallOfWisps_2번_Apply_SpawnMonster_8회_호출_가산누적()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;
            try
            {
                WallOfWispsEffect effect = new WallOfWispsEffect();
                effect.Apply(ctx);
                effect.Apply(ctx);

                Assert.AreEqual(8, ctx.Spawned.Count, "2픽 → 4+4 = 8회 SpawnMonster 호출 (가산 누적)");
                foreach (EMonster m in ctx.Spawned) Assert.AreEqual(EMonster.Wisp, m);
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# SwarmRush 2픽 → SpawnMonster(Phantom) 12회 호출.
        [Test]
        public void SwarmRush_2번_Apply_SpawnMonster_12회_호출_가산누적()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;
            try
            {
                SwarmRushEffect effect = new SwarmRushEffect();
                effect.Apply(ctx);
                effect.Apply(ctx);

                Assert.AreEqual(12, ctx.Spawned.Count, "2픽 → 6+6 = 12회 호출 (가산 누적)");
                foreach (EMonster m in ctx.Spawned) Assert.AreEqual(EMonster.Phantom, m);
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# GuardianRage 다중 픽 → AddMonsterBuff 다중 호출 (지속시간 누적은 MonsterBuffService.AddBuff 의 Max 정책).
        [Test]
        public void GuardianRage_3번_Apply_AddMonsterBuff_3회_호출()
        {
            FakeCtx ctx = new FakeCtx();
            GuardianRageEffect effect = new GuardianRageEffect();

            for (int i = 0; i < 3; ++i) effect.Apply(ctx);

            Assert.AreEqual(3, ctx.MonsterBuffs.Count, "3픽 → 3회 호출");
            foreach (EMonsterBuff b in ctx.MonsterBuffs)
                Assert.AreEqual(EMonsterBuff.GuardianRage, b);
        }

        //# ===== Fake =====

        private class FakeCtx : IBattleContext
        {
            public readonly List<(EMonster, EMonsterStatKind, float)> Buffs = new();
            public readonly List<EMonster> Spawned = new();
            public readonly List<EMonsterBuff> MonsterBuffs = new();
            public readonly List<float> SpawnerPeriods = new();
            public readonly List<IHeroAura> Auras = new();
            public Transform HeroTransform;

            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
                => Buffs.Add((type, stat, multiplier));
            public void SpawnMonster(EMonster key, Vector3 nearHero) => Spawned.Add(key);
            public void AddMonsterBuff(EMonsterBuff type, float duration) => MonsterBuffs.Add(type);
            public void ScaleAllSpawnerPeriods(float mul) => SpawnerPeriods.Add(mul);
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) => Auras.Add(aura);
            public Transform GetHeroTransform() => HeroTransform;

            //# no-op stubs
            public float DeltaTime => 0f;
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public IMover GetHeroMover() => null;
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void IncrementGlobalMonsterCap(int delta) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
        }
    }
}
