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

        //# B3 — 받는 데미지 배율 오버레이. MonsterBuffService 가 매 tick 설정. 기본 1.0.
        public float DamageTakenScale { get; set; } = 1f;

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        //# MonoBehaviour 라이프사이클 — 인스펙터 _max 로 초기화.
        private void Awake()
        {
            Current = _max;
        }

        //# CHMPool 재사용 시 사망 상태로 풀에서 빠져나온 인스턴스를 복원.
        //# Pop → SetActive(true) → OnEnable. Current > 0 이면 그대로 유지.
        //# B3 — 오버레이 배율도 풀 재사용 시 잔존 방지 위해 1.0 리셋.
        private void OnEnable()
        {
            if (Current <= 0) Current = _max;
            DamageTakenScale = 1f;
        }

        public void TakeDamage(int amount)
        {
            if (IsAlive == false) return;
            //# B3 — 받는 데미지 배율 적용
            amount = Mathf.RoundToInt(amount * DamageTakenScale);
            int next = Mathf.Max(0, Current - amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, _max);
            if (Current == 0) OnDied?.Invoke();
        }

        //# B3 — 피의 갈증 카드. Max 초과 불가.
        public void Heal(int amount)
        {
            if (IsAlive == false) return;
            int next = Mathf.Min(_max, Current + amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, _max);
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            _max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, _max);
        }
    }
}
