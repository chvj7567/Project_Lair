using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# B3 공포 도주 안정화 — TryGetThreatCentroidMonster centroid/count 동작 박제.
    //# 정상(포위 4마리) + 엣지(반경 제외 / 전부 Marching) 케이스.
    public class ThreatCentroidTests
    {
        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        private Transform SpawnMonster(Vector3 pos, bool engaging)
        {
            Transform t = new GameObject("m").transform;
            t.position = pos;
            CharacterRegistry.RegisterMonster(t, new FakeHealth());
            CharacterRegistry.SetMonsterEngaging(t, engaging);
            return t;
        }

        [Test]
        public void 포위_4마리_centroid는_중심_count는_4()
        {
            //# (0,0,0) 영웅을 ±X/±Z 로 둘러싼 4마리 → centroid ≈ 원점, count=4.
            Transform a = SpawnMonster(new Vector3(2, 0, 0), true);
            Transform b = SpawnMonster(new Vector3(-2, 0, 0), true);
            Transform c = SpawnMonster(new Vector3(0, 0, 2), true);
            Transform d = SpawnMonster(new Vector3(0, 0, -2), true);

            bool found = CharacterRegistry.TryGetThreatCentroidMonster(
                Vector3.zero, 4f, out Vector3 centroid, out int count);

            Assert.IsTrue(found);
            Assert.AreEqual(4, count);
            Assert.Less(centroid.magnitude, 0.001f, "대칭 포위 centroid 는 원점 근처");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(d.gameObject);
        }

        [Test]
        public void 반경_밖_몬스터는_centroid에서_제외()
        {
            //# 엣지 — radius 4. 안쪽(2,0,0) 1마리만 포함, 바깥(10,0,0) 제외.
            Transform near = SpawnMonster(new Vector3(2, 0, 0), true);
            Transform far = SpawnMonster(new Vector3(10, 0, 0), true);

            bool found = CharacterRegistry.TryGetThreatCentroidMonster(
                Vector3.zero, 4f, out Vector3 centroid, out int count);

            Assert.IsTrue(found);
            Assert.AreEqual(1, count, "반경 4 밖의 far 는 제외");
            Assert.Less(Vector3.Distance(centroid, new Vector3(2, 0, 0)), 0.001f);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        [Test]
        public void 전부_Marching이면_count0_false()
        {
            //# 엣지 — 반경 안이지만 전부 Marching(IsEngaging=false) → 후보 0.
            Transform a = SpawnMonster(new Vector3(1, 0, 0), false);
            Transform b = SpawnMonster(new Vector3(-1, 0, 0), false);

            bool found = CharacterRegistry.TryGetThreatCentroidMonster(
                Vector3.zero, 4f, out Vector3 _, out int count);

            Assert.IsFalse(found, "Engaging 몬스터 없음 — false");
            Assert.AreEqual(0, count);

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }
    }
}
