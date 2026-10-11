# 침입 지도 팝업(WorldMapPopup) 리디자인

## § 헤더

- **목표**: 침입 지도 팝업을 "상단 가로 탭 + 좌 지도 / 우 상세" 에서 "좌 챕터 카드 레일 / 중앙 챕터 풍경 지도 / 우 상세 카드 / 하단 바" 3단 구성으로 재배치한다. 기능은 그대로이고 표시만 바뀐다.
- **검증 가설**: 챕터별 진행도(클리어 칩 5칸)와 챕터 색조가 한눈에 보이면, 플레이어가 "지금 어느 챕터의 몇 번째 스테이지를 도전 중인지" 를 탭 전환 없이 파악하는가. (정량 메트릭 없음 — 사용자 육안 확인용 UI 개선)
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 챕터 구조 허용 항목 ④ "챕터 월드맵 팝업(마을 HUD 안, HeroSelectPopup 확장)" 의 표시 개선이다. Ch4~Ch5 는 기존과 같이 "???" 카드로만 노출하며 신규 챕터·규칙·콘텐츠 추가 없음.
- **핵심 메커니즘**: (1) 챕터 탭 5개를 200x84 세로 카드로 바꾸고 번호·챕터명·영웅 직업·진행 칩 5칸을 표시한다. (2) 지도에 챕터 색조 배경과 지그재그 노드, 클리어 구간 금색 실선 / 미개척 구간 점선 연결선을 둔다. (3) 상세를 "초상 배너 → 번호·이름·별 → 내 기록 → 규칙 → 보스 페이즈" 카드 흐름으로 재배치하고, 하단 바 버튼을 상세 열 폭에 맞춘다.

승인된 목업: `.mockups/worldmap-redesign.html` (1280x720). 본 문서의 좌표는 목업 수치를 옮기되 §10 의 1건(하단 바 겹침)만 보정했다.

---

## 1. 불변 사항

- `WorldMapViewModel` 의 동작: 탭/노드 선택, 잠금·준비 중 토스트(`OnNotice`), dim/X 닫기, 확정(`Confirm`) 후 닫기(`OnConfirmed`), 초기 선택 보정 — 전부 변경 없음.
- 기존 `WorldMapViewModel` 공개 멤버, `WorldMapTabData`·`WorldMapNodeData` 기존 필드, `WorldMapPopup`·`WorldMapChapterTabView`·`WorldMapNodeView` 의 기존 `[SerializeField]` 필드는 이름·타입 그대로 유지한다.
- **신규 Strings_Ko 문구 0건.** 신규 문구처럼 보이는 표시는 모두 기존 문구 재사용이다.
  - 카드의 영웅 직업 = 기존 `hero.nameKey` 문구(1021 기사 / 1022 궁수 / 1023 암살자).
  - 카드의 챕터명 = 기존 `chapter.nameKey` 문구.
  - "내 기록" · "스테이지 규칙" · "보스 페이즈" · "침입 지도" · 태그라인 · "BOSS" · "잠금" 은 기존 프리팹의 정적 라벨을 재사용한다.
  - 진행 칩은 문구 없는 도형이다.
- **새 JSON 스키마 0건**, 새 SO 0건. 챕터 색조는 View 상수다.

## 2. 화면 구성 (좌표는 팝업 루트 1280x720 기준, 원점 좌상단, y 아래로 증가)

| 영역 | 좌표 (x, y) | 크기 | 비고 |
|---|---|---|---|
| dim | (0, 0) | 1280x720 | 기존 `_dimButton`, 알파 0.55 |
| 팝업 패널 | (72, 40) | 1136x640 | 기존 `Panel` |
| 헤더 | (72, 40) | 1136x64 | 어두운 띠(`#0d1119`), 하단 3px 테두리 |
| 제목 "침입 지도" | (96, 52) | 300x40 | 글자 30, 색 `#e8e1cf` |
| 태그라인 | (282, 64) | 600x20 | 글자 13, 색 `#8e98ad` |
| 닫기 X | (1144, 48) | 48x48 | 기존 `_closeButton` |
| 챕터 카드 레일 | (88, 120) | 200x472 | 카드 5장 세로 배치 (§3) |
| 지도 | (304, 120) | 520x472 | 챕터 색조 배경 (§4) |
| 상세 카드 | (840, 120) | 352x472 | §5 |
| 하단 안내 문구 `_bottomText` | (96, 604) | 700x56 | 글자 13, 줄 높이 56, 단일 줄, 색 `#8e98ad` |
| 선택 버튼 `_selectButton` | (840, 604) | 352x56 | 글자 20, 상세 열 폭과 같은 x·폭 |

검산: 버튼 하단 = 604 + 56 = 660 ≤ 패널 하단 680, 여백 20. 레일 5장 높이 = 5×84 + 4×8 = 452 ≤ 472.

## 3. 챕터 카드 레일

카드 5장: x = 88, y = 120 + k×92 (k = 0..4 → 120 / 212 / 304 / 396 / 488), 크기 200x84, 간격 8. 카드 슬롯 i 는 기존과 같이 `_tabs[i]` ↔ 챕터 id = i+1.

카드 내부 (카드 좌상단 기준):

| 위젯 | 위치 | 크기 | 내용·스타일 |
|---|---|---|---|
| 번호 배지 `_numberBadge`(Image) + `_numberText`(CHText) | (0, 0) | 52x84 | 챕터 id 숫자, 글자 30, 흰색, 우측 3px 테두리. 배지색은 §4.1 챕터 Accent |
| 챕터명 `_labelText`(기존) | (62, 10) | 132x20 | 글자 15. 개방: `#e8e1cf` / 잠금·준비 중: `#6c7385` / 미출시 "???": `#4a5266` |
| 영웅 직업 `_heroText`(CHText) | (62, 32) | 132x16 | 글자 12, 개방 `#8e98ad` / 잠금 `#6c7385` / 미출시는 숨김 |
| 진행 칩 `_chips[5]`(Image) + 루트 `_chipsRoot` | (62, 66) | 칩 14x8, 칩 간격 4 | 폭 = 5×14 + 4×4 = 86. 켜짐 `#f7c64a` / 꺼짐 `#0c1017`(테두리 1px `#07090e`) |
| 잠금 태그 `_lockTagText`(기존) | 우하단 (right 8, bottom 8) | 가변 | 글자 11, 색 `#8e98ad`. "잠금" / "준비 중" |

상태별 표시:

| 상태 | 조건 | 카드 배경 스프라이트 | 칩 | 번호 배지색 | 클릭 |
|---|---|---|---|---|---|
| 선택됨 | `IsSelected` | 기존 `_selectedSprite` (금색 외곽) | 표시 | 챕터 Accent | 활성 |
| 개방 | 선택 아님, `IsLocked == false` | 기존 `_normalSprite` | 표시 | 챕터 Accent | 활성 |
| 잠금 / 준비 중 | `IsLocked && IsPlaceholder == false` | 기존 `_lockedSprite` | 숨김 | `#1b1f27`, 숫자 `#59617a` | 활성 (토스트 안내) |
| 미출시 "???" | `IsPlaceholder` | 기존 `_lockedSprite` | 숨김 | `#171b25`, 숫자 `#3c4458` | 비활성 (기존 동작) |

- 미출시 카드는 숫자 배지 + 챕터명 자리에 "{N}장 ???" 만 표시한다 (영웅·태그 없음, 챕터명 y 를 30 으로 내림). 정보 비노출 규칙은 기존과 동일.
- 칩 표시 조건은 `IsLocked == false` 일 때만이다. 3장 출격 게이트가 닫혀 "준비 중" 이면 `ClearedCount` 값이 있어도 칩은 숨긴다.
- 카드 배경 스프라이트가 9-슬라이스 border 를 갖지 않으면 Image Type 은 Simple 로 두고 gameplay-programmer 가 보고한다 (스프라이트 신규 제작 없음).

## 4. 지도

지도 영역 (304, 120) 520x472. 지도 로컬 좌표는 좌상단 원점, y 아래로 증가. 렌더 순서(아래→위): 배경 → 연결선 → 노드 → 제목 오버레이.

### 4.1 챕터 색조 팔레트 (View 상수, 단일 진실)

챕터 id → 색 표. 챕터 카드 번호 배지, 지도 배경, 상세 배너가 같은 표를 쓴다. UI 폴더에 View 전용 정적 팔레트 한 곳을 두고 `WorldMapChapterTabView` 와 `WorldMapPopup` 이 함께 참조한다. 파일·클래스명은 gameplay-programmer 가 정한다.

| 챕터 id | Accent (배지·배너 틴트) | 지도 배경 단색 | 근거 |
|---|---|---|---|
| 1 | `#4a6fa5` | `#141b2c` | 목업 bg 그라데이션 `#1a2438`~`#0d1220` 의 중간값 |
| 2 | `#2f8a5f` | `#0e2216` | `#14301f`~`#08140d` 의 중간값 |
| 3 | `#7a55c8` | `#1b112d` | `#271a42`~`#0e0818` 의 중간값 |
| 그 외(4 이상) | `#2c3750` | `#0d1119` | 미출시 슬롯용 중립색 (실제 지도 표시 불가라 안전망) |

**대안 비교** (지도 색조를 어디에 두는가):

| 안 | 장점 | 단점 |
|---|---|---|
| A. View 상수 팔레트 (채택) | 스키마 0건, 지시 범위와 일치, 3곳이 한 표 공유 | 챕터 추가 시 코드 수정 |
| B. `Chapter.json` 에 색 컬럼 추가 | 데이터 주도 | 새 스키마 필드 필요 — 이번 지시 범위 밖 |
| C. 프리팹에 챕터별 Color 배열 `[SerializeField]` | 인스펙터 튜닝 | 3곳(카드·지도·배너)이 각자 배열을 들어 이중 진실 |

권장 A. Ch4~Ch5 출시 시점에 B 로 승격을 검토한다 (현재 범위 밖).

### 4.2 배경·제목

- 지도 배경 `_mapBackground`(Image, 신규): 지도 영역 전체를 덮는 단색, 색 = 팔레트 "지도 배경 단색"[선택 챕터]. 목업의 방사형 그라데이션·비네트·장식 점 26개는 Unity 구현에서 생략한다 (§10).
- 기존 `_chapterTitleText`: 지도 로컬 (16, 10) 380x28, 글자 22, 색 `#e8e1cf`. 기존 `_chapterThemeText`: (16, 40) 380x20, 글자 13, 색 `#8e98ad`. 텍스트 값은 기존 VM 값 그대로 ("{N}장 {챕터명}" / "테마: … / 영웅: …").
- 기존 `MapArea` 안의 정적 위치였던 두 텍스트를 위 좌표로 옮길 뿐이며, 더는 지도 위쪽 제목 줄 영역을 차지하지 않는다.

### 4.3 노드 배치 (지그재그)

노드 중심 좌표 (지도 로컬). 노드 i 는 기존과 같이 `_nodes[i-1]`.

| 노드 | 중심 (x, y) | 크기 | 좌상단 (x, y) | 검산 |
|---|---|---|---|---|
| 1 | (70, 390) | 64x64 | (38, 358) | 70−32, 390−32 |
| 2 | (190, 330) | 64x64 | (158, 298) | 190−32, 330−32 |
| 3 | (110, 240) | 64x64 | (78, 208) | 110−32, 240−32 |
| 4 | (270, 220) | 64x64 | (238, 188) | 270−32, 220−32 |
| 5 (보스) | (400, 140) | 88x88 | (356, 96) | 400−44, 140−44 |

- 노드 스프라이트(해금/보스/클리어/잠금)·`_labelText`·`_nameText`·`_clearedTagText`·`_bossTag`·`_selectedMarker` 는 기존 구조 유지. 노드 5 인스턴스만 크기 88x88, 라벨 글자 24 (나머지 노드 글자 20, 잠금 노드 13).
- 스테이지명(`_nameText`): 노드 아래 4px, 폭 = 노드 폭 + 60 (노드 좌우 30px 확장), 글자 12, 가운데 정렬, 하단 그림자.
- "클리어" 태그(`_clearedTagText`): 스테이지명 아래 +16px (노드 하단 + 20), 금색.
- 선택 표시(`_selectedMarker`): 금색 `#f7c64a` 4px 외곽선(노드 가장자리에서 4px 띄움) + 노드 위 "▼" 마커 (노드 상단 −26px, 글자 16, 금색). 목업의 회전 점선 프레임과 ▼ 상하 흔들림 애니메이션은 생략한다 (정적, §10).
- BOSS 태그(`_bossTag`): 노드 상단 −20px, 글자 12, 금색. 보스 노드가 선택된 상태(`IsSelected`)에서는 ▼ 마커와 겹치지 않도록 −44px 로 올린다 — 이 오프셋 전환은 `WorldMapNodeView.Show` 안에서 `data.IsSelected` 로 처리한다.
- 노드 최하단 검산: 노드 1 하단 358+64 = 422, 클리어 태그 하단 422+20+16 = 458 ≤ 472.

### 4.4 연결선 (구간 4개)

노드 k 와 k+1 을 잇는 직선 구간 4개. 목업의 곡선은 Unity 에서 직선으로 단순화한다 (§10). 각 구간은 중심 피벗 RectTransform 이며 노드보다 아래 렌더된다 (노드가 선 끝을 덮음).

| 구간 | 연결 | 중심 (x, y) | 길이 | 회전 z (도, 반시계 +) | 산식 |
|---|---|---|---|---|---|
| 1 | 노드1→2 | (130, 360) | 134.2 | +26.6 | dx 120, dy(위) 60: √18000, atan(60/120) |
| 2 | 노드2→3 | (150, 285) | 120.4 | +131.6 | dx −80, dy(위) 90: √14500, 180−atan(90/80) |
| 3 | 노드3→4 | (190, 230) | 161.2 | +7.1 | dx 160, dy(위) 20: √26000, atan(20/160) |
| 4 | 노드4→5 | (335, 180) | 152.6 | +31.6 | dx 130, dy(위) 80: √23300, atan(80/130) |

구간마다 Image 2장을 같은 위치에 겹쳐 둔다.

| 위젯 | 크기(높이) | 색 | 스프라이트 | 표시 조건 |
|---|---|---|---|---|
| `_pathSolid[k]` (실선) | 길이 × 5 | `#f7c64a` | 없음(단색) | 노드 k 의 `NextPathCleared == true` |
| `_pathDashed[k]` (점선) | 길이 × 4 | `#ffffff` 알파 0.25 | 신규 `WorldMapPathDash` (Image Type Tiled) | 그 외 |

- 신규 스프라이트 `Assets/_Lair/Art/Sprites/WorldMapPathDash.png`: 16x4 px, 왼쪽 8px 불투명 흰색 + 오른쪽 8px 투명(목업의 `8 8` 대시 패턴), Filter Point, Wrap Mode Repeat. 프리팹의 참조 필드로만 쓰이므로 Addressables 키·Enum 은 없다.
- 선택 챕터가 바뀌면 지도 전체(배경색·구간 표시·노드)가 `OnChanged` 로 한 번에 갱신된다. 잠금 챕터는 선택 불가라 지도에 노출되지 않는다.

## 5. 상세 카드

상세 패널 (840, 120) 352x472. 이하 좌표는 패널 좌상단 기준.

| 순서 | 위젯 (기존 필드 → 신규) | 위치 | 크기 | 스타일 |
|---|---|---|---|---|
| 1 | 초상 배너 `PortraitFrame`(기존) + `_bannerTint`(Image 신규) | (12, 12) | 328x120 | 바탕 `#0c1017`, 그 위 `_bannerTint` 색 = 챕터 Accent 알파 0.53. RectMask2D 로 자식 클리핑 |
| 1a | `_detailPortrait`(기존) | 배너 하단 중앙 | 96x96, Preserve Aspect | `_portraitOf` 스프라이트 없으면 숨김 (기존) |
| 1b | `_detailNumberText`(기존) | 배너 (12, 8) | 80x20 | 글자 15, 금색 `#f7c64a`, 그림자 |
| 1c | `_detailHeroText`(기존) | 배너 우하단 (right 10, bottom 6) | 160x16 | 글자 13, `#e8e1cf` ("영웅: …" 기존 문구) |
| 1d | `_detailBossBadge`(기존) | 배너 좌하단 (12, bottom 6) | 56x20 | 배경 `#7a2a31`, "BOSS" 글자 12 (기존 정적 라벨) |
| 1e | `_portraitLockOverlay`(기존) | 배너 전체 | 328x120 | 검정 알파 0.67 + "잠금" 글자 18 (기존) |
| 2 | `_detailNameText`(기존) | (16, 140) | 232x32 | 글자 21, `#e8e1cf` |
| 2 | `_detailStarsText`(기존) | (242, 144) | 96x24 | 글자 16, 금색, 우측 정렬 |
| 3 | 기록 박스 `RecordPanel`(Image 신규, 단색 `#0d1119`) | (12, 180) | 328x48 | 안에 `RecordLabel`(기존 "내 기록", (24, 186), 글자 11, `#8e98ad`) + `_detailRecordText`(기존, (24, 202), 304x22, 글자 15) |
| 4 | `RulesLabel`(기존 "스테이지 규칙") | (12, 236) | 328x16 | 글자 12, `#8e98ad` |
| 4 | `_detailRulesText`(기존) + 좌측 띠 `_rulesBar`(Image 신규) | (12, 256) 폭 328 | 높이는 텍스트에 맞춤 | 바탕 `#0d1119`, 띠 폭 4 색 `#5aa9ff`, 텍스트 좌 패딩 12, 글자 12, 줄 간격 16 |
| 5 | `_bossPhaseRoot`(기존) = 라벨 "보스 페이즈"(기존) + `_bossPhaseText`(기존) + 좌측 띠 `_phaseBar`(Image 신규) | 규칙 블록 바로 아래, 간격 8 | 폭 328 | 라벨 글자 12 `#e5484d`, 본문 블록 바탕 `#0d1119`, 띠 폭 4 색 `#e5484d`, 글자 12 |

- 규칙·보스 페이즈는 VM 이 문자열 하나로 합쳐 주므로(`DetailRulesText`, `DetailBossPhaseText`) 목업의 "규칙 1개당 띠 1개" 대신 **블록당 띠 1개** 로 단순화한다 (VM 불변 우선, §10).
- 규칙·페이즈 블록 배치는 수직 레이아웃(`VerticalLayoutGroup` + 높이 맞춤)로 규칙 블록 높이에 따라 페이즈 블록이 따라 내려오게 한다. 영역은 (12, 256)~(340, 464), 높이 208.
- 텍스트 오버플로 방지: 규칙·페이즈 CHText 는 Auto Size 최소 10 ~ 최대 12 로 둔다. 가장 긴 사례(2-5 사냥꾼 우두머리: 페이즈 2건, 3-5 자객 수장: 페이즈 2건, 3-4 봉인된 서고: 규칙 2건)가 영역 208 안에 들어오는지 육안 확인한다 (§9 체크).
- 잠금 스테이지(`DetailLocked`): 이름 "???", 기록 "-", 배너에 잠금 오버레이 — VM 값 그대로이며 별도 분기 없음.
- 배너 틴트색은 선택 챕터(`_vm.SelectedChapter`)의 Accent.

## 6. ViewModel 변경점

기존 멤버는 모두 유지하고 아래만 추가한다. 신규 문구 id 상수 추가 없음.

| 대상 | 추가 필드 | 값 정의 |
|---|---|---|
| `WorldMapTabData` | `string ChapterNameText` | 개방 챕터: `_text(chapter.nameKey)` (예: "그림자 성채"). 미출시 슬롯: 기존 `Label` 과 같은 값("{N}장 ???") |
| `WorldMapTabData` | `string HeroText` | 개방 챕터: `_text(hero.nameKey)` (예: "암살자"), 영웅 row 없으면 빈 문자열. 미출시 슬롯: 빈 문자열 |
| `WorldMapTabData` | `int ClearedCount` | 챕터 안 클리어 수 0~5. 개방·잠금 챕터는 `StageProgress.ClearedCountInChapter(slot, ClearedStage)`, 미출시 슬롯은 0 |
| `WorldMapNodeData` | `bool NextPathCleared` | `State == Cleared && StageIndex < StageProgress.StagesPerChapter`. 이 노드에서 다음 노드로 가는 구간이 금색 실선인지 |

- 기존 `Label`("1장 지하 묘지") 은 그대로 유지한다. 카드 View 는 `ChapterNameText` 를, 기존 테스트는 `Label` 을 본다.
- `ClearedCount` 산식은 해금 판정(`IsUnlocked` · `IsChapterUnlocked`)을 단일 소유하는 `StageProgress` 에 순수 정적 헬퍼로 둔다.
  - 시그니처: `int ClearedCountInChapter(int chapterId, int clearedStage)`
  - 정의: `Max(0, Min(StagesPerChapter, clearedStage − (chapterId − 1) × StagesPerChapter))`
  - 3장 출격 게이트(`ChapterGate`)와 무관하게 값만 계산한다 (표시 여부는 View 가 `IsLocked` 로 결정).
- **대안 비교** (클리어 수를 어디서 계산하는가):

| 안 | 장점 | 단점 |
|---|---|---|
| A. `StageProgress` 헬퍼 + VM 이 `WorldMapTabData` 에 채움 (채택) | 순수 함수라 경계 테스트 쉬움, 진행 산식 단일 소유, View 로직 없음 | VM 필드 1개 추가 |
| B. View 가 `ClearedStage` 로 직접 계산 | VM 불변 | View 에 비즈니스 로직 (Rule 02 §6 위반) |
| C. `MetaProfile` 에 챕터별 클리어 수 저장 | 조회 빠름 | 파생값을 저장해 이중 진실, 세이브 마이그레이션 필요 |

## 7. View 변경점

### 7.1 `WorldMapChapterTabView` (카드)

- 기존 `[SerializeField]` 유지: `_button` `_background` `_labelText` `_lockTagText` `_selectedSprite` `_normalSprite` `_lockedSprite`.
- 신규 `[SerializeField] private`: `_numberBadge`(Image), `_numberText`(CHText), `_heroText`(CHText), `_chipsRoot`(GameObject), `_chips`(Image[5]).
- `Show(WorldMapTabData)` 추가 동작: `_labelText` 에 `ChapterNameText` 표시, `_numberText` 에 `ChapterId` 숫자, `_numberBadge` 색(§3 표), `_heroText` 에 `HeroText` (빈 문자열이면 숨김), `_chipsRoot` 는 `IsLocked == false` 일 때만 표시하고 `_chips[i]` 는 `i < ClearedCount` 이면 켜짐색 아니면 꺼짐색.
- 기존 동작 유지: 배경 스프라이트 3종 스왑, 잠금 태그 표시, 미출시 슬롯 버튼 비활성.
- 챕터 색은 §4.1 팔레트 조회. 의도 API 만 노출(Rule 02 §6.1) — 새 public 메서드 추가 없음 (`Show` 가 전부).

### 7.2 `WorldMapNodeView`

- 필드 변경 없음. `_bossTag` 오프셋 전환(§4.3)만 추가.

### 7.3 `WorldMapPopup`

- 기존 `[SerializeField]` 전부 유지.
- 신규 `[SerializeField] private`: `_mapBackground`(Image), `_pathSolid`(Image[4]), `_pathDashed`(Image[4]), `_bannerTint`(Image).
- `_rulesBar` · `_phaseBar` · `RecordPanel` 은 색이 고정인 장식 Image 이므로 코드 참조 없이 프리팹에만 둔다.
- `Refresh()` 추가 동작: `_mapBackground.color` ← 팔레트(`_vm.SelectedChapter`) 지도 배경색, `_bannerTint.color` ← Accent 알파 0.53, 구간 k(0..3)마다 `_pathSolid[k]` 활성 = `_vm.Nodes[k].NextPathCleared`, `_pathDashed[k]` 활성 = 그 반대.
- `BindInputs`·`HandleNotice`·`HandleConfirmed`·`InitUI` 는 변경 없음.

## 8. 프리팹 변경 범위

| 프리팹 | 변경 |
|---|---|
| `Assets/_Lair/Art/UI/WorldMapChapterTab.prefab` | 루트 크기 200x84 로 변경. 기존 위젯 위치를 §3 표대로 재배치. 신규: `Number`(배지 Image + CHText), `Hero`(CHText), `Chips`(루트 + Image 5장). 기존 `_labelText` · `_lockTagText` · 스프라이트 3종 참조 유지 |
| `Assets/_Lair/Art/UI/WorldMapNode.prefab` | 구조 변경 없음. `_bossTag`·`_selectedMarker` 하위 위치만 §4.3 수치에 맞춤 |
| `Assets/_Lair/Art/UI/WorldMapPopup.prefab` | 기존 위젯 위치 재배치(§2·§4·§5). 신규: `MapBackground`, `Paths`(구간 4개 × 실선·점선 2장), `BannerTint`, `RecordPanel`, `RulesBar`, `PhaseBar`. `_tabs[5]` 카드 5장을 레일에 세로 배치, `_nodes[5]` 를 §4.3 좌표에 배치, 노드 5 만 88x88. 신규 필드 4종 와이어링 |
| `Assets/_Lair/Art/Sprites/WorldMapPathDash.png` | 신규 스프라이트 1개 (§4.4) |

- Addressables 주소(`WorldMapPopup` 등)·Enum 값(`EUI`)은 변경 없다. 프리팹 파일명 불변.
- 기존 `WorldMapStringsAndPrefabTests` 가 "모든 `[SerializeField]` 참조가 연결돼 있는가" 를 `_tabs[i]`·`_nodes[i]`·탭/노드 프리팹까지 검사하므로, **신규 필드(배열 원소 포함)는 하나도 비워 두지 않는다.**
- 프리팹 생성에 일회용 에디터 빌더를 쓰면 생성 직후 삭제한다 (Rule 04 §3). 모든 TMP 텍스트에는 CHText 를 함께 붙인다 (Rule 03 §3).
- `Instantiate`/`new GameObject` 로 칩·구간을 코드에서 동적 생성하지 않는다. 전부 프리팹에 정적 배치하고 인스펙터로 연결한다.

## 9. 테스트 관점

### 9.1 EditMode (test-engineer)

대상: `StageProgress.ClearedCountInChapter` 와 `WorldMapViewModel.Tabs`/`Nodes` (기존 `WorldMapTestSupport` 로 챕터 3행 데이터 구성).

**클리어 수 계산 경계** — `ClearedStage` 별 챕터 1/2/3 의 `ClearedCount` 기대값:

| ClearedStage | 챕터1 | 챕터2 | 챕터3 | 경계 의미 |
|---|---|---|---|---|
| 0 | 0 | 0 | 0 | 신규 |
| 4 | 4 | 0 | 0 | 1장 보스 직전 |
| 9 | 5 | 4 | 0 | 2장 보스 직전 |
| 10 | 5 | 5 | 0 | 2장 보스 클리어 = 3장 해금 직전 (3장 칩은 0) |
| 15 | 5 | 5 | 5 | 전부 클리어 |
| 5 | 5 | 0 | 0 | 챕터 경계 정확히 일치 |
| 14 | 5 | 5 | 4 | 마지막 챕터 보스 직전 |
| −1 (방어) | 0 | 0 | 0 | 하한 클램프 |

추가 케이스:
- 미출시 슬롯(챕터 4·5) `ClearedCount == 0`, `HeroText` 빈 문자열, `ChapterNameText == Label`.
- `ChapterGate` 3장 닫힘 + `ClearedStage` 15 이어도 3장 탭 `ClearedCount == 5`, `IsLocked == true` (값 계산은 게이트와 독립).
- 기존 `Label` 값 불변 ("1장 지하 묘지").
- `NextPathCleared`: `ClearedStage` 3 일 때 1장 노드 1·2·3 = true, 4·5 = false / `ClearedStage` 10 일 때 1장 노드 1~4 = true, 노드 5 = false (보스는 다음 구간 없음) / `ClearedStage` 0 일 때 전부 false / 잠금 챕터(2장을 선택할 수 없는 상태) 노드는 `Locked` 이므로 false.
- 기존 `WorldMapViewModelTests`·`WorldMapViewModelBehaviorTests` 전부 무수정 통과 (회귀).
- `WorldMapStringsAndPrefabTests` 무수정 통과 — 신규 필드 미연결 시 이 테스트가 잡는다. Strings_Ko 무결성 테스트도 그대로 통과(문구 id 추가 없음).

### 9.2 수동 확인 (사용자 육안, 팝업 열어서)

- `ClearedStage` 0 / 9 / 10 / 15 × 3장 게이트 열림·닫힘 조합에서 카드 5장 상태(선택·개방·잠금·준비 중·???), 칩 칸 수.
- 챕터 1·2·3 선택 시 지도 배경색·상세 배너색 전환, 연결선 실선/점선 경계.
- 2-5 · 3-4 · 3-5 선택 시 상세의 규칙·페이즈 텍스트가 영역 208 안에 들어오는지.
- 잠금 노드/탭 클릭 토스트, dim/X 닫기, 선택 버튼 확정 후 닫기가 기존과 같은지.

## 10. 목업 대비 의도적 차이

| 항목 | 목업 | Unity 구현 | 사유 |
|---|---|---|---|
| 하단 바 y | 안내 문구 y=576, 버튼 y=572 (지도·상세 하단 592 와 최대 20px 겹침) | 문구·버튼 y=604, 높이 56 | 겹침 해소. 패널 하단 680 안 (604+56=660). **목업 파일은 수정하지 않았다** — 메인이 필요하면 동기화 |
| 연결선 | 곡선 | 직선 4개 | 곡선은 스프라이트 신규 제작 필요, YAGNI |
| 지도 배경 | 방사형 그라데이션 + 비네트 + 장식 점 | 챕터 단색 | 그라데이션 스프라이트·장식 Image 26장 생략 (챕터 색조 구분은 단색으로 충분) |
| 선택 노드 | 회전 점선 프레임 + ▼ 흔들림 | 금색 외곽선 + 정적 ▼ | 애니메이션 YAGNI |
| 노드 열림색 | 챕터 색 | 기존 노드 스프라이트 그대로 | 지시 범위(배경 색조)에 노드 색조 없음 |
| 규칙 띠 | 규칙 1개당 1개 | 블록당 1개 | VM 이 문자열 하나로 제공, VM 불변 |

## 11. 구현 요청사항 (gameplay-programmer)

- **Enum**: 추가 없음. (`EUI.WorldMapPopup` 등 기존 값 그대로, 신규 에셋 로드 키 없음)
- **Interface**: 추가 없음.
- **에셋 키**: 신규 Addressable 키 없음. 신규 스프라이트 `WorldMapPathDash.png` 는 프리팹 직접 참조(로드 키 불필요).
- **SO / JSON 스키마**: 신규 없음. Strings_Ko 신규 id 없음.
- **코드 변경 목록**:
  - `StageProgress`: `int ClearedCountInChapter(int chapterId, int clearedStage)` 정적 헬퍼 (정의 §6).
  - `WorldMapTabData`: `ChapterNameText`, `HeroText`, `ClearedCount` 추가. `WorldMapNodeData`: `NextPathCleared` 추가. `WorldMapViewModel.RebuildTabs`/`RebuildNodes` 가 값을 채운다.
  - 챕터 팔레트 View 정적 표 (§4.1) 1곳.
  - `WorldMapChapterTabView` 신규 필드 5종·`Show` 확장 (§7.1), `WorldMapNodeView` `_bossTag` 오프셋 (§4.3), `WorldMapPopup` 신규 필드 4종·`Refresh` 확장 (§7.3).
- **프리팹**: §8 표 그대로. UI 배선 실행(UnityMCP·빌더)은 이 목업(`.mockups/worldmap-redesign.html`) 승인이 이미 끝난 뒤이므로 바로 진행 가능하다.
- 코드 구조(클래스 분할·메서드 시그니처 세부)는 gameplay-programmer 판단이며, 위 시그니처·필드명은 테스트와 프리팹 와이어링의 기준이므로 글자 그대로 쓴다.
