# 빌드 상세 모달(BuildModalPopup) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/build-modal-redesign.html` 의 마감(금테 이중 프레임·유리 광택·비네트 모달, 마름모 보석 제목판, 붉은 입체 닫기 X, 섹션 헤더의 종류색 마름모·금 페이드 라인·**N/9 · N/5 개수 칩**, 카드 셀의 종류색 띠·일러스트 프레임 발광·유리 inset 효과 문구·금테 ×N 배지, 금색 슬림 스크롤바, 빈 상태 마름모 장식)을 `BuildModalPopup.prefab` 과 `BuildModalCardCell.prefab` 에 옮긴다. 레이아웃·좌표·문구·동작·ViewModel 은 바꾸지 않고, 그 위에 **장식 레이어와 스프라이트만 얹는다.** 코드 변경은 승인된 개수 칩 1건과, 시안 구현에 필요한 카드 셀의 종류색 칠하기·×N 배지 토글 2건뿐이다(§6).
- **검증 가설**: 좌표가 같은 상태에서 마감만 올려도, 빌드 상세 모달이 배틀 HUD(`battle-hud-visual-polish.md`)·마을 HUD(`village-hud-visual-polish.md`)·침입 지도(`worldmap-popup-visual-polish.md`)와 같은 톤으로 읽히고, 패시브(녹)/액티브(금) 구분과 "지금 몇 장 / 최대 몇 장"이 한눈에 보이는가. 정량 메트릭 없음, 사용자 육안 확인용 UI 개선이다.
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 시스템·메인 메뉴/세팅 화면 없음.
- **핵심 메커니즘**: (1) 선행 세 기획서가 만든 `WorldMap*.png`·`VillageHud*.png`·`BattleHud*.png`·`UISoftGlow.png` 를 최대한 재사용한다. (2) 재사용이 안 되는 마감만 신규 스프라이트 3종(`BuildModal*.png`)을 절차 생성한다. (3) 움직임(`UILoopMotion`)은 0건이다.

이 문서의 수치가 목업과 다르면 이 문서가 우선한다. 의도적 차이는 §9 에 모았다.

---

## 1. 불변 사항

- 기존 위젯의 위치·크기·앵커·부모-자식 관계·글자 크기·문구는 불변이다. 장식은 **새 GameObject 를 추가**하는 방식이다. 기존 위젯의 재부모는 하지 않는다. 허용되는 것은 기존 Image 의 sprite·color 교체, 형제 순서 조정, 장식 전용 기존 Image(`PassiveHeaderSquare`·`ActiveHeaderSquare`·`Divider`·`TitlePlaque`)의 크기·스프라이트 교체뿐이다.
- 현재 프리팹 실측값(이 값이 기준이며 목업 설명문과 다르면 프리팹이 우선한다):
  - `ModalBody` 640x480 중앙, `TitlePlaque`/`Title` 320x52 (상단 중앙 앵커, 위치 0,0), `CloseButton` 44x44 (앵커 우상단, 피벗 1,1, 위치 13,13), `Divider` 4 x (부모 −100) 위치 (0,−22)
  - `PassiveSection`/`ActiveSection` 282x380, 위치 (±9,−22). 섹션 안 `Label` (18,0) 폭 −18 높이 32, `ScrollView` 높이 342(섹션 상단에서 38), `EmptyText` 같은 영역
  - `BuildModalCardCell` 280x80: `RowBg` 전체 / `Frame`·`KindRing` 52x52 (6, 중앙) / `Icon` 40x40 (12, 중앙) / `NameText` (68,−6) 160x30 / `DescText` (68, 6) 204x34 / `CountText` 우상단 (−8,−8) 44x26
  - 두 `CHPoolingScrollView` 모두 `_itemGap` = (0,8). 즉 실제 스크롤 피치는 **88**(80 + 8)이다. 목업은 피치 80 으로 그렸으나 §9 에 따라 **프리팹 값 8 을 유지**한다.
  - `Dim` 색 `#040610` 알파 0.8 (목업 `#000` 알파 0.6 과 다름 — 프리팹 값 유지).
- 로직 불변: `BattleViewModel`·`UiDotPalette`·`CategoryOrder`·`BuildEntry`·`BuildModalCardPoolingScrollView` 변경 0건. `Strings_Ko` 신규 문구 0건(칩은 숫자·`/` 뿐), 신규 JSON 0건, 신규 SO 0건, 신규 Enum 0건, 신규 Interface 0건, 신규 Addressable 키 0건(`BuildModalPopup`·`BuildModalCardCell` 프리팹 키 그대로).
- Rule 03 §3 구조 유지: `BuildModalPopup`(컨테이너) → `BuildModalCardPoolingScrollView`(×2) → `BuildModalCardCell`. 두 스크롤뷰의 `_origin`(Content 안 셀 인스턴스)·`ScrollRect`·`Viewport`(RectMask2D)·`Content` 연결은 훼손하지 않는다. origin 셀 인스턴스에서 컴포넌트를 제거(`m_RemovedComponents`)하지 않는다.
- 신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작하고 모든 장식 Graphic 은 `raycastTarget = false` 이다. 클릭 대상은 기존 `Dim`·`CloseButton` 과 신규 스크롤바(`Scrollbar` 트랙·`Handle`)뿐이다.
- 신규 글리프 의존 0건(× 도 스프라이트로 그린다). 신규 텍스트 위젯은 칩 2개(`ChipText`)뿐이고 둘 다 `CHText`(`_stringID` −1)를 붙인다.
- 공통 용어 (선행서와 같음): 글로우 = `UISoftGlow.png`. "그라데이션 V" = `WorldMapGradientV`(알파 1→0, 위→아래), "그라데이션 H" = `WorldMapGradientH`(알파 1→0, 좌→우)를 지정 색으로 틴트. 좌표는 별도 표기가 없으면 "부모 좌상단 기준 (x, y 아래로 증가)". 알파 환산: 0.60=0x99/255, 0.80=0xcc/255, 0.40=0x66/255, 0.33=0x55/255.

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image** = 정적 장식 / **Shadow** = UGUI 컴포넌트 / **코드** = §6 의 최소 View 코드 / **UGUI Scrollbar** = 래퍼 미지원 컴포넌트(Rule 03 §3 예외, Slider 와 같은 분류).

### 2.1 모달 프레임·헤더 (`BuildModalPopup.prefab`)

`ModalBody`(640x480) 의 자식 최종 순서(아래→위): `DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL/TR/BL/BR` → `DecoPlaqueGlow` → `TitlePlaque` → `Title` → `DecoDividerGlow` → `Divider` → `DecoDividerGem` → `PassiveSection` → `ActiveSection` → `DecoCloseGlow` → `CloseButton`. 루트 `BuildModalPopup` 의 자식 순서: `Dim` → `DecoModalGlow` → `ModalBody`.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 모달 바탕 | `ModalBody` Image 스프라이트 `Px_Panel` → `Px_Solid`, Simple, 색 `#0d1220` 알파 0.96 (목업 `#0d1220f5`, 0xf5=245/255). `raycastTarget` 켬 유지(모달 위 클릭이 Dim 으로 새지 않게) | `Px_Solid` | 없음 |
| 바탕 그라데이션 | `DecoModalGrad`: stretch 0, 그라데이션 V, 색 `#1b2336` 알파 0.95 (목업 `#1b2336f2`, 0xf2=242/255) | `WorldMapGradientV` | 없음 |
| 내측 비네트 | `DecoModalVignette`: stretch 0, 색 `#000000` 알파 0.75 (스프라이트 최대 알파 0.80 × 0.75 = 0.60 = 목업 `inset 0 0 40px #00000099` 의 0x99/255 = 0.60) | `WorldMapVignette` | 없음 |
| 상단 유리 광택 | `DecoModalGloss`: 좌상단 앵커, 위치 (8,8), 크기 624x34, 그라데이션 V 흰색 알파 0.09 (목업 `#ffffff17`, 0x17/255) | `WorldMapGradientV` | 없음 |
| 금테 이중 프레임 | `DecoModalFrame`: stretch 0, Sliced, fillCenter 끔 | `WorldMapFrame` | 없음 |
| 모서리 장식 4개 | `DecoCornerTL/TR/BL/BR` 14x14 (`WorldMapCornerGold` 28x28 의 0.5 배, 배틀 HUD §1 와 동일). 로컬 TL (4,4) / TR (622,4) / BL (4,462) / BR (622,462). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` | 없음 |
| 모달 그림자 | `ModalBody` Image 에 `Shadow` effectDistance (0,−8), 색 `#000000` 알파 0.47 (목업 `0 8px 0 #0007`, 0x77/255) | — | 없음 |
| 모달 금빛 외곽 발광 | 루트 직속 `DecoModalGlow`: 중앙 앵커, 크기 684x524, 색 `#f7c64a` 알파 0.13 (목업 `0 0 22px #f7c64a22`, 0x22/255) | `UISoftGlow` | 없음 |
| 제목판 바탕 | `TitlePlaque` Image 스프라이트 → `Px_Solid`, Simple, 색 `#141a2c`. 자식 `DecoPlaqueGrad`(stretch 0, 그라데이션 V, 색 `#2a3450` 알파 1.0), `DecoPlaqueFrame`(stretch 0, Sliced, fillCenter 끔) | `Px_Solid`, `WorldMapGradientV`, `WorldMapFrame` | 없음 |
| 제목판 발광 | `DecoPlaqueGlow`: `TitlePlaque` 앞 형제, 위치·앵커 `TitlePlaque` 와 동일(상단 중앙 앵커, 위치 0,0), 크기 356x88, 색 `#f7c64a` 알파 0.27 (목업 `0 0 18px #f7c64a44`, 0x44/255) | `UISoftGlow` | 없음 |
| 제목판 양끝 마름모 | `TitlePlaque` 자식 `DecoPlaqueGemL`/`DecoPlaqueGemR`: 18x18, 중앙 앵커, 위치 (−133, 4) / (133, 4), 색 `#f7c64a`. 렌더 대각선 18 × 28/36 = 14 (목업 10x10 회전 대각선 14.1) | `VillageHudGem` | 없음 |
| 제목 글자 | 기존 `Title`(CHText "빌드 상세", 22, 뼈색 `#e8e1cf`) 불변. 텍스트 글로우·그림자 없음 | — | 기존 |
| 닫기 버튼 몸체 | 기존 `CloseButton` Image 스프라이트 `Px_CloseX` → `VillageHudCta`(Sliced), 색 흰색. `raycastTarget`·`Selectable`(ColorTint, 눌림 0.85)·`CHButton`·`_closeButton` 연결 불변. 기존 비활성 자식 `X`(TMP "×")는 건드리지 않는다 | `VillageHudCta` | 기존 `_closeButton` |
| 닫기 × 글리프 | `CloseButton` 자식 `DecoCloseGlyph`: 20x20 중앙, 색 `#ffd6d8`. 글리프 의존 없이 스프라이트로 그림 | `BuildModalCloseGlyph`(신규) | 없음 |
| 닫기 붉은 발광 | `DecoCloseGlow`: `CloseButton` 앞 형제, `ModalBody` 중앙 앵커, 위치 (311, 231), 크기 68x68, 색 `#e5484d` 알파 0.40 (목업 `0 0 12px #e5484d66`, 0x66/255) | `UISoftGlow` | 없음 |
| 가운데 세로 구분선 | 기존 `Divider` Image: 스프라이트 `Px_Solid` → `WorldMapFog`(알파 0→255→0 사인 곡선, 위·아래 끝이 사라짐), 색 `#e0a93a` 알파 1.0. 크기·위치 불변 | `WorldMapFog` | 기존 장식 |
| 구분선 발광 | `DecoDividerGlow`: `Divider` 앞 형제, 위치 (0,−22), 크기 20x388, 색 `#f7c64a` 알파 0.33 (목업 `0 0 8px #f7c64a55`) | `UISoftGlow` | 없음 |
| 구분선 중앙 보석 | `DecoDividerGem`: 중앙 앵커, 위치 (1,−18), 크기 22x22, 색 `#f7c64a`. 렌더 대각선 22 × 28/36 = 17.1 (목업 12x12 회전 대각선 17) | `VillageHudGem` | 없음 |

검산: 코너 TR x = 640 − 14 − 4 = 622, BL y = 480 − 14 − 4 = 462. 광택 폭 624 = 640 − 2×8. 외곽 발광 684 = 640 + 2×22, 524 = 480 + 2×22. 제목판 발광 356 = 320 + 2×18, 88 = 52 + 2×18. 닫기 중심: HUD (951,129) → `ModalBody` 중심 (960,360) 기준 x = 951 − 640 = 311, y = 360 − 129 = 231 (닫기 버튼 우측 653 = 640 + 13, 폭 44 → 중심 631 → 631 − 320 = 311 ✓). 구분선 보석 중심 HUD (641,378) → 모달 중심 (640,360) 기준 (+1, −18). 제목판 보석 좌우 대칭 ±133 = 160 − 27 (목업 왼쪽 중심 x 27, 오른쪽 276+5 = 281 은 비대칭이라 대칭으로 보정, §9). 제목판 보석 y: 중심 22 → 판 중심 26 기준 +4.

### 2.2 섹션 (`PassiveSection`/`ActiveSection`, 각 282x380)

섹션 자식 최종 순서(아래→위): `DecoSectionGlass` → `Label` → `DecoHeaderLine` → `PassiveCountChip`(액티브는 `ActiveCountChip`) → `ScrollView` → `EmptyText` → `PassiveHeaderSquare`(액티브는 `ActiveHeaderSquare`). 종류색 `K` 는 패시브 `#5ef0b4`, 액티브 `#f7c64a` (기존 헤더 마름모 색과 같고, `UiDotPalette.CardKind` 값과 같다).

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 섹션 유리 | `DecoSectionGlass`: 좌상단 앵커, 위치 (−4,0), 크기 290x380, Simple, 색 `#05070c` 알파 0.33 (목업 하단색 `#05070c55`). 자식 `DecoSectionGlassGrad`(stretch 0, 그라데이션 V, 색 `#05070c` 알파 0.30): 두 층 합성 상단 알파 = 1 − (1−0.333)(1−0.302) = 0.534 ≈ 목업 상단 `#05070c88`(0x88/255 = 0.533) | `Px_Solid`, `WorldMapGradientV` | 없음 |
| 섹션 유리 테두리 | `DecoSectionGlass` 자식 `DecoSectionFrame`: stretch 0, Sliced, fillCenter 끔, 색 흰색 알파 0.5 (검정 2px + 금 헤어라인이 반투명으로 얹힘) | `BattleHudSmallFrame` | 없음 |
| 헤더 마름모 | 기존 `PassiveHeaderSquare`/`ActiveHeaderSquare`(색 `K`, 코드 참조 없음): 스프라이트 `Px_Solid` → `VillageHudGem`, 피벗 (0.5,0.5), 위치 (7,−13), 크기 10x10 → 18x18. 렌더 대각선 14 (목업 중심 (7,13) 10x10 회전 = 대각선 14.1). 글로우는 스프라이트에 구워져 있음 | `VillageHudGem` | 기존 장식 |
| 헤더 라벨 | 기존 `Label`(CHText "패시브"/"액티브", 22, 뼈색) 불변 | — | 기존 |
| 헤더 금 페이드 라인 | `DecoHeaderLine`: 좌상단 앵커, 위치 (0,−32), 크기 282x2, 그라데이션 H, 색 `#f7c64a` 알파 0.80 | `WorldMapGradientH` | 없음 |
| **개수 칩 N/9 · N/5** | 섹션 자식 루트 `PassiveCountChip`/`ActiveCountChip`: 우상단 앵커, 피벗 (1,1), 위치 (−5,−4), 크기 56x24. 자식 ① `DecoChipGlow`(중앙, 72x40, 색 `K` 알파 0.45 — 목업 `0 0 8px` 45%) ② `DecoChipFrame`(stretch 0, Sliced, 색 `K` — 가장자리 1px 가 종류색 링, 안쪽 2px 검정, 중앙 `#0a0e18`) ③ `ChipText`(stretch, 사방 오프셋 1 → 54x22, `CHText`, 글자 14, 가운데 정렬, 글자 색 `K`, 기본 문구 `0/9` / `0/5`, `raycastTarget` 꺼짐) | `UISoftGlow`, `BuildModalChipFrame`(신규) | **신규 `_passiveCountText`·`_activeCountText`** |
| 스크롤 영역 | 기존 `ScrollView`(282x342, 섹션 상단에서 38) 불변. 아래 §2.4 의 스크롤바만 자식으로 추가 | — | 기존 `_passiveScrollView`·`_activeScrollView` |

검산: 칩 56x24 = 목업 박스 54x22 + 종류색 링 1px ×2. 칩 우측 끝 x = 282 − 5 = 277, 목업 right 6 + 링 1 = 277 ✓. 칩 위 y = 4 (목업 top 5 − 링 1). 유리 폭 290 = 282 + 4 + 4, 우측 끝 −4 + 290 = 286 (목업 `.glass` 와 동일). 칩 좌측 x = 277 − 56 = 221, `Label` 글자("패시브" 22pt ≈ 66px, x18~84)와 겹치지 않는다.

### 2.3 카드 셀 (`BuildModalCardCell.prefab`, 280x80)

셀 자식 최종 순서(아래→위): `RowBg` → `DecoKindStrip` → `DecoFrameGlow` → `Frame` → `DecoFrameBloom` → `Icon` → `KindRing` → `DecoFrameGem` → `NameText` → `DecoDescInset` → `DescText` → `DecoCountBadge` → `CountText`. 종류색 칠하기 대상 4개(`DecoKindStrip`·`DecoFrameGlow`·`DecoFrameBloom`·`DecoFrameGem`)는 `_kindTinted` 배열의 이 순서로 연결한다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 셀 바탕 | 기존 `RowBg` 스프라이트 `Px_PanelDark` → `Px_Solid`, Simple, 색 `#0d1220` 알파 0.93 (목업 `#0d1220ee`, 0xee/255). 자식 `DecoCellGrad`(stretch 0, 그라데이션 H, 색 `#1a2133` 알파 0.93), `DecoCellFrame`(stretch 0, Sliced, fillCenter 끔, 색 흰색: 검정 2px + 금 `#8a6a2c` 알파 0.80 헤어라인 1px). `RowBg` 에 `Shadow` effectDistance (0,−2), 색 `#000000` 알파 0.40 (목업 `0 2px 0 #0006`) | `Px_Solid`, `WorldMapGradientH`, `BattleHudSmallFrame` | 없음 |
| 종류색 띠 | `DecoKindStrip`: 좌측 중앙 앵커 피벗 (0,0.5), 위치 (0,0), 크기 3x76, 색 `K`(코드가 칠함), 알파 1.0 | `Px_Solid` | **`_kindTinted[0]`** |
| 프레임 바깥 발광 | `DecoFrameGlow`: `Frame` 앞 형제, 좌측 중앙 앵커 피벗 (0.5,0.5), 위치 (32,0), 크기 70x70 (프레임 52 + 각 9), 알파 0.65 (목업 `0 0 9px` 65%) | `UISoftGlow` | **`_kindTinted[1]`** |
| 프레임·링 | 기존 `Frame`(Px_PanelSunk, 흰색)·`KindRing`(Px_Ring, 종류색) 불변. `Icon` 40x40 불변 | — | 기존 `_frame`·`_kindRing`·`_icon` |
| 프레임 안쪽 발광 | `DecoFrameBloom`: `Frame` 바로 뒤(= `Icon` 앞) 형제, 위치 (32,0), 크기 48x48, 알파 0.22 (목업 안쪽 `color-mix 45%` 와 방사 바탕 22% 의 중간으로 근사) | `UISoftGlow` | **`_kindTinted[2]`** |
| 프레임 모서리 보석 | `DecoFrameGem`: `KindRing` 위 형제, 좌측 중앙 앵커 피벗 (0.5,0.5), 위치 (7, 25) (셀 좌상단 기준 (7,15)), 크기 14x14 (대각선 14 × 28/36 = 10.9 ≈ 목업 8x8 회전 11.3), 알파 1.0 | `VillageHudGem` | **`_kindTinted[3]`** |
| 이름 | 기존 `NameText`(CHText, 뼈색, 자동 크기 11~22) 불변 | — | 기존 `_nameText` |
| 효과 문구 유리 inset | `DecoDescInset`: `DescText` 앞 형제, `DescText` 와 같은 앵커·위치·크기(하단 stretch, x 68, y 6, 폭 −76 = 204, 높이 34), Simple, 색 `#05070c` 알파 0.80 (목업 `#05070ccc`). 자식 `DecoDescInsetFrame`(stretch 0, Sliced, fillCenter 끔, 색 흰색 알파 1.0) | `Px_Solid`, `BattleHudTrackFrame` | 없음 |
| 효과 문구 | 기존 `DescText`(CHText 11, `#d1d5db`, 2줄, 말줄임)의 **RectTransform 불변**, TMP Margin 만 (좌 6, 상 2, 우 6, 하 2) 로 설정(목업 padding 2px 6px). 글자 색은 기존 코드가 칠하는 값 그대로 | — | 기존 `_descText` |
| ×N 배지 | `DecoCountBadge`(루트 GameObject): `CountText` 앞 형제, 우상단 앵커 피벗 (1,1), 위치 (−8,−8), 크기 44x26 (= `CountText`). 자식 `DecoCountGlow`(중앙, 60x42, 색 `#f7c64a` 알파 0.40 — 목업 `0 0 8px #f7c64a66`), `DecoCountFrame`(stretch 0, Sliced, 색 `#b9832a`: 링 1px 금 `#b9832a`, 안쪽 검정 2px, 중앙 `#0a0e18`) | `UISoftGlow`, `BuildModalChipFrame`(신규) | **신규 `_countBadgeDecor`** |
| ×N 글자 | 기존 `CountText`(CHText 16, `Bind` 가 `×N` 과 색을 칠함, N≥2 일 때만 표시) 불변 | — | 기존 `_countText` |

검산: 배지 44 = 목업 박스 42 + 링 2, 26 = 24 + 2, 배지 좌측 x = 280 − 8 − 44 = 228 (목업 요약 @ (228,8) ✓). 글로우 60 = 44 + 2×8, 42 = 26 + 2×8. 프레임 글로우 중심 x = 6 + 52/2 = 32. 보석 위치 y 25 = 40 − 15. `DescText` 텍스트 영역 = 204 − 12 = 192 x 34 − 4 = 30.

`DecoDescInsetFrame` 색: 스프라이트 `BattleHudTrackFrame`(바깥 1px `#3a4764`, 안쪽 1px 검정)의 색을 그대로 쓰기 위해 틴트는 **흰색 알파 1.0**이다(목업 링 `#2a3248` 보다 약간 밝으나 §9 에 기록).

### 2.4 스크롤바·빈 상태

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 스크롤바 | 각 섹션 `ScrollView` 의 자식 `Scrollbar`: UGUI `Scrollbar`(방향 Bottom To Top), 앵커 (0,0)–(0,1), 피벗 (0,0.5), 위치 (286,0), 크기 6 x (부모 높이 342). 트랙 Image `Px_Solid` 색 `#05070c`, `raycastTarget` 켬. 자식 `SlidingArea`(오프셋 1: 4x340) → `Handle`(stretch 0, Image 스프라이트 `BuildModalScrollThumb`, `raycastTarget` 켬) → `DecoThumbGlow`(`Handle` 자식, stretch 오프셋 −6, 색 `#f7c64a` 알파 0.67 = 목업 `0 0 6px #f7c64aaa`). 같은 `ScrollRect` 의 `Vertical Scrollbar` 에 연결하고 `Vertical Scrollbar Visibility` = Auto Hide (내용이 넘칠 때만 표시), `Spacing` 0 | `Px_Solid`, `BuildModalScrollThumb`(신규), `UISoftGlow` | 코드 없음 (프리팹 연결) |
| 스크롤바 외곽 링 | `Scrollbar` 자식 `DecoScrollTrackFrame`: stretch 오프셋 −1 (8x344), Sliced, fillCenter 끔, 색 흰색 | `BattleHudTrackFrame` | 없음 |
| 빈 상태 | 기존 `EmptyText`(CHText "아직 픽한 카드가 없습니다", 16, `#8e98ad`) 불변. 자식 `DecoEmptyGemTop`(중앙 앵커 위치 (0, 28), 22x22, 색 `#8a6a2c`), `DecoEmptyGemBottom`(위치 (0,−30), 14x14, 색 `#8a6a2c`). `EmptyText` 가 `SetActive` 로 켜고 꺼지므로 보석도 함께 토글된다 | `VillageHudGem` | 기존 `_passiveEmptyText`·`_activeEmptyText` |

검산: 스크롤바 위치 286 = 섹션 폭 282 + 4, 우측 끝 292 → 패시브 HUD x 348 + 286 = 634 ~ 640, 구분선(638~642)과 2px 겹치며 섹션이 구분선보다 위에 그려진다(목업 동일). 액티브 650 + 292 = 942 < 모달 우측 960. 핸들 폭 4 = 6 − 1×2. 스크롤바 링 폭 8 = 6 + 1×2. 핸들 최소 크기 규칙은 두지 않는다: 패시브 카드가 13종까지 가정해도 13 × 88 = 1144 에서 핸들 비율 342/1144 = 0.30 (핸들 102px).

빈 상태 보석 좌표 검산: 목업 컬럼(보석 12 + 간격 12 + 글자 24 + 간격 12 + 보석 8 = 68)을 342 영역 중앙에 놓으면 시작 y = 137, 위 보석 중심 143, 글자 중심 173, 아래 보석 중심 201. `EmptyText` 글자 중심은 영역 중앙 171 이므로 위 보석 = 171 − 143 = +28, 아래 보석 = 171 − 201 = −30. 보석 크기: 박스 12 회전 대각선 17 → 17 × 36/28 = 21.9 ≈ 22, 박스 8 → 11.3 × 36/28 = 14.5 → 14.

---

## 3. 신규 스프라이트

### 3.1 재사용 목록 (신규 생성 0장)

| 재사용 에셋 | 이 문서에서의 용도 |
|---|---|
| `UISoftGlow` | 모달·제목판·닫기·구분선·칩·프레임·배지·스크롤 핸들 글로우, 빈 안쪽 발광 |
| `WorldMapGradientV` / `WorldMapGradientH` | 모달/제목판/섹션 유리 그라데이션, 모달 광택, 헤더 금 라인, 셀 가로 그라데이션 |
| `WorldMapFrame` / `WorldMapCornerGold` | 모달·제목판 금테, 모달 코너 4 |
| `WorldMapVignette` / `WorldMapFog` | 모달 내측 비네트 / 구분선 끝 페이드 |
| `VillageHudGem` | 제목판 보석·구분선 보석·헤더 마름모·프레임 보석·빈 상태 보석 |
| `VillageHudCta` | 닫기 X 의 붉은 입체 몸체 |
| `BattleHudSmallFrame` / `BattleHudTrackFrame` | 섹션·셀·문구 박스 테두리 / 스크롤바 외곽 링·문구 박스 링 |
| `Px_Solid` (`UiDot/`) | 단색 바탕 |

위 에셋의 임포트 설정·크기는 선행서 값 그대로이며 이 작업에서 수정하지 않는다. 이제 네 화면이 참조하므로 삭제·리네임 금지(§7). `Px_Panel`·`Px_PanelDark`·`Px_CloseX` 는 다른 화면이 쓸 수 있으므로 삭제하지 않는다.

### 3.2 공통 규칙 (신규 3종)

- 위치: `Assets/_Lair/Art/Sprites/` 루트, 접두어 `BuildModal`(기존 `BuildModal*.png` 없음 확인). 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.
- 생성: **일회용 에디터 스크립트** `Assets/_Lair/Editor/BuildModalSpriteGenerator.cs` 가 `Texture2D` 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋된 뒤 스크립트를 삭제한다(Rule 04 §3 — 단일 진실은 PNG).
- 임포트 공통: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center, Wrap Clamp.
- 9-slice border 표기는 (좌,하,우,상). 스프라이트에 글자는 굽지 않는다. 행/열 번호 0 은 맨 위 행/맨 왼쪽 열.

### 3.3 스프라이트 표

| # | 파일명 | 크기(px) | 9-slice border | Filter | 내용 (픽셀 정의) |
|---|---|---|---|---|---|
| 1 | `BuildModalChipFrame.png` | 8x8 | 3,3,3,3 | Point | 바깥 1px 흰색 불투명(틴트용 종류색 링), 그 안쪽 2px `#000000` 불투명, 중앙 2x2 `#0a0e18` 불투명 (합 1 + 2 + 2 + 2 + 1 = 8) |
| 2 | `BuildModalScrollThumb.png` | 4x4 | 없음 | Bilinear | 불투명 가로 그라데이션(4행 모두 동일). 열 0 `#f8d671`, 열 1 `#eab73f`, 열 2 `#da9d1e`, 열 3 `#c48c26` (목업 `90deg #ffe58a → #e0a21c 55% → #b9832a` 를 열 중심 0.125/0.375/0.625/0.875 에서 샘플한 값) |
| 3 | `BuildModalCloseGlyph.png` | 12x12 | 없음 | Bilinear | 흰색 × 글리프. 픽셀 (x,y) 의 알파 = 255 × clamp(1.2 − d, 0, 1), d = min(\|x − y\|, \|x + y − 11\|) / √2 (두 대각선 중 가까운 쪽까지의 거리, 선 두께 약 2.4px). 나머지 투명. 프리팹이 색 `#ffd6d8` 로 틴트 |

검산:
- #1: border 3 = 링 1 + 검정 2. 칩 56x24 에서 늘어나는 영역은 중앙 50x18. 배지 44x26 에서 중앙 38x20.
- #2: 열 값 = 구간 선형 보간. 열 0: t = 0.125/0.55 = 0.227 → (255−31×0.227, 229−67×0.227, 138−110×0.227) = (248,214,113) = `#f8d671`. 열 1: t = 0.682 → (234,183,63) = `#eab73f`. 열 2: u = (0.625−0.55)/0.45 = 0.167 → (218,157,30) = `#da9d1e`. 열 3: u = 0.722 → (196,140,38) = `#c48c26`.
- #3: 12x12 에서 대각선 d = 0 인 픽셀이 알파 255, d = 1.2 에서 0. 20x20 로 확대 표시(1.67 배).

---

## 4. 애니메이션

**`UILoopMotion` 사용 0건.** 이 모달은 평상시 정지 화면이며 시안에도 상시 움직임이 없다(닫기 X 의 호버 밝기·눌림 이동만 있고 이는 §9 에서 처리). 정적 폴백 기준·성능 측정 항목 없음. 모든 장식은 정적 Image 이고, 상태 의존 표시는 활성 토글(빈 상태 보석, ×N 배지, 스크롤바 Auto Hide)과 색 칠하기(종류색)뿐이다.

---

## 5. SerializeField 변경 목록

### 5.1 유지 (이름·타입·의미 불변)

- `BuildModalPopup`: `_dimButton` `_closeButton` `_passiveScrollView` `_activeScrollView` `_passiveEmptyText` `_activeEmptyText` (상속 `UIBase` 필드 포함 연결 전부 불변)
- `BuildModalCardCell`: `_frame` `_icon` `_nameText` `_countText` `_descText` `_kindRing`
- `BuildModalCardPoolingScrollView`: `_origin`(Content 안 셀 인스턴스)·`_itemGap` (0,8)·`_columnCount` 1 등 전부 불변. 두 섹션 각각 자기 origin 인스턴스를 가리키는 현재 연결을 그대로 둔다.

### 5.2 신규 `[SerializeField] private` (4개)

| 클래스 | 필드 | 타입 | 용도 |
|---|---|---|---|
| `BuildModalPopup` | `_passiveCountText` | `CHText` | `PassiveCountChip/ChipText`. 패시브 보유/최대 표시 |
| `BuildModalPopup` | `_activeCountText` | `CHText` | `ActiveCountChip/ChipText`. 액티브 보유/최대 표시 |
| `BuildModalCardCell` | `_kindTinted` | `Image[]` | 종류색을 칠할 장식 4개(`DecoKindStrip`·`DecoFrameGlow`·`DecoFrameBloom`·`DecoFrameGem`, 순서 고정) |
| `BuildModalCardCell` | `_countBadgeDecor` | `GameObject` | `DecoCountBadge` 루트. ×N 표시와 함께 켜고 끔 |

신규 필드는 모두 비어 있어도 예외 없이 동작해야 한다(§6; 기존 `BuildModalCardCellTests` 가 리플렉션으로 `_icon` 등 일부만 채운 빈 컴포넌트를 쓴다).

---

## 6. 신규 코드 동작 (View 만, 로직 없음)

### 6.1 BuildModalPopup — 개수 칩

- `Build(vm)` 의 끝(빈 상태 라벨·스크롤뷰 갱신 뒤)에서 칩 텍스트를 갱신한다: `_passiveCountText` 가 null 이 아니면 `"{vm.PassivePickCount}/{vm.PassiveTriggerTotal}"`, `_activeCountText` 가 null 이 아니면 `"{vm.ActivePickCount}/{vm.ActiveTriggerTotal}"` 를 `SetText`. 모달이 열릴 때(`OnEnable`/`InitUI` 경로의 `Build`)와 `OnBuildChanged` 때마다 갱신된다.
- **최대값 9/5 의 출처**: 배틀 HUD `BuildPanel.RefreshCounts` 가 이미 같은 네 개 VM 프로퍼티(`PassivePickCount`·`PassiveTriggerTotal`·`ActivePickCount`·`ActiveTriggerTotal`)로 같은 형식의 문자열을 만든다. 모달도 **같은 네 프로퍼티를 그대로 읽는다**(최대값 9/5 는 VM 의 트리거 임계값 배열 길이이며, 모달에 상수를 새로 두지 않는다 → 단일 진실). 목업 "가득" 상태 패시브 9/9·액티브 5/5 는 이 값과 같다.
- 신규 문구 0건: 칩은 숫자와 `/` 로만 구성되어 `Strings_Ko` 를 쓰지 않는다. VM 신규 필드 0건. `CategoryOrder`·정렬·`BuildEntry` 필터 불변.

### 6.2 BuildModalCardCell — 종류색 칠하기·배지 토글

- `Bind(CardData, int, bool)` 안에서 기존 `_kindRing` 색 설정과 같은 위치에 추가: `_kindTinted` 가 null 이 아니면 각 원소(null 은 건너뜀)의 색을 `(UiDotPalette.CardKind(isPassive).rgb, 그 Image 의 현재 알파)` 로 칠한다(알파는 프리팹 값을 보존: 띠 1.0 / 바깥 발광 0.65 / 안쪽 발광 0.22 / 보석 1.0). `Bind` 가 풀 재사용 때마다 호출되므로 이전 종류색이 남지 않는다.
- 기존 `_countText` 처리에서 `show = count >= 2` 를 정한 직후, `_countBadgeDecor` 가 null 이 아니면 `SetActive(show)`. 한 번 ×N 이 켜졌던 풀 셀이 1장 카드에 재사용되면 배지가 꺼진다.
- `card == null` 조기 반환·`Bind(BuildEntry)` 시그니처·글자 색·아이콘 처리 불변.

---

## 7. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/BuildModal*.png` 3종 + `.meta` | 신규(§3.3) |
| `Assets/_Lair/Editor/BuildModalSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 삭제(Rule 04 §3) |
| `Assets/_Lair/Art/UI/BuildModalPopup.prefab` | §2.1·2.2·2.4. 기존 파일에 추가·교체만 |
| `Assets/_Lair/Art/UI/BuildModalCardCell.prefab` | §2.3. 이 프리팹을 두 섹션 origin 이 인스턴스로 쓰므로 변경은 셀 프리팹 파일에만 하고 `BuildModalPopup.prefab` 안에는 오버라이드를 만들지 않는다 |
| `Assets/_Lair/Scripts/UI/BuildModalPopup.cs` · `BuildModalCardCell.cs` | §5.2·§6 |
| `WorldMap*.png`, `VillageHud*.png`, `BattleHud*.png`, `UISoftGlow.png`, `UILoopMotion.cs` | 변경 없음. 삭제·리네임 금지 |

- 모든 신규 장식은 프리팹에 정적으로 배치한다. 코드에서 `Instantiate`·`new GameObject` 로 만들지 않는다(Rule 03 §3). 신규 TMP 는 `CHText` 를 함께 붙인다.
- UI 배선은 목업(`.mockups/build-modal-redesign.html`) 승인이 끝났으므로 바로 진행한다. 프리팹 편집용 에디터 스크립트를 쓰면 완료 직후 삭제한다. 재직렬화로 기존 연결이 날아가는 사고를 막기 위해 프리팹은 기존 파일에 추가·교체만 하고 새로 만들어 덮어쓰지 않는다.
- **구현 시 현황 확인 항목**: ① `CHPoolingScrollView` 가 `Content` 크기를 전체 목록 길이로 맞추는지 → `Scrollbar` 핸들 비율이 카드 수에 따라 변하는지(카드 3장·9장·13장 상태에서 확인). 맞지 않으면 §9 의 스크롤바 폴백 행을 적용. ② 풀링 스크롤이 `ScrollRect.onValueChanged` 로 구동되어 핸들 드래그 시 셀이 재바인딩되는지. ③ `VillageHudCta` 가 44x44 닫기 버튼에서 늘어나도 모서리가 깨지지 않는지(border 6,8,6,6 → 중앙 늘어남 32x30).

---

## 8. 테스트 관점

### 8.1 영향받는 기존 테스트

| 테스트 | 영향 |
|---|---|
| `BuildModalPopupTests` (`CategoryOrder` 리플렉션) | 정렬 함수 불변. 무수정 통과가 기준 |
| `BuildModalCardCellTests` (`_icon` 리플렉션, `Bind`) | 신규 필드 `_kindTinted`·`_countBadgeDecor` 가 비어 있어도 `Bind` 가 예외 없이 동작해야 한다 → null 가드가 보장. 무수정 통과가 기준 |
| `ShopPopupCellDataTests` | 이름만 인용, 무관 |
| BuildModal 프리팹 연결 검사 | 현재 없음. 신규 작성(§8.2) |
| BattleHud 계열(`BattleHudPrefabLayoutTests` 등) | 이 작업은 `BattleHud*`·`BuildPanel` 을 수정하지 않음. 영향 없음 |

### 8.2 신규 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| 스프라이트 임포트 | `BuildModalChipFrame`·`BuildModalScrollThumb`·`BuildModalCloseGlyph` 존재, Texture Type = Sprite. ChipFrame border 3,3,3,3 + Point, ScrollThumb·CloseGlyph Bilinear, 전부 Wrap Clamp |
| 일회용 스크립트 삭제 | `Assets/_Lair/Editor/BuildModalSpriteGenerator.cs` 가 존재하지 않음 |
| 기존 연결 보존 | `BuildModalPopup` 프리팹: §5.1 의 6개 필드와 `_dimButton`·`_closeButton` 이 비어 있지 않음, 두 스크롤뷰 `_origin` 이 서로 다른 셀 인스턴스. `BuildModalCardCell` 프리팹: §5.1 의 6개 필드가 비어 있지 않음. 재직렬화 클로버 가드 |
| 신규 연결 | `_passiveCountText`(경로 `PassiveSection/PassiveCountChip/ChipText`)·`_activeCountText`(`ActiveSection/ActiveCountChip/ChipText`)가 비어 있지 않음, 둘 다 `CHText` 보유. 셀 `_kindTinted` 길이 4 이고 이름이 순서대로 `DecoKindStrip`·`DecoFrameGlow`·`DecoFrameBloom`·`DecoFrameGem`, `_countBadgeDecor` 이름 `DecoCountBadge` |
| 스크롤바 연결 | 두 `ScrollView` 의 `ScrollRect.verticalScrollbar` 가 비어 있지 않고 Visibility = Auto Hide |
| 장식 레이캐스트 | 두 프리팹 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false`. `Dim`·`CloseButton`·`Scrollbar`·`Handle` 은 켜져 있음 |
| 순서 | `ModalBody` 자식 중 `TitlePlaque` 가 `PassiveSection` 보다 앞이고 `CloseButton` 이 마지막. 셀에서 `DecoDescInset` 이 `DescText` 보다 앞, `DecoCountBadge` 가 `CountText` 보다 앞 |
| 좌표 불변 | 모달 640x480, `TitlePlaque` 320x52, `CloseButton` 44x44 위치 (13,13), 섹션 282x380, 셀 280x80, `Icon` 40x40, `_itemGap` (0,8) 이 §1 의 값과 같음 |
| 셀 종류색 | `_kindTinted` 4개를 채운 셀에 `Bind(card, 1, true)` → 4개의 rgb 가 `UiDotPalette.CardKind(true)` 와 같고 알파가 사전 값(1.0/0.65/0.22/1.0) 유지 (허용오차 0.005). 이어 `Bind(card, 1, false)` → rgb 가 `CardKind(false)` |
| ×N 배지 | `Bind(card, 1, true)` → `_countBadgeDecor.activeSelf` 거짓, `Bind(card, 2, true)` → 참, 다시 `Bind(card, 1, true)` → 거짓(풀 재사용). `card == null` 이면 상태 불변 |
| 칩 텍스트 | 리플렉션으로 `_vm` 에 `BattleViewModel` 을 주입해 `HandleBuildChanged` 호출 → 칩 문자열이 `$"{vm.PassivePickCount}/{vm.PassiveTriggerTotal}"` / `$"{vm.ActivePickCount}/{vm.ActiveTriggerTotal}"` 와 같음. 픽 추가 후 재호출하면 값이 갱신됨 |
| 빈 필드 안전 | 위 신규 필드를 하나도 채우지 않은 `BuildModalPopup`·`BuildModalCardCell` 에서 `Build`/`Bind` 가 예외 없이 동작 |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과 |

### 8.3 PlayMode 불요

신규 로직은 색 칠하기·활성 토글·텍스트 문자열뿐이라 EditMode 로 덮인다.

### 8.4 수동 확인 (사용자 육안, 배틀 씬에서 빌드 패널을 눌러)

- 카드 0장(양쪽 빈 상태 + `0/9`·`0/5`), 적음(패시브 3/9·액티브 2/5), 가득(패시브 9/9·액티브 5/5)에서 칩 숫자와 색(녹/금)이 맞는지.
- 카드가 4장 이상이어 스크롤될 때만 금색 스크롤바가 나타나고, 휠·핸들 드래그로 셀이 정상 갱신되는지. 카드 선택(일시정지) 중 모달을 열었다 닫아도 카드 추가가 반영되고 칩이 갱신되는지.
- 패시브/액티브 셀의 띠·프레임 발광·보석 색이 섞이지 않는지(풀 재사용 후에도), ×2 이상일 때만 금테 배지가 보이는지.
- 효과 문구 2줄(가장 긴 "영웅 -50% 이동속도, 모든 몬스터 +30% 이동속도 (10초)")이 문구 박스 안에서 잘리지 않는지.
- 닫기 X·Dim 클릭으로 닫히는지, 닫기 X 눌림 시 색이 어두워지는지.

---

## 9. 목업 대비 의도적 차이 (시안과 다르게 포기·변경하는 연출)

| 항목 | 목업 | Unity 구현 | 사유 |
|---|---|---|---|
| 카드 간 간격 | 피치 80, 셀 본체 상하 2px 안쪽 → 셀 사이 틈 4px | 피치 88(`_itemGap` 8 유지), `RowBg` 80 꽉 채움 → 틈 8px (그 안에 `Shadow` 2px) | 프리팹 실측 `_itemGap` = 8 이고 "좌표 불변" 원칙. 화면에 보이는 카드 수는 4.3장(342/80) 대신 3.9장(342/88) |
| Dim | `#000` 알파 0.6 | 프리팹 값 `#040610` 알파 0.8 유지 | "그대로" 원칙. 실제 화면이 시안 미리보기보다 더 어둡다 |
| 텍스트 하드 그림자·글로우 | 제목·이름·칩 글자에 그림자·색 글로우 | 생략 | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 타 화면에 영향(선행서와 같은 판단) |
| 이름 글자 크기 | 17 | 기존 자동 크기(11~22) 유지 | 글자 크기 불변 원칙 |
| 모달 금테 두께·색 | 잉크 3 + 금 `#8a6a2c` 2 + 잉크 2 | `WorldMapFrame` 9px (금 `#6b5a2a`, 안쪽 `#3a4764` 2px 추가) | 선행서 스프라이트 재사용 |
| 모달 바깥 검은 번짐 | `0 0 34px #000c` | 없음 (`Shadow` 0,−8 만) | 뒤가 이미 Dim 알파 0.8 로 어두움 |
| 제목판 프레임 | 잉크 3 + 금 `#b9832a` 2 + 잉크 2 | `WorldMapFrame` 동일 | 선행서 스프라이트 재사용 |
| 제목판 오른쪽 보석 위치 | x 276 (비대칭) | 좌우 대칭 ±133 | 시안 좌표 오류 보정 |
| 닫기 X 호버·눌림 | 호버 밝기 +30%, 눌림 아래 2px 이동 | 기존 `Selectable` 눌림 색 0.85 | 모바일 중심 UI, 위치 이동은 코드 필요 |
| 닫기 X 형태 | 붉은 그라데이션 + 이중 테두리 | `VillageHudCta` 붉은 입체 스프라이트 | 선행서 스프라이트 재사용 (색감 거의 동일) |
| 구분선 | 금색 그라데이션, 끝 15% 구간에서 알파 0.8 까지 상승 | `WorldMapFog` 사인 곡선(끝에서 서서히 올라 중앙에서 최대) | 선행서 스프라이트 재사용. 끝 페이드가 시안보다 부드러움 |
| 섹션 안쪽 그림자 | `inset 0 0 18px #0009` | 없음 | 가장자리에만 걸리는 inset 그림자는 방사 비네트로 재현 시 카드 영역 전체가 어두워짐 |
| 섹션 금 헤어라인 알파 | 0.33 | 0.8 × 0.5 = 0.40 | `BattleHudSmallFrame` 재사용 |
| 문구 박스 링 색 | `#2a3248` | `#3a4764` | `BattleHudTrackFrame` 재사용 (약간 밝음) |
| 문구 박스 안쪽 그림자 | `inset 0 2px 4px #000a` | 없음 | `Px_Solid` 단색 + 링으로 충분 |
| 셀 상단 하이라이트 | 안쪽 위 `#ffffff0c` | 없음 (`DecoCellGrad` 가로 그라데이션만) | 알파 0.05 이하라 식별 불가 |
| 프레임 안쪽 방사 | 종류색 22% → 검정 78% 방사 바탕 + 안쪽 inset 발광 | `Frame`(Px_PanelSunk) 위 `DecoFrameBloom` 방사 발광 1장 | UGUI 에 inset 발광이 없어 근사 |
| 칩 안쪽 그라데이션 | `#0c111d → #070a12` | 단색 `#0a0e18` | 차이가 명도 5 이하 |
| 칩 글자 글로우 | 종류색 글로우 | 없음 | 위 텍스트 글로우와 같은 사유 |
| 스크롤바 비활성 | 내용이 안 넘쳐도 트랙이 알파 0.25 로 남음 | Auto Hide: 안 넘치면 스크롤바 자체가 사라짐 | UGUI 표준 동작(코드 없이 달성) |
| 스크롤바 핸들 최소 길이 | 28px | 최소 길이 없음 (13종 가정 시 102px) | 카드 종류가 적어 최소 길이가 필요한 구간이 없음 |
| 스크롤바 폴백 | — | §7 확인 항목 ①이 실패하면 `Scrollbar` 와 연결 해제만 하고 장식 트랙은 숨김 | 핸들이 콘텐츠 비율과 맞지 않으면 오히려 혼동 |
| 휠 한 칸 이동량 | 40px | `ScrollRect` 기존 `Scroll Sensitivity` 1 | 기존 스크롤 동작 불변 |
| 목업 하단 도구(보유 수 버튼·배경 체크박스) | 시안 조작용 | 구현 대상 아님 | 시안 전용 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트 3종(`BuildModalChipFrame`·`BuildModalScrollThumb`·`BuildModalCloseGlyph`)은 프리팹 직접 참조. 삭제 대상 없음.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록** (이 두 파일 외 `.cs` 변경 0건. `BattleViewModel`·`BuildPanel`·`UiDotPalette`·`BuildModalCardPoolingScrollView` 수정 금지):
  - `BuildModalPopup`: 신규 필드 `_passiveCountText`·`_activeCountText`(`CHText`), `Build` 끝에서 칩 텍스트 갱신 — §5.2·6.1.
  - `BuildModalCardCell`: 신규 필드 `_kindTinted`(`Image[]`)·`_countBadgeDecor`(`GameObject`), `Bind` 에 종류색 칠하기·배지 토글 — §5.2·6.2.
  - 신규 코드는 전부 null 가드. 주석은 `//#`, 가드 절은 중괄호 없이 개행, `var`·`!` 금지(Rule 02).
- **프리팹**: §7 표. 필드명·장식 GameObject 이름(`Deco*`·`PassiveCountChip`·`ActiveCountChip`·`ChipText`·`Scrollbar`·`Handle`)은 테스트 기준이므로 글자 그대로 쓴다. 장식 좌표·크기는 §2 표 그대로이며, 프리팹 앵커 체계 환산은 gameplay-programmer 가 하되 코드가 참조하는 기존 위젯은 움직이지 않는다.
- **일회용 도구**: `BuildModalSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.

---

## 부록. 핵심 결정과 검토한 대안

| 결정 | 선택 | 대안 | trade-off |
|---|---|---|---|
| 셀 종류색(띠·발광·보석)을 칠하는 방법 | **A. `_kindTinted` 배열을 `Bind` 가 칠함 (권장)** | B. 두 섹션의 origin 셀 인스턴스에 프리팹 오버라이드로 색을 미리 박음(코드 0) / C. 종류색 장식 포기, `KindRing` 만 유지(코드 0) | A: 코드 10줄 이내 추가, `BuildIconCell._kindGlow` 와 같은 선행 패턴, 풀 재사용에서도 안전. B: 코드는 없으나 `BuildModalPopup.prefab` 안에 셀 오버라이드가 생겨 "셀 프리팹만 수정" 원칙이 깨지고 테스트가 어려움. C: 시안의 핵심 구분(녹/금 띠·발광)을 잃음 |
| ×N 배지 장식 표시 | **`_countBadgeDecor` 토글 (권장)** | 배지 장식 없이 글자만(코드 0) | 배지 장식은 `CountText` 의 자식으로 만들면 글자를 덮고, 형제로 두면 항상 보이므로 토글이 필요. 대안은 시안 핵심 요소 중 하나를 잃음 |
| 스크롤바 | **UGUI `Scrollbar` + Auto Hide (권장)** | 핸들 없는 정적 장식 트랙 | UGUI 는 코드 0 이고 표준 동작이나 풀링 스크롤과의 비율 정합을 §7 ①에서 확인해야 함. 정적 트랙은 안전하나 스크롤 위치 표시가 없어 시안의 "썸 드래그"가 사라짐 |
| 카드 피치 | **프리팹 88 유지 (권장)** | 목업 피치 80(`_itemGap` 0, 본체 상하 2px 안쪽) | 88 은 좌표 불변 원칙에 충실하나 한 화면에 보이는 카드가 0.4장 적음. 80 은 시안과 같으나 `_itemGap` 변경은 레이아웃 변경이라 이번 범위("마감만 업그레이드") 밖 |
