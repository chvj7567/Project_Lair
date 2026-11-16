using UnityEngine;

namespace Lair.UI
{
    //# 편성 아이콘 등급 테두리 색 (목업 dungeon-formation.html gradeStyle, 영웅 5단계 테두리 규약과 같은 순서).
    //# 1·3 = 잉크, 2 = 밝은 테, 4 = 종족색, 5 = 금. 아이콘 자체는 기준 몬스터 것을 쓴다(monster-grades V7).
    public static class MonsterGradeFrame
    {
        private static readonly Color Ink = new Color32(0x07, 0x09, 0x0e, 0xff);
        private static readonly Color Bright = new Color32(0xf8, 0xfa, 0xfc, 0xff);
        private static readonly Color Gold = new Color32(0xfd, 0xe0, 0x47, 0xff);

        private static readonly Color[] BadgeColors =
        {
            new Color32(0x9a, 0xa5, 0xbd, 0xff), new Color32(0x4c, 0xd9, 0x64, 0xff), new Color32(0x4a, 0xa3, 0xff, 0xff),
            new Color32(0xb8, 0x6b, 0xff, 0xff), new Color32(0xff, 0xb0, 0x20, 0xff),
        };

        //# 등급 동그라미 배지색 — 1 회색·2 초록·3 파랑·4 보라·5 금 (formation-states 목업). 테두리색과 별개.
        public static Color BadgeColor(int grade)
        {
            return BadgeColors[Mathf.Clamp(grade, 1, BadgeColors.Length) - 1];
        }

        public static Color BorderColor(int grade, Color raceColor)
        {
            switch (grade)
            {
                case 2:
                    return Bright;
                case 4:
                    return raceColor;
                case 5:
                    return Gold;
                default:
                    return Ink;
            }
        }
    }
}
