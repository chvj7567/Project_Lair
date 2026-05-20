using UnityEngine;

namespace Lair.Character
{
    //# Vector3.MoveTowards 기반 단순 추적. 장애물 회피 없음. Slice A 한정.
    public class SimpleMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed = 3f;
        private bool _moving;
        private Vector3 _target;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        //# B3 — MoveTo 후 Stop 전까지 true. 출혈 카드가 영웅 이동 판정에 사용.
        public bool IsMoving => _moving;

        public void MoveTo(Vector3 target)
        {
            _target = target;
            _moving = true;
        }

        public void Stop()
        {
            _moving = false;
        }

        private void Update()
        {
            if (_moving == false) return;
            transform.position = Vector3.MoveTowards(
                transform.position, _target, _speed * Time.deltaTime);

            //# Y 평면 고정 — 캐릭터가 카메라 각도로 떠오르지 않도록
            var p = transform.position;
            transform.position = new Vector3(p.x, 0, p.z);
        }
    }
}
