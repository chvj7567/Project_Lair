using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using Lair.Village;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        //# 전투 밸런스(BalanceConfig.json·MonsterStat.json 조립 결과) — Start 가 로더 결과를 1회 받아 둔다. 직렬화하지 않는다.
        private BalanceConfig _balance;
        //# 영웅 스킬(HeroSkill·HeroSkill_Param·HeroArchetype_Skill 조립 결과) — Start 가 로더 결과를 1회 받아 둔다. 직렬화하지 않는다.
        private HeroSkillDatabase _heroSkills;

        //# v0.2 메타 — 상점 보너스/보상 정산 수치(MetaConfig.json 외 8종 조립 결과). Start 가 로더 결과를 1회 받아 둔다. 직렬화하지 않는다.
        //# Start 를 거치지 않는 격리 컨트롤러(테스트)에서는 null 이라 메타 로직 전부 skip.
        private MetaConfig _metaConfig;

        //# 전장 배경 스프라이트 렌더러(씬 'Backdrop') — 챕터 backdropKey 가 기본(Battle_Backdrop)이 아니면 시작 시 해당 스프라이트로 교체(chapter-system §7.4).
        [SerializeField] private SpriteRenderer _backdropRenderer;

        //# battle-wide-screen-fit §3 — HUD 는 HudRoot(Safe Area), 팝업은 PopupRoot(1280x720 중앙)에 붙는다. null 이면 CHMUI 기본 루트(기존 동작).
        [SerializeField] private Transform _hudRoot;
        [SerializeField] private Transform _popupRoot;

        //# 지속 스폰 — 종별 누적 스탯 배율 (§3.0.1). 강화 카드가 곱연산 갱신, Pop 시 적용.
        private readonly Dictionary<EMonster, StatMultiplier> _typeModifiers = new();

        //# 둥지 숙련 (monster-growth-currencies §3.3) — 전투 시작 1회 확정, 종족 → 누적 효과. 미로드면 비어 있음.
        private readonly Dictionary<EMonster, NestMasteryEffects> _nestEffects = new();

        //# 종족 특성 (monster-growth-currencies §3.4) — 전투 시작 1회 생성, 특성 단계가 하나라도 있을 때만.
        private MonsterTraitService _traits;

        //# Debuff Tier1 누적 — 역병귀 독 최대 중첩 보너스(기본 3 + 이 값). 판마다 새 컨트롤러라 리셋 불필요.
        private int _toxinMaxStackBonus;
        //# ApplyMonsterStats 를 거친 역병귀 인스턴스 — Tier1 소급 대상(풀 재사용 인스턴스 포함).
        private readonly Dictionary<Transform, Health> _plagueInstances = new();

        //# 종족 강화 레벨 캐시 — ApplyMetaBonuses 에서 1회 채움, 스폰 시 발광 적용에 사용 (monster-species-enhancement §4.3).
        private readonly Dictionary<EMonster, int> _speciesLevels = new();
        //# 이번 판의 강화 티어 전신 시트 로더 — 전투 시작 로딩 구간에서 만들고 OnDestroy 에서 해제(monster-tier-sheets §7).
        private Lair.Character.MonsterTierSheetLoader _tierSheets;

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
        private SynergyVisualConfig _synergyVisual;
        private bool _synergyVisualTried;

        private CHPoolable _hero;
        private Health _heroHealth;
        //# 챕터/스테이지/영웅 직업 데이터 — Start 에서 1회 확보. null 이면 영웅 스탯 데이터 경로 skip.
        private ChapterDatabase _chapters;
        //# 전투 시작에 확정한 난이도(difficulty-select §4.3) — 판 도중 다시 읽지 않는다. 프로필은 수정하지 않는다(읽기 전용 클램프).
        private int _effectiveDifficulty;
        //# 이번 전투 사양 단일 해석 지점 (infinite-tower §8.1) — 시작 시 1회 확정. 배경·제목·규칙·외형·영웅 스탯·유물·기록이 SelectedStage 가 아니라 이것만 읽는다.
        private BattleSpec _spec;
        //# 이번 판 EffectiveFormation (dungeon-formation §6.3) — 시작 시 1회 계산. null 이면 편성 기능 비활성(씬 인스펙터 배치 그대로).
        private EMonster?[] _formation;
        //# 유물(relics §6.1) — 전투 시작에 보유 유물을 1회 집계한 스냅샷. 판 도중 보유는 변하지 않는다. 유물 DB 미로드면 빈 스냅샷.
        private RelicBattleBonus _relicBonus = RelicBattleBonus.None;
        //# 유물 전투 효과(오라·소환·광폭화)는 BattleClock 시작(영웅 입장 완료) 시점에 1회만 개시한다.
        private bool _relicCombatBegan;
        //# 분노의 영혼 — 영웅 HP 구독 핸들러와 사양별 발동 여부.
        private System.Action<int, int> _relicFrenzyHandler;
        private bool[] _relicFrenzyFired = System.Array.Empty<bool>();
        //# 스테이지 규칙(chapter-system §3) — 규칙이 있는 스테이지에서만 러너 생성. 없으면 null(기존 동작 불변).
        private StageRuleRunner _ruleRunner;
        //# 짙은 안개 사거리 규칙 — 비활성이면 ApplyMonsterStats 가 사거리를 그대로 통과시킨다.
        private readonly MonsterRangeModifier _rangeModifier = new MonsterRangeModifier();
        //# 안개 하한용 콜라이더 반경 캐시(E-7) — 영웅은 규칙 시작 시 1회, 몬스터는 종별 첫 적용 시 1회 측정.
        private float _heroColliderRadius;
        private readonly Dictionary<EMonster, float> _monsterColliderRadius = new Dictionary<EMonster, float>();

        //# 상태 아이콘 — ECardId→Sprite 해석 dict. HUD 에 reference 주입 후 카드 풀 로드 시점에 populate.
        //# 인스턴스 1회 생성·재할당 금지 — HUD 가 stale empty reference 를 들지 않도록(주입은 reference).
        private readonly Dictionary<ECardId, Sprite> _cardIconById = new();
        //# 상태 아이콘 — 영웅 HeroAuraRunner 이벤트 구독 핸들러. OnDestroy 에서 정확히 -=.
        private HeroAuraRunner _heroAuraRunner;
        private System.Action<object, ECardId> _onStatusShownHandler;
        private System.Action<object> _onStatusHiddenHandler;
        private System.Action<object, int> _onStatusCountHandler;

        //# 스킬 해금 컷인 — 정지·쉐이크·배너 오케스트레이터 + 배너 View 핸들 + 러너 핸들.
        //# 구독은 _cutscene 생성 후 — SpawnHero(await) 안의 Bind 시점엔 _cutscene 미생성이므로 러너만 캐싱.
        private SkillUnlockCutsceneController _cutscene;
        private SkillUnlockBannerView _bannerView;
        private HeroSkillRunner _skillRunner;

        //# B1 신규
        private PauseService _pause;
        //# 봉인 안내 팝업 문구 id (Strings_Ko).
        private const int SealPopupTitleStringId = 1688;
        private const int SealPopupBodyStringId = 1689;
        private const int SealPopupConfirmStringId = 1690;
        //# 뒤로가기 확인 팝업이 ESC 로 닫힌 프레임 — 같은 프레임 LateUpdate ESC 로 재오픈 방지.
        private int _exitCancelFrame = -1;
        private PassiveTriggerService _passiveTriggers;
        //# 오른쪽 열 패널 접힘 상태 VM — HUD 에 BattleHudArg 로 전달(scene-2d-conversion §4.10).
        private HudLayoutViewModel _hudLayout;
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
            //# 이펙트 동시 상한 카운터 초기화 — 이전 판 잔여 슬롯이 새 판에 새지 않게(MonsterGradeVisual.OnDisable 반납과 이중 안전망).
            Lair.Character.GradeFxBudget.Reset();
            //# 매 Battle 진입(재시작 포함)마다 전투 BGM 을 0초부터 재생 — 프리웜 await 전에 정지해 마을 BGM 잔존 차단.
            CHMSound.Instance.StopAllBGM();
            CHMSound.Instance.Play(EAudio.Bgm);

            //# 챕터 데이터 — Loading 씬이 선행 로드했으면 즉시 반환, Battle 직행이면 여기서 로드. 실패 시 null(영웅 기본 스탯 폴백 로그).
            _chapters = await ChapterDatabaseLoader.EnsureLoadedAsync();
            //# await 재개 시 이미 파괴된 컨트롤러(씬 재로드로 교체)는 아무것도 하지 않는다 — Start 의 모든 await 뒤에 같은 가드.
            if (this == null)
                return;
            //# 밸런스·영웅 스킬·카드·메타 JSON 은 필수 묶음(so-to-json-migration §6.4) — 하나라도 없으면 기본 스탯·스킬 없음·무보상으로 진행하지 않고 마을로 돌아간다.
            _balance = BalanceConfig.Current;
            _heroSkills = HeroSkillDatabase.Current;
            _metaConfig = MetaConfig.Current;
            if (_balance == null || _heroSkills == null || _metaConfig == null || CardDatabase.Current == null)
            {
                Debug.LogError("[BattleController] 필수 데이터 미로드(밸런스·영웅 스킬·카드·메타 JSON) — 전투 진입 중단, 마을로 복귀");
                AbortToVillage();
                return;
            }
            _spec = BattleSpecResolver.Resolve(MetaSession.BattleMode, MetaSession.GetOrLoad(), _chapters, TowerDatabase.Current, MetaSession.DailyContext);
            if (_spec == null)
            {
                //# 탑 데이터 부재 (E-1) · 데일리 컨텍스트 없음(에디터 Battle 직행 등) — 조용한 폴백 없이 전투 진입을 중단하고 마을로.
                Debug.LogError($"[BattleController] {MetaSession.BattleMode} 전투 사양을 만들지 못함(탑/데일리/챕터 데이터 없음) — 전투 진입 중단, 마을로 복귀");
                AbortToVillage();
                return;
            }
            //# 데일리 — 남은 도전이 없으면 시작하지 않는다(결과창 재도전 판정의 안전망, E-15).
            if (_spec.IsDaily && DailyAttemptsLeft() <= 0)
            {
                Debug.LogWarning("[BattleController] 데일리 남은 도전 0 — 전투 진입 중단, 마을로 복귀");
                AbortToVillage();
                return;
            }
            _effectiveDifficulty = _spec.EffectiveDifficulty;
            await ApplyChapterBackdrop();
            if (this == null)
                return;
            _formation = Lair.Character.MonsterLooks.ResolveFormation(MetaSession.GetOrLoad(), _spec.IsDaily);
            await LoadMonsterTierSheets();
            if (this == null)
                return;

            //# 1.5 풀 사전 워밍 (Rule 12) — 첫 Pop spike 방지 + 카드 스폰 부담 감소
            await PrewarmPools();
            if (this == null)
                return;

            //# 2. MVVM
            _model = new BattleStateModel();
            //# Slice C — BalanceConfig 의 런 길이 적용
            _model.TotalSeconds = _balance.RunDuration;
            _vm = new BattleViewModel(_model);
            _vm.BindTriggerThresholds(_balance.ActiveThresholds, _balance.PassiveThresholds);
            _hudLayout = new HudLayoutViewModel(new PlayerPrefsHudLayoutPrefs());
            MetaProfile heroProfile = MetaSession.GetOrLoad();
            _vm.SetHeroTitle(ComposeBattleTitle(heroProfile));

            //# 3. 영웅 스폰 + Spawner 바인딩.
            //#    스포너 상태 UI — VM 의 _spawnerSnapshots 6개가 HUD 표시보다 먼저 채워져야 한다.
            bool heroSpawned = await SpawnHero();
            if (this == null)
                return;
            if (heroSpawned == false)
                return;
            //# 데일리 도전 차감 — 영웅 스폰 직후 즉시 저장(앱 강제 종료로 차감을 피하지 못하게, §10.1·E-6). 뒤로가기 종료도 1회 소모.
            if (_spec.IsDaily)
            {
                MetaProfile dailyProfile = MetaSession.GetOrLoad();
                dailyProfile.UseDailyAttempt(_spec.Daily.DayIndex);
                MetaSession.Store?.Save(dailyProfile);
            }
            //# zone 폴백 분기에서 이미 EnableHeroAIAfterDelay 호출됨. zone 활성 분기는 HandleHeroReachedCenter 가 담당.
            //# 편성 적용은 BindSpawners(종별 base 주기)·SetupStageRules(봉인)·첫 스폰 지연 등 CurrentType 을 읽는 모든 단계보다 먼저(§6.4).
            ApplyFormation();
            BindSpawners();
            //# 스테이지 규칙 — 첫 스포너 Tick 전에 활성해야 봉인이 첫 스폰보다 앞선다(Start 의 이후 await 중 Update 가 돈다).
            if (SetupStageRules() == false)
                return;

            //# v0.2 메타 — 상점 영구 업그레이드를 전투 시작 배율로 적용 (BindSpawners 의 base 주기 주입 뒤, 첫 스폰 전).
            ApplyMetaBonuses();
            //# 유물 — 상점 배율 뒤에 같은 표면(_typeModifiers)에 곱연산 누적. 첫 스폰 지연 계산(이동속도 반영)보다 앞서야 한다(relics §6.1).
            ApplyRelicBonuses();

            //# 스포너가 영웅 쪽으로 당겨진 만큼 첫 스폰만 늦춰 몬스터 도착 시각을 보존(scene-2d-conversion §3.5.4) — 메타 배율 반영 뒤 1회.
            ApplyFirstSpawnDelays();
            //# 서두르는 둥지 — 이동 보정으로 정해진 첫 스폰 지연을 앞당긴다(상기 계산 뒤여야 함).
            ApplyRelicSpawnPrelude();
            //# 둥지 숙련 Lv1 — 서두르는 둥지(SpawnPrelude)와 같은 지점·같은 방식(0 하한, 봉인 해제 처리도 동일).
            ApplyNestFirstSpawnAdvance();

            //# 4. HUD 표시 — 스포너 상태 UI 가 진행 바 폴링에 필요한 Spawners 함께 주입.
            //#    프리팹 로드 실패 시 ShowUIAsync 는 null 을 반환하므로(부트 정지 방지), 명확히 로그하고 부팅은 계속한다.
            if (_hudRoot != null)
            {
                CHMUI.Instance.SetRoot(_hudRoot);
            }
            UIBase hudUI = await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm, HudLayout = _hudLayout, Spawners = _spawners, CardIcons = _cardIconById });
            if (this == null)
                return;
            //# HUD 이후 모든 팝업/배너/토스트는 PopupRoot — await 이후라 다른 UI 가 먼저 뜨지 않는다.
            if (_popupRoot != null)
            {
                CHMUI.Instance.SetRoot(_popupRoot);
            }
            if (hudUI == null)
            {
                Debug.LogError("[BattleController] BattleHud UI 표시 실패(프리팹 로드/캔버스 확보 불가) — HUD 없이 부팅 계속");
            }

            //# B1 — 일시정지 / 트리거 / 카드 풀
            _pause = new PauseService();
            _queue = new TriggerQueue();
            _vm.OnExitRequested += HandleExitRequested;
            _vm.OnSealInfoRequested += HandleSealInfoRequested;
            //# 2배속 — 저장된 토글 상태를 목표 속도로 반영하고 이후 변경을 구독(relics §5).
            _vm.OnBattleSpeedChanged += HandleBattleSpeedChanged;
            ApplyBattleSpeed(_vm.BattleSpeedOn);

            //# 스킬 해금 컷인 — 배너 팝업 1회 표시해 View 핸들 확보 후 구독.
            //# 콜백형으로 부트와 분리 — 배너는 부트 진행을 막을 필요 없는 부수 표시라 fire-and-forget.
            SetupSkillUnlockCutscene();
            if (_heroHealth != null)
            {
                _passiveTriggers = new PassiveTriggerService(_heroHealth, _balance.PassiveThresholds);
                _passiveTriggers.OnTriggered += idx =>
                {
                    _vm?.MarkPassiveTriggered(idx);
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
            //# 시너지 모달이 _vm.GetTier 로 설명을 조립하도록 바인딩된 서비스 주입.
            _vm.BindSynergyService(_synergy);
            _ctx = new BattleContext(this, _synergy);

            //# B3 — 몬스터 글로벌 버프 / 피의 갈증 서비스
            _monsterBuffs = new MonsterBuffService();
            _bloodThirst = new BloodThirstService();
            DespawnOnDeath.MonsterDied += HandleMonsterDied;

            CardPool pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (this == null)
                return;
            if (pool != null)
            {
                //# 데일리는 날짜 시드 덱 — 같은 날 같은 선택이면 같은 카드가 제시된다(D-5). 그 외 시드 0 = 무작위.
                List<CardData> passiveCards = pool.PlayableCards();
                _passiveDeck = new CardDeck(passiveCards, _spec.IsDaily ? _spec.Daily.PassiveSeed : 0);
                _allCards.AddRange(passiveCards);
                //# 상태 아이콘 — 패시브 풀도 스캔(HeroAttackDown=14 가 유일하게 여기 있음. 누락 시 silent-bug).
                PopulateCardIcons(passiveCards);
            }

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            //# 스테이지 규칙 갱신은 BattleClock 틱 — 카드 선택 일시정지 중엔 dt=0 이라 러너가 활성·갱신을 미룬다.
            if (_ruleRunner != null)
            {
                _clock.OnTick += _ruleRunner.Tick;
            }
            //# 폴백 분기(zone 미할당)는 즉시 시작.
            if (_zone == null)
            {
                StartBattleClock();
            }
            //# zone 활성 분기 — race 보강. clock 생성 전 영웅이 이미 입장했으면(L334 _clock?.Start() 가 null no-op)
            //# 여기서 시작. 아직이면 이후 HandleHeroReachedCenter 가 시작 → 두 진입 순서 모두 정확히 1회 Start.
            else if (_heroEntered)
            {
                StartBattleClock();
            }

            //# B2 — 30초 액티브 트리거 (BattleClock.OnTick 구독)
            _activeTriggers = new ActiveTriggerService(_clock, _balance.ActiveThresholds);
            _activeTriggers.OnTriggered += idx =>
            {
                _queue.Enqueue(TriggerQueue.Source.Active, idx);
                TryProcessNext();
            };

            CardPool activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (this == null)
                return;
            if (activePool != null)
            {
                List<CardData> activeCards = activePool.PlayableCards();
                _activeDeck = new CardDeck(activeCards, _spec.IsDaily ? _spec.Daily.ActiveSeed : 0);
                _allCards.AddRange(activeCards);
                //# 상태 아이콘 — 액티브 풀 스캔(8종 중 7종이 여기).
                PopulateCardIcons(activeCards);
            }
        }

        //# 상태 아이콘 — ECardId→Sprite 매핑을 카드 풀에서 채운다(reference 보존 — 재할당 금지).
        //# 인덱서 사용(중복 키 throw 회피) + null icon skip. 패시브/액티브 두 풀 모두에서 호출.
        private void PopulateCardIcons(IReadOnlyList<CardData> cards)
        {
            if (cards == null)
                return;
            foreach (CardData card in cards)
            {
                if (card == null || card.Icon == null)
                    continue;
                _cardIconById[card.Id] = card.Icon;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _clock?.Tick(dt);
            //# B3 — 글로벌 버프/피의 갈증 시간 진행. Pause 중엔 deltaTime=0 이라 자연 정지.
            _monsterBuffs?.Tick(dt);
            //# 특성 시한 배율은 버프 오버레이 리셋 "뒤"에 곱한다 — 리셋 주체(_monsterBuffs)가 생기기 전엔 복리 누적 방지로 건너뛴다.
            if (_monsterBuffs != null)
            {
                _traits?.Tick(dt);
            }
            _bloodThirst?.Tick(dt);

            //# 지속 스폰 — Spawner 들의 주기 타이머 틱. Pause 중 dt=0 자연 정지.
            //# 전투 종료 후엔 스폰 중단. BindSpawners 전(_spawnersActive=false) 엔 무동작.
            if (_spawnersActive && _model != null && _model.Result == BattleResult.None && _spawners != null)
            {
                foreach (Spawner sp in _spawners)
                {
                    if (sp != null)
                    {
                        sp.Tick(dt);
                    }
                }
            }
        }

        //# 안드로이드 뒤로가기 = ESC. CHMUI.Update(ESC→팝업 닫기) 이후에 처리하려고 LateUpdate.
        private void LateUpdate()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) == false)
                return;
            if (_exitCancelFrame == Time.frameCount)
                return;
            _vm?.RequestExit();
#endif
        }

        //# 뒤로가기 — 즉시 정지 + 확인 팝업. 예=저장 없이 마을, 아니오(취소·배경·ESC)=재개.
        private async void HandleExitRequested()
        {
            _pause.Pause();
            _vm.SetExitAvailable(false);
            UIBase popup = await CHMUI.Instance.ShowUIAsync(EUI.ConfirmPopup, new ConfirmPopupArg
            {
                Title = "전투 종료",
                Message = "진행 중인 게임은 저장되지 않습니다.\n전투를 종료하시겠습니까?",
                ConfirmLabel = "예",
                CancelLabel = "아니오",
                OnConfirm = ExitToVillage,
                OnCancel = ResumeFromExit,
            });
            if (popup == null)
            {
                ResumeFromExit();
            }
        }

        //# 봉인 셀 탭 — 정지 + 안내 팝업(1버튼). 닫힘은 전투 종료 확인과 같은 ResumeFromExit 로 수렴(E-1).
        private async void HandleSealInfoRequested(int index)
        {
            //# 카드 선택·컷인·종료 확인·다른 안내 팝업 중이면 무시(E-3·E-4), 이미 해제된 셀도 무시(E-5).
            if (_vm.IsExitAvailable == false || _pause.IsPaused)
                return;
            if (_spawners == null || index < 0 || index >= _spawners.Length || CHText.StringProvider == null)
                return;
            Spawner nest = _spawners[index];
            if (nest == null || nest.IsLocked == false)
                return;

            //# 해제 % 를 모르는 봉인(규칙 없음)은 셀 버튼도 꺼져 있다 — 같은 조건으로 무시.
            int unlockPercent = SealUnlockHpPercentOf(nest.CurrentType);
            if (unlockPercent <= 0)
                return;

            float heroRatio = _heroHealth != null ? _heroHealth.Ratio : 1f;
            string body = SealedNestInfo.FormatNestPopupBody(
                CHText.StringProvider.GetString(SealPopupBodyStringId),
                SpeciesVisual.SpeciesName(nest.CurrentType),
                unlockPercent,
                heroRatio);

            _pause.Pause();
            _vm.SetExitAvailable(false);
            UIBase popup = await CHMUI.Instance.ShowUIAsync(EUI.ConfirmPopup, new ConfirmPopupArg
            {
                Title = CHText.StringProvider.GetString(SealPopupTitleStringId),
                Message = body,
                ConfirmLabel = CHText.StringProvider.GetString(SealPopupConfirmStringId),
                CancelLabel = null,
                OnConfirm = ResumeFromExit,
                OnCancel = ResumeFromExit,
            });
            if (popup == null)
            {
                ResumeFromExit();
            }
        }

        //# 종 둥지의 해제 HP% — 그 종을 봉인한 규칙 우선, 없으면 스냅샷과 같은 아무 봉인 규칙 값.
        private int SealUnlockHpPercentOf(EMonster species)
        {
            if (_chapters == null || _spec == null)
                return 0;
            foreach (StageRuleData data in _spec.ResolveRules(_chapters))
            {
                int percent = SealedNestInfo.UnlockHpPercent(data, species);
                if (percent > 0)
                    return percent;
            }
            return GetSpawnerSealUnlockHpPercent();
        }

        private void ResumeFromExit()
        {
            _exitCancelFrame = Time.frameCount;
            _pause.Resume();
            _vm.SetExitAvailable(true);
        }

        //# 전투 사양 해석 실패 — 모드·데일리 컨텍스트를 챕터 기본값으로 돌리고 마을로.
        private static void AbortToVillage()
        {
            MetaSession.BattleMode = EBattleMode.Chapter;
            MetaSession.DailyContext = null;
            SceneManager.LoadScene(EScene.Village.ToString());
        }

        //# 이번 데일리 날짜의 남은 도전 — 데일리 데이터가 없으면 0.
        private int DailyAttemptsLeft()
        {
            DailyDatabase db = DailyDatabase.Current;
            if (db == null || _spec == null || _spec.IsDaily == false)
                return 0;
            return MetaSession.GetOrLoad().DailyAttemptsLeft(_spec.Daily.DayIndex, db.Config.attemptsPerDay);
        }

        //# 런 기록·세이브·랭킹 제출(EndBattle 경로)을 거치지 않고 바로 이동.
        private void ExitToVillage()
        {
            _pause.ResetAll();
            SceneManager.LoadScene(EScene.Village.ToString());
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
            {
                _cutscene.Enqueue(data.DisplayName);
            }
        }

        //# 정적 이벤트 구독 해제 — 씬 재시작 시 누수 방지.
        //# 스포너 상태 UI — VM 이 Spawner / 본 컨트롤러 이벤트를 구독했으므로 함께 해제.
        private void OnDestroy()
        {
            //# 씬 전환 직전 — 이번 판에 로드한 티어 시트 번들 해제(monster-tier-sheets §7.5). 풀 인스턴스의 몸 스프라이트는 Animator 가 매 프레임 풋내기로 덮어쓰고
            //# 세트 참조는 Pop(OnEnable) 에서 비우므로 풀 정리 순서와 무관하게 안전하다.
            _tierSheets?.Release();
            _tierSheets = null;
            if (_vm != null)
            {
                _vm.OnSealInfoRequested -= HandleSealInfoRequested;
            }
            Lair.Character.GradeFxBudget.Reset();
            DespawnOnDeath.MonsterDied -= HandleMonsterDied;
            _traits?.Dispose();
            _traits = null;
            if (_zone != null)
            {
                _zone.OnHeroReachedCenter -= HandleHeroReachedCenter;
            }
            if (_skillRunner != null)
            {
                _skillRunner.OnSkillUnlocked -= HandleSkillUnlocked;
            }
            _ruleRunner?.Shutdown();
            _vm?.DetachSpawners();
            TeardownHeroAuraEvents();
            DetachRelicFrenzy();
            if (_vm != null)
            {
                _vm.OnBattleSpeedChanged -= HandleBattleSpeedChanged;
            }
            //# 씬 이탈 시 배속이 다음 씬으로 새지 않게 — ExitToVillage 외 경로(결과창 버튼·재시작)의 안전망.
            _pause?.ResetAll();
        }

        //# BattleClock 시작 + 유물 전투 효과 개시 — 시계가 실제로 시작되는 세 경로가 모두 이 메서드를 거친다.
        private void StartBattleClock()
        {
            if (_clock == null)
                return;
            _clock.Start();
            BeginRelicCombat();
        }

        //# 스테이지 규칙 러너 구성·시작 — Stage_Rule 이 없으면(1-1 등) 아무것도 만들지 않는다. 실패 시 false(전투 진입 중단).
        private bool SetupStageRules()
        {
            if (_chapters == null || _heroHealth == null || _hero == null)
                return true;

            IReadOnlyList<StageRuleData> rules = _spec.ResolveRules(_chapters);
            if (rules.Count == 0)
                return true;

            _heroColliderRadius = ColliderRadiusOf(_hero.gameObject);
            try
            {
                StageRuleContext ctx = new StageRuleContext(
                    _heroHealth, _hero.transform, _spawners, _zone, _rangeModifier, ReapplyAllMonsterStats,
                    text => ToastView.Show(text, EToastKind.Warning), CHText.StringProvider);
                _ruleRunner = new StageRuleRunner(ctx, _heroHealth, rules);
                _ruleRunner.Start();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleController] 스테이지 규칙 구성 실패 — 전투 진입 중단: {e}");
                _ruleRunner = null;
                return false;
            }
            return true;
        }

        //# 영웅 + 몬스터 콜라이더 반경 합 — 안개 사거리 하한(E-7). 몬스터 반경은 종별 캐시.
        private float CollisionDistanceOf(GameObject monster, EMonster key)
        {
            if (_monsterColliderRadius.TryGetValue(key, out float radius) == false)
            {
                radius = ColliderRadiusOf(monster);
                _monsterColliderRadius[key] = radius;
            }
            return _heroColliderRadius + radius;
        }

        //# 프리팹 CapsuleCollider 반경 x 월드 스케일(XZ 최대). 콜라이더가 없으면 LogError 후 0 — 하한이 minRange 로만 걸린다.
        private static float ColliderRadiusOf(GameObject go)
        {
            CapsuleCollider capsule = go.GetComponentInChildren<CapsuleCollider>();
            if (capsule == null)
            {
                Debug.LogError($"[BattleController] {go.name} 에 CapsuleCollider 가 없어 안개 사거리 하한 충돌 거리를 구하지 못함");
                return 0f;
            }
            Vector3 scale = capsule.transform.lossyScale;
            return capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        //# 스포너 상태 UI 봉인 표기용 — 스테이지에 봉인된 둥지 규칙이 있으면 해제 HP%(정수), 없으면 0. 종과 무관 — 셀이 실제 봉인 여부(IsLocked)와 함께 판정한다(대체 규칙, dungeon-formation §5.1).
        public int GetSpawnerSealUnlockHpPercent()
        {
            if (_chapters == null || _spec == null)
                return 0;
            foreach (StageRuleData data in _spec.ResolveRules(_chapters))
            {
                int percent = SealedNestInfo.AnyUnlockHpPercent(data);
                if (percent > 0)
                    return percent;
            }
            return 0;
        }

        //# 사거리 규칙 변경 시 필드 몬스터 전원에 소급 — RegisterMonsterTypeBuff 와 같은 resetCurrent:false(현재 HP 보존).
        private void ReapplyAllMonsterStats()
        {
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || e.Health.IsAlive == false || e.Transform == null)
                    continue;
                MonsterTag tag = e.Transform.GetComponent<MonsterTag>();
                if (tag == null)
                    continue;
                ApplyMonsterStats(e.Transform.gameObject, tag.Key, resetCurrent: false);
            }
        }

        //# 상태 아이콘 — 영웅 HeroAuraRunner 이벤트를 VM 으로 forward 구독.
        //# 영웅은 풀 count-1 → 씬 재진입 시 같은 인스턴스 재사용. 재구독 전 기존 구독 정리(중복 방지).
        private void SetupHeroAuraEvents(GameObject heroGo)
        {
            if (heroGo == null)
                return;
            TeardownHeroAuraEvents();

            HeroAuraRunner runner = heroGo.GetComponent<HeroAuraRunner>();
            if (runner == null)
            {
                runner = heroGo.AddComponent<HeroAuraRunner>();
            }
            _heroAuraRunner = runner;

            _onStatusShownHandler  = (key, id) => _vm?.AddStatusIcon(key, id);
            _onStatusHiddenHandler = key => _vm?.RemoveStatusIcon(key);
            _onStatusCountHandler  = (key, count) => _vm?.SetStatusIconCount(key, count);
            runner.OnStatusShown  += _onStatusShownHandler;
            runner.OnStatusHidden += _onStatusHiddenHandler;
            runner.OnStatusCountChanged += _onStatusCountHandler;
        }

        //# 상태 아이콘 — 구독 해제(저장 핸들러로 정확히 -=). 재구독·OnDestroy 양쪽에서 호출.
        private void TeardownHeroAuraEvents()
        {
            if (_heroAuraRunner != null)
            {
                if (_onStatusShownHandler != null)
                {
                    _heroAuraRunner.OnStatusShown -= _onStatusShownHandler;
                }
                if (_onStatusHiddenHandler != null)
                {
                    _heroAuraRunner.OnStatusHidden -= _onStatusHiddenHandler;
                }
                if (_onStatusCountHandler != null)
                {
                    _heroAuraRunner.OnStatusCountChanged -= _onStatusCountHandler;
                }
            }
            _heroAuraRunner = null;
            _onStatusShownHandler = null;
            _onStatusHiddenHandler = null;
            _onStatusCountHandler = null;
        }

        //# 챕터 전장 배경 교체 — 기본 키(Battle_Backdrop = 씬에 배치된 1챕터 배경)는 건드리지 않아 기존 동작 불변.
        //# 키 파싱/에셋 로드 실패는 조용히 넘기지 않고 LogError(기획서 E-11 취지) — 배경은 시각 요소라 전투는 씬 기본 배경으로 계속한다.
        private async Task ApplyChapterBackdrop()
        {
            if (_chapters == null || _backdropRenderer == null)
                return;

            ChapterRow chapter = _chapters.GetChapter(_spec.BackdropChapterId);
            if (chapter == null)
            {
                Debug.LogError($"[BattleController] 챕터 {_spec.BackdropChapterId} 의 챕터 데이터 없음 — 기본 배경으로 진행");
                return;
            }
            if (System.Enum.TryParse(chapter.backdropKey, out EBackdrop key) == false || System.Enum.IsDefined(typeof(EBackdrop), key) == false)
            {
                Debug.LogError($"[BattleController] 챕터 {chapter.chapterId} 의 backdropKey 가 EBackdrop 에 없음: {chapter.backdropKey}");
                return;
            }
            if (key == EBackdrop.Battle_Backdrop)
                return;

            Sprite sprite = await CHMResource.Instance.LoadAsync<Sprite>(key);
            if (this == null)
                return;
            if (sprite == null)
            {
                Debug.LogError($"[BattleController] 전장 배경 로드 실패: {key} — Addressables 등록(Art/Sprites/Backdrop2D, 라벨 Resource) 확인, 기본 배경으로 진행");
                return;
            }
            _backdropRenderer.sprite = sprite;
        }

        //# chapter-system §2.1 — 앵커(BalanceConfig) x 직업(HeroArchetype) x 스테이지(Stage) 로 도출한 영웅 스탯을 적용. Pop 직후 1회.
        //# 풀 재사용 복리 없음 — 매 스폰마다 앵커에서 재계산한 절대값으로 덮는다.
        private void ApplyHeroStats(GameObject character, HeroStats stats)
        {
            if (character == null)
                return;
            Health health = character.GetComponent<Health>();
            if (health != null)
            {
                health.SetMax(stats.Hp, resetCurrent: true);
            }
            MeleeAttacker attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null)
            {
                attacker.Configure(stats.Range, stats.Cooldown, stats.Power);
            }
            SimpleMover mover = character.GetComponent<SimpleMover>();
            if (mover != null)
            {
                mover.Speed = stats.MoveSpeed;
            }
        }

        //# hero-stage-variant — 선택 스테이지의 외형 variant 적용(스탯 배수는 ApplyHeroStats 가 Stage.json 값으로 처리).
        //# Pop 시점 GetComponent 는 기존 SpawnHero 관례와 동일한 일회성 경로(Rule 02 §5).
        private void ApplyStageVariant(GameObject hero)
        {
            if (hero == null || _chapters == null)
                return;

            StageRow stage = _chapters.GetStageClamped(_spec.LinearStage);
            if (stage == null)
                return;

            HeroStageVariantApplier applier = hero.GetComponent<HeroStageVariantApplier>();
            if (applier != null)
            {
                applier.Apply(stage.heroScale, stage.heroTier);
            }
        }

        //# 지속 스폰 — 몬스터에 raw 스탯 × 글로벌 타입 모디파이어 배율 적용 (§7.5.2).
        //# 모든 몬스터 스폰·소급 경로가 이 한 메서드를 거친다. 영웅은 절대 거치지 않는다.
        public void ApplyMonsterStats(GameObject character, EMonster key, bool resetCurrent)
            => ApplyMonsterStatsCore(character, key, resetCurrent);

        //# 반환 = 그 몬스터의 Health (스폰 경로가 스폰 보호를 거는 데 재사용 — GetComponent 중복 회피). 적용 불가면 null.
        //# key = 그 개체의 EMonster(등급 몬스터면 <기준>G<n>). 스탯·배율·육성·카드는 기준 몬스터 키, HP·공격력만 개체 등급 배율을 곱한다(monster-grades §6).
        private Health ApplyMonsterStatsCore(GameObject character, EMonster key, bool resetCurrent)
        {
            if (character == null)
                return null;
            EMonster species = MonsterGrades.BaseOf(key);
            BalanceConfig.CharacterStat raw = _balance?.GetMonster(species);
            if (raw == null)
                return null;
            StatMultiplier mul = _typeModifiers.TryGetValue(species, out StatMultiplier m) ? m : StatMultiplier.Identity;
            FormationDatabase formation = FormationDatabase.Current;
            float gradeHp = formation != null ? formation.HpMulOf(key) : 1f;
            float gradeAtk = formation != null ? formation.AtkMulOf(key) : 1f;

            Health health = character.GetComponent<Health>();
            if (health != null)
            {
                health.SetMax(Mathf.Max(1, Mathf.RoundToInt(raw.Hp * mul.HpMul * gradeHp)), resetCurrent);
                //# 각성 받는 피해·넉백 — DamageTakenScale 을 덮지 않는 별도 배율 (monster-growth-currencies §3.2).
                health.ConfigureGrowthTaken(mul.DamageTakenMul, mul.KnockbackTakenMul);
            }

            MeleeAttacker attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null)
            {
                //# 사거리 = raw x 카드 배율, 그 뒤 마지막으로 스테이지 규칙(짙은 안개) — 규칙 비활성이면 값 불변.
                float range = raw.Range * mul.RangeMul;
                if (_rangeModifier.IsActive)
                {
                    range = _rangeModifier.Apply(range, CollisionDistanceOf(character, key));
                }
                attacker.Configure(
                    range,
                    Mathf.Max(0.05f, raw.Cooldown * mul.CooldownMul),
                    Mathf.Max(1, Mathf.RoundToInt(raw.Power * mul.PowerMul * gradeAtk)));
            }

            SimpleMover mover = character.GetComponent<SimpleMover>();
            if (mover != null)
            {
                mover.Speed = raw.MoveSpeed * mul.MoveSpeedMul;
            }

            //# 역병귀 한정 — 독 피해 배율·최대 중첩 보너스를 불변 baseline 위에 절대 대입. 복리 누적 없음 (monster-growth-currencies §15.1).
            PlagueToxinOnHit toxin = character.GetComponent<PlagueToxinOnHit>();
            if (toxin != null)
            {
                toxin.Configure(mul.PoisonDamageMul, mul.PoisonDurationMul, _toxinMaxStackBonus);
                toxin.BindHero(_heroHealth, _heroAuraRunner);
                //# Debuff Tier1 소급 대상 — 풀 인스턴스는 씬 동안 유지되므로 1회 등록이면 충분.
                _plagueInstances[character.transform] = health;
            }

            //# 등급 연출 → 강화 시각(티어·발광·HP바) — 마을·로딩 쇼케이스와 같은 공유 함수(순서: sizeMul 확정 후 HP바 합성).
            int level = _speciesLevels.TryGetValue(species, out int lv) ? lv : 0;
            Lair.Character.MonsterLookApplier.Apply(character, key, species, level, resetCurrent, _tierSheets);

            //# 특성 훅 — 스폰(resetCurrent) 이면 잔상 회피도 충전한다. 특성은 기준 몬스터 단위.
            _traits?.OnMonsterConfigured(character, species, resetCurrent);
            return health;
        }

        //# 영웅 스폰 — 챕터 데이터/직업/스테이지 조회나 프리팹 로드에 실패하면 false(전투 진입 중단). 조용한 폴백 금지(chapter-system E-11).
        private async Task<bool> SpawnHero()
        {
            //# 영웅 직업 — 전투 사양이 해석한 값 (챕터: 선택 스테이지 챕터의 영웅, 탑: 층 영웅). chapter-system §7.4.
            HeroArchetypeRow archetype = _spec.Hero;
            if (_spec.CanComputeHeroStats == false || System.Enum.TryParse(archetype.heroId, out EHero heroKey) == false
                || System.Enum.IsDefined(typeof(EHero), heroKey) == false)
            {
                Debug.LogError($"[BattleController] 챕터/탑 데이터에서 영웅·스탯 사양({_spec.Mode}, 스테이지 {_spec.LinearStage})을 찾지 못함 — 전투 진입 중단");
                return false;
            }

            //# Kiter 영웅은 카이팅 설정 row 가 필수 — 없으면 기본값으로 진행하지 않고 중단(E-11 취지).
            HeroArchetype_KiteRow kiteRow = null;
            if (archetype.aiType == EHeroAi.Kiter.ToString())
            {
                kiteRow = _chapters.GetKite(archetype.heroId);
                if (kiteRow == null)
                {
                    Debug.LogError($"[BattleController] {archetype.heroId} 카이팅 설정(HeroArchetype_Kite) 없음 — 전투 진입 중단");
                    return false;
                }
            }

            //# Skirmisher 영웅도 습격 설정 row 가 필수 — 없으면 기본값으로 진행하지 않고 중단.
            HeroArchetype_SkirmishRow skirmishRow = null;
            if (archetype.aiType == EHeroAi.Skirmisher.ToString())
            {
                skirmishRow = _chapters.GetSkirmish(archetype.heroId);
                if (skirmishRow == null)
                {
                    Debug.LogError($"[BattleController] {archetype.heroId} 습격 설정(HeroArchetype_Skirmish) 없음 — 전투 진입 중단");
                    return false;
                }
            }

            //# Charger 영웅도 군집 돌진 설정 row 가 필수 — 없으면 기본값으로 진행하지 않고 중단.
            HeroArchetype_ChargeRow chargeRow = null;
            if (archetype.aiType == EHeroAi.Charger.ToString())
            {
                chargeRow = _chapters.GetCharge(archetype.heroId);
                if (chargeRow == null)
                {
                    Debug.LogError($"[BattleController] {archetype.heroId} 군집 돌진 설정(HeroArchetype_Charge) 없음 — 전투 진입 중단");
                    return false;
                }
            }
            //# Guardian 영웅도 응징 표적 설정 row 가 필수 — 없으면 기본값으로 진행하지 않고 중단.
            HeroArchetype_GuardRow guardRow = null;
            if (archetype.aiType == EHeroAi.Guardian.ToString())
            {
                guardRow = _chapters.GetGuard(archetype.heroId);
                if (guardRow == null)
                {
                    Debug.LogError($"[BattleController] {archetype.heroId} 응징 표적 설정(HeroArchetype_Guard) 없음 — 전투 진입 중단");
                    return false;
                }
            }
            //# 디버프 저항 패시브는 선택 — row 가 있는 영웅만(없으면 부착 경로 불변).
            HeroArchetype_DebuffResistRow resistRow = _chapters.GetDebuffResist(archetype.heroId);
            //# 범위 베기 설정은 선택 — row 가 있는 영웅만 기본 공격이 범위 판정(없으면 단일 판정 그대로).
            HeroArchetype_CleaveRow cleaveRow = _chapters.GetCleave(archetype.heroId);

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(heroKey);
            if (this == null)
                return false;
            if (prefab == null)
            {
                Debug.LogError($"[BattleController] {heroKey} 프리팹 로드 실패 — 전투 진입 중단");
                return false;
            }

            //# Kiter 프리팹은 AutoCombatAI 가 필수 — Pop 전에 검증해 중단 시 풀 인스턴스가 새지 않게 한다.
            if ((kiteRow != null || skirmishRow != null || chargeRow != null || guardRow != null) && prefab.GetComponent<AutoCombatAI>() == null)
            {
                Debug.LogError($"[BattleController] {heroKey} 프리팹에 AutoCombatAI 없음 — AI 설정 불가, 전투 진입 중단");
                return false;
            }

            CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null)
                return false;
            //# 우선순위 — BattleZone.HeroEntryPoint > _heroSpawn > Vector3.zero.
            Vector3 spawnPos;
            if (_zone != null && _zone.HeroEntryPoint != null)
            {
                spawnPos = _zone.HeroEntryPoint.position;
            }
            else if (_heroSpawn != null)
            {
                spawnPos = _heroSpawn.position;
            }
            else
            {
                spawnPos = Vector3.zero;
            }
            p.transform.position = spawnPos;

            _hero = p;
            _heroHealth = p.GetComponent<Health>();

            //# 상태 아이콘 — HeroAuraRunner 를 셋업 시점에 확보(BattleContext/PlagueToxinOnHit 가 같은 인스턴스 사용).
            //# 이벤트를 VM 으로 forward. 핸들러는 필드 보관 → OnDestroy 에서 정확히 -=.
            SetupHeroAuraEvents(p.gameObject);

            //# Slice C — 영웅 스탯 적용 (이후 UpdateHeroHp 가 올바른 값 반영)
            if (_balance != null && _balance.Hero != null)
            {
                ApplyHeroStats(p.gameObject, _spec.ComputeHeroStats(_balance.Hero.Hp, _balance.Hero.Power));
                //# hero-stage-variant — 스탯 직후 스테이지 외형 반영.
                ApplyStageVariant(p.gameObject);
            }
            //# 카이팅(Kiter) 영웅 AI 설정 주입 — 기사(Melee)는 호출하지 않아 AutoCombatAI 의 기존 경로 그대로.
            if (kiteRow != null)
            {
                //# AutoCombatAI 존재는 Pop 전에 검증됨.
                p.GetComponent<AutoCombatAI>().ConfigureKite(KiteSettings.From(kiteRow));
            }
            //# 습격-이탈(Skirmisher) 영웅 AI 설정 주입 — 기사·궁수는 호출하지 않아 AutoCombatAI 의 기존 경로 그대로.
            if (skirmishRow != null)
            {
                p.GetComponent<AutoCombatAI>().ConfigureSkirmish(SkirmishSettings.From(skirmishRow));
            }
            //# 군집 돌진(Charger) 영웅 AI 설정 주입 — 그 외 영웅은 호출하지 않아 AutoCombatAI 의 기존 경로 그대로.
            if (chargeRow != null)
            {
                p.GetComponent<AutoCombatAI>().ConfigureCharge(ChargeSettings.From(chargeRow));
            }
            //# 응징 표적(Guardian) 영웅 AI 설정 + 디버프 가해 통지원 주입 — 그 외 영웅은 호출하지 않아 AutoCombatAI 의 기존 경로 그대로.
            if (guardRow != null)
            {
                p.GetComponent<AutoCombatAI>().ConfigureGuard(GuardSettings.From(guardRow), _heroAuraRunner);
            }
            //# 신앙의 가호 — 풀 재사용 영웅이라 row 가 없으면 이전 설정을 반드시 해제한다.
            if (_heroAuraRunner != null)
            {
                if (resistRow != null)
                {
                    _heroAuraRunner.ConfigureDebuffResist(DebuffResistSettings.From(resistRow));
                }
                else
                {
                    _heroAuraRunner.ClearDebuffResist();
                }
            }
            //# 범위 베기 — row 가 있는 영웅만 MeleeAttacker 에 주입(풀 재사용이라 스폰마다).
            if (cleaveRow != null)
            {
                MeleeAttacker heroAttacker = p.GetComponent<MeleeAttacker>();
                if (heroAttacker != null)
                {
                    heroAttacker.ConfigureCleave(CleaveSettings.From(cleaveRow), new RegistryCleaveTargets());
                }
                else
                {
                    Debug.LogError($"[BattleController] {heroKey} 프리팹에 MeleeAttacker 없음 — 범위 베기 비활성");
                }
            }
            if (_heroHealth != null)
            {
                _heroHealth.OnChanged += _vm.UpdateHeroHp;
                _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
                _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
            }

            //# 영웅 스킬 — HeroArchetype_Skill 구간을 스킬 인스턴스로 풀어 러너에 주입(런타임 Bind, 풀 reference 안전).
            //# 컷인 구독은 여기서 하지 않는다 — _cutscene 은 SpawnHero await 종료 후(Start L115 이후) 생성됨.
            //# 러너만 필드에 캐싱하고, 컨트롤러 생성 직후 Start 에서 구독한다(이중구독 가드 포함).
            HeroSkillRunner skillRunner = p.GetComponent<HeroSkillRunner>();
            if (skillRunner != null)
            {
                _skillRunner = skillRunner;
                IReadOnlyList<HeroSkillPhaseEntry> entries = SkillPhasesOf(_heroSkills, heroKey);
                List<HeroSkillPhase> phases = new List<HeroSkillPhase>(entries.Count);
                foreach (HeroSkillPhaseEntry entry in entries)
                {
                    phases.Add(new HeroSkillPhase(entry.HpFraction, HeroSkillData.Create(_heroSkills.GetSkill(entry.Skill))));
                }
                skillRunner.Bind(phases);
            }
            else
            {
                Debug.LogError($"[BattleController] {heroKey} 프리팹에 HeroSkillRunner 없음 — 영웅 스킬 비활성");
            }

            //# 영웅 AutoCombatAI 비활성 — HeroEntryDriver 가 march 를 수행.
            foreach (AutoCombatAI ai in p.GetComponentsInChildren<AutoCombatAI>())
            {
                if (ai != null)
                {
                    ai.enabled = false;
                }
            }

            //# BattleZone 이 있을 때만 HeroEntryDriver 동작. 없으면 기존 EnableHeroAIAfterDelay 폴백.
            if (_zone != null)
            {
                //# 풀 Pop 직후 SimpleMover._clampZone 런타임 주입 — 씬 인스펙터 와이어링은 풀 reference broken 이라 사용 불가 (game-designer §12.B).
                SimpleMover heroMover = p.GetComponent<SimpleMover>();
                if (heroMover != null)
                {
                    heroMover.BindClampZone(_zone);
                }

                _heroEntryDriver = p.GetComponent<HeroEntryDriver>();
                if (_heroEntryDriver == null)
                {
                    _heroEntryDriver = p.gameObject.AddComponent<HeroEntryDriver>();
                }
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
            return true;
        }

        //# 영웅의 스킬 구간 — HeroArchetype_Skill 의 heroId 행(chapter-system §1.4). 행이 없는 영웅은 기사 구성으로 폴백한다(영웅 추가 시 그 JSON 에 행 추가).
        private static IReadOnlyList<HeroSkillPhaseEntry> SkillPhasesOf(HeroSkillDatabase skills, EHero hero)
        {
            IReadOnlyList<HeroSkillPhaseEntry> phases = skills.GetPhases(hero.ToString());
            if (phases.Count > 0)
                return phases;
            Debug.LogError($"[BattleController] {hero} 스킬 구간 없음 — 기사 구성으로 폴백(HeroArchetype_Skill.json 에 등록 필요)");
            return skills.GetPhases(EHero.Knight.ToString());
        }

        //# BattleZone.OnHeroReachedCenter 핸들러 — 영웅이 zone 중심 도달 시 clock 시작 + 영웅 AI 활성화.
        //# 1회 동작 보장 — _heroEntered 가 false 일 때만 진입. 스폰 개시(_spawnersActive)는 BindSpawners 가 담당.
        private void HandleHeroReachedCenter()
        {
            if (_heroEntered)
                return;
            _heroEntered = true;

            //# 영웅 AutoCombatAI 활성화 — MonsterTargetProvider 가 zone 안 Engaging 몬스터 검색.
            if (_hero != null)
            {
                foreach (AutoCombatAI ai in _hero.GetComponentsInChildren<AutoCombatAI>())
                {
                    if (ai != null)
                    {
                        ai.enabled = true;
                    }
                }
            }

            //# HeroEntryDriver 1회 비활성화 보강 — driver 자체도 enabled=false 호출하지만 안전망.
            if (_heroEntryDriver != null)
            {
                _heroEntryDriver.enabled = false;
            }

            //# BattleClock 시작 — 5분 카운트다운 개시.
            StartBattleClock();
        }

        //# 영웅 AutoCombatAI 를 delay 초 후 활성화. Start() 백그라운드 호출용.
        private async Task EnableHeroAIAfterDelay(float delay)
        {
            await Task.Delay((int)(delay * 1000));
            if (_hero == null)
                return;
            foreach (Lair.Character.AutoCombatAI ai in _hero.GetComponentsInChildren<Lair.Character.AutoCombatAI>())
            {
                if (ai != null)
                {
                    ai.enabled = true;
                }
            }
        }

        //# 종족 강화 레벨 캐시 — 스폰 시 발광·티어 시트 적용용 단일 출처. Id 문자열 파싱 없이 config 항목 순회로 Species→레벨 매핑.
        private void CacheSpeciesLevels(MetaProfile profile)
        {
            _speciesLevels.Clear();
            foreach (KeyValuePair<EMonster, int> pair in Lair.Character.MonsterLooks.SpeciesLevels(profile, _metaConfig))
            {
                _speciesLevels[pair.Key] = pair.Value;
            }
        }

        //# 편성 6둥지의 기준 몬스터 x 상점 강화 Lv → 티어(>0)인 종족만 병렬 로드. 모든 전투 모드 공통(챕터·탑·데일리), 실패는 풋내기 폴백.
        private async Task LoadMonsterTierSheets()
        {
            _tierSheets = new Lair.Character.MonsterTierSheetLoader(
                key => CHMResource.Instance.LoadAsync<Lair.Character.MonsterTierSheetSet>(key),
                key => CHMResource.Instance.Unload(key),
                Debug.LogWarning);
            if (_metaConfig == null)
                return;

            CacheSpeciesLevels(MetaSession.GetOrLoad());
            Lair.Character.MonsterLook[] looks = Lair.Character.MonsterLooks.ComputeSlots(_formation, _speciesLevels);
            List<(EMonster, int)> needs = Lair.Character.MonsterLooks.TierNeeds(looks);
            await _tierSheets.LoadAsync(needs);
        }

        //# 슬롯 i 의 스포너 출력 종 = 편성[i] (§6.4). 기존 Spawner.ReplaceOutput 재사용 — 제단 틴트는 OnOutputTypeChanged 로 갱신.
        //# 스포너 수가 슬롯 수와 다르면 슬롯 번호 의미가 깨지므로 적용하지 않고 씬 배치를 쓴다. 종 불일치는 경고만(ReplaceOutput 은 절대 대입).
        private void ApplyFormation()
        {
            if (_formation == null || _spawners == null)
                return;
            EMonster[] defaults = FormationDatabase.Current.DefaultFormation(null);
            if (_spawners.Length != defaults.Length)
            {
                Debug.LogError($"[BattleController] 스포너 수({_spawners.Length}) 가 FormationSlot 수({defaults.Length}) 와 다름 — 편성 미적용");
                return;
            }
            for (int i = 0; i < _spawners.Length; ++i)
            {
                if (_spawners[i] == null)
                {
                    Debug.LogError($"[BattleController] 씬 스포너 {i + 1} 이 비어 있음 — 편성 미적용");
                    return;
                }
                if (MonsterGrades.SameSpecies(_spawners[i].CurrentType, defaults[i]) == false)
                {
                    Debug.LogWarning($"[BattleController] 씬 스포너 {i + 1} 의 출력 종족이 FormationSlot({defaults[i]})과 다름 — 편성은 절대 대입이라 적용 계속");
                }
            }
            for (int i = 0; i < _spawners.Length; ++i)
            {
                EMonster? output = _formation[i];
                if (output.HasValue == false)
                {
                    _spawners[i].ClearOutput();
                }
                else if (_spawners[i].HasOutput == false || _spawners[i].CurrentType != output.Value)
                {
                    _spawners[i].ReplaceOutput(output.Value);
                }
            }
        }

        //# 지속 스폰 — 씬의 Spawner 들에 호스트 주입. 이후 Update 가 각자 주기 틱.
        //# 스포너 상태 UI — Spawner 6개 + 본 컨트롤러를 VM 에 묶어 SpawnerSnapshot 통합 노출.
        private void BindSpawners()
        {
            if (_spawners == null)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp == null)
                    continue;
                sp.Bind(this, _zone);
                //# BalanceConfig 종별 base 주기 주입 — 카드 ScalePeriod(곱연산) 보다 시간상 먼저 (기획서 §3.3).
                if (_balance != null && sp.HasOutput)
                {
                    sp.SetBasePeriod(_balance.GetSpawnPeriod(MonsterGrades.BaseOf(sp.CurrentType)));
                }
            }
            //# 전투 시작 직후 스폰 개시 — 영웅 입장 완료를 기다리지 않고 첫 Tick 에 즉시 첫 스폰.
            //# (clock 시작·영웅 AI 활성화는 HandleHeroReachedCenter 가 별도로 처리.)
            _spawnersActive = true;
            //# VM 이 초기 스냅샷 폴링 + 이벤트 구독을 시작. Detach 는 OnDestroy 에서.
            _vm?.AttachSpawners(_spawners, this);
        }

        //# 스포너별 첫 스폰 지연 — 그 스포너 위치에서 BattleZone 중심까지 XZ 거리 D 와 출력 종의 실효 사거리·이동속도로 계산.
        private void ApplyFirstSpawnDelays()
        {
            if (_spawners == null || _balance == null || _zone == null)
                return;
            Vector3 center = _zone.transform.position;
            foreach (Spawner sp in _spawners)
            {
                if (sp == null || sp.HasOutput == false)
                    continue;
                EMonster species = MonsterGrades.BaseOf(sp.CurrentType);
                BalanceConfig.CharacterStat raw = _balance.GetMonster(species);
                if (raw == null)
                    continue;
                StatMultiplier mul = _typeModifiers.TryGetValue(species, out StatMultiplier m) ? m : StatMultiplier.Identity;
                Vector3 d = sp.transform.position - center;
                float distance = new Vector2(d.x, d.z).magnitude;
                sp.SetFirstSpawnDelay(SpawnTravelCompensation.FirstSpawnDelay(
                    SpawnTravelCompensation.LegacyRadius, distance, raw.Range * mul.RangeMul,
                    raw.MoveSpeed * mul.MoveSpeedMul, SpawnTravelCompensation.DelayScale));
            }
        }

        //# v0.2 메타 — 상점 레벨 → 종 스탯 배율(_typeModifiers) + 스포너 주기 일괄 적용 (기획서 §3.2 / plan Task 3.1).
        //# _metaConfig 가 null(Start 를 거치지 않는 격리 컨트롤러)이면 전부 skip — 기존 테스트 호환 (plan G2).
        //# 육성 상점 항목 무결성은 로더가 메타 로드 직후 1회 검증한다(§6.2) — 위반이면 GrowthDatabase.Current 가 null 이라 육성 축 미적용.
        private void ApplyMetaBonuses()
        {
            if (_metaConfig == null)
                return;

            MetaProfile profile = MetaSession.GetOrLoad();
            MetaBattleBonus bonus = MetaBattleBonus.From(profile, _metaConfig);

            //# 몬스터 전종 글로벌 — 카드 RegisterMonsterTypeBuff 와 동일 표면(StatMultiplier)에서 곱연산 누적.
            foreach (EMonster type in MonsterGrades.BaseMonsters)
            {
                foreach (EMonsterStatKind kind in (EMonsterStatKind[])System.Enum.GetValues(typeof(EMonsterStatKind)))
                {
                    float mul = bonus.GetStatMul(kind);
                    if (Mathf.Approximately(mul, 1f))
                        continue;

                    if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
                    {
                        m = new StatMultiplier();
                        _typeModifiers[type] = m;
                    }
                    m.Multiply(kind, mul);
                }
            }

            //# 종족별 강화 — 전종 글로벌과 같은 _typeModifiers 표면의 Hp·Power 축에 곱연산 접기(monster-species-enhancement §2).
            foreach (EMonster type in MonsterGrades.BaseMonsters)
            {
                float speciesMul = bonus.GetSpeciesMul(type);
                if (Mathf.Approximately(speciesMul, 1f))
                    continue;

                if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
                {
                    m = new StatMultiplier();
                    _typeModifiers[type] = m;
                }
                m.Multiply(EMonsterStatKind.Hp, speciesMul);
                m.Multiply(EMonsterStatKind.Power, speciesMul);
            }

            //# 각성 — 종족 x 스탯 독립 곱 축(AwakenStat.json, monster-growth-currencies §3.2). 같은 _typeModifiers 표면.
            foreach (EMonster type in MonsterGrades.BaseMonsters)
            {
                foreach (EMonsterStatKind kind in (EMonsterStatKind[])System.Enum.GetValues(typeof(EMonsterStatKind)))
                {
                    float awaken = bonus.GetAwakenMul(type, kind);
                    if (Mathf.Approximately(awaken, 1f))
                        continue;
                    if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
                    {
                        m = new StatMultiplier();
                        _typeModifiers[type] = m;
                    }
                    m.Multiply(kind, awaken);
                }
            }

            CacheSpeciesLevels(profile);

            //# 둥지 숙련 — 종족별 누적 효과 확정 + Lv2 주기 배율을 그 종족 둥지 전부에 곱한다(상점·유물·카드와 곱연산).
            _nestEffects.Clear();
            GrowthDatabase growth = GrowthDatabase.Current;
            if (growth != null)
            {
                foreach (EMonster type in MonsterGrades.BaseMonsters)
                {
                    int nestLevel = bonus.GetNestLevel(type);
                    if (nestLevel > 0)
                    {
                        _nestEffects[type] = growth.NestEffectsUpTo(nestLevel);
                    }
                }
            }
            if (_spawners != null)
            {
                foreach (Spawner sp in _spawners)
                {
                    if (sp != null && _nestEffects.TryGetValue(MonsterGrades.BaseOf(sp.CurrentType), out NestMasteryEffects nest) && Mathf.Approximately(nest.PeriodMul, 1f) == false)
                    {
                        sp.ScalePeriod(nest.PeriodMul);
                    }
                }
            }

            //# 특성 — 첫 스폰 전에 만들어 스폰 훅·영웅 반사 구독을 연다.
            if (growth != null && growth.HasTraits && _heroHealth != null)
            {
                MonsterTraitService traits = new MonsterTraitService(growth, bonus.CopyTraitTiers(), _heroHealth, _heroAuraRunner, _hero.GetComponent<MeleeAttacker>());
                if (traits.HasAny)
                {
                    _traits = traits;
                }
                else
                {
                    traits.Dispose();
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

        //# 유물 능력치형 — 보유 유물을 집계(_relicBonus)해 종 스탯 배율(_typeModifiers)·스포너 주기에 적용 (relics §6.1).
        //# _metaConfig 유무와 독립이다(ApplyMetaBonuses 의 조기 반환과 무관하게 적용).
        private void ApplyRelicBonuses()
        {
            _relicBonus = BuildRelicBonus();

            foreach (EMonsterStatKind kind in (EMonsterStatKind[])System.Enum.GetValues(typeof(EMonsterStatKind)))
            {
                float mul = _relicBonus.GetStatMul(kind);
                if (Mathf.Approximately(mul, 1f))
                    continue;
                foreach (EMonster type in MonsterGrades.BaseMonsters)
                {
                    if (_typeModifiers.TryGetValue(type, out StatMultiplier m) == false)
                    {
                        m = new StatMultiplier();
                        _typeModifiers[type] = m;
                    }
                    m.Multiply(kind, mul);
                }
            }

            if (Mathf.Approximately(_relicBonus.SpawnerPeriodMul, 1f) == false && _spawners != null)
            {
                foreach (Spawner sp in _spawners)
                {
                    if (sp != null)
                    {
                        sp.ScalePeriod(_relicBonus.SpawnerPeriodMul);
                    }
                }
            }

            //# 2배속 토글 — 유물 보유 시에만 노출, 마지막 상태는 프로필(BattleSpeedOn)에서 복원.
            _vm?.ConfigureBattleSpeed(_relicBonus.HasBattleSpeed, _relicBonus.HasBattleSpeed && MetaSession.GetOrLoad().BattleSpeedOn);
        }

        //# 이번 판 유물 집계 — 데일리는 개인 유물 중 보상·배속만 + 오늘의 유물 2개(daily-challenge §3), 그 외는 보유 유물 전부. 적용·워밍이 같은 집계를 쓴다.
        private RelicBattleBonus BuildRelicBonus()
        {
            MetaProfile profile = MetaSession.GetOrLoad();
            if (_spec != null && _spec.IsDaily)
            {
                DailyChallenge daily = _spec.Daily;
                return RelicBattleBonus.FromDaily(profile, RelicDatabase.Current, daily.RelicAId, daily.RelicAStack, daily.RelicBId, daily.RelicBStack);
            }
            return RelicBattleBonus.From(profile, RelicDatabase.Current);
        }

        //# 서두르는 둥지 — 모든 스포너의 첫 스폰 지연을 advanceSec 만큼 앞당김(0 하한).
        private void ApplyRelicSpawnPrelude()
        {
            if (_relicBonus.SpawnPreludeAdvanceSec <= 0f || _spawners == null)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null)
                {
                    sp.AdvanceFirstSpawnDelay(_relicBonus.SpawnPreludeAdvanceSec);
                }
            }
        }

        private void ApplyNestFirstSpawnAdvance()
        {
            if (_spawners == null)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && _nestEffects.TryGetValue(MonsterGrades.BaseOf(sp.CurrentType), out NestMasteryEffects nest) && nest.FirstSpawnAdvanceSec > 0f)
                {
                    sp.AdvanceFirstSpawnDelay(nest.FirstSpawnAdvanceSec);
                }
            }
        }

        //# 유물 전투 효과 개시 — 시계 시작(영웅이 중심에 도달한 "전투 시작") 시 1회. 화상·낙뢰의 첫 틱도 이 시각부터 센다.
        private void BeginRelicCombat()
        {
            if (_relicCombatBegan)
                return;
            _relicCombatBegan = true;
            if (_model != null && _model.Result != BattleResult.None)
                return;

            AttachRelicHeroAuras();
            SummonRelicOpeners();
            AttachRelicFrenzy();
        }

        //# 영웅 오라형(화상·낙뢰·공격력 감소) — 영구(-1) 부착. 성기사 저항(ConfigureDebuffResist)은 SpawnHero 에서 이미 설정돼 Attach 가 보정한다.
        private void AttachRelicHeroAuras()
        {
            if (_heroAuraRunner == null)
                return;
            if (_relicBonus.HeroBurnRatioPerSec > 0f)
            {
                _heroAuraRunner.Attach(new EmberBrandAura(_relicBonus.HeroBurnRatioPerSec), -1f);
            }
            foreach (RelicBattleBonus.LightningSpec spec in _relicBonus.Lightnings)
            {
                _heroAuraRunner.Attach(new ThunderShardAura(spec.IntervalSec, spec.HpRatio), -1f);
            }
            if (_relicBonus.HasHeroAttackDown && _hero != null)
            {
                LairCharacter character = _hero.GetComponent<LairCharacter>();
                IAttacker attacker = character != null ? character.Get<IAttacker>() : null;
                if (attacker != null)
                {
                    _heroAuraRunner.Attach(new HeroAttackDownAura(attacker, _relicBonus.HeroAttackFactor), -1f);
                }
            }
        }

        //# 매복의 뿔피리 — 영웅 근처에 몬스터 소환. 스테이지 규칙이 그 종을 봉인 중이면 소환 skip(E-17).
        private void SummonRelicOpeners()
        {
            if (_hero == null)
                return;
            foreach (RelicBattleBonus.SummonSpec spec in _relicBonus.OpeningSummons)
            {
                if (IsSpeciesSpawnerLocked(spec.Monster))
                    continue;
                for (int i = 0; i < spec.Count; ++i)
                {
                    SpawnMonsterRuntime(spec.Monster, _hero.transform.position);
                }
            }
        }

        //# 봉인된 둥지가 지금 이 종 둥지를 봉인 중인가 — 봉인은 둥지 1곳이라(대체 규칙 포함) 규칙의 봉인 종이 아니라 실제 봉인 상태로 판정한다(relics E-17, dungeon-formation §5.1).
        private bool IsSpeciesSpawnerLocked(EMonster species)
        {
            if (_spawners == null)
                return false;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.IsLocked && MonsterGrades.SameSpecies(sp.CurrentType, species))
                    return true;
            }
            return false;
        }

        //# 분노의 영혼 — 영웅 HP 비율이 hpFraction 이하가 되면 몬스터 전체 광폭화를 전투당 1회 발동.
        private void AttachRelicFrenzy()
        {
            if (_relicBonus.Frenzies.Count == 0 || _heroHealth == null)
                return;
            DetachRelicFrenzy();
            _relicFrenzyFired = new bool[_relicBonus.Frenzies.Count];
            _relicFrenzyHandler = HandleRelicFrenzyHp;
            _heroHealth.OnChanged += _relicFrenzyHandler;
        }

        private void HandleRelicFrenzyHp(int current, int max)
        {
            if (max <= 0 || (_model != null && _model.Result != BattleResult.None))
                return;
            float ratio = (float)current / max;
            for (int i = 0; i < _relicBonus.Frenzies.Count; ++i)
            {
                RelicBattleBonus.FrenzySpec spec = _relicBonus.Frenzies[i];
                if (_relicFrenzyFired[i] || ratio > spec.HpFraction)
                    continue;
                _relicFrenzyFired[i] = true;
                AddMonsterBuff(EMonsterBuff.Frenzy, spec.DurationSec);
            }
        }

        private void DetachRelicFrenzy()
        {
            if (_heroHealth != null && _relicFrenzyHandler != null)
            {
                _heroHealth.OnChanged -= _relicFrenzyHandler;
            }
            _relicFrenzyHandler = null;
        }

        //# 2배속 토글 변경 — 목표 속도 반영 + 프로필 저장(로컬 선호값). 카드 선택 중이면 PauseService 가 보관만 하고 Resume 때 반영.
        private void HandleBattleSpeedChanged(bool on)
        {
            ApplyBattleSpeed(on);
            MetaProfile profile = MetaSession.GetOrLoad();
            if (profile.BattleSpeedOn == on)
                return;
            profile.BattleSpeedOn = on;
            MetaSession.Store?.Save(profile);
        }

        private void ApplyBattleSpeed(bool on)
        {
            if (_pause == null)
                return;
            _pause.SetTargetScale(on && _relicBonus.HasBattleSpeed ? _relicBonus.BattleSpeedScale : 1f);
        }

        //# 드랍 굴림 — 영웅(챕터)·스테이지·EffectiveDifficulty 기준. 유물 DB 미로드/영웅 데이터 없음이면 굴리지 않는다.
        private RelicDropResult RollRelicDrop(MetaProfile profile, bool win)
        {
            RelicDatabase db = RelicDatabase.Current;
            HeroArchetypeRow archetype = _spec.Hero;
            if (db == null || archetype == null)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };
            return RelicDropRoller.Roll(db, profile, archetype.heroId, _spec.RelicChapterId,
                _spec.RelicStageIndex, _spec.RelicDifficulty, win, RandomUnit, GrowthDatabase.Current);
        }

        //# [0,1) 난수 — UnityEngine.Random.value 는 1.0 을 포함하므로 확률 1.0 경계가 어긋나지 않게 절단.
        private static float RandomUnit() => Mathf.Min(UnityEngine.Random.value, 0.99999994f);

        //# ISpawnerHost — Spawner 한 사이클. 종료 검사만 통과하면 count 전량 스폰 (동시 캡 제거, spec §2.A).
        public async void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
        {
            if (_model != null && _model.Result != BattleResult.None)
                return;
            //# v0.2 메타 — 도감 등장 종 기록 (HashSet — 중복 무해). 등급 몬스터는 기준 몬스터 항목에 합산(monster-grades §6).
            _seenMonsters.Add(MonsterGrades.BaseOf(type));

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(type);
            if (prefab == null)
                return;
            //# await 후 종료 재검사 — 인터리브로 await 동안 전투가 끝났으면 잔여 스폰 중단.
            if (_model != null && _model.Result != BattleResult.None)
                return;
            for (int i = 0; i < count; ++i)
            {
                CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
                if (p == null)
                    continue;
                //# 산개 — SpawnMonsterRuntime 과 동일. count 만큼 같은 exactPos 에 겹쳐 1마리처럼 보이는 문제 차단.
                //# 몬스터는 OnEnable 에서 isKinematic 이라 물리로 흩어지지 않으므로 스폰 시 좌표를 분산한다.
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.5f;
                offset.y = 0f;
                p.transform.position = exactPos + offset;
                Health spawned = ApplyMonsterStatsCore(p.gameObject, type, resetCurrent: true);
                //# 둥지 숙련 Lv3 스폰 보호 — 둥지에서 나온 몬스터만(카드·유물 소환 제외), 스폰 직후 durationSec 동안 받는 피해 배율.
                if (spawned != null && _nestEffects.TryGetValue(MonsterGrades.BaseOf(type), out NestMasteryEffects nest) && nest.GuardSec > 0f)
                {
                    spawned.GrantSpawnGuard(nest.GuardDamageTakenMul, nest.GuardSec);
                }
            }
        }

        //# 끈질긴 저주 — 영웅 러너의 Debuff 축 시한 디버프 지속 보너스 누적 (§15.3).
        public void AddHeroDebuffDurationBonus(float bonus)
        {
            if (_heroAuraRunner == null)
                return;
            _heroAuraRunner.AddDebuffDurationBonus(bonus);
        }

        //# Debuff Tier1 — 역병귀 독 최대 중첩 +bonus, 필드 역병귀에 소급 (§15.2).
        public void AddPlagueToxinMaxStack(int bonus)
        {
            if (bonus <= 0)
                return;
            _toxinMaxStackBonus += bonus;
            //# 살아 있는 필드 역병귀만 소급(사망 연출 중은 IsAlive 로 제외).
            //# ApplyMonsterStatsCore 가 _plagueInstances 를 재기록하므로 복사본 순회.
            List<KeyValuePair<Transform, Health>> snapshot = new List<KeyValuePair<Transform, Health>>(_plagueInstances);
            foreach (KeyValuePair<Transform, Health> plague in snapshot)
            {
                if (plague.Key == null || plague.Key.gameObject.activeInHierarchy == false || plague.Value == null || plague.Value.IsAlive == false)
                    continue;
                //# 등급 역병귀의 HP·공격력 배율을 잃지 않게 개체 자신의 키로 재적용한다.
                MonsterTag plagueTag = plague.Key.GetComponent<MonsterTag>();
                ApplyMonsterStatsCore(plague.Key.gameObject, plagueTag != null ? plagueTag.Key : EMonster.Plague, resetCurrent: false);
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
                if (e?.Health == null || e.Health.IsAlive == false || e.Transform == null)
                    continue;
                MonsterTag tag = e.Transform.GetComponent<MonsterTag>();
                if (tag == null || MonsterGrades.SameSpecies(tag.Key, type) == false)
                    continue;
                ApplyMonsterStats(e.Transform.gameObject, tag.Key, resetCurrent: false);
            }

            //# 카드 source 가 있으면 픽 누적 추적 (직접 호출 / 시뮬레이션 외 경로는 _currentCardScope null).
            if (_currentCardScope != null)
            {
                TrackCardPick(type, stat, _currentCardScope);
            }

            //# VM 셀이 동일 종 출력 스포너를 모두 갱신하도록 broadcast.
            OnTypeModifierChanged?.Invoke(type);
        }

        //# 스포너 상태 UI — 카드 효과 적용의 단일 진입점 (기획서 §4.2 BLOCKER 4 결정).
        //# 3개 기존 호출지점(card.Effect.Apply(_ctx))을 이 메서드로 치환해 source 를 잠시 보관한다.
        public void ApplyCardEffect(CardData card)
        {
            if (card?.Effect == null || _ctx == null)
                return;
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
            {
                _typeModifierPicks[type] = list = new List<BattleViewModel.AppliedBuff>();
            }

            BattleViewModel.AppliedBuff existing = list.Find(b => b.Source == source);
            if (existing != null)
            {
                existing.PickCount++;
            }
            else
            {
                list.Add(new BattleViewModel.AppliedBuff
                {
                    Source = source,
                    PickCount = 1,
                    Stat = stat,
                    AggregateMultiplier = 1f,
                });
            }

            //# 동일 종·동일 Stat 의 엔트리들에 누적 배율을 일괄 동기화 (종 1 ↔ 카드 1 매핑이지만
            //# 향후 1↔다 매핑 확장에 대비해 list 순회로 갱신).
            if (_typeModifiers.TryGetValue(type, out StatMultiplier mul))
            {
                foreach (BattleViewModel.AppliedBuff b in list)
                {
                    if (b.Stat == stat && b.Source != null && b.Source.Axis == EBuildAxis.Tank)
                    {
                        b.AggregateMultiplier = mul.Get(stat);
                    }
                }
            }
        }

        //# v1.0 — Spawn 카테고리 픽 누적. Enhance 의 TrackCardPick 와 자료구조 공유 (Dictionary<EMonster, List<AppliedBuff>>).
        //# Stat 필드는 EMonsterStatKind.Hp (기본값) — 표시 가공 모듈이 카테고리 분기로 읽지 않음.
        private void TrackSpawnPick(EMonster type, CardData source)
        {
            if (_typeModifierPicks.TryGetValue(type, out List<BattleViewModel.AppliedBuff> list) == false)
            {
                _typeModifierPicks[type] = list = new List<BattleViewModel.AppliedBuff>();
            }

            BattleViewModel.AppliedBuff existing = list.Find(b => b.Source == source);
            if (existing != null)
            {
                existing.PickCount++;
            }
            else
            {
                list.Add(new BattleViewModel.AppliedBuff
                {
                    Source = source,
                    PickCount = 1,
                    Stat = EMonsterStatKind.Hp,    //# default — Category=Spawn 분기에서 안 읽음
                    AggregateMultiplier = 1f,      //# unused for Spawn
                });
            }
        }

        //# 스포너 상태 UI — VM 이 SpawnerSnapshot 채울 때 사용. 없는 종이면 빈 array.
        public IReadOnlyList<BattleViewModel.AppliedBuff> GetAppliedBuffs(EMonster type)
            => _typeModifierPicks.TryGetValue(MonsterGrades.BaseOf(type), out List<BattleViewModel.AppliedBuff> list)
                ? (IReadOnlyList<BattleViewModel.AppliedBuff>)list
                : System.Array.Empty<BattleViewModel.AppliedBuff>();

        //# 지속 스폰 — 추가소환 카드. 해당 종을 출력 중인 모든 Spawner 동시 출력 +1.
        //# v1.0 — _currentCardScope non-null (= ApplyCardEffect 진입 중) 이면 Spawn 픽 추적 + 셀 IconRow 갱신 이벤트 발행.
        public void IncrementSpawnerOutput(EMonster type)
        {
            if (_spawners == null)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.HasOutput && MonsterGrades.SameSpecies(sp.CurrentType, type))
                {
                    sp.IncrementOutput();
                }
            }

            //# v1.0 — Spawn 픽 누적 (type 출력 Spawner 0 대여도 누적 — retroactive 정책 §2.3.6).
            if (_currentCardScope != null)
            {
                TrackSpawnPick(type, _currentCardScope);
            }

            //# v1.0 — Spawn 슬롯 IconRow 갱신용 broadcast. Enhance 와 같은 이벤트 (의미 확장).
            OnTypeModifierChanged?.Invoke(type);
        }

        //# 지속 스폰 — 융합 카드. 출력 종이 from 인 모든 Spawner 의 출력 종을 to 로 변경.
        public void ReplaceSpawnerOutput(EMonster from, EMonster to)
        {
            if (_spawners == null)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.HasOutput && MonsterGrades.SameSpecies(sp.CurrentType, from))
                {
                    sp.ReplaceOutput(to);
                }
            }
        }

        //# 카드 리뉴얼 v0.6 — SpawnerHaste 카드 / Swarm Tier2. 모든 Spawner 의 _spawnPeriod ×mul (영구).
        public void ScaleAllSpawnerPeriods(float mul)
        {
            if (_spawners == null || mul <= 0f)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null)
                {
                    sp.ScalePeriod(mul);
                }
            }
        }

        //# 카드 리뉴얼 v0.6 — Swarm Tier3. 모든 Spawner 의 _outputCount +delta (영구).
        public void IncrementAllSpawnerOutputs(int delta)
        {
            if (_spawners == null || delta <= 0)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null)
                {
                    sp.IncrementOutput(delta);
                }
            }
        }

        //# 카드 리뉴얼 v0.6 — FastBreeding 카드 전용. 출력 종이 type 인 Spawner 만 ScalePeriod(mul).
        //# ScaleAllSpawnerPeriods 와 달리 종 매칭. 매칭 Spawner 0개면 no-op.
        public void ScaleSpawnerPeriodForType(EMonster type, float mul)
        {
            if (_spawners == null || mul <= 0f)
                return;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.HasOutput && MonsterGrades.SameSpecies(sp.CurrentType, type))
                {
                    sp.ScalePeriod(mul);
                }
            }
        }

        private async void EndBattle(BattleResult result)
        {
            if (_model.Result != BattleResult.None)
                return;   //# 중복 방지
            _clock.Stop();
            //# 결과창 구간은 1배속 — 배속이 결과 연출·다음 씬으로 새지 않게 목표 속도를 1 로 되돌린다(relics §5.2-5).
            _pause?.SetTargetScale(1f);
            //# 스테이지 규칙 전원 비활성 — 부수효과 원복 + FX 풀 반환(E-6). 결과 확정(_vm.EndBattle) 전 동기 실행.
            _ruleRunner?.Shutdown();

            //# Slice C — 한 판 결과 기록 (생존 몬스터 수 집계)
            int aliveMonsters = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Health != null && e.Health.IsAlive)
                {
                    aliveMonsters++;
                }
            }
            _recorder.FinishRun(result, _clock.Elapsed, aliveMonsters);

            //# v0.2 메타 — 보상 정산. RunRecorder(에디터 한정)와 별개로 빌드에서도 동작.
            //# 순서 계약 (기획서 §11.4): 런 보상 → 프로필 가산 → 영주 보상 자동 수령 → 도전과제 → 저장 → Arg 구성.
            int soulsGained = 0;
            int xpGained = 0;
            int lordLevelUp = 0;
            int lordRewardSouls = 0;
            bool isNewBest = false;
            int newlyUnlockedDifficulty = 0;
            RelicDropResult relicDrop = new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };
            List<AchievementDef> newlyAchieved = new List<AchievementDef>();
            DailyEndResult dailyEnd = new DailyEndResult { Multiplier = 1m };
            List<CurrencyGain> currencyGains = new List<CurrencyGain>();
            if (_metaConfig != null)
            {
                MetaProfile profile = MetaSession.GetOrLoad();
                SoulReward reward = SoulRewardCalculator.Calculate(
                    result, _clock.Elapsed, _model.TotalSeconds, GetHeroDamagedRatio(), _metaConfig);
                //# 유물 보상 가산 — 전투 시작 스냅샷 기준이라 이번 판 드랍은 반영되지 않는다(relics §2.7).
                reward = _relicBonus.ApplyReward(reward, result == BattleResult.Win);

                int levelBefore = LordLevelService.LevelFromXp(profile.LordXp, _metaConfig);
                profile.Souls += reward.Souls;
                profile.LordXp += reward.Xp;
                profile.TotalRuns++;
                //# 탑 분기는 easyRun(_effectiveDifficulty == 0 이라 탑에서도 참)·IsNewStageBest·RecordStageRun 계산보다 "먼저" 갈라야 한다 —
                //# 아니면 챕터 최단 기록·전역/스테이지 랭킹·ClearedStage·StageRecords 가 오염된다 (infinite-tower §8.1).
                //# 데일리도 같은 이유로 먼저 가른다 — 챕터 진행·스테이지 기록·랭킹·탑·난이도 해금을 건드리지 않는다 (daily-challenge §10.1).
                //# 각성석 첫 클리어/반복 판정은 기록 갱신 "전" 지급 기록으로 가른다(monster-growth-currencies §2.1).
                int awakenPaidBefore = profile.GetAwakenPaid(profile.SelectedStage);
                if (_spec.IsTower)
                {
                    RecordTowerResult(profile, result);
                }
                else if (_spec.IsDaily)
                {
                    dailyEnd = RecordDailyResult(profile, result);
                }
                else
                {
                    RecordChapterResult(profile, result, out isNewBest, out newlyUnlockedDifficulty);
                }
                //# 재화 정산 (§11.6 순서: 기록 갱신 → 재화 → 유물 굴림 → 저장) — 모드별 배타(챕터 = 각성석, 데일리 = 인장, 탑 = 결정).
                currencyGains = SettleBattleCurrencies(profile, result, dailyEnd, awakenPaidBefore);

                //# 의도: 메타 정산(_metaConfig) 블록 안 — MetaConfig 없는 격리 컨트롤러(테스트)에선 보상·드랍을 모두 건너뛴다(프로필 미사용).
                //# 유물 드랍 굴림 — 승리 정상 확률 / 패배 1/4 (relics §2.1). 데일리는 평소 굴림 없이 그날 첫 승리에만 1개 확정(§7.1, RelicMissStreak 불변).
                if (_spec.IsDaily)
                {
                    if (dailyEnd.Record.IsFirstWin)
                    {
                        relicDrop = RelicDropRoller.RollGuaranteed(RelicDatabase.Current, profile, _spec.Hero.heroId, _spec.RelicDifficulty, RandomUnit, GrowthDatabase.Current);
                    }
                }
                else
                {
                    relicDrop = RollRelicDrop(profile, result == BattleResult.Win);
                }

                //# 도감 — 이번 판 등장 종 + 픽 카드 누적 (distinct).
                foreach (EMonster seen in _seenMonsters)
                    profile.AddDistinct(profile.SeenMonsters, seen.ToString());
                foreach (ECardId pick in _runPicks)
                    profile.AddDistinct(profile.PickedCards, pick.ToString());
                //# 카드별 누적 픽 횟수 — 같은 판 중첩 픽은 픽한 횟수만큼 +N (기록 팝업 "가장 많이 픽한 카드").
                foreach (ECardId pick in _runPicks)
                    profile.AddCardPick(pick.ToString());

                //# 영주 보상 자동 수령 — 멱등 가드 (기획서 §4.4). LordLevel=0 이면 결과 팝업 영주 줄 생략 (§9.2).
                int levelAfter = LordLevelService.LevelFromXp(profile.LordXp, _metaConfig);
                lordRewardSouls = LordLevelService.ClaimRewards(profile, _metaConfig);
                if (levelAfter > levelBefore)
                {
                    lordLevelUp = levelAfter;
                }

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

            UIBase resultUI = await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup, new ResultPopupArg
            {
                Result = result,
                HasMeta = _metaConfig != null,
                SoulsGained = soulsGained,
                XpGained = xpGained,
                LordLevel = lordLevelUp,
                LordRewardSouls = lordRewardSouls,
                NewlyAchieved = newlyAchieved,
                ClearTime = _clock.Elapsed,
                IsNewBest = isNewBest,
                Difficulty = _effectiveDifficulty,
                DifficultyUnlocked = newlyUnlockedDifficulty,
                IsTower = _spec.IsTower,
                TowerFloor = _spec.IsTower ? _spec.Floor.Floor : 0,
                TowerIsBoss = _spec.IsTower && _spec.Floor.IsBoss,
                //# 남은 HP 비율 — 패배 화면 "영웅 남은 HP" 줄 (깎은 비율의 여집합).
                HeroHpRatio = 1f - GetHeroDamagedRatio(),
                HasRelicDrop = relicDrop.Acquired,
                RelicId = relicDrop.RelicId,
                RelicStackAfter = relicDrop.StackAfter,
                RelicMaxStack = relicDrop.MaxStack,
                RelicIsNew = relicDrop.IsNew,
                ShowNextChance = relicDrop.ShowNextChance,
                NextWinChancePercent = relicDrop.NextWinChancePercent,
                IsDaily = _spec.IsDaily,
                DailyDayIndex = _spec.IsDaily ? _spec.Daily.DayIndex : 0,
                DailyAttemptsLeft = _spec.IsDaily ? DailyAttemptsLeft() : 0,
                DailyFirstWinBonus = dailyEnd.Record.IsFirstWin,
                DailyBonusSouls = dailyEnd.BonusSouls,
                DailyStreak = dailyEnd.Record.Streak,
                DailyStreakMultiplier = dailyEnd.Multiplier,
                OnDailyRetry = _spec.IsDaily ? TryDailyRetry : (System.Func<bool>)null,
                CurrencyGains = currencyGains,
                HasRelicShard = relicDrop.IsShard,
                RelicShardAmount = relicDrop.ShardAmount,
                RelicShardRarity = relicDrop.IsShard ? relicDrop.ShardRarity.ToString() : null,
            });

            //# 데일리 순위 — 결과창을 먼저 띄우고("순위 집계 중") 제출·조회는 비동기로 이어서 갱신한다(§9.4). 끝낸 판이 기록됐을 때만.
            if (_spec.IsDaily && dailyEnd.Recorded)
            {
                SubmitDailyRank(resultUI as ResultPopup, dailyEnd.Record.BestScore);
            }
        }

        //# 이번 판 특수 재화 지급 (monster-growth-currencies §2) — 육성 데이터 미로드면 지급 없음(§6.3-8). 반환 = 결과창 칩 입력.
        private List<CurrencyGain> SettleBattleCurrencies(MetaProfile profile, BattleResult result, DailyEndResult dailyEnd, int awakenPaidBefore)
        {
            List<CurrencyGain> gains = new List<CurrencyGain>();
            GrowthDatabase growth = GrowthDatabase.Current;
            if (growth == null)
                return gains;
            if (_spec.IsDaily)
            {
                DailyDatabase daily = DailyDatabase.Current;
                if (dailyEnd.Recorded && daily != null)
                {
                    int seals = CurrencySettlement.DailySeals(dailyEnd.Record.IsFirstWin, dailyEnd.Record.Streak, daily.Config);
                    profile.AddCurrency(ECurrency.MoonSeal, seals);
                    CurrencySettlement.AddGain(gains, ECurrency.MoonSeal, seals);
                }
                return gains;
            }
            if (result != BattleResult.Win)
                return gains;
            if (_spec.IsTower)
            {
                TowerCrystalGain crystal = CurrencySettlement.SettleTowerWin(profile, growth, TowerDatabase.Current, DifficultyTable.CurrentMaxIndex, _spec.Floor.Floor);
                CurrencySettlement.AddGain(gains, ECurrency.AbyssCrystal, crystal.ThisRun);
                ShowRetroToast(ECurrency.AbyssCrystal, crystal.Retroactive, growth);
                return gains;
            }
            ChapterAwakenGain awaken = CurrencySettlement.SettleChapterWin(profile, growth, _effectiveDifficulty, awakenPaidBefore);
            CurrencySettlement.AddGain(gains, ECurrency.AwakenStone, awaken.ThisRun);
            ShowRetroToast(ECurrency.AwakenStone, awaken.Retroactive, growth);
            return gains;
        }

        //# 같은 정산이 함께 지급한 업데이트 이전 기록분 — 결과창 칩이 아니라 소급 토스트(1481)로 알린다.
        private void ShowRetroToast(ECurrency currency, int amount, GrowthDatabase growth)
        {
            if (amount < 1)
                return;
            ToastView.Show(string.Format(LookupFormat(GrowthStrings.RetroReward, "이전 기록 보상 {0} +{1}"),
                GrowthText.CurrencyName(currency, growth, id => LookupFormat(id, string.Empty)), amount));
        }

        //# 데일리 판 종료 정산 결과 — 결과창·순위 제출 입력.
        private struct DailyEndResult
        {
            public bool Recorded;
            public DailyRecordResult Record;
            public int BonusSouls;
            public decimal Multiplier;
        }

        //# 데일리 판 기록 (daily-challenge §7·§10.1) — 전체 전적(TotalWins)·날짜별 최고 점수·연속·첫 승리 보너스 소울. 챕터·탑 기록과 랭킹은 건드리지 않는다.
        //# 점수 = 승리 클리어 ms / 패배 300000 + 남은 HP 천분율. 판 도중 0시를 넘겨도 시작 시 확정한 날짜로 기록한다(E-1).
        private DailyEndResult RecordDailyResult(MetaProfile profile, BattleResult result)
        {
            DailyEndResult end = new DailyEndResult { Multiplier = 1m };
            bool win = result == BattleResult.Win;
            if (win)
            {
                profile.TotalWins++;
            }
            int score = win
                ? DailyRules.WinScore(_clock.Elapsed)
                : DailyRules.LoseScore(_heroHealth != null ? _heroHealth.Current : 0, _heroHealth != null ? _heroHealth.Max : 0);
            end.Record = profile.RecordDailyResult(_spec.Daily.DayIndex, score);
            end.Recorded = DailyRules.IsValidScore(end.Record.BestScore);
            DailyDatabase db = DailyDatabase.Current;
            if (end.Record.IsFirstWin && db != null)
            {
                end.Multiplier = DailyRules.StreakMultiplier(end.Record.Streak, db.Config);
                end.BonusSouls = DailyRules.BonusSouls(end.Record.Streak, db.Config);
                profile.Souls += end.BonusSouls;
            }
            return end;
        }

        //# 데일리 순위 제출 + 내 순위 조회 (§6.2) — 그날 로컬 최고 점수를 제출(서버가 더 좋으면 미갱신). 실패는 토스트만, 다음 판 종료 때 자연 재시도.
        //# 전역·스테이지 랭킹(SubmitScoreAsync/SubmitStageScoreAsync)은 호출하지 않는다.
        private async void SubmitDailyRank(ResultPopup popup, int bestScore)
        {
            DailyChallenge daily = _spec.Daily;
            string failedText = LookupFormat(DailyStrings.RankFailed, "순위를 불러오지 못했습니다");
            string submitFailedText = LookupFormat(DailyStrings.SubmitFailed, "기록 제출 실패 - 다음 도전 때 다시 제출됩니다");
            if (MetaSession.Ranking == null)
            {
                ToastView.Show(submitFailedText, EToastKind.Warning);
                if (popup != null)
                {
                    popup.ShowDailyRank(failedText);
                }
                return;
            }
            MetaProfile profile = MetaSession.GetOrLoad();
            string name = MetaProfile.ResolveDisplayName(profile.DisplayName, AuthTokenStore.GetOrCreateDeviceId());
            bool submitted = await MetaSession.Ranking.SubmitDailyAsync(daily.DayIndex, bestScore, daily.HeroId, daily.ComboKey, name);
            if (submitted == false)
            {
                ToastView.Show(submitFailedText, EToastKind.Warning);
            }
            List<RankingRowDto> mine = await MetaSession.Ranking.GetMyDailyRankAsync(daily.DayIndex);
            if (popup == null)
                return;
            if (mine == null || mine.Count == 0)
            {
                popup.ShowDailyRank(failedText);
                return;
            }
            popup.ShowDailyRank(string.Format(LookupFormat(DailyStrings.RankPlace, "{0}위"), mine[0].rank));
        }

        //# 결과창 "다시 도전" (§10.1) — 세션 기준값 + 단조 시계로 날짜를 다시 판정(서버 호출 없음). 같은 날·남은 도전 있음이면 같은 조합으로 재시작.
        //# 날짜가 바뀌었거나 세션 시각이 없으면 안내 후 마을로(차감 없음, E-2). 반환 = 씬 이동을 시작했는가.
        private bool TryDailyRetry()
        {
            EDailyStartCheck check = DailyRules.CheckStart(MetaSession.GetOrLoad(), DailyDatabase.Current, MetaSession.DailyClock, _spec.Daily.DayIndex);
            switch (check)
            {
                case EDailyStartCheck.Ok:
                    SceneManager.LoadScene(EScene.Battle.ToString());
                    return true;
                case EDailyStartCheck.Exhausted:
                    ToastView.Show(LookupFormat(DailyStrings.Exhausted, "오늘 도전을 모두 사용했습니다"), EToastKind.Warning);
                    return false;
                case EDailyStartCheck.DayChanged:
                    ToastView.Show(LookupFormat(DailyStrings.DayChanged, "날짜가 바뀌었습니다. 새 도전이 열렸습니다"), EToastKind.Warning);
                    break;
                case EDailyStartCheck.Offline:
                    ToastView.Show(LookupFormat(DailyStrings.OnlineRequired, "온라인 연결이 필요합니다"), EToastKind.Warning);
                    break;
                default:
                    //# Disabled/Locked — 전용 문구 없이 마을 복귀만(마을 타일이 상태를 보여 준다).
                    break;
            }
            AbortToVillage();
            return true;
        }

        //# 챕터 판 기록 (기존 동작 그대로) — 최단·스테이지 전적·클리어 진행·난이도·전역/스테이지 랭킹.
        private void RecordChapterResult(MetaProfile profile, BattleResult result, out bool isNewBest, out int newlyUnlockedDifficulty)
        {
            //# 신기록 판정 — RecordStageRun 이 최단을 덮기 "전에" 이전 기록과 비교해야 한다(결과 팝업 NEW 배지).
            //# Normal/Hard 판은 최단 기록·랭킹에서 제외(difficulty-select §1.6) — Runs/Wins 만 올린다.
            bool easyRun = _effectiveDifficulty == 0;
            int maxDifficulty = DifficultyTable.CurrentMaxIndex;
            int unlockedBefore = profile.MaxSelectableDifficulty(profile.SelectedStage, maxDifficulty);
            isNewBest = easyRun && profile.IsNewStageBest(profile.SelectedStage, result == BattleResult.Win, _clock.Elapsed);
            //# 스테이지별 전적 — 이번 판의 SelectedStage 기준 (spec §5). 총계와 같은 판에서 함께 움직인다.
            profile.RecordStageRun(profile.SelectedStage, result == BattleResult.Win, _clock.Elapsed, easyRun);
            if (result == BattleResult.Win)
            {
                profile.TotalWins++;
                //# hero-stage-variant — 방금 클리어한 SelectedStage 로 해금 진행 갱신(MaxStage 종점, spec §6.1). 저장은 아래 Save.
                profile.ClearedStage = StageProgress.ResolveClearedStage(profile.ClearedStage, profile.SelectedStage);
                profile.RecordDifficultyClear(profile.SelectedStage, _effectiveDifficulty);
                if (easyRun)
                {
                    if (profile.BestClearTime < 0f || _clock.Elapsed < profile.BestClearTime)
                    {
                        profile.BestClearTime = _clock.Elapsed;
                    }
                    //# 최단 클리어 랭킹 제출(best-effort) — 클리어타임 = 영웅 처치까지 경과(낮을수록 상위).
                    SubmitRanking(profile);
                }
            }
            int unlockedAfter = profile.MaxSelectableDifficulty(profile.SelectedStage, maxDifficulty);
            newlyUnlockedDifficulty = unlockedAfter > unlockedBefore ? unlockedAfter : 0;
        }

        //# 탑 판 기록 (infinite-tower §7.1) — TotalWins 와 탑 층 클리어만. 챕터 전적·최단·ClearedStage·난이도·전역/스테이지 랭킹은 건드리지 않는다. 탑 랭킹 제출은 슬라이스 B.
        private void RecordTowerResult(MetaProfile profile, BattleResult result)
        {
            if (result != BattleResult.Win)
                return;
            profile.TotalWins++;
            profile.RecordTowerClear(_spec.Floor.Floor, _clock.Elapsed, TowerDatabase.Current.Config.seasonId);
        }

        //# 승리 시 랭킹 제출 — fire-and-forget. 실패해도 결과창 흐름 차단 안 함(기획서 §6 무음 로그).
        //# 표시명: MetaProfile.DisplayName 우선, 없으면 "영주 #"+deviceId 앞4자 대문자(Delta 1 / 기획서 §1).
        private async void SubmitRanking(MetaProfile profile)
        {
            if (MetaSession.Ranking == null)
                return;
            int clearMs = Mathf.RoundToInt(_clock.Elapsed * 1000f);
            //# 랭킹 hero = 선택 스테이지가 속한 챕터의 heroId (chapter-system §2.3). 데이터 없으면 프로필 값 → 기사.
            HeroArchetypeRow archetype = _spec.Hero;
            string hero = archetype != null
                ? archetype.heroId
                : (profile != null && string.IsNullOrEmpty(profile.SelectedHero) == false ? profile.SelectedHero : EHero.Knight.ToString());
            string name = MetaProfile.ResolveDisplayName(
                profile != null ? profile.DisplayName : null,
                AuthTokenStore.GetOrCreateDeviceId());
            await MetaSession.Ranking.SubmitAsync(clearMs, hero, name);
            //# 스테이지별 랭킹 — 서버가 스테이지 기존 최단보다 빠를 때만 갱신한다(느린 기록이 덮지 않음).
            await MetaSession.Ranking.SubmitStageAsync(profile != null ? _spec.LinearStage : 1, clearMs, hero, name);
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

        //# 카드 추첨은 배치 상태와 독립이며 기존 픽 상한만 적용한다.
        private bool IsCardExcluded(ECardId id)
        {
            return _pickCounter.IsCapped(id);
        }

        private bool HasOccupiedNest(EMonster? species)
        {
            if (_spawners == null)
                return false;
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.HasOutput && (species.HasValue == false || MonsterGrades.SameSpecies(sp.CurrentType, species.Value)))
                    return true;
            }
            return false;
        }

        private bool HasMonsterEffectTarget(EMonster? species)
        {
            if (HasOccupiedNest(species))
                return true;
            foreach (CharacterRegistry.Entry entry in CharacterRegistry.Monsters)
            {
                if (entry.Transform == null || entry.Health == null || entry.Health.IsAlive == false)
                    continue;
                if (species.HasValue == false)
                    return true;
                //# Registry Entry 에 종 키가 없어 카드 선택창을 열 때만 1회 조회한다(프레임 경로 아님).
                MonsterTag tag = entry.Transform.GetComponent<MonsterTag>();
                if (tag != null && MonsterGrades.SameSpecies(tag.Key, species.Value))
                    return true;
            }
            return false;
        }

        //# 대상 안내는 카드 선택창에만 제공하며 덱 후보나 실제 효과를 바꾸지 않는다.
        private string CardTargetNotice(CardData card)
        {
            CardEntry entry = CardDatabase.Current?.GetCard(card.Id);
            if (entry == null)
                return string.Empty;
            switch (entry.Effect)
            {
                case ECardEffect.SpawnWisps: return NestTargetNotice(EMonster.Wisp);
                case ECardEffect.SpawnWraith: return NestTargetNotice(EMonster.Wraith);
                case ECardEffect.SpawnReapers: return NestTargetNotice(EMonster.Reaper);
                case ECardEffect.SpawnPlagues: return NestTargetNotice(EMonster.Plague);
                case ECardEffect.SpawnPhantoms:
                case ECardEffect.FastBreeding: return NestTargetNotice(EMonster.Phantom);
                case ECardEffect.SpawnerHaste: return NestTargetNotice(null);
                case ECardEffect.WispHpBoost: return MonsterTargetNotice(EMonster.Wisp);
                case ECardEffect.WraithDamageBoost: return MonsterTargetNotice(EMonster.Wraith);
                case ECardEffect.ReaperAtkSpeed: return MonsterTargetNotice(EMonster.Reaper);
                case ECardEffect.HexRangeBoost: return MonsterTargetNotice(EMonster.Hex);
                case ECardEffect.PlagueToxinBoost: return MonsterTargetNotice(EMonster.Plague);
                case ECardEffect.PhantomMoveSpeedBoost: return MonsterTargetNotice(EMonster.Phantom);
                case ECardEffect.WispWraithPowerBoost:
                case ECardEffect.GuardianRage:
                case ECardEffect.ToughHide: return MonsterTargetNotice(EMonster.Wisp, EMonster.Wraith);
                case ECardEffect.ReaperHexPowerBoost: return MonsterTargetNotice(EMonster.Reaper, EMonster.Hex);
                case ECardEffect.Slow:
                case ECardEffect.Frenzy:
                case ECardEffect.IronWill:
                case ECardEffect.BloodThirst: return MonsterTargetNotice(null);
                default: return string.Empty;
            }
        }

        private string NestTargetNotice(EMonster? species)
        {
            if (HasOccupiedNest(species))
                return string.Empty;
            return species.HasValue
                ? string.Format(LookupFormationText(FormationViewModel.StrNoEffectNestSpecies), SpeciesVisual.SpeciesName(species.Value))
                : LookupFormationText(FormationViewModel.StrNoEffectNest);
        }

        private string MonsterTargetNotice(EMonster? species)
        {
            if (HasMonsterEffectTarget(species))
                return string.Empty;
            return species.HasValue
                ? string.Format(LookupFormationText(FormationViewModel.StrNoEffectMonsterSpecies), SpeciesVisual.SpeciesName(species.Value))
                : LookupFormationText(FormationViewModel.StrNoEffectMonster);
        }

        private string MonsterTargetNotice(EMonster first, EMonster second)
        {
            if (HasMonsterEffectTarget(first))
                return MonsterTargetNotice(second);
            if (HasMonsterEffectTarget(second))
                return MonsterTargetNotice(first);
            return string.Format(LookupFormationText(FormationViewModel.StrNoEffectMonsterSpecies),
                SpeciesVisual.SpeciesName(first) + "·" + SpeciesVisual.SpeciesName(second));
        }

        private static string LookupFormationText(int id) => CHText.StringProvider?.GetString(id) ?? string.Empty;

        //# 시너지 임계 3/5/7장 = Tier 1/2/3 (card-renewal §4) — 4축 최고값.
        private int GetMaxSynergyTier()
        {
            if (_synergy == null)
                return 0;
            int max = 0;
            foreach (EBuildAxis axis in (EBuildAxis[])System.Enum.GetValues(typeof(EBuildAxis)))
            {
                int count = _synergy.GetCount(axis);
                int tier = count >= BuildSynergyService.Tier3Threshold ? 3
                    : count >= BuildSynergyService.Tier2Threshold ? 2
                    : count >= BuildSynergyService.Tier1Threshold ? 1 : 0;
                if (tier > max)
                {
                    max = tier;
                }
            }
            return max;
        }

        //# B1+B2 — 큐 비울 때까지 카드 선택 팝업 순차 처리
        private async void TryProcessNext()
        {
            if (_processingQueue)
                return;
            if (_queue.Count == 0)
                return;
            if (_model.Result != BattleResult.None)
                return;

            _processingQueue = true;

            while (_queue.TryDequeue(out TriggerQueue.Entry entry))
            {
                if (_model.Result != BattleResult.None)
                    break;

                //# B2 — Source 에 따라 적절한 덱 선택. 덱 미로드면 해당 트리거 스킵.
                CardDeck deck = entry.SourceType == TriggerQueue.Source.Passive ? _passiveDeck : _activeDeck;
                if (deck == null)
                    continue;

#if UNITY_EDITOR
                //# 시뮬레이션 전용 — 팝업/일시정지를 건너뛰고 즉시 픽. tcs 무한 대기 회피.
                if (DebugAutoPicker != null)
                {
                    IReadOnlyList<CardData> simChoices = deck.Draw(3, IsCardExcluded);
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

                IReadOnlyList<CardData> choices = deck.Draw(3, IsCardExcluded);
#if UNITY_EDITOR
                //# 디버그 — 지정 카드 강제 등장. 팝업 경로 한정 (시뮬 DebugAutoPicker 경로 비간섭) — 토글 OFF 면 내부 no-op.
                if (_forcedCardDraw != null)
                {
                    choices = _forcedCardDraw.Apply(choices, entry.SourceType == TriggerQueue.Source.Passive,
                        _allCards, id => _pickCounter.IsCapped(id));
                }
#endif
                if (choices.Count == 0)
                    continue;
                _pause.Pause();
                _vm.SetExitAvailable(false);
                System.Threading.Tasks.TaskCompletionSource<bool> tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                SynergyVisualConfig synergyVisual = await LoadSynergyVisual();
                CardSelectionArg arg = new CardSelectionArg
                {
                    Choices = choices,
                    TargetNoticeOf = CardTargetNotice,
                    SynergyVisual = synergyVisual,
                    BuildCountOf = axis => _vm.GetBuildCount(axis),
                    IsPassive = entry.SourceType == TriggerQueue.Source.Passive,
                    TriggerHpPercent = PassiveTriggerService.ResolveHpPercent(_balance?.PassiveThresholds, entry.Index),
                    ActivePeriodSeconds = ActiveTriggerService.ResolvePeriodSeconds(_balance?.ActiveThresholds),
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
                        if (card != null)
                        {
                            ApplyCardEffect(card);
                        }
                        tcs.TrySetResult(true);
                    }
                };
                UIBase popup = await CHMUI.Instance.ShowUIAsync(EUI.CardSelectionPopup, arg);
                if (popup != null)
                {
                    await tcs.Task;
                }
                _pause.Resume();
                _vm.SetExitAvailable(true);
            }

            _processingQueue = false;
        }

        //# 시너지 아이콘 SO — 카드 선택창 첫 오픈 시 1회 로드. 실패하면 경고 1회 후 아이콘 없이 진행.
        private async Task<SynergyVisualConfig> LoadSynergyVisual()
        {
            if (_synergyVisualTried == false)
            {
                _synergyVisualTried = true;
                _synergyVisual = await CHMResource.Instance.LoadAsync<SynergyVisualConfig>(EData.SynergyVisualConfig);
                if (_synergyVisual == null)
                {
                    Debug.LogWarning("[BattleController] SynergyVisualConfig 로드 실패 — 카드 축 아이콘 비활성");
                }
            }
            return _synergyVisual;
        }

        //# 선택 스테이지 챕터의 영웅 종류 — 데이터 없음/알 수 없는 값이면 기사(SpawnHero 가 데이터 실패를 따로 중단한다).
        private EHero SelectedHeroKey()
        {
            HeroArchetypeRow hero = _spec != null ? _spec.Hero : null;
            if (hero != null && System.Enum.TryParse(hero.heroId, out EHero parsed) && System.Enum.IsDefined(typeof(EHero), parsed))
                return parsed;
            return EHero.Knight;
        }

        //# 전투 HUD 제목 — 챕터: "{영웅} · {n}단계[ · 난이도]", 탑: "{영웅} · 탑 {N}층[ · 보스층]" (infinite-tower §6.5).
        private string ComposeBattleTitle(MetaProfile profile)
        {
            string heroName = HeroTitleName(profile);
            //# 데일리 "데일리 · 궁수" — 난이도 고정이라 접미 없음 (daily-challenge §9.3).
            if (_spec.IsDaily)
                return string.Format(LookupFormat(DailyStrings.ModeTitle, "데일리 · {0}"), heroName);
            if (_spec.IsTower)
                return BattleViewModel.ComposeTowerTitle(heroName, _spec.Floor.Floor, _spec.Floor.IsBoss, LookupText);
            string difficultyLabel = _effectiveDifficulty >= 1 ? DifficultyTable.LabelOf(_effectiveDifficulty) : string.Empty;
            return BattleViewModel.ComposeHeroTitle(heroName, _spec.StageIndex, difficultyLabel);
        }

        private static string LookupText(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        private static string LookupFormat(int id, string fallback)
        {
            string text = LookupText(id);
            return string.IsNullOrEmpty(text) ? fallback : text;
        }

        //# 전투 HUD 영웅명 — 챕터 영웅 직업 데이터(문구 id)를 우선, 없으면 프로필 값 폴백. 단계는 호출부가 챕터 내 인덱스를 준다.
        private string HeroTitleName(MetaProfile profile)
        {
            HeroArchetypeRow hero = _spec.Hero;
            if (hero != null && CHText.StringProvider != null)
                return CHText.StringProvider.GetString(hero.nameKey);
            return Lair.Village.VillageViewModel.HeroDisplayName(hero != null ? hero.heroId : profile.SelectedHero);
        }

        //# 풀 사전 워밍 — 에셋이 없으면 건너뛴다(없는 에셋은 사용 지점이 LogError 로 알린다).
        private async Task WarmVisual(EVisual key, int count)
        {
            GameObject fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (fx == null)
            {
                Debug.LogError($"[BattleController] 풀 워밍 FX 로드 실패: {key}");
                return;
            }
            CHMPool.Instance.CreatePool(fx, count);
        }

        //# 스테이지 규칙 → FX 키·워밍 수(chapter-system §3.4 — 덫은 trapCount 만큼 동시 표시, 사슬은 봉인 스포너 수).
        private static bool StageRuleFx(StageRuleData data, out EVisual key, out int count)
        {
            switch (data.RuleId)
            {
                case EStageRule.HolySpring:
                    key = EVisual.StageSpringFx;
                    count = 1;
                    return true;
                case EStageRule.DenseFog:
                    key = EVisual.StageFogFx;
                    count = 1;
                    return true;
                case EStageRule.SealedNest:
                    key = EVisual.StageSealChainFx;
                    count = 2;
                    return true;
                case EStageRule.BossBulwark:
                    key = EVisual.StageBulwarkFx;
                    count = 1;
                    return true;
                case EStageRule.ArrowRain:
                    key = EVisual.StageArrowRainFx;
                    count = 2;
                    return true;
                case EStageRule.HunterTraps:
                    key = EVisual.StageTrapFx;
                    count = Mathf.Max(1, Mathf.RoundToInt(data.Params["trapCount"]));
                    return true;
                default:
                    Debug.LogError($"[BattleController] 워밍 FX 매핑이 없는 스테이지 규칙: {data.RuleId}");
                    key = EVisual.StageTrapFx;
                    count = 0;
                    return false;
            }
        }

        //# Rule 12 — 알려진 풀 대상을 미리 CreatePool 로 비축. 첫 Pop spike 방지.
        private async Task PrewarmPools()
        {
            //# 영웅 1마리 — 선택 스테이지 챕터의 영웅(기사/궁수).
            EHero warmHero = SelectedHeroKey();
            GameObject heroPrefab = await CHMResource.Instance.LoadAsync<GameObject>(warmHero);
            if (heroPrefab != null)
            {
                CHMPool.Instance.CreatePool(heroPrefab, count: 1);
            }

            //# 궁수 전용 FX(화살 궤적은 매 공격, 스킬 FX 는 동시 표시 적음).
            if (warmHero == EHero.Archer)
            {
                await WarmVisual(EVisual.ArcherArrowFx, 4);
                await WarmVisual(EVisual.HeroPiercingShotFx, 2);
                await WarmVisual(EVisual.HeroArrowVolleyFx, 2);
                await WarmVisual(EVisual.HeroEvadeFx, 2);
            }

            //# 암살자 전용 FX(기본 베기는 매 공격, 슬립·스킬 FX 는 동시 표시 적음).
            if (warmHero == EHero.Assassin)
            {
                await WarmVisual(EVisual.AssassinSlashFx, 3);
                await WarmVisual(EVisual.HeroSlipFx, 2);
                await WarmVisual(EVisual.HeroShadowStrikeFx, 2);
                await WarmVisual(EVisual.HeroBladeWhirlFx, 2);
                await WarmVisual(EVisual.HeroSmokeBombFx, 1);
            }

            //# 성기사 전용 FX(망치는 6초 주기 1회 재생, 정화·성역은 동시 1개).
            if (warmHero == EHero.Paladin)
            {
                await WarmVisual(EVisual.HeroJudgmentHammerFx, 2);
                await WarmVisual(EVisual.HeroPurifyingLightFx, 1);
                await WarmVisual(EVisual.HeroSanctuaryFx, 1);
            }

            //# 광전사 전용 FX(범위 베기는 매 공격, 스킬 FX 는 동시 표시 적음, 광란 오라는 영웅 부착 1개).
            if (warmHero == EHero.Berserker)
            {
                await WarmVisual(EVisual.BerserkerSlashFx, 3);
                await WarmVisual(EVisual.HeroCrushingLeapFx, 2);
                await WarmVisual(EVisual.HeroChainPullFx, 2);
                await WarmVisual(EVisual.HeroBloodFrenzyFx, 1);
            }

            if (this == null)
                return;

            //# 유물 뇌운 조각 낙뢰 FX — 보유할 때만(동시 1발이지만 지연 겹침 여유로 count 2, relics §6.5).
            if (BuildRelicBonus().Lightnings.Count > 0)
            {
                await WarmVisual(EVisual.RelicLightningFx, 2);
            }

            if (this == null)
                return;

            //# 이번 스테이지 규칙 FX — 규칙이 없는 스테이지(1-1 등)는 아무것도 추가하지 않는다.
            if (_chapters != null)
            {
                foreach (StageRuleData data in _spec.ResolveRules(_chapters))
                {
                    if (StageRuleFx(data, out EVisual ruleFx, out int ruleFxCount) == false)
                        continue;
                    await WarmVisual(ruleFx, ruleFxCount);
                }
            }

            if (this == null)
                return;

            //# 지속 스폰 — 동시 캡 제거 + 동시 출력 증가(SpawnX 카드) 대비 → 편성된 6몬스터 각 10마리 비축(편성 비활성이면 기준 6종). 종족당 둥지 1곳이라 배수 없음(§0.7).
            Lair.Character.MonsterLook[] warmMonsters = Lair.Character.MonsterLooks.ComputeSlots(_formation, _speciesLevels);
            foreach (Lair.Character.MonsterLook look in warmMonsters)
            {
                GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(look.Monster);
                if (prefab != null)
                {
                    CHMPool.Instance.CreatePool(prefab, count: 10);
                }
            }


            //# TimeStop 실드 FX — 영웅 1명 한정, 카드 중첩 가능성 대비 count 2 (2026-06-05).
            GameObject timeStopShieldFx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.TimeStopShield);
            if (timeStopShieldFx != null)
            {
                CHMPool.Instance.CreatePool(timeStopShieldFx, count: 2);
            }

            //# Fear 스컬 FX — 적용 순간 1회성, 영웅 1명 한정 count 2 (2026-06-05).
            GameObject fearSkullFx = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.FearSkull);
            if (fearSkullFx != null)
            {
                CHMPool.Instance.CreatePool(fearSkullFx, count: 2);
            }

            //# 영웅 스킬 FX (2026-06-04) — 동시 표시 적음. count 4 (궤도 2 + 돌진/노바 순간).
            foreach (EVisual key in new[] { EVisual.HeroDashConeFx, EVisual.HeroOrbitBladeFx, EVisual.HeroNovaFx })
            {
                GameObject fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
                if (fx != null)
                {
                    CHMPool.Instance.CreatePool(fx, count: 4);
                }
            }

            //# 타격 피드백 FX — 동시 표시 상한 없음 → 스웜 최악 추정 floor 40 (기획서 §6).
            GameObject impact = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.HitImpact);
            if (impact != null)
            {
                CHMPool.Instance.CreatePool(impact, count: 40);
            }
            //# 영웅이 몬스터를 때릴 때 전용 CFXR 임팩트 — 영웅용과 동일 워밍 floor.
            GameObject monsterImpact = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.MonsterHitImpact);
            if (monsterImpact != null)
            {
                CHMPool.Instance.CreatePool(monsterImpact, count: 40);
            }
            GameObject popup = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.DamagePopup);
            if (popup != null)
            {
                CHMPool.Instance.CreatePool(popup, count: 40);
            }

            if (this == null)
                return;

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
            if (_model != null && _model.Result != BattleResult.None)
                return;
            //# 카드·유물이 기준 몬스터로 소환하면 그 종족에 편성된 등급 몬스터로 나온다(풀 워밍 대상과 같은 프리팹).
            key = DeployedOf(key);
            //# v0.2 메타 — 도감 등장 종 기록 (카드 소환 경로 포함).
            _seenMonsters.Add(MonsterGrades.BaseOf(key));

            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null)
                return;
            //# await 후 종료 재검사 — await 동안 전투가 끝났으면 스폰 중단.
            if (_model != null && _model.Result != BattleResult.None)
                return;
            CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null)
                return;

            Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.5f;
            offset.y = 0f;
            p.transform.position = nearHero + offset;
            //# 지속 스폰 — 카드 소환 몬스터도 글로벌 타입 모디파이어 적용 (신규 Pop → resetCurrent:true)
            ApplyMonsterStats(p.gameObject, key, resetCurrent: true);
        }

        //# 그 종족으로 이번 판 출전 중인 몬스터 — 편성 비활성·미편성이면 요청 값 그대로.
        private EMonster DeployedOf(EMonster monster)
        {
            if (_formation == null)
                return monster;
            foreach (EMonster? deployed in _formation)
            {
                if (deployed.HasValue && MonsterGrades.SameSpecies(deployed.Value, monster))
                    return deployed.Value;
            }
            return monster;
        }

#if UNITY_EDITOR
        //# ===== Slice C-M4 디버그 API — LairBalanceWindow 전용 =====

        //# 시뮬레이션 전용 — 비-null 이면 카드 팝업 대신 호출되어 즉시 픽 결정.
        //# 인자: 제시된 3장, 트리거 출처. 반환: 고른 카드(null 이면 스킵).
        public System.Func<System.Collections.Generic.IReadOnlyList<CardData>, TriggerQueue.Source, CardData> DebugAutoPicker;

        //# 패시브 카드 선택을 즉시 큐에 넣음.
        public void DebugForcePassiveTrigger()
        {
            if (_queue == null)
                return;
            _queue.Enqueue(TriggerQueue.Source.Passive, 0);
            TryProcessNext();
        }

        //# 액티브 카드 선택을 즉시 큐에 넣음.
        public void DebugForceActiveTrigger()
        {
            if (_queue == null)
                return;
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
            if (_heroHealth == null)
                return;
            int delta = hp - _heroHealth.Current;
            if (delta < 0)
            {
                _heroHealth.TakeDamage(-delta);
            }
            else if (delta > 0)
            {
                _heroHealth.Heal(delta);
            }
        }

        //# 영웅 즉사.
        public void DebugKillHero()
        {
            if (_heroHealth != null)
            {
                _heroHealth.TakeDamage(_heroHealth.Current);
            }
        }

        //# 전투 즉시 종료.
        public void DebugEndBattle(BattleResult result)
        {
            if (_model == null || _clock == null)
                return;
            EndBattle(result);
        }
#endif
    }
}
