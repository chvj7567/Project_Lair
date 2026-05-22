using System;
using System.Collections.Generic;
using Lair.Card;
using Lair.Data;

namespace Lair.UI
{
    //# Model 가공 + 이벤트 노출. View 를 모름.
    //# BattleResult 는 Lair.Data 의 공용 enum (Rule 09).
    public class BattleViewModel
    {
        //# 빌드 패널 1개 항목 — 카드 + 패시브 여부 + 중복 픽 횟수.
        public class BuildEntry
        {
            public CardData Card;
            public bool IsPassive;
            public int Count;
        }

        private readonly BattleStateModel _model;
        private readonly List<BuildEntry> _build = new();

        public event Action<float, float> OnTimerChanged;
        public event Action<float> OnHeroHpRatioChanged;
        public event Action<BattleResult> OnBattleEnded;
        public event Action OnBuildChanged;

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

        //# 카드 픽 누적 — 같은 카드면 Count++, 아니면 신규 엔트리. 이후 OnBuildChanged.
        public void AddPick(CardData card, bool isPassive)
        {
            if (card == null) return;
            foreach (var e in _build)
            {
                if (e.Card == card)
                {
                    e.Count++;
                    OnBuildChanged?.Invoke();
                    return;
                }
            }
            _build.Add(new BuildEntry { Card = card, IsPassive = isPassive, Count = 1 });
            OnBuildChanged?.Invoke();
        }

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        public BattleResult Result  => _model.Result;
        public IReadOnlyList<BuildEntry> Build => _build;
    }
}
