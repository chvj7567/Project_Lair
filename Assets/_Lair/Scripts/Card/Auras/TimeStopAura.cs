using System;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 이동·공격 완전 정지. OnDetached 시 백업값 복원.
    //# 부착 동안 영웅 위치에 실드 FX(EVisual.TimeStopShield) 스폰, OnDetached 시 풀 반환(PoisonAura 패턴).
    [Serializable]
    public class TimeStopAura : IHeroAura, IStatusVisual
    {
        public ECardId IconCardId => ECardId.TimeStop;

        private readonly IMover _mover;
        private readonly IAttacker _attacker;
        private float _speedBackup;
        private bool _atkBackup;

        [NonSerialized] private CHPoolable _visualPoolable;

        public TimeStopAura(IMover mover, IAttacker attacker)
        {
            _mover = mover;
            _attacker = attacker;
        }

        public void OnAttached(IHealth hero)
        {
            if (_mover != null)
            {
                _speedBackup = _mover.Speed;
                _mover.Speed = 0f;
                _mover.Stop();
            }
            if (_attacker != null)
            {
                _atkBackup = _attacker.Enabled;
                _attacker.Enabled = false;
            }

            //# 실드 FX — 영웅을 감싸야 하니 영웅 위치 그대로(살짝 위 0.5) 스폰.
            if (hero is MonoBehaviour mb && mb != null)
            {
                Vector3 p = mb.transform.position;
                RequestVisualAt(new Vector3(p.x, p.y + 0.5f, p.z));
            }
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            if (_mover != null) _mover.Speed = _speedBackup;
            if (_attacker != null) _attacker.Enabled = _atkBackup;

            if (_visualPoolable != null)
            {
                //# Rule 03 §4 — Destroy 대신 풀 반환. root 레벨이라 안전.
                if (CHMPool.Instance != null)
                    CHMPool.Instance.Push(_visualPoolable);
            }
            _visualPoolable = null;
        }

        //# CHMResource 캐시 hit 시 즉시 callback (사전 워밍 후 즉시 처리). 인프라 null 시 무동작(부트 전/테스트).
        private void RequestVisualAt(Vector3 worldPos)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null)
                return;

            CHMResource.Instance.Load<GameObject>(EVisual.TimeStopShield, prefab =>
            {
                if (prefab == null)
                    return;
                if (CHMPool.Instance == null)
                    return;
                CHPoolable poolable = CHMPool.Instance.Pop(prefab, null);
                if (poolable == null)
                    return;
                _visualPoolable = poolable;
                _visualPoolable.transform.position = worldPos;
            });
        }
    }
}
