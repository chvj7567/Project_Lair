# Slice B1 — HP% 패시브 카드 시스템 설계서

> Project Lair MVP 의 두 번째 수직 슬라이스 — 빌드업 시스템의 패시브 절반.
> 작성일: 2026-05-19
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
**HP 임계점 트리거 + 3택 1 선택지가 빌드업의 재미를 만드는가** 검증.
액티브 카드(30초 트리거) 는 B2 로 분리.

### 0.2 In Scope (B1)
- HP 10% 임계점 트리거 (9회: 90/80/.../10%)
- 일시정지 + 큐 시스템 (Time.timeScale=0 기반)
- 카드 데이터 모델 (CardData ScriptableObject + ICardEffect Strategy)
- 카드 7장 (강화 3 / 추가 2 / 교체 1 / 환경 1)
- 3택 1 선택 UI (CardSelectionPopup)
- 카드 효과 적용 시스템 (IBattleContext, HeroAuraRunner)

### 0.3 Out of Scope
- 액티브 30초 트리거 (B2)
- 패시브 카드 풀 15장 (B3 — 컨텐츠 확장)
- 메타 진행 / 서버 / 사운드 / 아트
- 진화 카테고리

### 0.4 검증 가설
"트리거 + 선택지 시스템이 한 판 안에서 빌드업/긴장감을 만드는가." B1-M5 직후 사용자가 한 판 플레이로 판단.

---

## 1. 프로젝트 룰 매핑

| 룰 | 본 설계에서의 적용 |
|---|---|
| 01 자동 커밋 금지 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만 전달 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | 카드 효과는 BattleController 모름, IBattleContext 만 사용 |
| 04 반복 에셋 프리팹화 | CardData SO 7개 + CardPool SO 1개 + CardSelectionPopup 프리팹 |
| 05 MVVM | CardSelectionPopup = View, CardSelectionArg = 즉시 VM 격 |
| 06 상위 인터페이스 | **B1 에서 자연 도입** — IBattleContext (카드 효과 ↔ BattleController) |
| 07 ChvjPackage | CHMUI/UIBase/CHText/CHButton 활용, 신규 패키지 코드 0 |
| 08 Enum 키 | ECardId 값명 = SO 파일명 + EUI.CardSelectionPopup |
| 09 CommonEnum | ECardCategory, ECardId, EUI.CardSelectionPopup, EData.CardPool_Passive → CommonEnum.cs |
| 10 CommonInterface | ICardEffect, IBattleContext, IHeroAura → `Card/CommonInterface.cs` |
| 11 CHText/CHButton | CardView 의 텍스트/버튼 |

---

## 2. 아키텍처 개요

### 2.1 데이터 흐름

```
[Hero Health.OnChanged]
        │ ratio < threshold?
        ▼
[PassiveTriggerService]    HP 90%/80%/.../10% 임계점 통과 1회 감지
        │ OnTriggered(thresholdIndex)
        ▼
[BattleController]
        │ TriggerQueue.Enqueue → TryProcessNext()
        │   1) PauseService.Pause() (Time.timeScale=0)
        │   2) CardDeck.Draw(3)
        │   3) await CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)
        ▼
[CardSelectionPopup → CardView × 3 → CHButton.OnClick]
        │ 사용자 카드 1장 클릭
        ▼
[OnPicked callback]
        │ card.Effect.Apply(_battleContext)
        │ CHMUI.Close → PauseService.Resume()
        ▼
[게임 재개 + 큐에 남은 트리거 처리]
```

### 2.2 단방향 의존

```
[Bootstrap]              BattleController (기존 진입점)
        ↓
[UI Layer]              CardSelectionPopup ← CardSelectionArg
        ↓                          ↑
[Battle Logic]          BattleController, PauseService, PassiveTriggerService,
                        TriggerQueue, CardDeck, BattleContext, HeroAuraRunner
        ↓                          ↑
[Card System]           CardData (SO), ICardEffect, IBattleContext, IHeroAura
        ↓
[Character Layer]       기존 Slice A — IHealth/IMover/IAttacker/ITargetProvider
        ↓
[Infra]                 com.chvj.unityinfra
```

### 2.3 Slice A 와의 통합

`BattleController` 에 새 필드 추가:
- `_pause`, `_passiveTriggers`, `_queue`, `_passiveDeck`, `_ctx`, `_processingQueue`

`Start()` 마지막 부분에 B1 초기화 추가 (기존 Slice A 흐름 유지).
`BattleClock`, `BattleHud`, `Health`, `MeleeAttacker`, `SimpleMover`, `AutoCombatAI` — 수정 없음 (Time.timeScale=0 시 자동 정지).

`CharacterRegistry.Entry` 에 향후 `MonsterTag` 컴포넌트 직접 참조 가능 — Entry 자체 구조 변경 X, BattleContext 가 `Transform.GetComponent<MonsterTag>()` 로 조회.

### 2.4 폴더 추가

```
Assets/_Lair/
  Scripts/
    Battle/
      PauseService.cs                ← 신규
      PassiveTriggerService.cs       ← 신규
      TriggerQueue.cs                ← 신규
      BattleContext.cs               ← 신규
      HeroAuraRunner.cs              ← 신규
    Card/
      CommonInterface.cs             ← 신규 (ICardEffect/IBattleContext/IHeroAura)
      CardData.cs                    ← 신규 (ScriptableObject)
      CardPool.cs                    ← 신규 (ScriptableObject)
      CardDeck.cs                    ← 신규 (POCO)
      Effects/                       ← 신규 — 7개 효과 클래스
        SlimeHpBoostEffect.cs
        GolemDamageBoostEffect.cs
        OrcAtkSpeedEffect.cs
        SpawnSlimesEffect.cs
        SpawnGolemEffect.cs
        ReplaceSlimesToGolemEffect.cs
        HeroPoisonAuraEffect.cs
      Auras/
        PoisonAura.cs                ← 신규 (IHeroAura 첫 구현)
    Character/
      MonsterTag.cs                  ← 신규 (EMonster Key 직렬화)
    UI/
      CardSelectionArg.cs            ← 신규
      CardSelectionPopup.cs          ← 신규 (UIBase)
      CardView.cs                    ← 신규 (단일 카드 View)
  Prefabs/UI/
    CardSelectionPopup.prefab        ← 신규
  Data/Cards/
    CardPool_Passive.asset           ← 신규 (SO, 7장 리스트)
    Cards/
      SlimeHpBoost.asset             ← 신규
      GolemDamageBoost.asset
      OrcAtkSpeed.asset
      SpawnSlimes.asset
      SpawnGolem.asset
      ReplaceSlimesToGolem.asset
      HeroPoisonAura.asset
```

---

## 3. Enum 및 Interface 추가

### 3.1 CommonEnum.cs 추가

```csharp
namespace Lair.Data
{
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# 신규
    }

    //# 신규
    public enum EData
    {
        CardPool_Passive,
    }

    //# 신규
    public enum ECardCategory
    {
        Enhance,        //# 강화
        Spawn,          //# 추가 소환
        Replace,        //# 교체
        Environment,    //# 환경 (영웅 디버프)
    }

    //# 신규 — B1 카드 7장 식별자
    public enum ECardId
    {
        SlimeHpBoost,
        GolemDamageBoost,
        OrcAtkSpeed,
        SpawnSlimes,
        SpawnGolem,
        ReplaceSlimesToGolem,
        HeroPoisonAura,
    }
}
```

### 3.2 `Card/CommonInterface.cs`

```csharp
namespace Lair.Card
{
    //# 카드 효과 Strategy. SerializeReference 로 CardData 에 직렬화.
    public interface ICardEffect
    {
        void Apply(IBattleContext ctx);
    }

    //# 카드 효과 ↔ BattleController 의 표면 (Rule 06).
    public interface IBattleContext
    {
        IEnumerable<IHealth> GetMonsters(EMonster? filter = null);
        IHealth GetHero();
        Transform GetHeroTransform();

        //# "슬라임 3마리 소환" 같은 카드용
        void SpawnMonster(EMonster key, Vector3 nearHero);

        //# 환경 카드 (예: 독 장판) — duration < 0 이면 무제한
        void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f);

        //# 카드가 직접 tick 만들 때 (현재 미사용, 확장 여지)
        float DeltaTime { get; }
    }

    //# 영웅에 붙는 일시/영구 효과
    public interface IHeroAura
    {
        void OnAttached(IHealth hero);
        void Tick(IHealth hero, float dt);
        void OnDetached(IHealth hero);
    }
}
```

---

## 4. 카드 데이터 모델

### 4.1 CardData (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
public class CardData : ScriptableObject
{
    [SerializeField] private ECardId _id;
    [SerializeField] private ECardCategory _category;
    [SerializeField] private string _displayName;
    [TextArea] [SerializeField] private string _description;
    [SerializeReference] private ICardEffect _effect;

    public ECardId Id => _id;
    public ECardCategory Category => _category;
    public string DisplayName => _displayName;
    public string Description => _description;
    public ICardEffect Effect => _effect;
}
```

`[SerializeReference]` — Unity polymorphic 직렬화. 인스펙터에서 ICardEffect 구현체 드롭다운 선택. 모든 효과 클래스는 `[System.Serializable]` 필수.

### 4.2 CardPool (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "CardPool_", menuName = "Lair/Card Pool")]
public class CardPool : ScriptableObject
{
    [SerializeField] private List<CardData> _cards = new();
    public IReadOnlyList<CardData> Cards => _cards;
}
```

### 4.3 CardDeck (POCO 런타임)
```csharp
public class CardDeck
{
    private readonly List<CardData> _all;
    private readonly System.Random _rng;

    public CardDeck(IEnumerable<CardData> cards, int seed = 0)
    {
        _all = new List<CardData>(cards);
        _rng = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    //# 무작위 n장 (중복 X). 풀 부족 시 가능한 만큼.
    public IReadOnlyList<CardData> Draw(int n)
    {
        var pool = new List<CardData>(_all);
        var result = new List<CardData>(Math.Min(n, pool.Count));
        for (int i = 0; i < n && pool.Count > 0; ++i)
        {
            int idx = _rng.Next(pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }
}
```

### 4.4 B1 카드 7장 정의

| ID | Category | 효과 클래스 | 동작 |
|---|---|---|---|
| `SlimeHpBoost` | Enhance | `SlimeHpBoostEffect` | 살아있는 모든 슬라임 Max HP × 1.5 (Current 비율 유지) |
| `GolemDamageBoost` | Enhance | `GolemDamageBoostEffect` | 살아있는 모든 골렘 Power × 1.5 |
| `OrcAtkSpeed` | Enhance | `OrcAtkSpeedEffect` | 살아있는 모든 오크 Cooldown × 0.7 |
| `SpawnSlimes` | Spawn | `SpawnSlimesEffect` | 영웅 근처에 슬라임 3마리 |
| `SpawnGolem` | Spawn | `SpawnGolemEffect` | 영웅 근처에 골렘 1마리 |
| `ReplaceSlimesToGolem` | Replace | `ReplaceSlimesToGolemEffect` | 살아있는 슬라임 전부 제거 → 위치 평균에 골렘 1마리 |
| `HeroPoisonAura` | Environment | `HeroPoisonAuraEffect` | 영웅에 PoisonAura(DPS 5) 부착, 무제한 |

---

## 5. 일시정지 / 트리거 / 큐 시스템

### 5.1 PauseService

```csharp
public class PauseService
{
    private int _depth;
    public bool IsPaused => _depth > 0;

    public void Pause()
    {
        _depth++;
        if (_depth == 1) Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (_depth == 0) return;
        _depth--;
        if (_depth == 0) Time.timeScale = 1f;
    }

    public void ForcePause() { _depth = int.MaxValue / 2; Time.timeScale = 0f; }
}
```

중첩 호출 안전. `depth` 카운터로 중첩 깊이 추적.

### 5.2 PassiveTriggerService

```csharp
public class PassiveTriggerService : IDisposable
{
    private static readonly float[] Thresholds =
        { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

    private readonly bool[] _fired = new bool[Thresholds.Length];
    private readonly IHealth _hero;

    public event Action<int> OnTriggered;   //# 0=90%, 1=80%, ..., 8=10%

    public PassiveTriggerService(IHealth hero)
    {
        _hero = hero;
        _hero.OnChanged += HandleChanged;
    }

    public void Dispose()
    {
        if (_hero != null) _hero.OnChanged -= HandleChanged;
    }

    private void HandleChanged(int current, int max)
    {
        if (max <= 0) return;
        float ratio = (float)current / max;
        for (int i = 0; i < Thresholds.Length; ++i)
        {
            if (_fired[i]) continue;
            if (ratio <= Thresholds[i])
            {
                _fired[i] = true;
                OnTriggered?.Invoke(i);
            }
        }
    }
}
```

큰 데미지로 여러 임계점 한 번에 통과해도 각각 순차 발동.

### 5.3 TriggerQueue

```csharp
public class TriggerQueue
{
    public enum Source { Passive, Active }
    public readonly struct Entry
    {
        public readonly Source SourceType;
        public readonly int Index;
        public Entry(Source s, int i) { SourceType = s; Index = i; }
    }

    private readonly Queue<Entry> _q = new();
    public int Count => _q.Count;
    public void Enqueue(Source s, int i) => _q.Enqueue(new Entry(s, i));
    public bool TryDequeue(out Entry e) => _q.TryDequeue(out e);
}
```

B1 엔 Passive 만 사용. Active 는 B2 에서 추가.

### 5.4 일시정지 동안 시스템 동작

| 시스템 | timeScale=0 효과 |
|---|---|
| `BattleClock.Tick(Time.deltaTime)` | Elapsed 안 늘어남 ✅ |
| `AutoCombatAI.Update` | dt=0 → 캐릭터 정지 ✅ |
| `SimpleMover.Update` | dt=0 → 이동 정지 ✅ |
| `Health.TakeDamage` | 호출 없음 (AI 정지) ✅ |
| `CHButton.onClick` | 입력 system은 unscaled — 정상 ✅ |
| `CHMUI` 인스턴스화 | EditorApplication.Update 기반 — 정상 ✅ |

---

## 6. BattleContext 구현

### 6.1 MonsterTag (캐릭터 식별)

```csharp
public class MonsterTag : MonoBehaviour
{
    [SerializeField] private EMonster _key;
    public EMonster Key => _key;
    public void Configure(EMonster k) => _key = k;
}
```

`LairCharacterPrefabBuilder` 가 몬스터 프리팹 빌드 시 부착 + `Configure(spec.Key)` 호출.

### 6.2 BattleContext (Battle 도메인)

```csharp
public class BattleContext : IBattleContext
{
    private readonly BattleController _owner;
    public float DeltaTime => Time.deltaTime;

    public BattleContext(BattleController owner) => _owner = owner;

    public IEnumerable<IHealth> GetMonsters(EMonster? filter = null)
    {
        foreach (var e in CharacterRegistry.Monsters)
        {
            if (e?.Health == null || !e.Health.IsAlive) continue;
            if (filter.HasValue)
            {
                var tag = e.Transform != null ? e.Transform.GetComponent<MonsterTag>() : null;
                if (tag == null || tag.Key != filter.Value) continue;
            }
            yield return e.Health;
        }
    }

    public IHealth GetHero()
    {
        foreach (var e in CharacterRegistry.Heroes)
            if (e?.Health != null && e.Health.IsAlive) return e.Health;
        return null;
    }

    public Transform GetHeroTransform()
    {
        foreach (var e in CharacterRegistry.Heroes)
            if (e?.Transform != null) return e.Transform;
        return null;
    }

    public void SpawnMonster(EMonster key, Vector3 nearHero)
        => _owner.SpawnMonsterRuntime(key, nearHero);

    public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f)
    {
        var heroT = GetHeroTransform();
        if (heroT == null) return;
        var runner = heroT.GetComponent<HeroAuraRunner>()
                  ?? heroT.gameObject.AddComponent<HeroAuraRunner>();
        runner.Attach(aura, durationSeconds);
    }
}
```

### 6.3 HeroAuraRunner

```csharp
public class HeroAuraRunner : MonoBehaviour
{
    private class Slot { public IHeroAura Aura; public float Remain; public bool Indefinite; }
    private readonly List<Slot> _slots = new();
    private IHealth _hero;

    private void Awake() => _hero = GetComponent<IHealth>();

    public void Attach(IHeroAura aura, float duration)
    {
        aura.OnAttached(_hero);
        _slots.Add(new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f });
    }

    private void Update()
    {
        for (int i = _slots.Count - 1; i >= 0; --i)
        {
            var s = _slots[i];
            s.Aura.Tick(_hero, Time.deltaTime);
            if (!s.Indefinite)
            {
                s.Remain -= Time.deltaTime;
                if (s.Remain <= 0f)
                {
                    s.Aura.OnDetached(_hero);
                    _slots.RemoveAt(i);
                }
            }
        }
    }
}
```

### 6.4 PoisonAura (첫 IHeroAura 구현)

```csharp
[Serializable]
public class PoisonAura : IHeroAura
{
    private readonly float _dps;
    private float _tickAccumulator;

    public PoisonAura(float dps) { _dps = dps; }
    public void OnAttached(IHealth hero) { _tickAccumulator = 0f; }
    public void Tick(IHealth hero, float dt)
    {
        _tickAccumulator += dt;
        while (_tickAccumulator >= 1f)
        {
            _tickAccumulator -= 1f;
            hero.TakeDamage((int)_dps);
        }
    }
    public void OnDetached(IHealth hero) { }
}
```

---

## 7. CardSelectionPopup UI

### 7.1 CardSelectionArg
```csharp
public class CardSelectionArg : UIArg
{
    public IReadOnlyList<CardData> Choices;
    public Action<CardData> OnPicked;
}
```

### 7.2 CardSelectionPopup (UIBase)
```csharp
public class CardSelectionPopup : UIBase
{
    [SerializeField] private CardView[] _slots = new CardView[3];

    public override void InitUI(UIArg arg)
    {
        if (arg is not CardSelectionArg sa) return;
        for (int i = 0; i < _slots.Length; ++i)
        {
            if (i < sa.Choices.Count)
            {
                var card = sa.Choices[i];
                _slots[i].gameObject.SetActive(true);
                _slots[i].Bind(card, () =>
                {
                    sa.OnPicked?.Invoke(card);
                    Close(reuse: true);
                });
            }
            else
            {
                _slots[i].gameObject.SetActive(false);
            }
        }
    }
}
```

### 7.3 CardView
```csharp
public class CardView : MonoBehaviour
{
    [SerializeField] private CHText _nameText;
    [SerializeField] private CHText _descText;
    [SerializeField] private Image _border;
    [SerializeField] private CHButton _pickButton;

    public void Bind(CardData card, Action onClick)
    {
        _nameText.SetText(card.DisplayName);
        _descText.SetText(card.Description);
        _border.color = CategoryColor(card.Category);
        _pickButton.OnClick(onClick);
    }

    private static Color CategoryColor(ECardCategory c) => c switch
    {
        ECardCategory.Enhance     => new Color(0.13f, 0.77f, 0.37f),
        ECardCategory.Spawn       => new Color(0.23f, 0.51f, 0.96f),
        ECardCategory.Replace     => new Color(0.96f, 0.62f, 0.23f),
        ECardCategory.Environment => new Color(0.66f, 0.33f, 0.96f),
        _                         => Color.gray,
    };
}
```

### 7.4 프리팹 구조 (LairUIPrefabBuilder.BuildCardSelectionPopup)

```
CardSelectionPopup (Full Stretch)
├── Dim (Image, #00000080)
├── Title (TMP+CHText "카드 선택", Top-Center)
└── CardsLayout (HorizontalLayoutGroup, Center)
    ├── CardView_Left   (320×420)
    │   ├── Border (Image — 카테고리 색)
    │   ├── NameText (TMP+CHText)
    │   ├── DescText (TMP+CHText)
    │   └── PickButton (Button+CHButton, Full stretch over card)
    ├── CardView_Mid
    └── CardView_Right
```

---

## 8. BattleController 통합

### 8.1 신규 필드
```csharp
private PauseService _pause;
private PassiveTriggerService _passiveTriggers;
private TriggerQueue _queue;
private CardDeck _passiveDeck;
private IBattleContext _ctx;
private bool _processingQueue;
```

### 8.2 Start 확장 (Slice A 끝 부분에 추가)
```csharp
async void Start()
{
    //# ... Slice A 초기화 ...
    await SpawnHero();
    await SpawnMonsters();

    //# B1 신규
    _pause = new PauseService();
    _queue = new TriggerQueue();
    _passiveTriggers = new PassiveTriggerService(_heroHealth);
    _passiveTriggers.OnTriggered += idx =>
    {
        _queue.Enqueue(TriggerQueue.Source.Passive, idx);
        TryProcessNext();
    };
    _ctx = new BattleContext(this);
    var pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
    _passiveDeck = new CardDeck(pool.Cards);

    _clock = new BattleClock(_model.TotalSeconds);
    //# ... 기존 _clock 구독 ...
    _clock.Start();
}
```

### 8.3 큐 처리 루프
```csharp
private async void TryProcessNext()
{
    if (_processingQueue) return;
    if (_queue.Count == 0) return;
    if (_model.Result != BattleResult.None) return;
    _processingQueue = true;

    while (_queue.TryDequeue(out var entry))
    {
        if (_model.Result != BattleResult.None) break;

        _pause.Pause();
        var choices = _passiveDeck.Draw(3);
        var tcs = new TaskCompletionSource<bool>();

        var arg = new CardSelectionArg
        {
            Choices = choices,
            OnPicked = card =>
            {
                card.Effect.Apply(_ctx);
                tcs.TrySetResult(true);
            }
        };
        await CHMUI.Instance.ShowUIAsync(EUI.CardSelectionPopup, arg);
        await tcs.Task;   //# 사용자 클릭 대기
        _pause.Resume();
    }

    _processingQueue = false;
}
```

### 8.4 SpawnMonsterRuntime (BattleContext 호출용)
```csharp
public void SpawnMonsterRuntime(EMonster key, Vector3 nearHero)
{
    //# 영웅 근처 랜덤 오프셋 1~3 유닛
    var offset = UnityEngine.Random.insideUnitSphere * 2f;
    offset.y = 0f;
    SpawnAt(key, nearHero + offset);
}

//# (기존 SpawnMonsters 메서드를 SpawnAt 한 곳에서 재사용 가능하게 분리)
```

---

## 9. 자동화 (Editor 스크립트)

### 9.1 LairCardPrefabBuilder (신규)

`[MenuItem("Lair/Setup/B1 - Build All Cards")]` — 7개 CardData SO + CardPool_Passive SO 자동 생성 + Addressables 등록.

각 SO 의 `[SerializeReference] _effect` 슬롯에 해당 ICardEffect 인스턴스 주입 (SerializedProperty.managedReferenceValue).

```csharp
//# 예시 (실제 구현은 LairCharacterPrefabBuilder 의 SerializedObject 패턴 참고)
foreach (var spec in CardSpecs)
{
    var card = ScriptableObject.CreateInstance<CardData>();
    var so = new SerializedObject(card);
    so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
    so.FindProperty("_category").enumValueIndex = (int)spec.Category;
    so.FindProperty("_displayName").stringValue = spec.DisplayName;
    so.FindProperty("_description").stringValue = spec.Description;
    so.FindProperty("_effect").managedReferenceValue = spec.EffectFactory();
    so.ApplyModifiedPropertiesWithoutUndo();

    AssetDatabase.CreateAsset(card, $"Assets/_Lair/Data/Cards/Cards/{spec.Id}.asset");
    //# Addressables 등록 — address = spec.Id.ToString(), label = "Resource"
}

//# CardPool_Passive.asset 생성 + 7장 List 채우기 + Addressables 등록
```

### 9.2 LairUIPrefabBuilder 확장
`BuildCardSelectionPopup()` 메서드 추가 — Title, HorizontalLayout, 3개 CardView slot, 각 slot 의 Border/Name/Desc/Button 자동 생성 + SerializedObject 주입.

### 9.3 LairCharacterPrefabBuilder 확장
몬스터 프리팹 빌드 시 `MonsterTag.Configure(spec.MonsterKey)` 호출 추가. SerializedObject 로 `_key` enum 주입.

---

## 10. 테스트 전략

### 10.1 EditMode 신규 (~17개)

| 대상 | 케이스 | 수 |
|---|---|---|
| `PauseService` | Pause/Resume timeScale 전환, 중첩, ForcePause | 3 |
| `PassiveTriggerService` | HP 90% 통과 시 OnTriggered(0), 여러 임계점 동시 통과 시 순차 발동, 한 임계점 1회만 | 3 |
| `TriggerQueue` | Enqueue/Dequeue 순서, 빈 큐 false | 2 |
| `CardDeck` | Draw(3) 카드 3장 + 중복 X, 풀 부족 시 가능한 만큼, seed 고정 reproducibility | 3 |
| `PoisonAura` | Tick 누적 1초 마다 데미지 1회, 부착 시 OnAttached 호출, Detach 시 OnDetached | 3 |
| `ECardId/ECardCategory` 매핑 | 효과 클래스가 카드 카테고리와 일관 | 1 (smoke) |
| `BattleContext` (FakeRegistry) | GetMonsters(filter) 살아있는 적만, GetHero null 안전 | 2 |

### 10.2 PlayMode 신규 (~1개)

- `CardFlowSmokeTest` — Battle 씬 로드 → Knight HP 강제 50% → CardSelectionPopup 자동 표시 확인 → 0번 카드 자동 선택(`OnPicked` 호출) → Resume + 게임 진행 확인

### 10.3 수동 검증 체크리스트 (B1-M5)

- [ ] Knight HP 90% 도달 시 CardSelectionPopup 자동 표시 + 시간 정지
- [ ] 카드 3장 모두 다른 카드
- [ ] 카드 클릭 시 효과 적용 + 팝업 닫힘 + 시간 재개
- [ ] HP 80%, 70%, ..., 10% 까지 9회 전부 트리거
- [ ] 큰 데미지로 여러 임계점 동시 통과 시 순차 처리
- [ ] 강화 카드 동작 (슬라임 HP +50% 등) 시각 확인 (HitFlash 그대로)
- [ ] 스폰 카드 동작 (영웅 근처 슬라임 3마리 생성)
- [ ] 교체 카드 동작 (슬라임 사라지고 골렘 1마리 생성)
- [ ] 환경 카드 동작 (영웅 HP 가 1초마다 5 감소 — Console [Combat] 로그)
- [ ] 영웅 사망 시 ResultPopup 정상 (Slice A 회귀)
- [ ] 5분 도달 시 ResultPopup 정상 (Slice A 회귀)

---

## 11. 작업 순서 / 마일스톤

| M | 제목 | 핵심 산출물 | 검증 게이트 | 예상 |
|---|---|---|---|---|
| B1-M1 | Enum/Interface + POCO TDD | CommonEnum 갱신, Card/CommonInterface, CardData/Pool/Deck, PauseService, PassiveTriggerService, TriggerQueue, PoisonAura | EditMode ~17개 PASS | 1일 |
| B1-M2 | 효과 클래스 + Aura 인프라 + MonsterTag | 7개 Effect, HeroAuraRunner, MonsterTag, BattleContext | 컴파일 + 기존 회귀 24/24 | 1일 |
| B1-M3 | 카드 자동 생성 + UI 프리팹 | LairCardPrefabBuilder, LairUIPrefabBuilder.BuildCardSelectionPopup, 7장 CardData + CardPool_Passive + UI 프리팹 + Addressables | 프리팹 YAML 확인, UI 표시 자동 검증 | 1일 |
| B1-M4 | BattleController 통합 + SpawnMonsterRuntime + MonsterTag 부착 | BattleController 풀 큐 루프, 캐릭터 프리팹 재빌드 | 자동 시뮬: Knight 강제 데미지로 트리거 발생 + 카드 자동 선택 후 게임 재개 | 1일 |
| B1-M5 | PlayMode smoke + 수동 9개 체크리스트 + 페이싱 튜닝 | CardFlowSmokeTest, 수동 한 판 풀 플레이 | 자동 25개 + 수동 11개 | 1일 |

**총 ~5일 (1주 안).**

### 마일스톤별 커밋 메시지(안)

```
# [feat] - B1-M1 POCO 로직 TDD (Card 데이터/PauseService/PassiveTriggerService/TriggerQueue/PoisonAura)
# [feat] - B1-M2 카드 효과 7개 + HeroAuraRunner + MonsterTag + BattleContext
# [feat] - B1-M3 카드 SO 7장 + CardPool + CardSelectionPopup 프리팹 자동 생성
# [feat] - B1-M4 BattleController 큐 루프 통합 + 캐릭터 프리팹 재빌드 (MonsterTag 부착)
# [test] - B1-M5 PlayMode smoke + 수동 체크리스트 11개 통과
```

### 사용자 검토 포인트
- **B1-M3 직후**: UI 의 카드 3장 시각/배치 OK 인지 (직접 Play)
- **B1-M4 직후**: 자동 시뮬에서 트리거→일시정지→카드 적용→재개 흐름 OK
- **B1-M5 직후**: 한 판 풀 플레이 + 검증 가설 평가

---

## 12. 위험 / 가정 / 미정 항목

### 12.1 가정
- Time.timeScale=0 시 모든 게임 컴포넌트가 자동 정지 (Slice A 의 시스템 모두 Time.deltaTime 사용 — 검증됨)
- CHMUI.ShowUIAsync 가 timeScale=0 영향 안 받음 (EditorApplication.Update 기반)
- CHButton.onClick 이 timeScale=0 환경에서 정상 발화 (Unity 입력 시스템은 unscaled)

### 12.2 위험 + 완화
| 위험 | 완화 |
|---|---|
| `[SerializeReference]` 의 ICardEffect 가 누락된 SO (null effect) | LairCardPrefabBuilder 가 빌드 시 검증 + 효과 null 시 Apply 스킵 가드 |
| 한 frame 에 여러 임계점 통과 시 큐가 비대 (9개 동시) | TriggerQueue 가 큐 추상화, BattleController 가 순차 처리 — 동작 가능 |
| `SpawnSlimes` 가 영웅 근처에 즉시 3마리 → 영웅 즉사 위험 | offset 2~3 유닛 + 처음엔 비교적 약함, 페이싱 튜닝 단계 (B1-M5) 에서 조정 |
| Time.timeScale=0 시 HUD HP/Timer 갱신 0 — 사용자 변화 못 봄 | 의도된 동작. CardSelectionPopup 이 화면 덮음 |
| 일시정지 중 사용자가 결과 팝업까지 도달 (영웅 HP 0) | EndBattle 가 _model.Result 체크로 중복 방지. 일시정지 중엔 데미지 X 라 안전 |

### 12.3 슬라이스 B1 종료 후 결정
- **B2**: 30초 액티브 트리거 + 카드 10장 + 일시정지 큐 활용
- **B3**: 카드 풀 확장 (패시브 15장 / 액티브 10장)
- **B 폴리시**: 카드 일러스트, 카드 등장 애니메이션, 사운드

---

## 변경 이력
- **v0.1 (2026-05-19)**: 초안. 브레인스토밍 7개 섹션 통합.
