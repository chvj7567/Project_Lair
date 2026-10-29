using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 유물 도감 하단 잔해 교환 줄 (monster-growth-currencies §8.3) — 잔해 칩 + 교환 버튼 3개. 표시·입력만, 판단은 ShardExchangeViewModel.
    public class ShardExchangeView : MonoBehaviour
    {
        [SerializeField] private GameObject _row;           //# 교환 줄 루트
        [SerializeField] private GameObject _hint;          //# 기존 안내 문구 — 교환 줄이 보이면 숨긴다
        [SerializeField] private CHText _headerText;
        [SerializeField] private CHText _shardLabelText;
        [SerializeField] private CHText _shardCountText;
        [SerializeField] private CHButton[] _buttons = new CHButton[0];
        [SerializeField] private CHText[] _buttonLabels = new CHText[0];
        [SerializeField] private Image[] _buttonBorders = new Image[0];
        [SerializeField] private GameObject[] _buttonLocks = new GameObject[0];
        [SerializeField] private CanvasGroup[] _buttonGroups = new CanvasGroup[0];

        private const float OffAlpha = 0.45f;

        private ShardExchangeRowData _data;
        private Action<ECurrency> _onExchange;
        private bool _wired;

        public void Bind(ShardExchangeRowData data, Action<ECurrency> onExchange)
        {
            _data = data;
            _onExchange = onExchange;
            bool visible = data != null && data.Visible;
            if (_row != null)
            {
                _row.SetActive(visible);
            }
            if (_hint != null)
            {
                _hint.SetActive(visible == false);
            }
            if (visible == false)
                return;
            WireOnce();
            SetText(_headerText, data.HeaderText);
            SetText(_shardLabelText, data.ShardLabelText);
            SetText(_shardCountText, data.ShardCountText);
            for (int i = 0; i < _buttons.Length; ++i)
            {
                ShardExchangeButtonData button = i < data.Buttons.Length ? data.Buttons[i] : null;
                if (_buttons[i] != null)
                {
                    _buttons[i].gameObject.SetActive(button != null);
                }
                if (button == null)
                    continue;
                if (i < _buttonLabels.Length && _buttonLabels[i] != null)
                {
                    _buttonLabels[i].SetText(button.LabelText);
                }
                if (i < _buttonBorders.Length && _buttonBorders[i] != null)
                {
                    _buttonBorders[i].gameObject.SetActive(button.CanExchange);
                    _buttonBorders[i].color = ColorUtility.TryParseHtmlString(button.ColorHex, out Color c) ? c : Color.white;
                }
                if (i < _buttonLocks.Length && _buttonLocks[i] != null)
                {
                    _buttonLocks[i].SetActive(button.IsLocked);
                }
                if (i < _buttonGroups.Length && _buttonGroups[i] != null)
                {
                    _buttonGroups[i].alpha = button.CanExchange ? 1f : OffAlpha;
                }
            }
        }

        //# CHButton.OnClick 은 listener 누적 — 1회만 등록하고 현재 _data 로 위임.
        private void WireOnce()
        {
            if (_wired)
                return;
            _wired = true;
            for (int i = 0; i < _buttons.Length; ++i)
            {
                int index = i;
                if (_buttons[i] != null)
                {
                    _buttons[i].OnClick(() => HandleClick(index));
                }
            }
        }

        private static void SetText(CHText label, string text)
        {
            if (label == null)
                return;
            label.SetText(text ?? string.Empty);
        }

        private void HandleClick(int index)
        {
            if (_data == null || index >= _data.Buttons.Length)
                return;
            _onExchange?.Invoke(_data.Buttons[index].To);
        }
    }
}
