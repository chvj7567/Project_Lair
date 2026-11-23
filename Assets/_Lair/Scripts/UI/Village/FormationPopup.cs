using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class FormationPopupArg : UIArg
    {
        public FormationViewModel ViewModel;
        public Action<bool, List<FormationSlotEntry>> OnSortie;
        public Sprite HeroPortrait;
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
        [SerializeField] private CHButton _clearButton;
        [SerializeField] private CHButton[] _speciesFilters = new CHButton[0];
        [SerializeField] private FormationSpeciesPoolingScrollView _speciesScrollView;
        [SerializeField] private CHText _statusText;
        [SerializeField] private CHText _filledText;
        [SerializeField] private CHText _emptyWarningText;

        //# 침입 요약
        [SerializeField] private CHText _stageNumberText;    //# "2-2"
        [SerializeField] private CHText _stageNameText;
        [SerializeField] private CHText _heroText;           //# "영웅: 궁수"
        [SerializeField] private Image _heroPortrait;
        [SerializeField] private GameObject _difficultyChipRoot;   //# Normal/Hard 만 표시
        [SerializeField] private Image _difficultyChipBackground;
        [SerializeField] private CHText _difficultyChipText;
        [SerializeField] private CHText _hintText;           //# 영웅 성향 문구
        [SerializeField] private CHText _rulesText;
        [SerializeField] private GameObject _sealForecastRoot;
        [SerializeField] private CHText _sealForecastText;

        //# 둥지 칸 6 + 선택 둥지 종족의 몬스터 행 최대 5(등급 1~5) (프리팹 인스턴스 정적 배치)
        [SerializeField] private FormationNestTile[] _nestTiles = new FormationNestTile[0];

        //# 우측 상단 종족 정보 행 (종족 이름 + 성격 문구)
        [SerializeField] private CHText _raceNameText;
        [SerializeField] private CHText _raceDescText;

        //# 몬스터 아이콘 — 인덱스 = (int)기준 몬스터 (도감 몬스터 아이콘 재사용, 등급 몬스터도 기준 아이콘 — monster-grades V7)
        [SerializeField] private Sprite[] _monsterIcons = new Sprite[0];

        private FormationViewModel _vm;
        private Action<bool, List<FormationSlotEntry>> _onSortie;
        private int _lastFilter = -1;
        private GameObject _focusBeforeOpen;
        private bool _departing;

        public override void InitUI(UIArg arg)
        {
            FormationPopupArg popupArg = arg as FormationPopupArg;
            if (popupArg == null || popupArg.ViewModel == null)
                return;

            _vm = popupArg.ViewModel;
            _onSortie = popupArg.OnSortie;
            _focusBeforeOpen = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            _departing = false;
            if (_heroPortrait != null)
            {
                _heroPortrait.sprite = popupArg.HeroPortrait;
                _heroPortrait.enabled = popupArg.HeroPortrait != null;
            }
            _lastFilter = -1;
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
                _resetButton.OnClick(ResetFormation, closeDisposable);
            }
            if (_sortieButton != null)
            {
                _sortieButton.OnClick(HandleSortie, closeDisposable);
            }
            if (_clearButton != null)
            {
                _clearButton.SetText(_vm.ClearText);
                _clearButton.OnClick(ClearSelectedNest, closeDisposable);
            }
            for (int i = 0; i < _speciesFilters.Length && i <= FormationDatabase.SlotCount; ++i)
            {
                CHButton filter = _speciesFilters[i];
                if (filter == null)
                    continue;
                int index = i;
                filter.OnClick(() => _vm?.SelectFilter(index), closeDisposable);
            }
            _speciesScrollView?.Configure(IconOf, monster => _vm?.Place(monster));

            for (int i = 0; i < _nestTiles.Length; ++i)
            {
                if (_nestTiles[i] == null)
                    continue;
                int index = i;
                _nestTiles[i].SetClickHandler(() => _vm?.SelectNest(index), closeDisposable);
            }

            ShowSummary();
            Refresh();
            FocusSelectedNest();
        }

        public override void Close(bool reuse = true)
        {
            GameObject restore = _focusBeforeOpen;
            _focusBeforeOpen = null;
            base.Close(reuse);
            if (_departing == false && restore != null && restore.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(restore);
            }
        }

        private void Update()
        {
            if (_vm == null || Input.GetKeyDown(KeyCode.Tab) == false || EventSystem.current == null)
                return;
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            MoveTabFocus(reverse ? -1 : 1);
        }

        private void MoveTabFocus(int direction)
        {
            List<GameObject> beforeRows = new List<GameObject>();
            List<GameObject> afterRows = new List<GameObject>();
            AddFocusTarget(beforeRows, _closeButton);
            foreach (FormationNestTile nest in _nestTiles)
            {
                if (nest != null && nest.FocusTarget != null)
                {
                    beforeRows.Add(nest.FocusTarget);
                }
            }
            AddFocusTarget(beforeRows, _clearButton);
            foreach (CHButton filter in _speciesFilters)
            {
                AddFocusTarget(beforeRows, filter);
            }
            AddFocusTarget(afterRows, _resetButton);
            AddFocusTarget(afterRows, _sortieButton);

            int rows = _speciesScrollView != null ? _speciesScrollView.RowCount : 0;
            int total = beforeRows.Count + rows + afterRows.Count;
            if (total == 0)
                return;
            GameObject current = EventSystem.current.currentSelectedGameObject;
            int index = beforeRows.IndexOf(current);
            if (index < 0 && current != null)
            {
                int rowIndex = _speciesScrollView != null ? _speciesScrollView.FocusedRowIndex(current) : -1;
                int afterIndex = afterRows.IndexOf(current);
                if (rowIndex >= 0)
                {
                    index = beforeRows.Count + rowIndex;
                }
                else if (afterIndex >= 0)
                {
                    index = beforeRows.Count + rows + afterIndex;
                }
            }
            int next = index < 0 ? (direction < 0 ? total - 1 : 0) : (index + direction + total) % total;
            if (next < beforeRows.Count)
            {
                EventSystem.current.SetSelectedGameObject(beforeRows[next]);
            }
            else if (next < beforeRows.Count + rows)
            {
                _speciesScrollView.FocusRow(next - beforeRows.Count);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(afterRows[next - beforeRows.Count - rows]);
            }
        }

        private static void AddFocusTarget(List<GameObject> targets, CHButton button)
        {
            if (button != null && button.Interactable && button.gameObject.activeInHierarchy)
            {
                targets.Add(button.gameObject);
            }
        }

        private void FocusSelectedNest()
        {
            int index = _vm.SelectedIndex;
            if (index < _nestTiles.Length)
            {
                _nestTiles[index]?.Focus();
            }
        }

        private void ClearSelectedNest()
        {
            if (_vm == null)
                return;
            _vm.ClearNest();
            FocusSelectedNest();
        }

        private void ResetFormation()
        {
            if (_vm == null)
                return;
            _lastFilter = -1;
            _vm.ResetToDefault();
            FocusSelectedNest();
        }

        private void HandleSortie()
        {
            if (_vm == null)
                return;
            Action<bool, List<FormationSlotEntry>> onSortie = _onSortie;
            bool isCustom = _vm.IsCustom;
            List<FormationSlotEntry> rows = _vm.BuildSaveRows();
            _departing = true;
            Close(reuse: true);
            onSortie?.Invoke(isCustom, rows);
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
                    _nestTiles[i].Show(data, data.IsEmpty ? null : IconOf(data.Monster));
                }
            }
            IReadOnlyList<FormationSpeciesData> species = _vm.BuildSpecies();
            if (_speciesScrollView != null)
            {
                _speciesScrollView.ShowRows(species, _lastFilter != _vm.FilterIndex);
            }
            _lastFilter = _vm.FilterIndex;
            for (int i = 0; i < _speciesFilters.Length && i <= FormationDatabase.SlotCount; ++i)
            {
                string name = _vm.FilterName(i);
                _speciesFilters[i]?.SetText(i == _lastFilter ? "<color=#F5C542>" + name + "</color>" : name);
            }
            if (_raceNameText != null)
            {
                _raceNameText.SetText(_vm.SelectedNestText);
                _raceNameText.SetColor(BoneColor);
            }
            if (_raceDescText != null)
            {
                _raceDescText.SetText(_vm.CardsRandomText);
            }
            if (_clearButton != null)
            {
                _clearButton.Interactable = _vm.CanClear;
            }
            _statusText?.SetText(_vm.FeedbackText);
            _filledText?.SetText(_vm.FilledText);
            if (_emptyWarningText != null)
            {
                _emptyWarningText.SetText(_vm.EmptyWarningText);
                _emptyWarningText.gameObject.SetActive(_vm.AssignedCount == 0);
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
