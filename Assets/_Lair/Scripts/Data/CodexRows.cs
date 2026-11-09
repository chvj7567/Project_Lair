using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# Grade_CodexUnlock.json — Grade 의 필수 1:1 관계: 등급 하나의 도감 해금 연출 강도 (monster-codex §11.1, 5등급 모두 행 존재).
    [Serializable]
    public class Grade_CodexUnlockRow
    {
        public int grade;
        public string colorHex;
        public int pulseCount;
        public int spreadPx;
        public int ringCount;
        public int sparkCount;
        public int glowPx;
    }

    //# CodexConfig.json (단일 객체) — 핵심 엔티티: 도감 화면 튜닝 값 (monster-codex §11.1). 초기값 = 기획서 값(JSON 누락 필드 방어).
    [Serializable]
    public class CodexConfigRow
    {
        public int fxLiveCap = 8;
        public int cellBodyBasePx = 40;
        public int cellPulseMax = 2;
        public int cellRingMax = 1;
        public int cellSparkMax = 8;
        public int cellSparkRadiusPx = 34;
        public float cellFxFadeDelaySec = 1.4f;
        public float cellFxFadeSec = 0.6f;
        public float newCellStaggerSec = 0.15f;
        public float unlockFadeSec = 1.4f;
        public float pulseSec = 0.7f;
        public float ringSec = 0.9f;
        public float sparkSec = 0.9f;
    }

    //# 도감 연출 JSON 2종 검증 + 조회. 위반은 ChapterDataException 으로 모아 던진다. Current==null 이면 연출 없음(해금 칸 이펙트·해금 연출 생략, 도감 자체는 동작).
    public sealed class CodexDatabase
    {
        public static CodexDatabase Current { get; set; }

        private readonly Dictionary<int, Grade_CodexUnlockRow> _unlock = new Dictionary<int, Grade_CodexUnlockRow>();
        private readonly Dictionary<int, Color> _color = new Dictionary<int, Color>();

        private CodexDatabase(CodexConfigRow config)
        {
            Config = config;
        }

        public CodexConfigRow Config { get; }

        public static CodexDatabase Build(CodexConfigRow config, Grade_CodexUnlockRow[] rows)
        {
            if (config == null)
                throw new ChapterDataException("CodexConfig.json 이 비어 있음");
            if (rows == null)
                throw new ChapterDataException("Grade_CodexUnlock.json 이 비어 있음");

            List<string> errors = new List<string>();
            CodexDatabase db = new CodexDatabase(config);
            if (config.fxLiveCap < 0 || config.cellBodyBasePx <= 0 || config.cellPulseMax < 0 || config.cellRingMax < 0 || config.cellSparkMax < 0)
            {
                errors.Add("CodexConfig 의 상한·기준 크기 값이 잘못됨(fxLiveCap/cell*Max >= 0, cellBodyBasePx > 0)");
            }
            if (config.cellFxFadeDelaySec < 0f || config.cellFxFadeSec < 0f || config.newCellStaggerSec < 0f || config.unlockFadeSec < 0f
                || config.pulseSec < 0f || config.ringSec < 0f || config.sparkSec < 0f)
            {
                errors.Add("CodexConfig 의 시간 값은 0 이상이어야 함");
            }
            foreach (Grade_CodexUnlockRow row in rows)
            {
                if (row == null || row.grade < MonsterGrades.MinGrade || row.grade > MonsterGrades.MaxGrade)
                {
                    errors.Add($"Grade_CodexUnlock.json grade {row?.grade} 는 {MonsterGrades.MinGrade}~{MonsterGrades.MaxGrade} 이어야 함");
                    continue;
                }
                if (db._unlock.ContainsKey(row.grade))
                {
                    errors.Add($"Grade_CodexUnlock.json grade {row.grade} 중복");
                    continue;
                }
                if (ColorUtility.TryParseHtmlString(row.colorHex, out Color color) == false)
                {
                    errors.Add($"Grade_CodexUnlock.json grade {row.grade} colorHex '{row.colorHex}' 가 #RRGGBB 가 아님");
                    continue;
                }
                if (row.pulseCount < 0 || row.spreadPx < 0 || row.ringCount < 0 || row.sparkCount < 0 || row.glowPx < 0)
                {
                    errors.Add($"Grade_CodexUnlock.json grade {row.grade} 수치는 0 이상이어야 함");
                    continue;
                }
                db._unlock[row.grade] = row;
                db._color[row.grade] = color;
            }
            for (int g = MonsterGrades.MinGrade; g <= MonsterGrades.MaxGrade; ++g)
            {
                if (db._unlock.ContainsKey(g) == false)
                {
                    errors.Add($"Grade_CodexUnlock.json 에 grade {g} 행이 없음");
                }
            }
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return db;
        }

        //# 행이 없는 등급(범위 밖)은 null.
        public Grade_CodexUnlockRow UnlockOf(int grade) => _unlock.TryGetValue(grade, out Grade_CodexUnlockRow row) ? row : null;

        //# 등급색 — 행이 없으면 흰색.
        public Color ColorOf(int grade) => _color.TryGetValue(grade, out Color color) ? color : Color.white;
    }
}
