# BattleZone 시스템 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 카메라 가시영역을 `BattleZone` 컴포넌트의 BoxCollider 로 단일 진실화하고, 몬스터는 zone 밖에서 스폰 → 행진 → zone 진입 시 영웅 AI 타겟 후보로 전환되며, 영웅도 zone 밖에서 진입해 중심 도달 시 전투 시작.

**Architecture:** 씬에 단일 `BattleZone` GameObject 배치 — 직접 부착된 BoxCollider(isTrigger)가 zone 경계이자 OnTriggerEnter 이벤트 소스. 자식 Transform 들이 spawn point pool 과 영웅 entry 지점. `Awake` 에 4면 thin BoxCollider 가 인비저블 벽으로 자동 생성. 몬스터 상태는 `CharacterRegistry.Entry.IsEngaging` 단일 bool 로 표현, `TryFindNearestMonster` 가 필터. `HeroEntryDriver` 가 영웅을 중심까지 이동시킨 후 자기 비활성화하며 `OnHeroReachedCenter` 이벤트로 게임 시작 신호.

**Tech Stack:** Unity 6 (6000.0.68f1), C# 9, Unity Test Framework (NUnit), ChvjPackage 인프라

---

## 파일 구조

**신규 생성:**
```
Assets/_Lair/Scripts/Battle/BattleZone.cs                      ← 본 시스템의 단일 진실 컴포넌트
Assets/_Lair/Scripts/Character/HeroEntryDriver.cs              ← 영웅 zone 진입 단계 드라이버
Assets/_Lair/Tests/EditMode/Battle/BattleZoneTests.cs          ← BattleZone 시드 테스트 (gameplay-programmer 작성, 본격 스위트는 test-engineer)
Assets/_Lair/Tests/EditMode/Character/MonsterTagResetTests.cs  ← MonsterTag OnEnable 시 IsEngaging 리셋 검증
Assets/_Lair/Tests/EditMode/Character/SimpleMoverClampTests.cs ← SimpleMover._clampZone 회귀 검증
```

**수정:**
```
Assets/_Lair/Scripts/Character/CharacterRegistry.cs            ← Entry 에 IsEngaging + SetMonsterEngaging + 필터
Assets/_Lair/Scripts/Character/MonsterTag.cs                   ← OnEnable 에서 IsEngaging=false 초기화
Assets/_Lair/Scripts/Character/SimpleMover.cs                  ← _clampZone 필드 + FixedUpdate next 좌표 클램프 (영웅 한정)
Assets/_Lair/Scripts/Battle/Spawner.cs                         ← Bind(host, zone) + Tick 시 zone.GetRandomSpawn() 픽
Assets/_Lair/Scripts/Battle/BattleController.cs                ← _zone 주입, SpawnHero entry 흐름, BattleClock / Spawner Tick 게이트
Assets/_Lair/Tests/EditMode/Character/CharacterRegistryTests.cs ← IsEngaging 필터 회귀 테스트 보강
Assets/_Lair/Tests/EditMode/Battle/SpawnerTests.cs             ← Bind 새 시그니처, zone.GetRandomSpawn 픽 회귀
Assets/_Lair/Scenes/Battle.unity                               ← BattleZone GameObject 추가 (마지막 Task)
```

**범위 외 (spec §13 후속 작업):**
- AI leash (advance lane 수직 거리 필터)
- 다중 BattleZone / prefab variant
- Marching 상태 전용 VFX
- 카메라 줌 한계 자동 산출

---

## 사전 의사결정 — Spec 와 차이

Spec §3.1 은 BoxCollider 를 "Child: ZoneTrigger" 로 표기했으나, 본 plan 은 **BattleZone GameObject 본체에 직접 BoxCollider 를 부착**한다. 이유:
- `OnTriggerEnter` 는 Collider 가 있는 GameObject 의 컴포넌트에서만 호출됨. 자식 BoxCollider 로 두면 별도 forwarder 컴포넌트가 필요해 복잡.
- 자식 Transform 들(spawn points, hero entry)은 그대로 자식 유지.

기능적으로 spec 의 의도(zone 경계 + 자동 전환)는 동일하게 충족.

**design-reviewer B1/B2 정정 (2026-05-29)**: spec §3.3 의 "인비저블 벽 자동 생성" 채택 안 함. 사유 — 몬스터 6종 프리팹이 Dynamic Rigidbody 부착이라 BattleZone 본체의 Kinematic Compound Collider 가 몬스터의 zone 진입을 물리적으로 차단해 OnTriggerEnter 가 발화 안 됨. 대안 — 영웅 SimpleMover 에 `_clampZone` 필드 추가, `FixedUpdate` 의 next 좌표를 `BattleZone.ClampInside` 로 클램프 (영웅 한정).

**design-reviewer B3 정정 (2026-05-29)**: Task 9 좌표가 zone size 24 기준으로 갱신됨 (이전 plan 은 16 기준). game-designer 기획서 §2.3·§3.2·§5.1 과 일치.

---

## Task 1: CharacterRegistry — IsEngaging 필드 + 필터

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CharacterRegistry.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Character/CharacterRegistryTests.cs`

- [ ] **Step 1: 회귀 테스트 추가 — Marching 몬스터는 검색 결과에서 제외**

`Assets/_Lair/Tests/EditMode/Character/CharacterRegistryTests.cs` 의 클래스 안에 다음 테스트 추가:

```csharp
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
```

- [ ] **Step 2: 테스트 실패 확인**

UnityMCP `editor_execute_menu` 로 `Lair/Tests/Run EditMode Tests` 실행. `Library/lair-test-result.json` 의 `failures` 에 `TryFindNearestMonster_Marching_몬스터_제외` 와 `SetMonsterEngaging_미등록_몬스터_무동작` 표시 확인 (컴파일 에러여도 OK — 다음 step 에 production 추가).

- [ ] **Step 3: Entry 에 IsEngaging 필드 + SetMonsterEngaging + 필터 추가**

`Assets/_Lair/Scripts/Character/CharacterRegistry.cs` 를 다음으로 교체:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅·몬스터 정적 레지스트리. 캐릭터의 OnEnable/OnDisable 에서 자기 등록.
    //# TryFindNearest 는 IsAlive 필터링 + 거리순 정렬 후 최근접 1개 반환.
    //# 몬스터는 추가로 IsEngaging=true 만 영웅 AI 의 타겟 후보 (BattleZone 진입 후 true).
    public static class CharacterRegistry
    {
        public class Entry
        {
            public Transform Transform;
            public IHealth Health;
            //# Marching → Engaging 상태. 영웅은 무관 (Hero 검색은 무시).
            //# 풀 Pop 직후엔 false (MonsterTag.OnEnable 이 보장). BattleZone trigger 진입 시 true.
            public bool IsEngaging;
        }

        public static readonly List<Entry> Heroes = new();
        public static readonly List<Entry> Monsters = new();

        public static void RegisterHero(Transform t, IHealth h)   => Add(Heroes, t, h);
        public static void UnregisterHero(Transform t)            => Remove(Heroes, t);
        public static void RegisterMonster(Transform t, IHealth h)=> Add(Monsters, t, h);
        public static void UnregisterMonster(Transform t)         => Remove(Monsters, t);

        //# 몬스터의 Marching/Engaging 상태 전환 — BattleZone.OnTriggerEnter 또는 MonsterTag.OnEnable 이 호출.
        public static void SetMonsterEngaging(Transform t, bool engaging)
        {
            if (t == null) return;
            foreach (Entry e in Monsters)
            {
                if (e.Transform == t)
                {
                    e.IsEngaging = engaging;
                    return;
                }
            }
        }

        public static bool TryFindNearestHero(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Heroes, from, out t, out h, requireEngaging: false);
        public static bool TryFindNearestMonster(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Monsters, from, out t, out h, requireEngaging: true);

        private static void Add(List<Entry> list, Transform t, IHealth h)
        {
            if (t == null) return;
            //# 신규 등록 — IsEngaging 기본 false. 몬스터는 MonsterTag.OnEnable 이 재호출.
            list.Add(new Entry { Transform = t, Health = h, IsEngaging = false });
        }

        private static void Remove(List<Entry> list, Transform t)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].Transform == t) list.RemoveAt(i);
            }
        }

        private static bool TryFindNearest(
            List<Entry> list, Vector3 from, out Transform t, out IHealth h, bool requireEngaging)
        {
            t = null; h = null;
            float best = float.MaxValue;
            foreach (Entry e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                //# 몬스터 검색은 Engaging 만 후보 — 영웅 검색(requireEngaging=false)엔 무영향.
                if (requireEngaging && e.IsEngaging == false) continue;
                float d = (e.Transform.position - from).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    t = e.Transform;
                    h = e.Health;
                }
            }
            return t != null;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests` 재실행. `CharacterRegistryTests` 의 모든 케이스 `pass`. 특히 기존 `TryFindNearestMonster_거리순_가장_가까운` 테스트가 이제 fail 해야 함 — 등록만 한 몬스터들이 IsEngaging=false 라 검색에서 빠진다.

- [ ] **Step 5: 기존 테스트 보정**

`TryFindNearestMonster_거리순_가장_가까운` 와 `TryFindNearestMonster_죽은_적_제외` 테스트가 이제 모두 IsEngaging=true 로 등록을 마쳐야 통과. 두 테스트의 `RegisterMonster` 호출 직후 `SetMonsterEngaging(t, true)` 한 줄씩 추가:

```csharp
//# TryFindNearestMonster_거리순_가장_가까운 — Register 직후
CharacterRegistry.RegisterMonster(near, new FakeHealth());
CharacterRegistry.RegisterMonster(far, new FakeHealth());
CharacterRegistry.SetMonsterEngaging(near, true);
CharacterRegistry.SetMonsterEngaging(far, true);

//# TryFindNearestMonster_죽은_적_제외 — Register 직후
CharacterRegistry.RegisterMonster(alive, aliveHp);
CharacterRegistry.RegisterMonster(dead, deadHp);
CharacterRegistry.SetMonsterEngaging(alive, true);
CharacterRegistry.SetMonsterEngaging(dead, true);
```

- [ ] **Step 6: 전체 EditMode 테스트 재실행**

`Lair/Tests/Run EditMode Tests`. `CharacterRegistryTests` 전체 `pass` 확인. 다른 테스트 회귀 0건.

---

## Task 2: MonsterTag — OnEnable 시 IsEngaging 리셋

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/MonsterTag.cs`
- Create: `Assets/_Lair/Tests/EditMode/Character/MonsterTagResetTests.cs`

- [ ] **Step 1: 회귀 테스트 추가 — OnEnable 시 IsEngaging 리셋**

`Assets/_Lair/Tests/EditMode/Character/MonsterTagResetTests.cs` 신규:

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# 풀 재사용(OnEnable 재호출) 시 MonsterTag 가 자기 Transform 의 IsEngaging 을 false 로 리셋.
    public class MonsterTagResetTests
    {
        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [Test]
        public void OnEnable_재호출시_IsEngaging_false_리셋()
        {
            GameObject go = new GameObject("MonsterUT");
            MonsterTag tag = go.AddComponent<MonsterTag>();
            FakeHealth health = new FakeHealth();

            //# Register + Engaging 상태 진입 (zone 안에 있는 몬스터 가정)
            CharacterRegistry.RegisterMonster(go.transform, health);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging);

            //# 풀 재사용 시뮬레이션 — OnEnable 리플렉션 호출.
            MethodInfo mi = typeof(MonsterTag).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "MonsterTag.OnEnable 메서드 존재 — 시그니처 변경 감지");
            mi.Invoke(tag, null);

            Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging,
                "OnEnable 재호출 후 IsEngaging 가 false (Marching 상태로 복귀)");

            Object.DestroyImmediate(go);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

`Lair/Tests/Run EditMode Tests`. `OnEnable_재호출시_IsEngaging_false_리셋` fail — `MonsterTag.OnEnable` 메서드 자체가 없음.

- [ ] **Step 3: MonsterTag 에 OnEnable 추가**

`Assets/_Lair/Scripts/Character/MonsterTag.cs`:

```csharp
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 프리팹에 부착되어 EMonster 값을 직렬화.
    //# BattleContext.GetMonsters(filter) 가 이를 통해 위스프/레이스/리퍼 구분.
    //# 풀 재사용 시 자기 Transform 의 IsEngaging 를 false 로 리셋 — Marching 상태 보장.
    public class MonsterTag : MonoBehaviour
    {
        [SerializeField] private EMonster _key;
        public EMonster Key => _key;

        //# 빌더 또는 런타임 동적 설정
        public void Configure(EMonster k) => _key = k;

        //# 풀 Pop 시 자동 호출. CharacterRegistry 등록은 MonsterTargetProvider 가 담당하므로
        //# 여기선 IsEngaging 만 리셋. 등록 안 된 상태에서 호출돼도 SetMonsterEngaging 가 no-op.
        private void OnEnable()
        {
            CharacterRegistry.SetMonsterEngaging(transform, false);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests`. `MonsterTagResetTests` 전 케이스 `pass`. 회귀 0건.

---

## Task 3: BattleZone (skeleton) — Bounds, API, Gizmo

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/BattleZone.cs`
- Create: `Assets/_Lair/Tests/EditMode/Battle/BattleZoneTests.cs`

- [ ] **Step 1: BattleZone skeleton 테스트**

`Assets/_Lair/Tests/EditMode/Battle/BattleZoneTests.cs`:

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# BattleZone 시드 테스트 — IsInside / GetRandomSpawn / ClampInside.
    //# gameplay-programmer 가 핵심 동작을 보장하는 최소 테스트. 본격 엣지 케이스는 test-engineer 단계.
    public class BattleZoneTests
    {
        private GameObject _zoneGo;

        [TearDown]
        public void TearDown()
        {
            if (_zoneGo != null) Object.DestroyImmediate(_zoneGo);
        }

        //# 본체 BoxCollider 의 bounds 가 IsInside / ClampInside 의 진실. center=(0,0,0), size=(10,1,10) 기준.
        private BattleZone CreateZone(Vector3 center, Vector3 size)
        {
            _zoneGo = new GameObject("BattleZoneUT");
            _zoneGo.transform.position = center;
            BoxCollider col = _zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            BattleZone zone = _zoneGo.AddComponent<BattleZone>();
            //# Awake 가 _zoneTrigger 폴백 — EditMode 에서는 리플렉션 호출.
            InvokeAwake(zone);
            return zone;
        }

        private static void InvokeAwake(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null) mi.Invoke(c, null);
        }

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

        [Test]
        public void Center_BoxCollider_bounds_center()
        {
            BattleZone zone = CreateZone(new Vector3(3, 0, 3), new Vector3(10, 1, 10));
            Assert.AreEqual(new Vector3(3, 0, 3), zone.Center);
        }

        [Test]
        public void GetRandomSpawn_spawnPoints_없으면_null()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            //# _spawnPoints 미할당 — null array.
            Assert.IsNull(zone.GetRandomSpawn());
        }

        [Test]
        public void GetRandomSpawn_할당시_배열중_하나_반환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject sp1 = new GameObject("sp1"); sp1.transform.position = new Vector3(7, 0, 0);
            GameObject sp2 = new GameObject("sp2"); sp2.transform.position = new Vector3(0, 0, 7);
            SetPrivate(zone, "_spawnPoints", new Transform[] { sp1.transform, sp2.transform });

            Transform picked = zone.GetRandomSpawn();
            Assert.IsNotNull(picked);
            Assert.IsTrue(picked == sp1.transform || picked == sp2.transform);

            Object.DestroyImmediate(sp1);
            Object.DestroyImmediate(sp2);
        }

        [Test]
        public void HeroEntryPoint_할당된_Transform_반환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
            GameObject hep = new GameObject("HeroEntry"); hep.transform.position = new Vector3(-8, 0, 0);
            SetPrivate(zone, "_heroEntryPoint", hep.transform);
            Assert.AreSame(hep.transform, zone.HeroEntryPoint);
            Object.DestroyImmediate(hep);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"BattleZone.{field} 필드가 존재해야 함");
            fi.SetValue(target, value);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

`Lair/Tests/Run EditMode Tests`. 컴파일 에러 ( `BattleZone` 클래스 없음) — OK.

- [ ] **Step 3: BattleZone skeleton 작성**

`Assets/_Lair/Scripts/Battle/BattleZone.cs`:

```csharp
using System;
using UnityEngine;

namespace Lair.Battle
{
    //# 씬 단일 인스턴스. 전장 경계(BoxCollider isTrigger) + spawn point pool + hero entry 지점.
    //# 영웅 차단은 SimpleMover._clampZone 의 ClampInside 호출로 처리 (인비저블 벽 자동 생성 안 함 — design-reviewer B1).
    [RequireComponent(typeof(BoxCollider))]
    public class BattleZone : MonoBehaviour
    {
        //# 가시영역 안쪽 사각형. isTrigger=true. 본체 GameObject 에 직접 부착 — OnTriggerEnter 직수신.
        [SerializeField] private BoxCollider _zoneTrigger;
        //# 4 edge 분산 spawn point. 신경님 수동 배치 (zone 밖, 거리 = moveSpeed × 1.0초).
        [SerializeField] private Transform[] _spawnPoints;
        //# 영웅이 zone 진입 전 머무는 한 고정 위치 (zone 밖).
        [SerializeField] private Transform _heroEntryPoint;

        //# 영웅이 zone 중심 도달 시 1회 발행. BattleController 가 구독해 BattleClock + Spawner Tick 활성화.
        public event Action OnHeroReachedCenter;

        public Vector3 Center => _zoneTrigger != null ? _zoneTrigger.bounds.center : transform.position;
        public Transform HeroEntryPoint => _heroEntryPoint;

        //# bounds.Contains — XYZ 모든 축. 단순 사각형 판정.
        public bool IsInside(Vector3 worldPos)
        {
            if (_zoneTrigger == null) return false;
            return _zoneTrigger.bounds.Contains(worldPos);
        }

        //# 영웅 SimpleMover 가 매 FixedUpdate next 좌표 클램프에 사용. zone 밖이면 bounds 안쪽 가장자리로.
        //# Y 평면 (X/Z) 만 클램프 — Y 는 입력 그대로 (SimpleMover 가 어차피 0 으로 고정).
        public Vector3 ClampInside(Vector3 worldPos)
        {
            if (_zoneTrigger == null) return worldPos;
            Bounds b = _zoneTrigger.bounds;
            float x = Mathf.Clamp(worldPos.x, b.min.x, b.max.x);
            float z = Mathf.Clamp(worldPos.z, b.min.z, b.max.z);
            return new Vector3(x, worldPos.y, z);
        }

        //# _spawnPoints 가 비어있으면 null. 비-null 배열 안에서 균등 랜덤 픽.
        public Transform GetRandomSpawn()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0) return null;
            int idx = UnityEngine.Random.Range(0, _spawnPoints.Length);
            return _spawnPoints[idx];
        }

        //# HeroEntryDriver 가 Center 도달 시 호출 — 이벤트 발행.
        public void NotifyHeroReachedCenter()
        {
            OnHeroReachedCenter?.Invoke();
        }

        //# RequireComponent 보장 — Awake 시점에 BoxCollider 존재. _zoneTrigger 미할당이면 GetComponent 로 자동 픽업.
        private void Awake()
        {
            if (_zoneTrigger == null) _zoneTrigger = GetComponent<BoxCollider>();
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests`. `BattleZoneTests` 의 `IsInside_*`, `Center_*`, `GetRandomSpawn_*`, `HeroEntryPoint_*` 모두 `pass`.

---

## Task 4: SimpleMover — `_clampZone` 옵션 (영웅 한정 zone 차단)

> design-reviewer B1/B2 정정: 인비저블 벽 자동 생성은 채택 안 함 (몬스터 Dynamic Rigidbody 와 충돌 매트릭스 충돌 — zone 진입 차단 위험). 대신 영웅 SimpleMover 의 FixedUpdate 가 매 next 좌표를 `BattleZone.ClampInside` 로 클램프.

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/SimpleMover.cs`
- Create: `Assets/_Lair/Tests/EditMode/Character/SimpleMoverClampTests.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Battle/BattleZoneTests.cs` (ClampInside 회귀 테스트)

- [ ] **Step 1: BattleZone.ClampInside 회귀 테스트 추가**

`BattleZoneTests.cs` 의 클래스 안에 추가:

```csharp
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
```

- [ ] **Step 2: BattleZoneTests 통과 확인**

`Lair/Tests/Run EditMode Tests`. 세 케이스 `pass` (Task 3 에서 ClampInside 가 이미 production 에 있어 즉시 통과).

- [ ] **Step 3: SimpleMover.cs 클램프 테스트**

`Assets/_Lair/Tests/EditMode/Character/SimpleMoverClampTests.cs` 신규:

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# SimpleMover._clampZone 옵션 — 영웅 한정 zone-clamp. 몬스터는 미할당 (null) 이라 무동작.
    public class SimpleMoverClampTests
    {
        private GameObject _zoneGo;
        private GameObject _moverGo;

        [TearDown]
        public void TearDown()
        {
            if (_moverGo != null) Object.DestroyImmediate(_moverGo);
            if (_zoneGo != null) Object.DestroyImmediate(_zoneGo);
        }

        private BattleZone CreateZone()
        {
            _zoneGo = new GameObject("ZoneUT");
            BoxCollider col = _zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(10, 1, 10);
            BattleZone zone = _zoneGo.AddComponent<BattleZone>();
            MethodInfo mi = typeof(BattleZone).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(zone, null);
            return zone;
        }

        private SimpleMover CreateMover(Vector3 startPos, BattleZone clampZone)
        {
            _moverGo = new GameObject("MoverUT");
            _moverGo.transform.position = startPos;
            SimpleMover mover = _moverGo.AddComponent<SimpleMover>();
            mover.Speed = 100f;   //# 한 FixedUpdate 에 충분히 큰 이동량
            if (clampZone != null)
            {
                FieldInfo fi = typeof(SimpleMover).GetField("_clampZone",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(fi, "SimpleMover._clampZone 필드 존재");
                fi.SetValue(mover, clampZone);
            }
            return mover;
        }

        //# FixedUpdate 를 리플렉션으로 1회 호출 — EditMode 에서 물리 시뮬 없이 검증.
        private static void InvokeFixedUpdate(SimpleMover mover)
        {
            MethodInfo mi = typeof(SimpleMover).GetMethod("FixedUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SimpleMover.FixedUpdate 메서드 존재");
            mi.Invoke(mover, null);
        }

        [Test]
        public void clampZone_null_이면_zone_밖으로_자유_이동()
        {
            //# 몬스터 시뮬레이션 — _clampZone 미할당.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: null);
            //# zone 밖 X=20 으로 이동 명령.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# Speed=100 + 큰 fixedDeltaTime — 한 번에 도달.
            Assert.Greater(_moverGo.transform.position.x, 5f,
                "_clampZone null — 자유 이동, zone 경계(5)를 넘어감");
        }

        [Test]
        public void clampZone_할당시_zone_경계로_clamp()
        {
            BattleZone zone = CreateZone();
            //# 영웅 시뮬레이션 — _clampZone 할당.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);
            //# zone 밖 X=20 으로 이동 명령.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# 한 번에 X=20 직진하지만, ClampInside 가 X=5 로 자름.
            Assert.LessOrEqual(_moverGo.transform.position.x, 5.001f,
                "_clampZone 할당 — zone bounds.max.x (5) 안쪽으로 클램프");
        }
    }
}
```

- [ ] **Step 4: 테스트 실패 확인**

`Lair/Tests/Run EditMode Tests`. `clampZone_할당시_*` 케이스 fail — `_clampZone` 필드 없음. `clampZone_null_이면_*` 는 컴파일 에러 시점에 양쪽 다 안 돔.

- [ ] **Step 5: SimpleMover 에 `_clampZone` + 클램프 로직 추가**

`Assets/_Lair/Scripts/Character/SimpleMover.cs` 의 `FixedUpdate` 와 필드 선언부 변경. 전체 파일을 다음으로 교체:

> **game-designer §12.B 정정 (2026-05-29)**: 영웅은 풀 스폰 (`CHMPool.Instance.Pop`) 으로 생성되어 씬 인스펙터의 `_clampZone` 와이어링이 작동 불가. `_clampZone` 은 SerializeField 유지하되 기본 null + `public void BindClampZone(BattleZone)` 메서드로 런타임 주입. `BattleController.SpawnHero` 의 Pop 직후 호출 (Task 8 에서 통합).

```csharp
using Lair.Battle;
using UnityEngine;

namespace Lair.Character
{
    //# Vector3.MoveTowards 기반 단순 추적. Rigidbody 있으면 MovePosition 사용, 없으면 transform.position 폴백.
    //# _clampZone 비-null 이면 next 좌표를 BattleZone.ClampInside 로 클램프 — 영웅 한정 (몬스터는 null).
    //# 영웅은 풀 스폰이라 BattleController.SpawnHero 가 Pop 직후 BindClampZone 호출. 몬스터는 호출 안 함 → 무동작.
    public class SimpleMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed = 3f;
        //# 런타임 주입 — BindClampZone 으로 설정. 인스펙터 와이어링은 풀 Pop 시 reference broken 이라 사용 안 함.
        [SerializeField] private BattleZone _clampZone;
        private bool _moving;
        private Vector3 _target;
        private Rigidbody _rigidbody;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        //# B3 — MoveTo 후 Stop 전까지 true. 출혈 카드가 영웅 이동 판정에 사용.
        public bool IsMoving => _moving;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void MoveTo(Vector3 target)
        {
            _target = target;
            _moving = true;
        }

        public void Stop()
        {
            _moving = false;
        }

        //# 런타임 주입 — BattleController.SpawnHero 가 Pop 직후 호출. null 도 허용 (몬스터 또는 폴백 해제).
        public void BindClampZone(BattleZone zone)
        {
            _clampZone = zone;
        }

        private void FixedUpdate()
        {
            if (_moving == false)
                return;

            Vector3 next = Vector3.MoveTowards(transform.position, _target, _speed * Time.fixedDeltaTime);
            //# Y 평면 고정 — 캐릭터가 카메라 각도로 떠오르지 않도록
            next.y = 0f;

            //# 영웅 한정 zone-clamp — _clampZone 미할당이면 무동작.
            if (_clampZone != null)
                next = _clampZone.ClampInside(next);

            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }
        }
    }
}
```

- [ ] **Step 6: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests`. `SimpleMoverClampTests` 두 케이스 `pass`. 기존 SimpleMover 회귀 0건 (Speed/IsMoving/MoveTo/Stop 동작 무변경).

---

## Task 5: BattleZone — OnTriggerEnter 시 IsEngaging=true 전환

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleZone.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Battle/BattleZoneTests.cs`

- [ ] **Step 1: 회귀 테스트 추가**

`BattleZoneTests.cs` 의 using 절에 `using Lair.Character;`, `using Lair.Tests.Helpers;` 추가. 클래스 안에 추가:

```csharp
[Test]
public void OnTriggerEnter_MonsterTag_있으면_IsEngaging_true()
{
    CharacterRegistry.Heroes.Clear();
    CharacterRegistry.Monsters.Clear();

    BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
    GameObject m = new GameObject("Monster");
    m.AddComponent<MonsterTag>();
    BoxCollider mc = m.AddComponent<BoxCollider>();

    CharacterRegistry.RegisterMonster(m.transform, new FakeHealth());
    Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging, "초기 Marching");

    //# OnTriggerEnter 직접 호출 — EditMode 라 물리 시뮬레이션 없음.
    MethodInfo mi = typeof(BattleZone).GetMethod("OnTriggerEnter",
        BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(mi, "BattleZone.OnTriggerEnter 메서드 존재");
    mi.Invoke(zone, new object[] { mc });

    Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging,
        "MonsterTag 진입 후 IsEngaging=true");

    Object.DestroyImmediate(m);
}

[Test]
public void OnTriggerEnter_MonsterTag_없으면_무동작()
{
    CharacterRegistry.Heroes.Clear();
    CharacterRegistry.Monsters.Clear();

    BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));
    //# MonsterTag 없는 GameObject — 영웅이나 다른 컬라이더 시뮬레이션.
    GameObject other = new GameObject("Other");
    BoxCollider oc = other.AddComponent<BoxCollider>();

    MethodInfo mi = typeof(BattleZone).GetMethod("OnTriggerEnter",
        BindingFlags.NonPublic | BindingFlags.Instance);
    //# MonsterTag 없으니 SetMonsterEngaging 호출도 안 일어남. 예외 없이 무동작.
    Assert.DoesNotThrow(() => mi.Invoke(zone, new object[] { oc }));

    Object.DestroyImmediate(other);
}
```

- [ ] **Step 2: 테스트 실패 확인**

`Lair/Tests/Run EditMode Tests`. fail — `OnTriggerEnter` 메서드 없음.

- [ ] **Step 3: OnTriggerEnter 구현**

`BattleZone.cs` 끝에 추가 (using 절에 `using Lair.Character;` 도 추가):

```csharp
//# zone 본체의 BoxCollider(isTrigger) 가 OnTriggerEnter 발행.
//# MonsterTag 있는 Collider 만 Engaging 으로 전환. 영웅은 MonsterTag 가 없어 자동 무시.
private void OnTriggerEnter(Collider other)
{
    if (other == null) return;
    MonsterTag tag = other.GetComponent<MonsterTag>();
    if (tag == null) return;
    CharacterRegistry.SetMonsterEngaging(other.transform, true);
}
```

- [ ] **Step 4: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests`. `OnTriggerEnter_*` 케이스 `pass`. 회귀 0건.

---

## Task 6: Spawner — Bind 시 BattleZone 주입 + Tick 시 GetRandomSpawn 픽

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/Spawner.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Battle/SpawnerTests.cs`

- [ ] **Step 1: 회귀 테스트 추가**

`SpawnerTests.cs` 의 클래스 안에 추가:

```csharp
[Test]
public void Bind_with_zone_Tick시_zone_랜덤픽_사용()
{
    FakeSpawnerHost host = new FakeSpawnerHost();
    Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
    sp.transform.position = new Vector3(30f, 0f, 0f);    //# 본체 위치 무관

    //# BattleZone + spawn points 준비.
    GameObject zoneGo = new GameObject("BattleZoneUT");
    BoxCollider col = zoneGo.AddComponent<BoxCollider>();
    col.isTrigger = true; col.size = new Vector3(10, 1, 10);
    BattleZone zone = zoneGo.AddComponent<BattleZone>();
    _spawned.Add(zoneGo);
    GameObject sp1 = new GameObject("sp1"); sp1.transform.position = new Vector3(8f, 0f, 0f);
    _spawned.Add(sp1);
    SetPrivate(zone, "_spawnPoints", new Transform[] { sp1.transform });

    //# 새 시그니처 — Bind(host, zone).
    sp.Bind(host, zone);
    sp.Tick(0f);

    Assert.AreEqual(new Vector3(8f, 0f, 0f), host.Spawns[0].pos,
        "Bind 의 zone.GetRandomSpawn() 위치 사용 — transform.position(30)/_spawnPoint 어느 쪽도 아님");
}

[Test]
public void Bind_zone_null_이면_기존_spawnPoint_fallback()
{
    FakeSpawnerHost host = new FakeSpawnerHost();
    Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
    sp.transform.position = new Vector3(30f, 0f, 0f);
    GameObject sp1 = new GameObject("spawnPoint"); sp1.transform.position = new Vector3(8f, 0f, 0f);
    _spawned.Add(sp1);
    SetPrivate(sp, "_spawnPoint", sp1.transform);

    //# 새 시그니처 — zone=null. _spawnPoint 가 fallback.
    sp.Bind(host, null);
    sp.Tick(0f);

    Assert.AreEqual(new Vector3(8f, 0f, 0f), host.Spawns[0].pos,
        "zone null fallback — _spawnPoint(8) 사용");
}
```

- [ ] **Step 2: 기존 테스트 보정 — Bind 시그니처 변경**

`SpawnerTests.cs` 의 모든 `sp.Bind(host)` 호출을 `sp.Bind(host, null)` 로 일괄 치환. 기존 동작 유지 (zone 없으면 transform/_spawnPoint fallback).

또한 `OnEnable_재호출시_*` 테스트의 `sp.Bind(host)` 도 동일 치환:

```csharp
//# 변경 전
sp.Bind(host);
//# 변경 후
sp.Bind(host, null);
```

- [ ] **Step 3: 테스트 실패 확인**

`Lair/Tests/Run EditMode Tests`. 컴파일 에러 — `Bind(host, null)` 의 두 번째 인자 미정의.

- [ ] **Step 4: Spawner Bind 시그니처 + zone 픽 로직 추가**

`Assets/_Lair/Scripts/Battle/Spawner.cs` 의 변경:

(a) 클래스에 `_zone` 필드 추가 (런타임 내부 상태 섹션):
```csharp
//# BattleController 가 Bind 시 주입. null 이면 _spawnPoint → transform.position 순으로 fallback.
private BattleZone _zone;
```

(b) `Bind` 메서드 시그니처 변경:
```csharp
//# BattleController 가 수집 시 1회 주입. zone 은 BattleZone.GetRandomSpawn() 픽 소스.
public void Bind(ISpawnerHost host, BattleZone zone)
{
    _host = host;
    _zone = zone;
}
```

(c) `Tick` 의 spawn 위치 계산을 다음으로 교체:
```csharp
//# 스폰 위치 우선순위 — zone.GetRandomSpawn() > _spawnPoint > transform.position.
Vector3 spawnPos = transform.position;
if (_zone != null)
{
    Transform pick = _zone.GetRandomSpawn();
    if (pick != null) spawnPos = pick.position;
    else if (_spawnPoint != null) spawnPos = _spawnPoint.position;
}
else if (_spawnPoint != null)
{
    spawnPos = _spawnPoint.position;
}
_host.SpawnFromSpawner(_currentType, spawnPos, _outputCount);
```

- [ ] **Step 5: 테스트 통과 확인**

`Lair/Tests/Run EditMode Tests`. 새 두 테스트 `pass` + 기존 Spawner 테스트 전체 `pass` (보정한 시그니처 호환).

---

## Task 7: HeroEntryDriver — 영웅을 zone 중심까지 이동시키고 이벤트 발행

**Files:**
- Create: `Assets/_Lair/Scripts/Character/HeroEntryDriver.cs`

> 이 컴포넌트의 핵심 동작은 PlayMode 통합 테스트(Task 9·test-engineer 단계) 영역. 본 task 는 production 코드만 추가. 시그니처 회귀는 BattleController 통합 테스트에서 보장.

- [ ] **Step 1: HeroEntryDriver 작성**

`Assets/_Lair/Scripts/Character/HeroEntryDriver.cs`:

```csharp
using Lair.Battle;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 zone 진입 단계 드라이버. AutoCombatAI 가 비활성화된 동안 영웅을 BattleZone.Center 로 이동시킨다.
    //# 도달 시 BattleZone.NotifyHeroReachedCenter() 호출 후 자기 비활성화. AutoCombatAI 활성화는 BattleController 가 담당.
    //# Rule 02 §5/§7 — IMover/IRotator 인터페이스 의존 (구체 클래스 직접 참조 회피).
    [RequireComponent(typeof(IMover))]
    public class HeroEntryDriver : MonoBehaviour
    {
        //# 도달 임계값 (m). spec §10 — 0.5m.
        [SerializeField] private float _arriveThreshold = 0.5f;

        private BattleZone _zone;
        private IMover _mover;
        private IRotator _rotator;
        private IHealth _health;
        private bool _notified;

        public void Bind(BattleZone zone)
        {
            _zone = zone;
            _notified = false;
        }

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _rotator = GetComponent<IRotator>();
            _health = GetComponent<IHealth>();
        }

        private void Update()
        {
            if (_zone == null) return;
            if (_notified) return;
            //# 영웅 사망 — march 중단 (현실적으로 발생 안 함, 안전망).
            if (_health != null && _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }

            Vector3 center = _zone.Center;
            Vector3 dir = center - transform.position;
            //# Y 평면 거리만 — SimpleMover 가 Y=0 고정이라 일관성 유지.
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dist <= _arriveThreshold)
            {
                _mover?.Stop();
                _notified = true;
                _zone.NotifyHeroReachedCenter();
                enabled = false;   //# 1회 동작 후 비활성화. BattleController 가 AutoCombatAI.enabled = true 로 전환.
                return;
            }

            _rotator?.FaceDirection(dir);
            _mover.MoveTo(center);
        }

        //# 풀 재사용 잔존 상태 방지 — OnDisable 에서 _notified 만 유지(다음 Bind 가 리셋).
        //# OnEnable 자체에서는 외부 Bind 없이 동작 시작 못 하므로 추가 처리 불필요.
    }
}
```

- [ ] **Step 2: 컴파일 검증**

UnityMCP `editor_refresh_assets` 후 `editor_recompile` → `editor_wait_ready` → `editor_read_log` (types=Error).
컴파일 에러 0건 확인.

- [ ] **Step 3: EditMode 전체 재실행 — 회귀 0건**

`Lair/Tests/Run EditMode Tests`. 모든 기존 테스트 `pass`. HeroEntryDriver 전용 테스트는 Task 9(통합) 에서 다룬다.

---

## Task 8: BattleController 통합 — entry 흐름, BattleClock/Spawner 게이트

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: BattleZone 직렬화 필드 + 게이트 플래그 추가**

`BattleController.cs` 의 필드 선언부 (`[SerializeField] private Transform _heroSpawn;` 근처) 를 다음으로 교체:

```csharp
//# Entry/march 시스템 단일 진실. 씬에 BattleZone GameObject 1개 배치 후 인스펙터 할당.
//# 미할당 시 _heroSpawn fallback 동작 (안전 경로).
[SerializeField] private BattleZone _zone;

//# (deprecated, fallback only — BattleZone 미할당 시 사용) 영웅 초기 스폰 Transform.
[SerializeField] private Transform _heroSpawn;

//# 지속 스폰 — 씬에 배치된 Spawner 들. Rule 03 — FindObjectsOfType 대신 인스펙터 직렬 할당.
[SerializeField] private Spawner[] _spawners;
//# Slice C — 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
[SerializeField] private BalanceConfig _balance;
```

다른 필드 옆에 게이트 플래그 추가 (예: `private bool _processingQueue;` 근처):

```csharp
//# 영웅이 zone 중심에 도달했는지. true 가 되면 Update 의 Spawner.Tick 호출이 활성화된다.
private bool _spawnersActive;

//# HeroEntryDriver 핸들 — Center 도달 후 비활성화 fallback 용.
private HeroEntryDriver _heroEntryDriver;
```

- [ ] **Step 2: SpawnHero 흐름 변경 — BattleZone.HeroEntryPoint 사용 + HeroEntryDriver 활성**

`SpawnHero` 메서드를 다음으로 교체:

```csharp
private async Task SpawnHero()
{
    GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
    if (prefab == null)
    {
        Debug.LogError("[BattleController] Knight 프리팹 로드 실패");
        return;
    }

    CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
    if (p == null) return;
    //# 우선순위 — BattleZone.HeroEntryPoint > _heroSpawn > Vector3.zero.
    Vector3 spawnPos;
    if (_zone != null && _zone.HeroEntryPoint != null)
        spawnPos = _zone.HeroEntryPoint.position;
    else if (_heroSpawn != null)
        spawnPos = _heroSpawn.position;
    else
        spawnPos = Vector3.zero;
    p.transform.position = spawnPos;

    _hero = p;
    _heroHealth = p.GetComponent<Health>();
    if (_balance != null) ApplyStats(p.gameObject, _balance.Hero);
    if (_heroHealth != null)
    {
        _heroHealth.OnChanged += _vm.UpdateHeroHp;
        _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
        _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
    }

    //# 영웅 AutoCombatAI 비활성 — HeroEntryDriver 가 march 를 수행.
    foreach (AutoCombatAI ai in p.GetComponentsInChildren<AutoCombatAI>())
        if (ai != null) ai.enabled = false;

    //# BattleZone 이 있을 때만 HeroEntryDriver 동작. 없으면 기존 EnableHeroAIAfterDelay 폴백.
    if (_zone != null)
    {
        //# 풀 Pop 직후 SimpleMover._clampZone 런타임 주입 — 씬 인스펙터 와이어링은 풀 reference broken 이라 사용 불가 (game-designer §12.B).
        SimpleMover heroMover = p.GetComponent<SimpleMover>();
        if (heroMover != null) heroMover.BindClampZone(_zone);

        _heroEntryDriver = p.GetComponent<HeroEntryDriver>();
        if (_heroEntryDriver == null)
            _heroEntryDriver = p.gameObject.AddComponent<HeroEntryDriver>();
        _heroEntryDriver.Bind(_zone);
        _heroEntryDriver.enabled = true;
        _zone.OnHeroReachedCenter += HandleHeroReachedCenter;
    }
    else
    {
        //# 폴백 — 기존 3초 후 AI 활성화.
        _ = EnableHeroAIAfterDelay(3f);
        _spawnersActive = true;   //# zone 미할당 시 즉시 spawner 활성
    }
}
```

- [ ] **Step 3: Start 메서드에서 EnableHeroAIAfterDelay 호출 위치 정리 + BattleClock 시작 분기**

기존 `Start` 안의 두 줄:

```csharp
await SpawnHero();
//# 3초 후 영웅 AutoCombatAI 재활성화 — Start() 를 막지 않도록 백그라운드 실행.
_ = EnableHeroAIAfterDelay(3f);
BindSpawners();
```

를 다음으로 교체:

```csharp
await SpawnHero();
//# zone 폴백 분기에서 이미 EnableHeroAIAfterDelay 호출됨. zone 활성 분기는 HandleHeroReachedCenter 가 담당.
BindSpawners();
```

그리고 기존 `Start` 끝부분의 `_clock.Start();` 를 게이트 조건 분기로 교체:

```csharp
//# 5. 시계
_clock = new BattleClock(_model.TotalSeconds);
_clock.OnTick   += _vm.UpdateTimer;
_clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
//# BattleZone 활성 분기 — HandleHeroReachedCenter 가 Start() 호출. 폴백 분기는 즉시.
if (_zone == null) _clock.Start();
```

ActiveTriggerService 가 `_clock` 에 의존하므로 그 위치는 그대로 유지 (트리거 발화는 clock 이 Tick 호출돼야 일어남 — Start 전엔 무동작).

- [ ] **Step 4: HandleHeroReachedCenter 추가**

`BattleController.cs` 의 `private void HandleMonsterDied(Vector3 pos)` 근처에 추가:

```csharp
//# BattleZone.OnHeroReachedCenter 핸들러 — 영웅이 zone 중심 도달 시 게임 시작.
//# 1회 동작 보장 — _spawnersActive 가 false 일 때만 진입.
private void HandleHeroReachedCenter()
{
    if (_spawnersActive) return;
    _spawnersActive = true;

    //# 영웅 AutoCombatAI 활성화 — MonsterTargetProvider 가 zone 안 Engaging 몬스터 검색.
    if (_hero != null)
    {
        foreach (AutoCombatAI ai in _hero.GetComponentsInChildren<AutoCombatAI>())
            if (ai != null) ai.enabled = true;
    }

    //# HeroEntryDriver 1회 비활성화 보강 — driver 자체도 enabled=false 호출하지만 안전망.
    if (_heroEntryDriver != null) _heroEntryDriver.enabled = false;

    //# BattleClock 시작 — 5분 카운트다운 개시.
    _clock?.Start();
}
```

- [ ] **Step 5: Update 의 Spawner.Tick 게이트 + OnDestroy 구독 해제**

기존 `Update` 안의 spawner tick 호출 조건을 다음으로 교체:

```csharp
//# 지속 스폰 — Spawner 들의 주기 타이머 틱. Pause 중 dt=0 자연 정지.
//# 전투 종료 후엔 스폰 중단. Hero 가 zone 중심 도달 전(_spawnersActive=false) 엔 무동작.
if (_spawnersActive && _model != null && _model.Result == BattleResult.None && _spawners != null)
{
    foreach (Spawner sp in _spawners)
        if (sp != null) sp.Tick(dt);
}
```

`OnDestroy` 메서드에 zone 이벤트 구독 해제 추가:

```csharp
private void OnDestroy()
{
    DespawnOnDeath.MonsterDied -= HandleMonsterDied;
    if (_zone != null) _zone.OnHeroReachedCenter -= HandleHeroReachedCenter;
    _vm?.DetachSpawners();
}
```

- [ ] **Step 6: BindSpawners 가 새 Bind 시그니처 호출**

`BindSpawners` 메서드 안의 `sp.Bind(this)` 호출을 다음으로 치환:

```csharp
foreach (Spawner sp in _spawners)
    if (sp != null) sp.Bind(this, _zone);
```

- [ ] **Step 7: 컴파일 검증**

`editor_refresh_assets` → `editor_recompile` → `editor_wait_ready` → `editor_read_log` (types=Error).
컴파일 에러 0건 확인.

- [ ] **Step 8: EditMode 전체 재실행 — 회귀 0건**

`Lair/Tests/Run EditMode Tests`. 모든 테스트 `pass`. `BattleControllerCardScopeTests` 등 기존 컨트롤러 테스트가 새 시그니처와 호환되는지 확인. (해당 테스트는 ApplyCardEffect 만 검증해 Bind 변경엔 무관.)

---

## Task 9: 씬 와이어링 — Battle.unity 에 BattleZone GameObject 추가

> 이 단계는 씬 파일(.unity YAML) 편집과 UnityMCP 의 한계가 있어 **수동 작업** 중심. gameplay-programmer 가 UnityMCP 로 일부 자동화 가능한 부분만 수행하고, 나머지는 사용자 수동 셋업.

**Files:**
- Modify: `Assets/_Lair/Scenes/Battle.unity` (수동)

- [ ] **Step 1: BattleZone GameObject 생성 (UnityMCP)**

zone 크기는 game-designer 기획서 §2.3 기준 (24, 1, 24) — 카메라 frustum 정밀 산출.

```
mcp__UnityMCP__editor_open_scene → Assets/_Lair/Scenes/Battle.unity
mcp__UnityMCP__game_object_create
  name: "BattleZone"
  position: (0, 0, 0)
mcp__UnityMCP__component_add
  game_object: "BattleZone"
  component: BoxCollider
mcp__UnityMCP__component_set
  game_object: "BattleZone"
  component: BoxCollider
  field: isTrigger
  value: true
mcp__UnityMCP__component_set
  game_object: "BattleZone"
  component: BoxCollider
  field: size
  value: (24, 1, 24)
mcp__UnityMCP__component_add
  game_object: "BattleZone"
  component: BattleZone   (Lair.Battle 네임스페이스)
```

- [ ] **Step 2: Spawn point 자식 GameObject 12개 생성 (4 edge × 3)**

각 edge 에 3개씩 spawn point Transform 자식 생성. zone size (24, 1, 24) 기준 — bounds X·Z 방향 ±12.
6 몬스터 종 중 최고 moveSpeed (Phantom 2.4) 기준 1초 거리 = 2.4m → spawn 위치는 zone 가장자리 + 2.4m (game-designer §3.2).

```
SpawnPoint_N1: (-6, 0,  14.4)
SpawnPoint_N2: ( 0, 0,  14.4)
SpawnPoint_N3: ( 6, 0,  14.4)
SpawnPoint_S1: (-6, 0, -14.4)
SpawnPoint_S2: ( 0, 0, -14.4)
SpawnPoint_S3: ( 6, 0, -14.4)
SpawnPoint_E1: ( 14.4, 0, -6)
SpawnPoint_E2: ( 14.4, 0,  0)
SpawnPoint_E3: ( 14.4, 0,  6)
SpawnPoint_W1: (-14.4, 0, -6)
SpawnPoint_W2: (-14.4, 0,  0)
SpawnPoint_W3: (-14.4, 0,  6)
```

UnityMCP `game_object_create` 로 각각 생성 후 `transform_set_parent` 로 BattleZone 자식화, `transform_set_position` 로 위치 설정.

- [ ] **Step 3: HeroEntryPoint 자식 GameObject 1개 생성**

```
HeroEntryPoint: (-15, 0, 0)     // 서쪽 edge 밖, 영웅 moveSpeed 3 × 5초 = 15m 행진 (game-designer §5.1)
```

zone 서쪽 가장자리(X=-12) + entry 거리 3m → X=-15. 5초 행진으로 첫 액티브 카드(30초)까지 페이싱 충돌 없음.

- [ ] **Step 4: BattleZone 인스펙터 필드 와이어링 (수동)**

UnityMCP `component_set` 으로 씬 내 Transform/Component 참조 와이어링 가능 (SO 에셋 와이어링은 한계 — memory reference_unity_verification.md). 다음 필드 설정:

```
_zoneTrigger     ← BattleZone 자기 BoxCollider
_spawnPoints     ← 12개 SpawnPoint_* Transform 배열
_heroEntryPoint  ← HeroEntryPoint Transform
```

UnityMCP `component_set` 으로 와이어링이 어려우면 사용자에게 인스펙터 직접 설정 요청 안내문 출력.

- [ ] **Step 4-1: ~~Knight 프리팹 SimpleMover._clampZone 씬 와이어링~~ (폐기)**

> **폐기 사유** (game-designer §12.B): 영웅은 `CHMPool.Instance.Pop` 으로 풀 스폰이라 씬 인스펙터 와이어링이 작동 불가 (reference broken). 대신 **Task 8 Step 2 에서 `BattleController.SpawnHero` 가 Pop 직후 `heroMover.BindClampZone(_zone)` 호출로 런타임 주입**. 본 step 은 수동 작업 없이 건너뜀.

- [ ] **Step 5: BattleController._zone 와이어링 (수동)**

```
BattleController._zone   ← BattleZone GameObject
```

UnityMCP `component_set` 이 가능하면 자동 와이어링, 안 되면 사용자에게 수동 설정 안내.

- [ ] **Step 6: 씬 저장**

```
mcp__UnityMCP__editor_save_scene
```

- [ ] **Step 7: PlayMode smoke test (수동 또는 PlayMode 테스트)**

UnityMCP `sim_play` 는 헤드리스에서 frame 진행 안 됨 (메모리 참고). PlayMode 테스트 러너로 통합 검증은 test-engineer 단계.
대신 사용자에게 다음 수동 검증 안내:
1. Battle 씬 Play 진입
2. 영웅이 HeroEntryPoint (-15, 0, 0) 에 스폰
3. 영웅이 zone 중심 (0, 0, 0) 으로 자동 행진
4. 도달 즉시 BattleClock 카운트다운 시작 + Spawner 들이 12개 spawn point 중 랜덤 픽으로 몬스터 스폰
5. 몬스터가 zone 진입 시점부터 영웅이 추격·공격 시작

---

## 작업 완료 후 — gameplay-programmer 셀프 체크

- [ ] **컴파일 에러 0건** — `editor_recompile` 후 `editor_read_log` Error 없음.
- [ ] **EditMode 테스트 전 케이스 pass** — 본 plan 의 신규 테스트 + 기존 회귀 0건.
- [ ] **Rule 02 §1~4** — `//#` 주석, 가드 절 중괄호 X, `var` 0건, `!` 부정 연산자 0건 (`Where == false`/`Where == null` 만).
- [ ] **Rule 03** — CHMResource/CHMPool/CHMUI 직접 사용 (Object.Instantiate 0건, Resources.Load 0건).
- [ ] **인터페이스 의존** — HeroEntryDriver 가 IMover/IRotator/IHealth 만 참조 (SimpleMover/Health 직접 참조 X).
- [ ] **BattleZone Awake** — GetComponent 폴백 (벽 생성 안 함, B1 정정).
- [ ] **CharacterRegistry 변경 후방 호환** — TryFindNearestHero 동작 무변경 (requireEngaging:false).
- [ ] **BattleController 폴백** — _zone == null 시 기존 동작(EnableHeroAIAfterDelay + 즉시 Spawner Tick + 즉시 Clock Start) 보존.

---

## Self-Review

### Spec 커버리지 점검

| Spec § | 항목 | 커버 Task |
|---|---|---|
| §3.1 | BattleZone GameObject 구조 | Task 3, 9 |
| §3.2 | 공개 API (Center, HeroEntryPoint, OnHeroReachedCenter, IsInside, GetRandomSpawn, NotifyHeroReachedCenter) | Task 3 |
| §3.3 | 영웅 zone-clamp (B1 정정 후) | Task 4 (SimpleMover._clampZone) |
| §3.4 | OnDrawGizmos | **미커버** — MVP 디버그 편의, 생략 (사용자 요구 시 후속) |
| §4.1 | Marching/Engaging 상태 정의 | Task 1 |
| §4.2 | 전환 트리거 (OnTriggerEnter + MonsterTag) | Task 5 |
| §4.3 | 풀 재사용 시 상태 리셋 (MonsterTag.OnEnable) | Task 2 |
| §5.1 | 영웅 entry 시퀀스 4 단계 | Task 7, 8 |
| §5.2 | HeroEntryDriver 컴포넌트 | Task 7 |
| §6.1 | Spawner.Bind(host, zone) + GetRandomSpawn 픽 | Task 6 |
| §6.2 | Spawner Tick 게이트 | Task 8 |
| §7.1 | CharacterRegistry IsEngaging + 필터 | Task 1 |
| §7.2 | HeroTargetProvider 무변경 | (변경 없음 — 자동 만족) |
| §7.3 | MonsterTargetProvider 무변경 | (변경 없음 — 자동 만족) |
| §8 | 파일 변경 매핑 | Task 1~9 전체 |
| §10 | 수치 (도달 임계 0.5, zone size 24, spawn 거리 2.4) | Task 7 (임계), Task 9 (씬 좌표) |
| §11 | 엣지 케이스 | 1, 2, 3 은 코드 분기로 커버. 4·5·6 은 PlayMode 테스트 (test-engineer 단계) |
| §12 | 테스트 시나리오 | EditMode 시드 Task 1·2·3·4·5·6, PlayMode 본격은 test-engineer |

§3.4 (Gizmo) 미커버 — MVP 비주얼 보조라 후속으로 미루기로. spec 변경 불필요 (§13 후속 작업으로 자연 이전).

### Placeholder 스캔

- "TODO" / "TBD" — 0건
- "implement later" — 0건
- "Similar to Task N" — 0건 (Step 5 의 코드 보정 안내가 있지만 실제 코드 블록은 풀로 제공됨)

### Type 일관성 점검

- `BattleZone.Center` (Vector3 property) — Task 3 정의, Task 7 사용 ✓
- `BattleZone.HeroEntryPoint` (Transform property) — Task 3 정의, Task 8 사용 ✓
- `BattleZone.OnHeroReachedCenter` (event Action) — Task 3 정의, Task 8 구독 ✓
- `BattleZone.NotifyHeroReachedCenter()` (public) — Task 3 정의, Task 7 호출 ✓
- `BattleZone.GetRandomSpawn()` (returns Transform) — Task 3 정의, Task 6 사용 ✓
- `BattleZone.IsInside(Vector3)` — Task 3 정의, 외부 사용처 없음 (API 노출용) ✓
- `BattleZone.ClampInside(Vector3)` — Task 3 정의, Task 4 (SimpleMover) 사용 ✓
- `SimpleMover._clampZone` (BattleZone) — Task 4 정의, Task 9 씬 와이어링 ✓
- `CharacterRegistry.SetMonsterEngaging(Transform, bool)` — Task 1 정의, Task 2·5 사용 ✓
- `Spawner.Bind(ISpawnerHost, BattleZone)` — Task 6 정의, Task 8 사용 ✓
- `HeroEntryDriver.Bind(BattleZone)` — Task 7 정의, Task 8 사용 ✓
