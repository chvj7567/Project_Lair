# 기록 팝업 스테이지별 전적 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 마을 기록 팝업에 스테이지 1~5 각각의 판수·승수·최단시간을 스크롤 리스트로 표시한다.

**Architecture:** `MetaProfile` 에 엔트리-리스트형 스테이지 전적을 추가하고(`ShopLevels` 패턴), `BattleController` 정산 한 곳에서 `SelectedStage` 기준으로 집계한다. UI 는 `RecordsPopup` 을 `CHPoolingScrollView` 3-class 구조로 재구성하며(Rule 03 §3), 행 조립은 `HeroSelectPopup.BuildCellData` 와 같은 static 순수 함수로 빼서 EditMode 로 검증한다.

**Tech Stack:** Unity 6 (6000.0.68f1) / C# / ChvjPackage(`CHMUI`·`CHText`·`CHButton`·`CHPoolingScrollView`) / NUnit EditMode

**Spec:** `docs/superpowers/specs/2026-07-22-records-stage-breakdown-design.md`
**목업:** `.mockups/records-stage-list.html`

## Global Constraints

- **Rule 01 — 자동 커밋 금지.** 각 태스크 끝은 `git add` 까지만. `git commit` 을 실행하지 않는다. 커밋 메시지(안)는 전체 완료 후 메인이 한 번에 제시한다.
- **Rule 02** — `//#` 주석만 / `var` 금지(명시적 타입) / `!` 금지(`== false`·`== null`) / 가드 절은 중괄호 없이 개행 / View 에 비즈니스 로직 금지.
- **Rule 03 §3** — `ScrollRect` + 수동 풀링 금지. `CHPoolingScrollView<TItem, TData>` 3-class(Panel / ScrollView / Cell) 필수. GameObject 코드 동적 생성 금지 — 프리팹 정적 배치 + 인스펙터 배선.
- **Rule 03 §5** — `UIArg` 파생은 페어 `UIBase` 와 같은 `.cs` 파일 상단.
- **Rule 04 §3** — 프리팹 *생성* 에디터 빌더는 일회용. 실행·검증 후 삭제한다.
- **CLAUDE.md §8** — 신규 영웅/스테이지 리소스 제작 금지. 초상은 기존 `Knight` 스프라이트 1장 + `HeroStageVariantConfig.GetStage(n).TintColor` 재사용.
- **테스트 실행** — UnityMCP `editor_invoke_method` 로 `Lair.Editor.LairTestRunner.RunEditModeTests` 호출, 결과는 `Library/lair-test-result.json`.
- **베이스라인** — 현재 EditMode `pass 1231 / fail 1` (잔여 1건 = `SkillUnlockCutsceneControllerSuiteTests`, 범위 밖). 이 수치보다 fail 이 늘면 회귀다.
- **테스트 메서드명은 한글** (`project.md` `test_method_naming: korean`).

---

## File Structure

**Create**
- `Assets/_Lair/Scripts/UI/Village/RecordsStagePoolingScrollView.cs` — 스크롤뷰 컴포넌트 (`InitItem` 만)
- `Assets/_Lair/Scripts/UI/Village/RecordsStageCell.cs` — 셀 View (표시 전용)
- `Assets/_Lair/Art/UI/RecordsStageCell.prefab` — 셀 프리팹
- `Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs` — 데이터·집계 테스트
- `Assets/_Lair/Tests/EditMode/UI/RecordsStageRowTests.cs` — 행 조립 테스트

**Modify**
- `Assets/_Lair/Scripts/Meta/MetaProfile.cs` — `Version` 3, `StageRecords`, `StageRecordEntry`, 조회/갱신 헬퍼, `CopyFrom`
- `Assets/_Lair/Scripts/Battle/BattleController.cs:952-966` — 정산 블록에 스테이지 집계 1줄 계열 추가
- `Assets/_Lair/Scripts/UI/Village/RecordsPopup.cs` — Arg 에 `VariantConfig` 추가, `RecordsStageCellData`, `BuildCellData`, 스크롤뷰 배선
- `Assets/_Lair/Scripts/Village/VillageController.cs:169-172` — `RecordsPopupArg` 에 `_stageVariantConfig` 전달
- `Assets/_Lair/Art/UI/RecordsPopup.prefab` — 요약 4칸 + ScrollView 구조

---

### Task 1: MetaProfile 스테이지 전적 필드

**Files:**
- Modify: `Assets/_Lair/Scripts/Meta/MetaProfile.cs`
- Test: `Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs` (Create)

**Interfaces:**
- Consumes: 없음 (첫 태스크)
- Produces:
  - `Lair.Meta.StageRecordEntry` — `public int Stage; public int Runs; public int Wins; public float BestClearTime = -1f;`
  - `MetaProfile.StageRecords` — `List<StageRecordEntry>`
  - `StageRecordEntry MetaProfile.GetStageRecord(int stage)` — 없으면 기본값 엔트리(비저장) 반환, 절대 null 아님
  - `void MetaProfile.RecordStageRun(int stage, bool win, float clearTime)` — 엔트리 없으면 생성 후 갱신
  - `MetaProfile.Version` == 3

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs` 를 새로 만든다:

```csharp
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 스테이지별 전적 — 조회 폴백 / 집계 / 최단 갱신 / 직렬화 (spec §4).
    public class StageRecordTests
    {
        [Test]
        public void GetStageRecord_기록이_없는_스테이지는_0판_0승_최단없음을_돌려준다()
        {
            MetaProfile p = new MetaProfile();

            StageRecordEntry r = p.GetStageRecord(3);

            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.Runs);
            Assert.AreEqual(0, r.Wins);
            Assert.AreEqual(-1f, r.BestClearTime);
        }

        [Test]
        public void GetStageRecord_조회만으로는_엔트리가_생기지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.GetStageRecord(3);

            Assert.AreEqual(0, p.StageRecords.Count);
        }

        [Test]
        public void RecordStageRun_패배는_판수만_올리고_승수와_최단은_그대로다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(2, win: false, clearTime: 180f);

            StageRecordEntry r = p.GetStageRecord(2);
            Assert.AreEqual(1, r.Runs);
            Assert.AreEqual(0, r.Wins);
            Assert.AreEqual(-1f, r.BestClearTime);
        }

        [Test]
        public void RecordStageRun_승리는_판수와_승수를_올리고_최단을_기록한다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(2, win: true, clearTime: 180f);

            StageRecordEntry r = p.GetStageRecord(2);
            Assert.AreEqual(1, r.Runs);
            Assert.AreEqual(1, r.Wins);
            Assert.AreEqual(180f, r.BestClearTime);
        }

        [Test]
        public void RecordStageRun_최단은_더_빠를_때만_갱신된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, win: true, clearTime: 150f);

            p.RecordStageRun(1, win: true, clearTime: 200f);

            Assert.AreEqual(150f, p.GetStageRecord(1).BestClearTime);

            p.RecordStageRun(1, win: true, clearTime: 120f);

            Assert.AreEqual(120f, p.GetStageRecord(1).BestClearTime);
        }

        [Test]
        public void RecordStageRun_같은_스테이지_반복은_엔트리를_늘리지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(4, win: false, clearTime: 300f);
            p.RecordStageRun(4, win: true, clearTime: 240f);

            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(2, p.GetStageRecord(4).Runs);
        }

        [Test]
        public void RecordStageRun_스테이지가_다르면_엔트리가_각각_추가된다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(5, win: false, clearTime: 300f);

            Assert.AreEqual(2, p.StageRecords.Count);
            Assert.AreEqual(1, p.GetStageRecord(1).Wins);
            Assert.AreEqual(0, p.GetStageRecord(5).Wins);
        }

        [Test]
        public void 구버전_세이브에_StageRecords_필드가_없어도_빈_리스트로_로드된다()
        {
            //# Version 2 시절 JSON — StageRecords 키 자체가 없다.
            string legacyJson = "{\"Version\":2,\"Souls\":500,\"TotalRuns\":40,\"TotalWins\":25}";

            MetaProfile p = JsonUtility.FromJson<MetaProfile>(legacyJson);

            Assert.IsNotNull(p.StageRecords);
            Assert.AreEqual(0, p.StageRecords.Count);
            Assert.AreEqual(25, p.TotalWins);
            Assert.AreEqual(0, p.GetStageRecord(1).Wins);
        }

        [Test]
        public void StageRecords_는_JSON_왕복으로_보존된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(3, win: true, clearTime: 199.5f);

            MetaProfile round = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(1, round.GetStageRecord(3).Wins);
            Assert.AreEqual(199.5f, round.GetStageRecord(3).BestClearTime, 0.001f);
        }

        [Test]
        public void CopyFrom_은_스테이지_전적을_복원한다()
        {
            MetaProfile cloud = new MetaProfile();
            cloud.RecordStageRun(2, win: true, clearTime: 210f);
            MetaProfile local = new MetaProfile();

            local.CopyFrom(cloud);

            Assert.AreEqual(1, local.GetStageRecord(2).Wins);
            Assert.AreEqual(210f, local.GetStageRecord(2).BestClearTime);
        }

        [Test]
        public void 신규_프로필의_스키마_버전은_3이다()
        {
            Assert.AreEqual(3, new MetaProfile().Version);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

UnityMCP `editor_invoke_method` → `Lair.Editor.LairTestRunner.RunEditModeTests`
기대: `StageRecordTests` 가 **컴파일 에러**(`StageRecordEntry` 없음 / `GetStageRecord` 없음). 컴파일이 막히면 그것이 이 단계의 "실패" 신호다.

- [ ] **Step 3: 구현**

`MetaProfile.cs` — `Version` 을 3으로 올리고 `ShopLevels` 선언 아래에 필드를 추가한다:

```csharp
        public int Version = 3;
```

```csharp
        public List<ShopLevelEntry> ShopLevels = new List<ShopLevelEntry>();
        //# 스테이지별 전적(spec §4) — JsonUtility 가 Dictionary 를 못 다뤄 ShopLevels 와 같은 엔트리-리스트.
        //# 구버전 세이브엔 이 키가 없어 빈 리스트로 로드된다 = 기존 유저는 전 스테이지 0부터.
        public List<StageRecordEntry> StageRecords = new List<StageRecordEntry>();
```

메서드 — `SetShopLevel` 아래에 추가:

```csharp
        //# 스테이지 전적 조회 — 없으면 기본값(0/0/-1) 엔트리를 반환하되 리스트에 넣지 않는다(조회는 부수효과 0).
        public StageRecordEntry GetStageRecord(int stage)
        {
            foreach (StageRecordEntry entry in StageRecords)
            {
                if (entry != null && entry.Stage == stage)
                    return entry;
            }
            return new StageRecordEntry { Stage = stage };
        }

        //# 한 판 결과를 스테이지 전적에 반영 — 엔트리 없으면 생성. 최단은 승리 시에만, 더 빠를 때만 갱신.
        public void RecordStageRun(int stage, bool win, float clearTime)
        {
            StageRecordEntry entry = null;
            foreach (StageRecordEntry e in StageRecords)
            {
                if (e != null && e.Stage == stage)
                {
                    entry = e;
                    break;
                }
            }
            if (entry == null)
            {
                entry = new StageRecordEntry { Stage = stage };
                StageRecords.Add(entry);
            }

            entry.Runs++;
            if (win == false)
                return;

            entry.Wins++;
            if (entry.BestClearTime < 0f || clearTime < entry.BestClearTime)
            {
                entry.BestClearTime = clearTime;
            }
        }
```

`CopyFrom` — `ShopLevels` 복사 줄 바로 아래에 추가:

```csharp
            ShopLevels = other.ShopLevels ?? new List<ShopLevelEntry>();
            //# 스테이지 전적도 클라우드 복원 대상 — 빠뜨리면 복원 시 이 기록만 유실된다(spec §8).
            StageRecords = other.StageRecords ?? new List<StageRecordEntry>();
```

파일 하단 `ShopLevelEntry` 옆에 추가:

```csharp
    //# 스테이지 한 칸의 누적 전적. JsonUtility 직렬화 대상.
    [Serializable]
    public class StageRecordEntry
    {
        public int Stage;                    //# 1~5
        public int Runs;
        public int Wins;
        public float BestClearTime = -1f;    //# 승리 기록 없으면 -1 (MetaProfile.BestClearTime 과 같은 규약)
    }
```

- [ ] **Step 4: 통과 확인**

`RunEditModeTests` 재실행. 기대: `StageRecordTests` 11건 전부 PASS, 전체 fail 이 베이스라인 1건에서 늘지 않음.

- [ ] **Step 5: 스테이징 (커밋 금지 — Rule 01)**

```bash
git add Assets/_Lair/Scripts/Meta/MetaProfile.cs \
        Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs \
        Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs.meta
```

---

### Task 2: 전투 정산에서 스테이지 집계

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (정산 블록, `profile.TotalRuns++` 부근 — 현재 952~966행)
- Test: `Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs` (Task 1 파일에 추가)

**Interfaces:**
- Consumes: `MetaProfile.RecordStageRun(int, bool, float)` (Task 1)
- Produces: 없음 (호출 지점만 추가)

- [ ] **Step 1: 실패하는 테스트 추가**

`StageRecordTests.cs` 클래스 안에 두 건 추가한다. `BattleController` 는 MonoBehaviour 라 직접 호출하지 않고, **정산 계약(총계와 스테이지 전적이 같은 판에서 함께 움직인다)** 을 프로필 수준에서 고정한다:

```csharp
        [Test]
        public void 정산_계약_승리는_총계와_선택스테이지_전적이_함께_증가한다()
        {
            MetaProfile p = new MetaProfile();
            p.SelectedStage = 3;

            //# BattleController 정산 블록과 같은 순서 — 총계 가산 후 스테이지 집계.
            p.TotalRuns++;
            p.TotalWins++;
            p.RecordStageRun(p.SelectedStage, win: true, clearTime: 175f);

            Assert.AreEqual(1, p.TotalRuns);
            Assert.AreEqual(1, p.TotalWins);
            Assert.AreEqual(1, p.GetStageRecord(3).Runs);
            Assert.AreEqual(1, p.GetStageRecord(3).Wins);
            Assert.AreEqual(0, p.GetStageRecord(2).Runs);
        }

        [Test]
        public void 정산_계약_패배는_선택스테이지_판수만_증가한다()
        {
            MetaProfile p = new MetaProfile();
            p.SelectedStage = 5;

            p.TotalRuns++;
            p.RecordStageRun(p.SelectedStage, win: false, clearTime: 300f);

            Assert.AreEqual(0, p.TotalWins);
            Assert.AreEqual(1, p.GetStageRecord(5).Runs);
            Assert.AreEqual(0, p.GetStageRecord(5).Wins);
        }
```

- [ ] **Step 2: 실패 확인**

`RunEditModeTests` 실행. Task 1 이 끝난 뒤라면 이 두 건은 **통과할 수 있다** — 프로필 API 만 쓰기 때문이다. 그 경우 다음 스텝의 실제 검증은 **호출 지점이 실제로 존재하는지**이며, Step 4 의 grep 확인이 그 게이트다.

- [ ] **Step 3: 구현**

`BattleController.cs` 정산 블록. 현재:

```csharp
                profile.TotalRuns++;
                if (result == BattleResult.Win)
                {
                    profile.TotalWins++;
```

이렇게 바꾼다 (`ClearedStage` 갱신보다 **앞**에서 집계해야 이번 판의 스테이지가 그대로 쓰인다 — `SelectedStage` 자체는 정산 중 바뀌지 않지만 순서를 명시적으로 둔다):

```csharp
                profile.TotalRuns++;
                //# 스테이지별 전적 — 이번 판의 SelectedStage 기준 (spec §5). 총계와 같은 판에서 함께 움직인다.
                profile.RecordStageRun(profile.SelectedStage, result == BattleResult.Win, _clock.Elapsed);
                if (result == BattleResult.Win)
                {
                    profile.TotalWins++;
```

그 아래 `ClearedStage` / `BestClearTime` / `SubmitRanking` 블록은 **그대로 둔다.**

- [ ] **Step 4: 검증**

호출 지점 확인:

```bash
grep -n "RecordStageRun" Assets/_Lair/Scripts/Battle/BattleController.cs
```

기대: 1건, `profile.TotalRuns++;` 바로 다음 줄.

`RunEditModeTests` 재실행 — 기대: 신규 2건 PASS, 전체 fail 베이스라인 유지.

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs \
        Assets/_Lair/Tests/EditMode/Meta/StageRecordTests.cs
```

---

### Task 3: 행 조립 순수 함수 + 셀 데이터

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/Village/RecordsPopup.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/RecordsStageRowTests.cs` (Create)

**Interfaces:**
- Consumes: `MetaProfile.GetStageRecord`(Task 1), `Lair.Battle.StageProgress.IsUnlocked(int, int)`, `StageProgress.MaxStage`(=5), `Lair.Data.HeroStageVariantConfig.GetStage(int).TintColor`
- Produces:
  - `Lair.UI.RecordsStageCellData` — `public int Stage; public bool IsLocked; public bool IsSelected; public Sprite Portrait; public Color PortraitTint; public string StageText; public string ThreatText; public string WinText; public string RunRateText; public string BestText; public string LockHintText;`
  - `static List<RecordsStageCellData> RecordsPopup.BuildCellData(MetaProfile, HeroStageVariantConfig, Sprite portrait)`
  - `static string RecordsPopup.FormatClearTime(float seconds)` — `-1` → `"-"`, 그 외 `"m:ss"`
  - `RecordsPopupArg.VariantConfig` — `HeroStageVariantConfig`

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Lair/Tests/EditMode/UI/RecordsStageRowTests.cs`:

```csharp
using System.Collections.Generic;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 기록 팝업 스테이지 행 조립 — 항상 5행 / 잠금 판정 / 승률 / 최단 표기 (spec §6.3).
    public class RecordsStageRowTests
    {
        [Test]
        public void 항상_스테이지_1부터_5까지_다섯_행이_나온다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null, null);

            Assert.AreEqual(5, rows.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i + 1, rows[i].Stage);
            }
        }

        [Test]
        public void 미클리어_프로필은_1스테이지만_해금이고_나머지는_잠금이다()
        {
            MetaProfile p = new MetaProfile();   //# ClearedStage = 0

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsFalse(rows[0].IsLocked);
            Assert.IsTrue(rows[1].IsLocked);
            Assert.IsTrue(rows[4].IsLocked);
        }

        [Test]
        public void 세_스테이지_클리어면_네번째까지_해금이다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 3 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsFalse(rows[3].IsLocked);   //# 스테이지 4
            Assert.IsTrue(rows[4].IsLocked);    //# 스테이지 5
        }

        [Test]
        public void 전부_클리어면_잠긴_행이_없다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 5 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            foreach (RecordsStageCellData row in rows)
            {
                Assert.IsFalse(row.IsLocked);
            }
        }

        [Test]
        public void 잠긴_행은_해금_조건_문구를_들고_전적_문구는_비어_있다()
        {
            MetaProfile p = new MetaProfile();   //# ClearedStage = 0 → 스테이지 3 잠금

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[2];

            Assert.AreEqual("스테이지 2 클리어 필요", row.LockHintText);
            Assert.IsEmpty(row.WinText);
            Assert.IsEmpty(row.RunRateText);
        }

        [Test]
        public void 해금_행은_승리수와_판수_승률을_문구로_만든다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 2 };
            for (int i = 0; i < 3; ++i)
                p.RecordStageRun(1, win: true, clearTime: 200f);
            p.RecordStageRun(1, win: false, clearTime: 300f);

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[0];

            Assert.AreEqual("3승", row.WinText);
            Assert.AreEqual("4판 · 75%", row.RunRateText);
        }

        [Test]
        public void 판수가_0이면_승률은_0퍼센트로_표기된다()
        {
            MetaProfile p = new MetaProfile();

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[0];

            Assert.AreEqual("0승", row.WinText);
            Assert.AreEqual("0판 · 0%", row.RunRateText);
        }

        [Test]
        public void 승률은_반올림된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(1, win: false, clearTime: 100f);

            //# 2/3 = 66.67% → 67%
            Assert.AreEqual("3판 · 67%", RecordsPopup.BuildCellData(p, null, null)[0].RunRateText);
        }

        [Test]
        public void 위협도는_스테이지_수만큼_별이_찬다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null, null);

            Assert.AreEqual("★☆☆☆☆", rows[0].ThreatText);
            Assert.AreEqual("★★★★★", rows[4].ThreatText);
        }

        [Test]
        public void 최단시간이_없으면_대시로_표기된다()
        {
            Assert.AreEqual("-", RecordsPopup.FormatClearTime(-1f));
        }

        [Test]
        public void 최단시간은_분초로_표기된다()
        {
            Assert.AreEqual("3:18", RecordsPopup.FormatClearTime(198.4f));
            Assert.AreEqual("0:07", RecordsPopup.FormatClearTime(7f));
        }

        [Test]
        public void 선택중_배지는_해금된_선택스테이지에만_붙는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 2, SelectedStage = 3 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsTrue(rows[2].IsSelected);
            Assert.IsFalse(rows[0].IsSelected);
        }

        [Test]
        public void 잠긴_스테이지가_선택중이어도_배지는_붙지_않는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 0, SelectedStage = 4 };

            Assert.IsFalse(RecordsPopup.BuildCellData(p, null, null)[3].IsSelected);
        }

        [Test]
        public void 프로필이_null이면_진행도_0으로_폴백하고_예외가_없다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(null, null, null);

            Assert.AreEqual(5, rows.Count);
            Assert.IsFalse(rows[0].IsLocked);
            Assert.IsTrue(rows[1].IsLocked);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

`RunEditModeTests` — 기대: 컴파일 에러(`RecordsStageCellData` / `BuildCellData` / `FormatClearTime` 없음).

- [ ] **Step 3: 구현**

`RecordsPopup.cs` 전체를 다음으로 바꾼다 (기존 `BuildBody` 는 상단 요약에 그대로 쓰이므로 **유지**):

```csharp
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class RecordsPopupArg : UIArg
    {
        public MetaProfile Profile;
        public HeroStageVariantConfig VariantConfig;
    }

    //# 스테이지 한 행의 표시 확정값 — 셀은 계산하지 않는다(spec §6.2).
    public class RecordsStageCellData
    {
        public int Stage;
        public bool IsLocked;
        public bool IsSelected;
        public Sprite Portrait;
        public Color PortraitTint;
        public string StageText;        //# "STAGE 3"
        public string ThreatText;       //# "★★★☆☆"
        public string WinText;          //# "12승" (잠금이면 빈 문자열)
        public string RunRateText;      //# "20판 · 60%" (잠금이면 빈 문자열)
        public string BestText;         //# "최단 3:18" (잠금이면 빈 문자열)
        public string LockHintText;     //# "스테이지 2 클리어 필요" (해금이면 빈 문자열)
    }

    //# 전적 기록 — 상단 총계 4항목 + 스테이지 1~5 스크롤 리스트 (spec §6).
    public class RecordsPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _bodyText;                             //# 상단 총계
        [SerializeField] private RecordsStagePoolingScrollView _scrollView;

        //# 영웅 초상 — 인스펙터 직접 참조 (HeroSelectPopup 관례, Addressables 키 아님).
        //# 스켈레톤 1모델 재스킨이라 5스테이지가 같은 초상을 틴트만 달리해 공유한다.
        [SerializeField] private Sprite _knightPortrait;

        //# 잠금 행 어둠 비율 — 캐러셀/영웅 목록의 잠금 톤과 동일.
        public const float LockedDimRatio = 0.55f;

        private RecordsPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as RecordsPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() => _arg = null);
            }

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }

            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        //# prefab 이 inactive 로 저장된 경우 InitUI 시점은 layout 미산정 → 첫 조립은 OnEnable 이 담당.
        private void OnEnable()
        {
            if (_arg == null)
                return;
            BuildAndLayout();
        }

        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Rebuild();
        }

        private void Rebuild()
        {
            if (_arg == null)
                return;

            if (_bodyText != null)
            {
                _bodyText.SetText(BuildBody(_arg.Profile));
            }
            if (_scrollView != null)
            {
                _scrollView.SetItemList(BuildCellData(_arg.Profile, _arg.VariantConfig, _knightPortrait));
            }
        }

        //# 상단 총계 — 기존 표기 유지(spec D3). 스테이지 합과 어긋날 수 있으나 의도된 동작.
        public static string BuildBody(MetaProfile profile)
        {
            if (profile == null)
                return string.Empty;

            int winRate = profile.TotalRuns > 0
                ? Mathf.RoundToInt(profile.TotalWins * 100f / profile.TotalRuns)
                : 0;
            string bestClear = profile.BestClearTime < 0f ? "-" : $"{profile.BestClearTime:0.0}초";
            return $"총 출격  {profile.TotalRuns}\n승리  {profile.TotalWins}\n승률  {winRate}%\n최단 클리어  {bestClear}";
        }

        //# 스테이지 1~5 행 — 해금은 전적, 잠금은 해금 조건. profile null 이면 진행도 0, config null 이면 틴트 흰색 폴백.
        public static List<RecordsStageCellData> BuildCellData(
            MetaProfile profile, HeroStageVariantConfig variantConfig, Sprite portrait)
        {
            List<RecordsStageCellData> list = new List<RecordsStageCellData>();
            int cleared = profile != null ? profile.ClearedStage : 0;
            int selected = profile != null ? profile.SelectedStage : 0;

            for (int stage = 1; stage <= StageProgress.MaxStage; ++stage)
            {
                //# 해금 판정은 캐러셀과 같은 단일 소유 헬퍼.
                bool unlocked = StageProgress.IsUnlocked(stage, cleared);
                Color tint = variantConfig != null ? variantConfig.GetStage(stage).TintColor : Color.white;
                StageRecordEntry record = profile != null
                    ? profile.GetStageRecord(stage)
                    : new StageRecordEntry { Stage = stage };
                int rate = record.Runs > 0 ? Mathf.RoundToInt(record.Wins * 100f / record.Runs) : 0;

                list.Add(new RecordsStageCellData
                {
                    Stage = stage,
                    IsLocked = unlocked == false,
                    IsSelected = unlocked && stage == selected,
                    Portrait = portrait,
                    PortraitTint = unlocked ? tint : Color.Lerp(tint, Color.black, LockedDimRatio),
                    StageText = $"STAGE {stage}",
                    ThreatText = BuildThreat(stage),
                    WinText = unlocked ? $"{record.Wins}승" : string.Empty,
                    RunRateText = unlocked ? $"{record.Runs}판 · {rate}%" : string.Empty,
                    BestText = unlocked ? $"최단 {FormatClearTime(record.BestClearTime)}" : string.Empty,
                    LockHintText = unlocked ? string.Empty : $"스테이지 {stage - 1} 클리어 필요",
                });
            }
            return list;
        }

        //# 위협도 — 채운 별 N + 빈 별 (5-N). VillageHud.BuildThreat 과 같은 규약(저쪽은 private static,
        //# 여기선 테스트 대상이라 public). 두 곳이 어긋나면 표기가 갈리므로 규약 변경 시 함께 고친다.
        public static string BuildThreat(int stage)
        {
            int filled = Mathf.Clamp(stage, 0, StageProgress.MaxStage);
            return new string('★', filled) + new string('☆', StageProgress.MaxStage - filled);
        }

        //# 클리어타임 표기 — 기록 없음(-1)은 "-", 그 외 m:ss (초는 내림).
        public static string FormatClearTime(float seconds)
        {
            if (seconds < 0f)
                return "-";
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
```

- [ ] **Step 4: 통과 확인**

`RunEditModeTests` — 기대: `RecordsStageRowTests` 14건 PASS, 전체 fail 베이스라인 유지.

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/UI/Village/RecordsPopup.cs \
        Assets/_Lair/Tests/EditMode/UI/RecordsStageRowTests.cs \
        Assets/_Lair/Tests/EditMode/UI/RecordsStageRowTests.cs.meta
```

---

### Task 4: 스크롤뷰 · 셀 컴포넌트 + Arg 전달

**Files:**
- Create: `Assets/_Lair/Scripts/UI/Village/RecordsStagePoolingScrollView.cs`
- Create: `Assets/_Lair/Scripts/UI/Village/RecordsStageCell.cs`
- Modify: `Assets/_Lair/Scripts/Village/VillageController.cs` (169~172행 `RecordsPopupArg` 생성부)

**Interfaces:**
- Consumes: `RecordsStageCellData`(Task 3), `RecordsPopupArg.VariantConfig`(Task 3), `VillageController._stageVariantConfig`(기존 필드 — `HeroSelectPopupArg` 에 이미 쓰임)
- Produces: `RecordsStagePoolingScrollView`(= `CHPoolingScrollView<RecordsStageCell, RecordsStageCellData>`), `RecordsStageCell.Bind(RecordsStageCellData)`

- [ ] **Step 1: 스크롤뷰 작성**

`Assets/_Lair/Scripts/UI/Village/RecordsStagePoolingScrollView.cs`:

```csharp
using ChvjUnityInfra;

namespace Lair.UI
{
    //# 기록 스테이지 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class RecordsStagePoolingScrollView : CHPoolingScrollView<RecordsStageCell, RecordsStageCellData>
    {
        public override void InitItem(RecordsStageCell item, RecordsStageCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(RecordsStageCell item)
        {
        }
    }
}
```

- [ ] **Step 2: 셀 작성**

`Assets/_Lair/Scripts/UI/Village/RecordsStageCell.cs`:

```csharp
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 기록 스테이지 셀 — 표시 전용. 문구·색은 전부 RecordsStageCellData 가 확정해서 들어온다 (Rule 02 §6).
    public class RecordsStageCell : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private CHText _stageText;
        [SerializeField] private CHText _threatText;
        [SerializeField] private CHText _bestText;
        [SerializeField] private CHText _winText;
        [SerializeField] private CHText _runRateText;
        [SerializeField] private CHText _lockHintText;
        [SerializeField] private GameObject _selectedBadge;

        //# 풀 재사용 리셋 — 이전 행의 잠금/배지 상태가 새 행에 새지 않게 (Rule 03 §4).
        private void OnEnable()
        {
            if (_selectedBadge != null)
            {
                _selectedBadge.SetActive(false);
            }
            if (_lockHintText != null)
            {
                _lockHintText.gameObject.SetActive(false);
            }
        }

        public void Bind(RecordsStageCellData data)
        {
            if (data == null)
                return;

            if (_portrait != null)
            {
                _portrait.sprite = data.Portrait;
                _portrait.color = data.PortraitTint;
            }
            if (_stageText != null)
            {
                _stageText.SetText(data.StageText);
            }
            if (_threatText != null)
            {
                _threatText.SetText(data.ThreatText);
            }
            if (_bestText != null)
            {
                _bestText.SetText(data.BestText);
                _bestText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_winText != null)
            {
                _winText.SetText(data.WinText);
                _winText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_runRateText != null)
            {
                _runRateText.SetText(data.RunRateText);
                _runRateText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_lockHintText != null)
            {
                _lockHintText.SetText(data.LockHintText);
                _lockHintText.gameObject.SetActive(data.IsLocked);
            }
            if (_selectedBadge != null)
            {
                _selectedBadge.SetActive(data.IsSelected);
            }
        }
    }
}
```

- [ ] **Step 3: VillageController 에서 config 전달**

`VillageController.cs` 169~172행:

```csharp
                case EUI.RecordsPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.RecordsPopup, new RecordsPopupArg
                    {
                        Profile = profile,
                        VariantConfig = _stageVariantConfig,
                    });
                    break;
```

- [ ] **Step 4: 컴파일 확인**

UnityMCP `editor_recompile` 후 `editor_read_log` — 기대: 컴파일 에러 0.
`RunEditModeTests` 재실행 — 기대: fail 베이스라인 유지.

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/UI/Village/RecordsStagePoolingScrollView.cs \
        Assets/_Lair/Scripts/UI/Village/RecordsStagePoolingScrollView.cs.meta \
        Assets/_Lair/Scripts/UI/Village/RecordsStageCell.cs \
        Assets/_Lair/Scripts/UI/Village/RecordsStageCell.cs.meta \
        Assets/_Lair/Scripts/Village/VillageController.cs
```

---

### Task 5: 프리팹 — 셀 신규 + 팝업 개조

**Files:**
- Create: `Assets/_Lair/Art/UI/RecordsStageCell.prefab`
- Modify: `Assets/_Lair/Art/UI/RecordsPopup.prefab`
- Create(임시): `Assets/_Lair/Editor/OneShot/LairRecordsUIBuilder.cs` — 실행 후 **삭제** (Rule 04 §3)

**Interfaces:**
- Consumes: `RecordsStageCell`, `RecordsStagePoolingScrollView`, `RecordsPopup`(Task 3·4)
- Produces: 배선 완료된 프리팹 2종

**참조 구현:** `Assets/_Lair/Art/UI/CodexPopup.prefab` + `CodexCell.prefab` (같은 3-class 구조, 목록형)

- [ ] **Step 1: 셀 프리팹 구성**

`RecordsStageCell.prefab` — 루트에 `RectTransform`(높이 82) + `Image`(배경) + `RecordsStageCell`.
자식:

| 이름 | 컴포넌트 | 배선 대상 |
|---|---|---|
| `Portrait` | `Image` | `_portrait` |
| `StageText` | `TextMeshProUGUI` + `CHText` | `_stageText` |
| `ThreatText` | `TextMeshProUGUI` + `CHText` | `_threatText` |
| `BestText` | `TextMeshProUGUI` + `CHText` | `_bestText` |
| `WinText` | `TextMeshProUGUI` + `CHText` | `_winText` |
| `RunRateText` | `TextMeshProUGUI` + `CHText` | `_runRateText` |
| `LockHintText` | `TextMeshProUGUI` + `CHText` | `_lockHintText` |
| `SelectedBadge` | `Image` + 자식 `TextMeshProUGUI`+`CHText`("선택 중") | `_selectedBadge` |

**모든 TMP_Text 에 `CHText` 동행 필수** — 정적 라벨도 예외 없음 (Rule 03 §3).

- [ ] **Step 2: 팝업 프리팹 개조**

`RecordsPopup.prefab` — 기존 `_bodyText` 는 상단 요약으로 **유지**하고, 그 아래에 스크롤 구조를 추가한다:

```
RecordsPopup (RecordsPopup)
├ Dim (CHButton) / Title / Close (CHButton)
├ BodyText (TMP + CHText)                 ← 기존 _bodyText 유지 (상단 총계)
└ ScrollView
   ├ ScrollRect + RecordsStagePoolingScrollView
   ├ Viewport (Image + RectMask2D)
   │  └ Content (VerticalLayoutGroup)
   │     └ RecordsStageCell.prefab 인스턴스   ← origin
```

인스펙터 배선:
- `RecordsPopup._scrollView` → 자식 `ScrollView` GameObject
- `RecordsPopup._knightPortrait` → `HeroSelectPopup._knightPortrait` 가 참조하는 것과 **같은 스프라이트**
- `RecordsStagePoolingScrollView._origin` → Content 아래 origin Cell 인스턴스

origin Cell 인스턴스에서 컴포넌트를 제거(`m_RemovedComponents`)하지 않는다 — `_portrait` 등 참조가 null 이 되어 시각이 깨진다 (Rule 03 §3 체크리스트).

- [ ] **Step 3: Addressable 확인**

`RecordsPopup.prefab` 은 이미 `EUI.RecordsPopup` 주소로 등록되어 있다. `RecordsStageCell.prefab` 은 **직접 로드되지 않고 프리팹 참조로만 쓰이므로 Addressable 등록 불필요** — `CodexCell.prefab` 과 동일.

확인:

```bash
grep -c "RecordsPopup" Assets/AddressableAssetsData/AssetGroups/*.asset
```

- [ ] **Step 4: 육안 검증**

UnityMCP `prefab_open` → `screenshot_editor` 로 `RecordsPopup.prefab` 캡처.
확인 항목: 5행이 세로로 쌓임 / 초상 색이 스테이지마다 다름 / 잠금 행에 해금 문구 / 요약 4항목이 위에 남아 있음 / 셀 높이 균일.

- [ ] **Step 5: 빌더 삭제 + 스테이징**

프리팹 생성 빌더를 썼다면 **삭제한다** (Rule 04 §3 — 프리팹이 단일 진실).

```bash
git add Assets/_Lair/Art/UI/RecordsStageCell.prefab \
        Assets/_Lair/Art/UI/RecordsStageCell.prefab.meta \
        Assets/_Lair/Art/UI/RecordsPopup.prefab
```

---

### Task 6: 회귀 확인 + 마무리

**Files:** 없음 (검증만)

- [ ] **Step 1: EditMode 전체 실행**

UnityMCP `editor_invoke_method` → `Lair.Editor.LairTestRunner.RunEditModeTests`
기대: **fail 이 베이스라인 1건(`SkillUnlockCutsceneControllerSuiteTests`)을 넘지 않는다.** 신규 27건(Task 1 의 11 + Task 2 의 2 + Task 3 의 14) 전부 PASS.

- [ ] **Step 2: 실패 유입 여부 대조**

fail 이 늘었다면 `Library/lair-test-result.json` 의 실패 목록을 베이스라인과 대조해 이번 변경이 원인인지 가린다. 원인이면 고치고, 아니면 기록만 남긴다.

- [ ] **Step 3: 스테이징 최종 점검**

```bash
git status --short
```

무관한 dirty 파일(예: `VillageHud.prefab`, `NotoSansKR SDF.asset`, `.mcp.json`)이 섞여 있으면 unstage 한다 (Rule 01 add 범위 주의).

- [ ] **Step 4: 커밋 메시지(안) 제시 — 커밋은 하지 않는다**

```
# [feat] - 기록에서 스테이지별 승리 수를 확인

- 기록 화면에 스테이지 1~5 리스트 추가 — 각 스테이지 영웅 초상·위협도·승리 수·판수·승률·최단 기록
- 아직 못 깬 스테이지는 회색 실루엣 + 해금 조건으로 다음 목표를 표시
- 스테이지별 기록은 이번 업데이트부터 집계 — 기존 전적 총계는 그대로 유지
```

---

## Self-Review

**Spec 커버리지**

| Spec 항목 | 태스크 |
|---|---|
| §4 데이터 (StageRecords / Version 3 / CopyFrom) | Task 1 |
| §5 집계 | Task 2 |
| §6.1 프리팹 | Task 5 |
| §6.2 코드 3-class | Task 3(Popup) · Task 4(ScrollView·Cell) |
| §6.3 행 구성 규칙 | Task 3 |
| §6.4 초상 재사용 | Task 3(`_knightPortrait`) · Task 5(배선) |
| §6.5 순수 함수 분리 | Task 3 |
| §7 테스트 5종 | Task 1(3종) · Task 3(2종) |
| §8 리스크 — CopyFrom 라운드트립 | Task 1 |
| §8 리스크 — origin Cell 컴포넌트 제거 | Task 5 Step 2 |

**타입 일관성 점검** — `RecordsStageCellData` 필드명이 Task 3 정의와 Task 4 `Bind` 사용처에서 일치(`StageText`/`ThreatText`/`WinText`/`RunRateText`/`BestText`/`LockHintText`/`IsLocked`/`IsSelected`/`Portrait`/`PortraitTint`/`Stage`). `RecordStageRun(int, bool, float)` 시그니처가 Task 1 정의와 Task 2 호출에서 일치. `BuildCellData(MetaProfile, HeroStageVariantConfig, Sprite)` 가 Task 3 정의와 Task 3 `Rebuild()` 호출에서 일치.

**플레이스홀더 스캔** — TBD/TODO 없음. 모든 코드 스텝에 실제 코드 포함.
