using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 유물 뇌운 조각 — intervalSec 마다 영웅 머리 위 낙뢰, 최대 HP x hpRatio 피해 (relics §6.2). 첫 낙뢰 = 부착 + intervalSec.
    //# 순수 피해라 IDebuffAxisAura 마커 없음(성기사 저항 비대상, D-7).
    [Serializable]
    public class ThunderShardAura : IHeroAura
    {
        //# 낙뢰 FX(피벗 하단)의 착탄점이 영웅 머리 높이에 오도록 올리는 높이(월드 단위, 카메라 기울기 보정).
        private const float FxLiftY = 0.4f;

        private readonly float _intervalSec;
        private readonly float _hpRatio;
        private float _acc;

        public ThunderShardAura(float intervalSec, float hpRatio)
        {
            _intervalSec = Mathf.Max(0.1f, intervalSec);
            _hpRatio = hpRatio;
        }

        //# 최대 HP 비례 피해 = Max(1, Round(최대HP x hpRatio)) — 순수 계산(테스트 가능).
        public int DamagePerStrike(int maxHp) => Mathf.Max(1, Mathf.RoundToInt(maxHp * _hpRatio));

        //# dt 누적 후 이번 호출에서 떨어질 낙뢰 수 — 순수 로직(Transform/풀 비의존).
        public int AdvanceStrikes(float dt)
        {
            _acc += dt;
            int strikes = 0;
            while (_acc >= _intervalSec)
            {
                _acc -= _intervalSec;
                ++strikes;
            }
            return strikes;
        }

        public void OnAttached(IHealth hero)
        {
            _acc = 0f;
        }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null)
                return;
            int strikes = AdvanceStrikes(dt);
            for (int i = 0; i < strikes; ++i)
            {
                if (hero.IsAlive == false)
                    return;
                //# FX/SFX 를 피해보다 먼저 — 피해로 영웅이 죽어 풀 반환되기 전에 위치를 읽는다.
                if (hero is Component comp && comp != null)
                {
                    HeroSkillFx.SpawnAtRequired(EVisual.RelicLightningFx, comp.transform.position + new Vector3(0f, FxLiftY, 0f), 1f, Quaternion.identity);
                    comp.GetComponent<LairCharacter>()?.Get<IDamageColorSink>()?.StampDamageColor(HitFeedbackPalette.Lightning);
                }
                HeroSkillFx.PlaySound(EAudio.RelicThunder);
                hero.TakeDamage(DamagePerStrike(hero.Max));
            }
        }

        public void OnDetached(IHealth hero) { }
    }
}
