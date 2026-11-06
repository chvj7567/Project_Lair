using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# SkillUnlockBannerView.ComputeHiddenX 계약 — 숨김 시 밴드 가장자리가 캔버스 밖 + 마진. 좌표는 앵커 기준점 원점 부모 로컬 X.
    public class SkillUnlockBannerHiddenXTests
    {
        private const float Eps = 0.001f;

        //# 밴드(pivot 0.5) 중심이 leftX/rightX 일 때 안쪽 가장자리가 캔버스에서 margin 이상 떨어져야 한다.
        private static void AssertFullyOutside(float canvasLeft, float canvasRight, float bandWidth, float margin, float leftX, float rightX)
        {
            Assert.LessOrEqual(leftX + bandWidth * 0.5f, canvasLeft - margin + Eps, "왼쪽 밴드 오른쪽 가장자리가 캔버스 왼쪽 밖이어야 한다");
            Assert.GreaterOrEqual(rightX - bandWidth * 0.5f, canvasRight + margin - Eps, "오른쪽 밴드 왼쪽 가장자리가 캔버스 오른쪽 밖이어야 한다");
        }

        [Test]
        public void ComputeHiddenX_대칭_캔버스_밴드폭_1280_마진_100이면_좌우_1380이다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-640f, 640f, 1280f, 100f, out float leftX, out float rightX);

            Assert.AreEqual(-1380f, leftX, Eps);
            Assert.AreEqual(1380f, rightX, Eps);
            AssertFullyOutside(-640f, 640f, 1280f, 100f, leftX, rightX);
        }

        [Test]
        public void ComputeHiddenX_좌인셋_88로_중심이_어긋난_비대칭_캔버스도_완전히_퇴장한다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-728f, 552f, 1280f, 100f, out float leftX, out float rightX);

            Assert.AreEqual(-1468f, leftX, Eps);
            Assert.AreEqual(1292f, rightX, Eps);
            AssertFullyOutside(-728f, 552f, 1280f, 100f, leftX, rightX);
        }

        [Test]
        public void ComputeHiddenX_반대쪽_인셋_비대칭에서도_완전히_퇴장한다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-552f, 728f, 1280f, 100f, out float leftX, out float rightX);

            Assert.AreEqual(-1292f, leftX, Eps);
            Assert.AreEqual(1468f, rightX, Eps);
            AssertFullyOutside(-552f, 728f, 1280f, 100f, leftX, rightX);
        }

        [Test]
        public void ComputeHiddenX_밴드가_캔버스보다_좁아도_완전히_퇴장한다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-640f, 640f, 400f, 100f, out float leftX, out float rightX);

            Assert.AreEqual(-940f, leftX, Eps);
            Assert.AreEqual(940f, rightX, Eps);
            AssertFullyOutside(-640f, 640f, 400f, 100f, leftX, rightX);
        }

        [Test]
        public void ComputeHiddenX_마진_0이면_밴드_가장자리가_캔버스_끝에_정확히_맞닿는다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-640f, 640f, 1280f, 0f, out float leftX, out float rightX);

            Assert.AreEqual(-1280f, leftX, Eps);
            Assert.AreEqual(1280f, rightX, Eps);
            Assert.AreEqual(-640f, leftX + 640f, Eps, "왼쪽 밴드 오른쪽 가장자리 = 캔버스 왼쪽 끝");
            Assert.AreEqual(640f, rightX - 640f, Eps, "오른쪽 밴드 왼쪽 가장자리 = 캔버스 오른쪽 끝");
        }

        [Test]
        public void ComputeHiddenX_마진이_클수록_바깥으로_그만큼_더_멀어진다()
        {
            SkillUnlockBannerView.ComputeHiddenX(-640f, 640f, 1280f, 100f, out float l1, out float r1);
            SkillUnlockBannerView.ComputeHiddenX(-640f, 640f, 1280f, 250f, out float l2, out float r2);

            Assert.AreEqual(150f, l1 - l2, Eps);
            Assert.AreEqual(150f, r2 - r1, Eps);
        }
    }
}
