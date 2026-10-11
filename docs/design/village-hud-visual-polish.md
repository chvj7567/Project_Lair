# 마을 HUD(VillageHud) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/village-hud-redesign.html` 의 마감(상단바 그라데이션+금 라인, 금테 메달리온 아바타, 광택 XP 바, 입체 메뉴 타일, 금테 프레임 패널, 마름모 보석 점, 광택·맥동 CTA)을 Unity `VillageHud.prefab` 에 옮긴다. 현재 마을 HUD 의 배치·좌표·문구·동작·ViewModel 은 한 글자도 바꾸지 않고, 그 위에 **장식 레이어와 스프라이트만 얹는다.**
- **검증 가설**: 레이아웃이 같은 상태에서 프레임·입체 버튼·발광만 올려도, 마을 HUD 가 침입 지도(WorldMapPopup)와 같은 톤으로 읽히고 핵심 행동(방어하기)의 활성/잠금이 한눈에 구분되는가. 정량 메트릭은 없고 사용자 육안 확인용 UI 개선이다.
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 시스템·메인 메뉴/세팅 화면 없음.
- **핵심 메커니즘**: (1) 침입 지도 폴리시(`docs/design/worldmap-popup-visual-polish.md`, 이하 "선행서")가 만든 `WorldMap*.png` 와 `UISoftGlow.png`, `UILoopMotion` 을 최대한 재사용한다. (2) 재사용이 안 되는 마감만 신규 스프라이트 9종(`VillageHud*.png`)을 절차 생성한다. (3) 움직임은 `UILoopMotion` 2곳(계정 빨간 점, 방어하기 글로우)뿐이고, 끄면 정적 포즈가 된다.

이 문서의 수치가 목업과 다르면 이 문서가 우선한다. 의도적 차이는 §9 에 모았다.

---

## 1. 불변 사항

- 모든 기존 위젯의 위치·크기·앵커·부모-자식 관계·글자 크기·글자 색·문구는 불변이다. 장식은 **새 GameObject 를 추가**하는 방식이고, 기존 위젯을 재부모하지 않는다. (`Assets/_Lair/Editor/UiLayoutAudit.cs` 가 `VillageHud/DisplayNameText`, `VillageHud/SoulText` 등 경로 문자열로 텍스트 위젯을 찾으므로 기존 경로가 바뀌면 안 된다. 또한 과거에 `VillageHud` 재직렬화 클로버 사고가 있었으므로 프리팹은 기존 파일에 추가·교체만 하고 새로 만들어 덮어쓰지 않는다.)
- 레이아웃 기준 좌표(1280x720, 좌상단 원점)는 목업 하단 규격 줄과 같다: 상단바 h88 / 메뉴 112x112 2열3행 시작 (16,104) 피치 128 / 전적 패널 256x216 (1008,104) / ◀▶ 48x68 (416,326)·(816,326) / 침입자 카드 340x324 (924,380) / 영웅 버튼 100x72 (470,632) / 방어하기 232x72 (578,632). 이 좌표는 "현재 프리팹 값"이며, 구현은 프리팹의 기존 앵커 체계로 환산한 값을 새 장식에 적용한다.
- `VillageViewModel` 변경 0건. `Strings_Ko` 신규 문구 0건, 신규 JSON 0건, 신규 SO 0건, 신규 Enum 0건, 신규 Interface 0건, 신규 Addressable 키 0건 (`VillageHud` 프리팹 키는 그대로).
- 신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작한다(§8 테스트가 이 접두어로 일괄 검사). 신규 장식 Image 는 전부 `raycastTarget = false`. 클릭 대상은 기존 `CHButton` 위젯뿐이다.
- 신규 글리프 의존 0건. ★ ☆ 는 기존 `StageThreatText` 가 이미 쓰는 문자이며 새로 추가하지 않는다. ◀ ▶ 화살표는 기존 `Arrow` 자식 위젯을 그대로 쓴다.
- 장식에 쓰는 글로우는 전부 `Assets/_Lair/Art/Sprites/UISoftGlow.png` (선행서와 동일)이다. 아래 표에서 "UISoftGlow" 로 적는다.
- "그라데이션 2장 조합"은 선행서 §2 정의와 같다: 바닥색 단색 Image 위에 세로 방향은 `WorldMapGradientV`(알파 1→0, 위→아래), 가로 방향은 `WorldMapGradientH`(알파 1→0, 좌→우)를 각 행의 지정 색으로 틴트해 겹친다.

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image** = 정적 장식(프리팹에 박고 코드 참조 없음) / **Shadow** = UGUI `Shadow` 컴포넌트 / **트윈** = `UILoopMotion`(§4). 좌표는 별도 표기가 없으면 HUD 기준(1280x720, 좌상단 원점, 목업과 동일)이고, 패널 내부 장식은 "패널 로컬"로 적는다. 색은 sRGB hex, 알파는 0~1.

### 2.1 상단바

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 바 그라데이션 | 기존 `TopBar` Image 색을 `#0f131e` 로 두고, 자식 `DecoTopGrad` 를 추가: 1280x88 stretch, 색 `#27304a`. 위 `#27304a` 에서 아래 `#0f131e` 로 이어지는 2색 그라데이션 | `WorldMapGradientV` | 없음 |
| 바 하단 잉크선 | `DecoTopInk` Image 1280x3 at y85, 색 `#07090e` (단색, 스프라이트 없음) | — | 없음 |
| 바 하단 금 라인 | `DecoTopGold` Image 1280x3 at y88, 색 `#6b5a2a` (단색). `TopBar` 아래에 붙는다 | — | 없음 |
| 바 아래 그림자 | `TopBar` Image 에 `Shadow`: effectDistance (0,−8), 색 `#000000` 알파 0.67 | — | 없음 |
| 제목 글로우 | `VillageNameText` 뒤에 `DecoTitleGlow`: UISoftGlow, 색 `#f7c64a` 알파 0.40, 크기 340x64, 제목 텍스트 중심 | `UISoftGlow` | 없음 |
| 제목 금 구분선 | `DecoTitleLineL`·`DecoTitleLineR` 2장, 각 140x2. 왼쪽 장: x500~640, y54, `WorldMapGradientH` 를 RectTransform scale (−1,1) 로 뒤집어 (알파가 중앙 쪽에서 1), 오른쪽 장: x640~780, y54, 원본. 색 `#f7c64a` 알파 0.60 | `WorldMapGradientH` | 없음 |
| 아바타 몸체 | 기존 `LordFace` Image 의 스프라이트를 `WorldMapMedalOpen` 으로 교체, 색 `#5a6a98` (회색조 라디얼 × 틴트로 중심 밝음→가장자리 어두움) | `WorldMapMedalOpen` | 기존 `LordFace` |
| 아바타 금테 | `LordFace` 바로 위(`LordFaceIcon` 아래)에 `DecoAvatarRing`: 72x72, 중심은 `LordFace` 중심, 흰색 틴트 | `VillageHudAvatarRing`(신규) | 없음 |
| 아바타 후광 | `LordFace` 바로 뒤에 `DecoAvatarGlow`: UISoftGlow 색 `#f7c64a` 알파 0.40, 크기 88x88, `LordFace` 중심 | `UISoftGlow` | 없음 |
| XP 바 배경 | 기존 `XpBarBg` 색 `#080b12` 유지 (170x16 at (96,58)) | — | 기존 |
| XP 바 fill | 기존 `XpBarFill`(Filled, `_lordXpFill`) 의 스프라이트를 `VillageHudXpFill` 로 교체, 색 흰색, Image Type·Fill Method·Fill Origin 은 기존 값 유지. 광택 하이라이트는 스프라이트에 구워져 있다 | `VillageHudXpFill`(신규) | 기존 `_lordXpFill` |
| XP 바 테두리 | `XpBarBg` 의 마지막 자식으로 `DecoXpFrame`: Sliced, stretch 170x16 (fill 위) | `VillageHudBarFrame`(신규) | 없음 |
| 소울 칩 | 기존 `SoulPill`(232x48 at (900,20)) Image 의 스프라이트를 `VillageHudChip` 으로 교체(Sliced), 색 흰색 | `VillageHudChip`(신규) | 기존 `SoulPill` |
| 소울 칩 후광 | `SoulPill` 바로 뒤에 `DecoSoulGlow`: UISoftGlow 색 `#5ef0b4` 알파 0.13, 크기 260x76, 칩 중심 | `UISoftGlow` | 없음 |
| 소울 코인 발광 | `SoulCoin` 바로 뒤에 `DecoCoinGlow`: UISoftGlow 색 `#5ef0b4` 알파 0.50, 크기 36x36, 코인 중심 | `UISoftGlow` | 없음 |
| 계정 버튼 | 기존 `CloudButton`(112x52 at (1148,18)) Image 스프라이트를 `VillageHudBtn` 으로 교체(Sliced). §2.3 공통 규칙 적용 | `VillageHudBtn`(신규) | 기존 `_cloudButton` |
| 계정 빨간 점 | 기존 `RedDot`(`_cloudConflictDot` 가 가리키는 GameObject)의 Image 스프라이트를 `WorldMapMedalBoss` 로 교체, 색 흰색. 자식 `DecoRedDotGlow`: UISoftGlow 색 `#e5484d` 알파 0.60, 크기 40x40, 점 중심, 점 뒤. `RedDot` 루트에 `UILoopMotion` Pulse(§4.2) + 중첩 `Canvas`(Override Sorting 끔) | `WorldMapMedalBoss`, `UISoftGlow` | 기존 `_cloudConflictDot` |

검산: 아바타 몸체 60x60 at (20,14) → 중심 (50,44). 금테 72x72 → 좌상단 (14,8), 우하단 (86,80) ≤ 바 높이 88. 금테 반지름: 바깥 33 − 안 30 = 3px (목업 금 링 3px 일치). 제목 구분선 양쪽 합 x500~780 = 280 (목업 `280x2` 일치). 바 잉크선 y85 + 3 = 88, 금 라인 y88 + 3 = 91.

### 2.2 좌측 메뉴 타일 6종 (`_lordButton`·`_shopButton`·`_codexButton`·`_questButton`·`_recordsButton`·`_rankingButton`)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 입체 배경 | 각 버튼 Image 의 스프라이트를 `VillageHudBtn` 으로 교체(Sliced), 색 흰색 | `VillageHudBtn`(신규) | 기존 버튼 6종 |
| 아래 그림자 | 각 버튼 Image 에 `Shadow`: effectDistance (0,−4), 색 `#000000` 알파 0.47 | — | 없음 |
| 아이콘 후광 | 각 버튼의 `Icon` 자식 바로 뒤에 `DecoIconHalo`: UISoftGlow 색 `#ffffff` 알파 0.13, 크기 64x64, 아이콘 중심 | `UISoftGlow` | 없음 |
| 라벨·아이콘 | 기존 `Icon`(48x48)·`Label` 불변. 라벨 하드 그림자는 생략(§9) | — | 기존 |

6종 모두 같은 구성이라 한 타일을 만든 뒤 나머지에 같은 변경을 적용한다. 영웅 버튼 슬롯(`_heroButton`, 라벨 "지도")은 §2.6.

### 2.3 버튼 공통 규칙 (`VillageHudBtn` 사용처 전부)

- 사용처: 메뉴 타일 6종, 계정 버튼, ◀ ▶ 두 버튼, 영웅 버튼. 방어하기만 `VillageHudCta`(§2.6).
- 기존 `Selectable` 의 transition·ColorBlock 설정은 유지한다(눌림 색 등은 기존 동작). 스프라이트가 색을 굽고 있어 Normal 색은 흰색 유지 전제다. Normal 이 흰색이 아닌 버튼이 있으면 흰색으로 맞춘다.
- 목업의 hover 떠오름(−2px)·눌림 +2px 이동은 구현하지 않는다(§9).
- ◀ ▶ 버튼만 `Selectable` transition 을 SpriteSwap 으로 설정하고 `disabledSprite` 를 `VillageHudBtnOff` 로 지정한다(§2.5).

### 2.4 우상단 전적 패널 (`RecordPanel`, 256x216 at (1008,104))

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 바닥 | 기존 `RecordPanel` Image 색 `#111726` 알파 0.93 유지. 자식 `DecoPanelGrad`(stretch, 색 `#1a2132` 알파 0.93)를 추가: 위 `#1a2132` 에서 아래 `#111726` | `WorldMapGradientV` | 기존 `RecordPanel` |
| 이중 테두리 | 자식 `DecoPanelFrame`: Sliced, fillCenter 끔, stretch 256x216. `DecoPanelGrad` 위 | `WorldMapFrame` | 없음 |
| 아래 그림자 | `RecordPanel` Image 에 `Shadow`: effectDistance (0,−6), 색 `#000000` 알파 0.40 | — | 없음 |
| 모서리 장식 4개 | `DecoCornerTL`·`TR`·`BL`·`BR`, 각 20x20. 패널 로컬 좌상단 좌표 TL (4,4) / TR (232,4) / BL (4,192) / BR (232,192). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` | 없음 |
| 항목 불릿 3개 | `DecoBullet1`~`3`, 각 14x14, 색 `#f7c64a`. 패널 로컬 중심 (26,27) / (26,91) / (26,155) | `VillageHudGem`(신규) | 없음 |
| 항목 구분선 2개 | `DecoSep1`·`DecoSep2`, 각 210x2, 색 `#f7c64a` 알파 0.60, 패널 로컬 좌상단 (22,76) / (22,140) | `WorldMapGradientH` | 없음 |

그리는 순서(아래→위): `RecordPanel` 자체 Image → `DecoPanelGrad` → `DecoPanelFrame` → 텍스트·라벨 위젯 → `DecoCorner*`·`DecoBullet*`·`DecoSep*`. 즉 `DecoPanelGrad`·`DecoPanelFrame` 은 기존 텍스트 위젯보다 앞 형제 순서에 두고, 코너·불릿·구분선은 텍스트와 겹치는 영역이 없으므로 같은 `Deco` 루트 안에 둔다. 기존 텍스트 위젯(`RecordIntruderLabel/Text`, `RecordBestLabel/Text`, `RecordWinsLabel/Text`)은 손대지 않는다.

검산: 코너 TR x = 232 + 20 = 252 ≤ 256, BL/BR y = 192 + 20 = 212 ≤ 216. 불릿 y = 16 + 11 + 64×i (i=0,1,2) = 27 / 91 / 155, 구분선 y = 16 + 60 + 64×i (i=0,1) = 76 / 140. 불릿 렌더 크기: 스프라이트 마름모 대각선 28px × (14/36) = 10.9px (목업 8px 정사각 회전의 대각선 11.3px 와 일치).

### 2.5 ◀ ▶ 화살표 (`_stagePrevButton`·`_stageNextButton`, 48x68)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 입체 배경 | 두 버튼 Image 스프라이트를 `VillageHudBtn` 으로 교체(Sliced), `Shadow` (0,−4) 알파 0.47 | `VillageHudBtn` | 기존 버튼 2종 |
| 화살표 | 기존 `Arrow` 자식 위젯 유지, 색을 `#f7c64a` 로 지정 | — | 기존 |
| 비활성 | `Selectable` transition SpriteSwap, `disabledSprite` = `VillageHudBtnOff`. `CHButton.Interactable` 이 꺼지면(첫/마지막 스테이지) 어두운 평판으로 바뀐다 | `VillageHudBtnOff`(신규) | 기존 `_stagePrevButton`·`_stageNextButton` |

목업의 화살표 발광(`0 0 14px`)과 비활성 불투명도 0.35 는 구현하지 않는다(§9).

### 2.6 침입자 카드 (`StagePanel`, 340x324 at (924,380))

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 바닥·프레임·그림자·코너 | §2.4 와 같은 구성을 카드 크기 340x324 에 적용. `DecoPanelGrad`·`DecoPanelFrame`·Shadow (0,−6) 알파 0.40, 코너 20x20 좌표 TL (4,4) / TR (316,4) / BL (4,300) / BR (316,300) | `WorldMapGradientV`, `WorldMapFrame`, `WorldMapCornerGold` | 기존 `StagePanel` |
| STAGE 글로우 | `StageIndicatorText` 뒤에 `DecoStageGlow`: UISoftGlow 색 `#f7c64a` 알파 0.40, 크기 220x48, 텍스트 중심 | `UISoftGlow` | 없음 |
| ★ 발광 | `StageThreatText` 뒤에 `DecoThreatGlow`: UISoftGlow 색 `#f7c64a` 알파 0.30, 크기 180x28, 텍스트 중심 | `UISoftGlow` | 없음 |
| 금 구분선 | `DecoNameDivL`·`DecoNameDivR`, 각 110x2, 카드 로컬 좌상단 (60,68)·(170,68). L 은 scale (−1,1), 색 `#f7c64a` | `WorldMapGradientH` | 없음 |
| 5칸 점 → 마름모 보석 | 기존 `StageDot1`~`5` (`_stageDots`) Image 의 스프라이트를 `VillageHudGem` 으로 교체, 크기 11x11 → 24x24, 앵커 위치(−38/−19/0/+19/+38, 0)는 불변, 색은 프리팹 값을 흰색에서 `UiDotPalette.Stone4` 값(`#48546e`)으로 유지. 켜짐/꺼짐 색은 기존 코드(`UiDotPalette.Gold` / `UiDotPalette.Stone4`)가 계속 틴트한다 | `VillageHudGem`(신규) | 기존 `_stageDots` |
| 현상수배서 그림자 | `WantedPosterBorder` Image 에 `Shadow` (3,−3) 색 `#04060a` 알파 1.00, `DecoPosterGlow`: `WantedPosterBorder` 뒤에 UISoftGlow 색 `#000000` 알파 0.60, 크기 = 수배서 크기 + 36 | `UISoftGlow` | 없음 |
| 종이 안쪽 음영 | `WantedPosterPaper` 위, `WantedPosterAged` 아래에 `DecoPaperInset`: Sliced, stretch(종이와 같은 크기) | `VillageHudPaperInset`(신규) | 없음 |
| 사진 틀 음영 | `WantedPosterPhotoBed` 위에 `DecoPhotoShade`: stretch, 색 `#000000` 알파 0.60. `WorldMapVignette` 는 알파가 내장돼 있어 Image 알파 1 이 아니라 0.60 으로 낮춘다 | `WorldMapVignette` | 없음 |
| 못 4개 | 기존 `WantedPosterNail` 4개 Image 스프라이트를 `WorldMapMedalOpen` 으로 교체, 색 `#8a98bc` | `WorldMapMedalOpen` | 기존 |
| 초상 발광 | `StageHeroPortrait` 뒤에 `DecoPortraitGlow`: UISoftGlow 색 `#e5484d` 알파 0.53, 크기 = 초상 크기 + 40, 초상 중심 | `UISoftGlow` | 없음 |
| 잠금 오버레이 | `StageLockDim`(`_stageLockDim`) 프리팹 색 알파 0.55 → 0.72 (목업 `#000000b8` = 184/255 = 0.72). 오버레이 구조·텍스트 불변 | — | 기존 `_stageLockDim` |

보석 점 렌더 검산: 스프라이트 36px 의 마름모 대각선 28px, 렌더 24px 이면 대각선 28 × 24/36 = 18.7px, 점 피치 19px → 이웃 보석 꼭짓점 간격 0.3px (목업: 정사각 13px 회전 대각선 18.4px, 피치 13 + 6 = 19px 와 일치).

### 2.7 하단 영웅·방어하기

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 영웅 버튼 | 기존 `HeroButton`(`_heroButton`) 의 Image 스프라이트를 `VillageHudBtn`(Sliced), `Shadow` (0,−4) 알파 0.47. 이 슬롯은 §2.2 의 타일 6종이 아니라 하단 영웅 버튼이다 | `VillageHudBtn` | 기존 |
| 방어하기 입체 | 기존 `SortieButton`(`_sortieButton`, 232x72 at (578,632)) Image 의 스프라이트를 `VillageHudCta`(Sliced), `Shadow` (0,−4) 알파 0.47 | `VillageHudCta`(신규) | 기존 |
| 방어하기 잠금 | `SortieButton` 의 `Selectable` transition SpriteSwap, `disabledSprite` = `VillageHudBtnOff`. `_sortieButton.Interactable == false` 가 곧 잠금 | `VillageHudBtnOff` | 기존 |
| 붉은 글로우(맥동) | 신규 루트 `DecoSortieGlow`(`_sortieGlow`): `SortieButton` 바로 뒤. 자식 없이 자기 자신이 Image: UISoftGlow, 색 `#e5484d` 알파 0.47, 크기 296x136 (버튼 232x72 + 각 32), 버튼 중심. `UILoopMotion` Pulse(§4.2) + 중첩 `Canvas` | `UISoftGlow` | **신규 `_sortieGlow`** |
| 상단 광택 | 신규 루트 `DecoSortieGloss`(`_sortieGloss`): `SortieButton` 의 자식(버튼 위), 212x12 at 버튼 로컬 (10,6), 흰색 알파 0.27 (0x44 = 68/255 = 0.27), 세로 그라데이션 | `WorldMapGradientV` | **신규 `_sortieGloss`** |
| 잠금 시 | `_sortieGlow`·`_sortieGloss` 비활성(§6.3). 잠금 상태에서 글자색은 변경하지 않는다(§9) | — | `_sortieGlow`, `_sortieGloss` |

---

## 3. 신규 스프라이트

### 3.1 재사용 목록 (신규 생성 0장)

| 재사용 에셋 | 이 문서에서의 용도 |
|---|---|
| `UISoftGlow` | 모든 글로우(제목·아바타·소울·메뉴 후광·STAGE·★·수배서·초상·방어하기·빨간 점) |
| `WorldMapGradientV` | 상단바 바닥, 패널 바닥, 방어하기 광택 |
| `WorldMapGradientH` | 제목 금 구분선, 전적 구분선, 카드 금 구분선 |
| `WorldMapFrame` | 전적 패널·침입자 카드 이중 금테 프레임 |
| `WorldMapCornerGold` | 전적 패널·침입자 카드 모서리 4개씩 |
| `WorldMapMedalOpen` | 아바타 몸체, 수배서 못 |
| `WorldMapMedalBoss` | 계정 빨간 점 |
| `WorldMapVignette` | 수배서 사진 틀 음영 |

위 8개의 임포트 설정·크기는 선행서 §3 값 그대로이며 이 작업에서 수정하지 않는다. 이 파일들은 이제 침입 지도와 마을 HUD 두 화면이 참조한다. 삭제·리네임 금지(§7).

### 3.2 공통 규칙 (신규 9종)

- 위치: `Assets/_Lair/Art/Sprites/` 루트, 접두어 `VillageHud`. 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.
- 생성: **일회용 에디터 스크립트** `Assets/_Lair/Editor/VillageHudSpriteGenerator.cs` 가 `Texture2D` 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋된 뒤 스크립트를 삭제한다(Rule 04 §3 — 단일 진실은 PNG).
- 임포트 공통: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center, Wrap Clamp.
- Filter Mode: `VillageHudBtn`·`VillageHudBtnOff`·`VillageHudCta`·`VillageHudChip`·`VillageHudBarFrame`·`VillageHudPaperInset` 는 Point(잉크 테두리를 또렷하게). `VillageHudAvatarRing`·`VillageHudXpFill`·`VillageHudGem` 은 Bilinear.
- 곡선·사선 가장자리는 4x4 슈퍼샘플링으로 커버리지를 구해 알파에 반영한다.
- 9-slice border 표기는 (좌,하,우,상). 스프라이트에 글자는 굽지 않는다.

### 3.3 스프라이트 표

| # | 파일명 | 크기(px) | 9-slice border | 내용 (픽셀 정의) |
|---|---|---|---|---|
| 1 | `VillageHudAvatarRing.png` | 72x72 | 없음 | 중심 (36,36). 반지름 30 이상 33 미만은 `#b9832a` 불투명. 반지름 33 이상 36 미만은 `#f7c64a` 알파 102→0 선형 감쇠. 그 외 투명 |
| 2 | `VillageHudXpFill.png` | 4x16 | 없음 | 세로 그라데이션: 행 0 `#ffe58a`, 행 9 `#e0a21c`, 행 15 `#b9832a`, 구간 선형(위가 행 0). 행 3~5 위에 흰색 알파 102 덧칠 (목업 `top:1px, height:3px` 를 3px 잉크 테두리 안쪽 기준으로 환산) |
| 3 | `VillageHudBarFrame.png` | 16x16 | 3,3,3,3 | 링만(중앙 알파 0). 바깥 1px `#3a4764`, 안쪽 2px `#07090e` (합 3px) |
| 4 | `VillageHudChip.png` | 16x16 | 5,5,5,5 | 바깥 3px `#07090e`, 그 안쪽 2px `#1f9e74` 알파 102. 내부 중앙은 세로 그라데이션 `#0b1a17`(위)→`#07100e`(아래) 불투명 |
| 5 | `VillageHudBtn.png` | 24x24 | 6,7,6,6 | 바깥 3px `#07090e`. 안쪽 위 2행 `#7080aa`. 그 아래 12행(행 5~16) 세로 그라데이션 `#3a4664`→`#262e48`. 그 아래 4행(행 17~20) `#11172a`. 마지막 3행 `#07090e` |
| 6 | `VillageHudBtnOff.png` | 24x24 | 6,7,6,6 | 바깥 3px `#07090e`. 행 3~16 `#232834`. 행 17~20 `#151922`. 마지막 3행 `#07090e` |
| 7 | `VillageHudCta.png` | 24x24 | 6,8,6,6 | 바깥 3px `#07090e`. 안쪽 위 2행(행 3~4) `#ffa0a4`. 행 5~10 세로 그라데이션 `#e0525a`→`#b8323a`. 행 11~15 세로 그라데이션 `#8f2029`→`#a62a33` (행 10→11 에서 색이 끊긴다). 행 16~20 `#5a0f17`. 마지막 3행 `#07090e` |
| 8 | `VillageHudGem.png` | 36x36 | 없음 | 중심 (18,18). 마름모 영역 \|dx\|+\|dy\| ≤ 14. 가장자리 1px `#000000`. 내부 대각선 그라데이션 흰색 명도 1.00(좌상)→0.78(우하). 마름모 밖 4px 에 흰색 알파 90→0 선형 글로우(틴트가 곱해져 켜짐은 금빛, 꺼짐은 어두운 청회색 후광이 된다) |
| 9 | `VillageHudPaperInset.png` | 32x32 | 12,12,12,12 | 링만(중앙 알파 0). 바깥 2px `#b39a66`, 그 안쪽 10px 은 `#7a5a2a` 알파 102→0 선형 감쇠 |

검산:
- 버튼 #5: 행 수 = 3(잉크) + 2(하이라이트) + 12(그라데이션) + 4(아래 음영) + 3(잉크) = 24. border 상 6 = 잉크 3 + 하이라이트 2 + 그라데이션 첫 행 1, border 하 7 = 음영 4 + 잉크 3 → 늘어나는 영역은 행 6~16 (11행)의 그라데이션뿐이다.
- #6: 3 + 14 + 4 + 3 = 24. border 하 7 = 4 + 3.
- CTA #7: 3 + 2 + 11(행 5~15) + 5 + 3 = 24. border 상 6 = 3 + 2 + 1, border 하 8 = 5 + 3. 늘어나는 영역 행 6~15 는 위 5행(행 6~10)·아래 5행(행 11~15)으로 정확히 반분 → 색 단차는 늘어난 높이의 50% 지점 (목업 51%).
- #4: 3 + 2 = 5 = border.
- #8: 마름모 대각선 = 2 × 14 = 28, 글로우 포함 폭 = 28 + 2 × 4 = 36.

---

## 4. 애니메이션 — `UILoopMotion` 재사용 + 정적 폴백

`Assets/_Lair/Scripts/UI/UILoopMotion.cs` 는 **수정하지 않고** 그대로 쓴다(Pulse 의 `_graphic`, `_alphaFrom/To`, `_scaleFrom/To`, `_period`; `OnDisable` 에서 기준 포즈 복원).

### 4.1 움직임 2종

| # | 대상 | 종류 | 파라미터 | 정적 폴백(끄면) |
|---|---|---|---|---|
| 1 | `RedDot` 루트 (`_cloudConflictDot`) | `Pulse` | 스케일 1.00 ↔ 1.18, 알파 변경 없음(`_graphic` 비움), `_period` 1.4초. 목업 `pls 1.4s` 와 같다(`PulseFactor` 는 t=0 에서 0, 주기 절반에서 1, 주기 끝에서 0) | 스케일 1.00 고정 |
| 2 | `DecoSortieGlow` (`_sortieGlow`) | `Pulse` | `_graphic` = 자기 Image, 알파 0.47 ↔ 0.80, 스케일 1.00 ↔ 1.00, `_period` 2.2초 (목업 `glow 2.2s`, 글로우 강도 `#e5484d77`↔`#e5484dcc` = 0.47↔0.80) | 알파 0.47 고정(시작 포즈) |

- `RedDot` 은 코드가 `SetActive` 로 켜고 끈다. `UILoopMotion` 은 `OnEnable` 에서 기준 포즈를 잡고 `OnDisable` 에서 복원하므로 추가 코드가 필요 없다. `DecoSortieGlow` 도 `_sortieGlow.SetActive` 에 따라 같은 방식으로 켜지고 꺼진다.
- 동시 동작 상한: 최대 2개. 두 대상 모두 중첩 `Canvas`(Override Sorting 끔)를 달아, 알파·스케일 변화가 HUD 전체 메시를 재빌드하지 않게 격리한다. 글로우·점은 `raycastTarget` 이 꺼져 있으므로 `GraphicRaycaster` 는 붙이지 않는다.

### 4.2 정적 폴백 기준

기본은 2종 전부 **동작**이다. 최저 사양 대상 기기에서 마을 HUD 를 연 상태의 프레임 비용이 연 직후 대비 평균 +0.5ms 를 넘으면 두 `UILoopMotion` 컴포넌트의 Enabled 를 프리팹에서 해제한다(측정: Unity Profiler, 사용자 확인). 최저 사양 대상 기기는 **사용자 선택 필요** — 현재 프로젝트 메타에 빌드 타깃 기기 기준이 없다. 폴백 후에도 정보 손실은 없다: 충돌 배지는 점의 존재로, 잠금은 회색 버튼 스프라이트로 구분되며 움직임은 가산 연출이다.

---

## 5. 색 연동 확인 (`UiDotPalette`)

- 코드의 `RefreshStageCard` 는 `_stageDots[i].color = i < stageIndex ? UiDotPalette.Gold : UiDotPalette.Stone4` 로 틴트한다. 이 로직·팔레트는 **불변**이다(`UiDotPalette` 는 토스트·카드 등 타 화면도 쓰므로 값 변경 금지).
- `UiDotPalette.Gold` = `#F7C64A` 는 목업 켜진 점 금색(`#ffe58a`→`#e0a21c` 그라데이션의 중간 톤)과 같은 계열이다. 보석 스프라이트의 명도 그라데이션(1.00→0.78)이 곱해져 금색 안에서 입체감이 생긴다.
- `UiDotPalette.Stone4` = `#48546E` 는 목업 꺼진 점 `#0a0d14` 보다 밝다. 보석은 검정 1px 테두리 + 청회색 몸체가 되어 "소등 보석"으로 읽힌다. 목업과 정확히 같은 암색은 팔레트를 바꿔야 해서 채택하지 않는다(§9).
- 켜짐 발광(`0 0 8px #f7c64acc`)은 스프라이트에 구운 흰색 글로우가 틴트와 곱해져 켜진 점은 금빛, 꺼진 점은 `#48546e` 의 옅은 후광으로 나타난다.
- 프리팹의 점 초기 색: 5개 모두 현재 프리팹 값(`StageDot1`의 `m_Color` r 0.282 g 0.329 b 0.431 = `#48546E` = Stone4)을 유지한다. 코드가 `RefreshStage` 에서 즉시 덮어쓴다.

---

## 6. SerializeField 변경 목록

### 6.1 유지 (이름·타입·의미 불변)

`VillageHud` 의 기존 `[SerializeField]` 전부: `_displayNameText` `_soulText` `_lordLevelText` `_lordXpFill` `_shopButton` `_codexButton` `_recordsButton` `_heroButton` `_questButton` `_lordButton` `_rankingButton` `_cloudButton` `_cloudConflictDot` `_sortieButton` `_stagePrevButton` `_stageNextButton` `_stageIndicatorText` `_stageThreatText` `_stageLockOverlay` `_stageLockDim` `_stageLockLabel` `_stageLockHintText` `_stageHeroPortrait` `_stageDots`(`Image[]`, 길이 5 유지) `_recordIntruderText` `_recordBestText` `_recordWinsText`. `_stageDots` 는 **스프라이트만 `VillageHudGem` 으로 교체**하고 타입·길이·순서는 그대로다.

### 6.2 신규 `[SerializeField] private` (2개, 모두 `VillageHud`)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_sortieGlow` | `GameObject` | 방어하기 붉은 글로우 루트(`DecoSortieGlow`). 잠금이면 비활성 |
| `_sortieGloss` | `GameObject` | 방어하기 상단 광택 루트(`DecoSortieGloss`). 잠금이면 비활성 |

### 6.3 신규 코드 동작 (View 만, 로직 없음)

- `VillageHud.RefreshStage()` 에 두 줄 추가: `unlocked`(= `_vm.IsCurrentStageUnlocked`, 이미 계산된 지역 변수)로 `_sortieGlow`·`_sortieGloss` 의 `SetActive`. 각각 null 이면 건너뛴다(기존 필드와 같은 방식; `VillageHudEscCloseTests` 가 필드가 비어 있는 빈 컴포넌트를 쓰므로 null 가드가 필수다).
- `VillageHud.cs` 44행 주석의 `(α0.55)` 는 `(α0.72)` 로 바꾼다(§2.6 잠금 오버레이 값 변경에 따른 주석 정정, 동작 변경 아님).
- ViewModel 은 읽지 않는 값이 없다. 신규 VM 필드 0건. 위 외의 `.cs` 변경 0건.

---

## 7. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/VillageHud*.png` 9종 + `.meta` | 신규(§3.3) |
| `Assets/_Lair/Editor/VillageHudSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 삭제(Rule 04 §3) |
| `Assets/_Lair/Art/UI/VillageHud.prefab` | §2 전체. 기존 파일에 추가·교체만, `_sortieGlow`·`_sortieGloss` 와이어링 |
| `Assets/_Lair/Scripts/UI/Village/VillageHud.cs` | §6.2·6.3 |
| `WorldMap*.png`, `UISoftGlow.png`, `UILoopMotion.cs` | 변경 없음. **이제 마을 HUD 도 참조하므로 삭제·리네임 금지** |

- 모든 신규 장식은 프리팹에 정적으로 배치한다. 코드에서 `Instantiate`·`new GameObject` 로 만들지 않는다.
- 신규 텍스트 위젯 0건(문구 0건). 기존 TMP 에는 이미 `CHText` 가 붙어 있다.
- UI 배선(UnityMCP·빌더 실행)은 목업(`.mockups/village-hud-redesign.html`) 승인이 이미 끝났으므로 바로 진행한다. 프리팹 편집용 에디터 스크립트를 쓰면 완료 직후 삭제한다.

---

## 8. 테스트 관점

### 8.1 영향받는 기존 테스트

| 테스트 | 영향 |
|---|---|
| `VillageHudEscCloseTests` | 빈 GameObject 에 `VillageHud` 를 AddComponent 하는 방식이라 신규 필드가 비어 있어도 통과해야 한다 → `RefreshStage` 의 null 가드가 이를 보장. 무수정 통과가 기준 |
| `VillageSmokePlayTests` (PlayMode) | 씬 로드→`VillageHud` 표시 1사이클. 무수정 통과가 기준 |
| `CloudPopupWiringTests` | `CloudPopup.prefab` 만 검사. 영향 없음 |
| `UiLayoutAudit`(에디터 툴) | 텍스트 위젯 경로 `VillageHud/…Text` 가 §1 대로 불변이므로 영향 없음 |
| `WorldMap*` 관련 테스트 | `WorldMap*.png` 임포트 설정·존재를 검사한다. 이 작업이 그 파일들을 수정하지 않으므로 무수정 통과가 기준 |
| `UiRedesignDataTests` | `UiDotPalette` 값을 검사한다. 팔레트 불변이므로 통과 |

현재 `VillageHud.prefab` 의 필드 연결을 검사하는 테스트는 **없다**. 따라서 아래 신규 테스트가 필요하다.

### 8.2 신규 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| 스프라이트 임포트 | `VillageHud*.png` 9종 존재. Texture Type = Sprite. 9-slice border 가 §3.3 표와 일치(#3·#4·#5·#6·#7·#9). #3·#4·#5·#6·#7·#9 는 filterMode Point, #1·#2·#8 은 Bilinear, 전부 Wrap Clamp |
| 일회용 스크립트 삭제 | `Assets/_Lair/Editor/VillageHudSpriteGenerator.cs` 가 존재하지 않음 |
| `VillageHud.prefab` 기존 연결 | §6.1 의 기존 필드 전부 비어 있지 않음(`_stageDots` 는 길이 5이고 원소 5개 전부 비어 있지 않음). 재직렬화 클로버 가드 |
| `VillageHud.prefab` 신규 연결 | `_sortieGlow`·`_sortieGloss` 가 비어 있지 않고 이름이 각각 `DecoSortieGlow`·`DecoSortieGloss` |
| 보석 점 스프라이트 | `_stageDots` 5개의 `sprite` 가 모두 `VillageHudGem` |
| 비활성 스프라이트 | `_sortieButton`·`_stagePrevButton`·`_stageNextButton` 의 `Selectable.spriteState.disabledSprite` 가 `VillageHudBtnOff`, transition = SpriteSwap |
| 장식 레이캐스트 | 프리팹 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false` |
| 움직임 개수 | 프리팹 안 `UILoopMotion` 정확히 2개: `RedDot`(Pulse, 주기 1.4, 스케일 1.00→1.18)·`DecoSortieGlow`(Pulse, 주기 2.2, 알파 0.47→0.80) |
| 잠금 오버레이 | `_stageLockDim.color.a` 가 0.72 (허용 오차 0.005) |
| `RefreshStage` 글로우 토글 | VM 의 `IsCurrentStageUnlocked` 가 참/거짓일 때 `_sortieGlow`·`_sortieGloss` 의 `activeSelf` 가 각각 참/거짓. 해당 필드가 비어 있어도 예외 없음 |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과 |

`UILoopMotion` 곡선 순수 함수 테스트는 선행서 §8.1 에서 이미 커버되므로 중복 작성하지 않는다.

### 8.3 PlayMode 불요

신규 로직이 활성 토글 두 줄뿐이며 EditMode 로 덮인다.

### 8.4 수동 확인 (사용자 육안, 마을 씬에서)

- 상단바 그라데이션·금 라인, 아바타 금테, XP 바 광택이 `LordProgress` 0 / 0.62 / 1 에서 fill 길이대로 보이는지(fill 이 테두리 안쪽으로 들어가는지).
- 메뉴 타일 6종·계정·영웅 버튼의 눌림 색이 기존대로 동작하는지, 모든 버튼 클릭이 열리는지(장식이 클릭을 막지 않는지).
- 스테이지 1~5 로 ◀▶ 이동: 보석 점이 스테이지까지 금색·나머지 청회색, 첫/마지막 스테이지에서 해당 화살표가 어두운 평판으로 바뀌는지, 잠금 스테이지에서 오버레이(α0.72)가 보석·수배서를 덮고 방어하기가 회색 평판이 되며 글로우·광택이 사라지는지.
- 클라우드 충돌 배지가 켜질 때 빨간 점이 맥동하고 꺼지면 보이지 않는지.
- §4.2 의 프레임 비용 확인 후 폴백 여부 결정.

---

## 9. 목업 대비 의도적 차이

| 항목 | 목업 | Unity 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·텍스트 글로우(`text-shadow`) | 닉네임·소울·타일 라벨 등 2px 검정, 소울 녹색 글로우 | 생략. 제목·STAGE·★ 는 뒤 글로우 Image 로 대체 | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 타 화면에 영향(선행서 §9 와 동일 판단) |
| hover 떠오름·눌림 이동 | 타일 hover −2px, 눌림 +2px | 기존 Selectable 눌림 색만 | 모바일 중심 터치 UI, 지시 목록에 없음 |
| ◀▶ 화살표 발광·비활성 불투명도 0.35 | 있음 | 없음. 비활성은 `VillageHudBtnOff` 평판 스프라이트 | 기존 `Arrow` 자식이 Selectable 전환 대상이 아니라 불투명도를 따라가지 않으며, 코드 변경 없이는 연동 불가 |
| 방어하기 잠금 글자색 | `#6c7385` | 변경 없음(기존 글자색) | 글자색 전환에 신규 CHText 참조와 코드가 필요해 YAGNI |
| 꺼진 점 색 | `#0a0d14` | `UiDotPalette.Stone4` (`#48546e`) | 팔레트는 타 화면 공용(§5) |
| 아바타 몸체 | 방사형 `#4a5a86`→`#1b2236` | `WorldMapMedalOpen` 회색조 라디얼 × 틴트 `#5a6a98` | 신규 스프라이트 대신 재사용, 가장자리가 목업보다 약간 밝음 |
| XP 바 fill 발광(`0 0 8px #f7c64aaa`) | 있음 | 없음 | 바 테두리 안쪽만 보이게 하는 구조라 외곽 발광이 잘림. 광택 하이라이트로 대체 |
| XP 바 안쪽 높이 | 12px (테두리 2px) | 10px (`VillageHudBarFrame` 3px 테두리) | 바 크기 170x16 불변이 우선 |
| 소울 텍스트 글로우 | `0 0 10px #5ef0b455` | 칩·코인 글로우 Image 로 대체 | 위 텍스트 그림자와 같은 사유 |
| 수배서 종이 줄무늬 | `repeating-linear-gradient` | 기존 `WantedPosterAged` 유지 | 기존 에셋이 이미 같은 역할 |
| 초상 발광 | 아이콘 `drop-shadow` | 뒤 UISoftGlow Image | 초상이 없는 경우(`PortraitOf` 가 null)에도 글로우는 남지만, 실제 게임은 초상을 항상 공급한다 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트 9종은 프리팹 직접 참조. 삭제 대상 없음.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `VillageHud`: 신규 필드 `_sortieGlow`·`_sortieGloss`(`GameObject`), `RefreshStage()` 에 활성 토글 추가 — §6.2·6.3. 44행 주석 α 값 정정.
  - 그 외 `.cs` 변경 0건 (`UILoopMotion`·`UiDotPalette`·`VillageViewModel` 수정 금지).
- **프리팹**: §7 표. 필드명·장식 GameObject 이름(`Deco*`, `DecoSortieGlow`, `DecoSortieGloss`)은 테스트 기준이므로 글자 그대로 쓴다. 장식 좌표·크기는 §2 표 그대로이며, 프리팹 앵커 체계 환산은 gameplay-programmer 가 하되 기존 위젯은 움직이지 않는다.
- **일회용 도구**: `VillageHudSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.
- **구현 시 현황 확인 항목**(기획서가 프리팹 값을 직접 보지 못한 부분): ① 각 버튼의 `Selectable` transition 종류와 Normal 색(§2.3), ② `RedDot` 이 `_cloudConflictDot` 의 대상 GameObject 인지, ③ `TopBar`·`RecordPanel`·`StagePanel` 의 현재 Image 색. 다르면 §2 표의 목표 색으로 맞추되 위치·크기는 바꾸지 않는다.
