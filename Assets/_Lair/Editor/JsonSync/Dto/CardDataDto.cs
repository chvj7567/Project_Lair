using Newtonsoft.Json;
using Lair.Card;

namespace Lair.EditorTools
{
    public class CardDataDto
    {
        [JsonProperty("id")]          public string Id;
        [JsonProperty("category")]    public string Category;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("description")] public string Description;
        [JsonProperty("effect")]      public ICardEffect Effect;
    }
}
