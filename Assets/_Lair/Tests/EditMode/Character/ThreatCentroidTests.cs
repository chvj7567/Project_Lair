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

        //# A-1 도주 안정화 — 도주용 centroid(engaging 무관)는 Marching 몬스터도 집계해야 한다.
        //# 정상: 같은 배치에서 타겟팅용(engaging=true)은 false, 도주용(engaging=false)은 count>0.
        [Test]
        public void 도주centroid는_Marching몬스터도_집계한다()
        {
            //# 반경 안 2마리 전부 Marching(IsEngaging=false).
            Transform a = SpawnMonster(new Vector3(1, 0, 0), false);
            Transform b = SpawnMonster(new Vector3(-1, 0, 0), false);

            //# 타겟팅용(engaging 필터) — Marching 제외 → false.
            bool targetFound = CharacterRegistry.TryGetThreatCentroidMonster(
                Vector3.zero, 4f, out Vector3 _, out int targetCount);
            //# 도주용(engaging 무관) — Marching 포함 → count=2.
            bool fleeFound = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 4f, out Vector3 fleeCentroid, out int fleeCount);

            Assert.IsFalse(targetFound, "타겟팅 centroid 는 Marching 제외");
            Assert.AreEqual(0, targetCount);
            Assert.IsTrue(fleeFound, "도주 centroid 는 Marching 포함");
            Assert.AreEqual(2, fleeCount);
            Assert.Less(fleeCentroid.magnitude, 0.001f, "대칭 2마리 centroid 는 원점 근처");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }

        //# A-1 엣지 — 도주용 centroid 도 반경 필터는 동일 적용(engaging 무관해도 거리 제한 유지).
        [Test]
        public void 도주centroid_반경밖_Marching은_제외()
        {
            Transform near = SpawnMonster(new Vector3(2, 0, 0), false);
            Transform far = SpawnMonster(new Vector3(10, 0, 0), false);

            bool found = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 4f, out Vector3 centroid, out int count);

            Assert.IsTrue(found);
            Assert.AreEqual(1, count, "반경 4 밖의 far 는 engaging 무관해도 제외");
            Assert.Less(Vector3.Distance(centroid, new Vector3(2, 0, 0)), 0.001f);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        //# ===== test-engineer 보강 (A-1) — 기존 2건과 중복 없는 상호보완 케이스 =====

        //# A-1 값 정확성 — 비대칭 배치에서 도주 centroid 가 실제 위치 평균(무게중심)인가.
        //# 기존 케이스는 대칭(원점) centroid 만 검증 — 여기선 한쪽 치우친 평균값을 박제.
        [Test]
        public void 도주centroid는_비대칭_배치의_실제_평균_위치다()
        {
            //# (2,0,0)·(4,0,0) Marching → 평균 (3,0,0). engaging 무관 집계.
            Transform a = SpawnMonster(new Vector3(2, 0, 0), false);
            Transform b = SpawnMonster(new Vector3(4, 0, 0), false);

            bool found = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 10f, out Vector3 centroid, out int count);

            Assert.IsTrue(found);
            Assert.AreEqual(2, count);
            Assert.Less(Vector3.Distance(centroid, new Vector3(3, 0, 0)), 0.001f,
                "비대칭 2마리 centroid 는 두 위치의 산술 평균 (3,0,0)");
            //# centroid 가 +X 쪽(원점 기준 양수) 이라 도주방향(pos-centroid)이 -X 가 됨을 함의 — AutoCombatAI 도주 분기 입력 정합.
            Assert.AreEqual(3f, centroid.x, 0.001f, "centroid.x 는 두 위치 X 의 평균 (3)");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }

        //# A-1 엣지 — Engaging 과 Marching 이 섞이면 도주 centroid 는 둘 다 집계(engaging 무관).
        //# 기존 케이스(전부 Marching)와 달리 혼합 상태에서의 count 를 박제.
        [Test]
        public void 도주centroid는_Engaging과_Marching을_모두_집계한다()
        {
            Transform engaging = SpawnMonster(new Vector3(1, 0, 0), true);
            Transform marching = SpawnMonster(new Vector3(-1, 0, 0), false);

            //# 타겟팅용(engaging 필터) — Engaging 1마리만.
            CharacterRegistry.TryGetThreatCentroidMonster(
                Vector3.zero, 4f, out Vector3 _, out int targetCount);
            //# 도주용 — 둘 다 포함 → 2.
            bool fleeFound = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 4f, out Vector3 fleeCentroid, out int fleeCount);

            Assert.AreEqual(1, targetCount, "타겟팅 centroid 는 Engaging 1마리만");
            Assert.IsTrue(fleeFound);
            Assert.AreEqual(2, fleeCount, "도주 centroid 는 Engaging+Marching 둘 다");
            Assert.Less(fleeCentroid.magnitude, 0.001f, "대칭 배치 → 원점");

            Object.DestroyImmediate(engaging.gameObject);
            Object.DestroyImmediate(marching.gameObject);
        }

        //# A-1 엣지 — 반경 내 0마리(빈 무리) → false, centroid 0벡터. AutoCombatAI 가 최근접 fallback 으로 분기하는 입력.
        [Test]
        public void 도주centroid_반경내_없으면_false_count0()
        {
            //# 전부 반경(4) 밖.
            Transform far1 = SpawnMonster(new Vector3(8, 0, 0), false);
            Transform far2 = SpawnMonster(new Vector3(0, 0, 9), true);

            bool found = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 4f, out Vector3 centroid, out int count);

            Assert.IsFalse(found, "반경 내 0마리 → false (AutoCombatAI 최근접 fallback 경로)");
            Assert.AreEqual(0, count);
            Assert.Less(centroid.magnitude, 0.001f, "실패 시 centroid 는 0벡터");

            Object.DestroyImmediate(far1.gameObject);
            Object.DestroyImmediate(far2.gameObject);
        }

        //# A-1 격리 — 도주 centroid 는 Monsters 리스트만 본다(Heroes 미집계).
        //# 영웅이 원점 근처에 등록돼 있어도 도주 무리 집계에 끼면 안 됨.
        [Test]
        public void 도주centroid는_Heroes를_집계하지_않는다()
        {
            //# Heroes 에 원점 근처 1명 등록 — 집계되면 count 가 늘어남.
            Transform hero = new GameObject("h").transform;
            hero.position = new Vector3(0.5f, 0, 0);
            CharacterRegistry.RegisterHero(hero, new FakeHealth());

            //# Monsters 에 반경 안 1마리.
            Transform monster = SpawnMonster(new Vector3(2, 0, 0), false);

            bool found = CharacterRegistry.TryGetFleeCentroidMonster(
                Vector3.zero, 4f, out Vector3 centroid, out int count);

            Assert.IsTrue(found);
            Assert.AreEqual(1, count, "Monsters 1마리만 — Heroes 는 도주 centroid 에 미집계");
            Assert.Less(Vector3.Distance(centroid, new Vector3(2, 0, 0)), 0.001f,
                "centroid 는 몬스터 위치 — 영웅(0.5,0,0) 이 섞이지 않음");

            Object.DestroyImmediate(hero.gameObject);
            Object.DestroyImmediate(monster.gameObject);
        }
    }
}
