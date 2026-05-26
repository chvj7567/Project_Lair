using System;
using System.Threading.Tasks;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 패키지 통합 초기화 진입점. 외부에서 Initialize 한 번 호출로 모든 매니저 + 옵트인 모듈 초기화.
    /// 옵트인 모듈(Ads/IAP)은 Tools/ChvjUnityInfra/Settings에서 켜야 컴파일되며, 그때만 Init 호출됨.
    /// Social/GPGS는 Login 시 자동 init이라 여기서는 처리하지 않음.
    /// </summary>
    public static class ChvjUnityInfraSDK
    {
        /// <summary>
        /// 옵트인 모듈(Ads/IAP)이 자기 어셈블리에서 RuntimeInitializeOnLoadMethod로 등록.
        /// 모듈이 꺼져 어셈블리가 컴파일 안 되면 hook도 null이라 SDK는 자동 skip.
        /// 게임 코드에서 직접 건드릴 일 없음.
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
