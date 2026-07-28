#if UNITY_INFRA_FIREBASE
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Firebase.Auth;
using Firebase.Firestore;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# Firebase Auth + Firestore SDK 로 ILairApiClient 를 구현. Firebase.* 타입은 이 클래스 밖으로 나가지 않는다.
    //# 접속 설정은 google-services.json 이 담당 — 별도 설정 SO 없음.
    public class FirebaseSdkApiClient : ILairApiClient
    {
        private const string SavesCollection = "saves";
        private const string LeaderboardCollection = "leaderboard";
        private const string DisplayNamesCollection = "displayNames";

        //# Firestore Unity SDK 는 DocumentSnapshot 에 REST 와 달리 UpdateTime 이 없다 —
        //# 직접 관리하는 서버 타임스탬프 필드로 그 역할을 대신한다(문서 하단 GetSaveAsync/PutSaveAsync 주석 참조).
        private const string ServerVersionField = "serverVersion";

        //# GetSave 시 캐시 — PutSave 트랜잭션의 충돌 판정 기준(마지막으로 본 버전).
        private Timestamp? _saveUpdateTime;
        //# 문서 자체 존재 여부(버전 필드 유무와 무관) — REST 시절 문서(serverVersion 필드 없음) 호환용.
        private bool _saveDocExists;

        private static FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

        //# 내부 조회 3상태 — "문서 없음"과 "조회 실패(통신 오류 등)"를 구분한다. spec §6 에러 매핑상
        //# 통신 실패는 Failed 여야지 PutSave 의 "최초 생성 기대" 판정(Conflict 오탐)으로 새면 안 된다.
        private enum SaveFetchStatus
        {
            Found,
            NotFound,
            Failed,
        }

        //# GetSaveAsync(public)와 PutSaveAsync 시딩이 공유하는 내부 조회 결과 — 로직 중복 방지.
        private readonly struct SaveFetchResult
        {
            public readonly SaveFetchStatus Status;
            public readonly SaveResponseBody Body;

            public SaveFetchResult(SaveFetchStatus status, SaveResponseBody body)
            {
                Status = status;
                Body = body;
            }

            public static SaveFetchResult Of(SaveFetchStatus status) => new SaveFetchResult(status, null);
        }

        public async Task<bool> AuthenticateAsync()
        {
            bool ready = await CHMFirebase.Instance.EnsureReadyAsync();
            if (ready == false)
                return false;

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            //# 이미 로그인돼 있으면(SDK 가 자격증명을 영속화) 재로그인하지 않는다 — uid 유지가 핵심.
            if (auth.CurrentUser == null)
            {
                try
                {
                    //# 반환값을 쓰지 않는다 — SDK 버전에 따라 Task<FirebaseUser>/Task<AuthResult> 로 갈리므로
                    //# CurrentUser 로 읽어야 양쪽에서 컴파일된다.
                    await auth.SignInAnonymouslyAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FirebaseSdkApiClient] 익명 인증 실패: {e.Message}");
                    return false;
                }
            }

            if (auth.CurrentUser == null)
                return false;
            AuthTokenStore.SaveUid(auth.CurrentUser.UserId);
            return true;
        }

        //# 현재 로그인 uid — 미인증이면 빈 문자열.
        private static string Uid
        {
            get
            {
                FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
                return user == null ? string.Empty : user.UserId;
            }
        }

        //# 클라우드 세이브 조회 — 없으면 null, 통신 실패면 null(ILairApiClient 계약 — 둘을 구분하지 않는다).
        //# 내부적으로는 FetchSaveAsync 의 3상태를 구분해서 쓴다(PutSave 시딩이 그 구분을 필요로 함 — 아래 참조).
        public async Task<SaveResponseBody> GetSaveAsync()
        {
            SaveFetchResult result = await FetchSaveAsync();
            return result.Status == SaveFetchStatus.Found ? result.Body : null;
        }

        //# GetSaveAsync/PutSaveAsync 시딩이 공유하는 실제 조회 로직. serverVersion 캐시를 함께 갱신(PutSave 충돌 판정 기준).
        //# GetValue<T> 두번째 인자(ServerTimestampBehavior)는 이 SDK 시그니처상 필수 — 일반 필드엔 의미 없어 Estimate 고정.
        private async Task<SaveFetchResult> FetchSaveAsync()
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return SaveFetchResult.Of(SaveFetchStatus.Failed);
            try
            {
                //# 트랜잭션 밖 조회는 캐시가 아니라 서버 원본을 봐야 baseline 이 어긋나지 않는다.
                DocumentSnapshot snap = await Db.Collection(SavesCollection).Document(uid).GetSnapshotAsync(Source.Server);
                if (snap.Exists == false)
                {
                    //# 문서 없음 = "세이브 없음". 최초 생성 경로를 위해 캐시를 비운다.
                    _saveUpdateTime = null;
                    _saveDocExists = false;
                    return SaveFetchResult.Of(SaveFetchStatus.NotFound);
                }
                _saveDocExists = true;
                //# serverVersion 필드가 없으면(REST 시절 문서) baseline 미확보 — PutSave 가 존재여부만으로 판정하는 경로로 빠진다.
                _saveUpdateTime = snap.ContainsField(ServerVersionField)
                    ? snap.GetValue<Timestamp>(ServerVersionField, ServerTimestampBehavior.Estimate)
                    : (Timestamp?)null;
                string profileJson = snap.ContainsField("profile") ? snap.GetValue<string>("profile", ServerTimestampBehavior.Estimate) : null;
                if (string.IsNullOrEmpty(profileJson))
                    return SaveFetchResult.Of(SaveFetchStatus.Found);
                MetaProfile profile = JsonUtility.FromJson<MetaProfile>(profileJson);
                if (profile == null)
                    return SaveFetchResult.Of(SaveFetchStatus.Found);
                SaveResponseBody body = new SaveResponseBody
                {
                    profile = profile,
                    schemaVersion = snap.ContainsField("schemaVersion") ? (int)snap.GetValue<long>("schemaVersion", ServerTimestampBehavior.Estimate) : 0,
                    updatedAt = snap.ContainsField("updatedAt") ? snap.GetValue<string>("updatedAt", ServerTimestampBehavior.Estimate) : null,
                };
                return new SaveFetchResult(SaveFetchStatus.Found, body);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 조회 실패: {e.Message}");
                _saveUpdateTime = null;
                _saveDocExists = false;
                return SaveFetchResult.Of(SaveFetchStatus.Failed);
            }
        }

        //# 클라우드 세이브 저장 — RunTransactionAsync 로 충돌(Conflict) 판정. 델리게이트 안에서는 예외를 던지지 않고
        //# 플래그만 세우고 return 으로 빠진다(쓰기 없이 무해하게 커밋 — 재시도/AggregateException 에 의한 원인 뒤섞임 방지).
        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            if (profile == null)
                return CloudSaveResult.Failed;
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return CloudSaveResult.Failed;

            //# 첫 백업 전 서버 기준시각(base version)을 시딩 — 복귀 유저의 오충돌 방지.
            //# 이번 세션에서 한 번도 조회한 적 없을 때만(둘 다 비확보) — 문서가 없으면 캐시는 null 로 남아 "최초 생성" 경로가 된다.
            //# 시딩 조회 자체가 실패(통신 오류 등)하면 트랜잭션에 들어가지 않고 즉시 Failed — "문서 없음"과 혼동해
            //# 정상 문서를 "최초 생성 기대" 판정으로 오인, 거짓 Conflict 를 내면 안 된다(spec §6 에러 매핑).
            if (_saveUpdateTime.HasValue == false && _saveDocExists == false)
            {
                SaveFetchResult seed = await FetchSaveAsync();
                if (seed.Status == SaveFetchStatus.Failed)
                    return CloudSaveResult.Failed;
            }

            Timestamp? expected = _saveUpdateTime;
            bool expectedDocExists = _saveDocExists;
            DocumentReference doc = Db.Collection(SavesCollection).Document(uid);
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "profile", JsonUtility.ToJson(profile) },
                { "schemaVersion", profile.Version },
                { "updatedAt", clientUpdatedAt },
                //# 서버가 커밋 시점에 실제 타임스탬프로 치환 — 이 값이 이후 충돌 판정의 baseline.
                { ServerVersionField, FieldValue.ServerTimestamp },
            };

            try
            {
                bool conflict = false;
                await Db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot snap = await transaction.GetSnapshotAsync(doc);
                    //# 충돌 판정 — "내가 마지막으로 본 버전 이후에 누가 썼는가".
                    if (expected.HasValue)
                    {
                        //# 기대: 문서가 존재하고 serverVersion 이 캐시와 같다.
                        bool sameVersion = snap.Exists && snap.ContainsField(ServerVersionField)
                            && snap.GetValue<Timestamp>(ServerVersionField, ServerTimestampBehavior.Estimate).Equals(expected.Value);
                        if (sameVersion == false)
                        {
                            conflict = true;
                            return;
                        }
                    }
                    else if (expectedDocExists)
                    {
                        //# 레거시(REST) 문서 — serverVersion 필드가 없어 baseline 비교 불가. 존재 여부만 확인하고 통과,
                        //# 이 쓰기로 serverVersion 필드가 신설되며 이후 백업부터는 정상 버전 비교 경로를 탄다.
                        if (snap.Exists == false)
                        {
                            conflict = true;
                            return;
                        }
                    }
                    else
                    {
                        //# 기대: 최초 생성 — 문서가 없어야 한다.
                        if (snap.Exists)
                        {
                            conflict = true;
                            return;
                        }
                    }
                    transaction.Set(doc, fields);
                });

                if (conflict)
                    return CloudSaveResult.Conflict;

                //# 성공 시 새 버전시각을 재캐시 — 세션 내 2번째+ 백업의 자기충돌 방지. 서버 원본 조회(로컬 캐시 스냅샷 금지).
                //# 재캐시 실패는 이미 커밋된 쓰기를 실패로 되돌리지 않는다 — 다음 PutSave 가 GetSave 로 재시딩(방어적).
                try
                {
                    DocumentSnapshot after = await doc.GetSnapshotAsync(Source.Server);
                    _saveDocExists = after.Exists;
                    _saveUpdateTime = after.Exists && after.ContainsField(ServerVersionField)
                        ? after.GetValue<Timestamp>(ServerVersionField, ServerTimestampBehavior.Estimate)
                        : (Timestamp?)null;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 재캐시 실패(다음 백업이 재조회): {e.Message}");
                    _saveUpdateTime = null;
                    _saveDocExists = false;
                }
                return CloudSaveResult.Success;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 저장 실패: {e.Message}");
                return CloudSaveResult.Failed;
            }
        }

        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return false;
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "uid", uid },
                { "displayName", displayName ?? string.Empty },
                { "clearTimeMs", clearTimeMs },
                { "hero", hero ?? string.Empty },
            };
            try
            {
                await Db.Collection(LeaderboardCollection).Document(uid).SetAsync(fields);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 랭킹 제출 실패: {e.Message}");
                return false;
            }
        }

        public async Task<List<RankingRowDto>> GetTopAsync(int top)
        {
            List<RankingRowDto> rows = new List<RankingRowDto>();
            try
            {
                //# 유령 문서(clearTimeMs 없음/0)를 쿼리 단계에서 배제 — Limit 이 필터보다 먼저 걸리면
                //# 유령이 top 개 이상일 때 진짜 기록이 통째로 잘려나간다(표시 단계 가드만으론 못 막음).
                QuerySnapshot snap = await Db.Collection(LeaderboardCollection)
                    .WhereGreaterThan("clearTimeMs", 0)
                    .OrderBy("clearTimeMs")
                    .Limit(top)
                    .GetSnapshotAsync();
                int rank = 1;
                foreach (DocumentSnapshot doc in snap.Documents)
                {
                    RankingRowDto row = ToRow(doc);
                    if (row == null)
                        continue;
                    //# clearTimeMs<=0 은 유령 문서(표시명만 있고 클리어 기록 없음) — 거짓 "1위 00:00" 방지.
                    if (IsRankedClearTime(row.clearTimeMs) == false)
                        continue;
                    //# 쿼리가 rank 를 내려주지 않는다 — clearTimeMs 오름차순 순서 = 순위(1부터).
                    row.rank = rank;
                    rank++;
                    rows.Add(row);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 랭킹 조회 실패: {e.Message}");
            }
            return rows;
        }

        //# 리더보드 문서 → 행 DTO. 필드 누락은 기본값으로 흡수(흐름을 막지 않는다).
        private static RankingRowDto ToRow(DocumentSnapshot doc)
        {
            if (doc == null || doc.Exists == false)
                return null;
            return new RankingRowDto
            {
                uid = doc.ContainsField("uid") ? doc.GetValue<string>("uid") : null,
                displayName = doc.ContainsField("displayName") ? doc.GetValue<string>("displayName") : null,
                clearTimeMs = doc.ContainsField("clearTimeMs") ? (int)doc.GetValue<long>("clearTimeMs") : 0,
                hero = doc.ContainsField("hero") ? doc.GetValue<string>("hero") : null,
            };
        }

        public async Task<List<RankingRowDto>> GetMyRankAsync()
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return new List<RankingRowDto>();
            try
            {
                DocumentSnapshot mine = await Db.Collection(LeaderboardCollection).Document(uid).GetSnapshotAsync();
                RankingRowDto myRow = ToRow(mine);
                //# clearTimeMs<=0 은 유효한 클리어 기록이 아님(유령 문서) — 거짓 "1위 00:00" 방지.
                if (myRow == null || IsRankedClearTime(myRow.clearTimeMs) == false)
                    return new List<RankingRowDto>();

                //# 유령 문서는 clearTimeMs=0 이라 항상 "나보다 빠름"으로 잡혀 순위를 부풀린다 — 집계에서도 배제.
                AggregateQuerySnapshot agg = await Db.Collection(LeaderboardCollection)
                    .WhereGreaterThan("clearTimeMs", 0)
                    .WhereLessThan("clearTimeMs", myRow.clearTimeMs)
                    .Count
                    .GetSnapshotAsync(AggregateSource.Server);
                myRow.rank = agg.Count + 1;
                return new List<RankingRowDto> { myRow };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 내 순위 조회 실패: {e.Message}");
                return new List<RankingRowDto>();
            }
        }

        //# 유효 클리어 시간 판정 — clearTimeMs 는 소요시간(ms)이라 0/음수는 실제 클리어일 수 없다.
        public static bool IsRankedClearTime(long ms) => ms > 0;

        public async Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName)
        {
            string norm = displayName == null ? string.Empty : displayName.Trim();
            if (string.IsNullOrEmpty(norm))
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            //# 문서ID 파손 문자 차단 — / 는 경로 분리자. 제품 charset 정책이 아니라 기술 제약.
            if (norm.IndexOf('/') >= 0)
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return DisplayNameResult.Of(DisplayNameStatus.Offline);

            DocumentReference lockDoc = Db.Collection(DisplayNamesCollection).Document(norm);
            DocumentReference lbDoc = Db.Collection(LeaderboardCollection).Document(uid);

            try
            {
                bool taken = false;
                await Db.RunTransactionAsync(async transaction =>
                {
                    //# Firestore 트랜잭션은 모든 read 가 write 보다 먼저 와야 한다 — lock/leaderboard 두 문서를 먼저 다 읽는다.
                    DocumentSnapshot lockSnap = await transaction.GetSnapshotAsync(lockDoc);
                    DocumentSnapshot lbSnap = await transaction.GetSnapshotAsync(lbDoc);
                    //# 이미 존재하고 소유자가 내가 아니면 중복. 내 것이면 재점유 허용(멱등).
                    if (lockSnap.Exists)
                    {
                        string owner = lockSnap.ContainsField("uid") ? lockSnap.GetValue<string>("uid") : null;
                        if (owner != uid)
                        {
                            taken = true;
                            return;
                        }
                    }
                    transaction.Set(lockDoc, new Dictionary<string, object> { { "uid", uid } });
                    //# displayName 만 병합(MergeAll) — 문서가 없으면 새로 만들지 않는다(유령 문서 방지).
                    //# 존재할 때만 병합하므로 clearTimeMs/hero 는 그대로 보존된다.
                    if (lbSnap.Exists)
                    {
                        transaction.Set(lbDoc, new Dictionary<string, object> { { "displayName", norm } }, SetOptions.MergeAll);
                    }
                });

                if (taken)
                    return DisplayNameResult.Of(DisplayNameStatus.Taken);
                return new DisplayNameResult(DisplayNameStatus.Success, norm);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 표시명 변경 실패: {e.Message}");
                return DisplayNameResult.Of(DisplayNameStatus.Offline);
            }
        }
    }
}
#endif
