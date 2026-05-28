using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 증식 — 가장 수가 많은 몬스터 종을 현재 수만큼 즉시 추가 소환 (2배).
    [Serializable]
    public class MultiplyEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            //# EMonster 별 집계 → 최다 종 찾기
            Dictionary<EMonster, int> counts = new Dictionary<EMonster, int>();
            foreach (EMonster key in Enum.GetValues(typeof(EMonster)))
            {
                int c = 0;
                foreach (IHealth _ in ctx.GetMonsters(key)) ++c;
                if (c > 0) counts[key] = c;
            }
            if (counts.Count == 0) return;

            EMonster top = default;
            int max = 0;
            foreach (KeyValuePair<EMonster, int> kv in counts)
                if (kv.Value > max) { max = kv.Value; top = kv.Key; }

            Transform heroT = ctx.GetHeroTransform();
            Vector3 pos = heroT != null ? heroT.position : Vector3.zero;
            for (int i = 0; i < max; ++i)
                ctx.SpawnMonster(top, pos);
        }
    }
}
