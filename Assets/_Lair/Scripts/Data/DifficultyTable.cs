using System;
using System.Collections.Generic;
using ChvjUnityInfra;

namespace Lair.Data
{
    //# Difficulty.json — 핵심 엔티티: 난이도 단계 하나(스테이지와 무관한 공통 배율). 단일 PK difficulty.
    [Serializable]
    public class DifficultyRow
    {
        public int difficulty;
        public int nameKey;
        public float hpMultiplier;
        public float powerMultiplier;
    }

    //# 난이도 배율표 검증 + 조회 (difficulty-select §2.2). Unity 비의존 순수 클래스. 위반 시 ChapterDataException 으로 전부 모아 던진다.
    public class DifficultyTable
    {
        //# UI(세그먼트 3칸) 고정 전제 — 4단계 이상 확장 시 UI 개편과 함께 이 상수만 푼다.
        public const int RequiredCount = 3;

        private static readonly string[] FallbackNames = { "Easy", "Normal", "Hard" };

        //# 로드 성공 시에만 채워진다. null 이면 "Easy 만 존재" 상태(난이도 기능 비활성, 전투는 정상).
        public static DifficultyTable Current { get; set; }

        //# 최대 난이도 인덱스 — 표 미로드면 0(Easy 만).
        public static int CurrentMaxIndex => Current != null ? Current.MaxIndex : 0;

        private readonly DifficultyRow[] _rows;

        private DifficultyTable(DifficultyRow[] rows)
        {
            _rows = rows;
        }

        public int MaxIndex => _rows.Length - 1;

        //# 범위를 벗어나면 마지막/첫 행으로 클램프.
        public DifficultyRow Get(int difficulty)
        {
            return _rows[Math.Max(0, Math.Min(MaxIndex, difficulty))];
        }

        //# 현재 표 기준 행 — 표 미로드이거나 Easy(0)면 null(= 배율 1.0, 기존 경로).
        public static DifficultyRow CurrentRow(int difficulty)
        {
            return Current != null && difficulty > 0 ? Current.Get(difficulty) : null;
        }

        //# 난이도 라벨 — Strings_Ko nameKey 로 조회, 표/문구 미로드면 영문 기본명.
        public static string LabelOf(int difficulty, Func<int, string> text = null)
        {
            if (Current == null)
                return FallbackNames[Math.Max(0, Math.Min(FallbackNames.Length - 1, difficulty))];
            return Current.Label(difficulty, text);
        }

        //# 이 표 기준 라벨 — 문구가 비면 영문 기본명.
        public string Label(int difficulty, Func<int, string> text = null)
        {
            Func<int, string> lookup = text ?? DefaultText;
            string label = lookup(Get(difficulty).nameKey);
            return string.IsNullOrEmpty(label) ? FallbackNames[Math.Max(0, Math.Min(FallbackNames.Length - 1, difficulty))] : label;
        }

        private static string DefaultText(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        //# nameKeyExists: Strings_Ko id 존재 판정(null 이면 nameKey 검증 생략).
        public static DifficultyTable Build(DifficultyRow[] rows, Func<int, bool> nameKeyExists = null)
        {
            List<string> errors = new List<string>();
            DifficultyRow[] sorted = new DifficultyRow[RequiredCount];
            if (rows == null || rows.Length != RequiredCount)
            {
                errors.Add($"Difficulty.json 행이 정확히 {RequiredCount}개여야 함(현재 {(rows == null ? 0 : rows.Length)})");
            }
            else
            {
                foreach (DifficultyRow row in rows)
                {
                    if (row == null || row.difficulty < 0 || row.difficulty >= RequiredCount)
                    {
                        errors.Add("Difficulty.json difficulty 는 0~2 여야 함");
                        continue;
                    }
                    if (sorted[row.difficulty] != null)
                    {
                        errors.Add($"Difficulty.json difficulty {row.difficulty} 중복");
                        continue;
                    }
                    sorted[row.difficulty] = row;
                }
            }

            if (errors.Count == 0)
            {
                Validate(sorted, nameKeyExists, errors);
            }
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return new DifficultyTable(sorted);
        }

        private static void Validate(DifficultyRow[] sorted, Func<int, bool> nameKeyExists, List<string> errors)
        {
            if (sorted[0].hpMultiplier != 1.0f || sorted[0].powerMultiplier != 1.0f)
            {
                errors.Add("Difficulty.json Easy(0) 는 배율 1.0/1.0 이어야 함");
            }
            for (int i = 0; i < sorted.Length; ++i)
            {
                if (sorted[i].hpMultiplier <= 0f || sorted[i].powerMultiplier <= 0f)
                {
                    errors.Add($"Difficulty.json difficulty {i} 배율은 0 보다 커야 함");
                }
                if (i > 0 && (sorted[i].hpMultiplier < sorted[i - 1].hpMultiplier || sorted[i].powerMultiplier < sorted[i - 1].powerMultiplier))
                {
                    errors.Add($"Difficulty.json difficulty {i} 배율이 이전 단계보다 작음(단조 비감소 위반)");
                }
                if (nameKeyExists != null && nameKeyExists(sorted[i].nameKey) == false)
                {
                    errors.Add($"Difficulty.json difficulty {i} nameKey {sorted[i].nameKey} 가 Strings_Ko 에 없음");
                }
            }
        }
    }
}
