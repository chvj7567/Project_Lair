using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# Health.OnDied 발행 시 GameObject 를 CHMPool 로 반환 (Rule 12).
    //# CHPoolable 없으면 Destroy 로 fallback. _delay > 0 이면 그 시간만큼 후에 처리.
    //# OnDisable 흐름: Push → SetActive(false) → 자식 컴포넌트 OnDisable
    //#                 → CharacterRegistry.Unregister*. 즉시 적 목록 자동 제외.
    [RequireComponent(typeof(Health))]
    public class DespawnOnDeath : MonoBehaviour
    {
        [SerializeField] private float _delay = 0f;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            if (_health != null) _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            if (_delay > 0f) StartCoroutine(DespawnDelayed());
            else             DespawnNow();
        }

        private IEnumerator DespawnDelayed()
        {
            yield return new WaitForSeconds(_delay);
            DespawnNow();
        }

        private void DespawnNow()
        {
            //# 재사용 대비 — EndBattle 등에서 ai.enabled=false 됐던 상태 복원
            var ai = GetComponent<AutoCombatAI>();
            if (ai != null) ai.enabled = true;

            var poolable = GetComponent<CHPoolable>();
            if (poolable != null)
            {
                CHMPool.Instance.Push(poolable);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
