using NUnit.Framework;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    public class CloudSaveServiceTests
    {
        [Test]
        public void 백업_성공이면_프로필을_업로드한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Success };
            CloudSaveService svc = new CloudSaveService(fake);
            MetaProfile profile = new MetaProfile { Souls = 10 };

            CloudSaveResult result = svc.BackupAsync(profile).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Success, result);
            Assert.AreSame(profile, fake.LastPutProfile);
        }

        [Test]
        public void 백업_409면_충돌을_그대로_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Conflict };
            CloudSaveService svc = new CloudSaveService(fake);

            CloudSaveResult result = svc.BackupAsync(new MetaProfile()).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Conflict, result);
        }

        [Test]
        public void 백업_프로필이_null이면_실패를_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Success };
            CloudSaveService svc = new CloudSaveService(fake);

            CloudSaveResult result = svc.BackupAsync(null).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Failed, result);
        }

        [Test]
        public void 복원_서버데이터있으면_프로필을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient
            {
                SaveToReturn = new SaveResponseBody { profile = new MetaProfile { Souls = 99 }, schemaVersion = 1 },
            };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile restored = svc.RestoreAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(restored);
            Assert.AreEqual(99, restored.Souls);
        }

        [Test]
        public void 복원_서버데이터없으면_null을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SaveToReturn = null };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile restored = svc.RestoreAsync().GetAwaiter().GetResult();

            Assert.IsNull(restored);
        }
    }
}
