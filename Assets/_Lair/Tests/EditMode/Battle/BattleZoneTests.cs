using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Battle
{
    //# BattleZone 본격 스위트 — IsInside / ClampInside / GetRandomSpawn / OnTriggerEnter / HeroEntryPoint.
    //# 시드 테스트(gameplay-programmer) 위에 엣지·회귀(test-engineer) 를 쌓는다.
    public class BattleZoneTests
    {
        //# 테스트가 만든 GameObject 들을 한곳에서 관리해 누수 차단 (NIT-2 컨테이너 패턴).
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void Setup()
        {
            //# 정적 레지스트리 — 다른 스위트의 잔존 방지.
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        //# 본체 BoxCollider 의 bounds 가 IsInside / ClampInside 의 진실. center=(0,0,0), size=(10,1,10) 기준.
        private BattleZone CreateZone(Vector3 center, Vector3 size)
        {
            GameObject zoneGo = new GameObject("BattleZoneUT");
            zoneGo.transform.position = center;
            BoxCollider col = zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            BattleZone zone = zoneGo.AddComponent<BattleZone>();
            //# Awake 가 _zoneTrigger 폴백 — EditMode 에서는 리플렉션 호출.
            InvokeAwake(zone);
            _spawned.Add(zoneGo);
            return zone;
        }

        private static void InvokeAwake(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null) mi.Invoke(c, null);
        }

        private static void InvokeOnTriggerEnter(BattleZone zone, Collider other)
        {
            MethodInfo mi = typeof(BattleZone).GetMethod("OnTriggerEnter",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "BattleZone.OnTriggerEnter 메서드 존재");
            mi.Invoke(zone, new object[] { other });
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"BattleZone.{field} 필드가 존재해야 함");
            fi.SetValue(target, value);
        }

        //# ===== IsInside 기본 =====

        [Test]
        public void IsInside_중심_true()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Assert.IsTrue(zone.IsInside(Vector3.zero));
        }

        [Test]
        public void IsInside_경계밖_false()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# zone 은 X 방향 ±5. (10, 0, 0) 은 명확히 밖.
            Assert.IsFalse(zone.IsInside(new Vector3(10, 0, 0)));
        }

        //# 엣지 — IsInside 가 bounds.Contains 를 그대로 호출하므로 Y 축도 함께 검사된다.
        //# size.y=1 (±0.5) 기준으로 Y=10 은 XZ 가 zone 안이라도 false 가 정확한 동작.
        //# Y 가 큰 입력이 zone 안으로 잘못 판정되지 않도록 회귀 박제.
        [Test]
        public void IsInside_Y가_bounds_밖이면_false()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# XZ 는 zone 중심, Y 는 bounds.max.y(0.5) 초과 → bounds.Contains false.
            Vector3 highY = new Vector3(0f, 10f, 0f);
            Assert.IsFalse(zone.IsInside(highY),
                "IsInside 는 bounds.Contains — Y 도 함께 검사. Y=10 은 size.y=1 의 ±0.5 밖.");
        }

        //# 회귀 — Y 가 bounds 안(0 = bounds.center.y)이면 XZ 중심도 true. Y 평면 가정.
        [Test]
        public void IsInside_Y가_bounds_안이면_XZ만_본_결과와_동일()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# Y=0 (bounds 안) 의 zone 중심.
            Assert.IsTrue(zone.IsInside(new Vector3(0f, 0f, 0f)));
            //# Y=0.4 (size.y=1 의 절반(0.5) 안).
            Assert.IsTrue(zone.IsInside(new Vector3(0f, 0.4f, 0f)));
        }

        //# 가드 — _zoneTrigger null 이면 false.
        [Test]
        public void IsInside_zoneTrigger_null이면_false()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            SetPrivate(zone, "_zoneTrigger", null);
            Assert.IsFalse(zone.IsInside(Vector3.zero));
        }

        //# ===== Center / HeroEntryPoint =====

        [Test]
        public void Center_BoxCollider_bounds_center()
        {
            BattleZone zone = CreateZone(new Vector3(3, 0, 3), new Vector3(10, 1, 10));
            Assert.AreEqual(new Vector3(3, 0, 3), zone.Center);
        }

        //# 가드 — _zoneTrigger null 이면 transform.position 폴백.
        [Test]
        public void Center_zoneTrigger_null이면_transform_position_폴백()
        {
            BattleZone zone = CreateZone(new Vector3(7, 2, 7), new Vector3(10, 1, 10));
            SetPrivate(zone, "_zoneTrigger", null);
            Assert.AreEqual(new Vector3(7, 2, 7), zone.Center);
        }

        [Test]
        public void HeroEntryPoint_할당된_Transform_반환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject hep = new GameObject("HeroEntry"); hep.transform.position = new Vector3(-8, 0, 0);
            _spawned.Add(hep);
            SetPrivate(zone, "_heroEntryPoint", hep.transform);
            Assert.AreSame(hep.transform, zone.HeroEntryPoint);
        }

        //# ===== GetRandomSpawn =====

        [Test]
        public void GetRandomSpawn_spawnPoints_없으면_null()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# _spawnPoints 미할당 — null array.
            Assert.IsNull(zone.GetRandomSpawn());
        }

        //# 엣지 — 길이 0 배열도 null 반환 (단순 null 체크가 아닌 Length 체크 회귀).
        [Test]
        public void GetRandomSpawn_빈_배열도_null()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            SetPrivate(zone, "_spawnPoints", new Transform[0]);
            Assert.IsNull(zone.GetRandomSpawn(),
                "Length 0 배열 — null array 와 동일하게 null 반환 (Length 가드 회귀)");
        }

        //# 엣지 — 단일 요소 배열은 deterministic 하게 그 요소만 반환.
        [Test]
        public void GetRandomSpawn_단일요소_배열이면_그_요소만_반환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject sp1 = new GameObject("sp1"); sp1.transform.position = new Vector3(7, 0, 0);
            _spawned.Add(sp1);
            SetPrivate(zone, "_spawnPoints", new Transform[] { sp1.transform });

            //# 다회 호출에도 같은 요소 (Range(0,1) = 항상 0).
            for (int i = 0; i < 20; ++i)
                Assert.AreSame(sp1.transform, zone.GetRandomSpawn());
        }

        [Test]
        public void GetRandomSpawn_할당시_배열중_하나_반환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject sp1 = new GameObject("sp1"); sp1.transform.position = new Vector3(7, 0, 0);
            GameObject sp2 = new GameObject("sp2"); sp2.transform.position = new Vector3(0, 0, 7);
            _spawned.Add(sp1); _spawned.Add(sp2);
            SetPrivate(zone, "_spawnPoints", new Transform[] { sp1.transform, sp2.transform });

            Transform picked = zone.GetRandomSpawn();
            Assert.IsNotNull(picked);
            Assert.IsTrue(picked == sp1.transform || picked == sp2.transform);
        }

        //# 분산 검증 — 충분한 다회 호출 시 모든 요소가 적어도 1번 등장.
        //# 4개 요소 / 200회 호출 → 각 요소가 등장할 기댓값 50회. 분산 회귀.
        [Test]
        public void GetRandomSpawn_다회호출시_모든_요소_등장()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject[] sps = new GameObject[4];
            Transform[] tfs = new Transform[4];
            for (int i = 0; i < 4; ++i)
            {
                sps[i] = new GameObject($"sp{i}");
                sps[i].transform.position = new Vector3(i, 0, 0);
                tfs[i] = sps[i].transform;
                _spawned.Add(sps[i]);
            }
            SetPrivate(zone, "_spawnPoints", tfs);

            //# 고정 시드 — 결정적 검증.
            Random.State prevState = Random.state;
            Random.InitState(12345);
            try
            {
                HashSet<Transform> seen = new HashSet<Transform>();
                for (int i = 0; i < 200; ++i)
                    seen.Add(zone.GetRandomSpawn());

                Assert.AreEqual(4, seen.Count,
                    "200회 호출 시 4개 spawn point 모두 등장 (분산 회귀)");
            }
            finally
            {
                Random.state = prevState;
            }
        }

        //# ===== ClampInside =====

        [Test]
        public void ClampInside_zone_안의_좌표는_그대로()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 inside = new Vector3(2, 0, 2);
            Vector3 clamped = zone.ClampInside(inside);
            Assert.AreEqual(inside, clamped);
        }

        [Test]
        public void ClampInside_zone_밖의_좌표는_경계로()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# X 방향 ±5 가 zone 경계. X=10 입력 → X=5 로 클램프.
            Vector3 outside = new Vector3(10, 0, 0);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(5f, clamped.x, 0.001f, "X 가 5 (bounds.max.x) 로 클램프");
            Assert.AreEqual(0f, clamped.z, 0.001f);
        }

        [Test]
        public void ClampInside_zoneTrigger_null이면_입력_그대로()
        {
            //# _zoneTrigger 강제 null — 안전 가드 검증.
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            SetPrivate(zone, "_zoneTrigger", null);
            Vector3 input = new Vector3(99, 0, 99);
            Assert.AreEqual(input, zone.ClampInside(input));
        }

        //# 엣지 — Y 축은 ClampInside 가 건드리지 않는다 (입력 Y 그대로 반환).
        //# SimpleMover 가 Y=0 으로 고정한다는 전제하에 Y 보존을 박제 — 회귀.
        [Test]
        public void ClampInside_Y는_입력값_그대로_보존()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 input = new Vector3(2f, 7.5f, 2f);
            Vector3 clamped = zone.ClampInside(input);
            Assert.AreEqual(7.5f, clamped.y, 0.0001f,
                "ClampInside 는 X/Z 만 클램프 — Y 는 입력값 그대로");
        }

        //# 엣지 — Y 가 큰 음수일 때도 보존.
        [Test]
        public void ClampInside_Y_음수도_보존()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 input = new Vector3(0f, -99f, 0f);
            Vector3 clamped = zone.ClampInside(input);
            Assert.AreEqual(-99f, clamped.y, 0.0001f);
        }

        //# 엣지 분기 1 — X 만 밖, Z 는 안 (변 케이스).
        [Test]
        public void ClampInside_X만_밖이면_X만_클램프되고_Z는_그대로()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 outside = new Vector3(20f, 0f, 2f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(5f, clamped.x, 0.001f, "X 는 bounds.max.x(5) 로 클램프");
            Assert.AreEqual(2f, clamped.z, 0.001f, "Z 는 zone 안 — 입력 그대로");
        }

        //# 엣지 분기 2 — Z 만 밖, X 는 안 (변 케이스).
        [Test]
        public void ClampInside_Z만_밖이면_Z만_클램프되고_X는_그대로()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 outside = new Vector3(2f, 0f, -20f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(2f, clamped.x, 0.001f);
            Assert.AreEqual(-5f, clamped.z, 0.001f, "Z 는 bounds.min.z(-5) 로 클램프");
        }

        //# 엣지 분기 3 — X/Z 모두 밖 (모서리 케이스).
        [Test]
        public void ClampInside_XZ_둘다_밖이면_각각_클램프()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 outside = new Vector3(15f, 0f, 15f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(5f, clamped.x, 0.001f, "X 는 max(5) 로 클램프");
            Assert.AreEqual(5f, clamped.z, 0.001f, "Z 는 max(5) 로 클램프");
        }

        //# 엣지 — 정확히 경계 좌표는 무변동 (Mathf.Clamp 는 양 끝 포함).
        [Test]
        public void ClampInside_정확히_경계_좌표는_그대로()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Vector3 corner = new Vector3(5f, 0f, 5f);
            Vector3 clamped = zone.ClampInside(corner);
            Assert.AreEqual(5f, clamped.x, 0.0001f);
            Assert.AreEqual(5f, clamped.z, 0.0001f);
        }

        //# ===== OnTriggerEnter =====

        [Test]
        public void OnTriggerEnter_MonsterTag_있으면_IsEngaging_true()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject m = new GameObject("Monster");
            m.AddComponent<MonsterTag>();
            BoxCollider mc = m.AddComponent<BoxCollider>();
            _spawned.Add(m);

            CharacterRegistry.RegisterMonster(m.transform, new FakeHealth());
            Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging, "초기 Marching");

            InvokeOnTriggerEnter(zone, mc);

            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging,
                "MonsterTag 진입 후 IsEngaging=true");
        }

        [Test]
        public void OnTriggerEnter_MonsterTag_없으면_무동작()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# MonsterTag 없는 GameObject — 영웅이나 다른 컬라이더 시뮬레이션.
            GameObject other = new GameObject("Other");
            BoxCollider oc = other.AddComponent<BoxCollider>();
            _spawned.Add(other);

            //# MonsterTag 없으니 SetMonsterEngaging 호출도 안 일어남. 예외 없이 무동작.
            Assert.DoesNotThrow(() => InvokeOnTriggerEnter(zone, oc));
        }

        //# 엣지 — null Collider 가 들어와도 가드 절로 무동작.
        [Test]
        public void OnTriggerEnter_null_Collider_무동작()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            Assert.DoesNotThrow(() => InvokeOnTriggerEnter(zone, null));
        }

        //# 엣지 — 같은 Transform 다중 호출 시 idempotent (IsEngaging 유지, Entry 개수 무증가).
        //# OnTriggerEnter 는 SetMonsterEngaging 만 호출하므로 사이드이펙트 없음. 회귀 박제.
        [Test]
        public void OnTriggerEnter_같은_Transform_다중호출_idempotent()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject m = new GameObject("Monster");
            m.AddComponent<MonsterTag>();
            BoxCollider mc = m.AddComponent<BoxCollider>();
            _spawned.Add(m);

            CharacterRegistry.RegisterMonster(m.transform, new FakeHealth());

            //# 5회 호출.
            for (int i = 0; i < 5; ++i)
                InvokeOnTriggerEnter(zone, mc);

            Assert.AreEqual(1, CharacterRegistry.Monsters.Count,
                "Entry 개수는 등록 1회 그대로 — OnTriggerEnter 가 Add 부작용 없음");
            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging,
                "다중 호출에도 IsEngaging=true 유지 (idempotent)");
        }

        //# 엣지 — 다양한 비-MonsterTag 컴포넌트 (HeroEntryDriver, Spawner 본체, Health) 부착 객체.
        //# 어느 쪽도 MonsterTag 가 없으므로 모두 무동작이어야.
        [Test]
        public void OnTriggerEnter_다양한_비_MonsterTag_컴포넌트_무동작()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            //# (a) HeroEntryDriver — 영웅 시뮬레이션.
            GameObject hero = new GameObject("Hero");
            hero.AddComponent<BoxCollider>();
            hero.AddComponent<HeroEntryDriver>();
            _spawned.Add(hero);

            //# (b) Spawner 본체 (Trigger 안에 우연히 배치된 경우).
            GameObject spawnerGo = new GameObject("SpawnerBody");
            BoxCollider spc = spawnerGo.AddComponent<BoxCollider>();
            spawnerGo.AddComponent<Spawner>();
            _spawned.Add(spawnerGo);

            //# (c) Health 만 부착된 객체 (다른 풀 객체).
            GameObject other = new GameObject("OtherHealth");
            BoxCollider oc = other.AddComponent<BoxCollider>();
            other.AddComponent<Health>();
            _spawned.Add(other);

            Assert.DoesNotThrow(() =>
            {
                InvokeOnTriggerEnter(zone, hero.GetComponent<Collider>());
                InvokeOnTriggerEnter(zone, spc);
                InvokeOnTriggerEnter(zone, oc);
            });

            //# Heroes/Monsters 어느 쪽도 추가 안 됨 — OnTriggerEnter 에 register 부작용 없음.
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count,
                "비-MonsterTag 다양한 컴포넌트 — Monsters 등록 안 됨");
        }

        //# 엣지 — MonsterTag 가 있어도 CharacterRegistry 미등록 Transform 이면 SetMonsterEngaging no-op.
        //# 예외 없이 통과해야 (안전망).
        [Test]
        public void OnTriggerEnter_MonsterTag_있고_미등록이면_예외없이_무동작()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject m = new GameObject("UnregisteredMonster");
            m.AddComponent<MonsterTag>();
            BoxCollider mc = m.AddComponent<BoxCollider>();
            _spawned.Add(m);
            //# RegisterMonster 호출 안 함 — 미등록.

            Assert.DoesNotThrow(() => InvokeOnTriggerEnter(zone, mc));
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count);
        }
    }
}
