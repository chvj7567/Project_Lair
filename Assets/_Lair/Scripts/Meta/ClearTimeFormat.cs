using System;

namespace Lair.Meta
{
    //# 클리어 시간 표기 단일 소유 — 결과 팝업·마을 전적 패널이 공유한다. Unity 비의존 순수 함수.
    public static class ClearTimeFormat
    {
        //# "m:ss.d" — 소수 첫째 자리 내림(시안 3:42.1). 기록 없음(음수)은 "-".
        public static string WithTenths(float seconds)
        {
            if (seconds < 0f)
                return "-";
            int tenths = (int)Math.Floor(seconds * 10f);
            int total = tenths / 10;
            return $"{total / 60}:{total % 60:00}.{tenths % 10}";
        }
    }
}
