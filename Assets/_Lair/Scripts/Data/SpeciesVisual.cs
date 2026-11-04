using System;
using UnityEngine;

namespace Lair.Data
{
    //# 종족 표시 단일 SoT — 발광색·한글 이름. 전투·UI 모든 표시 지점이 이 하나를 읽어 문구/색 갈림 방지.
    //# Lair.Data 레이어라 Character·UI 양쪽 역참조 없이 참조. 발광색 값·정규화 규칙은 기획서 §4.2 참조.
    public static class SpeciesVisual
    {
        //# 각 종족 SpeciesColor 의 색조를 유지하되 최대 RGB 성분 = 0.90 으로 정규화(§4.2) —
        //# 다크 배경 프레임·Lv1 세기 1.5 발광에서 6종이 균일하게 보이도록.
        public static Color SpeciesGlowColor(EMonster species) => MonsterGrades.BaseOf(species) switch
        {
            EMonster.Wisp    => new Color(0.155f, 0.900f, 0.430f, 1f),   //# #28E66E
            EMonster.Wraith  => new Color(0.753f, 0.801f, 0.900f, 1f),   //# #C0CCE6 냉백 유령빛
            EMonster.Reaper  => new Color(0.900f, 0.256f, 0.256f, 1f),   //# #E64141
            EMonster.Hex     => new Color(0.900f, 0.688f, 0.030f, 1f),   //# #E6AF08
            EMonster.Plague  => new Color(0.612f, 0.309f, 0.900f, 1f),   //# #9C4FE6
            EMonster.Phantom => new Color(0.508f, 0.671f, 0.900f, 1f),   //# #82ABE6 청회 상향
            _                => Color.white,
        };

        //# 종족 → 빌드 축 — 스포너 칸 색띠 등 HUD 가 읽는 단일 SoT(scene-2d-conversion §4.5).
        public static EBuildAxis SpeciesAxis(EMonster species) => MonsterGrades.BaseOf(species) switch
        {
            EMonster.Wisp    => EBuildAxis.Tank,
            EMonster.Wraith  => EBuildAxis.Tank,
            EMonster.Reaper  => EBuildAxis.Dps,
            EMonster.Hex     => EBuildAxis.Dps,
            EMonster.Plague  => EBuildAxis.Debuff,
            EMonster.Phantom => EBuildAxis.Swarm,
            _                => EBuildAxis.Tank,
        };

        //# 종족 한글 표시명 단일 SoT — 인게임 모든 표기(스포너 상태·도감 등). 상점 §7 DisplayName 철자와 일치.
        //# 주의: EMonster 값 이름(에셋 키·SeenMonsters 저장)은 영어 그대로 — 여기는 사람이 읽는 표기만.
        public static string SpeciesName(EMonster species) => MonsterGrades.BaseOf(species) switch
        {
            EMonster.Wisp    => "도깨비불",
            EMonster.Wraith  => "망령",
            EMonster.Reaper  => "사신",
            EMonster.Hex     => "저주술사",
            EMonster.Plague  => "역병귀",
            EMonster.Phantom => "환령",
            _                => "?",
        };

        //# 개별 몬스터 표시 이름 = 등급 접두어 + 기존 이름 (dungeon-formation §0.2 U-E A) — 상점 행 머리·편성 칸·도감·스포너 상태만.
        //# 카드·시너지·유물·규칙 문구는 SpeciesName(접두어 없음). 등급 데이터·문구가 없으면 기존 이름 그대로.
        public static string MonsterName(EMonster monster, FormationDatabase db, Func<int, string> text)
        {
            string name = SpeciesName(monster);
            string prefix = GradePrefix(MonsterGrades.GradeOf(monster), db, text);
            return string.IsNullOrEmpty(prefix) ? name : prefix + " " + name;
        }

        //# 좁은 칸 2줄 표시(§0.2) — 위 접두어(한 단계 작게) / 아래 기존 이름. 접두어가 없으면 한 줄.
        public static string MonsterNameTwoLine(EMonster monster, FormationDatabase db, Func<int, string> text)
        {
            string name = SpeciesName(monster);
            string prefix = GradePrefix(MonsterGrades.GradeOf(monster), db, text);
            return string.IsNullOrEmpty(prefix) ? name : "<size=80%>" + prefix + "</size>\n" + name;
        }

        public static string GradePrefix(int grade, FormationDatabase db, Func<int, string> text)
        {
            GradeRow row = db != null ? db.GradeRowOf(grade) : null;
            return row != null && text != null ? text(row.prefixKey) : null;
        }
    }
}
