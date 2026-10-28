using System;
using System.Collections.Generic;
using System.Text;
using Lair.Meta;

namespace Lair.Net
{
    //# 데일리 리더보드(dailyLeaderboard/{day}_{uid}) 규약 — Firebase 타입 비의존 순수 헬퍼라 게이트 밖에 둔다 (daily-challenge §6.3).
    //# 문서 필드: uid / day / displayName / hero / score / comboKey / updatedAt. 보안 규칙·복합 인덱스(day, score)는 Firebase 콘솔 소관(C-1~C-3).
    public static class DailyLeaderboard
    {
        public const string CollectionName = "dailyLeaderboard";
        public const string ScoreField = "score";
        public const string DayField = "day";

        public static string DocId(int day, string uid)
        {
            return $"{day}_{uid}";
        }

        //# 새 점수를 쓸지 — 기존이 없거나(<=0) 더 작을 때만, 동점 미갱신 (§6.2, StageLeaderboard.ShouldReplace 와 같은 규칙).
        public static bool ShouldReplace(long existingScore, int newScore)
        {
            if (DailyRules.IsValidScore(newScore) == false)
                return false;
            return existingScore <= 0 || newScore < existingScore;
        }

        //# 오름차순 점수 → 경쟁 순위(1,2,2,4). 내 순위 조회 "나보다 작은 점수 수 + 1" 과 같은 규칙이라 목록과 내 행 순위가 일치한다.
        public static long[] AssignRanks(IReadOnlyList<int> sortedScores)
        {
            long[] ranks = new long[sortedScores.Count];
            for (int i = 0; i < ranks.Length; ++i)
            {
                ranks[i] = i > 0 && sortedScores[i] == sortedScores[i - 1] ? ranks[i - 1] : i + 1;
            }
            return ranks;
        }
    }

    //# JWT 페이로드의 iat(발급 시각, 초) 디코드 (§2.2 대안 B) — 서명 검증은 하지 않는다(서버가 방금 발급한 토큰의 시각만 읽는다).
    public static class JwtClaims
    {
        public static bool TryReadIatMs(string jwt, out long utcMs)
        {
            utcMs = 0L;
            if (string.IsNullOrEmpty(jwt))
                return false;
            string[] parts = jwt.Split('.');
            if (parts.Length < 2)
                return false;
            string payload;
            try
            {
                payload = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            }
            catch (FormatException)
            {
                return false;
            }
            if (TryReadNumber(payload, "iat", out long seconds) == false || seconds <= 0L)
                return false;
            utcMs = seconds * 1000L;
            return true;
        }

        private static byte[] DecodeBase64Url(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            int pad = base64.Length % 4;
            if (pad == 2)
            {
                base64 += "==";
            }
            else if (pad == 3)
            {
                base64 += "=";
            }
            return Convert.FromBase64String(base64);
        }

        //# 평탄한 JSON 에서 "key": 정수 를 읽는다 — JWT 클레임은 최상위 숫자라 전체 파서가 필요 없다.
        private static bool TryReadNumber(string json, string key, out long value)
        {
            value = 0L;
            string token = "\"" + key + "\"";
            int index = json.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
                return false;
            int i = index + token.Length;
            while (i < json.Length && (json[i] == ' ' || json[i] == ':'))
            {
                ++i;
            }
            int start = i;
            while (i < json.Length && char.IsDigit(json[i]))
            {
                ++i;
            }
            if (i == start)
                return false;
            return long.TryParse(json.Substring(start, i - start), out value);
        }
    }
}
