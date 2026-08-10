using System.Threading.Tasks;
using ChvjUnityInfra;
using TMPro;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// 매니저 개별 초기화 데모 — 각 매니저 Init()을 직접 순서대로 호출하는 패턴.
    /// 실제 게임 부팅 코드(예: GameManagement.InitManager)에 쓰는 패턴. 유일한 순서 규칙:
    /// CHMResource.Init()이 가장 먼저(다른 매니저가 리소스 로드에 의존).
    /// </summary>
    public class BootstrapDemo : MonoBehaviour
    {
        public enum EAudio { None, BGM, Click }
        public enum EFont { Main }

        private TMP_FontAsset _fontAsset;
        private Material _fontMaterial;

        private async void Start()
        {
            // 1. 리소스 매니저 — 다른 매니저의 전제, 항상 먼저
            bool ok = await CHMResource.Instance.Init();
            if (ok == false)
            {
                Debug.LogError("[BootstrapDemo] CHMResource.Init 실패");
                return;
            }

            // 2. 게임 측 추가 로드 (폰트 등 — CHMResource.Init 이후 아무 때나)
            await LoadGameDataAsync();

            // 3. 나머지 매니저 — 순서 무관, 필요한 것만
            CHMUI.Instance.Init();

            // 4. 오디오 + UI hook/Provider 설정 — SDK에 묶이지 않으므로 필요한 것만 직접 등록

            // BGM 채널로 자동 loop 처리할 enum 키 명시 (params, 0~N개)
            CHMSound.Instance.Init<EAudio>(EAudio.BGM);

            // 클릭/토글 시 자동 재생할 사운드
            CHButton.ClickSoundHook = () => CHMSound.Instance.Play(EAudio.Click);
            CHToggle.ChangeSoundHook = CHButton.ClickSoundHook;

            // CHText의 stringID → 문자열 변환 (게임 측 구현)
            CHText.StringProvider = new DemoStringProvider();

            // CHText의 stringID 모드일 때 폰트/머티리얼
            CHText.FontProvider = new DemoFontProvider(this);

            // 5. 옵트인 모듈(Ads/IAP) — Tools/ChvjUnityInfra/Settings 에서 켜져 컴파일된 경우만 hook 존재
            ChvjUnityInfraSDK.AdsInitHook?.Invoke();
            ChvjUnityInfraSDK.IapInitHook?.Invoke();

            Debug.Log("[BootstrapDemo] 초기화 완료. 모든 매니저 사용 가능.");
            // 이 시점부터:
            // - CHMResource / CHMUI / CHMSound 사용 가능
            // - CHButton / CHText hook + provider 등록됨
            // - 활성화된 옵트인 모듈(Ads/IAP)도 초기화 완료
        }

        private async Task LoadGameDataAsync()
        {
            // 폰트/머티리얼 로드 (FontProvider가 사용)
            var fontTask = new TaskCompletionSource<bool>();
            CHMResource.Instance.Load<TMP_FontAsset>(EFont.Main, font =>
            {
                _fontAsset = font;
                fontTask.SetResult(true);
            });
            await fontTask.Task;

            var matTask = new TaskCompletionSource<bool>();
            CHMResource.Instance.Load<Material>($"{EFont.Main}Material", mat =>
            {
                _fontMaterial = mat;
                matTask.SetResult(true);
            });
            await matTask.Task;

            // 게임 데이터 JSON 로드 등 추가 작업도 여기에
            // await MyJsonLoader.LoadAllAsync();
        }

        private class DemoStringProvider : IStringProvider
        {
            // 실제 게임에선 JsonManager.GetString(id) 등으로 i18n 처리
            public string GetString(int stringID) => $"[String#{stringID}]";
        }

        private class DemoFontProvider : IFontProvider
        {
            private readonly BootstrapDemo _owner;
            public DemoFontProvider(BootstrapDemo owner) => _owner = owner;
            public TMP_FontAsset GetFont() => _owner._fontAsset;
            public Material GetFontMaterial() => _owner._fontMaterial;
        }
    }
}
