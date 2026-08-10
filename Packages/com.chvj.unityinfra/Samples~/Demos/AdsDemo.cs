#if UNITY_INFRA_ADS
using ChvjUnityInfra;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMAdmob 데모.
    /// 사전 준비:
    /// 1. Tools > ChvjUnityInfra > Settings > Ads 탭 → Use Admob ✓
    /// 2. 같은 탭에서 AdConfig 에셋 편집 → 광고 ID 입력 (또는 UseTestAds 체크)
    /// 3. Google Mobile Ads Unity Plugin 임포트 필요
    ///
    /// 안전장치:
    /// - 에디터 빌드는 항상 테스트 광고
    /// - 프로덕션 ID 비어있으면 디바이스 빌드에서도 테스트 광고 fallback
    /// </summary>
    public class AdsDemo : MonoBehaviour
    {
        private void Start()
        {
            // CHMAdmob.Init은 통합 SDK에서 자동 호출됨 (ChvjUnityInfraSDK.Initialize)
            // 여기서는 명시적 Init 시연
            CHMAdmob.Instance.Init();

            // 리워드 콜백 등록
            CHMAdmob.Instance.AcquireReward = OnRewardEarned;
            CHMAdmob.Instance.CloseAD = () => Debug.Log("[AdsDemo] Ad closed");
        }

        private void OnRewardEarned()
        {
            Debug.Log("[AdsDemo] 리워드 획득! 게임 재화 지급");
            // GameState.Instance.gold += 100;
        }

        // 인스펙터에서 버튼 OnClick에 연결하거나 코드에서 직접 호출
        public void OnClickBanner() => CHMAdmob.Instance.ShowBanner(AdPosition.Bottom);
        public void OnClickInterstitial() => CHMAdmob.Instance.ShowInterstitialAd();
        public void OnClickRewarded() => CHMAdmob.Instance.ShowRewardedAd();
    }
}
#endif
