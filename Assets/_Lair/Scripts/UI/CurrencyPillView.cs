using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 재화 알약/칩 한 개 표시 데이터 (monster-growth-currencies §8.1 지갑 · §8.4 결과창 칩). 아이콘은 호출 View 가 주입.
    public class CurrencyPillData
    {
        public ECurrency Currency;
        public string AmountText;     //# 지갑 "25" / 결과창 "+25"
        public string NameText;       //# 결과창 칩 재화 이름 (지갑은 비움)
        public string ColorHex;
        public Sprite Icon;
        public bool TintIcon;         //# 전용 아이콘이 없어 소울 코인을 재화 색으로 물들인 임시 표시
    }

    //# 재화 알약 위젯 — 아이콘 + 숫자 (+ 선택 이름). 표시만 한다.
    public class CurrencyPillView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _amountText;
        [SerializeField] private CHText _nameText;   //# 선택 — 결과창 칩만

        public void Bind(CurrencyPillData data)
        {
            gameObject.SetActive(data != null);
            if (data == null)
                return;
            Color color = ColorUtility.TryParseHtmlString(data.ColorHex, out Color c) ? c : Color.white;
            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
                _icon.color = data.TintIcon ? color : Color.white;
            }
            if (_amountText != null)
            {
                _amountText.SetText(data.AmountText ?? string.Empty);
                _amountText.SetColor(color);
            }
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(string.IsNullOrEmpty(data.NameText) == false);
                _nameText.SetText(data.NameText ?? string.Empty);
            }
        }
    }
}
