---
name: start-develop-auto
description: Use ONLY when the user explicitly invokes this skill by name. Runs Project Lair's feature-development pipeline (game-designer through test-engineer) end-to-end with no approval gate. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop-auto — 기획→구현→테스트 파이프라인 (전자동 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, Project Lair 의 표준 협업 흐름(CLAUDE.md §7 "새 기능 개발")을 **사용자 승인 게이트 없이 끝까지** 오케스트레이션한다. design-reviewer 검토를 통과한 기획서로 곧장 구현에 들어간다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (CLAUDE.md §6). 각 단계를 해당 서브에이전트에 위임한다.

> 주의: 이 버전은 기획서를 사람이 검토하지 않고 구현으로 넘어간다. 사람 검토가 필요하면 `start-develop`(게이트 버전)을 쓴다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다 (이 한 번만 멈춘다).

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성.
2. **design-reviewer** 위임 → 기획서 검토.
   - BLOCKER 가 있으면 game-designer 에 수정 위임 후 재검토. **최대 3회.** 3회 후에도 남으면 거기서 멈추고 사용자에게 보고한다 (전자동이라도 풀리지 않는 결함은 강행하지 않는다).
3. **gameplay-programmer** 위임 → design-reviewer 를 통과한 기획서대로 구현.
4. **code-reviewer** 위임 → 14개 룰 준수 + 기획서 일치 검토.
   - BLOCKER 가 있으면 gameplay-programmer 에 수정 위임 후 재검토. **최대 3회.** 3회 후에도 남으면 멈추고 사용자에게 보고한다.
5. **test-engineer** 위임 → 본격 테스트 스위트 작성.
6. **마무리** — 변경사항 요약 + 커밋 메시지(안) 제시. Rule 01 준수 — `git commit` 직접 실행 금지, 관련 파일 `git add` 까지만.

## 규칙

- "전자동"은 **단계 간 사용자 승인을 묻지 않는다**는 뜻이다. 그래도 다음 경우엔 멈춘다:
  - 호출 시 기능 설명이 없을 때 — 무엇을 만들지 묻는다.
  - design-reviewer / code-reviewer 의 BLOCKER 가 3회 내에 안 풀릴 때 — 사용자에게 에스컬레이션.
  - 최종 커밋 — **절대 자동 커밋하지 않는다** (Rule 01). `git add` + 커밋 메시지(안) 까지만.
- qa-simulator(밸런스 시뮬)는 포함하지 않는다. 밸런스 검증이 필요하면 마무리 후 별도 호출을 제안한다.
- 룰·위임 기준은 CLAUDE.md §5 / §6 / §7 을 따른다.

## 흔한 실수

- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- BLOCKER 를 무시하고 강행 — 금지. 3회 후 에스컬레이션.
- 끝나고 자동 커밋 — 금지 (Rule 01).
