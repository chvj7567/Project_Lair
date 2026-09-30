using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class LordLevelPopupArg : UIArg
    {
        public MetaProfile Profile;
        public MetaConfig Config;
    }

    //# 셀 표시 데이터 — 자동 수령(§4.4)이라 표시 전용: 도달(완료) / 미도달(잠금) / 잠금 더미.
    public class LordRewardCellData
    {
        public string LevelText;     //# "Lv 2"
        public string DisplayName;   //# 잠금 더미면 "??? — 추후 해금"
        public string RewardText;    //# "+50 소울" (더미면 빈 문자열)
        public bool Reached;         //# 현재 영주 레벨 도달 여부
        public bool IsLockedDummy;
        public bool IsCurrent;       //# 도달한 보상 중 가장 높은 레벨(금테 강조). 더미는 항상 false
        public string SubText;       //# 이름 아래 보조 줄 — "수령 완료" / "현재 레벨" / "N XP 남음" (더미는 빈 문자열)
    }

    //# 영주성 — 레벨 보상 트랙 Lv2~10 표시 전용 (기획서 §4).
    public class LordLevelPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _lordLevelText;   //# 헤더 — "영주 Lv 3"
        //# UI 리디자인 — 제목 아래 XP 바 + "다음 레벨까지 N XP". 위젯 연결은 프리팹 단계, 미할당이면 건너뛴다.
        [SerializeField] private Image _xpFill;
        [SerializeField] private CHText _xpNextText;
        [SerializeField] private LordRewardPoolingScrollView _scrollView;

        private LordLevelPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as LordLevelPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() => _arg = null);
            }

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }

            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        private void OnEnable()
        {
            if (_arg == null)
                return;
            BuildAndLayout();
        }

        private void BuildAndLayout()
        {
            //# Arg 캐스팅 실패/해제 후 호출 가드 — ShopPopup.Rebuild 와 동일 (NRE 방지).
            if (_arg == null)
                return;

            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            int currentLevel = LordLevelService.LevelFromXp(_arg.Profile.LordXp, _arg.Config);
            if (_lordLevelText != null)
            {
                _lordLevelText.SetText($"영주 Lv {currentLevel}");
            }

            if (_xpFill != null)
            {
                _xpFill.fillAmount = LordLevelService.ProgressInLevel(_arg.Profile.LordXp, _arg.Config);
            }
            if (_xpNextText != null)
            {
                _xpNextText.SetText(BuildXpNextText(_arg.Profile.LordXp, _arg.Config));
            }

            List<LordRewardCellData> data = BuildCellData(_arg.Profile, _arg.Config);
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        //# "다음 레벨까지 380 XP" — 최대 레벨이면 "최대 레벨".
        public static string BuildXpNextText(int xp, MetaConfig cfg)
        {
            int remain = LordLevelService.XpToNextLevel(xp, cfg);
            return remain > 0 ? $"다음 레벨까지 {remain:N0} XP" : "최대 레벨";
        }

        public static List<LordRewardCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<LordRewardCellData> list = new List<LordRewardCellData>();
            if (profile == null || cfg == null)
                return list;

            int currentLevel = LordLevelService.LevelFromXp(profile.LordXp, cfg);
            List<LordRewardDef> sorted = new List<LordRewardDef>(cfg.LordRewards);
            sorted.Sort((a, b) => a.Level.CompareTo(b.Level));

            int currentReward = 0;
            foreach (LordRewardDef def in sorted)
            {
                if (def != null && def.IsLockedDummy == false && currentLevel >= def.Level && def.Level > currentReward)
                {
                    currentReward = def.Level;
                }
            }

            foreach (LordRewardDef def in sorted)
            {
                if (def == null)
                    continue;
                bool reached = currentLevel >= def.Level;
                bool isCurrent = def.IsLockedDummy == false && def.Level == currentReward;
                string subText = string.Empty;
                if (def.IsLockedDummy == false)
                {
                    subText = reached
                        ? (isCurrent ? "현재 레벨" : "수령 완료")
                        : $"{Mathf.Max(0, LordLevelService.XpForLevel(def.Level, cfg) - profile.LordXp):N0} XP 남음";
                }
                list.Add(new LordRewardCellData
                {
                    IsCurrent = isCurrent,
                    SubText = subText,
                    LevelText = $"Lv {def.Level}",
                    DisplayName = def.IsLockedDummy ? "??? — 추후 해금" : def.DisplayName,
                    RewardText = def.IsLockedDummy ? string.Empty : $"+{def.RewardSouls} 소울",
                    Reached = currentLevel >= def.Level,
                    IsLockedDummy = def.IsLockedDummy,
                });
            }
            return list;
        }
    }
}
