using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 2 — 신규/리뉴얼 카드 효과 단위 테스트.
    //# gameplay-programmer 자체 검증 수준 (정상 + 엣지 1). 본격 스위트는 test-engineer.
    //# Phase 2 BLOCKER 회귀 (B1·B2·B3) 통합 — GuardianRage HP×2 + Slow SwarmSpeed + HeroAttackDown 누적.
    public class EffectsRenewal2026Tests
    {
        //# 최소 stub — 본 테스트 사용 표면만 기록.
        private class FakeCtx : IBattleContext
        {
            public readonly List<(EMonster, EMonsterStatKind, float)> Buffs = new();
            public readonly List<EMonster> Spawned = new();
            public readonly List<EMonsterBuff> MonsterBuffs = new();
            public readonly List<float> SpawnerPeriods = new();
            public readonly List<int> CapDeltas = new();
            public readonly List<int> OutputDeltas = new();
            public Transform HeroTransform;

            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
                => Buffs.Add((type, stat, multiplier));
            public void SpawnMonster(EMonster key, Vector3 nearHero) => Spawned.Add(key);
            public void AddMonsterBuff(EMonsterBuff type, float duration) => MonsterBuffs.Add(type);
            public void ScaleAllSpawnerPeriods(float mul) => SpawnerPeriods.Add(mul);
            public void IncrementGlobalMonsterCap(int delta) => CapDeltas.Add(delta);
            public void IncrementAllSpawnerOutputs(int delta) => OutputDeltas.Add(delta);
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public Transform GetHeroTransform() => HeroTransform;

            //# no-op stubs
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public IMover GetHeroMover() => null;
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public float DeltaTime => 0f;
        }

        //# WraithDamageBoost (리뉴얼) — Power 가 아닌 Hp 등록 (기획서 §3.1 #2).
        [Test]
        public void WraithDamageBoost_Apply_Wraith_Hp_1점5_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new WraithDamageBoostEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual((EMonster.Wraith, EMonsterStatKind.Hp, 1.5f), ctx.Buffs[0]);
        }

        //# GuardianRage (Berserk 자리 리뉴얼) — EMonsterBuff.GuardianRage 등록.
        [Test]
        public void GuardianRage_Apply_GuardianRage_buff_등록()
        {
            FakeCtx ctx = new FakeCtx();
            new GuardianRageEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.MonsterBuffs.Count);
            Assert.AreEqual(EMonsterBuff.GuardianRage, ctx.MonsterBuffs[0]);
        }

        //# WallOfWisps — Wisp 4마리 즉시 소환.
        [Test]
        public void WallOfWisps_Apply_Wisp_4마리_소환()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;

            new WallOfWispsEffect().Apply(ctx);

            Assert.AreEqual(4, ctx.Spawned.Count);
            foreach (EMonster m in ctx.Spawned)
                Assert.AreEqual(EMonster.Wisp, m);

            Object.DestroyImmediate(heroGo);
        }

        //# WallOfWisps 엣지 — Hero 가 없으면 no-op (예외 없음).
        [Test]
        public void WallOfWisps_Hero_없으면_noop()
        {
            FakeCtx ctx = new FakeCtx();

            Assert.DoesNotThrow(() => new WallOfWispsEffect().Apply(ctx));
            Assert.AreEqual(0, ctx.Spawned.Count);
        }

        //# SwarmRush — Phantom 6마리 즉시 소환.
        [Test]
        public void SwarmRush_Apply_Phantom_6마리_소환()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;

            new SwarmRushEffect().Apply(ctx);

            Assert.AreEqual(6, ctx.Spawned.Count);
            foreach (EMonster m in ctx.Spawned)
                Assert.AreEqual(EMonster.Phantom, m);

            Object.DestroyImmediate(heroGo);
        }

        //# SpawnerHaste — ScaleAllSpawnerPeriods(0.8) 1회 호출.
        [Test]
        public void SpawnerHaste_Apply_ScaleAllSpawnerPeriods_0점8_호출()
        {
            FakeCtx ctx = new FakeCtx();
            new SpawnerHasteEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.SpawnerPeriods.Count);
            Assert.AreEqual(0.8f, ctx.SpawnerPeriods[0], 0.0001f);
        }

        //# ===== Phase 2 BLOCKER 회귀 =====

        //# 테스트 격리 — MonsterBuffService Tick 이 CharacterRegistry.Monsters 를 순회하므로 매 테스트 후 정리.
        [TearDown]
        public void CleanRegistry()
        {
            //# 등록된 모든 몬스터 GameObject 파괴 → OnDisable 의 UnregisterMonster 가 정리.
            //# 일부 테스트는 직접 UnregisterMonster + DestroyImmediate 하지만 안전망.
            for (int i = CharacterRegistry.Monsters.Count - 1; i >= 0; --i)
            {
                CharacterRegistry.Entry e = CharacterRegistry.Monsters[i];
                if (e?.Transform != null) Object.DestroyImmediate(e.Transform.gameObject);
            }
            CharacterRegistry.Monsters.Clear();
        }

        //# [B1] GuardianRage Tick 시 적용 종 (Wisp/Wraith) 만 HP×2 + 받는 데미지 ×0.5.
        //# 받는 데미지 ×0.5 확인 — DamageTakenScale 곱연산.
        //# HP ×2 확인 — HpMaxScale 곱연산 + Current 비율 보존.
        [Test]
        public void GuardianRage_Tick_Wisp만_HP_2배_받는데미지_0점5_적용()
        {
            //# 1) Wisp Health 준비 (base max 100, current 100).
            GameObject wisp = new GameObject("wisp");
            wisp.AddComponent<MonsterTag>().Configure(EMonster.Wisp);
            Health hpWisp = wisp.AddComponent<Health>();
            hpWisp.SetMax(100, resetCurrent: true);
            CharacterRegistry.RegisterMonster(wisp.transform, hpWisp);

            //# 2) Reaper Health 준비 — GuardianRage 적용 종 (Wisp/Wraith) 에 미포함 → 변화 없어야 함.
            GameObject reaper = new GameObject("reaper");
            reaper.AddComponent<MonsterTag>().Configure(EMonster.Reaper);
            Health hpReaper = reaper.AddComponent<Health>();
            hpReaper.SetMax(100, resetCurrent: true);
            CharacterRegistry.RegisterMonster(reaper.transform, hpReaper);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            //# Wisp — HP×2, 받는 데미지 ×0.5 모두 적용.
            Assert.AreEqual(2f, hpWisp.HpMaxScale, 0.0001f, "Wisp HpMaxScale = 2");
            Assert.AreEqual(0.5f, hpWisp.DamageTakenScale, 0.0001f, "Wisp DamageTakenScale = 0.5");
            Assert.AreEqual(200, hpWisp.EffectiveMaxHp, "Wisp EffectiveMaxHp = base 100 × 2");
            Assert.AreEqual(200, hpWisp.Current, "Wisp Current 도 비율 보존으로 2배 (100 → 200)");
            //# Reaper — 적용 종 외 → 1.0 유지.
            Assert.AreEqual(1f, hpReaper.HpMaxScale, 0.0001f, "Reaper HpMaxScale 유지 (적용 종 외)");
            Assert.AreEqual(1f, hpReaper.DamageTakenScale, 0.0001f, "Reaper DamageTakenScale 유지");
        }

        //# [B1] 엣지 — GuardianRage 활성 → 비활성 (Tick 후 만료) 시 HpMaxScale 1.0 복원 + Current 비율 보존으로 축소.
        [Test]
        public void GuardianRage_만료_후_HpMaxScale_복원_Current_축소()
        {
            GameObject wisp = new GameObject("wisp");
            wisp.AddComponent<MonsterTag>().Configure(EMonster.Wisp);
            Health hp = wisp.AddComponent<Health>();
            hp.SetMax(100, resetCurrent: true);
            CharacterRegistry.RegisterMonster(wisp.transform, hp);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 0.01f);
            svc.Tick(0.005f);
            Assert.AreEqual(2f, hp.HpMaxScale, 0.0001f, "Tick 1: 활성 → 2배");
            Assert.AreEqual(200, hp.Current, "Tick 1: Current 2배");

            //# 만료 발생할 dt 로 Tick → buff 제거 → HpMaxScale 1.0 복원 → Current 절반.
            svc.Tick(0.02f);
            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "Tick 2: 만료 → 1.0 복원");
            Assert.AreEqual(100, hp.Current, "Tick 2: Current 도 비율 복원 (200 → 100)");
        }

        //# [B2] Slow 카드 Apply → SwarmSpeed buff 활성 후 Tick → 모든 몬스터 SpeedScale=1.3.
        [Test]
        public void Slow_Apply_후_Tick_모든_몬스터_SpeedScale_1점3()
        {
            //# Wisp + Reaper — SwarmSpeed 는 적용 종 한정 없음 (전체) → 둘 다 1.3.
            GameObject wisp = new GameObject("wisp");
            wisp.AddComponent<MonsterTag>().Configure(EMonster.Wisp);
            SimpleMover mvWisp = wisp.AddComponent<SimpleMover>();
            CharacterRegistry.RegisterMonster(wisp.transform, wisp.AddComponent<Health>());

            GameObject reaper = new GameObject("reaper");
            reaper.AddComponent<MonsterTag>().Configure(EMonster.Reaper);
            SimpleMover mvReaper = reaper.AddComponent<SimpleMover>();
            CharacterRegistry.RegisterMonster(reaper.transform, reaper.AddComponent<Health>());

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.SwarmSpeed, 10f);
            svc.Tick(0.016f);

            Assert.AreEqual(1.3f, mvWisp.SpeedScale, 0.0001f, "Wisp SpeedScale = 1.3 (전체 적용)");
            Assert.AreEqual(1.3f, mvReaper.SpeedScale, 0.0001f, "Reaper SpeedScale = 1.3 (전체 적용)");
        }

        //# [B2] 엣지 — SwarmSpeed 만료 후 Tick → SpeedScale 1.0 복원.
        [Test]
        public void Slow_SwarmSpeed_만료_후_SpeedScale_1점0_복원()
        {
            GameObject wisp = new GameObject("wisp");
            wisp.AddComponent<MonsterTag>().Configure(EMonster.Wisp);
            SimpleMover mv = wisp.AddComponent<SimpleMover>();
            CharacterRegistry.RegisterMonster(wisp.transform, wisp.AddComponent<Health>());

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.SwarmSpeed, 0.01f);
            svc.Tick(0.005f);
            Assert.AreEqual(1.3f, mv.SpeedScale, 0.0001f, "Tick 1: 활성 → 1.3");

            svc.Tick(0.02f);
            Assert.AreEqual(1f, mv.SpeedScale, 0.0001f, "Tick 2: 만료 → 1.0 복원");
        }

        //# [B3] HeroAttackDownAura 1픽 (×0.75) + Debuff Tier2 (×0.85) 동시 부착 시
        //# IDistinctHeroAura.ShouldStackAsNew=true → 두 인스턴스 모두 OnAttached 호출 → PowerScale 곱연산.
        //# 1.0 × 0.75 × 0.85 = 0.6375.
        [Test]
        public void HeroAttackDown_1픽_더하기_Tier2_누적_PowerScale_0점6375()
        {
            GameObject heroGo = new GameObject("hero");
            heroGo.AddComponent<Health>();
            MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
            atk.PowerScale = 1f;
            HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

            //# 카드 픽 ×0.75 부착.
            runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
            Assert.AreEqual(0.75f, atk.PowerScale, 0.0001f, "카드 픽 1회 → ×0.75");

            //# Debuff Tier2 ×0.85 부착 — factor 다르므로 ShouldStackAsNew=true → 신규 OnAttached.
            runner.Attach(new HeroAttackDownAura(atk, 0.85f), duration: -1f);
            Assert.AreEqual(0.75f * 0.85f, atk.PowerScale, 0.0001f, "Tier2 누적 → ×0.6375");

            Object.DestroyImmediate(heroGo);
        }

        //# [B3] 엣지 — 같은 factor 재부착은 ShouldStackAsNew=false → 기존 가드로 OnAttached 재호출 안 됨.
        [Test]
        public void HeroAttackDown_같은_factor_재부착은_PowerScale_재곱연산_안됨()
        {
            GameObject heroGo = new GameObject("hero");
            heroGo.AddComponent<Health>();
            MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
            atk.PowerScale = 1f;
            HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

            runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
            float after1 = atk.PowerScale;
            //# 같은 factor 재부착 — 기존 가드로 신규 인스턴스 무시 → PowerScale 변화 없음.
            runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
            Assert.AreEqual(after1, atk.PowerScale, 0.0001f, "같은 factor 재부착 → PowerScale 변화 없음");

            Object.DestroyImmediate(heroGo);
        }
    }
}
