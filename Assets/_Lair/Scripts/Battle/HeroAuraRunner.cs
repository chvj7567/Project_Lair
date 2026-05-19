using System.Collections.Generic;
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
        }

        private readonly List<Slot> _slots = new();
        private IHealth _hero;

        private void Awake() => _hero = GetComponent<IHealth>();

        public void Attach(IHeroAura aura, float duration)
        {
            if (aura == null) return;

            //# 같은 type 의 aura 가 이미 부착돼 있으면 Remain 연장 + 새 인스턴스 무시.
            //# 예: PoisonAura 가 이미 3초 남았는데 5초짜리 재부착 → Remain = 3 + 5 = 8초.
            //# 무제한(Indefinite) 슬롯은 변경 안 함.
            foreach (var existing in _slots)
            {
                if (existing.Aura.GetType() == aura.GetType())
                {
                    if (!existing.Indefinite && duration > 0f) existing.Remain += duration;
                    return;
                }
            }

            aura.OnAttached(_hero);
            _slots.Add(new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f });
        }

        private void Update()
        {
            if (_hero == null) return;
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                var s = _slots[i];
                s.Aura.Tick(_hero, Time.deltaTime);
                if (!s.Indefinite)
                {
                    s.Remain -= Time.deltaTime;
                    if (s.Remain <= 0f)
                    {
                        s.Aura.OnDetached(_hero);
                        _slots.RemoveAt(i);
                    }
                }
            }
        }

        //# Rule 12 — 풀 반환 시 슬롯 cleanup. 각 Aura 의 OnDetached 호출하여
        //# 자식 GameObject (예: PoisonAura_Visual) 도 풀로 반환되도록.
        private void OnDisable()
        {
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                try { _slots[i].Aura.OnDetached(_hero); } catch { }
            }
            _slots.Clear();
        }
    }
}
