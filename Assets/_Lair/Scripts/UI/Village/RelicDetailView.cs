using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 유물 도감 상세 패널 — 내부 위젯을 private 소유하고 의도 API(Show/Hide)만 노출 (Rule 02 §6.1).
    public class RelicDetailView : MonoBehaviour
    {
        private static readonly Color SilhouetteColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        [SerializeField] private GameObject _content;         //# 선택이 있을 때만 켜지는 루트
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;
        [SerializeField] private Image _rarityChip;
        [SerializeField] private CHText _rarityText;
        [SerializeField] private CHText _categoryText;
        [SerializeField] private CHText _stackText;
        [SerializeField] private CHText _currentLabel;
        [SerializeField] private CHText _currentEffectText;
        [SerializeField] private GameObject _nextBlock;
        [SerializeField] private CHText _nextLabel;
        [SerializeField] private CHText _nextEffectText;
        [SerializeField] private CHText _maxLabel;
        [SerializeField] private CHText _dropHeroText;
        [SerializeField] private CHText _lockedText;

        public void Hide()
        {
            if (_content != null)
            {
                _content.SetActive(false);
            }
        }

        public void Show(RelicDetailData data)
        {
            if (data == null)
            {
                Hide();
                return;
            }
            if (_content != null)
            {
                _content.SetActive(true);
            }

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
                _icon.color = data.IsOwned ? Color.white : SilhouetteColor;
            }
            SetText(_nameText, data.NameText);
            if (_nameText != null)
            {
                _nameText.SetColor(data.IsOwned ? data.RarityColor : Color.white);
            }
            if (_rarityChip != null)
            {
                _rarityChip.color = data.RarityColor;
            }
            SetText(_rarityText, data.RarityText);
            if (_rarityText != null)
            {
                _rarityText.SetColor(data.RarityColor);
            }
            SetText(_categoryText, data.CategoryText);
            SetText(_dropHeroText, data.DropHeroText);

            //# 보유 블록 — 미보유면 전부 숨기고 안내 문구만.
            SetActive(_stackText, data.IsOwned);
            SetActive(_currentLabel, data.IsOwned);
            SetActive(_currentEffectText, data.IsOwned);
            SetActive(_lockedText, data.IsOwned == false);
            if (data.IsOwned)
            {
                SetText(_stackText, data.StackText);
                SetText(_currentLabel, data.CurrentLabelText);
                SetText(_currentEffectText, data.CurrentEffectText);
            }
            else
            {
                SetText(_lockedText, data.LockedText);
            }

            bool showNext = data.IsOwned && data.ShowNext;
            if (_nextBlock != null)
            {
                _nextBlock.SetActive(showNext);
            }
            if (showNext)
            {
                SetText(_nextLabel, data.NextLabelText);
                SetText(_nextEffectText, data.NextEffectText);
            }
            SetActive(_maxLabel, data.IsOwned && data.ShowMaxLabel);
            if (data.IsOwned && data.ShowMaxLabel)
            {
                SetText(_maxLabel, data.MaxLabelText);
            }
        }

        private static void SetText(CHText label, string text)
        {
            if (label != null)
            {
                label.SetText(text);
            }
        }

        private static void SetActive(CHText label, bool active)
        {
            if (label != null)
            {
                label.gameObject.SetActive(active);
            }
        }
    }
}
