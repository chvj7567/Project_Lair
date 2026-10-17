# 시너지 모달 팝업(SynergyModalPopup) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/synergy-modal-redesign.html` 의 마감(금색 이중 테두리 프레임·모서리 장식·마름모 명패·붉은 입체 닫기 버튼·축 색 발광 띠·원형 후광·Tier 마름모 보석 3개·유리 칩 배지·유리 inset 효과 문구·금색 슬림 스크롤바)을 Unity 에 옮긴다. 레이아웃·좌표·문구·동작·ViewModel 은 바꾸지 않고 **장식 레이어와 스프라이트 스왑만** 얹는다.
- **검증 가설**: 같은 레이아웃·같은 문구 상태에서 축 색 발광과 Tier 보석만 추가해도, 플레이어가 "어느 축이 몇 단계까지 켜졌는가"를 배지 숫자("n/3")를 읽지 않고 한눈에 구분하는가. (정량 메트릭 없음 — 사용자 육안 확인용 UI 개선. 선행 사례 `battle-hud-visual-polish.md` · `village-hud-visual-polish.md` · `worldmap-popup-visual-polish.md` 와 동일한 성격.)
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 규칙·메타/서버 기능 없음.
- **핵심 메커니즘**: (1) 기존 스프라이트 9종을 재사용하고 신규 스프라이트는 3종(`SynergyModalCellBg` · `SynergyModalHalo` · `SynergyModalBadge`)만 절차 생성한다(§3). (2) 축 색에 따라 달라지는 장식(띠 발광·후광·배지·보석)은 `SynergyModalCell.Bind` 가 이미 받는 `AxisColor` 로 틴트한다 — 필드 3개와 데이터 필드 1개만 추가한다(§5). (3) 애니메이션은 쓰지 않는다(§4).

이 문서의 수치가 목업과 다르면 이 문서가 우선한다. 달라지는 지점은 §1.2 와 §9 에 모았다.

---

## 1. 불변 사항

### 1.1 불변 목록

- 레이아웃·좌표·크기·글자 크기·글자 색·문구·동작 전부 불변. 특히 아래는 변경 0건이다.
  - 모달 `ModalBody` 460x480 화면 중앙(좌상단 (410,120)), `TitlePlaque`/`Title` 320x52 상단 중앙, `CloseButton` 44x44 pivot(1,1) +13/+13 → 화면 (839,107), `ScrollView`·`EmptyText` 중심 (0,-22) 크기 404x380 → 화면 (438,192), `_itemGap` (0,10).
  - 셀 392x136 안의 기존 위젯 좌표: `AxisStrip` x4 폭 8, `IconSlot` 52x52 (20,중앙), `Icon` 36x36, `Label` (84,-8) 200x30 f22, `DescText` (84,-42) 294x44 f16, `NextText` (84,-92) 294x36 f11, `TierBadgeBg`·`TierBadge` 우상단 (-14,-8) 64x28 f11.
  - dim(`Dim` Image): 색 `#04060a` 알파 0.8, 클릭 시 닫힘. 변경 없음.
  - 닫힘 동작(dim 클릭·X 클릭), 빈 상태 라벨 토글, 스크롤, 재오픈 시 누적 픽 반영.
- `BattleViewModel` · `BuildSynergyPanel` · `SynergyModalCardPoolingScrollView` 변경 0건. `Strings_Ko` 신규 문구 0건 · 신규 JSON 0건 · 신규 SO 0건 · 신규 Enum 0건 · 신규 Addressable 키 0건.
- **문구·수치는 전부 실제 데이터 기준이다.** 목업의 DPS 2단계 "사신·저주술사 공속 +25%" 는 `Strings_Ko` id 204 = "사신·저주술사 공속 +{0}%" 에 `DpsSynergyTier2` 가 파생한 25 가 들어간 결과(쿨다운 ×0.8 → (1/0.8−1)×100 = 25)이고, 다른 축 문구도 id 200~211 과 제목 id·"아직 발동한 시너지가 없습니다" 의 기존 값 그대로다. 이 작업이 만드는 신규 문구는 0건이다.
- 기존 `[SerializeField]` 필드는 이름·타입·연결 대상을 전부 유지한다. **기존 위젯의 재부모와 위치·크기 이동은 하지 않는다.** 신규 장식을 기존 위젯 사이에 끼우기 위한 형제 순서(sibling index) 삽입만 허용한다.
- 신규 장식 Image 는 전부 `raycastTarget = false`. 클릭 대상은 기존 `Dim`·`CloseButton` 과 스크롤뿐이다. `ModalBody` Image 의 `raycastTarget` 은 true 를 유지한다(패널 클릭이 뒤의 dim 으로 새어 닫히는 것을 막는 현재 동작).

### 1.2 목업과 실제 prefab 의 불일치 정정 (이 문서의 값이 우선)

| 항목 | 목업 | 실제 prefab / 이 문서의 결정 | 근거 |
|---|---|---|---|
| 제목 명패 x | left 490 → 중심이 패널 중심보다 +10 | 패널 중심 정렬(목업 좌측 480 상당), 좌표 불변 | `TitlePlaque` anchor (0.5,1) pos (0,0). 목업 점검표도 "상단 중앙" |
| dim | `#000` 알파 0.6 | prefab 값 `#04060a` 알파 0.8 유지 | dim 은 "기존 그대로"(목업 요약 2번). 실제 화면은 목업보다 어둡다 |
| 셀 간격 | 8 | prefab `_itemGap.y` = 10 유지 | 목업 점검표 "현재 prefab 값 유지" |
| 목업 CSS 좌표 | 셀·패널 border 3px 의 안쪽 기준 | 이 문서의 모든 좌표는 **border 포함 박스의 좌상단 기준**(CSS 값 +3) | 목업은 `position:absolute` 가 padding box 기준 |
| 룬 라인 우측 | 좌 30 / 우 24 여백(비대칭) | 좌우 33 대칭 | 목업 우측은 반올림 오차로 보이는 비대칭 |
| 보석 간격 | 19 | 18 | 셋째 보석이 inset 왼쪽 77 과 겹치는 것을 막는다(§2.3 검산) |
| 라벨 "(5장)" 소형·축 색 | `<i>` 로 15px 축 색 | 22px 단색(기존 `Label` 한 줄 텍스트 유지) | 라벨 문자열은 `BuildRows` 가 한 줄로 만든다. 분리하면 VM/테스트 변경 |

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image(스프라이트)** = 정적 장식, 프리팹에 박음 / **Shadow·Outline** = UGUI 이펙트 / **코드 틴트** = `SynergyModalCell.Bind` 가 축 색을 입힘(§5). 좌표는 별도 표기가 없으면 부모 박스 좌상단 기준 (x 오른쪽, y 아래) px 이다. 스프라이트 이름은 §3 의 파일명이며 "UISoftGlow" 는 `Assets/_Lair/Art/Sprites/UISoftGlow.png`, "Px_Solid" 는 `Assets/_Lair/Art/Sprites/UiDot/Px_Solid.png` 이다. 그라데이션 2장 조합(바닥 단색 + `WorldMapGradientV`/`H` 틴트)의 정의는 `worldmap-popup-visual-polish.md` §2 를 그대로 따른다.

### 2.1 팝업 프레임·제목·닫기 (`SynergyModalPopup.prefab`)

`ModalBody` 자식의 최종 형제 순서(뒤 → 앞): BodyGradient, BodyGloss, BodyFrame, CornerTL, CornerTR, CornerBL, CornerBR, RuneL, RuneR, TitleGlow, **TitlePlaque**(기존), **Title**(기존), **EmptyText**(기존), **ScrollView**(기존), CloseGlow, **CloseButton**(기존).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 패널 바닥 | 기존 `ModalBody` Image 의 스프라이트를 `Px_Panel` → `Px_Solid` 로 교체(Simple), 색 `#0f1420`. `raycastTarget` true 유지 | `Px_Solid` | 없음 |
| 바닥 그라데이션 | `BodyGradient`: `ModalBody` 전체 stretch, `WorldMapGradientV` 색 `#1a2132`(위 `#1a2132` → 아래 `#0f1420`) | `WorldMapGradientV` | 없음 |
| 상단 광택 | `BodyGloss`: 좌상단 (13,13) 434x44, `WorldMapGradientV` 흰색 알파 0.08 | `WorldMapGradientV` | 없음 |
| 이중 테두리 | `BodyFrame`: 전체 stretch, Sliced, fillCenter 끔 | `WorldMapFrame` | 없음 |
| 패널 아래 그림자 | `ModalBody` Image 에 `Shadow`: effectDistance (0,−10), 색 `#000000` 알파 0.44 | — | 없음 |
| 모서리 장식 4개 | `CornerTL`·`TR`·`BL`·`BR`, 각 24x24, 좌상단 (8,8) / (428,8) / (8,448) / (428,448). TR scale (−1,1), BL (1,−1), BR (−1,−1). 닫기 X 가 위에서 TR 을 덮는다(목업과 동일) | `WorldMapCornerGold` | 없음 |
| 룬 라인 2개 | `RuneL`: 좌상단 (33,65) 130x3 색 `#f7c64a`. `RuneR`: 좌상단 (297,65) 130x3 scale (−1,1) 색 `#f7c64a` | `WorldMapGradientH` | 없음 |
| 제목 글로우 | `TitleGlow`: 중심 = 패널 상단 중앙선 위 `TitlePlaque` 중심, 380x96, UISoftGlow 색 `#f7c64a` 알파 0.20 | `UISoftGlow` | 없음 |
| 명패 바닥 | 기존 `TitlePlaque` Image 의 스프라이트를 `Px_Plaque` → `Px_Solid` 로 교체(Simple), 색 `#10151f`. 아래 그림자: `Shadow` (0,−6) 색 `#000000` 알파 0.44 | `Px_Solid` | 없음 |
| 명패 그라데이션 | `PlaqueGradient`: `TitlePlaque` 의 자식, 전체 stretch, `WorldMapGradientV` 색 `#2c3858` | `WorldMapGradientV` | 없음 |
| 명패 테두리 | `PlaqueFrame`: `TitlePlaque` 의 자식, 전체 stretch, Sliced, fillCenter 끔(`PlaqueGradient` 위) | `WorldMapFrame` | 없음 |
| 명패 마름모 2개 | `PlaqueDiamondL`·`R`: `TitlePlaque` 의 자식, 18x18, 명패 중심 기준 x = −166 / +166, y = 0, 색 `#f7c64a`. 스프라이트 자체가 마름모(대각 28/36)라 회전하지 않는다 | `VillageHudGem` | 없음 |
| 제목 글자 | 기존 `Title` 불변(색 `#e8e1cf`, f22). 글자 그림자는 생략(§9) | — | 기존 |
| 닫기 버튼 면 | 기존 `CloseButton` Image 의 스프라이트를 `Px_CloseX` → `VillageHudCta` 로 교체(Sliced), 색 흰색. `Selectable` 은 ColorTint 유지(누르면 ×0.85) | `VillageHudCta` | 기존 `_closeButton` |
| 닫기 × 글자 | 기존 자식 `X`(CHText, 텍스트 "×")를 **활성화**(현재 prefab 에서 비활성). f16 → f24, 색 `#e8e1cf`. `×`(U+00D7)는 `Strings_Ko` 의 "×{0}" 문구에 이미 쓰여 글리프가 보장된다 | — | 없음 |
| 닫기 글로우 | `CloseGlow`: `CloseButton` 바로 뒤 형제, 중심 = 닫기 버튼 중심(패널 우상단에서 안쪽으로 9, 아래로 9), 76x76, UISoftGlow 색 `#e5484d` 알파 0.40 | `UISoftGlow` | 없음 |

검산:
- 모서리 TR: 428 + 24 = 452 ≤ 460, BL/BR: 448 + 24 = 472 ≤ 480.
- 룬: 좌 33 + 130 = 163, 우 297 + 130 = 427 = 460 − 33. 룬 하단 65 + 3 = 68 < 스크롤 영역 상단 72(= 화면 192 − 120).
- 명패 마름모 바깥 끝: 166 + 9 = 175 ≤ 패널 반폭 230.
- 닫기 버튼 중심: 우상단 pivot(1,1) +13/+13, 크기 44 → 중심은 패널 우측선에서 22 − 13 = 9 안쪽, 상단선에서 22 − 13 = 9 아래.

### 2.2 빈 상태·스크롤바

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 빈 상태 마름모 | `EmptyDiamond`: **`EmptyText` 의 자식**(→ 라벨 토글과 함께 켜고 꺼지므로 코드 0건). 18x18, `EmptyText` 중심 기준 (0, +73) 위쪽, 색 `#3a4764`. 위치 검산: 목업 마름모 중심 패널 상단 기준 y 189, `EmptyText` 중심 y = 240 + 22 = 262, 262 − 189 = 73 | `VillageHudGem` | 기존 `_emptyText` |
| 스크롤 트랙 | 기존 `VerticalScrollbar` Image(현재 알파 0 투명): 스프라이트 `Px_Solid`, 색 `#05070c` 알파 1. `Outline` effectDistance (1,1) 색 `#3a4764`. 폭 8·위치(−2)는 불변 | `Px_Solid` | 기존 |
| 스크롤 핸들 | 기존 `Handle` Image 색을 `#48546e` → `#e0a21c` 로 변경(스프라이트 `Px_Solid` 유지). 자식 `HandleShine`(`WorldMapGradientH` 흰색 알파 0.45, 왼쪽이 밝음)·`HandleShade`(`WorldMapGradientH` scale (−1,1) 색 `#000000` 알파 0.25, 오른쪽이 어두움), 둘 다 전체 stretch | `Px_Solid`, `WorldMapGradientH` | 기존 |

스크롤바 표시 규칙은 기존 `m_VerticalScrollbarVisibility = AutoHideAndExpandViewport` 그대로다(셀이 스크롤 영역에 다 들어가면 숨김).

### 2.3 셀 (`SynergyModalCell.prefab`)

셀 prefab 하나만 수정한다. 팝업 prefab 안의 셀은 이 prefab 의 인스턴스(origin)라 별도 오버라이드 없이 반영된다.

`SynergyModalCell` 자식의 최종 형제 순서(뒤 → 앞): CellGradient, CellGloss, StripGlow, **AxisStrip**(기존, 자식 StripShine·StripShade), IconGlow, **IconSlot**(기존), **Icon**(기존), TierGem1, TierGem2, TierGem3, InsetFill, InsetTop, InsetFrame, InsetDivider, **Label**(기존), **TierBadgeBg**(기존), **TierBadge**(기존), **DescText**(기존), **NextText**(기존).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 셀 면·테두리·입체 | 셀 루트 Image 스프라이트를 `Px_PanelDark` → `SynergyModalCellBg` 로 교체(Sliced). 루트에 `Shadow` (0,−3) 색 `#000000` 알파 0.40 | `SynergyModalCellBg` | 없음 |
| 셀 가로 그라데이션 | `CellGradient`: 셀 안쪽 stretch(좌 3 · 우 3 · 상 5 · 하 6 오프셋 = 베벨 안쪽), `WorldMapGradientH` 색 `#222c44`(왼쪽 `#222c44` → 오른쪽 면색 `#151b2c`) | `WorldMapGradientH` | 없음 |
| 셀 상단 광택 | `CellGloss`: 좌상단 (3,5) 386x62, `WorldMapGradientV` 흰색 알파 0.05 | `WorldMapGradientV` | 없음 |
| 축 색 띠 | 기존 `AxisStrip`(Px_Solid, 코드가 `AxisColor` 로 칠함) 유지. 자식 `StripShine`: `WorldMapGradientV` 흰색 알파 0.35(위가 밝음), `StripShade`: `WorldMapGradientV` scale (1,−1) 색 `#000000` 알파 0.40(아래가 어두움), 둘 다 stretch | `WorldMapGradientV` | 기존 `_axisStrip` |
| 띠 발광 | `StripGlow`: 중심 (8,68), 32x144, UISoftGlow, 프리팹 알파 0.50, **색은 코드 틴트(축 색)** | `UISoftGlow` | **신규 `_axisTintImages`** |
| 아이콘 후광 면 | 기존 `IconSlot` Image 스프라이트를 `Px_PanelSunk` → `SynergyModalHalo` 로 교체(Simple, 위치·크기 52x52 불변), 프리팹 알파 1.00, **색은 코드 틴트(축 색)** | `SynergyModalHalo` | **신규 `_axisTintImages`**(기존 위젯을 목록에 넣을 뿐 위치·구성 불변) |
| 아이콘 후광 발광 | `IconGlow`: 중심 (46,68)(= `IconSlot` 중심), 84x84, UISoftGlow, 프리팹 알파 0.45, **색은 코드 틴트(축 색)** | `UISoftGlow` | **신규 `_axisTintImages`** |
| 축 아이콘 | 기존 `Icon`(36x36, 코드가 스프라이트·활성 제어) 불변 | — | 기존 `_icon` |
| Tier 보석 3개 | `TierGem1`~`3`: 각 22x22, 중심 (31,116) / (49,116) / (67,116). 아이콘 중심 x 46 이 아니라 후광 중심 49 를 가운데로 둔 좌우 대칭. 색은 **코드가 켜짐(= 축 색) / 꺼짐(`#3a4764`) 으로 틴트**(§5) | `VillageHudGem` | **신규 `_tierGems`** |
| 효과 문구 inset 바닥 | `InsetFill`: 좌상단 (77,37) 304x92, `Px_Solid` 색 `#0d121c` | `Px_Solid` | 없음 |
| inset 윗면 어둠 | `InsetTop`: `InsetFill` 과 같은 박스, `WorldMapGradientV` 색 `#080b12`(위 `#080b12` → 아래 `#0d121c`) | `WorldMapGradientV` | 없음 |
| inset 테두리 | `InsetFrame`: 같은 박스, Sliced, fillCenter 끔 | `WorldMapInset` | 없음 |
| 현재/다음 구분선 | `InsetDivider`: 좌상단 (84,89) 294x1, `Px_Solid` 흰색 알파 0.08 | `Px_Solid` | 없음 |
| 라벨 | 기존 `Label` 불변 | — | 기존 `_label` |
| 단계 배지 | 기존 `TierBadgeBg` Image 스프라이트를 `Px_Badge_Soul` → `SynergyModalBadge` 로 교체(Sliced, 64x28 불변). **색은 코드 틴트: 일반 = 축 색, 최대 단계 = 금색 `#f7c64a`**. 기존 `TierBadge` 글자 색도 같은 규칙(일반 = 축 색, 최대 = 금색) | `SynergyModalBadge` | **신규 `_tierBadgeBg`**, 기존 `_tierBadge` |
| 효과 문구 | 기존 `DescText`·`NextText` 불변. `NextText` 색: 일반 `#8e98ad`(현재 prefab 값), 최대 단계 금색 `#f7c64a` | — | 기존 `_nextText` |

검산:
- 셀 베벨 안쪽 영역: 가로 3 ~ 389(폭 386 = 392 − 3 − 3), 세로 5 ~ 130(높이 125 = 136 − 5 − 6).
- inset: x 77 ~ 381, y 37 ~ 129. `DescText` x 84 ~ 378 · y 42 ~ 86, `NextText` x 84 ~ 378 · y 92 ~ 128 이 전부 inset 안(우측 여유 3, 하단 여유 1). 구분선 y 89 는 `DescText` 하단 86 과 `NextText` 상단 92 사이.
- 셋째 보석 눈에 보이는 마름모 반폭 = 22 × 28/36 / 2 = 8.6 → 67 + 8.6 = 75.6 < inset 왼쪽 77. 간격 19 였다면 70 + 8.6 = 78.6 > 77 로 겹친다.
- 보석 하단 116 + 11 = 127 < 셀 하단 베벨 시작 130. 아이콘 후광 하단 94 < 보석 마름모 상단 116 − 8.6 = 107.4.
- 띠 발광 x −8 ~ 24 는 뷰포트 좌측 여백 6 보다 2 바깥으로 나가지만 소프트글로우 가장자리 알파가 0 에 가까워 눈에 띄지 않는다.
- 기존 위젯의 `Raycast`·`Maskable` 설정은 건드리지 않는다. 신규 장식 Image 는 `Maskable` 기본값(true) 이라 스크롤 뷰포트(`RectMask2D`)에 함께 잘린다.

**비용**: 셀당 신규 Image 14장(CellGradient · CellGloss · StripGlow · StripShine · StripShade · IconGlow · TierGem×3 · InsetFill · InsetTop · InsetFrame · InsetDivider). 현재 풀 셀 수가 최대 4~5개 수준이라 신규 Image 합계는 70장 이하이며, 기존 PlayMode 풀링 재현 테스트(`SynergyModalPopupPoolingRepro`)가 그대로 통과해야 한다.

---

## 3. 신규 스프라이트 (3종) 와 재사용

### 3.1 재사용 (신규 생성 0건)

| 스프라이트 | 위치 | 이 작업에서의 용도 |
|---|---|---|
| `WorldMapFrame` | `Assets/_Lair/Art/Sprites/` | 패널 이중 테두리, 명패 테두리 |
| `WorldMapCornerGold` | 〃 | 모서리 장식 4개 |
| `WorldMapGradientV` / `WorldMapGradientH` | 〃 | 바닥 그라데이션, 광택, 룬 라인, 띠 음영, 핸들 음영 |
| `WorldMapInset` | 〃 | 효과 문구 inset 테두리 |
| `VillageHudGem` | 〃 | 명패 마름모, 빈 상태 마름모, Tier 보석 |
| `VillageHudCta` | 〃 | 닫기 버튼 붉은 입체 면(색 `#e0525a` → `#b8323a` → `#8f2029` → `#a62a33` 가 목업의 닫기 버튼 그라데이션과 같은 팔레트) |
| `UISoftGlow` | 〃 | 제목·띠·후광·닫기 발광 |
| `Px_Solid` | `Assets/_Lair/Art/Sprites/UiDot/` | 단색 바닥(패널·명패·inset·구분선·스크롤) |

### 3.2 신규 3종

위치는 `Assets/_Lair/Art/Sprites/` 루트, 접두어 `SynergyModal`(`WorldMap*` · `VillageHud*` · `BattleHud*` 관례와 동일). 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.

- 생성 방법: **일회용 에디터 스크립트** `Assets/_Lair/Editor/SynergyModalPolishSpriteGenerator.cs` 가 `Texture2D` 에 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋되면 **스크립트를 삭제한다**(Rule 04 §3 — 단일 진실은 PNG).
- 임포트 공통: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center, Wrap Clamp.
- 곡선(원) 가장자리는 4x4 슈퍼샘플링으로 커버리지를 구해 알파에 반영한다. 색은 sRGB `#rrggbb`, 알파는 0~255 정수(0.xx × 255 반올림).
- 스프라이트에 글자를 굽지 않는다.

| # | 파일명 | 크기(px) | 9-slice border (좌,하,우,상) | Filter | 내용 (픽셀 정의) |
|---|---|---|---|---|---|
| 1 | `SynergyModalCellBg.png` | 16x16 | 3,6,3,5 | Point | 불투명. 위·좌·우 바깥 3px `#07090e`, 아래 바깥 3px `#07090e`. 위 안쪽 2행(바깥 3행 다음) `#3d4b6e`, 아래 안쪽 3행(바깥 3행 다음) `#0b0e16`. 나머지 안쪽 면 `#151b2c`. (대각 모서리 3x3 은 `#07090e`) |
| 2 | `SynergyModalHalo.png` | 64x64 | 없음 | Bilinear | 중심 (32,32) 원 반지름 31(가장자리 AA). 바깥 2px(반지름 29~31) 흰색 명도 0.70 불투명. 안쪽 면은 회색조 라디얼: 반지름 비율 rn = r/29 에서 명도 0.45(rn 0) → 0.04(rn 0.68 이상) 선형, rn 0.85~1.00 구간은 위 명도에 1.0 → 0.6 선형 배율을 곱해 안쪽 그림자를 만든다. 틴트가 곱해져 면 중심은 축 색 45%, 가장자리는 거의 검정, 테두리는 축 색 70% 가 된다(목업 후광: 중심 축 색 38% 혼합 · 테두리 축 색 70%) |
| 3 | `SynergyModalBadge.png` | 16x16 | 4,4,4,4 | Point | 바깥 2px 흰색 알파 179(= 0.70 × 255 반올림). 안쪽 면 회색조 세로 그라데이션 명도 0.22(위) → 0.11(아래) 불투명. 면의 위 2행(바깥 2행 다음)은 명도 0.07 불투명(목업의 안쪽 위 그림자). 틴트가 곱해져 일반은 축 색의 어두운 유리 칩, 금색 틴트는 `#362b10`(위) → `#1b1508`(아래) 의 어두운 금색 칩(목업 `#3a2f12` → `#1c1608`)이 된다 |

검산:
- Halo 렌더 52px 에서 테두리 2 × 52/64 = 1.6px(목업 2px). 가운데 라디얼 반지름 비율 0.68 → 렌더 반지름 26 × 0.68 = 17.7px(목업 68% 지점).
- Badge 금색 틴트: 위 명도 0.22 × (247,198,74) = (54,44,16) = `#362c10`, 아래 0.11 × (247,198,74) = (27,22,8) = `#1b1608`. 테두리 0.70 × `#f7c64a` = (173,139,52) = `#ad8b34`(목업 `#b9832a`).
- CellBg: 9-slice 세로 5 + 6 = 11 ≤ 16, 가로 3 + 3 = 6 ≤ 16.

---

## 4. 애니메이션

**신규 애니메이션 0건.** 목업에도 움직임이 없고, 이 팝업은 전투 일시정지 중 짧게 확인하는 정보 화면이라 `UILoopMotion` 을 쓰지 않는다. 풀링 셀에 루프 모션 컴포넌트를 두면 셀 재사용 시 기준 포즈 저장 시점이 어긋날 위험도 있다.

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | 애니메이션 없음 | 풀링 셀과 충돌 없음, 구현·테스트 최소 | 발광이 정적 | 채택 |
| B | 최대 단계 배지 금색 글로우 `UILoopMotion` Pulse | 만렙 축이 눈에 띔 | 풀 재사용마다 모션 시작/정지 제어 필요(`SetRunning` 호출이 셀 Bind 에 추가됨), 가설 검증에 불필요 | 기각 |
| C | 후광 느린 맥동 | 화면이 살아 보임 | 셀당 상시 Update, 가독성 이득 없음 | 기각 |

닫기 버튼 누름 반응은 기존 ColorTint(×0.85) 를 유지한다(목업의 `translateY(2px)` 는 쓰지 않는다 — §9).

---

## 5. SerializeField·코드 변경 (View 한정, 최소)

### 5.1 유지 (이름·타입·의미 불변)

- `SynergyModalPopup`: 기존 전부(`_dimButton` · `_closeButton` · `_scrollView` · `_emptyText` · `_headerRowsOnly` · `_tankIcon` · `_dpsIcon` · `_debuffIcon` · `_swarmIcon`). **팝업 쪽 신규 필드 0건, 팝업 코드 변경은 §5.3 의 한 줄뿐이다.**
- `SynergyModalCell`: 기존 전부(`_axisStrip` · `_icon` · `_label` · `_tierBadge` · `_descText` · `_nextText`).

### 5.2 신규 `[SerializeField] private` (`SynergyModalCell` 에만)

| 필드 | 타입 | 용도 | 미할당 시 |
|---|---|---|---|
| `_tierBadgeBg` | `Image` | 단계 배지 면 틴트(일반 = 축 색 / 최대 = 금색) | 건너뜀 |
| `_tierGems` | `Image[]` (원소 3) | Tier 보석 켜짐/꺼짐 | 건너뜀 |
| `_axisTintImages` | `Image[]` (원소 3: `StripGlow` · `IconGlow` · `IconSlot` 순) | 축 색 틴트 대상 | 건너뜀 |

기존 `_axisStrip` 이 이미 `Image` 라 같은 틴트 로직에 합치지 않는다(`_axisStrip` 은 알파 1 로 칠하고, `_axisTintImages` 는 각 Image 의 프리팹 알파를 보존).

### 5.3 신규 데이터 필드와 동작

- `SynergyModalCellData` 에 `public int ActiveTier` 추가(기본 0). `SynergyModalPopup.BuildRows` 의 헤더 행 생성부가 이미 계산하는 `tiers` 를 이 필드에 한 줄로 대입한다. 효과 행은 대입하지 않아 0. **`TierBadgeText`·`IsMaxTier`·`DescText`·`NextText`·`Label` 등 기존 필드 값과 `BuildRows` 의 반환 구조는 바뀌지 않는다.**
- `SynergyModalCell.Bind` 추가 동작(전부 null 가드, 기존 호출 흐름 유지):
  1. **축 색 틴트**: `_axisTintImages` 각 원소의 색을 `AxisColor` 의 rgb + 그 Image 의 현재 알파로 설정(프리팹이 정한 알파 보존).
  2. **배지**: 헤더 행일 때 `_tierBadgeBg` 활성, 색 = `IsMaxTier ? UiDotPalette.Gold : AxisColor`(알파 1). 효과 행이면 비활성. `_tierBadge` 글자 색도 같은 값(현재 코드는 항상 `AxisColor`).
  3. **보석**: 헤더 행이면 `_tierGems[i]` 활성, 켜짐(`i < ActiveTier`)은 `AxisColor`, 꺼짐은 `#3a4764`. 효과 행이면 전부 비활성. 배열 길이가 3 이 아니어도 `i < _tierGems.Length` 범위에서만 처리.
  4. **다음 단계 문구 색**: 헤더 행일 때 `_nextText` 색 = `IsMaxTier ? UiDotPalette.Gold : #8e98ad`. 풀 재사용 시 이전 행의 금색이 남지 않게 **매 Bind 마다 명시적으로 지정**한다.
- 새 상수는 `SynergyModalCell` 안에 둔다: 보석 꺼짐색 `#3a4764`, 다음 단계 일반색 `#8e98ad`. 금색은 기존 `UiDotPalette.Gold`(`#f7c64a`)를 재사용한다.
- 풀 재사용 안전성: 비주얼 리셋을 `OnEnable` 에 두지 않는다는 기존 주석의 원칙을 유지한다 — 위 4개 동작은 모두 `Bind` 안에서 끝나고 `OnEnable` 은 건드리지 않는다.

**대안 비교** (보석 켜짐 개수를 어디서 얻는가):

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | `TierBadgeText` 를 "n/3" 로 파싱 | 데이터 클래스 변경 0건 | 문자열 포맷에 종속(배지 문구가 바뀌면 보석이 조용히 깨짐), 테스트가 취약 | 기각 |
| B | `SynergyModalCellData.ActiveTier` 신설 + `BuildRows` 한 줄 | 명시적, 순수 로직 테스트 가능, 기존 값 불변 | 데이터 클래스와 `BuildRows` 한 줄 변경 | 채택 |
| C | `IsMaxTier` 만으로 보석 3/3 vs 나머지 처리 | 코드 최소 | 1단계·2단계 구분 불가 → 보석 목적 상실 | 기각 |

---

## 6. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/SynergyModalCellBg.png` · `SynergyModalHalo.png` · `SynergyModalBadge.png` + `.meta` | 신규(§3.2) |
| `Assets/_Lair/Editor/SynergyModalPolishSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 **삭제**(Rule 04 §3) |
| `Assets/_Lair/Scripts/UI/SynergyModalCell.cs` | §5 필드·동작 추가, `SynergyModalCellData.ActiveTier` 추가 |
| `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` | `BuildRows` 헤더 행에 `ActiveTier = tiers` 한 줄 |
| `Assets/_Lair/Art/UI/SynergyModalCell.prefab` | §2.3 전부 + 신규 필드 3종 와이어링 |
| `Assets/_Lair/Art/UI/SynergyModalPopup.prefab` | §2.1·§2.2 전부 |

- 프리팹 작업은 **기존 프리팹에 추가·교체만** 한다(새로 만들어 덮어쓰기 금지 — 손-편집 보존, 기존 fileID 보존). 일회용 에디터 스크립트나 UnityMCP 로 편집하며, 스크립트를 쓰면 완료 직후 삭제한다(Rule 04 §3).
- 신규 텍스트 위젯 없음(문구 0건). 기존 TMP 에는 이미 `CHText` 가 붙어 있고, 활성화하는 `X` 도 `CHText` 가 붙어 있다.
- `Instantiate`/`new GameObject` 로 장식을 코드에서 만들지 않는다. 전부 프리팹 정적 배치.
- UI 배선 전 목업 승인 게이트: `.mockups/synergy-modal-redesign.html` 은 이미 승인되었으므로 바로 진행한다.

---

## 7. 기존 테스트·프리팹 연결 검사 영향

| 대상 | 영향 | 근거 |
|---|---|---|
| `SynergyModalPopupBuildTests` · `SynergyModalRealStringIntegrationTests` · `SynergyModalPopupIconTests` · `SynergyModalPopupIconEdgeCasesTests` · `SynergyModalPopupEdgeCasesTests` · `SynergyRealStringTableTests` · `UiRedesignDataTests`(시너지 모달 행 검증) | 무수정 통과가 기준 | `BuildRows` 기존 필드 값 불변, `ActiveTier` 는 초기화자에 없는 필드라 기존 `new SynergyModalCellData { ... }` 에 영향 없음 |
| `SynergyModalPopupPoolingRepro`(PlayMode) | 무수정 통과가 기준 | 리플렉션이 읽는 기존 필드(`_scrollView` · `_icon` 등) 이름 불변, 셀 좌표·풀 구조 불변 |
| `UiLayoutAudit`(에디터) 의 `SynergyModalCell/DescText`·`NextText`·`Label`·`TierBadge` 샘플 | 변경 없음 | 텍스트 RectTransform 좌표·크기·폰트 불변 |
| 프리팹 연결 일괄 검사 계열 | 신규 `[SerializeField]` 3종은 프리팹에서 비우면 실패하도록 §8.1 에서 검사 추가 | 배열 필드는 기존 일괄 검사가 못 잡으므로 별도 검사 |

실행 전 확인(gameplay-programmer): `_tierBadge` 의 색을 단정하는 테스트가 있는지 Grep 으로 한 번 더 확인한다(현재 EditMode `SynergyModal*` 테스트에서는 `SynergyModalCell` 직접 참조가 0건이다).

---

## 8. 테스트 관점

### 8.1 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| 신규 스프라이트 3종 | 파일이 존재하고 Texture Type = Sprite, `SynergyModalCellBg` 9-slice border (좌,하,우,상) = (3,6,3,5), `SynergyModalBadge` = (4,4,4,4), `SynergyModalHalo` border 없음, `SynergyModalCellBg`·`SynergyModalBadge` filterMode = Point, `SynergyModalHalo` = Bilinear |
| 셀 prefab 연결 | `_tierBadgeBg` 비어 있지 않음 · `_tierGems` 길이 3 · 원소 전부 비어 있지 않고 스프라이트 = `VillageHudGem` · `_axisTintImages` 길이 3 · 원소 이름이 순서대로 `StripGlow` / `IconGlow` / `IconSlot` · `IconSlot` 스프라이트 = `SynergyModalHalo` · `TierBadgeBg` 스프라이트 = `SynergyModalBadge` · 셀 루트 스프라이트 = `SynergyModalCellBg` |
| 기존 위젯 좌표 회귀 | 셀 prefab 의 `AxisStrip`·`IconSlot`·`Icon`·`Label`·`TierBadgeBg`·`TierBadge`·`DescText`·`NextText` 의 anchor·anchoredPosition·sizeDelta 가 §1.1 값과 일치. 팝업 prefab 의 `TitlePlaque`·`CloseButton`·`ScrollView`·`EmptyText` 도 일치 |
| 신규 장식 레이캐스트 | 두 prefab 의 신규 장식 Image(§2 표의 신규 이름 전부) `raycastTarget` = false, `ModalBody` Image `raycastTarget` = true |
| 팝업 prefab 마감 | `CloseButton` Image 스프라이트 = `VillageHudCta`, 자식 `X` 활성, `ModalBody` 자식 형제 순서가 §2.1 과 일치, `EmptyDiamond` 가 `EmptyText` 의 자식, 스크롤 `Handle` 색 = `#e0a21c` |
| 셀 Bind — 보석 | prefab 인스턴스에 `ActiveTier` = 0/1/2/3 인 헤더 데이터를 Bind: 켜진 보석 수 = ActiveTier, 켜진 보석 색 rgb = `AxisColor` rgb, 꺼진 보석 색 = `#3a4764`. 효과 행 Bind 시 보석 3개 비활성 |
| 셀 Bind — 최대 단계 | `IsMaxTier` = true: `_tierBadgeBg` 색 = `UiDotPalette.Gold`, `_tierBadge` 글자 색 = 금색, `_nextText` 색 = 금색. false 로 재 Bind 하면 `_tierBadgeBg` = 축 색, `_nextText` = `#8e98ad` 로 복귀(풀 재사용 시 금색 잔류 없음) |
| 셀 Bind — 축 색 | `_axisTintImages` 각 Image 의 rgb = `AxisColor` rgb 이고 알파는 Bind 전과 같음. 다른 축 색으로 재 Bind 해도 알파 불변 |
| 셀 Bind — 가드 | 신규 필드 3종을 비운 셀(`new GameObject` + 컴포넌트)에 `Bind` 를 호출해도 예외 없음 |
| `BuildRows` | 축별 count 5 → 헤더 `ActiveTier` = 2, count 7 → 3, count 3 → 1. 헤더의 기존 필드(`TierBadgeText` · `IsMaxTier` · `DescText` · `NextText` · `Label`) 값은 변경 전 기대값과 동일. 효과 행 `ActiveTier` = 0 |
| 문구 | `Strings_Ko` 무결성 테스트 무수정 통과(신규 문구 0건) |

### 8.2 PlayMode

신규 PlayMode 테스트 없음. 기존 `SynergyModalPopupPoolingRepro` 가 그대로 통과하면 셀 풀링·Bind 흐름이 보존된 것이다.

### 8.3 수동 확인 (사용자 육안, 팝업을 열어서)

- TANK 5장(2단계) · DPS 3장(1단계) · DEBUFF 7장(3단계) 조합: 보석 켜짐 수 2/1/3, 3단계 셀의 배지·다음 문구 금색, 축마다 발광 색이 다른지.
- 빈 상태(활성 단계 0)에서 `EmptyText` 와 마름모가 함께 보이고, 한 축이라도 켜지면 둘 다 사라지는지.
- 셀 4개 이상(스크롤 발생)에서 금색 슬림 스크롤바가 나타나고, 셀 장식이 뷰포트 경계에서 함께 잘리는지.
- 닫기 × 글자가 보이고, X 클릭과 dim 클릭으로 닫히며, 패널 본문 클릭으로는 닫히지 않는지(`ModalBody` raycast 유지).
- 제목 명패가 패널 중앙에 정렬되고 마름모가 좌우 대칭인지.

---

## 9. 시안과 다르게 포기하는 연출

| 항목 | 목업 | Unity 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·텍스트 글로우 | 제목·라벨·배지·다음 문구에 `text-shadow` | 생략 | TMP 그림자는 공용 폰트 머티리얼(SDF 에셋) 수정이 필요해 다른 화면에 영향. 가독성은 명패·inset 면이 보강 |
| 라벨 "(N장)" 소형·축 색 | `<i>` 로 분리 | 한 줄 단색 `Label` 유지 | §1.2 |
| 셀 바깥 축 색 발광 | `0 0 14px` | 생략 | 스크롤 뷰포트가 셀 좌우 6px 에서 잘라 번짐이 한쪽만 끊김. 띠 발광으로 대체 |
| 배지 바깥 발광·손잡이 발광 | 배지 `0 0 8px`, 스크롤 핸들 `0 0 6px` | 생략 | 배지는 글로우 Image 를 셀마다 추가해야 하고, 핸들 글로우는 핸들을 따라가는 별도 위치 추종이 필요 |
| 패널 바깥 60px 어둠 번짐 | `0 0 60px #000a` | `Shadow`(0,−10) 하나 | 번짐은 dim 이 대체 |
| 닫기 버튼 누름 `translateY(2px)`·hover 밝기 | 있음 | 기존 ColorTint 누름 ×0.85 | `Selectable` 구성 불변, 모바일에 hover 없음 |
| 닫기 버튼 상단 광택선 | 별도 흰색 광택 | `VillageHudCta` 에 구워진 상단 하이라이트 | 스프라이트 재사용 |
| 명패 테두리 두께 | 7px(3+2+2) | 9px(`WorldMapFrame`) | 스프라이트 재사용. 52px 높이에서 2px 차이 |
| 패널 테두리 금색 | `#8a6a2c` | `#6b5a2a`(`WorldMapFrame`) | 월드맵 팝업과 같은 프레임으로 통일 |
| Tier 보석 꺼짐 표현 | 어두운 면 + 밝은 테두리 2톤 | 단색 `#3a4764` 마름모(스프라이트 명도 1.0 → 0.78 그라데이션) | `VillageHudGem` 재사용. 켜짐 보석과의 구분은 밝기·글로우 차이로 충분 |
| 보석 간격 | 19 | 18 | §1.2 |
| inset 상단 12px 광택 | 있음 | 생략 | `WorldMapInset` 흰 링과 `InsetTop` 어둠이 대체 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 스프라이트 3종은 프리팹 직접 참조: `SynergyModalCellBg` · `SynergyModalHalo` · `SynergyModalBadge`. 삭제 대상 에셋 없음(기존 `Px_Panel` · `Px_Plaque` · `Px_PanelDark` · `Px_PanelSunk` · `Px_Badge_Soul` · `Px_CloseX` 는 다른 프리팹이 쓰므로 유지).
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `SynergyModalCellData`: `public int ActiveTier` 추가(§5.3).
  - `SynergyModalPopup.BuildRows`: 헤더 행에 `ActiveTier = tiers` 한 줄(§5.3).
  - `SynergyModalCell`: 신규 `[SerializeField] private` — `_tierBadgeBg`(`Image`) · `_tierGems`(`Image[]`) · `_axisTintImages`(`Image[]`). `Bind` 추가 동작 4종(§5.3), 전부 null 가드.
- **프리팹**: §6 표. 필드·오브젝트 이름은 테스트와 프리팹 와이어링 기준이므로 글자 그대로 쓴다(`StripGlow` · `IconGlow` · `TierGem1`~`3` · `InsetFill` · `InsetTop` · `InsetFrame` · `InsetDivider` · `CellGradient` · `CellGloss` · `StripShine` · `StripShade` · `BodyGradient` · `BodyGloss` · `BodyFrame` · `CornerTL`/`TR`/`BL`/`BR` · `RuneL` · `RuneR` · `TitleGlow` · `PlaqueGradient` · `PlaqueFrame` · `PlaqueDiamondL`/`R` · `EmptyDiamond` · `CloseGlow` · `HandleShine` · `HandleShade`). 클래스 내부 구조·메서드 분할은 gameplay-programmer 판단.
- **일회용 도구**: `SynergyModalPolishSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.
- **UI 목업**: `.mockups/synergy-modal-redesign.html`(승인 완료, 수정 없음).
