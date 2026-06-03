using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Character
{
    //# HpBar.prefab 루트에 붙는 View 컴포넌트. 외부는 SetHp 만 호출 —
    //# 내부 Fill/txtHp 위젯은 노출하지 않는다 (Rule 02 §5/§6 캡슐화).
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private Image _fill;     //# HpBar 내부 Fill (채움)
        [SerializeField] private CHText _txtHp;   //# HpBar 내부 txtHp ("현재/최대")

        private bool _showText = true;

        //# 텍스트 표시 토글 — 몬스터 바는 false 로 숨김(영웅 HUD 는 기본 true).
        public void SetTextVisible(bool visible)
        {
            _showText = visible;
            if (_txtHp != null)
                _txtHp.gameObject.SetActive(visible);
        }

        public void SetHp(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (_showText == false) return;
            if (_txtHp != null)
                _txtHp.SetText($"{current}/{max}");
        }
    }
}
