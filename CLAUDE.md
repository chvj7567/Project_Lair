# Project Lair

## 1. 한 줄 컨셉

**5분짜리 역방향 보스전 로그라이크.** 플레이어는 던전 주인. 모험가 영웅 한 명이 자동으로 던전을 돌파해오고, 플레이어가 배치한 몬스터 무리가 자동 전투한다. 영웅 HP 10%마다 패시브 카드(3택 1), 30초마다 액티브 카드(3택 1)를 골라 5분 안에 영웅을 처치하면 승리.

- 코드네임 **Project Lair** · 코드 namespace `Lair` · 자동전투 로그라이크 + 덱빌딩 · 2.5D 탑다운 · 싱글플레이

## 2. 현재 단계 — MVP

검증 가설: **"5분 자동전투 + HP%/시간 트리거 선택지가 재미있는가."**
영웅 1 · 몬스터 6종 · 패시브 15장 · 액티브 10장 구현 완료, 밸런싱·검증 중.
메타 진행 / 서버 / 사운드 / 아트는 **이 단계에서 작업하지 않는다** (§8).

## 3. 기술 스택

- **엔진**: Unity 2022.3+ / URP
- **인프라 패키지**: `com.chvj.unityinfra` (ChvjPackage) — `Packages/com.chvj.unityinfra/`. 리소스·풀·UI·오디오. 모든 작업의 기준 인프라.
- **에셋 로딩**: Addressables — `CHMResource`, Enum 키 기반
- **아키텍처**: MVVM (View ↔ ViewModel ↔ Model 단방향)
- **테스트**: Unity Test Framework (NUnit) — EditMode / PlayMode

## 4. 폴더 구조

```
Assets/_Lair/                  게임 코드·에셋  (주의: _Project 가 아니라 _Lair)
  Scripts/                     런타임 C#  (asmdef: Lair) — Battle/ Card/ Character/ Data/ UI/
  Editor/                      에디터 툴 — 프리팹 빌더 / 테스트 러너 / 디버그 윈도우
  Tests/EditMode/  Tests/PlayMode/   NUnit 테스트  (asmdef: Lair.Tests.EditMode / .PlayMode)
  Art/                         Addressable 에셋 — Characters/ FX/ UI/ Cards/ Materials/ Sprites/
  Data/                        비-Addressable 데이터 — Fonts/ , BalanceConfig.asset
  Scenes/                      Battle.unity
Packages/com.chvj.unityinfra/  인프라 패키지  (수정 시 Rule 07)
.claude/
  rules/                       코딩 룰 01~14  ← 단일 진실의 원천
  agents/                      서브에이전트 정의
docs/
  design/                      project_lair_concept.md (컨셉) + game-designer 기능 기획서
  superpowers/specs/           메인 brainstorming 산출물 — 의도·범위·메커니즘 윤곽
  superpowers/plans/           메인 writing-plans 산출물 — 단계별 구현 계획
  qa-reports/                  qa-simulator 시뮬레이션 리포트
```

## 5. 코딩 룰 — 14개 요약

전문은 `.claude/rules/NN-*.md` 각 파일에 있다. **룰이 걸린 작업이면 해당 룰 파일 전문을 반드시 읽는다.** 아래 요약만 보고 넘어가지 않는다.

| # | 파일 | 한 줄 요약 |
|---|---|---|
| 00 | `00-project-meta-file.md` | agent 가 첫 단계에서 읽는 `.claude/project.md` 의 필수 키와 갱신 규약 (다른 모든 룰·에이전트의 진입점) |
| 01 | `01-no-auto-commit.md` | `git commit` 직접 실행 금지. `git add`(스테이징) + 한글 커밋 메시지(안) `# [주제] - 메시지` 까지만 |
| 02 | `02-comment-prefix.md` | 모든 단일 라인 주석은 `//#` 접두어 |
| 03 | `03-loose-coupling.md` | 종속성 최소화 — 인터페이스/주입 우선, 싱글톤 직접 호출·`FindObjectOfType` 지양 |
| 04 | `04-prefab-repeated-assets.md` | 2회 이상 반복되는 GameObject 구성은 프리팹, 변형은 Prefab Variant |
| 05 | `05-mvvm-pattern.md` | MVVM — View ↔ VM ↔ Model 단방향, View 에 비즈니스 로직 금지 |
| 06 | `06-interface-for-parent.md` | 상위 스크립트는 인터페이스로 접근 — `GetComponentInParent<IXxx>()` |
| 07 | `07-base-on-chvj-package.md` | ChvjPackage 기준 — 신규 작업 전 패키지 기능 우선 확인, 공통 기능은 패키지에 |
| 08 | `08-enum-key-naming.md` | Enum 키 = 에셋 파일명 (대소문자 일치). `CHMResource`/`CHMUI` 는 `Enum.ToString()` 로드 |
| 09 | `09-common-enum-single-file.md` | 공용 Enum 은 `Scripts/Data/CommonEnum.cs` 단일 파일에 통합 |
| 10 | `10-common-interface-single-file.md` | 공용 Interface 는 도메인별 `CommonInterface.cs` 단일 파일에 통합 |
| 11 | `11-use-chvj-ui-components.md` | UI 는 `CHText`/`CHButton`/`CHToggle`/`CHPoolingScrollView` 래퍼. Legacy Text·단일 Button 직접 사용 금지 |
| 12 | `12-use-chvj-pool-for-all-spawns.md` | 모든 런타임 스폰은 `CHMPool.Pop`/`Push`. `Instantiate`/`CreatePrimitive` 직접 금지 |
| 13 | `13-uiarg-with-uibase.md` | `UIArg` 파생은 페어 `UIBase` 파생과 같은 `.cs` 파일 상단에 정의 |
| 14 | `14-asset-folder-structure.md` | Addressable 에셋은 `Art/` 하위 타입별 정리. 이동 시 `.meta` 동행. `Resources/` 금지 |

## 6. 멀티 에이전트 위임 규칙

이 프로젝트는 **메인 오케스트레이터 + 6개 서브에이전트** 협업 구조다. 메인은 흐름을 조율하고 위임하며, **직접 코드를 짜지 않는다.**

| 에이전트 | 호출 시점 | 정의 |
|---|---|---|
| **game-designer** | 새 카드·몬스터·시스템의 기획, 밸런스 수치, 페이싱·시너지 설계. 기능 기획서가 필요할 때 | `.claude/agents/game-designer.md` |
| **design-reviewer** | game-designer 기획서를 사용자 리뷰 전에 1차 검토할 때. 논리·밸런스·범위·명세 완성도 점검 (읽기 전용) | `.claude/agents/design-reviewer.md` |
| **gameplay-programmer** | `.cs` 파일을 한 줄이라도 작성·수정할 때. 구현·ChvjPackage 연동·MVVM·SO 스키마 | `.claude/agents/gameplay-programmer.md` |
| **code-reviewer** | gameplay-programmer 산출 `.cs` 가 14개 룰 준수 + 기획서 일치하는지 검토할 때 (읽기 전용) | `.claude/agents/code-reviewer.md` |
| **test-engineer** | gameplay-programmer 산출물의 본격 테스트 스위트(엣지 망라·회귀·통합)가 필요할 때 | `.claude/agents/test-engineer.md` |
| **qa-simulator** | 밸런스 검증 — 헤드리스 N판 시뮬레이션 + 메트릭 리포트가 필요할 때 | `.claude/agents/qa-simulator.md` |

위임 판단 기준:
- 기획·수치·밸런스를 정해야 함 → **game-designer**
- 기획서가 나왔고 사용자 리뷰 전 1차 검토가 필요함 → **design-reviewer**
- C# 코드를 만들거나 고쳐야 함 → **gameplay-programmer** (기획서 없으면 game-designer 먼저)
- 코드가 나왔고 룰 준수·기획 일치 검토가 필요함 → **code-reviewer**
- 본격 테스트 코드가 필요함 (gameplay-programmer 의 "정상+엣지1" 을 넘어서) → **test-engineer**
- "X 가 너무 강하다/약하다" 밸런스 의심 → **qa-simulator**

## 7. 표준 협업 흐름

### 새 기능 개발
0. **메인** 이 `superpowers:brainstorming` 으로 사용자와 의도·범위·메커니즘 윤곽 합의 → `docs/superpowers/specs/YYYY-MM-DD-[기능명]-design.md`
   - 구체 수치(HP/데미지/쿨다운 등)는 이 단계에서 정하지 않는다 — game-designer 결정 영역
1. **메인** 이 `superpowers:writing-plans` 로 구현 계획 작성 → `docs/superpowers/plans/YYYY-MM-DD-[기능명].md`
   - 호출 시 룰 컨텍스트(01·02·07·08·10·11·12·14 + ChvjPackage API) 와 스펙 문서를 함께 전달
   - **우리 룰 적응**: 매 step `git commit` → "git add 까지"로 치환 (Rule 01), 매 step TDD 5단계는 강제하지 않음 — 본격 테스트는 test-engineer
2. **game-designer** 가 스펙 + 계획서를 읽고 게임 도메인 기획서 작성 → `docs/design/[기능명].md`
   - 밸런스 수치 / 시너지 / 페이싱 / 구현 요청사항 (계획서가 정한 파일 구조·인터페이스 안에서 결정)
3. **design-reviewer** 가 기획서 1차 검토 — BLOCKER 있으면 game-designer 재작업 후 재검토
4. **사용자** 가 기획서 리뷰·승인
5. **gameplay-programmer** 가 스펙·계획서·기획서를 모두 읽고 구현
6. **code-reviewer** 가 코드 검토 — BLOCKER 있으면 gameplay-programmer 재작업 후 재검토
7. **test-engineer** 가 본격 테스트 스위트 작성
8. (게임플레이 영향이 큰 경우) **qa-simulator** 가 시뮬레이션으로 밸런스 검증
9. 변경사항 요약 + 커밋 메시지(안) 제시 — Rule 01 (직접 커밋 X, `git add` 까지)

> **간이 흐름**: 프로토타입·throwaway 작업은 `start-develop-simple` 스킬을 통해 0~1 단계(brainstorming·writing-plans) 와 3·6·8 단계(design-reviewer·code-reviewer·qa-simulator) 를 생략 — game-designer → gameplay-programmer → test-engineer 만 돌린다.

### 밸런스 조정
1. 사용자 또는 game-designer 가 의심 제기 ("X 카드가 너무 강한 것 같다")
2. **qa-simulator** 호출 → N판 시뮬 후 데이터 리포트 (`docs/qa-reports/`)
3. **game-designer** 가 데이터 기반 조정안 작성
4. **design-reviewer** 가 조정안 검토 — BLOCKER 있으면 game-designer 재작업
5. 사용자 승인
6. **gameplay-programmer** 가 SO/수치 수정
7. **code-reviewer** 가 수정 코드 검토 — BLOCKER 있으면 gameplay-programmer 재작업
8. **test-engineer** 가 회귀 테스트 통과 확인
9. **qa-simulator** 가 조정 후 재시뮬로 검증

## 8. MVP 단계 특수 규칙

- **비주얼은 프리미티브 도형 고정** — Unity 기본 캡슐/큐브/구체 + 색상. 아트 작업 금지 (컨셉 §11.4 매핑 준수).
- **사운드 작업 금지** — 사운드 hook 미등록 상태 허용.
- **메타 진행 / 서버 연동 금지** — v0.2 범위.
- **메인 메뉴 / 세팅 화면 금지** — 바로 게임 시작.
- MVP 범위는 컨셉 기획서 §11 이 단일 기준. 범위 밖 기능은 game-designer 가 명시적으로 승격하기 전까지 착수하지 않는다.

## 9. 절대 금지

- `git commit` / `git push` 직접 실행 (Rule 01) — `git add` + 커밋 메시지(안)까지만
- `Object.Instantiate` / `GameObject.CreatePrimitive` 직접 호출 (Rule 12) — `CHMPool.Pop`/`Push`
- Legacy `UnityEngine.UI.Text` / 단일 `Button`·`Toggle` 직접 사용 (Rule 11) — `CHText`/`CHButton`/`CHToggle`
- 하드코딩 문자열로 에셋 로드 (Rule 08) — Enum 키
- `Resources/` 특수 폴더 사용 (Rule 14) — Addressables
- View 에 비즈니스 로직 (Rule 05)
- ChvjPackage → 게임 코드 역참조 (Rule 07) — 의존 방향은 게임 → 패키지
- `//` 일반 주석 (Rule 02) — `//#`
- MVP 범위 밖 작업 (§8)
- 메인 오케스트레이터가 직접 코드 작성 — 적절한 서브에이전트에 위임
- `.claude/project.md` 의 필수 키 누락 또는 파일 부재 (Rule 00) — agent 가 프로젝트 메타를 못 읽어 동작 불능
