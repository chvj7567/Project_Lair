using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 모든 몬스터에 글로벌 버프를 매 tick 재적용. HeroAuraRunner 의 몬스터판.
    //# "상태 보존" 이 아니라 "매 tick 강제" — 중간 스폰 몬스터도 자동 포함, 만료 시 자연 복원.
    //# 카드 리뉴얼 v0.6 — GuardianRage 신규 추가 (Tank 한정 {Wisp, Wraith} 매핑, 기획서 §10.1 옵션 (ii)).
    public class MonsterBuffService
    {
        private class Buff { public EMonsterBuff Type; public float Remain; }

        private readonly List<Buff> _buffs = new();

        //# 카드 리뉴얼 v0.6 — EMonsterBuff 의 적용 종 한정 매핑.
        //# 미지정 buff 는 "전체 종" — 기존 Frenzy/IronWill/BerserkPower 동작 보존.
        //# GuardianRage 만 {Wisp, Wraith} 한정 (기획서 §10.1 디자인 단정).
        private static readonly Dictionary<EMonsterBuff, HashSet<EMonster>> TargetTypes
            = new Dictionary<EMonsterBuff, HashSet<EMonster>>
            {
                { EMonsterBuff.GuardianRage, new HashSet<EMonster> { EMonster.Wisp, EMonster.Wraith } },
            };

        public bool IsActive(EMonsterBuff type)
        {
            foreach (Buff b in _buffs) if (b.Type == type) return true;
            return false;
        }

        //# 같은 type 이 있으면 Remain 을 더 큰 값으로 연장.
        public void AddBuff(EMonsterBuff type, float duration)
        {
            foreach (Buff b in _buffs)
            {
                if (b.Type == type) { b.Remain = Mathf.Max(b.Remain, duration); return; }
            }
            _buffs.Add(new Buff { Type = type, Remain = duration });
        }

        //# 1) 만료 제거 2) 전체 몬스터 스케일 base 리셋 3) 활성 버프 곱셈 적용 (적용 종 한정).
        public void Tick(float dt)
        {
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                _buffs[i].Remain -= dt;
                if (_buffs[i].Remain <= 0f) _buffs.RemoveAt(i);
            }

            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null) continue;
                MeleeAttacker atk = e.Transform.GetComponent<MeleeAttacker>();
                Health hp  = e.Transform.GetComponent<Health>();
                SimpleMover mv = e.Transform.GetComponent<SimpleMover>();
                MonsterTag tag = e.Transform.GetComponent<MonsterTag>();
                //# 매 tick 오버레이 리셋. 이후 활성 buff 가 다시 곱연산.
                if (atk != null) { atk.CooldownScale = 1f; atk.PowerScale = 1f; }
                if (hp  != null)
                {
                    hp.DamageTakenScale = 1f;
                    //# [B1] HpMaxScale 도 매 tick 리셋. setter 가 비율 보존 로직을 자동 처리.
                    hp.HpMaxScale = 1f;
                }
                if (mv  != null) mv.SpeedScale = 1f;

                foreach (Buff b in _buffs)
                {
                    //# 적용 종 한정 매핑이 있으면 tag.Key 가 그 집합에 들어있을 때만 적용.
                    if (TargetTypes.TryGetValue(b.Type, out HashSet<EMonster> allowed))
                    {
                        if (tag == null || allowed.Contains(tag.Key) == false) continue;
                    }

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
                        case EMonsterBuff.GuardianRage:
                            //# 카드 리뉴얼 v0.6 [B1] — 적용 종 한정 {Wisp, Wraith} (위 TargetTypes 매핑으로 필터).
                            //# 기획서 §10.4 단정: 받는 데미지 ×0.5 + HP ×2.0 (동시 적용).
                            //# 받는 데미지 — DamageTakenScale 곱연산 오버레이.
                            //# HP ×2.0 — HpMaxScale 곱연산 오버레이 (setter 가 Current 비율 보존).
                            if (hp != null)
                            {
                                hp.DamageTakenScale *= 0.5f;
                                hp.HpMaxScale *= 2f;
                            }
                            break;
                        case EMonsterBuff.SwarmSpeed:
                            //# 카드 리뉴얼 v0.6 [B2] — Slow 카드의 이중 효과 (모든 몬스터 이동속도 ×1.3 시한).
                            //# 적용 종 한정 없음 (TargetTypes 매핑에 미등록 → "전체 종") — 기획서 §3.4 #7 단정.
                            //# SpeedScale 곱연산 — FixedUpdate 의 effectiveSpeed 계산에 반영.
                            if (mv != null) mv.SpeedScale *= 1.3f;
                            break;
                    }
                }
            }
        }
    }
}
