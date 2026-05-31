using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 위스프 장벽 (Tank A 신규, 기획서 §3.1 #6 / §10.4).
    //# 영웅 주변 4방위(0°/90°/180°/270°)에 _radius 거리에서 _count 마리 Wisp 즉시 소환.
    //# 캡 18 truncate 는 SpawnMonster 내부의 글로벌 캡 로직이 담당.
    [Serializable]
    public class WallOfWispsEffect : ICardEffect
    {
        [SerializeField] private int _count = 4;
        [SerializeField] private float _radius = 2.5f;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;

            Vector3 origin = heroT.position;
            //# 4방위 균등 — 0/90/180/270°. _count != 4 인 경우에도 균등 각도 분배.
            int n = Mathf.Max(1, _count);
            for (int i = 0; i < n; ++i)
            {
                float angle = (360f / n) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _radius;
                ctx.SpawnMonster(EMonster.Wisp, origin + offset);
            }
        }
    }
}
