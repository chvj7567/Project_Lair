using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 모든 몬스터에 글로벌 버프를 매 tick 재적용. HeroAuraRunner 의 몬스터판.
    //# "상태 보존" 이 아니라 "매 tick 강제" — 중간 스폰 몬스터도 자동 포함, 만료 시 자연 복원.
    public class MonsterBuffService
    {
        private class Buff { public EMonsterBuff Type; public float Remain; }

        private readonly List<Buff> _buffs = new();

        public bool IsActive(EMonsterBuff type)
        {
            foreach (var b in _buffs) if (b.Type == type) return true;
            return false;
        }

        //# 같은 type 이 있으면 Remain 을 더 큰 값으로 연장.
        public void AddBuff(EMonsterBuff type, float duration)
        {
            foreach (var b in _buffs)
            {
                if (b.Type == type) { b.Remain = Mathf.Max(b.Remain, duration); return; }
            }
            _buffs.Add(new Buff { Type = type, Remain = duration });
        }

        //# 1) 만료 제거 2) 전체 몬스터 스케일 base 리셋 3) 활성 버프 곱셈 적용.
        public void Tick(float dt)
        {
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                _buffs[i].Remain -= dt;
                if (_buffs[i].Remain <= 0f) _buffs.RemoveAt(i);
            }

            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null) continue;
                var atk = e.Transform.GetComponent<MeleeAttacker>();
                var hp  = e.Transform.GetComponent<Health>();
                if (atk != null) { atk.CooldownScale = 1f; atk.PowerScale = 1f; }
                if (hp  != null) hp.DamageTakenScale = 1f;

                foreach (var b in _buffs)
                {
                    switch (b.Type)
                    {
                        case EMonsterBuff.Frenzy:
                            if (atk != null) atk.CooldownScale *= 0.67f;
                            break;
                        case EMonsterBuff.IronWill:
                            if (hp != null) hp.DamageTakenScale *= 0.7f;
                            break;
                        case EMonsterBuff.BerserkPower:
                            if (atk != null) atk.PowerScale *= 3f;
                            break;
                    }
                }
            }
        }
    }
}
