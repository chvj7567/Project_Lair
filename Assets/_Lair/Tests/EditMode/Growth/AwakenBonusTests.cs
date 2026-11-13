using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 각성 슬라이스 B 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §11.7).
    public class AwakenBonusTests
    {
        private static GrowthDatabase Growth()
        {
            List<AwakenStatRow> stats = new List<AwakenStatRow>
            {
                new AwakenStatRow { monster = "Wisp", statIndex = 1, statKind = "DamageTaken", perLevelMul = 0.96f },
                new AwakenStatRow { monster = "Wraith", statIndex = 1, statKind = "KnockbackTaken", perLevelMul = 0.85f },
                new AwakenStatRow { monster = "Wraith", statIndex = 2, statKind = "DamageTaken", perLevelMul = 0.97f },
                new AwakenStatRow { monster = "Reaper", statIndex = 1, statKind = "Cooldown", perLevelMul = 0.95f },
                new AwakenStatRow { monster = "Hex", statIndex = 1, statKind = "Range", perLevelMul = 1.06f },
                new AwakenStatRow { monster = "Plague", statIndex = 1, statKind = "PoisonDamage", perLevelMul = 1.05f },
                new AwakenStatRow { monster = "Plague", statIndex = 2, statKind = "PoisonDuration", perLevelMul = 1.10f },
                new AwakenStatRow { monster = "Phantom", statIndex = 1, statKind = "MoveSpeed", perLevelMul = 1.06f },
            };
            return GrowthDatabase.Build(new GrowthDataSet
            {
                Currencies = new[]
                {
                    new CurrencyRow { currencyId = "Soul", colorHex = "#5ef0b4" },
                    new CurrencyRow { currencyId = "AwakenStone", colorHex = "#ff9f43" },
                    new CurrencyRow { currencyId = "MoonSeal", colorHex = "#d8e4ff" },
                    new CurrencyRow { currencyId = "AbyssCrystal", colorHex = "#2ec4c4" },
                    new CurrencyRow { currencyId = "RelicShard", colorHex = "#c8a27a" },
                },
                Exchanges = new Currency_ExchangeRow[0],
                DifficultyAwakens = new Difficulty_AwakenRow[0],
                TowerCrystals = new[] { new Difficulty_TowerCrystalRow { difficulty = 0, crystalPerFloor = 1 } },
                AwakenStats = stats.ToArray(),
            }, 0);
        }

        [Test]
        public void 망령_각성_Lv5_는_넉백_0점85의_5제곱과_받는_피해_0점97의_5제곱을_준다()
        {
            MetaConfig cfg = new MetaConfig();
            cfg.ShopItems.Add(new ShopItemDef { Id = "Awaken_Wraith", EffectKind = EShopEffectKind.MonsterAwaken, Species = EMonster.Wraith, Currency = ECurrency.AwakenStone, MaxLevel = 5 });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Awaken_Wraith", 5);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg, Growth());

            Assert.AreEqual(Mathf.Pow(0.85f, 5), bonus.GetAwakenMul(EMonster.Wraith, EMonsterStatKind.KnockbackTaken), 0.0001f);
            Assert.AreEqual(Mathf.Pow(0.97f, 5), bonus.GetAwakenMul(EMonster.Wraith, EMonsterStatKind.DamageTaken), 0.0001f);
            Assert.AreEqual(1f, bonus.GetAwakenMul(EMonster.Wisp, EMonsterStatKind.DamageTaken), 0.0001f, "다른 종족 무영향");
            Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.DamageTaken), 0.0001f, "전종 축에 새지 않음");
        }

        [Test]
        public void 각성_받는_피해는_버프_오버레이가_1로_되돌아가도_유지된다()
        {
            GameObject go = new GameObject("wisp");
            Health hp = go.AddComponent<Health>();
            hp.SetMax(100);
            hp.ConfigureGrowthTaken(0.5f, 1f);
            hp.DamageTakenScale = 0.5f;
            hp.DamageTakenScale = 1f;   //# MonsterBuffService 틱 리셋 시뮬

            hp.TakeDamage(10);

            Assert.AreEqual(95, hp.Current, "10 x 0.5 = 5");
            Object.DestroyImmediate(go);
        }
    }
}
