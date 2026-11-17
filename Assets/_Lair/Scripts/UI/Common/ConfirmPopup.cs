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
        //# 확인 버튼 뒤 글로우 장식 — 1버튼 모드에서 확인 버튼과 같이 가운데로 옮긴다.
        [SerializeField] private RectTransform _confirmGlow;

        //# 2버튼 모드의 확인 버튼·글로우 X(프리팹 기본값). 1버튼 모드는 0.
        private const float TwoButtonConfirmX = -130f;

        //# 확인 외 모든 닫힘(취소·배경·ESC)은 OnDisable 에서 OnCancel 1회 — CHMUI ESC 는 Close 를 거치지 않는다.
        private Action _pendingCancel;

        public override void InitUI(UIArg arg)
        {
            ConfirmPopupArg confirmArg = arg as ConfirmPopupArg;
            if (confirmArg == null)
                return;

            _pendingCancel = confirmArg.OnCancel;

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
                    _pendingCancel = null;
                    Close(reuse: true);
                    onConfirm?.Invoke();
                }, closeDisposable);
            }

            //# 재사용 인스턴스라 호출마다 1/2버튼 상태를 모두 명시 설정 — 이전 호출 상태 오염 방지.
            bool oneButton = string.IsNullOrEmpty(confirmArg.CancelLabel);
            SetConfirmX(oneButton ? 0f : TwoButtonConfirmX);
            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(oneButton == false);
                _cancelButton.SetText(confirmArg.CancelLabel);
                _cancelButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
        }

        private void SetConfirmX(float x)
        {
            RectTransform confirmRect = _confirmButton != null ? _confirmButton.transform as RectTransform : null;
            if (confirmRect != null)
            {
                confirmRect.anchoredPosition = new Vector2(x, confirmRect.anchoredPosition.y);
            }
            if (_confirmGlow != null)
            {
                _confirmGlow.anchoredPosition = new Vector2(x, _confirmGlow.anchoredPosition.y);
            }
        }

        private void OnDisable()
        {
            Action cancel = _pendingCancel;
            _pendingCancel = null;
            cancel?.Invoke();
        }
    }
}
