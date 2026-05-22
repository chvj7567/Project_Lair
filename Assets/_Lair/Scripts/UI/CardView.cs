using System;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 단일 카드 표시 — 이름/설명/카테고리 색 테두리/픽 버튼.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private Image _border;
        [SerializeField] private CHButton _pickButton;

        public void Bind(CardData card, Action onClick)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            _border.color = CategoryColor(card.Category);
            _pickButton.OnClick(onClick);
        }

        //# 카테고리 색 — CardView 와 BuildIconCell 이 공유 (Rule 03 — 색 매핑 단일 출처).
        public static Color CategoryColor(ECardCategory c) => c switch
        {
            ECardCategory.Enhance     => new Color(0.13f, 0.77f, 0.37f),
            ECardCategory.Spawn       => new Color(0.23f, 0.51f, 0.96f),
            ECardCategory.Replace     => new Color(0.96f, 0.62f, 0.23f),
            ECardCategory.Environment => new Color(0.66f, 0.33f, 0.96f),
            _                         => Color.gray,
        };
    }
}
