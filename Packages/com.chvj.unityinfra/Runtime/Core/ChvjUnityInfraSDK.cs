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

        public static async Task Initialize<TAudio>(InfraInitConfig<TAudio> config = null)
            where TAudio : struct, Enum
        {
            config ??= new InfraInitConfig<TAudio>();

            // 1) 리소스 매니저 (다른 매니저의 전제)
            await CHMResource.Instance.Init();

            // 2) 게임 측 추가 로드 (게임 데이터·폰트 등 — CHMResource에 의존)
            if (config.AfterResourceInit != null)
            {
                await config.AfterResourceInit();
            }

            // 3) UI/Sound 매니저
            CHMUI.Instance.Init();
            CHMSound.Instance.Init<TAudio>(config.BGMKeys ?? Array.Empty<TAudio>());

            // 4) Hook + Provider 등록
            if (config.ClickSoundHook != null)
            {
                CHButton.ClickSoundHook = config.ClickSoundHook;
                CHToggle.ChangeSoundHook = config.ClickSoundHook;
            }
            if (config.StringProvider != null)
            {
                CHText.StringProvider = config.StringProvider;
            }
            if (config.FontProvider != null)
            {
                CHText.FontProvider = config.FontProvider;
            }

            // 5) 옵트인 모듈 — 컴파일된 모듈만 hook 등록되어 있음
            AdsInitHook?.Invoke();
            IapInitHook?.Invoke();
        }
    }

    /// <summary>
    /// ChvjUnityInfraSDK.Initialize 호출 시 넘기는 게임별 옵션.
    /// 필요한 필드만 채우고 나머진 null/default로 두면 패키지가 알아서 처리.
    /// </summary>
    public class InfraInitConfig<TAudio> where TAudio : struct, Enum
    {
        /// <summary>
        /// 버튼 클릭/토글 변경 시 자동 재생할 사운드 hook.
        /// 예: ClickSoundHook = () => CHMSound.Instance.Play(EAudio.Click);
        /// null이면 hook 미등록(클릭 무음).
        /// </summary>
        public Action ClickSoundHook;

        /// <summary>
        /// CHText의 stringID → 문자열 변환 제공자.
        /// null이면 stringID 모드 비활성(SetText의 plain 인자만 동작).
        /// </summary>
        public IStringProvider StringProvider;

        /// <summary>
        /// CHText의 stringID 모드일 때 폰트/머티리얼 제공자.
        /// null이면 default TMP 폰트 사용.
        /// </summary>
        public IFontProvider FontProvider;

        /// <summary>
        /// CHMResource.Init 직후 실행할 추가 비동기 작업.
        /// 게임 데이터 JSON 로드, 폰트 로드 등 CHMResource에 의존하는 게임 측 셋업.
        /// </summary>
        public Func<Task> AfterResourceInit;

        /// <summary>
        /// BGM 채널로 자동 loop 처리할 enum 키들. null 또는 빈 배열이면 BGM 채널 없음.
        /// 예: new[] { EAudio.MainBGM, EAudio.BattleBGM }
        /// </summary>
        public TAudio[] BGMKeys;
    }
}
