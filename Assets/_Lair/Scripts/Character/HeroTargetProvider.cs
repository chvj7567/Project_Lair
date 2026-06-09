using UnityEngine;

namespace Lair.Character
{
    //# 영웅용 — Monsters 레지스트리에서 최근접 살아있는 적 검색.
    //# OnEnable 시 자기 자신을 Heroes 레지스트리에 등록.
    public class HeroTargetProvider : MonoBehaviour, ITargetProvider
    {
        private IHealth _selfHealth;
        //# Camera.main 은 내부적으로 tag 검색 — Awake 1회 캐싱(Rule 02 §5).
        private Camera _camera;

        private void Awake()
        {
            _selfHealth = GetComponent<IHealth>();
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (_selfHealth != null)
                CharacterRegistry.RegisterHero(transform, _selfHealth);
        }

        private void OnDisable()
        {
            CharacterRegistry.UnregisterHero(transform);
        }

        //# 영웅의 화면상 위치에 따라 자기 기준 화면 중앙 쪽 사분면의 적을 우선 타겟(없으면 전체 최근접 폴백).
        //# 카메라 viewport 판정을 월드 방향 벡터로 변환해 레지스트리에 전달(레지스트리는 카메라 비의존).
        public bool TryFindNearest(Vector3 from, out Transform target, out IHealth health)
        {
            ComputePreferredDirections(out Vector3 preferDirH, out Vector3 preferDirV);
            return CharacterRegistry.TryFindNearestMonster(from, preferDirH, preferDirV, out target, out health);
        }

        //# 영웅 본인 위치를 viewport 로 변환해 선호 월드 방향 산출. 카메라 null 이면 zero(=전체 최근접 폴백).
        //# 카메라 right/up 을 XZ 평면에 투영해 yaw 변화에도 안전(현재 씬은 yaw=0, pitch-down).
        private void ComputePreferredDirections(out Vector3 preferDirH, out Vector3 preferDirV)
        {
            preferDirH = Vector3.zero;
            preferDirV = Vector3.zero;
            if (_camera == null)
                return;

            Vector3 vp = _camera.WorldToViewportPoint(transform.position);
            Vector3 camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            Vector3 camUp = Vector3.ProjectOnPlane(_camera.transform.up, Vector3.up).normalized;

            //# 화면 왼쪽(<0.5) → 자기 기준 오른쪽(camRight) 우선, 오른쪽(≥0.5) → 왼쪽(-camRight).
            preferDirH = vp.x < 0.5f ? camRight : -camRight;
            //# 화면 위쪽(≥0.5) → 자기 기준 아래쪽(-camUp) 우선, 아래쪽(<0.5) → 위쪽(camUp).
            preferDirV = vp.y >= 0.5f ? -camUp : camUp;
        }

        public bool TryGetThreatCentroid(Vector3 from, float radius, out Vector3 centroid, out int count)
            => CharacterRegistry.TryGetThreatCentroidMonster(from, radius, out centroid, out count);

        //# 도주 안정화(A-1) — Engaging 무관 변형. 교전 토글 진동원 제거.
        public bool TryGetFleeCentroid(Vector3 from, float radius, out Vector3 centroid, out int count)
            => CharacterRegistry.TryGetFleeCentroidMonster(from, radius, out centroid, out count);
    }
}
