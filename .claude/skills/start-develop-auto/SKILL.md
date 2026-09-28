---
name: start-develop-auto
description: Use ONLY when the user explicitly invokes this skill by name. Runs the project's feature-development pipeline (game-designer through test-engineer) end-to-end with no approval gate. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop-auto — 기획→구현→테스트 파이프라인 (전자동 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, 이 프로젝트의 표준 협업 흐름(`project.md` "협업 흐름 (Workflow)")을 **사용자 승인 게이트 없이 끝까지** 오케스트레이션한다. game-designer 가 작성한 기획서로 곧장 구현에 들어간다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (Rule 00 "메인 오케스트레이터 행동 규칙"). 각 단계를 해당 서브에이전트에 위임한다.

> 주의: 이 버전은 기획서를 사람이 검토하지 않고 구현으로 넘어간다. 사람 검토가 필요하면 `start-develop`(게이트 버전)을 쓴다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다 (이 한 번만 멈춘다).

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성.
2. **gameplay-programmer** 위임 → 기획서대로 구현.
3. **test-engineer** 위임 → 본격 테스트 스위트 작성.
4. **마무리** — 변경사항 요약 + 커밋 메시지(안) 제시. Rule 01 준수 — `git commit` 직접 실행 금지, 관련 파일 `git add` 까지만.

## 규칙

- "전자동"은 **단계 간 사용자 승인을 묻지 않는다**는 뜻이다. 그래도 다음 경우엔 멈춘다:
  - 호출 시 기능 설명이 없을 때 — 무엇을 만들지 묻는다.
  - 최종 커밋 — **절대 자동 커밋하지 않는다** (Rule 01). `git add` + 커밋 메시지(안) 까지만.
- qa-simulator(밸런스 시뮬)는 포함하지 않는다. 밸런스 검증이 필요하면 마무리 후 별도 호출을 제안한다.
- 룰은 `.claude/rules/01~04`, 위임·게이트 기준은 Rule 00 "메인 오케스트레이터 행동 규칙", 단계 순서는 `project.md` "협업 흐름 (Workflow)" 를 따른다.

## 에이전트 세션 관리

### 수정 루프 — 세션 유지 (`SendMessage`)
gameplay-programmer / test-engineer 가 자체 보고한 컴파일·테스트 실패를 같은 에이전트에게 다시 고치게 할 때는 `Agent` 로 **새 호출(새 세션, cold start)하지 않고 `SendMessage` 로 기존 세션에 이어 보낸다.** 방금 자기가 작성한 코드·의도를 기억한 채로 고쳐야 정확하다.

- 단계 전진(game-designer → gameplay-programmer → test-engineer)은 서로 다른 에이전트라 새 세션이 불가피하다 — 이때는 산출물(기획서/코드 경로)을 위임 프롬프트에 명시해 컨텍스트를 잇는다.

### 완료 시 — 세션 종료 확인 게이트
"전자동"이라도 마무리(4단계)에서 작업이 끝났다고 판단되면 **에이전트 세션을 바로 종료하지 않는다.** 사용자에게 **"에이전트 세션을 종료할지, 추가 수정을 위해 유지할지"** 를 묻고 멈춘다 (단계 간 무정지 규칙의 예외).

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

- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- 수정 루프에서 같은 에이전트를 `Agent` 로 새로 호출(cold start) — 금지. `SendMessage` 로 기존 세션 유지.
- 완료라고 판단하고 세션 종료·마무리 직행 — 금지. 사용자에게 종료 여부를 먼저 묻고, 해결 확인 후에만 종료.
- 컨텍스트 요약 후 `.claude/.active-sessions.md` 를 안 읽고 곧장 새 `Agent` 호출 — 금지. 먼저 레지스트리로 세션 맵 복구.
- 끝나고 자동 커밋 — 금지 (Rule 01).
