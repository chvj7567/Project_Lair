using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 공격력 영구 -25%. 무제한 지속 (HeroAuraRunner duration<0).
    //# PowerScale 에 _factor 를 1회 곱함 — 같은 카드 중복 선택 시 누적.
    //# 카드 리뉴얼 v0.6 [B3] — IDistinctHeroAura 구현. 같은 type 이지만 factor 가 다르면 신규 인스턴스로 부착되어
    //# Debuff Tier2 ×0.85 + 카드 픽 ×0.75 가 PowerScale 위에서 자연 곱연산 누적 (×0.6375 / ×0.4781 등).
    [Serializable]
    public class HeroAttackDownAura : IHeroAura, IStatusVisual, IDistinctHeroAura
    {
        public EVisual VisualKey => EVisual.AttackDownStatus;
        public Vector3 Offset => new Vector3(0.5f, 0.6f, 0f);

        private readonly IAttacker _attacker;
        private readonly float _factor;
        private bool _applied;

        //# [B3] 외부 비교용 — IDistinctHeroAura.ShouldStackAsNew 가 factor 비교에 사용.
        public float Factor => _factor;

        public HeroAttackDownAura(IAttacker attacker, float factor = 0.75f)
        {
            _attacker = attacker;
            _factor = factor;
        }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null || _applied) return;
            _attacker.PowerScale *= _factor;
            _applied = true;
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            //# 무제한 효과 — 정상 흐름에선 Detach 안 됨.
            //# 풀 회수 시엔 MeleeAttacker.OnEnable 의 PowerScale=1 리셋이 복원.
        }

        //# [B3] factor 가 동일한 재부착은 기존 가드로 무시 (같은 카드 픽 중복 방지),
        //# factor 가 다른 부착은 신규 인스턴스로 허용 → OnAttached 재호출로 PowerScale 곱연산 누적.
        public bool ShouldStackAsNew(IHeroAura existing)
        {
            HeroAttackDownAura other = existing as HeroAttackDownAura;
            if (other == null) return false;
            return Mathf.Approximately(_factor, other._factor) == false;
        }
    }
}
