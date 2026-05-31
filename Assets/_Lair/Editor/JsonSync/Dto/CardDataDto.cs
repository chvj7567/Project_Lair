using Newtonsoft.Json;
using Lair.Card;

namespace Lair.EditorTools
{
    public class CardDataDto
    {
        [JsonProperty("id")]          public string Id;
        //# 카드 리뉴얼 v0.6 — json 키 "category" → "axis". 값은 EBuildAxis 의 enum 명 (Tank/Dps/Debuff/Swarm).
        //# 필드명은 Category 그대로 유지 — Syncer / 테스트의 호출자 영향 최소화.
        [JsonProperty("axis")]        public string Category;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("description")] public string Description;
        [JsonProperty("effect")]      public ICardEffect Effect;
    }
}
