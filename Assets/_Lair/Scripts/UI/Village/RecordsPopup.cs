using ChvjUnityInfra;
using Lair.Meta;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class RecordsPopupArg : UIArg
    {
        public MetaProfile Profile;
    }

    //# 전적 기록 — 총 출격 / 승리 / 승률 / 최단 클리어 정적 텍스트 (스크롤뷰 불요, 기획서 §7 id 25~28).
    public class RecordsPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _bodyText;

        public override void InitUI(UIArg arg)
        {
            if (arg is RecordsPopupArg recordsArg && _bodyText != null)
            {
                _bodyText.SetText(BuildBody(recordsArg.Profile));
            }

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
        }

        public static string BuildBody(MetaProfile profile)
        {
            if (profile == null)
                return string.Empty;

            int winRate = profile.TotalRuns > 0
                ? Mathf.RoundToInt(profile.TotalWins * 100f / profile.TotalRuns)
                : 0;
            string bestClear = profile.BestClearTime < 0f ? "-" : $"{profile.BestClearTime:0.0}초";
            return $"총 출격  {profile.TotalRuns}\n승리  {profile.TotalWins}\n승률  {winRate}%\n최단 클리어  {bestClear}";
        }
    }
}
