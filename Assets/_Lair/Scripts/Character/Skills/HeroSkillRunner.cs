using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 부착 — 로드아웃 페이즈를 HP% 게이트로 활성화하고 활성 스킬을 매 프레임 Tick.
    //# 영웅은 풀 객체(count 1) → OnEnable/OnDisable 에서 활성 상태 리셋(HeroAuraRunner 패턴).
    [RequireComponent(typeof(LairCharacter))]
    [RequireComponent(typeof(Health))]
    public class HeroSkillRunner : MonoBehaviour
    {
        private IHealth _health;
        private HeroSkillLoadout _loadout;
        private HeroSkillPhaseGate _gate;
        private IHeroSkillContext _ctx;

        private readonly List<IHeroSkillRuntime> _active = new();
        private readonly List<int> _newly = new();

        //# 해금 순간 발행 — BattleController 가 구독해 컷인 컨트롤러로 라우팅. 미구독 안전(null).
        public event Action<HeroSkillData> OnSkillUnlocked;

        private void Awake()
        {
            //# Rule 02 §5 — Awake 1회 캐싱 (런타임 경로 GetComponent 아님).
            LairCharacter character = GetComponent<LairCharacter>();
            _health = character.Get<IHealth>();
            _ctx = new HeroSkillContext(transform);
        }

        //# BattleController 가 로드아웃 로드 후 주입. 게이트를 페이즈 HP비율로 구성.
        public void Bind(HeroSkillLoadout loadout)
        {
            _loadout = loadout;
            if (loadout == null)
            {
                _gate = null;
                return;
            }
            List<float> fractions = new List<float>(loadout.Phases.Count);
            foreach (HeroSkillLoadout.Phase p in loadout.Phases)
                fractions.Add(p.HpFraction);
            _gate = new HeroSkillPhaseGate(fractions);
            ResetActive();
        }

        private void OnEnable() => _gate?.Reset();

        private void OnDisable() => ResetActive();

        private void Update()
        {
            if (_loadout == null || _gate == null || _health == null || _health.IsAlive == false)
                return;

            _gate.Poll(_health.Ratio, _newly);
            for (int i = 0; i < _newly.Count; ++i)
            {
                HeroSkillData data = _loadout.Phases[_newly[i]].Skill;
                if (data != null)
                {
                    _active.Add(data.CreateRuntime());
                    OnSkillUnlocked?.Invoke(data);   //# 컷인 트리거 (정지 중에도 다음 프레임까지 무해)
                }
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < _active.Count; ++i)
                _active[i].Tick(_ctx, dt);
        }

        private void ResetActive()
        {
            for (int i = 0; i < _active.Count; ++i)
                _active[i].OnDeactivate();
            _active.Clear();
        }
    }
}
