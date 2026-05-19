using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 슬라임 전부 제거 → 위치 평균에 골렘 1마리.
    [Serializable]
    public class ReplaceSlimesToGolemEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            var positions = new List<Vector3>();
            foreach (var hp in ctx.GetMonsters(EMonster.Slime))
            {
                if (hp is MonoBehaviour mb)
                {
                    positions.Add(mb.transform.position);
                    //# 슬라임에 즉시 큰 데미지 → DespawnOnDeath 가 Destroy
                    hp.TakeDamage(int.MaxValue / 2);
                }
            }

            if (positions.Count == 0) return;

            Vector3 avg = Vector3.zero;
            foreach (var p in positions) avg += p;
            avg /= positions.Count;

            ctx.SpawnMonster(EMonster.Golem, avg);
        }
    }
}
