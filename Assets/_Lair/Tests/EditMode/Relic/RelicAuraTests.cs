using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 신규 오라 2종 + 재사용 1종 (relics §6.2, §12.6 "오라") — 순수 로직. 월드 의존(낙뢰 FX/SFX, 데미지 숫자 색)은 PlayMode 수동 확인.
    //# ThunderShardAura.Tick 의 살아 있는 영웅 경로는 HeroSkillFx.PlaySound 가 CHMSound 싱글톤을 자동 생성하므로 EditMode 에서 호출하지 않는다.
    public class RelicAuraTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void 생성물을_정리한다()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private static FakeHealth Hero(int max)
        {
            FakeHealth hero = new FakeHealth();
            hero.SetMax(max);
            return hero;
        }

        //# --- EmberBrandAura ---

        [Test]
        public void Ember_틱_피해는_최대HP_x_비율을_반올림하고_최소_1이다()
        {
            Assert.AreEqual(2, new EmberBrandAura(0.0002f).DamagePerTick(10000));
            Assert.AreEqual(6, new EmberBrandAura(0.00056f).DamagePerTick(10000), "5.6 → 6");
            Assert.AreEqual(1, new EmberBrandAura(0.0002f).DamagePerTick(100), "0.02 → 최소 1");
            Assert.AreEqual(1, new EmberBrandAura(0.0002f).DamagePerTick(1000), "0.2 → 최소 1");
            Assert.AreEqual(1, new EmberBrandAura(0f).DamagePerTick(10000), "비율 0 이어도 최소 1");
        }

        [Test]
        public void Ember_1초가_차기_전에는_피해가_없고_1초에_한번_들어간다()
        {
            FakeHealth hero = Hero(10000);
            EmberBrandAura aura = new EmberBrandAura(0.0002f);
            aura.OnAttached(hero);

            aura.Tick(hero, 0.5f);
            Assert.AreEqual(0, hero.DamageCallCount);
            aura.Tick(hero, 0.5f);

            Assert.AreEqual(1, hero.DamageCallCount);
            Assert.AreEqual(2, hero.LastDamage);
            Assert.AreEqual(9998, hero.Current);
        }

        [Test]
        public void Ember_큰_dt는_경과한_초만큼_여러_틱으로_나눠_들어가고_나머지는_이월된다()
        {
            FakeHealth hero = Hero(10000);
            EmberBrandAura aura = new EmberBrandAura(0.0002f);
            aura.OnAttached(hero);

            aura.Tick(hero, 3.0f);
            Assert.AreEqual(3, hero.DamageCallCount);
            aura.Tick(hero, 2.5f);
            Assert.AreEqual(5, hero.DamageCallCount, "2.5 → 2틱, 0.5 이월");
            aura.Tick(hero, 0.5f);

            Assert.AreEqual(6, hero.DamageCallCount, "이월 0.5 + 0.5 = 1.0");
        }

        [Test]
        public void Ember_OnAttached가_누적_시간을_리셋한다_풀_재사용()
        {
            FakeHealth hero = Hero(10000);
            EmberBrandAura aura = new EmberBrandAura(0.0002f);
            aura.OnAttached(hero);
            aura.Tick(hero, 0.75f);

            aura.OnAttached(hero);
            aura.Tick(hero, 0.5f);

            Assert.AreEqual(0, hero.DamageCallCount);
        }

        [Test]
        public void Ember_영웅이_null이어도_예외가_없고_OnDetached는_무동작이다()
        {
            EmberBrandAura aura = new EmberBrandAura(0.0002f);
            FakeHealth hero = Hero(10000);

            Assert.DoesNotThrow(() => aura.Tick(null, 5f));
            Assert.DoesNotThrow(() => aura.OnDetached(hero));
            Assert.AreEqual(0, hero.DamageCallCount);
        }

        [Test]
        public void Ember는_Debuff_축이고_영구_감소_보정_대상이다()
        {
            object aura = new EmberBrandAura(0.0002f);

            Assert.IsTrue(aura is IDebuffAxisAura);
            Assert.IsTrue(((IDebuffAxisAura)aura).IsDebuffAxis);
            Assert.IsTrue(aura is IPenaltyScalable);
        }

        [Test]
        public void Ember_WithPenaltyScale은_비율을_곱한_새_인스턴스를_돌려주고_원본은_그대로다()
        {
            EmberBrandAura original = new EmberBrandAura(0.0002f);

            IHeroAura scaled = original.WithPenaltyScale(0.6f);

            Assert.AreNotSame(original, scaled);
            Assert.IsInstanceOf<EmberBrandAura>(scaled);
            Assert.AreEqual(0.00012f, ((EmberBrandAura)scaled).RatioPerSec, 1e-9f);
            Assert.AreEqual(0.0002f, original.RatioPerSec, 1e-9f);
        }

        //# --- ThunderShardAura ---

        [Test]
        public void Thunder_낙뢰_피해는_최대HP_x_비율을_반올림하고_최소_1이다()
        {
            Assert.AreEqual(63, new ThunderShardAura(10f, 0.0063f).DamagePerStrike(10000));
            Assert.AreEqual(30, new ThunderShardAura(10f, 0.003f).DamagePerStrike(10000));
            Assert.AreEqual(1, new ThunderShardAura(10f, 0.0063f).DamagePerStrike(100), "0.63 → 1");
            Assert.AreEqual(1, new ThunderShardAura(10f, 0.0001f).DamagePerStrike(100), "0.01 → 최소 1");
        }

        [Test]
        public void Thunder_첫_낙뢰는_부착_후_간격이_지난_시점이다()
        {
            ThunderShardAura aura = new ThunderShardAura(10f, 0.003f);
            aura.OnAttached(Hero(1000));

            Assert.AreEqual(0, aura.AdvanceStrikes(4.5f));
            Assert.AreEqual(0, aura.AdvanceStrikes(5.25f), "9.75 경과");
            Assert.AreEqual(1, aura.AdvanceStrikes(0.25f), "정확히 10.0 에서 첫 낙뢰");
        }

        [Test]
        public void Thunder_큰_dt는_여러_번_떨어지고_나머지가_이월된다()
        {
            ThunderShardAura aura = new ThunderShardAura(10f, 0.003f);
            aura.OnAttached(Hero(1000));

            Assert.AreEqual(2, aura.AdvanceStrikes(25f));
            Assert.AreEqual(0, aura.AdvanceStrikes(4.5f), "이월 5 + 4.5 = 9.5");
            Assert.AreEqual(1, aura.AdvanceStrikes(0.5f));
        }

        [Test]
        public void Thunder_OnAttached가_누적을_리셋한다()
        {
            ThunderShardAura aura = new ThunderShardAura(10f, 0.003f);
            aura.AdvanceStrikes(9f);

            aura.OnAttached(Hero(1000));

            Assert.AreEqual(0, aura.AdvanceStrikes(2f));
        }

        [Test]
        public void Thunder_간격이_0_이하면_0_1초로_보정된다()
        {
            ThunderShardAura aura = new ThunderShardAura(0f, 0.003f);

            Assert.AreEqual(2, aura.AdvanceStrikes(0.25f), "0.1 간격: 0.25 에 2번");
        }

        [Test]
        public void Thunder는_Debuff_축이_아니고_성기사_저항_보정_대상도_아니다()
        {
            object aura = new ThunderShardAura(10f, 0.003f);

            Assert.IsFalse(aura is IDebuffAxisAura, "D-7: 순수 피해");
            Assert.IsFalse(aura is IPenaltyScalable);
        }

        [Test]
        public void Thunder_간격이_안_찬_Tick은_피해가_없고_null_영웅이나_죽은_영웅은_안전하다()
        {
            ThunderShardAura aura = new ThunderShardAura(10f, 0.003f);
            FakeHealth alive = Hero(10000);
            FakeHealth dead = Hero(10000);
            dead.ForceSetCurrent(0);
            aura.OnAttached(alive);

            aura.Tick(alive, 5f);
            Assert.AreEqual(0, alive.DamageCallCount);
            Assert.DoesNotThrow(() => aura.Tick(null, 30f));
            ThunderShardAura second = new ThunderShardAura(10f, 0.003f);
            second.OnAttached(dead);
            Assert.DoesNotThrow(() => second.Tick(dead, 30f));

            Assert.AreEqual(0, dead.DamageCallCount, "죽은 영웅은 낙뢰 FX·피해 없이 종료");
        }

        //# --- CursedBanner = HeroAttackDownAura 재사용 ---

        [Test]
        public void CursedBanner_factor는_영웅_공격력_배율에_곱해지고_중복_부착은_한번만_적용된다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile();
            profile.Relics.Add(new RelicEntry { RelicId = "CursedBanner", Stack = 3 });
            RelicBattleBonus bonus = RelicBattleBonus.From(profile, db);
            FakeAttacker attacker = new FakeAttacker();
            HeroAttackDownAura aura = new HeroAttackDownAura(attacker, bonus.HeroAttackFactor);

            aura.OnAttached(Hero(100));
            aura.OnAttached(Hero(100));

            Assert.AreEqual(0.916f, attacker.PowerScale, 1e-4f);
        }

        [Test]
        public void CursedBanner_성기사_저항_보정은_감소분에_곱해진다()
        {
            HeroAttackDownAura aura = new HeroAttackDownAura(new FakeAttacker(), 0.916f);

            IHeroAura scaled = aura.WithPenaltyScale(0.6f);

            Assert.AreEqual(1f - (1f - 0.916f) * 0.6f, ((HeroAttackDownAura)scaled).Factor, 1e-5f);
        }

        //# --- 성기사 "신앙의 가호" x HeroAuraRunner (영구 부착 duration < 0) ---

        private HeroAuraRunner NewRunner(bool withResist)
        {
            GameObject go = new GameObject("RelicHero");
            _spawned.Add(go);
            go.AddComponent<Health>();
            HeroAuraRunner runner = go.AddComponent<HeroAuraRunner>();
            if (withResist)
            {
                runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            }
            return runner;
        }

        private static object AttachedAura(HeroAuraRunner runner, System.Type auraType)
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "HeroAuraRunner._slots 존재 — 시그니처 변경 감지");
            foreach (object slot in (IList)field.GetValue(runner))
            {
                object aura = slot.GetType().GetField("Aura").GetValue(slot);
                if (aura.GetType() == auraType)
                    return aura;
            }
            return null;
        }

        [Test]
        public void 성기사_저항은_Ember_영구_화상_비율을_줄이고_Thunder_낙뢰는_줄이지_않는다()
        {
            HeroAuraRunner runner = NewRunner(true);

            runner.Attach(new EmberBrandAura(0.0002f), -1f);
            runner.Attach(new ThunderShardAura(10f, 0.003f), -1f);

            EmberBrandAura ember = (EmberBrandAura)AttachedAura(runner, typeof(EmberBrandAura));
            ThunderShardAura thunder = (ThunderShardAura)AttachedAura(runner, typeof(ThunderShardAura));
            Assert.AreEqual(0.00012f, ember.RatioPerSec, 1e-9f, "x0.6");
            Assert.AreEqual(300, thunder.DamagePerStrike(100000), "E-16: 저항 비대상");
        }

        [Test]
        public void 성기사_저항은_CursedBanner_공격력_감소분을_줄인다()
        {
            HeroAuraRunner runner = NewRunner(true);

            runner.Attach(new HeroAttackDownAura(new FakeAttacker(), 0.916f), -1f);

            HeroAttackDownAura aura = (HeroAttackDownAura)AttachedAura(runner, typeof(HeroAttackDownAura));
            Assert.AreEqual(1f - (1f - 0.916f) * 0.6f, aura.Factor, 1e-5f);
        }

        [Test]
        public void 저항이_없는_영웅은_유물_오라가_원래_강도로_부착된다()
        {
            HeroAuraRunner runner = NewRunner(false);

            runner.Attach(new EmberBrandAura(0.0002f), -1f);
            runner.Attach(new HeroAttackDownAura(new FakeAttacker(), 0.916f), -1f);

            Assert.AreEqual(0.0002f, ((EmberBrandAura)AttachedAura(runner, typeof(EmberBrandAura))).RatioPerSec, 1e-9f);
            Assert.AreEqual(0.916f, ((HeroAttackDownAura)AttachedAura(runner, typeof(HeroAttackDownAura))).Factor, 1e-5f);
        }
    }
}
