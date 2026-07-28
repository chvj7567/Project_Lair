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

        //# GetSave 시 캐시 — PutSave 트랜잭션의 충돌 판정 기준(마지막으로 본 버전).
        private Timestamp? _saveUpdateTime;

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

        //# --- 이하 Task 4·5 에서 구현. 스텁으로 컴파일 유지. ---
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult<SaveResponseBody>(null);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt) => Task.FromResult(CloudSaveResult.Failed);
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(false);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(DisplayNameResult.Of(DisplayNameStatus.Offline));
    }
}
#endif
