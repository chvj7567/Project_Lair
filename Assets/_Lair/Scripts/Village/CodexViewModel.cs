using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.Village
{
    //# 매트릭스 칸 하나 — (종족, 등급, 티어). 미해금도 실제 이름을 들고 있다(monster-codex D10).
    public sealed class CodexCellModel
    {
        public EMonster Race;       //# 기준 몬스터(종족 식별)
        public EMonster Monster;    //# 이 등급의 몬스터
        public int Index;
        public int Grade;
        public int Tier;
        public bool Open;
        public bool IsNew;          //# 세션 신규 칸 — 팝업이 닫힐 때까지 NEW 배지
        public bool PlayUnlock;     //# 이번 종족 표시에서 칸 해금 연출을 재생할 칸(View 는 VM.ConsumeUnlock 으로 소비)
        public bool PlayDetailUnlock;   //# 이 칸이 선택됐을 때 상세 미리보기 해금 연출을 재생할 칸(VM.ConsumeDetailUnlock)
        public string Name;
        public float SizeMul = 1f;
        public float MaxSizeMul = 1f;   //# 전 등급 중 최대 크기 배율 - 슬롯 안 몸 크기를 5등급 기준으로 정규화할 때 쓴다
        public float HpMul = 1f;
        public float AtkMul = 1f;
    }

    public sealed class CodexConditionLine
    {
        public string Text;
        public bool Met;
    }

    //# 상세 패널 표시 데이터 (monster-codex §4).
    public sealed class CodexDetailModel
    {
        public CodexCellModel Cell;
        public string Name;
        public string GradeName;
        public string TierLabel;
        public string RaceLabel;
        public int StarCount;
        public string RaceProgress;
        public bool Locked;
        public string StatsText;                 //# 해금일 때만
        public string ConditionTitle;            //# 잠금일 때만
        public List<CodexConditionLine> Conditions = new List<CodexConditionLine>();
        public string HiddenStatsText;           //# 잠금일 때만
        public string AppearanceText;
        public List<CodexCellModel> SameGradeTiers = new List<CodexCellModel>();   //# 티어 선택 미니 칸
    }

    public sealed class CodexRaceChipModel
    {
        public EMonster Race;
        public string Name;
        public int OpenCount;
        public int TotalCount;
        public bool HasUnseenNew;
    }

    //# 도감 몬스터 탭 ViewModel (monster-codex §3·§6) — Unity View 비의존. 종족 6 x (등급 x 티어) 칸을 데이터에서 도출한다.
    public sealed class CodexViewModel
    {
        private const string LogTag = "[Codex]";

        private sealed class RaceState
        {
            public bool Seen;
            public int OwnedGrade = MonsterGrades.MinGrade;
            public int Level;
            public ERace? Race;
            public List<CodexCellModel> Cells = new List<CodexCellModel>();
        }

        public const int StrConditionTitle = 1525;
        public const int StrMeet = 1526;
        public const int StrNeedGrade = 1527;
        public const int StrNeedTier = 1528;
        public const int StrHiddenStats = 1529;
        public const int StrCollected = 1530;
        public const int StrRaceLabel = 1531;
        public const int StrRaceProgress = 1532;
        public const int StrStats = 1533;
        public const int StrAppearance = 1534;
        public const int StrBaseSheet = 1535;
        public const int StrTier = 1536;
        public const int StrNew = 1539;
        public const int StrBattleFx = 1540;
        private const int StrTierBaseSource = 1876;
        private const int StrTierShopSource = 1877;

        private readonly FormationDatabase _formation;
        private readonly Func<int, string> _text;
        private readonly Dictionary<EMonster, RaceState> _states = new Dictionary<EMonster, RaceState>();
        private bool _loggedMissingRace;

        public CodexViewModel(MetaProfile profile, FormationDatabase formation, Func<int, string> text)
        {
            _formation = formation;
            _text = text;
            foreach (EMonster race in MonsterGrades.BaseMonsters)
            {
                _states[race] = BuildState(profile, race);
            }
            SelectRace(InitialRace());
        }

        public IReadOnlyList<EMonster> Races => MonsterGrades.BaseMonsters;
        public int GradeCount => CodexRules.GradeCount;
        public int TierCount => CodexRules.TierCount;
        public int CellsPerRace => GradeCount * TierCount;
        public EMonster SelectedRace { get; private set; }
        public int SelectedIndex { get; private set; }

        //# 선택 종족의 칸 — 인덱스 = (등급-1) x 티어 수 + 티어. 풀링 스크롤뷰에 그대로 넣는다.
        public List<CodexCellModel> Cells => _states[SelectedRace].Cells;

        public CodexCellModel SelectedCell => SelectedIndex >= 0 && SelectedIndex < Cells.Count ? Cells[SelectedIndex] : null;

        public int TotalOpen
        {
            get
            {
                int total = 0;
                foreach (EMonster race in MonsterGrades.BaseMonsters)
                {
                    total += OpenCountOf(race);
                }
                return total;
            }
        }

        public int TotalCount => MonsterGrades.BaseMonsters.Count * CellsPerRace;

        public string CollectedText => Fmt(StrCollected, TotalOpen, TotalCount);

        public string NewLabel => Text(StrNew);

        //# 그 종족의 현재 강화 티어(0~) — 도감이 로드할 티어 시트 범위(1..OwnedTier).
        public int OwnedTier(EMonster race) => _states.TryGetValue(race, out RaceState state) ? CodexRules.TierOf(state.Level) : 0;

        public int OpenCountOf(EMonster race)
        {
            int count = 0;
            foreach (CodexCellModel cell in _states[race].Cells)
            {
                if (cell.Open)
                {
                    count++;
                }
            }
            return count;
        }

        //# 스냅샷에 없는 열린 칸이 있는 종족(아직 보지 않음) — 종족 칩 NEW 점.
        public bool HasUnseenNew(EMonster race)
        {
            HashSet<int> snapshot = CodexSession.Get(race);
            if (snapshot == null)
                return false;
            foreach (CodexCellModel cell in _states[race].Cells)
            {
                if (cell.Open && snapshot.Contains(cell.Index) == false)
                    return true;
            }
            return false;
        }

        public List<CodexRaceChipModel> BuildChips()
        {
            List<CodexRaceChipModel> chips = new List<CodexRaceChipModel>();
            foreach (EMonster race in MonsterGrades.BaseMonsters)
            {
                chips.Add(new CodexRaceChipModel
                {
                    Race = race,
                    Name = SpeciesVisual.SpeciesName(race),
                    OpenCount = OpenCountOf(race),
                    TotalCount = CellsPerRace,
                    HasUnseenNew = HasUnseenNew(race),
                });
            }
            return chips;
        }

        //# 종족 표시 — 선택 칸 초기화, 신규 칸(스냅샷 대비)을 NEW + 연출 재생 대상으로 표시하고 그 종족 스냅샷을 즉시 갱신(보지 않은 종족은 그대로 신규 유지).
        public bool SelectRace(EMonster race)
        {
            if (_states.TryGetValue(race, out RaceState state) == false)
                return false;
            SelectedRace = race;
            HashSet<int> snapshot = CodexSession.Get(race);
            HashSet<int> open = new HashSet<int>();
            foreach (CodexCellModel cell in state.Cells)
            {
                cell.PlayUnlock = false;
                cell.PlayDetailUnlock = false;
                if (cell.Open == false)
                    continue;
                open.Add(cell.Index);
                if (snapshot != null && snapshot.Contains(cell.Index) == false)
                {
                    cell.IsNew = true;
                    cell.PlayUnlock = true;
                    cell.PlayDetailUnlock = true;
                }
            }
            CodexSession.Set(race, open);
            SelectedIndex = BestOpenIndex(state.Cells);
            return true;
        }

        //# 칸 해금 연출 재생권 소비 — 풀 재바인딩에서 다시 재생되지 않게 한 번만 true.
        public bool ConsumeUnlock(CodexCellModel cell)
        {
            if (cell == null || cell.PlayUnlock == false)
                return false;
            cell.PlayUnlock = false;
            return true;
        }

        //# 상세 미리보기 해금 연출 재생권 소비(선택 칸이 신규일 때만).
        public bool ConsumeDetailUnlock(CodexCellModel cell)
        {
            if (cell == null || cell.PlayDetailUnlock == false)
                return false;
            cell.PlayDetailUnlock = false;
            return true;
        }

        public bool SelectCell(int index)
        {
            if (index < 0 || index >= Cells.Count)
                return false;
            SelectedIndex = index;
            return true;
        }

        public CodexDetailModel BuildDetail()
        {
            CodexCellModel cell = SelectedCell;
            if (cell == null)
                return null;
            RaceState state = _states[cell.Race];
            CodexDetailModel detail = new CodexDetailModel
            {
                Cell = cell,
                Name = cell.Name,
                GradeName = GradeName(cell.Grade),
                TierLabel = cell.Tier == 0 ? Text(StrBaseSheet) : Fmt(StrTier, cell.Tier),
                RaceLabel = RaceLabel(state),
                StarCount = cell.Grade,
                RaceProgress = Fmt(StrRaceProgress, OpenCountOf(cell.Race), CellsPerRace),
                Locked = cell.Open == false,
                AppearanceText = Fmt(StrAppearance, TierName(cell.Tier))
                    + (cell.Grade > MonsterGrades.MinGrade ? "\n" + Text(StrBattleFx) : string.Empty),
            };
            if (detail.Locked)
            {
                detail.ConditionTitle = Text(StrConditionTitle);
                detail.HiddenStatsText = Text(StrHiddenStats);
                AddConditions(detail, cell, state);
            }
            else
            {
                detail.StatsText = Fmt(StrStats, cell.HpMul.ToString("0.00"), cell.AtkMul.ToString("0.00"));
            }
            for (int tier = 0; tier < TierCount; ++tier)
            {
                int index = CodexRules.CellIndex(cell.Grade, tier);
                if (index >= 0 && index < state.Cells.Count)
                {
                    detail.SameGradeTiers.Add(state.Cells[index]);
                }
            }
            return detail;
        }

        //# 티어 열 머리글은 상점의 직접 티어 구매를 안내한다.
        public string TierHeader(int tier) => TierName(tier);

        public string TierSource(int tier) => Text(tier == 0 ? StrTierBaseSource : StrTierShopSource);

        public string GradeName(int grade) => SpeciesVisual.GradePrefix(grade, _formation, _text) ?? string.Empty;

        private void AddConditions(CodexDetailModel detail, CodexCellModel cell, RaceState state)
        {
            detail.Conditions.Add(new CodexConditionLine { Text = Text(StrMeet), Met = state.Seen });
            if (cell.Grade > MonsterGrades.MinGrade)
            {
                detail.Conditions.Add(new CodexConditionLine
                {
                    Text = Fmt(StrNeedGrade, GradeName(cell.Grade), GradeName(state.OwnedGrade)),
                    Met = state.OwnedGrade >= cell.Grade,
                });
            }
            if (cell.Tier > 0)
            {
                detail.Conditions.Add(new CodexConditionLine
                {
                    Text = Fmt(StrNeedTier, TierName(cell.Tier), TierName(CodexRules.TierOf(state.Level))),
                    Met = CodexRules.TierOf(state.Level) >= cell.Tier,
                });
            }
        }

        private RaceState BuildState(MetaProfile profile, EMonster baseMonster)
        {
            RaceState state = new RaceState();
            if (profile != null)
            {
                state.Seen = profile.SeenMonsters != null && profile.SeenMonsters.Contains(baseMonster.ToString());
                state.Level = profile.GetShopLevel(MonsterEnhanceTierRules.ItemId(baseMonster));
            }
            if (_formation != null && _formation.TryGetRace(baseMonster, out ERace race))
            {
                state.Race = race;
                if (profile != null)
                {
                    state.OwnedGrade = profile.GetRaceGrade(race);
                }
            }
            else
            {
                //# 데이터 결함 — 칩을 숨기지 않고 잠금 칸 + 로그 1회(monster-codex §10).
                state.Seen = false;
                if (_loggedMissingRace == false && _formation != null)
                {
                    _loggedMissingRace = true;
                    Debug.LogError($"{LogTag} {baseMonster} 종족이 FormationDatabase 에 없음 — 잠금 칸으로 표시");
                }
            }
            float maxSizeMul = 1f;
            for (int g = MonsterGrades.MinGrade; g <= MonsterGrades.MaxGrade; ++g)
            {
                GradeRow gradeRow = _formation != null ? _formation.GradeRowOf(g) : null;
                if (gradeRow != null && gradeRow.sizeMul > maxSizeMul)
                {
                    maxSizeMul = gradeRow.sizeMul;
                }
            }
            for (int grade = MonsterGrades.MinGrade; grade <= MonsterGrades.MaxGrade; ++grade)
            {
                EMonster monster = MonsterOf(state.Race, baseMonster, grade);
                GradeRow row = _formation != null ? _formation.GradeRowOf(grade) : null;
                for (int tier = 0; tier < TierCount; ++tier)
                {
                    state.Cells.Add(new CodexCellModel
                    {
                        Race = baseMonster,
                        Monster = monster,
                        Index = CodexRules.CellIndex(grade, tier),
                        Grade = grade,
                        Tier = tier,
                        Open = CodexRules.IsOpen(state.Seen, state.OwnedGrade, state.Level, grade, tier),
                        Name = SpeciesVisual.MonsterName(monster, _formation, _text),
                        SizeMul = row != null ? row.sizeMul : 1f,
                        MaxSizeMul = maxSizeMul,
                        HpMul = row != null ? row.hpMul : 1f,
                        AtkMul = row != null ? row.atkMul : 1f,
                    });
                }
            }
            return state;
        }

        private EMonster MonsterOf(ERace? race, EMonster baseMonster, int grade)
        {
            if (race.HasValue)
            {
                foreach (EMonster monster in _formation.MonstersOf(race.Value))
                {
                    if (MonsterGrades.GradeOf(monster) == grade)
                        return monster;
                }
            }
            return MonsterGrades.Of(baseMonster, grade);
        }

        //# 오픈 시 초기 종족 = 신규 칸이 있는 첫 종족, 없으면 첫 종족.
        private EMonster InitialRace()
        {
            foreach (EMonster race in MonsterGrades.BaseMonsters)
            {
                if (HasUnseenNew(race))
                    return race;
            }
            return MonsterGrades.BaseMonsters[0];
        }

        //# 최고 해금 칸(등급 높은 것, 같으면 티어 높은 것). 열린 칸이 없으면 0.
        private static int BestOpenIndex(List<CodexCellModel> cells)
        {
            int best = 0;
            for (int i = 0; i < cells.Count; ++i)
            {
                if (cells[i].Open && i > best)
                {
                    best = i;
                }
            }
            return best;
        }

        private string RaceLabel(RaceState state)
        {
            RaceRow row = state.Race.HasValue && _formation != null ? _formation.RaceRowOf(state.Race.Value) : null;
            return row != null ? Fmt(StrRaceLabel, Text(row.nameKey)) : string.Empty;
        }

        private string TierName(int tier) => tier == 0 ? Text(StrBaseSheet) : "T" + tier;

        private string Text(int id)
        {
            string value = _text != null ? _text(id) : null;
            return value ?? string.Empty;
        }

        private string Fmt(int id, params object[] args)
        {
            string format = Text(id);
            if (format.Length == 0)
                return string.Empty;
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }
    }
}
