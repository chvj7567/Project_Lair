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
    }
}
