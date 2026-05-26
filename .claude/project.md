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
  - **specs**: `docs/superpowers/specs/` — superpowers:brainstorming 산출물
  - **plans**: `docs/superpowers/plans/` — superpowers:writing-plans 산출물

## 도메인 데이터 (선택)

- **balance_config_asset**: `Assets/_Lair/Data/BalanceConfig.asset`
- **card_data_folder**: `Assets/_Lair/Art/Cards/`
