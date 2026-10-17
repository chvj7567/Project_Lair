using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# Spawner 봉인/해제(SealedNest 실효과)와 SimpleMover.RuleSpeedScale(HunterTraps 감속 채널) — chapter-system §3.4·§9 E-3.
    //# EditMode 라 OnEnable/Awake 는 리플렉션으로 직접 호출한다.
    public class SpawnerSealAndMoverRuleSpeedTests
    {
        private class RecordingHost : ISpawnerHost
        {
            public readonly List<(EMonster type, int count)> Spawns = new List<(EMonster type, int count)>();

            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
            {
                Spawns.Add((type, count));
            }
        }

        private GameObject _go;
        private RecordingHost _host;

        [SetUp]
        public void 호스트를_초기화한다()
        {
            _host = new RecordingHost();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private static void InvokeNonPublic(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 존재 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        private Spawner NewSpawner()
        {
            _go = new GameObject("SpawnerSealUT");
            Spawner sp = _go.AddComponent<Spawner>();
            InvokeNonPublic(sp, "OnEnable");
            sp.Bind(_host, null);
            return sp;
        }

        //# 첫 Tick 으로 첫 발사를 소모해 주기 국면으로 진입시킨다.
        private Spawner NewSpawnerInPeriodPhase()
        {
            Spawner sp = NewSpawner();
            sp.Tick(0.01f);
            Assert.AreEqual(1, _host.Spawns.Count, "전제: 첫 Tick 즉시 첫 발사");
            return sp;
        }

        //# --- Lock ---

        [Test]
        public void 봉인된_스포너는_아무리_Tick해도_스폰하지_않는다()
        {
            Spawner sp = NewSpawner();

            sp.Lock();
            for (int i = 0; i < 100; ++i)
            {
                sp.Tick(1f);
            }

            Assert.AreEqual(0, _host.Spawns.Count);
            Assert.IsTrue(sp.IsLocked);
        }

        [Test]
        public void 봉인된_동안_타이머가_진행하지_않아_진행도와_남은시간이_멈춘다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Tick(3f);
            float progress = sp.Progress;
            float remaining = sp.RemainingSeconds;

            sp.Lock();
            sp.Tick(50f);

            Assert.AreEqual(progress, sp.Progress, 0.0001f);
            Assert.AreEqual(remaining, sp.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void Lock을_중복_호출해도_상태는_같다()
        {
            Spawner sp = NewSpawner();

            sp.Lock();
            sp.Lock();
            sp.Tick(100f);

            Assert.IsTrue(sp.IsLocked);
            Assert.AreEqual(0, _host.Spawns.Count);
        }

        [Test]
        public void 봉인은_카드_즉시_효과인_출력수_증가와_출력종_교체를_막지_않는다()
        {
            Spawner sp = NewSpawner();
            sp.Lock();

            sp.IncrementOutput();
            sp.ReplaceOutput(EMonster.Hex);

            Assert.AreEqual(2, sp.OutputCount);
            Assert.AreEqual(EMonster.Hex, sp.CurrentType);
            Assert.IsTrue(sp.IsLocked, "카드 효과가 봉인을 풀지 않는다(E-3)");
        }

        //# --- Unlock ---

        [Test]
        public void 첫_발사_전에_봉인했다_풀면_다음_Tick에_즉시_한번_스폰한다()
        {
            Spawner sp = NewSpawner();
            sp.Lock();
            sp.Tick(100f);

            sp.Unlock();
            sp.Tick(0.01f);

            Assert.AreEqual(1, _host.Spawns.Count);
            Assert.IsFalse(sp.IsLocked);
        }

        [Test]
        public void 주기_중간에_봉인했다_풀면_남은_시간과_무관하게_즉시_스폰한다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Tick(2f);
            sp.Lock();
            sp.Tick(60f);

            sp.Unlock();
            sp.Tick(0.01f);

            Assert.AreEqual(2, _host.Spawns.Count, "첫 발사 + 해제 버스트");
        }

        [Test]
        public void 해제_버스트는_한번뿐이고_그_다음_스폰은_한_주기_뒤다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Lock();
            sp.Unlock();
            sp.Tick(0.01f);
            Assert.AreEqual(2, _host.Spawns.Count);

            sp.Tick(sp.SpawnPeriod - 1f);
            Assert.AreEqual(2, _host.Spawns.Count, "한 주기 미만에서는 추가 스폰 없음");

            sp.Tick(sp.SpawnPeriod);
            Assert.AreEqual(3, _host.Spawns.Count);
        }

        [Test]
        public void 봉인하지_않은_스포너의_Unlock은_타이머를_건드리지_않는다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Tick(3f);
            float remaining = sp.RemainingSeconds;

            sp.Unlock();
            sp.Tick(0.01f);

            Assert.AreEqual(1, _host.Spawns.Count, "가짜 버스트 없음");
            Assert.AreEqual(remaining - 0.01f, sp.RemainingSeconds, 0.001f);
        }

        [Test]
        public void Unlock을_두번_불러도_버스트는_한번이다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Lock();

            sp.Unlock();
            sp.Unlock();
            sp.Tick(0.01f);

            Assert.AreEqual(2, _host.Spawns.Count);
        }

        [Test]
        public void 해제된_스포너를_다시_Unlock해도_재봉인되지_않는다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.Lock();
            sp.Unlock();

            sp.Unlock();

            Assert.IsFalse(sp.IsLocked);
        }

        [Test]
        public void 해제_버스트는_현재_출력수만큼_한_사이클로_위임된다()
        {
            Spawner sp = NewSpawnerInPeriodPhase();
            sp.IncrementOutput();
            sp.Lock();
            sp.Unlock();

            sp.Tick(0.01f);

            Assert.AreEqual((EMonster.Wisp, 2), _host.Spawns[1]);
        }

        //# --- 호스트 미바인딩 / 재진입 ---

        [Test]
        public void 호스트가_없으면_봉인과_무관하게_Tick은_아무것도_하지_않는다()
        {
            _go = new GameObject("SpawnerSealUT_NoHost");
            Spawner sp = _go.AddComponent<Spawner>();
            InvokeNonPublic(sp, "OnEnable");

            Assert.DoesNotThrow(() => sp.Tick(100f));
            Assert.AreEqual(0, _host.Spawns.Count);
        }

        [Test]
        public void OnEnable_재진입은_봉인을_풀고_타이머를_초기화한다()
        {
            Spawner sp = NewSpawner();
            sp.Lock();

            InvokeNonPublic(sp, "OnEnable");

            Assert.IsFalse(sp.IsLocked);
            sp.Tick(0.01f);
            Assert.AreEqual(1, _host.Spawns.Count, "재진입 후 첫 Tick 즉시 첫 발사");
        }

        //# --- SimpleMover.RuleSpeedScale ---

        private SimpleMover NewMover()
        {
            _go = new GameObject("MoverRuleUT");
            SimpleMover mover = _go.AddComponent<SimpleMover>();
            mover.Speed = 3f;
            return mover;
        }

        private static float StepDistance(SimpleMover mover, GameObject go)
        {
            go.transform.position = Vector3.zero;
            mover.MoveTo(new Vector3(1000f, 0f, 0f));
            InvokeNonPublic(mover, "FixedUpdate");
            return go.transform.position.x;
        }

        [Test]
        public void RuleSpeedScale_기본값은_1이다()
        {
            SimpleMover mover = NewMover();

            Assert.AreEqual(1f, mover.RuleSpeedScale, 0.0001f);
        }

        [Test]
        public void RuleSpeedScale은_이동_속도에_곱해진다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover, _go);

            mover.RuleSpeedScale = 0.5f;
            float slowed = StepDistance(mover, _go);

            Assert.Greater(baseline, 0f);
            Assert.AreEqual(baseline * 0.5f, slowed, baseline * 0.001f);
        }

        [Test]
        public void RuleSpeedScale은_SpeedScale과_독립된_채널로_곱연산된다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover, _go);

            mover.SpeedScale = 1.3f;
            mover.RuleSpeedScale = 0.5f;
            float both = StepDistance(mover, _go);

            Assert.AreEqual(baseline * 1.3f * 0.5f, both, baseline * 0.001f);
        }

        [Test]
        public void SpeedScale을_1로_리셋해도_RuleSpeedScale_감속은_유지된다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover, _go);
            mover.RuleSpeedScale = 0.5f;

            //# MonsterBuffService 는 매 tick SpeedScale 만 1 로 리셋한다.
            mover.SpeedScale = 1f;
            float slowed = StepDistance(mover, _go);

            Assert.AreEqual(baseline * 0.5f, slowed, baseline * 0.001f);
        }

        [Test]
        public void 풀_재사용_OnEnable은_RuleSpeedScale과_SpeedScale을_1로_리셋한다()
        {
            SimpleMover mover = NewMover();
            mover.RuleSpeedScale = 0.5f;
            mover.SpeedScale = 1.3f;
            mover.MoveTo(Vector3.one);

            InvokeNonPublic(mover, "OnEnable");

            Assert.AreEqual(1f, mover.RuleSpeedScale, 0.0001f);
            Assert.AreEqual(1f, mover.SpeedScale, 0.0001f);
            Assert.IsFalse(mover.IsMoving);
        }

        [Test]
        public void RuleSpeedScale이_0이면_이동하지_않는다()
        {
            SimpleMover mover = NewMover();
            mover.RuleSpeedScale = 0f;

            Assert.AreEqual(0f, StepDistance(mover, _go), 0.0001f);
        }
    }
}
