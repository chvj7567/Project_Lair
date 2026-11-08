using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 프리팹의 쇼케이스 모드 루트 파사드(Rule 02 §10, village-monster-showcase §3). 마을·로딩에서 전투 프리팹을 순수 비주얼로 쓴다.
    //# 끈 것은 SetShowcaseMode(false) 한 번에 Pop 직후 상태로 복구된다(켜기 전 상태 기억). 하위 참조는 전부 인스펙터 와이어링(빌더가 배선).
    public class MonsterShowcase : MonoBehaviour, IMonsterShowcase
    {
        //# AutoCombatAI·MeleeAttacker·SimpleMover·SimpleRotator·CharacterAnimationDriver·DespawnOnDeath·AttackJuice·PlagueToxinOnHit·HitFlash·MonsterTargetProvider 등.
        [SerializeField] private Behaviour[] _battleBehaviours;
        [SerializeField] private Collider[] _colliders;
        //# HP바 래퍼·바닥 원판 Aura·강화 링 AuraSigil — 쇼케이스에서는 숨김(그림자는 씬이 그린다).
        [SerializeField] private GameObject[] _hiddenObjects;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _visualRoot;
        //# 몸 렌더러가 0번. 나머지(오라 원판·이펙트 자식)는 몸 기준 상대 order 를 유지한 채 함께 옮긴다.
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private MonsterEnhancementVisual _enhancement;

        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private bool[] _behaviourWasOn;
        private bool[] _colliderWasOn;
        private bool[] _hiddenWasActive;
        private int[] _baseLayers;
        private int[] _baseOrders;
        private Vector3 _visualBaseLocalPosition;
        private bool _captured;
        private bool _on;

        public float DisplayHeightWorld => _enhancement != null ? _enhancement.AppliedHeightWorld : 0f;

        private void Awake()
        {
            Capture();
        }

        private void Capture()
        {
            if (_captured)
                return;
            _captured = true;
            int renderers = _renderers != null ? _renderers.Length : 0;
            _baseLayers = new int[renderers];
            _baseOrders = new int[renderers];
            for (int i = 0; i < renderers; i++)
            {
                _baseLayers[i] = _renderers[i] != null ? _renderers[i].sortingLayerID : 0;
                _baseOrders[i] = _renderers[i] != null ? _renderers[i].sortingOrder : 0;
            }
            if (_visualRoot != null)
            {
                _visualBaseLocalPosition = _visualRoot.localPosition;
            }
        }

        public void SetShowcaseMode(bool on)
        {
            Capture();
            if (on == _on)
                return;
            _on = on;
            if (on)
            {
                Remember();
                SetAll(false);
            }
            else
            {
                Restore();
            }
        }

        private void Remember()
        {
            _behaviourWasOn = new bool[_battleBehaviours != null ? _battleBehaviours.Length : 0];
            for (int i = 0; i < _behaviourWasOn.Length; i++)
            {
                _behaviourWasOn[i] = _battleBehaviours[i] != null && _battleBehaviours[i].enabled;
            }
            _colliderWasOn = new bool[_colliders != null ? _colliders.Length : 0];
            for (int i = 0; i < _colliderWasOn.Length; i++)
            {
                _colliderWasOn[i] = _colliders[i] != null && _colliders[i].enabled;
            }
            _hiddenWasActive = new bool[_hiddenObjects != null ? _hiddenObjects.Length : 0];
            for (int i = 0; i < _hiddenWasActive.Length; i++)
            {
                _hiddenWasActive[i] = _hiddenObjects[i] != null && _hiddenObjects[i].activeSelf;
            }
        }

        private void SetAll(bool on)
        {
            for (int i = 0; i < _behaviourWasOn.Length; i++)
            {
                if (_battleBehaviours[i] != null)
                {
                    _battleBehaviours[i].enabled = on && _behaviourWasOn[i];
                }
            }
            for (int i = 0; i < _colliderWasOn.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = on && _colliderWasOn[i];
                }
            }
            for (int i = 0; i < _hiddenWasActive.Length; i++)
            {
                if (_hiddenObjects[i] != null)
                {
                    _hiddenObjects[i].SetActive(on && _hiddenWasActive[i]);
                }
            }
        }

        //# 켜기 전 상태로 — 비활성이던 것은 그대로 비활성. 정렬·몸 오프셋·Animator 속도도 원위치.
        private void Restore()
        {
            if (_behaviourWasOn != null)
            {
                SetAll(true);
            }
            RestoreSorting();
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = _visualBaseLocalPosition;
            }
            SetMoving(false);
        }

        public void SetMoving(bool moving)
        {
            if (_animator != null)
            {
                _animator.SetFloat(SpeedId, moving ? 1f : 0f);
            }
        }

        public void SetVisualOffset(Vector3 worldOffset)
        {
            if (_visualRoot != null)
            {
                _visualRoot.position = transform.position + worldOffset;
            }
        }

        public void SetSorting(int sortingLayerId, int bodyOrder)
        {
            Capture();
            if (_renderers == null || _renderers.Length == 0)
                return;
            int delta = bodyOrder - _baseOrders[0];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;
                _renderers[i].sortingLayerID = sortingLayerId;
                _renderers[i].sortingOrder = _baseOrders[i] + delta;
            }
        }

        private void RestoreSorting()
        {
            if (_renderers == null)
                return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;
                _renderers[i].sortingLayerID = _baseLayers[i];
                _renderers[i].sortingOrder = _baseOrders[i];
            }
        }

        //# 풀 Pop 시 쇼케이스 상태가 남지 않게(Rule 03 §4) — 호출자가 복구를 빼먹어도 안전망.
        private void OnEnable()
        {
            if (_on)
            {
                SetShowcaseMode(false);
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public bool IsOnForTest => _on;
        public void SetForTest(Behaviour[] behaviours, Collider[] colliders, GameObject[] hidden)
        {
            _battleBehaviours = behaviours;
            _colliders = colliders;
            _hiddenObjects = hidden;
        }
#endif
    }
}
