using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 궁수의 사거리 × _rangeMul.
    [Serializable]
    public class ArcherRangeBoostEffect : ICardEffect
    {
        [SerializeField] private float _rangeMul = 1.4f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Archer))
            {
                if (hp is MonoBehaviour mb)
                {
                    var attacker = mb.GetComponent<MeleeAttacker>();
                    if (attacker != null)
                        attacker.Configure(attacker.Range * _rangeMul,
                                           attacker.Cooldown, attacker.Power);
                }
            }
        }
    }
}
