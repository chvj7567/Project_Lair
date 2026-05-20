using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 거미의 둔화 배율 강화 (0.8 → _slowFactor, 낮을수록 강한 둔화).
    [Serializable]
    public class SpiderSlowBoostEffect : ICardEffect
    {
        [SerializeField] private float _slowFactor = 0.6f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Spider))
            {
                if (hp is MonoBehaviour mb)
                    mb.GetComponent<SpiderSlowOnHit>()?.SetSlowFactor(_slowFactor);
            }
        }
    }
}
