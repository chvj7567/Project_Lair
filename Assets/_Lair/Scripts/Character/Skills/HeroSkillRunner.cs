using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 부착 — 스킬 구간(HP 비율 + 스킬)을 HP% 게이트로 활성화하고 활성 스킬을 매 프레임 Tick.
    //# 영웅은 풀 객체(count 1) → OnEnable/OnDisable 에서 활성 상태 리셋(HeroAuraRunner 패턴).
    [RequireComponent(typeof(LairCharacter))]
    [RequireComponent(typeof(Health))]
    public class HeroSkillRunner : MonoBehaviour
    {
        private IHealth _health;
        private IReadOnlyList<HeroSkillPhase> _phases;
        private HeroSkillPhaseGate _gate;
        private IHeroSkillContext _ctx;

        private readonly List<IHeroSkillRuntime> _active = new();
        //# _active 와 같은 순서의 CastPriority — 클수록 앞(동시 조건 우선순위, chapter-3-hero §4).
        private readonly List<int> _activePriority = new();
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

        //# BattleController 가 영웅 스킬 구간(HeroArchetype_Skill 조립 결과)을 주입. 게이트를 구간 HP비율로 구성.
        public void Bind(IReadOnlyList<HeroSkillPhase> phases)
        {
            _phases = phases;
            if (phases == null)
            {
                _gate = null;
                return;
            }
            List<float> fractions = new List<float>(phases.Count);
            foreach (HeroSkillPhase p in phases)
                fractions.Add(p.HpFraction);
            _gate = new HeroSkillPhaseGate(fractions);
            ResetActive();
        }

        private void OnEnable() => _gate?.Reset();

        private void OnDisable() => ResetActive();

        private void Update()
        {
            if (_phases == null || _gate == null || _health == null || _health.IsAlive == false)
                return;

            _gate.Poll(_health.Ratio, _newly);
            for (int i = 0; i < _newly.Count; ++i)
            {
                HeroSkillData data = _phases[_newly[i]].Skill;
                if (data != null)
                {
                    int insertAt = _active.Count;
                    while (insertAt > 0 && _activePriority[insertAt - 1] < data.CastPriority)
                    {
                        --insertAt;
                    }
                    _active.Insert(insertAt, data.CreateRuntime());
                    _activePriority.Insert(insertAt, data.CastPriority);
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
            _activePriority.Clear();
        }
    }
}
