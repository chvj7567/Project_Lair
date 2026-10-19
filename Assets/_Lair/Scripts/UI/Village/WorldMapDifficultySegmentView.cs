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
        [SerializeField] private Image _hatch;          //# 잠금 칸 빗금 오버레이
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _labelText;
        [SerializeField] private CHText _multiplierText;
        [SerializeField] private Image _tail;           //# 선택 칸 아래 꼬리(마름모) — 난이도색
        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private Sprite _difficultySprite;
        [SerializeField] private Sprite _lockSprite;

        private const float ShakeDuration = 0.32f;
        private const float ShakeAmplitude = 4f;

        private static readonly Color AvailableBackground = new Color32(0x2a, 0x33, 0x50, 0xff);
        private static readonly Color LockedBackground = new Color32(0x1d, 0x22, 0x2e, 0xff);
        private static readonly Color AvailableText = new Color32(0xe8, 0xe1, 0xcf, 0xff);
        private static readonly Color AvailableSubText = new Color32(0x8e, 0x98, 0xad, 0xff);
        private static readonly Color LockedText = new Color32(0x6c, 0x73, 0x85, 0xff);
        private static readonly Color LockedSubText = new Color32(0x4f, 0x56, 0x68, 0xff);
        private static readonly Color SelectedText = new Color32(0x10, 0x14, 0x1d, 0xff);

        private bool _shaking;
        private float _shakeStart;
        private Vector2 _shakeBase;

        private void OnDisable()
        {
            StopShake();
        }

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
            Color difficultyColor = UiDotPalette.DifficultyColor(data.Difficulty);
            Color nameColor = selected ? SelectedText : (locked ? LockedText : AvailableText);
            Color subColor = selected ? SelectedText : (locked ? LockedSubText : AvailableSubText);

            if (_background != null)
            {
                _background.color = selected ? difficultyColor : (locked ? LockedBackground : AvailableBackground);
            }
            if (_hatch != null)
            {
                _hatch.gameObject.SetActive(locked);
            }
            if (_icon != null)
            {
                _icon.sprite = locked ? _lockSprite : _difficultySprite;
                _icon.color = selected ? SelectedText : (locked ? LockedText : difficultyColor);
            }
            if (_labelText != null)
            {
                _labelText.SetText(data.Label);
                _labelText.SetColor(nameColor);
            }
            if (_multiplierText != null)
            {
                _multiplierText.SetText(data.MultiplierText);
                _multiplierText.SetColor(subColor);
            }
            if (_tail != null)
            {
                _tail.gameObject.SetActive(selected);
                _tail.color = difficultyColor;
            }
            if (_button != null)
            {
                _button.Interactable = boxEnabled;
            }
        }

        //# 잠긴 칸을 눌렀을 때 좌우로 짧게 흔든다(Update 기반 — 비활성/풀 재사용 시 OnDisable 이 원위치).
        public void PlayShake()
        {
            if (_shakeTarget == null)
                return;

            if (_shaking == false)
            {
                _shakeBase = _shakeTarget.anchoredPosition;
            }
            _shaking = true;
            _shakeStart = Time.unscaledTime;
        }

        private void Update()
        {
            if (_shaking == false)
                return;

            float t = (Time.unscaledTime - _shakeStart) / ShakeDuration;
            if (t >= 1f)
            {
                StopShake();
                return;
            }
            float offset = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * ShakeAmplitude;
            _shakeTarget.anchoredPosition = _shakeBase + new Vector2(offset, 0f);
        }

        private void StopShake()
        {
            if (_shaking == false)
                return;

            _shaking = false;
            if (_shakeTarget != null)
            {
                _shakeTarget.anchoredPosition = _shakeBase;
            }
        }
    }
}
