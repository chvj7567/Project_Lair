using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 월드맵 난이도 세그먼트 1칸 — 표시 + 클릭 수신만 (Rule 02 §6). 판정(잠금/선택)은 WorldMapViewModel. 위젯은 private, 의도 API 만 노출(§6.1).
    public class WorldMapDifficultySegmentView : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private CHText _labelText;

        private static readonly Color AvailableBackground = new Color32(0x2a, 0x33, 0x50, 0xff);
        private static readonly Color LockedBackground = new Color32(0x23, 0x28, 0x34, 0xff);
        private static readonly Color AvailableText = new Color32(0xe8, 0xe1, 0xcf, 0xff);
        private static readonly Color LockedText = new Color32(0x6c, 0x73, 0x85, 0xff);
        private static readonly Color SelectedText = new Color32(0x10, 0x14, 0x1d, 0xff);

        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        //# boxEnabled=false(스테이지 잠김/표 미로드)면 눌러도 무반응 — 선택 표시만 남긴다.
        public void Show(WorldMapDifficultyData data, bool boxEnabled)
        {
            if (data == null)
                return;

            bool selected = data.State == EDifficultySegmentState.Selected;
            bool locked = data.State == EDifficultySegmentState.Locked || (boxEnabled == false && selected == false);
            if (_background != null)
            {
                _background.color = selected ? UiDotPalette.DifficultyColor(data.Difficulty) : (locked ? LockedBackground : AvailableBackground);
            }
            if (_labelText != null)
            {
                _labelText.SetText(data.Label);
                _labelText.SetColor(selected ? SelectedText : (locked ? LockedText : AvailableText));
            }
            if (_button != null)
            {
                _button.Interactable = boxEnabled;
            }
        }
    }
}
