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
            _border.color = CategoryColor(card.Axis);
            _pickButton.OnClick(onClick);
        }

        //# 카테고리 색 — CardView 와 BuildIconCell 이 공유 (Rule 03 — 색 매핑 단일 출처).
        //# 카드 리뉴얼 v0.6 Phase 2 — 기획서 §2 4축 색 매핑 정정.
        //# Tank #22C55E (Wisp 초록) / Dps #EF4444 (Reaper 빨강) / Debuff #A855F7 (Plague 보라) / Swarm #1F2937 (Phantom 검정).
        public static Color CategoryColor(EBuildAxis c) => c switch
        {
            EBuildAxis.Tank   => new Color(0.133f, 0.773f, 0.369f, 1f),
            EBuildAxis.Dps    => new Color(0.937f, 0.267f, 0.267f, 1f),
            EBuildAxis.Debuff => new Color(0.659f, 0.333f, 0.969f, 1f),
            EBuildAxis.Swarm  => new Color(0.122f, 0.157f, 0.220f, 1f),
            _                 => Color.gray,
        };
    }
}
