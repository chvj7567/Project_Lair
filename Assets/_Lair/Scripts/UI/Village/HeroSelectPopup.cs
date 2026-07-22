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

    //# 셀 표시 데이터 — 스테이지 1~5 영웅 외형 (초상 + 스테이지 틴트). 표시 전용 (선택은 마을 캐러셀 담당).
    public class HeroSelectCellData
    {
        public string DisplayName;     //# 해금 "스테이지 N" / 잠금 "스테이지 N — 잠금"
        public bool IsLocked;
        public Sprite Portrait;        //# 영웅 초상 (잠금이어도 어둡게 표시)
        public Color PortraitTint;     //# 스테이지 틴트, 잠금이면 어둠 반영
    }

    //# 영웅 목록 — 스테이지 1~5 의 영웅 외형을 나열하는 표시 전용 팝업 (hero-stage-variant 기획서 §1.2/§4.3).
    public class HeroSelectPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private HeroSelectPoolingScrollView _scrollView;

        //# 영웅 초상 — 인스펙터 직접 참조 (SpawnerStatusCell 관례, Addressables 키 아님 — 캐릭터 프리팹 "Knight" 주소와 충돌 방지).
        //# 스켈레톤 1모델 재스킨이므로 5스테이지가 같은 초상을 스테이지 틴트만 달리해 공유한다.
        [SerializeField] private Sprite _knightPortrait;

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

            List<HeroSelectCellData> data = BuildCellData(_arg.Profile, _arg.VariantConfig, _knightPortrait);
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        //# 스테이지 1~5 셀 — 해금은 스테이지 틴트 초상, 잠금은 같은 초상을 어둡게 + 잠금 표기.
        //# profile null 이면 진행도 0, variantConfig null 이면 틴트 없음(흰색)으로 폴백.
        public static List<HeroSelectCellData> BuildCellData(MetaProfile profile, HeroStageVariantConfig variantConfig, Sprite portrait)
        {
            List<HeroSelectCellData> list = new List<HeroSelectCellData>();
            int cleared = profile != null ? profile.ClearedStage : 0;

            for (int stage = 1; stage <= StageProgress.MaxStage; ++stage)
            {
                //# 해금 판정은 캐러셀과 같은 단일 소유 헬퍼 (기획서 §4.6).
                bool unlocked = StageProgress.IsUnlocked(stage, cleared);
                Color tint = variantConfig != null ? variantConfig.GetStage(stage).TintColor : Color.white;
                list.Add(new HeroSelectCellData
                {
                    DisplayName = unlocked ? $"스테이지 {stage}" : $"스테이지 {stage} — 잠금",
                    IsLocked = unlocked == false,
                    Portrait = portrait,
                    PortraitTint = unlocked ? tint : Color.Lerp(tint, Color.black, LockedDimRatio),
                });
            }
            return list;
        }
    }
}
