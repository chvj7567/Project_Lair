using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# 풀 재사용(OnEnable 재호출) 시 MonsterTag 가 자기 Transform 의 IsEngaging 을 false 로 리셋.
    //# 시드: gameplay-programmer. 본 스위트는 다회 호출 idempotent + 미등록 안전망 회귀.
    public class MonsterTagResetTests
    {
        private GameObject _go;

        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        private static void InvokeOnEnable(MonsterTag tag)
        {
            MethodInfo mi = typeof(MonsterTag).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "MonsterTag.OnEnable 메서드 존재 — 시그니처 변경 감지");
            mi.Invoke(tag, null);
        }

        [Test]
        public void OnEnable_재호출시_IsEngaging_false_리셋()
        {
            _go = new GameObject("MonsterUT");
            MonsterTag tag = _go.AddComponent<MonsterTag>();
            FakeHealth health = new FakeHealth();

            //# Register + Engaging 상태 진입 (zone 안에 있는 몬스터 가정)
            CharacterRegistry.RegisterMonster(_go.transform, health);
            CharacterRegistry.SetMonsterEngaging(_go.transform, true);
            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging);

            //# 풀 재사용 시뮬레이션 — OnEnable 리플렉션 호출.
            InvokeOnEnable(tag);

            Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging,
                "OnEnable 재호출 후 IsEngaging 가 false (Marching 상태로 복귀)");
        }

        //# 엣지 — 풀 Push/Pop 이 N회 반복돼도 매번 동일 동작 (idempotent).
        //# 각 OnEnable 사이에 SetMonsterEngaging(true) 가 끼어들어도 다음 OnEnable 이 또 리셋.
        [Test]
        public void OnEnable_다회_재호출_idempotent()
        {
            _go = new GameObject("MonsterUT");
            MonsterTag tag = _go.AddComponent<MonsterTag>();
            CharacterRegistry.RegisterMonster(_go.transform, new FakeHealth());

            //# 풀 Push → Pop → Engaging → Push → Pop → Engaging ... 10 사이클.
            for (int i = 0; i < 10; ++i)
            {
                //# 사이클마다 zone 진입 시뮬 (true) → OnEnable 재호출이 false 로 리셋.
                CharacterRegistry.SetMonsterEngaging(_go.transform, true);
                Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging,
                    $"사이클 {i} 진입 후 IsEngaging=true");

                InvokeOnEnable(tag);

                Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging,
                    $"사이클 {i} OnEnable 후 IsEngaging=false 리셋");
            }

            //# 누적 부작용 없음 — Entry 1개 유지.
            Assert.AreEqual(1, CharacterRegistry.Monsters.Count,
                "다회 OnEnable 이 Entry 를 추가하지 않음");
        }

        //# 엣지 — CharacterRegistry 에 등록 안 된 Transform 의 MonsterTag 가 OnEnable 호출돼도 예외 없이 무동작.
        //# 풀 Pop 직후 MonsterTargetProvider 등록 *이전* 시점 안전망.
        [Test]
        public void OnEnable_미등록_상태에서도_예외없이_무동작()
        {
            _go = new GameObject("UnregisteredMonster");
            MonsterTag tag = _go.AddComponent<MonsterTag>();
            //# CharacterRegistry.RegisterMonster 호출 안 함 — 미등록.

            Assert.DoesNotThrow(() => InvokeOnEnable(tag),
                "미등록 Transform 의 MonsterTag.OnEnable — SetMonsterEngaging 가 no-op");
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count,
                "Monsters 리스트 변동 없음");
        }
    }
}
