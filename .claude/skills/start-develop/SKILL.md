---
name: start-develop
description: Use ONLY when the user explicitly invokes this skill by name. Runs Project Lair's feature-development pipeline (game-designer through test-engineer) with a user-approval gate after the design doc. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop — 기획→구현→테스트 파이프라인 (승인 게이트 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, Project Lair 의 표준 협업 흐름(CLAUDE.md §7 "새 기능 개발")을 한 번에 오케스트레이션한다. 기획서 단계에서 **멈춰 사용자 승인을 받은 뒤** 구현으로 넘어간다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (CLAUDE.md §6). 각 단계를 해당 서브에이전트에 위임한다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다.

## 사전 단계 — 슈퍼파워 분기

`.claude/project.md` 의 `uses_superpowers` 키를 확인해 시작 지점을 결정한다.

- **uses_superpowers: true** — 아래 파이프라인 앞에 다음 2단계를 먼저 수행:
  - **0. `superpowers:brainstorming`** → `docs/superpowers/specs/YYYY-MM-DD-[기능명]-design.md` (의도·범위 합의)
  - **1. `superpowers:writing-plans`** → `docs/superpowers/plans/YYYY-MM-DD-[기능명].md` (Task 단계화)
  - **1 완료 후 — 실행 방식 선택 게이트**: writing-plans 산출물을 사용자에게 제시하고 다음 두 갈래를 선택하게 한다. 선택 전까지 진행하지 않는다.
    - **A. 슈퍼파워 실행** — `superpowers:subagent-driven-development` (태스크별 서브에이전트) 또는 `superpowers:executing-plans` (현 세션 일괄 실행) 으로 플랜을 직접 구현. game-designer 이후 파이프라인은 건너뜀.
    - **B. start-develop 파이프라인 계속** — 아래 game-designer 단계부터 이어서 진행. spec + plan 경로를 game-designer에 함께 전달.
- **uses_superpowers: false** — 0·1 단계 생략. 메인이 사용자와 의도·범위를 대화로만 합의한 뒤 아래 파이프라인 1번부터 시작.

## 파이프라인 (순서대로)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성.
2. **design-reviewer** 위임 → 기획서 검토.
   - BLOCKER 가 있으면 game-designer 에 수정 위임 후 design-reviewer 재검토. **최대 3회** 반복. 3회 후에도 남으면 사용자에게 보고하고 중단.
3. **⛔ 승인 게이트** — 기획서 요약 + design-reviewer 결과를 사용자에게 제시하고 **멈춘다.** 사용자가 승인하기 전까지 구현을 시작하지 않는다. 사용자가 수정을 요청하면 game-designer 에 반영 위임 후 다시 이 게이트로 돌아온다.
4. **gameplay-programmer** 위임 → 승인된 기획서대로 구현.
5. **code-reviewer** 위임 → 코딩 룰(Rule 00~04) 준수 + 기획서 일치 검토.
   - BLOCKER 가 있으면 gameplay-programmer 에 수정 위임 후 code-reviewer 재검토. **최대 3회** 반복. 3회 후에도 남으면 사용자에게 보고하고 중단.
6. **test-engineer** 위임 → 본격 테스트 스위트 작성.
7. **마무리** — 변경사항 요약 + 커밋 메시지(안) 제시. Rule 01 준수 — `git commit` 직접 실행 금지, 관련 파일 `git add` 까지만.

## 규칙

- qa-simulator(밸런스 시뮬)는 이 파이프라인에 포함하지 않는다. 게임플레이 영향이 커서 밸런스 검증이 필요하면 마무리 후 사용자에게 별도 호출을 제안한다.
- 각 서브에이전트의 산출물·보고를 사용자에게 단계별로 간결히 전달한다.
- 룰·위임 기준은 CLAUDE.md §5 / §6 / §7 을 따른다.

## 흔한 실수

- 승인 게이트(3단계)를 건너뛰고 구현으로 직진 — 금지. 이 버전의 존재 이유가 게이트다. 게이트 없는 흐름은 `start-develop-auto`.
- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- BLOCKER 무한 루프 — 최대 3회 후 사용자에게 에스컬레이션.
- 끝나고 자동 커밋 — 금지 (Rule 01).
