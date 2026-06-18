using NUnit.Framework;
using ChvjUnityInfra;
using Lair.Meta;
using Lair.Net;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class NetDtoMappingTests
    {
        [Test]
        public void MetaProfile_을_JsonUtility로_왕복하면_보존된다()
        {
            MetaProfile profile = new MetaProfile { Souls = 42, LordXp = 7, SelectedHero = "Knight", BestClearTime = 123.5f };
            profile.SetShopLevel("HpUp", 3);
            profile.AddDistinct(profile.AchievedIds, "FirstWin");
            profile.AddDistinct(profile.SeenMonsters, "Wisp");

            string json = JsonUtility.ToJson(profile);
            MetaProfile back = JsonUtility.FromJson<MetaProfile>(json);

            Assert.AreEqual(42, back.Souls);
            Assert.AreEqual(3, back.GetShopLevel("HpUp"));
            Assert.Contains("FirstWin", back.AchievedIds);
            Assert.Contains("Wisp", back.SeenMonsters);
            Assert.AreEqual(123.5f, back.BestClearTime);
        }

        [Test]
        public void PutSaveRequestBody_가_profile을_품고_직렬화된다()
        {
            PutSaveRequestBody body = new PutSaveRequestBody
            {
                profile = new MetaProfile { Souls = 5 },
                schemaVersion = 1,
                clientUpdatedAt = "2026-06-15T00:00:00Z",
            };
            string json = JsonUtility.ToJson(body);
            Assert.IsTrue(json.Contains("\"Souls\":5") || json.Contains("\"souls\":5"));
            Assert.IsTrue(json.Contains("schemaVersion"));
        }

        [Test]
        public void DisplayName_은_로컬필드로_왕복보존된다()
        {
            MetaProfile profile = new MetaProfile { DisplayName = "내영주" };
            MetaProfile back = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(profile));
            Assert.AreEqual("내영주", back.DisplayName);
        }

        //# 정상 — record DTO 도 public 필드면 JsonUtility 직렬화에 displayName 이 실린다(positional record 면 {} 가 됨).
        [Test]
        public void DisplayNameRequestBody_가_displayName필드로_직렬화된다()
        {
            DisplayNameRequestBody body = new DisplayNameRequestBody { displayName = "새이름" };
            string json = JsonUtility.ToJson(body);
            Assert.IsTrue(json.Contains("\"displayName\":\"새이름\""), $"기대 displayName 누락: {json}");
        }

        //# 엣지 — 서버 200 응답 JSON 을 record DTO 로 역직렬화하면 displayName 필드가 채워진다.
        [Test]
        public void 서버200응답JSON이_record_DTO로_역직렬화된다()
        {
            DisplayNameResponseBody ok = JsonUtility.FromJson<DisplayNameResponseBody>("{\"displayName\":\"정규화이름\"}");
            Assert.AreEqual("정규화이름", ok.displayName);
        }

        //# 정상 — 200 + 정상 본문은 Success + 서버 정규화 이름.
        [Test]
        public void 표시명200_정상본문이면_Success를_반환한다()
        {
            CHHttpResult res = new CHHttpResult { IsSuccess = true, StatusCode = 200, Body = "{\"displayName\":\"정규화이름\"}" };
            DisplayNameResult result = LairApiClient.ParseDisplayName(res);
            Assert.AreEqual(DisplayNameStatus.Success, result.Status);
            Assert.AreEqual("정규화이름", result.Name);
        }

        //# 엣지 — 200 + 빈 본문이면 JsonUtility.FromJson("") 이 throw 하지만 try/catch 가 잡아 Offline 으로 안전 처리.
        [Test]
        public void 표시명200_빈본문이면_throw없이_Offline를_반환한다()
        {
            CHHttpResult res = new CHHttpResult { IsSuccess = true, StatusCode = 200, Body = "" };
            DisplayNameResult result = LairApiClient.ParseDisplayName(res);
            Assert.AreEqual(DisplayNameStatus.Offline, result.Status);
        }

        //# 엣지 — 200 + malformed 본문도 throw 없이 Offline.
        [Test]
        public void 표시명200_malformed본문이면_throw없이_Offline를_반환한다()
        {
            CHHttpResult res = new CHHttpResult { IsSuccess = true, StatusCode = 200, Body = "{not json" };
            DisplayNameResult result = LairApiClient.ParseDisplayName(res);
            Assert.AreEqual(DisplayNameStatus.Offline, result.Status);
        }
    }
}
