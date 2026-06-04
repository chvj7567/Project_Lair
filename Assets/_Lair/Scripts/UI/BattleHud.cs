using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using UnityEngine;

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
        //# 상태 아이콘 — ECardId→Sprite 해석 dict. BattleController 가 카드 풀 1회 스캔으로 채워 주입.
        public IReadOnlyDictionary<ECardId, Sprite> CardIcons;
    }

    //# CHMUI 로 띄워지는 HUD. UIArg 통해 ViewModel 주입받아 구독.
    //# 구독 해제는 UIBase.closeDisposable 활용 (Close 시 자동 정리).
    public class BattleHud : UIBase
    {
        [SerializeField] private CHText _timerText;
        //# 영웅 HP 바 — Fill/텍스트 내부 위젯은 HpBarView 가 캡슐화. HUD 는 SetHp 만 호출.
        [SerializeField] private HpBarView _heroHpBar;
        [SerializeField] private BuildPanel _buildPanel;
        //# 스포너 상태 UI — 화면 하단 6셀 패널 (기획서 §2.1).
        [SerializeField] private SpawnerStatusPanel _spawnerStatusPanel;

        //# 카드 리뉴얼 v0.6 — 좌측 빌드 시너지 패널 (롤토체스 스타일).
        //# 사용자가 BattleHud prefab 안에 BuildSynergyPanel.prefab 자식으로 배치 + 인스펙터에서 본 필드에 드래그.
        [SerializeField] private BuildSynergyPanel _synergyPanel;

        private BattleViewModel _vm;
        //# 상태 아이콘 — ECardId→Sprite 해석 dict (BattleHudArg 로 주입).
        private IReadOnlyDictionary<ECardId, Sprite> _cardIcons;

        public override void InitUI(UIArg arg)
        {
            if (arg is BattleHudArg ba && ba.ViewModel != null)
                Bind(ba);
        }

        private void Bind(BattleHudArg ba)
        {
            BattleViewModel vm = ba.ViewModel;
            _vm = vm;
            _cardIcons = ba.CardIcons;
            vm.OnTimerChanged        += HandleTimer;
            vm.OnHeroHpValuesChanged += HandleHpValues;
            vm.OnBattleEnded         += HandleEnded;
            vm.OnStatusIconAdded     += HandleStatusIconAdded;
            vm.OnStatusIconRemoved   += HandleStatusIconRemoved;

            //# Close 시 자동 해제
            closeDisposable.Add(() => vm.OnTimerChanged        -= HandleTimer);
            closeDisposable.Add(() => vm.OnHeroHpValuesChanged -= HandleHpValues);
            closeDisposable.Add(() => vm.OnBattleEnded         -= HandleEnded);
            closeDisposable.Add(() => vm.OnStatusIconAdded     -= HandleStatusIconAdded);
            closeDisposable.Add(() => vm.OnStatusIconRemoved   -= HandleStatusIconRemoved);

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
                _spawnerStatusPanel.Bind(vm, ba.Spawners);
                closeDisposable.Add(() => _spawnerStatusPanel.Unbind());
            }

            //# 초기 동기화
            HandleTimer(vm.ElapsedSeconds, vm.TotalSeconds);
            HandleHpValues(vm.HeroHp, vm.HeroMaxHp);
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

        private void HandleHpValues(int current, int max)
        {
            if (_heroHpBar != null) _heroHpBar.SetHp(current, max);
        }

        //# 상태 아이콘 — ECardId→Sprite 해석 후 영웅 HP바 아이콘 행에 추가.
        //# dict 매핑 누락 시 icon null → HpBarView 가 슬롯 미표시(graceful).
        private void HandleStatusIconAdded(object key, ECardId iconId)
        {
            if (_heroHpBar == null) return;
            Sprite icon = null;
            _cardIcons?.TryGetValue(iconId, out icon);
            _heroHpBar.AddStatusIcon(key, icon);
        }

        private void HandleStatusIconRemoved(object key)
        {
            if (_heroHpBar != null) _heroHpBar.RemoveStatusIcon(key);
        }

        private void HandleEnded(BattleResult result)
        {
            //# HUD 는 자기 표시만 — ResultPopup 은 BattleController 가 직접 띄움
        }
    }
}
