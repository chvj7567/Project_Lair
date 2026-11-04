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
