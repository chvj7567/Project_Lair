using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.Battle
{
    //# BattleController.HandleHeroReachedCenter / Update 게이트 동작 검증 (EditMode).
    //# Start() 풀스택은 PlayMode 통합 영역. 본 스위트는 1회 동작 보장 + 중복 호출 idempotent + Spawner Tick 게이트.
    //# HandleHeroReachedCenter 의 모든 의존성(_hero, _heroEntryDriver, _clock) 은 ?.* 가드 — null 안전.
    public class BattleControllerEntryTests
    {
        //# Update 게이트 테스트용 ISpawnerHost — Spawner 가 Bind(host, null) 받아야 Tick 진입.
        //# 본 스위트는 BattleController 자체의 Update 게이트만 검증하므로 spawn 결과는 무시.
        private class StubSpawnerHost : ISpawnerHost
        {
            public int CallCount { get; private set; }
            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
                => CallCount++;
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private BattleController CreateController()
        {
            GameObject go = new GameObject("BattleControllerUT");
            BattleController bc = go.AddComponent<BattleController>();
            _spawned.Add(go);
            return bc;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"BattleController.{field} 필드 존재 — 시그니처 변경 감지");
            fi.SetValue(target, value);
        }

        private static T GetPrivate<T>(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"BattleController.{field} 필드 존재");
            return (T)fi.GetValue(target);
        }

        private static void InvokeHandleHeroReachedCenter(BattleController bc)
        {
            MethodInfo mi = typeof(BattleController).GetMethod("HandleHeroReachedCenter",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "BattleController.HandleHeroReachedCenter 메서드 존재");
            mi.Invoke(bc, null);
        }

        //# 정상 — HandleHeroReachedCenter 호출 시 _spawnersActive=true + _clock.IsRunning=true.
        [Test]
        public void HandleHeroReachedCenter_호출시_spawnersActive_true_및_clock_시작()
        {
            BattleController bc = CreateController();
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            //# 사전 상태 — _spawnersActive false, clock 미시작.
            Assert.IsFalse(GetPrivate<bool>(bc, "_spawnersActive"), "초기 false");
            Assert.IsFalse(clock.IsRunning, "초기 미시작");

            InvokeHandleHeroReachedCenter(bc);

            Assert.IsTrue(GetPrivate<bool>(bc, "_spawnersActive"),
                "HandleHeroReachedCenter 후 _spawnersActive=true");
            Assert.IsTrue(clock.IsRunning,
                "HandleHeroReachedCenter 후 _clock.Start() 호출 → IsRunning=true");
        }

        //# 엣지 — 중복 호출시 첫 호출만 동작 (early return 으로 _spawnersActive 변동 없음).
        //# _clock 도 첫 호출에서 Start() 됐고 두 번째 호출은 진입 자체가 안 됨 → Elapsed=0 그대로.
        [Test]
        public void HandleHeroReachedCenter_중복호출시_idempotent()
        {
            BattleController bc = CreateController();
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            InvokeHandleHeroReachedCenter(bc);
            Assert.IsTrue(GetPrivate<bool>(bc, "_spawnersActive"), "첫 호출로 true");
            Assert.IsTrue(clock.IsRunning);

            //# 첫 호출 후 Tick — Elapsed 진행.
            clock.Tick(1.5f);
            Assert.AreEqual(1.5f, clock.Elapsed, 0.001f);

            //# 두 번째 호출 — early return. clock.Start() 재호출되면 Elapsed 가 0 으로 리셋되는데,
            //# early return 이라 Elapsed 가 그대로 1.5f 유지돼야 함.
            InvokeHandleHeroReachedCenter(bc);

            Assert.IsTrue(GetPrivate<bool>(bc, "_spawnersActive"), "여전히 true");
            Assert.AreEqual(1.5f, clock.Elapsed, 0.001f,
                "early return 이라 clock.Start() 재호출 안 됨 → Elapsed 유지 (중복 호출 idempotent)");
        }

        //# 엣지 — _clock=null 이어도 ?.Start() 가드로 예외 없이 동작. _spawnersActive 는 그대로 set.
        [Test]
        public void HandleHeroReachedCenter_clock_null_이어도_예외없이_spawnersActive_true()
        {
            BattleController bc = CreateController();
            //# _clock 미할당 — 기본 null.

            Assert.DoesNotThrow(() => InvokeHandleHeroReachedCenter(bc),
                "_clock null 이어도 ?. 가드로 예외 없음");
            Assert.IsTrue(GetPrivate<bool>(bc, "_spawnersActive"),
                "_clock null 이어도 _spawnersActive=true 는 set");
        }

        //# 엣지 — _heroEntryDriver 가 할당돼 있으면 호출 시 enabled=false 로 전환.
        //# (1회 동작 후 자기 자신을 끄는 보강 — driver 자체도 enabled=false 호출하지만 안전망.)
        [Test]
        public void HandleHeroReachedCenter_heroEntryDriver_있으면_비활성화()
        {
            BattleController bc = CreateController();
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            //# 별도 GameObject 에 HeroEntryDriver 부착 (영웅 풀 Pop 시뮬).
            GameObject heroGo = new GameObject("HeroUT");
            Lair.Character.HeroEntryDriver driver = heroGo.AddComponent<Lair.Character.HeroEntryDriver>();
            driver.enabled = true;
            _spawned.Add(heroGo);

            SetPrivate(bc, "_heroEntryDriver", driver);

            InvokeHandleHeroReachedCenter(bc);

            Assert.IsFalse(driver.enabled,
                "HandleHeroReachedCenter 후 _heroEntryDriver.enabled = false (1회 동작 보강)");
        }

        //# ===== Update 게이트 (Spawner.Tick 차단) =====

        //# Update 의 핵심 게이트 — Hero entry 전(_spawnersActive=false) 에는 Spawner.Tick 호출 안 됨.
        //# BattleClock 도 미시작이라 Tick(dt) 은 Elapsed 안 증가시킴.
        //# 영웅 entry 동안 게임 진행이 정지되어야 한다는 spec §6.2 의 단일 진실 회귀.
        [Test]
        public void Update_spawnersActive_false_이면_Spawner_Tick_호출_안됨()
        {
            BattleController bc = CreateController();
            //# 모델 + 클럭 셋업. _clock 은 미시작 — Update 에서 clock.Tick 호출돼도 IsRunning false 로 early return.
            BattleStateModel model = new BattleStateModel();
            SetPrivate(bc, "_model", model);
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            //# Spawner 1개 셋업 — Bind 까지 해서 Tick 진입 가능 상태.
            StubSpawnerHost host = new StubSpawnerHost();
            Spawner sp = CreateBoundSpawner(host);
            SetPrivate(bc, "_spawners", new Spawner[] { sp });

            //# _spawnersActive = false (초기값). Update 가 Spawner.Tick 진입 안 함.
            Assert.IsFalse(GetPrivate<bool>(bc, "_spawnersActive"));

            //# Update 1회 호출 (dt 임의 — Time.deltaTime 0 가능성 회피).
            //# 한 Update 호출에 영향을 줄 만큼 클럭이 Elapsed 변동 없는지도 검증.
            InvokeUpdate(bc);

            Assert.AreEqual(0, host.CallCount,
                "_spawnersActive=false → Update 안 Spawner.Tick 호출 안 됨 (entry 동안 spawn 차단)");
            Assert.IsFalse(clock.IsRunning,
                "Hero entry 전 _clock.Start() 호출 안 됨 → IsRunning false");
            Assert.AreEqual(0f, clock.Elapsed, 0.0001f,
                "clock.IsRunning=false 라 clock.Tick(dt) 이 early return → Elapsed 0 유지");
        }

        //# Update 게이트 활성화 — HandleHeroReachedCenter 호출 후 Spawner.Tick 진입 + clock Tick 진행.
        //# spec §6.2 — 영웅이 zone 중심 도달 후 게임 진행 시작.
        [Test]
        public void Update_HandleHeroReachedCenter_호출후_Spawner_Tick_진입()
        {
            BattleController bc = CreateController();
            BattleStateModel model = new BattleStateModel();
            SetPrivate(bc, "_model", model);
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            //# InitialDelay 0, 주기 9 — 첫 Update 의 dt(=Time.deltaTime) 가 0 이라도 첫 발사.
            StubSpawnerHost host = new StubSpawnerHost();
            Spawner sp = CreateBoundSpawner(host);
            SetPrivate(bc, "_spawners", new Spawner[] { sp });

            //# 게이트 열기 — _spawnersActive=true + clock.Start().
            InvokeHandleHeroReachedCenter(bc);
            Assert.IsTrue(GetPrivate<bool>(bc, "_spawnersActive"));
            Assert.IsTrue(clock.IsRunning);

            //# Update 호출 → Spawner.Tick 진입. InitialDelay 0 이라 첫 Tick 에 1발.
            InvokeUpdate(bc);

            Assert.AreEqual(1, host.CallCount,
                "_spawnersActive=true 이후 첫 Update — Spawner.Tick 진입 → 첫 발사 (InitialDelay 0)");
        }

        //# 엣지 — 전투 종료(_model.Result != None) 이면 _spawnersActive 가 true 라도 Spawner.Tick 안 됨.
        //# 게이트 조건 3개(_spawnersActive && Result==None && _spawners!=null) 의 AND 회귀.
        [Test]
        public void Update_battleResult_None_아니면_Spawner_Tick_차단()
        {
            BattleController bc = CreateController();
            BattleStateModel model = new BattleStateModel();
            model.Result = BattleResult.Win;   //# 전투 종료 상태.
            SetPrivate(bc, "_model", model);
            BattleClock clock = new BattleClock(300f);
            SetPrivate(bc, "_clock", clock);

            StubSpawnerHost host = new StubSpawnerHost();
            Spawner sp = CreateBoundSpawner(host);
            SetPrivate(bc, "_spawners", new Spawner[] { sp });

            //# spawnersActive=true 강제 (HandleHeroReachedCenter 이미 호출됐다고 가정).
            SetPrivate(bc, "_spawnersActive", true);

            InvokeUpdate(bc);

            Assert.AreEqual(0, host.CallCount,
                "Result != None → Spawner.Tick 호출 안 됨 (전투 종료 후 spawn 차단)");
        }

        //# ===== 헬퍼 =====

        //# Spawner 를 host 와 Bind 한 상태로 생성 — Tick 진입 가능 상태.
        private Spawner CreateBoundSpawner(ISpawnerHost host)
        {
            GameObject go = new GameObject("StubSpawnerUT");
            Spawner sp = go.AddComponent<Spawner>();
            _spawned.Add(go);

            //# Spawner 의 _outputType=Wisp / _spawnPeriod=9 / _initialDelay=0 가 기본값.
            //# 명시적으로 InitialDelay=0 셋업 — 첫 Tick 에 즉시 발사 보장.
            SetPrivate(sp, "_initialDelay", 0f);
            SetPrivate(sp, "_spawnPeriod", 9f);

            //# OnEnable 리플렉션 호출 — _currentType/_outputCount/_timer/_firstSpawnDone 초기화.
            MethodInfo onEnableMi = typeof(Spawner).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMi?.Invoke(sp, null);

            sp.Bind(host, null);
            return sp;
        }

        private static void InvokeUpdate(BattleController bc)
        {
            MethodInfo mi = typeof(BattleController).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "BattleController.Update 메서드 존재");
            mi.Invoke(bc, null);
        }
    }
}
