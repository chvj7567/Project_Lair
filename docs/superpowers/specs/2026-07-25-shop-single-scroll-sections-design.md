# 상점 단일 스크롤뷰 + 섹션 헤더 (탭 제거) Design Spec

- **작성일**: 2026-07-25
- **단계**: v0.3 (기존 상점 UI 재구성 — 신규 기능 아님, 범위 내)
- **분류**: UI 재구성 (상점)
- **문서 성격**: spec — 무엇을 만들지의 골격 + 결정 락. 헤더 스타일·높이 등 수치/디자인은 game-designer 기획서가 확정.

---

## 1. 의도

현재 상점은 「스탯 강화」/「몬스터 강화」를 **탭**으로 분리해, 몬스터 강화를 보려면 탭 전환(추가 클릭)이 필요하다. 이를 **탭 없이 단일 스크롤뷰**로 바꿔, 두 종류를 **섹션 헤더로 구분한 하나의 목록**에서 스크롤로 한 번에 훑게 한다. 추가 클릭 없이 전체 강화 항목을 연속 탐색하는 UX.

## 2. 범위

### 포함
- 상점 탭 UI 제거(`_statTabButton`/`_speciesTabButton`, `ShopTab` enum, 탭 상태·필터).
- 단일 스크롤뷰에 **섹션 헤더 행 + 항목 행**을 순서대로 표시: 「스탯 강화」 섹션 → 「몬스터 강화」 섹션.
- 상단 "현재 강화" 요약줄 **유지**(기본안 — game-designer가 재검토 가능).

### 비포함
- 상점 항목 자체의 추가/변경(스탯·스포너·6종족 그대로).
- 강화 밸런스·가격·효과 변경.
- 신규 상점 기능.

## 3. 핵심 결정 (락)

1. **탭 제거 → 단일 스크롤 + 섹션 헤더.** 「스탯 강화」·「몬스터 강화」 각 섹션 앞에 제목 헤더 행.
2. **섹션 순서**: 스탯 강화(전종 글로벌: MonsterStat·SpawnerPeriod) → 몬스터 강화(MonsterSpecies 6종). 현재 탭 순서 유지.
3. **이종 셀을 단일 셀 타입으로 통합.** `CHPoolingScrollView<TItem,TData>`는 셀 타입 1개 바인딩이므로, 헤더 행과 항목 행을 **한 셀 타입**이 `RowKind`(Header/Item)로 분기해 그린다(헤더면 제목만, 항목이면 기존 항목 UI, 나머지 위젯 off).
4. **`BuildCellData`는 탭 인자 제거** — 헤더 행을 끼운 통합 리스트를 반환(스탯 헤더 + 스탯 항목 + 몬스터 헤더 + 종족 항목).
5. **요약줄 유지**(기본).

## 4. 아키텍처 — 기존 상점 재구성

현 구조(작업 I):
- `ShopPopup`: `_statTabButton`/`_speciesTabButton` + 단일 `_scrollView`(`ShopItemPoolingScrollView<ShopItemCell,ShopItemCellData>`) + `ShopTab _tab` + `BuildCellData(profile,cfg,tab)` 필터.

변경:
- `ShopPopup`: 탭 필드·`ShopTab`·`_tab`·탭 클릭 배선 제거. `Rebuild`이 탭 없는 통합 `BuildCellData`로 `SetItemList`.
- `ShopItemCellData`: `RowKind`(enum: `SectionHeader`/`Item`) + `HeaderText`(헤더 행일 때) 추가. 기존 항목 필드는 Item 행에서만 유효.
- `ShopItemCell`(또는 통합 셀): `Bind`이 `RowKind`로 분기 — Header면 제목 텍스트만 표시하고 이름/레벨/가격/구매버튼 등 항목 위젯 off, Item이면 기존대로.
- `BuildCellData(profile,cfg)`: 스탯 헤더 행 → 스탯 항목들 → 몬스터 헤더 행 → 종족 항목들 순으로 통합 리스트 생성. `MatchesTab`/`ShopTab` 로직 제거.

## 5. ⚠️ 핵심 기술 이슈 — 가변 행 높이

`CHPoolingScrollView` 가상화가 **고정 행 높이**를 가정하면, 헤더 행(짧음)과 항목 행(김)의 높이 차이로 스크롤/풀 계산이 어긋난다. 해소안 중 하나를 game-designer/plan이 확정:
- **(A) 헤더를 항목과 같은 높이로** — 헤더 셀도 항목 셀과 동일 높이(제목만 있고 나머지 빈 공간). 가장 안전(고정 높이 유지).
- **(B) 가변 높이 지원 확인** — CHPoolingScrollView가 셀별 가변 높이를 지원하면 헤더를 짧게. (프레임워크 확인 필요.)
- 구현 착수 전 이 이슈를 반드시 확정. gameplay-programmer가 CHPoolingScrollView 구현을 확인해 (A)/(B) 결정.

## 6. game-designer가 확정할 것

- 헤더 행 **스타일**(폰트·색·정렬·구분선 유무)과 **높이 처리**(§5 (A)/(B) 방향 — 단, 프레임워크 제약은 gameplay-programmer가 최종 확인).
- "현재 강화" 요약줄 **최종 유지 여부**.
- 섹션 제목 문구("스탯 강화"/"몬스터 강화" 그대로인지).
- 스크롤 시작 시 상단 여백·섹션 간 간격.

## 7. 구현 요청사항 (개요)

- Enum: `ShopTab` 제거, `RowKind`(SectionHeader/Item) 신설(ShopPopup 내부 또는 데이터 옆).
- `ShopItemCellData`에 `RowKind`·`HeaderText` 추가.
- `BuildCellData` 시그니처 3-arg(tab) → 2-arg 통합. **기존 테스트(ShopPopupTabFilterTests 등)는 탭 제거에 맞춰 갱신** — 통합 리스트 구성(헤더 위치·순서) 검증으로 전환.
- 셀 `Bind` RowKind 분기 + 풀 재사용 리셋(헤더↔항목 전이 시 위젯 상태 완전 재설정).
- 프리팹: ShopPopup 탭 버튼 제거, 헤더 표현 배선(§5 확정 후). **UI 목업 승인 게이트**(Rule 00) 후 배선.

## 8. 테스트 관점

- `BuildCellData(profile,cfg)` — 통합 리스트가 [스탯헤더, 스탯항목…, 몬스터헤더, 종족항목…] 순서·개수로 구성되는가. 항목 데이터(가격·레벨) 기존과 동일.
- 셀 `Bind` RowKind 분기 — 헤더/항목 위젯 표시 전환, 풀 재사용 잔상 없음(헤더↔항목).
- 회귀: 구매 로직(ShopService)·요약줄(유지 시) 무변경.
