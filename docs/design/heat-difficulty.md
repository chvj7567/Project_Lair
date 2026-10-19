# Heat (직접 고르는 난이도) 1차 - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-19 · 단계: v0.3
> 입력: `C:\Users\Tae\.claude\plans\reactive-wishing-newt.md` · `chapter-system.md` · `worldmap-popup-redesign.md` · `village-meta-hub.md` · `project_lair_concept.md` · 코드 `HeroStatCalculator.cs` / `MetaProfile.cs` / `MetaProfileMigration.cs` / `BattleController.cs`(SpawnHero ~691, EndBattle ~1340) / `WorldMapViewModel.cs` / `Stage.json`
> **UI 목업**: `.mockups/heat-select.html` (사용자 승인 게이트 대상 - 월드맵 하단 바 Heat 스테퍼, 결과창 줄/해금 배너, 기록 행)
> **데이터 구조표**: 본 문서 §2 (사용자 승인 게이트 대상)

---

## § 헤더

- **목표**: 스테이지 입장 전 플레이어가 **Heat 단계(0~5)** 를 직접 골라, 같은 스테이지를 영웅 체력·공격력 배율만 높인 채 재도전할 수 있게 한다. 해금은 스테이지별로 "Heat N 클리어 -> Heat N+1 선택 가능".
- **검증 가설**: (1) 클리어한 스테이지를 더 어렵게 다시 하는 선택지가 "영웅이 너무 쉽다" 문제를 해소하고 마을 외 재도전 동기를 만드는가. (2) 영웅 능력치 배율만으로 단계 간 체감 난이도 차(클리어 시간 증가 +8~15%/단계, 승률 하락)가 일정하게 나오는가. (3) 2차(유물)가 얹힐 때 Heat 가 난이도 상쇄 장치로 기능할 수 있는 기반(스테이지×Heat 클리어 기록)이 되는가.
- **현재 단계 범위 적합성**: **범위 밖이었던 항목의 명시 승격 요청 - 사용자 승인 필요.** CLAUDE.md §8 은 "악몽(Heat)" 을 개별 승격 전까지 금지한다. 본 기획서가 §9 에 승격 문구 초안을 제안하며 **승인 전에는 구현에 착수하지 않는다.** 유물·★ 목표·무한 탑·데일리·던전 편성은 계속 금지(본 문서에 포함하지 않음). 서버 연동은 Firebase 클라이언트 코드 범위 유지 - 신규 서버 코드 0. 메인 메뉴/세팅 화면 금지 유지(Heat 선택은 기존 월드맵 팝업 안).
- **핵심 메커니즘**: (a) `Heat.json` 6행(0~5)이 영웅 HP/공격력 **곱 배율**을 제공 - 스테이지 JSON 은 건드리지 않고 `영웅 최종 스탯 = ScaleHeroStat(앵커, 직업 배율 × 스테이지 배수 × Heat 배율)` 1회 반올림. (b) 스테이지별 최고 클리어 Heat 를 `MetaProfile.HeatClears` 에 저장, 선택 가능 상한 = 클리어한 최고 Heat + 1. (c) Battle 은 `MetaSession` 에서 매 전투 `SelectedHeat` 를 재독(Retry 가 마을을 거치지 않음).

---

## 1. 룰·수치

### 1.1 단계 수 - 대안 비교

| 안 | 내용 | trade-off |
|---|---|---|
| A. 0~3 (4단계) | 단계 폭 큼 | 단계가 적어 마스터 후 할 게 빨리 끝남. 2차에서 유물 보정 폭을 흡수할 여유 부족 |
| **B. 0~5 (6단계, 권장)** | Heat 0 = 기본 + 선택 5단계 | 검증 매트릭스 25스테이지 × 6 = 150칸(대표 6스테이지 표본으로 축소 가능, §8.2). 단계당 HP +8~14%p 로 체감 가능(130초 스테이지에서 +13~16초)한 최소 폭. 2차에서 상한을 늘릴 때 JSON 행 추가만 필요 |
| C. 0~10 | 세분화 | 능력치 2종뿐이라 단계 간 차가 +5% 이하로 체감 불가, QA 매트릭스 250칸 |

**결정: B (Heat 0~5).** 최대 단계는 `Heat.json` 행 수에서 도출한다(코드 상수 금지). 행이 늘면 UI·해금이 자동 확장된다.

### 1.2 Heat 배율표 (`Heat.json` 초기값, 튜닝 대상)

| heat | hpMultiplier | powerMultiplier | 설명 |
|---|---|---|---|
| 0 | 1.00 | 1.00 | 기본. 기존 동작과 비트 단위 동일(회귀 기준) |
| 1 | 1.10 | 1.08 | |
| 2 | 1.20 | 1.16 | |
| 3 | 1.32 | 1.26 | |
| 4 | 1.46 | 1.38 | |
| 5 | 1.60 | 1.50 | |

- HP 증가폭: +0.10, +0.10, +0.12, +0.14, +0.14 (후반 단계 가속). Power 증가폭: +0.08, +0.08, +0.10, +0.12, +0.12.
- **Power 를 HP 보다 낮게 둔 이유**: 영웅 공격력이 오르면 몬스터가 더 빨리 죽어 보드 압력이 줄고, 그 결과 영웅 처치 시간이 HP 비율보다 더 늘어난다(비선형). HP 만큼 올리면 후반 단계가 클리어 불가로 뛰므로 한 단계 아래로 시작한다. HP-only / Power-only 민감도는 §8.2 QA 에서 분리 측정한다.
- **곱연산 관계**: 최종 배율 = 챕터 영웅 직업 배율 × `Stage.json` 배수 × Heat 배율. 곱셈 한 번에 묶어 **1회만 반올림**(중간 반올림 금지, `chapter-system.md` §2.1 규칙 유지). Heat 0 은 ×1.00 이라 기존 수치가 그대로다.

곱연산 예시 (Stage 배수는 `Stage.json` 현행 값, 직업 배율은 별도 곱):

| 스테이지 | Stage hp / power | Heat 5 곱 (hp / power) | 산식 |
|---|---|---|---|
| 1-1 | 1.00 / 1.00 | 1.600 / 1.500 | 1.00×1.60, 1.00×1.50 |
| 1-5 | 1.92 / 1.50 | 3.072 / 2.250 | 1.92×1.60, 1.50×1.50 |
| 3-5 | 3.20 / 1.75 | 5.120 / 2.625 | 3.20×1.60, 1.75×1.50 |
| 5-5 | 2.25 / 1.95 | 3.600 / 2.925 | 2.25×1.60, 1.95×1.50 |

### 1.3 시간 예산 점검 (초기값의 근거)

`chapter-system.md` §0.4 의 시간 예산(5분 = 300초)에 Heat HP 배율을 곱해 본다. 기대 처치 시간 ≈ `targetClearSec` × Heat HP 배율 ÷ `M_ref`(상점 보조 계수 1.00~1.16, `M_max` 1.35). 아래는 `M_ref` 를 1.00 으로 둔 상한 추정이다.

| 스테이지 (targetClearSec) | H0 | H1 | H2 | H3 | H4 | H5 |
|---|---|---|---|---|---|---|
| 1-1 (130) | 130 | 143 | 156 | 171.6 | 189.8 | 208 |
| 1-5 (240) | 240 | 264 | 288 | 316.8 | 350.4 | 384 |
| 5-5 (260) | 260 | 286 | 312 | 343.2 | 379.6 | 416 |

검산: 130×1.32 = 171.6 / 240×1.20 = 288 / 260×1.60 = 416.

- **의도된 결과**: 초반 스테이지는 Heat 5 까지 300초 안에 클리어 가능, **보스 스테이지는 상점 만렙 보조(M 1.35 이내)를 받아도 Heat 3~4 부근에서 300초 천장에 닿는다**(1-5: 240×1.46÷1.07 = 327.5초 > 300 이므로 M_ref 1.07 기준 Heat 4 는 시간 초과 위험). 보스 최고 Heat 는 "1차 단독 목표치"가 아니라 2차(유물)가 열어주는 상위 영역으로 둔다.
- **리스크**: 1차 단독 출시에서 보스 Heat 4~5 가 사실상 도달 불가면 "열렸는데 못 깨는 단계"가 된다. §8.2 게이트 H3 가 이를 수치로 판정하고, 미달 시 조정 레버는 **`Heat.json` 숫자뿐**이다(코드 변경 0).
- 대안(보스 한정 별도 배율 `Stage_Heat` 관계 데이터)은 YAGNI 로 보류 - H3 에서 보스 클리어 가능 최고 Heat 가 2 미만으로 나오면 그때 도입을 제안한다.

### 1.4 해금 규칙

- 스테이지 s(선형 번호)에서 **선택 가능 최대 Heat** `MaxSelectableHeat(s)`:
  - `s > ClearedStage` (해당 스테이지 미클리어): **0**
  - `s <= ClearedStage`: `Min(최대 Heat, 클리어한 최고 Heat(s) + 1)`. 클리어한 최고 Heat(s) 는 `HeatClears` 에 s 의 행이 있으면 그 `MaxHeat`, 없으면 0 (= Heat 0 만 클리어)
- 즉 Heat 0 클리어(= `ClearedStage` 갱신) 가 Heat 1 을 열고, Heat N 클리어가 Heat N+1 을 연다. 최대 단계(5)를 클리어하면 더 열 것이 없다(상한 5 유지).
- 스테이지끼리 독립: 1-1 Heat 3 클리어가 1-2 Heat 해금에 영향 주지 않는다. 신규 스테이지 해금은 기존 `StageProgress.IsUnlocked` 그대로(Heat 0 클리어로 진행).
- 챕터 출격 게이트(`ChapterGate`)는 Heat 와 무관하다.

### 1.5 최고 Heat 기록 갱신 규칙

| 상황 | 처리 |
|---|---|
| 승리, 선택 Heat h >= 1, h > 해당 스테이지 기존 `MaxHeat`(행 없으면 0) | `HeatClears` 에서 s 행의 `MaxHeat = h` 로 갱신, 행 없으면 새로 추가 |
| 승리, h <= 기존 `MaxHeat` | 변경 없음 (낮은 Heat 재클리어가 기록을 내리지 않는다) |
| 승리, h = 0 | `HeatClears` 변경 없음 (`ClearedStage` 갱신만) |
| 패배 / 시간 초과 | 변경 없음 |
| 전투 중 뒤로가기 → 마을 복귀(저장 없이 종료, 기존 동작) | 변경 없음 |

- `HeatClears` 의 행은 **Heat >= 1 을 클리어한 스테이지에만 존재**한다 (`MaxHeat >= 1` 불변식). "클리어한 Heat 없음" 을 -1/0 으로 채운 행을 만들지 않는다.
- 전투에 적용되는 Heat 는 **전투 시작 시점에 확정**한 `EffectiveHeat`(§4.3) 이며 판 도중 변하지 않는다. 승리 시 기록 갱신은 이 값으로 한다.

### 1.6 Heat 가 건드리지 않는 것 (범위 고정)

- 소울·영주 XP 보상(`SoulRewardCalculator`) - Heat 가산 없음. 근거: 사용자 확정 요구가 "영웅 체력·공격력 배율만"이며, 보상 연동은 2차(유물 드랍 확률/등급)에서 정한다. **보상이 없는 1차의 동기 리스크는 §8.3 에 기록.**
- 몬스터 능력치·스폰·스테이지 규칙·카드·액티브/패시브 임계 - 변경 없음. 패시브(영웅 HP 10% 구간)·영웅 스킬 게이트(85/65/45%)는 HP **비율**이라 Heat 로 불변.
- 도전과제·도감 - 변경 없음.
- **랭킹·최단 기록(Heat 0 한정)**: Heat >= 1 판은 `MetaProfile.BestClearTime`, `StageRecordEntry.BestClearTime`, `IsNewStageBest`, `SubmitRanking` 에서 **제외**한다. 근거: 같은 스테이지라도 Heat 가 다르면 클리어 시간이 달라 비교 불가이며, 최단 기록 랭킹이 Heat 0 기준으로 남아야 기존 리더보드 문서의 의미가 유지된다. 전적 `Runs`/`Wins` 카운트는 Heat 무관하게 올린다.
  - 대안: Heat 별 랭킹 분리(복합키 `stage + heat`) - Firestore 규칙·인덱스 변경이 필요해 이 레포 범위 밖, 기각.
- 영웅 기본 공격력(`Power`) 외에 스킬 데미지(`hero_skills.json`)가 Power 를 참조하는지 여부는 코드 확인 후 보고한다 (참조하지 않으면 Heat 공격력 배율은 기본 공격에만 적용 - 구현 요청사항 §6 확인 항목).

---

## 2. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 콘텐츠·밸런스 수치 = JSON(`Assets/_Lair/Data/Json/`, 파일명 = Enum 값명). 세이브 데이터 = `MetaProfile` 엔트리 리스트(JsonUtility 직렬화, `StageRecordEntry` 선례). sentinel(-1/0/null)로 "관계 없음" 을 표현하지 않는다.

### 2.1 엔티티 / 관계 트리

```
Heat (엔티티, JSON)  HeatRow            <- "난이도 단계 하나". 스테이지와 무관한 공통 배율표
   (스테이지별 예외 배율이 필요해지면 선택적 관계 Stage_Heat 를 신설 - 이번엔 없음, §1.3)

MetaProfile (세이브 루트)
 |-- SelectedHeat : int                  <- 필수 1:1 스칼라. 0 은 "Heat 0 단계" 라는 실제 값 (sentinel 아님)
 |
 +-- HeatClears : List<HeatClearEntry>   <- Stage 의 선택적 관계 (Heat>=1 클리어한 스테이지만 행 존재)
        HeatClearEntry { Stage (PK, 선형 스테이지 번호), MaxHeat (>=1) }

참조: HeatClearEntry.Stage -> StageRow (선형 번호, StageRecordEntry.Stage 와 동일 키 규약)
      MetaProfile.SelectedHeat / HeatClearEntry.MaxHeat -> HeatRow.heat
```

### 2.2 필드 표

**HeatRow** - `Heat.json` (EData `Heat`). 핵심 엔티티: "난이도 단계 하나". 단일 PK(그룹 내 순서 구조가 아니라 전역 단계 번호 하나이므로 복합 키 불필요).

| 필드 | 타입 | 키/관계 | 설명 · 값 |
|---|---|---|---|
| `heat` | int | PK | 0~5, 0부터 연속 (§1.2 표) |
| `hpMultiplier` | float | | 영웅 HP 곱 배율. Heat 0 은 1.00 |
| `powerMultiplier` | float | | 영웅 공격력 곱 배율. Heat 0 은 1.00 |

- 로드 검증(위반 시 로드 오류, `ChapterDatabase` 오류 수집 패턴과 동일): (1) `heat` 가 0부터 빠짐·중복 없이 연속, (2) `heat = 0` 행이 정확히 1.0 / 1.0, (3) 두 배율 모두 `heat` 증가에 대해 단조 비감소, (4) 두 배율 > 0.
- 표시용 문구는 JSON 에 두지 않는다 - UI 가 `Strings_Ko` 포맷("Heat {0}")으로 만든다 (nameKey 필드 불필요, 단계 이름이 숫자뿐).

**MetaProfile 신규 필드** (`Scripts/Meta/MetaProfile.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `SelectedHeat` | int | 0 | 마지막으로 확정한 Heat. 로컬 선호값이나 `SelectedStage` 와 동일하게 `CopyFrom` 이 함께 복사 |
| `HeatClears` | `List<HeatClearEntry>` | 빈 리스트 | 스테이지별 클리어한 최고 Heat. 클라우드 복원 대상 |
| `Version` | int | 6 -> **7** | 스키마 추가에 따른 승격 |

**HeatClearEntry** (`MetaProfile.cs` 안, `[Serializable]`, `StageRecordEntry` 옆)

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `Stage` | int | PK | 선형 스테이지 번호 1~MaxStage |
| `MaxHeat` | int | | 이 스테이지에서 클리어한 최고 Heat. 불변식 `>= 1` |

> 명명 메모: `HeatClearEntry` 는 **게임 콘텐츠 JSON 관계 데이터가 아니라 세이브 엔트리**라 §11-1-5 의 `<부모>_<관계명>` JSON 명명 규칙이 아니라 기존 `StageRecordEntry` / `CardPickCountEntry` 선례를 따른다.

### 2.3 조회 헬퍼 (의도만, 시그니처는 gameplay-programmer 판단)

- `GetMaxClearedHeat(stage)`: `HeatClears` 에 행이 있으면 `MaxHeat`, 없으면 0 (조회 부수효과 0, `GetStageRecord` 와 같은 규약. 반환 0 은 "Heat 0 까지 클리어" 의미이며 저장되는 값이 아니다. 호출부가 `stage <= ClearedStage` 를 먼저 확인한다)
- `MaxSelectableHeat(stage, clearedStage, maxHeatLevel)`: §1.4 정의. 순수 정적 함수로 `StageProgress` 옆에 둔다(해금 판정 단일 소유 원칙, `worldmap-popup-redesign.md` §6).
- `ClampHeat(heat, stage, clearedStage, maxHeatLevel)`: `Clamp(heat, 0, MaxSelectableHeat)`. 모든 읽기 지점이 이 함수를 쓴다.

---

## 3. 마이그레이션 · 클라우드

### 3.1 `MetaProfile` Version 6 -> 7

| 단계 | 규칙 (`MetaProfileMigration.Apply`, 멱등) |
|---|---|
| 1 | `CurrentVersion = 7`. `Version < 7` 이면 7 로 승격 |
| 2 | 기존 단계(진행 범위 보정, 전적 정리, `SelectedHero`) 유지 |
| 3 | `HeatClears` null 이면 빈 리스트 |
| 4 | `HeatClears` 정리: 행이 null / `Stage < 1` / `Stage > MaxStage` / `MaxHeat < 1` 이면 제거. **`Stage > ClearedStage` 인 행도 제거**(미클리어 스테이지에 Heat 기록이 있을 수 없음). 같은 `Stage` 중복이면 `MaxHeat` 최댓값 한 행만 남김 |
| 5 | `SelectedHeat < 0` 이면 0 으로 보정 |

- **Heat 상한 클램프는 마이그레이션에서 하지 않는다.** 이유: 기존 마이그레이션이 "데이터 로드 전 호출 시 기본 `MaxStage` 로 잘리는" 경로를 이미 방어하고 있다(`heroIdOfLinearStage == null && Version >= CurrentVersion` 이면 조기 반환). Heat 최대 단계도 `Heat.json` 로드 전에는 알 수 없으므로 마이그레이션에서 상한으로 자르면 로드 순서에 따라 정상 진행 기록을 지울 위험이 있다. 상한 보정은 읽는 순간 `ClampHeat`(§2.3) 가 담당한다.
- v6 세이브: 필드 부재 -> `SelectedHeat = 0`, `HeatClears` 빈 리스트로 로드 (기존 유저는 전 스테이지 Heat 0 부터, 데이터 이전 코드 불필요).
- 갱신 필요 테스트: `Tests/EditMode/Chapter/MetaProfileMigrationTests.cs` 의 버전 기대값 6 -> 7 (§7).

### 3.2 클라우드 동기

- `MetaProfile.CopyFrom` 에 `SelectedHeat`, `HeatClears`(`?? new List<HeatClearEntry>()`) 추가 필수 (빠뜨리면 복원 시 Heat 진행만 유실 - `StageRecords` 와 같은 함정).
- 업로드 페이로드의 `schemaVersion` 은 `profile.Version` 을 그대로 쓰므로(`FirebaseSdkApiClient` 175행) 7 로 자동 승격된다. 별도 상수 없음.
- 복원 직후 `MetaProfileMigration.Apply` 를 동일 함수로 재적용(기존 규칙 유지) -> `HeatClears` 정리(§3.1-4)가 복원 데이터에도 적용된다.
- v6 문서를 v7 클라이언트가 복원하면 Heat 필드가 없어 빈 상태가 된다 = 복원은 문서 기준으로 전체 대체되는 기존 동작과 동일(충돌 UX 배지·복원 권유 변경 없음). v7 문서를 v6 클라이언트가 복원하면 Heat 필드는 무시된다(구버전 코드, 알려진 제약 - `chapter-system.md` §2.2 와 동일 처리, 신구 클라이언트 동시 배포).
- `SelectedHeat` 는 `SelectedStage` 와 동일하게 "로컬 선호값이지만 일관성 위해 함께 복사" 한다. 복원된 `SelectedHeat` 가 해금 범위를 넘어도 §4.3 의 `EffectiveHeat` 계산이 방어한다.

---

## 4. 전투 적용

### 4.1 능력치 계산

- `HeroStatCalculator.Compute` 에 Heat 행을 받는 경로를 추가한다. `HP = ScaleHeroStat(앵커 HP, 직업 hpScale × stage.hpMultiplier × heat.hpMultiplier)`, `Power` 도 동일. `range` / `cooldown` / `moveSpeed` 는 직업 값 그대로(Heat 무관).
- **기존 4인자 `Compute` 시그니처는 유지**(Heat 0 경로, 기존 테스트 12개 이상이 이 시그니처를 호출) - 신규 오버로드가 Heat 행을 추가로 받는다. `StageProgress.ScaleHeroStat` 도 곱 하나를 더 받는 오버로드를 추가한다(기존 시그니처 유지). 중간 반올림 금지, decimal 계산 규약 유지.
- 호출처: `BattleController.SpawnHero` (~691~794행: `archetype`/`stageRow` 조회 후 `ApplyHeroStats(..., HeroStatCalculator.Compute(...))`).

### 4.2 Heat 읽기 경로 (Retry 대응)

- Battle 은 이미 `MetaSession.GetOrLoad().SelectedStage` 를 전투마다 읽는다. `SelectedHeat` 도 **같은 지점에서 같은 방식으로 읽는다** (`SpawnHero` 시점). Retry(`ResultPopup.OnClickRetry`)는 마을을 거치지 않고 Battle 을 재시작하므로 이 방식이면 자동으로 같은 Heat 가 유지된다.
- Retry 시 Heat 자동 상승 없음(방금 해금된 Heat N+1 로 올라가지 않고 같은 N 유지). 올리려면 월드맵에서 선택.

### 4.3 `EffectiveHeat` (방어 로직)

전투 시작 시 `EffectiveHeat = ClampHeat(profile.SelectedHeat, profile.SelectedStage, profile.ClearedStage, 최대 Heat)` 를 계산해 그 전투의 능력치·결과 팝업·기록 갱신에 일관되게 쓴다. 이유: 클라우드 복원·수동 편집 등으로 해금되지 않은 Heat 가 `SelectedHeat` 에 들어와도 전투가 방어한다. 이 계산은 프로필을 **수정하지 않는다**(읽기 전용) - 정규화 저장은 월드맵 확정과 복원 직후에만 한다.

### 4.4 EndBattle 순서 (`BattleController` ~1340~1380)

```
승리 판정 후, RecordStageRun / ClearedStage 갱신 "전에":
  unlockedBefore = MaxSelectableHeat(SelectedStage)         <- 결과창 해금 배너 판정용
기존 처리 (IsNewStageBest 는 Heat 0 판만 / RecordStageRun 의 Runs·Wins 는 모든 판 / ClearedStage 갱신)
승리 시: EffectiveHeat >= 1 이면 HeatClears 갱신 (§1.5)
unlockedAfter = MaxSelectableHeat(SelectedStage)
newlyUnlockedHeat = unlockedAfter > unlockedBefore ? unlockedAfter : 0
```

- `newlyUnlockedHeat` 의미: 이번 승리로 새로 열린 선택 가능 최대 Heat 번호. 0 이면 배너 없음 (열리는 단계는 항상 1 이상이라 0 이 "없음" 으로 모호하지 않다 - Heat 0 은 "해금되는" 단계가 아님).
- 예: 미클리어 1-1 을 Heat 0 으로 클리어 -> before 0 / after 1 -> "Heat 1 해금!". Heat 3 최초 클리어(기존 최고 2) -> before 3 / after 4 -> "Heat 4 해금!". Heat 5 클리어 -> before 5 / after 5 -> 배너 없음. 이미 깬 Heat 재클리어 -> 배너 없음.
- Heat >= 1 판은 `SubmitRanking` 호출 안 함, `profile.BestClearTime` 갱신 안 함 (§1.6).

### 4.5 전투 HUD

- 영웅 이름 표기(`BattleViewModel.ComposeHeroTitle(name, stageIndex)` 호출부)에 `EffectiveHeat >= 1` 이면 " · Heat N" 접미를 붙인다 (신규 문구 1327 재사용). Heat 0 은 기존 표시 그대로. 신규 위젯 없음.

---

## 5. UI

> 목업: `.mockups/heat-select.html` - 기존 `worldmap-redesign-v2` 의 프레임·색·카드·노드 스타일 재사용. 신규 강조색은 Heat 전용 `#ff8a3d` 1색.

### 5.1 월드맵 팝업 - Heat 스테퍼 (신규)

**배치 대안**:

| 안 | 내용 | 평가 |
|---|---|---|
| A. 상세 카드 안 (기록 박스 아래) | 같은 열에서 스테이지·기록·Heat 가 묶여 보임 | 규칙/보스 페이즈 영역 208px 이 160px 로 줄고, 2-5 / 3-5 같은 규칙+페이즈 2건 사례가 오버플로 위험. `worldmap-popup-redesign.md` §5 가 이미 Auto Size 로 빠듯하게 맞춘 영역 |
| **B. 하단 바, 선택 버튼 왼쪽 (권장)** | 선택 버튼과 같은 줄이라 "Heat 정하고 선택" 흐름이 한 시선 | 안내 문구(`_bottomText`) 폭을 줄여야 함. 기존 영역을 건드리지 않음 |

**결정: B.** 좌표(팝업 루트 1280x720 기준, 좌상단 원점):

| 영역 | 좌표 (x, y) | 크기 | 비고 |
|---|---|---|---|
| 안내 문구 `_bottomText` (변경) | (96, 604) | 480x56 (기존 700x56 에서 축소) | 글자 13 -> 12, 2줄 허용(줄 높이 18). 문구 1316 은 한 줄에 480px 이 안 들어가 2줄이 된다 |
| Heat 스테퍼 박스 (신규) | (592, 604) | 232x56 | 바탕 `#0d1119`, 테두리 3px `#07090e` |
| 선택 버튼 `_selectButton` | (840, 604) | 352x56 | 변경 없음 |

검산: 96+480 = 576, 간격 16, 592+232 = 824, 간격 16, 840 시작, 840+352 = 1192 <= 패널 우측 1208.

스테퍼 박스 내부(박스 좌상단 기준):

| 위젯 | 위치 | 크기 | 스타일 |
|---|---|---|---|
| `−` 버튼 | (8, 4) | 40x32 | 기존 `.btn` 톤(`#2a3350` 계열), 글자 20 |
| 값 텍스트 | (52, 4) | 128x32 | "Heat N", 글자 18, 색 `#ff8a3d`, 가운데 정렬. 비활성 상태는 `#6c7385` |
| `+` 버튼 | (184, 4) | 40x32 | 같은 스타일 |
| 효과 문구 | (4, 36) | 224x14 | "영웅 HP ×1.32 · 공격력 ×1.26", 글자 11, 색 `#8e98ad`, 가운데 정렬 |

폭 검산: 8+40+4+128+4+40+8 = 232.

**동작 규칙**:

| 상황 | 표시 | 클릭 |
|---|---|---|
| 선택 스테이지 클리어함, `h > 0` | `−` 활성 | `h - 1` |
| `h == 0` | `−` 비활성 스타일(회색) | 무반응 |
| `h < MaxSelectableHeat` | `+` 활성 | `h + 1` |
| `h == MaxSelectableHeat` 이고 `h < 최대 Heat` (다음 단계 잠김) | `+` 비활성 스타일, 클릭은 가능 | 토스트 "Heat {h} 클리어 시 해금" (문구 1329, 인자 = 현재 h) |
| 선택 스테이지 미클리어 (`MaxSelectableHeat == 0`) | 박스 전체 비활성 스타일, 값 "Heat 0" | `+` 클릭 시 토스트 "먼저 이 스테이지를 클리어하세요" (문구 1330) |
| `h == 최대 Heat` | `+` 비활성 스타일 | 무반응 (토스트 없음) |
| 잠긴 스테이지 선택(기존 불가 상태) | 스테퍼 비활성, 값 "Heat 0" | 무반응 |

- 효과 문구의 배율은 `Heat.json` 의 해당 단계 값(`{0:0.##}` 형식). 스테이지 배수와 곱한 최종값은 표시하지 않는다(스테이지 배수가 이미 숨겨진 값이라 일관).
- **스테이지/챕터 전환 시 표시 Heat**: ViewModel 이 플레이어의 마지막 명시 선택값 `_wantedHeat` 를 들고, 표시 Heat = `Min(_wantedHeat, MaxSelectableHeat(선택 스테이지))`. 노드를 옮겨 다녀도 선택값이 사라지지 않고(예: 3-1 에서 Heat 3 선택 -> 3-2(미클리어, 상한 0)로 이동하면 표시 0 -> 다시 3-1 로 돌아오면 3 복원), 표시만 클램프된다. `+`/`−` 를 누르면 `_wantedHeat` 를 표시값 ±1 로 갱신한다. 초기 `_wantedHeat = profile.SelectedHeat`.
- **확정 시 저장**: "이 스테이지로 선택" 확정 -> `SelectedStage` 와 **함께** `SelectedHeat = 표시 Heat` 저장(기존 `onConfirm` 경로 확장; 기존 생성자 시그니처는 유지해 `WorldMapViewModelTests` 무수정 통과). 팝업을 확정 없이 닫으면 Heat 변경은 저장하지 않는다.

### 5.2 월드맵 상세 카드 - 최고 Heat 표시 (신규, 선택 스테이지 한정)

- 기록 박스(`RecordPanel`, 기존 328x48) 우측 상단에 "최고 Heat {N}" 작은 텍스트(글자 11, 색 `#ff8a3d`, 우측 12px, 상단 4px)를 추가한다. 표시 조건: 해당 스테이지 `HeatClears` 행 존재(`MaxHeat >= 1`). 행 없으면 숨김.
- 노드에는 표시하지 않는다(노드는 이미 이름·클리어 태그로 빡빡함).

### 5.3 결과창 (`ResultPopup`)

- `ResultPopupArg` 에 `Heat`(이번 판 `EffectiveHeat`) 와 `HeatUnlocked`(§4.4 `newlyUnlockedHeat`, 0 이면 없음) 추가.
- 결과 항목 줄 신규 "난이도 - Heat N": `Heat >= 1` 일 때만 표시(Heat 0 판은 기존 화면 그대로). 위치는 "클리어 시간" 줄 아래(승리) / "영웅 남은 HP" 줄 아래(패배), 값 색 `#ff8a3d`. `ResultRowsData` 에 `ShowHeatRow`, `HeatText` 추가(View 는 확정값만 표시, Rule 02 §6).
- 해금 배너(승리 한정, `HeatUnlocked >= 1` 일 때): 항목 줄 아래 폭 472x44 띠, 바탕 `#4a2810`→`#1c1008`, 테두리 2px `#ff8a3d`, 글자 18 `#ff8a3d` "Heat {N} 해금!" (문구 1332).
- "다시 도전" 은 같은 Heat 로 재시작(§4.2). 패배 화면은 배너 없음.

### 5.4 기록 팝업 (`RecordsPopup`) - 스테이지 행

- 스테이지 행 우측에 "Heat N" (글자 14, 색 `#ff8a3d`, 폭 90, 우측 정렬)를 추가. `HeatClears` 행이 있는 스테이지에만 표시, 없으면 빈 칸. 풀링 스크롤 셀의 기존 열 배치에서 전적 텍스트 폭을 90 줄여 확보한다 - 현행 셀 폭은 `RecordsPopup` 코드 확인 후 gameplay-programmer 가 맞추되, 기존 열을 가리지 않는 것이 조건.
- 이 항목은 **2순위(선택)** 이다. 일정 압박 시 §5.1 / §5.3 만으로도 1차 가설 검증은 성립한다.

### 5.5 마을 현상수배서

- 마을의 침입자 표기 `VillageViewModel.IntruderText` ("기사 · 3단계")에 `SelectedHeat >= 1` 이면 " · Heat N" 을 덧붙인다 (문구 1327 재사용). 출격 전 선택 확인용이다.
- 마을 진입 시 `SelectedStage` 가 "최대 플레이 가능 스테이지" 로 재설정되는 기존 규칙(`VillageController` 진입 초기화)은 유지한다. 따라서 마을 재진입마다 `SelectedHeat` 도 `ClampHeat` 로 재보정되며, 최고 도전 지점이 아직 미클리어면 0 이 된다. 같은 Heat 로 반복 도전하려면 Retry 를 쓰거나 월드맵에서 다시 고른다(의도된 기존 규칙의 귀결, §8.3 리스크 기록).

### 5.6 Strings_Ko 신규 id (현행 최대 id 1326 다음, 1327~1333)

| id | 문구 | 용도 |
|---|---|---|
| 1327 | `Heat {0}` | 스테퍼 값 · 최고 Heat 접두 · 전투 HUD 접미 · 마을 표기 · 기록 행 · 결과창 값 |
| 1328 | `영웅 HP ×{0} · 공격력 ×{1}` | 스테퍼 효과 문구 |
| 1329 | `Heat {0} 클리어 시 해금` | 다음 단계 잠금 토스트 (인자 = 현재 Heat) |
| 1330 | `먼저 이 스테이지를 클리어하세요` | 미클리어 스테이지에서 `+` 토스트 |
| 1331 | `최고 Heat {0}` | 상세 카드 기록 박스 |
| 1332 | `Heat {0} 해금!` | 결과창 해금 배너 |
| 1333 | `난이도` | 결과창 항목 줄 라벨 |

- 기존 `Strings_Ko` 무결성 테스트(id 연속·중복·포맷 인자)가 새 id 를 포함해 통과해야 한다. 프리팹의 정적 라벨(결과창 "난이도")도 CHText `_stringID` 로 연결한다 (Rule 03 §3).

---

## 6. 구현 요청사항 (gameplay-programmer)

- **Enum**: `EData` 에 `Heat` 추가 (`CommonEnum.cs`). `Heat.json` 파일명 = `Heat`. 신규 `EUI` / 에셋 키 없음.
- **Interface**: 신규 없음. (순수 정적 헬퍼와 데이터 클래스만)
- **에셋 키**: `Assets/_Lair/Data/Json/Heat.json` (Addressables 주소 = `Heat`, `Stage.json` 과 같은 등록 방식).
- **JSON 스키마**: `HeatRow { int heat; float hpMultiplier; float powerMultiplier; }` + §2.2 로드 검증. 로드 실패 시 크래시하지 않고 오류 로그 + "Heat 0 만 존재" 상태로 동작해 스테퍼를 비활성 처리한다 (Heat 기능만 비활성, 전투는 정상).
- **세이브 스키마**: `MetaProfile.SelectedHeat`(int=0), `MetaProfile.HeatClears`(List<HeatClearEntry>), `HeatClearEntry{Stage, MaxHeat}`, `Version = 7`, `CopyFrom` 갱신, `MetaProfileMigration.CurrentVersion = 7` + §3.1 단계 3~5.
- **코드 접점**
  - `Scripts/Meta/MetaProfile.cs`: 필드·엔트리·조회/갱신 헬퍼(§2.3, 갱신은 §1.5), `CopyFrom`.
  - `Scripts/Meta/MetaProfileMigration.cs`: §3.1.
  - `Scripts/Battle/StageProgress.cs`: `ScaleHeroStat` 오버로드, `MaxSelectableHeat` / `ClampHeat` 순수 정적 헬퍼.
  - `Scripts/Battle/HeroStatCalculator.cs`: Heat 행을 받는 `Compute` 오버로드(기존 시그니처 유지).
  - `Scripts/Battle/BattleController.cs`: `SpawnHero`(~691~794) Heat 읽기, `EndBattle`(~1340~1380) §4.4 순서·Heat>=1 판의 `IsNewStageBest`/`BestClearTime`/`SubmitRanking` 제외, `ResultPopupArg` 채우기(~1408), 전투 HUD 제목(~163, 라인 번호는 변동 가능).
  - `Scripts/Village/WorldMapViewModel.cs`: `_wantedHeat`, 표시 Heat, `+`/`−` 명령, `MaxSelectableHeat` 노출, 확정 시 Heat 전달 (기존 공개 멤버·생성자 유지).
  - `Scripts/UI/Village/WorldMapPopup.cs`: 스테퍼 위젯 와이어링(`[SerializeField] private`, Rule 02 §6.1), 토스트(`HandleNotice` 경로 재사용).
  - `Scripts/UI/ResultPopup.cs`(+ `ResultRowsData` 계산): 난이도 줄·해금 배너.
  - `Scripts/UI/RecordsPopup*.cs`: 기록 행 Heat (2순위).
  - `Scripts/Village/VillageController.cs` / `VillageViewModel.cs`: 진입 시·`OnStageChanged` 시 `SelectedHeat` `ClampHeat` 보정, 확정 시 Heat 저장, `IntruderText` 접미.
  - 확인 항목: 영웅 스킬 데미지가 영웅 `Power` 를 참조하는지(§1.6). 참조하지 않으면 Heat 공격력 배율은 기본 공격에만 적용된다는 사실을 보고한다(수치 변경 금지).
- **프리팹**(UI 배선은 목업 승인 후):
  - `Assets/_Lair/Art/UI/WorldMapPopup.prefab`: 하단 바에 스테퍼 박스(−/값/+/효과 문구) 신규, `_bottomText` 폭 700 -> 480 + 글자 12 + 2줄, 상세 카드 기록 박스에 "최고 Heat" CHText 신규.
  - `Assets/_Lair/Art/UI/ResultPopup.prefab`: 난이도 줄(라벨+값), 해금 배너 신규.
  - `RecordsPopup` 셀 프리팹: Heat 텍스트 열 (2순위).
  - 모든 TMP 에 CHText 부착, 버튼은 CHButton, 코드 동적 생성 금지, 신규 `[SerializeField]` 는 하나도 비우지 않는다 (`WorldMapStringsAndPrefabTests` 가 검사).
- **문서 반영**(메인 `[docs]` 소작업): `CLAUDE.md` §8, `docs/design/chapter-system.md` 129행, `chapter-5.md` 14행(문구 중 "악몽(Heat)" 금지 목록에서 Heat 제외), 컨셉서 §11 범위 표에 Heat 1차 한 줄. 문구 초안은 §9.

---

## 7. 테스트 항목 (test-engineer)

**EditMode**

| 영역 | 항목 |
|---|---|
| Heat 데이터 | 출하 `Heat.json` 6행 로드 / 0~5 연속 / Heat 0 이 1.0·1.0 / 단조 비감소. 검증 위반 케이스(빈 행·중복·역전·0 이하)가 각각 로드 오류 |
| 능력치 | `Compute`(Heat 0) 가 기존 결과와 비트 단위 동일 (기존 `HeroStatTableRegressionTests` 전체 무수정 통과). Heat 1~5 × 직업 × 스테이지 대표 표본에서 `ScaleHeroStat(앵커, 직업 × 스테이지 × Heat)` 1회 반올림 기댓값 일치. 반올림 경계(.5) 케이스 - 앵커 5·배율 곱 결과 N.5 인 입력이 round-half-up. Heat 로 HP 최소 1 보장 |
| 해금 | `MaxSelectableHeat`: 미클리어 -> 0 / 클리어(행 없음) -> 1 / 행 MaxHeat 2 -> 3 / MaxHeat 5 -> 5(상한) / 최대 Heat 데이터 3 으로 줄었을 때 상한 3. `ClampHeat`: 음수 -> 0, 상한 초과 -> 상한 |
| 기록 갱신 | 승리 Heat 3 (기존 1) -> 3 / 승리 Heat 1 (기존 3) -> 3 유지 / 승리 Heat 0 -> 행 불변 / 패배 -> 불변 / 행 없을 때 새 행 생성 / `MaxHeat >= 1` 불변식. 스테이지별 독립 (1-1 기록이 1-2 에 영향 없음) |
| 해금 배너 값 | `newlyUnlockedHeat`: 신규 클리어 Heat 0 -> 1 / Heat 3 최초 클리어(기존 2) -> 4 / Heat 5 클리어 -> 0 / 이미 깬 Heat 재클리어 -> 0 |
| 마이그레이션 | v6 JSON 로드 -> Version 7, `SelectedHeat` 0, `HeatClears` 빈 리스트. `CurrentVersion == 7`. v7 재적용 멱등. 정리 규칙: `Stage` 범위 밖·`MaxHeat < 1`·`Stage > ClearedStage` 행 제거, 중복 `Stage` 최댓값 병합, `SelectedHeat` 음수 -> 0. Heat 최대 단계 데이터 미로드 상태 호출에서 상한으로 자르지 않음 |
| 클라우드 | `CopyFrom` 이 `SelectedHeat`·`HeatClears` 를 복사(기존 필드와 같은 규약 - other 의 리스트를 대입, null 이면 빈 리스트). v6 형태 프로필 복사 시 Heat 빈 상태. 복원 후 Migration 재적용으로 해금 범위 밖 행 정리 |
| 월드맵 VM | 노드 이동 시 표시 Heat 클램프 / 되돌아오면 `_wantedHeat` 복원 / `+` 상한·잠김 토스트 / 미클리어 스테이지 `+` 토스트 / 확정 시 `SelectedHeat` 전달 / 닫기(미확정) 시 미저장. 기존 `WorldMapViewModelTests`·`WorldMapViewModelBehaviorTests` 무수정 통과 |
| 전투 읽기 | `EffectiveHeat`: `SelectedHeat` 가 해금 초과일 때 클램프(프로필 비수정) / Retry 시나리오(같은 프로필 두 번 읽기)에서 동일 Heat |
| 제외 규칙 | Heat >= 1 승리가 `BestClearTime`·`StageRecordEntry.BestClearTime`·`IsNewStageBest` 를 건드리지 않음, `Runs`/`Wins` 는 증가 |
| 문자열 | `Strings_Ko` 1327~1333 존재 / 포맷 인자 개수 일치 / 기존 무결성 테스트 통과 |
| 프리팹 | `WorldMapStringsAndPrefabTests` 무수정 통과(신규 `[SerializeField]` 미연결 0). `ResultPopup` 프리팹 신규 참조 연결 |

**PlayMode (수동 확인 시나리오)**: 월드맵에서 Heat 선택 -> 출격 -> 영웅 HP 가 기대값 -> Retry 시 Heat 유지 -> 승리 후 결과창 해금 배너 -> 월드맵 재오픈 시 다음 Heat 열림 -> 앱 재시작 후 저장 유지 -> 클라우드 복원 후 유지.

---

## 8. 밸런스 위험 · QA 후속

### 8.1 위험 요약

| # | 위험 | 영향 | 완화 |
|---|---|---|---|
| R1 | 보스 스테이지 Heat 3~5 가 1차 단독으로 시간 초과(300초) | "열렸는데 못 깨는 단계" 좌절 | §8.2 게이트 H3, 조정은 `Heat.json` 만. 최악의 경우 최대 Heat 를 행 삭제로 낮춤(UI 자동 반영) |
| R2 | Power 배율의 비선형 효과 (보드 압력 감소 -> 시간 급증) | 단계 간 난이도 곡선 불연속 | HP-only / Power-only 분리 측정(§8.2 H2), Power 증가폭이 HP 보다 낮은 상태로 시작 |
| R3 | 보상 없는 Heat (소울·XP 가산 0) | 해금 외 동기 부재 -> 사용률 저조 | 사용자 확정 요구가 "능력치 배율만". 1차 출시 후 Heat >= 1 선택률·재도전 횟수를 측정하고, 2차 유물 드랍(확률/등급)이 보상이 된다. 소울 가산이 필요하면 별도 승인 요청 |
| R4 | 마을 재진입 시 선택 Heat 가 최고 도전 지점의 0 으로 초기화 | 같은 Heat 반복 도전 시 월드맵 재조작 필요 | 의도된 기존 규칙의 귀결(§5.5). Retry 로 반복 가능. 불만이 크면 후속에서 "진입 시 마지막 Heat 유지" 로 바꾸는 별도 기획 |
| R5 | Heat >= 1 판이 랭킹·최단 기록에서 제외 | 높은 Heat 로 얻는 경쟁 요소 없음 | 의도(§1.6). Heat 랭킹은 Firestore 규칙 변경이 필요한 별도 범위 |
| R6 | 영웅 HP 가 커지면 `damagePopup`·HP 바 표시 자릿수 증가 (5-5 Heat 5 에서 앵커×3.6×직업 배율) | 표시 잘림 | 구현 시 HP 바/팝업 텍스트 폭 확인(수동), 문제 시 보고 |
| R7 | 패시브 임계(HP 10% 구간)는 비율이라 불변이지만 Heat 로 총 HP 가 늘면 임계 사이 시간이 늘어 카드 선택 사이 공백이 길어짐 | 페이싱 | H2 에서 "카드 픽 간 평균 간격" 을 Heat 별로 기록 |

### 8.2 qa-simulator 후속 제안 (구현 + 테스트 완료 후)

- **표본**: 스테이지 6개(1-1, 1-5, 2-3, 3-5, 4-5, 5-5) × Heat 0~5 × 봇 프로필 5종(G0 와 동일) × 100런 = 18,000런. 메타 레벨은 각 스테이지 도전 시점의 `M_ref` 대응 상점 진행도(`chapter-system.md` §0.4 표). 시드 고정(런 i 의 시드 = i).
- **게이트**:

| # | 지표 | 통과 기준 | 실패 시 조치 |
|---|---|---|---|
| H1 | Heat 0 회귀 | 모든 표본에서 Heat 도입 전 기록과 영웅 능력치 완전 일치, 승률·처치 시간 동일 시드 비교에서 차이 0 | 코드 버그 - 개발 재작업 |
| H2 | 단계 간 처치 시간 증가율 (스테이지별 평균 처치 시각의 Heat N+1 / Heat N) | 비보스 스테이지에서 +8% ~ +18% 구간, 단조 증가 | 구간 밖이면 해당 단계 `hpMultiplier`/`powerMultiplier` 재산출 (단계 증가율이 목표의 중앙 +13% 에 오도록) |
| H3 | 승률 하한 (메타 `M_ref` 기준) | 비보스: Heat 1 >= 70%, Heat 2 >= 55%, Heat 3 >= 40%, Heat 4 >= 25%, Heat 5 >= 10% / 보스: Heat 1 >= 60%, Heat 2 >= 40%, Heat 3 >= 20% (Heat 4~5 는 관찰 지표) | 하한 미달 단계의 배율을 낮추거나, 보스 Heat 4~5 클리어율 0% 가 확정이면 사용자에게 최대 Heat 축소·`Stage_Heat` 관계 데이터 도입을 에스컬레이션 |
| H4 | 프로필별 편차 | 각 봇 프로필 평균 처치 시각이 같은 (스테이지, Heat) 평균의 ±25초 이내 (G0-3 와 동일 기준) | Heat 가 특정 빌드 축을 불리하게 하는지 분석 |
| H5 | 처치 시각 하한 | 모든 (스테이지, Heat) p10 >= 75초, 최소 >= 30초 | 불가(Heat 는 영웅을 단단하게만 하므로 하한 위반은 비정상 - 버그 점검) |
| H6 | 카드 픽 간격 | Heat 별 평균 패시브 픽 수가 Heat 0 대비 하락하지 않고 액티브 픽 수가 증가 | 페이싱 이상이면 보고 |

- 결정 메트릭이 H2·H3 이므로 **`Heat.json` 배율의 최종값은 이 리포트 이후 확정**한다. 본 문서의 §1.2 표는 초기값이다.
- 분리 측정: Heat 배율을 (HP만 적용 / Power만 적용 / 둘 다) 3조건으로 6개 스테이지 × Heat 3 에서 각 200런 - R2 판정용.

### 8.3 엣지 케이스 정리

| # | 케이스 | 처리 |
|---|---|---|
| E-1 | 스테이지 변경 시 `SelectedHeat` | 월드맵: 표시값만 클램프, `_wantedHeat` 유지(§5.1). 마을 캐러셀/진입: `SelectedStage` 를 쓰는 모든 경로가 `SelectedHeat` 를 `ClampHeat` 로 같이 보정 |
| E-2 | 해금 안 된 Heat 가 저장돼 있음 (클라우드 복원·수동 편집) | 전투는 `EffectiveHeat` 로 방어(§4.3), 월드맵 열 때 표시 클램프, 확정 시 정규화 저장 |
| E-3 | `Heat.json` 최대 단계가 줄어듦 (조정으로 행 삭제) | 저장된 `MaxHeat` 가 상한을 넘어도 읽는 지점에서 `ClampHeat` 가 상한으로 자름. 저장값은 그대로 두어 되돌리면 복구 |
| E-4 | `Heat.json` 로드 실패 | 오류 로그, Heat 0 만 존재로 동작, 스테퍼 비활성. 전투·월드맵 나머지 기능 정상 |
| E-5 | 구버전(v6) 프로필 | 필드 부재 -> 기본값, 마이그레이션 v7 승격(§3.1) |
| E-6 | `HeatClears` 에 `Stage > ClearedStage` 행 (복원 데이터 불일치) | 마이그레이션 정리에서 제거(§3.1-4) |
| E-7 | 같은 `Stage` 중복 행 | 최댓값 하나로 병합 |
| E-8 | 승리 중 `EffectiveHeat` 가 판 도중 바뀜 | 불가 - 전투 시작에 확정, 판 도중 프로필 Heat 를 다시 읽지 않음. 결과 처리는 확정값 사용 |
| E-9 | Retry 중 해금 Heat 가 늘어남 | 선택 Heat 는 그대로(자동 상승 없음) |
| E-10 | 마지막 챕터/스테이지·`MaxStage` 변동 | `Stage` 범위 정리는 기존 `MaxStage` 기반이라 챕터 추가 시에도 동일 |
| E-11 | 전투 중 뒤로가기 종료 | 기록 갱신 없음(`EndBattle` 승리 분기를 타지 않음) |

---

## 9. CLAUDE.md §8 승격 문구 초안 (사용자 승인 후 메인이 반영)

§8 의 챕터 구조 허용 항목 끝 문장

> **★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.**

을 다음으로 교체한다.

> **★ 목표, 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.**

그리고 "서버 연동 허용" 항목 앞에 다음 항목을 추가한다.

> - **Heat(직접 고르는 난이도) 1차 허용 (2026-10-19 v0.3 승격) - 단 영웅 능력치 배율에 한정** - 스테이지 입장 전 월드맵 팝업(마을 HUD 안)에서 Heat 단계(0~5)를 직접 고르며, Heat 가 오르면 **영웅 체력·공격력 배율만** 상승한다(몬스터 제한·신규 스테이지 규칙 없음). 해금은 스테이지별로 "Heat N 클리어 -> Heat N+1 선택 가능". 허용 범위는 ① `Heat.json` 배율표 ② `MetaProfile` 의 스테이지별 최고 클리어 Heat 와 선택 Heat 저장(Version 7)·클라우드 동기 호환 ③ 월드맵 팝업·결과창·기록·마을 표기의 Heat 표시. Heat >= 1 판은 최단 기록·랭킹에서 제외한다. **유물은 Heat 1차 완료 후 2차로 개별 승격 예정이며 그때까지 금지**(Heat 가 유물 드랍 확률/등급에 쓰이는 것은 2차 범위). ★ 목표·던전 편성·무한 탑·데일리 금지는 유지. 별도 메인 메뉴/세팅 화면 금지는 유지(Heat 선택은 월드맵 팝업 안).

같은 문장이 있는 `docs/design/chapter-system.md` 129행, `chapter-5.md` 14행(과거 승격 기록)은 **당시 기록이라 수정하지 않고**, 본 기획서와 CLAUDE.md 를 현행 기준으로 둔다. 컨셉서 §11 범위 표에는 "Heat 1차(영웅 능력치 배율 난이도) - v0.3 승격 2026-10-19" 한 줄을 추가한다.

---

## 10. 결정 요약 (사용자 확인 필요 항목)

| ID | 결정 | 대안 | 확인 필요 이유 |
|---|---|---|---|
| D-1 | 6단계(0~5), HP 1.00~1.60 / Power 1.00~1.50 (§1.2) | 4단계, 11단계 | 수치는 초기값, QA 후 확정 |
| D-2 | Heat >= 1 판은 최단 기록·랭킹 제외 (§1.6) | Heat 별 랭킹 | 랭킹 의미 보존. 반대로 가려면 Firestore 규칙 작업 필요 |
| D-3 | 스테퍼를 하단 바에 배치 (§5.1-B) | 상세 카드 안 | 목업 승인 항목 |
| D-4 | 소울·XP 보상 가산 없음 (§1.6, R3) | Heat 당 소울 +N% | 사용자 확정 요구와 일치, 동기 리스크는 수용 |
| D-5 | 마을 재진입 시 Heat 재보정 (§5.5, R4) | 마지막 Heat 유지 | 기존 `SelectedStage` 리셋 규칙과 일관 |

Self-Review 는 보고에 기록한다.
