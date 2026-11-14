using Lair.Character;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — Slow (Swarm A 축 이동 + 리뉴얼, 기획서 §3.4 #7).
    //# 이중 효과: 영웅 이동속도 ×_heroFactor + 모든 몬스터 이동속도 ×_monsterMul, _duration 초.
    public class SlowEffect : ICardEffect
    {
        private readonly float _heroFactor;
        private readonly float _monsterMul;
        private readonly float _duration;

        public SlowEffect(CardParams p)
        {
            _heroFactor = p.Get("heroFactor");
            _monsterMul = p.Get("monsterMul");
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
        {
            //# 영웅 슬로우 — 기존 SlowAura 사용 (변경 없음).
            IMover mover = ctx.GetHeroMover();
            if (mover != null)
                ctx.ApplyHeroAura(new SlowAura(mover, _heroFactor), _duration);
            //# 몬스터 가속 — SwarmSpeed enum 자리만 보존, MonsterBuffService 의 case 본문은 후속 작업.
            //# _monsterMul(Card_Param monsterMul)은 후속 작업에서 그대로 사용한다.
            ctx.AddMonsterBuff(EMonsterBuff.SwarmSpeed, _duration);
        }
    }
}
