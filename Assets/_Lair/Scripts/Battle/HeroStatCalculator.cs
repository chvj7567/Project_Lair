using Lair.Data;

namespace Lair.Battle
{
    //# 영웅 최종 스탯 — 직업(HeroArchetype) + 스테이지(Stage) 로부터 도출한 값 묶음.
    public readonly struct HeroStats
    {
        public readonly int Hp;
        public readonly int Power;
        public readonly float Range;
        public readonly float Cooldown;
        public readonly float MoveSpeed;

        public HeroStats(int hp, int power, float range, float cooldown, float moveSpeed)
        {
            Hp = hp;
            Power = power;
            Range = range;
            Cooldown = cooldown;
            MoveSpeed = moveSpeed;
        }
    }

    //# 영웅 스탯 도출 순수 헬퍼 (chapter-system §2.1) — Unity 비의존, EditMode 검증 가능.
    //# HP/Power = ScaleHeroStat(앵커, 직업 배율 x 스테이지 배수) 1회 반올림. range/cooldown/moveSpeed 는 직업 값 그대로.
    public static class HeroStatCalculator
    {
        public static HeroStats Compute(int anchorHp, int anchorPower, HeroArchetypeRow archetype, StageRow stage)
        {
            return new HeroStats(
                StageProgress.ScaleHeroStat(anchorHp, archetype.hpScale, stage.hpMultiplier),
                StageProgress.ScaleHeroStat(anchorPower, archetype.powerScale, stage.powerMultiplier),
                archetype.range,
                archetype.cooldown,
                archetype.moveSpeed);
        }

        //# 난이도 행을 곱하는 오버로드 — difficulty 가 null(Easy)이면 기존 4인자 결과와 동일. range/cooldown/moveSpeed 는 직업 값 그대로.
        public static HeroStats Compute(int anchorHp, int anchorPower, HeroArchetypeRow archetype, StageRow stage, DifficultyRow difficulty)
        {
            if (difficulty == null)
                return Compute(anchorHp, anchorPower, archetype, stage);
            return new HeroStats(
                StageProgress.ScaleHeroStat(anchorHp, archetype.hpScale, stage.hpMultiplier, difficulty.hpMultiplier),
                StageProgress.ScaleHeroStat(anchorPower, archetype.powerScale, stage.powerMultiplier, difficulty.powerMultiplier),
                archetype.range,
                archetype.cooldown,
                archetype.moveSpeed);
        }
    }
}
