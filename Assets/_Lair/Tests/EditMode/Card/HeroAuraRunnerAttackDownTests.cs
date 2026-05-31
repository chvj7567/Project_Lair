using Lair.Battle;
using Lair.Card;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — HeroAttackDownAura factor 누적 시나리오 망라.
    //# EffectsRenewal2026Tests B3 회귀는 *1픽 + Tier2 = ×0.6375* 와 *같은 factor 재부착 무시* 만 확인.
    //# 본 스위트는 *2픽 + Tier2 = ×0.4781* 까지 확장 (기획서 §4.5 누적 정책 예시 수치).
    public class HeroAuraRunnerAttackDownTests
    {
        //# 기획서 §4.5 누적 예시: 2픽 + Tier2 = ×0.75² × ×0.85 = ×0.4781.
        //# 단, "같은 factor 재부착 무시" 정책 때문에 시스템상 ×0.75 는 1회만 곱연산 — 따라서
        //# 본 스위트는 *factor 가 다른 인스턴스 3개* 를 부착해 실제 PowerScale 가 3중 곱연산 되는지 검증.
        //# (×0.75 카드픽 + ×0.85 Tier2 + ×0.65 가상 카드픽 = ×0.4144 — 시스템 누적 메커니즘 자체 회귀)
        [Test]
        public void HeroAttackDown_세_다른_factor_부착_PowerScale_3중_곱연산()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                heroGo.AddComponent<Health>();
                MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
                atk.PowerScale = 1f;
                HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

                runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
                runner.Attach(new HeroAttackDownAura(atk, 0.85f), duration: -1f);
                runner.Attach(new HeroAttackDownAura(atk, 0.65f), duration: -1f);

                Assert.AreEqual(0.75f * 0.85f * 0.65f, atk.PowerScale, 0.0001f,
                    "3개 다른 factor 부착 — PowerScale 3중 곱연산 (×0.4144)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# IDistinctHeroAura.ShouldStackAsNew 회귀 — Mathf.Approximately 의 fp 오차 정책.
        //# 두 factor 가 fp 동등 (Approximately) 이면 ShouldStackAsNew=false → 재부착 무시.
        [Test]
        public void HeroAttackDown_factor_fp_근사_동등_재부착_무시()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                heroGo.AddComponent<Health>();
                MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
                atk.PowerScale = 1f;
                HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

                runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
                //# fp 오차 범위 안 — Mathf.Approximately 가 동등으로 판정.
                runner.Attach(new HeroAttackDownAura(atk, 0.75f + 1e-7f), duration: -1f);

                Assert.AreEqual(0.75f, atk.PowerScale, 0.0001f,
                    "fp 근사 factor 재부착 → 무시 (PowerScale ×0.75 그대로)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# 회귀 — 다른 type Aura (예: BleedAura) 가 같이 있어도 HeroAttackDownAura 의 가드는 *같은 type* 만 체크.
        //# BleedAura 가 있어도 HeroAttackDownAura 의 factor 누적은 정상 동작.
        [Test]
        public void HeroAttackDown_다른_type_Aura_혼재_누적_정상()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);
                MeleeAttacker atk = heroGo.AddComponent<MeleeAttacker>();
                atk.PowerScale = 1f;
                SimpleMover mover = heroGo.AddComponent<SimpleMover>();
                HeroAuraRunner runner = heroGo.AddComponent<HeroAuraRunner>();

                //# Bleed 와 AttackDown 동시 부착 — 서로 다른 type 라 가드 무관.
                runner.Attach(new BleedAura(mover, 0.02f), duration: 10f);
                runner.Attach(new HeroAttackDownAura(atk, 0.75f), duration: -1f);
                runner.Attach(new HeroAttackDownAura(atk, 0.85f), duration: -1f);

                Assert.AreEqual(0.75f * 0.85f, atk.PowerScale, 0.0001f,
                    "다른 type Aura(Bleed) 가 있어도 HeroAttackDown factor 누적은 정상 ×0.6375");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }
    }
}
