# AutoCombatAI 사거리 히스테리시스 — 설계 spec

- **날짜**: 2026-06-01
- **유형**: 버그 수정 (코어 자동전투 루프) + 임시 진단 계측 제거
- **단계**: MVP

---

## 1. 문제 (데이터 확정)

`AutoCombatAI.Update` 가 매 프레임 다음으로 판정한다:
- `dist ≤ Range` → `Stop` + `TryAttack` (교전)
- `dist > Range` → `MoveTo(target)` (추격)

히스테리시스가 없어, 영웅 주위 **사거리(~1.5) 부근에 모인 몬스터들**은 영웅이 조금만 움직여도 거리가 Range 경계를 들락거려 **매 프레임 Stop↔Move 로 뒤집힌다.** 여러 몬스터가 동시에 같은 boundary 에 있으면 **동기 진동 → "전체가 멈췄다 진행"** 으로 보인다.

**진단 근거(임시 계측 `MonsterSpeedDiag` 로그):**
- `effSpeed` 정상(종별 0.8~2.4) — 속도 0 문제 아님.
- `actualDisp` 가 0 ↔ 0.3 을 오감(Stop 윈도우 disp 0 / Move 윈도우 disp ≈ 예상치).
- 동일 타임스탬프에 다수 몬스터 `movingButStuck` — 동기 진동 확인.

NOT: 일시정지(timeScale, `[PAUSE-DIAG]` 무관 카드 픽뿐), 프레임 히칭(`[HITCH-DIAG]` 0), 타겟 상실(Heroes 안정), 영웅 콜라이더(kinematic). 코어 `AutoCombatAI` 로직 문제이며 이번 세션 hit-feedback 작업과 무관(해당 파일 미수정).

---

## 2. 수정 — `_engaged` 상태 + 히스테리시스 밴드

`AutoCombatAI` 에 교전 상태 bool `_engaged` 도입.

| 현재 상태 | 조건 | 행위 |
|---|---|---|
| 미교전 (`_engaged=false`) | `dist ≤ Range` | **교전 진입** `_engaged=true` → 그 프레임부터 Stop+공격 |
| 미교전 | `dist > Range` | `MoveTo(target)` (추격) |
| 교전 (`_engaged=true`) | `dist > Range + 버퍼` | **교전 해제** `_engaged=false` → 다음 프레임 추격 |
| 교전 | `dist ≤ Range + 버퍼` | `Stop` + `FaceDirection(target)` + `TryAttack` (명중은 dist≤Range 일 때만) |

- 효과: 밴드 `(Range, Range+버퍼]` 안에서 상태가 유지되어 **매 프레임 Move/Stop 뒤집힘 제거.** 영웅이 버퍼 밖으로 벗어나야 추격 재개.
- 밴드 안에서 Stop 이지만 회전·공격 시도는 유지 → 몬스터가 멍하니 굳지 않음.

### 버퍼 크기 — game-designer 확정 (밸런스 민감)
- 너무 크면 카이팅 영웅을 너무 쉽게 놓침(추격 안 함), 너무 작으면 진동 재발.
- 후보: `Range × 1.2` 또는 `Range + 0.5`(고정). game-designer 가 공격 빈도·추격 거동 고려해 확정.

---

## 3. 기존 동작 보존 / 엣지

- **FleeMode(공포)** — 기존 그대로 최우선 분기. 교전 상태와 독립(Flee 중엔 교전 무시·도주).
- **타겟 상실(`TryFindNearest` 실패)** — 기존 Idle(Stop) 그대로. `_engaged` 와 무관.
- **사망/Dead** — 기존 그대로.
- **풀 재사용** — `OnEnable` 에서 `_engaged=false` 리셋 (Rule 03 §4).
- **영웅(AutoCombatAI)** — 영웅도 동일 컴포넌트 사용. 히스테리시스가 영웅 전투에도 적용됨(영웅은 단일 타겟 추격이라 동일 개선, 부작용 없음 — game-designer 확인).

---

## 4. 포함 작업 — 임시 진단 계측 제거

진단 종료. 다음 `[DIAG]` 잔재 제거:
- `Assets/_Lair/Scripts/Battle/MonsterSpeedDiag.cs` — 파일 삭제(+ meta).
- `BattleController.Start()` 의 `MonsterSpeedDiag` 부착 `[DIAG]` 줄 제거.
- `grep "[DIAG]"` 잔존 0 확인. (PauseService/FrameHitchDiag 는 앞서 제거됨)

---

## 5. 테스트

- **회귀 잠금**: 기존 진단 PlayMode `MonsterApproachDiagTests`
  - B(`멀어지는영웅 추격`)·B2(`원호이동 경계`) 의 `toggles ≤ 2` 단언이 **수정 전 FAIL(toggles 다수) → 수정 후 PASS** 로 전환. 이게 본 수정의 핵심 회귀 잠금.
  - A(정지 영웅 대조군)는 계속 PASS.
- test-engineer: 히스테리시스 전이(미교전→교전→밴드 유지→해제) 단위/PlayMode 테스트 추가. 버퍼 경계값, FleeMode·타겟상실 비간섭, 풀 재사용 `_engaged` 리셋.

---

## 6. 범위 밖

- 영웅 콜라이더 제거 여부 — 별개 사안(stutter 와 무관 확정). 본 spec 에 포함 안 함.
- 밸런스 재시뮬(qa-simulator) — 버퍼가 공격 빈도에 영향 가능하나, 필요 시 마무리 후 별도 제안.
