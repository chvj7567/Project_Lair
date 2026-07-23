using NUnit.Framework;
using Lair.Meta;

namespace Lair.Tests.EditMode
{
    //# 스테이지 진행 세이브 필드 — 기본값·복사·버전(hero-stage-variant plan Task 1).
    public class MetaProfileStageTests
    {
        [Test]
        public void 신규_프로필은_SelectedStage_1_ClearedStage_0_이다()
        {
            MetaProfile p = new MetaProfile();
            Assert.AreEqual(1, p.SelectedStage);
            Assert.AreEqual(0, p.ClearedStage);
        }

        [Test]
        public void 신규_프로필의_Version은_3이다()
        {
            MetaProfile p = new MetaProfile();
            Assert.AreEqual(3, p.Version);
        }

        [Test]
        public void CopyFrom_은_ClearedStage_와_SelectedStage_를_복사한다()
        {
            MetaProfile src = new MetaProfile { SelectedStage = 3, ClearedStage = 4 };
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);
            Assert.AreEqual(3, dst.SelectedStage);
            Assert.AreEqual(4, dst.ClearedStage);
        }
    }
}
