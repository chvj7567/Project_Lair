using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 오크의 MeleeAttacker.Cooldown × _cdMul (낮을수록 빠른 공격).
    [Serializable]
    public class OrcAtkSpeedEffect : ICardEffect
    {
        [SerializeField] private float _cdMul = 0.7f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Orc))
            {
                if (hp is MonoBehaviour mb)
                {
                    var attacker = mb.GetComponent<MeleeAttacker>();
                    if (attacker != null)
                    {
                        attacker.Configure(attacker.Range,
                                           Mathf.Max(0.05f, attacker.Cooldown * _cdMul),
                                           attacker.Power);
                    }
                }
            }
        }
    }
}
