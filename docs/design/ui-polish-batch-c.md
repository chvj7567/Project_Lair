# UI 비주얼 폴리시 배치 C — QuestPopup · LordLevelPopup · RankingPopup · CloudPopup

## § 헤더

- **목표**: 이미 승인·구현된 화면(침입 지도 · 마을 HUD · 배틀 HUD · 시너지 모달 · 빌드 모달 · 카드 선택)과 배치 A(`ui-polish-batch-a.md`)의 디자인 언어(금색 포인트 이중 프레임 + 모서리 장식 + 상단 광택, 금테 제목판 + 마름모, 붉은 입체 닫기(`VillageHudCta`), 유리 패널, 상태색 발광, 마름모 보석, 금색 슬림 스크롤바)를 **도전과제 팝업(`QuestPopup` + `QuestCell`) · 영주성 팝업(`LordLevelPopup` + `LordRewardCell`) · 랭킹 팝업(`RankingPopup` + `RankingCell`) · 계정 팝업(`CloudPopup`)** 4종에 옮긴다. 레이아웃·좌표·문구·동작·ViewModel·`CHPoolingScrollView` 3-class 구조(Rule 03 §3)는 바꾸지 않고, 그 위에 장식 레이어와 스프라이트 교체만 얹는다.
- **검증 가설**: 좌표가 같은 상태에서 마감만 올려도 (1) 앞선 10개 화면과 같은 톤으로 읽히고, (2) 각 화면의 핵심 상태 — 도전과제의 진행 중/달성, 영주 보상의 잠금/진행 중/수령 완료/현재 레벨, 랭킹의 일반/1~3위 메달/내 행, 계정의 연결/오프라인/충돌 — 가 글자를 읽기 전에 색·발광·프레임으로 구분되는가. 정량 메트릭 없음 — 사용자 육안 확인용 UI 개선(선행서와 같은 성격). 사용자가 시안(목업) 제작 없이 전부 사전 승인했으므로 `.mockups/` 파일은 만들지 않는다(UI 목업 승인 게이트는 이 사전 승인으로 갈음).
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 시스템·메인 메뉴/세팅 화면·서버 기능 추가 없음. 신규 문구 0건.
- **핵심 메커니즘**: (1) 선행서가 만든 스프라이트(`WorldMap*` · `VillageHud*` · `BattleHud*` · `SynergyModal*` · `CardSelect*` · `BuildModal*` · `UISoftGlow` · `Px_Solid`)만 재사용하고 **신규 스프라이트는 0건**이다(§7). (2) 코드 변경은 View 한정 4곳, 모두 기존 클래스 안의 필드 추가와 몇 줄의 활성/색 토글이다: `QuestCell`(달성 장식 묶음 토글) · `LordRewardCell`(배지 색 1줄) · `RankingCell`(1~3위 메달 토글·틴트) · `CloudPopup`(연결 점 발광 틴트). 신규 MonoBehaviour 클래스 0건(§9). (3) 움직임(`UILoopMotion`)은 0건이다(§8).

이 문서의 수치가 기존 프리팹 값과 다르면 §1.1 의 "현재 값 유지 목록"이 우선한다. 배치 B(`ui-polish-batch-b.md`)는 이 문서 작성 시점에 존재하지 않아 읽지 못했다. 공통 틀(프레임·제목판·닫기·유리 패널·스크롤바·셀 배경)은 배치 A 와 빌드 모달 기획서의 이름·스프라이트·수치를 그대로 따랐고, 새로 정의한 이름은 §2 의 키트 표 한 곳에만 둔다(배치 B 와 이름이 어긋나면 §2 표만 고치면 된다).

---

## 1. 불변 사항과 공통 규약

### 1.1 현재 값 유지 목록 (프리팹 실측, 이 값이 기준)

네 팝업은 같은 골격을 쓴다: 루트 전체 stretch → 자식 `Dim`(전체 stretch) + `ModalBody`(중앙 앵커 (0,0) **720x480**), `ModalBody` 안에 `TitlePlaque`·`Title`(상단 중앙 앵커 (0,0) 320x52, 피벗 (0.5,0.5)) · `CloseButton`(우상단 앵커, 피벗 (1,1), 위치 (13,13), 44x44, 자식 비활성 `X`/`Label` 텍스트 "×").

| 화면 | 위젯 | 실측값 (유지) |
|---|---|---|
| 공통 4종 | `Dim` | 색 `#040610` 알파 **0.7216** (r 0.0157 g 0.0235 b 0.0392 a 0.72156864), `raycastTarget` 켬, Button(ColorTint). **색·알파 변경 없음**(시너지·빌드 모달의 0.8 과 다르지만 이 프리팹 값이 기준) |
| | `ModalBody` | `Px_Panel` Sliced 흰색, `raycastTarget` 켬 |
| | `TitlePlaque` | `Px_Plaque` Sliced 흰색, `raycastTarget` 꺼짐 |
| | `Title` | CHText f22 `#e8e1cf` 가운데. 문구: Quest "도전과제"(stringID 16) · Lord "영주성"(13) · Ranking "랭킹"(36) · Cloud "계정"(37) |
| | `CloseButton` | `Px_CloseX` Simple 흰색, `Selectable` ColorTint(Normal 0.9 · Highlighted 1.0 · Pressed 0.75 · Fade 0.05), CHButton |
| QuestPopup | `ScrollView` | stretch, 위치 (0,−22), sizeDelta (−56,−100) → ModalBody 좌상단 기준 x 28~692, y 72~452 (664x380). `ScrollRect` 세로만, 감도 12, 스크롤바 없음. `Viewport`: stretch, 색 `#000000` 알파 0.001, `raycastTarget` 켬, RectMask2D. `CHPoolingScrollView`: `_itemGap` (0,6), `_columnCount` 1, `_origin` = `QuestCell` 인스턴스(Content 안, 664x112, 비활성) |
| QuestCell | 루트 | 664x112, `Px_Panel` Sliced 흰색, `raycastTarget` 켬. 코드가 `_normalSprite`(= `Px_Panel`)를 매 Bind 에 대입하고 색을 달성 `(0.72,1,0.84)` / 미달성 흰색으로 칠함 |
| | 자식 순서·좌표(셀 좌상단 기준) | `NameText` TL피벗 (20,10) 380x32 f22 `#e8e1cf` 자동 11~22 · `DescText` (20,50) 520x24 f16 `#8e98ad` 자동 11~16 · `RewardText` 우하단 앵커 피벗 (1,0) (−20,10) 160x32 f22 자동 11~22 오른쪽 정렬(코드가 `UiDotPalette.Soul` 로 칠함) · `BadgeBg` 우상단 앵커 피벗 (1,1) (−20,−10) 76x36 `Px_Badge_Soul` Sliced · `AchievedBadge` 같은 위치·크기 CHText "달성" f16 가운데(코드가 `UiDotPalette.Soul`) · `ProgressRoot` 좌하단 앵커 피벗 (0,0) (20,12) 220x20 |
| | `ProgressRoot` 자식 | `Track` stretch `Px_BarBg` Sliced 흰색 · `Fill` stretch sizeDelta (−8,−8) → 212x12, `Px_BarFill`, 색 `#5ef0b4`, Image Type Filled/Horizontal, 코드가 `fillAmount` 칠함 · `ProgressText` 우측 중앙 앵커 피벗 (0,0.5) (12,0) 90x24 CHText f16 `#eef1f6` 왼쪽 정렬 |
| LordLevelPopup | 헤더 텍스트 | `LordLevelText` TL앵커 피벗 (0,1) (28,−60) 220x46 CHText f33 `#f7c64a` 왼쪽 · `LordXpBar` 상단 stretch 피벗 (0,1) 위치 (260,−66) sizeDelta (−288,24) → ModalBody x 260~692, y 66~90 (432x24), `Px_BarBg` Sliced · `LordXpNextText` 같은 앵커 (260,−96) sizeDelta (−288,22) → x 260~692, y 96~118, CHText f11 `#8e98ad` 오른쪽 정렬 |
| | `LordXpBar` 자식 | `LordXpFill` stretch sizeDelta (−8,−8) → 424x16, `Px_BarFill`, 색 `#f7c64a`, Filled/Horizontal(`_xpFill`, 코드가 `fillAmount`) 이 인덱스 0 · `XpSeg1`~`XpSeg9` 앵커 x = 0.1~0.9, 폭 4, 높이 −8(16), `Px_Solid` 검정 알파 0.6 |
| | `ScrollView` | stretch, 위치 (0,−50), sizeDelta (−56,−156) → x 28~692, y 128~452 (664x324). `Viewport` 는 Quest 와 같은 값. `_itemGap` (0,6), `_columnCount` 1, `_origin` = `LordRewardCell` 인스턴스(664x72, 비활성) |
| LordRewardCell | 루트 | 664x72, `Px_Panel` Sliced 흰색, `raycastTarget` 켬. 코드가 `_normalSprite`(`Px_Panel`) / `_pendingSprite`(`Px_PanelDark`) 를 대입하고 색을 흰색으로 칠함(수령 완료 행만 흰색 알파 0.65) |
| | 자식 순서·좌표(셀 좌상단 기준 중심) | `StateRing` stretch `Px_Ring` Sliced 색 `#f7c64a`(현재 레벨 행만 활성) · `LevelText` 좌중앙 피벗 (0,0.5) (20,0) 80x32 f22 자동 11~22 · `NameText` 좌중앙 (108,+16) 300x32 f22 자동 `#eef1f6` · `RewardText` 우중앙 피벗 (1,0.5) (−118,0) 190x32 f22 자동 오른쪽 정렬 · `BadgeBg` 우중앙 피벗 (1,0.5) (−20,0) 76x36 `Px_Badge_Soul` Sliced · `ReachedBadge` 같은 자리 CHText "달성" f16 · `SubText` 좌중앙 (108,−20) 300x18 f11 `#8e98ad` |
| RankingPopup | 탭 줄 | `StageTabRow` TL앵커 피벗 (0,1) 위치 (28,−60) 664x40 → x 28~692, y 60~100. 자식 `StageTab1`~`5` 좌중앙 앵커 피벗 (0,0.5) 92x40, x 오프셋 0/98/196/294/392, `OverallTab` x 490 폭 130. 각 탭: Image `Px_Tab` Sliced(raycast 켬) + Toggle(Transition None · ToggleTransition Fade · 같은 ToggleGroup, AllowSwitchOff 꺼짐) + 자식 `Checkmark`(stretch, `Px_TabOn` Sliced) + 자식 `Label`(stretch, CHText f17 `#e8e1cf`; "STAGE n" / "최단 클리어") |
| | `Header` | 상단 stretch 피벗 (0.5,1) 위치 (0,−108) sizeDelta (−56,36) → x 28~692, y 108~144, `Px_PanelDark` Sliced `raycastTarget` 꺼짐. 자식 `HRank`(앵커 x 0~0.14) · `HName`(0.14~0.58) · `HTime`(0.58~0.84) · `HHero`(0.84~1), 각 sizeDelta x −12, CHText f16 `#8e98ad` ("순위"·"이름"·"시간"·"영웅") |
| | `ScrollView` | stretch, 위치 (0,−32), sizeDelta (−56,−232) → x 28~692, y 148~396 (664x248). `Viewport` 색 `#000000` 알파 0.15, `raycastTarget` 켬, RectMask2D. `_itemGap` (0,0). `_origin` = `LeaderboardCell` 인스턴스(`RankingCell` 프리팹, 648x52) |
| | `Divider` | 하단 stretch 피벗 (0.5,0) 위치 (0,72) sizeDelta (−56,8) → x 28~692, y 400~408, `Px_Divider` Sliced `raycastTarget` 꺼짐 |
| | `MyRankRow` | 하단 stretch 피벗 (0.5,0) 위치 (0,12) sizeDelta (−56,52) → x 28~692, y 416~468, **기본 비활성**(코드가 `_myRankContainer` 로 켬). 자식: `RankingCell` 인스턴스 1개(`_myRankCell`, 컨테이너 stretch) |
| | `EmptyText` | stretch, 위치 (0,−45), sizeDelta (−60,−420) → x 30~690, y 255~315, CHText f22 `#8e98ad` 가운데, 기본 문구 비어 있음 |
| RankingCell | 루트 | 648x52, `Px_PanelDark` Sliced 흰색 `raycastTarget` 켬. 코드가 `_normalSprite`(`Px_PanelDark`) / `_mineSprite`(`Px_Panel`) 를 대입하고 색 흰색 |
| | 자식 순서·앵커 | `MineRing` stretch `Px_Ring` Sliced 색 `#5ef0b4`(내 행만 활성, 코드가 소울 색 칠함) · `RankText` 앵커 x 0~0.14 sizeDelta x −12, f22 자동 11~22 가운데 · `NameText` x 0.14~0.58 f22 자동 왼쪽 `#eef1f6` · `TimeText` x 0.58~0.84 f22 자동 가운데 `#5ef0b4` · `HeroText` x 0.84~1 f16 자동 가운데 `#8e98ad` |
| CloudPopup | 위젯(ModalBody 좌상단 기준 박스) | `ConnectionText` x 68~692, y 92~132 f22 `#eef1f6` · `ConflictDot` 우상단 앵커 중심 (694,24) 20x20 `Px_Dot` 색 `#e5484d` **기본 비활성** · `DisplayNameText` x 30~520, y 150~198 f22 `#e8e1cf` 왼쪽 · `ChangeNameButton` x 560~700, y 150~198 `Px_Btn` · `RestoreButton` 상단 중앙 중심 (360,320) 340x60 `Px_Btn`(코드가 `ShowRestoreButton == false` 로 **항상 비활성**) |
| | `NameEditGroup` | **기본 비활성**. 520x182, x 100~620, y 198~380, `Px_PanelDark` Sliced. 자식(그룹 좌상단 기준): `NameInput` 중심 (260,52) 480x52 `Px_PanelSunk` + TMP_InputField(자식 `TextArea` RectMask2D → `Text` f22 `#eef1f6`, 글자 수 12) · `NameConfirmButton` 중심 (130,132) 220x48 `Px_BtnSoul`, "확인" · `NameCancelButton` 중심 (390,132) 220x48 `Px_Btn`, "취소" |
| | `ConflictGroup` | **기본 비활성**. x 20~700, y 82~334 (680x252), `Px_Panel` Sliced 색 `(1,0.62,0.62)`(불투명 — 뒤의 연결·표시명 줄을 가림). 자식(그룹 좌상단 기준): `ConflictRing` stretch `Px_Ring` Sliced 색 `#e5484d` · `ConflictText` x 16~664, y 14~90 f17 `#ff9a9d` · `ConflictRestoreButton` 중심 (150,202) 260x44 `Px_BtnGold` "클라우드로 복원" · `ConflictLaterButton` 중심 (550,202) 220x44 `Px_Btn` "나중에" · `ConflictCompare` 상단 stretch 피벗 (0.5,1) (0,−96) sizeDelta (−32,72) → 그룹 x 16~664, y 96~168, 자식 `ConflictLocalBox`(왼쪽 절반, `Px_PanelSunk`) · `ConflictCloudBox`(오른쪽 절반, `Px_PanelSunk`) 각 자식 `Label` f11 `#8e98ad`("이 기기"/"클라우드") + `ConflictLocalText`/`ConflictCloudText` f22 자동 11~22 `#e8e1cf` |
| | `ConnectionDot` | 좌상단 앵커 중심 (44,112) 20x20 `Px_Dot` Simple, 색은 코드가 칠함(연결됨 `#5ef0b4` / 오프라인 `#e5484d`). 형제 순서상 `ConflictGroup` **뒤**(충돌 패널 위에 그려짐) |
| | 루트 자식 순서(`ModalBody`) | `TitlePlaque` → `Title` → `ConnectionText` → `ConflictDot` → `DisplayNameText` → `ChangeNameButton` → `RestoreButton` → `NameEditGroup` → `ConflictGroup` → `ConnectionDot` → `CloseButton` |
| 팝업별 `ModalBody` 자식 순서(기존) | Quest | `TitlePlaque` → `Title` → `ScrollView` → `CloseButton` |
| | Lord | `TitlePlaque` → `Title` → `LordLevelText` → `ScrollView` → `LordXpBar` → `LordXpNextText` → `CloseButton` |
| | Ranking | `TitlePlaque` → `Title` → `Header` → `ScrollView` → `Divider` → `MyRankRow` → `EmptyText` → `StageTabRow` → `CloseButton` |

위 표는 프리팹 YAML 실측이다. 모든 기존 TMP 의 색·크기·정렬·문구, `Selectable` 의 Transition/ColorBlock, `CHPoolingScrollView` 의 `_origin`·`_itemGap`·`_columnCount`, 셀 인스턴스의 크기·비활성 오버라이드(Quest 664x112 · Lord 664x72 · Ranking 648x52)는 **변경하지 않는다.**

### 1.2 불변 규칙

- 기존 위젯의 **재부모와 위치·크기 이동은 하지 않는다.** 허용되는 것은 ① 신규 장식 GameObject 추가(기존 위젯의 자식으로 추가 포함), ② 기존 Image 의 스프라이트·색·Image Type 교체(이 문서가 명시한 것만), ③ 형제 순서 삽입, ④ `Shadow` 컴포넌트 추가뿐이다. 이 문서가 색을 지정하지 않은 기존 TMP 는 색도 바꾸지 않는다.
- 로직 불변: 닫기(`Close(reuse:true)`) · 도전과제 13개 목록/진행 바 계산(`BuildCellData`) · 영주 레벨/보상/XP 문구(`BuildCellData`·`BuildXpNextText`) · 랭킹 조회/탭 전환/내 행 판정(`Load`·`IsMyRow`·`PickMyRow`·`ResolveInitialStage`) · 표시명 변경 왕복/충돌 권유/복원 콜백 · `ShowRestoreButton`(false) 전부 변경 0건. `QuestPopup`·`LordLevelPopup`·`RankingPopup`·`QuestPoolingScrollView`·`LordRewardPoolingScrollView`·`RankingPoolingScrollView`·`UILoopMotion`·`UiDotPalette` 는 수정 금지.
- `Strings_Ko` 신규 문구 0건 · 신규 JSON 0건 · 신규 SO 0건 · 신규 Enum 0건 · 신규 Interface 0건 · 신규 Addressable 키 0건(네 팝업과 세 셀 프리팹 키 그대로). 신규 텍스트 위젯 0건이므로 `CHText` 추가도 0건. 신규 글리프 의존 0건(× 도 스프라이트로 그린다).
- 기존 `[SerializeField]` 필드는 이름·타입·연결 대상 전부 유지한다. 두 `CHPoolingScrollView` 상속 컴포넌트의 `_origin`(Content 안 셀 인스턴스)은 연결을 보존하고, origin 셀 인스턴스에서 컴포넌트를 제거(`m_RemovedComponents`)하지 않는다.
- **셀 프리팹 변경은 `QuestCell.prefab` · `LordRewardCell.prefab` · `RankingCell.prefab` 파일에만 한다.** 팝업 프리팹 안의 origin 인스턴스(`QuestCell`·`LordRewardCell`·`LeaderboardCell`·`RankingCell`)에는 오버라이드를 새로 만들지 않는다.
- **신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작**(컨테이너 포함)하고, 신규 장식 Graphic 은 전부 `raycastTarget = false` 다. 클릭 대상은 기존 위젯(`Dim`·`CloseButton`·각 버튼·탭·입력창)과 신규 스크롤바(`Scrollbar` 트랙·`Handle`)뿐이다. 장식은 `Instantiate`/`new GameObject` 로 만들지 않고 프리팹에 정적으로 배치한다.
- **MonoBehaviour 는 파일명 = 클래스명인 별도 파일**이다. 이 작업은 신규 MonoBehaviour 가 0건이라 새 `.cs` 파일이 없다(같은 파일에 MonoBehaviour 를 둘 이상 두는 것은 Missing Script 의 원인이므로 금지).
- **프리팹 편집은 UnityMCP 로 한다.** 기존 프리팹 파일에 추가·교체만 하며 새로 만들어 덮어쓰지 않는다(fileID 보존, 마을 HUD 재직렬화 클로버 전례). 프리팹 생성 에디터 스크립트를 쓰면 완료 직후 삭제한다(Rule 04 §3).
- `Selectable` 의 `ColorBlock`(Normal 0.9 · Highlighted 1.0 · Pressed 0.75)과 Transition 종류는 모든 버튼에서 **불변**이다(배치 A 와 같은 결정: 스프라이트가 명암을 굽고 있어 Normal 0.9 가 곱해져 휴지 상태가 10% 어두워 보이는 것을 허용).
- 코드 규약(§9): 가드 절(조건 충족 시 즉시 `return`/`continue`)만 중괄호 없이 개행하고, 그 외 분기는 한 줄이어도 중괄호를 쓴다. `if (x != null) { ... }` 처럼 가드 뒤에 계속 진행하는 분기는 가드 절이 아니므로 중괄호를 쓴다. 주석은 `//#`, `var`·`!` 금지.

### 1.3 용어와 좌표 규약

- "UISoftGlow" = `Assets/_Lair/Art/Sprites/UISoftGlow.png`, "Px_Solid" = `Assets/_Lair/Art/Sprites/UiDot/Px_Solid.png`. "그라데이션 V" = `WorldMapGradientV`(알파 1→0, 위→아래), "그라데이션 H" = `WorldMapGradientH`(알파 1→0, 왼→오)를 지정 색으로 틴트(정의는 `worldmap-popup-visual-polish.md` §2).
- **좌표는 별도 표기가 없으면 "부모 박스 좌상단 기준 (x 오른쪽, y 아래) 중심점"** 이다. `중심 (x,y)` 와 `크기 WxH` 로 적으며 구현자가 부모의 앵커 체계로 환산한다. `stretch` 는 부모 전체(오프셋 0), `stretch(좌 a, 우 b, 상 c, 하 d)` 는 안쪽 오프셋이다. 앵커·피벗을 적은 경우(`상단 중앙 앵커 위치 (x,y)`)는 UGUI `anchoredPosition` 값(y 위로 +)이다. 좌우/상하 뒤집기는 RectTransform scale (−1,1)/(1,−1)이고 피벗이 중심이라 중심점이 바뀌지 않는다.
- 색은 sRGB hex, 알파는 0~1. 소울 `#5ef0b4`, 금 `#f7c64a`, 피 `#e5484d` 는 `UiDotPalette` 의 `Soul`·`Gold`·`Blood` 값과 같다.
- 알파 환산: 0.33 = 0x55/255, 0.96 = 0xf5/255, 0.95 = 0xf2/255.

### 1.4 요청문 상태 이름과 실제 상태의 대응

요청문의 "퀘스트 진행/완료/수령"·"영주 보상 잠금/수령가능/수령"은 현재 코드와 다르다. 이 문서는 **코드의 실제 상태**를 기준으로 한다(수령 버튼이 없고 달성/도달 시 자동 지급 — `LordRewardCellData` 주석 "자동 수령(§4.4)이라 표시 전용").

| 요청문 | 실제 상태 (코드 근거) | 판별 데이터 |
|---|---|---|
| 퀘스트 진행 | 진행 중 — 진행 바 표시 | `QuestCellData.Achieved == false && HasProgress == true` |
| (없음) | 미달성·진행 바 없음 — 단발형 과제(Threshold 1 이하) | `Achieved == false && HasProgress == false` |
| 퀘스트 완료/수령 | 달성 — "달성" 배지, 소울 보상은 달성 시 자동 지급 | `Achieved == true` |
| 영주 보상 잠금 | 잠금 더미(문구가 물음표 3개로 시작하는 자리표시 행) 와 미도달(레벨 부족, "N XP 남음") 두 가지 | `IsLockedDummy` / `Reached == false` |
| 영주 보상 수령가능 | 존재하지 않음 | — |
| 영주 보상 수령 | 수령 완료("수령 완료") 와 현재 레벨("현재 레벨", 금테) 두 가지 | `Reached && IsCurrent == false` / `IsCurrent` |

---

## 2. 공통 키트 (네 팝업 공통 — 단일 진실 위치)

네 팝업의 `ModalBody` 는 같은 크기(720x480)이므로 같은 키트를 쓴다. 아래 이름·수치는 이 절에만 정의하고 §3~§6 은 이 키트를 인용한다.

### 2.1 K1. 모달 프레임 (`ModalBody` 장식)

루트 직속 자식 순서(뒤 → 앞): `Dim` → `DecoModalGlow` → `ModalBody`.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `DecoModalGlow` | 루트 직속, 중앙 앵커 위치 (0,0) 크기 764x524, 색 `#f7c64a` 알파 0.13 | `UISoftGlow` |
| 모달 바탕 | 기존 `ModalBody` Image 스프라이트 `Px_Panel` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.96. `raycastTarget` 켬 유지(모달 위 클릭이 `Dim` 으로 새지 않게). `Shadow` effectDistance (0,−8) 색 `#000000` 알파 0.47 | `Px_Solid` |
| `DecoModalGrad` | `ModalBody` 자식, stretch, 그라데이션 V 색 `#1b2336` 알파 0.95 | `WorldMapGradientV` |
| `DecoModalVignette` | stretch, 색 `#000000` 알파 0.75 (스프라이트 최대 알파 0.80 × 0.75 = 0.60) | `WorldMapVignette` |
| `DecoModalGloss` | 중심 (360,25) 704x34, 그라데이션 V 흰색 알파 0.09 | `WorldMapGradientV` |
| `DecoModalFrame` | stretch, Sliced, fillCenter 끔, 흰색 | `WorldMapFrame` |
| `DecoCornerTL`·`TR`·`BL`·`BR` | 각 14x14. 중심 TL (11,11) / TR (709,11) / BL (11,469) / BR (709,469). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` |

`ModalBody` 자식 순서(뒤 → 앞) 앞부분은 네 팝업 공통으로 `DecoModalGrad` → `DecoModalVignette` → `DecoModalGloss` → `DecoModalFrame` → `DecoCornerTL` → `DecoCornerTR` → `DecoCornerBL` → `DecoCornerBR` → `DecoPlaqueGlow` → `TitlePlaque` → `Title` 이다. 그 뒤는 팝업별 절이 정한다.

검산: 코너 TR 중심 x = 720 − 4 − 7 = 709, BL 중심 y = 480 − 4 − 7 = 469. 광택 폭 704 = 720 − 2×8, 중심 x = 8 + 352 = 360, 중심 y = 8 + 17 = 25. 외곽 발광 764 = 720 + 2×22, 524 = 480 + 2×22 (빌드 모달 `684x524 = 640x480 + 2×22` 와 같은 산식). 프레임 `WorldMapFrame` 9px 이므로 안쪽 영역은 x 9~711, y 9~471 이고 §3~§6 의 모든 콘텐츠(x 16 이상 ~ 704 이하, y 50 이상 ~ 468 이하)가 그 안에 든다. 단 `CloseButton`(x 689~733, y −13~31)과 `ChangeNameButton`(우측 끝 x 700)은 기존 위치 그대로다.

### 2.2 K2. 제목판

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `DecoPlaqueGlow` | `ModalBody` 자식, `TitlePlaque` 바로 앞 형제. 상단 중앙 앵커 위치 (0,0) 크기 356x88, 색 `#f7c64a` 알파 0.27 | `UISoftGlow` |
| 제목판 바탕 | 기존 `TitlePlaque` Image 스프라이트 `Px_Plaque` → `Px_Solid`(Simple), 색 `#141a2c`. `raycastTarget` 꺼짐 유지 | `Px_Solid` |
| `DecoPlaqueGrad` | `TitlePlaque` 자식, stretch, 그라데이션 V 색 `#2a3450` 알파 1.0 | `WorldMapGradientV` |
| `DecoPlaqueFrame` | `TitlePlaque` 자식, stretch, Sliced, fillCenter 끔 | `WorldMapFrame` |
| `DecoPlaqueGemL`·`R` | `TitlePlaque` 자식, 판 중앙 앵커 18x18, 위치 (−133,+4) / (+133,+4), 색 `#f7c64a` | `VillageHudGem` |
| 제목 글자 | 기존 `Title` 불변 (CHText f22 `#e8e1cf`) | 기존 |

검산: 발광 356 = 320 + 2×18, 88 = 52 + 2×18. 마름모 ±133 = 160 − 27. 렌더 대각선 18 × 28/36 = 14.

### 2.3 K3. 닫기 버튼

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 닫기 몸체 | 기존 `CloseButton` Image 스프라이트 `Px_CloseX` → `VillageHudCta`(Sliced), 색 흰색. `raycastTarget`·`Selectable`·CHButton 연결 불변. 기존 비활성 자식 텍스트(Quest·Lord·Ranking 은 `X`, Cloud 는 `Label`)는 건드리지 않는다 | `VillageHudCta` |
| `DecoCloseGlyph` | `CloseButton` 자식, 중앙 앵커 20x20, 색 `#ffd6d8` | `BuildModalCloseGlyph` |
| `DecoCloseGlow` | `ModalBody` 자식, `CloseButton` 바로 앞 형제. 중앙 앵커 위치 (351,231) 크기 68x68, 색 `#e5484d` 알파 0.40 | `UISoftGlow` |

검산: 닫기 버튼 중심은 `ModalBody` 우측선에서 안쪽으로 22 − 13 = 9, 상단선에서 아래로 9 → 중앙 기준 (360 − 9, 240 − 9) = (351,231). 빌드 모달의 `(311,231)`(폭 640)과 같은 산식이다.

### 2.4 K4. 유리 패널 (목록·헤더 뒤판)

유리 패널은 이름만 다르고 구성이 같다. `Deco<Name>Glass` 한 벌 = 3개 GameObject:

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `Deco<Name>Glass` | 지정한 중심·크기, Simple, 색 `#05070c` 알파 0.33 | `Px_Solid` |
| `Deco<Name>GlassGrad` | `Deco<Name>Glass` 자식, stretch, 그라데이션 V 색 `#05070c` 알파 0.30 (두 층 합성 상단 알파 = 1 − (1−0.33)(1−0.30) = 0.531) | `WorldMapGradientV` |
| `Deco<Name>GlassFrame` | `Deco<Name>Glass` 자식, stretch, Sliced, fillCenter 끔, 흰색 알파 0.5 | `BattleHudSmallFrame` |

`<Name>` 은 Quest/Lord/Ranking 의 목록 뒤판 `List`, 영주성 헤더 뒤판 `Header`, 계정 정보 뒤판 `Account` 이다(빌드 모달 `DecoSectionGlass` 와 같은 구성·수치).

### 2.5 K5. 금색 슬림 스크롤바 (Quest · Lord · Ranking 의 `ScrollView` 3곳)

각 `ScrollView` 의 자식으로 추가(기존 `Viewport` 뒤 형제). 기존 `ScrollRect` 의 `Vertical Scrollbar` 에 연결하고 `Vertical Scrollbar Visibility` = Auto Hide(`m_VerticalScrollbarVisibility` 1), `Spacing` 0.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `Scrollbar` | UGUI `Scrollbar`(방향 Bottom To Top). 앵커 (1,0)–(1,1), 피벗 (0,0.5), 위치 (4,0), sizeDelta (6,0) → `ScrollView` 우측선 바깥 4px 에 폭 6, 높이는 `ScrollView` 와 같음. 트랙 Image `Px_Solid` 색 `#05070c`, `raycastTarget` 켬 | `Px_Solid` |
| `SlidingArea` | `Scrollbar` 자식, stretch(오프셋 1 → 폭 4, 높이 −2) | — |
| `Handle` | `SlidingArea` 자식, stretch 0, Image 스프라이트 `BuildModalScrollThumb`, `raycastTarget` 켬 | `BuildModalScrollThumb` |
| `DecoThumbGlow` | `Handle` 자식, stretch 오프셋 −6, 색 `#f7c64a` 알파 0.67 | `UISoftGlow` |
| `DecoScrollTrackFrame` | `Scrollbar` 자식, stretch 오프셋 −1 (폭 8), Sliced, fillCenter 끔, 흰색 | `BattleHudTrackFrame` |

검산: `ScrollView` 우측선 + 4 ~ + 10 = ModalBody x 696~702 ≤ 프레임 안쪽 711. 핸들 폭 4 = 6 − 1×2, 링 폭 8 = 6 + 1×2. 핸들 길이 = 슬라이딩 영역 높이 × 뷰포트 높이 ÷ 콘텐츠 전체 길이:
- Quest: 콘텐츠 = 13×112 + 12×6 = 1528, 슬라이딩 378(= 380 − 2) → 378 × 380 ÷ 1528 = 94.0px.
- Lord: 콘텐츠 = 9×72 + 8×6 = 696(Lv 2~10 의 9행), 슬라이딩 322(= 324 − 2) → 322 × 324 ÷ 696 = 149.9px.
- Ranking: 콘텐츠 = 100×52 = 5200(Top 100), 슬라이딩 246(= 248 − 2) → 246 × 248 ÷ 5200 = 11.7px. 가늘지만 위치 표시 용도이며 휠·드래그 스크롤은 `ScrollRect` 가 그대로 담당한다.

### 2.6 K6. 버튼 몸체 매핑 (계정 팝업 · 랭킹 탭)

| 기존 스프라이트 | 새 스프라이트 | 사용처 | 부가 |
|---|---|---|---|
| `Px_Btn` | `VillageHudBtn`(Sliced) | `ChangeNameButton` · `NameCancelButton` · `ConflictLaterButton` · `RestoreButton`(항상 비활성이지만 같이 교체) | `Shadow` (0,−4) 색 `#000000` 알파 0.47 |
| `Px_BtnSoul` | `CardSelectPickBtnSoul`(Sliced) | `NameConfirmButton` | `Shadow` 동일 + `DecoConfirmGloss` |
| `Px_BtnGold` | `CardSelectPickBtnGold`(Sliced) | `ConflictRestoreButton` | `Shadow` 동일 + `DecoRestoreGloss` |

`DecoConfirmGloss`·`DecoRestoreGloss`: 버튼 자식 인덱스 0(라벨 앞), 상단 stretch, 피벗 (0.5,1), 위치 (0,−4), sizeDelta (−10,10), 그라데이션 V 흰색 알파 0.31 (배치 A `DecoConfirmGloss` 와 같은 값). 색은 흰색 `Image` 색 그대로이고 버튼 글자(흰색 f22)는 불변이다(카드 선택·배치 A 확정 버튼과 같은 결정 — 밝은 민트 면 위 흰 글자의 대비는 §13 확인 항목).

---

## 3. QuestPopup · QuestCell

### 3.1 QuestPopup (`QuestPopup.prefab`)

`ModalBody` 자식 최종 순서(뒤 → 앞): K1 의 앞부분(`DecoModalGrad` … `Title`) → `DecoListGlass` → `ScrollView`(자식 `Viewport` → `Scrollbar`) → `DecoCloseGlow` → `CloseButton`(자식 `X` → `DecoCloseGlyph`).

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 프레임·제목판·닫기 | §2.1~2.3 그대로 | K1~K3 |
| `DecoListGlass` | K4 `<Name>` = `List`. 중심 (360,262) 크기 672x388 (좌상단 (24,68), 목록 영역 x 28~692·y 72~452 보다 사방 4px 크다) | K4 |
| 스크롤바 | K5 | K5 |

검산: 유리 우측 끝 24 + 672 = 696 = 스크롤바 왼쪽 끝(692 + 4). 유리 하단 68 + 388 = 456 < 프레임 안쪽 471. 제목판 하단 26 < 유리 상단 68. 셀 폭 664 이 유리 안(672)에 사방 4px 여백으로 놓인다.

### 3.2 QuestCell (`QuestCell.prefab`, 664x112, 셀 좌상단 기준)

루트 자식 최종 순서(뒤 → 앞): `DecoCellGrad` → `DecoCellGloss` → `DecoStateStrip` → `DecoAchieved`(자식 `DecoAchievedStripGlow` → `DecoAchievedStrip` → `DecoAchievedBadgeGlow`) → `NameText` → `DescText` → `RewardText` → `BadgeBg` → `AchievedBadge` → `ProgressRoot`(자식 `DecoProgressGlow` → `Track` → `Fill` → `DecoProgressFrame` → `ProgressText`). 루트 Image(= `_background`)가 가장 먼저 그려진다.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 셀 면·테두리 | 루트 Image 스프라이트 `Px_Panel` → `SynergyModalCellBg`(Sliced), 색 흰색(코드가 달성 시 `(0.72,1,0.84)` 로 곱함). **`_normalSprite` 필드의 값도 `SynergyModalCellBg` 로 바꾼다**(코드가 매 Bind 에 대입). `raycastTarget` 켬 유지. `Shadow` (0,−2) 색 `#000000` 알파 0.40 | `SynergyModalCellBg` | 기존 `_background`·`_normalSprite` |
| `DecoCellGrad` | stretch(좌 3 · 우 3 · 상 5 · 하 6), 그라데이션 H 색 `#222c44` 알파 1.0 | `WorldMapGradientH` | 없음 |
| `DecoCellGloss` | 중심 (332,30) 658x50, 그라데이션 V 흰색 알파 0.05 | `WorldMapGradientV` | 없음 |
| `DecoStateStrip` | 중심 (8,56) 4x96, 색 `#48546e` (진행 중/미달성의 석판색) | `Px_Solid` | 없음 |
| `DecoAchieved` | 달성 장식 묶음 루트: stretch, Graphic 없음, **프리팹 기본 비활성**. 코드가 `Achieved` 일 때만 활성 | — | **신규 `_achievedDecor`** |
| `DecoAchievedStripGlow` | `DecoAchieved` 자식, 중심 (8,56) 32x120, 색 `#5ef0b4` 알파 0.50 | `UISoftGlow` | 없음 |
| `DecoAchievedStrip` | `DecoAchieved` 자식, 중심 (8,56) 4x96, 색 `#5ef0b4` (`DecoStateStrip` 위를 덮는다) | `Px_Solid` | 없음 |
| `DecoAchievedBadgeGlow` | `DecoAchieved` 자식, 중심 (606,28) 108x68, 색 `#5ef0b4` 알파 0.35 | `UISoftGlow` | 없음 |
| 이름·설명·보상 | 기존 `NameText`·`DescText`·`RewardText` 불변 | — | 기존 |
| 달성 배지 몸체 | 기존 `BadgeBg` Image 스프라이트 `Px_Badge_Soul` → `SynergyModalBadge`(Sliced), 색 `#5ef0b4`(정적; 어두운 소울 유리 칩 + 소울 테두리). 76x36 불변. 기존 코드가 `SetActive(Achieved)` | `SynergyModalBadge` | 기존 `_badgeBg` |
| 달성 배지 글자 | 기존 `AchievedBadge`("달성" f16, 코드가 `UiDotPalette.Soul`) 불변 | — | 기존 `_achievedBadge` |
| 진행 바 발광 | `ProgressRoot` 자식 인덱스 0 `DecoProgressGlow`: 루트 중심 (110,10) 252x52, 색 `#5ef0b4` 알파 0.25. 부모(`ProgressRoot`)가 `HasProgress` 로 켜고 꺼지므로 함께 토글됨 | `UISoftGlow` | 없음 |
| 진행 바 트랙 | 기존 `Track` Image 스프라이트 `Px_BarBg` → `Px_Solid`(Simple), 색 `#080b12` | `Px_Solid` | 없음 |
| 진행 바 채움 | 기존 `Fill` Image 스프라이트 `Px_BarFill` → `BattleHudGaugeFill`, **색 `#5ef0b4` 유지**(회색조 스프라이트 × 소울 색 = 위가 밝은 광택 채움), Image Type Filled/Horizontal·`fillAmount` 코드 칠하기 불변 | `BattleHudGaugeFill` | 기존 `_progressFill` |
| `DecoProgressFrame` | `ProgressRoot` 자식, `Fill` 바로 뒤, stretch, Sliced, fillCenter 끔, 흰색 | `VillageHudBarFrame` | 없음 |
| 진행 글자 | 기존 `ProgressText` 불변 | — | 기존 `_progressText` |

검산: 셀 베벨 안쪽 영역 가로 3~661(폭 658 = 664 − 3 − 3), 세로 5~106(높이 101 = 112 − 5 − 6) — `SynergyModalCellBg` 의 border (좌3·하6·우3·상5)와 같다. 스트립 y 8~104 은 베벨 안쪽(5~106) 안, x 6~10 은 `NameText` 시작 x 20 보다 10 왼쪽. 배지 중심: x = 664 − 20 − 38 = 606, y = 10 + 18 = 28. 배지 글로우 108 = 76 + 2×16, 68 = 36 + 2×16. 진행 바 루트 x 20~240·y 80~100 → 중심 (130,90). 글로우는 루트 자식이므로 루트 중심 기준 (110,10) 이고 252 = 220 + 2×16, 52 = 20 + 2×16. `Fill` 은 트랙 안쪽 4px(sizeDelta −8) 이고 프레임 두께 3px 이므로 트랙 어두운 면 1px 이 프레임과 채움 사이에 남는다. 셀 간격 `_itemGap` 6 안에 `Shadow` 2px 가 들어간다.

### 3.3 상태별 표시 (QuestCell)

| 상태 | `Achieved` / `HasProgress` | 루트 면 | 띠 | `DecoAchieved` | 배지 | 진행 바 |
|---|---|---|---|---|---|---|
| 진행 중 | false / true | `SynergyModalCellBg` 흰색 | `DecoStateStrip` 석판색 | 꺼짐 | 꺼짐 | 켜짐 + 소울 발광 + `fillAmount = Current/Target` |
| 미달성·진행 바 없음 | false / false | 흰색 | 석판색 | 꺼짐 | 꺼짐 | 꺼짐 |
| 달성 | true / false | `(0.72,1,0.84)` 곱(코드 기존) | 소울 띠가 석판 띠를 덮음 + 띠·배지 소울 발광 | 켜짐 | 켜짐(소울 칩 + "달성") | 꺼짐 |

---

## 4. LordLevelPopup · LordRewardCell

### 4.1 LordLevelPopup (`LordLevelPopup.prefab`)

`ModalBody` 자식 최종 순서(뒤 → 앞): K1 앞부분(`DecoModalGrad` … `Title`) → `DecoHeaderGlass` → `DecoLevelGlow` → `LordLevelText` → `DecoListGlass` → `ScrollView`(자식 `Viewport` → `Scrollbar`) → `DecoXpGlow` → `LordXpBar`(자식 `LordXpFill` → `XpSeg1`…`XpSeg9` → `DecoXpFrame`) → `LordXpNextText` → `DecoCloseGlow` → `CloseButton`.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 프레임·제목판·닫기 | §2.1~2.3 그대로 | K1~K3 |
| `DecoHeaderGlass` | K4 `<Name>` = `Header`. 중심 (360,87) 크기 672x74 (좌상단 (24,50)) | K4 |
| `DecoLevelGlow` | `ModalBody` 자식, `LordLevelText` 바로 앞 형제. 중심 (138,83) 260x70, 색 `#f7c64a` 알파 0.30 | `UISoftGlow` |
| `LordLevelText` | 기존 불변 (f33 `#f7c64a`) | 기존 `_lordLevelText` |
| `DecoListGlass` | K4 `<Name>` = `List`. 중심 (360,290) 크기 672x328 (좌상단 (24,126)) | K4 |
| 스크롤바 | K5 | K5 |
| `DecoXpGlow` | `ModalBody` 자식, `LordXpBar` 바로 앞 형제. 중심 (476,78) 456x40, 색 `#f7c64a` 알파 0.18 | `UISoftGlow` |
| XP 바 트랙 | 기존 `LordXpBar` Image 스프라이트 `Px_BarBg` → `Px_Solid`(Simple), 색 `#080b12` (마을 HUD `XpBarBg` 와 같은 값) | `Px_Solid` |
| XP 바 채움 | 기존 `LordXpFill` Image 스프라이트 `Px_BarFill` → `VillageHudXpFill`, **색 `#f7c64a` → `#ffffff`**(스프라이트가 금색 그라데이션을 굽고 있음, 코드는 `fillAmount` 만 칠하고 색은 건드리지 않음), Image Type Filled/Horizontal 불변 | `VillageHudXpFill` |
| 구간 눈금 | 기존 `XpSeg1`~`XpSeg9`(검정 알파 0.6, 폭 4) 불변 | 기존 |
| `DecoXpFrame` | `LordXpBar` 자식, **`XpSeg9` 바로 뒤(마지막)**, stretch, Sliced, fillCenter 끔, 흰색 | `VillageHudBarFrame` |
| 다음 레벨 글자 | 기존 `LordXpNextText` 불변 | 기존 `_xpNextText` |

검산: 헤더 유리 x 24~696·y 50~124 안에 `LordLevelText`(x 28~248·y 60~106) · `LordXpBar`(x 260~692·y 66~90) · `LordXpNextText`(x 260~692·y 96~118)가 모두 든다(여백: 좌 4, 우 4, 상 10, 하 6). 레벨 글로우 중심 = (28 + 110, 60 + 23) = (138,83), 260 = 220 + 2×20, 70 = 46 + 2×12. XP 글로우 중심 = (260 + 216, 66 + 12) = (476,78), 456 = 432 + 2×12, 40 = 24 + 2×8. 목록 유리 y 126~454 이 헤더 유리 하단 124 보다 2 아래이고 목록 영역 y 128~452 을 사방 2px 감싼다 (좌우는 4px). `LordXpFill` 424x16 은 `VillageHudBarFrame` 3px 프레임 안쪽 4px 에 놓인다.

### 4.2 LordRewardCell (`LordRewardCell.prefab`, 664x72, 셀 좌상단 기준)

루트 자식 최종 순서(뒤 → 앞): `StateRing`(자식 `DecoCurrentGlow`) → `DecoLevelPlate`(자식 `DecoLevelPlateFrame`) → `LevelText` → `NameText` → `RewardText` → `BadgeBg` → `ReachedBadge` → `SubText`.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 셀 면(달성) | 루트 Image 스프라이트 `Px_Panel` → `SynergyModalCellBg`(Sliced), 흰색. **`_normalSprite` 필드의 값도 `SynergyModalCellBg`** (코드가 도달한 행에 대입). `raycastTarget` 켬 유지. 루트에 `Shadow` (0,−2) 색 `#000000` 알파 0.40 | `SynergyModalCellBg` | 기존 `_background`·`_normalSprite` |
| 셀 면(미도달·잠금 더미) | **`_pendingSprite` 필드의 값을 `Px_PanelDark` → `VillageHudBtnOff`** (회색 무광 평판; 코드가 미도달·잠금 더미 행에 대입). 프리팹 루트 Image 의 초기 스프라이트는 `SynergyModalCellBg` | `VillageHudBtnOff` | 기존 `_pendingSprite` |
| 현재 레벨 프레임 | 기존 `StateRing` Image 스프라이트 `Px_Ring` → `CardSelectFrame`(Sliced, fillCenter 끔). 색은 기존 코드가 `UiDotPalette.Gold` 로 칠함 → 먹색 3 + 금 2 + 먹색 2 의 이중 프레임. 활성은 기존 코드(`IsCurrent`) | `CardSelectFrame` | 기존 `_stateRing` |
| `DecoCurrentGlow` | `StateRing` 자식(현재 레벨 행에서만 함께 켜짐), 중심 (332,36) 688x96, 색 `#f7c64a` 알파 0.18 | `UISoftGlow` | 없음 |
| `DecoLevelPlate` | `StateRing` 바로 뒤 형제, 중심 (60,36) 88x40, Simple, 색 `#05070c` 알파 0.80. 자식 `DecoLevelPlateFrame`: stretch, Sliced, fillCenter 끔, 흰색 | `Px_Solid` · `BattleHudTrackFrame` | 없음 |
| 레벨·이름·보상·부제 | 기존 `LevelText`·`NameText`·`RewardText`·`SubText` 불변 | — | 기존 |
| 달성 배지 몸체 | 기존 `BadgeBg` Image 스프라이트 `Px_Badge_Soul` → `SynergyModalBadge`(Sliced). **`_badgeSoulSprite`·`_badgeGoldSprite` 두 필드의 값도 모두 `SynergyModalBadge`**. 배지 색은 코드가 칠함(§9: 현재 레벨 `#f7c64a` / 그 외 `#5ef0b4`). 프리팹 초기 색은 `#5ef0b4` | `SynergyModalBadge` | 기존 `_badgeBg`·`_badgeSoulSprite`·`_badgeGoldSprite` |
| 달성 배지 글자 | 기존 `ReachedBadge` 불변(코드가 현재 레벨 `Gold` / 그 외 `Soul`) | — | 기존 `_reachedBadge` |

검산: 레벨 칸 글자 박스 x 20~100·y 20~52(셀 중앙 36 ± 16) 에 대해 판 x 16~104·y 16~56 → 사방 4px 여백. `NameText`·`SubText` 시작 x 108 은 판 우측 104 보다 4 오른쪽. `SynergyModalCellBg` 의 세로 border 5 + 6 = 11 ≤ 72. `VillageHudBtnOff` 의 border (6,7,6,6) 에서 세로 13 ≤ 72, 가로 12 ≤ 664. `CardSelectFrame` border 7 → 높이 72 에서 안쪽 58. 글로우 688 = 664 + 2×12, 96 = 72 + 2×12.

### 4.3 상태별 표시 (LordRewardCell)

| 상태 | 데이터 | 루트 면(코드 대입) | `StateRing` | 레벨 글자 | 이름 글자 | 보상 글자 | 배지 | 부제 |
|---|---|---|---|---|---|---|---|---|
| 잠금 더미 | `IsLockedDummy` | `VillageHudBtnOff` 흰색 | 꺼짐 | `#9ca3af` | `#9ca3af` | 숨김(빈 문자열) | 꺼짐 | 숨김(빈 문자열) |
| 미도달 | `Reached == false` | `VillageHudBtnOff` 흰색 | 꺼짐 | `#9ca3af` | 흰색 | `#9ca3af` | 꺼짐 | "N XP 남음" |
| 수령 완료 | `Reached && IsCurrent == false` | `SynergyModalCellBg` 흰색 알파 0.65 | 꺼짐 | `#f7c64a` | 흰색 | 소울 | 켜짐, 소울 칩 | "수령 완료" |
| 현재 레벨 | `IsCurrent` | `SynergyModalCellBg` 흰색 알파 1.0 | 켜짐 + `DecoCurrentGlow` | `#f7c64a` | 흰색 | 소울 | 켜짐, **금 칩** | "현재 레벨" |

(`#9ca3af` = 기존 코드 `DummyTextColor` (0.612,0.639,0.686).) 수령 완료 행의 알파 0.65 는 루트 면(과 그 `Shadow`)만 흐리게 한다. 이 행에는 자식 장식이 없어 모순이 생기지 않는다(`DecoLevelPlate` 는 어두운 판이라 흐려진 면 위에서도 자연스럽다). 이 때문에 이 셀에는 전체 면 그라데이션 계열 장식을 두지 않는다.

---

## 5. RankingPopup · RankingCell

### 5.1 RankingPopup (`RankingPopup.prefab`)

`ModalBody` 자식 최종 순서(뒤 → 앞): K1 앞부분(`DecoModalGrad` … `Title`) → `Header`(자식 `DecoHeaderGrad` → `DecoHeaderFrame` → `DecoHeaderLine` → `HRank` → `HName` → `HTime` → `HHero`) → `DecoListGlass` → `ScrollView`(자식 `Viewport` → `Scrollbar`) → `Divider`(자식 `DecoDividerCore` → `DecoDividerGem`) → `MyRankRow`(자식 `DecoMyRankGlow` → 기존 `RankingCell`) → `EmptyText`(자식 `DecoEmptyGemTop` → `DecoEmptyGemBottom`) → `StageTabRow`(탭 6개) → `DecoCloseGlow` → `CloseButton`.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 프레임·제목판·닫기 | §2.1~2.3 그대로 | K1~K3 |
| 탭 몸체(꺼짐) | 6개 탭(`StageTab1`~`5`·`OverallTab`)의 Image 스프라이트 `Px_Tab` → `VillageHudBtnOff`(Sliced), 색 흰색, `raycastTarget` 켬 유지. `Shadow` (0,−4) 색 `#000000` 알파 0.47. Toggle 설정(Transition None · Fade · ToggleGroup)·`Label`(f17 `#e8e1cf`) 불변 | `VillageHudBtnOff` |
| 탭 선택 표시 | 6개 탭의 자식 `Checkmark` Image 스프라이트 `Px_TabOn` → `CardSelectChip`(Sliced), **색 `#f7c64a`** (알파 1; Toggle 의 Fade 가 알파를 구동). 켜진 탭 = 어두운 유리 면 + 금색 65% 테두리, 글자는 그대로 읽힘 | `CardSelectChip` |
| `Header` 면 | 기존 `Header` Image 스프라이트 `Px_PanelDark` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.96. `raycastTarget` 꺼짐 유지 | `Px_Solid` |
| `DecoHeaderGrad` | `Header` 자식 인덱스 0, stretch, 그라데이션 V 색 `#1a2133` 알파 0.95 | `WorldMapGradientV` |
| `DecoHeaderFrame` | `Header` 자식 인덱스 1, stretch, Sliced, fillCenter 끔, 흰색 | `BattleHudSmallFrame` |
| `DecoHeaderLine` | `Header` 자식 인덱스 2, 중심 (332,35) 664x2, 그라데이션 H 색 `#f7c64a` 알파 0.80 (헤더 하단에 붙는 금 페이드 라인) | `WorldMapGradientH` |
| `DecoListGlass` | K4 `<Name>` = `List`. 중심 (360,272) 크기 672x252 (좌상단 (24,146)) | K4 |
| 스크롤바 | K5 | K5 |
| 구분선 면 | 기존 `Divider` Image 스프라이트 `Px_Divider` → `CardSelectFadeLine`(Simple), 색 `#f7c64a` 알파 0.35 (664x8 의 부드러운 금 띠). 크기·위치 불변 | `CardSelectFadeLine` |
| `DecoDividerCore` | `Divider` 자식 인덱스 0, 중심 (332,4) 664x2, 색 `#f7c64a` 알파 1.0 | `CardSelectFadeLine` |
| `DecoDividerGem` | `Divider` 자식 인덱스 1, 중심 (332,4) 14x14, 색 `#f7c64a` | `VillageHudGem` |
| `DecoMyRankGlow` | `MyRankRow` 자식 인덱스 0(기존 `RankingCell` 앞), 중심 (332,26) 704x84, 색 `#5ef0b4` 알파 0.22. 컨테이너가 코드로 켜고 꺼지므로 함께 토글됨 | `UISoftGlow` |
| 빈 상태 보석 | `EmptyText` 자식 `DecoEmptyGemTop`: 중앙 앵커 위치 (0,+44) 22x22 색 `#8a6a2c`. `DecoEmptyGemBottom`: 위치 (0,−44) 14x14 색 `#8a6a2c`. `EmptyText` 가 `SetActive` 로 켜고 꺼지므로 함께 토글됨. 문구는 기존 코드가 `SetText` | `VillageHudGem` |
| 셀 | `RankingCell.prefab`(§5.2) | — |

검산: 목록 유리 y 146~398 이 `Header` 하단 144 보다 2 아래, `Divider` 상단 400 보다 2 위. 구분선 코어 중심 y = 400 + 4 = 404. 빈 상태 보석: `EmptyText` 중심 y 285 → 위 보석 중심 y 241(= 285 − 44), 아래 보석 329(= 285 + 44) — 헤더(144)·구분선(400) 사이에 든다. 마이랭크 글로우 704 = 664 + 2×20, 84 = 52 + 2×16 → 글로우 상단 y 416 − 16 = 400 = 구분선 상단. 탭 `Shadow` 4px 는 탭 줄(y 60~100) 아래 `Header` 상단(108) 보다 위에서 끝난다(100 + 4 = 104). `VillageHudBtnOff` border 세로 6 + 7 = 13 ≤ 40.

### 5.2 RankingCell (`RankingCell.prefab`, 648x52, 셀 좌상단 기준)

루트 자식 최종 순서(뒤 → 앞): `MineRing`(자식 `DecoMineGlow`) → `DecoRankMedal`(자식 `DecoMedalGlow` → `DecoMedalDisc` → `DecoMedalCore`) → `RankText` → `NameText` → `TimeText` → `HeroText`.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 행 면(일반) | 루트 Image 스프라이트 `Px_PanelDark` → `SynergyModalCellBg`(Sliced), 흰색. **`_normalSprite` 필드의 값도 `SynergyModalCellBg`**. `raycastTarget` 켬 유지. 행 간격이 0 이라 `Shadow` 는 두지 않는다(스프라이트에 아래 음영이 구워져 있음) | `SynergyModalCellBg` | 기존 `_background`·`_normalSprite` |
| 행 면(내 행) | **`_mineSprite` 필드의 값을 `Px_Panel` → `VillageHudChip`** (소울 테두리 + 어두운 녹색 면; 코드가 내 행에 대입) | `VillageHudChip` | 기존 `_mineSprite` |
| 내 행 링 | 기존 `MineRing` Image 스프라이트 `Px_Ring` → `CardSelectFrame`(Sliced, fillCenter 끔). 색·활성은 기존 코드(`UiDotPalette.Soul`, 내 행만) | `CardSelectFrame` | 기존 `_mineRing` |
| `DecoMineGlow` | `MineRing` 자식(내 행에서만 함께 켜짐), 중심 (324,26) 680x84, 색 `#5ef0b4` 알파 0.15 | `UISoftGlow` | 없음 |
| `DecoRankMedal` | 메달 묶음 루트: 좌중앙 앵커 피벗 (0.5,0.5) 위치 (45,0) 크기 64x64, Graphic 없음, **프리팹 기본 비활성**. 코드가 순위 1~3 에서만 활성 | — | **신규 `_rankMedalRoot`** |
| `DecoMedalGlow` | `DecoRankMedal` 자식, 중앙 64x64, 색 `#ffffff` 알파 0.50 → **코드가 순위색 rgb 로 틴트** | `UISoftGlow` | **신규 `_rankMedalTinted[0]`** |
| `DecoMedalDisc` | `DecoRankMedal` 자식, 중앙 38x38, 색 `#ffffff` 알파 1.0 → **코드가 순위색 rgb 로 틴트**(회색조 메달 × 금/은/동 = 순위색 테두리 원) | `WorldMapMedalOpen` | **신규 `_rankMedalTinted[1]`** |
| `DecoMedalCore` | `DecoRankMedal` 자식, 중앙 24x24, 흰색(틴트 없음). 어두운 원판이 순위 숫자의 바탕이 됨 | `WorldMapMedalLocked` | 없음 |
| 순위·이름·시간·영웅 | 기존 `RankText`·`NameText`·`TimeText`·`HeroText` 불변 (순위 글자색은 기존 코드가 금 `#FFD24A` / 은 `#C9CDD2` / 동 `#D08B5B` / 기본 흰색) | — | 기존 |

검산: 순위 칸 글자 박스는 앵커 x 0~0.14(= 0~90.7)에서 sizeDelta −12 → 중심 x 45.4. 메달 중심 (45,26): 원판 38 은 y 7~45 로 행 높이 52 안(여백 7). 테두리 폭 = (38 − 24) ÷ 2 = 7px(그중 `WorldMapMedalOpen` 의 먹색 외곽 8/128 × 38 = 2.4px). 코어 24 안에 `RankText`(f22, 숫자 1자) 가 든다. 글로우 64 = 38 + 2×13. `SynergyModalCellBg` border 세로 5 + 6 = 11 ≤ 52, `VillageHudChip` border 5 → 세로 10 ≤ 52. 내 행 글로우 680 = 648 + 2×16, 84 = 52 + 2×16.

순위색 상수(코드 기존, 변경 없음): 1위 `#FFD24A` (1,0.823,0.29) · 2위 `#C9CDD2` (0.788,0.804,0.823) · 3위 `#D08B5B` (0.816,0.545,0.357).

### 5.3 상태별 표시 (RankingCell)

| 상태 | 입력 | 행 면(코드 대입) | `MineRing` | 메달 | 순위 칸 |
|---|---|---|---|---|---|
| 일반(4위 이하) | `rank >= 4`, `IsMine == false` | `SynergyModalCellBg` | 꺼짐 | 꺼짐 | 숫자, 흰색 |
| 1·2·3위 | `rank` 1~3 | `SynergyModalCellBg` | 꺼짐 | 켜짐: 글로우·원판 rgb = 금/은/동 | 숫자, 금/은/동 |
| 내 행 | `IsMine == true` | `VillageHudChip` | 켜짐 + `DecoMineGlow` | 순위에 따름 | 순위에 따름, 이름 뒤 " (나)" |
| 미등재(내 행) | `Unranked == true` | `VillageHudChip` | 켜짐 + `DecoMineGlow` | 꺼짐 | "랭킹 없음" 흰색, 시간·영웅 "-" |
| 고정 "내 순위" 영역 | `MyRankRow` 활성 | 내 행과 같음 + `DecoMyRankGlow` | 켜짐 | 순위에 따름 | 순위에 따름 |
| 빈 목록·오프라인 | `EmptyText` 활성 | — | — | — | 문구 + 위아래 보석 |

---

## 6. CloudPopup (`CloudPopup.prefab`)

`ModalBody` 자식 최종 순서(뒤 → 앞, **기존 상대 순서 불변**): K1 앞부분(`DecoModalGrad` … `Title`) → `DecoAccountGlass` → `DecoAccountSep` → `ConnectionText` → `ConflictDot` → `DisplayNameText` → `ChangeNameButton`(자식 `Label`) → `RestoreButton` → `NameEditGroup` → `ConflictGroup` → `DecoConnectionGlow` → `ConnectionDot` → `DecoCloseGlow` → `CloseButton`.

### 6.1 상단 정보 영역

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 프레임·제목판·닫기 | §2.1~2.3 그대로 | K1~K3 | 기존 `_backButton` 등 |
| `DecoAccountGlass` | K4 `<Name>` = `Account`. 중심 (360,144) 크기 688x120 (좌상단 (16,84), 연결 줄 + 표시명 줄 + 변경 버튼(우측 끝 x 700)을 감싼다) | K4 | 없음 |
| `DecoAccountSep` | 중심 (360,141) 648x2, 그라데이션 H 색 `#f7c64a` 알파 0.60 (연결 줄 하단 132 과 표시명 줄 상단 150 의 중간) | `WorldMapGradientH` | 없음 |
| 연결 글자·표시명 | 기존 `ConnectionText`·`DisplayNameText` 불변 | — | 기존 |
| 연결 점 몸체 | 기존 `ConnectionDot` Image 스프라이트 `Px_Dot` → `WorldMapMedalOpen`(Simple), 20x20 불변. 색은 기존 코드가 칠함(연결됨 `#5ef0b4` / 오프라인 `#e5484d`) → 회색조 입체 원판 × 상태색 | `WorldMapMedalOpen` | 기존 `_connectionDot` |
| `DecoConnectionGlow` | `ConnectionDot` 바로 앞 형제(= `ConflictGroup` 뒤), 중심 (44,112) 44x44, 색 `#5ef0b4` 알파 0.50 → **코드가 점과 같은 rgb 로 틴트**(알파 보존) | `UISoftGlow` | **신규 `_connectionGlow`** |
| 충돌 점 | 기존 `ConflictDot` Image 스프라이트 `Px_Dot` → `WorldMapMedalBoss`, 색 `#e5484d` → `#ffffff`(스프라이트가 붉은 입체를 굽고 있음). 크기·위치·기본 비활성 불변. 발광·펄스 없음(§13 확인 항목) | `WorldMapMedalBoss` | 기존 `_conflictDot` |
| 변경 버튼 | K6 (`VillageHudBtn` + `Shadow`) | K6 | 기존 `_changeNameButton` |
| 복원 버튼 | K6 (`VillageHudBtn` + `Shadow`), 코드가 항상 비활성 | K6 | 기존 `_restoreButton` |

검산: 계정 유리 x 16~704·y 84~204 안에 `ConnectionText`(x 68~692·y 92~132) · `DisplayNameText`(x 30~520·y 150~198) · `ChangeNameButton`(x 560~700·y 150~198)이 든다(여백: 좌 14, 우 4, 상 8, 하 6). 유리 폭 688 = 720 − 2×16. 구분선 x 36~684 (폭 648). 점 글로우 44 = 20 + 2×12, 중심 = 점 중심 (44,112).

### 6.2 이름 편집 그룹 (`NameEditGroup`, 520x182, 기본 비활성)

자식 최종 순서(뒤 → 앞): `DecoEditGrad` → `DecoEditFrame` → `NameInput`(자식 `DecoInputFrame` → `TextArea`) → `DecoConfirmGlow` → `NameConfirmButton`(자식 `DecoConfirmGloss` → `Label`) → `NameCancelButton`.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 그룹 면 | 기존 `NameEditGroup` Image 스프라이트 `Px_PanelDark` → `Px_Solid`(Simple), 색 `#0d1220` 알파 1.0 (불투명) | `Px_Solid` |
| `DecoEditGrad` | 그룹 자식 인덱스 0, stretch, 그라데이션 V 색 `#1a2133` 알파 1.0 | `WorldMapGradientV` |
| `DecoEditFrame` | 그룹 자식 인덱스 1, stretch, Sliced, fillCenter 끔, 흰색 | `BattleHudBossFrame` |
| 입력창 면 | 기존 `NameInput` Image 스프라이트 `Px_PanelSunk` → `Px_Solid`(Simple), 색 `#05070c`. TMP_InputField·`TextArea`·`Text` 불변 | `Px_Solid` |
| `DecoInputFrame` | `NameInput` 자식 인덱스 0(`TextArea` 앞), stretch, Sliced, fillCenter 끔, 흰색 | `WorldMapInset` |
| `DecoConfirmGlow` | 그룹 자식, `NameConfirmButton` 바로 앞 형제. 중심 (130,132) 248x76, 색 `#5ef0b4` 알파 0.45 | `UISoftGlow` |
| 확인 버튼 | K6 (`CardSelectPickBtnSoul` + `Shadow` + `DecoConfirmGloss`) | K6 |
| 취소 버튼 | K6 (`VillageHudBtn` + `Shadow`). 글로우 없음(주 행동 강조) | K6 |

검산: 글로우 248 = 220 + 2×14, 76 = 48 + 2×14. 확인 버튼 중심 (130,132) = 그룹 좌하단 앵커 (130,50) → y = 182 − 50. 입력창 박스 x 20~500·y 26~78 (중심 (260,52), 480x52)이 그룹 안.

### 6.3 충돌 권유 그룹 (`ConflictGroup`, 680x252, 기본 비활성)

자식 최종 순서(뒤 → 앞): `DecoConflictGlow` → `DecoConflictGrad` → `ConflictRing` → `ConflictText` → `DecoRestoreGlow` → `ConflictRestoreButton`(자식 `DecoRestoreGloss` → `Label`) → `ConflictLaterButton` → `ConflictCompare`(자식 `ConflictLocalBox`·`ConflictCloudBox`).

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 그룹 면 | 기존 `ConflictGroup` Image 스프라이트 `Px_Panel` → `Px_Solid`(Simple), **색 `(1,0.62,0.62)` → `#150b10` 알파 1.0**. 불투명을 유지해 뒤의 연결·표시명 줄을 가리는 현재 동작을 보존. `raycastTarget` 켬 유지 | `Px_Solid` |
| `DecoConflictGlow` | 그룹 자식 인덱스 0, 중심 (340,126) 744x316, 색 `#e5484d` 알파 0.18 | `UISoftGlow` |
| `DecoConflictGrad` | 그룹 자식 인덱스 1, stretch, 그라데이션 V 색 `#3a1a20` 알파 1.0 (위 `#3a1a20` → 아래 `#150b10`) | `WorldMapGradientV` |
| 경고 프레임 | 기존 `ConflictRing` Image 스프라이트 `Px_Ring` → `CardSelectFrame`(Sliced, fillCenter 끔). 색 `#e5484d` 유지 → 먹색 + 붉은 2px + 먹색 | `CardSelectFrame` |
| 경고 글자 | 기존 `ConflictText`(f17 `#ff9a9d`) 불변 | 기존 |
| `DecoRestoreGlow` | `ConflictRestoreButton` 바로 앞 형제, 중심 (150,202) 288x72, 색 `#f7c64a` 알파 0.45 | `UISoftGlow` |
| 복원 버튼 | K6 (`CardSelectPickBtnGold` + `Shadow` + `DecoRestoreGloss`) | K6 |
| 나중에 버튼 | K6 (`VillageHudBtn` + `Shadow`). 글로우 없음 | K6 |
| 비교 칸 면 | 기존 `ConflictLocalBox`·`ConflictCloudBox` Image 스프라이트 `Px_PanelSunk` → `Px_Solid`(Simple), 색 `#0d121c`. 각 박스 자식 `DecoCompareTop`(인덱스 0, stretch, 그라데이션 V 색 `#080b12` 알파 1.0), `DecoCompareFrame`(인덱스 1, stretch, Sliced, fillCenter 끔, 흰색) | `Px_Solid` · `WorldMapGradientV` · `WorldMapInset` |
| 비교 칸 글자 | 기존 `Label`("이 기기"/"클라우드")·`ConflictLocalText`·`ConflictCloudText` 불변 | 기존 |

검산: 그룹 글로우 744 = 680 + 2×32, 316 = 252 + 2×32. 복원 버튼 중심 (150,202) = 그룹 좌하단 앵커 (150,50) → y = 252 − 50, 글로우 288 = 260 + 2×14, 72 = 44 + 2×14. 비교 칸은 그룹 x 16~664·y 96~168, 버튼 줄은 y 180~224(복원) 이고 그룹 하단 252 안.

### 6.4 상태별 표시 (CloudPopup)

| 상태 | 입력 | 표시 |
|---|---|---|
| 연결됨 | `IsConnected == true` | "연결됨", `ConnectionDot`·`DecoConnectionGlow` 소울색 |
| 오프라인 | `IsConnected == false` | "오프라인", 점·글로우 피색 `#e5484d` |
| 충돌 없음 | `ConflictPending == false` | `ConflictGroup`·`ConflictDot` 꺼짐, 정보 영역만 보임 |
| 충돌 | `ConflictPending == true` | `ConflictGroup` 켜짐(붉은 경고 패널 + 금색 복원 CTA), `ConflictDot` 켜짐(붉은 입체 점), 비교 칸은 두 요약이 모두 있을 때만 |
| 이름 편집 | `SetNameEditActive(true)` | `NameEditGroup` 켜짐(불투명 패널, 소울 확인 CTA) |
| 확정 왕복 중 | `NameConfirmButton.Interactable == false` | 기존 `Selectable` 비활성 색(스프라이트 불변) |

---

## 7. 스프라이트: 신규 0건, 재사용 목록

신규 스프라이트 0건이므로 `Assets/_Lair/Editor/` 에 일회용 생성 스크립트를 만들지 않는다. 이 4개 화면이 쓰는 스프라이트는 전부 기존 파일이며 임포트 설정을 수정하지 않는다. 이제 이 화면들도 참조하므로 삭제·리네임 금지.

| 스프라이트 | 위치 | 이 문서에서의 용도 |
|---|---|---|
| `Px_Solid` | `Art/Sprites/UiDot/` | 모달·제목판·유리·셀 판·입력창·비교 칸 바탕, 트랙, 띠 |
| `UISoftGlow` | `Art/Sprites/` | 모든 발광 |
| `WorldMapGradientV` / `WorldMapGradientH` | 〃 | 바탕 그라데이션, 광택, 헤더 라인, 구분선, 경고 그라데이션 |
| `WorldMapVignette` | 〃 | 모달 내측 비네트 |
| `WorldMapFrame` / `WorldMapCornerGold` | 〃 | 모달·제목판 금테, 모서리 4 |
| `WorldMapInset` | 〃 | 입력창·비교 칸 테두리 |
| `WorldMapMedalOpen` / `WorldMapMedalLocked` / `WorldMapMedalBoss` | 〃 | 연결 점·순위 메달 원판·메달 코어·충돌 점 |
| `VillageHudGem` | 〃 | 제목판·빈 상태·구분선 보석 |
| `VillageHudCta` | 〃 | 닫기 X 몸체 |
| `VillageHudBtn` / `VillageHudBtnOff` | 〃 | 계정 버튼, 랭킹 꺼진 탭, 영주 미도달·잠금 행 면 |
| `VillageHudXpFill` / `VillageHudBarFrame` | 〃 | 영주 XP 채움, 진행 바·XP 바 프레임 |
| `SynergyModalCellBg` / `SynergyModalBadge` | 〃 | 셀 면(Quest/Lord/Ranking), 달성 배지 |
| `BattleHudSmallFrame` / `BattleHudTrackFrame` / `BattleHudBossFrame` / `BattleHudGaugeFill` | 〃 | 유리·헤더 테두리 / 스크롤 트랙·레벨 판 테두리 / 편집 그룹 프레임 / 퀘스트 채움 |
| `CardSelectFrame` / `CardSelectChip` / `CardSelectFadeLine` | 〃 | 현재·내 행·충돌 프레임 / 선택된 탭 / 랭킹 구분선 |
| `CardSelectPickBtnSoul` / `CardSelectPickBtnGold` | 〃 | 확인 CTA / 복원 CTA |
| `VillageHudChip` | 〃 | 랭킹 내 행 면 |
| `BuildModalScrollThumb` / `BuildModalCloseGlyph` | 〃 | 스크롤 핸들 / 닫기 × |

교체되어 더 이상 이 화면들이 쓰지 않는 `Px_Panel` · `Px_PanelDark` · `Px_PanelSunk` · `Px_Plaque` · `Px_CloseX` · `Px_Btn` · `Px_BtnSoul` · `Px_BtnGold` · `Px_Badge_Soul` · `Px_BarBg` · `Px_BarFill` · `Px_Ring` · `Px_Tab` · `Px_TabOn` · `Px_Divider` · `Px_Dot` 는 다른 프리팹이 쓸 수 있으므로 삭제하지 않는다.

---

## 8. 애니메이션

**`UILoopMotion` 사용 0건.** 네 화면 모두 정보 확인용 정지 화면이고 선행 시안에도 상시 움직임이 없다. 풀링 셀(Quest/Lord/Ranking)에 루프 모션을 두면 재사용 시 기준 포즈 저장 시점이 어긋날 위험이 있어(시너지 모달 §4 와 같은 판단) 두지 않는다. 정적 폴백 기준·성능 측정 항목 없음. 상태 의존 표시는 활성 토글(달성 묶음·메달·`MineRing` 자식 글로우·`StateRing` 자식 글로우)과 색 칠하기(배지·메달·연결 글로우)뿐이다.

| 안 | 내용 | 장점 | 단점 | 결정 |
|---|---|---|---|---|
| A | 애니메이션 없음 | 풀링 셀과 충돌 없음, 구현·테스트 최소 | 충돌 점이 정적 | 채택 |
| B | `ConflictDot` 에 `UILoopMotion` Pulse(스케일 1.00→1.18, 주기 1.4초, 마을 HUD `RedDot` 와 동일) | 충돌 알림이 눈에 띔 | `ConflictDot` 이 현재 `CloseButton` 에 대부분 가려져 있어 효과가 거의 안 보임(§13) | 기각 |
| C | 현재 레벨 행 `StateRing` 맥동 | 현재 레벨 강조 | 풀 셀마다 모션 시작/정지 제어 필요, 가독성 이득 없음 | 기각 |

---

## 9. SerializeField · 코드 변경 (View 한정, 최소)

### 9.1 유지

네 팝업과 세 셀의 기존 `[SerializeField]` 전부의 이름·타입·연결 대상 불변:
- `QuestPopup`: `_dimButton` `_closeButton` `_scrollView` (+ `UIBase` 필드)
- `QuestCell`: `_background` `_nameText` `_descText` `_rewardText` `_achievedBadge` `_progressRoot` `_progressFill` `_progressText` `_normalSprite`(값만 `SynergyModalCellBg`) `_badgeBg`
- `LordLevelPopup`: `_dimButton` `_closeButton` `_lordLevelText` `_xpFill` `_xpNextText` `_scrollView`
- `LordRewardCell`: `_background` `_levelText` `_nameText` `_rewardText` `_reachedBadge` `_subText` `_stateRing` `_normalSprite`(값만 `SynergyModalCellBg`) `_pendingSprite`(값만 `VillageHudBtnOff`) `_badgeBg` `_badgeSoulSprite`·`_badgeGoldSprite`(값만 `SynergyModalBadge`)
- `RankingPopup`: `_scrollView` `_emptyText` `_myRankContainer` `_myRankCell` `_stageTabs`(Toggle[5]) `_overallTab` (+ `_backgroundButton` `_backButton`)
- `RankingCell`: `_background` `_rankText` `_nameText` `_timeText` `_heroText` `_normalSprite`(값만 `SynergyModalCellBg`) `_mineSprite`(값만 `VillageHudChip`) `_mineRing`
- `CloudPopup`: `_connectionText` `_displayNameText` `_changeNameButton` `_nameInput` `_nameConfirmButton` `_nameCancelButton` `_nameEditGroup` `_restoreButton` `_conflictGroup` `_conflictText` `_conflictRestoreButton` `_conflictLaterButton` `_conflictDot` `_connectionDot` `_conflictCompare` `_conflictLocalText` `_conflictCloudText` (+ `_backgroundButton` `_backButton`)
- `QuestPoolingScrollView`·`LordRewardPoolingScrollView`·`RankingPoolingScrollView`: `_origin` `_itemGap` `_columnCount` 등 전부 불변.

### 9.2 신규 `[SerializeField] private` (4개, 모두 기존 클래스 안)

| 클래스 | 필드 | 타입 | 용도 | 미할당 시 |
|---|---|---|---|---|
| `QuestCell` | `_achievedDecor` | `GameObject` | `DecoAchieved` 루트. 달성일 때만 활성 | 건너뜀 |
| `RankingCell` | `_rankMedalRoot` | `GameObject` | `DecoRankMedal` 루트. 순위 1~3 에서만 활성 | 건너뜀 |
| `RankingCell` | `_rankMedalTinted` | `Image[]` (원소 2, 순서 `DecoMedalGlow` · `DecoMedalDisc`) | 순위색 rgb 로 틴트(각 원소의 프리팹 알파 보존) | 건너뜀 |
| `CloudPopup` | `_connectionGlow` | `Image` | `DecoConnectionGlow`. 연결 점과 같은 rgb 로 틴트 | 건너뜀 |

`LordRewardCell` 은 신규 필드 0건이다. 신규 필드는 모두 비어 있어도 예외 없이 동작해야 한다(기존 리플렉션/빈 컴포넌트 테스트 보호).

### 9.3 신규 동작 (View 만, 로직 없음)

- **`QuestCell.Bind`**: 기존 `_badgeBg.SetActive(data.Achieved)` 처리와 같은 위치에서, `_achievedDecor` 가 null 이 아니면 `SetActive(data.Achieved)`. `Bind` 가 풀 재사용 때마다 호출되므로 달성 → 미달성 재사용에도 이전 장식이 남지 않는다. `data == null` 조기 반환과 기존 처리 순서·진행 바 처리 불변.
- **`LordRewardCell.Bind`**: 기존 `_badgeBg` 처리에서 `showBadge`(= `Reached && IsLockedDummy == false`)가 참일 때 `_badgeBg.color = data.IsCurrent ? UiDotPalette.Gold : UiDotPalette.Soul` 을 추가한다(알파 1). 스프라이트 대입(`badgeSprite != null` 가드)과 무관하게 수행한다. 두 스프라이트 슬롯이 같은 `SynergyModalBadge` 를 가리키므로 색이 금/소울 구분을 담당한다(기존 `_reachedBadge` 글자색 `Gold`/`Soul` 과 같은 규칙).
- **`RankingCell`**:
  1. `OnEnable`(풀 재사용 리셋): `_rankMedalRoot` 비활성.
  2. `Bind(entry)`: `entry == null` 조기 반환 뒤, `Unranked` 분기 이전에 `_rankMedalRoot` 를 먼저 비활성으로 만든다(`entry.Row == null` 조기 반환 경로에서도 이전 행의 메달이 남지 않음). 정상 경로(`entry.Row != null`)에서 `data.rank >= 1 && data.rank <= 3` 이면 `_rankMedalRoot` 활성, `_rankMedalTinted` 각 원소(null 건너뜀)의 색을 `(RankColor(rank) 의 rgb, 원소의 현재 알파)` 로 칠한다. 4위 이하는 비활성 유지.
  3. `BindUnranked`: 메달 비활성 유지(진입 시점 `Bind` 가 이미 껐으므로 추가 코드 없음).
  4. `RankColor`·`ApplyBackground`·`Truncate`·`FormatMs` 와 순위 글자색 칠하기 불변.
- **`CloudPopup.InitUI`**: 기존 `_connectionDot.color = IsConnected ? Soul : Blood` 바로 뒤에서, `_connectionGlow` 가 null 이 아니면 같은 rgb 와 `_connectionGlow` 의 현재 알파로 색을 칠한다. 점이 null 이어도 글로우는 독립적으로 가드한다.
- 신규 클래스·인터페이스·이벤트 0건. 코드 구조의 세부는 gameplay-programmer 판단이며 위 동작과 §12 의 검증을 충족하면 된다.

**대안 비교** (상태 의존 장식을 어떻게 켜는가):

| 결정 | 선택 | 대안 | trade-off |
|---|---|---|---|
| 달성 장식 묶음 | **A. `_achievedDecor` 1필드 토글 (채택)** | B. 달성 장식을 `BadgeBg` 자식으로 두어 코드 0(앵커 오프셋으로 셀 전체 좌표 환산) / C. 장식 없이 배지만 | A: 코드 1줄, 의도가 명확·테스트 쉬움. B: 코드는 없으나 배지 크기가 바뀌면 장식이 따라 틀어지는 숨은 결합. C: 달성 셀이 면 틴트와 배지만으로 구분되어 시안의 상태색 발광 언어가 빠짐 |
| 1~3위 메달 | **A. 기존 메달 스프라이트 2장 합성 + 틴트 (채택)** | B. 신규 `RankingMedal.png` 1장(테두리+어두운 코어) / C. 순위 글자색만(현행) | A: 신규 스프라이트 0, 필드 2개. B: 한 장으로 깔끔하나 스프라이트 생성 스크립트·임포트 테스트 추가("신규는 최소" 위배). C: 코드 0이지만 1~3위가 글자색 차이뿐이라 메달로 읽히지 않음 |
| 영주 배지 금/소울 | **A. 코드로 `_badgeBg.color` 1줄 (채택)** | B. 금/소울 색을 구운 신규 스프라이트 2장 | A: 스프라이트 0, 코드 1줄. B: 코드 0이지만 신규 에셋 2장 |
| 선택된 랭킹 탭 | **A. `CardSelectChip` 금색 틴트 (채택)** | B. `CardSelectPickBtnGold`(금 면) / C. 현행 `Px_TabOn` 유지 | A: 어두운 유리 면 + 금 테두리라 탭 글자(`#e8e1cf`)가 그대로 읽힘. B: 금 면 위 밝은 글자의 대비 부족, 글자색 변경 필요. C: 새 톤과 어긋남 |
| 연결 점 발광 | **A. `_connectionGlow` 1필드 틴트 (채택)** | B. 중립색 정적 글로우(코드 0) | A: 소울/피색을 따라가 상태가 발광으로도 읽힘. B: 오프라인에서도 소울색 후광이 남아 모순 |

---

## 10. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/UI/QuestPopup.prefab` | §2·§3.1 (K1~K5 + `DecoListGlass`) |
| `Assets/_Lair/Art/UI/QuestCell.prefab` | §3.2 + `_achievedDecor` 와이어링, `_normalSprite` 값 교체 |
| `Assets/_Lair/Art/UI/LordLevelPopup.prefab` | §2·§4.1 (K1~K5 + 헤더·XP 바 마감) |
| `Assets/_Lair/Art/UI/LordRewardCell.prefab` | §4.2, 필드 값 교체(`_normalSprite` `_pendingSprite` `_badgeSoulSprite` `_badgeGoldSprite`) |
| `Assets/_Lair/Art/UI/RankingPopup.prefab` | §2·§5.1 (K1~K5 + 탭·헤더·구분선·내 순위·빈 상태) |
| `Assets/_Lair/Art/UI/RankingCell.prefab` | §5.2 + `_rankMedalRoot`·`_rankMedalTinted` 와이어링, `_normalSprite`·`_mineSprite` 값 교체 |
| `Assets/_Lair/Art/UI/CloudPopup.prefab` | §2·§6 (K1~K3, K6 + 정보·편집·충돌 영역) + `_connectionGlow` 와이어링 |
| `Assets/_Lair/Scripts/UI/Village/QuestCell.cs` · `LordRewardCell.cs` · `RankingCell.cs` · `CloudPopup.cs` | §9 |
| `QuestPopup.cs` · `LordLevelPopup.cs` · `RankingPopup.cs` · `*PoolingScrollView.cs` · `UILoopMotion.cs` · `UiDotPalette.cs` | 변경 없음 |
| `Assets/_Lair/Art/Sprites/*` | 신규 0건, 수정 0건 |

- 프리팹 작업은 **기존 프리팹에 추가·교체만** 한다(새로 만들어 덮어쓰기 금지). 신규 장식 이름은 §2~§6 표 그대로 글자 단위로 쓴다.
- 팝업 프리팹의 origin 셀 인스턴스(PrefabInstance)에는 오버라이드를 만들지 않고, 셀 변경은 셀 프리팹 파일에만 한다. 컴포넌트 제거(`m_RemovedComponents`)를 하지 않는다.
- 새 파일 0건이므로 `.meta` 신규 추가는 없다.
- UI 배선(UnityMCP) 승인 상태: 사용자가 사전 승인했으므로 바로 진행한다.

---

## 11. 기존 테스트·프리팹 연결 검사 영향

| 대상 | 영향 | 근거 |
|---|---|---|
| `RankingPopupWiringTests` (루트 이름 `RankingPopup` · `Dim`/`ModalBody` 존재 · `_backgroundButton`/`_backButton` · `_scrollView`/`_emptyText` 연결) | 무수정 통과가 기준 | 이름·재부모·연결 불변 |
| `CloudPopupWiringTests` (`Dim`/`ModalBody` · 13개 기능 필드 연결) | 무수정 통과가 기준 | 기존 필드·구조 불변. `_connectionGlow` 는 이 테스트의 목록에 없으므로 §12 에서 별도 검사 |
| `RankingMyRowMatchTests` · `RankingInitialStageTests` · `RankingTimeFormatTests` | 무수정 통과가 기준 | `IsMyRow`·`ResolveInitialStage`·`FormatMs` 로직 불변. 신규 `RankingCell` 필드가 비어 있어도 예외 없음 |
| `QuestProgressTests` · `QuestProgressEdgeTests` (`QuestPopup.BuildCellData`) | 무수정 통과가 기준 | 데이터 빌드 불변 |
| `UiRedesignDataTests` (`LordLevelPopup.BuildXpNextText` 등) | 무수정 통과가 기준 | 순수 함수 불변 |
| `UiLayoutAudit`(에디터) 의 텍스트 위젯 샘플 | 변경 없음 | 텍스트 위젯의 경로·RectTransform·폰트·색 불변 |
| `CHPoolingScrollView` 관련 PlayMode 재현 테스트 | 무수정 통과가 기준 | 풀 구조·`_origin`·`_itemGap` 불변 |

현재 이 7개 프리팹의 장식 규약·상태 전환을 직접 검사하는 테스트는 없다. §12 의 신규 테스트가 필요하다.

---

## 12. 테스트 관점

### 12.1 EditMode (test-engineer) — 공통

| 대상 | 검증 |
|---|---|
| Missing Script 0건 | 7개 프리팹(`QuestPopup` `QuestCell` `LordLevelPopup` `LordRewardCell` `RankingPopup` `RankingCell` `CloudPopup`)의 모든 GameObject 에서 `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` = 0 |
| 기존 필드 연결 | §9.1 의 기존 `[SerializeField]` 가 전부 비어 있지 않음(배열은 길이와 원소 — `_stageTabs` 길이 5 원소 전부 비어 있지 않음). 재직렬화 클로버 가드. 팝업별 `_origin`(Quest/Lord/Ranking 스크롤뷰)이 비어 있지 않고 셀 프리팹 인스턴스 |
| 장식 레이캐스트 | 7개 프리팹 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false`. `Dim` · `CloseButton` · `Scrollbar` · `Handle` · 각 버튼 · 탭 · `NameInput` · 셀 루트 Image 는 `raycastTarget == true` 유지(`Header`·`TitlePlaque`·`Divider` 는 false 유지) |
| 기존 좌표·색 회귀 | §1.1 표의 위젯 anchor · anchoredPosition · sizeDelta · pivot 이 불변(`ModalBody` 720x480 · `TitlePlaque` 320x52 · `CloseButton` 44x44 위치 (13,13) · Quest `ScrollView` · Lord `LordXpBar` 위치 (260,−66) 크기 (−288,24) · Ranking `Header`/`Divider`/`MyRankRow`/`StageTabRow` · Cloud 버튼/그룹 · 셀 664x112/664x72/648x52). `Dim` 색 rgb (0.0157,0.0235,0.0392) 알파 0.7216 (허용오차 0.005) 4개 팝업 모두 |
| `Selectable` 불변 | 모든 버튼의 Transition = ColorTint, `ColorBlock` Normal 0.9 / Highlighted 1.0 / Pressed 0.75 유지. 랭킹 탭 Toggle 의 Transition = None, ToggleTransition = Fade |
| 스프라이트 연결 | `ModalBody` = `Px_Solid` · `TitlePlaque` = `Px_Solid` · `CloseButton` = `VillageHudCta` · `DecoCloseGlyph` = `BuildModalCloseGlyph` · `QuestCell` 루트/`_normalSprite` = `SynergyModalCellBg` · `QuestCell/BadgeBg` = `SynergyModalBadge` · `Fill` = `BattleHudGaugeFill` · `LordXpFill` = `VillageHudXpFill`(색 흰색) · `LordRewardCell` `_normalSprite` = `SynergyModalCellBg` · `_pendingSprite` = `VillageHudBtnOff` · `_badgeSoulSprite`/`_badgeGoldSprite` = `SynergyModalBadge` · `StateRing` = `CardSelectFrame` · `RankingCell` `_normalSprite` = `SynergyModalCellBg` · `_mineSprite` = `VillageHudChip` · `MineRing` = `CardSelectFrame` · 랭킹 탭 6개 Image = `VillageHudBtnOff`, `Checkmark` = `CardSelectChip`(색 `#f7c64a`) · `Divider` = `CardSelectFadeLine` · Cloud 버튼: `ChangeNameButton`/`NameCancelButton`/`ConflictLaterButton`/`RestoreButton` = `VillageHudBtn`, `NameConfirmButton` = `CardSelectPickBtnSoul`, `ConflictRestoreButton` = `CardSelectPickBtnGold` · `ConnectionDot` = `WorldMapMedalOpen` · `ConflictDot` = `WorldMapMedalBoss`(색 흰색) · `ConflictRing` = `CardSelectFrame`(색 `#e5484d`) |
| 형제 순서 | 각 팝업 `ModalBody` 자식 순서가 §3.1·§4.1·§5.1·§6 과 일치(`DecoModalFrame` < `DecoPlaqueGlow` < `TitlePlaque` < `Title` 이 모든 콘텐츠보다 앞, `DecoCloseGlow` 가 `CloseButton` 바로 앞이고 `CloseButton` 이 마지막). Cloud 는 기존 상대 순서(`ConflictGroup` 뒤에 `ConnectionDot`) 유지 |
| 스크롤바 연결 | Quest/Lord/Ranking `ScrollView` 의 `ScrollRect.verticalScrollbar` 가 비어 있지 않고 `verticalScrollbarVisibility` = AutoHide, `Scrollbar.direction` = BottomToTop, `Handle` 스프라이트 = `BuildModalScrollThumb` |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과. 신규 TMP 위젯 0건(프리팹 안 TMP 개수가 변경 전과 같음) |
| 신규 스프라이트·스크립트 0건 | `Assets/_Lair/Editor/` 에 이 작업의 일회용 스크립트가 남아 있지 않음. `Assets/_Lair/Art/Sprites/` 에 이 작업으로 추가된 파일 없음. §7 의 재사용 스프라이트가 모두 존재(삭제·리네임 회귀 가드) |
| 움직임 개수 | 7개 프리팹 안 `UILoopMotion` 컴포넌트 0개 |
| `Instantiate` 금지 | 신규 `.cs` 변경에 `Instantiate`/`new GameObject` 없음(코드 리뷰 항목) |

### 12.2 EditMode — 화면별

| 대상 | 검증 |
|---|---|
| Quest 연결 | `QuestCell._achievedDecor` 가 비어 있지 않고 이름 `DecoAchieved`, 프리팹 기본 비활성. 자식 3개 이름 `DecoAchievedStripGlow`/`DecoAchievedStrip`/`DecoAchievedBadgeGlow`. `ProgressRoot` 자식 순서 `DecoProgressGlow` → `Track` → `Fill` → `DecoProgressFrame` → `ProgressText` |
| Quest 상태 전환 | 프리팹 인스턴스에 `Bind`: ① `Achieved=false, HasProgress=true, Current=3, Target=10` → `_progressRoot` 활성, `fillAmount` 0.3, `_achievedDecor` 비활성, `_badgeBg` 비활성. ② `Achieved=true` → `_achievedDecor` 활성, `_badgeBg` 활성, `_progressRoot` 비활성, 루트 색 `(0.72,1,0.84)`. ③ ②에 이어 ① 재 Bind(풀 재사용) → `_achievedDecor` 비활성·루트 색 흰색 복귀. ④ `Achieved=false, HasProgress=false` → 진행 바·배지·장식 모두 비활성 |
| Quest 가드 | `_achievedDecor` 를 비운 셀에 `Bind` 호출 시 예외 없음 |
| Lord 연결 | `LordXpBar` 자식 마지막이 `DecoXpFrame`(`XpSeg9` 뒤), `DecoLevelGlow` 가 `LordLevelText` 바로 앞 형제, `DecoXpGlow` 가 `LordXpBar` 바로 앞 형제. 셀 `StateRing` 자식 `DecoCurrentGlow`, `DecoLevelPlate` 가 `StateRing` 바로 뒤 |
| Lord 상태 전환 | 프리팹 인스턴스에 `Bind` 4종: 잠금 더미 → 면 `VillageHudBtnOff` · `StateRing` 꺼짐 · 배지 꺼짐 · 보상·부제 꺼짐. 미도달 → 면 `VillageHudBtnOff` · 부제 "N XP 남음" · 배지 꺼짐. 수령 완료 → 면 `SynergyModalCellBg` 색 알파 0.65 · `StateRing` 꺼짐 · 배지 켜짐·`_badgeBg.color` rgb = `UiDotPalette.Soul`. 현재 레벨 → 면 알파 1.0 · `StateRing` 켜짐 · `DecoCurrentGlow` `activeInHierarchy` · `_badgeBg.color` rgb = `UiDotPalette.Gold`. 현재 레벨 → 수령 완료 재 Bind 시 `_badgeBg.color` 가 소울로 복귀, `StateRing` 꺼짐 |
| Lord XP 바 | `LordXpFill.color` = 흰색, Image Type Filled · FillMethod Horizontal 유지, `_xpFill.fillAmount` 칠하기가 `LordLevelPopup` 경로에서 동작(기존 동작 회귀) |
| Ranking 연결 | `RankingCell._rankMedalRoot` 이름 `DecoRankMedal` 기본 비활성, `_rankMedalTinted` 길이 2·원소 이름 순서 `DecoMedalGlow`/`DecoMedalDisc`, 프리팹 알파 0.50/1.0. `DecoMedalDisc` 스프라이트 = `WorldMapMedalOpen`, `DecoMedalCore` = `WorldMapMedalLocked`. `MineRing` 자식 `DecoMineGlow`. `Header` 자식 순서 `DecoHeaderGrad` → `DecoHeaderFrame` → `DecoHeaderLine` → `HRank`…. `Divider` 자식 `DecoDividerCore`·`DecoDividerGem`. `MyRankRow` 자식 인덱스 0 = `DecoMyRankGlow`. `EmptyText` 자식 `DecoEmptyGemTop`·`DecoEmptyGemBottom` |
| Ranking 상태 전환 | 프리팹 인스턴스에 `Bind(RankingRowEntry)`: rank 1/2/3 → 메달 활성, `_rankMedalTinted` 두 원소 rgb = 금 `#FFD24A` / 은 `#C9CDD2` / 동 `#D08B5B`(허용오차 0.005), 알파 0.50/1.0 보존. rank 4 → 메달 비활성. rank 1 → rank 4 재 Bind(풀 재사용) → 메달 비활성. `IsMine=true` → `_background.sprite` = `VillageHudChip`, `MineRing` 활성·색 소울. `IsMine=false` → `SynergyModalCellBg`, `MineRing` 비활성. `Unranked=true` → 메달 비활성·순위 글자 "랭킹 없음"·내 행 스킨. `Row=null` 인 entry 를 rank 1 직후 Bind 해도 메달이 남지 않음 |
| Ranking 가드 | 신규 필드 2종을 비운 셀에 `Bind`/`OnEnable` 호출 시 예외 없음 |
| Ranking 탭 | 6개 탭 모두 Toggle 의 `graphic` 이 `Checkmark` 이고 같은 ToggleGroup 유지, `Checkmark` 색 rgb `#f7c64a` |
| Cloud 연결 | `_connectionGlow` 가 비어 있지 않고 이름 `DecoConnectionGlow`, `ConnectionDot` 바로 앞 형제. `NameEditGroup` 자식 순서 §6.2, `ConflictGroup` 자식 순서 §6.3. `ConflictGroup` Image 색 알파 1.0 (불투명 유지) |
| Cloud 상태 전환 | 팝업 인스턴스에 `InitUI(CloudPopupArg)`: `IsConnected=true` → `_connectionDot` rgb = `UiDotPalette.Soul`, `_connectionGlow` rgb 동일·알파 0.50. `false` → 둘 다 `UiDotPalette.Blood` rgb, 알파 0.50 유지. `ConflictPending=true` → `_conflictGroup`·`_conflictDot` 활성. `false` → 비활성. 비교 칸은 `LocalSummary`·`CloudSummary` 둘 다 있을 때만 활성 |
| Cloud 가드 | `_connectionGlow` 를 비운 `CloudPopup` 에 `InitUI` 호출 시 예외 없음 |
| Cloud 버튼 | `NameConfirmButton.Interactable = false` 후 `Selectable.interactable == false`, 스프라이트·`ColorBlock` 불변. `RestoreButton` 은 `InitUI` 후 비활성(`ShowRestoreButton == false` 회귀) |
| 기존 테스트 | §11 의 7종 무수정 통과 |

### 12.3 PlayMode

신규 PlayMode 테스트 없음. 코드 변경이 활성 토글과 색 칠하기뿐이라 EditMode 로 덮인다.

### 12.4 수동 확인 (사용자 육안, 마을 씬에서 각 팝업을 열어)

- 도전과제: 진행 중(진행 바 + 소울 발광) / 미달성 단발 / 달성(소울 띠·배지·발광) 셀이 한눈에 구분되는지, 13개 목록 스크롤 시 금색 스크롤바와 셀 장식이 뷰포트 경계에서 함께 잘리는지.
- 영주성: 레벨 0/중간/최대에서 XP 바 광택·눈금·프레임, "다음 레벨까지 N XP"; 잠금 더미(회색 평판) · 미도달 · 수령 완료(흐림) · 현재 레벨(금 이중 프레임 + 금 배지) 4종 구분, 레벨 판 위 "Lv n" 가독성.
- 랭킹: 전체/스테이지 1~5 탭 전환 시 선택 탭이 금색 테두리로 바뀌는지, 1~3위 메달 3색, 내 행(목록 안·고정 영역 모두) 강조, 100행 스크롤바(가는 핸들), 빈 목록·오프라인 문구 위아래 보석, 내 순위 미등재 표기.
- 계정: 연결됨/오프라인 점 색·후광, 표시명 변경 입력창 열기(불투명 패널·소울 확인), 충돌 권유 패널(붉은 경고 패널 + 금색 복원 + 비교 칸 2개)이 뒤의 연결·표시명 줄을 완전히 가리는지, 확인 왕복 중 버튼 비활성 색.
- 네 팝업 공통: `Dim` 클릭·× 클릭으로 닫히고 패널 본문 클릭으로는 닫히지 않는지, 장식이 클릭을 막지 않는지.

---

## 13. 구현 시 확인 항목 (사용자 확인 필요 포함)

1. **`CHPoolingScrollView` 와 `Scrollbar` 정합**: 풀링 스크롤이 `Content` 크기를 전체 목록 길이로 맞추어 핸들 길이가 §2.5 의 계산값(Quest 94.0 · Lord 149.9 · Ranking 11.7px)과 같은지, 핸들 드래그 시 셀이 재바인딩되는지 확인한다. 맞지 않으면 해당 화면만 `ScrollRect.verticalScrollbar` 연결을 해제하고 `Scrollbar` GameObject 를 비활성으로 둔다(빌드 모달 §9 와 같은 폴백; 나머지 장식은 유지).
2. **`VillageHudCta` 44x44 늘림**: 닫기 버튼에서 모서리가 깨지지 않는지(border 6,8,6,6 → 중앙 32x30, 빌드 모달과 같은 확인).
3. **`CardSelectFrame` 이 72px·52px 셀에서**: border 7 이 `LordRewardCell`(안쪽 58)·`RankingCell`(안쪽 38)에서 숫자·텍스트를 가리지 않는지.
4. **확정·복원 CTA 의 흰 글자 대비**: `CardSelectPickBtnSoul`/`Gold` 의 밝은 윗면 위 흰 글자(f22)는 카드 선택·배치 A 확정 버튼과 같은 결정이다. 육안으로 읽기 어려우면 글자색만 조정하는 별도 요청을 한다(이 문서는 변경하지 않는다).
5. **(사용자 선택 필요) `ConnectionDot` 의 겹침**: `ConnectionDot`(중심 (44,112))은 형제 순서상 `ConflictGroup` 뒤라 충돌 패널이 열렸을 때 패널 위에 그려지고, 충돌 글자 첫 줄(x 36~, y 96~)과 겹친다. 이 작업은 레이아웃 불변 원칙에 따라 순서를 바꾸지 않고(`DecoConnectionGlow` 도 같은 순서를 따른다) 사용자에게 의도한 배치인지만 확인한다. 순서를 `ConflictGroup` 앞으로 옮기는 것은 별도 요청이 있을 때만 한다.
6. **(사용자 선택 필요) `ConflictDot` 의 가려짐**: `ConflictDot`(중심 (694,24), 20x20)은 `CloseButton`(x 689~733·y −13~31)에 대부분 가려진다(보이는 부분은 x 684~689 와 y 31~34 의 가장자리뿐). 위치 이동은 레이아웃 변경이라 이 작업에서 하지 않고, 스프라이트만 `WorldMapMedalBoss` 로 교체한다. 충돌 알림의 실질적 표시는 `ConflictGroup` 패널이 담당한다. 위치 이동이 필요하면 별도 요청한다.
7. **`UISoftGlow` 알파 프로파일**: 글로우 크기·알파 수치는 선행 화면과 같은 산식(+12~+32)이다. 실제 화면에서 번짐이 뷰포트·프레임 밖으로 과하게 보이면 알파만 낮춘다(크기·좌표는 유지).

---

## 14. 시안(디자인 언어) 대비 포기하는 연출

| 항목 | 선행 화면 사례 | 이 배치 C | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·글로우 | 제목·이름에 `text-shadow` | 생략 (제목·레벨 글자 뒤 `UISoftGlow` Image 로 대체) | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 다른 화면에 영향(선행서와 같은 판단) |
| 버튼 호버 떠오름·눌림 이동 | 카드 선택의 호버 연출 | 없음 (`Selectable` 색 반응만) | 범위는 마감만. 입력·상태 연출은 목표가 아님 |
| 셀 행 간 그림자 (Ranking) | 빌드 모달 셀 `Shadow` (0,−2) | 없음 | 행 간격이 0 이라 다음 행이 그림자를 덮음. `SynergyModalCellBg` 아래 음영이 대신함 |
| 수령 완료·미도달 행의 면 그라데이션 (Lord) | 시너지 모달 `CellGradient` | 없음 | 행 면에 알파 0.65 를 코드가 곱하는데 자식 장식은 따라 흐려지지 않아 모순됨 |
| 영주 보상 잠금 아이콘 | 없음 | 추가하지 않음 | 잠금 더미 판별용 신규 필드·코드가 필요함. 회색 평판 + 회색 글자로 충분 |
| 1~3위 메달 리본·왕관 | 없음 | 추가하지 않음 | 신규 스프라이트 필요. 순위색 원판 + 어두운 코어로 대체 |
| 랭킹 탭 선택 글로우 | 카드 선택 호버 글로우 | 없음 | 선택 표시(`Checkmark`)만 Toggle Fade 로 구동되고, 자식 Image 는 알파를 따라가지 않음 |
| 충돌 점·충돌 패널 맥동 | 마을 HUD `RedDot` Pulse | 없음 | `ConflictDot` 이 가려져 효과 없음(§8·§13). 패널은 정적 경고색으로 충분 |
| 계정 비교 칸 상단 강조선 | 없음 | 추가하지 않음 | 라벨(y 6~24)과 간격이 없어 글자와 겹침. 라벨 글자로 구분 |
| 도전과제 보상 코인 아이콘 | 없음 | 추가하지 않음 | 신규 위젯·위치 결정이 필요한 확장 |
| `Selectable` Normal 흰색화 | 마을 HUD 는 흰색 요구 | 하지 않음 (0.9 유지) | 호버 반응 유지 우선(§1.2, 배치 A 와 같은 결정) |
| 스크롤바 핸들 최소 길이 | 없음 | 두지 않음 (Ranking 11.7px) | 위치 표시 용도, 드래그는 `ScrollRect` 가 담당 |
| `Dim` 알파 통일 | 시너지·빌드 모달 0.8 | 프리팹 값 0.7216 유지 | "그대로" 원칙 |

---

## 15. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트·프리팹 0건. 삭제 대상 에셋 없음.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록** (이 4개 파일 외 `.cs` 변경 0건. 신규 MonoBehaviour 0건 — 별도 파일·같은 파일 다중 클래스 모두 해당 없음):
  - `QuestCell`(`Assets/_Lair/Scripts/UI/Village/QuestCell.cs`): 신규 `[SerializeField] private GameObject _achievedDecor`, `Bind` 에 `SetActive(data.Achieved)` 추가(§9.3).
  - `LordRewardCell`(`.../LordRewardCell.cs`): 필드 추가 없음, `Bind` 의 배지 처리에 `_badgeBg.color` 한 줄 추가(§9.3).
  - `RankingCell`(`.../RankingCell.cs`): 신규 `[SerializeField] private GameObject _rankMedalRoot` · `Image[] _rankMedalTinted`, `OnEnable`·`Bind` 에 메달 리셋/토글/틴트 추가(§9.3).
  - `CloudPopup`(`.../CloudPopup.cs`): 신규 `[SerializeField] private Image _connectionGlow`, `InitUI` 에 틴트 추가(§9.3).
  - `QuestPopup` · `LordLevelPopup` · `RankingPopup` · `*PoolingScrollView` · `UILoopMotion` · `UiDotPalette`: 수정 금지.
  - 신규 코드는 전부 null 가드. 주석은 `//#`, 가드 절만 중괄호 없이 개행(가드 뒤에 계속 진행하는 `if (x != null) { ... }` 는 중괄호 사용), `var`·`!` 금지(Rule 02).
- **프리팹**: §10 표. 필드명·장식 오브젝트 이름은 테스트와 프리팹 와이어링 기준이므로 글자 그대로 쓴다. 신규 장식 이름 목록:
  - 공통(네 팝업): `DecoModalGlow`(루트 직속) · `DecoModalGrad` · `DecoModalVignette` · `DecoModalGloss` · `DecoModalFrame` · `DecoCornerTL`/`TR`/`BL`/`BR` · `DecoPlaqueGlow` · `DecoPlaqueGrad` · `DecoPlaqueFrame` · `DecoPlaqueGemL`/`R` · `DecoCloseGlow` · `DecoCloseGlyph`.
  - 유리 패널: `DecoListGlass`/`DecoListGlassGrad`/`DecoListGlassFrame`(Quest·Lord·Ranking) · `DecoHeaderGlass`/`…Grad`/`…Frame`(Lord) · `DecoAccountGlass`/`…Grad`/`…Frame`(Cloud).
  - 스크롤바(Quest·Lord·Ranking): `Scrollbar` · `SlidingArea` · `Handle` · `DecoThumbGlow` · `DecoScrollTrackFrame`.
  - QuestCell: `DecoCellGrad` · `DecoCellGloss` · `DecoStateStrip` · `DecoAchieved`(`DecoAchievedStripGlow` · `DecoAchievedStrip` · `DecoAchievedBadgeGlow`) · `DecoProgressGlow` · `DecoProgressFrame`.
  - LordLevelPopup: `DecoLevelGlow` · `DecoXpGlow` · `DecoXpFrame`. LordRewardCell: `DecoCurrentGlow` · `DecoLevelPlate` · `DecoLevelPlateFrame`.
  - RankingPopup: `DecoHeaderGrad` · `DecoHeaderFrame` · `DecoHeaderLine` · `DecoDividerCore` · `DecoDividerGem` · `DecoMyRankGlow` · `DecoEmptyGemTop` · `DecoEmptyGemBottom`. RankingCell: `DecoMineGlow` · `DecoRankMedal`(`DecoMedalGlow` · `DecoMedalDisc` · `DecoMedalCore`).
  - CloudPopup: `DecoAccountSep` · `DecoConnectionGlow` · `DecoEditGrad` · `DecoEditFrame` · `DecoInputFrame` · `DecoConfirmGlow` · `DecoConfirmGloss` · `DecoConflictGlow` · `DecoConflictGrad` · `DecoRestoreGlow` · `DecoRestoreGloss` · `DecoCompareTop` · `DecoCompareFrame`.
- **프리팹 정적 값 설정**: §2~§6 표의 기존 Image 스프라이트·색·Image Type 교체와 `Shadow` 추가, 셀의 `Sprite` 필드 값 교체(`_normalSprite` `_pendingSprite` `_mineSprite` `_badgeSoulSprite` `_badgeGoldSprite`)만 수행한다. `Dim` 색·알파, `Selectable` `ColorBlock`, 모든 기존 TMP 색·크기·문구, `CHPoolingScrollView` 값, 셀 인스턴스 오버라이드는 변경 금지.
- **UI 목업**: 해당 없음(사용자 사전 승인으로 시안 제작 생략, `.mockups/` 파일 없음).
- **구현 순서 제안**: 코드 변경이 가장 작은 `QuestPopup`(+`QuestCell`) → `LordLevelPopup`(+`LordRewardCell`) → `CloudPopup` → `RankingPopup`(+`RankingCell`) 순으로 프리팹을 마감하고 화면 1개 완료마다 커밋한다. 공통 키트(§2)는 첫 화면에서 완성한 뒤 나머지 화면에 같은 값으로 옮긴다.
