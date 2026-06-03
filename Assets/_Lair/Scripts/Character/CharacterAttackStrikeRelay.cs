using UnityEngine;

namespace Lair.Character
{
    //# 애니 이벤트 수신구 — Visual(Animator) 자식 GameObject 에 부착 (hero-animation-timing-sync §2.3·§B4).
    //# Unity AnimationEvent 는 Animator 와 같은 GameObject 의 메서드만 호출(부모 전파 X)하므로 Visual 에 둔다.
    //# Awake 에서 GetComponentInParent 로 루트(Knight) 게임플레이 컴포넌트를 잡아 위임.
    //# 메서드명(OnAttackStrike/OnAttackEnd/OnSpawnAnimEnd)은 클립에 베이크된 functionName 과 글자 그대로 일치해야 함(§7.7).
    public class CharacterAttackStrikeRelay : MonoBehaviour
    {
        private MeleeAttacker _attacker;
        private IAttackGate _gate;
        private HeroEntryDriver _entryDriver;
        private AutoCombatAI _ai;

        private void Awake()
        {
            _attacker = GetComponentInParent<MeleeAttacker>();
            _gate = GetComponentInParent<IAttackGate>();
            _entryDriver = GetComponentInParent<HeroEntryDriver>();
            _ai = GetComponentInParent<AutoCombatAI>();
        }

        //# strike 프레임 — 캐싱 타겟에 데미지 적용 (재검사 포함). 데미지 실제 적용 순간 OnHit 발행.
        public void OnAttackStrike()
        {
            _attacker?.TryApplyStrike(Time.time);
        }

        //# 공격 클립 종료 — IsAttacking 해제 → 다음 공격 개시 판정 재개.
        public void OnAttackEnd()
        {
            _gate?.EndAttack();
        }

        //# spawn 클립 종료 — march(HeroEntryDriver)/spawn(AutoCombatAI) 게이트 open → 영웅 이동·전투 시작 허용(§1.2).
        public void OnSpawnAnimEnd()
        {
            _entryDriver?.OpenMarchGate();
            if (_ai != null)
                _ai.OpenSpawnGate();
        }
    }
}
