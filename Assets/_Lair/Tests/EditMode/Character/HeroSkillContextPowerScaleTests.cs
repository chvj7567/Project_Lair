using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.Character
{
    //# 스모크: 스킬 데미지가 영웅 PowerScale 을 받는지. 본격 스위트(라운딩 경계/디버프 연동)는 test-engineer.
    public class HeroSkillContextPowerScaleTests
    {
        private GameObject _hero;
        private GameObject _monster;

        [SetUp]
        public void SetUp()
        {
            _hero = new GameObject("Hero");
            _monster = new GameObject("Monster");
            _monster.transform.position = new Vector3(2f, 0f, 0f);   //# 반경 3 안
            Health h = _monster.AddComponent<Health>();
            h.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(_monster.transform, h);
            CharacterRegistry.SetMonsterEngaging(_monster.transform, true);
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRegistry.UnregisterMonster(_monster.transform);
            Object.DestroyImmediate(_hero);
            Object.DestroyImmediate(_monster);
        }

        [Test]
        public void PowerScale_0_75_적용_스킬데미지_75퍼센트()
        {
            MeleeAttacker attacker = _hero.AddComponent<MeleeAttacker>();
            attacker.PowerScale = 0.75f;
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);
            int before = _monster.GetComponent<Health>().Current;

            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);

            //# RoundToInt(100 * 0.75) = 75
            Assert.AreEqual(before - 75, _monster.GetComponent<Health>().Current);
        }

        [Test]
        public void PowerScale_1_회귀_데미지_불변()
        {
            MeleeAttacker attacker = _hero.AddComponent<MeleeAttacker>();
            attacker.PowerScale = 1f;
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);
            int before = _monster.GetComponent<Health>().Current;

            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);

            Assert.AreEqual(before - 100, _monster.GetComponent<Health>().Current);
        }

        [Test]
        public void IAttacker_부재_배율1_fallback()
        {
            //# 영웅에 MeleeAttacker 미부착 → GetComponent<IAttacker> null → 배율 1.
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);
            int before = _monster.GetComponent<Health>().Current;

            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);

            Assert.AreEqual(before - 100, _monster.GetComponent<Health>().Current);
        }
    }
}
