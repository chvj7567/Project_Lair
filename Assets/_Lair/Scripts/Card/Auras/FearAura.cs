using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 AutoCombatAI 도주 모드 ON. OnDetached 시 복원.
    [Serializable]
    public class FearAura : IHeroAura, IStatusVisual
    {
        public ECardId IconCardId => ECardId.Fear;

        private readonly AutoCombatAI _ai;

        public FearAura(AutoCombatAI ai) => _ai = ai;

        public void OnAttached(IHealth hero) { if (_ai != null) _ai.FleeMode = true; }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero) { if (_ai != null) _ai.FleeMode = false; }
    }
}
