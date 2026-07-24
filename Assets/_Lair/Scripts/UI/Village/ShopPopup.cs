using System;
using System.Collections.Generic;
using System.Text;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ShopPopupArg : UIArg
    {
        public ShopService Shop;
        public MetaProfile Profile;
        public MetaConfig Config;
        public Action OnPurchased;   //# 구매 성공 시 — VillageController 가 저장 + VM 갱신
    }

    //# 셀 표시 데이터 — BuildCellData 가 가공 (EditMode 테스트 대상).
    public class ShopItemCellData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string LevelText;          //# "Lv 2/5"
        public int Price;                 //# 만렙이면 0
        public bool IsMax;
        public bool CanBuy;               //# 만렙 아님 + 소울 충분
        public Action<string> OnBuy;      //# 셀 구매 버튼 → 팝업 핸들러

        //# 종족 강화 셀 전용 (monster-species-enhancement §5.3). 글로벌 항목이면 Species=null·Icon=null → 아이콘/발광 프레임 숨김.
        public EMonster? Species;         //# null = 글로벌 항목
        public int Level;                 //# 발광 프레임 밝기 계산용 (현재 레벨)
        public int MaxLevel;              //# 발광 프레임 밝기 정규화 분모
        public Sprite Icon;               //# 종족 아이콘 — ShopPopup.Rebuild 가 인스펙터 스프라이트 주입 (BuildCellData 는 미설정)
    }

    //# 소울 상점 — 레벨제 영구 업그레이드 목록 (기획서 §3) + 종족 강화 2탭 (monster-species-enhancement §5).
    public class ShopPopup : UIBase
    {
        //# 상점 탭 — 단일 시스템(ShopPopup) 내부 enum (Rule 02 §8 예외 — 파일 내 정의).
        public enum ShopTab { Stat, Species }

        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _soulText;     //# 잔액 "N 소울"
        [SerializeField] private CHText _bonusSummaryText;   //# 상단 요약줄 — "현재 강화  HP +10% · 공속 +5%"
        [SerializeField] private ShopItemPoolingScrollView _scrollView;

        //# 탭 버튼 2개 — 「스탯 강화」 / 「몬스터 강화」 (monster-species-enhancement §9).
        [SerializeField] private CHButton _statTabButton;
        [SerializeField] private CHButton _speciesTabButton;
        //# 선택 탭 강조용 배경 이미지 — CHButton 은 image 를 노출하지 않으므로 별도 참조로 tint.
        [SerializeField] private Image _statTabBg;
        [SerializeField] private Image _speciesTabBg;

        //# 종족 → 강화 셀 아이콘 — 인스펙터 직접 참조 (CodexPopup.SpeciesIcon 관례, Addressables 키 아님).
        [SerializeField] private Sprite _wispIcon;
        [SerializeField] private Sprite _wraithIcon;
        [SerializeField] private Sprite _reaperIcon;
        [SerializeField] private Sprite _hexIcon;
        [SerializeField] private Sprite _plagueIcon;
        [SerializeField] private Sprite _phantomIcon;

        //# 선택 탭 강조 색 — 활성 노랑 (#FBBF24) / 비활성 회색 (#9CA3AF).
        private static readonly Color TabActiveColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color TabInactiveColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        private ShopPopupArg _arg;
        private ShopTab _tab = ShopTab.Stat;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as ShopPopupArg;
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

            //# 탭 — 열 때 기본 스탯 탭. 클릭 시 필터 교체 후 재빌드.
            _tab = ShopTab.Stat;
            if (_statTabButton != null)
            {
                _statTabButton.OnClick(() => SelectTab(ShopTab.Stat), closeDisposable);
            }
            if (_speciesTabButton != null)
            {
                _speciesTabButton.OnClick(() => SelectTab(ShopTab.Species), closeDisposable);
            }
            UpdateTabHighlight();

            //# prefab active 저장 케이스 보강 — BuildModalPopup 과 동일 (layout 산정 후 Build).
            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        //# 탭 전환 — 같은 스크롤뷰·셀을 공유하고 필터 데이터만 교체 (monster-species-enhancement §5.1).
        private void SelectTab(ShopTab tab)
        {
            if (_tab == tab)
                return;
            _tab = tab;
            UpdateTabHighlight();
            Rebuild();
        }

        //# 선택 탭 강조 — 활성 노랑 / 비활성 회색 (§9). 배경 이미지 tint (미배선이면 skip).
        private void UpdateTabHighlight()
        {
            if (_statTabBg != null)
            {
                _statTabBg.color = _tab == ShopTab.Stat ? TabActiveColor : TabInactiveColor;
            }
            if (_speciesTabBg != null)
            {
                _speciesTabBg.color = _tab == ShopTab.Species ? TabActiveColor : TabInactiveColor;
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
            if (_soulText != null)
            {
                _soulText.SetText($"{_arg.Profile.Souls:N0} 소울");
            }
            if (_bonusSummaryText != null)
            {
                _bonusSummaryText.SetText(BuildSummaryText(_arg.Profile, _arg.Config));
            }

            List<ShopItemCellData> data = BuildCellData(_arg.Profile, _arg.Config, _tab);
            foreach (ShopItemCellData cell in data)
            {
                cell.OnBuy = HandleBuy;
                //# 아이콘 주입 — BuildCellData 는 Sprite 의존 없이 순수 데이터만 채우므로 여기서 인스펙터 스프라이트 주입(§5.3).
                cell.Icon = SpeciesIcon(cell.Species);
            }
            if (_scrollView != null)
            {
                _scrollView.SetItemList(data);
            }
        }

        //# 종족 → 강화 셀 아이콘 매핑 (인스펙터 직접 참조). 글로벌 항목(null)이면 null → 셀이 아이콘/발광 프레임 숨김.
        private Sprite SpeciesIcon(EMonster? species) => species switch
        {
            EMonster.Wisp    => _wispIcon,
            EMonster.Wraith  => _wraithIcon,
            EMonster.Reaper  => _reaperIcon,
            EMonster.Hex     => _hexIcon,
            EMonster.Plague  => _plagueIcon,
            EMonster.Phantom => _phantomIcon,
            _                => null,
        };

        private void HandleBuy(string itemId)
        {
            if (_arg == null || _arg.Shop == null)
                return;
            if (_arg.Shop.Buy(itemId) == false)
                return;

            //# 구매 전 최상단 인덱스 기억 — Rebuild 의 SetItemList 가 스크롤을 맨 위로 리셋하므로 직후 복원한다.
            int keepIndex = _scrollView != null ? _scrollView.FirstVisibleIndex : 0;
            _arg.OnPurchased?.Invoke();
            Rebuild();
            if (_scrollView != null)
            {
                //# duration 기본 0 → 같은 프레임 즉시 스냅 복원이라 튀는 게 보이지 않는다.
                _scrollView.SetScrollPosition(keepIndex);
            }
        }

        //# 표시 문자열 조립 — "현재 강화  HP +10% · 공속 +5% · 스폰률 +8%" / 강화 없으면 "현재 강화  아직 없음" (기획서 §2.2).
        //# 접두("현재 강화" + 더블 스페이스)·구분자(" · ")·"아직 없음" 은 동적 문구 → 코드 리터럴 (기획서 §7 ②표). 문구는 기획서가 SoT.
        public static string BuildSummaryText(MetaProfile profile, MetaConfig cfg)
        {
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(profile, cfg);
            if (lines.Count == 0)
                return "현재 강화  아직 없음";

            StringBuilder sb = new StringBuilder("현재 강화  ");
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" · ");
                }
                sb.Append(lines[i].Label).Append(" +").Append(lines[i].Percent).Append('%');
            }
            return sb.ToString();
        }

        //# 데이터 가공 — 전 항목(무탭). 하위호환 유지 (기존 테스트/호출부). Icon 은 미설정(Rebuild 가 주입).
        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<ShopItemCellData> list = new List<ShopItemCellData>();
            if (profile == null || cfg == null)
                return list;

            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;
                list.Add(MakeCell(def, profile));
            }
            return list;
        }

        //# 탭 필터 오버로드 — 스탯 탭은 글로벌(MonsterStat/SpawnerPeriod)만, 몬스터 탭은 MonsterSpecies 만 (monster-species-enhancement §5).
        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg, ShopTab tab)
        {
            List<ShopItemCellData> list = new List<ShopItemCellData>();
            if (profile == null || cfg == null)
                return list;

            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;
                if (MatchesTab(def.EffectKind, tab) == false)
                    continue;
                list.Add(MakeCell(def, profile));
            }
            return list;
        }

        private static bool MatchesTab(EShopEffectKind kind, ShopTab tab)
            => tab == ShopTab.Species
                ? kind == EShopEffectKind.MonsterSpecies
                : kind == EShopEffectKind.MonsterStat || kind == EShopEffectKind.SpawnerPeriod;

        //# 셀 표시 데이터 조립 단일 진실 — 두 오버로드가 공유. Species/Level/MaxLevel 은 채우되 Icon 은 Rebuild 가 주입(§5.3).
        private static ShopItemCellData MakeCell(ShopItemDef def, MetaProfile profile)
        {
            int level = profile.GetShopLevel(def.Id);
            bool isMax = level >= def.MaxLevel;
            int price = isMax ? 0 : ShopService.PriceOf(def, level);
            return new ShopItemCellData
            {
                Id = def.Id,
                DisplayName = def.DisplayName,
                Description = def.Description,
                LevelText = $"Lv {level}/{def.MaxLevel}",
                Price = price,
                IsMax = isMax,
                CanBuy = isMax == false && profile.Souls >= price,
                Species = def.EffectKind == EShopEffectKind.MonsterSpecies ? def.Species : (EMonster?)null,
                Level = level,
                MaxLevel = def.MaxLevel,
            };
        }
    }
}
