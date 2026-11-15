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
        [Header("슬롯 아트 (매트릭스 칸과 같은 규칙, 미배선이면 기존 표시)")]
        [SerializeField] private Image _border;             //# 등급 프레임
        [SerializeField] private Sprite _bgOpen;             //# CodexCellBg
        [SerializeField] private Sprite _bgLocked;           //# CodexCellBgLocked
        [SerializeField] private Sprite[] _gradeFrames;      //# CodexCellFrameG1..G5
        [SerializeField] private float _bodyMaxPx = 33f;     //# 5등급 몸 한 변 - 이름 라벨·보더를 뺀 영역의 짧은 변
        [SerializeField] private CHText _tierText;
        [SerializeField] private CHText _levelText;

        private static readonly Color Gold = new Color32(0xF7, 0xC6, 0x4A, 0xFF);

        //# 바탕·프레임 - 해금은 원색, 미해금은 같은 등급 프레임을 어둡게(CodexSlotSkin).
        private void ApplySkin(CodexCellModel model)
        {
            Sprite bg = model.Open ? _bgOpen : _bgLocked;
            if (_background != null && bg != null)
            {
                _background.sprite = bg;
                _background.color = Color.white;
            }
            Sprite frame = CodexSlotSkin.FrameOf(_gradeFrames, model.Grade);
            if (_border != null && frame != null)
            {
                _border.sprite = frame;
                _border.color = model.Open ? Color.white : CodexSlotSkin.LockedFrameTint;
            }
        }

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
            ApplySkin(model);
            if (_body != null)
            {
                float maxMul = Mathf.Max(1f, model.MaxSizeMul);
                float side = Mathf.Min(_bodyMaxPx, _bodyMaxPx * model.SizeMul / maxMul);
                _body.rectTransform.sizeDelta = new Vector2(side, side);   //# 중앙 앵커 - 영역 중심에 놓인다
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
