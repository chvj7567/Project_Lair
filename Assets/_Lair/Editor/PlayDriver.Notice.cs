using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Play 중 공지 UI 3종(토스트·확인 팝업·스킬 해금 배너) 미리보기 진입점 — 스크린샷 확인용. 인자 없음.
    public static partial class PlayDriver
    {
        public static string ShowToastPreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            ToastView.Show("토스트 미리보기 메시지입니다", EToastKind.Warning);
            return "OK toast(Warning)";
        }

        public static string ShowConfirmPreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            CHMUI.Instance.ShowUI(EUI.ConfirmPopup, new ConfirmPopupArg
            {
                Title = "확인 미리보기",
                Message = "공용 확인 팝업 미리보기 메시지입니다.",
                ConfirmLabel = "확인",
                CancelLabel = "취소",
                OnConfirm = () => { },
                OnCancel = null,
            });
            return "OK confirm";
        }

        public static string ShowBannerPreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            CHMUI.Instance.ShowUI(EUI.SkillUnlockBanner, new SkillUnlockBannerArg(), ui =>
            {
                SkillUnlockBannerView banner = ui as SkillUnlockBannerView;
                if (banner == null)
                    return;

                banner.StartCoroutine(banner.PlayCo("영웅의 '번개 일격' 스킬 해제"));
            });
            return "OK banner";
        }
    }
}
