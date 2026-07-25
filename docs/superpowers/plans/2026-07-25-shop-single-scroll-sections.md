# 상점 단일 스크롤뷰 + 섹션 헤더 (탭 제거) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: subagent-driven-development 또는 executing-plans. 이 레포는 start-develop 파이프라인으로도 실행된다 — 그 경우 헤더 스타일·높이·요약줄 등 **수치/디자인은 game-designer 기획서 SoT**(`⟨기획서 확정⟩`), 프레임워크 제약(가변 높이)은 gameplay-programmer가 최종 확정.

**Goal:** 상점의 「스탯 강화」/「몬스터 강화」 탭을 없애고, 단일 스크롤뷰에 섹션 헤더로 구분된 통합 목록으로 재구성한다.

**Architecture:** `CHPoolingScrollView` 셀 타입 1개 제약에 맞춰, 헤더 행과 항목 행을 `ShopItemCellData.RowKind`(SectionHeader/Item)로 구분하는 단일 셀로 통합한다. `BuildCellData`가 [스탯 헤더 + 스탯 항목 + 몬스터 헤더 + 종족 항목] 통합 리스트를 반환하고, `ShopItemCell.Bind`이 RowKind로 헤더/항목 표시를 분기한다. 탭 상태·필터·버튼은 전부 제거.

**Tech Stack:** Unity 6 / C# / ChvjPackage(CHText/CHButton/CHPoolingScrollView) / NUnit.

## Global Constraints
- 커밋(Rule 01): 자동 커밋 금지, 체크포인트는 `git add`까지.
- 스타일(Rule 02): //#·가드절·var/! 금지·위젯 private(§6.1).
- 인프라(Rule 03): CHText/CHButton, CHPoolingScrollView 3-class 패턴, 아이콘 인스펙터 resolver.
- 범위: 기존 상점 재구성(신규 기능 없음, v0.3 범위). 항목·가격·효과 무변경.
- **UI 목업 게이트(Rule 00)**: Task 4 프리팹 배선 전 메인이 목업 승인 게이트.
- 요약줄·헤더 문구·스타일: `⟨기획서 확정⟩`. 가변 높이 해소: gameplay-programmer 확정(§Task 4).

---

## 파일 구조

**수정:**
- `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs` — `ShopTab`/`_tab`/탭버튼·배경 필드/`SelectTab`/`UpdateTabHighlight`/`MatchesTab`/3-arg `BuildCellData` 제거. `ShopItemCellData`에 `RowKind`·`HeaderText`. `BuildCellData(profile,cfg)`를 섹션 헤더 포함 통합으로 재작성. `Rebuild`이 무탭 호출.
- `Assets/_Lair/Scripts/UI/Village/ShopItemCell.cs` — `Bind` RowKind 분기(헤더 제목 / 항목 UI) + `_headerText` 위젯 + 풀 재사용 리셋.
- `Assets/_Lair/Tests/EditMode/ShopPopupTabFilterTests.cs` (+ `ShopPopupTabFilterEdgeTests.cs`) — 탭 필터 검증 → 통합 리스트 구성(헤더 위치·순서) 검증으로 갱신.

**에셋/프리팹:**
- `ShopPopup` 프리팹 — 탭 버튼 2개 + 강조 배경 제거. `ShopItemCell` 프리팹 — 헤더 표현(`_headerText`) 배선.

---

### Task 1: 데이터 + BuildCellData 통합 (탭 제거)

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs`
- Test: `Assets/_Lair/Tests/EditMode/ShopPopupTabFilterTests.cs`

**Interfaces:**
- Produces:
  - `enum ShopRowKind { SectionHeader, Item }` (ShopPopup 내부).
  - `ShopItemCellData.RowKind`(`ShopRowKind`, 기본 Item)·`HeaderText`(string).
  - `BuildCellData(MetaProfile, MetaConfig)` → 통합 리스트: [스탯 헤더(HeaderText=⟨기획서: "스탯 강화"⟩), MonsterStat/SpawnerPeriod 항목들, 몬스터 헤더("몬스터 강화"), MonsterSpecies 항목들].
- Consumes: `MetaConfig.ShopItems`, `EShopEffectKind`, `MakeCell`(기존).
- **제거**: `ShopTab`·`MatchesTab`·3-arg `BuildCellData`.

- [ ] **Step 1: 실패 테스트 작성** — `ShopPopupTabFilterTests.cs` 재작성(탭 필터 → 통합 구성):

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class ShopSectionListTests
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
        public void 통합리스트는_스탯헤더_스탯항목_몬스터헤더_종족항목_순서다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg());
            //# [스탯헤더, MonsterHpUp, SpawnFaster, 몬스터헤더, Enhance_Wisp]
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(ShopPopup.ShopRowKind.SectionHeader, list[0].RowKind);
            Assert.AreEqual("MonsterHpUp", list[1].Id);
            Assert.AreEqual("SpawnFaster", list[2].Id);
            Assert.AreEqual(ShopPopup.ShopRowKind.SectionHeader, list[3].RowKind);
            Assert.AreEqual("Enhance_Wisp", list[4].Id);
        }

        [Test]
        public void 항목행은_Item_RowKind다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg());
            Assert.AreEqual(ShopPopup.ShopRowKind.Item, list[1].RowKind);
        }

        [Test]
        public void null_가드는_빈리스트다()
        {
            Assert.IsEmpty(ShopPopup.BuildCellData(null, Cfg()));
            Assert.IsEmpty(ShopPopup.BuildCellData(new MetaProfile(), null));
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — `ShopRowKind`·통합 시그니처 미정의 컴파일 실패.
- [ ] **Step 3: 구현** — `ShopRowKind` enum + `ShopItemCellData.RowKind`/`HeaderText`. `BuildCellData(profile,cfg)`를 통합 생성으로 교체:

```csharp
public enum ShopRowKind { SectionHeader, Item }

private static ShopItemCellData Header(string text)
    => new ShopItemCellData { RowKind = ShopRowKind.SectionHeader, HeaderText = text };

public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
{
    List<ShopItemCellData> list = new List<ShopItemCellData>();
    if (profile == null || cfg == null)
        return list;

    //# 스탯 강화 섹션 (전종 글로벌). ⟨헤더 문구 기획서⟩
    list.Add(Header("스탯 강화"));
    AddItems(list, cfg, profile, isSpecies: false);
    //# 몬스터 강화 섹션 (종족별).
    list.Add(Header("몬스터 강화"));
    AddItems(list, cfg, profile, isSpecies: true);
    return list;
}

private static void AddItems(List<ShopItemCellData> list, MetaConfig cfg, MetaProfile profile, bool isSpecies)
{
    foreach (ShopItemDef def in cfg.ShopItems)
    {
        if (def == null || string.IsNullOrEmpty(def.Id))
            continue;
        bool species = def.EffectKind == EShopEffectKind.MonsterSpecies;
        if (species != isSpecies)
            continue;
        list.Add(MakeCell(def, profile));   //# RowKind 기본 Item
    }
}
```
`ShopTab`·`MatchesTab`·3-arg `BuildCellData` 삭제.

- [ ] **Step 4: 통과 확인** — 3 테스트 PASS + 기존 상점 회귀(가격·레벨) 없음.
- [ ] **Step 5: 체크포인트** — `git add` ShopPopup.cs + 테스트.

> 참고: 섹션에 항목이 0개인 경우 헤더만 남는다 — 현 config는 양 섹션 모두 항목이 있어 무해. 빈 섹션 헤더 숨김이 필요하면 ⟨기획서/후속⟩.

---

### Task 2: ShopPopup 탭 배선 제거 + Rebuild 무탭화

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs`

**Interfaces:**
- Consumes: 통합 `BuildCellData(profile,cfg)`(Task 1).
- 제거: `_tab`·탭버튼/배경 `[SerializeField]`·`SelectTab`·`UpdateTabHighlight`·InitUI 탭 배선.

- [ ] **Step 1: 탭 필드/메서드 제거** — `_statTabButton`/`_speciesTabButton`/`_statTabBg`/`_speciesTabBg`/`TabActiveColor`/`TabInactiveColor`/`_tab`/`SelectTab`/`UpdateTabHighlight` 삭제. `InitUI`의 탭 OnClick·`UpdateTabHighlight()` 호출 삭제.
- [ ] **Step 2: `Rebuild` 무탭화** — `BuildCellData(_arg.Profile, _arg.Config, _tab)` → `BuildCellData(_arg.Profile, _arg.Config)`. 아이콘 주입 루프(`cell.Icon = SpeciesIcon(cell.Species)`)는 유지 — 단, 헤더 행은 Species=null이라 Icon=null(무해).
- [ ] **Step 3: 컴파일 확인** — orphan 참조 0(제거 필드 사용처 없음).
- [ ] **Step 4: 체크포인트** — `git add` ShopPopup.cs.

---

### Task 3: ShopItemCell — RowKind 분기 (헤더/항목)

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/ShopItemCell.cs`

**Interfaces:**
- Consumes: `ShopItemCellData.RowKind`·`HeaderText`.
- Produces: `Bind`이 헤더면 제목만·항목이면 기존 UI. 신규 `[SerializeField] private CHText _headerText`.

- [ ] **Step 1: `_headerText` 필드 추가** + `Bind` 진입에 RowKind 분기:

```csharp
public void Bind(ShopItemCellData data)
{
    if (data == null)
        return;
    _data = data;

    bool isHeader = data.RowKind == ShopItemCellData.ShopRowKind... //# (RowKind 위치에 맞게)
        == ShopPopup.ShopRowKind.SectionHeader;

    if (_headerText != null)
    {
        _headerText.gameObject.SetActive(isHeader);
        if (isHeader)
        {
            _headerText.SetText(data.HeaderText);
        }
    }
    //# 항목 위젯 전부 헤더면 off — 이름/레벨/설명/가격/구매버튼 컨테이너.
    SetItemWidgetsActive(isHeader == false);
    if (isHeader)
        return;

    //# 기존 항목 바인딩(이름/레벨/설명/가격/구매/아이콘/발광) 그대로 …
}
```
`SetItemWidgetsActive`가 이름·레벨·설명·가격·구매버튼(+종족 아이콘/발광)을 일괄 토글. 항목 셀 재사용 시 헤더 잔상 없음(매 Bind 재설정 — RecordsStageCell 교훈: OnEnable 리셋 대신 Bind 소유).

- [ ] **Step 2: 헤더/항목 전이 스모크** — 헤더 셀 재사용→항목, 항목→헤더 시 위젯 표시 정확(풀 재사용). 육안은 Task 4.
- [ ] **Step 3: 컴파일 + 체크포인트** — `git add` ShopItemCell.cs.

> **주의**: RowKind enum 위치(ShopPopup 내부 vs ShopItemCellData 옆) — Task 1에서 `ShopPopup.ShopRowKind`로 정의했으면 셀도 그 타입 참조. gameplay-programmer가 네임스페이스/접근 일관 정리.

---

### Task 4: 프리팹 배선 + 가변 높이 해소 (⛔ 목업 승인 게이트 선행)

**Files (에셋/프리팹):**
- Modify: `ShopPopup` 프리팹 — 탭 버튼 2개·강조 배경 제거.
- Modify: `ShopItemCell` 프리팹 — 헤더 텍스트(`_headerText`) 자식 + 배선.

- [ ] **Step 0 (gameplay-programmer): 가변 높이 확정** — `CHPoolingScrollView` 구현을 확인해 헤더/항목 높이 처리를 결정: **(A) 헤더를 항목과 동일 높이**(프레임워크가 고정 높이 가정 시 — 안전) 또는 **(B) 가변 높이 지원 시 헤더 짧게**. 결정·근거를 보고(spec §5). 이게 프리팹 배선 방식을 정한다.
- [ ] **Step 1 (메인): 목업 승인 게이트** — 섹션 헤더 상점 목업을 사용자에게 제시·승인(Rule 00). 승인 후 배선.
- [ ] **Step 2: 프리팹 배선** — 탭 버튼 제거 + ShopItemCell에 `_headerText` 추가. 헤더 스타일 = ⟨기획서/목업⟩. 코드로 찍어야 하면 일회용 빌더(Rule 04 §3), 실행은 메인 MCP.
- [ ] **Step 3: (메인) 에디터 Play 육안** — 탭 없이 단일 스크롤에 [스탯 헤더 + 항목 + 몬스터 헤더 + 종족] 순서, 헤더-항목 높이/정렬, 스크롤·구매 정상.
- [ ] **Step 4: 체크포인트** — `git add` 프리팹(+신규 .meta), 빌더 있으면 삭제.

---

## Self-Review

**1. Spec coverage:** spec §3.1 탭 제거·섹션 헤더 → Task 1·2·4 / §3.3 이종 셀 단일 타입 → Task 1(RowKind)·3(Bind 분기) / §3.4 BuildCellData 탭 제거 → Task 1 / §5 가변 높이 → Task 4 Step 0 / §2 요약줄 유지 → Task 2(Rebuild 요약 유지, 미제거) ✅.

**2. Placeholder scan:** 헤더 문구·스타일·요약줄 최종은 `⟨기획서⟩`, 프레임워크 높이는 gameplay-programmer 확정으로 명시적 위임. 구조/시그니처/테스트 구체.

**3. Type consistency:** `ShopRowKind{SectionHeader,Item}`·`ShopItemCellData.RowKind/HeaderText`·`BuildCellData(profile,cfg)` 2-arg 단일·`ShopItemCell._headerText` — Task 간 일치. (RowKind enum 최종 위치는 Task 1이 정의, Task 3가 참조 — gameplay-programmer가 접근 경로 통일.)

**주의(구현자):** 기존 `BuildCellData(profile,cfg)` 2-arg는 지금 "전 항목 무탭"인데, 이 plan은 그 2-arg를 **섹션 헤더 포함 통합**으로 의미를 바꾼다. 이 2-arg를 참조하던 기존 테스트/호출부(ShopPopupCellDataTests 등)가 있으면 새 의미(헤더 포함)에 맞춰 함께 갱신.
