using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lair.Net
{
    //# 랭킹 제출/조회 — best-effort. 실패해도 게임 흐름 차단 금지.
    public class RankingClient
    {
        private readonly ILairApiClient _api;

        public RankingClient(ILairApiClient api)
        {
            _api = api;
        }

        public Task<bool> SubmitAsync(int clearTimeMs, string hero, string displayName)
            => _api.SubmitScoreAsync(clearTimeMs, hero, displayName);

        public Task<List<RankingRowDto>> GetTopAsync(int top)
            => _api.GetTopAsync(top);

        public Task<List<RankingRowDto>> GetMyRankAsync()
            => _api.GetMyRankAsync();

        public Task<bool> SubmitStageAsync(int stage, int clearTimeMs, string hero, string displayName)
            => _api.SubmitStageScoreAsync(stage, clearTimeMs, hero, displayName);

        public Task<List<RankingRowDto>> GetStageTopAsync(int stage, int top)
            => _api.GetStageTopAsync(stage, top);

        public Task<List<RankingRowDto>> GetMyStageRankAsync(int stage)
            => _api.GetMyStageRankAsync(stage);
    }
}
