using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# P2 (HP 60%) — 영웅 주위를 공전하는 3D 구(sphere) N개. 각 구 반경 안 몬스터에 인터벌마다 지속 데미지(per-sphere overlap, union dedup).
    public class OrbitingBladeSkillData : HeroSkillData
    {
        private int _damage;
        private float _hitInterval;
        private float _orbitRadius;
        private float _bladeSphereRadius;   //# 각 공전 구 반경 = 히트 반경 = 비주얼 반경
        private int _bladeCount;
        private float _rotationSpeedDeg;

        public int Damage => _damage;
        public float HitInterval => _hitInterval;
        public float OrbitRadius => _orbitRadius;
        public float BladeSphereRadius => _bladeSphereRadius;
        public int BladeCount => _bladeCount;
        public float RotationSpeedDeg => _rotationSpeedDeg;

        public OrbitingBladeSkillData(string displayName, Lair.Data.HeroSkillParams p) : base(displayName)
        {
            _damage = p.GetInt("damage");
            _hitInterval = p.Get("hitInterval");
            _orbitRadius = p.Get("orbitRadius");
            _bladeSphereRadius = p.Get("bladeSphereRadius");
            _bladeCount = p.GetInt("bladeCount");
            _rotationSpeedDeg = p.Get("rotationSpeedDeg");
        }

        public override IHeroSkillRuntime CreateRuntime() => new OrbitingBladeRuntime(this);
    }

    public class OrbitingBladeRuntime : IHeroSkillRuntime
    {
        private readonly OrbitingBladeSkillData _data;
        private float _accum;
        private float _angleDeg;
        //# 지속형이라 매 틱 울리면 안 됨 — 공전 블레이드 활성화 첫 틱 1회만 P3Skill 재생.
        private bool _played;
        //# 시트 FX 1개(블레이드 3개가 한 장에 포함) — 영웅 추적. 공전 각도에서 프레임을 직접 지정해 판정과 동기.
        private ChvjUnityInfra.CHPoolable _blade;
        private ISpriteSheetFx _bladeFx;
        //# 구 중심 재사용 버퍼 — 매 틱 _angleDeg 로 계산(transform 비의존, 인프라 미부팅에도 정상).
        private readonly Vector3[] _centers;

        public OrbitingBladeRuntime(OrbitingBladeSkillData data)
        {
            _data = data;
            int n = Mathf.Max(1, data.BladeCount);
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
            if (_blade == null)
            {
                _blade = HeroSkillFx.SpawnTracked(Lair.Data.EVisual.HeroOrbitBladeFx);
                if (_blade == null)
                    return;
                _bladeFx = _blade.GetComponent<ISpriteSheetFx>();   //# 스폰 시 1회 캐싱
            }
            _blade.transform.position = heroPos;
            _blade.transform.localScale = Vector3.one * _data.OrbitRadius;
            //# 시트는 시계 방향 영상 → 위상 반전(1 − frac)으로 판정 구(반시계)와 같은 방향·각도.
            if (_bladeFx != null)
                _bladeFx.SetLoopPhase(1f - OrbitPhase(_angleDeg));
        }

        //# 공전 각도(도) → 0~1 위상(frac). 음수 각도도 안전.
        public static float OrbitPhase(float angleDeg)
        {
            float p = angleDeg / 360f;
            return p - Mathf.Floor(p);
        }

        public void OnDeactivate()
        {
            if (ChvjUnityInfra.CHMPool.Instance == null)
                return;
            if (_blade != null)
            {
                ChvjUnityInfra.CHMPool.Instance.Push(_blade);
            }
            _blade = null;
            _bladeFx = null;
        }
    }
}
