using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# 상점 품목 셀 — 이름/레벨/효과 설명/가격 + 구매 버튼 (기획서 §3.2 / §7).
    public class ShopItemCell : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _levelText;
        [SerializeField] private CHText _descText;
        [SerializeField] private CHText _priceText;
        [SerializeField] private CHButton _buyButton;
        [SerializeField] private CHText _buyLabel;

        //# 구매 가능 노랑 (#FBBF24) / 불가 회색 (#9CA3AF).
        private static readonly Color BuyableColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color DisabledColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        private ShopItemCellData _data;
        private bool _wired;

        //# 표시 상태는 Bind 가 완전히 결정 — 풀 재사용 시 매 InitItem 마다 호출됨.
        public void Bind(ShopItemCellData data)
        {
            if (data == null)
                return;
            _data = data;
            WireOnce();

            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
            }
            if (_levelText != null)
            {
                _levelText.SetText(data.LevelText);
            }
            if (_descText != null)
            {
                _descText.SetText(data.Description);
            }

            if (_priceText != null)
            {
                _priceText.gameObject.SetActive(data.IsMax == false);
                if (data.IsMax == false)
                {
                    _priceText.SetText($"{data.Price:N0} 소울");
                }
            }

            if (_buyButton != null)
            {
                _buyButton.Interactable = data.CanBuy;
            }
            if (_buyLabel != null)
            {
                //# 버튼 문구 치환 — 구매 / 만렙 / 소울 부족 (기획서 §7). 프리팹 _buyLabel 은 stringID 미사용 전제.
                string label = data.IsMax ? "만렙" : data.CanBuy ? "구매" : "소울 부족";
                _buyLabel.SetText(label);
                _buyLabel.SetColor(data.CanBuy ? BuyableColor : DisabledColor);
            }
        }

        //# CHButton.OnClick 은 listener 누적 — 풀 재사용 셀은 1회만 등록하고 현재 _data 로 위임.
        private void WireOnce()
        {
            if (_wired)
                return;
            _wired = true;
            if (_buyButton != null)
            {
                _buyButton.OnClick(HandleBuyClick);
            }
        }

        private void HandleBuyClick()
        {
            if (_data == null || _data.CanBuy == false)
                return;
            _data.OnBuy?.Invoke(_data.Id);
        }
    }
}
