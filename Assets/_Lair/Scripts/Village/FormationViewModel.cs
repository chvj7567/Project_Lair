using System;
using System.Collections.Generic;
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
        public string NameText;
        public Color SpeciesColor;
        public string CostText;         //# "코스트 3"
        public int Cost;                //# 금색 칸 개수
        public bool IsSelected;
        public bool IsDefault;          //# 기본 편성과 같은 종 — 회색 원 표식
        public bool IsSealed;           //# 봉인 예정 칸 — 사슬 표식
    }

    //# 종 팔레트 행 한 개의 확정 표시값.
    public class FormationSpeciesData
    {
        public EMonster Monster;
        public string NameText;
        public Color SpeciesColor;
        public string CostText;         //# "코스트 3"
        public string CountText;        //# "편성 2 / 3"
        public string AwakenText;       //# "각성 Lv 2"
        public bool ShowAwaken;         //# 상점 각성 Lv 0 이면 숨김
        public EBuildAxis Axis;         //# 축 칩 (이름은 axis.ToString())
        public bool Dimmed;             //# 선택 둥지에 넣으면 무효가 되는 종
    }

    //# 던전 편성 팝업 ViewModel (dungeon-formation §7) — 코스트 합·종별 수·행 활성·봉인 예고·토스트 문구·저장 행 계산.
    //# View 를 모르고 프로필은 읽기만 한다(저장은 출격 콜백 소유자). Unity 오브젝트 비의존(Color 값만).
    public class FormationViewModel
    {
        //# Strings_Ko id (dungeon-formation §7.5 — 1393 시작 연속 배정).
        public const int StrCostGauge = 1397;      //# 코스트 {0} / {1}
        public const int StrCostOf = 1398;         //# 코스트 {0}
        public const int StrCountOf = 1399;        //# 편성 {0} / {1}
        public const int StrAwaken = 1400;         //# 각성 Lv {0}
        public const int StrNest = 1401;           //# 둥지 {0}
        public const int StrToastCost = 1402;      //# 코스트가 부족합니다 (필요 {0} · 남은 코스트 {1})
        public const int StrToastSpecies = 1403;   //# {0}은(는) 최대 {1}곳까지 편성할 수 있습니다
        public const int StrSealForecast = 1404;   //# 봉인 예정: 둥지 {0} ({1}) · 영웅 HP {2}% 에서 해제
        public const int StrToastInvalid = 1405;   //# 코스트를 초과한 편성입니다. 고친 뒤 출격하세요

        private readonly FormationDatabase _db;
        private readonly Func<int, string> _text;
        private readonly Func<EMonster, int> _awakenLevelOf;
        private readonly EMonster[] _formation;
        private readonly EMonster[] _defaults;
        private readonly int _maxPerSpecies;
        private readonly StageRuleData _sealRule;
        private readonly EMonster _sealSpecies;
        private int _selected;

        public event Action OnChanged;

        //# stage: 선형 스테이지 번호. awakenLevelOf: 종 → 상점 각성 레벨(Enhance_<종>).
        public FormationViewModel(
            FormationDatabase db, ChapterDatabase chapters, MetaProfile profile, int stage,
            Func<int, string> text, Func<EMonster, int> awakenLevelOf)
        {
            _db = db;
            _text = text ?? (id => string.Empty);
            _awakenLevelOf = awakenLevelOf ?? (monster => 0);
            _defaults = db.DefaultFormation();
            EMonster[] saved = FormationRules.FromSaved(profile.Formation);
            _formation = saved ?? db.DefaultFormation();
            _maxPerSpecies = FormationRules.MaxPerSpecies(db.Config, profile.ClearedStage);
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
        public int CostCap => _db.Config.costCap;
        public int MaxPerSpecies => _maxPerSpecies;
        public int CostSum => FormationRules.CostSum(_formation, _db.CostOf);
        public string CostGaugeText => string.Format(_text(StrCostGauge), CostSum, CostCap);

        //# 코스트 합이 상한 초과이거나 종당 최대 초과(무효 복원 편성 등) — 게이지 적색 + 출격 비활성.
        public bool IsInvalid => FormationRules.IsValid(_formation, CostCap, _maxPerSpecies, _db.CostOf) == false;
        public bool CanSortie => IsInvalid == false;

        //# 게이지 칸 수 = max(상한, 코스트 합). 앞 CostSum 칸이 사용분, 상한 밖은 초과 칸.
        public int PipCount => Math.Max(CostCap, CostSum);

        //# 봉인된 둥지 스테이지에서 봉인될 둥지 인덱스 — 아니면 -1. 전투와 같은 FormationRules.PickSealedSlot.
        public int SealedIndex => _sealRule != null ? FormationRules.PickSealedSlot(_formation, _sealSpecies, _db.CostOf) : -1;

        public bool HasSealForecast => SealedIndex >= 0;

        public string SealForecastText
        {
            get
            {
                int index = SealedIndex;
                if (index < 0)
                    return string.Empty;
                int percent = SealedNestInfo.AnyUnlockHpPercent(_sealRule);
                return string.Format(_text(StrSealForecast), index + 1, SpeciesVisual.SpeciesName(_formation[index]), percent);
            }
        }

        public void SelectNest(int index)
        {
            if (index < 0 || index >= _formation.Length || index == _selected)
                return;
            _selected = index;
            OnChanged?.Invoke();
        }

        //# 선택 둥지에 monster 배치. 불가하면 토스트 문구를 돌려주고 상태는 그대로, 성공/무동작이면 null.
        //# 성공 시 다음 둥지로 선택 이동(6 다음은 1).
        public string Place(EMonster monster)
        {
            EFormationPlace result = FormationRules.CanPlace(_formation, _selected, monster, CostCap, _maxPerSpecies, _db.CostOf);
            switch (result)
            {
                case EFormationPlace.Same:
                    return null;
                case EFormationPlace.OverCost:
                    int remaining = CostCap - (CostSum - _db.CostOf(_formation[_selected]));
                    return string.Format(_text(StrToastCost), _db.CostOf(monster), remaining);
                case EFormationPlace.OverSpecies:
                    return string.Format(_text(StrToastSpecies), SpeciesVisual.SpeciesName(monster), _maxPerSpecies);
            }
            _formation[_selected] = monster;
            _selected = (_selected + 1) % _formation.Length;
            OnChanged?.Invoke();
            return null;
        }

        //# 표시 편성을 기본 편성으로 되돌림(저장은 출격 때) — 선택은 둥지 1.
        public void ResetToDefault()
        {
            Array.Copy(_defaults, _formation, _formation.Length);
            _selected = 0;
            OnChanged?.Invoke();
        }

        //# 출격 시도 — 무효면 토스트 문구와 함께 false.
        public bool TrySortie(out string toast)
        {
            toast = IsInvalid ? _text(StrToastInvalid) : null;
            return IsInvalid == false;
        }

        //# 저장 행 — 기본 편성과 같으면 0행, 다르면 6행.
        public List<FormationSlotEntry> BuildSaveRows()
        {
            return FormationRules.ToSaved(_formation, _defaults);
        }

        public FormationNestData GetNest(int index)
        {
            EMonster monster = _formation[index];
            int cost = _db.CostOf(monster);
            return new FormationNestData
            {
                Index = index,
                TitleText = string.Format(_text(StrNest), index + 1),
                Monster = monster,
                NameText = SpeciesVisual.SpeciesName(monster),
                SpeciesColor = SpeciesVisual.SpeciesGlowColor(monster),
                CostText = string.Format(_text(StrCostOf), cost),
                Cost = cost,
                IsSelected = index == _selected,
                IsDefault = monster == _defaults[index],
                IsSealed = index == SealedIndex,
            };
        }

        public IReadOnlyList<FormationSpeciesData> BuildSpecies()
        {
            List<FormationSpeciesData> list = new List<FormationSpeciesData>();
            foreach (EMonster monster in _db.Monsters)
            {
                int awaken = _awakenLevelOf(monster);
                EFormationPlace place = FormationRules.CanPlace(_formation, _selected, monster, CostCap, _maxPerSpecies, _db.CostOf);
                list.Add(new FormationSpeciesData
                {
                    Monster = monster,
                    NameText = SpeciesVisual.SpeciesName(monster),
                    SpeciesColor = SpeciesVisual.SpeciesGlowColor(monster),
                    CostText = string.Format(_text(StrCostOf), _db.CostOf(monster)),
                    CountText = string.Format(_text(StrCountOf), FormationRules.CountOf(_formation, monster), _maxPerSpecies),
                    AwakenText = string.Format(_text(StrAwaken), awaken),
                    ShowAwaken = awaken > 0,
                    Axis = SpeciesVisual.SpeciesAxis(monster),
                    Dimmed = place == EFormationPlace.OverCost || place == EFormationPlace.OverSpecies,
                });
            }
            return list;
        }
    }
}
