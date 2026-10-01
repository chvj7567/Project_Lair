using Lair.Stage;
using UnityEngine;

namespace Lair.Village
{
    //# 마을 장식 몬스터 배회자(Rule 02 §10 루트) — scene-2d-conversion §5.6 · §7.7. 시안 stepWalker 규칙.
    //# 위치는 배경 도트 좌표로 굴리고 매 프레임 도트 단위로 반올림해 지면(XZ)에 놓는다 — 영웅과 같은 Z 깊이 정렬을 쓴다.
    public class VillageWanderer : MonoBehaviour
    {
        //# 도착 판정 거리(도트)
        private const float ArriveDots = 1f;

        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private Vector2[] _waypoints;
        [SerializeField] private float _speed = 26f;
        [SerializeField] private bool _flying;
        [SerializeField] private bool _startFacingLeft;
        //# 비행 흔들림 위상(시안 ph)
        [SerializeField] private float _hoverPhase;

        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private Vector2 _pos;
        private int _index;
        private float _wait;
        private bool _moving;
        private bool _flipLeft;
        private Transform _cam;

        //# 시안 규칙: 개수 1 이면 제자리, 아니면 (현재 + 1 + 오프셋[0, 개수−2]) mod 개수.
        public static int NextWaypointIndex(int current, int count, int randomOffset)
        {
            if (count <= 1)
                return 0;
            int offset = ((randomOffset % (count - 1)) + (count - 1)) % (count - 1);
            return (current + 1 + offset) % count;
        }

        //# 목표까지 거리 < 1도트면 도착(이동 없음), 아니면 speed×dt 만큼 직선 이동(목표 초과 금지).
        public static (Vector2 position, bool arrived) StepToward(Vector2 pos, Vector2 target, float speed, float dt)
        {
            Vector2 delta = target - pos;
            float d = delta.magnitude;
            if (d < ArriveDots)
                return (pos, true);
            float step = Mathf.Min(d, speed * dt);
            return (pos + delta / d * step, false);
        }

        private void Start()
        {
            Camera cam = Camera.main;
            _cam = cam != null ? cam.transform : null;
            _flipLeft = _startFacingLeft;
            if (_waypoints != null && _waypoints.Length > 0)
            {
                _pos = _waypoints[0];
            }
            _wait = Random.value * 2f;
            Apply();
        }

        private void Update()
        {
            if (_waypoints == null || _waypoints.Length == 0)
                return;
            Tick(Time.deltaTime);
            Apply();
        }

        private void Tick(float dt)
        {
            if (_wait > 0f)
            {
                _wait -= dt;
                _moving = false;
                return;
            }
            Vector2 target = _waypoints[_index];
            (Vector2 next, bool arrived) = StepToward(_pos, target, _speed, dt);
            if (arrived)
            {
                int offset = _waypoints.Length > 1 ? Random.Range(0, _waypoints.Length - 1) : 0;
                _index = NextWaypointIndex(_index, _waypoints.Length, offset);
                _wait = 1f + Random.value * 2.5f;
                _moving = false;
                return;
            }
            //# 시안: 목표까지 가로 거리 > 0.5도트일 때만 방향 갱신(세로 이동 중 깜빡임 방지)
            float dx = target.x - _pos.x;
            if (Mathf.Abs(dx) > 0.5f)
            {
                _flipLeft = dx < 0f;
            }
            _pos = next;
            _moving = true;
        }

        private void Apply()
        {
            Vector2 snapped = new Vector2(Mathf.Round(_pos.x), Mathf.Round(_pos.y));
            Vector3 ground = DotStageMapping.VillageDotToGround(snapped);
            transform.position = ground;

            if (_animator != null)
            {
                _animator.SetFloat(SpeedId, _moving ? 1f : 0f);
            }
            Quaternion face = _cam != null ? _cam.rotation : Quaternion.identity;
            Vector3 up = _cam != null ? _cam.up : Vector3.up;
            float hover = 0f;
            if (_flying)
            {
                hover = 10f + Mathf.Round(Mathf.Sin(3f * Time.time + _hoverPhase) * 2f);
            }
            if (_body != null)
            {
                _body.transform.rotation = face;
                _body.transform.position = ground + up * (hover / DotStageMapping.Ppu);
                _body.flipX = _flipLeft;
            }
            if (_shadow != null)
            {
                _shadow.transform.rotation = face;
                _shadow.transform.position = ground;
            }
        }
    }
}
