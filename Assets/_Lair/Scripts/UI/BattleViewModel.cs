using System;
using Lair.Data;

namespace Lair.UI
{
    //# Model 가공 + 이벤트 노출. View 를 모름.
    //# BattleResult 는 Lair.Data 의 공용 enum (Rule 09).
    public class BattleViewModel
    {
        private readonly BattleStateModel _model;

        public event Action<float, float> OnTimerChanged;
        public event Action<float> OnHeroHpRatioChanged;
        public event Action<BattleResult> OnBattleEnded;

        public BattleViewModel(BattleStateModel model)
        {
            _model = model;
        }

        public void UpdateTimer(float elapsed)
        {
            _model.ElapsedSeconds = elapsed;
            OnTimerChanged?.Invoke(elapsed, _model.TotalSeconds);
        }

        public void UpdateHeroHp(int current, int max)
        {
            _model.HeroHp = current;
            _model.HeroMaxHp = max;
            OnHeroHpRatioChanged?.Invoke(max > 0 ? (float)current / max : 0f);
        }

        public void EndBattle(BattleResult result)
        {
            _model.Result = result;
            OnBattleEnded?.Invoke(result);
        }

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        public BattleResult Result  => _model.Result;
    }
}
