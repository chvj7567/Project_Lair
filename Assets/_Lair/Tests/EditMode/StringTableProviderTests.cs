using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# StringTableProvider.Load(TextAsset) / GetString 동작 검증.
    public class StringTableProviderTests
    {
        [Test]
        public void Load_유효한_TextAsset_등록된_id로_문자열_반환()
        {
            string json = "[{\"id\":1,\"text\":\"안녕\"},{\"id\":2,\"text\":\"세계\"}]";
            TextAsset asset = new TextAsset(json);
            StringTableProvider provider = new StringTableProvider();

            provider.Load(asset);

            Assert.AreEqual("안녕", provider.GetString(1));
            Assert.AreEqual("세계", provider.GetString(2));
        }

        [Test]
        public void Load_null_asset_예외없이_처리()
        {
            StringTableProvider provider = new StringTableProvider();
            LogAssert.Expect(LogType.Error, "[StringTableProvider] asset 이 null");
            Assert.DoesNotThrow(() => provider.Load(null));
        }

        [Test]
        public void GetString_없는_id_빈문자열_반환()
        {
            StringTableProvider provider = new StringTableProvider();
            Assert.AreEqual(string.Empty, provider.GetString(999));
        }
    }
}
