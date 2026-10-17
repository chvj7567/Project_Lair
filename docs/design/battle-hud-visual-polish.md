# 배틀 HUD(BattleHud) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/battle-hud-redesign.html` 의 마감(금테 이중 프레임·유리 광택 패널, 보스 바 금테+광택 fill+눈금 마름모 보석, 30초 이하 붉은 맥동, 5초 이하 금색 맥동, 시너지 행 색 띠·발광, 빌드 슬롯 종류색 테두리, 스포너 셀 프레임·Lv 배지·광택 진행바)를 Unity `BattleHud.prefab` 과 그 하위 프리팹에 옮긴다. 배치·좌표·문구·동작·ViewModel 은 한 글자도 바꾸지 않고, 그 위에 **장식 레이어와 스프라이트만 얹는다.**
- **검증 가설**: 레이아웃이 같은 상태에서 프레임·발광·상태별 맥동만 올려도, 배틀 HUD 가 마을 HUD(`village-hud-visual-polish.md`)·침입 지도(`worldmap-popup-visual-polish.md`)와 같은 톤으로 읽히고, "30초 이하 임박 / 5초 후 액티브 카드 / 보스 30% 이하"가 한눈에 구분되는가. 정량 메트릭은 없고 사용자 육안 확인용 UI 개선이다. 중앙 경기장 영역(x180~1100, y100~640)은 가리지 않는다.
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 시스템·메인 메뉴/세팅 화면 없음.
- **핵심 메커니즘**: (1) 선행 두 기획서가 만든 `WorldMap*.png`·`UISoftGlow.png`·`VillageHudGem.png`·`VillageHudBtn.png` 와 `UILoopMotion` 을 최대한 재사용한다. (2) 재사용이 안 되는 마감만 신규 스프라이트 8종(`BattleHud*.png`)을 절차 생성한다. (3) 움직임은 `UILoopMotion` 3곳(타이머 경고 글로우·타이머 숫자·카운트다운 임박 글로우)뿐이며 평상시에는 꺼져 있고, 끄면 정적 포즈가 된다.

이 문서의 수치가 목업과 다르면 이 문서가 우선한다. 의도적 차이는 §9 에 모았다.

---

## 1. 불변 사항

- 기존 위젯의 위치·크기·앵커·부모-자식 관계·글자 크기·문구는 불변이다(글자 색은 §6 의 상태 전환 2건 외 불변). 장식은 **새 GameObject 를 추가**하는 방식이고 기존 위젯을 재부모하지 않는다. 기존 Image 의 sprite·color 교체, 기존 장식 전용 Image(`Bar/Frame`)의 오프셋 확장, 형제 순서(sibling index) 조정만 허용한다.
- `BattleHudPrefabLayoutTests` 가 `TimerPlate`(앵커 0.5,1 / 피벗 0,1 / 위치 -403,-16 / 크기 104x69)·`BossHpBar`(-277,-13 / 557x89)·`ActiveCountdown`(301,-16 / 115x69)·`SpawnerStatusPanel`(0,16 / 1006x64)·`BuildSynergyPanel`(-13,-112 / 165x247)·`BuildPanel`(-13,-387 / 165x239)의 RectTransform 값과 `Tick_0`~`Tick_8` 9개, `FoldedTab` 존재, `HeroHpBar` 부재를 검사한다. 이 값·이름은 변하지 않는다. 신규 장식은 이름이 `Tick_` 로 시작하지 않는다.
- `BattleViewModel`·`HudLayoutViewModel`·`SynergyProgress`·`UiDotPalette`·`UILoopMotion` 변경 0건. `Strings_Ko` 신규 문구 0건, 신규 JSON 0건, 신규 SO 0건, 신규 Enum 0건, 신규 Interface 0건, 신규 Addressable 키 0건(`BattleHud` 프리팹 키 그대로).
- 신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작한다(§8 테스트가 이 접두어로 일괄 검사). 신규 장식 Image 는 전부 `raycastTarget = false`. 클릭 대상은 기존 `CHButton` 위젯(패널 루트·접기/펼치기 버튼)뿐이다.
- 신규 글리프 의존 0건. 신규 텍스트 위젯 0건.
- 글로우는 전부 `Assets/_Lair/Art/Sprites/UISoftGlow.png`(표에서 "UISoftGlow"). "그라데이션"은 선행서 정의와 같다: `WorldMapGradientV`(알파 1→0, 위→아래), `WorldMapGradientH`(알파 1→0, 좌→우)를 각 행의 지정 색으로 틴트한다. 좌우 반전은 RectTransform scale (-1,1), 상하 반전은 (1,-1).
- 좌표 표기: 별도 표기가 없으면 "HUD 기준"은 1280x720 좌상단 원점(목업과 동일). "로컬"은 해당 부모 좌상단 기준 (x, y 아래로 증가)이다. 코너 장식 크기는 전부 14x14(`WorldMapCornerGold` 28x28 를 0.5 배: 막대 길이 22×0.5 = 11 ≈ 목업 10).
- 패널 공통 레시피(이하 "P-레시피", 선행서 마을 HUD §2.4 와 동일): 부모 Image 를 `Px_Solid`(Simple) + 색 `#0e1320` 알파 0.85 로 두고, 자식으로 ① `DecoPanelGrad`(stretch, `WorldMapGradientV`, 색 `#1a2132` 알파 0.85) ② `DecoPanelFrame`(stretch, Sliced, fillCenter 끔, `WorldMapFrame`) ③ `DecoPanelGloss`(`WorldMapGradientV`, 흰색 알파 0.10, 폭 = 패널 폭 − 12, 높이 12, 로컬 (6,6)) ④ `DecoCornerTL/TR/BL/BR`(`WorldMapCornerGold`, 14x14, 로컬 TL (4,4) / TR (W−18,4) / BL (4,H−18) / BR (W−18,H−18), TR scale (−1,1)·BL (1,−1)·BR (−1,−1)) ⑤ 부모 Image 에 `Shadow` effectDistance (0,−5) 색 `#000000` 알파 0.40. 알파 0.10 = 0x1a/255 = 26/255, 0.40 = 0x66/255 = 102/255.

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image** = 정적 장식(프리팹에 박고 코드 참조 없음) / **Shadow·Outline** = UGUI 컴포넌트 / **트윈** = `UILoopMotion`(§4) / **코드** = §6 의 최소 View 코드. 색은 sRGB hex, 알파는 0~1.

### 2.1 타이머 패널 (`TimerPlate`, HUD (237,16) 104x69)

`TimerPlate` 는 자식이 없는 단일 Image 이고, 루트의 첫 번째 자식이다(뒤 형제 `TimerCaption`·`TimerText` 등이 모든 자식 장식보다 위에 그려진다).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 패널 | `TimerPlate` Image 스프라이트 `Px_PanelDark` → `Px_Solid`, Type Simple, 색 `#0e1320` 알파 0.85. 이후 P-레시피 ①~⑤ 를 `TimerPlate` 자식으로 적용: W=104, H=69 → 광택 92x12, 코너 TL (4,4) / TR (86,4) / BL (4,51) / BR (86,51) | `Px_Solid`, `WorldMapGradientV`, `WorldMapFrame`, `WorldMapCornerGold` | 없음 |
| 경고 붉은 글로우 | 루트의 **첫 번째 자식(index 0, `TimerPlate` 앞)** 으로 `DecoTimerWarnGlow`: UISoftGlow, 색 `#e5484d`, 크기 160x125 (패널 + 각 28), 앵커 (0.5,1) 피벗 (0.5,0.5), 위치 (−351,−50.5) = 패널 중심. 평상시 비활성. `UILoopMotion` Pulse(§4.1 #1) + 중첩 `Canvas` | `UISoftGlow` | **신규 `_timerWarnGlow`** |
| 경고 숫자 맥동 | 기존 `TimerText` 에 `UILoopMotion` Pulse(§4.1 #2) + 중첩 `Canvas` 를 추가. 글자 색은 기존 `HandleTimer` 가 `UiDotPalette.TimerWarn`(`#FF8A8D`)으로 바꾸는 동작을 그대로 쓴다 | — | **신규 `_timerWarnTextMotion`** |

검산: 160 = 104 + 2×28, 125 = 69 + 2×28. 패널 중심 x = −403 + 104/2 = −351, y = −16 − 69/2 = −50.5. 코너 TR x = 104 − 14 − 4 = 86, BL y = 69 − 14 − 4 = 51. 광택 폭 92 = 104 − 12.

### 2.2 보스 HP 바 (`BossHpBar`, HUD (363,13) 557x89)

`BossHpBar` 루트에는 Image 가 없다. 자식은 `Title`·`HpText`·`Bar`·`StatusRow`. `Bar`(557x24, 로컬 (0,27))의 자식은 `Background`·`Lag`·`Fill`·`Ticks`·`Frame`.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 바 뒤 유리 플레이트 | `BossHpBar` 의 **첫 번째 자식(index 0)** 으로 `DecoBossPlate`: Sliced, 앵커 (0,1) 피벗 (0,1) 위치 (−8,7) 크기 573x104 (HUD (355,6)), 색 흰색 알파 0.85 | `WorldMapNamePlate` | 없음 |
| 플레이트 하단 금선 | `DecoBossPlateLine`: `DecoBossPlate` 의 자식, 앵커 (0,0) 피벗 (0,0) 위치 (2,2) 크기 569x2, 단색 `#8a6a2c` 알파 0.60 (`0x99` = 153/255) | — (스프라이트 없음) | 없음 |
| 바 금빛 발광 | `Bar` 의 첫 번째 자식으로 `DecoBossBarGlow`: UISoftGlow, 색 `#f7c64a` 알파 0.20 (`0x33`=51/255), stretch 오프셋 −14 (크기 585x52) | `UISoftGlow` | 없음 |
| 바 금테 이중 프레임 | 기존 `Bar/Frame` (장식 전용, 코드 참조 없음)의 스프라이트 `Px_Ring` → `BattleHudBossFrame`(Sliced, fillCenter 끔), 색 흰색, 오프셋을 stretch −3 으로 확장(크기 563x30). 형제 순서는 `Fill`·`Lag`·`Ticks` 보다 뒤(위) | `BattleHudBossFrame`(신규) | 기존 장식 |
| fill (평상시) | 기존 `Fill`(`_fill`, Filled Horizontal)의 스프라이트를 `BattleHudBossFill` 로 교체, 색 흰색. Image Type·Fill Method·Fill Origin 기존 값 유지 | `BattleHudBossFill`(신규) | 기존 `_fill` |
| fill (30% 이하 주황) | 같은 `Fill` Image 의 스프라이트를 `SetHp` 가 `BattleHudBossFillLow` 로 교체(§6.2) | `BattleHudBossFillLow`(신규) | **신규 `_fillNormalSprite`·`_fillLowSprite`** |
| 지연 잔상 | 기존 `Lag`(`_lag`)의 스프라이트·Type 유지, 색만 `#ffd8d8` 알파 0.67 (`0xaa`=170/255) | — | 기존 `_lag` |
| 눈금 마름모 보석 | 기존 `Gem` 9개(`_tickGems`)의 스프라이트 `BossBar_TickGem` → `VillageHudGem`, 크기 12x12 → 24x24, 앵커 (0.5,1)·위치 (0,0)·피벗 (0.5,0.5) 불변(바 상단 경계선 y=40 중심). 켜짐/꺼짐 색은 기존 `SetTickAcquired`(`GemOn` `#F7C64A` / `GemOff` `#48546E`)가 틴트하므로 획득 시 금빛 발광으로 나타난다 | `VillageHudGem` | 기존 `_tickGems` |
| 상태 아이콘 슬롯 | `StatusRow/Icon0`~`Icon7`(27x27, 8개)의 자식 `Frame` Image 색 `#07090e` → `#6b7aa8`. 슬롯 크기·`_iconSlots` 불변 | — | 기존 `_iconSlots` |

그리는 순서(아래→위): `DecoBossPlate` → `Title`·`HpText`·`Bar`(`DecoBossBarGlow` → `Background` → `Lag` → `Fill` → `Ticks`/`Gem` → `Frame`) → `StatusRow`. `Ticks` 가 `Frame` 보다 뒤 형제이면 `Frame` 을 `Ticks` 뒤로 옮긴다(형제 순서만, 부모 불변).

검산: 플레이트 573 = 557 + 2×8, 104 = 89 + 15 (위 7, 아래 8: HUD y 6~110). 플레이트 좌우 HUD x 355~928, 타이머 우측 끝 341 < 355, 카운트다운 시작 941 > 928. 금선 569 = 573 − 2×2. 바 글로우 585 = 557 + 2×14, 52 = 24 + 2×14. 프레임 563 = 557 + 2×3, 30 = 24 + 2×3, 프레임 안쪽 투명 영역 553x20 (563 − 10, 30 − 10; 목업 `.bbar` 안쪽 557 − 4 = 553). 보석 렌더 대각선 28 × 24/36 = 18.7px (목업 정사각 13px 회전 대각선 18.4px).

### 2.3 액티브 카운트다운 (`ActiveCountdown`, HUD (941,16) 115x69)

루트 Image 가 패널이고, 자식은 `Caption`(액티브 카드)·`Value`·`MiniBar`(미니바). 장식 자식은 `Caption` 앞(index 0 부터)에 삽입한다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 패널 | 루트 Image 를 `Px_Solid` Simple + `#0e1320` 알파 0.85 로 교체 후 P-레시피 ①~⑤: W=115, H=69 → 광택 103x12, 코너 TL (4,4) / TR (97,4) / BL (4,51) / BR (97,51) | P-레시피 | 없음 |
| 임박 금색 글로우 | 루트의 자식 순서에서 `ActiveCountdown` **바로 앞 형제**로 `DecoCountdownGlow`: UISoftGlow, 색 `#f7c64a`, 크기 171x125 (패널 + 각 28), 앵커 (0.5,1) 피벗 (0.5,0.5) 위치 (358.5,−50.5). 평상시 비활성. `UILoopMotion` Pulse(§4.1 #3) + 중첩 `Canvas` | `UISoftGlow` | **신규 `_imminentGlow`** |
| 미니바 fill | 기존 `_miniBar`(Filled)가 가리키는 Image 의 스프라이트를 `BattleHudMiniFill` 로 교체, 색 흰색. Type·Fill Method 유지 | `BattleHudMiniFill`(신규) | 기존 `_miniBar` |
| 미니바 테두리 | `MiniBar` 의 마지막 자식 `DecoMiniFrame`: Sliced, fillCenter 끔, stretch 오프셋 −1 (91x5 → 93x7) | `BattleHudTrackFrame`(신규) | 없음 |
| 임박 숫자 색 | `Value` 글자 색을 `Show` 가 임박(남은 5초 이하)이면 `UiDotPalette.Gold`(`#F7C64A`), 아니면 `_valueNormalColor` 로 전환(§6.2) | — | **신규 `_valueNormalColor`** |

검산: 171 = 115 + 2×28. 글로우 중심 x = 301 + 115/2 = 358.5. 코너 TR x = 115 − 14 − 4 = 97. 광택 폭 103 = 115 − 12. 미니바 HUD (953,69) 91x5 → 패널 로컬 (12,53), 테두리 93x7.

### 2.4 시너지 패널 (`BuildSynergyPanel` 프리팹, HUD (1102,112) 165x247, 접힘 높이 31)

패널 루트는 `_rootButton`(CHButton)의 클릭 대상이며 자식은 `Body`(펼침)·`FoldedTab`(접힘)이 상호 토글된다. 장식은 두 자식 안에 각각 넣어 접힘 상태 높이 변화(247 ↔ 31)에 자연스럽게 따라가게 한다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 루트 Image | 클릭 수신용으로 유지(`raycastTarget` 켬), 색 알파만 0 으로 변경(스프라이트 유지). 기존 어두운 바탕이 장식과 겹쳐 두 겹이 되는 것을 막는다 | — | 기존 `_rootButton` |
| 펼침 유리 | `Body` 첫 자식 `DecoSynBodyGlass`: 단색 `#0b101b` 알파 0.72, stretch 오프셋 −4 (173x255, HUD (1098,108)) | — | 없음 |
| 펼침 프레임 | `DecoSynBodyFrame`: Sliced, fillCenter 끔, stretch 오프셋 −4 | `BattleHudSmallFrame`(신규) | 없음 |
| 펼침 광택 | `DecoSynBodyGloss`: 흰색 알파 0.08 (`0x14`=20/255), 로컬 (0,−1) 크기 165x8, 상단 앵커 | `WorldMapGradientV` | 없음 |
| 펼침 코너 | `DecoCornerTL/TR/BL/BR` 14x14, 로컬 TL (−2,−2) / TR (153,−2) / BL (−2,235) / BR (153,235) | `WorldMapCornerGold` | 없음 |
| 헤더 금 밑줄 | `DecoSynHeaderLine`: 색 `#f7c64a` 알파 0.80 (`0xcc`=204/255), 165x2, 로컬 (0,22) | `WorldMapGradientH` | 없음 |
| 접힘 유리·프레임·광택 | `FoldedTab` 첫 자식 `DecoSynFoldGlass`(단색 `#0b101b` 알파 0.72, stretch 0), `DecoSynFoldFrame`(`BattleHudSmallFrame`, stretch 0), `DecoSynFoldGloss`(흰색 알파 0.08, 로컬 (4,3) 크기 157x8) | `BattleHudSmallFrame`, `WorldMapGradientV` | 없음 |
| 접힘 축 점 4개 | 기존 `Dot0`~`Dot3`(`_foldedDots`)의 스프라이트를 `VillageHudGem` 으로 교체, 크기 14x14, 중심 위치 불변. 켜짐(축 색)/꺼짐(`#0B0E14`) 틴트는 기존 코드 그대로 | `VillageHudGem` | 기존 `_foldedDots` |
| 접기/펼치기 버튼 | `CollapseButton` 과 `FoldedTab` 의 펼치기 버튼(`PlusIcon` 를 가진 CHButton): 버튼 Image 스프라이트를 `VillageHudBtn`(Sliced), 색 흰색. 글리프(`—`·`+` 아이콘) 색을 `#f7c64a` 로 지정. 크기 28x28 불변, Selectable 설정 유지 | `VillageHudBtn` | 기존 `_collapseButton`·`_expandButton` |

**시너지 셀 (`BuildSynergyCell.prefab`, 165x45, 7개)**

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 행 바탕 | 셀 루트 Image 를 `Px_Solid` Simple + `#0d1220` 알파 0.90 로 교체. 자식 `DecoRowGrad`(stretch, `WorldMapGradientH`, 색 `#161d2d` 알파 0.90)를 추가. 루트 Image 에 `Outline` effectDistance (2,2) 색 `#07090e` 알파 1.00 | `WorldMapGradientH` | 없음 |
| 활성 행 글로우 | `DecoRowGlow`: 셀 첫 자식, UISoftGlow, 크기 181x61 (행 + 각 8 / 8), 알파 0.40, 색은 코드가 축 색으로 칠함 | `UISoftGlow` | **신규 `_activeGlows[0]`** |
| 칩(트랙) 글로우 | `DecoTrackGlow`: `Track` 바로 뒤, UISoftGlow, 크기 = `Track` 크기 + 12 (가로·세로), 알파 0.55, 색은 코드가 축 색으로 칠함 | `UISoftGlow` | **신규 `_activeGlows[1]`** |
| 색 띠 | 기존 `AxisStripLeft`(`_axisStrip`) 유지 | — | 기존 |
| 비활성 어둡게 | 기존 `_group.alpha = SynergyProgress.Opacity(count)` 유지(0장일 때 흐림) | — | 기존 `_group` |

### 2.5 빌드 패널 (`BuildPanel`, HUD (1102,387) 165x239, 접힘 높이 31)

`BuildPanel` 루트 Image(`Px_PanelDark`, Sliced)는 `_rootButton` 클릭 대상이다. 자식은 `Body`(stretch)·`FoldedTab`. 장식은 시너지와 같은 이유로 `Body`/`FoldedTab` 안에 넣는다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 루트 Image | 시너지 루트와 동일: `raycastTarget` 켬 유지, 색 알파 0 | — | 기존 `_rootButton` |
| 펼침 패널 | `Body` 첫 자식들로 P-레시피 ①~④ 적용(바닥은 `Body` 자식 `DecoPanelBase`: 단색 `#0e1320` 알파 0.85 stretch 0): W=165, H=239 → 광택 153x12, 코너 TL (4,4) / TR (147,4) / BL (4,221) / BR (147,221). P-레시피 ⑤ `Shadow` 는 `DecoPanelBase` 에 부착 | P-레시피 | 없음 |
| 헤더 금 밑줄 | `DecoBuildHeaderLine`: `WorldMapGradientH` 색 `#f7c64a` 알파 0.80, 139x2, `Body` 로컬 (13,34) | `WorldMapGradientH` | 없음 |
| 섹션 불릿 2개 | `DecoPassiveBullet`·`DecoActiveBullet`: `VillageHudGem` 14x14, 중심 `Body` 로컬 (17,46) / (17,170). 색 각각 `#5ef0b4` / `#f7c64a` | `VillageHudGem` | 없음 |
| 접힘 탭 | `FoldedTab` 첫 자식 `DecoBuildFoldGlass`·`DecoBuildFoldFrame`·`DecoBuildFoldGloss`: 시너지 접힘(§2.4)과 같은 값 | `BattleHudSmallFrame`, `WorldMapGradientV` | 없음 |
| 접기/펼치기 버튼 | 시너지와 동일(`VillageHudBtn`, 글리프 `#f7c64a`) | `VillageHudBtn` | 기존 `_collapseButton`·`_expandButton` |

**빌드 슬롯 (`BuildIconCell.prefab`, 40x40)**

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 종류색 테두리 | 기존 `KindRing`(`_kindRing`) 유지. 코드가 패시브 `#5ef0b4`(Soul)·액티브 금색으로 칠한다 | — | 기존 `_kindRing` |
| 종류색 발광 | `DecoKindGlow`: 셀 첫 자식, UISoftGlow, 크기 56x56 (슬롯 + 각 8), 색은 코드가 종류색 알파 0.55 로 칠함. 빈 슬롯·풀 리셋 시 비활성 | `UISoftGlow` | **신규 `_kindGlow`** |
| 빈 슬롯 inset | 기존 `EmptyHatch`(`_emptyHatch`) 유지 + 자식 `DecoEmptyInset`: 색 `#000000` 알파 0.80, 36x8, 슬롯 상단 정렬. 빈 슬롯의 `KindRing` 색은 코드가 `#262D40` 으로 어둡게 칠함 | `WorldMapGradientV` | 기존 `_emptyHatch` |
| 개수 배지 | 기존 `CountBg`(`_countBg`) 스프라이트를 `WorldMapNamePlate`(Sliced), 색 흰색. `CountText` 불변 | `WorldMapNamePlate` | 기존 `_countBg` |

### 2.6 스포너 셀 6종 (`SpawnerStatusCell.prefab`, 156x64)

HUD 위치·간격(피치 170)은 `SpawnerStatusPanel`(Container + 셀 6개)이 정하며 불변이다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 프레임 | 기존 `_border` Image 의 스프라이트를 `BattleHudSmallFrame`(Sliced, fillCenter 끔)로 교체. 코드가 종 대표색을 `color` 로 곱하므로 금 헤어라인은 종족색을 띤 어두운 선이 된다(§9) | `BattleHudSmallFrame`(신규) | 기존 `_border` |
| 바탕 | 기존 `InnerBackground` 를 `Px_Solid` Simple + `#0d1220` 알파 0.90 로 교체. 자식 `DecoCellGrad`(stretch, `WorldMapGradientV`, `#171e2e` 알파 0.90), `DecoCellGloss`(`WorldMapGradientV`, 흰색 알파 0.08, 148x10, 로컬 (4,3)). `InnerBackground` Image 에 `Shadow` (0,−3) 색 `#000000` 알파 0.40 | `WorldMapGradientV` | 없음 |
| 색 띠 + 발광 | 기존 `ColorChip`(`_colorChip`)의 스프라이트를 `BattleHudChipBar`(Simple)로 교체, 크기 4x56 → 10x62 (중심 불변; 코어 4x56 + 사방 3px 발광이 스프라이트에 구워져 있고 코드의 축 색 틴트가 곱해진다) | `BattleHudChipBar`(신규) | 기존 `_colorChip` |
| 아이콘 슬롯 | 기존 `IconSlot` 스프라이트를 `WorldMapMedalOpen`, 색 `#2a3350`. `Icon`(`_icon`)·`GlowOverlay`(`_glowOverlay`)·`MaxLevelRing`(`_maxLevelRing`) 불변 | `WorldMapMedalOpen` | 기존 `IconSlot` |
| Lv 배지 | 기존 `LevelBadge`(`_levelBadgeRoot`)의 Image 스프라이트를 `WorldMapBadge`(Sliced), 색 `#e9b53e`. `BadgeText` 글자 색은 기존 코드(`EnhanceLevelVisual`)가 정한 값 그대로 | `WorldMapBadge` | 기존 `_levelBadgeRoot` |
| 진행바 테두리 | 기존 `ProgressBackground` 색 `#05070c`(스프라이트 `Px_Solid`) 유지. 자식 `DecoProgressFrame`: Sliced, fillCenter 끔, stretch 오프셋 −1 (50x8 → 52x10) | `BattleHudTrackFrame`(신규) | 없음 |
| 진행바 광택 fill | 기존 `Fill`(`_progressFill`, Filled)의 스프라이트를 `BattleHudGaugeFill`, Type·Fill Method 유지. 코드가 매 프레임 `GaugeColor` 로 칠하므로 종족색 명암 그라데이션이 된다 | `BattleHudGaugeFill`(신규) | 기존 `_progressFill` |

검산: `DecoCellGloss` 폭 148 = 156 − 8. 칩 높이 62 = 코어 56 + 3×2, 셀 높이 64 ≥ 62. 진행바 테두리 52 = 50 + 2, 10 = 8 + 2.

---

## 3. 신규 스프라이트

### 3.1 재사용 목록 (신규 생성 0장)

| 재사용 에셋 | 이 문서에서의 용도 |
|---|---|
| `UISoftGlow` | 모든 글로우(타이머 경고·카운트다운 임박·보스 바·시너지 행·트랙·빌드 슬롯) |
| `WorldMapGradientV` | 패널 바닥·광택·셀 바닥·빈 슬롯 inset |
| `WorldMapGradientH` | 시너지 행 바닥, 헤더 금 밑줄 |
| `WorldMapFrame` | 타이머·카운트다운·빌드 펼침의 이중 금테 |
| `WorldMapCornerGold` | 타이머·카운트다운·시너지 펼침·빌드 펼침 코너 |
| `WorldMapNamePlate` | 보스 바 유리 플레이트, 빌드 개수 배지 |
| `WorldMapBadge` | 스포너 Lv 배지 |
| `WorldMapMedalOpen` | 스포너 아이콘 슬롯 |
| `VillageHudGem` | 보스 눈금 보석·접힘 축 점·빌드 섹션 불릿 |
| `VillageHudBtn` | 접기/펼치기 버튼 4개 |
| `Px_Solid` (`UiDot/`) | 단색 바탕(이미 프리팹이 쓰는 에셋) |

위 에셋의 임포트 설정·크기는 선행서 값 그대로이며 이 작업에서 수정하지 않는다. 이제 침입 지도·마을 HUD·배틀 HUD 세 화면이 참조하므로 삭제·리네임 금지(§7).

### 3.2 공통 규칙 (신규 8종)

- 위치: `Assets/_Lair/Art/Sprites/` 루트, 접두어 `BattleHud`(기존 `BattleHud*.png` 없음 확인). 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.
- 생성: **일회용 에디터 스크립트** `Assets/_Lair/Editor/BattleHudSpriteGenerator.cs` 가 `Texture2D` 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋된 뒤 스크립트를 삭제한다(Rule 04 §3 — 단일 진실은 PNG).
- 임포트 공통: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center, Wrap Clamp.
- Filter Mode: `BattleHudBossFrame`·`BattleHudSmallFrame`·`BattleHudTrackFrame` 는 Point(잉크 테두리를 또렷하게). `BattleHudBossFill`·`BattleHudBossFillLow`·`BattleHudMiniFill`·`BattleHudGaugeFill`·`BattleHudChipBar` 는 Bilinear(늘어나는 그라데이션).
- 9-slice border 표기는 (좌,하,우,상). 스프라이트에 글자는 굽지 않는다. 행 번호 0 은 맨 위 행.

### 3.3 스프라이트 표

| # | 파일명 | 크기(px) | 9-slice border | 내용 (픽셀 정의) |
|---|---|---|---|---|
| 1 | `BattleHudBossFill.png` | 4x20 | 없음 | 불투명 세로 그라데이션. 행 0 `#ff8a8e` → 행 4 `#ee5a60` → 행 10 `#d1353c` (구간 선형). 행 11 `#9c1f27` → 행 19 `#b42a32` (선형, 행 10→11 에서 색이 끊긴다). 행 2~6 위에 흰색 알파 119(행 2)→17(행 6) 선형 덧칠 (목업 `top:2px, height:5px, #ffffff77→#ffffff11`) |
| 2 | `BattleHudBossFillLow.png` | 4x20 | 없음 | #1 과 같은 구조. 행 0 `#ffb26a` → 행 4 `#f07a2a` → 행 10 `#d4531a`, 행 11 `#99350f` → 행 19 `#b34415`. 같은 흰색 덧칠 |
| 3 | `BattleHudBossFrame.png` | 16x16 | 5,5,5,5 | 링만(중앙 알파 0). 바깥 2px `#07090e`, 그 안쪽 1px `#b9832a`, 그 안쪽 2px `#07090e` (합 5px) |
| 4 | `BattleHudSmallFrame.png` | 16x16 | 3,3,3,3 | 링만(중앙 알파 0). 바깥 2px `#07090e`, 그 안쪽 1px `#8a6a2c` 알파 204 (합 3px) |
| 5 | `BattleHudTrackFrame.png` | 8x8 | 2,2,2,2 | 링만(중앙 알파 0). 바깥 1px `#3a4764`, 그 안쪽 1px `#07090e` (합 2px) |
| 6 | `BattleHudMiniFill.png` | 4x5 | 없음 | 불투명 세로 그라데이션. 행 0 `#ffe58a` → 행 3 `#e0a21c` (선형), 행 4 `#b9832a` |
| 7 | `BattleHudGaugeFill.png` | 4x6 | 없음 | 불투명 회색조(틴트 곱셈용). 행 0~2 흰색 명도 1.00, 행 3 0.85, 행 4 0.70, 행 5 0.55 |
| 8 | `BattleHudChipBar.png` | 10x62 | 없음 | 흰색. 코어 x 3~6 · y 3~58 (4x56) 알파 255. 코어 밖 체비셰프 거리 d=1/2/3 픽셀의 알파는 105/70/35 (= 140×(1−d/4)), 그 밖은 0 |

검산:
- #1: 목업 `.bfill` 22%→20×0.22 = 4.4 → 행 4, 52% → 10.4 → 행 10, 53% → 10.6 → 행 11. 안쪽 높이 20 = 바 24 − 프레임 안쪽 잉크 2px × 2.
- #3: 2 + 1 + 2 = 5 = border. 목업 `.bbar` 바깥: 잉크 `0 0 0 3px` 중 금 링 1px 제외 2px, 금 1px, 자체 border 잉크 2px.
- #4: 2 + 1 = 3 = border. 목업 `.sm` border 잉크 2px + inset 1px 금.
- #5: 목업 `.sp .pb` border 1px 잉크 + `0 0 0 1px #3a4764`.
- #8: 폭 10 = 4 + 3×2, 높이 62 = 56 + 3×2.

---

## 4. 애니메이션 — `UILoopMotion` 재사용 + 정적 폴백

`Assets/_Lair/Scripts/UI/UILoopMotion.cs` 는 **수정하지 않고** 그대로 쓴다(Pulse 의 `_graphic`, `_alphaFrom/To`, `_scaleFrom/To`, `_period`; `OnDisable` 에서 기준 포즈 복원; `SetRunning(bool)` 로 정지·재개; 시간 기준 `Time.unscaledTime` 이라 카드 선택 일시정지 중에도 맥동이 유지된다).

### 4.1 움직임 3종 (모두 평상시 꺼짐)

| # | 대상 | 종류 | 파라미터 | 켜고 끄는 조건 | 정적 폴백(끄면) |
|---|---|---|---|---|---|
| 1 | `DecoTimerWarnGlow` | `Pulse` | `_graphic` = 자기 Image, 알파 0.45 ↔ 0.85, 스케일 1.00 ↔ 1.00, `_period` 1.0초 (목업 `wp 1s`) | 남은 시간 30초 이하(`BattleViewModel.IsTimerWarning`) 동안 GameObject 활성 | 활성 시 알파 0.45 고정 |
| 2 | `TimerText` | `Pulse` | `_graphic` 비움(글자 색은 `SetColor` 가 관리), 스케일 1.00 ↔ 1.07, `_period` 1.0초 (목업 `tp 1s`) | 같은 30초 조건에서 `SetRunning(true)`, 아니면 `SetRunning(false)` | 스케일 1.00 고정 |
| 3 | `DecoCountdownGlow` | `Pulse` | `_graphic` = 자기 Image, 알파 0.45 ↔ 0.90, 스케일 1.00 ↔ 1.00, `_period` 1.1초 (목업 `ip 1.1s`) | 다음 액티브 카드까지 5초 이하 동안 GameObject 활성 | 활성 시 알파 0.45 고정 |

- #1·#3 은 `SetActive` 로 켜고 끄며 `UILoopMotion` 이 `OnEnable`/`OnDisable` 에서 시작·복원한다. #2 는 `TimerText` 가 항상 활성이므로 `SetRunning` 으로 제어한다(프리팹 초기 `_running` 은 기본 true 이지만 `Bind` 직후 `HandleTimer` 초기 호출이 `SetRunning(false)` 를 보장한다).
- 동시 동작 상한: 3개(남은 시간 30초 이하 구간과 카운트다운 5초 이하 구간은 겹칠 수 있다). 세 대상 모두 중첩 `Canvas`(Override Sorting 끔)를 달아 알파·스케일 변화가 HUD 전체 메시를 재빌드하지 않게 격리한다. 글로우는 `raycastTarget` 이 꺼져 있으므로 `GraphicRaycaster` 는 붙이지 않는다.
- 이 외 모든 요소는 정적이다(보스 30% 이하 주황 전환·눈금 보석 획득 발광·활성 행 글로우는 색/스프라이트 교체이며 트윈이 아니다).

### 4.2 정적 폴백 기준

기본은 3종 전부 **동작**이다. 최저 사양 대상 기기에서 배틀 중 프레임 비용이 같은 구간의 맥동 비활성 대비 평균 +0.5ms 를 넘으면 세 `UILoopMotion` 컴포넌트의 Enabled 를 프리팹에서 해제한다(측정: Unity Profiler, 사용자 확인). 최저 사양 대상 기기는 **사용자 선택 필요** — 프로젝트 메타에 빌드 타깃 기기 기준이 없다. 폴백 후에도 정보 손실은 없다: 경고는 글자 색(`TimerWarn`)과 금색 숫자로, 임박은 숫자 색으로 구분되며 맥동은 가산 연출이다.

---

## 5. SerializeField 변경 목록

### 5.1 유지 (이름·타입·의미 불변)

- `BattleHud`: `_timerText` `_timerNormalColor` `_bossBar` `_activeCountdown` `_buildPanel` `_spawnerStatusPanel` `_synergyPanel`
- `BossHpBarView`: `_title` `_hpText` `_fill` `_lag` `_ticks`(길이 9) `_tickGems`(길이 9) `_statusIconRow` `_iconSlots`(길이 8)
- `ActiveCountdownView`: `_value` `_miniBar`
- `BuildPanel`: `_passiveScrollView` `_activeScrollView` `_rootButton` `_body` `_foldedTab` `_collapseButton` `_expandButton` `_passiveCountText` `_activeCountText` `_foldedPassiveText` `_foldedActiveText` `_expandedHeight` `_collapsedHeight`
- `BuildSynergyPanel`: `_scrollView` `_rootButton` `_tankIcon` `_dpsIcon` `_debuffIcon` `_swarmIcon` `_body` `_foldedTab` `_collapseButton` `_expandButton` `_foldedDots` `_expandedHeight` `_collapsedHeight`
- `BuildSynergyCell`: `_background` `_tierMarkers` `_text` `_axisIcon` `_axisStrip` `_group` `_countText` `_track`
- `BuildIconCell`: `_iconImage` `_frameImage` `_countText` `_button` `_kindRing` `_emptyHatch` `_countBg`
- `SpawnerStatusCell`: 기존 전부(`_border` `_colorChip` `_icon` `_speciesText` `_countText` `_progressFill` `_periodText` `_button` `_glowOverlay` `_levelBadge` `_levelBadgeRoot` `_iconRect` `_maxLevelRing` `_wispIcon` `_wraithIcon` `_reaperIcon` `_hexIcon` `_plagueIcon` `_phantomIcon`)
- `SynergyTrack`·`BuildSynergyPanel` 하위 스크롤뷰 컴포넌트: 변경 없음

### 5.2 신규 `[SerializeField] private` (8개)

| 클래스 | 필드 | 타입 | 용도 |
|---|---|---|---|
| `BattleHud` | `_timerWarnGlow` | `GameObject` | `DecoTimerWarnGlow` 루트. 30초 이하이면 활성 |
| `BattleHud` | `_timerWarnTextMotion` | `UILoopMotion` | `TimerText` 의 맥동. 30초 이하이면 `SetRunning(true)` |
| `ActiveCountdownView` | `_imminentGlow` | `GameObject` | `DecoCountdownGlow` 루트. 5초 이하이면 활성 |
| `ActiveCountdownView` | `_valueNormalColor` | `Color` | 평상시 `Value` 글자 색. 프리팹에는 `Value` 의 현재 글자 색을 그대로 기입 |
| `BossHpBarView` | `_fillNormalSprite` | `Sprite` | 평상시 fill 스프라이트(`BattleHudBossFill`) |
| `BossHpBarView` | `_fillLowSprite` | `Sprite` | 30% 이하 fill 스프라이트(`BattleHudBossFillLow`) |
| `BuildIconCell` | `_kindGlow` | `Image` | 종류색 발광(`DecoKindGlow`) |
| `BuildSynergyCell` | `_activeGlows` | `Image[]` | `DecoRowGlow`·`DecoTrackGlow` 2개(순서 고정) |

신규 필드는 모두 비어 있어도 예외 없이 동작해야 한다(§6, 기존 테스트가 리플렉션으로 필드를 일부만 채운 빈 컴포넌트를 쓴다).

---

## 6. 신규 코드 동작 (View 만, 로직 없음)

### 6.1 BattleHud

- `HandleTimer` 에서 `_timerWarnState = warnState;` 직후, `SetColor` 호출 전에 추가: `_timerWarnGlow` 가 null 이 아니면 `SetActive(warnState == 1)`, `_timerWarnTextMotion` 이 null 이 아니면 `SetRunning(warnState == 1)`. 상태 캐시(`_timerWarnState`)가 이미 변할 때만 진입하게 막고 있으므로 매 틱 호출되지 않는다.
- 그 외 `BattleHud` 변경 0건.

### 6.2 BossHpBarView · ActiveCountdownView

- `BossHpBarView`: 상수 `LowHpRatio = 0.3f`, 정적 순수 함수 `IsLowHp(float ratio)`(`ratio <= LowHpRatio` 이면 참, 목업 `hpPct <= .3`). `SetHp` 가 `ratio` 를 계산한 직후 낮음 상태가 바뀔 때만(`-1` 미적용 / `0` 평상시 / `1` 낮음 캐시, `OnEnable` 에서 `-1` 로 리셋) `_fill.sprite` 를 `_fillLowSprite`(낮음) / `_fillNormalSprite`(평상시)로 바꾼다. `_fill`·해당 스프라이트가 null 이면 건너뛴다. `fillAmount`·지연 잔상 로직은 불변.
- `ActiveCountdownView`: 상수 `ImminentRemainSeconds = 5f`, 정적 순수 함수 `IsImminent(ActiveCountdown c)`(`c.HasNext && c.RemainingSeconds <= ImminentRemainSeconds`). `Show` 가 임박 여부가 바뀔 때만(`-1`/`0`/`1` 캐시, `OnEnable` 에서 `-1` 로 리셋) `_imminentGlow` 를 `SetActive`, `_value` 를 `SetColor`(임박이면 `UiDotPalette.Gold`, 아니면 `_valueNormalColor`). 각 필드가 null 이면 건너뛴다. `FormatCountdown`·미니바 fill 로직은 불변.

### 6.3 BuildIconCell · BuildSynergyCell

- `BuildIconCell`: 상수 빈 슬롯 링 색 `#262D40`, 발광 알파 0.55. `SetKind` 는 기존 링 색 설정에 더해 `_kindGlow` 가 있으면 종류색 알파 0.55 로 칠하고 활성화. `SetEmpty` 는 `_kindGlow` 를 비활성화하고 `_kindRing` 색을 `#262D40` 으로 칠한다. `OnEnable` 은 `_kindGlow` 를 비활성화한다. (`BuildIconPoolingScrollView.InitItem` 의 호출 순서가 `SetKind` → `SetEmpty`/`Bind` 이므로 빈 슬롯 색이 마지막에 적용된다.) 필드가 null 이면 건너뛴다.
- `BuildSynergyCell`: `BindRedesign` 끝에서 `_activeGlows` 가 null 이 아니면 각 원소(null 건너뜀)를 `SetActive(strip)`(`SynergyProgress.HasStrip(data.Count)`)하고 색을 `(_axisColor.rgb, 원소의 현재 알파)` 로 칠한다. 기존 표시 모드(`RedesignMode` 아님)에서는 건너뛴다.
- ViewModel 은 읽지 않는 값이 없다. 신규 VM 필드 0건. 위 외 `.cs` 변경 0건.

---

## 7. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/BattleHud*.png` 8종 + `.meta` | 신규(§3.3) |
| `Assets/_Lair/Editor/BattleHudSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 삭제(Rule 04 §3) |
| `Assets/_Lair/Art/UI/BattleHud.prefab` | §2.1·2.2·2.3·2.5 (타이머·보스 바·카운트다운·빌드 패널·상태 아이콘 슬롯). 기존 파일에 추가·교체만 |
| `Assets/_Lair/Art/UI/BuildSynergyPanel.prefab` | §2.4 패널 |
| `Assets/_Lair/Art/UI/BuildSynergyCell.prefab` | §2.4 셀 |
| `Assets/_Lair/Art/UI/BuildIconCell.prefab` | §2.5 슬롯 |
| `Assets/_Lair/Art/UI/SpawnerStatusCell.prefab` | §2.6 |
| `Assets/_Lair/Scripts/UI/BattleHud.cs` · `BossHpBarView.cs` · `ActiveCountdownView.cs` · `BuildIconCell.cs` · `BuildSynergyCell.cs` | §5.2·§6 |
| `WorldMap*.png`, `VillageHud*.png`, `UISoftGlow.png`, `UILoopMotion.cs` | 변경 없음. **이제 배틀 HUD 도 참조하므로 삭제·리네임 금지** |

- 모든 신규 장식은 프리팹에 정적으로 배치한다. 코드에서 `Instantiate`·`new GameObject` 로 만들지 않는다. `BuildSynergyPanel`·`SpawnerStatusPanel` 은 `BattleHud` 안의 중첩 프리팹이므로 변경은 각 프리팹 파일에 하고 `BattleHud` 쪽에는 오버라이드를 만들지 않는다.
- UI 배선(UnityMCP·빌더 실행)은 목업(`.mockups/battle-hud-redesign.html`) 승인이 끝났으므로 바로 진행한다. 프리팹 편집용 에디터 스크립트를 쓰면 완료 직후 삭제한다. 과거 재직렬화 클로버 사고 방지를 위해 프리팹은 기존 파일에 추가·교체만 하고 새로 만들어 덮어쓰지 않는다.
- **구현 시 현황 확인 항목**(기획서가 프리팹 값을 직접 보지 못한 부분): ① `ActiveCountdown/MiniBar` 에서 `_miniBar` 가 가리키는 Filled Image 가 어느 오브젝트인지 ② `BuildPanel` 의 섹션 라벨 시작 x 가 목업과 같은지(`Body` 로컬 29) ③ 시너지 셀 루트 Image 의 현재 색·스프라이트 ④ `SpawnerStatusCell` 의 `InnerBackground`·`ProgressBackground`·`Track` 의 부모-자식 관계 ⑤ `Bar/Frame` 의 현재 오프셋(stretch 0 가정). 다르면 §2 표의 목표 색·크기로 맞추되 코드가 참조하는 위젯의 위치는 바꾸지 않는다.

---

## 8. 테스트 관점

### 8.1 영향받는 기존 테스트

| 테스트 | 영향 |
|---|---|
| `BattleHudPrefabLayoutTests` | §1 의 6개 RectTransform·`Tick_0~8`·`FoldedTab`·`HeroHpBar` 부재·깨진 스크립트 참조 없음. 무수정 통과가 기준 |
| `BattleHudLogicTests` · `BattleHudLogicEdgeTests` | `HandleTimer`·`IsTimerWarning` 로직 불변. 신규 필드가 비어 있어도 통과해야 한다 → null 가드가 보장 |
| `BuildSynergyPanelCountsTests` · `SynergyIndicatorPrefabTests` · `SynergyIndicatorWidgetTests` | 시너지 셀 `_axisStrip`·`_track` 등 기존 연결 검사. 무수정 통과가 기준. 리플렉션으로 필드를 일부만 채운 `BuildSynergyCell` 에서 `_activeGlows` 가 null 이어도 예외 없음 |
| `SpawnerStatusUIPlayTests` (PlayMode) | 스포너 셀 진행·강화 표시. 무수정 통과가 기준 |
| `UiRedesignDataTests` | `UiDotPalette` 값 검사. 팔레트 불변이므로 통과 |
| `HpBarView*Tests` | `HpBarView` 는 이 작업의 대상이 아니다(보스 바는 `BossHpBarView`). 영향 없음 |

### 8.2 신규 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| 스프라이트 임포트 | `BattleHud*.png` 8종 존재. Texture Type = Sprite. 9-slice border 가 §3.3 표와 일치(#3 5,5,5,5 / #4 3,3,3,3 / #5 2,2,2,2). #3·#4·#5 filterMode Point, #1·#2·#6·#7·#8 Bilinear, 전부 Wrap Clamp |
| 일회용 스크립트 삭제 | `Assets/_Lair/Editor/BattleHudSpriteGenerator.cs` 가 존재하지 않음 |
| 신규 연결 | `BattleHud` 의 `_timerWarnGlow`(이름 `DecoTimerWarnGlow`)·`_timerWarnTextMotion`(`TimerText` 소유), `ActiveCountdownView._imminentGlow`(`DecoCountdownGlow`), `BossHpBarView._fillNormalSprite`(`BattleHudBossFill`)·`_fillLowSprite`(`BattleHudBossFillLow`), `BuildIconCell._kindGlow`(`DecoKindGlow`), `BuildSynergyCell._activeGlows` 길이 2(`DecoRowGlow`·`DecoTrackGlow`)가 비어 있지 않음 |
| 기존 연결 보존 | §5.1 의 기존 필드 전부 비어 있지 않음(`_ticks`·`_tickGems` 길이 9, `_iconSlots` 길이 8). 재직렬화 클로버 가드 |
| 보석 스프라이트 | `_tickGems` 9개와 `_foldedDots` 4개의 `sprite` 가 모두 `VillageHudGem` |
| 장식 레이캐스트 | 위 프리팹 5종 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false` |
| 움직임 개수 | `BattleHud.prefab` 안 `UILoopMotion` 정확히 3개: `DecoTimerWarnGlow`(Pulse, 주기 1.0, 알파 0.45→0.85)·`TimerText`(Pulse, 주기 1.0, 스케일 1.00→1.07)·`DecoCountdownGlow`(Pulse, 주기 1.1, 알파 0.45→0.90). 글로우 두 개는 프리팹 기본 비활성 |
| 보스 낮음 판정 | `BossHpBarView.IsLowHp`: 0.31 거짓, 0.30 참, 0.0 참, 1.0 거짓 |
| 보스 fill 전환 | 빈 `BossHpBarView` 에 `_fill` 과 두 스프라이트만 채워 `SetHp(31,100)` → 평상시 스프라이트, `SetHp(30,100)` → 낮음 스프라이트, 다시 `SetHp(60,100)` → 평상시. 필드가 비어 있으면 예외 없음 |
| 임박 판정 | `ActiveCountdownView.IsImminent`: `HasNext=false` 거짓, 남은 5.0 참, 남은 5.01 거짓, 남은 0.5 참 |
| 임박 전환 | `Show` 로 남은 30 → 4.9 → 30 순서로 호출 시 `_imminentGlow.activeSelf` 가 거짓→참→거짓, `Value` 색이 `_valueNormalColor`→`UiDotPalette.Gold`→`_valueNormalColor`. 필드가 비어 있어도 예외 없음 |
| 타이머 경고 전환 | `HandleTimer` 를 elapsed 269→270(total 300)로 호출 시 `_timerWarnGlow.activeSelf` 가 거짓→참, 다시 복구 호출 시 거짓. 필드가 비어 있어도 예외 없음 |
| 빌드 슬롯 | `SetKind(true)`→`_kindGlow.activeSelf` 참·색 rgb 가 `UiDotPalette.CardKind(true)` 와 같고 알파 0.55 (허용오차 0.005). 이어 `SetEmpty()` → 글로우 비활성, `_kindRing.color` 가 `#262D40` |
| 시너지 셀 | `BindRedesign` 경로에서 Count 0 이면 `_activeGlows` 전부 비활성, Count ≥ 1 이면 활성이며 rgb 가 축 색, 알파가 프리팹 값 그대로 |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과 |

`UILoopMotion` 곡선 순수 함수 테스트는 선행서 §8.1 에서 이미 커버되므로 중복 작성하지 않는다.

### 8.3 PlayMode 불요

신규 로직은 활성 토글·스프라이트 교체·색 전환뿐이며 EditMode 로 덮인다.

### 8.4 수동 확인 (사용자 육안, 배틀 씬에서)

- 타이머 4:30 → 0:30 구간에서 남은 30초 진입 순간 붉은 글로우·숫자 맥동이 시작되고, 승패로 전투가 끝나면 멈추는지.
- 카운트다운이 30초 주기마다 5초 전부터 금색 맥동·금색 숫자가 되고 트리거 직후 평상시로 돌아오는지(카드 선택 일시정지 중에도 맥동 유지).
- 보스 HP 100 → 0 구간: 30% 이하에서 fill 이 주황으로 바뀌고, 눈금(90%~10%)을 넘을 때마다 해당 보석이 금빛으로 켜지며, 큰 피해 시 지연 잔상이 0.7초에 걸쳐 따라오는지. 상태 아이콘 슬롯 8개가 모두 보이는지.
- 시너지 4축 0장 → 7장 구간에서 행이 어둡게 시작해 1장 이상에서 색 띠·글로우가 켜지는지, 접기/펼치기 후 프레임이 잘리지 않는지(우상단 오른쪽 가장자리 x ≤ 1280 확인).
- 빌드 슬롯 패시브 9칸·액티브 5칸의 빈 슬롯 inset, 획득 시 종류색 테두리·발광, ×2 이상 배지.
- 스포너 6셀: 종족별 색 띠·프레임, 강화 Lv 1~5 배지 색, 진행바 종족색 명암.
- §4.2 의 프레임 비용 확인 후 폴백 여부 결정.

---

## 9. 목업 대비 의도적 차이 (시안과 다르게 포기·변경하는 연출)

| 항목 | 목업 | Unity 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·텍스트 글로우(`text-shadow`) | 타이머·제목·장수·스포너 글자에 검정 그림자·색 글로우 | 생략 | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 타 화면에 영향(마을 HUD §9 와 동일 판단) |
| 타이머·카운트다운 프레임 색 변화 | 경고 시 프레임이 붉은색, 임박 시 금색으로 바뀜 | 프레임은 고정 금테, 바깥 글로우(붉은/금색)와 숫자 색만 전환 | `WorldMapFrame` 은 색을 굽고 있어 틴트로 붉게 만들면 탁해지고, 전용 스프라이트·코드 참조가 추가로 필요해 YAGNI |
| 패널 금테 색·두께 | 잉크 3 + 금 `#8a6a2c` 2 + 잉크 2 = 7px | `WorldMapFrame` 9px (금 `#6b5a2a`, 안쪽 `#3a4764` 2px 추가) | 선행서 스프라이트 재사용 |
| 보스 fill 외곽 발광 | `0 0 12px` 붉은/주황 | 없음. 바 전체 금빛 글로우(`DecoBossBarGlow`)로 대체 | fill 은 바 프레임 안쪽에 클리핑되어 외곽 발광이 잘림 |
| 꺼진 눈금 보석 색 | `#0a0d14` | `#48546E` (`BossHpBarView.GemOff`) | 코드 상수 불변, 마을 HUD §9 와 동일 |
| 상태 아이콘 슬롯 | 슬롯별 테두리색(파랑·보라·빨강)·발광 | 단일 `#6b7aa8` 테두리, 발광 없음 | 슬롯 색을 상태 종류로 바꾸는 로직이 없고, 슬롯 Image 가 곧 아이콘이라 뒤 글로우를 둘 자리가 없음 |
| 시너지 칩 | 5칸 기울인 pip 별 발광 | 실제 7칸 `SynergyTrack` 유지, 행 글로우(`DecoRowGlow`)+트랙 글로우(`DecoTrackGlow`)로 대체. 칸 단위 발광 없음 | 목업 pip 5칸은 시안용 단순화이고 실제 트랙은 7칸(3·5·7 번째가 Tier 칸) |
| 시너지 비활성 행 | 채도 0.15 + 밝기 0.7 | 기존 `Opacity(count)` 알파 흐림 유지 | 채도 필터는 UGUI 에 없음 |
| 시너지 아이콘 | SVG 단색 아이콘 | 기존 `SynergyVisualConfig` 스프라이트 | 기존 에셋·코드가 담당 |
| 시너지 행 아래 그림자 | `0 2px 0 #0006` | 없음. `Outline` 잉크 2px 만 | 행 간격 12px 안에서 Shadow 중복 렌더 비용 대비 효과 작음 |
| 시너지 패널 세로 그라데이션 | `#0c111dc0 → #0a0e18b0` | 단색 `#0b101b` 알파 0.72 | 두 색 차이가 알파 0.06 이하라 단색으로 충분 |
| 빌드 개수 배지 | 종류색 테두리 | `WorldMapNamePlate` 흰 림 | 종류색을 칠하려면 코드 참조 추가 필요 |
| 빌드 섹션 카운트 텍스트 글로우 | `5/9` 소울색 글로우 | 없음 | 위 텍스트 글로우와 같은 사유 |
| 스포너 프레임 금 헤어라인 | 금색 `#8a6a2c88` | 종족색을 곱한 어두운 선 | 기존 코드가 `_border.color` 를 종 대표색으로 칠함(동작 불변 우선) |
| 스포너 아이콘 슬롯 배경 | 종족색 라디얼 | 고정 어두운 청색 메달 `#2a3350` | 종족색 라디얼은 코드 참조 추가 필요, 종족 구분은 색 띠·`GlowOverlay` 가 담당 |
| 스포너 진행바 | 흰색 하이라이트 → 종족색 → 어두운 종족색, 외곽 발광 | 종족색 명암 그라데이션(밝기 1.00→0.55), 흰색 하이라이트·발광 없음 | 스프라이트 틴트는 곱셈이라 흰색으로 밝힐 수 없음 |
| 스포너 Lv 배지 위치·글자 색 | 셀 좌하단, 글자 `#1a1206` | 프리팹 기존 위치·`EnhanceLevelVisual` 글자 색 | 위치·동작 불변 원칙 |
| 호버·눌림 이동 | `.cbtn:hover` 밝기 | 기존 Selectable 눌림 색 | 모바일 중심 UI |
| 목업 하단 도구(슬라이더·재생·체크박스) | 시안 조작용 | 구현 대상 아님 | 시안 전용 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트 8종은 프리팹 직접 참조. 삭제 대상 없음(`Px_Ring`·`BossBar_TickGem` 은 다른 화면이 쓰는지 이 작업에서 확인하지 않으므로 삭제하지 않는다).
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `BattleHud`: 신규 필드 `_timerWarnGlow`·`_timerWarnTextMotion`, `HandleTimer` 에 토글 추가 — §5.2·6.1.
  - `BossHpBarView`: 신규 필드 `_fillNormalSprite`·`_fillLowSprite`, 상수 `LowHpRatio`, 정적 함수 `IsLowHp`, `SetHp` 스프라이트 전환 — §6.2.
  - `ActiveCountdownView`: 신규 필드 `_imminentGlow`·`_valueNormalColor`, 상수 `ImminentRemainSeconds`, 정적 함수 `IsImminent`, `Show` 전환 — §6.2.
  - `BuildIconCell`: 신규 필드 `_kindGlow`, `SetKind`·`SetEmpty`·`OnEnable` 보강 — §6.3.
  - `BuildSynergyCell`: 신규 필드 `_activeGlows`, `BindRedesign` 보강 — §6.3.
  - 그 외 `.cs` 변경 0건 (`UILoopMotion`·`UiDotPalette`·`BattleViewModel`·`HudLayoutViewModel` 수정 금지). 신규 코드는 전부 null 가드.
- **프리팹**: §7 표. 필드명·장식 GameObject 이름(`Deco*`)은 테스트 기준이므로 글자 그대로 쓴다. 장식 좌표·크기는 §2 표 그대로이며, 프리팹 앵커 체계 환산은 gameplay-programmer 가 하되 코드가 참조하는 기존 위젯은 움직이지 않는다.
- **일회용 도구**: `BattleHudSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.
