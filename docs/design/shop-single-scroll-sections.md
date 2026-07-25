# 상점 단일 스크롤뷰 + 섹션 헤더 (탭 제거)

> 입력: spec `docs/superpowers/specs/2026-07-25-shop-single-scroll-sections-design.md` · plan `docs/superpowers/plans/2026-07-25-shop-single-scroll-sections.md`
> 이 기획서는 plan 의 `⟨기획서 확정⟩` 자리(헤더 스타일·높이·요약줄·간격·빈 섹션)를 닫는다. 파일 구조·시그니처·TDD 골격은 plan 이 SoT, 도메인 시각/UX 수치는 본 문서가 SoT. 상점 항목·가격·효과는 무변경(spec §2).

---

## § 헤더

- **목표**: 상점의 「스탯 강화」/「몬스터 강화」 **탭을 제거**하고, 단일 세로 스크롤에 **섹션 헤더 행 + 항목 행**을 순서대로 얹은 통합 목록으로 재구성한다. 추가 클릭 없이 전체 강화 항목을 스크롤로 연속 탐색.
- **검증 가설**: 탭 전환(추가 클릭) 제거 + 섹션 헤더 그룹핑이 (a) 강화 항목 전체 인지·탐색을 빠르게 하는가, (b) 스탯/몬스터 두 축의 관계를 한 화면 흐름에서 더 잘 읽히게 하는가.
- **현재 단계 범위 적합성**: **범위 내**. spec §1·§2 가 "기존 상점 UI 재구성 — 신규 기능 아님, 항목·가격·효과 무변경"으로 확인. CLAUDE.md §8 메타(상점) 범위 내. 신규 리소스 0(헤더는 CHText + 기존 톤 색/내장 스프라이트로 배선).
- **핵심 메커니즘**: `CHPoolingScrollView` 는 셀 타입 1개만 바인딩하므로, 헤더 행과 항목 행을 **한 셀 타입**(`ShopItemCell`)이 `RowKind`(SectionHeader/Item)로 분기해 그린다. `BuildCellData(profile,cfg)` 가 [스탯 헤더 + 스탯 항목들 + 몬스터 헤더 + 종족 항목들] 통합 리스트를 반환. 탭 상태·필터·버튼 전부 제거.

---

## 0. 레이아웃 사실 확인 — 단일 컬럼 리스트(4열 아님)

> **중요(코디네이터 지시 정정)**: 작업 지시에 "4열"이 언급됐으나, 이는 직전 도감 작업(4열 그리드)에서 넘어온 오기다. 상점은 **단일 컬럼 세로 리스트**다 — 근거(1차 확인):
> - `ShopItemPoolingScrollView` 프리팹의 `_columnCount: 1`(단일 컬럼).
> - `ShopItemCell` 루트 = **660×84**(넓은 가로 행: 좌측 아이콘 + 중앙 이름/레벨/설명 + 우측 가격/구매 버튼).
> - spec 락 결정(단일 스크롤 + 기존 셀 재사용, 항목·레이아웃 무변경)과도 정합. 4열 그리드로 바꾸는 것은 spec 위반이므로 채택하지 않는다.
>
> 따라서 본 기획서·목업은 **단일 컬럼 리스트** 기준으로 설계한다.

---

## 1. 섹션 헤더 행 스타일 (⟨기획서 확정⟩)

헤더 행은 항목 행(660×84 가로 카드)과 **같은 셀 타입·같은 폭(660)·같은 높이(84 — §2 강제)** 지만, **배너 트리트먼트**로 항목과 시각적으로 확실히 구분한다.

| 요소 | 값 | 근거 |
|---|---|---|
| **문구** | `"스탯 강화"` / `"몬스터 강화"` (spec 문구 그대로) | 기존 탭 라벨과 동일 → 사용자가 이미 학습한 라벨. 새 표현 발명 금지(일관). |
| **폰트 크기** | 17 (Jua) | 실측 위계 확인: 팝업 타이틀 "상점"=**18** > 섹션 헤더 **17** > 항목명 **16**. 헤더를 18(=타이틀)로 두면 화면 타이틀과 동급이 되는 인버전이라 17로 한 칸 낮춤. 단 17 vs 항목명 16 은 1px 차라 **크기는 보조 신호** — 헤더/항목 구분의 주 신호는 배너 트리트먼트(금 액센트 바 + 밴드 배경 + 하단 구분선)다. |
| **색** | `#F5F5F5`(흰색) | 금색(#FBBF24)은 상호작용/가격 강조 전용(소울·구매·탭활성에 이미 사용) — 헤더까지 금색이면 강조가 분산된다. 헤더는 **중립 밝은 흰색**으로 "라벨" 역할. |
| **정렬** | 좌측 정렬, 좌측 인셋 24px | 제목이 좌측 열로 스캔됨. 항목 행 좌측 콘텐츠 라인과 정렬해 깔끔한 좌측 축. |
| **금색 액센트 바** | 제목 왼쪽에 3×22 세로 바, `#FBBF24` | 금색을 **점(bar)** 으로만 써서 섹션 마커로 스캔성↑ + 앱의 금 액센트 톤 연결(제목 전체를 금색으로 칠하지 않아 강조 분산 회피). |
| **배너 배경** | `rgba(255,255,255,0.03)` 은은한 밴드(항목 카드 스타일 테두리 없음) | 항목 행은 카드(테두리/배경), 헤더는 **테두리 없는 얇은 밴드** → 카드가 아니라 구획선으로 읽힘. |
| **하단 구분선** | 1px `rgba(255,255,255,0.12)`, 행 폭 전체 | 헤더와 그 아래 항목들 사이 경계. "이 아래가 이 섹션" 신호. |

**대안 비교(헤더 시각 강도)**:

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **흰 제목 + 금 액센트 바 + 은은한 밴드 + 하단 구분선 (채택)** | 항목과 명확 구분, 금 강조 분산 안 함, 톤 일관 | 화려하진 않음 | ✅ |
| 제목 전체 금색 + 굵은 배경 | 매우 눈에 띔 | 금색이 가격/구매 강조와 경쟁 → 시선 혼란, 헤더가 카드처럼 무거움 | ✗ |
| 제목만(장식 0) | 최소 스코프 | 84px 빈 행에 텍스트만 → 버그/빈칸처럼 보임, 섹션 인지 약함 | ✗ |

---

## 2. 헤더 행 높이 — (A) 항목과 동일 높이 84px, **프레임워크 강제** (⟨기획서 확정⟩)

**결정: 헤더 행 = 항목 행과 동일한 84px 고정 높이(spec §5 안 (A)).** 이는 디자인 선호가 아니라 **프레임워크 하드 제약**이다 — 1차 코드 확인:

- `CHPoolingScrollView.SetItemSize()` 는 **`_origin`(단일 프로토타입 셀) rect 하나**에서 `_itemSize` 를 구한다(셀별 높이 개념 없음).
- 위치 계산 `GetItemVerticalPosition` 이 `rowIndex * _itemSize.y + rowIndex * _itemGap.y` 로 **모든 행에 동일 높이**를 곱해 배치한다.
- 즉 헤더만 짧게 두면 가상화 위치·풀 계산이 어긋난다. **(B) 가변 높이는 이 프레임워크가 지원하지 않는다.**

> **gameplay-programmer 확인 요청**: 위는 game-designer 의 1차 코드 판독이다. 구현 착수 시 `CHPoolingScrollView` 를 재확인해 (A) 강제를 확정하라(spec §5). 만약 프레임워크를 확장해 가변 높이를 지원하기로 하면 별도 스코프 — 본 기획은 (A) 전제.

### 2.1 강제된 84px 를 "섹션 간격"으로 활용 — 헤더 셀 내부 구성

헤더가 84px 로 강제되므로, 그 높이를 빈 공간으로 낭비하지 않고 **상단 여백을 섹션 구분 간격으로** 쓴다. 헤더 셀 84px 내부 분할:

```
헤더 셀 84 = 상단 투명 스페이서 24  +  배너 60
검산: 24 + 60 = 84 ✓
```

- **상단 24px = 투명 스페이서**: 헤더 위(직전 섹션 마지막 항목)와의 시각 간격을 만든다. 프레임워크가 특정 행 앞에만 여분 gap 을 못 넣으므로(행 간격 균일), 이 "강제된 여분 높이"가 곧 **섹션 앞 간격**이 된다 → 제약을 기능으로 전환.
- **하단 60px = 배너**: §1 배너(밴드 배경 + 금 액센트 바 + 제목 + 하단 구분선). 제목은 배너 60px 안에서 **수직 중앙**(셀 상단 기준 24 + 30 = 54px). 하단 구분선은 셀 하단(84px) 라인.
- 배너(60px)가 항목 카드(84px)보다 낮아 헤더가 항목과 다른 실루엣으로 읽힌다 — "카드가 아닌 구획 배너".

---

## 3. "현재 강화" 요약줄 — 유지 확정 (⟨기획서 확정⟩)

**상단 요약줄 `_bonusSummaryText` 유지**(spec 기본안). 섹션 헤더와 **역할이 다르므로 중복 아님**:

| | 요약줄 | 섹션 헤더 |
|---|---|---|
| 역할 | **상태 readout** — 지금 내가 **이미 산** 강화의 집계("현재 강화  HP +10% · 공속 +5%") | **내비게이션 라벨** — 목록의 **구매 가능** 항목 그룹 제목 |
| 답하는 질문 | "내가 지금 뭘 가졌나" | "목록 어디쯤 / 무슨 그룹인가" |
| 범위 | 글로벌 스탯 강화 집계(`DungeonPowerSummary`, 종족 강화는 미포함 — monster-species §6) | 스탯/몬스터 두 섹션 라벨 |

- 두 요소는 **상태(가진 것) vs 위치(살 것의 구획)** 로 축이 달라 겹치지 않는다. 요약줄은 스크롤과 무관하게 상단 고정 상태판, 헤더는 스크롤되는 목록 내 구획선.
- 요약줄은 종족 강화를 포함하지 않으므로(monster-species §6) 헤더가 새로 만든 "몬스터 강화" 구획과도 정보 중복이 없다.
- **무변경**: `Rebuild` 의 요약줄 세팅·`BuildSummaryText`·`DungeonPowerSummary.Build` 전부 그대로(스코프 최소화).

---

## 4. 섹션 간격 · 상단 여백 (⟨기획서 확정⟩)

`CHPoolingScrollView` 의 `_padding`(스크롤 상/하 여백)·`_itemGap.y`(행 간격) 인스펙터 값 확정:

| 항목 | 값 | 근거 / 검산 |
|---|---|---|
| **행 간격 `_itemGap.y`** | 8px | 항목 행 사이 숨 쉴 틈. 목업 9px 톤과 근사, 660폭에서 과하지 않음. |
| **스크롤 상단 `_padding.top`** | 8px | 첫 헤더가 뷰포트 상단에 딱 붙지 않게. 첫 헤더 배너는 셀 내부 24px 스페이서와 합쳐 **뷰포트 상단에서 8 + 24 = 32px** 아래 시작 → 첫 섹션도 충분한 top breathing. |
| **스크롤 하단 `_padding.bottom`** | 16px | 마지막 항목이 뷰포트 하단에 붙지 않게(스크롤 끝 여운). |
| **섹션 사이 시각 간격** | ≈ 8(행 gap) + 24(헤더 내부 상단 스페이서) = **32px** | 직전 섹션 마지막 항목 하단 ~ 다음 헤더 배너 상단 사이. 균일 gap 제약 하에서 헤더 내부 스페이서로 확보(§2.1). 검산: item gap 8 + 헤더 상단 스페이서 24 = 32px 시각 분리. |

- **왜 32px 인가**: 항목 간 8px 대비 4배 → 새 섹션 시작이 확실히 읽힌다. 헤더 배너 자체(밴드+구분선)와 합쳐 이중 신호(간격 + 스타일)로 섹션 인지.
- 이 값들은 프리팹 `ShopItemPoolingScrollView` 인스펙터 배선 값이다 — 현재 프리팹 값과 다르면 Task 4 에서 갱신.

---

## 5. 빈 섹션 처리 (⟨기획서 확정⟩)

**규칙: 섹션에 항목이 0개면 그 섹션 헤더를 넣지 않는다(헤더 숨김).**

- `BuildCellData` 는 각 섹션의 항목을 먼저 수집해 **1개 이상일 때만 헤더 행을 앞에 추가**한다(항목 0개면 헤더도 제외).
- **근거**: 헤더는 그 아래 항목들의 라벨이다. 항목이 없으면 라벨 대상이 없어 **외로운 배너**만 남아 "왜 빈 섹션이 있지?" 혼란을 준다. 헤더-항목은 항상 짝.
- **현 상태**: 두 섹션(스탯: MonsterStat/SpawnerPeriod, 몬스터: MonsterSpecies 6종) 모두 항목이 있으므로 이 가드는 현재 무해(방어적). 향후 config 에서 한 섹션이 비면 자동으로 그 헤더가 사라진다.
- **구현 노트**: 섹션별 임시 리스트에 항목을 채운 뒤, `count > 0` 이면 `[헤더] + 항목들` 을 통합 리스트에 append. plan Task 1 의 `AddItems` 를 "항목을 먼저 모으고 비어있지 않으면 헤더+항목 append" 순서로 조정(§7 구현 요청).

---

## 6. 스크롤 가독성 종합

- **섹션 인지 이중 신호**: (a) 32px 간격(§4) + (b) 배너 스타일(밴드+금 바+구분선, §1). 스크롤 중 헤더가 지나갈 때 항목 카드 흐름과 확실히 구분.
- **좌측 축 정렬**: 헤더 제목 좌측 인셋(24px)이 항목 콘텐츠 좌측과 정렬 → 눈이 좌측 열을 따라 섹션을 스캔.
- **위계**: 타이틀 18 > 헤더 17 흰색 > 항목명 16(실측). 크기 차는 작으나 배너 스타일(금 바+밴드+구분선) + 흰색이 "제목 vs 항목"을 즉시 구분(§1).
- **단일 스크롤 연속성**: 탭 클릭 없이 스탯→몬스터를 한 번의 스크롤로 훑음(의도, §1). 헤더가 위치 감각을 줘 "지금 몬스터 강화 구간"을 알 수 있음.

---

## 7. 구현 요청사항 (gameplay-programmer 용)

> 시그니처·파일 구조는 plan 이 SoT. 아래는 도메인 값·시각 스펙 확정.

### Enum (Rule 02 §8)
- **제거**: `ShopPopup.ShopTab { Stat, Species }`.
- **신설**: `ShopPopup.ShopRowKind { SectionHeader, Item }`(단일 시스템 내부 enum — ShopPopup 파일 내, plan Task 1). 기본값 `Item`.

### 데이터 필드
- `ShopItemCellData` 에 추가: `public ShopRowKind RowKind;`(기본 Item), `public string HeaderText;`(헤더 행일 때 섹션 제목). 기존 항목 필드는 Item 행에서만 유효.

### 시그니처 변경
- `BuildCellData(profile, cfg)` **2-arg 단일** — 통합 리스트 [스탯 헤더 + 스탯 항목들 + 몬스터 헤더 + 종족 항목들] 반환. **빈 섹션 헤더 제외**(§5): 섹션 항목을 먼저 모아 `count>0` 일 때만 헤더+항목 append.
- **제거**: 3-arg `BuildCellData(profile,cfg,ShopTab)`, `MatchesTab`, `_tab`, `SelectTab`, `UpdateTabHighlight`, 탭 `[SerializeField]`(`_statTabButton`/`_speciesTabButton`/`_statTabBg`/`_speciesTabBg`)·`TabActiveColor`/`TabInactiveColor`, InitUI 탭 배선.
- `Rebuild` 은 `BuildCellData(_arg.Profile, _arg.Config)` 무탭 호출. 아이콘 주입 루프 유지(헤더 행은 `Species=null`→`Icon=null`, 무해).

### 셀 위젯 (`[SerializeField] private`, Rule 02 §6.1)
- `_headerText` (CHText) — 섹션 제목(§1).
- `_headerBg` (Image) — 배너 밴드 배경 + 하단 구분선(§1·2.1). 밴드 `rgba(255,255,255,0.03)`, 하단 구분선 1px `rgba(255,255,255,0.12)`(구분선은 `_headerBg` 자식 별도 Image 로 두어도 됨).
- `_headerAccent` (Image) — 금 액센트 바 3×22, `#FBBF24`(0.984,0.749,0.141).
- 위 3개 = **헤더 전용 위젯 그룹**. `Bind` 이 `RowKind==SectionHeader` 면 이 그룹 on + 항목 위젯 그룹 off, `Item` 이면 반대.

> **plan sync (delta)**: plan Task 3/4 는 헤더 위젯을 `_headerText` **1개**로만 명세한다. 본 기획서는 배너 스타일(§1·2.1) SoT 로 `_headerBg`·`_headerAccent` **2개를 추가**해 헤더 위젯을 **3개**로 확장한다. plan↔기획서 sync 규칙상 이 delta 를 명시 — gameplay-programmer 는 plan Task 3(셀 위젯)·Task 4(프리팹 배선)에서 헤더 위젯 3개(`_headerText`/`_headerBg`/`_headerAccent`)를 기준으로 구현·배선한다. `SetItemWidgetsActive`(plan) 는 이 3개 그룹 토글까지 포함.

### Bind 분기 + 풀 재사용 리셋
- `Bind` 진입에서 `RowKind` 분기(plan Task 3):
  - `SectionHeader`: `_headerText.SetText(HeaderText)`, 헤더 그룹 on, **항목 위젯 전부 off**(이름/레벨/설명/가격/구매버튼/아이콘/발광 프레임·힌트링), 즉시 return.
  - `Item`: 헤더 그룹 off, 기존 항목 바인딩 그대로(`BindSpeciesGlow` 포함).
- **풀 재사용 잔상 방지**: 헤더↔항목 전이 시 매 `Bind` 가 두 그룹 표시를 완전 재설정(RowKind 마다 명시 on/off). 헤더 셀이 항목으로 재사용될 때 헤더 밴드/제목 잔존 없음, 반대도 마찬가지.

### 셀 높이 / 스크롤 값 (프리팹 배선, Task 4)
- 헤더 행 높이 = 항목과 동일 **84px**(§2, 프레임워크 강제). 헤더 셀 내부: 상단 24 스페이서 + 배너 60(§2.1).
- `_itemGap.y = 8`, `_padding.top = 8`, `_padding.bottom = 16`(§4).

### 프리팹 (Task 4 — ⛔ 목업 승인 게이트 선행)
- `ShopPopup` 프리팹: 탭 버튼 2개 + 강조 배경 Image 제거.
- `ShopItemCell` 프리팹: `_headerText`/`_headerBg`/`_headerAccent` 헤더 그룹 자식 추가·배선(항목 위젯과 같은 셀, 겹쳐 두고 그룹 토글). 헤더 스타일 = §1·2.1.
- 요약줄 `_bonusSummaryText` 는 그대로(§3).

### 테스트 갱신 (Rule: plan Task 1 · plan line 233)

**현재 ShopPopup 관련 EditMode 테스트는 5개**(전수 재조사 — design-reviewer 지적으로 정정. 이전 초안의 "2개 / `ShopPopupCellDataTests` 없음" 단정은 **오기, 철회**함 — `Meta/` 하위를 누락한 부실 조사였다):

| 테스트 파일 | 호출 | 이번 작업 영향 | 갱신 |
|---|---|---|---|
| `Meta/ShopPopupCellDataTests.cs` | **2-arg** `BuildCellData(profile,cfg)` | ⚠️ **런타임 red(컴파일 통과)** — 아래 상세 | **필수** |
| `ShopPopupTabFilterTests.cs` | 3-arg `BuildCellData(profile,cfg,ShopTab)` | 컴파일 깨짐(ShopTab 제거) | **필수** |
| `ShopPopupTabFilterEdgeTests.cs` | 3-arg `BuildCellData(...,ShopTab)` | 컴파일 깨짐 | **필수** |
| `Meta/ShopPopupSummaryTextTests.cs` | `BuildSummaryText` | 무변경(요약줄 유지 §3) | 무변경(안전) |
| `Meta/ShopPopupSummaryTextEdgeTests.cs` | `BuildSummaryText` | 무변경 | 무변경(안전) |

- **`ShopPopupCellDataTests` (BLOCKER — plan line 233이 경고한 그 파일)**: `MonsterHpUp` 단일 항목 config 로 **2-arg** 를 호출해 단정한다: `Assert.AreEqual(1, list.Count)`, `list[0].DisplayName=="강골 군세"`, `list[0].LevelText=="Lv 2/5"`, `list[0].IsMax`, `list[0].CanBuy` 등. 2-arg 시맨틱이 **헤더 인터리브 + 빈 섹션 제외**(§5)로 바뀌면 MonsterStat 1항목 → 스탯 섹션 항목 1개 → 헤더 추가 → 반환 `[스탯헤더(SectionHeader), MonsterHpUp(Item)]`. 결과: **count 1 → 2**, **`list[0]` 이 SectionHeader**(DisplayName=null, RowKind=SectionHeader)가 되어 두 테스트의 `list[0]` 단정이 전부 실패. **2-arg 시그니처는 유지되므로 컴파일은 통과·런타임만 조용히 red** → 반드시 갱신 범위에 포함.
  - **갱신 방향**: (a) `count` 단정을 인터리브 개수(헤더 1 + 항목 N)로, (b) 항목 필드 단정을 `list[0]` → **`list[1]`**(헤더 다음 첫 항목)로 이동하거나 `list.Find(c => c.RowKind==Item)` 로 헤더 스킵. LevelText/Price/IsMax/CanBuy 값 자체는 무변경(가공 로직 동일).
- **TabFilter 2개**: 3-arg 탭 필터 검증 → 탭 제거로 컴파일 깨짐 → 통합 리스트 구성(헤더 위치·순서·개수) 검증으로 재작성.
- **빈 섹션 케이스 추가**(§5): 한 섹션 0항목 config 에서 그 헤더가 제외되는지 검증.
- **구현 시 상점 테스트 전수 grep 권고**: 위 표는 현 시점 조사이나, 구현 착수 시 `Grep "BuildCellData|ShopPopup" Assets/_Lair/Tests/EditMode`(하위 폴더 포함)로 **전수 재확인** 후 갱신하라 — 2-arg 를 참조하는 어떤 테스트든 인터리브 형태로 뒤집는다.

---

## 8. Self-Review

- **Placeholder 잔존 0**: 미정 마커·애매한 권유·두 갈래 위임·본문 비움 참조·검산 누락 없음. 헤더 높이는 "선호"가 아니라 프레임워크 1차 코드 판독으로 (A) 강제 단정(§2, gameplay-programmer 재확인 명시). 간격은 검산(84=24+60, 섹션 간격 32=8+24)으로 단정.
- **스펙 커버리지**: spec §3.1 탭 제거·섹션 헤더 → §1·2 / §3.2 섹션 순서(스탯→몬스터) → §7 BuildCellData / §3.3 이종 셀 단일 타입(RowKind) → §7 / §3.4 BuildCellData 탭 제거 → §7 / §3.5·§2(요약줄 유지) → §3 / §5(가변 높이 이슈) → §2 (A) 확정 / §6 game-designer 확정 4항(헤더 스타일·높이·요약줄·문구·간격) → §1·2·3·4 / spec §7 빈 섹션 언급 → §5. **plan line 233(2-arg BuildCellData 시맨틱 변경 → 테스트 갱신)** → §7 테스트 표(`ShopPopupCellDataTests` breaking 적시). 갭 0.
- **테스트 조사 정정(design-reviewer 1차 반영, BLOCKER)**: 초안의 "상점 EditMode 테스트 2개 / `ShopPopupCellDataTests` 없음" 은 **오기 — `Meta/` 하위 누락한 부실 조사**였다. 전수 재조사로 **5개**(`Meta/ShopPopupCellDataTests`·`Meta/ShopPopupSummaryTextTests`·`Meta/ShopPopupSummaryTextEdgeTests`·`ShopPopupTabFilterTests`·`ShopPopupTabFilterEdgeTests`) 확인, §7 표로 각 영향·갱신 명시. `ShopPopupCellDataTests`(2-arg, 컴파일 통과·런타임 red)를 갱신 범위에 적시(count 1→2, `list[0]`=헤더 → 항목 단정 `list[1]` 이동/헤더 스킵). "구현 시 전수 grep" 권고 추가.
- **plan sync delta(design-reviewer 개선①)**: 헤더 위젯을 plan 의 1개(`_headerText`) → **3개**(`_headerText`/`_headerBg`/`_headerAccent`)로 확장하는 delta 를 §7 에 명시(배너 스타일 SoT).
- **내부 일관성**: 헤더 높이 84(§2·7), 내부 24+60(§2.1·7), 간격 8/32(§4·6·7), 문구 "스탯 강화"/"몬스터 강화"(§1·7), 요약줄 유지(§3·7) — 본문·표·구현요청 동일.
- **시그니처/명명 일관성**: `ShopRowKind{SectionHeader,Item}`·`ShopItemCellData.RowKind`·`HeaderText`·`BuildCellData(profile,cfg)` 2-arg·`ShopItemCell._headerText`/`_headerBg`/`_headerAccent` — plan 과 일치. 제거 대상(`ShopTab`/`MatchesTab`/`_tab`/`SelectTab`/`UpdateTabHighlight`/탭 필드) 전부 명시. Grep 자체 확인.
- **모호 표현 0**: "적당히/유연하게/또는(디자인 결정)" 없음. (A)/(B) 두 갈래는 프레임워크 증거로 (A) 단일화(gameplay-programmer 확인만 남김).
- **스코프**: 단일 구현 단위(상점 탭 제거 + 헤더 통합). 분할 불필요.
- **구현 요청사항 완전성**: Enum(신설/제거)·데이터 필드·시그니처·위젯·Bind·프리팹·테스트 모두 명세.
- **레이아웃 사실 정정**: 코디네이터 "4열"은 도감 작업 carry-over 오기 — 상점은 `_columnCount:1` 단일 컬럼(§0, 1차 확인). 4열 그리드는 spec 위반이라 미채택, 단일 컬럼으로 설계.
- **UI 목업**: `.mockups/shop-single-scroll-sections.html` 작성(다크 #262626·흰 아웃라인·Jua·**단일 컬럼** 리스트·상단 요약줄·2 섹션 헤더 배너[금 액센트 바+구분선]·항목 행[아이콘/이름/레벨/설명/가격/구매]·헤더 84px 내부 24+60 구성·섹션 간격·빈 섹션 토글 데모·폰트 타이틀18>헤더17>항목명16). **목업 범위**: 본 작업은 항목 행을 바꾸지 않으므로(spec 락) 항목 행은 맥락 표시용이며, 목업 승인 = **헤더 + 섹션 트리트먼트** 승인.

판정: **통과**.
