# 마을 메타 가시화 — 던전 강화 요약 + 도전과제 진행도 spec

> 작성일: 2026-06-12
> 단계: v0.2 (마을 허브 + 메타 진행 후속)
> 검증 가설 보강: **"런 사이 메타 성장이 재방문 동기를 만드는가"** — 성장의 *체감*과 *장기 목표 가시성*을 마을 안에서 직접 노출해 가설 검증력을 높인다.
> 산출 흐름: 본 spec → plan (`docs/superpowers/plans/`) → 기획서 (`docs/design/`) → 구현
> 입력 맥락: `docs/design/village-meta-hub.md` (마을+메타 본 기획서), `Assets/_Lair/Scripts/Meta/MetaBattleBonus.cs`, `Assets/_Lair/Scripts/UI/Village/{ShopPopup,QuestPopup,QuestCell}.cs`

---

## 1. 의도

v0.2 마을 허브는 소울/영주레벨/상점/도전과제를 갖췄지만, **메타 성장이 마을 안에서 보이지 않는다**:

- **상점**: 항목별 효과(`Description`, "모든 몬스터 HP +2%/Lv")는 보이나, **지금까지 산 것의 누적 합산**이 어디에도 없다. 성장 체감은 전투에 들어가야만 생긴다 — 가설의 절반("구매가 다음 런에서 체감되는가")이 마을에서 닫히지 않는다.
- **도전과제**: `QuestCell` 이 **달성/미달성 바이너리**뿐. `Wins50`·`Runs30` 같은 누적 장기 목표가 "지금 몇 / 목표 몇"으로 안 보여 "다음 런을 누를 이유"로 작동하지 않는다.

두 기능 모두 **신규 리소스·신규 저장 필드 없이** 기존 집계/누적 데이터를 화면에 노출하는 범위다.

## 2. 범위 (확정 — 사용자 선택)

| 항목 | 결정 |
|---|---|
| ① 던전 강화 가시화 — 노출 위치 | **상점 팝업 상단 요약줄** (마을 상단바 상시 노출은 비범위) |
| ① 표기 형식 | **스탯별 % 나열** (단일 종합 수치는 비범위) |
| ② 도전과제 진행도 — 범위 | **누적형(TotalWins·TotalRuns)만 진행 바 + "N/M"** — 시간형·시너지·첫승은 현행 바이너리 유지 |

## 3. 시스템별 윤곽

> 구체 라벨·환산 반올림·표시 문구는 game-designer 기획서가 단일 진실. 본 절은 메커니즘 골격만 락.

### 3.1 ① 던전 강화 요약 (상점 팝업)

- **신규 순수 C# 단위 `DungeonPowerSummary`** (`Scripts/Meta/`, Unity UI 비종속 — EditMode 테스트 대상): `(MetaProfile, MetaConfig) → 표시 라인 목록`.
  - `MetaBattleBonus` 의 집계 배율을 **재사용**(중복 환산 로직 금지 — Rule 02 §5)하되, `cfg.ShopItems` **순서로 순회**해 레벨 > 0 항목만 라벨 + 퍼센트로 환산.
  - 환산 방향: 증가형(HP·공격·이동·사거리) = `(mul − 1)`, 감소형(공속·둔화·스폰률) = `(1 / mul − 1)` → 반올림 %. 각 항목의 본 기획서 §3.2 표기 방향(공속↑/둔화 강화/스폰률↑)과 부호 일치.
- **`ShopPopup`**: `_bonusSummaryText`(CHText) 추가, `Rebuild()` 에서 세팅 → **구매 직후 자동 갱신**. 강화 0건이면 "아직 없음" 류 베이스라인 문구.
- 라벨·요약 문구는 **동적 조립 문구 → 코드 리터럴**(본 기획서 §7 rev4 의 ②표 규칙). `Strings_Ko.json` id 추가 없음.

### 3.2 ② 도전과제 진행도 (퀘스트 팝업)

- **`QuestCellData` 확장**: 진행도 필드(현재값·목표값·비율·표시문구) 추가.
- **`QuestPopup.BuildCellData`** (기존 static, 테스트 대상) 에서 `Condition == TotalWins → profile.TotalWins`, `TotalRuns → profile.TotalRuns` 인 **미달성** 셀만 진행도 on (현재값은 목표로 클램프). 그 외 조건은 진행도 off → 현행 동작 불변.
- **`QuestCell`**: 진행 바(Image fill) + 진행 텍스트("N/M") 추가. `진행도 on && 미달성` 일 때만 표시, 달성 시 숨김(기존 달성 뱃지로 대체).

### 3.3 프리팹 / 빌더 (영속화 — 손-편집 클로버 방지)

- `ShopPopup.prefab` 에 `_bonusSummaryText` 노드, `QuestCell.prefab` 에 진행 바 노드 추가.
- **둘 다 `LairVillageBuilder`(V2 메뉴) 빌더 코드에 함께 반영** — V2 재실행 시 프리팹이 재생성(빌더가 SoT)되므로 빌더 미반영 시 손-편집이 덮어써진다.

## 4. 데이터 흐름

```
① profile + cfg → MetaBattleBonus(집계) → DungeonPowerSummary(라벨 + %) → ShopPopup._bonusSummaryText (Rebuild 시)
② profile(TotalWins / TotalRuns) → QuestPopup.BuildCellData(진행 필드) → QuestCell 진행 바
```

## 5. 비범위 (명시 제외)

- 마을 상단바 상시 노출(①) · 단일 종합 수치(①) · 시간형/시너지 진행도(②) — 사용자 선택에서 제외.
- 신규 `MetaProfile` 저장 필드 0건. JSON 문자열 id 추가 0건(동적 문구는 코드 리터럴).
- 알림 뱃지 · 첫 세션 온보딩 · 마을 상단바 개편 — 별도 후속(이전 브레인스토밍 ③④).
- 밸런스 수치 변경 없음 — 표시 전용. 전투 적용 경로(`MetaBattleBonus` 소비처)는 불변.

## 6. 테스트

- `DungeonPowerSummaryTests` — 환산 %(증가형/감소형), 항목 순서, 레벨 0 항목 제외, 강화 0건 케이스.
- `QuestPopup.BuildCellData` 회귀 — 누적형 진행 필드 산출 + 비누적형/달성 셀의 진행도 off 확인.

## 7. 리스크 / 주의

- **부호 방향 혼동**: 감소형 스탯(공속·둔화·스폰러 주기)의 mul 은 1 미만 — `(1/mul − 1)` 로 환산해야 "강화 +%"가 양수로 표기된다. 기획서가 항목별 방향·라벨을 표로 확정한다.
- **빌더 SoT**: §3.3 — 프리팹 직접 편집만 하고 빌더 미반영 시 다음 V2 실행에서 소실. 두 곳 동시 반영을 plan 체크리스트로 못박는다.
- **표시 전용 보증**: 본 기능은 `MetaBattleBonus` 의 *전투 적용* 로직을 건드리지 않는다 — 동일 배율을 읽기만 한다. 회귀 테스트로 전투 경로 불변 확인.
