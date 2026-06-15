# 마을 메타 가시화 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **Lair 파이프라인 주의:** 본 plan 은 start-develop 파이프라인 안에서 game-designer 기획서(`docs/design/village-meta-visibility.md`, 작성 예정)와 결합된다. 라벨 문구·반올림 표기는 기획서가 단일 진실 — 본 plan 의 라벨 리터럴("HP"·"공격" 등)은 **잠정값**이며 구현 시 기획서 확정값으로 교체한다.
> **Rule 01:** 커밋 단계는 `git add` + 커밋 메시지(안) 제시까지만. `git commit` 직접 실행 금지.

**Goal:** 상점 팝업에 "현재 던전 강화" 누적 요약줄(스탯별 %)을, 퀘스트 팝업의 누적형 도전과제에 진행 바(N/M)를 붙여 메타 성장을 마을 안에서 보이게 한다.

**Architecture:** 신규 순수 C# `DungeonPowerSummary` 가 `MetaBattleBonus` 집계 배율을 라벨+% 라인으로 환산(EditMode 테스트). `ShopPopup.Rebuild` 가 이를 한 줄로 표기. `QuestPopup.BuildCellData` 가 `TotalWins/TotalRuns` 누적값을 진행 필드로 채우고 `QuestCell` 이 진행 바로 그린다. 신규 저장 필드·전투 로직 변경 0건 — 읽기/표시 전용.

**Tech Stack:** Unity 6 / ChvjPackage (CHText·CHButton·CHPoolingScrollView·UIBase) / NUnit (한글 테스트 메서드명) / 기존 Meta 코어 (`MetaBattleBonus`·`MetaProfile`·`MetaConfig`)

---

## 파일 구조 맵

```
Assets/_Lair/Scripts/Meta/
  DungeonPowerSummary.cs       ← 신설 — 상점 레벨 → 라벨+% 표시 라인 (MetaBattleBonus 재사용, EditMode 테스트)
Assets/_Lair/Scripts/UI/Village/
  ShopPopup.cs                 (수정) _bonusSummaryText 추가 + Rebuild 에서 세팅
  QuestPopup.cs                (수정) QuestCellData 진행 필드 + BuildCellData 누적형 산출
  QuestCell.cs                 (수정) 진행 바(Image fill)+텍스트 표시
Assets/_Lair/Editor/
  LairVillageBuilder.cs        (수정) ShopPopup 요약 CHText 노드 + QuestCell 진행 바 노드 빌드
Assets/_Lair/Art/UI/
  ShopPopup.prefab             (재생성) _bonusSummaryText 노드 포함
  QuestCell.prefab             (재생성) 진행 바 노드 포함
Assets/_Lair/Tests/EditMode/
  DungeonPowerSummaryTests.cs  ← 신설
  QuestProgressTests.cs        ← 신설 (QuestPopup.BuildCellData 진행 필드)
```

의존 방향: `UI/Village → Meta`. `DungeonPowerSummary` 는 `MetaBattleBonus`·`MetaConfig` 에만 의존, UI 비참조.

---

## Milestone 1 — DungeonPowerSummary (순수 C# 코어)

### Task 1: DungeonPowerSummary — 상점 레벨 → 라벨+% 라인

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/DungeonPowerSummary.cs`
- Test: `Assets/_Lair/Tests/EditMode/DungeonPowerSummaryTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class DungeonPowerSummaryTests
{
    private MetaConfig _cfg;

    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        //# 기획서 §3.2 PerLevelMul 과 동일한 잠정 수치
        _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp",       EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp,       PerLevelMul = 1.02f,  MaxLevel = 5 });
        _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterAtkSpeedUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Cooldown, PerLevelMul = 0.99f,  MaxLevel = 5 });
        _cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnerHasteUp",    EffectKind = EShopEffectKind.SpawnerPeriod,                                     PerLevelMul = 0.985f, MaxLevel = 5 });
    }

    [Test]
    public void 강화가_없으면_빈_목록이다()
    {
        List<DungeonPowerLine> lines = DungeonPowerSummary.Build(new MetaProfile(), _cfg);
        Assert.AreEqual(0, lines.Count);
    }

    [Test]
    public void 증가형_스탯은_양수_퍼센트로_환산된다()
    {
        MetaProfile p = new MetaProfile();
        p.SetShopLevel("MonsterHpUp", 5);                 //# 1.02^5 = 1.104 → +10%
        List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("HP", lines[0].Label);
        Assert.AreEqual(10, lines[0].Percent);
    }

    [Test]
    public void 감소형_스탯은_역수로_강화_퍼센트가_된다()
    {
        MetaProfile p = new MetaProfile();
        p.SetShopLevel("MonsterAtkSpeedUp", 5);           //# 0.99^5 = 0.951 → (1/0.951-1) = +5%
        p.SetShopLevel("SpawnerHasteUp", 5);              //# 0.985^5 = 0.927 → (1/0.927-1) = +8%
        List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("공속", lines[0].Label);
        Assert.AreEqual(5, lines[0].Percent);
        Assert.AreEqual("스폰률", lines[1].Label);
        Assert.AreEqual(8, lines[1].Percent);
    }

    [Test]
    public void 표시_순서는_ShopItems_순서를_따르고_레벨0은_제외된다()
    {
        MetaProfile p = new MetaProfile();
        p.SetShopLevel("SpawnerHasteUp", 3);              //# 목록상 3번째만 구매
        List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("스폰률", lines[0].Label);        //# Hp·Cooldown 은 레벨0 → 제외
    }
}
```

- [ ] **Step 2: 실행 — 컴파일/실패 확인**

Run: Unity `Lair/Tests/Run EditMode Tests` (또는 UnityMCP `run_tests`)
Expected: FAIL — `DungeonPowerSummary` / `DungeonPowerLine` 미정의

- [ ] **Step 3: 구현**

```csharp
using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 누적 효과 한 줄 — 라벨 + 강화 퍼센트 (양수 = 강해짐).
    public struct DungeonPowerLine
    {
        public string Label;
        public int Percent;
    }

    //# 상점 레벨 → "현재 던전 강화" 표시 라인 (spec §3.1). MetaBattleBonus 집계 배율 재사용 — 전투 적용과 단일 출처.
    //# 라벨은 동적 표시 문구 → 코드 리터럴 (마을+메타 기획서 §7 rev4 ②표 규칙). 문구 변경 시 기획서가 SoT.
    public static class DungeonPowerSummary
    {
        public static List<DungeonPowerLine> Build(MetaProfile profile, MetaConfig cfg)
        {
            List<DungeonPowerLine> lines = new List<DungeonPowerLine>();
            if (profile == null || cfg == null)
                return lines;

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);
            foreach (ShopItemDef item in cfg.ShopItems)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                if (profile.GetShopLevel(item.Id) <= 0)
                    continue;

                float mul;
                bool inverse;
                if (item.EffectKind == EShopEffectKind.SpawnerPeriod)
                {
                    mul = bonus.SpawnerPeriodMul;
                    inverse = true;                       //# 주기 단축 → 스폰률 상승
                }
                else
                {
                    mul = bonus.GetStatMul(item.StatKind);
                    inverse = item.StatKind == EMonsterStatKind.Cooldown
                           || item.StatKind == EMonsterStatKind.SlowFactor;
                }

                float ratio = inverse ? (1f / mul - 1f) : (mul - 1f);
                int percent = Mathf.RoundToInt(ratio * 100f);
                if (percent == 0)
                    continue;                             //# 반올림 0%는 노출 가치 없음

                lines.Add(new DungeonPowerLine { Label = LabelOf(item), Percent = percent });
            }
            return lines;
        }

        //# 잠정 라벨 — 기획서 확정값으로 교체 (마을+메타 기획서 §7 방향 일치).
        private static string LabelOf(ShopItemDef item)
        {
            if (item.EffectKind == EShopEffectKind.SpawnerPeriod)
                return "스폰률";
            switch (item.StatKind)
            {
                case EMonsterStatKind.Hp:        return "HP";
                case EMonsterStatKind.Power:     return "공격";
                case EMonsterStatKind.Cooldown:  return "공속";
                case EMonsterStatKind.Range:     return "사거리";
                case EMonsterStatKind.MoveSpeed: return "이동";
                case EMonsterStatKind.SlowFactor:return "둔화";
                default:                         return "?";
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Run: `Lair/Tests/Run EditMode Tests`
Expected: PASS (4 케이스)

- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 상점 누적 강화 효과를 스탯별 퍼센트 한 줄로 환산(DungeonPowerSummary)`

---

## Milestone 2 — ShopPopup 요약줄 표기

### Task 2: ShopPopup — _bonusSummaryText 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs` (`Rebuild` L84~)

- [ ] **Step 1: 필드 추가** — 클래스 상단 `[SerializeField]` 목록에:

```csharp
[SerializeField] private CHText _bonusSummaryText;   //# 상단 요약줄 — "현재 강화  HP +10% · 공속 +5%"
```

- [ ] **Step 2: Rebuild 에 요약 세팅 추가** — `_soulText` 세팅 직후:

```csharp
if (_bonusSummaryText != null)
{
    _bonusSummaryText.SetText(BuildSummaryText(_arg.Profile, _arg.Config));
}
```

- [ ] **Step 3: 요약 문자열 헬퍼 추가** — 클래스 내 private static (라벨 join 은 표시 책임이라 팝업이 담당):

```csharp
//# 표시 문자열 조립 — "현재 강화  HP +10% · 공속 +5% · 스폰률 +8%" / 강화 없으면 "현재 강화  아직 없음".
//# 접두("현재 강화")·구분자(" · ")·"아직 없음" 은 동적 문구 → 코드 리터럴 (기획서 §7 ②표). 문구는 기획서가 SoT.
private static string BuildSummaryText(MetaProfile profile, MetaConfig cfg)
{
    List<DungeonPowerLine> lines = DungeonPowerSummary.Build(profile, cfg);
    if (lines.Count == 0)
        return "현재 강화  아직 없음";

    System.Text.StringBuilder sb = new System.Text.StringBuilder("현재 강화  ");
    for (int i = 0; i < lines.Count; i++)
    {
        if (i > 0)
            sb.Append(" · ");
        sb.Append(lines[i].Label).Append(" +").Append(lines[i].Percent).Append('%');
    }
    return sb.ToString();
}
```

- [ ] **Step 4: 컴파일 확인** — `_bonusSummaryText` 가 null 이어도(프리팹 미연결 시) 가드로 무해. EditMode 회귀 통과.
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 상점 상단에 현재 던전 강화 누적 효과 한 줄 표시`

---

## Milestone 3 — 도전과제 진행도

### Task 3: QuestCellData + BuildCellData 진행 필드

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/QuestPopup.cs` (`QuestCellData` L18~, `BuildCellData` L84~)
- Test: `Assets/_Lair/Tests/EditMode/QuestProgressTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

public class QuestProgressTests
{
    private MetaConfig _cfg;

    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        _cfg.Achievements.Add(new AchievementDef { Id = "Wins25",   Condition = EAchievementCondition.TotalWins, Threshold = 25f, DisplayName = "영웅 학살자", RewardSouls = 100 });
        _cfg.Achievements.Add(new AchievementDef { Id = "Runs10",   Condition = EAchievementCondition.TotalRuns, Threshold = 10f, DisplayName = "성실한 영주", RewardSouls = 50 });
        _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", Condition = EAchievementCondition.FirstWin,  Threshold = 1f,  DisplayName = "첫 사냥감",   RewardSouls = 30 });
    }

    private QuestCellData 셀(MetaProfile p, string id)
        => DungeonTestHelper.Find(QuestPopup.BuildCellData(p, _cfg), _cfg, id);

    [Test]
    public void 누적형_미달성은_현재값과_목표를_노출한다()
    {
        MetaProfile p = new MetaProfile { TotalWins = 12 };
        QuestCellData cell = 셀(p, "Wins25");
        Assert.IsTrue(cell.HasProgress);
        Assert.AreEqual(12, cell.Current);
        Assert.AreEqual(25, cell.Target);
    }

    [Test]
    public void 현재값은_목표를_넘지_않도록_클램프된다()
    {
        MetaProfile p = new MetaProfile { TotalRuns = 30 };   //# 미달성 가정(플래그 미보유) — 표시 클램프만 검증
        QuestCellData cell = 셀(p, "Runs10");
        Assert.AreEqual(10, cell.Current);
        Assert.AreEqual(10, cell.Target);
    }

    [Test]
    public void 이미_달성한_누적형은_진행도를_끈다()
    {
        MetaProfile p = new MetaProfile { TotalWins = 30 };
        p.AchievedIds.Add("Wins25");
        Assert.IsFalse(셀(p, "Wins25").HasProgress);
    }

    [Test]
    public void 비누적형_조건은_진행도가_없다()
    {
        Assert.IsFalse(셀(new MetaProfile(), "FirstWin").HasProgress);
    }
}

//# 테스트 보조 — Id 로 셀 찾기 (BuildCellData 결과는 Achievements 순서).
public static class DungeonTestHelper
{
    public static QuestCellData Find(List<QuestCellData> cells, MetaConfig cfg, string id)
    {
        for (int i = 0; i < cfg.Achievements.Count; i++)
        {
            if (cfg.Achievements[i].Id == id)
                return cells[i];
        }
        return null;
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**

Run: `Lair/Tests/Run EditMode Tests`
Expected: FAIL — `QuestCellData.HasProgress`/`Current`/`Target` 미정의

- [ ] **Step 3: QuestCellData 확장** — 기존 필드 뒤에 추가:

```csharp
public bool HasProgress;    //# 누적형 미달성일 때만 true
public int Current;         //# 현재 누적값 (Target 으로 클램프)
public int Target;          //# 달성 임계
```

- [ ] **Step 4: BuildCellData 에 진행 산출 추가** — 각 `AchievementDef` 루프에서 `QuestCellData` 생성 시:

```csharp
bool achieved = profile.AchievedIds.Contains(def.Id);
bool cumulative = def.Condition == EAchievementCondition.TotalWins
               || def.Condition == EAchievementCondition.TotalRuns;
int target = cumulative ? Mathf.Max(0, (int)def.Threshold) : 0;
int raw = def.Condition == EAchievementCondition.TotalWins ? profile.TotalWins
        : def.Condition == EAchievementCondition.TotalRuns ? profile.TotalRuns : 0;
int current = cumulative ? Mathf.Min(raw, target) : 0;

list.Add(new QuestCellData
{
    DisplayName = def.DisplayName,
    Description = def.Description,
    RewardText = $"+{def.RewardSouls} 소울",
    Achieved = achieved,
    HasProgress = cumulative && achieved == false,
    Current = current,
    Target = target,
});
```

(상단 `using UnityEngine;` 는 이미 존재 — `Mathf` 사용 가능)

- [ ] **Step 5: 테스트 통과 확인**

Run: `Lair/Tests/Run EditMode Tests`
Expected: PASS (4 케이스)

- [ ] **Step 6: 스테이징 + 커밋 메시지(안)** — `# [feat] - 누적형 도전과제(승수·출격수)에 현재 진행도 데이터 산출`

### Task 4: QuestCell — 진행 바 표시

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/QuestCell.cs` (`Bind` L21~)

- [ ] **Step 1: 필드 추가** — `[SerializeField]` 목록에:

```csharp
[SerializeField] private GameObject _progressRoot;   //# 진행 바 컨테이너 — 표시/숨김 토글
[SerializeField] private Image _progressFill;         //# fillAmount = Current/Target
[SerializeField] private CHText _progressText;        //# "12/25"
```

- [ ] **Step 2: Bind 에 진행 바 갱신 추가** — `_achievedBadge` 처리 뒤:

```csharp
if (_progressRoot != null)
{
    _progressRoot.SetActive(data.HasProgress);
}
if (data.HasProgress)
{
    if (_progressFill != null)
    {
        _progressFill.fillAmount = data.Target > 0 ? (float)data.Current / data.Target : 0f;
    }
    if (_progressText != null)
    {
        _progressText.SetText($"{data.Current}/{data.Target}");
    }
}
```

- [ ] **Step 3: 컴파일 확인** — 필드 null(프리팹 미연결) 가드로 무해.
- [ ] **Step 4: 스테이징 + 커밋 메시지(안)** — `# [feat] - 도전과제 셀에 누적 진행 바(N/M) 표시`

---

## Milestone 4 — 프리팹 / 빌더 반영 (영속화)

### Task 5: 프리팹 직접 편집 — 신규 노드 추가

> **delta #3 — 빌더 제거됨 (2026-06-12 구현 시 확인):** 본 plan 작성 당시 가정한 `LairVillageBuilder`(`Lair/Setup/V2` 메뉴)는 커밋 ecd6cb1/3559005 에서 **이미 제거**됐다(이유: git 관리 프리팹·씬·MetaConfig 실수 덮어쓰기 방지). 따라서:
> - 빌더를 **재생성하지 않는다** — ecd6cb1 의 결정을 정면으로 되돌리는 행위. (백로그 "V2b 아이콘 주입 합류" 항목도 이 제거 이후 stale.)
> - 프리팹(`ShopPopup.prefab`·`QuestCell.prefab`)이 이제 **git 관리 SoT** — 노드 추가는 **프리팹 직접 편집**(에디터 인스펙터 작업)이 영속 경로. "다음 V2 에서 소실" 리스크는 V2 자체가 없으므로 void.
> - 아래 Step 1~3 의 "빌더 코드 수정 / V2 재실행" 은 **"에디터에서 프리팹 직접 편집"** 으로 대체해 읽는다.
>
> **delta #1 정정 (기획서 §2.3) — 아래 Step 1 의 "한 줄 + Overflow Ellipsis" 는 무효:** 요약줄 TMP 는 `enableWordWrapping = true` + `overflowMode = TextOverflowModes.Overflow`(**ellipsis 금지, 잘림 없음**) 로 설정해 1~2줄 가변 노출. Ellipsis 로 두면 trailing 토큰(스폰률 등)이 잘려 §1 "성장 체감" 의도가 깨진다.

**Files:**
- Modify: `Assets/_Lair/Editor/LairVillageBuilder.cs` (ShopPopup·QuestCell 빌드부)
- Re-generate: `Assets/_Lair/Art/UI/ShopPopup.prefab`, `Assets/_Lair/Art/UI/QuestCell.prefab`

- [ ] **Step 1: ShopPopup 빌드부 수정** — 기존 `_soulText` 생성/연결 코드를 찾아, 동일 패턴으로 요약 CHText 노드를 상단(소울 잔액 아래)에 추가:
  - TMP_Text + **CHText 동행** (Rule 03 §3 — 정적/동적 라벨 예외 없음)
  - 생성한 컴포넌트를 `ShopPopup._bonusSummaryText` 직렬화 필드에 연결 (`SerializedObject`/`FindProperty` 기존 와이어링 패턴 그대로)
  - 폭은 팝업 콘텐츠 폭. **TMP: enableWordWrapping=true, overflowMode=Overflow (ellipsis 금지, 1~2줄 가변·잘림 없음 — 기획서 §2.3, delta #1)**
- [ ] **Step 2: QuestCell 빌드부 수정** — 기존 `_rewardText`/`_achievedBadge` 생성 코드를 찾아, 동일 패턴으로 진행 바 추가:
  - `_progressRoot`(빈 RectTransform 컨테이너) 아래 배경 Image + `_progressFill`(Image, type Filled / Horizontal) + `_progressText`(TMP_Text + CHText 동행)
  - 세 컴포넌트를 `QuestCell._progressRoot`/`_progressFill`/`_progressText` 에 연결
  - 달성 뱃지와 같은 영역에 배치하되, 런타임 토글이 상호 배타이므로 레이아웃 충돌 무관
- [ ] **Step 3: V2 메뉴 실행** — `Lair/Setup/V2 - Build Village UI Prefabs` 재실행 → ShopPopup·QuestCell 프리팹 재생성. 다른 팝업 프리팹은 회귀 없음 확인(diff 로 두 프리팹만 의미 변경).
- [ ] **Step 4: Addressables 확인** — 주소=파일명 유지(`ShopPopup`·`QuestCell`), 신규 에셋 없음 → 엔트리 변동 없음. 변동 시 `Lair/Build Addressables` 재빌드.
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [asset] - 상점 강화 요약줄·도전과제 진행 바 노드를 마을 UI 빌더에 반영`

---

## Milestone 5 — 통합 검증

### Task 6: 수동 + 회귀 검증

- [ ] **Step 1: EditMode 전체 통과** — `Lair/Tests/Run EditMode Tests` (신규 2 파일 + 기존 회귀)
- [ ] **Step 2: 수동 — 상점 요약줄** — 에디터 Play: Loading→Village→상점. 강화 0건 시 "현재 강화  아직 없음", 한 항목 구매 후 즉시 "현재 강화  HP +N%" 갱신 확인(`Rebuild` 호출 경로).
- [ ] **Step 3: 수동 — 도전과제 진행 바** — 퀘스트 팝업: 누적형(승수·출격수) 셀에 진행 바 + "N/M", 비누적형/달성 셀은 진행 바 없음. (누적값은 세이브 파일을 직접 편집하거나 런 반복으로 확보)
- [ ] **Step 4: 표시 전용 보증** — Battle 전투 시작 시 `MetaBattleBonus` 적용 경로 불변(본 작업이 읽기만 함) — PlayMode/EditMode 회귀로 확인.
- [ ] **Step 5: 최종 스테이징 + 커밋 메시지(안)** — 마일스톤별 분할 커밋 권장 (Rule 01).

---

## Verification Gates

| Gate | 시점 | 기준 |
|---|---|---|
| G1 | M1 완료 | `DungeonPowerSummaryTests` green — 증가/감소형 환산·순서·0건 |
| G2 | M3 완료 | `QuestProgressTests` green + 기존 EditMode 회귀 |
| G3 | M4 완료 | V2 재생성 후 ShopPopup 요약줄·QuestCell 진행 바가 Play 에서 보임, 다른 프리팹 회귀 없음 |
| G4 | 전체 | 라벨 문구·반올림 표기가 game-designer 기획서 확정값과 일치 (plan↔기획서 sync) |

## Self-Review

- **스펙 커버리지**: spec §3.1(강화 요약)→Task 1·2·5 / §3.2(진행도)→Task 3·4·5 / §3.3(빌더 영속)→Task 5 / §5(비범위: 저장필드 0·JSON id 0)→전 Task 준수(신규 필드/문자열 id 없음) / §6(테스트)→Task 1·3·6 / §7(부호 방향·빌더 SoT·표시전용)→Task 1(inverse 분기)·Task 5(주의 박스)·Task 6 Step 4. 누락 0건.
- **Placeholder 스캔**: 라벨 리터럴("HP" 등)은 "기획서 확정값 교체" 명시 — Lair plan↔기획서 분담 규약상 의도된 잠정값(추상 placeholder 아님). TODO/TBD/빈 코드블록 0건.
- **타입 일관성**: `DungeonPowerLine{Label,Percent}` · `DungeonPowerSummary.Build` · `QuestCellData{HasProgress,Current,Target}` · `QuestPopup.BuildCellData` · `ShopPopup._bonusSummaryText` · `QuestCell._progressRoot/_progressFill/_progressText` — Task 간 시그니처 글자 단위 일치.

## 리스크 메모

- **부호 방향**(spec §7): 감소형(Cooldown·SlowFactor·SpawnerPeriod)은 `(1/mul-1)` — Task 1 의 `inverse` 분기가 단일 지점. 라벨/방향은 기획서 §7 확정값과 G4 에서 대조.
- **빌더 SoT**(spec §3.3): Task 5 미수행 시 프리팹 손-편집이 다음 V2 에서 소실 — Task 4(코드)와 Task 5(프리팹)는 한 사이클에서 함께 끝낸다.
- **MetaBattleBonus 재사용**: `DungeonPowerSummary` 가 전투 적용과 동일 집계를 읽음 — 표시/전투 불일치 원천 차단. 전투 로직은 불변.
