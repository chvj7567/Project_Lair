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
            PlayerPrefs.DeleteKey("Lair.Net.Token");
            PlayerPrefs.DeleteKey("Lair.Net.AccountId");
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
        public void 토큰_저장후_읽으면_같은값이다()
        {
            AuthTokenStore.SaveToken("abc.def.ghi");
            Assert.AreEqual("abc.def.ghi", AuthTokenStore.Token);
            Assert.IsTrue(AuthTokenStore.HasToken);
        }

        [Test]
        public void AccountId_저장후_읽으면_같은값이다()
        {
            AuthTokenStore.SaveAccountId(12345);
            Assert.AreEqual(12345, AuthTokenStore.AccountId);
            Assert.IsTrue(AuthTokenStore.HasAccountId);
        }

        [Test]
        public void AccountId_미설정이면_0이고_HasAccountId는_false다()
        {
            Assert.AreEqual(0, AuthTokenStore.AccountId);
            Assert.IsFalse(AuthTokenStore.HasAccountId);
        }
    }
}
