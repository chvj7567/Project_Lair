using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 — 구매 가능/가격/구매 로직 (기획서 §3). 저장은 호출자(VillageController) 책임.
    //# 결제 재화는 ShopItemDef.Currency — 소울 = Souls, 그 외 = Currencies (monster-growth-currencies §3.6).
    public class ShopService
    {
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

        //# 현재 레벨 기준 다음 구매 가격 — floor(BasePrice × PriceGrowth^현재레벨). 미발견 항목은 -1.
        public int PriceOf(string itemId)
        {
            ShopItemDef def = _config != null ? _config.FindShopItem(itemId) : null;
            if (def == null)
                return -1;
            return PriceOf(def, _profile.GetShopLevel(itemId));
        }

        //# 가격 공식 단일 진실 — ShopPopup.BuildCellData 와 공유.
        public static int PriceOf(ShopItemDef def, int currentLevel)
            => Mathf.FloorToInt(def.BasePrice * Mathf.Pow(def.PriceGrowth, currentLevel));

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

            int level = _profile.GetShopLevel(itemId);
            if (level >= def.MaxLevel)
                return false;
            if (IsCurrencyLocked(def.Currency))
                return false;
            return _profile.GetCurrency(def.Currency) >= PriceOf(def, level);
        }

        //# CanBuy 통과 시 결제 재화 차감 + 레벨업. 성공 여부 반환.
        public bool Buy(string itemId)
        {
            if (CanBuy(itemId) == false)
                return false;

            ShopItemDef def = _config.FindShopItem(itemId);
            int level = _profile.GetShopLevel(itemId);
            if (_profile.TrySpendCurrency(def.Currency, PriceOf(def, level)) == false)
                return false;
            _profile.SetShopLevel(itemId, level + 1);
            return true;
        }
    }
}
