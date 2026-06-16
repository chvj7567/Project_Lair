using NUnit.Framework;
using Lair.Meta;

namespace Lair.Tests.EditMode
{
    //# 클라우드 복원의 핵심 회귀 — CopyFrom 이 모든 필드/리스트를 옮기고 DisplayName 만 보존하는지(기획서 §1).
    public class MetaProfileCopyFromTests
    {
        //# 모든 스칼라 필드를 채운 원본 프로필 생성(테스트 입력 픽스처).
        private static MetaProfile MakeFullProfile()
        {
            MetaProfile p = new MetaProfile
            {
                Version = 7,
                Souls = 1234,
                LordXp = 555,
                LordRewardGrantedLevel = 4,
                TotalRuns = 30,
                TotalWins = 11,
                BestClearTime = 87.5f,
                SelectedHero = "Mage",
            };
            p.SetShopLevel("MonsterHpUp", 3);
            p.SetShopLevel("MonsterAtkUp", 2);
            p.AddDistinct(p.AchievedIds, "FirstWin");
            p.AddDistinct(p.SeenMonsters, "Wisp");
            p.AddDistinct(p.SeenMonsters, "Slime");
            p.AddDistinct(p.PickedCards, "B1");
            return p;
        }

        [Test]
        public void CopyFrom_모든_스칼라필드를_복사한다()
        {
            MetaProfile src = MakeFullProfile();
            MetaProfile dst = new MetaProfile();

            dst.CopyFrom(src);

            Assert.AreEqual(7, dst.Version);
            Assert.AreEqual(1234, dst.Souls);
            Assert.AreEqual(555, dst.LordXp);
            Assert.AreEqual(4, dst.LordRewardGrantedLevel);
            Assert.AreEqual(30, dst.TotalRuns);
            Assert.AreEqual(11, dst.TotalWins);
            Assert.AreEqual(87.5f, dst.BestClearTime);
            Assert.AreEqual("Mage", dst.SelectedHero);
        }

        [Test]
        public void CopyFrom_모든_리스트내용을_옮긴다()
        {
            MetaProfile src = MakeFullProfile();
            MetaProfile dst = new MetaProfile();

            dst.CopyFrom(src);

            Assert.AreEqual(3, dst.GetShopLevel("MonsterHpUp"));
            Assert.AreEqual(2, dst.GetShopLevel("MonsterAtkUp"));
            Assert.Contains("FirstWin", dst.AchievedIds);
            Assert.Contains("Wisp", dst.SeenMonsters);
            Assert.Contains("Slime", dst.SeenMonsters);
            Assert.Contains("B1", dst.PickedCards);
        }

        [Test]
        public void CopyFrom_DisplayName은_의도적으로_보존한다()
        {
            MetaProfile src = MakeFullProfile();
            src.DisplayName = "서버표시명";
            MetaProfile dst = new MetaProfile { DisplayName = "로컬표시명" };

            dst.CopyFrom(src);

            //# DisplayName 은 로컬 전용 — 복원으로 덮지 않는다(기획서 §1).
            Assert.AreEqual("로컬표시명", dst.DisplayName);
        }

        [Test]
        public void CopyFrom_로컬DisplayName이_비어도_복원으로_채우지_않는다()
        {
            MetaProfile src = MakeFullProfile();
            src.DisplayName = "서버표시명";
            MetaProfile dst = new MetaProfile { DisplayName = null };

            dst.CopyFrom(src);

            Assert.IsNull(dst.DisplayName);
        }

        [Test]
        public void CopyFrom_other가_null이면_변경없이_그대로다()
        {
            MetaProfile dst = MakeFullProfile();
            dst.DisplayName = "유지";

            dst.CopyFrom(null);

            Assert.AreEqual(1234, dst.Souls);
            Assert.AreEqual("유지", dst.DisplayName);
        }

        [Test]
        public void CopyFrom_원본_리스트가_null이면_빈리스트로_대체한다()
        {
            MetaProfile src = new MetaProfile
            {
                ShopLevels = null,
                AchievedIds = null,
                SeenMonsters = null,
                PickedCards = null,
            };
            MetaProfile dst = new MetaProfile();

            dst.CopyFrom(src);

            //# null 가드 — 이후 AddDistinct/Get 호출이 NRE 나지 않도록 빈 리스트 보장.
            Assert.IsNotNull(dst.ShopLevels);
            Assert.IsNotNull(dst.AchievedIds);
            Assert.IsNotNull(dst.SeenMonsters);
            Assert.IsNotNull(dst.PickedCards);
            Assert.AreEqual(0, dst.AchievedIds.Count);
        }

        [Test]
        public void CopyFrom_복원후_사본수정이_원본스칼라에_번지지_않는다()
        {
            MetaProfile src = MakeFullProfile();
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);

            dst.Souls = 9999;
            dst.SelectedHero = "Rogue";

            //# 스칼라는 값 복사라 사본 변경이 원본을 건드리지 않는다.
            Assert.AreEqual(1234, src.Souls);
            Assert.AreEqual("Mage", src.SelectedHero);
        }

        [Test]
        public void CopyFrom_리스트는_참조를_공유한다_현행동작_고정()
        {
            //# 회귀 박제 — 현 구현은 리스트를 deep copy 하지 않고 참조를 그대로 대입한다(MetaProfile.CopyFrom).
            //# 복원 흐름에선 other(서버 deserialize 결과)가 즉시 폐기되므로 실害 없으나, 동작을 명시 고정한다.
            MetaProfile src = MakeFullProfile();
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);

            Assert.AreSame(src.AchievedIds, dst.AchievedIds);
            Assert.AreSame(src.SeenMonsters, dst.SeenMonsters);
            Assert.AreSame(src.PickedCards, dst.PickedCards);
            Assert.AreSame(src.ShopLevels, dst.ShopLevels);
        }
    }
}
