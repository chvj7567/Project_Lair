using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// AdMob 광고 단위 ID 설정 ScriptableObject.
    /// 에셋 위치: Assets/Resources/ChvjUnityInfra/AdConfig.asset (Tools/ChvjUnityInfra/Edit Ad Config 메뉴로 자동 생성)
    /// CHMAdmob이 Init 시 Resources.Load로 자동 로드.
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "ChvjUnityInfra/Ad Config", order = 0)]
    public class AdConfig : ScriptableObject
    {
        [Tooltip("프로덕션 ID가 비어있으면 자동으로 테스트 광고 사용. true로 강제 설정도 가능.")]
        public bool UseTestAds = true;

        [Header("Production AdMob IDs")]
        public string BannerAdUnitId;
        public string InterstitialAdUnitId;
        public string RewardedAdUnitId;

        [Header("Test IDs (Google 공식 테스트 광고 ID — 변경 비추천)")]
        public string TestBannerAdUnitId = "ca-app-pub-3940256099942544/9214589741";
        public string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

        [Header("UMP / GDPR Consent")]
        [Tooltip("UMP 동의 폼 테스트용. 비어있지 않으면 ConsentDebugSettings로 EEA 강제 표시. 디바이스 SHA-256 해시.")]
        public string TestDeviceHashedId;

        [Header("Ad Policy (COPPA / 연령 등급)")]
        [Tooltip("아동 대상 처리 태그. 키즈 앱이면 True.")]
        public AdChildTreatment ChildDirectedTreatment = AdChildTreatment.Unspecified;
        [Tooltip("동의 연령 미만 사용자 처리 태그.")]
        public AdChildTreatment UnderAgeOfConsent = AdChildTreatment.Unspecified;
        [Tooltip("최대 광고 콘텐츠 등급. 전체 이용가는 G.")]
        public AdContentRating MaxAdContentRating = AdContentRating.Unspecified;

        public enum AdChildTreatment { Unspecified, True, False }
        public enum AdContentRating { Unspecified, G, PG, T, MA }

        /// <summary>
        /// 실제로 사용할 광고 단위 ID 세트를 반환.
        /// 에디터/UseTestAds/프로덕션 ID 미설정 중 하나라도 해당하면 테스트 ID 반환.
        /// </summary>
        public (string banner, string interstitial, string rewarded) ResolveIds()
        {
            bool useTest = UseTestAds;
#if UNITY_EDITOR
            useTest = true;
#endif
            if (string.IsNullOrEmpty(BannerAdUnitId)) useTest = true;

            return useTest
                ? (TestBannerAdUnitId, TestInterstitialAdUnitId, TestRewardedAdUnitId)
                : (BannerAdUnitId, InterstitialAdUnitId, RewardedAdUnitId);
        }
    }
}
