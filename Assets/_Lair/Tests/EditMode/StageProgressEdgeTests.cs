using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.EditMode
{
    //# StageProgress 순수 헬퍼 엣지 — ResolveClearedStage 방어값 · ScaleStat 반올림 경계 · IsUnlocked 해금 경계
    //# (hero-stage-variant plan Task 6, 기획서 §2.1·§4.6). 핀값 전부/기본 max/5종점은 StageProgressionTests 가 커버.
    //# IsUnlocked 케이스는 폐기된 StageSelectCellData(Edge)Tests 의 해금 경계 검증을 재home 한 것(기획서 §4.6 지정).
    public class StageProgressEdgeTests
    {
        [Test]
        public void ResolveClearedStage_는_음수_0_인자를_안전하게_처리한다()
        {
            Assert.AreEqual(0, StageProgress.ResolveClearedStage(0, 0));   //# 미클리어 유지
            Assert.AreEqual(2, StageProgress.ResolveClearedStage(-1, 2));  //# 음수 cleared 방어
            Assert.AreEqual(3, StageProgress.ResolveClearedStage(3, 0));   //# justCleared 0 → 기존 유지
        }

        [Test]
        public void ResolveClearedStage_는_비정상_6이상_입력도_5로_상한한다()
        {
            //# SelectedStage 가 어떤 경위로 6 이 되어도 클리어 최고치는 5 를 넘지 않는다(spec §6.1).
            Assert.AreEqual(5, StageProgress.ResolveClearedStage(3, 6));
            Assert.AreEqual(5, StageProgress.ResolveClearedStage(5, 99));
            Assert.AreEqual(5, StageProgress.ResolveClearedStage(5, 3)); //# 5 클리어 후 낮은 재도전 → 5 유지
        }

        [Test]
        public void ScaleStat_은_소수_0_5이상은_올림_0_5미만은_내림한다()
        {
            //# round-half-up: base×mul 의 소수부 0.5 경계.
            Assert.AreEqual(3, StageProgress.ScaleStat(2, 1.25f)); //# 2.5 → 3 (정확히 0.5 → 올림)
            Assert.AreEqual(2, StageProgress.ScaleStat(2, 1.24f)); //# 2.48 → 2 (0.5 미만 → 내림)
            Assert.AreEqual(3, StageProgress.ScaleStat(2, 1.30f)); //# 2.6 → 3 (0.5 초과 → 올림)
        }

        [Test]
        public void ScaleStat_은_배수0이하여도_최소1을_보장한다()
        {
            //# baseline × 0 = 0 → Max(1, …) 로 1 클램프(스탯 0 방지).
            Assert.AreEqual(1, StageProgress.ScaleStat(4000, 0f));
            Assert.AreEqual(1, StageProgress.ScaleStat(4000, -1f));
            Assert.AreEqual(1, StageProgress.ScaleStat(1, 0.1f)); //# 0.1 → floor(0.6)=0 → Max(1,0)=1
        }

        //# --- 해금 경계 (IsUnlocked) — 폐기된 StageSelectCellData(Edge)Tests 재home ---

        [Test]
        public void IsUnlocked_ClearedStage0이면_스테이지1만_해금이고_2는_잠금이다()
        {
            //# 신규 프로필(ClearedStage=0) — stage <= 0+1 = 1 까지만 해금.
            Assert.IsTrue(StageProgress.IsUnlocked(1, 0));
            Assert.IsFalse(StageProgress.IsUnlocked(2, 0));
            Assert.IsFalse(StageProgress.IsUnlocked(5, 0));
        }

        [Test]
        public void IsUnlocked_ClearedStage1이면_스테이지2까지_해금되고_3은_잠금이다()
        {
            Assert.IsTrue(StageProgress.IsUnlocked(1, 1));
            Assert.IsTrue(StageProgress.IsUnlocked(2, 1)); //# ClearedStage+1
            Assert.IsFalse(StageProgress.IsUnlocked(3, 1));
        }

        [Test]
        public void IsUnlocked_ClearedStage2이면_스테이지3까지_해금되고_4_5는_잠금이다()
        {
            Assert.IsTrue(StageProgress.IsUnlocked(3, 2)); //# ClearedStage+1
            Assert.IsFalse(StageProgress.IsUnlocked(4, 2));
            Assert.IsFalse(StageProgress.IsUnlocked(5, 2));
        }

        [Test]
        public void IsUnlocked_ClearedStage4이면_5스테이지_전부_해금된다()
        {
            for (int stage = 1; stage <= 5; stage++)
            {
                Assert.IsTrue(StageProgress.IsUnlocked(stage, 4), $"stage {stage} 은 해금이어야 한다(4+1=5)");
            }
        }

        [Test]
        public void IsUnlocked_ClearedStage5_전체클리어면_경계상한을_자연처리한다()
        {
            //# 5 전체 클리어 — 실제 캐러셀은 1~5 로만 이동하나, 공식상 6 도 true(6<=6). 상한은 호출부 클램프가 담당.
            for (int stage = 1; stage <= 5; stage++)
            {
                Assert.IsTrue(StageProgress.IsUnlocked(stage, 5));
            }
            Assert.IsTrue(StageProgress.IsUnlocked(6, 5)); //# 경계 상한 자연 처리(예외 없음)
        }

        [Test]
        public void IsUnlocked_음수_0_인자도_예외없이_공식을_따른다()
        {
            //# 순수 int 비교라 방어 예외 없음 — clearedStage 음수면 stage1 도 잠금(공식 그대로 박제).
            Assert.IsFalse(StageProgress.IsUnlocked(1, -1)); //# 1 <= -1+1=0 → false
            Assert.IsTrue(StageProgress.IsUnlocked(0, 0));   //# 0 <= 1 → true (무의미 stage 지만 예외 없음)
            Assert.IsTrue(StageProgress.IsUnlocked(1, 0));
        }
    }
}
