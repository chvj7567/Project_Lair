using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 영웅 GameObject 에 부착되어 여러 IHeroAura 를 매 frame Tick.
    //# Attach(aura, duration) — duration < 0 이면 무제한.
    [RequireComponent(typeof(Health))]
    public class HeroAuraRunner : MonoBehaviour
    {
        private class Slot
        {
            public IHeroAura Aura;
            public float Remain;
            public bool Indefinite;
            public CHPoolable Visual;   //# IStatusVisual 인 경우만 — root 레벨 풀 인스턴스
        }

        private readonly List<Slot> _slots = new();
        private IHealth _hero;

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

            //# 상태 visual 이 있으면 풀에서 Pop (root 레벨 — 영웅 자식 X).
            if (aura is IStatusVisual sv)
            {
                CHMResource.Instance.Load<GameObject>(sv.VisualKey, prefab =>
                {
                    if (prefab == null) return;
                    CHPoolable poolable = CHMPool.Instance.Pop(prefab, null);
                    if (poolable == null) return;
                    //# 콜백 도착 시 슬롯이 이미 사라졌으면 즉시 반환.
                    if (_slots.Contains(slot)) slot.Visual = poolable;
                    else                       CHMPool.Instance.Push(poolable);
                });
            }
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

                //# visual 이 있으면 영웅 위치 + Offset 으로 추적.
                if (s.Visual != null && s.Aura is IStatusVisual sv)
                    s.Visual.transform.position = transform.position + sv.Offset;

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
                        if (s.Visual != null) CHMPool.Instance.Push(s.Visual);
                        _slots.RemoveAt(i);
                    }
                }
            }
        }

        //# Rule 12 — 풀 반환 시 슬롯 cleanup. Aura.OnDetached 먼저 → visual Push 순서.
        private void OnDisable()
        {
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                try { _slots[i].Aura.OnDetached(_hero); } catch { }
                if (_slots[i].Visual != null) CHMPool.Instance.Push(_slots[i].Visual);
            }
            _slots.Clear();
        }
    }
}
