using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# CHMHttpNetwork 로 서버 엔드포인트를 호출하는 구현. 토큰/accountId 는 AuthTokenStore.
    public class LairApiClient : ILairApiClient
    {
        private readonly NetworkConfig _config;

        public LairApiClient(NetworkConfig config)
        {
            _config = config;
        }

        private string Url(string path) => $"{_config.BaseUrl}{path}";
        private int Timeout => _config.TimeoutSec;

        public async Task<bool> AuthenticateAsync()
        {
            AnonymousAuthRequestBody req = new AnonymousAuthRequestBody { deviceId = AuthTokenStore.GetOrCreateDeviceId() };
            CHHttpResult res = await CHMHttpNetwork.PostAsync(Url("/auth/anonymous"), JsonUtility.ToJson(req), null, Timeout);
            if (res.IsSuccess == false)
            {
                Debug.LogWarning($"[LairApiClient] 인증 실패: {res.StatusCode} {res.Error}");
                return false;
            }
            AnonymousAuthResponse parsed = JsonUtility.FromJson<AnonymousAuthResponse>(res.Body);
            if (parsed == null || string.IsNullOrEmpty(parsed.token))
                return false;
            AuthTokenStore.SaveToken(parsed.token);
            //# 랭킹 "내 행" 식별용 accountId 저장(Delta 6 / 기획서 §4·§8).
            AuthTokenStore.SaveAccountId(parsed.accountId);
            return true;
        }

        public async Task<SaveResponseBody> GetSaveAsync()
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url("/save"), AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
                return null;
            return JsonUtility.FromJson<SaveResponseBody>(res.Body);
        }

        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            PutSaveRequestBody body = new PutSaveRequestBody
            {
                profile = profile,
                schemaVersion = profile.Version,
                clientUpdatedAt = clientUpdatedAt,
            };
            CHHttpResult res = await CHMHttpNetwork.PutAsync(Url("/save"), JsonUtility.ToJson(body), AuthTokenStore.Token, Timeout);
            if (res.IsConflict)
                return CloudSaveResult.Conflict;
            if (res.IsSuccess == false)
                return CloudSaveResult.Failed;
            return CloudSaveResult.Success;
        }

        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            SubmitScoreRequestBody body = new SubmitScoreRequestBody { clearTimeMs = clearTimeMs, hero = hero, displayName = displayName };
            CHHttpResult res = await CHMHttpNetwork.PostAsync(Url("/leaderboard/submit"), JsonUtility.ToJson(body), AuthTokenStore.Token, Timeout);
            return res.IsSuccess;
        }

        public async Task<List<RankingRowDto>> GetTopAsync(int top)
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url($"/leaderboard?top={top}"), AuthTokenStore.Token, Timeout);
            return ParseRows(res);
        }

        public async Task<List<RankingRowDto>> GetMyRankAsync()
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url("/leaderboard/me"), AuthTokenStore.Token, Timeout);
            return ParseRows(res);
        }

        //# 서버가 최상위 JSON 배열을 반환하므로 래퍼로 감싸 JsonUtility 파싱.
        //# malformed body 는 JsonUtility 가 예외를 던지므로 try/catch 로 감싸 빈 리스트 fallback(기획서 §6 흐름 차단 금지).
        private static List<RankingRowDto> ParseRows(CHHttpResult res)
        {
            if (res.IsSuccess == false || string.IsNullOrEmpty(res.Body))
                return new List<RankingRowDto>();
            try
            {
                string wrapped = "{\"rows\":" + res.Body + "}";
                RankingRowListWrapper parsed = JsonUtility.FromJson<RankingRowListWrapper>(wrapped);
                return parsed != null && parsed.rows != null ? parsed.rows : new List<RankingRowDto>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LairApiClient] 랭킹 응답 파싱 실패 — 빈 목록 반환: {e.Message}");
                return new List<RankingRowDto>();
            }
        }
    }
}
