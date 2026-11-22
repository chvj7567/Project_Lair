using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 — 구매 가능/가격/구매 로직 (기획서 §3). 저장은 호출자(VillageController) 책임.
    //# 결제 재화는 ShopItemDef.Currency — 소울 = Souls, 그 외 = Currencies (monster-growth-currencies §3.6).
    public class ShopService
    {
        public readonly struct TierOffer
        {
            public readonly int CurrentTier;
            public readonly float CurrentMultiplier;
            public readonly int NextLevel;
            public readonly float NextMultiplier;
            public readonly int SoulCost;
            public readonly int MissingSouls;
            public readonly bool IsLocked;
            public bool IsMax => CurrentTier >= EnhanceTierTable.MaxTier;
            public bool HasNext => NextLevel > 0;
            public bool CanBuy => HasNext && IsLocked == false && MissingSouls == 0;
            public int NextTier => HasNext ? CurrentTier + 1 : CurrentTier;

            public TierOffer(int tier, float currentMultiplier, int nextLevel, float nextMultiplier,
                int soulCost, int missingSouls, bool isLocked)
            {
                CurrentTier = tier;
                CurrentMultiplier = currentMultiplier;
                NextLevel = nextLevel;
                NextMultiplier = nextMultiplier;
                SoulCost = soulCost;
                MissingSouls = missingSouls;
                IsLocked = isLocked;
            }
        }

        private readonly MetaProfile _profile;
        private readonly MetaConfig _config;
        private readonly Func<ECurrency, bool> _isCurrencyUnlocked;

        //# isCurrencyUnlocked 미지정이면 육성 데이터 로드 + 콘텐츠 해금(CurrencyRules) 기준. 테스트는 주입.
        public ShopService(MetaProfile profile, MetaConfig config, Func<ECurrency, bool> isCurrencyUnlocked = null)
        {
            _profile = profile;
            _config = config;
            _isCurrencyUnlocked = isCurrencyUnlocked ?? DefaultUnlocked;
        }

        private bool DefaultUnlocked(ECurrency currency)
        {
            if (currency == ECurrency.Soul)
                return true;
            return GrowthDatabase.Current != null && CurrencyRules.IsUnlocked(currency, _profile);
        }

        //# 구매 가능한 단계의 표시 가격. 티어 단계가 없거나 항목 미발견이면 -1.
        public int PriceOf(string itemId)
        {
            ShopItemDef def = _config != null ? _config.FindShopItem(itemId) : null;
            if (def == null)
                return -1;
            if (MonsterEnhanceTierRules.IsTierItem(def))
            {
                TierOffer offer = BuildTierOffer(def);
                return offer.HasNext ? offer.SoulCost : -1;
            }
            return PriceOf(def, _profile.GetShopLevel(itemId));
        }

        //# 레벨형 항목의 가격 공식. 체급 티어는 단계 가격표를 사용한다.
        public static int PriceOf(ShopItemDef def, int currentLevel)
            => Mathf.FloorToInt(def.BasePrice * Mathf.Pow(def.PriceGrowth, currentLevel));

        public bool TryGetTierOffer(string itemId, out TierOffer offer)
        {
            ShopItemDef def = _config?.FindShopItem(itemId);
            offer = default;
            if (MonsterEnhanceTierRules.IsTierItem(def) == false || _profile == null)
                return false;
            offer = BuildTierOffer(def);
            return true;
        }

        private TierOffer BuildTierOffer(ShopItemDef def)
        {
            int tier = EnhanceTierTable.TierOfLevel(_profile.GetShopLevel(def.Id));
            float currentMultiplier = Mathf.Pow(def.PerLevelMul, EnhanceTierTable.LevelEquivalent(tier));
            bool locked = IsCurrencyLocked(def.Currency);
            if (_config.EnhanceTiers == null
                || _config.EnhanceTiers.TryGetStep(tier + 1, out int nextLevel, out int cost) == false
                || def.Currency != ECurrency.Soul || nextLevel > def.MaxLevel)
                return new TierOffer(tier, currentMultiplier, 0, currentMultiplier, 0, 0, locked);

            int missingSouls = Mathf.Max(0, cost - Mathf.Max(0, _profile.GetCurrency(def.Currency)));
            return new TierOffer(tier, currentMultiplier, nextLevel, Mathf.Pow(def.PerLevelMul, nextLevel), cost, missingSouls, locked);
        }

        //# 결제 재화가 잠겼는가 (재화 미해금 또는 육성 데이터 로드 실패) — 잠긴 항목은 구매 거부.
        public bool IsLocked(string itemId)
        {
            ShopItemDef def = _config != null ? _config.FindShopItem(itemId) : null;
            return def != null && IsCurrencyLocked(def.Currency);
        }

        public bool IsCurrencyLocked(ECurrency currency) => _isCurrencyUnlocked(currency) == false;

        //# 존재 · 만렙 · 잠김 · 결제 재화 잔액 검사.
        public bool CanBuy(string itemId)
        {
            ShopItemDef def = _config != null ? _config.FindShopItem(itemId) : null;
            if (def == null)
                return false;
            if (MonsterEnhanceTierRules.IsTierItem(def))
                return BuildTierOffer(def).CanBuy;
            return CanBuyLevel(def);
        }

        private bool CanBuyLevel(ShopItemDef def)
        {
            int level = _profile.GetShopLevel(def.Id);
            if (level >= def.MaxLevel)
                return false;
            if (IsCurrencyLocked(def.Currency))
                return false;
            return _profile.GetCurrency(def.Currency) >= PriceOf(def, level);
        }

        //# 표시와 같은 단계 계약으로 구매 성공 때만 재화와 저장값을 함께 갱신한다.
        public bool Buy(string itemId)
        {
            ShopItemDef def = _config?.FindShopItem(itemId);
            if (def == null)
                return false;
            if (MonsterEnhanceTierRules.IsTierItem(def))
            {
                TierOffer offer = BuildTierOffer(def);
                if (offer.CanBuy == false || _profile.TrySpendCurrency(def.Currency, offer.SoulCost) == false)
                    return false;
                _profile.SetShopLevel(itemId, offer.NextLevel);
                return true;
            }

            if (CanBuyLevel(def) == false)
                return false;
            int level = _profile.GetShopLevel(itemId);
            if (_profile.TrySpendCurrency(def.Currency, PriceOf(def, level)) == false)
                return false;
            _profile.SetShopLevel(itemId, level + 1);
            return true;
        }
    }
}
