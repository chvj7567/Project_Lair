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

        //# B3 — 공격 적중 시 target 으로 발행. 플레이그 PlagueSlowOnHit 가 구독.
        event Action<IHealth> OnHit;

        //# 거리·쿨다운 만족 시 target.TakeDamage 호출 후 true.
        bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now);
    }

    //# ===== 타겟 검색 =====

    //# AutoCombatAI 에 적 검색 전략을 주입. Hero/Monster 의 유일한 차이점.
    public interface ITargetProvider
    {
        bool TryFindNearest(Vector3 from, out Transform target, out IHealth health);
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
}
