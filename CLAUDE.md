# Project Lair

## 1. 한 줄 컨셉

**5분짜리 역방향 보스전 로그라이크.** 플레이어는 던전 주인. 모험가 영웅 한 명이 자동으로 던전을 돌파해오고, 플레이어가 배치한 몬스터 무리가 자동 전투한다. 영웅 HP 10%마다 패시브 카드(3택 1), 30초마다 액티브 카드(3택 1)를 골라 5분 안에 영웅을 처치하면 승리.

- 코드네임 **Project Lair** · 코드 namespace `Lair` · 자동전투 로그라이크 + 덱빌딩 · 2.5D 탑다운 · 싱글플레이

## 2. 현재 단계 — MVP

검증 가설: **"5분 자동전투 + HP%/시간 트리거 선택지가 재미있는가."**
영웅 1 · 몬스터 6종 · 패시브 15장 · 액티브 10장 구현 완료, 밸런싱·검증 중.
메타 진행 / 서버 / 사운드 / 아트는 **이 단계에서 작업하지 않는다** (§8).

## 3. 기술 스택

- **엔진**: Unity 6 (6000.0.68f1) / URP 17.0.4
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

## 5. 코딩 룰

룰은 `.claude/rules/NN-*.md` 에 정의되어 있다 — Rule 00(프로젝트 메타), 01(커밋), 02(C# 스타일), 03(ChvjPackage 인프라), 04(Unity 에셋). **룰이 걸린 작업이면 해당 룰 파일 전문을 반드시 읽는다** (각 서브에이전트의 "작업 종류별 필독 룰 매핑" 표 참조).

룰은 도메인 비종속 reusable 정의 — 다른 프로젝트에 같은 `.claude/rules/` 폴더가 통째로 적용된다.

## 6. 멀티 에이전트 위임

6개 서브에이전트가 `.claude/agents/*.md` 에 정의되어 있다. **메인 오케스트레이터는 흐름 조율과 위임만 하며 직접 코드를 짜지 않는다.**

- 에이전트 목록 · 역할 · 호출 시점 · 보고 형식 → 각 `.claude/agents/<name>.md` 전문
- 단계별 위임 순서 (누구를 언제 호출할지) → `.claude/project.md` 의 "협업 흐름 (Workflow)" 섹션

에이전트 정의도 도메인 비종속 — 다른 프로젝트에 그대로 적용된다.

## 7. 표준 협업 흐름

표준 흐름·간이 흐름·밸런스 조정 흐름은 **`.claude/project.md` 의 "협업 흐름 (Workflow)" 섹션** 이 단일 진실이다. 본 CLAUDE.md 는 진입점만 둔다.

`.claude/project.md` 의 `uses_superpowers` 키가 `true` 면 표준 흐름(0~9 단계), `false` 면 간이 흐름(2번부터 시작) 으로 분기.

### Lair 특수 사항

- **프로토타입 간이 흐름**: `start-develop-simple` 스킬은 `uses_superpowers: true` 상태에서도 design-reviewer·code-reviewer·qa-simulator 단계를 추가 생략. throwaway 작업에만 사용.
- **MVP 단계 제약**: §8 의 비주얼/사운드/메타/메인메뉴 비작업 규칙이 모든 단계에 함께 적용됨.

## 8. MVP 단계 특수 규칙

- **비주얼은 프리미티브 도형 고정** — Unity 기본 캡슐/큐브/구체 + 색상. 아트 작업 금지 (컨셉 §11.4 매핑 준수).
- **사운드 작업 금지** — 사운드 hook 미등록 상태 허용.
- **메타 진행 / 서버 연동 금지** — v0.2 범위.
- **메인 메뉴 / 세팅 화면 금지** — 바로 게임 시작.
- MVP 범위는 컨셉 기획서 §11 이 단일 기준. 범위 밖 기능은 game-designer 가 명시적으로 승격하기 전까지 착수하지 않는다.

## 9. 절대 금지

- `git commit` / `git push` 직접 실행 (Rule 01) — `git add` + 커밋 메시지(안)까지만
- `Object.Instantiate` / `GameObject.CreatePrimitive` 직접 호출 (Rule 03 §4) — `CHMPool.Pop`/`Push`
- Legacy `UnityEngine.UI.Text` / 단일 `Button`·`Toggle` 직접 사용 (Rule 03 §3) — `CHText`/`CHButton`/`CHToggle`
- 하드코딩 문자열로 에셋 로드 (Rule 03 §2) — Enum 키
- `Resources/` 특수 폴더 사용 (Rule 04 §2) — Addressables
- View 에 비즈니스 로직 (Rule 02 §6)
- ChvjPackage → 게임 코드 역참조 (Rule 03 §1) — 의존 방향은 게임 → 패키지
- `//` 일반 주석 (Rule 02 §1) — `//#`
- MVP 범위 밖 작업 (§8)
- 메인 오케스트레이터가 직접 코드 작성 — 적절한 서브에이전트에 위임
- `.claude/project.md` 의 필수 키 누락 또는 파일 부재 (Rule 00) — agent 가 프로젝트 메타를 못 읽어 동작 불능
