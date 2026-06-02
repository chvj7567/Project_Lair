# Card Editor Tool — 설계 (spec)

- 날짜: 2026-06-02
- 단계: MVP (개발용 에디터 툴 — 게임플레이 콘텐츠 아님)
- 작성: brainstorming 산출물 (의도·범위·메커니즘 윤곽 + 결정 락)

## 1. 의도 / 한 줄 목표

한 화면에서 모든 카드를 **조회 · 추가 · 삭제 · 편집**하는 Unity 커스텀 EditorWindow.
카드의 전 필드(이미지·아이콘·이름·설명·능력/Effect)를 GUI로 편집하고, 풀 소속과 신규 카드 종류 추가(enum 코드젠)까지 툴 안에서 처리한다.

검증 대상이 아니라 **개발 생산성 도구**다 — 런타임 게임 로직/밸런스에 영향 없음.

## 2. 배경 — 기존 자산

- `CardData : ScriptableObject` — 필드: `_id`(ECardId) · `_axis`(EBuildAxis) · `_displayName` · `_description` · `_icon`(Sprite) · `_cardImage`(Sprite) · `_effect`(ICardEffect, `[SerializeReference]` 폴리모픽). 위치: `Assets/_Lair/Art/Cards/Items/{ECardId}.asset` (파일명 = ECardId 이름).
- `CardPool : ScriptableObject` — `_cards`(List<CardData>, private). 2개: `CardPool_Passive` · `CardPool_Active` (`Assets/_Lair/Art/Cards/`).
- `ECardId` enum — 현재 `Assets/_Lair/Scripts/Data/CommonEnum.cs`에 정의 (28값).
- `ICardEffect` 구현체 다수 — `Assets/_Lair/Scripts/Card/Effects/`, `Card/Auras/`.
- 기존 에디터 툴 (참고/비충돌):
  - `Lair > JSON Sync` (`LairJsonSyncWindow` + `CardDataSyncer`) — CardData/CardPool ↔ JSON 양방향 동기화. 카드 편집 GUI는 아님. **본 툴이 대체하지 않음** — 별개 책임.
  - `LairCardPrefabBuilder` — `_icon`/`_cardImage`를 ECardId 이름 PNG 컨벤션으로 자동 배정.

## 3. 범위 (이번 작업)

### 포함
- 전용 EditorWindow (master-detail): 좌측 카드 목록 + 우측 편집 패인.
- `ECardId` enum을 전용 파일로 분리 (툴이 코드젠으로 관리).
- 카드 종류 추가 (enum append) + .asset 생성 + .asset 삭제.
- 전 필드 편집 (네이티브 인스펙터 임베드).
- 풀(Passive/Active) 소속 토글 관리.

### 비포함 (YAGNI)
- JSON Sync 윈도우 통합/대체.
- Effect 구현체 자체 신규 생성(코드 작성) — 기존 ICardEffect 타입 중에서 선택만.
- `_icon`/`_cardImage` PNG 임포트/생성 — 기존 Sprite를 지정만.
- enum 값 자동 제거(삭제 시 enum 유지가 정책 — §5.3).
- 런타임 스키마(`CardData`/`CardPool`) 변경.
- 메타/서버/사운드/메인메뉴 (MVP §8 무관).

## 4. 결정 락 (brainstorming 합의)

| # | 결정 | 선택 | 사유 |
|---|---|---|---|
| D1 | 편집 UI | **Unity 네이티브 인스펙터 임베드** (`SerializedObject` + `EditorGUILayout.PropertyField`) | Sprite 피커·TextArea·enum 팝업·Effect 타입 드롭다운+수치를 Unity 기본 드로어로 공짜 획득. 최소 코드/안정. |
| D2 | 추가 범위 | **enum 자동 추가 + .asset 생성** | "진짜 새 카드를 맨바닥부터" 가능. enum은 별도 파일로 분리해 안전 코드젠. |
| D3 | ECardId 위치 | **전용 파일 `ECardId.cs`로 분리** | 툴이 append-codegen으로 관리. `CommonEnum.cs` 직접 수정/다른 enum·주석 훼손 회피. |
| D4 | 풀 소속 | **툴에서 관리 (Passive/Active 토글)** | 새 카드가 풀 미등록으로 게임에 안 보이는 사고 방지. |
| D5 | 삭제 동작 | **asset + 풀에서만 제거, enum 값 유지** | enum 중간 값 제거 시 정수 인덱스 시프트 → 기존 카드 `.asset`의 `_id` 어긋나 데이터 손상. 남은 enum은 "(미생성)" 슬롯으로 재노출, [생성하기]로 재생성. |

## 5. 메커니즘 상세

### 5.1 ECardId 분리 (코드젠 기반)
- 신규 런타임 파일 `Assets/_Lair/Scripts/Data/ECardId.cs` — `public enum ECardId { ... }` 만 포함, `namespace Lair.Data` 유지. `Lair` asmdef 소속(Scripts/Data 하위).
- `CommonEnum.cs`에서 `ECardId` 정의 제거 (다른 enum은 잔류). enum 이름·namespace·기존 값 순서·정수값 보존 → 모든 기존 참조·직렬화 무손상.
- ⚠️ **Rule 02 §8 의도적 예외**: 공용 asset-key enum은 `CommonEnum.cs`에 모으는 게 룰이나, 본 enum은 툴이 codegen으로 관리해야 하므로 분리. 기획서·code-review에 사유 명시 — BLOCKER 아님.
- 파일에 툴이 삽입 위치를 안정적으로 찾을 수 있도록 enum 블록을 인식(닫는 `}` 직전 삽입). 필요 시 마커 주석(`//# <card-editor:cards>`) 사용 — 세부는 plan에서 확정.

### 5.2 추가 흐름 (2종)
- **새 카드 종류 (enum 없음)**: 상단 입력란에 신규 ID 문자열 + [Enum 추가].
  - 유효성: C# 식별자 규칙 + 기존 ECardId 값과 중복 금지.
  - `ECardId.cs` enum 끝에 식별자 한 줄 **append**(기존 값 뒤 — 정수 시프트 없음) → `AssetDatabase.Refresh` → 재컴파일.
  - 재컴파일 후 목록에 "(미생성)" 슬롯으로 등장.
- **미생성 슬롯 (enum 있음, asset 없음)**: [생성하기] 클릭.
  - `ScriptableObject.CreateInstance<CardData>()` → `SerializedObject`로 `_id` = 해당 enum → `AssetDatabase.CreateAsset(card, "Assets/_Lair/Art/Cards/Items/{ECardId}.asset")` (기존 `CardDataSyncer` 컨벤션 일치).
- 두 단계가 재컴파일(도메인 리로드)로 분리됨 — 한 액션에 합치지 않는다(리로드로 상태 유실). 사용자가 의도한 "enum 추가 → (미생성) → 생성하기" 흐름과 일치.

### 5.3 편집
- 우측 패인: 선택된 CardData의 `SerializedObject` 생성 → `_id`/`_axis`/`_displayName`/`_description`/`_icon`/`_cardImage`/`_effect` 각각 `EditorGUILayout.PropertyField`(자식 포함) 렌더.
- `_effect`는 `[SerializeReference]` → Unity 6 기본 드로어가 타입 선택 드롭다운 + 구상 타입 필드 제공.
- 변경 시 `ApplyModifiedProperties` + `EditorUtility.SetDirty`. 저장은 Unity 표준(`Ctrl+S`/AssetDatabase.SaveAssets) — 명시 [저장] 버튼 둘지는 plan에서.

### 5.4 풀 소속
- `CardPool_Passive`/`CardPool_Active`를 `AssetDatabase`로 로드.
- 선택 카드에 대해 각 풀 포함 여부를 토글로 표시.
- 토글 ON → 해당 풀 `_cards`(SerializedObject로 접근)에 추가(중복 방지), OFF → 제거. `SetDirty`.

### 5.5 삭제
- [카드 삭제] → 확인 다이얼로그(`EditorUtility.DisplayDialog`).
- 양 풀 `_cards`에서 제거 → `AssetDatabase.DeleteAsset(path)`.
- enum 값은 유지 → 목록에서 "(미생성)" 슬롯으로 재노출.

### 5.6 목록 / 필터
- `Enum.GetValues(typeof(ECardId))` 순회.
- 각 값: `Items/{ECardId}.asset` 존재 → 편집 가능 행(아이콘 썸네일 + DisplayName + Axis + 풀 뱃지[P]/[A]). 미존재 → "(미생성)" + [생성하기].
- 검색(이름/ID 부분일치) + 축 필터 + 풀 필터. (필터 구현 범위는 plan에서 — 최소 검색만이라도 가능.)

## 6. 컴포넌트 구조

| 단위 | 책임 | 의존 |
|---|---|---|
| `ECardId.cs` (런타임) | 카드 식별자 enum (툴이 codegen 관리) | 없음 |
| `LairCardEditorWindow` (에디터) | 윈도우 셸: 목록·선택·필터·CRUD 트리거·풀 토글 | `CardData`, `CardPool`, `ECardId`, `AssetDatabase` |
| (윈도우 내부) enum codegen 유틸 | `ECardId.cs` 읽고 식별자 append + Refresh | 파일 IO, `AssetDatabase` |
| (윈도우 내부) 편집 패인 | 선택 CardData의 `SerializedObject` 임베드 렌더 | `SerializedObject`, `PropertyField` |

- 모두 `namespace Lair.EditorTools`, `Assets/_Lair/Editor/` (Rule 11 — 에디터 전용 UI 예외).
- 단일 파일로 시작; 비대해지면 codegen 유틸을 `LairCardEnumCodegen.cs`로 분리(plan 판단).

## 7. 안내 문구 (툴 UI)

- 상단 HelpBox: "ECardId enum에 값을 추가하면 이 툴에 (미생성) 슬롯으로 나타납니다. [Enum 추가]로 새 카드 종류를 만들 수 있습니다."
- 편집 패인 HelpBox: "Icon/CardImage는 Build UI Prefabs(LairCardPrefabBuilder) 재실행 시 ECardId 이름 PNG 컨벤션으로 덮어쓰일 수 있습니다."

## 8. 위험 / 주의

- **R1 — enum codegen 손상**: 정규식/문자열 삽입이 enum 외 영역을 건드리면 컴파일 깨짐. → 삽입 지점을 명확히(마커 또는 enum 블록 마지막 멤버 뒤), append-only, 기존 텍스트 비파괴.
- **R2 — 도메인 리로드 타이밍**: enum 추가 후 컴파일 전 [생성하기]를 누르면 새 enum 미존재. → 컴파일 중/직후엔 해당 슬롯 비활성 또는 안내.
- **R3 — Rule 02 §8 위반 오탐**: code-reviewer가 ECardId 분리를 BLOCKER로 볼 수 있음. → 본 spec/기획서에 사유 기록, 리뷰 시 참조.
- **R4 — 아이콘 컨벤션 충돌**: §7 편집 패인 문구로 사용자 인지.

## 9. 완료 기준 (수용 조건)

1. `Lair > Card Editor` 메뉴로 윈도우 열림.
2. 기존 28장이 목록에 아이콘·이름·축·풀과 함께 보임.
3. 카드 선택 → 우측에서 전 필드(이미지·아이콘·이름·설명·Effect 타입·수치) 편집 → 에셋에 반영.
4. 새 ID 입력 → [Enum 추가] → 재컴파일 후 "(미생성)" 등장 → [생성하기] → .asset 생성 → 편집 가능.
5. 풀 토글로 Passive/Active 소속 변경이 `CardPool` 에셋에 반영.
6. [카드 삭제] → 에셋·풀에서 제거, enum 유지, "(미생성)"으로 재노출, [생성하기]로 재생성 가능.
7. 런타임 컴파일·기존 카드 직렬화 무손상.
