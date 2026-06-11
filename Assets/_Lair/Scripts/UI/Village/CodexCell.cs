using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도감 셀 — 해금(컬러+이름) / 미조우(실루엣) / 잠금 더미 (기획서 §6).
    public class CodexCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;        //# 카드 일러스트 또는 몬스터 색칩
        [SerializeField] private CHText _nameText;

        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color SilhouetteColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        public void Bind(CodexCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
                _background.color = data.IsLockedDummy ? DummyBg : NormalBg;

            if (_icon != null)
            {
                bool showIcon = data.IsLockedDummy == false;
                _icon.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    if (data.Icon != null)
                    {
                        _icon.sprite = data.Icon;
                        //# 미해금 카드 — 검정 실루엣 (기획서 §6 미조우 실루엣 규칙).
                        _icon.color = data.Unlocked ? Color.white : SilhouetteColor;
                    }
                    else
                    {
                        //# 몬스터 — 종 색칩. 미조우면 실루엣 톤.
                        _icon.sprite = null;
                        _icon.color = data.Unlocked ? data.TintColor : SilhouetteColor;
                    }
                }
            }

            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
                _nameText.SetColor(data.Unlocked ? Color.white : DummyTextColor);
            }
        }
    }
}
