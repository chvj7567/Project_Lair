using System;
using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 영웅 GameObject 에 부착되어 여러 IHeroAura 를 매 frame Tick.
    //# Attach(aura, duration) — duration < 0 이면 무제한.
    //# IStatusVisual 인 aura 는 시작/종료 이벤트(OnStatusShown/OnStatusHidden)를 발행 —
    //# HP바 아래 상태 아이콘 표시는 BattleViewModel→HpBarView 가 처리(월드 visual 제거).
    [RequireComponent(typeof(LairCharacter))]
    [RequireComponent(typeof(Health))]
    public class HeroAuraRunner : MonoBehaviour, IDebuffSourceEvents, IHeroDebuffControl
    {
        private class Slot
        {
            public IHeroAura Aura;
            public float Remain;
            public bool Indefinite;
        }

        private readonly List<Slot> _slots = new();
        private IHealth _hero;

        //# 상태 아이콘 — key(aura 타입), 대표 ECardId. View 가 ECardId→Sprite 해석.
        public event Action<object, ECardId> OnStatusShown;
        public event Action<object> OnStatusHidden;

        //# 가해 몬스터가 있는 Debuff 축 부착 통지 - 성기사 응징 표적이 구독.
        public event Action<Transform, IHealth> OnDebuffedBy;

        //# 패시브 "신앙의 가호" - 설정된 영웅만 Debuff 축 출처(IDebuffAxisAura)를 보정. 풀 재사용이라 스폰마다 호출(미보유 영웅은 Clear).
        private bool _hasResist;
        private DebuffResistSettings _resist;

        public void ConfigureDebuffResist(DebuffResistSettings settings)
        {
            _resist = settings;
            _hasResist = true;
        }

        public void ClearDebuffResist() => _hasResist = false;

        //# 카드 "끈질긴 저주" (monster-growth-currencies §15.3) — Debuff 축 시한 디버프 지속 배율 = 1 + 누적 보너스(가산). 풀 재사용 리셋은 OnDisable.
        private float _debuffDurationBonus;

        public float DebuffDurationMul => 1f + _debuffDurationBonus;

        public void AddDebuffDurationBonus(float bonus)
        {
            if (bonus > 0f)
            {
                _debuffDurationBonus += bonus;
            }
        }

        private void Awake()
        {
            LairCharacter character = GetComponent<LairCharacter>();
            _hero = character.Get<IHealth>();
        }

        public void Attach(IHeroAura aura, float duration) => Attach(aura, duration, null, null);

        //# source/sourceHealth = 디버프를 건 몬스터(없으면 null). 두 부착 경로(BattleContext·PlagueToxinOnHit)가 모두 여기를 거친다.
        public void Attach(IHeroAura aura, float duration, Transform source, IHealth sourceHealth)
        {
            if (aura == null) return;

            IDebuffAxisAura axis = aura as IDebuffAxisAura;
            if (axis != null && axis.IsDebuffAxis)
            {
                //# 끈질긴 저주 — 시한 디버프 지속 x (1 + 0.25n). 신앙의 가호 x0.6 과 같은 지점에서 곱으로 합성(§15.3).
                if (duration > 0f)
                {
                    duration *= DebuffDurationMul;
                }
                //# 시한은 부착·연장 지속 x durationMul, 영구는 강도(감소분) 보정. 연장은 아래 Remain += duration 이라 자동 반영.
                if (_hasResist)
                {
                    if (duration > 0f)
                    {
                        duration *= _resist.DurationMul;
                    }
                    else if (duration < 0f && aura is IPenaltyScalable scalable)
                    {
                        aura = scalable.WithPenaltyScale(_resist.PenaltyMul);
                    }
                }
                if (source != null && sourceHealth != null)
                {
                    OnDebuffedBy?.Invoke(source, sourceHealth);
                }
            }

            //# 같은 type 의 aura 가 이미 부착돼 있으면 Remain 연장 + 새 인스턴스 무시.
            //# 예: BleedAura 가 이미 3초 남았는데 5초짜리 재부착 → Remain = 3 + 5 = 8초.
            foreach (Slot existing in _slots)
            {
                if (existing.Aura.GetType() == aura.GetType())
                {
                    //# 역병귀 독 — 중첩 +N · 지속 갱신(연장 누적 아님). 전 중첩이 이 슬롯 타이머 하나를 공유한다(§15.1).
                    if (existing.Aura is PlagueToxinAura toxin)
                    {
                        toxin.Merge(aura as PlagueToxinAura);
                        if (existing.Indefinite == false && duration > 0f)
                        {
                            existing.Remain = Mathf.Max(existing.Remain, duration);
                        }
                        return;
                    }
                    IDistinctHeroAura distinct = aura as IDistinctHeroAura;
                    if (distinct == null || distinct.ShouldStackAsNew(existing.Aura) == false)
                    {
                        if (existing.Indefinite == false && duration > 0f) existing.Remain += duration;
                        return;
                    }
                    //# Distinct + ShouldStackAsNew=true — 신규 부착 흐름으로 진입 (OnAttached 호출).
                    break;
                }
            }

            Slot slot = new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f };
            _slots.Add(slot);
            aura.OnAttached(_hero);

            //# 상태 아이콘 — 신규 슬롯이고 IStatusVisual 이면 표시 이벤트(key = aura 타입).
            if (aura is IStatusVisual sv)
                OnStatusShown?.Invoke(aura.GetType(), sv.IconCardId);
        }

        //# 정화 대상 시한 슬롯 - 출처와 무관하게 타입으로 판정 (SlowAura 는 Swarm Slow 카드 포함, chapter-5 §4 E-C5-1).
        private static bool IsPurifiable(Slot s)
        {
            if (s.Indefinite)
                return false;
            IHeroAura a = s.Aura;
            return a is SlowAura || a is PlagueToxinAura || a is FearAura || a is BleedAura || a is WeakenAura;
        }

        public int CountTimedDebuffs()
        {
            int count = 0;
            foreach (Slot s in _slots)
            {
                if (IsPurifiable(s))
                {
                    ++count;
                }
            }
            return count;
        }

        //# 해제한 슬롯 수 반환. OnDetached -> 아이콘 숨김 순서는 만료 경로와 동일.
        public int ClearTimedDebuffs()
        {
            int cleared = 0;
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                Slot s = _slots[i];
                if (IsPurifiable(s) == false)
                    continue;
                s.Aura.OnDetached(_hero);
                if (s.Aura is IStatusVisual)
                    OnStatusHidden?.Invoke(s.Aura.GetType());
                _slots.RemoveAt(i);
                ++cleared;
            }
            return cleared;
        }

        private void Update()
        {
            if (_hero == null) return;
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                //# 재진입 가드 — 이전 반복의 Tick/OnDetached 가 영웅 사망→OnDisable 을 유발해
                //# _slots 가 비워졌을 수 있다. 인덱스가 무효면 정리는 OnDisable 이 끝냈으므로 즉시 종료.
                if (i >= _slots.Count)
                    continue;

                Slot s = _slots[i];
                s.Aura.Tick(_hero, Time.deltaTime);

                //# Tick 이 영웅을 죽이면 OnDisable 이 _slots 를 비우거나 GameObject 가 꺼진다.
                //# 그 경우 cleanup 은 이미 OnDisable 이 수행했으니 더 진행하지 않고 루프 종료.
                if (_hero.IsAlive == false || _slots.Count == 0 || i >= _slots.Count
                    || ReferenceEquals(_slots[i], s) == false)
                    return;

                if (s.Indefinite == false)
                {
                    s.Remain -= Time.deltaTime;
                    if (s.Remain <= 0f)
                    {
                        s.Aura.OnDetached(_hero);
                        //# 현재 어떤 IHeroAura.OnDetached 도 _slots 를 변경(영웅 사망/runner disable)하지 않음
                        //# → OnDetached 재진입은 도달 불가. 아래 가드는 무해한 하드닝일 뿐, 재진입 플래그 등 추가 방어는 의도적으로 안 함.
                        if (_slots.Count == 0 || i >= _slots.Count || ReferenceEquals(_slots[i], s) == false)
                            return;
                        //# 상태 아이콘 — 만료 시 숨김 이벤트.
                        if (s.Aura is IStatusVisual)
                            OnStatusHidden?.Invoke(s.Aura.GetType());
                        _slots.RemoveAt(i);
                    }
                }
            }
        }

        //# Rule 12 — 풀 반환 시 슬롯 cleanup. Aura.OnDetached 먼저 → 상태 아이콘 숨김 순서.
        private void OnDisable()
        {
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                Slot s = _slots[i];
                try { s.Aura.OnDetached(_hero); } catch { }
                //# 상태 아이콘 — 풀 반환 시 무기한 상태 포함 모두 숨김(잔존 방지).
                if (s.Aura is IStatusVisual)
                    OnStatusHidden?.Invoke(s.Aura.GetType());
            }
            _slots.Clear();
            _debuffDurationBonus = 0f;
        }
    }
}
