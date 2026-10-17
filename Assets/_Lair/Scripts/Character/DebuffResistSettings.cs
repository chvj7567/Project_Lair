using Lair.Data;

namespace Lair.Character
{
    //# HeroArchetype_DebuffResist row -> 불변 설정 (패시브 "신앙의 가호", chapter-5 §2.2). 시한 디버프 지속 x DurationMul, 영구 디버프 감소분 x PenaltyMul.
    public readonly struct DebuffResistSettings
    {
        public readonly float DurationMul;
        public readonly float PenaltyMul;

        public DebuffResistSettings(float durationMul, float penaltyMul)
        {
            DurationMul = durationMul;
            PenaltyMul = penaltyMul;
        }

        public static DebuffResistSettings From(HeroArchetype_DebuffResistRow row)
        {
            return new DebuffResistSettings(row.durationMul, row.penaltyMul);
        }
    }
}
