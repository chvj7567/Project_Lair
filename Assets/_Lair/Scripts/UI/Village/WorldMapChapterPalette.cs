using UnityEngine;

namespace Lair.UI
{
    //# 월드맵 챕터 색조 표(단일 진실) — 카드 번호 배지·지도 배경·상세 배너가 함께 참조한다.
    public static class WorldMapChapterPalette
    {
        public const float BannerTintAlpha = 0.53f;
        public const float NodeOpenLightness = 0.30f;
        public const float NodeGlowAlpha = 0.45f;
        public const float SilhouetteDarken = 0.30f;
        public const float SilhouetteAlpha = 0.80f;

        private static readonly Color[] Accents =
        {
            new Color32(0x4a, 0x6f, 0xa5, 0xff),
            new Color32(0x2f, 0x8a, 0x5f, 0xff),
            new Color32(0x7a, 0x55, 0xc8, 0xff),
        };

        private static readonly Color[] MapBackgrounds =
        {
            new Color32(0x14, 0x1b, 0x2c, 0xff),
            new Color32(0x0e, 0x22, 0x16, 0xff),
            new Color32(0x1b, 0x11, 0x2d, 0xff),
        };

        private static readonly Color NeutralAccent = new Color32(0x2c, 0x37, 0x50, 0xff);
        private static readonly Color NeutralMapBackground = new Color32(0x0d, 0x11, 0x19, 0xff);

        public static Color Accent(int chapterId)
        {
            return chapterId >= 1 && chapterId <= Accents.Length ? Accents[chapterId - 1] : NeutralAccent;
        }

        public static Color MapBackground(int chapterId)
        {
            return chapterId >= 1 && chapterId <= MapBackgrounds.Length ? MapBackgrounds[chapterId - 1] : NeutralMapBackground;
        }

        public static Color BannerTint(int chapterId)
        {
            Color color = Accent(chapterId);
            color.a = BannerTintAlpha;
            return color;
        }

        //# 열림(일반) 노드 메달 틴트 — 회색조 스프라이트에 곱해지므로 Accent 를 미리 밝힌다.
        public static Color NodeOpenTint(int chapterId)
        {
            Color color = Color.Lerp(Accent(chapterId), Color.white, NodeOpenLightness);
            color.a = 1f;
            return color;
        }

        public static Color NodeGlow(int chapterId)
        {
            Color color = Accent(chapterId);
            color.a = NodeGlowAlpha;
            return color;
        }

        public static Color Silhouette(int chapterId)
        {
            Color color = MapBackground(chapterId);
            return new Color(color.r * SilhouetteDarken, color.g * SilhouetteDarken, color.b * SilhouetteDarken, SilhouetteAlpha);
        }
    }
}
