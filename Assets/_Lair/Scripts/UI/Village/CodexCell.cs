using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도감 셀 — 해금(컬러+이름) / 미조우(실루엣) / 잠금 더미 (기획서 §6).
    //# 몬스터 셀은 강화 레벨을 4채널(발광 오버레이·아이콘 틴트·스케일·레벨 배지)로 표현 (기획서 §1~5).
    public class CodexCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;        //# 카드 일러스트 또는 몬스터 색칩
        [SerializeField] private CHText _nameText;
        //# 강화 표현 위젯 (프리팹 배선 Task 4). 미배선이어도 null 가드로 안전.
        [SerializeField] private CHText _levelBadge;        //# 우상단 "Lv N" 배지 (§5)
        [SerializeField] private Image _glowOverlay;        //# 아이콘 뒤 종족색 발광 아우라 (§3)
        [SerializeField] private RectTransform _iconRect;   //# 스케일 대상 = _icon 의 RectTransform (§4)

        //# 레벨(0~5) → 시각 매핑 상수 — 기획서 §9 SoT. index = 강화 레벨.
        //# public static: 테스트(별도 어셈블리)에서 배열 핀 검증 + 런타임 Mathf.Pow 불필요.
        public static readonly float[] IconTintByLevel        = { 0.00f, 0.16f, 0.28f, 0.40f, 0.52f, 0.64f };
        public static readonly float[] GlowOverlayAlphaByLevel = { 0.00f, 0.25f, 0.42f, 0.58f, 0.74f, 0.90f };
        public static readonly float[] ScaleByLevel           = { 1.00f, 1.015f, 1.03f, 1.05f, 1.07f, 1.10f };

        private const int MaxLevel = 5;

        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color SilhouetteColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        public void Bind(CodexCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                _background.color = data.IsLockedDummy ? DummyBg : NormalBg;
            }

            bool showIcon = data.IsLockedDummy == false;
            if (_icon != null)
            {
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

            //# 강화 4채널 — 매 재사용마다 전부 재설정(풀 재사용 잔상 방지, §9).
            ApplyEnhancement(data, showIcon);
        }

        //# 해금된 몬스터 셀만 강화 4채널 적용. 그 외(카드·더미·미해금)는 전부 리셋 = "담백한 원본"(§6).
        private void ApplyEnhancement(CodexCellData data, bool showIcon)
        {
            bool enhanced = data.Species.HasValue && data.Unlocked;
            int lv = enhanced ? Mathf.Clamp(data.EnhanceLevel, 0, MaxLevel) : 0;
            bool lit = enhanced && lv > 0;

            if (_levelBadge != null)
            {
                _levelBadge.gameObject.SetActive(lit);
                if (lit)
                {
                    _levelBadge.SetText($"Lv {lv}");
                }
            }

            if (_glowOverlay != null)
            {
                _glowOverlay.gameObject.SetActive(lit);
                if (lit)
                {
                    Color glow = SpeciesVisual.SpeciesGlowColor(data.Species.Value);
                    glow.a = GlowOverlayAlphaByLevel[lv];
                    _glowOverlay.color = glow;
                }
            }

            //# 아이콘 틴트(hue wash) — lv>0 일 때만 종족색으로 Lerp. 그 외는 위 기본 색 규칙 유지.
            if (lit && showIcon && _icon != null)
            {
                Color glow = SpeciesVisual.SpeciesGlowColor(data.Species.Value);
                _icon.color = Color.Lerp(Color.white, glow, IconTintByLevel[lv]);
            }

            if (_iconRect != null)
            {
                _iconRect.localScale = Vector3.one * (enhanced ? ScaleByLevel[lv] : 1f);
            }
        }
    }
}
