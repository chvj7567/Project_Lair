using System;
using Lair.Data;

namespace Lair.Character
{
    //# 영웅 스킬 데이터의 추상 베이스. HeroSkill.json·HeroSkill_Param.json 조립 결과로 만든다(so-to-json-migration §7.3).
    //# 서브클래스가 수치 + behavior 를 캡슐화한다. 공유 인스턴스이므로 가변 상태는 보관 금지 — CreateRuntime() 이 만든 런타임이 보유.
    public abstract class HeroSkillData
    {
        protected HeroSkillData(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }

        //# 같은 프레임에 여러 스킬 조건이 참일 때 먼저 Tick 되는 순서 — 클수록 먼저. 기본 0(기존 스킬은 활성화 순서 유지).
        public virtual int CastPriority => 0;

        //# 활성화 시 1회 호출. 이 스킬의 가변 상태 런타임 생성.
        public abstract IHeroSkillRuntime CreateRuntime();

        //# EHeroSkill → 구상 클래스 (so-to-json-migration §7.3). 새 스킬은 여기 한 줄 + 클래스(수치 필드명 = paramKey).
        public static HeroSkillData Create(HeroSkillEntry entry)
        {
            switch (entry.Id)
            {
                case EHeroSkill.DashStrike: return new DashStrikeSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.AoeNova: return new AoeNovaSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.OrbitingBlade: return new OrbitingBladeSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.PiercingShot: return new PiercingShotSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.ArrowVolley: return new ArrowVolleySkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.Evade: return new EvadeSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.ShadowStrike: return new ShadowStrikeSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.BladeWhirl: return new BladeWhirlSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.SmokeBomb: return new SmokeBombSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.CrushingLeap: return new CrushingLeapSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.ChainPull: return new ChainPullSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.BloodFrenzy: return new BloodFrenzySkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.JudgmentHammer: return new JudgmentHammerSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.PurifyingLight: return new PurifyingLightSkillData(entry.DisplayName, entry.Params);
                case EHeroSkill.Sanctuary: return new SanctuarySkillData(entry.DisplayName, entry.Params);
                default: throw new ArgumentOutOfRangeException(nameof(entry), entry.Id, "HeroSkillData.Create 에 없는 스킬");
            }
        }
    }

    //# 영웅 HP 구간 하나가 여는 스킬 — HeroSkillRunner.Bind 입력 (HeroArchetype_Skill 한 행을 스킬 인스턴스로 푼 것).
    public readonly struct HeroSkillPhase
    {
        public readonly float HpFraction;
        public readonly HeroSkillData Skill;

        public HeroSkillPhase(float hpFraction, HeroSkillData skill)
        {
            HpFraction = hpFraction;
            Skill = skill;
        }
    }
}
