using System;
using Lair.Card;

namespace Lair.Tests.Helpers
{
    //# 테스트용 ICardEffect 더블 — Apply 호출 횟수 / 인자 / 동작을 자유 조립.
    //# A 영역 (ApplyCardEffect 카드 스코프 추적) 테스트가 Apply 본문에서 RegisterMonsterTypeBuff 를
    //# 호출하거나 예외를 던지는 시나리오를 구성하는 데 사용한다.
    public class FakeCardEffect : ICardEffect
    {
        //# Apply 가 호출되면 본 액션 실행 — null 이면 no-op.
        public Action<IBattleContext> OnApply;
        //# Apply 호출 횟수.
        public int ApplyCount;
        //# Apply 시 받은 ctx (마지막 호출).
        public IBattleContext LastCtx;

        public void Apply(IBattleContext ctx)
        {
            ApplyCount++;
            LastCtx = ctx;
            OnApply?.Invoke(ctx);
        }
    }
}
