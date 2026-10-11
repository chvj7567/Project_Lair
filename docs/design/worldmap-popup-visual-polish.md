# 침입 지도 팝업(WorldMapPopup) 비주얼 폴리시

## § 헤더

- **목표**: 승인된 목업 `.mockups/worldmap-redesign-v2.html` 의 마감(이중 테두리·글로우·보석 칩·원형 메달리온 노드·챕터 풍경·광택 버튼)을 Unity 에 옮긴다. 선행 기획서 `docs/design/worldmap-popup-redesign.md`(이하 "선행서") 의 레이아웃·좌표·문구·동작·ViewModel 은 한 글자도 바꾸지 않고, 그 위에 **장식 레이어와 스프라이트만 얹는다.**
- **검증 가설**: 레이아웃이 같은 상태에서 입체감·발광·풍경 레이어만 올려도, 선택 노드·클리어 구간·보스 노드·진행 칩이 한눈에 구분되는가. (정량 메트릭 없음 — 사용자 육안 확인용 UI 개선)
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 "아트/에셋 작업 허용" + 챕터 구조 허용 항목 ④(월드맵 팝업 표시 개선). Ch4~Ch5·신규 규칙·신규 콘텐츠 추가 없음.
- **핵심 메커니즘**: (1) 신규 절차 생성 스프라이트 23종(§3)을 프리팹의 정적 Image 로 배치한다. (2) 챕터별로 달라지는 값(실루엣 색·노드 열림색·노드 글로우색)은 기존 `WorldMapChapterPalette` 에 3개 조회 함수를 더해 View 가 읽는다(§5). (3) 움직임 7종은 공용 경량 컴포넌트 `UILoopMotion` 한 종류로 처리하고, 끄면 정적 포즈가 되게 한다(§4).

목업 대비 의도적 차이는 §9 에 모았다. 이 문서의 수치가 목업과 다르면 이 문서가 우선한다.

---

## 1. 불변 사항

- 선행서 §1~§5 의 좌표·크기·문구·상태 표시 규칙 전부 불변. 아래는 그중 특히 못박는 항목이다.
  - 팝업 패널 (72,40) 1136x640, 헤더 64, 카드 레일 (88,120) 200x472, 지도 (304,120) 520x472, 상세 (840,120) 352x472, 안내 문구·선택 버튼 y=604 높이 56. (v2 목업의 하단 바 y=572/576 은 선행서 §10 에서 이미 보정한 겹침 문제를 그대로 가진다 — 604 유지.)
  - 노드 중심·크기(1~4번 64x64, 5번 88x88), 연결선 4구간 중심·길이·회전(직선), 카드 내부 위젯 좌표, 상세 카드 위젯 좌표.
  - 글자 크기·글자 색: 선행서 값 그대로. (목업 v2 가 카드 번호 32·챕터명 16 으로 키운 것은 따르지 않는다.)
- `WorldMapViewModel`·`WorldMapTabData`·`WorldMapNodeData`·`StageProgress` 변경 0건. `Strings_Ko` 신규 문구 0건, 신규 JSON 0건, 신규 SO 0건, 신규 Enum 0건, 신규 Addressable 키 0건.
- 기존 `[SerializeField]` 필드는 이름·타입 유지. 신규 필드는 §6 에 전부 나열한다.
- 신규 장식 Image 는 전부 `raycastTarget = false`. 클릭 대상은 기존 `CHButton` 위젯뿐이다.
- 신규 글리프 의존 0건: ★ · ▶ 는 글리프가 아니라 스프라이트로 그린다(NotoSansKR SDF 아틀라스에 해당 글자가 있는지에 의존하지 않기 위함). 기존 `▼`(`_selectedMarker` 안 CHText)는 그대로 둔다.

---

## 2. 요소별 Unity 구현 방식

판단 기준: **Image(스프라이트)** = 정적 장식, 프리팹에 박고 코드 참조 없음 / **Shadow·Outline** = UGUI 이펙트 컴포넌트 / **트윈** = `UILoopMotion`(§4). 스프라이트 이름은 §3 의 파일명이다. 글로우는 전부 기존 `Assets/_Lair/Art/Sprites/UISoftGlow.png`(라디얼 소프트글로우) 재사용이며 아래 표에서 "UISoftGlow" 로 적는다.

"그라데이션 2장 조합"이란 바닥색 단색 Image 위에 `WorldMapGradientV`(알파 1→0, 위→아래, 세로 그라데이션 용도)와 `WorldMapGradientH`(알파 1→0, 좌→우, 가로 그라데이션 용도)를 각각 해당 방향 표에서 지정한 색으로 틴트해 겹치는 방식이다. 결과는 위(왼) 색에서 바닥색으로 이어지는 2색 그라데이션이 된다. UGUI 에 그라데이션 컴포넌트가 없어 쓰는 표준 조합이다.

### 2.1 프레임·헤더

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 프레임 이중 테두리 | Image(Sliced, fillCenter 끔) 를 기존 Panel(72,40,1136x640) 위에 전체 크기로 덮음 | `WorldMapFrame` | 없음 |
| 패널 바닥 그라데이션 | 그라데이션 2장 조합: 바닥 `#121827`, 위 `#1a2132` | `WorldMapGradientV` | 없음 |
| 패널 아래 그림자 | 기존 Panel Image 에 `Shadow` 컴포넌트: effectDistance (0,−10), 색 `#000000` 알파 0.44 | — | 없음 |
| 모서리 장식 4개 | Image 4장. TL 은 원본, TR 은 RectTransform scale (−1,1), BL 은 (1,−1), BR 은 (−1,−1). 크기 28x28, 좌상단 좌표 (75,43)/(1177,43)/(75,649)/(1177,649). 렌더 순서는 닫기 X 버튼 **아래**(TR 이 X 버튼 영역과 15x23 겹침 — X 버튼이 덮는다) | `WorldMapCornerGold` | 없음 |
| 헤더 그라데이션 | 기존 헤더 띠에 그라데이션 2장 조합: 바닥 `#10151f`, 위 `#232c44` | `WorldMapGradientV` | 없음 |
| 제목 글로우 | 제목 "침입 지도" 뒤에 Image: UISoftGlow, 색 `#f7c64a` 알파 0.40, 크기 240x64, 중심은 제목 텍스트 중심 | `UISoftGlow` | 없음 |
| 룬 라인 | Image 180x3 at (96,94), 색 `#f7c64a` | `WorldMapGradientH` | 없음 |
| 룬 끝 다이아 | Image 10x10 at (276,91), z 회전 45°, 색 `#f7c64a`, 뒤에 UISoftGlow 28x28 색 `#f7c64a` 알파 0.50 | 단색 Image(스프라이트 없음) + `UISoftGlow` | 없음 |

검산: 모서리 TR x = 1177 + 28 = 1205 ≤ 패널 우측 1208. BL/BR y = 649 + 28 = 677 ≤ 패널 하단 680. 룬 라인 하단 94 + 3 = 97 ≤ 헤더 하단 104.

### 2.2 챕터 카드 (WorldMapChapterTab.prefab)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 번호 배지 입체감 | 기존 `_numberBadge` Image 의 스프라이트를 `WorldMapBadge` 로 교체(Sliced). 색은 기존 코드가 넣는 챕터 Accent·잠금색 그대로 | `WorldMapBadge` | 기존 `_numberBadge` |
| 진행 칩 보석(사선) | 기존 `_chips[5]` Image 의 스프라이트를 `WorldMapChipGem` 으로 교체, 크기 15x9 유지. 켜짐/꺼짐은 기존 코드의 색(`#f7c64a` / `#0c1017`)이 틴트로 작동 — 스프라이트가 흰색 기반 그라데이션이라 켜짐은 금색 광택, 꺼짐은 암색이 된다. 칩 글로우는 생략(§9) | `WorldMapChipGem` | 기존 `_chips` |
| 선택 글로우 + ▶ | 새 루트 `SelectedDecor`(GameObject) 안에 ① UISoftGlow Image 232x116(카드 중심, 카드보다 가로·세로 각 16 큼) 색 `#f7c64a` 알파 0.33 ② `WorldMapPointer` Image 18x18 at (카드 right 6, top 30), `UILoopMotion` Nudge. 카드 선택일 때만 켠다. 카드 금색 외곽은 기존 `_selectedSprite` 그대로 | `UISoftGlow`, `WorldMapPointer` | **신규 `_selectedDecor`** |
| 카드 배경 | 기존 3 스프라이트(선택/개방/잠금) 유지. 그라데이션 변경 없음(§9) | — | 기존 |

### 2.3 지도 (MapArea)

렌더 순서(아래→위, 목업 DOM 순서와 동일): `MapBackground` → 달 글로우 → 달 → 실루엣 → 안개 3 → 연결선 4구간(선행서 §4.4) → 비네트 → 제목 오버레이 → 노드 5.

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 지도 테두리 | Image(Sliced, fillCenter 끔) 520x472 전체 덮음, 지도 위 최상단 | `WorldMapInset` | 없음 |
| 지도 배경 | 기존 `_mapBackground` 단색 유지(선행서 §4.2). 목업의 방사형 그라데이션은 비네트가 대체 | — | 기존 |
| 챕터 실루엣 | Image 520x260, 지도 하단 정렬(지도 로컬 y 212~472). 스프라이트는 챕터별 3종, 색은 팔레트 `Silhouette(chapterId)` | `WorldMapSilhouetteCh1`/`Ch2`/`Ch3` | **신규 `_mapSilhouette`, `_silhouetteSprites`** |
| 달 | Image 46x46 at 지도 로컬 (446,56). 뒤에 UISoftGlow 120x120 같은 중심, 색 `#fff2b8` 알파 0.27. 챕터 불문 고정 | `WorldMapMoon`, `UISoftGlow` | 없음 |
| 안개 3줄 | Image 3장 624x70, 지도 로컬 x = −52(120% 폭, 좌우 −10%), y = 90 / 220 / 330. 색 흰색 알파 0.05. 각각 `UILoopMotion` Drift | `WorldMapFog` | 없음 |
| 비네트 | Image 520x472 전체, 색 검정(스프라이트에 알파 내장, Image 알파 1) | `WorldMapVignette` | 없음 |
| 제목 오버레이 | 기존 `_chapterTitleText`·`_chapterThemeText` 유지(선행서 §4.2 좌표). 하드 그림자는 생략(§9) | — | 기존 |
| 지도 영역 렌더 격리 | `MapArea` 루트에 `Canvas`(Override Sorting 끔) + `GraphicRaycaster` 추가 — 안개·링 이동이 팝업 전체 메시를 재빌드하지 않게 한다 | — | 없음 |

### 2.4 노드 (WorldMapNode.prefab)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 원형 메달리온 4상태 | 기존 `_openSprite`/`_bossSprite`/`_clearedSprite`/`_lockedSprite` 슬롯에 새 스프라이트를 연결(필드 불변). 열림 노드만 Image 색을 틴트(§5 `NodeOpenTint`), 나머지 3상태는 Color.white | `WorldMapMedalOpen`/`Boss`/`Cleared`/`Locked` | 기존 + **신규 `SetChapter`**(§6) |
| 노드 드롭 그림자 | 노드 `_background` Image 에 `Shadow` 컴포넌트: effectDistance (0,−5), 색 `#000000` 알파 0.53 | — | 없음 |
| 노드 글로우 | 새 Image `_glow`(UISoftGlow, 노드 중심, 크기 = 노드 크기 + 40 → 1~4번 104x104, 5번 128x128, 노드 뒤). 색·표시는 상태별(아래 표). 보스 맥동은 `_glow` 오브젝트의 `UILoopMotion`(Pulse) | `UISoftGlow` | **신규 `_glow`, `_glowMotion`** |
| 클리어 ★ | 새 루트 `_starBadge`(GameObject) 안에 `WorldMapStarBadge` Image 24x24 at (노드 right −6, top −8). `Cleared` 상태에서만 켠다 | `WorldMapStarBadge` | **신규 `_starBadge`** |
| 클리어 노드 라벨색 | 금빛 바탕 위 가독성을 위해 `Cleared` 상태의 `_labelText` 색을 `#2a2108` 로 지정(그 외 상태는 기존 흰색/잠금색) | — | 기존 `_labelText` |
| 선택 회전 프레임 | 기존 `_selectedMarker` 루트 안의 외곽선 Image 를 `WorldMapNodeSelRing` 으로 교체, 크기 = 노드 크기 + 24 → 1~4번 88x88, 5번 112x112, 노드 중심. `UILoopMotion` Rotate. 기존 ▼ CHText 는 그대로 + `UILoopMotion` Nudge | `WorldMapNodeSelRing` | 기존 `_selectedMarker` |
| 노드 이름 명패 | `_nameText` 뒤에 Image(Sliced) 를 `_nameText` 와 같은 RectTransform 크기(폭 = 노드 폭 + 60, 높이 18)로 깔고, `_nameText` 를 그 위에 둔다 | `WorldMapNamePlate` | 없음 |

노드 글로우 상태표(코드가 `Show` 에서 결정):

| 노드 상태 | `_glow` 표시 | `_glow` 색 | `_glowMotion` |
|---|---|---|---|
| 잠금 | 숨김 | — | 정지 |
| 열림(일반) | 표시 | 챕터 Accent 알파 0.45 (§5 `NodeGlow`) | 정지 |
| 열림(보스) | 표시 | `#e5484d` 알파 0.67 | 동작(§4) |
| 클리어(보스 포함) | 표시 | `#f7c64a` 알파 0.47 | 정지 |

목업의 `.node.boss.cleared` 가 맥동을 끄는 것과 같다 — 클리어되면 맥동이 멈추고 금색으로 바뀐다.

### 2.5 연결선 (선행서 §4.4 구간 4개)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 금색 발광 실선 | 기존 `_pathSolid[k]`(길이×5, `#f7c64a`) 유지. 자식 Image 1장 추가: 크기 = (길이+16)×18, 부모 중심, 부모 뒤, 색 `#f7c64a` 알파 0.60. 자식이라 실선 활성과 함께 켜지고 꺼진다 | `WorldMapPathGlow`(Sliced) | 기존 `_pathSolid` |
| 점 점선 | 기존 `_pathDashed[k]` 의 스프라이트를 `WorldMapPathDot` 으로 교체(Tiled), 높이 4, 색 흰색 알파 0.33 | `WorldMapPathDot` | 기존 `_pathDashed` |

`WorldMapPathDash.png`(선행서 §4.4 신설분)는 교체 후 어디서도 참조되지 않으므로, 프리팹 재배선이 끝난 같은 커밋에서 `WorldMapPathDash.png` 와 `.meta` 를 삭제한다(참조처 확인: 현재 `WorldMapPopup.prefab` 1곳뿐).

### 2.6 상세 카드 (WorldMapPopup.prefab 안)

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 상세 패널 바닥·테두리 | 그라데이션 2장 조합(바닥 `#121827`, 위 `#1a2132`) + `WorldMapInset` Image 352x472 덮음 | `WorldMapGradientV`, `WorldMapInset` | 없음 |
| 초상 발광 | `_detailPortrait` 뒤에 새 Image `_portraitGlow`(UISoftGlow 140x140, 초상 중심). 색 = 선택 챕터 Accent 알파 0.60. `_detailPortrait.enabled` 와 같이 켜고 끈다 | `UISoftGlow` | **신규 `_portraitGlow`** |
| 배너 하단 암부 | 배너(328x120) 안 하단에 Image 328x50: `WorldMapGradientV` 를 RectTransform scale (1,−1) 로 뒤집어(알파가 아래에서 1), 색 검정 알파 0.67. 초상 위, 배너 텍스트 아래 | `WorldMapGradientV` | 없음 |
| 이름 아래 금색 구분선 | Image 320x2 at 패널 (16,174), 색 `#f7c64a` | `WorldMapGradientH` | 없음 |
| 기록 박스 | 기존 `RecordPanel` 단색 `#0d1119` 위에 `WorldMapInset` Image 328x48 덮음 | `WorldMapInset` | 없음 |
| 섹션 구분선 | "스테이지 규칙"·"보스 페이즈" 라벨 행을 `HorizontalLayoutGroup`(spacing 8, Child Alignment Middle Left, Control Child Size 가로 켬) 으로 묶고, 라벨 오른쪽에 Image(높이 1, `LayoutElement.flexibleWidth` 1, 흰색 알파 0.20)를 추가. 라벨 폭은 텍스트 폭에 맞고 선은 남는 폭을 채운다 | `WorldMapGradientH` | 없음 |
| 규칙 그라데이션 띠 | 규칙 블록 바탕을 그라데이션 2장 조합(바닥 `#0e131f`, 왼쪽 `#1b2740`, `WorldMapGradientH`)으로 교체. 기존 `_rulesBar` 색 `#5aa9ff` 폭 4 유지 | `WorldMapGradientH` | 기존 `_rulesBar` |
| 페이즈 그라데이션 띠 | 페이즈 블록 바탕을 그라데이션 2장 조합(바닥 `#150b10`, 왼쪽 `#3a1a20`)으로 교체. 기존 `_phaseBar` 색 `#e5484d` 폭 4 유지 | `WorldMapGradientH` | 기존 `_phaseBar` |

규칙·페이즈 블록의 높이는 선행서 §5 의 `VerticalLayoutGroup` 이 정한다. 새 그라데이션 Image 는 바닥 Image 와 같은 RectTransform(stretch) 으로 맞춰 높이 변화에 따라가게 한다.

### 2.7 하단 바·버튼

| 요소 | 방식 | 스프라이트·수치 | 코드 참조 |
|---|---|---|---|
| 선택 버튼 광택+글로우 | 새 루트 `_selectGlow`(GameObject) 안에 ① UISoftGlow Image 396x100(버튼 중심) 색 `#e5484d` 알파 0.40, 버튼 뒤 ② `WorldMapGradientV` Image 336x10 at (버튼 left 8, top 6) 흰색 알파 0.25, 버튼 위. 기존 `_selectButton` 스프라이트·텍스트는 불변 | `UISoftGlow`, `WorldMapGradientV` | **신규 `_selectGlow`** |
| 잠금(비활성) 버튼 | `_vm.SelectEnabled == false` 이면 `_selectGlow` 숨김. 회색 무광은 기존 비활성 스프라이트가 담당 | — | `_selectGlow` |
| 닫기 X·글자 간격 | 변경 없음(§9) | — | 기존 |

---

## 3. 신규 스프라이트 (23종)

### 3.1 공통 규칙

- 위치: `Assets/_Lair/Art/Sprites/` 루트에 파일명 접두어 `WorldMap` (선행서 `WorldMapPathDash.png` 와 같은 관례). 프리팹이 인스펙터로 직접 참조하므로 Addressables 키·Enum 없음.
- 생성 방법: **일회용 에디터 스크립트** `Assets/_Lair/Editor/WorldMapPolishSpriteGenerator.cs` 가 `Texture2D` 에 픽셀을 계산해 PNG 로 저장하고 `TextureImporter` 로 설정한다. PNG + `.meta` 가 생성·커밋되면 **스크립트를 삭제한다**(Rule 04 §3 — 단일 진실은 PNG). 재생성이 필요하면 스크립트를 재작성해 한 번 돌리고 다시 삭제한다.
- 임포트 공통 설정: Texture Type Sprite(2D and UI), Sprite Mode Single, Pixels Per Unit 100, Mesh Type Full Rect, Generate Mip Maps 끔, Compression None, sRGB 켬, Alpha Is Transparency 켬, Pivot Center. Wrap Mode 는 Clamp(예외: `WorldMapPathDot` = Repeat). Filter Mode 는 Bilinear(예외: `WorldMapPathDot`, `WorldMapChipGem` = Point).
- 안티앨리어싱: 곡선·사선 가장자리는 4x4 슈퍼샘플링으로 커버리지를 구해 알파에 반영한다.
- 색은 전부 sRGB `#rrggbb`, 알파는 0~255 정수(0.xx 는 ×255 반올림).
- 신규 텍스트(문구) 0건 — 스프라이트에는 글자를 굽지 않는다.

### 3.2 스프라이트 표

| # | 파일명 | 크기(px) | 9-slice border (좌,하,우,상) | 내용 (픽셀 정의) |
|---|---|---|---|---|
| 1 | `WorldMapFrame.png` | 32x32 | 9,9,9,9 | 테두리 링만(중앙 알파 0). 바깥→안쪽: 3px `#07090e` · 2px `#6b5a2a` · 2px `#07090e` · 2px `#3a4764` (합 9px) |
| 2 | `WorldMapCornerGold.png` | 28x28 | 없음 | TL 방향 ㄱ자. 가로 막대 x 3~24·y 3~5, 세로 막대 x 3~5·y 3~24 (두께 3), 색 `#f7c64a`. 바깥 3px 에 `#f7c64a` 알파 136→0 선형 감쇠 글로우 |
| 3 | `WorldMapGradientV.png` | 4x64 | 없음 | 흰색. 알파 = 255 × (1 − y/63), y=0 이 위 |
| 4 | `WorldMapGradientH.png` | 64x4 | 없음 | 흰색. 알파 = 255 × (1 − x/63), x=0 이 왼쪽 |
| 5 | `WorldMapInset.png` | 16x16 | 5,5,5,5 | 링만(중앙 알파 0). 바깥 3px `#07090e`, 안쪽 2px 흰색 알파 24 |
| 6 | `WorldMapBadge.png` | 16x16 | 0,3,3,2 | 흰색 기반. 대각 그라데이션 명도 1.00(좌상)→0.62(우하). 위 2행에 흰색 알파 84 덧칠, 아래 3행에 `#000000` 알파 84 덧칠, 오른쪽 3열 `#07090e` 불투명 |
| 7 | `WorldMapChipGem.png` | 30x18 | 없음 | 평행사변형(skewX −20°): 아래 변 x 0~22, 위 변 x 7~29 (좌하→우상 사선). 1px `#000000` 테두리, 내부 수직 그라데이션 흰색 명도 1.00(위)→0.82(아래) |
| 8 | `WorldMapPointer.png` | 24x24 | 없음 | 우향 삼각형 꼭짓점 (6,3)(6,21)(20,12), 색 `#f7c64a`. 바깥 4px `#f7c64a` 알파 136→0 글로우 |
| 9 | `WorldMapMedalOpen.png` | 128x128 | 없음 | 원(지름 128). 바깥 테두리 8px `#07090e`. 내부 라디얼(중심 (35%,30%), 반경 = 0.955×128 로 정규화, 0→0.65 구간 선형): 흰색 명도 1.00→0.72 (틴트 곱셈용 회색조). 윗 초승 6px 흰색 알파 102, 아랫 초승 10px `#000000` 알파 102 |
| 10 | `WorldMapMedalBoss.png` | 128x128 | 없음 | #9 와 같은 구조. 라디얼 `#e0646c`→`#8a2229`. 윗 초승 `#ffb0b4` 알파 255, 아랫 초승 `#3a0a0e` 알파 255 |
| 11 | `WorldMapMedalCleared.png` | 128x128 | 없음 | #9 와 같은 구조. 라디얼 `#f3dc86`→`#a8892e`. 윗 초승 `#fff3b0`, 아랫 초승 `#4a3d0e` (알파 255) |
| 12 | `WorldMapMedalLocked.png` | 128x128 | 없음 | #9 와 같은 구조. 라디얼 `#262c3c`→`#141822`. 윗 초승 없음, 아랫 초승 8px `#0a0c12` 알파 255 |
| 13 | `WorldMapNodeSelRing.png` | 256x256 | 없음 | 링: 바깥 반지름 128, 두께 8, 색 `#f7c64a`. 16개 점선 호(호 11.25° + 틈 11.25° 반복, 시작 각 0°). 링 양옆 6px `#f7c64a` 알파 128→0 글로우 |
| 14 | `WorldMapStarBadge.png` | 48x48 | 없음 | 5각 별(바깥 반지름 20, 안쪽 8.5, 중심 (24,24), 꼭짓점 위쪽). 흰색 채움, 1px `#000000` 외곽선, 바깥 5px `#f7c64a` 알파 153→0 글로우 |
| 15 | `WorldMapNamePlate.png` | 8x8 | 2,2,2,2 | 채움 `#05070b` 알파 187. 1px 테두리 흰색 알파 34 |
| 16 | `WorldMapPathDot.png` | 12x4 | 없음 | 왼쪽 4x4 에 중심 (2,2) 반지름 2 흰색 원(알파 255), 나머지 8열 투명. Wrap Repeat, Filter Point |
| 17 | `WorldMapPathGlow.png` | 32x32 | 12,12,12,12 | 흰색. 중앙 코어 x 12~19 · y 12~19 는 알파 255, 코어 밖 거리 d(px) 에서 알파 = 255 × (1 − d/12)² |
| 18 | `WorldMapSilhouetteCh1.png` | 520x260 | 없음 | 흰색 불투명 도형, 바닥 정렬. "집" 5각형(꼭짓점 (0,100%)(0,40%)(50%,0)(100%,40%)(100%,100%)) 6개를 [left,높이,폭] = [0,150,40] [60,110,60] [150,170,36] [300,130,50] [380,190,44] [470,120,52] 로 배치한 합집합 |
| 19 | `WorldMapSilhouetteCh2.png` | 520x260 | 없음 | 삼각형(꼭짓점 (50%,0)(100%,100%)(0,100%)) 7개 [10,200,30] [70,150,26] [130,220,32] [250,170,28] [330,230,34] [420,160,30] [480,210,32] 의 합집합 |
| 20 | `WorldMapSilhouetteCh3.png` | 520x260 | 없음 | 성채 첨탑(꼭짓점 (0,100%)(0,18%)(18%,18%)(18%,0)(36%,0)(36%,18%)(64%,18%)(64%,0)(82%,0)(82%,18%)(100%,18%)(100%,100%)) 6개 [0,170,70] [90,230,44] [190,140,80] [300,200,50] [380,260,56] [450,150,70] 의 합집합 |
| 21 | `WorldMapMoon.png` | 64x64 | 없음 | 원(지름 64). 라디얼 중심 (35%,35%), `#fffbe8`→`#d9cfa0` (0→0.70 선형) |
| 22 | `WorldMapFog.png` | 4x64 | 없음 | 흰색. 알파 = 255 × sin²(π × y/63) (위·아래 0, 중앙 255) |
| 23 | `WorldMapVignette.png` | 128x128 | 없음 | 검정. 알파 = 204 × clamp((rn − 0.40)/0.60, 0, 1). rn = √(((x−63.5)/63.5)² + ((y−63.5)/63.5)²) / √2 |

실루엣 [left, 높이, 폭] 의 단위는 520x260 캔버스 기준 px 이고 도형은 캔버스 바닥(y=260)에 붙인다. 목업 배열과 같은 값이다(최대 높이 260 = Ch3 의 `[380,260,56]`).

검산:
- 평행사변형 #7: 위 변 폭 22, 아래 변 폭 22, 사선 이동 18 × tan 20° = 6.55 ≈ 7px → 전체 폭 22 + 7 = 29 ≤ 30.
- 노드 메달 #9: 렌더 64 에서 테두리 8 × 64/128 = 4px(목업 border 4px 와 일치), 윗 초승 6 × 0.5 = 3px, 아랫 초승 10 × 0.5 = 5px(목업 inset 3px·5px 일치).
- 링 #13: 렌더 88 에서 두께 8 × 88/256 = 2.75px, 렌더 112 에서 3.5px (목업 3px).
- 실루엣 렌더: 지도 로컬 y 212 + 260 = 472 = 지도 하단.

---

## 4. 애니메이션 (경량 트윈 + 정적 폴백)

### 4.1 공용 컴포넌트 `UILoopMotion`

- 외부 트윈 라이브러리를 도입하지 않는다(패키지에 없음). 기존 UI 가 쓰는 코루틴·`Mathf` 보간과 같은 수준의 **자기 Transform 만 건드리는 MonoBehaviour 1종**이다. 위치는 `Assets/_Lair/Scripts/UI/`.
- 시간 기준은 `Time.unscaledTime`(팝업이 일시정지·슬로모션과 무관하게 움직이게).
- `OnEnable` 에서 기준 포즈(위치·회전·스케일·알파)를 저장하고 시작, `OnDisable` 에서 기준 포즈로 복원한다 → 팝업이 닫혀 비활성이 되면 자동 정지·정지 포즈 복귀.
- 의도 API: `SetRunning(bool)` 하나만 외부에 연다(`_glowMotion` 용). 나머지는 프리팹 인스펙터 값으로 동작하고 코드가 건드리지 않는다.
- 움직임 종류(컴포넌트 내부 Enum, 단일 시스템 내부라 `CommonEnum.cs` 에 두지 않음): `Drift`(기준→기준+진폭→기준 왕복), `Rotate`(등속 회전), `Nudge`(기준↔기준+진폭 2단 스텝), `Pulse`(사인 곡선 알파·스케일 왕복).
- 알파 변경이 필요한 `Pulse` 는 `[SerializeField] private Graphic` 하나를 받아 그 `color.a` 를 바꾼다(`GetComponent` 금지, Rule 02 §5).
- 곡선 값 계산은 시간 t 와 파라미터만 받는 정적 순수 함수로 분리해 EditMode 에서 시험 가능하게 한다(§8).

### 4.2 움직임 7종

| # | 대상 | 종류 | 파라미터 | 정적 폴백(끄면) |
|---|---|---|---|---|
| 1~3 | 안개 3줄 | `Drift` | 진폭 (+40, 0), 편도 14초, 선형 왕복 | 안개가 기준 위치(x = −52)에 고정 |
| 4 | 보스 글로우 `_glow`(열림 보스) | `Pulse` | 알파 0.67 ↔ 1.00, 스케일 1.00 ↔ 1.15, 주기 2초 사인. 마지막 값이 아니라 **시작 포즈가 알파 0.67·스케일 1.00** | 알파 0.67, 스케일 1.00 고정 |
| 5 | 선택 링 | `Rotate` | z −11.25°/초(시계 방향, 목업 90°/8초 와 같은 속도) | 회전 0° 고정 |
| 6 | 선택 마커 ▼ | `Nudge` | 진폭 (0, +4), 주기 1초, 2단 스텝(0.5초 기준, 0.5초 +4) | 기준 위치 고정 |
| 7 | 카드 ▶ 포인터 | `Nudge` | 진폭 (+3, 0), 주기 1초, 2단 스텝 | 기준 위치 고정 |

동시 동작 상한: 안개 3 + 링 1 + ▼ 1 + 보스 글로우 1 = 지도 6개, ▶ 1개(선택 카드 1장뿐) = 최대 7개. 지도 6개는 `MapArea` 의 중첩 Canvas(§2.3) 안이고, ▶ 는 카드 레일 부모 컨테이너(`Rail`, 없으면 신설)에 같은 방식의 `Canvas` + `GraphicRaycaster` 를 달아 격리한다. 두 중첩 Canvas 모두 Override Sorting 은 끈다.

### 4.3 정적 폴백 기준

기본은 7종 전부 **동작**이다. 아래 조건이면 해당 `UILoopMotion` 컴포넌트를 프리팹에서 끈다(Enabled 해제 — 정적 포즈가 정의된 이유):

- 최저 사양 대상 기기에서 팝업을 연 상태의 프레임 비용이 연 직후 대비 평균 +1.0ms 를 넘으면 안개 3종부터 끈다 → 그래도 넘으면 나머지도 끈다. (측정: Unity Profiler, 사용자 확인. 최저 사양 대상 기기는 사용자 선택 필요 — 현재 프로젝트 메타에 빌드 타깃 기기 기준이 없음.)
- 정적 폴백에서도 시각 정보 손실은 없다. 선택은 링·▼ 의 **존재**로, 보스는 붉은 글로우의 **존재**로 구분되며 움직임은 가산 연출이다.

---

## 5. 챕터 색조 — `WorldMapChapterPalette` 확장

기존 표(선행서 §4.1)·상수·함수(`Accent`·`MapBackground`·`BannerTint`·`BannerTintAlpha`)는 **변경 없음**. 아래 3개 함수와 상수 4개를 같은 정적 클래스에 추가한다. 챕터 4 이상은 기존과 같이 중립색 기반으로 계산한다(안전망).

| 신규 멤버 | 정의 | 사용처 |
|---|---|---|
| `Color NodeOpenTint(int chapterId)` | `Color.Lerp(Accent(chapterId), Color.white, NodeOpenLightness)` — rgb 만 보간, 알파 1 | 열림(일반) 노드 `_background.color` |
| `Color NodeGlow(int chapterId)` | `Accent(chapterId)` 의 알파만 `NodeGlowAlpha` 로 교체 | 열림(일반) 노드 `_glow.color` |
| `Color Silhouette(int chapterId)` | `MapBackground(chapterId)` 의 rgb × `SilhouetteDarken`, 알파 `SilhouetteAlpha` | `_mapSilhouette.color` |
| `const float NodeOpenLightness` | 0.30 | 위 |
| `const float NodeGlowAlpha` | 0.45 | 위 |
| `const float SilhouetteDarken` | 0.30 | 위 |
| `const float SilhouetteAlpha` | 0.80 | 위 (목업 `#05070bcc` 의 0xcc = 204/255 = 0.80) |

챕터별 결과값(검산):

| 챕터 | Accent | NodeOpenTint (rgb 0~1) | Silhouette rgb (= MapBackground × 0.30) |
|---|---|---|---|
| 1 | `#4a6fa5` = (0.290, 0.435, 0.647) | 0.290 + 0.3×(1−0.290) = 0.503 / 0.435 + 0.3×(1−0.435) = 0.605 / 0.647 + 0.3×(1−0.647) = 0.753 | `#141b2c` = (0.078, 0.106, 0.173) × 0.3 = (0.0235, 0.0318, 0.0518) |
| 2 | `#2f8a5f` = (0.184, 0.541, 0.373) | (0.184+0.245, 0.541+0.138, 0.373+0.188) = (0.429, 0.679, 0.561) | `#0e2216` = (0.055, 0.133, 0.086) × 0.3 = (0.0165, 0.0400, 0.0259) |
| 3 | `#7a55c8` = (0.478, 0.333, 0.784) | (0.478+0.157, 0.333+0.200, 0.784+0.065) = (0.635, 0.533, 0.849) | `#1b112d` = (0.106, 0.067, 0.176) × 0.3 = (0.0318, 0.0200, 0.0529) |

**대안 비교** (열림 노드 색을 어떻게 내는가):

| 안 | 장점 | 단점 |
|---|---|---|
| A. 회색조 메달 스프라이트 1장 + 팔레트 틴트 (채택) | 스프라이트 1장으로 챕터 3종·확장 챕터 대응, 팔레트가 단일 진실 | 곱셈 틴트라 하이라이트가 틴트색보다 밝아질 수 없음 → `NodeOpenLightness` 로 틴트를 미리 밝혀 보정 |
| B. 챕터별 열림 메달 스프라이트 3종 | 목업과 픽셀 일치 | 스프라이트 +2, 챕터 추가마다 스프라이트 추가, 색 이중 진실 |
| C. 프리팹에 챕터별 Color 배열 `[SerializeField]` | 인스펙터 튜닝 | 선행서 §4.1 대안 C 와 같은 이중 진실 |

실루엣은 같은 논리로 흰색 불투명 스프라이트 + `Silhouette()` 틴트다(챕터마다 도형만 다르므로 스프라이트는 챕터별 3종).

---

## 6. SerializeField 변경 목록과 연결 검사 영향

### 6.1 유지 (이름·타입·의미 불변)

- `WorldMapPopup`: 기존 전부(`_dimButton` … `_selectButton`, `_mapBackground`, `_pathSolid`, `_pathDashed`, `_bannerTint` 포함).
- `WorldMapChapterTabView`: 기존 전부(`_button` … `_chips`).
- `WorldMapNodeView`: 기존 전부(`_button` … `_lockedSprite`). `_openSprite`/`_bossSprite`/`_clearedSprite`/`_lockedSprite` 는 **슬롯 유지, 가리키는 스프라이트만 §3 의 메달 4종으로 교체**.

### 6.2 신규 `[SerializeField] private`

| 클래스 | 필드 | 타입 | 용도 |
|---|---|---|---|
| `WorldMapPopup` | `_mapSilhouette` | `Image` | 챕터 실루엣 |
| `WorldMapPopup` | `_silhouetteSprites` | `Sprite[]` (원소 3, 인덱스 = 챕터 id − 1) | 챕터별 실루엣 스프라이트 |
| `WorldMapPopup` | `_portraitGlow` | `Image` | 상세 초상 발광 |
| `WorldMapPopup` | `_selectGlow` | `GameObject` | 선택 버튼 글로우+광택 루트 |
| `WorldMapChapterTabView` | `_selectedDecor` | `GameObject` | 카드 선택 글로우+▶ 루트 |
| `WorldMapNodeView` | `_glow` | `Image` | 노드 글로우 |
| `WorldMapNodeView` | `_glowMotion` | `UILoopMotion` | 보스 글로우 맥동 on/off |
| `WorldMapNodeView` | `_starBadge` | `GameObject` | 클리어 ★ |
| `UILoopMotion` | 컴포넌트 자체 인스펙터 값(§4.1·4.2) | — | 프리팹 정적 설정, 코드 미접근 |

### 6.3 신규 코드 동작 (View 만, 로직 없음)

- `WorldMapPopup.Refresh()` 추가 동작:
  1. `_mapSilhouette`: `_silhouetteSprites[_vm.SelectedChapter − 1]` 를 스프라이트로, 색을 `Silhouette(_vm.SelectedChapter)` 로. 인덱스가 범위 밖(챕터 4 이상)이면 `_mapSilhouette` 를 숨긴다.
  2. `_portraitGlow`: 색 = `Accent(_vm.SelectedChapter)` 알파 0.60, 활성 = `_detailPortrait.enabled` 와 동일.
  3. `_selectGlow`: 활성 = `_vm.SelectEnabled`.
  4. 노드 5개 각각에 `Show` 호출 **전에** `SetChapter(_vm.SelectedChapter)` 호출.
- `WorldMapNodeView` 신규 의도 API 1개 — `void SetChapter(int chapterId)`: 그 챕터의 열림 틴트·글로우색을 내부에 저장한다. `Show` 가 이 값을 써서 `_background.color`(열림 일반 노드만 `NodeOpenTint`, 나머지 상태 Color.white), `_glow`(§2.4 상태표), `_glowMotion.SetRunning`(열림 보스일 때만 true), `_starBadge`(Cleared 일 때만 활성), `_labelText` 색(Cleared 면 `#2a2108`)을 결정한다.
- `WorldMapChapterTabView.Show` 추가 동작: `_selectedDecor` 활성 = `data.IsSelected`.
- ViewModel 입력 데이터는 기존 필드(`State`·`IsBoss`·`IsSelected`·`IsLocked`)만 읽는다. 신규 VM 필드 없음.

### 6.4 `WorldMapStringsAndPrefabTests` 연결 검사 영향

현재 검사 규칙(코드 확인 완료): ① `WorldMapPopup` 의 **배열이 아닌** `[SerializeField]` 는 전부 비어 있으면 실패 ② `_tabs[i]`·`_nodes[i]`·탭/노드 프리팹 컴포넌트는 `Sprite` 타입과 배열을 **제외한** `UnityEngine.Object` 필드가 전부 연결돼야 통과.

| 신규 필드 | 검사 대상 | 영향 |
|---|---|---|
| `_mapSilhouette` `_portraitGlow` `_selectGlow` | ① 해당 | 프리팹에서 비우면 실패 → 반드시 연결 |
| `_silhouetteSprites` | 배열이라 제외 | 검사에서 안 잡힘 → §8.1 에서 별도 테스트 추가 |
| `_selectedDecor` | 탭 검사 해당 | 5장 전부 연결 |
| `_glow` `_glowMotion` `_starBadge` | 노드 검사 해당 | 5개 전부 연결 |
| 메달 Sprite 4종(기존 슬롯) | `Sprite` 제외 | 안 잡힘 → §8.1 |

기존 `WorldMapStringsAndPrefabTests` **파일은 수정하지 않고** 무수정 통과가 기준이다. 신규 필드를 하나라도 비우면 이 테스트가 잡는다.

---

## 7. 프리팹·에셋 변경 범위

| 대상 | 변경 |
|---|---|
| `Assets/_Lair/Art/Sprites/WorldMap*.png` 23종 + `.meta` | 신규(§3). `WorldMapPathDash.png`+`.meta` 는 삭제(§2.5) |
| `Assets/_Lair/Editor/WorldMapPolishSpriteGenerator.cs` | 일회용. PNG 생성·임포트 설정 후 삭제(Rule 04 §3) |
| `Assets/_Lair/Scripts/UI/UILoopMotion.cs` | 신규 |
| `WorldMapChapterPalette.cs` | §5 멤버 추가 |
| `WorldMapPopup.cs` · `WorldMapChapterTabView.cs` · `WorldMapNodeView.cs` | §6 필드·동작 추가 |
| `Assets/_Lair/Art/UI/WorldMapChapterTab.prefab` | §2.2 |
| `Assets/_Lair/Art/UI/WorldMapNode.prefab` | §2.4 |
| `Assets/_Lair/Art/UI/WorldMapPopup.prefab` | §2.1·2.3·2.5·2.6·2.7, `MapArea`·`Rail` 중첩 Canvas, 신규 필드 4종 와이어링, `_nodes[5]`/`_tabs[5]` 인스턴스에도 변경 반영 |

- 프리팹 작업은 **기존 프리팹에 추가·교체만** 한다(새로 만들어 덮어쓰기 금지 — 손-편집 보존). 일회용 에디터 스크립트를 쓴다면 완료 직후 삭제한다(Rule 04 §3).
- 모든 신규 텍스트 위젯 없음(문구 0건). 기존 TMP 에는 이미 `CHText` 가 붙어 있다.
- `Instantiate`/`new GameObject` 로 장식을 코드에서 만들지 않는다. 전부 프리팹 정적 배치.
- UI 배선(UnityMCP·빌더 실행)은 목업(`.mockups/worldmap-redesign-v2.html`) 승인이 이미 끝났으므로 바로 진행한다.

---

## 8. 테스트 관점

### 8.1 EditMode (test-engineer)

| 대상 | 검증 |
|---|---|
| `WorldMapChapterPalette.NodeOpenTint` | 챕터 1/2/3 이 §5 표의 값과 허용 오차 0.002 이내로 일치. 챕터 0·4·5 는 `Accent` 가 중립색 `#2c3750` 이므로 `Lerp(중립색, 흰색, 0.30)` 과 같음. 알파 1 |
| `NodeGlow` | rgb 는 `Accent` 와 동일, 알파 0.45. 챕터 4 이상도 알파 0.45 |
| `Silhouette` | 챕터 1/2/3 의 rgb = `MapBackground` × 0.30 (허용 오차 0.0001), 알파 0.80. 챕터 4 이상은 `NeutralMapBackground` × 0.30 |
| 기존 `Accent`·`MapBackground`·`BannerTint` | 기존 `WorldMapRedesignTests` 가 무수정 통과(회귀) |
| `UILoopMotion` 곡선 순수 함수 | `Drift`: t=0 → 기준, t=편도 시간 → 기준+진폭, t=2×편도 → 기준. `Rotate`: t=8초 → −90°. `Nudge`: t=0.49초 → 기준, t=0.51초 → 기준+진폭, t=1.0초 → 기준. `Pulse`: t=0 → 알파 0.67·스케일 1.00, t=1초 → 알파 1.00·스케일 1.15, t=2초 → 시작값 복귀 |
| 스프라이트 임포트 설정 | `AssetImporter` 로 §3 의 23개 파일이 존재하고 Texture Type = Sprite, 9-slice border 가 표와 일치(#1·#5·#6·#15·#17), `WorldMapPathDot` 의 wrapMode = Repeat·filterMode = Point, `WorldMapChipGem` 의 filterMode = Point, 나머지 Wrap Clamp |
| `WorldMapPathDash.png` 삭제 | 파일이 존재하지 않음(고아 에셋 재발 방지) |
| 프리팹 추가 연결 | `WorldMapPopup._silhouetteSprites` 길이 3·원소 전부 비어 있지 않음. 노드 프리팹 `_openSprite`/`_bossSprite`/`_clearedSprite`/`_lockedSprite` 가 각각 `WorldMapMedalOpen`/`Boss`/`Cleared`/`Locked` 를 가리킴. 탭 프리팹 `_chips` 5개가 `WorldMapChipGem` 사용 |
| 신규 `[SerializeField]` 연결 | `WorldMapStringsAndPrefabTests` 무수정 통과(§6.4) |
| 신규 문구 0건 | `Strings_Ko` 무결성 테스트 무수정 통과 |

### 8.2 PlayMode 불요

신규 로직이 색 조회·활성 토글·곡선 수치뿐이며 EditMode 순수 함수·프리팹 검사로 덮인다.

### 8.3 수동 확인 (사용자 육안, 팝업을 열어서)

- 챕터 1/2/3 선택 시 실루엣 형태·색, 열림 노드 색, 상세 초상 발광색이 챕터별로 달라지는지.
- `ClearedStage` 0 / 4 / 9 / 10 / 15 에서 노드 4상태(열림·클리어 ★·보스 맥동·잠금), 글로우 유무, 실선/점선 경계.
- 보스를 클리어하면 맥동이 멈추고 금색 글로우로 바뀌는지, 선택 링 회전과 ▼ 이 보스 선택 시 BOSS 태그와 겹치지 않는지(태그 오프셋은 선행서 §4.3 그대로).
- 규칙·보스 페이즈 긴 사례(2-5·3-4·3-5)에서 그라데이션 Image 가 블록 높이를 따라 늘어나는지, 섹션 구분선이 라벨 오른쪽 끝까지 채워지는지.
- 선택 버튼 활성/비활성 전환 시 글로우·광택이 같이 켜지고 꺼지는지.
- 중첩 Canvas(`MapArea`·`Rail`) 도입 후에도 노드·카드 클릭, 잠금 토스트, 팝업 닫기가 기존과 같이 동작하는지(GraphicRaycaster 누락 시 클릭이 안 먹는다).
- §4.3 의 프레임 비용 확인 후 폴백 여부 결정.

---

## 9. 목업 대비 의도적 차이

| 항목 | 목업 v2 | Unity 구현 | 사유 |
|---|---|---|---|
| 텍스트 하드 그림자(`text-shadow`) | 제목·지도 제목·노드 이름 등에 2px 검정 | 생략 (제목만 뒤 글로우 Image) | TMP 그림자는 폰트 머티리얼(공용 SDF 에셋) 수정이 필요해 다른 화면에 영향. 가독성은 명패·비네트가 보강 |
| 카드 배경 그라데이션·hover 이동 | 가로 그라데이션, hover 시 +3px | 기존 3 스프라이트 유지, hover 없음 | 선행서 §3 불변 + 지시 목록에 없음 |
| 칩 글로우 | 켜진 칩 금색 6px 발광 | 생략 | 켜짐/꺼짐이 색 틴트만 바뀌는 구조를 유지(코드 변경 0). 켜진 칩은 광택 그라데이션으로 구분 |
| 노드 이름 명패 폭 | 글자 폭에 맞춤(최대 노드 폭+60) | 항상 노드 폭+60 고정 | 자동 폭 맞춤은 레이아웃 컴포넌트 + 폭 상한 처리가 필요해 YAGNI. 명패는 반투명이라 어색함이 작음 |
| 연결선 형태 | 곡선 + 둥근 점선 | 직선 + 4x4 둥근 점 타일 | 선행서 §10 에서 직선 확정. 좌표·회전 불변 |
| 규칙·페이즈 띠 폭 | 5px | 4px 유지 | 선행서 §5 좌표 불변 |
| 초상 | 1.2배 확대 + 챕터색 drop-shadow | 96x96 유지 + 뒤 소프트 글로우 Image | 크기 불변, `drop-shadow` 대체 |
| 지도 장식 점 26개 | 있음 | 생략 | 선행서 §10 에서 생략 확정, 실루엣·안개가 대체 |
| 선택 버튼 | 가운데 분할 그라데이션 + 안쪽 하이라이트선 + 글자 간격 2px | 기존 버튼 스프라이트 + 글로우·광택 Image, 글자 간격 변경 없음 | 버튼 스프라이트는 CHButton 공용, 글자 간격은 TMP 설정이라 다른 버튼과 일관성 유지 |
| 닫기 X 버튼 | 그라데이션 | 기존 유지 | 지시 목록에 없음 |
| 패널 바닥 그림자 | `0 10px 0` + `0 0 60px` | `Shadow`(0,−10) 하나 | 60px 번짐은 dim 이 대체 |
| 모서리 장식 TR | X 버튼과 겹침 | X 버튼이 위에서 덮음 | 목업과 같은 겹침, 동작 영향 없음 |

---

## 10. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음. `UILoopMotion` 내부 움직임 종류 Enum 은 단일 시스템 내부용이라 해당 파일 안에 둔다(Rule 02 §8).
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 스프라이트 23종은 프리팹 직접 참조. 삭제: `WorldMapPathDash.png`.
- **SO / JSON 스키마**: 신규 없음. `Strings_Ko` 신규 id 없음.
- **코드 변경 목록**:
  - `WorldMapChapterPalette`: `NodeOpenTint(int)` · `NodeGlow(int)` · `Silhouette(int)` + 상수 4종(`NodeOpenLightness` 0.30, `NodeGlowAlpha` 0.45, `SilhouetteDarken` 0.30, `SilhouetteAlpha` 0.80) — 정의 §5.
  - `UILoopMotion` 신규 — §4.1·4.2.
  - `WorldMapPopup`: 신규 필드 4종, `Refresh()` 추가 동작 — §6.2·6.3.
  - `WorldMapChapterTabView`: 신규 필드 `_selectedDecor`, `Show` 추가 동작 — §6.3.
  - `WorldMapNodeView`: 신규 필드 3종, 신규 의도 API `SetChapter(int chapterId)`, `Show` 확장 — §6.2·6.3.
- **프리팹**: §7 표. 시그니처·필드명은 테스트와 프리팹 와이어링 기준이므로 글자 그대로 쓴다. 클래스 내부 구조·메서드 분할은 gameplay-programmer 판단.
- **일회용 도구**: `WorldMapPolishSpriteGenerator.cs` — PNG 생성·임포트 설정 후 삭제(Rule 04 §3). 프리팹 편집용 에디터 스크립트를 쓰면 마찬가지로 삭제.
