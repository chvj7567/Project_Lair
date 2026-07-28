using System;
using Lair.Meta;

namespace Lair.Net
{
    //# ILairApiClient 시그니처에 남은 서버 응답 본문 DTO. 필드명은 서버 JSON 과 정확히 일치해야 한다(JsonUtility 대소문자 그대로).
    [Serializable]
    public class SaveResponseBody
    {
        public MetaProfile profile;
        public int schemaVersion;
        public string updatedAt;
    }

    [Serializable]
    public class RankingRowDto
    {
        public long rank;
        public string displayName;
        public int clearTimeMs;
        public string hero;
        //# Firebase 계정 식별자 — 내 행 매칭 키.
        public string uid;
    }
}
