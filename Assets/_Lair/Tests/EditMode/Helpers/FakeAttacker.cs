using Lair.Character;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# IAttacker 테스트 더블. Enabled 토글 + TryAttack 호출 추적.
    public class FakeAttacker : IAttacker
    {
        public float Range { get; set; } = 1.5f;
        public float Cooldown { get; set; } = 1f;
        public int Power { get; set; } = 50;
        public bool Enabled { get; set; } = true;

        public int TryAttackCallCount { get; private set; }

        public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            TryAttackCallCount++;
            return false;
        }
    }
}
