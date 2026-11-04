using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class FormationPopupArg : UIArg
    {
        public FormationViewModel ViewModel;
        //# 출격 확정 — 저장할 편성 행(0행 = 기본 편성, 6행 = 사용자 편성). 저장·입장은 호출자(VillageController) 소유.
        public Action<List<FormationSlotEntry>> OnSortie;
    }

    //# 던전 편성 팝업 (dungeon-formation §0.10) — 침입 요약 + 둥지 칸 6(종족 고정) + 우측 종족 정보·보유 몬스터 행 최대 5 + 하단 바. 칸·행은 프리팹 정적 배치(스크롤 없음).
    //# 표시·입력만 한다. 모든 계산은 FormationViewModel (Rule 02 §6).
    public class FormationPopup : UIBase
    {
        private static readonly Color BoneColor = new Color32(0xe8, 0xe1, 0xcf, 0xff);

        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHButton _resetButton;
        [SerializeField] private CHButton _sortieButton;

        //# 침입 요약
        [SerializeField] private CHText _stageNumberText;    //# "2-2"
        [SerializeField] private CHText _stageNameText;
        [SerializeField] private CHText _heroText;           //# "영웅: 궁수"
        [SerializeField] private GameObject _difficultyChipRoot;   //# Normal/Hard 만 표시
        [SerializeField] private Image _difficultyChipBackground;
        [SerializeField] private CHText _difficultyChipText;
        [SerializeField] private CHText _hintText;           //# 영웅 성향 문구
        [SerializeField] private CHText _rulesText;
        [SerializeField] private GameObject _sealForecastRoot;
        [SerializeField] private CHText _sealForecastText;

        //# 둥지 칸 6 + 선택 둥지 종족의 몬스터 행 최대 5(등급 1~5) (프리팹 인스턴스 정적 배치)
        [SerializeField] private FormationNestTile[] _nestTiles = new FormationNestTile[0];
        [SerializeField] private FormationSpeciesRow[] _speciesRows = new FormationSpeciesRow[0];

        //# 우측 상단 종족 정보 행 (종족 이름 + 성격 문구)
        [SerializeField] private CHText _raceNameText;
        [SerializeField] private CHText _raceDescText;
        [SerializeField] private Image _raceBorder;

        //# 몬스터 아이콘 — 인덱스 = (int)기준 몬스터 (도감 몬스터 아이콘 재사용, 등급 몬스터도 기준 아이콘 — monster-grades V7)
        [SerializeField] private Sprite[] _monsterIcons = new Sprite[0];

        private FormationViewModel _vm;
        private Action<List<FormationSlotEntry>> _onSortie;

        public override void InitUI(UIArg arg)
        {
            FormationPopupArg popupArg = arg as FormationPopupArg;
            if (popupArg == null || popupArg.ViewModel == null)
                return;

            _vm = popupArg.ViewModel;
            _onSortie = popupArg.OnSortie;
            _vm.OnChanged += Refresh;
            closeDisposable.Add(() =>
            {
                if (_vm != null)
                {
                    _vm.OnChanged -= Refresh;
                }
                _vm = null;
                _onSortie = null;
            });

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_resetButton != null)
            {
                _resetButton.OnClick(() => _vm?.ResetToDefault(), closeDisposable);
            }
            if (_sortieButton != null)
            {
                _sortieButton.OnClick(HandleSortie, closeDisposable);
            }

            for (int i = 0; i < _nestTiles.Length; ++i)
            {
                if (_nestTiles[i] == null)
                    continue;
                int index = i;
                _nestTiles[i].SetClickHandler(() => _vm?.SelectNest(index), closeDisposable);
            }
            //# 행 목록은 선택 둥지마다 바뀌므로 클릭은 행 인덱스로 받아 그 순간의 목록에서 몬스터를 찾는다.
            for (int i = 0; i < _speciesRows.Length; ++i)
            {
                if (_speciesRows[i] == null)
                    continue;
                int index = i;
                _speciesRows[i].SetClickHandler(() => HandlePlace(index), closeDisposable);
            }

            ShowSummary();
            Refresh();
        }

        //# 몬스터 행 클릭 — 선택 둥지에 그 몬스터 지정(VM 이 소속·보유를 판정).
        private void HandlePlace(int rowIndex)
        {
            if (_vm == null)
                return;
            IReadOnlyList<FormationSpeciesData> rows = _vm.BuildSpecies();
            if (rowIndex < 0 || rowIndex >= rows.Count)
                return;
            _vm.Place(rows[rowIndex].Monster);
        }

        private void HandleSortie()
        {
            if (_vm == null)
                return;
            Action<List<FormationSlotEntry>> onSortie = _onSortie;
            List<FormationSlotEntry> rows = _vm.BuildSaveRows();
            Close(reuse: true);
            onSortie?.Invoke(rows);
        }

        //# 침입 요약은 열 때 한 번만 채운다(편성 변경과 무관). 봉인 예고만 Refresh 가 갱신한다.
        private void ShowSummary()
        {
            if (_stageNumberText != null)
            {
                _stageNumberText.SetText(_vm.StageNumberText);
            }
            if (_stageNameText != null)
            {
                _stageNameText.SetText(_vm.StageNameText);
            }
            if (_heroText != null)
            {
                _heroText.SetText(_vm.HeroText);
            }
            if (_hintText != null)
            {
                _hintText.SetText(_vm.HintText);
            }
            if (_rulesText != null)
            {
                _rulesText.SetText(_vm.RulesText);
            }
            bool showChip = _vm.Difficulty >= 1;
            if (_difficultyChipRoot != null)
            {
                _difficultyChipRoot.SetActive(showChip);
            }
            if (showChip && _difficultyChipBackground != null)
            {
                _difficultyChipBackground.color = UiDotPalette.DifficultyColor(_vm.Difficulty);
            }
            if (showChip && _difficultyChipText != null)
            {
                _difficultyChipText.SetText(_vm.DifficultyText);
            }
        }

        private void Refresh()
        {
            if (_vm == null)
                return;

            for (int i = 0; i < _nestTiles.Length && i < FormationDatabase.SlotCount; ++i)
            {
                if (_nestTiles[i] != null)
                {
                    FormationNestData data = _vm.GetNest(i);
                    _nestTiles[i].Show(data, IconOf(data.Monster));
                }
            }
            IReadOnlyList<FormationSpeciesData> species = _vm.BuildSpecies();
            for (int i = 0; i < _speciesRows.Length; ++i)
            {
                if (_speciesRows[i] != null)
                {
                    FormationSpeciesData row = i < species.Count ? species[i] : null;
                    _speciesRows[i].Show(row, row != null ? IconOf(row.Monster) : null);
                }
            }
            if (_raceNameText != null)
            {
                _raceNameText.SetText(_vm.SelectedRaceName);
                _raceNameText.SetColor(_vm.SelectedRaceColor);
            }
            if (_raceDescText != null)
            {
                _raceDescText.SetText(_vm.SelectedRaceDesc);
            }
            if (_raceBorder != null)
            {
                _raceBorder.color = _vm.SelectedRaceColor;
            }
            if (_sealForecastRoot != null)
            {
                _sealForecastRoot.SetActive(_vm.HasSealForecast);
            }
            if (_sealForecastText != null && _vm.HasSealForecast)
            {
                _sealForecastText.SetText(_vm.SealForecastText);
            }
        }

        private Sprite IconOf(EMonster monster)
        {
            int index = (int)MonsterGrades.BaseOf(monster);
            return index >= 0 && index < _monsterIcons.Length ? _monsterIcons[index] : null;
        }
    }
}
