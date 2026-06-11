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
    }

    //# 영주성 — 레벨 보상 트랙 Lv2~10 표시 전용 (기획서 §4).
    public class LordLevelPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _lordLevelText;   //# 헤더 — "영주 Lv 3"
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
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);

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
                _lordLevelText.SetText($"영주 Lv {currentLevel}");

            List<LordRewardCellData> data = BuildCellData(_arg.Profile, _arg.Config);
            if (_scrollView != null)
                _scrollView.SetItemList(data);
        }

        public static List<LordRewardCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<LordRewardCellData> list = new List<LordRewardCellData>();
            if (profile == null || cfg == null)
                return list;

            int currentLevel = LordLevelService.LevelFromXp(profile.LordXp, cfg);
            List<LordRewardDef> sorted = new List<LordRewardDef>(cfg.LordRewards);
            sorted.Sort((a, b) => a.Level.CompareTo(b.Level));

            foreach (LordRewardDef def in sorted)
            {
                if (def == null)
                    continue;
                list.Add(new LordRewardCellData
                {
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
