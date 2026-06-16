using System.Collections.Generic;
using NUnit.Framework;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    public class RankingClientTests
    {
        [Test]
        public void 제출은_클리어타임을_그대로_전달한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SubmitResult = true };
            RankingClient client = new RankingClient(fake);

            bool ok = client.SubmitAsync(120000, "Knight", "Alice").GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.AreEqual(120000, fake.LastSubmittedMs);
        }

        [Test]
        public void Top조회는_서버목록을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient
            {
                TopToReturn = new List<RankingRowDto> { new RankingRowDto { rank = 1, displayName = "Bob", clearTimeMs = 60000, hero = "Knight" } },
            };
            RankingClient client = new RankingClient(fake);

            List<RankingRowDto> rows = client.GetTopAsync(100).GetAwaiter().GetResult();

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Bob", rows[0].displayName);
        }

        [Test]
        public void 제출은_표시명을_그대로_전달한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SubmitResult = true };
            RankingClient client = new RankingClient(fake);

            client.SubmitAsync(90000, "Knight", "영주 #A3F9").GetAwaiter().GetResult();

            Assert.AreEqual("영주 #A3F9", fake.LastSubmittedName);
        }
    }
}
