using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;

namespace Lair.Net
{
    //# 서버 엔드포인트 추상화 — 서비스가 이 인터페이스에만 의존(테스트 시 가짜 주입, Rule 02 §5).
    public interface ILairApiClient
    {
        //# 인증 — Firebase 익명 로그인으로 계정 보장 + uid 저장. 성공 여부 반환.
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
        //# 표시명 변경 — 서버 권위 중복 체크. 결과(성공/중복/유효하지않음/오프라인)와 확정 이름 반환.
        Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName);
    }

    //# PutSave 결과 — 409(서버가 더 최신)를 호출부가 구분하도록.
    public enum CloudSaveResult
    {
        Success,
        Conflict,
        Failed,
    }

    //# 표시명 변경 결과 코드 — 200/409/400/오프라인(네트워크오류·미인증)을 호출부가 분기.
    public enum DisplayNameStatus
    {
        Success,   //# 200 — Name 에 서버 정규화 이름
        Taken,     //# 409 name_taken
        Invalid,   //# 400 invalid_name
        Offline,   //# 네트워크 오류/미인증/오프라인
    }

    //# 표시명 변경 결과 — 상태 + 성공 시 서버가 돌려준 확정 이름. JsonUtility 비경유라 record 안전.
    public readonly struct DisplayNameResult
    {
        public readonly DisplayNameStatus Status;
        public readonly string Name;

        public DisplayNameResult(DisplayNameStatus status, string name)
        {
            Status = status;
            Name = name;
        }

        public static DisplayNameResult Of(DisplayNameStatus status) => new DisplayNameResult(status, null);
    }
}
