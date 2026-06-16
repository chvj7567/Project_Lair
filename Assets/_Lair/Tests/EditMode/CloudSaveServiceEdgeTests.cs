using NUnit.Framework;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# CloudSaveService 의 기존 5건이 안 다룬 빈 경로 보강 — 409 전파/응답껍데기만 있고 profile null/백업 실패코드.
    public class CloudSaveServiceEdgeTests
    {
        [Test]
        public void 복원_응답은있으나_profile이_null이면_null을_반환한다()
        {
            //# 기존 5건은 SaveToReturn==null 만 다룸 — 응답 껍데기는 있고 profile 만 null 인 경계 보강.
            FakeLairApiClient fake = new FakeLairApiClient
            {
                SaveToReturn = new SaveResponseBody { profile = null, schemaVersion = 1 },
            };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile restored = svc.RestoreAsync().GetAwaiter().GetResult();

            Assert.IsNull(restored);
        }

        [Test]
        public void 백업_서버실패코드면_실패를_그대로_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Failed };
            CloudSaveService svc = new CloudSaveService(fake);

            CloudSaveResult result = svc.BackupAsync(new MetaProfile { Souls = 1 }).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Failed, result);
        }

        [Test]
        public void 백업_프로필null이면_API를_호출하지_않는다()
        {
            //# profile null 가드가 API 호출 전에 끊는지 — 불필요한 PUT 방지.
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Success };
            CloudSaveService svc = new CloudSaveService(fake);

            svc.BackupAsync(null).GetAwaiter().GetResult();

            Assert.IsNull(fake.LastPutProfile);
        }

        [Test]
        public void 백업_clientUpdatedAt이_ISO8601_UTC형식이다()
        {
            //# clientUpdatedAt 은 'yyyy-MM-ddTHH:mm:ssZ' 형식 — 서버 충돌 비교 기준(기획서 §4).
            RecordingApiClient rec = new RecordingApiClient();
            CloudSaveService svc = new CloudSaveService(rec);

            svc.BackupAsync(new MetaProfile()).GetAwaiter().GetResult();

            string ts = rec.LastClientUpdatedAt;
            Assert.IsNotNull(ts);
            Assert.IsTrue(ts.EndsWith("Z"), $"UTC 'Z' suffix 누락: {ts}");
            Assert.IsTrue(ts.Contains("T"), $"날짜/시간 구분자 'T' 누락: {ts}");
            Assert.AreEqual(20, ts.Length, $"yyyy-MM-ddTHH:mm:ssZ 20자 형식 위반: {ts}");
        }

        //# PutSave 인자(clientUpdatedAt)를 캡처하는 가짜 — 공용 FakeLairApiClient 는 타임스탬프를 안 기록해 별도.
        private class RecordingApiClient : ILairApiClient
        {
            public string LastClientUpdatedAt;

            public System.Threading.Tasks.Task<bool> AuthenticateAsync()
                => System.Threading.Tasks.Task.FromResult(true);
            public System.Threading.Tasks.Task<SaveResponseBody> GetSaveAsync()
                => System.Threading.Tasks.Task.FromResult<SaveResponseBody>(null);
            public System.Threading.Tasks.Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
            {
                LastClientUpdatedAt = clientUpdatedAt;
                return System.Threading.Tasks.Task.FromResult(CloudSaveResult.Success);
            }
            public System.Threading.Tasks.Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
                => System.Threading.Tasks.Task.FromResult(true);
            public System.Threading.Tasks.Task<System.Collections.Generic.List<RankingRowDto>> GetTopAsync(int top)
                => System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<RankingRowDto>());
            public System.Threading.Tasks.Task<System.Collections.Generic.List<RankingRowDto>> GetMyRankAsync()
                => System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<RankingRowDto>());
        }
    }
}
