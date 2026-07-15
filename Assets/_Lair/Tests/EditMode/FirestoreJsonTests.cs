using NUnit.Framework;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    public class FirestoreJsonTests
    {
        [Test]
        public void 문자열_필드를_타입_JSON_으로_감싼다()
        {
            string json = FirestoreJson.StringField("영주 #A3F9");
            Assert.AreEqual("{\"stringValue\":\"영주 #A3F9\"}", json);
        }

        [Test]
        public void 정수_필드는_문자열_integerValue_로_직렬화된다()
        {
            Assert.AreEqual("{\"integerValue\":\"92500\"}", FirestoreJson.IntField(92500));
        }

        [Test]
        public void 문서에서_stringValue_를_추출한다()
        {
            string doc = "{\"name\":\"...\",\"fields\":{\"profile\":{\"stringValue\":\"HELLO\"}},\"updateTime\":\"2026-07-14T00:00:00Z\"}";
            Assert.AreEqual("HELLO", FirestoreJson.ExtractString(doc, "profile"));
            Assert.AreEqual("2026-07-14T00:00:00Z", FirestoreJson.ExtractUpdateTime(doc));
        }

        [Test]
        public void 없는_필드_추출은_null_또는_0()
        {
            Assert.IsNull(FirestoreJson.ExtractString("{\"fields\":{}}", "nope"));
            Assert.AreEqual(0, FirestoreJson.ExtractInt("{\"fields\":{}}", "nope"));
        }

        [Test]
        public void 큰따옴표와_역슬래시를_이스케이프한다()
        {
            string json = FirestoreJson.StringField("a\"b\\c");
            Assert.AreEqual("{\"stringValue\":\"a\\\"b\\\\c\"}", json);
        }
    }
}
