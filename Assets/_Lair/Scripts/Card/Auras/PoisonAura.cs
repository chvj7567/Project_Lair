using System;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 발 밑에 독장판 고정 + 영웅이 그 영역 안에 있을 때만 1초마다 _dps 데미지.
    //# Visual 은 OnAttached 시점의 영웅 위치에 고정 — 영웅 이동에 따라가지 않음.
    //# Visual 은 root 레벨 (영웅 자식 X) — Push 시 부모 deactivating 제약 회피.
    [Serializable]
    public class PoisonAura : IHeroAura
    {
        private readonly float _dps;
        private readonly float _radius;
        private readonly float _radiusSq;
        private float _tickAccumulator;

        [NonSerialized] private CHPoolable _visualPoolable;
        [NonSerialized] private Transform _heroTransform;

        public PoisonAura(float dps, float radius = 1.25f)
        {
            _dps = dps;
            _radius = radius;
            _radiusSq = radius * radius;
        }

        public void OnAttached(IHealth hero)
        {
            _tickAccumulator = 0f;
            if (hero is MonoBehaviour mb && mb != null)
            {
                _heroTransform = mb.transform;
                Vector3 p = mb.transform.position;
                //# Floor(y=0) 위 0.05 — z-fight 회피
                RequestVisualAt(new Vector3(p.x, 0.05f, p.z));
            }
        }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null || _heroTransform == null || _visualPoolable == null) return;

            int ticks = AccumulateDamageTicks(
                _heroTransform.position, _visualPoolable.transform.position, dt);
            for (int i = 0; i < ticks; ++i)
                hero.TakeDamage((int)_dps);
        }

        //# 영역판정 + 1초 누적 코어 (순수 로직 — Transform/CHPoolable 비의존, 테스트 가능).
        //# heroPos 가 diskPos 의 XZ 반경 _radius 내일 때만 dt 누적, 이번 호출의 1초 틱 수 반환.
        public int AccumulateDamageTicks(Vector3 heroPos, Vector3 diskPos, float dt)
        {
            float dx = heroPos.x - diskPos.x;
            float dz = heroPos.z - diskPos.z;
            if (dx * dx + dz * dz > _radiusSq) return 0;   //# 밖 — accumulator 유지

            _tickAccumulator += dt;
            int ticks = 0;
            while (_tickAccumulator >= 1f)
            {
                _tickAccumulator -= 1f;
                ++ticks;
            }
            return ticks;
        }

        public void OnDetached(IHealth hero)
        {
            if (_visualPoolable != null)
            {
                //# Rule 12 — Destroy 대신 풀 반환. root 레벨이라 SetParent 안전.
                CHMPool.Instance.Push(_visualPoolable);
            }
            _visualPoolable = null;
            _heroTransform = null;
        }

        //# CHMResource 캐시 hit 시 즉시 callback (사전 워밍 후 즉시 처리).
        private void RequestVisualAt(Vector3 worldPos)
        {
            CHMResource.Instance.Load<GameObject>(EVisual.PoisonAura, prefab =>
            {
                if (prefab == null) return;
                CHPoolable poolable = CHMPool.Instance.Pop(prefab, null);
                if (poolable == null) return;
                _visualPoolable = poolable;
                _visualPoolable.transform.position = worldPos;
            });
        }
    }
}
