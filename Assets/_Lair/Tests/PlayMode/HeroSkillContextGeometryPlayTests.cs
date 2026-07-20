using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.PlayMode
{
    //# [보강] HeroSkillContext 기하 통합 (test-engineer) — 실제 CharacterRegistry 몬스터 대상.
    //# 기존 HeroSkillContextPlayTests(1) 는 Ring 반경내/밖 1케이스만 다룬다.
    //# 본 스위트는 부채꼴(cone) 판정(전방/각도초과/반경초과/뒤쪽)·구(sphere) union dedup, centroid 의 살아있음+Engaging 필터,
    //# 넉백 방향/변위, 분모0 엣지를 실제 레지스트리로 검증한다 (Gap 1·4).
    public class HeroSkillContextGeometryPlayTests
    {
        private GameObject _hero;

        [SetUp]
        public void SetUp()
        {
            //# 정적 레지스트리 누수 방지 — ThreatCentroidTests 패턴(기존 HeroSkillContextPlayTests 미준수분 보강).
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            _hero = new GameObject("Hero");
            _hero.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            if (_hero != null)
                Object.DestroyImmediate(_hero);
        }

        //# 등록 + Engaging 켠 살아있는 몬스터. 반환 GameObject 는 호출자가 정리.
        private GameObject MakeMonster(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        //# 사망 시 레지스트리에서 즉시 빠지는 몬스터 — 프로덕션의
        //# OnDied → DespawnOnDeath → CHMPool.Push → SetActive(false) → OnDisable → UnregisterMonster
        //# 동기 경로(열거 중 컬렉션 수정)를 풀 없이 재현. HP 를 낮춰 1회 데미지로 사망시킨다.
        private GameObject MakeDespawningMonster(string name, Vector3 pos, int maxHp)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(maxHp, true);
            DummyDespawn dummy = go.AddComponent<DummyDespawn>();
            dummy.Bind(h, go.transform);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        //# StampDamageColor 호출만 기록하는 경량 스파이 — DamageFeedback(RequireComponent 등) 없이
        //# 스킬 데미지가 데미지색을 스탬프하는지 검증. Apply 가 e.Transform 의 Character 로케이터 경유로
        //# IDamageColorSink 를 찾으므로, 몬스터 GameObject 에 스파이 + Character 를 함께 붙인다.
        private class DamageColorSinkSpy : MonoBehaviour, IDamageColorSink
        {
            public int CallCount { get; private set; }
            public Color LastColor { get; private set; }

            public void StampDamageColor(Color color)
            {
                ++CallCount;
                LastColor = color;
            }
        }

        //# 스파이 동반 몬스터 — RegisterMonster 전에 스파이를 붙여야 GetComponent 로 잡힌다.
        private GameObject MakeMonsterWithSpy(string name, Vector3 pos, out DamageColorSinkSpy spy)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000, true);
            spy = go.AddComponent<DamageColorSinkSpy>();
            //# 스킬 데미지색 스탬프는 대상 Character 로케이터 경유로 IDamageColorSink 를 해석 → 부착 필수.
            go.AddComponent<LairCharacter>();
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        //# Health.OnDied 구독 → CharacterRegistry.UnregisterMonster 동기 호출.
        //# DespawnOnDeath+CHMPool 전체 구성 없이 "사망 시 레지스트리 수정" 만 재현하는 테스트 더블.
        private class DummyDespawn : MonoBehaviour
        {
            private Health _health;
            private Transform _self;

            public void Bind(Health health, Transform self)
            {
                _health = health;
                _self = self;
                if (_health != null)
                    _health.OnDied += HandleDied;
            }

            private void HandleDied() => CharacterRegistry.UnregisterMonster(_self);

            private void OnDestroy()
            {
                if (_health != null)
                    _health.OnDied -= HandleDied;
            }
        }

        //# ── Gap 1: DamageMonstersInCone 부채꼴(radial) 판정 ───────────────

        [Test]
        public void DamageMonstersInCone_전방각도내만_피격_각도초과_제외()
        {
            //# +Z 로 반경 6, 반각 35°. inFront(0,_,3) 각도 0° 안 / side(3,_,3) 각도 45° 초과.
            GameObject inFront = MakeMonster("InFront", new Vector3(0f, 0f, 3f));
            GameObject side = MakeMonster("Side", new Vector3(3f, 0f, 3f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            int hit = ctx.DamageMonstersInCone(Vector3.forward, 6f, 35f, 100, 0f);

            Assert.AreEqual(1, hit, "부채꼴 각도 내 1마리만 피격");
            Assert.AreEqual(900, inFront.GetComponent<Health>().Current, "정면 몬스터 피격");
            Assert.AreEqual(1000, side.GetComponent<Health>().Current, "각도 초과 몬스터 제외");

            Object.DestroyImmediate(inFront);
            Object.DestroyImmediate(side);
        }

        [Test]
        public void DamageMonstersInCone_길이초과와_뒤쪽_제외()
        {
            //# +Z 로 반경 5. tooFar(0,_,7) 반경 초과 / behind(0,_,-2) 뒤쪽(각도 180°).
            GameObject tooFar = MakeMonster("TooFar", new Vector3(0f, 0f, 7f));
            GameObject behind = MakeMonster("Behind", new Vector3(0f, 0f, -2f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            int hit = ctx.DamageMonstersInCone(Vector3.forward, 5f, 35f, 100, 0f);

            Assert.AreEqual(0, hit, "반경 초과·뒤쪽 모두 제외");
            Assert.AreEqual(1000, tooFar.GetComponent<Health>().Current);
            Assert.AreEqual(1000, behind.GetComponent<Health>().Current);

            Object.DestroyImmediate(tooFar);
            Object.DestroyImmediate(behind);
        }

        //# ── per-sphere union dedup (형태 변경 핵심) ────────────────────────

        [Test]
        public void DamageMonstersInSpheres_여러구_겹침_몬스터1회만_데미지()
        {
            //# 몬스터가 두 구에 동시에 들어가도(union) 데미지 1회만 → union dedup 검증.
            GameObject mon = MakeMonster("Overlap", new Vector3(0f, 0f, 0f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            //# 두 구 중심 모두 몬스터(원점) 반경 0.9 안 → 둘 다 포함하지만 1회.
            Vector3[] centers = { new Vector3(0.3f, 0f, 0f), new Vector3(-0.3f, 0f, 0f) };
            int hit = ctx.DamageMonstersInSpheres(centers, 0.9f, 100, 0f);

            Assert.AreEqual(1, hit, "두 구 동시 진입 → 고유 1마리");
            Assert.AreEqual(900, mon.GetComponent<Health>().Current, "데미지 1회만(중복 없음)");

            Object.DestroyImmediate(mon);
        }

        //# ── Gap 1: MonsterCentroid 필터 (살아있음 + Engaging) ────────────

        [Test]
        public void MonsterCentroid_죽은몬스터_제외()
        {
            //# alive(0,_,2) / dead(0,_,4) — dead 는 HP 0 으로 죽임. centroid 는 alive 위치.
            GameObject alive = MakeMonster("Alive", new Vector3(0f, 0f, 2f));
            GameObject dead = MakeMonster("Dead", new Vector3(0f, 0f, 4f));
            dead.GetComponent<Health>().TakeDamage(1000);   //# SetActive 토글 금지 — Health.OnEnable 부활 방지.
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            Vector3 centroid = ctx.MonsterCentroid(10f);

            Assert.Less(Vector3.Distance(centroid, new Vector3(0f, 0f, 2f)), 0.001f,
                "죽은 몬스터 제외 → centroid 는 살아있는 alive 위치");

            Object.DestroyImmediate(alive);
            Object.DestroyImmediate(dead);
        }

        [Test]
        public void MonsterCentroid_비Engaging_제외()
        {
            //# engaging(0,_,2) / marching(0,_,6) Marching. centroid 는 engaging 위치.
            GameObject engaging = MakeMonster("Engaging", new Vector3(0f, 0f, 2f));
            GameObject marching = MakeMonster("Marching", new Vector3(0f, 0f, 6f));
            CharacterRegistry.SetMonsterEngaging(marching.transform, false);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            Vector3 centroid = ctx.MonsterCentroid(10f);

            Assert.Less(Vector3.Distance(centroid, new Vector3(0f, 0f, 2f)), 0.001f,
                "Marching 제외 → centroid 는 Engaging 몬스터 위치");

            Object.DestroyImmediate(engaging);
            Object.DestroyImmediate(marching);
        }

        [Test]
        public void MonsterCentroid_후보없으면_영웅위치_반환()
        {
            //# 반경 내 Engaging 몬스터 0 → MonsterCentroid 는 HeroPosition 반환(돌진 발동 보류 트리거).
            _hero.transform.position = new Vector3(5f, 0f, 5f);
            GameObject far = MakeMonster("Far", new Vector3(50f, 0f, 50f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            Vector3 centroid = ctx.MonsterCentroid(3f);

            Assert.Less(Vector3.Distance(centroid, _hero.transform.position), 0.001f,
                "반경 내 후보 없음 → HeroPosition");

            Object.DestroyImmediate(far);
        }

        //# ── Gap 4: 넉백 방향/변위 ────────────────────────────────────────

        [Test]
        public void 넉백_영웅반대방향으로_변위()
        {
            //# 영웅 원점, 몬스터 (2,0,0). 넉백 3 → +X 로 (5,0,0) 부근으로 밀림.
            GameObject mon = MakeMonster("Mon", new Vector3(2f, 0f, 0f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            ctx.DamageMonstersInRing(0f, 3.5f, 100, 3f);

            Vector3 after = mon.transform.position;
            Assert.AreEqual(5f, after.x, 0.001f, "영웅 반대(+X)로 3유닛 변위 → 2+3=5");
            Assert.AreEqual(0f, after.z, 0.001f);

            Object.DestroyImmediate(mon);
        }

        [Test]
        public void 넉백_영웅과_동일위치_분모0_예외없이_noop()
        {
            //# 엣지 — 몬스터가 영웅과 같은 위치(분모 0). 넉백 변위 없이 데미지만, 예외 없음.
            GameObject mon = MakeMonster("Overlap", Vector3.zero);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            int hit = 0;
            Assert.DoesNotThrow(() => hit = ctx.DamageMonstersInRing(0f, 3.5f, 100, 3f));

            Assert.AreEqual(1, hit, "동일 위치도 디스크 안 → 피격");
            Assert.AreEqual(900, mon.GetComponent<Health>().Current, "데미지는 적용");
            Assert.Less(mon.transform.position.magnitude, 0.001f, "분모 0 → 변위 없음(no-op)");

            Object.DestroyImmediate(mon);
        }

        [Test]
        public void 넉백0이면_변위없음()
        {
            //# 회귀 — knockbackStrength 0(궤도 블레이드 경로)이면 데미지만, 위치 불변.
            GameObject mon = MakeMonster("Mon", new Vector3(2f, 0f, 0f));
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            ctx.DamageMonstersInRing(0f, 3.5f, 100, 0f);

            Assert.AreEqual(2f, mon.transform.position.x, 0.001f, "넉백 0 → 위치 불변");
            Assert.AreEqual(900, mon.GetComponent<Health>().Current);

            Object.DestroyImmediate(mon);
        }

        //# ── 회귀(qa): 데미지로 사망 → 열거 중 레지스트리 수정 → 예외 재발 방지 ──

        [Test]
        public void DamageMonstersInRing_데미지로_여러마리_동시사망_예외없이_언레지스터()
        {
            //# 반경 내 2마리 모두 HP10 → 100 데미지 1회로 동시 사망.
            //# 사망 시 DummyDespawn 이 Monsters 리스트를 동기 수정 → unfixed 면 enumeration 예외.
            GameObject a = MakeDespawningMonster("DyingA", new Vector3(1f, 0f, 0f), 10);
            GameObject b = MakeDespawningMonster("DyingB", new Vector3(2f, 0f, 0f), 10);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            int hit = 0;
            Assert.DoesNotThrow(() => hit = ctx.DamageMonstersInRing(0f, 5f, 100, 0f),
                "열거 중 사망으로 레지스트리가 수정돼도 예외 없이 완료");
            Assert.AreEqual(2, hit, "반경 내 2마리 모두 데미지 적용");
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count, "사망 2마리 모두 언레지스터");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void DamageMonstersInCone_데미지로_여러마리_동시사망_예외없이_언레지스터()
        {
            //# +Z 부채꼴 안 2마리 모두 HP10 → 동시 사망. 부채꼴 경로도 동일 스냅샷 분리 검증.
            GameObject a = MakeDespawningMonster("ConeDyingA", new Vector3(0f, 0f, 2f), 10);
            GameObject b = MakeDespawningMonster("ConeDyingB", new Vector3(0.5f, 0f, 4f), 10);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            int hit = 0;
            Assert.DoesNotThrow(() => hit = ctx.DamageMonstersInCone(Vector3.forward, 6f, 35f, 100, 0f),
                "부채꼴 열거 중 사망으로 레지스트리가 수정돼도 예외 없이 완료");
            Assert.AreEqual(2, hit, "부채꼴 안 2마리 모두 데미지 적용");
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count, "사망 2마리 모두 언레지스터");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void DamageMonstersInSpheres_데미지로_여러마리_동시사망_예외없이_언레지스터()
        {
            //# 두 구 각각 안 2마리 모두 HP10 → 동시 사망. 구 경로도 동일 스냅샷 분리 검증.
            GameObject a = MakeDespawningMonster("SphereDyingA", new Vector3(1f, 0f, 0f), 10);
            GameObject b = MakeDespawningMonster("SphereDyingB", new Vector3(-1f, 0f, 0f), 10);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            Vector3[] centers = { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f) };
            int hit = 0;
            Assert.DoesNotThrow(() => hit = ctx.DamageMonstersInSpheres(centers, 0.9f, 100, 0f),
                "구 열거 중 사망으로 레지스트리가 수정돼도 예외 없이 완료");
            Assert.AreEqual(2, hit, "두 구 안 2마리 모두 데미지 적용");
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count, "사망 2마리 모두 언레지스터");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        //# ── 회귀(버그): 스킬 데미지가 데미지색(흰색)을 스탬프 ─────────────────
        //# 미수정 시 Apply 가 StampDamageColor 를 안 불러 CallCount==0 → 데미지 숫자/임팩트 누락.

        [Test]
        public void DamageMonstersInRing_피격대상에_흰색_데미지색_스탬프()
        {
            GameObject mon = MakeMonsterWithSpy("RingHit", new Vector3(2f, 0f, 0f), out DamageColorSinkSpy spy);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            ctx.DamageMonstersInRing(0f, 3.5f, 100, 0f);

            Assert.AreEqual(1, spy.CallCount, "스킬 데미지가 데미지색을 1회 스탬프");
            Assert.AreEqual(Color.white, spy.LastColor, "영웅 공격 → 흰색(근접과 일관)");

            Object.DestroyImmediate(mon);
        }

        [Test]
        public void DamageMonstersInCone_피격대상에_흰색_데미지색_스탬프()
        {
            GameObject mon = MakeMonsterWithSpy("ConeHit", new Vector3(0f, 0f, 3f), out DamageColorSinkSpy spy);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            ctx.DamageMonstersInCone(Vector3.forward, 6f, 35f, 100, 0f);

            Assert.AreEqual(1, spy.CallCount, "부채꼴 경로도 데미지색을 1회 스탬프");
            Assert.AreEqual(Color.white, spy.LastColor, "영웅 공격 → 흰색");

            Object.DestroyImmediate(mon);
        }

        [Test]
        public void DamageMonstersInSpheres_피격대상에_흰색_데미지색_스탬프()
        {
            GameObject mon = MakeMonsterWithSpy("SphereHit", new Vector3(1f, 0f, 0f), out DamageColorSinkSpy spy);
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);

            Vector3[] centers = { new Vector3(1f, 0f, 0f) };
            ctx.DamageMonstersInSpheres(centers, 0.9f, 100, 0f);

            Assert.AreEqual(1, spy.CallCount, "구 경로도 데미지색을 1회 스탬프");
            Assert.AreEqual(Color.white, spy.LastColor, "영웅 공격 → 흰색");

            Object.DestroyImmediate(mon);
        }
    }
}
