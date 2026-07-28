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

        //# 클라우드 세이브 조회 — 없으면 null, 통신 실패면 null. serverVersion 캐시를 함께 갱신(PutSave 충돌 판정 기준).
        //# GetValue<T> 두번째 인자(ServerTimestampBehavior)는 이 SDK 시그니처상 필수 — 일반 필드엔 의미 없어 Estimate 고정.
        public async Task<SaveResponseBody> GetSaveAsync()
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return null;
            try
            {
                //# 트랜잭션 밖 조회는 캐시가 아니라 서버 원본을 봐야 baseline 이 어긋나지 않는다.
                DocumentSnapshot snap = await Db.Collection(SavesCollection).Document(uid).GetSnapshotAsync(Source.Server);
                if (snap.Exists == false)
                {
                    //# 문서 없음 = "세이브 없음". 최초 생성 경로를 위해 캐시를 비운다.
                    _saveUpdateTime = null;
                    _saveDocExists = false;
                    return null;
                }
                _saveDocExists = true;
                //# serverVersion 필드가 없으면(REST 시절 문서) baseline 미확보 — PutSave 가 존재여부만으로 판정하는 경로로 빠진다.
                _saveUpdateTime = snap.ContainsField(ServerVersionField)
                    ? snap.GetValue<Timestamp>(ServerVersionField, ServerTimestampBehavior.Estimate)
                    : (Timestamp?)null;
                string profileJson = snap.ContainsField("profile") ? snap.GetValue<string>("profile", ServerTimestampBehavior.Estimate) : null;
                if (string.IsNullOrEmpty(profileJson))
                    return null;
                MetaProfile profile = JsonUtility.FromJson<MetaProfile>(profileJson);
                if (profile == null)
                    return null;
                return new SaveResponseBody
                {
                    profile = profile,
                    schemaVersion = snap.ContainsField("schemaVersion") ? (int)snap.GetValue<long>("schemaVersion", ServerTimestampBehavior.Estimate) : 0,
                    updatedAt = snap.ContainsField("updatedAt") ? snap.GetValue<string>("updatedAt", ServerTimestampBehavior.Estimate) : null,
                };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 조회 실패: {e.Message}");
                _saveUpdateTime = null;
                _saveDocExists = false;
                return null;
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
            if (_saveUpdateTime.HasValue == false && _saveDocExists == false)
            {
                await GetSaveAsync();
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

        //# --- 이하 Task 5 에서 구현. 스텁으로 컴파일 유지. ---
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(false);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(DisplayNameResult.Of(DisplayNameStatus.Offline));
    }
}
#endif
