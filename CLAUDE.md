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
  qa-reports/                  qa-simulator 시뮬레이션 리포트
```

## 5. 코딩 룰 — 14개 요약

전문은 `.claude/rules/NN-*.md` 각 파일에 있다. **룰이 걸린 작업이면 해당 룰 파일 전문을 반드시 읽는다.** 아래 요약만 보고 넘어가지 않는다.

| # | 파일 | 한 줄 요약 |
|---|---|---|
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

이 프로젝트는 **메인 오케스트레이터 + 4개 서브에이전트** 협업 구조다. 메인은 흐름을 조율하고 위임하며, **직접 코드를 짜지 않는다.**

| 에이전트 | 호출 시점 | 정의 |
|---|---|---|
| **game-designer** | 새 카드·몬스터·시스템의 기획, 밸런스 수치, 페이싱·시너지 설계. 기능 기획서가 필요할 때 | `.claude/agents/game-designer.md` |
| **gameplay-programmer** | `.cs` 파일을 한 줄이라도 작성·수정할 때. 구현·ChvjPackage 연동·MVVM·SO 스키마 | `.claude/agents/gameplay-programmer.md` |
| **test-engineer** | gameplay-programmer 산출물의 본격 테스트 스위트(엣지 망라·회귀·통합)가 필요할 때 | `.claude/agents/test-engineer.md` |
| **qa-simulator** | 밸런스 검증 — 헤드리스 N판 시뮬레이션 + 메트릭 리포트가 필요할 때 | `.claude/agents/qa-simulator.md` |

위임 판단 기준:
- 기획·수치·밸런스를 정해야 함 → **game-designer**
- C# 코드를 만들거나 고쳐야 함 → **gameplay-programmer** (기획서 없으면 game-designer 먼저)
- 본격 테스트 코드가 필요함 (gameplay-programmer 의 "정상+엣지1" 을 넘어서) → **test-engineer**
- "X 가 너무 강하다/약하다" 밸런스 의심 → **qa-simulator**

## 7. 표준 협업 흐름

### 새 기능 개발
1. **game-designer** 가 기획서 작성 → `docs/design/[기능명].md`
2. **사용자** 가 기획서 리뷰·승인
3. **gameplay-programmer** 가 기획서를 읽고 구현
4. **test-engineer** 가 본격 테스트 스위트 작성
5. (게임플레이 영향이 큰 경우) **qa-simulator** 가 시뮬레이션으로 밸런스 검증
6. 변경사항 요약 + 커밋 메시지(안) 제시 — Rule 01 (직접 커밋 X, `git add` 까지)

### 밸런스 조정
1. 사용자 또는 game-designer 가 의심 제기 ("X 카드가 너무 강한 것 같다")
2. **qa-simulator** 호출 → N판 시뮬 후 데이터 리포트 (`docs/qa-reports/`)
3. **game-designer** 가 데이터 기반 조정안 작성
4. 사용자 승인
5. **gameplay-programmer** 가 SO/수치 수정
6. **test-engineer** 가 회귀 테스트 통과 확인
7. **qa-simulator** 가 조정 후 재시뮬로 검증

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
