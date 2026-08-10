using System;
using System.Threading.Tasks;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 매니저 초기화용 선택적 편의 함수. CHMResource → (afterResourceInit) → CHMUI 순서 보장 +
    /// 옵트인 모듈(Ads/IAP) init hook 호출만 대신 해준다. CHMPool/CHMSound/훅·Provider 등록은
    /// 이 함수를 쓰든 개별 Init()을 쓰든 게임 부팅 코드가 항상 직접 처리해야 한다.
    /// 옵트인 모듈(Ads/IAP)은 Tools/ChvjUnityInfra/Settings에서 켜야 컴파일되며, 그때만 Init 호출됨.
    /// Social/GPGS는 Login 시 자동 init이라 여기서는 처리하지 않음.
    /// </summary>
    public static class ChvjUnityInfraSDK
    {
        /// <summary>
        /// 옵트인 모듈(Ads/IAP)이 자기 어셈블리에서 RuntimeInitializeOnLoadMethod로 등록.
        /// 모듈이 꺼져 어셈블리가 컴파일 안 되면 hook도 null이라 자동 skip.
        /// `Initialize()` 사용 시 내부에서 자동 호출되지만, 매니저를 개별 초기화하는 경우엔
        /// 게임 부팅 코드가 직접 `?.Invoke()` 해야 한다.
        /// </summary>
        public static Action AdsInitHook;
        public static Action IapInitHook;

        /// <summary>
        /// 패키지 코어 초기화. CHMResource → (afterResourceInit) → CHMUI 순으로 진행하고,
        /// 컴파일된 옵트인 모듈(Ads/IAP)의 init hook을 호출한다.
        ///
        /// 오디오 / Click 사운드 hook / i18n Provider / 폰트 Provider는 SDK에 묶이지 않으므로
        /// 게임 부팅 코드에서 각자 직접 설정:
        /// <code>
        /// CHMSound.Instance.Init&lt;EAudio&gt;(EAudio.BGM);
        /// CHButton.ClickSoundHook = () =&gt; CHMSound.Instance.Play(EAudio.Click);
        /// CHToggle.ChangeSoundHook = CHButton.ClickSoundHook;
        /// CHText.StringProvider = new GameStringProvider();
        /// CHText.FontProvider = new GameFontProvider();
        /// await ChvjUnityInfraSDK.Initialize();
        /// </code>
        /// </summary>
        /// <param name="afterResourceInit">
        /// CHMResource.Init 직후 / CHMUI.Init 이전에 실행할 비동기 작업.
        /// 게임 데이터 JSON 로드, 폰트 로드 등 CHMResource에 의존하는 게임 측 셋업.
        /// null이면 skip.
        /// </param>
        public static async Task Initialize(Func<Task> afterResourceInit = null)
        {
            // 1) 리소스 매니저 (다른 매니저의 전제)
            await CHMResource.Instance.Init();

            // 2) 게임 측 추가 로드 (CHMResource에 의존)
            if (afterResourceInit != null)
            {
                await afterResourceInit();
            }

            // 3) UI 매니저
            CHMUI.Instance.Init();

            // 4) 옵트인 모듈 — 컴파일된 모듈만 hook 등록되어 있음
            AdsInitHook?.Invoke();
            IapInitHook?.Invoke();
        }
    }
}
