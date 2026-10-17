using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# HeroArchetype_Guard row -> 불변 설정 (HeroArchetype_Guard.json, chapter-5 §2.2).
    public readonly struct GuardSettings
    {
        public readonly float RetributionLockSec;

        public GuardSettings(float retributionLockSec)
        {
            RetributionLockSec = retributionLockSec;
        }

        public static GuardSettings From(HeroArchetype_GuardRow row)
        {
            return new GuardSettings(row.retributionLockSec);
        }
    }

    //# 응징 표적 (chapter-5 §3.3) - 영웅에게 Debuff 축 디버프를 건 몬스터를 lockSec 동안 강제 표적으로 기억. Guardian 영웅에만 생성된다.
    public class GuardBehavior
    {
        private readonly GuardSettings _settings;
        private Transform _target;
        private IHealth _health;
        private float _lockUntil;

        public GuardBehavior(GuardSettings settings)
        {
            _settings = settings;
        }

        public GuardSettings Settings => _settings;

        //# 풀 재사용 - 표적 잔존 방지.
        public void Reset()
        {
            _target = null;
            _health = null;
            _lockUntil = 0f;
        }

        //# 가해 통지 - 가장 최근 가해자로 교체하고 잠금 시간을 새로 잰다.
        public void NotifyDebuffedBy(Transform source, IHealth sourceHealth, float now)
        {
            if (source == null || sourceHealth == null)
                return;
            _target = source;
            _health = sourceHealth;
            _lockUntil = now + _settings.RetributionLockSec;
        }

        //# 잠금 중이고 표적이 살아있을 때만 true. 사망/비활성/만료 시 잠금을 풀고 false.
        public bool TryGetLockedTarget(float now, out Transform target, out IHealth health)
        {
            target = null;
            health = null;
            if (_target == null)
                return false;
            if (now >= _lockUntil || _health == null || _health.IsAlive == false || _target.gameObject.activeInHierarchy == false)
            {
                Reset();
                return false;
            }
            target = _target;
            health = _health;
            return true;
        }
    }
}
