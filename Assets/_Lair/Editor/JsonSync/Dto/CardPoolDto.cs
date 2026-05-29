using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lair.EditorTools
{
    public class CardPoolDto
    {
        [JsonProperty("passive")] public List<string> Passive = new List<string>();
        [JsonProperty("active")]  public List<string> Active  = new List<string>();
    }
}
