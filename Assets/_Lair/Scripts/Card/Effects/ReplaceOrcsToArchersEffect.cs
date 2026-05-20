using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 오크 전부 제거 → 각 오크 위치에 궁수 1마리 (1:1 교체).
    [Serializable]
    public class ReplaceOrcsToArchersEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            var positions = new List<Vector3>();
            foreach (var hp in ctx.GetMonsters(EMonster.Orc))
            {
                if (hp is MonoBehaviour mb)
                {
                    positions.Add(mb.transform.position);
                    //# 오크에 즉시 큰 데미지 → DespawnOnDeath 가 풀 반환
                    hp.TakeDamage(int.MaxValue / 2);
                }
            }

            foreach (var pos in positions)
                ctx.SpawnMonster(EMonster.Archer, pos);
        }
    }
}
