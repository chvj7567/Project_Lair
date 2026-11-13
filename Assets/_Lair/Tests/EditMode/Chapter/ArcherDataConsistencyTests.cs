using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 궁수 데이터 일관성 — 영웅 스킬 JSON(HeroSkill·HeroSkill_Param·HeroArchetype_Skill) 의 궁수·기사 값과 스킬 게이트, 영웅 스탯, 열거형 (chapter-system §1.4·§5.4).
    //# 스킬 에셋·hero_skills.json 동기 사본이 사라져 에셋 ↔ JSON 일치 검증은 없다(so-to-json-migration §8.1). 실제 프로젝트 파일은 수정하지 않는다.
    public class ArcherDataConsistencyTests : ChapterStaticStateTestBase
    {
        //# --- 스킬 수치 ---

        [Test]
        public void 궁수_스킬_수치는_기획서_5_4_표와_같다()
        {
            PiercingShotSkillData pierce = WorldMapTestSupport.ShippedSkill<PiercingShotSkillData>();
            ArrowVolleySkillData volley = WorldMapTestSupport.ShippedSkill<ArrowVolleySkillData>();
            EvadeSkillData evade = WorldMapTestSupport.ShippedSkill<EvadeSkillData>();

            Assert.AreEqual(70, pierce.Damage);
            Assert.AreEqual(4f, pierce.Cooldown, 0.0001f);
            Assert.AreEqual(9f, pierce.LineLength, 0.0001f);
            Assert.AreEqual(0.6f, pierce.LineHalfWidth, 0.0001f);
            Assert.AreEqual(40, volley.DamagePerTick);
            Assert.AreEqual(3, volley.TickCount);
            Assert.AreEqual(0.5f, volley.TickIntervalSec, 0.0001f);
            Assert.AreEqual(2.8f, volley.Radius, 0.0001f);
            Assert.AreEqual(8f, volley.Cooldown, 0.0001f);
            Assert.AreEqual(30, evade.Damage);
            Assert.AreEqual(10f, evade.Cooldown, 0.0001f);
            Assert.AreEqual(4.5f, evade.LeapDistance, 0.0001f);
            Assert.AreEqual(0.25f, evade.LeapDurationSec, 0.0001f);
            Assert.AreEqual(2f, evade.BlastRadius, 0.0001f);
            Assert.AreEqual(3, evade.TriggerMonsterCount);
            Assert.AreEqual(2f, evade.TriggerRadius, 0.0001f);
        }

        //# --- 영웅별 스킬 구간 ---

        [Test]
        public void 영웅별_스킬_구간의_heroId는_모두_EHero_값명이고_영웅마다_3개다()
        {
            HeroSkillDatabase db = WorldMapTestSupport.ShippedHeroSkills();
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (HeroArchetype_SkillRow row in WorldMapTestSupport.ShippedHeroSkillData().Phases)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(EHero), row.heroId), $"heroId {row.heroId}");
                counts[row.heroId] = counts.TryGetValue(row.heroId, out int c) ? c + 1 : 1;
            }

            Assert.AreEqual(3, counts["Knight"]);
            Assert.AreEqual(3, counts["Archer"]);
            Assert.AreEqual(3, counts["Assassin"]);
            Assert.AreEqual(3, counts["Berserker"]);
            Assert.AreEqual(3, counts["Paladin"]);
            Assert.AreEqual(5, counts.Count);
            foreach (string heroId in counts.Keys)
            {
                Assert.AreEqual(3, db.GetPhases(heroId).Count, heroId);
            }
        }

        [Test]
        public void 궁수_구간은_HP_85_65_45_내림차순이고_스킬_순서가_관통_소나기_도약이다()
        {
            IReadOnlyList<HeroSkillPhaseEntry> archer = WorldMapTestSupport.ShippedHeroSkills().GetPhases("Archer");
            float[] expected = { 0.85f, 0.65f, 0.45f };
            EHeroSkill[] skills = { EHeroSkill.PiercingShot, EHeroSkill.ArrowVolley, EHeroSkill.Evade };

            Assert.AreEqual(3, archer.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(expected[i], archer[i].HpFraction, 0.0001f);
                Assert.AreEqual(skills[i], archer[i].Skill);
            }
        }

        [Test]
        public void 기사와_궁수의_스킬_해금_HP는_같은_게이트_값을_쓴다()
        {
            HeroSkillDatabase db = WorldMapTestSupport.ShippedHeroSkills();
            IReadOnlyList<HeroSkillPhaseEntry> knight = db.GetPhases("Knight");
            IReadOnlyList<HeroSkillPhaseEntry> archer = db.GetPhases("Archer");

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(knight[i].HpFraction, archer[i].HpFraction, 0.0001f);
            }
        }

        //# --- 영웅 스탯 / 열거형 ---

        [Test]
        public void 궁수_스테이지_2_1_영웅_스탯은_HP_4992_공격력_44를_유지한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            HeroStats stats = HeroStatCalculator.Compute(4000, 50, db.GetHero("Archer"), db.GetStage(2, 1));

            Assert.AreEqual(4992, stats.Hp);
            Assert.AreEqual(44, stats.Power);
            Assert.AreEqual(6f, stats.Range, 0.0001f);
        }

        [Test]
        public void 궁수_FX_Enum_키가_정의돼_있다()
        {
            foreach (string name in new[] { "HeroPiercingShotFx", "HeroArrowVolleyFx", "HeroEvadeFx", "ArcherArrowFx" })
            {
                Assert.IsTrue(Enum.IsDefined(typeof(EVisual), name), name);
            }
            Assert.IsTrue(Enum.IsDefined(typeof(EHero), "Archer"));
        }
    }
}
