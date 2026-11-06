# 전 씬 전 UI 해상도 대응 — 마을·배틀·로딩 (resolution-adaptation)

> 구현용 기획서(수치 SoT). 사용자용 가이드는 `resolution-adaptation.html`, 합의용 목업은 `.mockups/resolution-adaptation.html`.
> 개정 이력: 초판은 마을 HUD + 팝업 4종만 다뤘다. 이번 개정은 **마을·배틀·로딩 3개 씬의 모든 UI 프리팹을 전수 점검**하도록 범위를 넓혔다(§2 인벤토리).

## 0. 헤더

| 항목 | 내용 |
|---|---|
| **목표** | 마을·배틀·로딩 씬에 나오는 모든 UI(팝업 전부 포함)가 폰(16:9~22:9)·태블릿(16:10, 4:3) 어디서나 겹침·잘림 없이 맞는다. |
| **검증 가설** | "배틀이 이미 쓰는 `Expand` 스케일러 + `HudRoot`(Safe Area) / `PopupRoot` 루트 분리 패턴을 마을에 옮기고, 로딩 스케일러를 같은 `Expand` 로 맞추고, 배틀·마을의 남은 프리팹 값 몇 개만 고치면, UI 프리팹 내부 재배치 없이 전 비율에서 겹침·잘림이 사라진다." |
| **현재 단계 범위 적합성** | 범위 안. v0.3 §8 의 신규 화면·콘텐츠가 아니라 기존 3개 씬 UI 의 레이아웃 보정이다. 별도 메인 메뉴/세팅 화면 없음, 신규 몬스터·재화·규칙 없음. 배틀 월드 레터박스 규칙(`battle-wide-screen-fit.md` §2)은 불변. |
| **핵심 메커니즘** | ① 마을·로딩 `CanvasScaler` 를 `Expand` 로 통일(배틀은 이미 Expand). ② 마을에 `HudRoot`/`PopupRoot` 를 두고(둘 다 기존 `SafeAreaFitter` 재사용) `VillageController` 가 배틀과 같은 순서로 `CHMUI.SetRoot` 전환, 배틀 `PopupRoot` 도 Safe Area 방식으로 바꾼다. ③ 로딩 `Panel` 에도 `SafeAreaFitter` 를 붙여 하단 인셋을 피한다. ④ 프리팹·씬 값 보정 8개 ID(V1~V3, B1~B2, C1~C2, P1; §4). 인셋 띠와 UI 사각형을 **쌍으로** 검사한다. ⑤ 전수 인벤토리 순회 테스트로 이후 UI 추가 시 누락 방지. |

- 새 JSON 스키마: **없음**(Rule 00 데이터 구조 확인 게이트 N/A). 수치는 씬·프리팹 직렬화 값(Rule 02 §11 배선 값).
- 신규 Enum 없음 · 신규 Interface 없음 · 신규 프리팹/에셋 키 없음. 밸런스 수치 변경 없음(qa-simulator 불필요).

### 0.1 초판과 달라진 점 (기존 결정과의 충돌 명시)

| 항목 | 초판 | 개정 | 충돌 처리 |
|---|---|---|---|
| 로딩 씬 | "변경 없음" | `CanvasScaler` Match 0.5 → **Expand** (보정 C1) | 초판 §7 "배틀·로딩 불변" 문장을 폐기. 배틀 레터박스 규칙은 그대로 |
| 배틀 씬 | "변경 없음" | `BattleHud` 우측 패널 2개 y 보정(B1, 조건부 D6) + `SkillUnlockBanner` 밴드 폭 점검(B2, 캡처 후 결정) | 초판 §7 "배틀 불변" 폐기. `battle-wide-screen-fit.md` 의 Dim 오버행(±400)·`PopupRoot` 1280x720 방식은 유지 |
| `battle-wide-screen-fit.md` §3.1 | "Expand 는 카메라 rect/화면 기준 해석과 무관, 구현자가 W3 에서 실측" | 본 문서 §3.3 이 근거를 보강: Expand 는 두 해석이 같은 값이 되고, Match 0.5 는 달라진다 | 충돌 없음, 보강 |
| 인벤토리 | 마을 HUD + 팝업 12종 | 3개 씬 UI 전부 47행 | 확장 |
| **인셋 판정 정정 (2차 개정)** | 로딩 §2.5·§8: "박스 밖 띠가 인셋을 흡수한다" → OK. 팝업: "패널 중앙 고정이라 인셋을 침범하지 않는다"(D3) | **둘 다 틀렸다.** 띠가 인셋을 흡수하는 조건은 띠 두께(px) ≥ 인셋(px) 뿐이다(좌우 띠, M6·M7 위아래 띠). M1~M5 는 박스 높이 = 화면 높이라 하단 인셋이 박스 안으로 들어온다(§3.1.1). 로딩 하단 요소가 인셋에 덮이고(§1.8), 큰 팝업 패널 프레임이 하단 인셋 띠에 걸친다(§2.6) | 로딩: 보정 C2. 팝업: 보정 P1. **D3 폐기**, 새 D8 로 교체 |
| 로딩 판정 | 초판·1차 개정: "취약, 보정 C1 하나뿐" | "확정 ✗, 보정 C1 + C2" | 인벤토리 문제 확정 7 → 9행 (§4 집계) |

---

## 1. 근본 원인 (코드·프리팹 근거)

### 1.1 기존 인프라 (재사용 대상, 신규 인프라 코드 없음)

| 인프라 | 위치 | 현재 사용처 |
|---|---|---|
| `SafeAreaFitter` (`Screen.safeArea ∩ 카메라 pixelRect` → 앵커) + `ComputeAnchors` 정적 함수, 테스트 4건 | `Assets/_Lair/Scripts/UI/SafeAreaFitter.cs`, `Tests/EditMode/SafeAreaFitterTests.cs` | 배틀 `HudRoot` 만 |
| 루트 분리: HUD 직전 `SetRoot(_hudRoot)`, 직후 `SetRoot(_popupRoot)` | `Scripts/Battle/BattleController.cs` 245~255행 | 배틀만 |
| `LetterboxFramer` (`_targetAspect` 1.7778, `_maxAspect`) | `Scripts/Battle/LetterboxFramer.cs`; 배틀 씬 `_maxAspect` 2.3333333(`Battle.unity` 6519행), 로딩 씬 `_targetAspect` 1.7778 + `_maxAspect` 미설정=0(`Loading.unity` 8007~8012행) | 배틀·로딩 |
| `CHMUI.SetRoot` + 모든 UI 를 루트에 전체 스트레치 | `Packages/com.chvj.unityinfra/Runtime/UI/CHMUI.cs` 90~121, 249~257행 | 전 씬 공통 |

### 1.2 씬별 캔버스 현황

| 씬 | 렌더 모드 | 스케일러 | 루트 | 근거 |
|---|---|---|---|---|
| Village | Overlay (`m_RenderMode: 0`) | 기준 1280x720, **Match 0.5** (`m_ScreenMatchMode: 0`) | 없음(캔버스 직접, 자식 0개) | `Village.unity` 11381~11401행 |
| Battle | ScreenSpaceCamera (`m_RenderMode: 1`) | 기준 1280x720, **Expand** (`m_ScreenMatchMode: 1`) | `HudRoot`(Safe Area), `PopupRoot`(중앙 1280x720) | `Battle.unity` 6637~6654행, 685행, 6321~6344행 |
| Loading | ScreenSpaceCamera (`m_RenderMode: 1`) | 기준 **1920x1080**, **Match 0.5** | 없음. `Panel`(전체 스트레치)에 UI 직접 배치. `CHMUI` 미사용(`LoadingHud.cs` 7행 주석, `LoadingController` 는 `CHMUI.Init` 만) | `Loading.unity` 1140~1160행, 4676~4706행 |

### 1.3 원인 1 — 마을 캔버스가 Match 0.5 이다

- 산식: `scale = sqrt(W/1280 · H/720)`, 논리 크기 = `(W/scale, H/scale)`. 16:9 보다 가로로 길면 논리 높이가 720 미만, 짧으면 논리 폭이 1280 미만이 된다. 마을 HUD·팝업 오프셋은 전부 1280x720 에서만 맞게 저작돼 있다.

| 기기(px) | 비율 | 현행 scale | 현행 논리 크기 | `Expand` 논리 크기 (scale = min(W/1280, H/720)) |
|---|---|---|---|---|
| M1 1920x1080 | 16:9 | 1.5000 | 1280x720 | 1280x720 (1.5) |
| M2 2340x1080 (S25+) | 19.5:9 | 1.6559 | 1413.1x652.2 | 1560x720 (1.5) |
| M3 2400x1080 | 20:9 | 1.6771 | 1431.1x644.0 | 1600x720 (1.5) |
| M4 2520x1080 | 21:9 | 1.7185 | 1466.4x628.4 | 1680x720 (1.5) |
| M5 2640x1080 | 22:9 | 1.7589 | 1500.9x614.0 | 1760x720 (1.5) |
| M6 1920x1200 | 16:10 | 1.5811 | 1214.3x759.0 | 1280x800 (1.5) |
| M7 2048x1536 | 4:3 | 1.8475 | 1108.5x831.4 | 1280x960 (1.6) |

### 1.4 원인 2 — 마을에는 Safe Area 처리가 없다

- `VillageController.cs` 77행 `ShowUIAsync(EUI.VillageHud, ...)` 앞에 `SetRoot` 호출 없음 → HUD 가 Safe Area 를 무시하고 캔버스 전체에 스트레치.
- `ProjectSettings/ProjectSettings.asset` 70~71행 `androidStartInFullscreen: 1`, `androidRenderOutsideSafeArea: 1` → 앱이 카메라 구멍·제스처 바 영역까지 그린다.

### 1.5 원인 3 — 마을 HUD 영역 분할이 1280x720 에만 맞는 여유다

근거 `Assets/_Lair/Art/UI/VillageHud.prefab` (논리 단위, 위쪽 기준 y).

| 영역(오브젝트) | 앵커 | 사각형 |
|---|---|---|
| `TopBar` | 상단 스트레치 | 높이 88 (y 0..88) |
| 좌측 메뉴 8칸 (`LordButton` `ShopButton` `CodexButton` `QuestButton` `RecordsButton` `RankingButton` `RelicButton` `DailyButton`) | 좌상단 | 2열 x 4행, 각 112x112, x 16..256, y 104..600 |
| `RecordPanel` | 우상단 | 256x216, 오른쪽 여백 16, y 104..320 |
| `StagePanel` | 우하단 | 340x324, 여백 16, y (H−340)..(H−16) |
| `HeroButton` / `SortieButton` / `TowerButton` | 하단 중앙 | 중앙 기준 x: Hero −170..−70, Sortie −62..+170, Tower +178..+278(현행), 높이 72, 아래 16 |
| `StagePrevButton` / `StageNextButton` | 정중앙 | 48x68, 중앙 기준 x: −224..−176 / +176..+224 |

| 쌍 | 여유(음수=겹침) | 1280x720 | 현행 실패 지점 |
|---|---|---|---|
| `RecordPanel` ↔ `StagePanel` (세로) | `H − 660` | 60 | M2 −7.8, M3 −16.0, M4 −31.6, M5 −46.0 |
| `TowerButton` ↔ `StagePanel` (가로, 현행 위치) | `W/2 − 634` | **6** | M6 −26.8, M7 −79.7 |
| `StageNextButton` ↔ `StagePanel` (H ≤ 748 일 때 세로 겹침) | `W/2 − 580` | 60 | 실제 겹침 없음 |

### 1.6 원인 4 — 마을 팝업 패널 4종이 좌상단 고정이다

| 패널 크기 | 앵커 | 팝업(프리팹) |
|---|---|---|
| 764x524 | 중앙 | Shop, Codex, Quest, Cloud, LordLevel, Ranking |
| 524x604 | 중앙 | Records |
| 1136x640 (`ModalBody`, scale 1) | 중앙 | Relic 본체. 루트 직계에 장식 글로우 `DecoModalGlow` 1180x684 가 함께 있다(**정정**: 초판은 이 글로우의 크기를 패널로 오인했다) |
| **1136x640** | **좌상단 (0,1), pivot (0,1), 위치 (72,−40)** | **WorldMap, Daily, Formation, Tower** |

근거: `WorldMapPopup.prefab` 3889~3893행(`Panel`), `DailyPopup.prefab` 984~987행, `FormationPopup.prefab` 4254~4257행, `TowerPopup.prefab` 1478~1481행. 1280x720 에서는 정중앙이지만(72=(1280−1136)/2, 40=(720−640)/2) 화면이 변하면 중앙에서 벗어난다.

침입 지도 현행 잘림 (패널 안 좌표, 화면 안 위치 = +40):

| 요소 | 패널 안 사각형 (근거) | 화면 안 아래 끝 |
|---|---|---|
| `DifficultyBox` | x 512..752, y 512..620 (`WorldMapPopup.prefab` 2893~2894행) | 660 |
| `SelectButton` | x 768..1120, y 564..620 (3984~3985행) | 660 |
| `BottomText` | x 24..504, y 564..620 (4836~4837행) | 660 |
| `Panel` | 1136x640 | 680 |
| `CloseButton` | x 1072..1120, y 8..56 (2755~2756행) | x 1144..1192 |

| 비율 | 논리 H | 패널 아래(680) | 선택·난이도 아래(660) | 오른쪽 |
|---|---|---|---|---|
| M2 | 652.2 | 27.8 잘림 | 7.8 잘림 | 오른쪽 여백 205.1 vs 왼쪽 72 |
| M3 | 644.0 | 36.0 | 16.0 | 여백 223.1 |
| M4 | 628.4 | 51.6 | 31.6 | 여백 258.4 |
| M5 | 614.0 | 66.0 | 46.0 | 여백 292.9 |
| M7 | 831.4 | 0 | 0 | 패널 오른쪽 1208 > 폭 1108.5 → 99.5 잘림, `CloseButton` 전체가 화면 밖 |

**근거 한계**: 계산상 침입 지도 선택 버튼은 7.8~46 만 잘린다. 사용자 스크린샷은 "아예 안 보임" 이다. 실기에서 `Screen.height` 가 더 작거나 시스템 바가 차지했을 가능성이 있다 → 게이트 V0 로 확정한다. 해결안은 논리 높이를 항상 ≥720 으로 만들어 이 차이와 무관하게 유효하다.

### 1.7 원인 5 — 로딩 씬: (a) 스케일러 해석 차이, (b) 하단 인셋이 하단 요소를 덮는다

(b)는 2차 개정에서 추가·정정한 판정이다(§1.8 계산). (a)는 아래.

- 로딩은 `LetterboxFramer` 가 캔버스를 게임 카메라(16:9 박스)에 붙인다(`LetterboxFramer.cs` 114~115행). 캔버스 기준은 1920x1080, `Match 0.5`.
- 확인 못 한 점: `CanvasScaler` 가 scale 을 **디스플레이 전체 크기**로 계산하는지 **카메라 박스 크기**로 계산하는지는 코드로 단정하지 않았다(Unity 내부 동작). 게이트 W3(논리 크기 실측)로 확정한다.
- 두 해석에서의 논리 크기(박스 BW x BH 를 scale 로 나눈 값):

| 비율 | 박스(px) | 해석 H1: 디스플레이 기준 scale = sqrt(W/1920 · H/1080) | 해석 H2: 박스 기준 |
|---|---|---|---|
| M1 1920x1080 | 1920x1080 | 1.0000 → 1920x1080 | 1920x1080 |
| M2 2340x1080 | 1920x1080 | 1.1040 → 1739x978 | 1920x1080 |
| M3 2400x1080 | 1920x1080 | 1.1180 → 1717x966 | 1920x1080 |
| M4 2520x1080 | 1920x1080 | 1.1456 → 1676x943 | 1920x1080 |
| M5 2640x1080 | 1920x1080 | 1.1726 → 1637x921 | 1920x1080 |
| M6 1920x1200 | 1920x1080 | 1.0541 → 1821x1025 | 1920x1080 |
| M7 2048x1536 | 2048x1152 | 1.2316 → 1663x935 | 1920x1080 |

- 로딩 UI 가장 넓은 요소는 `TitleMain` 1600 폭이다(`Loading.unity` 6827행). H1 에서 M5 의 1637 이 최소라 7개 비율은 모두 통과하지만 여유 37 이다. 25:9(3000x1080) 이상이면 H1 에서 1536 < 1600 이 되어 잘린다. **`Expand` 로 바꾸면 두 해석이 모두 1920x1080 으로 같아진다**(M2: min(2340/1920, 1080/1080) = 1.0 → 박스 1920x1080; M7: min(2048/1920, 1536/1080) = 1.0667 → 2048/1.0667 = 1920, 1152/1.0667 = 1080).

### 1.8 원인 6 — 하단 인셋이 박스 안으로 들어오는 화면에서 로딩 하단 요소가 덮인다 (초판 판정 정정)

**정정**: 초판·1차 개정은 로딩 UI 가 "16:9 박스 안에만 그려지므로 박스 밖 띠가 인셋을 흡수한다" 고 판정했다. 이 판정은 **좌우 띠**와 **박스가 세로를 다 채우지 못하는 M6·M7** 에서만 맞다. M1~M5 는 박스 높이 = 화면 높이(`ComputeViewportRect` 가 풀 높이 박스를 만듦)라 하단 인셋이 박스 안으로 들어오고, 로딩 하단 요소는 아래 36 에서 시작해 인셋보다 아래에 놓인다. 로딩 씬에는 Safe Area 처리가 없다(`SafeAreaFitter` 사용처는 배틀 `HudRoot` 뿐, 로딩 `Panel` 은 전체 스트레치 `Loading.unity` 4702~4705행).

인셋이 박스 안으로 들어오는 양 = max(0, 인셋px − 띠px) ÷ scale (`Expand` 후 로딩 scale = min(W/1920, H/1080), 예시 인셋은 §3.1 과 동일: M2~M5 좌 132·아래 63, M6 아래 30, M7 아래 40, M1 0).

| 비율 | 박스(px) / 띠(px) | scale | 하단 인셋 → 박스 안 | 좌 인셋 → 박스 안 | `ProgressBarBg`(아래 36..80) 겹침 | `PercentText`(아래 40..84) 겹침 | `DescText`(아래 104..160) 겹침 | 판정 |
|---|---|---|---|---|---|---|---|---|
| M1 1920x1080 | 1920x1080 / 없음 | 1.0 | 0 | 0 | 0 | 0 | 0 | OK (인셋 0) |
| M2 2340x1080 | 1920x1080 / 좌우 210 | 1.0 | 63 − 0 = **63** | 132 − 210 → 0 (띠 흡수) | 63 − 36 = **27** | 63 − 40 = **23** | 0 (63 < 104) | **✗** |
| M3 2400x1080 | 1920x1080 / 좌우 240 | 1.0 | **63** | 0 (띠 흡수) | **27** | **23** | 0 | **✗** |
| M4 2520x1080 | 1920x1080 / 좌우 300 | 1.0 | **63** | 0 (띠 흡수) | **27** | **23** | 0 | **✗** |
| M5 2640x1080 | 1920x1080 / 좌우 360 | 1.0 | **63** | 0 (띠 흡수) | **27** | **23** | 0 | **✗** |
| M6 1920x1200 | 1920x1080 / 위아래 60 | 1.0 | 30 − 60 → 0 (띠 흡수) | 0 | 0 | 0 | 0 | OK |
| M7 2048x1536 | 2048x1152 / 위아래 192 | 1.0667 | 40 − 192 → 0 (띠 흡수) | 0 | 0 | 0 | 0 | OK |
| 조건 사례 1920x1080, 하단 인셋 63 | 1920x1080 / 없음 | 1.0 | **63** | 0 | **27** | **23** | 0 | **✗** |

- 겹침 양 = 인셋 높이(논리) − 요소 아래 시작값, 요소 높이(44)를 넘지 못함. `ProgressFill`(720x28, 아래 44..72)도 63 − 44 = 19 덮인다.
- 상단 인셋(카메라 구멍이 위에 올 때): 예시 인셋에 상단이 없어 수치로는 겹침 0 이다. 상단 요소 `TitleSmall` 은 위 32 에서 시작하므로 상단 인셋이 32 를 넘으면 덮인다. 보정 C2 의 `SafeAreaFitter` 는 상단 인셋도 같이 반영한다(요소가 인셋 아래로 내려감).
- 좌우 인셋: 가장 넓은 요소 1600 폭은 중앙 기준 ±800 이라 좌우 여백 160. 좌 인셋이 박스 안으로 들어와도 88 논리(16:9 폰) < 160 이라 요소와는 겹치지 않는다(OK).

**코드 근거로 확인한 `SafeAreaFitter` 재사용 가능성** (보정 C2):

| 확인 항목 | 결과 (근거) |
|---|---|
| 로딩 캔버스가 카메라 박스에 붙는가 | `LetterboxFramer.Apply()` 가 `_uiCanvas.renderMode = ScreenSpaceCamera`, `worldCamera = _gameCamera` 로 설정(`LetterboxFramer.cs` 110~117행). 로딩 씬 `_gameCamera` 참조 = 캔버스 `m_Camera` 와 같은 `Main Camera`(`Loading.unity` 1161행·8009행 fileID 1303509417 동일) |
| 앵커 정규화 기준 | `SafeAreaFitter.ComputeAnchors(safe, pixel)` 는 `safe ∩ 카메라 pixelRect` 를 `pixelRect` 기준 0~1 로 정규화(`SafeAreaFitter.cs` 44~60행). 캔버스 rect = 카메라 박스이므로 그대로 맞다 |
| 실행 순서 | `LetterboxFramer` 실행 순서 −50 이 `SafeAreaFitter` −40 보다 먼저라 같은 프레임에 박스 rect 적용 후 인셋 계산(`LetterboxFramer.cs` 10행, `SafeAreaFitter.cs` 8행). 화면 크기 변동 시 `Update` 가 `safe`/`pixelRect` 변화를 감지해 재계산 |
| 붙일 대상 | `Panel`(전체 스트레치, `Loading.unity` 4702~4706행): 하위 6요소가 전부 이 `Panel` 기준 앵커. `Panel` 의 `Image` 알파 0(4720행)이라 줄어들어도 보이는 변화 없음. 캔버스 직계 자식은 `Panel` 하나 |
| 배경·월드 미러 | 월드 오브젝트(`Backdrop`, `@DotStage` 등)와 `LetterboxBackground` 카메라는 UI 가 아니므로 인셋과 무관하게 전체를 채운다 — 변경 없음 |
| `LoadingHud` 스크립트 | 앵커·위치를 코드로 만지지 않음(`LoadingHud.cs` 에 앵커 관련 호출 없음 — 검색 결과 0) → `Panel` 크기 변경과 충돌 없음 |

→ **기존 `SafeAreaFitter` 를 그대로 재사용하면 맞다.** 대안(요소를 인셋만큼 올리는 개별 오프셋)은 비율·인셋마다 값을 새로 정해야 하고 코드 분기가 필요해 비채택.

#### (a) 스케일러 해석 차이 (1차 개정 내용 유지)

- 확인 못 한 점·H1/H2 표는 이 절 앞부분(§1.7)의 표를 따른다. 보정 C1(Expand) 유지.

---

## 2. 전수 인벤토리

### 2.1 집계 기준

- 출처: `Scripts/Data/CommonEnum.cs` `EUI` 열거 23개 값(91~119행) 중 `SpawnerStatusTooltip` 은 "v0.6.4 폐기, enum 자리만 보존"이라 프리팹이 없다 → 살아 있는 `EUI` **22개**. `Assets/_Lair/Art/UI/*.prefab` 은 **43개**(EUI 22 + `EUI` 밖 보조 프리팹 3 + 셀 18). 씬 직접 배치 UI 는 로딩 `Panel` 1 묶음. 씬 캔버스 3개.
- **총 47행** = 씬 캔버스 3 + 프리팹 `EUI` 22 + 보조 3 + 셀 18 + 로딩 `LoadingHud` 묶음 1.
- 판정 기호: `OK` = 해당 비율에서 겹침·잘림 없음(산식 또는 값 근거 표기). `✗` = 겹침·잘림. `확인 못 함` = 근거를 못 찾았거나 값을 읽지 않음(추정으로 채우지 않음). 비율은 §1.3 의 M1~M7. 판정은 **현행(변경 전) 값** 기준이며 보정 후 값은 §4·§9 에서 검증한다.
- "내부 요소 잘림"은 팝업 패널 **안**의 배치가 패널 고정 크기 안에서 완결되는지를 뜻한다. 패널 크기는 비율과 무관하므로 비율에 의존하는 변화는 없다고 보지만, 패널 안 개별 텍스트가 넘치는지는 전부 읽지 않았다 → 게이트 V3 에서 캡처로 확인.

### 2.2 씬 캔버스 (3행)

| # | 대상 | 씬 | 구분 | 현재 | M1~M7 판정 | 보정 |
|---|---|---|---|---|---|---|
| S1 | Village `UICanvas` | Village | 기타(캔버스) | Overlay, 1280x720, Match 0.5, 루트 없음 (`Village.unity` 11381~11401행) | ✗ M2~M5(논리 높이 614~652 < 720), ✗ M6·M7(논리 폭 1214·1108 < 1280) | V1 |
| S2 | Battle `UICanvas` + `HudRoot` + `PopupRoot` | Battle | 기타(캔버스) | ScreenSpaceCamera, Expand, `HudRoot`(Safe Area), `PopupRoot` 1280x720 중앙 (`Battle.unity` 6637~6654, 685, 6321~6344행) | OK M1~M7 (카메라 박스 16:9~7:3, Expand → 논리 최소 1280x720) | 없음 |
| S3 | Loading `Canvas` | Loading | 기타(캔버스) | ScreenSpaceCamera, 1920x1080, Match 0.5, `SafeAreaFitter` 없음 (`Loading.unity` 1140~1160행, 4702행) | **✗ M2~M5(+조건 사례 16:9 하단 인셋 63)**: 하단 인셋 63 이 `ProgressBarBg` 27·`PercentText` 23 덮음(§1.8). OK M1·M6·M7. 스케일러는 H1·H2 에서 폭 ≥1637 이나 25:9 이상은 ✗ 가능 | C1 + C2 |

### 2.3 `EUI` 22개 + 보조 프리팹 3개 (25행)

| # | 프리팹 (EUI) | 씬 | 구분 | 현재 루트 | 앵커·크기 (근거) | M1~M7 판정 | 보정 |
|---|---|---|---|---|---|---|---|
| 1 | `VillageHud` | Village | HUD | 캔버스 직접 | §1.5 (`VillageHud.prefab`: `TopBar` 12731행 외 값 표) | ✗ M2~M5 `RecordPanel`↔`StagePanel` 세로 겹침 + Safe Area 미적용, ✗ M6·M7 `TowerButton`↔`StagePanel` 가로 겹침, OK M1 | V1 + V2 |
| 2 | `ShopPopup` | Village | 팝업 | 캔버스 직접 | 패널 764x524 중앙, Dim 스트레치 0 (`ShopPopup.prefab` 2331~2334행, dim 2975~2978행) | OK M1~M7 (필요 H 524, W 764) | V1 |
| 3 | `QuestPopup` | Village | 팝업 | 캔버스 직접 | 764x524 중앙 (`QuestPopup.prefab` 2279~2282행, dim 2753~2756행) | OK M1~M7 | V1 |
| 4 | `CodexPopup` | Village | 팝업 | 캔버스 직접 | 764x524 중앙 (`CodexPopup.prefab` 1522~1525행, dim 1391~1394행) | OK M1~M7 | V1 |
| 5 | `RecordsPopup` | Village | 팝업 | 캔버스 직접 | 524x604 중앙 (`RecordsPopup.prefab` 4006~4009행, dim 4777~4780행) | OK M1~M7 (필요 H 604, 최소 논리 H 614.0 → 여유 10) | V1 |
| 6 | `HeroSelectPopup` | Village(레거시 분기, HUD 에서 도달 불가: `VillageHud.cs` 92행이 `WorldMapPopup` 로 연결, `VillageController.cs` 321행 분기 잔존) | 팝업 | 캔버스 직접 | 720x480 중앙 (`HeroSelectPopup.prefab` 94~97행, dim 658~661행) | OK M1~M7 | 없음 |
| 7 | `LordLevelPopup` | Village | 팝업 | 캔버스 직접 | 764x524 중앙 (`LordLevelPopup.prefab` 2783~2786행, dim 563~566행) | OK M1~M7 | V1 |
| 8 | `RankingPopup` | Village | 팝업 | 캔버스 직접 | 764x524 중앙 (`RankingPopup.prefab` 4000~4003행, dim 6263~6266행) | OK M1~M7 | V1 |
| 9 | `CloudPopup` | Village | 팝업 | 캔버스 직접 | 764x524 중앙 (`CloudPopup.prefab` 5178~5181행, 배경 6992~6995행) | OK M1~M7 | V1 |
| 10 | `WorldMapPopup` (침입 지도) | Village | 팝업 | 캔버스 직접 | 패널 1136x640 좌상단 (72,−40), Dim 스트레치 (`WorldMapPopup.prefab` 3889~3893, 1644~1648행) | ✗ M2~M5 패널 아래·선택 버튼 잘림(§1.6), ✗ M7 오른쪽 잘림·`CloseButton` 화면 밖, OK M1·M6 (V3 후에도 M2~M5 패널 아래 680 이 하단 인셋 띠 678 을 2 침범, §2.6) | V3 + P1 |
| 11 | `RelicPopup` | Village | 팝업 | 캔버스 직접 | **정정**: 본체 `ModalBody` 1136x640 중앙 scale 1 + 장식 글로우 `DecoModalGlow` 1180x684 (`RelicPopup.prefab` 10210~10213행의 1180x684 는 글로우 값이며 본체는 구현 리뷰에서 확인; dim 2828~2831행) | 본체 기준 ✗ M4 위·아래 각 (640−628.4)/2 = 5.8, ✗ M5 각 (640−614.0)/2 = 13.0, ✗ M7 좌우 각 (1136−1108.5)/2 = 13.8, OK M1·M2·M3·M6 (글로우 잘림은 장식이라 판정 제외). 인셋: 본체는 현행(캔버스 중앙) 아래 360+320 = 680 → 하단 인셋 띠(678) 2 침범, P1 후(안전영역 중앙) 339+320 = 659 ≤ 678 → **침범 0**. 글로우는 P1 후 339+342 = 681 → 3 침범(장식) → scale 0.97 로 670.7 해소 | V1 + P1 (글로우만 scale) |
| 12 | `FormationPopup` | Village | 팝업 | 캔버스 직접 | 1136x640 좌상단 (72,−40) (`FormationPopup.prefab` 4254~4257행; `Dim` rect 는 읽지 않음) | ✗ M2~M5, ✗ M7 (WorldMap 과 동일, V3 후 인셋 띠 2 침범) | V3 + P1 |
| 13 | `TowerPopup` | Village | 팝업 | 캔버스 직접 | 1136x640 좌상단 (72,−40) (`TowerPopup.prefab` 1478~1481행) | ✗ M2~M5, ✗ M7 (V3 후 인셋 띠 2 침범) | V3 + P1 |
| 14 | `DailyPopup` | Village | 팝업 | 캔버스 직접 | 1136x640 좌상단 (72,−40) (`DailyPopup.prefab` 984~987행) | ✗ M2~M5, ✗ M7 (V3 후 인셋 띠 2 침범) | V3 + P1 |
| 15 | `BattleHud` | Battle | HUD | `HudRoot`(Safe Area) | 최상위 자식: 상단 중앙 클러스터(`SpeedToggle` x −539..−483, `ExitButton` −475..−419, `TimerPlate` −403..−299, `BossHpBar` −277..+280, `ActiveCountdown` +301..+416; 위 y 13~102), 우상단 앵커 `BuildSynergyPanel` 165x247 (−13,−112), `BuildPanel` 165x239 (−13,−387), 하단 중앙 `SpawnerStatusPanel` 1006x64 (0,16) (`BattleHud.prefab` 12772~12837, 12889~12954행, rect 4796, 7037, 8202, 8455, 8732, 10855행) | OK M1~M7(인셋 예시 적용) 단 **조건부 ✗: 논리 폭 < 1362 이고 하단 인셋 > 14 이면** `BuildPanel`↔`SpawnerStatusPanel` 겹침 `b − 14` (b 하단 인셋). 예: 16:9 폰 하단 인셋 42 → 28 겹침. M6·M7 만 레터박스 위아래 띠(60px·192px)가 하단 인셋(30px·40px)보다 두꺼워 인셋을 흡수한다. M2~M5 는 박스가 세로를 다 채워 하단 인셋이 박스 안으로 들어오지만 `HudRoot`(`SafeAreaFitter`)가 이미 반영한다(§3.1.1 재검증: 오류 없음) | B1 |
| 16 | `ResultPopup` | Battle | 팝업 | `PopupRoot` | 패널 600x652 중앙 (0,−24), Dim 스트레치 + sizeDelta.x 800 (`ResultPopup.prefab` 46~49, 3907~3910행) | 캔버스 안에서는 OK(패널 위 58, 아래 10 여유, Dim 좌우 ±400 이 `PopupRoot`(1280) 밖 최대 200 을 덮음). **정정 ✗ M2~M5**: 패널 아래 = 360+24+326 = 710 이 하단 인셋 띠(678..720)를 32 침범(§2.6) | P1 |
| 17 | `CardSelectionPopup` | Battle | 팝업 | `PopupRoot` | 912x460 중앙 (0,−40), Dim sizeDelta.x 800 (`CardSelectionPopup.prefab` 13560~13563, 1610~1613행) | OK M1~M7 | 없음 |
| 18 | `BuildModalPopup` | Battle | 팝업 | `PopupRoot` | 684x524 중앙, Dim sizeDelta.x 800 (`BuildModalPopup.prefab` 3718~3721, 5744~5747행) | OK M1~M7 | 없음 |
| 19 | `SynergyModalPopup` | Battle | 팝업 | `PopupRoot` | 460x480 중앙, Dim sizeDelta.x 800 (`SynergyModalPopup.prefab` 1690~1694, 1543~1546행) | OK M1~M7 | 없음 |
| 20 | `SkillUnlockBanner` | Battle | 기타(컷인 배너) | `PopupRoot` | `Band` 가로 스트레치(앵커 x 0..1, 높이 84, 가운데 y) (`SkillUnlockBanner.prefab` 670~673행). 밴드 폭 = 루트 폭 = `PopupRoot` 1280. 오프스크린 거리는 캔버스 폭으로 계산(`SkillUnlockBannerView.cs` 39~53행) | **확인 못 함**: 와이드(M2~M5, 캔버스 1560~1680)에서 밴드가 `PopupRoot` 폭(1280)이라 좌우 140~200 이 비는지 실캡처로 확인하지 않음 | B2 (조건부) |
| 21 | `ConfirmPopup` | Village + Battle (`VillageController.cs` 606행, `BattleController.cs` 422행) | 팝업(공용) | 마을 `PopupRoot`(제안) / 배틀 `PopupRoot` | 패널 560x320 중앙(프레임 604x364), Dim 스트레치 + sizeDelta 4800x2800 (`ConfirmPopup.prefab` 869~873, 1758~1762, 2233~2236행) | OK M1~M7 (Dim 오버행 가로 ±2400·세로 ±1400) | 없음 |
| 22 | `ToastView` | Village + Battle (`ToastView.Show` 양쪽 사용) | 토스트(공용) | 마을 `PopupRoot`(제안) / 배틀 `PopupRoot` | `Panel` 하단 중앙, 위치 (0,120), 560x72 (`ToastView.prefab` 345~348행) → 화면 아래 120..192 | OK M1~M7 (마을 `HeroButton` 등 하단 버튼 윗단 88 보다 32 위). 하단 인셋 최대 예시 42 < 120 | 없음 |
| 23 | `BuildSynergyPanel` | Battle (`BattleHud` 하위 프리팹 인스턴스) | HUD 부품 | `BattleHud` 자식 | 우상단 165x247 (−13,−112) (`BattleHud.prefab` 12889~12954행) | OK 단독. `BuildPanel` 과 상하 간격 −112−247=359 → 387 사이 28 | B1(위치 보정 대상) |
| 24 | `SpawnerStatusPanel` | Battle (`BattleHud` 하위) | HUD 부품 | `BattleHud` 자식 | 하단 중앙 1006x64 (0,16) (`BattleHud.prefab` 12772~12837행) | 단독 OK. 15행 조건부 겹침의 한쪽 | B1 |
| 25 | `HpBar` | Battle? | 기타 | `EUI` 밖 | `Canvas` 컴포넌트 없음(`HpBar.prefab`에서 `m_RenderMode` 미검출). 부착처(월드/HUD) | **확인 못 함** | 게이트 V4 에서 부착처 확인 |

### 2.4 셀·행 프리팹 18개 (18행, 풀링 스크롤 안 재사용 단위)

셀은 부모 패널의 스크롤뷰 안에서 **고정 크기 레이아웃**으로 쓰이므로 비율 의존이 없다는 구조적 판단이지만, 각 셀의 개별 크기·넘침은 읽지 않았다. 전부 **확인 못 함(내부 크기 미확인)** 이며 판정은 부모 행을 따른다.

| # | 셀 프리팹 | 부모 | 씬 |
|---|---|---|---|
| 26 | `BuildIconCell` | `BattleHud` 빌드 패널 | Battle |
| 27 | `BuildModalCardCell` | `BuildModalPopup` | Battle |
| 28 | `BuildSynergyCell` | `BuildSynergyPanel` | Battle |
| 29 | `SpawnerStatusCell` | `SpawnerStatusPanel` | Battle |
| 30 | `SynergyModalCell` | `SynergyModalPopup` | Battle |
| 31 | `CodexCell` | `CodexPopup` | Village |
| 32 | `HeroSelectCell` | `HeroSelectPopup` | Village(레거시) |
| 33 | `LordRewardCell` | `LordLevelPopup` | Village |
| 34 | `QuestCell` | `QuestPopup` | Village |
| 35 | `RankingCell` | `RankingPopup` | Village |
| 36 | `RecordsStageCell` | `RecordsPopup` | Village |
| 37 | `RelicCell` | `RelicPopup` | Village |
| 38 | `ShopItemCell` | `ShopPopup` | Village |
| 39 | `WorldMapChapterTab` | `WorldMapPopup` | Village |
| 40 | `WorldMapNode` | `WorldMapPopup` | Village |
| 41 | `FormationSpeciesRow` | `FormationPopup` | Village |
| 42 | `FormationNestTile` | `FormationPopup` | Village |
| 43 | `TowerFloorCell` | `TowerPopup` | Village |

### 2.5 로딩 씬 직접 배치 UI (1행, 요소 6개)

| # | 묶음 | 씬 | 구분 | 현재 | 판정 | 보정 |
|---|---|---|---|---|---|---|
| 44 | `LoadingHud` 의 `Panel`(전체 스트레치) 아래 6요소 | Loading | HUD | `Canvas` 직접(`CHMUI` 미사용) | 아래 표 | C1 + C2 |

| 요소 | 앵커 | 사각형 (근거 `Loading.unity`) | 수직 구간(위 기준) |
|---|---|---|---|
| `TitleSmall` | 상단 중앙 | 1200x48, y −32 (15525~15528행) | 32..80 |
| `TitleMain` | 상단 중앙 | 1600x150, y −72 (6824~6827행) | 72..222 |
| `Tagline` | 상단 중앙 | 1200x48, y −224 (779~782행) | 224..272 |
| `DescText` | 하단 중앙 | 1600x56, 아래 104 (8254~8257행) | 아래 104..160 |
| `ProgressBarBg` (+ `ProgressFill` 720x28) | 하단 중앙 | 736x44, 아래 36 (9442~9445, 6261~6264행) | 아래 36..80 |
| `PercentText` | 하단 중앙 | 200x44, 중앙에서 오른쪽 +392, 아래 40 (329~332행) | 아래 40..84 |

- 겹침: `TitleSmall`(80) → `TitleMain`(72) 은 의도된 겹침 구간 8 이 아니라 값상 `TitleSmall` 아래 80 > `TitleMain` 위 72 로 8 겹친다 — 기존 연출 값이며 비율과 무관(본 기획 범위 밖, 비율에 따라 바뀌지 않음). `TitleMain` 아래 222 < `Tagline` 위 224 (2 여유). 상단 묶음(끝 272)과 하단 묶음(높이 기준 160)이 겹치지 않는 필요 높이 = 272 + 160 = 432 ≤ §1.7 의 최소 논리 높이 921 → 여유 489.
- 폭: 가장 넓은 `TitleMain`/`DescText` 1600 ≤ 논리 폭 최소 1637(H1, M5). `PercentText` 오른쪽 끝 = 960+392+200 = 1552 ≤ 1637 (Expand 후 1920 에서 368 여유).
- 판정(**정정**): 크기·폭 기준으로는 H1·H2 모두 통과하지만, **하단 인셋 기준으로는 ✗ M2~M5**(§1.8 표: `ProgressBarBg` 27, `PercentText` 23 덮임, `DescText` 0). 초판의 "OK M1~M7" 은 박스 밖 띠가 인셋을 흡수한다는 잘못된 전제였다. 보정 C1(스케일러 `Expand` 로 1920x1080 고정) + **C2(`Panel` 에 `SafeAreaFitter`)**.

### 2.6 정정 — 팝업 패널과 하단 인셋 띠 (초판 D3 "팝업은 인셋을 침범하지 않는다" 정정)

초판은 팝업 패널이 캔버스 중앙에 놓이고 가로 여백이 인셋보다 커서 안전하다고 보았다. **세로는 확인하지 않았다.** 하단 인셋 띠(M2~M5: 아래 42 논리, 캔버스 높이 720)는 y 678..720 이다. 중앙(캔버스 기준) 패널의 아래 끝 = 360 + dy + 높이/2.

| 패널 | 높이 / dy | 아래 끝 | 하단 인셋 띠(678) 침범 | 판정 M2~M5 |
|---|---|---|---|---|
| Relic 본체 `ModalBody` | 640 / 0 | 현행 680 → P1 후 659 | 현행 2 → P1 후 **0** | 현행 ✗(2), P1 후 OK |
| Relic 장식 `DecoModalGlow` | 684 / 0 | 현행 702 → P1 후 681 | 현행 24 → P1 후 **3** (장식) | scale 0.97 로 670.7 해소 |
| Result (배틀) | 652 / +24 | 710 | **32** | ✗ |
| WorldMap·Daily·Formation·Tower (V3 후) | 640 / 0 | 680 | **2** | ✗ |
| Records | 604 / 0 | 662 | 0 | OK |
| CardSelection (배틀) | 460 / +40 | 630 | 0 | OK |
| Shop·Codex·Quest·Cloud·LordLevel·Ranking | 524 / 0 | 622 | 0 | OK |
| BuildModal | 524 / 0 | 622 | 0 | OK |
| Confirm | 364 / 0 | 542 | 0 | OK |
| Synergy | 480 / 0 | 600 | 0 | OK |

M1(인셋 0)·M6·M7(마을 캔버스가 길어 중앙이 아래로 이동하나 인셋이 20·25 로 작음: M6 Relic 본체 아래 = 400 + 320 = 720 ≤ 780, M7 480 + 320 = 800 ≤ 935; 글로우도 742·822 로 안쪽)은 OK. 배틀 M6·M7 은 띠가 흡수. 따라서 침범은 **캔버스 높이 720 이고 하단 인셋 42 인 M2~M5(및 16:9 + 하단 인셋 조건 사례)** 에서만 생긴다. 침범하는 것은 패널 프레임이며, 그 안의 버튼이 띠에 있는지는 읽지 않았다(확인 못 함) — 프레임이 걸치는 것 자체를 겹침으로 판정한다. 보정 P1.

---

## 3. 결정

### 3.1 지원 해상도 매트릭스 (전 씬 공통)

규칙: **마을은 레터박스 없음**(긴 화면은 가로, 짧은 화면은 세로로 확장). **배틀·로딩은 기존 레터박스 규칙 불변**(`LetterboxFramer`: 배틀 16:9~7:3 풀스크린, 그 밖 띠. 로딩은 항상 16:9 박스 + 띠).

| # | 대표 기기 | 해상도(px) | 비율 | 마을 논리(Expand) | 배틀 캔버스(박스, Expand) | 로딩 캔버스(Expand) | 예시 인셋(px) → 논리 |
|---|---|---|---|---|---|---|---|
| M1 | 일반 16:9 | 1920x1080 | 1.7778 | 1280x720 | 1280x720 | 1920x1080 | 0 |
| M2 | **S25+** | 2340x1080 | 2.1667 | 1560x720 | 1560x720 | 1920x1080 | 좌 132, 아래 63 → 좌 88, 아래 42 (÷1.5) |
| M3 | 20:9 | 2400x1080 | 2.2222 | 1600x720 | 1600x720 | 1920x1080 | 동일 |
| M4 | 21:9 | 2520x1080 | 2.3333 | 1680x720 | 1680x720 | 1920x1080 | 동일 |
| M5 | 22:9 초와이드 | 2640x1080 | 2.4444 | 1760x720 | 1680x720(좌우 띠 60px씩) | 1920x1080 | 동일 |
| M6 | 16:10 태블릿 | 1920x1200 | 1.6 | 1280x800 | 1280x720(위아래 띠 60px) | 1920x1080 | 마을 아래 30 → 20 (배틀·로딩은 띠 60px ≥ 30px 가 흡수) |
| M7 | 4:3 태블릿 | 2048x1536 | 1.3333 | 1280x960 | 1280x720(위아래 띠 192px) | 1920x1080 | 마을 아래 40 → 25 (배틀·로딩은 띠 192px ≥ 40px 가 흡수) |

- 산식: 마을 scale = min(W/1280, H/720), 논리 = W/scale x H/scale. 배틀 박스 = `ComputeViewportRect`(16:9~7:3 이내는 풀 rect, 이외는 가까운 경계 종횡비 박스), Expand 로 박스 안 논리 = 박스px ÷ min(W/1280, H/720). M5 박스 폭 = 1080 x 7/3 = 2520px → 2520/1.5 = 1680.
- 세로 모드는 지원 대상 아님.

### 3.1.1 인셋이 띠에 흡수되는가 — 씬·비율별 재검증 (2차 개정)

규칙: 띠가 인셋을 흡수하는 조건 = 띠 두께(px) ≥ 인셋(px). 아니면 (인셋px − 띠px) ÷ scale 만큼 박스 안으로 들어온다. 예시 인셋은 §3.1 과 같다(M2~M5 좌 132·아래 63, M6 아래 30, M7 아래 40).

| 비율 | 띠 (배틀·로딩 박스 기준) | 마을: 박스 안 인셋(논리) | 배틀: 박스 안 인셋(논리) | 로딩: 박스 안 인셋(논리) | 재검증 결과 |
|---|---|---|---|---|---|
| M1 | 없음 | 0 | 0 | 0 | OK |
| M2 | 좌우 210 | 좌 88, 아래 42 (띠 없음) | 좌 0 (210 ≥ 132 흡수), 아래 42 | 좌 0, **아래 63** | 마을·배틀: `HudRoot` 반영 OK. **로딩: ✗ 정정(C2)** |
| M3 | 좌우 240 | 좌 88, 아래 42 | 좌 0, 아래 42 | 좌 0, **아래 63** | 위와 동일 |
| M4 | 좌우 300 | 좌 88, 아래 42 | 좌 0, 아래 42 | 좌 0, **아래 63** | 위와 동일 |
| M5 | 좌우 360 (배틀은 7:3 박스라 60) | 좌 88, 아래 42 | 좌 (132 − 60) ÷ 1.5 = 48, 아래 42 | 좌 0, **아래 63** | 배틀: `HudRoot` 가 48 반영 OK. 로딩 ✗ |
| M6 | 위아래 60 | 아래 20 (띠 없음) | 0 (60 ≥ 30 흡수) | 0 (흡수) | 마을 `HudRoot` OK, 배틀·로딩 흡수 OK |
| M7 | 위아래 192 | 아래 25 | 0 (192 ≥ 40 흡수) | 0 (흡수) | 같음 |

재점검 결과 요약:
- 흡수로 처리한 곳 4군데를 모두 다시 계산했다: 로딩(오류, 정정), 배틀 `BattleHud` M6·M7(맞음), 배틀 `HudRoot` M2~M5(`SafeAreaFitter` 가 이미 반영, 흡수 가정 아님), 마을(띠 없음, `HudRoot` 신규).
- 같은 종류의 오류가 하나 더 있었다: **팝업**(§2.6). 팝업은 `HudRoot`/`SafeAreaFitter` 를 거치지 않아 하단 인셋 띠를 그대로 맞는다.

### 3.2 지원 하한

| 구분 | 하한(논리) | 근거 |
|---|---|---|
| 마을 `HudRoot` (인셋 제외 후) | 폭 ≥ 1160, 높이 ≥ 660 | §5.2 |
| 마을·배틀 `PopupRoot`(Safe Area 안, P1 후) 패널 | 패널 높이 + 2·abs(dy) ≤ 안전영역 높이 (하단 인셋 42 → 678), 폭 ≤ 안전영역 폭 | §6 |
| 배틀 `HudRoot` | 폭 ≥ 1280, 높이 ≥ 720 − 46 = 674 (보정 B1 후) | §4 B1 |
| 로딩 `Panel`(C2 후) | 논리 1920x1080 − 인셋. 상·하 합계 인셋 ≤ 489 (필요 높이 432 = 272 + 160) | §1.8, §4 C2 |

### 3.3 결정 1 — CanvasScaler 정책: 전 씬 `Expand`

| 안 | 내용 | 결과 | 판정 |
|---|---|---|---|
| **A. Expand (권장)** | `m_ScreenMatchMode` = Expand(1) | 논리 크기 항상 ≥ 기준(마을·배틀 1280x720, 로딩 1920x1080). 디스플레이 기준/카메라 박스 기준 해석과 무관하게 같은 값 | 채택 |
| B. Match Height (match=1) | 논리 높이 고정 | 4:3 마을 논리 960x720 → 폭 부족 | 폐기 |
| C. Match Width (match=0) | 논리 폭 고정 | 19.5:9 논리 높이 590.8 → 겹침 | 폐기 |
| D. 비율 구간별 기준 분기 | 구간별 기준값 | 경계 튜닝·프리팹 이원화 | 폐기 |

확정 값:

| 대상 | 변경 |
|---|---|
| `Village.unity` `UICanvas` `CanvasScaler` | `m_ScreenMatchMode` 0 → **1** (기준 1280x720·Overlay 유지) |
| `Loading.unity` `Canvas` `CanvasScaler` | `m_ScreenMatchMode` 0 → **1** (기준 1920x1080 유지, `m_MatchWidthOrHeight` 0.5 유지·미사용) |
| `Battle.unity` | 불변(이미 Expand) |

### 3.4 결정 2 — 마을 루트 분리와 안전영역

마을 `UICanvas` 아래 정적 RectTransform 2개(런타임 생성 금지):

| 이름 | 앵커/크기 | 컴포넌트 | 역할 |
|---|---|---|---|
| `HudRoot` | (0,0)~(1,1), offset 0 | 기존 `SafeAreaFitter` (`_gameCamera` = 마을 `Main Camera`, `_target` = 자신) | `VillageHud` 전용. Safe Area 반영 |
| `PopupRoot` | (0,0)~(1,1), offset 0 | 기존 `SafeAreaFitter` (`_gameCamera` = 마을 `Main Camera`, `_target` = 자신) — **2차 개정: Safe Area 적용** | 마을 모든 팝업·토스트·확인창. 패널은 안전영역 중앙에 놓인다 |

형제 순서 `HudRoot` → `PopupRoot`.

**2차 개정 (D3 폐기, P1)**: 1차 개정은 `PopupRoot` 를 Safe Area 미적용 전체 스트레치로 두고(`Dim` 수정 0건) 팝업이 인셋을 침범하지 않는다고 보았으나 §2.6 에서 틀림이 확인됐다(Relic 본체 2·장식 글로우 24, Result 32, 640 높이 패널 2 침범). 대안:

| 안 | 내용 | 코드 | 프리팹 수정 | 판정 |
|---|---|---|---|---|
| **P1-A. `PopupRoot` 에 `SafeAreaFitter` + `Dim` 오버행 + 패널 2개 값 보정 (권장)** | 패널이 안전영역 중앙에 놓임. `Dim` 은 세로·가로로 넘치게 늘려 인셋 띠·구멍도 덮음 | 신규 0 (기존 컴포넌트 재사용) | `Dim` 16개 sizeDelta, `RelicPopup` 장식 글로우 `DecoModalGlow` scale 0.97, `ResultPopup` 패널 dy 보정 | 채택 |
| P1-B. 큰 패널에 자동 맞춤(scale-to-fit) 컴포넌트 | 안전영역에 맞춰 패널을 줄임 | 신규 컴포넌트 필요 | 패널 17개에 부착 | 폐기(재사용 가능한 정적 값으로 충분) |
| P1-C. 프레임 걸침을 허용하고 버튼만 안전영역 안에 두기 | 검사 대상이 버튼으로 축소 | 없음 | 버튼 위치 전수 확인 필요(미확인) | 폐기(사용자 기준은 인셋 띠 위 겹침 0) |

P1-A 상세 값:
- `PopupRoot` 앵커가 안전영역이 되므로 마을 `Dim`(현행 스트레치 offset 0)은 안전영역에서 끊긴다 → 팝업 `Dim` 의 `m_SizeDelta` 를 (800, 400) 으로 한다(가로 ±400, 세로 ±200 오버행. 필요한 오버행은 예시 최대 인셋 가로 88·세로 42 이므로 충분).
- `Dim` 대상 16개: 마을 12(Shop·Quest·Codex·Records·LordLevel·Ranking·Cloud·Relic·WorldMap·Formation·Tower·Daily), 배틀 4(Result·CardSelection·BuildModal·SynergyModal; 현행 (800,0) → (800,400)). `ConfirmPopup` 은 이미 (4800,2800), `HeroSelectPopup` 은 도달 불가라 제외.
- `RelicPopup` **장식 글로우 `DecoModalGlow`**(정정: 패널이 아니라 글로우): `m_LocalScale` (1,1,1) → **(0.97,0.97,1)**. 684 × 0.97 = 663.5, 아래 끝 339 + 331.7 = 670.7 ≤ 678(보정 전 681, 3 침범 해소), 폭 1180 × 0.97 = 1144.6. 본체 `ModalBody` 1136x640 scale 1 은 보정 없음(아래 끝 339 + 320 = 659 ≤ 678).
- `ResultPopup` 패널: `m_AnchoredPosition.y` −24 → **−10**. 아래 끝 = 339 + 10 + 326 = 675 ≤ 678, 위 끝 = 339 − 10 − 326 = 3 ≥ 0 (안전영역 높이 678 기준, 하단 인셋 42).
- 640 높이 패널(WorldMap·Daily·Formation·Tower): 안전영역 중앙 339 + 320 = 659 ≤ 678 → 보정 불필요.
- 인셋 0(M1)에서는 결과가 현행과 같다(Relic 은 장식 글로우만 0.97 로 작아짐, 본체 불변; Result 는 14 위로 이동).
- **배틀 `PopupRoot`**: 현행 중앙 고정 1280x720(`Battle.unity` 6341~6344행)을 앵커 (0,0)~(1,1)·offset 0 으로 바꾸고 `SafeAreaFitter`(`_gameCamera` = 배틀 `Main Camera`) 를 붙인다. 안전영역 폭은 M1 에서 1280 이상이라 패널 배치가 달라지지 않는다.

`Dim` 이 위 오버행 수치 때문에 인셋이 300 논리 이상인 기기에서는 모자랄 수 있다(예시 최대 88·42 의 3배 이상 여유라 지원 범위 안에서는 충분).

`SetRoot` 순서(`VillageController.Start`): ① 맨 앞 `SetRoot(_popupRoot)` (HUD 이전 `ToastView.Show`(103행)용) ② `ShowUIAsync(EUI.VillageHud ...)`(77행) 직전 `SetRoot(_hudRoot)` ③ 직후 `SetRoot(_popupRoot)`. 필드 `[SerializeField] RectTransform _hudRoot, _popupRoot`, 둘 다 null 이면 기존 동작. `CHMUI.OnSceneLoaded` 가 루트를 리셋하므로 진입마다 호출.

`SafeAreaFitter` 요건(코드 구조는 구현자 판단): `_gameCamera` 가 null 이면 갱신을 건너뛰는 기존 동작이므로 마을 `Main Camera` 연결 필수. 마을 `Main Camera.rect` 가 (0,0,1,1) 이 아니면 V0 에서 보고.

### 3.5 공용 UI 가 씬별로 어느 루트에 뜨는가

| UI | 마을 (보정 후) | 배틀 (현행) | 로딩 |
|---|---|---|---|
| `VillageHud` | `HudRoot`(Safe Area) | — | — |
| `BattleHud` | — | `HudRoot`(Safe Area) | — |
| 팝업 전부(마을 13 / 배틀 4) | `PopupRoot`(Safe Area, 중앙 패널, `Dim` ±400/±200) | `PopupRoot`(P1 후 Safe Area 스트레치, `Dim` ±400/±200; 현행은 1280x720 중앙) | — |
| `ConfirmPopup` | `PopupRoot` (`Dim` 4800x2800 이 어느 비율도 덮음) | `PopupRoot` (`BattleController.cs` 422행) | 사용 안 함 |
| `ToastView` | `PopupRoot` (하단 중앙, 아래 120) | `PopupRoot` (`ToastView.Show` 다수) | 사용 안 함 |
| `SkillUnlockBanner` | — | `PopupRoot` (`BattleController.cs` 483행) | — |
| 로딩 `Panel` | — | — | `Canvas` 직접 (`CHMUI` 미사용) |

---

## 4. 보정 목록 (문제 항목별 구체안)

| ID | 대상 (인벤토리 #) | 문제 | 보정안 (값) | 근거 |
|---|---|---|---|---|
| **V1** | Village `UICanvas` (S1), 팝업 11 (`RelicPopup`) 외 마을 전 팝업 | Match 0.5 → 논리 크기 변동 | 스케일러 Expand + 루트 2개 + `SetRoot` 3지점(§3.3~3.4). 프리팹 수정 0 | §1.3 |
| **V2** | `VillageHud` (#1) | 가로 겹침(`TowerButton`↔`StagePanel`), Safe Area 미적용 | `TowerButton` `m_AnchoredPosition` x +228 → **−228** (중앙 기준 −278..−178, `HeroButton` −170..−70 과 8 간격). 열 순서: 탑 · 지도 · 방어하기. 지원 하한 폭 1268 → 1160 | §5 |
| **V3** | `WorldMapPopup` (#10), `DailyPopup` (#14), `FormationPopup` (#12), `TowerPopup` (#13) | 패널 좌상단 고정 | `Panel` 앵커 중앙화: `m_AnchorMin`/`m_AnchorMax`/`m_Pivot` (0,1)→**(0.5,0.5)**, `m_AnchoredPosition` (72,−40)→**(0,0)**, `m_SizeDelta` (1136,640) 불변. 1280x720 에서 결과 동일(중심 (72+568, 40+320) = (640, 360)) | §1.6 |
| **B1** | `BattleHud` (#15) 의 `BuildSynergyPanel` (#23)·`BuildPanel` | 하단 인셋 b > 14 이고 논리 폭 < 1362 이면 `BuildPanel`(아래 626)↔`SpawnerStatusPanel`(위 H−80) 겹침 b−14 | 두 우측 패널을 위로 32 이동: `BuildSynergyPanel` 인스턴스 `m_AnchoredPosition.y` −112 → **−80** (`BattleHud.prefab` 12954행), `BuildPanel` `m_AnchoredPosition.y` −387 → **−355** (8736행). 새 아래 끝 594, 허용 b ≤ 46 (예시 42 통과, 여유 4). 상단 중앙 클러스터 오른쪽 끝 +416 → 우측 패널 왼쪽 끝 (W−178): W=1280 에서 1102 > 936, 겹침 없음 | §2.3 #15 산식: 겹침 = 626 − (720 − b − 80) = b − 14, 보정 후 594 − (640 − b) = b − 46 |
| **B2** | `SkillUnlockBanner` (#20) | 밴드 폭이 `PopupRoot` 폭(1280)이라 와이드에서 좌우가 빌 수 있음(미확인) | **V4 캡처로 먼저 확인.** 비어 보이면 `Band` `m_SizeDelta` (0,84) → **(800,84)** (Dim 과 같은 ±400 오버행, 최대 필요 ±200) | §2.3 #20 |
| **C1** | Loading `Canvas` (S3), `LoadingHud` (#44) | 스케일러 Match 0.5, 해석에 따라 논리 크기 1637~1920 | `CanvasScaler` Expand (§3.3). 요소 값 불변 | §1.7 |
| **C2** (신규, 정정) | Loading `Panel` (#44, S3) | M1~M5·조건 사례에서 하단 인셋 63 이 `ProgressBarBg` 27·`PercentText` 23 를 덮음 | `Panel` 에 기존 `SafeAreaFitter` 부착(`_gameCamera` = 로딩 `Main Camera`, `_target` = `Panel` 자신). `Panel` 앵커가 (인셋/박스) 로 줄어들어 하단 요소가 63 위로 올라감 → 새 `ProgressBarBg` 위치 99..143(아래 기준), `PercentText` 103..147, `DescText` 167..223, 모두 인셋 63 위. 상단 인셋도 반영. 배경·월드 미러·`LetterboxBackground` 불변. 요소 값 불변 | §1.8 |
| **P1** (신규, 정정) | 팝업 마을 12 + 배틀 4 + 마을·배틀 `PopupRoot` | Result 32, 640 높이 패널(Relic 본체 포함) 2, Relic 장식 글로우 24(P1 후 3)가 하단 인셋 띠 침범 | §3.4 P1-A: `PopupRoot` 에 `SafeAreaFitter`(배틀 `PopupRoot` 는 1280x720 중앙 → 스트레치로 변경), 팝업 `Dim` 16개 `m_SizeDelta` (800,400), `RelicPopup` `DecoModalGlow` scale 0.97, `ResultPopup` 패널 y −24 → −10 | §2.6 |

**보정 없음으로 확정한 항목**: 마을 팝업 중 Shop·Quest·Codex·Records·LordLevel·Ranking·Cloud(캔버스·인셋 모두 OK, `Dim` 오버행 값만 P1 에서 변경)·`HeroSelectPopup`(레거시, 도달 불가)·배틀 팝업 CardSelection·BuildModal·SynergyModal(패널 값 OK, `Dim` 값만 P1)·`ConfirmPopup`·`ToastView`·`BuildSynergyPanel`/`SpawnerStatusPanel` 단독·셀 18종(부모 따름).

**집계 (2차 개정 후)**: 전체 47행.
- **현행 ✗ 확정 9행** (1차 개정 7행 → 9): S1, #1, #10, #11, #12, #13, #14 (1차 개정과 동일 7행) + **S3 로딩 캔버스**(취약 → 확정, 이번 정정) + **#16 `ResultPopup`**(인셋 띠 침범 32, 같은 종류 오류 재점검에서 새로 발견).
- 조건부 ✗ 1행: #15 `BattleHud`.
- 취약 0행 (S3 가 확정으로 이동).
- 확인 못 함 2행: #20 `SkillUnlockBanner`, #25 `HpBar`.
- 셀 18행: 내부 크기 미확인(부모 따름).
- 나머지 OK. 단 #11·#10·#12·#13·#14 는 V1/V3 후에도 인셋 띠 침범이 남아 P1 이 필요하다(이미 ✗ 확정 행에 포함, 행 수 변화 없음).

---

## 5. 결정 3 — 마을 HUD 영역 분할 규칙

### 5.1 규칙

1. 모든 HUD 요소는 **크기 고정 + 가장자리/모서리 앵커.** 비율이 바뀌면 요소가 커지거나 줄지 않고 **요소 사이 빈 공간만 늘어난다.**
2. 겹침 판정은 상호작용·정보 사각형(버튼, 패널)이다. `Deco*`(글로우, 예: `DecoSortieGlow` 296x136)는 제외.

| 존 | 구성 | 앵커 | 가로 확장(M2~M5) | 세로 확장(M6~M7) |
|---|---|---|---|---|
| Z1 상단 | `TopBar` (높이 88) | 상단 스트레치 | 폭만 늘어남 | 불변 |
| Z2 좌측 | 메뉴 8칸 | 좌상단 | 불변(인셋만큼 오른쪽 이동) | 아래 빈 공간 증가 |
| Z3 우상단 | `RecordPanel` | 우상단 | 불변 | 불변 |
| Z4 우하단 | `StagePanel` | 우하단 | 불변 | 아래에 붙은 채 위 빈 공간 증가 |
| Z5 하단 중앙 | `TowerButton` `HeroButton` `SortieButton` | 하단 중앙 | 중앙 유지 | 아래에 붙은 채 유지 |
| Z6 정중앙 | `StagePrevButton` `StageNextButton`, 월드 영웅 | 정중앙 | 중앙 유지 | 세로 중앙 이동 |

### 5.2 최소 영역(HudRoot 논리 W x H)에서 겹침 여유

| 쌍 | 변경 후 여유 | 하한(W 1160, H 660) | 1280x720 |
|---|---|---|---|
| `RecordPanel` ↔ `StagePanel` | `H − 660` | 0 | 60 |
| `StageNextButton` ↔ `StagePanel` (H ≤ 748) | `W/2 − 580` | 0 | 60 |
| `SortieButton` ↔ `StagePanel` | `W/2 − 526` | 54 | 114 |
| `TowerButton`(이동 후) ↔ 좌측 메뉴 | 세로 `H − 688`, 세로 겹치면 가로 `W/2 − 534` | 가로 46 | 가로 106 |
| `TopBar`: `SoulPill` ↔ 마을 이름 | `W/2 − 540` | 40 | 100 |
| `StagePrevButton` ↔ 좌측 메뉴 | `W/2 − 480` | 100 | 160 |
| `RecordPanel` ↔ `TopBar` | 104 − 88 | 16 | 16 |

하한에서 간격 ≥ 0 (두 쌍이 0). M1~M7 의 `HudRoot` 최소는 1280x678 이라 모두 하한 위(폭 ≥ 1160, 높이 678 ≥ 660, 여유 18).

### 5.3 하단 버튼 열 재배치 — 대안

| 안 | 내용 | 효과 | 단점 |
|---|---|---|---|
| **A. `TowerButton` 을 `HeroButton` 왼쪽으로 (권장, V2)** | x +228 → −228 | 우측 존과의 간섭이 `SortieButton`(+170) 으로 감소, 하한 폭 1268 → 1160 | 버튼 순서 바뀜 |
| B. `StagePanel` 폭 340 → 300 | 간섭 여유 +40 | 카드 내부 17개 이상 재작업 | 작업량 큼 |
| C. 작은 화면에서 HUD 전체 축소 | `HudRoot` 스케일 | 스트레치 앵커 오프셋 불일치 | 폐기 |

---

## 6. 팝업 공통 규칙 (전 씬)

1. **패널은 중앙 앵커**: `anchorMin = anchorMax = pivot = (0.5, 0.5)`, 위치 (0,0), 크기 고정. 한 모서리 기준 앵커 금지.
2. **패널 상한**: 마을 팝업 본체 ≤ **1136x640** (현행 최대 WorldMap·Daily·Formation·Tower·Relic `ModalBody`; Relic 장식 글로우 1180x684 는 장식이라 제외). 배틀 팝업은 `PopupRoot` 1280x720 안(현행 최대 `ResultPopup` 600x652).
3. **`Dim`** (P1 후): 마을·배틀 모두 스트레치 + `sizeDelta == (800,400)`(가로 ±400, 세로 ±200 오버행)이라 `PopupRoot`(안전영역)보다 넓게 화면 끝·인셋 띠까지 덮는다. 공용 `ConfirmPopup` = 4800x2800.
3-1. **패널은 안전영역 안**: 패널 아래 끝 = 안전영역 중앙 + dy + 높이/2 ≤ 안전영역 아래(하단 인셋 42 에서 678). 침범하는 `ResultPopup` 패널과 `RelicPopup` 장식 글로우는 §3.4 값으로 보정(Relic 본체는 침범 0).
4. **넘침 정책**: 패널은 줄이지 않는다. 긴 목록은 `CHPoolingScrollView` 안에서 스크롤.
5. **하단 고정 버튼**: 패널 안 좌표 유지, 패널 아래 끝에서 20 위(`SelectButton` 아래 끝 620 / 패널 640).
6. **태블릿 확대 없음**: 4:3 에서 팝업·HUD 를 키우지 않는다(D2).

### 6.1 마을 패널 검산 (캔버스 최소 1280x720)

| 팝업 | 패널 | 가로 여유/변 | 세로 여유/변 |
|---|---|---|---|
| Shop·Codex·Quest·Cloud·LordLevel·Ranking | 764x524 | 258 | 98 |
| Records | 524x604 | 378 | 58 |
| Relic 본체 `ModalBody` | 1136x640 | 72 | 40 |
| Relic 장식 `DecoModalGlow` (장식) | 1180x684 → scale 0.97 후 1144.6x663.5 | 67.7 | 28.2 |
| WorldMap·Daily·Formation·Tower (V3 후) | 1136x640 | 72 | 40 |

### 6.2 침입 지도 구체 재배치 (최종, 모든 비율 공통)

| 안 | 내용 | 판정 |
|---|---|---|
| **A. 루트 수정만 (권장)** | Expand + `Panel` 앵커 중앙화(V3). 패널 내부 불변 | 채택 |
| B. 하단 영역 3개를 패널 하단 앵커로 | 패널 높이 가변 대비 | 패널 높이 640 고정이라 효과 0 → 폐기(YAGNI) |
| C. 패널 높이 가변 | 작은 화면에서 `MapArea` 축소 | 하한 밖 지원 불필요 → 폐기 |

패널 안 좌표: `MapArea` (232,−80) 520x472, `RulesColumn` (12,−256) 328x208, `BottomText` (24,−564) 480x56, `DifficultyBox` (512,−512) 240x108, `SelectButton` (768,−564) 352x56, `CloseButton` (1072,−8) 48x48. 1280x720 기준 화면 안 좌표: 패널 x 72..1208, y 40..680; `SelectButton` y 604..660; `DifficultyBox` y 552..660.

---

## 7. 배틀 씬 UI — 항목별 재검증

| 항목 | 기대(`battle-wide-screen-fit.md`) | 현행 확인 결과 | 판정 |
|---|---|---|---|
| 카메라 rect | 16:9~7:3 풀 rect | `_targetAspect` 1.7777778, `_maxAspect` 2.3333333 (`Battle.unity` 6513~6519행) | OK |
| 스케일러 | Expand | `m_ScreenMatchMode: 1` (6638행) | OK |
| `HudRoot` Safe Area | `SafeAreaFitter` | 존재(685행), `BattleController.cs` 245~248행 `SetRoot` | OK |
| `PopupRoot` 1280x720 중앙 | 앵커 (0.5,0.5), 크기 1280x720 | `Battle.unity` 6341~6344행 일치. 단 **Safe Area 미적용** — 팝업 패널이 하단 인셋 띠를 침범(`ResultPopup` 32, §2.6) | **정정 ✗ → P1** |
| `Dim` 오버행 | 가로 ±400 | Result·CardSelection·BuildModal·SynergyModal 은 sizeDelta (800,0)(각 prefab 3910, 1613, 5747, 1546행), Confirm 은 4800x2800. 가로만 있고 세로 오버행 없음 | 가로 OK, 세로 부족 → P1 `Dim` (800,400) |
| 인셋 흡수 가정 | 배틀 M6·M7: 띠 ≥ 인셋 | 60px ≥ 30px, 192px ≥ 40px → 흡수 맞음. M2~M5 는 `HudRoot` 가 반영, 흡수 가정 없음(§3.1.1) | OK |
| `BattleHud` 상단 중앙 클러스터·우측 패널·하단 중앙 | 모서리/중앙 앵커 | 전부 상단 중앙/우상단/하단 중앙 앵커(§2.3 #15) | OK |
| 하단 인셋 + 좁은 폭 | (battle 문서에 없음) | 겹침 `b − 14` | **부족 → B1** |
| `SkillUnlockBanner` 와이드 | (battle 문서에 없음) | 밴드 폭 = `PopupRoot` 폭, 실캡처 미확인 | **미확인 → B2** |
| 월드 레터박스 규칙 | — | 불변 | 건드리지 않음 |

---

## 8. 로딩 씬 UI — 점검 결과

§1.7·§1.8·§2.5 참조. 레터박스(16:9 박스, 띠에 월드 미러)는 불변. 보정은 **C1(Expand) + C2(`Panel` 에 `SafeAreaFitter`)** 두 개다.

**정정 기록**: 1차 개정은 "로딩이 16:9 박스 안에만 UI 를 그리므로 인셋 문제는 박스 밖 띠가 흡수한다" 고 썼다. 이는 좌우 띠(M2~M5 의 210~360px ≥ 132px)와 M6·M7 위아래 띠(60px ≥ 30px, 192px ≥ 40px)에서만 맞다. M1~M5 는 박스 높이가 화면 높이와 같아 하단 인셋 63 이 박스 안으로 들어오고, `ProgressBarBg` 27·`PercentText` 23 이 덮인다(§1.8). 7개 비율 수치와 코드 근거(`SafeAreaFitter` 재사용 가능성)는 §1.8 표.

C2 후 위치 (M2~M5, 하단 인셋 63): `Panel` 아래가 63 올라가므로 `ProgressBarBg` 아래 기준 99..143, `PercentText` 103..147, `DescText` 167..223. 모든 하단 요소가 인셋(63) 위. 상단 요소는 인셋이 0 이면 불변. 로딩 화면을 전체 폭으로 확장하는 것은 이번 범위 밖(D5).

---

## 9. 엣지 케이스

| 케이스 | 처리 |
|---|---|
| 16:9 폰 + 마을 가로 인셋 합 > 120 논리 | 지원 하한 밖 |
| 마을 하단 인셋 > 60 논리 | 지원 하한 밖 |
| 마을·배틀 팝업 (P1 후) | `PopupRoot` 가 안전영역이므로 가로·세로 인셋과 무관하게 패널이 안전영역 안. 단 안전영역 폭·높이가 패널보다 작으면 침범(마을 하한 높이 678 미만 또는 폭 1136 미만 = 가장 넓은 본체 폭) |
| 로딩 상단 인셋 > 32 | C2 의 `SafeAreaFitter` 가 `Panel` 을 줄여 `TitleSmall` 이 인셋 아래로 내려감 |
| 배틀 16:9 폰 + 하단 인셋 > 46 논리 | 지원 하한 밖(B1 후) |
| 회전·분할 화면 | `SafeAreaFitter` 가 변화 감지, Expand 가 하한 보장 |
| 마을 진입마다 | `SetRoot` 매번 호출 |
| 토스트가 HUD 보다 먼저 | `PopupRoot` 로 먼저 전환 |
| 25:9 이상 로딩 | Expand 후 1920x1080 고정이라 잘림 없음 |
| 신규 UI 프리팹 추가 | 인벤토리 테스트 T6 이 분류 누락을 실패로 잡는다 |
| 띠가 인셋을 흡수한다는 가정 | 금지. 흡수 여부는 항상 "띠px ≥ 인셋px" 으로 계산(§3.1.1), 테스트 T16 이 인셋 띠와 UI 사각형을 쌍으로 검사 |

---

## 10. 구현 요청사항 (gameplay-programmer 용)

### 10.1 Enum / Interface / 에셋 키 / 스키마

- Enum: 없음. Interface: 없음. 에셋 키: 신규 프리팹 없음(씬 정적 RectTransform `HudRoot`, `PopupRoot` 2개만). SO/JSON: 없음.

### 10.2 씬·프리팹 값 (일괄)

| 대상 | 필드 | 값 | 보정 |
|---|---|---|---|
| `Village.unity` `UICanvas` `CanvasScaler` | `m_ScreenMatchMode` | 0 → 1 | V1 |
| `Village.unity` `HudRoot`(신규) | anchor/offset, 컴포넌트 | (0,0)~(1,1)/0, `SafeAreaFitter`(`_gameCamera`=마을 Main Camera, `_target`=자신) | V1 |
| `Village.unity` `PopupRoot`(신규) | anchor/offset | (0,0)~(1,1)/0, 형제 순서 `HudRoot` 뒤 (Safe Area 는 아래 P1 행) | V1 |
| `VillageController` | `_hudRoot`, `_popupRoot` | 위 2개 연결 | V1 |
| `VillageHud.prefab` `TowerButton` | `m_AnchoredPosition` | (228,16) → (−228,16) | V2 |
| `WorldMapPopup` `DailyPopup` `FormationPopup` `TowerPopup` 의 `Panel` | anchorMin/Max/pivot/anchoredPosition | (0.5,0.5)/(0.5,0.5)/(0.5,0.5)/(0,0), sizeDelta (1136,640) 불변 | V3 |
| `BattleHud.prefab` `BuildSynergyPanel` 인스턴스 | `m_AnchoredPosition.y` | −112 → −80 | B1 (D6 승인 시) |
| `BattleHud.prefab` `BuildPanel` | `m_AnchoredPosition.y` | −387 → −355 | B1 (D6 승인 시) |
| `SkillUnlockBanner.prefab` `Band` | `m_SizeDelta` | (0,84) → (800,84) | B2 (V4 캡처로 필요 확인 후) |
| `Loading.unity` `Canvas` `CanvasScaler` | `m_ScreenMatchMode` | 0 → 1 | C1 |
| `Loading.unity` `Panel`(전체 스트레치 자식) | 컴포넌트 추가 | 기존 `SafeAreaFitter` (`_gameCamera` = 로딩 `Main Camera`, `_target` = `Panel` 자신) | C2 |
| `Village.unity` `PopupRoot` | 컴포넌트 추가 | 기존 `SafeAreaFitter` (`_gameCamera` = 마을 `Main Camera`, `_target` = 자신) | P1 |
| `Battle.unity` `PopupRoot` | anchor/size, 컴포넌트 | (0.5,0.5)/1280x720 → (0,0)~(1,1)/offset 0, `SafeAreaFitter`(`_gameCamera` = 배틀 `Main Camera`) | P1 |
| 팝업 `Dim` 16개 (마을 12: Shop·Quest·Codex·Records·LordLevel·Ranking·Cloud·Relic·WorldMap·Formation·Tower·Daily, 배틀 4: Result·CardSelection·BuildModal·SynergyModal) | `m_SizeDelta` | 마을 (0,0) / 배틀 (800,0) → **(800,400)** | P1 |
| `RelicPopup.prefab` 장식 글로우 `DecoModalGlow` (본체 `ModalBody` 는 불변) | `m_LocalScale` | (1,1,1) → (0.97,0.97,1) | P1 |
| `ResultPopup.prefab` 패널 | `m_AnchoredPosition.y` | −24 → −10 | P1 |

### 10.3 코드 연결점

| 파일 | 변경 |
|---|---|
| `Scripts/Village/VillageController.cs` | `[SerializeField]` 루트 2개 + `SetRoot` 3지점 (77행 전후, `Start` 최상단) |
| `Tests/EditMode/UI/ResolutionAdaptationTests.cs` (신규) | 11장 |
| `SafeAreaFitter.cs`, `LetterboxFramer.cs`, `CHMUI.cs`, `LoadingHud.cs`, `LoadingController.cs`, `BattleController.cs` | 변경 없음 (`BattleController` 는 `_hudRoot`/`_popupRoot` 참조만 쓰므로 `PopupRoot` 모양이 바뀌어도 코드 불변) |

프리팹 수정은 일회용 에디터 스크립트 또는 수동 편집(Rule 04 §3: 스크립트는 사용 후 삭제).

---

## 11. 검증 게이트

### 11.1 EditMode 테스트 (`ResolutionAdaptationTests`, 한글 메서드명)

| ID | 항목 | 방법 | 통과 기준 |
|---|---|---|---|
| T1 | 스케일러 3씬 | 3개 `.unity` 의 캔버스 `CanvasScaler` 읽기 | 마을·배틀·로딩 모두 `screenMatchMode == Expand`; 마을·배틀 기준 (1280,720), 로딩 (1920,1080) |
| T2 | 논리 크기 | M1~M7 에서 scale = min(W/기준W, H/기준H) 계산 | 마을 논리 ≥ 1280x720, 로딩 = 1920x1080 (±0.1), §3.1 표 일치 |
| T3 | `HudRoot` 영역 | 인셋 적용(`SafeAreaFitter.ComputeAnchors`) 후 논리 크기 | 마을 M2~M7 폭 ≥ 1160 / 높이 ≥ 660 |
| T4 | 마을 HUD 겹침 | `VillageHud.prefab` 을 `HudRoot` 논리 크기 부모 아래 인스턴스화, §5.1 존 사각형 월드 코너 계산, `Deco*` 제외 | M1~M7 + 하한(1160x660) 에서 모든 쌍 교차 면적 0 |
| T5 | 하단 버튼 순서 | `TowerButton` x < `HeroButton` x < `SortieButton` x | V2 반영 |
| T6 | **인벤토리 순회** | `AssetDatabase.FindAssets("t:Prefab", "Assets/_Lair/Art/UI")` 로 모든 프리팹을 열거해 §2 의 분류 목록(EUI 22 + 보조 3 + 셀 18 = 43)과 대조 | 목록에 없는 프리팹 0, 목록에만 있는 프리팹 0 (신규 UI 추가 시 실패) |
| T7 | **팝업 앵커 순회** | EUI 팝업 프리팹(마을 13: Shop·Quest·Codex·Records·HeroSelect·LordLevel·Ranking·Cloud·WorldMap·Relic·Formation·Tower·Daily, 배틀 4: Result·CardSelection·BuildModal·SynergyModal, 공용 Confirm)의 루트 직계 자식 중 스트레치가 아닌 것 | 전부 `anchorMin == anchorMax == pivot == (0.5,0.5)` (단, `ResultPopup` y −10(P1 후)·`CardSelectionPopup` y −40 오프셋 허용) |
| T8 | **팝업 패널 크기 순회** | 위 프리팹의 non-stretch 자식 `sizeDelta` | 마을 팝업 본체 ≤ 1136x640 (`DecoModalGlow` 등 `Deco*` 장식 제외), 배틀·공용 ≤ 1280x720 |
| T9 | **Dim 순회** | 위 프리팹의 `Dim`(`_dimButton` 또는 `_backgroundButton` 연결 오브젝트) rect | 마을 12 + 배틀 4: anchor (0,0)~(1,1), `sizeDelta == (800,400)` (P1). `ConfirmPopup`: sizeDelta ≥ (4800,2800). `HeroSelectPopup` 제외(도달 불가) |
| T10 | **패널 안전 순회** | 위 패널을 M1~M7 + 조건 사례의 `PopupRoot`(= 안전영역: 캔버스 − 인셋) 중앙에 배치(`m_AnchoredPosition`·`m_LocalScale` 반영) | 패널 ⊂ 안전영역. 특히 M2~M5 하단 인셋 42: `RelicPopup` 본체 `ModalBody` 아래 끝 = 659 ≤ 678 (보정 없음), `DecoModalGlow` 는 scale 0.97 반영 후 670.7 ≤ 678 (별도 단언), `ResultPopup` 아래 끝 = 675 ≤ 678 |
| T11 | 배틀 HUD 겹침 | `BattleHud.prefab` 을 `HudRoot` 논리 크기로 배치: 상단 중앙 클러스터·우측 패널 2개·`SpawnerStatusPanel` | M1~M7 + 조건 사례(W 1280, 하단 인셋 42) 에서 교차 면적 0 (B1 반영 후) |
| T12 | 루트 배선 | `Village.unity` 의 `HudRoot`(`SafeAreaFitter` 포함)·`PopupRoot`, `VillageController` 필드 연결 | null 0, `HudRoot` 가 `PopupRoot` 앞 형제 |
| T13 | 로딩 레터박스 불변 | `Loading.unity` `LetterboxFramer` | `_targetAspect` 1.7777778, `_maxAspect` 0 유지 |
| T14 | 배틀 레터박스 불변 | `Battle.unity` | `_targetAspect` 1.7777778, `_maxAspect` 2.3333333 유지, 기존 `LetterboxFramerTests` 통과 |
| T15 | 회귀 | `SafeAreaFitterTests`, `VillageHudVisualPolishTests`, `DotSceneRegressionTests` | 전부 통과 |
| T16 | **인셋 띠 × UI 사각형 쌍 검사 (전 씬)** | 비율 M1~M7 + 조건 사례(16:9, 하단 인셋 63)마다 인셋 띠 사각형(좌·우·상·하)을 §3.1.1 규칙으로 계산: 박스 안 인셋 = max(0, 인셋px − 띠px) ÷ scale. UI 사각형 목록 = 마을 HUD 존(T4)·배틀 HUD 존(T11)·로딩 6요소·팝업 패널(T10). 각 UI 사각형이 각 인셋 띠와 만나는지 **쌍으로** 교차 검사. 마을·배틀 HUD·로딩은 `SafeAreaFitter.ComputeAnchors` 로 줄인 영역을 부모로 사용 | 모든 쌍 교차 면적 0. **보정 전 상태에서는 로딩 M2~M5 가 실패해야 한다**(검사기 자체의 회귀 확인용으로 현행 앵커 값 입력 케이스도 하나 포함해 "실패함"을 단언) |
| T17 | 로딩 하단 3요소 | 로딩 `Panel` 에 C2 를 적용한 앵커로 `ProgressBarBg`·`PercentText`·`DescText` 의 아래 끝 계산 | M2~M5·조건 사례(인셋 63): 3요소 모두 아래 끝 ≥ 인셋 63 (예: `ProgressBarBg` 99, `PercentText` 103, `DescText` 167). M1·M6·M7: 인셋 0 이라 현행 값(36, 40, 104) |
| T18 | 로딩 배선 | `Loading.unity` 의 `Panel` 에 `SafeAreaFitter`, 그 `_gameCamera` == `LetterboxFramer._gameCamera` == 캔버스 `m_Camera` | null 0, 동일 카메라 |
| T19 | 팝업 루트 배선 | `Village.unity`·`Battle.unity` 의 `PopupRoot` 에 `SafeAreaFitter`, 배틀 `PopupRoot` 앵커 (0,0)~(1,1) | 위 값 일치, `_gameCamera` 연결 |

노트: 월드 코너 계산이 `LayoutGroup` 영향을 받으면 `LayoutRebuilder.ForceRebuildLayoutImmediate`. `Canvas` 없이 RectTransform 부모만으로 앵커 계산이 되므로 PlayMode 불필요.

### 11.2 Game 뷰·실기 체크리스트 (자동화 불가)

해상도 7종: 1920x1080, 2340x1080, 2400x1080, 2520x1080, 2640x1080, 1920x1200, 2048x1536.

| ID | 씬 | 항목 | 통과 기준 |
|---|---|---|---|
| V0 | Village | 원인 실측: S25+ 실기 1대와 Device Simulator 1회에서 `Screen.width/height`, `Screen.safeArea`, 캔버스 `rect`, 마을 `Main Camera.rect` 로그 | §1.3~1.6 계산값과 대조해 차이 기록. 논리 높이 < 640 이면 §1.6 "근거 한계" 갱신 |
| V1 | Village | HUD 7해상도 캡처 | 사각형 겹침 없음, 영웅 가운데, 메뉴·상단 바 가장자리 정렬, 구멍 영역 안쪽 시작(인셋 기기) |
| V2 | Village | 팝업 13종 순서대로 열기(M2·M7 필수, 나머지 비율은 침입 지도·유물·기록만) | 패널 전체가 화면 안, 닫기 버튼 보임, `Dim` 이 HUD 터치 차단 |
| V3 | Village | 침입 지도: 7해상도 | 난이도 박스·"이 스테이지로 선택"·닫기가 보임, 좌우 쏠림 없음. 패널 안 텍스트 넘침 확인(§2.1 단서) |
| V4 | Battle | `HpBar` 부착처 확인, `SkillUnlockBanner` 해금 연출 M1·M4·M5 캡처 | 밴드가 화면 양끝에 닿는지(B2 필요 여부), `HpBar` 위치 기록 |
| V5 | Battle | `BattleHud` + 팝업 5종(Result·CardSelection·BuildModal·SynergyModal·Confirm) M1~M7 | 패널 전체 안, `Dim` 이 화면 끝까지, 하단 인셋 기기에서 `BuildPanel`↔`SpawnerStatusPanel` 겹침 없음 |
| V6 | Battle | 레터박스 불변 | M5·M6·M7 에서 띠 모양이 변경 전과 동일 |
| V7 | Loading | 7해상도 캡처 + 캔버스 `rect.size` 로그(W3). **하단 인셋 확인**: 하단 인셋이 있는 기기(Device Simulator, 또는 Game 뷰에 인셋 63px 시뮬) 에서 M1~M5 캡처 | 논리 1920x1080, 텍스트·진행바·퍼센트 안 잘림, **진행바·퍼센트·설명이 하단 인셋 띠 위**(겹침 0), 상단 인셋이 있으면 `TitleSmall` 이 구멍 아래, 띠 모양 불변, 배경·월드가 인셋 영역까지 채워짐 |
| V9 | 마을·배틀 | 하단 인셋 기기에서 큰 팝업 3개(`RelicPopup`, `ResultPopup`, 침입 지도) M2~M5 | 패널 프레임(Relic 은 본체와 글로우 모두) 전체가 하단 인셋 띠 위, `Dim` 이 띠·구멍까지 덮음 |
| V8 | 공용 | 토스트 1개, 확인창 1개를 마을·배틀 각 M2 에서 | 가운데, 하단 버튼과 안 겹침 |

qa-simulator 밸런스 게이트: 불필요.

---

## 12. 열린 결정 (사용자 선택)

| # | 질문 | 권장 | 대안 | 기존 결정과의 관계 |
|---|---|---|---|---|
| D1 | 마을 하단 버튼 순서를 "탑 · 지도 · 방어하기" 로 바꿀까? | 바꾼다(V2) | 순서 유지 + `StagePanel` 폭 축소(B, 작업량 큼) | 유지 |
| D2 | 태블릿에서 버튼·팝업을 키우지 않고 여백만 늘릴까? | 여백만 | 태블릿 전용 확대 스케일 | 유지 |
| ~~D3~~ | (폐기) 팝업은 Safe Area 를 따르지 않고 "중앙 고정 + 크기 상한" 으로만 보호 | — | — | **폐기.** §2.6 에서 하단 인셋 띠 침범이 확인돼 틀린 전제였음. D8 로 교체 |
| D4 | 지원 하한 밖(16:9 폰 + 큰 노치)은 지원 밖으로 둘까? | 지원 밖 | 하한 밖 보정(HUD 축소) | 유지 |
| D5 | 로딩 화면을 16:9 박스 안에만 두는 현행을 유지할까(Expand 로 크기만 고정)? | 유지 | 로딩 UI·배경을 와이드에서 전체 폭으로 확장(배경·미러 재작업, 범위 확대) | **신규.** 초판 "로딩 불변"과 부분 충돌(스케일러만 바뀜) |
| D6 | 배틀 우측 패널 2개를 위로 32 올려 하단 인셋 기기(16:9 폰)에서도 겹치지 않게 할까? | 올린다(B1) | 현행 유지 + 16:9 하단 인셋 폰을 지원 밖으로 | **신규.** 초판 "배틀 불변"과 충돌 |
| D7 | 스킬 해금 배너 밴드가 와이드에서 모자라면 `Dim` 처럼 가로로 늘릴까? | 늘린다(B2, 캡처 확인 후) | 그대로 | **신규** |
| D8 | 팝업도 HUD 처럼 Safe Area 안에 놓고(마을·배틀 `PopupRoot` 에 기존 `SafeAreaFitter`), 배경 어두운 막(`Dim`) 16개를 위아래·좌우로 늘리며, 유물 팝업의 장식 글로우를 3% 줄이고 결과창을 14 위로 올릴까? | 그렇다(P1-A) | 큰 패널에 자동 맞춤 컴포넌트 신규(P1-B, 코드 추가) / 프레임 걸침 허용(P1-C, 사용자 기준 위반) | **신규.** 초판·1차 D3 와 반대, D3 폐기 |
| D9 | 로딩 `Panel` 에 기존 `SafeAreaFitter` 를 붙여 하단 인셋(진행바·퍼센트)을 피하게 할까? | 그렇다(C2) | 하단 요소를 인셋만큼 올리는 개별 오프셋(비율마다 값·코드 필요) | **신규.** 1차 개정 "로딩 보정은 C1 뿐"을 정정 |

---

## 13. Self-Review

- Placeholder·모호 표현·두 갈래 위임 표현: 0건(열린 선택은 12장에만).
- 내부 일관성: 하한 폭 1160·높이 660(마을), 본체 상한 1136x640, `TowerButton` x −228, B1 값 −80/−355 가 §3·§4·§5·§10·§11 에서 동일. 검산: 폭 1160 = 2×580, 높이 660 = 320+340, 가로 겹침 `b−14` = 626 − (640 − b), B1 후 `b−46` = 594 − (640 − b), 인벤토리 47 = 3 + 22 + 3 + 18 + 1, 프리팹 43 = 22 + 3 + 18.
- 식별자: `HudRoot`, `PopupRoot`, `SafeAreaFitter`, `CHMUI.SetRoot`, `VillageController`, `_hudRoot`, `_popupRoot` 통일.
- 확인 못 한 항목 명시: #20, #25, 셀 18행, 패널 내부 텍스트 넘침, 패널 안 버튼이 인셋 띠에 있는지(P1-C 폐기 사유), `CanvasScaler` 해석(H1/H2).
- 2차 개정 검산: 로딩 겹침 = 인셋 − 요소 시작값 (63 − 36 = 27, 63 − 40 = 23, 63 − 44 = 19). C2 후 위치 = 36 + 63 = 99, 40 + 63 = 103, 104 + 63 = 167. Relic 장식 글로우 아래 끝 = 360 + 684/2 = 702 → 702 − 678 = 24 (P1 후 681 → 3); scale 0.97 → 339 + 331.7 = 670.7 ≤ 678. Relic 본체 = 339 + 320 = 659 (침범 0). Result 아래 끝 = 360 + 24 + 326 = 710 → 32; 보정 후 339 + 10 + 326 = 675 ≤ 678. 640 패널 = 339 + 320 = 659. 인벤토리 집계 7 → 9 (S3, #16 추가).
- 이 문서 안의 "띠가 인셋을 흡수" 서술은 §3.1.1 규칙(띠px ≥ 인셋px)만 남기고 모두 계산으로 대체했다.
- 스코프: 단일 구현 단위(표시 계층 보정). 신규 JSON·Enum·Interface·프리팹·화면 없음. §8 위반 없음.
- UI 목업: `.mockups/resolution-adaptation.html` 갱신.
