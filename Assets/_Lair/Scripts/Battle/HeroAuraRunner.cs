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
    [RequireComponent(typeof(Health))]
    public class HeroAuraRunner : MonoBehaviour
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

        private void Awake() => _hero = GetComponent<IHealth>();

        public void Attach(IHeroAura aura, float duration)
        {
            if (aura == null) return;

            //# 같은 type 의 aura 가 이미 부착돼 있으면 Remain 연장 + 새 인스턴스 무시.
            //# 예: PoisonAura 가 이미 3초 남았는데 5초짜리 재부착 → Remain = 3 + 5 = 8초.
            foreach (Slot existing in _slots)
            {
                if (existing.Aura.GetType() == aura.GetType())
                {
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
        }
    }
}
