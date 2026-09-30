using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# ILairApiClient 의 인메모리 가짜 — 호출 기록과 반환값을 테스트가 제어.
    public class FakeLairApiClient : ILairApiClient
    {
        public bool AuthResult = true;
        public SaveResponseBody SaveToReturn;
        public CloudSaveResult PutResultToReturn = CloudSaveResult.Success;
        public MetaProfile LastPutProfile;
        public bool SubmitResult = true;
        public int LastSubmittedMs = -1;
        public string LastSubmittedName;
        public List<RankingRowDto> TopToReturn = new List<RankingRowDto>();
        public DisplayNameResult ChangeNameResultToReturn = new DisplayNameResult(DisplayNameStatus.Success, "영주");
        public string LastChangeNameArg;
        public int LastSubmittedStage = -1;
        public int LastSubmittedStageMs = -1;
        public int LastTopStage = -1;
        public List<RankingRowDto> StageTopToReturn = new List<RankingRowDto>();

        public Task<bool> AuthenticateAsync() => Task.FromResult(AuthResult);
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult(SaveToReturn);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            LastPutProfile = profile;
            return Task.FromResult(PutResultToReturn);
        }
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            LastSubmittedMs = clearTimeMs;
            LastSubmittedName = displayName;
            return Task.FromResult(SubmitResult);
        }
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(TopToReturn);
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(TopToReturn);
        public Task<bool> SubmitStageScoreAsync(int stage, int clearTimeMs, string hero, string displayName)
        {
            LastSubmittedStage = stage;
            LastSubmittedStageMs = clearTimeMs;
            return Task.FromResult(SubmitResult);
        }
        public Task<List<RankingRowDto>> GetStageTopAsync(int stage, int top)
        {
            LastTopStage = stage;
            return Task.FromResult(StageTopToReturn);
        }
        public Task<List<RankingRowDto>> GetMyStageRankAsync(int stage) => Task.FromResult(StageTopToReturn);
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName)
        {
            LastChangeNameArg = displayName;
            return Task.FromResult(ChangeNameResultToReturn);
        }
    }
}
