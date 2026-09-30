using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class HeroSelectPopupArg : UIArg
    {
        public MetaProfile Profile;
        public HeroStageVariantConfig VariantConfig;
    }

    //# 셀 표시 데이터 — 스테이지 1~5 영웅 외형 (스테이지별 초상). 표시 전용 (선택은 마을 캐러셀 담당).
    public class HeroSelectCellData
    {
        public string SubText;         //# 보조 줄 — 해금 "N단계" / 잠금 "잠김"
        public string DisplayName;     //# 해금 "스테이지 N" / 잠금 "스테이지 N — 잠금"
        public bool IsLocked;
        public Sprite Portrait;        //# 스테이지 영웅 초상 (잠금이어도 어둡게 표시)
        public Color PortraitTint;     //# 해금 흰색(원본 그대로), 잠금이면 어둠 반영
    }

    //# 영웅 목록 — 스테이지 1~5 의 영웅 외형을 나열하는 표시 전용 팝업 (hero-stage-variant 기획서 §1.2/§4.3).
    public class HeroSelectPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private HeroSelectPoolingScrollView _scrollView;

        //# 잠금 스테이지 어둠 비율 — 캐러셀 잠금 오버레이(검정 α0.55, 기획서 §4.3) 와 동일 톤.
        public const float LockedDimRatio = 0.55f;

        private HeroSelectPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as HeroSelectPopupArg;
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
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Rebuild();
        }

        private void Rebuild()
        {
            if (_arg == null)
                return;

            List<HeroSelectCellData> data = BuildCellData(_arg.Profile, _arg.VariantConfig);
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        //# 스테이지 1~5 셀 — 스테이지별 초상, 잠금은 같은 초상을 어둡게 + 잠금 표기.
        //# profile null 이면 진행도 0, variantConfig null 이면 초상 없음(셀이 이미지를 숨김)으로 폴백.
        public static List<HeroSelectCellData> BuildCellData(MetaProfile profile, HeroStageVariantConfig variantConfig)
        {
            List<HeroSelectCellData> list = new List<HeroSelectCellData>();
            int cleared = profile != null ? profile.ClearedStage : 0;

            for (int stage = 1; stage <= StageProgress.MaxStage; ++stage)
            {
                //# 해금 판정은 캐러셀과 같은 단일 소유 헬퍼 (기획서 §3.1).
                bool unlocked = StageProgress.IsUnlocked(stage, cleared);
                Sprite portrait = variantConfig != null ? variantConfig.GetStage(stage).Portrait : null;
                list.Add(new HeroSelectCellData
                {
                    SubText = unlocked ? $"{stage}단계" : "잠김",
                    DisplayName = unlocked ? $"스테이지 {stage}" : $"스테이지 {stage} — 잠금",
                    IsLocked = unlocked == false,
                    Portrait = portrait,
                    PortraitTint = unlocked ? Color.white : Color.Lerp(Color.white, Color.black, LockedDimRatio),
                });
            }
            return list;
        }
    }
}
