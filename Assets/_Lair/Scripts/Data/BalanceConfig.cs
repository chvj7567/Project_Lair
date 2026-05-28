using System;
using UnityEngine;

namespace Lair.Data
{
    //# 캐릭터 스탯 + 전투 상수의 단일 진실. BattleController 가 씬에서 참조해 런타임 적용.
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Lair/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        //# 한 캐릭터의 튜닝 가능한 스탯.
        [Serializable]
        public class CharacterStat
        {
            public int   Hp;
            public int   Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
        }

        //# EMonster 키 ↔ 스탯 매핑 행.
        [Serializable]
        public class MonsterStatRow
        {
            public EMonster Key;
            public CharacterStat Stat;
        }

        [SerializeField] private CharacterStat _hero;
        [SerializeField] private MonsterStatRow[] _monsters;

        [SerializeField] private float _runDuration = 300f;
        [SerializeField] private float[] _passiveThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        [SerializeField] private float[] _activeThresholds =
            { 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f };

        public CharacterStat Hero => _hero;
        public float RunDuration => _runDuration;
        public float[] PassiveThresholds => _passiveThresholds;
        public float[] ActiveThresholds => _activeThresholds;

        //# EMonster 키로 스탯 행 조회. 미발견 시 null + 경고.
        public CharacterStat GetMonster(EMonster key)
        {
            if (_monsters != null)
            {
                foreach (MonsterStatRow row in _monsters)
                {
                    if (row != null && row.Key == key) return row.Stat;
                }
            }
            Debug.LogWarning($"[BalanceConfig] 몬스터 스탯 미발견: {key}");
            return null;
        }
    }
}
