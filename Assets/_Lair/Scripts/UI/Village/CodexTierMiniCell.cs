using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 상세 패널의 티어 선택 미니 칸(monster-codex §4) — 같은 등급의 기본·T1·T2·T3. 잠긴 티어는 실루엣. 프리팹 정적 배치.
    public class CodexTierMiniCell : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _selectedFrame;
        [SerializeField] private Image _body;
        [SerializeField] private CHText _tierText;
        [SerializeField] private CHText _levelText;

        private static readonly Color Gold = new Color32(0xF7, 0xC6, 0x4A, 0xFF);

        private Action<int> _onSelect;
        private int _tier;
        private bool _wired;

        public void Bind(CodexCellModel model, Sprite body, string tierLabel, string levelRange, bool selected, Action<int> onSelect)
        {
            if (model == null)
                return;
            _tier = model.Tier;
            _onSelect = onSelect;
            if (_button != null && _wired == false)
            {
                _wired = true;
                _button.OnClick(() => _onSelect?.Invoke(_tier));
            }
            if (_body != null)
            {
                _body.sprite = body;
                _body.enabled = body != null;   //# 스프라이트 없는 Image 는 흰 사각형
                _body.color = model.Open ? Color.white : Color.black;
            }
            if (_selectedFrame != null)
            {
                _selectedFrame.gameObject.SetActive(selected);
                _selectedFrame.color = Gold;
            }
            if (_tierText != null)
            {
                _tierText.SetText(tierLabel);
            }
            if (_levelText != null)
            {
                _levelText.SetText(levelRange);
            }
        }
    }
}
