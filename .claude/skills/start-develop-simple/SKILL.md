---
name: start-develop-simple
description: Use ONLY when the user explicitly invokes this skill by name. Runs the project's feature-development pipeline as a stripped-down prototype path — game-designer → gameplay-programmer → test-engineer, skipping qa-simulator. Trades the balance gate for speed; intended for throwaway prototypes only. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop-simple — 기획→구현→테스트 파이프라인 (프로토타입 간소 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, 이 프로젝트의 표준 협업 흐름(`project.md` "협업 흐름 (Workflow)")에서 **qa-simulator 를 생략**하고 game-designer → gameplay-programmer → test-engineer 만으로 빠르게 돌린다. **프로토타입을 짧게 짜볼 때 시간 절약용**으로만 쓴다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (Rule 00 "메인 오케스트레이터 행동 규칙"). 각 단계를 해당 서브에이전트에 위임한다.

> 주의: 이 버전은 밸런스 시뮬을 건너뛴다. 본격 기능·머지 대상 코드는 `start-develop`(승인 게이트) 또는 `start-develop-auto`(전자동)를 쓰고, 밸런스 의심이 생기면 qa-simulator 를 별도 호출한다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다 (이 한 번만 멈춘다).

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성.
   - 프로토타입 범위로 작성해도 됨을 위임 프롬프트에 명시한다 (수치는 임시값, 시너지 컬럼 생략 등 허용).
   - 완료 즉시 Rule 01 에 따라 자동 커밋.
2. **gameplay-programmer** 위임 → 기획서대로 구현. 완료 즉시 자동 커밋.
3. **test-engineer** 위임 → 본격 테스트 스위트 작성. 완료 즉시 자동 커밋.
4. **마무리** — 전체 변경 요약 보고. 각 단계는 이미 Rule 01 에 따라 자동 커밋되어 있다 (별도 커밋 불필요).

## 규칙

- qa-simulator 생략은 **이 스킬 한정**이다. 다른 스킬/흐름의 단계를 임의로 빼지 않는다.
- 코딩 룰(`.claude/rules/00~04`) 과 `project.md` 의 현재 단계 범위(`stage` · `stage_goal` · `concept_doc`) 는 그대로 적용된다. 단계가 빠질 뿐 룰이 사라진 게 아니다.
- gameplay-programmer 가 자체적으로 "정상 케이스 + 엣지 케이스 1개" 수준의 스모크 확인을 수행해야 한다 (정의 §6).
- qa-simulator(밸런스 시뮬)는 포함하지 않는다. 밸런스 검증이 필요하면 마무리 후 사용자에게 별도 호출을 제안한다.
- 다음 경우엔 멈춘다:
  - 호출 시 기능 설명이 없을 때 — 무엇을 만들지 묻는다.
  - gameplay-programmer / test-engineer 가 자체 보고한 **컴파일 실패·테스트 실패**가 풀리지 않을 때 — 사용자에게 에스컬레이션.

## 사용 시점 가이드

| 상황 | 사용 스킬 |
|---|---|
| 본격 기능, 사람 검토 필요 | `start-develop` |
| 본격 기능, 승인 게이트 없이 자동 진행 | `start-develop-auto` |
| **버릴 수도 있는 프로토타입, 가장 빠르게** | **`start-develop-simple` (이 스킬)** |

## 에이전트 세션 관리

### 수정 루프 — 세션 유지 (`SendMessage`)
gameplay-programmer / test-engineer 가 자체 보고한 컴파일·테스트 실패를 같은 에이전트에게 다시 고치게 할 때는 `Agent` 로 **새 호출(새 세션, cold start)하지 않고 `SendMessage` 로 기존 세션에 이어 보낸다.** 방금 자기가 작성한 코드·의도를 기억한 채로 고쳐야 정확하다.

- 단계 전진(game-designer → gameplay-programmer → test-engineer)은 서로 다른 에이전트라 새 세션이 불가피하다 — 이때는 산출물(기획서/plan/코드 경로)을 위임 프롬프트에 명시해 컨텍스트를 잇는다.

### 완료 시 — 세션 종료 확인 게이트
프로토타입이라도 마무리(4단계)에서 작업이 끝났다고 판단되면 **에이전트 세션을 바로 종료하지 않는다.** 사용자에게 **"에이전트 세션을 종료할지, 추가 수정을 위해 유지할지"** 를 묻고 멈춘다.

- 사용자가 **"완전히 해결됐다"** 고 확인하기 전까지 세션을 유지한다. 추가 수정 요청이 오면 해당 에이전트 세션에 `SendMessage` 로 이어간다 (새 `Agent` 호출 금지).
- 사용자가 해결을 확인하면 그때 세션을 종료한다.

### 컨텍스트 요약 대비 — 세션 레지스트리 (`.claude/.active-sessions.md`)
대화 컨텍스트가 요약되면 메인이 세션 ID 추적을 잃을 수 있다 (요약은 대화창만 압축하고 파일은 건드리지 않는다). 이를 대비해 **best-effort 복구**용 레지스트리를 둔다.

- 에이전트를 `Agent` 로 spawn 하거나 `SendMessage` 로 이어갈 때마다 `.claude/.active-sessions.md` 에 `에이전트 종류 | 세션 ID(또는 이름) | 상태(대기/수정중/완료)` 를 한 줄로 갱신한다.
- 컨텍스트 요약 직후 첫 행동에서 이 파일을 **먼저 읽어** 세션 맵을 복구한 뒤 `SendMessage` 대상을 정한다.
- 사용자가 "완전히 해결됐다" 고 확인하면 세션을 종료하고 레지스트리를 비운다.
- **한계(best-effort)**: 하네스가 세션을 이미 폐기했으면 ID 가 있어도 `SendMessage` 가 실패한다 — 이때는 산출물(기획서/plan/코드 경로)로 컨텍스트를 재구성해 cold start 한다.
- 이 파일은 런타임 스크래치다 — 커밋하지 않는다 (`git add` 범위에서 제외).

## 흔한 실수

- 프로토타입이라며 코딩 룰(Rule 00~04)을 무시 — 금지. 룰은 그대로다.
- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- 프로토타입 코드를 그대로 머지 — 권장하지 않음. 머지 전엔 `start-develop` 또는 `start-develop-auto` 로 승인 게이트를 한 번 거친다.
- 수정 루프에서 같은 에이전트를 `Agent` 로 새로 호출(cold start) — 금지. `SendMessage` 로 기존 세션 유지.
- 완료라고 판단하고 세션 종료·마무리 직행 — 금지. 사용자에게 종료 여부를 먼저 묻고, 해결 확인 후에만 종료.
- 밸런스 의심이 생겼는데 이 스킬 안에서 해결하려 함 — 금지. 마무리 후 qa-simulator 별도 호출을 제안한다.
- 컨텍스트 요약 후 `.claude/.active-sessions.md` 를 안 읽고 곧장 새 `Agent` 호출 — 금지. 먼저 레지스트리로 세션 맵 복구.
- 소작업이 끝났는데 커밋을 미루고 다음 단계로 넘어가기 — 금지 (Rule 01). 단계마다 완료 즉시 커밋.
