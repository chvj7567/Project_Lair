using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Village
{
    //# Village 씬 진입점 — 프로필 로드 → 해골 idle 배치 → VillageHud 표시 → 메뉴/출격 위임.
    //# Loading 을 거쳐 진입하므로 CHMResource/CHMUI/CHMPool Init 은 완료 상태 (Battle 씬과 동일 정책).
    public class VillageController : MonoBehaviour
    {
        [SerializeField] private MetaConfig _metaConfig;
        //# 탑 팝업 능력치 줄용 영웅 앵커(HP/Power) — 미할당이면 능력치 줄만 숨긴다.
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private Transform _heroAnchor;    //# 중앙 해골 배치 지점 (씬 정적 배치)
        //# hero-stage-variant v2 캐러셀 — 쇼케이스 영웅 재스킨 정본 SO (씬 인스펙터 할당). null 이면 재스킨 skip.
        [SerializeField] private HeroStageVariantConfig _stageVariantConfig;

        //# resolution-adaptation §3.4 — HUD 는 HudRoot, 이후 팝업·토스트는 PopupRoot(둘 다 Safe Area). 둘 다 null 이면 기존 동작.
        [SerializeField] private RectTransform _hudRoot;
        [SerializeField] private RectTransform _popupRoot;

        private VillageViewModel _vm;
        //# 캐러셀 — 중앙 쇼케이스 영웅의 외형 적용기(Pop 직후 1회 캐싱). 스테이지 이동 시 재적용.
        private HeroStageVariantApplier _showcaseApplier;
        //# 쇼케이스 영웅 인스턴스/종류 — 챕터가 바뀌면(기사↔궁수) 풀에 반환하고 새로 배치한다.
        private CHPoolable _showcase;
        private EHero _showcaseHero;
        private bool _showcaseSwapping;
        private bool _showcaseSwapPending;
        private bool _sortieBusy;   //# 출격 팝업 로드 중 재진입 가드
        private bool _towerEntering;   //# 탑 도전 연타 가드(씬 전환은 다음 프레임 적용)
        private bool _dailyEntering;   //# 데일리 시작 연타 가드
        private DailyViewModel _dailyVm;

        private async void Start()
        {
            //# HUD 이전 ToastView.Show(소급 정산 등)도 PopupRoot 로 뜨게 맨 앞에서 전환 — OnSceneLoaded 가 루트를 리셋하므로 진입마다 호출.
            if (_popupRoot != null)
            {
                CHMUI.Instance.SetRoot(_popupRoot);
            }

            //# 씬 진입점 BGM 전환 일원화 — 이전 씬 BGM(전투 Bgm 등) 정지 후 마을 루프 BGM 0초부터 재생.
            CHMSound.Instance.StopAllBGM();
            CHMSound.Instance.Play(EAudio.VillageBgm);

            //# 마을 진입 = 챕터 모드로 복귀 (infinite-tower §6.3) — 탑에서 "마을로" 돌아온 뒤 챕터 출격이 탑으로 열리는 것을 막는다.
            MetaSession.BattleMode = EBattleMode.Chapter;
            //# 데일리 컨텍스트도 마을에서 끝난다 — 다음 데일리는 팝업에서 다시 판정·확정한다(§10.1). 세션 서버 시각(DailyClock)은 유지.
            MetaSession.DailyContext = null;
            MetaProfile profile = MetaSession.GetOrLoad();
            _vm = new VillageViewModel(profile, _metaConfig, ChapterDatabase.Current, LookupString);

            //# 진입 초기값(최대 플레이 가능)을 profile.SelectedStage 에 반영·저장 — 출격/전적(BattleController)이 읽는 값과 동기.
            //# 캐러셀 미이동 상태로 즉시 출격해도 엉뚱한 스테이지 입장 방지(HandleStageChanged 의 이동 시 저장과 동일 관례).
            string showcaseHeroId = ShowcaseHeroOf(_vm.SelectedStage).ToString();
            //# 난이도도 같은 지점에서 보정 — 진입 스테이지 기준 해금 범위 밖이면 낮춘다(difficulty-select E-1).
            int clampedDifficulty = _vm.EffectiveDifficulty;
            if (profile.SelectedStage != _vm.SelectedStage || profile.SelectedHero != showcaseHeroId || profile.SelectedDifficulty != clampedDifficulty)
            {
                profile.SelectedDifficulty = clampedDifficulty;
                profile.SelectedStage = _vm.SelectedStage;
                profile.SelectedHero = showcaseHeroId;   //# 전투 HUD/랭킹이 읽는 표시용 영웅도 선택 스테이지 챕터에 맞춘다
                MetaSession.Store?.Save(profile);
            }

            ChapterDatabaseLoader.ValidateGrowthShop(_metaConfig);
            SettleRetroactiveCurrencies(profile);

            //# 클라우드 연동 보장(best-effort) — 실패해도 마을은 정상 동작(기획서 §6 무음).
            await MetaSession.EnsureNetworkAsync();

            await SpawnIdleHero();

            //# 캐러셀 — VM 스테이지 이동 시 쇼케이스 재스킨 + SelectedStage 로컬 저장(클라우드 백업 제외, 기획서 §4.5).
            _vm.OnStageChanged += HandleStageChanged;

            if (_hudRoot != null)
            {
                CHMUI.Instance.SetRoot(_hudRoot);
            }
            UIBase hud = await CHMUI.Instance.ShowUIAsync(EUI.VillageHud,
                new VillageHudArg
                {
                    Vm = _vm,
                    OnOpenMenu = OpenMenu,
                    OnSortie = Sortie,
                    PortraitOf = stage => _stageVariantConfig != null ? _stageVariantConfig.GetStage(stage).Portrait : null,
                });
            //# HUD 이후 모든 팝업/토스트는 PopupRoot.
            if (_popupRoot != null)
            {
                CHMUI.Instance.SetRoot(_popupRoot);
            }
            if (hud == null)
            {
                Debug.LogError("[VillageController] VillageHud 표시 실패(프리팹 로드/캔버스 확보 불가)");
            }
        }

        //# 소급 정산 (monster-growth-currencies §7.2-2) — 업데이트 이전 Normal/Hard·탑 기록분을 재화별 토스트 1회와 함께 지급. 멱등(지급 기록 비교).
        private static void SettleRetroactiveCurrencies(MetaProfile profile)
        {
            GrowthDatabase growth = GrowthDatabase.Current;
            if (growth == null)
                return;
            List<CurrencyGain> gains = CurrencySettlement.SettleRetroactive(profile, growth, TowerDatabase.Current, DifficultyTable.CurrentMaxIndex);
            if (gains.Count == 0)
                return;
            MetaSession.Store?.Save(profile);
            foreach (CurrencyGain gain in gains)
            {
                ToastView.Show(string.Format(LookupString(GrowthStrings.RetroReward), GrowthText.CurrencyName(gain.Currency, growth, LookupString), gain.Amount));
            }
        }

        //# 선택 스테이지가 속한 챕터의 영웅 — 챕터 데이터가 없거나 알 수 없는 값이면 기사.
        private static EHero ShowcaseHeroOf(int linearStage)
        {
            HeroArchetypeRow hero = ChapterDatabase.Current != null ? ChapterDatabase.Current.GetHeroOfLinear(linearStage) : null;
            if (hero != null && System.Enum.TryParse(hero.heroId, out EHero parsed) && System.Enum.IsDefined(typeof(EHero), parsed))
                return parsed;
            return EHero.Knight;
        }

        //# 중앙 쇼케이스 — 선택 스테이지 챕터 영웅 프리팹을 CHMPool 로 배치, 전투 컴포넌트 비활성 (기획서 §8.1).
        private async Task SpawnIdleHero()
        {
            EHero heroKey = ShowcaseHeroOf(_vm != null ? _vm.SelectedStage : 1);
            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(heroKey);
            //# await 중 씬이 파괴됐으면 배치하지 않는다 — 파괴된 transform 으로 Pop 하면 부모 없는 인스턴스가 남는다.
            if (this == null)
                return;
            if (prefab == null)
            {
                Debug.LogError($"[VillageController] {heroKey} 프리팹 로드 실패 — 쇼케이스 없이 진행");
                return;
            }

            CHPoolable poolable = CHMPool.Instance.Pop(prefab, transform);
            if (poolable == null)
                return;
            _showcase = poolable;
            _showcaseHero = heroKey;

            Vector3 pos = _heroAnchor != null ? _heroAnchor.position : Vector3.zero;
            poolable.transform.position = pos;
            //# Y 180° — 카메라 정면 응시 (기획서 §8.1).
            poolable.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            DisableBattleComponents(poolable.gameObject);

            //# 캐러셀 — 쇼케이스 applier 캐싱 후 현재 SelectedStage 외형 즉시 반영(기획서 §4.1). 전투 컴포넌트 off 여도 재스킨은 성립(§6).
            _showcaseApplier = poolable.gameObject.GetComponent<HeroStageVariantApplier>();
            ApplyShowcaseVariant();

            //# 투영진 위 푸른 홀로그램(scene-2d-conversion §5.5) — 조명 안 받는 반투명 머티리얼, 영웅 조명·그림자 숨김
            HeroHologramMode hologram = poolable.gameObject.GetComponent<HeroHologramMode>();
            if (hologram != null)
            {
                hologram.SetHologram(true);
            }
        }

        //# 현재 VM.SelectedStage 의 variant 를 쇼케이스 영웅에 적용. config/applier 미비 시 무동작(가드).
        private void ApplyShowcaseVariant()
        {
            if (_showcaseApplier == null || _stageVariantConfig == null || _vm == null)
                return;
            _showcaseApplier.Apply(_stageVariantConfig.GetStage(_vm.SelectedStage));
        }

        //# 캐러셀 스테이지 이동 — SelectedStage/SelectedHero 로컬 저장(클라우드 백업 제외, 기획서 §4.5) + 쇼케이스 재스킨.
        //# 챕터 영웅이 바뀌면(기사↔궁수) 쇼케이스 영웅을 교체한 뒤 재스킨한다. 같은 영웅이면 기존처럼 동기 재스킨.
        private async void HandleStageChanged()
        {
            if (_vm == null)
                return;
            MetaProfile profile = MetaSession.GetOrLoad();
            profile.SelectedStage = _vm.SelectedStage;
            profile.SelectedHero = ShowcaseHeroOf(_vm.SelectedStage).ToString();
            profile.SelectedDifficulty = _vm.EffectiveDifficulty;
            MetaSession.Store?.Save(profile);

            bool needSwap = _showcase == null || _showcaseHero != ShowcaseHeroOf(_vm.SelectedStage);
            if (needSwap == false)
            {
                ApplyShowcaseVariant();
                return;
            }

            //# 교체 중 또 이동하면 끝난 뒤 한 번 더 맞춘다(중복 배치 방지).
            if (_showcaseSwapping)
            {
                _showcaseSwapPending = true;
                return;
            }
            _showcaseSwapping = true;
            try
            {
                do
                {
                    _showcaseSwapPending = false;
                    await SwapShowcaseHero();
                    if (this == null)
                        return;
                }
                while (_showcaseSwapPending);
            }
            finally
            {
                _showcaseSwapping = false;
            }
        }

        //# 현재 선택 챕터의 영웅과 다르면 기존 쇼케이스를 풀에 반환하고 새 영웅을 배치(SpawnIdleHero 가 variant·홀로그램까지 적용).
        private async Task SwapShowcaseHero()
        {
            if (_vm == null)
                return;
            if (_showcase != null && _showcaseHero == ShowcaseHeroOf(_vm.SelectedStage))
            {
                ApplyShowcaseVariant();
                return;
            }
            ReleaseShowcase();
            await SpawnIdleHero();
        }

        //# 쇼케이스를 영속 풀에 돌려준다 — 끈 전투 컴포넌트를 먼저 복구해 풀에 오염 상태를 남기지 않는다(Rule 03 §4).
        private void ReleaseShowcase()
        {
            if (_showcase == null)
                return;
            SetBattleComponents(_showcase.gameObject, true);
            CHMPool.Instance.Push(_showcase);
            _showcase = null;
            _showcaseApplier = null;
        }

        private void OnDestroy()
        {
            if (_vm != null)
            {
                _vm.OnStageChanged -= HandleStageChanged;
            }
            ReleaseShowcase();
        }

        //# 전투 전용 컴포넌트만 끈다 — Animator/CharacterAnimationDriver 는 유지 (idle 루프).
        //# Pop 직후 1회 GetComponent — Rule 02 §5 의 Awake 1회 캐싱과 동등한 일회성 경로.
        private void DisableBattleComponents(GameObject hero)
        {
            SetBattleComponents(hero, false);
        }

        //# 전투 컴포넌트 일괄 on/off — false 는 쇼케이스 배치, true 는 풀 반환 전 복구.
        private void SetBattleComponents(GameObject hero, bool enabled)
        {
            if (hero == null)
                return;
            foreach (AutoCombatAI ai in hero.GetComponentsInChildren<AutoCombatAI>(true))
            {
                if (ai != null)
                {
                    ai.enabled = enabled;
                }
            }
            ToggleComponent<HeroSkillRunner>(hero, enabled);
            ToggleComponent<MeleeAttacker>(hero, enabled);
            ToggleComponent<SimpleMover>(hero, enabled);
            ToggleComponent<SimpleRotator>(hero, enabled);
            ToggleComponent<HeroEntryDriver>(hero, enabled);
        }

        private static void ToggleComponent<T>(GameObject go, bool enabled) where T : Behaviour
        {
            T component = go.GetComponent<T>();
            if (component != null)
            {
                component.enabled = enabled;
            }
        }

        //# 메뉴 6종 — 팝업별 Arg 구성 후 CHMUI 표시. 프로필 변경 콜백은 저장 + VM 통지로 일원화.
        private async void OpenMenu(EUI ui)
        {
            if (_metaConfig == null)
            {
                Debug.LogError("[VillageController] MetaConfig 미할당 — 메뉴 열기 불가");
                return;
            }

            MetaProfile profile = MetaSession.GetOrLoad();
            switch (ui)
            {
                case EUI.ShopPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.ShopPopup, new ShopPopupArg
                    {
                        Shop = new ShopService(profile, _metaConfig),
                        Profile = profile,
                        Config = _metaConfig,
                        OnPurchased = HandleProfileChanged,
                    });
                    break;

                case EUI.QuestPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.QuestPopup, new QuestPopupArg
                    {
                        Profile = profile,
                        Config = _metaConfig,
                    });
                    break;

                case EUI.CodexPopup:
                    await OpenCodex(profile);
                    break;

                case EUI.RecordsPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.RecordsPopup, new RecordsPopupArg
                    {
                        Profile = profile,
                        VariantConfig = _stageVariantConfig,
                        AllCards = await LoadAllCardsAsync(),
                    });
                    break;

                case EUI.WorldMapPopup:
                    await OpenWorldMap(profile);
                    break;

                case EUI.HeroSelectPopup:
                    //# (구) 표시 전용 — 월드맵으로 대체됐고 HUD 는 더 이상 열지 않는다. 호환용으로 남김.
                    //# 표시 전용 — 스테이지 1~5 영웅 외형 나열. 실제 스테이지 선택은 마을 캐러셀 담당(기획서 §4).
                    await CHMUI.Instance.ShowUIAsync(EUI.HeroSelectPopup, new HeroSelectPopupArg
                    {
                        Profile = profile,
                        VariantConfig = _stageVariantConfig,
                    });
                    break;

                case EUI.LordLevelPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.LordLevelPopup, new LordLevelPopupArg
                    {
                        Profile = profile,
                        Config = _metaConfig,
                    });
                    break;

                case EUI.RankingPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.RankingPopup, new RankingPopupArg
                    {
                        Ranking = MetaSession.Ranking,
                        MyUid = AuthTokenStore.Uid,
                        MyBestClearTime = profile.BestClearTime,
                        //# 미등재 표기용 닉네임 — 상단 HUD 와 같은 해석 경로(VM) 재사용, 두 곳이 다른 이름을 보이면 안 된다.
                        MyDisplayName = _vm.DisplayName,
                        //# 스테이지 탭 — 현재 캐러셀 스테이지에서 열고, 내 행 시간 fallback 은 스테이지 전적 최단.
                        InitialStage = _vm.SelectedStage,
                        MyStageBestClearTime = stage => profile.GetStageRecord(stage).BestClearTime,
                    });
                    break;

                case EUI.CloudPopup:
                    await OpenCloud(profile);
                    break;

                case EUI.TowerPopup:
                    await OpenTower(profile);
                    break;

                case EUI.DailyPopup:
                    await OpenDaily(profile);
                    break;

                case EUI.RelicPopup:
                    //# 유물 도감 — 팝업을 연 동안 보유는 변하지 않는다(전투 불가 상태). 닫고 다시 열면 재계산(relics E-11).
                    await CHMUI.Instance.ShowUIAsync(EUI.RelicPopup, new RelicPopupArg { Profile = profile });
                    break;
            }
        }

        //# 무한의 탑 (infinite-tower §6) — 데이터 없음 = "준비 중", 잠김 = 해금 스테이지 안내 토스트, 열림 = 팝업. 시즌이 바뀌었으면 이전 시즌 진행을 지우고 저장한다 (§4.6).
        private async Task OpenTower(MetaProfile profile)
        {
            TowerDatabase tower = TowerDatabase.Current;
            ChapterDatabase chapters = ChapterDatabase.Current;
            if (tower == null || chapters == null)
            {
                ToastView.Show(LookupString(TowerStrings.NotReady), EToastKind.Warning);
                return;
            }
            if (tower.IsUnlocked(profile.ClearedStage) == false)
            {
                string needed = StageLabels.Number(tower.Config.unlockClearedStage);
                ToastView.Show(string.Format(LookupString(TowerStrings.LockedToast), needed), EToastKind.Warning);
                return;
            }
            if (profile.NormalizeTowerSeason(tower.Config.seasonId))
            {
                MetaSession.Store?.Save(profile);
            }

            int anchorHp = 0;
            int anchorPower = 0;
            if (_balance != null && _balance.Hero != null)
            {
                anchorHp = _balance.Hero.Hp;
                anchorPower = _balance.Hero.Power;
            }
            TowerViewModel towerVm = new TowerViewModel(tower, chapters, profile, LookupString, RelicDatabase.Current, anchorHp, anchorPower);
            await CHMUI.Instance.ShowUIAsync(EUI.TowerPopup, new TowerPopupArg
            {
                ViewModel = towerVm,
                OnChallenge = EnterTower,
            });
        }

        //# 탑 도전 — 팝업에서 바로 출격 (D-5). 편성은 현재 저장된 편성을 그대로 쓰고(Battle 이 프로필에서 읽는다), SelectedStage/SelectedDifficulty 는 건드리지 않는다.
        private void EnterTower()
        {
            if (_towerEntering)
                return;
            _towerEntering = true;
            MetaSession.BattleMode = EBattleMode.Tower;
            EnterBattle();
        }

        //# 데일리 (daily-challenge §9) — 데이터 없음 = 타일 숨김(여기 도달 시 "준비 중"), 잠김 = 해금 스테이지 토스트, 열림 = 팝업 + 서버 시각·순위 비동기 로드.
        private async Task OpenDaily(MetaProfile profile)
        {
            DailyDatabase daily = DailyDatabase.Current;
            if (daily == null || ChapterDatabase.Current == null)
            {
                ToastView.Show(LookupString(TowerStrings.NotReady), EToastKind.Warning);
                return;
            }
            if (daily.IsUnlocked(profile.ClearedStage) == false)
            {
                string needed = StageLabels.Number(daily.Config.unlockClearedStage);
                ToastView.Show(string.Format(LookupString(DailyStrings.LockedToast), needed), EToastKind.Warning);
                return;
            }

            _dailyVm = new DailyViewModel(daily, ChapterDatabase.Current, RelicDatabase.Current, profile, MetaSession.Api,
                LookupString, () => Time.realtimeSinceStartupAsDouble, AuthTokenStore.Uid, _vm.DisplayName);
            DailyViewModel vm = _dailyVm;
            UIBase popup = await CHMUI.Instance.ShowUIAsync(EUI.DailyPopup, new DailyPopupArg
            {
                ViewModel = vm,
                OnStart = StartDaily,
                PortraitOf = linear => _stageVariantConfig != null ? _stageVariantConfig.GetStage(linear).Portrait : null,
            });
            if (popup == null)
            {
                Debug.LogError("[VillageController] DailyPopup 표시 실패(프리팹 로드/캔버스 확보 불가)");
                return;
            }
            await vm.LoadAsync();
            //# 서버 시각을 받았으면 세션 시각을 갱신 — 타일 강조(오늘 남은 도전)와 결과창 재도전 판정이 같은 기준을 쓴다.
            if (this != null && vm.Clock != null)
            {
                MetaSession.DailyClock = vm.Clock;
                _vm?.NotifyProfileChanged();
            }
        }

        //# 데일리 시작 (§2.2-3) — 시작 순간 날짜 재판정(0시 경과면 새 조합 + 토스트, 시작 안 함). 차감은 Battle 이 영웅 스폰 직후 한다.
        //# 편성은 저장된 편성 그대로(Battle 이 프로필에서 읽는다), SelectedStage/SelectedDifficulty 는 건드리지 않는다(E-14).
        private void StartDaily()
        {
            if (_dailyEntering || _dailyVm == null)
                return;
            EDailyStartCheck check = _dailyVm.TryStart();
            switch (check)
            {
                case EDailyStartCheck.Ok:
                    _dailyEntering = true;
                    MetaSession.DailyClock = _dailyVm.Clock;
                    MetaSession.DailyContext = _dailyVm.Challenge;
                    MetaSession.BattleMode = EBattleMode.Daily;
                    EnterBattle();
                    return;
                case EDailyStartCheck.DayChanged:
                    ToastView.Show(LookupString(DailyStrings.DayChanged), EToastKind.Warning);
                    return;
                case EDailyStartCheck.Exhausted:
                    ToastView.Show(LookupString(DailyStrings.Exhausted), EToastKind.Warning);
                    return;
                default:
                    ToastView.Show(LookupString(DailyStrings.OnlineRequired), EToastKind.Warning);
                    return;
            }
        }

        //# 침입 지도 — 챕터 데이터가 없으면 안내만(조용히 열지 않음). 선택 확정은 VM 이 선형 번호를 돌려주면 캐러셀 VM 으로 반영한다.
        private async Task OpenWorldMap(MetaProfile profile)
        {
            ChapterDatabase chapters = ChapterDatabase.Current;
            if (chapters == null)
            {
                Debug.LogError("[VillageController] 챕터 데이터 미로드 — 월드맵을 열 수 없음");
                ToastView.Show(LookupString(WorldMapViewModel.StrNotReady), EToastKind.Error);
                return;
            }

            //# 확정 → 캐러셀 이동(OnStageChanged 구독이 SelectedStage 로컬 저장 + 쇼케이스 재스킨, 클라우드 백업 호출 없음).
            WorldMapViewModel worldMapVm = new WorldMapViewModel(chapters, profile, LookupString, linear =>
            {
                //# 난이도는 WorldMapViewModel.Confirm 이 profile.SelectedDifficulty 에 기록해 둔다 — 스테이지 불변이면 OnStageChanged 가 없으므로 여기서 저장·표기 갱신.
                _vm?.SelectStage(linear);
                MetaSession.Store?.Save(profile);
                _vm?.NotifyProfileChanged();
            });
            await CHMUI.Instance.ShowUIAsync(EUI.WorldMapPopup, new WorldMapPopupArg
            {
                ViewModel = worldMapVm,
                PortraitOf = linear => _stageVariantConfig != null ? _stageVariantConfig.GetStage(linear).Portrait : null,
            });
        }

        //# Strings_Ko 조회 — CHText 와 같은 테이블. 미등록이면 빈 문자열.
        private static string LookupString(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        //# 클라우드 팝업 — 연결상태/표시명/복원/충돌 권유를 콜백으로 주입(기획서 §5).
        private async Task OpenCloud(MetaProfile profile)
        {
            //# 충돌 권유는 세션당 1회만 노출(기획서 §3) — 게이트 판정·플래그 set 은 MetaSession 으로 추출.
            bool showConflict = MetaSession.TryConsumeConflictPrompt();

            //# 충돌 비교 칸 — 충돌 감지 시점엔 서버 프로필을 받아 두지 않으므로(PutSave 트랜잭션은 버전만 본다) 권유를 띄울 때 1회 읽는다.
            SaveSummary localSummary = null;
            SaveSummary cloudSummary = null;
            if (showConflict && MetaSession.Cloud != null)
            {
                MetaProfile cloud = await MetaSession.Cloud.RestoreAsync();
                localSummary = SaveSummary.From(profile, _metaConfig);
                cloudSummary = SaveSummary.From(cloud, _metaConfig);
            }

            await CHMUI.Instance.ShowUIAsync(EUI.CloudPopup, new CloudPopupArg
            {
                IsConnected = MetaSession.IsCloudConnected,
                DisplayName = _vm.DisplayName,   //# 상단바와 같은 해석 경로(미설정 시 기본명)
                ConflictPending = showConflict,
                OnRestore = RestoreFromCloud,
                OnChangeName = ChangeDisplayName,
                OnConflictRestore = RestoreFromCloud,
                OnConflictLater = () => { },   //# 로컬 유지·배지 유지(기획서 §3)
                LocalSummary = localSummary,
                CloudSummary = cloudSummary,
            });
        }

        //# 표시명 변경(기획서 §1) — 1차 로컬 가드(NormalizeDisplayName)로 빈값 거부 후, 서버 권위 중복 체크 호출.
        //# 200 만 로컬 DisplayName 반영 + 성공 토스트. 409/400/오프라인은 실패 통지(라벨 불변·편집창 유지).
        private async Task<DisplayNameResult> ChangeDisplayName(string name)
        {
            //# 1차 가드 — 빈/공백은 왕복 없이 거부(서버 400 과 동일 UX).
            string normalized = MetaProfile.NormalizeDisplayName(name);
            if (string.IsNullOrEmpty(normalized))
            {
                ToastView.Show("표시명을 확인해 주세요.", EToastKind.Warning);
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            }

            //# 오프라인 단축 — Api 미구성이면 doomed 요청 없이 즉시 오프라인 통지(RestoreFromCloud 와 동일 가드 패턴).
            if (MetaSession.Api == null)
            {
                ToastView.Show("오프라인 상태입니다. 잠시 후 다시 시도해 주세요.", EToastKind.Error);
                return DisplayNameResult.Of(DisplayNameStatus.Offline);
            }

            DisplayNameResult result = await MetaSession.Api.ChangeDisplayNameAsync(normalized);
            switch (result.Status)
            {
                case DisplayNameStatus.Success:
                    MetaProfile profile = MetaSession.GetOrLoad();
                    //# 서버가 돌려준 정규화 이름을 권위값으로 반영(로컬 normalized 가 아님).
                    profile.DisplayName = result.Name;
                    HandleProfileChanged();
                    ToastView.Show("표시명을 변경했습니다.");
                    break;
                case DisplayNameStatus.Taken:
                    ToastView.Show("이미 사용 중인 이름입니다.", EToastKind.Warning);
                    break;
                case DisplayNameStatus.Invalid:
                    ToastView.Show("표시명을 확인해 주세요.", EToastKind.Warning);
                    break;
                default:
                    ToastView.Show("오프라인 상태입니다. 잠시 후 다시 시도해 주세요.", EToastKind.Error);
                    break;
            }
            return result;
        }

        //# 수동 복원(기획서 §2) — 먼저 GET /save 존재 확인 → 없으면 토스트만, 있으면 확인 다이얼로그 후 덮어쓰기.
        private async void RestoreFromCloud()
        {
            if (MetaSession.Cloud == null)
            {
                ToastView.Show("오프라인 상태입니다. 클라우드 기능을 사용할 수 없습니다.", EToastKind.Error);
                return;
            }

            MetaProfile cloud = await MetaSession.Cloud.RestoreAsync();
            if (cloud == null)
            {
                //# 헛경고 방지 — 데이터 없으면 확인 다이얼로그 생략(기획서 §2·§7).
                ToastView.Show("클라우드에 저장된 데이터가 없습니다.");
                return;
            }

            await CHMUI.Instance.ShowUIAsync(EUI.ConfirmPopup, new ConfirmPopupArg
            {
                Title = "클라우드에서 복원",
                Message = "클라우드 저장 데이터로 현재 진행을 덮어씁니다. 지금 기기의 진행이 사라질 수 있습니다. 복원할까요?",
                ConfirmLabel = "복원",
                CancelLabel = "취소",
                OnConfirm = () => ApplyRestoredProfile(cloud),
            });
        }

        //# 복원 적용 — 참조 유지 위해 in-place 복사(VM/HUD 가 보던 객체 그대로) + 저장 + VM 갱신.
        private void ApplyRestoredProfile(MetaProfile cloud)
        {
            MetaProfile profile = MetaSession.GetOrLoad();
            //# 데일리 — 복원 전 로컬 값을 떠 두고 복원 뒤 날짜별 병합(도전 수 최댓값·점수 최솟값·연속 최댓값) — 오래된 클라우드로 도전 횟수 환불 방지(daily-challenge §5.2).
            List<DailyDayEntry> localDailyDays = MetaProfile.CopyDailyDays(profile.DailyDays);
            List<DailyResultEntry> localDailyResults = MetaProfile.CopyDailyResults(profile.DailyResults);
            int localDailyStreak = profile.DailyStreak;
            profile.CopyFrom(cloud);
            //# v4 클라우드 문서를 v5 클라이언트가 복원하는 경우 대비 — 로드 경로와 같은 마이그레이션 재적용(chapter-system §2.2).
            MetaProfileMigration.Apply(profile, ChapterDatabase.Current);
            profile.MergeDailyFrom(localDailyDays, localDailyResults, localDailyStreak);
            MetaProfileMigration.NormalizeDaily(profile);
            MetaSession.Store?.Save(profile);
            //# 복원 성공 — 충돌 해소.
            MetaSession.CloudConflictPending = false;
            _vm?.NotifyProfileChanged();
            ToastView.Show("클라우드에서 복원했습니다.");
        }

        //# 도감 — 카드 풀 2종을 로드해 전체 카드 목록을 Arg 로 전달 (조우/픽 판정은 프로필).
        private async Task OpenCodex(MetaProfile profile)
        {
            List<CardData> cards = await LoadAllCardsAsync();

            await CHMUI.Instance.ShowUIAsync(EUI.CodexPopup, new CodexPopupArg
            {
                Profile = profile,
                Config = _metaConfig,
                AllCards = cards,
            });
        }

        //# 패시브+액티브 카드 풀 전체 — 도감·기록(가장 많이 픽한 카드 이름)이 공유한다.
        private async Task<List<CardData>> LoadAllCardsAsync()
        {
            List<CardData> cards = new List<CardData>();
            CardPool passive = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (passive != null)
                cards.AddRange(passive.Cards);
            CardPool active = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (active != null)
                cards.AddRange(active.Cards);
            return cards;
        }

        //# 상점 구매 등 프로필 변경 시 — 즉시 저장 (spec §5.7) + 상단바 갱신 + 클라우드 백업(best-effort).
        private void HandleProfileChanged()
        {
            MetaSession.Store?.Save(MetaSession.Profile);
            _vm?.NotifyProfileChanged();
            BackupToCloud();
        }

        //# fire-and-forget 백업 — 실패/충돌은 무음(게임 흐름 차단 금지, 기획서 §6). 409 는 배지 플래그 set(§3).
        private async void BackupToCloud()
        {
            if (MetaSession.Cloud == null)
                return;
            CloudSaveResult result = await MetaSession.Cloud.BackupAsync(MetaSession.Profile);
            if (result == CloudSaveResult.Conflict)
            {
                MetaSession.CloudConflictPending = true;
            }
        }

        //# 출격 — 현재 스테이지가 해금이면 그 스테이지로 Battle 입장(SelectedStage 는 이동 시 이미 저장됨, 기획서 §4.5).
        //# 잠금이면 방어적 차단(출격 버튼 비활성이 1차 가드, §4.5). 편성 해금(1-1 클리어) 후에는 편성 팝업이 먼저 열리고 "출격" 이 저장 + 입장한다(dungeon-formation §6.2).
        private async void Sortie()
        {
            if (_sortieBusy || _vm == null || _vm.IsCurrentStageUnlocked == false)
                return;
            //# 출격 불가 챕터(ChapterGate)는 오류 대신 명시적 안내 — 월드맵·캐러셀이 1차로 막고 여기는 방어 가드.
            if (ChapterGate.IsStagePlayable(_vm.SelectedStage) == false)
            {
                ToastView.Show(LookupString(WorldMapViewModel.StrNotReadyHint), EToastKind.Warning);
                return;
            }

            //# 편성 잠김(1-1 클리어 전)·고를 몬스터가 2개 이상인 종족 없음·편성 데이터 로드 실패 -> 팝업 없이 바로 입장 (dungeon-formation §0.3 U-D A).
            MetaProfile profile = MetaSession.GetOrLoad();
            FormationDatabase formation = FormationDatabase.Current;
            if (FormationRules.ShouldShowPopup(formation, profile.ClearedStage, profile.GetRaceGrade) == false)
            {
                EnterBattle();
                return;
            }

            //# 팝업 로드(await) 중 연타로 중복 표시되지 않게 재진입 가드.
            _sortieBusy = true;
            FormationViewModel formationVm = new FormationViewModel(
                formation, ChapterDatabase.Current, profile, _vm.SelectedStage, LookupString,
                monster => profile.GetShopLevel("Enhance_" + monster));
            UIBase popup = await CHMUI.Instance.ShowUIAsync(EUI.FormationPopup, new FormationPopupArg
            {
                ViewModel = formationVm,
                OnSortie = SortieWithFormation,
            });
            _sortieBusy = false;
            if (popup == null)
            {
                //# 프리팹 로드 실패 시 출격이 막히지 않게 — 저장된 편성 그대로 입장.
                Debug.LogError("[VillageController] FormationPopup 표시 실패 - 저장된 편성으로 바로 출격");
                EnterBattle();
            }
        }

        //# 편성 팝업 확정 — 편성(0행=기본, 6행=사용자) 저장 후 입장. Battle 이 매 전투 프로필에서 다시 읽는다(다시 도전도 같은 편성).
        private void SortieWithFormation(List<FormationSlotEntry> rows)
        {
            MetaProfile profile = MetaSession.GetOrLoad();
            profile.Formation = rows ?? new List<FormationSlotEntry>();
            MetaSession.Store?.Save(profile);
            EnterBattle();
        }

        private static void EnterBattle()
        {
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
