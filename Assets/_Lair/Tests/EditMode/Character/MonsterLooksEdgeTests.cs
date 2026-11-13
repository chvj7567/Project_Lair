using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;
using Lair.Meta;

namespace Lair.Tests.Character
{
    //# 전투·쇼케이스 공유 외형 계산 엣지·회귀(village-monster-showcase §2·§8, T-S2·T-S7) - 시드(MonsterLooksTests)의 등급·티어 산출·편성 없음과 중복하지 않는다.
    //# BattleController 추출 전 원본 알고리즘을 테스트 안에 복제해 임의 편성/강화 Lv 에서 결과가 일치함을 고정한다.
    public class MonsterLooksEdgeTests
    {
        private static readonly string[] Ids = { "WispLv", "WraithLv", "ReaperLv", "HexLv", "PlagueLv", "PhantomLv" };

        //# BattleController.LoadMonsterTierSheets 의 추출 전 로직(커밋 f07c3dd 이전) - 회귀 기준.
        private static List<(EMonster, int)> LegacyNeeds(IReadOnlyList<EMonster> formation, IReadOnlyDictionary<EMonster, int> levels)
        {
            IReadOnlyList<EMonster> placed = formation != null ? formation : MonsterGrades.BaseMonsters;
            List<(EMonster, int)> needs = new List<(EMonster, int)>();
            HashSet<EMonster> seen = new HashSet<EMonster>();
            foreach (EMonster monster in placed)
            {
                EMonster root = MonsterGrades.BaseOf(monster);
                if (seen.Add(root) == false)
                    continue;
                int level = levels.TryGetValue(root, out int lv) ? lv : 0;
                int tier = MonsterEnhancementVisual.TierOfLevel(level);
                if (tier > 0)
                {
                    needs.Add((root, tier));
                }
            }
            return needs;
        }

        [Test]
        public void 임의_편성과_강화_Lv_에서_필요_시트_목록이_추출_전_전투_로직과_같다()
        {
            System.Random random = new System.Random(20261108);
            for (int iteration = 0; iteration < 300; iteration++)
            {
                EMonster[] formation = new EMonster[6];
                for (int i = 0; i < 6; i++)
                {
                    formation[i] = (EMonster)random.Next(0, 30);
                }
                Dictionary<EMonster, int> levels = new Dictionary<EMonster, int>();
                foreach (EMonster monster in MonsterGrades.BaseMonsters)
                {
                    if (random.Next(0, 3) > 0)
                    {
                        levels[monster] = random.Next(0, 6);
                    }
                }

                List<(EMonster, int)> expected = LegacyNeeds(formation, levels);
                List<(EMonster, int)> actual = MonsterLooks.TierNeeds(MonsterLooks.Compute(formation, levels));

                Assert.AreEqual(expected, actual, $"반복 {iteration}");
            }
        }

        [Test]
        public void 편성이_null_이어도_추출_전_전투_로직과_같다()
        {
            Dictionary<EMonster, int> levels = new Dictionary<EMonster, int> { { EMonster.Wisp, 1 }, { EMonster.Phantom, 5 }, { EMonster.Hex, 0 } };

            Assert.AreEqual(LegacyNeeds(null, levels), MonsterLooks.TierNeeds(MonsterLooks.Compute(null, levels)));
        }

        [Test]
        public void 슬롯_번호와_편성_몬스터는_그대로_보존되고_등급_enum_도_유지된다()
        {
            EMonster[] formation = { EMonster.WraithG5, EMonster.Wisp, EMonster.ReaperG2, EMonster.Hex, EMonster.PlagueG4, EMonster.Phantom };

            MonsterLook[] looks = MonsterLooks.Compute(formation, new Dictionary<EMonster, int>());

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, looks[i].Slot);
                Assert.AreEqual(formation[i], looks[i].Monster, "프리팹 키는 등급 enum 그대로");
                Assert.AreEqual(MonsterGrades.BaseOf(formation[i]), looks[i].Base);
            }
            Assert.AreEqual(5, looks[0].Grade);
            Assert.AreEqual(1, looks[1].Grade);
        }

        [Test]
        public void 같은_종족이_편성에_두번_있어도_시트는_한번만_요청한다()
        {
            EMonster[] formation = { EMonster.Wisp, EMonster.WispG3, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague };

            List<(EMonster, int)> needs = MonsterLooks.TierNeeds(MonsterLooks.Compute(formation, new Dictionary<EMonster, int> { { EMonster.Wisp, 3 } }));

            Assert.AreEqual(new[] { (EMonster.Wisp, 2) }, needs.ToArray());
        }

        [Test]
        public void 빈_편성은_빈_결과이고_강화_표가_null_이면_전부_Lv0이다()
        {
            Assert.AreEqual(0, MonsterLooks.Compute(new EMonster[0], new Dictionary<EMonster, int>()).Length);
            Assert.AreEqual(0, MonsterLooks.TierNeeds(new MonsterLook[0]).Count);

            MonsterLook[] looks = MonsterLooks.Compute(null, null);
            Assert.AreEqual(6, looks.Length);
            foreach (MonsterLook look in looks)
            {
                Assert.AreEqual(0, look.Level);
                Assert.AreEqual(0, look.Tier);
            }
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        public void 강화_Lv_는_Level_로_보존되고_Tier_로_변환된다(int level, int tier)
        {
            MonsterLook look = MonsterLooks.Compute(null, new Dictionary<EMonster, int> { { EMonster.Plague, level } })[(int)EMonster.Plague];

            Assert.AreEqual(level, look.Level);
            Assert.AreEqual(tier, look.Tier);
        }

        //# ───────── 갱신 범위 (T-S7) - 구매·승급·편성 변경·클라우드 복원 후 다시 계산해도 바뀐 슬롯만 달라진다 ─────────

        private static bool SameLook(MonsterLook a, MonsterLook b)
            => a.Slot == b.Slot && a.Monster == b.Monster && a.Base == b.Base && a.Grade == b.Grade && a.Level == b.Level && a.Tier == b.Tier;

        private static List<int> ChangedSlots(MonsterLook[] before, MonsterLook[] after)
        {
            List<int> changed = new List<int>();
            for (int i = 0; i < before.Length; i++)
            {
                if (SameLook(before[i], after[i]) == false)
                {
                    changed.Add(i);
                }
            }
            return changed;
        }

        [Test]
        public void 한_종족_강화_Lv_만_바뀌면_그_슬롯의_외형만_달라진다()
        {
            Dictionary<EMonster, int> before = new Dictionary<EMonster, int> { { EMonster.Wisp, 1 }, { EMonster.Hex, 4 } };
            Dictionary<EMonster, int> after = new Dictionary<EMonster, int> { { EMonster.Wisp, 1 }, { EMonster.Hex, 5 } };

            List<int> changed = ChangedSlots(MonsterLooks.Compute(null, before), MonsterLooks.Compute(null, after));

            Assert.AreEqual(new[] { (int)EMonster.Hex }, changed.ToArray());
        }

        [Test]
        public void 한_종족_승급만_바뀌면_그_슬롯의_프리팹_키만_달라진다()
        {
            EMonster[] before = { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            EMonster[] after = (EMonster[])before.Clone();
            after[2] = EMonster.ReaperG2;

            List<int> changed = ChangedSlots(MonsterLooks.Compute(before, null), MonsterLooks.Compute(after, null));

            Assert.AreEqual(new[] { 2 }, changed.ToArray());
        }

        [Test]
        public void 강화_Lv_가_같으면_티어_시트_추가_요청은_없고_티어가_오르면_새_티어만_요청한다()
        {
            Dictionary<EMonster, int> lv2 = new Dictionary<EMonster, int> { { EMonster.Wisp, 2 } };
            Dictionary<EMonster, int> lv3 = new Dictionary<EMonster, int> { { EMonster.Wisp, 3 } };

            Assert.AreEqual(MonsterLooks.TierNeeds(MonsterLooks.Compute(null, lv2)), MonsterLooks.TierNeeds(MonsterLooks.Compute(null, lv2)), "같은 입력 → 같은 결과");
            Assert.AreEqual(new[] { (EMonster.Wisp, 1) }, MonsterLooks.TierNeeds(MonsterLooks.Compute(null, lv2)).ToArray());
            Assert.AreEqual(new[] { (EMonster.Wisp, 2) }, MonsterLooks.TierNeeds(MonsterLooks.Compute(null, lv3)).ToArray());
        }

        [Test]
        public void 클라우드_복원으로_모든_종족이_바뀌면_6슬롯이_전부_갱신_대상이다()
        {
            Dictionary<EMonster, int> before = new Dictionary<EMonster, int>();
            Dictionary<EMonster, int> after = new Dictionary<EMonster, int>();
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                after[monster] = 5;
            }

            Assert.AreEqual(6, ChangedSlots(MonsterLooks.Compute(null, before), MonsterLooks.Compute(null, after)).Count);
        }

        //# ───────── SpeciesLevels ─────────

        private static MetaConfig ConfigWithSpecies(params int[] maxLevels)
        {
            MetaConfig config = new MetaConfig();
            config.ShopItems = new List<ShopItemDef>();
            for (int i = 0; i < 6; i++)
            {
                config.ShopItems.Add(new ShopItemDef
                {
                    Id = Ids[i],
                    EffectKind = EShopEffectKind.MonsterSpecies,
                    Species = (EMonster)i,
                    MaxLevel = maxLevels.Length > i ? maxLevels[i] : 5,
                });
            }
            return config;
        }

        [Test]
        public void 종족_강화_Lv_는_프로필_값을_최대_레벨로_클램프해_읽는다()
        {
            MetaConfig config = ConfigWithSpecies(5, 5, 3, 5, 5, 5);
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("WispLv", 4);
            profile.SetShopLevel("ReaperLv", 9);
            profile.SetShopLevel("HexLv", -2);

            Dictionary<EMonster, int> levels = MonsterLooks.SpeciesLevels(profile, config);

            Assert.AreEqual(4, levels[EMonster.Wisp]);
            Assert.AreEqual(3, levels[EMonster.Reaper], "최대 3 으로 클램프");
            Assert.AreEqual(0, levels[EMonster.Hex], "음수는 0");
            Assert.AreEqual(0, levels[EMonster.Plague], "저장값 없음 → 0");
            Assert.AreEqual(6, levels.Count);
        }

        [Test]
        public void 종족_강화가_아닌_상점_항목과_null_항목은_무시한다()
        {
            MetaConfig config = ConfigWithSpecies();
            config.ShopItems.Add(new ShopItemDef { Id = "SoulBoost", EffectKind = EShopEffectKind.MonsterStat, MaxLevel = 5 });
            config.ShopItems.Add(null);
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("SoulBoost", 5);

            Dictionary<EMonster, int> levels = MonsterLooks.SpeciesLevels(profile, config);

            Assert.AreEqual(6, levels.Count);
        }

        [Test]
        public void 설정이나_프로필이_null_이면_빈_표를_돌려준다()
        {
            MetaConfig config = ConfigWithSpecies();

            Assert.AreEqual(0, MonsterLooks.SpeciesLevels(new MetaProfile(), null).Count);
            Assert.AreEqual(0, MonsterLooks.SpeciesLevels(null, config).Count);
        }
    }
}
