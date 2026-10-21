using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 유물 2차 — 출하 JSON 10종 로드/교차 검증 (relics §3.4~3.5, §12.6 "데이터 로드"). 기대값은 기획서 표를 독립으로 다시 적었다.
    public class RelicDataTests
    {
        private static readonly string[] Heroes = { "Knight", "Archer", "Assassin", "Berserker", "Paladin" };

        //# 기획서 §3.4 Relic_Drop 가중치 행렬 — 0 = 행 없음. 열 순서 = Heroes.
        private static readonly Dictionary<string, int[]> DropMatrix = new Dictionary<string, int[]>
        {
            { "HeartOfStone", new[] { 6, 2, 2, 3, 5 } },
            { "RustyBlade", new[] { 2, 3, 6, 6, 2 } },
            { "SwiftBoots", new[] { 2, 6, 5, 2, 2 } },
            { "EmberBrand", new[] { 3, 3, 2, 6, 3 } },
            { "AmbushHorn", new[] { 4, 3, 4, 3, 3 } },
            { "SoulPurse", new[] { 3, 3, 3, 3, 3 } },
            { "LordsTome", new[] { 3, 3, 3, 3, 4 } },
            { "ConsolationFeather", new[] { 2, 2, 2, 2, 2 } },
            { "BrokenHourglass", new[] { 2, 3, 5, 4, 2 } },
            { "BreedingCharm", new[] { 4, 3, 2, 2, 4 } },
            { "ThunderShard", new[] { 3, 5, 2, 2, 3 } },
            { "CursedBanner", new[] { 2, 2, 4, 3, 0 } },
            { "HasteHourglass", new[] { 3, 3, 3, 3, 3 } },
            { "SpawnPrelude", new[] { 3, 2, 2, 2, 3 } },
            { "KingsCrownShard", new[] { 5, 0, 0, 5, 5 } },
            { "WrathSpirit", new[] { 0, 5, 5, 5, 0 } },
        };

        //# 기획서 §7.5 이름 (id → 표시명).
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "HeartOfStone", "단단한 심장석" }, { "RustyBlade", "녹슨 칼날" }, { "SwiftBoots", "질풍의 장화" },
            { "BrokenHourglass", "깨진 모래시계" }, { "BreedingCharm", "번식의 부적" }, { "KingsCrownShard", "왕관 조각" },
            { "EmberBrand", "잿불 낙인" }, { "AmbushHorn", "매복의 뿔피리" }, { "ThunderShard", "뇌운 조각" },
            { "CursedBanner", "저주의 깃발" }, { "WrathSpirit", "분노의 영혼" }, { "HasteHourglass", "가속 모래시계" },
            { "SoulPurse", "소울 주머니" }, { "LordsTome", "영주의 서" }, { "ConsolationFeather", "위로의 깃털" },
            { "SpawnPrelude", "서두르는 둥지" },
        };

        //# 기획서 §3.3/3.4 유물별 (종류, 등급, maxStack, 효과 종류들).
        private struct Expect
        {
            public ERelicCategory Category;
            public ERelicRarity Rarity;
            public int MaxStack;
            public ERelicEffectKind[] Kinds;
        }

        private static Expect E(ERelicCategory c, ERelicRarity r, int max, params ERelicEffectKind[] kinds)
        {
            return new Expect { Category = c, Rarity = r, MaxStack = max, Kinds = kinds };
        }

        private static readonly Dictionary<string, Expect> Expected = new Dictionary<string, Expect>
        {
            { "HeartOfStone", E(ERelicCategory.Stat, ERelicRarity.Common, 5, ERelicEffectKind.MonsterStat) },
            { "RustyBlade", E(ERelicCategory.Stat, ERelicRarity.Common, 5, ERelicEffectKind.MonsterStat) },
            { "SwiftBoots", E(ERelicCategory.Stat, ERelicRarity.Common, 5, ERelicEffectKind.MonsterStat) },
            { "BrokenHourglass", E(ERelicCategory.Stat, ERelicRarity.Rare, 3, ERelicEffectKind.MonsterStat) },
            { "BreedingCharm", E(ERelicCategory.Stat, ERelicRarity.Rare, 3, ERelicEffectKind.SpawnerPeriod) },
            { "KingsCrownShard", E(ERelicCategory.Stat, ERelicRarity.Legendary, 1, ERelicEffectKind.MonsterStat, ERelicEffectKind.MonsterStat) },
            { "EmberBrand", E(ERelicCategory.Combat, ERelicRarity.Common, 5, ERelicEffectKind.HeroBurn) },
            { "AmbushHorn", E(ERelicCategory.Combat, ERelicRarity.Common, 3, ERelicEffectKind.OpeningSummon) },
            { "ThunderShard", E(ERelicCategory.Combat, ERelicRarity.Rare, 3, ERelicEffectKind.HeroLightning) },
            { "CursedBanner", E(ERelicCategory.Combat, ERelicRarity.Rare, 3, ERelicEffectKind.HeroAttackDown) },
            { "WrathSpirit", E(ERelicCategory.Combat, ERelicRarity.Legendary, 1, ERelicEffectKind.LowHpFrenzy) },
            { "HasteHourglass", E(ERelicCategory.Convenience, ERelicRarity.Rare, 1, ERelicEffectKind.BattleSpeed) },
            { "SoulPurse", E(ERelicCategory.Convenience, ERelicRarity.Common, 5, ERelicEffectKind.SoulBonus) },
            { "LordsTome", E(ERelicCategory.Convenience, ERelicRarity.Common, 5, ERelicEffectKind.XpBonus) },
            { "ConsolationFeather", E(ERelicCategory.Convenience, ERelicRarity.Common, 3, ERelicEffectKind.LoseSoulBonus) },
            { "SpawnPrelude", E(ERelicCategory.Convenience, ERelicRarity.Rare, 1, ERelicEffectKind.SpawnPrelude) },
        };

        //# --- 행 수 / 구조 ---

        [Test]
        public void 출하_JSON_10종의_행_수가_기획서_12_6과_같다()
        {
            RelicDataSet set = RelicDataSet.Shipped();

            Assert.AreEqual(16, set.Relics.Count, "Relic");
            Assert.AreEqual(17, set.Effects.Count, "Relic_Effect");
            Assert.AreEqual(26, set.Params.Count, "Relic_Effect_Param");
            Assert.AreEqual(75, set.Drops.Count, "Relic_Drop");
            Assert.AreEqual(3, set.Rarities.Count, "RelicRarity");
            Assert.AreEqual(5, set.Stacks.Count, "RelicStack");
            Assert.IsNotNull(set.Config, "RelicConfig");
            Assert.AreEqual(25, set.StageChances.Count, "Stage_Relic");
            Assert.AreEqual(3, set.DifficultyChances.Count, "Difficulty_Relic");
            Assert.AreEqual(9, set.DifficultyRarities.Count, "Difficulty_Relic_Rarity");
        }

        [Test]
        public void 출하_데이터는_챕터DB_교차검증과_문구_검증까지_통과한다()
        {
            Assert.DoesNotThrow(() => RelicDataSet.Shipped().Build());
        }

        [Test]
        public void 유물_16종의_종류_등급_최대중첩_효과종류가_기획서와_같다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(16, db.Relics.Count);
            foreach (KeyValuePair<string, Expect> pair in Expected)
            {
                RelicData data = db.Get(pair.Key);
                Assert.IsNotNull(data, pair.Key);
                Assert.AreEqual(pair.Value.Category, data.Category, pair.Key + " category");
                Assert.AreEqual(pair.Value.Rarity, data.Rarity, pair.Key + " rarity");
                Assert.AreEqual(pair.Value.MaxStack, data.MaxStack, pair.Key + " maxStack");
                Assert.AreEqual(pair.Value.Kinds.Length, data.Effects.Count, pair.Key + " effects");
                for (int i = 0; i < pair.Value.Kinds.Length; ++i)
                {
                    Assert.AreEqual(pair.Value.Kinds[i], data.Effects[i].Kind, $"{pair.Key} effect {i + 1}");
                }
            }
        }

        [Test]
        public void 유물_분포는_종류_6_5_5_등급_8_6_2_중첩_슬롯_합_52다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            int stat = 0, combat = 0, convenience = 0, common = 0, rare = 0, legendary = 0, slots = 0;

            foreach (RelicData data in db.Relics)
            {
                stat += data.Category == ERelicCategory.Stat ? 1 : 0;
                combat += data.Category == ERelicCategory.Combat ? 1 : 0;
                convenience += data.Category == ERelicCategory.Convenience ? 1 : 0;
                common += data.Rarity == ERelicRarity.Common ? 1 : 0;
                rare += data.Rarity == ERelicRarity.Rare ? 1 : 0;
                legendary += data.Rarity == ERelicRarity.Legendary ? 1 : 0;
                slots += data.MaxStack;
            }

            Assert.AreEqual(6, stat);
            Assert.AreEqual(5, combat);
            Assert.AreEqual(5, convenience);
            Assert.AreEqual(8, common);
            Assert.AreEqual(6, rare);
            Assert.AreEqual(2, legendary);
            Assert.AreEqual(52, slots);
        }

        [Test]
        public void 도감_정렬은_JSON_순서이고_첫째와_마지막이_고정된다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual("HeartOfStone", db.Relics[0].RelicId);
            Assert.AreEqual("SpawnPrelude", db.Relics[15].RelicId);
        }

        [Test]
        public void ERelic_Enum_값명_16종이_Relic_JSON_id와_일대일이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            HashSet<string> enumNames = new HashSet<string>(Enum.GetNames(typeof(ERelic)));
            HashSet<string> jsonIds = new HashSet<string>();
            foreach (RelicData data in db.Relics)
            {
                jsonIds.Add(data.RelicId);
            }

            Assert.AreEqual(16, enumNames.Count);
            Assert.IsTrue(enumNames.SetEquals(jsonIds));
        }

        //# --- 효과 파라미터 값 (기획서 §3.4) ---

        [Test]
        public void 효과_파라미터_초기값이_기획서_표와_같다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(0f, db.Get("HeartOfStone").Effects[0].Get("statKind"));
            Assert.AreEqual(0.03f, db.Get("HeartOfStone").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(1f, db.Get("RustyBlade").Effects[0].Get("statKind"));
            Assert.AreEqual(4f, db.Get("SwiftBoots").Effects[0].Get("statKind"));
            Assert.AreEqual(2f, db.Get("BrokenHourglass").Effects[0].Get("statKind"));
            Assert.AreEqual(-0.02f, db.Get("BrokenHourglass").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(-0.03f, db.Get("BreedingCharm").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(0.05f, db.Get("KingsCrownShard").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(0f, db.Get("KingsCrownShard").Effects[0].Get("statKind"));
            Assert.AreEqual(1f, db.Get("KingsCrownShard").Effects[1].Get("statKind"));
            Assert.AreEqual(0.0002f, db.Get("EmberBrand").Effects[0].Get("hpRatioPerSec"), 1e-8f);
            Assert.AreEqual(0f, db.Get("AmbushHorn").Effects[0].Get("monster"));
            Assert.AreEqual(2f, db.Get("AmbushHorn").Effects[0].Get("countPerStack"));
            Assert.AreEqual(10f, db.Get("ThunderShard").Effects[0].Get("intervalSec"));
            Assert.AreEqual(0.003f, db.Get("ThunderShard").Effects[0].Get("hpRatio"), 1e-7f);
            Assert.AreEqual(0.04f, db.Get("CursedBanner").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(0.5f, db.Get("WrathSpirit").Effects[0].Get("hpFraction"));
            Assert.AreEqual(10f, db.Get("WrathSpirit").Effects[0].Get("durationSec"));
            Assert.AreEqual(2f, db.Get("HasteHourglass").Effects[0].Get("speedScale"));
            Assert.AreEqual(0.04f, db.Get("SoulPurse").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(0.04f, db.Get("LordsTome").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(0.25f, db.Get("ConsolationFeather").Effects[0].Get("amount"), 1e-6f);
            Assert.AreEqual(3f, db.Get("SpawnPrelude").Effects[0].Get("advanceSec"));
        }

        //# --- 드랍 테이블 ---

        [Test]
        public void Relic_Drop_영웅별_가중치가_기획서_행렬과_정확히_일치한다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            for (int h = 0; h < Heroes.Length; ++h)
            {
                IReadOnlyList<Relic_DropRow> table = db.DropsOf(Heroes[h]);
                int expectedRows = 0;
                foreach (KeyValuePair<string, int[]> pair in DropMatrix)
                {
                    int expected = pair.Value[h];
                    Relic_DropRow row = null;
                    foreach (Relic_DropRow candidate in table)
                    {
                        if (candidate.relicId == pair.Key)
                        {
                            row = candidate;
                        }
                    }
                    if (expected == 0)
                    {
                        Assert.IsNull(row, $"{Heroes[h]} 에 {pair.Key} 행이 없어야 함");
                        continue;
                    }
                    expectedRows++;
                    Assert.IsNotNull(row, $"{Heroes[h]} 에 {pair.Key} 행 필요");
                    Assert.AreEqual(expected, row.weight, $"{Heroes[h]}/{pair.Key} weight");
                }
                Assert.AreEqual(expectedRows, table.Count, $"{Heroes[h]} 테이블 행 수");
            }
        }

        [Test]
        public void 영웅별_테이블_행_수는_기사15_궁수15_암살자15_광전사16_성기사14다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(15, db.DropsOf("Knight").Count);
            Assert.AreEqual(15, db.DropsOf("Archer").Count);
            Assert.AreEqual(15, db.DropsOf("Assassin").Count);
            Assert.AreEqual(16, db.DropsOf("Berserker").Count);
            Assert.AreEqual(14, db.DropsOf("Paladin").Count);
        }

        [Test]
        public void 모든_유물이_최소_한_영웅의_테이블에_있고_모든_영웅이_Common_후보를_가진다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            foreach (RelicData data in db.Relics)
            {
                bool any = false;
                foreach (string hero in Heroes)
                {
                    foreach (Relic_DropRow row in db.DropsOf(hero))
                    {
                        any |= row.relicId == data.RelicId;
                    }
                }
                Assert.IsTrue(any, data.RelicId);
            }
            foreach (string hero in Heroes)
            {
                bool common = false;
                foreach (Relic_DropRow row in db.DropsOf(hero))
                {
                    common |= db.Get(row.relicId).Rarity == ERelicRarity.Common;
                }
                Assert.IsTrue(common, hero);
            }
        }

        [Test]
        public void 모든_영웅의_Legendary_후보는_1에서_2개다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            foreach (string hero in Heroes)
            {
                int legendary = 0;
                foreach (Relic_DropRow row in db.DropsOf(hero))
                {
                    legendary += db.Get(row.relicId).Rarity == ERelicRarity.Legendary ? 1 : 0;
                }
                Assert.That(legendary, Is.InRange(1, 2), hero);
            }
        }

        [Test]
        public void 드랍_테이블에_없는_영웅과_null은_빈_목록이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(0, db.DropsOf("Nobody").Count);
            Assert.AreEqual(0, db.DropsOf(null).Count);
        }

        //# --- 확률 표 ---

        [Test]
        public void Stage_Relic_25행이_일반_스테이지_0_30_보스_스테이지_0_60이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            for (int chapter = 1; chapter <= 5; ++chapter)
            {
                for (int stage = 1; stage <= 4; ++stage)
                {
                    Assert.AreEqual(0.30f, db.StageDropChance(chapter, stage), 1e-6f, $"{chapter}-{stage}");
                }
                Assert.AreEqual(0.60f, db.StageDropChance(chapter, 5), 1e-6f, $"{chapter}-5");
            }
        }

        [Test]
        public void 없는_스테이지의_드랍_확률은_0이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(0f, db.StageDropChance(6, 1));
            Assert.AreEqual(0f, db.StageDropChance(1, 0));
            Assert.AreEqual(0f, db.StageDropChance(0, 0));
        }

        [Test]
        public void 난이도_드랍_배율과_등급_가중_배율이_기획서_표와_같다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(1.00f, db.DifficultyChanceMul(0), 1e-6f);
            Assert.AreEqual(1.25f, db.DifficultyChanceMul(1), 1e-6f);
            Assert.AreEqual(1.50f, db.DifficultyChanceMul(2), 1e-6f);
            float[,] weights = { { 1f, 1f, 1f }, { 0.8f, 1.4f, 2.0f }, { 0.6f, 1.8f, 4.0f } };
            for (int d = 0; d < 3; ++d)
            {
                Assert.AreEqual(weights[d, 0], db.RarityWeightMul(d, ERelicRarity.Common), 1e-6f, $"d{d} Common");
                Assert.AreEqual(weights[d, 1], db.RarityWeightMul(d, ERelicRarity.Rare), 1e-6f, $"d{d} Rare");
                Assert.AreEqual(weights[d, 2], db.RarityWeightMul(d, ERelicRarity.Legendary), 1e-6f, $"d{d} Legendary");
            }
        }

        [Test]
        public void 범위_밖_난이도는_양끝으로_클램프된다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(1.50f, db.DifficultyChanceMul(99), 1e-6f);
            Assert.AreEqual(1.00f, db.DifficultyChanceMul(-5), 1e-6f);
            Assert.AreEqual(4.0f, db.RarityWeightMul(99, ERelicRarity.Legendary), 1e-6f);
            Assert.AreEqual(1.0f, db.RarityWeightMul(-1, ERelicRarity.Legendary), 1e-6f);
        }

        [Test]
        public void 등급_기본_가중치와_색과_전역_설정이_기획서와_같다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(70, db.RarityBaseWeight(ERelicRarity.Common));
            Assert.AreEqual(25, db.RarityBaseWeight(ERelicRarity.Rare));
            Assert.AreEqual(5, db.RarityBaseWeight(ERelicRarity.Legendary));
            Assert.AreEqual("#9aa5bd", db.RarityRow(ERelicRarity.Common).colorHex);
            Assert.AreEqual("#5aa9ff", db.RarityRow(ERelicRarity.Rare).colorHex);
            Assert.AreEqual("#b07cff", db.RarityRow(ERelicRarity.Legendary).colorHex);
            Assert.AreEqual(0.25f, db.Config.lossChanceMul, 1e-6f);
            Assert.AreEqual(0.10f, db.Config.pityPerMiss, 1e-6f);
            Assert.AreEqual(0.40f, db.Config.pityMaxBonus, 1e-6f);
            Assert.AreEqual(0.95f, db.Config.maxChance, 1e-6f);
        }

        //# --- 중첩 스케일 / 조회 방어 ---

        [Test]
        public void 중첩_스케일_S_n은_1_00_1_60_2_10_2_50_2_80이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(5, db.MaxScaleStack);
            Assert.AreEqual(1.00f, db.EffectScale(1), 1e-6f);
            Assert.AreEqual(1.60f, db.EffectScale(2), 1e-6f);
            Assert.AreEqual(2.10f, db.EffectScale(3), 1e-6f);
            Assert.AreEqual(2.50f, db.EffectScale(4), 1e-6f);
            Assert.AreEqual(2.80f, db.EffectScale(5), 1e-6f);
        }

        [Test]
        public void S_n은_0이하는_0_5초과는_마지막_단계로_클램프된다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual(0f, db.EffectScale(0));
            Assert.AreEqual(0f, db.EffectScale(-3));
            Assert.AreEqual(2.80f, db.EffectScale(6), 1e-6f);
            Assert.AreEqual(2.80f, db.EffectScale(999), 1e-6f);
        }

        [Test]
        public void ScaledParam은_amount류만_S를_곱하고_countPerStack은_선형이며_나머지는_고정이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            RelicEffectData heart = db.Get("HeartOfStone").Effects[0];
            RelicEffectData boots = db.Get("SwiftBoots").Effects[0];
            RelicEffectData horn = db.Get("AmbushHorn").Effects[0];
            RelicEffectData thunder = db.Get("ThunderShard").Effects[0];
            RelicEffectData wrath = db.Get("WrathSpirit").Effects[0];
            RelicEffectData prelude = db.Get("SpawnPrelude").Effects[0];

            Assert.AreEqual(0.03f * 2.1f, db.ScaledParam(heart, "amount", 3), 1e-6f);
            Assert.AreEqual(4f, db.ScaledParam(boots, "statKind", 3), "statKind 고정");
            Assert.AreEqual(6f, db.ScaledParam(horn, "countPerStack", 3), 1e-6f);
            Assert.AreEqual(0f, db.ScaledParam(horn, "monster", 3), "monster 고정");
            Assert.AreEqual(10f, db.ScaledParam(thunder, "intervalSec", 3), 1e-6f, "intervalSec 고정");
            Assert.AreEqual(0.003f * 2.1f, db.ScaledParam(thunder, "hpRatio", 3), 1e-7f);
            Assert.AreEqual(0.5f, db.ScaledParam(wrath, "hpFraction", 1), 1e-6f);
            Assert.AreEqual(10f, db.ScaledParam(wrath, "durationSec", 1), 1e-6f);
            Assert.AreEqual(3f * 2.8f, db.ScaledParam(prelude, "advanceSec", 5), 1e-5f);
        }

        [Test]
        public void 없는_id_조회는_null이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.IsNull(db.Get("Nope"));
            Assert.IsNull(db.Get(null));
            Assert.IsNull(db.Get(string.Empty));
        }

        //# --- Strings_Ko (실제 문구 번호는 기획서 +3 이동: 1338~1392) ---

        [Test]
        public void Strings_Ko_유물_문구_1338에서_1392가_연속으로_존재하고_비어_있지_않다()
        {
            for (int id = 1338; id <= 1392; ++id)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
        }

        [Test]
        public void 유물_이름_문구가_nameKey로_정확한_표시명을_가리킨다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            foreach (RelicData data in db.Relics)
            {
                Assert.AreEqual(Names[data.RelicId], WorldMapTestSupport.Text(data.NameKey), data.RelicId);
            }
        }

        [Test]
        public void 유물_이름_키와_설명_키는_각각_1360부터_1375_1376부터_1391까지_연속이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            for (int i = 0; i < db.Relics.Count; ++i)
            {
                Assert.AreEqual(1360 + i, db.Relics[i].NameKey, db.Relics[i].RelicId + " nameKey");
                Assert.AreEqual(1376 + i, db.Relics[i].DescKey, db.Relics[i].RelicId + " descKey");
            }
        }

        [Test]
        public void 등급_이름_키가_일반_희귀_전설을_가리킨다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            Assert.AreEqual("일반", WorldMapTestSupport.Text(db.RarityRow(ERelicRarity.Common).nameKey));
            Assert.AreEqual("희귀", WorldMapTestSupport.Text(db.RarityRow(ERelicRarity.Rare).nameKey));
            Assert.AreEqual("전설", WorldMapTestSupport.Text(db.RarityRow(ERelicRarity.Legendary).nameKey));
        }

        [Test]
        public void 설명_문구의_포맷_인자_개수가_효과_종류별_인자_개수와_일치한다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();

            foreach (RelicData data in db.Relics)
            {
                string format = WorldMapTestSupport.Text(data.DescKey);
                int max = -1;
                foreach (Match match in Regex.Matches(format, @"\{(\d+)\}"))
                {
                    max = Math.Max(max, int.Parse(match.Groups[1].Value));
                }
                object[] args = RelicText.BuildArgs(db, data.Effects[0], 1);
                Assert.AreEqual(max + 1, args.Length, data.RelicId);
                Assert.DoesNotThrow(() => string.Format(format, args), data.RelicId);
            }
        }

        //# --- 위반 케이스: 각 항목이 ChapterDataException 으로 수집된다 ---

        private static void AssertInvalid(Action<RelicDataSet> mutate, string fragment)
        {
            RelicDataSet set = RelicDataSet.Shipped();
            mutate(set);

            ChapterDataException error = Assert.Throws<ChapterDataException>(() => set.Build());
            StringAssert.Contains(fragment, error.Message);
        }

        [Test]
        public void 위반_relicId가_ERelic에_없으면_로드_오류다()
        {
            AssertInvalid(s => s.Relics[0].relicId = "Nope", "relicId 가 ERelic 에 없음");
        }

        [Test]
        public void 위반_relicId_대소문자나_숫자_문자열은_ERelic으로_인정하지_않는다()
        {
            AssertInvalid(s => s.Relics[0].relicId = "heartofstone", "relicId 가 ERelic 에 없음");
            AssertInvalid(s => s.Relics[0].relicId = "3", "relicId 가 ERelic 에 없음");
            AssertInvalid(s => s.Relics[0].relicId = "", "relicId 가 ERelic 에 없음");
        }

        [Test]
        public void 위반_relicId_중복이면_로드_오류다()
        {
            AssertInvalid(s => s.Relics.Add(new RelicRow
            {
                relicId = "HeartOfStone", category = "Stat", rarity = "Common", maxStack = 5, nameKey = 1360, descKey = 1376,
            }), "HeartOfStone 중복");
        }

        [Test]
        public void 위반_category가_정의되지_않은_값이면_로드_오류다()
        {
            AssertInvalid(s => s.Relics[0].category = "Weird", "category 가 ERelicCategory 에 없음");
        }

        [Test]
        public void 위반_rarity가_정의되지_않은_값이면_로드_오류다()
        {
            AssertInvalid(s => s.Relics[0].rarity = "Mythic", "rarity 가 ERelicRarity 에 없음");
        }

        [Test]
        public void 위반_maxStack이_0이거나_스케일표_최대를_넘으면_로드_오류다()
        {
            AssertInvalid(s => s.Relics[0].maxStack = 0, "maxStack");
            AssertInvalid(s => s.Relics[0].maxStack = 6, "maxStack");
        }

        [Test]
        public void 위반_nameKey나_descKey가_Strings_Ko에_없으면_로드_오류다()
        {
            AssertInvalid(s => s.Relics[0].nameKey = 999999, "Strings_Ko 에 없음");
            AssertInvalid(s => s.Relics[0].descKey = 999999, "Strings_Ko 에 없음");
        }

        [Test]
        public void 위반_유물에_효과가_하나도_없으면_로드_오류다()
        {
            AssertInvalid(s =>
            {
                s.Effects.RemoveAll(e => e.relicId == "HeartOfStone");
                s.Params.RemoveAll(p => p.relicId == "HeartOfStone");
            }, "HeartOfStone 효과가 1개 이상 필요");
        }

        [Test]
        public void 위반_effectIndex가_1부터_연속이_아니면_로드_오류다()
        {
            AssertInvalid(s => s.Effects.Find(e => e.relicId == "KingsCrownShard" && e.effectIndex == 2).effectIndex = 3,
                "effectIndex 는 1부터 연속");
            AssertInvalid(s => s.Effects.Find(e => e.relicId == "HeartOfStone").effectIndex = 0,
                "effectIndex 는 1부터 연속");
        }

        [Test]
        public void 위반_effectKind가_정의되지_않은_값이면_로드_오류다()
        {
            AssertInvalid(s => s.Effects[0].effectKind = "Explode", "effectKind 가 ERelicEffectKind 에 없음");
        }

        [Test]
        public void 위반_효과의_필수_파라미터_키가_빠지면_로드_오류다()
        {
            AssertInvalid(s => s.Params.Remove(s.Param("HeartOfStone", 1, "amount")), "필수 키 amount");
            AssertInvalid(s => s.Params.Remove(s.Param("ThunderShard", 1, "intervalSec")), "필수 키 intervalSec");
            AssertInvalid(s => s.Params.Remove(s.Param("AmbushHorn", 1, "countPerStack")), "필수 키 countPerStack");
        }

        [Test]
        public void 위반_파라미터_키가_중복되면_로드_오류다()
        {
            AssertInvalid(s => s.Params.Add(new Relic_Effect_ParamRow
            {
                relicId = "HeartOfStone", effectIndex = 1, paramKey = "amount", value = 0.5f,
            }), "HeartOfStone/1/amount 중복");
        }

        [Test]
        public void 위반_파라미터나_효과가_존재하지_않는_유물을_가리키면_로드_오류다()
        {
            AssertInvalid(s => s.Params.Add(new Relic_Effect_ParamRow { relicId = "Ghost", effectIndex = 1, paramKey = "amount", value = 1f }),
                "Relic_Effect_Param.json relicId 가 Relic 에 없음");
            AssertInvalid(s => s.Effects.Add(new Relic_EffectRow { relicId = "Ghost", effectIndex = 1, effectKind = "SoulBonus" }),
                "Relic_Effect.json relicId 가 Relic 에 없음");
        }

        [Test]
        public void 위반_드랍_행의_유물이나_영웅이_존재하지_않으면_로드_오류다()
        {
            AssertInvalid(s => s.Drops.Add(new Relic_DropRow { relicId = "Ghost", heroId = "Knight", weight = 1 }),
                "Relic_Drop.json relicId 가 Relic 에 없음");
            AssertInvalid(s => s.Drops.Add(new Relic_DropRow { relicId = "HeartOfStone", heroId = "Nobody", weight = 1 }),
                "heroId Nobody 가 Chapter.json 에 없음");
        }

        [Test]
        public void 위반_드랍_복합키가_중복되면_로드_오류다()
        {
            AssertInvalid(s => s.Drops.Add(new Relic_DropRow { relicId = "HeartOfStone", heroId = "Knight", weight = 9 }),
                "(HeartOfStone, Knight) 중복");
        }

        [Test]
        public void 위반_드랍_weight가_1_미만이면_로드_오류다()
        {
            AssertInvalid(s => s.Drops[0].weight = 0, "weight 는 1 이상");
            AssertInvalid(s => s.Drops[0].weight = -3, "weight 는 1 이상");
        }

        [Test]
        public void 위반_어느_영웅에도_드랍되지_않는_유물이_있으면_로드_오류다()
        {
            AssertInvalid(s => s.Drops.RemoveAll(d => d.relicId == "SpawnPrelude"), "SpawnPrelude 가 없음(드랍 불가능 유물)");
        }

        [Test]
        public void 위반_영웅에_Common_후보가_하나도_없으면_로드_오류다()
        {
            AssertInvalid(s =>
            {
                List<string> commons = new List<string>();
                foreach (RelicRow relic in s.Relics)
                {
                    if (relic.rarity == "Common")
                    {
                        commons.Add(relic.relicId);
                    }
                }
                s.Drops.RemoveAll(d => d.heroId == "Paladin" && commons.Contains(d.relicId));
            }, "영웅 Paladin 에 Common 후보가 없음");
        }

        [Test]
        public void 위반_등급_행이_빠지거나_중복되거나_정의되지_않으면_로드_오류다()
        {
            AssertInvalid(s => s.Rarities.RemoveAll(r => r.rarity == "Rare"), "Rare 행이 없음(3행 필요)");
            AssertInvalid(s => s.Rarities.Add(new RelicRarityRow { rarity = "Common", nameKey = 1344, baseWeight = 1, colorHex = "#fff" }),
                "rarity Common 중복");
            AssertInvalid(s => s.Rarities[0].rarity = "Mythic", "rarity 가 ERelicRarity 에 없음");
        }

        [Test]
        public void 위반_등급_baseWeight가_1_미만이거나_nameKey가_없으면_로드_오류다()
        {
            AssertInvalid(s => s.Rarities[0].baseWeight = 0, "baseWeight 는 1 이상");
            AssertInvalid(s => s.Rarities[0].nameKey = 999999, "Strings_Ko 에 없음");
        }

        [Test]
        public void 위반_RelicStack이_비었거나_번호가_연속이_아니면_로드_오류다()
        {
            AssertInvalid(s => s.Stacks.Clear(), "RelicStack.json 이 비어 있음");
            AssertInvalid(s => s.Stacks[4].stack = 6, "stack 은 1부터 연속");
            AssertInvalid(s => s.Stacks[4].stack = 4, "stack 은 1부터 연속");
        }

        [Test]
        public void 위반_RelicStack_1단계_배율이_1_0이_아니면_로드_오류다()
        {
            AssertInvalid(s => s.Stacks[0].effectScale = 1.2f, "stack 1 의 effectScale 은 1.0");
        }

        [Test]
        public void 위반_RelicStack이_단조_증가가_아니면_로드_오류다()
        {
            AssertInvalid(s => s.Stacks[2].effectScale = 1.5f, "단조 증가가 아님");
            AssertInvalid(s => s.Stacks[1].effectScale = 1.0f, "단조 증가가 아님");
        }

        [Test]
        public void 위반_RelicStack의_한계_증가분이_커지면_체감_감소_위반이다()
        {
            AssertInvalid(s => s.Stacks[3].effectScale = 2.8f, "체감 감소 위반");
        }

        [Test]
        public void 위반_유물_maxStack이_줄어든_스케일표_최대보다_크면_로드_오류다()
        {
            AssertInvalid(s => s.Stacks.RemoveAt(4), "maxStack 5 은 1~4");
        }

        [Test]
        public void 위반_Stage_Relic이_스테이지를_빠뜨리거나_중복하거나_범위를_벗어나면_로드_오류다()
        {
            AssertInvalid(s => s.StageChances.RemoveAll(r => r.chapterId == 1 && r.stageIndex == 1), "에 (1, 1) 행이 없음");
            AssertInvalid(s => s.StageChances.Add(new Stage_RelicRow { chapterId = 1, stageIndex = 1, dropChance = 0.3f }), "(1, 1) 중복");
            AssertInvalid(s => s.StageChances[0].dropChance = 1.5f, "dropChance 는 0~1");
            AssertInvalid(s => s.StageChances[0].dropChance = -0.1f, "dropChance 는 0~1");
        }

        [Test]
        public void Stage_Relic_dropChance_경계값_0과_1은_허용된다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.StageChances[0].dropChance = 0f;
            set.StageChances[1].dropChance = 1f;

            RelicDatabase db = null;
            Assert.DoesNotThrow(() => db = set.Build());
            Assert.AreEqual(0f, db.StageDropChance(1, 1));
            Assert.AreEqual(1f, db.StageDropChance(1, 2));
        }

        [Test]
        public void 위반_Difficulty_Relic이_난이도를_빠뜨리거나_Easy가_1_0이_아니거나_단조가_깨지면_로드_오류다()
        {
            AssertInvalid(s => s.DifficultyChances.RemoveAll(r => r.difficulty == 2), "difficulty 2 행이 없음");
            AssertInvalid(s => s.DifficultyChances[0].dropChanceMul = 0.9f, "Easy(0) 의 dropChanceMul 은 1.0");
            AssertInvalid(s => s.DifficultyChances[1].dropChanceMul = 0.9f, "단조 비감소 위반");
            AssertInvalid(s => s.DifficultyChances.Add(new Difficulty_RelicRow { difficulty = 1, dropChanceMul = 1.25f }),
                "difficulty 는 0~2 이고 중복이 없어야 함");
            AssertInvalid(s => s.DifficultyChances[2].difficulty = 3, "difficulty 는 0~2 이고 중복이 없어야 함");
        }

        [Test]
        public void 위반_Difficulty_Relic_Rarity가_빠지거나_Easy가_1_0이_아니거나_등급별_단조가_깨지면_로드_오류다()
        {
            AssertInvalid(s => s.DifficultyRarities.RemoveAll(r => r.difficulty == 2 && r.rarity == "Legendary"), "에 (2, Legendary) 행이 없음");
            AssertInvalid(s => s.DifficultyRarities.Find(r => r.difficulty == 0 && r.rarity == "Common").weightMul = 0.9f,
                "Easy Common weightMul 은 1.0");
            AssertInvalid(s => s.DifficultyRarities.Find(r => r.difficulty == 2 && r.rarity == "Common").weightMul = 0.9f,
                "Common 가 난이도 2 에서 단조 규칙 위반");
            AssertInvalid(s => s.DifficultyRarities.Find(r => r.difficulty == 2 && r.rarity == "Rare").weightMul = 1.0f,
                "Rare 가 난이도 2 에서 단조 규칙 위반");
            AssertInvalid(s => s.DifficultyRarities.Find(r => r.difficulty == 2 && r.rarity == "Legendary").weightMul = 1.5f,
                "Legendary 가 난이도 2 에서 단조 규칙 위반");
        }

        [Test]
        public void 위반_Difficulty_Relic_Rarity의_weightMul이_0이하이거나_중복이거나_등급이_틀리면_로드_오류다()
        {
            AssertInvalid(s => s.DifficultyRarities[3].weightMul = 0f, "weightMul 은 0 보다 커야 함");
            AssertInvalid(s => s.DifficultyRarities.Add(new Difficulty_Relic_RarityRow { difficulty = 1, rarity = "Rare", weightMul = 1.4f }),
                "(1, Rare) 중복");
            AssertInvalid(s => s.DifficultyRarities[0].rarity = "Mythic", "difficulty(0~2)/rarity 가 올바르지 않음");
        }

        [Test]
        public void 위반_RelicConfig가_없거나_범위를_벗어나면_로드_오류다()
        {
            AssertInvalid(s => s.Config = null, "RelicConfig.json 이 비어 있음");
            AssertInvalid(s => s.Config.lossChanceMul = 1.5f, "lossChanceMul 은 0~1");
            AssertInvalid(s => s.Config.lossChanceMul = -0.1f, "lossChanceMul 은 0~1");
            AssertInvalid(s => s.Config.pityPerMiss = -0.1f, "pityPerMiss/pityMaxBonus 는 0 이상");
            AssertInvalid(s => s.Config.pityMaxBonus = -0.1f, "pityPerMiss/pityMaxBonus 는 0 이상");
            AssertInvalid(s => s.Config.maxChance = 0f, "maxChance 는 0 초과 1 이하");
            AssertInvalid(s => s.Config.maxChance = 1.2f, "maxChance 는 0 초과 1 이하");
        }

        [Test]
        public void RelicConfig_경계값_lossChanceMul_0과_1_maxChance_1은_허용된다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Config.lossChanceMul = 0f;
            Assert.DoesNotThrow(() => set.Build());
            set.Config.lossChanceMul = 1f;
            set.Config.maxChance = 1f;
            Assert.DoesNotThrow(() => set.Build());
        }

        [Test]
        public void 여러_위반은_한_번에_모아서_던져진다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Relics[0].category = "Weird";
            set.Config.maxChance = 0f;
            set.Stacks[0].effectScale = 3f;

            ChapterDataException error = Assert.Throws<ChapterDataException>(() => set.Build());

            StringAssert.Contains("ERelicCategory", error.Message);
            StringAssert.Contains("maxChance", error.Message);
            StringAssert.Contains("effectScale", error.Message);
        }

        [Test]
        public void 챕터DB와_문구_검사기를_생략하면_교차_검증만_건너뛴다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Relics[0].nameKey = 999999;
            set.Drops.Add(new Relic_DropRow { relicId = "HeartOfStone", heroId = "Nobody", weight = 1 });
            set.StageChances.RemoveAll(r => r.chapterId == 5);

            Assert.DoesNotThrow(() => set.BuildWithoutCrossCheck());
            Assert.Throws<ChapterDataException>(() => set.Build());
        }

        [Test]
        public void null_배열은_빈_배열로_취급해_필수_행_누락_오류로_보고된다()
        {
            Assert.Throws<ChapterDataException>(() => RelicDatabase.Build(null, null, null, null, null, null, null, null, null, null));
        }
    }
}
