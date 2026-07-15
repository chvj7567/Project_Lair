using NUnit.Framework;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# FirebaseApiClient 의 signUp 응답 파서(정적, 순수 함수) 검증 — 네트워크 없이 확인 가능.
    public class FirebaseApiClientParseTests
    {
        private const string SignUpBody =
            "{\"idToken\":\"eyJ.a.b\",\"refreshToken\":\"r123\",\"localId\":\"kZ9xAbC\",\"expiresIn\":\"3600\"}";

        [Test]
        public void signUp_응답에서_uid_idToken_refreshToken_추출()
        {
            Assert.AreEqual("kZ9xAbC", FirebaseApiClient.ParseSignUpUid(SignUpBody));
            Assert.AreEqual("eyJ.a.b", FirebaseApiClient.ParseSignUpIdToken(SignUpBody));
            Assert.AreEqual("r123", FirebaseApiClient.ParseSignUpRefreshToken(SignUpBody));
        }

        [Test]
        public void 빈_본문은_null_반환()
        {
            Assert.IsNull(FirebaseApiClient.ParseSignUpUid(string.Empty));
            Assert.IsNull(FirebaseApiClient.ParseSignUpUid(null));
        }

        //# 버그 재현 — securetoken 갱신 응답은 snake_case(id_token/refresh_token/user_id)로 signUp(camelCase)과 키가 다르다(Firebase 공식 문서).
        [Test]
        public void securetoken_갱신_응답은_snake_case_키에서_추출한다()
        {
            string refreshBody = "{\"expires_in\":\"3600\",\"token_type\":\"Bearer\",\"refresh_token\":\"AOvVZnew\",\"id_token\":\"eyJ.c.d\",\"user_id\":\"kZ9xAbC\",\"project_id\":\"123\"}";

            Assert.AreEqual("eyJ.c.d", FirebaseApiClient.ParseRefreshedIdToken(refreshBody));
            Assert.AreEqual("AOvVZnew", FirebaseApiClient.ParseRefreshedRefreshToken(refreshBody));
            Assert.AreEqual("kZ9xAbC", FirebaseApiClient.ParseRefreshedUid(refreshBody));
        }

        [Test]
        public void commit_409_는_충돌()
            => Assert.AreEqual(CloudSaveResult.Conflict, FirebaseApiClient.ClassifyCommit(409, ""));

        [Test]
        public void commit_400_FAILED_PRECONDITION_은_충돌()
            => Assert.AreEqual(CloudSaveResult.Conflict,
                FirebaseApiClient.ClassifyCommit(400, "{\"error\":{\"status\":\"FAILED_PRECONDITION\"}}"));

        [Test]
        public void commit_200_은_성공()
            => Assert.AreEqual(CloudSaveResult.Success, FirebaseApiClient.ClassifyCommit(200, "{}"));

        [Test]
        public void commit_500_은_실패()
            => Assert.AreEqual(CloudSaveResult.Failed, FirebaseApiClient.ClassifyCommit(500, ""));

        [Test]
        public void 세이브_문서에서_profile_문자열을_MetaProfile_로_복원한다()
        {
            //# MetaProfile 최소 JSON 을 stringValue 로 감싼 Firestore 문서.
            string inner = "{\\\"Version\\\":3}";
            string doc = "{\"fields\":{\"profile\":{\"stringValue\":\"" + inner + "\"}}}";
            MetaProfile p = FirebaseApiClient.ParseSaveProfile(doc);
            Assert.IsNotNull(p);
            Assert.AreEqual(3, p.Version);
        }

        [Test]
        public void commit_응답에서_writeResults_updateTime_을_추출한다()
        {
            string body = "{\"writeResults\":[{\"updateTime\":\"2026-07-15T03:17:56.123456Z\"}],\"commitTime\":\"2026-07-15T03:17:56.999999Z\"}";
            Assert.AreEqual("2026-07-15T03:17:56.123456Z", FirebaseApiClient.ParseCommitUpdateTime(body));
        }

        [Test]
        public void commit_응답에_writeResults_updateTime_없으면_commitTime_으로_폴백한다()
        {
            string body = "{\"writeResults\":[{}],\"commitTime\":\"2026-07-15T03:17:56.999999Z\"}";
            Assert.AreEqual("2026-07-15T03:17:56.999999Z", FirebaseApiClient.ParseCommitUpdateTime(body));
        }

        [Test]
        public void commit_응답이_비면_null_을_반환한다()
        {
            Assert.IsNull(FirebaseApiClient.ParseCommitUpdateTime(""));
            Assert.IsNull(FirebaseApiClient.ParseCommitUpdateTime(null));
        }

        [Test]
        public void runQuery_응답을_행리스트로_파싱한다()
        {
            string body =
              "[{\"document\":{\"fields\":{\"uid\":{\"stringValue\":\"u1\"},\"displayName\":{\"stringValue\":\"영주 #A3F9\"},\"clearTimeMs\":{\"integerValue\":\"92500\"},\"hero\":{\"stringValue\":\"Knight\"}}}}," +
              "{\"document\":{\"fields\":{\"uid\":{\"stringValue\":\"u2\"},\"displayName\":{\"stringValue\":\"영주 #B1C2\"},\"clearTimeMs\":{\"integerValue\":\"93000\"},\"hero\":{\"stringValue\":\"Knight\"}}}}]";
            System.Collections.Generic.List<RankingRowDto> rows = FirebaseApiClient.ParseRunQueryRows(body);
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("u1", rows[0].uid);
            Assert.AreEqual(92500, rows[0].clearTimeMs);
            Assert.AreEqual("영주 #A3F9", rows[0].displayName);
        }

        [Test]
        public void aggregation_COUNT_결과를_파싱한다()
        {
            string body = "[{\"result\":{\"aggregateFields\":{\"count\":{\"integerValue\":\"7\"}}}}]";
            Assert.AreEqual(7, FirebaseApiClient.ParseAggregationCount(body));
        }

        //# clearTimeMs<=0(유령 리더보드 문서 등)은 유효 클리어가 아님 — GetMyRankAsync 가 랭크 미표시로 걸러야 한다.
        [Test]
        public void 유효클리어시간_판정_0과음수는_랭크아님_양수만_랭크()
        {
            Assert.IsFalse(FirebaseApiClient.IsRankedClearTime(0));
            Assert.IsFalse(FirebaseApiClient.IsRankedClearTime(-1));
            Assert.IsTrue(FirebaseApiClient.IsRankedClearTime(1));
            Assert.IsTrue(FirebaseApiClient.IsRankedClearTime(92500));
        }

        [Test]
        public void 표시명_commit_409_또는_400FAILEDPRECONDITION_는_Taken()
        {
            Assert.AreEqual(DisplayNameStatus.Taken, FirebaseApiClient.ClassifyDisplayName(409, ""));
            Assert.AreEqual(DisplayNameStatus.Taken, FirebaseApiClient.ClassifyDisplayName(400, "FAILED_PRECONDITION"));
        }

        [Test]
        public void 표시명_commit_200_은_Success()
            => Assert.AreEqual(DisplayNameStatus.Success, FirebaseApiClient.ClassifyDisplayName(200, "{}"));

        [Test]
        public void 표시명_commit_기타_400_은_Invalid_5xx0_은_Offline()
        {
            Assert.AreEqual(DisplayNameStatus.Invalid, FirebaseApiClient.ClassifyDisplayName(400, "{\"error\":{\"status\":\"INVALID_ARGUMENT\"}}"));
            Assert.AreEqual(DisplayNameStatus.Offline, FirebaseApiClient.ClassifyDisplayName(0, ""));
        }

        //# 회귀방지 — 리더보드 write 에 updateMask 없으면 문서 전체 치환되어 clearTimeMs/hero 가 증발한다.
        [Test]
        public void 표시명_commit_리더보드write는_updateMask로_displayName만_patch한다()
        {
            string lockFields = "{\"fields\":{\"uid\":{\"stringValue\":\"u1\"}}}";
            string lbFields = "{\"fields\":{\"displayName\":{\"stringValue\":\"영주 #A3F9\"}}}";
            string body = FirebaseApiClient.BuildDisplayNameCommit("lockPath", lockFields, "lbPath", lbFields);
            Assert.IsTrue(body.Contains("\"updateMask\":{\"fieldPaths\":[\"displayName\"]}"), body);
            //# 잠금 write 는 신규 생성이라 updateMask 없이 exists:false precondition 만.
            Assert.IsTrue(body.Contains("\"currentDocument\":{\"exists\":false}"), body);
        }

        //# 회귀방지 — 문서ID/JSON 파손 문자(/ " \)는 commit 전에 걸러야 한다.
        [Test]
        public void 표시명_기술적파손문자_슬래시_따옴표_백슬래시는_거부한다()
        {
            Assert.IsFalse(FirebaseApiClient.IsValidDisplayNameChars("a/b"));
            Assert.IsFalse(FirebaseApiClient.IsValidDisplayNameChars("a\"b"));
            Assert.IsFalse(FirebaseApiClient.IsValidDisplayNameChars("a\\b"));
            Assert.IsTrue(FirebaseApiClient.IsValidDisplayNameChars("영주 #A3F9"));
        }
    }
}
