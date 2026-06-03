using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# IAttacker 테스트 더블. Enabled/PowerScale 토글 + TryAttack/OnHit 추적.
    public class FakeAttacker : IAttacker
    {
        public float Range { get; set; } = 1.5f;
        public float Cooldown { get; set; } = 1f;
        public int Power { get; set; } = 50;
        public bool Enabled { get; set; } = true;

        //# B3 — 무력화/약화 카드 테스트용 데미지 배율.
        public float PowerScale { get; set; } = 1f;

        //# 영웅 흐름 역전 스위치 (hero-animation-timing-sync §B.2). 기본 false=즉시 경로(현행 더블 흐름 보존).
        public bool DeferStrike { get; set; }

        //# B3 — 공격 적중 이벤트.
        public event Action<IHealth> OnHit;
        public void RaiseOnHit(IHealth target) => OnHit?.Invoke(target);

        public int TryAttackCallCount { get; private set; }
        public int TryBeginAttackCallCount { get; private set; }
        public int TryApplyStrikeCallCount { get; private set; }

        public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            TryAttackCallCount++;
            return false;
        }

        public bool TryBeginAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            TryBeginAttackCallCount++;
            return false;
        }

        public bool TryApplyStrike(float now)
        {
            TryApplyStrikeCallCount++;
            return false;
        }
    }
}
