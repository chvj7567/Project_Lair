using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// AdMob 광고 매니저. Init에서 AdConfig로 광고 단위 ID 주입.
    /// UNITY_EDITOR 빌드에서는 자동으로 테스트 ID 사용.
    /// </summary>
    public class CHMAdmob : CHSingletonStatic<CHMAdmob>
    {
        private string _bannerAdUnitId;
        private string _interstitialAdUnitId;
        private string _rewardedAdUnitId;

        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;

        private AdRequest _adRequest;

        private bool _initialize = false;

        /// <summary>보상형 광고 시청 완료 시 발생.</summary>
        public event Action AcquireReward;
        /// <summary>전면/보상형 광고가 닫혔을 때 발생.</summary>
        public event Action CloseAD;

        public void Init()
        {
            if (_initialize)
                return;

            _initialize = true;

            var config = Resources.Load<AdConfig>("ChvjUnityInfra/AdConfig");
            if (config == null)
            {
                Debug.LogWarning("[CHMAdmob] AdConfig 에셋을 찾을 수 없습니다. " +
                    "메뉴 Tools/ChvjUnityInfra/Edit Ad Config 로 생성해주세요. 임시로 기본 테스트 광고를 사용합니다.");
                config = ScriptableObject.CreateInstance<AdConfig>();
            }

            var ids = config.ResolveIds();
            _bannerAdUnitId = ids.banner;
            _interstitialAdUnitId = ids.interstitial;
            _rewardedAdUnitId = ids.rewarded;

#if UNITY_EDITOR
            Debug.Log("[CHMAdmob] 에디터 빌드 — 테스트 광고로 동작합니다.");
#else
            if (string.IsNullOrEmpty(config.BannerAdUnitId) || config.UseTestAds)
            {
                Debug.Log("[CHMAdmob] 테스트 광고 모드 (프로덕션 ID 미설정 또는 UseTestAds=true).");
            }
#endif

            // MobileAds 초기화 완료 후 광고 load (SDK 권장 순서).
            MobileAds.Initialize(initStatus =>
            {
                _adRequest = new AdRequest();
                LoadInterstitialAd();
                LoadRewardedAd();
            });
        }

        /// <summary>배너 표시. 기존 배너가 있으면 destroy 후 새로 생성.</summary>
        public void ShowBanner(AdPosition position)
        {
            HideBanner();
            _bannerView = new BannerView(_bannerAdUnitId, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), position);
            _bannerView.LoadAd(_adRequest);
        }

        /// <summary>배너 제거. 호출 안전 (배너 없으면 무동작).</summary>
        public void HideBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        /// <summary>전면 광고 즉시 표시 가능 여부.</summary>
        public bool IsInterstitialReady => _interstitialAd != null && _interstitialAd.CanShowAd();

        /// <summary>보상형 광고 즉시 표시 가능 여부.</summary>
        public bool IsRewardedReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        private void LoadInterstitialAd(bool show = false)
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            InterstitialAd.Load(_interstitialAdUnitId, _adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"[CHMAdmob] Interstitial load failed: {error}");
                        return;
                    }

                    _interstitialAd = ad;
                    RegisterEventHandlers(_interstitialAd);

                    if (show) _interstitialAd.Show();
                });
        }

        public void ShowInterstitialAd()
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.Show();
            }
            else
            {
                LoadInterstitialAd(true);
            }
        }

        private void LoadRewardedAd(bool show = false)
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            RewardedAd.Load(_rewardedAdUnitId, _adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"[CHMAdmob] Rewarded load failed: {error}");
                        return;
                    }

                    _rewardedAd = ad;
                    RegisterEventHandlers(_rewardedAd);

                    if (show) _rewardedAd.Show(RewardHandler);
                });
        }

        public void ShowRewardedAd()
        {
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show(RewardHandler);
            }
            else
            {
                LoadRewardedAd(true);
            }
        }

        private void RewardHandler(Reward reward)
        {
            AcquireReward?.Invoke();
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                CloseAD?.Invoke();
                LoadRewardedAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[CHMAdmob] Rewarded full screen failed: {error}");
            };
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                CloseAD?.Invoke();
                LoadInterstitialAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[CHMAdmob] Interstitial full screen failed: {error}");
            };
        }
    }
}
