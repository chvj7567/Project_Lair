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

    //# 던전 편성 팝업 (dungeon-formation §7) — 침입 요약 + 둥지 칸 6 + 코스트 게이지 + 종 팔레트 6 + 하단 바. 칸·행은 프리팹 정적 배치(스크롤 없음).
    //# 표시·입력만 한다. 모든 계산은 FormationViewModel (Rule 02 §6).
    public class FormationPopup : UIBase
    {
        private static readonly Color BoneColor = new Color32(0xe8, 0xe1, 0xcf, 0xff);

        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHButton _resetButton;
        [SerializeField] private CHButton _sortieButton;
        [SerializeField] private Image _sortieBackground;
        [SerializeField] private Sprite _sortieOnSprite;     //# 유효 편성 — 붉은 출격 버튼
        [SerializeField] private Sprite _sortieOffSprite;    //# 무효 편성 — 회색(누르면 토스트)

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

        //# 둥지 칸 6 + 종 팔레트 6 (프리팹 인스턴스 정적 배치)
        [SerializeField] private FormationNestTile[] _nestTiles = new FormationNestTile[0];
        [SerializeField] private FormationSpeciesRow[] _speciesRows = new FormationSpeciesRow[0];

        //# 코스트 게이지
        [SerializeField] private CHText _costGaugeText;
        [SerializeField] private Image[] _costPips = new Image[0];

        //# 종 아이콘 — 인덱스 = (int)EMonster (도감 몬스터 아이콘 재사용)
        [SerializeField] private Sprite[] _monsterIcons = new Sprite[0];

        private static readonly Color PipOn = new Color32(0xf7, 0xc6, 0x4a, 0xff);
        private static readonly Color PipOff = new Color32(0x0a, 0x0d, 0x14, 0xff);

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
            IReadOnlyList<FormationSpeciesData> species = _vm.BuildSpecies();
            for (int i = 0; i < _speciesRows.Length && i < species.Count; ++i)
            {
                if (_speciesRows[i] == null)
                    continue;
                EMonster monster = species[i].Monster;
                _speciesRows[i].SetClickHandler(() => HandlePlace(monster), closeDisposable);
            }

            ShowSummary();
            Refresh();
        }

        //# 팔레트 클릭 — 불가하면 VM 이 돌려준 토스트 문구를 띄운다.
        private void HandlePlace(EMonster monster)
        {
            if (_vm == null)
                return;
            string toast = _vm.Place(monster);
            if (string.IsNullOrEmpty(toast) == false)
            {
                ToastView.Show(toast, EToastKind.Warning);
            }
        }

        private void HandleSortie()
        {
            if (_vm == null)
                return;
            if (_vm.TrySortie(out string toast) == false)
            {
                ToastView.Show(toast, EToastKind.Warning);
                return;
            }
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
            for (int i = 0; i < _speciesRows.Length && i < species.Count; ++i)
            {
                if (_speciesRows[i] != null)
                {
                    _speciesRows[i].Show(species[i], IconOf(species[i].Monster));
                }
            }

            RefreshGauge();
            if (_sealForecastRoot != null)
            {
                _sealForecastRoot.SetActive(_vm.HasSealForecast);
            }
            if (_sealForecastText != null && _vm.HasSealForecast)
            {
                _sealForecastText.SetText(_vm.SealForecastText);
            }
            if (_sortieBackground != null)
            {
                _sortieBackground.sprite = _vm.CanSortie ? _sortieOnSprite : _sortieOffSprite;
            }
        }

        //# 코스트 게이지 — 사용분 금색, 남은분 어두운 칸, 상한 밖 초과분·문자는 적색(무효 편성).
        private void RefreshGauge()
        {
            if (_costGaugeText != null)
            {
                _costGaugeText.SetText(_vm.CostGaugeText);
                _costGaugeText.SetColor(_vm.IsInvalid ? UiDotPalette.Blood : BoneColor);
            }
            int shown = _vm.PipCount;
            for (int i = 0; i < _costPips.Length; ++i)
            {
                if (_costPips[i] == null)
                    continue;
                _costPips[i].gameObject.SetActive(i < shown);
                if (i >= shown)
                    continue;
                bool used = i < _vm.CostSum;
                bool over = i >= _vm.CostCap;
                _costPips[i].color = used ? (over ? UiDotPalette.Blood : PipOn) : PipOff;
            }
        }

        private Sprite IconOf(EMonster monster)
        {
            int index = (int)monster;
            return index >= 0 && index < _monsterIcons.Length ? _monsterIcons[index] : null;
        }
    }
}
