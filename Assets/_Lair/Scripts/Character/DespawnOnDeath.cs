using System;
using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# Health.OnDied 발행 시 GameObject 를 CHMPool 로 반환 (Rule 12).
    //# CHPoolable 없으면 Destroy 로 fallback. _delay > 0 이면 그 시간만큼 후에 처리.
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

        //# monster-2d-conversion.md §5.5 불변식 1 — MonsterDied 는 풀 반환 지연(_delay)과 무관하게 사망 순간 즉시 발행.
        //# (버그 수정: 과거엔 DespawnNow 내부, 즉 지연 이후에 발행돼 _delay>0 프리팹에서 지연만큼 늦게 통지됐다.)
        private void HandleDied()
        {
            //# B3 — 몬스터면 사망 위치 발행 (위치가 유효한 사망 순간 즉시 — Push/연출 지연과 분리).
            if (GetComponent<MonsterTag>() != null)
                MonsterDied?.Invoke(transform.position);

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
            AutoCombatAI ai = GetComponent<AutoCombatAI>();
            if (ai != null) ai.enabled = true;

            CHPoolable poolable = GetComponent<CHPoolable>();
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
