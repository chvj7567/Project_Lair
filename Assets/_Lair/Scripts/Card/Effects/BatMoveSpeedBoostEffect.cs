using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 박쥐의 이동속도 × _speedMul.
    [Serializable]
    public class BatMoveSpeedBoostEffect : ICardEffect
    {
        [SerializeField] private float _speedMul = 1.5f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Bat))
            {
                if (hp is MonoBehaviour mb)
                {
                    var mover = mb.GetComponent<IMover>();
                    if (mover != null) mover.Speed *= _speedMul;
                }
            }
        }
    }
}
