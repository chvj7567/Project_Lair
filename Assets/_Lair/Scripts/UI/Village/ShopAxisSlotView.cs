using System;
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 상점 종족 행의 육성 칸 하나 (체급·각성·둥지·특성, monster-growth-currencies §8.1). 칸 전체가 버튼. 표시만 하고 판단은 ShopPopup 이 한다.
    public class ShopAxisSlotView : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private CHText _axisText;      //# 축 이름
        [SerializeField] private CHText _effectText;    //# 효과 요약 2줄
        [SerializeField] private CHText _priceText;     //# 가격 / MAX / 잠김 안내
        [SerializeField] private Image _currencyIcon;   //# 가격 띠 재화 아이콘
        [SerializeField] private Image[] _pips = new Image[0];   //# 레벨 눈금 (체급·각성 5칸, 둥지·특성 3칸)
        [SerializeField] private Image _border;         //# 구매 가능 = 재화 색 테두리
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private CanvasGroup _canvasGroup;   //# 잠김 = 밝기 0.45

        private const float LockedAlpha = 0.45f;

        private ShopAxisSlotData _data;
        private Action<ShopAxisSlotData> _onClick;
        private bool _wired;

        //# 표시 상태는 Bind 가 완전히 결정 — 풀 재사용 셀이 매 InitItem 마다 호출한다.
        public void Bind(ShopAxisSlotData data, Action<ShopAxisSlotData> onClick)
        {
            _data = data;
            _onClick = onClick;
            gameObject.SetActive(data != null);
            if (data == null)
                return;
            WireOnce();
            if (_button != null)
            {
                _button.Interactable = data.IsTierUpgrade == false || data.CanBuy;
            }

            Color currencyColor = ParseColor(data.ColorHex);
            SetText(_axisText, data.AxisName);
            SetText(_effectText, data.EffectText);
            if (_priceText != null)
            {
                string price = data.IsLocked ? data.LockText : string.IsNullOrEmpty(data.PriceText) == false ? data.PriceText : data.IsMax ? "MAX" : $"{data.Price:N0}";
                _priceText.SetText(price);
                _priceText.SetColor(data.IsMax ? UiDotPalette.Gold : data.CanBuy ? currencyColor : UiDotPalette.Stone4);
            }
            if (_currencyIcon != null)
            {
                bool showIcon = data.IsLocked == false && data.IsMax == false && data.CurrencyIcon != null && string.IsNullOrEmpty(data.PriceText);
                _currencyIcon.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    _currencyIcon.sprite = data.CurrencyIcon;
                    _currencyIcon.color = data.TintIcon ? currencyColor : Color.white;
                }
            }
            for (int i = 0; i < _pips.Length; ++i)
            {
                if (_pips[i] == null)
                    continue;
                _pips[i].gameObject.SetActive(data.IsLocked == false && i < data.MaxLevel);
                _pips[i].color = i < data.Level ? currencyColor : UiDotPalette.Stone4;
            }
            if (_border != null)
            {
                _border.gameObject.SetActive(data.CanBuy);
                _border.color = currencyColor;
            }
            if (_lockIcon != null)
            {
                _lockIcon.SetActive(data.IsLocked);
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = data.IsLocked ? LockedAlpha : 1f;
            }
        }

        private void WireOnce()
        {
            if (_wired || _button == null)
                return;
            _wired = true;
            _button.OnClick(HandleClick);
        }

        private void HandleClick()
        {
            if (_data == null)
                return;
            _onClick?.Invoke(_data);
        }

        private static void SetText(CHText label, string text)
        {
            if (label == null)
                return;
            label.SetText(text ?? string.Empty);
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }
    }
}
