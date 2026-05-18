using System;
using NUnit.Framework;

namespace ChvjUnityInfra.Tests
{
    public class JsonArrayUtilityTests
    {
        [Serializable]
        private class Item
        {
            public int id;
            public string name;
        }

        [Test]
        public void FromJsonArray_ParsesTopLevelJsonArray()
        {
            string json = "[{\"id\":1,\"name\":\"a\"},{\"id\":2,\"name\":\"b\"}]";

            Item[] items = JsonArrayUtility.FromJsonArray<Item>(json);

            Assert.AreEqual(2, items.Length);
            Assert.AreEqual(1, items[0].id);
            Assert.AreEqual("a", items[0].name);
            Assert.AreEqual(2, items[1].id);
            Assert.AreEqual("b", items[1].name);
        }

        [Test]
        public void FromJsonArray_EmptyArrayReturnsEmptyArray()
        {
            string json = "[]";

            Item[] items = JsonArrayUtility.FromJsonArray<Item>(json);

            Assert.IsNotNull(items);
            Assert.AreEqual(0, items.Length);
        }

        [Test]
        public void FromJsonArray_NullReturnsEmptyArray()
        {
            Item[] items = JsonArrayUtility.FromJsonArray<Item>(null);

            Assert.IsNotNull(items);
            Assert.AreEqual(0, items.Length);
        }
    }
}
