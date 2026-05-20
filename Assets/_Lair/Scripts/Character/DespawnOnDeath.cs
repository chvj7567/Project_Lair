using System;
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
        //# B3 — 몬스터(MonsterTag 보유) 사망 시 위치 발행. 피의 갈증 카드가 BattleController 경유 구독.
        public static event Action<Vector3> MonsterDied;

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
            //# B3 — 몬스터면 사망 위치 발행 (Push 전 — 위치가 유효할 때).
            if (GetComponent<MonsterTag>() != null)
                MonsterDied?.Invoke(transform.position);

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
