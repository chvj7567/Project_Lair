using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Lair.Data
{
    //# 타입 레벨 [Preserve] — IL2CPP AOT 에서 Newtonsoft 가 ctor+populate 하도록 전체 보존.
    //# DTO 는 매개변수 없는 기본 ctor 보유(암시적) — Newtonsoft construct 가능.
    [Preserve]
    public class CharacterStatDto
    {
        [Preserve] [JsonProperty("hp")]        public int   Hp;
        [Preserve] [JsonProperty("power")]     public int   Power;
        [Preserve] [JsonProperty("range")]     public float Range;
        [Preserve] [JsonProperty("cooldown")]  public float Cooldown;
        [Preserve] [JsonProperty("moveSpeed")] public float MoveSpeed;
    }

    [Preserve]
    public class MonsterStatRowDto
    {
        [Preserve] [JsonProperty("key")]         public string          Key;
        [Preserve] [JsonProperty("stat")]        public CharacterStatDto Stat;
        [Preserve] [JsonProperty("spawnPeriod")] public float           SpawnPeriod;
    }

    [Preserve]
    public class BalanceConfigDto
    {
        [Preserve] [JsonProperty("hero")]              public CharacterStatDto        Hero;
        [Preserve] [JsonProperty("monsters")]          public List<MonsterStatRowDto> Monsters = new List<MonsterStatRowDto>();
        [Preserve] [JsonProperty("runDuration")]       public float                   RunDuration;
        [Preserve] [JsonProperty("passiveThresholds")] public float[]                 PassiveThresholds;
        [Preserve] [JsonProperty("activeThresholds")]  public float[]                 ActiveThresholds;
    }
}
