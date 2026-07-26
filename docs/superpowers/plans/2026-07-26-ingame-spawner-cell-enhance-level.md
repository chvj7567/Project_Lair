# 인게임 상태 셀 강화 레벨 표현 (도감 동일) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: subagent-driven-development 또는 executing-plans. start-develop 파이프라인으로도 실행 — 레이아웃/배지 위치/스케일 헤드룸 수치는 game-designer 기획서 SoT(`⟨기획서 확정⟩`), 프레임워크/레벨소스 배선은 gameplay-programmer 확정.

**Goal:** 인게임 하단 `SpawnerStatusCell` 아이콘에 도감(작업 L)과 동일한 4채널 강화 레벨 표현(발광 오버레이·틴트·스케일·"Lv N" 배지)을 적용하고, 레벨→시각 매핑을 도감과 공유 SoT로 묶는다.

**Architecture:** 작업 L에서 `CodexCell`에 있는 레벨 매핑 배열 + 적용 로직을 **공유 UI 헬퍼**(`Lair.UI.EnhanceLevelVisual`)로 추출한다. `CodexCell`·`SpawnerStatusCell`이 그 헬퍼를 호출 → 두 곳의 레벨 표현이 동일 코드로 보장(drift 방지). 레벨 소스는 `MetaProfile.GetShopLevel("Enhance_<species>")`(전투 중 고정).

**Tech Stack:** Unity 6 / C# / ChvjPackage(CHText/CHPoolingScrollView 아님·MonoBehaviour 셀) / NUnit.

## Global Constraints
- 커밋(Rule 01): 자동 커밋 금지, 체크포인트 `git add`까지.
- 스타일(Rule 02): //#·가드절·var/! 금지·위젯 private(§6.1).
- 인프라(Rule 03): CHText·인스펙터 아이콘 resolver·풀 재사용 리셋은 Bind 소유(OnEnable 리셋 금지 — RecordsStageCell 교훈).
- 범위: 기존 강화 시각(`SpeciesGlowColor`·UISoftGlow·MonsterIcons) 재사용, 신규 리소스 0, v0.3.
- **UI 목업 게이트(Rule 00)**: Task 3 프리팹 배선 전 목업 승인.
- 레이아웃·배지 위치·스케일 헤드룸: `⟨기획서 확정⟩`.

---

## 파일 구조

**생성:**
- `Assets/_Lair/Scripts/UI/EnhanceLevelVisual.cs` — 공유 레벨 시각 SoT(배열) + Apply 헬퍼

**수정:**
- `Assets/_Lair/Scripts/UI/Village/CodexCell.cs` — 로컬 배열·apply 로직 → 공유 헬퍼 호출로 이관(동작 불변)
- `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs` — `_glowOverlay`·`_levelBadge`·`_iconRect` 위젯 + 레벨 조회 + Apply 호출 + 풀 리셋
- `Assets/_Lair/Tests/EditMode/CodexEnhanceMappingArrayTests.cs`(있음, 작업 L) — 배열 참조를 `CodexCell.*` → `EnhanceLevelVisual.*`로 갱신
- `Assets/_Lair/Art/UI/SpawnerStatusCell.prefab` — 발광 오버레이·배지·아이콘 rect 배선(Task 3)

**생성(테스트):**
- `Assets/_Lair/Tests/EditMode/SpawnerCellEnhanceLevelTests.cs`

---

### Task 1: 공유 레벨 시각 SoT + 헬퍼 추출 (CodexCell 리팩터)

**Files:**
- Create: `Assets/_Lair/Scripts/UI/EnhanceLevelVisual.cs`
- Modify: `Assets/_Lair/Scripts/UI/Village/CodexCell.cs`
- Test: `Assets/_Lair/Tests/EditMode/CodexEnhanceMappingArrayTests.cs`

**Interfaces:**
- Produces (신규 `Lair.UI.EnhanceLevelVisual`):
  - `public static readonly float[] IconTintByLevel/GlowOverlayAlphaByLevel/ScaleByLevel` (CodexCell에서 이관, 값 불변).
  - `public const int MaxLevel = 5`.
  - `public static void Apply(int level, EMonster species, Image icon, Image glowOverlay, CHText levelBadge, RectTransform iconRect, Color baseIconColor)` — lv>0면 발광 오버레이(`SpeciesGlowColor`×alpha[lv])·틴트(`Lerp(baseIconColor, glow, tint[lv])`)·스케일(scale[lv])·배지 "Lv N", lv≤0이면 전부 off(오버레이/배지 비활성·스케일 1·icon.color=baseIconColor). 각 위젯 null 가드.
- Consumes: `SpeciesVisual.SpeciesGlowColor`, `EMonster`.

**설계 메모:** 헬퍼는 `Lair.UI`(Image/CHText 참조 필요 — Data 레이어 아님). CodexCell의 기존 `ApplyEnhancement`가 baseIconColor를 케이스별(white/실루엣/색칩)로 정하던 로직은 CodexCell에 남기고, 최종 4채널 적용만 헬퍼에 위임.

- [ ] **Step 1: 실패 테스트(배열 SoT 이관 핀)** — `CodexEnhanceMappingArrayTests.cs`의 `CodexCell.IconTintByLevel` 등을 `EnhanceLevelVisual.IconTintByLevel`로 바꾼 어서션 작성(길이 6·단조·Lv0 항등·Lv5 상한). 이관 전엔 `EnhanceLevelVisual` 미정의로 컴파일 실패.
- [ ] **Step 2: 헬퍼 생성** — `EnhanceLevelVisual` 에 3배열 + MaxLevel + `Apply(...)`. 로직은 CodexCell.ApplyEnhancement의 4채널 부분을 이식(baseIconColor 파라미터화).
- [ ] **Step 3: CodexCell 이관** — 로컬 3배열·MaxLevel 제거, `ApplyEnhancement`가 baseIconColor(해금=white/미해금=SilhouetteColor/색칩=TintColor 기존 규칙) 계산 후 `EnhanceLevelVisual.Apply(lv, species, _icon, _glowOverlay, _levelBadge, _iconRect, baseIconColor)` 호출. **도감 동작 불변**(값·표현 동일).
- [ ] **Step 4: 통과 확인** — 갱신 배열 테스트 + 기존 도감 테스트(CodexMonsterEnhance*·회귀) green. 도감 시각 무변경.
- [ ] **Step 5: 체크포인트** — `git add` EnhanceLevelVisual.cs(+meta)·CodexCell.cs·테스트.

> **주의(구현자)**: 작업 L의 `CodexEnhanceMappingArrayTests`가 `CodexCell.IconTintByLevel/GlowOverlayAlphaByLevel/ScaleByLevel`을 참조한다 — 배열 이관 시 이 참조가 **컴파일 깨짐**. 반드시 `EnhanceLevelVisual.*`로 갱신. `Grep "IconTintByLevel|GlowOverlayAlphaByLevel|ScaleByLevel" Tests/` 전수 확인.

---

### Task 2: SpawnerStatusCell — 레벨 4채널 적용

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs`
- Test: `Assets/_Lair/Tests/EditMode/SpawnerCellEnhanceLevelTests.cs`

**Interfaces:**
- Consumes: `EnhanceLevelVisual.Apply`, 종족 강화 레벨.
- Produces: 신규 위젯 `[SerializeField] private Image _glowOverlay`·`CHText _levelBadge`·`RectTransform _iconRect`. `RebindSnapshot`이 레벨 조회 후 `Apply(lv, type, _icon, _glowOverlay, _levelBadge, _iconRect, Color.white)` 호출.

**레벨 소스 배선 (plan 확정):** `RebindSnapshot`에서 `MetaSession.GetOrLoad().GetShopLevel("Enhance_" + snapshot.CurrentType)` 조회(전투 중 불변). (스냅샷 필드로 싣는 대안도 가능하나, 조회가 최소 변경. gameplay-programmer가 MetaSession 접근이 셀에서 적절한지 확인 — 부적절하면 SpawnerStatusPanel이 레벨을 Bind 인자로 전달.)

- [ ] **Step 1: 실패 테스트** — `SpawnerCellEnhanceLevelTests.cs`: 레벨→시각 계약을 `EnhanceLevelVisual` 수준에서 검증(SpawnerStatusCell은 MonoBehaviour+MetaSession 의존이라 직접 단위테스트 대신, 셀이 쓰는 헬퍼 계약 + 레벨 조회 키 `"Enhance_"+type` 형식을 검증). 예: `EnhanceLevelVisual.Apply`가 lv0에서 오버레이 off·scale1, lv5에서 alpha0.90·scale1.10 적용(더미 Image/RectTransform로).
- [ ] **Step 2: 위젯 필드 + Apply 호출 추가** — `SpawnerStatusCell`에 3위젯. `RebindSnapshot` 끝에 레벨 조회 + `EnhanceLevelVisual.Apply(...)`. `OnEnable` 풀 리셋에 오버레이/배지 off·스케일1·icon.color white 추가(잔상 방지, 단 실제 재설정은 매 RebindSnapshot의 Apply가 소유).
- [ ] **Step 3: 통과 + 컴파일** — 회귀(진행바·×N·이름·초·클릭 무변경) 확인.
- [ ] **Step 4: 체크포인트** — `git add` SpawnerStatusCell.cs·테스트.

---

### Task 3: SpawnerStatusCell 프리팹 배선 (⛔ 목업 승인 게이트 선행)

**Files (에셋/프리팹):**
- Modify: `Assets/_Lair/Art/UI/SpawnerStatusCell.prefab`

- [ ] **Step 0 (메인): 목업 승인 게이트** — game-designer 목업(작은 셀 발광·배지 배치·스케일 헤드룸)을 제시·승인(Rule 00). 승인 후 배선.
- [ ] **Step 1: 프리팹 배선** — `_glowOverlay`(아이콘 뒤, UISoftGlow, ⟨크기 기획서⟩)·`_levelBadge`(CHText, ⟨위치 기획서 — ×N과 구분⟩)·`_iconRect`(=중앙 아이콘 RectTransform, 스케일 헤드룸 위해 pivot·여백 ⟨기획서⟩) 추가·배선. 초기 비활성. 코드로 찍어야 하면 일회용 빌더(Rule 04 §3), 실행은 메인 MCP.
- [ ] **Step 2: (메인) 에디터 Play 육안** — 강화 종족 상태 셀이 발광·틴트·스케일·배지로 표시, Lv0 담백, 기존 요소(진행바·×N·이름·초)와 안 겹침, 스케일이 이웃/이름 침범 없음.
- [ ] **Step 3: 체크포인트** — `git add` 프리팹(+신규 .meta), 빌더 있으면 삭제.

---

## Self-Review

**1. Spec coverage:** spec §3.1 4채널 → Task 1 헬퍼·2 적용·3 프리팹 / §3.2 레벨 소스 → Task 2(GetShopLevel) / §3.3 공유 SoT → Task 1(EnhanceLevelVisual 추출) / §3.4 Lv0 off → Apply lv≤0 / §3.5 풀 리셋 Bind 소유 → Task 2 / §5 레이아웃 → Task 3 ⟨기획서⟩ ✅.

**2. Placeholder scan:** 레이아웃·배지 위치·스케일 헤드룸·발광 크기만 `⟨기획서⟩` 위임. 구조/시그니처/테스트 구체.

**3. Type consistency:** `EnhanceLevelVisual.IconTintByLevel/GlowOverlayAlphaByLevel/ScaleByLevel/MaxLevel/Apply(int,EMonster,Image,Image,CHText,RectTransform,Color)`·`SpawnerStatusCell._glowOverlay/_levelBadge/_iconRect` — Task 간 일치. CodexCell·SpawnerStatusCell 동일 `Apply` 호출.

**주의(구현자):** 작업 L `CodexEnhanceMappingArrayTests`의 `CodexCell.*` 배열 참조가 이관으로 깨진다 → `EnhanceLevelVisual.*`로 갱신(Task 1 Step 1). 도감 동작은 리팩터 후 **완전 불변**이어야 함(값·표현 동일) — 회귀 테스트로 확인.
