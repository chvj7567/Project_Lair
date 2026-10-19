using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 유물 도감 그리드 셀 — 표시·입력만 (Rule 02 §6). 미획득은 실루엣 + ???, 최대 중첩은 MAX 라벨.
    public class RelicCell : MonoBehaviour
    {
        //# 미획득 아이콘 밝기 0.15 (relics §7.2).
        private static readonly Color SilhouetteColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        [SerializeField] private Image _border;           //# 등급 색 3px 테두리
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _stackText;       //# 우하단 "n/m"
        [SerializeField] private GameObject _maxBadge;    //# 우상단 "MAX"
        [SerializeField] private GameObject _selectedMark;
        [SerializeField] private CHButton _button;

        private RelicCellData _data;
        private bool _clickRegistered;

        //# 풀 재사용 — 이전 바인딩 잔존 방지 (Rule 03 §4).
        private void OnEnable()
        {
            _data = null;
        }

        public void Bind(RelicCellData data)
        {
            if (data == null)
                return;
            _data = data;

            if (_border != null)
            {
                _border.color = data.BorderColor;
            }
            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
                _icon.color = data.IsOwned ? Color.white : SilhouetteColor;
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.NameText);
            }
            if (_stackText != null)
            {
                _stackText.gameObject.SetActive(data.IsOwned);
                _stackText.SetText(data.StackText);
            }
            if (_maxBadge != null)
            {
                _maxBadge.SetActive(data.IsMax);
            }
            if (_selectedMark != null)
            {
                _selectedMark.SetActive(data.IsSelected);
            }
            //# CHButton.OnClick 은 listener 누적 — 풀 재사용 셀은 1회만 등록하고 현재 _data 로 위임.
            if (_clickRegistered == false && _button != null)
            {
                _clickRegistered = true;
                _button.OnClick(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (_data != null)
            {
                _data.OnSelect?.Invoke(_data.RelicId);
            }
        }
    }
}
