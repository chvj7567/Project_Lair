using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 스포너 상태 UI — 영역 B 보강 (OnOutputCountChanged / OnOutputTypeChanged 이벤트 경계).
    //#
    //# gameplay-programmer 의 SpawnerOutputCountTests / SpawnerTests 가 다루지 않는 엣지·회귀:
    //#  - ReplaceOutput 은 OnOutputCountChanged 를 발행하지 않는다 (출력 종만 변경).
    //#  - IncrementOutput N 회 호출 시 정확히 N 회 발행 + 단조 증가.
    //#  - OnEnable 시 _outputCount 가 1 로 리셋되지만 OnOutputCountChanged 는 미발행 (기획서 §4.5).
    //#  - 다중 구독자 — 모두에게 동일 인자 전달.
    //#  - 구독 해제 후 IncrementOutput 호출 시 해제된 핸들러 미호출 (누수 방지).
    public class SpawnerOutputEventTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Spawner CreateSpawner(EMonster type = EMonster.Wisp)
        {
            GameObject go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", type);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            InvokeOnEnable(sp);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        private static void InvokeOnEnable(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Spawner.OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        //# ===== ReplaceOutput vs IncrementOutput 분리 =====

        //# 회귀 — ReplaceOutput 은 OnOutputCountChanged 를 발행하지 않는다.
        //# (출력 종만 바뀌고 출력 수는 유지 — 두 이벤트는 의미적으로 분리).
        [Test]
        public void ReplaceOutput_OnOutputCountChanged_미발행()
        {
            Spawner sp = CreateSpawner();
            int countCallback = 0;
            sp.OnOutputCountChanged += _ => countCallback++;

            sp.ReplaceOutput(EMonster.Wraith);

            Assert.AreEqual(0, countCallback,
                "ReplaceOutput 은 출력 종만 바꿔야 — OnOutputCountChanged 미발행");
        }

        //# 회귀 — ReplaceOutput 은 OnOutputTypeChanged 만 발행 (1회).
        [Test]
        public void ReplaceOutput_OnOutputTypeChanged_만_발행()
        {
            Spawner sp = CreateSpawner();
            int typeCallback = 0;
            EMonster? lastType = null;
            sp.OnOutputTypeChanged += t => { typeCallback++; lastType = t; };

            sp.ReplaceOutput(EMonster.Plague);

            Assert.AreEqual(1, typeCallback, "ReplaceOutput → OnOutputTypeChanged 1회");
            Assert.AreEqual(EMonster.Plague, lastType);
        }

        //# 회귀 — IncrementOutput 은 OnOutputTypeChanged 를 발행하지 않는다 (출력 종 미변).
        [Test]
        public void IncrementOutput_OnOutputTypeChanged_미발행()
        {
            Spawner sp = CreateSpawner();
            int typeCallback = 0;
            sp.OnOutputTypeChanged += _ => typeCallback++;

            sp.IncrementOutput();
            sp.IncrementOutput();

            Assert.AreEqual(0, typeCallback,
                "IncrementOutput 은 출력 수만 변경 — OnOutputTypeChanged 미발행");
        }

        //# ===== IncrementOutput 다중 호출 =====

        //# 정상 — IncrementOutput N 회 호출 → 정확히 N 회 이벤트 발행 + 인자 단조 증가.
        [Test]
        public void IncrementOutput_3회_호출시_3회_발행_인자_단조증가()
        {
            Spawner sp = CreateSpawner();
            List<int> values = new List<int>();
            sp.OnOutputCountChanged += n => values.Add(n);

            sp.IncrementOutput();   //# 1 → 2
            sp.IncrementOutput();   //# 2 → 3
            sp.IncrementOutput();   //# 3 → 4

            Assert.AreEqual(3, values.Count, "3회 호출 → 3회 발행");
            Assert.AreEqual(new List<int> { 2, 3, 4 }, values, "인자가 단조 증가");
            Assert.AreEqual(4, sp.OutputCount, "OutputCount 프로퍼티 = 4");
        }

        //# ===== OnEnable 동작 (기획서 §4.5) =====

        //# 회귀 — OnEnable 시 _outputCount 가 1 로 리셋되지만 OnOutputCountChanged 미발행.
        //# (gameplay-programmer 가 추가한 케이스 — 본 테스트는 IncrementOutput 후 재호출 시나리오로 보강).
        [Test]
        public void Increment_후_OnEnable_재호출시_count_1로_리셋_이벤트_미발행()
        {
            Spawner sp = CreateSpawner();
            sp.IncrementOutput();   //# 2
            sp.IncrementOutput();   //# 3
            Assert.AreEqual(3, sp.OutputCount);

            int callCount = 0;
            sp.OnOutputCountChanged += _ => callCount++;

            //# 씬 재진입 시뮬레이션.
            InvokeOnEnable(sp);

            Assert.AreEqual(1, sp.OutputCount, "OnEnable 후 _outputCount = 1 (Rule 12 풀 재사용 정책)");
            Assert.AreEqual(0, callCount, "OnEnable 동안 OnOutputCountChanged 발행 안 함 (§4.5)");
        }

        //# 정상 — OnEnable 후에도 IncrementOutput 이 정상 동작 (이벤트 발행, 값 증가).
        [Test]
        public void OnEnable_재호출_후_IncrementOutput_정상_동작()
        {
            Spawner sp = CreateSpawner();
            sp.IncrementOutput();
            sp.IncrementOutput();
            InvokeOnEnable(sp);

            int captured = -1;
            sp.OnOutputCountChanged += n => captured = n;
            sp.IncrementOutput();

            Assert.AreEqual(2, captured, "리셋 후 첫 Increment → 2");
            Assert.AreEqual(2, sp.OutputCount);
        }

        //# ===== 다중 구독자 / 구독 해제 =====

        //# 정상 — 두 구독자에게 같은 인자 발행.
        [Test]
        public void IncrementOutput_다중_구독자에게_동일_인자_발행()
        {
            Spawner sp = CreateSpawner();
            int a = -1, b = -1;
            sp.OnOutputCountChanged += n => a = n;
            sp.OnOutputCountChanged += n => b = n;

            sp.IncrementOutput();

            Assert.AreEqual(2, a, "구독자 A");
            Assert.AreEqual(2, b, "구독자 B");
        }

        //# 회귀 — 구독 해제한 핸들러는 IncrementOutput 후 호출 안 됨 (VM.DetachSpawners 누수 방지).
        [Test]
        public void OnOutputCountChanged_구독_해제후_핸들러_미호출()
        {
            Spawner sp = CreateSpawner();
            int call = 0;
            System.Action<int> handler = _ => call++;
            sp.OnOutputCountChanged += handler;

            sp.IncrementOutput();   //# 1회 발행 — call=1
            Assert.AreEqual(1, call);

            //# 해제 후 추가 호출.
            sp.OnOutputCountChanged -= handler;
            sp.IncrementOutput();
            sp.IncrementOutput();

            Assert.AreEqual(1, call, "해제 후 핸들러 호출 안 됨 (누수 방지)");
        }

        //# 회귀 — OnOutputTypeChanged 도 구독 해제 후 미호출.
        [Test]
        public void OnOutputTypeChanged_구독_해제후_핸들러_미호출()
        {
            Spawner sp = CreateSpawner();
            int call = 0;
            System.Action<EMonster> handler = _ => call++;
            sp.OnOutputTypeChanged += handler;
            //# 기준 호출.
            sp.ReplaceOutput(EMonster.Wraith);
            Assert.AreEqual(1, call);

            sp.OnOutputTypeChanged -= handler;
            sp.ReplaceOutput(EMonster.Plague);

            Assert.AreEqual(1, call, "해제 후 미호출");
        }
    }
}
