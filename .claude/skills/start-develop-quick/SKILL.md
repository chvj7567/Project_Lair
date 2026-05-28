---
name: start-develop-quick
description: Use ONLY when the user explicitly invokes this skill by name, or when the user selects it from the main orchestrator's pipeline-choice prompt. Runs Project Lair's lightest path — gameplay-programmer → code-reviewer only — for small bug fixes / renames / minor edits with no design or test work. Skips game-designer, design-reviewer, user-approval gate, test-engineer, qa-simulator, and superpowers 0·1 stages. Do not auto-trigger from an ordinary feature request.
---

# start-develop-quick — 간단 수정 전용 파이프라인 (최경량 버전)

## 개요

사용자가 **명시적으로 호출했거나, 메인 오케스트레이터가 제시한 후보 중 사용자가 선택한 경우에만** 발동한다. Project Lair 의 협업 흐름 4종 중 가장 가볍다 — `gameplay-programmer` 와 `code-reviewer` 만 돌린다.

오타 · 리네임 · 문구·색상값 변경 · 단일 함수 안의 소규모 버그 수정 같은 **사소한 수정** 전용. 본격 기능 · 머지 대상 · 회귀 위험 있는 변경은 `start-develop` 또는 `start-develop-auto` / `start-develop-simple` 을 쓴다.

메인 오케스트레이터는 **직접 코드를 짜지 않는다** (CLAUDE.md §6). 각 단계를 해당 서브에이전트에 위임한다.

## 호출 시 입력

수정 내용을 인자로 받는다. 인자가 없으면 무엇을 고칠지 사용자에게 1회만 묻는다.

## 사전 단계 — 슈퍼파워 분기

`.claude/project.md` 의 `uses_superpowers` 키 값과 무관하게 **0단계(`superpowers:brainstorming`) · 1단계(`superpowers:writing-plans`) 를 완전 스킵** 한다. `start-develop-simple` 의 "짧게 수행" 과 달리 본 스킬은 아예 호출하지 않는다 — 사소한 수정에 spec/plan 문서를 작성하는 비용이 의도와 모순되기 때문.

긴 합의·정밀 plan 이 필요한 작업이면 본 스킬 대신 `start-develop` 또는 `start-develop-auto` 사용.

## 파이프라인 (순서대로, 단계 간 멈춤 없이)

1. **gameplay-programmer** 위임 → 수정 구현.
   - "정상 케이스 + 엣지 케이스 1개" 수준의 스모크 확인을 gameplay-programmer 가 자체 수행한다 (`.claude/agents/gameplay-programmer.md` 의 self-review 정의).
2. **code-reviewer** 위임 → 14개 룰(`CLAUDE.md §5`) 준수 + 사용자 의도와의 부합 검토.
   - BLOCKER 가 있으면 gameplay-programmer 에 수정 위임 후 code-reviewer 재검토. **최대 3회** 반복. 3회 후에도 남으면 사용자에게 보고하고 중단.
3. **마무리** — 변경사항 요약 + 한글 커밋 메시지(안) 제시. Rule 01 준수 — `git commit` 직접 실행 금지, 관련 변경 파일 `git add` 까지만.

## 사후 안전망 — 에스컬레이션 출구

gameplay-programmer 가 작업에 들어간 뒤 **"이 수정은 quick 수준이 아니다 — game-designer / 기획서가 필요하다"** 라고 판단하면 즉시 사용자에게 보고하고 멈춘다. 사용자는 다음 중 선택:

- **(a)** `start-develop` (또는 `-auto` / `-simple`) 으로 흐름 재시작
- **(b)** 위험 인지하에 본 흐름 그대로 계속 진행

이 출구가 "메인이 임의 판단으로 큰 작업을 quick 으로 직진" 을 막는 마지막 안전망. 메인 자체 분기를 도입하지 않은 이유이기도 하다.

## 규칙

- 14개 코딩 룰(CLAUDE.md §5) · MVP 범위(§8) · 금지 사항(§9) 은 그대로 적용된다. 단계가 빠질 뿐 룰이 사라진 게 아니다.
- `test-engineer` 를 스킵한다. 회귀 위험은 gameplay-programmer 의 자체 스모크 확인에 의존하며, 본격 회귀 테스트가 필요한 작업이면 본 스킬 대신 `start-develop-simple` 이상을 사용한다.
- `qa-simulator` 도 포함하지 않는다. 밸런스 의심이 생기면 마무리 후 사용자에게 별도 호출을 제안한다.
- 최종 커밋 — **절대 자동 커밋하지 않는다** (Rule 01). `git add` + 커밋 메시지(안) 까지만.

## 사용 시점 가이드

| 상황 | 사용 스킬 |
|---|---|
| 본격 기능, 사람 검토 게이트 필요 | `start-develop` |
| 본격 기능, 검토는 자동 리뷰어로 대체 | `start-develop-auto` |
| 버릴 수도 있는 프로토타입, 가장 빠르게 | `start-develop-simple` |
| **사소한 수정 · 작은 버그 수정 · 리네임 · 문구 변경** | **`start-develop-quick` (이 스킬)** |

## 흔한 실수

- 큰 작업을 quick 으로 진행 — gameplay-programmer 가 발각하면 에스컬레이션으로 빠져나옴. 사용자 측에서도 quick 후보 선택 전 의심되면 다른 스킬로.
- 메인이 직접 `.cs` 수정 — 금지. gameplay-programmer 에 위임.
- "사소하니까" 라며 14개 룰 무시 — 금지. code-reviewer 가 BLOCKER 로 차단.
- test-engineer 도 스킵이라며 정상 케이스 확인까지 빼먹기 — gameplay-programmer 자체 스모크는 반드시 수행.
- 끝나고 자동 커밋 — 금지 (Rule 01).
