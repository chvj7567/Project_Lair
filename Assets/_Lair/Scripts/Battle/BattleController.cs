using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Card;
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
        //# Slice C — 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
        [SerializeField] private BalanceConfig _balance;

        private BattleClock _clock;
        private BattleStateModel _model;
        private BattleViewModel _vm;

        private CHPoolable _hero;
        private Health _heroHealth;
        private readonly List<CHPoolable> _monsters = new();

        //# B1 신규
        private PauseService _pause;
        private PassiveTriggerService _passiveTriggers;
        private TriggerQueue _queue;
        private CardDeck _passiveDeck;
        private IBattleContext _ctx;
        private bool _processingQueue;

        //# B2 신규
        private ActiveTriggerService _activeTriggers;
        private CardDeck _activeDeck;

        //# B3 신규 — 몬스터 글로벌 버프 / 피의 갈증
        private MonsterBuffService _monsterBuffs;
        private BloodThirstService _bloodThirst;

        //# Slice C — 한 판 결과 측정
        private readonly RunRecorder _recorder = new RunRecorder();

        //# Slice C-M4 — 디버그 카드픽용 전체 카드 (패시브 15 + 액티브 10)
        private readonly List<CardData> _allCards = new();

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

            //# 1.5 풀 사전 워밍 (Rule 12) — 첫 Pop spike 방지 + 카드 스폰 부담 감소
            await PrewarmPools();

            //# 2. MVVM
            _model = new BattleStateModel();
            //# Slice C — BalanceConfig 의 런 길이 적용
            if (_balance == null)
                Debug.LogError("[BattleController] BalanceConfig(_balance) 미할당 — 프리팹 기본 스탯으로 진행");
            else
                _model.TotalSeconds = _balance.RunDuration;
            _vm = new BattleViewModel(_model);

            //# 3. HUD 표시
            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm });

            //# 4. 스폰
            await SpawnHero();
            await SpawnMonsters();

            //# B1 — 일시정지 / 트리거 / 카드 풀
            _pause = new PauseService();
            _queue = new TriggerQueue();
            if (_heroHealth != null)
            {
                _passiveTriggers = new PassiveTriggerService(_heroHealth, _balance?.PassiveThresholds);
                _passiveTriggers.OnTriggered += idx =>
                {
                    _queue.Enqueue(TriggerQueue.Source.Passive, idx);
                    TryProcessNext();
                };
            }
            _ctx = new BattleContext(this);

            //# B3 — 몬스터 글로벌 버프 / 피의 갈증 서비스
            _monsterBuffs = new MonsterBuffService();
            _bloodThirst = new BloodThirstService();
            DespawnOnDeath.MonsterDied += HandleMonsterDied;

            var pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (pool != null)
            {
                _passiveDeck = new CardDeck(pool.Cards);
                _allCards.AddRange(pool.Cards);
            }

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            _clock.Start();

            //# B2 — 30초 액티브 트리거 (BattleClock.OnTick 구독)
            _activeTriggers = new ActiveTriggerService(_clock, _balance?.ActiveThresholds);
            _activeTriggers.OnTriggered += idx =>
            {
                _queue.Enqueue(TriggerQueue.Source.Active, idx);
                TryProcessNext();
            };

            var activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (activePool != null)
            {
                _activeDeck = new CardDeck(activePool.Cards);
                _allCards.AddRange(activePool.Cards);
            }
        }

        private void Update()
        {
            _clock?.Tick(Time.deltaTime);
            //# B3 — 글로벌 버프/피의 갈증 시간 진행. Pause 중엔 deltaTime=0 이라 자연 정지.
            _monsterBuffs?.Tick(Time.deltaTime);
            _bloodThirst?.Tick(Time.deltaTime);
        }

        //# B3 — BattleContext 가 위임하는 글로벌 버프/피의 갈증 진입점.
        public void AddMonsterBuff(EMonsterBuff type, float duration)
            => _monsterBuffs?.AddBuff(type, duration);

        public void ActivateBloodThirst(float duration)
            => _bloodThirst?.Activate(duration);

        //# B3 — 몬스터 사망 시 피의 갈증 회복 트리거 (DespawnOnDeath.MonsterDied 구독).
        private void HandleMonsterDied(Vector3 pos)
            => _bloodThirst?.NotifyMonsterDied(pos);

        //# 정적 이벤트 구독 해제 — 씬 재시작 시 누수 방지.
        private void OnDestroy()
            => DespawnOnDeath.MonsterDied -= HandleMonsterDied;

        //# Slice C — BalanceConfig 스탯을 스폰된 캐릭터에 적용. Pop 직후 호출.
        private void ApplyStats(GameObject character, BalanceConfig.CharacterStat stat)
        {
            if (character == null || stat == null) return;
            var health = character.GetComponent<Health>();
            if (health != null) health.SetMax(stat.Hp, resetCurrent: true);
            var attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null) attacker.Configure(stat.Range, stat.Cooldown, stat.Power);
            var mover = character.GetComponent<SimpleMover>();
            if (mover != null) mover.Speed = stat.MoveSpeed;
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
            //# Slice C — 영웅 스탯 적용 (이후 UpdateHeroHp 가 올바른 값 반영)
            if (_balance != null) ApplyStats(p.gameObject, _balance.Hero);
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
                //# Slice C — 몬스터 스탯 적용
                if (_balance != null) ApplyStats(p.gameObject, _balance.GetMonster(sp.Key));
            }
        }

        private async void EndBattle(BattleResult result)
        {
            if (_model.Result != BattleResult.None) return;   //# 중복 방지
            _clock.Stop();

            //# Slice C — 한 판 결과 기록 (생존 몬스터 수 집계)
            int aliveMonsters = 0;
            foreach (var e in CharacterRegistry.Monsters)
                if (e?.Health != null && e.Health.IsAlive) aliveMonsters++;
            _recorder.FinishRun(result, _clock.Elapsed, aliveMonsters);

            //# B2 — 트리거 서비스 구독 해제 (BattleClock.OnTick / Health.OnChanged 누수 방지)
            _activeTriggers?.Dispose();
            _passiveTriggers?.Dispose();

            //# 모든 AI 정지
            foreach (var ai in GetComponentsInChildren<AutoCombatAI>())
                ai.enabled = false;

            _vm.EndBattle(result);

            await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup,
                new ResultPopupArg { Result = result });
        }

        //# B1+B2 — 큐 비울 때까지 카드 선택 팝업 순차 처리
        private async void TryProcessNext()
        {
            if (_processingQueue) return;
            if (_queue.Count == 0) return;
            if (_model.Result != BattleResult.None) return;

            _processingQueue = true;

            while (_queue.TryDequeue(out var entry))
            {
                if (_model.Result != BattleResult.None) break;

                //# B2 — Source 에 따라 적절한 덱 선택. 덱 미로드면 해당 트리거 스킵.
                var deck = entry.SourceType == TriggerQueue.Source.Passive ? _passiveDeck : _activeDeck;
                if (deck == null) continue;

                _pause.Pause();
                var choices = deck.Draw(3);
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                var arg = new CardSelectionArg
                {
                    Choices = choices,
                    OnPicked = card =>
                    {
                        //# Slice C — 픽 기록
                        if (card != null) _recorder.RecordPick(card.Id);
                        if (card?.Effect != null && _ctx != null) card.Effect.Apply(_ctx);
                        tcs.TrySetResult(true);
                    }
                };
                await CHMUI.Instance.ShowUIAsync(EUI.CardSelectionPopup, arg);
                await tcs.Task;
                _pause.Resume();
            }

            _processingQueue = false;
        }

        //# Rule 12 — 알려진 풀 대상을 미리 CreatePool 로 비축. 첫 Pop spike 방지.
        private async Task PrewarmPools()
        {
            //# 영웅 1마리
            var heroPrefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (heroPrefab != null) CHMPool.Instance.CreatePool(heroPrefab, count: 1);

            //# 자연 스폰 + 카드 소환/증식/교체 대비 → 6종 각 5마리 비축
            foreach (var key in new[] { EMonster.Slime, EMonster.Golem, EMonster.Orc,
                                        EMonster.Archer, EMonster.Spider, EMonster.Bat })
            {
                var prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (prefab != null) CHMPool.Instance.CreatePool(prefab, count: 5);
            }

            //# 시각 이펙트 — PoisonAura + 영웅 디버프 상태 표시 6종. 동시 표시 적어 count 2.
            foreach (var key in new[] { EVisual.PoisonAura,
                                        EVisual.SlowStatus, EVisual.FearStatus, EVisual.WeakenStatus,
                                        EVisual.AttackDownStatus, EVisual.TimeStopStatus, EVisual.BleedStatus })
            {
                var fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (fx != null) CHMPool.Instance.CreatePool(fx, count: 2);
            }
        }

        //# B1 — BattleContext.SpawnMonster 가 호출하는 런타임 스폰
        //# 주의: 여기서 생성된 몬스터는 _monsters 리스트에 추가되지 않음 (현재 코드에서 _monsters 는 사용 안 됨)
        public async void SpawnMonsterRuntime(Lair.Data.EMonster key, Vector3 nearHero)
        {
            var prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null) return;
            var p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null) return;

            var offset = UnityEngine.Random.insideUnitSphere * 2.5f;
            offset.y = 0f;
            p.transform.position = nearHero + offset;
            //# Slice C — 카드 소환 몬스터도 스탯 적용
            if (_balance != null) ApplyStats(p.gameObject, _balance.GetMonster(key));
        }

#if UNITY_EDITOR
        //# ===== Slice C-M4 디버그 API — LairBalanceWindow 전용 =====

        //# 패시브 카드 선택을 즉시 큐에 넣음.
        public void DebugForcePassiveTrigger()
        {
            if (_queue == null) return;
            _queue.Enqueue(TriggerQueue.Source.Passive, 0);
            TryProcessNext();
        }

        //# 액티브 카드 선택을 즉시 큐에 넣음.
        public void DebugForceActiveTrigger()
        {
            if (_queue == null) return;
            _queue.Enqueue(TriggerQueue.Source.Active, 0);
            TryProcessNext();
        }

        //# 지정 카드의 효과를 팝업 없이 즉시 적용.
        public void DebugApplyCard(ECardId id)
        {
            foreach (var c in _allCards)
            {
                if (c != null && c.Id == id)
                {
                    if (c.Effect != null && _ctx != null) c.Effect.Apply(_ctx);
                    return;
                }
            }
            Debug.LogWarning($"[BattleController] 디버그 카드 미발견: {id}");
        }

        //# 영웅 HP 를 목표값으로 설정 (delta 만큼 데미지/회복).
        public void DebugSetHeroHp(int hp)
        {
            if (_heroHealth == null) return;
            int delta = hp - _heroHealth.Current;
            if (delta < 0)      _heroHealth.TakeDamage(-delta);
            else if (delta > 0) _heroHealth.Heal(delta);
        }

        //# 영웅 즉사.
        public void DebugKillHero()
        {
            if (_heroHealth != null) _heroHealth.TakeDamage(_heroHealth.Current);
        }

        //# 전투 즉시 종료.
        public void DebugEndBattle(BattleResult result)
        {
            if (_model == null || _clock == null) return;
            EndBattle(result);
        }
#endif
    }
}
