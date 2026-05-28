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
    public class BattleController : MonoBehaviour, ISpawnerHost
    {
        [SerializeField] private Transform _heroSpawn;
        //# 지속 스폰 — 씬에 배치된 Spawner 들. Rule 03 — FindObjectsOfType 대신 인스펙터 직렬 할당.
        [SerializeField] private Spawner[] _spawners;
        //# Slice C — 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
        [SerializeField] private BalanceConfig _balance;

        //# 지속 스폰 — 글로벌 필드 몬스터 하드 캡 (§4.2). 어느 스폰 경로에서든 절대값.
        //# v7: 12 → 18. Power 차등 하향으로 DPS 여유 확보 → 캡 복귀 (continuous-spawn-round.md §6.3).
        private const int MonsterCap = 18;

        //# 지속 스폰 — 종별 누적 스탯 배율 (§3.0.1). 강화 카드가 곱연산 갱신, Pop 시 적용.
        private readonly Dictionary<EMonster, StatMultiplier> _typeModifiers = new();

        //# 스포너 상태 UI — 종별 적용된 강화 카드 픽 누적 (툴팁 본문 + 셀 상단 아이콘 row 의 source).
        //# Source 추적은 ApplyCardEffect(card) 가 _currentCardScope 에 카드를 저장한 동안
        //# RegisterMonsterTypeBuff 가 호출되는 패턴으로만 갱신된다 (기획서 §4.2).
        private readonly Dictionary<EMonster, List<BattleViewModel.AppliedBuff>> _typeModifierPicks = new();

        //# 스포너 상태 UI — 카드 효과 적용 진입점이 임시로 저장하는 현재 픽 카드. RegisterMonsterTypeBuff 가 source 로 읽는다.
        private CardData _currentCardScope;

        //# 스포너 상태 UI — VM 이 구독. 종 단위 강화가 갱신되면 해당 종 출력 스포너 셀 모두 재계산.
        public event System.Action<EMonster> OnTypeModifierChanged;

        private BattleClock _clock;
        private BattleStateModel _model;
        private BattleViewModel _vm;

        private CHPoolable _hero;
        private Health _heroHealth;

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

            //# 3. 영웅 스폰 + Spawner 바인딩.
            //#    스포너 상태 UI — VM 의 _spawnerSnapshots 6개가 HUD 표시보다 먼저 채워져야 한다.
            //#    HUD 가 먼저 뜨면 SpawnerStatusPanel.Bind 시점에 vm.Spawners 가 빈 리스트라 셀 0 개가 만들어진다.
            //#    Spawner.Tick 은 _host == null 이면 early return 이라 사전 Bind 부작용 없음.
            await SpawnHero();
            //# 3초 후 영웅 AutoCombatAI 재활성화 — Start() 를 막지 않도록 백그라운드 실행.
            _ = EnableHeroAIAfterDelay(3f);
            BindSpawners();

            //# 4. HUD 표시 — 스포너 상태 UI 가 진행 바 폴링·툴팁 base 스탯 표시에 필요한 Spawners·Balance 함께 주입.
            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm, Spawners = _spawners, Balance = _balance });

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

            CardPool pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
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

            CardPool activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (activePool != null)
            {
                _activeDeck = new CardDeck(activePool.Cards);
                _allCards.AddRange(activePool.Cards);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _clock?.Tick(dt);
            //# B3 — 글로벌 버프/피의 갈증 시간 진행. Pause 중엔 deltaTime=0 이라 자연 정지.
            _monsterBuffs?.Tick(dt);
            _bloodThirst?.Tick(dt);

            //# 지속 스폰 — Spawner 들의 주기 타이머 틱. Pause 중 dt=0 자연 정지.
            //# 전투 종료 후엔 스폰 중단.
            if (_model != null && _model.Result == BattleResult.None && _spawners != null)
            {
                foreach (Spawner sp in _spawners)
                    if (sp != null) sp.Tick(dt);
            }
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
        //# 스포너 상태 UI — VM 이 Spawner / 본 컨트롤러 이벤트를 구독했으므로 함께 해제.
        private void OnDestroy()
        {
            DespawnOnDeath.MonsterDied -= HandleMonsterDied;
            _vm?.DetachSpawners();
        }

        //# Slice C — BalanceConfig 스탯을 영웅에 적용. Pop 직후 호출.
        //# 영웅 전용 — 글로벌 타입 모디파이어가 없다. 시그니처·동작 그대로 유지.
        private void ApplyStats(GameObject character, BalanceConfig.CharacterStat stat)
        {
            if (character == null || stat == null) return;
            Health health = character.GetComponent<Health>();
            if (health != null) health.SetMax(stat.Hp, resetCurrent: true);
            MeleeAttacker attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null) attacker.Configure(stat.Range, stat.Cooldown, stat.Power);
            SimpleMover mover = character.GetComponent<SimpleMover>();
            if (mover != null) mover.Speed = stat.MoveSpeed;
        }

        //# 지속 스폰 — 몬스터에 raw 스탯 × 글로벌 타입 모디파이어 배율 적용 (§7.5.2).
        //# 모든 몬스터 스폰·소급 경로가 이 한 메서드를 거친다. 영웅은 절대 거치지 않는다.
        //# resetCurrent: 신규 Pop = true(풀피), 강화 카드 필드 소급 = false(현재 HP 보존).
        public void ApplyMonsterStats(GameObject character, EMonster key, bool resetCurrent)
        {
            if (character == null) return;
            BalanceConfig.CharacterStat raw = _balance?.GetMonster(key);
            if (raw == null) return;
            StatMultiplier mul = _typeModifiers.TryGetValue(key, out StatMultiplier m) ? m : StatMultiplier.Identity;

            Health health = character.GetComponent<Health>();
            if (health != null)
                health.SetMax(Mathf.Max(1, Mathf.RoundToInt(raw.Hp * mul.HpMul)), resetCurrent);

            MeleeAttacker attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null)
                attacker.Configure(
                    raw.Range * mul.RangeMul,
                    Mathf.Max(0.05f, raw.Cooldown * mul.CooldownMul),
                    Mathf.Max(1, Mathf.RoundToInt(raw.Power * mul.PowerMul)));

            SimpleMover mover = character.GetComponent<SimpleMover>();
            if (mover != null) mover.Speed = raw.MoveSpeed * mul.MoveSpeedMul;

            //# 플레이그 한정 — 불변 baseline const × 배율. 복리 누적 버그 없음 (§7.5.9).
            PlagueSlowOnHit slow = character.GetComponent<PlagueSlowOnHit>();
            if (slow != null)
                slow.SetSlowFactor(PlagueSlowOnHit.BaseSlowFactor * mul.SlowFactorMul);
        }

        private async Task SpawnHero()
        {
            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (prefab == null)
            {
                Debug.LogError("[BattleController] Knight 프리팹 로드 실패");
                return;
            }

            CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
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

            //# 영웅 이동 3초 지연 — 스폰 직후 AutoCombatAI 비활성화.
            foreach (AutoCombatAI ai in p.GetComponentsInChildren<AutoCombatAI>())
                if (ai != null) ai.enabled = false;
        }

        //# 영웅 AutoCombatAI 를 delay 초 후 활성화. Start() 백그라운드 호출용.
        private async Task EnableHeroAIAfterDelay(float delay)
        {
            await Task.Delay((int)(delay * 1000));
            if (_hero == null) return;
            foreach (Lair.Character.AutoCombatAI ai in _hero.GetComponentsInChildren<Lair.Character.AutoCombatAI>())
                if (ai != null) ai.enabled = true;
        }

        //# 지속 스폰 — 씬의 Spawner 들에 호스트 주입. 이후 Update 가 각자 주기 틱.
        //# 스포너 상태 UI — Spawner 6개 + 본 컨트롤러를 VM 에 묶어 SpawnerSnapshot 통합 노출.
        private void BindSpawners()
        {
            if (_spawners == null) return;
            foreach (Spawner sp in _spawners)
                if (sp != null) sp.Bind(this);
            //# VM 이 초기 스냅샷 폴링 + 이벤트 구독을 시작. Detach 는 OnDestroy 에서.
            _vm?.AttachSpawners(_spawners, this);
        }

        //# 지속 스폰 — 현재 살아있는 필드 몬스터 수 (캡 검사용).
        private static int AliveMonsterCount()
        {
            int n = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
                if (e?.Health != null && e.Health.IsAlive) ++n;
            return n;
        }

        //# ISpawnerHost — Spawner 한 사이클. 사이클 진입 가부는 사이클 단위 검사 (§4.3).
        //# 캡(18) 이상이면 사이클 전량 skip, 미만이면 count 마리 스폰.
        public async void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
        {
            if (_model != null && _model.Result != BattleResult.None) return;
            //# 사이클 진입 판정 — 시작 시 1회 (§4.3 사이클 단위 검사).
            if (AliveMonsterCount() >= MonsterCap) return;   //# 사이클 백오프 (await 전 선검사)

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(type);
            if (prefab == null) return;
            //# await 후 종료/캡 재검사 — 동프레임 인터리브(다른 Spawner·증식) 시에도
            //# 캡 18 절대값 보장 (§4.2). 사이클 진입은 이미 통과했으므로 잔여만 중단한다.
            if (_model != null && _model.Result != BattleResult.None) return;
            for (int i = 0; i < count; ++i)
            {
                //# 마리 단위 캡 재검사 — 사이클 잔여를 중단해 캡을 절대 넘기지 않는다.
                if (AliveMonsterCount() >= MonsterCap) break;
                CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
                if (p == null) continue;
                p.transform.position = exactPos;
                ApplyMonsterStats(p.gameObject, type, resetCurrent: true);
            }
        }

        //# 지속 스폰 — 강화 카드. 글로벌 dict 곱연산 갱신 + 필드 동일 종 소급 (§7.5.3).
        //# 스포너 상태 UI — _currentCardScope 가 non-null (= ApplyCardEffect 진입 중) 이면 source 추적.
        public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
        {
            if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
            {
                m = new StatMultiplier();
                _typeModifiers[type] = m;
            }
            m.Multiply(stat, multiplier);

            //# 필드 동일 종 소급 — resetCurrent:false (현재 HP 보존, 최대치만 상향).
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || e.Health.IsAlive == false || e.Transform == null) continue;
                MonsterTag tag = e.Transform.GetComponent<MonsterTag>();
                if (tag == null || tag.Key != type) continue;
                ApplyMonsterStats(e.Transform.gameObject, type, resetCurrent: false);
            }

            //# 카드 source 가 있으면 픽 누적 추적 (직접 호출 / 시뮬레이션 외 경로는 _currentCardScope null).
            if (_currentCardScope != null)
                TrackCardPick(type, stat, _currentCardScope);

            //# VM 셀이 동일 종 출력 스포너를 모두 갱신하도록 broadcast.
            OnTypeModifierChanged?.Invoke(type);
        }

        //# 스포너 상태 UI — 카드 효과 적용의 단일 진입점 (기획서 §4.2 BLOCKER 4 결정).
        //# 3개 기존 호출지점(card.Effect.Apply(_ctx))을 이 메서드로 치환해 source 를 잠시 보관한다.
        //# ICardEffect / IBattleContext / 25개 효과 클래스 시그니처는 일체 변경하지 않는다.
        public void ApplyCardEffect(CardData card)
        {
            if (card?.Effect == null || _ctx == null) return;
            _currentCardScope = card;
            try { card.Effect.Apply(_ctx); }
            finally { _currentCardScope = null; }
        }

        //# 스포너 상태 UI — _typeModifierPicks 에 픽 누적. 동일 source 면 PickCount++,
        //# 신규 source 면 add. 동일 종·동일 Stat 의 누적 배율은 _typeModifiers 의 Get(stat) 으로 일괄 갱신.
        private void TrackCardPick(EMonster type, EMonsterStatKind stat, CardData source)
        {
            if (_typeModifierPicks.TryGetValue(type, out List<BattleViewModel.AppliedBuff> list) == false)
                _typeModifierPicks[type] = list = new List<BattleViewModel.AppliedBuff>();

            BattleViewModel.AppliedBuff existing = list.Find(b => b.Source == source);
            if (existing != null)
                existing.PickCount++;
            else
                list.Add(new BattleViewModel.AppliedBuff
                {
                    Source = source,
                    PickCount = 1,
                    Stat = stat,
                    AggregateMultiplier = 1f,
                });

            //# 동일 종·동일 Stat 의 엔트리들에 누적 배율을 일괄 동기화 (종 1 ↔ 카드 1 매핑이지만
            //# 향후 1↔다 매핑 확장에 대비해 list 순회로 갱신).
            //# v1.0 — Enhance 카테고리만 갱신 대상. Spawn 엔트리(같은 list 에 들어감)의 AggregateMultiplier 가
            //# 잘못 덮어쓰이지 않도록 Category 필터.
            if (_typeModifiers.TryGetValue(type, out StatMultiplier mul))
            {
                foreach (BattleViewModel.AppliedBuff b in list)
                    if (b.Stat == stat && b.Source != null && b.Source.Category == ECardCategory.Enhance)
                        b.AggregateMultiplier = mul.Get(stat);
            }
        }

        //# v1.0 — Spawn 카테고리 픽 누적. Enhance 의 TrackCardPick 와 자료구조 공유 (Dictionary<EMonster, List<AppliedBuff>>).
        //# Stat 필드는 EMonsterStatKind.Hp (default, BuffLine.FormatBody 의 Category 분기로 읽히지 않음 — §2.5.5 v1.0).
        //# AggregateMultiplier 는 Spawn 에선 의미 없음. retroactive 정책 (§2.3.6) — type 출력 Spawner 가 0 대여도 누적.
        private void TrackSpawnPick(EMonster type, CardData source)
        {
            if (_typeModifierPicks.TryGetValue(type, out List<BattleViewModel.AppliedBuff> list) == false)
                _typeModifierPicks[type] = list = new List<BattleViewModel.AppliedBuff>();

            BattleViewModel.AppliedBuff existing = list.Find(b => b.Source == source);
            if (existing != null)
                existing.PickCount++;
            else
                list.Add(new BattleViewModel.AppliedBuff
                {
                    Source = source,
                    PickCount = 1,
                    Stat = EMonsterStatKind.Hp,    //# default — Category=Spawn 분기에서 안 읽음
                    AggregateMultiplier = 1f,      //# unused for Spawn
                });
        }

        //# 스포너 상태 UI — VM 이 SpawnerSnapshot 채울 때 사용. 없는 종이면 빈 array.
        public IReadOnlyList<BattleViewModel.AppliedBuff> GetAppliedBuffs(EMonster type)
            => _typeModifierPicks.TryGetValue(type, out List<BattleViewModel.AppliedBuff> list)
                ? (IReadOnlyList<BattleViewModel.AppliedBuff>)list
                : System.Array.Empty<BattleViewModel.AppliedBuff>();

        //# 스포너 상태 UI — 툴팁이 base 스탯(Hp/Power/Range/Cooldown/MoveSpeed)을 읽기 위해 노출.
        //# Plague SlowFactor 의 base 는 코드 상수 Lair.Character.PlagueSlowOnHit.BaseSlowFactor 사용 (§2.5.5).
        public BalanceConfig Balance => _balance;

        //# 지속 스폰 — 추가소환 카드. 해당 종을 출력 중인 모든 Spawner 동시 출력 +1.
        //# v1.0 — _currentCardScope non-null (= ApplyCardEffect 진입 중) 이면 Spawn 픽 추적 + 셀 IconRow 갱신 이벤트 발행.
        //# 셀 IconRow Spawn 슬롯은 OnTypeModifierChanged 의 동일 이벤트로 재계산 (의미 확장 — §4.3 v1.0).
        public void IncrementSpawnerOutput(EMonster type)
        {
            if (_spawners == null) return;
            foreach (Spawner sp in _spawners)
                if (sp != null && sp.CurrentType == type) sp.IncrementOutput();

            //# v1.0 — Spawn 픽 누적 (type 출력 Spawner 0 대여도 누적 — retroactive 정책 §2.3.6).
            if (_currentCardScope != null)
                TrackSpawnPick(type, _currentCardScope);

            //# v1.0 — Spawn 슬롯 IconRow 갱신용 broadcast. Enhance 와 같은 이벤트 (의미 확장).
            OnTypeModifierChanged?.Invoke(type);
        }

        //# 지속 스폰 — 융합 카드. 출력 종이 from 인 모든 Spawner 의 출력 종을 to 로 변경.
        public void ReplaceSpawnerOutput(EMonster from, EMonster to)
        {
            if (_spawners == null) return;
            foreach (Spawner sp in _spawners)
                if (sp != null && sp.CurrentType == from) sp.ReplaceOutput(to);
        }

        private async void EndBattle(BattleResult result)
        {
            if (_model.Result != BattleResult.None) return;   //# 중복 방지
            _clock.Stop();

            //# Slice C — 한 판 결과 기록 (생존 몬스터 수 집계)
            int aliveMonsters = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
                if (e?.Health != null && e.Health.IsAlive) aliveMonsters++;
            _recorder.FinishRun(result, _clock.Elapsed, aliveMonsters);

            //# B2 — 트리거 서비스 구독 해제 (BattleClock.OnTick / Health.OnChanged 누수 방지)
            _activeTriggers?.Dispose();
            _passiveTriggers?.Dispose();

            //# 모든 AI 정지
            foreach (AutoCombatAI ai in GetComponentsInChildren<AutoCombatAI>())
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

            while (_queue.TryDequeue(out TriggerQueue.Entry entry))
            {
                if (_model.Result != BattleResult.None) break;

                //# B2 — Source 에 따라 적절한 덱 선택. 덱 미로드면 해당 트리거 스킵.
                CardDeck deck = entry.SourceType == TriggerQueue.Source.Passive ? _passiveDeck : _activeDeck;
                if (deck == null) continue;

#if UNITY_EDITOR
                //# 시뮬레이션 전용 — 팝업/일시정지를 건너뛰고 즉시 픽. tcs 무한 대기 회피.
                if (DebugAutoPicker != null)
                {
                    IReadOnlyList<CardData> simChoices = deck.Draw(3);
                    CardData picked = DebugAutoPicker(simChoices, entry.SourceType);
                    if (picked != null)
                    {
                        _recorder.RecordPick(picked.Id);
                        _vm.AddPick(picked, entry.SourceType == TriggerQueue.Source.Passive);
                        //# 스포너 상태 UI — source 추적용 단일 진입점 (기획서 §4.2).
                        ApplyCardEffect(picked);
                    }
                    continue;
                }
#endif

                _pause.Pause();
                IReadOnlyList<CardData> choices = deck.Draw(3);
                System.Threading.Tasks.TaskCompletionSource<bool> tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                CardSelectionArg arg = new CardSelectionArg
                {
                    Choices = choices,
                    OnPicked = card =>
                    {
                        //# Slice C — 픽 기록
                        if (card != null)
                        {
                            _recorder.RecordPick(card.Id);
                            //# 빌드 패널 — VM 에 픽 누적
                            _vm.AddPick(card, entry.SourceType == TriggerQueue.Source.Passive);
                        }
                        //# 스포너 상태 UI — source 추적용 단일 진입점 (기획서 §4.2).
                        if (card != null) ApplyCardEffect(card);
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
            GameObject heroPrefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (heroPrefab != null) CHMPool.Instance.CreatePool(heroPrefab, count: 1);

            //# 지속 스폰 — 캡 18 + 동시 출력 증가(SpawnX 카드) 대비 → 6종 각 10마리 비축
            foreach (EMonster key in new[] { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper,
                                        EMonster.Hex, EMonster.Plague, EMonster.Phantom })
            {
                GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (prefab != null) CHMPool.Instance.CreatePool(prefab, count: 10);
            }

            //# 시각 이펙트 — PoisonAura + 영웅 디버프 상태 표시 6종. 동시 표시 적어 count 2.
            foreach (EVisual key in new[] { EVisual.PoisonAura,
                                        EVisual.SlowStatus, EVisual.FearStatus, EVisual.WeakenStatus,
                                        EVisual.AttackDownStatus, EVisual.TimeStopStatus, EVisual.BleedStatus })
            {
                GameObject fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (fx != null) CHMPool.Instance.CreatePool(fx, count: 2);
            }
        }

        //# B1 — BattleContext.SpawnMonster 가 호출하는 런타임 스폰 (액티브 증식 카드 등).
        //# 지속 스폰 — 마리 단위 캡 검사 (§4.4 truncate). 캡 이상이면 no-op.
        //# 증식은 이 메서드를 N회 호출하므로 캡 도달 시점부터 자동으로 잘린다.
        public async void SpawnMonsterRuntime(Lair.Data.EMonster key, Vector3 nearHero)
        {
            if (_model != null && _model.Result != BattleResult.None) return;
            if (AliveMonsterCount() >= MonsterCap) return;   //# 빠른 선검사 (await 전)

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null) return;
            //# await 후 재검사 — 동프레임 다중 호출(증식)이 await 로 인터리브돼도 캡 절대값 보장.
            if (_model != null && _model.Result != BattleResult.None) return;
            if (AliveMonsterCount() >= MonsterCap) return;
            CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null) return;

            Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.5f;
            offset.y = 0f;
            p.transform.position = nearHero + offset;
            //# 지속 스폰 — 카드 소환 몬스터도 글로벌 타입 모디파이어 적용 (신규 Pop → resetCurrent:true)
            ApplyMonsterStats(p.gameObject, key, resetCurrent: true);
        }

#if UNITY_EDITOR
        //# ===== Slice C-M4 디버그 API — LairBalanceWindow 전용 =====

        //# 시뮬레이션 전용 — 비-null 이면 카드 팝업 대신 호출되어 즉시 픽 결정.
        //# 인자: 제시된 3장, 트리거 출처. 반환: 고른 카드(null 이면 스킵).
        public System.Func<System.Collections.Generic.IReadOnlyList<CardData>, TriggerQueue.Source, CardData> DebugAutoPicker;

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
            foreach (CardData c in _allCards)
            {
                if (c != null && c.Id == id)
                {
                    //# 스포너 상태 UI — 디버그 경로도 ApplyCardEffect 로 통과 (source 추적 동일하게 적용).
                    ApplyCardEffect(c);
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
