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

        //# 종족 행 4칸 (monster-growth-currencies §8.1) — [체급, 각성, 둥지, 특성]. 종족 행에서만 non-null. 항목이 없는 축은 IsAvailable=false.
        public ShopAxisSlotData[] Slots;
        public string SpeciesName;
        public Action<ShopAxisSlotData> OnSlotClick;   //# 칸 클릭 → 팝업 핸들러 (구매·부족·잠김 토스트 분기)
    }

    //# 종족 행의 육성 칸 하나 표시 데이터 — BuildCellData 가 가공, 아이콘은 Rebuild 가 주입.
    public class ShopAxisSlotData
    {
        public string AxisName;
        public bool IsAvailable;          //# 이 축의 상점 항목이 있는가 (없으면 잠긴 칸으로 표시)
        public string ItemId;
        public ECurrency Currency;
        public string CurrencyName;
        public string ColorHex;
        public string EffectText;
        public int Level;
        public int MaxLevel;
        public int Price;                 //# 만렙이면 0
        public bool IsMax;
        public bool CanBuy;               //# 만렙 아님 + 해금 + 재화 충분
        public bool IsLocked;             //# 재화 미해금 (또는 항목·육성 데이터 없음)
        public string LockText;           //# "1-1 클리어 시 해금" — 데이터가 없으면 빈 문자열
        public Sprite CurrencyIcon;
        public bool TintIcon;             //# 전용 아이콘이 없어 소울 코인을 재화 색으로 물들인 임시 표시
        public bool IsTierUpgrade;

        //# 승급 칸 (monster-grades §5.1) — 상점 항목이 아니라 GradePromotion 으로 구매. 가격은 재화 2종이라 PriceText 로 표시(아이콘 없음).
        public bool IsPromotion;
        public ERace Race;
        public string PriceText;          //# 비어 있지 않으면 가격 띠에 숫자 대신 이 문구 ("소울 1,500 · 각성석 30" / "최고 등급")
        public ECurrency LackingCurrency; //# 재화 부족 토스트용 — 처음 모자라는 재화
    }

    //# 체급 칸의 표시 모델 — 구매 데이터와 재화 상태는 ShopService 가 결정한다.
    internal static class ShopTierPresentation
    {
        private const int StrAxisName = 1870;
        private const int StrBaseTier = 1871;
        private const int StrPreviewStats = 1872;
        private const int StrMaxTier = 1873;
        private const int StrMissingSouls = 1874;
        private const int StrUnavailable = 1875;
        private const int StrCurrentStats = 1878;

        public static void Bind(ShopAxisSlotData slot, ShopService.TierOffer offer, Func<int, string> text)
        {
            string current = Name(offer.CurrentTier, text);
            slot.AxisName = Label(text, StrAxisName, "티어 강화");
            slot.IsTierUpgrade = true;
            slot.Level = offer.CurrentTier;
            slot.MaxLevel = EnhanceTierTable.MaxTier;
            slot.IsMax = offer.IsMax;
            slot.IsLocked = offer.IsLocked;
            slot.CanBuy = offer.CanBuy;
            slot.Price = offer.SoulCost;
            slot.EffectText = offer.HasNext
                ? string.Format(Label(text, StrPreviewStats, "{0} HP·공격력\n×{1:0.00} → ×{2:0.00}"),
                    current + "→" + Name(offer.NextTier, text), offer.CurrentMultiplier, offer.NextMultiplier)
                : string.Format(Label(text, StrCurrentStats, "{0} HP·공격력\n×{1:0.00}"), current, offer.CurrentMultiplier);
            slot.PriceText = PriceText(offer, text);
        }

        public static string Name(int tier, Func<int, string> text)
            => tier == 0 ? Label(text, StrBaseTier, "기본") : $"T{tier}";

        private static string PriceText(ShopService.TierOffer offer, Func<int, string> text)
        {
            if (offer.IsMax)
                return Label(text, StrMaxTier, "최대 티어");
            if (offer.HasNext == false)
                return Label(text, StrUnavailable, "강화 준비 중");
            if (offer.MissingSouls > 0)
                return string.Format(Label(text, StrMissingSouls, "{0:N0} 소울 · {1:N0} 부족"), offer.SoulCost, offer.MissingSouls);
            return string.Empty;
        }

        private static string Label(Func<int, string> text, int id, string fallback)
        {
            string localized = text?.Invoke(id);
            return string.IsNullOrEmpty(localized) ? fallback : localized;
        }
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

        //# 재화 아이콘 (monster-growth-currencies §11.4) — 전용 스프라이트 미할당이면 소울 코인을 재화 색으로 물들여 쓴다.
        [SerializeField] private Sprite _soulIcon;
        [SerializeField] private Sprite _awakenStoneIcon;
        [SerializeField] private Sprite _moonSealIcon;
        [SerializeField] private Sprite _abyssCrystalIcon;
        //# 지갑 알약 112x32 (§8.1) — [각성석, 달빛 인장, 심연 결정] 순, 소울 알약 왼쪽. 해금된 재화만 표시.
        [SerializeField] private CurrencyPillView[] _walletPills = new CurrencyPillView[0];

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
            CurrencyPillData[] wallet = BuildWalletData(_arg.Profile, _arg.Shop, GrowthDatabase.Current);
            for (int i = 0; i < _walletPills.Length; ++i)
            {
                if (_walletPills[i] == null)
                    continue;
                CurrencyPillData pill = i < wallet.Length ? wallet[i] : null;
                if (pill != null)
                {
                    Sprite own = CurrencyIcon(pill.Currency);
                    pill.Icon = own != null ? own : _soulIcon;
                    pill.TintIcon = own == null;
                }
                _walletPills[i].Bind(pill);
            }
            if (_soulText != null)
            {
                _soulText.SetText($"{_arg.Profile.Souls:N0} 소울");
            }
            if (_bonusSummaryText != null)
            {
                _bonusSummaryText.SetText(BuildSummaryText(_arg.Profile, _arg.Config));
            }

            List<ShopItemCellData> data = BuildCellData(_arg.Profile, _arg.Config, _arg.Shop, GrowthDatabase.Current, LookupString);
            foreach (ShopItemCellData cell in data)
            {
                cell.OnBuy = HandleBuy;
                cell.OnSlotClick = HandleSlotClick;
                //# 아이콘 주입 — BuildCellData 는 Sprite 의존 없이 순수 데이터만 채우므로 여기서 인스펙터 스프라이트 주입(§5.3).
                cell.Icon = SpeciesIcon(cell.Species);
                if (cell.Slots == null)
                    continue;
                foreach (ShopAxisSlotData slot in cell.Slots)
                {
                    Sprite own = CurrencyIcon(slot.Currency);
                    slot.CurrencyIcon = own != null ? own : _soulIcon;
                    slot.TintIcon = own == null && slot.Currency != ECurrency.Soul;
                }
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

        private Sprite CurrencyIcon(ECurrency currency) => currency switch
        {
            ECurrency.Soul         => _soulIcon,
            ECurrency.AwakenStone  => _awakenStoneIcon,
            ECurrency.MoonSeal     => _moonSealIcon,
            ECurrency.AbyssCrystal => _abyssCrystalIcon,
            _                      => null,
        };

        //# 칸 클릭 (§8.1 칸 상태 표) — 구매 가능 = 1탭 구매 / 재화 부족 = 1482 / 잠김 = 1331 / 만렙·항목 없음 = 무반응.
        private void HandleSlotClick(ShopAxisSlotData slot)
        {
            if (slot == null || slot.IsAvailable == false || slot.IsMax)
                return;
            if (slot.IsPromotion)
            {
                HandlePromote(slot);
                return;
            }
            if (slot.IsLocked)
            {
                if (string.IsNullOrEmpty(slot.LockText) == false)
                {
                    ToastView.Show(slot.LockText, EToastKind.Warning);
                }
                return;
            }
            if (slot.CanBuy == false)
            {
                ToastView.Show(string.Format(LookupString(GrowthStrings.CurrencyShort), slot.CurrencyName), EToastKind.Warning);
                return;
            }
            HandleBuy(slot.ItemId);
        }

        private static string LookupString(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        public static readonly ECurrency[] WalletCurrencies = { ECurrency.AwakenStone, ECurrency.MoonSeal, ECurrency.AbyssCrystal };

        //# 지갑 알약 데이터 — WalletCurrencies 와 같은 인덱스, 잠긴 재화는 null(알약 숨김). 잔해는 상점에서 쓰지 않아 제외(§8.1). 아이콘은 Rebuild 가 주입.
        public static CurrencyPillData[] BuildWalletData(MetaProfile profile, ShopService shop, GrowthDatabase growth)
        {
            CurrencyPillData[] pills = new CurrencyPillData[WalletCurrencies.Length];
            if (profile == null || shop == null || growth == null)
                return pills;
            for (int i = 0; i < WalletCurrencies.Length; ++i)
            {
                ECurrency currency = WalletCurrencies[i];
                if (shop.IsCurrencyLocked(currency))
                    continue;
                pills[i] = new CurrencyPillData
                {
                    Currency = currency,
                    AmountText = profile.GetCurrency(currency).ToString("N0"),
                    ColorHex = GrowthText.CurrencyColorHex(currency, growth),
                };
            }
            return pills;
        }

        //# 승급 칸 — 재화 부족이면 1482 토스트, 성공하면 구매와 같은 저장·갱신 경로.
        private void HandlePromote(ShopAxisSlotData slot)
        {
            if (_arg == null)
                return;
            if (slot.CanBuy == false)
            {
                ToastView.Show(string.Format(LookupString(GrowthStrings.CurrencyShort),
                    GrowthText.CurrencyName(slot.LackingCurrency, GrowthDatabase.Current, LookupString)), EToastKind.Warning);
                return;
            }
            if (GradePromotion.TryPromote(_arg.Profile, FormationDatabase.Current, slot.Race) == false)
                return;
            AfterPurchase();
        }

        private void HandleBuy(string itemId)
        {
            if (_arg == null || _arg.Shop == null)
                return;
            if (_arg.Shop.Buy(itemId) == false)
                return;
            AfterPurchase();
        }

        private void AfterPurchase()
        {
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
            => BuildCellData(profile, cfg, null, GrowthDatabase.Current, null);

        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg, ShopService shop, GrowthDatabase growth, Func<int, string> text)
            => BuildCellData(profile, cfg, shop, growth, text, FormationDatabase.Current);

        //# shop 이 null 이면 기본 해금 규칙의 ShopService 로 칸 잠김을 판정한다. text 는 칸 문구용(null 이면 축 이름 기본값).
        //# formation 이 있으면 종족 행에 승급 칸·등급 접두어 이름을 붙인다(monster-grades §5, dungeon-formation §0.2). null 이면 기존 4칸.
        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg, ShopService shop, GrowthDatabase growth, Func<int, string> text,
            FormationDatabase formation)
        {
            List<ShopItemCellData> list = new List<ShopItemCellData>();
            if (profile == null || cfg == null)
                return list;
            ShopService service = shop ?? new ShopService(profile, cfg);

            //# 스탯 강화 섹션 (전종 글로벌: MonsterStat/SpawnerPeriod) → 몬스터 강화 섹션 (종족 1행 = 체급 항목 + 같은 종족 육성 3칸 + 승급 칸).
            AppendSection(list, cfg, profile, "스탯 강화", false, service, growth, text, formation);
            AppendSection(list, cfg, profile, "몬스터 강화", true, service, growth, text, formation);
            return list;
        }

        //# 섹션 항목을 먼저 모아 1개 이상일 때만 [헤더 + 항목들] append — 빈 섹션 헤더 제외 (기획서 §5).
        //# 육성 3축 항목(MonsterAwaken/NestMastery/MonsterTrait)은 독립 행을 만들지 않고 같은 종족 체급 행의 칸으로 묶인다(§11.6).
        private static void AppendSection(List<ShopItemCellData> list, MetaConfig cfg, MetaProfile profile, string headerText, bool isSpecies,
            ShopService shop, GrowthDatabase growth, Func<int, string> text, FormationDatabase formation)
        {
            List<ShopItemCellData> items = new List<ShopItemCellData>();
            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;
                if (isSpecies == false)
                {
                    if (def.EffectKind == EShopEffectKind.MonsterStat || def.EffectKind == EShopEffectKind.SpawnerPeriod)
                    {
                        items.Add(MakeCell(def, profile));
                    }
                    continue;
                }
                if (def.EffectKind != EShopEffectKind.MonsterSpecies)
                    continue;
                ShopItemCellData cell = MakeCell(def, profile, shop, text);
                ShopAxisSlotData[] axes =
                {
                    MakeSlot(def, GrowthStrings.AxisBody, "체급", profile, shop, growth, text),
                    MakeSlot(FindAxisItem(cfg, EShopEffectKind.MonsterAwaken, def.Species), GrowthStrings.AxisAwaken, "각성", profile, shop, growth, text),
                    MakeSlot(FindAxisItem(cfg, EShopEffectKind.NestMastery, def.Species), GrowthStrings.AxisNest, "둥지", profile, shop, growth, text),
                    MakeSlot(FindAxisItem(cfg, EShopEffectKind.MonsterTrait, def.Species), GrowthStrings.AxisTrait, "특성", profile, shop, growth, text),
                };
                if (formation != null && formation.TryGetRace(def.Species, out ERace race))
                {
                    //# 행 머리·항목 이름 = 보유 최고 등급 접두어 + 기존 이름(좁은 칸 2줄, §0.2). 아이콘은 기준 몬스터(V7).
                    EMonster top = formation.HighestOwned(race, profile.GetRaceGrade(race));
                    cell.SpeciesName = SpeciesVisual.MonsterNameTwoLine(top, formation, text);
                    string prefix = SpeciesVisual.GradePrefix(MonsterGrades.GradeOf(top), formation, text);
                    cell.DisplayName = string.IsNullOrEmpty(prefix) ? def.DisplayName : prefix + " " + def.DisplayName;
                    cell.Slots = new[] { axes[0], axes[1], axes[2], axes[3], MakePromotionSlot(race, profile, formation, growth, text) };
                }
                else
                {
                    cell.SpeciesName = SpeciesVisual.SpeciesName(def.Species);
                    cell.Slots = axes;
                }
                items.Add(cell);
            }
            if (items.Count == 0)
                return;
            list.Add(new ShopItemCellData { RowKind = ShopRowKind.SectionHeader, HeaderText = headerText });
            list.AddRange(items);
        }

        private static ShopItemDef FindAxisItem(MetaConfig cfg, EShopEffectKind kind, EMonster species)
        {
            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def != null && def.EffectKind == kind && def.Species == species && string.IsNullOrEmpty(def.Id) == false)
                    return def;
            }
            return null;
        }

        //# 칸 하나 조립 — 항목이 없으면 IsAvailable=false + 잠김 표시. 칸 문구: 체급·각성 = Description, 둥지 = 다음 단계 효과, 특성 = 이름 + 단계 값.
        private static ShopAxisSlotData MakeSlot(ShopItemDef def, int axisNameId, string axisFallback, MetaProfile profile,
            ShopService shop, GrowthDatabase growth, Func<int, string> text)
        {
            string axisName = text != null ? text(axisNameId) : null;
            ShopAxisSlotData slot = new ShopAxisSlotData { AxisName = string.IsNullOrEmpty(axisName) ? axisFallback : axisName };
            if (def == null)
            {
                slot.IsLocked = true;
                slot.LockText = string.Empty;
                return slot;
            }
            bool locked = shop.IsCurrencyLocked(def.Currency);
            slot.IsAvailable = true;
            slot.ItemId = def.Id;
            slot.Currency = def.Currency;
            slot.CurrencyName = GrowthText.CurrencyName(def.Currency, growth, text);
            slot.ColorHex = growth == null && def.Currency == ECurrency.Soul ? "#5ef0b4" : GrowthText.CurrencyColorHex(def.Currency, growth);
            slot.LockText = locked ? GrowthText.UnlockText(def.Currency, text) : string.Empty;
            if (shop.TryGetTierOffer(def.Id, out ShopService.TierOffer offer))
            {
                ShopTierPresentation.Bind(slot, offer, text);
                return slot;
            }

            int level = profile.GetShopLevel(def.Id);
            bool isMax = level >= def.MaxLevel;
            int price = isMax ? 0 : ShopService.PriceOf(def, level);
            slot.Level = Mathf.Min(level, def.MaxLevel);
            slot.MaxLevel = def.MaxLevel;
            slot.Price = price;
            slot.IsMax = isMax;
            slot.IsLocked = locked;
            slot.CanBuy = isMax == false && locked == false && profile.GetCurrency(def.Currency) >= price;
            switch (def.EffectKind)
            {
                case EShopEffectKind.NestMastery:
                    slot.EffectText = GrowthText.NestSlotText(level, growth, text);
                    break;
                case EShopEffectKind.MonsterTrait:
                    slot.EffectText = GrowthText.TraitSlotText(def.Species, level, growth, text);
                    break;
                default:
                    slot.EffectText = def.Description;
                    break;
            }
            return slot;
        }

        //# 승급 칸 (monster-grades §5.1·§11 UI) — 제목 "승급" · 다음 등급 문구 · 비용(소울 + 특수 1종) / 군주면 "최고 등급".
        private const string PromotionColorHex = "#f7c64a";
        private const int StrPromote = 1516;            //# 승급
        private const int StrPromoteToFirst = 1517;     //# 견습으로 승급 (1517~1520 = 등급 2~5)
        private const int StrTopGrade = 1521;           //# 최고 등급

        private static ShopAxisSlotData MakePromotionSlot(ERace race, MetaProfile profile, FormationDatabase formation, GrowthDatabase growth, Func<int, string> text)
        {
            string title = text != null ? text(StrPromote) : null;
            ShopAxisSlotData slot = new ShopAxisSlotData
            {
                AxisName = string.IsNullOrEmpty(title) ? "승급" : title,
                IsAvailable = true,
                IsPromotion = true,
                Race = race,
                ColorHex = PromotionColorHex,
            };
            int next = GradePromotion.NextGrade(profile, formation, race);
            if (next == 0)
            {
                slot.IsMax = true;
                slot.EffectText = SpeciesVisual.GradePrefix(profile.GetRaceGrade(race), formation, text);
                slot.PriceText = text != null ? text(StrTopGrade) : "최고 등급";
                return slot;
            }
            IReadOnlyList<GradeCost> costs = formation.PromotionCostOf(next);
            slot.EffectText = text != null ? text(StrPromoteToFirst + next - 2) : string.Empty;
            StringBuilder price = new StringBuilder();
            foreach (GradeCost cost in costs)
            {
                if (price.Length > 0)
                {
                    price.Append(" · ");
                }
                price.Append(GrowthText.CurrencyName(cost.Currency, growth, text)).Append(' ').Append(cost.Amount.ToString("N0"));
            }
            slot.PriceText = price.ToString();
            ECurrency lacking = ECurrency.Soul;
            slot.CanBuy = costs.Count > 0 && GradePromotion.CanAfford(profile, costs, out lacking);
            slot.LackingCurrency = lacking;
            return slot;
        }

        //# 셀 표시 데이터 조립 단일 진실. Species/Level/MaxLevel 은 채우되 Icon 은 Rebuild 가 주입(§5.3).
        private static ShopItemCellData MakeCell(ShopItemDef def, MetaProfile profile, ShopService shop = null, Func<int, string> text = null)
        {
            int level = profile.GetShopLevel(def.Id);
            bool isMax = level >= def.MaxLevel;
            int price = isMax ? 0 : ShopService.PriceOf(def, level);
            ShopItemCellData cell = new ShopItemCellData
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
            if (shop != null && shop.TryGetTierOffer(def.Id, out ShopService.TierOffer offer))
            {
                cell.Price = offer.SoulCost;
                cell.IsMax = offer.IsMax;
                cell.CanBuy = offer.CanBuy;
                cell.Level = EnhanceTierTable.LevelEquivalent(offer.CurrentTier);
                cell.LevelText = ShopTierPresentation.Name(offer.CurrentTier, text);
            }
            return cell;
        }
    }
}
