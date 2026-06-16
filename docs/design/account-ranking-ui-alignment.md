# 계정·랭킹 팝업 UI 정합 (공통 모달 디자인 통일)

> 작성: game-designer · 단계: v0.3 · 대상: `CloudPopup`(계정) · `RankingPopup`(랭킹)
> 참조(reference, 변경 대상 아님): ShopPopup · CodexPopup · QuestPopup · RecordsPopup · HeroSelectPopup · LordLevelPopup

---

## § 헤더

- **목표** — 계정(CloudPopup)·랭킹(RankingPopup) 두 팝업을 마을 메타 팝업들의 공통 모달 프레임(ModalBody · Dim · Title · CloseButton)에 맞춰 시각 구조만 통일한다. 기능 동작은 불변.
- **검증 가설** — "두 팝업이 다른 메타 팝업과 같은 프레임·딤·헤더를 쓰면, 서버 연동 화면(계정·랭킹)이 마을 UI의 일부로 자연스럽게 읽혀 재방문/경쟁 동기를 깨지 않는가." (v0.3 stage_goal 보조)
- **현재 단계 범위 적합성** — **범위 내**. v0.3 §8 "서버 연동(계정·리더보드) 클라이언트 UI" + "아트/에셋 작업 허용". 신규 영웅/몬스터/카드 리소스 제작 없음(기존 스프라이트·폰트만 재사용).
- **핵심 메커니즘** — ShopPopup/CodexPopup/RecordsPopup에서 추출한 공통 모달 템플릿(§1)을 두 팝업에 적용. 두 팝업 모두 현재 **루트 자체를 패널로** 쓰고 있어 Dim·ModalBody가 없다 → 루트를 전체화면 컨테이너로 바꾸고 그 아래 `Dim` + `ModalBody`를 형제로 추가, 기존 콘텐츠를 ModalBody로 재부모(re-parent)한다. GameObject 정체성은 유지되어 스크립트 `[SerializeField]` 와이어링은 보존된다.

---

## § 1. 공통 모달 디자인 템플릿 (canonical)

**추출 출처**: ShopPopup을 1차 기준으로, CodexPopup · RecordsPopup을 대조 확인. 아래 토큰은 **3개 팝업에서 동일**함을 프리팹 실측으로 검증했다(n=3).

### 1.1 계층 구조 (공통 골격)

```
{Popup}Root (RectTransform: anchors 0,0–1,1 stretch, 전체화면)   ← Image/Button 없음(투명 컨테이너)
├─ Dim          (전체 stretch · Image 단색 · CHButton = 딤 클릭 닫기)
└─ ModalBody    (중앙 고정 패널 · Image 9-slice + Shadow · CHButton 없음)
   ├─ Title         (좌상단 헤더 라벨 · CHText)
   ├─ CloseButton   (우상단 코너 X · Image + Button + CHButton, 자식 X=CHText)
   ├─ (콘텐츠 보조 라벨/요약줄 — 팝업별)
   └─ ScrollView    (Viewport → Content → LayoutGroup → Cell)  ← 콘텐츠 영역
```

### 1.2 공통 스펙 표 (실측값 — n=3 동일 확인)

| 요소 | 속성 | 값 (canonical) | 비고 |
|---|---|---|---|
| **Root** | RectTransform | AnchorMin (0,0) · AnchorMax (1,1) · sizeDelta (0,0) | 전체화면 stretch. Image/Graphic 없음(투명) |
| **Dim** | RectTransform | 전체 stretch (root와 동일) | ModalBody 뒤(형제 순서상 먼저) |
| | Image `m_Color` | **(0, 0, 0, 0.6)** | `m_Sprite: {fileID: 0}` (스프라이트 없음, 단색) · `m_Type: 0` |
| | Button + CHButton | TargetGraphic = Dim Image | 딤 클릭 시 팝업 닫기 (ShopPopup `_dimButton`) |
| **ModalBody** | RectTransform | AnchorMin/Max (0.5, 0.5) 중앙 · sizeDelta = 팝업별(§2.1·§3.1) | 화면 중앙 고정 |
| | Image `m_Color` | **(0.122, 0.161, 0.216, 0.98)** | 어두운 남보라 프레임. `m_Sprite: {fileID: 10907, guid: 0000000000000000f000000000000000}` (Unity 내장 sliced `UISprite`) · `m_Type: 1` (Sliced/9-slice) |
| | Shadow (`m_EffectColor`) | **(0.984, 0.749, 0.141, 1)** 금색 · `m_EffectDistance (2, -2)` | 프레임 외곽 금색 그림자. 컴포넌트 guid `e19747de3f5aca642ab2be37e372fb86` |
| **Title** | RectTransform | AnchorMin/Max (0, 1) 좌상단 · AnchoredPos **(16, -12)** · sizeDelta **(240, 26)** · Pivot (0, 1) | ModalBody 자식 |
| | CHText `m_fontSize` | **16** | 폰트 guid `12e8e80fec3a8554fa63474df537b505` (NotoSansKR SDF) |
| | CHText `m_fontColor` | (1, 1, 1, 1) 흰색 | `m_HorizontalAlignment: 1`(Left), `m_VerticalAlignment: 512`(Middle) |
| | CHText 컴포넌트 | guid `288ac871b0c72164b8da2b27def4d408` · `_stringID` = 화면별 string ID | ShopPopup `_stringID: 8`(상점) |
| | **헤더 아이콘** | **없음** | 참조 3개 팝업 모두 Title `m_Children: []` — 아이콘 자식 없음 (§4 결정) |
| **CloseButton** | RectTransform | AnchorMin/Max (1, 1) 우상단 · AnchoredPos **(-12, -12)** · sizeDelta **(28, 28)** · Pivot (1, 1) | ModalBody 자식 |
| | Image `m_Color` | **(0.3, 0.3, 0.3, 0.5)** | `m_Sprite: {fileID: 10907, guid: 0000…f000}` 내장 sliced · `m_Type: 0` |
| | Button + CHButton | TargetGraphic = CloseButton Image | CHButton guid `c00ea53ccddc06c49a6e7c4417b41001` |
| | 자식 `X` (CHText) | full stretch · text `×` · `m_fontSize: 18` · Center/Middle · 흰색 | 코너 X 글리프 |
| **ScrollView** | RectTransform | AnchorMin (0,0) · AnchorMax (1,1) stretch · AnchoredPos/​sizeDelta = 팝업별 인셋 | Viewport→Content→LayoutGroup→Cell 구조(Rule 03 §3) |
| | ScrollRect | `m_Horizontal: 0` · `m_Vertical: 1` (세로 스크롤) | |

> **폰트 단일 진실**: 모든 CHText/TMP_Text는 NotoSansKR SDF (`guid 12e8e80fec3a8554fa63474df537b505`, sharedMaterial `-2629464854542371455`). 두 정합 대상 팝업의 기존 텍스트도 이미 동일 폰트 사용 중 — 폰트 변경 없음.

> **신규 아트 0건 확인(v0.3 제약)**: ModalBody/Dim/CloseButton 프레임은 Unity 내장 sliced 스프라이트(`guid 0000…f000`) + 단색 + Shadow 컴포넌트만 사용. 새 스프라이트·머티리얼 제작 불필요.

---

## § 2. CloudPopup(계정) 정합안

### 2.1 현재 상태 (실측)

- 루트 GameObject `CloudPopup` 가 곧 패널 — AnchorMin/Max (0.5,0.5) 중앙 · sizeDelta **(620, 720)** · Image 색 **(0.12, 0.13, 0.16, 0.96)** (canonical과 미세 차이) · 루트 GO에 **Image + Button 존재**.
- **Dim 없음 · ModalBody 없음.** Title은 있음. 닫기 수단은 두 개다(실측):
  - **루트 GO Button** 이 `UIBase._backgroundButton` 에 **이미 와이어링됨** → 루트(패널 전체) 클릭 시 닫힘.
  - **하단 중앙 큰 "닫기" 버튼**(`CloseButton`, AnchorMin/Max (0.5,0) · AnchoredPos (0,40) · sizeDelta **(300, 60)**) 이 `UIBase._backButton` 슬롯에 연결 → 명시 닫기. 코너 X 패턴이 아님.
  - 따라서 딤 클릭 닫기는 "미동작"이 아니라 **현재는 패널 자체(루트) 클릭이 그 역할**을 하고 있다. 정합 후 루트가 투명 컨테이너가 되면 이 `_backgroundButton` 역할을 신규 Dim Button으로 이관한다(§6.6).
- 콘텐츠 요소가 전부 루트 직속 자식: `Title` · `DisplayNameText` · `ConnectionText` · `ChangeNameButton` · `NameEditGroup`(자식: NameInput · NameConfirmButton · NameCancelButton) · `RestoreButton` · `ConflictDot` · `ConflictGroup`(자식: ConflictText · ConflictRestoreButton · ConflictLaterButton) · `CloseButton`.

### 2.2 목표 구조 (정합 후)

```
CloudPopup (Root)  ← anchors 0,0–1,1 전체화면 stretch, Image 제거(투명 컨테이너)
├─ Dim                       ← 신규: 전체 stretch, Image (0,0,0,0.6), CHButton 딤클릭닫기
└─ ModalBody                 ← 신규: 중앙 (0.5,0.5), sizeDelta (620, 720), Image (0.122,0.161,0.216,0.98) + Shadow 금색
   ├─ Title                  ← 좌상단 (16,-12) 240×26, fontSize 16. text "계정" (string ID 37 — 단 §6.4 step6 선확인, 미존재 시 literal "계정" 유지)
   ├─ CloseButton            ← 신규 코너 X: 우상단 (-12,-12) 28×28 (기존 하단 "닫기" 대체)
   ├─ ConnectionText         ← 상단 stretch 상태 라벨 "연결됨/오프라인"
   ├─ ConflictDot            ← 우상단 빨간 dot 배지(충돌 대기 시)
   ├─ DisplayNameText        ← "표시명: ___" 본문
   ├─ ChangeNameButton       ← [변경] 버튼
   ├─ RestoreButton          ← [클라우드에서 복원] 버튼
   ├─ NameEditGroup          ← 이름 입력 영역(기본 숨김) : NameInput · NameConfirmButton · NameCancelButton
   └─ ConflictGroup          ← 충돌 권유 영역(배지 켜졌을 때만) : ConflictText · ConflictRestoreButton · ConflictLaterButton
```

### 2.3 요소 배치 명세 (ModalBody 콘텐츠 영역, 패널 620×720 기준)

좌표는 ModalBody 로컬. ModalBody가 현 패널 치수(620×720)를 그대로 인계하고 콘텐츠 GameObject를 좌표 보존 재부모하므로, **아래 보존 콘텐츠 행의 좌표는 현 프리팹 실측값(현 루트=패널 로컬)을 그대로 ModalBody 로컬로 이식**한다(좌표 변환 불필요). Title·CloseButton 두 행만 공통 템플릿(§1.2) 규격으로 신규/변경한다.

**앵커종류 표기(구현 분기 제거)**: 각 행이 **stretch**(AnchorMin≠AnchorMax, 음수 sizeDelta는 인셋)인지 **점앵커**(AnchorMin==AnchorMax, sizeDelta는 절대 폭이므로 반드시 양수)인지를 명기한다. 음수 폭은 stretch 앵커에서만 유효하다 — 점앵커 행은 모두 양수 폭이다.

| 요소 | 앵커종류 | AnchorMin–Max | AnchoredPos | sizeDelta | Pivot | 정렬/비고 |
|---|---|---|---|---|---|---|
| Title | **점앵커** | (0,1)–(0,1) 좌상 | (16, -12) | (240, 26) | (0,1) | fontSize 16, Left. text "계정"(string ID 37, 단 §6.4 step6 선확인 단서). 현 stretch Title을 공통 코너 라벨 규격으로 변경(양수 폭) |
| CloseButton | **점앵커** | (1,1)–(1,1) 우상 | (-12, -12) | (28, 28) | (1,1) | 코너 X (공통 §1.2). 신규(기존 하단 "닫기" 대체, 양수 폭) |
| DisplayNameText | **stretch** | (0,1)–(1,1) 좌우stretch·상단 | (-85, -150) | (-230, 48) | (0.5,1) | 실측 그대로 이식. 좌우 인셋 230. Center. "표시명: ___" |
| ConnectionText | **stretch** | (0,1)–(1,1) 좌우stretch·상단 | (0, -92) | (-40, 40) | (0.5,1) | 실측 그대로 이식. 좌우 인셋 40. "연결됨"=녹 (0.45,0.82,0.5,1) / "오프라인"=회 (0.6,0.62,0.66,1) |
| ConflictDot | **점앵커** | (1,1)–(1,1) 우상 | (-26, -24) | (20, 20) | (0.5,0.5) | 실측 그대로 이식. 우상단 빨간 dot 배지. 충돌 대기 시만 active(양수 폭) |
| ChangeNameButton | **점앵커** | (1,1)–(1,1) 우상 | (-90, -174) | (140, 48) | (0.5,0.5) | 실측 그대로 이식. [변경](양수 폭) |
| RestoreButton | **점앵커** | (0.5,1)–(0.5,1) 상단중앙 | (0, -240) | (340, 60) | (0.5,0.5) | 실측 그대로 이식. [클라우드에서 복원](양수 폭) |
| NameEditGroup | **점앵커** | (0.5,1)–(0.5,1) 상단중앙 | (0, -320) | (520, 120) | (0.5,0.5) | 실측 그대로 이식. 입력 영역 컨테이너(기본 숨김). 내부 자식 좌표 기존 유지(양수 폭) |
| ConflictGroup | **stretch** | (0,1)–(1,1) 좌우stretch·상단 | (0, -82) | (-40, 180) | (0.5,1) | 실측 그대로 이식. 좌우 인셋 40, 상단에서 흐름. 충돌 권유 영역(배지 켜졌을 때만 active). 내부 자식(ConflictText·ConflictRestoreButton·ConflictLaterButton) 좌표 기존 유지 |

> **앵커종류 단일 진실**: 위 표의 "앵커종류" 컬럼이 구현 기준이다. stretch 행(DisplayNameText·ConnectionText·ConflictGroup)은 AnchorMin≠AnchorMax + 음수 sizeDelta(좌우 인셋)로, 점앵커 행은 AnchorMin==AnchorMax + 양수 sizeDelta(절대 폭)로 구현한다. 보존 콘텐츠 행 7종은 모두 현 프리팹 실측값이므로, 재부모 시 **좌표를 새로 잡지 말고 기존 RectTransform 값을 그대로 유지**하면 된다(Title·CloseButton만 변경/신규).

> **결정 — 하단 "닫기" 버튼 → 코너 X로 대체**: 다른 모든 메타 팝업이 코너 X 단일 패턴이므로 일관성을 위해 하단 중앙 "닫기" 버튼(300×60)을 제거하고 우상단 코너 X(28×28)로 통일한다. 이로써 닫기 동선이 마을 전 팝업에서 동일. (대안 — 둘 다 유지: 기각. 닫기 수단 이중화는 다른 팝업과 불일치하고 헤더/하단 모두 영역을 먹는다.)

### 2.4 기능 동작 불변 보증

- 위 모든 GameObject는 **이름·컴포넌트 유지**, 변경은 `m_Father`(부모를 ModalBody로) + RectTransform 좌표뿐. 따라서 `CloudPopup.cs`의 `[SerializeField]` 참조(`_connectionText` `_displayNameText` `_changeNameButton` `_nameInput` `_nameConfirmButton` `_nameCancelButton` `_nameEditGroup` `_restoreButton` `_conflictGroup` `_conflictText` `_conflictRestoreButton` `_conflictLaterButton` `_conflictDot`)는 그대로 살아 있어 복원·표시명 변경·충돌 처리 동작이 불변이다.
- **닫기 수단은 UIBase 슬롯 재사용으로 충족**: `UIBase`는 `_backgroundButton`(클릭 시 Close) · `_backButton`(클릭 시 Close) `[SerializeField]`를 이미 제공한다. 현재 `_backgroundButton`=루트 GO Button(루트 클릭 닫기), `_backButton`=하단 "닫기" 버튼이다(§2.1 실측). 정합 후엔 신규 코너 X CloseButton → `_backButton`, 신규 Dim Button → `_backgroundButton`에 재지정하면 닫기 동작이 코드 추가 없이 보장된다. 하단 "닫기"가 쓰던 `_backButton` 슬롯을 코너 X가 인계받고, 루트 GO Button이 쓰던 `_backgroundButton` 슬롯을 Dim Button이 인계받는다(닫기 회귀 0). **루트 GO Button 제거 시 `_backgroundButton` 참조가 댕글링되므로 반드시 Dim Button으로 재지정한다** — 와이어링 방식 결정은 §6.6.

---

## § 3. RankingPopup(랭킹) 정합안

### 3.1 현재 상태 (실측)

- 루트 GameObject 이름이 **`LeaderboardPopup`** (다른 팝업은 루트 이름 = 팝업 이름). AnchorMin/Max (0.5,0.5) 중앙 · sizeDelta **(640, 900)** · Image 색 **(0.12, 0.13, 0.16, 0.96)**.
- **Dim 없음 · ModalBody 없음 · 코너 X 없음.** Title 있음(상단, 아이콘 없음). 컬럼 헤더 `Header`(자식: HHero · HTime · HRank · HName) · `ScrollView` · `EmptyText` 보유. 닫기는 루트 GO 자체의 Button이 `UIBase._backgroundButton`에 연결되어 **루트(패널 전체) 클릭으로 닫히는** 상태 — 명시적 닫기 버튼은 없다.
- 루트 직속 자식: `Title`(상단 (0,-14) stretch · sizeDelta (-40,56)) · `Header`(컬럼 헤더 바, (0,-78) · sizeDelta (-40,40)) · `ScrollView`((0,-38) · sizeDelta (-40,-324)) · `EmptyText`.

### 3.2 목표 구조 (정합 후)

```
RankingPopup (Root)  ← 루트 GO 이름 LeaderboardPopup → RankingPopup 으로 변경. anchors 0,0–1,1 전체화면 stretch, Image 제거
├─ Dim                       ← 신규: 전체 stretch, Image (0,0,0,0.6), CHButton 딤클릭닫기
└─ ModalBody                 ← 신규: 중앙 (0.5,0.5), sizeDelta (640, 900), Image (0.122,0.161,0.216,0.98) + Shadow 금색
   ├─ Title                  ← 좌상단 (16,-12) 240×26, fontSize 16. text "랭킹" (string ID 36 — 단 §6.5 step4 선확인, 미존재 시 literal "랭킹" 유지)
   ├─ CloseButton            ← 신규 코너 X: 우상단 (-12,-12) 28×28 (누락 보완)
   ├─ Header                 ← 컬럼 헤더 바(HRank · HName · HHero · HTime). 콘텐츠 영역 상단
   ├─ ScrollView             ← 랭킹 행 리스트(Viewport→Content→Cell)
   └─ EmptyText              ← 빈/실패/오프라인 안내(중앙, 기본 숨김)
```

### 3.3 요소 배치 명세 (ModalBody 콘텐츠 영역, 패널 640×900 기준)

**앵커종류 표기(§2.3과 동일 규칙)**: stretch 행은 음수 sizeDelta(인셋), 점앵커 행은 양수 절대 폭. EmptyText·Header·ScrollView는 현 프리팹 실측 stretch 값을 이식한다.

| 요소 | 앵커종류 | AnchorMin–Max | AnchoredPos | sizeDelta | 비고 |
|---|---|---|---|---|---|
| Title | **점앵커** | (0,1)–(0,1) 좌상 | (16, -12) | (240, 26) | fontSize 16, Left. text "랭킹"(string ID 36 — 단 §6.5 step4 선확인, 미존재 시 literal "랭킹" 유지). 기존 stretch full-width Title을 공통 좌상단 라벨 규격으로 변경(양수 폭) |
| CloseButton | **점앵커** | (1,1)–(1,1) 우상 | (-12, -12) | (28, 28) | 코너 X (신규, 누락 보완. 양수 폭) |
| Header | **stretch** | (0,1)–(1,1) 좌우stretch·상단 | (0, -52) | (-40, 40) | 컬럼 헤더 바. Title 아래로 한 칸 내림(기존 -78 → -52, 헤더가 ModalBody 안쪽으로 들어오므로 인셋 재계산). 좌우 인셋 40. 내부 HRank/HName/HHero/HTime 좌표 기존 유지 |
| ScrollView | **stretch** | (0,0)–(1,1) full stretch | (0, -96) | (-40, -116) | Title(56) + Header(40) 합 ≈ 96px 상단 인셋, 하단 20px 여백. 검산: 가용 높이 = 900 − 96 − 20 = 784px. 좌우 인셋 40·상하 인셋 116. 내부 Viewport/Content/Cell 구조 유지 |
| EmptyText | **stretch** | (0,0)–(1,1) full stretch | (0, -60) | (-60, -520) | 실측 그대로 이식(현 full stretch 인셋: 좌우 60·상하 520). 빈/실패 메시지(중앙 정렬은 TMP HorizontalAlignment=Center). 기본 비활성(`Load`에서 제어) |

> **EmptyText 앵커 정정**: 이전 안의 점앵커 (0.5,0.5)+음수 폭 (-80,120)은 기하 모순(점앵커는 양수 절대 폭만 유효)이었다. 현 프리팹은 full stretch (0,0)–(1,1) + 인셋 sizeDelta (-60,-520)이므로 실측값을 그대로 이식한다. 텍스트 중앙 표시는 RectTransform이 아니라 TMP 정렬(Center/Middle)로 달성한다.

> **컬럼 헤더(Header) 유지**: 랭킹은 표 형식이라 HRank/HName/HHero/HTime 4열 헤더가 콘텐츠 가독성에 필수다. 이는 랭킹 고유 콘텐츠로 공통 템플릿과 충돌하지 않으며, ModalBody 안쪽 Title 아래에 둔다.

### 3.4 기능 동작 불변 보증

- `Header` · `ScrollView`(+자식 Viewport/Content/Cell) · `EmptyText` · `Title`은 이름·컴포넌트 유지, `m_Father`(→ ModalBody) + RectTransform 좌표만 변경. `RankingPopup.cs`의 `_scrollView`(RankingPoolingScrollView) · `_emptyText` `[SerializeField]` 참조는 보존 → Top 100 조회·내 행 표시·빈 목록 안내 동작 불변.
- **코너 X 신규 — UIBase 슬롯 재사용**: 현재 루트 클릭(`_backgroundButton`)으로 닫히는데, 루트가 전체화면 투명 컨테이너가 되면 이 닫기는 Dim Button(`_backgroundButton` 인계)으로 이전한다. 신규 코너 X는 `_backButton`에 연결 → 코드 추가 없이 명시 닫기 버튼이 생긴다. 닫기 회귀 0(루트클릭→딤클릭으로 동선만 이동). §6.6.
- **루트 GO 리네임 안전**: Addressable 로드 키는 파일명(`RankingPopup.prefab`)이며 이미 RankingPopup이므로, 루트 GameObject 이름을 `LeaderboardPopup` → `RankingPopup`으로 바꿔도 로드에 영향 없음(Rule 03 §2). 정합 차원의 명명 일관화.

---

## § 4. 헤더 아이콘 결정 (VillageIcons)

- **사용 가능 에셋 확인**: `Assets/_Lair/Art/Sprites/VillageIcons/Icon_Account.png` (guid `41fb5f77bcfed5646b4f1049b7de7898`) · `Icon_Rank.png` (guid `69831bc1babf0604b84e148efc25b4df`) 존재.
- **참조 팝업 실측**: ShopPopup · CodexPopup · RecordsPopup의 Title은 모두 `m_Children: []` — **헤더 아이콘을 쓰지 않는다.**
- **결정 — 헤더 아이콘 없음(Title 텍스트만)**: 정합의 목적이 "다른 팝업과 같게"이므로 참조 팝업 선례(아이콘 없는 텍스트 헤더)를 따른다. CloudPopup·RankingPopup도 Title을 텍스트 라벨("계정"·"랭킹")만 둔다.
- (대안 — 두 팝업 Title에 Icon_Account/Icon_Rank를 paired 추가: 기각. 다른 6개 팝업에 헤더 아이콘이 없는데 이 2개만 아이콘을 달면 오히려 새 불일치를 만든다. 헤더 아이콘을 도입하려면 6개 팝업 전체에 일괄 적용하는 별도 기획이 필요하며 본 기획 범위 밖.)

---

## § 5. 제약 (구현 단계 준수)

- **v0.3 단계 범위** — 신규 영웅/몬스터/카드 리소스 제작 금지. 본 정합은 기존 스프라이트(Unity 내장 sliced) · 기존 폰트(NotoSansKR) · 기존 색 토큰만 사용 → 신규 아트 0건.
- **콘텐츠 기능 불변** — 랭킹 조회(Top 100 + 내 행) · 계정 표시명 변경 · 클라우드 복원 · 충돌 처리 동작은 변경하지 않는다. 시각 구조(프레임/딤/헤더 정합)만 통일.
- **Rule 03 §3 (래퍼 우선)** — 추가/변경되는 텍스트는 `CHText` + TMP, 버튼은 `Button` + `CHButton`. 신규 CloseButton·Dim 버튼은 `CHButton` 구성(공통 템플릿과 동일). Legacy Text/단일 Button 금지.
- **Rule 03 §2 (Enum 키)** — 프리팹 파일명(`CloudPopup.prefab`·`RankingPopup.prefab`)·EUI Enum 값 불변. 루트 GO 리네임은 로드 키에 무관(§3.4).
- **Rule 02 §6 (MVVM)** — View(팝업)는 표시·입력만. 닫기/딤 클릭은 `UIBase.CloseUI` 경로로(비즈니스 로직 없음).
- **Rule 03 §3 (CHPoolingScrollView 구조)** — RankingPopup ScrollView의 Viewport/Content/Cell 정적 구조 유지. 재부모 시 코드 동적 생성 금지(기존 PrefabInstance 구조 보존).

---

## § 6. 구현 요청사항 (gameplay-programmer 용)

대상 파일: `Assets/_Lair/Art/UI/CloudPopup.prefab` · `Assets/_Lair/Art/UI/RankingPopup.prefab` · `Assets/_Lair/Scripts/UI/Village/CloudPopup.cs` · `Assets/_Lair/Scripts/UI/Village/RankingPopup.cs`.

### 6.1 Enum

- **추가 없음.** EUI에 `CloudPopup` · `RankingPopup` 이미 존재(파일명 일치). 변경 없음.

### 6.2 Interface

- **추가 없음.**

### 6.3 에셋 키 / 스프라이트

- 신규 스프라이트 **없음**. ModalBody/CloseButton: Unity 내장 sliced (`m_Sprite {fileID: 10907, guid: 0000000000000000f000000000000000}`). Dim: 스프라이트 없음(단색).
- VillageIcons는 **사용 안 함**(§4 결정).
- 폰트: NotoSansKR SDF (`guid 12e8e80fec3a8554fa63474df537b505`) — 기존 그대로.

### 6.4 프리팹 작업 (CloudPopup.prefab)

1. 루트 `CloudPopup`: Image/Button/CHButton 컴포넌트 제거(투명 컨테이너화), RectTransform을 anchors (0,0)–(1,1) stretch · sizeDelta (0,0)로 변경. **댕글링 주의(CloudPopup도 동일 리스크)**: 이 루트 Button은 현재 `UIBase._backgroundButton`에 와이어링돼 있다(§2.1 실측). 제거하면 `_backgroundButton` 참조가 댕글링되므로, 반드시 step2의 신규 Dim Button으로 재지정한다(§6.6).
2. 자식 `Dim` 신규: 전체 stretch · Image (0,0,0,0.6) `m_Sprite 0` `m_Type 0` · Button + CHButton. ModalBody보다 형제 순서 앞(뒤에 렌더되도록).
3. 자식 `ModalBody` 신규: 중앙 (0.5,0.5) · sizeDelta (620,720) · Image (0.122,0.161,0.216,0.98) 내장 sliced `m_Type 1` + Shadow(`m_EffectColor` (0.984,0.749,0.141,1) · `m_EffectDistance` (2,-2)).
4. 기존 콘텐츠 GameObject 9종을 ModalBody 자식으로 재부모 + §2.3 좌표 적용. (이름·컴포넌트 유지)
5. 기존 하단 "닫기" `CloseButton`(300×60) → 제거하고, 공통 코너 X CloseButton(28×28, §1.2)을 ModalBody 우상단에 신규 배치. 자식 `X`(CHText `×`, fontSize 18). 닫기 와이어링은 §6.6(UIBase 슬롯 재사용).
6. **Title 문자열 — string-table 선확인 필수(동작 변경 주의)**: 현 Title은 `_stringID:-1` + 하드코딩 `m_text "계정"` 으로 **이미 정상 렌더**된다. string table에 ID 37("계정")이 실제 등록돼 있는지 **먼저 확인**한 뒤에만 `_stringID:37` 로 전환한다. 37이 미존재면 빈 칸이 되므로 **`_stringID:-1` + literal `m_text "계정"` 유지(fallback)**. 이 항은 순수 시각 정합에 불필요한 동작 변경이며 string-table 의존을 추가하는 선택사항이다 — 미확인 시 literal 유지가 기본값.

### 6.5 프리팹 작업 (RankingPopup.prefab)

1. 루트 GO 이름 `LeaderboardPopup` → `RankingPopup` 변경. Image/Button 제거(투명 컨테이너), anchors (0,0)–(1,1) stretch · sizeDelta (0,0).
2. 자식 `Dim` 신규(CloudPopup과 동일 스펙).
3. 자식 `ModalBody` 신규: 중앙 · sizeDelta (640,900) · Image (0.122,0.161,0.216,0.98) sliced `m_Type 1` + Shadow 금색.
4. 기존 `Title` · `Header` · `ScrollView` · `EmptyText`를 ModalBody 자식으로 재부모 + §3.3 좌표 적용. Title을 stretch full-width → 좌상단 240×26 라벨 규격으로 변경. **Title 문자열 — string-table 선확인 필수(동작 변경 주의)**: 현 Title은 `_stringID:-1` + 하드코딩 `m_text "랭킹"` 으로 이미 정상 렌더된다. string table에 ID 36("랭킹")이 실제 등록돼 있는지 **먼저 확인**한 뒤에만 `_stringID:36` 으로 전환한다. 36이 미존재면 빈 칸이 되므로 **`_stringID:-1` + literal `m_text "랭킹"` 유지(fallback)**. 순수 시각 정합에 불필요한 동작 변경(string-table 의존 추가)이므로 미확인 시 literal 유지가 기본값.
5. 공통 코너 X CloseButton 신규(누락 보완) — ModalBody 우상단 (-12,-12) 28×28. 닫기 와이어링은 §6.6(UIBase 슬롯 재사용 + 루트 `_backgroundButton` 재지정).

### 6.6 닫기 동작 요구사항 (behavioral — gameplay-programmer 판단 영역)

> `UIBase`가 닫기 슬롯(`_backgroundButton`·`_backButton`, 클릭 시 Close)을 이미 제공하므로, 닫기는 새 스크립트 필드 없이 프리팹 인스펙터 와이어링만으로 충족될 가능성이 높다. 구체 방식은 gameplay-programmer가 UIBase 대조 후 결정한다(코드 구조 지정은 본 기획 범위 밖).

요구사항(결과 기준):
- 코너 X CloseButton 클릭 · Dim 클릭 두 경로로 팝업이 닫혀야 한다.
- 기존 닫기 동작을 잃지 않는다 — 닫기 회귀 0. (Cloud: 기존 하단 "닫기"가 쓰던 `UIBase._backButton`을 코너 X가 인계. Ranking: 기존 루트 클릭이 쓰던 `UIBase._backgroundButton`을 Dim Button이 인계.)
- 권장 와이어링: Dim Button → `UIBase._backgroundButton`, 코너 X CloseButton → `UIBase._backButton`. (둘 다 CHButton 구성, Rule 03 §3)
- 루트 GO의 Image/Button을 제거할 때 RankingPopup 루트에 연결돼 있던 `UIBase._backgroundButton` 참조가 댕글링되지 않도록, 새 Dim Button으로 재지정한다.
- 두 스크립트의 기존 기능 `[SerializeField]` 참조(§2.4·§3.4)는 재부모 후에도 인스펙터에서 끊기지 않아야 한다(재부모는 참조를 끊지 않음). 빌드 후 인스펙터 null 참조 0건 확인.

### 6.7 SO 스키마 / 수치 필드

- **변경 없음.** 데이터 SO 미관여(순수 프리팹/뷰 정합).

---

## § 7. 검증 게이트 (구현 후 사용자/QA 확인)

- 두 팝업이 다른 메타 팝업과 나란히 떴을 때 **프레임 색·딤·헤더 라벨 위치·코너 X 위치가 픽셀 단위로 동일**해 보이는가.
- 계정: 표시명 변경 · 복원 · 충돌 권유 동작이 정합 전과 동일하게 작동하는가(기능 회귀 0).
- 랭킹: Top 100 조회 · 내 행 표시 · 빈/오프라인 안내가 정합 전과 동일한가.
- 닫기: 코너 X · 딤 클릭 · ESC(최상위 팝업) 모두로 닫히는가. (ESC 동작은 CHMUI `CanCloseByEsc` 기존 정책 따름)
- 인스펙터 null 참조 0건(재부모 후 와이어링 보존 확인).

> 본 정합은 시각 구조 변경 위주라 qa-simulator 게임플레이 시뮬 대상이 아니다(전투/밸런스 무관). 화면 검증은 사용자 리뷰 + EditMode 회귀로 충분.
