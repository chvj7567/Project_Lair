using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅이 이동 중일 때만 1초당 Max×_ratio 데미지.
    [Serializable]
    public class BleedAura : IHeroAura
    {
        private readonly IMover _mover;
        private readonly float _ratio;   //# 0.02 = 2%
        private float _acc;

        public BleedAura(IMover mover, float ratio = 0.02f)
        {
            _mover = mover;
            _ratio = ratio;
        }

        public void OnAttached(IHealth hero) { _acc = 0f; }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null || _mover == null || !_mover.IsMoving) return;
            _acc += dt;
            while (_acc >= 1f)
            {
                _acc -= 1f;
                //# RoundToInt — float 정밀도로 (int) 캐스팅이 2.0→1 로 깎이는 것 방지.
                hero.TakeDamage(Mathf.RoundToInt(hero.Max * _ratio));
            }
        }

        public void OnDetached(IHealth hero) { }
    }
}
