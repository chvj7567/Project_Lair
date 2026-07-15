using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# Firebase Auth + Firestore REST 로 ILairApiClient 를 구현. 모든 쓰기는 documents:commit(POST).
    public class FirebaseApiClient : ILairApiClient
    {
        private readonly NetworkConfig _config;
        private string _saveUpdateTime;   //# GetSave 시 캐시 — PutSave precondition(충돌 감지)용.

        public FirebaseApiClient(NetworkConfig config) { _config = config; }

        private int Timeout => _config.TimeoutSec;
        private string Key => _config.FirebaseApiKey;
        private string DocBase => $"https://firestore.googleapis.com/v1/projects/{_config.FirebaseProjectId}/databases/(default)/documents";

        public async Task<bool> AuthenticateAsync()
        {
            //# refreshToken 있으면 갱신 우선.
            if (string.IsNullOrEmpty(AuthTokenStore.RefreshToken) == false)
            {
                if (await RefreshAsync())
                    return true;
            }
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={Key}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, "{\"returnSecureToken\":true}", null, Timeout);
            if (res.IsSuccess == false)
            {
                Debug.LogWarning($"[FirebaseApiClient] 익명 인증 실패: {res.StatusCode} {res.Error}");
                return false;
            }
            string uid = ParseSignUpUid(res.Body);
            string idToken = ParseSignUpIdToken(res.Body);
            string refresh = ParseSignUpRefreshToken(res.Body);
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(idToken))
                return false;
            AuthTokenStore.SaveUid(uid);
            AuthTokenStore.SaveToken(idToken);
            AuthTokenStore.SaveRefreshToken(refresh);
            return true;
        }

        private async Task<bool> RefreshAsync()
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={Key}";
            //# securetoken 엔드포인트는 OAuth2 표준대로 form-urlencoded 본문을 요구 — Content-Type 을 명시적으로 맞춘다.
            string body = $"grant_type=refresh_token&refresh_token={AuthTokenStore.RefreshToken}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, body, null, Timeout, "application/x-www-form-urlencoded");
            if (res.IsSuccess == false)
                return false;
            string idToken = ParseRefreshedIdToken(res.Body);
            if (string.IsNullOrEmpty(idToken))
                return false;
            AuthTokenStore.SaveToken(idToken);
            string refresh = ParseRefreshedRefreshToken(res.Body);
            if (string.IsNullOrEmpty(refresh) == false)
                AuthTokenStore.SaveRefreshToken(refresh);
            string uid = ParseRefreshedUid(res.Body);
            if (string.IsNullOrEmpty(uid) == false)
                AuthTokenStore.SaveUid(uid);
            return true;
        }

        public static string ParseSignUpUid(string body) => Field(body, "localId");
        public static string ParseSignUpIdToken(string body) => Field(body, "idToken");
        public static string ParseSignUpRefreshToken(string body) => Field(body, "refreshToken");

        //# securetoken 갱신 응답은 snake_case(id_token/refresh_token/user_id) — signUp(camelCase)과 키가 다르다(Firebase 공식 문서).
        public static string ParseRefreshedIdToken(string body) => Coalesce(Field(body, "idToken"), Field(body, "id_token"));
        public static string ParseRefreshedRefreshToken(string body) => Coalesce(Field(body, "refreshToken"), Field(body, "refresh_token"));
        public static string ParseRefreshedUid(string body) => Coalesce(Field(body, "localId"), Field(body, "user_id"));

        private static string Coalesce(string a, string b) => string.IsNullOrEmpty(a) == false ? a : b;

        private static string Field(string body, string name)
        {
            if (string.IsNullOrEmpty(body))
                return null;
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                body, "\"" + name + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        public async Task<SaveResponseBody> GetSaveAsync()
        {
            string url = $"{DocBase}/saves/{AuthTokenStore.Uid}";
            CHHttpResult res = await CHMHttpNetwork.GetAsync(url, AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
            {
                _saveUpdateTime = null;
                return null;
            }
            _saveUpdateTime = FirestoreJson.ExtractUpdateTime(res.Body);
            MetaProfile profile = ParseSaveProfile(res.Body);
            if (profile == null)
                return null;
            return new SaveResponseBody
            {
                profile = profile,
                schemaVersion = (int)FirestoreJson.ExtractInt(res.Body, "schemaVersion"),
                updatedAt = _saveUpdateTime,
            };
        }

        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            //# 첫 백업 전 서버 기준시각(base version)을 시딩 — 복귀 유저의 exists:false 오충돌 방지.
            //# GetSave 가 404(신규 유저)면 _saveUpdateTime 은 null 로 남아 exists:false(정상 최초생성) 경로 유지.
            if (string.IsNullOrEmpty(_saveUpdateTime))
                await GetSaveAsync();
            string docPath = $"projects/{_config.FirebaseProjectId}/databases/(default)/documents/saves/{AuthTokenStore.Uid}";
            string fields = FirestoreJson.Document(
                ("profile", FirestoreJson.StringField(JsonUtility.ToJson(profile))),
                ("schemaVersion", FirestoreJson.IntField(profile.Version)),
                ("updatedAt", FirestoreJson.StringField(clientUpdatedAt)));
            //# precondition: 캐시된 updateTime 있으면 그 시점 기준, 없으면 최초 생성(exists=false).
            string precond = string.IsNullOrEmpty(_saveUpdateTime)
                ? "{\"exists\":false}"
                : "{\"updateTime\":\"" + _saveUpdateTime + "\"}";
            string url = $"{DocBase}:commit";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, BuildSaveCommit(docPath, fields, precond), AuthTokenStore.Token, Timeout);
            CloudSaveResult result = ClassifyCommit(res.StatusCode, res.Body);
            //# 성공 시 서버가 돌려준 새 updateTime 을 재캐시 — 세션 내 2번째+ 백업의 자기충돌 방지.
            //# 파싱 실패(null)면 다음 PutSave 가 다시 GetSave 로 재시딩(방어적).
            if (result == CloudSaveResult.Success)
                _saveUpdateTime = ParseCommitUpdateTime(res.Body);
            return result;
        }

        //# :commit 본문 조립 — write 항목의 currentDocument precondition 은 update 의 형제로 들어간다.
        private static string BuildSaveCommit(string docPath, string fieldsJson, string precondJson)
        {
            //# fieldsJson = {"fields":{...}} → 겉 중괄호 제거해 update 객체 내부(name + fields)로 병합.
            string fieldsInner = fieldsJson.Substring(1, fieldsJson.Length - 2);
            return "{\"writes\":[{\"update\":{\"name\":\"" + docPath + "\"," + fieldsInner + "},\"currentDocument\":" + precondJson + "}]}";
        }

        public static CloudSaveResult ClassifyCommit(long statusCode, string body)
        {
            if (statusCode == 409)
                return CloudSaveResult.Conflict;
            if (statusCode == 400 && (body ?? string.Empty).Contains("FAILED_PRECONDITION"))
                return CloudSaveResult.Conflict;
            if (statusCode >= 200 && statusCode < 300)
                return CloudSaveResult.Success;
            return CloudSaveResult.Failed;
        }

        //# documents:commit 응답에서 새 버전시각 추출 — writeResults[0].updateTime 우선, 없으면 commitTime 폴백, 둘 다 없으면 null.
        public static string ParseCommitUpdateTime(string body)
        {
            if (string.IsNullOrEmpty(body))
                return null;
            //# writeResults[0].updateTime 추출은 ExtractUpdateTime 과 동일 정규식 — 단일 진실로 재사용(DRY).
            string updateTime = FirestoreJson.ExtractUpdateTime(body);
            if (string.IsNullOrEmpty(updateTime) == false)
                return updateTime;
            System.Text.RegularExpressions.Match commit = System.Text.RegularExpressions.Regex.Match(
                body, "\"commitTime\"\\s*:\\s*\"([^\"]+)\"");
            return commit.Success ? commit.Groups[1].Value : null;
        }

        public static MetaProfile ParseSaveProfile(string documentJson)
        {
            string profileJson = FirestoreJson.ExtractString(documentJson, "profile");
            if (string.IsNullOrEmpty(profileJson))
                return null;
            try
            {
                return JsonUtility.FromJson<MetaProfile>(profileJson);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseApiClient] profile 파싱 실패: {e.Message}");
                return null;
            }
        }

        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            string docPath = $"projects/{_config.FirebaseProjectId}/databases/(default)/documents/leaderboard/{AuthTokenStore.Uid}";
            string fields = FirestoreJson.Document(
                ("uid", FirestoreJson.StringField(AuthTokenStore.Uid)),
                ("displayName", FirestoreJson.StringField(displayName)),
                ("clearTimeMs", FirestoreJson.IntField(clearTimeMs)),
                ("hero", FirestoreJson.StringField(hero)));
            string commit = "{\"writes\":[{\"update\":{\"name\":\"" + docPath + "\"," + fields.Substring(1, fields.Length - 2) + "}}]}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:commit", commit, AuthTokenStore.Token, Timeout);
            return res.IsSuccess;
        }

        public async Task<List<RankingRowDto>> GetTopAsync(int top)
        {
            string query = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"leaderboard\"}],\"orderBy\":[{\"field\":{\"fieldPath\":\"clearTimeMs\"},\"direction\":\"ASCENDING\"}],\"limit\":" + top + "}}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:runQuery", query, AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
                return new List<RankingRowDto>();
            List<RankingRowDto> rows = ParseRunQueryRows(res.Body);
            //# runQuery 는 rank 필드를 안 내려준다 — clearTimeMs ASC 정렬 순서 = 순위(1부터).
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].rank = i + 1;
            }
            return rows;
        }

        public async Task<List<RankingRowDto>> GetMyRankAsync()
        {
            //# 내 기록 조회 → COUNT(clearTimeMs < 내기록) → 등수 = count+1. 실패 시 빈 리스트.
            CHHttpResult mine = await CHMHttpNetwork.GetAsync($"{DocBase}/leaderboard/{AuthTokenStore.Uid}", AuthTokenStore.Token, Timeout);
            if (mine.IsSuccess == false)
                return new List<RankingRowDto>();
            long myMs = FirestoreJson.ExtractInt(mine.Body, "clearTimeMs");
            //# clearTimeMs<=0 은 유효한 클리어 기록이 아님(유령 문서 등) — 거짓 "1위 00:00" 방지.
            if (IsRankedClearTime(myMs) == false)
                return new List<RankingRowDto>();
            string agg ="{\"structuredAggregationQuery\":{\"aggregations\":[{\"count\":{},\"alias\":\"count\"}],\"structuredQuery\":{\"from\":[{\"collectionId\":\"leaderboard\"}],\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"clearTimeMs\"},\"op\":\"LESS_THAN\",\"value\":{\"integerValue\":\"" + myMs + "\"}}}}}}";
            CHHttpResult cnt = await CHMHttpNetwork.PostAsync($"{DocBase}:runAggregationQuery", agg, AuthTokenStore.Token, Timeout);
            //# 집계 실패면 count 0 → rank 1(거짓 "#1")로 새므로 빈 리스트로 차단(mine.IsSuccess 가드와 대칭).
            if (cnt.IsSuccess == false)
                return new List<RankingRowDto>();
            long rank = ParseAggregationCount(cnt.Body) + 1;
            RankingRowDto myRow = new RankingRowDto
            {
                rank = rank,
                uid = AuthTokenStore.Uid,
                displayName = FirestoreJson.ExtractString(mine.Body, "displayName"),
                clearTimeMs = (int)myMs,
                hero = FirestoreJson.ExtractString(mine.Body, "hero"),
            };
            return new List<RankingRowDto> { myRow };
        }

        //# runQuery 응답 [{document:{fields:{...}}}, ...] → 행 리스트. document 블록별로 분해, 실패해도 빈 리스트로 방어.
        public static List<RankingRowDto> ParseRunQueryRows(string body)
        {
            List<RankingRowDto> rows = new List<RankingRowDto>();
            if (string.IsNullOrEmpty(body))
                return rows;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(body, "\"document\"\\s*:\\s*(\\{.*?\\}\\s*\\}\\s*\\})"))
            {
                string doc = m.Groups[1].Value;
                rows.Add(new RankingRowDto
                {
                    uid = FirestoreJson.ExtractString(doc, "uid"),
                    displayName = FirestoreJson.ExtractString(doc, "displayName"),
                    clearTimeMs = (int)FirestoreJson.ExtractInt(doc, "clearTimeMs"),
                    hero = FirestoreJson.ExtractString(doc, "hero"),
                });
            }
            return rows;
        }

        //# 유효 클리어 시간 판정 — clearTimeMs 는 소요시간(ms)이라 0/음수는 실제 클리어일 수 없다(유령 문서 걸러냄).
        public static bool IsRankedClearTime(long ms) => ms > 0;

        //# runAggregationQuery COUNT 결과 → 정수. 실패 시 0.
        public static long ParseAggregationCount(string body)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                body ?? string.Empty, "\"count\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"?(\\d+)\"?");
            return m.Success && long.TryParse(m.Groups[1].Value, out long v) ? v : 0;
        }

        public async Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName)
        {
            string norm = displayName == null ? string.Empty : displayName.Trim();
            if (string.IsNullOrEmpty(norm))
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            //# 문서ID/JSON 파손 문자(/ " \) 차단 — 잠금 컬렉션 평평화 전제·commit body JSON 보호. 제품 charset 정책 아님.
            if (IsValidDisplayNameChars(norm) == false)
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            string uid = AuthTokenStore.Uid;
            string prj = _config.FirebaseProjectId;
            string newLockPath = $"projects/{prj}/databases/(default)/documents/displayNames/{norm}";
            string lbPath = $"projects/{prj}/databases/(default)/documents/leaderboard/{uid}";
            string lockFields = FirestoreJson.Document(("uid", FirestoreJson.StringField(uid)));
            string lbFields = FirestoreJson.Document(("displayName", FirestoreJson.StringField(norm)));
            string body = BuildDisplayNameCommit(newLockPath, lockFields, lbPath, lbFields);
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:commit", body, AuthTokenStore.Token, Timeout);
            DisplayNameStatus status = ClassifyDisplayName(res.StatusCode, res.Body);
            return status == DisplayNameStatus.Success ? new DisplayNameResult(status, norm) : DisplayNameResult.Of(status);
        }

        //# writes: (1) 새 이름 잠금 생성(exists:false → 중복이면 precondition 실패) (2) 리더보드 displayName 갱신.
        //# 옛 이름 잠금 삭제는 의도적으로 생략(로컬이 직전 이름을 모를 수 있음, 잔여 잠금은 무해) — 유일성은 (1)이 담당.
        //# 리더보드 write 는 updateMask 로 displayName 만 patch — mask 없으면 문서 전체 치환되어 clearTimeMs/hero 가 증발한다.
        public static string BuildDisplayNameCommit(string lockPath, string lockFields, string lbPath, string lbFields)
        {
            string lockInner = lockFields.Substring(1, lockFields.Length - 2);
            string lbInner = lbFields.Substring(1, lbFields.Length - 2);
            return "{\"writes\":[" +
                "{\"update\":{\"name\":\"" + lockPath + "\"," + lockInner + "},\"currentDocument\":{\"exists\":false}}," +
                "{\"update\":{\"name\":\"" + lbPath + "\"," + lbInner + "},\"updateMask\":{\"fieldPaths\":[\"displayName\"]}}" +
                "]}";
        }

        //# 기술적 파손 문자 3종(/ " \)만 차단 — / 는 문서ID 경로 분리, " \ 는 commit body JSON 파손.
        public static bool IsValidDisplayNameChars(string norm)
        {
            if (string.IsNullOrEmpty(norm))
                return false;
            return norm.IndexOf('/') < 0 && norm.IndexOf('"') < 0 && norm.IndexOf('\\') < 0;
        }

        public static DisplayNameStatus ClassifyDisplayName(long statusCode, string body)
        {
            if (statusCode == 409 || (statusCode == 400 && (body ?? string.Empty).Contains("FAILED_PRECONDITION")))
                return DisplayNameStatus.Taken;
            if (statusCode >= 200 && statusCode < 300)
                return DisplayNameStatus.Success;
            if (statusCode == 400)
                return DisplayNameStatus.Invalid;
            return DisplayNameStatus.Offline;
        }
    }
}
