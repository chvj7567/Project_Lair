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
- **stage**: v0.3
- **stage_goal**: 클라우드 계정·세이브·리더보드(서버 연동)가 재방문·기기이전·경쟁 동기를 강화하는가
- **concept_sections** — 컨셉서 § 단축키
  - **stage_scope**: 11 (단계 범위)
  - **balancing**: 8 (밸런싱 기준)
  - **core_loop**: 4 (코어 루프)
  - **synergy_visibility**: 5.2 (시너지 가시성)
  - **visual_mapping**: 11.4 (비주얼 매핑)

## 코드 / 인프라

- **engine**: Unity 6 (6000.0.68f1) / URP 17.0.4
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
- **editor_menu** — UnityMCP `editor_execute_menu` 로 호출하는 검증 게이트 메뉴 (정의: `Assets/_Lair/Editor/LairTestRunner.cs`)
  - **run_edit_mode_tests**: `Lair/Tests/Run EditMode Tests`
  - **run_play_mode_tests**: `Lair/Tests/Run PlayMode Tests`
  - **run_sim_batch**: `Lair/Tests/Run PlayMode Simulation`
- **test_result_json**: `Library/lair-test-result.json` — EditMode 결과
- **test_result_json_play_mode**: `Library/lair-test-result-playmode.json` — PlayMode 결과 (EditMode 와 별도 파일)
- **infrastructure**
  - **package_id**: com.chvj.unityinfra
  - **alias**: ChvjPackage
  - **path**: `Packages/com.chvj.unityinfra/`

## 문서 위치

- **docs**
  - **design**: `docs/design/` — game-designer 기획서
  - **qa_reports**: `docs/qa-reports/` — qa-simulator 리포트
  - **specs**: `docs/superpowers/specs/` — 과거 기능별 의도·범위 스펙 문서 (레거시 보관, 신규 작성 안 함)
  - **plans**: `docs/superpowers/plans/` — 과거 단계별 구현 계획 문서 (레거시 보관, 신규 작성 안 함)

## 협업 흐름 (Workflow)

새 기능 개발은 다음 순서를 따른다. 각 단계의 산출물이 다음 단계의 입력이 된다 — 단방향 흐름으로 desync 를 줄인다.

| 단계 | 주체 | 행위 | 산출물 |
|---|---|---|---|
| 1 | 메인 | 사용자와 의도·범위·메커니즘 윤곽을 대화로 합의 | - |
| 2 | **game-designer** | 사용자 요구·컨셉서를 입력으로 도메인 결정 채움 — 수치·UX·시각·밸런스·페이싱·시너지 | `docs.design/[기능명].md` (완료 시 자동 커밋 — Rule 01) |
| 3 | **사용자** | 기획서 리뷰·승인 게이트 | 승인 |
| 4 | **gameplay-programmer** | 승인된 기획서를 참조해 `.cs` 구현 | 코드 (완료 시 자동 커밋 — Rule 01) |
| 5 | **test-engineer** | 본격 테스트 스위트 (엣지·회귀·통합) | 테스트 .cs (완료 시 자동 커밋 — Rule 01) |
| 6 | **qa-simulator** (게임플레이 영향 큰 경우) | 헤드리스 시뮬레이션 + 메트릭 리포트 | `docs.qa_reports/YYYY-MM-DD.md` (완료 시 자동 커밋 — Rule 01) |
| 7 | 메인 | 전체 변경 요약 보고 (각 단계는 이미 자동 커밋되어 있음) | 요약 |

### 스킬 미지정 요청 — 메인 후보 제시 규칙

게이트 동작·제외 케이스·자체 분기 금지 등 **메인 오케스트레이터 행동 규칙은 Rule 00 (`.claude/rules/00-project-meta-file.md`) 의 "메인 오케스트레이터 행동 규칙" 섹션이 단일 진실(SoT)** 이다. 본 파일은 이 프로젝트가 제공하는 후보 스킬 표만 정의한다.

| 스킬 | 적합 작업 | 파이프라인 단계 |
|---|---|---|
| `/start-develop`       | 본격 기능 + 사람 검토 + 승인 게이트 | game-designer → ⛔승인 → gameplay-programmer → test-engineer |
| `/start-develop-auto`  | 본격 기능, 승인 게이트 없음 | game-designer → gameplay-programmer → test-engineer |
| `/start-develop-quick` | 사소한 수정 · 작은 버그 · 리네임 · 문구 변경 | gameplay-programmer |

### 밸런스 조정 흐름 (별도 짧은 사이클)

새 기능이 아닌 기존 시스템의 밸런스 조정은 다음 짧은 사이클을 돈다.

1. 사용자 또는 game-designer 가 밸런스 의심 제기
2. **qa-simulator** → N판 시뮬 후 데이터 리포트
3. **game-designer** → 데이터 기반 조정안 작성 (기존 기획서 갱신 또는 짧은 patch note)
4. **사용자 승인**
5. **gameplay-programmer** → SO/수치 수정
6. **test-engineer** → 회귀 테스트 통과 확인
7. **qa-simulator** → 조정 후 재시뮬로 검증

## 도메인 데이터 (선택)

- **balance_config_asset**: `Assets/_Lair/Data/BalanceConfig.asset`
- **card_data_folder**: `Assets/_Lair/Art/Cards/`
