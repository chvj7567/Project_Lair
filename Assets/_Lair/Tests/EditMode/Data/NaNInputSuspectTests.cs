using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 의심 지점 고정 — float 범위 검사가 NaN 을 걸러내는가(§10 SJ3 "0 보다 커야 함" 류는 NaN 도 위반이어야 한다).
    //# 코드 읽기 근거: 아래 검사들은 `<= 0f`·`>= upper`·`< 1f` 만 쓰고 IsNaN 이 없어 NaN 이 통과한다(비교가 항상 false).
    //# 반면 HeroSkill_Param·Card_Param·Stage.heroScale 은 IsNaN 을 검사한다. 현재 출하 JSON 에는 NaN 이 없어 실사용 영향은 낮다 — 실패하면 프로덕션 보강(gameplay-programmer) 대상이다.
    [Category("SuspectNaN")]
    public class NaNInputSuspectTests
    {
        private static BalanceConfigRow Config()
        {
            return new BalanceConfigRow
            {
                heroHp = 4000,
                heroPower = 50,
                runDuration = 300f,
                passiveThresholds = new[] { 0.9f, 0.5f, 0.1f },
                activeThresholds = new[] { 30f, 90f, 150f },
            };
        }

        private static MonsterStatRow[] Monsters()
        {
            string[] names = { "Wisp", "Wraith", "Reaper", "Hex", "Plague", "Phantom" };
            MonsterStatRow[] rows = new MonsterStatRow[names.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new MonsterStatRow { monster = names[i], hp = 100, power = 10, range = 1f, cooldown = 1f, moveSpeed = 1f, spawnPeriod = 5f };
            }
            return rows;
        }

        [Test]
        public void 밸런스_런_길이가_NaN이면_위반이다()
        {
            BalanceConfigRow config = Config();
            config.runDuration = float.NaN;

            Assert.Throws<ChapterDataException>(() => BalanceConfig.Build(config, Monsters()));
        }

        [Test]
        public void 밸런스_패시브_임계_원소가_NaN이면_위반이다()
        {
            BalanceConfigRow config = Config();
            config.passiveThresholds = new[] { 0.9f, float.NaN, 0.1f };

            Assert.Throws<ChapterDataException>(() => BalanceConfig.Build(config, Monsters()));
        }

        [Test]
        public void 밸런스_액티브_임계_원소가_NaN이면_위반이다()
        {
            BalanceConfigRow config = Config();
            config.activeThresholds = new[] { 30f, float.NaN, 150f };

            Assert.Throws<ChapterDataException>(() => BalanceConfig.Build(config, Monsters()));
        }

        [TestCase("range")]
        [TestCase("cooldown")]
        [TestCase("moveSpeed")]
        [TestCase("spawnPeriod")]
        public void 몬스터_실수_필드가_NaN이면_위반이다(string field)
        {
            MonsterStatRow[] rows = Monsters();
            typeof(MonsterStatRow).GetField(field).SetValue(rows[2], float.NaN);

            Assert.Throws<ChapterDataException>(() => BalanceConfig.Build(Config(), rows), field);
        }

        [Test]
        public void 승리_시간_보너스가_NaN이면_위반이다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.Config.winTimeBonusPerSec = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));
        }

        [Test]
        public void 영주_XP_성장률이_NaN이면_위반이다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.Config.xpGrowth = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));
        }

        [Test]
        public void 상점_가격_성장률이_NaN이면_위반이다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.ShopItems[0].priceGrowth = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));
        }

        [Test]
        public void 상점_레벨당_배율이_NaN이면_위반이다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.Muls[0].perLevelMul = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));
        }

        [Test]
        public void 도전과제_임계가_NaN이면_위반이다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.Achievements[0].threshold = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));
        }

        [Test]
        public void 영웅_스킬_구간_HP_비율이_NaN이면_위반이다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Phases[0].hpFraction = float.NaN;

            Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildHeroSkills(data));
        }
    }
}
