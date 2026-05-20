using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 활성 _remain 초간 몬스터 사망 시 사망 위치 주변 몬스터 HP +30.
    public class BloodThirstService
    {
        private const float HealRadius = 3f;
        private const int HealAmount = 30;

        private float _remain;
        public bool IsActive => _remain > 0f;

        //# 같은 카드 재선택 시 더 긴 쪽으로 연장.
        public void Activate(float duration) => _remain = Mathf.Max(_remain, duration);

        public void Tick(float dt)
        {
            if (_remain > 0f) _remain -= dt;
        }

        //# 몬스터 사망 시 BattleController 가 사망 위치와 함께 호출.
        public void NotifyMonsterDied(Vector3 pos)
        {
            if (IsActive == false) return;
            float sqr = HealRadius * HealRadius;
            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null || e.Health == null || !e.Health.IsAlive) continue;
                if ((e.Transform.position - pos).sqrMagnitude <= sqr)
                    e.Health.Heal(HealAmount);
            }
        }
    }
}
