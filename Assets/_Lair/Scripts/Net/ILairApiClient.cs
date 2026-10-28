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
        //# 스테이지별 랭킹 제출 — 해당 스테이지 기존 최단보다 빠를 때만 갱신. 범위 밖 스테이지/실패면 false.
        Task<bool> SubmitStageScoreAsync(int stage, int clearTimeMs, string hero, string displayName);
        //# 스테이지 Top N 조회 — 실패/범위 밖이면 빈 리스트.
        Task<List<RankingRowDto>> GetStageTopAsync(int stage, int top);
        //# 스테이지 내 순위 — 실패/미등재/범위 밖이면 빈 리스트.
        Task<List<RankingRowDto>> GetMyStageRankAsync(int stage);
        //# 표시명 변경 — 서버 권위 중복 체크. 결과(성공/중복/유효하지않음/오프라인)와 확정 이름 반환.
        Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName);
        //# 서버 현재 시각 (daily-challenge §10.3) — ID 토큰 강제 갱신 후 발급 시각(iat). 실패(오프라인·미인증)면 Ok=false.
        Task<ServerTimeResult> GetServerTimeAsync();
        //# 데일리 순위 제출 — 서버에 더 작은(좋은) 점수가 있으면 쓰지 않는다. 실패(오프라인·보안 규칙 거부)면 false.
        Task<bool> SubmitDailyScoreAsync(int day, int score, string hero, string comboKey, string displayName);
        //# 데일리 Top N (score 오름차순, RankingRowDto.clearTimeMs = 점수). 조회 실패(인덱스 미생성 등)는 null — 팝업이 "불러오지 못했습니다" 를 띄운다.
        Task<List<RankingRowDto>> GetDailyTopAsync(int day, int top);
        //# 데일리 내 순위 — 조회 실패면 null, 기록 없음이면 빈 리스트.
        Task<List<RankingRowDto>> GetMyDailyRankAsync(int day);
    }

    //# 서버 시각 조회 결과 — 실패를 sentinel 시각 대신 Ok 로 구분한다.
    public readonly struct ServerTimeResult
    {
        public readonly bool Ok;
        public readonly long UtcMs;

        public ServerTimeResult(bool ok, long utcMs)
        {
            Ok = ok;
            UtcMs = utcMs;
        }

        public static ServerTimeResult Failed => new ServerTimeResult(false, 0L);
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
