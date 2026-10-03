using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 선형 스테이지 번호 / 영웅 스탯 도출 / v4→v5 마이그레이션 (chapter-system §2). gameplay-programmer 자체 스모크.
    public class ChapterProgressTests
    {
        [Test]
        public void 선형번호와_챕터인덱스는_경계_5와_6에서_챕터가_넘어간다()
        {
            Assert.AreEqual(1, StageProgress.ChapterOf(5));
            Assert.AreEqual(5, StageProgress.IndexOf(5));
            Assert.AreEqual(2, StageProgress.ChapterOf(6));
            Assert.AreEqual(1, StageProgress.IndexOf(6));
            Assert.AreEqual(7, StageProgress.ToLinear(2, 2));
            Assert.AreEqual(10, StageProgress.MaxStage);
        }

        [Test]
        public void 챕터1_보스_클리어하면_챕터2가_열리고_3장_스테이지는_잠긴다()
        {
            Assert.IsFalse(StageProgress.IsChapterUnlocked(2, 4));
            Assert.IsTrue(StageProgress.IsChapterUnlocked(2, 5));
            Assert.IsTrue(StageProgress.IsUnlocked(6, 5));
            Assert.IsFalse(StageProgress.IsUnlocked(7, 5));
        }

        [Test]
        public void 영웅_스탯은_직업배율과_스테이지배수를_곱해_한번만_반올림한다()
        {
            HeroArchetypeRow archer = new HeroArchetypeRow
            {
                heroId = "Archer", hpScale = 0.8f, powerScale = 0.8f, range = 6f, cooldown = 1f, moveSpeed = 3.2f, aiType = "Kiter",
            };
            StageRow stage = new StageRow { chapterId = 2, stageIndex = 1, hpMultiplier = 1.56f, powerMultiplier = 1.10f };

            HeroStats stats = HeroStatCalculator.Compute(4000, 50, archer, stage);

            Assert.AreEqual(4992, stats.Hp);
            Assert.AreEqual(44, stats.Power);
            Assert.AreEqual(6f, stats.Range, 0.0001f);
            Assert.AreEqual(3.2f, stats.MoveSpeed, 0.0001f);
        }

        [Test]
        public void v4_프로필은_v5로_승격되고_범위밖_전적은_제거된다()
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = 5, SelectedStage = 5, SelectedHero = "Knight" };
            p.StageRecords.Add(new StageRecordEntry { Stage = 3, Runs = 2 });
            p.StageRecords.Add(new StageRecordEntry { Stage = 11, Runs = 9 });

            MetaProfileMigration.Apply(p, linear => linear > 5 ? "Archer" : "Knight");

            Assert.AreEqual(5, p.Version);
            Assert.AreEqual(5, p.ClearedStage);
            Assert.AreEqual(5, p.SelectedStage);
            Assert.AreEqual("Knight", p.SelectedHero);
            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(3, p.StageRecords[0].Stage);
        }

        [Test]
        public void 마이그레이션은_잠긴_선택스테이지를_해금_범위로_보정하고_챕터영웅으로_재설정한다()
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = 7, SelectedStage = 10, SelectedHero = "Knight" };

            MetaProfileMigration.Apply(p, linear => linear > 5 ? "Archer" : "Knight");

            Assert.AreEqual(8, p.SelectedStage, "Selected 는 MaxPlayable(cleared+1=8) 이하");
            Assert.AreEqual("Archer", p.SelectedHero);
        }
    }
}
