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
    }

    //# JsonUtility 는 최상위 배열을 못 읽으므로 래퍼로 감싼다.
    [Serializable]
    public class RankingRowListWrapper
    {
        public List<RankingRowDto> rows;
    }
}
