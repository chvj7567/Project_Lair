using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class QuestPopupArg : UIArg
    {
        public MetaProfile Profile;
        public MetaConfig Config;
    }

    //# 셀 표시 데이터 — 도전과제 이름/설명/보상/달성 여부.
    public class QuestCellData
    {
        public string DisplayName;
        public string Description;
        public string RewardText;   //# "+30 소울"
        public bool Achieved;
        public bool HasProgress;    //# 누적형(Threshold≥2) 미달성일 때만 true (기획서 §3.1)
        public int Current;         //# 현재 누적값 (Target 으로 클램프)
        public int Target;          //# 달성 임계
    }

    //# 고정 도전과제 13개 목록 (기획서 §5).
    public class QuestPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private QuestPoolingScrollView _scrollView;

        private QuestPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as QuestPopupArg;
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

            List<QuestCellData> data = BuildCellData(_arg.Profile, _arg.Config);
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        public static List<QuestCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<QuestCellData> list = new List<QuestCellData>();
            if (profile == null || cfg == null)
                return list;

            foreach (AchievementDef def in cfg.Achievements)
            {
                if (def == null)
                    continue;

                bool achieved = profile.AchievedIds.Contains(def.Id);
                //# 진행 바 3조건 AND (기획서 §3.1) — 누적형 + 장기(Threshold≥2, FirstRun carve-out) + 미달성.
                bool cumulative = def.Condition == EAchievementCondition.TotalWins
                               || def.Condition == EAchievementCondition.TotalRuns;
                int target = cumulative ? Mathf.Max(0, (int)def.Threshold) : 0;
                int raw = def.Condition == EAchievementCondition.TotalWins ? profile.TotalWins
                        : def.Condition == EAchievementCondition.TotalRuns ? profile.TotalRuns : 0;
                int current = cumulative ? Mathf.Min(raw, target) : 0;   //# §3.2 현재값은 목표로 클램프 (세이브 변조 방어)

                list.Add(new QuestCellData
                {
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    RewardText = $"+{def.RewardSouls} 소울",
                    Achieved = achieved,
                    HasProgress = cumulative && target >= 2 && achieved == false,
                    Current = current,
                    Target = target,
                });
            }
            return list;
        }
    }
}
