using System;
using Lair.Character;

namespace Lair.Tests.Helpers
{
    //# IHealth 테스트 더블. TakeDamage 호출 추적 + 임의 값 설정 가능.
    public class FakeHealth : IHealth
    {
        public int Max { get; private set; } = 100;
        public int Current { get; private set; } = 100;
        public float Ratio => Max > 0 ? (float)Current / Max : 0f;
        public bool IsAlive => Current > 0;

        public int LastDamage { get; private set; }
        public int DamageCallCount { get; private set; }

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        public void TakeDamage(int amount)
        {
            LastDamage = amount;
            DamageCallCount++;
            if (IsAlive == false) return;
            Current = Math.Max(0, Current - amount);
            OnChanged?.Invoke(Current, Max);
            if (Current == 0) OnDied?.Invoke();
        }

        //# B3 — 피의 갈증 카드. Max 초과 불가.
        public void Heal(int amount)
        {
            if (IsAlive == false) return;
            Current = Math.Min(Max, Current + amount);
            OnChanged?.Invoke(Current, Max);
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            Max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, Max);
        }

        public void ForceSetCurrent(int v)
        {
            Current = v;
        }
    }
}
