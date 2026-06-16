using NUnit.Framework;
using Lair.Meta;

namespace Lair.Tests.EditMode
{
    //# MetaSession 클라우드 충돌 플래그(public static) 전이 검증(기획서 §3).
    //# 세션가드 계산(showConflict = pending && shown==false → shown=true)의 '실제 게이트'는
    //# VillageController.OpenCloud(private async View) 안에 있어 단위 테스트 불가 — 보고 항목 참조.
    //# 여기서는 플래그 의미와 가드 진리표를 고정한다.
    public class CloudConflictFlagTests
    {
        [SetUp]
        public void 초기화()
        {
            //# 정적 상태 격리 — 다른 테스트로 leak 방지.
            MetaSession.CloudConflictPending = false;
            MetaSession.CloudConflictPromptShownThisSession = false;
        }

        [TearDown]
        public void 정리()
        {
            MetaSession.CloudConflictPending = false;
            MetaSession.CloudConflictPromptShownThisSession = false;
        }

        [Test]
        public void 초기상태는_충돌없음이고_미노출이다()
        {
            Assert.IsFalse(MetaSession.CloudConflictPending);
            Assert.IsFalse(MetaSession.CloudConflictPromptShownThisSession);
        }

        [Test]
        public void 백업409모사_pending을_set한다()
        {
            //# 자동 백업이 409 받으면 호출부가 pending 을 올린다(VillageController.AutoBackup).
            MetaSession.CloudConflictPending = true;

            Assert.IsTrue(MetaSession.CloudConflictPending);
        }

        [Test]
        public void 복원성공모사_pending을_해제한다()
        {
            MetaSession.CloudConflictPending = true;

            //# 복원 성공 시 충돌 해소(VillageController.RestoreFromCloud).
            MetaSession.CloudConflictPending = false;

            Assert.IsFalse(MetaSession.CloudConflictPending);
        }

        [Test]
        public void TryConsumeConflictPrompt_pending이고_미노출이면_이번에_노출하고_플래그를_올린다()
        {
            MetaSession.CloudConflictPending = true;
            MetaSession.CloudConflictPromptShownThisSession = false;

            bool showConflict = MetaSession.TryConsumeConflictPrompt();

            Assert.IsTrue(showConflict);
            Assert.IsTrue(MetaSession.CloudConflictPromptShownThisSession);
        }

        [Test]
        public void TryConsumeConflictPrompt_세션내_재호출은_false다()
        {
            MetaSession.CloudConflictPending = true;
            MetaSession.CloudConflictPromptShownThisSession = false;

            bool first = MetaSession.TryConsumeConflictPrompt();
            bool second = MetaSession.TryConsumeConflictPrompt();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
        }

        [Test]
        public void TryConsumeConflictPrompt_이미노출했으면_재노출하지_않는다()
        {
            MetaSession.CloudConflictPending = true;
            MetaSession.CloudConflictPromptShownThisSession = true;

            Assert.IsFalse(MetaSession.TryConsumeConflictPrompt());
        }

        [Test]
        public void TryConsumeConflictPrompt_pending이_없으면_노출하지_않는다()
        {
            MetaSession.CloudConflictPending = false;
            MetaSession.CloudConflictPromptShownThisSession = false;

            Assert.IsFalse(MetaSession.TryConsumeConflictPrompt());
        }

        [Test]
        public void 노출후_복원해도_세션가드는_재노출막힘을_유지한다()
        {
            //# 한 번 노출(shown=true) 후 복원으로 pending 해제 → 다시 409 와도 같은 세션엔 재노출 안 함.
            MetaSession.CloudConflictPromptShownThisSession = true;
            MetaSession.CloudConflictPending = false;

            //# 새 충돌 발생.
            MetaSession.CloudConflictPending = true;

            Assert.IsFalse(MetaSession.TryConsumeConflictPrompt());
        }
    }
}
