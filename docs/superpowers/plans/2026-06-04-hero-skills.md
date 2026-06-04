# 영웅 스킬 시스템 (Hero Skills) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> ⚠️ **Rule 01 (자동 커밋 금지)**: 워커는 `git commit` 을 실행하지 않는다. 각 Task 의 "Checkpoint" 는 **테스트 그린 확인 + 관련 파일 stage** 까지만. 최종 커밋 메시지(안)는 파이프라인 마무리에서 메인 오케스트레이터가 제시한다.
>
> ⚠️ **수치 위임**: 데미지·반경·쿨다운·넉백·HP 게이트 정확값은 **game-designer 기획서** 가 단일 진실이다. 본 plan 은 *구조·시그니처·필드*만 정의하고, `.asset` 값은 기획서 §수치표에서 채운다. 테스트는 자체 주입값을 쓴다.

**Goal:** 영웅(적 모험가)이 HP 3페이즈(90%/60%/30%)로 점진 획득하는 Survivor.io식 자동 스킬(돌진→회전블레이드→AOE노바)로 몰려오는 몬스터 무리를 쓸어담게 한다.

**Architecture:** 데이터드리븐 폴리모픽 ScriptableObject — 추상 `HeroSkillData` SO 의 서브클래스 3종이 behavior 를 캡슐화하고 `CreateRuntime()` 으로 가변 상태 런타임을 생성한다. `HeroSkillRunner`(영웅 부착)가 `HeroSkillPhaseGate`(순수)로 HP% 를 폴링해 임계 돌파 시 스킬을 활성화하고, 활성 런타임을 매 프레임 `IHeroSkillContext`(= `CharacterRegistry` 래퍼) 로 Tick 한다. 데미지 선택 기하는 순수 `SkillGeometry` 로 분리해 테스트한다.

**Tech Stack:** Unity 6 / C# / ScriptableObject / ChvjPackage(`CHMResource`/`CHMPool`) / NUnit (EditMode 순수 + PlayMode 통합).

---

## 파일 구조

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Scripts/Character/CommonInterface.HeroSkill.cs` | `ISkillTarget`·`IHeroSkillContext`·`IHeroSkillRuntime` 인터페이스 (Rule 02 §9 prefixed split) | 신규 |
| `Scripts/Character/Skills/SkillGeometry.cs` | 링/라인 멤버십 순수 기하 | 신규 |
| `Scripts/Character/Skills/HeroSkillPhaseGate.cs` | HP비율 폴링 → 신규 활성 인덱스 (순수) | 신규 |
| `Scripts/Character/Skills/HeroSkillData.cs` | 추상 SO 베이스 + `CreateRuntime()` | 신규 |
| `Scripts/Character/Skills/DashStrikeSkillData.cs` | P1 돌진 SO + 런타임 | 신규 |
| `Scripts/Character/Skills/OrbitingBladeSkillData.cs` | P2 회전블레이드 SO + 런타임 | 신규 |
| `Scripts/Character/Skills/AoeNovaSkillData.cs` | P3 노바 SO + 런타임 | 신규 |
| `Scripts/Character/Skills/HeroSkillLoadout.cs` | `{HpFraction, HeroSkillData}` 페이즈 리스트 SO | 신규 |
| `Scripts/Character/Skills/HeroSkillContext.cs` | `IHeroSkillContext` 실구현 (`CharacterRegistry` 순회 + `SkillGeometry`) | 신규 |
| `Scripts/Character/Skills/HeroSkillRunner.cs` | 영웅 부착 MonoBehaviour — 페이즈 게이트 + Tick + 풀 리셋 | 신규 |
| `Scripts/Data/CommonEnum.cs` | `EData.HeroSkillLoadout`, `EVisual.HeroDashFx/HeroOrbitBladeFx/HeroNovaFx` 추가 | 수정 |
| `Editor/LairVisualPrefabBuilder.cs` | 3개 스킬 프리미티브 FX 프리팹 빌드 + Addressable | 수정 |
| `Editor/LairCharacterPrefabBuilder.cs` | Knight 프리팹에 `HeroSkillRunner` 부착 | 수정 |
| `Editor/LairHeroSkillAssetBuilder.cs` | 3 스킬 `.asset` + `HeroSkillLoadout.asset` 생성 + Addressable 등록 | 신규 |
| `Scripts/Battle/BattleController.cs` | 로드아웃 로드 → 러너 Bind + FX 프리워밍 | 수정 |
| `Editor/JsonSync/Dto/HeroSkillsDto.cs` | hero_skills.json DTO (skills + loadout) | 신규 |
| `Editor/JsonSync/HeroSkillDataConverter.cs` | `HeroSkillData` 폴리모픽 `$type` 컨버터 (EffectConverter 미러) | 신규 |
| `Editor/JsonSync/HeroSkillSyncer.cs` | 3 스킬 SO + 로드아웃 ↔ hero_skills.json Export/Import | 신규 |
| `Editor/JsonSync/LairJsonSyncWindow.cs` | "Hero Skills" 섹션 + ExportAll/ImportAll 엔트리 | 수정 |
| `Tests/EditMode/SkillGeometryTests.cs` | 기하 순수 테스트 | 신규 |
| `Tests/EditMode/HeroSkillPhaseGateTests.cs` | 페이즈 게이트 순수 테스트 | 신규 |
| `Tests/EditMode/FakeHeroSkillContext.cs` | 테스트 더블 (호출 기록) | 신규 |
| `Tests/EditMode/DashStrikeSkillTests.cs` | 돌진 런타임 타이밍/파라미터 테스트 | 신규 |
| `Tests/EditMode/OrbitingBladeSkillTests.cs` | 회전 런타임 인터벌/링 테스트 | 신규 |
| `Tests/EditMode/AoeNovaSkillTests.cs` | 노바 런타임 쿨다운/디스크 테스트 | 신규 |
| `Tests/PlayMode/HeroSkillRunnerPlayTests.cs` | HP 페이즈 → 스킬 활성 → 몬스터 피격 통합 | 신규 |

> **빌더 경유 필수**: Knight·FX 프리팹은 빌더 생성형이다. 프리팹을 손으로 편집하면 다음 빌드에서 덮어써진다 — 반드시 빌더 코드에 반영한다.

---

## Phase A — Foundation + Dash Strike (P1, HP 90%)

### Task A1: 공용 인터페이스 정의

**Files:**
- Create: `Assets/_Lair/Scripts/Character/CommonInterface.HeroSkill.cs`

- [ ] **Step 1: 인터페이스 파일 작성** (테스트 불필요 — 순수 선언)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# Rule 02 §9 — Character 도메인 공용 인터페이스의 hero-skill 분할 파일.

    //# 영웅 스킬이 데미지를 줄 수 있는 몬스터 1체. CharacterRegistry.Entry 를 래핑하거나 테스트 더블이 구현.
    public interface ISkillTarget
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        IHealth Health { get; }
    }

    //# 영웅 스킬이 월드와 상호작용하는 단일 seam. 실구현은 HeroSkillContext(CharacterRegistry 순회),
    //# 테스트는 FakeHeroSkillContext(호출 기록). 스킬은 "언제·어떤 파라미터로" 만 결정하고 적용은 ctx 가 한다.
    public interface IHeroSkillContext
    {
        Vector3 HeroPosition { get; }

        //# 영웅 중심 XZ 링 [inner, outer] 안의 살아있는 교전 몬스터 전원에 amount 데미지(+넉백). 피격 수 반환.
        //# inner=0 이면 꽉 찬 디스크(노바).
        int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength);

        //# 영웅에서 direction 으로 length·halfWidth 의 직선 띠 안 몬스터 전원에 amount 데미지(+넉백). 피격 수 반환.
        int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength);

        //# 영웅 중심 radius 내 몬스터 무게중심 (돌진 방향 결정용). 없으면 HeroPosition 반환.
        Vector3 MonsterCentroid(float radius);
    }

    //# 활성화된 스킬 1개의 가변 상태(쿨다운 타이머·궤도 각도). HeroSkillData.CreateRuntime() 이 생성.
    public interface IHeroSkillRuntime
    {
        //# 내부 타이머를 dt 만큼 진행하고, 준비되면 ctx 로 데미지 적용 + 비주얼 갱신.
        void Tick(IHeroSkillContext ctx, float dt);

        //# 풀 반환/비활성 시 호출 — 점유 중인 풀 비주얼 반환.
        void OnDeactivate();
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Run (Unity): `Lair > Test > Compile` 또는 UnityMCP `editor_recompile`
Expected: 컴파일 에러 없음.

- [ ] **Step 3: Checkpoint** (Rule 01 — stage only)

```bash
git add Assets/_Lair/Scripts/Character/CommonInterface.HeroSkill.cs Assets/_Lair/Scripts/Character/CommonInterface.HeroSkill.cs.meta
```

---

### Task A2: SkillGeometry 순수 기하 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/SkillGeometry.cs`
- Test: `Assets/_Lair/Tests/EditMode/SkillGeometryTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class SkillGeometryTests
    {
        [Test]
        public void InRing_링밴드_안이면_true()
        {
            Vector3 center = Vector3.zero;
            //# 반경 2 점 — 밴드 [1.5, 2.5] 안.
            Assert.IsTrue(SkillGeometry.InRing(new Vector3(2f, 0f, 0f), center, 1.5f, 2.5f));
        }

        [Test]
        public void InRing_밴드_안쪽이면_false()
        {
            Assert.IsFalse(SkillGeometry.InRing(new Vector3(1f, 0f, 0f), Vector3.zero, 1.5f, 2.5f));
        }

        [Test]
        public void InRing_Y차이_무시_XZ만판정()
        {
            //# y=10 이어도 XZ 거리 2 면 밴드 안.
            Assert.IsTrue(SkillGeometry.InRing(new Vector3(2f, 10f, 0f), Vector3.zero, 1.5f, 2.5f));
        }

        [Test]
        public void InLine_전방_띠_안이면_true()
        {
            //# 원점에서 +Z 로 길이 5, 반폭 1. 점 (0.5, 0, 3) 은 띠 안.
            Assert.IsTrue(SkillGeometry.InLine(new Vector3(0.5f, 0f, 3f), Vector3.zero, Vector3.forward, 5f, 1f));
        }

        [Test]
        public void InLine_옆으로_반폭초과면_false()
        {
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(2f, 0f, 3f), Vector3.zero, Vector3.forward, 5f, 1f));
        }

        [Test]
        public void InLine_길이초과면_false()
        {
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(0f, 0f, 6f), Vector3.zero, Vector3.forward, 5f, 1f));
        }

        [Test]
        public void InLine_뒤쪽이면_false()
        {
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(0f, 0f, -1f), Vector3.zero, Vector3.forward, 5f, 1f));
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: Unity Test Runner (EditMode) `SkillGeometryTests`
Expected: FAIL — `SkillGeometry` 미정의 (컴파일 에러).

- [ ] **Step 3: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 데미지 영역 멤버십 — XZ 평면 순수 기하. Unity 월드 비의존(테스트 가능).
    public static class SkillGeometry
    {
        //# p 가 center 기준 XZ 링 밴드 [inner, outer] 안인가. inner=0 이면 디스크.
        public static bool InRing(Vector3 p, Vector3 center, float inner, float outer)
        {
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            float sq = dx * dx + dz * dz;
            return sq >= inner * inner && sq <= outer * outer;
        }

        //# p 가 origin 에서 dir(정규화 가정) 방향 길이 length·반폭 halfWidth 직선 띠 안인가 (XZ).
        //# 전방(투영 0~length) + 측면거리 <= halfWidth.
        public static bool InLine(Vector3 p, Vector3 origin, Vector3 dir, float length, float halfWidth)
        {
            Vector3 d = dir;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return false;
            d.Normalize();

            Vector3 rel = p - origin;
            rel.y = 0f;
            float along = Vector3.Dot(rel, d);
            if (along < 0f || along > length) return false;

            Vector3 perp = rel - d * along;
            return perp.sqrMagnitude <= halfWidth * halfWidth;
        }
    }
}
```

- [ ] **Step 4: 통과 확인**

Run: EditMode `SkillGeometryTests`
Expected: PASS (7 tests).

- [ ] **Step 5: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/SkillGeometry.cs Assets/_Lair/Scripts/Character/Skills/SkillGeometry.cs.meta Assets/_Lair/Tests/EditMode/SkillGeometryTests.cs Assets/_Lair/Tests/EditMode/SkillGeometryTests.cs.meta
```

---

### Task A3: HeroSkillPhaseGate 순수 페이즈 게이트 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillPhaseGate.cs`
- Test: `Assets/_Lair/Tests/EditMode/HeroSkillPhaseGateTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    public class HeroSkillPhaseGateTests
    {
        private static HeroSkillPhaseGate Gate()
            => new HeroSkillPhaseGate(new[] { 0.9f, 0.6f, 0.3f });

        [Test]
        public void Poll_100퍼센트면_활성없음()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(1.0f, outv);
            Assert.AreEqual(0, outv.Count);
        }

        [Test]
        public void Poll_90퍼센트_도달시_인덱스0_활성()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.9f, outv);
            CollectionAssert.AreEqual(new[] { 0 }, outv);
        }

        [Test]
        public void Poll_같은페이즈_재폴링시_중복활성없음()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.9f, outv);
            g.Poll(0.85f, outv);   //# outv 는 호출마다 clear 됨
            Assert.AreEqual(0, outv.Count);
        }

        [Test]
        public void Poll_급격한_HP하락시_누락된_페이즈_모두_활성()
        {
            //# 90·60 을 건너뛰고 한 번에 25% 로 떨어져도 0,1,2 전부 활성.
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.25f, outv);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, outv);
        }

        [Test]
        public void Reset_후_다시_활성가능()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.2f, outv);
            g.Reset();
            g.Poll(0.9f, outv);
            CollectionAssert.AreEqual(new[] { 0 }, outv);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: EditMode `HeroSkillPhaseGateTests`
Expected: FAIL — `HeroSkillPhaseGate` 미정의.

- [ ] **Step 3: 구현 작성**

```csharp
using System.Collections.Generic;

namespace Lair.Character
{
    //# 영웅 HP 비율을 폴링해 임계(HpFraction) 를 *처음* 하향 돌파한 페이즈 인덱스를 반환하는 순수 게이트.
    //# 한 번 활성된 인덱스는 다시 반환하지 않는다. 급락으로 여러 임계를 한 번에 넘으면 모두 반환.
    public class HeroSkillPhaseGate
    {
        private readonly float[] _fractions;
        private readonly bool[] _activated;

        public HeroSkillPhaseGate(IReadOnlyList<float> hpFractions)
        {
            _fractions = new float[hpFractions.Count];
            for (int i = 0; i < hpFractions.Count; ++i) _fractions[i] = hpFractions[i];
            _activated = new bool[_fractions.Length];
        }

        //# newlyActivated 를 clear 후, hpRatio <= fraction 인데 아직 미활성인 인덱스를 채운다.
        public void Poll(float hpRatio, List<int> newlyActivated)
        {
            newlyActivated.Clear();
            for (int i = 0; i < _fractions.Length; ++i)
            {
                if (_activated[i]) continue;
                if (hpRatio <= _fractions[i])
                {
                    _activated[i] = true;
                    newlyActivated.Add(i);
                }
            }
        }

        //# 풀 재사용/라운드 재시작 대비.
        public void Reset()
        {
            for (int i = 0; i < _activated.Length; ++i) _activated[i] = false;
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — Expected: PASS (5 tests).

- [ ] **Step 5: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/HeroSkillPhaseGate.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillPhaseGate.cs.meta Assets/_Lair/Tests/EditMode/HeroSkillPhaseGateTests.cs Assets/_Lair/Tests/EditMode/HeroSkillPhaseGateTests.cs.meta
```

---

### Task A4: HeroSkillData 추상 SO 베이스

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillData.cs`

- [ ] **Step 1: 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 데이터의 추상 베이스. 서브클래스가 튜닝 필드 + behavior 를 캡슐화한다.
    //# 공유 에셋이므로 가변 상태는 보관 금지 — CreateRuntime() 이 만든 런타임이 보유.
    public abstract class HeroSkillData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        public string DisplayName => _displayName;

        //# 활성화 시 1회 호출. 이 스킬의 가변 상태 런타임 생성.
        public abstract IHeroSkillRuntime CreateRuntime();
    }
}
```

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/HeroSkillData.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillData.cs.meta
```

---

### Task A5: FakeHeroSkillContext 테스트 더블

**Files:**
- Create: `Assets/_Lair/Tests/EditMode/FakeHeroSkillContext.cs`

- [ ] **Step 1: 작성** (테스트 헬퍼 — 자체 테스트 없음, 이후 Task 가 사용)

```csharp
using System.Collections.Generic;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# IHeroSkillContext 테스트 더블 — 데미지 호출을 기록한다.
    //# 각 Damage* 메서드는 미리 설정한 HitCount 를 반환하고, 호출 파라미터를 로그에 남긴다.
    public class FakeHeroSkillContext : IHeroSkillContext
    {
        public Vector3 HeroPosition { get; set; } = Vector3.zero;

        //# 다음 Damage* 호출이 반환할 피격 수.
        public int NextHitCount = 0;
        //# centroid 반환값.
        public Vector3 CentroidResult = Vector3.zero;

        public struct RingCall { public float Inner, Outer; public int Amount; public float Knockback; }
        public struct LineCall { public Vector3 Dir; public float Length, HalfWidth; public int Amount; public float Knockback; }

        public readonly List<RingCall> RingCalls = new List<RingCall>();
        public readonly List<LineCall> LineCalls = new List<LineCall>();

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            RingCalls.Add(new RingCall { Inner = innerRadius, Outer = outerRadius, Amount = amount, Knockback = knockbackStrength });
            return NextHitCount;
        }

        public int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength)
        {
            LineCalls.Add(new LineCall { Dir = direction, Length = length, HalfWidth = halfWidth, Amount = amount, Knockback = knockbackStrength });
            return NextHitCount;
        }

        public Vector3 MonsterCentroid(float radius) => CentroidResult;
    }
}
```

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Tests/EditMode/FakeHeroSkillContext.cs Assets/_Lair/Tests/EditMode/FakeHeroSkillContext.cs.meta
```

---

### Task A6: DashStrikeSkillData + 런타임 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/DashStrikeSkillData.cs`
- Test: `Assets/_Lair/Tests/EditMode/DashStrikeSkillTests.cs`

**동작**: 쿨다운마다 1회 발동. 발동 시 영웅→몬스터 무게중심 방향으로 직선 띠 데미지(+넉백). 몬스터가 0이면(centroid==heroPos) 발동 보류.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class DashStrikeSkillTests
    {
        //# 테스트용 SO + 런타임 생성 (필드는 reflection 으로 주입).
        private static IHeroSkillRuntime MakeRuntime(out DashStrikeSkillData data)
        {
            data = ScriptableObject.CreateInstance<DashStrikeSkillData>();
            TestReflection.SetField(data, "_damage", 100);
            TestReflection.SetField(data, "_cooldown", 2f);
            TestReflection.SetField(data, "_dashLength", 6f);
            TestReflection.SetField(data, "_halfWidth", 1.5f);
            TestReflection.SetField(data, "_knockbackStrength", 2f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_쿨다운_경과전엔_발동안함()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 1.0f);   //# cooldown 2 미만
            Assert.AreEqual(0, ctx.LineCalls.Count);
        }

        [Test]
        public void Tick_쿨다운_경과시_라인데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 2.0f);
            Assert.AreEqual(1, ctx.LineCalls.Count);
            Assert.AreEqual(100, ctx.LineCalls[0].Amount);
            Assert.AreEqual(6f, ctx.LineCalls[0].Length, 0.001f);
            Assert.AreEqual(1.5f, ctx.LineCalls[0].HalfWidth, 0.001f);
            Assert.AreEqual(2f, ctx.LineCalls[0].Knockback, 0.001f);
        }

        [Test]
        public void Tick_방향은_영웅에서_무게중심쪽()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext
            {
                HeroPosition = Vector3.zero,
                CentroidResult = new Vector3(0, 0, 5)
            };
            rt.Tick(ctx, 2.0f);
            Vector3 dir = ctx.LineCalls[0].Dir.normalized;
            Assert.AreEqual(0f, dir.x, 0.01f);
            Assert.AreEqual(1f, dir.z, 0.01f);
        }

        [Test]
        public void Tick_몬스터없으면_발동보류()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            //# centroid == heroPosition → 방향 0 → 발동 안 함.
            FakeHeroSkillContext ctx = new FakeHeroSkillContext
            {
                HeroPosition = Vector3.zero,
                CentroidResult = Vector3.zero
            };
            rt.Tick(ctx, 2.0f);
            Assert.AreEqual(0, ctx.LineCalls.Count);
        }

        [Test]
        public void Tick_발동후_쿨다운_재충전()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 2.0f);   //# 1회 발동
            rt.Tick(ctx, 1.0f);   //# 쿨다운 미경과
            Assert.AreEqual(1, ctx.LineCalls.Count);
            rt.Tick(ctx, 2.0f);   //# 다시 경과
            Assert.AreEqual(2, ctx.LineCalls.Count);
        }
    }
}
```

> **참고:** `TestReflection.SetField` 는 Task A6-pre 에서 만든다 (아래 Step 0). 이미 있으면 재사용.

- [ ] **Step 0: 공용 reflection 헬퍼 (없으면 생성)**

Create: `Assets/_Lair/Tests/EditMode/TestReflection.cs`

```csharp
using System.Reflection;

namespace Lair.Tests.EditMode
{
    //# private [SerializeField] 필드 주입 헬퍼 (테스트 전용).
    public static class TestReflection
    {
        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert(f != null, $"필드 미발견: {target.GetType().Name}.{fieldName}");
            f.SetValue(target, value);
        }

        private static void Assert(bool cond, string msg)
        {
            if (cond == false) throw new System.Exception(msg);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Expected: FAIL — `DashStrikeSkillData` 미정의.

- [ ] **Step 3: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# P1 (HP 90%) — 영웅이 몬스터 무게중심 방향으로 직선 돌진하며 일직선 몬스터 관통 데미지(+넉백).
    [CreateAssetMenu(fileName = "HeroSkill_DashStrike", menuName = "Lair/Hero Skills/Dash Strike")]
    public class DashStrikeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 100;
        [SerializeField] private float _cooldown = 3f;
        [SerializeField] private float _dashLength = 6f;
        [SerializeField] private float _halfWidth = 1.5f;
        [SerializeField] private float _knockbackStrength = 2f;
        [SerializeField] private float _centroidRadius = 8f;   //# 방향 결정용 무게중심 수집 반경

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float DashLength => _dashLength;
        public float HalfWidth => _halfWidth;
        public float KnockbackStrength => _knockbackStrength;
        public float CentroidRadius => _centroidRadius;

        public override IHeroSkillRuntime CreateRuntime() => new DashStrikeRuntime(this);
    }

    //# 가변 상태 = 쿨다운 타이머. 발동 시 비주얼은 CHMPool(가용 시)로 스폰.
    public class DashStrikeRuntime : IHeroSkillRuntime
    {
        private readonly DashStrikeSkillData _data;
        private float _cooldownRemain;

        public DashStrikeRuntime(DashStrikeSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지 — 첫 쿨다운 대기
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            if (_cooldownRemain > 0f) return;

            Vector3 centroid = ctx.MonsterCentroid(_data.CentroidRadius);
            Vector3 dir = centroid - ctx.HeroPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;   //# 몬스터 없음 — 발동 보류(쿨다운은 유지, 다음 프레임 재시도)

            dir.Normalize();
            ctx.DamageMonstersInLine(dir, _data.DashLength, _data.HalfWidth, _data.Damage, _data.KnockbackStrength);
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnLine(EVisualKeyForDash(), ctx.HeroPosition, dir, _data.DashLength);
        }

        public void OnDeactivate() { }

        private static Lair.Data.EVisual EVisualKeyForDash() => Lair.Data.EVisual.HeroDashFx;
    }
}
```

> `HeroSkillFx` 비주얼 헬퍼는 Task A7 에서 만든다. `EVisual.HeroDashFx` 는 Task A9 에서 enum 추가.
> 위 구현은 `HeroSkillFx`/`EVisual.HeroDashFx` 가 아직 없으면 컴파일 실패한다 → A6 은 **데미지 로직까지만 먼저 작성**하고, `HeroSkillFx.SpawnLine(...)` 줄과 `EVisualKeyForDash()` 는 A9 enum + A7 헬퍼 완료 후 추가한다. 그 전까지는 해당 줄을 `//# TODO A7/A9: 비주얼` 주석으로 둔다.

- [ ] **Step 4: 통과 확인** — Expected: PASS (5 tests). (비주얼 줄 주석 처리 상태)

- [ ] **Step 5: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/DashStrikeSkillData.cs Assets/_Lair/Scripts/Character/Skills/DashStrikeSkillData.cs.meta Assets/_Lair/Tests/EditMode/DashStrikeSkillTests.cs Assets/_Lair/Tests/EditMode/DashStrikeSkillTests.cs.meta Assets/_Lair/Tests/EditMode/TestReflection.cs Assets/_Lair/Tests/EditMode/TestReflection.cs.meta
```

---

### Task A7: HeroSkillFx 비주얼 헬퍼 + EVisual/EData enum

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillFx.cs`

- [ ] **Step 1: CommonEnum 에 enum 값 추가**

`EVisual` 에 (HitImpact/DamagePopup 뒤에) 추가:

```csharp
        //# 영웅 스킬 FX (2026-06-04) — 프리미티브, CHMPool 대상.
        HeroDashFx,        //# 돌진 — 늘어난 큐브
        HeroOrbitBladeFx,  //# 회전 블레이드 — 궤도 큐브 1개
        HeroNovaFx,        //# AOE 노바 — 팽창 반투명 실린더
```

`EData` 에 추가:

```csharp
        HeroSkillLoadout,   //# 영웅 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout.asset
```

- [ ] **Step 2: HeroSkillFx 작성** (월드 비주얼 — 싱글톤 null 가드로 EditMode 안전)

```csharp
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 비주얼 스폰 헬퍼. CHMResource/CHMPool 미가용(EditMode/부트 전)이면 무동작.
    //# 자동 반환은 프리팹의 ReturnToPoolAfter 가 담당(빌더가 부착).
    public static class HeroSkillFx
    {
        //# 디스크/노바 — center 에 균일 스케일 비주얼 1개.
        public static void SpawnAt(EVisual key, Vector3 center, float scale)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null) return;
            CHMResource.Instance.Load<GameObject>(key, prefab =>
            {
                if (prefab == null) return;
                CHPoolable p = CHMPool.Instance.Pop(prefab, null);
                if (p == null) return;
                p.transform.position = center;
                p.transform.localScale = Vector3.one * scale;
            });
        }

        //# 라인/돌진 — origin 에서 dir 로 length 만큼 늘어난 비주얼.
        public static void SpawnLine(EVisual key, Vector3 origin, Vector3 dir, float length)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null) return;
            CHMResource.Instance.Load<GameObject>(key, prefab =>
            {
                if (prefab == null) return;
                CHPoolable p = CHMPool.Instance.Pop(prefab, null);
                if (p == null) return;
                Vector3 d = dir; d.y = 0f;
                Vector3 mid = origin + d.normalized * (length * 0.5f);
                p.transform.position = new Vector3(mid.x, origin.y, mid.z);
                p.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                p.transform.localScale = new Vector3(0.4f, 0.4f, length);
            });
        }

        //# 영웅 추적 궤도 비주얼 1개 Pop (회전 블레이드용). 핸들 반환(없으면 null).
        public static CHPoolable SpawnTracked(EVisual key)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null) return null;
            GameObject prefab = CHMResource.Instance.Load<GameObject>(key, null);
            if (prefab == null) return null;
            return CHMPool.Instance.Pop(prefab, null);
        }
    }
}
```

> **검증 필요:** `CHMResource.Instance.Load<GameObject>(key, null)` 동기 반환 시그니처가 패키지에 존재하는지 확인. 없으면 `SpawnTracked` 는 콜백형으로 바꾼다(PoisonAura.RequestVisualAt 참고). 회전 블레이드(A-Phase B)에서만 쓰이므로 A7 시점엔 `SpawnAt`/`SpawnLine` 만 검증해도 된다.

- [ ] **Step 3: A6 의 비주얼 줄 활성화**

`DashStrikeRuntime.Tick` 의 `//# TODO A7/A9` 주석을 풀어 `HeroSkillFx.SpawnLine(EVisual.HeroDashFx, ...)` 활성화.

- [ ] **Step 4: 컴파일 + EditMode 재실행** — Expected: `DashStrikeSkillTests` 여전히 PASS (FX 는 싱글톤 null 가드로 no-op).

- [ ] **Step 5: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillFx.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillFx.cs.meta Assets/_Lair/Scripts/Character/Skills/DashStrikeSkillData.cs
```

---

### Task A8: HeroSkillLoadout SO

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillLoadout.cs`

- [ ] **Step 1: 작성**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 페이즈 정의 — {HP비율, 스킬} 순서 리스트. CHMResource 로 로드(EData.HeroSkillLoadout).
    [CreateAssetMenu(fileName = "HeroSkillLoadout", menuName = "Lair/Hero Skill Loadout")]
    public class HeroSkillLoadout : ScriptableObject
    {
        [System.Serializable]
        public class Phase
        {
            [Range(0f, 1f)] public float HpFraction = 1f;
            public HeroSkillData Skill;
        }

        [SerializeField] private List<Phase> _phases = new();
        public IReadOnlyList<Phase> Phases => _phases;
    }
}
```

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/HeroSkillLoadout.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillLoadout.cs.meta
```

---

### Task A9: HeroSkillContext 실구현

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs`

> 데미지 적용은 월드 의존(`CharacterRegistry`)이라 EditMode 순수 테스트 대상이 아니다 — PlayMode 통합(Task D3)에서 검증. 기하는 이미 `SkillGeometry`(A2)로 테스트됨.

- [ ] **Step 1: 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# IHeroSkillContext 실구현 — CharacterRegistry.Monsters(살아있음+교전)를 SkillGeometry 로 필터링해 데미지/넉백.
    public class HeroSkillContext : IHeroSkillContext
    {
        private readonly Transform _hero;

        public HeroSkillContext(Transform hero) => _hero = hero;

        public Vector3 HeroPosition => _hero != null ? _hero.position : Vector3.zero;

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            int hit = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false) continue;
                if (SkillGeometry.InRing(e.Transform.position, origin, innerRadius, outerRadius) == false) continue;
                Apply(e, origin, amount, knockbackStrength);
                ++hit;
            }
            return hit;
        }

        public int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            int hit = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false) continue;
                if (SkillGeometry.InLine(e.Transform.position, origin, direction, length, halfWidth) == false) continue;
                Apply(e, origin, amount, knockbackStrength);
                ++hit;
            }
            return hit;
        }

        public Vector3 MonsterCentroid(float radius)
        {
            if (CharacterRegistry.TryGetThreatCentroidMonster(HeroPosition, radius, out Vector3 c, out int n) && n > 0)
                return c;
            return HeroPosition;
        }

        private static bool Valid(CharacterRegistry.Entry e)
            => e != null && e.Transform != null && e.Health != null && e.Health.IsAlive && e.IsEngaging;

        private static void Apply(CharacterRegistry.Entry e, Vector3 origin, int amount, float knockback)
        {
            e.Health.TakeDamage(amount);
            if (knockback > 0f)
            {
                Vector3 away = e.Transform.position - origin;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    e.Transform.position += away.normalized * knockback;
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs.meta
```

---

### Task A10: HeroSkillRunner MonoBehaviour

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs`

- [ ] **Step 1: 작성**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 부착 — 로드아웃 페이즈를 HP% 게이트로 활성화하고 활성 스킬을 매 프레임 Tick.
    //# 영웅은 풀 객체(count 1) → OnEnable/OnDisable 에서 활성 상태 리셋(HeroAuraRunner 패턴).
    [RequireComponent(typeof(Health))]
    public class HeroSkillRunner : MonoBehaviour
    {
        private IHealth _health;
        private HeroSkillLoadout _loadout;
        private HeroSkillPhaseGate _gate;
        private IHeroSkillContext _ctx;

        private readonly List<IHeroSkillRuntime> _active = new();
        private readonly List<int> _newly = new();

        private void Awake()
        {
            _health = GetComponent<IHealth>();
            _ctx = new HeroSkillContext(transform);
        }

        //# BattleController 가 로드아웃 로드 후 주입. 게이트를 페이즈 HP비율로 구성.
        public void Bind(HeroSkillLoadout loadout)
        {
            _loadout = loadout;
            if (loadout == null) { _gate = null; return; }
            List<float> fractions = new List<float>(loadout.Phases.Count);
            foreach (HeroSkillLoadout.Phase p in loadout.Phases) fractions.Add(p.HpFraction);
            _gate = new HeroSkillPhaseGate(fractions);
            ResetActive();
        }

        private void OnEnable() => _gate?.Reset();

        private void OnDisable() => ResetActive();

        private void Update()
        {
            if (_loadout == null || _gate == null || _health == null || _health.IsAlive == false) return;

            _gate.Poll(_health.Ratio, _newly);
            for (int i = 0; i < _newly.Count; ++i)
            {
                HeroSkillData data = _loadout.Phases[_newly[i]].Skill;
                if (data != null) _active.Add(data.CreateRuntime());
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < _active.Count; ++i) _active[i].Tick(_ctx, dt);
        }

        private void ResetActive()
        {
            for (int i = 0; i < _active.Count; ++i) _active[i].OnDeactivate();
            _active.Clear();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs.meta
```

---

### Task A11: FX 프리팹 빌더 — 3종 추가

**Files:**
- Modify: `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs`

> 기존 `BuildVisual` 은 균일 스케일 부착물용이라 런타임에 스케일을 덮어쓴다(HeroSkillFx 가 localScale 설정). 노바/돌진/궤도 FX 는 단색 프리미티브 + Collider 제거 + 자동 풀 반환(`CHPoolable`+`ReturnToPoolAfter`)이 필요하다.

- [ ] **Step 1: FX 스펙 + 빌드 메서드 추가**

`BuildAllVisuals()` 의 `BuildDamagePopup(...)` 호출 뒤에 추가:

```csharp
            //# 영웅 스킬 FX 3종 (2026-06-04) — 자동 풀 반환 포함.
            BuildHeroSkillFx(EVisual.HeroNovaFx,       PrimitiveType.Cylinder, "#FBBF24", 0.5f, settings, group);
            BuildHeroSkillFx(EVisual.HeroDashFx,       PrimitiveType.Cube,     "#93C5FD", 1.0f, settings, group);
            BuildHeroSkillFx(EVisual.HeroOrbitBladeFx, PrimitiveType.Cube,     "#E5E7EB", 1.0f, settings, group);
```

새 메서드 (클래스 내 추가):

```csharp
        //# 영웅 스킬 FX — 단색 프리미티브 + Collider 제거 + CHPoolable + ReturnToPoolAfter(수명 0.5s).
        //# 스케일/회전은 런타임(HeroSkillFx)이 덮어쓴다 — 여기선 메시·색·풀 컴포넌트만.
        private static void BuildHeroSkillFx(EVisual key, PrimitiveType mesh, string colorHex, float alpha,
            AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            string prefabName = key.ToString();
            GameObject go = GameObject.CreatePrimitive(mesh);
            go.name = prefabName;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            ColorUtility.TryParseHtmlString(colorHex, out Color c);
            c.a = alpha;

            string matPath = $"{MaterialDir}/Mat_{prefabName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool created = mat == null;
            if (created)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                if (alpha < 1f)
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                }
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            if (created) AssetDatabase.CreateAsset(mat, matPath);
            else EditorUtility.SetDirty(mat);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            //# 자동 풀 반환 — 노바/돌진은 짧은 fire-and-forget. 궤도(회전)는 러너가 OnDeactivate 로 회수하지만
            //# 안전망으로 동일 컴포넌트 부착(수명 길게). 수명값은 게임 디자이너 튜닝 — 기본 0.5s.
            go.AddComponent<CHPoolable>();
            go.AddComponent<ReturnToPoolAfter>();

            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            RegisterAddressable(settings, group, prefabPath, prefabName);
            Debug.Log($"[LairVisualPrefabBuilder] {prefabName} FX 프리팹 생성 + Addressables 등록");
        }
```

> **확인:** `ReturnToPoolAfter` 의 수명 필드명/설정 방식을 `Scripts/Character/ReturnToPoolAfter.cs` 에서 읽고, 회전 블레이드(지속형)는 수명을 충분히 길게 두거나 별도 처리한다. 궤도 비주얼은 Phase B 에서 정밀화.

- [ ] **Step 2: 빌더 실행**

Unity 메뉴: `Lair > Setup > B1 - Build Visual Prefabs`
Expected: 콘솔에 `HeroNovaFx/HeroDashFx/HeroOrbitBladeFx FX 프리팹 생성 + Addressables 등록` 3줄.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Editor/LairVisualPrefabBuilder.cs Assets/_Lair/Art/FX/HeroNovaFx.prefab Assets/_Lair/Art/FX/HeroNovaFx.prefab.meta Assets/_Lair/Art/FX/HeroDashFx.prefab Assets/_Lair/Art/FX/HeroDashFx.prefab.meta Assets/_Lair/Art/FX/HeroOrbitBladeFx.prefab Assets/_Lair/Art/FX/HeroOrbitBladeFx.prefab.meta Assets/_Lair/Art/Materials/Mat_HeroNovaFx.mat Assets/_Lair/Art/Materials/Mat_HeroNovaFx.mat.meta Assets/_Lair/Art/Materials/Mat_HeroDashFx.mat Assets/_Lair/Art/Materials/Mat_HeroDashFx.mat.meta Assets/_Lair/Art/Materials/Mat_HeroOrbitBladeFx.mat Assets/_Lair/Art/Materials/Mat_HeroOrbitBladeFx.mat.meta
```

---

### Task A12: 스킬 SO `.asset` + 로드아웃 빌더

**Files:**
- Create: `Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs`

> SO 수치는 game-designer 기획서 §수치표가 단일 진실. 빌더는 *기본값으로 생성*하고, 값 조정은 인스펙터/기획서 동기화로 한다. (Phase A 는 Dash 만 채워도 되지만, 3개 .asset + loadout 을 한 번에 생성하고 P2/P3 스킬 필드는 이후 Phase 에서 SO 가 생기면 채운다.)

- [ ] **Step 1: 빌더 작성** (Dash 만 우선 — Orbit/Nova SO 는 Phase B/C 에서 타입 존재 후 추가)

```csharp
using System.IO;
using Lair.Character;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 영웅 스킬 SO + 로드아웃 .asset 생성 + 로드아웃 Addressable 등록(EData.HeroSkillLoadout).
    public static class LairHeroSkillAssetBuilder
    {
        public const string Dir = "Assets/_Lair/Art/Skills";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        [MenuItem("Lair/Setup/Build Hero Skill Assets")]
        public static void BuildAll()
        {
            EnsureDir(Dir);

            //# P1 Dash — 기본 수치(기획서로 조정).
            DashStrikeSkillData dash = LoadOrCreate<DashStrikeSkillData>($"{Dir}/HeroSkill_DashStrike.asset");

            //# 로드아웃 — 페이즈 3개(90/60/30). Skill 참조는 존재하는 SO 만 연결.
            HeroSkillLoadout loadout = LoadOrCreate<HeroSkillLoadout>($"{Dir}/HeroSkillLoadout.asset");
            SerializedObject so = new SerializedObject(loadout);
            SerializedProperty phases = so.FindProperty("_phases");
            phases.ClearArray();
            AddPhase(phases, 0.9f, dash);
            //# P2/P3 는 Phase B/C 완료 후 이 빌더에 추가.
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterAddressable($"{Dir}/HeroSkillLoadout.asset", "HeroSkillLoadout");
            Debug.Log("[LairHeroSkillAssetBuilder] Hero Skill assets 빌드 완료");
        }

        private static void AddPhase(SerializedProperty phases, float hpFraction, Object skill)
        {
            int i = phases.arraySize;
            phases.InsertArrayElementAtIndex(i);
            SerializedProperty el = phases.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("HpFraction").floatValue = hpFraction;
            el.FindPropertyRelative("Skill").objectReferenceValue = skill;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("Addressables 미설정"); return; }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, true, true, false);
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureDir(string path)
        {
            if (Directory.Exists(path) == false) { Directory.CreateDirectory(path); AssetDatabase.Refresh(); }
        }
    }
}
```

- [ ] **Step 2: 실행** — Unity 메뉴 `Lair > Setup > Build Hero Skill Assets`
Expected: `Art/Skills/HeroSkill_DashStrike.asset` + `HeroSkillLoadout.asset` 생성, 로드아웃 Addressable 등록.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs.meta Assets/_Lair/Art/Skills
```

---

### Task A13: Knight 프리팹에 HeroSkillRunner 부착 (빌더 경유)

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs`

> 먼저 `LairCharacterPrefabBuilder.cs` 전문을 읽어 Knight 프리팹 빌드 함수·컴포넌트 부착 패턴을 파악한다. `HeroAuraRunner`/`HeroAttackGate` 등 영웅 전용 컴포넌트가 어디서 `AddComponent` 되는지 찾아 그 옆에 `HeroSkillRunner` 를 추가한다.

- [ ] **Step 1: Knight 빌드 경로에 컴포넌트 추가**

Knight(영웅) 프리팹을 구성하는 메서드 안, 영웅 전용 컴포넌트 부착부에 추가:

```csharp
            //# 영웅 스킬 러너 (2026-06-04) — 로드아웃은 BattleController 가 런타임 Bind.
            if (heroRoot.GetComponent<Lair.Character.HeroSkillRunner>() == null)
                heroRoot.AddComponent<Lair.Character.HeroSkillRunner>();
```

> `heroRoot` 는 해당 빌더에서 영웅 루트 GameObject 변수명으로 치환한다. `RequireComponent(typeof(Health))` 라 Health 부착 이후 줄에 둔다.

- [ ] **Step 2: 영웅 프리팹 재빌드**

Unity 메뉴: 영웅/캐릭터 프리팹 빌드 메뉴 실행 (LairCharacterPrefabBuilder 의 `[MenuItem]` — 파일에서 확인).
Expected: Knight 프리팹에 `HeroSkillRunner` 컴포넌트 존재.

- [ ] **Step 3: 확인** — UnityMCP `component_get_all` 로 Knight 프리팹의 컴포넌트 목록에 `HeroSkillRunner` 포함 확인.

- [ ] **Step 4: Checkpoint**

```bash
git add Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs Assets/_Lair/Art/Characters/Knight.prefab
```

---

### Task A14: BattleController — 로드아웃 로드 + Bind + 프리워밍

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: SpawnHero 에서 러너 Bind**

`SpawnHero()` 내 `ApplyStats(...)` 호출 이후, 영웅 AI 비활성 처리 부근에 추가:

```csharp
            //# 영웅 스킬 — 로드아웃 로드 후 러너에 주입(런타임 Bind, 풀 reference 안전).
            HeroSkillRunner skillRunner = p.GetComponent<HeroSkillRunner>();
            if (skillRunner != null)
            {
                HeroSkillLoadout loadout = await CHMResource.Instance.LoadAsync<HeroSkillLoadout>(EData.HeroSkillLoadout);
                if (loadout != null) skillRunner.Bind(loadout);
                else Debug.LogWarning("[BattleController] HeroSkillLoadout 로드 실패 — 영웅 스킬 비활성");
            }
```

> `HeroSkillRunner` / `HeroSkillLoadout` 은 `Lair.Character` 네임스페이스 — 파일 상단 `using Lair.Character;` 이미 존재(확인).

- [ ] **Step 2: PrewarmPools 에 FX 3종 추가**

`PrewarmPools()` 의 EVisual 프리워밍 배열에 추가하거나 별도 루프:

```csharp
            //# 영웅 스킬 FX — 동시 표시 적음. count 4 (궤도 1 + 돌진/노바 순간).
            foreach (EVisual key in new[] { EVisual.HeroDashFx, EVisual.HeroOrbitBladeFx, EVisual.HeroNovaFx })
            {
                GameObject fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (fx != null) CHMPool.Instance.CreatePool(fx, count: 4);
            }
```

- [ ] **Step 3: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 4: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs
```

---

### Task A15: Phase A 통합 스모크 (수동 + 최소 PlayMode)

- [ ] **Step 1: 전체 EditMode 테스트 그린 확인**

Run: EditMode 전체
Expected: 신규 테스트 전부 PASS, 기존 회귀 없음.

- [ ] **Step 2: 수동 플레이 확인**

Battle 씬 Play → 영웅 HP 90% 하향 시 돌진 FX(파란 큐브)가 몬스터 밀집 방향으로 발생하고 일직선 몬스터가 데미지 받는지 육안 확인. (디버그: `LairBalanceWindow` 의 `DebugSetHeroHp` 로 HP 강제 하락.)

- [ ] **Step 3: Checkpoint** (Phase A 완료)

---

## Phase B — Orbiting Blade (P2, HP 60%)

### Task B1: OrbitingBladeSkillData + 런타임 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/OrbitingBladeSkillData.cs`
- Test: `Assets/_Lair/Tests/EditMode/OrbitingBladeSkillTests.cs`

**동작**: 지속형. `_hitInterval` 마다 영웅 중심 링밴드 `[radius-half, radius+half]` 내 몬스터에 데미지. 비주얼은 궤도 큐브 N개를 각속도로 회전.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class OrbitingBladeSkillTests
    {
        private static IHeroSkillRuntime MakeRuntime()
        {
            OrbitingBladeSkillData data = ScriptableObject.CreateInstance<OrbitingBladeSkillData>();
            TestReflection.SetField(data, "_damage", 20);
            TestReflection.SetField(data, "_hitInterval", 0.5f);
            TestReflection.SetField(data, "_orbitRadius", 2f);
            TestReflection.SetField(data, "_bandHalfThickness", 0.5f);
            TestReflection.SetField(data, "_bladeCount", 2);
            TestReflection.SetField(data, "_rotationSpeedDeg", 180f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_인터벌_미만이면_데미지없음()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 3 };
            rt.Tick(ctx, 0.3f);
            Assert.AreEqual(0, ctx.RingCalls.Count);
        }

        [Test]
        public void Tick_인터벌_도달시_링데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 3 };
            rt.Tick(ctx, 0.5f);
            Assert.AreEqual(1, ctx.RingCalls.Count);
            Assert.AreEqual(20, ctx.RingCalls[0].Amount);
            Assert.AreEqual(1.5f, ctx.RingCalls[0].Inner, 0.001f);   //# 2 - 0.5
            Assert.AreEqual(2.5f, ctx.RingCalls[0].Outer, 0.001f);   //# 2 + 0.5
        }

        [Test]
        public void Tick_긴dt_여러인터벌_누적틱()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 1 };
            rt.Tick(ctx, 1.2f);   //# 0.5 인터벌 → 2회 (1.0 소비, 0.2 잔여)
            Assert.AreEqual(2, ctx.RingCalls.Count);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Expected: FAIL — 타입 미정의.

- [ ] **Step 3: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# P2 (HP 60%) — 영웅 주위를 도는 궤도 블레이드. 링밴드 내 몬스터에 인터벌마다 지속 데미지.
    [CreateAssetMenu(fileName = "HeroSkill_OrbitingBlade", menuName = "Lair/Hero Skills/Orbiting Blade")]
    public class OrbitingBladeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 20;
        [SerializeField] private float _hitInterval = 0.4f;
        [SerializeField] private float _orbitRadius = 2f;
        [SerializeField] private float _bandHalfThickness = 0.5f;
        [SerializeField] private int _bladeCount = 2;
        [SerializeField] private float _rotationSpeedDeg = 180f;

        public int Damage => _damage;
        public float HitInterval => _hitInterval;
        public float OrbitRadius => _orbitRadius;
        public float BandHalfThickness => _bandHalfThickness;
        public int BladeCount => _bladeCount;
        public float RotationSpeedDeg => _rotationSpeedDeg;

        public override IHeroSkillRuntime CreateRuntime() => new OrbitingBladeRuntime(this);
    }

    public class OrbitingBladeRuntime : IHeroSkillRuntime
    {
        private readonly OrbitingBladeSkillData _data;
        private float _accum;
        private float _angleDeg;
        private readonly ChvjUnityInfra.CHPoolable[] _blades;

        public OrbitingBladeRuntime(OrbitingBladeSkillData data)
        {
            _data = data;
            _blades = new ChvjUnityInfra.CHPoolable[Mathf.Max(1, data.BladeCount)];
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            //# 데미지 — 인터벌 누적.
            _accum += dt;
            while (_accum >= _data.HitInterval)
            {
                _accum -= _data.HitInterval;
                ctx.DamageMonstersInRing(
                    _data.OrbitRadius - _data.BandHalfThickness,
                    _data.OrbitRadius + _data.BandHalfThickness,
                    _data.Damage, 0f);
            }

            //# 비주얼 — 궤도 큐브 회전(가용 시).
            _angleDeg += _data.RotationSpeedDeg * dt;
            UpdateBlades(ctx.HeroPosition);
        }

        private void UpdateBlades(Vector3 heroPos)
        {
            if (ChvjUnityInfra.CHMResource.Instance == null || ChvjUnityInfra.CHMPool.Instance == null) return;
            float step = 360f / _blades.Length;
            for (int i = 0; i < _blades.Length; ++i)
            {
                if (_blades[i] == null)
                    _blades[i] = HeroSkillFx.SpawnTracked(Lair.Data.EVisual.HeroOrbitBladeFx);
                if (_blades[i] == null) continue;
                float a = (_angleDeg + step * i) * Mathf.Deg2Rad;
                Vector3 pos = heroPos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _data.OrbitRadius;
                _blades[i].transform.position = pos;
            }
        }

        public void OnDeactivate()
        {
            if (ChvjUnityInfra.CHMPool.Instance == null) return;
            for (int i = 0; i < _blades.Length; ++i)
            {
                if (_blades[i] != null) ChvjUnityInfra.CHMPool.Instance.Push(_blades[i]);
                _blades[i] = null;
            }
        }
    }
}
```

> **확인:** 궤도 블레이드 FX 프리팹의 `ReturnToPoolAfter` 가 비주얼을 조기 회수하면 궤도가 깜빡인다. 지속형 비주얼은 `ReturnToPoolAfter` 를 빼거나 수명을 매우 길게 둔다 — A11 빌더에서 `HeroOrbitBladeFx` 만 `ReturnToPoolAfter` 미부착으로 분기. (A11 Step 1 의 `BuildHeroSkillFx` 에 `addAutoReturn` bool 파라미터 추가해 OrbitBlade 만 false 전달.)

- [ ] **Step 4: 통과 확인** — Expected: PASS (3 tests).

- [ ] **Step 5: A11 빌더 보정** — `HeroOrbitBladeFx` 는 `ReturnToPoolAfter` 미부착하도록 빌더 수정 후 재빌드.

- [ ] **Step 6: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/OrbitingBladeSkillData.cs Assets/_Lair/Scripts/Character/Skills/OrbitingBladeSkillData.cs.meta Assets/_Lair/Tests/EditMode/OrbitingBladeSkillTests.cs Assets/_Lair/Tests/EditMode/OrbitingBladeSkillTests.cs.meta Assets/_Lair/Editor/LairVisualPrefabBuilder.cs Assets/_Lair/Art/FX/HeroOrbitBladeFx.prefab
```

---

### Task B2: 로드아웃에 P2 페이즈 추가

**Files:**
- Modify: `Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs`

- [ ] **Step 1: 빌더에 Orbit SO + P2 페이즈 추가**

`BuildAll()` 에 추가:

```csharp
            OrbitingBladeSkillData orbit = LoadOrCreate<OrbitingBladeSkillData>($"{Dir}/HeroSkill_OrbitingBlade.asset");
```

`AddPhase(phases, 0.9f, dash);` 다음 줄:

```csharp
            AddPhase(phases, 0.6f, orbit);
```

- [ ] **Step 2: 재실행** — `Lair > Setup > Build Hero Skill Assets`
Expected: `HeroSkill_OrbitingBlade.asset` 생성, 로드아웃에 P2(0.6) 페이즈 추가.

- [ ] **Step 3: 수동 확인** — Play 중 HP 60% 하향 시 궤도 큐브가 영웅 주위를 돌며 근접 몬스터에 데미지.

- [ ] **Step 4: Checkpoint**

```bash
git add Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs Assets/_Lair/Art/Skills/HeroSkill_OrbitingBlade.asset Assets/_Lair/Art/Skills/HeroSkillLoadout.asset
```

---

## Phase C — AOE Nova (P3, HP 30%)

### Task C1: AoeNovaSkillData + 런타임 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Skills/AoeNovaSkillData.cs`
- Test: `Assets/_Lair/Tests/EditMode/AoeNovaSkillTests.cs`

**동작**: 쿨다운마다 영웅 중심 디스크(반경) 일괄 데미지 + 넉백. 비주얼은 팽창 실린더.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class AoeNovaSkillTests
    {
        private static IHeroSkillRuntime MakeRuntime()
        {
            AoeNovaSkillData data = ScriptableObject.CreateInstance<AoeNovaSkillData>();
            TestReflection.SetField(data, "_damage", 80);
            TestReflection.SetField(data, "_cooldown", 4f);
            TestReflection.SetField(data, "_radius", 3f);
            TestReflection.SetField(data, "_knockbackStrength", 3f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_쿨다운_경과전엔_발동안함()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 2f);
            Assert.AreEqual(0, ctx.RingCalls.Count);
        }

        [Test]
        public void Tick_쿨다운_경과시_디스크데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 4f);
            Assert.AreEqual(1, ctx.RingCalls.Count);
            Assert.AreEqual(0f, ctx.RingCalls[0].Inner, 0.001f);     //# 디스크 = inner 0
            Assert.AreEqual(3f, ctx.RingCalls[0].Outer, 0.001f);
            Assert.AreEqual(80, ctx.RingCalls[0].Amount);
            Assert.AreEqual(3f, ctx.RingCalls[0].Knockback, 0.001f);
        }

        [Test]
        public void Tick_발동후_쿨다운_재충전()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 4f);
            rt.Tick(ctx, 2f);
            Assert.AreEqual(1, ctx.RingCalls.Count);
            rt.Tick(ctx, 4f);
            Assert.AreEqual(2, ctx.RingCalls.Count);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Expected: FAIL — 타입 미정의.

- [ ] **Step 3: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# P3 (HP 30%) — 쿨다운마다 영웅 주변 원형 폭발. 반경 내 몬스터 일괄 데미지 + 넉백.
    [CreateAssetMenu(fileName = "HeroSkill_AoeNova", menuName = "Lair/Hero Skills/AOE Nova")]
    public class AoeNovaSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 80;
        [SerializeField] private float _cooldown = 4f;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _knockbackStrength = 3f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float Radius => _radius;
        public float KnockbackStrength => _knockbackStrength;

        public override IHeroSkillRuntime CreateRuntime() => new AoeNovaRuntime(this);
    }

    public class AoeNovaRuntime : IHeroSkillRuntime
    {
        private readonly AoeNovaSkillData _data;
        private float _cooldownRemain;

        public AoeNovaRuntime(AoeNovaSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            if (_cooldownRemain > 0f) return;

            ctx.DamageMonstersInRing(0f, _data.Radius, _data.Damage, _data.KnockbackStrength);
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnAt(Lair.Data.EVisual.HeroNovaFx, ctx.HeroPosition, _data.Radius * 2f);
        }

        public void OnDeactivate() { }
    }
}
```

- [ ] **Step 4: 통과 확인** — Expected: PASS (3 tests).

- [ ] **Step 5: Checkpoint**

```bash
git add Assets/_Lair/Scripts/Character/Skills/AoeNovaSkillData.cs Assets/_Lair/Scripts/Character/Skills/AoeNovaSkillData.cs.meta Assets/_Lair/Tests/EditMode/AoeNovaSkillTests.cs Assets/_Lair/Tests/EditMode/AoeNovaSkillTests.cs.meta
```

---

### Task C2: 로드아웃에 P3 페이즈 추가

**Files:**
- Modify: `Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs`

- [ ] **Step 1: 빌더에 Nova SO + P3 페이즈 추가**

```csharp
            AoeNovaSkillData nova = LoadOrCreate<AoeNovaSkillData>($"{Dir}/HeroSkill_AoeNova.asset");
```

```csharp
            AddPhase(phases, 0.3f, nova);
```

- [ ] **Step 2: 재실행 + 수동 확인** — HP 30% 하향 시 노바 폭발 + 넉백.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Editor/LairHeroSkillAssetBuilder.cs Assets/_Lair/Art/Skills/HeroSkill_AoeNova.asset Assets/_Lair/Art/Skills/HeroSkillLoadout.asset
```

---

## Phase D — 통합 검증

### Task D1: HeroSkillContext 단위 PlayMode 테스트

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/HeroSkillContextPlayTests.cs`

- [ ] **Step 1: 테스트 작성** — 실제 GameObject 몬스터를 `CharacterRegistry` 에 등록(Engaging=true)하고 `HeroSkillContext.DamageMonstersInRing/Line` 이 반경 내만 데미지 주는지 검증.

```csharp
using System.Collections;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    public class HeroSkillContextPlayTests
    {
        private GameObject _hero;
        private GameObject _near;
        private GameObject _far;

        [SetUp]
        public void SetUp()
        {
            _hero = new GameObject("Hero");
            _near = MakeMonster("Near", new Vector3(2f, 0f, 0f));   //# 반경 3 안
            _far  = MakeMonster("Far",  new Vector3(8f, 0f, 0f));   //# 반경 3 밖
        }

        private GameObject MakeMonster(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRegistry.UnregisterMonster(_near.transform);
            CharacterRegistry.UnregisterMonster(_far.transform);
            Object.DestroyImmediate(_hero);
            Object.DestroyImmediate(_near);
            Object.DestroyImmediate(_far);
        }

        [Test]
        public void DamageMonstersInRing_반경내만_피격()
        {
            HeroSkillContext ctx = new HeroSkillContext(_hero.transform);
            int nearBefore = _near.GetComponent<Health>().Current;
            int farBefore = _far.GetComponent<Health>().Current;

            int hit = ctx.DamageMonstersInRing(0f, 3f, 100, 0f);

            Assert.AreEqual(1, hit);
            Assert.AreEqual(nearBefore - 100, _near.GetComponent<Health>().Current);
            Assert.AreEqual(farBefore, _far.GetComponent<Health>().Current);
        }
    }
}
```

- [ ] **Step 2: 실행** — Expected: PASS.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Tests/PlayMode/HeroSkillContextPlayTests.cs Assets/_Lair/Tests/PlayMode/HeroSkillContextPlayTests.cs.meta
```

---

### Task D2: HeroSkillRunner 통합 PlayMode 테스트

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/HeroSkillRunnerPlayTests.cs`

- [ ] **Step 1: 테스트 작성** — 영웅 GameObject(Health+HeroSkillRunner) + 로드아웃(코드 생성 SO) Bind 후 HP 를 90% 아래로 내리고 몬스터 1체를 라인 안에 둔 뒤 여러 프레임 Tick(yield) → 몬스터 HP 감소 확인.

```csharp
using System.Collections;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    public class HeroSkillRunnerPlayTests
    {
        [UnityTest]
        public IEnumerator HP90이하_Dash활성_라인몬스터_피격()
        {
            GameObject hero = new GameObject("Hero");
            hero.transform.position = Vector3.zero;
            Health hh = hero.AddComponent<Health>();
            hh.SetMax(1000, true);
            HeroSkillRunner runner = hero.AddComponent<HeroSkillRunner>();

            //# 로드아웃 — Dash 1페이즈(90%), 쿨다운 짧게.
            DashStrikeSkillData dash = ScriptableObject.CreateInstance<DashStrikeSkillData>();
            SetField(dash, "_damage", 50);
            SetField(dash, "_cooldown", 0.05f);
            SetField(dash, "_dashLength", 10f);
            SetField(dash, "_halfWidth", 2f);
            SetField(dash, "_centroidRadius", 20f);
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, dash);
            runner.Bind(loadout);

            //# 라인 안 몬스터.
            GameObject mon = new GameObject("Mon");
            mon.transform.position = new Vector3(0f, 0f, 5f);
            Health mh = mon.AddComponent<Health>();
            mh.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(mon.transform, mh);
            CharacterRegistry.SetMonsterEngaging(mon.transform, true);

            //# HP 89% 로 하락 → Dash 페이즈 활성.
            hh.TakeDamage(110);
            int before = mh.Current;

            //# 몇 프레임 진행(쿨다운 0.05 경과 보장).
            for (int i = 0; i < 10; ++i) yield return null;

            Assert.Less(mh.Current, before, "Dash 가 라인 몬스터에 데미지를 줘야 한다");

            CharacterRegistry.UnregisterMonster(mon.transform);
            Object.DestroyImmediate(mon);
            Object.DestroyImmediate(hero);
        }

        private static void SetField(object t, string f, object v)
            => t.GetType().GetField(f, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(t, v);

        private static void AddPhase(HeroSkillLoadout loadout, float frac, HeroSkillData skill)
        {
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(loadout);
            UnityEditor.SerializedProperty phases = so.FindProperty("_phases");
            int i = phases.arraySize;
            phases.InsertArrayElementAtIndex(i);
            UnityEditor.SerializedProperty el = phases.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("HpFraction").floatValue = frac;
            el.FindPropertyRelative("Skill").objectReferenceValue = skill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
```

> **주의:** `UnityEditor` API 사용 → PlayMode asmdef 에 Editor 참조 필요. 불가하면 `AddPhase` 를 reflection 으로 `_phases` 리스트에 직접 add 하도록 대체(테스트 asmdef 가 `UNITY_EDITOR` 가드 하에서만 동작). 구현 시 `Lair.Tests.PlayMode` asmdef 의 제약 확인.

- [ ] **Step 2: 실행** — Expected: PASS.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Tests/PlayMode/HeroSkillRunnerPlayTests.cs Assets/_Lair/Tests/PlayMode/HeroSkillRunnerPlayTests.cs.meta
```

---

### Task D3: 전체 회귀 + 마무리

- [ ] **Step 1: 전체 테스트 (EditMode + PlayMode) 그린 확인**

Run: `Lair > Test > Run All` (또는 LairTestRunner)
Expected: 신규 전부 PASS, 기존 회귀 없음.

- [ ] **Step 2: 콘솔 에러 0 확인** — Battle 씬 5분 1판 플레이, NRE/풀 경고 없음.

- [ ] **Step 3: qa-simulator 권장 보고** — spec §5 대로, 밸런스(특히 Swarm 카운터·종반 데스스파이럴) 검증을 위해 qa-simulator 별도 호출을 사용자에게 제안.

- [ ] **Step 4: 마무리** — 메인 오케스트레이터가 변경 요약 + 한글 커밋 메시지(안) 제시 (Rule 01).

---

## Phase E — JSON Sync (카드/밸런스와 동일하게 hero_skills.json 양방향 동기화)

> 기존 JSON Sync 시스템(`Lair > JSON Sync` 창)은 Cards/CardPools/BalanceConfig 3종을 수동 Export/Import 한다. 영웅 스킬 SO 도 동일하게 `hero_skills.json` 으로 동기화한다.
> 패턴 미러: 폴리모픽 `$type` = `EffectConverter`, `[SerializeField]`→JSON 키 = `UnitySerializeFieldContractResolver`, 파일명 ref = `CardPoolSyncer`, SO 필드 적용 = `BalanceConfigSyncer`.
> **선행 조건**: Phase A~C 의 SO 타입 3종(`DashStrikeSkillData`/`OrbitingBladeSkillData`/`AoeNovaSkillData`) + `HeroSkillLoadout` + `.asset` 4종이 이미 존재해야 한다(Phase E 는 마지막).

### Task E1: HeroSkillsDto + 폴리모픽 컨버터

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/Dto/HeroSkillsDto.cs`
- Create: `Assets/_Lair/Editor/JsonSync/HeroSkillDataConverter.cs`

- [ ] **Step 1: DTO 작성**

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Lair.Character;

namespace Lair.EditorTools
{
    //# hero_skills.json 루트 — 스킬 정의(폴리모픽) + 로드아웃 페이즈(파일명 ref).
    public class HeroSkillsDto
    {
        [JsonProperty("skills")] public List<HeroSkillData> Skills = new List<HeroSkillData>();
        [JsonProperty("loadout")] public List<HeroSkillPhaseDto> Loadout = new List<HeroSkillPhaseDto>();
    }

    public class HeroSkillPhaseDto
    {
        [JsonProperty("hpFraction")] public float HpFraction;
        //# 스킬 .asset 파일명(확장자 제외). 예: "HeroSkill_DashStrike".
        [JsonProperty("skill")] public string Skill;
    }
}
```

- [ ] **Step 2: 폴리모픽 컨버터 작성** (EffectConverter 미러 — SO 라 ScriptableObject.CreateInstance 사용)

```csharp
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lair.Character;
using UnityEngine;

namespace Lair.EditorTools
{
    //# HeroSkillData 폴리모픽 직렬화 — $type 으로 구상 SO 타입 기록/복원 + fileName 보존.
    //# 가변 상태 없는 데이터 SO 라 CreateInstance 후 Populate 안전.
    public class HeroSkillDataConverter : JsonConverter<HeroSkillData>
    {
        private readonly JsonSerializer _inner;

        public HeroSkillDataConverter()
        {
            _inner = new JsonSerializer { ContractResolver = new UnitySerializeFieldContractResolver() };
        }

        public override HeroSkillData ReadJson(JsonReader reader, Type objectType,
            HeroSkillData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            JObject jo = JObject.Load(reader);
            string typeName = jo["$type"]?.Value<string>();
            if (string.IsNullOrEmpty(typeName)) return null;

            Type type = FindSkillType(typeName);
            if (type == null) throw new JsonException($"[HeroSkillDataConverter] 알 수 없는 스킬 타입: {typeName}");

            HeroSkillData skill = (HeroSkillData)ScriptableObject.CreateInstance(type);
            using (JsonReader jr = jo.CreateReader()) _inner.Populate(jr, skill);
            return skill;
        }

        public override void WriteJson(JsonWriter writer, HeroSkillData value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            JObject jo = JObject.FromObject(value, _inner);
            jo.AddFirst(new JProperty("fileName", value.name));
            jo.AddFirst(new JProperty("$type", value.GetType().Name));
            jo.WriteTo(writer);
        }

        private static Type FindSkillType(string typeName)
        {
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType($"Lair.Character.{typeName}");
                if (t != null) return t;
            }
            return null;
        }
    }
}
```

> **검증:** `JObject.FromObject(value, _inner)` 가 SO 의 `[SerializeField]` 필드를 직렬화하는지 확인(ContractResolver 가 처리). `value.name` 은 .asset 파일명과 일치(Unity SO 의 name = 에셋명). Populate 시 `fileName`/`$type` 키는 SO 필드에 없어 무시됨.

- [ ] **Step 3: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 4: Checkpoint**

```bash
git add Assets/_Lair/Editor/JsonSync/Dto/HeroSkillsDto.cs Assets/_Lair/Editor/JsonSync/Dto/HeroSkillsDto.cs.meta Assets/_Lair/Editor/JsonSync/HeroSkillDataConverter.cs Assets/_Lair/Editor/JsonSync/HeroSkillDataConverter.cs.meta
```

---

### Task E2: HeroSkillSyncer (Export/Import)

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/HeroSkillSyncer.cs`

- [ ] **Step 1: 작성**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Character;

namespace Lair.EditorTools
{
    //# 영웅 스킬 SO 3종 + HeroSkillLoadout ↔ hero_skills.json 양방향 동기화.
    public static class HeroSkillSyncer
    {
        private const string JsonPath    = "Assets/_Lair/Data/Json/hero_skills.json";
        private const string SkillDir    = "Assets/_Lair/Art/Skills";
        private const string LoadoutPath = "Assets/_Lair/Art/Skills/HeroSkillLoadout.asset";

        private static JsonSerializerSettings Settings()
        {
            JsonSerializerSettings s = JsonSyncSettings.Build();
            s.Converters.Add(new HeroSkillDataConverter());
            return s;
        }

        public static void Export()
        {
            //# 모든 HeroSkillData .asset 수집 (Art/Skills).
            List<HeroSkillData> skills = new List<HeroSkillData>();
            foreach (string guid in AssetDatabase.FindAssets("t:HeroSkillData", new[] { SkillDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroSkillData s = AssetDatabase.LoadAssetAtPath<HeroSkillData>(path);
                if (s != null) skills.Add(s);
            }

            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(LoadoutPath);
            List<HeroSkillPhaseDto> phases = new List<HeroSkillPhaseDto>();
            if (loadout != null)
            {
                foreach (HeroSkillLoadout.Phase p in loadout.Phases)
                    phases.Add(new HeroSkillPhaseDto { HpFraction = p.HpFraction, Skill = p.Skill != null ? p.Skill.name : null });
            }

            HeroSkillsDto dto = new HeroSkillsDto { Skills = skills, Loadout = phases };
            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(dto, Settings()), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkillSyncer] Export → {JsonPath}");
        }

        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            //# 컨버터가 만든 in-memory SO 를 .asset 으로 반영하기 위해 JObject 로 fileName 도 읽는다.
            Newtonsoft.Json.Linq.JObject root = Newtonsoft.Json.Linq.JObject.Parse(json);
            JsonSerializer ser = JsonSerializer.Create(Settings());

            //# 스킬 — fileName 으로 기존 .asset 에 필드 적용(없으면 생성).
            foreach (Newtonsoft.Json.Linq.JObject sj in root["skills"].Cast<Newtonsoft.Json.Linq.JObject>())
            {
                string fileName = sj["fileName"]?.Value<string>();
                HeroSkillData parsed = (HeroSkillData)ser.Deserialize(sj.CreateReader(), typeof(HeroSkillData));
                if (parsed == null || string.IsNullOrEmpty(fileName)) continue;

                string assetPath = $"{SkillDir}/{fileName}.asset";
                HeroSkillData existing = AssetDatabase.LoadAssetAtPath<HeroSkillData>(assetPath);
                if (existing == null || existing.GetType() != parsed.GetType())
                {
                    if (existing != null) AssetDatabase.DeleteAsset(assetPath);   //# 타입 변경 시 교체
                    AssetDatabase.CreateAsset(parsed, assetPath);
                }
                else
                {
                    EditorUtility.CopySerialized(parsed, existing);   //# 필드 일괄 복사 → 기존 GUID 보존
                }
            }

            //# 로드아웃 — fileName ref 로 스킬 연결.
            HeroSkillsDto dto = JsonConvert.DeserializeObject<HeroSkillsDto>(json, Settings());
            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(LoadoutPath);
            if (loadout != null)
            {
                SerializedObject so = new SerializedObject(loadout);
                SerializedProperty phases = so.FindProperty("_phases");
                phases.ClearArray();
                for (int i = 0; i < dto.Loadout.Count; ++i)
                {
                    phases.InsertArrayElementAtIndex(i);
                    SerializedProperty el = phases.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("HpFraction").floatValue = dto.Loadout[i].HpFraction;
                    HeroSkillData s = AssetDatabase.LoadAssetAtPath<HeroSkillData>($"{SkillDir}/{dto.Loadout[i].Skill}.asset");
                    el.FindPropertyRelative("Skill").objectReferenceValue = s;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(loadout);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkillSyncer] Import ← {JsonPath}");
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
        }
    }
}
```

> **검증 포인트(구현 중):**
> - `HeroSkillsDto.Skills` 역직렬화 시 컨버터가 동작하려면 `Settings()` 에 컨버터 등록 필요(위 반영). `DeserializeObject<HeroSkillsDto>` 의 Skills 리스트는 fileName 을 잃으므로, .asset 반영은 JObject 순회 경로(위)가 담당하고 DeserializeObject 결과는 loadout 에만 쓴다.
> - `EditorUtility.CopySerialized` 가 SO 필드를 복사하되 GUID 보존(로드아웃/Addressable ref 무손상). 타입 변경 시에만 DeleteAsset+CreateAsset.
> - 위 Import 는 2-pass(JObject 로 스킬 .asset 반영 → DeserializeObject 로 로드아웃)로 단순화. 한 번만 Parse 하도록 리팩터 가능하나 명료성 우선.

- [ ] **Step 2: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Editor/JsonSync/HeroSkillSyncer.cs Assets/_Lair/Editor/JsonSync/HeroSkillSyncer.cs.meta
```

---

### Task E3: LairJsonSyncWindow 에 Hero Skills 섹션 연결

**Files:**
- Modify: `Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs`

- [ ] **Step 1: DrawSection 추가**

`DrawSection("Balance Config", ...)` 줄 다음에:

```csharp
            DrawSection("Hero Skills",    "hero_skills.json",    HeroSkillSyncer.Export,      HeroSkillSyncer.Import);
```

- [ ] **Step 2: ExportAll/ImportAll 에 추가**

`ExportAll()` 에:

```csharp
            HeroSkillSyncer.Export();
```

`ImportAll()` 에:

```csharp
            if (File.Exists(Path.Combine(JsonDir, "hero_skills.json")))
            {
                HeroSkillSyncer.Import();
            }
```

- [ ] **Step 3: 컴파일 확인** — Expected: 에러 없음.

- [ ] **Step 4: Checkpoint**

```bash
git add Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs
```

---

### Task E4: 라운드트립 검증

- [ ] **Step 1: Export**

Unity 메뉴: `Lair > JSON Sync` → "Hero Skills" 행 Export (또는 Export All)
Expected: `Assets/_Lair/Data/Json/hero_skills.json` 생성. 내용에 skills 3개(각 `$type`/`fileName`/필드) + loadout 3페이즈(hpFraction 0.9/0.6/0.3 + skill 파일명).

- [ ] **Step 2: 값 변경 → Import 왕복**

`hero_skills.json` 에서 Dash `damage` 를 임시로 80→81 변경 → JSON Sync 창 "Hero Skills" Import → `HeroSkill_DashStrike.asset` 의 `_damage` 가 81 로 반영되는지 인스펙터 확인 → 다시 80 으로 되돌리고 Import.
Expected: SO 필드가 JSON 과 일치, 로드아웃 ref 무손상(Addressable `HeroSkillLoadout` 주소 유지).

- [ ] **Step 3: Checkpoint**

```bash
git add Assets/_Lair/Data/Json/hero_skills.json
```

---

## Self-Review

**1. Spec 커버리지:**
- §0 범위 확장 명시 → 본 plan 헤더 + game-designer 단계 위임(§8 표). ✅
- §2 메커니즘(HP 3페이즈/3스킬) → Phase A~C + HeroSkillPhaseGate(A3) + 로드아웃(A8/A12). ✅
- §3 데이터드리븐 폴리모픽 SO → HeroSkillData(A4) + 3 서브클래스 + CreateRuntime. ✅
- §4 데이터 흐름(HP폴링→CreateRuntime→Tick, Pause=dt0) → HeroSkillRunner(A10). ✅
- §5 밸런스 — 수치는 game-designer 위임, qa-simulator 권장(D3 Step 3). ✅ (가드레일 수치는 기획서)
- §6 시퀀싱(돌진→회전→노바) → Phase A/B/C 순. ✅
- §7 테스트/비주얼 → EditMode 순수 + PlayMode 통합 + 프리미티브 FX(A11). ✅
- JSON Sync (사용자 추가 요청 2026-06-04) → Phase E (카드/밸런스와 동일하게 hero_skills.json 양방향). ✅

**2. Placeholder 스캔:** 수치 위임은 의도된 game-designer 경계 (placeholder 아님). 코드 스텝은 전부 완전한 코드 포함. ✅

**3. 타입 일관성:** `IHeroSkillContext`(DamageMonstersInRing/InLine/MonsterCentroid/HeroPosition) — A1 정의 ↔ A5 Fake ↔ A6/B1/C1 사용 ↔ A9 실구현 일치. `IHeroSkillRuntime.Tick/OnDeactivate` 일관. `HeroSkillData.CreateRuntime` 일관. `HeroSkillLoadout.Phase{HpFraction,Skill}` ↔ 게이트/빌더 일치. ✅

**확인 필요(구현 중 검증) 항목:**
- `CHMResource.Instance.Load<GameObject>(key, null)` 동기 시그니처 존재 여부 (A7) — 없으면 콜백형으로.
- `ReturnToPoolAfter` 수명 필드/분기 (A11/B1) — 궤도형은 미부착.
- `LairCharacterPrefabBuilder` 의 영웅 루트 변수명·MenuItem (A13).
- PlayMode asmdef 의 UnityEditor 참조 가능 여부 (D2).

---

## 형태 변경 v0.8 — Task delta (2026-06-04 사용자 확정, 구현 완료)

> 기획서 `docs/design/hero-skills.md` §12 의 형태 변경 7항목을 plan Task 에 반영한 delta. 위 Phase A~E 본문 코드 스니펫(직선 띠·링밴드·Cube/Cylinder 가정)은 **본 delta 가 override** 한다. 데미지·쿨·HP 게이트·색은 불변.

**요지**: P1 직선 띠 → 부채꼴(radial), P2 링밴드 → 구 N개 공전 per-sphere(union dedup), P3 비주얼 Cylinder → Sphere.

| # | Task | 기존(plan 본문) | 신규(v0.8 구현) |
|---|---|---|---|
| 1 | A6 `DashStrikeSkillData` | `_halfWidth = 1.5` | `_coneHalfAngle = 35` (반각, 도). `_halfWidth` 제거 |
| 1 | B1 `OrbitingBladeSkillData` | `_bandHalfThickness = 0.6` / `_orbitRadius = 2` / `_hitInterval = 0.4` / `_bladeCount = 2` | `_bandHalfThickness` 제거, `_bladeSphereRadius = 0.9` 신설 / `_orbitRadius = 1.4` / `_hitInterval = 0.3` / `_bladeCount = 3` |
| 2 | A1 `IHeroSkillContext` | `DamageMonstersInLine(dir, length, halfWidth, …)` | `DamageMonstersInCone(dir, length, halfAngleDeg, …)` (대체) + `DamageMonstersInSpheres(IReadOnlyList<Vector3> centers, radius, …)` (신규, union dedup). `DamageMonstersInLine` 제거 |
| 3 | A2 `SkillGeometry` | `InLine(p, origin, dir, length, halfWidth)` (축투영) | `InCone(p, origin, dir, length, halfAngleDeg)` (radial: 반경거리≤length AND 각도≤halfAngle) + `InSphere(p, center, radius)` (XZ 거리). `InLine` 제거. `InRing` 유지(P3 디스크) |
| 4 | A6 `DashStrikeRuntime` | `ctx.DamageMonstersInLine(...)` + `SpawnLine(...)` | `ctx.DamageMonstersInCone(dir, DashLength, ConeHalfAngle, …)` + `SpawnCone(...)` |
| 4 | B1 `OrbitingBladeRuntime` | `DamageMonstersInRing(R-band, R+band, …)` 단일 | `_angleDeg` 로 구 중심 N개 순수 계산(`ComputeCenters`, transform·인프라 비의존) → `ctx.DamageMonstersInSpheres(centers, BladeSphereRadius, …)`. 비주얼은 같은 centers 로 블레이드 추적(스케일 = `BladeSphereRadius`×2) |
| 5 | A7 `HeroSkillFx` | `SpawnLine(key, origin, dir, length)` (늘린 큐브) | `SpawnCone(key, origin, dir, length)` — 영웅 원점에 부채꼴 mesh 배치, dir 로 LookRotation, length 균일 스케일 |
| 5 | A11 `BuildHeroSkillFx` | Nova=Cylinder / Orbit=Cube / Dash=Cube | Nova=Sphere(지름 `_radius`×2=7) / Orbit=Sphere(지름 `_bladeSphereRadius`×2=1.8) / **Dash=절차 부채꼴 mesh**(`BuildHeroDashFanFx` + `LoadOrCreateFanMesh`/`FillFanMesh` — 단위 반경·고정 반각 35°·XZ 평면·양면, `_Fan.mesh` asset 영속·GUID 보존) |
| 6 | E1 `hero_skills.json` DTO | (리플렉션 자동) | 코드 변경 불필요 — `HeroSkillDataConverter` 가 `[SerializeField]` 리플렉션 기반이라 `coneHalfAngle`/`bladeSphereRadius` 자동 포함, `halfWidth`/`bandHalfThickness` 자동 제외. **기존 `hero_skills.json` 은 Export 재실행으로 재생성** |
| 7 | A6/B1 EditMode 테스트 | `InLine` 케이스·`LineCalls`·링밴드 단언 | `InCone`(전방/각도초과/반경초과/뒤쪽)·`InSphere`(안/밖/경계) / `ConeCalls`·`SpheresCalls`(centers=bladeCount·radius=0.9·interval 0.3) / PlayMode line 동시사망 회귀 → cone+sphere 동시사망 회귀로 대체(enumeration-safety 유지) + 흰색 스탬프 회귀 cone/sphere 확장 |

**보존 불변 (형태 변경에도 유지)**:
- ① `HeroSkillContext` 스냅샷 버퍼(`_buffer` — 열거-중-수정 예외 방지) — cone·sphere 경로 모두 `ApplyAll` 공유로 적용.
- ② 흰색 `StampDamageColor(Color.white)` — `Apply` 공유로 cone·sphere 경로 모두 적용.

**자체 분기 결정 (기획서 §9 위임 항목)**: 부채꼴 mesh 각 정합 = **(a) 채택** — mesh 를 반각 35° 고정 빌드 + SO 기본 `_coneHalfAngle` 35. mesh 는 단위 반경(1)으로 빌드, 런타임 `SpawnCone` 이 length 로 균일 스케일. SO 반각이 35 와 다르면 비주얼만 근사(히트는 SO 값 기준).
