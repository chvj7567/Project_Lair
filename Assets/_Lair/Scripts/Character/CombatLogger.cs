using UnityEngine;

namespace Lair.Character
{
    //# Health 의 OnChanged/OnDied 를 구독해 [Combat] 로그로 출력.
    //# 검증/디버깅 한정 — 프리팹에 영구 부착하지 않고 LairManualVerify 가 동적으로 AddComponent.
    public class CombatLogger : MonoBehaviour
    {
        public static bool Enabled = true;

        private Health _health;
        private int _lastHp;

        private void OnEnable()
        {
            _health = GetComponent<Health>();
            if (_health == null) return;
            _health.OnChanged += HandleChanged;
            _health.OnDied += HandleDied;
        }

        private void Start()
        {
            //# Awake 가 다른 컴포넌트보다 먼저 호출될 수 있어 Start 에서 캐시
            if (_health != null) _lastHp = _health.Current;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnChanged -= HandleChanged;
            _health.OnDied -= HandleDied;
        }

        private void HandleChanged(int current, int max)
        {
            if (Enabled == false) return;
            int delta = _lastHp - current;
            _lastHp = current;
            if (delta > 0)
            {
                Debug.Log($"[Combat] {gameObject.name} -{delta} HP ({current}/{max})");
            }
        }

        private void HandleDied()
        {
            if (Enabled == false) return;
            Debug.Log($"[Combat] {gameObject.name} 사망");
        }
    }
}
