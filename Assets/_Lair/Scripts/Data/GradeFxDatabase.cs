using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 한 몬스터·한 등급의 연출 사양 — GradeFxDatabase.SpecOf 가 만든다. 값은 전부 JSON 에서 온다.
    public sealed class GradeFxSpec
    {
        public float SizeMul = 1f;
        public Color GlowColor = Color.white;
        public bool HasGlow;
        public float GlowWidthCell;
        public bool HasAura;
        public float AuraOpacity;
        public float AuraScale;
        public float AuraOffsetY;
        public Grade_PulseRow Pulse;
        public List<Grade_FxRow> Fx = new List<Grade_FxRow>();
    }

    //# 등급 연출 JSON 5종 검증 + 조회(monster-grades §8). 위반은 ChapterDataException 으로 모아 던진다. Current==null 이면 연출 없음(크기 1).
    public sealed class GradeFxDatabase
    {
        public const string KindTornado = "Tornado";
        public const string KindSunburst = "Sunburst";

        public static GradeFxDatabase Current { get; set; }

        private readonly Dictionary<int, float> _sizeMul = new Dictionary<int, float>();
        private readonly Dictionary<int, Grade_GlowRow> _glow = new Dictionary<int, Grade_GlowRow>();
        private readonly Dictionary<int, Grade_AuraRow> _aura = new Dictionary<int, Grade_AuraRow>();
        private readonly Dictionary<int, Grade_PulseRow> _pulse = new Dictionary<int, Grade_PulseRow>();
        private readonly Dictionary<int, List<Grade_FxRow>> _fx = new Dictionary<int, List<Grade_FxRow>>();
        private readonly Dictionary<EMonster, Color> _color = new Dictionary<EMonster, Color>();

        private GradeFxDatabase() { }

        public static GradeFxDatabase Build(GradeRow[] grades, Grade_GlowRow[] glows, Grade_AuraRow[] auras, Grade_PulseRow[] pulses,
            Grade_FxRow[] fxs, Monster_GradeFxColorRow[] colors)
        {
            List<string> errors = new List<string>();
            GradeFxDatabase db = new GradeFxDatabase();
            db.LoadGrades(grades, errors);
            db.LoadGlow(glows, errors);
            db.LoadAura(auras, errors);
            db.LoadPulse(pulses, errors);
            db.LoadFx(fxs, errors);
            db.LoadColors(colors, errors);
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return db;
        }

        public float SizeMulOf(int grade) => _sizeMul.TryGetValue(grade, out float mul) ? mul : 1f;

        //# 기준 몬스터 + 등급의 연출 사양. 색이 없는 몬스터면 흰색.
        public GradeFxSpec SpecOf(EMonster baseMonster, int grade)
        {
            GradeFxSpec spec = new GradeFxSpec { SizeMul = SizeMulOf(grade) };
            if (_color.TryGetValue(baseMonster, out Color color))
            {
                spec.GlowColor = color;
            }
            if (_glow.TryGetValue(grade, out Grade_GlowRow glow))
            {
                spec.HasGlow = true;
                spec.GlowWidthCell = glow.widthCell;
            }
            if (_aura.TryGetValue(grade, out Grade_AuraRow aura))
            {
                spec.HasAura = true;
                spec.AuraOpacity = aura.opacity;
                spec.AuraScale = aura.scale;
                spec.AuraOffsetY = aura.offsetY;
            }
            if (_pulse.TryGetValue(grade, out Grade_PulseRow pulse))
            {
                spec.Pulse = pulse;
            }
            if (_fx.TryGetValue(grade, out List<Grade_FxRow> list))
            {
                spec.Fx.AddRange(list);
            }
            return spec;
        }

        //# Grade.json 의 sizeMul(필수 1:1) — 0 이하면 검증 오류.
        private void LoadGrades(GradeRow[] rows, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Grade.json 이 비어 있음");
                return;
            }
            foreach (GradeRow row in rows)
            {
                if (row == null)
                    continue;
                if (row.sizeMul > 0f == false)
                {
                    errors.Add($"Grade.json grade {row.grade} sizeMul 은 0 보다 커야 함");
                    continue;
                }
                _sizeMul[row.grade] = row.sizeMul;
            }
        }

        private void LoadGlow(Grade_GlowRow[] rows, List<string> errors)
        {
            foreach (Grade_GlowRow row in rows ?? Array.Empty<Grade_GlowRow>())
            {
                if (row == null || row.grade < 2 || row.grade > 5 || row.widthCell > 0f == false || _glow.ContainsKey(row.grade))
                {
                    errors.Add($"Grade_Glow.json grade {row?.grade} 가 잘못됨(2~5, 폭>0, 중복 금지)");
                    continue;
                }
                _glow[row.grade] = row;
            }
        }

        private void LoadAura(Grade_AuraRow[] rows, List<string> errors)
        {
            foreach (Grade_AuraRow row in rows ?? Array.Empty<Grade_AuraRow>())
            {
                if (row == null || row.grade < 2 || row.grade > 5 || row.opacity > 0f == false || row.scale > 0f == false || _aura.ContainsKey(row.grade))
                {
                    errors.Add($"Grade_Aura.json grade {row?.grade} 가 잘못됨(2~5, 불투명도·스케일>0, 중복 금지)");
                    continue;
                }
                _aura[row.grade] = row;
            }
        }

        private void LoadPulse(Grade_PulseRow[] rows, List<string> errors)
        {
            foreach (Grade_PulseRow row in rows ?? Array.Empty<Grade_PulseRow>())
            {
                if (row == null || row.grade < 2 || row.grade > 5 || row.periodSec > 0f == false || row.brightnessMax < row.brightnessMin
                    || row.saturationMax < row.saturationMin || _pulse.ContainsKey(row.grade))
                {
                    errors.Add($"Grade_Pulse.json grade {row?.grade} 가 잘못됨(주기>0, 최소<=최대, 중복 금지)");
                    continue;
                }
                _pulse[row.grade] = row;
            }
        }

        private void LoadFx(Grade_FxRow[] rows, List<string> errors)
        {
            HashSet<(int, int)> keys = new HashSet<(int, int)>();
            foreach (Grade_FxRow row in rows ?? Array.Empty<Grade_FxRow>())
            {
                bool kindOk = row != null && (row.kind == KindTornado || row.kind == KindSunburst);
                if (row == null || row.grade < 2 || row.grade > 5 || row.index < 0 || kindOk == false || row.scale > 0f == false
                    || keys.Add((row.grade, row.index)) == false)
                {
                    errors.Add($"Grade_Fx.json (grade {row?.grade}, index {row?.index}) 가 잘못됨(kind Tornado|Sunburst, 스케일>0, 복합키 중복 금지)");
                    continue;
                }
                if (_fx.TryGetValue(row.grade, out List<Grade_FxRow> list) == false)
                {
                    list = new List<Grade_FxRow>();
                    _fx[row.grade] = list;
                }
                list.Add(row);
            }
            foreach (List<Grade_FxRow> list in _fx.Values)
            {
                list.Sort((a, b) => a.index.CompareTo(b.index));
            }
        }

        private void LoadColors(Monster_GradeFxColorRow[] rows, List<string> errors)
        {
            foreach (Monster_GradeFxColorRow row in rows ?? Array.Empty<Monster_GradeFxColorRow>())
            {
                if (row == null || Enum.TryParse(row.monster, out EMonster monster) == false || ColorUtility.TryParseHtmlString(row.glowColor, out Color color) == false
                    || _color.ContainsKey(monster))
                {
                    errors.Add($"Monster_GradeFxColor.json '{row?.monster}' 가 잘못됨(EMonster 값명·#RRGGBB·중복 금지)");
                    continue;
                }
                _color[monster] = color;
            }
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                if (_color.ContainsKey(monster) == false)
                {
                    errors.Add($"Monster_GradeFxColor.json 에 {monster} 행이 없음");
                }
            }
        }
    }
}
