using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# ISpawnerProgress(Progress 프로퍼티) 및 ISpawnerOutputProvider(OnOutputTypeChanged 이벤트) 본격 스위트.
    //# Spawner 의 시각화 인터페이스 계약을 엣지·회귀·순서 케이스로 망라한다.
    public class SpawnerProgressTests
    {
        //# ISpawnerHost 최소 더블 — Tick 이 스폰 콜백을 요구하므로 Bind 에 필요.
        private class FakeSpawnerHost : ISpawnerHost
        {
            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count) { }
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# 리플렉션 헬퍼 — 필드 강제 주입 (EditMode 직렬화 우회).
        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드 존재 확인 (production 시그니처 변경 감지)");
            fi.SetValue(target, value);
        }

        //# OnEnable 리플렉션 호출 — EditMode 라이프사이클 보정.
        private static void InvokeOnEnable(Component c)
        {
            var mi = c.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        //# 구독 후 OnEnable 순서를 지원하는 Spawner 생성 — 이벤트 구독 전 초기화 옵션 포함.
        //# invokeOnEnable=false 이면 InvokeOnEnable 을 호출하지 않아 구독 → 호출 순서를 테스트에서 제어 가능.
        private Spawner CreateSpawnerRaw(EMonster outputType, float spawnPeriod, float initialDelay,
            bool invokeOnEnable = true)
        {
            var go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            var sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", spawnPeriod);
            SetPrivate(sp, "_initialDelay", initialDelay);
            if (invokeOnEnable) InvokeOnEnable(sp);
            return sp;
        }

        //# ===== ISpawnerProgress — 초기 지연 국면 (firstSpawnDone == false) =====

        //# 첫 발사 전이면 InitialDelay 가 아직 안 지나도 Progress = 0.
        [Test]
        public void 초기지연_국면에서_Progress는_0()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 5f);
            //# OnEnable 직후 _firstSpawnDone == false → 0f 고정.
            Assert.AreEqual(0f, sp.Progress, 0.0001f, "초기 지연 국면 Progress = 0");
        }

        //# InitialDelay 가 크더라도 Tick 전까지 Progress = 0.
        [Test]
        public void 초기지연_국면_Tick없이_Progress는_0()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 99f);
            Assert.AreEqual(0f, sp.Progress, 0.0001f, "Tick 없이도 초기 지연 국면 Progress = 0");
        }

        //# ===== ISpawnerProgress — 주기 국면 (firstSpawnDone == true) =====

        //# 주기 국면 진입 직후 _timer = 0 이면 Progress = 0.
        [Test]
        public void 주기국면_진입직후_Progress는_0()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var host = new FakeSpawnerHost();
            sp.Bind(host);
            sp.Tick(0f);    //# InitialDelay=0 → 첫 발사 → _firstSpawnDone=true, _timer=0
            Assert.AreEqual(0f, sp.Progress, 0.0001f, "첫 발사 직후 _timer=0 → Progress=0");
        }

        //# 주기 절반(_timer = period/2) 에서 Progress ≈ 0.5.
        [Test]
        public void 주기절반_Progress_0점5()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var host = new FakeSpawnerHost();
            sp.Bind(host);
            sp.Tick(0f);         //# 첫 발사
            sp.Tick(4.5f);       //# _timer = 4.5
            Assert.AreEqual(0.5f, sp.Progress, 0.001f, "주기 절반에서 Progress ≈ 0.5");
        }

        //# _timer 가 spawnPeriod 에 가까워지면 Progress → 1 에 근접.
        [Test]
        public void 주기_거의_만료_Progress_1에_근접()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var host = new FakeSpawnerHost();
            sp.Bind(host);
            sp.Tick(0f);         //# 첫 발사
            sp.Tick(8.99f);      //# _timer = 8.99
            float progress = sp.Progress;
            Assert.Greater(progress, 0.99f, "주기 거의 만료 — Progress > 0.99");
            Assert.LessOrEqual(progress, 1f, "Progress 는 1 초과 불가 (Clamp01)");
        }

        //# 스폰 직후 _timer 가 초과분만 남아 Progress 가 거의 0 으로 리셋.
        //# InitialDelay=0, period=9, Tick(0) → 첫발사, Tick(9.5) → 두 번째 발사(_timer=0.5).
        [Test]
        public void 스폰_직후_Progress_0에_가깝게_리셋()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var host = new FakeSpawnerHost();
            sp.Bind(host);
            sp.Tick(0f);         //# 첫 발사, _timer=0
            sp.Tick(9.5f);       //# 두 번째 발사 → _timer = 0.5
            float progress = sp.Progress;
            //# 초과분 0.5 / 주기 9 ≈ 0.0556
            Assert.Less(progress, 0.1f, "스폰 직후 Progress 는 초과분만 남아 거의 0");
        }

        //# ===== spawnPeriod == 0 방어 (divide-by-zero) =====

        //# _spawnPeriod = 0 이면 첫 발사 후 Progress = 1f (0 나누기 방어).
        [Test]
        public void spawnPeriod_0이면_Progress_1f()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 0f, 0f);
            //# _firstSpawnDone 을 강제 true — spawnPeriod=0 경계 단독 검증.
            SetPrivate(sp, "_firstSpawnDone", true);
            Assert.AreEqual(1f, sp.Progress, 0.0001f, "spawnPeriod=0 이면 Progress=1 (divide-by-zero 방어)");
        }

        //# _firstSpawnDone == false + spawnPeriod == 0 이면 여전히 0f.
        [Test]
        public void spawnPeriod_0_firstSpawnDone_false이면_Progress_0f()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 0f, 0f);
            //# OnEnable 에서 _firstSpawnDone = false 로 초기화됨.
            Assert.AreEqual(0f, sp.Progress, 0.0001f,
                "spawnPeriod=0 이어도 초기 지연 국면이면 Progress=0");
        }

        //# ===== ISpawnerOutputProvider — OnEnable 시 이벤트 발행 =====

        //# 이벤트를 먼저 구독한 후 OnEnable 을 호출해야 수신 가능 (Spawner.OnEnable 이 Invoke 함).
        [Test]
        public void OnEnable_시_현재_출력종으로_이벤트_1회_발행()
        {
            //# invokeOnEnable=false — 구독 후 OnEnable 을 직접 호출.
            var sp = CreateSpawnerRaw(EMonster.Golem, 9f, 0f, invokeOnEnable: false);

            var received = new List<EMonster>();
            sp.OnOutputTypeChanged += t => received.Add(t);

            InvokeOnEnable(sp);

            Assert.AreEqual(1, received.Count, "OnEnable 에서 이벤트 1회 발행");
            Assert.AreEqual(EMonster.Golem, received[0], "발행 값은 초기 _outputType 과 일치");
        }

        //# OnEnable 에서 이전 종이 아닌 현재 직렬화 값(_outputType)으로 발행.
        [Test]
        public void OnEnable_발행값은_직렬화_outputType()
        {
            var sp = CreateSpawnerRaw(EMonster.Orc, 9f, 0f, invokeOnEnable: false);
            EMonster? received = null;
            sp.OnOutputTypeChanged += t => received = t;
            InvokeOnEnable(sp);
            Assert.AreEqual(EMonster.Orc, received, "OnEnable 발행 값은 직렬화 _outputType");
        }

        //# ===== ISpawnerOutputProvider — ReplaceOutput 시 이벤트 발행 =====

        //# ReplaceOutput 시 변경된 종으로 이벤트 발행.
        [Test]
        public void ReplaceOutput_이벤트_변경종으로_발행()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var received = new List<EMonster>();
            sp.OnOutputTypeChanged += t => received.Add(t);

            sp.ReplaceOutput(EMonster.Golem);

            //# OnEnable(CreateSpawnerRaw 내부 InvokeOnEnable) 이후 구독 → OnEnable 발행은 수신 못함.
            //# ReplaceOutput 에서 1회만 수신.
            Assert.AreEqual(1, received.Count, "ReplaceOutput 에서 이벤트 1회 발행");
            Assert.AreEqual(EMonster.Golem, received[0], "발행 값은 변경 종");
        }

        //# 이벤트 발행 값이 이전 종이 아닌 새 종인지 확인.
        [Test]
        public void ReplaceOutput_발행값이_이전종_아닌_새종()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            EMonster? lastReceived = null;
            sp.OnOutputTypeChanged += t => lastReceived = t;

            sp.ReplaceOutput(EMonster.Archer);

            Assert.AreEqual(EMonster.Archer, lastReceived,
                "발행 값이 이전 종(Slime) 이 아닌 새 종(Archer)");
            Assert.AreNotEqual(EMonster.Slime, lastReceived,
                "이전 종이 발행되면 안 됨");
        }

        //# 연속 ReplaceOutput — Slime→Golem→Orc 각각 1회씩 발행.
        [Test]
        public void ReplaceOutput_연속_각각_1회씩_발행()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            var received = new List<EMonster>();
            sp.OnOutputTypeChanged += t => received.Add(t);

            sp.ReplaceOutput(EMonster.Golem);
            sp.ReplaceOutput(EMonster.Orc);

            Assert.AreEqual(2, received.Count, "ReplaceOutput 2회 → 이벤트 2회 발행");
            Assert.AreEqual(EMonster.Golem, received[0], "첫 번째 발행 = Golem");
            Assert.AreEqual(EMonster.Orc, received[1], "두 번째 발행 = Orc");
        }

        //# ===== CurrentType 과 이벤트 일관성 =====

        //# ReplaceOutput 후 CurrentType 과 이벤트 발행 값이 일치.
        [Test]
        public void ReplaceOutput_CurrentType과_이벤트값_일치()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);
            EMonster? eventVal = null;
            sp.OnOutputTypeChanged += t => eventVal = t;

            sp.ReplaceOutput(EMonster.Bat);

            Assert.AreEqual(EMonster.Bat, sp.CurrentType, "CurrentType 갱신");
            Assert.AreEqual(sp.CurrentType, eventVal, "CurrentType == 이벤트 발행 값");
        }

        //# ===== 이벤트 구독 없이 ReplaceOutput — NullReferenceException 없음 =====

        [Test]
        public void ReplaceOutput_구독자_없으면_예외없음()
        {
            var sp = CreateSpawnerRaw(EMonster.Slime, 9f, 0f);

            Assert.DoesNotThrow(() => sp.ReplaceOutput(EMonster.Golem),
                "구독자 없을 때 ReplaceOutput 은 예외 없이 무동작");
        }
    }
}
