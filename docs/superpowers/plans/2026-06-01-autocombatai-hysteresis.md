# AutoCombatAI 사거리 히스테리시스 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans. Steps use checkbox (`- [ ]`).
>
> **파이프라인 주의**: start-develop 파이프라인 입력. **버퍼 크기(도메인 수치)는 game-designer 기획서가 단일 진실.** 아래 코드의 버퍼 기본값은 *예시*이며 기획서 확정값으로 치환. spec: `docs/superpowers/specs/2026-06-01-autocombatai-hysteresis-design.md`.

**Goal:** `AutoCombatAI` 의 사거리 경계 Move/Stop 매 프레임 진동(몬스터 동기 stop-go)을 `_engaged` 히스테리시스 밴드로 제거하고, 임시 진단 계측을 정리한다.

**Architecture:** `AutoCombatAI.Update` 의 단일 `dist<=Range` 분기를 `_engaged` 상태 머신으로 교체. 미교전→`dist≤Range`면 교전 진입, 교전→`dist>Range+버퍼`여야 해제. 밴드 안에선 상태 유지(Stop+회전+공격시도)로 토글 제거. FleeMode·타겟상실·사망 분기는 불변.

**Tech Stack:** Unity 6 / C#, MonoBehaviour + 인터페이스 조합(IMover/IHealth/IAttacker/ITargetProvider/IRotator), NUnit PlayMode 테스트.

---

## 파일 구조

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` | `_engaged` 필드 + 버퍼 + Update 히스테리시스 + OnEnable 리셋 | 수정 |
| `Assets/_Lair/Scripts/Battle/MonsterSpeedDiag.cs` | 임시 진단 — 삭제 | 삭제 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | `Start()` 의 MonsterSpeedDiag 부착 `[DIAG]` 줄 제거 | 수정 |
| `Assets/_Lair/Tests/PlayMode/Diagnostics/MonsterApproachDiagTests.cs` | 수정 후 B/B2 PASS 전환 — 회귀 잠금 (코드 변경 없이 정합 확인) | 검증 |
| `Assets/_Lair/Tests/PlayMode/Character/AutoCombatAIHysteresisTests.cs` | 히스테리시스 전이 PlayMode 테스트 | 신규(test-engineer) |

> **테스트 방식**: `AutoCombatAI` 는 `Awake` 에서 `GetComponent` 로 의존성을 잡아 EditMode 순수 단위테스트가 어렵다. 기존 `AutoCombatAIRotationTests`(PlayMode, 실제 컴포넌트)·`MonsterApproachDiagTests` 패턴을 따라 **PlayMode** 로 검증.

---

## Task 1: AutoCombatAI 히스테리시스 밴드

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`

현재 `Update()` 말미(거리 분기):
```csharp
            float dist = Vector3.Distance(transform.position, t.position);
            if (dist <= _attacker.Range)
            {
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.Stop();
                _attacker.TryAttack(th, transform.position, t.position, Time.time);
            }
            else
            {
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.MoveTo(t.position);
            }
```

- [ ] **Step 1: 필드 추가**

클래스 상단 필드 영역에:
```csharp
        //# 교전 히스테리시스 — 사거리 경계 Move/Stop 매 프레임 토글(동기 stop-go) 방지.
        //# dist<=Range 면 교전 진입, 교전 중엔 dist>Range+버퍼 여야 해제.
        [SerializeField] private float _engageBuffer = 0.5f;   //# 기획서 확정값으로 치환 (밸런스)
        private bool _engaged;
```
> 버퍼는 절대값(+0.5) 예시. 기획서가 `Range×배율` 방식을 택하면 그 방식으로 교체.

- [ ] **Step 2: OnEnable 리셋**

`AutoCombatAI.OnEnable`(이미 존재 — FleeMode 리셋 + SnapToDirection) 에 추가:
```csharp
            FleeMode = false;
            _engaged = false;   //# 풀 재사용 시 교전 상태 잔존 방지
            _rotator?.SnapToDirection(Vector3.zero - transform.position);
```

- [ ] **Step 3: 거리 분기를 히스테리시스로 교체**

위 거리 분기 블록을 다음으로 교체:
```csharp
            float dist = Vector3.Distance(transform.position, t.position);
            float range = _attacker.Range;

            //# 히스테리시스 — 미교전: 사거리 닿으면 진입 / 교전: 버퍼 밖으로 벗어나야 해제.
            if (_engaged)
            {
                if (dist > range + _engageBuffer) _engaged = false;
            }
            else
            {
                if (dist <= range) _engaged = true;
            }

            if (_engaged)
            {
                //# 교전 — 정지 + 영웅 향해 회전 + 공격(명중은 dist<=Range 일 때만).
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.Stop();
                _attacker.TryAttack(th, transform.position, t.position, Time.time);
            }
            else
            {
                //# 추격 — 이동 목표(=타겟) 방향.
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.MoveTo(t.position);
            }
```
> 효과: 밴드 `(Range, Range+버퍼]` 안에서 `_engaged` 가 유지돼 매 프레임 Stop/Move 뒤집힘이 사라진다.

- [ ] **Step 4: Rule 02 점검** — `//#` 주석, `var` 미사용, `!` 미사용(`== false`), 가드절 형식. FleeMode/타겟상실/사망 분기 미변경 확인.

- [ ] **Step 5: 컴파일 확인** — 재컴파일 에러 0.

---

## Task 2: 임시 진단 계측 제거

**Files:**
- Delete: `Assets/_Lair/Scripts/Battle/MonsterSpeedDiag.cs` (+ .meta)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (`Start()` 의 MonsterSpeedDiag 부착 `[DIAG]` 줄)

- [ ] **Step 1: BattleController 부착 줄 제거** — `Start()` 안의 `//# [DIAG]` 표식 + `MonsterSpeedDiag` 부착 코드 제거.
- [ ] **Step 2: MonsterSpeedDiag.cs 삭제** — 파일 + meta.
- [ ] **Step 3: 잔존 확인** — `grep -rn "\[DIAG\]" Assets/_Lair/Scripts` → 0건. `grep -rn "MonsterSpeedDiag\|FrameHitchDiag\|PAUSE-DIAG" Assets/_Lair/Scripts` → 0건.
- [ ] **Step 4: 컴파일 확인** — 에러 0.

---

## Task 3: 회귀 잠금 — MonsterApproachDiagTests 정합 확인

**Files:**
- Verify: `Assets/_Lair/Tests/PlayMode/Diagnostics/MonsterApproachDiagTests.cs`

이 테스트의 B(`멀어지는영웅 추격`)·B2(`원호이동 경계`) 는 `toggles ≤ 2` 단언으로, **수정 전엔 FAIL(toggles 다수)** 이었다. 히스테리시스 수정 후 **PASS** 로 전환되어야 한다 = 본 수정의 회귀 잠금.

- [ ] **Step 1: 코드 변경 없이 정합 확인** — 수정 후 B/B2 가 PASS 인지 메인 UnityMCP 실행으로 확인. (테스트가 진단 목적이라 라벨/단언이 그대로 회귀 잠금 역할을 하면 유지. game-designer 가 버퍼를 크게 잡아도 toggles≤2 면 통과.)
- [ ] **Step 2: (필요 시) 라벨 정리** — "진단" 뉘앙스 주석을 "회귀 잠금"으로 갱신할지 test-engineer 판단. 단언 로직은 유지.

---

## Task 4: 히스테리시스 PlayMode 테스트 (test-engineer)

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/Character/AutoCombatAIHysteresisTests.cs`

- [ ] **Step 1: 전이 테스트** — 실제(또는 최소 조립) 몬스터 + 정지 타겟. 미교전→`dist≤Range` 진입 → `dist` 가 밴드 안(Range~Range+버퍼)일 때 `_engaged` 유지(Move 복귀 안 함, IsMoving 안 켜짐) → `dist>Range+버퍼` 로 멀어지면 해제(IsMoving 켜짐) 검증.
  - `_engaged` private 접근은 리플렉션 또는 IsMoving/위치로 행위 검증(권장: 행위 — 밴드 안에서 IsMoving=false 유지).
- [ ] **Step 2: 토글 부재** — 타겟을 Range 경계에서 미세하게 움직여도 `IsMoving` 토글이 임계 이하(예: ≤2) — 기존 진단 B 와 동형이되 별도 명시 케이스.
- [ ] **Step 3: 비간섭** — FleeMode 활성 시 교전 무시(도주), 타겟 상실 시 Idle, 풀 재사용 OnEnable 후 `_engaged` 리셋.
- [ ] **Step 4: 메인 실행** — `Lair/Tests/Run PlayMode Tests` PASS 확인.

---

## Self-Review (작성자 — 완료)

- **Spec 커버리지**: §2 히스테리시스→Task1, §3 엣지(Flee/타겟/풀)→Task1 Step2 + Task4 Step3, §4 계측 제거→Task2, §5 회귀잠금→Task3 + 신규 테스트 Task4. 버퍼=game-designer(코드 SerializeField 자리). 누락 없음.
- **Placeholder**: 버퍼값만 "기획서 확정"(도메인 분담) — TBD 아님, 예시 기본값 제공.
- **타입 일관성**: `_engaged`(bool)·`_engageBuffer`(float)·`_attacker.Range`·`IMover.MoveTo/Stop`·`IRotator.FaceDirection` 기존 시그니처와 일치.
