using System.Collections;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ToastArg : UIArg
    {
        public string Message;
    }

    //# 공용 토스트 — 하단중앙 1.8초 노출 후 페이드아웃 자동 닫힘. 입력 통과(게임 흐름 차단 금지, 기획서 §6).
    //# 동시 1개 — 연속 호출 시 최신 메시지로 교체(같은 캐시 인스턴스 재사용).
    public class ToastView : UIBase
    {
        [SerializeField] private CHText _messageText;   //# 메시지 라벨
        [SerializeField] private CanvasGroup _canvasGroup;   //# 페이드용

        private const float HoldSeconds = 1.8f;
        private const float FadeSeconds = 0.35f;

        private Coroutine _routine;
        private bool _pendingPlay;

        //# 정적 헬퍼 — 어디서든 ToastView.Show("...") 1줄. CHMUI 캐시 재사용으로 동시 1개 보장.
        public static void Show(string message)
        {
            CHMUI.Instance.ShowUI(EUI.ToastView, new ToastArg { Message = message });
        }

        //# CHMUI 순서는 InitUI → SetActive(true) 다. 이 시점 GO 가 비활성일 수 있어 StartCoroutine 이 실패하므로
        //# 메시지만 세팅하고 실제 재생은 OnEnable 로 미룬다(BuildModalPopup 과 동일한 패턴).
        public override void InitUI(UIArg arg)
        {
            if (arg is ToastArg toastArg)
            {
                if (_messageText != null)
                    _messageText.SetText(toastArg.Message);
                _pendingPlay = true;
                if (isActiveAndEnabled)
                    StartFade();
            }
        }

        private void OnEnable()
        {
            if (_pendingPlay)
                StartFade();
        }

        private void StartFade()
        {
            _pendingPlay = false;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;   //# 입력 통과
                _canvasGroup.interactable = false;
            }

            yield return new WaitForSecondsRealtime(HoldSeconds);

            float elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / FadeSeconds);
                yield return null;
            }
            _routine = null;
            //# 재사용 캐시 — Close(reuse: true) 로 다음 Show 때 재활용.
            Close(reuse: true);
        }
    }
}
