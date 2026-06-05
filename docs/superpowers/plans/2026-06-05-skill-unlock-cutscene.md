# 영웅 스킬 해금 컷인 연출 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **이 프로젝트는 Rule 01(자동 커밋 금지)** — 각 Task 의 "Checkpoint" 는 `git add` 스테이징까지만. 실제 `git commit` 은 start-develop 파이프라인 마무리(9단계)에서 한글 메시지(안)과 함께 일괄. 테스트 메서드명은 **한글**(project.md `test_method_naming: korean`).

**Goal:** 영웅 HP 가 스킬 해금 임계에 처음 도달하면 시간 정지 + 카메라 쉐이크와 함께 "영웅의 '{스킬명}' 스킬 해제" 배너가 좌→중→우로 슬라이드되는 컷인을 재생한다.

**Architecture:** `HeroSkillRunner` 가 해금 순간 이벤트를 발행 → `BattleController` 가 `SkillUnlockCutsceneController`(plain C# 오케스트레이터)로 라우팅 → 컨트롤러가 공유 `PauseService`(정지)·`ICameraShake`(BattleCamera 쉐이크)·`ISkillUnlockBanner`(독립 EUI 팝업 View)를 unscaled 코루틴으로 A안 순차 구동. 배너 프리팹은 `LairUIPrefabBuilder` 가 생성(빌더 영속, BattleHud 무수정).

**Tech Stack:** Unity 6 / C# / MVVM / ChvjPackage(CHMUI·CHText·CHMResource) / Unity Test Framework(NUnit EditMode).

---

## File Structure

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EUI.SkillUnlockBanner` 추가 | 수정 |
| `Assets/_Lair/Scripts/Battle/CommonInterface.cs` | `ICameraShake` 인터페이스 | 수정 |
| `Assets/_Lair/Scripts/Battle/BattleCamera.cs` | `ICameraShake` 구현 — 쉐이크 오프셋 합성 | 수정 |
| `Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs` | `OnSkillUnlocked` 이벤트 발행 | 수정 |
| `Assets/_Lair/Scripts/UI/SkillUnlockBannerView.cs` | `ISkillUnlockBanner` + UIArg + UIBase View (슬라이드 연출) | 신규 |
| `Assets/_Lair/Scripts/Battle/SkillUnlockCutsceneController.cs` | 큐 오케스트레이터 (정지·쉐이크·배너 순차) | 신규 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 컷인 컨트롤러 생성·배선·구독 | 수정 |
| `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` | `BuildSkillUnlockBanner` + `BuildAllUIPrefabs` 등록 | 수정 |
| `Assets/_Lair/Tests/EditMode/Battle/SkillUnlockCutsceneControllerTests.cs` | 컨트롤러 큐/정지/쉐이크 검증 | 신규 |
| `Assets/_Lair/Tests/EditMode/Character/HeroSkillRunnerUnlockEventTests.cs` | 이벤트 발행 검증 (선택, 합성) | 신규 |

`ISkillUnlockBanner` 는 View 파일(`SkillUnlockBannerView.cs`) 상단에 정의(단일 도메인, 테스트 모킹용). `ICameraShake` 는 Battle 공용이라 `Battle/CommonInterface.cs`.

---

## Task 1: EUI 에 SkillUnlockBanner 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs:27-35`

- [ ] **Step 1: enum 값 추가 (끝에 — int 직렬화 정합)**

`EUI` enum 마지막 항목 뒤에 추가:

```csharp
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
        BuildModalPopup,       //# 스포너 상태 UI — BuildPanel 클릭 시 화면 중앙 모달
        SpawnerStatusTooltip,  //# (v0.6.4 폐기 — enum 자리 보존, int 직렬화 정합)
        SynergyModalPopup,     //# 시너지 패널 클릭 시 화면 중앙 모달 — 적용된 시너지 효과 목록
        SkillUnlockBanner,     //# 스킬 해금 컷인 배너 — 독립 팝업(빌더 생성)
    }
```

- [ ] **Step 2: 컴파일 확인** — UnityMCP `editor_recompile` 후 에러 0 (reference_unity_verification 참조).
- [ ] **Step 3: Checkpoint** — `git add Assets/_Lair/Scripts/Data/CommonEnum.cs`

---

## Task 2: ICameraShake 인터페이스

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/CommonInterface.cs`

- [ ] **Step 1: 인터페이스 추가**

`Lair.Battle` namespace 안에 추가:

```csharp
    //# 카메라 쉐이크 추상. 컷인이 BattleCamera 구체 대신 참조 → EditMode 모킹.
    public interface ICameraShake
    {
        //# duration 초 동안 magnitude(월드 유닛) 세기로 카메라를 흔든다. unscaled 진행.
        void Shake(float duration, float magnitude);
    }
```

- [ ] **Step 2: 컴파일 확인** — 에러 0.
- [ ] **Step 3: Checkpoint** — `git add Assets/_Lair/Scripts/Battle/CommonInterface.cs`

---

## Task 3: BattleCamera 쉐이크 구현

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleCamera.cs`

**주의(spec §5 함정):** `ApplyZoom` 은 줌 안정 시 early-return → 그 경로에 쉐이크 넣으면 평소 안 돈다. 쉐이크는 **매 unscaled 프레임 무조건** `base 줌 위치 + 감쇠 오프셋` 으로 합성. 종료 시 오프셋 0 복원(드리프트 방지).

- [ ] **Step 1: 클래스 선언에 ICameraShake 추가 + 필드**

```csharp
    public class BattleCamera : MonoBehaviour, ICameraShake
```

필드 추가:

```csharp
        //# 쉐이크 상태 — 남은 시간/총 시간/세기. 활성 시 매 프레임 base 위치에 랜덤 오프셋 합성.
        private float _shakeRemain;
        private float _shakeDuration;
        private float _shakeMagnitude;
```

- [ ] **Step 2: Shake 메서드 추가**

```csharp
        //# ICameraShake — duration 동안 magnitude 세기로 흔든다. 중첩 호출 시 더 센/긴 쪽으로 갱신.
        public void Shake(float duration, float magnitude)
        {
            if (duration <= 0f || magnitude <= 0f)
                return;
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeRemain   = Mathf.Max(_shakeRemain, duration);
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        }
```

- [ ] **Step 3: base 위치 계산 분리 + 쉐이크 합성**

`ApplyZoom` 은 줌 보간만 유지하되 위치 적용을 base 계산으로 바꾸고, `Update` 끝에서 쉐이크 오프셋을 **항상** 합성한다. `Update` 수정:

```csharp
        private void Update()
        {
            HandleScrollInput();
            ApplyZoom();
            ApplyShake();   //# 줌 여부와 무관하게 매 프레임 — early-return 함정 회피
        }

        //# base 위치(앵커 기준 줌) 위에 감쇠 랜덤 오프셋. 남은 시간 0 이면 오프셋 0 으로 복원.
        private void ApplyShake()
        {
            Vector3 basePos = _worldAnchor + (-_forward) * _currentDist;
            if (_shakeRemain > 0f)
            {
                _shakeRemain -= Time.unscaledDeltaTime;
                float k = _shakeDuration > 0f ? Mathf.Clamp01(_shakeRemain / _shakeDuration) : 0f;
                float amp = _shakeMagnitude * k;   //# 선형 감쇠
                Vector3 offset = new Vector3(
                    (Random.value * 2f - 1f) * amp,
                    (Random.value * 2f - 1f) * amp,
                    0f);
                transform.position = basePos + offset;
                if (_shakeRemain <= 0f)
                {
                    _shakeRemain = 0f;
                    _shakeMagnitude = 0f;
                    transform.position = basePos;   //# 복원
                }
            }
        }
```

`ApplyZoom` 의 `transform.position = _worldAnchor + (-_forward) * _currentDist;` 는 그대로 둔다(줌 변할 때 즉시 반영). 쉐이크 활성 중엔 `ApplyShake` 가 같은 프레임 뒤에 덮어쓰므로 충돌 없음.

- [ ] **Step 4: 컴파일 확인** — 에러 0.
- [ ] **Step 5: Checkpoint** — `git add Assets/_Lair/Scripts/Battle/BattleCamera.cs`

---

## Task 4: HeroSkillRunner 해금 이벤트

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs:1-64`

- [ ] **Step 1: using + 이벤트 선언**

상단 `using System;` 추가(없으면). 필드부에 추가:

```csharp
        //# 해금 순간 발행 — BattleController 가 구독해 컷인 컨트롤러로 라우팅. 미구독 안전(null).
        public event Action<HeroSkillData> OnSkillUnlocked;
```

- [ ] **Step 2: Update 의 해금 루프에서 발행**

`Update()` 의 `_newly` 루프(현 `:52-59`)를 수정 — `_active.Add` 직후 발행:

```csharp
            _gate.Poll(_health.Ratio, _newly);
            for (int i = 0; i < _newly.Count; ++i)
            {
                HeroSkillData data = _loadout.Phases[_newly[i]].Skill;
                if (data != null)
                {
                    _active.Add(data.CreateRuntime());
                    OnSkillUnlocked?.Invoke(data);   //# 컷인 트리거 (정지 중에도 다음 프레임까지 무해)
                }
            }
```

- [ ] **Step 3: 컴파일 확인** — 에러 0.
- [ ] **Step 4: Checkpoint** — `git add Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs`

---

## Task 5: SkillUnlockBannerView (인터페이스 + UIBase View)

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SkillUnlockBannerView.cs`

연출: 라벨 세팅 → `_root.anchoredPosition` 을 왼쪽밖(`-offscreenX`)→중앙(0) 슬라이드 인 → 홀드 → 중앙→오른쪽밖(`+offscreenX`) 슬라이드 아웃. 전부 `unscaledDeltaTime`. Rule 02 §6.1 — 위젯 private, 의도 API 만.

- [ ] **Step 1: 파일 작성**

```csharp
using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# 컨트롤러가 배너 구체 대신 참조 — EditMode 모킹. (Rule 03 §5 — 페어 정의 같은 파일)
    public interface ISkillUnlockBanner
    {
        //# text 를 좌→중→우 1회 슬라이드 재생. 코루틴 완료 = 아웃 종료.
        IEnumerator PlayCo(string text);
        //# 즉시 화면 밖 숨김(초기/리셋).
        void HideImmediate();
    }

    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SkillUnlockBannerArg : UIArg { }

    //# 스킬 해금 컷인 배너. 독립 EUI 팝업(빌더 생성). 슬라이드 연출만 — 정지/쉐이크/큐는 컨트롤러.
    public class SkillUnlockBannerView : UIBase, ISkillUnlockBanner
    {
        [SerializeField] private RectTransform _root;   //# 슬라이드 대상(가로 밴드)
        [SerializeField] private CHText _label;         //# "영웅의 '...' 스킬 해제"

        [SerializeField] private float _slideInDuration = 0.35f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField] private float _slideOutDuration = 0.35f;
        [SerializeField] private float _offscreenX = 1200f;   //# 화면 밖 X (배너 폭+여유)

        public override void InitUI(UIArg arg) => HideImmediate();

        public void HideImmediate()
        {
            if (_root != null)
                _root.anchoredPosition = new Vector2(-_offscreenX, _root.anchoredPosition.y);
        }

        public IEnumerator PlayCo(string text)
        {
            if (_label != null)
                _label.SetText(text);

            float y = _root != null ? _root.anchoredPosition.y : 0f;

            //# 인 — 왼쪽밖 → 중앙
            yield return SlideCo(new Vector2(-_offscreenX, y), new Vector2(0f, y), _slideInDuration);
            //# 홀드
            float t = 0f;
            while (t < _holdDuration) { t += Time.unscaledDeltaTime; yield return null; }
            //# 아웃 — 중앙 → 오른쪽밖
            yield return SlideCo(new Vector2(0f, y), new Vector2(_offscreenX, y), _slideOutDuration);

            HideImmediate();
        }

        private IEnumerator SlideCo(Vector2 from, Vector2 to, float dur)
        {
            if (_root == null || dur <= 0f)
                yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                _root.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }
            _root.anchoredPosition = to;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인** — 에러 0.
- [ ] **Step 3: Checkpoint** — `git add Assets/_Lair/Scripts/UI/SkillUnlockBannerView.cs Assets/_Lair/Scripts/UI/SkillUnlockBannerView.cs.meta`

---

## Task 6: SkillUnlockCutsceneController (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/SkillUnlockCutsceneController.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/SkillUnlockCutsceneControllerTests.cs`

오케스트레이터: plain C# class. `Enqueue` 로 스킬명 누적, 진행 중 아니면 host.StartCoroutine(`RunQueueCo`). 시퀀스: Pause 1회 → 큐 빌 때까지 [Shake + banner.PlayCo] → Resume 1회. 텍스트 포맷·빈 이름 가드 포함.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System.Collections;
using System.Collections.Generic;
using Lair.Battle;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode.Battle
{
    public class SkillUnlockCutsceneControllerTests
    {
        //# PlayCo 가 즉시 끝나는 mock 배너 — 호출된 텍스트 기록.
        private class FakeBanner : ISkillUnlockBanner
        {
            public readonly List<string> Played = new();
            public IEnumerator PlayCo(string text) { Played.Add(text); yield break; }
            public void HideImmediate() { }
        }

        private class FakeShake : ICameraShake
        {
            public int ShakeCount;
            public void Shake(float duration, float magnitude) => ShakeCount++;
        }

        //# IEnumerator 를 끝까지 수동 펌핑(중첩 yield return IEnumerator 포함).
        private static void Pump(IEnumerator co)
        {
            Stack<IEnumerator> stack = new();
            stack.Push(co);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is IEnumerator inner) stack.Push(inner);
                }
                else stack.Pop();
            }
        }

        [Test]
        public void 단일_해금_시_포맷된_텍스트로_배너_재생_및_쉐이크_1회()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("회전 블레이드");
            Pump(c.DrainForTest());

            Assert.AreEqual(1, banner.Played.Count);
            Assert.AreEqual("영웅의 '회전 블레이드' 스킬 해제", banner.Played[0]);
            Assert.AreEqual(1, shake.ShakeCount);
        }

        [Test]
        public void 다중_해금_큐_순차_재생_후_정지_재개_1쌍()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("A");
            c.Enqueue("B");
            Pump(c.DrainForTest());

            Assert.AreEqual(2, banner.Played.Count);
            Assert.AreEqual("영웅의 'A' 스킬 해제", banner.Played[0]);
            Assert.AreEqual("영웅의 'B' 스킬 해제", banner.Played[1]);
            Assert.IsFalse(pause.IsPaused, "큐 드레인 후 Resume 으로 정지 해제되어야 함");
        }

        [Test]
        public void 빈_이름은_fallback_문구로_재생()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("");
            Pump(c.DrainForTest());

            Assert.AreEqual("영웅의 새 스킬 해제", banner.Played[0]);
        }
    }
}
```

> 참고: 위 테스트는 host.StartCoroutine 없이 검증하려고 컨트롤러에 **테스트 전용 `DrainForTest()`** (= `RunQueueCo` 동일 IEnumerator 반환, 코루틴 호스트 미사용)를 둔다. 실게임은 `Enqueue` 가 host.StartCoroutine(RunQueueCo) 호출.

- [ ] **Step 2: 테스트 실패 확인** — `SkillUnlockCutsceneController` 미정의로 컴파일 실패.

- [ ] **Step 3: 컨트롤러 구현**

```csharp
using System.Collections;
using System.Collections.Generic;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# 스킬 해금 컷인 오케스트레이터(plain). 정지·쉐이크·배너를 A안 순차 구동.
    //# 실게임: Enqueue → (idle 시) _host.StartCoroutine(RunQueueCo). 테스트: DrainForTest 수동 펌핑.
    public class SkillUnlockCutsceneController
    {
        private const float ShakeDuration = 0.4f;
        private const float ShakeMagnitude = 0.3f;
        private const string Fallback = "영웅의 새 스킬 해제";

        private readonly PauseService _pause;
        private readonly ICameraShake _shake;
        private readonly ISkillUnlockBanner _banner;
        private readonly MonoBehaviour _host;   //# 코루틴 호스트(실게임). 테스트는 null.

        private readonly Queue<string> _pending = new();
        private bool _running;

        public SkillUnlockCutsceneController(PauseService pause, ICameraShake shake, ISkillUnlockBanner banner, MonoBehaviour host = null)
        {
            _pause = pause;
            _shake = shake;
            _banner = banner;
            _host = host;
        }

        //# 스킬명 누적. idle 이고 host 있으면 코루틴 구동.
        public void Enqueue(string skillName)
        {
            _pending.Enqueue(skillName);
            if (_running == false && _host != null)
                _host.StartCoroutine(RunQueueCo());
        }

        //# 라운드 리셋.
        public void Reset()
        {
            _pending.Clear();
            _running = false;
            _banner?.HideImmediate();
        }

        //# 테스트용 — host 없이 시퀀스 IEnumerator 직접 펌핑.
        public IEnumerator DrainForTest() => RunQueueCo();

        private IEnumerator RunQueueCo()
        {
            if (_running)
                yield break;
            _running = true;
            _pause?.Pause();
            //# TODO(sound): 컷인 시작 사운드 seam — 추후 CHMSound.Play(EAudio.SkillUnlock) 한 줄.
            while (_pending.Count > 0)
            {
                string name = _pending.Dequeue();
                _shake?.Shake(ShakeDuration, ShakeMagnitude);
                if (_banner != null)
                    yield return _banner.PlayCo(Format(name));
            }
            _pause?.Resume();
            _running = false;
        }

        private static string Format(string skillName)
        {
            if (string.IsNullOrEmpty(skillName))
                return Fallback;
            return $"영웅의 '{skillName}' 스킬 해제";
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인** — EditMode 3 테스트 PASS (reference_unity_verification 의 테스트 러너).
- [ ] **Step 5: Checkpoint** — `git add` 컨트롤러 + 테스트(.cs + .meta).

---

## Task 7: LairUIPrefabBuilder — BuildSkillUnlockBanner

**Files:**
- Modify: `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` (`BuildAllUIPrefabs` ~`:153-155`, 신규 `BuildSkillUnlockBanner`)

`BuildResultPopup`/`BuildCardSelectionPopup` 패턴 참조. 풀폭 가로 밴드 RectTransform(`_root`) + 중앙 `CHText _label`(TMP 동반) 구성, `SkillUnlockBannerView` 부착, `_root`·`_label`·SerializeField 와이어링, Addressables 등록(주소=`SkillUnlockBanner`, 라벨=group). EUI 키와 파일명 일치(Rule 03 §2).

- [ ] **Step 1: BuildAllUIPrefabs 에 호출 추가**

`BuildCardSelectionPopup(settings, group);` 다음 줄에:

```csharp
            BuildSkillUnlockBanner(settings, group);   //# 스킬 해금 컷인 배너
```

- [ ] **Step 2: BuildSkillUnlockBanner 구현** (BuildCardSelectionPopup 구조 모사 — Canvas/RectTransform/CHText/CHMUI 등록). 핵심:
  - 루트 GameObject `SkillUnlockBanner` (UIBase 표준: Canvas/CanvasGroup 등 기존 팝업 빌더 헬퍼 재사용).
  - 자식 `Band`(RectTransform, 가로 풀폭, 중앙 정렬) → `SkillUnlockBannerView._root` 에 연결.
  - `Band` 자식 `Label`(TextMeshProUGUI + CHText) → `_label` 에 연결.
  - `SkillUnlockBannerView` AddComponent 후 `_root`/`_label` SerializedObject 와이어링.
  - 프리팹 저장 경로 `{PrefabDir}/SkillUnlockBanner.prefab`, Addressables 주소 `SkillUnlockBanner`.

> 구현 세부(헬퍼명·group 등록 호출)는 같은 파일의 `BuildCardSelectionPopup`/`BuildResultPopup` 을 그대로 따른다 — 신규 헬퍼 만들지 말 것(DRY).

- [ ] **Step 3: 빌더 실행** — Unity 메뉴 `Lair/Setup/M4 - Build UI Prefabs` 실행 → `SkillUnlockBanner.prefab` 생성·Addressables 등록 확인.
- [ ] **Step 4: Checkpoint** — `git add` 빌더 .cs + 생성된 프리팹/메타/Addressables 설정.

---

## Task 8: BattleController 배선

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (필드, `:115` 부근 init, `:356-361` 구독)

- [ ] **Step 1: 필드 추가**

```csharp
        private SkillUnlockCutsceneController _cutscene;
        private SkillUnlockBannerView _bannerView;
```

- [ ] **Step 2: 배너 팝업 표시 + 컨트롤러 생성** (`_pause = new PauseService();` 직후)

배너 팝업을 1회 표시해 View 핸들 확보(`CHMUI.Instance.ShowUIAsync(EUI.SkillUnlockBanner, new SkillUnlockBannerArg())` 후 인스턴스에서 `SkillUnlockBannerView` 취득 — 기존 HUD 취득 패턴 동형). BattleCamera 는 Main Camera 부착이므로 `Camera.main` 경유 1회 캐싱 또는 인스펙터 참조. 컨트롤러 생성:

```csharp
            //# 컷인 — 기존 _pause(카드픽과 depth 공유) + 카메라 쉐이크 + 배너. host=this(코루틴 구동).
            ICameraShake cameraShake = Camera.main != null ? Camera.main.GetComponent<ICameraShake>() : null;
            if (_bannerView != null && cameraShake != null)
            {
                _bannerView.HideImmediate();
                _cutscene = new SkillUnlockCutsceneController(_pause, cameraShake, _bannerView, this);
            }
```

> `GetComponent<ICameraShake>()` 는 Awake/init 1회 — Rule 02 §5 런타임 경로 아님(허용).

- [ ] **Step 3: 해금 구독** (`skillRunner.Bind(loadout);` 직후, 이중 구독 가드)

```csharp
                    if (_cutscene != null)
                    {
                        skillRunner.OnSkillUnlocked -= HandleSkillUnlocked;   //# 재시작 이중구독 방지
                        skillRunner.OnSkillUnlocked += HandleSkillUnlocked;
                    }
```

핸들러 메서드 추가:

```csharp
        private void HandleSkillUnlocked(HeroSkillData data)
        {
            if (_cutscene != null && data != null)
                _cutscene.Enqueue(data.DisplayName);
        }
```

- [ ] **Step 4: 컴파일 확인** — 에러 0. (`using Lair.UI;` 필요 시 추가)
- [ ] **Step 5: Checkpoint** — `git add Assets/_Lair/Scripts/Battle/BattleController.cs`

---

## Task 9: 인게임 수동 검증

- [ ] **Step 1:** Battle 씬 진입(`sim_play`), 영웅 HP 를 임계 아래로 떨어뜨려(디버그/자연 전투) 해금 트리거.
- [ ] **Step 2:** 확인 — (a) 시간 정지, (b) 카메라 쉐이크, (c) 배너 좌→중→우 슬라이드, (d) 텍스트 `영웅의 '{스킬명}' 스킬 해제`, (e) 재개. `screenshot_game` 로 캡처.
- [ ] **Step 3:** 동시 해금(HP 급락) 시 순차 재생 확인.
- [ ] **Step 4:** 카드 픽과 같은 구간 겹침 시 timeScale 정상(멈춤→재개) 확인.

---

## Self-Review (작성자 점검 완료)

- **Spec coverage**: §3-1 배너 독립팝업→Task1/5/7, §3-2 컴포넌트표→Task2~6/8, §3-3 배선→Task8, §3-4 수치→Task5/6 SerializeField·const, §5 엣지(동시·중첩·종료·재시작·빈이름·쉐이크함정)→Task3 주석/Task6 테스트/Task8 가드/Task9, §6 테스트→Task6. 누락 없음.
- **Placeholder scan**: Task7 빌더 세부는 "기존 BuildCardSelectionPopup 따름"으로 위임(신규 헬퍼 금지 명시) — 도메인 빌더 코드라 game-designer/gameplay-programmer 가 동일 파일 패턴으로 채움. 그 외 결정 코드 전량 명시.
- **Type consistency**: `OnSkillUnlocked(HeroSkillData)`, `ICameraShake.Shake(float,float)`, `ISkillUnlockBanner.PlayCo(string)/HideImmediate()`, `SkillUnlockCutsceneController(PauseService, ICameraShake, ISkillUnlockBanner, MonoBehaviour)`/`Enqueue`/`Reset`/`DrainForTest` — Task 간 시그니처 일치 확인.
