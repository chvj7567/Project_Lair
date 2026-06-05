using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# IHeroSkillContext 실구현 — CharacterRegistry.Monsters(살아있음+교전)를 SkillGeometry 로 필터링해 데미지/넉백.
    public class HeroSkillContext : IHeroSkillContext
    {
        private readonly Transform _hero;

        //# 영웅 공격 배율 소스 — 스킬 데미지에 PowerScale 을 입혀 근접과 일관. 생성자 1회 캐싱.
        private readonly IAttacker _heroAttacker;

        //# 타겟 스냅샷 재사용 버퍼 — 데미지로 몬스터 사망 시 Monsters 리스트가 수정되므로
        //# 열거(수집)와 변형(데미지/넉백)을 분리한다. 메인스레드 전용 → 재진입 없음.
        private readonly List<CharacterRegistry.Entry> _buffer = new();

        public HeroSkillContext(Transform hero)
        {
            _hero = hero;
            _heroAttacker = _hero != null ? _hero.GetComponent<IAttacker>() : null;
        }

        public Vector3 HeroPosition => _hero != null ? _hero.position : Vector3.zero;

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (SkillGeometry.InRing(e.Transform.position, origin, innerRadius, outerRadius) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (SkillGeometry.InCone(e.Transform.position, origin, direction, length, halfAngleDeg) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            if (sphereCenters == null || sphereCenters.Count == 0)
                return 0;
            //# 한 몬스터가 여러 구에 동시에 들어도 _buffer 에 1회만 add → union dedup.
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (InAnySphere(e.Transform.position, sphereCenters, sphereRadius) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        private static bool InAnySphere(Vector3 p, IReadOnlyList<Vector3> centers, float radius)
        {
            for (int i = 0; i < centers.Count; ++i)
            {
                if (SkillGeometry.InSphere(p, centers[i], radius))
                    return true;
            }
            return false;
        }

        public Vector3 MonsterCentroid(float radius)
        {
            if (CharacterRegistry.TryGetThreatCentroidMonster(HeroPosition, radius, out Vector3 c, out int n) && n > 0)
                return c;
            return HeroPosition;
        }

        //# 스냅샷 버퍼를 순회하며 데미지/넉백 적용. 이 루프에서 몬스터가 죽어 Monsters 가 수정돼도
        //# 순회 대상은 _buffer 라 안전. 반환값 = 실제 데미지를 적용한 수.
        private int ApplyAll(Vector3 origin, int amount, float knockbackStrength)
        {
            //# 적용 시점 live read — 부착된 디버프(HeroAttackDown/Weaken)가 자동 반영. null 이면 배율 1.
            //# 근접(MeleeAttacker)과 동일한 RoundToInt 라운딩. 넉백은 무변경.
            float scale = _heroAttacker != null ? _heroAttacker.PowerScale : 1f;
            int scaledAmount = Mathf.RoundToInt(amount * scale);
            int hit = 0;
            foreach (CharacterRegistry.Entry e in _buffer)
            {
                //# 수집~적용 사이 다른 효과로 죽었을 수 있으므로 재확인.
                if (Valid(e) == false)
                    continue;
                Apply(e, origin, scaledAmount, knockbackStrength);
                ++hit;
            }
            return hit;
        }

        private static bool Valid(CharacterRegistry.Entry e)
            => e != null && e.Transform != null && e.Health != null && e.Health.IsAlive && e.IsEngaging;

        private static void Apply(CharacterRegistry.Entry e, Vector3 origin, int amount, float knockback)
        {
            //# 데미지색 스탬프 — TakeDamage 가 OnChanged 를 동기 발행하기 전에 찍어야 DamageFeedback 이 팝업/임팩트를 띄운다(스킬=영웅 흰색, 근접과 일관).
            //# Valid(e) 가 Transform 비null 보장. GetComponent 는 스킬 발동(쿨다운 게이트) 시점·대상가변이라 캐싱 불가 → 1회성 허용(MeleeAttacker 와 동일).
            e.Transform.GetComponent<IDamageColorSink>()?.StampDamageColor(Color.white);
            e.Health.TakeDamage(amount);
            if (knockback > 0f)
            {
                Vector3 away = e.Transform.position - origin;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                {
                    e.Transform.position += away.normalized * knockback;
                }
            }
        }
    }
}
