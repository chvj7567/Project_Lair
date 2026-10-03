using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# Rule 02 §9 — Character 도메인 공용 인터페이스의 hero-skill 분할 파일.

    //# 영웅 스킬이 데미지를 줄 수 있는 몬스터 1체. CharacterRegistry.Entry 를 래핑하거나 테스트 더블이 구현.
    public interface ISkillTarget
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        IHealth Health { get; }
    }

    //# 영웅 스킬이 월드와 상호작용하는 단일 seam. 실구현은 HeroSkillContext(CharacterRegistry 순회),
    //# 테스트는 FakeHeroSkillContext(호출 기록). 스킬은 "언제·어떤 파라미터로" 만 결정하고 적용은 ctx 가 한다.
    public interface IHeroSkillContext
    {
        Vector3 HeroPosition { get; }

        //# 영웅 중심 XZ 링 [inner, outer] 안의 살아있는 교전 몬스터 전원에 amount 데미지(+넉백). 피격 수 반환.
        //# inner=0 이면 꽉 찬 디스크(노바).
        int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength);

        //# 영웅에서 direction 기준 ± halfAngleDeg·반경 length 의 radial 부채꼴 안 몬스터 전원에 amount 데미지(+넉백). 피격 수 반환.
        int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength);

        //# centers 각 구(반경 sphereRadius) union 안 몬스터 전원에 amount 데미지(+넉백). 여러 구 동시 진입도 1회만(union dedup). 고유 피격 수 반환.
        int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength);

        //# 영웅 중심 radius 내 몬스터 무게중심 (돌진 방향 결정용). 없으면 HeroPosition 반환.
        Vector3 MonsterCentroid(float radius);
    }

    //# 궁수 스킬 3종이 추가로 쓰는 월드 기능 (chapter-system §5.4). 기존 IHeroSkillContext 소비자·테스트 더블을 깨지 않으려 확장 인터페이스로 분리.
    //# 실구현 HeroSkillContext 가 둘 다 구현 — 스킬 런타임은 ctx as IArcherSkillContext 로 확인(없으면 LogError 후 보류).
    public interface IArcherSkillContext : IHeroSkillContext
    {
        //# 영웅에서 direction 방향 직사각(길이 length, 반폭 halfWidth) 안 살아있는 교전 몬스터 전원에 amount 데미지(+넉백). 피격 수 반환.
        int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength);

        //# 영웅 중심 searchRadius 안에서 clusterRadius 안에 몬스터가 가장 많이 모인 지점. 없으면 false.
        bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point);

        //# 영웅 중심 radius 안 살아있는 교전 몬스터 수.
        int CountMonstersInRadius(float radius);

        //# 도약 목적지 등을 전장(카이팅 아레나 소프트 반경) 안으로 클램프.
        Vector3 ClampToArena(Vector3 position);

        //# 회피 도약 — duration 동안 destination 으로 이동하고 그 동안 AI 이동 명령을 잠근다(이동 소유권은 스킬 쪽).
        void BeginLeap(Vector3 destination, float duration);
    }

    //# 활성화된 스킬 1개의 가변 상태(쿨다운 타이머·궤도 각도). HeroSkillData.CreateRuntime() 이 생성.
    public interface IHeroSkillRuntime
    {
        //# 내부 타이머를 dt 만큼 진행하고, 준비되면 ctx 로 데미지 적용 + 비주얼 갱신.
        void Tick(IHeroSkillContext ctx, float dt);

        //# 풀 반환/비활성 시 호출 — 점유 중인 풀 비주얼 반환.
        void OnDeactivate();
    }
}
