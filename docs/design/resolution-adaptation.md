# 모바일·태블릿 전 해상도 대응 — 마을 HUD + 팝업 (resolution-adaptation)

> 구현용 기획서(수치 SoT). 사용자용 가이드는 `resolution-adaptation.html`, 합의용 목업은 `.mockups/resolution-adaptation.html`.

## 0. 헤더

| 항목 | 내용 |
|---|---|
| **목표** | 마을 씬의 HUD 와 팝업이 폰(16:9~22:9)·태블릿(16:10, 4:3) 어디서나 겹침·잘림 없이 맞는다. |
| **검증 가설** | "배틀이 이미 쓰는 `Expand` 스케일러 + `HudRoot`(Safe Area) / `PopupRoot` 루트 분리 패턴을 마을에 그대로 옮기고, 팝업 패널 4개의 앵커만 중앙으로 고치면, 프리팹 내부 재배치 없이 전 비율에서 겹침·잘림이 사라진다." |
| **현재 단계 범위 적합성** | 범위 안. v0.3 §8 의 신규 화면·콘텐츠가 아니라 기존 마을 화면(HUD, 팝업)의 레이아웃 보정이다. 별도 메인 메뉴/세팅 화면 없음, 신규 몬스터·재화·규칙 없음. |
| **핵심 메커니즘** | ① 마을 `CanvasScaler` 를 Match 0.5 → `Expand`(논리 최소 1280x720)로 바꾼다. ② 마을 캔버스 아래 `HudRoot`(= 기존 `SafeAreaFitter` 재사용)와 `PopupRoot`(전체 스트레치)를 두고 `VillageController` 가 배틀과 같은 순서로 `CHMUI.SetRoot` 를 전환한다. ③ 하단 `TowerButton` 을 `HeroButton` 왼쪽으로 옮겨 우측 `StagePanel` 과의 충돌 여유를 넓히고, 상단 왼쪽 앵커로 박혀 있던 팝업 패널 4개(침입 지도·데일리·편성·탑)를 중앙 앵커로 바꾼다. |

- 새 JSON 스키마: **없음**(Rule 00 데이터 구조 확인 게이트 N/A). 수치는 씬·프리팹 직렬화 값이다(Rule 02 §11: 배선 값).
- 밸런스 수치 변경 없음(qa-simulator 불필요).
- 신규 Enum 없음 · 신규 Interface 없음 · 신규 프리팹/에셋 키 없음.

---

## 1. 근본 원인 (코드·프리팹 근거)

### 1.1 기존 인프라 (재사용 대상)

| 인프라 | 위치 | 마을 사용 여부 |
|---|---|---|
| `SafeAreaFitter` (`Screen.safeArea ∩ 카메라 pixelRect` → 앵커) + `ComputeAnchors` 정적 함수, 테스트 4건 | `Assets/_Lair/Scripts/UI/SafeAreaFitter.cs`, `Tests/EditMode/SafeAreaFitterTests.cs` | **미사용** (배틀 전용) |
| 배틀 루트 분리: `BattleController` 가 HUD 표시 직전 `SetRoot(_hudRoot)`, 직후 `SetRoot(_popupRoot)` | `Scripts/Battle/BattleController.cs` 245~255행 | 마을 없음 |
| `CHMUI.SetRoot` + 모든 UI 를 루트에 전체 스트레치 | `Packages/com.chvj.unityinfra/Runtime/UI/CHMUI.cs` 90~121, 249~257행 | 마을도 공통 사용 |
| 배틀 캔버스 `Expand` | `Battle.unity` 6638행 (`m_ScreenMatchMode: 1`) | 마을 없음 |

→ 신규 인프라 코드는 만들지 않는다(Rule 03 §1). 마을에 **배선**만 한다.

### 1.2 원인 1 — 마을 캔버스가 Match 0.5 이다 (겹침·잘림의 공통 뿌리)

- 근거: `Assets/_Lair/Scenes/Village.unity` 11381~11386행 — `m_UiScaleMode: 1`, `m_ReferenceResolution: 1280x720`, `m_ScreenMatchMode: 0`(Match Width Or Height), `m_MatchWidthOrHeight: 0.5`, 캔버스 `m_RenderMode: 0`(Overlay). 캔버스 자식 0개(모든 UI 는 런타임 `CHMUI.ShowUIAsync`).
- 산식: `scale = sqrt(W/1280 · H/720)`, 논리 크기 = `(W/scale, H/scale)`. 16:9 보다 가로로 길면 논리 높이가 720 미만으로, 짧으면 논리 폭이 1280 미만으로 줄어든다.
- 마을 HUD·팝업 프리팹의 오프셋은 전부 **1280x720 에서만 맞게 저작**돼 있다(아래 1.4·1.5).

| 기기(해상도 px) | 비율 | 현행 scale | 현행 논리 크기 | `Expand` 논리 크기 (scale = min(W/1280, H/720)) |
|---|---|---|---|---|
| 1920x1080 | 16:9 | 1.5000 | 1280x720 | 1280x720 (scale 1.5) |
| 2340x1080 (S25+) | 19.5:9 | 1.6559 | 1413.1x652.2 | 1560x720 (scale 1.5) |
| 2400x1080 | 20:9 | 1.6771 | 1431.1x644.0 | 1600x720 (scale 1.5) |
| 2520x1080 | 21:9 | 1.7185 | 1466.4x628.4 | 1680x720 (scale 1.5) |
| 2640x1080 | 22:9 | 1.7589 | 1500.9x614.0 | 1760x720 (scale 1.5) |
| 1920x1200 | 16:10 | 1.5811 | 1214.3x759.0 | 1280x800 (scale 1.5) |
| 2048x1536 | 4:3 | 1.8475 | 1108.5x831.4 | 1280x960 (scale 1.6) |

### 1.3 원인 2 — 마을에는 Safe Area 처리가 없다

- 근거: `VillageController.cs` 77행 — `CHMUI.Instance.ShowUIAsync(EUI.VillageHud, ...)` 앞에 `SetRoot` 호출 없음 → HUD 가 캔버스 전체(Safe Area 무시)에 스트레치된다. `SafeAreaFitter` 를 쓰는 곳은 배틀 `HudRoot` 뿐이다(`Grep SafeAreaFitter` 결과 `SafeAreaFitter.cs` 한 파일).
- 설정 근거: `ProjectSettings/ProjectSettings.asset` 70~71행 — `androidStartInFullscreen: 1`, `androidRenderOutsideSafeArea: 1` → **앱이 카메라 구멍·제스처 바 영역까지 그린다.** 따라서 안전영역을 직접 비켜 주지 않으면 좌측 메뉴 열·상단 바가 구멍 아래로 들어간다.

### 1.4 원인 3 — HUD 영역 분할이 1280x720 에만 맞는 여유다

근거: `Assets/_Lair/Art/UI/VillageHud.prefab` (모든 값은 논리 단위, HUD 루트 기준). 위쪽 기준 y 는 위에서 아래로 잰 거리.

| 영역(프리팹 오브젝트) | 앵커 | 사각형 (x는 앵커 쪽 가장자리 기준, 크기 w x h) |
|---|---|---|
| `TopBar` | 상단 스트레치 | 높이 88 (y 0..88) |
| 좌측 메뉴 8칸 (`LordButton` `ShopButton` `CodexButton` `QuestButton` `RecordsButton` `RankingButton` `RelicButton` `DailyButton`) | 좌상단 | 2열 x 4행, 각 112x112, x 16..256, y 104..600 (열 x 16·144, 행 y 104·232·360·488) |
| `RecordPanel` | 우상단 | 256x216, 오른쪽 여백 16, y 104..320 |
| `StagePanel` | 우하단 | 340x324, 오른쪽·아래 여백 16, y (H−340)..(H−16) |
| `HeroButton` / `SortieButton` / `TowerButton` | 하단 중앙 | 중앙 기준 x: Hero −170..−70, Sortie −62..+170, Tower +178..+278 (현행), 높이 72, 아래 여백 16 |
| `StagePrevButton` / `StageNextButton` | 정중앙 | 48x68, 중앙 기준 x: Prev −224..−176, Next +176..+224 |

겹침 여유 산식(논리 HUD 영역 폭 W, 높이 H):

| 쌍 | 여유(= 간격, 음수면 겹침) | 1280x720 에서 | 현행 실패 지점 |
|---|---|---|---|
| `RecordPanel` ↔ `StagePanel` (세로) | `(H−340) − 320 = H − 660` | 60 | 19.5:9 에서 H=652.2 → **−7.8**, 20:9 −16.0, 21:9 −31.6, 22:9 −46.0 |
| `TowerButton` ↔ `StagePanel` (가로, 현행 위치) | `(W−356) − (W/2+278) = W/2 − 634` | **6** | 16:10 에서 W=1214.3 → **−26.8**, 4:3 에서 W=1108.5 → **−79.7** |
| `StageNextButton` ↔ `StagePanel` (H ≤ 748 일 때 세로가 겹침) | `(W−356) − (W/2+224) = W/2 − 580` | 60 | 4:3 에서 −25.7 (세로는 안 겹쳐 실제 겹침 없음) |

→ S25+ 보고의 "우측 패널 겹침" = `RecordPanel`/`StagePanel` 세로 겹침(원인 1), "좌측 메뉴·상단 바 가림" = Safe Area 미적용(원인 2). 태블릿은 `TowerButton`/`StagePanel` 가로 겹침.

### 1.5 원인 4 — 팝업 패널 일부가 상단-왼쪽 고정이다

근거: 각 팝업 프리팹의 패널 `RectTransform` (anchorMin/Max, anchoredPosition, sizeDelta, pivot).

| 패널 크기 | 앵커 방식 | 해당 팝업(프리팹) |
|---|---|---|
| 764x524 | 중앙 (0.5,0.5), 위치 (0,0) | `ShopPopup` `CodexPopup` `QuestPopup` `CloudPopup` `LordLevelPopup` `RankingPopup` |
| 524x604 | 중앙 | `RecordsPopup` |
| 1180x684 | 중앙 | `RelicPopup` |
| **1136x640** | **좌상단 (0,1), pivot (0,1), 위치 (72,−40)** | **`WorldMapPopup`(침입 지도) `DailyPopup` `FormationPopup` `TowerPopup`** |

- 1136x640 패널은 1280x720 의 정중앙(72 = (1280−1136)/2, 40 = (720−640)/2)을 **좌상단 기준으로** 박은 것이라 화면 크기가 변하면 중앙에서 벗어난다. 근거: `WorldMapPopup.prefab` 3889~3893행(`Panel`), 같은 값이 `DailyPopup` 984~987행, `FormationPopup` 4254~4257행, `TowerPopup` 1478~1481행.
- 팝업 루트·`Dim` 은 스트레치(0,0)~(1,1) — `WorldMapPopup.prefab` 1644~1648행(`Dim`), 12386~12390행(루트).

현행(Match 0.5) 잘림 계산 — `WorldMapPopup`(침입 지도), 패널 안 좌표는 `Panel` 기준:

| 요소 | 패널 안 사각형 | 화면 안 위치(패널 위 40 더함) |
|---|---|---|
| `DifficultyBox` (난이도 선택) | x 512..752, y 512..620 (`WorldMapPopup.prefab` 2893~2894행) | 아래 끝 660 |
| `SelectButton` ("이 스테이지로 선택") | x 768..1120, y 564..620 (3984~3985행) | 아래 끝 660 |
| `BottomText` | x 24..504, y 564..620 (4836~4837행) | 아래 끝 660 |
| `Panel` 전체 | 1136x640 | 아래 끝 680 |
| `CloseButton` | x 1072..1120, y 8..56 (2755~2756행) | x 1144..1192 |

| 비율 | 논리 H | 패널 아래 끝 680 대비 | 선택 버튼·난이도 아래 끝 660 대비 | 오른쪽 |
|---|---|---|---|---|
| 19.5:9 | 652.2 | 27.8 잘림 | **7.8 잘림** | 오른쪽 여백 205.1 vs 왼쪽 72 (한쪽으로 쏠림) |
| 20:9 | 644.0 | 36.0 | 16.0 | 여백 223.1 |
| 21:9 | 628.4 | 51.6 | 31.6 | 여백 258.4 |
| 22:9 | 614.0 | 66.0 | 46.0 | 여백 292.9 |
| 4:3 | 831.4 (세로는 충분) | 0 | 0 | 패널 오른쪽 끝 1208 > 폭 1108.5 → **99.5 잘림, `CloseButton`(x 1144..1192) 전체가 화면 밖** |

`RelicPopup`(중앙 앵커, 1180x684)도 19.5:9 에서 위·아래가 각 (684−652.2)/2 = 15.9 잘리고, 4:3 에서 좌우가 각 (1180−1108.5)/2 = 35.8 잘린다. 나머지 중앙 팝업은 높이 604 이하라 22:9 까지 맞는다.

**주의(근거 한계)**: 위 계산으로는 침입 지도의 난이도·선택 버튼이 "아래 7.8~46 만" 잘린다. 사용자 스크린샷은 "아예 안 보인다" 이므로, 실기기에서 `Screen.width/height` 가 더 작거나(시스템 바 차지) 논리 높이가 더 낮을 가능성이 남는다. → 검증 게이트 **R0**(실기기·Device Simulator 수치 로그)로 확정한다. 아래 해결안은 논리 높이를 항상 ≥720 으로 만들기 때문에 이 차이와 무관하게 유효하다.

---

## 2. 지원 해상도 매트릭스

규칙: **마을은 레터박스를 두지 않는다.** 16:9 보다 가로로 길면 **가로 확장**, 16:9 보다 가로로 짧으면(태블릿) **세로 확장**. 어느 쪽이든 논리 크기는 항상 1280x720 이상이다(`Expand`: scale = min(W/1280, H/720)). 배틀 월드·레터박스 규칙(`battle-wide-screen-fit.md`)은 불변.

| # | 대표 기기 | 해상도(px) | 비율 | scale | 논리 크기 | 확장 구간 | 예시 인셋(px) → 논리 | `HudRoot` 논리 크기 |
|---|---|---|---|---|---|---|---|---|
| M1 | 일반 16:9 폰·태블릿 | 1920x1080 | 1.7778 | 1.5 | 1280x720 | 없음(기준) | 0 | 1280x720 |
| M2 | **Galaxy S25+** | 2340x1080 | 2.1667 | 1.5 | 1560x720 | 가로 +280 | 좌 132, 아래 63 → 좌 88, 아래 42 | 1472x678 |
| M3 | 20:9 폰 | 2400x1080 | 2.2222 | 1.5 | 1600x720 | 가로 +320 | 좌 132, 아래 63 → 좌 88, 아래 42 | 1512x678 |
| M4 | 21:9 와이드 | 2520x1080 | 2.3333 | 1.5 | 1680x720 | 가로 +400 | 좌 132, 아래 63 → 좌 88, 아래 42 | 1592x678 |
| M5 | 22:9 이상 초와이드 | 2640x1080 | 2.4444 | 1.5 | 1760x720 | 가로 +480 | 좌 132, 아래 63 → 좌 88, 아래 42 | 1672x678 |
| M6 | 16:10 태블릿 | 1920x1200 | 1.6 | 1.5 | 1280x800 | 세로 +80 | 아래 30 → 20 | 1280x780 |
| M7 | 4:3 태블릿(iPad 류) | 2048x1536 | 1.3333 | 1.6 | 1280x960 | 세로 +240 | 아래 40 → 25 | 1280x935 |

- 인셋은 "예시"다(실기 값은 기기마다 다름). 산식: 논리 인셋 = 픽셀 인셋 ÷ scale (예: 132 ÷ 1.5 = 88, 63 ÷ 1.5 = 42, 30 ÷ 1.5 = 20, 40 ÷ 1.6 = 25).
- 22:9 초과(25:9 폴더블 외부 화면 등)도 같은 규칙(가로 확장)이며 별도 상한을 두지 않는다. 마을 월드 배경은 `DotStageCoverFit` 이 덮어 처리하고 HUD 는 가장자리 앵커라 영향이 없다.
- 세로 모드(0.5625)는 지원 대상 아님(`defaultScreenOrientation: 4` 자동 회전이지만 가로 기준 기획).

### 2.1 지원 하한 (HUD·팝업이 겹침·잘림 없이 맞는 최소 영역)

| 구분 | 하한(논리) | 근거 |
|---|---|---|
| `HudRoot` (인셋 제외 후) | **폭 ≥ 1160, 높이 ≥ 660** | 5.2 의 여유 산식 중 0 이 되는 두 쌍: 폭은 `StageNextButton` ↔ `StagePanel` (`W/2 − 580 ≥ 0` ⇒ W ≥ 1160), 높이는 `RecordPanel` ↔ `StagePanel` (`H − 660 ≥ 0`) |
| `PopupRoot` 안 팝업 패널 | 가로 한쪽 인셋 ≤ (캔버스 폭 − 1180) ÷ 2 | 가장 큰 패널 `RelicPopup` 1180x684 (5.1) |

- 매트릭스 M1~M7 은 전부 하한을 넘는다(`HudRoot` 최소 폭 1280−0 = 1280 ≥ 1160, 최소 높이 678 ≥ 660 → 여유 18).
- 하한 밖 예(= 16:9 폰 + 가로 인셋 합 120 논리 초과)는 지원 범위 밖으로 둔다(기록: 7장 엣지 케이스).

---

## 3. 결정 1 — CanvasScaler 정책

### 3.1 대안

| 안 | 내용 | 결과 | 판정 |
|---|---|---|---|
| **A. Expand (권장)** | `m_ScreenMatchMode` = Expand(1), 기준 1280x720 | 논리 크기 항상 ≥ 1280x720. 배틀과 동일 방식, 프리팹 오프셋을 건드리지 않음 | 채택 |
| B. Match Height (match=1) | 논리 높이 항상 720 | 4:3 은 논리 960x720 → 폭 1280 미만이라 `StagePanel`/`TowerButton`·1136 폭 팝업 잘림 재발 | 폐기 |
| C. Match Width (match=0) | 논리 폭 항상 1280 | 19.5:9 논리 높이 590.8 < 660 → 겹침 | 폐기 |
| D. 비율별 기준 해상도 분기 | 비율 구간마다 다른 기준값 | 구간 경계 튜닝·테스트 부담, 프리팹 이원화 | 폐기 |

권장 A 의 이유: 하한(2.1)을 **스케일러가 구조적으로 보장**한다. 이후 어떤 비율이 와도 HUD·팝업 프리팹의 1280x720 저작 값이 그대로 유효하다.

### 3.2 확정 값

| 대상 | 값 |
|---|---|
| `Village.unity` `UICanvas` `CanvasScaler` | `m_UiScaleMode` 1(유지), `m_ReferenceResolution` 1280x720(유지), **`m_ScreenMatchMode` 0 → 1(Expand)**, `m_MatchWidthOrHeight` 0.5(Expand 에서 미사용, 유지) |
| 렌더 모드 | Overlay(`m_RenderMode: 0`) 유지 |
| 로딩·배틀 씬 | 불변(배틀은 이미 Expand, 로딩은 별개) |

---

## 4. 결정 2 — 루트 분리와 안전영역

### 4.1 루트 구성 (배틀 `battle-wide-screen-fit.md` §3.2 (i) 안의 마을 적용)

`Village.unity` 의 `UICanvas` 아래에 정적 RectTransform 두 개를 둔다(런타임 생성 금지, Rule 03).

| 이름 | 앵커/크기 | 컴포넌트 | 역할 |
|---|---|---|---|
| `HudRoot` | anchor (0,0)~(1,1), offset 0 (런타임에 `SafeAreaFitter` 가 덮어씀) | 기존 `SafeAreaFitter` (`_gameCamera` = 마을 `Main Camera`, `_target` = 자신) | `VillageHud` 전용 루트. 안전영역을 앵커로 반영 |
| `PopupRoot` | anchor (0,0)~(1,1), offset 0, **안전영역 미적용** | 없음 | 마을의 모든 팝업·토스트·확인창 루트. `Dim` 이 캔버스 전체를 덮는다 |

형제 순서: `HudRoot`(먼저) → `PopupRoot`(나중) — 팝업이 HUD 위에 그려진다.

### 4.2 `PopupRoot` 를 전체 스트레치로 두는 이유 (배틀의 "1280 중앙 고정"을 쓰지 않는 이유)

| 안 | 내용 | 팝업 프리팹 작업 | 판정 |
|---|---|---|---|
| **(i) 전체 스트레치 (권장)** | `PopupRoot` = 캔버스 전체. 패널은 중앙 앵커 | `Dim` 수정 0건. 패널 앵커 4건만 | 채택 |
| (ii) 1280x720 중앙 고정(배틀 방식) | `PopupRoot` 1280x720 → 가로·세로 확장분에 `Dim` 이 없음 | 확장 최대치(M5 가로 480 / M7 세로 240)를 덮으려면 `Dim` 오버행(좌우·위아래)을 팝업 12종 이상에 추가 | 폐기(작업량 큼, 세로 확장까지 필요) |
| (iii) `PopupRoot` 도 안전영역 적용 | `Dim` 이 구멍 영역을 못 덮음 | (ii) 와 같은 오버행 필요 | 폐기 |

(i) 에서 팝업 패널은 최대 1180x684 로 캔버스 중앙에 놓이므로 인셋을 침범하지 않는다(5.1 검산).

### 4.3 안전영역 적용 범위

- **HUD(`VillageHud`)**: 적용(`HudRoot`).
- **팝업·토스트·확인창**: 직접 적용하지 않는다. 대신 패널이 중앙 고정이고 최대 1180x684 라서 인셋과 겹치지 않는다는 것을 게이트 R5 로 검증한다. 하한(2.1) 밖에서만 겹친다.
- `SafeAreaFitter` 동작 요건(코드 구조는 구현자 판단): 마을 `Main Camera` 의 `pixelRect` 가 전체 화면이므로 `Screen.safeArea ∩ pixelRect` = 안전영역 그대로 반영되어야 한다. `_gameCamera` 가 null 이면 갱신을 건너뛰는 기존 동작 때문에 **마을 `Main Camera` 를 반드시 연결**한다. 마을 `Main Camera` 가 `Camera.rect` 를 전체(0,0,1,1)가 아닌 값으로 쓰고 있으면 게이트 R0 에서 발견해 보고한다.

### 4.4 `SetRoot` 순서 (`VillageController.Start`)

| 순서 | 동작 | 이유 |
|---|---|---|
| 1 | `Start` 맨 앞에서 `SetRoot(_popupRoot)` | 아래 `SettleRetroactiveCurrencies` 의 `ToastView.Show`(103행)가 HUD 이전에 뜰 수 있다 → 토스트가 `PopupRoot` 에 붙는다 |
| 2 | `await CHMUI.ShowUIAsync(EUI.VillageHud, ...)` **직전** `SetRoot(_hudRoot)` | HUD 는 `HudRoot` |
| 3 | HUD 표시 `await` 직후 `SetRoot(_popupRoot)` | 이후 모든 팝업(메뉴 8종·침입 지도·편성·탑·데일리·확인창)·토스트는 `PopupRoot` |

- 필드: `VillageController` 에 `[SerializeField] RectTransform _hudRoot`, `_popupRoot`(배틀과 같은 관례). 둘 다 null 이면 기존 동작(CHMUI 기본 루트)을 유지한다.
- `CHMUI.OnSceneLoaded` 가 씬 전환 때 루트를 리셋하므로 마을 진입(`Start`)마다 호출해야 한다.
- 마을의 `HeroSelectPopup`(`VillageController.cs` 321행 분기)은 HUD 의 "지도" 버튼이 `WorldMapPopup` 으로 연결(`VillageHud.cs` 92행)돼 도달하지 않는 레거시 경로다. 프리팹(중앙 720x480)은 수정하지 않는다.

---

## 5. 결정 3 — 마을 HUD 영역 분할 규칙

### 5.1 규칙

1. **모든 HUD 요소는 크기 고정 + 가장자리/모서리 앵커.** 비율이 바뀌면 요소가 커지거나 줄지 않고, **요소 사이의 빈 공간만 늘어난다.**
2. 영역(존) 정의와 비율 변화 시 거동은 아래 표와 같다. 가로 확장 시 `TopBar` 만 늘고, 세로 확장 시 좌/우 열 사이가 벌어진다.
3. 겹침 판정 대상은 상호작용·정보 사각형(버튼, 패널)이다. `Deco*`(글로우·장식, 예: `DecoSortieGlow` 296x136 은 설계상 `HeroButton`/`TowerButton` 과 겹침)는 판정에서 제외한다.

| 존 | 구성 | 앵커 | 가로 확장(M2~M5)에서 | 세로 확장(M6~M7)에서 |
|---|---|---|---|---|
| Z1 상단 | `TopBar` (높이 88) | 상단 스트레치 | 폭만 늘어남 (내부 요소는 좌/중앙/우 앵커 고정) | 불변 |
| Z2 좌측 | 메뉴 8칸 (2열 x 4행) | 좌상단 | 불변(안전영역 인셋만큼 오른쪽으로 이동) | 아래 빈 공간만 늘어남 |
| Z3 우상단 | `RecordPanel` | 우상단 | 불변(우측 인셋 있으면 왼쪽으로 이동) | 불변 |
| Z4 우하단 | `StagePanel` | 우하단 | 불변 | 위로 이동하지 않고 아래에 붙은 채 위쪽 빈 공간만 늘어남 |
| Z5 하단 중앙 | `TowerButton`, `HeroButton`, `SortieButton` | 하단 중앙 | 중앙 유지(좌우 여백만 늘어남) | 아래에 붙은 채 유지 |
| Z6 정중앙 | `StagePrevButton`, `StageNextButton`, 월드의 영웅 | 정중앙 | 중앙 유지 | 중앙 유지(세로 중앙 이동) |

### 5.2 최소 영역에서 겹침 여유 (`HudRoot` 논리 W x H)

| 쌍 | 변경 후 여유 산식 | 하한(W=1160, H=660)에서 | 1280x720 에서 |
|---|---|---|---|
| `RecordPanel` ↔ `StagePanel` | `H − 660` | 0 | 60 |
| `StageNextButton` ↔ `StagePanel` (H ≤ 748) | `W/2 − 580` | 0 | 60 |
| `SortieButton` ↔ `StagePanel` | `(W − 356) − (W/2 + 170) = W/2 − 526` | 54 | 114 |
| `TowerButton`(이동 후) ↔ 좌측 메뉴 | 세로 분리 `(H − 88) − 600 = H − 688`; 세로가 겹치면 가로 `(W/2 − 278) − 256 = W/2 − 534` | 가로 46 (H=660 이라 세로 −28 → 가로 사용) | 가로 106 |
| `TopBar` 안 `SoulPill`(x 왼끝 W−380) ↔ 마을 이름(오른끝 W/2+160) | `W/2 − 540` | 40 | 100 |
| `StagePrevButton` ↔ 좌측 메뉴 | `(W/2 − 224) − 256 = W/2 − 480` | 100 | 160 |
| `RecordPanel` ↔ `TopBar` | 104 − 88 | 16 | 16 |

→ 하한은 두 쌍(`RecordPanel`↔`StagePanel`, `StageNextButton`↔`StagePanel`)에서 0 이 되고 나머지는 양수다. 게이트 R3 는 하한에서도 간격 ≥ 0 을 검증한다.

### 5.3 하단 버튼 열 재배치 — 대안 (권장 A, 사용자 확인은 10장 D1)

16:9 + 가로 인셋 조합(예: 폴더블·노치 있는 16:9 폰)에서 `TowerButton`(현행 +178..+278) ↔ `StagePanel`(왼끝 W−356) 여유가 `W/2 − 634` 라 인셋이 12 논리를 넘으면(1280 기준 6 여유) 겹친다. 대안:

| 안 | 내용 | 효과 | 단점 |
|---|---|---|---|
| **A. `TowerButton` 을 `HeroButton` 왼쪽으로 이동 (권장)** | `TowerButton` anchoredPosition x +228 → **−228** (중앙 기준 −278..−178, `HeroButton` −170..−70 과 8 간격). 열 순서: 탑 · 지도 · 방어하기 | 우측 존과의 간섭이 `SortieButton`(+170)으로 줄어 하한 폭 1268 → 1160 | 버튼 순서 바뀜(탑이 왼쪽). `SortieButton` 위치(+54) 불변 |
| B. `StagePanel` 폭 축소 340 → 300 | 간섭 여유 40 증가 | 위협도·초상 레이아웃 재작업(내부 요소 17개 이상) | 작업량 큼 |
| C. 작은 화면에서 HUD 전체 축소 | `HudRoot` 안 localScale | 구조상 스트레치 앵커에서 오프셋이 같이 줄지 않음 | 구현 복잡, 폐기 |

권장 A: 프리팹 한 개 RectTransform 값만 바꾼다(`VillageHud.prefab` 12731~12737행 `TowerButton` 의 `m_AnchoredPosition` x 228 → −228, 나머지 불변).

---

## 6. 팝업 공통 규칙

### 6.1 규칙

1. **패널은 항상 중앙 앵커**: `anchorMin = anchorMax = pivot = (0.5, 0.5)`, 위치는 (0,0)에 두고 크기는 고정값. 좌상단 같은 한 모서리 기준 앵커를 쓰지 않는다.
2. **패널 최대 크기 상한 1180x684** (`RelicPopup` 이 현행 최대). 신규·수정 팝업 패널은 이 상한 이하여야 한다. 근거: 캔버스 최소 1280x720 에서 가로 여유 50/변, 세로 여유 18/변이 남는 값.
3. **`Dim` 은 루트 전체 스트레치**(현행 유지). `PopupRoot` 가 캔버스 전체라 확장 구간도 덮고 HUD 터치를 막는다.
4. **내용이 넘칠 때**: 패널을 줄이지 않는다(축소 정책 없음). 긴 목록은 이미 팝업별 `CHPoolingScrollView` 안에서 스크롤된다(예: `RelicPoolingScrollView`, `TowerFloorPoolingScrollView`). 패널 크기는 고정이고 비율이 바뀌어도 팝업 안 레이아웃은 불변이다.
5. **하단 고정 버튼**: 패널 안 좌표를 그대로 유지한다. 패널 아래 끝에서 20 올린 위치에 둔다(침입 지도 `SelectButton` 아래 끝 620 / 패널 높이 640 = 여백 20). 패널 자체가 항상 화면 안이라 별도 처리가 필요 없다.
6. **패널 확대(태블릿 4:3 에서 키우기)는 하지 않는다**: 1280x960 에서 764x524 패널은 작아 보이지만 정보량·가독성 문제가 없어 YAGNI(사용자 확인은 10장 D2).

### 6.2 팝업 패널 감사표 (캔버스 최소 1280x720 기준 검산)

| 팝업 | 패널 크기 | 앵커 | 조치 | 가로 여유/변 | 세로 여유/변 |
|---|---|---|---|---|---|
| ShopPopup, CodexPopup, QuestPopup, CloudPopup, LordLevelPopup, RankingPopup | 764x524 | 중앙 | 없음 | (1280−764)/2 = 258 | (720−524)/2 = 98 |
| RecordsPopup | 524x604 | 중앙 | 없음 | 378 | 58 |
| RelicPopup | 1180x684 | 중앙 | 없음 | 50 | 18 |
| WorldMapPopup(침입 지도), DailyPopup, FormationPopup, TowerPopup | 1136x640 | **좌상단 → 중앙** | `Panel` 앵커 변경 | 72 | 40 |
| ConfirmPopup, ToastView | (토스트 하단 중앙 560x72, 아래 120) | 하단 중앙 | 없음 | — | 토스트 위쪽 끝 192 < 720 |

- 앵커 변경 값(4개 팝업 `Panel` 동일): `m_AnchorMin` (0,1) → **(0.5,0.5)**, `m_AnchorMax` (0,1) → **(0.5,0.5)**, `m_Pivot` (0,1) → **(0.5,0.5)**, `m_AnchoredPosition` (72,−40) → **(0,0)**, `m_SizeDelta` (1136,640) 불변.
- 1280x720 에서 결과가 현행과 **동일**하다: 좌상단 기준 중심 = (72 + 568, −40 − 320) = (640, −360) = 화면 중심. 패널의 자식은 모두 `Panel` 기준 좌표라 건드리지 않는다.
- 토스트: 하단 중앙 `m_AnchoredPosition` (0,120), 크기 560x72 → 위쪽 끝 192. 하단 인셋 최대 42(예시) 보다 위, HUD 하단 버튼 위쪽 끝(88)보다 32 위. 변경 없음.

### 6.3 침입 지도(WorldMapPopup) 구체 재배치안

| 안 | 내용 | 효과 | 판정 |
|---|---|---|---|
| **A. 루트 수정만 (권장)** | 3·4 장의 Expand + 위 `Panel` 앵커 중앙화. 패널 내부는 한 칸도 안 바꿈 | 논리 높이 ≥ 720 이라 패널(640)이 40/변 여유로 항상 보임. 난이도 박스(아래 끝 620)·선택 버튼(아래 끝 620)·닫기 버튼 전부 화면 안 | 채택 |
| B. 하단 영역을 하단 앵커 스트립으로 | `BottomText`/`DifficultyBox`/`SelectButton` 을 `Panel` 하단 앵커로 옮겨 패널 높이가 줄어도 바닥에 붙게 | 패널 높이 가변화 가능성에 대비 | 패널 높이가 640 고정이라 현재는 효과 0, 프리팹 수정 3건 추가 → 폐기(YAGNI) |
| C. 패널 높이 가변(최대 640, 작으면 `MapArea` 축소) | 작은 화면에서 지도 영역(520x472)을 줄임 | 하한 이하 화면 대응 | 하한 밖은 지원 범위 밖 → 폐기 |

최종 배치(침입 지도, 모든 비율 공통): 패널 1136x640 중앙. 패널 안 좌표 — `MapArea` (232,−80) 520x472, `RulesColumn` (12,−256) 328x208, `BottomText` (24,−564) 480x56, `DifficultyBox` (512,−512) 240x108, `SelectButton` (768,−564) 352x56, `CloseButton` (1072,−8) 48x48. 하단 열은 패널 아래 끝에서 20 위. 1280x720 기준 화면 안 좌표: 패널 x 72..1208, y 40..680; `SelectButton` y 604..660; `DifficultyBox` y 552..660.

---

## 7. 엣지 케이스

| 케이스 | 처리 |
|---|---|
| 16:9 폰 + 가로 인셋 합 > 120 논리 (`HudRoot` 폭 < 1160) | 지원 하한 밖. 겹침을 막지 않는다. 게이트에 하한 값을 명시하고 R3 가 하한까지만 보증 |
| 하단 인셋 > 60 논리 (`HudRoot` 높이 < 660) | 지원 하한 밖. 시스템 3버튼 바 등은 `Screen.safeArea` 에 반영되므로 `SafeAreaFitter` 가 흡수하지만 `RecordPanel`↔`StagePanel` 이 겹칠 수 있음 |
| 팝업 패널 가로 한쪽 인셋 > (캔버스 폭 − 1180)/2 | 16:9(캔버스 1280) 이면 50 논리 초과 시 `RelicPopup` 이 인셋을 침범. M2~M7 은 190 이상이라 해당 없음 |
| 회전(가로 좌/우 반전) | `SafeAreaFitter.Update` 가 `Screen.safeArea` 변화를 감지해 재계산. 인셋이 반대쪽으로 이동 |
| 분할 화면·창 모드 | 임의 비율에도 Expand 로 하한이 보장됨 |
| 마을 진입 때마다 | `SetRoot` 를 `Start` 마다 호출(4.4) |
| 토스트가 HUD 보다 먼저 | 4.4 순서 1 로 `PopupRoot` 에 붙음 |
| 에디터 Game 뷰 | `SafeAreaFitter` 는 `[ExecuteAlways]` 라 Device Simulator 인셋도 반영 |
| 배틀·로딩 씬 | 불변. 단 `ConfirmPopup` 은 공용이라 마을에서만 `PopupRoot` 로 뜬다 |

---

## 8. 구현 요청사항 (gameplay-programmer 용)

### 8.1 Enum / Interface / 에셋 키 / SO·JSON

- Enum: **추가 없음**.
- Interface: **추가 없음**(공개 진입점이 없는 배선 변경).
- 에셋 키: **신규 프리팹 없음**. `Village.unity` 의 `UICanvas` 아래 정적 RectTransform `HudRoot`, `PopupRoot` 2개를 추가(런타임 생성 금지).
- SO / JSON 스키마: **없음**.

### 8.2 씬·프리팹 값

| 대상 | 필드 | 값 |
|---|---|---|
| `Village.unity` `UICanvas` `CanvasScaler` | `m_ScreenMatchMode` | 0 → **1** (Expand) |
| `Village.unity` `HudRoot` (신규) | anchor / offset | (0,0)~(1,1) / 0, 컴포넌트 `SafeAreaFitter`(`_gameCamera` = 마을 `Main Camera`, `_target` = 자기 RectTransform) |
| `Village.unity` `PopupRoot` (신규) | anchor / offset | (0,0)~(1,1) / 0, 컴포넌트 없음, 형제 순서는 `HudRoot` 뒤 |
| `VillageController` (씬 컴포넌트) | `_hudRoot`, `_popupRoot` | 위 두 RectTransform 연결 |
| `VillageHud.prefab` `TowerButton` | `m_AnchoredPosition` | (228,16) → **(−228,16)** (5.3 안 A) |
| `WorldMapPopup.prefab`, `DailyPopup.prefab`, `FormationPopup.prefab`, `TowerPopup.prefab` 의 `Panel` | `m_AnchorMin` / `m_AnchorMax` / `m_Pivot` / `m_AnchoredPosition` | (0.5,0.5) / (0.5,0.5) / (0.5,0.5) / (0,0), `m_SizeDelta` (1136,640) 불변 |

### 8.3 코드 연결점 (파일만 명시, 구조는 구현자 판단)

| 파일 | 변경 |
|---|---|
| `Assets/_Lair/Scripts/Village/VillageController.cs` | `[SerializeField]` 루트 2개 + 4.4 의 `SetRoot` 3지점. 77행 `ShowUIAsync(EUI.VillageHud, ...)` 전후, `Start` 최상단 |
| `Assets/_Lair/Scenes/Village.unity` | 8.2 |
| `Assets/_Lair/Art/UI/VillageHud.prefab` | `TowerButton` 위치 한 값 |
| `Assets/_Lair/Art/UI/{WorldMap,Daily,Formation,Tower}Popup.prefab` | `Panel` 앵커 한 블록 |
| `Assets/_Lair/Tests/EditMode/UI/ResolutionAdaptationTests.cs` (신규) | 9장 |
| `SafeAreaFitter.cs` | 변경 없음(재사용). 코드 변경이 필요하면 구현자가 보고 |

### 8.4 코드 외 주의

- 프리팹 수정은 `PrefabUtility` 에디터 스크립트로 한 번 돌리고 삭제(Rule 04 §3: 일회용 authoring 툴은 레포에 남기지 않는다) 하거나 수동 편집한다.
- `VillageHud`의 `DecoTopGrad` 등 상단 장식은 `HudRoot` 안에 있어 안전영역(구멍 쪽 인셋)에서 끊긴다. 허용한다(밝기 차이만 보임, 정보 손실 없음). 개선이 필요하면 별도 건.

---

## 9. 검증 게이트

### 9.1 EditMode 테스트 (`ResolutionAdaptationTests`)

모든 테스트는 한글 메서드명(프로젝트 규칙). 매트릭스 M1~M7(2장)을 데이터 행으로 쓴다.

| ID | 항목 | 방법 | 통과 기준 |
|---|---|---|---|
| T1 | 스케일러 설정 | `Village.unity` 의 `UICanvas` `CanvasScaler` 읽기 | `screenMatchMode == Expand`, `referenceResolution == (1280,720)` |
| T2 | 논리 크기 | M1~M7 각각 `scale = min(W/1280, H/720)`, 논리 = `W/scale x H/scale` 계산 | 모든 행 논리 폭 ≥ 1280, 논리 높이 ≥ 720 (2장 표 값과 ±0.1 일치) |
| T3 | `HudRoot` 영역 | 인셋 적용(`SafeAreaFitter.ComputeAnchors` 재사용) 후 `HudRoot` 논리 크기 | M2~M7 각각 폭 ≥ 1160, 높이 ≥ 660 (2장 표 값 일치) |
| T4 | HUD 겹침 없음 | `VillageHud.prefab` 을 `HudRoot` 논리 크기의 부모 RectTransform 아래 인스턴스화 → 5.1 의 존별 사각형(`TopBar`, 좌측 메뉴 8칸 합집합, `RecordPanel`, `StagePanel`, `TowerButton`, `HeroButton`, `SortieButton`, `StagePrevButton`, `StageNextButton`) 월드 코너로 계산, `Deco*` 제외 | M1~M7 + **하한 케이스(1160x660)** 에서 모든 쌍 교차 면적 0 (간격 ≥ 0) |
| T5 | 하단 버튼 순서 | `TowerButton` x < `HeroButton` x < `SortieButton` x | 5.3 안 A 반영 확인 |
| T6 | 팝업 패널 앵커 | 12개 팝업 프리팹(`Shop` `Codex` `Records` `Quest` `LordLevel` `Ranking` `Cloud` `Relic` `Tower` `Daily` `Formation` `WorldMap`)의 루트 직계 자식 중 스트레치가 아닌 것 | 전부 `anchorMin == anchorMax == pivot == (0.5,0.5)` |
| T7 | 팝업 패널 안전 | 위 12개의 non-stretch 패널 사각형을 `PopupRoot`(캔버스 전체) 논리 크기 위에 배치 | M1~M7 각각 패널 사각형 ⊂ (캔버스 − 예시 인셋). 단 M1 은 인셋 0. 패널 최대 크기 ≤ 1180x684 |
| T8 | `Dim` 전체 덮음 | 12개 프리팹의 `Dim` 스트레치 확인 | `anchorMin (0,0)`, `anchorMax (1,1)`, offset 0 |
| T9 | 루트 배선 | `Village.unity` 에 `HudRoot`(`SafeAreaFitter` 포함)·`PopupRoot` 존재, `VillageController` 필드 연결 | null 0건, `HudRoot` 가 `PopupRoot` 보다 앞 형제 |
| T10 | 회귀 | 기존 `SafeAreaFitterTests`, `VillageHudVisualPolishTests`, `DotSceneRegressionTests` | 전부 통과 |

테스트 구현 노트: 월드 코너 계산이 `LayoutGroup` 영향을 받는 프리팹이면 `LayoutRebuilder.ForceRebuildLayoutImmediate` 를 호출한다. `Canvas` 없이 RectTransform 부모만으로 앵커 계산이 되므로 PlayMode 불필요.

### 9.2 실기·Game 뷰 확인 (자동화 불가)

| ID | 항목 | 방법 | 통과 기준 |
|---|---|---|---|
| R0 | **원인 실측** | S25+ 실기 1대와 Device Simulator 1회, 두 곳 모두에서 현행(변경 전) 빌드로 `Screen.width/height`, `Screen.safeArea`, 캔버스 `rect`, 마을 `Main Camera.rect` 를 로그 | 1.2~1.5 의 계산값(논리 1413x652, 구멍 인셋 유무)과 대조해 차이를 기록. 차이가 크면(논리 높이 < 640) 이 기획서 1.5 주의 항목을 갱신 |
| R1 | Game 뷰 7해상도 스크린샷 | 마을 씬을 1920x1080, 2340x1080, 2400x1080, 2520x1080, 2640x1080, 1920x1200, 2048x1536 으로 캡처 | HUD 사각형 서로 안 겹침, 영웅이 가운데 보임, 좌측 메뉴·상단 바가 가장자리 정렬 |
| R2 | 인셋 있는 기기 | Device Simulator 의 구멍·제스처 바가 있는 기기(가로) | 좌측 메뉴·상단 바 왼쪽이 구멍 영역 안쪽에서 시작, 하단 버튼이 제스처 바 위 |
| R3 | 침입 지도 | 7해상도에서 월드맵 팝업 열기 | 패널이 화면 가운데, 난이도 박스·"이 스테이지로 선택"·닫기 버튼이 전부 보임, 좌우 쏠림 없음, 배경 Dim 이 화면 끝까지 덮음 |
| R4 | 나머지 팝업 | 탑·데일리·편성·유물·기록·상점 열기(7해상도 중 M2, M7 필수) | 패널 전체가 화면 안, Dim 이 HUD 터치 차단 |
| R5 | 토스트·확인창 | 토스트 1개, 확인창 1개 (M2 에서) | 가운데 정렬, 하단 버튼과 안 겹침 |
| R6 | 배틀·로딩 회귀 | 배틀 씬 16:9, 20:9 캡처 | 변경 전과 동일 |

qa-simulator 밸런스 게이트: 불필요(수치·AI 불변).

---

## 10. 열린 결정 (사용자 선택)

| # | 질문 | 권장 | 대안 |
|---|---|---|---|
| D1 | 하단 버튼 열 순서를 "탑 · 지도 · 방어하기" 로 바꿔 좁은 화면 여유를 키울까? | 바꾼다(5.3 A) | 순서 유지 + 우측 패널 폭 축소(B, 작업량 큼) |
| D2 | 태블릿(4:3, 16:10)에서 팝업/HUD 를 키우지 않고 여백만 늘릴까? | 여백만(6.1-6) | 태블릿 전용 확대 스케일 — 추가 튜닝 필요 |
| D3 | 팝업은 안전영역을 직접 따르지 않고 "중앙 고정 + 크기 상한" 으로만 보호할까? | 그렇다(4.3) | 팝업도 안전영역 적용 — `Dim` 오버행 작업이 팝업 12종에 추가됨 |
| D4 | 지원 하한(HUD 폭 1160·높이 660) 밖 기기(16:9 폰 + 큰 노치)는 지원 범위 밖으로 둘까? | 둔다(2.1) | 하한 밖 보정(HUD 축소) 추가 — 구현 복잡 |

---

## 11. Self-Review

- Placeholder·모호 표현·두 갈래 위임 표현 검색 결과 0건. 디자인 결정은 전부 한 갈래(Expand, `PopupRoot` 전체 스트레치, 앵커 중앙화, `TowerButton` 이동). 열린 선택은 10장에 모았다.
- 내부 일관성: 하한 폭 1160·높이 660 이 2.1, 5.2, 9.1(T3, T4)에서 동일. `TowerButton` x −228 이 5.3, 8.2, T5 에서 동일. 패널 상한 1180x684 가 2.1, 6.1, 6.2 에서 동일. 검산: 폭 1160 = 2×580, 높이 660 = 320+340.
- 식별자: `HudRoot`, `PopupRoot`, `SafeAreaFitter`, `CHMUI.SetRoot`, `VillageController`, `_hudRoot`, `_popupRoot` 로 통일.
- 근거 경로: 1장에 파일·행 번호 표기. 실기 차이는 R0 로 분리.
- 스코프: 단일 구현 단위(마을 표시 계층). 새 JSON·Enum·Interface·프리팹 없음. §8 위반 없음(신규 화면·메인 메뉴·세팅 없음).
- UI 목업: `.mockups/resolution-adaptation.html` 작성.
