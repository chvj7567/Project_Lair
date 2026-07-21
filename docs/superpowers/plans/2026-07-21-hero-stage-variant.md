# 스켈레톤 영웅 5스테이지 재스킨 시스템 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **주의 — 이 프로젝트는 start-develop 파이프라인을 쓴다.** 실제 코드 authoring 은 gameplay-programmer, 도메인 수치(색상·발광·스케일·스탯 배수)는 game-designer 가 채운다. 본 plan 은 파일 구조·태스크 경계·인터페이스·테스트 기준을 확정한다. "game-designer 확정" 으로 표시된 값은 플레이스홀더가 아니라 **다음 단계 담당자에게 위임된 결정**이다.

**Goal:** 해골 영웅 모델 하나를 셰이더/머터리얼·Transform 으로 재스킨해 5스테이지의 서로 다른 적 + 순차 해금 + 스테이지별 스탯 배수를 만든다.

**Architecture:** SO 정본(`HeroStageVariantConfig`) 이 5스테이지의 외형·스탯 배수를 보유. `HeroStageVariantApplier` 가 스폰 시 Knight 프리팹에 적용. 아웃라인은 스킨드 대응 인버티드-헐 셰이더 1개. 스테이지 선택은 기존 `HeroSelectPopup` 패턴을 복제한 팝업 + `MetaProfile.SelectedStage/ClearedStage` 순차 해금.

**Tech Stack:** Unity 6 (6000.0.68f1) / URP 17.0.4 / C# / com.chvj.unityinfra(ChvjPackage) / NUnit(Unity Test Framework) / Addressables(CHMResource) / MVVM.

## Global Constraints

- 코딩 룰 Rule 00~04 준수 — `//#` 주석, `var` 금지, `!` 금지(`== false`/`== null`), 가드절 무중괄호, MVVM.
- 런타임 스폰은 `CHMPool.Pop/Push` — `Instantiate`/`CreatePrimitive` 금지 (Rule 03 §4).
- 에셋 로드는 Enum 키 + Addressables — 하드코딩 문자열/`Resources/` 금지 (Rule 03 §2, Rule 04 §2).
- UI 는 `CHText`/`CHButton`/`CHToggle` 래퍼 + `CHPoolingScrollView` BuildModalPopup 패턴 (Rule 03 §3).
- **신규 영웅/몬스터/카드 리소스 제작 금지** — 스켈레톤 모델 1개 재사용만 (CLAUDE.md §8).
- **색상 채널 단일화** — variant 틴트와 `HitFlash` 는 동일 `.material` 인스턴스의 `_BaseColor` 채널. `MaterialPropertyBlock` 금지 (spec §5.1).
- 스테이지 식별은 `int`(1~5). `EStage` enum 만들지 않음 (spec §4).
- 자동 커밋 금지 (Rule 01) — 아래 각 태스크의 `git commit` 스텝은 **커밋 메시지(안) 준비**로 해석. 실제 커밋은 사용자 몫. 파이프라인 마무리에서 일괄 스테이징.
- test method 명명: 한글.

---

### Task 1: MetaProfile 에 스테이지 진행 필드 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Meta/MetaProfile.cs`
- Test: `Assets/_Lair/Tests/EditMode/MetaProfileStageTests.cs` (Create)

**Interfaces:**
- Produces:
  - `MetaProfile.SelectedStage : int` (1~5, 기본 1)
  - `MetaProfile.ClearedStage : int` (0~5, 기본 0 = 미클리어)
  - `MetaProfile.CopyFrom` 이 `ClearedStage` 를 복사(클라우드 동기 대상 — 진행 데이터). `SelectedStage` 는 로컬 preference 이나 일관성 위해 함께 복사.
  - `MetaProfile.Version` 은 2 로 증가 (스키마 변경).

**주의:** `CopyFrom` 에 신규 필드를 빠뜨리면 클라우드 복원 시 진행이 소실된다. Version 증가 시 세이브 마이그레이션(`MetaSession`/Store 로드 분기)이 있으면 기본값 채움 분기 확인.

- [ ] **Step 1: 실패 테스트 작성** — `MetaProfileStageTests.cs`

```csharp
using NUnit.Framework;
using Lair.Meta;

public class MetaProfileStageTests
{
    [Test]
    public void 신규_프로필은_SelectedStage_1_ClearedStage_0_이다()
    {
        MetaProfile p = new MetaProfile();
        Assert.AreEqual(1, p.SelectedStage);
        Assert.AreEqual(0, p.ClearedStage);
    }

    [Test]
    public void CopyFrom_은_ClearedStage_와_SelectedStage_를_복사한다()
    {
        MetaProfile src = new MetaProfile { SelectedStage = 3, ClearedStage = 4 };
        MetaProfile dst = new MetaProfile();
        dst.CopyFrom(src);
        Assert.AreEqual(3, dst.SelectedStage);
        Assert.AreEqual(4, dst.ClearedStage);
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — 컴파일 에러(필드 없음) 확인.
- [ ] **Step 3: 최소 구현** — `SelectedStage=1`, `ClearedStage`(기본 0) 필드 추가, `CopyFrom` 에 두 줄 추가, `Version` 을 2 로. 마이그레이션 로드부에서 구버전(Version<2) 프로필은 기본값 유지되게 확인.
- [ ] **Step 4: 테스트 통과 확인.**
- [ ] **Step 5: 커밋 메시지(안) 준비** — `# [feat] - 스테이지 진행(선택/클리어) 세이브 필드 추가`

---

### Task 2: HeroStageVariantConfig SO

**Files:**
- Create: `Assets/_Lair/Scripts/Data/HeroStageVariantConfig.cs`
- Test: `Assets/_Lair/Tests/EditMode/HeroStageVariantConfigTests.cs` (Create)

**Interfaces:**
- Produces:
  - `HeroStageVariantConfig : ScriptableObject` — `IReadOnlyList<HeroStageVariant> Stages`
  - `HeroStageVariant` (Serializable): `Color TintColor`, `bool UseOutline`, `Color OutlineColor`, `bool UseEmission`, `Color EmissionColor`, `float EmissionIntensity`, `float ScaleMultiplier`, `float HpMultiplier`, `float PowerMultiplier`
  - `HeroStageVariantConfig.GetStage(int stage1Based) : HeroStageVariant` — 1~5 를 받아 클램프 후 반환. 범위 밖은 클램프(1↔5).
- Consumes: 없음.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

public class HeroStageVariantConfigTests
{
    [Test]
    public void GetStage_는_1미만이면_1스테이지로_클램프한다()
    {
        HeroStageVariantConfig cfg = ScriptableObject.CreateInstance<HeroStageVariantConfig>();
        cfg.SetStagesForTest(new[] {
            new HeroStageVariant { ScaleMultiplier = 1f },
            new HeroStageVariant { ScaleMultiplier = 2f },
        });
        Assert.AreEqual(1f, cfg.GetStage(0).ScaleMultiplier);
        Assert.AreEqual(2f, cfg.GetStage(99).ScaleMultiplier);
    }
}
```

- [ ] **Step 2: 실패 확인** — 타입 없음.
- [ ] **Step 3: 최소 구현** — `[CreateAssetMenu]` SO + `HeroStageVariant` + `GetStage` 클램프. 테스트 전용 `SetStagesForTest` 는 `#if UNITY_INCLUDE_TESTS` 또는 internal + InternalsVisibleTo 로 노출(프로젝트 관례 따름).
- [ ] **Step 4: 통과 확인.**
- [ ] **Step 5: 커밋 메시지(안)** — `# [feat] - 스테이지별 영웅 외형/스탯 배수 데이터(SO) 추가`

> **game-designer 확정:** 5스테이지 각 `TintColor`/`UseOutline`/`OutlineColor`/`UseEmission`/`EmissionColor`/`EmissionIntensity`/`ScaleMultiplier`/`HpMultiplier`/`PowerMultiplier` 실제 값. 외형 조합은 spec §3 표(1=틴트, 2=+아웃라인, 3=+발광, 4=전부, 5=전부+확대), 스탯 곡선은 컨셉서 §8.

---

### Task 3: 스킨드 대응 아웃라인 셰이더 + 머터리얼 (기법 B)

**Files:**
- Create: `Assets/_Lair/Art/Materials/HeroOutline.shader`
- Create: `Assets/_Lair/Art/Materials/Mat_HeroOutline.mat`

**Interfaces:**
- Produces: 셰이더 프로퍼티 `_OutlineColor`(Color), `_OutlineWidth`(Float). 인버티드-헐 — `Cull Front`, 버텍스 스테이지에서 `positionOS += normalOS * _OutlineWidth` 로 팽창. **스키닝 이후(스킨드 버텍스) 노멀로 팽창**해야 뼈 애니메이션을 따라간다.

- [ ] **Step 1: 셰이더 작성** — URP 호환 인버티드-헐 아웃라인. SkinnedMeshRenderer 의 두 번째 서브머터리얼로 부착되도록 단일 패스, `Cull Front`, 깊이 쓰기 유지.
- [ ] **Step 2: 컴파일 확인** — Unity 콘솔에 셰이더 에러 0. `Mat_HeroOutline` 생성 후 인스펙터에서 `_OutlineColor`/`_OutlineWidth` 노출 확인.
- [ ] **Step 3: 스켈레톤 애니메이션 중 아웃라인이 실루엣을 따라가는지 씬뷰 육안 확인** (idle/walk).
- [ ] **Step 4: 커밋 메시지(안)** — `# [asset] - 스테이지 정예 영웅용 아웃라인 셰이더/머터리얼 추가`

> **검증 한계:** 셰이더는 자동 단위테스트가 어렵다 — 컴파일 + 씬뷰 육안이 게이트. test-engineer 단계에서 이 태스크는 회귀 대상 아님(에셋).

---

### Task 4: HitFlash — variant 색을 원본으로 취급 (색상 충돌 해소)

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/HitFlash.cs`
- Test: `Assets/_Lair/Tests/EditMode/HitFlashVariantTests.cs` (Create)

**Interfaces:**
- Produces: `HitFlash.SetBaselineColor(Color baseline)` — variant 적용부(Task 5)가 호출. 내부 `_originalColors` 를 이 색으로 (재)설정하여 이후 모든 `RestoreOriginalColors()`(피격/공격 flash 종료·`OnEnable` 풀 재사용)가 variant 색으로 원복되게 한다.
- Consumes: 없음.

**근거(spec §5.1):** 현재 `CacheRenderers()` 는 Awake 1회 스냅샷 + `OnEnable`/flash 후 restore. variant 를 이 스냅샷에 반영하지 않으면 첫 피격·풀 재사용마다 틴트 소실.

- [ ] **Step 1: 실패 테스트 작성** — 합성 Renderer + Material 로 HitFlash 를 세운 뒤, `SetBaselineColor(red)` 호출 → 피격 flash 시뮬레이션(감소 이벤트) → `RestoreOriginalColors` 경로 후 `_BaseColor == red` 검증. `OnEnable` 재호출(풀 재사용) 후에도 red 유지 검증.

```csharp
[Test]
public void SetBaselineColor_후_피격_원복하면_틴트가_유지된다()
{
    //# 합성 GameObject + MeshRenderer + Material(_BaseColor) 구성 후 HitFlash 부착
    //# SetBaselineColor(Color.red) → HandleChanged 로 데미지 유발 → flash 종료 → _BaseColor == red
}
```

- [ ] **Step 2: 실패 확인** — `SetBaselineColor` 없음.
- [ ] **Step 3: 최소 구현** — `SetBaselineColor(Color)` 추가: `_originalColors` 의 모든 항목을 baseline 으로 덮고 즉시 `RestoreOriginalColors()`(현재 flash 중이면 종료 후). 기존 flash/attack 로직 불변.
- [ ] **Step 4: 통과 확인.**
- [ ] **Step 5: 커밋 메시지(안)** — `# [fix] - 스테이지 틴트가 피격 연출/풀 재사용 후에도 유지되도록 수정`

---

### Task 5: HeroStageVariantApplier 컴포넌트

**Files:**
- Create: `Assets/_Lair/Scripts/Character/HeroStageVariantApplier.cs`
- Test: `Assets/_Lair/Tests/EditMode/HeroStageVariantApplierTests.cs` (Create)

**Interfaces:**
- Consumes: `HeroStageVariant`(Task 2), `HitFlash.SetBaselineColor`(Task 4).
- Produces: `HeroStageVariantApplier.Apply(HeroStageVariant variant)` — 자식 Renderer 의 `.material` 인스턴스에 `_BaseColor`=TintColor, (UseEmission 이면) `_EmissionColor`·`EmissionEnabled`, (UseOutline 이면) 아웃라인 서브머터리얼 슬롯 활성 + `_OutlineColor`, root Transform `localScale *= ScaleMultiplier`. 마지막에 `HitFlash.SetBaselineColor(TintColor)` 호출(있으면).

**Rule 02 §5:** `HitFlash` 는 `[SerializeField]` 또는 `Awake` 1회 캐싱으로 참조(런타임 `GetComponent` 반복 금지). 아웃라인 서브머터리얼은 인스펙터 참조(Task 8 에서 프리팹 와이어링).

- [ ] **Step 1: 실패 테스트 작성** — 합성 Renderer + HitFlash 로 `Apply(variant{TintColor=green})` 후 material `_BaseColor==green` 및 HitFlash baseline==green 검증. `ScaleMultiplier=2` 면 localScale 2배 검증.
- [ ] **Step 2: 실패 확인.**
- [ ] **Step 3: 최소 구현.**
- [ ] **Step 4: 통과 확인.**
- [ ] **Step 5: 커밋 메시지(안)** — `# [feat] - 스테이지별 영웅 외형(색/발광/아웃라인/크기) 적용 컴포넌트 추가`

---

### Task 6: BattleController — 스테이지 반영 (외형 + 스탯 배수 + 클리어 갱신)

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (`SpawnHero` ~442-489, 승리 처리 `EndBattle`)
- Test: `Assets/_Lair/Tests/EditMode/StageProgressionTests.cs` (Create)

**Interfaces:**
- Consumes: `MetaProfile.SelectedStage/ClearedStage`(Task 1), `HeroStageVariantConfig.GetStage`(Task 2), `HeroStageVariantApplier.Apply`(Task 5), `MetaSession.GetOrLoad()`.
- Produces: 순수 진행 헬퍼 `StageProgress.ResolveClearedStage(int cleared, int justClearedStage) : int` = `Max(cleared, justClearedStage)`, 5 초과 없음. (테스트 가능하도록 static 순수 함수로 분리 — BattleController 는 이를 호출.)

**적용 지점:** `SpawnHero` 의 `ApplyStats(p.gameObject, _balance.Hero)`(480) 직후 — `GetStage(profile.SelectedStage)` 로 variant 를 얻어 `HeroStageVariantApplier.Apply` 호출 + HP/공격력에 `HpMultiplier`/`PowerMultiplier` 곱. 승리(`EndBattle(BattleResult.Win)`) 시 `profile.ClearedStage = StageProgress.ResolveClearedStage(...)` 후 세이브.

- [ ] **Step 1: 실패 테스트 작성** — `StageProgressionTests`

```csharp
[Test]
public void 더_높은_스테이지_클리어시_ClearedStage_갱신되고_5를_넘지_않는다()
{
    Assert.AreEqual(3, StageProgress.ResolveClearedStage(2, 3));
    Assert.AreEqual(4, StageProgress.ResolveClearedStage(4, 2)); //# 낮은 재도전은 유지
    Assert.AreEqual(5, StageProgress.ResolveClearedStage(5, 5)); //# 5 종점
}
```

- [ ] **Step 2: 실패 확인.**
- [ ] **Step 3: 최소 구현** — `StageProgress` static + `SpawnHero`/`EndBattle` 배선. 스탯 배수는 기존 `ApplyStats` 결과에 곱하는 방식(복리 버그 없이 baseline×mul).
- [ ] **Step 4: 통과 확인** (순수 헬퍼 EditMode; 스폰 배선은 PlayMode 스모크는 test-engineer 단계에서).
- [ ] **Step 5: 커밋 메시지(안)** — `# [feat] - 선택 스테이지에 따라 영웅 외형/강함이 달라지고 클리어 시 다음 스테이지 해금`

---

### Task 7: 스테이지 선택 팝업 (3-class + EUI + 마을 진입)

**Files:**
- Create: `Assets/_Lair/Scripts/UI/Village/StageSelectPopup.cs` (Panel + Arg)
- Create: `Assets/_Lair/Scripts/UI/Village/StageSelectPoolingScrollView.cs`
- Create: `Assets/_Lair/Scripts/UI/Village/StageSelectCell.cs`
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (`EUI.StageSelectPopup` 추가)
- Modify: `Assets/_Lair/Scripts/Village/VillageController.cs` (팝업 진입 + `HandleStageSelected`)
- Test: `Assets/_Lair/Tests/EditMode/StageSelectCellDataTests.cs` (Create)

**Interfaces:**
- Consumes: `MetaProfile.SelectedStage/ClearedStage`(Task 1).
- Produces:
  - `StageSelectCellData { int Stage; bool IsLocked; bool IsSelected; Action<int> OnSelect; }`
  - `StageSelectPopup.BuildCellData(MetaProfile profile) : List<StageSelectCellData>` — 5셀 생성. `stage <= profile.ClearedStage + 1` 이면 해금, 그 이상 잠금. `IsSelected = (stage == profile.SelectedStage)`.
- 패턴: Rule 03 §3 BuildModalPopup — Panel `[SerializeField] _scrollView`, ScrollView `CHPoolingScrollView<StageSelectCell, StageSelectCellData>`, Cell `[SerializeField]` 위젯 + `Bind`/`OnEnable` 리셋. `HeroSelectPopup` 3-class 를 템플릿으로.

- [ ] **Step 1: 실패 테스트 작성** — `StageSelectCellDataTests`

```csharp
[Test]
public void ClearedStage_2면_스테이지_3까지_해금되고_4_5는_잠금이다()
{
    MetaProfile p = new MetaProfile { ClearedStage = 2, SelectedStage = 1 };
    var cells = StageSelectPopup.BuildCellData(p);
    Assert.AreEqual(5, cells.Count);
    Assert.IsFalse(cells[2].IsLocked); //# stage 3 해금
    Assert.IsTrue(cells[3].IsLocked);  //# stage 4 잠금
    Assert.IsTrue(cells[4].IsLocked);  //# stage 5 잠금
}
```

- [ ] **Step 2: 실패 확인.**
- [ ] **Step 3: 최소 구현** — 3-class + `BuildCellData` + `EUI.StageSelectPopup` + `VillageController` 진입(기존 `EUI.HeroSelectPopup` 케이스 옆). 선택 시 `profile.SelectedStage = stage` 저장 후 Battle 씬 로드(`HandleHeroSelected` 흐름 동형).
- [ ] **Step 4: 통과 확인.**
- [ ] **Step 5: 커밋 메시지(안)** — `# [feat] - 마을에서 스테이지를 골라 입장(순차 해금, 잠금 슬롯 표시)`

> **프리팹(Task 8 에서 제작):** `StageSelectPopup.prefab` + `StageSelectCell.prefab`. UIBase/CHPoolingScrollView 정적 배치, 코드 동적 생성 금지 (Rule 03 §3).

---

### Task 8: 에셋 배선 — SO asset + 프리팹 와이어링 + Addressable

**Files:**
- Create: `Assets/_Lair/Art/Cards/`? → 데이터 성격이므로 `Assets/_Lair/Data/HeroStageVariantConfig.asset` (BalanceConfig.asset 옆, 비-Addressable) 또는 BattleController 인스펙터 참조. 위치는 game-designer/gameplay-programmer 합의.
- Create: `Assets/_Lair/Art/UI/StageSelectPopup.prefab`, `Assets/_Lair/Art/UI/StageSelectCell.prefab` (+ .meta, Addressable 주소 = 파일명 = `EUI` 값명)
- Modify: `Knight.prefab` — `HeroStageVariantApplier` 부착, 아웃라인 서브머터리얼 슬롯(`Mat_HeroOutline`) 추가, applier 의 HitFlash/Renderer/outline 참조 인스펙터 와이어링.
- Modify: BattleController/VillageController 인스펙터 — `HeroStageVariantConfig`/팝업 참조.

**Interfaces:** 앞 태스크 산출물을 실제 에셋으로 연결. 코드 신규 없음(에셋 사이클).

- [ ] **Step 1: `HeroStageVariantConfig.asset` 생성** — game-designer 확정 5스테이지 값 입력.
- [ ] **Step 2: `StageSelectPopup.prefab`/`StageSelectCell.prefab` 제작** — `HeroSelectPopup.prefab` 구조 복제(BuildModalPopup), `_scrollView`/`_origin`/Cell 위젯 참조 연결. Addressable 등록(주소=파일명, 라벨=`Resource`).
- [ ] **Step 3: `Knight.prefab` 와이어링** — `HeroStageVariantApplier` 부착 + HitFlash/Renderer/아웃라인 서브머터리얼 참조. 스켈레톤 SkinnedMeshRenderer 의 materials 배열에 `Mat_HeroOutline` 슬롯 추가(기본 비활성/투명, applier 가 스테이지별 토글).
- [ ] **Step 4: 인스펙터 참조** — BattleController 에 `HeroStageVariantConfig`, VillageController 에 팝업 진입 배선.
- [ ] **Step 5: 플레이 검증** — 스테이지 1~5 진입 시 외형(틴트/아웃라인/발광/스케일 누적) + 스탯 배수 + 해금 동작 육안 확인. 피격/풀 재사용 후 틴트 유지 확인(spec §8 회귀).
- [ ] **Step 6: 커밋 메시지(안)** — `# [asset] - 5스테이지 영웅 외형 데이터/스테이지 선택 UI/프리팹 배선`

---

## Self-Review

**Spec coverage:**
- §2 A(재스킨) → Task 2,3,5,8 · B(선택/해금) → Task 1,7 · C(스탯 배수) → Task 2,6 ✅
- §3 외형 표(기법 A/B/C/D) → Task 2(데이터)·3(B 셰이더)·5(적용)·8(값) ✅
- §4 SO 정본 + int 식별 → Task 1,2 ✅
- §5.1 색상 충돌 → Task 4,5 (최우선) ✅
- §6 선택/해금/BattleController/승리갱신 → Task 1,6,7 ✅
- §6.1 5 종점 → Task 6 `ResolveClearedStage` (5 초과 없음) ✅
- §7 영향 파일 전부 태스크에 매핑 ✅

**Placeholder scan:** "game-designer 확정"/"검증 한계" 는 파이프라인 위임 표시(플레이스홀더 아님). TBD/TODO 없음. ✅

**Type consistency:** `SelectedStage`/`ClearedStage`(int), `HeroStageVariant`/`GetStage`, `SetBaselineColor(Color)`, `Apply(HeroStageVariant)`, `ResolveClearedStage(int,int)`, `BuildCellData(MetaProfile)` — 태스크 간 명칭·시그니처 일치 확인. ✅

**주의 잔여 리스크:**
- Task 1 Version 증가 → 기존 세이브 마이그레이션 경로 확인 필수(구버전 로드 시 기본값).
- Task 3 셰이더는 스킨드 팽창 — 정적 아웃라인으로 짜면 애니메이션 중 어긋남.
- Task 8 아웃라인 서브머터리얼을 SkinnedMeshRenderer 에 추가 시 submesh 개수/머터리얼 슬롯 매핑 주의.
