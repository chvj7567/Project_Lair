# Spec — BattleZone 시스템 + 영웅 entry/몬스터 march

**날짜**: 2026-05-29
**상태**: 승인됨

---

## 1. 목적 / 동기

### 1.1 해결할 문제
- **카메라 줌아웃 시 캐릭터 가시성**: 줌아웃 한계가 모호해 캐릭터가 너무 작아짐. 카메라 시야와 전장 크기의 기준이 단일 진실로 묶여 있지 않음.
- **스폰 연출 불일치**: 던전 주인(플레이어) 컨셉 상 카메라 = 내 영역이어야 하는데, 스폰이 카메라 안에서 일어나면 "쟤 저기서 나와서 여기까지 와" 라는 좁음이 드러남.
- **영웅이 화면 밖으로 끌려나가는 리스크**: 영웅 AI 가 nearest-target 만 보고 따라가면 zone 가장자리 몬스터를 쫓아 카메라 밖으로 나갈 수 있음.
- **전투 시작 신호 모호함**: 현재는 `BattleClock.Start()` 가 씬 진입 즉시 — 영웅이 등장하기도 전에 5분 타이머가 흐름.

### 1.2 컨셉 일치
- 던전 주인의 시점 = 화면 = 내 영역. 영웅은 침입자.
- 5분 압축 세션 = 한 화면에 다 보이는 좁은 무대 (고정 카메라 페이싱).
- 자동전투 + 카드 픽 핵심 — 플레이어 시선이 화면 안 라인업·카드 픽 UI 에 모여야 함.

---

## 2. 범위

### 2.1 포함
- `BattleZone` 컴포넌트 신설 — 단일 씬 인스턴스, 전장 경계·스폰 지점·영웅 진입점의 단일 진실.
- 몬스터 상태 `Marching` / `Engaging` 도입 + zone 진입 시 자동 전환.
- 영웅 AI 타겟 필터에 `IsEngaging=true` 조건 추가.
- 영웅 entry 시퀀스: 영웅이 zone 밖 진입점에서 zone 중심까지 행진 → 도달 시 BattleClock + Spawner Tick 시작.
- Spawner 의 spawn 위치를 BattleZone 의 spawn point pool 에서 픽.
- 영웅이 zone 밖으로 못 나가게 막는 수단 — `SimpleMover._clampZone` 옵션 (§3.3, design-reviewer B1 정정). 인비저블 벽 자동 생성은 채택 안 함.

### 2.2 제외 (이번 작업 범위 아님)
- AI leash (advance lane 수직 거리 필터) — 부작용 발견 시 후속 작업.
- 다중 BattleZone / prefab variant 다중 스테이지 — MVP 단일 인스턴스만.
- Marching 상태 전용 VFX·SFX — MVP §8 비주얼/사운드 비작업.
- 카메라 줌 한계 자동 산출 (frustum projection) — 수동 인스펙터 설정 유지.
- BattleZone 의 시각 동기화 (예: zone 경계에 outline 표시) — 디버그용 Gizmo 만.

### 2.3 MVP 단계 제약 (Project Lair §8)
- 비주얼은 프리미티브 도형 + 색상 유지. Marching/Engaging 상태 시각 구분 없음.
- 사운드 hook 미등록.
- 메타 진행·메인 메뉴 작업 없음.

---

## 3. 아키텍처

### 3.1 BattleZone 컴포넌트

씬에 단일 인스턴스로 사전 배치. GameObject 1개 + 자식 BoxCollider(isTrigger) 1개 + 자식 Transform 들 (spawn points, hero entry).

```
BattleZone (GameObject, scene-placed singleton)
  ├ Component: BoxCollider (isTrigger=true, BattleZone 본체에 직접 부착 — OnTriggerEnter 직수신)
  ├ Component: BattleZone (MonoBehaviour)
  ├ Children: SpawnPoint_* (Transform × 12~20, 4 edge 분산)
  └ Child: HeroEntryPoint (Transform, zone 밖 한 위치)
```

> plan §"사전 의사결정" 정정 — BoxCollider 를 BattleZone GameObject 본체에 직접 부착 (자식 X). 인비저블 벽 자동 생성 채택 안 함 (§3.3 결정 변경).

### 3.2 공개 API

```csharp
public class BattleZone : MonoBehaviour
{
    [SerializeField] private BoxCollider _zoneTrigger;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _heroEntryPoint;

    public Vector3 Center { get; }                    //# _zoneTrigger.bounds.center
    public Transform HeroEntryPoint { get; }
    public event Action OnHeroReachedCenter;          //# 영웅 entry march 완료 신호

    public bool IsInside(Vector3 worldPos);           //# _zoneTrigger.bounds.Contains
    public Vector3 ClampInside(Vector3 worldPos);     //# zone 밖이면 bounds 안쪽 가장자리로 클램프
    public Transform GetRandomSpawn();                //# _spawnPoints 에서 랜덤 픽
    public void NotifyHeroReachedCenter();            //# 영웅 entry 로직이 호출, 이벤트 발행
}
```

`SimpleMover` 변경:
```csharp
public class SimpleMover : MonoBehaviour, IMover
{
    [SerializeField] private BattleZone _clampZone;   //# null = clamp 비활성 (몬스터)
    //# FixedUpdate 의 next 계산 후, _clampZone 비-null 이면 _clampZone.ClampInside(next) 적용
}
```

### 3.3 영웅 zone-clamp (인비저블 벽 대신)

**결정 변경** (design-reviewer BLOCKER B1 정정): 인비저블 벽 자동 생성은 채택하지 않는다. 사유 — 몬스터 6종 프리팹이 이미 Dynamic Rigidbody 부착이라, BattleZone 본체 Kinematic Compound Collider 가 몬스터의 zone 진입을 물리적으로 차단해 OnTriggerEnter 자체가 발화 안 됨 (Marching→Engaging 전환 불가 → 전 시스템 마비).

대안 — 영웅의 `SimpleMover` 에 zone-clamp 옵션을 추가:
- `SimpleMover` 에 `[SerializeField] private BattleZone _clampZone` 필드 신설. null 이면 clamp 무동작 (몬스터 프리팹에서는 미할당 → 영웅 한정).
- `MoveTo` 또는 `FixedUpdate` 의 최종 좌표가 `_clampZone.IsInside` 를 벗어나면 zone bounds 안쪽 가장자리로 클램프.
- 몬스터는 별도 clamp 불필요 — zone 진입 후 영웅을 추적하므로 자연히 zone 안에 머무름.

부수 효과 — `_wallThickness`, `_wallHeight` 필드 제거, Awake 에서 벽 생성 로직 제거. `BattleZone` 컴포넌트가 단순해진다.

### 3.4 디버그 표시

`OnDrawGizmos` 로 zone 경계(녹색), spawn points (노란 점), hero entry (빨간 점) 표시. 에디터 작업 편의용.

---

## 4. 몬스터 상태머신

### 4.1 상태 정의

`CharacterRegistry.MonsterEntry` 에 `bool IsEngaging` 필드 추가 (default `false`).

- **Marching** (`IsEngaging == false`): 스폰 직후 ~ zone 진입 전. 자기 AutoCombatAI 는 정상 동작(영웅 향해 이동). 단, 영웅 AI 의 `TryFindNearestMonster` 후보에서 제외. AoE/아우라/도트(독·출혈) 데미지는 정상 수용.
- **Engaging** (`IsEngaging == true`): zone 진입 후. 영웅 AI 타겟 후보로 포함.

### 4.2 전환 트리거

`BattleZone._zoneTrigger.OnTriggerEnter(Collider other)`:
- `other` 의 컴포넌트에 `MonsterTag` 가 있으면 `CharacterRegistry.SetMonsterEngaging(other.transform, true)` 호출.
- 영웅은 별도 분기 — `MonsterTag` 없음이라 자동 무시.

역방향 전환(zone 밖으로 다시 나감) 은 다루지 않는다 — 영웅은 `SimpleMover._clampZone` 으로 zone 안에 잡혀 있고, 몬스터는 사망 시 풀로 회수되므로 zone 재진입 시 풀 재사용 = `OnEnable` 에서 `IsEngaging = false` 로 초기화.

### 4.3 풀 재사용 시 상태 리셋

`MonsterTag.OnEnable` 에서 `CharacterRegistry.SetMonsterEngaging(transform, false)` 호출. 풀 Pop 직후 = Marching 상태 보장. 별도 컴포넌트 분리 안 함 (MonsterTag 가 이미 모든 몬스터 프리팹에 부착돼 있어 추가 와이어링 불필요).

---

## 5. 영웅 entry 시퀀스

### 5.1 단계

1. **BattleController.SpawnHero**: 영웅을 `BattleZone.HeroEntryPoint.position` 에 스폰 (현재 `_heroSpawn` 대신).
2. **영웅 march 모드 활성화**: 영웅의 `AutoCombatAI.enabled = false`, 신규 `HeroEntryDriver.enabled = true`. HeroEntryDriver 가 `IMover.MoveTo(BattleZone.Center)` 로 zone 중심을 향해 이동시킴.
3. **Center 도달 판정**: `HeroEntryDriver.Update` 에서 영웅과 zone center 거리가 임계값(예: 0.5m) 이하면 도달. `BattleZone.NotifyHeroReachedCenter()` 호출 후 자기 비활성화.
4. **BattleController 가 이벤트 구독**: `OnHeroReachedCenter` 핸들러에서:
   - `BattleClock.Start()`
   - Spawner Tick 게이트 플래그 활성화
   - `HeroEntryDriver` 비활성화 (이미 자기 비활성화했음 — fallback)
   - 영웅 AI 가 자동으로 정상 nearest-target 로직 진입

### 5.2 영웅 AI 동작 분기

영웅에 부착된 `AutoCombatAI` 는 변경하지 않는다. 대신 새 컴포넌트:

```csharp
public class HeroEntryDriver : MonoBehaviour
{
    private BattleZone _zone;
    private IMover _mover;
    private IRotator _rotator;
    public void Bind(BattleZone zone) { ... }
    private void Update() { /* Center 까지 이동, 도달 시 zone.NotifyHeroReachedCenter() */ }
}
```

`HeroEntryDriver` 활성 동안 영웅의 `AutoCombatAI.enabled = false` (현재 `EnableHeroAIAfterDelay(3f)` 대체).

도달 후 `BattleController` 가 `AutoCombatAI.enabled = true` 로 전환.

---

## 6. Spawner 통합

### 6.1 spawn 위치 결정

기존 `Spawner._spawnPoint` 가 `null` 이면 `transform.position` fallback 동작 유지. 변경점은 호스트(BattleController) 가 Bind 시 `Spawner` 에 `BattleZone` 참조를 주입하고, `Spawner.Tick` 마다 `BattleZone.GetRandomSpawn()` 로 위치 갱신:

```csharp
//# Spawner.cs 변경
public void Bind(ISpawnerHost host, BattleZone zone)
{
    _host = host;
    _zone = zone;
}

//# Tick 안에서 spawn 직전:
Vector3 spawnPos = _zone != null
    ? _zone.GetRandomSpawn().position
    : (_spawnPoint != null ? _spawnPoint.position : transform.position);
```

기존 인스펙터 `_spawnPoint` 는 fallback 으로 유지 — `_zone` 미할당 시 후방 호환.

### 6.2 Spawner Tick 게이트

`BattleController.Update` 안에서 spawner tick 호출 조건에 게이트 플래그 추가:

```csharp
private bool _spawnersActive;   //# OnHeroReachedCenter 에서 true

private void Update()
{
    float dt = Time.deltaTime;
    _clock?.Tick(dt);    //# clock 도 hero entry 후만 동작 — Start() 호출 안 되어 있으면 no-op
    ...
    if (_spawnersActive && _model?.Result == BattleResult.None && _spawners != null)
        foreach (Spawner sp in _spawners)
            sp?.Tick(dt);
}
```

`BattleClock.Start()` 는 entry 완료 시점에 호출 — clock 자체는 호출 전엔 Tick 무동작이라 추가 게이트 불필요.

---

## 7. 영웅 AI 타겟 필터 변경

### 7.1 CharacterRegistry 변경

```csharp
public class Entry { public Transform Transform; public IHealth Health; }
public class MonsterEntry : Entry { public bool IsEngaging; }   //# 신설 or Entry 에 필드 추가

public static void SetMonsterEngaging(Transform monster, bool engaging);
public static bool TryFindNearestMonster(Vector3 from, out Transform t, out IHealth h);
//# ↑ IsEngaging == true 만 검색
```

가장 단순한 구현: 기존 `Entry` 에 `IsEngaging` 필드 추가, 영웅 Entry 는 무시. 별도 클래스 분리는 후속.

### 7.2 HeroTargetProvider 변경

변경 없음 — `CharacterRegistry.TryFindNearestMonster` 자체가 필터링하므로 호출지는 그대로.

### 7.3 MonsterTargetProvider 영향

몬스터의 영웅 타겟팅은 변경 없음. 몬스터는 Marching 상태에서도 자기 AutoCombatAI 로 영웅을 추적해야 함 (= zone 안으로 들어옴). `TryFindNearestHero` 는 그대로.

---

## 8. 파일 변경 윤곽

| 변경 종류 | 파일 | 설명 |
|---|---|---|
| 신규 | `Assets/_Lair/Scripts/Battle/BattleZone.cs` | 본 시스템 컴포넌트 |
| 신규 | `Assets/_Lair/Scripts/Character/HeroEntryDriver.cs` | 영웅 zone 진입 단계 드라이버 |
| 수정 | `Assets/_Lair/Scripts/Battle/BattleController.cs` | SpawnHero · EnableHeroAI · BattleClock 시작 시점 · Spawner Tick 게이트 |
| 수정 | `Assets/_Lair/Scripts/Battle/Spawner.cs` | Bind 시 `BattleZone` 주입 + Tick 시 GetRandomSpawn 픽 |
| 수정 | `Assets/_Lair/Scripts/Character/CharacterRegistry.cs` | Entry 에 `IsEngaging` 필드 + 필터링 TryFindNearestMonster |
| 수정 | `Assets/_Lair/Scripts/Character/MonsterTag.cs` | `OnEnable` 에서 IsEngaging 초기화 |
| 수정 | `Assets/_Lair/Scripts/Character/SimpleMover.cs` | `_clampZone` 필드 + FixedUpdate 의 next 클램프 (영웅 zone 차단) |

씬 변경: `Assets/_Lair/Scenes/Battle.unity` 에 BattleZone GameObject 배치 (BoxCollider, 자식 Transform 들 포함). 기존 `BattleController._heroSpawn` 필드는 사용 안 함 (제거 또는 deprecated 코멘트).

---

## 9. 인터페이스 의존성

- BattleZone — 다른 시스템에 의존하지 않음 (씬 단일 인스턴스).
- BattleController — `BattleZone` 직접 참조 (`[SerializeField] private BattleZone _zone`).
- Spawner — `BattleZone` 직접 참조 (BattleController 가 Bind 시 주입).
- HeroEntryDriver — `BattleZone` 직접 참조 (BattleController 가 Bind 시 주입).
- SimpleMover — `BattleZone` 직접 참조 (영웅 프리팹에서 인스펙터 와이어링, 몬스터 미할당).
- CharacterRegistry — `BattleZone` 의존 없음 (Marching/Engaging 은 외부에서 set).
- AutoCombatAI — 변경 없음 (HeroEntryDriver 가 enabled 토글로 제어).

순환 의존 없음. BattleZone 은 leaf 컴포넌트.

---

## 10. 데이터 / 수치

| 항목 | 값 | 비고 |
|---|---|---|
| Zone trigger 크기 | 인스펙터 수동 — BoxCollider size | 카메라 가시영역의 ~80~90% (수동 조정) |
| Spawn point 개수 | edge 당 3~5개 (총 12~20) | 4 edge 모두에 분산 |
| Spawn point 위치 | zone 밖, 거리 = `moveSpeed × 1.0초` | 디자이너 수동 배치 (자동 산출 X) |
| 영웅 zone-clamp | SimpleMover._clampZone 인스펙터 와이어링 | 영웅 프리팹에만 할당, 몬스터는 미할당 |
| Hero entry 도달 임계값 | 0.5m | `HeroEntryDriver` 내부 상수 |
| Marching 동안 영웅 entry 차단 | 자동 | spawner tick 게이트로 보장 (몬스터가 영웅 march 동안 0마리) |

---

## 11. 에러·엣지 케이스

1. **BattleZone 미할당** — `BattleController._zone == null` 이면 Start 시 LogError + 기존 `_heroSpawn` 폴백 동작 (안전 fallback, MVP 외 회피용).
2. **spawnPoints 0개** — `GetRandomSpawn` 이 null 반환 → Spawner 가 fallback (`_spawnPoint` 또는 `transform.position`) 사용.
3. **풀 재사용 누락** — `MonsterTag.OnEnable` 에서 `IsEngaging=false` 초기화 보장. 누락 시 풀 재사용 몬스터가 Engaging 상태로 등장하는 버그 가능.
4. **영웅이 entry 중 사망** — `HeroEntryDriver` 가 `Health.IsAlive == false` 체크해 이동 중단. (현실적으로 발생 안 함 — 몬스터 0마리)
5. **영웅이 zone 밖으로 이탈 시도** — `SimpleMover._clampZone` 가 매 FixedUpdate 의 next 좌표를 `ClampInside` 로 zone bounds 안쪽 가장자리로 잡아냄. 영웅 한정 적용.
6. **몬스터가 zone 밖으로 이탈** — 몬스터 SimpleMover 는 `_clampZone` 미할당 (null) 이라 clamp 무동작. zone 진입 후엔 영웅 추적이라 자연히 zone 안 머무름. 예외적 이탈은 인비저블 벽 부재로 막을 수 없음 — MVP 에선 허용 (드물고, 비치명적).

---

## 12. 테스트 시나리오 (test-engineer 단계 입력)

### 12.1 EditMode
- `BattleZone.IsInside(pos)` — bounds.Contains 정합성
- `BattleZone.ClampInside(pos)` — zone 안/밖 좌표 클램프 정합성
- `BattleZone.GetRandomSpawn()` — null 처리, 분산
- `SimpleMover._clampZone` 동작 — null=자유, 비-null=zone 경계 클램프
- `CharacterRegistry.TryFindNearestMonster` — IsEngaging=false 몬스터 제외 검증

### 12.2 PlayMode (헤드리스)
- 영웅 spawn → entry march → Center 도달 → OnHeroReachedCenter 1회 발행
- 영웅 entry 동안 BattleClock 정지 (Tick 호출돼도 카운트 안 흐름)
- 영웅 entry 동안 Spawner Tick 호출 안 됨 (모니터링)
- 몬스터 spawn → zone trigger 진입 → IsEngaging=true 자동 전환
- 영웅 AI 가 Marching 몬스터를 타겟하지 않음 (인접해도 무시)
- 풀 재사용 시 IsEngaging=false 초기화

---

## 13. 후속 작업 (out of scope)

1. **AI leash (advance lane 수직 거리 필터)** — Marching 만으로 zigzag 가 거슬리면 추가.
2. **Marching 상태 시각 구분 VFX** — 비주얼 단계 들어가면.
3. **다중 BattleZone / prefab variant** — 스테이지 차별화 필요 시.
4. **카메라 줌 한계 자동 산출** — `BattleCamera._maxZoomDistance` 를 `BattleZone` 크기로부터 역산.
5. **Zone 경계 outline 시각 표시** — 디버그 Gizmo 외 인게임 표시 필요 시.

---

## 결정 락

본 spec 의 모든 결정은 사용자와 대화 5라운드에 걸쳐 결정됨:
- 고정 카메라 vs 추적 카메라 → 고정
- 스폰 가시 vs 비가시 → 비가시 (카메라 밖)
- AI 제한 방식 3중 방어 중 #1+#3 채택, #2 (leash) 생략
- Zone 경계: BoxCollider isTrigger (수동 BoxCollider 핸들로 조절)
- March 경로: 영웅 향해 (별도 path 정의 없음)
- Spawn 거리: moveSpeed × 1.0초
- 영웅 entry: zone 밖 한 위치 → Center 행진 → 도달 시 게임 시작
- 몬스터 spawn 방향: 4 edge 전체
- BattleClock 시작 시점: 영웅이 Center 도달 시점
- Marching 중 데미지 수용: 정상 (AoE/도트는 적용, 영웅 AI 타겟만 제외)
- 영웅 zone 차단: SimpleMover._clampZone 옵션 (인비저블 벽 자동 생성은 design-reviewer B1 후 폐기 — 몬스터 Dynamic RB 와 충돌 매트릭스 충돌로 zone 진입 차단 위험)
