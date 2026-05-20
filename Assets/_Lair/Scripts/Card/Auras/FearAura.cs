using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 AutoCombatAI 도주 모드 ON. OnDetached 시 복원.
    [Serializable]
    public class FearAura : IHeroAura
    {
        private readonly AutoCombatAI _ai;

        public FearAura(AutoCombatAI ai) => _ai = ai;

        public void OnAttached(IHealth hero) { if (_ai != null) _ai.FleeMode = true; }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero) { if (_ai != null) _ai.FleeMode = false; }
    }
}
