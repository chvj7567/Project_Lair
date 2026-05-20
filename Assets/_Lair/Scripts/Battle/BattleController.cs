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
                _passiveTriggers = new PassiveTriggerService(_heroHealth);
                _passiveTriggers.OnTriggered += idx =>
                {
                    _queue.Enqueue(TriggerQueue.Source.Passive, idx);
                    TryProcessNext();
                };
            }
            _ctx = new BattleContext(this);

            var pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (pool != null) _passiveDeck = new CardDeck(pool.Cards);

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            _clock.Start();

            //# B2 — 30초 액티브 트리거 (BattleClock.OnTick 구독)
            _activeTriggers = new ActiveTriggerService(_clock);
            _activeTriggers.OnTriggered += idx =>
            {
                _queue.Enqueue(TriggerQueue.Source.Active, idx);
                TryProcessNext();
            };

            var activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (activePool != null) _activeDeck = new CardDeck(activePool.Cards);
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
            if (_model.Result != BattleResult.None) return;   //# 중복 방지
            _clock.Stop();

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

            //# 자연 스폰 1 + 카드 SpawnSlimes 가 3마리 + 여유 → 슬라임/골렘/오크 각 5
            foreach (var key in new[] { EMonster.Slime, EMonster.Golem, EMonster.Orc })
            {
                var prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (prefab != null) CHMPool.Instance.CreatePool(prefab, count: 5);
            }

            //# 시각 이펙트 — 동시 표시 1개라 워밍 2 로 여유
            var fx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.PoisonAura);
            if (fx != null) CHMPool.Instance.CreatePool(fx, count: 2);
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
        }
    }
}
