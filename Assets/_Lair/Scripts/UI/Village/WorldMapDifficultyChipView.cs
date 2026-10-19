using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 난이도 효과 칩(HP / ATK) 1개 — 값 문구 + ▲ 증가 표시. 위젯은 private, 의도 API 만 노출(Rule 02 §6.1).
    public class WorldMapDifficultyChipView : MonoBehaviour
    {
        [SerializeField] private CHText _valueText;
        [SerializeField] private Image _upMarker;

        private static readonly Color FlatColor = new Color32(0x8e, 0x98, 0xad, 0xff);

        //# up=false(Easy 기준값)면 ▲ 숨기고 회색.
        public void Show(string value, bool up, Color upColor)
        {
            if (_valueText != null)
            {
                _valueText.SetText(value ?? string.Empty);
                _valueText.SetColor(up ? upColor : FlatColor);
            }
            if (_upMarker != null)
            {
                _upMarker.gameObject.SetActive(up);
                _upMarker.color = upColor;
            }
        }
    }
}
