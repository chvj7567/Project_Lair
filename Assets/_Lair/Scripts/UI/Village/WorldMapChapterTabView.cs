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
                _labelText.SetText(data.Label);
                _labelText.SetColor(data.IsPlaceholder ? PlaceholderTextColor : (data.IsLocked ? LockedTextColor : (data.IsSelected ? SelectedTextColor : NormalTextColor)));
            }
            if (_lockTagText != null)
            {
                _lockTagText.SetText(data.LockTagText);
                _lockTagText.gameObject.SetActive(string.IsNullOrEmpty(data.LockTagText) == false);
            }
            //# 미출시 슬롯은 눌러도 반응하지 않는다(정보 노출 없음). 잠금 탭은 눌러서 안내를 받는다.
            if (_button != null)
            {
                _button.Interactable = data.IsPlaceholder == false;
            }
        }
    }
}
