using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.PlayMode
{
    public class HeroSkillContextPlayTests
    {
        private GameObject _hero;
        private GameObject _near;
        private GameObject _far;

        [SetUp]
        public void SetUp()
        {
            _hero = new GameObject("Hero");
            _near = MakeMonster("Near", new Vector3(2f, 0f, 0f));   //# 반경 3 안
            _far  = MakeMonster("Far",  new Vector3(8f, 0f, 0f));   //# 반경 3 밖
        }

        private GameObject MakeMonster(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRegistry.UnregisterMonster(_near.transform);
            CharacterRegistry.UnregisterMonster(_far.transform);
            Object.DestroyImmediate(_hero);
            Object.DestroyImmediate(_near);
            Object.DestroyImmediate(_far);
        }

        [Test]
        public void DamageMonstersInRing_반경내만_피격()
        {
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);
            int nearBefore = _near.GetComponent<Health>().Current;
            int farBefore = _far.GetComponent<Health>().Current;

            int hit = ctx.DamageMonstersInRing(0f, 3f, 100, 0f);

            Assert.AreEqual(1, hit);
            Assert.AreEqual(nearBefore - 100, _near.GetComponent<Health>().Current);
            Assert.AreEqual(farBefore, _far.GetComponent<Health>().Current);
        }
    }
}
