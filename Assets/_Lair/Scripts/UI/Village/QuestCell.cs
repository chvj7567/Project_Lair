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
        [SerializeField] private GameObject _progressRoot;   //# 진행 바 컨테이너 — 표시/숨김 토글 (기획서 §3.3)
        [SerializeField] private Image _progressFill;         //# fillAmount = Current/Target (Image type=Filled/Horizontal)
        [SerializeField] private CHText _progressText;        //# "12/25"
        //# UI 리디자인 — 석판 스프라이트(달성은 초록 틴트) + 달성 배지 배경. 미할당이면 기존 색 방식.
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private GameObject _badgeBg;

        private static readonly Color AchievedTint = new Color(0.72f, 1f, 0.84f, 1f);

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
                if (_normalSprite != null)
                {
                    _background.sprite = _normalSprite;
                    _background.color = data.Achieved ? AchievedTint : Color.white;
                }
                else
                {
                    _background.color = data.Achieved ? AchievedBg : NormalBg;
                }
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
                _rewardText.SetColor(UiDotPalette.Soul);
            }
            if (_badgeBg != null)
            {
                _badgeBg.SetActive(data.Achieved);
            }
            if (_achievedBadge != null)
            {
                _achievedBadge.gameObject.SetActive(data.Achieved);
                if (data.Achieved)
                {
                    _achievedBadge.SetText("달성");
                    _achievedBadge.SetColor(_badgeBg != null ? UiDotPalette.Soul : BadgeColor);
                }
            }
            //# 진행 바 — HasProgress 일 때만 표시 (달성 뱃지와 상호 배타, 기획서 §3.3).
            if (_progressRoot != null)
            {
                _progressRoot.SetActive(data.HasProgress);
            }
            if (data.HasProgress)
            {
                if (_progressFill != null)
                {
                    _progressFill.fillAmount = data.Target > 0 ? (float)data.Current / data.Target : 0f;
                }
                if (_progressText != null)
                {
                    _progressText.SetText($"{data.Current}/{data.Target}");
                }
            }
        }
    }
}
