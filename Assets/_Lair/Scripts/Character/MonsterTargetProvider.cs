using UnityEngine;

namespace Lair.Character
{
    //# 몬스터용 — Heroes 레지스트리에서 영웅 검색.
    public class MonsterTargetProvider : MonoBehaviour, ITargetProvider
    {
        private IHealth _selfHealth;

        private void Awake() => _selfHealth = GetComponent<IHealth>();

        private void OnEnable()
        {
            if (_selfHealth != null)
                CharacterRegistry.RegisterMonster(transform, _selfHealth);
        }

        private void OnDisable()
        {
            CharacterRegistry.UnregisterMonster(transform);
        }

        public bool TryFindNearest(Vector3 from, out Transform target, out IHealth health)
            => CharacterRegistry.TryFindNearestHero(from, out target, out health);
    }
}
