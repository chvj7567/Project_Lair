using System;

namespace Lair.Data
{
    //# 무한의 탑 JSON row 3종 (infinite-tower §4.2). 순수 데이터 — 파일명 = EData 값명. sentinel 없음.

    //# TowerConfig.json (단일 객체) — 핵심 엔티티: 탑 전역 규칙 수치.
    [Serializable]
    public class TowerConfigRow
    {
        public int seasonId;
        public int seed;
        public int unlockClearedStage;
        public float hpBase;
        public float hpGrowthPerFloor;
        public float powerGrowthPerFloor;
        public float bossHpMul;
        public float bossPowerMul;
        public float multiplierCap;
        public int relicNormalFromFloor;
        public int relicHardFromFloor;
        public int previewFloors;
        public int leaderboardTop;
        //# 심연 결정 이정표 (monster-growth-currencies §2.3) — 층 번호가 every 의 배수면 bonus 추가. every 0 = 이정표 없음.
        public int crystalMilestoneEvery;
        public int crystalMilestoneBonus;
    }

    //# HeroArchetype_Tower.json — HeroArchetype 의 관계. 이 직업이 탑에 나올 때의 HP/공격력 보정. PK heroId.
    [Serializable]
    public class HeroArchetype_TowerRow
    {
        public string heroId;
        public float hpMultiplier;
        public float powerMultiplier;
    }

    //# Stage_Tower.json — Stage 의 관계. 이 스테이지 규칙 묶음이 탑 템플릿으로 쓰인다. 복합키 chapterId + stageIndex(1~4 일반, 5 보스).
    [Serializable]
    public class Stage_TowerRow
    {
        public int chapterId;
        public int stageIndex;
        public int minFloor;
        public float ruleFactor;
    }
}
