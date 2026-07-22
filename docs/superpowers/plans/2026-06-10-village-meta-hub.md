# 마을(허브) + 메타 진행 v0.2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **Lair 파이프라인 주의:** 본 plan 은 start-develop 파이프라인 안에서 game-designer 기획서(`docs/design/village-meta-hub.md`)와 결합된다. 수치(가격·보상량·XP 곡선·도전과제 목록)는 기획서가 단일 진실 — 본 plan 의 수치는 **잠정 기본값**이며 구현 시 기획서 확정값으로 교체한다.
> **Rule 01:** 커밋 단계는 `git add` + 커밋 메시지(안) 제시까지만. `git commit` 직접 실행 금지.

**Goal:** 런 사이에 마을(허브) 씬을 끼워 넣고, 소울 경제 위에 상점(영구 업그레이드)·영주 레벨·영웅 선택·도감/기록·고정 도전과제를 올린다. 세이브는 로컬 JSON.

**Architecture:** 순수 C# 메타 코어(`Scripts/Meta/` — MetaProfile/보상/상점/레벨/도전과제, EditMode 테스트 가능) 위에 Village 씬(3D 해골 idle + CHMUI 팝업)을 올리고, BattleController 는 시작 시 메타 보너스 적용·종료 시 보상 정산만 추가한다. spec: `docs/superpowers/specs/2026-06-10-village-meta-hub-design.md`

**Tech Stack:** Unity 6 / ChvjPackage (CHMUI·CHMResource·CHMPool·CHText·CHButton·CHPoolingScrollView) / JsonUtility / NUnit (한글 테스트 메서드명)

---

## 파일 구조 맵

```
Assets/_Lair/Scripts/Meta/                  ← 신설 — 순수 C# 메타 코어 (Unity 씬 비종속)
  MetaProfile.cs            세이브 모델 (Serializable, 버전 필드)
  MetaProfileStore.cs       JSON 로드/세이브 (persistentDataPath, 경로 주입 가능)
  MetaSession.cs            씬 간 공유 static 홀더 (Profile, SelectedHero)
  SoulRewardCalculator.cs   런 결과 → (소울, XP) 순수 계산
  LordLevelService.cs       누적 XP → 레벨/진행률 계산
  ShopService.cs            구매 가능/가격/구매 로직
  AchievementService.cs     RunSummary → 신규 달성 판정 + 보상
  RunSummary.cs             한 판 요약 (도전과제 판정 입력)
  MetaBattleBonus.cs        상점 레벨 → 전투 배율 집계
Assets/_Lair/Scripts/Data/
  MetaConfig.cs             ScriptableObject — 상점 품목/보상 공식/XP 곡선/영주 보상/도전과제 정의
  CommonEnum.cs             EScene.Village, EUI 7종, EShopEffectKind, EAchievementCondition 추가 (모두 뒤에 append)
Assets/_Lair/Scripts/Village/               ← 신설
  VillageController.cs      씬 진입점 — 프로필 로드, 해골 idle 배치, VillageHud 표시
  VillageViewModel.cs       소울/레벨/XP 게이지 가공 + 변경 이벤트
Assets/_Lair/Scripts/UI/Village/            ← 신설 (팝업 + 셀)
  VillageHud.cs                                  상단바/좌우 메뉴/출격 (UIBase)
  ShopPopup.cs / ShopItemCell.cs / ShopItemPoolingScrollView.cs
  QuestPopup.cs / QuestCell.cs / QuestPoolingScrollView.cs
  CodexPopup.cs / CodexCell.cs / CodexPoolingScrollView.cs
  RecordsPopup.cs                                통계 텍스트 (스크롤뷰 불필요)
  HeroSelectPopup.cs / HeroSelectCell.cs / HeroSelectPoolingScrollView.cs
  LordLevelPopup.cs / LordRewardCell.cs / LordRewardPoolingScrollView.cs
Assets/_Lair/Scripts/Battle/
  BattleController.cs       (수정) 시작 시 메타 보너스 적용 + EndBattle 보상 정산/저장
Assets/_Lair/Scripts/UI/
  ResultPopup.cs            (수정) 보상 요약 표시 + 마을 복귀
Assets/_Lair/Editor/
  LairVillageBuilder.cs     Village 씬 + UI 프리팹 7종 생성 메뉴
Assets/_Lair/Scenes/Village.unity           ← 신설 (빌더 생성)
Assets/_Lair/Data/MetaConfig.asset          ← 신설 (빌더 생성)
Assets/_Lair/Art/Json/Strings_Ko.json       (수정) 마을 UI 문자열 키 추가
Assets/_Lair/Tests/EditMode/                Meta* 테스트 6파일 신설
```

의존 방향: `UI/Village → Village(VM) → Meta ← Battle`. Meta 는 UnityEngine 비참조(MetaConfig·Store 제외) — EditMode 테스트 대상.

---

## Milestone 0 — 단계 전환 (문서·메타 파일)

> **가장 먼저 수행.** project.md 가 MVP 인 채로 두면 design-reviewer/code-reviewer 가 본 기능을 "MVP 범위 밖"으로 BLOCKER 처리한다.

### Task 0.1: 단계 키 갱신

**Files:**
- Modify: `.claude/project.md` (stage / stage_goal / concept_sections)
- Modify: `CLAUDE.md` §2·§8
- Modify: `docs/design/project_lair_concept.md` §11 + 변경 이력

- [ ] **Step 1: `.claude/project.md` 갱신** — `stage: v0.2`, `stage_goal: 런 사이 메타 성장이 재방문 동기를 만드는가`
- [ ] **Step 2: `CLAUDE.md` 갱신** — §2 현재 단계 서술을 v0.2 로, §8 "메타 진행/메인 메뉴 금지" → "메타 진행 허용(로컬 세이브 한정), 마을이 시작 화면 겸임. 서버 연동은 여전히 금지". §9 의 "MVP 범위 밖 작업" 문구는 "현 단계(v0.2) 범위 밖 작업"으로
- [ ] **Step 3: 컨셉서 §11 표 갱신** — 메타 진행 ❌→✅(v0.2, 로컬), 메인 메뉴 ❌→"마을 허브가 겸임". §7 에 "v0.2 1차분: 상점/영주레벨/영웅선택/도감·기록/도전과제 — 신규 리소스는 잠금 더미" 명시. 변경 이력 v0.7 항목 추가
- [ ] **Step 4: 스테이징 + 커밋 메시지(안)** — `# [docs] - v0.2 단계 전환: 마을 허브 + 메타 진행 범위 승격`

---

## Milestone 1 — 메타 데이터 코어 (MetaProfile + Store)

### Task 1.1: MetaProfile 세이브 모델

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/MetaProfile.cs`
- Test: `Assets/_Lair/Tests/EditMode/MetaProfileTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class MetaProfileTests
{
    [Test]
    public void 새_프로필은_버전1_소울0으로_시작한다()
    {
        MetaProfile p = new MetaProfile();
        Assert.AreEqual(1, p.Version);
        Assert.AreEqual(0, p.Souls);
        Assert.AreEqual(0, p.LordXp);
        Assert.IsNotNull(p.ShopLevels);
        Assert.IsNotNull(p.AchievedIds);
    }

    [Test]
    public void JsonUtility_왕복_직렬화로_필드가_보존된다()
    {
        MetaProfile p = new MetaProfile { Souls = 120, LordXp = 350 };
        p.SetShopLevel("MonsterHp", 3);
        p.AchievedIds.Add("FirstWin");
        MetaProfile r = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));
        Assert.AreEqual(120, r.Souls);
        Assert.AreEqual(3, r.GetShopLevel("MonsterHp"));
        Assert.Contains("FirstWin", r.AchievedIds);
    }

    [Test]
    public void 없는_상점_항목_레벨은_0이다()
    {
        Assert.AreEqual(0, new MetaProfile().GetShopLevel("없는항목"));
    }
}
```

- [ ] **Step 2: 실행 — 컴파일 실패 확인** (Lair/Tests/Run EditMode Tests 또는 UnityMCP `run_tests`)
- [ ] **Step 3: 구현**

```csharp
using System;
using System.Collections.Generic;

namespace Lair.Meta
{
    //# 메타 진행 세이브 모델 — JsonUtility 직렬화 (Dictionary 불가 → 엔트리 리스트).
    //# 스키마 변경 시 Version 증가 + Store 마이그레이션 분기 (spec §5.7).
    [Serializable]
    public class MetaProfile
    {
        public int Version = 1;
        public int Souls;
        public int LordXp;                                  //# 누적 XP — 레벨은 LordLevelService 가 계산
        public List<ShopLevelEntry> ShopLevels = new List<ShopLevelEntry>();
        public List<string> AchievedIds = new List<string>();   //# 달성한 도전과제 Id
        public List<string> SeenMonsters = new List<string>();  //# 도감 — EMonster.ToString()
        public List<string> PickedCards = new List<string>();   //# 도감 — ECardId.ToString() (distinct)
        public int TotalRuns;
        public int TotalWins;
        public float BestClearTime = -1f;                   //# 승리 최단 시간(초). 없으면 -1
        public string SelectedHero = "Knight";              //# EHero.ToString()

        public int GetShopLevel(string itemId) { ... }      //# 리스트 탐색, 없으면 0
        public void SetShopLevel(string itemId, int level) { ... }  //# 있으면 갱신, 없으면 추가
        public void AddDistinct(List<string> list, string value) { ... }
    }

    [Serializable]
    public class ShopLevelEntry { public string ItemId; public int Level; }
}
```

(`...` 부분은 구현자가 채운다 — 동작은 테스트가 정의)

- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 메타 진행 세이브 모델(MetaProfile) 추가`

### Task 1.2: MetaProfileStore — 로컬 JSON 저장

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/MetaProfileStore.cs`
- Test: `Assets/_Lair/Tests/EditMode/MetaProfileStoreTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System.IO;
using Lair.Meta;
using NUnit.Framework;

public class MetaProfileStoreTests
{
    private string _dir;
    [SetUp]    public void 준비() { _dir = Path.Combine(Path.GetTempPath(), "lair_meta_test"); if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
    [TearDown] public void 정리() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Test]
    public void 파일이_없으면_새_프로필을_반환한다()
    {
        MetaProfile p = new MetaProfileStore(_dir).Load();
        Assert.IsNotNull(p);
        Assert.AreEqual(0, p.Souls);
    }

    [Test]
    public void 저장_후_로드하면_값이_복원된다()
    {
        MetaProfileStore store = new MetaProfileStore(_dir);
        MetaProfile p = store.Load();
        p.Souls = 777;
        store.Save(p);
        Assert.AreEqual(777, new MetaProfileStore(_dir).Load().Souls);
    }

    [Test]
    public void 깨진_JSON_파일이면_새_프로필로_폴백한다()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "meta_profile.json"), "{{{broken");
        Assert.IsNotNull(new MetaProfileStore(_dir).Load());
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — 핵심 계약:

```csharp
namespace Lair.Meta
{
    //# 로컬 JSON 세이브 — 기본 경로 Application.persistentDataPath (Android dataPath 읽기 전용 — 과거 사고 재발 방지).
    //# 테스트는 임시 폴더 주입. 모든 IO 는 try-catch — 세이브 실패가 게임 흐름을 끊지 않는다.
    public class MetaProfileStore
    {
        public const string FileName = "meta_profile.json";
        public MetaProfileStore(string directory = null) { ... }   //# null 이면 persistentDataPath
        public MetaProfile Load() { ... }    //# 파일 없음/파싱 실패 → new MetaProfile()
        public void Save(MetaProfile profile) { ... }   //# 디렉토리 보장 후 JsonUtility.ToJson(pretty:true)
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 메타 프로필 로컬 JSON 저장/로드(MetaProfileStore)`

### Task 1.3: MetaSession — 씬 간 공유 홀더

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/MetaSession.cs` (테스트 불요 — 단순 홀더)

- [ ] **Step 1: 구현**

```csharp
namespace Lair.Meta
{
    //# 씬 전환 간 프로필 공유 static 홀더. Village 진입 시 Load, Battle 은 null 이면 직접 Load (에디터 Battle 직행 안전).
    public static class MetaSession
    {
        public static MetaProfile Profile;
        public static MetaProfileStore Store;

        public static MetaProfile GetOrLoad()
        {
            Store ??= new MetaProfileStore();      //# (구현 시 == null 분기 — Rule 02 §4)
            Profile ??= Store.Load();
            return Profile;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 씬 간 메타 프로필 공유(MetaSession)`

---

## Milestone 2 — 보상·성장·도전과제 로직 + MetaConfig

### Task 2.1: MetaConfig SO + Enum 추가

**Files:**
- Create: `Assets/_Lair/Scripts/Data/MetaConfig.cs`
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` — **모든 enum 값은 기존 뒤에 append (int 직렬화 보존)**

- [ ] **Step 1: CommonEnum.cs 에 추가**

```csharp
//# EScene — Village 를 Battle 뒤에 append
public enum EScene { Loading, Battle, Village }

//# EUI — 기존 8개 뒤에 append
//# VillageHud, ShopPopup, QuestPopup, CodexPopup, RecordsPopup, HeroSelectPopup, LordLevelPopup

//# 마을 — 상점 효과 종류 (v0.2 는 2종으로 한정 — YAGNI)
public enum EShopEffectKind { MonsterStat, SpawnerPeriod }

//# 마을 — 도전과제 판정 조건 종류
public enum EAchievementCondition { FirstWin, WinUnderSeconds, TotalWins, TotalRuns, SynergyTierReached }
```

- [ ] **Step 2: MetaConfig.cs 작성**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 메타 진행 수치 단일 진실 — 기획서 docs/design/village-meta-hub.md 값으로 채움 (BalanceConfig 패턴).
    [CreateAssetMenu(menuName = "Lair/MetaConfig", fileName = "MetaConfig")]
    public class MetaConfig : ScriptableObject
    {
        [Header("소울 보상 (기획서 §소울 경제)")]
        public int WinBaseSouls = 100;            //# 잠정 — 기획서 확정값으로 교체
        public float WinTimeBonusPerSec = 0.5f;   //# 남은 1초당 보너스
        public int LoseMaxSouls = 50;             //# 패배 시 영웅 HP 깎은 비율 × 이 값

        [Header("영주 XP (기획서 §영주 레벨)")]
        public int WinXp = 100;
        public int LoseXp = 40;
        public int XpPerLevelBase = 100;          //# 1→2 필요 XP
        public float XpGrowth = 1.25f;            //# 레벨당 필요 XP 증가율

        public List<ShopItemDef> ShopItems = new List<ShopItemDef>();
        public List<LordRewardDef> LordRewards = new List<LordRewardDef>();
        public List<AchievementDef> Achievements = new List<AchievementDef>();
        public int HeroLockedSlots = 3;           //# 영웅 메뉴 잠금 더미 슬롯 수
        public int CodexLockedSlots = 4;          //# 도감 잠금 더미 슬롯 수

        public ShopItemDef FindShopItem(string id) { ... }
    }

    [Serializable]
    public class ShopItemDef
    {
        public string Id;                 //# 예: "MonsterHp" — MetaProfile.ShopLevels 키
        public string DisplayName;
        public EShopEffectKind EffectKind;
        public EMonsterStatKind StatKind; //# EffectKind == MonsterStat 일 때만 사용
        public float PerLevelMul = 1.03f; //# 레벨당 곱연산 배율 (SpawnerPeriod 는 1 미만이 단축)
        public int MaxLevel = 5;
        public int BasePrice = 50;
        public float PriceGrowth = 1.6f;  //# 레벨당 가격 배율 (floor)
    }

    [Serializable]
    public class LordRewardDef
    {
        public int Level;
        public bool IsLockedDummy;        //# true 면 "??? — 추후 해금" 잠금 슬롯
        public int RewardSouls;           //# IsLockedDummy == false 일 때 지급
        public string DisplayName;
    }

    [Serializable]
    public class AchievementDef
    {
        public string Id;                 //# 예: "FirstWin" — MetaProfile.AchievedIds 키
        public string DisplayName;
        public string Description;
        public EAchievementCondition Condition;
        public float Threshold;           //# WinUnderSeconds=초 / TotalWins·TotalRuns=횟수 / SynergyTierReached=티어
        public int RewardSouls = 30;
    }
}
```

- [ ] **Step 3: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 메타 수치 SO(MetaConfig)·마을 관련 Enum 추가`

### Task 2.2: SoulRewardCalculator

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/SoulRewardCalculator.cs`
- Test: `Assets/_Lair/Tests/EditMode/SoulRewardCalculatorTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class SoulRewardCalculatorTests
{
    private MetaConfig _cfg;
    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        _cfg.WinBaseSouls = 100; _cfg.WinTimeBonusPerSec = 0.5f; _cfg.LoseMaxSouls = 50;
        _cfg.WinXp = 100; _cfg.LoseXp = 40;
    }

    [Test]
    public void 승리는_기본보상에_남은시간_보너스를_더한다()
    {
        //# 300초 중 180초에 처치 → 남은 120초 × 0.5 = 60 보너스
        SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, deathTime: 180f, totalSeconds: 300f, heroDamagedRatio: 1f, _cfg);
        Assert.AreEqual(160, r.Souls);
        Assert.AreEqual(100, r.Xp);
    }

    [Test]
    public void 패배는_영웅HP_깎은_비율에_비례한다()
    {
        SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, heroDamagedRatio: 0.6f, _cfg);
        Assert.AreEqual(30, r.Souls);   //# 50 × 0.6
        Assert.AreEqual(40, r.Xp);
    }

    [Test]
    public void 패배_무피해면_소울_0이다()
    {
        Assert.AreEqual(0, SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, 0f, _cfg).Souls);
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현**

```csharp
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    public struct SoulReward { public int Souls; public int Xp; }

    //# 런 결과 → 소울/XP 순수 계산 (spec §5.1). 수치는 MetaConfig — 기획서가 단일 진실.
    public static class SoulRewardCalculator
    {
        public static SoulReward Calculate(BattleResult result, float deathTime, float totalSeconds, float heroDamagedRatio, MetaConfig cfg)
        {
            if (result == BattleResult.Win)
            {
                float remain = Mathf.Max(0f, totalSeconds - deathTime);
                return new SoulReward { Souls = cfg.WinBaseSouls + Mathf.FloorToInt(remain * cfg.WinTimeBonusPerSec), Xp = cfg.WinXp };
            }
            return new SoulReward { Souls = Mathf.FloorToInt(cfg.LoseMaxSouls * Mathf.Clamp01(heroDamagedRatio)), Xp = cfg.LoseXp };
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 런 결과에 따른 소울/XP 보상 계산 — 승리는 빨리 잡을수록, 패배도 깎은 만큼 부분 지급`

### Task 2.3: LordLevelService

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/LordLevelService.cs`
- Test: `Assets/_Lair/Tests/EditMode/LordLevelServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class LordLevelServiceTests
{
    private MetaConfig _cfg;
    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        _cfg.XpPerLevelBase = 100; _cfg.XpGrowth = 1.25f;
        //# 필요 XP: Lv1→2 = 100, Lv2→3 = 125, Lv3→4 = 156(floor)
    }

    [Test] public void XP_0은_레벨1이다()           => Assert.AreEqual(1, LordLevelService.LevelFromXp(0, _cfg));
    [Test] public void XP_100은_레벨2다()           => Assert.AreEqual(2, LordLevelService.LevelFromXp(100, _cfg));
    [Test] public void XP_224는_아직_레벨2다()      => Assert.AreEqual(2, LordLevelService.LevelFromXp(224, _cfg));
    [Test] public void XP_225는_레벨3이다()         => Assert.AreEqual(3, LordLevelService.LevelFromXp(225, _cfg));

    [Test]
    public void 진행률은_현재_레벨_구간_내_비율이다()
    {
        //# Lv2 구간(100~225) 의 중간 162.5 근처 → 0.5
        Assert.AreEqual(0.5f, LordLevelService.ProgressInLevel(162, _cfg), 0.01f);
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — `LevelFromXp(int xp, MetaConfig cfg)` : 누적 필요치를 `need = floor(XpPerLevelBase × XpGrowth^(lv-1))` 로 감산 루프 (상한 Lv99 가드). `ProgressInLevel(int xp, MetaConfig cfg)` : 현재 구간 소모/필요 비율 0~1.
- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 영주 레벨 XP 곡선 계산(LordLevelService)`

### Task 2.4: ShopService

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/ShopService.cs`
- Test: `Assets/_Lair/Tests/EditMode/ShopServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class ShopServiceTests
{
    private MetaConfig _cfg;
    private MetaProfile _profile;
    private ShopService _shop;

    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHp", BasePrice = 50, PriceGrowth = 1.6f, MaxLevel = 5 });
        _profile = new MetaProfile { Souls = 100 };
        _shop = new ShopService(_profile, _cfg);
    }

    [Test] public void 가격은_레벨에_따라_점증한다()
    {
        Assert.AreEqual(50, _shop.PriceOf("MonsterHp"));      //# Lv0→1
        _profile.SetShopLevel("MonsterHp", 1);
        Assert.AreEqual(80, _shop.PriceOf("MonsterHp"));      //# 50×1.6 floor
    }

    [Test] public void 구매하면_소울이_차감되고_레벨이_오른다()
    {
        Assert.IsTrue(_shop.Buy("MonsterHp"));
        Assert.AreEqual(50, _profile.Souls);
        Assert.AreEqual(1, _profile.GetShopLevel("MonsterHp"));
    }

    [Test] public void 소울_부족이면_구매_실패한다()
    {
        _profile.Souls = 10;
        Assert.IsFalse(_shop.Buy("MonsterHp"));
        Assert.AreEqual(10, _profile.Souls);
    }

    [Test] public void 만렙이면_구매_불가다()
    {
        _profile.Souls = 99999;
        _profile.SetShopLevel("MonsterHp", 5);
        Assert.IsFalse(_shop.CanBuy("MonsterHp"));
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — `ShopService(MetaProfile, MetaConfig)` / `PriceOf(id)` = `floor(BasePrice × PriceGrowth^currentLevel)` / `CanBuy(id)` (존재·만렙·소울 검사) / `Buy(id)` (CanBuy 통과 시 차감+레벨업, bool 반환). 저장은 호출자(VillageController) 책임.
- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 소울 상점 구매 로직 — 레벨제 영구 업그레이드, 가격 점증`

### Task 2.5: RunSummary + AchievementService

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/RunSummary.cs`, `Assets/_Lair/Scripts/Meta/AchievementService.cs`
- Test: `Assets/_Lair/Tests/EditMode/AchievementServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AchievementServiceTests
{
    private MetaConfig _cfg;
    private MetaProfile _profile;

    [SetUp] public void 준비()
    {
        _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", Condition = EAchievementCondition.FirstWin, RewardSouls = 30 });
        _cfg.Achievements.Add(new AchievementDef { Id = "Win180", Condition = EAchievementCondition.WinUnderSeconds, Threshold = 180f, RewardSouls = 50 });
        _profile = new MetaProfile();
    }

    private RunSummary 승리런(float deathTime) => new RunSummary { Result = BattleResult.Win, DeathTime = deathTime, HeroDamagedRatio = 1f, MaxSynergyTier = 0 };

    [Test]
    public void 첫_승리에_FirstWin이_달성되고_보상이_지급된다()
    {
        _profile.TotalWins = 1;   //# 정산 후 호출 가정 — 이번 런 반영 완료 상태
        List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f), _cfg);
        Assert.AreEqual(1, got.Count);
        Assert.AreEqual("FirstWin", got[0].Id);
        Assert.AreEqual(30, _profile.Souls);
        Assert.Contains("FirstWin", _profile.AchievedIds);
    }

    [Test]
    public void 이미_달성한_과제는_다시_달성되지_않는다()
    {
        _profile.TotalWins = 2;
        _profile.AchievedIds.Add("FirstWin");
        List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f), _cfg);
        Assert.AreEqual(0, got.Count);
        Assert.AreEqual(0, _profile.Souls);
    }

    [Test]
    public void 시간_조건은_threshold_미만_승리만_인정한다()
    {
        _profile.TotalWins = 1;
        List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(170f), _cfg);
        CollectionAssert.Contains(got.ConvertAll(a => a.Id), "Win180");
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현**

```csharp
namespace Lair.Meta
{
    //# 한 판 요약 — EndBattle 시점 수집, 도전과제 판정 입력 (jsonl RunRecord 와 별개 — 빌드에서도 동작).
    public class RunSummary
    {
        public Lair.Data.BattleResult Result;
        public float DeathTime;
        public float HeroDamagedRatio;     //# 0~1 — 영웅 최대HP 대비 깎은 비율
        public int MaxSynergyTier;         //# 4축 중 최고 달성 티어 (0~3)
        public System.Collections.Generic.List<string> Picks = new();
    }

    //# 고정 도전과제 판정 (spec §5.6). 프로필은 이번 런 정산(TotalRuns/Wins 증가) "후" 상태로 전달.
    public static class AchievementService
    {
        //# 신규 달성 목록 반환 + 프로필에 보상 소울/달성 플래그 즉시 반영.
        public static List<AchievementDef> Evaluate(MetaProfile profile, RunSummary run, MetaConfig cfg) { ... }
        //# 조건 분기: FirstWin=TotalWins>=1 / WinUnderSeconds=승리&&DeathTime<Threshold
        //#           TotalWins·TotalRuns=프로필 누적>=Threshold / SynergyTierReached=run.MaxSynergyTier>=Threshold
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 고정 도전과제 판정·보상 지급(AchievementService)`

### Task 2.6: MetaBattleBonus — 상점 레벨 → 전투 배율

**Files:**
- Create: `Assets/_Lair/Scripts/Meta/MetaBattleBonus.cs`
- Test: `Assets/_Lair/Tests/EditMode/MetaBattleBonusTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

public class MetaBattleBonusTests
{
    [Test]
    public void 상점_레벨만큼_거듭제곱_배율이_집계된다()
    {
        MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
        cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f });
        cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnFaster", EffectKind = EShopEffectKind.SpawnerPeriod, PerLevelMul = 0.97f });
        MetaProfile p = new MetaProfile();
        p.SetShopLevel("MonsterHp", 2);
        p.SetShopLevel("SpawnFaster", 1);

        MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg);
        Assert.AreEqual(1.21f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);   //# 1.1^2
        Assert.AreEqual(1f,    bonus.GetStatMul(EMonsterStatKind.Power), 0.001f);
        Assert.AreEqual(0.97f, bonus.SpawnerPeriodMul, 0.001f);
    }

    [Test]
    public void 레벨_0이면_전부_항등이다()
    {
        MetaBattleBonus bonus = MetaBattleBonus.From(new MetaProfile(), ScriptableObject.CreateInstance<MetaConfig>());
        Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);
        Assert.AreEqual(1f, bonus.SpawnerPeriodMul, 0.001f);
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — `MetaBattleBonus.From(profile, cfg)` : ShopItems 순회, `mul = PerLevelMul^level` 을 StatKind 별로 곱연산 누적. `GetStatMul(EMonsterStatKind)` + `SpawnerPeriodMul` 노출.
- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 상점 영구 업그레이드의 전투 적용 배율 집계(MetaBattleBonus)`

---

## Milestone 3 — Battle 통합 (보상 정산 + 메타 보너스)

### Task 3.1: BattleController — 메타 보너스 적용 + EndBattle 정산

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (Start 초기화부 + `EndBattle` L671~)
- Modify: `Assets/_Lair/Scripts/UI/ResultPopup.cs`

- [ ] **Step 1: BattleController 시작부 — 메타 보너스 적용**
  - `[SerializeField] private MetaConfig _metaConfig;` 추가 (Battle 씬 인스펙터 할당)
  - Start 에서 `MetaProfile profile = MetaSession.GetOrLoad();` → `MetaBattleBonus bonus = MetaBattleBonus.From(profile, _metaConfig);`
  - 전 `EMonster` 종의 `_typeModifiers` 에 `bonus.GetStatMul(kind)` 를 6개 `EMonsterStatKind` 별 `Multiply` (1f 면 no-op)
  - `BindSpawners` 후 전 스포너에 `sp.ScalePeriod(bonus.SpawnerPeriodMul)` (1f 면 skip)
  - `_metaConfig == null` 이면 전부 skip (기존 테스트/씬 호환)
- [ ] **Step 2: EndBattle 정산 추가** — `_recorder.FinishRun(...)` 직후:

```csharp
//# v0.2 메타 — 보상 정산 → 프로필 갱신 → 저장. RunRecorder(에디터 한정)와 별개로 빌드에서도 동작.
int soulsGained = 0, xpGained = 0;
List<string> newlyAchieved = new List<string>();
if (_metaConfig != null)
{
    MetaProfile profile = MetaSession.GetOrLoad();
    float damagedRatio = _heroHealth != null && _heroHealth.Max > 0
        ? 1f - (float)_heroHealth.Current / _heroHealth.Max : 0f;
    SoulReward reward = SoulRewardCalculator.Calculate(result, _clock.Elapsed, _model.TotalSeconds, damagedRatio, _metaConfig);

    profile.Souls += reward.Souls;
    profile.LordXp += reward.Xp;
    profile.TotalRuns++;
    if (result == BattleResult.Win)
    {
        profile.TotalWins++;
        if (profile.BestClearTime < 0f || _clock.Elapsed < profile.BestClearTime)
            profile.BestClearTime = _clock.Elapsed;
    }
    //# 도감 — 이번 판 등장 종 + 픽 카드 기록 (스포너 종 + _pickCounter 픽 목록)
    //# 도전과제 — RunSummary 구성 (MaxSynergyTier 는 BuildSynergyService 에서 조회) 후 Evaluate
    RunSummary summary = BuildRunSummary(result);            //# private 헬퍼 신설
    foreach (AchievementDef a in AchievementService.Evaluate(profile, summary, _metaConfig))
        newlyAchieved.Add(a.DisplayName);

    soulsGained = reward.Souls; xpGained = reward.Xp;
    MetaSession.Store?.Save(profile);
}
```

- [ ] **Step 3: ResultPopupArg 확장 + ResultPopup 갱신**

```csharp
public class ResultPopupArg : UIArg
{
    public BattleResult Result;
    public int SoulsGained;
    public int XpGained;
    public List<string> NewlyAchieved = new List<string>();
}
```

  - ResultPopup: `_rewardText`(CHText) 추가 — `"💎 +{SoulsGained}  XP +{XpGained}"` + 달성 목록 줄바꿈 표시
  - `OnClickRestart` → `OnClickToVillage` 리네임, `SceneManager.LoadScene(EScene.Village.ToString())`
  - ResultPopup 프리팹에 `_rewardText` 노드 추가는 Task 5.4 빌더에서
- [ ] **Step 4: 컴파일 + 기존 EditMode 테스트 전체 회귀 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 전투 종료 시 소울/XP 정산과 도전과제 달성이 결과 팝업에 표시되고 마을로 복귀`

---

## Milestone 4 — Village 씬 + 허브 UI

### Task 4.1: VillageViewModel

**Files:**
- Create: `Assets/_Lair/Scripts/Village/VillageViewModel.cs`
- Test: `Assets/_Lair/Tests/EditMode/VillageViewModelTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

public class VillageViewModelTests
{
    [Test]
    public void 소울_변경시_이벤트가_발화한다()
    {
        MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
        VillageViewModel vm = new VillageViewModel(new MetaProfile { Souls = 10 }, cfg);
        int seen = -1;
        vm.OnSoulsChanged += s => seen = s;
        vm.NotifyProfileChanged();
        Assert.AreEqual(10, seen);
    }

    [Test]
    public void 영주_레벨과_진행률을_노출한다()
    {
        MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
        cfg.XpPerLevelBase = 100; cfg.XpGrowth = 1.25f;
        VillageViewModel vm = new VillageViewModel(new MetaProfile { LordXp = 100 }, cfg);
        Assert.AreEqual(2, vm.LordLevel);
        Assert.AreEqual(0f, vm.LordProgress, 0.01f);
    }
}
```

- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — `VillageViewModel(MetaProfile, MetaConfig)` : `int Souls` / `int LordLevel`(LordLevelService 위임) / `float LordProgress` / `event Action<int> OnSoulsChanged` / `event Action OnChanged` / `NotifyProfileChanged()` (상점 구매·보상 후 호출). MonoBehaviour 아님 (Rule 02 §6).
- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 마을 허브 ViewModel — 소울/영주 레벨 게이지 바인딩`

### Task 4.2: VillageHud (UIBase) + VillageController

**Files:**
- Create: `Assets/_Lair/Scripts/UI/Village/VillageHud.cs`, `Assets/_Lair/Scripts/Village/VillageController.cs`

- [ ] **Step 1: VillageHud 작성** — UIBase. `[SerializeField]` : `_soulText`·`_lordLevelText`(CHText), `_lordXpFill`(Image), 메뉴 버튼 6종 + `_sortieButton`(CHButton). `VillageHudArg : UIArg { VillageViewModel Vm; Action<EUI> OnOpenMenu; Action OnSortie; }` — 같은 파일 상단 (Rule 03 §5). `InitUI` 에서 VM 이벤트 구독(+`closeDisposable` 해제) 및 버튼 → `OnOpenMenu(EUI.ShopPopup)` 식 위임. 비즈니스 로직 없음 (Rule 02 §6).
- [ ] **Step 2: VillageController 작성**

```csharp
namespace Lair.Village
{
    //# Village 씬 진입점 — 프로필 로드 → 해골 idle 배치 → VillageHud 표시 → 메뉴/출격 위임.
    public class VillageController : MonoBehaviour
    {
        [SerializeField] private MetaConfig _metaConfig;
        [SerializeField] private Transform _heroAnchor;    //# 중앙 해골 배치 지점 (씬 정적 배치)

        private VillageViewModel _vm;

        private async void Start()
        {
            MetaProfile profile = MetaSession.GetOrLoad();
            _vm = new VillageViewModel(profile, _metaConfig);
            await SpawnIdleHero();        //# CHMResource.LoadAsync<GameObject>(EHero.Knight) → CHMPool.Pop → _heroAnchor 위치
                                          //# AutoCombatAI·HeroSkillRunner 등 전투 컴포넌트 disable, Animator 는 idle 상태 유지
            await CHMUI.Instance.ShowUIAsync(EUI.VillageHud, new VillageHudArg { Vm = _vm, OnOpenMenu = OpenMenu, OnSortie = Sortie });
        }

        private async void OpenMenu(EUI ui) { ... }   //# 각 팝업 Arg 구성 (M5 에서 케이스 추가)
        private void Sortie() => SceneManager.LoadScene(EScene.Battle.ToString());
        //# 상점 구매 등 프로필 변경 시: MetaSession.Store?.Save(profile); _vm.NotifyProfileChanged();
    }
}
```

  - **주의**: Village 씬은 Loading 을 거쳐 진입하므로 CHMResource/CHMUI/CHMPool Init 은 이미 완료 상태. 에디터에서 Village 직행 시를 위한 Init 가드는 넣지 않는다 (Battle 씬과 동일 정책).
- [ ] **Step 3: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 마을 씬 진입점 — 해골 영웅 대기 + 허브 메뉴/출격`

### Task 4.3: 씬 흐름 변경 — Loading → Village

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/LoadingController.cs:125`

- [ ] **Step 1: 수정** — `SceneManager.LoadScene(EScene.Battle.ToString())` → `EScene.Village.ToString()`
- [ ] **Step 2: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 게임 시작 시 마을로 진입하도록 흐름 변경`

---

## Milestone 5 — 팝업 6종 + 프리팹/씬 빌더

> 모든 팝업은 BuildModalPopup 패턴 (Rule 03 §3 CHPoolingScrollView 3-class 구조 + prefab 정적 배치). 코드 동적 GameObject 생성 금지. 더미/잠금 슬롯은 어두운 셀 + "???" 텍스트 + 자물쇠 아이콘(단색 생성 스프라이트)로 표현.

### Task 5.1: ShopPopup (3-class) — 상점

**Files:**
- Create: `Assets/_Lair/Scripts/UI/Village/ShopPopup.cs`, `ShopItemCell.cs`, `ShopItemPoolingScrollView.cs`
- Test: `Assets/_Lair/Tests/EditMode/ShopItemCellTests.cs`

- [ ] **Step 1: 실패 테스트** — `ShopItemCellData` 가공 검증 (BuildModalCardCellTests 패턴): 표시 문자열 `"Lv 2/5"`, 가격, 구매가능 여부가 (profile, cfg) 에서 올바로 계산되는지. 셀 자체는 GameObject 라 데이터 빌더 함수(`ShopPopup.BuildCellData(profile, cfg)` static)를 테스트
- [ ] **Step 2: 실행 — 실패 확인**
- [ ] **Step 3: 구현** — `ShopPopupArg : UIArg { ShopService Shop; MetaProfile Profile; MetaConfig Config; Action OnPurchased; }`. 셀: 이름/레벨/효과 요약/가격 + 구매 CHButton (불가 시 회색). 구매 성공 → `OnPurchased()` (VillageController 가 저장+VM 갱신) → `SetItemList` 재호출로 목록 갱신
- [ ] **Step 4: 테스트 통과 확인**
- [ ] **Step 5: 스테이징 + 커밋 메시지(안)** — `# [feat] - 소울 상점 팝업 — 영구 업그레이드 구매 UI`

### Task 5.2: QuestPopup · LordLevelPopup · HeroSelectPopup (3-class × 3)

**Files:**
- Create: `QuestPopup.cs`/`QuestCell.cs`/`QuestPoolingScrollView.cs`, `LordLevelPopup.cs`/`LordRewardCell.cs`/`LordRewardPoolingScrollView.cs`, `HeroSelectPopup.cs`/`HeroSelectCell.cs`/`HeroSelectPoolingScrollView.cs` (전부 `Assets/_Lair/Scripts/UI/Village/`)

- [ ] **Step 1: QuestPopup** — Arg `{ MetaProfile Profile; MetaConfig Config; }`. 셀: 이름/설명/보상/달성 체크 (달성 = `Profile.AchievedIds` 포함). 달성 셀은 강조, 미달성은 일반
- [ ] **Step 2: LordLevelPopup** — Arg `{ MetaProfile; MetaConfig; }`. `LordRewards` 를 레벨순 정렬. 셀 3상태: 수령가능(현재 레벨 도달+미수령→소울 즉시 지급은 v0.2 에선 **자동 수령** — 도달 시점 EndBattle 정산에서 처리하지 않고 단순 표시만; 기획서가 자동/수동 확정), 도달(완료 표시), 미도달(잠금). `IsLockedDummy` 셀은 "??? — 추후 해금"
- [ ] **Step 3: HeroSelectPopup** — Arg `{ MetaProfile; MetaConfig; Action<EHero> OnSelected; }`. 셀: Knight(선택 가능, 현재 선택 강조) + `HeroLockedSlots` 만큼 잠금 더미 셀. 선택 시 `Profile.SelectedHero` 갱신 + 저장 + 마을 중앙 모델 교체(v0.2 는 Knight 뿐이라 사실상 표시만)
- [ ] **Step 4: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 도전과제·영주성·영웅 선택 팝업 — 미래 콘텐츠는 잠금 슬롯으로 표시`

> **delta 각주 (2026-07-22)**: 위 **Step 3(HeroSelectPopup)** 의 명세(`EHero` 선택 + `HeroLockedSlots` 잠금 더미 + 중앙 모델 교체)는 **폐기**되었다. 현행 정본은 `docs/design/village-meta-hub.md` **rev 7 §6.1 — 스테이지 1~5 표시 전용 목록**(선택 없음, 실제 스테이지 선택은 마을 캐러셀 전담)이다. 본 plan 은 이력 문서로 본문을 수정하지 않는다.

### Task 5.3: CodexPopup + RecordsPopup

**Files:**
- Create: `CodexPopup.cs`/`CodexCell.cs`/`CodexPoolingScrollView.cs`, `RecordsPopup.cs`

- [ ] **Step 1: CodexPopup** — Arg `{ MetaProfile; MetaConfig; }`. 탭 2개(몬스터/카드, CHToggle): 몬스터 6종(= `SeenMonsters` 포함 시 컬러+이름, 미조우 실루엣) + 카드 28장(= `PickedCards`) + `CodexLockedSlots` 잠금 더미. 카드 일러스트는 기존 `Art/Sprites/CardIllustrations/` 재사용
- [ ] **Step 2: RecordsPopup** — Arg `{ MetaProfile; }`. CHText 행: 총 런 수 / 승리 수 / 승률 % / 최단 클리어(없으면 "-") . 스크롤뷰 불필요 — 정적 텍스트
- [ ] **Step 3: 컴파일 확인 + 스테이징 + 커밋 메시지(안)** — `# [feat] - 도감(몬스터·카드)과 전적 기록 팝업`

### Task 5.4: LairVillageBuilder — 씬 + 프리팹 + 에셋 생성

**Files:**
- Create: `Assets/_Lair/Editor/LairVillageBuilder.cs`
- Create(생성물): `Assets/_Lair/Scenes/Village.unity`, `Assets/_Lair/Art/UI/{VillageHud,ShopPopup,QuestPopup,CodexPopup,RecordsPopup,HeroSelectPopup,LordLevelPopup}.prefab` + 각 Cell prefab, `Assets/_Lair/Data/MetaConfig.asset`
- Modify(생성물): `Assets/_Lair/Art/UI/ResultPopup.prefab` (_rewardText 노드 추가)

- [ ] **Step 1: 빌더 작성** — 메뉴 3개:
  - `Lair/Setup/V1 - Build MetaConfig Asset` : MetaConfig.asset 생성 + 기획서 §의 상점 품목/영주 보상/도전과제 기본값 주입
  - `Lair/Setup/V2 - Build Village UI Prefabs` : 팝업 7종 + 셀 prefab 생성. **CHText 동행 필수** (TMP_Text 마다, Rule 03 §3), 모달은 BuildModalPopup 구조 복제, 카드 테두리 4색 톤 재사용. ResultPopup.prefab 에 `_rewardText` 추가 연결
  - `Lair/Setup/V3 - Build Village Scene` : Village.unity 생성 — 카메라(45° 탑다운, Battle 동일 톤), 라이트, 바닥 Plane(더미 배경 — 어두운 단색 머티리얼), `_heroAnchor` Transform, VillageController(+ MetaConfig·anchor 와이어링). Build Settings 에 씬 등록 (Loading 0 · Village 1 · Battle 2)
- [ ] **Step 2: 메뉴 실행 + Addressables 등록** — 프리팹 7종 주소=파일명, 라벨 `Resource` (Rule 03 §2 — EUI 값명과 정확 일치 확인). `Lair/Build Addressables` 재빌드
- [ ] **Step 3: 검증** — 에디터 Play: Loading → Village 진입, 해골 idle 표시, 메뉴 6종 팝업 개폐, 출격 → Battle → 결과 → 마을 복귀 1 사이클
- [ ] **Step 4: 스테이징 + 커밋 메시지(안)** — `# [asset] - 마을 씬·허브 UI 프리팹 일괄 생성 빌더`

### Task 5.5: 문자열 키 추가

**Files:**
- Modify: `Assets/_Lair/Art/Json/Strings_Ko.json`

- [ ] **Step 1: 마을 UI 문자열 키 추가** — 메뉴명(상점/도감/기록/영웅/퀘스트/영주성/출격), 팝업 타이틀, "??? — 추후 해금", 구매/만렙/소울 부족 등. 키 네이밍은 기존 파일 컨벤션 따름
- [ ] **Step 2: 스테이징 + 커밋 메시지(안)** — `# [feat] - 마을 UI 한글 문자열 추가`

---

## Milestone 6 — 통합 검증 + 마무리

### Task 6.1: 통합 회귀

- [ ] **Step 1: EditMode 전체 통과** (`Lair/Tests/Run EditMode Tests`) — 신규 Meta 테스트 + 기존 회귀
- [ ] **Step 2: PlayMode 전체 통과** — Battle 씬 기존 테스트가 MetaConfig 미할당 가드로 영향 없는지 확인
- [ ] **Step 3: 수동 풀 사이클 2회** — (1) 승리 런: 보상 표시→마을 소울 증가→상점 구매→재출격 시 몬스터 스탯 강화 확인 (2) 패배 런: 부분 소울 지급 확인. 세이브 파일 `persistentDataPath/meta_profile.json` 생성 확인
- [ ] **Step 4: 최종 스테이징 + 커밋 메시지(안)** — 마일스톤별 분할 커밋 권장 (Rule 01)

---

## Verification Gates

| Gate | 시점 | 기준 |
|---|---|---|
| G1 | M2 완료 | Meta 코어 EditMode 테스트 전부 green (Unity 씬 비의존) |
| G2 | M3 완료 | 기존 EditMode/PlayMode 회귀 통과 — MetaConfig null 가드로 기존 흐름 무영향 |
| G3 | M5 완료 | Loading→Village→Battle→Result→Village 풀 사이클 + 팝업 6종 수동 검증 |
| G4 | 전체 | 기획서 수치와 MetaConfig.asset 값 일치 (plan↔기획서 sync 규칙) |

## 리스크 메모 (spec §8 연동)

- 상점 만렙 총 보정률 상한은 기획서가 정의 — qa-simulator 재검증을 마무리 후 별도 제안
- `EScene`/`EUI` enum 은 반드시 뒤에 append (int 직렬화 — CommonEnum.cs 경고 주석 준수)
- Village↔Battle 씬 전환 시 CHMPool 풀 객체는 씬 파괴로 소멸 — Battle 진입마다 기존 워밍 경로 그대로 재워밍 (LoadingController 의 PreloadByLabelAsync 캐시는 유지)
