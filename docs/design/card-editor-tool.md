# Card Editor Tool — 기획서 (개발용 에디터 툴)

> 본 문서는 게임플레이 콘텐츠가 아니라 **개발자용 Unity 커스텀 EditorWindow** 기획서다. 런타임 게임 로직·밸런스에 영향 없음 → 밸런스 수치 / 시너지 / 페이싱 / 난이도 곡선 항목은 **해당 없음**.
> 도메인(UX·워크플로) 결정은 brainstorming 에서 사용자와 합의 완료 (spec 결정 락 표 §4). 본 기획서는 합의 내용을 design 문서로 정리한 것이며, spec/plan 과 모순되는 신규 결정은 없다.

## §0 헤더

- **목표** — 한 화면에서 모든 카드를 조회·추가·삭제·편집하고, 풀 소속과 신규 카드 종류 추가(enum codegen)까지 처리하는 `Lair > Card Editor` EditorWindow.
- **성공 기준 (수용 조건)** — spec §9 그대로:
  1. `Lair > Card Editor` 메뉴로 윈도우가 열린다.
  2. 기존 28장이 목록에 아이콘·이름·축·풀 뱃지와 함께 보인다.
  3. 카드 선택 → 우측에서 전 필드(이미지·아이콘·이름·설명·Effect 타입·수치) 편집 → [저장] → 에셋에 반영.
  4. 새 ID 입력 → [Enum 추가] → 재컴파일 후 (미생성) 등장 → [생성하기] → .asset 생성 → 편집 가능.
  5. 풀 소속 단일 선택(없음/Passive/Active) → [저장] → `CardPool` 에셋에 반영.
  6. [카드 삭제] → 에셋·풀에서 제거, enum 유지, (미생성) 으로 재노출, [생성하기]로 재생성 가능.
  7. 런타임 컴파일·기존 카드 직렬화 무손상.
- **현재 단계 범위 적합성** — 범위 내. 개발 생산성 도구로 MVP §8 비작업 항목(메타/서버/사운드/메인메뉴)과 무관하며, 런타임 스키마(`CardData`/`CardPool`) 변경 없음.
- **핵심 메커니즘** — master-detail EditorWindow. 좌측 카드 목록(ECardId 전 값 순회) + 우측 편집 패인(`SerializedObject` + `PropertyField` 로 Unity 네이티브 인스펙터 임베드). enum 은 전용 파일 `ECardId.cs` 로 분리해 툴이 append-codegen 으로 관리한다.

---

## §1 사용 시나리오 (누가 언제 쓰는가)

| 기능 | 사용 시점 | 동작 한 줄 |
|---|---|---|
| **조회** | 카드 밸런스/필드 점검 시 | 좌측 목록에서 28장 + 미생성 슬롯을 아이콘·이름·축·풀 뱃지로 한눈에 본다 |
| **편집** | 기존 카드의 이름/설명/이미지/Effect 수치 조정 시 | 카드 선택 → 우측 패인에서 전 필드를 네이티브 드로어로 수정 → [저장] 으로 커밋 |
| **추가 (새 종류)** | 신규 카드 종류를 맨바닥부터 만들 때 | 신규 ID 입력 → [Enum 추가] → 재컴파일 → (미생성) 슬롯 → [생성하기] |
| **추가 (미생성)** | enum 은 있으나 .asset 이 없는 슬롯을 채울 때 | (미생성) 행의 [생성하기] → .asset 생성 |
| **삭제** | 카드 .asset 을 폐기할 때 | [카드 삭제] → 확인 → asset·풀에서 제거 (enum 값은 유지) |
| **풀 관리** | 새 카드가 게임에 안 보이는 사고 방지 / 풀 재배치 시 | 편집 패인에서 없음/Passive/Active 단일 선택 후 [저장] 으로 커밋 |

기존 `Lair > JSON Sync` 윈도우는 CardData/CardPool ↔ JSON 동기화 전용으로 **별개 책임** — 본 툴이 대체하지 않는다.

---

## §2 윈도우 레이아웃

spec 의 master-detail 서술 + plan Task 4 의 좌측 폭 280px 기준으로 합성한 스케치:

```
┌─ Lair > Card Editor ─────────────────────────────────────────────┐
│ [상단 HelpBox — 안내 문구 ① (§4)]                                  │
├──────────────────────┬────────────────────────────────────────────┤
│ 좌: 목록 패인 (280px) │ 우: 편집 패인 (가변 폭)                       │
│                      │                                              │
│ [검색: ____________ ] │ [편집 패인 HelpBox — 안내 문구 ② (§4)]        │
│ ┌──────────────────┐ │                                              │
│ │ ⬚ WispHpBoost [P]│ │ Id           WispHpBoost   (read-only)       │
│ │ ⬚ Frenzy      [A]│ │ Axis         [Tank ▼]                        │
│ │ ...              │ │ DisplayName  [____________]                  │
│ │ TestNewCard      │ │ Description  [textarea........]              │
│ │   (미생성)[생성하기]│ │ Icon         [Sprite ◎]                      │
│ └──────────────────┘ │ CardImage    [Sprite ◎]                      │
│                      │ Effect       [타입 ▼] + 파라미터 필드          │
│ ── 새 카드 종류 (Enum)─│ 풀 소속 (한 풀만) [없음|Passive|Active]        │
│ [_________][Enum 추가] │ ● 저장되지 않은 변경   [ 저장 ]                 │
│                      │ [ 카드 삭제 ] (빨강)                          │
└──────────────────────┴────────────────────────────────────────────┘
```

- 좌측 폭 280px 고정, 우측 가변. 양 패인 모두 세로 스크롤뷰.
- `[P]`/`[A]` = 풀 소속 뱃지(Passive/Active). 아이콘 썸네일은 편집 가능 행에 20×20 으로 표시.

---

## §3 기능별 UX 동작

### 3.1 목록 행 표시
- `Enum.GetValues(typeof(ECardId))` 전 값을 순회한다.
- `Items/{ECardId}.asset` **존재** → 편집 가능 행: 아이콘 썸네일(20×20) + 이름 + `[Axis]` + 풀 뱃지. 클릭 시 우측 편집 패인에서 선택.
- `Items/{ECardId}.asset` **미존재** → "(미생성)" 라벨 + [생성하기] 버튼.
- 검색: 이름/ID 부분일치(대소문자 무시). **검색만 구현하며, 축/풀 필터는 본 작업 범위 외**(plan 미구현 — spec §5.6 의 "필터 구현 범위는 plan" 을 검색 단독으로 확정).

### 3.2 생성하기 (미생성 슬롯 → .asset)
- (미생성) 행 [생성하기] 클릭 → `ScriptableObject.CreateInstance<CardData>()` → `_id` = 해당 enum, `_displayName` = enum 이름 초기값 → `Items/{ECardId}.asset` 경로로 생성(기존 `CardDataSyncer` 컨벤션 일치).
- 생성 직후 해당 카드를 선택 상태로 전환 → 우측 편집 패인 활성.

### 3.3 Enum 추가 (2-스텝 — 재컴파일로 분리)
신규 카드 종류는 도메인 리로드를 사이에 두고 **2 스텝**으로 진행한다. 한 액션으로 합치지 않는다(리로드로 상태 유실).

1. **[Enum 추가]** — 좌측 하단 입력란에 신규 ID 문자열 입력 → 버튼.
   - 유효성: C# 식별자 규칙(`^[A-Za-z_][A-Za-z0-9_]*$`) + 기존 ECardId 값과 중복 금지. 위반 시 에러 다이얼로그, 파일 미변경.
   - `ECardId.cs` 의 삽입 마커 `//# <card-editor:insert>` 줄 **바로 위**에 `{newId},` 한 줄 append(기존 값 뒤 — 정수 인덱스 시프트 없음) → `AssetDatabase.Refresh` → 재컴파일.
2. **재컴파일 후** — 목록에 `{newId} (미생성)` 슬롯으로 등장 → [생성하기]로 .asset 생성(3.2 와 동일).

### 3.4 전 필드 편집 + 명시 [저장] (명시 저장 모델)
- 선택 카드의 `SerializedObject` 를 생성(선택 변경 시에만 재생성)하여 다음 필드를 `EditorGUILayout.PropertyField`(자식 포함)로 렌더:
  `_axis` · `_displayName` · `_description` · `_icon` · `_cardImage` · `_effect`.
- `_id` 는 **read-only LabelField** 로만 표시(편집 루프에서 제외 — §5 엣지 참조).
- `_effect` 는 `[SerializeReference]` 폴리모픽 → Unity 6 기본 드로어가 타입 선택 드롭다운 + 구상 타입 파라미터 필드를 제공.
- **명시 [저장] 모델**: `PropertyField` 편집은 `SerializedObject` 에만 머무르고 **[저장] 버튼 전엔 .asset 에 커밋하지 않는다**. 매 프레임 `ApplyModifiedProperties()` 호출은 제거하고, `SerializedObject.Update()` 는 재생성 시점(선택 전환)에만 1회 호출한다 — 매 프레임 `Update()` 하면 pending 키스트로크를 에셋 값으로 덮어써 편집 자체가 불가능해진다.
- 편집 패인 하단 [저장] 버튼: 누르면 `ApplyModifiedProperties()` + 풀 소속 커밋(§3.5) + `EditorUtility.SetDirty(card)` + 변경된 풀 `SetDirty` + `AssetDatabase.SaveAssets()` 를 한 번에 수행.
- **미저장 표시 / 가드**: 필드(`hasModifiedProperties`) 또는 풀 소속(pending ≠ 현재 실제)이 다르면 "● 저장되지 않은 변경" 표시 + [저장] 활성, 변경 없으면 [저장] 비활성. 다른 카드로 전환할 때 미저장 변경이 있으면 `DisplayDialog("저장", "버리기")` 로 묻는다(저장→커밋 후 전환 / 버리기→그냥 전환). 윈도우 닫기는 막지 않는다.
- **이전 design-reviewer 지적 "저장 UX 비대칭" 해소**: 풀 토글은 즉시 저장하는데 필드는 Ctrl+S 에 일임하던 비대칭을, 필드·풀 모두 단일 [저장] 버튼으로 함께 커밋하는 대칭 모델로 통일했다(spec §5.3 의 "[저장] 버튼 둘지 plan" 을 **설치**로 재확정).

### 3.5 풀 소속 (단일 선택 — Passive ⊻ Active)
- 카드는 **최대 한 풀에만** 속한다. `없음 / Passive / Active` 3택 단일 선택(`GUILayout.Toolbar`)으로 표시하며, 선택값은 pending(`EPoolKind`)으로만 보관 — 저장 전엔 풀 에셋 미변경.
- 선택 카드 전환 시 pending 값을 카드의 **현재 실제 소속**으로 초기화(`LoadPool`+`IndexInPool`). 비정상적으로 양 풀에 모두 속해 있으면 Passive 우선 표시(정상 데이터에선 없음/하나).
- 저장 시 커밋: pending Passive → Active 풀 제거 + Passive 풀 추가(중복 방지), Active → 반대, 없음 → 양 풀 제거. 제거는 object-reference 배열에 null 엔트리가 남지 않도록 안전 제거(런타임 카드 픽 NRE 방지).
- 목록 행 뱃지 `[P]`/`[A]` 는 실제 풀 소속을 읽으므로 저장 후 다음 OnGUI 에서 갱신된다. 단일 선택이므로 정상 데이터에선 `[P] [A]` 동시 표기는 나오지 않는다.

### 3.6 삭제 (확인 다이얼로그)
- [카드 삭제](빨강) → `EditorUtility.DisplayDialog` 확인.
- 양 풀 `_cards` 에서 제거 → `AssetDatabase.DeleteAsset(path)`.
- **enum 값은 유지** → 목록에서 "(미생성)" 슬롯으로 재노출, [생성하기]로 재생성 가능.

---

## §4 안내 문구 (툴 UI) — spec §7 그대로

- **상단 HelpBox**: "ECardId enum에 값을 추가하면 이 툴에 (미생성) 슬롯으로 나타납니다. [Enum 추가]로 새 카드 종류를 만들 수 있습니다."
- **편집 패인 HelpBox**: "Icon/CardImage는 Build UI Prefabs(LairCardPrefabBuilder) 재실행 시 ECardId 이름 PNG 컨벤션으로 덮어쓰일 수 있습니다."

> 구현 주의(문구 drift): plan Task 4 의 상단 HelpBox 코드에는 "아래 [Enum 추가]" 로 "아래" 가 삽입되어 있어 spec §7 과 미세하게 다르다. **기획 기준 = spec §7 원문. 구현은 이 문자열을 그대로 사용한다** — plan Task 4 코드의 "아래" 삽입은 drift 이므로 spec §7 로 정렬한다. 하단 문구는 두 문서 동일.

---

## §5 엣지 / 주의 (구현·리뷰 시 반드시 참조)

| # | 항목 | 결정 / 처리 | 출처 |
|---|---|---|---|
| E1 | **`_id` read-only** | 편집 패인에서 `_id` 는 편집 불가(LabelField). 변경 시 파일명(=ECardId)과 `_id` 가 어긋나 loader/list/CardDataSyncer 가 desync 됨. PropertyField 루프에서 제외. | spec §5.3, plan R5 |
| E2 | **Effect 타입 피커 fallback** | 기본 가정: `PropertyField(_effect, true)` 가 null effect 신규 카드에도 타입 선택 드롭다운을 렌더. **안 그려지면 fallback** — 리플렉션으로 `ICardEffect` 구현체(`Lair.Card`)를 모아 수동 `Popup` 선택 시 `managedReferenceValue = Activator.CreateInstance(type)` 설정 후 파라미터 렌더. 신규 카드(null effect)로 조기 검증할 것(기존 28장은 effect 가 있어 버그가 가려짐). | plan Task 5 Step 0, R6 |
| E3 | **ECardId 분리는 Rule 02 §8 의도적 예외** | 공용 asset-key enum 은 `CommonEnum.cs` 에 모으는 게 룰이나, 본 enum 은 툴이 codegen 으로 관리해야 하므로 전용 파일 `ECardId.cs` 로 분리한다. enum 이름·namespace(`Lair.Data`)·값 순서·정수값 보존 → 모든 기존 참조·직렬화 무손상. **code-reviewer 는 이를 BLOCKER 로 오인하지 말 것** — 합의된 예외다. | spec §5.1·§8-R3 |
| E4 | **enum codegen 비파괴** | 삽입은 마커 `//# <card-editor:insert>` 줄 바로 위 append-only. 마커 없으면 예외(파일 미변경). 정규식이 enum 외 영역을 건드리지 않게 멤버 라인 패턴으로만 중복 검사. | spec §8-R1 |
| E5 | **도메인 리로드 타이밍** | [Enum 추가] 직후 컴파일 전에는 새 enum 이 미존재 → 2-스텝으로 분리(§3.3). 한 액션 합침 금지. | spec §8-R2 |

---

## §6 구현 요청사항 (gameplay-programmer 용)

> 본 툴은 런타임 스키마(`CardData`/`CardPool`) 를 변경하지 않는다 — **신규 SO 스키마·신규 게임 Enum·신규 게임 Interface 없음**. 아래는 툴 구현에 필요한 에디터 측 산출물만.

- **신규 런타임 파일 (codegen 대상)**: `Assets/_Lair/Scripts/Data/ECardId.cs`
  - `CommonEnum.cs` 의 `ECardId` 정의를 그대로 이동(이름·`namespace Lair.Data`·값 순서·정수값 보존), `CommonEnum.cs` 측 정의 제거.
  - enum 닫는 `}` 직전에 삽입 마커 한 줄: `//# <card-editor:insert>` — [Enum 추가] 가 이 줄 바로 위에 신규 ID 를 append. 마커 줄 삭제 금지.
- **신규 에디터 어셈블리**: `Assets/_Lair/Editor/CardTool/Lair.Editor.CardTool.asmdef`
  - `references: ["Lair"]`, `includePlatforms: ["Editor"]`, `autoReferenced: false`(JsonSync 미러링). EditMode 테스트가 codegen pure 함수를 참조할 수 있도록 자체 asmdef 서브폴더에 둔다.
- **에디터 클래스** (모두 `namespace Lair.EditorTools`, `Assets/_Lair/Editor/CardTool/`):
  - `LairCardEditorWindow` — 메뉴 `Lair/Card Editor`. 목록·선택·검색·CRUD 트리거·풀 단일선택·명시 [저장]·편집 패인.
  - `CardEnumCodegen` — `ECardId.cs` 텍스트에 신규 ID append 하는 pure 함수(`InsertCardId`) + 파일 IO 래퍼(`AppendCardId`). pure 함수는 단위 테스트 대상.
- **에셋 키 / 경로 컨벤션**:
  - 카드 .asset: `Assets/_Lair/Art/Cards/Items/{ECardId}.asset` (파일명 = ECardId 값명, Rule 03 §2).
  - 풀 에셋: `CardPool_Passive` · `CardPool_Active` (`Assets/_Lair/Art/Cards/`).
- **편집 대상 SO 스키마 (기존, 변경 없음)** — 참고용:
  - `CardData`: `_id`(ECardId, read-only) · `_axis`(EBuildAxis) · `_displayName`(string) · `_description`(string) · `_icon`(Sprite) · `_cardImage`(Sprite) · `_effect`(ICardEffect, `[SerializeReference]`).
  - `CardPool`: `_cards`(List<CardData>, private — SerializedObject 로 접근).

---

## §7 비포함 (YAGNI)

- JSON Sync 윈도우 통합/대체 (별개 책임).
- Effect 구현체 자체 신규 생성(코드 작성) — 기존 `ICardEffect` 타입 중에서 선택만.
- `_icon`/`_cardImage` PNG 임포트/생성 — 기존 Sprite 지정만.
- enum 값 자동 제거(삭제 시 enum 유지가 정책 — §3.6).
- 축/풀 필터 UI(검색만 구현 — §3.1).
- 런타임 스키마 변경, 메타/서버/사운드/메인메뉴.
