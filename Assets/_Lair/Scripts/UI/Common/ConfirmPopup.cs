using System;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ConfirmPopupArg : UIArg
    {
        public string Title;
        public string Message;
        public string ConfirmLabel;   //# 예: "복원"
        public string CancelLabel;    //# 예: "취소"
        public Action OnConfirm;
        public Action OnCancel;       //# null 허용
    }

    //# 공용 2버튼 확인 다이얼로그 — 복원·충돌 권유 재사용(기획서 §2·§3). 표시·입력만(Rule 02 §6).
    public class ConfirmPopup : UIBase
    {
        [SerializeField] private CHText _titleText;
        [SerializeField] private CHText _messageText;
        [SerializeField] private CHButton _confirmButton;
        [SerializeField] private CHButton _cancelButton;

        public override void InitUI(UIArg arg)
        {
            ConfirmPopupArg confirmArg = arg as ConfirmPopupArg;
            if (confirmArg == null)
                return;

            if (_titleText != null)
                _titleText.SetText(confirmArg.Title);
            if (_messageText != null)
                _messageText.SetText(confirmArg.Message);

            if (_confirmButton != null)
            {
                _confirmButton.SetText(confirmArg.ConfirmLabel);
                Action onConfirm = confirmArg.OnConfirm;
                _confirmButton.OnClick(() =>
                {
                    Close(reuse: true);
                    onConfirm?.Invoke();
                }, closeDisposable);
            }

            if (_cancelButton != null)
            {
                _cancelButton.SetText(confirmArg.CancelLabel);
                Action onCancel = confirmArg.OnCancel;
                _cancelButton.OnClick(() =>
                {
                    Close(reuse: true);
                    onCancel?.Invoke();
                }, closeDisposable);
            }
        }
    }
}
