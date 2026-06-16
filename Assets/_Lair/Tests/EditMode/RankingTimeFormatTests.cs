using System.Globalization;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using Lair.UI;

namespace Lair.Tests.EditMode
{
    //# RankingCell.FormatMs(private static) 경계 검산 — 기획서 §4 의 m:ss.f 표기.
    //# 리플렉션 호출(MonoBehaviour 인스턴스 불필요 — static 순수 함수).
    public class RankingTimeFormatTests
    {
        private CultureInfo _prev;

        [SetUp]
        public void 문화권고정()
        {
            //# "{s:00.0}" 의 소수점 표기는 문화권 의존 — 검산값 고정 위해 Invariant 로 강제.
            _prev = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void 문화권복구()
        {
            Thread.CurrentThread.CurrentCulture = _prev;
        }

        private static string FormatMs(int ms)
        {
            MethodInfo m = typeof(RankingCell).GetMethod("FormatMs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "FormatMs 미발견 — RankingCell 시그니처 변경 시 갱신");
            return (string)m.Invoke(null, new object[] { ms });
        }

        [Test]
        public void FormatMs_0ms는_0초00이다()
        {
            Assert.AreEqual("0:00.0", FormatMs(0));
        }

        [Test]
        public void FormatMs_6000ms는_0초06이다()
        {
            Assert.AreEqual("0:06.0", FormatMs(6000));
        }

        [Test]
        public void FormatMs_92500ms는_1분32초5다()
        {
            //# 기획서 §4 검산값.
            Assert.AreEqual("1:32.5", FormatMs(92500));
        }

        [Test]
        public void FormatMs_300000ms는_5분00이다()
        {
            //# 5:00 (제한시간 정각) 검산.
            Assert.AreEqual("5:00.0", FormatMs(300000));
        }

        [Test]
        public void FormatMs_59초경계는_0분59초9다()
        {
            Assert.AreEqual("0:59.9", FormatMs(59900));
        }

        [Test]
        public void FormatMs_초가_10미만이면_0패딩한다()
        {
            //# 한자리 초는 "06.0" 처럼 0 패딩 — 00.0 포맷 검증.
            Assert.AreEqual("2:03.0", FormatMs(123000));
        }
    }
}
