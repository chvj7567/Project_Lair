# UI 비주얼 폴리시 배치 A — ResultPopup · ConfirmPopup · ToastView · SkillUnlockBanner

## § 헤더

- **목표**: 이미 승인·구현된 6개 화면(침입 지도 · 마을 HUD · 배틀 HUD · 시너지 모달 · 빌드 모달 · 카드 선택)의 디자인 언어(금색 포인트 이중 프레임 + 모서리 장식 + 상단 광택 + 룬 라인, 금테 제목판 + 마름모, 붉은 입체 CTA(`VillageHudCta`), 유리 inset, 종류/상태색 발광, 마름모 보석)를 **결과 팝업(`ResultPopup`) · 확인 팝업(`ConfirmPopup`) · 토스트(`ToastView`) · 스킬 해금 배너(`SkillUnlockBanner`)** 4종 프리팹에 옮긴다. 레이아웃·좌표·문구·동작·ViewModel·컷인 타이밍은 바꾸지 않고, 그 위에 장식 레이어와 스프라이트 교체만 얹는다.
- **검증 가설**: 좌표가 같은 상태에서 마감만 올려도 (1) 결과 팝업의 승리(금) / 패배(붉은 톤)가 제목 글자색뿐 아니라 프레임·발광·배경 전체로 한눈에 읽히고, (2) 확인 팝업·토스트·배너가 다른 6개 화면과 같은 톤으로 읽히는가. 정량 메트릭 없음 — 사용자 육안 확인용 UI 개선(선행서 `card-selection-visual-polish.md` · `build-modal-visual-polish.md` 와 같은 성격). 사용자가 시안(목업) 제작 없이 전부 사전 승인했으므로 `.mockups/` 파일은 만들지 않는다(UI 목업 승인 게이트는 이 사전 승인으로 갈음).
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용". 신규 콘텐츠·신규 시스템·메인 메뉴/세팅 화면 없음. 신규 문구 0건.
- **핵심 메커니즘**: (1) 선행서가 만든 스프라이트(`WorldMap*` · `VillageHud*` · `BattleHud*` · `SynergyModal*` · `CardSelect*` · `UISoftGlow` · `Px_Solid`)만 재사용하고 **신규 스프라이트는 0건**이다(§3). (2) 코드 변경은 View 한정 3곳뿐이다: `ResultPopup`(승리/패배 장식 묶음 토글), `ToastView`(종류색 장식 틴트), 나머지 2종은 코드 변경 0건(§5). 신규 MonoBehaviour 클래스 0건. (3) 움직임(`UILoopMotion` 재사용)은 총 3곳이며 전부 끄면 정적 포즈로 남는다(§4).

이 문서의 수치가 기존 프리팹 값과 다르면 §1 의 "현재 값 유지 목록"이 우선한다.

---

## 1. 불변 사항과 공통 규약

### 1.1 현재 값 유지 목록 (프리팹 실측, 이 값이 기준)

| 화면 | 위젯 | 실측값 (유지) |
|---|---|---|
| ResultPopup | 루트 `ResultPopup` | 전체 stretch |
| | `Dim` | 전체 stretch, 색 `#040610` 알파 0.8, `raycastTarget` 켬. **색·알파 변경 없음** |
| | `Rays` | 중앙 앵커 (0,40) 640x640, `Px_Rays`, 기본 비활성(승리에서 코드가 활성) |
| | `ResultPanel` | 중앙 앵커 (0,0) 600x560, `Px_Panel` Sliced, `raycastTarget` 켬 |
| | `ResultText` | (0,210) 560x80, f55, 색은 코드가 칠함(승리 `#f7c64a` / 패배 `#ff6b6f`) |
| | `SubText` | (0,150) 560x26, f17, `#8e98ad` |
| | `RowClear`·`RowHeroHp` | (0,104) 520x44 (둘은 상호 배타). `RowSouls` (0,52), `RowXp` (0,0). 모두 `Px_PanelSunk` Sliced. 자식 `Label` 왼쪽 (16,0) 220x26 f17 `#8e98ad`, 값 텍스트 오른쪽 (−16,0) 150x30 f22 (`ClearTimeText`·`SoulsText` `#5ef0b4`, `XpText` `#eef1f6`, `HeroHpText` `#ff8a8d`) |
| | `NewBadge` (`RowClear` 자식) | 우측 앵커 피벗 (1,0.5) (−140,0) 56x28 `Px_Badge_Soul` Sliced, 기본 비활성. 자식 `Label` "NEW" f11 `#5ef0b4` |
| | `RewardText` | (0,−34) 520x120 피벗 (0.5,1), f14 자동 크기, `#8e98ad` |
| | `RetryButton` | (118,−230) 220x68, `Px_BtnBlood` Sliced, `Selectable` ColorTint, 자식 `ButtonText` "다시 도전" f22 흰색 |
| | `VillageButton` | (−118,−230) 220x68, `Px_Btn` Sliced, ColorTint, 자식 `ButtonText` "마을로" f22 `#e8e1cf` |
| | 루트 자식 순서 | `Dim` → `Rays` → `ResultPanel` → `ResultText` → `RewardText` → `RetryButton` → `VillageButton` → `SubText` → `RowClear` → `RowHeroHp` → `RowSouls` → `RowXp` |
| ConfirmPopup | 루트 `ConfirmPopup` | 중앙 앵커 560x320, 투명 `Image`(알파 0, `raycastTarget` 켬) + `Button` |
| | `Dim` | 루트 기준 stretch + sizeDelta (4800,2800), 색 `#040610` 알파 0.7216, `raycastTarget` 켬. **색·알파 변경 없음** |
| | `Panel` | 전체 stretch, `Px_Panel` Sliced, `raycastTarget` 켬 |
| | `TitlePlaque`·`Title` | 상단 중앙 앵커 (0,0) 320x52 (중심이 패널 상단 모서리에 걸침). `Title` 문구는 코드, f22 `#e8e1cf` |
| | `Message` | stretch, 위치 (0,−8), sizeDelta (−60,−184) → 패널 좌상단 기준 박스 x 30~530, y 100~236. f22 `#eef1f6` 가운데 정렬 |
| | `ConfirmButton` | 하단 중앙 앵커 (−130,40) 220x64, `Px_BtnSoul` Sliced, ColorTint. 자식 `Label` f22 흰색 |
| | `CancelButton` | 하단 중앙 앵커 (130,40) 220x64, `Px_Btn` Sliced, ColorTint. 자식 `Label` f22 `#e8e1cf` |
| | 루트 자식 순서 | `Dim` → `Panel` → `TitlePlaque` → `Title` → `Message` → `ConfirmButton` → `CancelButton` |
| ToastView | 루트 `ToastView` | 중앙 앵커 (0,0) 640x96, `CanvasGroup`(blocksRaycasts 꺼짐 · interactable 꺼짐) |
| | `Panel` | 하단 중앙 앵커 (0,120) 560x72, `Px_PanelDark` Sliced, `raycastTarget` 꺼짐 |
| | `Message` | stretch, 위치 (14,0), sizeDelta (−76,−12), f22 `#eef1f6`, 왼쪽 정렬 |
| | `Dot` | 중앙 앵커 (28,0) 20x20, `Px_Dot`, 색은 코드가 `UiDotPalette.ToastDot` 로 칠함 |
| | `Panel` 자식 순서 | `Message` → `Dot` |
| SkillUnlockBanner | 루트 `SkillUnlockBanner` | 전체 stretch (Graphic 없음) |
| | `Band` (`_root`) | 가로 stretch, 세로 중앙 앵커, 높이 84, 위치 (0,0). 색 `#0c0514` 알파 0.92, 스프라이트 없음 |
| | `AccentTop`·`AccentBottom` | 상단/하단 stretch 높이 4, `Px_Solid`, 색 `#b58cff` |
| | `Label` | stretch, sizeDelta (−400,0), f22 `#e8e1cf` 가운데 정렬 |
| | `SkillTag` | 중앙 앵커 (−330,0) 92x34, `Px_Ring` Sliced, 색 `#5b3ba6`. 자식 `TagLabel` "SKILL" f13 `#b58cff` |
| | `Band` 자식 순서 | `AccentTop` → `AccentBottom` → `Label` → `SkillTag` |

### 1.2 불변 규칙

- 위 표의 위치·크기·앵커·부모-자식 관계·글자 크기·문구는 불변이다. **기존 위젯의 재부모와 위치·크기 이동은 하지 않는다.** 허용되는 것은 신규 장식 GameObject 추가(기존 위젯의 자식 추가 포함), 기존 Image 의 스프라이트·색 교체(§2 가 명시한 것만), 형제 순서 삽입, 기존 TMP 의 정적 색 설정뿐이다. 이 문서가 색을 지정하지 않은 기존 TMP 는 색도 바꾸지 않는다.
- `Selectable` 의 `ColorBlock`(Normal 0.9 · Highlighted 1.0 · Pressed 0.75 · FadeDuration)과 Transition 종류(ColorTint)는 4개 버튼 모두 **불변**이다. 스프라이트가 명암을 굽고 있어 Normal 0.9 가 곱해져 휴지 상태가 10% 어두워 보이는 것을 허용한다(호버 반응 유지가 우선).
- 로직 불변: 닫기 동작(`Close(reuse:true)`), 버튼 콜백, `BuildRows`/`BuildRewardText` 문구, 슬라이드 타이밍(`_slideInDuration` 0.35 · `_holdDuration` 1.2 · `_slideOutDuration` 0.35 · `_offscreenX` 1300), 토스트 노출 1.8초 + 페이드 0.35초, `CanvasGroup` 입력 통과 전부 변경 0건. `SkillUnlockCutsceneController`·`ISkillUnlockBanner` 변경 0건.
- `Strings_Ko` 신규 문구 0건 · 신규 JSON 0건 · 신규 SO 0건 · 신규 Enum 0건 · 신규 Interface 0건 · 신규 Addressable 키 0건(`ResultPopup` · `ConfirmPopup` · `ToastView` · `SkillUnlockBanner` 프리팹 키 그대로). 신규 글리프 의존 0건(기존 "NEW" · "SKILL" 글자만 유지).
- 기존 `[SerializeField]` 필드는 이름·타입·연결 대상 전부 유지한다.
- **신규 장식 GameObject 의 이름은 전부 접두어 `Deco` 로 시작**(컨테이너 포함)하고, 신규 장식 Graphic 은 전부 `raycastTarget = false` 다. 클릭 대상은 기존 `Button` 위젯과 `Dim` 뿐이다. 장식은 `Instantiate`/`new GameObject` 로 만들지 않고 프리팹에 정적으로 배치한다.
- **MonoBehaviour 는 파일명 = 클래스명인 별도 파일**이다. 이 작업은 신규 MonoBehaviour 가 0건이므로 새 파일이 없다(같은 파일에 MonoBehaviour 를 둘 이상 두는 것은 Missing Script 의 원인이므로 금지). 기존 `UILoopMotion`(`Assets/_Lair/Scripts/UI/UILoopMotion.cs`)은 수정 없이 재사용한다.
- **프리팹 편집은 UnityMCP 로 한다.** 기존 프리팹 파일에 추가·교체만 하며 새로 만들어 덮어쓰지 않는다(fileID 보존). 프리팹 생성 에디터 스크립트를 쓰면 완료 직후 삭제한다(Rule 04 §3). 신규 텍스트 위젯 0건이므로 `CHText` 추가도 0건이다.
- **들여쓰기·중괄호 규약(코드 변경 §5)**: 가드 절(조건 충족 시 즉시 `return`/`continue`)만 중괄호 없이 개행하고, 그 외 분기는 한 줄이어도 중괄호를 쓴다. 신규 분기에서 `null` 가드 이후 계속 진행하는 `if (x != null) { ... }` 는 가드 절이 아니므로 중괄호를 쓴다.

### 1.3 공통 용어와 좌표 규약

- "UISoftGlow" = `Assets/_Lair/Art/Sprites/UISoftGlow.png`, "Px_Solid" = `Assets/_Lair/Art/Sprites/UiDot/Px_Solid.png`. "그라데이션 V" = `WorldMapGradientV`(알파 1→0, 위→아래), "그라데이션 H" = `WorldMapGradientH`(알파 1→0, 왼→오)를 지정 색으로 틴트(정의는 `worldmap-popup-visual-polish.md` §2).
- **좌표는 별도 표기가 없으면 "부모 박스 좌상단 기준 (x 오른쪽, y 아래) 중심점"** 이다. 앵커는 부모 좌상단, 피벗은 (0.5,0.5)로 둔다는 뜻이며 구현자가 부모의 앵커 체계로 환산한다. `중심 (x,y)` 와 `크기 WxH` 로 적는다. 좌우/상하 뒤집기는 RectTransform scale (−1,1)/(1,−1) 이고 피벗이 중심이라 중심점이 바뀌지 않는다.
- 색은 sRGB hex, 알파는 0~1. 본문에서 stretch 는 부모 전체(오프셋 0)를 뜻한다.
- 종류/상태색: 소울(확정) `#5ef0b4`, 금(보상·승리) `#f7c64a`, 피(패배·오류) `#e5484d`, 스킬 `#b58cff`(기존 `SkillUnlockBanner` 프리팹 값). 앞 3개는 `UiDotPalette` 의 `Soul` · `Gold` · `Blood` 값과 같다(코드 변경 없음).

---

## 2. 화면별 요소 구현 방식

### 2.1 ResultPopup

**승리/패배 상태 처리 원칙**: 공통 장식(§2.1.1)은 한 벌만 두고, 상태색이 필요한 장식은 **승리 묶음 2개 · 패배 묶음 2개**로 미리 색을 구워 프리팹에 박아 둔다. 코드는 묶음의 `SetActive` 만 토글한다(§5). 묶음은 각각 "패널 뒤쪽(`Back`)" 과 "패널 위쪽(`Front`)" 으로 나뉜다(`Back` 은 패널 바깥 발광이라 패널보다 뒤에 있어야 한다).

루트 자식 최종 순서(뒤 → 앞): `Dim` → `DecoWinBack` → `DecoLoseBack` → `Rays` → `ResultPanel`(자식 아래 참조) → `ResultText` → `RewardText` → `DecoRetryGlow` → `RetryButton`(자식 `DecoRetryGloss` → `ButtonText`) → `VillageButton` → `SubText` → `RowClear` → `RowHeroHp` → `RowSouls` → `RowXp`.

`ResultPanel` 자식 최종 순서: `DecoPanelGrad` → `DecoPanelVignette` → `DecoPanelGloss` → `DecoPanelFrame` → `DecoCornerTL` → `DecoCornerTR` → `DecoCornerBL` → `DecoCornerBR` → `DecoWinFront` → `DecoLoseFront`.

#### 2.1.1 공통 장식 (승리·패배 모두 표시)

패널 `ResultPanel` 은 600x560, 좌표 기준은 패널 좌상단.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 패널 바탕 | 기존 `ResultPanel` Image 스프라이트 `Px_Panel` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.96. `raycastTarget` 켬 유지. `Shadow` effectDistance (0,−8) 색 `#000000` 알파 0.47 | `Px_Solid` | 없음 |
| 바탕 그라데이션 | `DecoPanelGrad`: stretch, 그라데이션 V 색 `#1b2336` 알파 0.95 | `WorldMapGradientV` | 없음 |
| 내측 비네트 | `DecoPanelVignette`: stretch, 색 `#000000` 알파 0.75 (스프라이트 최대 알파 0.80 × 0.75 = 0.60) | `WorldMapVignette` | 없음 |
| 상단 유리 광택 | `DecoPanelGloss`: 중심 (300,25) 584x34, 그라데이션 V 흰색 알파 0.09 | `WorldMapGradientV` | 없음 |
| 금테 이중 프레임 | `DecoPanelFrame`: stretch, Sliced, fillCenter 끔, 색 흰색 | `WorldMapFrame` | 없음 |
| 모서리 장식 4개 | `DecoCornerTL/TR/BL/BR` 각 14x14. 중심 TL (11,11) / TR (589,11) / BL (11,549) / BR (589,549). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` | 없음 |
| 보상 구분선 | `RowXp` 의 자식 3개(§2.1.4) | — | 없음 |
| 부제 | 기존 `SubText` 불변 | — | 기존 |
| 행 4종 | §2.1.4 | — | 없음 |
| 버튼 2종 | §2.1.5 | — | 없음 |

검산 (모서리): TR 중심 x = 600 − 4 − 7 = 589, BL 중심 y = 560 − 4 − 7 = 549. 광택 폭 584 = 600 − 2×8, 중심 x = 8 + 292 = 300, 중심 y = 8 + 17 = 25. 모서리 TL 박스 (4,4)~(18,18) 와 광택 박스(x 8~592, y 8~42) 는 겹치지만 광택이 먼저 그려지고 모서리가 위다.

#### 2.1.2 승리 묶음

`DecoWinBack`(루트 직속, stretch, Graphic 없음, **프리팹 기본 비활성**):

| 자식 | 방식 | 스프라이트·수치 |
|---|---|---|
| `DecoWinOuterGlow` | 중앙 앵커 중심 (0,0) 644x604, 색 `#f7c64a` 알파 0.13 | `UISoftGlow` |

`DecoWinFront`(`ResultPanel` 자식, stretch, Graphic 없음, **프리팹 기본 비활성**) 자식 최종 순서:

| 순서 | 자식 | 방식 | 스프라이트·수치 |
|---|---|---|---|
| 1 | `DecoWinTitleGlow` | 중심 (300,70) 360x110, 색 `#f7c64a` 알파 0.40. `UILoopMotion` Pulse + 중첩 `Canvas`(Override Sorting 끔) (§4) | `UISoftGlow` |
| 2 | `DecoWinTopBandGlow` | 중심 (300,5.5) 600x29, 색 `#f7c64a` 알파 0.70 | `UISoftGlow` |
| 3 | `DecoWinTopBand` | 중심 (300,5.5) 576x5, 색 `#f7c64a` 알파 1.0 | `CardSelectFadeLine` |
| 4 | `DecoWinRuneL` | 중심 (135,70) 150x3, 색 `#f7c64a` 알파 1.0 (뒤집지 않음: 왼쪽 끝이 가장 진함) | `WorldMapGradientH` |
| 5 | `DecoWinRuneR` | 중심 (465,70) 150x3, scale (−1,1), 색 `#f7c64a` | `WorldMapGradientH` |
| 6 | `DecoWinGemL` | 중심 (226,70) 18x18, 색 `#f7c64a` | `VillageHudGem` |
| 7 | `DecoWinGemR` | 중심 (374,70) 18x18, 색 `#f7c64a` | `VillageHudGem` |
| 8 | `DecoWinFrameShine` | stretch, Sliced, fillCenter 끔, 색 `#f7c64a` 알파 0.60 | `WorldMapFrame` |

#### 2.1.3 패배 묶음

`DecoLoseBack`(루트 직속, stretch, Graphic 없음, **프리팹 기본 비활성**):

| 순서 | 자식 | 방식 | 스프라이트·수치 |
|---|---|---|---|
| 1 | `DecoLoseVeil` | stretch(화면 전체), 색 `#e5484d` 알파 0.35 (스프라이트 최대 알파 0.80 × 0.35 = 0.28 이 화면 모서리에 붉게 깔린다) | `WorldMapVignette` |
| 2 | `DecoLoseOuterGlow` | 중앙 앵커 중심 (0,0) 644x604, 색 `#e5484d` 알파 0.20 | `UISoftGlow` |

`DecoLoseFront`(`ResultPanel` 자식, stretch, Graphic 없음, **프리팹 기본 비활성**): `DecoWinFront` 와 같은 8개 구성·같은 좌표·같은 크기이며 이름의 `Win` 만 `Lose` 로 바꾸고(`DecoLoseTitleGlow` · `DecoLoseTopBandGlow` · `DecoLoseTopBand` · `DecoLoseRuneL` · `DecoLoseRuneR` · `DecoLoseGemL` · `DecoLoseGemR` · `DecoLoseFrameShine`) 색을 전부 `#e5484d` 로 한다. 알파는 승리 묶음과 같되, `DecoLoseTitleGlow` 는 알파 0.40 고정이고 **`UILoopMotion` 을 붙이지 않는다**(패배는 정적).

검산 (제목 장식): 제목 글자 "승리"/"패배" 는 2자 × 55 = 110 폭이라 x 245~355. 룬 L 60~210 · R 390~540, 보석 렌더 대각선 18 × 28/36 = 14 → L 보석 x 219~233 · R 367~381. 룬 끝(210)↔보석(219) 간격 9, 보석(233)↔글자(245) 간격 12. 제목 중심 y = 280 − 210 = 70. 글로우 360 = 110 + 2×125, 110 = 55 + 2×27.5. 상단 띠 576 = 600 − 2×12, 글로우 600 = 576 + 2×12, 29 = 5 + 2×12, 중심 y = 3 + 2.5 = 5.5 (카드 선택 팝업 `DecoBand` 와 같은 산식).

색 처리 요약 (승리 금 / 패배 붉은 톤):

| 요소 | 승리 | 패배 |
|---|---|---|
| 제목 글자(기존 코드) | `#f7c64a` | `#ff6b6f` |
| 프레임 겹침 | 금 `#f7c64a` α0.60 → 안쪽 금선이 `#bf9b3d` 로 밝아짐 | 피 `#e5484d` α0.60 → 안쪽 금선이 `#b44f3f` 로 붉어짐 |
| 제목 글로우 · 룬 · 보석 · 상단 띠 | 금 | 피 |
| 패널 바깥 발광 | 금 α0.13 | 피 α0.20 |
| 화면 가장자리 | `Rays` 방사광(기존) | `DecoLoseVeil` 붉은 비네트 |
| 움직임 | 광선 회전 + 제목 글로우 맥동 | 없음 |

프레임 겹침 검산: 겹침 색 = 틴트 × 0.6 + 기존 금선 `#6b5a2a` × 0.4. 승리: R 247×0.6 + 107×0.4 = 191, G 198×0.6 + 90×0.4 = 155, B 74×0.6 + 42×0.4 = 61 → `#bf9b3d`. 패배: R 229×0.6 + 107×0.4 = 180, G 72×0.6 + 90×0.4 = 79, B 77×0.6 + 42×0.4 = 63 → `#b44f3f`. 바깥 잉크 `#07090e` 는 겹쳐도 거의 그대로다. (스프라이트의 알파 1 픽셀 위에 틴트 알파 0.6 이 겹친 결과이며 피 `#e5484d` 는 `UiDotPalette.Blood` 값이다.)

패배 `None`(결과 `-`) 인 경우 두 묶음 모두 꺼져 중립(공통 장식만)이 된다.

#### 2.1.4 행 4종 (`RowClear` · `RowHeroHp` · `RowSouls` · `RowXp`, 각 520x44)

각 행 Image: 스프라이트 `Px_PanelSunk` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.93, `Shadow` effectDistance (0,−2) 색 `#000000` 알파 0.40. 행 자식 최종 순서: `DecoRowGrad` → `DecoRowFrame` → `DecoRowStripe` → (기존 `Label` → 값 텍스트 → `RowClear` 한정 `NewBadge`) → (`RowXp` 한정 `DecoRuleL` → `DecoRuleR` → `DecoRuleGem`). 좌표 기준은 행 좌상단(520x44).

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `DecoRowGrad` | stretch, 그라데이션 H 색 `#1a2133` 알파 0.93 | `WorldMapGradientH` |
| `DecoRowFrame` | stretch, Sliced, fillCenter 끔, 색 흰색 | `BattleHudSmallFrame` |
| `DecoRowStripe` | 좌측 중앙 앵커 피벗 (0,0.5) 위치 (3,0) 크기 3x38. 색: `RowClear`·`RowSouls` `#5ef0b4` / `RowXp` `#f7c64a` / `RowHeroHp` `#e5484d` | `Px_Solid` |
| `DecoRuleL` (`RowXp` 만) | 중심 (160,50) 200x2, scale (−1,1), 색 `#f7c64a` 알파 0.60 | `WorldMapGradientH` |
| `DecoRuleR` (`RowXp` 만) | 중심 (360,50) 200x2, 색 `#f7c64a` 알파 0.60 | `WorldMapGradientH` |
| `DecoRuleGem` (`RowXp` 만) | 중심 (260,50) 14x14, 색 `#f7c64a` | `VillageHudGem` |
| `NewBadge` (`RowClear` 의 기존) | Image 스프라이트 `Px_Badge_Soul` → `SynergyModalBadge`(Sliced), 색 `#5ef0b4`. 자식 `DecoNewGlow`(`NewBadge` 자식 인덱스 0): 중심 (28,14) 76x48, 색 `#5ef0b4` 알파 0.45. "NEW" 글자 불변 | `SynergyModalBadge` · `UISoftGlow` |

검산: 스트라이프 높이 38 = 44 − 2×3(프레임 3px). 스트라이프 x 3~6 과 `Label` x 16 사이 간격 10. 구분선 중심 y = 44 + 6 = 50 → `RowXp` 하단(−22)에서 6 아래 = −28, `RewardText` 상단(−34) 보다 6 위. 구분선 양쪽 합 x 60~460 = 400, 중심 260 = 520/2. `NewBadge` 글로우 76 = 56 + 2×10, 48 = 28 + 2×10, 중심 = 배지 중앙 (28,14). `DecoNewGlow` 가 배지 자식이라 배지가 꺼지면(`RowHeroHp` 와 비승리 포함) 함께 꺼진다. 구분선이 `RowXp` 자식이라 메타 없음(`HasMeta` false → `RowXp` 비활성)이면 함께 숨는다.

행 값 텍스트·`Label` 색은 불변(§1.1). 승리/패배에 따라 행 장식이 바뀌지 않는다(행 켜짐/꺼짐은 기존 `ApplyRows` 가 처리).

#### 2.1.5 버튼 2종

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| `RetryButton` 몸체 | Image 스프라이트 `Px_BtnBlood` → `VillageHudCta`(Sliced), 색 흰색. `Shadow` effectDistance (0,−4) 색 `#000000` 알파 0.47 | `VillageHudCta` |
| `DecoRetryGlow` | 루트 직속, `RetryButton` 바로 앞 형제. 중앙 앵커 위치 (118,−230) 284x132, 색 `#e5484d` 알파 0.47. 정적(맥동 없음) | `UISoftGlow` |
| `DecoRetryGloss` | `RetryButton` 자식 인덱스 0(`ButtonText` 앞). 상단 stretch, 피벗 (0.5,1), 위치 (0,−6), sizeDelta (−20,12), 그라데이션 V 흰색 알파 0.27 | `WorldMapGradientV` |
| `VillageButton` 몸체 | Image 스프라이트 `Px_Btn` → `VillageHudBtn`(Sliced), 색 흰색. `Shadow` (0,−4) 알파 0.47 | `VillageHudBtn` |
| 글자 | 기존 `ButtonText` 2개 불변 | 기존 |

검산: 글로우 284 = 220 + 2×32, 132 = 68 + 2×32 (마을 HUD 방어하기 `296x136 = 232x72 + 각 32` 와 같은 산식). 광택 폭 200 = 220 − 20, 위치 y −6 = 상단에서 6 아래 박스 중심(광택 박스 y 0~12 중심 6 → 마을 HUD 광택 (10,6) 시작과 동일한 6). 두 버튼 간격: 중심 ±118, 폭 220 → 사이 간격 236 − 220 = 16, 변화 없음.

### 2.2 ConfirmPopup

루트 자식 최종 순서(뒤 → 앞): `Dim` → `DecoPanelGlow` → `Panel`(자식 아래) → `DecoPlaqueGlow` → `TitlePlaque`(자식 아래) → `Title` → `DecoRuleL` → `DecoRuleR` → `DecoRuleGem` → `Message` → `DecoConfirmGlow` → `ConfirmButton`(자식 `DecoConfirmGloss` → `Label`) → `CancelButton`(자식 `Label`).

`Panel`(560x320) 자식 최종 순서: `DecoPanelGrad` → `DecoPanelVignette` → `DecoPanelGloss` → `DecoPanelFrame` → `DecoCornerTL` → `DecoCornerTR` → `DecoCornerBL` → `DecoCornerBR`.

`TitlePlaque`(320x52) 자식 최종 순서: `DecoPlaqueGrad` → `DecoPlaqueFrame` → `DecoPlaqueGemL` → `DecoPlaqueGemR`.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 패널 외곽 금빛 발광 | `DecoPanelGlow`: 루트 직속, 중앙 앵커 (0,0) 604x364, 색 `#f7c64a` 알파 0.13 | `UISoftGlow` |
| 패널 바탕 | 기존 `Panel` Image 스프라이트 `Px_Panel` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.96. `raycastTarget` 켬 유지. `Shadow` (0,−8) 알파 0.47 | `Px_Solid` |
| 바탕 그라데이션 | `DecoPanelGrad`: stretch, 그라데이션 V 색 `#1b2336` 알파 0.95 | `WorldMapGradientV` |
| 내측 비네트 | `DecoPanelVignette`: stretch, 색 `#000000` 알파 0.75 | `WorldMapVignette` |
| 상단 광택 | `DecoPanelGloss`: 패널 좌상단 기준 중심 (280,25) 544x34, 그라데이션 V 흰색 알파 0.09 | `WorldMapGradientV` |
| 금테 이중 프레임 | `DecoPanelFrame`: stretch, Sliced, fillCenter 끔 | `WorldMapFrame` |
| 모서리 장식 4개 | `DecoCornerTL/TR/BL/BR` 각 14x14. 중심 TL (11,11) / TR (549,11) / BL (11,309) / BR (549,309). TR scale (−1,1), BL (1,−1), BR (−1,−1) | `WorldMapCornerGold` |
| 제목판 발광 | `DecoPlaqueGlow`: 루트 직속, `TitlePlaque` 바로 앞 형제, 상단 중앙 앵커 위치 (0,0) 356x88, 색 `#f7c64a` 알파 0.27 | `UISoftGlow` |
| 제목판 바탕 | 기존 `TitlePlaque` Image 스프라이트 `Px_Plaque` → `Px_Solid`(Simple), 색 `#141a2c` | `Px_Solid` |
| 제목판 그라데이션·프레임 | `DecoPlaqueGrad`: stretch, 그라데이션 V 색 `#2a3450` 알파 1.0. `DecoPlaqueFrame`: stretch, Sliced, fillCenter 끔 | `WorldMapGradientV` · `WorldMapFrame` |
| 제목판 마름모 | `DecoPlaqueGemL/R`: 판 중앙 앵커 18x18, 위치 (−133,+4) / (+133,+4) (UGUI 좌표, y 위로 +), 색 `#f7c64a` | `VillageHudGem` |
| 제목 글자 | 기존 `Title` 불변 (f22 `#e8e1cf`) | 기존 |
| 본문 | 기존 `Message` 불변 (f22 `#eef1f6`) | 기존 |
| 본문-버튼 구분선 | `DecoRuleL`: 패널 좌상단 기준 중심 (180,241) 200x2, scale (−1,1). `DecoRuleR`: 중심 (380,241) 200x2. 색 `#f7c64a` 알파 0.60. `DecoRuleGem`: 중심 (280,241) 14x14, 색 `#f7c64a`. 셋 다 루트 직속(`Message` 앞 형제) | `WorldMapGradientH` · `VillageHudGem` |
| 확정 버튼 몸체 | 기존 `ConfirmButton` Image 스프라이트 `Px_BtnSoul` → `CardSelectPickBtnSoul`(Sliced), 색 흰색. `Shadow` (0,−4) 알파 0.47 | `CardSelectPickBtnSoul` |
| 확정 버튼 발광 | `DecoConfirmGlow`: 루트 직속, `ConfirmButton` 바로 앞 형제, 하단 중앙 앵커 위치 (−130,40) 248x92, 색 `#5ef0b4` 알파 0.45 | `UISoftGlow` |
| 확정 버튼 광택 | `DecoConfirmGloss`: `ConfirmButton` 자식 인덱스 0, 상단 stretch, 피벗 (0.5,1), 위치 (0,−4), sizeDelta (−10,10), 그라데이션 V 흰색 알파 0.31 | `WorldMapGradientV` |
| 취소 버튼 몸체 | 기존 `CancelButton` Image 스프라이트 `Px_Btn` → `VillageHudBtn`(Sliced), 색 흰색. `Shadow` (0,−4) 알파 0.47 | `VillageHudBtn` |

검산: 패널 모서리 TR 중심 x = 560 − 4 − 7 = 549, BL 중심 y = 320 − 4 − 7 = 309. 광택 폭 544 = 560 − 2×8, 중심 x = 8 + 272 = 280. 외곽 발광 604 = 560 + 2×22, 364 = 320 + 2×22. 제목판 발광 356 = 320 + 2×18, 88 = 52 + 2×18 (빌드 모달과 같음). 마름모 ±133 = 160 − 27. 구분선 y 241: `Message` 박스 하단 236 과 버튼 상단 248(= 320 − 40 − 32 = 248) 사이 12px 의 중앙(242)에서 1px 위. 구분선 양쪽 x 80~480 = 400, 중심 280 = 560/2. 확정 버튼 글로우 248 = 220 + 2×14, 92 = 64 + 2×14 (카드 선택 `DecoPickGlow` 와 같은 +14 산식), 중심 x = 280 − 130 = 150. 취소 버튼은 글로우 없음(주 행동 강조). 제목판이 패널 상단 모서리에 걸쳐 있으므로 `DecoPanelFrame` 의 상단 변이 제목판 아래로 가려진다(제목판이 뒤에서 앞으로 그려짐).

본문이 3줄(현재 최장 문구 "클라우드 저장 데이터로 현재 진행을 덮어씁니다. 지금 기기의 진행이 사라질 수 있습니다. 복원할까요?" 공백 포함 56자 → 박스 폭 500 ÷ 글자 22 = 줄당 22자 → ceil(56/22) = 3줄 × 줄 높이 27 = 81px)일 때 `Message` 박스 높이 136 안에 들어가므로 구분선과 겹치지 않는다. 구현 후 `UiLayoutAudit` 의 `ConfirmPopup/Message` 샘플로 재확인한다.

### 2.3 ToastView

`Panel`(560x72) 자식 최종 순서: `DecoPanelGrad` → `DecoPanelGloss` → `DecoKindGlow` → `DecoPanelFrame` → `DecoKindStrip` → `DecoDotGlow` → `Message` → `Dot`.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 패널 바탕 | 기존 `Panel` Image 스프라이트 `Px_PanelDark` → `Px_Solid`(Simple), 색 `#0d1220` 알파 0.96. `raycastTarget` 꺼짐 유지. `Shadow` effectDistance (0,−6) 색 `#000000` 알파 0.47 | `Px_Solid` | 없음 |
| 바탕 그라데이션 | `DecoPanelGrad`: stretch, 그라데이션 H 색 `#1b2336` 알파 0.95 (왼쪽이 밝음) | `WorldMapGradientH` | 없음 |
| 상단 광택 | `DecoPanelGloss`: 패널 좌상단 기준 중심 (280,14) 548x16, 그라데이션 V 흰색 알파 0.07 | `WorldMapGradientV` | 없음 |
| 종류색 발광 | `DecoKindGlow`: 좌측 중앙 앵커, 중심 (7,36) 44x86, 알파 0.45, **종류색 틴트** | `UISoftGlow` | **신규 `_kindTintImages[1]`** |
| 프레임 | `DecoPanelFrame`: stretch, Sliced, fillCenter 끔, 색 흰색 (바깥 잉크 2 + 금 1 + 잉크 2 = 5px) | `BattleHudBossFrame` | 없음 |
| 종류색 띠 | `DecoKindStrip`: 좌측 중앙 앵커 피벗 (0,0.5) 위치 (5,0) 크기 4x62, 알파 1.0, **종류색 틴트** | `Px_Solid` | **신규 `_kindTintImages[0]`** |
| 점 발광 | `DecoDotGlow`: `Dot` 과 같은 앵커(중앙)·같은 위치 (28,0), 크기 48x48, 알파 0.55, **종류색 틴트** | `UISoftGlow` | **신규 `_kindTintImages[2]`** |
| 점 | 기존 `Dot` Image 스프라이트 `Px_Dot` → `VillageHudGem`, 크기 20x20 유지. 색은 기존 `SetKind` 가 칠함 | `VillageHudGem` | 기존 `_dot` |
| 메시지 | 기존 `Message` 불변 | — | 기존 `_messageText` |

종류색 틴트 규칙: 정보 `#5ef0b4` / 경고 `#f7c64a` / 오류 `#e5484d` (`UiDotPalette.ToastDot` 가 이미 반환하는 값). 틴트는 rgb 만 바꾸고 각 장식의 프리팹 알파는 보존한다. 프리팹 기본 색은 정보(`#5ef0b4`)로 맞춘다.

검산: 프레임 안쪽 5px → 띠 x 5~9, 높이 62 = 72 − 2×5. 종류색 발광 44 = 4 + 2×20, 86 = 62 + 2×12, 중심 x = 5 + 2 = 7. 점 발광 48 = 20 + 2×14. 광택 548 = 560 − 2×6, 중심 x = 6 + 274 = 280, 중심 y = 6 + 8 = 14. 보석 점 렌더 대각선 20 × 28/36 = 15.6.

페이드: 모든 장식이 `Panel` 안에 있으므로 루트 `CanvasGroup.alpha` 페이드와 `Shadow` 가 함께 따라간다(별도 코드 없음). `Close(reuse:true)` 로 캐시 재사용되므로 종류색은 매 `Show` 의 `SetKind` 가 덮어쓴다(§5.2).

**확인 항목(사용자 확인 필요)**: 프리팹 실측상 `Dot` 은 중앙 앵커 (28,0) 이라 `Panel` 가로 중앙에서 오른쪽 28px 지점 = 패널 좌측 기준 x 308 에 놓인다. `Message` 는 왼쪽 정렬로 x 52 부터 시작하므로 짧은 문구에서는 점이 글자 영역과 겹친다. 이 작업은 레이아웃 불변 원칙에 따라 `Dot` 위치를 건드리지 않고(`DecoDotGlow` 도 같은 위치를 따른다), 의도한 배치인지만 사용자에게 확인을 요청한다. 위치를 옮기는 것은 별도 요청이 있을 때만 한다.

### 2.4 SkillUnlockBanner

`Band`(높이 84, 가로 stretch) 자식 최종 순서: `DecoBandGrad` → `DecoBandGloss` → `DecoAccentGlowTop` → `DecoAccentGlowBottom` → `AccentTop` → `AccentBottom` → `DecoHairTop` → `DecoHairBottom` → `Label` → `DecoTagGlow` → `SkillTag`(자식 `TagLabel`) → `DecoTagGem` → `DecoEndGem`. 장식 전부가 `Band` 의 자식이므로 슬라이드 연출(`_root.anchoredPosition`)에 함께 이동하고 `HideImmediate` 로 함께 화면 밖에 숨는다.

| 요소 | 방식 | 스프라이트·수치 |
|---|---|---|
| 띠 바탕 | 기존 `Band` Image 색 `#0c0514` 알파 0.92 **유지**, 스프라이트 없음 유지 | — |
| 띠 그라데이션 | `DecoBandGrad`: stretch, 그라데이션 V 색 `#2a1850` 알파 0.55 (위가 보랏빛, 아래로 사라짐) | `WorldMapGradientV` |
| 상단 광택 | `DecoBandGloss`: 상단 stretch, 피벗 (0.5,1), 위치 (0,−5), sizeDelta (0,14), 그라데이션 V 흰색 알파 0.06 | `WorldMapGradientV` |
| 강조선 발광 | `DecoAccentGlowTop`: 상단 stretch, 피벗 (0.5,0.5), 위치 (0,−2), sizeDelta (0,24), 색 `#b58cff` 알파 0.35. `DecoAccentGlowBottom`: 하단 stretch, 위치 (0,+2), sizeDelta (0,24), 같은 색·알파 | `UISoftGlow` |
| 강조선 | 기존 `AccentTop`·`AccentBottom`(`Px_Solid`, `#b58cff`, 높이 4) **불변** | 기존 |
| 금 헤어라인 | `DecoHairTop`: 상단 stretch, 피벗 (0.5,1), 위치 (0,−4), sizeDelta (0,1), 색 `#f7c64a` 알파 0.60. `DecoHairBottom`: 하단 stretch, 피벗 (0.5,0), 위치 (0,+4), sizeDelta (0,1), 같은 색 | `Px_Solid` |
| 라벨 | 기존 `Label` 불변 (f22 `#e8e1cf`) | 기존 |
| 태그 발광 | `DecoTagGlow`: 중앙 앵커 위치 (−330,0), 크기 112x54, 색 `#b58cff` 알파 0.45. `UILoopMotion` Pulse(§4) | `UISoftGlow` |
| 태그 몸체 | 기존 `SkillTag` Image 스프라이트 `Px_Ring` → `CardSelectChip`(Sliced), 색 `#5b3ba6` → `#b58cff`(링은 베이크 65% 로 `#7659a6`, 면은 어두운 유리). `TagLabel` "SKILL" f13 `#b58cff` 불변 | `CardSelectChip` |
| 태그 옆 보석 | `DecoTagGem`: 중앙 앵커 위치 (−270,0) 14x14, 색 `#b58cff`. `DecoEndGem`: 위치 (+330,0) 14x14, 같은 색 | `VillageHudGem` |

검산: 태그 발광 112 = 92 + 2×10, 54 = 34 + 2×10. 강조선 발광 높이 24 = 4 + 2×10, 위치 ±2 = 강조선 중심(띠 가장자리에서 2). 금 헤어라인 y 4 = 강조선 4px 바로 안쪽. 광택 위치 −5 = 헤어라인(−4~−5) 바로 아래. 태그 우측 끝 x = −330 + 46 = −284, 보석 렌더 대각선 14 × 28/36 = 10.9 → 중심 −270 에서 x −275~−265, 태그와 간격 9. `Label` 글자("영웅의 '스킬명' 스킬 해제" 21자 가정 × 22 = 462 → x ±231) 와 보석(−275) 사이 간격 44.

색 `#b58cff` 는 기존 `SkillUnlockBanner` 프리팹의 `AccentTop` 값(0.7098, 0.5490, 1.0)과 같다. 새 색을 만들지 않는다.

---

## 3. 스프라이트: 신규 0건, 재사용 목록

신규 스프라이트 0건이므로 `Assets/_Lair/Editor/` 일회용 생성 스크립트는 만들지 않는다. 4개 화면이 쓰는 스프라이트는 전부 기존 파일이며 임포트 설정을 수정하지 않는다. 이제 이 4개 화면도 참조하므로 삭제·리네임 금지.

| 스프라이트 | 위치 | 이 문서에서의 용도 |
|---|---|---|
| `Px_Solid` | `Art/Sprites/UiDot/` | 패널·제목판·행·토스트 바탕, 띠·헤어라인·스트라이프 |
| `UISoftGlow` | `Art/Sprites/` | 모든 발광 |
| `WorldMapGradientV` / `WorldMapGradientH` | 〃 | 바탕 그라데이션, 광택, 룬 라인, 구분선 |
| `WorldMapVignette` | 〃 | 패널 내측 비네트, 패배 가장자리 붉은 비네트 |
| `WorldMapFrame` | 〃 | 패널·제목판 금테 프레임, 승리/패배 프레임 겹침 |
| `WorldMapCornerGold` | 〃 | 패널 모서리 장식 |
| `VillageHudGem` | 〃 | 마름모 보석 전부, 토스트 점 |
| `VillageHudBtn` | 〃 | `VillageButton` · `CancelButton` |
| `VillageHudCta` | 〃 | `RetryButton` |
| `CardSelectPickBtnSoul` | 〃 | `ConfirmButton` |
| `CardSelectFadeLine` | 〃 | 결과 팝업 상단 띠 |
| `CardSelectChip` | 〃 | 배너 `SkillTag` |
| `SynergyModalBadge` | 〃 | `NewBadge` |
| `BattleHudSmallFrame` | 〃 | 결과 행 프레임 |
| `BattleHudBossFrame` | 〃 | 토스트 프레임 |

교체되어 더 이상 이 4개 화면이 쓰지 않는 `Px_Panel` · `Px_PanelDark` · `Px_PanelSunk` · `Px_Plaque` · `Px_Btn` · `Px_BtnBlood` · `Px_BtnSoul` · `Px_Badge_Soul` · `Px_Dot` · `Px_Ring` 는 다른 프리팹이 쓸 수 있으므로 삭제하지 않는다.

---

## 4. 애니메이션 — `UILoopMotion` 재사용 + 정적 폴백

`UILoopMotion` 은 수정하지 않고 쓴다(`OnEnable` 에서 기준 포즈 캡처, `OnDisable` 에서 복원, 시간 기준 `Time.unscaledTime` — 전투 정지·컷인 중에도 동작). 총 3곳, 동시 동작 상한은 화면당 2개(결과 팝업 승리 시 2개, 배너 1개)다.

| # | 대상 | 종류 | 파라미터 | 정적 폴백(끄면) |
|---|---|---|---|---|
| 1 | `Rays` (`ResultPopup`, 기존 위젯) | `Rotate` | `_degreesPerSecond` = −6 (시계 방향 60초에 1바퀴). 중첩 `Canvas`(Override Sorting 끔) 추가 | 회전 0(기준 포즈) |
| 2 | `DecoWinTitleGlow` | `Pulse` | `_graphic` = 자기 Image, `_alphaFrom` 0.40 / `_alphaTo` 0.70, `_scaleFrom` 1.00 / `_scaleTo` 1.00, `_period` 2.4초. 중첩 `Canvas`(Override Sorting 끔) 추가 | 알파 0.40 고정 |
| 3 | `DecoTagGlow` (`SkillUnlockBanner`) | `Pulse` | `_graphic` = 자기 Image, `_alphaFrom` 0.45 / `_alphaTo` 0.75, `_scaleFrom` 1.00 / `_scaleTo` 1.00, `_period` 0.9초 | 알파 0.45 고정 |

- `Rays` 는 코드가 `SetActive` 로 켜고 끄고, `DecoWinTitleGlow` 는 `DecoWinFront` 토글로 켜지고 꺼진다. `UILoopMotion` 의 `OnEnable`/`OnDisable` 이 기준 포즈를 잡고 복원하므로 추가 코드가 없다.
- 배너의 `DecoTagGlow` 는 `Band` 자식이라 `_root` 슬라이드 중에도 위치를 건드리지 않는다(Pulse 는 스케일·알파만 쓴다). 배너 노출 총 시간 0.35 + 1.2 + 0.35 = 1.9초 동안 주기 0.9초가 2회 이상 돈다.
- 슬라이드 컷인 타이밍 불변: `PlayCo` · `SlideCo` · `HideImmediate` · `OffscreenX` 코드와 4개 시간 필드 값을 손대지 않는다.
- 폴백 기준은 `village-hud-visual-polish.md` §4.2 와 같다: 최저 사양 기기의 해당 화면 프레임 비용이 연 직후 대비 평균 +0.5ms 를 넘으면 3개 컴포넌트의 Enabled 를 프리팹에서 해제한다(측정: Unity Profiler, 사용자 확인). 최저 사양 기기 정의는 프로젝트 메타에 없으므로 **사용자 선택 필요**. 폴백 후 정보 손실은 없다(승리/패배 구분은 색·장식 묶음이 담당, 움직임은 가산 연출).
- 토스트·확인 팝업은 움직임 0건(토스트는 기존 페이드만).

---

## 5. SerializeField · 코드 변경 (View 한정, 최소)

### 5.1 유지

`ResultPopup` 기존 필드 전부(`_resultText` `_rewardText` `_subText` `_rowClear` `_clearTimeText` `_newBadge` `_rowSouls` `_soulsText` `_rowXp` `_xpText` `_rowHeroHp` `_heroHpText` `_rays` `_retryButton` `_villageButton`), `ConfirmPopup` 기존 필드 전부(`_titleText` `_messageText` `_confirmButton` `_cancelButton`), `ToastView` 기존 필드 전부(`_messageText` `_canvasGroup` `_dot`), `SkillUnlockBannerView` 기존 필드 전부(`_root` `_label` `_slideInDuration` `_holdDuration` `_slideOutDuration` `_offscreenX`)의 이름·타입·연결 대상 불변. `BuildRows` · `FormatClearTime` · `BuildRewardText` · `TryBeginSceneLoad` · 버튼 콜백 · `FadeRoutine` · `PlayCo` 시그니처·동작 불변.

### 5.2 신규 `[SerializeField] private`

| 클래스 | 필드 | 타입 | 용도 | 미할당 시 |
|---|---|---|---|---|
| `ResultPopup` | `_winDecor` | `GameObject[]` (원소 2, 순서 `DecoWinBack` · `DecoWinFront`) | 승리일 때만 활성 | 건너뜀 |
| `ResultPopup` | `_loseDecor` | `GameObject[]` (원소 2, 순서 `DecoLoseBack` · `DecoLoseFront`) | 패배일 때만 활성 | 건너뜀 |
| `ToastView` | `_kindTintImages` | `Image[]` (원소 3, 순서 `DecoKindStrip` · `DecoKindGlow` · `DecoDotGlow`) | `SetKind` 가 종류색 rgb 로 틴트(각 원소의 프리팹 알파 보존) | 건너뜀 |

`ConfirmPopup` · `SkillUnlockBannerView` 는 신규 필드 0건, 코드 변경 0건이다.

### 5.3 신규 동작

- **`ResultPopup.InitUI`**: 기존 `_rays.SetActive(rp.Result == BattleResult.Win)` 바로 옆에서, `_winDecor` 의 각 원소(null 건너뜀)를 `rp.Result == BattleResult.Win` 으로, `_loseDecor` 의 각 원소(null 건너뜀)를 `rp.Result == BattleResult.Lose` 로 `SetActive` 한다. 매 `InitUI` 호출이 두 배열을 모두 덮어쓰므로 풀 재사용 시 이전 결과의 묶음이 남지 않는다(Rule 03 §4 State Reset). `arg` 가 `ResultPopupArg` 가 아니면 기존 동작 그대로 이 분기를 건너뛴다.
- **`ToastView.SetKind(EToastKind kind)`**: 기존 동작(`_dot` 이 있으면 `UiDotPalette.ToastDot(kind)` 로 칠함)을 유지하되, `_dot` 이 비어 있어도 `_kindTintImages` 틴트는 수행하도록 `_dot == null` 조기 반환을 `_kindTintImages` 처리 뒤로 옮긴다. `_kindTintImages` 각 원소(null 건너뜀)를 `(ToastDot(kind) 의 rgb, 원소의 현재 알파)` 로 칠한다. 풀 재사용 시 이전 종류색이 남지 않도록 매 `Show` 마다 덮어쓴다.
- 신규 클래스·인터페이스·이벤트 0건. 코드 구조의 세부는 gameplay-programmer 판단이며 위 동작과 §8 의 검증을 충족하면 된다.

---

## 6. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/UI/ResultPopup.prefab` | §2.1 전부 + `_winDecor`/`_loseDecor` 와이어링 + `Rays` 에 `UILoopMotion`·중첩 `Canvas` 추가 |
| `Assets/_Lair/Art/UI/ConfirmPopup.prefab` | §2.2 전부 |
| `Assets/_Lair/Art/UI/ToastView.prefab` | §2.3 전부 + `_kindTintImages` 와이어링 |
| `Assets/_Lair/Art/UI/SkillUnlockBanner.prefab` | §2.4 전부 + `DecoTagGlow` 에 `UILoopMotion` 추가 |
| `Assets/_Lair/Scripts/UI/ResultPopup.cs` | §5.2·5.3 |
| `Assets/_Lair/Scripts/UI/Common/ToastView.cs` | §5.2·5.3 |
| `Assets/_Lair/Scripts/UI/Common/ConfirmPopup.cs` · `Assets/_Lair/Scripts/UI/SkillUnlockBannerView.cs` | 변경 없음 |
| `Assets/_Lair/Art/Sprites/*` | 신규 0건, 수정 0건 |

- 프리팹 작업은 **기존 프리팹에 추가·교체만** 한다(새로 만들어 덮어쓰기 금지). 신규 장식 이름은 §2 표 그대로 글자 단위로 쓴다.
- 4개 프리팹 모두 일반 프리팹이며 중첩 프리팹 인스턴스가 아니다. 컴포넌트 제거(`m_RemovedComponents`)를 하지 않는다.
- 새 파일 0건이므로 `.meta` 신규 추가도 프리팹 내부 fileID 추가 외에는 없다.
- UI 배선(UnityMCP) 승인 상태: 사용자가 사전 승인했으므로 바로 진행한다.

---

## 7. 기존 테스트·프리팹 연결 검사 영향

| 대상 | 영향 | 근거 |
|---|---|---|
| `ResultPopupRewardTextTests` · `ResultPopupSceneGuardTests` · `UiRedesignDataTests`(`BuildRows`) · `MetaSettlementOrderTests` | 무수정 통과가 기준 | 순수 함수·씬 가드 불변. 신규 필드가 비어 있어도 예외 없음 |
| `SkillUnlockCutsceneControllerTests` · `SkillUnlockCutsceneControllerSuiteTests` | 무수정 통과가 기준 | `ISkillUnlockBanner` 모킹 기반, 배너 코드 변경 0건 |
| `UiLayoutAudit`(에디터) 샘플 `ConfirmPopup/Message` · `ConfirmPopup/Title` · `ToastView/Message` · `ResultPopup/RewardText` · `ClearTimeText` · `HeroHpText` · `SoulsText` · `XpText` · `SubText` · `SkillUnlockBanner/Label` | 변경 없음 | 텍스트 위젯의 경로·RectTransform·폰트 크기·색 불변. 구현 후 재실행해 `ConfirmPopup/Message` 최장 문구가 박스 안에 들어가는지 확인 |
| `VillageHudEscCloseTests` | 영향 없음 | 별개 프리팹 |
| `ChannelVolumeTests` 등 오디오 테스트 | 영향 없음 | 이 작업은 사운드 변경 0건 |

현재 4개 프리팹의 필드 연결·장식 규약을 직접 검사하는 테스트는 없다. 아래 §8 의 신규 테스트가 필요하다.

---

## 8. 테스트 관점

### 8.1 EditMode (test-engineer) — 공통

| 대상 | 검증 |
|---|---|
| 프리팹 Missing Script 0건 | 4개 프리팹의 모든 GameObject 에서 `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` = 0 |
| 기존 필드 연결 | §5.1 의 기존 `[SerializeField]` 가 전부 비어 있지 않음(재직렬화 클로버 가드). 4개 프리팹 모두 |
| 장식 레이캐스트 | 프리팹 안 이름이 `Deco` 로 시작하는 GameObject 의 모든 `Graphic` 은 `raycastTarget == false`. 기존 클릭 위젯(`Dim` · `ResultPanel` · `RetryButton` · `VillageButton` · `ConfirmPopup` 루트 · `Panel` · `ConfirmButton` · `CancelButton`)의 `raycastTarget` 은 §1.1 값 유지 |
| 기존 좌표 회귀 | §1.1 표의 위젯 anchor · anchoredPosition · sizeDelta · pivot 과 `Dim` 색(`ResultPopup` α0.8, `ConfirmPopup` α0.7216)이 불변 |
| 스프라이트 연결 | `ResultPanel`·`Panel`·`Px_Solid` · `RetryButton` = `VillageHudCta` · `VillageButton`·`CancelButton` = `VillageHudBtn` · `ConfirmButton` = `CardSelectPickBtnSoul` · `NewBadge` = `SynergyModalBadge` · `SkillTag` = `CardSelectChip` · `Dot` = `VillageHudGem` · 토스트 `DecoPanelFrame` = `BattleHudBossFrame` |
| `Selectable` 불변 | 4개 버튼의 Transition = ColorTint, `ColorBlock` Normal 0.9 / Highlighted 1.0 / Pressed 0.75 유지 |
| 형제 순서 | §2.1~2.4 의 루트·`ResultPanel`·`Panel`·`TitlePlaque`·`Band` 자식 순서와 일치 |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과. 신규 TMP 위젯 0건(프리팹 안 TMP 개수가 변경 전과 같음) |
| 신규 스프라이트·스크립트 0건 | `Assets/_Lair/Editor/` 에 이 작업의 일회용 스크립트가 남아 있지 않음 |
| 움직임 개수 | `UILoopMotion` 컴포넌트: `ResultPopup.prefab` 정확히 2개(`Rays` Rotate −6 / `DecoWinTitleGlow` Pulse 주기 2.4 · 알파 0.40→0.70), `SkillUnlockBanner.prefab` 정확히 1개(`DecoTagGlow` Pulse 주기 0.9 · 알파 0.45→0.75), `ToastView.prefab`·`ConfirmPopup.prefab` 0개 |
| 슬라이드 필드 불변 | `SkillUnlockBanner.prefab` 의 `_slideInDuration` 0.35 · `_holdDuration` 1.2 · `_slideOutDuration` 0.35 · `_offscreenX` 1300 |
| 배너 장식 소속 | `SkillUnlockBanner.prefab` 의 모든 `Deco*` 가 `Band`(`_root`) 의 후손 |

### 8.2 EditMode — 화면별

| 대상 | 검증 |
|---|---|
| ResultPopup 연결 | `_winDecor` 길이 2 · 원소 이름 `DecoWinBack`/`DecoWinFront`, `_loseDecor` 길이 2 · 원소 이름 `DecoLoseBack`/`DecoLoseFront`. 4개 모두 프리팹 기본 비활성. `DecoWinBack`/`DecoLoseBack` 은 루트 직속, `DecoWinFront`/`DecoLoseFront` 는 `ResultPanel` 의 자식 |
| ResultPopup 상태 전환 | `InitUI(Win)` → `_winDecor` 2개 활성 · `_loseDecor` 2개 비활성 · `_rays` 활성. `InitUI(Lose)` → 반대(`_rays` 비활성). `InitUI(Win)` 직후 `InitUI(Lose)` 재호출(풀 재사용) 시 승리 묶음이 남지 않음. 결과 `None` 이면 4개 모두 비활성 |
| ResultPopup 색 | 승리 묶음 장식(`DecoWin*`) 색 rgb 가 `#f7c64a`, 패배 묶음(`DecoLose*`) rgb 가 `#e5484d`. `DecoLoseVeil` 알파 0.35, `DecoWinFrameShine`·`DecoLoseFrameShine` 알파 0.60. `_resultText` 색은 기존 코드 값(승리 `UiDotPalette.Gold`, 패배 `#ff6b6f`) 그대로 |
| ResultPopup 행 | 행 4종 스프라이트 = `Px_Solid`, 각 행에 `DecoRowGrad`·`DecoRowFrame`·`DecoRowStripe` 존재, `RowXp` 에 `DecoRuleL`/`R`/`Gem` 존재, `NewBadge` 의 자식 `DecoNewGlow` 가 인덱스 0. 행 줄 표시(`ApplyRows`)는 기존 동작 그대로 |
| ResultPopup 가드 | 신규 필드 두 배열을 비운(`null`·빈 배열·원소 null) `ResultPopup` 에서 `InitUI` 가 예외 없음 |
| ConfirmPopup 연결 | `DecoConfirmGlow` 가 `ConfirmButton` 바로 앞 형제, `DecoConfirmGloss` 가 `ConfirmButton` 자식 인덱스 0, `DecoPlaqueGlow` 가 `TitlePlaque` 바로 앞 형제 |
| ConfirmPopup 동작 불변 | 기존 확인/취소 콜백 흐름(`Close(reuse:true)` 후 `OnConfirm`/`OnCancel`)이 무수정 통과 |
| ToastView 연결 | `_kindTintImages` 길이 3 · 원소 이름 순서 `DecoKindStrip`/`DecoKindGlow`/`DecoDotGlow`. 각 원소의 프리팹 알파 1.0 / 0.45 / 0.55 |
| ToastView 틴트 | `SetKind` 호출 후 3개 원소 rgb = 종류색(정보 `UiDotPalette.Soul` · 경고 `Gold` · 오류 `Blood`), 알파는 호출 전과 같음. 종류를 바꿔 다시 호출해도 알파 불변. `_dot` 이 비어 있어도 틴트가 수행됨. `_kindTintImages` 를 비워도 예외 없음 |
| ToastView 페이드 불변 | 기존 `FadeRoutine`(1.8초 + 0.35초, `blocksRaycasts` 꺼짐) 동작 무수정 통과 |
| SkillUnlockBanner | `DecoTagGlow` 가 `SkillTag` 바로 앞 형제, `SkillTag` 색 `#b58cff`, `AccentTop`/`AccentBottom` 의 스프라이트·색(`#b58cff`)·높이 4 불변, `Band` 색 `#0c0514` 알파 0.92 불변 |

### 8.3 PlayMode

신규 PlayMode 테스트 없음. 코드 변경이 활성 토글과 색 칠하기뿐이라 EditMode 로 덮인다.

### 8.4 수동 확인 (사용자 육안)

- 결과 팝업을 승리·패배로 각각 열어 제목 글자색 외에 프레임·발광·룬·가장자리가 금/붉은 톤으로 나뉘는지, 승리 광선이 천천히 도는지, 패배에서 광선이 없고 가장자리가 붉게 깔리는지. 승리 → 마을 복귀 → 다음 판 패배 순서로 열어도 이전 장식이 남지 않는지.
- 메타 없음(`HasMeta` false)일 때 `RowXp` 구분선이 같이 사라지는지, 신기록일 때 `NewBadge` 와 글로우가 보이는지.
- `RetryButton` 붉은 글로우와 광택, 두 버튼 모두 호버·눌림 색 반응이 기존과 같은지, 장식이 클릭을 막지 않는지.
- 확인 팝업(클라우드 복원)에서 제목판·구분선·확정(초록)/취소 버튼이 보이고, 본문 3줄이 구분선과 겹치지 않는지.
- 토스트 정보/경고/오류 3종에서 띠·발광·점이 종류색을 따라가고, 연속 호출 시 이전 색이 남지 않는지, 1.8초 + 페이드가 기존과 같은지. 점 위치 확인 항목(§2.3).
- 스킬 해금 컷인: 좌→중→우 슬라이드 타이밍이 기존과 같고, 금 헤어라인·보라 발광·태그 맥동이 밴드와 함께 이동하며 퇴장 후 잔존하지 않는지.

---

## 9. 시안(디자인 언어) 대비 포기하는 연출

| 항목 | 선행 화면 사례 | 이 배치 A | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자·글로우 | 제목·이름에 `text-shadow` | 생략 (제목 글자 뒤 `UISoftGlow` Image 로 대체) | TMP 그림자는 공용 SDF 폰트 머티리얼 수정이 필요해 다른 화면에 영향(선행서와 같은 판단) |
| 버튼 호버 떠오름·눌림 이동 | 카드 선택의 호버 연출 | 없음 (`Selectable` 색 반응만) | 범위는 마감만. 입력·상태 연출은 이 배치의 목표가 아님 |
| 결과 팝업 진입 연출(스케일 팝·페이드 인) | 없음 | 추가하지 않음 | 현재 진입 연출이 없고 "동작 불변" 원칙 |
| 패배 화면 균열·흔들림 | 없음 | 추가하지 않음 | 신규 에셋·동작 필요 |
| 확인 팝업 취소 버튼 글로우 | 카드 선택 주 버튼만 글로우 | 취소는 글로우 없음 | 주 행동(확정) 강조 |
| 토스트·배너 모서리 장식 | 모달에는 있음 | 없음 | 토스트 72px·배너 84px 의 얇은 띠에서 모서리 14px 장식은 과밀. 대신 종류색 띠·강조선 사용 |
| 배너 라벨 양옆 룬 라인 | 카드 선택 룬 라인 | 없음 (태그 옆 보석만) | `Label` 글자 폭이 스킬명에 따라 가변이라 고정 좌표 장식이 겹칠 위험 |
| 결과 행 값 텍스트 색·글자 | 없음 | 변경 없음 | 기존 코드가 의미(보상 녹/금, 패배 붉음)를 칠하고 있음 |
| 코드 구동 승리/패배 틴트 | 카드 선택은 코드 틴트 | 미리 색을 구운 묶음 2벌 + 토글 | 틴트 대상 배열이 12개 이상이 되어 필드·코드가 불어남. 묶음 토글이 코드 2줄로 끝남 |
| 토스트 점 위치 보정 | — | 하지 않음 | 레이아웃 불변. 확인 항목으로만 남김(§2.3) |
| `Selectable` Normal 흰색화 | 마을 HUD 는 흰색 요구 | 하지 않음(0.9 유지) | 호버 반응 유지 우선(§1.2) |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음.
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트·프리팹 0건. 삭제 대상 에셋 없음.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `ResultPopup`(`Assets/_Lair/Scripts/UI/ResultPopup.cs`): 신규 `[SerializeField] private GameObject[] _winDecor` · `_loseDecor`, `InitUI` 에서 `_rays` 토글 옆에 두 배열 토글 추가(§5.3). 한 줄 `if` 중 가드 절이 아닌 것은 중괄호를 쓴다. 기존 `ResultPopupArg`·`ResultRowsData`·`BuildRows` 등 변경 없음.
  - `ToastView`(`Assets/_Lair/Scripts/UI/Common/ToastView.cs`): 신규 `[SerializeField] private Image[] _kindTintImages`, `SetKind` 에 틴트 추가와 `_dot` 조기 반환 순서 조정(§5.3).
  - `ConfirmPopup` · `SkillUnlockBannerView` · `UILoopMotion` · `UiDotPalette`: 수정 금지.
  - 신규 MonoBehaviour 0건(별도 파일·같은 파일 다중 클래스 모두 해당 없음).
- **프리팹**: §6 표. 오브젝트·필드 이름은 테스트와 프리팹 와이어링 기준이므로 글자 그대로 쓴다. 신규 장식 이름 목록:
  - ResultPopup: `DecoWinBack`(`DecoWinOuterGlow`) · `DecoLoseBack`(`DecoLoseVeil` · `DecoLoseOuterGlow`) · `DecoPanelGrad` · `DecoPanelVignette` · `DecoPanelGloss` · `DecoPanelFrame` · `DecoCornerTL`/`TR`/`BL`/`BR` · `DecoWinFront`/`DecoLoseFront`(각 `…TitleGlow` · `…TopBandGlow` · `…TopBand` · `…RuneL` · `…RuneR` · `…GemL` · `…GemR` · `…FrameShine`) · `DecoRowGrad` · `DecoRowFrame` · `DecoRowStripe`(행 4개 각각) · `DecoRuleL`/`DecoRuleR`/`DecoRuleGem`(`RowXp` 안) · `DecoNewGlow` · `DecoRetryGlow` · `DecoRetryGloss`.
  - ConfirmPopup: `DecoPanelGlow` · `DecoPanelGrad` · `DecoPanelVignette` · `DecoPanelGloss` · `DecoPanelFrame` · `DecoCornerTL`/`TR`/`BL`/`BR` · `DecoPlaqueGlow` · `DecoPlaqueGrad` · `DecoPlaqueFrame` · `DecoPlaqueGemL`/`R` · `DecoRuleL`/`DecoRuleR`/`DecoRuleGem` · `DecoConfirmGlow` · `DecoConfirmGloss`.
  - ToastView: `DecoPanelGrad` · `DecoPanelGloss` · `DecoKindGlow` · `DecoPanelFrame` · `DecoKindStrip` · `DecoDotGlow`.
  - SkillUnlockBanner: `DecoBandGrad` · `DecoBandGloss` · `DecoAccentGlowTop` · `DecoAccentGlowBottom` · `DecoHairTop` · `DecoHairBottom` · `DecoTagGlow` · `DecoTagGem` · `DecoEndGem`.
- **프리팹 정적 값 설정**: §2 표의 기존 Image 스프라이트·색 교체와 `Shadow` 추가만 수행한다. `Dim` 색·알파, `Selectable` `ColorBlock`, 모든 기존 TMP 색·크기, 슬라이드 시간 필드는 변경 금지.
- **UI 목업**: 해당 없음(사용자 사전 승인으로 시안 제작 생략, `.mockups/` 파일 없음).
- **구현 순서 제안**: 코드 변경이 가장 작은 `ConfirmPopup` → `SkillUnlockBanner` → `ToastView` → `ResultPopup` 순으로 프리팹을 마감하고 화면 1개 완료마다 커밋한다.
