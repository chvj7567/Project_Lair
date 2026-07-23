using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# MetaProfile 엣지 — SetShopLevel 갱신/추가 분기, AddDistinct 방어, 직렬화 필드 보존 (기존 MetaProfileTests 와 비중복).
    public class MetaProfileEdgeTests
    {
        [Test]
        public void SetShopLevel_같은_품목_재호출은_엔트리를_늘리지_않고_갱신한다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 1);
            p.SetShopLevel("MonsterHpUp", 3);

            Assert.AreEqual(1, p.ShopLevels.Count);
            Assert.AreEqual(3, p.GetShopLevel("MonsterHpUp"));
        }

        [Test]
        public void SetShopLevel_다른_품목은_엔트리가_각각_추가된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 1);
            p.SetShopLevel("SpawnerHasteUp", 2);

            Assert.AreEqual(2, p.ShopLevels.Count);
            Assert.AreEqual(1, p.GetShopLevel("MonsterHpUp"));
            Assert.AreEqual(2, p.GetShopLevel("SpawnerHasteUp"));
        }

        [Test]
        public void AddDistinct_null이나_빈값은_무시된다()
        {
            MetaProfile p = new MetaProfile();
            p.AddDistinct(p.SeenMonsters, null);
            p.AddDistinct(p.SeenMonsters, string.Empty);
            p.AddDistinct(null, "Wisp");   //# 리스트 null — 예외 없이 무시.

            Assert.AreEqual(0, p.SeenMonsters.Count);
        }

        [Test]
        public void Version과_멱등가드가_왕복직렬화로_보존된다()
        {
            MetaProfile p = new MetaProfile { LordRewardGrantedLevel = 5 };
            MetaProfile r = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            //# 현 스키마 버전 3 이 왕복 직렬화로 보존되는지(스테이지 전적 추가로 승격, spec §4).
            Assert.AreEqual(3, r.Version);
            Assert.AreEqual(5, r.LordRewardGrantedLevel);
        }

        [Test]
        public void 선택영웅과_최단클리어가_왕복직렬화로_보존된다()
        {
            MetaProfile p = new MetaProfile { SelectedHero = "Knight", BestClearTime = 76.04f };
            MetaProfile r = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual("Knight", r.SelectedHero);
            Assert.AreEqual(76.04f, r.BestClearTime, 0.0001f);
        }

        [Test]
        public void 기록없음_최단클리어는_마이너스1로_보존된다()
        {
            //# RecordsPopup "-" 표기 키 — 직렬화로 0 이 되면 "0초 클리어" 오표기 (기획서 §7).
            MetaProfile r = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(new MetaProfile()));
            Assert.AreEqual(-1f, r.BestClearTime, 0.0001f);
        }
    }
}
