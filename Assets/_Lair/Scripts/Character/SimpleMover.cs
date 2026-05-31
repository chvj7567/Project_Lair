using Lair.Battle;
using UnityEngine;

namespace Lair.Character
{
    //# Vector3.MoveTowards 기반 단순 추적. Rigidbody 있으면 MovePosition 사용, 없으면 transform.position 폴백.
    //# _clampZone 비-null 이면 next 좌표를 BattleZone.ClampInside 로 클램프 — 영웅 한정 (몬스터는 null).
    //# 영웅은 풀 스폰이라 BattleController.SpawnHero 가 Pop 직후 BindClampZone 호출. 몬스터는 호출 안 함 → 무동작.
    public class SimpleMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed = 3f;
        //# 런타임 주입 — BindClampZone 으로 설정. 인스펙터 와이어링은 풀 Pop 시 reference broken 이라 사용 안 함.
        [SerializeField] private BattleZone _clampZone;
        private bool _moving;
        private Vector3 _target;
        private Rigidbody _rigidbody;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        //# 카드 리뉴얼 v0.6 [B2] — 일시 속도 배율 오버레이. MonsterBuffService 가 매 tick 재설정.
        //# 기본 1.0. SwarmSpeed buff 활성 중 1.3 곱연산 → FixedUpdate 의 effectiveSpeed 계산에 반영.
        //# buff 종료 후 Tick 에서 1.0 복원, 풀 재사용 시에도 1.0 리셋.
        public float SpeedScale { get; set; } = 1f;

        //# B3 — MoveTo 후 Stop 전까지 true. 출혈 카드가 영웅 이동 판정에 사용.
        public bool IsMoving => _moving;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        //# [B2] 풀 재사용 시 SpeedScale 잔존 방지.
        private void OnEnable()
        {
            SpeedScale = 1f;
        }

        public void MoveTo(Vector3 target)
        {
            _target = target;
            _moving = true;
        }

        public void Stop()
        {
            _moving = false;
        }

        //# 런타임 주입 — BattleController.SpawnHero 가 Pop 직후 호출. null 도 허용 (몬스터 또는 폴백 해제).
        public void BindClampZone(BattleZone zone)
        {
            _clampZone = zone;
        }

        private void FixedUpdate()
        {
            if (_moving == false)
                return;

            //# [B2] effectiveSpeed = base × SpeedScale. SwarmSpeed buff 활성 시 1.3 배수.
            float effectiveSpeed = _speed * SpeedScale;
            Vector3 next = Vector3.MoveTowards(transform.position, _target, effectiveSpeed * Time.fixedDeltaTime);
            //# Y 평면 고정 — 캐릭터가 카메라 각도로 떠오르지 않도록
            next.y = 0f;

            //# 영웅 한정 zone-clamp — _clampZone 미할당이면 무동작.
            if (_clampZone != null)
                next = _clampZone.ClampInside(next);

            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }
        }
    }
}
