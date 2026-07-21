using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class MetaProfileTests
    {
        [Test]
        public void 새_프로필은_버전2_소울0으로_시작한다()
        {
            MetaProfile p = new MetaProfile();
            //# 스키마 버전은 스테이지 진행 필드 추가로 2 로 승격(hero-stage-variant plan Task 1).
            Assert.AreEqual(2, p.Version);
            Assert.AreEqual(0, p.Souls);
            Assert.AreEqual(0, p.LordXp);
            //# 영주 보상 멱등 가드 초기값 1 (기획서 §4.4 / §11.3 [추가 1]).
            Assert.AreEqual(1, p.LordRewardGrantedLevel);
            Assert.IsNotNull(p.ShopLevels);
            Assert.IsNotNull(p.AchievedIds);
        }

        [Test]
        public void JsonUtility_왕복_직렬화로_필드가_보존된다()
        {
            MetaProfile p = new MetaProfile { Souls = 120, LordXp = 350 };
            p.SetShopLevel("MonsterHpUp", 3);
            p.AchievedIds.Add("FirstWin");
            MetaProfile r = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));
            Assert.AreEqual(120, r.Souls);
            Assert.AreEqual(3, r.GetShopLevel("MonsterHpUp"));
            Assert.Contains("FirstWin", r.AchievedIds);
        }

        [Test]
        public void 없는_상점_항목_레벨은_0이다()
        {
            Assert.AreEqual(0, new MetaProfile().GetShopLevel("없는항목"));
        }

        [Test]
        public void AddDistinct는_중복을_누적하지_않는다()
        {
            MetaProfile p = new MetaProfile();
            p.AddDistinct(p.SeenMonsters, "Wisp");
            p.AddDistinct(p.SeenMonsters, "Wisp");
            Assert.AreEqual(1, p.SeenMonsters.Count);
        }
    }
}
