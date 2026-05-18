using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    public class CharacterRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [Test]
        public void Register_Unregister_Monsters()
        {
            var t = new GameObject("m1").transform;
            var h = new FakeHealth();
            CharacterRegistry.RegisterMonster(t, h);

            Assert.AreEqual(1, CharacterRegistry.Monsters.Count);

            CharacterRegistry.UnregisterMonster(t);
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count);

            Object.DestroyImmediate(t.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_거리순_가장_가까운()
        {
            var near = new GameObject("near").transform; near.position = new Vector3(1, 0, 0);
            var far  = new GameObject("far").transform;  far.position  = new Vector3(5, 0, 0);
            CharacterRegistry.RegisterMonster(near, new FakeHealth());
            CharacterRegistry.RegisterMonster(far, new FakeHealth());

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var _);

            Assert.IsTrue(found);
            Assert.AreSame(near, t);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_빈_레지스트리_false()
        {
            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var h);

            Assert.IsFalse(found);
            Assert.IsNull(t);
            Assert.IsNull(h);
        }

        [Test]
        public void TryFindNearestMonster_죽은_적_제외()
        {
            var alive = new GameObject("alive").transform; alive.position = new Vector3(5, 0, 0);
            var dead  = new GameObject("dead").transform;  dead.position  = new Vector3(1, 0, 0);
            var aliveHp = new FakeHealth();
            var deadHp = new FakeHealth();
            deadHp.ForceSetCurrent(0);
            CharacterRegistry.RegisterMonster(alive, aliveHp);
            CharacterRegistry.RegisterMonster(dead, deadHp);

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var _);

            Assert.IsTrue(found);
            Assert.AreSame(alive, t, "더 가까운 dead 대신 alive 가 선택돼야 함");

            Object.DestroyImmediate(alive.gameObject);
            Object.DestroyImmediate(dead.gameObject);
        }
    }
}
