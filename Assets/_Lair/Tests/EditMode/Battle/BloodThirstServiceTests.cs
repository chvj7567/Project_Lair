using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# 피의 갈증 활성/만료 검증. 회복 적용은 CharacterRegistry 의존이라 PlayMode/수동.
    public class BloodThirstServiceTests
    {
        [Test]
        public void Activate_후_IsActive_true()
        {
            BloodThirstService svc = new BloodThirstService();
            svc.Activate(30f);
            Assert.IsTrue(svc.IsActive);
        }

        [Test]
        public void 지속시간_경과_후_비활성()
        {
            BloodThirstService svc = new BloodThirstService();
            svc.Activate(30f);
            svc.Tick(31f);
            Assert.IsFalse(svc.IsActive);
        }

        [Test]
        public void 재Activate_시_더_긴_쪽으로_연장()
        {
            BloodThirstService svc = new BloodThirstService();
            svc.Activate(10f);
            svc.Tick(8f);            //# 남은 2
            svc.Activate(30f);       //# 30 으로 연장
            svc.Tick(25f);           //# 남은 5
            Assert.IsTrue(svc.IsActive);
        }
    }
}
