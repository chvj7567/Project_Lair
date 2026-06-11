using System;
using System.Collections.Generic;
using ChvjUnityInfra;
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
        public MetaConfig Config;
        public Action<EHero> OnSelected;
    }

    //# 셀 표시 데이터 — Knight 1종 + HeroLockedSlots 잠금 더미 (기획서 §6).
    public class HeroSelectCellData
    {
        public string DisplayName;     //# 잠금 더미면 "??? — 추후 해금"
        public bool IsLocked;
        public bool IsSelected;
        public EHero Hero;
        public Sprite Portrait;        //# 영웅 초상 (잠금 더미는 null → 기존 표시 유지)
        public Action<EHero> OnSelect;
    }

    //# 영웅 선택 — 선택 영웅이 마을 중앙 모델·다음 런 상대로 반영 (spec §5.4).
    public class HeroSelectPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private HeroSelectPoolingScrollView _scrollView;

        //# 영웅 → 초상 — 인스펙터 직접 참조 (SpawnerStatusCell 관례, Addressables 키 아님 — 캐릭터 프리팹 "Knight" 주소와 충돌 방지).
        [SerializeField] private Sprite _knightPortrait;

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

            List<HeroSelectCellData> data = BuildCellData(_arg.Profile, _arg.Config, HeroPortrait);
            foreach (HeroSelectCellData cell in data)
            {
                cell.OnSelect = HandleSelect;
            }
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        private void HandleSelect(EHero hero)
        {
            if (_arg == null)
                return;
            _arg.OnSelected?.Invoke(hero);
            Rebuild();   //# 선택 강조 갱신
        }

        //# 영웅 → 초상 매핑 (인스펙터 직접 참조). 미할당이면 null → 셀이 초상 숨김.
        private Sprite HeroPortrait(EHero hero) => hero switch
        {
            EHero.Knight => _knightPortrait,
            _            => null,
        };

        public static List<HeroSelectCellData> BuildCellData(MetaProfile profile, MetaConfig cfg, Func<EHero, Sprite> portraitResolver)
        {
            List<HeroSelectCellData> list = new List<HeroSelectCellData>();
            if (profile == null || cfg == null)
                return list;

            //# v0.2 — 선택 가능 영웅은 Knight 1종 (spec §5.4).
            foreach (EHero hero in (EHero[])Enum.GetValues(typeof(EHero)))
            {
                list.Add(new HeroSelectCellData
                {
                    DisplayName = hero.ToString(),
                    IsLocked = false,
                    IsSelected = profile.SelectedHero == hero.ToString(),
                    Hero = hero,
                    Portrait = portraitResolver != null ? portraitResolver(hero) : null,
                });
            }
            for (int i = 0; i < cfg.HeroLockedSlots; ++i)
            {
                list.Add(new HeroSelectCellData
                {
                    DisplayName = "??? — 추후 해금",
                    IsLocked = true,
                });
            }
            return list;
        }
    }
}
