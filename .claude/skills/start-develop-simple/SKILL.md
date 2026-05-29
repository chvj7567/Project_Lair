---
name: start-develop-simple
description: Use ONLY when the user explicitly invokes this skill by name. Runs Project Lair's feature-development pipeline as a stripped-down prototype path — game-designer → gameplay-programmer → test-engineer, skipping design-reviewer, code-reviewer, and qa-simulator. Trades review and balance gates for speed; intended for throwaway prototypes only. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop-simple — 기획→구현→테스트 파이프라인 (프로토타입 간소 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, Project Lair 의 표준 협업 흐름(CLAUDE.md §7 "새 기능 개발")에서 **리뷰어 2명(design-reviewer / code-reviewer) 과 qa-simulator 를 모두 생략**하고 game-designer → gameplay-programmer → test-engineer 만으로 빠르게 돌린다. **프로토타입을 짧게 짜볼 때 시간 절약용**으로만 쓴다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (CLAUDE.md §6). 각 단계를 해당 서브에이전트에 위임한다.

> 주의: 이 버전은 리뷰·시뮬을 모두 건너뛴다. 본격 기능·머지 대상 코드는 `start-develop`(승인 게이트) 또는 `start-develop-auto`(전자동, 리뷰 포함)를 쓰고, 밸런스 의심이 생기면 qa-simulator 를 별도 호출한다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다 (이 한 번만 멈춘다).

## 사전 단계 — 슈퍼파워 분기 (프로토타입 정신 유지)

`.claude/project.md` 의 `uses_superpowers` 키를 확인해 시작 지점을 결정한다. **본 스킬은 "프로토타입을 가장 빠르게" 가 목적**이므로 슈퍼파워 단계를 돌리더라도 최소한으로.

- **uses_superpowers: true** — 아래 2단계를 **짧게** 수행 (긴 합의·세분화는 본 스킬과 모순):
  - **0. `superpowers:brainstorming`** → 1~2턴으로 의도·범위 합의만 끝내고 `docs/superpowers/specs/YYYY-MM-DD-[기능명]-design.md` 작성. 결정 락은 최소화 (프로토타입이라 바뀔 수 있음).
  - **1. `superpowers:writing-plans`** → 골격 plan 만 작성 (`docs/superpowers/plans/YYYY-MM-DD-[기능명].md`). TDD 5단계 강제 안 함, verification gate 도 가벼움.
  - **1 완료 후 — 실행 방식 선택 게이트**: writing-plans 산출물을 사용자에게 제시하고 다음 두 갈래를 선택하게 한다. 선택 전까지 진행하지 않는다.
    - **A. 슈퍼파워 실행** — `superpowers:subagent-driven-development` (태스크별 서브에이전트) 또는 `superpowers:executing-plans` (현 세션 일괄 실행) 으로 플랜을 직접 구현. game-designer 이후 파이프라인은 건너뜀.
    - **B. start-develop-simple 파이프라인 계속** — 아래 game-designer 단계부터 이어서 진행. spec + plan 경로를 game-designer에 함께 전달.
  - 긴 합의·정밀 plan 이 필요한 본격 기능이면 `start-develop` 또는 `start-develop-auto` 사용.
- **uses_superpowers: false** — 0·1 단계 생략. 메인이 사용자와 의도만 짧게 합의 후 아래 파이프라인 1번부터 시작.

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성.
   - 프로토타입 범위로 작성해도 됨을 위임 프롬프트에 명시한다 (수치는 임시값, 시너지 컬럼 생략 등 허용).
2. **gameplay-programmer** 위임 → 기획서대로 구현. (design-reviewer 생략)
3. **test-engineer** 위임 → 본격 테스트 스위트 작성. (code-reviewer 생략)
4. **마무리** — 변경사항 요약 + 커밋 메시지(안) 제시. Rule 01 준수 — `git commit` 직접 실행 금지, 관련 파일 `git add` 까지만.

## 규칙

- 리뷰어·qa-simulator 생략은 **이 스킬 한정**이다. 다른 스킬/흐름의 단계를 임의로 빼지 않는다.
- 코딩 룰(Rule 00~04)(CLAUDE.md §5)·MVP 범위(§8)·금지 사항(§9)은 그대로 적용된다. 단계가 빠질 뿐 룰이 사라진 게 아니다.
- gameplay-programmer 가 자체적으로 "정상 케이스 + 엣지 케이스 1개" 수준의 스모크 확인을 수행해야 한다 (정의 §6).
- qa-simulator(밸런스 시뮬)는 포함하지 않는다. 밸런스 검증이 필요하면 마무리 후 사용자에게 별도 호출을 제안한다.
- 다음 경우엔 멈춘다:
  - 호출 시 기능 설명이 없을 때 — 무엇을 만들지 묻는다.
  - gameplay-programmer / test-engineer 가 자체 보고한 **컴파일 실패·테스트 실패**가 풀리지 않을 때 — 사용자에게 에스컬레이션.
  - 최종 커밋 — **절대 자동 커밋하지 않는다** (Rule 01). `git add` + 커밋 메시지(안) 까지만.

## 사용 시점 가이드

| 상황 | 사용 스킬 |
|---|---|
| 본격 기능, 사람 검토 필요 | `start-develop` |
| 본격 기능, 검토는 자동 리뷰어로 대체 | `start-develop-auto` |
| **버릴 수도 있는 프로토타입, 가장 빠르게** | **`start-develop-simple` (이 스킬)** |

## 흔한 실수

- 프로토타입이라며 코딩 룰(Rule 00~04)을 무시 — 금지. 룰은 그대로다.
- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- 프로토타입 코드를 그대로 머지 — 권장하지 않음. 머지 전엔 `start-develop` 또는 `start-develop-auto` 로 리뷰를 한 번 받는다.
- 밸런스 의심이 생겼는데 이 스킬 안에서 해결하려 함 — 금지. 마무리 후 qa-simulator 별도 호출을 제안한다.
- 끝나고 자동 커밋 — 금지 (Rule 01).
