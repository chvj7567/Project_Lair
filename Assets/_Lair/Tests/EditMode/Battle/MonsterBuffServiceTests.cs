using NUnit.Framework;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 글로벌 버프 활성/만료/연장 검증. 몬스터 적용은 CharacterRegistry 의존이라 PlayMode/수동.
    public class MonsterBuffServiceTests
    {
        [Test]
        public void AddBuff_후_IsActive_true()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.Frenzy, 10f);
            Assert.IsTrue(svc.IsActive(EMonsterBuff.Frenzy));
        }

        [Test]
        public void 지속시간_경과_후_만료()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.Frenzy, 5f);
            svc.Tick(6f);
            Assert.IsFalse(svc.IsActive(EMonsterBuff.Frenzy));
        }

        [Test]
        public void 같은_버프_재부착_시_지속시간_연장()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.IronWill, 5f);
            svc.Tick(3f);                          //# 남은 2
            svc.AddBuff(EMonsterBuff.IronWill, 5f); //# 5 로 연장
            svc.Tick(4f);                          //# 남은 1
            Assert.IsTrue(svc.IsActive(EMonsterBuff.IronWill));
        }

        [Test]
        public void 서로_다른_버프_독립_관리()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.Frenzy, 5f);
            svc.AddBuff(EMonsterBuff.BerserkPower, 15f);
            svc.Tick(6f);
            Assert.IsFalse(svc.IsActive(EMonsterBuff.Frenzy));
            Assert.IsTrue(svc.IsActive(EMonsterBuff.BerserkPower));
        }
    }
}
