using System;
using System.Collections.Generic;
using Lair.Meta;

namespace Lair.Net
{
    //# 서버 응답/요청 본문 — 필드명은 서버 JSON 과 정확히 일치해야 한다(JsonUtility 대소문자 그대로).
    [Serializable]
    public class AnonymousAuthRequestBody
    {
        public string deviceId;
    }

    [Serializable]
    public class AnonymousAuthResponse
    {
        public long accountId;
        public string token;
    }

    //# PUT /save 본문 — profile 은 MetaProfile 을 그대로 직렬화(필드명 일치, spec §4).
    [Serializable]
    public class PutSaveRequestBody
    {
        public MetaProfile profile;
        public int schemaVersion;
        public string clientUpdatedAt;   //# ISO8601 UTC
    }

    [Serializable]
    public class SaveResponseBody
    {
        public MetaProfile profile;
        public int schemaVersion;
        public string updatedAt;
    }

    [Serializable]
    public class SubmitScoreRequestBody
    {
        public int clearTimeMs;
        public string hero;
        public string displayName;
    }

    [Serializable]
    public class SubmitScoreResponseBody
    {
        public bool accepted;
        public long rank;
    }

    [Serializable]
    public class RankingRowDto
    {
        public long rank;
        public string displayName;
        public int clearTimeMs;
        public string hero;
        //# 서버가 행마다 내려주는 계정 식별자 — "내 행" 1차 매칭 키. 구서버 응답엔 없어 0 으로 역직렬화됨(시간 폴백).
        public long accountId;
        //# Firebase 계정 식별자 — "내 행" 매칭 키(2026-07-14). 사문화된 accountId 보다 우선.
        public string uid;
    }

    //# POST /account/displayname 요청 본문 — 서버 계약 { "displayName": "<문자열>" }.
    //# record 지만 JsonUtility 는 필드만 직렬화하므로 positional 이 아닌 public 필드 형태로 둔다(속성 record 면 ToJson 이 {} 가 됨).
    [Serializable]
    public record DisplayNameRequestBody
    {
        public string displayName;
    }

    //# POST /account/displayname 200 응답 — { "displayName": "<정규화된 최종 이름>" }.
    //# 400/409 본문(error 코드)은 상태코드로 분기하므로 별도 DTO 불필요(LairApiClient).
    [Serializable]
    public record DisplayNameResponseBody
    {
        public string displayName;
    }

    //# JsonUtility 는 최상위 배열을 못 읽으므로 래퍼로 감싼다.
    [Serializable]
    public class RankingRowListWrapper
    {
        public List<RankingRowDto> rows;
    }
}
