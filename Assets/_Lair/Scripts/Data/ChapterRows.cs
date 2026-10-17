using System;

namespace Lair.Data
{
    //# 챕터 시스템 JSON row 7종 (chapter-system §1.2). 순수 데이터 — JsonArrayUtility 로 파싱, 파일명 = EData 값명.

    //# Chapter.json — 핵심 엔티티: 챕터 하나. 스테이지 수(5 고정)·보스 여부는 필드로 두지 않는다.
    [Serializable]
    public class ChapterRow
    {
        public int chapterId;
        public int nameKey;
        public int themeTextKey;
        public string heroId;
        public string backdropKey;
    }

    //# Stage.json — 핵심 엔티티: 챕터 안의 스테이지 하나. 복합키 chapterId + stageIndex.
    [Serializable]
    public class StageRow
    {
        public int chapterId;
        public int stageIndex;
        public int nameKey;
        public float hpMultiplier;
        public float powerMultiplier;
        public float targetClearSec;
    }

    //# Stage_Rule.json — 스테이지에 부착된 규칙 하나(선택적 관계). 복합키 chapterId + stageIndex + ruleIndex.
    [Serializable]
    public class Stage_RuleRow
    {
        public int chapterId;
        public int stageIndex;
        public int ruleIndex;
        public string ruleId;
    }

    //# Stage_Rule_Param.json — 규칙 하나의 수치 하나(key-value). 복합키 + paramKey.
    [Serializable]
    public class Stage_Rule_ParamRow
    {
        public int chapterId;
        public int stageIndex;
        public int ruleIndex;
        public string paramKey;
        public float value;
    }

    //# Stage_Rule_Trigger.json — 영웅 HP 구간 발동 규칙(보스 페이즈)만 존재하는 선택적 관계.
    [Serializable]
    public class Stage_Rule_TriggerRow
    {
        public int chapterId;
        public int stageIndex;
        public int ruleIndex;
        public float hpFraction;
    }

    //# HeroArchetype.json — 영웅 직업 하나. hp/power 앵커는 balance_config 에 유지, 직업은 배율만 가진다.
    [Serializable]
    public class HeroArchetypeRow
    {
        public string heroId;
        public int nameKey;
        public float hpScale;
        public float powerScale;
        public float range;
        public float cooldown;
        public float moveSpeed;
        public string aiType;
    }

    //# HeroArchetype_Kite.json — aiType = Kiter 인 영웅만 존재하는 선택적 관계.
    [Serializable]
    public class HeroArchetype_KiteRow
    {
        public string heroId;
        public float triggerDist;
        public float resumeDist;
        public float threatRadius;
        public float arenaSoftRadius;
        public float corneredCheckSec;
        public float corneredMinMove;
        public float minDwellSec;
    }

    //# HeroArchetype_Skirmish.json — aiType = Skirmisher 인 영웅만 존재하는 선택적 관계 (chapter-3-hero §2.2).
    [Serializable]
    public class HeroArchetype_SkirmishRow
    {
        public string heroId;
        public float huntRadius;
        public float retargetScoreMul;
        public float strikeWindowSec;
        public int crowdCount;
        public float crowdRadius;
        public float slipDistance;
        public float slipDurationSec;
        public float slipInvulnSec;
        public float slipCooldownSec;
        public float minDwellSec;
        public float arenaSoftRadius;
    }
}
