# 캐릭터 회전 — 기능 기획서

> 작성: game-designer · 2026-05-27
> 대상 버전: MVP
> 흐름: start-develop-simple (간소 버전)

---

## § 헤더

- **목표**: 영웅과 몬스터(7종)가 **이동 시 이동 방향**, **공격 시 공격 대상 방향**으로 부드럽게 Y축(yaw) 회전하도록 한다.
- **검증 가설**: 회전이 자동전투의 가독성(누가 누구를 보고 있는지 / 누가 어디로 가는지)을 직관적으로 만들고, 향후 아트 단계에서 캐릭터 정면이 의미를 가질 때 즉시 동작하는 기반이 된다.
- **현재 단계 범위 적합성**: **범위 내**. 컨셉서 §11.4 비주얼 매핑(프리미티브 도형) 유지. 회전은 메커니즘 레이어 추가일 뿐 아트 작업이 아님. 새 프리팹/머티리얼/씬 작업 없음.
- **핵심 메커니즘**: AutoCombatAI 가 매 Update 마다 캐릭터 상태(공격/이동/도주/대기/사망)를 판정 → 그에 맞는 방향 벡터를 새 `IRotator` 컴포넌트에 전달 → IRotator 가 deg/s 기반 부드러운 yaw 회전을 적용. 신규 인터페이스 1개, 신규 컴포넌트 1개, AutoCombatAI 1곳 수정.

---

## 1. 회전 트리거 / 우선순위

### 1.1 상태 기반 정책 (우선순위 X)

AutoCombatAI 의 기존 행동 로직은 이미 **상호 배타적인 상태**로 동작한다 — 사정거리 안이면 `Stop + Attack`, 밖이면 `Move`. 따라서 "이동 vs 공격" 우선순위라는 표현은 부정확하다. 회전 정책은 **상태별 정책**으로 정의한다.

| 상태 (AutoCombatAI 판정) | 회전 대상 방향 | 비고 |
|---|---|---|
| **Attacking** — 타겟 있음 + 사정거리 안 (`Stop + TryAttack`) | 타겟 위치 - 자기 위치 | 공격 대상을 정확히 바라봄. 타격 가독성 |
| **Moving** — 타겟 있음 + 사정거리 밖 (`MoveTo`) | 이동 목표 - 자기 위치 | 이동 방향. 타겟이 곧 이동 목표 = 결과적으로 타겟 방향 |
| **Fleeing** — `FleeMode == true` (공포 카드) | 도주 목표 - 자기 위치 | `transform.position + (self - target).normalized * 5f` 가 이동 목표 → 자연스럽게 적의 반대 방향을 바라봄 |
| **Idle** — `TryFindNearest` 실패 (타겟 없음) | (회전 안 함) | **마지막 yaw 유지**. 0으로 스냅하지 않음 — 시각적 점프 방지 |
| **Dead** — `IHealth.IsAlive == false` | (회전 안 함) | 마지막 yaw 유지. Pool Push 시점까지 정지된 시체 |

**상태 우선순위는 AutoCombatAI 의 기존 if/else 흐름과 정확히 동일**. 별도 우선순위 테이블 불필요.

### 1.2 방향 벡터 정의 — 0 또는 무효 시 처리

- 방향 벡터의 `magnitude < 0.001f` (자기 위치와 동일)이면 → **회전 명령 무시** (마지막 yaw 유지).
- Y 성분은 무시 — XZ 평면 벡터만으로 yaw 계산.

---

## 2. 회전 속도

### 2.1 권장값

- **부드러운 회전, 540 deg/s** (180° 를 0.33초에 회전).
- 모든 캐릭터(영웅 + 몬스터 6종) **동일값**. 종별 차등 X.
- **수치 근거**: 자동전투 가독성 기준 — 1초 미만에 반대 방향 정렬을 마쳐야 "보고 있는 적"을 플레이어가 추적 가능. 너무 빠르면 회전이 보이지 않고(즉시 스냅과 동일), 너무 느리면 공격하는데 옆을 보는 어색함이 발생. 540 deg/s 는 일반적 탑다운 자동전투 표준 범위(360~720 deg/s) 의 중앙값.
- **표기**: BalanceConfig 또는 컴포넌트 인스펙터에 노출. 임시값으로 두고 qa-simulator 시뮬 + 사용자 시각 체감으로 조정.

### 2.2 검토된 대안과 기각 사유

| 대안 | 사유 | 기각 이유 |
|---|---|---|
| 즉시 스냅 (Quaternion.LookRotation 직접 대입) | 구현 단순 | 자동전투에서 캐릭터가 끊임없이 방향 전환 → "삑삑" 깜빡이는 느낌, 가독성 저하 |
| 매우 빠름 (1080 deg/s+) | 반응성 강조 | 즉시 스냅과 시각적 차이 거의 없음. 회전 도입의 의미가 약함 |
| 매우 느림 (180 deg/s 이하) | 캐릭터 회전감 강조 | 공격 시작 시점에 옆을 보고 있는 어색함. 사정거리 안에서 멈추고 공격할 때 "타겟을 보고 친다" 느낌이 깨짐 |
| **540 deg/s (권장)** | — | 회전이 보이지만 0.5초 안에 정렬 완료 — 자동전투 페이싱과 충돌 없음 |

### 2.3 qa-simulator 검증 메트릭

회전 도입 후 다음을 본다:
- 평균 전투 시간 (4.1 절 컨셉 §8 "2~4분 사망" 페이싱) **불변** — 회전이 DPS 에 영향 주지 않아야 함
- 사용자 시각 체감 (별도 플레이테스트 — qa 가 측정 불가)

---

## 3. 회전 축 — Y (yaw) 전용

- **Y축 회전만 적용**. X·Z 는 0으로 강제 (`Quaternion.Euler(0, yaw, 0)`).
- 근거: 2.5D 탑다운 시점 → X·Z 회전은 캐릭터가 누워보이거나 카메라 각도로 떠오르는 버그를 만듦. SimpleMover 가 이미 `transform.position.y = 0` 으로 강제하는 패턴과 동일 (코드 참조 시 SimpleMover.cs:39-40).
- **X/Z 강제 적용 시점**: 매 회전 갱신마다 — 외부에서 잘못 설정된 회전이 들어와도 IRotator 가 yaw 만 살리고 X/Z 는 0으로 덮어쓴다.

---

## 4. 적용 대상 — 영웅 + 몬스터 6종 동일 정책

| 대상 | 적용 | 비고 |
|---|---|---|
| 영웅 (Knight) | ✅ | Capsule, 파랑 |
| Wisp | ✅ | Sphere |
| Wraith | ✅ | Cube |
| Reaper | ✅ | Capsule |
| Hex | ✅ | Capsule |
| Plague | ✅ | Cube |
| Phantom | ✅ | Sphere |

**모두 동일 회전 속도 / 동일 정책**. 종별 차등은 YAGNI — MVP 가설 검증과 무관하며, 종 특성은 HP/DPS/사거리/이동속도에서 이미 차별화됨.

### 4.1 프리미티브 비주얼 한계 — 명시적 인정

컨셉서 §11.4 의 프리미티브 매핑상 회전이 **시각적으로 드러나지 않는 종이 있다**:

| 메쉬 | 대상 | Y축 회전 시각화 |
|---|---|---|
| **Sphere** | Wisp, Phantom | **보이지 않음** (회전 불변) |
| **Capsule (직립)** | 영웅, Reaper, Hex | **보이지 않음** (원기둥 단면 회전 불변) |
| **Cube** | Wraith, Plague | **보임** (모서리 방향이 바뀜) |

즉 MVP 단계에서는 **7종 중 2종(Wraith/Plague)만 회전이 눈에 보인다**. 그럼에도 회전을 도입하는 이유:
- 메커니즘 레이어가 완성되어 있어야 아트 단계에서 캐릭터 정면 메시(눈/입/무기)가 들어오면 즉시 동작
- 발사체·이펙트가 도입될 때 캐릭터 transform.forward 를 발사 방향으로 쓸 수 있음
- Wraith/Plague 만으로도 자동전투 디버깅(타겟이 맞는지) 가시화 가능

§11.4 비주얼 매핑은 **변경하지 않는다** — 정면 마커 자식 추가 등은 별도 사용자 승인 사안.

---

## 5. 정지 상태 / 초기 방향

### 5.1 정지 상태 (Idle / Dead)

- `_targetProvider.TryFindNearest` 가 false 를 반환하거나 IsAlive 가 false 일 때 → IRotator 에 회전 명령을 보내지 않음 → 마지막 yaw 유지.
- "Idle 시 ring 중심을 바라봄" 같은 디폴트 정책은 도입하지 않음 — 자동전투 중 idle 은 극히 드물고(타겟 항상 존재), 강제 회전은 오히려 부자연스러움.

### 5.2 풀 재사용 시 초기 방향 (CHMPool Pop)

- IRotator 는 `OnEnable` 에서 **`SnapToDirection(initialFacing)`** 으로 즉시 방향을 스냅 (deg/s 적용 X).
- `initialFacing` 정책: **스폰 시점 자기 위치에서 ring 중심(`Vector3.zero` 또는 던전 중심)을 향하는 벡터**. Spawner 가 ring 둘레에 배치되어 있고 영웅은 중심에서 등장하므로, 스폰 직후 몬스터가 처음부터 영웅 쪽을 바라보고 출발하는 자연스러운 시작이 됨.
- 영웅은 ring 중심에서 등장 → 첫 타겟을 향하도록 SnapToDirection. 첫 타겟이 없으면 현재 yaw 유지.
- 풀 재사용 시 이전 인스턴스의 yaw 가 남아있으면 새 스폰이 엉뚱한 방향을 보고 시작하는 버그가 발생 → OnEnable 에서 반드시 스냅.

---

## 6. 구현 요청사항 (gameplay-programmer 용)

### 6.1 새 인터페이스 — `IRotator`

**위치**: `Assets/_Lair/Scripts/Character/CommonInterface.cs` (Rule 02 §9 — 단일 파일에 추가).

**카테고리 주석**: `//# ===== 회전 =====` 으로 그룹 표시.

**스케치**:
```csharp
//# ===== 회전 =====

//# Y축(yaw) 회전 추상. AutoCombatAI 가 상태별로 FaceDirection 호출.
//# 풀 재사용 시 SnapToDirection 으로 초기 방향 즉시 적용.
public interface IRotator
{
    //# deg/s. 인스펙터 또는 BalanceConfig 로 설정.
    float TurnSpeedDegPerSec { get; set; }

    //# 목표 방향 설정 — 매 Update 호출 가능. magnitude < 0.001f 면 no-op.
    //# Y 성분 무시 (XZ 평면 yaw 만 계산).
    void FaceDirection(Vector3 worldDir);

    //# 즉시 스냅 — OnEnable / 초기 스폰 시 사용. magnitude < 0.001f 면 no-op.
    void SnapToDirection(Vector3 worldDir);
}
```

**기각된 대안**:
- IMover 에 회전 통합: IMover 는 이미 `IsMoving` (B3 출혈 카드 의존)을 들고 있고, 회전은 공격 상태에서도 필요(이동이 멈춤) → 책임 영역이 다름
- IAttacker 에 회전 통합: 공격 상태가 아닌 이동/도주 중에도 회전 필요 → 부적합
- IRotator 분리 (권장): AutoCombatAI 한 곳에서 양쪽 상태를 보고 호출 → SRP 충족, 테스트 더블도 단순

### 6.2 새 컴포넌트 — `SimpleRotator`

**위치**: `Assets/_Lair/Scripts/Character/SimpleRotator.cs`

**책임**:
- IRotator 구현
- 매 Update 마다 현재 yaw 와 목표 yaw 사이를 `Mathf.MoveTowardsAngle(currentYaw, targetYaw, TurnSpeedDegPerSec * Time.deltaTime)` 로 보간
- 매 적용 시 `transform.rotation = Quaternion.Euler(0, yaw, 0)` 으로 X/Z 0 강제
- `OnEnable` 에서 내부 상태(_hasTarget, _targetYaw) 리셋 — 풀 재사용 시 이전 목표 잔존 방지

**참고 (외부 사용)**: Rule 02 §7 — 외부에서 회전을 다룰 필요가 생기면 `GetComponentInParent<IRotator>()` 또는 `GetComponent<IRotator>()` 로 접근. 구체 클래스 `SimpleRotator` 직접 참조 금지.

### 6.3 AutoCombatAI 수정

**위치**: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`

**변경 사항**:
- `[RequireComponent(typeof(SimpleRotator))]` 추가
- 필드에 `private IRotator _rotator;` 추가, `Awake` 에서 `GetComponent<IRotator>()`
- `Update` 의 각 분기에서 회전 명령 추가:
  - **사망/타겟 없음** → 회전 명령 없음 (마지막 yaw 유지)
  - **FleeMode** → `_rotator.FaceDirection(away - transform.position)` (away 는 기존 계산 결과)
  - **사정거리 안** → `_rotator.FaceDirection(t.position - transform.position)`
  - **사정거리 밖** → `_rotator.FaceDirection(t.position - transform.position)` (이동 목표 = 타겟 위치)
- `OnEnable` 에서 `_rotator?.SnapToDirection(Vector3.zero - transform.position)` 호출 — ring 중심을 바라봄. 단 자기 위치가 `Vector3.zero` 와 거의 같으면(영웅) 명령 무효(no-op)이므로 안전.

### 6.4 Enum / 에셋 키

- **신규 Enum 값 없음** — IRotator 는 컴포넌트 인터페이스이지 에셋 키가 아님.
- **신규 프리팹 / SO 없음** — 기존 캐릭터 프리팹(영웅 1, 몬스터 6)에 `SimpleRotator` 컴포넌트만 추가하면 됨.

### 6.5 SO 스키마

- 회전 속도(TurnSpeedDegPerSec)는 일단 `SimpleRotator` 컴포넌트의 SerializeField 로 노출 (기본값 540).
- 향후 BalanceConfig 에 `[Header("Rotation")] public float DefaultTurnSpeedDegPerSec = 540f;` 같은 필드 추가는 **이번 작업 범위 외** — 모든 캐릭터가 동일값을 쓰는데 굳이 중앙 SO 로 빼지 않는다 (YAGNI). 종별 차등 도입이 필요해질 때 BalanceConfig 또는 캐릭터별 SO 로 승격.

### 6.6 프리팹 갱신 작업

기존 캐릭터 프리팹 7종 모두에 `SimpleRotator` 컴포넌트를 추가해야 함 — Editor 빌더(`LairCharacterPrefabBuilder` 등이 있다면)에 추가, 없다면 수동 추가 후 프리팹 저장. 작업은 gameplay-programmer 가 판단해 진행.

---

## 7. 테스트 케이스 권고 (test-engineer 용)

`test_method_naming: korean` 기준 (project.md). EditMode 우선 — Time.deltaTime 의존이 있으나 IRotator 추상 + 직접 yaw 검증으로 PlayMode 의존 회피 가능.

| # | 테스트명 (한글) | 시나리오 |
|---|---|---|
| 1 | `공격_중_타겟_방향으로_회전한다` | AutoCombatAI 가 사정거리 안 타겟을 갖는 상황. 충분한 시간 진행 후 transform.forward 가 타겟 방향과 정렬되는지 검증 |
| 2 | `이동_중_이동_방향으로_회전한다` | 사정거리 밖 타겟. 시간 진행 후 transform.forward 가 타겟(=이동 목표) 방향과 정렬 |
| 3 | `타겟_없을_때_마지막_방향을_유지한다` | 타겟이 있다가 사라진 상황. 회전 명령이 안 와도 transform.rotation 이 그대로 유지 |
| 4 | `풀_재사용_시_초기_방향으로_스냅된다` | SetActive(false) → 이전 yaw 90° 상태 → SetActive(true) → OnEnable 직후 `SnapToDirection(ring 중심)` 이 즉시 적용되어 yaw 가 그 방향으로 점프 |
| 5 | `사망_후_회전하지_않는다` | IsAlive false 후 타겟 위치를 변경해도 transform.rotation 이 사망 시점 yaw 그대로 |

**보충 케이스 (선택)**:
- `회전_시_X축과_Z축은_0을_유지한다` — IRotator 가 매번 X/Z = 0 강제 적용 검증
- `목표_방향이_0벡터일_때_회전하지_않는다` — `FaceDirection(Vector3.zero)` 호출 시 no-op
- `도주_모드에서_적의_반대_방향을_본다` — FleeMode = true 일 때 transform.forward · (target.position - self).normalized < 0

---

## 8. 미정 사항 (사용자 결정 또는 후속 검증)

- **회전 속도 540 deg/s** — qa-simulator 시뮬 후 시각 체감 기반 조정. 메트릭: 평균 전투 시간 불변 + 사용자 시각 체감
- **BalanceConfig 승격 여부** — 종별 차등 도입 시점에 재검토 (현재 작업에서는 컴포넌트 SerializeField 로 충분)
- **회전이 보이는 종이 2종(Wraith/Plague)뿐인 한계** — 컨셉서 §11.4 정면 마커 자식 추가는 사용자 승인 필요. **이번 기획서에는 포함하지 않음**

---

## 변경 이력

- **v0 (2026-05-27)**: 초안. 상태별 회전 정책 / 540 deg/s / IRotator + SimpleRotator / 영웅 + 몬스터 6종 동일 정책 / 프리미티브 비주얼 한계 명시.
