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
        //# 카드 리뉴얼 v0.6 — 빌드 시너지 코어 (Phase 1 Task 4). BattleController 가 주입.
        private readonly BuildSynergyService _synergy;
        public float DeltaTime => Time.deltaTime;

        public BattleContext(BattleController owner, BuildSynergyService synergy)
        {
            _owner = owner;
            _synergy = synergy;
        }

        public IEnumerable<IHealth> GetMonsters(EMonster? filter = null)
        {
            //# 스냅샷 반환 — 호출자가 iteration 중 TakeDamage/Destroy 로 CharacterRegistry.Monsters
            //# 컬렉션을 수정해도 안전. yield return 으로 lazy 였을 때 ReplaceWispsToWraith 같은 카드가
            //# Collection-modified 예외 일으킴.
            List<IHealth> result = new List<IHealth>();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || e.Health.IsAlive == false) continue;
                if (filter.HasValue)
                {
                    MonsterTag tag = e.Transform != null ? e.Transform.GetComponent<MonsterTag>() : null;
                    if (tag == null || tag.Key != filter.Value) continue;
                }
                result.Add(e.Health);
            }
            return result;
        }

        public IHealth GetHero()
        {
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Heroes)
                if (e?.Health != null && e.Health.IsAlive) return e.Health;
            return null;
        }

        public Transform GetHeroTransform()
        {
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Heroes)
                if (e?.Transform != null) return e.Transform;
            return null;
        }

        public IMover GetHeroMover()
        {
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Heroes)
                if (e?.Transform != null) return e.Transform.GetComponent<IMover>();
            return null;
        }

        public void SpawnMonster(EMonster key, Vector3 nearHero)
        {
            _owner.SpawnMonsterRuntime(key, nearHero);
        }

        public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f)
        {
            Transform heroT = GetHeroTransform();
            if (heroT == null) return;
            HeroAuraRunner runner = heroT.GetComponent<HeroAuraRunner>()
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
            foreach (IHealth m in GetMonsters())
                m.TakeDamage(m.Current / 2);
        }

        //# 지속 스폰 — 강화/추가소환/융합 카드는 BattleController 가 보유한
        //# dict·Spawner 집합 조작이 필요하므로 위임.
        public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
            => _owner.RegisterMonsterTypeBuff(type, stat, multiplier);

        public void IncrementSpawnerOutput(EMonster type)
            => _owner.IncrementSpawnerOutput(type);

        public void ReplaceSpawnerOutput(EMonster from, EMonster to)
            => _owner.ReplaceSpawnerOutput(from, to);

        //# 카드 리뉴얼 v0.6 — 카드 픽 hook. BuildSynergyService 에 위임, 본 ctx 자신을 임계 발화 인자로 전달.
        //# _synergy null 가드: 테스트가 단축 생성자 없이 부른 경우 안전 분기.
        public void RegisterCardPick(EBuildAxis axis)
        {
            if (_synergy == null) return;
            _synergy.RegisterPick(axis, this);
        }

        public int GetBuildCount(EBuildAxis axis)
        {
            if (_synergy == null) return 0;
            return _synergy.GetCount(axis);
        }

        //# 카드 리뉴얼 v0.6 — Tank Tier3 시너지 / SpawnerHaste / Swarm Tier2·3 표면.
        //# BattleController 가 _monsterCap (필드 승격) + Spawner 컴포넌트 집합 조작을 위임.
        public void IncrementGlobalMonsterCap(int delta)
            => _owner.IncrementGlobalMonsterCap(delta);

        public void ScaleAllSpawnerPeriods(float mul)
            => _owner.ScaleAllSpawnerPeriods(mul);

        public void IncrementAllSpawnerOutputs(int delta)
            => _owner.IncrementAllSpawnerOutputs(delta);
    }
}
