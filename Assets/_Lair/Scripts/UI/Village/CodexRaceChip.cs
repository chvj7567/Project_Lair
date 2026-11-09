using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 종족 칩(monster-codex §2) — 종족 색 띠 + 이름 + "n/20" + NEW 점. 프리팹 정적 배치 6개(종족 수는 데이터로 늘면 배열만 늘린다).
    public class CodexRaceChip : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _colorBar;
        [SerializeField] private Image _background;
        [SerializeField] private Image _selectedFrame;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _countText;
        [SerializeField] private GameObject _newDot;

        private Action<EMonster> _onSelect;
        private EMonster _race;
        private bool _wired;

        public void Bind(CodexRaceChipModel model, bool selected, Action<EMonster> onSelect)
        {
            if (model == null)
                return;
            _race = model.Race;
            _onSelect = onSelect;
            if (_button != null && _wired == false)
            {
                _wired = true;
                _button.OnClick(() => _onSelect?.Invoke(_race));
            }
            if (_colorBar != null)
            {
                _colorBar.color = SpeciesVisual.SpeciesGlowColor(model.Race);
            }
            if (_selectedFrame != null)
            {
                _selectedFrame.gameObject.SetActive(selected);
            }
            if (_nameText != null)
            {
                _nameText.SetText(model.Name);
            }
            if (_countText != null)
            {
                _countText.SetText($"{model.OpenCount}/{model.TotalCount}");
            }
            if (_newDot != null)
            {
                _newDot.SetActive(model.HasUnseenNew);
            }
        }
    }
}
