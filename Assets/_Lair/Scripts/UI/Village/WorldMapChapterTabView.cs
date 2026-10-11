using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 월드맵 챕터 탭 1칸 — 표시 + 클릭 수신만 (Rule 02 §6). 위젯은 [SerializeField] private, 의도 API 만 노출(§6.1).
    public class WorldMapChapterTabView : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private CHText _labelText;      //# "1장 지하 묘지" / "3장 ???"
        [SerializeField] private CHText _lockTagText;    //# "잠금" / "준비 중"
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private Image _numberBadge;
        [SerializeField] private CHText _numberText;
        [SerializeField] private CHText _heroText;
        [SerializeField] private GameObject _chipsRoot;
        [SerializeField] private Image[] _chips = new Image[0];

        private const float NormalLabelY = 10f;
        private const float PlaceholderLabelY = 30f;

        private static readonly Color ChipOnColor = new Color32(0xf7, 0xc6, 0x4a, 0xff);
        private static readonly Color ChipOffColor = new Color32(0x0c, 0x10, 0x17, 0xff);
        private static readonly Color LockedBadgeColor = new Color32(0x1b, 0x1f, 0x27, 0xff);
        private static readonly Color LockedNumberColor = new Color32(0x59, 0x61, 0x7a, 0xff);
        private static readonly Color PlaceholderBadgeColor = new Color32(0x17, 0x1b, 0x25, 0xff);
        private static readonly Color PlaceholderNumberColor = new Color32(0x3c, 0x44, 0x58, 0xff);
        private static readonly Color HeroLockedColor = new Color32(0x6c, 0x73, 0x85, 0xff);

        private static readonly Color LockedTextColor = new Color(0.424f, 0.451f, 0.522f, 1f);
        private static readonly Color PlaceholderTextColor = new Color(0.290f, 0.322f, 0.400f, 1f);
        private static readonly Color SelectedTextColor = new Color(0.910f, 0.882f, 0.812f, 1f);
        private static readonly Color NormalTextColor = new Color(0.557f, 0.596f, 0.678f, 1f);

        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        public void Show(WorldMapTabData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                Sprite sprite = data.IsSelected ? _selectedSprite : (data.IsLocked ? _lockedSprite : _normalSprite);
                if (sprite != null)
                {
                    _background.sprite = sprite;
                }
            }
            if (_labelText != null)
            {
                //# 미출시 슬롯은 영웅·태그가 없어 챕터명을 카드 중앙 쪽(y=30)으로 내린다.
                RectTransform labelRect = _labelText.transform as RectTransform;
                if (labelRect != null)
                {
                    Vector2 labelPos = labelRect.anchoredPosition;
                    labelPos.y = data.IsPlaceholder ? -PlaceholderLabelY : -NormalLabelY;
                    labelRect.anchoredPosition = labelPos;
                }
                _labelText.SetText(data.ChapterNameText);
                _labelText.SetColor(data.IsPlaceholder ? PlaceholderTextColor : (data.IsLocked ? LockedTextColor : (data.IsSelected ? SelectedTextColor : NormalTextColor)));
            }
            if (_lockTagText != null)
            {
                _lockTagText.SetText(data.LockTagText);
                _lockTagText.gameObject.SetActive(string.IsNullOrEmpty(data.LockTagText) == false);
            }
            ShowBadge(data);
            ShowHero(data);
            ShowChips(data);
            //# 미출시 슬롯은 눌러도 반응하지 않는다(정보 노출 없음). 잠금 탭은 눌러서 안내를 받는다.
            if (_button != null)
            {
                _button.Interactable = data.IsPlaceholder == false;
            }
        }

        //# 번호 배지 — 개방/선택은 챕터 Accent, 잠금·미출시는 어두운 고정색.
        private void ShowBadge(WorldMapTabData data)
        {
            Color badge = WorldMapChapterPalette.Accent(data.ChapterId);
            Color number = Color.white;
            if (data.IsPlaceholder)
            {
                badge = PlaceholderBadgeColor;
                number = PlaceholderNumberColor;
            }
            else if (data.IsLocked)
            {
                badge = LockedBadgeColor;
                number = LockedNumberColor;
            }

            if (_numberBadge != null)
            {
                _numberBadge.color = badge;
            }
            if (_numberText != null)
            {
                _numberText.SetText(data.ChapterId.ToString());
                _numberText.SetColor(number);
            }
        }

        private void ShowHero(WorldMapTabData data)
        {
            if (_heroText == null)
                return;

            bool visible = data.IsPlaceholder == false && string.IsNullOrEmpty(data.HeroText) == false;
            _heroText.gameObject.SetActive(visible);
            _heroText.SetText(data.HeroText ?? string.Empty);
            _heroText.SetColor(data.IsLocked ? HeroLockedColor : NormalTextColor);
        }

        //# 진행 칩 — 개방(IsLocked == false)일 때만 표시, 앞에서부터 ClearedCount 칸을 켠다.
        private void ShowChips(WorldMapTabData data)
        {
            if (_chipsRoot != null)
            {
                _chipsRoot.SetActive(data.IsLocked == false);
            }
            for (int i = 0; i < _chips.Length; ++i)
            {
                if (_chips[i] != null)
                {
                    _chips[i].color = i < data.ClearedCount ? ChipOnColor : ChipOffColor;
                }
            }
        }
    }
}
