# 몬스터 도감 — 프리팹 렌더 아이콘 + 강화 레벨/발광 반영 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development 또는 executing-plans. 단, 이 레포는 start-develop 파이프라인(game-designer → gameplay-programmer …)으로도 실행된다. 그 경우 **수치·스타일(밝기/스케일 곡선·배지)은 game-designer 기획서가 SoT** — 본 plan의 `⟨기획서 확정⟩` 자리를 채운다.

**Goal:** 도감 몬스터 아이콘을 실제 프리팹 렌더로 교체하고, 강화 레벨 + 그 레벨의 발광색·밝기·스케일을 도감 셀에 반영한다.

**Architecture:** (Part 1) 6종 프리팹을 RenderTexture 카메라로 찍어 `MonsterIcons/*.png` 를 덮어쓰는 일회용 에디터 베이커(스켈레톤 영웅 아이콘 캡처 미러링, 생성 후 삭제). (Part 2) `CodexCellData`에 강화 레벨·종족을 실어 `CodexCell`이 레벨 배지 + `SpeciesVisual.SpeciesGlowColor` 기반 발광/밝기/스케일로 아이콘을 그린다. 종족당 아이콘 1장 유지(레벨별 스프라이트 없음).

**Tech Stack:** Unity 6 / C# / ChvjPackage(CHText/CHPoolingScrollView) / RenderTexture 캡처 / NUnit.

## Global Constraints

- **커밋 (Rule 01)**: 자동 커밋 금지. 각 Task 체크포인트는 테스트 통과 + `git add`까지. 최종 커밋 메시지(안)는 마무리에서.
- **스타일 (Rule 02)**: `//#` 주석, 가드절, `var`/`!` 금지, MVVM(셀 위젯 private + 의도 API).
- **인프라 (Rule 03)**: CHText, 아이콘은 인스펙터 switch resolver(Enum 키 로드 아님), CHPoolingScrollView 패턴.
- **에셋 (Rule 04)**: 아이콘 베이커는 authoring 일회용 → 생성 후 삭제. 구워진 PNG+meta 가 SoT.
- **범위 (§8)**: 신규 몬스터 제작 아님(기존 프리팹의 UI 표현). 레벨별 스프라이트 제작 금지 — 종족당 1장.
- **발광색 SoT**: `Lair.Data.SpeciesVisual.SpeciesGlowColor(EMonster)` — 전투·상점·도감 공유.
- **레벨 소스**: `MetaProfile.GetShopLevel("Enhance_"+EMonster)`. Lv0 = 발광 off.
- **UI 목업 게이트 (Rule 00)**: Part 2의 CodexCell 프리팹 UI 배선(레벨 배지·발광 요소) 전, 메인이 목업 승인 게이트를 태운다.

---

## 파일 구조 (생성/수정 맵)

**수정:**
- `Assets/_Lair/Scripts/UI/Village/CodexPopup.cs` — `CodexCellData`에 `EnhanceLevel`·`Species` 추가, `BuildMonsterCellData`가 레벨 채움
- `Assets/_Lair/Scripts/UI/Village/CodexCell.cs` — `Bind`에서 레벨 배지 + 아이콘 발광/밝기/스케일
- `Assets/_Lair/Art/Sprites/MonsterIcons/{Wisp,Wraith,Reaper,Hex,Plague,Phantom}.png` — 프리팹 렌더로 교체(Part 1)

**생성(일회용, 삭제 예정):**
- `Assets/_Lair/Editor/MonsterCodexIconBaker.cs` — 프리팹 → 아이콘 PNG 베이커

**생성(테스트):**
- `Assets/_Lair/Tests/EditMode/CodexMonsterEnhanceLevelTests.cs`

**에셋/프리팹 (인스펙터/MCP):**
- `CodexCell` 프리팹 — 레벨 배지(CHText) + 발광 오버레이 요소 추가·배선

---

### Task 1: 아이콘 베이커 — 6종 프리팹 → 도감 아이콘 PNG

**Files:**
- Create: `Assets/_Lair/Editor/MonsterCodexIconBaker.cs` (일회용)
- Overwrite: `Assets/_Lair/Art/Sprites/MonsterIcons/{6종}.png`

**Interfaces:**
- 메뉴 `Lair/Build/Monster Codex Icon Baker` → 6종 프리팹 렌더 → PNG 저장.
- Produces: 6개 갱신 PNG(동일 경로 → GUID 보존 → CodexPopup 인스펙터 참조·임포트 설정 무손실).

**설계 메모:** 스켈레톤 영웅 아이콘 캡처 방식 미러링 — git 히스토리의 삭제된 hero 아이콘 베이커를 참조해 카메라 각도·조명·프레이밍·해상도·투명배경 규약을 맞춘다. 6종 일관 프레이밍.

- [ ] **Step 1: 베이커 작성** — 각 프리팹을 임시 씬/프리뷰 스테이지에 인스턴스화, 전용 카메라 + RenderTexture 로 렌더, `ReadPixels`→`EncodeToPNG`→기존 경로 덮어쓰기, `AssetDatabase.ImportAsset` 로 Sprite 임포트 유지. 프레이밍/조명 상수는 hero 베이커 규약. ⟨정확한 카메라/조명 값은 hero 참조 + game-designer 육안 확정⟩
- [ ] **Step 2: (메인) MCP 실행** — 메뉴 실행 → 콘솔에 6종 저장 로그 확인.
- [ ] **Step 3: (메인) 육안 검증** — 6 PNG가 실제 몬스터 원본으로 보이는지, 영웅 아이콘과 톤 일치, 투명배경·프레이밍 OK.
- [ ] **Step 4: (메인) 베이커 삭제** (Rule 04 §3) — 커밋엔 PNG+meta만.
- [ ] **Step 5: 체크포인트** — `git add` 6 PNG(+.meta 변경분).

---

### Task 2: CodexCellData + BuildMonsterCellData — 강화 레벨 주입

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/CodexPopup.cs` (`CodexCellData`, `BuildMonsterCellData`)
- Test: `Assets/_Lair/Tests/EditMode/CodexMonsterEnhanceLevelTests.cs`

**Interfaces:**
- Consumes: `MetaProfile.GetShopLevel(string)`, `EMonster`.
- Produces:
  - `CodexCellData.EnhanceLevel` (`int`, 0~5), `CodexCellData.Species` (`EMonster?` — 몬스터 셀만, 카드/더미는 null).
  - `BuildMonsterCellData`가 `profile.GetShopLevel("Enhance_"+type)`로 `EnhanceLevel` 채움, `Species=type`. 카드/더미 빌더는 두 필드 미설정(기본값).

- [ ] **Step 1: 실패 테스트 작성** — `CodexMonsterEnhanceLevelTests.cs`:

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class CodexMonsterEnhanceLevelTests
    {
        private static MetaConfig Cfg()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.CodexLockedSlots = 0;
            return cfg;
        }

        [Test]
        public void 몬스터셀은_강화레벨을_ShopLevels에서_읽는다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Enhance_Wisp", 3);
            p.SeenMonsters.Add("Wisp");

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);
            CodexCellData wisp = list.Find(c => c.Species == EMonster.Wisp);

            Assert.IsNotNull(wisp);
            Assert.AreEqual(3, wisp.EnhanceLevel);
        }

        [Test]
        public void 미강화_종족은_레벨0이다()
        {
            MetaProfile p = new MetaProfile();
            p.SeenMonsters.Add("Wraith");
            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);
            CodexCellData wraith = list.Find(c => c.Species == EMonster.Wraith);
            Assert.AreEqual(0, wraith.EnhanceLevel);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — `EnhanceLevel`/`Species` 미정의 컴파일 실패.
- [ ] **Step 3: 구현** — `CodexCellData`에 `public int EnhanceLevel;`·`public EMonster? Species;` 추가. `BuildMonsterCellData` 루프에서:

```csharp
list.Add(new CodexCellData
{
    DisplayName = seen ? SpeciesVisual.SpeciesName(type) : "???",
    Unlocked = seen,
    Icon = iconResolver != null ? iconResolver(type) : null,
    TintColor = SpawnerStatusCell.SpeciesColor(type),
    Species = type,
    EnhanceLevel = profile.GetShopLevel("Enhance_" + type),
});
```

- [ ] **Step 4: 통과 확인** — 2 테스트 PASS + 기존 도감 테스트 회귀 없음.
- [ ] **Step 5: 체크포인트** — `git add` CodexPopup.cs + 테스트(+.meta).

---

### Task 3: CodexCell — 레벨 배지 + 아이콘 발광/밝기/스케일

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/CodexCell.cs`

**Interfaces:**
- Consumes: `CodexCellData.EnhanceLevel`·`Species`, `SpeciesVisual.SpeciesGlowColor(EMonster)`.
- Produces: `Bind`이 레벨 배지 텍스트 + 아이콘 색/밝기/스케일 반영. 신규 위젯은 `[SerializeField] private`(§6.1).

- [ ] **Step 1: 위젯 필드 추가** — `[SerializeField] private CHText _levelBadge;`·`[SerializeField] private RectTransform _iconRect;`(스케일 대상). (발광은 `_icon.color` 로 반영 → 별도 Image 불필요, 단 기획서가 오버레이 원하면 `_glowOverlay` 추가.)
- [ ] **Step 2: `Bind`에 레벨 표현 추가** — Unlocked 인 몬스터 셀(`data.Species != null`)일 때:

```csharp
//# 강화 레벨 표현 — 발광색(공유 SoT) × 밝기(레벨), 스케일, 배지. Lv0 은 순수 아이콘.
if (data.Species.HasValue && data.Unlocked)
{
    int lv = data.EnhanceLevel;
    if (_levelBadge != null)
    {
        _levelBadge.gameObject.SetActive(lv > 0);
        if (lv > 0)
        {
            _levelBadge.SetText($"Lv {lv}");
        }
    }
    if (_icon != null && showIcon)
    {
        Color glow = SpeciesVisual.SpeciesGlowColor(data.Species.Value);
        _icon.color = lv > 0 ? Color.Lerp(Color.white, glow, LevelTint(lv)) : Color.white;
    }
    if (_iconRect != null)
    {
        _iconRect.localScale = Vector3.one * LevelScale(lv);
    }
}
else
{
    if (_levelBadge != null) _levelBadge.gameObject.SetActive(false);
    if (_iconRect != null) _iconRect.localScale = Vector3.one;
}
```

- [ ] **Step 3: 레벨→밝기/스케일 매핑 헬퍼** — `LevelTint(int)`·`LevelScale(int)` 상수/곡선. **값은 ⟨기획서 확정⟩**(예: Tint = lv/5, Scale = 1 + 0.04·lv). 기획서 곡선으로 교체.
- [ ] **Step 4: 풀 재사용 리셋 확인** — `Bind`이 매 재사용마다 배지 활성·아이콘 색·스케일을 **전부 재설정**(글로벌/카드/더미 셀은 배지 off·색 white·스케일 1). OnEnable 잔상 없음.
- [ ] **Step 5: 컴파일 + 스모크** — 컴파일 클린. (레벨별 시각은 프리팹 배선 후 육안.)
- [ ] **Step 6: 체크포인트** — `git add` CodexCell.cs.

---

### Task 4: CodexCell 프리팹 UI 배선 (⛔ 목업 승인 게이트 선행)

**Files (에셋/프리팹):**
- Modify: `CodexCell` 프리팹 — 레벨 배지(CHText+TMP) 자식 + `_levelBadge` 배선, `_icon`의 RectTransform 을 `_iconRect` 배선(또는 기획서가 오버레이 요구 시 `_glowOverlay` Image 추가).

- [ ] **Step 0 (메인): 목업 승인 게이트** — 도감 셀 레벨 배지·발광 표현 목업을 사용자에게 제시하고 승인받는다(Rule 00 UI 목업 게이트). 승인 후에만 아래 배선.
- [ ] **Step 1: 프리팹 배선** — 레벨 배지 위치/스타일 = ⟨기획서/목업 확정⟩. 코드로 찍어야 하면 일회용 빌더(Rule 04 §3), 실행은 메인 MCP.
- [ ] **Step 2: (메인) 에디터 Play 육안** — 강화한 종족이 도감에서 레벨 배지 + 발광색·스케일로 표시, Lv0 순수, 미해금 실루엣 우선.
- [ ] **Step 3: 체크포인트** — `git add` CodexCell 프리팹(+신규 .meta), 빌더 있으면 삭제.

---

## Self-Review

**1. Spec coverage:** spec §4 아이콘 베이커 → Task 1 ✅ / §5 도감 레벨·발광 → Task 2(데이터)·3(표현)·4(프리팹) ✅ / §3.3 발광 SoT 공유 → Task 3 `SpeciesGlowColor` ✅ / §3.2 종족당 1장 → 레벨은 셀 표현(Task 3), 스프라이트 6장만(Task 1) ✅ / §5 미해금 상호작용 → Task 3 `data.Unlocked` 가드 ✅.

**2. Placeholder scan:** 수치(밝기/스케일 곡선·배지 스타일·카메라 프레이밍)만 `⟨기획서 확정⟩` 위임. 구조/시그니처/테스트는 구체.

**3. Type consistency:** `CodexCellData.EnhanceLevel:int`·`Species:EMonster?`, `BuildMonsterCellData(profile,cfg,iconResolver)`(기존 시그니처 보존), `SpeciesVisual.SpeciesGlowColor(EMonster)`, `CodexCell._levelBadge/_iconRect`, `"Enhance_"+type` 키 — Task 간 일치.

**주의(구현자):** `BuildMonsterCellData` 시그니처는 기존 3-arg 유지(테스트·호출부 무회귀). 카드/더미 빌더는 `Species=null`·`EnhanceLevel=0` 기본값 그대로.
