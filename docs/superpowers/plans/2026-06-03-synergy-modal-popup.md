# 시너지 효과 모달 팝업 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **단, 본 프로젝트는 `start-develop-auto` 파이프라인(game-designer → … → test-engineer) 으로 실행될 수도 있다.** 그 경우 본 plan 은 game-designer·gameplay-programmer·test-engineer 의 공통 입력으로 쓰인다.

**Goal:** 좌측 `BuildSynergyPanel` 클릭 시 현재 적용된(임계 도달) 시너지 효과 목록을 보여주는 모달 팝업(`SynergyModalPopup`)을 `BuildModalPopup` 패턴 그대로 추가한다.

**Architecture:** 읽기 전용 표시 UI. `BattleViewModel.GetBuildCount(axis)` 로 4축 카운트를 읽어 임계(3/5/7) 도달 Tier 만 헤더+효과 행으로 평탄화 → `CHPoolingScrollView` 로 표시. 시너지 발동 로직은 무변경.

**Tech Stack:** Unity 6 / C# / MVVM / ChvjPackage(CHMUI·CHPoolingScrollView·CHButton·CHText) / Unity Test Framework(NUnit, EditMode).

**제약:**
- Rule 01 — `git commit` 직접 실행 금지. 각 Task 의 "Commit" 단계는 **`git add`(스테이징) + 한글 커밋 메시지(안)** 까지만. 실제 커밋은 사용자.
- Rule 02 — `var`/`!`/일반주석 금지, `//#` 주석, 명시적 타입, 가드절.
- Rule 03 — `CHText`/`CHButton`, `CHMUI.ShowUI`, BuildModalPopup 패턴(코드 동적 GameObject 생성 금지), Enum 키=파일명 일치.
- 테스트 메서드명 한글 (`test_method_naming: korean`).

---

## File Structure

**신규**
- `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` — `UIBase` + `SynergyModalPopupArg`. 데이터 가공(`Build`) + 12개 `TierDesc` 정적 테이블 + 빈 상태/갱신/수명.
- `Assets/_Lair/Scripts/UI/SynergyModalCell.cs` — `MonoBehaviour` 한 행(Header/Effect) + `SynergyModalCellData`.
- `Assets/_Lair/Scripts/UI/SynergyModalCardPoolingScrollView.cs` — `CHPoolingScrollView<SynergyModalCell, SynergyModalCellData>`.
- `Assets/_Lair/Art/UI/SynergyModalPopup.prefab` / `SynergyModalCell.prefab`.
- `Assets/_Lair/Tests/EditMode/UI/SynergyModalPopupBuildTests.cs` — 데이터 가공 검증.

**변경**
- `Assets/_Lair/Scripts/Data/CommonEnum.cs` — `EUI.SynergyModalPopup` 추가.
- `Assets/_Lair/Scripts/UI/BuildSynergyPanel.cs` — `CHButton _rootButton` + 클릭 ShowUI + `CompositeDisposable`.

**데이터 가공 테스트 가능성**: `SynergyModalPopup.Build` 의 행 평탄화 로직을 `static` 순수 함수(`BuildRows(Func<EBuildAxis,int> countOf)`)로 분리해 EditMode 에서 VM/Prefab 없이 검증한다.

---

### Task 1: EUI 에 SynergyModalPopup 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EUI enum, `BuildModalPopup` 아래)

- [ ] **Step 1: Enum 값 추가**

`EUI` enum 의 `BuildModalPopup` 다음 줄에 추가:

```csharp
        SynergyModalPopup,     //# 시너지 패널 클릭 시 화면 중앙 모달 — 적용된 시너지 효과 목록
```

- [ ] **Step 2: 컴파일 확인**

Run: Unity 에디터 재컴파일 (UnityMCP `editor_recompile` 또는 에디터 포커스).
Expected: 컴파일 에러 없음. `EUI.SynergyModalPopup` 참조 가능.

- [ ] **Step 3: Commit(안)**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs
```
메시지(안): `# [feat] - EUI.SynergyModalPopup 추가 (시너지 효과 모달 키)`

---

### Task 2: 행 평탄화 순수 함수 + 실패 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SynergyModalCell.cs` (데이터 타입 `SynergyModalCellData` 먼저 정의)
- Create: `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` (`BuildRows` 정적 함수 + `TierDesc` 테이블 먼저, UIBase 본체는 Task 4)
- Test: `Assets/_Lair/Tests/EditMode/UI/SynergyModalPopupBuildTests.cs`

- [ ] **Step 1: 셀 데이터 타입 정의 (`SynergyModalCell.cs` 상단 — Bind 본체는 Task 5)**

```csharp
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# CHPoolingScrollView 의 TData — 한 행(축 헤더 or 티어 효과).
    public class SynergyModalCellData
    {
        public enum Kind { Header, Effect }
        public Kind   RowKind;
        public Color  AxisColor;   //# 축 색 띠 (헤더·효과 공통)
        public string Label;       //# Header: "TANK (5장)" / Effect: "Tier1  Wisp·Wraith HP ×1.3"
    }
}
```

- [ ] **Step 2: `BuildRows` 정적 함수 + `TierDesc` 테이블 (`SynergyModalPopup.cs` 신규, UIBase 미상속 임시 — Task 4 에서 합침)**

```csharp
using System;
using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    public partial class SynergyModalPopup
    {
        private static readonly int[] Thresholds = { 3, 5, 7 };

        private static readonly EBuildAxis[] AllAxes =
            { EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm };

        //# 기획서 §4.2 마스터 표. (축, 티어) → 효과 설명.
        private static readonly Dictionary<(EBuildAxis, int), string> TierDesc = new()
        {
            { (EBuildAxis.Tank,   1), "Wisp·Wraith HP ×1.3" },
            { (EBuildAxis.Tank,   2), "Wisp·Wraith Power ×1.2" },
            { (EBuildAxis.Tank,   3), "필드 캡 +6 (18→24)" },
            { (EBuildAxis.Dps,    1), "Reaper·Hex Power ×1.3" },
            { (EBuildAxis.Dps,    2), "Reaper·Hex 공속 +25%" },
            { (EBuildAxis.Dps,    3), "Reaper·Hex Range ×1.3" },
            { (EBuildAxis.Debuff, 1), "Plague 둔화 ×0.8" },
            { (EBuildAxis.Debuff, 2), "영웅 공격력 ×0.85" },
            { (EBuildAxis.Debuff, 3), "출혈 영구 — 이동 시 1s당 HP -1%" },
            { (EBuildAxis.Swarm,  1), "Phantom·Wisp 이동속도 ×1.3" },
            { (EBuildAxis.Swarm,  2), "모든 스포너 주기 ×0.85" },
            { (EBuildAxis.Swarm,  3), "모든 스포너 동시 출력 +1" },
        };

        //# 활성 티어 수 (count 가 넘은 임계 개수).
        private static int ActiveTier(int count)
        {
            int tier = 0;
            foreach (int t in Thresholds)
                if (count >= t) ++tier;
            return tier;
        }

        //# 축 카운트 조회 함수를 받아 행 리스트로 평탄화. 활성 티어 0 인 축은 스킵.
        //# 축 순서(AllAxes) × 티어 1→ActiveTier. 각 축 헤더 1행 + 효과 N행.
        public static List<SynergyModalCellData> BuildRows(Func<EBuildAxis, int> countOf)
        {
            List<SynergyModalCellData> rows = new List<SynergyModalCellData>();
            foreach (EBuildAxis axis in AllAxes)
            {
                int count = countOf(axis);
                int tiers = ActiveTier(count);
                if (tiers <= 0)
                    continue;

                Color color = BuildSynergyPanel.AxisColor[axis];
                rows.Add(new SynergyModalCellData
                {
                    RowKind   = SynergyModalCellData.Kind.Header,
                    AxisColor = color,
                    Label     = $"{BuildSynergyPanel.AxisLabel[axis]} ({count}장)",
                });
                for (int tier = 1; tier <= tiers; ++tier)
                {
                    string desc;
                    TierDesc.TryGetValue((axis, tier), out desc);
                    rows.Add(new SynergyModalCellData
                    {
                        RowKind   = SynergyModalCellData.Kind.Effect,
                        AxisColor = color,
                        Label     = $"Tier{tier}  {desc}",
                    });
                }
            }
            return rows;
        }
    }
}
```

> 비고: `SynergyModalPopup` 을 `partial` 로 선언해 Task 4 의 `UIBase` 본체와 합친다. `BuildSynergyPanel.AxisColor`/`AxisLabel` 은 이미 `public static`.

- [ ] **Step 3: 실패 테스트 작성**

`SynergyModalPopupBuildTests.cs`:

```csharp
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode.UI
{
    public class SynergyModalPopupBuildTests
    {
        private static System.Func<EBuildAxis, int> Counts(int tank, int dps, int debuff, int swarm)
        {
            Dictionary<EBuildAxis, int> map = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, tank }, { EBuildAxis.Dps, dps },
                { EBuildAxis.Debuff, debuff }, { EBuildAxis.Swarm, swarm },
            };
            return a => map[a];
        }

        [Test]
        public void 활성_티어_0개면_빈_리스트()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(2, 0, 1, 0));
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void Tank5_Dps3_헤더와_효과행_수_검증()
        {
            //# Tank 5 → 헤더1 + 효과2(Tier1,2), Dps 3 → 헤더1 + 효과1(Tier1) = 5행
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(5, 3, 0, 0));
            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.AreEqual("TANK (5장)", rows[0].Label);
            Assert.AreEqual(SynergyModalCellData.Kind.Effect, rows[1].RowKind);
            Assert.IsTrue(rows[1].Label.StartsWith("Tier1"));
            Assert.IsTrue(rows[2].Label.StartsWith("Tier2"));
            Assert.AreEqual("DPS (3장)", rows[3].Label);
        }

        [Test]
        public void Tank7_이상이면_Tier3까지_3행()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(7, 0, 0, 0));
            //# 헤더1 + 효과3
            Assert.AreEqual(4, rows.Count);
            Assert.IsTrue(rows[3].Label.StartsWith("Tier3"));
        }

        [Test]
        public void 축_순서는_Tank_Dps_Debuff_Swarm()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(3, 3, 3, 3));
            //# 각 축 헤더1+효과1 = 8행, 헤더 라벨 순서 확인
            Assert.AreEqual("TANK (3장)", rows[0].Label);
            Assert.AreEqual("DPS (3장)", rows[2].Label);
            Assert.AreEqual("DEBUFF (3장)", rows[4].Label);
            Assert.AreEqual("SWARM (3장)", rows[6].Label);
        }

        [Test]
        public void TierDesc_12개_키_전부_채워짐()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7));
            foreach (SynergyModalCellData r in rows)
                if (r.RowKind == SynergyModalCellData.Kind.Effect)
                    Assert.IsFalse(r.Label.EndsWith("  "), $"빈 설명: {r.Label}");
        }
    }
}
```

- [ ] **Step 4: 테스트 실패 확인**

Run: Unity Test Runner (EditMode) → `SynergyModalPopupBuildTests`.
Expected: 컴파일 통과 후 모두 PASS (로직을 Step 2 에서 이미 작성했으므로). 만약 `BuildSynergyPanel.AxisLabel` 접근성 에러면 `public static` 확인.

> TDD 주의: 본 Task 는 로직과 테스트를 함께 둔다. 엄격 TDD 를 원하면 Step 2 의 `BuildRows` 본문을 `return new List<…>();` 로 먼저 두고 테스트 실패 확인 후 채운다.

- [ ] **Step 5: Commit(안)**

```bash
git add Assets/_Lair/Scripts/UI/SynergyModalCell.cs Assets/_Lair/Scripts/UI/SynergyModalCell.cs.meta \
        Assets/_Lair/Scripts/UI/SynergyModalPopup.cs Assets/_Lair/Scripts/UI/SynergyModalPopup.cs.meta \
        Assets/_Lair/Tests/EditMode/UI/SynergyModalPopupBuildTests.cs Assets/_Lair/Tests/EditMode/UI/SynergyModalPopupBuildTests.cs.meta
```
메시지(안): `# [feat] - 시너지 모달 행 평탄화 로직 + EditMode 테스트`

---

### Task 3: ScrollView 컴포넌트

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SynergyModalCardPoolingScrollView.cs`

- [ ] **Step 1: 구현 (BuildModalCardPoolingScrollView 동일 골격)**

```csharp
using ChvjUnityInfra;

namespace Lair.UI
{
    //# Rule 03 §3 — CHPoolingScrollView 상속. InitItem/InitPoolingObject 만 오버라이드.
    public class SynergyModalCardPoolingScrollView
        : CHPoolingScrollView<SynergyModalCell, SynergyModalCellData>
    {
        public override void InitItem(SynergyModalCell item, SynergyModalCellData data, int index)
            => item.Bind(data);

        public override void InitPoolingObject(SynergyModalCell item) { }
    }
}
```

> 확인: `BuildModalCardPoolingScrollView.cs` 의 실제 시그니처(메서드명·제네릭 순서)를 읽고 정확히 맞춘다.

- [ ] **Step 2: 컴파일 확인**

Run: 재컴파일. Expected: 에러 없음 (`SynergyModalCell.Bind` 는 Task 5 에서 추가 — 그 전이면 일시 에러 가능, Task 5 와 묶어 컴파일).

- [ ] **Step 3: Commit(안)** — Task 5 와 함께 스테이징.

---

### Task 4: SynergyModalPopup UIBase 본체

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` (Task 2 partial 에 UIBase 본체 추가)

- [ ] **Step 1: UIArg + UIBase 본체 추가 (BuildModalPopup 패턴 그대로)**

`SynergyModalPopup.cs` 에 같은 namespace 로 추가 (파일 상단에 `SynergyModalPopupArg`, 클래스는 `partial`):

```csharp
using ChvjUnityInfra;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SynergyModalPopupArg : UIArg
    {
        public BattleViewModel ViewModel;
    }

    //# 화면 중앙 모달 — 적용된(임계 도달) 시너지 효과 목록. BuildModalPopup 패턴.
    public partial class SynergyModalPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private SynergyModalCardPoolingScrollView _scrollView;
        [SerializeField] private CHText _emptyText;

        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            if (arg is SynergyModalPopupArg ma && ma.ViewModel != null)
            {
                _vm = ma.ViewModel;
                System.Action refresh = HandleBuildChanged;
                _vm.OnBuildChanged += refresh;
                BattleViewModel vmRef = _vm;
                closeDisposable.Add(() => vmRef.OnBuildChanged -= refresh);
                closeDisposable.Add(() => _vm = null);
            }

            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);

            if (isActiveAndEnabled)
                BuildAndLayout();
        }

        private void OnEnable()
        {
            if (_vm == null)
                return;
            BuildAndLayout();
        }

        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Build();
        }

        private void HandleBuildChanged()
        {
            if (_vm == null)
                return;
            Build();
        }

        private void Build()
        {
            if (_vm == null)
                return;
            System.Collections.Generic.List<SynergyModalCellData> rows =
                BuildRows(_vm.GetBuildCount);

            if (_emptyText != null)
                _emptyText.gameObject.SetActive(rows.Count == 0);
            if (_scrollView != null)
                _scrollView.SetItemList(rows);
        }
    }
}
```

> 확인: `BuildModalPopup.cs` 의 `Close(reuse: true)` · `closeDisposable` · `UIBase.InitUI` 시그니처를 그대로 따른다. `_vm.GetBuildCount` 는 `int GetBuildCount(EBuildAxis)` — `Func<EBuildAxis,int>` 로 그대로 전달 가능.

- [ ] **Step 2: 컴파일 확인**

Run: 재컴파일. Expected: 에러 없음.

- [ ] **Step 3: Commit(안)** — Task 5 와 함께.

---

### Task 5: SynergyModalCell 본체 (Bind/리셋)

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/SynergyModalCell.cs`

- [ ] **Step 1: MonoBehaviour 본체 추가 (BuildModalCardCell 패턴)**

`SynergyModalCellData` 아래에 추가:

```csharp
    //# Rule 03 §3 — 풀 재사용 셀. Header/Effect 한 행 렌더.
    public class SynergyModalCell : MonoBehaviour
    {
        [SerializeField] private Image _axisStrip;   //# 좌측 축 색 띠
        [SerializeField] private CHText _label;       //# 헤더/효과 텍스트

        private void OnEnable()
        {
            //# 풀 재사용 리셋 — 잔여 상태 제거.
            if (_label != null)
                _label.SetText(string.Empty);
        }

        public void Bind(SynergyModalCellData data)
        {
            if (data == null)
                return;
            if (_axisStrip != null)
                _axisStrip.color = data.AxisColor;
            if (_label != null)
                _label.SetText(data.Label);
            //# 헤더는 굵게/들여쓰기 없음 — MVP 텍스트만. 시각 구분은 색 띠 + "(N장)" 표기로 충분.
        }
    }
```

> 확인: `BuildModalCardCell.cs` 의 `CHText.SetText`·`[SerializeField]` 관례를 따른다.

- [ ] **Step 2: 컴파일 + Task 2 테스트 재실행**

Run: 재컴파일 → EditMode 테스트 전체.
Expected: 컴파일 에러 없음, `SynergyModalPopupBuildTests` 전부 PASS.

- [ ] **Step 3: Commit(안)**

```bash
git add Assets/_Lair/Scripts/UI/SynergyModalCardPoolingScrollView.cs Assets/_Lair/Scripts/UI/SynergyModalCardPoolingScrollView.cs.meta \
        Assets/_Lair/Scripts/UI/SynergyModalCell.cs Assets/_Lair/Scripts/UI/SynergyModalPopup.cs
```
메시지(안): `# [feat] - SynergyModalPopup/Cell/ScrollView 본체 구현`

---

### Task 6: prefab 2종 제작 + Addressables 등록

**Files:**
- Create: `Assets/_Lair/Art/UI/SynergyModalCell.prefab`
- Create: `Assets/_Lair/Art/UI/SynergyModalPopup.prefab`

> 참고 단일 진실: `BuildModalPopup.prefab` + `BuildModalCardCell.prefab` + `BuildModalCardPoolingScrollView` 구성을 복제 후 시너지용으로 치환.

- [ ] **Step 1: SynergyModalCell.prefab**

`BuildModalCardCell.prefab` 복제 → 이름 변경. 구성: RectTransform + 좌측 `Image`(_axisStrip) + `CHText`(_label, TMP_Text 동반). `SynergyModalCell` 컴포넌트 부착, `_axisStrip`/`_label` 인스펙터 연결. LayoutElement 높이 고정(예: 44px).

- [ ] **Step 2: SynergyModalPopup.prefab**

`BuildModalPopup.prefab` 복제 → 이름 변경. 좌우 50:50 분할 제거하고 단일 세로 ScrollView 로 단순화:
- DimButton(CHButton, #000 α0.6) / CloseButton(CHButton, X) / EmptyText(CHText "아직 발동한 시너지가 없습니다").
- ScrollView → `SynergyModalCardPoolingScrollView` 컴포넌트, Viewport + Content(VerticalLayoutGroup) + origin Cell 인스턴스.
- `SynergyModalPopup` 컴포넌트의 `_dimButton`/`_closeButton`/`_scrollView`/`_emptyText` 인스펙터 연결.
- `SynergyModalCardPoolingScrollView._origin` 에 origin Cell 인스턴스 연결.

- [ ] **Step 3: Addressables 등록 (Rule 03 §2)**

`SynergyModalPopup.prefab` 의 Addressables 주소 = `SynergyModalPopup` (파일명=EUI 값명 정확히 일치), 기존 UI 와 동일 라벨(`Resource`).
Cell.prefab 은 Popup 내부 정적 참조이므로 Addressables 주소 불요(BuildModalCardCell 관례 확인 — 필요 시 동일하게).

- [ ] **Step 4: 검증**

Run: 에디터에서 `CHMUI.Instance.ShowUI(EUI.SynergyModalPopup, …)` 동작 확인 (Task 7 통합 후).

- [ ] **Step 5: Commit(안)**

```bash
git add Assets/_Lair/Art/UI/SynergyModalCell.prefab Assets/_Lair/Art/UI/SynergyModalCell.prefab.meta \
        Assets/_Lair/Art/UI/SynergyModalPopup.prefab Assets/_Lair/Art/UI/SynergyModalPopup.prefab.meta \
        Assets/AddressableAssetsData
```
메시지(안): `# [asset] - 시너지 모달 팝업/셀 prefab + Addressables 등록`

---

### Task 7: BuildSynergyPanel 클릭 트리거 연결

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BuildSynergyPanel.cs`
- Modify: `Assets/_Lair/Art/UI/BuildSynergyPanel.prefab` (루트 CHButton 추가 + 연결)

- [ ] **Step 1: 필드 + 클릭 핸들러 추가 (BuildPanel 패턴)**

`BuildSynergyPanel` 클래스에 추가:

```csharp
        //# 패널 루트 클릭 → SynergyModalPopup 호출.
        [SerializeField] private CHButton _rootButton;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
```

`Bind` 안 (`HandleBuildChanged();` 직전) 에 추가:

```csharp
            if (_rootButton != null)
            {
                _rootButton.OnClick(() =>
                {
                    if (_vm == null) return;
                    CHMUI.Instance.ShowUI(EUI.SynergyModalPopup,
                        new SynergyModalPopupArg { ViewModel = _vm });
                }, _disposable);
            }
```

`Unbind` 끝에 추가:

```csharp
            _disposable.Clear();
```

> `using` 추가 필요: `CHMUI` 는 `ChvjUnityInfra` (이미 import). `CompositeDisposable` 도 `ChvjUnityInfra`.

- [ ] **Step 2: prefab 에 CHButton 추가**

`BuildSynergyPanel.prefab` 루트(또는 클릭 영역 자식)에 `Button` + `CHButton` 추가, `BuildSynergyPanel._rootButton` 인스펙터 연결. 기존 셀 표시 동작은 유지.

- [ ] **Step 3: 컴파일 + 동작 확인**

Run: Play 모드 → 카드 3장 같은 축 픽 → 좌측 패널 클릭 → 모달에 해당 축 Tier1 효과 표시 확인. 픽 0 상태 클릭 → 빈 상태 라벨.

- [ ] **Step 4: Commit(안)**

```bash
git add Assets/_Lair/Scripts/UI/BuildSynergyPanel.cs Assets/_Lair/Art/UI/BuildSynergyPanel.prefab
```
메시지(안): `# [feat] - 시너지 패널 클릭 시 SynergyModalPopup 오픈`

---

## Self-Review

**Spec coverage:**
- spec §2 트리거 → Task 7 ✅
- spec §3 활성 티어만 표시 → Task 2 `BuildRows`(tiers<=0 skip) ✅
- spec §4 코드 정적 테이블 → Task 2 `TierDesc` 12개 ✅
- spec §5 UI 구조/3-class → Task 3·4·5·6 ✅
- spec §6 갱신/수명 → Task 4 OnBuildChanged·closeDisposable·OnEnable ✅
- spec §7 신규/변경 파일 → 전 Task 커버 ✅
- spec §9 테스트 관점 → Task 2 테스트(빈/Tank5Dps3/7+/정렬/12키) ✅

**Placeholder scan:** TBD/TODO 없음. 모든 코드 블록 실내용 포함.

**Type consistency:** `SynergyModalCellData`(RowKind·AxisColor·Label) / `BuildRows(Func<EBuildAxis,int>)` / `SynergyModalCell.Bind` / `SynergyModalCardPoolingScrollView.InitItem` — Task 간 일치. `_vm.GetBuildCount` 는 `BattleViewModel` 의 `int GetBuildCount(EBuildAxis)` 확인됨.

**미확정 (gameplay-programmer 가 코드 읽고 정합)**: `CHPoolingScrollView` 제네릭/오버라이드 시그니처, `UIBase.Close(reuse:)`·`closeDisposable` 정확한 형태, `BuildModalCardCell` 의 Addressables 등록 여부 — 모두 기존 `BuildModal*` 파일을 단일 진실로 따른다.
