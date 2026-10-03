# 챕터 시스템 (0단계 기반 정비 + 1단계 챕터 프레임워크 + 2챕터) - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-03 · 단계: v0.3
> 입력: 승인된 로드맵 플랜 `C:\Users\TSK\.claude\plans\witty-napping-hoare.md` · `hero-stage-variant.md` · `hero-skills.md` · `village-meta-hub.md` · `village-hud-layout.md` · 코드 `StageProgress.cs` / `MetaProfile.cs` / `ActiveTriggerService.cs` / `AutoCombatAI.cs` / `HeroSelectPopup.cs` / `HeroStageVariantConfig.cs` / `balance_config.json` / `hero_skills.json`
> **UI 목업**: `.mockups/chapter-worldmap.html` (사용자 승인 게이트 대상)
> **데이터 구조표**: 본 문서 §1 (사용자 승인 게이트 대상)

---

## § 헤더

- **목표**: 현행 "같은 기사 영웅 + HP 배수 5스테이지"를 **챕터(5스테이지 묶음) 구조**로 일반화하고, (0단계) 밸런스 앵커·액티브 트리거 기준을 확정한 뒤, (1단계) 챕터/스테이지 데이터화 + 진행 저장 확장 + 챕터 월드맵 UI + 스테이지 규칙 엔진 3종 + 궁수 영웅 + 2챕터(어둠의 숲) 5스테이지를 만든다. 25스테이지 중 10스테이지 완성.
- **검증 가설**: (1) 난이도를 HP 숫자가 아니라 **메커니즘(스테이지 규칙·영웅 직업·보스 페이즈)** 으로 올리면 스테이지 2~5가 "같은 판 더 단단한 적"이 아니라 "다른 판"으로 체감되는가. (2) 영웅 직업(기사 근접 -> 궁수 카이팅)이 카드 빌드 선택(Tank/Dps/Debuff/Swarm 축)의 의미를 바꾸는가. (3) 톱니형 곡선(챕터 첫 스테이지는 직전 챕터 보스보다 쉽게)이 진행 동기를 유지하는가.
- **현재 단계 범위 적합성**: **범위 밖이었던 항목의 명시 승격 요청 - 사용자 승인 필요.** 현재 CLAUDE.md §8 은 챕터/신규 영웅 직업/스테이지 규칙을 허용 목록에 두지 않는다(5스테이지 재스킨 + 메타 + 서버 연동 한정). 본 기획서는 §0.5 의 승격 문안을 제안하며, **승인 전에는 구현에 착수하지 않는다.** 서버 연동 범위(Firebase 클라이언트 코드만)는 변경 없음. 메인 메뉴/세팅 화면은 만들지 않는다(월드맵은 마을 HUD 안 팝업).
- **핵심 메커니즘**: (a) 챕터 = 5스테이지 + 챕터 영웅 직업 1종. 스테이지 데이터는 JSON(정규화), 외형 참조는 SO. (b) 스테이지 규칙(`IStageRule`) 6종(상시 3종 + 보스 페이즈 3종)을 데이터로 배정, 일부는 영웅 HP 구간에서 발동. (c) 진행은 선형 스테이지 번호(1~10, 최대 25)로 저장해 기존 `ClearedStage` 해금 규칙(`stage <= cleared + 1`)이 챕터 경계를 자동 통과. (d) 난이도 배수는 **시간 기반 도출식**(G0 앵커 T1 + 목표 클리어 시간)으로 산출 - 숫자 상한은 도출식으로 정당화.

---

## 0. 0단계 - 기반 정비

### 0.1 액티브 트리거 5회 vs 9회 결정

**사실 확인 (코드/문서 대조)**

| 출처 | 값 |
|---|---|
| 코드 `ActiveTriggerService.DefaultThresholds` | {30, 90, 150, 210, 270}초 = **5회** |
| 데이터 `balance_config.json` `activeThresholds` | {30, 90, 150, 210, 270} = **5회** |
| `scene-2d-conversion.md` §트리거 · `content-audit/2026-06-03/04/05-*` | 5회(2026-06-03 monster-cap-removal spec A.B 적용 기준) |
| `CLAUDE.md` §1 · `project_lair_concept.md` §4.2/§11 · `continuous-spawn-round.md` §126 | "30초마다 / 최대 9회" (옛 값, stale) |

패시브는 HP 10% 구간 9회(`passiveThresholds` 0.9~0.1)로 문서·코드 일치.

**대안**

| 안 | 내용 | trade-off |
|---|---|---|
| **A. 5회 확정, 문서를 코드에 맞춤 (권장)** | CLAUDE.md/컨셉서/`continuous-spawn-round.md` 의 "9회/30초마다" 를 "5회(30·90·150·210·270초)" 로 정정 | 코드·데이터·최근 6개 문서가 이미 5회. G0 앵커 측정이 5회 코드 기준이라 **측정 전에 코드를 바꾸지 않는다**(앵커 무효화 방지). 단점: 스테이지 1에서 영웅이 130초 내외로 처치되면 액티브 픽이 2회(30·90초)뿐 |
| B. 9회로 코드 복귀 (30초 간격) | `activeThresholds` 를 {30,60,...,270} 으로 변경 | 컨셉서 원문과 일치. 그러나 결정 빈도가 늘어(총 픽 18회) 2026-06-03 spec 의 의도(밀도 완화)를 되돌리고, 76초 베이스라인과 카드 밸런스 감사 문서 전부가 5회 기준이라 재측정 부담. 클리어 시간 130초 기준 액티브 4회 |
| C. 스테이지별 트리거 (StageRow 에 임계점 배열) | 후반 스테이지만 9회 | YAGNI. 스키마 확장 + 검증 매트릭스 증가. 가설 검증에 불필요 |

**결정: A 확정.** `activeThresholds` 는 이미 데이터(`balance_config.json`)라 향후 B 로 바꾸는 비용이 **데이터 1줄**이므로 가역적이다. 결정 근거 수치: 패시브 9회 + 액티브 5회 = 최대 14픽 / 300초 = 평균 21.4초당 1회(컨셉서 "평균 ~17초/회" 는 18픽 가정의 옛 값). 액티브 픽이 적은 구간(스테이지 1)은 패시브 9픽이 보완한다.

### 0.2 문서 정정 목록 (이번 사이클에 반영, 코드 변경 없음)

| 파일 | 위치 | 정정 |
|---|---|---|
| `CLAUDE.md` | §1 한 줄 컨셉 | "30초마다 액티브 카드(3택 1)" -> "30초부터 60초 간격(30·90·150·210·270초, 최대 5회)으로 액티브 카드(3택 1)" |
| `docs/design/project_lair_concept.md` | §4.2 액티브 이벤트 · §11 범위 표 "30초 액티브 카드 (최대 9회)" · 같은 문서 line 227 | "최대 9회" -> "최대 5회 (30·90·150·210·270초)" |
| `docs/design/continuous-spawn-round.md` | line 126 | "30초마다 3택 1 (한 판 최대 9픽)" -> "30·90·150·210·270초 5회 3택 1" |
| `docs/design/hero-skills.md` | §2 수치표 | **수치 드리프트 발견**: 문서 값과 shipped `hero_skills.json` 이 다르다 - Nova cooldown 7.0 vs JSON 5.0, Dash dashLength 7.0 vs 3.0 / coneHalfAngle 35 vs 180 / knockback 2.0 vs 1.0 / centroidRadius 8.0 vs 5.0, Orbit damage 15 vs 30. **G0 는 JSON(shipped) 값으로 측정한다.** 문서 갱신은 별도 소작업(본 기획서 범위 밖, 사용자에게 보고) |

> 문서 정정은 gameplay-programmer 가 아니라 메인이 `[docs]` 소작업으로 처리한다(코드 변경 0).

### 0.3 G0 - 밸런스 앵커 검증 게이트 (qa-simulator 용)

**배경**: 스테이지 1 평균 사망(영웅 처치) 시각 베이스라인 **76.04초**(`village-meta-hub.md` §3.3). 컨셉서 §8 목표 2~4분(120~240초)에 미달 -> 앵커 교정 필요. 또한 `hero-stage-variant.md` §2.3 이 G0 를 선행 게이트로 정의했으나 QA 하네스 BLOCKED 로 미실행.

**하네스 요구사항** (qa-simulator 구축/복구 - 메뉴 `Lair/Tests/Run PlayMode Simulation`)

| 항목 | 사양 |
|---|---|
| 실행 | 헤드리스 PlayMode 배치, 배속 최대, 시드 고정(런 i 의 시드 = i) |
| 대상 | 기사 영웅, 스테이지 1(배수 1.0), **메타 레벨 0**(상점 0/영주 Lv1), 현행 `balance_config.json` + shipped `hero_skills.json` |
| 봇 프로필 | 5종 x 100런 = 500런: Tank 축 우선 / Dps 축 우선 / Debuff 축 우선 / Swarm 축 우선 / 랜덤 픽. 우선 축 카드가 3택에 있으면 그 카드, 없으면 랜덤 |
| 기록 | 런별: 결과(승/시간초과), 영웅 처치 시각, 패시브·액티브 픽 수, 영웅 HP 구간 통과 시각(90%~10%), 필드 몬스터 평균 수 |

**통과 기준 (모두 만족)**

| # | 지표 | 통과 기준 | 근거 |
|---|---|---|---|
| G0-1 | 500런 평균 처치 시각 **T1** | **120 <= T1 <= 140 초** | 컨셉 §8 창(120~240)의 하단. 상단 근거는 §0.4(상한 도출식): T1 이 140 을 넘으면 후반 챕터 보스를 상점 만렙으로도 시간 내 처치 불가 |
| G0-2 | 승률(300초 내 처치) | 80% 이상 98% 이하 | 첫 스테이지는 대부분 클리어 가능, 단 100% 면 긴장 없음 |
| G0-3 | 프로필별 평균 처치 시각 | 각 프로필이 T1 +-25초 이내 | 4축 빌드 균형(특정 축이 스테이지 1 을 지배하지 않음) |
| G0-4 | 처치 시각 하위 10%(p10) | 75초 이상, 최소값 30초 이상 | 컨셉 §8 실패 기준(30초 안 깎임) 절대 가드 |
| G0-5 | 평균 패시브 픽 수 / 평균 액티브 픽 수 | 패시브 7.0 이상 / 액티브 1.8~2.3 (30·90초 도달) | T1 120~140 에서 5회 트리거의 기대 노출 확인 |
| G0-6 | 95% 신뢰구간 반폭(T1) | 6초 이하 | 500런 표본(표준편차 약 60초 가정 시 약 5.3초) |

**실패 시 교정 절차 (단일 레버 = 영웅 HP 앵커 `balance_config.json` `hero.hp`)**

1. T1 이 창 밖이면 `hero.hp_new = hero.hp_old x 130 / T1` 로 갱신. 1회 변경폭은 x0.6 ~ x1.8 로 제한. (현행 76.04초 기준 예상 6839 부근, 시간-HP 관계는 비선형(시간이 길수록 필드 누적)이라 **반복 측정 필수**.)
2. 갱신 후 G0 재측정. **최대 3회 반복.** 3회 후에도 미수렴이면 사용자에게 에스컬레이션(영웅 Power·몬스터 스포너 주기 등 다른 레버 검토).
3. G0 통과 후 확정된 T1(측정값)을 `Stage.json` 도출식(§0.4) 입력으로 확정하고, §4 의 `hpMultiplier` 를 재산출한다. **Power 는 G0 에서 건드리지 않는다**(보드 붕괴 리스크, `hero-stage-variant.md` §2.2).
4. 영웅 스킬 페이즈 게이트(85/65/45%)는 HP 분수라 앵커 변화에 불변.

### 0.4 G0 연동 - 시간 기반 배수 도출식과 HP 배수 상한 재산정

**발견된 문제**: 플랜의 "HP 배수 총 상한 약 x3.5" 와 현행 Ch1 배수(x1.0 -> x2.3)는 T1=76초 전제에서 나온 값이다(`2.3 x 76 = 175초`). G0 가 T1 을 120~140초로 올리면 같은 배수가 x1.7 배 길어져 후반이 **300초 안에 처치 불가능**해진다(예: Ch1 보스 2.3 x 130 = 299초). 배수는 G0 와 **분리해서 확정할 수 없다.**

**도출식 (단일 진실 규칙)**

```
스테이지 N 의 기대 처치 시간  =  T1 x eqMultiplier(N) / M_ref(N)
eqMultiplier(N)  =  archetype.hpScale x stage.hpMultiplier x ruleFactor(N)
stage.hpMultiplier(N) = round2( targetClearSec(N) x M_ref(N) / T1 / (archetype.hpScale x ruleFactor(N)) )
```

- `T1` = G0 확정 앵커 시간(본 문서 수치는 **T1 = 130초** 기준, §4).
- `M_ref(N)` = 플레이어 보조 계수(상점 만렙 시 환산 압력 +35% 한도, `village-meta-hub.md` §3.3 상한 ①; 해당 스테이지 도전 시점의 상점 진행도 가정). 값: Ch1 S1~S5 = 1.00 / 1.02 / 1.04 / 1.06 / 1.07, Ch2 S1~S5 = 1.08 / 1.10 / 1.12 / 1.14 / 1.16 (Ch2 보스 1.16 = 상점 최대 효과 0.35 의 약 46% 소진 = 약 22런 시점 가정, 소울 만렙 약 49런 기준).
- `ruleFactor(N)` = 스테이지 규칙이 영웅 유효 체력에 더하는 환산 계수(§4.1 산식 명시). 규칙이 없으면 1.00.
- 모든 계수는 **모델 추정치**이며 qa-simulator 게이트 G1/G3/G4 로 검증, 어긋나면 위 도출식으로 재산출한다.

**HP 배수 상한 재산정**

실현 가능 상한 `C(T1) = 270 x M_max / T1` (270 = 보스 목표 시간 상단, `M_max = 1.35` = 상점 만렙 한도).

| T1 (초) | 76.04 (현행) | 104 | 120 | 130 | 140 | 150 | 180 | 240 |
|---|---|---|---|---|---|---|---|---|
| 상한 C (eq. 배수) | 4.80 | 3.50 | 3.04 | **2.80** | 2.60 | 2.43 | 2.03 | 1.52 |

검산: 270 x 1.35 = 364.5 -> 364.5/130 = 2.80, 364.5/76.04 = 4.79, 364.5/104 = 3.50.

- 플랜의 "약 x3.5" 는 **T1 <= 104 일 때만 성립**한다. G0 창(T1 120~140)에서는 **상한 2.60~3.04**, 기준값 T1=130 에서 **2.80**.
- **결정(사용자 승인 필요 D-3)**: 전 챕터 `eqMultiplier` 상한을 "x3.5 고정" 대신 **`C(T1)` (T1=130 -> 2.80)** 로 한다. 챕터별 참고 상한(T1=130): Ch1 보스 1.98 / Ch2 보스 2.28 / Ch3 <= 2.45 / Ch4 <= 2.60 / Ch5 <= 2.80 (Ch3~5 는 후속 승인).
- 톱니형 곡선: Ch1 보스 1.98 -> Ch2 S1 1.25(37% 하강) -> Ch2 보스 2.28. 챕터 내 단조 증가, 챕터 경계에서 하강.

**대안 비교**

| 안 | 내용 | 평가 |
|---|---|---|
| **A. 앵커 T1=130 고정 + 시간 기반 도출 (권장)** | 본 문서 방식. Ch1 배수도 재산출(1.00/1.10/1.36/1.46/1.92) | 컨셉 §8 창 하단 준수 + 전 챕터 시간 예산이 닫힘. Ch1 기존 배수(1.25/1.55/1.90/2.30)가 바뀜 -> 승인 필요(D-2) |
| B. 앵커 HP 만 교정, 배수는 기존 유지 | T1 130 + Ch1 보스 2.3 | Ch1 보스 기대 시간 130 x 2.3 / 1.07 = 279초 -> 보스 밴드(210~270) 초과, 후반 챕터 처치 불가 |
| C. 컨셉 창을 하향(예: 1:15~2:15) 개정 | 현행 76초 부근 유지 | 5분 런 중 액티브 픽 1~2회로 끝나 "5분 로그라이크" 체감 훼손. 컨셉 변경 필요 |

### 0.5 CLAUDE.md §8 챕터 구조 명시 승격 문안 (사용자 승인 후 메인이 반영)

CLAUDE.md §8 "서버 연동 허용" 항목 앞에 다음 항목을 추가한다.

> - **챕터 구조 허용 (2026-10-03 v0.3 승격) - 단 1단계(Ch1 + Ch2)에 한정** - 5챕터 x 5스테이지(최대 25) 구조로 확장하며, 허용 범위는 ① 챕터/스테이지 JSON 데이터화와 진행 저장 일반화(선형 스테이지 번호) ② 스테이지 규칙 엔진(`IStageRule`) 및 규칙 6종 ③ 챕터별 영웅 직업(궁수) 신규 영웅 아트·스킬·AI ④ 챕터 월드맵 팝업(마을 HUD 안, `HeroSelectPopup` 확장). **Ch3~Ch5, ★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지(월드맵은 팝업).

또한 컨셉서 §11 범위 표에 "챕터 구조(1단계: Ch1+Ch2) - v0.3 승격 2026-10-03" 한 줄을 추가한다. §1 문장에는 §0.2 정정만 적용한다.

---

## 1. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 수치·콘텐츠 = JSON(`Assets/_Lair/Data/Json/`, 파일명 = Enum 값명). `UnityEngine.Object` 참조(초상화·FX) = SO 유지. sentinel(-1/0/null) 없음 - 선택적 관계는 별도 JSON, 존재하는 row 만 기록.

### 1.1 엔티티 / 관계 트리

```
Chapter (엔티티)  ChapterRow            <- 챕터 하나
 |
 +-- Stage (엔티티, 복합키 chapterId + stageIndex)  StageRow     <- 챕터 소속 스테이지 5개
 |     |
 |     +-- Stage_Rule (선택적 관계, 복합키 chapterId + stageIndex + ruleIndex)   Stage_RuleRow
 |           |                                  <- 규칙 있는 스테이지의 row 만 존재
 |           +-- Stage_Rule_Param (필수 하위, 복합키 +paramKey)   Stage_Rule_ParamRow
 |           |                                  <- 규칙마다 필요한 수치 (규칙 종류별 필수 키는 §3.4)
 |           +-- Stage_Rule_Trigger (선택적 관계, 복합키 chapterId + stageIndex + ruleIndex)  Stage_Rule_TriggerRow
 |                                              <- 영웅 HP 구간에서 발동하는 규칙(보스 페이즈)만 존재
 |
 +-- (Chapter.heroId) --> HeroArchetype (엔티티)  HeroArchetypeRow    <- 영웅 직업
                           |
                           +-- HeroArchetype_Kite (선택적 관계)  HeroArchetype_KiteRow
                           |                       <- aiType=Kiter 인 영웅만 존재
                           +-- (스킬 로드아웃) hero_skills.json 의 loadout row (heroId 추가, §1.4)

SO (Object 참조): HeroStageVariantConfig  <- 스테이지별 초상/발광/스케일/티어 (키 chapterId + stageIndex)
```

### 1.2 필드 표

**ChapterRow** - `Chapter.json` (EData `Chapter`). 핵심 엔티티: "챕터 하나".

| 필드 | 타입 | 키/관계 | 설명 · 예 |
|---|---|---|---|
| `chapterId` | int | PK | 1, 2 (연속, 1부터) |
| `nameKey` | int | 필수 1:1 -> Strings_Ko id | 1001 "지하 묘지", 1002 "어둠의 숲" |
| `themeTextKey` | int | 필수 1:1 -> Strings_Ko id | 1011 "기사단 침공", 1012 "숲속 정찰대" |
| `heroId` | string | 필수 1:1 -> `HeroArchetypeRow.heroId` | "Knight", "Archer" (= `EHero` 값명) |
| `backdropKey` | string | 필수 1:1 (에셋 키) | "Battle_Backdrop", "Battle_Backdrop_DarkForest" (= `EBackdrop` 값명) |

> 챕터당 스테이지 수는 필드로 두지 않는다(5 고정, `StageProgress.StagesPerChapter`, 로드 시 챕터당 `StageRow` 가 정확히 5개 아니면 로드 오류). 보스 여부도 필드로 두지 않는다(`stageIndex == 5` 이 보스, 모순 데이터 방지).

**StageRow** - `Stage.json` (EData `Stage`). 핵심 엔티티: "챕터 안의 스테이지 하나". 복합키: 한 챕터 안의 5개 중 하나이므로 `chapterId + stageIndex` (Rule 02 §11-1-4).

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `chapterId` | int | 복합 PK(1) -> `ChapterRow` | |
| `stageIndex` | int | 복합 PK(2) | 1~5 |
| `nameKey` | int | 필수 1:1 -> Strings_Ko id | 스테이지명 (1101~1105, 1111~1115) |
| `hpMultiplier` | float | | 영웅 HP 스테이지 배수 (§4 도출식) |
| `powerMultiplier` | float | | 영웅 공격력 스테이지 배수 |
| `targetClearSec` | float | | 기준 메타·기준 빌드의 목표 평균 처치 시간(초). QA 밴드 중심값이자 `hpMultiplier` 도출 입력 |

**Stage_RuleRow** - `Stage_Rule.json` (EData `Stage_Rule`). 핵심: "스테이지에 부착된 규칙 하나". 선택적 관계(규칙 없는 스테이지는 row 없음). 복합키: 한 스테이지에 복수 규칙 가능.

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `chapterId` | int | 복합 PK(1) -> `StageRow` | |
| `stageIndex` | int | 복합 PK(2) -> `StageRow` | |
| `ruleIndex` | int | 복합 PK(3) | 같은 스테이지 내 순번, 1부터 |
| `ruleId` | string | 필수 -> `EStageRule` 값명 | "HolySpring" 등 6종 |

**Stage_Rule_ParamRow** - `Stage_Rule_Param.json` (EData `Stage_Rule_Param`). 핵심: "규칙 하나의 수치 하나". 규칙 종류마다 파라미터 집합이 달라 단일 row 에 필드를 나열하면 sentinel 이 생기므로 key-value 로 분리한다.

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `chapterId` | int | 복합 PK(1) -> `Stage_RuleRow` | |
| `stageIndex` | int | 복합 PK(2) | |
| `ruleIndex` | int | 복합 PK(3) | |
| `paramKey` | string | 복합 PK(4) | 예 "radius" (규칙별 필수 키 §3.4) |
| `value` | float | | 수치. 종족 지정은 `EMonster` 정수값(Wisp=0 ... Phantom=5, 순서 불변) |

**Stage_Rule_TriggerRow** - `Stage_Rule_Trigger.json` (EData `Stage_Rule_Trigger`). 선택적 관계: HP 구간 발동 규칙(보스 페이즈)만 row 존재. 없으면 "스테이지 시작부터 상시 활성".

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `chapterId` | int | 복합 PK(1) -> `Stage_RuleRow` | |
| `stageIndex` | int | 복합 PK(2) | |
| `ruleIndex` | int | 복합 PK(3) | |
| `hpFraction` | float | | 영웅 HP 비율이 이 값 이하로 처음 내려가는 순간 1회 활성 |

**HeroArchetypeRow** - `HeroArchetype.json` (EData `HeroArchetype`). 핵심: "영웅 직업 하나". `hp`/`power` 앵커는 `balance_config.json` `hero` 블록에 유지(G0 교정 대상), 직업은 배율만 가진다.

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `heroId` | string | PK | `EHero` 값명: "Knight", "Archer" |
| `nameKey` | int | 필수 1:1 -> Strings_Ko id | 1021 "기사", 1022 "궁수" |
| `hpScale` | float | | 앵커 HP 대비 배율 (Knight 1.00 / Archer 0.80) |
| `powerScale` | float | | 앵커 Power 대비 배율 (Knight 1.00 / Archer 0.80) |
| `range` | float | | 공격 사거리 (Knight 1.5 / Archer 6.0) |
| `cooldown` | float | | 공격 쿨다운(초) (둘 다 1.0) |
| `moveSpeed` | float | | 이동속도 (Knight 3.0 / Archer 3.2) |
| `aiType` | string | 필수 -> `EHeroAi` 값명 | "Melee", "Kiter" |

> 이관 규칙: `balance_config.json` `hero` 의 `range`/`cooldown`/`moveSpeed` 는 `HeroArchetypeRow` 로 이관하고 `balance_config.json` 에서 제거한다(이중 진실 방지). `hero.hp`/`hero.power` 는 **앵커로 유지**.

**HeroArchetype_KiteRow** - `HeroArchetype_Kite.json` (EData `HeroArchetype_Kite`). 선택적 관계: `aiType = Kiter` 인 영웅만 row 존재.

| 필드 | 타입 | 설명 | Archer 값 |
|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow` | "Archer" |
| `triggerDist` | float | 가장 가까운 몬스터가 이 거리 미만이면 후퇴 진입 | 3.0 |
| `resumeDist` | float | 이 거리 이상이면 후퇴 해제 (히스테리시스) | 4.5 |
| `threatRadius` | float | 후퇴 방향 계산용 위협 중심 집계 반경 | 4.0 |
| `arenaSoftRadius` | float | 원점 기준 이 반경을 넘으면 후퇴 방향에 원점 방향 가산 | 7.0 |
| `corneredCheckSec` | float | 갇힘 판정 관찰 구간(초) | 0.5 |
| `corneredMinMove` | float | 관찰 구간 동안 이 거리 미만 이동하면 갇힘 | 0.3 |
| `minDwellSec` | float | 후퇴/교전 상태 전이 후 최소 유지(진동 방지) | 0.4 |

**SO 변경 - `HeroStageVariantConfig`** (외형 참조, Object 포함이라 SO 유지)

| 변경 | 내용 |
|---|---|
| 엔트리 키 추가 | 각 엔트리에 `int ChapterId`, `int StageIndex` 필드 추가(10엔트리: 1-1~1-5, 2-1~2-5). `GetStage(int stage1Based)` 호출부는 선형 번호 -> (챕터, 인덱스) 변환 후 조회 |
| 필드 제거 | `HpMultiplier`, `PowerMultiplier` 를 SO 에서 **제거**(-> `StageRow` 로 이전, 이중 진실 방지) |
| 유지 | `Portrait`, `UseEmission`, `EmissionColor`, `EmissionIntensity`, `ScaleMultiplier`, `Tier` |
| Ch2 값 | `Tier` 0,1,2,3,4 (Ch1 과 동일 규약, 궁수는 `MonsterTierOverlay` 로 강화 표현), `ScaleMultiplier` S1~S4 = 1.00, S5 = 1.25(보스 확대) |

### 1.3 초기 데이터 값 요약

- `Chapter.json`: 2 row (아래 표).
- `Stage.json`: 10 row - 값은 §4.3(Ch1) / §4.4(Ch2) 표.
- `Stage_Rule.json`: Ch1 4 row(S2·S3·S4·S5) + Ch2 6 row(S2·S3·S4 x2·S5 x2) = 10 row - §4.1.
- `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`: §3.4 / §4.1 에서 row 단위로 확정.
- `HeroArchetype.json`: 2 row (Knight, Archer). `HeroArchetype_Kite.json`: 1 row (Archer).

| chapterId | nameKey | themeTextKey | heroId | backdropKey |
|---|---|---|---|---|
| 1 | 1001 | 1011 | Knight | Battle_Backdrop |
| 2 | 1002 | 1012 | Archer | Battle_Backdrop_DarkForest |

### 1.4 영웅 스킬 로드아웃 확장 (기존 `hero_skills.json`)

- `skills[]`: 기존 3종 유지 + 궁수 스킬 3종 추가(§5.4).
- `loadout[]` row 에 `heroId`(string) 필드를 추가해 복합키 `heroId + hpFraction` 으로 만든다. 기존 3 row 는 `"heroId": "Knight"` 로 이전. (관계 데이터 명명 규칙 §11-1-5 에 따르면 `HeroArchetype_Skill` 이 맞으나, **`Lair > JSON Sync` 창과 양방향 동기화 호환을 위해 파일명 `hero_skills.json` 은 유지** - 예외로 명시, 승인 항목 D-5.)
- 로드아웃 SO: `HeroSkillLoadout`(Knight, 기존 EData) + `HeroSkillLoadout_Archer`(신규 EData, `Art/Skills/`). 스킬 SO 는 Object 참조라 SO 유지.

### 1.5 Strings_Ko id 배정 (`Strings_Ko.json`, 현행 최대 id 300)

| 범위 | 용도 | 값 |
|---|---|---|
| 1001~1002 | 챕터명 | 1001 지하 묘지 / 1002 어둠의 숲 |
| 1011~1012 | 챕터 테마 | 1011 기사단 침공 / 1012 숲속 정찰대 |
| 1021~1022 | 영웅 직업명 | 1021 기사 / 1022 궁수 |
| 1101~1105 | Ch1 스테이지명 | 납골당 입구 / 성수의 회랑 / 안개 낀 지하수로 / 봉인된 지하 둥지 / 기사단장의 안치소 |
| 1111~1115 | Ch2 스테이지명 | 숲의 입구 / 속삭이는 오솔길 / 안개 낀 늪 / 수호 샘터 / 사냥꾼 우두머리의 야영지 |
| 1201~1206 | 규칙명 | 1201 성수 샘 / 1202 짙은 안개 / 1203 봉인된 둥지 / 1204 수호 결계 / 1205 화살비 / 1206 사냥꾼의 덫 |
| 1221~1226 | 규칙 설명(포맷 인자 {0}~) | §3.4 문구 |
| 1301~1318 | UI 라벨 | §6.5 |

---

## 2. 진행 / 저장

### 2.1 `StageProgress` 일반화 (순수 헬퍼 유지, Unity 비의존)

대안 3개:

| 안 | 내용 | 평가 |
|---|---|---|
| **A. 선형 번호 유지 (권장)** | `ClearedStage` / `SelectedStage` 를 **선형 스테이지 번호**(`(chapterId-1) x 5 + stageIndex`, 0~출시 챕터수 x 5)로 계속 저장. (챕터, 인덱스) 는 변환 함수로 도출 | 기존 해금 규칙 `IsUnlocked(stage, cleared) = stage <= cleared + 1` 이 **챕터 경계를 그대로 통과**(1장 보스 클리어 = cleared 5 -> 선형 6 = 2장 S1 해금). `StageRecords.Stage`·랭킹 문서 ID 가 Ch1 구간에서 **무변환 호환**. 데이터 이전 코드 불필요 |
| B. 챕터별 진행 리스트 (`ChapterProgress{chapterId, clearedStageIndex}`) | 플랜 원안 | 가변 스테이지 수에 강건하나, 마이그레이션·`CopyFrom`·`StageRecords`·랭킹 키 전부 복합키 변환 필요. 가변 스테이지 수가 필요 없는 현 단계에서 과설계 |
| C. A 에 챕터별 ★ 슬롯 예약 필드 선반영 | | YAGNI (★ 는 2단계) |

**결정: A.** 단 챕터당 5스테이지 고정을 전제한다(로드맵의 "5 고정 vs 가변" 미결을 **5 고정으로 확정**: 근거 - 챕터 사이클 길이가 학습 단위(1~4 규칙 학습, 5 보스)로 설계됐고, 가변을 허용하면 선형 번호가 챕터 추가 시 깨진다).

동작 사양 (시그니처는 gameplay-programmer 판단):

| 기능 | 사양 |
|---|---|
| 상수 | `StagesPerChapter = 5` (고정). `MaxStage` 는 const 5 에서 **출시 챕터 수 x 5**(현재 10, 데이터 `Chapter.json` row 수 기반 런타임 값)로 변경. `StageLeaderboard.MaxStage` 도 동일 값 참조(const 이므로 읽기 전용 프로퍼티로) |
| 변환 | `ToLinear(chapterId, stageIndex)`, `ChapterOf(linear)`, `IndexOf(linear)` (예: 7 -> 챕터 2, 인덱스 2) |
| 해금 | `IsUnlocked(stage, cleared)` 불변. `IsChapterUnlocked(chapterId, cleared) = IsUnlocked(ToLinear(chapterId, 1), cleared)` 신설 |
| 클리어 | `ResolveClearedStage(cleared, justCleared)` = `Min(MaxStage, Max(cleared, justCleared))` (MaxStage 가 런타임 값) |
| 진입 위치 | `MaxPlayableStage(cleared)` = `Max(1, Min(MaxStage, cleared + 1))` |
| 보스 | `IsBossStage(stageIndex) = (stageIndex == StagesPerChapter)` |
| 스탯 | `ScaleStat(baseValue, multiplier)` round-half-up 불변. 영웅 스탯 = `ScaleStat(anchor, archetype.scale x stage.multiplier)` **1회 반올림**(중간 반올림 금지) |

### 2.2 `MetaProfile` Version 4 -> 5

- 스키마 필드 추가/삭제 **없음.** `Version` 을 5 로 올리는 이유는 **의미 변경**(ClearedStage/SelectedStage 범위 0~5 -> 0~10)과 클라우드 `schemaVersion` 구분이다.
- **마이그레이션 규칙 (v4 로드 시, `MetaProfileStore` 로드 경로)**:

| 단계 | 규칙 |
|---|---|
| 1 | `Version < 5` 이면 `Version = 5` |
| 2 | `ClearedStage = Clamp(ClearedStage, 0, MaxStage)` (v4 값은 0~5 라 불변 = Ch1 구간 동일 의미. `ClearedStage == 5` 였던 유저는 2장 S1 해금 상태가 됨) |
| 3 | `SelectedStage = Clamp(SelectedStage, 1, MaxPlayableStage(ClearedStage))` |
| 4 | `StageRecords` 에서 `Stage < 1` 또는 `Stage > MaxStage` 인 엔트리 제거 |
| 5 | `SelectedHero` = 선택 스테이지가 속한 챕터의 `heroId` 로 재설정(표시 전용 필드) |

- 신규 필드가 없으므로 `CopyFrom` 변경 없음. 단 **클라우드 복원 후**(`CopyFrom` 호출 직후) 위 마이그레이션을 **동일 함수로 재적용**(v4 문서를 v5 클라이언트가 복원하는 경우).
- 충돌 UX(배지·복원 권유)는 **변경 없음**: 전체 MetaProfile 단위 동기, `serverVersion` 비교 로직 불변.
- 알려진 제약: v4 클라이언트가 v5 문서(ClearedStage > 5)를 복원하는 경우는 방지 수단이 없다(구버전 코드). 출시 시 v5 클라이언트를 동시에 배포한다.
- 소울 경제: 챕터별 소울 배율·최초 클리어 보너스는 **2단계(반복 레이어) 소관**이며 이번 범위에서 `SoulRewardCalculator` 는 변경하지 않는다(Ch2 는 Ch1 과 같은 보상식).

### 2.3 랭킹 키

- 문서 ID `{stage}_{uid}` 에서 `stage` = **선형 스테이지 번호**(1~10). Ch1 구간(1~5)은 **기존 문서 그대로 유효**, 신규 6~10 은 추가만 된다. 필드 `hero` = 해당 챕터 `heroId`.
- `StageLeaderboard.IsValidStage` = `1 <= stage <= MaxStage`(10). `RankingPopup`: 스테이지 선택 컨트롤이 1~10 을 순회(라벨 `{챕터}-{인덱스}`, 예 "2-3"); 기존 5칸 가정이 있으면 확장. `RecordsPopup`: 스테이지 행 5 -> 10(풀링 스크롤이라 행 추가만, 잠금 스테이지 행은 "-" 표시).
- **사용자 작업 안내 (이 레포 범위 밖, Firebase 콘솔)**: Firestore 보안 규칙에 `stage` 허용 범위가 1~5 로 제한돼 있다면 1~10 으로 확장하고, 복합 인덱스 `(stage, clearTimeMs)` 는 그대로 유효함을 확인한다. 규칙 변경 전에는 6~10 제출이 서버에서 거부될 수 있으며, 클라이언트는 기존 제출 실패 처리(조용한 실패)를 따른다.

---

## 3. 스테이지 규칙 엔진

### 3.1 개념

- `IStageRule`: 스테이지에 부착되는 전투 규칙 하나. 생명주기 = **활성화 -> 매 프레임 갱신 -> 비활성화**. 구현체당 `EStageRule` 값 1개.
- **스테이지 규칙 러너(`StageRuleRunner`)** 가 전투 시작 시 `Stage_Rule*` JSON 으로 규칙을 생성한다. `Stage_Rule_TriggerRow` 가 있는 규칙은 영웅 HP 비율이 `hpFraction` 이하로 **처음** 내려가는 순간 1회 활성화, 없으면 몬스터 스포너 바인딩(`BindSpawners`) 직후 활성화.
- 갱신 시계는 `BattleClock`(카드 픽 일시정지 중 멈춤). 전투 종료 시 전원 비활성화.
- 규칙이 필요로 하는 전투 기능(영웅 회복, 영웅 받는 피해 배율, 몬스터 사거리 배율, 스포너 잠금, 구형 범위 피해, 몬스터 감속, 몬스터 밀집 지점 조회, 알림 표시)은 `IStageRule` 이 구체 클래스에 의존하지 않도록 **컨텍스트 인터페이스로 주입**한다(`IHeroSkillContext` 의 범위 피해 기능 재사용 가능). 인터페이스 세부 설계는 gameplay-programmer 판단.
- 파라미터는 `Stage_Rule_ParamRow` 의 key-value. **규칙별 필수 키(§3.4)가 하나라도 없거나 추가 키가 있으면 로드 오류**(기본값 보정 금지 - 데이터 오타 조기 발견).

### 3.2 HP 임계 중복 방지 규칙 (페이싱 충돌 회피)

기존 규칙(`hero-skills.md` §1): 영웅 스킬 게이트(0.85/0.65/0.45)는 패시브 카드 임계(0.9~0.1, 0.1 단위)와 **0.05 오프셋**으로 일시정지 연출 충돌을 피한다. 보스 페이즈 트리거 `hpFraction` 도 이를 따른다 - **스킬 게이트(0.85/0.65/0.45)와 패시브 임계(0.9~0.1) 어느 것과도 같지 않은 값만 사용**: 본 기획서 사용값 0.75 / 0.55 / 0.35 (패시브와 0.05, 스킬 게이트와 0.10 이격). 봉인 해제 임계(`unlockHpFraction`: 0.70, 0.60)는 일시정지 연출이 없으므로 패시브 임계와 같아도 무방하나, 같은 순간 카드 팝업이 겹치지 않도록 **0.60/0.70 은 패시브 임계와 일치** -> 해제 효과(스폰 버스트·알림)는 카드 선택 팝업이 닫힌 뒤 처리한다(엣지 케이스 E-4).

### 3.3 규칙 6종 요약

| ruleId (`EStageRule`) | 분류 | 효과 | 사용처 |
|---|---|---|---|
| `HolySpring` 성수 샘 | 상시 | 주기적으로 열리는 회복 지대 - 영웅이 안에 있으면 회복 | Ch1 S2, Ch2 S4 |
| `DenseFog` 짙은 안개 | 상시 | 몬스터 공격 사거리 배율 하향 | Ch1 S3, Ch2 S3·S4 |
| `SealedNest` 봉인된 둥지 | 상시(해제 조건) | 특정 종족 스포너 1개 봉인, 영웅 HP 구간에서 해제 | Ch1 S4, Ch2 S2 |
| `BossBulwark` 수호 결계 | 보스 페이즈 | 영웅 받는 피해 감소(1회) | Ch1 S5 (트리거 0.55) |
| `ArrowRain` 화살비 | 보스 페이즈 | 몬스터 밀집 지점에 범위 연속 피해 반복 | Ch2 S5 (트리거 0.75) |
| `HunterTraps` 사냥꾼의 덫 | 보스 페이즈 | 영웅 주변에 덫 설치 - 몬스터 감속·지속 피해 | Ch2 S5 (트리거 0.35) |

### 3.4 규칙별 사양 · 필수 파라미터 · 수치

**HolySpring 성수 샘**

| paramKey | 값 (Ch1 S2) | 값 (Ch2 S4) | 설명 |
|---|---|---|---|
| `centerX` | 0.0 | 0.0 | 샘 중심 X (전장 원점 = 영웅 구역 중심) |
| `centerZ` | 0.0 | 0.0 | 샘 중심 Z |
| `radius` | 3.0 | 3.0 | 회복 반경 (기사 중앙 끌림 deadzone 3.0 과 동일) |
| `healPercentPerSec` | 0.4 | 0.5 | 활성 중 영웅이 반경 안일 때 초당 회복 (최대 HP 의 %) |
| `activeSec` | 8 | 8 | 샘이 열려 있는 시간 |
| `periodSec` | 40 | 40 | 개폐 주기 |
| `firstActiveSec` | 20 | 20 | 첫 개방 시각 (BattleClock 기준) |

- 개방 구간 = `[first + k x period, first + k x period + active)` -> 20·60·100·140·180·220·260 초에 시작하는 7회(마지막 종료 268초 <= 300 초).
- 회복은 0.5초 틱, 소수 누적(carry) 후 정수 반영, 최대 HP 초과 금지.
- 비주얼: 휴면 시 샘 윤곽(저알파 링), 활성 시 청백 발광 링+물방울 파티클, 개방 2초 전 윤곽 점멸(예고). 영웅이 안에 있으면 회복 수치 팝업(`DamagePopup` 재사용, 초록).
- 회복 상한: 회복량은 **영웅이 지금까지 기록한 최저 HP 비율**(패시브/스킬/보스 트리거 판정 기준)에 영향을 주지 않는다 - HP 임계 판정은 "최저 HP 비율" 단조 감소 값으로 하며 회복으로 임계를 **재무장하지 않는다**(엣지 E-1).
- 설명 문구 `StageRule_1221`: "{0}초마다 {1}초간 샘이 열려, 샘 안의 영웅이 초당 최대 HP의 {2}% 회복".

**DenseFog 짙은 안개**

| paramKey | 값 | 설명 |
|---|---|---|
| `monsterRangeMul` | 0.7 | 몬스터 공격 사거리 배율 (카드 `HexRangeBoost` 등 모든 사거리 보정 **이후** 마지막 곱) |
| `minRange` | 0.8 | 배율 적용 후 하한 (근접 몬스터 Range 1.0 -> 0.8, Wraith 1.3 -> 0.91, Hex 5.0 -> 3.5) |

- 하한 0.8 근거: 근접 몬스터가 영웅 충돌 반경 안쪽에서 정지해 공격 불가가 되는 현상 방지. **구현 시 영웅 충돌 반경과의 관계를 확인**(0.8 이 영웅 충돌 거리보다 작으면 하한을 충돌 거리 + 0.1 로 상향, 변경 시 기획자에게 보고).
- 비주얼: 전장 위 회녹색 안개 오버레이(알파 0.18, 천천히 흐름), HUD·카드 UI 위에는 덮지 않는다. 영웅·몬스터 가독성 유지(오버레이는 캐릭터 레이어 아래).
- 설명: `StageRule_1222` "몬스터 공격 사거리 x{0} (최소 {1})".

**SealedNest 봉인된 둥지**

| paramKey | Ch1 S4 | Ch2 S2 | 설명 |
|---|---|---|---|
| `lockedSpecies` | 1 (Wraith) | 5 (Phantom) | 봉인할 종족 (`EMonster` 정수값) |
| `unlockHpFraction` | 0.60 | 0.70 | 영웅 HP 비율이 이 값 이하가 되면 해제 |

- 봉인 중: 해당 종족 스포너의 **주기 스폰 타이머가 진행하지 않고 스폰하지 않는다.** 카드의 **즉시 소환 효과(SpawnWraith 등)는 봉인과 무관하게 작동**한다(플레이어 대응책으로 남김 - 대안 "즉시 소환도 차단"은 카드 선택 낭비 불만을 만든다고 판단해 기각).
- 해제 순간: 스포너 타이머 0 으로 리셋(**즉시 1회 스폰 버스트**) + 알림 "{종족명} 둥지의 봉인이 풀렸다!" + 해제 연출(봉인 사슬 파괴 파티클).
- UI: 스포너 상태 UI(`BuildPanel`/`BuildModalPopup` 의 스포너 셀)에 "봉인 - 영웅 HP {x}% 이하 해제" 표기, 제단 비주얼은 회색 조 + 사슬.
- 설명: `StageRule_1223` "{0} 둥지가 봉인되어 있다 - 영웅 HP {1}% 이하에서 해제".

**BossBulwark 수호 결계** (트리거 row: Ch1 S5 `hpFraction` 0.55)

| paramKey | 값 | 설명 |
|---|---|---|
| `damageTakenMul` | 0.6 | 영웅이 받는 모든 피해(지속 피해 포함)에 곱 |
| `durationSec` | 15 | 지속 시간, 1회 발동 |

- 연출: 발동 시 영웅 주위 금빛 결계 링 + 배너 "수호 결계". 종료 1.5초 전 결계 점멸.
- 카드 상호작용: 결계 중 저주 카드(공포·둔화 등)는 정상 적용된다. 피해 감소만 곱해지며 영웅 이동·공격에는 영향 없음.

**ArrowRain 화살비** (트리거 row: Ch2 S5 `hpFraction` 0.75)

| paramKey | 값 | 설명 |
|---|---|---|
| `intervalSec` | 10 | 반복 주기 (활성화 시각부터 시작, 첫 낙하는 1.0초 예고 후) |
| `searchRadius` | 8.0 | 영웅 중심 이 반경 안에서 몬스터가 가장 밀집한 지점 선택 |
| `radius` | 3.5 | 낙하 범위 |
| `tickCount` | 4 | 연속 타격 횟수 |
| `tickIntervalSec` | 0.5 | 타격 간격 |
| `damagePerTick` | 50 | 틱당 피해 (한 몬스터는 틱당 1회) |

- 총 피해 = 50 x 4 = 200 / 대상. 처치 타수: Phantom 30·Plague 50·Hex 60 = 1틱, Reaper 100 = 2틱, Wisp 200 = 4틱, Wraith 500 = 불가(밀집 지점 제압용, 탱커 면역 유지).
- 몬스터가 없으면 낙하하지 않고 다음 주기로 넘긴다. 예고: 붉은 원형 경고(1.0초), 낙하: 화살 다발 FX.
- 밸런스 의도: 보스전에서 영웅(궁수)이 무리를 쓸어내는 압박. 궁수 스킬 `ArrowVolley`(§5.4)와 겹치지 않도록 **주기 10초 vs 스킬 8초**로 어긋나 있으며 qa 게이트 G4 에서 필드 점유율을 확인.

**HunterTraps 사냥꾼의 덫** (트리거 row: Ch2 S5 `hpFraction` 0.35)

| paramKey | 값 | 설명 |
|---|---|---|
| `trapCount` | 6 | 한 번에 설치하는 덫 수 |
| `ringDistance` | 3.5 | 설치 시점 영웅 위치 기준 링 반경 (60° 간격 균등 배치) |
| `trapRadius` | 1.2 | 덫 한 개의 효과 반경 |
| `lifeSec` | 12 | 덫 지속 시간 |
| `rearmSec` | 15 | 재설치 주기 (이전 덫 소멸 후 3초 공백) |
| `monsterSpeedMul` | 0.5 | 덫 안 몬스터 이동속도 배율 (0.6초 갱신형, 이탈 즉시 복구) |
| `damagePerSec` | 10 | 덫 안 몬스터 초당 피해 (0.5초 틱 x 5) |

- 설치 범위가 전장 밖(원점 기준 `arenaSoftRadius` 초과)이면 해당 덫은 원점 방향으로 당겨 설치.
- 감속은 몬스터 이동속도에 곱연산(Plague 둔화 카드와 곱연산 누적 하한은 기존 몬스터 속도 정책 준수).

---

## 4. 챕터 1 소급 규칙 + 챕터 2 스테이지

### 4.1 규칙 배정과 환산 계수 `ruleFactor`

**dps-share 표 (환산 기준)** - 종족별 "공급률 x DPS" 비중(`balance_config.json`: spawnPeriod, power/cooldown)

| 종 | DPS | 스폰주기(s) | DPS/주기 | 비중 |
|---|---|---|---|---|
| Wisp | 5.0 | 9 | 0.556 | 17.0% |
| Wraith | 10.0 | 20 | 0.500 | 15.3% |
| Reaper | 12.0 | 12 | 1.000 | 30.6% |
| Hex | 9.0 | 15 | 0.600 | 18.3% |
| Plague | 2.0 | 10 | 0.200 | 6.1% |
| Phantom | 2.5 (power 2 / cooldown 0.8) | 6 | 0.417 | 12.7% |
| 합 | | | 3.273 | 100% |

검산: 0.556 + 0.500 + 1.000 + 0.600 + 0.200 + 0.417 = 3.273.

**규칙 배정과 ruleFactor 산식**

| 스테이지 | 규칙 | ruleFactor 산식 | 값 |
|---|---|---|---|
| Ch1 S2 | HolySpring(0.4%/s) | 개방 횟수 = floor((targetClearSec 150 - 20) / 40) + 1 = 4회. 회복률 = 4 x (0.4% x 8s = 3.2%) x 영웅 내부 체류율 0.6(기사는 중앙 끌림 deadzone 3.0 에 머묾) = 7.68% -> 1 + 0.0768 | **1.077** |
| Ch1 S3 | DenseFog | Hex 만 실질 영향(근접은 하한 0.8, 영향 미미). 미보정 | **1.000** |
| Ch1 S4 | SealedNest(Wraith, 0.60) | Wraith 비중 15.3% x 봉인 시간 비율 (1 - 0.60) = 40% = 6.1% 압력 감소 -> 1 / (1 - 0.061) | **1.065** |
| Ch1 S5 | BossBulwark | 15초 x 40% 경감 x 후반 피격 가중 1.3 / 목표 240초 = 3.25% | **1.030** |
| Ch2 S2 | SealedNest(Phantom, 0.70) | Phantom 비중 12.7% x 30% = 3.8% -> 1 / (1 - 0.038) | **1.040** |
| Ch2 S3 | DenseFog | 궁수 vs Hex 사거리(6.0 vs 3.5)로 Hex 위협 감소 -> 미보정 | **1.000** |
| Ch2 S4 | HolySpring(0.5%/s) + DenseFog | 개방 횟수 = floor((215 - 20) / 40) + 1 = 5회. 5 x (0.5% x 8 = 4.0%) x 체류율 0.4(궁수는 카이팅으로 샘 밖 이동) = 8.0% -> 1.080. 안개 미보정 | **1.080** |
| Ch2 S5 | ArrowRain + HunterTraps | 규칙 2종 각 약 6% 추정(영웅 측 광역/감속이 몬스터 DPS 를 줄이는 효과, 정량 모델 없음) | **1.120** |

> `ruleFactor` 중 BossBulwark(1.030)와 Ch2 S5(1.120)는 **추정치**다 - 정량 모델이 없어 qa-simulator 게이트(G1/G3/G4)에서 실측 후 §0.4 도출식으로 재산출한다.

**`Stage_Rule.json` / `Stage_Rule_Trigger.json` 전체 row**

| chapterId | stageIndex | ruleIndex | ruleId | 트리거 hpFraction |
|---|---|---|---|---|
| 1 | 2 | 1 | HolySpring | (row 없음 = 상시) |
| 1 | 3 | 1 | DenseFog | (없음) |
| 1 | 4 | 1 | SealedNest | (없음) |
| 1 | 5 | 1 | BossBulwark | 0.55 |
| 2 | 2 | 1 | SealedNest | (없음) |
| 2 | 3 | 1 | DenseFog | (없음) |
| 2 | 4 | 1 | HolySpring | (없음) |
| 2 | 4 | 2 | DenseFog | (없음) |
| 2 | 5 | 1 | ArrowRain | 0.75 |
| 2 | 5 | 2 | HunterTraps | 0.35 |

### 4.2 Ch1 소급 규칙 (기사)

- 챕터 1 은 영웅 기사(근접) 유지. S2~S4 에 "가벼운 규칙 1개", S5 에 보스 페이즈 1개.
- 콘셉트: 묘지 지하에서 점점 기사단이 방어 장치를 가동하는 서사 - S2 성수의 회랑(회복 샘) -> S3 지하수로 안개 -> S4 봉인된 지하 둥지(망령 봉인) -> S5 안치소 수호 결계.

### 4.3 Ch1 스테이지 표 (T1 = 130초 기준 도출)

| 스테이지 | 스테이지명(id) | 규칙 | targetClearSec | M_ref | eq 배수 | ruleFactor | **hpMultiplier** | **powerMultiplier** | 현행 HP/Power 배수(참고) |
|---|---|---|---|---|---|---|---|---|---|
| 1-1 | 납골당 입구 (1101) | 없음 | 130 | 1.00 | 1.00 | 1.000 | **1.00** | **1.00** | 1.00 / 1.00 |
| 1-2 | 성수의 회랑 (1102) | HolySpring | 150 | 1.02 | 1.18 | 1.077 | **1.10** | **1.10** | 1.25 / 1.10 |
| 1-3 | 안개 낀 지하수로 (1103) | DenseFog | 170 | 1.04 | 1.36 | 1.000 | **1.36** | **1.20** | 1.55 / 1.20 |
| 1-4 | 봉인된 지하 둥지 (1104) | SealedNest | 190 | 1.06 | 1.55 | 1.065 | **1.46** | **1.35** | 1.90 / 1.35 |
| 1-5 | 기사단장의 안치소 (1105) | BossBulwark | 240 | 1.07 | 1.98 | 1.030 | **1.92** | **1.50** | 2.30 / 1.50 |

검산 (도출식 `round2(target x M_ref / T1 / ruleFactor)`, T1=130): 1-2: 150 x 1.02 / 130 = 1.177 -> eq 1.18, /1.077 = 1.096 -> 1.10. 1-3: 170 x 1.04 / 130 = 1.360. 1-4: 190 x 1.06 / 130 = 1.549 -> 1.55, /1.065 = 1.455 -> 1.46. 1-5: 240 x 1.07 / 130 = 1.975 -> 1.98, /1.03 = 1.922 -> 1.92. 역검산: 1.46 x 1.065 = 1.555, 1.92 x 1.03 = 1.978.

> Power 배수는 **현행 유지**(`hero-stage-variant.md` §2.1) - Power 는 부(副) 레버. HP 앵커 `A`(G0 확정값) 기준 절대 HP = `A x hpScale x hpMultiplier` (예: A=4000 이면 1-5 = 7680, A 가 G0 후 상향되면 비례 상승).

### 4.4 Ch2 스테이지 표 (궁수 `hpScale` 0.80 / `powerScale` 0.80)

| 스테이지 | 스테이지명(id) | 규칙 | 보스 페이즈 | targetClearSec | M_ref | eq 배수 | ruleFactor | **hpMultiplier** | **powerMultiplier** | `ScaleMultiplier` (SO) |
|---|---|---|---|---|---|---|---|---|---|---|
| 2-1 | 숲의 입구 (1111) | 없음 | - | 150 | 1.08 | 1.25 | 1.000 | **1.56** | **1.10** | 1.00 |
| 2-2 | 속삭이는 오솔길 (1112) | SealedNest(Phantom, 0.70) | - | 175 | 1.10 | 1.48 | 1.040 | **1.78** | **1.20** | 1.00 |
| 2-3 | 안개 낀 늪 (1113) | DenseFog | - | 195 | 1.12 | 1.68 | 1.000 | **2.10** | **1.30** | 1.00 |
| 2-4 | 수호 샘터 (1114) | HolySpring + DenseFog | - | 215 | 1.14 | 1.89 | 1.080 | **2.19** | **1.45** | 1.00 |
| 2-5 | 사냥꾼 우두머리의 야영지 (1115) | (페이즈 2종) | 0.75 화살비 / 0.35 사냥꾼의 덫 | 255 | 1.16 | 2.28 | 1.120 | **2.55** | **1.60** | 1.25 |

검산 (T1=130): 2-1: 150 x 1.08 / 130 = 1.246 -> 1.25, /0.8 = 1.5625 -> 1.56 (역: 0.8 x 1.56 = 1.248). 2-2: 175 x 1.10 / 130 = 1.481 -> 1.48, /(0.8 x 1.04) = 1.779 -> 1.78 (역: 0.8 x 1.78 x 1.04 = 1.481). 2-3: 195 x 1.12 / 130 = 1.68, /0.8 = 2.10. 2-4: 215 x 1.14 / 130 = 1.885 -> 1.89, /(0.8 x 1.08) = 2.188 -> 2.19 (역: 0.8 x 2.19 x 1.08 = 1.892). 2-5: 255 x 1.16 / 130 = 2.275 -> 2.28, /(0.8 x 1.12) = 2.545 -> 2.55 (역: 0.8 x 2.55 x 1.12 = 2.285).

Ch2 Power: 이펙티브 Power = `anchor 50 x 0.80 x powerMultiplier` = 44 / 48 / 52 / 58 / 64 (반올림: 50x0.8x1.10 = 44.0, x1.20 = 48.0, x1.30 = 52.0, x1.45 = 58.0, x1.60 = 64.0). 궁수 기본 공격 DPS 44~64 (사거리 6.0, 쿨다운 1.0) - 기사 S1~S5 의 50~75 보다 낮되 원거리라 접근 압박이 적다.

### 4.5 톱니형 곡선과 상한 점검

| 구간 | eq 배수 | 비고 |
|---|---|---|
| Ch1 S1 -> S5 | 1.00 -> 1.18 -> 1.36 -> 1.55 -> 1.98 | 단조 증가 (+0.18/+0.18/+0.19/+0.43: 보스 도약) |
| Ch1 S5 -> Ch2 S1 | 1.98 -> **1.25** | **-37% 하강** (톱니 골). Ch1 S2~S3 사이 난이도에서 재시작 |
| Ch2 S1 -> S5 | 1.25 -> 1.48 -> 1.68 -> 1.89 -> 2.28 | 단조 증가 (+0.23/+0.20/+0.21/+0.39) |

- 상한 점검: 최대 eq 2.28 <= `C(130)` 2.80 이내. 분자 `archetype.hpScale x hpMultiplier` 최대는 Ch2 S5 의 2.04 로 3.5 미만(플랜 원안 상한도 만족).
- **계수 검증 책임**: 위 표는 **T1=130 가정치**이며, G0 확정 후 T1 이 다르면 §0.4 도출식으로 `Stage.json` 을 일괄 재산출한다(규칙 파라미터·구조는 불변).

### 4.6 Ch2 보스 페이즈 (2-5)

| 구분 | 영웅 HP 비율 | 효과 | 예고/연출 |
|---|---|---|---|
| 스킬 페이즈(궁수 로드아웃) | 85% / 65% / 45% | 관통 사격 / 화살 소나기(스킬) / 회피 도약 (§5.4) | 기존 스킬 해금 컷인 |
| 보스 페이즈 1 | **75%** | `ArrowRain` 활성 (10초 주기 광역 압박) | 배너 "화살비", 예고 원 1초 |
| 보스 페이즈 2 | **35%** | `HunterTraps` 활성 (덫 6곳, 15초 재설치) | 배너 "사냥꾼의 덫" |

HP 비율 시퀀스(내림차순): 90 패시브 -> **85 스킬** -> 80 패시브 -> **75 보스1** -> 70 -> **65 스킬** -> 60 -> **55(Ch1 보스 전용)** -> 50 -> **45 스킬** -> 40 -> **35 보스2** -> 30 ... 모든 비-패시브 임계는 패시브 임계와 0.05 이격(§3.2 규칙).

---

## 5. 궁수 영웅 (Archer)

### 5.1 직업 개요와 빌드 압박

- 콘셉트: 숲속 정찰대의 궁수. 사거리 6.0 에서 쏘며 몬스터가 3.0 안으로 들어오면 후퇴(카이팅).
- 빌드 가위바위보: 근접 위주 **Tank 빌드가 약함**(느린 몬스터는 따라잡지 못함: 궁수 이속 3.2 > Wisp 1.0 / Wraith 0.8 / Reaper 1.5 / Hex 1.4 / Plague 1.3) -> 잡으려면 **Swarm**(Phantom 이속 2.4, `PhantomMoveSpeedBoost` 후 약 4.7 > 3.2)·**Debuff 둔화**(Plague·Slow 카드로 궁수 이속 감소)·**Dps(Hex 사거리 5.0 + `HexRangeBoost`)** 가 필요. 즉 "항상 같은 Tank 빌드"가 막힌다.
- 안개(`DenseFog`)는 Hex 사거리를 3.5 로 줄여 궁수(6.0)를 더 안전하게 하므로 Ch2 S3·S4 에서 Hex 의존 빌드가 한 번 더 억제된다. 이는 의도(궁수 챕터의 "원거리 응수 억제")이며 Debuff/Swarm 경로를 열어둔다.

### 5.2 스탯과 기본 공격

| 항목 | 값 | 출처 |
|---|---|---|
| hpScale / powerScale | 0.80 / 0.80 | `HeroArchetype.json` |
| range / cooldown / moveSpeed | 6.0 / 1.0 / 3.2 | `HeroArchetype.json` |
| 기본 공격 방식 | 즉발 판정(hit-scan) - 시전 모션 strike 시점에 선택 타깃에 피해, 화살은 시각 FX(`ArcherArrowFx`)일 뿐 충돌 판정 없음 | 몬스터 Hex 의 원거리 선례 재사용, 투사체 풀·미스 판정 없음 |
| 사격 모션 길이 | 총 0.6초 이내 (strike 0.25초) | 0.4초/주기 이동 여유 확보. 시트 `stab` 행 9프레임 15fps, strike 프레임 4 |

### 5.3 AI - 카이팅 사양

**상태**

| 상태 | 진입 | 행동 | 해제 |
|---|---|---|---|
| 접근 | 타깃 거리 > range(6.0) + 버퍼 | 타깃 방향 이동 | 거리 <= 6.0 -> 교전 |
| 교전 | 타깃 거리 <= 6.0 이고 후퇴 상태 아님 | 정지 + 타깃 응시 + 사격 | 가장 가까운 몬스터 거리 < `triggerDist`(3.0) -> 후퇴 |
| 후퇴 | 가장 가까운 몬스터 거리 < 3.0 | 위협 중심(반경 `threatRadius` 4.0)의 반대 방향으로 이동, 시선은 가장 가까운 몬스터(뒷걸음 연출), 사격 억제 | 가장 가까운 몬스터 거리 >= `resumeDist`(4.5) 이고 상태 최소 유지 `minDwellSec`(0.4초) 경과 |
| 갇힘 | 후퇴 중 `corneredCheckSec`(0.5초) 동안 이동 < `corneredMinMove`(0.3) | 후퇴 중단, 사격 허용(가장 가까운 몬스터에게), 회피 도약 스킬 사용 가능 | 거리 >= 4.5 또는 이동 재개 |

- 후퇴 방향 보정: 원점으로부터 거리 > `arenaSoftRadius`(7.0)이면 후퇴 방향 단위벡터에 "원점 방향 단위벡터"를 더해 정규화(전장 가장자리에 몰리지 않게).
- 중앙 끌림(`_centerPullEnabled`)은 **궁수 프리팹에서 끈다**(중앙 쪽으로 걸어 들어가 몬스터 밀집지로 진입하는 행동 방지).

**`AutoCombatAI` 충돌 점검 포인트 (gameplay-programmer 필독)**

| # | 기존 로직 | 충돌/주의 | 처리 방침 |
|---|---|---|---|
| 1 | Update 게이트 순서: 사망 -> TimeStop(`IAttacker.Enabled`) -> 스폰 게이트 -> `IAttackGate.IsAttacking` -> 타깃 없음 -> 고정 타깃 -> **FleeMode** -> 중앙 끌림 -> 교전/이동 | 카이팅은 FleeMode(공포 카드) **뒤**, 교전/이동 **앞**에 끼운다. 공포(`FleeMode`)와 후퇴가 동시에 걸리면 FleeMode 우선(공포는 사격도 막음) | 카이팅 분기는 `aiType == Kiter` 일 때만 활성(`KiteSettings` 주입), 기사 경로는 **바이트 단위 동일 동작 유지** |
| 2 | `IsAttacking` 동안 이동 정지 | 후퇴가 사격 모션(0.6초)에 막힘 -> 모션 길이 상한이 카이팅 성패를 정함 | §5.2 의 모션 0.6초 이내를 아트 요구사항으로 고정. 후퇴 중엔 사격 자체를 억제해 모션에 묶이지 않음 |
| 3 | `_engaged` 히스테리시스(`range + _engageBuffer 0.5`) | 사거리가 6.0 으로 커지면 교전-후퇴-교전 전이가 `triggerDist`/`resumeDist` 와 이중 히스테리시스가 됨 | 후퇴 상태가 교전을 **덮어쓴다**: 후퇴 중 `_engaged` 는 유지하되 사격만 억제. 상태 전이 후 `minDwellSec` 동안 재전이 금지 |
| 4 | 고정 타깃 `_retargetMargin 1.5` | 궁수는 후퇴 중 타깃이 계속 바뀔 수 있음 | 고정 타깃 히스테리시스 유지(사격 대상). 후퇴 방향은 타깃이 아니라 위협 중심 기준 |
| 5 | 중앙 끌림 | 궁수는 비활성 | 프리팹 플래그로 처리, 코드 분기 추가 금지 |
| 6 | `TryGetFleeCentroid(pos, radius, ...)` | 후퇴 방향 계산에 재사용 가능. `count == 0` 이면 후퇴 불필요 | 위협 중심이 자기 위치와 거의 같을 때(대칭 포위)는 직전 후퇴 방향 유지 |
| 7 | 스킬 `회피 도약`(§5.4) | 도약 중 `IMover` 이동 명령을 덮어써야 함 | 스킬이 도약 지속시간(0.25초) 동안 AI 이동을 잠그는 소유권을 갖는다(AI 는 잠금 동안 이동 명령 중단) |
| 8 | `HeroHologramMode`(마을 쇼케이스) | 궁수도 마을 홀로그램 영웅으로 표시 | 마을에서는 전투 컴포넌트 off, 카이팅 코드 비활성(기사 쇼케이스와 동일 경로) |
| 9 | Fear 카드 | 궁수는 이미 후퇴 행동이 있어 공포 효과 체감이 약함 | 카드 수치는 변경하지 않음 - 단, qa 에서 Fear 픽률 모니터링(§8) |

### 5.4 궁수 스킬 3종 (영웅 스킬 시스템 `hero_skills.json` 확장)

게이트: 기사와 같은 HP **85% / 65% / 45%**(패시브 임계와 0.05 오프셋 유지). 기사 스킬 구조(`HeroSkillRunner`, 쿨다운 자동 시전, 컷인)를 그대로 재사용.

| 스킬 (`$type`, `fileName`) | 게이트 | 필드 / 값 | 효과 · 처치 타수 |
|---|---|---|---|
| **관통 사격** `PiercingShotSkillData`, `HeroSkill_PiercingShot` ("관통 사격") | 85% | `damage` 70 / `cooldown` 4.0 / `lineLength` 9.0 / `lineHalfWidth` 0.6 / `knockbackStrength` 0.5 / `centroidRadius` 8.0 | 몬스터 무게중심(반경 8.0) 방향으로 직선 관통. 처치 타수: Phantom·Plague·Hex 1타, Reaper 2타(70+70), Wisp 3타, Wraith 8타. 접근하는 몬스터 줄을 세로로 꿴다 |
| **화살 소나기** `ArrowVolleySkillData`, `HeroSkill_ArrowVolley` ("화살 소나기") | 65% | `damagePerTick` 40 / `tickCount` 3 / `tickIntervalSec` 0.5 / `radius` 2.8 / `cooldown` 8.0 / `targetSearchRadius` 7.0 | 영웅 중심 7.0 안 가장 밀집한 지점에 3연속 타격(총 120/대상, 1틱 1회 피해). Phantom 1틱, Plague·Hex 2틱, Reaper 3틱, Wisp·Wraith 면역(120 < 200). 구형 피해 기능(`DamageMonstersInSpheres` 단일 구) 재사용 |
| **회피 도약** `EvadeSkillData`, `HeroSkill_Evade` ("회피 도약") | 45% | `damage` 30 / `cooldown` 10.0 / `leapDistance` 4.5 / `leapDurationSec` 0.25 / `blastRadius` 2.0 / `knockbackStrength` 2.5 / `triggerMonsterCount` 3 / `triggerRadius` 2.0 | 반경 2.0 안에 몬스터 3마리 이상이면(0.25초마다 검사) 위협 중심 반대 방향으로 4.5 도약하며 출발 지점 반경 2.0 에 피해 30 + 넉백 2.5. 갇힘 탈출 수단. 도약 목적지는 `arenaSoftRadius` 7.0 안으로 클램프 |

- `loadout` row: `{heroId: "Archer", hpFraction: 0.85, skill: "HeroSkill_PiercingShot"}`, `{.., 0.65, "HeroSkill_ArrowVolley"}`, `{.., 0.45, "HeroSkill_Evade"}`.
- 이론 상한 DPS(참고, 밀집 가정): 기본 44~64 + 관통(70 x 2.5마리 / 4s = 44) + 화살비(120 x 3마리 / 8s = 45) -> 기사(50 + 돌진 + 노바 ...) 대비 낮지 않으나 후퇴 중엔 기본 공격이 막혀 실효는 낮다. **궁수 실효 DPS 는 qa 게이트 G2 에서 기사 대비 비율로 확인.**
- 스킬 컷인(시간정지 1.9초 x 3회)은 기사와 동일 재사용.

### 5.5 에셋 요구 (신규 영웅 아트 - Claude 제작)

| 에셋 | 사양 |
|---|---|
| `Archer_Sheet.png` + `Archer_Sheet_Emission.png` + `Archer_SheetSpec.json` | `Knight_SheetSpec.json` 동일 규격: 셀 72px, 20열 x 9행, 행 순서 idle/walk/run/slash01/slash02/stab/hit/death/spawn, pivot (0.4236, 0.2917), PPU 31. **사격은 `stab` 행 사용(9프레임 15fps, strike 프레임 4)**, 후퇴는 `walk`/`run` 재사용(뒷걸음은 좌우 반전 없이 시선 고정으로 표현) |
| `HeroIcons/Archer.png`, `Archer_S2~S5.png` | 스테이지 초상 5장 (기사 초상 규격 동일) |
| FX (프리팹 = `EVisual` 값명) | `ArcherArrowFx` `HeroPiercingShotFx` `HeroArrowVolleyFx` `HeroEvadeFx` `StageSpringFx` `StageFogFx` `StageSealChainFx` `StageBulwarkFx` `StageArrowRainFx` `StageTrapFx` |
| 배경 `Battle_Backdrop_DarkForest.png` | 전장 어둠의 숲 도트 배경 (기존 `Battle_Backdrop.png` 해상도·레이어 규격 동일) |
| 사운드 | 이번 범위 제외(후속) |

---

## 6. UI - 챕터 월드맵 팝업 (`HeroSelectPopup` 확장)

> UI 목업: `.mockups/chapter-worldmap.html` (메인이 `node .mockups/server.js` 로 로컬 서버 제공 요청). 톤은 `village-hud-layout.html` 의 돌 패널·16px 격자·금색 강조를 따름.

### 6.1 진입과 구조

- 마을 HUD 하단 중앙 `HeroButton`(영웅)이 기존대로 `EUI.HeroSelectPopup` 을 연다(별도 메인 메뉴/세팅 금지 준수). 버튼 라벨은 `영웅` -> **`지도`** (Strings_Ko id 11 문구 변경 - 영웅 목록이 아니라 챕터 지도가 되므로). 아이콘은 기존 `Icon_Heroes` 재사용(신규 아이콘 후속).
- 현행 팝업(표시 전용 5칸 스크롤)을 **월드맵 구성으로 교체**: `HeroSelectPopup` 클래스·`EUI.HeroSelectPopup` 값·프리팹명 유지(Enum/에셋 키 변경 없음). 풀링 스크롤(`HeroSelectPoolingScrollView`/`HeroSelectCell`)은 제거하고 5개 노드는 프리팹 정적 배치(고정 5개라 풀링 불필요, Rule 03 §3 의 풀링 대상 아님).
- 기존 단일 테스트 경로: 5칸 고정 가정(`BuildCellData` 1..5)과 `HeroSelectCellData` 는 챕터 구조 데이터(`ChapterRow`/`StageRow`/`HeroStageVariantConfig`)로 대체.

### 6.2 화면 구성 (1280x720 기준, 16px 격자)

| 영역 | 좌표 (x,y,w,h) | 내용 |
|---|---|---|
| 딤 | 전체, 검정 알파 0.62 | 탭하면 닫기 |
| 팝업 패널 | 96,48,1088,624 | 돌 패널 |
| 타이틀 바 | 96,48,1088,64 | "침입 지도" + 닫기 X (우측) |
| 챕터 탭 줄 | 112,128 / 탭 200x44 / 간격 208 | 탭 5개: 출시 챕터 2개(`{id}장 {이름}`) + 미출시 3개(`{id}장 ???` 비활성, 정보 노출 없음) |
| 지도 영역 | 112,188,640,392 | 선택 챕터 이름·테마·영웅명 + 스테이지 노드 5개(지그재그 점선 경로) |
| 상세 패널 | 768,188,400,392 | 초상 112x112, `{챕터}-{인덱스}`, 스테이지명, 위협도 별, 영웅명, 내 기록, 규칙 목록, (보스) 보스 페이즈 목록 |
| 하단 | y 608 부근 | 좌: 안내 문구 / 우: 선택 버튼(상세 폭 x 56) |

### 6.3 노드 상태

| 상태 | 판정 | 표현 |
|---|---|---|
| 잠금 | `IsUnlocked(linear, cleared) == false` | 어두운 노드 + 글자 `잠금`, 이름은 `???`, 탭해도 선택 불가(하단 안내 "스테이지 X-Y 클리어 필요") |
| 해금 | `IsUnlocked == true` 이고 `linear > cleared` | 기본 돌 노드 + `{챕터}-{인덱스}` |
| 클리어 | `linear <= cleared` | 황동색 노드 + 하단 태그 `클리어` (체크 이모지 미사용) |
| 선택 | `linear == SelectedStage` | 금색 외곽선 4px + 위쪽 `▼` |
| 보스 | `stageIndex == 5` | 노드 96px(일반 72px), 적갈색 + 위쪽 태그 `BOSS` |
| 챕터 탭 잠금 | `IsChapterUnlocked == false` | 탭 글자 회색 + `잠금`, 탭해도 이동 불가(안내 "{n}장 보스를 클리어하면 열립니다") |

- 챕터 해금 연출(최소): 1장 보스 클리어 결과 후 마을 복귀 시 지도 버튼에 빨간 점(`RedDot` 재사용)을 한 번 표시, 팝업을 열면 2장 탭이 한 번 점멸(0.6초). 별도 연출 화면 없음.
- 팝업을 열 때 초기 선택 = 현재 `SelectedStage`(해금 보장, 잠긴 값이면 `MaxPlayableStage`).

### 6.4 선택과 마을 HUD 반영

- 상세 패널의 **"이 스테이지로 선택"** 버튼: 해금 스테이지에서만 활성. 누르면 `SelectedStage` 를 로컬 저장(클라우드 백업 호출 없음, `hero-stage-variant.md` §4.5 규칙 동일)하고 팝업을 닫는다. **출격은 마을 HUD 의 출격 버튼**(기존 흐름 유지).
- 마을 HUD 스테이지 패널(현상수배서): ◀/▶ 는 **선형 번호 1~10 전체를 순회**(챕터 경계 통과). 표기 변경만: 인디케이터 `STAGE {N}` -> `STAGE {챕터}-{인덱스}`, 위협도 별은 **챕터 내 인덱스** 기준 `★` x 인덱스 + `☆` x (5-인덱스), 도트 5개도 인덱스 기준, 헤더 `침입자` 줄은 `침입자 - {챕터명}`. 잠금 안내: 인덱스 > 1 이면 `스테이지 {챕터}-{인덱스-1} 클리어 필요`, 인덱스 == 1 이면 `{챕터-1}장 보스 클리어 필요`. 위젯 신설 없음(기존 `CHText` 문구만 교체).
- 쇼케이스 영웅: 선택 스테이지의 챕터 영웅(`HeroArchetype`)으로 교체 표시(기사/궁수), 외형은 해당 스테이지 `HeroStageVariantConfig` 엔트리.

### 6.5 UI 문구 (Strings_Ko 1301~1318)

| id | 문구 |
|---|---|
| 1301 | 침입 지도 |
| 1302 | 지도 (마을 하단 버튼) |
| 1303 | 스테이지 규칙 |
| 1304 | 보스 페이즈 |
| 1305 | 내 기록 |
| 1306 | 기록 없음 |
| 1307 | 이 스테이지로 선택 |
| 1308 | 클리어 |
| 1309 | 잠금 |
| 1310 | 특수 규칙 없음 |
| 1311 | {0}장 {1} |
| 1312 | {0}장 ??? |
| 1313 | 스테이지 {0}-{1} 클리어 필요 |
| 1314 | {0}장 보스 클리어 필요 |
| 1315 | {0}장은 {1}장 보스를 클리어하면 열립니다 |
| 1316 | 선택한 스테이지는 마을의 침입자 현상수배서에 반영됩니다. 출격은 마을에서 합니다. |
| 1317 | 영웅 HP {0}% |
| 1318 | 침입자 - {0} |

---

## 7. 구현 요청사항 (gameplay-programmer 용)

### 7.1 Enum 값 (`CommonEnum.cs`, 카테고리별 분리 - Rule 02 §8, Rule 03 §2)

| Enum | 추가 값 | 비고 |
|---|---|---|
| `EHero` | `Archer` | `Knight` 뒤에 추가(기존 직렬화 정합) |
| `EHeroAi` (신규) | `Melee`, `Kiter` | `HeroArchetype.json` `aiType` 값명 |
| `EStageRule` (신규) | `HolySpring`, `DenseFog`, `SealedNest`, `BossBulwark`, `ArrowRain`, `HunterTraps` | `Stage_Rule.json` `ruleId` 값명 |
| `EData` | `Chapter`, `Stage`, `Stage_Rule`, `Stage_Rule_Param`, `Stage_Rule_Trigger`, `HeroArchetype`, `HeroArchetype_Kite`, `HeroSkillLoadout_Archer` | JSON/SO 파일명 = 값명 |
| `EBackdrop` (신규) | `Battle_Backdrop`, `Battle_Backdrop_DarkForest` | `ChapterRow.backdropKey` 값명. 현행 씬 직배치 `Battle_Backdrop` 도 Addressable 키로 등록 |
| `EVisual` | `ArcherArrowFx`, `HeroPiercingShotFx`, `HeroArrowVolleyFx`, `HeroEvadeFx`, `StageSpringFx`, `StageFogFx`, `StageSealChainFx`, `StageBulwarkFx`, `StageArrowRainFx`, `StageTrapFx` | 기존 값 뒤에 추가(int 직렬화 정합), 프리팹 `CHMPool` 대상 |

`EUI` 신규 값 없음(`HeroSelectPopup` 재사용).

### 7.2 Interface (`CommonInterface*.cs`, Rule 02 §9)

- `IStageRule` (Battle 도메인): `EStageRule` 식별 + 활성화/갱신/비활성화 (§3.1). 규칙 구현체는 컨텍스트 인터페이스만 의존.
- 규칙 컨텍스트 인터페이스(이름·시그니처는 gameplay-programmer 판단): §3.1 에 나열한 기능 7종(영웅 회복, 영웅 받는 피해 배율, 몬스터 사거리 배율, 스포너 잠금, 구형 범위 피해(기존 `IHeroSkillContext` 재사용), 몬스터 감속, 몬스터 밀집 지점 조회) + 알림 표시.
- 궁수 카이팅: `AutoCombatAI` 에 `KiteSettings`(`HeroArchetype_KiteRow` 로부터 주입) 설정. 인터페이스 신설 여부는 판단.

### 7.3 에셋 키 (파일명 = Enum 값명)

- JSON(`Assets/_Lair/Data/Json/`): `Chapter.json`, `Stage.json`, `Stage_Rule.json`, `Stage_Rule_Param.json`, `Stage_Rule_Trigger.json`, `HeroArchetype.json`, `HeroArchetype_Kite.json`, `hero_skills.json`(확장), `balance_config.json`(hero 블록 정리), `Strings_Ko.json`(id 1001~1318 추가).
- SO: `HeroSkillLoadout_Archer.asset`, 스킬 SO `HeroSkill_PiercingShot.asset` `HeroSkill_ArrowVolley.asset` `HeroSkill_Evade.asset`(`Art/Skills/`), `HeroStageVariantConfig.asset`(10엔트리, ChapterId/StageIndex 추가·배수 필드 제거).
- 프리팹: `Archer.prefab`(`Art/Characters/`, Knight 프리팹과 동일 컴포넌트 구성, `_centerPullEnabled` off), `EVisual` 10종 FX 프리팹(`Art/FX/`), `HeroSelectPopup.prefab` 개편(`Art/UI/`).
- 스프라이트: §5.5.
- 데이터 로드: JSON 정규화 검증기(로드 시) - 챕터당 스테이지 5개, 복합키 중복 0, FK(ChapterRow.heroId -> HeroArchetype, Stage_Rule* -> StageRow/Stage_RuleRow), 규칙 파라미터 필수 키 집합 일치, `Stage_Rule_Trigger.hpFraction` 가 스킬 게이트/패시브 임계와 불일치.

### 7.4 SO / 코드 변경 요약

| 대상 | 변경 |
|---|---|
| `StageProgress` | §2.1 |
| `MetaProfile` / `MetaProfileStore` | `Version = 5` + 마이그레이션 §2.2. 클라우드 복원 후 마이그레이션 재적용 |
| `BattleController` | 챕터 영웅 프리팹 선택(`EHero`), 영웅 스탯 = 앵커 x `archetype.scale` x `StageRow.multiplier`(1회 반올림), 챕터 배경 교체(`backdropKey`), `StageRuleRunner` 생성·연결, 영웅 스킬 로드아웃을 영웅별로 로드, 랭킹 제출 `hero` = 챕터 `heroId` |
| `StageLeaderboard` / `RankingPopup` / `RecordsPopup` | §2.3 |
| `HeroStageVariantApplier` | 조회 키 (챕터, 인덱스) |
| `AutoCombatAI` | §5.3 카이팅 분기 (기사 경로 불변) |
| 스포너(`Spawner`) | 종족 단위 "봉인" 상태 + 해제 시 즉시 1회 스폰 |
| 스포너 상태 UI(`BuildPanel` 계열) | 봉인 표기 |
| 몬스터 | 사거리 배율 최종 곱 적용 지점, 속도 배율 임시 적용(덫) |
| `VillageController` / `VillageHud` | §6.4 문구, 쇼케이스 영웅 교체 |
| `HeroSelectPopup` | §6 월드맵 |
| 문서 | §0.2 정정은 메인 `[docs]` 소작업 |

---

## 8. 테스트 / QA 검증 항목

### 8.1 test-engineer (EditMode)

| 영역 | 항목 |
|---|---|
| `StageProgress` | 선형 <-> (챕터, 인덱스) 변환 경계(5<->6, 10), `IsUnlocked` 챕터 경계(cleared 5 -> 6 해금, 7 잠금), `IsChapterUnlocked`, `ResolveClearedStage` 상한(MaxStage=10), `ScaleStat` 단일 반올림(Archer 0.8 x 1.56, 소수 경계 0.5) |
| 마이그레이션 | v4(Cleared 0/3/5, Selected 5)->v5 값 보존·`Version == 5`, `StageRecords` 범위 밖 제거, 클라우드 복원 후 재적용, `CopyFrom` 후 동일 |
| JSON 검증 | 정상 데이터 파싱, 각 위반 케이스(스테이지 4개뿐, 복합키 중복, 존재하지 않는 heroId, 규칙 필수 키 누락/추가 키, 트리거 hpFraction 이 0.85 또는 0.8 과 일치) 로드 오류 |
| 규칙 | `HolySpring` 개방 구간(20·60...260, 마지막 268 종료)·회복 틱 소수 누적·최대 HP 초과 금지, `SealedNest` 봉인/해제/해제 시 즉시 스폰, `DenseFog` 하한, `BossBulwark` 1회·지속, 트리거 규칙의 HP 단조 최저값 기준(회복 후 재트리거 금지), `ArrowRain` 대상 없음 시 스킵, `HunterTraps` 이탈 시 감속 복구 |
| 카이팅 | 상태 전이(3.0 진입/4.5 해제/`minDwellSec`), 갇힘 판정, `arenaSoftRadius` 클램프, 공포(FleeMode) 우선, 기사 경로 회귀(동작 불변) |

### 8.2 PlayMode / UnityMCP

- 월드맵 플로우: 마을 -> 지도 버튼 -> 팝업 -> 챕터 탭 전환 -> 노드 선택 -> 선택 -> 마을 HUD 표기 갱신 -> 출격 -> 규칙 발동 -> 결과 -> 해금 -> 2장 탭 점멸.
- Ch2 보스(2-5)까지 실제 플레이: 보스 페이즈 75% / 35% 발동, 스킬 게이트 85/65/45%, 카드 일시정지와 비중첩.

### 8.3 qa-simulator 게이트 (리포트 `docs/qa-reports/`)

| 게이트 | 대상 | 측정 | 통과 기준 |
|---|---|---|---|
| **G0** | 스테이지 1 앵커 | §0.3 | T1 120~140초 등 6항목 |
| **G1** | Ch1 전 스테이지(소급 규칙·재산출 배수) | 스테이지별 평균 처치 시간 (메타 `M_ref` 상당) | 각 스테이지 `targetClearSec` +-15%, 보스 1-5 첫 도전 승률 40~50%. 승률은 `M_ref` 메타 수준 봇 기준 |
| **G2** | 궁수 AI | 후퇴 시간 비율, 갇힘 이벤트 수, 상태 전이 빈도, 전장 이탈, 기본 공격 실효 DPS | 후퇴 상태 비율 20~45%(생존 시간 대비), 갇힘 런당 3회 이하, 상태 전이 10초당 4회 이하, 전장 밖(`arenaSoftRadius` + 1) 프레임 0, 실효 DPS(스킬 제외) >= 같은 스테이지 기사 DPS 의 60% |
| **G3** | Ch2 곡선 | 스테이지별 평균 처치 시간 | 각 스테이지 `targetClearSec` +-15%, 톱니(2-1 < 1-5 기대 시간) 성립, 4축 빌드별 편차 +-30초 |
| **G4** | Ch1/Ch2 보스 | 첫 도전 승률, 보스 구간 시간(보스 밴드 210~270초), 페이즈 발동 후 필드 평균 몬스터 수 | 승률 40~50%, 2-5 필드 점유 평균 12마리 이상(`ArrowRain` + `ArrowVolley` 가 필드를 비우지 않음, `hero-skills.md` §7 기준 승계), 2-5 에서 Tank 빌드 승률 <= Swarm/Debuff 빌드 승률(빌드 가위바위보 의도) |
| 보조 | 성수 샘 | 샘 개방 중 영웅 평균 체류율 | 기사(1-2) 0.6 +-0.15, 궁수(2-4) 0.4 +-0.15 (`ruleFactor` 가정 검증) |
| 보조 | Fear 카드 | 궁수 스테이지 Fear 픽률/효과 | 기사 스테이지 대비 효과 저하가 확인되면 카드 재조정안은 별도 기획 |

### 8.4 클라우드 수동 확인

마이그레이션된 프로필(Cleared 5 -> 2장 S1 해금) 백업 -> 다른 기기에서 복원 -> 충돌 UX 배지·복원 권유 정상 -> 복원 후 Selected 범위 보정 확인.

---

## 9. 엣지 케이스

| # | 케이스 | 처리 |
|---|---|---|
| E-1 | 성수 샘 회복으로 영웅 HP 가 이미 지난 패시브/스킬/보스 임계 위로 올라감 | 임계 판정은 **지금까지의 최저 HP 비율** 기준 단조. 재진입해도 재발동 없음 |
| E-2 | 영웅이 한 프레임에 여러 임계를 건너뜀(대량 피해) | 건너뛴 모든 임계를 높은 값부터 순서대로 1회씩 처리(기존 패시브 큐 규칙과 동일) |
| E-3 | 봉인 종족 스포너가 이미 소환된 몬스터를 가진 채 봉인 | 봉인은 **주기 스폰만** 막음. 필드 몬스터는 유지 |
| E-4 | 봉인 해제 임계(0.70/0.60)가 패시브 카드 팝업과 같은 순간 | 팝업 처리 후 해제(스폰 버스트는 팝업이 닫힌 뒤) |
| E-5 | 보스 페이즈 활성 중 일시정지(카드 선택) | 규칙 갱신이 `BattleClock` 이라 정지. 타이머(예: 결계 15초)도 정지 |
| E-6 | 영웅 사망/승리 직후 남은 효과 | 전투 종료 시 전 규칙 비활성화, FX 풀 반환 |
| E-7 | 안개 하한 0.8 이 영웅 충돌 거리보다 작음 | §3.4 DenseFog 구현 확인 항목 - 충돌 거리 + 0.1 로 상향하고 보고 |
| E-8 | 궁수가 전장 가장자리 코너에 몰림 | 갇힘 판정 -> 사격 허용 + 회피 도약 |
| E-9 | 궁수 회피 도약 목적지가 전장 밖 | 목적지를 `arenaSoftRadius` 안으로 클램프 |
| E-10 | `SelectedStage` 가 잠긴 값(클라우드 복원·데이터 편집) | 마을 진입 시 `MaxPlayableStage` 로 보정(마이그레이션 단계 3과 별개로 런타임 가드) |
| E-11 | 챕터 데이터에 존재하는 챕터가 `HeroStageVariantConfig` 엔트리 없음 | 로드 오류(조용한 폴백 금지) |
| E-12 | v5 클라이언트가 아직 열리지 않은 챕터(예 3)를 랭킹 조회 | `IsValidStage` 가 `MaxStage`(10) 초과를 서버 왕복 없이 거부 |
| E-13 | 2장 진입 후 1장 스테이지 재도전 | 허용(소울 파밍). 보상은 챕터 무관 동일식 |
| E-14 | 서버 보안 규칙이 stage 6~10 거부 | 클라이언트는 제출 실패를 조용히 처리(기존 정책), 사용자에게 콘솔 확장 안내(§2.3) |
| E-15 | 구버전 로컬 세이브에 `SelectedHero` 만 있고 `SelectedStage` 가 챕터와 불일치 | 마이그레이션 단계 5 가 챕터 영웅으로 재설정 |

---

## 10. 미결 사항 · 사용자 승인 항목

| # | 항목 | 추천안 | 상태 |
|---|---|---|---|
| **D-1** | 액티브 트리거 5회 확정 + 문서 정정 (§0.1·§0.2) | **A. 5회** (대안 B 9회 복귀, C 스테이지별) | 승인 필요 |
| **D-2** | Ch1 기존 배수(1.25/1.55/1.90/2.30) -> 시간 기반 재산출(1.10/1.36/1.46/1.92) (§0.4·§4.3) | **A. 재산출** | 승인 필요 (현행 값 유지 시 G0 후 후반 처치 불가 - §0.4 대안 B) |
| **D-3** | HP 배수 상한 x3.5 고정 -> 도출식 `C(T1)` (T1=130 -> 2.80) (§0.4) | 도출식 채택 | 승인 필요 |
| **D-4** | G0 통과 창 T1 120~140초 (컨셉 2~4분 중 하단) | 채택 | 승인 필요 (컨셉 §8 해석) |
| **D-5** | `hero_skills.json` 명명 예외(관계 데이터 이름 규칙 미적용, 동기 도구 호환) | 예외 허용 | 승인 필요 |
| **D-6** | 진행 저장 선형 번호 유지(플랜의 "챕터별 진행 리스트" 대신) (§2.1) | **A. 선형** | 승인 필요 |
| **D-7** | 챕터당 스테이지 5 고정 (플랜 미결 "5 고정 vs 가변") | **5 고정** | 승인 필요 |
| **D-8** | CLAUDE.md §8 챕터 구조 승격 문안 (§0.5) | 제안 문안 | 승인 후 메인 반영 |
| **D-9** | 데이터 구조표 (§1) | 제안 구조 | **구조 확인 게이트 - 승인 필요** |
| **D-10** | UI 목업 `.mockups/chapter-worldmap.html` (§6) | 제안 | **목업 승인 게이트 - 승인 필요** |
| D-11 | 지도 버튼 라벨 `영웅` -> `지도` (아이콘 `Icon_Heroes` 재사용, 신규 아이콘 후속) | 채택 | 확인 |
| D-12 | 사용자 작업(Firebase 콘솔): stage 허용 범위 1~10 확장 (§2.3) | - | 사용자 작업 |

**후속 소작업(본 기획서 범위 밖, 메인이 보고만)**: `hero-skills.md` §2 수치를 shipped JSON 에 맞게 정정(§0.2), 컨셉서·CLAUDE.md 정정.

**수치 확정 방식**: 본 문서 모든 배수·`ruleFactor`·`M_ref` 는 shipping 기본값이며 G0(T1 확정) -> G1 -> G3 -> G4 순서로 검증한다. 결정 메트릭은 §8.3 표, 재산출 규칙은 §0.4 도출식.

---

## 11. Self-Review

- **Placeholder 잔존**: 미정 마커·"적절히"·"또는/둘 중 하나"(디자인 결정 영역)·"약/대략" 어림 - 수치 어림은 산식 병기(`약 22런`, `약 46%` 는 가정치로 명시하고 검산식 있음). 모든 미확정 계수는 "추정치 + 결정 메트릭(게이트)" 으로 표기. 잔존 0건.
- **내부 일관성**: Ch1/Ch2 배수는 §1.3·§4.3·§4.4 에서 동일(1.00/1.10/1.36/1.46/1.92, 1.56/1.78/2.10/2.19/2.55). 규칙 파라미터는 §3.4 와 §4.1 표 일치. 트리거 임계(0.55/0.75/0.35, 봉인 0.60/0.70)는 §3.2 규칙과 모순 없음(스킬 85/65/45·패시브 90~10 과 불일치).
- **명명 일관성**: `HolySpring`/`DenseFog`/`SealedNest`/`BossBulwark`/`ArrowRain`/`HunterTraps`, `ChapterRow`/`StageRow`/`Stage_RuleRow`/`Stage_Rule_ParamRow`/`Stage_Rule_TriggerRow`/`HeroArchetypeRow`/`HeroArchetype_KiteRow`, `StageRuleRunner`, `KiteSettings` 일관. 스킬 `HeroSkill_ArrowVolley`(표시명 "화살 소나기")와 보스 규칙 `ArrowRain`(표시명 "화살비")은 별개 식별자·별개 표시명으로 구분.
- **스코프**: 0단계 + 1단계 단일 기획서이나 구현 단위는 (a) 데이터/진행 (b) 규칙 엔진 (c) 월드맵 UI (d) 궁수+Ch2 로 분할 가능. 분할 구현은 메인 판단(권장 순서 a -> b -> c -> d).
- **구현 요청사항 완전성**: Enum/Interface/에셋 키/SO·JSON 스키마 §1·§7 수록.
- **UI 목업**: `.mockups/chapter-worldmap.html` 작성(마을 배경·돌 패널 톤). 영웅 초상은 목업에서 `Icon_Heroes` 대체.

**Self-Review: 1항목 보강 후 통과 - 스킬 표시명 "화살비"가 보스 규칙 `ArrowRain`("화살비")과 충돌해 스킬 표시명을 "화살 소나기"로 정정(§4.6·§5.4 반영).**
