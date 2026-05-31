# 카드 전체 리뉴얼 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **For start-develop 흐름:** 이 plan 의 Phase 1·3 은 spec 의 결정 락만으로 완전 명세됨. Phase 2 (카드 28장 효과) 는 **game-designer 가 작성할 기획서 `docs/design/card-renewal.md` 의 카드 라인업·수치를 입력으로** 받아 채운다. game-designer 단계를 거치지 않고 슈퍼파워 실행으로 가려면, Phase 2 시작 전 plan 작성자가 카드 라인업 28장 표 + 수치를 plan 안에 추가해야 한다.

**Goal:** Project Lair 의 카드 28장을 4축 빌드 시너지 구조 위에 재배치하고, 같은 축 N장 픽 시 단계별 시너지(3/5/7장 임계)와 같은 카드 중첩 강화를 동시에 작동시키는 시스템을 도입한다.

**Architecture:**
- 데이터: `ECardCategory`(4종) → `EBuildAxis`(4종 — Tank/Dps/Debuff/Swarm) Enum 교체. `CardData._category` 필드 EBuildAxis 로 전환. SO 28장 일괄 마이그레이션.
- 시스템: `BuildSynergyService` 신설 (한 라운드의 픽 카운트 추적 + Tier 3/5/7 임계 도달 시 즉시 Apply). `IBattleContext` 에 `RegisterCardPick(EBuildAxis)` 표면 추가. 기존 곱연산 누적 (`RegisterMonsterTypeBuff` 등) 은 Layer 2 카드 중첩 도구로 그대로 재사용.
- 카드: 28장 효과 코드는 ICardEffect 인터페이스 패턴 유지. Multiply 삭제. Plague Spawner 추가 (`SpawnerConfig.asset`).
- 문서: 컨셉서 §11.3·§11.4·§5.2, `continuous-spawn-round.md` §3.1·§7 동기 갱신.

**Tech Stack:** Unity 6 (6000.0.68f1) / C# / MVVM / NUnit (Unity Test Framework) / Addressables / ChvjPackage.

**Rule 준수**: Rule 00~04. **Rule 01** — `git commit` 직접 실행 금지. 본 plan 의 commit step 은 모두 `git add` 까지만, 최종 Phase 3 끝에 사용자 승인 받아 일괄 커밋.

---

## File Structure

### 신규 파일
| 경로 | 책임 |
|---|---|
| `Assets/_Lair/Scripts/Card/BuildSynergyService.cs` | 한 라운드 축별 픽 카운트 + Tier 임계 도달 시 Apply 호출 (POCO, BattleController 가 보유) |
| `Assets/_Lair/Scripts/Card/CommonInterface.BuildSynergy.cs` (분할) | `IBuildSynergyTier` 인터페이스 — Tier 발동 시 IBattleContext 받아 효과 적용 |
| `Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier1.cs` 등 12개 | 축 4 × Tier 3 = 12 시너지 효과 클래스 (game-designer 가 수치 채움) |
| `Assets/_Lair/Tests/EditMode/Card/BuildSynergyServiceTests.cs` | BuildSynergy 단위 테스트 |
| `Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs` | Enum 정합 회귀 테스트 |

### 수정 파일
| 경로 | 변경 종류 |
|---|---|
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `ECardCategory` 삭제 → `EBuildAxis { Tank, Dps, Debuff, Swarm }` 추가. `ECardId` 에 신규 3장 enum 값 추가 (끝에 — int 직렬화 정합) |
| `Assets/_Lair/Scripts/Card/CardData.cs` | `_category` 필드 타입 `ECardCategory` → `EBuildAxis`. 프로퍼티 `Category` → `Axis` |
| `Assets/_Lair/Scripts/Card/CommonInterface.cs` | `IBattleContext` 에 `RegisterCardPick(EBuildAxis)`, `GetBuildCount(EBuildAxis)` 추가 |
| `Assets/_Lair/Scripts/Battle/BattleContext.cs` | 위 두 메서드 구현, `BuildSynergyService` 위임 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | `BuildSynergyService` 인스턴스화 + 라운드 시작 시 초기화 + 카드 픽 hook 에서 `RegisterCardPick` 호출 |
| `Assets/_Lair/Scripts/Card/Effects/*.cs` 25개 | 기존 효과 보존(메커니즘만 재사용)·리뉴얼·삭제 (구체는 Phase 2) |
| `Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs` | **삭제** |
| `Assets/_Lair/Scripts/Card/Effects/*` 신규 N장 | game-designer 기획서대로 신규 효과 클래스 추가 |
| `Assets/_Lair/Data/Json/cards.json` | 28장 항목 + `axis` 필드 (4축) |
| `Assets/_Lair/Data/Json/card_pools.json` | Passive 16장 / Active 12장 풀 구성 |
| `Assets/_Lair/Art/Cards/Items/*.asset` 25개 | `_category` 필드 → `_axis` (EBuildAxis int) 마이그레이션 + Multiply.asset 삭제 |
| `Assets/_Lair/Art/Cards/Items/*.asset` 신규 3장 | 신규 카드 SO 생성 |
| `Assets/_Lair/Art/Cards/CardPool_Passive.asset` | 카드 ref 15 → 16 |
| `Assets/_Lair/Art/Cards/CardPool_Active.asset` | 카드 ref 10 → 12 (Multiply 제거 + 신규 3장 추가) |
| `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` | 카테고리 색상 매핑 7→4 (Rule 03 §3 CHText/CHButton 유지) |
| `Assets/_Lair/Art/Cards/SpawnerConfig.asset` (또는 동등 위치) | Wisp 스포너 2개 중 1개를 Plague 스포너로 전환 |
| `Assets/_Lair/Tests/EditMode/**` | 효과 단위 테스트 갱신·신규 |
| `Assets/_Lair/Tests/PlayMode/SimPickStrategy.cs` | 4축 우선 픽 전략으로 갱신 |
| `Assets/_Lair/Tests/PlayMode/ContinuousSpawnIntegrationTest.cs` | Plague Spawner 통합 시나리오 추가 |
| `docs/design/project_lair_concept.md` | §4.2 트리거 유지, §5.2 시너지 방향성 4축으로 재정렬, §11.3 카드 정의 28장으로 재작성, §11.4 카드 테두리 색 4색 |
| `docs/design/continuous-spawn-round.md` | §3.1 Spawner 구성 (Wisp 2 → Wisp 1 + Plague 1) · §7 Plague no-op 문구 제거 |

### 삭제 파일
- `Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs` (+ `.meta`)
- `Assets/_Lair/Art/Cards/Items/Multiply.asset` (+ `.meta`)
- `Assets/_Lair/Data/Json/cards.json` 내 Multiply 항목

---

## Phase 분리

| Phase | 입력 의존 | 산출물 |
|---|---|---|
| **Phase 1 — 시너지 시스템 코어** | spec 결정 락 D2~D9 만 의존 (수치 무관) | EBuildAxis Enum · BuildSynergyService · IBattleContext 확장 · BattleController 통합 · Plague Spawner 추가 |
| **Phase 2 — 카드 콘텐츠** | game-designer 의 카드 28장 라인업 표 + 효과 수치 (`docs/design/card-renewal.md`) | 28장 효과 코드 · SO asset · json · 12개 시너지 Tier 효과 · 카드 색 매핑 |
| **Phase 3 — 통합 검증 + 문서** | Phase 2 산출물 | 컨셉서·continuous-spawn-round.md 갱신 · PlayMode 통합 테스트 · verification gate · 한글 커밋 메시지(안) |

---

## Phase 1 — 시너지 시스템 코어

### Task 1: EBuildAxis Enum 도입 + ECardCategory 제거 준비

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs` (Create)

- [ ] **Step 1: Write failing test — Enum 값 순서·이름**

`Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs`:
```csharp
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode.Card
{
    public class EBuildAxisMigrationTests
    {
        [Test]
        public void EBuildAxis_네_값_순서_고정()
        {
            Assert.AreEqual(0, (int)EBuildAxis.Tank);
            Assert.AreEqual(1, (int)EBuildAxis.Dps);
            Assert.AreEqual(2, (int)EBuildAxis.Debuff);
            Assert.AreEqual(3, (int)EBuildAxis.Swarm);
        }
    }
}
```

- [ ] **Step 2: Run test — 컴파일 실패 예상**

Run: Unity Test Runner → EditMode → `EBuildAxisMigrationTests`
Expected: Compile error — `EBuildAxis` not found.

- [ ] **Step 3: Add EBuildAxis enum**

`Assets/_Lair/Scripts/Data/CommonEnum.cs` 의 `ECardCategory` 정의 아래에 추가:
```csharp
    //# 카드 빌드 축 — 카드 리뉴얼(2026-05-31) 으로 ECardCategory 를 대체.
    //# 순서 절대 변경 금지 — CardData._axis (int 직렬화) 와 1:1 대응.
    public enum EBuildAxis
    {
        Tank,    //# 탱커/포위 — Wisp + Wraith 중심
        Dps,     //# 순수 DPS — Reaper + Hex 중심
        Debuff,  //# 디버프 누적 — Plague + 액티브 저주 콤보 (둔화/속박 포함)
        Swarm,   //# 수적 압박 — Phantom 중심
    }
```

`ECardCategory` 는 **이 Task 에서 제거하지 않는다** — Task 4 에서 모든 참조를 EBuildAxis 로 옮긴 뒤 일괄 제거.

- [ ] **Step 4: Run test — 통과 확인**

Expected: Pass.

- [ ] **Step 5: 스테이징 (Rule 01)**

`git add Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs.meta`

---

### Task 2: BuildSynergyService 코어 (카운트 추적)

**Files:**
- Create: `Assets/_Lair/Scripts/Card/BuildSynergyService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/BuildSynergyServiceTests.cs`

- [ ] **Step 1: Write failing test — 픽 카운트 추적**

```csharp
using Lair.Card;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode.Card
{
    public class BuildSynergyServiceTests
    {
        [Test]
        public void RegisterPick_같은축_카운트_누적()
        {
            BuildSynergyService sut = new BuildSynergyService();
            sut.RegisterPick(EBuildAxis.Tank);
            sut.RegisterPick(EBuildAxis.Tank);
            sut.RegisterPick(EBuildAxis.Dps);
            Assert.AreEqual(2, sut.GetCount(EBuildAxis.Tank));
            Assert.AreEqual(1, sut.GetCount(EBuildAxis.Dps));
            Assert.AreEqual(0, sut.GetCount(EBuildAxis.Debuff));
        }
    }
}
```

- [ ] **Step 2: Run test — 컴파일 실패 예상**

Expected: `BuildSynergyService not found`.

- [ ] **Step 3: 최소 구현**

`Assets/_Lair/Scripts/Card/BuildSynergyService.cs`:
```csharp
using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Card
{
    //# 한 라운드 빌드 축 픽 카운트 추적 + Tier 임계(3/5/7) 도달 시 즉시 Apply.
    //# POCO. BattleController 가 보유. 라운드 시작 시 Reset.
    public class BuildSynergyService
    {
        private readonly Dictionary<EBuildAxis, int> _counts = new Dictionary<EBuildAxis, int>();

        public void RegisterPick(EBuildAxis axis)
        {
            int prev;
            _counts.TryGetValue(axis, out prev);
            _counts[axis] = prev + 1;
        }

        public int GetCount(EBuildAxis axis)
        {
            int v;
            return _counts.TryGetValue(axis, out v) ? v : 0;
        }

        public void Reset()
        {
            _counts.Clear();
        }
    }
}
```

- [ ] **Step 4: Run test — 통과 확인**

Expected: Pass.

- [ ] **Step 5: 스테이징**

`git add Assets/_Lair/Scripts/Card/BuildSynergyService.cs Assets/_Lair/Scripts/Card/BuildSynergyService.cs.meta Assets/_Lair/Tests/EditMode/Card/BuildSynergyServiceTests.cs Assets/_Lair/Tests/EditMode/Card/BuildSynergyServiceTests.cs.meta`

---

### Task 3: IBuildSynergyTier 인터페이스 + 임계 도달 발화 로직

**Files:**
- Create: `Assets/_Lair/Scripts/Card/CommonInterface.BuildSynergy.cs`
- Modify: `Assets/_Lair/Scripts/Card/BuildSynergyService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/BuildSynergyServiceTests.cs`

- [ ] **Step 1: Write failing test — Tier 3 임계 도달 시 1회 Apply**

`BuildSynergyServiceTests.cs` 에 추가:
```csharp
        [Test]
        public void RegisterPick_3장_도달_시_Tier1_1회_Apply()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankTier1 = new FakeTier();
            BuildSynergyService sut = new BuildSynergyService();
            sut.BindTier(EBuildAxis.Tank, threshold: 3, tier: tankTier1);

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(0, tankTier1.AppliedCount);   //# 2장 — 아직

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tankTier1.AppliedCount);   //# 3장 — 발화 1회

            sut.RegisterPick(EBuildAxis.Tank, ctx);
            Assert.AreEqual(1, tankTier1.AppliedCount);   //# 4장 — 재발화 X
        }

        private class FakeTier : IBuildSynergyTier
        {
            public int AppliedCount { get; private set; }
            public void Apply(IBattleContext ctx) => ++AppliedCount;
        }

        private class FakeBattleContext : IBattleContext { /* … 최소 stub. 메서드 모두 빈 구현 또는 throw. */ }
```

> Note: FakeBattleContext 는 `IBattleContext` 의 16개 메서드를 모두 stub. 본 plan 의 Task 4 에서 `RegisterCardPick` 추가하면 그것도 stub.

- [ ] **Step 2: Run — 컴파일 실패 예상**

Expected: `IBuildSynergyTier`, `BindTier`, 2-arg `RegisterPick` 미정의.

- [ ] **Step 3: 인터페이스 + Bind + 임계 발화 구현**

`Assets/_Lair/Scripts/Card/CommonInterface.BuildSynergy.cs`:
```csharp
namespace Lair.Card
{
    //# 빌드 시너지 Tier 효과 — 같은 축 N장 임계 도달 시 BuildSynergyService 가 호출.
    //# Apply 안에서 IBattleContext 의 RegisterMonsterTypeBuff/AddMonsterBuff 등 표면 호출.
    public interface IBuildSynergyTier
    {
        void Apply(IBattleContext ctx);
    }
}
```

`BuildSynergyService.cs` 에 추가:
```csharp
        private readonly Dictionary<(EBuildAxis, int), IBuildSynergyTier> _tiers
            = new Dictionary<(EBuildAxis, int), IBuildSynergyTier>();

        //# 부팅 시(BattleController) 한 번 호출. axis×threshold 당 1개 Tier 바인딩.
        public void BindTier(EBuildAxis axis, int threshold, IBuildSynergyTier tier)
        {
            _tiers[(axis, threshold)] = tier;
        }

        //# 픽 발생 시 호출. 카운트 증가 후 새로 도달한 임계가 있으면 1회 Apply.
        public void RegisterPick(EBuildAxis axis, IBattleContext ctx)
        {
            int prev = GetCount(axis);
            int next = prev + 1;
            _counts[axis] = next;

            //# Spec D6: 임계 도달 시 즉시 발화. 같은 임계는 1회만.
            IBuildSynergyTier tier;
            if (_tiers.TryGetValue((axis, next), out tier))
            {
                tier.Apply(ctx);
            }
        }
```

기존 1-arg `RegisterPick(EBuildAxis)` 는 보존 (테스트·UI 표시용 — Apply 없는 카운트만 누적).

- [ ] **Step 4: Run — 통과 확인**

Expected: 두 테스트 Pass.

- [ ] **Step 5: 스테이징**

`git add` 위 두 수정 파일 + 신규 인터페이스 파일 (`.meta` 동행).

---

### Task 4: IBattleContext 확장 + CardData._axis 필드 전환

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Card/CardData.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleContext.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (ECardCategory 삭제)

- [ ] **Step 1: Write failing test — BattleContext.RegisterCardPick 위임**

`BuildSynergyServiceTests.cs` 에 추가하지 말고, `Assets/_Lair/Tests/EditMode/Battle/BattleContextCardPickTests.cs` 신설:
```csharp
[Test]
public void RegisterCardPick_BuildSynergyService_위임_확인()
{
    BuildSynergyService syn = new BuildSynergyService();
    BattleContext ctx = new BattleContext( /* … 의존성 stub …*/, syn);
    ctx.RegisterCardPick(EBuildAxis.Tank);
    Assert.AreEqual(1, syn.GetCount(EBuildAxis.Tank));
}
```
> 구체 시그니처는 기존 `BattleContext` 생성자 구조 확인 후 동일 패턴으로.

- [ ] **Step 2: Run — 컴파일 실패 예상**

`IBattleContext.RegisterCardPick`, `BattleContext` 생성자에 syn 인자 없음.

- [ ] **Step 3: `IBattleContext` 표면 확장**

`CommonInterface.cs` 의 `IBattleContext` 에 추가:
```csharp
        //# 카드 픽 시 호출 — BuildSynergyService 에 카운트 등록 + 임계 도달 시 시너지 발화.
        //# 호출 시점: 패시브/액티브 카드 선택 직후, ICardEffect.Apply 직전.
        void RegisterCardPick(EBuildAxis axis);

        //# 빌드 카운트 조회 (UI/시너지 카드 효과 내부 조건문에서 사용).
        int GetBuildCount(EBuildAxis axis);
```

`CardData.cs`:
```csharp
        //# Spec D3: ECardCategory → EBuildAxis 전환. 필드명·접근자도 _axis/Axis 로 변경.
        [SerializeField] private EBuildAxis _axis;
        public EBuildAxis Axis => _axis;
```
`_category` / `Category` 는 같은 파일에서 삭제 — 모든 참조 컴파일 에러 발생, 다음 task 들에서 조각조각 갱신할 수 없으니 한 번에 마이그레이션.

`BattleContext.cs`:
```csharp
        private readonly BuildSynergyService _synergy;
        //# 생성자에 syn 추가 (BattleController 가 주입).
        public BattleContext(/* 기존 인자들 */, BuildSynergyService synergy)
        {
            //# 기존 초기화 …
            _synergy = synergy;
        }
        public void RegisterCardPick(EBuildAxis axis) => _synergy.RegisterPick(axis, this);
        public int GetBuildCount(EBuildAxis axis) => _synergy.GetCount(axis);
```

`BattleController.cs`:
- 필드 `private BuildSynergyService _synergy;` 추가
- `Start()` (또는 동등 진입점) 에서 `_synergy = new BuildSynergyService();` + 12개 Tier 바인딩 (Phase 2 에서 실제 인스턴스 채움, Phase 1 에선 빈 dict 로 두고 컴파일만)
- 라운드 시작/Restart 시 `_synergy.Reset()`
- 카드 선택 hook (CardSelectionPopup → 콜백) 에서 `_battleContext.RegisterCardPick(card.Axis)` 호출 후 `card.Effect.Apply(_battleContext)`.

`CommonEnum.cs`:
- `ECardCategory` enum 정의 삭제.

- [ ] **Step 4: 전체 컴파일 + 기존 테스트 + 신규 테스트 통과**

Run: Unity Test Runner → 전체 EditMode.
Expected: 모든 테스트 Pass. `ECardCategory` 참조한 코드 모두 마이그레이션 완료 (LairCardPrefabBuilder 도 동일 task 에서 EBuildAxis 4색으로 갱신 — Task 13 에서 디테일).

> 카드 SO 의 `_category` 필드는 이 시점에 직렬화 desync — Unity 가 default(0)=Tank 로 읽음. SO 마이그레이션은 Task 12. 일시적으로 모든 카드가 Tank 로 분류돼도 컴파일은 됨.

- [ ] **Step 5: 스테이징**

`git add` 위 모든 수정 파일.

---

### Task 5: Plague Spawner 추가 (SpawnerConfig)

**Files:**
- Modify: `Assets/_Lair/Art/Cards/SpawnerConfig.asset` (또는 `docs/design/continuous-spawn-round.md` §3.1 에서 정의한 위치)

> 정확 경로는 game-designer 단계 또는 본 plan 작성자가 다음 명령으로 확인:
> `Glob: "**/*SpawnerConfig*.asset"` 또는 `Grep: "SpawnerConfig"`

- [ ] **Step 1: Write failing test — Plague Spawner ≥ 1 검증**

`Assets/_Lair/Tests/EditMode/Battle/SpawnerConfigTests.cs` (신규):
```csharp
[Test]
public void SpawnerConfig_Plague_스포너_최소_1개()
{
    SpawnerConfig cfg = AssetDatabase.LoadAssetAtPath<SpawnerConfig>("Assets/_Lair/Art/Cards/SpawnerConfig.asset");
    int plagueCount = 0;
    foreach (SpawnerConfig.Entry e in cfg.Entries)
        if (e.MonsterType == EMonster.Plague) ++plagueCount;
    Assert.GreaterOrEqual(plagueCount, 1, "Plague Spawner 가 디버프 축 작동의 전제조건");
}
```
> SpawnerConfig 의 실제 클래스명·필드명은 기존 코드에 맞춰 보정.

- [ ] **Step 2: Run — 실패 예상 (Wisp 2개 / Plague 0개)**

- [ ] **Step 3: SpawnerConfig.asset 수정**

`docs/design/continuous-spawn-round.md` §3.1 의 Spawner 6개 중 Wisp Spawner #4(180°) 를 Plague 로 전환. Editor 에서 SpawnerConfig.asset 열고 해당 entry 의 MonsterType 을 Plague 로 변경. (또는 YAML 직접 수정 — `_monsterType: 4` (EMonster.Plague=4))

- [ ] **Step 4: Run — 통과 확인**

- [ ] **Step 5: 스테이징**

`git add Assets/_Lair/Art/Cards/SpawnerConfig.asset Assets/_Lair/Tests/EditMode/Battle/SpawnerConfigTests.cs Assets/_Lair/Tests/EditMode/Battle/SpawnerConfigTests.cs.meta`

(SpawnerConfig 는 수정 — `.meta` 스테이징 제외)

---

### Phase 1 마무리 verification gate

- [ ] EditMode 전체 테스트 Pass (Unity Test Runner)
- [ ] `Glob: "**/ECardCategory*"` 또는 `Grep: "ECardCategory"` 결과 0건 (코드 마이그레이션 완료 증명)
- [ ] `Grep: "BuildSynergyService"` 로 모든 참조 위치 확인 — BattleContext + BattleController + 테스트 외에 없어야 함
- [ ] Unity Editor 콘솔 컴파일 에러 0건
- [ ] (Rule 01) 사용자에게 Phase 1 변경 요약 + 한글 커밋 메시지(안) 제시. 사용자 승인 시에만 커밋.

Phase 1 커밋 메시지(안):
```
# [feat] - 카드 빌드 시너지 시스템 코어 (4축 + Tier 임계)

- EBuildAxis(Tank/Dps/Debuff/Swarm) Enum 도입, ECardCategory 폐기
- BuildSynergyService — 라운드 단위 픽 카운트 추적 + 임계 발화
- IBattleContext.RegisterCardPick / GetBuildCount 추가
- Plague Spawner 1개 추가 (Wisp #4 → Plague 전환)
```

---

## Phase 2 — 카드 콘텐츠 (game-designer 기획서 의존)

**입력**: `docs/design/card-renewal.md` (game-designer 산출물). 다음을 포함해야 함:
1. 카드 28장 표 — (ID, 축, 패시브/액티브, 효과 한 줄 요약, 수치, 중첩 정책)
2. 12개 Tier 효과 — (축, threshold, 효과 한 줄 요약, 수치)
3. 카드 테두리 색 4축 매핑
4. Multiply 대체 카드 ID (수적 압박 축 어느 카드로 흡수되는지)

기획서가 미확정이면 Phase 2 시작 금지.

---

### Task 6: 신규 ECardId 값 추가 (28장 ID 확정)

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/EBuildAxisMigrationTests.cs`

- [ ] **Step 1: Write failing test — ECardId 신규 값 존재 + 기존 값 순서 보존**

> 기획서 §X 의 신규 카드 3장 ID 명을 받아 다음 패턴으로 작성:
```csharp
[Test]
public void ECardId_기존_25개_순서_보존()
{
    //# 0~14 패시브 + 15~24 액티브. Multiply=20 자리 유지(enum 값 제거 시 정렬 깨짐).
    Assert.AreEqual(0, (int)ECardId.WispHpBoost);
    Assert.AreEqual(20, (int)ECardId.Multiply);   //# enum 자리 보존, SO/json 에서만 삭제
    Assert.AreEqual(24, (int)ECardId.Berserk);
}

[Test]
public void ECardId_신규_3장_25_26_27()
{
    //# 기획서가 정한 이름 그대로. 예시:
    Assert.AreEqual(25, (int)ECardId.<신규1>);
    Assert.AreEqual(26, (int)ECardId.<신규2>);
    Assert.AreEqual(27, (int)ECardId.<신규3>);
}
```

- [ ] **Step 2: Run — 실패 예상 (신규 값 미정의)**

- [ ] **Step 3: ECardId enum 끝에 3장 추가**

```csharp
        //# 카드 리뉴얼 신규 3장 (axis 균등 분배 보충). 순서 끝 추가 — int 직렬화 정합.
        <신규1>,    //# 기획서 §X
        <신규2>,    //# 기획서 §X
        <신규3>,    //# 기획서 §X
```

`Multiply` 는 enum 자리에 남긴다 (값 26→25→... 재정렬 방지). SO/json/풀 에서만 제거.

- [ ] **Step 4: Run — 통과 확인**

- [ ] **Step 5: 스테이징**

---

### Task 7~10: 4축 × 7장 = 카드 효과 28장 구현

각 축마다 1 task. 한 task 안에서 7장(패시브 4 + 액티브 3) 효과 클래스 + EditMode 단위 테스트를 함께 작성.

#### Task 7: 탱커/포위 축 카드 7장

**Files:**
- Modify or Create: `Assets/_Lair/Scripts/Card/Effects/<탱커축 카드 7개>.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/Effects/<탱커축 카드별>Tests.cs`

> 입력: 기획서 §탱커축 표 — 카드 7장 라인업.

- [ ] **Step 1: 카드 별 failing 단위 테스트 작성 (7장 × 1 테스트 = 최소 7건)**

패턴 (`WispHpBoost` 예시 — 보존 카드면 기존 테스트 그대로):
```csharp
[Test]
public void WispHpBoost_Apply_Wisp_Hp_×1_5_등록()
{
    FakeBattleContext ctx = new FakeBattleContext();
    WispHpBoostEffect sut = new WispHpBoostEffect();
    sut.Apply(ctx);
    Assert.AreEqual((EMonster.Wisp, EMonsterStatKind.Hp, 1.5f), ctx.LastRegister);
}
```

신규 효과(예: `WispLineBreakBoost` — 진로 방해 강화)는 기획서 수치대로:
```csharp
[Test]
public void <신규효과>_Apply_<예상_호출>()
{
    //# 기획서 §X 의 효과 한 줄 요약을 검증.
}
```

- [ ] **Step 2: Run — 컴파일 실패 예상 (신규 효과 클래스 미존재)**

- [ ] **Step 3: 효과 클래스 7개 구현**

`ICardEffect` 패턴 (`MultiplyEffect.cs` 와 동일 — 단 Rule 02 §3 `var` 금지, §4 `!` 금지 준수):
```csharp
using System;
using Lair.Data;

namespace Lair.Card
{
    //# <카드명> — 기획서 §X.
    [Serializable]
    public class <카드>Effect : ICardEffect
    {
        [SerializeField] private float _multiplier = <기획서 값>;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.<X>, EMonsterStatKind.<Y>, _multiplier);
        }
    }
}
```

기존 보존 카드(예: `WispHpBoostEffect.cs`) 는 수정 불요. 신규 카드만 새 파일.

- [ ] **Step 4: Run — 통과 확인**

- [ ] **Step 5: 스테이징**

#### Task 8: DPS 축 카드 7장

(Task 7 패턴 동일 — 기획서 §DPS축 표 입력)

#### Task 9: 디버프 누적 축 카드 7장 (둔화/속박 포함)

(Task 7 패턴 동일 — 기획서 §디버프축 표 입력. 둔화·출혈·공포·약화·독장판 흡수가 모두 이 축)

#### Task 10: 수적 압박 축 카드 7장 + Multiply 삭제

(Task 7 패턴 동일 — 기획서 §수적압박축 표 입력. Multiply 의 대체 카드가 여기)

추가 step (Task 10 끝):
- [ ] **Step 6: Multiply 카드 코드/asset 삭제**

```
Delete: Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs
Delete: Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs.meta
Delete: Assets/_Lair/Art/Cards/Items/Multiply.asset
Delete: Assets/_Lair/Art/Cards/Items/Multiply.asset.meta
```

PowerShell:
```
Remove-Item Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs
Remove-Item Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs.meta
Remove-Item Assets/_Lair/Art/Cards/Items/Multiply.asset
Remove-Item Assets/_Lair/Art/Cards/Items/Multiply.asset.meta
```

`ECardId.Multiply` enum 값은 **유지** (int 직렬화 정합). CardPool_Active 에서 ref 만 제거.

- [ ] **Step 7: 스테이징 (삭제 파일도 git add 가능 — `.meta` 동반)**

---

### Task 11: 12개 Tier 시너지 효과 클래스

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Synergy/<축><Tier>.cs` (4축 × 3Tier = 12 파일)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (BindTier 12회 호출)
- Test: `Assets/_Lair/Tests/EditMode/Card/Synergy/<축><Tier>Tests.cs`

> 입력: 기획서 §시너지 표 — 4축 × 3Tier = 12개 효과.

- [ ] **Step 1: 각 Tier 별 failing 테스트 — Apply 시 IBattleContext 호출 확인**

예: 탱커 Tier1 — 탱커 몬스터 HP 글로벌 +30%
```csharp
[Test]
public void TankTier1_Apply_Wisp_Wraith_Hp_×1_3()
{
    FakeBattleContext ctx = new FakeBattleContext();
    TankSynergyTier1 sut = new TankSynergyTier1();
    sut.Apply(ctx);
    //# Wisp + Wraith 두 종에 동일 배율 등록
    Assert.That(ctx.RegisterCalls, Contains.Item((EMonster.Wisp, EMonsterStatKind.Hp, 1.3f)));
    Assert.That(ctx.RegisterCalls, Contains.Item((EMonster.Wraith, EMonsterStatKind.Hp, 1.3f)));
}
```

- [ ] **Step 2: Run — 컴파일 실패 예상**

- [ ] **Step 3: 12개 효과 클래스 구현**

패턴 (기획서 §시너지 표 수치 입력):
```csharp
using Lair.Data;

namespace Lair.Card
{
    //# 탱커 축 Tier1 (3장 임계) — 기획서 §X.
    public class TankSynergyTier1 : IBuildSynergyTier
    {
        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Hp, <값>);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, <값>);
        }
    }
}
```

- [ ] **Step 4: BattleController 부팅 시 12회 Bind**

`BattleController.cs` 초기화에 추가:
```csharp
    _synergy.BindTier(EBuildAxis.Tank,   3, new TankSynergyTier1());
    _synergy.BindTier(EBuildAxis.Tank,   5, new TankSynergyTier2());
    _synergy.BindTier(EBuildAxis.Tank,   7, new TankSynergyTier3());
    _synergy.BindTier(EBuildAxis.Dps,    3, new DpsSynergyTier1());
    _synergy.BindTier(EBuildAxis.Dps,    5, new DpsSynergyTier2());
    _synergy.BindTier(EBuildAxis.Dps,    7, new DpsSynergyTier3());
    _synergy.BindTier(EBuildAxis.Debuff, 3, new DebuffSynergyTier1());
    _synergy.BindTier(EBuildAxis.Debuff, 5, new DebuffSynergyTier2());
    _synergy.BindTier(EBuildAxis.Debuff, 7, new DebuffSynergyTier3());
    _synergy.BindTier(EBuildAxis.Swarm,  3, new SwarmSynergyTier1());
    _synergy.BindTier(EBuildAxis.Swarm,  5, new SwarmSynergyTier2());
    _synergy.BindTier(EBuildAxis.Swarm,  7, new SwarmSynergyTier3());
```

- [ ] **Step 5: Run — 통과 확인 (12 테스트 + 기존 회귀)**

- [ ] **Step 6: 스테이징**

---

### Task 12: SO 28장 일괄 마이그레이션

**Files:**
- Modify: `Assets/_Lair/Art/Cards/Items/*.asset` 24개 (기존 25 − Multiply)
- Create: `Assets/_Lair/Art/Cards/Items/<신규3장>.asset` + `.meta`
- Modify: `Assets/_Lair/Art/Cards/CardPool_Passive.asset` (16 ref)
- Modify: `Assets/_Lair/Art/Cards/CardPool_Active.asset` (12 ref, Multiply 제외)

- [ ] **Step 1: Write failing test — SO 28장 EBuildAxis 분포 검증**

`Assets/_Lair/Tests/EditMode/Card/CardPoolDistributionTests.cs`:
```csharp
[Test]
public void CardPool_Passive_16장_4축_각4()
{
    CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>("Assets/_Lair/Art/Cards/CardPool_Passive.asset");
    Assert.AreEqual(16, pool.Cards.Count);
    Dictionary<EBuildAxis, int> dist = new Dictionary<EBuildAxis, int>();
    foreach (CardData c in pool.Cards)
    {
        int v;
        dist.TryGetValue(c.Axis, out v);
        dist[c.Axis] = v + 1;
    }
    Assert.AreEqual(4, dist[EBuildAxis.Tank]);
    Assert.AreEqual(4, dist[EBuildAxis.Dps]);
    Assert.AreEqual(4, dist[EBuildAxis.Debuff]);
    Assert.AreEqual(4, dist[EBuildAxis.Swarm]);
}

[Test]
public void CardPool_Active_12장_4축_각3()
{
    /* 동일 패턴, 12장 / 축당 3장 */
}
```

- [ ] **Step 2: Run — 실패 예상 (SO 미마이그레이션)**

- [ ] **Step 3: SO 일괄 마이그레이션**

방법 A (Editor 스크립트 — 추천): `Assets/_Lair/Editor/CardAxisMigrator.cs` 신설, MenuItem `Lair/Migrate Card Axes` 로 24장 SO 의 `_axis` 필드를 기획서 표대로 설정 + 신규 3장 SO 생성. Editor 메뉴 실행.

방법 B (YAML 직접 수정): 24개 .asset 파일을 열어 `_axis: <int>` 추가 (Tank=0, Dps=1, Debuff=2, Swarm=3). 신규 3장은 Unity Editor 에서 `Create > Lair > Card` 메뉴로 생성, ECardId/EBuildAxis/이름/설명/효과 SerializeReference 채움.

`CardPool_Passive.asset`: `_cards` 리스트에 신규 패시브 카드 1장 ref 추가.
`CardPool_Active.asset`: Multiply ref 제거 + 신규 액티브 2장 ref 추가.

- [ ] **Step 4: Run — 통과 확인 + 회귀 (EditMode 전체)**

- [ ] **Step 5: 스테이징**

신규 .asset 은 `.meta` 함께 git add. 수정 .asset 은 `.meta` 제외 (Rule 01 §meta 규칙).

---

### Task 13: LairCardPrefabBuilder 카드 색상 매핑 4축 갱신

**Files:**
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: 매핑 코드 갱신**

기획서 §카드 테두리 색 4축 매핑 입력. 예 (game-designer 수치 채움):
```csharp
private static readonly Dictionary<EBuildAxis, Color> AxisBorderColor = new Dictionary<EBuildAxis, Color>
{
    { EBuildAxis.Tank,   <기획서 §X>  },   //# 예: #6B7280 회색
    { EBuildAxis.Dps,    <기획서 §X>  },   //# 예: #EF4444 빨강
    { EBuildAxis.Debuff, <기획서 §X>  },   //# 예: #A855F7 보라
    { EBuildAxis.Swarm,  <기획서 §X>  },   //# 예: #1F2937 검정
};
```
기존 7카테고리 dict 는 삭제.

- [ ] **Step 2: 카드 프리팹 일괄 재빌드 (Editor 메뉴)**

`Lair/Build Card Prefabs` 메뉴 실행. 28장 카드 UI 프리팹이 새 4축 색상으로 재생성.

- [ ] **Step 3: 시각 회귀 — 카드 픽 팝업 열어 색상 확인**

PlayMode 실행, 카드 픽 시점에 4축 카드가 각각 다른 색 테두리로 표시되는지 시각 확인 (capture).

- [ ] **Step 4: 스테이징**

`git add Assets/_Lair/Editor/LairCardPrefabBuilder.cs Assets/_Lair/Art/UI/Cards/**/*.prefab` (재빌드된 프리팹).

---

### Task 14: cards.json / card_pools.json 갱신

**Files:**
- Modify: `Assets/_Lair/Data/Json/cards.json`
- Modify: `Assets/_Lair/Data/Json/card_pools.json`

- [ ] **Step 1: Write failing test — json 항목 수 검증**

`Assets/_Lair/Tests/EditMode/Card/CardJsonTests.cs`:
```csharp
[Test]
public void cards_json_28개_Multiply_제외()
{
    string path = "Assets/_Lair/Data/Json/cards.json";
    string raw = System.IO.File.ReadAllText(path);
    JArray arr = JArray.Parse(raw);
    Assert.AreEqual(28, arr.Count);
    Assert.IsFalse(arr.Any(t => (string)t["id"] == "Multiply"));
}
```

- [ ] **Step 2: Run — 실패 예상**

- [ ] **Step 3: json 갱신**

기존 cards.json 의 25개 항목 → 28개로 갱신:
- 각 항목에 `"axis": "Tank|Dps|Debuff|Swarm"` 필드 추가
- 기존 `"category": "Enhance|Spawn|Replace|Environment"` 필드 제거
- Multiply 항목 삭제
- 신규 3장 항목 추가 (기획서 ID/이름/설명/axis/효과 직렬화)
- 효과 수치는 기획서 표대로

card_pools.json:
- Passive 풀: 16개 카드 ID
- Active 풀: 12개 카드 ID (Multiply 제외)

- [ ] **Step 4: Run — 통과 확인**

- [ ] **Step 5: 스테이징**

---

### Phase 2 마무리 verification gate

- [ ] EditMode 전체 테스트 Pass
- [ ] Unity Editor 콘솔 컴파일 에러 0건
- [ ] SO 28장 모두 `_axis` 필드 정상 (CardPoolDistributionTests Pass)
- [ ] Multiply.asset / MultiplyEffect.cs 파일 부재 확인 (`Glob: "**/Multiply*"`)
- [ ] 카드 프리팹 시각 회귀 — 4축 색상 표시 확인 (스크린샷 첨부 권장)
- [ ] (Rule 01) 사용자에게 Phase 2 변경 요약 + 한글 커밋 메시지(안) 제시.

Phase 2 커밋 메시지(안):
```
# [feat] - 카드 28장 4축 리뉴얼 (Multiply 삭제, 신규 3장)

- 카드 효과 28장 재구현 (Tank/Dps/Debuff/Swarm 균등 분배)
- 12개 빌드 시너지 Tier 효과 (4축 × 3Tier)
- CardData._axis 마이그레이션 (SO 24장 + 신규 3장)
- cards.json/card_pools.json 28장 갱신
- 카드 테두리 색 4축 매핑 (LairCardPrefabBuilder)
- Multiply 카드 삭제 (수적 압박 축으로 흡수)
```

---

## Phase 3 — 통합 검증 + 문서

### Task 15: 컨셉서 §11.3·§11.4·§5.2 갱신

**Files:**
- Modify: `docs/design/project_lair_concept.md`
- Modify: `docs/design/continuous-spawn-round.md`

- [ ] **Step 1: 컨셉서 §4.2 트리거 유지 확인 (변경 없음)**

읽기만 — HP 10% / 30s 트리거는 그대로.

- [ ] **Step 2: §5.2 시너지 방향성 4축 재정렬**

기존 예시(언데드 부활 등)를 4축 시너지 패턴으로 정렬:
- 탱커 5+ → ...
- DPS 5+ → ...
- 디버프 5+ → ...
- 수적 압박 5+ → ...

수치·문구는 기획서 §시너지 표를 그대로 인용.

- [ ] **Step 3: §11.3 카드 정의 28장 재작성**

기존 "강화 6 + 추가 5 + 교체 2 + 환경 2 + 액티브 10" 구조 폐지 → "4축 × 7장 (패시브 4 + 액티브 3)" 구조로 재기술. 기획서 표 inline.

- [ ] **Step 4: §11.4 카드 테두리 색 4색 매핑**

기존 7색 매핑 행 폐지, 4색만 남김. LairCardPrefabBuilder 색상과 일치.

- [ ] **Step 5: §변경 이력 v0.6 추가**

```
- **v0.6 (2026-05-31)**: 카드 전체 리뉴얼. 25장 → 28장. 카테고리 7종 → 4축(Tank/Dps/Debuff/Swarm). 2-Layer 시너지(빌드 카운트 임계 + 카드 중첩) 도입. Plague Spawner 1개 추가. Multiply 카드 삭제. spec `docs/superpowers/specs/2026-05-31-card-renewal-design.md`, plan `docs/superpowers/plans/2026-05-31-card-renewal.md` 와 정합.
```

- [ ] **Step 6: continuous-spawn-round.md §3.1 · §7 갱신**

- §3.1 Spawner 6개 구성 표: Wisp 2 → Wisp 1 + Plague 1
- §7 "Plague 는 초기 Spawner 6개에 포함되지 않는다 — 의도된 설계" 문구 삭제 또는 "카드 리뉴얼 v0.6 부터 Plague Spawner 1개 배치 (디버프 축 작동 전제)" 로 갱신

- [ ] **Step 7: 스테이징**

---

### Task 16: PlayMode 통합 테스트 갱신

**Files:**
- Modify: `Assets/_Lair/Tests/PlayMode/SimPickStrategy.cs`
- Modify: `Assets/_Lair/Tests/PlayMode/ContinuousSpawnIntegrationTest.cs`

- [ ] **Step 1: SimPickStrategy 4축 우선 픽 전략 갱신**

기존 `AoEPriority` / `DealerPriority` / `Random` / `TankerPriority` 4종 →
- `TankAxisPriority` / `DpsAxisPriority` / `DebuffAxisPriority` / `SwarmAxisPriority` / `Random` 5종 (또는 기획서 합의)

각 전략은 해당 축 카드를 우선 픽 (4축 시너지 Tier 임계 도달 확인).

- [ ] **Step 2: ContinuousSpawnIntegrationTest 시나리오 갱신**

- Plague Spawner 작동 확인 시나리오
- 디버프 축 5장 픽 시 Tier2 임계 발화 시나리오
- Multiply 카드 부재 확인 (인덱싱 ID 사용 시 회귀)

- [ ] **Step 3: Run PlayMode — 전체 시나리오 Pass**

`run` 스킬 사용해 Unity 헤드리스 실행 또는 Editor PlayMode 수동.

- [ ] **Step 4: 스테이징**

---

### Task 17: Final verification gate + 마무리

- [ ] **Step 1: 전체 회귀**

- EditMode 전체 테스트 Pass
- PlayMode 전체 테스트 Pass
- Unity Editor 콘솔 컴파일 에러 0건
- `Grep: "ECardCategory"` 결과 0건
- `Grep: "Multiply"` 결과 — `ECardId.Multiply` 와 변경 이력 외에 0건
- 카드 픽 팝업 시각 회귀 (스크린샷)
- 5분 풀 플레이 1판 (영웅 처치 / 5분 타임오버 어느 쪽이든 시스템 미충돌 확인)

- [ ] **Step 2: Unity Editor 포커스 (meta 자동 생성 대기)**

UnityMCP `editor_focus` 호출.

- [ ] **Step 3: 변경 요약 + 한글 커밋 메시지(안) 제시 (Rule 01)**

Phase 3 단독 커밋 메시지(안):
```
# [docs] - 카드 리뉴얼 v0.6 컨셉서·문서 동기화 + 통합 테스트 갱신

- project_lair_concept.md §5.2/§11.3/§11.4 4축 재정렬
- continuous-spawn-round.md §3.1/§7 Plague Spawner 반영
- PlayMode SimPickStrategy 4축 전략 + ContinuousSpawnIntegrationTest 갱신
- 변경 이력 v0.6 추가
```

또는 사용자 합의 시 Phase 1·2·3 일괄 단일 커밋. Rule 01 — 메시지(안) 까지만, 사용자가 직접 `git commit` 실행.

---

## Self-Review

### Spec coverage (D1~D11)

| 결정 락 | 구현 task |
|---|---|
| D1 25장 → 28장 전체 리뉴얼 | Task 6 (ECardId 신규 3장) + Task 7~10 (효과 28장) + Task 12 (SO) + Task 14 (json) |
| D2 빌드 다양성 1순위 | Task 7~10 의 4축 분배 + Task 11 시너지 |
| D3 4축 = 새 카테고리 / ECardCategory 폐지 | Task 1 (EBuildAxis 도입) + Task 4 (ECardCategory 삭제) |
| D4 둔화/속박 → 디버프 축 | Task 9 |
| D5 한 축당 7장 균등 | Task 12 CardPoolDistributionTests |
| D6 Layer 1 = 3/5/7 임계 즉시 발동 | Task 3 RegisterPick 임계 발화 + Task 11 12개 Tier |
| D7 Layer 2 = 카드 중첩 (기존 곱연산 계승) | 기존 `RegisterMonsterTypeBuff` 곱연산 보존 — Task 7~10 의 카드 효과가 이 표면을 그대로 사용. 추가 시스템 변경 불요 |
| D8 환경 → 4축 흡수 | Task 9 (디버프 축에 독장판 흡수), Task 10 (수적 압박에 시야 감소 흡수 — 기획서 의존) |
| D9 Plague Spawner 추가 | Task 5 |
| D10 Multiply 삭제 | Task 10 Step 6 (코드+SO) + Task 14 (json) |
| D11 컨셉서 §11.3·§11.4·§5.2 / §4.2 유지 | Task 15 |

### Placeholder 잔여
- Phase 2 task 들에 `<기획서 §X>` placeholder 다수 — **의도된 dependency** (game-designer 산출물 위치). plan 작성자가 직접 채워서 슈퍼파워 실행 가려면 카드 28장 표 + Tier 12개 수치를 plan 안에 추가해야 함을 헤더에 명시함.
- Task 5 의 SpawnerConfig 경로 (`Assets/_Lair/Art/Cards/SpawnerConfig.asset`) 는 작성자 추정. 첫 step 에 `Glob` 으로 정확 경로 확인 step 포함.

### Type consistency
- `EBuildAxis` 값 4개 (Tank/Dps/Debuff/Swarm) — Task 1·2·3·4·11·12·14·15·16 모두 동일.
- `IBuildSynergyTier.Apply(IBattleContext)` — Task 3 정의, Task 11 구현, Task 11 BindTier 시그니처 일치.
- `BuildSynergyService.RegisterPick(EBuildAxis, IBattleContext)` 2-arg — Task 3 정의, Task 4 `BattleContext.RegisterCardPick` 호출에서 사용.
- `CardData.Axis` / `_axis` — Task 4 정의, Task 12 SO 마이그레이션, Task 14 json `axis` 필드 정합.

### 모호 사항 (game-designer 가 결정)
- 카드별 정확한 효과 수치 / 중첩 정책
- Tier 1/2/3 보너스 강도
- 신규 카드 3장 ID·이름·축
- 카드 테두리 4색 코드
- 액티브 카드 중첩 시 효과량 ↑ 인지 지속시간 ↑ 인지

위 모호 사항은 plan 안의 task 가 placeholder 로 명시.

---

## 부록 — 작업 양 추정

| Phase | Task 수 | 예상 소요 |
|---|---|---|
| Phase 1 | 5 | 4~6 시간 (시스템 코어) |
| Phase 2 | 9 (Task 6~14) | 12~18 시간 (카드 28장 + 시너지 12개 + 마이그레이션) |
| Phase 3 | 3 | 3~4 시간 (문서·통합 테스트) |
| **합계** | **17 tasks** | **약 20~28 시간** |

(game-designer 단계 + design-reviewer + code-reviewer + test-engineer 별도)
