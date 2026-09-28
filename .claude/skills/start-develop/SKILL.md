---
name: start-develop
description: Use ONLY when the user explicitly invokes this skill by name. Runs the project's feature-development pipeline (game-designer through test-engineer) with a user-approval gate after the design doc. Do not auto-trigger from an ordinary feature request — explicit invocation required.
---

# start-develop — 기획→구현→테스트 파이프라인 (승인 게이트 버전)

## 개요

사용자가 **명시적으로 호출했을 때만**, 이 프로젝트의 표준 협업 흐름(`project.md` "협업 흐름 (Workflow)")을 한 번에 오케스트레이션한다. 기획서 단계에서 **멈춰 사용자 승인을 받은 뒤** 구현으로 넘어간다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (Rule 00 "메인 오케스트레이터 행동 규칙"). 각 단계를 해당 서브에이전트에 위임한다.

## 호출 시 입력

기능 설명을 인자로 받는다. 인자가 없으면 무엇을 만들지 사용자에게 먼저 묻는다.

## 파이프라인 (순서대로)

1. **game-designer** 위임 → `docs/design/[기능명].md` 기획서 작성. 완료 즉시 Rule 01 에 따라 자동 커밋.
2. **⛔ 승인 게이트** — 기획서 요약을 사용자에게 제시하고 **멈춘다.** 사용자가 승인하기 전까지 구현을 시작하지 않는다. 사용자가 수정을 요청하면 game-designer 에 반영 위임 후 다시 이 게이트로 돌아온다 (수정본도 완료 시 자동 커밋).
3. **gameplay-programmer** 위임 → 승인된 기획서대로 구현. 완료 즉시 자동 커밋.
4. **test-engineer** 위임 → 본격 테스트 스위트 작성. 완료 즉시 자동 커밋.
5. **마무리** — 전체 변경 요약 보고. 각 단계는 이미 Rule 01 에 따라 자동 커밋되어 있다 (별도 커밋 불필요).

## 규칙

- qa-simulator(밸런스 시뮬)는 이 파이프라인에 포함하지 않는다. 게임플레이 영향이 커서 밸런스 검증이 필요하면 마무리 후 사용자에게 별도 호출을 제안한다.
- 각 서브에이전트의 산출물·보고를 사용자에게 단계별로 간결히 전달한다.
- 룰은 `.claude/rules/01~04`, 위임·게이트 기준은 Rule 00 "메인 오케스트레이터 행동 규칙", 단계 순서는 `project.md` "협업 흐름 (Workflow)" 를 따른다.

## 에이전트 세션 관리

### 수정 루프 — 세션 유지 (`SendMessage`)
승인 게이트에서 사용자가 기획서 수정을 요청해 game-designer 에게 다시 맡길 때는 `Agent` 로 **새 호출(새 세션, cold start)하지 않고 `SendMessage` 로 기존 세션에 이어 보낸다.** 방금 자기가 작성한 기획서·의도를 기억한 채로 고쳐야 정확하다.

- 단계 전진(game-designer → gameplay-programmer → test-engineer)은 서로 다른 에이전트라 새 세션이 불가피하다 — 이때는 산출물(기획서/코드 경로)을 위임 프롬프트에 명시해 컨텍스트를 잇는다.

### 완료 시 — 세션 종료 확인 게이트
마무리(5단계)에서 작업이 끝났다고 판단되면 **에이전트 세션을 바로 종료하지 않는다.** 사용자에게 **"에이전트 세션을 종료할지, 추가 수정을 위해 유지할지"** 를 묻고 멈춘다.

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

- 승인 게이트(2단계)를 건너뛰고 구현으로 직진 — 금지. 이 버전의 존재 이유가 게이트다. 게이트 없는 흐름은 `start-develop-auto`.
- 메인이 직접 `.cs` 를 수정 — 금지. gameplay-programmer 에 위임.
- 수정 루프에서 같은 에이전트를 `Agent` 로 새로 호출(cold start) — 금지. `SendMessage` 로 기존 세션 유지.
- 완료라고 판단하고 세션 종료·마무리 직행 — 금지. 사용자에게 종료 여부를 먼저 묻고, 해결 확인 후에만 종료.
- 컨텍스트 요약 후 `.claude/.active-sessions.md` 를 안 읽고 곧장 새 `Agent` 호출 — 금지. 먼저 레지스트리로 세션 맵 복구.
- 소작업이 끝났는데 커밋을 미루고 다음 단계로 넘어가기 — 금지 (Rule 01). 단계마다 완료 즉시 커밋.
