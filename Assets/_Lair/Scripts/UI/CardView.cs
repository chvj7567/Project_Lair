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
        [SerializeField] private CHButton _pickButton;
        //# 3픽 캡 — 이미 픽한 횟수 N (0 이면 숨김, 1~2 면 "N/3"). 3 도달 카드는 후보에 안 나옴.
        [SerializeField] private CHText _countBadge;

        public void Bind(CardData card, Action onClick) => Bind(card, onClick, 0);

        public void Bind(CardData card, Action onClick, int pickCount)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            //# 테두리 색 — 카드 ID 기준 단일 출처 (CardBorderColors).
            _border.color = CardBorderColors.BorderColorOf(card.Id);
            _pickButton.OnClick(onClick);
            UpdateBadge(pickCount);
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
