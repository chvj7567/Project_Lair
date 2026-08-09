using NUnit.Framework;

namespace ChvjUnityInfra.Tests
{
    /// <summary>
    /// <see cref="EnumNameCache{TEnum}"/> 스모크 검증.
    /// 정상: 정의된 enum 값이 올바른 이름 문자열로, 캐시 히트 시 동일 문자열 인스턴스 반환(박싱 없는 캐시 확인).
    /// 엣지: 서로 다른 enum 타입 두 개가 독립적으로 캐싱되는지(제네릭 정적 캐시 타입 분리).
    /// </summary>
    public class EnumNameCacheTests
    {
        private enum SampleEnumA { Alpha, Beta }
        private enum SampleEnumB { One, Two, Three }

        [Test]
        public void Get_DefinedValue_ReturnsMatchingName()
        {
            string name = EnumNameCache<SampleEnumA>.Get(SampleEnumA.Beta);

            Assert.AreEqual("Beta", name);
        }

        [Test]
        public void Get_SameValueTwice_ReturnsSameStringInstance()
        {
            string first = EnumNameCache<SampleEnumA>.Get(SampleEnumA.Alpha);
            string second = EnumNameCache<SampleEnumA>.Get(SampleEnumA.Alpha);

            Assert.IsTrue(ReferenceEquals(first, second), "캐시된 문자열이 재사용되지 않고 매번 새로 생성됐다");
        }

        [Test]
        public void Get_TwoDistinctEnumTypes_CacheIndependently()
        {
            string nameFromA = EnumNameCache<SampleEnumA>.Get(SampleEnumA.Alpha);
            string nameFromB = EnumNameCache<SampleEnumB>.Get(SampleEnumB.Two);

            Assert.AreEqual("Alpha", nameFromA);
            Assert.AreEqual("Two", nameFromB);
        }

        [Test]
        public void Get_UndefinedValue_FallsBackToToString()
        {
            string name = EnumNameCache<SampleEnumA>.Get((SampleEnumA)99);

            Assert.AreEqual("99", name);
        }
    }
}
