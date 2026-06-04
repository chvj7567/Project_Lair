using System.Collections.Generic;
using Newtonsoft.Json;
using Lair.Character;

namespace Lair.EditorTools
{
    //# hero_skills.json 루트 — 스킬 정의(폴리모픽) + 로드아웃 페이즈(파일명 ref).
    public class HeroSkillsDto
    {
        [JsonProperty("skills")] public List<HeroSkillData> Skills = new List<HeroSkillData>();
        [JsonProperty("loadout")] public List<HeroSkillPhaseDto> Loadout = new List<HeroSkillPhaseDto>();
    }

    public class HeroSkillPhaseDto
    {
        [JsonProperty("hpFraction")] public float HpFraction;
        //# 스킬 .asset 파일명(확장자 제외). 예: "HeroSkill_DashStrike".
        [JsonProperty("skill")] public string Skill;
    }
}
