using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 영웅 선택 셀 — 선택 가능(현재 선택 강조) / 잠금 더미(어두운 셀 + ??? — 클릭 무반응, 기획서 §6).
    public class HeroSelectCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;          //# 선택 강조 테두리
        [SerializeField] private Image _portrait;        //# 영웅 초상 — 잠금 더미/초상 미할당 시 숨김 (기존 표시 유지)
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHButton _selectButton;

        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color SelectedBorder = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color HiddenBorder = new Color(0f, 0f, 0f, 0f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        private HeroSelectCellData _data;
        private bool _wired;

        public void Bind(HeroSelectCellData data)
        {
            if (data == null)
                return;
            _data = data;
            WireOnce();

            if (_background != null)
            {
                _background.color = data.IsLocked ? DummyBg : NormalBg;
            }
            if (_border != null)
            {
                _border.color = data.IsSelected ? SelectedBorder : HiddenBorder;
            }
            if (_portrait != null)
            {
                //# 풀 재사용 셀 — 매 Bind 마다 표시/숨김 갱신 (이전 데이터 잔존 방지).
                bool showPortrait = data.IsLocked == false && data.Portrait != null;
                _portrait.gameObject.SetActive(showPortrait);
                if (showPortrait)
                {
                    _portrait.sprite = data.Portrait;
                }
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
                _nameText.SetColor(data.IsLocked ? DummyTextColor : Color.white);
            }
            if (_selectButton != null)
            {
                _selectButton.Interactable = data.IsLocked == false;
            }
        }

        //# CHButton.OnClick 은 listener 누적 — 풀 재사용 셀은 1회만 등록.
        private void WireOnce()
        {
            if (_wired)
                return;
            _wired = true;
            if (_selectButton != null)
            {
                _selectButton.OnClick(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (_data == null || _data.IsLocked)
                return;
            _data.OnSelect?.Invoke(_data.Hero);
        }
    }
}
