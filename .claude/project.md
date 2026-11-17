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
- **play_driver**: `Assets/_Lair/Editor/PlayDriver.cs` · `PlayDriver.Sweep.cs` — Play 모드 버튼 조작·UI 스위프 도구 (Rule 05). 클래스 `Lair.EditorTools.PlayDriver`, 리포트 `Temp/PlayDriverReport.txt`
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

## UI (선택)

UI 에이전트(ui-layout-engineer · ui-visual-reviewer)와 game-designer 의 UI 목업이 읽는 프로젝트 값. 에이전트 정의에는 이 값을 넣지 않는다.

- **ui**
  - **prefab_root**: `Assets/_Lair/Art/UI/` — UI 프리팹 폴더
  - **reference_resolution**: 1280x720 — 좌표·크기 계산의 기준 해상도
  - **scope_doc**: `CLAUDE.md` §8 "공통 UI 아트 교체 허용" 항목 + `docs/design/ui-art-refresh.md` — UI 변경 허용 범위(허용: 스프라이트 참조 교체·위치·크기·글자 조정 / 금지: 신규 화면·신규 UI 기능·구조·데이터 변경, 범위 밖 자산 종류)
  - **size_classes_doc**: `docs/design/ui-art-refresh.md` §4.4 — 팝업 본체 크기 규격(규격 이름·크기·배정 화면). 규격 개수는 문서가 정한다
  - **min_font_size**: 11 — 화면 기준 글자 자동 크기 하한
  - **screenshot_dir**: `Assets/.screenshots/` — 메인이 찍은 Play 캡처 저장 위치
  - **mockup_dir**: `.mockups/` — HTML 목업·승인 시안 폴더(엔진 임포트 밖, 커밋 대상 아님)
  - **mockup_server**: `node .mockups/server.js` (포트 8777) — 이 환경의 `python` 은 Microsoft Store 스텁이라 node 사용
  - **apply_plan_glob**: `.art-incoming/*/APPLY_PLAN.md` — 화면 세트별 적용 계획표(위젯→스프라이트·안전 영역·숨길 Deco)

## 협업 흐름 (Workflow)

새 기능 개발은 다음 순서를 따른다. 각 단계의 산출물이 다음 단계의 입력이 된다 — 단방향 흐름으로 desync 를 줄인다.

| 단계 | 주체 | 행위 | 산출물 |
|---|---|---|---|
| 1 | 메인 | 사용자와 의도·범위·메커니즘 윤곽을 대화로 합의 | - |
| 2 | **game-designer** | 사용자 요구·컨셉서를 입력으로 도메인 결정 채움 — 수치·UX·시각·밸런스·페이싱·시너지 | `docs.design/[기능명].md` (완료 시 자동 커밋 — Rule 01) |
| 3 | **사용자** | 기획서 리뷰·승인 게이트 | 승인 |
| 4 | **gameplay-programmer** | 승인된 기획서를 참조해 `.cs` 구현 | 코드 (완료 시 자동 커밋 — Rule 01) |
| 4b | **ui-layout-engineer** (프리팹·UI 배치 변경이 있을 때) | 승인된 기획서·시안·`APPLY_PLAN.md` 를 참조해 프리팹 YAML 생성·수정, 팝업 크기 규격·안전 영역 기준 배치 (런타임 `.cs` 는 만지지 않음) | 프리팹 (완료 시 자동 커밋 — Rule 01) |
| 5 | **code-reviewer** | 코딩 룰(Rule 00~04) + 기획서 일치 검토 — BLOCKER 있으면 gameplay-programmer 재작업 (최대 3회) | 검토 보고 |
| 5b | **ui-visual-reviewer** (프리팹·스프라이트 참조 변경이 있을 때만) | 메인이 Play 캡처를 찍어 넘기면 글자 넘침·장식 겹침·옛 장식 잔재·가독성·비율을 검토 — BLOCKER 있으면 ui-layout-engineer 재작업 (최대 3회) | 시각 검토 보고 |
| 6 | **test-engineer** | 본격 테스트 스위트 (엣지·회귀·통합) | 테스트 .cs (완료 시 자동 커밋 — Rule 01) |
| 7 | **qa-simulator** (게임플레이 영향 큰 경우) | 헤드리스 시뮬레이션 + 메트릭 리포트 | `docs.qa_reports/YYYY-MM-DD.md` (완료 시 자동 커밋 — Rule 01) |
| 7b | 메인 → **qa-simulator** (UI·씬 흐름·버튼 동작을 건드렸을 때) | 메인이 Rule 05 절차로 UI 스위프를 실행(에이전트는 UnityMCP 불가)하고 `Temp/PlayDriverReport.txt` 를 넘기면 FAIL·SKIP·시각 확인 필요 화면을 분석 — FAIL 원인이 코드면 gameplay-programmer, 레이아웃이면 ui-layout-engineer 재작업 | `docs.qa_reports/YYYY-MM-DD.md` 의 `## UI 스위프` 섹션 (완료 시 자동 커밋 — Rule 01) |
| 8 | 메인 | 전체 변경 요약 보고 (각 단계는 이미 자동 커밋되어 있음) | 요약 |

### UI 시각 검토 단계 — 캡처 절차 (메인)

서브에이전트는 UnityMCP 를 쓸 수 없으므로 메인이 캡처를 찍어 ui-visual-reviewer 에 넘긴다.
발동 조건: 이번 변경이 프리팹·스프라이트 참조를 건드렸을 때(코드만 바꾼 작업은 생략).

1. `editor_refresh_assets` → `sim_play` → PlayDriver 로 화면 진입(`Click`/`ClickArg`, 로딩은 완료 후 재시도 — Rule 05, 마우스 ctypes 클릭 쓰지 않음) → 보려는 팝업을 연 상태에서 `screenshot_editor` (Game 창을 16:9 로 맞춰 아래쪽까지 보이게)
2. 캡처 경로(`Assets/.screenshots/editor_*.png`) 와 프리팹 경로를 위임 프롬프트에 넣어 ui-visual-reviewer 호출
3. BLOCKER 는 ui-layout-engineer 기존 세션에 `SendMessage` 로 수정(코드 슬롯이 필요하면 gameplay-programmer) → 재캡처 → 재검토 (최대 3회)
4. 끝나면 `sim_stop`. Unity 가 응답 없으면 "Prefab Has Been Modified" 같은 모달 창을 먼저 의심한다.
5. Rule 00 "UI 작업 — 목업 승인 게이트"(배선 전)와 짝이다 — 본 단계는 배선·수정 후 실제 화면 검토.

### 스킬 미지정 요청 — 메인 후보 제시 규칙

게이트 동작·제외 케이스·자체 분기 금지 등 **메인 오케스트레이터 행동 규칙은 Rule 00 (`.claude/rules/00-project-meta-file.md`) 의 "메인 오케스트레이터 행동 규칙" 섹션이 단일 진실(SoT)** 이다. 본 파일은 이 프로젝트가 제공하는 후보 스킬 표만 정의한다.

| 스킬 | 적합 작업 | 파이프라인 단계 |
|---|---|---|
| `/start-develop`       | 본격 기능 + 사람 검토 + 승인 게이트 | game-designer → ⛔승인 → gameplay-programmer → (UI 변경 시) ui-layout-engineer → code-reviewer → (UI 변경 시) ui-visual-reviewer → test-engineer |
| `/start-develop-auto`  | 본격 기능, 승인 게이트 없음 | game-designer → gameplay-programmer → (UI 변경 시) ui-layout-engineer → code-reviewer → (UI 변경 시) ui-visual-reviewer → test-engineer |
| `/start-develop-quick` | 사소한 수정 · 작은 버그 · 리네임 · 문구 변경 | gameplay-programmer(UI 변경이면 ui-layout-engineer) → code-reviewer → (UI 변경 시) ui-visual-reviewer |

### 밸런스 조정 흐름 (별도 짧은 사이클)

새 기능이 아닌 기존 시스템의 밸런스 조정은 다음 짧은 사이클을 돈다.

1. 사용자 또는 game-designer 가 밸런스 의심 제기
2. **qa-simulator** → N판 시뮬 후 데이터 리포트
3. **game-designer** → 데이터 기반 조정안 작성 (기존 기획서 갱신 또는 짧은 patch note)
4. **사용자 승인**
5. **gameplay-programmer** → JSON 수치 수정
6. **code-reviewer** → 수정 검토
7. **test-engineer** → 회귀 테스트 통과 확인
8. **qa-simulator** → 조정 후 재시뮬로 검증

## 도메인 데이터 (선택)

- **balance_config_asset**: `Assets/_Lair/Data/Json/BalanceConfig.json` — 전투 전역 상수(영웅 앵커·런 길이·카드 트리거 시점). SO 가 아니라 JSON (`EData.BalanceConfig`)
- **monster_stat_json**: `Assets/_Lair/Data/Json/MonsterStat.json` — 기준 몬스터 6종 기본 능력치·스폰 주기 (`EData.MonsterStat`)
- **json_data_folder**: `Assets/_Lair/Data/Json/` — 런타임 JSON 데이터 (파일명 = `EData` 값명)
- **card_data_folder**: `Assets/_Lair/Art/Cards/` — 카드 SO(`CardData`)는 `_id`·`_icon`·`_cardImage` 만 가진다. 이름·설명·축·효과·수치는 `Assets/_Lair/Data/Json/Card.json`·`Card_Param.json` 에서 고친다 (`EData.Card`·`EData.Card_Param`)
- **synergy_visual_config_asset**: `Assets/_Lair/Art/Synergy/SynergyVisualConfig.asset` — 시너지 4축 아이콘 SO (`EData.SynergyVisualConfig`, Addressables 주소 `SynergyVisualConfig`)
- **monster_tier_sheet_folder**: `Assets/_Lair/Art/MonsterTiers/` — 강화(T1~T3) 전신 시트 래퍼 SO `MonsterTierSheetSet` 18개 (`EMonsterTierSheet`, 전투 시작 시 필요한 것만 온디맨드 로드)
