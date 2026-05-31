using System;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 단단한 살갗 (Tank A, 구 WallOfWisps 자리 — 효과 교체, ECardId 값명·SO 파일명 보존).
    //# 영구 효과: 위스프·레이스 받는 데미지 ×0.75 (MonsterBuffService.ToughHide 영구 buff 등록).
    //# 적용 종 한정 {Wisp, Wraith} 는 MonsterBuffService.TargetTypes 에서 처리.
    //# 같은 카드 K번 픽 시 ToughHide buff 가 누적되지 않음 (단일 인스턴스) — 효과량은 고정 ×0.75.
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
