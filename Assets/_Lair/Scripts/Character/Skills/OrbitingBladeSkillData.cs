using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# P2 (HP 60%) — 영웅 주위를 공전하는 3D 구(sphere) N개. 각 구 반경 안 몬스터에 인터벌마다 지속 데미지(per-sphere overlap, union dedup).
    [CreateAssetMenu(fileName = "HeroSkill_OrbitingBlade", menuName = "Lair/Hero Skills/Orbiting Blade")]
    public class OrbitingBladeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _hitInterval = 0.3f;
        [SerializeField] private float _orbitRadius = 1.4f;
        [SerializeField] private float _bladeSphereRadius = 0.9f;   //# 각 공전 구 반경 = 히트 반경 = 비주얼 반경
        [SerializeField] private int _bladeCount = 3;
        [SerializeField] private float _rotationSpeedDeg = 180f;

        public int Damage => _damage;
        public float HitInterval => _hitInterval;
        public float OrbitRadius => _orbitRadius;
        public float BladeSphereRadius => _bladeSphereRadius;
        public int BladeCount => _bladeCount;
        public float RotationSpeedDeg => _rotationSpeedDeg;

        public override IHeroSkillRuntime CreateRuntime() => new OrbitingBladeRuntime(this);
    }

    public class OrbitingBladeRuntime : IHeroSkillRuntime
    {
        private readonly OrbitingBladeSkillData _data;
        private float _accum;
        private float _angleDeg;
        //# 지속형이라 매 틱 울리면 안 됨 — 공전 블레이드 활성화 첫 틱 1회만 P3Skill 재생.
        private bool _played;
        private readonly ChvjUnityInfra.CHPoolable[] _blades;
        //# 구 중심 재사용 버퍼 — 매 틱 _angleDeg 로 계산(transform 비의존, 인프라 미부팅에도 정상).
        private readonly Vector3[] _centers;

        public OrbitingBladeRuntime(OrbitingBladeSkillData data)
        {
            _data = data;
            int n = Mathf.Max(1, data.BladeCount);
            _blades = new ChvjUnityInfra.CHPoolable[n];
            _centers = new Vector3[n];
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            //# 공전 블레이드 활성화(해금) 첫 틱 — 스킬 켜짐 사운드 1회.
            if (_played == false)
            {
                _played = true;
                HeroSkillFx.PlaySound(Lair.Data.EAudio.P3Skill);
            }

            _angleDeg += _data.RotationSpeedDeg * dt;

            //# 데미지 — 인터벌 누적. 매 틱 현재 각도로 구 중심 N개 계산 후 union dedup 히트.
            _accum += dt;
            while (_accum >= _data.HitInterval)
            {
                _accum -= _data.HitInterval;
                ComputeCenters(ctx.HeroPosition);
                ctx.DamageMonstersInSpheres(_centers, _data.BladeSphereRadius, _data.Damage, 0f);
            }

            //# 비주얼 — 계산된 구 중심으로 블레이드 추적(가용 시).
            UpdateBlades(ctx.HeroPosition);
        }

        //# _angleDeg 기준 N개 구 중심 좌표를 _centers 에 채운다 — 순수 계산(transform·인프라 비의존).
        private void ComputeCenters(Vector3 heroPos)
        {
            float step = 360f / _centers.Length;
            for (int i = 0; i < _centers.Length; ++i)
            {
                float a = (_angleDeg + step * i) * Mathf.Deg2Rad;
                _centers[i] = heroPos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _data.OrbitRadius;
            }
        }

        private void UpdateBlades(Vector3 heroPos)
        {
            if (ChvjUnityInfra.CHMResource.Instance == null || ChvjUnityInfra.CHMPool.Instance == null)
                return;
            ComputeCenters(heroPos);
            float scale = _data.BladeSphereRadius * 2f;
            for (int i = 0; i < _blades.Length; ++i)
            {
                if (_blades[i] == null)
                {
                    _blades[i] = HeroSkillFx.SpawnTracked(Lair.Data.EVisual.HeroOrbitBladeFx);
                }
                if (_blades[i] == null)
                    continue;
                _blades[i].transform.position = _centers[i];
                _blades[i].transform.localScale = Vector3.one * scale;
            }
        }

        public void OnDeactivate()
        {
            if (ChvjUnityInfra.CHMPool.Instance == null)
                return;
            for (int i = 0; i < _blades.Length; ++i)
            {
                if (_blades[i] != null)
                {
                    ChvjUnityInfra.CHMPool.Instance.Push(_blades[i]);
                }
                _blades[i] = null;
            }
        }
    }
}
