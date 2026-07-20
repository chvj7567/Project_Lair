# 밸런스 SO 롤백 + 문자열 Data/Json 이동 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 밸런스를 커밋 `5d52d7e` 이전의 ScriptableObject 정본 상태로 되돌리고, 문자열 JSON 2개를 `Art/Json` → `Data/Json` 으로 이동한다.

**Architecture:** `5d52d7e` 의 ②(밸런스 SO→JSON 전환)만 선택적으로 역행하고 ①(캐릭터 서비스 로케이터)은 유지한다. 대부분 `git show 5d52d7e~1:<path>` 로 이전 파일을 복원하고, 런타임 JSON 로더/빌드훅을 제거한 뒤, 에디터 Syncer 로 현재 밸런스 값을 SO에 재반영한다. 문자열 JSON은 `.meta` 동반 이동으로 Addressable GUID를 보존한다.

**Tech Stack:** Unity 6 / URP, ChvjPackage(`CHMResource` Addressable), Newtonsoft.Json, Unity Test Framework(NUnit).

## Global Constraints

- **Rule 01 — 자동 커밋 금지**: 각 Task 는 `git add`(스테이징)까지만. 실제 `git commit` 은 파이프라인 마무리에서 사용자에게 메시지(안)로 전달. 아래 각 Task 의 마지막 "스테이징" 스텝은 커밋하지 않는다.
- **meta 규칙**: 신규(A)·삭제(D) 파일의 `.meta` 만 함께 스테이징. 수정(M) 파일 `.meta` 제외.
- **Rule 02 스타일**: `//#` 주석, `var` 금지·명시적 타입, `!` 금지(`== false`/`== null`), 가드절 중괄호 없이 개행.
- **Rule 03**: Addressable 로드는 Enum 키(`CHMResource`). 문자열 주소=파일명 유지(`Strings_Ko`/`LoadingStrings_Ko`).
- **①은 건드리지 않는다**: `LairCharacter` 등 서비스 로케이터 관련 파일, 몬스터 프리팹의 5d52d7e 변경분 유지. 커밋 전체 `git revert` 금지.
- **밸런스 값 정본**: 롤백 후 최종 `BalanceConfig.asset` 값은 **현재 `balance_config.json`**(hero.hp=4000, activeThresholds 5개 [30,90,150,210,270]) 과 일치해야 한다. 5d52d7e~1 의 옛 .asset 값(activeThresholds 9개)이 아니다.

---

## 참고 — 확인된 사실 (구현 중 재확인 불필요)

- Battle 씬(`Assets/_Lair/Scenes/Battle.unity:2085`)은 `_balance: {guid: f37d9d9829bfbdf4198bbf1e4215d0ca}` 참조를 **아직 보유**. 이 GUID = 삭제된 `BalanceConfig.asset.meta` GUID. → `.asset`+`.meta` 복원 시 씬 참조 자동 재연결.
- `5d52d7e` 는 `Battle.unity` 를 변경하지 않았다.
- 현재 `BalanceConfigDto` 위치: `Assets/_Lair/Scripts/Data/Dto/BalanceConfigDto.cs` (5d52d7e 가 Editor→Scripts 로 옮김). 참조: `BattleController`, `BalanceConfig.cs`, `BalanceJsonLoader.cs`, DTO 자신, 테스트 4종.
- 밸런스 테스트 4종: `BalanceConfigTests.cs`, `BalanceConfigOverlayTests.cs`, `BalanceJsonLoaderParseTests.cs`, `BalanceShippedJsonRegressionTests.cs`.

---

## Task 1: BalanceConfig 를 ScriptableObject 로 복원 + 런타임 로딩 롤백

**Files:**
- Restore: `Assets/_Lair/Scripts/Data/BalanceConfig.cs` (← `5d52d7e~1`)
- Restore: `Assets/_Lair/Data/BalanceConfig.asset` (+`.meta`) (← `5d52d7e~1`)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (밸런스 로딩부)

**Interfaces:**
- Produces: `BalanceConfig : ScriptableObject`, `[SerializeField] private BalanceConfig _balance;` (BattleController), 필드 접근자 `Hero`/`RunDuration`/`PassiveThresholds`/`ActiveThresholds`/`GetMonster`/`GetSpawnPeriod` 유지.

- [ ] **Step 1: 이전 SO 버전 복원**

```bash
git show 5d52d7e~1:Assets/_Lair/Scripts/Data/BalanceConfig.cs > Assets/_Lair/Scripts/Data/BalanceConfig.cs
git show 5d52d7e~1:Assets/_Lair/Data/BalanceConfig.asset > Assets/_Lair/Data/BalanceConfig.asset
git show 5d52d7e~1:Assets/_Lair/Data/BalanceConfig.asset.meta > Assets/_Lair/Data/BalanceConfig.asset.meta
```

- [ ] **Step 2: BattleController 밸런스 로딩부 롤백**

`5d52d7e~1` 의 BattleController 밸런스 관련 부분을 참조해 되돌린다 (전체 파일 복원 금지 — ① 서비스 로케이터 변경분이 섞여 있으므로 밸런스 관련 hunk 만):

```csharp
//# 필드: private → SerializeField 로 복원
//# 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
[SerializeField] private BalanceConfig _balance;
```

```csharp
//# Start(): CreateDefault()+BalanceJsonLoader 오버레이 블록 제거 → SO 직접 사용 복원
//# Slice C — BalanceConfig 의 런 길이 적용
if (_balance == null)
{
    Debug.LogError("[BattleController] BalanceConfig(_balance) 미할당 — 프리팹 기본 스탯으로 진행");
}
else
{
    _model.TotalSeconds = _balance.RunDuration;
}
```

`BalanceJsonLoader`/`BalanceConfigDto`/`OverlayFromDto`/`CreateDefault` 호출을 BattleController 에서 전부 제거. `using` 정리.

- [ ] **Step 3: 컴파일 확인**

Unity 에디터로 전환해 컴파일. 콘솔 에러 0건 기대. (`BalanceJsonLoader` 는 Task 3 에서 제거하므로 이 시점엔 아직 존재해도 무방 — 단 BattleController 가 더 이상 참조하지 않아야 함.)

- [ ] **Step 4: 씬 자동 재연결 확인**

Battle 씬 열기 → BattleController 인스펙터의 `Balance` 슬롯에 `BalanceConfig.asset` 이 자동 연결됐는지 확인(GUID 일치). 비어 있으면 수동 드래그.

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Data/BalanceConfig.cs \
        Assets/_Lair/Data/BalanceConfig.asset Assets/_Lair/Data/BalanceConfig.asset.meta \
        Assets/_Lair/Scripts/Battle/BattleController.cs
```

---

## Task 2: 에디터 Syncer 복원 + 현재 밸런스 값 SO 에 반영

**Files:**
- Restore: `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs` (+`.meta`) (← `5d52d7e~1`)
- Modify: `Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs` (balance 동기 버튼 복원)
- Decide/Modify: `BalanceConfigDto` 위치 단일화 (§열린결정 1)

**Interfaces:**
- Consumes: `BalanceConfig`(SO, Task 1), `BalanceConfigDto`.
- Produces: `BalanceConfigSyncer.JsonToSo()` / `SoToJson()` (또는 원본 메서드명), `Lair/JSON Sync` 창의 balance 동기 버튼.

- [ ] **Step 1: Syncer 복원**

```bash
git show 5d52d7e~1:Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs > Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs
git show 5d52d7e~1:Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs.meta > Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs.meta
```

- [ ] **Step 2: DTO 단일화**

`5d52d7e~1` 의 syncer 가 참조하는 DTO 경로를 확인. 현재 런타임에 `Scripts/Data/Dto/BalanceConfigDto.cs` 가 이미 있으므로 **이것을 유일 DTO 로 사용**하고, syncer 의 `using`/네임스페이스를 여기에 맞춘다. `Editor/JsonSync/Dto/BalanceConfigDto.cs` 를 새로 복원하지 않는다(중복 금지). Editor asmdef 가 런타임 `Lair` asmdef 를 참조하는지 확인(참조하면 그대로 사용 가능).

- [ ] **Step 3: LairJsonSyncWindow 에 balance 동기 복원**

`5d52d7e` diff(`LairJsonSyncWindow.cs` 9줄 변경)를 역으로 적용 — 제거됐던 balance 동기 버튼/호출을 복원. `git show 5d52d7e -- Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs` 로 제거된 라인 확인 후 되살린다.

- [ ] **Step 4: 컴파일 확인**

Unity 컴파일 에러 0건.

- [ ] **Step 5: 현재 밸런스 값을 SO 에 반영 (JSON → SO 동기 1회)**

`Lair/JSON Sync` 창에서 balance **JSON → SO** 방향 동기 실행. 이후 `BalanceConfig.asset` 의 값이 현재 `balance_config.json`(hero.hp=4000, activeThresholds=[30,90,150,210,270]) 과 일치하는지 인스펙터로 확인. (복원된 옛 .asset 의 9-threshold 값이 현재 5개로 덮여야 정상.)

- [ ] **Step 6: 스테이징**

```bash
git add Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs.meta \
        Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs \
        Assets/_Lair/Data/BalanceConfig.asset \
        Assets/_Lair/Scripts/Data/Dto/BalanceConfigDto.cs
```

---

## Task 3: 런타임 JSON 로더 · 빌드훅 · gitignore 제거

**Files:**
- Delete: `Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs` (+`.meta`)
- Delete: `Assets/_Lair/Editor/BuildHooks/BalanceJsonBuildCopier.cs` (+`.meta`)
- Delete(조건부): `Assets/_Lair/Editor/BuildHooks/Lair.Editor.BuildHooks.asmdef` (+`.meta`) + 폴더 — BuildHooks 에 다른 파일 없을 때
- Modify: `.gitignore` (StreamingAssets balance 2줄 제거)
- Delete(존재 시): `Assets/StreamingAssets/balance_config.json` (+`.meta`)

- [ ] **Step 1: 로더/빌드훅 제거**

```bash
git rm Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs.meta
git rm Assets/_Lair/Editor/BuildHooks/BalanceJsonBuildCopier.cs Assets/_Lair/Editor/BuildHooks/BalanceJsonBuildCopier.cs.meta
```

- [ ] **Step 2: BuildHooks asmdef/폴더 정리 (조건부)**

`Assets/_Lair/Editor/BuildHooks/` 에 다른 `.cs` 가 없으면 asmdef 와 폴더 제거:

```bash
git rm Assets/_Lair/Editor/BuildHooks/Lair.Editor.BuildHooks.asmdef Assets/_Lair/Editor/BuildHooks/Lair.Editor.BuildHooks.asmdef.meta
```

다른 빌드훅이 있으면 asmdef 유지.

- [ ] **Step 3: .gitignore 정리**

`.gitignore` 에서 다음 블록 제거:

```
#! 밸런스 JSON 정본은 Assets/_Lair/Data/Json — StreamingAssets 사본은 빌드훅(BalanceJsonBuildCopier) 산출물이라 미추적
/Assets/StreamingAssets/balance_config.json
/Assets/StreamingAssets/balance_config.json.meta
```

- [ ] **Step 4: StreamingAssets 산출물 정리**

`Assets/StreamingAssets/balance_config.json`(+`.meta`) 존재 시 삭제(빌드 산출물). git 미추적이면 파일 삭제만.

- [ ] **Step 5: 컴파일 확인**

`BalanceJsonLoader`/`BalanceConfigDto` 잔존 참조로 인한 에러 0건. 남으면 참조부 정리(Task 4 테스트가 주 참조원 — 순서 무관하게 Task 4 와 함께 그린 확인).

- [ ] **Step 6: 스테이징**

```bash
git add .gitignore
git add -A Assets/_Lair/Scripts/Data/ Assets/_Lair/Editor/BuildHooks/
```

---

## Task 4: 밸런스 테스트 정리

**Files:**
- Delete: `Assets/_Lair/Tests/EditMode/Data/BalanceJsonLoaderParseTests.cs` (+`.meta`)
- Delete/Convert: `Assets/_Lair/Tests/EditMode/Data/BalanceShippedJsonRegressionTests.cs` (+`.meta`)
- Modify/Delete: `Assets/_Lair/Tests/EditMode/Data/BalanceConfigTests.cs`, `BalanceConfigOverlayTests.cs`

**Interfaces:**
- Consumes: `BalanceConfig`(SO). 제거 대상 API: `BalanceJsonLoader.Parse/LoadAsync`, `BalanceConfig.CreateDefault/OverlayFromDto`.

- [ ] **Step 1: 로더 파스 테스트 제거**

```bash
git rm Assets/_Lair/Tests/EditMode/Data/BalanceJsonLoaderParseTests.cs Assets/_Lair/Tests/EditMode/Data/BalanceJsonLoaderParseTests.cs.meta
```

- [ ] **Step 2: 출시 JSON 회귀 테스트 처리**

`BalanceShippedJsonRegressionTests` 는 `Data/Json` File.IO + `OverlayFromDto` 전제 → 롤백 후 무의미. 제거:

```bash
git rm Assets/_Lair/Tests/EditMode/Data/BalanceShippedJsonRegressionTests.cs Assets/_Lair/Tests/EditMode/Data/BalanceShippedJsonRegressionTests.cs.meta
```

- [ ] **Step 3: CreateDefault/OverlayFromDto 테스트 처리**

`BalanceConfigTests.cs` / `BalanceConfigOverlayTests.cs` 가 검증하는 `CreateDefault`/`OverlayFromDto` 가 SO 롤백으로 사라지면 해당 테스트를 제거. SO 값 접근(`GetMonster`/`GetSpawnPeriod` 등) 검증은 `5d52d7e~1` 에 대응 테스트가 있었다면 그 버전으로 복원, 없으면 제거. `git show 5d52d7e~1:Assets/_Lair/Tests/EditMode/Data/` 목록으로 이전 테스트 존재 확인.

- [ ] **Step 4: EditMode 테스트 실행**

Unity Test Runner(EditMode) 또는 CLI 로 전체 EditMode 실행 → 그린 확인. 컴파일 에러 0건.

- [ ] **Step 5: 스테이징**

```bash
git add -A Assets/_Lair/Tests/EditMode/Data/
```

---

## Task 5: 문자열 JSON Art/Json → Data/Json 이동

**Files:**
- Move: `Assets/_Lair/Art/Json/Strings_Ko.json` (+`.meta`) → `Assets/_Lair/Data/Json/Strings_Ko.json`
- Move: `Assets/_Lair/Art/Json/LoadingStrings_Ko.json` (+`.meta`) → `Assets/_Lair/Data/Json/LoadingStrings_Ko.json`
- Delete: `Assets/_Lair/Art/Json/.gitkeep` + 빈 폴더
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (주석 경로)
- Modify: `Assets/_Lair/Scripts/Data/StringTableProvider.cs` (주석 경로)

- [ ] **Step 1: .meta 동반 이동 (GUID 보존)**

```bash
git mv Assets/_Lair/Art/Json/Strings_Ko.json Assets/_Lair/Data/Json/Strings_Ko.json
git mv Assets/_Lair/Art/Json/Strings_Ko.json.meta Assets/_Lair/Data/Json/Strings_Ko.json.meta
git mv Assets/_Lair/Art/Json/LoadingStrings_Ko.json Assets/_Lair/Data/Json/LoadingStrings_Ko.json
git mv Assets/_Lair/Art/Json/LoadingStrings_Ko.json.meta Assets/_Lair/Data/Json/LoadingStrings_Ko.json.meta
```

- [ ] **Step 2: 빈 Art/Json 정리**

```bash
git rm Assets/_Lair/Art/Json/.gitkeep
```

폴더가 비면 `Art/Json.meta` 도 정리(Unity 가 빈 폴더 meta 재생성하지 않도록 폴더 삭제).

- [ ] **Step 3: 주석 경로 수정**

`CommonEnum.cs`:

```csharp
Strings_Ko,         //# 게임 전체 CHText 문자열 — Data/Json/Strings_Ko.json
LoadingStrings_Ko,  //# 로딩 설명 텍스트 — Data/Json/LoadingStrings_Ko.json
```

`StringTableProvider.cs` 클래스 주석의 `Art/Json/Strings_Ko.json` → `Data/Json/Strings_Ko.json`.

- [ ] **Step 4: Addressable 로드 확인**

Unity 재생 → 로딩씬에서 `Strings_Ko`/`LoadingStrings_Ko` 로드 성공(콘솔에 "로드 실패" 경고 없음, CHText 문자열·로딩 설명 정상 표시). Addressable Groups 창에서 두 엔트리 주소가 여전히 `Strings_Ko`/`LoadingStrings_Ko` 인지 확인.

- [ ] **Step 5: 스테이징**

```bash
git add -A Assets/_Lair/Art/Json/ Assets/_Lair/Data/Json/Strings_Ko.json Assets/_Lair/Data/Json/Strings_Ko.json.meta \
        Assets/_Lair/Data/Json/LoadingStrings_Ko.json Assets/_Lair/Data/Json/LoadingStrings_Ko.json.meta \
        Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Scripts/Data/StringTableProvider.cs
```

---

## Task 6: 최종 검증

- [ ] **Step 1: 전체 컴파일 그린** — 콘솔 에러 0건. `BalanceJsonLoader`/`BalanceJsonBuildCopier` 잔존 참조 0건(`grep -rn "BalanceJsonLoader\|BalanceJsonBuildCopier" Assets/_Lair --include=*.cs` → 결과 없음).
- [ ] **Step 2: EditMode + PlayMode 테스트 그린.**
- [ ] **Step 3: Battle 씬 재생** — 밸런스가 `BalanceConfig.asset` 값으로 적용(런 길이 300s? 몬스터 스탯·threshold 정상). 인스펙터에서 `_balance` 값 변경 → 재생 시 반영 확인(SO 정본 동작).
- [ ] **Step 4: `Lair/JSON Sync` balance 양방향 동기 동작 확인.**
- [ ] **Step 5: 마무리** — 파이프라인 마무리 단계로. Rule 01 준수: 스테이징된 변경 요약 + 커밋 메시지(안) 제시, 자동 커밋 금지.

---

## Self-Review (spec 대비)

- **2.A 밸런스 롤백** → Task 1(SO+asset+BattleController), Task 2(Syncer+값반영), Task 3(로더/훅/gitignore 제거). ✅ 씬 재와이어링은 GUID 자동 재연결로 Task 1 Step 4 에 흡수.
- **2.B 문자열 이동** → Task 5. ✅
- **3. 테스트 영향** → Task 4. ✅
- **4. 열린 결정**: DTO 단일화 → Task 2 Step 2. CreateDefault/OverlayFromDto 처치 → Task 1(BattleController 제거)+Task 4(테스트 제거). Syncer 스키마 정합 → Task 2 Step 5(현재 json→SO 동기로 확인). ✅
- **6. 검증 기준** → Task 6 에 5항목 매핑. ✅
- **범위 밖 유지**(카드/스킬 SO, ① 서비스 로케이터) → 어느 Task 도 건드리지 않음. Global Constraints 에 명시. ✅

커밋 세분화: writing-plans 기본은 Task 별 commit 이나, Rule 01 로 자동 커밋 금지 → Task 별 스테이징 + 마무리에서 단일 커밋 메시지(안). 의도된 프로젝트 규칙 우선.
