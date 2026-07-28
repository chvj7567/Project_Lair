using NUnit.Framework;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# FirebaseSdkApiClient.PutSaveAsync 트랜잭션 충돌 판정(CloudSaveConflict.IsConflict)의 4행 상태표 회귀.
    //# 이 함수는 #if UNITY_INFRA_FIREBASE 밖(CloudSaveConflict.cs)에 있어 Firebase DLL 없이도 테스트 가능하다.
    //# 조회 실패(Failed) 경로는 트랜잭션 진입 전에 처리되므로 이 함수 범위 밖 — 여기서는 다루지 않는다.
    public class CloudSaveConflictTests
    {
        [Test]
        public void 문서없음_최초생성기대이고_서버에도_없으면_충돌아니다()
        {
            //# expected 없음(최초 백업) + 서버 문서도 없음 = 기대대로.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: false, expectedDocExists: false,
                snapExists: false, snapHasServerVersion: false, versionsEqual: false);

            Assert.IsFalse(conflict);
        }

        [Test]
        public void 문서없음_최초생성기대인데_서버엔_이미있으면_충돌이다()
        {
            //# 최초 생성 기대인데 사이에 다른 기기가 먼저 썼다.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: false, expectedDocExists: false,
                snapExists: true, snapHasServerVersion: true, versionsEqual: false);

            Assert.IsTrue(conflict);
        }

        [Test]
        public void 레거시문서_serverVersion없이_존재확인만되면_충돌아니다()
        {
            //# REST 시절 문서 — serverVersion 필드가 없어 baseline 비교 불가, 존재 여부만 확인하고 통과.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: false, expectedDocExists: true,
                snapExists: true, snapHasServerVersion: false, versionsEqual: false);

            Assert.IsFalse(conflict);
        }

        [Test]
        public void 레거시문서기대인데_서버문서가_사라졌으면_충돌이다()
        {
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: false, expectedDocExists: true,
                snapExists: false, snapHasServerVersion: false, versionsEqual: false);

            Assert.IsTrue(conflict);
        }

        [Test]
        public void 정상문서_버전일치하면_충돌아니다()
        {
            //# 마지막으로 본 serverVersion 과 서버의 현재 값이 같다 = 내가 마지막 쓴 사람.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: true, expectedDocExists: true,
                snapExists: true, snapHasServerVersion: true, versionsEqual: true);

            Assert.IsFalse(conflict);
        }

        [Test]
        public void 정상문서_버전불일치하면_충돌이다()
        {
            //# 다른 기기가 내가 마지막으로 본 버전 이후에 이미 썼다.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: true, expectedDocExists: true,
                snapExists: true, snapHasServerVersion: true, versionsEqual: false);

            Assert.IsTrue(conflict);
        }

        [Test]
        public void 정상문서기대인데_serverVersion필드가_사라졌으면_충돌이다()
        {
            //# 버전 비교 기준 자체가 사라진 이상 상태 — sameVersion 성립 불가로 충돌 처리.
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: true, expectedDocExists: true,
                snapExists: true, snapHasServerVersion: false, versionsEqual: false);

            Assert.IsTrue(conflict);
        }

        [Test]
        public void 정상문서기대인데_서버문서자체가_사라졌으면_충돌이다()
        {
            bool conflict = CloudSaveConflict.IsConflict(
                expectedHasValue: true, expectedDocExists: true,
                snapExists: false, snapHasServerVersion: false, versionsEqual: false);

            Assert.IsTrue(conflict);
        }

        [Test]
        public void IsRankedClearTime_양수는_유효한_클리어기록이다()
        {
            Assert.IsTrue(CloudSaveConflict.IsRankedClearTime(1));
            Assert.IsTrue(CloudSaveConflict.IsRankedClearTime(299999));
        }

        [Test]
        public void IsRankedClearTime_0은_유령문서로_판정한다()
        {
            Assert.IsFalse(CloudSaveConflict.IsRankedClearTime(0));
        }

        [Test]
        public void IsRankedClearTime_음수는_유령문서로_판정한다()
        {
            Assert.IsFalse(CloudSaveConflict.IsRankedClearTime(-1));
        }
    }
}
