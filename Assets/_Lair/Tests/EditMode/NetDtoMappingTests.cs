using NUnit.Framework;
using Lair.Meta;
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
        public void DisplayName_은_로컬필드로_왕복보존된다()
        {
            MetaProfile profile = new MetaProfile { DisplayName = "내영주" };
            MetaProfile back = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(profile));
            Assert.AreEqual("내영주", back.DisplayName);
        }
    }
}
