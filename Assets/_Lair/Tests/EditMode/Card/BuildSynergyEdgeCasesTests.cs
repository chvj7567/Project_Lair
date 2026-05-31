using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — BuildSynergyService 엣지 망라.
    //# 기존 BuildSynergyServiceTests 는 정상 + 엣지 1 (3장 도달 / 4장 재발화 X / 다른 축 침범 X) 수준.
    //# 본 스위트는 5·7장 임계 / 7장 도달 후 8·9장 픽 / 4축 동시 임계 / Reset 후 카운트 0 / Tier 누적 (1+2+3) 까지 망라.
    public class BuildSynergyEdgeCasesTests
    {
        //# ===== 1. 임계 5·7장 발화 + 도달 후 재발화 X =====

        [Test]
        public void 시너지_5장_도달_시_Tier2_1회_Apply()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            FakeTier tier2 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);
            sut.BindTier(EBuildAxis.Tank, 5, tier2);

            for (int i = 0; i < 4; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tier1.AppliedCount, "4장 — Tier1 만 발화 (3장째 1회)");
            Assert.AreEqual(0, tier2.AppliedCount, "4장 — Tier2 미발화");

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tier1.AppliedCount, "5장 — Tier1 재발화 없음");
            Assert.AreEqual(1, tier2.AppliedCount, "5장 — Tier2 1회 발화");
        }

        [Test]
        public void 시너지_7장_도달_시_Tier3_1회_Apply()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            FakeTier tier2 = new FakeTier();
            FakeTier tier3 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);
            sut.BindTier(EBuildAxis.Tank, 5, tier2);
            sut.BindTier(EBuildAxis.Tank, 7, tier3);

            for (int i = 0; i < 6; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(0, tier3.AppliedCount, "6장 — Tier3 미발화");

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tier3.AppliedCount, "7장 — Tier3 1회 발화");
            Assert.AreEqual(1, tier1.AppliedCount, "Tier1 누적 — 7장 도달해도 1회 유지 (재발화 X)");
            Assert.AreEqual(1, tier2.AppliedCount, "Tier2 누적 — 7장 도달해도 1회 유지 (재발화 X)");
        }

        //# 기획서 §4.1: "같은 임계는 라운드당 1회만 (4장째, 5장째 픽해도 Tier1=3장 임계는 재발화 X)".
        //# 7장 도달 후 8·9장 픽 시 어떤 Tier 도 재발화하지 않아야 함.
        [Test]
        public void 시너지_7장_도달_후_8장_9장_픽_재발화_없음()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            FakeTier tier2 = new FakeTier();
            FakeTier tier3 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);
            sut.BindTier(EBuildAxis.Tank, 5, tier2);
            sut.BindTier(EBuildAxis.Tank, 7, tier3);

            for (int i = 0; i < 9; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);

            Assert.AreEqual(9, sut.GetCount(EBuildAxis.Tank), "카운트는 9 까지 계속 누적");
            Assert.AreEqual(1, tier1.AppliedCount, "Tier1 — 3장째 1회만");
            Assert.AreEqual(1, tier2.AppliedCount, "Tier2 — 5장째 1회만");
            Assert.AreEqual(1, tier3.AppliedCount, "Tier3 — 7장째 1회만");
        }

        //# ===== 2. 4축 동시 임계 도달 =====

        //# 한 축 9픽으로 7장 도달 후 다른 축 픽이 한 축 7장 도달과 동시 발생 — 기획서 §9.5 의 "원리상 불가" 단정.
        //# 본 테스트는 *그럼에도 시뮬레이션 / 테스트 코드가 동일 ctx 위에서 동시 호출했을 때* 4축의 Tier1 이 각각 1회만 발화하는지 회귀로 검증.
        [Test]
        public void 시너지_4축_동시_임계_도달_각_축_1회씩_발화_침범없음()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankT1 = new FakeTier();
            FakeTier dpsT1  = new FakeTier();
            FakeTier debT1  = new FakeTier();
            FakeTier swT1   = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank,   3, tankT1);
            sut.BindTier(EBuildAxis.Dps,    3, dpsT1);
            sut.BindTier(EBuildAxis.Debuff, 3, debT1);
            sut.BindTier(EBuildAxis.Swarm,  3, swT1);

            //# 각 축 3장씩 픽 (총 12픽) — 임계 동시 도달.
            for (int i = 0; i < 3; ++i)
            {
                sut.RegisterPick(EBuildAxis.Tank,   ctx);
                sut.RegisterPick(EBuildAxis.Dps,    ctx);
                sut.RegisterPick(EBuildAxis.Debuff, ctx);
                sut.RegisterPick(EBuildAxis.Swarm,  ctx);
            }

            Assert.AreEqual(1, tankT1.AppliedCount, "Tank Tier1 — 1회 발화");
            Assert.AreEqual(1, dpsT1.AppliedCount,  "Dps Tier1 — 1회 발화");
            Assert.AreEqual(1, debT1.AppliedCount,  "Debuff Tier1 — 1회 발화");
            Assert.AreEqual(1, swT1.AppliedCount,   "Swarm Tier1 — 1회 발화");

            Assert.AreEqual(3, sut.GetCount(EBuildAxis.Tank));
            Assert.AreEqual(3, sut.GetCount(EBuildAxis.Dps));
            Assert.AreEqual(3, sut.GetCount(EBuildAxis.Debuff));
            Assert.AreEqual(3, sut.GetCount(EBuildAxis.Swarm));
        }

        //# ===== 3. Reset 후 카운트 0 — 라운드 재시작 회귀 =====

        //# 기획서 §4.1: "라운드 시작 / Restart 시 Reset() 호출 — 픽 카운트만 초기화 (Tier 바인딩은 영구)".
        [Test]
        public void Reset_후_Tier_바인딩_유지_카운트만_초기화()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);

            //# 1라운드 — Tier1 발화.
            for (int i = 0; i < 3; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tier1.AppliedCount, "1라운드 — 1회 발화");

            //# Reset.
            sut.Reset();
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Tank), "Reset 후 카운트 0");

            //# 2라운드 — 같은 Tier 바인딩이 유지되어 다시 3장 도달 시 발화.
            for (int i = 0; i < 3; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(2, tier1.AppliedCount, "2라운드 — Tier 바인딩 유지 → 1회 추가 발화 (누적 2회)");
        }

        //# Reset 직전 7장까지 도달한 상태에서 Reset 후 다시 7장 도달 — Tier1·2·3 각각 한 라운드당 1회씩 정확히 발화.
        [Test]
        public void Reset_후_재라운드_Tier1_2_3_각_라운드당_1회씩_재발화()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier t1 = new FakeTier();
            FakeTier t2 = new FakeTier();
            FakeTier t3 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, t1);
            sut.BindTier(EBuildAxis.Tank, 5, t2);
            sut.BindTier(EBuildAxis.Tank, 7, t3);

            for (int i = 0; i < 7; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);
            sut.Reset();
            for (int i = 0; i < 7; ++i) sut.RegisterPick(EBuildAxis.Tank, ctx);

            Assert.AreEqual(2, t1.AppliedCount, "2라운드 누적 — 각 1회씩 = 2");
            Assert.AreEqual(2, t2.AppliedCount, "2라운드 누적 — 각 1회씩 = 2");
            Assert.AreEqual(2, t3.AppliedCount, "2라운드 누적 — 각 1회씩 = 2");
        }

        //# ===== 4. 같은 카드 K번 픽 = 시너지 카운트 K번 — 기획서 §9.2 (트레이드오프 T2) =====

        //# "WispHpBoost ×3 픽 → Layer 1 Tank 카운트 3 → Tier1 즉시 도달" — 본 정책 회귀.
        [Test]
        public void 같은_카드_3번_픽도_시너지_카운트_3으로_누적_Tier1_도달()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);

            //# 같은 카드(Tank 축) 3번 픽 — 카운트는 가산 누적.
            sut.RegisterPick(EBuildAxis.Tank, ctx);
            sut.RegisterPick(EBuildAxis.Tank, ctx);
            sut.RegisterPick(EBuildAxis.Tank, ctx);

            Assert.AreEqual(3, sut.GetCount(EBuildAxis.Tank), "고유 카드 1장이어도 누적 픽 카운트는 3");
            Assert.AreEqual(1, tier1.AppliedCount, "Tier1 — 누적 픽 3장으로 즉시 발화 (§9.2 T2 정책)");
        }

        //# ===== 5. Tier 바인딩 없는 임계는 no-op =====

        //# Tier1 만 바인딩하고 5장 픽 — Tier2 바인딩 없으므로 Tier1 만 발화. 예외 없음.
        [Test]
        public void Tier2_미바인딩_상태에서_5장_픽_예외없음_Tier1만_발화()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, 3, tier1);

            for (int i = 0; i < 5; ++i)
            {
                Assert.DoesNotThrow(() => sut.RegisterPick(EBuildAxis.Tank, ctx),
                    $"Tier 미바인딩 임계(5장)도 예외 없이 통과 (i={i})");
            }

            Assert.AreEqual(1, tier1.AppliedCount, "Tier1 만 1회 발화");
            Assert.AreEqual(5, sut.GetCount(EBuildAxis.Tank));
        }

        //# ===== Fakes =====

        private class FakeTier : IBuildSynergyTier
        {
            public int AppliedCount { get; private set; }
            public void Apply(IBattleContext ctx) { ++AppliedCount; }
        }

        //# 본 스위트는 ctx 전달만 확인 — 메서드 호출 자체는 더미.
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
