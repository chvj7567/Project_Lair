using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 영주 보상 트랙 셀 — 레벨/보상명/소울 + 도달(완료) 강조, 미도달·잠금 더미는 어둡게 (기획서 §4.3).
    public class LordRewardCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private CHText _levelText;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _rewardText;
        [SerializeField] private CHText _reachedBadge;   //# "달성"
        //# UI 리디자인 — 보조 줄, 현재 레벨 금테, 석판 스프라이트(달성=Px_Panel / 미달성=Px_PanelDark), 배지 배경(soul/gold). 미할당이면 기존 색 방식.
        [SerializeField] private CHText _subText;
        [SerializeField] private Image _stateRing;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _pendingSprite;
        [SerializeField] private Image _badgeBg;
        [SerializeField] private Sprite _badgeSoulSprite;
        [SerializeField] private Sprite _badgeGoldSprite;

        private static readonly Color ReachedBg = new Color(0.30f, 0.25f, 0.10f, 0.95f);
        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color BadgeColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        public void Bind(LordRewardCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                bool pending = data.IsLockedDummy || data.Reached == false;
                Sprite skin = pending ? _pendingSprite : _normalSprite;
                if (skin != null)
                {
                    //# 도트 석판은 색을 그대로 — 이미 받은 보상(현재 레벨 제외)만 흐리게(alpha 0.65).
                    _background.sprite = skin;
                    bool dimmed = pending == false && data.IsCurrent == false;
                    _background.color = dimmed ? new Color(1f, 1f, 1f, 0.65f) : Color.white;
                }
                else
                {
                    _background.color = data.IsLockedDummy ? DummyBg : data.Reached ? ReachedBg : NormalBg;
                }
            }
            if (_stateRing != null)
            {
                _stateRing.gameObject.SetActive(data.IsCurrent);
                _stateRing.color = UiDotPalette.Gold;
            }
            if (_subText != null)
            {
                bool showSub = string.IsNullOrEmpty(data.SubText) == false;
                _subText.gameObject.SetActive(showSub);
                if (showSub)
                {
                    _subText.SetText(data.SubText);
                }
            }
            if (_levelText != null)
            {
                _levelText.SetText(data.LevelText);
                _levelText.SetColor(data.Reached && data.IsLockedDummy == false ? UiDotPalette.Gold : DummyTextColor);
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
                _nameText.SetColor(data.IsLockedDummy ? DummyTextColor : Color.white);
            }
            if (_rewardText != null)
            {
                bool show = string.IsNullOrEmpty(data.RewardText) == false;
                _rewardText.gameObject.SetActive(show);
                if (show)
                {
                    _rewardText.SetText(data.RewardText);
                    _rewardText.SetColor(data.Reached ? UiDotPalette.Soul : DummyTextColor);
                }
            }
            if (_reachedBadge != null)
            {
                bool show = data.Reached && data.IsLockedDummy == false;
                _reachedBadge.gameObject.SetActive(show);
                if (show)
                {
                    _reachedBadge.SetText("달성");
                    _reachedBadge.SetColor(_badgeBg != null ? (data.IsCurrent ? UiDotPalette.Gold : UiDotPalette.Soul) : BadgeColor);
                }
            }
            if (_badgeBg != null)
            {
                bool showBadge = data.Reached && data.IsLockedDummy == false;
                _badgeBg.gameObject.SetActive(showBadge);
                Sprite badgeSprite = data.IsCurrent ? _badgeGoldSprite : _badgeSoulSprite;
                if (showBadge && badgeSprite != null)
                {
                    _badgeBg.sprite = badgeSprite;
                }
            }
        }
    }
}
