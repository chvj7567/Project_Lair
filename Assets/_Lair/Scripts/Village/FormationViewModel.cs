using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.Village
{
    //# 둥지 칸 한 개의 확정 표시값 — View(FormationNestTile)는 그대로 표시만 한다 (Rule 02 §6).
    public class FormationNestData
    {
        public int Index;               //# 0~5
        public string TitleText;        //# "둥지 3"
        public EMonster Monster;
        public int Grade;               //# 1~5 — 아이콘 테두리·크기 등급 표현
        public string NameText;         //# "견습 도깨비불" (접두어 포함)
        public string RaceText;         //# "이그니스"
        public string GradeText;        //# "2등급 · 보유 2등급"
        public Color SpeciesColor;
        public bool IsSelected;
        public bool IsDefault;          //# 기본 편성(보유 최고 등급)과 같은 몬스터 — 회색 원 표식
        public bool IsSealed;           //# 봉인 예정 칸 — 사슬 표식
    }

    //# 선택 둥지 종족의 보유 몬스터 행 한 개의 확정 표시값 (등급순, 최대 5).
    public class FormationSpeciesData
    {
        public EMonster Monster;
        public int Grade;
        public string NameText;         //# "견습 도깨비불"
        public string GradeText;        //# "2등급 · HP·공격력 x1.10 · 보유 최고"
        public Color SpeciesColor;
        public string AwakenText;       //# "체급 Lv 2"
        public bool ShowAwaken;         //# 상점 체급 Lv 0 이면 숨김
        public EBuildAxis Axis;         //# 축 칩 (이름은 axis.ToString())
        public bool IsSelected;         //# 지금 이 둥지에 출전 중
    }

    //# 던전 편성 팝업 ViewModel (dungeon-formation §0.3·§0.10) — 둥지 6곳(종족 고정)마다 보유한 등급의 몬스터 하나를 고른다.
    //# View 를 모르고 프로필은 읽기만 한다(저장은 출격 콜백 소유자). Unity 오브젝트 비의존(Color 값만).
    public class FormationViewModel
    {
        //# Strings_Ko id.
        public const int StrAwaken = 1400;         //# 체급 Lv {0}
        public const int StrNest = 1401;           //# 둥지 {0}
        public const int StrSealForecast = 1404;   //# 봉인 예정: 둥지 {0} ({1}) · 영웅 HP {2}% 에서 해제
        public const int StrNestGrade = 1522;      //# {0}등급 · 보유 {1}등급
        public const int StrRowGrade = 1523;       //# {0}등급 · HP·공격력 x{1}
        public const int StrOwnedTop = 1524;       //# 보유 최고

        private readonly FormationDatabase _db;
        private readonly Func<int, string> _text;
        private readonly Func<EMonster, int> _awakenLevelOf;
        private readonly Func<ERace, int> _ownedGradeOf;
        private readonly EMonster[] _formation;
        private readonly EMonster[] _defaults;
        private readonly StageRuleData _sealRule;
        private readonly EMonster _sealSpecies;
        private int _selected;

        public event Action OnChanged;

        //# stage: 선형 스테이지 번호. awakenLevelOf: 기준 몬스터 → 상점 체급 레벨(Enhance_<기준 몬스터>).
        public FormationViewModel(
            FormationDatabase db, ChapterDatabase chapters, MetaProfile profile, int stage,
            Func<int, string> text, Func<EMonster, int> awakenLevelOf)
        {
            _db = db;
            _text = text ?? (id => string.Empty);
            _awakenLevelOf = awakenLevelOf ?? (monster => 0);
            _ownedGradeOf = profile.GetRaceGrade;
            _defaults = db.DefaultFormation(_ownedGradeOf);
            EMonster[] saved = FormationSave.FromSaved(profile.Formation);
            //# 무효 슬롯(타 종족·미보유 등급)은 보유 최고 등급으로 바꿔 보여 주고, 출격 때 그대로 저장된다(§0.6 · U-C A 안내 없음).
            _formation = saved != null ? FormationRules.Repair(saved, db, _ownedGradeOf, out int _) : db.DefaultFormation(_ownedGradeOf);
            Stage = stage;

            StageNumberText = StageLabels.Number(stage);
            StageRow stageRow = chapters != null ? chapters.GetStageByLinear(stage) : null;
            StageNameText = stageRow != null ? _text(stageRow.nameKey) : string.Empty;
            HeroArchetypeRow heroRow = chapters != null ? chapters.GetHeroOfLinear(stage) : null;
            HeroText = heroRow != null ? string.Format(_text(WorldMapViewModel.StrHeroName), _text(heroRow.nameKey)) : string.Empty;
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
                    {
                        rules.Append("\n");
                    }
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
        public string DifficultyText { get; }      //# Easy 면 빈 문자열(칩 숨김)

        public int SelectedIndex => _selected;

        //# 봉인된 둥지 스테이지에서 봉인될 둥지 인덱스 — 아니면 -1. 전투와 같은 FormationRules.PickSealedSlot(종족 둥지 1곳).
        public int SealedIndex => _sealRule != null ? FormationRules.PickSealedSlot(_formation, _sealSpecies) : -1;

        public bool HasSealForecast => SealedIndex >= 0;

        public string SealForecastText
        {
            get
            {
                int index = SealedIndex;
                if (index < 0)
                    return string.Empty;
                int percent = SealedNestInfo.AnyUnlockHpPercent(_sealRule);
                return string.Format(_text(StrSealForecast), index + 1, RaceName(_db.SlotRace(index)), percent);
            }
        }

        //# 선택 둥지 종족 정보 행 (우측 패널 상단).
        public string SelectedRaceName => RaceName(_db.SlotRace(_selected));

        public string SelectedRaceDesc
        {
            get
            {
                RaceRow row = _db.RaceRowOf(_db.SlotRace(_selected));
                return row != null ? _text(row.descKey) : string.Empty;
            }
        }

        public Color SelectedRaceColor => SpeciesVisual.SpeciesGlowColor(_db.MonstersOf(_db.SlotRace(_selected))[0]);

        public void SelectNest(int index)
        {
            if (index < 0 || index >= _formation.Length || index == _selected)
                return;
            _selected = index;
            OnChanged?.Invoke();
        }

        //# 선택 둥지에 monster 지정 — 그 종족 보유 몬스터만. 같은 몬스터·무효면 무동작. 성공 시 다음 둥지로 선택 이동(6 다음은 1).
        public void Place(EMonster monster)
        {
            if (_formation[_selected] == monster || FormationRules.IsAllowed(_db, _selected, monster, _ownedGradeOf) == false)
                return;
            _formation[_selected] = monster;
            _selected = (_selected + 1) % _formation.Length;
            OnChanged?.Invoke();
        }

        //# 표시 편성을 기본 편성(보유 최고 등급)으로 되돌림(저장은 출격 때) — 선택은 둥지 1.
        public void ResetToDefault()
        {
            Array.Copy(_defaults, _formation, _formation.Length);
            _selected = 0;
            OnChanged?.Invoke();
        }

        //# 저장 행 — 기본 편성과 같으면 0행, 다르면 6행.
        public List<FormationSlotEntry> BuildSaveRows()
        {
            return FormationSave.ToSaved(_formation, _defaults);
        }

        public FormationNestData GetNest(int index)
        {
            EMonster monster = _formation[index];
            ERace race = _db.SlotRace(index);
            int grade = MonsterGrades.GradeOf(monster);
            return new FormationNestData
            {
                Index = index,
                TitleText = string.Format(_text(StrNest), index + 1),
                Monster = monster,
                Grade = grade,
                NameText = SpeciesVisual.MonsterName(monster, _db, _text),
                RaceText = RaceName(race),
                GradeText = string.Format(_text(StrNestGrade), grade, _ownedGradeOf(race)),
                SpeciesColor = SpeciesVisual.SpeciesGlowColor(monster),
                IsSelected = index == _selected,
                IsDefault = monster == _defaults[index],
                IsSealed = index == SealedIndex,
            };
        }

        //# 선택 둥지 종족의 보유 몬스터 행(등급순).
        public IReadOnlyList<FormationSpeciesData> BuildSpecies()
        {
            List<FormationSpeciesData> list = new List<FormationSpeciesData>();
            ERace race = _db.SlotRace(_selected);
            foreach (EMonster monster in _db.OwnedMonstersOf(race, _ownedGradeOf(race)))
            {
                int grade = MonsterGrades.GradeOf(monster);
                int awaken = _awakenLevelOf(MonsterGrades.BaseOf(monster));
                string gradeText = string.Format(_text(StrRowGrade), grade, _db.HpMulOf(monster).ToString("0.00", CultureInfo.InvariantCulture));
                if (monster == _defaults[_selected])
                {
                    gradeText += " · " + _text(StrOwnedTop);
                }
                list.Add(new FormationSpeciesData
                {
                    Monster = monster,
                    Grade = grade,
                    NameText = SpeciesVisual.MonsterName(monster, _db, _text),
                    GradeText = gradeText,
                    SpeciesColor = SpeciesVisual.SpeciesGlowColor(monster),
                    AwakenText = string.Format(_text(StrAwaken), awaken),
                    ShowAwaken = awaken > 0,
                    Axis = SpeciesVisual.SpeciesAxis(monster),
                    IsSelected = monster == _formation[_selected],
                });
            }
            return list;
        }

        private string RaceName(ERace race)
        {
            RaceRow row = _db.RaceRowOf(race);
            return row != null ? _text(row.nameKey) : race.ToString();
        }
    }
}
