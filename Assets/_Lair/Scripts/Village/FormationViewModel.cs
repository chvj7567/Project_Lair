using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.Village
{
    public class FormationNestData
    {
        public int Index;
        public string TitleText;
        public EMonster Monster;
        public int Grade;
        public string NameText;
        public string RaceText;
        public string GradeText;
        public string TierText;
        public Color SpeciesColor;
        public bool IsEmpty;
        public bool IsSelected;
        public bool IsDefault;
        public bool IsSealed;
    }

    public class FormationSpeciesData
    {
        public EMonster Monster;
        public int Grade;
        public string NameText;
        public string GradeText;
        public Color SpeciesColor;
        public string TierText;
        public string PlacementText;
        public EBuildAxis Axis;
        public bool IsSelected;
    }

    //# 편성 초안만 편집한다. 호출자가 출격을 확정하면 프로필을 저장한다.
    public class FormationViewModel
    {
        public const int StrNest = 1401;
        public const int StrSealForecast = 1404;
        public const int StrRowGrade = 1523;
        public const int StrEmpty = 1830;
        public const int StrFilled = 1831;
        public const int StrEmptyWarning = 1832;
        public const int StrNoSeal = 1833;
        public const int StrBaseTier = 1834;
        public const int StrTier = 1835;
        public const int StrPlaced = 1836;
        public const int StrMove = 1837;
        public const int StrSwap = 1838;
        public const int StrAvailable = 1839;
        public const int StrCleared = 1840;
        public const int StrPlacedFeedback = 1841;
        public const int StrMovedFeedback = 1842;
        public const int StrSwappedFeedback = 1843;
        public const int StrReset = 1844;
        public const int StrInitial = 1845;
        public const int StrCardsRandom = 1846;
        public const int StrRepaired = 1847;
        public const int StrAll = 1848;
        public const int StrClearButton = 1849;
        public const int StrTowerFormation = 1850;
        public const int StrDailyFormation = 1851;
        public const int StrNoEffectNest = 1852;
        public const int StrNoEffectMonster = 1853;
        public const int StrNoEffectMonsterSpecies = 1854;
        public const int StrNoEffectNestSpecies = 1855;
        public const int StrSelectedNest = 1856;

        private readonly FormationDatabase _db;
        private readonly Func<int, string> _text;
        private readonly Func<EMonster, int> _levelOf;
        private readonly Func<ERace, int> _ownedGradeOf;
        private readonly EMonster?[] _formation;
        private readonly EMonster?[] _defaults;
        private readonly StageRuleData _sealRule;
        private readonly EMonster _sealSpecies;
        private int _selected;
        private int _filter;

        public event Action OnChanged;

        public FormationViewModel(
            FormationDatabase db, ChapterDatabase chapters, MetaProfile profile, int stage,
            Func<int, string> text, Func<EMonster, int> awakenLevelOf)
        {
            _db = db;
            _text = text ?? (id => string.Empty);
            _levelOf = awakenLevelOf ?? (monster => 0);
            _ownedGradeOf = profile.GetRaceGrade;
            _defaults = FormationSave.DefaultSlots(db, _ownedGradeOf);
            if (profile.HasCustomFormation)
            {
                _formation = FormationSave.ReadCustom(db, profile.Formation, _ownedGradeOf, out bool repaired);
                FeedbackText = _text(repaired ? StrRepaired : StrInitial);
            }
            else
            {
                _formation = (EMonster?[])_defaults.Clone();
                FeedbackText = _text(StrInitial);
            }

            Stage = stage;
            StageNumberText = StageLabels.Number(stage);
            StageRow stageRow = chapters != null ? chapters.GetStageByLinear(stage) : null;
            StageNameText = stageRow != null ? _text(stageRow.nameKey) : string.Empty;
            HeroArchetypeRow heroRow = chapters != null ? chapters.GetHeroOfLinear(stage) : null;
            HeroText = heroRow != null ? Format(WorldMapViewModel.StrHeroName, _text(heroRow.nameKey)) : string.Empty;
            int chapterId = StageProgress.ChapterOf(stage);
            int hintKey = db.HintKeyOf(chapterId);
            HintText = hintKey > 0 ? _text(hintKey) : string.Empty;
            Difficulty = profile.ClampDifficulty(profile.SelectedDifficulty, stage, DifficultyTable.CurrentMaxIndex);
            DifficultyText = Difficulty >= 1 ? DifficultyTable.LabelOf(Difficulty, _text) : string.Empty;

            StringBuilder rules = new StringBuilder();
            if (chapters != null)
            {
                foreach (StageRuleData data in chapters.GetRules(chapterId, StageProgress.IndexOf(stage)))
                {
                    if (rules.Length > 0)
                        rules.Append("\n");
                    rules.Append(StageRuleText.Entry(data, _text));
                    if (data.RuleId == EStageRule.SealedNest)
                    {
                        _sealRule = data;
                        _sealSpecies = (EMonster)(int)Math.Round(data.Params["lockedSpecies"]);
                    }
                }
            }
            RulesText = rules.Length > 0 ? rules.ToString() : _text(WorldMapViewModel.StrNoRules);
        }

        public int Stage { get; }
        public string StageNumberText { get; }
        public string StageNameText { get; }
        public string HeroText { get; }
        public string HintText { get; }
        public string RulesText { get; }
        public int Difficulty { get; }
        public string DifficultyText { get; }
        public int SelectedIndex => _selected;
        public int FilterIndex => _filter;
        public bool CanClear => _formation[_selected].HasValue;
        public bool IsCustom => FormationSave.IsCustom(_formation, _defaults);
        public string FeedbackText { get; private set; }
        public string CardsRandomText => _text(StrCardsRandom);
        public string ClearText => _text(StrClearButton);
        public string SelectedNestText => Format(StrSelectedNest, _selected + 1);
        public bool HasSealForecast => _sealRule != null;

        public int AssignedCount
        {
            get
            {
                int count = 0;
                foreach (EMonster? monster in _formation)
                {
                    if (monster.HasValue)
                        ++count;
                }
                return count;
            }
        }

        public string FilledText => Format(StrFilled, AssignedCount, FormationDatabase.SlotCount);
        public string EmptyWarningText => AssignedCount == 0 ? _text(StrEmptyWarning) : string.Empty;
        public int SealedIndex => _sealRule != null ? FormationRules.PickSealedSlot(_formation, _sealSpecies, _db) : -1;

        public string SealForecastText
        {
            get
            {
                if (_sealRule == null)
                    return string.Empty;
                int index = SealedIndex;
                return index >= 0
                    ? Format(StrSealForecast, index + 1, MonsterName(_formation[index].Value), SealedNestInfo.AnyUnlockHpPercent(_sealRule))
                    : _text(StrNoSeal);
            }
        }

        public string FilterName(int index)
        {
            if (index == 0)
                return _text(StrAll);
            ERace race = _db.SlotRace(index - 1);
            return SpeciesVisual.SpeciesName(_db.MonstersOf(race)[0]);
        }

        public void SelectNest(int index)
        {
            if (index < 0 || index >= _formation.Length || index == _selected)
                return;
            _selected = index;
            FeedbackText = _text(StrInitial);
            OnChanged?.Invoke();
        }

        public void SelectFilter(int index)
        {
            if (index < 0 || index > FormationDatabase.SlotCount || index == _filter)
                return;
            _filter = index;
            OnChanged?.Invoke();
        }

        public void Place(EMonster monster)
        {
            if (_formation[_selected] == monster || FormationRules.IsAllowed(_db, _selected, monster, _ownedGradeOf) == false)
                return;
            int previous = Array.IndexOf<EMonster?>(_formation, monster);
            EMonster? displaced = _formation[_selected];
            _formation[_selected] = monster;
            if (previous >= 0)
            {
                _formation[previous] = displaced;
                FeedbackText = Format(displaced.HasValue ? StrSwappedFeedback : StrMovedFeedback, previous + 1, _selected + 1);
            }
            else
            {
                FeedbackText = Format(StrPlacedFeedback, _selected + 1, MonsterName(monster));
            }
            OnChanged?.Invoke();
        }

        public void ClearNest()
        {
            if (CanClear == false)
                return;
            _formation[_selected] = null;
            FeedbackText = Format(StrCleared, _selected + 1);
            OnChanged?.Invoke();
        }

        public void ResetToDefault()
        {
            Array.Copy(_defaults, _formation, _formation.Length);
            _selected = 0;
            _filter = 0;
            FeedbackText = _text(StrReset);
            OnChanged?.Invoke();
        }

        public List<FormationSlotEntry> BuildSaveRows()
        {
            return FormationSave.ToSaved(_formation, _defaults);
        }

        public FormationNestData GetNest(int index)
        {
            EMonster? monster = _formation[index];
            FormationNestData data = new FormationNestData
            {
                Index = index,
                TitleText = Format(StrNest, index + 1),
                NameText = _text(StrEmpty),
                SpeciesColor = new Color32(0xae, 0xb2, 0xc5, 0xff),
                IsEmpty = monster.HasValue == false,
                IsSelected = index == _selected,
                IsDefault = monster == _defaults[index],
                IsSealed = index == SealedIndex,
            };
            if (monster.HasValue == false)
                return data;

            EMonster value = monster.Value;
            data.Monster = value;
            data.Grade = MonsterGrades.GradeOf(value);
            data.NameText = MonsterName(value);
            data.TierText = TierText(value);
            data.SpeciesColor = SpeciesVisual.SpeciesGlowColor(value);
            return data;
        }

        public IReadOnlyList<FormationSpeciesData> BuildSpecies()
        {
            List<FormationSpeciesData> list = new List<FormationSpeciesData>();
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                if (_filter != 0 && _filter != i + 1)
                    continue;
                ERace race = _db.SlotRace(i);
                List<EMonster> owned = _db.OwnedMonstersOf(race, _ownedGradeOf(race));
                for (int gradeIndex = owned.Count - 1; gradeIndex >= 0; --gradeIndex)
                {
                    EMonster monster = owned[gradeIndex];
                    int slot = Array.IndexOf<EMonster?>(_formation, monster);
                    int grade = MonsterGrades.GradeOf(monster);
                    string placement = _text(StrAvailable);
                    if (slot == _selected)
                        placement = _text(StrPlaced);
                    else if (slot >= 0)
                        placement = Format(CanClear ? StrSwap : StrMove, slot + 1);

                    list.Add(new FormationSpeciesData
                    {
                        Monster = monster,
                        Grade = grade,
                        NameText = MonsterName(monster),
                        GradeText = Format(StrRowGrade, grade, _db.HpMulOf(monster).ToString("0.00", CultureInfo.InvariantCulture)),
                        SpeciesColor = SpeciesVisual.SpeciesGlowColor(monster),
                        TierText = TierText(monster),
                        PlacementText = placement,
                        Axis = SpeciesVisual.SpeciesAxis(monster),
                        IsSelected = slot == _selected,
                    });
                }
            }
            return list;
        }

        private string MonsterName(EMonster monster) => SpeciesVisual.MonsterName(monster, _db, _text);

        private string TierText(EMonster monster)
        {
            int tier = MonsterEnhancementVisual.TierOfLevel(_levelOf(MonsterGrades.BaseOf(monster)));
            return tier == 0 ? _text(StrBaseTier) : Format(StrTier, tier);
        }

        private string Format(int id, params object[] args) => string.Format(_text(id) ?? string.Empty, args);
    }
}