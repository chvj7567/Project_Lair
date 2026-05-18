using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# BattleController 인스펙터에서 (위치, 키) 쌍 직렬화용.
    [Serializable]
    public struct MonsterSpawnEntry
    {
        public Transform Point;
        public EMonster Key;
    }
}
