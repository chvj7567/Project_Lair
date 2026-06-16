using NUnit.Framework;
using Lair.Meta;

namespace Lair.Tests.EditMode
{
    //# MetaProfile.ResolveDisplayName 단위 검증(기획서 §1) — DisplayName 우선, 빈 값이면 "영주 #"+deviceId 앞4자 대문자.
    public class MetaProfileDisplayNameTests
    {
        [Test]
        public void DisplayName이_있으면_그대로_쓴다()
        {
            string name = MetaProfile.ResolveDisplayName("용맹한영주", "a3f9deadbeef");

            Assert.AreEqual("용맹한영주", name);
        }

        [Test]
        public void DisplayName이_비면_deviceId_앞4자_대문자_기본명을_쓴다()
        {
            string name = MetaProfile.ResolveDisplayName(string.Empty, "a3f9deadbeef");

            Assert.AreEqual("영주 #A3F9", name);
        }

        [Test]
        public void DisplayName이_null이고_deviceId가_4자미만이면_있는만큼만_쓴다()
        {
            //# 엣지 — Substring(0,4) 가 짧은 deviceId 에서 예외 던지지 않도록 길이 가드.
            string name = MetaProfile.ResolveDisplayName(null, "ab");

            Assert.AreEqual("영주 #AB", name);
        }

        //# NormalizeDisplayName 단위 검증 — null/빈값(trim 후)=거부(null), 그 외 trim + 12자 절삭.
        [Test]
        public void 정상_입력은_trim해서_그대로_반환한다()
        {
            string name = MetaProfile.NormalizeDisplayName("  용맹한영주  ");

            Assert.AreEqual("용맹한영주", name);
        }

        [Test]
        public void trim_후_빈값이면_null을_반환한다()
        {
            //# 엣지 — 공백만 입력 시 거부(null) → 호출부가 거부 토스트로 분기.
            string name = MetaProfile.NormalizeDisplayName("   ");

            Assert.IsNull(name);
        }

        [Test]
        public void null_입력은_NRE_없이_null을_반환한다()
        {
            //# 엣지 — 헬퍼 계약상 null 입력은 거부(null). NRE 회귀 가드.
            string name = MetaProfile.NormalizeDisplayName(null);

            Assert.IsNull(name);
        }

        [Test]
        public void 정확히_12자면_절삭하지_않고_그대로_반환한다()
        {
            //# 엣지 — 경계(> 12)라 12자는 통과. off-by-one 회귀 가드.
            string name = MetaProfile.NormalizeDisplayName("123456789012");

            Assert.AreEqual("123456789012", name);
        }

        [Test]
        public void 길이가_12자를_넘으면_앞_12자로_절삭한다()
        {
            //# 엣지 — 13자 입력은 앞 12자만 확정.
            string name = MetaProfile.NormalizeDisplayName("1234567890123");

            Assert.AreEqual("123456789012", name);
        }
    }
}
