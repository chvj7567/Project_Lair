# Project Meta

이 파일은 `.claude/agents/*.md` 의 모든 서브에이전트가 작업 시작 시 가장 먼저 읽는 **프로젝트 메타** 다. agent 정의는 도메인 정보를 직접 들고 있지 않고 이 파일을 통해서만 프로젝트를 인지한다.

다른 프로젝트로 `.claude/agents/` 와 `.claude/rules/` 를 옮길 때는 **이 파일만 새 프로젝트 값으로 갈아끼우면 된다** — `CLAUDE.md` 는 자유 양식.

자세한 규약은 `.claude/rules/00-project-meta-file.md` 참조.

---

## 프로젝트

- **name**: Project Lair
- **one_liner**: 5분짜리 역방향 보스전 로그라이크 — 플레이어는 던전 주인, 영웅 한 명이 자동 던전 돌파, 카드 선택으로 영웅을 처치한다

## 컨셉 / 단계

- **concept_doc**: `docs/design/project_lair_concept.md`
- **stage**: MVP
- **stage_goal**: 5분 자동전투 + HP%/시간 트리거 선택지가 재미있는가
- **concept_sections** — 컨셉서 § 단축키
  - **stage_scope**: 11 (단계 범위)
  - **balancing**: 8 (밸런싱 기준)
  - **core_loop**: 4 (코어 루프)
  - **synergy_visibility**: 5.2 (시너지 가시성)
  - **visual_mapping**: 11.4 (비주얼 매핑)

## 코드 / 인프라

- **engine**: Unity 2022.3+ / URP
- **language**: C#
- **namespace**: Lair
- **architecture**: MVVM
- **code_root**: `Assets/_Lair/`
- **test_paths**
  - **edit_mode**: `Assets/_Lair/Tests/EditMode/`
  - **play_mode**: `Assets/_Lair/Tests/PlayMode/`
- **test_asmdef**
  - **production**: Lair
  - **edit_mode**: Lair.Tests.EditMode
  - **play_mode**: Lair.Tests.PlayMode
- **test_framework**: Unity Test Framework (NUnit)
- **test_method_naming**: korean
- **infrastructure**
  - **package_id**: com.chvj.unityinfra
  - **alias**: ChvjPackage
  - **path**: `Packages/com.chvj.unityinfra/`

## 문서 위치

- **docs**
  - **design**: `docs/design/` — game-designer 기획서
  - **qa_reports**: `docs/qa-reports/` — qa-simulator 리포트
  - **specs**: `docs/superpowers/specs/` — superpowers:brainstorming 산출물 (uses_superpowers: true 일 때만)
  - **plans**: `docs/superpowers/plans/` — superpowers:writing-plans 산출물 (uses_superpowers: true 일 때만)

## 협업 흐름 (Workflow)

- **uses_superpowers**: true   ## 슈퍼파워 플러그인 사용 여부. false 면 아래 표준 흐름의 0·1 단계(brainstorming · writing-plans) 를 생략하고 2번부터 시작 (간이 흐름 참조).

### 표준 흐름 (uses_superpowers: true)

새 기능 개발은 다음 순서를 따른다. 각 단계의 산출물이 다음 단계의 입력이 된다 — 단방향 흐름으로 desync 를 줄인다.

| 단계 | 주체 | 행위 | 산출물 |
|---|---|---|---|
| 0 | 메인 + `superpowers:brainstorming` | 사용자와 의도·범위·메커니즘 윤곽 합의, 결정 락 | `docs.specs/YYYY-MM-DD-[기능명]-design.md` |
| 1 | 메인 + `superpowers:writing-plans` | spec 을 Task 단계로 분해 — 파일 경로·시그니처·TDD·verification gate | `docs.plans/YYYY-MM-DD-[기능명].md` |
| 2 | **game-designer** | plan 의 파일 구조 안에서 도메인 결정 채움 — 수치·UX·시각·밸런스·페이싱·시너지 | `docs.design/[기능명].md` |
| 3 | **design-reviewer** | 기획서 1차 검토 (사용자 리뷰 전) — BLOCKER 있으면 game-designer 재작업 | 검토 보고 |
| 4 | **사용자** | 기획서 리뷰·승인 게이트 | 승인 |
| 5 | **gameplay-programmer** | spec + plan + 기획서 모두 참조해 `.cs` 구현 | 코드 |
| 6 | **code-reviewer** | 14 룰 + 기획서 일치 검토 — BLOCKER 있으면 재작업 | 검토 보고 |
| 7 | **test-engineer** | 본격 테스트 스위트 (엣지·회귀·통합) | 테스트 .cs |
| 8 | **qa-simulator** (게임플레이 영향 큰 경우) | 헤드리스 시뮬레이션 + 메트릭 리포트 | `docs.qa_reports/YYYY-MM-DD.md` |
| 9 | 메인 | 변경 요약 + `git add` + 한글 커밋 메시지(안) (Rule 01) | 스테이징 + 메시지(안) |

### 간이 흐름 (uses_superpowers: false)

슈퍼파워 플러그인이 없거나 throwaway 프로토타입이면 0·1 단계를 생략한다.

- 메인이 사용자와 의도·범위를 직접 합의 (별도 spec 파일 작성 없이 대화로 종결)
- 2번 game-designer 가 사용자 요구·컨셉서를 직접 입력으로 받아 기획서 작성
- 이후 3~9 단계는 표준 흐름과 동일

> 간이 흐름에서는 `docs.specs/` · `docs.plans/` 폴더 자체가 비어 있어도 무방. game-designer / gameplay-programmer 의 self-review "스펙 커버리지" 항목은 "스펙 없음 — 사용자 요구 직접 매핑" 으로 처리.

### 문서 분담 (uses_superpowers: true 일 때 — 3 문서가 같이 있을 때)

세 문서는 같은 추상의 다른 버전이 아니라 **다른 차원**이다. 결합되어야 완전.

| 문서 | 위치 | 다루는 영역 | 단일 진실 |
|---|---|---|---|
| **spec** | `docs.specs/` | 의도·범위·메커니즘 윤곽 + 결정 락 | **무엇을** 만들지의 골격 |
| **plan** | `docs.plans/` | 파일 경로·시그니처·TDD 단계·체크박스·verification gate | **어떻게 단계별로** 만들지 |
| **기획서** | `docs.design/` | 도메인 — 수치·UX·시각·밸런스·페이싱·시너지 | **어떤 수치/디자인으로** 만들지 |

**plan ↔ 기획서 sync 규칙**: 구현 후 화면/사용자 검증 결과로 기획서가 갱신되면 (예: 가시성 문제로 수치 조정), **plan 도 동일 시점에 delta 마일스톤으로 보강**한다. 두 문서가 단방향 흐름에서 어긋나는 순간 후속 구현이 어느 쪽을 따라야 할지 갈린다.

### 밸런스 조정 흐름 (별도 짧은 사이클)

새 기능이 아닌 기존 시스템의 밸런스 조정은 다음 짧은 사이클을 돈다 — 0·1 단계 (spec·plan) 없음.

1. 사용자 또는 game-designer 가 밸런스 의심 제기
2. **qa-simulator** → N판 시뮬 후 데이터 리포트
3. **game-designer** → 데이터 기반 조정안 작성 (기존 기획서 갱신 또는 짧은 patch note)
4. **design-reviewer** → 조정안 검토
5. **사용자 승인**
6. **gameplay-programmer** → SO/수치 수정
7. **code-reviewer** → 수정 검토
8. **test-engineer** → 회귀 테스트 통과 확인
9. **qa-simulator** → 조정 후 재시뮬로 검증

## 도메인 데이터 (선택)

- **balance_config_asset**: `Assets/_Lair/Data/BalanceConfig.asset`
- **card_data_folder**: `Assets/_Lair/Art/Cards/`
