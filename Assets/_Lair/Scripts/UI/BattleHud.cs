using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class BattleHudArg : UIArg
    {
        public BattleViewModel ViewModel;
        //# 스포너 상태 UI — 진행 바 폴링용 ISpawnerProgress 6개.
        public IReadOnlyList<Spawner> Spawners;
        //# 스포너 상태 UI — 툴팁이 base 스탯을 읽기 위한 단일 진실.
        public BalanceConfig Balance;
    }

    //# CHMUI 로 띄워지는 HUD. UIArg 통해 ViewModel 주입받아 구독.
    //# 구독 해제는 UIBase.closeDisposable 활용 (Close 시 자동 정리).
    public class BattleHud : UIBase
    {
        [SerializeField] private CHText _timerText;
        [SerializeField] private Image _heroHpFill;
        [SerializeField] private BuildPanel _buildPanel;
        //# 스포너 상태 UI — 화면 하단 6셀 패널 (기획서 §2.1).
        [SerializeField] private SpawnerStatusPanel _spawnerStatusPanel;

        //# 카드 리뉴얼 v0.6 — 좌측 빌드 시너지 패널 (롤토체스 스타일).
        //# Awake 에서 동적 생성. 인스펙터 참조 없이 BattleHud 자식으로 자동 추가.
        private BuildSynergyPanel _synergyPanel;

        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            EnsureSynergyPanel();
            if (arg is BattleHudArg ba && ba.ViewModel != null)
                Bind(ba);
        }

        //# 카드 리뉴얼 v0.6 — 좌측 시너지 패널 동적 생성. 사용자 환경 액션 0건.
        //# HUD 좌측 상단 240×220 px 영역 (앵커 좌상단).
        private void EnsureSynergyPanel()
        {
            if (_synergyPanel != null) return;
            GameObject panelGo = new GameObject("BuildSynergyPanel");
            panelGo.transform.SetParent(transform, false);
            RectTransform rt = panelGo.AddComponent<RectTransform>();
            //# 좌상단 앵커 — pivot (0, 1) 기준.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(240f, 200f);
            _synergyPanel = panelGo.AddComponent<BuildSynergyPanel>();
        }

        private void Bind(BattleHudArg ba)
        {
            BattleViewModel vm = ba.ViewModel;
            _vm = vm;
            vm.OnTimerChanged       += HandleTimer;
            vm.OnHeroHpRatioChanged += HandleHp;
            vm.OnBattleEnded        += HandleEnded;

            //# Close 시 자동 해제
            closeDisposable.Add(() => vm.OnTimerChanged       -= HandleTimer);
            closeDisposable.Add(() => vm.OnHeroHpRatioChanged -= HandleHp);
            closeDisposable.Add(() => vm.OnBattleEnded        -= HandleEnded);

            //# 빌드 패널 바인딩 (Close 시 자동 해제)
            if (_buildPanel != null)
            {
                _buildPanel.Bind(vm);
                closeDisposable.Add(() => _buildPanel.Unbind());
            }

            //# 카드 리뉴얼 v0.6 — 시너지 패널 바인딩 (Close 시 자동 해제).
            if (_synergyPanel != null)
            {
                _synergyPanel.Bind(vm);
                closeDisposable.Add(() => _synergyPanel.Unbind());
            }

            //# 스포너 상태 패널 바인딩 (Close 시 자동 해제)
            if (_spawnerStatusPanel != null)
            {
                _spawnerStatusPanel.Bind(vm, ba.Spawners, ba.Balance);
                closeDisposable.Add(() => _spawnerStatusPanel.Unbind());
            }

            //# 초기 동기화
            HandleTimer(vm.ElapsedSeconds, vm.TotalSeconds);
            HandleHp(vm.HeroHpRatio);
        }

        private void HandleTimer(float elapsed, float total)
        {
            if (_timerText == null) return;
            //# ceil 표시 — elapsed=30.001 처럼 직후 시점에도 잔량 270 으로 올림 → "4:30" 유지.
            //# 액티브 트리거 (elapsed=30, 60, ...) 가 발동하는 순간 HUD 가 정확히 4:30, 4:00 표시.
            float remain = Mathf.Max(0f, total - elapsed);
            int totalSec = Mathf.CeilToInt(remain);
            _timerText.SetText($"{totalSec / 60}:{totalSec % 60:00}");
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
