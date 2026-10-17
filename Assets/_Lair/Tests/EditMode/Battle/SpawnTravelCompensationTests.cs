using Lair.Battle;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 스포너별 첫 스폰 지연 — 기획서 scene-2d-conversion §3.5.4 · §7.8 · §10.
    public class SpawnTravelCompensationTests
    {
        private const float DWide = 8.320f;
        private const float DDiag = 6.871f;

        [TestCase(DWide, 1f, 1.0f, 11.680f)]
        [TestCase(DDiag, 1f, 1.5f, 8.753f)]
        [TestCase(DDiag, 5f, 1.4f, 9.378f)]
        [TestCase(DWide, 1.3f, 0.8f, 14.600f)]
        [TestCase(DDiag, 1f, 1.3f, 10.099f)]
        [TestCase(DDiag, 1f, 2.4f, 5.470f)]
        public void 여섯_스포너의_지연이_기획서_표와_같다(float d, float range, float speed, float expected)
        {
            Assert.AreEqual(expected, SpawnTravelCompensation.FirstSpawnDelay(20f, d, range, speed, 1f), 0.002f);
        }

        [Test]
        public void 계수_c_는_지연에_곱해진다()
        {
            Assert.AreEqual(10.512f, SpawnTravelCompensation.FirstSpawnDelay(20f, DWide, 1f, 1f, 0.9f), 0.002f);
        }

        [Test]
        public void 속도_0이면_지연_0이다()
        {
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, DWide, 1f, 0f, 1f));
        }

        [Test]
        public void 새_반지름이_사거리_안이면_이동거리_0이다()
        {
            //# range 7, 새 거리 6 → 이동 0, 옛 거리 20 → 13 → 지연 = 13 / speed
            Assert.AreEqual(13f, SpawnTravelCompensation.FirstSpawnDelay(20f, 6f, 7f, 1f, 1f), 1e-4f);
        }

        [Test]
        public void 거리가_옛_반지름보다_멀면_음수가_아니라_0이다()
        {
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 25f, 1f, 1f, 1f));
        }
    }

    public class SpawnerFirstDelayTests
    {
        private class FakeHost : ISpawnerHost
        {
            public int Calls;
            public void SpawnFromSpawner(Lair.Data.EMonster type, Vector3 exactPos, int count) { Calls++; }
        }

        private GameObject _go;
        private Spawner _spawner;
        private FakeHost _host;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("spawner");
            _spawner = _go.AddComponent<Spawner>();
            _host = new FakeHost();
            _spawner.Bind(_host, null);
            _spawner.SetBasePeriod(9f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void 지연_0이면_기존과_같이_첫_Tick에_즉시_스폰한다()
        {
            _spawner.Tick(0.016f);
            Assert.AreEqual(1, _host.Calls);
        }

        [Test]
        public void 지연_중_진행률은_timer_나누기_delay다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(4f);
            Assert.AreEqual(0.4f, _spawner.Progress, 1e-4f);
            Assert.AreEqual(6f, _spawner.RemainingSeconds, 1e-4f);
            Assert.AreEqual(0, _host.Calls);
        }

        [Test]
        public void 지연_경과_시_첫_스폰하고_이후는_주기를_따른다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(9.9f);
            Assert.AreEqual(0, _host.Calls);
            _spawner.Tick(0.2f);
            Assert.AreEqual(1, _host.Calls);
            _spawner.Tick(8.9f);
            Assert.AreEqual(1, _host.Calls);
            _spawner.Tick(0.2f);
            Assert.AreEqual(2, _host.Calls);
        }
    }
}
