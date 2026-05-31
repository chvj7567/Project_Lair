using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 스웜 러시 (Swarm A 신규, Multiply 자리 대체, 기획서 §3.4 #6 / §10.4).
    //# 영웅 transform 위치에서 _count 회 Phantom 즉시 소환. 캡 18 truncate 는 SpawnMonster 내부 처리.
    [Serializable]
    public class SwarmRushEffect : ICardEffect
    {
        [SerializeField] private int _count = 6;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            Vector3 origin = heroT != null ? heroT.position : Vector3.zero;
            int n = Mathf.Max(0, _count);
            for (int i = 0; i < n; ++i)
            {
                ctx.SpawnMonster(EMonster.Phantom, origin);
            }
        }
    }
}
