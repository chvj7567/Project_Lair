using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 보스 바 오른쪽 "액티브 카드" 카운트다운 View(Rule 02 §6.1) — scene-2d-conversion §4.4.
    public class ActiveCountdownView : MonoBehaviour
    {
        [SerializeField] private CHText _value;
        [SerializeField] private Image _miniBar;
        //# 비주얼 폴리시 — 5초 이하 임박 금색 글로우와 평상시 숫자 색.
        [SerializeField] private GameObject _imminentGlow;
        [SerializeField] private Color _valueNormalColor = Color.white;

        public const float ImminentRemainSeconds = 5f;

        //# -1 미적용 / 0 평상시 / 1 임박. 변할 때만 글로우·글자 색 갱신.
        private int _imminentState = -1;

        public static bool IsImminent(ActiveCountdown c)
        {
            return c.HasNext && c.RemainingSeconds <= ImminentRemainSeconds;
        }

        private void OnEnable()
        {
            _imminentState = -1;
        }

        public void Show(ActiveCountdown c)
        {
            ApplyImminent(IsImminent(c) ? 1 : 0);
            if (_value != null)
                _value.SetText(BattleViewModel.FormatCountdown(c));
            if (_miniBar != null)
                _miniBar.fillAmount = c.HasNext ? c.Progress : 0f;
        }

        private void ApplyImminent(int state)
        {
            if (state == _imminentState)
                return;
            _imminentState = state;
            if (_imminentGlow != null)
            {
                _imminentGlow.SetActive(state == 1);
            }
            if (_value != null)
            {
                _value.SetColor(state == 1 ? UiDotPalette.Gold : _valueNormalColor);
            }
        }
    }
}
