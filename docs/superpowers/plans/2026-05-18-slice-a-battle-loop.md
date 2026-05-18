# Slice A — 5분 자동전투 코어 루프 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Project Lair MVP 의 첫 수직 슬라이스 — 영웅 1명, 몬스터 3종, 5분 자동전투, HUD, 결과 팝업, 재시작까지 한 판 풀 플레이가 가능한 빌드.

**Architecture:** 단방향 의존 4계층 (Bootstrap → UI MVVM → Battle Logic → Character Composition → ChvjPackage Infra). UI 만 풀세트 MVVM, 캐릭터는 인터페이스 4종(IMover/IHealth/IAttacker/ITargetProvider)의 컴포지션. 모든 에셋 로드는 `Enum.ToString()` 키를 통해 Addressables 로.

**Tech Stack:** Unity 2022.3, URP 17.0.4, C# 9, NUnit (Unity Test Framework 1.6.0), Addressables 2.8.1, `com.chvj.unityinfra` (로컬 패키지).

**참고 설계서:** `docs/superpowers/specs/2026-05-18-slice-a-battle-loop-design.md`

---

## 프로젝트 룰 — 매 태스크에 적용

- **Rule 01**: 모든 "Commit" 스텝은 *스테이지 + 한글 커밋 메시지(안) 출력만*. `git commit` 실행 금지. 포맷 `# [주제] - 메시지`.
- **Rule 02**: 모든 신규 주석은 `//#` 접두어.
- **Rule 03/06**: 구체 클래스 직접 참조 X, 인터페이스/이벤트.
- **Rule 04**: 캐릭터·UI 는 프리팹.
- **Rule 05**: UI 계층만 MVVM 풀세트. 캐릭터는 컴포지션.
- **Rule 07**: ChvjPackage(`CHMResource/CHMUI/CHMPool/UIBase`) 기존 API 만 사용. 패키지 수정 금지.
- **Rule 08**: 에셋 파일명 = Enum 값명 정확 일치 (대소문자 포함). Addressables 주소 = 파일명.

## 테스트 실행 방법 (Unity)

EditMode/PlayMode 테스트는 Unity 에디터에서 실행:
1. `Window → General → Test Runner` 열기
2. `EditMode` 또는 `PlayMode` 탭 선택
3. `Run All` 또는 개별 케이스 우클릭 → `Run Selected`

CLI 실행 옵션 (선택):
```powershell
& "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe" `
    -batchmode -projectPath "D:\Project_Lair" `
    -runTests -testPlatform EditMode `
    -testResults "TestResults-EditMode.xml" -quit
```

## 작업 디렉토리

모든 명령은 `D:\Project_Lair` 기준. PowerShell 환경 (Bash 도구는 `/d/Project_Lair`).

---

# M1 — 골격 셋업 (~30분)

## Task 1.1: 폴더 트리 + Lair.asmdef 생성

**Files:**
- Create dir: `Assets/_Lair/Scripts/Battle/`
- Create dir: `Assets/_Lair/Scripts/Character/`
- Create dir: `Assets/_Lair/Scripts/UI/`
- Create dir: `Assets/_Lair/Scripts/Data/Enums/`
- Create dir: `Assets/_Lair/Prefabs/Characters/`
- Create dir: `Assets/_Lair/Prefabs/UI/`
- Create dir: `Assets/_Lair/Scenes/`
- Create: `Assets/_Lair/Scripts/Lair.asmdef`

- [ ] **Step 1: 폴더 생성**

```powershell
New-Item -ItemType Directory -Force `
    "Assets/_Lair/Scripts/Battle", `
    "Assets/_Lair/Scripts/Character", `
    "Assets/_Lair/Scripts/UI", `
    "Assets/_Lair/Scripts/Data/Enums", `
    "Assets/_Lair/Prefabs/Characters", `
    "Assets/_Lair/Prefabs/UI", `
    "Assets/_Lair/Scenes" | Out-Null
```

Expected: 7개 경로 생성, 오류 없음.

- [ ] **Step 2: Lair.asmdef 작성**

Create `Assets/_Lair/Scripts/Lair.asmdef`:
```json
{
    "name": "Lair",
    "rootNamespace": "Lair",
    "references": [
        "ChvjUnityInfra"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Unity 에서 컴파일 확인**

Unity 에디터에서 `Ctrl+R` (Refresh) 또는 잠시 대기 → 콘솔에 컴파일 에러 0 확인.

`ChvjUnityInfra` 참조가 인식되지 않으면 패키지 `com.chvj.unityinfra/Runtime/com.chvj.unityinfra.asmdef` 의 이름과 일치하는지 확인 (대소문자 포함).

---

## Task 1.2: Enum 4개 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Data/Enums/EHero.cs`
- Create: `Assets/_Lair/Scripts/Data/Enums/EMonster.cs`
- Create: `Assets/_Lair/Scripts/Data/Enums/EUI.cs`
- Create: `Assets/_Lair/Scripts/Data/Enums/EScene.cs`

- [ ] **Step 1: EHero 작성**

Create `Assets/_Lair/Scripts/Data/Enums/EHero.cs`:
```csharp
namespace Lair.Data
{
    //# CHMResource 로 영웅 프리팹 로드 키. 값명 = 프리팹 파일명.
    public enum EHero
    {
        Knight,
    }
}
```

- [ ] **Step 2: EMonster 작성**

Create `Assets/_Lair/Scripts/Data/Enums/EMonster.cs`:
```csharp
namespace Lair.Data
{
    //# CHMResource 로 몬스터 프리팹 로드 키. 값명 = 프리팹 파일명.
    public enum EMonster
    {
        Slime,
        Golem,
        Orc,
    }
}
```

- [ ] **Step 3: EUI 작성**

Create `Assets/_Lair/Scripts/Data/Enums/EUI.cs`:
```csharp
namespace Lair.Data
{
    //# CHMUI.ShowUI 키. 값명 = UI 프리팹 파일명.
    public enum EUI
    {
        BattleHud,
        ResultPopup,
    }
}
```

- [ ] **Step 4: EScene 작성**

Create `Assets/_Lair/Scripts/Data/Enums/EScene.cs`:
```csharp
namespace Lair.Data
{
    //# SceneManager.LoadScene 호출 시 ToString() 키. 값명 = 씬 파일명.
    public enum EScene
    {
        Battle,
    }
}
```

- [ ] **Step 5: 컴파일 확인**

Unity 콘솔에 에러 0 확인.

---

## Task 1.3: Battle.unity 빈 씬 + Build Settings

- [ ] **Step 1: 빈 씬 생성**

Unity 에서:
1. `File → New Scene → Basic (URP)` 템플릿 선택
2. `Ctrl+S` → 경로 `Assets/_Lair/Scenes/Battle.unity` 로 저장

- [ ] **Step 2: Build Settings 등록**

1. `File → Build Settings` 열기
2. `Add Open Scenes` 클릭 → `_Lair/Scenes/Battle.unity` 가 Index 0 으로 추가됨
3. 기존 `Assets/Scenes/SampleScene.unity` 가 있으면 체크 해제 (Index 만 사라지고 파일은 유지)
4. 창 닫기

Expected: Build Settings 에 Battle 씬 1개 (Index 0).

---

## Task 1.4: Addressables Settings 초기화

- [ ] **Step 1: Addressables 메뉴 열기**

`Window → Asset Management → Addressables → Groups`

처음 열 때 "Create Addressables Settings" 버튼이 보이면 클릭. 자동으로 `Assets/AddressableAssetsData/` 폴더와 기본 그룹이 생성됨.

- [ ] **Step 2: "Resource" 그룹 생성**

Groups 창에서:
1. 상단 `+ Create → Group → Packed Assets` 클릭
2. 생성된 그룹을 우클릭 → `Rename` → `Resource`
3. 그룹 선택 후 인스펙터의 `Content Packing & Loading → Advanced Options`:
   - `Include In Build`: 체크
   - `Bundle Mode`: Pack Together
4. 같은 인스펙터의 `Schema` 영역에서 **Strip Hash from Name** 또는 비슷한 옵션은 그대로 두고, **주소(Address)는 다음 태스크에서 등록 시 수동으로 파일명만 사용**한다.

- [ ] **Step 3: "Resource" 라벨 생성**

Groups 창 우상단 `Profile` 드롭다운 옆 라벨 아이콘(태그 모양) → `Manage Labels` → `+` → `Resource` 입력 → 추가.

(이미 default 라벨이 있을 수 있음. `Resource` 라벨이 없으면 추가만 하면 됨.)

---

## Task 1.5: M1 검증 + 스테이지

- [ ] **Step 1: 컴파일 + 씬 진입 확인**

Unity 에디터:
1. 콘솔(`Window → General → Console`) 에러 0 확인
2. Battle 씬 열린 상태에서 Play 버튼 → 빈 씬이지만 에러 없이 재생 → Stop

- [ ] **Step 2: 변경 사항 스테이지**

```powershell
git add Assets/_Lair Assets/AddressableAssetsData ProjectSettings/EditorBuildSettings.asset ProjectSettings/AddressableAssetSettings.asset
git status --short
```

Expected: `A  Assets/_Lair/Scripts/Lair.asmdef` 등 새 파일들이 staged.

- [ ] **Step 3: 커밋 메시지(안) 출력 — Rule 01**

스테이지된 변경에 대해 다음 메시지를 사용자에게 제안 (실제 commit 실행 X):

```
# [chore] - Slice A 골격 셋업 (폴더/asmdef/Enum/씬/Addressables)
```

**M1 검증 게이트 통과 조건**: 컴파일 에러 0, Lair.asmdef 가 ChvjUnityInfra 참조 인식, Battle 씬 진입 가능, Addressables 그룹 "Resource" 존재.

**🛑 사용자 검토 포인트**: 폴더 구조와 Enum 명세가 의도대로인지 확인 요청.

---

# M2 — POCO 로직 TDD (~3~4시간)

## Task 2.1: Lair.Tests.EditMode.asmdef 생성

**Files:**
- Create dir: `Assets/_Lair/Tests/EditMode/`
- Create: `Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef`

- [ ] **Step 1: 폴더 + asmdef 작성**

```powershell
New-Item -ItemType Directory -Force `
    "Assets/_Lair/Tests/EditMode/Battle", `
    "Assets/_Lair/Tests/EditMode/Character", `
    "Assets/_Lair/Tests/EditMode/Helpers" | Out-Null
```

Create `Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef`:
```json
{
    "name": "Lair.Tests.EditMode",
    "rootNamespace": "Lair.Tests",
    "references": [
        "Lair",
        "ChvjUnityInfra",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Test Runner 인식 확인**

Unity 에디터:
1. `Window → General → Test Runner`
2. `EditMode` 탭에서 `Lair.Tests.EditMode` 어셈블리가 나타나는지 (비어있지만 OK)

---

## Task 2.2: BattleClock — TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/BattleClock.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BattleClockTests.cs`

- [ ] **Step 1: 실패하는 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Battle/BattleClockTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    public class BattleClockTests
    {
        [Test]
        public void Tick_누적되어_Elapsed_증가()
        {
            var clock = new BattleClock(10f);
            clock.Start();

            clock.Tick(0.5f);
            clock.Tick(0.5f);
            clock.Tick(1.0f);

            Assert.AreEqual(2.0f, clock.Elapsed, 0.0001f);
        }

        [Test]
        public void OnTick_매_Tick마다_발행()
        {
            var clock = new BattleClock(10f);
            clock.Start();
            int callCount = 0;
            clock.OnTick += _ => callCount++;

            clock.Tick(0.5f);
            clock.Tick(0.5f);
            clock.Tick(0.5f);

            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void OnTimeUp_Total_도달_시_1회만_발행()
        {
            var clock = new BattleClock(1.0f);
            clock.Start();
            int timeUpCount = 0;
            clock.OnTimeUp += () => timeUpCount++;

            clock.Tick(0.6f);
            clock.Tick(0.6f);  //# 누적 1.2, 초과
            clock.Tick(0.6f);  //# 이미 IsRunning false 이므로 무시

            Assert.AreEqual(1, timeUpCount);
            Assert.IsFalse(clock.IsRunning);
        }

        [Test]
        public void Stop_이후_Tick_무시()
        {
            var clock = new BattleClock(10f);
            clock.Start();
            clock.Tick(1.0f);
            clock.Stop();

            clock.Tick(5.0f);

            Assert.AreEqual(1.0f, clock.Elapsed, 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

Unity Test Runner → EditMode → Run All.
Expected: `BattleClockTests` 4개 모두 컴파일 에러로 실패 ("type or namespace 'BattleClock' could not be found").

- [ ] **Step 3: 최소 구현 작성**

Create `Assets/_Lair/Scripts/Battle/BattleClock.cs`:
```csharp
using System;

namespace Lair.Battle
{
    //# 전투 경과 시간 관리. POCO, Unity 의존성 0.
    public class BattleClock
    {
        public float Elapsed { get; private set; }
        public float TotalSeconds { get; }
        public bool IsRunning { get; private set; }

        public event Action<float> OnTick;
        public event Action OnTimeUp;

        public BattleClock(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        public void Start()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float dt)
        {
            if (IsRunning == false) return;
            Elapsed += dt;
            OnTick?.Invoke(Elapsed);
            if (Elapsed >= TotalSeconds)
            {
                IsRunning = false;
                OnTimeUp?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → Run All. Expected: 4/4 PASS.

- [ ] **Step 5: 스테이지**

```powershell
git add Assets/_Lair/Scripts/Battle/BattleClock.cs Assets/_Lair/Tests/EditMode/
git status --short
```

(아직 커밋 X. M2 끝에서 일괄 스테이지.)

---

## Task 2.3: BattleStateModel + BattleResult

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BattleStateModel.cs`

- [ ] **Step 1: 모델 + enum 작성**

Create `Assets/_Lair/Scripts/UI/BattleStateModel.cs`:
```csharp
namespace Lair.UI
{
    //# 순수 POCO. Unity 의존성 0. 테스트에서 직접 생성 가능.
    public class BattleStateModel
    {
        public float ElapsedSeconds;
        public float TotalSeconds = 300f;   //# 5:00
        public int HeroHp;
        public int HeroMaxHp;
        public BattleResult Result = BattleResult.None;
    }

    public enum BattleResult
    {
        None,
        Win,
        Lose,
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity 콘솔 에러 0.

(별도 테스트 X — 필드만 있는 모델.)

---

## Task 2.4: BattleViewModel — TDD

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BattleViewModel.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelTests.cs`:
```csharp
using NUnit.Framework;
using Lair.UI;

namespace Lair.Tests.UI
{
    public class BattleViewModelTests
    {
        [Test]
        public void UpdateTimer_이벤트_발행과_값_노출()
        {
            var model = new BattleStateModel { TotalSeconds = 300f };
            var vm = new BattleViewModel(model);
            float captured = -1f;
            float capturedTotal = -1f;
            vm.OnTimerChanged += (e, t) => { captured = e; capturedTotal = t; };

            vm.UpdateTimer(42.5f);

            Assert.AreEqual(42.5f, captured, 0.0001f);
            Assert.AreEqual(300f, capturedTotal, 0.0001f);
            Assert.AreEqual(42.5f, vm.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void UpdateHeroHp_비율_계산_및_이벤트()
        {
            var model = new BattleStateModel();
            var vm = new BattleViewModel(model);
            float capturedRatio = -1f;
            vm.OnHeroHpRatioChanged += r => capturedRatio = r;

            vm.UpdateHeroHp(250, 1000);

            Assert.AreEqual(0.25f, capturedRatio, 0.0001f);
            Assert.AreEqual(0.25f, vm.HeroHpRatio, 0.0001f);
        }

        [Test]
        public void UpdateHeroHp_max_0이면_비율_0_안전()
        {
            var model = new BattleStateModel();
            var vm = new BattleViewModel(model);
            float capturedRatio = -1f;
            vm.OnHeroHpRatioChanged += r => capturedRatio = r;

            vm.UpdateHeroHp(0, 0);

            Assert.AreEqual(0f, capturedRatio, 0.0001f);
        }

        [Test]
        public void EndBattle_Result_저장_이벤트_발행()
        {
            var model = new BattleStateModel();
            var vm = new BattleViewModel(model);
            BattleResult captured = BattleResult.None;
            vm.OnBattleEnded += r => captured = r;

            vm.EndBattle(BattleResult.Win);

            Assert.AreEqual(BattleResult.Win, captured);
            Assert.AreEqual(BattleResult.Win, vm.Result);
        }
    }
}
```

- [ ] **Step 2: 실행 → 실패 확인**

Test Runner → Run All. Expected: `BattleViewModelTests` 4개 컴파일 실패.

- [ ] **Step 3: 구현**

Create `Assets/_Lair/Scripts/UI/BattleViewModel.cs`:
```csharp
using System;

namespace Lair.UI
{
    //# Model 가공 + 이벤트 노출. View 를 모름.
    public class BattleViewModel
    {
        private readonly BattleStateModel _model;

        public event Action<float, float> OnTimerChanged;
        public event Action<float> OnHeroHpRatioChanged;
        public event Action<BattleResult> OnBattleEnded;

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

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        public BattleResult Result  => _model.Result;
    }
}
```

- [ ] **Step 4: 통과 확인**

Test Runner → Run All → 4/4 PASS.

---

## Task 2.5: FakeHealth 헬퍼 + IHealth 인터페이스

**Files:**
- Create: `Assets/_Lair/Scripts/Character/IHealth.cs`
- Create: `Assets/_Lair/Tests/EditMode/Helpers/FakeHealth.cs`

- [ ] **Step 1: IHealth 인터페이스 작성**

Create `Assets/_Lair/Scripts/Character/IHealth.cs`:
```csharp
using System;

namespace Lair.Character
{
    //# 캐릭터 HP 추상. Health 구현체와 테스트용 FakeHealth 모두 만족.
    public interface IHealth
    {
        int Max { get; }
        int Current { get; }
        float Ratio { get; }
        bool IsAlive { get; }

        event Action<int, int> OnChanged;   //# (current, max)
        event Action OnDied;

        void TakeDamage(int amount);
        void SetMax(int max, bool resetCurrent = true);
    }
}
```

- [ ] **Step 2: FakeHealth 테스트 더블 작성**

Create `Assets/_Lair/Tests/EditMode/Helpers/FakeHealth.cs`:
```csharp
using System;
using Lair.Character;

namespace Lair.Tests.Helpers
{
    //# IHealth 테스트 더블. TakeDamage 호출 추적 + 임의 값 설정 가능.
    public class FakeHealth : IHealth
    {
        public int Max { get; private set; } = 100;
        public int Current { get; private set; } = 100;
        public float Ratio => Max > 0 ? (float)Current / Max : 0f;
        public bool IsAlive => Current > 0;

        public int LastDamage { get; private set; }
        public int DamageCallCount { get; private set; }

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        public void TakeDamage(int amount)
        {
            LastDamage = amount;
            DamageCallCount++;
            if (IsAlive == false) return;
            Current = Math.Max(0, Current - amount);
            OnChanged?.Invoke(Current, Max);
            if (Current == 0) OnDied?.Invoke();
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            Max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, Max);
        }

        public void ForceSetCurrent(int v)
        {
            Current = v;
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Test Runner 새로고침 → 에러 0.

---

## Task 2.6: Health — TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Health.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/HealthTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Character/HealthTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.Character
{
    public class HealthTests
    {
        private static Health NewHealth(int max = 100)
        {
            //# Health 는 MonoBehaviour. 테스트에서는 ScriptableObject/MB 없이도 동작하도록
            //# 일반 클래스 형태의 내부 로직만 검증. (Unity 가 MB 인스턴스화 없이 new 호출은
            //# 권장하지 않지만 테스트 한정 허용.)
            var h = new Health();
            h.SetMax(max);
            return h;
        }

        [Test]
        public void TakeDamage_Current_감소_및_OnChanged_발행()
        {
            var h = NewHealth(100);
            int curCaptured = -1, maxCaptured = -1;
            h.OnChanged += (c, m) => { curCaptured = c; maxCaptured = m; };

            h.TakeDamage(30);

            Assert.AreEqual(70, h.Current);
            Assert.AreEqual(70, curCaptured);
            Assert.AreEqual(100, maxCaptured);
        }

        [Test]
        public void Current_0_도달_시_OnDied_1회만_발행()
        {
            var h = NewHealth(50);
            int diedCount = 0;
            h.OnDied += () => diedCount++;

            h.TakeDamage(30);
            h.TakeDamage(30);   //# 누적 60 → 0 으로 클램프 + OnDied
            h.TakeDamage(30);   //# 사망 후, OnDied 추가 발행 X

            Assert.AreEqual(0, h.Current);
            Assert.IsFalse(h.IsAlive);
            Assert.AreEqual(1, diedCount);
        }

        [Test]
        public void 사망_후_TakeDamage_무시()
        {
            var h = NewHealth(10);
            h.TakeDamage(10);
            int onChangedAfterDeath = 0;
            h.OnChanged += (_, _) => onChangedAfterDeath++;

            h.TakeDamage(5);

            Assert.AreEqual(0, h.Current);
            Assert.AreEqual(0, onChangedAfterDeath);
        }

        [Test]
        public void SetMax_resetCurrent_옵션()
        {
            var h = NewHealth(100);
            h.TakeDamage(40);   //# Current=60

            h.SetMax(200, resetCurrent: false);
            Assert.AreEqual(60, h.Current);
            Assert.AreEqual(200, h.Max);

            h.SetMax(50, resetCurrent: true);
            Assert.AreEqual(50, h.Current);
            Assert.AreEqual(50, h.Max);
        }
    }
}
```

**중요**: `Health` 는 다른 태스크(M3)에서 `MonoBehaviour` 로 만들지만, 테스트는 순수 로직만 검증한다. 따라서 본 태스크에서는 일단 **POCO** 로 작성하고, M3.4 (AutoCombatAI 단계) 에서 MonoBehaviour 로 승격한다.

- [ ] **Step 2: 실패 확인**

Test Runner → Run All → `HealthTests` 4개 컴파일 실패.

- [ ] **Step 3: Health POCO 구현**

Create `Assets/_Lair/Scripts/Character/Health.cs`:
```csharp
using System;
using UnityEngine;

namespace Lair.Character
{
    //# IHealth 구현체. 본 슬라이스 한정 POCO + MonoBehaviour 양립.
    //# Unity 컴포넌트로 사용 시: GameObject 에 AddComponent.
    //# 테스트에서는 new Health() 직접 생성 후 SetMax 로 초기화.
    public class Health : MonoBehaviour, IHealth
    {
        [SerializeField] private int _max = 100;

        public int Max => _max;
        public int Current { get; private set; }
        public float Ratio => _max > 0 ? (float)Current / _max : 0f;
        public bool IsAlive => Current > 0;

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        //# MonoBehaviour 라이프사이클 — 인스펙터 _max 로 초기화.
        private void Awake()
        {
            Current = _max;
        }

        public void TakeDamage(int amount)
        {
            if (IsAlive == false) return;
            int next = Mathf.Max(0, Current - amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, _max);
            if (Current == 0) OnDied?.Invoke();
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            _max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, _max);
        }
    }
}
```

- [ ] **Step 4: 통과 확인**

Test Runner → Run All → 4/4 PASS.

**주의**: `new Health()` 가 MonoBehaviour 의 `Awake` 를 호출하지 않으므로 `Current` 가 0 으로 시작. 테스트의 `NewHealth` 헬퍼가 `SetMax(max)` 를 호출해 `Current=max` 로 초기화한다.

---

## Task 2.7: MeleeAttacker — TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Character/IAttacker.cs`
- Create: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/MeleeAttackerTests.cs`

- [ ] **Step 1: IAttacker 인터페이스**

Create `Assets/_Lair/Scripts/Character/IAttacker.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 사거리·쿨다운·데미지 적용을 한 곳에서 담당.
    public interface IAttacker
    {
        float Range { get; }
        float Cooldown { get; }
        int Power { get; }

        //# 거리·쿨다운 만족 시 target.TakeDamage 호출 후 true.
        bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now);
    }
}
```

> **참고**: MeleeAttacker 가 `Time.time` 을 직접 읽으면 테스트 어려움. `now` 인자로 받음 (의존성 주입 패턴).

- [ ] **Step 2: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Character/MeleeAttackerTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    public class MeleeAttackerTests
    {
        private static MeleeAttacker NewAttacker(float range = 1.5f, float cd = 1.0f, int power = 50)
        {
            var a = new MeleeAttacker();
            a.Configure(range, cd, power);
            return a;
        }

        [Test]
        public void 사거리_밖_TryAttack_거부()
        {
            var atk = NewAttacker(range: 1.0f);
            var target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(5, 0, 0), now: 0f);

            Assert.IsFalse(hit);
            Assert.AreEqual(0, target.DamageCallCount);
        }

        [Test]
        public void 사거리_내_쿨_0_데미지_Power_만큼_적용()
        {
            var atk = NewAttacker(range: 2.0f, power: 50);
            var target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsTrue(hit);
            Assert.AreEqual(1, target.DamageCallCount);
            Assert.AreEqual(50, target.LastDamage);
        }

        [Test]
        public void 쿨다운_중_재시도_거부()
        {
            var atk = NewAttacker(cd: 1.0f);
            var target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0.5f);

            Assert.IsFalse(hit);
            Assert.AreEqual(1, target.DamageCallCount);
        }

        [Test]
        public void 쿨다운_경과_후_재공격_가능()
        {
            var atk = NewAttacker(cd: 1.0f);
            var target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 1.5f);

            Assert.IsTrue(hit);
            Assert.AreEqual(2, target.DamageCallCount);
        }
    }
}
```

- [ ] **Step 3: 실패 확인**

Test Runner → Run All → 4개 실패.

- [ ] **Step 4: MeleeAttacker 구현**

Create `Assets/_Lair/Scripts/Character/MeleeAttacker.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 근접 공격. 인스펙터 또는 Configure 로 스탯 설정.
    public class MeleeAttacker : MonoBehaviour, IAttacker
    {
        [SerializeField] private float _range = 1.5f;
        [SerializeField] private float _cooldown = 1.0f;
        [SerializeField] private int _power = 50;

        public float Range => _range;
        public float Cooldown => _cooldown;
        public int Power => _power;

        private float _lastAttackTime = float.NegativeInfinity;

        //# 테스트 또는 런타임 동적 설정.
        public void Configure(float range, float cooldown, int power)
        {
            _range = range;
            _cooldown = cooldown;
            _power = power;
        }

        public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            if (target == null || target.IsAlive == false) return false;
            float dist = Vector3.Distance(selfPos, targetPos);
            if (dist > _range) return false;
            if (now - _lastAttackTime < _cooldown) return false;

            target.TakeDamage(_power);
            _lastAttackTime = now;
            return true;
        }
    }
}
```

- [ ] **Step 5: 통과 확인**

Test Runner → 4/4 PASS.

---

## Task 2.8: CharacterRegistry — TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Character/CharacterRegistry.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/CharacterRegistryTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Character/CharacterRegistryTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    public class CharacterRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [Test]
        public void Register_Unregister_Monsters()
        {
            var t = new GameObject("m1").transform;
            var h = new FakeHealth();
            CharacterRegistry.RegisterMonster(t, h);

            Assert.AreEqual(1, CharacterRegistry.Monsters.Count);

            CharacterRegistry.UnregisterMonster(t);
            Assert.AreEqual(0, CharacterRegistry.Monsters.Count);

            Object.DestroyImmediate(t.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_거리순_가장_가까운()
        {
            var near = new GameObject("near").transform; near.position = new Vector3(1, 0, 0);
            var far  = new GameObject("far").transform;  far.position  = new Vector3(5, 0, 0);
            CharacterRegistry.RegisterMonster(near, new FakeHealth());
            CharacterRegistry.RegisterMonster(far, new FakeHealth());

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var _);

            Assert.IsTrue(found);
            Assert.AreSame(near, t);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }

        [Test]
        public void TryFindNearestMonster_빈_레지스트리_false()
        {
            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var h);

            Assert.IsFalse(found);
            Assert.IsNull(t);
            Assert.IsNull(h);
        }

        [Test]
        public void TryFindNearestMonster_죽은_적_제외()
        {
            var alive = new GameObject("alive").transform; alive.position = new Vector3(5, 0, 0);
            var dead  = new GameObject("dead").transform;  dead.position  = new Vector3(1, 0, 0);
            var aliveHp = new FakeHealth();
            var deadHp = new FakeHealth();
            deadHp.ForceSetCurrent(0);
            CharacterRegistry.RegisterMonster(alive, aliveHp);
            CharacterRegistry.RegisterMonster(dead, deadHp);

            bool found = CharacterRegistry.TryFindNearestMonster(
                Vector3.zero, out var t, out var _);

            Assert.IsTrue(found);
            Assert.AreSame(alive, t, "더 가까운 dead 대신 alive 가 선택돼야 함");

            Object.DestroyImmediate(alive.gameObject);
            Object.DestroyImmediate(dead.gameObject);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Test Runner → Run All → 4개 실패.

- [ ] **Step 3: 구현**

Create `Assets/_Lair/Scripts/Character/CharacterRegistry.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅·몬스터 정적 레지스트리. 캐릭터의 OnEnable/OnDisable 에서 자기 등록.
    //# TryFindNearest 는 IsAlive 필터링 + 거리순 정렬 후 최근접 1개 반환.
    public static class CharacterRegistry
    {
        public class Entry
        {
            public Transform Transform;
            public IHealth Health;
        }

        public static readonly List<Entry> Heroes = new();
        public static readonly List<Entry> Monsters = new();

        public static void RegisterHero(Transform t, IHealth h)   => Add(Heroes, t, h);
        public static void UnregisterHero(Transform t)            => Remove(Heroes, t);
        public static void RegisterMonster(Transform t, IHealth h)=> Add(Monsters, t, h);
        public static void UnregisterMonster(Transform t)         => Remove(Monsters, t);

        public static bool TryFindNearestHero(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Heroes, from, out t, out h);
        public static bool TryFindNearestMonster(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Monsters, from, out t, out h);

        private static void Add(List<Entry> list, Transform t, IHealth h)
        {
            if (t == null) return;
            list.Add(new Entry { Transform = t, Health = h });
        }

        private static void Remove(List<Entry> list, Transform t)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].Transform == t) list.RemoveAt(i);
            }
        }

        private static bool TryFindNearest(
            List<Entry> list, Vector3 from, out Transform t, out IHealth h)
        {
            t = null; h = null;
            float best = float.MaxValue;
            foreach (var e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                float d = (e.Transform.position - from).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    t = e.Transform;
                    h = e.Health;
                }
            }
            return t != null;
        }
    }
}
```

- [ ] **Step 4: 통과 확인**

Test Runner → Run All → 4/4 PASS.

---

## Task 2.9: M2 검증 + 스테이지

- [ ] **Step 1: 전체 EditMode 테스트 실행**

Test Runner → EditMode → Run All.

Expected: **총 20개 PASS** (`BattleClock` 4 + `BattleViewModel` 4 + `Health` 4 + `MeleeAttacker` 4 + `CharacterRegistry` 4).

테스트 카운트가 다르거나 실패가 있으면 해당 태스크로 되돌아가서 수정.

- [ ] **Step 2: 변경 사항 스테이지**

```powershell
git add Assets/_Lair/Scripts Assets/_Lair/Tests
git status --short
```

Expected: 새 `.cs` 파일과 `.meta` 들이 staged.

- [ ] **Step 3: 커밋 메시지(안) 출력 — Rule 01**

```
# [feat] - 전투 POCO 로직 + EditMode 테스트 20개
```

**M2 검증 게이트**: EditMode 테스트 20개 전부 녹색. POCO 클래스(`BattleClock`, `BattleStateModel`, `BattleViewModel`)는 Unity 의존성 없음. `Health`/`MeleeAttacker` 는 MB 지만 테스트는 순수 로직만 검증.

---

# M3 — 캐릭터 자동전투 (~2~3시간)

## Task 3.1: IMover / ITargetProvider 인터페이스

**Files:**
- Create: `Assets/_Lair/Scripts/Character/IMover.cs`
- Create: `Assets/_Lair/Scripts/Character/ITargetProvider.cs`

- [ ] **Step 1: IMover**

Create `Assets/_Lair/Scripts/Character/IMover.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 위치 이동 추상. 구현체 교체로 Vector3 추적 → NavMesh 전환 가능.
    public interface IMover
    {
        float Speed { get; set; }
        void MoveTo(Vector3 target);
        void Stop();
    }
}
```

- [ ] **Step 2: ITargetProvider**

Create `Assets/_Lair/Scripts/Character/ITargetProvider.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# AutoCombatAI 에 적 검색 전략을 주입. Hero/Monster 의 유일한 차이점.
    public interface ITargetProvider
    {
        bool TryFindNearest(Vector3 from, out Transform target, out IHealth health);
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity 콘솔 에러 0.

---

## Task 3.2: SimpleMover

**Files:**
- Create: `Assets/_Lair/Scripts/Character/SimpleMover.cs`

- [ ] **Step 1: 구현**

Create `Assets/_Lair/Scripts/Character/SimpleMover.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# Vector3.MoveTowards 기반 단순 추적. 장애물 회피 없음. Slice A 한정.
    public class SimpleMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed = 3f;
        private bool _moving;
        private Vector3 _target;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public void MoveTo(Vector3 target)
        {
            _target = target;
            _moving = true;
        }

        public void Stop()
        {
            _moving = false;
        }

        private void Update()
        {
            if (_moving == false) return;
            transform.position = Vector3.MoveTowards(
                transform.position, _target, _speed * Time.deltaTime);

            //# Y 평면 고정 — 캐릭터가 카메라 각도로 떠오르지 않도록
            var p = transform.position;
            transform.position = new Vector3(p.x, 0, p.z);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity 콘솔 에러 0.

(SimpleMover 는 thin wrapper. EditMode 단위 테스트는 생략. M3 끝에 수동 검증.)

---

## Task 3.3: TargetProvider 2종

**Files:**
- Create: `Assets/_Lair/Scripts/Character/HeroTargetProvider.cs`
- Create: `Assets/_Lair/Scripts/Character/MonsterTargetProvider.cs`

- [ ] **Step 1: HeroTargetProvider**

Create `Assets/_Lair/Scripts/Character/HeroTargetProvider.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 영웅용 — Monsters 레지스트리에서 최근접 살아있는 적 검색.
    //# OnEnable 시 자기 자신을 Heroes 레지스트리에 등록.
    public class HeroTargetProvider : MonoBehaviour, ITargetProvider
    {
        private IHealth _selfHealth;

        private void Awake() => _selfHealth = GetComponent<IHealth>();

        private void OnEnable()
        {
            if (_selfHealth != null)
                CharacterRegistry.RegisterHero(transform, _selfHealth);
        }

        private void OnDisable()
        {
            CharacterRegistry.UnregisterHero(transform);
        }

        public bool TryFindNearest(Vector3 from, out Transform target, out IHealth health)
            => CharacterRegistry.TryFindNearestMonster(from, out target, out health);
    }
}
```

- [ ] **Step 2: MonsterTargetProvider**

Create `Assets/_Lair/Scripts/Character/MonsterTargetProvider.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터용 — Heroes 레지스트리에서 영웅 검색.
    //# OnEnable 시 자기 자신을 Monsters 레지스트리에 등록.
    public class MonsterTargetProvider : MonoBehaviour, ITargetProvider
    {
        private IHealth _selfHealth;

        private void Awake() => _selfHealth = GetComponent<IHealth>();

        private void OnEnable()
        {
            if (_selfHealth != null)
                CharacterRegistry.RegisterMonster(transform, _selfHealth);
        }

        private void OnDisable()
        {
            CharacterRegistry.UnregisterMonster(transform);
        }

        public bool TryFindNearest(Vector3 from, out Transform target, out IHealth health)
            => CharacterRegistry.TryFindNearestHero(from, out target, out health);
    }
}
```

- [ ] **Step 3: 컴파일 확인**

---

## Task 3.4: AutoCombatAI

**Files:**
- Create: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`

- [ ] **Step 1: 구현**

Create `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`:
```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 자동전투 행동 — 인터페이스 4개 조합으로만 동작.
    //# 영웅/몬스터 공통. ITargetProvider 구현체로 진영이 결정됨.
    [RequireComponent(typeof(SimpleMover))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(MeleeAttacker))]
    public class AutoCombatAI : MonoBehaviour
    {
        private IMover _mover;
        private IHealth _health;
        private IAttacker _attacker;
        private ITargetProvider _targetProvider;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _health = GetComponent<IHealth>();
            _attacker = GetComponent<IAttacker>();
            _targetProvider = GetComponent<ITargetProvider>();
        }

        private void Update()
        {
            if (_health == null || _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }
            if (_targetProvider == null) return;

            if (_targetProvider.TryFindNearest(transform.position, out var t, out var th) == false)
            {
                _mover.Stop();
                return;
            }

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist <= _attacker.Range)
            {
                _mover.Stop();
                _attacker.TryAttack(th, transform.position, t.position, Time.time);
            }
            else
            {
                _mover.MoveTo(t.position);
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인 + 기존 EditMode 테스트 재실행**

Test Runner → EditMode → Run All. Expected: 여전히 20/20 PASS.

---

## Task 3.5: Knight.prefab 생성

Unity 에디터 작업.

- [ ] **Step 1: 베이스 GameObject 생성**

1. Hierarchy 빈 곳 우클릭 → `3D Object → Capsule` → 이름 `Knight`
2. Transform: Position `(0,0,0)`, Rotation `(0,0,0)`, Scale `(1,1,1)`

- [ ] **Step 2: 머티리얼 색상 적용**

1. `Assets/_Lair/Prefabs/Characters/` 우클릭 → `Create → Material` → 이름 `Mat_Knight`
2. 인스펙터에서 Shader: `Universal Render Pipeline/Lit`, Base Map 색상 `#3B82F6`
3. Mat_Knight 를 Hierarchy 의 Knight 에 드래그

- [ ] **Step 3: 컴포넌트 추가**

Knight 선택 후 인스펙터 `Add Component`:
1. `Lair.Character.SimpleMover` — Speed = `3`
2. `Lair.Character.Health` — Max = `1000`
3. `Lair.Character.MeleeAttacker` — Range = `1.5`, Cooldown = `1.0`, Power = `50`
4. `Lair.Character.HeroTargetProvider`
5. `Lair.Character.AutoCombatAI`
6. (자동으로 추가됨) Capsule Collider 는 그대로 두거나 제거. 본 슬라이스는 충돌 사용 안 함 — **제거 권장**.

- [ ] **Step 4: 프리팹화**

Knight GameObject 를 `Assets/_Lair/Prefabs/Characters/Knight.prefab` 으로 드래그 (Original Prefab).

- [ ] **Step 5: Hierarchy 에서 제거**

씬에 남은 Knight 인스턴스를 우클릭 → Delete (프리팹만 남기고 씬에서는 제거).

- [ ] **Step 6: 파일명 확인 — Rule 08**

`Assets/_Lair/Prefabs/Characters/Knight.prefab` 파일명이 `EHero.Knight.ToString()` 과 정확히 일치하는지 확인 (`Knight`).

---

## Task 3.6: Slime.prefab 생성

- [ ] **Step 1**: Hierarchy 우클릭 → `3D Object → Sphere` → 이름 `Slime`
- [ ] **Step 2**: Scale `(0.6, 0.6, 0.6)`, Position `(0,0,0)`
- [ ] **Step 3**: Material `Mat_Slime` (색 `#22C55E`) 적용
- [ ] **Step 4**: 컴포넌트
  - SimpleMover (Speed=`1.5`)
  - Health (Max=`200`)
  - MeleeAttacker (Range=`1.0`, Cooldown=`1.0`, Power=`10`)
  - MonsterTargetProvider
  - AutoCombatAI
  - SphereCollider 제거
- [ ] **Step 5**: 프리팹화 → `Assets/_Lair/Prefabs/Characters/Slime.prefab`
- [ ] **Step 6**: Hierarchy 에서 제거

---

## Task 3.7: Golem.prefab 생성

- [ ] **Step 1**: Hierarchy 우클릭 → `3D Object → Cube` → 이름 `Golem`
- [ ] **Step 2**: Scale `(1.2, 1.2, 1.2)`
- [ ] **Step 3**: Material `Mat_Golem` (`#6B7280`)
- [ ] **Step 4**: 컴포넌트
  - SimpleMover (Speed=`0.8`)
  - Health (Max=`500`)
  - MeleeAttacker (Range=`1.3`, Cooldown=`1.0`, Power=`20`)
  - MonsterTargetProvider
  - AutoCombatAI
  - BoxCollider 제거
- [ ] **Step 5**: 프리팹화 → `Golem.prefab`
- [ ] **Step 6**: Hierarchy 에서 제거

---

## Task 3.8: Orc.prefab 생성

- [ ] **Step 1**: Capsule → 이름 `Orc`, Scale `(0.9, 0.9, 0.9)`
- [ ] **Step 2**: Material `Mat_Orc` (`#EF4444`)
- [ ] **Step 3**: 컴포넌트
  - SimpleMover (Speed=`2.5`)
  - Health (Max=`100`)
  - MeleeAttacker (Range=`1.0`, Cooldown=`0.5`, Power=`20`)
  - MonsterTargetProvider
  - AutoCombatAI
  - CapsuleCollider 제거
- [ ] **Step 4**: 프리팹화 → `Orc.prefab`
- [ ] **Step 5**: Hierarchy 제거

---

## Task 3.9: Addressables 등록 (Rule 08)

- [ ] **Step 1: 4개 프리팹을 Addressable 로 표시**

각 프리팹(`Knight`, `Slime`, `Golem`, `Orc`)을 Project 창에서 선택 → 인스펙터 상단 **Addressable** 체크박스 ON.

- [ ] **Step 2: 그룹 이동**

`Window → Asset Management → Addressables → Groups` 열고 4개 프리팹을 `Resource` 그룹으로 드래그 (이미 자동 들어가 있을 수도).

- [ ] **Step 3: 주소를 파일명만으로 정규화**

각 엔트리의 `Address` 컬럼이 다음과 정확히 일치하는지 확인:
- `Knight`
- `Slime`
- `Golem`
- `Orc`

기본값이 `Assets/_Lair/Prefabs/Characters/Knight.prefab` 같이 전체 경로면, 우클릭 → `Change Address` → 파일명만 입력.

- [ ] **Step 4: 라벨 부여**

각 엔트리를 다중 선택 → `Labels` 드롭다운 → `Resource` 체크.

- [ ] **Step 5: 변경 사항 저장**

Addressables Groups 창에서 변경이 자동 저장됨. `Assets/AddressableAssetsData/` 파일 갱신 확인.

---

## Task 3.10: M3 수동 1대1 검증 + 스테이지

- [ ] **Step 1: 임시 테스트 씬 구성**

Battle 씬 열고 임시로:
1. 빈 GameObject 생성 → `EventSystem` 자동 안 생기면 `GameObject → UI → Event System` 추가
2. Knight 프리팹을 씬에 드래그 — Position `(-2, 0, 0)`
3. Slime 프리팹을 씬에 드래그 — Position `(2, 0, 0)`

- [ ] **Step 2: Play 후 동작 확인**

Play 버튼 → 다음 모두 확인:
- [ ] Knight 가 Slime 방향으로 이동
- [ ] 사거리 도달 시 멈춤
- [ ] 1초마다 Slime HP 줄어드는 모습 (Knight 의 Power 50 × 쿨다운 1초 → Slime HP 200 이 4초만에 0)
- [ ] Slime 사망 후 Knight 가 정지 (다른 적 없음)

- [ ] **Step 3: 임시 씬 정리**

테스트용 Knight/Slime/EventSystem 인스턴스를 Hierarchy 에서 제거. 씬 저장.

- [ ] **Step 4: 스테이지**

```powershell
git add Assets/_Lair Assets/AddressableAssetsData
git status --short
```

- [ ] **Step 5: 커밋 메시지(안)**

```
# [feat] - 캐릭터 컴포지션 + 자동전투 AI + 4종 프리팹 + Addressables 등록
```

**M3 검증 게이트**: Knight 1 vs Slime 1 → Knight 가 자동 이동·공격으로 Slime 처치, 그 후 정지. 콘솔 에러 0.

**🛑 사용자 검토 포인트**: 자동전투의 페이싱(이동 속도, 공격 텀)이 느낌상 OK인지. 필요 시 프리팹 스탯 튜닝.

---

# M4 — HUD MVVM (~2~3시간)

## Task 4.1: BattleHudArg + BattleHud.cs

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BattleHudArg.cs`
- Create: `Assets/_Lair/Scripts/UI/BattleHud.cs`

- [ ] **Step 1: BattleHudArg**

Create `Assets/_Lair/Scripts/UI/BattleHudArg.cs`:
```csharp
using ChvjUnityInfra;

namespace Lair.UI
{
    //# CHMUI.ShowUI 호출 시 BattleHud 에 ViewModel 주입용.
    public class BattleHudArg : UIArg
    {
        public BattleViewModel ViewModel;
    }
}
```

- [ ] **Step 2: BattleHud**

Create `Assets/_Lair/Scripts/UI/BattleHud.cs`:
```csharp
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# CHMUI 로 띄워지는 HUD. UIArg 통해 ViewModel 주입받아 구독.
    //# 구독 해제는 UIBase.closeDisposable 활용 (Close 시 자동 정리).
    public class BattleHud : UIBase
    {
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _heroHpFill;

        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            if (arg is BattleHudArg ba && ba.ViewModel != null)
                Bind(ba.ViewModel);
        }

        private void Bind(BattleViewModel vm)
        {
            _vm = vm;
            vm.OnTimerChanged       += HandleTimer;
            vm.OnHeroHpRatioChanged += HandleHp;
            vm.OnBattleEnded        += HandleEnded;

            //# Close 시 자동 해제
            closeDisposable.Add(() => vm.OnTimerChanged       -= HandleTimer);
            closeDisposable.Add(() => vm.OnHeroHpRatioChanged -= HandleHp);
            closeDisposable.Add(() => vm.OnBattleEnded        -= HandleEnded);

            //# 초기 동기화
            HandleTimer(vm.ElapsedSeconds, vm.TotalSeconds);
            HandleHp(vm.HeroHpRatio);
        }

        private void HandleTimer(float elapsed, float total)
        {
            if (_timerText == null) return;
            float remain = Mathf.Max(0f, total - elapsed);
            _timerText.text = $"{(int)(remain / 60)}:{(int)(remain % 60):00}";
        }

        private void HandleHp(float ratio)
        {
            if (_heroHpFill == null) return;
            _heroHpFill.fillAmount = ratio;
        }

        private void HandleEnded(BattleResult result)
        {
            //# HUD 는 자기 표시만 — ResultPopup 은 BattleController 가 직접 띄움.
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

---

## Task 4.2: BattleHud.prefab 생성

Unity 에디터.

- [ ] **Step 1: Canvas + EventSystem 임시 배치**

씬에 `GameObject → UI → Canvas` 추가 (Render Mode: Screen Space - Overlay).
필요 시 EventSystem 도 추가.

- [ ] **Step 2: BattleHud 루트 생성**

Canvas 하위에 빈 GameObject → 이름 `BattleHud`. RectTransform anchor 를 Full Stretch.

- [ ] **Step 3: 타이머 텍스트**

`BattleHud` 자식으로 `UI → Legacy → Text` 추가 → 이름 `TimerText`.
- 위치: 상단 중앙 (Anchor Top-Center, Position `(0, -40, 0)`)
- 폰트 크기: 48, 색상: 흰색 `#FFFFFF`
- 정렬: Center / Middle
- 초기 텍스트: `5:00`

- [ ] **Step 4: HP 바**

`BattleHud` 자식으로:
1. `UI → Image` → 이름 `HpBg` (배경)
   - 색상 `#374151`, 크기 `(300, 20)`
   - 위치: 화면 상단, 타이머 아래 `(0, -90, 0)`
2. `HpBg` 자식으로 `UI → Image` → 이름 `HpFill`
   - 색상 `#DC2626`
   - Image Type: Filled, Fill Method: Horizontal, Fill Origin: Left
   - 동일 크기 (`(300, 20)`)
   - Fill Amount: 1

- [ ] **Step 5: Lair.UI.BattleHud 컴포넌트 부착**

`BattleHud` GameObject 에 `Lair.UI.BattleHud` 추가:
- `_timerText` 필드에 `TimerText` 드래그
- `_heroHpFill` 필드에 `HpFill` 드래그

(BackgroundButton/BackButton 필드는 비워둠 — Slice A 엔 ESC 닫기 없음.)

- [ ] **Step 6: 프리팹화**

`BattleHud` GameObject 를 `Assets/_Lair/Prefabs/UI/BattleHud.prefab` 으로 드래그.

- [ ] **Step 7: 임시 Canvas/BattleHud 인스턴스 제거**

씬에서 임시로 추가한 Canvas/EventSystem 둘 다 삭제 (M5에서 다시 추가).

- [ ] **Step 8: 파일명 = `EUI.BattleHud`(Rule 08) 확인**

---

## Task 4.3: ResultPopupArg + ResultPopup.cs

**Files:**
- Create: `Assets/_Lair/Scripts/UI/ResultPopupArg.cs`
- Create: `Assets/_Lair/Scripts/UI/ResultPopup.cs`

- [ ] **Step 1: ResultPopupArg**

Create `Assets/_Lair/Scripts/UI/ResultPopupArg.cs`:
```csharp
using ChvjUnityInfra;

namespace Lair.UI
{
    public class ResultPopupArg : UIArg
    {
        public BattleResult Result;
    }
}
```

- [ ] **Step 2: ResultPopup**

Create `Assets/_Lair/Scripts/UI/ResultPopup.cs`:
```csharp
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 결과 표시 + 재시작 버튼.
    public class ResultPopup : UIBase
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _restartButton;

        public override void InitUI(UIArg arg)
        {
            if (arg is ResultPopupArg rp)
            {
                _resultText.text = rp.Result switch
                {
                    BattleResult.Win  => "승리",
                    BattleResult.Lose => "패배",
                    _                 => "-"
                };
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(OnClickRestart);
            }
        }

        private void OnClickRestart()
        {
            //# Rule 08 — EScene.Battle.ToString() == "Battle" 씬 파일명과 일치
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

---

## Task 4.4: ResultPopup.prefab 생성

- [ ] **Step 1: 임시 Canvas 추가**

`GameObject → UI → Canvas` (Screen Space - Overlay).

- [ ] **Step 2: ResultPopup 루트**

Canvas 하위 빈 GameObject → 이름 `ResultPopup`. Anchor Full Stretch.

- [ ] **Step 3: 반투명 배경**

`ResultPopup` 자식 → `UI → Image` → 이름 `Dim`
- Color: `#00000080` (검정 50%)
- Anchor Full Stretch

- [ ] **Step 4: 결과 텍스트**

`ResultPopup` 자식 → `UI → Legacy → Text` → 이름 `ResultText`
- 위치 화면 중앙
- 폰트 크기 96, 색상 흰색
- 정렬 Center/Middle
- 초기 텍스트 "결과"

- [ ] **Step 5: 재시작 버튼**

`ResultPopup` 자식 → `UI → Legacy → Button` → 이름 `RestartButton`
- 위치 결과 텍스트 아래 (Anchor Center, `(0, -120, 0)`)
- 자식 Text 의 텍스트 "다시 시작"

- [ ] **Step 6: Lair.UI.ResultPopup 컴포넌트 부착**

`ResultPopup` GameObject 에 `Lair.UI.ResultPopup` 추가:
- `_resultText` → `ResultText` 드래그
- `_restartButton` → `RestartButton` 드래그
- `_backgroundButton` (UIBase 슬롯) — 비워둠

- [ ] **Step 7: 프리팹화**

`ResultPopup` → `Assets/_Lair/Prefabs/UI/ResultPopup.prefab`.

- [ ] **Step 8: 임시 Canvas/EventSystem 제거**

---

## Task 4.5: UI 프리팹 Addressables 등록

- [ ] **Step 1**: `BattleHud.prefab` / `ResultPopup.prefab` 인스펙터에서 Addressable 체크
- [ ] **Step 2**: 그룹 `Resource` 로 이동
- [ ] **Step 3**: Address 가 정확히 `BattleHud`, `ResultPopup` 인지 확인
- [ ] **Step 4**: 라벨 `Resource` 부여

---

## Task 4.6: M4 검증 (임시 드라이버)

UI 가 실제로 뜨고 데이터를 받는지 임시 BattleController 로 검증.

**Files:**
- Create (임시): `Assets/_Lair/Scripts/Battle/BattleController.cs` (다음 M5 에서 완성)

- [ ] **Step 1: 임시 BattleController 작성**

Create `Assets/_Lair/Scripts/Battle/BattleController.cs`:
```csharp
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# M4 임시 드라이버 — HUD 표시 검증용. M5 에서 완성됨.
    public class BattleController : MonoBehaviour
    {
        private BattleStateModel _model;
        private BattleViewModel _vm;
        private float _fakeTimer;
        private bool _ready;

        async void Start()
        {
            if (await CHMResource.Instance.Init() == false) return;
            CHMUI.Instance.Init();

            _model = new BattleStateModel();
            _vm = new BattleViewModel(_model);

            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm });

            _vm.UpdateHeroHp(1000, 1000);   //# 풀 HP
            _ready = true;
        }

        void Update()
        {
            if (_ready == false) return;
            _fakeTimer += Time.deltaTime;
            _vm.UpdateTimer(_fakeTimer);
        }
    }
}
```

- [ ] **Step 2: Battle 씬 임시 셋업**

씬에서:
1. `GameObject → UI → Canvas` 추가 → Tag `UICanvas` 설정 (없으면 새 Tag 추가)
2. EventSystem 자동 생성 확인
3. 빈 GameObject `@Battle` → 자식 `BattleController` → Lair.Battle.BattleController 컴포넌트 추가

- [ ] **Step 3: Play 후 동작 확인**

Play → 다음 모두 확인:
- [ ] HUD 자동 표시 (콘솔에 Addressables / UI 관련 에러 없음)
- [ ] 타이머가 `5:00` 부터 시작해서 1초마다 감소
- [ ] HP 바가 가득 차 있음

- [ ] **Step 4: 스테이지**

```powershell
git add Assets/_Lair Assets/AddressableAssetsData
```

- [ ] **Step 5: 커밋 메시지(안)**

```
# [feat] - BattleHud / ResultPopup MVVM 바인딩 + 임시 BattleController
```

**M4 검증 게이트**: HUD 가 자동 표시되고 타이머/HP 가 ViewModel 갱신과 동기화.

---

# M5 — 풀 배틀 루프 (~1~2시간)

## Task 5.1: MonsterSpawnEntry struct

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/MonsterSpawnEntry.cs`

- [ ] **Step 1: 구조체 작성**

Create `Assets/_Lair/Scripts/Battle/MonsterSpawnEntry.cs`:
```csharp
using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# BattleController 인스펙터에서 (위치, 키) 쌍 직렬화용.
    [Serializable]
    public struct MonsterSpawnEntry
    {
        public Transform Point;
        public EMonster Key;
    }
}
```

---

## Task 5.2: BattleController 완성

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: 전체 교체**

Replace `Assets/_Lair/Scripts/Battle/BattleController.cs` 전체 내용:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# 전투 씬 라이프사이클·스폰·VM 갱신·종료 처리.
    public class BattleController : MonoBehaviour
    {
        [SerializeField] private Transform _heroSpawn;
        [SerializeField] private MonsterSpawnEntry[] _monsterSpawns;

        private BattleClock _clock;
        private BattleStateModel _model;
        private BattleViewModel _vm;

        private CHPoolable _hero;
        private Health _heroHealth;
        private readonly List<CHPoolable> _monsters = new();

        async void Start()
        {
            //# 1. ChvjPackage 초기화
            if (await CHMResource.Instance.Init() == false)
            {
                Debug.LogError("[BattleController] CHMResource.Init 실패");
                return;
            }
            CHMUI.Instance.Init();
            CHMPool.Instance.Init();

            //# 2. MVVM 준비
            _model = new BattleStateModel();
            _vm = new BattleViewModel(_model);

            //# 3. HUD
            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm });

            //# 4. 스폰
            await SpawnHero();
            await SpawnMonsters();

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            _clock.Start();
        }

        private void Update()
        {
            _clock?.Tick(Time.deltaTime);
        }

        private async Task SpawnHero()
        {
            var prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (prefab == null)
            {
                Debug.LogError("[BattleController] Knight 프리팹 로드 실패");
                return;
            }

            var p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null) return;
            p.transform.position = _heroSpawn != null ? _heroSpawn.position : Vector3.zero;

            _hero = p;
            _heroHealth = p.GetComponent<Health>();
            if (_heroHealth != null)
            {
                _heroHealth.OnChanged += _vm.UpdateHeroHp;
                _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
                _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
            }
        }

        private async Task SpawnMonsters()
        {
            if (_monsterSpawns == null) return;
            foreach (var sp in _monsterSpawns)
            {
                if (sp.Point == null) continue;
                var prefab = await CHMResource.Instance.LoadAsync<GameObject>(sp.Key);
                if (prefab == null) continue;
                var p = CHMPool.Instance.Pop(prefab, transform);
                if (p == null) continue;
                p.transform.position = sp.Point.position;
                _monsters.Add(p);
            }
        }

        private async void EndBattle(BattleResult result)
        {
            if (_model.Result != BattleResult.None) return;
            _clock.Stop();

            //# 모든 AI 정지
            foreach (var ai in GetComponentsInChildren<AutoCombatAI>())
                ai.enabled = false;

            _vm.EndBattle(result);

            await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup,
                new ResultPopupArg { Result = result });
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

콘솔 에러 0.

---

## Task 5.3: Battle.unity 씬 본격 구성

Unity 에디터.

- [ ] **Step 1: 카메라 설정**

기존 `Main Camera` 선택:
- 부모 빈 GameObject `@Camera` 생성 후 그 자식으로 이동
- Position `(0, 12, -8)`, Rotation `(50, 0, 0)`
- Camera Projection: Perspective, FOV: 60
- Camera Clear Flags: Solid Color, Background: `#1F2937`

- [ ] **Step 2: 조명**

Hierarchy `Directional Light` 선택:
- 부모 빈 GameObject `@Light` 그룹 아래로 이동
- Rotation `(50, -30, 0)`, Intensity 1

- [ ] **Step 3: 바닥**

빈 GameObject `@Stage` 생성:
- 자식으로 `3D Object → Plane` → 이름 `Floor`
- Scale `(3, 1, 3)` (= 30×30)
- Material `Mat_Floor` (URP Lit, Base Color `#374151`) 생성 후 적용

- [ ] **Step 4: UICanvas**

빈 GameObject `@UI` 생성:
- 자식으로 `GameObject → UI → Canvas` → 이름 `UICanvas`
- Tag: `UICanvas` (없으면 `Edit → Project Settings → Tags and Layers` 에서 추가)
- Canvas Scaler: UI Scale Mode = `Scale With Screen Size`, Reference Resolution `1920×1080`, Match = 0.5
- 자식 EventSystem 자동 생성 (없으면 `GameObject → UI → Event System`)

- [ ] **Step 5: @Battle 그룹 + 스폰 포인트**

빈 GameObject `@Battle` 생성:
- 자식 `BattleController` (빈 GameObject) → `Lair.Battle.BattleController` 컴포넌트 부착
- `BattleController` 자식들:
  - `HeroSpawn` (빈 GameObject) Position `(0, 0, -8)`
  - `MonsterSpawn_01` Position `(-3, 0, 5)`
  - `MonsterSpawn_02` Position `(0, 0, 6)`
  - `MonsterSpawn_03` Position `(3, 0, 5)`

- [ ] **Step 6: BattleController 인스펙터 바인딩**

`BattleController` 선택:
- `_heroSpawn` → `HeroSpawn` 드래그
- `_monsterSpawns` → Size 3, 각각:
  - [0] Point = MonsterSpawn_01, Key = Slime
  - [1] Point = MonsterSpawn_02, Key = Golem
  - [2] Point = MonsterSpawn_03, Key = Orc

- [ ] **Step 7: 씬 저장**

`Ctrl+S`.

---

## Task 5.4: M5 풀 플레이 검증 + 스테이지

- [ ] **Step 1: Play → 풀 루프 검증**

Battle 씬에서 Play 버튼. 다음 시나리오를 각각 확인:

**시나리오 A — 영웅이 이김(시간 내)**
1. 영웅 + 몬스터 3마리 자동 스폰
2. 영웅이 가장 가까운 몬스터 추적·공격
3. 몬스터 전부 처치되도록 한참 두기
4. 처치 후에는 영웅이 멈춤 (적 없음) → 5:00 도달
5. ResultPopup "패배" 표시 (영웅이 살아남았으니 던전 주인 입장에선 패배)

> 잠깐, 시나리오 A는 패배가 맞다. 영웅이 던전을 5분 동안 살아서 통과 = 던전 함락 = 우리 패배.

**시나리오 B — 시간 초과 패배**
1. 위와 동일 시작
2. 영웅이 끝까지 안 죽으면 5:00 에 ResultPopup "패배"

**시나리오 C — 영웅 죽임 = 승리**
영웅 HP 1000 vs 몬스터 DPS 합산이 부족할 수 있음. 빠른 검증을 위해 임시로 영웅 HP 를 50 정도로 줄이고(`Knight.prefab` 인스펙터에서 Health.Max=50) Play.
1. 영웅 빠르게 사망
2. ResultPopup "승리"

검증 후 Knight.Max 를 다시 1000 으로 원복.

**시나리오 D — 재시작**
ResultPopup 의 "다시 시작" 클릭 → 씬 재로드 → 위 흐름 재현.

- [ ] **Step 2: 발견된 이슈 수정**

스폰 위치/각도가 화면 밖, 영웅이 안 움직임, HUD 가 안 떠 등 — 발견되는 즉시 수정 후 재테스트.

흔한 문제:
- Y 위치 음수/양수로 떠서 안 보임 → SimpleMover 의 `transform.position = new Vector3(p.x, 0, p.z)` 작동 확인. 스폰 포인트의 Y 도 0 으로.
- HUD 가 안 뜸 → UICanvas Tag 가 `UICanvas` 인지, EventSystem 있는지.
- ResultPopup 클릭이 안 됨 → ResultPopup Canvas 가 UICanvas 와 같은 캔버스에 자식으로 들어가는지(CHMUI 가 알아서 처리).

- [ ] **Step 3: 스테이지**

```powershell
git add Assets/_Lair Assets/AddressableAssetsData
git status --short
```

- [ ] **Step 4: 커밋 메시지(안)**

```
# [feat] - BattleController 풀 루프 (스폰/시계/판정/재시작) + Battle 씬 본격 구성
```

**M5 검증 게이트**: 한 판 풀 플레이 가능 — 영웅 사망 = 승리, 5:00 타임아웃 = 패배, 재시작 정상.

**🛑 사용자 검토 포인트**: 한 판 플레이가 "압박감 / 보는 즐거움" 측면에서 검증 가설을 충족하는지. 안 되면 스탯 튜닝 또는 슬라이스 B로 넘어가지 말고 페이싱 재조정.

---

# M6 — 테스트 + 수동 체크리스트 (~1~2시간)

## Task 6.1: Lair.Tests.PlayMode.asmdef

**Files:**
- Create dir: `Assets/_Lair/Tests/PlayMode/`
- Create: `Assets/_Lair/Tests/PlayMode/Lair.Tests.PlayMode.asmdef`

- [ ] **Step 1: 폴더 + asmdef**

```powershell
New-Item -ItemType Directory -Force "Assets/_Lair/Tests/PlayMode" | Out-Null
```

Create `Assets/_Lair/Tests/PlayMode/Lair.Tests.PlayMode.asmdef`:
```json
{
    "name": "Lair.Tests.PlayMode",
    "rootNamespace": "Lair.Tests.PlayMode",
    "references": [
        "Lair",
        "ChvjUnityInfra",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Test Runner 의 PlayMode 탭에서 인식 확인**

---

## Task 6.2: BattleSmokeTest

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/BattleSmokeTest.cs`

- [ ] **Step 1: smoke test 작성**

Create `Assets/_Lair/Tests/PlayMode/BattleSmokeTest.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    public class BattleSmokeTest
    {
        [UnityTest]
        public IEnumerator Battle씬_로드_5초후_캐릭터_살아있음()
        {
            //# 씬 로드 (Build Settings 에 Battle 이 Index 0 이므로 이름으로 로드)
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# 5초 시뮬레이션 대기 — Time.timeScale 그대로
            float elapsed = 0f;
            while (elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# Heroes/Monsters 레지스트리에 등록된 살아있는 캐릭터 존재 확인
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "영웅이 레지스트리에 등록돼야 함");
            Assert.Greater(CharacterRegistry.Monsters.Count, 0,
                "몬스터가 레지스트리에 등록돼야 함");

            //# 영웅이 살아있어야 (5초 안에 안 죽음)
            bool heroAlive = false;
            foreach (var e in CharacterRegistry.Heroes)
            {
                if (e.Health != null && e.Health.IsAlive) { heroAlive = true; break; }
            }
            Assert.IsTrue(heroAlive, "5초 시점에 영웅 1명 이상 살아있어야 함");

            //# 정리 — Battle 씬을 다시 로드하면 레지스트리가 OnDisable 로 정리됨
            yield return null;
        }
    }
}
```

- [ ] **Step 2: 빌드 세팅의 Battle 씬 인덱스 확인**

`File → Build Settings` 에서 `_Lair/Scenes/Battle.unity` 가 체크되어 있는지. SceneManager.LoadSceneAsync 가 이름으로 찾으려면 Build Settings 에 포함돼야 함.

- [ ] **Step 3: 실행**

Test Runner → PlayMode → Run All. Expected: 1/1 PASS (실행 시간 ~7초).

실패 시:
- "Scene not found" → Build Settings 에서 Battle 씬 누락
- Hero/Monster 레지스트리 0 → BattleController 가 정상 스폰 못함 (Addressables 키 미일치 가능)

---

## Task 6.3: 수동 체크리스트 9개 통과

Battle 씬에서 Play 후 다음 9개 항목을 한 판으로 모두 확인.

- [ ] (1) 씬 진입 1초 이내 캐릭터 4개(영웅 1 + 몬스터 3) 표시
- [ ] (2) 영웅이 가장 가까운 몬스터로 자동 이동
- [ ] (3) 사거리 도달 시 정지·공격 반복
- [ ] (4) HP 바 실시간 감소
- [ ] (5) 타이머 5:00 → 0:00 카운트다운
- [ ] (6) 영웅 사망 시 ResultPopup "승리" (영웅 HP 임시 50 으로 줄여 검증)
- [ ] (7) 5분 도달 시 ResultPopup "패배" (Time.timeScale 5~10 으로 임시 가속하여 검증)
- [ ] (8) 종료 후 모든 AI 정지 — 결과 팝업 아래 캐릭터가 더 이상 공격 안 함
- [ ] (9) ResultPopup "다시 시작" → 씬 재로드 정상

(6), (7) 검증 후 Knight HP / Time.timeScale 원복.

각 항목 통과 시 체크. 실패 시 해당 시스템 디버그.

---

## Task 6.4: 최종 스테이지 + Slice A 종료

- [ ] **Step 1: 전체 테스트 한 번 더**

- Test Runner → EditMode → Run All → 20/20 PASS
- Test Runner → PlayMode → Run All → 1/1 PASS

- [ ] **Step 2: 스테이지**

```powershell
git add Assets/_Lair/Tests
git status --short
```

- [ ] **Step 3: 커밋 메시지(안)**

```
# [test] - Slice A smoke test + 수동 체크리스트 9개 검증 완료
```

- [ ] **Step 4: 슬라이스 A 완료 보고**

사용자에게:
- M1~M6 모든 검증 게이트 통과
- 자동 테스트 총 21개 ✅ (EditMode 20 + PlayMode 1)
- 수동 체크리스트 9개 ✅
- 6개 커밋 메시지(안) 일괄 전달 (또는 사용자가 마일스톤별 분할 커밋 결정)

**Slice A 검증 가설 평가**: "5분 자동전투 코어 루프가 보는 즐거움이 있는가" — 한 판 플레이 후 사용자 판단.

다음 결정:
- ✅ 재미 있음 → Slice B (HP 10% 선택지 + 30초 카드)
- 🔧 페이싱 튜닝 필요 → 스탯/스폰 위치/속도 조정 후 재플레이
- ❌ 컨셉 재검토 → 기획서로 돌아가 코어 가설 재논의

---

# 자가 검토 결과

## 1. 스펙 커버리지

설계서 각 섹션이 태스크로 매핑되는지 확인:

| 설계서 섹션 | 구현 태스크 |
|---|---|
| §0 목적/범위 | (개요로 인용) |
| §1 룰 매핑 | 모든 태스크에 적용 |
| §2.1 레이어 / §2.2 폴더 | Task 1.1, 1.2 |
| §2.3 Enum 키 매핑 | Task 1.2, 3.9, 4.5 |
| §3.1 4개 인터페이스 | 2.5(IHealth), 2.7(IAttacker), 3.1(IMover, ITargetProvider) |
| §3.2 표준 구현체 | 2.6(Health), 2.7(MeleeAttacker), 2.8(Registry), 3.2(SimpleMover), 3.3(TargetProviders) |
| §3.2.1 사망 처리 정책 | 2.8(Registry IsAlive 필터), 3.4(AI IsAlive 체크), 5.2(EndBattle AI disable) |
| §3.3 AutoCombatAI | Task 3.4 |
| §3.4 프리팹 스펙 | 3.5(Knight), 3.6(Slime), 3.7(Golem), 3.8(Orc) |
| §4 UI MVVM | 2.3(Model), 2.4(VM), 4.1(BattleHud), 4.3(ResultPopup), 4.2/4.4(prefab) |
| §5 BattleClock + Controller | 2.2(Clock), 5.1(SpawnEntry), 5.2(Controller) |
| §5.4 Rule 06 보류 | (구현 안 함 — 명시적 결정) |
| §6 씬/카메라/Addressables | Task 5.3, 3.9, 4.5, 1.4 |
| §7 테스트 전략 | Task 2.* (EditMode), Task 6.2 (PlayMode), Task 6.3 (수동) |
| §8 ChvjPackage 활용 | Task 4.1(UIBase 상속), 4.2(closeDisposable), 5.2(CHMResource/UI/Pool) |
| §9 마일스톤 | M1~M6 구조 그대로 |
| §10 위험/가정 | 5.4 Step 2 (흔한 문제) 에 일부 반영 |

→ **스펙 커버리지 OK**.

## 2. 플레이스홀더 스캔

- "TBD" / "TODO" / "implement later" / "fill in" 없음 ✅
- 모든 코드 스텝에 실제 코드 포함 ✅
- 모든 명령에 정확한 경로/명령어 ✅

## 3. 타입 일관성

- `IHealth.OnChanged: Action<int, int>` — 모든 사용처(Health, FakeHealth, BattleViewModel.UpdateHeroHp) 일치 ✅
- `IAttacker.TryAttack(IHealth, Vector3, Vector3, float)` 시그니처 — MeleeAttacker, AutoCombatAI 일치 ✅
- `EHero.Knight`, `EMonster.Slime/Golem/Orc`, `EUI.BattleHud/ResultPopup`, `EScene.Battle` — 모든 사용처 일치 ✅
- `BattleResult.None/Win/Lose` — 모델, VM, BattleController, ResultPopup 일치 ✅
- `CharacterRegistry.RegisterHero/Monster` — 호출처(HeroTargetProvider/MonsterTargetProvider) 일치 ✅

→ **타입 일관성 OK**.

---

## 변경 이력

- **v0.1 (2026-05-18)**: 초안 — 설계서 v0.1 기반 6 마일스톤 / ~40 태스크 / ~150 스텝.
