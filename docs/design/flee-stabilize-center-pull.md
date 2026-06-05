# 도주 떨림 안정화 + 영웅 중앙 끌림 — 기획서

- 작성일: 2026-06-05
- 흐름: start-develop-simple (프로토타입 간소 — design-reviewer / qa-simulator 생략, test-engineer 유지)
- 입력 문서: spec `docs/superpowers/specs/2026-06-05-flee-stabilize-center-pull-design.md` · plan `docs/superpowers/plans/2026-06-05-flee-stabilize-center-pull.md`
- 대상: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` (이동 결정부) · `Knight.prefab`

---

## § 헤더

- **목표**: 공포(Fear) 도주 시 영웅 제자리 떨림을 제거하고, 영웅을 3초마다 1초씩 중앙으로 잠깐 끌어 전투를 아레나 중앙 근처로 모은다.
- **검증 가설**: (A) 도주가 떨림 없이 자연스럽게 보이는가. (B) 전투가 맵 가장자리로 흘러 카메라 밖으로 빠지지 않고 중앙 근처에 머무는가 — 둘 다 "5분 자동전투가 보기 좋은가"의 가독성 가설을 보강.
- **현재 단계 범위 적합성**: 범위 내. 이동 AI 튜닝(기존 시스템 동작 보정)이며 새 콘텐츠·메타·서버·사운드 없음 (MVP §8). spec/plan 이 이미 결정 락을 잡았고, 본 기획서는 **수치만** 채운다.
- **핵심 메커니즘**: (A) 도주 방향을 프레임 독립 시간감쇠(`1 − exp(−rate·dt)`)로 수렴 + 도주 centroid 집계를 '반경 내 살아있는 몬스터 전체'로 보정. (B) 영웅 전용 플래그(`_centerPullEnabled`) 게이트로, FleeMode 가 아니고 중앙 거리 > deadzone 일 때 3초 주기마다 1초간 중앙(월드 원점 XZ) 방향 이동(이동 우선·공격 보류).

---

## 1. 참고 데이터 (수치 근거의 원천)

코드/프리팹에서 확정한 기준값. 모든 도메인 결정은 이 값들로부터 유도한다.

| 항목 | 값 | 출처 |
|---|---|---|
| 영웅 이동속도 (SimpleMover `_speed`) | **3 units/s** | `Knight.prefab` MonoBehaviour `_speed: 3` |
| 영웅 회전 (SimpleRotator `_turnSpeedDegPerSec` / `_snapInstant`) | **540 °/s, snap=1 (즉시 스냅)** | `Knight.prefab` `_turnSpeedDegPerSec: 540`, `_snapInstant: 1` |
| 영웅 공격 사거리 / 쿨다운 | Range **1.5**, cooldown **1s** | `Knight.prefab` MeleeAttacker `_range: 1.5`, `_cooldown: 1` |
| 도주 centroid 반경 (`_fleeThreatRadius`) | **4 units** | `Knight.prefab` `_fleeThreatRadius: 4` |
| 도주 목표 투영 거리 | 도주방향 × **5 units** 앞으로 `MoveTo` | `AutoCombatAI.cs:135` |
| 아레나 클램프 박스 (영웅 이동 한계) | **X 22 × Z 15** (반-extent X=11, Z=7.5), 중심 원점 | `Battle.unity` BattleZone BoxCollider `m_Size: {x:22, y:1, z:15}` |
| 몬스터 스폰 ring 반경 | **13 units** | `CircularSpawnerArranger.cs:10` `_radius = 13f` |

> **회전 스냅 주의**: SimpleRotator 가 `_snapInstant=1` 이라 facing 은 넘겨준 벡터로 **즉시** 스냅된다. 따라서 도주 방향 떨림의 유일한 완충 장치는 `_fleeTurnSmoothing`(A-2) 하나뿐이다 — 회전 쪽 이중 스무딩 없음. rate 값이 곧 도주 반응성의 단일 게이트.

---

## 2. A. 도주 떨림 안정화 — 수치 결정

### 2.1 A-1 centroid 원인 보정 (수치 변경 없음)
도주용 centroid 집계 필터를 '교전 중(IsEngaging) 몬스터만' → **'반경 4 내 살아있는 몬스터 전체'** 로 바꾼다 (spec A-1 / plan Task 1, 락된 결정). 반경값 `_fleeThreatRadius = 4` 는 **유지** — 영웅 사거리 1.5의 약 2.7배로, 도망칠 위협 무리를 충분히 넓게 보면서도 맵 반대쪽 무관 몬스터까지 끌어들이지 않는 거리. 타겟팅(`TryFindNearest`)의 `requireEngaging` 필터는 불변.

### 2.2 A-2 감쇠 rate (`_fleeTurnSmoothing`) — **권장 5.0**

감쇠는 `_fleeDirSmoothed += (raw − _fleeDirSmoothed) × (1 − exp(−rate·dt))` 형태. rate 를 시간상수 τ = 1/rate 로 환산해 두 가지를 동시에 만족시키는 값을 고른다.

- **떨림 억제 (필요조건)**: 한 프레임짜리 반대 방향 스파이크는 smoothed 방향을 프레임당 `1 − exp(−5·dt)` 만큼만 움직인다. 60fps(dt≈0.0167s)에서 **약 8%/프레임** (`1 − exp(−5×0.0167) = 0.080`) — 프레임 단위로 뒤집히는 centroid 진동은 8%씩만 반영되어 즉시 뭉개진다.
- **실제 도주 반응성 (충분조건)**: 무리가 실제로 이동해 도주 방향이 ~0.5초 지속 변하면 `1 − exp(−5×0.5) = 0.918` → **약 92% 추종**. 굼뜨지 않다. τ = 1/5 = 0.2s 로, 방향 전환의 63%를 0.2초, 95%를 0.6초 안에 따라잡는다.

**후보 비교**:

| rate | τ (=1/rate) | 0.5s 지속변화 추종 | 체감 | 판정 |
|---|---|---|---|---|
| 3.0 | 0.33s | 78% | 매우 부드럽지만 도주 방향 전환이 다소 느려 포위 회피가 늦을 수 있음 | 너무 느슨 |
| **5.0** | **0.2s** | **92%** | **떨림 완전 제거 + 도주 반응 민첩 — 권장** | **채택** |
| 8.0 | 0.125s | 98% | 거의 즉시 추종이라 밀집 상황 잔여 진동이 일부 새어나올 여지 | 너무 빡빡 |

**권장: `_fleeTurnSmoothing = 5.0`.** 프로토타입 튜너블이므로 육안 확인(plan Task 4 Step2) 후 3~8 범위에서 조정 가능. 조정 시 결정 메트릭: "도주 시 영웅 yaw 가 1초 안에 2회 이상 뒤집히지 않으면 통과(떨림 제거), 동시에 무리 이동 시 0.5초 내 도주 방향이 따라가면 통과(반응성)."

### 2.3 A-3 엣지 (수치 없음 — 동작만)
- FleeMode 진입 순간 `_fleeDirSmoothed` 를 현재 raw 방향으로 스냅 (이전 stale 값에서 lerp 시작 금지).
- centroid 실패(반경 내 0마리) → 기존 최근접 타겟 방향 fallback, 감쇠 동일 적용.
- `OnEnable`(풀 재사용)에서 `_fleeDirSmoothed` 리셋.

---

## 3. B. 영웅 중앙 끌림 — 수치 결정

### 3.1 타이밍 (락된 값)
- `_centerPullInterval = 3.0s` (사용자 합의 고정).
- `_centerPullDuration = 1.0s` (사용자 합의 고정).
- 사이클: 3초 주기 중 첫 1초는 중앙 방향 이동 창, 나머지 2초는 평소 전투. 반복.

### 3.2 `_centerPullDeadzone` — **권장 3.0 units**

**1회 끌림 이동거리** = 영웅 이동속도 × 이동창 = 3 units/s × 1s = **3 units** (단, `MoveTowards` 는 중앙에서 멈추므로 중앙을 지나치지 않음 — 최대 3 units, 중앙 근처면 더 짧음).

deadzone 을 이 1회 이동거리와 묶어서 정한다:
- deadzone ≪ 3 → 영웅이 매 사이클 중앙에 거의 정확히 박힘(로봇처럼 정중앙 고정 → 부자연).
- deadzone ≫ 3 → 한 번의 1초 창이 deadzone 경계를 의미 있게 넘지 못해 끌림 효과가 사라짐.
- **deadzone ≈ 3** → 영웅은 "한 번의 끌림 창이 정확히 중앙에 닿는 거리"에 도달하면 끌림을 멈춘다. 즉 중앙 반경 3 units 안에 들어오면 더 끌지 않아 자연스럽게 그 안에서 전투.

**아레나 비율 점검**: deadzone 3 / 짧은 반-extent(Z=7.5) = **40%**, / 긴 반-extent(X=11) = **27%**. 즉 전투는 중앙을 둘러싼 약 반경 3(직경 6) units 의 중앙 영역에 정착하고, 클램프 박스 가장자리(X±11 / Z±7.5)로는 흐르지 않는다. 가장자리에서 시작한 전투가 중앙으로 모이는 데 필요한 끌림 횟수: 가장 먼 코너(거리 ≈ √(11²+7.5²) ≈ 13.3 units)에서 deadzone 3 까지 약 (13.3−3)/3 ≈ **3.4회 = 약 10초** (3초 주기 × 3.4) — 전투를 끊지 않으면서도 체감되는 속도로 중앙 집결.

**후보 비교**:

| deadzone | 아레나 비율(Z반-extent 대비) | 거동 | 판정 |
|---|---|---|---|
| 1.5 | 20% | 거의 정중앙 고정 — 끌림 후 다시 0.5칸 더 박힘, 영웅 과밀착 | 너무 빡빡 |
| **3.0** | **40%** | **1회 끌림 거리와 일치 — 중앙 반경 3 안에서 자연 정착** | **채택** |
| 5.0 | 67% | 중앙 영역이 넓어 전투가 여전히 한쪽으로 치우칠 여지 | 너무 느슨 |

**권장: `_centerPullDeadzone = 3.0`.** 프로토타입 튜너블 — 육안 확인 후 2~4 범위 조정 가능. 조정 시 결정 메트릭: "5분 중 전투(영웅 위치)가 중앙 반경 deadzone+이동거리(≈6) units 안에 머무는 시간 비율 ≥ 70% 면 통과."

### 3.3 이동 창 중 공격 처리 — **plan 기본 유지: 이동 우선·공격 보류 (확정)**

plan 기본("1초간 이동 우선, 공격 보류")을 **그대로 채택**한다. 단순한 페이싱 선호가 아니라 **구조적으로 다른 선택지가 성립하지 않기 때문**이다.

- **구조적 근거 (결정적)**: `AutoCombatAI.Update` 의 영웅 공격 게이트(`AutoCombatAI.cs:106–110`)는 `_attackGate.IsAttacking` 이면 `_mover.Stop(); return;` 으로 **중앙 끌림 분기에 도달하기 전에 함수를 빠져나간다**. 만약 "사거리 내 적은 지나치며 공격 허용"으로 바꾸면, 몬스터가 늘 영웅을 둘러싸므로 이동 창 1초가 매 스윙마다 정지로 끊겨 **영웅이 중앙으로 거의 이동하지 못한다 → B의 목적 자체가 무력화**된다. 따라서 끌림 창 동안엔 교전/공격 분기로 흐르지 않고 중앙 이동만 수행해야 효과가 보장된다.
- **DPS 손실 점검 (무해)**: 영웅 공격 쿨다운 1s 이므로, 1초 보류는 3초 사이클당 최대 약 1회 공격 손실. 게다가 영웅은 **플레이어가 처치 대상으로 삼는 존재** — 영웅 DPS 가 약간 줄면 플레이어에게 미세하게 유리할 뿐 밸런스를 깨지 않는다.
- **수용 사항(프로토타입)**: 이동 창이 영웅 공격 recovery 중에 열리면 그 프레임들은 상위 게이트에 막혀 실제 이동이 3 units 보다 약간 짧아질 수 있다 — 프로토타입 범위에서 허용(끌림이 약간 느려질 뿐, 누적되면 다음 사이클이 보충).

### 3.4 발동 조건 / 상호작용 (락된 동작 — 수치는 위에서 결정)
- 조건: `_centerPullEnabled == true && FleeMode == false && (중앙까지 거리) > _centerPullDeadzone`.
- **FleeMode(공포) 중 미적용** — 도주 우선. 도주 끝나면 중앙 끌림 재개 (분기 순서상 FleeMode 분기보다 뒤).
- 모든 상위 가드(사망 / `IAttacker.Enabled==false` TimeStop / 스폰게이트 / 공격게이트) 통과 후에만 평가.
- `OnEnable`(풀 재사용)에서 중앙 끌림 사이클 타이머 리셋.
- deadzone 경계 떨림: 창은 3초 주기 단위라 매 프레임 토글이 아니므로 무해 (spec B-6).

### 3.5 게이트 (영웅 전용)
- `_centerPullEnabled`: **Knight.prefab 만 true**. 몬스터 프리팹 전부 기본 false → 몬스터 동작 완전 불변. 런타임 타입 추론 없이 명시 플래그로만 영웅 식별.

---

## 4. 구현 요청사항 (gameplay-programmer 용)

> 본 기능은 기존 `AutoCombatAI` 의 `[SerializeField]` 튜너블 추가 + 프리팹 값 설정으로 완결된다. **신규 Enum / Interface / 에셋 키 / SO 스키마 없음.** (centroid 보정용 메서드 분리는 plan Task 1 이 시그니처를 고정 — 본 기획서는 그 수치/동작만 명세하며 새 공용 타입을 만들지 않는다.)

- **신규 Enum**: 없음.
- **신규 Interface**: 없음. (plan Task 1 의 `ITargetProvider` 도주용 centroid 접근 메서드는 plan 이 시그니처를 정함 — 기획서가 추가하는 공용 인터페이스 아님.)
- **신규 에셋 키**: 없음. (Knight.prefab 기존 에셋 값 수정만.)
- **SO 스키마**: 없음.

### 4.1 AutoCombatAI 신규/변경 SerializeField 값 (프로토타입 초기값)

| 필드 | 타입 | 권장값 | 근거 (§) |
|---|---|---|---|
| `_fleeTurnSmoothing` | float | **5.0** | §2.2 — τ=0.2s, 떨림 8%/프레임 억제 + 0.5s 변화 92% 추종 |
| `_centerPullEnabled` | bool | **true (Knight.prefab 만)** | §3.5 — 몬스터는 false 유지(불변) |
| `_centerPullInterval` | float | **3.0** | §3.1 — 사용자 합의 고정 |
| `_centerPullDuration` | float | **1.0** | §3.1 — 사용자 합의 고정 |
| `_centerPullDeadzone` | float | **3.0** | §3.2 — 1회 끌림 이동거리(속도3×창1s)와 일치, 아레나 Z반-extent의 40% |

### 4.2 비변경 (회귀 방지)
- `_fleeThreatRadius = 4` 유지 (§2.1).
- 타겟팅 `TryFindNearest` 의 `requireEngaging` 필터 불변.
- 몬스터 프리팹 전부 미수정 — `_centerPullEnabled` 기본 false.

---

## 5. plan / spec 정합 점검 (Self-Review)

| spec/plan 항목 | 본 기획서 매핑 |
|---|---|
| spec A-1 / plan Task1 — centroid 보정(engaging 무관, 반경 유지) | §2.1 (반경 4 유지 근거 포함) |
| spec A-2 / plan Task2 — `_fleeTurnSmoothing` rate 수치 | §2.2 (**5.0** 확정 + 후보 비교 + 메트릭) |
| spec A-3 / plan Task2 — 스냅·fallback·OnEnable 리셋 | §2.3 (수치 없음, 동작 확인) |
| spec B-1/B-5 / plan Task3 — `_centerPullEnabled` 게이트·튜너블 | §3.5, §4.1 |
| spec B-3 — interval/duration | §3.1 (3.0 / 1.0 락) |
| spec B-4 — `_centerPullDeadzone` 수치 (필수) | §3.2 (**3.0** 확정 + 이동거리 점검 + 비율) |
| spec B-3 / plan — 이동 창 중 공격 처리(game-designer 결정) | §3.3 (**이동 우선·공격 보류** 확정, 구조적 근거) |
| spec B-4 — FleeMode 중 미적용 | §3.4 |
| spec B-6 / plan — 상위 가드 우선·OnEnable 리셋·deadzone 경계 | §3.4 |
| spec — 몬스터 불변 회귀 | §3.5, §4.2 |

**빠진 결정 없음.** spec 이 game-designer 에 위임한 3개 수치(감쇠 rate, deadzone, 공격 처리)를 모두 근거와 함께 확정. interval/duration 은 사용자 합의 고정값으로 명시.

**Self-Review 결과**: 통과. No-Placeholder 5개 카테고리 잔존 0건(모든 수치에 근거 한 줄 + 검산식). 식별자 표기 일관(`_fleeTurnSmoothing` / `_fleeDirSmoothed` / `_centerPullEnabled` / `_centerPullInterval` / `_centerPullDuration` / `_centerPullDeadzone` — spec/plan 과 글자 그대로 동일). "또는/재량" 등 두 갈래 위임 0건(공격 처리는 한 갈래로 확정). 구현 요청사항은 "신규 타입 없음"을 명시(빈칸 아님).
