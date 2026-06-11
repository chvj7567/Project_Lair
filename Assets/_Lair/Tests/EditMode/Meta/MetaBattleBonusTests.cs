using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class MetaBattleBonusTests
    {
        [Test]
        public void 상점_레벨만큼_거듭제곱_배율이_집계된다()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f });
            cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnerHasteUp", EffectKind = EShopEffectKind.SpawnerPeriod, PerLevelMul = 0.97f });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 2);
            p.SetShopLevel("SpawnerHasteUp", 1);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg);
            Assert.AreEqual(1.21f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);   //# 1.1^2
            Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Power), 0.001f);
            Assert.AreEqual(0.97f, bonus.SpawnerPeriodMul, 0.001f);

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void 레벨_0이면_전부_항등이다()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            MetaBattleBonus bonus = MetaBattleBonus.From(new MetaProfile(), cfg);
            Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);
            Assert.AreEqual(1f, bonus.SpawnerPeriodMul, 0.001f);

            Object.DestroyImmediate(cfg);
        }
    }
}
