# 도주 떨림 안정화 + 영웅 중앙 끌림 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **프로토타입 간소(start-develop-simple) 골격 plan** — TDD 5단계를 기계적으로 강제하지 않는다. 구체 수치(감쇠 rate, interval/duration/deadzone)는 game-designer 기획서가 채운다. 본 plan 은 파일 구조·시그니처·동작 윤곽만 고정한다.

**Goal:** 공포 도주 시 영웅 떨림을 제거하고, 영웅을 주기적으로 중앙 쪽으로 잠깐 끌어 전투를 중앙 근처로 모은다.

**Architecture:** `AutoCombatAI` 의 이동 결정부에 (A) 도주 방향 시간감쇠 + centroid 입력 보정, (B) 영웅 전용 중앙 끌림 사이클을 추가한다. 둘 다 기존 상위 가드(사망/TimeStop/스폰게이트/공격게이트) 뒤에서만 동작. 몬스터 동작은 불변(B 는 `[SerializeField]` 게이트로 영웅 프리팹만 활성).

**Tech Stack:** Unity 6 / C# / MVVM, Lair.Character. 테스트는 Unity Test Framework (EditMode 우선, 위치 검증은 PlayMode 가능).

---

## 파일 구조

- **Modify** `Assets/_Lair/Scripts/Character/CharacterRegistry.cs` — monster centroid 의 `requireEngaging=false` 변형 공개 메서드 추가(A-1). 기존 `TryGetThreatCentroidMonster`(engaging=true)는 유지.
- **Modify** `Assets/_Lair/Scripts/Character/CommonInterface.cs` — `ITargetProvider` 에 도주용 centroid(engaging 무관) 접근 추가(필요 시). 기존 `TryGetThreatCentroid` 시그니처는 유지하고 별도 메서드로 분리.
- **Modify** `Assets/_Lair/Scripts/Character/HeroTargetProvider.cs` — 새 도주용 centroid 메서드를 registry 의 `requireEngaging=false` 변형에 연결.
- **Modify** `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` — (A-2) `_fleeDirSmoothed` 시간감쇠, (B) 중앙 끌림 사이클 + `[SerializeField]` 튜너블/게이트, OnEnable 리셋.
- **Modify** `Assets/_Lair/Art/Characters/Knight.prefab` — `AutoCombatAI._centerPullEnabled = true` (영웅만). 몬스터 프리팹은 손대지 않음(기본 false).
- **Test (Create)** `Assets/_Lair/Tests/EditMode/Character/FleeStabilizeTests.cs` — A 감쇠/도주 보존 검증(POCO/순수 계산 단위로 가능한 부분).
- **Test (Create)** `Assets/_Lair/Tests/PlayMode/Character/CenterPullPlayTests.cs` — B 사이클/ deadzone / FleeMode 비발동 / 몬스터 불변(위치·이동 관측이 필요한 부분).

> 테스트 분리 기준: 순수 방향 계산(감쇠 함수)은 EditMode 로 추출 가능하면 그쪽, Transform/시간/풀이 필요한 위치 거동은 PlayMode.

---

## Task 1: 도주 centroid 원인 보정 (A-1)

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CharacterRegistry.cs`
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/HeroTargetProvider.cs`
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` (도주 분기에서 새 메서드 호출)

- [ ] **Step 1:** `CharacterRegistry` 에 `requireEngaging=false` 로 monster centroid 를 구하는 공개 메서드 추가. 기존 `TryGetThreatCentroidMonster`(engaging=true) 는 타겟팅용으로 그대로 둔다. 내부 `TryGetThreatCentroid(list, …, requireEngaging)` 재사용.
- [ ] **Step 2:** `ITargetProvider` 에 도주용 centroid 접근(engaging 무관) 메서드를 추가하고 `HeroTargetProvider` 에서 Step 1 메서드에 연결. `MonsterTargetProvider` 는 도주를 쓰지 않으므로 최소 구현(또는 기존 유지) — 빌드만 통과하게.
- [ ] **Step 3:** `AutoCombatAI` 의 `FleeMode` 분기(현재 `cs:124` 의 `TryGetThreatCentroid` 호출)를 새 도주용(engaging 무관) 메서드로 교체. 타겟팅 `TryFindNearest` 는 변경 금지.
- [ ] **Step 4:** EditMode 또는 PlayMode 로 "교전 상태와 무관하게 반경 내 몬스터가 centroid 에 집계됨"을 검증하는 테스트 1개.
- [ ] **Step 5:** 컴파일 확인(UnityMCP `editor_recompile` → `editor_read_log` Error 0). 커밋(메인이 Rule 01 로 처리 — 에이전트는 git add 까지).

## Task 2: 도주 방향 시간감쇠 (A-2, A-3)

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/FleeStabilizeTests.cs` (가능 시) 또는 PlayMode

- [ ] **Step 1:** `AutoCombatAI` 에 `private Vector3 _fleeDirSmoothed;` 와 `[SerializeField] float _fleeTurnSmoothing`(rate, 기본값은 기획서 수치) 추가.
- [ ] **Step 2:** 도주 분기에서 raw 도주방향(centroid 보정 결과, 실패 시 최근접 fallback)을 구한 뒤 `_fleeDirSmoothed` 를 시간기반 계수 `1 - Mathf.Exp(-_fleeTurnSmoothing * Time.deltaTime)` 로 raw 방향에 수렴시키고, 이 smoothed 방향으로 `FaceDirection` + `MoveTo`.
- [ ] **Step 3:** FleeMode 진입 순간 `_fleeDirSmoothed` 를 현재 raw 방향으로 **스냅**(이전 프레임 FleeMode=false → true 전이 감지). `OnEnable` 에서도 리셋.
- [ ] **Step 4:** 테스트 — 진동하는 위협 입력에 대해 출력(또는 smoothing 함수)의 프레임간 각도 변화가 유계임을 검증, 그리고 시간 경과 시 위협 반대 방향으로 수렴/이동(도주 보존).
- [ ] **Step 5:** 컴파일 확인 + git add.

## Task 3: 영웅 중앙 끌림 사이클 (B-1~B-6)

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`
- Test: `Assets/_Lair/Tests/PlayMode/Character/CenterPullPlayTests.cs`

- [ ] **Step 1:** `AutoCombatAI` 에 `[SerializeField] bool _centerPullEnabled`(기본 false) + `[SerializeField] float _centerPullInterval`(3) / `_centerPullDuration`(1) / `_centerPullDeadzone` 추가. 내부 사이클 타이머 필드.
- [ ] **Step 2:** `Update` 의 상위 가드(사망 / `IAttacker.Enabled==false` / 스폰게이트 / 공격게이트) 뒤, 그리고 **FleeMode 분기보다 뒤(=FleeMode 면 중앙 끌림 미실행)** 위치에 중앙 끌림 로직 삽입. 조건: `_centerPullEnabled && FleeMode==false && 중앙거리 > _centerPullDeadzone`.
- [ ] **Step 3:** 사이클 — 경과시간 기준으로 `[0, _centerPullDuration)` 구간이면 중앙(`Vector3.zero` XZ) 방향으로 `MoveTo`+`FaceDirection`(이동 우선, 그 1초 공격 보류), `[_centerPullDuration, _centerPullInterval)` 구간이면 평소 전투 로직으로 진행(return 하지 않고 아래 교전/이동 분기로 흐르게). `OnEnable` 에서 타이머 리셋.
- [ ] **Step 4:** Knight.prefab 의 `AutoCombatAI._centerPullEnabled` 를 true 로 설정(인스펙터/YAML). 몬스터 프리팹 미변경.
- [ ] **Step 5:** PlayMode 테스트 — 영웅: 3초 주기로 1초 중앙이동 창 발생 / deadzone 내 미발동 / FleeMode 중 미발동. 몬스터(`_centerPullEnabled=false`): 동작 불변.
- [ ] **Step 6:** 컴파일 확인 + git add.

## Task 4: 통합 회귀 + 정리

- [ ] **Step 1:** 전체 EditMode/PlayMode 테스트 실행 — 기존 AutoCombatAI/캐릭터 테스트 회귀 0.
- [ ] **Step 2:** 인게임 육안 확인 게이트(사용자) — 공포 도주 시 떨림 사라짐 + 영웅이 3초마다 1초씩 중앙으로 끌리며 전투가 중앙 근처로 모임.
- [ ] **Step 3:** 변경 요약 + Rule 01 커밋 메시지(안). (메인이 처리)

---

## Self-Review (spec 대비)

- **A-1 centroid 보정** → Task 1 ✅
- **A-2 시간감쇠 / A-3 엣지(스냅·OnEnable·fallback)** → Task 2 ✅
- **B-1 게이트 / B-5 튜너블** → Task 3 Step1, Step4 ✅
- **B-2 중앙 원점 / B-3 타이밍 / B-4 deadzone·FleeMode 비적용** → Task 3 Step2,3 ✅
- **B-6 엣지(상위 가드 우선·OnEnable 리셋)** → Task 3 Step2,3 ✅
- **테스트 A/B** → Task 1·2·4 (A), Task 3·4 (B) ✅
- **몬스터 불변 회귀** → Task 3 Step5, Task 4 Step1 ✅
- 수치 플레이스홀더는 의도적 위임(game-designer) — 프로토타입 골격 plan 규약. 시그니처/파일경로/동작은 구체화됨.
