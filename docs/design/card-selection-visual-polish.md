# 카드 선택 팝업(CardSelectionPopup) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/card-selection-redesign.html` 의 마감(금테 명패 제목·종류색 유리 부제 칩·금테 이중 프레임 카드·종류색 안쪽 링/윗면 발광 띠/모서리 장식/광택·종류 라벨 유리 칩·금테 N/3 칩·이중 프레임 일러스트·이름 스크림과 밑줄·유리 inset 효과 문구·종류색 입체 선택 버튼·축 후광과 Tier 보석 3개)을 `CardSelectionPopup.prefab` 에 옮기고, 사용자 결정에 따라 **호버(떠오름 + 금색 글로우)·선택(눌림 구간에서 더 떠오름 + 버튼 밝아짐 + 나머지 두 장 어두워짐) 연출**을 View 전용 최소 코드로 추가한다. 레이아웃·좌표·문구·동작·ViewModel·선택 로직은 바꾸지 않는다.
- **검증 가설**: 전투 중 가장 자주 보는 팝업에서 마감만 올려도 (1) 패시브(녹)/액티브(금) 구분과 카드의 축·Tier 가 읽기 쉬워지고, (2) 호버·눌림 반응이 "지금 어느 카드를 고르는지"를 선택 직전에 확신시키며, (3) 마을 HUD·배틀 HUD·모달 3종과 같은 톤으로 읽히는가. 정량 메트릭 없음 — 사용자 육안 확인용 UI 개선(선행 사례 `synergy-modal-visual-polish.md` · `build-modal-visual-polish.md` · `battle-hud-visual-polish.md` 와 같은 성격).
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 규칙·메타/서버 기능 없음. 문구 신규 0건.
- **핵심 메커니즘**: (1) 선행 3개 기획서가 만든 스프라이트(`WorldMap*` · `VillageHud*` · `SynergyModal*` · `UISoftGlow`)를 최대한 재사용하고 신규 스프라이트는 `CardSelect*` 7종만 절차 생성한다(§3). (2) 종류색·축색 장식은 `CardView.SetKind`/`SetAxisIcon` 이 이미 칠하는 색을 그대로 틴트 대상 배열에 얹는다(§5). (3) 호버/눌림은 `CardView` 가 상태(휴지/호버/눌림/어두움)를 들고 0.16초 단순 트윈으로 표시만 바꾼다. 입력 판정은 기존 `PickButton`(전면 투명 `Button`) 그대로이고 포인터 수신용 소형 릴레이만 같은 오브젝트에 붙인다(§4).

이 문서의 수치가 목업과 다르면 이 문서가 우선한다. 달라지는 지점은 §1.2 와 §9 에 모았다.

---

## 1. 불변 사항

### 1.1 불변 목록

- 레이아웃·좌표·크기·글자 크기·문구·동작 전부 불변이다. 프리팹 실측값(이 값이 기준이다).
  - `Dim` 전체 stretch, 색 `#04060a` 알파 0.902(= 0.9), `raycastTarget` 켬. 변경 없음.
  - `Title` 중앙 앵커 (0,258) 600x44 f22. `Subtitle` 중앙 앵커 (0,220) 600x30 f22(색은 `UiDotPalette.CardKind` 로 코드가 칠함). `SynergyCaption` 중앙 앵커 (0,−290) 400x20 f14.
  - `CardsLayout` 중앙 앵커 (0,−40) 912x460, `HorizontalLayoutGroup`(간격 36, 가운데 정렬, 자식 크기·확장 제어 끔). 카드 `CardView_0/1/2` 280x460, 화면 좌상단 기준 x 184 / 500 / 816, y 170~630.
  - 카드 내부(카드 좌상단 기준, x 오른쪽·y 아래): `Bg`·`Border`·`KindRing` 전체 stretch, `ArtSlot` 상단 14 높이 250 좌우 14, `CardArt` 상단 18 높이 242 좌우 18, `Scrim` 상단 160 높이 100 좌우 18, `NameText` 상단 222 높이 34 좌우 18(f22, 자동 크기 11~22), `KindLabelBg`/`KindLabel` (24,24) 84x28 f16, `CountBadgeBg`/`CountBadge` 우상단 (−24,−24) 72x28 f16 피벗 (1,1), `AxisIcon` (24,58) 44x44(자식 `Icon` 36x36), `DescText` 하단 78 높이 104 좌우 18(f17, 자동 크기 16~22), `PickButton` 전체 stretch(알파 0, `raycastTarget` 켬, `Button` Transition 없음), `PickVisual` 하단 14 높이 56 좌우 18.
  - `SynergyChip` 4개(`BuildSynergyCell` 프리팹 인스턴스) 165x45, 중심 x ±88.5 / ±265.5, y −326.5. 위치·크기·인스턴스 오버라이드 변경 0건.
- 닫기 동작(`Close(reuse:false)`)·`OnPicked` 1회 호출·`CHMSound` `EAudio.CardSelect` 재생·부제 문구(`BuildSubtitle`)·카드 이름/설명(`cards.json`)·3픽 캡 배지(`pickCount` → "N/3")·하단 칩 `BuildSynergyCellData.Create(axis, count, icon, count)` 전부 불변.
- `BattleViewModel` · `BattleController` · `CardSelectionArg` · `CardData` · `BuildSynergyPanel` · `BuildSynergyCell` · `BuildSynergyCell.prefab` 변경 0건. `Strings_Ko` 신규 문구 0건 · 신규 JSON 0건 · 신규 SO 0건 · 신규 Enum 0건 · 신규 Interface 0건 · 신규 Addressable 키 0건(`CardSelectionPopup` 프리팹 키 그대로).
- 기존 `[SerializeField]` 필드는 이름·타입·연결 대상을 전부 유지한다(`CardSelectionPopup`: `_slots` `_subtitleText` `_synergyChips`, `CardView`: `_nameText` `_descText` `_border` `_artImage` `_pickButton` `_countBadge` `_kindRing` `_kindLabel` `_pickButtonImage` `_passiveButtonSprite` `_activeButtonSprite` `_countBadgeBg` `_axisIconRoot` `_axisIconBorder` `_axisIconImage`). **기존 위젯의 재부모와 위치·크기 이동은 하지 않는다.** 허용되는 것은 신규 장식 GameObject 추가, 기존 Image 의 스프라이트·색 교체, 형제 순서 삽입, 기존 TMP 의 정적 색·정렬·Margin 값 설정뿐이다.
- `PickButton` 의 `Button`·`CHButton` 연결과 `_pickButton.OnClick(onClick)` 호출은 그대로다. 카드 클릭 영역은 `PickButton` 의 전면 `Image`(알파 0)다.
- **신규 장식 Image 는 전부 `raycastTarget = false`** 다. 예외는 `DecoHoverGuard` 하나(§4.3)이며 이 오브젝트는 기본 비활성이다. 이름이 `Deco` 로 시작하는 오브젝트가 신규 장식이다.
- 신규 장식은 `Instantiate`/`new GameObject` 로 만들지 않는다. 전부 프리팹에 정적 배치한다.

### 1.2 목업과 실제 prefab 의 불일치 정정 (이 문서의 값이 우선)

| 항목 | 목업 | 실제 prefab / 이 문서의 결정 | 근거 |
|---|---|---|---|
| 부제 글자 크기 | 칩 안 17px | prefab `Subtitle` f22 유지. 칩 폭을 380 으로 키워 22px 글자를 담는다(§2.1) | "글자 크기 불변". `UiLayoutAudit` 의 최장 샘플 `패시브 · 영웅 HP 100% 도달` 이 600 폭에 들어간다 |
| 부제 칩 폭 | 글자에 맞춰 늘어나는 칩 | 380x30 고정(패시브·액티브 어느 문구여도 칩 크기 불변) | 정적 스프라이트 배치. 칩 폭은 최장 샘플 글자 폭(§2.1 검산)보다 크다 |
| 제목 명패 y | 명패 중심 y 99, 글자 박스 중심 y 102 | 목업 그대로 명패 중심 y 99 (anchoredPosition y +261), `Title` (0,+258) 불변 | 승인된 시안의 배치. 글자가 3px 아래로 놓인다 |
| dim | `rgba(4,6,10,.9)` | prefab 값 `#04060a` 알파 0.902 유지 | 두 값이 사실상 같다 — 변경 없음 |
| 축 아이콘 크기 | SVG 28x28 | `Icon` 36x36 유지 | `SynergyIndicatorPrefabTests` 가 표시 크기를 PNG 해상도의 정수배로 검사한다 |
| 종류 링 색 | 종류색 55% + `#1b2438` 45% 혼합 | 코드가 칠하는 종류색 알파 1 에 신규 링 스프라이트의 베이크 알파 0.65 를 곱한다 | 코드 변경 없이 혼합 효과를 낸다(§3.2 검산) |
| Tier 보석 y | 카드 상단에서 79 | 80 (후광 중심 y 와 일치) | 후광 중심 y = 58 + 22 = 80. 1px 보정 |
| `Border` 위젯 | 시안에 없음 | `Border` GameObject 를 프리팹에서 비활성(코드 변경 0건) | `Border` 는 스프라이트 없는 전면 Image 이고 `CardView.Bind` 가 매번 종(種) 색 알파 1 을 칠한다 → 시안의 어두운 유리 면을 덮는다. 비활성 GameObject 에 색을 쓰는 것은 안전하다 |
| 내 시너지 칩 | 시안용 유리 칩 | 공유 프리팹 `BuildSynergyCell` 그대로(배틀 HUD 폴리시 완료분) | 이 작업에서 수정 0건(§2.3) |

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image(스프라이트)** = 정적 장식, 프리팹에 박음 / **Shadow** = UGUI 컴포넌트 / **코드 틴트** = `CardView`·`CardSelectionPopup` 이 색을 칠함(§5) / **연출** = §4 의 상태 표현. 좌표는 별도 표기가 없으면 부모 박스 **좌상단 기준 (x 오른쪽, y 아래) px** 이며, 앵커는 좌상단(0,1) 피벗 좌상단(0,1)로 둔다는 뜻이다. "UISoftGlow" = `Assets/_Lair/Art/Sprites/UISoftGlow.png`, "Px_Solid" = `Assets/_Lair/Art/Sprites/UiDot/Px_Solid.png`. "그라데이션 V" = `WorldMapGradientV`(알파 1→0, 위→아래), "그라데이션 H" = `WorldMapGradientH`(알파 1→0, 왼→오)를 지정 색으로 틴트(정의는 `worldmap-popup-visual-polish.md` §2). 알파 환산: 0.07 = 18/255, 0.31 = 0x50/255.

### 2.1 팝업 루트 (`CardSelectionPopup.prefab`)

팝업 루트의 최종 형제 순서(뒤 → 앞): `Dim` → `DecoRuneL` → `DecoRuneR` → `DecoTitleGlow` → `DecoTitlePlaque`(자식 `DecoPlaqueGradient` · `DecoPlaqueFrame` · `DecoPlaqueDiamondL` · `DecoPlaqueDiamondR`) → `Title` → `DecoSubtitleGlow` → `DecoSubtitlePill` → `DecoSubtitleDiamondL` → `DecoSubtitleDiamondR` → `Subtitle` → `CardsLayout` → `DecoCaptionDiamondL` → `DecoCaptionDiamondR` → `SynergyCaption` → `SynergyChip_0..3`. 아래 좌표는 팝업 루트 **중앙 앵커 기준 (x 오른쪽, y 위)** 이다(기존 `Title`·`Subtitle`·`SynergyCaption` 과 같은 좌표계).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 룬 라인 2개 | `DecoRuneL`: 중심 (−243, +257.5) 150x3, 색 `#f7c64a`. `DecoRuneR`: 중심 (+243, +257.5) 150x3, scale (−1,1), 색 `#f7c64a` | `WorldMapGradientH` | 없음 |
| 제목 글로우 | `DecoTitleGlow`: 중심 (0,+261) 364x90, 알파 0.35, **색은 코드 틴트(종류색)** | `UISoftGlow` | **신규 `_kindTintImages[0]`** |
| 명패 바닥 | `DecoTitlePlaque`: 중심 (0,+261) 320x46, `Px_Solid` Simple, 색 `#10151f`. `Shadow` effectDistance (0,−6) 색 `#000000` 알파 0.44 | `Px_Solid` | 없음 |
| 명패 그라데이션 | `DecoPlaqueGradient`: `DecoTitlePlaque` 자식, 전체 stretch, 그라데이션 V 색 `#2c3858` | `WorldMapGradientV` | 없음 |
| 명패 테두리 | `DecoPlaqueFrame`: `DecoTitlePlaque` 자식, 전체 stretch, Sliced, fillCenter 끔 | `WorldMapFrame` | 없음 |
| 명패 마름모 2개 | `DecoPlaqueDiamondL`·`R`: `DecoTitlePlaque` 자식, 중앙 앵커 18x18, 위치 (−169, 0) / (+169, 0), 색 `#f7c64a`. 스프라이트 자체가 마름모라 회전하지 않는다 | `VillageHudGem` | 없음 |
| 제목 글자 | 기존 `Title`(CHText "카드 선택" 22) 불변. 정적 색 `#e8e1cf` 로 설정. 글자 글로우·그림자는 생략(§9) | — | 기존 |
| 부제 글로우 | `DecoSubtitleGlow`: 중심 (0,+220) 404x54(칩 380x30 + 각 12), 알파 0.35, **종류색 틴트** | `UISoftGlow` | **신규 `_kindTintImages[1]`** |
| 부제 칩 | `DecoSubtitlePill`: 중심 (0,+220) 380x30, Sliced(`CardSelectChip`, 9-slice 4), **종류색 틴트**(알파 1) | `CardSelectChip`(신규) | **신규 `_kindTintImages[2]`** |
| 부제 마름모 2개 | `DecoSubtitleDiamondL`·`R`: 중심 (−174,+220) / (+174,+220), 14x14, **종류색 틴트** | `VillageHudGem` | **신규 `_kindTintImages[3]`·`[4]`** |
| 부제 글자 | 기존 `Subtitle`(f22, 색은 `InitUI` 가 `UiDotPalette.CardKind` 로 칠함) 불변 | — | 기존 `_subtitleText` |
| 캡션 마름모 2개 | `DecoCaptionDiamondL`·`R`: 중심 (−46,−290) / (+46,−290), 14x14, 색 `#f7c64a`(정적) | `VillageHudGem` | 없음 |
| 캡션 글자 | 기존 `SynergyCaption`(f14) 불변. 정적 색 `#8e98ad` 로 설정 | — | 기존 |

검산:
- 룬 중심 x ±243 = 목업 좌 322~472 의 중심 397 − 640 = −243, 우 808~958 의 중심 883 − 640 = +243. 룬 중심 y = 360 − 102.5 = 257.5. 룬 안쪽 끝 x = 243 − 75 = 168 > 명패 좌측 −160 → 룬 끝(168)과 명패(160) 사이 8px.
- 명패 중심 y +261 = 360 − 99(목업 top 76 + 46/2). 글로우 364 = 320 + 2×22, 90 = 46 + 2×22.
- 명패 마름모 x ±169 = 160 + 9(목업 left −14 + 박스 10/2 = 명패 가장자리에서 9 바깥). 스프라이트 18 의 렌더 대각선 18 × 28/36 = 14 (목업 10x10 회전 대각선 14.1).
- 부제 칩 y +220 = 360 − 140. 칩 상단 화면 y 125, 명패 하단 화면 y 122 → 간격 3(목업 동일).
- 칩 폭 380 vs 글자: `패시브 · 영웅 HP 100% 도달` 22px 는 CJK 7자 × 22 = 154 에 영문·숫자·기호·공백이 더해진 폭이 `Subtitle` 박스 600 안에 들어가는 것을 `UiLayoutAudit` 가 이미 검사한다. 칩 안쪽 가용 폭 = 380 − 2×(링 2 + 마름모 공간 22) = 332, 마름모 바깥 끝 x = 174 + 7 = 181 ≤ 190, 마름모 안쪽 끝 x = 174 − 7 = 167 → 글자 반폭 160 이하이면 겹치지 않는다. 구현 후 최장 샘플 2종의 실측 폭이 320 이하인지 `UiLayoutAudit` 샘플로 확인한다.
- 캡션 마름모 x ±46 = 글자 "내 시너지"(14px: 4자 × 14 = 56 + 공백 4 = 반폭 30) + 간격 12 + 마름모 반폭 4 = 46. 마름모 14 의 렌더 대각선 14 × 28/36 = 10.9 (목업 8x8 회전 11.3).
- 캡션·칩 y: 캡션 −290(= 360 − 650), 칩 −326.5 (= 360 − 686.5). 불변.

### 2.2 카드 (`CardView_0/1/2`, 280x460)

카드 자식의 최종 형제 순서(뒤 → 앞):

`DecoHoverGlow` → `Bg` → `DecoFaceGradient` → `DecoFaceGloss` → `Border`(비활성) → `KindRing` → `DecoFrameGold` → `DecoBandGlow` → `DecoBand` → `DecoArtRing` → `ArtSlot` → `CardArt` → `DecoArtVignette` → `Scrim` → `NameText` → `DecoNameLine` → `DecoDescFill` → `DecoDescTop` → `DecoDescFrame` → `DescText` → `DecoCornerTL` → `DecoCornerTR` → `DecoCornerBL` → `DecoCornerBR` → `CountBadgeBg`(자식 `DecoCountGlow`) → `CountBadge` → `DecoKindLabelGlow` → `KindLabelBg` → `DecoKindLabelFrame` → `KindLabel` → `PickButton` → `DecoAxisGlow` → `AxisIcon` → `DecoAxisGem1` → `DecoAxisGem2` → `DecoAxisGem3` → `DecoDimOverlay`.

(`DecoCountGlow` 는 `CountBadgeBg` 의 자식 인덱스 0 이다. 배지 배경이 `CardView.UpdateBadge` 로 꺼지면 글로우도 함께 꺼진다.)

`PickButton` 은 형제 순서상 중간이지만 전면 알파 0 이라 보이지 않고, 뒤에 놓이는 장식 Image 는 모두 `raycastTarget` 꺼짐이라 클릭을 가로채지 않는다(§1.1).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 카드 바닥 | 기존 `Bg` 스프라이트 `Px_PanelDark` → `Px_Solid`(Simple), 색 `#10151f`. `Shadow` effectDistance (0,−10) 색 `#000000` 알파 0.44 | `Px_Solid` | 없음 |
| 바닥 그라데이션 | `DecoFaceGradient`: 전체 stretch, 그라데이션 V 색 `#1f283e` | `WorldMapGradientV` | 없음 |
| 상단 광택 | `DecoFaceGloss`: 좌상단 (8,8) 264x40, 그라데이션 V 흰색 알파 0.07 | `WorldMapGradientV` | 없음 |
| 종(種) 색 면 | 기존 `Border`: GameObject 비활성(코드가 비활성 Image 에 색을 써도 안전, §1.2) | — | 기존 `_border` |
| 종류색 안쪽 링 | 기존 `KindRing` 스프라이트 `Px_CardRing` → `CardSelectKindInset`(Sliced, fillCenter 끔). 색은 기존 `SetKind` 가 종류색 알파 1 로 칠함, 스프라이트의 베이크 알파 0.65 가 시안의 55% 혼합을 낸다 | `CardSelectKindInset`(신규) | 기존 `_kindRing` |
| 금테 이중 프레임 | `DecoFrameGold`: 전체 stretch, Sliced, fillCenter 끔(바깥 3 먹색 + 2 금 + 2 먹색 = 7px). **색은 연출이 칠함**(휴지 `#8a6a2c` → 호버 `#ffd666` → 눌림 `#fff0a8`, §4.2) | `CardSelectFrame`(신규) | **신규 `_frameGold`** |
| 상단 종류색 발광 띠 | `DecoBand`: 좌상단 (12,3) 256x5, **종류색 틴트**(알파 1). `DecoBandGlow`: 중심 (140, 5.5) 280x29, 알파 0.70, **종류색 틴트** | `CardSelectFadeLine`(신규) · `UISoftGlow` | **신규 `_kindTintImages[0]`·`[1]`** |
| 모서리 장식 4개 | `DecoCornerTL`·`TR`·`BL`·`BR`, 각 14x14, 좌상단 (5,5) / (261,5) / (5,441) / (261,441). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` | 없음 |
| 일러스트 바깥 링 | `DecoArtRing`: 좌상단 (11,11) 258x256, Sliced, fillCenter 끔, **종류색 틴트** | `CardSelectArtRing`(신규) | **신규 `_kindTintImages[2]`** |
| 일러스트 슬롯·아트 | 기존 `ArtSlot`(Px_PanelSunk) · `CardArt` 불변 | — | 기존 `_artImage` |
| 일러스트 안쪽 비네트 | `DecoArtVignette`: 좌상단 (18,18) 244x242(= `CardArt`), 색 `#000000` 알파 1.0(스프라이트 최대 알파 0.8 = 시안 `inset 0 0 22px #000c`) | `WorldMapVignette` | 없음 |
| 이름 스크림 | 기존 `Scrim` 스프라이트 `Px_Solid` → `WorldMapGradientV`, scale (1,−1)(위 투명 → 아래 진함), 색 `#05070c` 알파 0.85, 위치·크기 불변 | `WorldMapGradientV` | 없음 |
| 이름 | 기존 `NameText`(f22) 불변. 정적 색 `#e8e1cf` | — | 기존 `_nameText` |
| 이름 밑줄 | `DecoNameLine`: 좌상단 (18,254) 244x2, **종류색 틴트** | `CardSelectFadeLine` | **신규 `_kindTintImages[3]`** |
| 효과 문구 inset | `DecoDescFill`: 하단 stretch(좌우 18, 하단 78, 높이 104) `Px_Solid` 색 `#0d121c`. `DecoDescTop`: 같은 박스, 그라데이션 V 색 `#080b12`(위 `#080b12` → 아래 `#0d121c`). `DecoDescFrame`: 같은 박스, Sliced, fillCenter 끔 | `Px_Solid` · `WorldMapGradientV` · `WorldMapInset` | 없음 |
| 효과 문구 | 기존 `DescText` 의 RectTransform 불변. 정적 색 `#cfd5e2`, 정렬 가운데·가운데, TMP Margin (좌 8, 상 4, 우 8, 하 4)(시안 padding: 좌우 8, 상하 4) | — | 기존 `_descText` |
| 종류 라벨 칩 | 기존 `KindLabelBg` 색 `#ffffff` 알파 1 로 두고 스프라이트 `Px_Solid` 유지·Simple·색 `#0b0f18`(칩 면). `DecoKindLabelGlow`: `KindLabelBg` 앞 형제, 중심 = `KindLabelBg` 중심 (66,38) 104x48, 알파 0.45, **종류색 틴트**. `DecoKindLabelFrame`: `KindLabelBg` 바로 뒤 형제, (24,24) 84x28, Sliced(`CardSelectChip`), **종류색 틴트**. 글자는 기존 `SetKind` 의 종류색 | `UISoftGlow` · `CardSelectChip` | **신규 `_kindTintImages[4]`·`[5]`**, 기존 `_kindLabel` |
| N/3 금테 칩 | 기존 `CountBadgeBg` 스프라이트 `Px_Badge_Gold` → `SynergyModalBadge`(Sliced), 색 `#f7c64a`(정적; 어두운 금색 칩 `#362c10` → `#1b1608`, 테두리 `#ad8b34`, `synergy-modal-visual-polish.md` §3.2 검산). 자식 `DecoCountGlow`: 중심 (36,14) 92x48, 알파 0.33, 색 `#f7c64a`. 글자 기존 `CountBadge` 는 정적 색 `#f7c64a` | `SynergyModalBadge` · `UISoftGlow` | 기존 `_countBadgeBg`·`_countBadge` |
| 선택 버튼 글로우 | `DecoPickGlow`: `PickButton` 의 자식, `PickVisual` 바로 앞 형제(인덱스 0), 하단 중앙 앵커 피벗 (0.5,0.5) 위치 (0,42) 272x84, 알파 0.45, **종류색 틴트** | `UISoftGlow` | **신규 `_kindTintImages[6]`** |
| 선택 버튼 면 | 기존 `PickVisual` 의 스프라이트는 `CardView` 가 `_passiveButtonSprite`/`_activeButtonSprite` 로 교체한다. 프리팹에서 그 두 필드와 `PickVisual` 의 초기 스프라이트를 `Px_BtnSoul`/`Px_BtnGold` → `CardSelectPickBtnSoul`/`CardSelectPickBtnGold` 로 바꾼다. 색 흰색 | `CardSelectPickBtnSoul`·`Gold`(신규) | 기존 `_pickButtonImage`·`_passiveButtonSprite`·`_activeButtonSprite` |
| 선택 버튼 광택 | `DecoPickGloss`: `PickVisual` 의 자식, `Label` 앞(인덱스 0), 상단 stretch, 위치 (0,−4) 폭 −10 높이 10, 그라데이션 V 흰색 알파 0.31 | `WorldMapGradientV` | 없음 |
| 선택 버튼 밝기 | `DecoPickBright`: `PickVisual` 의 자식, `Label` 앞(인덱스 1), 전체 stretch, `Px_Solid` 흰색 알파 0(**휴지 기준값**, 연출이 칠함) | `Px_Solid` | **신규 `_pickBright`** |
| 선택 버튼 글자 | 기존 `PickVisual/Label`("선택" f22, 흰색) 불변 | — | 기존 |
| 축 후광 면 | 기존 `AxisIcon` Image(코드가 `_axisIconBorder.color` 로 축 색을 칠함) 스프라이트 `Px_Solid` → `SynergyModalHalo`(Simple). 자식 `Icon` 36x36 불변 | `SynergyModalHalo` | 기존 `_axisIconBorder` |
| 축 후광 발광 | `DecoAxisGlow`: `AxisIcon` 바로 앞 형제, 중심 (46,80) 72x72(후광 44 + 각 14), 알파 0.70, **축 색 틴트** | `UISoftGlow` | **신규 `_axisTintImages[0]`** |
| Tier 보석 3개 | `DecoAxisGem1`~`3`: 중심 (81,80) / (98,80) / (115,80), 각 18x18, 코드가 켜짐(= 축 색) / 꺼짐(`#3a4764`) 으로 칠함. `AxisIcon` 이 숨겨지면 코드가 함께 숨긴다 | `VillageHudGem` | **신규 `_axisGems`** |
| 어둠 오버레이 | `DecoDimOverlay`: 전체 stretch, `Px_Solid` 색 `#05070c` 알파 0(**휴지 기준값**, 연출이 칠함) | `Px_Solid` | **신규 `_dimOverlay`** |
| 호버 글로우 | `DecoHoverGlow`: 카드 중심 (140,230) 348x528(카드 + 각 34), 색 `#f7c64a`, 알파 0(**휴지 기준값**, 연출이 칠함). 형제 순서 맨 앞이라 `Bg` 가 중앙을 덮어 바깥 번짐만 보인다 | `UISoftGlow` | **신규 `_hoverGlow`** |
| 호버 보호 영역 | `DecoHoverGuard`: `PickButton` 의 자식, 하단 stretch(좌우 0), 피벗 (0.5,1), 위치 (0,0) 높이 6, `Image` 알파 0 · `raycastTarget` 켬, **기본 비활성**(§4.3) | — | **신규 `_hoverGuard`** |

검산:
- 호버 글로우 348 = 280 + 2×34, 528 = 460 + 2×34. 카드 간격 36 ≥ 번짐 34 이므로 이웃 카드와 겹치지 않는다(이웃 `Bg` 가 번짐 끝 2px 를 가린다).
- 띠: 좌우 12 → 폭 280 − 24 = 256, 글로우 폭 256 + 2×12 = 280, 높이 5 + 2×12 = 29, 중심 y = 3 + 2.5 = 5.5.
- 일러스트 링: `ArtSlot` (14,14) 252x250 의 바깥 3px → (11,11) 258x256. 링 안쪽이 프레임(안쪽 7)과 겹치지 않는다(11 > 7).
- 이름 밑줄 y = 222 + 34 − 2 = 254. 효과 문구 상단 278 − 이름 하단 256 = 22 간격.
- 종류 라벨 칩 글로우 104x48 = 84 + 2×10, 28 + 2×10. 중심 = (24 + 42, 24 + 14) = (66,38).
- `DecoCountGlow` 중심은 `CountBadgeBg`(72x28) 기준 중앙 (36,14), 92 = 72 + 2×10, 48 = 28 + 2×10(시안 `0 0 10px #f7c64a55` 알파 0x55/255 = 0.33).
- 선택 버튼 글로우 중심 y(카드 하단 기준 위로) = 14 + 56/2 = 42, 272 = 244 + 2×14, 84 = 56 + 2×14.
- 후광 중심 (24 + 22, 58 + 22) = (46,80). 보석 중심 x = 76 + j×17 + 5 = 81 / 98 / 115, 보석 렌더 대각선 18 × 28/36 = 14(시안 10x10 회전 14.1), 인접 보석 간격 17 − 14 = 3. 보석 안쪽 시작 x = 81 − 7 = 74, 후광 오른쪽 끝 24 + 44 = 68 → 간격 6. 마지막 보석 오른쪽 끝 115 + 7 = 122 < 카드 폭 280.
- 모서리 TR x = 280 − 5 − 14 = 261, BL y = 460 − 5 − 14 = 441. BL/BR(y 441~455)는 `PickVisual`(y 390~446)과 x 18~19 / 261~262 에서 5px 높이만큼 겹치며, 모서리가 `PickButton` 보다 뒤라 선택 버튼이 위에 그려진다.
- 효과 문구 inset 박스: 좌우 18, 하단 78, 높이 104 = 카드 상단에서 278~382. `DescText` 텍스트 가용 영역 = 244 − 16 = 228 x 104 − 8 = 96(시안 desc 228x96).

### 2.3 내 시너지 칩 4개 (`BuildSynergyCell` 인스턴스)

| 요소 | 방식 | 코드 참조 |
|---|---|---|
| 칩 4개 | 이번 작업에서 **수정 0건**. `BuildSynergyCell.prefab`(165x45)은 `battle-hud-visual-polish.md` §2.4 에서 이미 유리 칩·축 색 띠·아이콘·7칸 `SynergyTrack`·행/트랙 글로우로 마감되어 있고 `BindRedesign` 이 0장 흐림(`SynergyProgress.Opacity`)·Tier 1 이상 색띠·글로우 켬을 처리한다. 팝업의 4개 인스턴스는 위치·크기 오버라이드(165x45, ±88.5 / ±265.5, −326.5)만 가진다 | 기존 `_synergyChips`·`BuildSynergyCell` |
| 시안과의 차이 | 시안의 "최대 단계 금테 칩" 은 `BuildSynergyCell` 이 공유하는 HUD 표현을 바꿔야 해서 이번 범위 밖(§9) | — |

---

## 3. 신규 스프라이트 (7종) 와 재사용

### 3.1 재사용 (신규 생성 0건)

| 스프라이트 | 위치 | 이 작업에서의 용도 |
|---|---|---|
| `WorldMapFrame` | `Assets/_Lair/Art/Sprites/` | 제목 명패 금테 |
| `WorldMapGradientV` / `WorldMapGradientH` | 〃 | 명패·카드 바닥 그라데이션, 광택, 스크림, inset 윗면, 버튼 광택, 룬 라인 |
| `WorldMapInset` | 〃 | 효과 문구 inset 테두리 |
| `WorldMapCornerGold` | 〃 | 카드 모서리 장식 |
| `WorldMapVignette` | 〃 | 일러스트 비네트 |
| `VillageHudGem` | 〃 | 명패·부제·캡션 마름모, Tier 보석 |
| `SynergyModalHalo` | 〃 | 축 아이콘 원형 후광 |
| `SynergyModalBadge` | 〃 | N/3 금테 칩 |
| `UISoftGlow` | 〃 | 제목·부제·띠·라벨·배지·버튼·후광·호버 발광 |
| `Px_Solid` | `Assets/_Lair/Art/Sprites/UiDot/` | 단색 바닥·오버레이 |

위 스프라이트의 임포트 설정은 선행서 값 그대로이며 이 작업에서 수정하지 않는다. 이제 이 화면도 참조하므로 삭제·리네임 금지. `Px_PanelDark` · `Px_CardRing` · `Px_Badge_Gold` · `Px_BtnSoul` · `Px_BtnGold` 는 다른 프리팹이 쓸 수 있으므로 삭제하지 않는다.

### 3.2 신규 7종

위치는 `Assets/_Lair/Art/Sprites/` 루트, 접두어 `CardSelect`(`WorldMap*` · `VillageHud*` · `BattleHud*` · `SynergyModal*` · `BuildModal*` 관례와 동일). 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.

- 생성 방법: **일회용 에디터 스크립트** `Assets/_Lair/Editor/CardSelectPolishSpriteGenerator.cs` 가 `Texture2D` 에 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋되면 **스크립트를 삭제한다**(Rule 04 §3 — 단일 진실은 PNG).
- 임포트 공통: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center, Wrap Clamp. 9-slice border 표기는 (좌,하,우,상). 행 0 은 맨 위. 색은 sRGB `#rrggbb`, 알파는 0~255 정수. 스프라이트에 글자를 굽지 않는다.

| # | 파일명 | 크기(px) | border | Filter | 내용 (픽셀 정의) |
|---|---|---|---|---|---|
| 1 | `CardSelectFrame.png` | 16x16 | 7,7,7,7 | Point | 바깥 3px `#07090e` 불투명, 그다음 2px 흰색 `#ffffff` 불투명(틴트로 금색이 된다), 그다음 2px `#07090e` 불투명. 안쪽 중앙 영역은 완전 투명(알파 0). 모서리도 같은 규칙(정사각 링) |
| 2 | `CardSelectKindInset.png` | 20x20 | 9,9,9,9 | Point | 바깥 7px 완전 투명, 그다음 2px 흰색 `#ffffff` **알파 166**(= 0.65 × 255 반올림). 안쪽 중앙 완전 투명 |
| 3 | `CardSelectArtRing.png` | 12x12 | 3,3,3,3 | Point | 바깥 1px `#07090e` 불투명, 그다음 2px 흰색 `#ffffff` 알파 153(= 0.60 × 255). 안쪽 중앙 완전 투명 |
| 4 | `CardSelectChip.png` | 16x16 | 4,4,4,4 | Point | 바깥 2px 링 `#a6a6a6`(= 0.65 × 255 반올림) 불투명(틴트가 곱해져 종류색의 65% 밝기 = 시안의 "종류색 65% 와 검정 혼합" 테두리). 면은 위 안쪽 2행(행 2~3) `#04060a` 알파 163(= 0.64 × 255, 시안 `inset 0 2px 0 #000a`)이고 그 아래 행 4~13 은 `#0d121c`(행 4) → `#090c14`(행 13) 세로 선형 그라데이션 불투명. 아래 링은 행 14~15 |
| 5 | `CardSelectPickBtnSoul.png` | 16x32 | 3,7,3,5 | Bilinear | 불투명. 바깥 3px `#07090e`(위 3행·아래 3행·좌우 3열). 위 안쪽 2행(행 3~4)은 `#ffffff` 알파 136(시안 `inset 0 2px 0 #fff8`)을 면 위에 얹은 색. 면 세로 그라데이션: 행 5 `#76f2bf` → 행 15 `#5ef0b4`(상반부), 행 16 `#348463` → 행 24 `#2a6c51`(하반부, 시안 52%·100% 지점). 아래 안쪽 4행(행 25~28)은 같은 색에 `#000000` 알파 102(시안 `inset 0 -4px 0 #0006`)를 얹은 색. 아래 3행(행 29~31) `#07090e` |
| 6 | `CardSelectPickBtnGold.png` | 16x32 | 3,7,3,5 | Bilinear | #5 와 같은 구조. 면 그라데이션: 행 5 `#f8cf65` → 행 15 `#f7c64a`, 행 16 `#886d29` → 행 24 `#6f5921` |
| 7 | `CardSelectFadeLine.png` | 64x4 | 없음 | Bilinear | 흰색 `#ffffff`. 알파 = 열 x 에서 `round(255 × sin(π × (x + 0.5) / 64))`(양 끝 0, 가운데 255), 4행 모두 같은 값. 틴트가 곱해져 종류색이 가운데에서 짙고 양끝에서 사라진다 |

검산:
- #1: 바깥 3 + 금 2 + 안쪽 먹색 2 = 7 = border. 280x460 에서 Sliced 로 늘려도 7px 두께 유지(PPU 100 · 캔버스 참조 PPU 100). 틴트 `#8a6a2c` 가 곱해지면 먹색 `#07090e` 는 `#030304` 로 더 어두워질 뿐 변화가 없고, 금 링은 `#8a6a2c`.
- #2: 투명 7 + 링 2 = 9 = border. 링 `#5ef0b4` × 알파 0.65 가 어두운 면 `#10151f` 위에서 R = 94 × 0.65 + 16 × 0.35 = 66.7, G = 240 × 0.65 + 21 × 0.35 = 163.4, B = 180 × 0.65 + 31 × 0.35 = 127.9 → `#43a380`. 시안 `#40947c`(= 종류색 55% + `#1b2438` 45%, G 148)과 G 차이 163 − 148 = 15.
- #3: 1 + 2 = 3 = border. 시안은 면 바깥 2px 종류색 60% 와 먹색 1px 그림자.
- #4: 링 2 + 위 그림자 2 = 4 = border, 면 그라데이션이 늘어나는 중앙 행은 4~11(8행). 28px 높이 칩에서 중앙이 28 − 8 = 20px 로 늘어난다.
- #5·#6: 위 border 5 = 먹색 3 + 하이라이트 2, 아래 border 7 = 안쪽 4 + 먹색 3, 중앙 늘어나는 행 5~24(20행) 이 56px 높이 버튼에서 56 − 12 = 44px(2.2 배)로 늘어난다. 상·하반부 경계(행 15/16)가 늘어난 뒤 위에서 5 + 11 × 2.2 = 29.2px = 56px 의 52% 지점이다(시안 48%/52% 경계와 같다). 값 산식: `#5ef0b4` = (94,240,180). 85% 혼합 `.85 × c + .15 × 255` = (118,242,191) = `#76f2bf`. 55% 혼합 `.55 × c` = (52,132,99) = `#348463`, 45% = (42,108,81) = `#2a6c51`. 금 `#f7c64a` = (247,198,74): 85% 혼합 (248,207,101) = `#f8cf65`, 55% = (136,109,41) = `#886d29`, 45% = (111,89,33) = `#6f5921`.
- #7: 열 0 알파 round(255 × sin(π × 0.5 / 64)) = 6, 열 31·32 알파 round(255 × sin(π × 31.5 / 64)) = round(255 × 0.9997) = 255.

---

## 4. 호버·선택 연출 (View 표시 전용)

사용자 결정에 따라 포함한다. **입력 판정(`PickButton` 클릭 → `OnPicked` → `Close(reuse:false)`)과 선택 로직(VM)은 건드리지 않는다.** 아래는 전부 "지금 이 카드가 어떤 상태로 보이는가" 의 표시 변경이다.

### 4.1 상태와 시각 표

상태는 카드마다 독립인 3개 불리언으로 정한다: `호버`(포인터가 카드 위), `눌림`(포인터를 누른 채 카드 위), `어두움`(다른 카드가 눌린 동안). 표시값은 아래 표와 같고 상태가 바뀌면 0.16초(`_tweenSeconds` 기본값, 시안 CSS `.16s`) 선형 트윈으로 목표값에 접근한다. 시간 기준은 `Time.unscaledDeltaTime`(카드 선택 중 일시정지 `PauseService` 상태에서도 동작).

| 요소 | 휴지 | 호버 | 눌림("선택") | 어두움 |
|---|---|---|---|---|
| 카드 위치 | 레이아웃 기준 y | 위로 +6px | 위로 +6px | 휴지와 같음 |
| 카드 스케일 | 1.00 | 1.00 | 1.03 | 1.00 |
| `DecoFrameGold` 색 | `#8a6a2c` | `#ffd666` | `#fff0a8` | 휴지와 같음 |
| `DecoHoverGlow` 알파 | 0 | 0.80 | 1.00 | 0 |
| `DecoPickBright` 알파 | 0 | 0.15(시안 밝기 ×1.15) | 0.30(시안 밝기 ×1.3) | 0 |
| `DecoDimOverlay` 알파 | 0 | 0 | 0 | 0.50(시안 밝기 ×0.5) |
| `DecoHoverGuard` | 비활성 | 활성 | 활성 | 비활성 |

- 눌림이면 호버 표시값을 포함한다(눌림 ⊃ 호버). 호버와 눌림이 동시에 참이면 눌림 값을 쓴다. 어두움은 눌림·호버와 독립이며, 카드 자신이 눌림이면 어두움은 무시한다.
- 알파·색의 휴지 기준값(`DecoFrameGold` `#8a6a2c`, `DecoHoverGlow`·`DecoPickBright`·`DecoDimOverlay` 알파 0)은 **프리팹이 정하고** 코드는 `Awake` 에서 캡처한다. 호버·눌림·어두움 목표값은 코드 상수다.
- "선택" 연출은 **눌림 구간**에만 보인다. 카드 클릭은 포인터를 뗄 때 발화해 `Close(reuse:false)` 가 즉시 팝업을 파괴하므로, 눌린 채로 머무는 시간(마우스 누름~뗌, 터치 누름~뗌)이 "선택 순간"이다. 클릭 후 연출을 남기려면 닫기를 지연해야 하는데 그것은 선택 흐름·타이밍을 바꾸는 변경이라 이번 범위에 넣지 않는다(§9).
- 눌림 취소: 눌린 채 포인터가 카드 밖으로 나가면(`PointerExit`) 눌림이 해제되어 호버도 함께 해제된다. 이때 클릭은 발화하지 않는다(Unity 기본 동작).

### 4.2 어두움 전파

카드 하나가 눌림이 되면 같은 팝업의 나머지 활성 카드는 `어두움` 이 되고, 눌림이 모두 해제되면 전부 해제된다. 눌린 카드 수는 0~3 이며 한 장 이상 눌려 있고 자신이 눌림이 아닌 카드만 어두움이다(멀티터치 안전). 조정 책임은 `CardSelectionPopup`(`_slots` 를 이미 소유)이고 `CardView` 는 자기 상태 변경만 알린다.

### 4.3 카드 위치 이동과 레이아웃·히트 영역

- `CardView` 루트는 `HorizontalLayoutGroup` 이 위치를 소유한다. 그래서 위치 이동은 **y 만** 쓴다: 이동이 시작되는 순간(리프트 0 → 양수) 현재 `anchoredPosition.y` 를 기준 y 로 캡처하고, 리프트가 양수인 동안 매 프레임 `기준 y + 리프트` 를 다시 쓰며(레이아웃 재빌드가 y 를 되돌려도 다음 프레임에 복구), 0 으로 돌아오면 기준 y 로 복원한 뒤 더 쓰지 않는다. x·앵커·크기는 건드리지 않는다.
- 스케일은 `localScale` 로 쓴다(레이아웃 구동 대상이 아님. `m_ChildScaleWidth/Height` 꺼짐).
- **히트 깜빡임 방지**: 카드가 6px 올라가면 원래 카드 하단 6px 구간에서 포인터가 카드 밖이 되어 `PointerExit` → 내려옴 → `PointerEnter` 가 반복된다. 이를 막기 위해 호버·눌림 중에만 켜지는 `DecoHoverGuard`(카드 하단 바로 아래 높이 6의 투명 히트 영역, `PickButton` 의 자식)를 둔다. 올라간 상태에서 이 영역이 원래 하단 구간을 덮는다. 자식이라 `PickButton` 의 `Button` 클릭 판정에도 포함된다(눌러도 같은 카드가 선택). 휴지 상태에서는 비활성이라 카드 바깥을 눌러도 아무 일이 없다.
- 위로 올라간 카드가 이웃과 겹치지 않는다(좌우 간격 36, 상하 이동 6).

### 4.4 대안 비교

**(a) 떠오름 방식**

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | 루트 y +6 + 호버 보호 영역 | 시안과 같은 "위로 떠오름". 코드 최소 | 레이아웃 y 를 매 프레임 재기록, 보호 영역 필요 | 채택 |
| B | 스케일만 키움(1.03) | 레이아웃·히트 문제 0건 | 떠오름이 아니라 부풀림. 시안과 다름 | 기각 |
| C | 글로우·프레임 색만 변경 | 위치 이동 0건 | 눌림 구분이 약함, 사용자 결정(떠오름 포함)과 불일치 | 기각 |

**(b) 상태 표현 수단**

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | `CardView` 가 상태·단순 트윈(0.16초 선형)을 소유하고 알파·색·위치·스케일만 쓴다 | 새 의존 0건, 풀/재오픈 리셋이 한 곳, 정적 폴백(`_tweenSeconds` 0 이하이면 즉시 적용) 가능 | 코드 약간 | 채택 |
| B | `UILoopMotion` 사용 | 기존 컴포넌트 재사용 | 루프 전용(Drift/Rotate/Nudge/Pulse)이라 상태 전환 트윈이 아님 | 기각 |
| C | `Animator`/상태 머신 에셋 | 시각 편집 | 3장 × 7 요소에 애니메이션 에셋·컨트롤러 신설, 일시정지 시간 처리 별도 | 기각 |

**(c) 눌림 입력 수신**

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | `PickButton` 에 소형 릴레이 컴포넌트(`CardPointerRelay`)를 추가해 `Enter/Exit/Down/Up` 을 이벤트로 `CardView` 에 전달 | `Selectable` 이 `PointerDown` 을 먼저 소비해도 같은 오브젝트의 모든 핸들러가 호출되므로 확실. `CardView` 는 이벤트만 구독 | 신규 클래스 1개(같은 파일) | 채택 |
| B | `CardView` 가 `IPointerDownHandler` 를 직접 구현 | 클래스 추가 없음 | `Button`(`Selectable`)이 `PointerDown` 의 첫 핸들러라 부모 `CardView` 에는 전달되지 않음 | 기각 |
| C | `EventTrigger` + 인스펙터 이벤트 연결 | 클래스 추가 없음 | 프리팹 영속 호출 9개 배선, 테스트·리팩터링에 취약 | 기각 |

### 4.5 풀/재오픈 리셋·null 가드

- `CardSelectionPopup` 은 `reuse:false` 로 매번 새 인스턴스를 띄우지만 `CardView` 는 `OnEnable`·`OnDisable` 에서 상태를 휴지로 되돌린다(Rule 03 §4 State Reset): 세 불리언 해제, 트윈 진행값 0, 위치 복원(리프트가 있었으면 기준 y 로), 스케일 1, 색·알파 휴지 기준값 복원, `DecoHoverGuard` 비활성. `Bind` 시작에서도 같은 리셋을 호출한다(슬롯 재바인딩 시 잔상 방지).
- 신규 위젯 참조(`_frameGold` · `_hoverGlow` · `_pickBright` · `_dimOverlay` · `_hoverGuard` · `_pointerRelay`)가 비어 있어도 예외 없이 해당 표시만 건너뛴다. `_pointerRelay` 가 비어 있으면 호버/눌림이 아예 일어나지 않아 카드는 정적 마감 그대로 동작한다(정적 폴백). `_tweenSeconds` 가 0 이하이면 상태 전환이 즉시 적용된다.

---

## 5. SerializeField·코드 변경 (View 한정, 최소)

### 5.1 유지

- `CardView` 기존 필드 전부와 기존 공개 메서드(`Bind(card, onClick)` · `Bind(card, onClick, pickCount)` · `SetKind` · `SetAxisIcon` · `ApplyArt`) 시그니처 불변. `CardSelectionPopup` 기존 필드(`_slots` `_subtitleText` `_synergyChips`)와 `BuildSubtitle` 불변.

### 5.2 신규 `[SerializeField] private`

| 클래스 | 필드 | 타입 | 용도 | 미할당 시 |
|---|---|---|---|---|
| `CardView` | `_kindTintImages` | `Image[]` (원소 7, 순서 `DecoBand` · `DecoBandGlow` · `DecoArtRing` · `DecoNameLine` · `DecoKindLabelGlow` · `DecoKindLabelFrame` · `DecoPickGlow`) | `SetKind` 가 종류색 rgb 로 틴트(각 원소의 프리팹 알파 보존) | 건너뜀 |
| `CardView` | `_axisTintImages` | `Image[]` (원소 1: `DecoAxisGlow`) | `SetAxisIcon` 이 축 색 rgb 로 틴트(알파 보존), 아이콘이 없으면 비활성 | 건너뜀 |
| `CardView` | `_axisGems` | `Image[]` (원소 3: `DecoAxisGem1`~`3`) | Tier 보석 켜짐/꺼짐(§5.3) | 건너뜀 |
| `CardView` | `_frameGold` | `Image` (`DecoFrameGold`) | 프레임 색 연출 | 건너뜀 |
| `CardView` | `_hoverGlow` | `Image` (`DecoHoverGlow`) | 호버·눌림 발광 알파 | 건너뜀 |
| `CardView` | `_pickBright` | `Image` (`DecoPickBright`) | 버튼 밝기 오버레이 알파 | 건너뜀 |
| `CardView` | `_dimOverlay` | `Image` (`DecoDimOverlay`) | 어두움 알파 | 건너뜀 |
| `CardView` | `_hoverGuard` | `GameObject` (`DecoHoverGuard`) | 호버·눌림 중에만 활성 | 건너뜀 |
| `CardView` | `_pointerRelay` | `CardPointerRelay` (`PickButton` 의 컴포넌트) | 포인터 이벤트 수신 | 호버/눌림 없음(정적 폴백) |
| `CardView` | `_tweenSeconds` | `float` (기본 0.16) | 상태 전환 시간 | 0 이하이면 즉시 적용 |
| `CardSelectionPopup` | `_kindTintImages` | `Image[]` (원소 5, 순서 `DecoTitleGlow` · `DecoSubtitleGlow` · `DecoSubtitlePill` · `DecoSubtitleDiamondL` · `DecoSubtitleDiamondR`) | `InitUI` 가 `UiDotPalette.CardKind(IsPassive)` rgb 로 틴트(알파 보존) | 건너뜀 |

### 5.3 신규 클래스·메서드·동작

- **`CardPointerRelay`** (`MonoBehaviour`, `CardView.cs` 같은 파일 하단, `IPointerEnterHandler` · `IPointerExitHandler` · `IPointerDownHandler` · `IPointerUpHandler`): `PickButton` 오브젝트에 붙는다. 공개 이벤트 `Action<bool> HoverChanged`(Enter true / Exit false), `Action<bool> PressedChanged`(Down true / Up false / Exit false). 클릭 판정은 하지 않는다.
- **`CardView`**
  - `OnEnable` 에서 `_pointerRelay` 의 두 이벤트를 구독하고 `OnDisable` 에서 해제한다. 상태 리셋은 §4.5.
  - 신규 공개 속성 `bool IsPressed`, 신규 공개 이벤트 `Action<CardView, bool> PressChanged`(자신의 눌림 상태가 바뀔 때 발화), 신규 공개 메서드 `void SetDimmed(bool dimmed)`(§4.2 의 어두움 상태 설정).
  - `SetKind(bool isPassive)` 추가 동작: `_kindTintImages` 각 원소(null 건너뜀)를 `(종류색 rgb, 원소의 현재 알파)` 로 칠한다.
  - `SetAxisIcon(card, config)` 추가 동작: 아이콘이 있으면 `_axisTintImages` 를 `(축 색 rgb, 현재 알파)` 로 칠하고 활성, 없으면 `_axisTintImages` 와 `_axisGems` 를 비활성(`_axisIconRoot` 와 같은 조건). 축 색은 기존 `BuildSynergyPanel.AxisColor[card.Axis]`.
  - 신규 공개 메서드 `void SetAxisTier(int activeTier)`: 호출 직전에 `SetAxisIcon` 이 정한 축 색을 사용해 `_axisGems[i]`(i < 길이) 를 `i < activeTier` 이면 축 색, 아니면 `#3a4764` 로 칠한다. 아이콘이 없어서 `SetAxisIcon` 이 보석을 비활성화했으면 켜지 않는다. 배열 길이가 3 이 아니어도 범위 안에서만 처리.
  - `Update`: 진행값이 목표와 같으면 즉시 반환(상시 비용 없음). 진행 중이면 `Time.unscaledDeltaTime` 으로 전진해 §4.1 표의 값을 쓴다.
  - 새 상수는 `CardView` 안에 둔다: 리프트 6, 눌림 스케일 1.03, 호버 프레임색 `#ffd666`, 눌림 프레임색 `#fff0a8`, 호버·눌림 글로우 알파 0.80 / 1.00, 버튼 밝기 알파 0.15 / 0.30, 어두움 알파 0.50, 보석 꺼짐색 `#3a4764`. 금색은 기존 `UiDotPalette.Gold` 를 재사용할 수 있는 값(`#f7c64a`)과 별개이므로 상수로 둔다.
- **`CardSelectionPopup`**
  - `InitUI` 의 슬롯 활성 분기에서 `SetAxisIcon` 직후 `_slots[i].SetAxisTier(sa.BuildCountOf != null ? SynergyProgress.ActiveTier(sa.BuildCountOf(card.Axis)) : 0)` 에 해당하는 호출을 추가한다(`Lair.Card` 는 이미 `using` 됨). 하단 칩이 쓰는 `BuildCountOf` 와 같은 출처라 보석 켜짐 수와 칩의 Tier 가 항상 같다.
  - 슬롯마다 `PressChanged` 를 구독하는 핸들러 하나: 눌린 슬롯이 하나라도 있으면 눌리지 않은 활성 슬롯에 `SetDimmed(true)`, 없으면 전부 `SetDimmed(false)`. 팝업이 `reuse:false` 로 파괴되며 슬롯도 같이 파괴되므로 별도 해제 불필요.
  - `InitUI` 에서 `_kindTintImages` 를 `UiDotPalette.CardKind(sa.IsPassive)` rgb(알파 보존)로 칠한다. 부제 텍스트 색 칠하기는 기존 그대로.
- 코드 구조·메서드 분할의 세부는 gameplay-programmer 판단이며, 위 동작과 §8 의 검증 항목을 충족하면 된다.

**대안 비교** (Tier 보석 켜짐 수를 어디서 얻는가):

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | 팝업이 `SynergyProgress.ActiveTier(BuildCountOf(axis))` 로 계산해 `SetAxisTier` 로 전달 | 하단 칩과 같은 출처, 순수 계산 재사용, 데이터 클래스·`CardSelectionArg` 변경 0건 | `CardSelectionPopup` 에 호출 1줄 | 채택 |
| B | `CardSelectionArg` 에 카드별 Tier 공급 델리게이트 추가 | `CardView` 가 팝업을 안 거침 | `CardSelectionArg`·`BattleController` 호출부 변경 | 기각 |
| C | `CardView.SetAxisIcon` 시그니처에 장수 추가 | 메서드 하나로 끝 | 기존 공개 시그니처 변경, `SynergyIndicatorWidgetTests` 가 직접 호출 | 기각 |

---

## 6. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/CardSelectFrame.png` · `CardSelectKindInset.png` · `CardSelectArtRing.png` · `CardSelectChip.png` · `CardSelectPickBtnSoul.png` · `CardSelectPickBtnGold.png` · `CardSelectFadeLine.png` + `.meta` | 신규(§3.2) |
| `Assets/_Lair/Editor/CardSelectPolishSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 **삭제**(Rule 04 §3) |
| `Assets/_Lair/Scripts/UI/CardView.cs` | §5.2·§5.3 필드·동작 추가, `CardPointerRelay` 추가 |
| `Assets/_Lair/Scripts/UI/CardSelectionPopup.cs` | §5.3 호출·구독·틴트 추가 |
| `Assets/_Lair/Art/UI/CardSelectionPopup.prefab` | §2.1·§2.2 전부 + §5.2 신규 필드 와이어링 + `PickButton` 에 `CardPointerRelay` 컴포넌트 추가 |

- 프리팹 작업은 **기존 프리팹에 추가·교체만** 한다(새로 만들어 덮어쓰기 금지 — 기존 fileID 보존). 신규 위젯 이름은 §2 표 그대로 글자 단위로 쓴다.
- 카드 3장(`CardView_0/1/2`)에 §2.2 를 동일하게 적용한다. 3장은 중첩 프리팹이 아니라 팝업 프리팹 안의 독립 오브젝트이므로 각각 작업한다.
- 신규 텍스트 위젯 없음(문구 0건). 기존 TMP 의 정적 값(색·정렬·Margin)만 §2 표대로 설정한다. 신규 글리프 의존 없음.
- `Instantiate`/`new GameObject` 로 장식을 코드에서 만들지 않는다. 일회용 에디터 스크립트나 UnityMCP 로 편집하며, 스크립트를 쓰면 완료 직후 삭제한다(Rule 04 §3).
- **UI 배선 전 목업 승인 게이트**: `.mockups/card-selection-redesign.html` 은 이미 승인되었으므로 바로 진행한다(호버·선택 연출 포함 여부는 사용자 결정으로 이 문서에 포함).

---

## 7. 기존 테스트·프리팹 연결 검사 영향

| 대상 | 영향 | 근거 |
|---|---|---|
| `CardIllustrationTests` (`CardSelectionPopup_3슬롯_artImage_및_countBadge_와이어링_non_null`, `CardView_ApplyArt_*`) | 무수정 통과가 기준 | `_artImage`·`_countBadge` 불변, `ApplyArt` 불변 |
| `SynergyIndicatorPrefabTests` (칩 4개 배선·165x45·가로 중앙 겹침 없음, 3슬롯 `_axisIconRoot`·`_axisIconBorder`·`_axisIconImage`, 카드 아이콘 표시 크기 PNG 정수배) | 무수정 통과가 기준 | 칩 인스턴스·`AxisIcon` 계층·`Icon` 36x36 불변. `AxisIcon` 스프라이트만 `SynergyModalHalo` 로 교체 |
| `SynergyIndicatorWidgetTests` (`CardView.SetAxisIcon` 계열, 코드로 만든 `CardView` + 리플렉션 주입) | 무수정 통과가 기준 | 신규 필드가 전부 미할당일 때 예외 없이 건너뛴다(§4.5·§5.2). `SetAxisIcon` 기존 동작(아이콘 표시·테두리색) 불변 |
| `UiRedesignDataTests` (`BuildSubtitle`) | 무수정 통과 | 문구 불변 |
| `UiLayoutAudit`(에디터)의 `CardSelectionPopup/NameText`·`DescText`·`KindLabel`·`CountBadge`·`Subtitle` 샘플 | 변경 없음 | 텍스트 RectTransform 좌표·크기·폰트 크기 불변. `DescText` 의 TMP Margin(좌우 8, 상하 4)이 추가되므로 가용 폭이 244 → 228, 높이 104 → 96 으로 줄어도 최장 샘플(`_longestCardDesc`)이 자동 크기 최소 16 에서 들어가는지 이 검사로 다시 확인한다 |
| `CardFlowSmokeTest`(PlayMode) | 무수정 통과가 기준 | 팝업 표시 흐름·`OnPicked` 불변 |
| `ChannelVolumeTests` · `AudioVolumeSettings` 의 `CardSelect` 언급 | 영향 없음 | `EAudio.CardSelect` 재생 불변 |

실행 전 확인(gameplay-programmer): `CardView`/`CardSelectionPopup` 을 리플렉션으로 구성하는 테스트(`MakeCardView`)가 `OnEnable` 호출 시 `_pointerRelay` null 을 가정하는지 Grep 으로 한 번 더 확인한다(현재는 신규 필드 미할당이어도 `OnEnable` 이 예외를 내지 않는 설계 §4.5).

---

## 8. 테스트 관점

### 8.1 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| 신규 스프라이트 7종 | 파일이 존재하고 Texture Type = Sprite, border 가 §3.2 와 일치(`CardSelectFrame` 7,7,7,7 · `CardSelectKindInset` 9,9,9,9 · `CardSelectArtRing` 3,3,3,3 · `CardSelectChip` 4,4,4,4 · `CardSelectPickBtnSoul`/`Gold` (좌3,하7,우3,상5) · `CardSelectFadeLine` 없음), filterMode(Point: Frame·KindInset·ArtRing·Chip / Bilinear: PickBtn 2종·FadeLine), `CardSelectKindInset` 링 픽셀 알파 166 |
| 프리팹 연결 | `CardView` 3개 전부 신규 필드 비어 있지 않음: `_kindTintImages` 길이 7 · 순서 이름 `DecoBand`/`DecoBandGlow`/`DecoArtRing`/`DecoNameLine`/`DecoKindLabelGlow`/`DecoKindLabelFrame`/`DecoPickGlow`, `_axisTintImages` 길이 1(`DecoAxisGlow`), `_axisGems` 길이 3(`DecoAxisGem1`~`3`, 스프라이트 `VillageHudGem`), `_frameGold`·`_hoverGlow`·`_pickBright`·`_dimOverlay`·`_hoverGuard`·`_pointerRelay` 비어 있지 않음, `_pointerRelay` 가 `PickButton` 오브젝트의 컴포넌트. `CardSelectionPopup._kindTintImages` 길이 5 · 순서 이름 §5.2 |
| 스프라이트 연결 | `KindRing` = `CardSelectKindInset`, `AxisIcon` Image = `SynergyModalHalo`, `CountBadgeBg` = `SynergyModalBadge`, `DecoFrameGold` = `CardSelectFrame`, `DecoCountGlow` 가 `CountBadgeBg` 의 자식, `_passiveButtonSprite`/`_activeButtonSprite` = `CardSelectPickBtnSoul`/`Gold`, `Scrim` = `WorldMapGradientV` · scale y −1 |
| 기존 위젯 좌표 회귀 | §1.1 값과 일치: 카드 3장·`Title`·`Subtitle`·`SynergyCaption`·`CardsLayout`, 카드 내부 `ArtSlot`·`CardArt`·`Scrim`·`NameText`·`KindLabelBg`·`KindLabel`·`CountBadgeBg`·`CountBadge`·`AxisIcon`·`DescText`·`PickButton`·`PickVisual`·칩 4개의 anchor·anchoredPosition·sizeDelta |
| 형제 순서 | 카드 자식과 팝업 루트 자식의 순서가 §2.1·§2.2 와 일치. `Border` 비활성 |
| 신규 장식 레이캐스트 | 이름이 `Deco` 로 시작하는 Image 는 `raycastTarget` = false, 단 `DecoHoverGuard` 는 true 이고 기본 비활성. `PickButton` Image `raycastTarget` = true 유지 |
| 휴지 기준값 | 프리팹에서 `DecoHoverGlow`·`DecoPickBright`·`DecoDimOverlay` 알파 0, `DecoFrameGold` 색 `#8a6a2c`, `DecoHoverGuard` 비활성 |
| `CardView` 틴트 | `SetKind(true)`/`SetKind(false)` 후 `_kindTintImages` 각 원소 rgb = 종류색(`UiDotPalette.Soul`/`Gold`), 알파는 호출 전과 같음. 반대 종류로 다시 호출해도 알파 불변 |
| `CardView` 축 | `SetAxisIcon` 후 `_axisTintImages[0]` rgb = 축 색·알파 보존·활성. 아이콘이 없는 카드면 `_axisTintImages`·`_axisGems` 비활성. `SetAxisTier(0/1/2/3)`: 켜진 보석 수 = tier, 켜진 보석 rgb = 축 색, 꺼진 보석 = `#3a4764` |
| 연출 상태(`_tweenSeconds` = 0 으로 즉시 적용) | 호버 true → 리프트 +6·글로우 알파 0.80·프레임 `#ffd666`·보호 영역 활성. 눌림 true → 스케일 1.03·글로우 1.00·버튼 밝기 0.30·프레임 `#fff0a8`. 눌림 해제 → 호버 표시로 복귀(포인터가 아직 위일 때). Exit → 모든 값이 휴지 기준값으로 복귀, 위치 y 가 호버 전 값으로 복원. `SetDimmed(true)` → 어두움 알파 0.50, 카드 자신이 눌림이면 어두움 무시 |
| 풀/재오픈 리셋 | 눌림·호버·어두움 상태에서 `SetActive(false)` → `SetActive(true)` 후 모든 표시가 휴지 값, `IsPressed` = false |
| 팝업 어두움 전파 | 슬롯 3개에서 한 슬롯 눌림 → 나머지 2개 어두움, 눌린 슬롯은 어두움 아님. 해제 → 전부 해제. 슬롯 1개만 활성(후보 1장)일 때 눌림에도 예외 없음 |
| 팝업 Tier 전달 | `BuildCountOf` 공급 시 카드 축의 장수 5 → 켜진 보석 2, 7 → 3, 3 → 1. `BuildCountOf` 가 null 이면 0 |
| 선택 로직 불변 | `PickButton` 클릭 시 `OnPicked` 정확히 1회 발화(기존 동작), 눌림 연출이 클릭 발화·횟수에 영향 없음 |
| 가드 | 신규 필드를 전부 비운 `CardView`(`new GameObject` + 컴포넌트)에서 `SetKind`·`SetAxisIcon`·`SetAxisTier`·`SetDimmed`·`OnEnable`·`OnDisable` 호출이 예외 없음 |
| 문구 | `Strings_Ko` 무결성 테스트 무수정 통과(신규 문구 0건) |

### 8.2 PlayMode

신규 PlayMode 테스트 없음. 기존 `CardFlowSmokeTest` 가 그대로 통과하면 팝업 표시 흐름이 보존된 것이다.

### 8.3 수동 확인 (사용자 육안, 전투 중 팝업을 열어서)

- 패시브(녹)·액티브(금) 팝업의 부제 칩·종류 라벨 칩·띠·링·버튼이 종류색을 따라가고, 최장 부제(`패시브 · 영웅 HP 100% 도달`)가 칩 안에 들어가는지.
- 카드 3장: 축이 다른 조합에서 후광과 보석 켜짐 수가 하단 "내 시너지" 칩의 Tier 와 일치하는지(예: TANK 5장 = 보석 2, DPS 3장 = 보석 1, DEBUFF 2장 = 보석 0).
- 3픽 캡 배지(N/3)가 0 일 때 칩·글로우까지 같이 사라지는지, 1~2 일 때 금테 칩으로 보이는지.
- 마우스 호버: 카드가 6px 올라가며 금색 글로우, 카드 하단 가장자리에 포인터를 대도 깜빡이지 않는지(`DecoHoverGuard`). 눌러서 누른 동안 더 올라가고 버튼이 밝아지며 나머지 두 장이 어두워지는지. 누른 채 밖으로 나가면 모두 복구되고 선택되지 않는지.
- 카드 어디를 눌러도(일러스트·문구·모서리 장식 위) 선택되는지(장식 레이캐스트 차단 없음).
- 카드 선택 후 팝업이 즉시 닫히고 다음 번 팝업이 휴지 상태로 열리는지(잔상 없음).
- `Border` 비활성 후 카드 면이 시안의 어두운 유리 면(`#1f283e` → `#10151f` 그라데이션)으로 보이는지.

---

## 9. 시안과 다르게 포기하는 연출

| 항목 | 시안 | Unity 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·텍스트 글로우 | 제목·이름·문구·버튼 글자에 `text-shadow` | 생략 | TMP 그림자는 공용 폰트 머티리얼(SDF 에셋) 수정이 필요해 다른 화면에 영향. 가독성은 명패·스크림·inset 면이 보강 |
| 부제 글자 17px | 칩 안 17px | prefab 22px 유지 + 칩 폭 380 | "글자 크기 불변"(§1.2) |
| 선택 후 연출 유지 | 시안 "선택(가운데)" 상태가 정지 화면으로 보임 | 눌림 구간에서만 표시(§4.1) | 클릭 발화와 동시에 팝업이 닫힌다. 닫기 지연은 선택 흐름 변경 — 별도 승인 시에만 가능(사용자 선택 필요) |
| 카드 바깥 어둠 번짐 | `0 0 28px #000b` | `Shadow`(0,−10) 하나 | 번짐은 dim 이 대체 |
| 호버 종류색 70px 번짐 | 호버 시 종류색 `0 0 70px` 45% | 금색 34px 글로우 하나 | 카드 간격 36 이라 70px 번짐이 이웃 카드 면을 덮는다 |
| 어두움의 채도 저하 | `brightness(.5) saturate(.6)` | 밝기 ×0.5 에 해당하는 어둠 오버레이만(알파 0.5) | UGUI 기본 머티리얼에 채도 필터 없음. 새 머티리얼·셰이더는 범위 밖 |
| 선택 버튼 눌림 2px 하강·버튼 글로우 강화 | `.pick translateY(2px)`·`0 0 24px #f7c64a` | 버튼 밝기 오버레이(0.30)만 | 눌림 구간이 짧아 지각 차이가 작고, `PickVisual` 위치를 건드리지 않는다 |
| 이름 밑줄 글로우 | `0 0 8px` 종류색 | 밑줄 라인만 | 별도 글로우 Image 추가 대비 이득 작음 |
| 모서리 장식 크기 | 16x16 3px 금 `L` 자 + 글로우 | `WorldMapCornerGold` 14x14 | 스프라이트 재사용(0.5 배 규칙, 선행서와 동일). 글로우는 스프라이트에 포함 |
| 종류 링 정확한 색 | `#40947c` 계열 혼합 | 베이크 알파 0.65 의 틴트(패시브 `#43a380`) | 코드 변경 없이 얻는 값. G 채널 차이 163 − 148 = 15 |
| 효과 문구 상단 광택 | inset 상단 12px 흰색 광택 | 생략(`DecoDescTop` 어둠과 `WorldMapInset` 링이 대체) | 선행서와 같은 사유 |
| 시너지 칩 최대 단계 금테 | 최대 단계 칩 금테·금색 글자 | `BuildSynergyCell` 공유 프리팹 그대로 | HUD 와 공유라 변경 영향 큼. 범위 밖 |
| 카드 일러스트 | `/cardart/<Id>.png` 임시 | 실제 `CardData.CardImage` | 시안은 임시 경로 사용 |
| 축 아이콘 | 임시 SVG 28x28 | 실제 `SynergyVisualConfig` 아이콘 36x36 | §1.2 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 스프라이트 7종은 프리팹 직접 참조: `CardSelectFrame` · `CardSelectKindInset` · `CardSelectArtRing` · `CardSelectChip` · `CardSelectPickBtnSoul` · `CardSelectPickBtnGold` · `CardSelectFadeLine`. 삭제 대상 에셋 없음(기존 `Px_PanelDark` · `Px_CardRing` · `Px_Badge_Gold` · `Px_BtnSoul` · `Px_BtnGold` 는 다른 프리팹이 쓰므로 유지).
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `CardView`: 신규 `[SerializeField] private` — `_kindTintImages`(`Image[]`) · `_axisTintImages`(`Image[]`) · `_axisGems`(`Image[]`) · `_frameGold`(`Image`) · `_hoverGlow`(`Image`) · `_pickBright`(`Image`) · `_dimOverlay`(`Image`) · `_hoverGuard`(`GameObject`) · `_pointerRelay`(`CardPointerRelay`) · `_tweenSeconds`(`float`, 기본 0.16). 신규 공개: `bool IsPressed` · `event Action<CardView, bool> PressChanged` · `void SetDimmed(bool)` · `void SetAxisTier(int)`. `SetKind`·`SetAxisIcon` 추가 동작, `OnEnable`/`OnDisable`/`Update`(§5.3). 전부 null 가드.
  - `CardPointerRelay`(신규, `CardView.cs` 같은 파일): `HoverChanged`·`PressedChanged` 이벤트(§5.3).
  - `CardSelectionPopup`: 신규 `[SerializeField] private Image[] _kindTintImages`. `InitUI` 에 틴트·`SetAxisTier` 호출·`PressChanged` 구독(§5.3).
- **프리팹**: §6 표. 오브젝트·필드 이름은 테스트와 프리팹 와이어링 기준이므로 글자 그대로 쓴다. 카드: `DecoHoverGlow` · `DecoFaceGradient` · `DecoFaceGloss` · `DecoFrameGold` · `DecoBandGlow` · `DecoBand` · `DecoArtRing` · `DecoArtVignette` · `DecoNameLine` · `DecoDescFill` · `DecoDescTop` · `DecoDescFrame` · `DecoCornerTL`/`TR`/`BL`/`BR` · `DecoCountGlow` · `DecoKindLabelGlow` · `DecoKindLabelFrame` · `DecoPickGlow` · `DecoPickGloss` · `DecoPickBright` · `DecoHoverGuard` · `DecoAxisGlow` · `DecoAxisGem1`~`3` · `DecoDimOverlay`. 팝업: `DecoRuneL` · `DecoRuneR` · `DecoTitleGlow` · `DecoTitlePlaque` · `DecoPlaqueGradient` · `DecoPlaqueFrame` · `DecoPlaqueDiamondL`/`R` · `DecoSubtitleGlow` · `DecoSubtitlePill` · `DecoSubtitleDiamondL`/`R` · `DecoCaptionDiamondL`/`R`.
- **프리팹 정적 값 설정**: `Border` GameObject 비활성, `Bg` 스프라이트·색, `Scrim` 스프라이트·scale·색, `KindRing` 스프라이트, `CountBadgeBg` 스프라이트·색, `AxisIcon` 스프라이트, `KindLabelBg` 색, `Title`·`NameText`·`DescText`·`CountBadge`·`SynergyCaption` 글자 색, `DescText` 정렬·Margin(§2).
- **일회용 도구**: `CardSelectPolishSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.
- **UI 목업**: `.mockups/card-selection-redesign.html`(승인 완료, 수정 없음).
