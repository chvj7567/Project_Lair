# 영웅 상태 아이콘 (HP바 아래) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 영웅에 걸린 상태 8종을 HP바 아래에 대응 카드 아이콘으로 on/off 표시하고, 기존 월드 프리미티브 status 도형을 제거한다.

**Architecture:** Aura 타입 → 대표 ECardId → 카드 Sprite 로 아이콘을 해석한다(소스 무관). `HeroAuraRunner` 가 상태 시작/종료 이벤트를 발행 → `BattleController` 가 `BattleViewModel` 로 forward → `BattleHud` 가 ECardId→Sprite 해석 후 `HpBarView` 아이콘 행을 갱신. 아이콘 행은 `HpBar.prefab` 빌더(`EnsureHpBarPrefab`)에 정적 슬롯으로 작성한다.

**Tech Stack:** Unity 6 / C# / NUnit (EditMode) / ChvjPackage(CHText·CHMResource·CHMPool) / Addressables.

> **커밋 정책 (Rule 01):** 본 플랜의 "Commit" 단계는 **`git add` 스테이징까지만** 수행한다. 실제 `git commit` 은 파이프라인 마지막에 메인 오케스트레이터가 한글 메시지(안)로 제안한다. 자동 커밋 금지.

> **참조 문서:** spec `docs/superpowers/specs/2026-06-04-hero-status-icons-design.md`. game-designer 가 §2.1 aura→ECardId 대표 표를 최종 확정한 기획서를 함께 입력으로 받는다.

---

## File Structure

| 파일 | 책임 | 작업 |
|---|---|---|
| `Assets/_Lair/Scripts/Card/CommonInterface.cs` | `IStatusVisual` 마커 정의 (`ECardId IconCardId`) | Modify |
| `Assets/_Lair/Scripts/Card/Auras/*.cs` (8종) | 각 Aura 의 `IconCardId` 구현, `VisualKey/Offset` 제거 | Modify |
| `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs` | 월드 visual 제거 → 상태 시작/종료 이벤트 발행 | Modify |
| `Assets/_Lair/Scripts/Character/HpBarView.cs` | 아이콘 행 의도 API (`AddStatusIcon`/`RemoveStatusIcon`/`ClearStatusIcons`) | Modify |
| `Assets/_Lair/Scripts/UI/BattleViewModel.cs` | 상태 아이콘 이벤트 + 메서드 | Modify |
| `Assets/_Lair/Scripts/UI/BattleHud.cs` | VM 구독 → ECardId→Sprite 해석 → HpBarView | Modify |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | HeroAuraRunner 이벤트 구독·forward, PrewarmPools cleanup, 카드 아이콘 dict 제공 | Modify |
| `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` | `EnsureHpBarPrefab` reconcile + 아이콘 행 슬롯 생성 | Modify |
| `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs` | status visual 6종 생성 제거 (PoisonAura 유지) | Modify |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EVisual` status 6값 제거 (`PoisonAura` 유지) | Modify |
| `Assets/_Lair/Tests/EditMode/HpBarViewTests.cs` | 아이콘 API 테스트 추가 | Modify |
| `Assets/_Lair/Tests/EditMode/HeroAuraRunnerStatusIconTests.cs` | 이벤트 발행 테스트 | Create |

---

## Task 0 (M0): HpBar.prefab 빌더 reconcile — 수작업 상태를 빌더로 흡수

> **선행 필수.** `EnsureHpBarPrefab()` 은 매 빌드 전체 재생성이므로, 아이콘 행을 얹기 전 현재 수작업 상태를 빌더에 반영하지 않으면 다음 빌드에 소실된다. 현재 `Assets/_Lair/Art/UI/HpBar.prefab` 을 단일 진실로 삼는다.

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` (`EnsureHpBarPrefab`, 268–341)

- [ ] **Step 1: 현재 prefab YAML 재확인**

Read `Assets/_Lair/Art/UI/HpBar.prefab`. 확인 항목: Background `m_Color`, Background/Fill `m_Sprite` guid, txtHp TMP 설정(autoSize min/max, m_fontSize, alignment), txtHp RectTransform(anchoredPosition, sizeDelta), 폰트 asset guid.

- [ ] **Step 2: Background 색 반영**

`bgImg.color` 를 현재값으로 지정 (현재 prefab: `new Color(0.26415092f, 0.26415092f, 0.26415092f, 1f)`). 구현 시 Step 1 재확인값 사용.

```csharp
Image bgImg = bgGo.AddComponent<Image>();
bgImg.sprite = bgSprite != null ? bgSprite : LairUIPrefabBuilder.GetUISprite();
bgImg.type = Image.Type.Simple;
bgImg.color = new Color(0.26415092f, 0.26415092f, 0.26415092f, 1f);   //# 수작업 반영 — 어두운 트랙
```

- [ ] **Step 3: txtHp 오토사이징 + Rect inset 반영**

```csharp
tmp.enableAutoSizing = true;
tmp.fontSizeMin = 6f;
tmp.fontSizeMax = 10f;
tmp.fontSize = 6f;
//# Rect inset — full stretch 후 sizeDelta/anchoredPos 보정 (현재 prefab: sizeDelta(-20,-14), anchoredPos(0,1))
RectTransform txtRt = (RectTransform)txtGo.transform;
txtRt.sizeDelta = new Vector2(-20f, -14f);
txtRt.anchoredPosition = new Vector2(0f, 1f);
```

- [ ] **Step 4: 스프라이트/폰트 guid 일치 확인**

`HpBarBgSpritePath`/`HpBarFillSpritePath` 가 현재 prefab guid(`e318ff71...` bg, `997ac05e...` fill)와 같은 에셋을 가리키는지 확인. 다르면 빌더 경로 상수를 현재 에셋 경로로 맞춘다. 폰트가 다르면(`12e8e80f...` = NotoSansKR) `tmp.font` 를 그 asset 으로 지정.

- [ ] **Step 5: 빌더 실행 후 회귀 확인**

Unity 메뉴 `Lair > Build Character Prefabs`(또는 `EnsureHpBarPrefab` 단독 실행) 실행 → `HpBar.prefab` 재생성. 재생성된 prefab 의 Background 색·txt 설정·rect 이 Step 1 값과 동일한지 확인(시각 + YAML diff). HUD/몬스터 바 표시 깨짐 없음.

- [ ] **Step 6: Commit (stage)**

```bash
git add Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs Assets/_Lair/Art/UI/HpBar.prefab
```

---

## Task 1 (M1): IStatusVisual 마커 교체 + 8 Aura IconCardId

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Card/Auras/SlowAura.cs`, `FearAura.cs`, `WeakenAura.cs`, `TimeStopAura.cs`, `BleedAura.cs`, `MarkOfDeathAura.cs`, `HeroAttackDownAura.cs`, `EternalBleedAura.cs`

- [ ] **Step 1: IStatusVisual 마커로 교체**

`Assets/_Lair/Scripts/Card/CommonInterface.cs` 의 `IStatusVisual` 를 다음으로 교체 (기존 `EVisual VisualKey` / `Vector3 Offset` 제거):

```csharp
//# 상태 아이콘 마커 — 이 상태를 대표하는 카드(능력) ECardId 를 노출.
//# 렌더링(아이콘)은 HeroAuraRunner→ViewModel→HpBarView 가 처리.
public interface IStatusVisual
{
    ECardId IconCardId { get; }
}
```

`using Lair.Data;` 가 파일에 있는지 확인(없으면 추가).

- [ ] **Step 2: 8개 Aura 의 IconCardId 구현 + 구 멤버 제거**

각 Aura 에서 `public EVisual VisualKey => ...;` 와 `public Vector3 Offset => ...;` 를 삭제하고 `IconCardId` 로 교체. 대표 ECardId 는 기획서 §2.1 표 기준:

```csharp
//# SlowAura
public ECardId IconCardId => ECardId.Slow;
//# FearAura
public ECardId IconCardId => ECardId.Fear;
//# WeakenAura
public ECardId IconCardId => ECardId.Weaken;
//# TimeStopAura
public ECardId IconCardId => ECardId.TimeStop;
//# BleedAura
public ECardId IconCardId => ECardId.Bleed;
//# MarkOfDeathAura
public ECardId IconCardId => ECardId.MarkOfDeath;
//# HeroAttackDownAura
public ECardId IconCardId => ECardId.HeroAttackDown;
//# EternalBleedAura — 전용 카드 없음, 동일 출혈 능력 아이콘 재사용
public ECardId IconCardId => ECardId.Bleed;
```

`using Lair.Data;` 가 각 Aura 파일에 있는지 확인.

- [ ] **Step 3: 컴파일 확인**

Unity `editor_recompile` → 에러 0. (이 시점엔 `HeroAuraRunner` 가 아직 구 멤버를 참조해 빌드가 깨질 수 있음 → Task 2 와 함께 묶어 컴파일. 순차 실행 시 Task 2 까지 완료 후 recompile.)

- [ ] **Step 4: Commit (stage)**

```bash
git add Assets/_Lair/Scripts/Card/CommonInterface.cs Assets/_Lair/Scripts/Card/Auras/
```

---

## Task 2 (M1): HeroAuraRunner — 월드 visual 제거 + 상태 이벤트 발행

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs`
- Test: `Assets/_Lair/Tests/EditMode/HeroAuraRunnerStatusIconTests.cs` (Create)

- [ ] **Step 1: 실패 테스트 작성**

`Assets/_Lair/Tests/EditMode/HeroAuraRunnerStatusIconTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class HeroAuraRunnerStatusIconTests
    {
        private GameObject _heroGo;
        private HeroAuraRunner _runner;

        //# 테스트용 최소 Aura — IHeroAura + IStatusVisual.
        private class FakeStatusAura : IHeroAura, IStatusVisual
        {
            public ECardId IconCardId => ECardId.TimeStop;
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt) { }
            public void OnDetached(IHealth hero) { }
        }

        [SetUp]
        public void SetUp()
        {
            _heroGo = new GameObject("Hero");
            _heroGo.AddComponent<Health>();
            _runner = _heroGo.AddComponent<HeroAuraRunner>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_heroGo);

        [Test]
        public void Attach_상태Aura_OnStatusShown_발행()
        {
            List<ECardId> shown = new List<ECardId>();
            _runner.OnStatusShown += (key, id) => shown.Add(id);

            _runner.Attach(new FakeStatusAura(), 5f);

            Assert.AreEqual(1, shown.Count);
            Assert.AreEqual(ECardId.TimeStop, shown[0]);
        }

        [Test]
        public void OnDisable_상태Aura_OnStatusHidden_발행()
        {
            int hidden = 0;
            _runner.OnStatusHidden += _ => hidden++;
            _runner.Attach(new FakeStatusAura(), 5f);

            _heroGo.SetActive(false);   //# OnDisable 유발

            Assert.AreEqual(1, hidden);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

Run: Unity EditMode 러너 `HeroAuraRunnerStatusIconTests`.
Expected: FAIL (컴파일 에러 — `OnStatusShown`/`OnStatusHidden` 미정의).

- [ ] **Step 3: HeroAuraRunner 수정**

`Slot.Visual`(CHPoolable) 필드 + `CHMResource.Load`/`CHMPool.Pop`/`Push`/위치추적 제거. 이벤트 발행 추가:

```csharp
using System;
//# (using ChvjUnityInfra 의 CHMResource/CHMPool 미사용 시 정리)

public event Action<object, ECardId> OnStatusShown;   //# key(aura 타입), 대표 ECardId
public event Action<object> OnStatusHidden;           //# key
```

`Attach` 의 신규 슬롯 분기에서 visual Pop 대신:

```csharp
//# 상태 아이콘 — 신규 슬롯이고 IStatusVisual 이면 표시 이벤트.
if (aura is IStatusVisual sv)
    OnStatusShown?.Invoke(aura.GetType(), sv.IconCardId);
```

`Slot` 클래스에서 `Visual` 필드 제거. `Update` 의 만료 분기에서 visual Push 대신:

```csharp
if (s.Aura is IStatusVisual)
    OnStatusHidden?.Invoke(s.Aura.GetType());
```

`OnDisable` 에서 각 슬롯에 대해 `Aura.OnDetached` 후 `if (slot.Aura is IStatusVisual) OnStatusHidden?.Invoke(slot.Aura.GetType());`. 재부착(같은 타입 early-return) 경로는 이벤트 발행 없음 — 기존 dedup 로직 그대로 유지.

- [ ] **Step 4: 테스트 실행 → 통과 확인**

Run: `HeroAuraRunnerStatusIconTests` + 기존 EditMode 전체.
Expected: PASS. 기존 HeroAuraRunner 회귀 테스트도 그린(월드 visual 단언이 있었다면 해당 테스트는 제거/수정 — Task 5 에서 정리).

- [ ] **Step 5: Commit (stage)**

```bash
git add Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs Assets/_Lair/Tests/EditMode/HeroAuraRunnerStatusIconTests.cs
```

---

## Task 3 (M2): HpBarView 아이콘 행 API

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/HpBarView.cs`
- Test: `Assets/_Lair/Tests/EditMode/HpBarViewTests.cs`

- [ ] **Step 1: 실패 테스트 추가**

`HpBarViewTests.cs` SetUp 에 아이콘 행 위젯 와이어링 + 신규 테스트:

```csharp
//# SetUp 추가 — 8 슬롯 Image + 컨테이너.
private GameObject _iconRow;
private Image[] _iconSlots;

//# (SetUp 끝부분에 추가)
_iconRow = new GameObject("StatusIconRow");
_iconRow.transform.SetParent(_barGo.transform, false);
_iconSlots = new Image[8];
for (int i = 0; i < 8; i++)
{
    _iconSlots[i] = new GameObject($"Slot{i}").AddComponent<Image>();
    _iconSlots[i].transform.SetParent(_iconRow.transform, false);
    _iconSlots[i].gameObject.SetActive(false);
}
_iconRow.SetActive(false);
typeof(HpBarView).GetField("_statusIconRow", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_view, _iconRow);
typeof(HpBarView).GetField("_iconSlots", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_view, _iconSlots);
```

```csharp
[Test]
public void AddStatusIcon_슬롯활성화_스프라이트세팅_컨테이너활성()
{
    Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one * 0.5f);
    _view.AddStatusIcon("slow", sp);

    Assert.IsTrue(_iconRow.activeSelf);
    Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
    Assert.AreEqual(sp, _iconSlots[0].sprite);
}

[Test]
public void RemoveStatusIcon_마지막제거시_컨테이너비활성()
{
    Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one * 0.5f);
    _view.AddStatusIcon("slow", sp);
    _view.RemoveStatusIcon("slow");

    Assert.IsFalse(_iconSlots[0].gameObject.activeSelf);
    Assert.IsFalse(_iconRow.activeSelf);
}

[Test]
public void AddStatusIcon_같은key중복_슬롯1개만사용()
{
    Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one * 0.5f);
    _view.AddStatusIcon("slow", sp);
    _view.AddStatusIcon("slow", sp);

    Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
    Assert.IsFalse(_iconSlots[1].gameObject.activeSelf);
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

Run: `HpBarViewTests`.
Expected: FAIL (`AddStatusIcon`/`RemoveStatusIcon` 미정의).

- [ ] **Step 3: HpBarView 구현**

```csharp
[SerializeField] private GameObject _statusIconRow;   //# HorizontalLayoutGroup 컨테이너 (기본 비활성)
[SerializeField] private Image[] _iconSlots;           //# 정적 8 슬롯

private readonly Dictionary<object, int> _keyToSlot = new();

public void AddStatusIcon(object key, Sprite icon)
{
    if (key == null || _iconSlots == null) return;
    if (_keyToSlot.ContainsKey(key)) return;           //# 중복 무시
    for (int i = 0; i < _iconSlots.Length; i++)
    {
        if (_iconSlots[i] != null && _iconSlots[i].gameObject.activeSelf == false)
        {
            _iconSlots[i].sprite = icon;
            _iconSlots[i].enabled = icon != null;       //# sprite null 이면 미표시(graceful)
            _iconSlots[i].gameObject.SetActive(true);
            _keyToSlot[key] = i;
            if (_statusIconRow != null) _statusIconRow.SetActive(true);
            return;
        }
    }
}

public void RemoveStatusIcon(object key)
{
    if (key == null || _iconSlots == null) return;
    if (_keyToSlot.TryGetValue(key, out int slot) == false) return;
    if (slot >= 0 && slot < _iconSlots.Length && _iconSlots[slot] != null)
    {
        _iconSlots[slot].sprite = null;
        _iconSlots[slot].gameObject.SetActive(false);
    }
    _keyToSlot.Remove(key);
    if (_keyToSlot.Count == 0 && _statusIconRow != null) _statusIconRow.SetActive(false);
}

public void ClearStatusIcons()
{
    if (_iconSlots != null)
        foreach (Image slot in _iconSlots)
            if (slot != null) { slot.sprite = null; slot.gameObject.SetActive(false); }
    _keyToSlot.Clear();
    if (_statusIconRow != null) _statusIconRow.SetActive(false);
}
```

`using System.Collections.Generic;` 추가.

- [ ] **Step 4: 테스트 실행 → 통과 확인**

Run: `HpBarViewTests`. Expected: PASS (기존 4 + 신규 3).

- [ ] **Step 5: Commit (stage)**

```bash
git add Assets/_Lair/Scripts/Character/HpBarView.cs Assets/_Lair/Tests/EditMode/HpBarViewTests.cs
```

---

## Task 4 (M2): EnsureHpBarPrefab 아이콘 행 슬롯 생성

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` (`EnsureHpBarPrefab`)

- [ ] **Step 1: 아이콘 행 생성 코드 추가**

`EnsureHpBarPrefab` 의 `SaveAsPrefabAsset` 직전에 추가:

```csharp
//# 상태 아이콘 행 — HP Fill 아래. 기본 비활성(몬스터 바 공유 — 비어있게 유지).
const int IconSlotCount = 8;
const float IconSize = 12f;   //# 기획서 §3.1 — 8 슬롯 worst-case(8×12+7×2=110)가 HP바 폭 120 안에 들어오도록 12 확정(plan 초안 16 override)
GameObject rowGo = new GameObject("StatusIconRow", typeof(RectTransform));
rowGo.transform.SetParent(root.transform, false);
RectTransform rowRt = (RectTransform)rowGo.transform;
rowRt.anchorMin = new Vector2(0f, 0f);
rowRt.anchorMax = new Vector2(1f, 0f);
rowRt.pivot = new Vector2(0.5f, 1f);
rowRt.anchoredPosition = new Vector2(0f, -2f);          //# HP바 바로 아래
rowRt.sizeDelta = new Vector2(0f, IconSize);
HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
hlg.spacing = 2f;
hlg.childAlignment = TextAnchor.MiddleCenter;
hlg.childControlWidth = false;
hlg.childControlHeight = false;
hlg.childForceExpandWidth = false;
hlg.childForceExpandHeight = false;

Image[] iconSlots = new Image[IconSlotCount];
for (int i = 0; i < IconSlotCount; i++)
{
    GameObject slotGo = new GameObject($"Icon{i}", typeof(RectTransform));
    slotGo.transform.SetParent(rowGo.transform, false);
    RectTransform slotRt = (RectTransform)slotGo.transform;
    slotRt.sizeDelta = new Vector2(IconSize, IconSize);
    Image slotImg = slotGo.AddComponent<Image>();
    slotImg.preserveAspect = true;
    iconSlots[i] = slotImg;
    slotGo.SetActive(false);
}
rowGo.SetActive(false);

//# HpBarView 와이어링
SetPrivateField(view, "_statusIconRow", rowGo);
SetPrivateField(view, "_iconSlots", iconSlots);
```

`using UnityEngine.UI;` 가 파일에 있는지 확인(`HorizontalLayoutGroup`/`Image`).

- [ ] **Step 2: 빌더 실행**

Unity `Lair > Build Character Prefabs`(또는 EnsureHpBarPrefab) 실행 → `HpBar.prefab` 재생성.

- [ ] **Step 3: 검증 (MCP/에디터)**

`HpBar.prefab` 열기 → `StatusIconRow`(비활성) + `Icon0~7`(비활성) 존재, `HpBarView._statusIconRow`/`_iconSlots[8]` 와이어링 확인. 몬스터 prefab nest 인스턴스에 아이콘 행이 비어있음 확인.

- [ ] **Step 4: Commit (stage)**

```bash
git add Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs Assets/_Lair/Art/UI/HpBar.prefab
```

---

## Task 5 (M3): ViewModel + BattleHud + BattleController 배선

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BattleViewModel.cs`
- Modify: `Assets/_Lair/Scripts/UI/BattleHud.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: BattleViewModel 이벤트/메서드 추가**

```csharp
//# 상태 아이콘 — key(aura 타입), 대표 ECardId. View 가 ECardId→Sprite 해석.
public event Action<object, ECardId> OnStatusIconAdded;
public event Action<object> OnStatusIconRemoved;

public void AddStatusIcon(object key, ECardId iconId)
    => OnStatusIconAdded?.Invoke(key, iconId);

public void RemoveStatusIcon(object key)
    => OnStatusIconRemoved?.Invoke(key);
```

- [ ] **Step 2: BattleController — 카드 아이콘 dict + HeroAuraRunner 구독**

`BattleController` 에 `ECardId→Sprite` 해석을 위한 dict 를 카드 풀로 1회 구성(기존 전체 카드 컬렉션 재사용; 카드 SO 의 `Icon`). 영웅 셋업 시점(HeroAuraRunner 확보 후)에 구독:

```csharp
//# 카드 아이콘 조회 — BattleHud 주입용. 카드 풀 1회 스캔.
private Dictionary<ECardId, Sprite> _cardIconById;
public IReadOnlyDictionary<ECardId, Sprite> CardIconById => _cardIconById;

//# 영웅 셋업 시:
HeroAuraRunner runner = heroGo.GetComponent<HeroAuraRunner>() ?? heroGo.AddComponent<HeroAuraRunner>();
runner.OnStatusShown  += (key, id) => _vm.AddStatusIcon(key, id);
runner.OnStatusHidden += key => _vm.RemoveStatusIcon(key);
//# 영웅 풀 반환/재사용 시 구독 해제 + _vm 아이콘 클리어 (중복 누수 방지).
```

> 구현 노트: 영웅 생성/해제 지점은 `BattleController` 의 영웅 스폰 경로를 따른다(기존 `CharacterRegistry.Heroes` 등록 흐름). 구독 해제 핸들러를 필드로 보관해 정확히 `-=`.

- [ ] **Step 3: BattleHud — VM 구독 + ECardId→Sprite → HpBarView**

`BattleHud` 의 `Bind` 에 추가:

```csharp
vm.OnStatusIconAdded   += HandleStatusIconAdded;
vm.OnStatusIconRemoved += HandleStatusIconRemoved;
closeDisposable.Add(() => vm.OnStatusIconAdded   -= HandleStatusIconAdded);
closeDisposable.Add(() => vm.OnStatusIconRemoved -= HandleStatusIconRemoved);
```

```csharp
private IReadOnlyDictionary<ECardId, Sprite> _cardIcons;   //# BattleHudArg 로 주입

private void HandleStatusIconAdded(object key, ECardId iconId)
{
    if (_heroHpBar == null) return;
    Sprite icon = null;
    _cardIcons?.TryGetValue(iconId, out icon);
    _heroHpBar.AddStatusIcon(key, icon);
}

private void HandleStatusIconRemoved(object key)
{
    if (_heroHpBar != null) _heroHpBar.RemoveStatusIcon(key);
}
```

`BattleHudArg` 에 `public IReadOnlyDictionary<ECardId, Sprite> CardIcons;` 추가, `BattleController` 가 HUD 띄울 때 `CardIconById` 주입. `InitUI`/`Bind` 에서 `_cardIcons = ba.CardIcons;`.

- [ ] **Step 4: 컴파일 + 수동 검증**

`editor_recompile` → 에러 0. Battle 씬 Play → 시간정지(5초) 카드 적용 시 HP바 아래 TimeStop 아이콘 5초 표시 후 사라짐. Plague 둔화 프로크 시 Slow 아이콘 표시.

- [ ] **Step 5: Commit (stage)**

```bash
git add Assets/_Lair/Scripts/UI/BattleViewModel.cs Assets/_Lair/Scripts/UI/BattleHud.cs Assets/_Lair/Scripts/Battle/BattleController.cs
```

---

## Task 6 (M4): cleanup — 월드 status visual 제거

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (`EVisual`)
- Modify: `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (`PrewarmPools`)
- Delete: 월드 status 프리팹 6종 (`Assets/_Lair/Art/FX/` 또는 빌더 산출 경로)

- [ ] **Step 1: BattleController.PrewarmPools 의 status visual 워밍 6종 제거**

`SlowStatus`/`FearStatus`/`WeakenStatus`/`AttackDownStatus`/`TimeStopStatus`/`BleedStatus` 워밍 루프/항목 삭제. `PoisonAura` 워밍은 유지.

- [ ] **Step 2: LairVisualPrefabBuilder 의 status visual 6종 생성 제거**

status visual 6종 `BuildVisual` 호출/스펙 제거. `PoisonAura` 생성은 유지.

- [ ] **Step 3: EVisual status 6값 제거**

`Assets/_Lair/Scripts/Data/CommonEnum.cs` 의 `EVisual` 에서 `SlowStatus, FearStatus, WeakenStatus, AttackDownStatus, TimeStopStatus, BleedStatus` 삭제. **`PoisonAura` 유지.** 잔존 참조 grep 으로 0 확인.

- [ ] **Step 4: 월드 status 프리팹 6종 + .meta 삭제**

해당 6 프리팹 파일과 `.meta` 삭제. Addressables 엔트리에서도 제거.

- [ ] **Step 5: 컴파일 + grep 회귀**

`editor_recompile` → 에러 0. `Grep "Status\b"` / `EVisual.SlowStatus` 등 잔존 참조 0 확인.

- [ ] **Step 6: Commit (stage)**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Editor/LairVisualPrefabBuilder.cs Assets/_Lair/Scripts/Battle/BattleController.cs
git add -A Assets/_Lair/Art   # 삭제된 프리팹/.meta 반영 (관련 경로만)
```

---

## Task 7 (M5): 통합 검증 + 테스트 그린

- [ ] **Step 1: EditMode 전체 실행** — `HpBarViewTests`, `HeroAuraRunnerStatusIconTests`, 기존 status-visuals 회귀(있으면 갱신/제거). 전부 PASS.
- [ ] **Step 2: Battle 씬 한 판 수동/MCP 검증** — spec §7 성공 기준 체크: 6 유한 상태 떴다 사라짐 / 2 무기한 지속 / Plague 둔화 표시 / 동시 다중 나열 / 월드 도형 미출현 / 몬스터 바 비어있음 / 영웅 사망 후 잔존 X.
- [ ] **Step 3: 빌더 회귀** — `Build Character Prefabs` 재실행 후 HpBar 수작업(M0) + 아이콘 행 모두 보존 확인.
- [ ] **Step 4: Commit (stage)** — 잔여 변경 스테이징.

---

## Self-Review

**Spec coverage:**
- spec §0.2 8종 아이콘 → Task 1(IconCardId 8종) + Task 3/4(행) + Task 5(배선). ✓
- spec §2.1 타입 바인딩(소스 무관) → Task 1·2(이벤트 key=타입), Plague 검증 Task 5 Step4 / Task 7. ✓
- spec §2.2 HeroAuraRunner 이벤트화 → Task 2. ✓
- spec §2.3 MVVM 배선 → Task 5. ✓
- spec §2.4 HpBarView 행 + 몬스터 바 클린 → Task 3(컨테이너 비활성) + Task 4. ✓
- spec §3 M0 reconcile → Task 0. ✓
- spec §4 cleanup → Task 6. ✓
- spec §5 마일스톤 M0~M5 → Task 0~7 매핑. ✓
- spec §7 성공 기준 → Task 7 Step2. ✓

**Placeholder scan:** 코드 블록은 실제 시그니처/구현 포함. Task 5 Step2 영웅 셋업 지점은 "기존 영웅 스폰 경로" 로 위임(BattleController 미열람부) — gameplay-programmer 가 실제 지점 확정. 그 외 placeholder 없음.

**Type consistency:** `OnStatusShown(object, ECardId)`/`OnStatusHidden(object)` (Task2) ↔ VM `AddStatusIcon(object, ECardId)`/`RemoveStatusIcon(object)` (Task5) ↔ HpBarView `AddStatusIcon(object, Sprite)`/`RemoveStatusIcon(object)` (Task3) ↔ BattleHud 핸들러(Task5) 시그니처 일치. `IStatusVisual.IconCardId` (Task1) ↔ HeroAuraRunner `sv.IconCardId` (Task2) 일치. `_statusIconRow`/`_iconSlots` 필드명 Task3·4 일치.
