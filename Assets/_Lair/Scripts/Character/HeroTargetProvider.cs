using UnityEngine;

namespace Lair.Character
{
    //# 영웅용 — Monsters 레지스트리에서 최근접 살아있는 적 검색.
    //# OnEnable 시 자기 자신을 Heroes 레지스트리에 등록.
    public class HeroTargetProvider : MonoBehaviour, ITargetProvider
    {
        private IHealth _selfHealth;

        private void Awake() => _selfHealth = GetComponent<IHealth>();

        private void OnEnable()
        {
            if (_selfHealth != null)
                CharacterRegistry.RegisterHero(transform, _selfHealth);
        }

        private void OnDisable()
        {
            CharacterRegistry.UnregisterHero(transform);
        }

        public bool TryFindNearest(Vector3 from, out Transform target, out IHealth health)
            => CharacterRegistry.TryFindNearestMonster(from, out target, out health);

        public bool TryGetThreatCentroid(Vector3 from, float radius, out Vector3 centroid, out int count)
            => CharacterRegistry.TryGetThreatCentroidMonster(from, radius, out centroid, out count);

        //# 도주 안정화(A-1) — Engaging 무관 변형. 교전 토글 진동원 제거.
        public bool TryGetFleeCentroid(Vector3 from, float radius, out Vector3 centroid, out int count)
            => CharacterRegistry.TryGetFleeCentroidMonster(from, radius, out centroid, out count);
    }
}
