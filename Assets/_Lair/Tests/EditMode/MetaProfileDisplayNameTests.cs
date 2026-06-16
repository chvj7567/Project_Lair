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
    }
}
