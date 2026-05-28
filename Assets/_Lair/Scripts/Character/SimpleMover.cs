using UnityEngine;

namespace Lair.Character
{
    //# Vector3.MoveTowards 기반 단순 추적. Rigidbody 있으면 MovePosition 사용, 없으면 transform.position 폴백.
    public class SimpleMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed = 3f;
        private bool _moving;
        private Vector3 _target;
        private Rigidbody _rigidbody;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        //# B3 — MoveTo 후 Stop 전까지 true. 출혈 카드가 영웅 이동 판정에 사용.
        public bool IsMoving => _moving;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
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

        private void FixedUpdate()
        {
            if (_moving == false)
                return;

            Vector3 next = Vector3.MoveTowards(transform.position, _target, _speed * Time.fixedDeltaTime);
            //# Y 평면 고정 — 캐릭터가 카메라 각도로 떠오르지 않도록
            next.y = 0f;

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
