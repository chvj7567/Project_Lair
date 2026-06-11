using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
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
        [SerializeField] private Transform _heroAnchor;    //# 중앙 해골 배치 지점 (씬 정적 배치)

        private VillageViewModel _vm;

        private async void Start()
        {
            //# 씬 진입점 BGM 전환 일원화 — 이전 씬 BGM(전투 Bgm 등) 정지 후 마을 루프 BGM 0초부터 재생.
            CHMSound.Instance.StopAllBGM();
            CHMSound.Instance.Play(EAudio.VillageBgm);

            MetaProfile profile = MetaSession.GetOrLoad();
            _vm = new VillageViewModel(profile, _metaConfig);

            await SpawnIdleHero();

            UIBase hud = await CHMUI.Instance.ShowUIAsync(EUI.VillageHud,
                new VillageHudArg { Vm = _vm, OnOpenMenu = OpenMenu, OnSortie = Sortie });
            if (hud == null)
            {
                Debug.LogError("[VillageController] VillageHud 표시 실패(프리팹 로드/캔버스 확보 불가)");
            }
        }

        //# 중앙 쇼케이스 — 선택 영웅 프리팹을 CHMPool 로 배치, 전투 컴포넌트 비활성 (기획서 §8.1).
        private async Task SpawnIdleHero()
        {
            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (prefab == null)
            {
                Debug.LogError("[VillageController] Knight 프리팹 로드 실패 — 쇼케이스 없이 진행");
                return;
            }

            CHPoolable poolable = CHMPool.Instance.Pop(prefab, transform);
            if (poolable == null)
                return;

            Vector3 pos = _heroAnchor != null ? _heroAnchor.position : Vector3.zero;
            poolable.transform.position = pos;
            //# Y 180° — 카메라 정면 응시 (기획서 §8.1).
            poolable.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            DisableBattleComponents(poolable.gameObject);
        }

        //# 전투 전용 컴포넌트만 끈다 — Animator/CharacterAnimationDriver 는 유지 (idle 루프).
        //# Pop 직후 1회 GetComponent — Rule 02 §5 의 Awake 1회 캐싱과 동등한 일회성 경로.
        private void DisableBattleComponents(GameObject hero)
        {
            if (hero == null)
                return;
            foreach (AutoCombatAI ai in hero.GetComponentsInChildren<AutoCombatAI>())
            {
                if (ai != null)
                {
                    ai.enabled = false;
                }
            }
            ToggleComponent<HeroSkillRunner>(hero, false);
            ToggleComponent<MeleeAttacker>(hero, false);
            ToggleComponent<SimpleMover>(hero, false);
            ToggleComponent<SimpleRotator>(hero, false);
            ToggleComponent<HeroEntryDriver>(hero, false);
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
                    });
                    break;

                case EUI.HeroSelectPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.HeroSelectPopup, new HeroSelectPopupArg
                    {
                        Profile = profile,
                        Config = _metaConfig,
                        OnSelected = HandleHeroSelected,
                    });
                    break;

                case EUI.LordLevelPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.LordLevelPopup, new LordLevelPopupArg
                    {
                        Profile = profile,
                        Config = _metaConfig,
                    });
                    break;
            }
        }

        //# 도감 — 카드 풀 2종을 로드해 전체 카드 목록을 Arg 로 전달 (조우/픽 판정은 프로필).
        private async Task OpenCodex(MetaProfile profile)
        {
            List<CardData> cards = new List<CardData>();
            CardPool passive = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (passive != null)
                cards.AddRange(passive.Cards);
            CardPool active = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (active != null)
                cards.AddRange(active.Cards);

            await CHMUI.Instance.ShowUIAsync(EUI.CodexPopup, new CodexPopupArg
            {
                Profile = profile,
                Config = _metaConfig,
                AllCards = cards,
            });
        }

        //# 상점 구매 등 프로필 변경 시 — 즉시 저장 (spec §5.7) + 상단바 갱신.
        private void HandleProfileChanged()
        {
            MetaSession.Store?.Save(MetaSession.Profile);
            _vm?.NotifyProfileChanged();
        }

        private void HandleHeroSelected(EHero hero)
        {
            MetaProfile profile = MetaSession.GetOrLoad();
            profile.SelectedHero = hero.ToString();
            HandleProfileChanged();
            //# v0.2 는 Knight 1종 — 중앙 모델 교체는 사실상 표시 유지 (spec §3).
        }

        private void Sortie()
        {
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
