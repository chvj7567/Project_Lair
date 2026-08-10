using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMUI + UIBase + CHButton + CHText 데모.
    /// 사전 준비:
    /// 1. Scene에 Canvas 추가 + Tag "UICanvas" 부여
    /// 2. UI 프리팹(예: DemoPanel.prefab) 만들기 + DemoUI 컴포넌트 부착
    /// 3. 프리팹을 Addressables 등록, Label "Resource", 파일명 "DemoPanel"
    /// </summary>
    public class UIDemo : MonoBehaviour
    {
        public enum EDemoUI { DemoPanel }

        [SerializeField] private CHButton _openButton;

        private async void Start()
        {
            // 통합 SDK로 한 번에 초기화
            await ChvjUnityInfraSDK.Initialize<EDemoSound>();

            // 버튼 클릭 시 UI 표시 + 인자 전달
            _openButton.OnClick(() =>
            {
                CHMUI.Instance.ShowUI(EDemoUI.DemoPanel, new DemoUIArg
                {
                    Greeting = "Hello from UIDemo!",
                    Count = 42,
                });
            });

            // ESC 키를 누르면 가장 최근 UI가 자동으로 닫힘 (CHMUI 기본 동작)
        }

        // CHMSound도 필요한 경우 (UI에 클릭 사운드 등)
        public enum EDemoSound { None, Click }
    }

    /// <summary>UI별 인자 — UIArg 상속.</summary>
    public class DemoUIArg : UIArg
    {
        public string Greeting;
        public int Count;
    }

    /// <summary>
    /// 실제 UI 컴포넌트. 프리팹의 루트에 부착.
    /// _backgroundButton/_backButton (UIBase의 SerializedField)을 Inspector에서 연결하면 자동 close.
    /// </summary>
    public class DemoUI : UIBase
    {
        [SerializeField] private CHText _greetingText;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHButton _closeButton;

        public override void InitUI(UIArg arg)
        {
            var demoArg = arg as DemoUIArg;

            // CHText — plain 텍스트 모드 (stringID = -1 으로 둠)
            _greetingText.SetText(demoArg.Greeting);
            _countText.SetText("Count: ", demoArg.Count);

            // CHButton — 클릭 SFX는 패키지 hook으로 자동 (게임 부팅 시 등록)
            // closeDisposable은 UIBase 보호 필드 — UI 닫힐 때 구독 자동 정리
            _closeButton.OnClick(() => Close(), closeDisposable);
        }
    }
}
