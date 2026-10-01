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

        public void Show(ActiveCountdown c)
        {
            if (_value != null)
                _value.SetText(BattleViewModel.FormatCountdown(c));
            if (_miniBar != null)
                _miniBar.fillAmount = c.HasNext ? c.Progress : 0f;
        }
    }
}
