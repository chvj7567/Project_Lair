using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 강화 레벨(0~5) → 4채널 시각(발광 오버레이·아이콘 틴트·스케일·"Lv N" 배지) 공유 SoT.
    //# 도감(CodexCell)·인게임 상태 셀(SpawnerStatusCell)이 같은 코드를 호출 — 표현 drift 방지 (기획서 §2·§6).
    public static class EnhanceLevelVisual
    {
        //# 레벨(0~5) → 시각 매핑 상수 — 기획서 §6 SoT. index = 강화 레벨. Lv0 = 항등.
        //# public static: 테스트(별도 어셈블리) 배열 핀 + 런타임 Mathf.Pow 불필요.
        public static readonly float[] IconTintByLevel        = { 0.00f, 0.16f, 0.28f, 0.40f, 0.52f, 0.64f };
        public static readonly float[] GlowOverlayAlphaByLevel = { 0.00f, 0.25f, 0.42f, 0.58f, 0.74f, 0.90f };
        public static readonly float[] ScaleByLevel           = { 1.00f, 1.015f, 1.03f, 1.05f, 1.07f, 1.10f };

        public const int MaxLevel = 5;

        //# 4채널 적용. level 은 내부에서 Clamp(0..MaxLevel) — 변조 세이브(99/음수)도 인덱싱 안전.
        //# lv≤0 이면 전부 항등(오버레이/배지 off·스케일 1·icon.color = baseIconColor). 각 위젯 null 가드.
        public static void Apply(int level, EMonster species, Image icon, Image glowOverlay,
            CHText levelBadge, RectTransform iconRect, Color baseIconColor)
        {
            int lv = Mathf.Clamp(level, 0, MaxLevel);
            bool lit = lv > 0;
            Color glow = SpeciesVisual.SpeciesGlowColor(species);

            if (levelBadge != null)
            {
                levelBadge.gameObject.SetActive(lit);
                if (lit)
                {
                    levelBadge.SetText($"Lv {lv}");
                }
            }

            if (glowOverlay != null)
            {
                glowOverlay.gameObject.SetActive(lit);
                if (lit)
                {
                    Color c = glow;
                    c.a = GlowOverlayAlphaByLevel[lv];
                    glowOverlay.color = c;
                }
            }

            if (icon != null)
            {
                //# 틴트 base 는 항상 흰색(도감 원본 동작 불변, 기획서 §11) — baseIconColor 로 바꾸지 말 것.
                //# baseIconColor 는 lv0/미강화의 "쉬는 색"(도감 실루엣·색칩, 상태 셀 white)으로만 쓴다.
                icon.color = lit ? Color.Lerp(Color.white, glow, IconTintByLevel[lv]) : baseIconColor;
            }

            if (iconRect != null)
            {
                iconRect.localScale = Vector3.one * ScaleByLevel[lv];
            }
        }
    }
}
