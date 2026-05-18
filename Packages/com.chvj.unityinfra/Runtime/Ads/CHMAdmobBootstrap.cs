using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Ads 모듈이 컴파일됐을 때 SDK에 자기 Init 액션을 등록.
    /// 메인 SDK는 ads 어셈블리를 참조하지 않으므로 이 부트스트랩을 통해 우회적으로 연결.
    /// </summary>
    internal static class CHMAdmobBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ChvjUnityInfraSDK.AdsInitHook = () => CHMAdmob.Instance.Init();
        }
    }
}
