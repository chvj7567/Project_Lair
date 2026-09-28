---
name: start-develop-quick
description: Use ONLY when the user explicitly invokes this skill by name, or when the user selects it from the main orchestrator's pipeline-choice prompt. Runs the project's lightest path — gameplay-programmer only — for small bug fixes / renames / minor edits with no design or test work. Skips game-designer, user-approval gate, test-engineer, and qa-simulator. Do not auto-trigger from an ordinary feature request.
---

# start-develop-quick — 간단 수정 전용 파이프라인 (최경량 버전)

## 개요

사용자가 **명시적으로 호출했거나, 메인 오케스트레이터가 제시한 후보 중 사용자가 선택한 경우에만** 발동한다. 이 프로젝트의 협업 흐름 4종 중 가장 가볍다 — `gameplay-programmer` 만 돌린다.

오타 · 리네임 · 문구·색상값 변경 · 단일 함수 안의 소규모 버그 수정 같은 **사소한 수정** 전용. 본격 기능 · 머지 대상 · 회귀 위험 있는 변경은 `start-develop` 또는 `start-develop-auto` / `start-develop-simple` 을 쓴다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (Rule 00 "메인 오케스트레이터 행동 규칙"). 각 단계를 해당 서브에이전트에 위임한다.

## 호출 시 입력

수정 내용을 인자로 받는다. 인자가 없으면 무엇을 고칠지 사용자에게 1회만 묻는다.

긴 합의·정밀 plan 이 필요한 작업이면 본 스킬 대신 `start-develop` 또는 `start-develop-auto` 사용.

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **gameplay-programmer** 위임 → 수정 구현.
   - "정상 케이스 + 엣지 케이스 1개" 수준의 스모크 확인을 gameplay-programmer 가 자체 수행한다 (`.claude/agents/gameplay-programmer.md` 의 self-review 정의).
   - 완료 즉시 Rule 01 에 따라 자동 커밋.
2. **마무리** — 변경사항 요약 보고. 1단계에서 이미 Rule 01 에 따라 자동 커밋되어 있다 (별도 커밋 불필요).

## 사후 안전망 — 에스컬레이션 출구

gameplay-programmer 가 작업에 들어간 뒤 **"이 수정은 quick 수준이 아니다 — game-designer / 기획서가 필요하다"** 라고 판단하면 즉시 사용자에게 보고하고 멈춘다. 사용자는 다음 중 선택:

- **(a)** `start-develop` (또는 `-auto` / `-simple`) 으로 흐름 재시작
- **(b)** 위험 인지하에 본 흐름 그대로 계속 진행

이 출구가 "메인이 임의 판단으로 큰 작업을 quick 으로 직진" 을 막는 마지막 안전망. 메인 자체 분기를 도입하지 않은 이유이기도 하다.

## 규칙

- 코딩 룰(`.claude/rules/00~04`) 과 `project.md` 의 현재 단계 범위(`stage` · `stage_goal` · `concept_doc`) 는 그대로 적용된다. 단계가 빠질 뿐 룰이 사라진 게 아니다.
- `test-engineer` 를 스킵한다. 회귀 위험은 gameplay-programmer 의 자체 스모크 확인에 의존하며, 본격 회귀 테스트가 필요한 작업이면 본 스킬 대신 `start-develop-simple` 이상을 사용한다.
- `qa-simulator` 도 포함하지 않는다. 밸런스 의심이 생기면 마무리 후 사용자에게 별도 호출을 제안한다.

## 사용 시점 가이드

| 상황 | 사용 스킬 |
|---|---|
| 본격 기능, 사람 검토 게이트 필요 | `start-develop` |
| 본격 기능, 승인 게이트 없이 자동 진행 | `start-develop-auto` |
| 버릴 수도 있는 프로토타입, 가장 빠르게 | `start-develop-simple` |
| **사소한 수정 · 작은 버그 수정 · 리네임 · 문구 변경** | **`start-develop-quick` (이 스킬)** |

## 에이전트 세션 관리

### 수정 루프 — 세션 유지 (`SendMessage`)
사용자가 추가 수정을 요청해 gameplay-programmer 에게 다시 맡길 때는 `Agent` 로 **새 호출(새 세션, cold start)하지 않고 `SendMessage` 로 기존 gameplay-programmer 세션에 이어 보낸다.** 방금 자기가 작성한 코드·의도를 기억한 채로 고쳐야 정확하다.

### 완료 시 — 세션 종료 확인 게이트
마무리(2단계)에서 작업이 끝났다고 판단되면 **에이전트 세션을 바로 종료하지 않는다.** 사용자에게 **"에이전트 세션을 종료할지, 추가 수정을 위해 유지할지"** 를 묻고 멈춘다.

- 사용자가 **"완전히 해결됐다"** 고 확인하기 전까지 세션을 유지한다. 추가 수정 요청이 오면 gameplay-programmer 세션에 `SendMessage` 로 이어간다 (새 `Agent` 호출 금지).
- 사용자가 해결을 확인하면 그때 세션을 종료한다.

### 컨텍스트 요약 대비 — 세션 레지스트리 (`.claude/.active-sessions.md`)
대화 컨텍스트가 요약되면 메인이 세션 ID 추적을 잃을 수 있다 (요약은 대화창만 압축하고 파일은 건드리지 않는다). 이를 대비해 **best-effort 복구**용 레지스트리를 둔다.

- 에이전트를 `Agent` 로 spawn 하거나 `SendMessage` 로 이어갈 때마다 `.claude/.active-sessions.md` 에 `에이전트 종류 | 세션 ID(또는 이름) | 상태(대기/수정중/완료)` 를 한 줄로 갱신한다.
- 컨텍스트 요약 직후 첫 행동에서 이 파일을 **먼저 읽어** 세션 맵을 복구한 뒤 `SendMessage` 대상을 정한다.
- 사용자가 "완전히 해결됐다" 고 확인하면 세션을 종료하고 레지스트리를 비운다.
- **한계(best-effort)**: 하네스가 세션을 이미 폐기했으면 ID 가 있어도 `SendMessage` 가 실패한다 — 이때는 산출물(코드 경로·사용자 의도)로 컨텍스트를 재구성해 cold start 한다.
- 이 파일은 런타임 스크래치다 — 커밋하지 않는다 (`git add` 범위에서 제외).

## 흔한 실수

- 큰 작업을 quick 으로 진행 — gameplay-programmer 가 발각하면 에스컬레이션으로 빠져나옴. 사용자 측에서도 quick 후보 선택 전 의심되면 다른 스킬로.
- 메인이 직접 `.cs` 수정 — 금지. gameplay-programmer 에 위임.
- "사소하니까" 라며 코딩 룰(Rule 00~04) 무시 — 금지. 이 흐름엔 별도 검토 단계가 없으므로 gameplay-programmer 의 self-review 가 유일한 안전망이다 — 반드시 수행한다.
- test-engineer 도 스킵이라며 정상 케이스 확인까지 빼먹기 — gameplay-programmer 자체 스모크는 반드시 수행.
- 수정 루프에서 gameplay-programmer 를 `Agent` 로 새로 호출(cold start) — 금지. `SendMessage` 로 기존 세션 유지.
- 완료라고 판단하고 세션 종료·마무리 직행 — 금지. 사용자에게 종료 여부를 먼저 묻고, 해결 확인 후에만 종료.
- 컨텍스트 요약 후 `.claude/.active-sessions.md` 를 안 읽고 곧장 새 `Agent` 호출 — 금지. 먼저 레지스트리로 세션 맵 복구.
- 소작업이 끝났는데 커밋을 미루고 마무리까지 넘어가기 — 금지 (Rule 01). 완료 즉시 커밋.
