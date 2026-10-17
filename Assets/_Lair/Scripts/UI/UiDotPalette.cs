using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# 도트 던전 UI 팔레트 중 코드에서 tint 로 쓰는 색 — 기획서 ui-dot-dungeon-redesign §2.2 토큰과 동일.
    public static class UiDotPalette
    {
        public static readonly Color Soul = new Color32(0x5E, 0xF0, 0xB4, 0xFF);      //# 패시브·소울·확정
        public static readonly Color Gold = new Color32(0xF7, 0xC6, 0x4A, 0xFF);      //# 액티브·XP·보상
        public static readonly Color Blood = new Color32(0xE5, 0x48, 0x4D, 0xFF);     //# 오류·적 HP
        public static readonly Color Stone4 = new Color32(0x48, 0x54, 0x6E, 0xFF);    //# 비활성 점·스크롤 핸들
        public static readonly Color TimerWarn = new Color32(0xFF, 0x8A, 0x8D, 0xFF); //# 타이머 30초 이하 붉은 글씨

        //# 카드 종류 색 — 패시브=소울, 액티브=금.
        public static Color CardKind(bool isPassive)
        {
            return isPassive ? Soul : Gold;
        }

        //# 토스트 점 색 — 정보=소울, 경고=금, 오류=피.
        public static Color ToastDot(EToastKind kind)
        {
            switch (kind)
            {
                case EToastKind.Warning:
                    return Gold;
                case EToastKind.Error:
                    return Blood;
                default:
                    return Soul;
            }
        }
    }
}
