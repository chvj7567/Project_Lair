using UnityEngine;

namespace Lair.Data
{
    //# 종족 강화 발광색 단일 SoT — 전투 발광·상점 셀 프레임이 같은 메서드를 읽어 "메뉴 색=전장 색" 보장.
    //# Lair.Data 레이어라 Character·UI 양쪽 역참조 없이 참조. 값·정규화 규칙은 기획서 §4.2 참조.
    public static class SpeciesVisual
    {
        //# 각 종족 SpeciesColor 의 색조를 유지하되 최대 RGB 성분 = 0.90 으로 정규화(§4.2) —
        //# 다크 배경 프레임·Lv1 세기 1.5 발광에서 6종이 균일하게 보이도록.
        public static Color SpeciesGlowColor(EMonster species) => species switch
        {
            EMonster.Wisp    => new Color(0.155f, 0.900f, 0.430f, 1f),   //# #28E66E
            EMonster.Wraith  => new Color(0.753f, 0.801f, 0.900f, 1f),   //# #C0CCE6 냉백 유령빛
            EMonster.Reaper  => new Color(0.900f, 0.256f, 0.256f, 1f),   //# #E64141
            EMonster.Hex     => new Color(0.900f, 0.688f, 0.030f, 1f),   //# #E6AF08
            EMonster.Plague  => new Color(0.612f, 0.309f, 0.900f, 1f),   //# #9C4FE6
            EMonster.Phantom => new Color(0.508f, 0.671f, 0.900f, 1f),   //# #82ABE6 청회 상향
            _                => Color.white,
        };
    }
}
