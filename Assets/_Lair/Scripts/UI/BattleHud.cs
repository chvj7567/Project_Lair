using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class BattleHudArg : UIArg
    {
        public BattleViewModel ViewModel;
    }

    //# CHMUI 로 띄워지는 HUD. UIArg 통해 ViewModel 주입받아 구독.
    //# 구독 해제는 UIBase.closeDisposable 활용 (Close 시 자동 정리).
    public class BattleHud : UIBase
    {
        [SerializeField] private CHText _timerText;
        [SerializeField] private Image _heroHpFill;

        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            if (arg is BattleHudArg ba && ba.ViewModel != null)
                Bind(ba.ViewModel);
        }

        private void Bind(BattleViewModel vm)
        {
            _vm = vm;
            vm.OnTimerChanged       += HandleTimer;
            vm.OnHeroHpRatioChanged += HandleHp;
            vm.OnBattleEnded        += HandleEnded;

            //# Close 시 자동 해제
            closeDisposable.Add(() => vm.OnTimerChanged       -= HandleTimer);
            closeDisposable.Add(() => vm.OnHeroHpRatioChanged -= HandleHp);
            closeDisposable.Add(() => vm.OnBattleEnded        -= HandleEnded);

            //# 초기 동기화
            HandleTimer(vm.ElapsedSeconds, vm.TotalSeconds);
            HandleHp(vm.HeroHpRatio);
        }

        private void HandleTimer(float elapsed, float total)
        {
            if (_timerText == null) return;
            float remain = Mathf.Max(0f, total - elapsed);
            _timerText.SetText($"{(int)(remain / 60)}:{(int)(remain % 60):00}");
        }

        private void HandleHp(float ratio)
        {
            if (_heroHpFill == null) return;
            _heroHpFill.fillAmount = ratio;
        }

        private void HandleEnded(BattleResult result)
        {
            //# HUD 는 자기 표시만 — ResultPopup 은 BattleController 가 직접 띄움
        }
    }
}
