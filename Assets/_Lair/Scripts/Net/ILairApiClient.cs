using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;

namespace Lair.Net
{
    //# 서버 엔드포인트 추상화 — 서비스가 이 인터페이스에만 의존(테스트 시 가짜 주입, Rule 02 §5).
    public interface ILairApiClient
    {
        //# 인증 — deviceId 로 계정 보장 + 토큰/accountId 저장. 성공 여부 반환.
        Task<bool> AuthenticateAsync();
        //# 클라우드 세이브 조회 — 없으면 null, 통신 실패면 null.
        Task<SaveResponseBody> GetSaveAsync();
        //# 클라우드 세이브 저장 — 결과(성공/409충돌/실패) 반환.
        Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt);
        //# 랭킹 제출.
        Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName);
        //# Top N 조회 — 실패면 빈 리스트.
        Task<List<RankingRowDto>> GetTopAsync(int top);
        //# 내 순위 ±주변 — 실패면 빈 리스트.
        Task<List<RankingRowDto>> GetMyRankAsync();
    }

    //# PutSave 결과 — 409(서버가 더 최신)를 호출부가 구분하도록.
    public enum CloudSaveResult
    {
        Success,
        Conflict,
        Failed,
    }
}
