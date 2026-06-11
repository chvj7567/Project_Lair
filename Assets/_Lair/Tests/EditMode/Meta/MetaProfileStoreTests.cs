using System.IO;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    public class MetaProfileStoreTests
    {
        private string _dir;

        [SetUp]
        public void 준비()
        {
            _dir = Path.Combine(Path.GetTempPath(), "lair_meta_test");
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [TearDown]
        public void 정리()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void 파일이_없으면_새_프로필을_반환한다()
        {
            MetaProfile p = new MetaProfileStore(_dir).Load();
            Assert.IsNotNull(p);
            Assert.AreEqual(0, p.Souls);
        }

        [Test]
        public void 저장_후_로드하면_값이_복원된다()
        {
            MetaProfileStore store = new MetaProfileStore(_dir);
            MetaProfile p = store.Load();
            p.Souls = 777;
            store.Save(p);
            Assert.AreEqual(777, new MetaProfileStore(_dir).Load().Souls);
        }

        [Test]
        public void 깨진_JSON_파일이면_새_프로필로_폴백한다()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, MetaProfileStore.FileName), "{{{broken");
            MetaProfile p = new MetaProfileStore(_dir).Load();
            Assert.IsNotNull(p);
            Assert.AreEqual(0, p.Souls);
        }
    }
}
