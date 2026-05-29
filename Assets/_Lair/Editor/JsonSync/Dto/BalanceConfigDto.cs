using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lair.EditorTools
{
    public class CharacterStatDto
    {
        [JsonProperty("hp")]        public int   Hp;
        [JsonProperty("power")]     public int   Power;
        [JsonProperty("range")]     public float Range;
        [JsonProperty("cooldown")]  public float Cooldown;
        [JsonProperty("moveSpeed")] public float MoveSpeed;
    }

    public class MonsterStatRowDto
    {
        [JsonProperty("key")]  public string          Key;
        [JsonProperty("stat")] public CharacterStatDto Stat;
    }

    public class BalanceConfigDto
    {
        [JsonProperty("hero")]              public CharacterStatDto        Hero;
        [JsonProperty("monsters")]          public List<MonsterStatRowDto> Monsters = new List<MonsterStatRowDto>();
        [JsonProperty("runDuration")]       public float                   RunDuration;
        [JsonProperty("passiveThresholds")] public float[]                 PassiveThresholds;
        [JsonProperty("activeThresholds")]  public float[]                 ActiveThresholds;
    }
}
