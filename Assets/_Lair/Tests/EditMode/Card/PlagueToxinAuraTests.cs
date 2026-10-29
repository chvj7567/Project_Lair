using System.Collections;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 역병귀 독 + 끈질긴 저주 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §15.8).
    public class PlagueToxinAuraTests
    {
        private GameObject _heroGo;
        private HeroAuraRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _heroGo = new GameObject("Hero");
            _heroGo.AddComponent<Health>();
            _runner = _heroGo.AddComponent<HeroAuraRunner>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_heroGo);

        private PlagueToxinAura AttachedToxin(out float remain)
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (object slot in (IList)field.GetValue(_runner))
            {
                if (slot.GetType().GetField("Aura").GetValue(slot) is PlagueToxinAura toxin)
                {
                    remain = (float)slot.GetType().GetField("Remain").GetValue(slot);
                    return toxin;
                }
            }
            remain = 0f;
            return null;
        }

        [Test]
        public void 적중마다_1중첩씩_최대3까지_쌓이고_지속은_연장되지_않고_갱신된다()
        {
            for (int i = 0; i < 4; ++i)
            {
                _runner.Attach(new PlagueToxinAura(2f, 1, 3), 3f);
            }

            PlagueToxinAura toxin = AttachedToxin(out float remain);
            Assert.AreEqual(3, toxin.Stacks, "4번째 적중은 상한 3 유지(E-14)");
            Assert.AreEqual(3f, remain, 0.0001f, "3 + 3 + ... 누적이 아니라 3초 갱신");
            Assert.AreEqual(6, toxin.DamagePerTick(), "3중첩 x 1중첩 피해 2");
        }

        [Test]
        public void 끈질긴_저주_1픽과_신앙의_가호는_지속에_곱으로_합성된다()
        {
            _runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            _runner.AddDebuffDurationBonus(0.25f);

            _runner.Attach(new PlagueToxinAura(2f, 1, 3), 3f);

            AttachedToxin(out float remain);
            Assert.AreEqual(2.25f, remain, 0.0001f, "3 x 1.25 x 0.6");
            Assert.AreEqual(1, _runner.ClearTimedDebuffs(), "독은 정화의 빛 해제 대상");
        }
    }

    //# 순수 로직 — GameObject 없이 (러너 SetUp 비의존).
    public class PlagueToxinAuraMergeTests
    {
        [Test]
        public void 상한이_큰_부착이_들어오면_Tier1_상한5까지_쌓이고_1중첩_피해는_큰_값을_쓴다()
        {
            PlagueToxinAura toxin = new PlagueToxinAura(2f, 3, 3);

            toxin.Merge(new PlagueToxinAura(3.4f, 3, 5));

            Assert.AreEqual(5, toxin.Stacks);
            Assert.AreEqual(3.4f, toxin.PerStackDamage, 0.0001f);
            Assert.AreEqual(17, toxin.DamagePerTick(), "5 x 3.4 = 17");
        }
    }
}
