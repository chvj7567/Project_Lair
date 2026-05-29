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
            Transform t = new GameObject("m1").transform;
            FakeHealth h = new FakeHealth();
            CharacterRegistry.RegisterMonster(t, h);

            Assert.AreEqual(1, CharacterRegistry.Monsters.Count);

            CharacterRegistry.UnregisterMonster(t);
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count);

            Object.DestroyImmediate(t.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_거리순_가장_가까운()
        {
            Transform near = new GameObject("near").transform; near.position = new Vector3(1, 0, 0);
            Transform far  = new GameObject("far").transform;  far.position  = new Vector3(5, 0, 0);
            CharacterRegistry.RegisterMonster(near, new FakeHealth());
            CharacterRegistry.RegisterMonster(far, new FakeHealth());
            //# BattleZone 진입 후 상태 시뮬레이션 — 두 몬스터 모두 Engaging.
            CharacterRegistry.SetMonsterEngaging(near, true);
            CharacterRegistry.SetMonsterEngaging(far, true);

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth _);

            Assert.IsTrue(found);
            Assert.AreSame(near, t);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_빈_레지스트리_false()
        {
            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth h);

            Assert.IsFalse(found);
            Assert.IsNull(t);
            Assert.IsNull(h);
        }

        [Test]
        public void TryFindNearestMonster_죽은_적_제외()
        {
            Transform alive = new GameObject("alive").transform; alive.position = new Vector3(5, 0, 0);
            Transform dead  = new GameObject("dead").transform;  dead.position  = new Vector3(1, 0, 0);
            FakeHealth aliveHp = new FakeHealth();
            FakeHealth deadHp = new FakeHealth();
            deadHp.ForceSetCurrent(0);
            CharacterRegistry.RegisterMonster(alive, aliveHp);
            CharacterRegistry.RegisterMonster(dead, deadHp);
            //# BattleZone 진입 후 상태 시뮬레이션 — 두 몬스터 모두 Engaging.
            CharacterRegistry.SetMonsterEngaging(alive, true);
            CharacterRegistry.SetMonsterEngaging(dead, true);

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth _);

            Assert.IsTrue(found);
            Assert.AreSame(alive, t, "더 가까운 dead 대신 alive 가 선택돼야 함");

            Object.DestroyImmediate(alive.gameObject);
            Object.DestroyImmediate(dead.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_Marching_몬스터_제외()
        {
            Transform marching = new GameObject("marching").transform; marching.position = new Vector3(1, 0, 0);
            Transform engaging = new GameObject("engaging").transform; engaging.position = new Vector3(5, 0, 0);
            CharacterRegistry.RegisterMonster(marching, new FakeHealth());
            CharacterRegistry.RegisterMonster(engaging, new FakeHealth());
            //# marching 은 IsEngaging=false (default), engaging 은 true 로 전환.
            CharacterRegistry.SetMonsterEngaging(engaging, true);

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth _);

            Assert.IsTrue(found);
            Assert.AreSame(engaging, t, "Marching(IsEngaging=false) 는 검색 결과에서 제외 — 더 멀어도 Engaging 이 선택");

            Object.DestroyImmediate(marching.gameObject);
            Object.DestroyImmediate(engaging.gameObject);
        }

        [Test]
        public void SetMonsterEngaging_미등록_몬스터_무동작()
        {
            //# 등록 안 된 Transform 에 SetMonsterEngaging 호출 — 예외 없이 무동작.
            Transform t = new GameObject("unregistered").transform;
            Assert.DoesNotThrow(() => CharacterRegistry.SetMonsterEngaging(t, true));
            Object.DestroyImmediate(t.gameObject);
        }

        //# 엣지 — 모든 몬스터가 Marching(IsEngaging=false) 일 때 TryFindNearestMonster 는
        //# false 를 반환하고 out 매개변수는 null. 영웅 entry 중에 영웅 AI 가 wandering 하지 않도록
        //# 보장하는 핵심 안전망 (회귀).
        [Test]
        public void TryFindNearestMonster_모두_Marching이면_false_out은_null()
        {
            Transform m1 = new GameObject("m1").transform; m1.position = new Vector3(1, 0, 0);
            Transform m2 = new GameObject("m2").transform; m2.position = new Vector3(2, 0, 0);
            Transform m3 = new GameObject("m3").transform; m3.position = new Vector3(3, 0, 0);
            CharacterRegistry.RegisterMonster(m1, new FakeHealth());
            CharacterRegistry.RegisterMonster(m2, new FakeHealth());
            CharacterRegistry.RegisterMonster(m3, new FakeHealth());
            //# 누구도 SetMonsterEngaging(true) 호출 안 함 — 전부 Marching.

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth h);

            Assert.IsFalse(found, "모든 몬스터가 Marching — 후보 0");
            Assert.IsNull(t, "out Transform null");
            Assert.IsNull(h, "out IHealth null");

            Object.DestroyImmediate(m1.gameObject);
            Object.DestroyImmediate(m2.gameObject);
            Object.DestroyImmediate(m3.gameObject);
        }

        //# 엣지 — 가까운 Marching 몬스터가 있어도 더 먼 Engaging 몬스터가 선택된다.
        //# 거리순 정렬보다 IsEngaging 필터가 선행하는 동작을 회귀 박제.
        [Test]
        public void TryFindNearestMonster_가까운Marching_먼Engaging이면_먼것_선택()
        {
            //# Marching — 가까움 (X=1).
            Transform near = new GameObject("near_marching").transform; near.position = new Vector3(1, 0, 0);
            //# Engaging — 멀음 (X=10).
            Transform far  = new GameObject("far_engaging").transform;  far.position  = new Vector3(10, 0, 0);
            CharacterRegistry.RegisterMonster(near, new FakeHealth());
            CharacterRegistry.RegisterMonster(far, new FakeHealth());
            CharacterRegistry.SetMonsterEngaging(far, true);
            //# near 는 Marching 유지 (default false).

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out Transform t, out IHealth _);

            Assert.IsTrue(found, "Engaging 몬스터가 있으므로 found=true");
            Assert.AreSame(far, t,
                "거리상 near 가 더 가깝지만 Marching 이라 제외 — Engaging 인 far 가 선택");

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        //# 엣지 — TryFindNearestHero 는 requireEngaging=false 라 IsEngaging 필드와 무관.
        //# 영웅이 잘못 SetMonsterEngaging 같은 API 로 false 가 돼도 검색은 정상.
        [Test]
        public void TryFindNearestHero_IsEngaging_필드_무관()
        {
            Transform hero = new GameObject("hero").transform; hero.position = new Vector3(5, 0, 0);
            CharacterRegistry.RegisterHero(hero, new FakeHealth());
            //# Heroes 도 같은 Entry 타입이므로 IsEngaging 기본 false. 영웅 검색은 그래도 found=true.

            bool found = CharacterRegistry.TryFindNearestHero(
                Vector3.zero, out Transform t, out IHealth _);

            Assert.IsTrue(found, "영웅 검색은 requireEngaging=false — IsEngaging 무관");
            Assert.AreSame(hero, t);

            Object.DestroyImmediate(hero.gameObject);
        }

        //# 엣지 — SetMonsterEngaging 이 같은 Transform 의 첫 Entry 만 갱신하고 즉시 return.
        //# (안전망 — 정상 경로에선 같은 Transform 의 중복 Entry 가 만들어지지 않지만, 방어 로직 박제.)
        [Test]
        public void SetMonsterEngaging_첫_매칭_Entry만_갱신()
        {
            Transform t = new GameObject("m").transform;
            FakeHealth h1 = new FakeHealth();
            FakeHealth h2 = new FakeHealth();
            //# 동일 Transform 으로 2회 등록 (드물지만 가능한 안전망 케이스).
            CharacterRegistry.RegisterMonster(t, h1);
            CharacterRegistry.RegisterMonster(t, h2);
            Assert.AreEqual(2, CharacterRegistry.Monsters.Count, "동일 Transform 중복 등록 2건");

            CharacterRegistry.SetMonsterEngaging(t, true);

            //# 첫 매칭 Entry 만 true — 동작 박제 (foreach + return).
            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging, "첫 매칭 Entry true");
            Assert.IsFalse(CharacterRegistry.Monsters[1].IsEngaging,
                "두 번째 매칭 Entry 는 갱신 안 됨 (첫 매칭에서 return) — 동작 박제");

            Object.DestroyImmediate(t.gameObject);
        }
    }
}
