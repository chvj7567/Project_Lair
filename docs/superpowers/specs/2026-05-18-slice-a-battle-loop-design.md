# Slice A — 5분 자동전투 코어 루프 설계서

> Project Lair MVP의 첫 수직 슬라이스.
> 작성일: 2026-05-18
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
**5분 자동전투 + 시간 압박이 게임으로 성립하는가** 를 가장 작은 코드로 검증.
빌드업(카드/선택지) 시스템은 본 슬라이스에 포함하지 않음.

### 0.2 In Scope
- 영웅 1명, 몬스터 3종(슬라임/골렘/오크) 고정 배치
- 자동 이동 + 근접 공격 + HP/사거리/쿨다운
- 5:00 카운트다운 타이머
- 영웅 HP 0 = 승리 / 타임아웃 = 패배
- HUD: 타이머 + 영웅 HP 바
- 종료 시 결과 팝업 + 재시작

### 0.3 Out of Scope
- HP 10% 패시브 선택지
- 30초 액티브 카드
- 일시정지/큐 시스템
- 몬스터 6종 전체 (3종만)
- 메타 진행 / 서버 / 사운드 / 아트
- 카드 풀 / 빌드업 시너지

### 0.4 검증 가설
"기획서 4장의 코어 루프(영웅 자동 진입 + 자동 전투 + 5분 압박) 만으로 보는 즐거움이 있는가."
재미 검증은 슬라이스 A 종료 직후 1판 플레이로 사용자가 판단.

---

## 1. 프로젝트 룰 매핑 (전체 적용)

| 룰 | 본 설계에서의 적용 |
|---|---|
| 01 자동 커밋 금지 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만 전달 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | 캐릭터는 컴포지션, 매니저 직접 참조 X, 이벤트 기반 |
| 04 반복 에셋 프리팹화 | 캐릭터 4종 + UI 2종 모두 프리팹 |
| 05 MVVM | UI 계층(Model/VM/View), 캐릭터는 컴포넌트 (의도된 부분 적용) |
| 06 상위 인터페이스 | Slice A 도입 보류 — 카드 슬라이스에서 자연 도입 |
| 07 ChvjPackage 기준 | 패키지 신규 코드 0줄. 100% 기존 API 사용 |
| 08 Enum 키 = 에셋명 | EHero/EMonster/EUI/EScene. 파일명·Addressables 주소·Enum 값명 3중 일치 |

---

## 2. 아키텍처 개요

### 2.1 레이어 (의존 방향 ↓ 만)

```
[Bootstrap / Scene]   BattleSceneEntry (별도 분리 안 함, BattleController 가 진입점)
        ↓
[Game UI Layer]       BattleHud (View) ← BattleViewModel ← BattleStateModel
        ↓                                       ↑
[Battle Logic]        BattleController, BattleClock
        ↓                                       ↑
[Character Layer]     Hero, Monster (컴포지션: IMover / IAttacker / IHealth / ITargetProvider)
        ↓
[Infra]               com.chvj.unityinfra (Pool / Resource / UI / Singleton)
```

- 위 → 아래 단방향 의존
- 캐릭터는 BattleController 를 모름 (이벤트로만 통지)
- View → ViewModel 구독, ViewModel → View 직접 호출 없음

### 2.2 폴더/asmdef

```
Assets/_Lair/
  Scripts/
    Battle/           BattleClock, BattleController, MonsterSpawnEntry
    Character/        IMover, IHealth, IAttacker, ITargetProvider,
                      SimpleMover, Health, MeleeAttacker,
                      HeroTargetProvider, MonsterTargetProvider,
                      AutoCombatAI, CharacterRegistry
    UI/               BattleStateModel, BattleViewModel,
                      BattleHud, BattleHudArg, ResultPopup, ResultPopupArg
    Data/Enums/       EHero, EMonster, EUI, EScene
    Lair.asmdef       참조: ChvjUnityInfra
  Prefabs/
    Characters/       Knight, Slime, Golem, Orc      (각 .prefab)
    UI/               BattleHud, ResultPopup         (각 .prefab)
  Scenes/
    Battle.unity
  Tests/
    EditMode/         BattleClockTests, BattleViewModelTests,
                      HealthTests, MeleeAttackerTests, CharacterRegistryTests,
                      Helpers/FakeHealth.cs
                      Lair.Tests.EditMode.asmdef    참조: Lair, nunit.framework
    PlayMode/         BattleSmokeTest
                      Lair.Tests.PlayMode.asmdef
```

기존 `Assets/Scenes/SampleScene.unity` 등은 유지(지우지 않음). 게임 코드는 `_Lair/` 하위에만 격리.

### 2.3 Enum 키 매핑 (Rule 08)

| Enum 키 | 파일 경로 | Addressables 주소 |
|---|---|---|
| `EHero.Knight` | `_Lair/Prefabs/Characters/Knight.prefab` | `Knight` |
| `EMonster.Slime` | `…/Slime.prefab` | `Slime` |
| `EMonster.Golem` | `…/Golem.prefab` | `Golem` |
| `EMonster.Orc` | `…/Orc.prefab` | `Orc` |
| `EUI.BattleHud` | `_Lair/Prefabs/UI/BattleHud.prefab` | `BattleHud` |
| `EUI.ResultPopup` | `…/ResultPopup.prefab` | `ResultPopup` |
| `EScene.Battle` | `_Lair/Scenes/Battle.unity` | (SceneManager 직접) |

- Addressables 그룹: `"Resource"` 단일
- Addressables 라벨: `"Resource"` 단일 (CHMResource 기본값)
- 그룹 설정에서 주소를 **파일명만** 으로 정규화 ("Simplify Addressable Names" 또는 수동)

---

## 3. 캐릭터 컴포지션 계층

### 3.1 4개 인터페이스

```csharp
public interface IMover
{
    float Speed { get; set; }
    void MoveTo(Vector3 target);
    void Stop();
}

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

public interface IAttacker
{
    float Range { get; }
    float Cooldown { get; }
    int Power { get; }
    bool TryAttack(IHealth target);     //# 사거리/쿨 만족 시 데미지 적용 후 true
}

public interface ITargetProvider
{
    bool TryFindNearest(Vector3 from, out Transform target, out IHealth health);
}
```

### 3.2 표준 구현체

| 클래스 | 구현 | 비고 |
|---|---|---|
| `SimpleMover` | `IMover` | `Vector3.MoveTowards`, 장애물 회피 없음 |
| `Health` | `IHealth` | 이벤트 발행, 사망 후 추가 데미지 무시 |
| `MeleeAttacker` | `IAttacker` | 거리/쿨다운 체크 후 `target.TakeDamage(Power)` |
| `HeroTargetProvider` | `ITargetProvider` | `MonsterRegistry` 정적 리스트에서 가장 가까운 살아있는 적 1개 |
| `MonsterTargetProvider` | `ITargetProvider` | `HeroRegistry` (영웅 1명 고정) |
| `CharacterRegistry` | static | Health 의 OnEnable/OnDisable 시 자기 등록·해제, `TryFindNearest` 가 `IHealth.IsAlive == true` 만 반환 |

### 3.2.1 사망 처리 정책

- `Health.TakeDamage` 가 Current ≤ 0 도달 시 `OnDied` 1회 발행, 이후 추가 데미지 무시
- **GameObject 는 destroy/disable 하지 않음** (Slice A 단순화)
  - 시각적으로 시체가 남지만 ResultPopup 이 곧 화면을 덮음
  - `AutoCombatAI.Update` 가 첫 줄에서 `IsAlive == false` 체크 → 즉시 return
  - `CharacterRegistry.TryFindNearest` 가 죽은 캐릭터 제외 → 공격 시도되지 않음
- Pool 반환은 슬라이스 B(런 재시작 동안 살아있는 몬스터 회수) 에서 도입

### 3.3 행동 컨트롤러 `AutoCombatAI`

```csharp
[RequireComponent(typeof(SimpleMover), typeof(Health), typeof(MeleeAttacker))]
public class AutoCombatAI : MonoBehaviour
{
    private IMover _mover;
    private IHealth _health;
    private IAttacker _attacker;
    private ITargetProvider _targetProvider;

    void Awake()
    {
        _mover = GetComponent<IMover>();
        _health = GetComponent<IHealth>();
        _attacker = GetComponent<IAttacker>();
        _targetProvider = GetComponent<ITargetProvider>();
    }

    void Update()
    {
        if (_health.IsAlive == false) return;
        if (_targetProvider.TryFindNearest(transform.position, out var t, out var th) == false)
        {
            _mover.Stop();
            return;
        }

        float dist = Vector3.Distance(transform.position, t.position);
        if (dist <= _attacker.Range)
        {
            _mover.Stop();
            _attacker.TryAttack(th);
        }
        else
        {
            _mover.MoveTo(t.position);
        }
    }
}
```

### 3.4 프리팹 컴포넌트 구성 (기획서 11.4 기준)

| 프리팹 | 메쉬 | 색상 | 스케일 | HP | DPS / Power-Cooldown | Range | Speed |
|---|---|---|---|---|---|---|---|
| Knight | Capsule | `#3B82F6` | 1.0 | 1000 | 50/타 × 1.0s | 1.5 | 3.0 |
| Slime | Sphere | `#22C55E` | 0.6 | 200 | DPS 10 (Power 10 × 1.0s) | 1.0 | 1.5 |
| Golem | Cube | `#6B7280` | 1.2 | 500 | DPS 20 (Power 20 × 1.0s) | 1.3 | 0.8 |
| Orc | Capsule | `#EF4444` | 0.9 | 100 | DPS 40 (Power 20 × 0.5s) | 1.0 | 2.5 |

- Knight: `HeroTargetProvider`, 나머지: `MonsterTargetProvider`
- 공통: `SimpleMover` + `Health` + `MeleeAttacker` + `AutoCombatAI`
- 값은 인스펙터에 직접. ScriptableObject 도입은 카드 슬라이스에서.

---

## 4. UI MVVM 계층

### 4.1 데이터 흐름

```
[BattleController]
       │ UpdateTimer / UpdateHeroHp / EndBattle
       ▼
[BattleViewModel] ──(Model 갱신)──> [BattleStateModel]
       │ 이벤트 발행: OnTimerChanged / OnHeroHpRatioChanged / OnBattleEnded
       ▼
[BattleHud (UIBase)] ──> UGUI Text/Image
```

### 4.2 `BattleStateModel` (POCO, Unity 의존 0)

```csharp
public class BattleStateModel
{
    public float ElapsedSeconds;
    public float TotalSeconds = 300f;
    public int HeroHp;
    public int HeroMaxHp;
    public BattleResult Result = BattleResult.None;
}

public enum BattleResult { None, Win, Lose }
```

### 4.3 `BattleViewModel`

```csharp
public class BattleViewModel
{
    private readonly BattleStateModel _model;

    public event Action<float, float> OnTimerChanged;     //# (elapsed, total)
    public event Action<float> OnHeroHpRatioChanged;
    public event Action<BattleResult> OnBattleEnded;

    public BattleViewModel(BattleStateModel model) => _model = model;

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

    public float ElapsedSeconds => _model.ElapsedSeconds;
    public float TotalSeconds   => _model.TotalSeconds;
    public float HeroHpRatio    => _model.HeroMaxHp > 0
        ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
    public BattleResult Result  => _model.Result;
}
```

### 4.4 `BattleHud` (UIBase 상속)

```csharp
public class BattleHud : UIBase
{
    [SerializeField] private Text _timerText;
    [SerializeField] private Image _heroHpFill;

    private BattleViewModel _vm;

    public override void InitUI(UIArg arg)
    {
        if (arg is BattleHudArg ba) Bind(ba.ViewModel);
    }

    private void Bind(BattleViewModel vm)
    {
        _vm = vm;
        vm.OnTimerChanged       += HandleTimer;
        vm.OnHeroHpRatioChanged += HandleHp;
        vm.OnBattleEnded        += HandleEnded;

        //# UIBase.closeDisposable 활용 — Close 시 자동 해제
        closeDisposable.Add(() => vm.OnTimerChanged       -= HandleTimer);
        closeDisposable.Add(() => vm.OnHeroHpRatioChanged -= HandleHp);
        closeDisposable.Add(() => vm.OnBattleEnded        -= HandleEnded);

        HandleTimer(vm.ElapsedSeconds, vm.TotalSeconds);
        HandleHp(vm.HeroHpRatio);
    }

    private void HandleTimer(float elapsed, float total)
    {
        float remain = Mathf.Max(0f, total - elapsed);
        _timerText.text = $"{(int)(remain / 60)}:{(int)(remain % 60):00}";
    }
    private void HandleHp(float r) => _heroHpFill.fillAmount = r;
    private void HandleEnded(BattleResult r) { /* HUD 자기 표시만 */ }
}

public class BattleHudArg : UIArg
{
    public BattleViewModel ViewModel;
}
```

### 4.5 `ResultPopup`

- `UIBase` 상속, `EUI.ResultPopup`
- `ResultPopupArg { BattleResult Result }` 받아 "승리" / "패배" 텍스트
- 재시작 버튼 → `SceneManager.LoadScene(EScene.Battle.ToString())`

---

## 5. 전투 진행 / 판정

### 5.1 `BattleClock` (POCO)

```csharp
public class BattleClock
{
    public float Elapsed { get; private set; }
    public float TotalSeconds { get; }
    public bool IsRunning { get; private set; }

    public event Action<float> OnTick;
    public event Action OnTimeUp;

    public BattleClock(float total) => TotalSeconds = total;

    public void Start() { Elapsed = 0; IsRunning = true; }
    public void Stop()  { IsRunning = false; }

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
```

### 5.2 `BattleController`

```csharp
public class BattleController : MonoBehaviour
{
    [SerializeField] private Transform _heroSpawn;
    [SerializeField] private MonsterSpawnEntry[] _monsterSpawns;

    private BattleClock _clock;
    private BattleStateModel _model;
    private BattleViewModel _vm;
    private CHPoolable _hero;
    private Health _heroHealth;
    private List<CHPoolable> _monsters = new();

    async void Start()
    {
        if (await CHMResource.Instance.Init() == false) return;
        CHMUI.Instance.Init();
        CHMPool.Instance.Init();

        _model = new BattleStateModel();
        _vm = new BattleViewModel(_model);

        await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
            new BattleHudArg { ViewModel = _vm });

        await SpawnHero();
        await SpawnMonsters();

        _clock = new BattleClock(_model.TotalSeconds);
        _clock.OnTick   += _vm.UpdateTimer;
        _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
        _clock.Start();
    }

    void Update() => _clock?.Tick(Time.deltaTime);

    private async Task SpawnHero()
    {
        var prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
        var p = CHMPool.Instance.Pop(prefab, transform);
        p.transform.position = _heroSpawn.position;

        _hero = p;
        _heroHealth = p.GetComponent<Health>();
        _heroHealth.OnChanged += _vm.UpdateHeroHp;
        _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
        _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
    }

    private async Task SpawnMonsters()
    {
        foreach (var sp in _monsterSpawns)
        {
            var prefab = await CHMResource.Instance.LoadAsync<GameObject>(sp.Key);
            var p = CHMPool.Instance.Pop(prefab, transform);
            p.transform.position = sp.Point.position;
            _monsters.Add(p);
        }
    }

    private async void EndBattle(BattleResult result)
    {
        if (_model.Result != BattleResult.None) return;
        _clock.Stop();
        foreach (var ai in GetComponentsInChildren<AutoCombatAI>())
            ai.enabled = false;
        _vm.EndBattle(result);
        await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup,
            new ResultPopupArg { Result = result });
    }
}

[Serializable]
public struct MonsterSpawnEntry
{
    public Transform Point;
    public EMonster Key;
}
```

### 5.3 이벤트 흐름

```
Time.deltaTime
   ↓
BattleClock.Tick
   ├─ OnTick → BattleViewModel.UpdateTimer → BattleHud
   └─ OnTimeUp → EndBattle(Lose)

Health.TakeDamage (영웅)
   ├─ OnChanged → BattleViewModel.UpdateHeroHp → BattleHud
   └─ OnDied → EndBattle(Win)

EndBattle
   ├─ Clock.Stop
   ├─ 모든 AutoCombatAI.enabled = false
   ├─ ViewModel.EndBattle(result)
   └─ CHMUI.ShowUI(ResultPopup)
```

### 5.4 Rule 06 (`IBattleEvents`) 도입 보류 근거
Slice A 에서는 자식 → 부모 호출이 없음. HP/사망은 자식이 이벤트 발행하고 부모가 구독(반대 방향). AI 종료는 부모가 자식의 `enabled` 직접 조작. 카드 슬라이스에서 자식이 부모의 "스폰" 기능을 호출해야 할 때 자연 도입.

---

## 6. 씬 셋업 + 카메라 + Addressables

### 6.1 `Battle.unity` 계층

```
Battle (Scene)
├── @Camera         / Main Camera (45° 비스듬)
├── @Light          / Directional Light
├── @Stage          / Floor (Plane 30×30)
├── @UI             / UICanvas (Tag "UICanvas") + EventSystem
└── @Battle         / BattleController
                      ├── HeroSpawn (Transform)
                      ├── MonsterSpawn_01 (Slime)
                      ├── MonsterSpawn_02 (Golem)
                      └── MonsterSpawn_03 (Orc)
```

- UICanvas 사전 배치 + Tag "UICanvas" → `CHMUI.EnsureRootAsync` 가 즉시 인식, Addressables 로드 불요
- 첫 플레이 후 카메라/스폰 위치 튜닝

### 6.2 카메라 초기값

| 속성 | 값 |
|---|---|
| Position | `(0, 12, -8)` |
| Rotation | `(50, 0, 0)` |
| Projection | Perspective, FOV 60 |
| Clear Flags | Solid Color `#1F2937` |

### 6.3 Addressables 설정
- 그룹: `"Resource"` 단일
- 라벨: `"Resource"` 단일
- 주소 정규화: 파일명만 (Simplify or 수동)
- 등록 대상: 캐릭터 4 + UI 2 = 6 에셋

### 6.4 빌드 세팅
- `_Lair/Scenes/Battle.unity` index 0

---

## 7. 테스트 전략

### 7.1 EditMode (NUnit, Unity 의존 0)

| 대상 | 케이스 수 | 핵심 검증 |
|---|---|---|
| `BattleClock` | 4 | Tick 누적/콜백 횟수/TimeUp 1회/Stop 후 무시 |
| `BattleViewModel` | 4 | 이벤트 발행, 비율 계산, max=0 안전, 늦은 구독 |
| `Health` | 4 | TakeDamage, OnDied 1회, 사망 후 무시, SetMax |
| `MeleeAttacker` | 4 | 사거리/쿨다운/Power 적용/FakeHealth 호출 |
| `CharacterRegistry` | 4 | 등록·해제, 거리순, 빈 레지스트리, IsAlive 필터링 |

**합계 20개**. 전부 POCO 기반, 모킹 없이 빠르게.

### 7.2 PlayMode
- `BattleSmokeTest` 1개 — Battle 씬 로드 → 5초 → 캐릭터 살아있음 + 타이머 갱신

### 7.3 수동 체크리스트
1. 씬 진입 1초 내 캐릭터 4개 표시
2. 영웅이 가장 가까운 몬스터로 자동 이동
3. 사거리 도달 시 정지·공격 반복
4. HP 바 실시간 감소
5. 타이머 5:00 → 0:00 카운트다운
6. 영웅 사망 시 ResultPopup "승리"
7. 5분 도달 시 ResultPopup "패배"
8. 종료 후 모든 AI 정지
9. ResultPopup 재시작 → 씬 재로드 정상

### 7.4 TDD 정책
- 핵심 POCO 5종: Red→Green→Refactor 엄격
- Thin wrapper (`SimpleMover`, TargetProvider): 사후 테스트
- MonoBehaviour 통합 (`BattleController`, `BattleHud`): 수동 검증

---

## 8. ChvjPackage 활용 매트릭스

### 8.1 사용 API

| 모듈 | API | 사용처 |
|---|---|---|
| `CHMResource` | `Init`, `LoadAsync<T>(Enum)` | BattleController, CHMUI 내부 |
| `CHMPool` | `Init`, `Pop` | 캐릭터 스폰 |
| `CHMUI` | `Init`, `ShowUIAsync(Enum, UIArg)` | HUD/ResultPopup |
| `UIBase` | `InitUI(UIArg)` 오버라이드 | BattleHud, ResultPopup |
| `UIArg` | 파생 클래스 | BattleHudArg, ResultPopupArg |
| `CompositeDisposable` | `closeDisposable.Add(…)` | BattleHud 의 이벤트 구독 해제 |
| `CHSingleton(Static)` | 매니저 베이스 | 간접 사용 |

### 8.2 패키지 보강 — **없음**
Slice A 에 필요한 모든 기능이 ChvjPackage 에 이미 존재. 신규 패키지 코드 0줄.

### 8.3 Addressables 환경
- `packages-lock.json` 에 `com.unity.addressables: 2.8.1` 자동 등록 확인
- ChvjPackage 의 transitive 의존성으로 해결
- 첫 사용 전 에디터 메뉴에서 Addressables Settings 1회 자동 생성 필요

---

## 9. 작업 순서 / 마일스톤

| M | 제목 | 핵심 산출물 | 검증 게이트 | 예상 시간 |
|---|---|---|---|---|
| M1 | 골격 셋업 | 폴더/asmdef/Enum/빈 씬 | 컴파일 OK, asmdef 인식 | ~30분 |
| M2 | POCO 로직 TDD | Clock/VM/Health/Attacker/Registry | EditMode 20개 ✅ | 3~4h |
| M3 | 캐릭터 자동전투 | 컴포넌트 + 프리팹 4 + Addressables | 1대1 자동전투 동작 영상 | 2~3h |
| M4 | HUD MVVM | BattleHud/ResultPopup 프리팹·스크립트 | 타이머/HP 실시간 갱신 | 2~3h |
| M5 | 풀 배틀 루프 | BattleController 완성 + 종료 | 한 판 풀 플레이 + Win/Lose | 1~2h |
| M6 | 테스트/체크리스트 | PlayMode smoke + 수동 9개 | 자동 21개 + 수동 9개 ✅ | 1~2h |

**총 ~12~16h**. 기획서 11.5 의 "1주" 안에 충분 여유.

### 9.1 마일스톤별 커밋 메시지(안) — Rule 01 포맷

```
# [chore] - Slice A 골격 셋업 (폴더/asmdef/Enum/씬)
# [feat] - 전투 POCO 로직 + EditMode 테스트
# [feat] - 캐릭터 컴포지션 + 자동전투 AI + 4종 프리팹
# [feat] - BattleHud / ResultPopup MVVM 바인딩
# [feat] - BattleController 풀 루프 (스폰/시계/판정/재시작)
# [test] - Slice A smoke test + 수동 체크리스트 검증
```

### 9.2 사용자 검토 포인트

| 시점 | 사용자 확인 |
|---|---|
| M1 직후 | 폴더 구조·asmdef·Enum 명세가 의도대로인지 |
| M3 직후 | 자동전투 페이싱이 느낌상 OK인지 |
| M5 직후 | 한 판 풀 루프가 검증 가설을 충족하는지 |

---

## 10. 위험 / 가정 / 미정 항목

### 10.1 가정
- 영웅·몬스터 스탯(기획서 11.3) 그대로 사용. 첫 플레이 후 튜닝 가능.
- Addressables 첫 사용 시 Settings 자동 생성은 사용자가 에디터에서 1회 수행 (자동화 X).
- URP 17.0.4 환경에서 프리미티브 색상 적용은 URP Lit 머티리얼로.

### 10.2 위험 + 완화
| 위험 | 완화 |
|---|---|
| 자동전투 페이싱이 단조로움 | M3 직후 사용자 플레이 + 수치 튜닝 |
| Addressables 주소 매칭 실패 | Rule 08 명시 + M3 스폰 시 즉시 발견 |
| ChvjPackage `CHMUI` 가 UICanvas 못 찾음 | 씬에 사전 배치 + Tag "UICanvas" 명시 |
| 종료 후 AI 가 ResultPopup 위에서 계속 공격 | EndBattle 에서 모든 AutoCombatAI.enabled=false |

### 10.3 슬라이스 A 종료 후 다음 결정
- HP 10% 패시브 선택지 → 슬라이스 B
- 30초 액티브 카드 → 슬라이스 B 또는 C
- 카메라/조명 폴리시 → 별도 패스
- 사운드 → 출시 직전 패스

---

## 변경 이력
- **v0.1 (2026-05-18)**: 초안. 브레인스토밍 8개 섹션 통합.
