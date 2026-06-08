using UnityEngine;
using Lair.Data;

namespace Lair.Character
{
    //# IRotator 구현 — Y(yaw) 회전 전용. deg/s 기반 부드러운 보간.
    //# AutoCombatAI 가 상태별로 FaceDirection 호출 → Update 에서 MoveTowardsAngle 로 따라감.
    public class SimpleRotator : MonoBehaviour, IRotator
    {
        //# 권장 540 deg/s — 기획서 §2.1. 180°를 0.33초. _snapInstant=true(영웅) 면 미사용.
        [SerializeField] private float _turnSpeedDegPerSec = 540f;

        //# 영웅 한정 즉시 스냅 (hero-animation-timing-sync §4.2). true 면 FaceDirection 이 보간 없이 즉시 ApplyYaw.
        //# 기본 false=현행 보간(몬스터 6종). 영웅 프리팹만 true. SerializeField 라 OnEnable 리셋 불요.
        [SerializeField] private bool _snapInstant;

        //# 보간 목표 yaw 와 목표 보유 여부. 풀 재사용 시 OnEnable 에서 false 로 리셋.
        private bool _hasTarget;
        private float _targetYaw;

        //# AttackAligned + _snapInstant 으로 이번 프레임 즉시 스냅했는지. 같은 프레임 Update 보간을 1회 건너뜀.
        //# MoveRotation 은 물리 step 까지 지연돼 같은 프레임 transform.eulerAngles 에 미반영 → 보간이 스냅을 덮어쓰는 것 방지.
        private bool _snappedThisFrame;

        //# Rigidbody 있으면 MoveRotation 사용. transform.rotation 직접 쓰기는 같은 바디의 MovePosition 을
        //# 덮어써(매 프레임 동기) 이동을 한 step 씩 무효화함 — 회전도 물리 경로로 통일.
        private Rigidbody _rigidbody;

        public float TurnSpeedDegPerSec
        {
            get => _turnSpeedDegPerSec;
            set => _turnSpeedDegPerSec = value;
        }

        //# 인자 없는 호출 — 기존 동작(공격 정렬) 보존을 위해 AttackAligned 로 위임.
        public void FaceDirection(Vector3 worldDir) => FaceDirection(worldDir, FacingMode.AttackAligned);

        public void FaceDirection(Vector3 worldDir, FacingMode mode)
        {
            //# Y 무시 — XZ 평면 yaw 만.
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.000001f) return;   //# magnitude < 0.001 → sqrMag < 1e-6
            _targetYaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            _hasTarget = true;
            //# AttackAligned + 영웅(_snapInstant) 일 때만 즉시 스냅(공격 정렬). 그 외엔 Update 에서 보간.
            if (mode == FacingMode.AttackAligned && _snapInstant)
            {
                ApplyYaw(_targetYaw);
                _snappedThisFrame = true;
            }
        }

        public void SnapToDirection(Vector3 worldDir)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.000001f) return;
            float yaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            _targetYaw = yaw;
            _hasTarget = true;
            //# X/Z = 0 강제. 외부에서 잘못된 회전이 들어와도 yaw 만 살림.
            ApplyYaw(yaw);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        //# Rule 12 — 풀 재사용 시 이전 목표 잔존 방지.
        //# transform.rotation 은 AutoCombatAI.OnEnable 의 SnapToDirection 이 별도로 처리.
        private void OnEnable()
        {
            _hasTarget = false;
            _targetYaw = 0f;
            _snappedThisFrame = false;
        }

        private void Update()
        {
            if (_hasTarget == false) return;
            //# 이번 프레임 AttackAligned 즉시 스냅함 — MoveRotation 은 물리 step 까지 지연돼 transform.eulerAngles 가
            //# 옛 yaw 라, 여기서 보간하면 그 스냅을 540°/s step 으로 덮어씀. 스냅한 프레임만 보간 1회 건너뜀.
            if (_snappedThisFrame)
            {
                _snappedThisFrame = false;
                return;
            }
            float currentYaw = transform.eulerAngles.y;
            float step = _turnSpeedDegPerSec * Time.deltaTime;
            float newYaw = Mathf.MoveTowardsAngle(currentYaw, _targetYaw, step);
            ApplyYaw(newYaw);
        }

        //# X/Z = 0 강제한 yaw-only 회전을 적용. Rigidbody 있으면 MoveRotation —
        //# transform.rotation 직접 쓰기는 같은 바디의 MovePosition 을 덮어써 이동을 무효화함.
        private void ApplyYaw(float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            if (_rigidbody != null)
            {
                _rigidbody.MoveRotation(rotation);
            }
            else
            {
                transform.rotation = rotation;
            }
        }
    }
}
