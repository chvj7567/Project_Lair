using System.IO;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# MetaProfileStore 엣지 — 부분 손상 JSON/중첩 경로/연속 왕복 (기존 MetaProfileStoreTests 와 비중복).
    //# 임시 폴더는 기존 테스트(lair_meta_test)와 분리 — 실제 persistentDataPath 는 절대 사용하지 않는다.
    public class MetaProfileStoreEdgeTests
    {
        private string _dir;

        [SetUp]
        public void 준비()
        {
            _dir = Path.Combine(Path.GetTempPath(), "lair_meta_edge_test");
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
        public void 필드_누락_부분JSON은_기본값으로_채워_로드한다()
        {
            //# 구버전/수동 편집 세이브 — 존재 필드만 덮고 나머지는 생성자 기본값 유지.
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, MetaProfileStore.FileName), "{\"Souls\":42}");

            MetaProfile p = new MetaProfileStore(_dir).Load();

            Assert.AreEqual(42, p.Souls);
            //# JSON 에 Version 필드 부재 → 생성자 기본값(현 스키마 2) 채움. 명시 Version=1 이 아니므로 마이그레이션 무관.
            Assert.AreEqual(2, p.Version);
            Assert.AreEqual(1, p.LordRewardGrantedLevel);
            Assert.AreEqual("Knight", p.SelectedHero);
            Assert.AreEqual(-1f, p.BestClearTime, 0.0001f);
            Assert.IsNotNull(p.ShopLevels);
            Assert.IsNotNull(p.AchievedIds);
        }

        [Test]
        public void 빈_파일이면_새_프로필로_폴백한다()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, MetaProfileStore.FileName), string.Empty);

            MetaProfile p = new MetaProfileStore(_dir).Load();
            Assert.IsNotNull(p);
            Assert.AreEqual(0, p.Souls);
        }

        [Test]
        public void 존재하지_않는_중첩_디렉토리에도_저장된다()
        {
            string nested = Path.Combine(_dir, "a", "b");
            MetaProfileStore store = new MetaProfileStore(nested);

            store.Save(new MetaProfile { Souls = 123 });

            Assert.IsTrue(File.Exists(Path.Combine(nested, MetaProfileStore.FileName)));
            Assert.AreEqual(123, new MetaProfileStore(nested).Load().Souls);
        }

        [Test]
        public void null_프로필_저장은_파일을_만들지_않는다()
        {
            new MetaProfileStore(_dir).Save(null);
            Assert.IsFalse(File.Exists(Path.Combine(_dir, MetaProfileStore.FileName)));
        }

        [Test]
        public void 연속_저장_로드_왕복에도_누적값이_유지된다()
        {
            //# 런 3회 정산을 흉내 — 매 정산마다 Save 후 새 Store 로 Load (씬 전환 재로드와 동일 경로).
            MetaProfileStore store = new MetaProfileStore(_dir);
            MetaProfile p = store.Load();

            for (int run = 1; run <= 3; ++run)
            {
                p.Souls += 100;
                p.TotalRuns++;
                p.AddDistinct(p.SeenMonsters, $"Monster{run}");
                store.Save(p);
                p = new MetaProfileStore(_dir).Load();
            }

            Assert.AreEqual(300, p.Souls);
            Assert.AreEqual(3, p.TotalRuns);
            Assert.AreEqual(3, p.SeenMonsters.Count);
        }
    }
}
