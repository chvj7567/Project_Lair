using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ChvjUnityInfra;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# LairApiClient.ParseRows(private static) 단위 검증 — JsonUtility 최상위 배열 한계를 래퍼로 회피하는지(기획서 §4).
    //# 리플렉션 직접 호출(ParseRows 는 CHHttpResult 만 받는 순수 함수라 네트워크 없이 검증 가능).
    public class LairApiClientParseRowsTests
    {
        private static List<RankingRowDto> ParseRows(CHHttpResult res)
        {
            MethodInfo m = typeof(LairApiClient).GetMethod("ParseRows",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "ParseRows 메서드 미발견 — 시그니처 변경 시 테스트 갱신 필요");
            return (List<RankingRowDto>)m.Invoke(null, new object[] { res });
        }

        private static CHHttpResult Success(string body)
            => new CHHttpResult { IsSuccess = true, StatusCode = 200, Body = body };

        [Test]
        public void ParseRows_정상_최상위배열을_파싱한다()
        {
            string json = "[{\"rank\":1,\"displayName\":\"Bob\",\"clearTimeMs\":60000,\"hero\":\"Knight\"}," +
                          "{\"rank\":2,\"displayName\":\"Amy\",\"clearTimeMs\":75000,\"hero\":\"Mage\"}]";

            List<RankingRowDto> rows = ParseRows(Success(json));

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(1, rows[0].rank);
            Assert.AreEqual("Bob", rows[0].displayName);
            Assert.AreEqual(60000, rows[0].clearTimeMs);
            Assert.AreEqual("Mage", rows[1].hero);
        }

        [Test]
        public void ParseRows_빈배열이면_빈리스트를_반환한다()
        {
            List<RankingRowDto> rows = ParseRows(Success("[]"));

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_실패응답이면_빈리스트를_반환한다()
        {
            CHHttpResult fail = new CHHttpResult { IsSuccess = false, StatusCode = 500, Body = "[]" };

            List<RankingRowDto> rows = ParseRows(fail);

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_본문이_null이면_빈리스트를_반환한다()
        {
            List<RankingRowDto> rows = ParseRows(Success(null));

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_본문이_빈문자열이면_빈리스트를_반환한다()
        {
            List<RankingRowDto> rows = ParseRows(Success(string.Empty));

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_깨진JSON본문이면_빈리스트를_반환한다()
        {
            //# robustness — malformed body 를 try/catch 로 감싸 빈 리스트 fallback(기획서 §6 흐름 차단 금지).
            //# (이전엔 JsonUtility 예외가 호출부로 전파됐으나 best-effort 원칙에 맞춰 동작 변경.)
            List<RankingRowDto> rows = ParseRows(Success("not-json"));

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_빈JSON객체본문이면_빈리스트를_반환한다()
        {
            //# "{}" 는 유효 JSON 이지만 rows 필드가 없어 null → 빈 리스트 fallback (예외 없음).
            List<RankingRowDto> rows = ParseRows(Success("{}"));

            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseRows_단일행배열도_파싱한다()
        {
            string json = "[{\"rank\":42,\"displayName\":\"Solo\",\"clearTimeMs\":120000,\"hero\":\"Knight\"}]";

            List<RankingRowDto> rows = ParseRows(Success(json));

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(42, rows[0].rank);
            Assert.AreEqual("Solo", rows[0].displayName);
        }
    }
}
