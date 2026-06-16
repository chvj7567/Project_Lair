using NUnit.Framework;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# 통합 — 백업→복원→CopyFrom 까지 서비스가 엮인 라운드트립(개별 단위테스트 너머의 시나리오).
    public class CloudSaveRoundTripTests
    {
        [Test]
        public void 백업한_프로필을_복원하면_스칼라가_왕복보존된다()
        {
            FakeLairApiClient fake = new FakeLairApiClient();
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile local = new MetaProfile { Souls = 500, LordXp = 120, SelectedHero = "Mage", BestClearTime = 91.2f };
            local.SetShopLevel("MonsterHpUp", 2);

            //# 백업 — 가짜가 마지막 업로드 프로필을 보관.
            CloudSaveResult put = svc.BackupAsync(local).GetAwaiter().GetResult();
            Assert.AreEqual(CloudSaveResult.Success, put);

            //# 서버가 그 프로필을 돌려준다고 모사.
            fake.SaveToReturn = new SaveResponseBody { profile = fake.LastPutProfile, schemaVersion = local.Version };

            MetaProfile cloud = svc.RestoreAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(cloud);
            Assert.AreEqual(500, cloud.Souls);
            Assert.AreEqual(120, cloud.LordXp);
            Assert.AreEqual("Mage", cloud.SelectedHero);
            Assert.AreEqual(2, cloud.GetShopLevel("MonsterHpUp"));
        }

        [Test]
        public void 복원후_CopyFrom하면_기존인스턴스가_서버값을_갖고_로컬이름은_유지한다()
        {
            //# 실제 복원 흐름 재현: cloud(서버) 받아 live profile.CopyFrom(cloud) → 참조 유지(기획서 §1).
            FakeLairApiClient fake = new FakeLairApiClient
            {
                SaveToReturn = new SaveResponseBody
                {
                    profile = new MetaProfile { Souls = 777, SelectedHero = "Rogue", DisplayName = "서버명" },
                    schemaVersion = 1,
                },
            };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile live = new MetaProfile { Souls = 1, SelectedHero = "Knight", DisplayName = "내로컬명" };
            MetaProfile liveRef = live;

            MetaProfile cloud = svc.RestoreAsync().GetAwaiter().GetResult();
            live.CopyFrom(cloud);

            Assert.AreSame(liveRef, live);                 //# 인스턴스 교체 아님 — VM/View 가 보던 객체 그대로.
            Assert.AreEqual(777, live.Souls);
            Assert.AreEqual("Rogue", live.SelectedHero);
            Assert.AreEqual("내로컬명", live.DisplayName);   //# 로컬 표시명 보존.
        }

        [Test]
        public void 서버데이터없는상태로_복원하면_로컬을_건드리지_않는다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SaveToReturn = null };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile live = new MetaProfile { Souls = 42 };

            MetaProfile cloud = svc.RestoreAsync().GetAwaiter().GetResult();
            //# 복원 흐름: cloud==null 이면 CopyFrom 호출 안 함(호출부 가드). live 그대로.
            if (cloud != null)
                live.CopyFrom(cloud);

            Assert.IsNull(cloud);
            Assert.AreEqual(42, live.Souls);
        }
    }
}
