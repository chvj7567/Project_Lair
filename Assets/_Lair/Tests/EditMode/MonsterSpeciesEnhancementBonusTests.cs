using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 종족별 강화(EShopEffectKind.MonsterSpecies) → MetaBattleBonus 집계 + 3축 곱연산 독립성 검증 (기획서 §2·§8).
    public class MonsterSpeciesEnhancementBonusTests
    {
        private static MetaConfig MakeConfig(params ShopItemDef[] items)
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>(items);
            return cfg;
        }

        private static ShopItemDef SpeciesItem(string id, EMonster species, float perLevelMul, int maxLevel)
            => new ShopItemDef
            {
                Id = id,
                EffectKind = EShopEffectKind.MonsterSpecies,
                Species = species,
                PerLevelMul = perLevelMul,
                MaxLevel = maxLevel,
            };

        [Test]
        public void 종족강화_레벨2면_PerLevelMul_제곱으로_집계된다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 5));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 2);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(1.2f * 1.2f, bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        [Test]
        public void 강화안한_종족은_1배수다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 5));
            MetaBattleBonus bonus = MetaBattleBonus.From(new MetaProfile(), cfg);

            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wraith), 1e-4f);
        }

        [Test]
        public void 저장레벨이_만렙초과여도_MaxLevel로_클램프된다()
        {
            MetaConfig cfg = MakeConfig(SpeciesItem("Enhance_Wisp", EMonster.Wisp, 1.2f, 3));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 99);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(Mathf.Pow(1.2f, 3), bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        //# Task 2 — ApplyMetaBonuses 규약 재현: 글로벌 Hp × 종족 Hp 가 같은 표면에 곱연산 누적된다.
        [Test]
        public void 종족강화와_글로벌스탯강화는_같은_HpMul에_곱연산_누적된다()
        {
            StatMultiplier mul = new StatMultiplier();
            mul.Multiply(EMonsterStatKind.Hp, 1.1f);    //# 글로벌 스탯강화분
            mul.Multiply(EMonsterStatKind.Hp, 1.44f);   //# 종족강화분 (GetSpeciesMul 결과)

            Assert.AreEqual(1.1f * 1.44f, mul.Get(EMonsterStatKind.Hp), 1e-4f);
        }
    }
}
