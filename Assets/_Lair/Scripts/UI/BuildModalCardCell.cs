using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 모달 카드 셀 — 프레임(카테고리 색) + 이름 + ×N + 설명 한 줄.
    //# 기획서 §2.7.3 — 8×40 프레임 + 12pt 이름 + 10pt ×N + 10pt 설명.
    //# CHPoolingScrollView<BuildModalCardCell, BuildEntry> 의 TItem (Rule 11 v0.8).
    //# 기존 BuildModalPopup.cs nested 정의를 분리 — CHPoolingScrollView 의 _origin 으로
    //# 참조되려면 별도 클래스 / 별도 prefab 의 component 로 직렬화돼야 함.
    public class BuildModalCardCell : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHText _descText;

        //# ×N 노랑 (#FBBF24).
        private static readonly Color CountColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        //# 설명 회색 (#D1D5DB).
        private static readonly Color DescColor  = new Color(0.820f, 0.835f, 0.859f, 1f);

        //# 풀 재사용 시 상태 리셋 (Rule 12) — Bind 가 매번 ×N 표시를 다시 결정하므로 OnEnable 도 안전.
        private void OnEnable()
        {
            if (_countText != null) _countText.gameObject.SetActive(false);
        }

        //# CardData + 픽 카운트 받기. CHPoolingScrollView 어댑터(BuildModalCardPoolingScrollView)가
        //# InitItem 안에서 entry.Card / entry.Count 를 풀어 호출.
        public void Bind(CardData card, int count)
        {
            if (card == null) return;
            if (_frame != null)
            {
                (char letter, Color bgColor, Color fgColor) iconInfo = SpawnerStatusCell.IconLetterFor(card.Id);
                _frame.color = iconInfo.letter != ' ' ? iconInfo.bgColor : CardView.CategoryColor(card.Axis);
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
            Bind(entry.Card, entry.Count);
        }
    }
}
