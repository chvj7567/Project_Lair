using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
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
        [SerializeField] private Transform _heroAnchor;    //# 중앙 해골 배치 지점 (씬 정적 배치)

        private VillageViewModel _vm;

        private async void Start()
        {
            //# 씬 진입점 BGM 전환 일원화 — 이전 씬 BGM(전투 Bgm 등) 정지 후 마을 루프 BGM 0초부터 재생.
            CHMSound.Instance.StopAllBGM();
            CHMSound.Instance.Play(EAudio.VillageBgm);

            MetaProfile profile = MetaSession.GetOrLoad();
            _vm = new VillageViewModel(profile, _metaConfig);

            //# 클라우드 연동 보장(best-effort) — 실패해도 마을은 정상 동작(기획서 §6 무음).
            await MetaSession.EnsureNetworkAsync();

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

                case EUI.RankingPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.RankingPopup, new RankingPopupArg
                    {
                        Ranking = MetaSession.Ranking,
                        MyAccountId = AuthTokenStore.AccountId,
                        MyBestClearTime = profile.BestClearTime,
                    });
                    break;

                case EUI.CloudPopup:
                    await OpenCloud(profile);
                    break;
            }
        }

        //# 클라우드 팝업 — 연결상태/표시명/복원/충돌 권유를 콜백으로 주입(기획서 §5).
        private async Task OpenCloud(MetaProfile profile)
        {
            //# 충돌 권유는 세션당 1회만 노출(기획서 §3) — 게이트 판정·플래그 set 은 MetaSession 으로 추출.
            bool showConflict = MetaSession.TryConsumeConflictPrompt();

            await CHMUI.Instance.ShowUIAsync(EUI.CloudPopup, new CloudPopupArg
            {
                IsConnected = MetaSession.IsCloudConnected,
                DisplayName = profile.DisplayName,
                ConflictPending = showConflict,
                OnRestore = RestoreFromCloud,
                OnChangeName = ChangeDisplayName,
                OnConflictRestore = RestoreFromCloud,
                OnConflictLater = () => { },   //# 로컬 유지·배지 유지(기획서 §3)
            });
        }

        //# 표시명 변경(기획서 §1) — 1~12자(trim 후), 빈값이면 거부 토스트.
        private void ChangeDisplayName(string name)
        {
            string trimmed = name != null ? name.Trim() : string.Empty;
            if (string.IsNullOrEmpty(trimmed))
            {
                ToastView.Show("표시명을 입력해 주세요.");
                return;
            }
            if (trimmed.Length > 12)
                trimmed = trimmed.Substring(0, 12);

            MetaProfile profile = MetaSession.GetOrLoad();
            profile.DisplayName = trimmed;
            HandleProfileChanged();
            ToastView.Show("표시명을 변경했습니다.");
        }

        //# 수동 복원(기획서 §2) — 먼저 GET /save 존재 확인 → 없으면 토스트만, 있으면 확인 다이얼로그 후 덮어쓰기.
        private async void RestoreFromCloud()
        {
            if (MetaSession.Cloud == null)
            {
                ToastView.Show("오프라인 상태입니다. 클라우드 기능을 사용할 수 없습니다.");
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
            profile.CopyFrom(cloud);
            MetaSession.Store?.Save(profile);
            //# 복원 성공 — 충돌 해소.
            MetaSession.CloudConflictPending = false;
            _vm?.NotifyProfileChanged();
            ToastView.Show("클라우드에서 복원했습니다.");
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
