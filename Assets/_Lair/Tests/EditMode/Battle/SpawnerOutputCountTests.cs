using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 스포너 상태 UI 자체 검증 — IncrementOutput 호출 시 OnOutputCountChanged 이벤트 발행 검증.
    //# 본격 스위트는 test-engineer. 본 테스트는 gameplay-programmer "정상 + 엣지 1" 수준.
    public class SpawnerOutputCountTests
    {
        private GameObject _spawnerGo;

        [TearDown]
        public void TearDown()
        {
            if (_spawnerGo != null) Object.DestroyImmediate(_spawnerGo);
        }

        private Spawner CreateSpawner()
        {
            _spawnerGo = new GameObject("SpawnerUT");
            Spawner sp = _spawnerGo.AddComponent<Spawner>();
            //# OnEnable 강제 호출 — EditMode 라이프사이클 한계 보정 (SpawnerTests 와 동일 패턴).
            MethodInfo mi = typeof(Spawner).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(sp, null);
            return sp;
        }

        //# 정상 — IncrementOutput 호출 시 OnOutputCountChanged 가 새 OutputCount 값과 함께 발행.
        [Test]
        public void IncrementOutput_OnOutputCountChanged_발행()
        {
            Spawner sp = CreateSpawner();
            int captured = -1;
            sp.OnOutputCountChanged += n => captured = n;

            sp.IncrementOutput();

            Assert.AreEqual(2, captured, "1 → 2 로 증가하면 인자 = 2");
            Assert.AreEqual(2, sp.OutputCount, "OutputCount 프로퍼티도 2");
        }

        //# 엣지 — OnEnable 시점에는 OnOutputCountChanged 발행되지 않아야 (기획서 §4.5).
        //# VM 의 AttachSpawners 가 초기값을 직접 폴링으로 채우므로 broadcast 가 무의미.
        [Test]
        public void OnEnable_시점에는_OnOutputCountChanged_미발행()
        {
            //# 미리 핸들러 부착할 수 없음(OnEnable 이 컴포넌트 Add 시점에 1회 자동 발행).
            //# 그러므로 직접 OnEnable 재호출로 검증 — Add 한 뒤 핸들러 부착 후 OnEnable 재호출.
            _spawnerGo = new GameObject("SpawnerUT_OnEnable");
            Spawner sp = _spawnerGo.AddComponent<Spawner>();
            //# 1차 OnEnable 은 컴포넌트 Add 직후 Unity 가 호출 (또는 EditMode 한계로 호출 안 될 수도).

            int callCount = 0;
            sp.OnOutputCountChanged += _ => callCount++;

            //# OnEnable 명시 호출 — 기획서 §4.5 의 "OnEnable 에서 OnOutputCountChanged 발행하지 않는다" 확인.
            MethodInfo mi = typeof(Spawner).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(sp, null);

            Assert.AreEqual(0, callCount, "OnEnable 직접 호출 시 OnOutputCountChanged 미발행");
        }
    }
}
