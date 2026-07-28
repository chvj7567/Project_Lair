using NUnit.Framework;
using Lair.Net;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class AuthTokenStoreTests
    {
        [TearDown]
        public void 정리()
        {
            PlayerPrefs.DeleteKey("Lair.Net.DeviceId");
            PlayerPrefs.DeleteKey("Lair.Net.Uid");
        }

        [Test]
        public void DeviceId_없으면_생성하고_재호출시_동일하다()
        {
            string first = AuthTokenStore.GetOrCreateDeviceId();
            string second = AuthTokenStore.GetOrCreateDeviceId();
            Assert.IsFalse(string.IsNullOrEmpty(first));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void Uid_저장후_조회된다()
        {
            AuthTokenStore.SaveUid("kZ9xAbC");
            Assert.AreEqual("kZ9xAbC", AuthTokenStore.Uid);
            Assert.IsTrue(AuthTokenStore.HasUid);
        }

        [Test]
        public void 미설정_Uid_는_빈문자열이고_HasUid_false()
        {
            Assert.AreEqual(string.Empty, AuthTokenStore.Uid);
            Assert.IsFalse(AuthTokenStore.HasUid);
        }
    }
}
