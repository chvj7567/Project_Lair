using System;
using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 봉인된 둥지 (chapter-system §3.4) — 특정 종족 스포너의 주기 스폰을 봉인하고, 영웅 HP 가 unlockHpFraction 이하가 되면 해제.
    //# 봉인은 주기 스폰만 막는다 — 카드 즉시 소환·필드 몬스터는 무관(E-3). 해제 시 즉시 1회 스폰 버스트 + 알림.
    //# 해제 판정은 래치 — 회복으로 HP 가 올라가도 다시 봉인되지 않는다(E-1).
    public class SealedNestRule : IStageRule
    {
        //# 봉인 해제 알림 문구 id — "{0} 둥지의 봉인이 풀렸다!" (Strings_Ko).
        public const int UnlockNoticeStringId = 1231;

        private readonly EMonster _species;
        private readonly float _unlockHpFraction;

        private IStageRuleContext _ctx;
        private readonly List<IStageFx> _chains = new List<IStageFx>();
        private bool _locked;

        public SealedNestRule(StageRuleParams p)
        {
            int species = p.GetInt("lockedSpecies");
            if (Enum.IsDefined(typeof(EMonster), species) == false)
                throw new ArgumentException($"SealedNest.lockedSpecies 가 EMonster 에 없음: {species}");
            _species = (EMonster)species;
            _unlockHpFraction = p.Get("unlockHpFraction");
        }

        public EStageRule Id => EStageRule.SealedNest;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            IsActive = true;
            Vector3[] positions = _ctx.LockSpawners(_species);
            if (positions.Length == 0)
            {
                Debug.LogError($"[SealedNestRule] 봉인할 {_species} 스포너가 씬에 없음 — 규칙 무효");
                return;
            }
            _locked = true;
            foreach (Vector3 position in positions)
            {
                _chains.Add(_ctx.ShowFx(EVisual.StageSealChainFx, position, 1f));
            }
        }

        public void Tick(float elapsed, float dt)
        {
            if (IsActive == false || _locked == false)
                return;
            if (_ctx.HeroHpRatio > _unlockHpFraction)
                return;

            Unlock();
            _ctx.ShowNotice(UnlockNoticeStringId, SpeciesVisual.SpeciesName(_species));
        }

        public void Deactivate()
        {
            if (IsActive == false)
                return;
            IsActive = false;
            //# 전투 종료 정리 — 해제 버스트 없이 봉인만 푼다(종료 후 스폰 방지는 호스트의 결과 가드).
            if (_locked)
            {
                _locked = false;
                _ctx.UnlockSpawners(_species);
            }
            ReleaseChains();
        }

        private void Unlock()
        {
            _locked = false;
            _ctx.UnlockSpawners(_species);
            ReleaseChains();
        }

        private void ReleaseChains()
        {
            foreach (IStageFx chain in _chains)
            {
                chain.Release();
            }
            _chains.Clear();
        }
    }
}
