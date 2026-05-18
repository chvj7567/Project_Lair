using Lair.Data;

namespace Lair.UI
{
    //# 순수 POCO. Unity 의존성 0. 테스트에서 직접 생성 가능.
    //# BattleResult enum 은 CommonEnum.cs (Lair.Data) 의 공용 정의를 사용한다 — Rule 09.
    public class BattleStateModel
    {
        public float ElapsedSeconds;
        public float TotalSeconds = 300f;   //# 5:00
        public int HeroHp;
        public int HeroMaxHp;
        public BattleResult Result = BattleResult.None;
    }
}
