# 빌드 패널 구현 계획서

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어가 이번 런에 픽한 카드를 HUD에 패시브/액티브 분리 아이콘 스트립으로 항상 표시한다.

**Architecture:** `CardData`에 아이콘 스프라이트 필드를 추가하고 카드 빌더가 `ECardId` 이름의 PNG를 자동 배정한다. `BattleViewModel`이 픽을 추적·집계(×N)하고, HUD 하위 `BuildPanel`이 `CHMPool`로 셀(`BuildIconCell`)을 스폰해 표시한다. UI 프리팹은 `LairUIPrefabBuilder` 절차적 빌드를 확장한다.

**Tech Stack:** Unity 6 (6000.0.68f1) / C# / ScriptableObject / MVVM / NUnit (EditMode) / `ChvjUnityInfra` (CHMPool·CHText·CHButton)

**설계서:** `docs/superpowers/specs/2026-05-21-build-panel-design.md`

**룰 주의 (CLAUDE.md):**
- Rule 01 — `git commit` 직접 실행 금지. 마일스톤 끝에서 **관련 파일 `git add` + 커밋 메시지(안)** 까지만.
- Rule 02 — 모든 신규 단일 라인 주석은 `//#`.

---

## 파일 구조

| 파일 | 책임 | MS |
|---|---|---|
| `Assets/_Lair/Scripts/Card/CardData.cs` | `_icon` 스프라이트 필드 추가 | M1 수정 |
| `Assets/_Lair/Scripts/UI/BattleViewModel.cs` | 픽 추적·집계 (`BuildEntry`/`AddPick`/`Build`/`OnBuildChanged`) | M1 수정 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | `OnPicked` 에서 `_vm.AddPick` 호출 | M1 수정 |
| `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` | `ECardId` 이름 PNG 를 `_icon` 으로 자동 배정 | M1 수정 |
| `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelTests.cs` | `AddPick` 검증 테스트 추가 | M1 수정 |
| `Assets/_Lair/Scripts/UI/CardView.cs` | `CategoryColor` 를 `public static` 으로 (재사용) | M2 수정 |
| `Assets/_Lair/Scripts/UI/BuildIconCell.cs` | 카드 1개 셀 — 아이콘+프레임+×N 배지 | M2 신규 |
| `Assets/_Lair/Scripts/UI/BuildPanel.cs` | 픽 목록 → 셀 정합, 상세 토글 | M2 신규 |
| `Assets/_Lair/Scripts/UI/BattleHud.cs` | `BuildPanel` 바인딩 | M2 수정 |
| `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` | `BuildIconCell.prefab` 생성 + `BattleHud` 스트립 확장 | M2 수정 |

**테스트 실행:** Unity Test Runner — `editor_execute_menu` 로 `Lair/Tests/Run EditMode Tests` 실행 후 `Library/lair-test-result.json` 의 `"done": true` 폴링. 코드 수정 후 재컴파일 완료 대기 필수. 신규 `.cs` 는 `editor_refresh_assets` 로 임포트.

---

## 마일스톤 M1 — 데이터 계층

### Task 1: `CardData._icon` 필드

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/CardData.cs`

- [ ] **Step 1: `_icon` 필드 + 프로퍼티 추가**

`CardData` 클래스의 `_description` 필드 다음 줄에 `_icon` 필드를 추가하고, `Description` 프로퍼티 다음에 `Icon` 프로퍼티를 추가한다. 결과 전체:

```csharp
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 데이터 정의 — Effect 는 SerializeReference 로 polymorphic 직렬화.
    [CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private ECardId _id;
        [SerializeField] private ECardCategory _category;
        [SerializeField] private string _displayName;
        [TextArea] [SerializeField] private string _description;
        //# 빌드 패널 아이콘 — LairCardPrefabBuilder 가 ECardId 이름 PNG 로 배정. 없으면 null.
        [SerializeField] private Sprite _icon;
        [SerializeReference] private ICardEffect _effect;

        public ECardId Id => _id;
        public ECardCategory Category => _category;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ICardEffect Effect => _effect;
    }
}
```

- [ ] **Step 2: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0. 기존 25 카드 .asset 은 `_icon` 미설정(null) 상태로 유지 — Task 4 의 빌더 실행 시 배정됨.

---

### Task 2: `BattleViewModel` 픽 추적

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BattleViewModel.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelTests.cs`

- [ ] **Step 1: `BattleViewModelTests.cs` 에 `AddPick` 테스트 추가**

`BattleViewModelTests` 클래스 안에 메서드 추가:

```csharp
        [Test]
        public void AddPick_중복_픽은_Count_누적_분류_구분()
        {
            var vm = new BattleViewModel(new BattleStateModel());
            var cardA = Lair.Tests.Helpers.FakeCardData.Create(ECardId.SlimeHpBoost);
            var cardB = Lair.Tests.Helpers.FakeCardData.Create(ECardId.Frenzy);
            int changed = 0;
            vm.OnBuildChanged += () => changed++;

            vm.AddPick(cardA, isPassive: true);
            vm.AddPick(cardA, isPassive: true);   //# 같은 카드 재픽
            vm.AddPick(cardB, isPassive: false);

            Assert.AreEqual(2, vm.Build.Count, "고유 카드 2종");
            Assert.AreEqual(2, vm.Build[0].Count, "cardA 2회 누적");
            Assert.IsTrue(vm.Build[0].IsPassive);
            Assert.AreEqual(1, vm.Build[1].Count, "cardB 1회");
            Assert.IsFalse(vm.Build[1].IsPassive);
            Assert.AreEqual(3, changed, "AddPick 마다 OnBuildChanged 발행");
        }
```

`BattleViewModelTests.cs` 상단 `using` 에 `using Lair.Data;` 가 없으면 추가한다 (`ECardId` 용 — 보통 이미 있음).

- [ ] **Step 2: Test Runner 실행 — 컴파일 실패 확인**

Run: `Lair/Tests/Run EditMode Tests`
Expected: 테스트 어셈블리 컴파일 실패 — `BattleViewModel` 에 `AddPick`/`Build`/`OnBuildChanged` 없음.

- [ ] **Step 3: `BattleViewModel.cs` 전체를 다음으로 교체**

```csharp
using System;
using System.Collections.Generic;
using Lair.Card;
using Lair.Data;

namespace Lair.UI
{
    //# Model 가공 + 이벤트 노출. View 를 모름.
    //# BattleResult 는 Lair.Data 의 공용 enum (Rule 09).
    public class BattleViewModel
    {
        //# 빌드 패널 1개 항목 — 카드 + 패시브 여부 + 중복 픽 횟수.
        public class BuildEntry
        {
            public CardData Card;
            public bool IsPassive;
            public int Count;
        }

        private readonly BattleStateModel _model;
        private readonly List<BuildEntry> _build = new();

        public event Action<float, float> OnTimerChanged;
        public event Action<float> OnHeroHpRatioChanged;
        public event Action<BattleResult> OnBattleEnded;
        public event Action OnBuildChanged;

        public BattleViewModel(BattleStateModel model)
        {
            _model = model;
        }

        public void UpdateTimer(float elapsed)
        {
            _model.ElapsedSeconds = elapsed;
            OnTimerChanged?.Invoke(elapsed, _model.TotalSeconds);
        }

        public void UpdateHeroHp(int current, int max)
        {
            _model.HeroHp = current;
            _model.HeroMaxHp = max;
            OnHeroHpRatioChanged?.Invoke(max > 0 ? (float)current / max : 0f);
        }

        public void EndBattle(BattleResult result)
        {
            _model.Result = result;
            OnBattleEnded?.Invoke(result);
        }

        //# 카드 픽 누적 — 같은 카드면 Count++, 아니면 신규 엔트리. 이후 OnBuildChanged.
        public void AddPick(CardData card, bool isPassive)
        {
            if (card == null) return;
            foreach (var e in _build)
            {
                if (e.Card == card)
                {
                    e.Count++;
                    OnBuildChanged?.Invoke();
                    return;
                }
            }
            _build.Add(new BuildEntry { Card = card, IsPassive = isPassive, Count = 1 });
            OnBuildChanged?.Invoke();
        }

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        public BattleResult Result  => _model.Result;
        public IReadOnlyList<BuildEntry> Build => _build;
    }
}
```

- [ ] **Step 4: Test Runner EditMode 실행**

Run: `Lair/Tests/Run EditMode Tests`
Expected: 전체 PASS — 신규 `AddPick_중복_픽은_Count_누적_분류_구분` 포함, 기존 테스트 무회귀.

---

### Task 3: `BattleController` 픽 연동

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: `TryProcessNext` 의 `OnPicked` 람다에 `AddPick` 추가**

`TryProcessNext` 안의 `OnPicked` 콜백을 다음으로 교체 (기존 C-M3 의 `RecordPick` 옆에 한 줄 추가):

```csharp
                    OnPicked = card =>
                    {
                        //# Slice C — 픽 기록
                        if (card != null)
                        {
                            _recorder.RecordPick(card.Id);
                            //# 빌드 패널 — VM 에 픽 누적
                            _vm.AddPick(card, entry.SourceType == TriggerQueue.Source.Passive);
                        }
                        if (card?.Effect != null && _ctx != null) card.Effect.Apply(_ctx);
                        tcs.TrySetResult(true);
                    }
```

`entry` 는 `while (_queue.TryDequeue(out var entry))` 의 루프 변수 — `await tcs.Task` 로 픽 해소까지 반복이 진행되지 않으므로 클로저 캡처 안전. `_vm` 은 `BattleController` 필드.

- [ ] **Step 2: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 4: 카드 빌더 아이콘 자동 할당

**Files:**
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: `IconDir` 상수 추가**

`LairCardPrefabBuilder` 의 기존 상수 블록(`CardDir`/`PoolDir` 등) 에 추가:

```csharp
        public const string IconDir = "Assets/_Lair/Art/Sprites/CardIcons";
```

- [ ] **Step 2: `RebuildAllCards` 에서 `IconDir` 디렉터리 보장**

`RebuildAllCards` 메서드의 기존 `EnsureDir(CardDir); EnsureDir(PoolDir);` 줄 다음에 추가:

```csharp
            EnsureDir(IconDir);
```

- [ ] **Step 3: `LoadCardIcon` 헬퍼 추가**

`LairCardPrefabBuilder` 클래스 안, `EnsureDir` 메서드 근처에 추가:

```csharp
        //# ECardId 이름의 PNG 를 Sprite 로 로드. 미존재 시 null.
        //# PNG 의 textureType=Sprite / spriteImportMode=Single 임포트 설정을 보정.
        private static Sprite LoadCardIcon(ECardId id)
        {
            string path = $"{IconDir}/{id}.png";
            if (File.Exists(path) == false) return null;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite
                                || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
```

- [ ] **Step 4: `BuildCardsAndPool` 카드 루프에서 `_icon` 배정**

`BuildCardsAndPool` 의 `foreach (var spec in specs)` 루프에서 `_description` 설정 줄 다음에 한 줄 추가:

```csharp
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;
                //# 빌드 패널 아이콘 — ECardId 이름 PNG 자동 배정 (없으면 null). _effect 와 달리 매번 재설정.
                so.FindProperty("_icon").objectReferenceValue = LoadCardIcon(spec.Id);
```

- [ ] **Step 5: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0. (`File`/`AssetImporter`/`TextureImporter` 는 기존 `using System.IO;` / `using UnityEditor;` 로 해결됨.)

- [ ] **Step 6: `B3 - Rebuild All Cards` 실행 — `_icon` 배정 확인**

Run: `Lair/Setup/B3 - Rebuild All Cards`
Expected: 25 카드 갱신 로그, 에러 0. `Art/Sprites/CardIcons/` 폴더 생성됨. PNG 가 없으므로 모든 카드 `_icon` 은 null (정상 — 런타임 폴백). 카드 .asset 의 `_effect` 튜닝값은 비파괴 보존.

---

### Task 5: M1 검증 + 스테이징

- [ ] **Step 1: EditMode 전체 테스트**

Run: `Lair/Tests/Run EditMode Tests`
Expected: 전체 PASS.

- [ ] **Step 2: 스테이징 + 커밋 메시지(안) (Rule 01)**

`git add` 대상:
```
Assets/_Lair/Scripts/Card/CardData.cs
Assets/_Lair/Scripts/UI/BattleViewModel.cs
Assets/_Lair/Scripts/Battle/BattleController.cs
Assets/_Lair/Editor/LairCardPrefabBuilder.cs
Assets/_Lair/Tests/EditMode/Battle/BattleViewModelTests.cs
Assets/_Lair/Art/Cards/Items/*.asset   (rebuild 로 _icon 필드 추가 직렬화된 25장)
```

커밋 메시지(안):
```
# [feat] - 빌드 패널 M1 데이터 계층 (카드 아이콘 + 픽 추적)

- CardData._icon 추가, 빌더가 ECardId 이름 PNG 자동 배정
- BattleViewModel 픽 추적 (AddPick/Build/OnBuildChanged)
```

`git commit` 은 사용자가 직접 — 직접 실행하지 않는다.

---

## 마일스톤 M2 — UI 계층

### Task 6: `CardView.CategoryColor` 공개

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/CardView.cs`

- [ ] **Step 1: `CategoryColor` 를 `public static` 으로 변경**

`CardView.cs` 의 `CategoryColor` 메서드 시그니처를 `private static` → `public static` 로 변경한다. 메서드 본문은 그대로. 결과:

```csharp
        //# 카테고리 색 — CardView 와 BuildIconCell 이 공유 (Rule 03 — 색 매핑 단일 출처).
        public static Color CategoryColor(ECardCategory c) => c switch
        {
            ECardCategory.Enhance     => new Color(0.13f, 0.77f, 0.37f),
            ECardCategory.Spawn       => new Color(0.23f, 0.51f, 0.96f),
            ECardCategory.Replace     => new Color(0.96f, 0.62f, 0.23f),
            ECardCategory.Environment => new Color(0.66f, 0.33f, 0.96f),
            _                         => Color.gray,
        };
```

- [ ] **Step 2: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 7: `BuildIconCell` 스크립트

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BuildIconCell.cs`

- [ ] **Step 1: `BuildIconCell.cs` 작성**

```csharp
using System;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 빌드 패널의 카드 1개 셀 — 아이콘 + 카테고리 색 프레임 + ×N 배지. CHMPool 로 스폰 (Rule 12).
    public class BuildIconCell : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHButton _button;

        //# 풀 재사용 시 상태 초기화 (Rule 12).
        private void OnEnable()
        {
            if (_countText != null) _countText.gameObject.SetActive(false);
            if (_iconImage != null) _iconImage.sprite = null;
        }

        //# 카드 바인딩 — 프레임 색·아이콘·클릭 콜백 설정.
        public void Bind(CardData card, Action onClick)
        {
            if (card == null) return;
            if (_frameImage != null)
                _frameImage.color = CardView.CategoryColor(card.Category);
            if (_iconImage != null)
            {
                _iconImage.sprite = card.Icon;
                //# 아이콘 누락 시 비활성 — 프레임 색이 폴백.
                _iconImage.enabled = card.Icon != null;
            }
            if (_button != null && onClick != null)
                _button.OnClick(onClick);
        }

        //# ×N 배지 — N >= 2 일 때만 표시.
        public void SetCount(int count)
        {
            if (_countText == null) return;
            bool show = count >= 2;
            _countText.gameObject.SetActive(show);
            if (show) _countText.SetText($"×{count}");
        }
    }
}
```

- [ ] **Step 2: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 8: `BuildPanel` 스크립트

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BuildPanel.cs`

- [ ] **Step 1: `BuildPanel.cs` 작성**

```csharp
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    public class BuildPanel : MonoBehaviour
    {
        [SerializeField] private Transform _passiveContainer;
        [SerializeField] private Transform _activeContainer;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _detailRoot;
        [SerializeField] private CHText _detailName;
        [SerializeField] private CHText _detailDesc;

        private BattleViewModel _vm;
        private readonly Dictionary<CardData, BuildIconCell> _cells = new();
        private CardData _detailShown;

        //# BattleHud.Bind 가 호출 — VM 구독 + 초기 동기화.
        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            if (_detailRoot != null) _detailRoot.SetActive(false);
            vm.OnBuildChanged += Refresh;
            Refresh();
        }

        //# BattleHud.closeDisposable 가 호출 — 구독 해제.
        public void Unbind()
        {
            if (_vm != null) _vm.OnBuildChanged -= Refresh;
        }

        //# vm.Build 재조회 후 셀 정합 — 신규 카드는 셀 생성, 기존은 카운트 갱신.
        private void Refresh()
        {
            if (_vm == null) return;
            foreach (var entry in _vm.Build)
            {
                if (_cells.TryGetValue(entry.Card, out var cell) == false)
                {
                    var parent = entry.IsPassive ? _passiveContainer : _activeContainer;
                    var poolable = CHMPool.Instance.Pop(_cellPrefab, parent);
                    cell = poolable.GetComponent<BuildIconCell>();
                    var captured = entry.Card;
                    cell.Bind(captured, () => ShowDetail(captured));
                    _cells[entry.Card] = cell;
                }
                cell.SetCount(entry.Count);
            }
        }

        //# 셀 클릭 — 같은 카드 재클릭 시 닫힘, 아니면 해당 카드 상세 표시.
        private void ShowDetail(CardData card)
        {
            if (_detailRoot == null || card == null) return;
            if (_detailShown == card && _detailRoot.activeSelf)
            {
                _detailRoot.SetActive(false);
                _detailShown = null;
                return;
            }
            _detailShown = card;
            if (_detailName != null) _detailName.SetText(card.DisplayName);
            if (_detailDesc != null) _detailDesc.SetText(card.Description);
            _detailRoot.SetActive(true);
        }
    }
}
```

- [ ] **Step 2: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 9: `BattleHud` 연동

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BattleHud.cs`

- [ ] **Step 1: `_buildPanel` 필드 추가**

`BattleHud` 의 `[SerializeField]` 영역에 추가 (`_heroHpFill` 다음 줄):

```csharp
        [SerializeField] private BuildPanel _buildPanel;
```

- [ ] **Step 2: `Bind` 에서 `BuildPanel` 바인딩**

`BattleHud.Bind` 메서드의 `//# 초기 동기화` 주석 줄 바로 앞에 추가:

```csharp
            //# 빌드 패널 바인딩 (Close 시 자동 해제)
            if (_buildPanel != null)
            {
                _buildPanel.Bind(vm);
                closeDisposable.Add(() => _buildPanel.Unbind());
            }

```

- [ ] **Step 3: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 10: `LairUIPrefabBuilder` 확장

**Files:**
- Modify: `Assets/_Lair/Editor/LairUIPrefabBuilder.cs`

- [ ] **Step 1: `BuildAllUIPrefabs` 에 셀 빌드 추가**

`BuildAllUIPrefabs` 의 `BuildBattleHud(settings, group);` 줄 **앞**에 `BuildBuildIconCell();` 호출을 추가한다 (셀 프리팹이 먼저 존재해야 `BuildBattleHud` 가 참조 가능):

```csharp
            BuildBuildIconCell();
            BuildBattleHud(settings, group);
            BuildResultPopup(settings, group);
            BuildCardSelectionPopup(settings, group);   //# B1 추가
```

- [ ] **Step 2: `BuildBuildIconCell` 메서드 추가**

`LairUIPrefabBuilder` 클래스 안, `BuildBattleHud` 메서드 **앞**에 추가:

```csharp
        //# ---------- BuildIconCell 프리팹 ----------
        //# CHMPool 로 스폰되는 빌드 패널 셀. Addressable 등록 X — BuildPanel 이 직접 참조.
        private static void BuildBuildIconCell()
        {
            const string PrefabName = "BuildIconCell";

            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(72f, 72f);

            //# Frame — 카테고리 색 (런타임에 BuildIconCell 이 변경), full stretch
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)frameGo.transform);
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.sprite = GetUISprite();
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.gray;

            //# Icon — frame 안쪽 약간 작게
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(root.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f);
            iconRt.offsetMax = new Vector2(-6f, -6f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;

            //# CountText — 우하단 ×N 배지
            var countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(root.transform, false);
            var countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 0f);
            countRt.pivot     = new Vector2(1f, 0f);
            countRt.anchoredPosition = new Vector2(-2f, 2f);
            countRt.sizeDelta = new Vector2(44f, 30f);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 24f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.BottomRight;
            countTmp.color = Color.white;
            var countText = countGo.AddComponent<CHText>();

            //# Button + CHButton — 셀 전체 클릭 (Rule 11)
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = frameImg;
            var chBtn = root.AddComponent<CHButton>();

            //# BuildIconCell 컴포넌트 + 필드 주입
            var cell = root.AddComponent<Lair.UI.BuildIconCell>();
            var so = new SerializedObject(cell);
            SetObjectField(so, "_iconImage",  iconImg);
            SetObjectField(so, "_frameImage", frameImg);
            SetObjectField(so, "_countText",  countText);
            SetObjectField(so, "_button",     chBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            //# 프리팹 저장 (Addressable 등록 안 함)
            var prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[LairUIPrefabBuilder] {PrefabName} 빌드 완료");
        }

        //# 빌드 패널의 한 섹션(패시브/액티브) — 라벨 + 가로 레이아웃 컨테이너. 컨테이너 Transform 반환.
        private static Transform BuildBuildSection(Transform parent, string name, string label, bool left)
        {
            var sectionGo = new GameObject(name, typeof(RectTransform));
            sectionGo.transform.SetParent(parent, false);
            var rt = (RectTransform)sectionGo.transform;
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.offsetMin = new Vector2(left ? 0f : 10f, 0f);
            rt.offsetMax = new Vector2(left ? -10f : 0f, 0f);

            //# 섹션 라벨
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(sectionGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot     = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(0f, 28f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.font = TMP_Settings.defaultFontAsset;
            labelTmp.fontSize = 20f;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = Color.white;

            //# 아이콘 컨테이너 — 가로 레이아웃
            var containerGo = new GameObject("Container", typeof(RectTransform));
            containerGo.transform.SetParent(sectionGo.transform, false);
            var containerRt = (RectTransform)containerGo.transform;
            containerRt.anchorMin = new Vector2(0f, 0f);
            containerRt.anchorMax = new Vector2(1f, 1f);
            containerRt.offsetMin = new Vector2(0f, 0f);
            containerRt.offsetMax = new Vector2(0f, -32f);
            var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.LowerLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            return containerGo.transform;
        }
```

- [ ] **Step 3: `BuildBattleHud` 에 빌드 패널 추가**

`BuildBattleHud` 메서드에서 `//# SerializeField 주입` 주석 줄 **앞**에 다음 블록을 삽입한다:

```csharp
            //# ----- 빌드 패널 (화면 하단) -----
            var buildPanelGo = new GameObject("BuildPanel", typeof(RectTransform));
            buildPanelGo.transform.SetParent(root.transform, false);
            var bpRt = (RectTransform)buildPanelGo.transform;
            bpRt.anchorMin = new Vector2(0f, 0f);
            bpRt.anchorMax = new Vector2(1f, 0f);
            bpRt.pivot     = new Vector2(0.5f, 0f);
            bpRt.anchoredPosition = new Vector2(0f, 16f);
            bpRt.sizeDelta = new Vector2(-40f, 150f);
            var buildPanel = buildPanelGo.AddComponent<Lair.UI.BuildPanel>();

            //# 패시브/액티브 섹션
            var passiveContainer = BuildBuildSection(buildPanelGo.transform, "PassiveSection", "패시브", left: true);
            var activeContainer  = BuildBuildSection(buildPanelGo.transform, "ActiveSection",  "액티브", left: false);

            //# 상세 패널 (빌드 패널 위, 기본 숨김)
            var detailGo = new GameObject("DetailPanel", typeof(RectTransform));
            detailGo.transform.SetParent(root.transform, false);
            var detailRt = (RectTransform)detailGo.transform;
            detailRt.anchorMin = new Vector2(0.5f, 0f);
            detailRt.anchorMax = new Vector2(0.5f, 0f);
            detailRt.pivot     = new Vector2(0.5f, 0f);
            detailRt.anchoredPosition = new Vector2(0f, 180f);
            detailRt.sizeDelta = new Vector2(520f, 160f);
            var detailBg = detailGo.AddComponent<Image>();
            detailBg.sprite = GetUISprite();
            detailBg.type = Image.Type.Sliced;
            detailBg.color = new Color(0f, 0f, 0f, 0.85f);

            var detailNameGo = new GameObject("DetailName", typeof(RectTransform));
            detailNameGo.transform.SetParent(detailGo.transform, false);
            var dnRt = (RectTransform)detailNameGo.transform;
            dnRt.anchorMin = new Vector2(0f, 1f);
            dnRt.anchorMax = new Vector2(1f, 1f);
            dnRt.pivot     = new Vector2(0.5f, 1f);
            dnRt.anchoredPosition = new Vector2(0f, -10f);
            dnRt.sizeDelta = new Vector2(-20f, 44f);
            var dnTmp = detailNameGo.AddComponent<TextMeshProUGUI>();
            dnTmp.text = "Name";
            dnTmp.font = TMP_Settings.defaultFontAsset;
            dnTmp.fontSize = 30f;
            dnTmp.alignment = TextAlignmentOptions.Center;
            dnTmp.color = Color.white;
            var detailName = detailNameGo.AddComponent<CHText>();

            var detailDescGo = new GameObject("DetailDesc", typeof(RectTransform));
            detailDescGo.transform.SetParent(detailGo.transform, false);
            var ddRt = (RectTransform)detailDescGo.transform;
            ddRt.anchorMin = new Vector2(0f, 0f);
            ddRt.anchorMax = new Vector2(1f, 1f);
            ddRt.offsetMin = new Vector2(16f, 16f);
            ddRt.offsetMax = new Vector2(-16f, -54f);
            var ddTmp = detailDescGo.AddComponent<TextMeshProUGUI>();
            ddTmp.text = "Description";
            ddTmp.font = TMP_Settings.defaultFontAsset;
            ddTmp.fontSize = 22f;
            ddTmp.alignment = TextAlignmentOptions.Top;
            ddTmp.color = Color.white;
            var detailDesc = detailDescGo.AddComponent<CHText>();

            //# 셀 프리팹 로드 (Step 1 의 BuildBuildIconCell 이 먼저 생성함)
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BuildIconCell.prefab");

            //# BuildPanel 필드 주입
            var bpSo = new SerializedObject(buildPanel);
            SetObjectField(bpSo, "_passiveContainer", passiveContainer);
            SetObjectField(bpSo, "_activeContainer",  activeContainer);
            SetObjectField(bpSo, "_cellPrefab",       cellPrefab);
            SetObjectField(bpSo, "_detailRoot",       detailGo);
            SetObjectField(bpSo, "_detailName",       detailName);
            SetObjectField(bpSo, "_detailDesc",       detailDesc);
            bpSo.ApplyModifiedPropertiesWithoutUndo();
            detailGo.SetActive(false);

```

- [ ] **Step 4: `BuildBattleHud` 의 `_buildPanel` 주입**

`BuildBattleHud` 의 기존 `//# SerializeField 주입` 블록에 `_buildPanel` 주입을 한 줄 추가:

```csharp
            //# SerializeField 주입 — _timerText (CHText), _heroHpFill
            var so = new SerializedObject(hud);
            SetObjectField(so, "_timerText",  timerText);    //# CHText 컴포넌트
            SetObjectField(so, "_heroHpFill", hpFillImg);
            SetObjectField(so, "_buildPanel", buildPanel);
            so.ApplyModifiedPropertiesWithoutUndo();
```

- [ ] **Step 5: 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

- [ ] **Step 6: UI 프리팹 재빌드**

Run: `Lair/Setup/M4 - Build UI Prefabs`
Expected: `BuildIconCell` + `BattleHud` + `ResultPopup` + `CardSelectionPopup` 빌드 로그, 에러 0. `Art/UI/BuildIconCell.prefab` 생성, `BattleHud.prefab` 에 `BuildPanel`/섹션/상세 패널 포함.

---

### Task 11: M2 검증 + 스테이징

- [ ] **Step 1: EditMode + PlayMode 전체 테스트**

Run: `Lair/Tests/Run EditMode Tests` → 전체 PASS.
Run: `Lair/Tests/Run PlayMode Tests` → `BattleSmokeTest`/`CardFlowSmokeTest` 2/2 PASS (BuildPanel 추가 후 씬 로드·HUD 무회귀).

- [ ] **Step 2: 수동 검증 (사용자 — Play)**

Play 로 한 판: 카드 픽 시 패시브/액티브 섹션에 아이콘 누적 / 같은 카드 재픽 시 ×N 배지 / 아이콘 클릭 시 이름·설명 상세 / 아이콘 PNG 없으므로 카테고리 색 프레임 폴백.

- [ ] **Step 3: 스테이징 + 커밋 메시지(안) (Rule 01)**

`git add` 대상:
```
Assets/_Lair/Scripts/UI/CardView.cs
Assets/_Lair/Scripts/UI/BuildIconCell.cs
Assets/_Lair/Scripts/UI/BuildIconCell.cs.meta
Assets/_Lair/Scripts/UI/BuildPanel.cs
Assets/_Lair/Scripts/UI/BuildPanel.cs.meta
Assets/_Lair/Scripts/UI/BattleHud.cs
Assets/_Lair/Editor/LairUIPrefabBuilder.cs
Assets/_Lair/Art/UI/BuildIconCell.prefab
Assets/_Lair/Art/UI/BuildIconCell.prefab.meta
Assets/_Lair/Art/UI/BattleHud.prefab
```

커밋 메시지(안):
```
# [feat] - 빌드 패널 M2 UI 계층 (아이콘 스트립 + 상세)

- BuildIconCell/BuildPanel 신규 — 픽 카드를 패시브/액티브 아이콘으로 표시
- BattleHud 에 빌드 스트립·상세 패널 통합, ×N 배지·클릭 상세·아이콘 폴백
```

---

## 완료 기준 (설계서 §7 대응)

- [ ] 패시브/액티브 픽이 분리 섹션에 아이콘으로 누적
- [ ] 같은 카드 중복 픽 시 ×N 배지
- [ ] 아이콘 클릭 시 이름+설명 상세
- [ ] 아이콘 스프라이트 누락 시 카테고리 색 폴백
- [ ] 빌더 rebuild 가 `<ECardId>.png` 자동 배정
- [ ] EditMode + PlayMode 테스트 전부 PASS
