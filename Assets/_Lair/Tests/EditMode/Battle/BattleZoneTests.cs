using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Battle
{
    //# BattleZone 본격 스위트 — IsInside / ClampInside / OnTriggerEnter / HeroEntryPoint.
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

        //# 본체 BoxCollider 의 bounds 는 IsInside / 교전 trigger 의 진실. ClampInside 는 _clampHalfExtentX/Z(분리됨)를 따른다.
        //# center=(0,0,0), size=(10,1,10) 기준.
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

        //# 클램프 테스트는 X/Z extent 를 명시 주입 — 기대값을 테스트에 드러내고 production 기본값 변경에 안 깨지게.
        //# 단일 float 오버로드는 X=Z(정사각)로 주입 — 기존 정사각 가정 케이스 의도 보존.
        private BattleZone CreateZoneWithClamp(Vector3 center, Vector3 size, float clampHalfExtent)
        {
            return CreateZoneWithClamp(center, size, clampHalfExtent, clampHalfExtent);
        }

        //# X/Z 분리 주입 — 직사각 클램프(비대칭) 검증용.
        private BattleZone CreateZoneWithClamp(Vector3 center, Vector3 size, float clampHalfExtentX, float clampHalfExtentZ)
        {
            BattleZone zone = CreateZone(center, size);
            SetPrivate(zone, "_clampHalfExtentX", clampHalfExtentX);
            SetPrivate(zone, "_clampHalfExtentZ", clampHalfExtentZ);
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

        //# ===== ClampInside (Center ± _clampHalfExtent — 교전 trigger 와 분리) =====

        //# 핵심 decoupling 시드 — collider 는 실제 교전 trigger 크기(22×15, half X 11 / Z 7.5), 클램프는 7.
        //# X=10 입력이 trigger half(11) 안인데도 클램프(7)로 잘려야 — "클램프만 축소, trigger 불변" 박제.
        [Test]
        public void ClampInside_교전trigger보다_작은_clampHalfExtent로_클램프()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(22, 1, 15), 7f);
            //# X=10 은 trigger half(11) 안이지만 clampHalfExtent(7) 밖 → 7 로 클램프(옛 bounds 계약이면 10 유지).
            Vector3 clamped = zone.ClampInside(new Vector3(10f, 0f, 0f));
            Assert.AreEqual(7f, clamped.x, 0.001f, "X 는 _clampHalfExtent(7) 로 클램프 — trigger half(11) 가 아님");
            Assert.AreEqual(0f, clamped.z, 0.001f);
        }

        //# 핵심 decouple 회귀 — 같은 zone 에서 IsInside(교전 trigger bounds) 와 ClampInside(_clampHalfExtent) 가 독립임을 한 단언에 입증.
        //# 트리거 size 22×15(half X 11 / Z 7.5) + 클램프 extent 7. 점 (9,0,0): 트리거 안(9<11) 이지만 클램프는 7 로 당김.
        //# = 교전 영역은 크게 유지하되 영웅 발은 7 에 갇힘 (기획서 §4.3 비등방 간극).
        [Test]
        public void IsInside는_trigger_기준이고_ClampInside는_clampHalfExtent_기준_독립()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(22, 1, 15), 7f);
            Vector3 gapPoint = new Vector3(9f, 0f, 0f);

            //# IsInside — 교전 trigger half(X 11) 기준 → 9 는 안(true). 클램프 축소가 교전 인지 범위를 줄이지 않음.
            Assert.IsTrue(zone.IsInside(gapPoint),
                "IsInside 는 trigger bounds(half X 11) 기준 — X=9 는 교전 영역 안");
            //# ClampInside — _clampHalfExtent(7) 기준 → 9 는 7 로 당김. 영웅 발이 닿는 범위만 축소.
            Assert.AreEqual(7f, zone.ClampInside(gapPoint).x, 0.001f,
                "ClampInside 는 _clampHalfExtent(7) 기준 — X=9 를 7 로 클램프 (trigger half 11 가 아님)");
        }

        //# 보강 — trigger X 경계(11) 바깥은 IsInside=false. 교전 trigger half 11 이 불변임을 핀.
        [Test]
        public void IsInside_trigger_X경계_밖이면_false()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(22, 1, 15), 7f);
            //# X=11.1 은 trigger half(11) 밖 → 교전 영역 아님.
            Assert.IsFalse(zone.IsInside(new Vector3(11.1f, 0f, 0f)),
                "trigger half(X 11) 밖 — IsInside false (클램프 7 과 무관하게 trigger 가 진실)");
        }

        [Test]
        public void ClampInside_clamp_안의_좌표는_그대로()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 inside = new Vector3(2, 0, 2);
            Vector3 clamped = zone.ClampInside(inside);
            Assert.AreEqual(inside, clamped);
        }

        [Test]
        public void ClampInside_clamp_밖의_좌표는_경계로()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            //# X 방향 ±7 이 클램프 경계. X=10 입력 → X=7 로 클램프.
            Vector3 outside = new Vector3(10, 0, 0);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(7f, clamped.x, 0.001f, "X 가 7 (Center.x + _clampHalfExtent) 로 클램프");
            Assert.AreEqual(0f, clamped.z, 0.001f);
        }

        //# 가드 — _zoneTrigger null 이면 Center 가 transform.position 폴백. clamp 는 그 중심 ± extent 로 계속 동작.
        [Test]
        public void ClampInside_zoneTrigger_null이면_transform_position_중심으로_클램프()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            SetPrivate(zone, "_zoneTrigger", null);
            //# Center 폴백 = transform.position(원점). X=99 → 7 로 클램프.
            Vector3 clamped = zone.ClampInside(new Vector3(99, 0, 99));
            Assert.AreEqual(7f, clamped.x, 0.001f);
            Assert.AreEqual(7f, clamped.z, 0.001f);
        }

        //# 엣지 — Y 축은 ClampInside 가 건드리지 않는다 (입력 Y 그대로 반환).
        //# SimpleMover 가 Y=0 으로 고정한다는 전제하에 Y 보존을 박제 — 회귀.
        [Test]
        public void ClampInside_Y는_입력값_그대로_보존()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 input = new Vector3(2f, 7.5f, 2f);
            Vector3 clamped = zone.ClampInside(input);
            Assert.AreEqual(7.5f, clamped.y, 0.0001f,
                "ClampInside 는 X/Z 만 클램프 — Y 는 입력값 그대로");
        }

        //# 엣지 — Y 가 큰 음수일 때도 보존.
        [Test]
        public void ClampInside_Y_음수도_보존()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 input = new Vector3(0f, -99f, 0f);
            Vector3 clamped = zone.ClampInside(input);
            Assert.AreEqual(-99f, clamped.y, 0.0001f);
        }

        //# 엣지 분기 1 — X 만 밖, Z 는 안 (변 케이스).
        [Test]
        public void ClampInside_X만_밖이면_X만_클램프되고_Z는_그대로()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 outside = new Vector3(20f, 0f, 2f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(7f, clamped.x, 0.001f, "X 는 Center.x + _clampHalfExtent(7) 로 클램프");
            Assert.AreEqual(2f, clamped.z, 0.001f, "Z 는 clamp 안 — 입력 그대로");
        }

        //# 엣지 분기 2 — Z 만 밖, X 는 안 (변 케이스).
        [Test]
        public void ClampInside_Z만_밖이면_Z만_클램프되고_X는_그대로()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 outside = new Vector3(2f, 0f, -20f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(2f, clamped.x, 0.001f);
            Assert.AreEqual(-7f, clamped.z, 0.001f, "Z 는 Center.z - _clampHalfExtent(-7) 로 클램프");
        }

        //# 엣지 분기 3 — X/Z 모두 밖 (모서리 케이스).
        [Test]
        public void ClampInside_XZ_둘다_밖이면_각각_클램프()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 outside = new Vector3(15f, 0f, 15f);
            Vector3 clamped = zone.ClampInside(outside);
            Assert.AreEqual(7f, clamped.x, 0.001f, "X 는 +extent(7) 로 클램프");
            Assert.AreEqual(7f, clamped.z, 0.001f, "Z 는 +extent(7) 로 클램프");
        }

        //# 엣지 — 정확히 경계 좌표는 무변동 (Mathf.Clamp 는 양 끝 포함).
        [Test]
        public void ClampInside_정확히_경계_좌표는_그대로()
        {
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(10, 1, 10), 7f);
            Vector3 corner = new Vector3(7f, 0f, 7f);
            Vector3 clamped = zone.ClampInside(corner);
            Assert.AreEqual(7f, clamped.x, 0.0001f);
            Assert.AreEqual(7f, clamped.z, 0.0001f);
        }

        //# 직사각 비대칭 — X/Z extent 가 다를 때 각 축이 자기 extent 로 클램프됨을 한 점으로 입증.
        //# X·Z 가 서로 다른 값으로 잘리므로 X/Z 축이 뒤바뀌면(스왑) 단언이 깨진다.
        [Test]
        public void ClampInside_X와_Z_extent가_다르면_각_축이_자기_extent로_클램프()
        {
            //# X extent 10 / Z extent 6 — production 직사각(20×12) 과 동일.
            BattleZone zone = CreateZoneWithClamp(Vector3.zero, new Vector3(22, 1, 15), 10f, 6f);
            Vector3 clamped = zone.ClampInside(new Vector3(15f, 0f, 8f));
            Assert.AreEqual(10f, clamped.x, 0.001f, "X 는 _clampHalfExtentX(10) 로 클램프");
            Assert.AreEqual(6f, clamped.z, 0.001f, "Z 는 _clampHalfExtentZ(6) 로 클램프 — X extent 와 다름");
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
