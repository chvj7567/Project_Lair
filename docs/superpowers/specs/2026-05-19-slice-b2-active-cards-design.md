# Slice B2 — 30초 액티브 카드 시스템 설계서

> Project Lair MVP 의 세 번째 수직 슬라이스 — 빌드업 시스템의 액티브 절반.
> 작성일: 2026-05-19
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
**시간 기반 액티브 트리거가 패시브와 어떻게 어우러져 빌드업의 재미를 더하는가** 검증.
B1 의 인프라(Queue/Pause/CardSelectionPopup/CHMPool) 를 그대로 재사용해 액티브 측을 추가.

### 0.2 In Scope (B2)
- 30초 임계점 트리거 (9회: 0:30, 1:00, 1:30, ..., 4:30) — `ActiveTriggerService`
- 액티브 카드 **5장** (즉발 카테고리)
- BattleController 큐 루프 합류 (`TriggerQueue.Source.Active`)
- 액티브 카드 풀 SO (`CardPool_Active`) + Editor 자동 빌더
- 패시브 + 액티브 동시 트리거 시 큐 순차 처리 검증

### 0.3 Out of Scope
- 패시브 카드 풀 15장 확장 (B3 — 컨텐츠)
- 액티브 카드 10장 확장 (B3 — 컨텐츠)
- 카드 등급/희귀도 / 진화 카테고리
- 메타 진행 / 사운드 / 아트

### 0.4 검증 가설
"30초마다 능동적인 선택을 더하면, 빌드업의 결정 빈도와 깊이가 충분히 커지는가." B2-M4 직후 사용자가 한 판 플레이로 판단.

---

## 1. 프로젝트 룰 매핑

| 룰 | 본 설계에서의 적용 |
|---|---|
| 01 자동 커밋 금지 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만 전달 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | 액티브 효과도 IBattleContext 만 사용 — BattleController 미참조 |
| 04 반복 에셋 프리팹화 | CardData SO 5개 + CardPool_Active SO 1개 |
| 05 MVVM | 기존 CardSelectionPopup 재사용 — 변경 X |
| 06 상위 인터페이스 | IBattleContext 재사용 (필요 시 메서드 1~2 추가) |
| 07 ChvjPackage | CHMUI/CHMResource/CHMPool 재사용, 패키지 코드 0 |
| 08 Enum 키 | ECardId 값명 = SO 파일명 + EData.CardPool_Active 키 |
| 09 CommonEnum | ECardId 5건 추가 + EData.CardPool_Active 추가 → 기존 CommonEnum.cs |
| 10 CommonInterface | Card/CommonInterface.cs 에 필요 메서드 추가 (있다면) |
| 11 CHText/CHButton | 변경 없음 — CardView 재사용 |
| 12 CHMPool 스폰 | 소환 효과는 SpawnMonsterRuntime 재사용 (이미 풀 사용) |
| 13 UIArg 동일 파일 | 변경 없음 — CardSelectionArg 재사용 |

---

## 2. 아키텍처 개요

### 2.1 데이터 흐름

```
[BattleClock.OnTick(elapsed)]
        │ 30/60/90/.../270 통과 1회 감지
        ▼
[ActiveTriggerService]
        │ OnTriggered(thresholdIndex)
        ▼
[BattleController]
        │ TriggerQueue.Enqueue(Active, idx) → TryProcessNext()
        │   1) PauseService.Pause()
        │   2) _activeDeck.Draw(3)
        │   3) await CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)
        ▼
[CardSelectionPopup] — 기존 그대로
        ▼
[OnPicked callback]
        │ card.Effect.Apply(_ctx) — 즉발 효과 발생
        │ Close → PauseService.Resume()
        ▼
[게임 재개]
```

### 2.2 단방향 의존
- ActiveTriggerService → BattleClock (구독)
- BattleController → ActiveTriggerService / _activeDeck (소유)
- 액티브 ICardEffect → IBattleContext (Rule 06, 03)
- 역참조 없음

### 2.3 기존 인프라 재사용
- `TriggerQueue` — `Source.Active` 이미 정의 (확인 완료)
- `PauseService` — Pause/Resume 동일
- `CardData` / `CardPool` / `CardDeck` — 동일 (장당 `[SerializeReference] ICardEffect`)
- `CardSelectionPopup` / `CardView` / `CardSelectionArg` — 동일
- `IBattleContext` / `BattleContext` — 동일 (필요 시 메서드 추가)
- `BattleController.TryProcessNext` — `_passiveDeck` 대신 `entry.SourceType` 에 따라 적절한 덱 선택

---

## 3. 신규 컴포넌트 명세

### 3.1 `ActiveTriggerService` (POCO)

```csharp
public class ActiveTriggerService : IDisposable
{
    //# 30, 60, 90, ..., 270 (초) — 총 9개
    private static readonly float[] Thresholds = { 30, 60, 90, 120, 150, 180, 210, 240, 270 };

    private readonly bool[] _fired = new bool[Thresholds.Length];
    private readonly BattleClock _clock;

    public event Action<int> OnTriggered;   //# 0=30s, ..., 8=270s

    public ActiveTriggerService(BattleClock clock)
    {
        _clock = clock;
        _clock.OnTick += HandleTick;
    }

    public void Dispose()
    {
        if (_clock != null) _clock.OnTick -= HandleTick;
    }

    private void HandleTick(float elapsed)
    {
        for (int i = 0; i < Thresholds.Length; ++i)
        {
            if (_fired[i]) continue;
            if (elapsed >= Thresholds[i])
            {
                _fired[i] = true;
                OnTriggered?.Invoke(i);
            }
        }
    }
}
```

**테스트 (TDD):**
- 첫 30초 도달 시 idx=0 발동
- 30초 전엔 발동 X
- 한 번 발동된 임계점 재발동 X (idempotent)
- 큰 dt 로 여러 임계점 동시 통과 시 각각 순차 발동
- Dispose 후 OnTriggered 발생 X

### 3.2 액티브 효과 5종

모두 `[Serializable]` + `ICardEffect` 구현. 즉발성 (오라/지속효과 X).

| ECardId | 효과 | 구현 |
|---|---|---|
| `MonsterAoeDamage` | 살아있는 모든 몬스터에 50 데미지 | `foreach m in ctx.GetMonsters() → m.TakeDamage(50)` |
| `HeroSlow` | 영웅 이동속도 40% 감소 5초 | `IHeroAura(SlowAura) → ctx.ApplyHeroAura` (지속 5초) |
| `HeroSilence` | 영웅 공격 5초 금지 | `IHeroAura(SilenceAura) → ctx.ApplyHeroAura` (지속 5초) |
| `InstantSpawnGolem` | 골렘 1마리 즉시 소환 | `ctx.SpawnMonster(EMonster.Golem, ctx.GetHeroTransform().position)` |
| `InstantSpawnSlimes` | 슬라임 3마리 즉시 소환 | `ctx.SpawnMonster × 3` |

**HeroSlow / HeroSilence 의 구현**: 새 `IHeroAura` 2종 (`SlowAura`, `SilenceAura`).
- `SlowAura.OnAttached`: 영웅의 `IMover.Speed` 백업 후 ×0.6
- `SlowAura.OnDetached`: 원본 Speed 복원
- `SilenceAura.OnAttached`: 영웅의 `MeleeAttacker` 비활성화 (또는 `IAttacker` 의 신규 `Enabled` 토글)
- `SilenceAura.OnDetached`: 활성화 복원

**IBattleContext / IAttacker 추가 메서드 (필요 시):**
- `IAttacker` 에 `bool Enabled { get; set; }` 추가 — 침묵 카드용
- 영웅의 `IMover` 접근을 위한 `IMover GetHeroMover()` — IBattleContext 확장

### 3.3 카드 풀 SO

- `Assets/_Lair/Data/Cards/Active/` 아래 5개 `CardData.asset`
- `Assets/_Lair/Data/CardPool_Active.asset` — 위 5장 참조
- Addressables 키: 파일명 일치 (Rule 08)
- `EData.CardPool_Active` enum 값 추가

### 3.4 Editor 빌더 — `LairCardPrefabBuilder` 확장

기존 `LairCardPrefabBuilder` 의 `BuildAllCards()` 가 `CardPool_Active` 도 함께 생성하도록 확장.

```csharp
[MenuItem("Lair/Setup/B2 - Build Active Cards")]
public static void BuildActiveCards() { ... }
```

각 액티브 카드 SO 의 `_effect` 에 `managedReferenceValue` 로 효과 인스턴스 주입.

### 3.5 BattleController 통합 변화

**추가 필드:**
```csharp
private ActiveTriggerService _activeTriggers;
private CardDeck _activeDeck;
```

**Start() 변화:**
```csharp
//# 액티브 트리거 — BattleClock 시작 직후
_activeTriggers = new ActiveTriggerService(_clock);
_activeTriggers.OnTriggered += idx =>
{
    _queue.Enqueue(TriggerQueue.Source.Active, idx);
    TryProcessNext();
};

var activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
if (activePool != null) _activeDeck = new CardDeck(activePool.Cards);
```

**TryProcessNext() 변화:**
```csharp
while (_queue.TryDequeue(out var entry))
{
    if (_model.Result != BattleResult.None) break;

    var deck = entry.SourceType == TriggerQueue.Source.Passive ? _passiveDeck : _activeDeck;
    if (deck == null) continue;

    _pause.Pause();
    var choices = deck.Draw(3);
    // ... 기존 로직 동일
}
```

**EndBattle() 변화:**
- `_activeTriggers?.Dispose()` 호출 추가

---

## 4. 동시 트리거 처리 (검증 시나리오)

| 시나리오 | 기대 동작 |
|---|---|
| HP 90% 도달 후 곧이어 30초 도달 | 패시브 먼저 큐 진입 → 패시브 팝업 처리 → Resume → 액티브 큐 처리 |
| 30초 도달 시점에 큰 데미지로 HP 90% 동시 도달 | 둘 다 큐 진입. dequeue 순서대로 처리 (Active 가 먼저 Tick 됐을 가능성 ↑) |
| 큐 처리 중 새 트리거 발생 | `_processingQueue` 가드 → 다음 dequeue 에서 자연 처리 |
| 전투 종료 후 트리거 | `_model.Result != None` 가드 → 무시 |

---

## 5. 마일스톤 / 검증 포인트

| 마일스톤 | 산출물 | 검증 |
|---|---|---|
| B2-M1 | ActiveTriggerService + EditMode 테스트 5개 | TDD Red-Green-Refactor |
| B2-M2 | 액티브 효과 5종 + SlowAura/SilenceAura + IAttacker 확장 | EditMode 테스트 (PoisonAura 패턴) |
| B2-M3 | 액티브 카드 SO 5장 + CardPool_Active + Editor 빌더 | 메뉴 실행 → 5장 + 풀 생성 확인 |
| B2-M4 | BattleController 통합 + PlayMode 스모크 1개 | 30초 도달 시 팝업 / 카드 선택 / 효과 발생 |

---

## 6. 위험 요소

| 위험 | 영향 | 완화 |
|---|---|---|
| HeroSlow/Silence 가 영웅 죽음 직전 발동 시 OnDetached 가 호출 안 됨 | 다음 판 영웅 상태 오염 | OnDisable 에서 모든 슬롯 강제 OnDetached (HeroAuraRunner 패턴 동일) |
| ActiveTriggerService 와 BattleClock.Tick 의 호출 순서 (Update vs FixedUpdate) | 미세 타이밍 오차 | BattleClock.OnTick 동기 호출이라 안전 |
| 액티브 + 패시브 동시 트리거가 같은 프레임에서 발생 | 큐가 둘 다 받음 — 순서는 dequeue 결정 | Source 표시로 디버깅 가능. 게임플레이상 큰 문제 없음 |
| MeleeAttacker.Enabled 토글 시 다음 OnEnable 의 reset 충돌 | _lastAttackTime 가 잘못된 값 | SilenceAura.OnDetached 에서 enabled = true 복원 시점에 `_lastAttackTime = NegativeInfinity` 까지 reset |

---

## 7. 성공 기준 (사용자 검증)

- [ ] 30초마다 카드 선택지가 9번 등장 (0:30, 1:00, ..., 4:30)
- [ ] 패시브와 액티브 트리거가 동시 발생해도 둘 다 큐로 순차 처리
- [ ] 5종 효과 각각 화면에서 의도된 변화 (몬스터 데미지/영웅 슬로우/침묵/소환)
- [ ] 영웅 사망 후 새 판 시작 시 슬로우/침묵 상태 잔존 X
- [ ] EditMode + PlayMode 테스트 전부 PASS
