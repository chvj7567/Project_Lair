using System;
using UnityEngine;

namespace Lair.Character
{
    //# Rule 10 — Character 도메인의 공용 인터페이스 단일 파일.
    //# 카테고리별 분리는 유지, 파일만 하나.

    //# ===== 이동 =====

    //# 위치 이동 추상. 구현체 교체로 Vector3 추적 → NavMesh 전환 가능.
    public interface IMover
    {
        float Speed { get; set; }
        bool IsMoving { get; }   //# B3 — 출혈 카드가 영웅 이동 여부 판정
        void MoveTo(Vector3 target);
        void Stop();
    }

    //# ===== HP / 데미지 =====

    //# 캐릭터 HP 추상. Health 구현체와 테스트용 FakeHealth 모두 만족.
    public interface IHealth
    {
        int Max { get; }
        int Current { get; }
        float Ratio { get; }
        bool IsAlive { get; }

        event Action<int, int> OnChanged;   //# (current, max)
        event Action OnDied;

        void TakeDamage(int amount);
        void Heal(int amount);   //# B3 — 피의 갈증 카드. Max 초과 불가
        void SetMax(int max, bool resetCurrent = true);
    }

    //# 데미지 숫자 색 스탬프 수신구. 데미지 출처가 TakeDamage 직전 호출.
    //# OnChanged 가 TakeDamage 내부에서 동기 발행되므로 직전 스탬프값이 핸들러에서 읽힌다.
    public interface IDamageColorSink
    {
        void StampDamageColor(Color color);
    }

    //# ===== 공격 =====

    //# 사거리·쿨다운·데미지 적용을 한 곳에서 담당. now 인자로 시간 의존성 주입 (테스트 가능).
    public interface IAttacker
    {
        float Range { get; }
        float Cooldown { get; }
        int Power { get; }

        //# 침묵/일시정지 효과용 토글. false 면 TryAttack 호출 자체가 정지하지 않더라도
        //# MonoBehaviour 의 Update 가 호출 안 되어 공격 시도 자체가 일어나지 않음.
        bool Enabled { get; set; }

        //# B3 — 데미지 배율 오버레이. 무력화/약화 카드가 IAttacker 타입으로 조작.
        float PowerScale { get; set; }

        //# 영웅 한정 흐름 역전 스위치 (hero-animation-timing-sync §B.2).
        //# true=공격 개시/strike 2분할(영웅), false=즉시 데미지(몬스터 6종 — 현행).
        bool DeferStrike { get; }

        //# B3 — 공격 적중 시 target 으로 발행. 플레이그 PlagueSlowOnHit 가 구독.
        event Action<IHealth> OnHit;

        //# 거리·쿨다운 만족 시 target.TakeDamage 호출 후 true. 몬스터(DeferStrike=false) 즉시 경로.
        bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now);

        //# 영웅 흐름 역전 — 개시 판정만 (사거리·쿨다운[CooldownScale]·타겟 유효 검사 + 쿨다운 기록 + 타겟 캐싱).
        //# 데미지 적용 안 함. true=공격 애니 트리거해도 됨 (§2.4).
        bool TryBeginAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now);

        //# 영웅 흐름 역전 — strike 프레임. 캐싱 타겟 사거리·생존 재검사 후 데미지[PowerScale strike시점] + OnHit (§2.4).
        bool TryApplyStrike(float now);
    }

    //# ===== 공격 게이트 (영웅 한정) =====

    //# 영웅 공격 모션 재생 중 상태(IsAttacking) 소유 + 개시 신호 중계 (hero-animation-timing-sync §3.2·§2.7).
    //# 루트에 부착(영웅 프리팹만). 몬스터는 미부착 → AutoCombatAI 가 null 로 잡아 보류 로직 미적용.
    //# 게임플레이 판정은 안 함 — AutoCombatAI/MeleeAttacker 가 통과시킨 개시 결과만 받아 애니 신호 발행.
    public interface IAttackGate
    {
        bool IsAttacking { get; }

        //# 개시 시점 호출 — IsAttacking=true + 공격 애니 트리거 개시 신호 발행(§2.7 ②).
        void BeginAttack();

        //# 클립 종료(OnAttackEnd) relay 호출 — IsAttacking=false.
        void EndAttack();

        //# 개시 신호 — Driver(View)가 구독해 TriggerAttack 출력. 게임플레이 판정 미포함.
        event Action OnAttackBegin;
    }

    //# ===== 타겟 검색 =====

    //# AutoCombatAI 에 적 검색 전략을 주입. Hero/Monster 의 유일한 차이점.
    public interface ITargetProvider
    {
        bool TryFindNearest(Vector3 from, out Transform target, out IHealth health);

        //# B3 공포 도주 안정화 — from 반경 내 살아있는 적들의 무게중심과 개수 조회.
        //# count>0 이면 (pos - centroid) 로 무리 바깥 도주. 최근접 1마리 진동 방지.
        bool TryGetThreatCentroid(Vector3 from, float radius, out Vector3 centroid, out int count);
    }

    //# ===== 회전 =====

    //# Y축(yaw) 회전 추상. AutoCombatAI 가 상태별로 FaceDirection 호출.
    //# 풀 재사용 시 SnapToDirection 으로 초기 방향 즉시 적용.
    public interface IRotator
    {
        //# deg/s. 인스펙터 또는 BalanceConfig 로 설정.
        float TurnSpeedDegPerSec { get; set; }

        //# 목표 방향 설정 — 매 Update 호출 가능. magnitude < 0.001f 면 no-op.
        //# Y 성분 무시 (XZ 평면 yaw 만 계산).
        void FaceDirection(Vector3 worldDir);

        //# 즉시 스냅 — OnEnable / 초기 스폰 시 사용. magnitude < 0.001f 면 no-op.
        void SnapToDirection(Vector3 worldDir);
    }

    //# ===== 애니메이션 채널 =====

    //# 애니메이션 구동의 Unity 의존을 격리하는 sink. 실제 구현은 Animator 래퍼,
    //# 테스트는 Fake 로 대체 → CharacterAnimationController 를 EditMode 에서 검증 가능.
    public interface IAnimatorSink
    {
        void SetSpeed(float speed);
        //# variant 0=slash01 / 1=slash02 / 2=stab (Knight.controller AttackVariant 파라미터).
        void TriggerAttack(int variant);
        void TriggerHit();
        void SetDead(bool dead);
        void TriggerSpawn();
    }
}
