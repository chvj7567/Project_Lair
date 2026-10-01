using Lair.Battle;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 첫 스폰 지연 엣지·경계 — 기획서 scene-2d-conversion §3.5.4 · §7.8 · §10 (기본 케이스는 SpawnTravelCompensationTests).
    public class SpawnTravelCompensationEdgeTests
    {
        private static readonly Vector3 Center = new Vector3(0f, 0f, -2f);
        private static readonly float[] Angles = { 0f, 60f, 120f, 180f, 240f, 300f };

        //# 스포너 순서: Wisp, Reaper, Hex, Wraith, Plague, Phantom (씬 각도 0·60·120·180·240·300°)
        private static readonly float[] Range = { 1f, 1f, 5f, 1.3f, 1f, 1f };
        private static readonly float[] Speed = { 1.0f, 1.5f, 1.4f, 0.8f, 1.3f, 2.4f };
        private static readonly float[] Expected = { 11.680f, 8.753f, 9.378f, 14.600f, 10.099f, 5.470f };

        private static float DistanceOf(int i)
        {
            Vector3 p = CircularSpawnerArranger.PositionOnEllipse(Center, 8.32f, 6.3149f, Angles[i]);
            Vector3 d = p - Center;
            return new Vector2(d.x, d.z).magnitude;
        }

        [Test]
        public void 상수는_기획서_값이다()
        {
            Assert.AreEqual(20f, SpawnTravelCompensation.LegacyRadius);
            Assert.AreEqual(1f, SpawnTravelCompensation.DelayScale);
        }

        [Test]
        public void 타원_배치_실제_거리로_계산해도_표와_일치한다()
        {
            for (int i = 0; i < 6; i++)
            {
                float delay = SpawnTravelCompensation.FirstSpawnDelay(
                    SpawnTravelCompensation.LegacyRadius, DistanceOf(i), Range[i], Speed[i], SpawnTravelCompensation.DelayScale);
                Assert.AreEqual(Expected[i], delay, 0.005f, "spawner " + i);
            }
        }

        [Test]
        public void 좌우_스포너_거리는_8_32_대각은_6_871이다()
        {
            Assert.AreEqual(8.32f, DistanceOf(0), 1e-3f);
            Assert.AreEqual(8.32f, DistanceOf(3), 1e-3f);
            foreach (int i in new[] { 1, 2, 4, 5 })
            {
                Assert.AreEqual(6.871f, DistanceOf(i), 1e-3f, "spawner " + i);
            }
        }

        [Test]
        public void 지연_더하기_새_이동시간은_옛_이동시간과_같다()
        {
            //# 이동 시간 보존 — 지연 + (D−range)/speed == (R−range)/speed (range < D 일 때)
            for (int i = 0; i < 6; i++)
            {
                float d = DistanceOf(i);
                float delay = SpawnTravelCompensation.FirstSpawnDelay(20f, d, Range[i], Speed[i], 1f);
                float legacyTime = (20f - Range[i]) / Speed[i];
                float newTime = (d - Range[i]) / Speed[i];
                Assert.AreEqual(legacyTime, delay + newTime, 1e-3f, "spawner " + i);
            }
        }

        [Test]
        public void 사거리가_거리와_같으면_새_이동거리_0이라_옛_이동시간_전부가_지연이다()
        {
            //# range == distance → 새 이동 0, 옛 이동 (20−5) = 15, speed 1.5 → 10
            Assert.AreEqual(10f, SpawnTravelCompensation.FirstSpawnDelay(20f, 5f, 5f, 1.5f, 1f), 1e-4f);
        }

        [Test]
        public void 사거리가_거리보다_크면_새_이동거리는_0이고_옛_이동만_남는다()
        {
            //# 옛 이동 max(0, 20−9) = 11, 새 이동 max(0, 5−9) = 0 → 11 / 1.5
            Assert.AreEqual(11f / 1.5f, SpawnTravelCompensation.FirstSpawnDelay(20f, 5f, 9f, 1.5f, 1f), 1e-4f);
        }

        [Test]
        public void 사거리가_옛_반지름_이상이면_지연_0이다()
        {
            //# range 25 ≥ R 20 → 옛 이동 0, 새 이동 0
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 8f, 25f, 1f, 1f));
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 8f, 20f, 1f, 1f));
        }

        [Test]
        public void 이동속도가_음수여도_지연_0이다()
        {
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, -1f, 1f));
        }

        [Test]
        public void 거리가_옛_반지름과_같으면_지연_0이다()
        {
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 20f, 1f, 1f, 1f));
        }

        [Test]
        public void 계수_0이면_지연_0이다()
        {
            Assert.AreEqual(0f, SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, 1f, 0f));
        }

        [Test]
        public void 계수는_선형으로_곱해진다()
        {
            float c1 = SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, 1f, 1f);
            float c2 = SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, 1f, 2f);
            Assert.AreEqual(c1 * 2f, c2, 1e-4f);
        }

        [Test]
        public void 이동속도가_빠를수록_지연이_짧다()
        {
            float slow = SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, 0.5f, 1f);
            float fast = SpawnTravelCompensation.FirstSpawnDelay(20f, 8.32f, 1f, 2f, 1f);
            Assert.Greater(slow, fast);
        }

        [Test]
        public void 실제_밸런스_설정으로_계산한_여섯_지연이_기획서_표와_같다()
        {
            BalanceConfig cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Lair/Data/BalanceConfig.asset");
            Assert.IsNotNull(cfg, "BalanceConfig.asset");
            EMonster[] types = { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom };
            for (int i = 0; i < 6; i++)
            {
                BalanceConfig.CharacterStat s = cfg.GetMonster(types[i]);
                Assert.IsNotNull(s, types[i].ToString());
                float delay = SpawnTravelCompensation.FirstSpawnDelay(
                    SpawnTravelCompensation.LegacyRadius, DistanceOf(i), s.Range, s.MoveSpeed, SpawnTravelCompensation.DelayScale);
                Assert.AreEqual(Expected[i], delay, 0.005f, types[i].ToString());
            }
        }
    }

    public class SpawnerFirstDelayEdgeTests
    {
        private class FakeHost : ISpawnerHost
        {
            public int Calls;
            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count) { Calls++; }
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
        public void 음수_지연은_0으로_보고_첫_Tick에_즉시_스폰한다()
        {
            _spawner.SetFirstSpawnDelay(-5f);
            _spawner.Tick(0.016f);
            Assert.AreEqual(1, _host.Calls);
        }

        [Test]
        public void 지연_0일_때_첫_스폰_전_진행률은_0_남은초는_주기다()
        {
            Assert.AreEqual(0f, _spawner.Progress);
            Assert.AreEqual(9f, _spawner.RemainingSeconds, 1e-4f);
        }

        [Test]
        public void 지연_중_진행률은_1을_넘지_않는다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(9.99f);
            Assert.LessOrEqual(_spawner.Progress, 1f);
            Assert.AreEqual(0, _host.Calls);
        }

        [Test]
        public void 지연_경과_직후_진행률은_주기_기준으로_전환된다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(10f);
            Assert.AreEqual(1, _host.Calls);
            Assert.AreEqual(0f, _spawner.Progress, 1e-4f);
            Assert.AreEqual(9f, _spawner.RemainingSeconds, 1e-4f);
        }

        [Test]
        public void 지연이_정확히_경계에서_스폰한다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(10f);
            Assert.AreEqual(1, _host.Calls);
        }

        [Test]
        public void 큰_dt여도_첫_스폰은_1회뿐이다()
        {
            _spawner.SetFirstSpawnDelay(10f);
            _spawner.Tick(100f);
            Assert.AreEqual(1, _host.Calls);
        }

        [Test]
        public void 지연_후_두_번째_스폰은_주기_뒤다()
        {
            _spawner.SetFirstSpawnDelay(5f);
            _spawner.Tick(5f);
            _spawner.Tick(8.99f);
            Assert.AreEqual(1, _host.Calls);
            _spawner.Tick(0.02f);
            Assert.AreEqual(2, _host.Calls);
        }

        [Test]
        public void 호스트가_없으면_Tick은_무시된다()
        {
            _spawner.Bind(null, null);
            _spawner.SetFirstSpawnDelay(1f);
            Assert.DoesNotThrow(() => _spawner.Tick(5f));
            Assert.AreEqual(0, _host.Calls);
        }
    }
}
