using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 슬라임 Max HP × _hpMul (Current 비율 유지).
    [Serializable]
    public class SlimeHpBoostEffect : ICardEffect
    {
        [SerializeField] private float _hpMul = 1.5f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Slime))
            {
                int newMax = Mathf.Max(1, (int)(hp.Max * _hpMul));
                hp.SetMax(newMax, resetCurrent: false);
            }
        }
    }
}
