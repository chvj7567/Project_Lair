using System;
using System.Globalization;
using Lair.Data;

namespace Lair.Meta
{
    //# 데일리 시작 판정 결과 (daily-challenge §2.2·§10.1) — 팝업 "도전 시작" 과 결과창 "다시 도전" 이 같은 판정을 쓴다.
    public enum EDailyStartCheck
    {
        Ok,
        Disabled,     //# DailyConfig 미로드
        Locked,       //# 해금 조건 미달
        Offline,      //# 이번 세션에 서버 시각을 받지 못함
        DayChanged,   //# 조합을 받은 날과 지금 날짜가 다름
        Exhausted,    //# 남은 도전 0
    }

    //# 데일리 순수 규칙 — 날짜·점수·연속 배율·보너스·시작 판정 (daily-challenge §2·§6·§7). Unity 비의존.
    public static class DailyRules
    {
        public const int WinScoreMax = 300000;
        public const int LoseScoreBase = 300000;
        public const int ScoreMax = 301000;
        public const long MsPerDay = 86400000L;
        //# 보관 정리 — 오늘 포함 최근 7일 (§4.3).
        public const int KeepDays = 7;

        //# dayIndex = floor((utcMs + offset x 60000) / 1일) — 음수 시각도 내림 (§2.1).
        public static int DayIndexOf(long utcMs, int utcOffsetMinutes)
        {
            long local = utcMs + utcOffsetMinutes * 60000L;
            long day = local / MsPerDay;
            if (local % MsPerDay != 0 && local < 0)
            {
                day -= 1;
            }
            return (int)day;
        }

        //# dayIndex → 그 날짜의 월·일 (1970-01-01 = 0, 시간대는 이미 반영된 값).
        public static void MonthDayOf(int dayIndex, out int month, out int dayOfMonth)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(Math.Max(0, dayIndex));
            month = date.Month;
            dayOfMonth = date.Day;
        }

        //# 다음 0시까지 남은 ms (팝업 "초기화까지").
        public static long MsUntilNextDay(long utcMs, int utcOffsetMinutes)
        {
            long nextStart = (DayIndexOf(utcMs, utcOffsetMinutes) + 1L) * MsPerDay - utcOffsetMinutes * 60000L;
            return Math.Max(0L, nextStart - utcMs);
        }

        //# 승리 점수 = 클리어 ms (1~300000). NaN/0 이하 경과는 1 로(점수 0 은 존재하지 않는다).
        public static int WinScore(float elapsedSec)
        {
            if (elapsedSec > 0f == false)
                return 1;
            double ms = Math.Round(elapsedSec * 1000.0);
            return (int)Math.Max(1.0, Math.Min(WinScoreMax, ms));
        }

        //# 패배 점수 = 300000 + ceil(남은 HP / 최대 HP x 1000), 천분율 1~1000 클램프 (§6.1). 정수 연산이라 부동소수 오차 없음.
        public static int LoseScore(int heroHp, int heroMaxHp)
        {
            if (heroMaxHp <= 0)
                return ScoreMax;
            long hp = Math.Max(0, heroHp);
            long permille = (hp * 1000L + heroMaxHp - 1) / heroMaxHp;
            permille = Math.Max(1L, Math.Min(1000L, permille));
            return LoseScoreBase + (int)permille;
        }

        public static bool IsWinScore(int score) => score >= 1 && score <= WinScoreMax;

        public static bool IsValidScore(int score) => score >= 1 && score <= ScoreMax;

        //# 패배 점수의 남은 HP % (천분율 ÷ 10 반올림).
        public static int LosePercent(int score)
        {
            return (int)Math.Round((score - LoseScoreBase) / 10.0, MidpointRounding.AwayFromZero);
        }

        //# 연속 배율 = 1 + perDay x (Min(streak, maxDays) - 1). streak 은 1 이상으로 본다 (§7.1).
        public static decimal StreakMultiplier(int streak, DailyConfigRow config)
        {
            int days = Math.Min(Math.Max(1, streak), Math.Max(1, config.streakBonusMaxDays));
            return 1m + ToDecimal(config.streakBonusPerDay) * (days - 1);
        }

        //# 첫 승리 보너스 = floor(firstWinSouls x 연속 배율) — decimal 이라 150 x 1.3 = 195 가 194 로 내려가지 않는다.
        public static int BonusSouls(int streak, DailyConfigRow config)
        {
            return (int)Math.Floor(Math.Max(0, config.firstWinSouls) * StreakMultiplier(streak, config));
        }

        //# "1.3" / "1" — 배너 배율 표기.
        public static string FormatMultiplier(decimal multiplier)
        {
            return multiplier.ToString("0.##", CultureInfo.InvariantCulture);
        }

        //# float 설정값을 소수 4자리로 확정 — (decimal)0.1f 의 표현 오차를 배율에 싣지 않는다.
        private static decimal ToDecimal(float value)
        {
            if (value > 0f == false)
                return 0m;
            return Math.Round((decimal)value, 4);
        }

        //# 시작/재도전 판정 — 서버 호출 없이 세션 기준값(clock) 만으로 판정한다 (§8 오프라인 Retry 허용).
        //# contextDay: 이미 받은 조합의 날짜. 지금 날짜와 다르면 DayChanged.
        public static EDailyStartCheck CheckStart(MetaProfile profile, DailyDatabase db, DailyClock clock, int contextDay)
        {
            if (db == null)
                return EDailyStartCheck.Disabled;
            if (profile == null || db.IsUnlocked(profile.ClearedStage) == false)
                return EDailyStartCheck.Locked;
            if (clock == null)
                return EDailyStartCheck.Offline;
            int today = clock.Today(db.Config.utcOffsetMinutes);
            if (today != contextDay)
                return EDailyStartCheck.DayChanged;
            if (profile.DailyAttemptsLeft(today, db.Config.attemptsPerDay) <= 0)
                return EDailyStartCheck.Exhausted;
            return EDailyStartCheck.Ok;
        }
    }

    //# 세션 서버 시각 — 팝업이 받은 서버 UTC ms 를 기준으로 단조 시계로만 전진한다. 기기 시계(DateTime.Now)는 쓰지 않는다 (§2.2).
    public sealed class DailyClock
    {
        private readonly long _serverUtcMsAtFetch;
        private readonly double _realtimeAtFetch;
        private readonly Func<double> _realtime;

        //# realtime: 단조 시계(초) — 런타임은 Time.realtimeSinceStartupAsDouble, 테스트는 가짜 주입.
        public DailyClock(long serverUtcMs, Func<double> realtime)
        {
            _serverUtcMsAtFetch = serverUtcMs;
            _realtime = realtime ?? (() => 0.0);
            _realtimeAtFetch = _realtime();
        }

        public long NowUtcMs
        {
            get
            {
                double elapsed = _realtime() - _realtimeAtFetch;
                if (elapsed > 0.0 == false)
                    return _serverUtcMsAtFetch;
                return _serverUtcMsAtFetch + (long)Math.Floor(elapsed * 1000.0);
            }
        }

        public int Today(int utcOffsetMinutes) => DailyRules.DayIndexOf(NowUtcMs, utcOffsetMinutes);
    }
}
