using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 소울 상점 — 구매 가능/가격/구매 로직 (기획서 §3). 저장은 호출자(VillageController) 책임.
    public class ShopService
    {
        private readonly MetaProfile _profile;
        private readonly MetaConfig _config;

        public ShopService(MetaProfile profile, MetaConfig config)
        {
            _profile = profile;
            _config = config;
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

        //# 존재 · 만렙 · 소울 잔액 검사.
        public bool CanBuy(string itemId)
        {
            ShopItemDef def = _config != null ? _config.FindShopItem(itemId) : null;
            if (def == null)
                return false;

            int level = _profile.GetShopLevel(itemId);
            if (level >= def.MaxLevel)
                return false;
            return _profile.Souls >= PriceOf(def, level);
        }

        //# CanBuy 통과 시 차감 + 레벨업. 성공 여부 반환.
        public bool Buy(string itemId)
        {
            if (CanBuy(itemId) == false)
                return false;

            ShopItemDef def = _config.FindShopItem(itemId);
            int level = _profile.GetShopLevel(itemId);
            _profile.Souls -= PriceOf(def, level);
            _profile.SetShopLevel(itemId, level + 1);
            return true;
        }
    }
}
