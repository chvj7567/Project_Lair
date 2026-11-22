using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    public static class MonsterEnhanceTierRules
    {
        public static bool IsTierItem(ShopItemDef item)
            => item != null && item.EffectKind == EShopEffectKind.MonsterSpecies
                && MonsterGrades.IsBase(item.Species) && item.Id == ItemId(item.Species);

        public static string ItemId(EMonster baseMonster) => "Enhance_" + baseMonster;

        //# 구버전 체급과 신버전 대표값을 함께 받아, 소울 차감 없이 높은 기존 단계만 남긴다.
        public static void NormalizeProfile(MetaProfile profile)
        {
            List<ShopLevelEntry> entries = profile?.ShopLevels;
            if (entries == null)
                return;
            foreach (EMonster species in MonsterGrades.BaseMonsters)
            {
                string itemId = ItemId(species);
                ShopLevelEntry retained = null;
                int tier = 0;
                for (int i = entries.Count - 1; i >= 0; --i)
                {
                    ShopLevelEntry entry = entries[i];
                    if (entry == null || entry.ItemId != itemId)
                        continue;
                    int entryTier = EnhanceTierTable.TierOfLevel(entry.Level);
                    if (entryTier > tier)
                    {
                        tier = entryTier;
                    }
                    if (retained == null)
                    {
                        retained = entry;
                    }
                    else
                    {
                        entries.RemoveAt(i);
                    }
                }
                if (retained != null)
                {
                    retained.Level = EnhanceTierTable.LevelEquivalent(tier);
                }
            }
        }
    }
}
