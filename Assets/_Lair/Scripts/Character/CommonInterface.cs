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
        void SetMax(int max, bool resetCurrent = true);
    }

    //# ===== 공격 =====

    //# 사거리·쿨다운·데미지 적용을 한 곳에서 담당. now 인자로 시간 의존성 주입 (테스트 가능).
    public interface IAttacker
    {
        float Range { get; }
        float Cooldown { get; }
        int Power { get; }

        //# 거리·쿨다운 만족 시 target.TakeDamage 호출 후 true.
        bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now);
    }

    //# ===== 타겟 검색 =====

    //# AutoCombatAI 에 적 검색 전략을 주입. Hero/Monster 의 유일한 차이점.
    public interface ITargetProvider
    {
        bool TryFindNearest(Vector3 from, out Transform target, out IHealth health);
    }
}
