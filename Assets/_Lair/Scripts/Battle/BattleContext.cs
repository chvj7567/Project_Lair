using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# IBattleContext 의 구체 구현. BattleController 가 보유.
    //# 카드 효과 클래스가 이 인터페이스만 통해 부모 기능 사용 (Rule 06).
    public class BattleContext : IBattleContext
    {
        private readonly BattleController _owner;
        public float DeltaTime => Time.deltaTime;

        public BattleContext(BattleController owner) => _owner = owner;

        public IEnumerable<IHealth> GetMonsters(EMonster? filter = null)
        {
            //# 스냅샷 반환 — 호출자가 iteration 중 TakeDamage/Destroy 로 CharacterRegistry.Monsters
            //# 컬렉션을 수정해도 안전. yield return 으로 lazy 였을 때 ReplaceSlimesToGolem 같은 카드가
            //# Collection-modified 예외 일으킴.
            var result = new List<IHealth>();
            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || !e.Health.IsAlive) continue;
                if (filter.HasValue)
                {
                    var tag = e.Transform != null ? e.Transform.GetComponent<MonsterTag>() : null;
                    if (tag == null || tag.Key != filter.Value) continue;
                }
                result.Add(e.Health);
            }
            return result;
        }

        public IHealth GetHero()
        {
            foreach (var e in CharacterRegistry.Heroes)
                if (e?.Health != null && e.Health.IsAlive) return e.Health;
            return null;
        }

        public Transform GetHeroTransform()
        {
            foreach (var e in CharacterRegistry.Heroes)
                if (e?.Transform != null) return e.Transform;
            return null;
        }

        public IMover GetHeroMover()
        {
            foreach (var e in CharacterRegistry.Heroes)
                if (e?.Transform != null) return e.Transform.GetComponent<IMover>();
            return null;
        }

        public void SpawnMonster(EMonster key, Vector3 nearHero)
        {
            _owner.SpawnMonsterRuntime(key, nearHero);
        }

        public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f)
        {
            var heroT = GetHeroTransform();
            if (heroT == null) return;
            var runner = heroT.GetComponent<HeroAuraRunner>()
                      ?? heroT.gameObject.AddComponent<HeroAuraRunner>();
            runner.Attach(aura, durationSeconds);
        }

        //# B3 — 몬스터 글로벌 버프/피의 갈증은 BattleController 소유 서비스로 위임.
        public void AddMonsterBuff(EMonsterBuff type, float duration)
            => _owner.AddMonsterBuff(type, duration);

        public void ActivateBloodThirst(float duration)
            => _owner.ActivateBloodThirst(duration);

        //# B3 — 폭주 즉발. GetMonsters 스냅샷 순회 (Collection-modified 회피).
        //# Current/2 데미지는 사망을 일으키지 않아 안전하지만 일관성 위해 스냅샷 사용.
        public void HalveAllMonsterHp()
        {
            foreach (var m in GetMonsters())
                m.TakeDamage(m.Current / 2);
        }
    }
}
