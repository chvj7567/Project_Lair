using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도전과제 셀 — 이름/설명/보상 + 달성 뱃지. 달성 셀 강조 (기획서 §5 / §7). 뱃지는 stringID 미사용 전제.
    public class QuestCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private CHText _rewardText;
        [SerializeField] private CHText _achievedBadge;   //# "달성"

        //# 달성 강조 (#FBBF24 톤 배경) / 미달성 기본 (#1F2937).
        private static readonly Color AchievedBg = new Color(0.30f, 0.25f, 0.10f, 0.95f);
        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color BadgeColor = new Color(0.984f, 0.749f, 0.141f, 1f);

        public void Bind(QuestCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                _background.color = data.Achieved ? AchievedBg : NormalBg;
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
            }
            if (_descText != null)
            {
                _descText.SetText(data.Description);
            }
            if (_rewardText != null)
            {
                _rewardText.SetText(data.RewardText);
            }
            if (_achievedBadge != null)
            {
                _achievedBadge.gameObject.SetActive(data.Achieved);
                if (data.Achieved)
                {
                    _achievedBadge.SetText("달성");
                    _achievedBadge.SetColor(BadgeColor);
                }
            }
        }
    }
}
