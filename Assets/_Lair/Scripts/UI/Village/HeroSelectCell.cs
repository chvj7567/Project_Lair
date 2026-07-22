using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 영웅 목록 셀 — 스테이지별 영웅 외형 표시 전용. 해금은 스테이지 틴트 초상, 잠금은 어두운 셀 + 어두운 초상.
    public class HeroSelectCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;          //# 미사용 테두리 — 표시 전용 목록이라 항상 투명 처리
        [SerializeField] private Image _portrait;        //# 영웅 초상 — 해금/잠금 모두 표시(잠금은 어두운 틴트), sprite 없을 때만 숨김
        [SerializeField] private CHText _nameText;

        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color HiddenBorder = new Color(0f, 0f, 0f, 0f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        public void Bind(HeroSelectCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                _background.color = data.IsLocked ? DummyBg : NormalBg;
            }
            if (_border != null)
            {
                //# 표시 전용 목록 — 선택 강조 없음(스테이지 선택은 마을 캐러셀 담당).
                _border.color = HiddenBorder;
            }
            SetPortrait(data.Portrait, data.PortraitTint);
            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
                _nameText.SetColor(data.IsLocked ? DummyTextColor : Color.white);
            }
        }

        //# 초상 의도 API (Rule 02 §6.1) — sprite + 틴트를 함께 받아 매 Bind 마다 리셋(풀 재사용 시 이전 스테이지 색 잔존 방지).
        public void SetPortrait(Sprite sprite, Color tint)
        {
            if (_portrait == null)
                return;
            bool show = sprite != null;
            _portrait.gameObject.SetActive(show);
            if (show == false)
                return;
            _portrait.sprite = sprite;
            _portrait.color = tint;
        }
    }
}
