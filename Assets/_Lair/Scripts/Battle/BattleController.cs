using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# 전투 씬 라이프사이클·스폰·VM 갱신·종료 처리.
    public class BattleController : MonoBehaviour, ISpawnerHost
    {
        //# Entry/march 시스템 단일 진실. 씬에 BattleZone GameObject 1개 배치 후 인스펙터 할당.
        //# 미할당 시 _heroSpawn fallback 동작 (안전 경로).
        [SerializeField] private BattleZone _zone;

        //# (deprecated, fallback only — BattleZone 미할당 시 사용) 영웅 초기 스폰 Transform.
        [SerializeField] private Transform _heroSpawn;
        //# 지속 스폰 — 씬에 배치된 Spawner 들. Rule 03 — FindObjectsOfType 대신 인스펙터 직렬 할당.
        [SerializeField] private Spawner[] _spawners;
        //# Slice C — 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
        [SerializeField] private BalanceConfig _balance;

        //# v0.2 메타 — 상점 보너스/보상 정산 수치 (씬 인스펙터 할당). null 이면 메타 로직 전부 skip (기존 테스트/씬 무영향).
        [SerializeField] private MetaConfig _metaConfig;

        //# 지속 스폰 — 종별 누적 스탯 배율 (§3.0.1). 강화 카드가 곱연산 갱신, Pop 시 적용.
        private readonly Dictionary<EMonster, StatMultiplier> _typeModifiers = new();

        //# 스포너 상태 UI — 종별 적용된 강화 카드 픽 누적 (툴팁 본문 + 셀 상단 아이콘 row 의 source).
        //# Source 추적은 ApplyCardEffect(card) 가 _currentCardScope 에 카드를 저장한 동안
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

        //# 상태 아이콘 — ECardId→Sprite 해석 dict. HUD 에 reference 주입 후 카드 풀 로드 시점에 populate.
        //# 인스턴스 1회 생성·재할당 금지 — HUD 가 stale empty reference 를 들지 않도록(주입은 reference).
        private readonly Dictionary<ECardId, Sprite> _cardIconById = new();
        //# 상태 아이콘 — 영웅 HeroAuraRunner 이벤트 구독 핸들러. OnDestroy 에서 정확히 -=.
        private HeroAuraRunner _heroAuraRunner;
        private System.Action<object, ECardId> _onStatusShownHandler;
        private System.Action<object> _onStatusHiddenHandler;

        //# 스킬 해금 컷인 — 정지·쉐이크·배너 오케스트레이터 + 배너 View 핸들 + 러너 핸들.
        //# 구독은 _cutscene 생성 후 — SpawnHero(await) 안의 Bind 시점엔 _cutscene 미생성이므로 러너만 캐싱.
        private SkillUnlockCutsceneController _cutscene;
        private SkillUnlockBannerView _bannerView;
        private HeroSkillRunner _skillRunner;

        //# B1 신규
        private PauseService _pause;
        private PassiveTriggerService _passiveTriggers;
        private TriggerQueue _queue;
        private CardDeck _passiveDeck;
        private IBattleContext _ctx;
        private bool _processingQueue;

        //# Spawner Tick 게이트 — BindSpawners(전투 시작 직후) 에서 true. 영웅 입장과 무관하게 즉시 스폰 개시.
        private bool _spawnersActive;

        //# 영웅 zone 중심 도달 1회 처리 게이트 — clock 시작 + 영웅 AI 활성화. _spawnersActive 와 분리.
        private bool _heroEntered;

        //# HeroEntryDriver 핸들 — Center 도달 후 비활성화 fallback 용.
        private HeroEntryDriver _heroEntryDriver;

        //# B2 신규
        private ActiveTriggerService _activeTriggers;
        private CardDeck _activeDeck;

        //# B3 신규 — 몬스터 글로벌 버프 / 피의 갈증
        private MonsterBuffService _monsterBuffs;
        private BloodThirstService _bloodThirst;

        //# 카드 리뉴얼 v0.6 — 빌드 시너지 (Phase 1 Task 4). BattleContext 가 위임 호출.
        //# Tier 바인딩은 Phase 2 Task 11 에서 12개 (4축 × 3Tier) 추가.
        private BuildSynergyService _synergy;
        private CardPickCounter _pickCounter;   //# 카드 3픽 캡 (전역)

#if UNITY_EDITOR
        //# 디버그 — 지정 카드 강제 등장 (LairBalanceWindow 토글, 기본 OFF). 팝업 드로우 경로 한정.
        private ForcedCardDraw _forcedCardDraw;
#endif

        //# Slice C — 한 판 결과 측정
        private readonly RunRecorder _recorder = new RunRecorder();

        //# v0.2 메타 — 도감 기록용 이번 판 등장 종 / 픽 카드 (EndBattle 정산에서 프로필로 이관).
        private readonly HashSet<EMonster> _seenMonsters = new();
        private readonly List<ECardId> _runPicks = new();

        //# Slice C-M4 — 디버그 카드픽용 전체 카드 (패시브 15 + 액티브 10)
        private readonly List<CardData> _allCards = new();

        async void Start()
        {
            //# 매 Battle 진입(재시작 포함)마다 전투 BGM 을 0초부터 재생 — 프리웜 await 전에 정지해 마을 BGM 잔존 차단.
            CHMSound.Instance.StopAllBGM();
            CHMSound.Instance.Play(EAudio.Bgm);

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
            await SpawnHero();
            //# zone 폴백 분기에서 이미 EnableHeroAIAfterDelay 호출됨. zone 활성 분기는 HandleHeroReachedCenter 가 담당.
            BindSpawners();

            //# v0.2 메타 — 상점 영구 업그레이드를 전투 시작 배율로 적용 (BindSpawners 의 base 주기 주입 뒤, 첫 스폰 전).
            ApplyMetaBonuses();

            //# 4. HUD 표시 — 스포너 상태 UI 가 진행 바 폴링·툴팁 base 스탯 표시에 필요한 Spawners·Balance 함께 주입.
            //#    프리팹 로드 실패 시 ShowUIAsync 는 null 을 반환하므로(부트 정지 방지), 명확히 로그하고 부팅은 계속한다.
            UIBase hudUI = await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm, Spawners = _spawners, Balance = _balance, CardIcons = _cardIconById });
            if (hudUI == null)
            {
                Debug.LogError("[BattleController] BattleHud UI 표시 실패(프리팹 로드/캔버스 확보 불가) — HUD 없이 부팅 계속");
            }

            //# B1 — 일시정지 / 트리거 / 카드 풀
            _pause = new PauseService();
            _queue = new TriggerQueue();

            //# 스킬 해금 컷인 — 배너 팝업 1회 표시해 View 핸들 확보 후 구독.
            //# 콜백형으로 부트와 분리 — 배너는 부트 진행을 막을 필요 없는 부수 표시라 fire-and-forget.
            SetupSkillUnlockCutscene();
            if (_heroHealth != null)
            {
                _passiveTriggers = new PassiveTriggerService(_heroHealth, _balance?.PassiveThresholds);
                _passiveTriggers.OnTriggered += idx =>
                {
                    _queue.Enqueue(TriggerQueue.Source.Passive, idx);
                    TryProcessNext();
                };
            }
            //# 카드 리뉴얼 v0.6 — 빌드 시너지 (Phase 1+2). BattleContext 보다 먼저 생성해 주입.
            //# 라운드 시작 시 카운트 초기화 (씬 1회 진입 = 1 라운드라 Reset 필수 호출 아니지만, Restart 패턴 대비).
            _synergy = new BuildSynergyService();
            _synergy.Reset();
            //# 카드 3픽 캡 — 시너지와 동일 시점 초기화 (Restart 패턴 대비).
            _pickCounter = new CardPickCounter();
            _pickCounter.Reset();
#if UNITY_EDITOR
            //# 디버그 강제 등장 — 픽 순번 카운터를 _pickCounter 와 동일 시점에 새로 시작 (Restart 패턴 대비).
            _forcedCardDraw = new ForcedCardDraw();
#endif
            //# Phase 2 Task 11 — 12개 Tier 바인딩 (4축 × 3Tier). 기획서 §4.2 표.
            _synergy.BindTier(EBuildAxis.Tank,   BuildSynergyService.Tier1Threshold, new TankSynergyTier1());
            _synergy.BindTier(EBuildAxis.Tank,   BuildSynergyService.Tier2Threshold, new TankSynergyTier2());
            _synergy.BindTier(EBuildAxis.Tank,   BuildSynergyService.Tier3Threshold, new TankSynergyTier3());
            _synergy.BindTier(EBuildAxis.Dps,    BuildSynergyService.Tier1Threshold, new DpsSynergyTier1());
            _synergy.BindTier(EBuildAxis.Dps,    BuildSynergyService.Tier2Threshold, new DpsSynergyTier2());
            _synergy.BindTier(EBuildAxis.Dps,    BuildSynergyService.Tier3Threshold, new DpsSynergyTier3());
            _synergy.BindTier(EBuildAxis.Debuff, BuildSynergyService.Tier1Threshold, new DebuffSynergyTier1());
            _synergy.BindTier(EBuildAxis.Debuff, BuildSynergyService.Tier2Threshold, new DebuffSynergyTier2());
            _synergy.BindTier(EBuildAxis.Debuff, BuildSynergyService.Tier3Threshold, new DebuffSynergyTier3());
            _synergy.BindTier(EBuildAxis.Swarm,  BuildSynergyService.Tier1Threshold, new SwarmSynergyTier1());
            _synergy.BindTier(EBuildAxis.Swarm,  BuildSynergyService.Tier2Threshold, new SwarmSynergyTier2());
            _synergy.BindTier(EBuildAxis.Swarm,  BuildSynergyService.Tier3Threshold, new SwarmSynergyTier3());
            _ctx = new BattleContext(this, _synergy);

            //# B3 — 몬스터 글로벌 버프 / 피의 갈증 서비스
            _monsterBuffs = new MonsterBuffService();
            _bloodThirst = new BloodThirstService();
            DespawnOnDeath.MonsterDied += HandleMonsterDied;

            CardPool pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (pool != null)
            {
                _passiveDeck = new CardDeck(pool.Cards);
                _allCards.AddRange(pool.Cards);
                //# 상태 아이콘 — 패시브 풀도 스캔(HeroAttackDown=14 가 유일하게 여기 있음. 누락 시 silent-bug).
                PopulateCardIcons(pool.Cards);
            }

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            //# 폴백 분기(zone 미할당)는 즉시 시작.
            if (_zone == null)
            {
                _clock.Start();
            }
            //# zone 활성 분기 — race 보강. clock 생성 전 영웅이 이미 입장했으면(L334 _clock?.Start() 가 null no-op)
            //# 여기서 시작. 아직이면 이후 HandleHeroReachedCenter 가 시작 → 두 진입 순서 모두 정확히 1회 Start.
            else if (_heroEntered)
            {
                _clock.Start();
            }

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
                //# 상태 아이콘 — 액티브 풀 스캔(8종 중 7종이 여기).
                PopulateCardIcons(activePool.Cards);
            }
        }

        //# 상태 아이콘 — ECardId→Sprite 매핑을 카드 풀에서 채운다(reference 보존 — 재할당 금지).
        //# 인덱서 사용(중복 키 throw 회피) + null icon skip. 패시브/액티브 두 풀 모두에서 호출.
        private void PopulateCardIcons(IReadOnlyList<CardData> cards)
        {
            if (cards == null) return;
            foreach (CardData card in cards)
            {
                if (card == null || card.Icon == null) continue;
                _cardIconById[card.Id] = card.Icon;
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
            //# 전투 종료 후엔 스폰 중단. BindSpawners 전(_spawnersActive=false) 엔 무동작.
            if (_spawnersActive && _model != null && _model.Result == BattleResult.None && _spawners != null)
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

        //# 스킬 해금 컷인 셋업 — 배너 팝업을 콜백형으로 표시(부트 미블로킹). 프리팹 부재 시 콜백 미호출 → 컷인 비활성.
        //# 기존 _pause(카드픽과 depth 공유) + 카메라 쉐이크 + 배너. host=this(코루틴 구동).
        private void SetupSkillUnlockCutscene()
        {
            CHMUI.Instance.ShowUI(EUI.SkillUnlockBanner, new SkillUnlockBannerArg(), ui =>
            {
                _bannerView = ui as SkillUnlockBannerView;
                ICameraShake cameraShake = Camera.main != null ? Camera.main.GetComponent<ICameraShake>() : null;
                if (_bannerView == null || cameraShake == null)
                {
                    Debug.LogWarning("[BattleController] 스킬 해금 컷인 비활성 — 배너 View 또는 ICameraShake 미확보");
                    return;
                }

                _bannerView.HideImmediate();
                _cutscene = new SkillUnlockCutsceneController(_pause, cameraShake, _bannerView, this);

                //# 해금 구독 — 러너가 SpawnHero 에서 캐싱됨. 재시작 이중구독 방지(-= 후 +=).
                if (_skillRunner != null)
                {
                    _skillRunner.OnSkillUnlocked -= HandleSkillUnlocked;
                    _skillRunner.OnSkillUnlocked += HandleSkillUnlocked;
                }
            });
        }

        //# 스킬 해금 — 러너 이벤트를 컷인 큐로 라우팅. data null/컷인 미구성 시 무해.
        private void HandleSkillUnlocked(HeroSkillData data)
        {
            if (_cutscene != null && data != null)
                _cutscene.Enqueue(data.DisplayName);
        }

        //# 정적 이벤트 구독 해제 — 씬 재시작 시 누수 방지.
        //# 스포너 상태 UI — VM 이 Spawner / 본 컨트롤러 이벤트를 구독했으므로 함께 해제.
        private void OnDestroy()
        {
            DespawnOnDeath.MonsterDied -= HandleMonsterDied;
            if (_zone != null) _zone.OnHeroReachedCenter -= HandleHeroReachedCenter;
            if (_skillRunner != null) _skillRunner.OnSkillUnlocked -= HandleSkillUnlocked;
            _vm?.DetachSpawners();
            TeardownHeroAuraEvents();
        }

        //# 상태 아이콘 — 영웅 HeroAuraRunner 이벤트를 VM 으로 forward 구독.
        //# 영웅은 풀 count-1 → 씬 재진입 시 같은 인스턴스 재사용. 재구독 전 기존 구독 정리(중복 방지).
        private void SetupHeroAuraEvents(GameObject heroGo)
        {
            if (heroGo == null) return;
            TeardownHeroAuraEvents();

            HeroAuraRunner runner = heroGo.GetComponent<HeroAuraRunner>();
            if (runner == null) runner = heroGo.AddComponent<HeroAuraRunner>();
            _heroAuraRunner = runner;

            _onStatusShownHandler  = (key, id) => _vm?.AddStatusIcon(key, id);
            _onStatusHiddenHandler = key => _vm?.RemoveStatusIcon(key);
            runner.OnStatusShown  += _onStatusShownHandler;
            runner.OnStatusHidden += _onStatusHiddenHandler;
        }

        //# 상태 아이콘 — 구독 해제(저장 핸들러로 정확히 -=). 재구독·OnDestroy 양쪽에서 호출.
        private void TeardownHeroAuraEvents()
        {
            if (_heroAuraRunner != null)
            {
                if (_onStatusShownHandler != null)  _heroAuraRunner.OnStatusShown  -= _onStatusShownHandler;
                if (_onStatusHiddenHandler != null) _heroAuraRunner.OnStatusHidden -= _onStatusHiddenHandler;
            }
            _heroAuraRunner = null;
            _onStatusShownHandler = null;
            _onStatusHiddenHandler = null;
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
            //# 우선순위 — BattleZone.HeroEntryPoint > _heroSpawn > Vector3.zero.
            Vector3 spawnPos;
            if (_zone != null && _zone.HeroEntryPoint != null)
                spawnPos = _zone.HeroEntryPoint.position;
            else if (_heroSpawn != null)
                spawnPos = _heroSpawn.position;
            else
                spawnPos = Vector3.zero;
            p.transform.position = spawnPos;

            _hero = p;
            _heroHealth = p.GetComponent<Health>();

            //# 상태 아이콘 — HeroAuraRunner 를 셋업 시점에 확보(BattleContext/PlagueSlowOnHit 가 같은 인스턴스 사용).
            //# 이벤트를 VM 으로 forward. 핸들러는 필드 보관 → OnDestroy 에서 정확히 -=.
            SetupHeroAuraEvents(p.gameObject);

            //# Slice C — 영웅 스탯 적용 (이후 UpdateHeroHp 가 올바른 값 반영)
            if (_balance != null) ApplyStats(p.gameObject, _balance.Hero);
            if (_heroHealth != null)
            {
                _heroHealth.OnChanged += _vm.UpdateHeroHp;
                _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
                _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
            }

            //# 영웅 스킬 — 로드아웃 로드 후 러너에 주입(런타임 Bind, 풀 reference 안전).
            //# 컷인 구독은 여기서 하지 않는다 — _cutscene 은 SpawnHero await 종료 후(Start L115 이후) 생성됨.
            //# 러너만 필드에 캐싱하고, 컨트롤러 생성 직후 Start 에서 구독한다(이중구독 가드 포함).
            HeroSkillRunner skillRunner = p.GetComponent<HeroSkillRunner>();
            if (skillRunner != null)
            {
                _skillRunner = skillRunner;
                HeroSkillLoadout loadout = await CHMResource.Instance.LoadAsync<HeroSkillLoadout>(EData.HeroSkillLoadout);
                if (loadout != null)
                    skillRunner.Bind(loadout);
                else
                    Debug.LogWarning("[BattleController] HeroSkillLoadout 로드 실패 — 영웅 스킬 비활성");
            }

            //# 영웅 AutoCombatAI 비활성 — HeroEntryDriver 가 march 를 수행.
            foreach (AutoCombatAI ai in p.GetComponentsInChildren<AutoCombatAI>())
                if (ai != null) ai.enabled = false;

            //# BattleZone 이 있을 때만 HeroEntryDriver 동작. 없으면 기존 EnableHeroAIAfterDelay 폴백.
            if (_zone != null)
            {
                //# 풀 Pop 직후 SimpleMover._clampZone 런타임 주입 — 씬 인스펙터 와이어링은 풀 reference broken 이라 사용 불가 (game-designer §12.B).
                SimpleMover heroMover = p.GetComponent<SimpleMover>();
                if (heroMover != null) heroMover.BindClampZone(_zone);

                _heroEntryDriver = p.GetComponent<HeroEntryDriver>();
                if (_heroEntryDriver == null)
                    _heroEntryDriver = p.gameObject.AddComponent<HeroEntryDriver>();
                _heroEntryDriver.Bind(_zone);
                _heroEntryDriver.enabled = true;
                _zone.OnHeroReachedCenter += HandleHeroReachedCenter;
            }
            else
            {
                //# 폴백 — 기존 3초 후 AI 활성화.
                _ = EnableHeroAIAfterDelay(3f);
                _spawnersActive = true;   //# zone 미할당 시 즉시 spawner 활성
            }
        }

        //# BattleZone.OnHeroReachedCenter 핸들러 — 영웅이 zone 중심 도달 시 clock 시작 + 영웅 AI 활성화.
        //# 1회 동작 보장 — _heroEntered 가 false 일 때만 진입. 스폰 개시(_spawnersActive)는 BindSpawners 가 담당.
        private void HandleHeroReachedCenter()
        {
            if (_heroEntered) return;
            _heroEntered = true;

            //# 영웅 AutoCombatAI 활성화 — MonsterTargetProvider 가 zone 안 Engaging 몬스터 검색.
            if (_hero != null)
            {
                foreach (AutoCombatAI ai in _hero.GetComponentsInChildren<AutoCombatAI>())
                    if (ai != null) ai.enabled = true;
            }

            //# HeroEntryDriver 1회 비활성화 보강 — driver 자체도 enabled=false 호출하지만 안전망.
            if (_heroEntryDriver != null) _heroEntryDriver.enabled = false;

            //# BattleClock 시작 — 5분 카운트다운 개시.
            _clock?.Start();
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
            {
                if (sp == null) continue;
                sp.Bind(this, _zone);
                //# BalanceConfig 종별 base 주기 주입 — 카드 ScalePeriod(곱연산) 보다 시간상 먼저 (기획서 §3.3).
                if (_balance != null)
                    sp.SetBasePeriod(_balance.GetSpawnPeriod(sp.CurrentType));
            }
            //# 전투 시작 직후 스폰 개시 — 영웅 입장 완료를 기다리지 않고 첫 Tick 에 즉시 첫 스폰.
            //# (clock 시작·영웅 AI 활성화는 HandleHeroReachedCenter 가 별도로 처리.)
            _spawnersActive = true;
            //# VM 이 초기 스냅샷 폴링 + 이벤트 구독을 시작. Detach 는 OnDestroy 에서.
            _vm?.AttachSpawners(_spawners, this);
        }

        //# v0.2 메타 — 상점 레벨 → 종 스탯 배율(_typeModifiers) + 스포너 주기 일괄 적용 (기획서 §3.2 / plan Task 3.1).
        //# _metaConfig 미할당이면 전부 skip — 기존 씬/테스트 호환 (plan G2).
        private void ApplyMetaBonuses()
        {
            if (_metaConfig == null) return;

            MetaProfile profile = MetaSession.GetOrLoad();
            MetaBattleBonus bonus = MetaBattleBonus.From(profile, _metaConfig);

            //# 몬스터 전종 글로벌 — 카드 RegisterMonsterTypeBuff 와 동일 표면(StatMultiplier)에서 곱연산 누적.
            foreach (EMonster type in (EMonster[])System.Enum.GetValues(typeof(EMonster)))
            {
                foreach (EMonsterStatKind kind in (EMonsterStatKind[])System.Enum.GetValues(typeof(EMonsterStatKind)))
                {
                    float mul = bonus.GetStatMul(kind);
                    if (Mathf.Approximately(mul, 1f)) continue;

                    if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
                    {
                        m = new StatMultiplier();
                        _typeModifiers[type] = m;
                    }
                    m.Multiply(kind, mul);
                }
            }

            //# 스포너 주기 — BindSpawners 의 SetBasePeriod(절대 대입) 이후라 곱연산 안전.
            if (Mathf.Approximately(bonus.SpawnerPeriodMul, 1f) == false && _spawners != null)
            {
                foreach (Spawner sp in _spawners)
                {
                    if (sp != null)
                    {
                        sp.ScalePeriod(bonus.SpawnerPeriodMul);
                    }
                }
            }
        }

        //# ISpawnerHost — Spawner 한 사이클. 종료 검사만 통과하면 count 전량 스폰 (동시 캡 제거, spec §2.A).
        public async void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
        {
            if (_model != null && _model.Result != BattleResult.None) return;
            //# v0.2 메타 — 도감 등장 종 기록 (HashSet — 중복 무해).
            _seenMonsters.Add(type);

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(type);
            if (prefab == null) return;
            //# await 후 종료 재검사 — 인터리브로 await 동안 전투가 끝났으면 잔여 스폰 중단.
            if (_model != null && _model.Result != BattleResult.None) return;
            for (int i = 0; i < count; ++i)
            {
                CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
                if (p == null) continue;
                //# 산개 — SpawnMonsterRuntime 과 동일. count 만큼 같은 exactPos 에 겹쳐 1마리처럼 보이는 문제 차단.
                //# 몬스터는 OnEnable 에서 isKinematic 이라 물리로 흩어지지 않으므로 스폰 시 좌표를 분산한다.
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.5f;
                offset.y = 0f;
                p.transform.position = exactPos + offset;
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
        public void ApplyCardEffect(CardData card)
        {
            if (card?.Effect == null || _ctx == null) return;
            _currentCardScope = card;
            try
            {
                //# 빌드 시너지 카운트 등록 (임계 도달 시 시너지 Tier Apply 1회 발화). Effect.Apply 직전 호출.
                _ctx.RegisterCardPick(card.Axis);
                card.Effect.Apply(_ctx);
            }
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
            if (_typeModifiers.TryGetValue(type, out StatMultiplier mul))
            {
                foreach (BattleViewModel.AppliedBuff b in list)
                    if (b.Stat == stat && b.Source != null && b.Source.Axis == EBuildAxis.Tank)
                        b.AggregateMultiplier = mul.Get(stat);
            }
        }

        //# v1.0 — Spawn 카테고리 픽 누적. Enhance 의 TrackCardPick 와 자료구조 공유 (Dictionary<EMonster, List<AppliedBuff>>).
        //# Stat 필드는 EMonsterStatKind.Hp (기본값) — 표시 가공 모듈이 카테고리 분기로 읽지 않음.
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

        //# 카드 리뉴얼 v0.6 — SpawnerHaste 카드 / Swarm Tier2. 모든 Spawner 의 _spawnPeriod ×mul (영구).
        public void ScaleAllSpawnerPeriods(float mul)
        {
            if (_spawners == null || mul <= 0f) return;
            foreach (Spawner sp in _spawners)
                if (sp != null) sp.ScalePeriod(mul);
        }

        //# 카드 리뉴얼 v0.6 — Swarm Tier3. 모든 Spawner 의 _outputCount +delta (영구).
        public void IncrementAllSpawnerOutputs(int delta)
        {
            if (_spawners == null || delta <= 0) return;
            foreach (Spawner sp in _spawners)
                if (sp != null) sp.IncrementOutput(delta);
        }

        //# 카드 리뉴얼 v0.6 — FastBreeding 카드 전용. 출력 종이 type 인 Spawner 만 ScalePeriod(mul).
        //# ScaleAllSpawnerPeriods 와 달리 종 매칭. 매칭 Spawner 0개면 no-op.
        public void ScaleSpawnerPeriodForType(EMonster type, float mul)
        {
            if (_spawners == null || mul <= 0f) return;
            foreach (Spawner sp in _spawners)
                if (sp != null && sp.CurrentType == type) sp.ScalePeriod(mul);
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

            //# v0.2 메타 — 보상 정산. RunRecorder(에디터 한정)와 별개로 빌드에서도 동작.
            //# 순서 계약 (기획서 §11.4): 런 보상 → 프로필 가산 → 영주 보상 자동 수령 → 도전과제 → 저장 → Arg 구성.
            int soulsGained = 0;
            int xpGained = 0;
            int lordLevelUp = 0;
            int lordRewardSouls = 0;
            List<AchievementDef> newlyAchieved = new List<AchievementDef>();
            if (_metaConfig != null)
            {
                MetaProfile profile = MetaSession.GetOrLoad();
                SoulReward reward = SoulRewardCalculator.Calculate(
                    result, _clock.Elapsed, _model.TotalSeconds, GetHeroDamagedRatio(), _metaConfig);

                int levelBefore = LordLevelService.LevelFromXp(profile.LordXp, _metaConfig);
                profile.Souls += reward.Souls;
                profile.LordXp += reward.Xp;
                profile.TotalRuns++;
                if (result == BattleResult.Win)
                {
                    profile.TotalWins++;
                    if (profile.BestClearTime < 0f || _clock.Elapsed < profile.BestClearTime)
                        profile.BestClearTime = _clock.Elapsed;
                }

                //# 도감 — 이번 판 등장 종 + 픽 카드 누적 (distinct).
                foreach (EMonster seen in _seenMonsters)
                    profile.AddDistinct(profile.SeenMonsters, seen.ToString());
                foreach (ECardId pick in _runPicks)
                    profile.AddDistinct(profile.PickedCards, pick.ToString());

                //# 영주 보상 자동 수령 — 멱등 가드 (기획서 §4.4). LordLevel=0 이면 결과 팝업 영주 줄 생략 (§9.2).
                int levelAfter = LordLevelService.LevelFromXp(profile.LordXp, _metaConfig);
                lordRewardSouls = LordLevelService.ClaimRewards(profile, _metaConfig);
                if (levelAfter > levelBefore)
                    lordLevelUp = levelAfter;

                //# 도전과제 — 프로필 누적 갱신 "후" 판정 (기획서 §5.3).
                newlyAchieved = AchievementService.Evaluate(profile, BuildRunSummary(result), _metaConfig);

                soulsGained = reward.Souls;
                xpGained = reward.Xp;
                MetaSession.Store?.Save(profile);
            }

            //# B2 — 트리거 서비스 구독 해제 (BattleClock.OnTick / Health.OnChanged 누수 방지)
            _activeTriggers?.Dispose();
            _passiveTriggers?.Dispose();

            //# 모든 AI 정지
            foreach (AutoCombatAI ai in GetComponentsInChildren<AutoCombatAI>())
                ai.enabled = false;

            _vm.EndBattle(result);

            await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup, new ResultPopupArg
            {
                Result = result,
                HasMeta = _metaConfig != null,
                SoulsGained = soulsGained,
                XpGained = xpGained,
                LordLevel = lordLevelUp,
                LordRewardSouls = lordRewardSouls,
                NewlyAchieved = newlyAchieved,
            });
        }

        //# v0.2 메타 — 영웅 최대 HP 대비 깎은 비율 0~1 (패배 부분 보상 입력).
        private float GetHeroDamagedRatio()
        {
            if (_heroHealth == null || _heroHealth.Max <= 0)
                return 0f;
            return Mathf.Clamp01(1f - (float)_heroHealth.Current / _heroHealth.Max);
        }

        //# v0.2 메타 — 도전과제 판정 입력 (기획서 §5.3). MaxSynergyTier 는 축별 픽 카운트에서 환산.
        private RunSummary BuildRunSummary(BattleResult result)
        {
            RunSummary summary = new RunSummary
            {
                Result = result,
                DeathTime = _clock.Elapsed,
                HeroDamagedRatio = GetHeroDamagedRatio(),
                MaxSynergyTier = GetMaxSynergyTier(),
            };
            foreach (ECardId pick in _runPicks)
                summary.Picks.Add(pick.ToString());
            return summary;
        }

        //# 시너지 임계 3/5/7장 = Tier 1/2/3 (card-renewal §4) — 4축 최고값.
        private int GetMaxSynergyTier()
        {
            if (_synergy == null) return 0;
            int max = 0;
            foreach (EBuildAxis axis in (EBuildAxis[])System.Enum.GetValues(typeof(EBuildAxis)))
            {
                int count = _synergy.GetCount(axis);
                int tier = count >= BuildSynergyService.Tier3Threshold ? 3
                    : count >= BuildSynergyService.Tier2Threshold ? 2
                    : count >= BuildSynergyService.Tier1Threshold ? 1 : 0;
                if (tier > max) max = tier;
            }
            return max;
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
                    IReadOnlyList<CardData> simChoices = deck.Draw(3, id => _pickCounter.IsCapped(id));
                    CardData picked = DebugAutoPicker(simChoices, entry.SourceType);
                    if (picked != null)
                    {
                        _recorder.RecordPick(picked.Id);
                        _runPicks.Add(picked.Id);             //# v0.2 메타 — 도감 픽 기록
                        _pickCounter.RecordPick(picked.Id);   //# 3픽 캡 누적
                        _vm.AddPick(picked, entry.SourceType == TriggerQueue.Source.Passive);
                        //# 스포너 상태 UI — source 추적용 단일 진입점 (기획서 §4.2).
                        ApplyCardEffect(picked);
                    }
                    continue;
                }
#endif

                _pause.Pause();
                IReadOnlyList<CardData> choices = deck.Draw(3, id => _pickCounter.IsCapped(id));
#if UNITY_EDITOR
                //# 디버그 — 지정 카드 강제 등장. 팝업 경로 한정 (시뮬 DebugAutoPicker 경로 비간섭) — 토글 OFF 면 내부 no-op.
                if (_forcedCardDraw != null)
                    choices = _forcedCardDraw.Apply(choices, entry.SourceType == TriggerQueue.Source.Passive,
                        _allCards, id => _pickCounter.IsCapped(id));
#endif
                System.Threading.Tasks.TaskCompletionSource<bool> tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                CardSelectionArg arg = new CardSelectionArg
                {
                    Choices = choices,
                    PickCountOf = c => _pickCounter.GetCount(c.Id),   //# 배지 N/3 — 픽 전 누적값
                    OnPicked = card =>
                    {
                        //# Slice C — 픽 기록
                        if (card != null)
                        {
                            _recorder.RecordPick(card.Id);
                            _runPicks.Add(card.Id);             //# v0.2 메타 — 도감 픽 기록
                            _pickCounter.RecordPick(card.Id);   //# 3픽 캡 누적
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

            //# 지속 스폰 — 동시 캡 제거 + 동시 출력 증가(SpawnX 카드) 대비 → 6종 각 10마리 비축
            foreach (EMonster key in new[] { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper,
                                        EMonster.Hex, EMonster.Plague, EMonster.Phantom })
            {
                GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (prefab != null) CHMPool.Instance.CreatePool(prefab, count: 10);
            }

            //# 시각 이펙트 — PoisonAura(독 장판). 영웅 디버프는 HP바 아래 아이콘으로 표시(월드 visual 제거).
            GameObject poisonFx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.PoisonAura);
            if (poisonFx != null) CHMPool.Instance.CreatePool(poisonFx, count: 2);

            //# TimeStop 실드 FX — 영웅 1명 한정, 카드 중첩 가능성 대비 count 2 (2026-06-05).
            GameObject timeStopShieldFx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.TimeStopShield);
            if (timeStopShieldFx != null) CHMPool.Instance.CreatePool(timeStopShieldFx, count: 2);

            //# Fear 스컬 FX — 적용 순간 1회성, 영웅 1명 한정 count 2 (2026-06-05).
            GameObject fearSkullFx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.FearSkull);
            if (fearSkullFx != null)
            {
                CHMPool.Instance.CreatePool(fearSkullFx, count: 2);
            }

            //# 영웅 스킬 FX (2026-06-04) — 동시 표시 적음. count 4 (궤도 2 + 돌진/노바 순간).
            foreach (EVisual key in new[] { EVisual.HeroDashFx, EVisual.HeroOrbitBladeFx, EVisual.HeroNovaFx })
            {
                GameObject fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (fx != null) CHMPool.Instance.CreatePool(fx, count: 4);
            }

            //# 타격 피드백 FX — 동시 표시 상한 없음 → 스웜 최악 추정 floor 40 (기획서 §6).
            GameObject impact = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.HitImpact);
            if (impact != null) CHMPool.Instance.CreatePool(impact, count: 40);
            //# 영웅이 몬스터를 때릴 때 전용 CFXR 임팩트 — 영웅용과 동일 워밍 floor.
            GameObject monsterImpact = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.MonsterHitImpact);
            if (monsterImpact != null) CHMPool.Instance.CreatePool(monsterImpact, count: 40);
            GameObject popup = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.DamagePopup);
            if (popup != null) CHMPool.Instance.CreatePool(popup, count: 40);

            //# HitFeedbackSpawner 보장 + 프리팹 핸들 주입 (DamageFeedback 가 동기 Pop).
            HitFeedbackSpawner spawner = FindOrCreateHitFeedbackSpawner();
            spawner.Init(impact, monsterImpact, popup);
        }

        //# 씬 진입점 단일 매니저 — 런타임 스폰 풀 대상 아님 (Rule 03 §4 예외: 매니저성 단일 오브젝트).
        private HitFeedbackSpawner FindOrCreateHitFeedbackSpawner()
        {
            if (HitFeedbackSpawner.Instance != null)
                return HitFeedbackSpawner.Instance;
            GameObject go = new GameObject("HitFeedbackSpawner");
            return go.AddComponent<HitFeedbackSpawner>();
        }

        //# B1 — BattleContext.SpawnMonster 가 호출하는 런타임 스폰 (액티브 증식 카드 등).
        //# 동시 캡 제거 (spec §2.A) — 종료 검사만 통과하면 1마리 스폰.
        public async void SpawnMonsterRuntime(Lair.Data.EMonster key, Vector3 nearHero)
        {
            if (_model != null && _model.Result != BattleResult.None) return;
            //# v0.2 메타 — 도감 등장 종 기록 (카드 소환 경로 포함).
            _seenMonsters.Add(key);

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null) return;
            //# await 후 종료 재검사 — await 동안 전투가 끝났으면 스폰 중단.
            if (_model != null && _model.Result != BattleResult.None) return;
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
