using System.IO;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.PlayMode
{
    //# PlayMode 스위트 전역 프로필 격리 — Battle.unity 에 MetaConfig 가 와이어돼 EndBattle 정산이
    //# MetaSession.Store.Save 를 부른다. 주입 없이는 실제 persistentDataPath/meta_profile.json 이
    //# 테스트 런마다 오염되고(TotalRuns/소울 누적), 반대로 개발자 프로필의 상점 레벨이 시뮬 수치를 왜곡한다.
    [SetUpFixture]
    public class MetaProfilePlayModeIsolation
    {
        private string _dir;

        [OneTimeSetUp]
        public void 전체_준비()
        {
            _dir = Path.Combine(Path.GetTempPath(), "lair_playmode_meta");
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
            //# 임시 스토어 + 깨끗한 프로필 주입 — 전 PlayMode 테스트가 메타 보너스 항등 상태로 시작.
            MetaSession.Store = new MetaProfileStore(_dir);
            MetaSession.Profile = new MetaProfile();
        }

        [OneTimeTearDown]
        public void 전체_정리()
        {
            MetaSession.Store = null;
            MetaSession.Profile = null;
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }
    }
}
