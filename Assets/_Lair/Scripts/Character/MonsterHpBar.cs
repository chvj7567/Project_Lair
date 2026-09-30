using UnityEngine;

namespace Lair.Character
{
    //# 빌더(AttachMonsterHpBar)가 몬스터 자식으로 생성하는 래퍼 GameObject 에 부착.
    //# 래퍼는 WorldSpace Canvas + 이 MonsterHpBar. 그 자식에 HpBar.prefab 인스턴스가 nest.
    public class MonsterHpBar : MonoBehaviour
    {
        //# 몬스터 HP바 표시 스위치 — true 로 바꾸면 복원 (영웅 HUD 는 별도 경로라 무영향).
        private const bool ShowBar = false;

        [SerializeField] private HpBarView _hpBar;   //# nest 된 HpBar.prefab 인스턴스의 View

        private IHealth _health;
        private Transform _cam;

        //# Rule 06 — 상위 캐릭터의 HP 를 루트 Character 로케이터 경유로 탐색 (구체 클래스 비참조).
        private void Awake()
        {
            LairCharacter character = GetComponentInParent<LairCharacter>();
            _health = character != null ? character.Get<IHealth>() : null;
        }

        //# 풀 재사용 시 재구독 + 카메라 재캐시 + 현재 HP 반영.
        private void OnEnable()
        {
            Camera mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;

            //# 몬스터 바는 현재/최대 텍스트를 숨긴다 (영웅 HUD 와 공유하는 prefab 이라 런타임 토글).
            if (_hpBar != null)
            {
                _hpBar.SetTextVisible(false);
                _hpBar.gameObject.SetActive(ShowBar);
            }

            if (_health == null)
            {
                LairCharacter character = GetComponentInParent<LairCharacter>();
                _health = character != null ? character.Get<IHealth>() : null;
            }
            if (_health == null) return;
            _health.OnChanged += HandleChanged;
            _health.OnDied += HandleDied;
            HandleChanged(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnChanged -= HandleChanged;
                _health.OnDied -= HandleDied;
            }
        }

        //# 사망 순간 즉시 숨김(monster-2d-conversion.md §5.5 불변식 3) — 풀 반환 지연(DespawnOnDeath._delay)과 무관.
        private void HandleDied() => gameObject.SetActive(false);

        //# 몬스터 루트(부모) 스케일을 상쇄해 목표 월드 높이를 유지(§6.3.7 — 종족별 절대 높이 표).
        public void SetHeightAboveRoot(float worldHeight)
        {
            float parentScaleY = transform.parent != null ? transform.parent.lossyScale.y : 1f;
            if (Mathf.Approximately(parentScaleY, 0f)) parentScaleY = 1f;
            Vector3 pos = transform.localPosition;
            pos.y = worldHeight / parentScaleY;
            transform.localPosition = pos;
        }

        //# 빌보드 — HP 바가 카메라 정면을 향하게.
        private void LateUpdate()
        {
            if (_cam != null) transform.rotation = _cam.rotation;
        }

        private void HandleChanged(int current, int max)
        {
            if (_hpBar != null) _hpBar.SetHp(current, max);
        }
    }
}
