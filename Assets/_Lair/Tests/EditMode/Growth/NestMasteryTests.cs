using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 둥지 숙련 슬라이스 C 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §11.7).
    public class NestMasteryTests
    {
        private static GrowthDatabase Growth(NestMastery_ParamRow[] parameters)
        {
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
                NestMasteries = new[]
                {
                    new NestMasteryRow { level = 1, effectKind = "FirstSpawnAdvance" },
                    new NestMasteryRow { level = 2, effectKind = "SpawnPeriod" },
                    new NestMasteryRow { level = 3, effectKind = "SpawnGuard" },
                },
                NestParams = parameters,
            }, 0);
        }

        private static readonly NestMastery_ParamRow[] Shipped =
        {
            new NestMastery_ParamRow { level = 1, paramKey = "advanceSec", value = 2f },
            new NestMastery_ParamRow { level = 2, paramKey = "periodMul", value = 0.93f },
            new NestMastery_ParamRow { level = 3, paramKey = "damageTakenMul", value = 0.5f },
            new NestMastery_ParamRow { level = 3, paramKey = "durationSec", value = 3f },
        };

        [Test]
        public void Lv3_은_첫스폰_2초_주기_0점93_스폰보호_3초_0점5를_모두_누적한다()
        {
            NestMasteryEffects fx = Growth(Shipped).NestEffectsUpTo(3);

            Assert.AreEqual(2f, fx.FirstSpawnAdvanceSec, 0.0001f);
            Assert.AreEqual(0.93f, fx.PeriodMul, 0.0001f);
            Assert.AreEqual(0.5f, fx.GuardDamageTakenMul, 0.0001f);
            Assert.AreEqual(3f, fx.GuardSec, 0.0001f);
        }

        [Test]
        public void SpawnGuard_필수_파라미터가_빠지면_로드_오류다()
        {
            NestMastery_ParamRow[] missing = { Shipped[0], Shipped[1], Shipped[2] };

            Assert.Throws<ChapterDataException>(() => Growth(missing));
        }
    }
}
