using System;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 단일 카드 표시 — 이름/설명/카테고리 색 테두리/픽 버튼/3픽 캡 배지.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private Image _border;
        //# 3택1 팝업 상단 일러스트. CardData.CardImage 가 null 이면 영역을 숨긴다.
        [SerializeField] private Image _artImage;
        [SerializeField] private CHButton _pickButton;
        //# 3픽 캡 — 이미 픽한 횟수 N (0 이면 숨김, 1~2 면 "N/3"). 3 도달 카드는 후보에 안 나옴.
        [SerializeField] private CHText _countBadge;
        //# 종류 표시 — 패시브=소울 / 액티브=금. 프레임 링 tint + 종류 라벨 + 선택 버튼 스프라이트(soul/gold).
        [SerializeField] private Image _kindRing;
        [SerializeField] private CHText _kindLabel;
        [SerializeField] private Image _pickButtonImage;
        [SerializeField] private Sprite _passiveButtonSprite;
        [SerializeField] private Sprite _activeButtonSprite;

        public void Bind(CardData card, Action onClick) => Bind(card, onClick, 0);

        //# 카드 종류 표시 — 팝업이 Bind 전에 호출. 미할당 위젯은 건너뛴다.
        public void SetKind(bool isPassive)
        {
            if (_kindRing != null)
            {
                _kindRing.color = UiDotPalette.CardKind(isPassive);
            }
            if (_kindLabel != null)
            {
                _kindLabel.SetText(isPassive ? "패시브" : "액티브");
            }
            Sprite buttonSprite = isPassive ? _passiveButtonSprite : _activeButtonSprite;
            if (_pickButtonImage != null && buttonSprite != null)
            {
                _pickButtonImage.sprite = buttonSprite;
            }
        }

        public void Bind(CardData card, Action onClick, int pickCount)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            //# 테두리 색 — 카드 ID 기준 단일 출처 (CardBorderColors).
            _border.color = CardBorderColors.BorderColorOf(card.Id);
            ApplyArt(card);
            _pickButton.OnClick(onClick);
            UpdateBadge(pickCount);
        }

        //# 일러스트 적용 — null 이면 아트 영역 비활성(폴백). Bind 에서 호출.
        public void ApplyArt(CardData card)
        {
            if (_artImage == null)
                return;

            Sprite art = card != null ? card.CardImage : null;
            if (art == null)
            {
                _artImage.gameObject.SetActive(false);
                return;
            }

            _artImage.gameObject.SetActive(true);
            _artImage.sprite = art;
        }

        private void UpdateBadge(int pickCount)
        {
            if (_countBadge == null)
                return;

            if (pickCount <= 0)
            {
                _countBadge.gameObject.SetActive(false);
                return;
            }

            _countBadge.gameObject.SetActive(true);
            _countBadge.SetText(pickCount + "/" + Lair.Card.CardPickCounter.Cap);
        }
    }
}
