using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 영웅 GameObject 에 부착되어 여러 IHeroAura 를 매 frame Tick.
    //# Attach(aura, duration) — duration < 0 이면 무제한.
    //# IStatusVisual 인 Aura 는 visual 을 중앙에서 Pop/추적/Push (Rule 12).
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
            //# 무제한(Indefinite) 슬롯은 변경 안 함. visual 도 재Pop 하지 않음.
            foreach (var existing in _slots)
            {
                if (existing.Aura.GetType() == aura.GetType())
                {
                    if (!existing.Indefinite && duration > 0f) existing.Remain += duration;
                    return;
                }
            }

            var slot = new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f };
            _slots.Add(slot);
            aura.OnAttached(_hero);

            //# 상태 visual 이 있으면 풀에서 Pop (root 레벨 — 영웅 자식 X).
            if (aura is IStatusVisual sv)
            {
                CHMResource.Instance.Load<GameObject>(sv.VisualKey, prefab =>
                {
                    if (prefab == null) return;
                    var poolable = CHMPool.Instance.Pop(prefab, null);
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
                var s = _slots[i];
                s.Aura.Tick(_hero, Time.deltaTime);

                //# visual 이 있으면 영웅 위치 + Offset 으로 추적.
                if (s.Visual != null && s.Aura is IStatusVisual sv)
                    s.Visual.transform.position = transform.position + sv.Offset;

                if (!s.Indefinite)
                {
                    s.Remain -= Time.deltaTime;
                    if (s.Remain <= 0f)
                    {
                        s.Aura.OnDetached(_hero);
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
