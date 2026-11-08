using System;

namespace Lair.Data
{
    //# 몬스터 등급 연출 JSON row (monster-grades §8). 모든 거리·폭은 "셀 비율"(종족 cellWorld 를 곱해 월드 길이가 된다).

    //# Grade_Glow.json — Grade 의 선택적 관계(2~5 등급만): 몸 외곽 후광 바깥 폭.
    [Serializable]
    public class Grade_GlowRow
    {
        public int grade;
        public float widthCell;
    }

    //# Grade_Aura.json — Grade 의 선택적 관계(2~5 등급만): 바닥 오라(룬 서클).
    [Serializable]
    public class Grade_AuraRow
    {
        public int grade;
        public float opacity;
        public float scale;
        public float offsetY;
    }

    //# Grade_Pulse.json — Grade 의 선택적 관계(5 등급만): 몸 밝기·채도 맥동.
    [Serializable]
    public class Grade_PulseRow
    {
        public int grade;
        public float brightnessMin;
        public float brightnessMax;
        public float saturationMin;
        public float saturationMax;
        public float periodSec;
    }

    //# Grade_Fx.json — Grade 의 선택적 관계, 복합키 grade + index: 이펙트 하나(불꽃 기둥/후광). kind = "Tornado" | "Sunburst".
    [Serializable]
    public class Grade_FxRow
    {
        public int grade;
        public int index;
        public string kind;
        public float offsetX;
        public float offsetY;
        public float scale;
        public float opacity;
        public int frameOffset;
    }

    //# Monster_GradeFxColor.json — 몬스터(기준 6종) 하나의 이펙트·후광 색.
    [Serializable]
    public class Monster_GradeFxColorRow
    {
        public string monster;
        public string glowColor;
    }
}
