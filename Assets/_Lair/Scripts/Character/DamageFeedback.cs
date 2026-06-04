using Lair.Battle;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 피격자 측 — Health.OnChanged 델타<0 감지 → 임팩트+숫자 스폰. 색은 출처가 스탬프.
    //# HitFlash 와 동일한 _lastHp 델타 추적. Health 이벤트 시그니처 무변경.
    [RequireComponent(typeof(Health))]
    public class DamageFeedback : MonoBehaviour, IDamageColorSink
    {
        private Health _health;
        private Collider _collider;   //# 팝업 시작 높이 = bounds.max.y. 매 타격 GetComponent 회피용 캐시.
        private HitVictimKind _victimKind = HitVictimKind.Hero;   //# 임팩트 프리팹 분기 — MonsterTag 유무로 Awake 1회 판정(Rule 02 §5).
        private int _lastHp = -1;
        private Color _nextColor = Color.white;
        private bool _hasFreshStamp;   //# 직전에 전투 데미지원이 색을 스탬프했는가. SetMax/Heal/HpMaxScale 등 비전투 감소 제외용.
        private IHitFeedbackSpawner _spawner;   //# 기본 = HitFeedbackSpawner.Instance

        private void Awake()
        {
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            //# 몬스터 프리팹에만 MonsterTag 가 붙음(영웅엔 없음 — MonsterTag.cs 주석 참조). 1회 캐싱.
            MonsterTag tag = GetComponent<MonsterTag>();
            _victimKind = tag != null ? HitVictimKind.Monster : HitVictimKind.Hero;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnChanged += HandleChanged;
                //# Rule 03 §4 — 풀 재사용 시 _lastHp 재캐시 (Health.OnEnable 가 Current 복원 후).
                _lastHp = _health.Current;
            }
            _nextColor = Color.white;
            _hasFreshStamp = false;   //# Rule 03 §4 — 풀 재사용 시 이전 스탬프 누수 방지.
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnChanged -= HandleChanged;
        }

        //# 전투 데미지원이 TakeDamage 직전 호출. fresh stamp 를 세워 직후 OnChanged 감소를 전투 데미지로 인정.
        public void StampDamageColor(Color color)
        {
            _nextColor = color;
            _hasFreshStamp = true;
        }

        //# 테스트 주입구 — IHitFeedbackSpawner 스파이 주입.
        public void SetSpawnerForTest(IHitFeedbackSpawner s) => _spawner = s;

        private void HandleChanged(int current, int max)
        {
            if (_lastHp < 0)
            {
                _lastHp = current;
                return;
            }
            //# 전투 데미지(스탬프된 감소)만 스폰. 회복(증가)·SetMax/Heal/HpMaxScale 등 비전투 감소는 무시.
            if (current < _lastHp && _hasFreshStamp)
            {
                int amount = _lastHp - current;
                IHitFeedbackSpawner sp = _spawner ?? HitFeedbackSpawner.Instance;
                if (sp != null)
                {
                    Vector3 impactPos = transform.position;
                    //# 숫자 팝업 시작 Y = 콜라이더 world 상단(bounds.max.y). 콜라이더 없으면 원점 폴백.
                    Vector3 popupPos = ResolvePopupPosition();
                    sp.SpawnImpact(impactPos, _nextColor, _victimKind);
                    sp.SpawnPopup(popupPos, amount, _nextColor);
                }
                //# 스탬프 1회 소비 — 다음 비스탬프 감소가 이 색/플래그를 재사용하지 못하게.
                _hasFreshStamp = false;
            }
            _lastHp = current;
        }

        //# 콜라이더 bounds(world) 상단에서 팝업 시작 — x·z 는 bounds 중심으로 자연스럽게 정렬.
        //# 콜라이더 없으면 transform.position 폴백.
        private Vector3 ResolvePopupPosition()
        {
            if (_collider == null)
                return transform.position;

            Bounds b = _collider.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
    }
}
