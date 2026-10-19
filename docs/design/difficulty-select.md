# 난이도 선택 (Easy / Normal / Hard) 1차 - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-19 · 단계: v0.3 · 개정: 초안의 "Heat 0~5" 6단계를 "Easy / Normal / Hard" 3단계로 개편 (구 파일 `heat-difficulty.md` 폐기)
> 입력: `C:\Users\Tae\.claude\plans\reactive-wishing-newt.md` · `chapter-system.md` · `worldmap-popup-redesign.md` · `village-meta-hub.md` · `project_lair_concept.md` · 코드 `HeroStatCalculator.cs` / `MetaProfile.cs` / `MetaProfileMigration.cs` / `BattleController.cs`(SpawnHero ~691, EndBattle ~1340) / `WorldMapViewModel.cs` / `Stage.json`
> **UI 목업**: `.mockups/difficulty-select.html` (사용자 승인 게이트 대상 - 월드맵 하단 바 3단 세그먼트, 결과창 줄/해금 배너, 기록 행)
> **데이터 구조표**: 본 문서 §2 (사용자 승인 게이트 대상)

---

## § 헤더

- **목표**: 스테이지 입장 전 플레이어가 **난이도(Easy / Normal / Hard)** 를 직접 골라, 같은 스테이지를 영웅 체력·공격력 배율만 높인 채 재도전할 수 있게 한다. 기본 선택은 Easy(= 기존 게임 그대로). 해금은 스테이지별로 "Easy 클리어 → Normal, Normal 클리어 → Hard".
- **검증 가설**: (1) 클리어한 스테이지를 더 어렵게 다시 하는 선택지가 "영웅이 너무 쉽다" 문제를 해소하고 재도전 동기를 만드는가. (2) 영웅 능력치 배율만으로 단계 간 체감 난이도 차(처치 시간 +8~20%/단계, 승률 하락)가 일정하게 나오는가. (3) 2차(유물)가 얹힐 때 난이도가 드랍 확률/등급의 기준이 될 수 있도록 스테이지×난이도 클리어 기록이 기반이 되는가.
- **현재 단계 범위 적합성**: **범위 밖이었던 항목의 명시 승격 요청 - 사용자 승인 필요.** CLAUDE.md §8 은 "악몽(Heat)" 형태의 난이도 선택을 개별 승격 전까지 금지한다. §9 에 승격 문구 초안이 있으며 **승인 전에는 구현에 착수하지 않는다.** 유물·★ 목표·무한 탑·데일리·던전 편성은 계속 금지. 서버는 Firebase 클라이언트 코드 범위 유지(신규 서버 코드 0). 별도 메인 메뉴/세팅 화면 금지 유지(선택은 기존 월드맵 팝업 안).
- **핵심 메커니즘**: (a) `Difficulty.json` 3행(0 Easy / 1 Normal / 2 Hard)이 영웅 HP/공격력 **곱 배율**을 제공. 스테이지 JSON 은 건드리지 않고 `영웅 최종 스탯 = ScaleHeroStat(앵커, 직업 배율 × 스테이지 배수 × 난이도 배율)` 1회 반올림. (b) 스테이지별 최고 클리어 난이도를 `MetaProfile.DifficultyClears` 에 저장, 선택 가능 상한 = 클리어한 최고 난이도 + 1. (c) Battle 은 매 전투 `MetaSession` 에서 `SelectedDifficulty` 를 재독(Retry 가 마을을 거치지 않음).

**해석 확인(사용자 요청 대로)**: Easy = 기존 수치 그대로(배율 1.00/1.00, 기존 스테이지와 비트 동일). Normal/Hard 는 영웅 HP·공격력만 곱연산 상승. 해금은 스테이지별로 독립. 해석 일치함 - 이견 없음.

---

## 1. 룰·수치

### 1.1 단계 구성

- 3단계 고정(Easy / Normal / Hard), 순서 Easy → Normal → Hard, 기본 선택 Easy. 내부 값은 정수 인덱스 0 / 1 / 2.
- 최대 난이도 인덱스는 `Difficulty.json` 의 최대 `difficulty` 에서 도출한다(코드 상수 금지). 단, 이번 UI(세그먼트 3칸)는 3행 전제이므로 행 수가 3이 아니면 로드 오류로 처리한다(§2.2).
- 2차에서 난이도 단계를 늘려야 하면 그때 UI 를 세그먼트 → 스테퍼로 바꾸는 별도 기획을 한다(YAGNI).

### 1.2 배율표 (`Difficulty.json` 초기값, 튜닝 대상)

| difficulty | 라벨 | hpMultiplier | powerMultiplier |
|---|---|---|---|
| 0 | Easy | 1.00 | 1.00 |
| 1 | Normal | 1.15 | 1.10 |
| 2 | Hard | 1.30 | 1.22 |

- 단계 간 증가: HP ×1.15 → ×1.13 (1.30 ÷ 1.15 = 1.130). 기대 처치 시간 증가율이 두 번째 단계에서도 +13% 이상이 되게 해 단계 간 체감 차이를 균등하게 둔다.
- **Power 가 HP 보다 낮은 이유**: 영웅 공격력이 오르면 몬스터가 더 빨리 죽어 보드 압력이 줄고, 그 결과 영웅 처치 시간이 HP 비율보다 더 늘어난다(비선형). 같은 폭으로 올리면 Hard 보스가 클리어 불가로 뛴다. HP-only / Power-only 분리 측정을 QA 에서 한다(§8.2).
- **수치 폭 근거 (6단계 초안 → 3단계)**: 초안은 단계당 +10~14% 로 5단계(최대 ×1.60)였다. 3단계에서는 시간 예산(§1.3)이 허용하는 최대 지점인 HP ×1.30 을 Hard 로 두고(초안 Heat 3 의 1.32 에 해당), 중간을 1.15 로 잡았다. 초안 Heat 4~5(1.46~1.60)는 보스에서 시간 초과(300초)가 확정적이라 삭제했다.
- **곱연산 관계**: 최종 배율 = 챕터 영웅 직업 배율 × `Stage.json` 배수 × 난이도 배율. 곱셈 한 번에 묶어 **1회만 반올림**(중간 반올림 금지, `chapter-system.md` §2.1 유지). Easy 는 ×1.00 이라 기존 수치가 그대로다.

곱연산 예시 (Stage 배수는 `Stage.json` 현행 값, 직업 배율은 별도 곱):

| 스테이지 | Stage hp / power | Normal 곱 (hp / power) | Hard 곱 (hp / power) |
|---|---|---|---|
| 1-1 | 1.00 / 1.00 | 1.150 / 1.100 | 1.300 / 1.220 |
| 1-5 | 1.92 / 1.50 | 2.208 / 1.650 | 2.496 / 1.830 |
| 3-5 | 3.20 / 1.75 | 3.680 / 1.925 | 4.160 / 2.135 |
| 5-5 | 2.25 / 1.95 | 2.588 / 2.145 | 2.925 / 2.379 |

검산: 1.92×1.15 = 2.208 / 1.92×1.30 = 2.496 / 1.50×1.22 = 1.830 / 3.20×1.15 = 3.680 / 3.20×1.30 = 4.160 / 1.75×1.10 = 1.925 / 1.75×1.22 = 2.135 / 2.25×1.15 = 2.5875 / 2.25×1.30 = 2.925 / 1.95×1.10 = 2.145 / 1.95×1.22 = 2.379.

### 1.3 시간 예산 점검 (R1 재계산)

기대 처치 시간 ≈ `targetClearSec` × 난이도 HP 배율 ÷ `M`(상점 보조 계수, `chapter-system.md` §0.4: Ch1 보스 1.07, Ch2 보스 1.16, 이후 챕터는 qa-simulator 가 정한다). 시간 천장은 300초.

| 스테이지 (targetClearSec) | M 가정 | Easy | Normal | Hard | 300초 대비 |
|---|---|---|---|---|---|
| 1-1 (130) | 1.00 | 130 | 149.5 | 169 | 여유 131초 |
| 1-5 (240) | 1.07 | 224.3 | 257.9 | 291.6 | 8.4초 여유 |
| 2-5 (255) | 1.16 | 219.8 | 252.8 | 285.8 | 14.2초 여유 |
| 3-5 (255) | 1.16 (가정) | 219.8 | 252.8 | 285.8 | 14.2초 여유 |
| 5-5 (260) | 1.16 (가정) | 224.1 | 257.8 | 291.4 | 8.6초 여유 |

검산: 240÷1.07 = 224.3 / 240×1.15÷1.07 = 257.9 / 240×1.30÷1.07 = 291.6 / 255×1.30÷1.16 = 285.8 / 260×1.30÷1.16 = 291.4 / 130×1.15 = 149.5.

- **R1(보스 시간 초과) 재판정**: 초안 6단계에서는 보스 Heat 3~5 가 300초를 넘었다. 3단계 재설계로 **보스 Hard 가 M 가정 아래 8~14초 여유로 천장 안에 들어온다.** 즉 "열렸는데 못 깨는 단계"는 설계상 제거됐다. 남은 위험은 ① 3-5·5-5 의 M 이 1.16 보다 작을 때(예: M 1.10 이면 3-5 Hard = 255×1.30÷1.10 = 301.4초 > 300) ② Power 의 비선형 효과로 처치 시간이 HP 비율을 넘을 때다. 둘 다 §8.2 게이트 D3 에서 수치로 판정하고, 미달 시 조정 레버는 **`Difficulty.json` 숫자뿐**(코드 변경 0)이다.
- **트레이드오프**: 보스를 안전하게 맞춘 결과 초반 스테이지(1-1)의 Hard 는 처치 시간 169초로 천장에 한참 못 미쳐 "쉬운 Hard" 가 된다. 전 스테이지 공통 배율의 한계이며, 이 문제가 H 지표에서 확인되면 스테이지별 보정 관계 데이터 `Stage_Difficulty` 도입을 제안한다(이번 범위에서는 YAGNI).

### 1.4 해금 규칙

- 스테이지 s(선형 번호)의 **선택 가능 최대 난이도** `MaxSelectableDifficulty(s)`:
  - `s > ClearedStage` (미클리어): **0** (Easy 만)
  - `s <= ClearedStage`: `Min(최대 난이도 인덱스, 클리어한 최고 난이도(s) + 1)`. 클리어한 최고 난이도(s) 는 `DifficultyClears` 에 s 의 행이 있으면 그 `MaxDifficulty`, 없으면 0 (= Easy 만 클리어)
- 즉 Easy 클리어(`ClearedStage` 갱신) → Normal 해금, Normal 클리어 → Hard 해금. Hard 를 클리어해도 열 것이 없다.
- 스테이지끼리 독립. 신규 스테이지 해금은 기존 `StageProgress.IsUnlocked` 그대로(Easy 클리어로 진행). 챕터 출격 게이트(`ChapterGate`)는 난이도와 무관.
- **기존 유저**: 이미 `ClearedStage` 까지 진행한 유저는 마이그레이션 직후 클리어한 모든 스테이지에서 Normal 이 열린 상태가 된다(Easy 클리어로 간주). Hard 는 Normal 을 깨야 열린다.

### 1.5 최고 난이도 기록 갱신 규칙

| 상황 | 처리 |
|---|---|
| 승리, 선택 난이도 d >= 1, d > 해당 스테이지 기존 `MaxDifficulty`(행 없으면 0) | `DifficultyClears` 의 s 행 `MaxDifficulty = d` 로 갱신, 행 없으면 새로 추가 |
| 승리, d <= 기존 `MaxDifficulty` | 변경 없음 (낮은 난이도 재클리어가 기록을 내리지 않는다) |
| 승리, d = 0 (Easy) | `DifficultyClears` 변경 없음 (`ClearedStage` 갱신만) |
| 패배 / 시간 초과 | 변경 없음 |
| 전투 중 뒤로가기 → 마을 복귀(저장 없이 종료, 기존 동작) | 변경 없음 |

- `DifficultyClears` 의 행은 **Normal 이상을 클리어한 스테이지에만 존재**한다(`MaxDifficulty >= 1` 불변식). "Easy 만 클리어" 를 0 으로 채운 행을 만들지 않는다.
- 전투에 적용되는 난이도는 **전투 시작 시점에 확정**한 `EffectiveDifficulty`(§4.3)이며 판 도중 변하지 않는다. 승리 시 기록 갱신은 이 값으로 한다.

### 1.6 난이도가 건드리지 않는 것 (범위 고정) · D-2 재검토

- 소울·영주 XP 보상(`SoulRewardCalculator`) - 난이도 가산 없음(요구 확정: 영웅 체력·공격력 배율만). 보상 없는 1차의 동기 리스크는 §8.1 R3.
- 몬스터 능력치·스폰·스테이지 규칙·카드·액티브/패시브 임계 - 변경 없음. 패시브(영웅 HP 10% 구간)·영웅 스킬 게이트(85/65/45%)는 HP **비율**이라 난이도로 불변.
- 도전과제·도감 - 변경 없음.
- **랭킹·최단 기록은 Easy 한정 (D-2 유지, 3단계 기준 재검토 결과 유지)**: Normal/Hard 판은 `MetaProfile.BestClearTime`, `StageRecordEntry.BestClearTime`, `IsNewStageBest`, `SubmitRanking` 에서 **제외**한다. 재검토: 3단계가 되어 Easy 가 "기존 게임 그대로" 이므로 기존 리더보드 문서의 의미(같은 조건의 최단 클리어)가 Easy 로 정확히 보존된다. 난이도별 랭킹 분리(복합키 `stage + difficulty`)는 Firestore 규칙·인덱스 변경이 필요해 이 레포 범위 밖이라 기각한다. 전적 `Runs`/`Wins` 카운트는 난이도와 무관하게 올린다.
- 영웅 스킬 데미지(`hero_skills.json`)가 영웅 `Power` 를 참조하는지는 코드 확인 후 보고한다(구현 요청사항 §6 확인 항목).

---

## 2. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 콘텐츠·밸런스 수치 = JSON(`Assets/_Lair/Data/Json/`, 파일명 = Enum 값명). 세이브 데이터 = `MetaProfile` 엔트리 리스트(`StageRecordEntry` 선례). "관계 없음" 을 sentinel(-1/0/null)로 표현하지 않는다.

### 2.1 엔티티 / 관계 트리

```
Difficulty (엔티티, JSON)  DifficultyRow     <- "난이도 단계 하나". 스테이지와 무관한 공통 배율표 (3행)
   (스테이지별 예외 배율이 필요해지면 선택적 관계 Stage_Difficulty 신설 - 이번엔 없음, §1.3)

MetaProfile (세이브 루트)
 |-- SelectedDifficulty : int            <- 필수 1:1 스칼라. 0 = Easy 라는 실제 값 (sentinel 아님)
 |
 +-- DifficultyClears : List<DifficultyClearEntry>   <- Stage 의 선택적 관계 (Normal 이상 클리어한 스테이지만 행 존재)
        DifficultyClearEntry { Stage (PK, 선형 스테이지 번호), MaxDifficulty (>=1) }

참조: DifficultyClearEntry.Stage -> StageRow (선형 번호, StageRecordEntry.Stage 와 동일 키 규약)
      MetaProfile.SelectedDifficulty / DifficultyClearEntry.MaxDifficulty -> DifficultyRow.difficulty
```

### 2.2 필드 표

**DifficultyRow** - `Difficulty.json` (EData `Difficulty`). 핵심 엔티티: "난이도 단계 하나". 단일 PK(전역 단계 번호 하나).

| 필드 | 타입 | 키/관계 | 설명 · 값 |
|---|---|---|---|
| `difficulty` | int | PK | 0 / 1 / 2 (연속, 0부터) |
| `nameKey` | int | 필수 1:1 → Strings_Ko id | 1327 Easy / 1328 Normal / 1329 Hard |
| `hpMultiplier` | float | | 영웅 HP 곱 배율 (§1.2 표) |
| `powerMultiplier` | float | | 영웅 공격력 곱 배율 (§1.2 표) |

- 로드 검증(위반 시 로드 오류, `ChapterDatabase` 오류 수집 패턴과 동일): (1) 행이 정확히 3개이고 `difficulty` 가 0·1·2 (빠짐·중복 없음), (2) `difficulty = 0` 행이 정확히 1.0 / 1.0, (3) 두 배율이 `difficulty` 증가에 대해 단조 비감소, (4) 두 배율 > 0, (5) `nameKey` 가 Strings_Ko 에 존재.
- 3단계 UI 고정 전제의 "정확히 3행" 검증은 §1.1 과 같은 이유다. 4단계 이상으로 확장할 때 UI 개편과 함께 풀기 위해 이 검증 한 곳만 바꾸면 되도록 둔다.

**MetaProfile 신규 필드** (`Scripts/Meta/MetaProfile.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `SelectedDifficulty` | int | 0 (Easy) | 마지막으로 확정한 난이도. 로컬 선호값이나 `SelectedStage` 와 동일하게 `CopyFrom` 이 함께 복사 |
| `DifficultyClears` | `List<DifficultyClearEntry>` | 빈 리스트 | 스테이지별 클리어한 최고 난이도. 클라우드 복원 대상 |
| `Version` | int | 6 → **7** | 스키마 추가에 따른 승격 |

**DifficultyClearEntry** (`MetaProfile.cs` 안, `[Serializable]`, `StageRecordEntry` 옆)

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `Stage` | int | PK | 선형 스테이지 번호 1~MaxStage |
| `MaxDifficulty` | int | | 이 스테이지에서 클리어한 최고 난이도 인덱스. 불변식 `>= 1` |

> 명명 메모: `DifficultyClearEntry` 는 게임 콘텐츠 JSON 관계 데이터가 아니라 세이브 엔트리라 §11-1-5 의 `<부모>_<관계명>` JSON 명명이 아니라 기존 `StageRecordEntry` / `CardPickCountEntry` 선례를 따른다.

### 2.3 조회 헬퍼 (의도만, 시그니처는 gameplay-programmer 판단)

- `GetMaxClearedDifficulty(stage)`: `DifficultyClears` 에 행이 있으면 `MaxDifficulty`, 없으면 0 (조회 부수효과 0. 반환 0 은 "Easy 까지 클리어" 의미이며 저장되는 값이 아니다. 호출부가 `stage <= ClearedStage` 를 먼저 확인한다)
- `MaxSelectableDifficulty(stage, clearedStage, maxDifficulty)`: §1.4 정의. 순수 정적 함수로 `StageProgress` 옆에 둔다(해금 판정 단일 소유 원칙).
- `ClampDifficulty(difficulty, stage, clearedStage, maxDifficulty)`: `Clamp(difficulty, 0, MaxSelectableDifficulty)`. 모든 읽기 지점이 이 함수를 쓴다.

---

## 3. 마이그레이션 · 클라우드

### 3.1 `MetaProfile` Version 6 → 7

| 단계 | 규칙 (`MetaProfileMigration.Apply`, 멱등) |
|---|---|
| 1 | `CurrentVersion = 7`. `Version < 7` 이면 7 로 승격 |
| 2 | 기존 단계(진행 범위 보정, 전적 정리, `SelectedHero`) 유지 |
| 3 | `DifficultyClears` null 이면 빈 리스트 |
| 4 | `DifficultyClears` 정리: 행이 null / `Stage < 1` / `Stage > MaxStage` / `MaxDifficulty < 1` 이면 제거. **`Stage > ClearedStage` 인 행도 제거**. 같은 `Stage` 중복이면 `MaxDifficulty` 최댓값 한 행만 남김 |
| 5 | `SelectedDifficulty < 0` 이면 0 으로 보정 |

- **난이도 상한 클램프는 마이그레이션에서 하지 않는다.** 기존 마이그레이션이 "데이터 로드 전 호출 시 기본 `MaxStage` 로 잘리는" 경로를 방어하고 있고(`heroIdOfLinearStage == null && Version >= CurrentVersion` 조기 반환), 최대 난이도도 `Difficulty.json` 로드 전에는 알 수 없다. 상한 보정은 읽는 순간 `ClampDifficulty`(§2.3)가 담당한다.
- v6 세이브: 필드 부재 → `SelectedDifficulty = 0`(Easy), `DifficultyClears` 빈 리스트로 로드(데이터 이전 코드 불필요).
- 갱신 필요 테스트: `Tests/EditMode/Chapter/MetaProfileMigrationTests.cs` 버전 기대값 6 → 7 (§7).

### 3.2 클라우드 동기

- `MetaProfile.CopyFrom` 에 `SelectedDifficulty`, `DifficultyClears`(`?? new List<DifficultyClearEntry>()`) 추가 필수(빠뜨리면 복원 시 난이도 진행만 유실).
- 업로드 `schemaVersion` 은 `profile.Version` 을 그대로 쓰므로(`FirebaseSdkApiClient` 175행) 7 로 자동 승격. 별도 상수 없음.
- 복원 직후 `MetaProfileMigration.Apply` 를 동일 함수로 재적용(기존 규칙) → §3.1-4 정리가 복원 데이터에도 적용된다.
- v6 문서를 v7 클라이언트가 복원하면 난이도 필드가 없어 Easy/빈 상태가 된다(복원은 문서 기준 전체 대체라는 기존 동작과 동일, 충돌 UX 배지·복원 권유 변경 없음). v7 문서를 v6 클라이언트가 복원하면 난이도 필드는 무시된다(구버전 코드, 알려진 제약. 신구 클라이언트 동시 배포).
- 복원된 `SelectedDifficulty` 가 해금 범위를 넘어도 §4.3 의 `EffectiveDifficulty` 가 방어한다.

---

## 4. 전투 적용

### 4.1 능력치 계산

- `HeroStatCalculator.Compute` 에 난이도 행을 받는 경로를 추가한다. `HP = ScaleHeroStat(앵커 HP, 직업 hpScale × stage.hpMultiplier × difficulty.hpMultiplier)`, `Power` 도 동일. `range` / `cooldown` / `moveSpeed` 는 직업 값 그대로.
- **기존 4인자 `Compute` 시그니처는 유지**(Easy 경로, 기존 테스트 12개 이상이 이 시그니처를 호출). 신규 오버로드가 난이도 행을 추가로 받는다. `StageProgress.ScaleHeroStat` 도 곱 하나를 더 받는 오버로드를 추가한다(기존 시그니처 유지). 중간 반올림 금지, decimal 계산 규약 유지.
- 호출처: `BattleController.SpawnHero` (~691~794행).

### 4.2 난이도 읽기 경로 (Retry 대응)

- Battle 은 이미 `MetaSession.GetOrLoad().SelectedStage` 를 전투마다 읽는다. `SelectedDifficulty` 도 **같은 지점에서 같은 방식으로 읽는다**(`SpawnHero` 시점). Retry(`ResultPopup.OnClickRetry`)는 마을을 거치지 않고 Battle 을 재시작하므로 자동으로 같은 난이도가 유지된다.
- Retry 시 난이도 자동 상승 없음(방금 해금된 다음 난이도로 올라가지 않고 같은 난이도 유지). 올리려면 월드맵에서 선택.

### 4.3 `EffectiveDifficulty` (방어 로직)

전투 시작 시 `EffectiveDifficulty = ClampDifficulty(profile.SelectedDifficulty, profile.SelectedStage, profile.ClearedStage, 최대 난이도)` 를 계산해 그 전투의 능력치·결과 팝업·기록 갱신에 일관되게 쓴다. 클라우드 복원·수동 편집으로 해금되지 않은 난이도가 `SelectedDifficulty` 에 들어와도 전투가 방어한다. 이 계산은 프로필을 **수정하지 않는다**(읽기 전용). 정규화 저장은 월드맵 확정과 복원 직후에만 한다.

### 4.4 EndBattle 순서 (`BattleController` ~1340~1380)

```
승리 판정 후, RecordStageRun / ClearedStage 갱신 "전에":
  unlockedBefore = MaxSelectableDifficulty(SelectedStage)     <- 결과창 해금 배너 판정용
기존 처리 (IsNewStageBest 는 Easy 판만 / RecordStageRun 의 Runs·Wins 는 모든 판 / ClearedStage 갱신)
승리 시: EffectiveDifficulty >= 1 이면 DifficultyClears 갱신 (§1.5)
unlockedAfter = MaxSelectableDifficulty(SelectedStage)
newlyUnlockedDifficulty = unlockedAfter > unlockedBefore ? unlockedAfter : 0
```

- `newlyUnlockedDifficulty` 의미: 이번 승리로 새로 열린 선택 가능 최대 난이도 인덱스. 0 이면 배너 없음(열리는 난이도는 항상 1 이상이라 0 이 "없음" 으로 모호하지 않다 - Easy 는 해금 대상이 아니다).
- 예: 미클리어 1-1 을 Easy 로 클리어 → before 0 / after 1 → "Normal 해금!". Normal 최초 클리어 → before 1 / after 2 → "Hard 해금!". Hard 클리어 → before 2 / after 2 → 배너 없음. 이미 깬 난이도 재클리어 → 배너 없음.
- Normal/Hard 판은 `SubmitRanking` 호출 안 함, `profile.BestClearTime` 갱신 안 함(§1.6).

### 4.5 전투 HUD

- 영웅 이름 표기(`BattleViewModel.ComposeHeroTitle(name, stageIndex)` 호출부)에 `EffectiveDifficulty >= 1` 이면 " · " + 난이도 라벨(Normal / Hard)을 접미로 붙인다. Easy 는 기존 표시 그대로(접미 없음). 신규 위젯·문구 없음(`nameKey` 문구 재사용).

---

## 5. UI

> 목업: `.mockups/difficulty-select.html` - 기존 `worldmap-redesign-v2` 의 프레임·색·카드·노드 스타일 재사용. 난이도 색은 기존 팔레트 재사용: Easy `#5ef0b4` / Normal `#f7c64a` / Hard `#e5484d`.

### 5.1 월드맵 팝업 - 3단 세그먼트 (신규)

**컨트롤 형태 대안**:

| 안 | 내용 | 평가 |
|---|---|---|
| A. −/값/+ 스테퍼 (6단계 초안) | 단계가 많을 때 적합 | 3단계에서는 전체 선택지가 안 보이고, 최고 단계로 가려면 2번 눌러야 함 |
| **B. 3단 세그먼트 (권장)** | Easy / Normal / Hard 3칸이 항상 보이고 한 번에 선택. 잠긴 칸은 회색으로 "다음에 열 것" 이 즉시 보임 | 단계가 4 이상으로 늘면 폭이 모자라 스테퍼로 재설계 필요(§1.1) |

**결정: B.**

**배치 대안**:

| 안 | 평가 |
|---|---|
| 상세 카드 안 (기록 박스 아래) | 규칙/보스 페이즈 영역 208px 이 160px 로 줄어 2-5 / 3-5 의 규칙+페이즈 2건 사례가 오버플로 위험 |
| **하단 바, 선택 버튼 왼쪽 (권장)** | 선택 버튼과 같은 줄이라 "난이도 정하고 선택" 흐름이 한 시선. 기존 영역을 건드리지 않음 |

**결정: 하단 바.** 좌표(팝업 루트 1280x720 기준, 좌상단 원점):

| 영역 | 좌표 (x, y) | 크기 | 비고 |
|---|---|---|---|
| 안내 문구 `_bottomText` (변경) | (96, 604) | 480x56 (기존 700x56 에서 축소) | 글자 13 → 12, 2줄 허용(줄 높이 18). 문구 1316 은 한 줄에 480px 이 안 들어가 2줄이 된다 |
| 난이도 박스 (신규) | (592, 604) | 232x56 | 바탕 `#0d1119`, 테두리 3px `#07090e` |
| 선택 버튼 `_selectButton` | (840, 604) | 352x56 | 변경 없음 |

검산: 96+480 = 576, 간격 16, 592+232 = 824, 간격 16, 840 시작, 840+352 = 1192 ≤ 패널 우측 1208.

박스 내부(박스 좌상단 기준):

| 위젯 | 위치 | 크기 | 스타일 |
|---|---|---|---|
| 세그먼트 Easy | (4, 4) | 72x32 | 글자 14. 기본: 기존 `.btn` 톤 `#2a3350` 계열 |
| 세그먼트 Normal | (80, 4) | 72x32 | 같음 |
| 세그먼트 Hard | (156, 4) | 72x32 | 같음 |
| 효과 문구 | (4, 36) | 224x14 | "영웅 HP ×1.15 · 공격력 ×1.1", 글자 11, 색 `#8e98ad`, 가운데 정렬 |

폭 검산: 4+72+4+72+4+72+4 = 232 (세그먼트 3×72 + 간격 2×4 + 좌우 여백 2×4).

**세그먼트 상태**:

| 상태 | 표시 |
|---|---|
| 선택됨 | 바탕이 난이도 색(Easy `#5ef0b4` / Normal `#f7c64a` / Hard `#e5484d`), 글자 `#10141d` |
| 선택 가능, 비선택 | 기본 버튼 톤, 글자 `#e8e1cf` |
| 잠김 (`index > MaxSelectableDifficulty`) | 회색 `#232834`, 글자 `#6c7385`. 클릭 가능하며 토스트 |
| 스테이지 자체가 잠김 | 박스 전체 비활성 스타일, Easy 만 선택 표시, 클릭 무반응 |

**클릭 동작**:

| 클릭 대상 | 처리 |
|---|---|
| 선택 가능 세그먼트 | `_wantedDifficulty = index` (즉시 표시 갱신, 효과 문구 갱신) |
| 잠긴 세그먼트 | 저장 변경 없음, 토스트 "{이전 난이도} 클리어 시 해금" (문구 1331, 인자 = 바로 아래 단계 라벨: Normal 잠김이면 Easy, Hard 잠김이면 Normal) |

- 효과 문구의 배율은 `Difficulty.json` 해당 단계 값(`{0:0.##}` 형식). 스테이지 배수와 곱한 최종값은 표시하지 않는다.
- **스테이지/챕터 전환 시 표시 난이도**: ViewModel 이 마지막 명시 선택값 `_wantedDifficulty` 를 들고 표시 난이도 = `Min(_wantedDifficulty, MaxSelectableDifficulty(선택 스테이지))`. 노드를 옮겨 다녀도 선택값이 사라지지 않는다(예: 3-1 에서 Hard 선택 → 미클리어 3-2 로 이동하면 표시 Easy → 3-1 로 돌아오면 Hard 복원). 초기 `_wantedDifficulty = profile.SelectedDifficulty`.
- **확정 시 저장**: "이 스테이지로 선택" 확정 → `SelectedStage` 와 **함께** `SelectedDifficulty = 표시 난이도` 저장(기존 `onConfirm` 경로 확장, 기존 생성자 시그니처 유지해 `WorldMapViewModelTests` 무수정 통과). 확정 없이 닫으면 저장하지 않는다.

### 5.2 월드맵 상세 카드 - 최고 난이도 표시 (선택 스테이지 한정)

- 기록 박스(`RecordPanel`, 기존 328x48) 우측 상단에 "최고 난이도 {라벨}"(글자 11, 해당 난이도 색, 우측 12px, 상단 4px). 표시 조건: 해당 스테이지 `DifficultyClears` 행 존재(`MaxDifficulty >= 1`). 행 없으면 숨김(Easy 만 클리어면 표시 없음). 문구 1332.
- 노드에는 표시하지 않는다(노드는 이미 이름·클리어 태그로 빡빡함).

### 5.3 결과창 (`ResultPopup`)

- `ResultPopupArg` 에 `Difficulty`(이번 판 `EffectiveDifficulty`) 와 `DifficultyUnlocked`(§4.4 `newlyUnlockedDifficulty`, 0 이면 없음) 추가.
- 결과 항목 줄 신규 "난이도 - Normal/Hard": `Difficulty >= 1` 일 때만 표시(Easy 판은 기존 화면 그대로). 위치는 "클리어 시간" 줄 아래(승리) / "영웅 남은 HP" 줄 아래(패배), 값 색은 해당 난이도 색. `ResultRowsData` 에 `ShowDifficultyRow`, `DifficultyText` 추가(View 는 확정값만 표시, Rule 02 §6).
- 해금 배너(승리 한정, `DifficultyUnlocked >= 1`): 항목 줄 아래 폭 472x44 띠, 바탕 `#33300f`→`#14120a`, 테두리 2px `#f7c64a`, 글자 18 `#f7c64a` "{라벨} 해금!" (문구 1333, 인자 = 새로 열린 난이도 라벨).
- "다시 도전" 은 같은 난이도로 재시작(§4.2). 패배 화면은 배너 없음.

### 5.4 기록 팝업 (`RecordsPopup`) - 스테이지 행

- 스테이지 행 우측에 최고 난이도 라벨(Normal/Hard, 글자 14, 해당 난이도 색, 폭 90, 우측 정렬). `DifficultyClears` 행이 있는 스테이지에만 표시, 없으면 빈 칸. 풀링 스크롤 셀의 전적 텍스트 폭을 90 줄여 확보하며, 현행 셀 폭은 `RecordsPopup` 코드 확인 후 gameplay-programmer 가 맞춘다(기존 열을 가리지 않는 것이 조건).
- **2순위(선택)**. 일정 압박 시 §5.1 / §5.3 만으로도 1차 가설 검증은 성립한다.

### 5.5 마을 현상수배서

- `VillageViewModel.IntruderText` ("기사 · 3단계")에 `SelectedDifficulty >= 1` 이면 " · " + 난이도 라벨(Normal/Hard)을 덧붙인다. Easy 는 기존 표시 그대로.
- 마을 진입 시 `SelectedStage` 가 "최대 플레이 가능 스테이지" 로 재설정되는 기존 규칙은 유지한다. 따라서 마을 재진입마다 `SelectedDifficulty` 도 `ClampDifficulty` 로 재보정되며, 최고 도전 지점이 미클리어면 Easy 가 된다. 같은 난이도 반복은 Retry 또는 월드맵 재선택(R4).

### 5.6 Strings_Ko 신규 id (현행 최대 id 1326 다음, 1327~1334)

| id | 문구 | 용도 |
|---|---|---|
| 1327 | `Easy` | `Difficulty.json` nameKey (세그먼트 라벨 · 결과창 값 · 접미 · 기록 행) |
| 1328 | `Normal` | 같음 |
| 1329 | `Hard` | 같음 |
| 1330 | `영웅 HP ×{0} · 공격력 ×{1}` | 세그먼트 박스 효과 문구 |
| 1331 | `{0} 클리어 시 해금` | 잠긴 세그먼트 토스트 (인자 = 아래 단계 라벨) |
| 1332 | `최고 난이도 {0}` | 상세 카드 기록 박스 |
| 1333 | `{0} 해금!` | 결과창 해금 배너 (인자 = 열린 난이도 라벨) |
| 1334 | `난이도` | 결과창 항목 줄 라벨 |

(8개: 1327~1334.) 기존 `Strings_Ko` 무결성 테스트(id 연속·중복·포맷 인자)가 새 id 를 포함해 통과해야 한다. 프리팹 정적 라벨(결과창 "난이도")도 CHText `_stringID` 로 연결한다(Rule 03 §3). 세그먼트 3칸의 라벨은 코드가 `nameKey` 로 채우므로 프리팹 정적 문구가 아니다.

---

## 6. 구현 요청사항 (gameplay-programmer)

- **Enum**: `EData` 에 `Difficulty` 추가(`CommonEnum.cs`). `Difficulty.json` 파일명 = `Difficulty`. 신규 `EUI` / 에셋 키 없음.
- **Interface**: 신규 없음(순수 정적 헬퍼와 데이터 클래스만).
- **에셋 키**: `Assets/_Lair/Data/Json/Difficulty.json` (Addressables 주소 = `Difficulty`, `Stage.json` 과 같은 등록 방식).
- **JSON 스키마**: `DifficultyRow { int difficulty; int nameKey; float hpMultiplier; float powerMultiplier; }` + §2.2 로드 검증. 로드 실패 시 크래시하지 않고 오류 로그 + "Easy 만 존재" 상태로 동작해 세그먼트 박스를 비활성 처리한다(난이도 기능만 비활성, 전투는 정상).
- **세이브 스키마**: `MetaProfile.SelectedDifficulty`(int=0), `MetaProfile.DifficultyClears`(List<DifficultyClearEntry>), `DifficultyClearEntry{Stage, MaxDifficulty}`, `Version = 7`, `CopyFrom` 갱신, `MetaProfileMigration.CurrentVersion = 7` + §3.1 단계 3~5.
- **코드 접점**
  - `Scripts/Meta/MetaProfile.cs`: 필드·엔트리·조회/갱신 헬퍼(§2.3, 갱신 §1.5), `CopyFrom`.
  - `Scripts/Meta/MetaProfileMigration.cs`: §3.1.
  - `Scripts/Battle/StageProgress.cs`: `ScaleHeroStat` 오버로드, `MaxSelectableDifficulty` / `ClampDifficulty` 순수 정적 헬퍼.
  - `Scripts/Battle/HeroStatCalculator.cs`: 난이도 행을 받는 `Compute` 오버로드(기존 시그니처 유지).
  - `Scripts/Battle/BattleController.cs`: `SpawnHero`(~691~794) 읽기, `EndBattle`(~1340~1380) §4.4 순서, Normal/Hard 판 최단 기록·랭킹 제외, `ResultPopupArg` 채우기(~1408), 전투 HUD 제목(~163, 라인 번호는 변동 가능).
  - `Scripts/Village/WorldMapViewModel.cs`: `_wantedDifficulty`, 표시 난이도, 세그먼트 선택 명령, `MaxSelectableDifficulty` 노출, 확정 시 전달(기존 공개 멤버·생성자 유지).
  - `Scripts/UI/Village/WorldMapPopup.cs`: 세그먼트 위젯 와이어링(`[SerializeField] private`, Rule 02 §6.1), 토스트(`HandleNotice` 경로 재사용).
  - `Scripts/UI/ResultPopup.cs`(+ `ResultRowsData` 계산): 난이도 줄·해금 배너.
  - `Scripts/UI/RecordsPopup*.cs`: 기록 행 난이도 (2순위).
  - `Scripts/Village/VillageController.cs` / `VillageViewModel.cs`: 진입 시·`OnStageChanged` 시 `SelectedDifficulty` `ClampDifficulty` 보정, 확정 시 저장, `IntruderText` 접미.
  - 확인 항목: 영웅 스킬 데미지가 영웅 `Power` 를 참조하는지(§1.6). 참조하지 않으면 공격력 배율은 기본 공격에만 적용됨을 보고(수치 변경 금지).
- **프리팹**(UI 배선은 목업 승인 후):
  - `Assets/_Lair/Art/UI/WorldMapPopup.prefab`: 하단 바에 난이도 박스(세그먼트 3 + 효과 문구) 신규, `_bottomText` 폭 700 → 480 + 글자 12 + 2줄, 상세 카드 기록 박스에 "최고 난이도" CHText 신규.
  - `Assets/_Lair/Art/UI/ResultPopup.prefab`: 난이도 줄(라벨+값), 해금 배너 신규.
  - `RecordsPopup` 셀 프리팹: 난이도 텍스트 열(2순위).
  - 모든 TMP 에 CHText 부착, 버튼은 CHButton, 코드 동적 생성 금지, 신규 `[SerializeField]` 는 하나도 비우지 않는다(`WorldMapStringsAndPrefabTests` 가 검사).
- **문서 반영**(메인 `[docs]` 소작업): `CLAUDE.md` §8, 컨셉서 §11 범위 표에 한 줄. 문구 초안은 §9.

---

## 7. 테스트 항목 (test-engineer)

**EditMode**

| 영역 | 항목 |
|---|---|
| 난이도 데이터 | 출하 `Difficulty.json` 3행 로드 / 0·1·2 / Easy 가 1.0·1.0 / 단조 비감소 / `nameKey` 존재. 검증 위반(행 수 2·4, 중복, 역전, 0 이하, Easy ≠ 1.0)이 각각 로드 오류 |
| 능력치 | `Compute`(Easy) 가 기존 결과와 비트 단위 동일(기존 `HeroStatTableRegressionTests` 무수정 통과). Normal/Hard × 직업 × 스테이지 대표 표본에서 `ScaleHeroStat(앵커, 직업 × 스테이지 × 난이도)` 1회 반올림 기댓값 일치. 반올림 경계(.5) 케이스. 난이도 적용 후에도 HP 최소 1 |
| 해금 | `MaxSelectableDifficulty`: 미클리어 → 0 / 클리어(행 없음) → 1 / 행 MaxDifficulty 1 → 2 / 행 MaxDifficulty 2 → 2(상한). `ClampDifficulty`: 음수 → 0, 상한 초과 → 상한 |
| 기록 갱신 | Hard 승리(기존 Normal) → 2 / Normal 승리(기존 Hard) → 2 유지 / Easy 승리 → 행 불변 / 패배 → 불변 / 행 없을 때 새 행 생성 / `MaxDifficulty >= 1` 불변식. 스테이지별 독립 |
| 해금 배너 값 | `newlyUnlockedDifficulty`: 미클리어 Easy 승리 → 1 / Normal 최초 클리어 → 2 / Hard 클리어 → 0 / 이미 깬 난이도 재클리어 → 0 |
| 마이그레이션 | v6 JSON 로드 → Version 7, `SelectedDifficulty` 0, `DifficultyClears` 빈 리스트. `CurrentVersion == 7`. v7 재적용 멱등. 정리 규칙: 범위 밖 `Stage`·`MaxDifficulty < 1`·`Stage > ClearedStage` 행 제거, 중복 `Stage` 최댓값 병합, `SelectedDifficulty` 음수 → 0. 난이도 데이터 미로드 상태 호출에서 상한으로 자르지 않음 |
| 클라우드 | `CopyFrom` 이 `SelectedDifficulty`·`DifficultyClears` 를 복사(기존 필드와 같은 규약 - other 리스트 대입, null 이면 빈 리스트). v6 형태 프로필 복사 시 Easy/빈 상태. 복원 후 Migration 재적용으로 범위 밖 행 정리 |
| 월드맵 VM | 노드 이동 시 표시 난이도 클램프 / 되돌아오면 `_wantedDifficulty` 복원 / 잠긴 세그먼트 토스트 문구와 인자 / 확정 시 `SelectedDifficulty` 전달 / 닫기(미확정) 시 미저장. 기존 `WorldMapViewModelTests`·`WorldMapViewModelBehaviorTests` 무수정 통과 |
| 전투 읽기 | `EffectiveDifficulty`: `SelectedDifficulty` 가 해금 초과일 때 클램프(프로필 비수정) / Retry 시나리오(같은 프로필 두 번 읽기)에서 동일 난이도 |
| 제외 규칙 | Normal/Hard 승리가 `BestClearTime`·`StageRecordEntry.BestClearTime`·`IsNewStageBest` 를 건드리지 않음, `Runs`/`Wins` 는 증가 |
| 문자열 | `Strings_Ko` 1327~1334 존재 / 포맷 인자 개수 일치 / 기존 무결성 테스트 통과 |
| 프리팹 | `WorldMapStringsAndPrefabTests` 무수정 통과(신규 `[SerializeField]` 미연결 0). `ResultPopup` 프리팹 신규 참조 연결 |

**PlayMode (수동 확인)**: 월드맵에서 Normal 선택 → 출격 → 영웅 HP 기대값 → Retry 시 난이도 유지 → 승리 후 결과창 해금 배너 → 월드맵 재오픈 시 Hard 열림 → 앱 재시작 후 저장 유지 → 클라우드 복원 후 유지.

---

## 8. 밸런스 위험 · QA 후속

### 8.1 위험 요약

| # | 위험 | 영향 | 완화 |
|---|---|---|---|
| R1 | (재계산) 3-5·5-5 보스 Hard 에서 M 이 가정(1.16)보다 낮으면 300초 초과 (M 1.10 → 3-5 Hard 301.4초) | Hard 가 일부 보스에서 클리어 불가 | 게이트 D3. 조정은 `Difficulty.json` 숫자만 |
| R2 | Power 배율의 비선형 효과(보드 압력 감소 → 시간 급증) | 단계 간 곡선 불연속 | HP-only / Power-only 분리 측정(D2), Power 폭을 HP 보다 낮게 시작 |
| R3 | 보상 없는 난이도(소울·XP 가산 0) | 해금 외 동기 부재 → 사용률 저조. 3단계는 단계가 적어 "깨면 끝" 이 빨라 더 두드러짐 | 요구 확정이 "능력치 배율만". 출시 후 Normal/Hard 선택률·재도전 횟수를 측정하고, 2차 유물 드랍(난이도별 확률/등급)이 보상이 된다. 소울 가산이 필요하면 별도 승인 요청 |
| R4 | 마을 재진입 시 선택 난이도가 최고 도전 지점의 Easy 로 초기화 | 같은 난이도 반복 시 월드맵 재조작 필요 | 기존 `SelectedStage` 리셋 규칙의 귀결. Retry 로 반복 가능. 불만이 크면 후속에서 "진입 시 마지막 난이도 유지" 별도 기획 |
| R5 | Normal/Hard 판이 랭킹·최단 기록에서 제외 | 높은 난이도에 경쟁 요소 없음 | 의도(§1.6). 난이도별 랭킹은 Firestore 규칙 변경 필요한 별도 범위 |
| R6 | 영웅 HP 증가로 HP 바·데미지 팝업 자릿수 증가 (5-5 Hard 곱 2.925) | 표시 잘림 | 구현 시 HP 바/팝업 텍스트 폭 수동 확인 |
| R7 | 초반 스테이지(1-1 등)의 Hard 가 "쉬운 Hard" (169초) | 난이도 선택의 도전성 편차 | §1.3 트레이드오프. D 지표에서 확인 후 `Stage_Difficulty` 도입 제안 |
| R8 | "Easy" 라벨이 기존 유저에게 낮은 난이도로 읽혀 기본값 선택에 거부감 | 첫인상 | 기본 선택 Easy 는 사용자 요청. 라벨 변경은 Strings_Ko 1327 한 줄 수정으로 가능 |

### 8.2 qa-simulator 후속 제안 (구현 + 테스트 완료 후)

- **표본**: 스테이지 6개(1-1, 1-5, 2-3, 3-5, 4-5, 5-5) × 난이도 3 × 봇 프로필 5종(G0 와 동일) × 100런 = 9,000런. 메타 레벨은 각 스테이지 도전 시점의 `M` 대응 상점 진행도. 시드 고정(런 i 의 시드 = i). 3-5·5-5 는 M 을 1.10 / 1.16 / 1.25 세 값으로 추가 측정해 R1 의 M 가정을 확정한다.
- **게이트**:

| # | 지표 | 통과 기준 | 실패 시 조치 |
|---|---|---|---|
| D1 | Easy 회귀 | 모든 표본에서 도입 전과 영웅 능력치 완전 일치, 같은 시드 비교에서 승률·처치 시간 차이 0 | 코드 버그 - 재작업 |
| D2 | 단계 간 처치 시간 증가율 (스테이지별 평균 처치 시각 Normal÷Easy, Hard÷Normal) | 비보스 스테이지: Normal÷Easy +10~20%, Hard÷Normal +8~18%, 단조 증가 | 구간 밖이면 해당 단계 배율 재산출 (증가율 목표 중앙: Normal +15%, Hard +13%) |
| D3 | 승률 하한 (M 기준) | 비보스: Normal ≥ 65%, Hard ≥ 40% / 보스: Normal ≥ 50%, Hard ≥ 20% | 미달 단계 배율을 낮춤. 보스 Hard 클리어율 0% 확정이면 사용자에게 Hard 보스 배율 인하·`Stage_Difficulty` 도입 에스컬레이션 |
| D4 | 프로필별 편차 | 각 봇 프로필 평균 처치 시각이 같은 (스테이지, 난이도) 평균의 ±25초 이내 (G0-3 와 동일) | 난이도가 특정 빌드 축을 불리하게 하는지 분석 |
| D5 | 처치 시각 하한 | 모든 (스테이지, 난이도) p10 ≥ 75초, 최소 ≥ 30초 | 영웅을 단단하게만 하므로 위반은 비정상 - 버그 점검 |
| D6 | 카드 픽 간격 | 난이도별 평균 패시브 픽 수가 Easy 대비 하락하지 않고 액티브 픽 수가 증가 | 페이싱 이상이면 보고 |
| D7 | 쉬운 Hard 판정 | 1-1 등 초반 스테이지 Hard 평균 처치 시각이 Easy 대비 +25% 이상 | 미달이면 R7 대응 제안 |

- 결정 메트릭이 D2·D3 이므로 **`Difficulty.json` 배율의 최종값은 이 리포트 이후 확정**한다. §1.2 표는 초기값이다.
- 분리 측정: 배율을 (HP만 / Power만 / 둘 다) 3조건으로 6개 스테이지 × Hard 에서 각 200런 - R2 판정용.

### 8.3 엣지 케이스 정리

| # | 케이스 | 처리 |
|---|---|---|
| E-1 | 스테이지 변경 시 `SelectedDifficulty` | 월드맵: 표시값만 클램프, `_wantedDifficulty` 유지(§5.1). 마을 캐러셀/진입: `SelectedStage` 를 쓰는 모든 경로가 `SelectedDifficulty` 를 `ClampDifficulty` 로 같이 보정 |
| E-2 | 해금 안 된 난이도가 저장돼 있음 (클라우드 복원·수동 편집) | 전투는 `EffectiveDifficulty` 로 방어(§4.3), 월드맵 열 때 표시 클램프, 확정 시 정규화 저장 |
| E-3 | `Difficulty.json` 이 3행이 아님 (조정 실수) | 로드 오류, Easy 만 존재로 동작, 세그먼트 박스 비활성(§2.2) |
| E-4 | `Difficulty.json` 로드 실패 | 오류 로그, Easy 만 존재로 동작. 전투·월드맵 나머지 기능 정상 |
| E-5 | 구버전(v6) 프로필 | 필드 부재 → 기본값(Easy), 마이그레이션 v7 승격(§3.1). 기존 유저는 클리어한 모든 스테이지에서 Normal 이 열림(§1.4) |
| E-6 | `DifficultyClears` 에 `Stage > ClearedStage` 행 | 마이그레이션 정리에서 제거(§3.1-4) |
| E-7 | 같은 `Stage` 중복 행 | 최댓값 하나로 병합 |
| E-8 | 승리 중 `EffectiveDifficulty` 가 판 도중 바뀜 | 불가 - 전투 시작에 확정, 판 도중 다시 읽지 않음 |
| E-9 | Retry 중 해금 난이도가 늘어남 | 선택 난이도는 그대로(자동 상승 없음) |
| E-10 | 전투 중 뒤로가기 종료 | 기록 갱신 없음(`EndBattle` 승리 분기를 타지 않음) |

---

## 9. CLAUDE.md §8 승격 문구 초안 (사용자 승인 후 메인이 반영)

§8 의 챕터 구조 허용 항목 끝 문장

> **★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.**

을 다음으로 교체한다.

> **★ 목표, 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.**

그리고 "서버 연동 허용" 항목 앞에 다음 항목을 추가한다.

> - **난이도 선택(Easy / Normal / Hard) 1차 허용 (2026-10-19 v0.3 승격) - 단 영웅 능력치 배율에 한정** - 스테이지 입장 전 월드맵 팝업(마을 HUD 안)에서 난이도를 직접 고르며, Normal/Hard 는 **영웅 체력·공격력 배율만** 상승한다(몬스터 제한·신규 스테이지 규칙 없음). 기본 선택은 Easy(기존 수치와 동일). 해금은 스테이지별로 "Easy 클리어 → Normal, Normal 클리어 → Hard". 허용 범위는 ① `Difficulty.json` 배율표 ② `MetaProfile` 의 스테이지별 최고 클리어 난이도와 선택 난이도 저장(Version 7)·클라우드 동기 호환 ③ 월드맵 팝업·결과창·기록·마을 표기의 난이도 표시. Normal/Hard 판은 최단 기록·랭킹에서 제외한다. **유물은 난이도 1차 완료 후 2차로 개별 승격 예정이며 그때까지 금지**(난이도가 유물 드랍 확률/등급에 쓰이는 것은 2차 범위). ★ 목표·던전 편성·무한 탑·데일리 금지는 유지. 별도 메인 메뉴/세팅 화면 금지는 유지(난이도 선택은 월드맵 팝업 안).

`docs/design/chapter-system.md` 129행, `chapter-5.md` 14행, `chapter-3-hero.md` 408행, `chapter-4.md` 14행의 "악몽(Heat)" 금지 문구는 당시 승격 기록이라 **수정하지 않는다**. 컨셉서 §11 범위 표에는 "난이도 선택(Easy/Normal/Hard, 영웅 능력치 배율) 1차 - v0.3 승격 2026-10-19" 한 줄을 추가한다.

---

## 10. 결정 요약 (사용자 확인 필요 항목)

| ID | 결정 | 대안 | 확인 필요 이유 |
|---|---|---|---|
| D-1 | 3단계, Normal HP 1.15 / 공격력 1.10, Hard HP 1.30 / 공격력 1.22 (§1.2) | 초안의 6단계 | 수치는 초기값, QA 후 확정 |
| D-2 | Normal/Hard 판은 최단 기록·랭킹 제외 (§1.6, 3단계 재검토 후 유지) | 난이도별 랭킹 | 랭킹 의미 보존. 반대로 가려면 Firestore 규칙 작업 필요 |
| D-3 | 컨트롤 = 하단 바 3단 세그먼트 (§5.1) | −/+ 스테퍼, 상세 카드 내 배치 | 목업 승인 항목 |
| D-4 | 소울·XP 보상 가산 없음 (§1.6, R3) | 난이도별 소울 +N% | 요구 확정과 일치, 동기 리스크 수용 |
| D-5 | 마을 재진입 시 난이도 재보정 (§5.5, R4) | 마지막 난이도 유지 | 기존 `SelectedStage` 리셋 규칙과 일관 |
| D-6 | 기존 유저는 마이그레이션 직후 클리어한 스테이지에서 Normal 열림 (§1.4) | 전 스테이지 Easy 재클리어 요구 | Easy = 기존 게임이므로 기존 클리어를 Easy 클리어로 간주 |
