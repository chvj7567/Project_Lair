using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 모달 카드 셀 — 아이콘 + 그 뒤 카테고리 색 테두리(48×48) + 이름 + ×N + 설명 한 줄.
    //# 기획서 §2.7.3 — 아이콘 둘레 축 색 테두리 + 12pt 이름 + 10pt ×N + 10pt 설명.
    public class BuildModalCardCell : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHText _descText;
        //# UI 리디자인 — 종류색 링(패시브=소울 / 액티브=금). 배선되면 슬롯 프레임(_frame)은 도트 sunk 스프라이트 그대로 둔다.
        [SerializeField] private Image _kindRing;

        //# ×N 노랑 (#FBBF24).
        private static readonly Color CountColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        //# 설명 회색 (#D1D5DB).
        private static readonly Color DescColor  = new Color(0.820f, 0.835f, 0.859f, 1f);

        //# 표시 상태는 Bind 가 완전히 결정 — CardView 와 동일 단일 출처. OnEnable 리셋은 재오픈 시 Bind 이후 발화해 잔상 역효과라 제거.
        //# CardData + 픽 카운트 받기. CHPoolingScrollView 어댑터(BuildModalCardPoolingScrollView)가
        //# InitItem 안에서 entry.Card / entry.Count 를 풀어 호출.
        public void Bind(CardData card, int count, bool isPassive = true)
        {
            if (card == null) return;
            if (_kindRing != null)
            {
                _kindRing.color = UiDotPalette.CardKind(isPassive);
            }
            if (_frame != null)
            {
                //# 카드 ID 기준 단일 출처 — 종 색/영웅 백색/몬스터 전체 시안. 종류 링이 있으면 프레임은 흰색(스킨 그대로).
                _frame.color = _kindRing != null ? Color.white : CardBorderColors.BorderColorOf(card.Id);
            }
            if (_icon != null)
            {
                //# 아이콘 없으면 비활성 — 풀 재사용 시 Bind 가 매 Pop 마다 active 상태를 다시 결정하므로 누수 없음.
                bool hasIcon = card.Icon != null;
                _icon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    _icon.sprite = card.Icon;
                }
            }
            if (_nameText != null) _nameText.SetText(card.DisplayName);
            if (_descText != null)
            {
                _descText.SetText(card.Description);
                _descText.SetColor(DescColor);
            }
            if (_countText != null)
            {
                bool show = count >= 2;
                _countText.gameObject.SetActive(show);
                if (show)
                {
                    _countText.SetText($"×{count}");
                    _countText.SetColor(CountColor);
                }
            }
        }

        //# CHPoolingScrollView 의 BuildEntry 어댑터용 — 명시 시그니처 분리로 호출부 가독성 ↑.
        public void Bind(BattleViewModel.BuildEntry entry)
        {
            if (entry == null) return;
            Bind(entry.Card, entry.Count, entry.IsPassive);
        }
    }
}
