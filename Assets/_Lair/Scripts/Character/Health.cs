using System;
using UnityEngine;

namespace Lair.Character
{
    //# IHealth 구현체. 본 슬라이스 한정 POCO + MonoBehaviour 양립.
    //# Unity 컴포넌트로 사용 시: GameObject 에 AddComponent.
    //# 테스트에서는 new Health() 직접 생성 후 SetMax 로 초기화.
    public class Health : MonoBehaviour, IHealth
    {
        [SerializeField] private int _max = 100;

        public int Max => _max;
        public int Current { get; private set; }
        public float Ratio => _max > 0 ? (float)Current / _max : 0f;
        public bool IsAlive => Current > 0;

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        //# MonoBehaviour 라이프사이클 — 인스펙터 _max 로 초기화.
        private void Awake()
        {
            Current = _max;
        }

        public void TakeDamage(int amount)
        {
            if (IsAlive == false) return;
            int next = Mathf.Max(0, Current - amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, _max);
            if (Current == 0) OnDied?.Invoke();
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            _max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, _max);
        }
    }
}
