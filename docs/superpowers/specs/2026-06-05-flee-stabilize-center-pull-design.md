# 도주 떨림 안정화 + 영웅 중앙 끌림 — 설계 (spec)

- 작성일: 2026-06-05
- 흐름: start-develop-simple (프로토타입 간소 — design-reviewer / code-reviewer / qa-simulator 생략, test-engineer 유지)
- 대상 시스템: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` (+ 도주 centroid 입력 경로 `CharacterRegistry` / `HeroTargetProvider`)

## 1. 의도 / 범위

자동전투 영웅의 이동 AI 두 가지를 조정한다. 둘 다 `AutoCombatAI` 의 이동 결정부에 들어가며, "AI 고치는 김에" 한 spec 으로 묶는다.

- **A. 도주 떨림 안정화** — 공포(Fear) 카드로 영웅이 `FleeMode` 일 때 제자리에서 덜덜 떠는 진동 제거. 도주 자체는 자연스럽게 유지.
- **B. 영웅 중앙 끌림** — 전투가 맵 가장자리로 흐르지 않게, 영웅을 주기적으로 중앙 쪽으로 잠깐 이동시켜 전투를 중앙 근처로 모은다.

범위 밖: 몬스터 이동 AI 변경 없음(B 는 영웅 전용), 밸런스 시뮬(qa-simulator)은 본 흐름 미포함, 사운드/메타 무관(MVP §8).

## 2. 원인 (A, 분석 완료)

`AutoCombatAI.Update` 의 `FleeMode` 분기(`AutoCombatAI.cs:121-139`)는 매 프레임 도주 방향을 위협 centroid 기준으로 재계산한다. centroid 는 `HeroTargetProvider.TryGetThreatCentroid` → `CharacterRegistry.TryGetThreatCentroidMonster`(`requireEngaging=true`, `CharacterRegistry.cs:90-109`) 로 **반경 4 안 "교전 중(IsEngaging)" 몬스터만** 집계한다. 몬스터가 반경 경계를 들락거리거나 교전 상태가 토글되면 centroid(=도주 방향)가 프레임마다 뒤집혀 진동한다. 기존 centroid 평균화로 완화했으나 밀집 상황에서 잔여 진동이 남는다.

## 3. 설계 — A. 도주 떨림 안정화

### A-1. 원인 보정 (centroid 입력 토글 제거)
- 도주용 centroid 집계를 **반경 내 살아있는 몬스터 전체**(engaging 무관)로 변경한다. 영웅을 향해 몰려오는 무리 전체에서 도망치는 것이 더 자연스럽고, 교전 토글 진동원이 제거된다.
- 도주 centroid 경로만 분리한다 — 타겟팅 `TryFindNearest`(공격 대상 선정)의 `requireEngaging` 필터는 **변경하지 않는다**.
- 구현 방향: `CharacterRegistry` 에 monster centroid 의 `requireEngaging=false` 변형(오버로드 또는 파라미터)을 추가하고, 도주 분기에서 그것을 쓴다. (정확한 시그니처는 plan/구현에서 확정.)

### A-2. 방향 감쇠 (프레임 독립 smoothing)
- `AutoCombatAI` 에 지속 필드 `_fleeDirSmoothed`(Vector3) 추가.
- 매 프레임 raw 도주방향(`pos − centroid`, centroid 실패 시 기존 최근접 fallback)을 구한 뒤, `_fleeDirSmoothed` 를 raw 방향으로 **시간 기반 수렴** 시킨다: 계수 `1 − exp(−rate·dt)` 형태로 프레임레이트 무관. 이 smoothed 방향으로 `FaceDirection` + `MoveTo`.
- 튜너블: `[SerializeField] float _fleeTurnSmoothing`(rate). 기본값은 구현 시 체감으로 정함.

### A-3. 엣지 케이스
- **FleeMode 진입 순간**: `_fleeDirSmoothed` 를 현재 raw 방향으로 **스냅**(이전 stale 값에서 lerp 시작 방지).
- **centroid 실패(반경 내 0마리)**: 기존 최근접 타겟 방향 fallback, 역시 감쇠 적용.
- **대칭 포위(centroid ≈ 자기 위치)**: 기존 0벡터 → 최근접 fallback 유지.
- **풀 재사용**: `OnEnable` 에서 `_fleeDirSmoothed` 리셋.

## 4. 설계 — B. 영웅 중앙 끌림

### B-1. 게이트 (영웅 전용)
- `[SerializeField] bool _centerPullEnabled` 추가. **영웅 프리팹만 true**, 몬스터 기본 false → 몬스터 동작 완전 불변. 명시적 토글로 영웅을 식별(런타임 타입 추론 없음).

### B-2. 중앙 기준점
- 월드 원점 `(0,0,0)` 의 XZ 평면. 코드상 링 중심(`AutoCombatAI.OnEnable` 의 `Vector3.zero = ring 중심` 주석과 일치).

### B-3. 타이밍 (반복 사이클)
- `_centerPullInterval`(기본 3초) 주기마다, **`_centerPullDuration`(기본 1초) 동안 중앙 방향으로 이동**한다. 그 1초 창에서는 평소 stop-and-attack / 타겟 추격을 덮어쓴다. 창이 끝나면 남은 시간(기본 2초)은 평소 전투. 반복.
- 영웅은 중앙에 **도달하지 않는다** — 1초 × 이동속도만큼만 끌려온다(점진적 중앙 집결).
- 이동 창 동안: 중앙 방향으로 `MoveTo` + 그 방향 바라봄. 기본은 "이동 우선, 그 1초간 공격 보류"(사거리 내 적 동시 타격 여부는 game-designer 가 결정).

### B-4. 발동 조건 / 상호작용 (필수 규칙)
- **중앙 deadzone**: 중앙까지 거리가 `_centerPullDeadzone` **이내면 1초 창을 발동하지 않는다**(중앙 근처에서 헛움직임 방지). — 사용자 명시 필수 규칙.
- **FleeMode(공포) 중엔 적용 안 함**: 도주가 우선. 도주 끝나면 중앙 끌림 재개.
- 몬스터가 영웅을 쫓으므로 영웅이 중앙으로 끌리면 무리도 따라와 전투 전체가 중앙 근처로 모인다(영웅만 건드려도 효과 달성).

### B-5. 튜너블
- `[SerializeField] float _centerPullInterval`(3s) · `_centerPullDuration`(1s) · `_centerPullDeadzone`. 구체 수치는 game-designer 가 결정.

### B-6. 엣지 케이스
- **풀 재사용**: `OnEnable` 에서 중앙 끌림 타이머/창 상태 리셋.
- **이동 창 중 사망/스폰게이트/TimeStop**: 기존 상위 가드(`AutoCombatAI.Update` 의 사망·`IAttacker.Enabled==false`·스폰게이트·공격게이트 체크)가 중앙 끌림보다 먼저 평가되어야 한다(중앙 끌림은 그 가드들을 통과한 뒤에만 동작).
- **deadzone 경계 떨림**: 영웅이 deadzone 경계를 오갈 때 창 발동이 토글될 수 있으나, 창은 주기(3s) 단위라 매 프레임 토글이 아니므로 무해.

## 5. 결정 락 (Locked)

- A 안정화 방식: **방향 감쇠 + 원인 보정**(둘 다). (감쇠만/throttle 단독 아님)
- B 적용 대상: **영웅만**. (몬스터/양쪽 아님)
- B 동작: 중앙 **도달이 아니라** 3초 주기마다 **1초간 중앙 방향 이동**.
- B ↔ 공포: **도주 중엔 중앙 끌림 끔**.
- B deadzone: 중앙 일정 거리 내 **미발동** (필수).
- 수치(감쇠 rate, interval/duration/deadzone)는 game-designer 가 채움 — 프로토타입이라 가변.

## 6. 테스트 (test-engineer)

### A
- 진동하는 위협 배치를 주입했을 때 출력 도주방향의 프레임간 변화가 **유계(감쇠)** 인가.
- 시간이 지나면 위협 반대쪽으로 영웅이 실제로 이동하는가(도주 기능 보존).
- FleeMode 진입 시 초기 방향 스냅(스폰/풀 재사용 후 stale 방향 미사용).

### B
- 영웅(`_centerPullEnabled=true`): 3초 주기로 1초짜리 중앙이동 창이 열리고, 창 동안 중앙 방향으로 이동, 창 밖 2초는 평소 전투.
- 중앙 deadzone 내에서는 창이 발동하지 않음.
- FleeMode 중에는 중앙 끌림 미발생.
- 몬스터(`_centerPullEnabled=false`): 동작 완전 불변(회귀).

## 7. 영향 / 안전

- FleeMode 는 영웅(Fear)만 켜지고, `_centerPullEnabled` 도 영웅 프리팹만 true → 몬스터 전투 AI 무영향.
- 코딩 룰(Rule 00~04) 그대로 적용. MVP 범위(§8) 내(이동 AI 튜닝).
