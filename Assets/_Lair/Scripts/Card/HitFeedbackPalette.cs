using UnityEngine;

namespace Lair.Card
{
    //# DoT(독·출혈) 데미지 숫자 색 단일 출처. FX 프리팹(PoisonAura/BleedStatus) 색과 동일 값 유지.
    //# 기획서 §8.4 확정값 — 빌더는 동일 hex(아래 주석) 를 참조해 .mat 에 적용한다.
    public static class HitFeedbackPalette
    {
        //# 독 — 짙은 틸 #0B5B4A (167°/88%/36%). Wisp 녹색·Phantom 검정과 hue+채도 분리.
        public static readonly Color Poison = new Color(0.043f, 0.357f, 0.290f, 1f);

        //# 출혈 — 짙은 자홍 #A01346 (339°/88%/63%). 다크레드 군집과 마젠타 lean(hue 21°) 분리.
        public static readonly Color Bleed = new Color(0.627f, 0.075f, 0.275f, 1f);
    }
}
