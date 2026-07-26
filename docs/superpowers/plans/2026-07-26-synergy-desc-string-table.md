# 시너지 티어 설명 스트링 테이블화 Implementation Plan

> **For agentic workers:** start-develop 파이프라인(B)으로 실행 — gameplay-programmer 구현 → code-reviewer → test-engineer. spec: `docs/superpowers/specs/2026-07-26-synergy-desc-string-table-design.md` 가 SoT.

**Goal:** `SynergyModalPopup` 의 하드코딩 `TierDesc` 를 제거하고, 각 시너지 tier 가 자기 설명(스트링 테이블 템플릿 id + 자기 밸런스 상수에서 계산한 수치)을 소유하게 하여, 표시 문자열은 `Strings_Ko.json` 에서·수치는 tier const 에서 단일 소스로 조립한다.

**Architecture:** `IBuildSynergyTier` 에 `DescriptionStringId`(int) + `DescriptionArgs`(string[]) 추가 → 12개 tier 가 구현. `BuildSynergyService.GetTier` 로 tier 조회. `SynergyModalPopup.BuildRows` 가 `IStringProvider` 주입받아 `string.Format(GetString(id), args)` 로 설명 생성.

**Tech Stack:** Unity 6 / C# / ChvjPackage(IStringProvider·CHText.StringProvider) / NUnit.

## Global Constraints
- Rule 01: 자동 커밋 금지, 체크포인트 `git add` 까지.
- Rule 02: `//#` 주석·가드절·명시타입·`== null`·위젯 private.
- Rule 03: 스트링은 Enum 아님(id 기반 기존 패턴 유지). CHText.StringProvider 정적.
- 범위: 축 헤더 라벨 영어 유지(범위 밖). 밸런스 수치 불변(Tank3 표시는 상수 일치로 자동 교정).
- 수치 포맷: `InvariantCulture`, 불필요한 소수 0 제거(1.3→"1.3", 1→"1").

---

## 파일 구조

**수정:**
- `Assets/_Lair/Scripts/Card/CommonInterface.BuildSynergy.cs` — `IBuildSynergyTier` 에 설명 멤버 2개 추가.
- `Assets/_Lair/Scripts/Card/Synergy/*.cs` (12개) — 각 tier 에 `DescriptionStringId`/`DescriptionArgs` 구현.
- `Assets/_Lair/Scripts/Card/BuildSynergyService.cs` — `GetTier(axis, threshold)` 추가.
- `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` — `TierDesc` 제거, `BuildRows` 가 provider+tierOf 로 설명 생성.
- `Assets/_Lair/Data/Json/Strings_Ko.json` — 12개 템플릿(id 200~211) 추가.
- `Assets/_Lair/Tests/EditMode/UI/SynergyModalPopupBuildTests.cs` (+ 관련 시너지 UI 테스트) — fake provider 주입으로 갱신.

**생성(테스트):**
- `Assets/_Lair/Tests/EditMode/Card/SynergyTierDescriptionTests.cs` — 각 tier id/arg 계약.

---

### Task 1: IBuildSynergyTier 설명 계약 + 12 tier 구현

**Files:** `CommonInterface.BuildSynergy.cs`, `Synergy/*.cs`(12), Test: `SynergyTierDescriptionTests.cs`

**Interfaces (Produces):**
- `IBuildSynergyTier` 에 `int DescriptionStringId { get; }` + `string[] DescriptionArgs { get; }`.
- 각 tier 는 spec §6 표대로 id(200~211)·arg 반환. 수치는 자기 const 에서 포맷.
  - Tank1→(200,["1.3"]) Tank2→(201,["1.2"]) Tank3→(202,["1.4"]) Dps1→(203,["1.3"]) Dps2→(204,["25"]) Dps3→(205,["1.3"]) Debuff1→(206,["0.8"]) Debuff2→(207,["0.85"]) Debuff3→(208,["1"]) Swarm1→(209,["1.3"]) Swarm2→(210,["0.85"]) Swarm3→(211,["1"]).
  - Dps2 arg = `((1f/CooldownMul - 1f)*100f)` 반올림 정수 문자열("25"). Debuff3 arg = `(Ratio*100f)` 정수("1").

- [ ] Step 1: 실패 테스트 — `SynergyTierDescriptionTests`: 각 tier `DescriptionStringId`·`DescriptionArgs[0]` 기대값 단언(Dps2=="25", Tank3=="1.4", Debuff3=="1" 등). 인터페이스 미구현이라 컴파일 실패.
- [ ] Step 2: 인터페이스 멤버 2개 추가.
- [ ] Step 3: 12개 tier 에 구현(자기 const 에서 arg 계산, spec §6).
- [ ] Step 4: 테스트 통과.
- [ ] Step 5: 체크포인트 `git add`.

**주의:** 수치 포맷은 `InvariantCulture` + 불필요 0 제거. `1.3f.ToString("0.##", CultureInfo.InvariantCulture)` → "1.3", `1.ToString()` → "1".

---

### Task 2: BuildSynergyService.GetTier + Strings_Ko.json 템플릿

**Files:** `BuildSynergyService.cs`, `Strings_Ko.json`, Test: `SynergyModalPopupBuildTests.cs`

**Interfaces (Produces):**
- `public IBuildSynergyTier GetTier(EBuildAxis axis, int threshold)` — `_tiers` 조회, 없으면 null.
- `Strings_Ko.json` 에 id 200~211 = spec §6 템플릿(예: `{"id":200,"text":"도깨비불·망령 HP ×{0}"}` … `{"id":211,"text":"모든 스포너 동시 출력 +{0}"}`).

- [ ] Step 1: `GetTier` 추가(가드: 미바인딩 시 null).
- [ ] Step 2: `Strings_Ko.json` 에 12개 항목 추가(기존 최대 id 37 과 비충돌 확인).
- [ ] Step 3: 체크포인트 `git add`.

---

### Task 3: SynergyModalPopup.BuildRows 리팩터

**Files:** `SynergyModalPopup.cs`, Test: 시너지 UI 테스트들

**Interfaces (Consumes):** `IBuildSynergyTier.DescriptionStringId/Args`, `BuildSynergyService.GetTier`, `IStringProvider`.

- BuildRows 시그니처 확장: `BuildRows(Func<EBuildAxis,int> countOf, Func<EBuildAxis,int,IBuildSynergyTier> tierOf, IStringProvider strings, Func<EBuildAxis,Sprite> iconOf = null)`.
  - 티어 행: `tier = tierOf(axis, threshold)`; `template = strings?.GetString(tier.DescriptionStringId)`; null/빈 가드 → `desc = template != null ? string.Format(CultureInfo.InvariantCulture, template, tier.DescriptionArgs) : ""`; `Label = $"Tier{n}  {desc}"`.
  - 기존 `TierDesc` 딕셔너리·`Thresholds` 무관 부분은 유지. threshold = Tier1/2/3Threshold 매핑.
- 호출부(팝업 Bind): `strings` = `CHText.StringProvider`, `tierOf` = 서비스 `GetTier`. 서비스 접근 경로 확인(팝업이 BattleViewModel/BattleController 경유로 서비스 참조 가능한지 — 불가 시 Bind 인자로 tierOf 전달).

- [ ] Step 1: 기존 시너지 UI 테스트를 fake provider + tierOf 주입으로 갱신(실패 상태).
- [ ] Step 2: `TierDesc` 제거, BuildRows 리팩터, 호출부 배선.
- [ ] Step 3: 컴파일 + 테스트 통과(행 수·RowKind·헤더·Tier 접두 회귀 확인).
- [ ] Step 4: 체크포인트 `git add`.

---

### Task 4: 테스트 스위트 (test-engineer)

- 기존 `SynergyModalPopupBuildTests`(`TierDesc_12개_키_전부_채워짐` 등)·`SynergyModalPopupEdgeCasesTests` 를 fake `IStringProvider`(id 200~211 템플릿 반환)로 갱신 — provider 없이 깨지던 것 복구.
- 신규: `string.Format` 결과 기대 문자열(Tank3="Tier3  도깨비불·망령 HP ×1.4", Dps2="…공속 +25%"), null provider 가드(예외 없음), 12 tier id 유일성.
- 회귀: `BuildSynergyService`·발화 로직 무변경 확인.

---

## Self-Review

**Spec coverage:** §3.1 tier 소유 → Task1 / §3.2 템플릿 → Task2 / §3.3 수치 단일소스 → Task1 arg / §3.4 provider 주입 → Task3 / §3.5 GetTier → Task2 / §5 Tank3 자동교정 → Task1(arg 1.4) / §7 테스트 → Task4. ✅

**Placeholder scan:** 호출부 서비스 접근 경로만 "확인"(Task3 Step2) — gameplay-programmer 가 실 구조로 확정. 나머지 수치·id·시그니처 구체.

**Type consistency:** `DescriptionStringId:int`·`DescriptionArgs:string[]`·`GetTier(EBuildAxis,int):IBuildSynergyTier`·`BuildRows(...,IStringProvider,...)` — Task 간 일치.
