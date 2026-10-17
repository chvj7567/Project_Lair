using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 기본 공격 시각 FX(궁수 화살·암살자 베기 — _fxKey 로 구분) — 판정과 분리(chapter-system §5.2). 데미지는 MeleeAttacker 가 strike 시점에 hit-scan 으로 이미 적용했고,
    //# 이 컴포넌트는 IAttacker.OnHit 을 받아 영웅→타깃 직선 화살 궤적만 띄운다. FX 에셋이 없으면 LogError(조용히 건너뛰지 않음), 판정엔 영향 없음.
    [RequireComponent(typeof(LairCharacter))]
    public class RangedHitFx : MonoBehaviour
    {
        [SerializeField] private EVisual _fxKey = EVisual.ArcherArrowFx;

        private IAttacker _attacker;

        private void Awake()
        {
            _attacker = GetComponent<LairCharacter>().Get<IAttacker>();
        }

        //# 풀 재사용 시 구독 중복 방지 — 활성화마다 구독, 비활성화마다 해제.
        private void OnEnable()
        {
            if (_attacker != null)
            {
                _attacker.OnHit += HandleHit;
            }
        }

        private void OnDisable()
        {
            if (_attacker != null)
            {
                _attacker.OnHit -= HandleHit;
            }
        }

        private void HandleHit(IHealth target)
        {
            Component targetComponent = target as Component;
            if (targetComponent == null)
                return;

            Vector3 from = transform.position;
            Vector3 toTarget = targetComponent.transform.position - from;
            HeroSkillFx.SpawnLineRequired(_fxKey, from, toTarget, toTarget.magnitude);
        }
    }
}
