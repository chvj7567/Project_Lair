using System;

namespace Lair.Data
{
    //# 영웅 스킬 JSON row 3종 (so-to-json-migration §3.9). 파일명 = EData 값명. 선택적 관계는 별도 row, sentinel 없음.

    //# HeroSkill.json — 핵심 엔티티: 영웅 스킬 하나. skillId 가 곧 동작 종류(런타임 클래스 선택 키). PK skillId.
    [Serializable]
    public class HeroSkillRow
    {
        public string skillId;      //# EHeroSkill 값명
        public int nameKey;         //# Strings_Ko — 해금 컷인에 표시되는 이름
    }

    //# HeroSkill_Param.json — HeroSkill 의 관계: 스킬 수치 하나(스킬마다 필드 구성이 달라 키-값). 복합키 skillId + paramKey.
    [Serializable]
    public class HeroSkill_ParamRow
    {
        public string skillId;
        public string paramKey;
        public float value;         //# 정수 의미 값(damage·tickCount 등)도 float 로 저장하고 읽을 때 반올림
    }

    //# HeroArchetype_Skill.json — HeroArchetype 의 관계: 이 영웅이 HP 가 이 비율 이하일 때 여는 스킬. 복합키 heroId + phaseIndex.
    [Serializable]
    public class HeroArchetype_SkillRow
    {
        public string heroId;       //# HeroArchetype.heroId
        public int phaseIndex;      //# 1부터 연속
        public float hpFraction;    //# 해금 HP 비율 (0 초과 1 미만, 구간이 늘수록 작아진다)
        public string skillId;
    }

    //# 영웅 스킬 JSON 3종을 한 번에 넘기는 묶음 — HeroSkillDatabase.Build 입력 (MetaDataSet 선례).
    public class HeroSkillDataSet
    {
        public HeroSkillRow[] Skills;
        public HeroSkill_ParamRow[] Params;
        public HeroArchetype_SkillRow[] Phases;
    }
}
