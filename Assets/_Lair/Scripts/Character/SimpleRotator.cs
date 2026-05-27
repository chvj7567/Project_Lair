using UnityEngine;

namespace Lair.Character
{
    //# IRotator 구현 — Y(yaw) 회전 전용. deg/s 기반 부드러운 보간.
    //# AutoCombatAI 가 상태별로 FaceDirection 호출 → Update 에서 MoveTowardsAngle 로 따라감.
    //# Rule 12 — CHMPool 재사용 시 OnEnable 에서 내부 상태 리셋. transform.rotation 은 외부(AutoCombatAI.OnEnable)
    //# 에서 SnapToDirection 으로 강제하므로 여기서는 _hasTarget / _targetYaw 만 비운다.
    public class SimpleRotator : MonoBehaviour, IRotator
    {
        //# 권장 540 deg/s — 기획서 §2.1. 180°를 0.33초.
        [SerializeField] private float _turnSpeedDegPerSec = 540f;

        //# 보간 목표 yaw 와 목표 보유 여부. 풀 재사용 시 OnEnable 에서 false 로 리셋.
        private bool _hasTarget;
        private float _targetYaw;

        public float TurnSpeedDegPerSec
        {
            get => _turnSpeedDegPerSec;
            set => _turnSpeedDegPerSec = value;
        }

        public void FaceDirection(Vector3 worldDir)
        {
            //# Y 무시 — XZ 평면 yaw 만.
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.000001f) return;   //# magnitude < 0.001 → sqrMag < 1e-6
            _targetYaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            _hasTarget = true;
        }

        public void SnapToDirection(Vector3 worldDir)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.000001f) return;
            float yaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            _targetYaw = yaw;
            _hasTarget = true;
            //# X/Z = 0 강제. 외부에서 잘못된 회전이 들어와도 yaw 만 살림.
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        //# Rule 12 — 풀 재사용 시 이전 목표 잔존 방지.
        //# transform.rotation 은 AutoCombatAI.OnEnable 의 SnapToDirection 이 별도로 처리.
        private void OnEnable()
        {
            _hasTarget = false;
            _targetYaw = 0f;
        }

        private void Update()
        {
            if (_hasTarget == false) return;
            float currentYaw = transform.eulerAngles.y;
            float step = _turnSpeedDegPerSec * Time.deltaTime;
            float newYaw = Mathf.MoveTowardsAngle(currentYaw, _targetYaw, step);
            //# 매 적용 시 X/Z = 0 강제 — 외부 회전 간섭 방지.
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
        }
    }
}
