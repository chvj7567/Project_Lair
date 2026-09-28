using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 2D 비주얼 루트(Rule 02 §10) — 카메라 정면 빌보드 + 좌우 반전 + 오버레이 프레임 순서를 소유.
    //# 하위(_body 스프라이트·_tierOverlay)는 private 소유, 외부는 이 컴포넌트가 매 프레임 갱신을 총괄(기획서 §5.8·§6.3.6).
    [RequireComponent(typeof(SpriteRenderer))]
    public class MonsterVisual2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        //# 강화 오버레이 — 강화 Lv0 몬스터는 없을 수 있음(null 허용).
        [SerializeField] private MonsterTierOverlay _tierOverlay;
        //# forward.x 데드존 — 세로 이동 중 좌우 깜빡임 방지(기획서 §5.8).
        [SerializeField] private float _flipDeadZone = 0.1f;

        private Transform _root;
        private Transform _cam;
        private bool _facingRight = true;

        private void Awake()
        {
            _root = transform.parent != null ? transform.parent : transform;
            if (_body == null) _body = GetComponent<SpriteRenderer>();
        }

        //# 풀 재사용 — 카메라 재캐시 + 기본 방향(오른쪽) 복귀(Rule 03 §4).
        private void OnEnable()
        {
            Camera mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;
            _facingRight = true;
            ApplyFacing();
        }

        //# 프레임 순서 소유 — 빌보드 회전 → 방향 판정 → 오버레이 동기(같은 프레임 안에서 Animator 갱신 이후).
        private void LateUpdate()
        {
            if (_cam != null) transform.rotation = _cam.rotation;

            UpdateFacing();
            ApplyFacing();

            if (_tierOverlay != null && _body != null) _tierOverlay.Tick(_body.sprite, _body.flipX);
        }

        private void UpdateFacing()
        {
            float x = _root.forward.x;
            if (x > _flipDeadZone)
            {
                _facingRight = true;
            }
            else if (x < -_flipDeadZone)
            {
                _facingRight = false;
            }
        }

        private void ApplyFacing()
        {
            if (_body != null) _body.flipX = _facingRight == false;
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetBodyForTest(SpriteRenderer body) => _body = body;
        public void SetTierOverlayForTest(MonsterTierOverlay overlay) => _tierOverlay = overlay;
        public bool FacingRightForTest => _facingRight;
#endif
    }
}
