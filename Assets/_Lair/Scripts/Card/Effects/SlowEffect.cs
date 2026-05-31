using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — Slow (Swarm A 축 이동 + 리뉴얼, 기획서 §3.4 #7).
    //# 이중 효과: 영웅 이동속도 ×_heroFactor + 모든 몬스터 이동속도 ×_monsterMul, _duration 초.
    //# 영웅 슬로우 — 기존 SlowAura 재사용. 몬스터 가속 — MonsterBuffService 의 SwarmSpeed 케이스 (후속 효과 본문 구현 필요).
    [Serializable]
    public class SlowEffect : ICardEffect
    {
        [FormerlySerializedAs("_factor")]
        [SerializeField] private float _heroFactor = 0.5f;
        [SerializeField] private float _monsterMul = 1.3f;
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
        {
            //# 영웅 슬로우 — 기존 SlowAura 사용 (변경 없음).
            IMover mover = ctx.GetHeroMover();
            if (mover != null)
                ctx.ApplyHeroAura(new SlowAura(mover, _heroFactor), _duration);
            //# 몬스터 가속 — SwarmSpeed enum 자리만 보존, MonsterBuffService 의 case 본문은 후속 작업.
            //# _monsterMul 은 SO 인스펙터에 노출되어 후속 작업 시 그대로 사용 가능.
            ctx.AddMonsterBuff(EMonsterBuff.SwarmSpeed, _duration);
        }
    }
}
