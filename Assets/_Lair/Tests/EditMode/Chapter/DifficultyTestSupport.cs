using ChvjUnityInfra;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 난이도 테스트 공용 — 정답 배율표 Rows() 와 정적 상태(DifficultyTable.Current · CHText.StringProvider) 격리 베이스.
    public static class DifficultyTestSupport
    {
        //# 기획서 §1.2 배율표 (production Difficulty.json 과 독립으로 다시 적은 기대값).
        public static DifficultyRow[] Rows()
        {
            return new[]
            {
                new DifficultyRow { difficulty = 0, nameKey = 1327, hpMultiplier = 1.0f, powerMultiplier = 1.0f },
                new DifficultyRow { difficulty = 1, nameKey = 1328, hpMultiplier = 1.15f, powerMultiplier = 1.1f },
                new DifficultyRow { difficulty = 2, nameKey = 1329, hpMultiplier = 1.3f, powerMultiplier = 1.22f },
            };
        }

        public static DifficultyTable Table()
        {
            return DifficultyTable.Build(Rows(), id => true);
        }

        //# 실제 Strings_Ko 로 문구를 조회하는 provider.
        public class RealStrings : IStringProvider
        {
            public string GetString(int stringID)
            {
                return WorldMapTestSupport.Text(stringID);
            }
        }
    }

    //# DifficultyTable.Current 는 null(Easy 만 존재), CHText.StringProvider 는 null 로 시작하고 테스트 후 원복한다.
    public abstract class DifficultyStaticStateTestBase : ChapterStaticStateTestBase
    {
        private DifficultyTable _prevTable;
        private IStringProvider _prevProvider;

        [SetUp]
        public void 난이도_정적상태를_저장하고_초기화한다()
        {
            _prevTable = DifficultyTable.Current;
            _prevProvider = CHText.StringProvider;
            DifficultyTable.Current = null;
            CHText.StringProvider = null;
        }

        [TearDown]
        public void 난이도_정적상태를_원복한다()
        {
            DifficultyTable.Current = _prevTable;
            CHText.StringProvider = _prevProvider;
        }
    }
}
