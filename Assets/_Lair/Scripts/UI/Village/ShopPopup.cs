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
        public ShopPopup.ShopRowKind RowKind;   //# 기본 Item — 섹션 헤더 행이면 SectionHeader (기획서 §7).
        public string HeaderText;               //# 헤더 행일 때 섹션 제목 ("스탯 강화"/"몬스터 강화").
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

    //# 소울 상점 — 레벨제 영구 업그레이드 단일 스크롤 + 섹션 헤더 (기획서 §1). 탭 제거.
    public class ShopPopup : UIBase
    {
        //# 행 종류 — 단일 시스템(ShopPopup) 내부 enum (Rule 02 §8 예외). Item 을 0(기본값)으로 두어 MakeCell 미설정 항목이 항목 행이 되게 한다.
        public enum ShopRowKind { Item, SectionHeader }

        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _soulText;     //# 잔액 "N 소울"
        [SerializeField] private CHText _bonusSummaryText;   //# 상단 요약줄 — "현재 강화  HP +10% · 공속 +5%"
        [SerializeField] private ShopItemPoolingScrollView _scrollView;

        //# 종족 → 강화 셀 아이콘 — 인스펙터 직접 참조 (CodexPopup.SpeciesIcon 관례, Addressables 키 아님).
        [SerializeField] private Sprite _wispIcon;
        [SerializeField] private Sprite _wraithIcon;
        [SerializeField] private Sprite _reaperIcon;
        [SerializeField] private Sprite _hexIcon;
        [SerializeField] private Sprite _plagueIcon;
        [SerializeField] private Sprite _phantomIcon;

        private ShopPopupArg _arg;

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

            //# prefab active 저장 케이스 보강 — BuildModalPopup 과 동일 (layout 산정 후 Build).
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
            if (_soulText != null)
            {
                _soulText.SetText($"{_arg.Profile.Souls:N0} 소울");
            }
            if (_bonusSummaryText != null)
            {
                _bonusSummaryText.SetText(BuildSummaryText(_arg.Profile, _arg.Config));
            }

            List<ShopItemCellData> data = BuildCellData(_arg.Profile, _arg.Config);
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

        //# 데이터 가공 — 단일 스크롤 통합 리스트: [스탯 헤더 + 스탯 항목 + 몬스터 헤더 + 종족 항목] (기획서 §1·7).
        //# 빈 섹션은 헤더도 제외(§5). Icon 은 미설정(Rebuild 가 주입).
        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<ShopItemCellData> list = new List<ShopItemCellData>();
            if (profile == null || cfg == null)
                return list;

            //# 스탯 강화 섹션 (전종 글로벌: MonsterStat/SpawnerPeriod) → 몬스터 강화 섹션 (MonsterSpecies).
            AppendSection(list, cfg, profile, "스탯 강화", isSpecies: false);
            AppendSection(list, cfg, profile, "몬스터 강화", isSpecies: true);
            return list;
        }

        //# 섹션 항목을 먼저 모아 1개 이상일 때만 [헤더 + 항목들] append — 빈 섹션 헤더 제외 (기획서 §5).
        private static void AppendSection(List<ShopItemCellData> list, MetaConfig cfg, MetaProfile profile, string headerText, bool isSpecies)
        {
            List<ShopItemCellData> items = new List<ShopItemCellData>();
            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;
                bool species = def.EffectKind == EShopEffectKind.MonsterSpecies;
                if (species != isSpecies)
                    continue;
                items.Add(MakeCell(def, profile));
            }
            if (items.Count == 0)
                return;
            list.Add(new ShopItemCellData { RowKind = ShopRowKind.SectionHeader, HeaderText = headerText });
            list.AddRange(items);
        }

        //# 셀 표시 데이터 조립 단일 진실. Species/Level/MaxLevel 은 채우되 Icon 은 Rebuild 가 주입(§5.3).
        private static ShopItemCellData MakeCell(ShopItemDef def, MetaProfile profile)
        {
            int level = profile.GetShopLevel(def.Id);
            bool isMax = level >= def.MaxLevel;
            int price = isMax ? 0 : ShopService.PriceOf(def, level);
            return new ShopItemCellData
            {
                RowKind = ShopRowKind.Item,
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
