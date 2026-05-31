using System;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 단단한 살갗 (Tank A, 구 WallOfWisps 자리 — 효과 교체, ECardId 값명·SO 파일명 보존).
    //# 영구 효과: 위스프·레이스 받는 데미지 ×0.75 (MonsterBuffService.ToughHide 영구 buff 등록).
    [Serializable]
    public class ToughHideEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            //# duration = -1f 로 영구 buff 등록. MonsterBuffService 의 Tick reset 후 매 tick 적용.
            ctx.AddMonsterBuff(EMonsterBuff.ToughHide, -1f);
        }
    }
}
