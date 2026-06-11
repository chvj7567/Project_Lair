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
                Color bg = data.IsLockedDummy ? DummyBg : data.Reached ? ReachedBg : NormalBg;
                _background.color = bg;
            }
            if (_levelText != null)
            {
                _levelText.SetText(data.LevelText);
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
                }
            }
            if (_reachedBadge != null)
            {
                bool show = data.Reached && data.IsLockedDummy == false;
                _reachedBadge.gameObject.SetActive(show);
                if (show)
                {
                    _reachedBadge.SetText("달성");
                    _reachedBadge.SetColor(BadgeColor);
                }
            }
        }
    }
}
