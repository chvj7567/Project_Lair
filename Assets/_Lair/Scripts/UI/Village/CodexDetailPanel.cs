using System;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도감 상세 패널(monster-codex §4) — 표시만. 로직(문구·조건 판정)은 CodexViewModel.BuildDetail 이 만든 모델을 그대로 그린다.
    public class CodexDetailPanel : MonoBehaviour
    {
        [SerializeField] private CodexDetailPreview _preview;
        [SerializeField] private Image _previewBorder;           //# 등급색 3px 테두리(잠금 #232834)
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _gradeChipText;
        [SerializeField] private CHText _tierChipText;
        [SerializeField] private CHText _raceText;
        [SerializeField] private Image[] _stars;                 //# 등급 수만큼 켜짐
        [SerializeField] private CHText _progressText;
        [SerializeField] private CodexTierMiniCell[] _tierCells;
        [SerializeField] private CHText _statsText;              //# 해금: 배율 / 잠금: 숨김
        [SerializeField] private CHText _conditionTitleText;
        [SerializeField] private CHText[] _conditionTexts;
        [SerializeField] private CHText _hiddenStatsText;
        [SerializeField] private CHText _appearanceText;

        private static readonly Color LockedBorder = new Color32(0x23, 0x28, 0x34, 0xFF);
        private static readonly Color LockedText = new Color32(0x7D, 0x87, 0x9F, 0xFF);
        private static readonly Color LockedStar = new Color32(0x59, 0x61, 0x7A, 0xFF);
        private static readonly Color Gold = new Color32(0xF7, 0xC6, 0x4A, 0xFF);
        private static readonly Color Met = new Color32(0x5E, 0xF0, 0xB4, 0xFF);
        private static readonly Color Unmet = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        public void OpenPreview()
        {
            if (_preview != null)
            {
                _preview.Open();
            }
        }

        public void ClosePreview()
        {
            if (_preview != null)
            {
                _preview.Close();
            }
        }

        public void Bind(CodexDetailModel model, Color gradeColor, Func<CodexCellModel, Sprite> bodyOf, Func<int, string> tierLabel,
            Func<int, string> levelRange, Action<int> onTierSelected)
        {
            if (model == null || model.Cell == null)
                return;
            bool locked = model.Locked;
            Color text = locked ? LockedText : Color.white;

            if (_previewBorder != null)
            {
                _previewBorder.color = locked ? LockedBorder : gradeColor;
            }
            SetText(_nameText, model.Name, text);
            SetText(_gradeChipText, model.GradeName, text);
            SetText(_tierChipText, model.TierLabel, text);
            SetText(_raceText, model.RaceLabel, text);
            SetText(_progressText, model.RaceProgress, text);
            SetText(_appearanceText, model.AppearanceText, text);
            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; ++i)
                {
                    if (_stars[i] == null)
                        continue;
                    _stars[i].gameObject.SetActive(i < model.StarCount);
                    _stars[i].color = locked ? LockedStar : Gold;
                }
            }

            BindTierCells(model, bodyOf, tierLabel, levelRange, onTierSelected);
            BindStatsOrConditions(model);
        }

        public Task ShowPreviewAsync(CodexDetailModel model, IMonsterTierSheetProvider tiers)
        {
            if (_preview == null || model == null || model.Cell == null)
                return Task.CompletedTask;
            CodexCellModel cell = model.Cell;
            return _preview.ShowAsync(cell.Monster, cell.Race, cell.Tier, model.Locked, tiers);
        }

        private void BindTierCells(CodexDetailModel model, Func<CodexCellModel, Sprite> bodyOf, Func<int, string> tierLabel,
            Func<int, string> levelRange, Action<int> onTierSelected)
        {
            if (_tierCells == null)
                return;
            for (int i = 0; i < _tierCells.Length; ++i)
            {
                CodexTierMiniCell mini = _tierCells[i];
                if (mini == null)
                    continue;
                bool exists = i < model.SameGradeTiers.Count;
                mini.gameObject.SetActive(exists);
                if (exists == false)
                    continue;
                CodexCellModel tierCell = model.SameGradeTiers[i];
                mini.Bind(tierCell, bodyOf != null ? bodyOf(tierCell) : null, tierLabel(tierCell.Tier), levelRange(tierCell.Tier),
                    tierCell.Tier == model.Cell.Tier, onTierSelected);
            }
        }

        private void BindStatsOrConditions(CodexDetailModel model)
        {
            bool locked = model.Locked;
            SetActive(_statsText, locked == false);
            SetActive(_conditionTitleText, locked);
            SetActive(_hiddenStatsText, locked);
            if (locked == false)
            {
                SetText(_statsText, model.StatsText, Color.white);
            }
            else
            {
                SetText(_conditionTitleText, model.ConditionTitle, LockedText);
                SetText(_hiddenStatsText, model.HiddenStatsText, LockedText);
            }
            if (_conditionTexts == null)
                return;
            for (int i = 0; i < _conditionTexts.Length; ++i)
            {
                CHText line = _conditionTexts[i];
                if (line == null)
                    continue;
                bool show = locked && i < model.Conditions.Count;
                line.gameObject.SetActive(show);
                if (show == false)
                    continue;
                CodexConditionLine condition = model.Conditions[i];
                line.SetText((condition.Met ? "O " : "X ") + condition.Text);
                line.SetColor(condition.Met ? Met : Unmet);
            }
        }

        private static void SetText(CHText target, string value, Color color)
        {
            if (target == null)
                return;
            target.SetText(value ?? string.Empty);
            target.SetColor(color);
        }

        private static void SetActive(CHText target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }
    }
}
