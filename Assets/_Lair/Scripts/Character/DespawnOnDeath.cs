using UnityEngine;

namespace Lair.Character
{
    //# Health.OnDied 발행 시 GameObject 파괴. 옵션 _delay 로 사망 직후 잠시 시신을 보여줄 수 있음.
    //# OnDisable 흐름: Destroy → 자식 컴포넌트(HeroTargetProvider/MonsterTargetProvider)의 OnDisable
    //#                 → CharacterRegistry.Unregister*. 따라서 사망 즉시 적 목록에서 자동 제외.
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
            if (_delay > 0f) Destroy(gameObject, _delay);
            else             Destroy(gameObject);
        }
    }
}
