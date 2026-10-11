# 상점·도감·기록 팝업(ShopPopup / CodexPopup / RecordsPopup) 통합 비주얼 폴리시

## § 헤더

- **목표**: 이미 구현·승인된 선행 폴리시 5종(`synergy-modal-visual-polish.md` · `build-modal-visual-polish.md` · `card-selection-visual-polish.md` · `village-hud-visual-polish.md` · `worldmap-popup-visual-polish.md`, 이하 "선행서")의 디자인 언어(금색 이중 프레임·모서리 장식·상단 광택·룬/금 페이드 라인, 금테 제목판+마름모, 붉은 입체 닫기 버튼, 유리 inset, 상태색 발광, 마름모 보석, 금색 슬림 스크롤바, 입체 버튼 톤 탭)를 **상점 · 도감 · 기록** 세 팝업과 그 셀 3종(`ShopItemCell` · `CodexCell` · `RecordsStageCell`)에 한 번에 입힌다. 세 팝업은 **공통 틀**(§2)을 한 번 정의해 같이 쓰고, 팝업별 차이만 §3~§5 에 적는다.
- **검증 가설**: 레이아웃이 같은 상태에서 마감만 올려도, 마을 HUD → 상점/도감/기록 으로 이어지는 흐름에서 화면 톤이 끊기지 않고, 구매 가능/만렙/소울 부족, 해금/미조우/잠금, 선택 중/잠금 스테이지 같은 **상태 구분이 색·테두리만으로 한눈에 읽히는가**. 정량 메트릭 없음 — 사용자 육안 확인용 UI 개선.
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용" + "메타 진행 허용(상점/도감·기록)". 신규 콘텐츠·시스템·화면 없음. UI 목업은 사용자 사전 승인으로 갈음(요청 메시지 명시) — `.mockups` 신규 작성 없음.
- **핵심 메커니즘**: (1) **신규 스프라이트 0종** — 선행서가 만든 `WorldMap*` · `VillageHud*` · `BattleHud*` · `SynergyModal*` · `BuildModal*` · `CardSelect*` · `UISoftGlow` 와 기존 `UiDot/Px_Solid` 만으로 전부 구성한다. (2) 장식은 새 `Deco*` GameObject 를 **추가**하는 방식이며 기존 위젯은 재부모·이동하지 않는다. (3) 코드 변경은 `ShopItemCell` 1개 파일(필드 2개와 활성 토글 1곳·색 칠하기 1곳, §7)뿐이고 `CodexCell` · `RecordsStageCell` · 세 팝업 `.cs` 는 변경 0건이다. (4) `UILoopMotion` 사용 0건(§6).

이 문서의 수치가 선행서와 다르면 이 문서가 이 세 팝업에서 우선한다. 의도적 포기·변경은 §10 에 모았다.

---

## 1. 불변 사항

- 기존 위젯의 위치·크기·앵커·부모-자식 관계·글자 크기·글자 색·문구·ViewModel·데이터 가공 로직은 불변이다. 허용되는 변경은 (a) 기존 Image 의 sprite·color·Image Type 교체, (b) **장식 전용 기존 Image**(`HeaderAccent` · `HeaderDivider` · `HeaderBg` · `IconSlot` · `LevelPip0~4` · `TitlePlaque`)의 크기·스프라이트 교체, (c) 새 `Deco*` GameObject 추가, (d) 형제 순서 조정, (e) §4.3 의 도감 탭 선택 표현 교체, (f) §5.4 의 기록 셀 폭 보정 1건이다.
- **Dim 불변**: 세 팝업의 `Dim` 색 (r 0.0157, g 0.0235, b 0.0392, a 0.7216) 과 `raycastTarget` 켬 상태를 그대로 둔다. `ModalBody` Image 의 `raycastTarget` 도 켬 유지(모달 위 클릭이 Dim 으로 새지 않게).
- 기존 `[SerializeField]` 전부 이름·타입·연결 유지. 특히 세 `CHPoolingScrollView` 의 `_origin`(Content 안 셀 PrefabInstance) · `_itemGap` · `_columnCount` · `_padding` · `_align` 과 `ScrollRect` · `Viewport`(RectMask2D) · `Content` 연결을 훼손하지 않는다(Rule 03 §3). origin 셀 인스턴스에서 컴포넌트를 제거(`m_RemovedComponents`)하지 않는다. 셀 변경은 **셀 프리팹 파일에만** 하고 팝업 프리팹 안 PrefabInstance 에는 오버라이드를 새로 만들지 않는다(예외: §5.4 의 기록 셀 폭 보정 1건).
- `Strings_Ko` 신규 문구 0건, 신규 JSON·SO·Enum·Interface·Addressable 키 0건. 신규 TMP 위젯 0건(스프라이트에도 글자를 굽지 않는다).
- 신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작하고 모든 장식 Graphic 은 `raycastTarget = false`. 클릭 대상은 기존 `Dim` · `CloseButton` · 탭 `Toggle` · `BuyButton` 과 신규 스크롤바(`Scrollbar` 트랙 · `Handle`)뿐이다.
- 신규 글리프 의존 0건(× · ★ 도 스프라이트나 기존 글리프를 그대로 쓰며 새로 추가하지 않는다).
- **교훈 반영(구현 규칙)**:
  1. 새 MonoBehaviour 클래스는 만들지 않는다. 만약 구현 중 필요해지면 **파일명 = 클래스명 별도 파일 1개당 MonoBehaviour 1개**로 만든다(같은 파일에 MonoBehaviour 를 둘 이상 두면 Missing Script 가 된다).
  2. **프리팹 편집은 UnityMCP 로 수행**한다(YAML 직접 편집·새로 만들어 덮어쓰기 금지). 일회용 에디터 스크립트를 쓰면 완료 직후 삭제한다(Rule 04 §3).
  3. 코드 한 줄짜리 `if` 도 **가드 절(즉시 `return`/`continue`)이 아니면 중괄호**를 쓴다(Rule 02 §2). 주석은 `//#`, `var` · `!` 금지.
  4. 현재 프리팹 값(Dim 알파, 글자 색, 글자 크기, Selectable 눌림 색 등)은 이 문서가 바꾼다고 명시하지 않은 한 유지한다.
- 공통 용어(선행서와 같음): 글로우 = `Assets/_Lair/Art/Sprites/UISoftGlow.png`. 그라데이션 V = `WorldMapGradientV`(알파 1→0, 위→아래), 그라데이션 H = `WorldMapGradientH`(알파 1→0, 좌→우). 단색 = `Assets/_Lair/Art/Sprites/UiDot/Px_Solid.png`. 좌표는 별도 표기가 없으면 **부모 좌상단 기준 (x, y 아래로 증가)**, 크기는 `가로x세로`. 알파 환산: 0.96=0xf5/255, 0.80=0xcc/255, 0.60=0x99/255, 0.47=0x77/255.

---

## 2. 공통 틀 (세 팝업 공유)

선행서 `build-modal-visual-polish.md` §2.1·2.4 의 모달 레시피를 이 세 팝업에 맞춰 **한 번 정의**한다. `W`·`H` 는 각 팝업 `ModalBody` 의 크기다.

| 팝업 | W x H | 프리팹 |
|---|---|---|
| ShopPopup | 720 x 480 | `Assets/_Lair/Art/UI/ShopPopup.prefab` |
| CodexPopup | 720 x 480 | `Assets/_Lair/Art/UI/CodexPopup.prefab` |
| RecordsPopup | 480 x 560 | `Assets/_Lair/Art/UI/RecordsPopup.prefab` |

### 2.1 프레임·헤더·닫기 (팝업 프리팹, 3곳 동일 레시피)

루트(`ShopPopup` / `CodexPopup` / `RecordsPopup`)의 자식 순서: `Dim` → `DecoModalGlow` → `ModalBody`. `ModalBody` 의 자식 중 **장식 배경군**(`DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL` · `TR` · `BL` · `BR`)은 기존 자식보다 앞(형제 인덱스 0~7)에 두고, `DecoPlaqueGlow` 는 `TitlePlaque` 바로 앞, `DecoCloseGlow` 는 `CloseButton` 바로 앞(마지막에서 두 번째)에 둔다. 기존 자식(`TitlePlaque` · `Title` · 팝업별 콘텐츠 · `CloseButton`)의 상대 순서는 불변이다.

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 모달 바탕 | 기존 `ModalBody` Image 스프라이트 `Px_Panel` → `Px_Solid`, Image Type Simple, 색 `#0d1220` 알파 0.96. `Shadow` 컴포넌트 추가: effectDistance (0,−8), 색 `#000000` 알파 0.47 | `Px_Solid` |
| 바탕 그라데이션 | `DecoModalGrad`: stretch 0, 그라데이션 V, 색 `#1b2336` 알파 0.95 | `WorldMapGradientV` |
| 내측 비네트 | `DecoModalVignette`: stretch 0, 색 `#000000` 알파 0.75 (스프라이트 최대 알파 0.80 × 0.75 = 0.60) | `WorldMapVignette` |
| 상단 유리 광택 | `DecoModalGloss`: 좌상단 (8,8), 크기 (W−16) x 34, 그라데이션 V, 색 `#ffffff` 알파 0.09 | `WorldMapGradientV` |
| 금테 이중 프레임 | `DecoModalFrame`: stretch 0, Sliced, fillCenter 끔(9px 링: 바깥 3px `#07090e` · 2px `#6b5a2a` · 2px `#07090e` · 2px `#3a4764`) | `WorldMapFrame` |
| 모서리 장식 4개 | `DecoCornerTL/TR/BL/BR` 각 14x14. 좌표 TL (4,4) / TR (W−18,4) / BL (4,H−18) / BR (W−18,H−18). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` |
| 금빛 외곽 발광 | 루트 직속 `DecoModalGlow`: 중앙 앵커, 크기 (W+44) x (H+44), 색 `#f7c64a` 알파 0.13 | `UISoftGlow` |
| 제목판 바탕 | 기존 `TitlePlaque`(320x52, 상단 중앙 앵커, 위치 (0,0), 피벗 (0.5,0.5)) Image 스프라이트 `Px_Plaque` → `Px_Solid`, Simple, 색 `#141a2c`. 자식 `DecoPlaqueGrad`(stretch 0, 그라데이션 V, 색 `#2a3450` 알파 1.0), `DecoPlaqueFrame`(stretch 0, Sliced, fillCenter 끔) | `Px_Solid`, `WorldMapGradientV`, `WorldMapFrame` |
| 제목판 발광 | `DecoPlaqueGlow`: `TitlePlaque` 바로 앞 형제, 앵커·위치 `TitlePlaque` 와 동일(상단 중앙, (0,0)), 크기 356x88, 색 `#f7c64a` 알파 0.27 | `UISoftGlow` |
| 제목판 양끝 마름모 | `TitlePlaque` 자식 `DecoPlaqueGemL`/`DecoPlaqueGemR`: 18x18, 중앙 앵커, 위치 (−133,+4) / (+133,+4) (y 위쪽이 +), 색 `#f7c64a`. 렌더 대각선 18 × 28/36 = 14 | `VillageHudGem` |
| 제목 글자 | 기존 `Title`(CHText, 22, `#e8e1cf`) 불변 | — |
| 닫기 버튼 몸체 | 기존 `CloseButton`(44x44, 우상단 앵커, 피벗 (1,1), 위치 (13,13)) Image 스프라이트 `Px_CloseX` → `VillageHudCta`, Image Type Simple → Sliced. `Selectable` 의 Normal 색 (0.9,0.9,0.9,1) → (1,1,1,1) (스프라이트가 색을 굽고 있어 흰색 전제, 선행서 마을 HUD §2.3 과 동일). Highlighted·Pressed(0.75)·Disabled 색, `CHButton`, `_closeButton` 연결 불변. 기존 비활성 자식 `X`(TMP "×")는 건드리지 않는다 | `VillageHudCta` |
| 닫기 × 글리프 | `CloseButton` 자식 `DecoCloseGlyph`: 20x20 중앙, 색 `#ffd6d8` | `BuildModalCloseGlyph` |
| 닫기 붉은 발광 | `DecoCloseGlow`: `CloseButton` 바로 앞 형제, `ModalBody` **중앙 앵커**, 위치 (W/2−9, H/2−9) (y 위쪽이 +), 크기 68x68, 색 `#e5484d` 알파 0.40. ShopPopup·CodexPopup = (351, 231), RecordsPopup = (231, 271) | `UISoftGlow` |

검산: 모서리 TR x = 720 − 14 − 4 = 702 / 480 − 14 − 4 = 462, BL y = 480 − 14 − 4 = 462 / 560 − 14 − 4 = 542. 광택 폭 = 720 − 16 = 704 / 480 − 16 = 464. 외곽 발광 = 720 + 44 = 764, 480 + 44 = 524 / 480 + 44 = 524, 560 + 44 = 604. 닫기 중심: 우측 가장자리 W + 13, 폭 44 → 중심 x = W + 13 − 22 = W − 9 → 모달 중심 기준 W/2 − 9. 위쪽 가장자리가 H 바깥 13 → 중심 y = H/2 − 9. 제목판 발광 356 = 320 + 2×18, 88 = 52 + 2×18. 마름모 ±133 = 160 − 27.

### 2.2 섹션 금 페이드 라인 (콘텐츠 상단과 목록 사이)

`DecoSectionLine`: `ModalBody` 자식, 크기 (스크롤 영역 폭) x 2, 그라데이션 H, 색 `#f7c64a` 알파 0.60. 위치는 팝업별(§3.1·§4.1·§5.1). 상단 정보 영역과 목록 영역을 가르는 룬 라인 역할이다.

### 2.3 금색 슬림 스크롤바 (스크롤뷰 4개: Shop 1 · Codex 2 · Records 1)

선행서 `build-modal-visual-polish.md` §2.4 와 같은 구성이다. 각 `ScrollView` GameObject(= `ScrollRect` · `CHPoolingScrollView` 가 붙은 객체)의 자식으로 `Scrollbar` 를 추가한다. `Viewport` 안이 아니므로 마스크에 잘리지 않는다.

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| `Scrollbar` | UGUI `Scrollbar`(래퍼 미지원 컴포넌트, Rule 03 §3 예외). Direction Bottom To Top, Number Of Steps 0. 앵커 (0,0)–(0,1), 피벗 (0,0.5), 위치 (**뷰포트 폭 + 4**, 0), 크기 6 x (ScrollView 높이). 트랙 Image 색 `#05070c`, `raycastTarget` 켬 | `Px_Solid` |
| `SlidingArea` | `Scrollbar` 자식, 사방 오프셋 1 (내부 4 x (높이−2)) | — |
| `Handle` | `SlidingArea` 자식, stretch 0, Image 스프라이트 `BuildModalScrollThumb`, `raycastTarget` 켬. `Scrollbar.handleRect` 와 `targetGraphic` 연결 | `BuildModalScrollThumb` |
| `DecoThumbGlow` | `Handle` 자식, stretch 오프셋 −6, 색 `#f7c64a` 알파 0.67 | `UISoftGlow` |
| `DecoScrollTrackFrame` | `Scrollbar` 자식, stretch 오프셋 −1 (8 x (높이+2)), Sliced, fillCenter 끔, 색 `#ffffff` | `BattleHudTrackFrame` |
| `ScrollRect` 연결 | `Vertical Scrollbar` = 위 `Scrollbar`, `Vertical Scrollbar Visibility` = **Auto Hide**(내용이 넘칠 때만 표시, 뷰포트 크기 불변), `Vertical Scrollbar Spacing` 0 | — |

위치·크기 값: ShopPopup = 위치 x 668, 높이 336. CodexPopup 의 두 ScrollView(`MonsterScrollView` · `CardScrollView`) = 위치 x 668, 높이 336. RecordsPopup = 위치 x 428, 높이 308. 검산은 §3.1·§4.1·§5.1.

### 2.4 셀 바탕 3종 (공통 규칙)

| 용도 | 스프라이트 | 설정 |
|---|---|---|
| 정상 셀 바탕(유리 석판) | `SynergyModalCellBg` | Sliced, border (3,6,3,5), 색 `#ffffff`. 면 `#151b2c`, 위 안쪽 2행 `#3d4b6e`, 바깥 3px `#07090e` |
| 잠금 셀 바탕(어두운 평판) | `VillageHudBtnOff` | Sliced, border (6,7,6,6), 색 `#ffffff`. 면 `#232834` |
| 아이콘·초상 슬롯 면 | `Px_Solid` | Simple, 색 `#05070c` 알파 0.85. 자식 링(상점·도감 슬롯 = `DecoIconRing`, 기록 초상 슬롯 = `DecoPortraitRing`) = `CardSelectArtRing` Sliced fillCenter 끔, 색 `#f7c64a` (링 = 바깥 1px `#07090e` + 안쪽 2px 흰색 알파 0.60 × 금색) |
| 셀 그림자 | 셀 바탕 Image 에 `Shadow`: effectDistance (0,−2), 색 `#000000` 알파 0.40 | — |

### 2.5 재사용 스프라이트 목록 (신규 0장)

| 재사용 에셋 | 이 문서에서의 용도 |
|---|---|
| `UISoftGlow` | 모달·제목판·닫기 발광, 스크롤 핸들 글로우, 소울 칩·코인 후광 |
| `WorldMapGradientV` / `WorldMapGradientH` | 모달·제목판 그라데이션, 광택, 섹션 라인, 헤더 배너, 구매 버튼 광택 |
| `WorldMapFrame` / `WorldMapCornerGold` / `WorldMapVignette` | 모달 금테·모서리·비네트, 제목판 프레임 |
| `WorldMapNamePlate` | 도감 셀 이름 명패 |
| `VillageHudGem` | 제목판 마름모, 섹션 헤더 마름모, 상점 레벨 눈금 |
| `VillageHudCta` / `VillageHudBtn` / `VillageHudBtnOff` / `VillageHudChip` | 닫기 버튼 / 도감 탭 / 잠금 셀 바탕 / 소울 칩 |
| `BattleHudTrackFrame` | 요약·통계 inset 테두리, 스크롤 트랙 링 |
| `BuildModalCloseGlyph` / `BuildModalScrollThumb` / `BuildModalChipFrame` | 닫기 ×, 스크롤 핸들, 칩·배지 프레임 |
| `CardSelectFrame` / `CardSelectArtRing` / `CardSelectFadeLine` | 탭 선택 금테, 슬롯 링·상태 링, 통계 타일 상단 페이드 |
| `SynergyModalCellBg` | 정상 셀 바탕 |
| `UiDot/Px_Solid` | 모든 단색 바탕 |

위 에셋의 임포트 설정·크기는 선행서 값 그대로이며 이 작업에서 수정하지 않는다. 이제 여섯 화면이 참조하므로 삭제·리네임 금지.

---

## 3. ShopPopup (+ ShopItemCell)

### 3.1 현재 프리팹 구조 (실측)

`ShopPopup.prefab` — 루트 stretch (0,0)–(1,1), 컴포넌트 `ShopPopup`.

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `Dim` | stretch | 0 · 0 | 색 알파 0.72, `CHButton` |
| `ModalBody` | 중앙 | (0,0) · 720x480 | `Px_Panel` Sliced |
| `TitlePlaque` / `Title` | 상단 중앙, 피벗 (0.5,0.5) | (0,0) · 320x52 | `Px_Plaque` / "상점" 22 `#e8e1cf` |
| `BonusSummaryBg` | 상단 stretch-x, 피벗 (0,1) | (28,−60) · 폭 −300(= 420) x 44 | `Px_PanelSunk`, raycast 꺼짐. 화면 좌표 x 28~448, y 60~104 |
| `BonusSummaryText` | 상단 stretch-x, 피벗 (0,1) | (40,−60) · 폭 −324(= 396) x 44 | 16, `#8e98ad`, 좌측 정렬 |
| `SoulPill` | 우상단, 피벗 (1,1) | (−28,−60) · 232x44 | `Px_PanelDark`. 화면 x 460~692, y 60~104. 자식 `SoulCoin`(20x20, 좌중앙 (14,0)), `SoulText`(stretch, (15,0) · 폭 −54 높이 −8, 22 `#5ef0b4`) |
| `ScrollView` | stretch | (0,−44) · 폭 −56 높이 −144 (= 664 x 336) | 화면 x 28~692, y 116~452. `ScrollRect` 세로, `CHPoolingScrollView`(`ShopItemPoolingScrollView`): `_itemGap` (0,8), `_padding` 상 8·하 16, `_columnCount` 1, `_align` 1 |
| `CloseButton` | 우상단, 피벗 (1,1) | (13,13) · 44x44 | `Px_CloseX` Simple |

`ShopItemCell.prefab`(= origin 인스턴스 `ShopItemCell`, 팝업 안에서 **오버라이드 없이** 664x108) — 루트 Image `Px_Panel`(raycast 켬). 자식:

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `HeaderBg` | stretch | 0 | `Px_PanelDark` Sliced. 자식 `HeaderDivider`(하단 stretch-x, 피벗 (0.5,0), 높이 8, `Px_Divider`) |
| `HeaderAccent` | 좌중앙, 피벗 (0,0.5) | (16,0) · 8x28 | `Px_Solid`, 색 `#5ef0b4` |
| `HeaderText` | 좌중앙, 피벗 (0,0.5) | (36,0) · 400x34 | 22, `#e8e1cf` |
| `StateRing` | stretch | 0 | `Px_Ring` Sliced fillCenter 끔, 색은 코드가 칠함(소울/금) |
| `IconSlot` | 좌중앙, 피벗 (0,0.5) | (12,0) · 68x68 | `Px_PanelSunk` Sliced |
| `IconImage` | 좌중앙, 피벗 (0.5,0.5) | (46,0) · 52x52 | 종족 아이콘(PreserveAspect) |
| `GlowFrame` / `GlowHintRing` | 좌중앙, 피벗 (0.5,0.5) | (46,0) · 76x76 / 84x84 | `Px_Ring` Sliced fillCenter 끔, 색·알파는 코드가 칠함 |
| `NameText` | 좌상단, 피벗 (0,1) | (96,−6) · 250x32 | 22(자동 크기), `#e8e1cf` |
| `LevelPips`(+ `LevelPip0~4`) | 좌상단, 피벗 (0,1) | (96,−44) · 130x12. 눈금 각 20x12, 좌중앙 피벗 (0,0.5), x = 0 / 26 / 52 / 78 / 104 | `Px_Solid`, 색은 코드가 칠함(금/`#48546e`) |
| `LevelText` | 좌상단 | (400,−12) · 90x22 | 비활성(눈금이 대체) |
| `DescText` | 좌상단, 피벗 (0,1) | (96,−62) · 258x44 | 16, `#8e98ad` |
| `PriceText` | 우중앙, 피벗 (1,0.5) | (−156,0) · 150x32 | 22(자동 크기), `#5ef0b4` |
| `BuyButton` | 우중앙, 피벗 (1,0.5) | (−16,0) · 128x52 | `Px_BtnSoul`, `Selectable`(ColorTint, Normal 0.9 · Pressed 0.75), `CHButton`. 자식 `Label`(stretch, 22) |

기존 자식의 형제 순서는 이 문서가 건드리지 않는다(아래 §3.3 의 삽입 위치만 지정).

### 3.2 팝업 프리팹 변경 (`ShopPopup.prefab`)

공통 틀 §2.1 을 `W=720, H=480` 으로 적용한다. 팝업 고유 항목:

`ModalBody` 자식 최종 순서(아래→위): `DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL` · `TR` · `BL` · `BR` → `DecoPlaqueGlow` → `TitlePlaque` → `Title` → `BonusSummaryBg` → `BonusSummaryText` → `DecoSoulGlow` → `SoulPill` → `DecoSectionLine` → `ScrollView` → `DecoCloseGlow` → `CloseButton`.

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 요약 줄 바탕 | 기존 `BonusSummaryBg` 스프라이트 `Px_PanelSunk` → `Px_Solid`, Simple, 색 `#05070c` 알파 0.80. 자식 `DecoSummaryFrame`(stretch 0, Sliced, fillCenter 끔, 색 `#ffffff`), `DecoSummaryStrip`(바탕 로컬 좌상단 (2,2), 3x40, 색 `#f7c64a`, 스프라이트 `Px_Solid`). 두 자식 모두 `BonusSummaryText` 보다 앞(바탕의 자식이라 자동으로 텍스트 아래) | `Px_Solid`, `BattleHudTrackFrame` |
| 요약 글자 | 기존 `BonusSummaryText` 불변(16, `#8e98ad`, 위치 (40,−60)). 스트립 x 30~33 과 글자 시작 x 40 사이 7px | — |
| 소울 칩 | 기존 `SoulPill` 스프라이트 `Px_PanelDark` → `VillageHudChip`(Sliced, border (5,5,5,5)), 색 `#ffffff`. `SoulText`·`SoulCoin` 불변 | `VillageHudChip` |
| 소울 칩 후광 | `DecoSoulGlow`: `SoulPill` 바로 앞 형제, 좌상단 (446,44), 크기 260x76, 색 `#5ef0b4` 알파 0.13. (칩 중심 (576,82) − (130,38)) | `UISoftGlow` |
| 소울 코인 발광 | `DecoCoinGlow`: `SoulPill` 자식, `SoulCoin` 앞(자식 인덱스 0), 칩 로컬 좌상단 (6,4), 크기 36x36, 색 `#5ef0b4` 알파 0.50. (코인 중심 칩 로컬 (24,22) − 18) | `UISoftGlow` |
| 섹션 라인 | `DecoSectionLine`: 좌상단 (28,108), 664x2 (§2.2). 요약 줄·소울 칩 하단 104 와 목록 상단 116 사이 | `WorldMapGradientH` |
| 스크롤바 | §2.3, `ScrollView` 자식. 위치 x 668, 높이 336 | — |

검산: 목록 영역 y = 240 − (−44 + 168) → 상단 116, 하단 116 + 336 = 452, 하단 여백 480 − 452 = 28. 스크롤바 x = 664 + 4 = 668 → 모달 x 28 + 668 = 696 ~ 702 (프레임 안쪽 9px 경계 711 이내, 우측 여백 18). 요약 줄 끝 448 과 소울 칩 시작 460 사이 12. 라인 y 108~110 은 정보 영역 하단 104 와 목록 상단 116 사이.

### 3.3 셀 프리팹 변경 (`ShopItemCell.prefab`)

한 셀 프리팹이 **항목 행**과 **섹션 헤더 행**을 모두 그린다(`RowKind`). 헤더 행은 코드가 항목 위젯을 숨기고 헤더 위젯 3개(`HeaderText` · `HeaderBg` · `HeaderAccent`)를 켠다.

**셀 루트**: Image 스프라이트 `Px_Panel` → `Px_Solid`, 색 `#000000` 알파 0(드래그 스크롤용 raycast 면, `raycastTarget` 켬 유지). 바탕은 아래 `DecoItemGroup` 이 그린다(헤더 행에서 항목 바탕이 비치지 않게).

**삽입**: `DecoItemGroup`(GameObject, stretch 0, Graphic 없음)을 셀 루트의 **형제 인덱스 0**에 추가한다. 자식은 `DecoCellBg` 하나.

**항목 행 요소**

| 요소 | 방식 | 스프라이트 · 수치 | 코드 참조 |
|---|---|---|---|
| 항목 바탕 | `DecoItemGroup/DecoCellBg`: stretch 0, §2.4 정상 셀 바탕, `Shadow` (0,−2) 알파 0.40 | `SynergyModalCellBg` | **신규 `_itemDecor`** (= `DecoItemGroup`) |
| 상태 링 | 기존 `StateRing` 스프라이트 `Px_Ring` → `CardSelectArtRing`(Sliced, fillCenter 끔). 색은 기존 코드가 칠함(구매 가능 `#5ef0b4` / 만렙 `#f7c64a`). 켜짐 조건(구매 가능이거나 만렙)은 기존 코드 그대로 | `CardSelectArtRing` | 기존 `_stateRing` |
| 상태 띠 | `StateRing` 자식 `DecoStateStrip`: 앵커 (0,0)–(0,1), 피벗 (0,0.5), 위치 (3,0), 크기 4 x (높이 −6) = 4x102, 스프라이트 `Px_Solid`, 프리팹 색 `#5ef0b4` 알파 1.0, **색은 코드 틴트(상태색)** | `Px_Solid` | **신규 `_stateTinted[1]`** |
| 상태 발광 | `StateRing` 자식 `DecoStateGlow`: 앵커 (0,0.5), 피벗 (0.5,0.5), 위치 (5,0), 크기 44x108, 프리팹 색 `#5ef0b4` 알파 0.40, **색은 코드 틴트(상태색)**. `DecoStateStrip` 앞(자식 인덱스 0) | `UISoftGlow` | **신규 `_stateTinted[0]`** |
| 종족 아이콘 슬롯 | 기존 `IconSlot` 스프라이트 `Px_PanelSunk` → `Px_Solid`, Simple, 색 `#05070c` 알파 0.85. 자식 `DecoIconRing`(stretch 0, §2.4). `IconSlot` 은 코드가 `SetActive` 로 켜고 끄므로 링도 함께 토글 | `Px_Solid`, `CardSelectArtRing` | 기존 `_iconSlot` |
| 종족 발광 프레임·힌트 링 | 기존 `GlowFrame`·`GlowHintRing` 불변(스프라이트·색·알파는 코드 소유) | — | 기존 `_glowFrame`·`_glowHintRing` |
| 레벨 눈금 5칸 | 기존 `LevelPip0~4`(`_levelPips[5]`) 스프라이트 `Px_Solid` → `VillageHudGem`(Simple), 크기 20x12 → 24x24. 위치 (0 / 26 / 52 / 78 / 104, 0)·앵커·피벗 (0,0.5) 불변. 색은 기존 코드가 칠함(켜짐 `#f7c64a` / 꺼짐 `#48546e`). 프리팹 초기 색 `#48546e` 유지. 눈금 중심 y = 44 + 6 = 50 → 마름모 영역 y 38~62 가 `NameText` 하단 38·`DescText` 상단 62 와 맞닿고 겹치지 않는다(렌더 대각선 24 × 28/36 = 18.7 이라 실제 모양은 y 40.7~59.3) | `VillageHudGem` | 기존 `_levelPips` |
| 이름·설명·가격 | 기존 `NameText` · `DescText` · `PriceText` 불변 | — | 기존 |
| 구매 버튼 면 | 기존 `BuyButton` Image 와 세 슬롯 `_buySoulSprite`(`Px_BtnSoul`) · `_buyOffSprite`(`Px_BtnOff`) · `_buyGoldSprite`(`Px_BtnGold`) **불변**(글자색 대비를 코드 변경 없이 보장 — §부록 결정 2). 버튼에 `Shadow` 추가: effectDistance (0,−3), 색 `#000000` 알파 0.47 | — | 기존 `_buyButton` · `_buyButtonImage` |
| 구매 버튼 광택 | `BuyButton` 자식 `DecoBuyGloss`: `Label` 앞(자식 인덱스 0), 버튼 로컬 좌상단 (4,4), 120x8, 그라데이션 V, 색 `#ffffff` 알파 0.25. 버튼이 `SetActive` 로 켜고 꺼지므로 함께 토글 | `WorldMapGradientV` | 없음 |

**섹션 헤더 행 요소** (모두 기존 위젯 재스킨, 코드 토글 그대로)

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 배너 바탕 | 기존 `HeaderBg` 스프라이트 `Px_PanelDark` → `WorldMapGradientH`(Simple), 색 `#243252` 알파 0.85. 왼쪽이 짙고 오른쪽으로 사라지는 배너 | `WorldMapGradientH` |
| 배너 하단 금 라인 | 기존 `HeaderDivider`(`HeaderBg` 자식) 스프라이트 `Px_Divider` → `WorldMapGradientH`, 색 `#f7c64a` 알파 0.80, 높이 8 → 2(하단 stretch-x, 피벗 (0.5,0) 불변) | `WorldMapGradientH` |
| 액센트 마름모 | 기존 `HeaderAccent` 스프라이트 `Px_Solid` → `VillageHudGem`(Simple), 크기 8x28 → 22x22, 위치 (16,0) → (9,0)(좌중앙 피벗 (0,0.5)). 색은 프리팹 값 `#5ef0b4` 유지. 렌더 대각선 22 × 28/36 = 17.1, 중심 x = 9 + 11 = 20 (기존 액센트 중심 16 + 4 = 20 과 동일) | `VillageHudGem` |
| 제목 | 기존 `HeaderText` 불변(36,0, 22, `#e8e1cf`). 마름모 우측 끝 렌더 x 20 + 8.6 = 28.6 < 36 | — |

### 3.4 상태별 시각 (구현자 확인표)

| 상태 | 판정(기존 코드) | 바탕 | 상태 링·띠·발광 | 버튼 | 눈금 |
|---|---|---|---|---|---|
| 구매 가능 | `CanBuy` | 정상 석판 | 소울색 `#5ef0b4` (`StateRing` 켬, `_stateTinted` 소울색, 발광 알파 0.40) | `Px_BtnSoul` + 광택, 라벨 "구매" 흰색 | Level 칸 금색 |
| 소울 부족 | `IsMax == false && CanBuy == false` | 정상 석판 | 링·띠·발광 모두 숨김 | `Px_BtnOff`, 라벨 "소울 부족" `#9ca3af` | Level 칸 금색 |
| 만렙 | `IsMax` | 정상 석판 | 금색 `#f7c64a` (`_stateTinted` 금색) | `Px_BtnGold`, 라벨 "만렙" `#07090e`, 가격 숨김 | 전 칸 금색 |
| 섹션 헤더 행 | `RowKind == SectionHeader` | `DecoItemGroup` 숨김, 루트 투명 | `StateRing` 숨김 | 항목 위젯 전부 숨김 | 숨김 |

---

## 4. CodexPopup (+ CodexCell, 몬스터/카드 탭)

### 4.1 현재 프리팹 구조 (실측)

`CodexPopup.prefab` — 루트 stretch, 컴포넌트 `CodexPopup`.

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `Dim` | stretch | 0 | 알파 0.72 |
| `ModalBody` | 중앙 | (0,0) · 720x480 | `Px_Panel` Sliced. 자식 순서: `TitlePlaque` · `Title` · `TabRow` · `CardScrollView` · `MonsterScrollView` · `CollectedText` · `CloseButton` |
| `TitlePlaque` / `Title` | 상단 중앙 | (0,0) · 320x52 | `Px_Plaque` / "도감" 22 |
| `TabRow` | 좌상단, 피벗 (0,1) | (28,−64) · 260x40 | 화면 x 28~288, y 64~104 |
| `MonsterTab` | 좌중앙, 피벗 (0,0.5) | (0,0) · 120x40 | Background Image `Px_Tab` Sliced, `Toggle`(transition None, graphic = `Checkmark`, toggleTransition Fade, `ToggleGroup` 공유, 초기 켜짐) + `CHToggle`. 자식 `Checkmark`(stretch, `Px_TabOn` Sliced), `Label`(stretch, "몬스터" 17 `#e8e1cf` 가운데) |
| `CardTab` | 좌중앙, 피벗 (0,0.5) | (130,0) · 120x40 | 위와 같은 구성, 초기 꺼짐, `Label` "카드" |
| `CollectedText` | 우상단, 피벗 (1,1) | (−28,−72) · 220x24 | 16, `#8e98ad`, 우측 정렬. 화면 x 472~692, y 72~96 |
| `MonsterScrollView` | stretch | (0,−44) · 폭 −56 높이 −144 (= 664 x 336) | 화면 x 28~692, y 116~452. `_itemGap` (8,8), `_columnCount` 4, `_align` 0. origin 셀 인스턴스 크기 **158x150** → 4 x 158 + 3 x 8 = 656 |
| `CardScrollView` | stretch | 같음 | 초기 **비활성**. `_itemGap` (6,6), `_columnCount` 6, `_align` 0. origin 셀 인스턴스 크기 **104x132** → 6 x 104 + 5 x 6 = 654 |
| `CloseButton` | 우상단 | (13,13) · 44x44 | `Px_CloseX` |

`CodexCell.prefab`(기본 108x136) — 루트 Image `Px_Panel`(`_background`, 코드가 `_normalSprite`/`_dummySprite` 로 교체, raycast 켬). 자식:

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `IconSlot` | 상단 중앙, 피벗 (0.5,1) | (0,−8) · 92x92 | `Px_PanelSunk` Sliced, raycast 꺼짐(코드가 토글하지 않음 — 잠금 더미도 빈 슬롯으로 보인다) |
| `GlowOverlay` | 상단 중앙, 피벗 (0.5,1) | (0,−4) · 100x100 | `Px_Ring` Sliced fillCenter 끔, 종족색 알파는 `EnhanceLevelVisual` 소유(`_glowOverlay`) |
| `Icon` | 상단 중앙, 피벗 (0.5,1) | (0,−12) · 84x84 | 카드 일러스트·몬스터 아이콘(`_icon`, `_iconRect`) |
| `NameText` | 하단 stretch-x, 피벗 (0.5,0) | (0,6) · 폭 −8 높이 30 | 17(자동 크기), `#e8e1cf` 가운데 |
| `LevelBadge` | 우상단, 피벗 (1,1) | (−6,−6) · 48x24 | CHText "Lv N" 16 `#f7c64a`, 강화 Lv>0 일 때만 켜짐(`_levelBadge`) |

슬롯: 몬스터 탭(4열) 셀 158x150 / 카드 탭(6열) 셀 104x132. 슬롯 하단 y = 8 + 92 = 100(양쪽 동일).

### 4.2 팝업 프리팹 변경 (`CodexPopup.prefab`)

공통 틀 §2.1 을 `W=720, H=480` 으로 적용한다. 팝업 고유:

`ModalBody` 자식 최종 순서(아래→위): `DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL` · `TR` · `BL` · `BR` → `DecoPlaqueGlow` → `TitlePlaque` → `Title` → `TabRow` → `DecoCollectedChip` → `CollectedText` → `DecoSectionLine` → `CardScrollView` → `MonsterScrollView` → `DecoCloseGlow` → `CloseButton`.

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 섹션 라인 | `DecoSectionLine`: 좌상단 (28,108), 664x2 (§2.2). 탭 하단 104 와 목록 상단 116 사이 | `WorldMapGradientH` |
| 수집 칩 | `DecoCollectedChip`: `CollectedText` 바로 앞 형제, 좌상단 (588,70), 크기 112x28, Sliced, 색 `#b9832a`(링 = 금 `#b9832a`, 안쪽 검정 2px, 중앙 `#0a0e18`). `CollectedText` 위치·크기·색 불변(우측 정렬, 오른쪽 끝 x 692). 칩 오른쪽 끝 700 − 692 = 8px 오른쪽 여백 | `BuildModalChipFrame` |
| 스크롤바 2개 | §2.3. `MonsterScrollView` · `CardScrollView` 각각의 자식. 위치 x 668, 높이 336 (두 개는 한 번에 하나만 활성) | — |

검산: 수집 칩 y 70~98 은 `CollectedText` y 72~96 의 위아래 2px 여백. 칩 x 588~700 은 목록 영역 y 116 이전이라 스크롤바(x 696~702, y 116 이후)와 겹치지 않는다. 몬스터 그리드 폭 656 ≤ 664, 카드 그리드 폭 654 ≤ 664.

### 4.3 탭 (몬스터 / 카드) — 입체 버튼 + 선택 금테

탭은 침입 지도 챕터 카드(선택 시 금 외곽 + 비선택 어두움)와 입체 버튼 톤을 따른다. 두 탭 모두 같은 구성이다(`MonsterTab` · `CardTab`).

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 탭 몸체(꺼짐 기본) | 기존 `Toggle` 의 Background Image 스프라이트 `Px_Tab` → `VillageHudBtn`, Sliced, 색 `#ffffff`. `Toggle` transition(None)·`CHToggle`·`ToggleGroup`·초기 켜짐/꺼짐 상태 불변 | `VillageHudBtn` |
| 탭 그림자 | Background Image 에 `Shadow`: effectDistance (0,−3), 색 `#000000` 알파 0.47 | — |
| 선택 표시(켜짐) | 기존 `Checkmark`(= `Toggle.graphic`, stretch 0) 스프라이트 `Px_TabOn` → `CardSelectFrame`, Sliced, **fillCenter 끔**, 색 `#f7c64a`. 켜짐일 때 금색 이중 링(바깥 `#07090e` 3px · 금 2px · 안쪽 `#07090e` 2px)이 몸체 위에 얹힌다. 꺼짐일 때는 `toggleTransition` Fade 가 링을 숨긴다 | `CardSelectFrame` |
| 라벨 | 기존 `Label`("몬스터"/"카드", 17, `#e8e1cf`) 불변 | — |

선택 표시를 `Checkmark` 한 장으로 끝내는 이유: UGUI `Toggle` 의 Fade 는 `graphic` 한 장의 알파만 바꾸므로, 켜짐 전용 장식(글로우·광택)을 별도로 추가하면 코드 없이는 꺼짐 상태에서도 보인다. 링 한 장은 코드 변경 0 으로 켜짐/꺼짐이 정확히 전환된다(§부록 결정 3).

### 4.4 셀 프리팹 변경 (`CodexCell.prefab`)

몬스터 셀(158x150)과 카드 셀(104x132)이 같은 프리팹이다.

| 요소 | 방식 | 스프라이트 · 수치 | 코드 참조 |
|---|---|---|---|
| 셀 바탕(해금) | 루트 Image 스프라이트 `Px_Panel` → `SynergyModalCellBg`(Sliced), 색 `#ffffff`. 기존 슬롯 `_normalSprite`(`Px_Panel`)도 `SynergyModalCellBg` 로 교체(코드가 해금 셀에 이 슬롯을 칠한다). 루트 Image 에 `Shadow` (0,−2) 알파 0.40 | `SynergyModalCellBg` | 기존 `_background` · `_normalSprite` |
| 셀 바탕(잠금 더미) | 기존 슬롯 `_dummySprite`(`Px_PanelDark`) → `VillageHudBtnOff`(Sliced). 코드가 해금/잠금에 따라 `_background.sprite` 를 매 `Bind` 마다 교체 | `VillageHudBtnOff` | 기존 `_dummySprite` |
| 아이콘 슬롯 | 기존 `IconSlot` 스프라이트 `Px_PanelSunk` → `Px_Solid`, 색 `#05070c` 알파 0.85. 자식 `DecoIconRing`(stretch 0, `CardSelectArtRing` Sliced, fillCenter 끔, 색 `#f7c64a`). 아이콘 84 가 슬롯 92 안쪽 4px 여백이라 링(3px)과 겹치지 않는다 | `Px_Solid`, `CardSelectArtRing` | 없음 |
| 강화 발광 오버레이 | 기존 `GlowOverlay`·`Icon`·`LevelBadge` 불변(`EnhanceLevelVisual` 소유) | — | 기존 `_glowOverlay` · `_icon` · `_levelBadge` |
| 이름 명패 | `DecoNamePlate`: `NameText` 바로 앞 형제(= `Icon` 뒤), 하단 stretch-x, 피벗 (0.5,0), 위치 (0,9), 크기 (폭 −8) x 22, Sliced, 색 `#ffffff`(스프라이트가 `#05070b` 알파 0.73 + 흰색 알파 0.13 1px 테두리를 굽고 있음). 명패 y 9~31(셀 하단 기준), `NameText` 영역 y 6~36 의 안쪽 | `WorldMapNamePlate` | 없음 |

검산: 카드 셀(132 높이)에서 명패 상단 = 132 − 31 = 101 > 슬롯 하단 100 (겹침 0). 몬스터 셀(150)에서 150 − 31 = 119 > 100. 명패 중심 y = 20, `NameText` 중심 y = 21.

해금·미조우·잠금 더미 구분(시각): 해금 = 석판 바탕 + 컬러 아이콘 + 흰 이름, 미조우 = 석판 바탕 + 실루엣 아이콘(`#0d0d12`) + 물음표 3개 문구(회색), 잠금 더미 = `VillageHudBtnOff` 평판 + 아이콘 숨김(빈 슬롯 링) + 회색 문구. 이 구분은 기존 코드(`Bind`)가 정하며 장식은 그대로 따른다.

### 4.5 상태별 시각 (구현자 확인표)

| 상태 | 판정(기존 코드) | 셀 바탕 | 슬롯 | 이름 | 비고 |
|---|---|---|---|---|---|
| 몬스터 해금 Lv0 | `Unlocked && EnhanceLevel == 0` | 석판 | 종색 색칩(아이콘 미할당 시) / 아이콘 | 흰색 | 오버레이·배지 꺼짐 |
| 몬스터 해금 Lv1~5 | `EnhanceLevel > 0` | 석판 | 종 발광 오버레이(알파 0.25~0.90) | 흰색 | `LevelBadge` "Lv N" 켜짐 |
| 몬스터 미조우 | `Unlocked == false` | 석판 | 실루엣 | 회색 물음표 3개 문구 | |
| 카드 해금 | `PickedCards` 포함 | 석판 | 카드 일러스트 | 흰색 | |
| 카드 미조우 | 미포함 | 석판 | 실루엣 | 회색 물음표 3개 문구 | |
| 잠금 더미 | `IsLockedDummy` | `VillageHudBtnOff` | 빈 슬롯(링만) | 회색 | 아이콘 숨김 |
| 탭 몬스터 켜짐 | `_monsterTab.isOn` | — | — | — | `MonsterTab/Checkmark` 금 링 표시, `CardTab/Checkmark` 숨김 |
| 탭 카드 켜짐 | `_cardTab.isOn` | — | — | — | 반대 |

---

## 5. RecordsPopup (+ RecordsStageCell)

### 5.1 현재 프리팹 구조 (실측)

`RecordsPopup.prefab` — 루트 stretch, 컴포넌트 `RecordsPopup`.

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `Dim` | stretch | 0 | 알파 0.72 |
| `ModalBody` | 중앙 | (0,0) · 480x560 | `Px_Panel` Sliced. 자식 순서: `TitlePlaque` · `Title` · `StageScrollView` · 통계 타일 5개 · `CloseButton` |
| `TitlePlaque` / `Title` | 상단 중앙 | (0,0) · 320x52 | `Px_Plaque` / "기록" 22 |
| `StatTileRuns` | 좌상단, 피벗 (0,1) | (28,−64) · 98x72 | `Px_PanelSunk`. 자식 `Label`("총 출격" 11 `#8e98ad`, 상단 stretch-x (0,−6) 폭 −8 높이 18), `Value`(하단 stretch-x (0,6) 폭 −8 높이 32, 22 자동 크기). 값 색 `#eef1f6` |
| `StatTileWins` | 좌상단 | (136,−64) · 98x72 | 라벨 "승리", 값 색 `#f7c64a` |
| `StatTileRate` | 좌상단 | (244,−64) · 98x72 | 라벨 "승률", 값 색 `#eef1f6` |
| `StatTileBest` | 좌상단 | (352,−64) · 98x72 | 라벨 "최단", 값 색 `#5ef0b4` |
| `StatTileTopCard` | 좌상단 | (28,−144) · 424x72 | 라벨 "가장 많이 픽한 카드", 값 색 `#e8e1cf` |
| `StageScrollView` | stretch | (0,−98) · 폭 −56 높이 −252 (= 424 x 308) | 화면 x 28~452, y 224~532. `_itemGap` (0,6), `_columnCount` 1, `_align` 1. origin 셀 인스턴스 크기 **440x84**(팝업 프리팹의 PrefabInstance 오버라이드) |
| `CloseButton` | 우상단 | (13,13) · 44x44 | `Px_CloseX` |

타일 y 64~136 / 144~216, 스크롤 영역 상단 224. `Viewport` 는 `RectMask2D`.

`RecordsStageCell.prefab`(기본 440x84) — 루트 Image `Px_Panel`(`_background`, 코드가 `_normalSprite`/`_lockedSprite` 로 교체, raycast 켬). 자식:

| 객체 | 앵커·피벗 | 위치 · 크기 | 현재 값 |
|---|---|---|---|
| `SelectedRing` | stretch | 0 | `Px_Ring` Sliced fillCenter 끔, 색 `#f7c64a`(코드가 매 `Bind` 칠함), 선택 행만 켜짐 |
| `PortraitSlot` | 좌중앙, 피벗 (0,0.5) | (10,0) · 68x68 | `Px_PanelSunk` Sliced |
| `Portrait` | 좌중앙, 피벗 (0,0.5) | (12,0) · 64x64 | 영웅 초상(PreserveAspect), 틴트는 코드 |
| `StageText` | 좌상단, 피벗 (0,1) | (92,−6) · 130x32 | "STAGE N" 22 `#e8e1cf` |
| `ThreatText` | 좌상단, 피벗 (0,1) | (92,−42) · 140x24 | "★★★☆☆" 16 `#f7c64a` |
| `SelectedBadge` | 좌상단, 피벗 (0,1) | (190,−10) · 76x30 | `Px_Badge_Gold` Sliced, 자식 `Label`("선택 중" 11 `#f7c64a` 가운데). 선택 행만 켜짐 |
| `BestText` | 우상단, 피벗 (1,1) | (−12,−6) · 150x32 | "최단 m:ss" 22 `#5ef0b4` 자동 크기, 우측 정렬 |
| `WinText` | 우상단, 피벗 (1,1) | (−12,−42) · 90x24 | "N승" 16 `#f7c64a` 우측 정렬 |
| `RunRateText` | 우상단, 피벗 (1,1) | (−104,−42) · 150x24 | "N판 · P%" 11 `#8e98ad` 우측 정렬 |
| `LockHintText` | 우중앙, 피벗 (1,0.5) | (−12,0) · 300x28 | "스테이지 N 클리어 필요" 16 `#ff9a9d`, 잠금 행만 켜짐 |

### 5.2 팝업 프리팹 변경 (`RecordsPopup.prefab`)

공통 틀 §2.1 을 `W=480, H=560` 으로 적용한다. 팝업 고유:

`ModalBody` 자식 최종 순서(아래→위): `DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL` · `TR` · `BL` · `BR` → `DecoPlaqueGlow` → `TitlePlaque` → `Title` → `StatTileRuns` → `StatTileWins` → `StatTileRate` → `StatTileBest` → `StatTileTopCard` → `DecoSectionLine` → `StageScrollView` → `DecoCloseGlow` → `CloseButton`. (현재 `StageScrollView` 가 `Title` 바로 뒤에 있으므로 형제 순서를 위처럼 맞춘다. 타일과 스크롤은 겹치지 않아 순서 변경이 화면에 영향이 없다.)

| 요소 | 방식 | 스프라이트 · 수치 |
|---|---|---|
| 통계 타일 바탕 5개 | 기존 5개 타일 Image 스프라이트 `Px_PanelSunk` → `Px_Solid`, Simple, 색 `#05070c` 알파 0.80(유리 inset, 선행서 빌드 모달 효과 문구 박스와 같은 톤) | `Px_Solid` |
| 통계 타일 테두리 | 각 타일 자식 `DecoTileFrame`: 자식 인덱스 0, stretch 0, Sliced, fillCenter 끔, 색 `#ffffff`(링 = 바깥 1px `#3a4764` + 안쪽 1px `#000000`) | `BattleHudTrackFrame` |
| 통계 타일 상단 페이드 | 각 타일 자식 `DecoTileAccent`: 자식 인덱스 1, 타일 로컬 좌상단 (8,3), 크기 (타일 폭 −16) x 2, 알파 0.80, **프리팹 고정 색 = 그 타일 `Value` 글자 색**: Runs `#eef1f6` · Wins `#f7c64a` · Rate `#eef1f6` · Best `#5ef0b4` · TopCard `#e8e1cf`. 타일 크기별: 98x72 → 82x2, 424x72 → 408x2. `Label` 상단 y 6 위로 3~5 | `CardSelectFadeLine` |
| 통계·목록 구분선 | `DecoSectionLine`: 좌상단 (28,219), 424x2 (§2.2). 타일 하단 216 과 목록 상단 224 사이 | `WorldMapGradientH` |
| 스크롤바 | §2.3, `StageScrollView` 자식. 위치 x 428, 높이 308 | — |

검산: 타일 `Label`(상단 y 6~24)과 액센트(y 3~5) 겹침 0. 통계 타일 오른쪽 끝 = 352 + 98 = 450, 최상위 `StatTileTopCard` 오른쪽 끝 = 28 + 424 = 452. 스크롤바 x = 424 + 4 = 428 → 모달 x 28 + 428 = 456 ~ 462(프레임 안쪽 경계 471 이내, 우측 여백 18). 목록 상단 y = 280 − (−98 + 154) → 224, 하단 224 + 308 = 532, 하단 여백 560 − 532 = 28.

### 5.3 셀 프리팹 변경 (`RecordsStageCell.prefab`)

| 요소 | 방식 | 스프라이트 · 수치 | 코드 참조 |
|---|---|---|---|
| 셀 바탕(해금) | 루트 Image 스프라이트 `Px_Panel` → `SynergyModalCellBg`(Sliced), 색 `#ffffff`. 기존 슬롯 `_normalSprite`(`Px_Panel`)도 `SynergyModalCellBg` 로 교체(코드가 해금 행에 칠함). 루트 Image 에 `Shadow` (0,−2) 알파 0.40 | `SynergyModalCellBg` | 기존 `_background` · `_normalSprite` |
| 셀 바탕(잠금) | 기존 슬롯 `_lockedSprite`(`Px_PanelDark`) → `VillageHudBtnOff`(Sliced). 코드가 잠금 행에 칠함 | `VillageHudBtnOff` | 기존 `_lockedSprite` |
| 선택 중 링 | 기존 `SelectedRing` 스프라이트 `Px_Ring` → `CardSelectFrame`(Sliced, fillCenter 끔). 색은 기존 코드(`UiDotPalette.Gold`)가 칠함. 켜짐 조건(`IsSelected`)은 코드 그대로. 링 두께 7px(바깥 `#07090e` 3px · 금 2px · 안쪽 `#07090e` 2px)라 선택 행이 단 하나뿐이어도 한눈에 구분된다 | `CardSelectFrame` | 기존 `_selectedRing` |
| 선택 중 배지 | 기존 `SelectedBadge` Image 스프라이트 `Px_Badge_Gold` → `BuildModalChipFrame`(Sliced), 색 `#b9832a`(링 금 `#b9832a`, 안쪽 검정 2px, 중앙 `#0a0e18`). 크기·위치·`Label` 불변(글자 `#f7c64a` 가 어두운 중앙 위에서 읽힌다) | `BuildModalChipFrame` | 기존 `_selectedBadge` |
| 초상 슬롯 | 기존 `PortraitSlot` 스프라이트 `Px_PanelSunk` → `Px_Solid`, 색 `#05070c` 알파 0.85. 자식 `DecoPortraitRing`: 앵커 stretch, 사방 오프셋 −1(70x70 → 슬롯 바깥 1px), `CardSelectArtRing` Sliced, fillCenter 끔, 색 `#f7c64a`. `Portrait` 64 가 슬롯 68 안쪽 2px 여백이라 링이 초상과 겹치지 않는다(링은 슬롯 바깥 −1~+2 구간) | `Px_Solid`, `CardSelectArtRing` | 없음 |
| 나머지 위젯 | `Portrait` · `StageText` · `ThreatText` · `BestText` · `WinText` · `RunRateText` · `LockHintText` 불변 | — | 기존 |

### 5.4 셀 폭 보정 (이 문서에서 **유일한 치수 변경**, 사용자 선택 필요)

현재 origin 셀 인스턴스가 440 폭인데 `StageScrollView` 뷰포트는 424 폭이라(`ModalBody` 480 − 56), `RectMask2D` 가 셀 좌우 8px 씩을 잘라내고 있다(셀 중심 정렬 `_align` 1). 새 프레임·링이 좌우에서 잘리면 폴리시가 깨지므로 **셀 폭을 뷰포트와 맞춘다.**

| 안 | 내용 | trade-off |
|---|---|---|
| **A (채택, 권장)** | 셀 프리팹의 루트 폭 440 → 424, 팝업 프리팹 안 origin PrefabInstance 오버라이드 폭 440 → 424. 높이 84 불변 | 셀 안 우측 정렬 텍스트(오프셋 −12 · −104)가 시각적으로 이전보다 오른쪽 가장자리에서 8px 안쪽으로 보인다(이전에는 잘려 4px). 좌측 위젯도 8px 덜 잘린다. 변경 2곳(1 숫자) |
| B | 440 유지, 프레임·링 좌우 8px 클립을 감수 | 좌우 테두리가 없고 위아래만 보이는 어색한 셀 |
| C | 뷰포트(`StageScrollView`)를 440 폭으로 넓힘 | `ModalBody` 480 안에서 좌우 여백 20 으로 줄어 팝업 전체 정렬과 어긋남(통계 타일 x 28 과 비정렬) |

A 로 정한다. 검증: 셀 폭 424 = 뷰포트 424, 위젯 오프셋은 전부 좌·우 앵커 기준이라 재배치가 필요 없다. 다른 두 팝업(상점 664 = 664, 도감 656·654 ≤ 664)은 이 문제가 없다.

### 5.5 상태별 시각 (구현자 확인표)

| 상태 | 판정(기존 코드) | 바탕 | 선택 링 | 선택 배지 | 초상 | 우측 텍스트 |
|---|---|---|---|---|---|---|
| 해금 · 비선택 | `IsLocked == false && IsSelected == false` | 석판 | 숨김 | 숨김 | 정상 색 | 최단·승·판/승률 표시 |
| 해금 · 선택 중 | `IsSelected` | 석판 | 금 링 켜짐 | 켜짐 | 정상 색 | 표시 |
| 잠금 | `IsLocked` | `VillageHudBtnOff` | 숨김 | 숨김 | 어둡게(`PortraitTint`) | 잠금 안내(`LockHintText`) 표시, 전적 텍스트 숨김 |
| 초상 없음 | `Portrait == null` | 위와 같음 | 위와 같음 | 위와 같음 | `Portrait` 숨김, 슬롯 링만 | 위와 같음 |

---

## 6. 애니메이션

**`UILoopMotion` 사용 0건.** 세 팝업은 평상시 정지 화면이며 선행서 `build-modal-visual-polish.md` §4 와 같은 판단이다. 탭 전환·선택 링·상태 링은 정적 활성 토글과 색 칠하기뿐이다. 따라서 정적 폴백 기준·성능 측정 항목이 없다. 풀링 셀에 `UILoopMotion` 을 달면 `OnEnable` 기준 포즈 저장과 풀 재사용이 충돌할 수 있어 도입하지 않는다.

---

## 7. SerializeField · 코드 변경

### 7.1 유지 (이름·타입·의미 불변)

- `ShopPopup`: 기존 전부(`_dimButton` · `_closeButton` · `_soulText` · `_bonusSummaryText` · `_scrollView` · 종족 아이콘 6종). `CodexPopup`: 기존 전부(`_monsterTab` · `_cardTab` · 두 스크롤뷰 · `_collectedText` · 종족 아이콘 6종). `RecordsPopup`: 기존 전부(`_statRunsText` … `_statTopCardText` · `_scrollView`).
- `ShopItemCell` 기존 전부(`_nameText` … `_buyGoldSprite`). 세 버튼 스프라이트 슬롯은 현재 `Px_Btn*` 를 **그대로 가리킨다**.
- `CodexCell` 기존 전부. `_normalSprite`·`_dummySprite` 슬롯은 **가리키는 스프라이트만 교체**(필드 유지).
- `RecordsStageCell` 기존 전부. `_normalSprite`·`_lockedSprite` 슬롯은 **가리키는 스프라이트만 교체**.

### 7.2 신규 `[SerializeField] private` (`ShopItemCell` 2개뿐)

| 클래스 | 필드 | 타입 | 용도 |
|---|---|---|---|
| `ShopItemCell` | `_itemDecor` | `GameObject` | `DecoItemGroup`. 항목 행에서만 켠다 |
| `ShopItemCell` | `_stateTinted` | `Image[]` | 상태색을 칠할 장식 2개(`DecoStateGlow` · `DecoStateStrip`, 이 순서 고정) |

### 7.3 신규 코드 동작 (View 만, 로직 없음) — `ShopItemCell.cs` 한 파일

- `SetItemWidgetsActive(bool on)` 안에서: `_itemDecor` 가 null 이 아니면 `_itemDecor.SetActive(on)`. 헤더 행 `Bind` 에서 `on == false` 가 되어 항목 바탕이 꺼지고, 항목 행 `Bind` 에서 켜진다. 풀 재사용 시 이전 상태가 남지 않는다(매 `Bind` 가 명시 on/off — 기존 클래스 주석의 교훈 그대로, `OnEnable` 리셋 금지).
- `ApplyBuyState(ShopItemCellData data)` 안에서, 기존 `_stateRing.color` 를 칠하는 위치와 같은 곳: 상태색 `data.IsMax ? UiDotPalette.Gold : UiDotPalette.Soul` 의 rgb 를 `_stateTinted` 의 각 원소(null 원소는 건너뜀)에 칠하되 **알파는 그 Image 의 현재 알파를 보존**(프리팹 값: 발광 0.40, 띠 1.0). `_stateTinted` 가 null 이면 건너뛴다.
- 신규 필드가 하나도 비어 있어도 예외 없이 동작해야 한다(기존 테스트가 리플렉션으로 일부 위젯만 채운 빈 컴포넌트를 쓴다).
- 그 외 `.cs` 변경 0건: `ShopPopup` · `ShopItemPoolingScrollView` · `CodexPopup` · `CodexCell` · `CodexPoolingScrollView` · `RecordsPopup` · `RecordsStageCell` · `RecordsStagePoolingScrollView` · `UiDotPalette` · `EnhanceLevelVisual` 수정 금지.

---

## 8. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/**` | **변경·신규 없음.** 삭제·리네임 금지 |
| `Assets/_Lair/Art/UI/ShopPopup.prefab` | §2·§3.2. `ScrollRect` 에 `Scrollbar` 연결 |
| `Assets/_Lair/Art/UI/ShopItemCell.prefab` | §3.3. 신규 필드 2종 와이어링 |
| `Assets/_Lair/Art/UI/CodexPopup.prefab` | §2·§4.2·§4.3. `ScrollRect` 2개에 `Scrollbar` 연결 |
| `Assets/_Lair/Art/UI/CodexCell.prefab` | §4.4 (`_normalSprite`·`_dummySprite` 슬롯 스프라이트 교체 포함) |
| `Assets/_Lair/Art/UI/RecordsPopup.prefab` | §2·§5.2·§5.4. `ScrollRect` 에 `Scrollbar` 연결, origin 인스턴스 폭 오버라이드 424 |
| `Assets/_Lair/Art/UI/RecordsStageCell.prefab` | §5.3·§5.4 (루트 폭 424, `_normalSprite`·`_lockedSprite` 슬롯 교체) |
| `Assets/_Lair/Scripts/UI/Village/ShopItemCell.cs` | §7.2·7.3 |

작업 순서: (1) UnityMCP 로 `ShopItemCell` · `CodexCell` · `RecordsStageCell` 프리팹을 먼저 편집(팝업 안 origin 인스턴스가 자동으로 따라옴) → (2) 세 팝업 프리팹 편집(공통 틀 → 팝업 고유) → (3) `ShopItemCell.cs` 필드·동작 추가 후 프리팹 와이어링 → (4) EditMode 테스트 실행(`Lair/Tests/Run EditMode Tests`).

- 모든 신규 장식은 프리팹에 정적으로 배치한다. 코드에서 `Instantiate` · `new GameObject` 로 만들지 않는다(Rule 03 §3~4).
- **구현 시 현황 확인 항목**: ① 세 `CHPoolingScrollView` 가 `Content` 크기를 전체 목록 길이로 맞춰 `Scrollbar` 핸들 비율이 항목 수에 따라 변하는지(선행서 시너지 모달이 같은 구조로 이미 동작하므로 기대값 확인). 맞지 않으면 §10 의 스크롤바 폴백 행을 적용. ② `VillageHudCta`(border 6,8,6,6)가 44x44 닫기 버튼에서 모서리가 깨지지 않는지. ③ 도감 두 `ToggleGroup` 탭에서 `CardSelectFrame` 7px 링이 40px 높이에서 라벨 17pt 를 가리지 않는지(안쪽 여유 40 − 14 = 26px). ④ `StateRing` 자식(`DecoStateStrip` · `DecoStateGlow`)이 `StateRing` 이 꺼질 때 같이 꺼지는지.

---

## 9. 테스트 관점

### 9.1 영향받는 기존 테스트

| 테스트 | 영향 |
|---|---|
| `ShopPopupCellDataTests` · `ShopPopupSummaryText*` · `ShopSection*` · `ShopPopupTabFilter*`(데이터 가공 EditMode) | 가공 함수 불변 → 무수정 통과가 기준 |
| `CodexCellIconColorTests` · `CodexMonsterEnhance*` · `CodexHeroIconTests` | `CodexCell`·`EnhanceLevelVisual` 불변 → 무수정 통과 |
| `RecordsStageRowTests` · `RecordsVisibleStageCountTests` · `VillageCarouselAndRecordsLabelTests` | 데이터·라벨 불변 → 무수정 통과 |
| 이 세 팝업의 프리팹 연결 검사 | 현재 **없음** → 신규 작성(§9.2) |

### 9.2 신규 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| Missing Script 0건 | 6개 프리팹(`ShopPopup` · `ShopItemCell` · `CodexPopup` · `CodexCell` · `RecordsPopup` · `RecordsStageCell`)의 모든 자식 GameObject 에 대해 `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` = 0 (이번 교훈의 직접 회귀 가드) |
| 기존 연결 보존 | §7.1 의 모든 기존 필드가 비어 있지 않음. `ShopPopup`·`CodexPopup`(두 개)·`RecordsPopup` 의 `_scrollView` 들의 `_origin` 이 비어 있지 않고 **셀 프리팹 인스턴스**를 가리킴. 도감 두 `_origin` 이 서로 다른 인스턴스. `_buySoulSprite`/`_buyOffSprite`/`_buyGoldSprite` 가 각각 `Px_BtnSoul`/`Px_BtnOff`/`Px_BtnGold` |
| 신규 연결 | `ShopItemCell._itemDecor` 이름 `DecoItemGroup`, `_stateTinted` 길이 2 이고 이름이 순서대로 `DecoStateGlow` · `DecoStateStrip`, 둘 다 `StateRing` 의 자식 |
| 스프라이트 연결 | 닫기 버튼 Image = `VillageHudCta`(3곳). `ModalBody` Image = `Px_Solid`. 도감 `_normalSprite`/`_dummySprite` = `SynergyModalCellBg`/`VillageHudBtnOff`, 기록 `_normalSprite`/`_lockedSprite` 동일. 상점 `LevelPip0~4` = `VillageHudGem`, `StateRing` = `CardSelectArtRing`. 도감 탭 두 개의 Background = `VillageHudBtn`, `Checkmark` = `CardSelectFrame` · `fillCenter == false` · 색 rgb `#f7c64a`. 기록 `SelectedRing` = `CardSelectFrame`, `SelectedBadge` Image = `BuildModalChipFrame` |
| 장식 레이캐스트 | 6개 프리팹 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false`. `Dim` · `CloseButton` · `Scrollbar` · `Handle` · 도감 탭 `Toggle` · `BuyButton` 은 켜져 있음 |
| 스크롤바 | 4개 `ScrollRect`(상점 1 · 도감 2 · 기록 1)의 `verticalScrollbar` 가 비어 있지 않고 Visibility = Auto Hide |
| 순서 | 각 팝업 `ModalBody`: `DecoModalGrad` 가 `TitlePlaque` 보다 앞, `DecoCloseGlow` 가 `CloseButton` 바로 앞, `CloseButton` 이 마지막. 상점 `SoulPill` 이 `DecoSoulGlow` 뒤, 상점 셀 `DecoItemGroup` 이 셀 루트 형제 인덱스 0 |
| 치수 불변 | 상점 `ModalBody` 720x480 · `ScrollView` 664x336 · 셀 664x108 · `_itemGap` (0,8) / 도감 `ModalBody` 720x480 · 몬스터 origin 158x150 · 카드 origin 104x132 / 기록 `ModalBody` 480x560 · 통계 타일 98x72 · 424x72 · `StageScrollView` 424x308. 기록 셀 폭은 **424**(§5.4 로 의도된 변경, 높이 84). `Dim` 알파 0.72(허용오차 0.005) |
| 상점 셀 상태 전환 | 리플렉션으로 필드를 채운 `ShopItemCell` 에 `Bind(CanBuy 데이터)` → `_stateTinted` rgb 가 `UiDotPalette.Soul`, 알파가 프리팹 값(0.40·1.0) 유지, `StateRing.activeSelf` 참. `Bind(IsMax 데이터)` → rgb 가 `UiDotPalette.Gold`, 링 켜짐. `Bind(소울 부족 데이터)` → 링 꺼짐. `Bind(헤더 데이터)` → `_itemDecor.activeSelf` 거짓, 이어 `Bind(항목 데이터)` → 참(풀 재사용 잔상 방지) |
| 빈 필드 안전 | `_itemDecor`·`_stateTinted` 를 채우지 않은 `ShopItemCell` 에서 `Bind` 가 예외 없이 동작 |
| 도감 탭 상태 | 프리팹 로드 후 `MonsterTab.isOn == true` · `CardTab.isOn == false` 초기 상태 불변. 한쪽 `isOn` 전환 시 `Toggle.graphic`(= `Checkmark`)가 해당 탭에서만 켜진다 |
| 기록 셀 상태 | `RecordsStageCell.Bind(잠금)` → `_background.sprite` = `VillageHudBtnOff`, 선택 링·배지 숨김. `Bind(해금·선택)` → `SynergyModalCellBg`, 링·배지 켜짐. `Bind(해금·비선택)` → 링·배지 숨김. `Portrait == null` → `Portrait` 숨김 |
| 도감 셀 상태 | `CodexCell.Bind(잠금 더미)` → `_background.sprite` = `VillageHudBtnOff`, `_icon` 숨김. `Bind(해금)` → `SynergyModalCellBg` (스프라이트 슬롯이 채워진 경우) |
| 신규 스프라이트 0 | `Assets/_Lair/Art/Sprites` 에 이 작업으로 추가된 PNG 가 없음(§2.5 에 열거한 선행 스프라이트가 모두 존재하고 Texture Type = Sprite) |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과 |

### 9.3 PlayMode 불요

신규 코드가 색 칠하기·활성 토글뿐이라 EditMode 로 덮인다.

### 9.4 수동 확인 (사용자 육안, 마을 씬에서 각 팝업을 열어)

- 상점: 소울 0 / 충분한 소울에서 구매 가능(녹색 링·띠·발광) · 소울 부족(링 없음) · 만렙(금 링) 3상태, 섹션 헤더 2종(스탯 강화·몬스터 강화) 배너·금 라인·마름모, 종족 강화 셀의 아이콘 슬롯 링과 발광 프레임 겹침, 구매 직후 스크롤 위치 복원과 상태 링 갱신, 스크롤바가 목록이 넘칠 때만 보이는지.
- 도감: 탭 전환 시 금 링이 선택 탭에만 있는지, 몬스터 Lv0/Lv5·미조우·잠금 더미 셀 구분, 카드 28장 스크롤과 스크롤바, 이름 명패 위 글자 가독성, 수집 칩 안의 텍스트 정렬.
- 기록: 스테이지 5개·잠금 행·선택 중 행(금 링+배지), 챕터 확장 시 25행 스크롤바, 셀 좌우 테두리가 잘리지 않는지, 통계 타일 5칸 상단 페이드 색이 값 색과 일치하는지.
- 세 팝업 공통: 닫기 X·Dim 클릭으로 닫히는지, 닫기 X 눌림 시 어두워지는지, 장식이 클릭을 막지 않는지, 제목판이 모달 상단 경계에 걸쳐 보이는지.

---

## 10. 포기하는 연출 (시안·선행서 대비 의도적 차이)

| 항목 | 선행서/시안 | 이 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·글로우 | 제목·이름 등에 그림자 | 생략 | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 타 화면에 영향(선행서와 같은 판단) |
| 도감 탭 켜짐 글로우·광택 | 침입 지도 카드의 선택 글로우 | 링 한 장만 | `Toggle` Fade 가 `graphic` 한 장의 알파만 바꿔, 별도 장식을 켜짐에 연동하려면 코드가 필요(§4.3) |
| 도감 `LevelBadge` 명패 | 칩 프레임 배경 | 없음 | 배지는 풀 셀에서 `EnhanceLevelVisual` 이 켜고 끈다. 형제 명패는 항상 보이고 자식 명패는 글자를 덮어 둘 다 불가(코드 변경 필요) |
| 상점 구매 버튼 면 교체 | 카드 선택의 `CardSelectPickBtnSoul/Gold` | 기존 `Px_BtnSoul/Off/Gold` 유지 + 그림자·광택 | 새 소울 버튼 면이 밝은 `#5ef0b4` 라 기존 코드의 흰색 라벨이 읽히지 않는다. 코드의 라벨색을 바꾸면 "동작 불변" 원칙 위반(§부록 결정 2) |
| 상점 상태 발광 바깥 번짐 | 선행서 시너지 셀의 띠 발광 | 셀 내부(44x108)로 한정 | 뷰포트가 셀 좌우를 잘라 번짐이 한쪽만 끊김 |
| 호버·눌림 이동 | 시안의 −2/+2px | 기존 `Selectable` 눌림 색만 | 모바일 중심 터치 UI, 위치 이동은 코드 필요 |
| 기록 선택 행 발광 | 선택 카드 글로우 | `CardSelectFrame` 링 + 배지 | 풀 셀 안에서 상태색 발광은 `_stateTinted` 류 코드가 필요하고, 선택 행은 7px 링과 배지만으로 구분됨 |
| 스크롤바 비활성 | 안 넘칠 때 트랙 알파 0.25 로 남김 | Auto Hide 로 사라짐 | UGUI 표준 동작, 코드 없음 |
| 스크롤바 폴백 | — | §8 확인 ①이 실패하면 `Scrollbar` 연결만 해제하고 트랙·핸들 장식을 숨김 | 핸들 비율이 콘텐츠와 맞지 않으면 혼동을 준다 |
| 닫기 호버 | 밝기 +30% | 기존 눌림 색 0.75 | 선행서와 동일 |
| 닫기 버튼 Normal 색 | 프리팹 0.9 | 1.0(흰색)으로 정규화 | `VillageHudCta` 가 색을 굽고 있어 0.9 곱이 붉은색을 탁하게 함(마을 HUD 선행서 §2.3 과 동일 처리) |
| 모달 바깥 검은 번짐 | 시안 `0 0 34px` | 없음(`Shadow` (0,−8) 만) | Dim 알파 0.72 가 이미 어둡게 함 |

---

## 11. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트 0종. 모든 스프라이트는 프리팹 직접 참조(선행서 에셋 재사용). 삭제 대상 없음. 프리팹 키(`ShopPopup` · `ShopItemCell` · `CodexPopup` · `CodexCell` · `RecordsPopup` · `RecordsStageCell`)와 Addressable 등록은 그대로.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록** (이 한 파일 외 `.cs` 변경 0건):
  - `ShopItemCell`: 신규 필드 `_itemDecor`(`GameObject`) · `_stateTinted`(`Image[]`), `SetItemWidgetsActive` 에 `_itemDecor.SetActive(on)`, `ApplyBuyState` 에 상태색 칠하기 — §7.2·7.3. 신규 코드는 전부 null 가드, 주석 `//#`, 가드 절 중괄호 없이 개행, 가드가 아닌 `if` 는 한 줄이어도 중괄호, `var`·`!` 금지(Rule 02).
- **프리팹**: §8 표. 장식 GameObject 이름(`Deco*` · `Scrollbar` · `SlidingArea` · `Handle`)과 신규 필드명은 테스트 기준이므로 글자 그대로 쓴다. **UnityMCP 로 편집**하고, 새로 만들어 덮어쓰지 않는다. 장식 좌표·크기는 §2~§5 표 그대로(앵커 체계 환산은 구현자가 하되 코드가 참조하는 기존 위젯은 움직이지 않는다).
- **MonoBehaviour**: 신규 클래스 없음. 필요해지면 파일명 = 클래스명, 파일당 MonoBehaviour 1개.
- **일회용 도구**: 프리팹 편집용 에디터 스크립트를 쓰면 완료 직후 삭제(Rule 04 §3).

---

## 부록. 핵심 결정과 검토한 대안

| # | 결정 | 선택 | 대안 | trade-off |
|---|---|---|---|---|
| 1 | 상점 항목/헤더 행의 바탕 처리 | **A. 루트를 투명 raycast 면으로 두고 `DecoItemGroup` 을 `_itemDecor` 로 토글 (권장)** | B. 루트 Image 에 항목 바탕을 그대로 두고 `HeaderBg` 를 불투명하게 / C. 헤더 행을 별도 셀 프리팹으로 분리 | A: 코드 1줄, 풀 재사용 안전. B: 코드 0 이나 헤더 배너를 오른쪽까지 불투명하게 칠해야 해 "왼쪽→사라지는 배너"를 못 함. C: 셀 타입이 둘이 되어 `CHPoolingScrollView<TItem,TData>` 구조(단일 셀 타입)와 충돌 |
| 2 | 상점 구매 버튼 면 | **A. 기존 `Px_Btn*` 3종 유지 + 그림자·광택 (권장)** | B. `CardSelectPickBtnSoul/Gold` + `VillageHudBtnOff` 로 교체하고 라벨색 코드 수정 / C. 교체만 하고 흰색 라벨 유지(카드 선택 선례) | A: 코드 0, 라벨 대비 보장, 톤이 픽셀풍으로 남음. B: 톤 통일이나 `Bind` 의 라벨색(CanBuy 흰색 → 짙은색)을 바꿔야 해 "동작 불변" 위반. C: 흰색 라벨이 밝은 `#5ef0b4` 위에서 명암비 1.4 대로 안 읽힘 |
| 3 | 도감 탭 선택 표현 | **A. `Checkmark` 에 `CardSelectFrame` 금 링 (권장)** | B. `Checkmark` 에 `CardSelectPickBtnGold` 밝은 금 버튼 / C. 켜짐 전용 글로우·광택을 추가하고 `onValueChanged` 로 코드 토글 | A: 코드 0, 라벨 `#e8e1cf` 가 어두운 몸체 위에서 계속 읽힘. B: 선택이 더 강하지만 라벨색이 금색 면 위에서 대비 부족. C: 가장 화려하나 코드와 필드 추가, 테스트 증가 |
| 4 | 기록 셀 폭 | **A. 424 로 보정 (권장)** | B. 440 유지·클립 감수 / C. 뷰포트 확대 | §5.4 표. **사용자 선택 필요**: 이 한 건만 "좌표 불변"에서 벗어난다 |
| 5 | 신규 스프라이트 | **0종 (권장)** | 상점·도감·기록 전용 프레임·링·타일 신규 생성 | 선행서 자산을 재사용해 파일·검증 비용 0, 6개 화면 톤 일치. 대신 셀 바탕 한 종(`SynergyModalCellBg`)을 시너지 모달과 공유 |
| 6 | 상태색 장식 방식 | **A. `_stateTinted` 배열을 `Bind` 가 칠함 (권장)** | B. 상태 링 한 장만(코드 0) / C. 상태 3종 프리팹 색 고정 | A: 코드 10줄 이내, 선행서 빌드 모달 `_kindTinted` 와 같은 패턴. B: 시안의 띠·발광(구매 가능 구분 핵심 신호)을 잃음. C: 구매 가능/만렙이 런타임에 바뀌므로 불가능 |
