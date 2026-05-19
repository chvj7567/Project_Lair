using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 골렘의 MeleeAttacker.Power × _mul.
    [Serializable]
    public class GolemDamageBoostEffect : ICardEffect
    {
        [SerializeField] private float _mul = 1.5f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Golem))
            {
                //# IHealth 에서 MonoBehaviour cast → MeleeAttacker 조회
                if (hp is MonoBehaviour mb)
                {
                    var attacker = mb.GetComponent<MeleeAttacker>();
                    if (attacker != null)
                    {
                        attacker.Configure(attacker.Range, attacker.Cooldown,
                                           Mathf.Max(1, (int)(attacker.Power * _mul)));
                    }
                }
            }
        }
    }
}
