using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# DungeonPowerSummary.Build — 상점 레벨 → 라벨+% 환산 검증 (기획서 §2.1).
    //# 증가형 (mul-1) / 감소형 (1/mul-1), ShopItems 순서, 레벨0·0% 제외.
    public class DungeonPowerSummaryTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            //# 기획서 §2.1 PerLevelMul 과 동일 수치.
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.02f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterAtkSpeedUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Cooldown, PerLevelMul = 0.99f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnerHasteUp", EffectKind = EShopEffectKind.SpawnerPeriod, PerLevelMul = 0.985f, MaxLevel = 5 });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 강화가_없으면_빈_목록이다()
        {
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(new MetaProfile(), _cfg);
            Assert.AreEqual(0, lines.Count);
        }

        [Test]
        public void 증가형_스탯은_양수_퍼센트로_환산된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 5);                 //# 1.02^5 = 1.104 → +10%
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("HP", lines[0].Label);
            Assert.AreEqual(10, lines[0].Percent);
        }

        [Test]
        public void 감소형_스탯은_역수로_강화_퍼센트가_된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterAtkSpeedUp", 5);           //# 0.99^5 = 0.951 → (1/0.951-1) = +5%
            p.SetShopLevel("SpawnerHasteUp", 5);              //# 0.985^5 = 0.927 → (1/0.927-1) = +8%
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("공속", lines[0].Label);
            Assert.AreEqual(5, lines[0].Percent);
            Assert.AreEqual("스폰률", lines[1].Label);
            Assert.AreEqual(8, lines[1].Percent);
        }

        [Test]
        public void 표시_순서는_ShopItems_순서를_따르고_레벨0은_제외된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("SpawnerHasteUp", 3);              //# 목록상 3번째만 구매
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("스폰률", lines[0].Label);        //# Hp·Cooldown 은 레벨0 → 제외
        }
    }
}
