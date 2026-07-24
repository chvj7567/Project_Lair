# 몬스터 종족별 강화 (상점 「몬스터 강화」 탭) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Project Lair 파이프라인 주의:** 이 레포는 start-develop 파이프라인(game-designer → gameplay-programmer → …)으로도 실행될 수 있다. 그 경우 **도메인 수치(배수 곡선·가격·발광 세기 5단계)는 game-designer 기획서(`docs/design/`)가 SoT**이며, 본 plan은 파일 구조·시그니처·TDD 골격·검증 게이트를 정의한다. 수치가 필요한 자리는 `⟨기획서 확정⟩`으로 표기한다.

**Goal:** 상점에 종족별(6종) 개별 강화 「몬스터 강화」 탭을 추가하고, 강화 단계가 전투 몬스터와 메뉴에 발광 세기로 드러나게 한다.

**Architecture:** 기존 소울 상점 파이프(`ShopItemDef → ShopLevels → ShopService → MetaBattleBonus → _typeModifiers → ApplyMonsterStats`)에 효과종류 `MonsterSpecies` 하나를 얹는다. 종족 강화 HP/공격력 배수는 `MetaBattleBonus`가 종족별로 집계하고, `BattleController.ApplyMetaBonuses`가 기존 `_typeModifiers[종족]`의 Hp/Power 축에 곱연산으로 접는다 — 신규 전투 적용 코드 없음. 시각은 보스 `HeroStageVariantApplier`의 발광/아웃라인 로직을 이식한 `MonsterEnhancementVisual`을 몬스터 프리팹에 부착, 스폰 시 종족 현재 레벨로 적용한다.

**Tech Stack:** Unity 6 / C# / ChvjPackage(CHMPool·CHPoolingScrollView·CHText·CHButton) / Unity Test Framework(NUnit) / MVVM.

## Global Constraints

- **커밋 정책 (Rule 01)**: 자동 `git commit` 금지. 각 Task의 "체크포인트"는 **테스트 통과 확인 + `git add` 스테이징까지만**. 최종 커밋 메시지(안)는 파이프라인 마무리에서 한글로 제시. 아래 Task 스텝의 "체크포인트"는 이 규약을 따른다.
- **코드 스타일 (Rule 02)**: `//#` 주석, 가드절 중괄호 생략, `var` 금지·명시 타입, `!` 금지(`== false`/`== null`), `GetComponent`는 런타임 반복 경로 금지(스폰 1회 경로는 기존 관례 허용).
- **인프라 (Rule 03)**: 신규 GameObject 는 `CHMPool`, UI 는 CHText/CHButton, `CHPoolingScrollView` 는 BuildModal 정적 prefab 패턴. Enum 키 = 파일명 일치.
- **에셋 (Rule 04)**: 프리팹 authoring 툴은 일회용(생성 후 삭제). Addressable 은 `Assets/_Lair/Art/` 하위.
- **범위 (§8)**: 신규 몬스터/카드/영웅 리소스 제작 금지 — 기존 6종 + 발광 채널 재사용만.
- **테스트 네이밍**: 한글 메서드명 (`test_method_naming: korean`).
- **최종 스탯 규약**: `기본 × 스탯강화(전종 글로벌) × 종족강화`, 3축 독립 곱연산.
- **강화 레벨**: 종족당 Lv0~Lv5. Lv0 = 미강화(배수 1.0, 발광 off).

---

## 파일 구조 (생성/수정 맵)

**수정:**
- `Assets/_Lair/Scripts/Data/CommonEnum.cs` — `EShopEffectKind.MonsterSpecies` 추가
- `Assets/_Lair/Scripts/Data/MetaConfig.cs` — `ShopItemDef.Species` 필드 추가
- `Assets/_Lair/Scripts/Meta/MetaBattleBonus.cs` — 종족별 HP/Power 배수 집계
- `Assets/_Lair/Scripts/Battle/BattleController.cs` — `ApplyMetaBonuses`에 종족 배수 접기 + 스폰 시 시각 적용
- `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs` — 탭 상태 + `EffectKind` 필터
- `Assets/_Lair/Scripts/UI/Village/ShopItemCell.cs` — 발광 미리보기/종족 아이콘(확장)

**생성:**
- `Assets/_Lair/Scripts/Character/MonsterEnhancementVisual.cs` — 레벨→발광/아웃라인 적용 컴포넌트
- `Assets/_Lair/Tests/EditMode/MonsterSpeciesEnhancementBonusTests.cs`
- `Assets/_Lair/Tests/EditMode/ShopPopupTabFilterTests.cs`
- `Assets/_Lair/Tests/PlayMode/Character/MonsterEnhancementVisualPlayTests.cs`

**에셋/프리팹 (gameplay-programmer + 인스펙터 작업):**
- `Assets/_Lair/Data/MetaConfig.asset` — 종족 강화 항목 6개 등록 (값 ⟨기획서 확정⟩)
- 6종 몬스터 프리팹 — `MonsterEnhancementVisual` 부착 + 렌더러 와이어링
- `ShopPopup` 프리팹 — 탭 버튼 2개 + 탭별 컨텐츠 토글

---

### Task 1: 데이터 모델 — `MonsterSpecies` 효과종류 + 종족 배수 집계

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (enum `EShopEffectKind`)
- Modify: `Assets/_Lair/Scripts/Data/MetaConfig.cs` (`ShopItemDef`)
- Modify: `Assets/_Lair/Scripts/Meta/MetaBattleBonus.cs`
- Test: `Assets/_Lair/Tests/EditMode/MonsterSpeciesEnhancementBonusTests.cs`

**Interfaces:**
- Consumes: `EMonster`(CommonEnum), `MetaProfile.GetShopLevel(string)`, `ShopService.PriceOf`.
- Produces:
  - `EShopEffectKind.MonsterSpecies`
  - `ShopItemDef.Species` (`EMonster`) — `EffectKind == MonsterSpecies`일 때만 유효
  - `MetaBattleBonus.GetSpeciesMul(EMonster species)` → `float` (미등록/Lv0 이면 1.0). HP·공격력 공용 단일 배수(기본안).

- [ ] **Step 1: 실패 테스트 작성** — `MonsterSpeciesEnhancementBonusTests.cs`

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 종족별 강화(EShopEffectKind.MonsterSpecies) → MetaBattleBonus 집계 검증.
    public class MonsterSpeciesEnhancementBonusTests
    {
        private static MetaConfig MakeConfig(params ShopItemDef[] items)
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>(items);
            return cfg;
        }

        private static ShopItemDef SpeciesItem(string id, EMonster species, float perLevelMul, int maxLevel)
            => new ShopItemDef
            {
                Id = id, EffectKind = EShopEffectKind.MonsterSpecies,
                Species = species, PerLevelMul = perLevelMul, MaxLevel = maxLevel,
            };

        [Test]
        public void 종족강화_레벨2면_PerLevelMul_제곱으로_집계된다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 5));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 2);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(1.2f * 1.2f, bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        [Test]
        public void 강화안한_종족은_1배수다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 5));
            MetaBattleBonus bonus = MetaBattleBonus.From(new MetaProfile(), cfg);

            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wraith), 1e-4f);
        }

        [Test]
        public void 저장레벨이_만렙초과여도_MaxLevel로_클램프된다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 3));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 99);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(Mathf.Pow(1.2f, 3), bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — EditMode 컴파일 실패(`MonsterSpecies`/`Species`/`GetSpeciesMul` 미정의) 예상.

- [ ] **Step 3: enum 추가** — `CommonEnum.cs` `EShopEffectKind`:

```csharp
public enum EShopEffectKind
{
    MonsterStat,     //# 몬스터 전종 글로벌 스탯 배율 (StatKind 지정)
    SpawnerPeriod,   //# 모든 스포너 주기 배율
    MonsterSpecies,  //# 종족(Species) 개별 HP·공격력 배율
}
```

- [ ] **Step 4: `ShopItemDef.Species` 추가** — `MetaConfig.cs`:

```csharp
public EMonster Species;   //# EffectKind == MonsterSpecies 일 때만 사용 — 강화 대상 종족
```

- [ ] **Step 5: `MetaBattleBonus` 집계 구현** — 종족 맵 + 조회 + `From` 분기:

```csharp
private readonly Dictionary<EMonster, float> _speciesMuls = new Dictionary<EMonster, float>();

public float GetSpeciesMul(EMonster species)
    => _speciesMuls.TryGetValue(species, out float mul) ? mul : 1f;
```

`From`의 `switch (item.EffectKind)`에 케이스 추가:

```csharp
case EShopEffectKind.MonsterSpecies:
    bonus._speciesMuls[item.Species] = bonus.GetSpeciesMul(item.Species) * mul;
    break;
```

- [ ] **Step 6: 테스트 통과 확인** — 3개 테스트 PASS.

- [ ] **Step 7: 체크포인트** — `git add` (CommonEnum.cs, MetaConfig.cs, MetaBattleBonus.cs, 신규 테스트 + .meta). 커밋은 마무리에서.

---

### Task 2: 전투 적용 — `_typeModifiers`에 종족 배수 접기

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (`ApplyMetaBonuses`, `:650~687`)
- Test: `Assets/_Lair/Tests/EditMode/MonsterSpeciesEnhancementBonusTests.cs` (곱연산 독립성 케이스 추가)

**Interfaces:**
- Consumes: `MetaBattleBonus.GetSpeciesMul`, `_typeModifiers[EMonster].Multiply(EMonsterStatKind, float)`, `StatMultiplier.Get`.
- Produces: 전투 시작 시 종족강화 배수가 `_typeModifiers[종족]`의 Hp·Power 축에 곱연산 반영 → 기존 `ApplyMonsterStats` 경로가 자동 적용. (신규 public API 없음.)

- [ ] **Step 1: 실패 테스트 작성** — 3축 곱연산 독립성을 `StatMultiplier` 수준에서 검증 (BattleController 는 MonoBehaviour 라 직접 단위테스트 대신 집계 규약을 검증):

```csharp
[Test]
public void 종족강화와_글로벌스탯강화는_같은_HpMul에_곱연산_누적된다()
{
    //# ApplyMetaBonuses 규약 재현 — 글로벌 Hp 1.1 × 종족 Wisp 1.44 가 곱으로 쌓인다.
    StatMultiplier mul = new StatMultiplier();
    mul.Multiply(EMonsterStatKind.Hp, 1.1f);    //# 글로벌 스탯강화분
    mul.Multiply(EMonsterStatKind.Hp, 1.44f);   //# 종족강화분 (GetSpeciesMul 결과)

    Assert.AreEqual(1.1f * 1.44f, mul.Get(EMonsterStatKind.Hp), 1e-4f);
}
```

- [ ] **Step 2: 테스트 실패/통과 확인** — 이 테스트는 기존 `StatMultiplier`만으로 통과할 수 있다(규약 고정용 회귀 테스트). 실패하면 `StatMultiplier` 회귀. PASS 확인.

- [ ] **Step 3: `ApplyMetaBonuses`에 종족 접기 추가** — 기존 전종 글로벌 루프 뒤, 스포너 주기 앞에 삽입:

```csharp
//# 종족별 강화 — 전종 글로벌과 같은 _typeModifiers 표면의 Hp·Power 축에 곱연산 접기(spec §4.2).
foreach (EMonster type in (EMonster[])System.Enum.GetValues(typeof(EMonster)))
{
    float speciesMul = bonus.GetSpeciesMul(type);
    if (Mathf.Approximately(speciesMul, 1f))
        continue;

    if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
    {
        m = new StatMultiplier();
        _typeModifiers[type] = m;
    }
    m.Multiply(EMonsterStatKind.Hp, speciesMul);
    m.Multiply(EMonsterStatKind.Power, speciesMul);
}
```

- [ ] **Step 4: 컴파일 + EditMode 전체 통과 확인** — 회귀 없음 확인.

- [ ] **Step 5: 체크포인트** — `git add` BattleController.cs + 테스트.

---

### Task 3: 시각 컴포넌트 — `MonsterEnhancementVisual`

**Files:**
- Create: `Assets/_Lair/Scripts/Character/MonsterEnhancementVisual.cs`
- Test: `Assets/_Lair/Tests/PlayMode/Character/MonsterEnhancementVisualPlayTests.cs`

**Interfaces:**
- Consumes: `Renderer`/`SkinnedMeshRenderer` (인스펙터 `[SerializeField]`), 셰이더 프로퍼티 `_EmissionColor`/`_OutlineColor`/`_OutlineWidth`, 키워드 `_EMISSION` (보스 `HeroStageVariantApplier` 관례 재사용).
- Produces:
  - `MonsterEnhancementVisual.ApplyLevel(int level)` — Lv0 = 발광/아웃라인 off, Lv1~5 = 세기 escalate.
  - 레벨→세기 매핑은 인스펙터 배열 `[SerializeField] float[] _emissionByLevel`(index0=Lv1) + `Color _enhanceGlowColor`. 실제 값은 ⟨기획서 확정⟩.

**설계 메모:** 보스 `HeroStageVariantApplier`(`Assets/_Lair/Scripts/Character/HeroStageVariantApplier.cs`)의 `ApplyEmission`/`ApplyOutline`을 참고로 이식하되, **틴트(_BaseColor)는 건드리지 않는다**(종족 정체성 유지, spec §3.5). `HitFlash` 의존 없음.

- [ ] **Step 1: 실패 PlayMode 테스트 작성**

```csharp
using System.Collections;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode.Character
{
    //# 강화 레벨 → 발광 세기 적용 + 풀 재사용 리셋 검증.
    public class MonsterEnhancementVisualPlayTests
    {
        private static MonsterEnhancementVisual NewVisual(out Renderer rd)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); //# 테스트 전용 new (Rule 03 예외)
            rd = go.GetComponent<Renderer>();
            rd.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            MonsterEnhancementVisual v = go.AddComponent<MonsterEnhancementVisual>();
            v.SetRenderersForTest(new[] { rd });          //# 테스트 주입 API
            v.SetEmissionByLevelForTest(new[] { 1f, 2f, 3f, 4f, 5f }, Color.cyan);
            return v;
        }

        [UnityTest]
        public IEnumerator Lv0은_발광이_꺼진다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(0);
            yield return null;
            Assert.IsFalse(rd.material.IsKeywordEnabled("_EMISSION"));
        }

        [UnityTest]
        public IEnumerator Lv3은_해당_세기의_발광색이_적용된다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(3);
            yield return null;
            Assert.IsTrue(rd.material.IsKeywordEnabled("_EMISSION"));
            Color e = rd.material.GetColor("_EmissionColor");
            Assert.Greater(e.maxColorComponent, 0f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — `MonsterEnhancementVisual` 미정의.

- [ ] **Step 3: 컴포넌트 구현** — 필드/프로퍼티ID/`ApplyLevel` + 테스트 주입 API:

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 종족 강화 레벨의 발광/아웃라인 세기 표현 (spec §4.3). 틴트는 건드리지 않음(종족 색 유지).
    //# 보스 HeroStageVariantApplier 발광/아웃라인 로직 이식. 참조는 [SerializeField] 와이어링(Rule 02 §5).
    public class MonsterEnhancementVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _enhanceGlowColor = Color.white;
        [SerializeField] private float[] _emissionByLevel;   //# index0 = Lv1 … (길이 5 권장). 값 ⟨기획서 확정⟩

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";

        //# level: 0 = 미강화(발광 off), 1~N = _emissionByLevel[level-1] 세기.
        public void ApplyLevel(int level)
        {
            if (_renderers == null)
                return;
            bool on = level >= 1 && _emissionByLevel != null && level <= _emissionByLevel.Length;
            float intensity = on ? Mathf.Max(0f, _emissionByLevel[level - 1]) : 0f;
            Color emission = on ? _enhanceGlowColor * intensity : Color.black;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer rd = _renderers[i];
                if (rd == null)
                    continue;
                Material mat = rd.material;
                if (mat == null)
                    continue;
                if (on)
                {
                    mat.EnableKeyword(EmissionKeyword);
                }
                else
                {
                    mat.DisableKeyword(EmissionKeyword);
                }
                if (mat.HasProperty(EmissionColorId))
                {
                    mat.SetColor(EmissionColorId, emission);
                }
            }
        }

        //# OnEnable 풀 재사용 리셋 — 레벨은 스폰 경로가 ApplyLevel 로 재지정하므로 여기선 발광 off 로 초기화.
        private void OnEnable() => ApplyLevel(0);

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRenderersForTest(Renderer[] r) => _renderers = r;
        public void SetEmissionByLevelForTest(float[] byLevel, Color glow)
        {
            _emissionByLevel = byLevel;
            _enhanceGlowColor = glow;
        }
#endif
    }
}
```

> 아웃라인 병행 여부(§7 열린 결정)는 기획서가 확정. 기본안은 발광 단일 축 — 아웃라인이 필요하면 `HeroStageVariantApplier.ApplyOutline` 패턴을 동일 컴포넌트에 추가.

- [ ] **Step 4: 테스트 통과 확인** — PlayMode 2개 PASS.

- [ ] **Step 5: 체크포인트** — `git add` MonsterEnhancementVisual.cs(+.meta) + 테스트(+.meta).

---

### Task 4: 스폰 시 시각 적용 — BattleController 배선

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (스폰 경로 — `SpawnMonsters`/카드 소환/소급, `ApplyMonsterStats` 근처)

**Interfaces:**
- Consumes: `MonsterEnhancementVisual` (몬스터 GameObject 의 컴포넌트), `MetaSession.GetOrLoad()`의 `MetaProfile`, 종족별 강화 레벨 = `profile.GetShopLevel("Enhance_" + key)`.
- Produces: 신규 스폰 몬스터가 그 종족 현재 강화 레벨의 발광으로 등장.

**설계 메모:** 종족 강화 레벨 조회는 매 스폰 반복 경로다. `ApplyMonsterStats`가 이미 모든 스폰을 통과하므로 **여기에 시각 적용을 함께 태운다.** 레벨 조회 문자열 키 조립을 피하려 전투 시작 시 `Dictionary<EMonster,int> _speciesLevels`를 1회 캐시(ApplyMetaBonuses 시점)하고 스폰 때 조회.

- [ ] **Step 1: 레벨 캐시 필드 추가** — `_typeModifiers` 근처:

```csharp
//# 종족 강화 레벨 캐시 — ApplyMetaBonuses 에서 1회 채움, 스폰 시 발광 적용에 사용.
private readonly Dictionary<EMonster, int> _speciesLevels = new();
```

- [ ] **Step 2: `ApplyMetaBonuses`에서 레벨 캐시 채우기** — Task 2에서 추가한 종족 루프 안에서 레벨도 저장. `MetaConfig`의 `MonsterSpecies` 항목 Id 규약(`Enhance_<EMonster>`)으로 조회하지 말고, `From` 집계와 동일하게 config 항목을 순회해 `profile.GetShopLevel(item.Id)`를 `_speciesLevels[item.Species]`에 대입 (Id 규약 의존 제거). ⟨정확한 구현은 gameplay-programmer 가 config 순회 재사용⟩

- [ ] **Step 3: `ApplyMonsterStats` 끝에 시각 적용 추가**:

```csharp
//# 종족 강화 시각 — 현재 레벨의 발광 적용 (Lv0 이면 off). 컴포넌트 없으면 skip.
MonsterEnhancementVisual visual = character.GetComponent<MonsterEnhancementVisual>();
if (visual != null)
{
    int level = _speciesLevels.TryGetValue(key, out int lv) ? lv : 0;
    visual.ApplyLevel(level);
}
```

- [ ] **Step 4: PlayMode 스모크 확인** — 기존 배틀 PlayMode 스위트가 컴파일·통과하는지 확인(회귀 게이트). 신규 전용 PlayMode 는 Task 3가 커버.

- [ ] **Step 5: 체크포인트** — `git add` BattleController.cs.

---

### Task 5: 상점 2탭 — `ShopPopup` 필터 + 탭 상태

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs`
- Test: `Assets/_Lair/Tests/EditMode/ShopPopupTabFilterTests.cs`

**Interfaces:**
- Consumes: `MetaConfig.ShopItems`(각 `EffectKind`), 기존 `ShopPopup.BuildCellData`.
- Produces:
  - `enum ShopTab { Stat, Species }` (ShopPopup 내부 또는 CommonEnum — 단일 시스템이면 파일 내 정의)
  - `ShopPopup.BuildCellData(MetaProfile, MetaConfig, ShopTab tab)` — 오버로드. `tab==Stat`이면 `MonsterStat`/`SpawnerPeriod`만, `tab==Species`이면 `MonsterSpecies`만 반환.
  - 기존 무탭 `BuildCellData(profile, cfg)`는 호환 위해 `Stat + Species` 전부 반환하거나 제거 후 호출부 갱신 — gameplay-programmer 판단.

- [ ] **Step 1: 실패 테스트 작성** — `ShopPopupTabFilterTests.cs`:

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class ShopPopupTabFilterTests
    {
        private static MetaConfig Cfg()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, MaxLevel = 5 },
                new ShopItemDef { Id = "SpawnFaster", EffectKind = EShopEffectKind.SpawnerPeriod, MaxLevel = 5 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, MaxLevel = 5 },
            };
            return cfg;
        }

        [Test]
        public void 스탯탭은_글로벌항목만_보인다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg(), ShopPopup.ShopTab.Stat);
            CollectionAssert.AreEquivalent(new[] { "MonsterHpUp", "SpawnFaster" }, list.ConvertAll(c => c.Id));
        }

        [Test]
        public void 몬스터탭은_종족항목만_보인다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg(), ShopPopup.ShopTab.Species);
            CollectionAssert.AreEquivalent(new[] { "Enhance_Wisp" }, list.ConvertAll(c => c.Id));
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — `ShopTab`/오버로드 미정의.

- [ ] **Step 3: `ShopPopup`에 탭 enum + 필터 오버로드 구현**:

```csharp
public enum ShopTab { Stat, Species }

private static bool MatchesTab(EShopEffectKind kind, ShopTab tab)
    => tab == ShopTab.Species
        ? kind == EShopEffectKind.MonsterSpecies
        : kind == EShopEffectKind.MonsterStat || kind == EShopEffectKind.SpawnerPeriod;

public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg, ShopTab tab)
{
    List<ShopItemCellData> list = new List<ShopItemCellData>();
    if (profile == null || cfg == null)
        return list;
    foreach (ShopItemDef def in cfg.ShopItems)
    {
        if (def == null || string.IsNullOrEmpty(def.Id))
            continue;
        if (MatchesTab(def.EffectKind, tab) == false)
            continue;
        int level = profile.GetShopLevel(def.Id);
        bool isMax = level >= def.MaxLevel;
        int price = isMax ? 0 : ShopService.PriceOf(def, level);
        list.Add(new ShopItemCellData
        {
            Id = def.Id, DisplayName = def.DisplayName, Description = def.Description,
            LevelText = $"Lv {level}/{def.MaxLevel}", Price = price,
            IsMax = isMax, CanBuy = isMax == false && profile.Souls >= price,
        });
    }
    return list;
}
```

- [ ] **Step 4: 런타임 탭 상태 배선** — `ShopPopup`에 `private ShopTab _tab = ShopTab.Stat;` + 탭 버튼 2개(`[SerializeField] CHButton _statTabButton/_speciesTabButton`) → 클릭 시 `_tab` 변경 후 `Rebuild()`. `Rebuild`의 `BuildCellData(_arg.Profile, _arg.Config)` 호출을 `BuildCellData(_arg.Profile, _arg.Config, _tab)`로 교체. 선택 탭 시각 강조(색/언더라인)는 프리팹 Task 6에서.

- [ ] **Step 5: 테스트 통과 확인** — 2개 PASS + 기존 상점 EditMode 회귀 없음.

- [ ] **Step 6: 체크포인트** — `git add` ShopPopup.cs + 테스트.

---

### Task 6: 에셋/프리팹 배선 — MetaConfig 항목 + 몬스터 프리팹 + ShopPopup 탭 UI

**Files (에셋 — 코드 아님):**
- Modify: `Assets/_Lair/Data/MetaConfig.asset` — `MonsterSpecies` 항목 6개(6종) 추가. `Id=Enhance_<종>`, `DisplayName`/`Description`/`Species`/`PerLevelMul`/`MaxLevel=5`/`BasePrice`/`PriceGrowth` = ⟨기획서 확정⟩
- Modify: 6종 몬스터 프리팹(`Assets/_Lair/Art/Characters/*.prefab`) — `MonsterEnhancementVisual` 부착 + `_renderers`에 스켈레톤/스프라이트 렌더러 와이어링 + `_emissionByLevel`(5) / `_enhanceGlowColor` = ⟨기획서 확정⟩
- Modify: `ShopPopup` 프리팹 — 탭 버튼 2개(CHButton) 추가 + `_statTabButton`/`_speciesTabButton` 인스펙터 연결 + 선택 탭 강조 표현

**메모:** 순수 에셋/프리팹 작업 — 코드 변경 0. 값은 game-designer 기획서가 SoT. 프리팹 authoring 을 코드 툴로 찍었다면 생성 후 삭제(Rule 04 §3).

- [ ] **Step 1: MetaConfig.asset 에 종족 6항목 추가** — Lair Meta Editor 또는 인스펙터. `EffectKind=MonsterSpecies`, `Species` 지정.
- [ ] **Step 2: 6종 몬스터 프리팹에 `MonsterEnhancementVisual` 부착 + 렌더러/발광 배열 와이어링.**
- [ ] **Step 3: ShopPopup 프리팹에 탭 버튼 2개 배치 + `[SerializeField]` 연결.**
- [ ] **Step 4: 에디터 Play 로 수동 확인** — 몬스터 강화 탭 노출, 구매→소울 차감→다음 가격 상승, 전투 진입 시 강화 종족 발광 확인.
- [ ] **Step 5: 체크포인트** — `git add` 변경 에셋/프리팹(+신규 .meta). 순수 에셋 사이클이면 리뷰 생략 게이트 대상(Rule 00) — 단 본 Task 이전에 코드 Task 다수라 전체 사이클은 리뷰 대상.

---

### Task 7 (선택): 도감/요약 반영 — "현재 강화" 요약줄

**Files:**
- Modify: `Assets/_Lair/Scripts/Meta/DungeonPowerSummary.cs` (종족 강화도 요약에 포함할지)

**메모:** `ShopPopup._bonusSummaryText`("현재 강화 …")에 종족 강화를 노출할지는 ⟨기획서 확정⟩. 노출한다면 `DungeonPowerSummary.Build`에 `MonsterSpecies` 라인 추가 + 테스트. 스코프 최소화를 위해 **기본안은 미포함**(탭 자체가 종족 강화 현황을 보여줌). game-designer 가 요구 시에만 착수.

---

## Self-Review

**1. Spec coverage (spec §2~6 대응):**
- §3.1 종족별 개별 강화 → Task 1(데이터)·5(탭 필터)·6(항목 등록) ✅
- §3.2 HP·공격력 배수 → Task 1(`GetSpeciesMul` 단일 배수)·2(Hp·Power 축 접기) ✅
- §3.3 3축 독립 곱연산 → Task 2 회귀 테스트 ✅
- §3.4 5단계 → MaxLevel=5(Task 6), Lv0~5 규약(Global Constraints) ✅
- §3.5 발광 세기·틴트 X → Task 3 `MonsterEnhancementVisual` ✅
- §4.1 데이터/저장 재사용 → Task 1(enum·필드), 저장은 기존 `ShopLevels`(신규 필드 0) ✅
- §4.2 전투 적용 → Task 2(_typeModifiers 접기) ✅
- §4.3 시각 → Task 3·4 ✅
- §4.4 상점 2탭 → Task 5·6 ✅
- §5 경제/밸런스 수치 → ⟨기획서 확정⟩ 명시, Task 6 배선 ✅
- §6 테스트 → Task 1·2·3·5 테스트 파일 ✅

**2. Placeholder scan:** 도메인 수치만 `⟨기획서 확정⟩`으로 의도적 위임(project.md 문서 분담 규약). 구조/시그니처/테스트는 구체 코드 제시 — 모호 플레이스홀더 없음.

**3. Type consistency:** `GetSpeciesMul(EMonster)`·`_speciesMuls`·`ShopItemDef.Species`·`EShopEffectKind.MonsterSpecies`·`ShopTab{Stat,Species}`·`BuildCellData(…, ShopTab)`·`MonsterEnhancementVisual.ApplyLevel(int)` — Task 간 명칭·시그니처 일치 확인.

**주의(구현자):** Task 2 Step 2의 `_speciesLevels` 채우기는 Id 규약(`Enhance_*`) 문자열 파싱이 아니라 **config 항목 순회로 `item.Species → GetShopLevel(item.Id)`** 매핑할 것(규약 결합 제거).
