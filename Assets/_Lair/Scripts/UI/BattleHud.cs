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
        //# 시너지·빌드 패널 접힘 상태 VM.
        public HudLayoutViewModel HudLayout;
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
        //# 타이머 평상시 색(txt). 30초 이하면 UiDotPalette.TimerWarn 붉은 글씨로 바뀐다.
        [SerializeField] private Color _timerNormalColor = new Color32(0xEE, 0xF1, 0xF6, 0xFF);
        //# 상단 보스 바 — 내부 위젯은 BossHpBarView 가 캡슐화. HUD 는 의도 API 만 호출(scene-2d-conversion §4.3).
        [SerializeField] private BossHpBarView _bossBar;
        //# 보스 바 오른쪽 "액티브 카드" 카운트다운(§4.4)
        [SerializeField] private ActiveCountdownView _activeCountdown;
        [SerializeField] private BuildPanel _buildPanel;
        //# 스포너 상태 UI — 화면 하단 6셀 패널 (기획서 §2.1).
        [SerializeField] private SpawnerStatusPanel _spawnerStatusPanel;

        //# 카드 리뉴얼 v0.6 — 좌측 빌드 시너지 패널 (롤토체스 스타일).
        //# 사용자가 BattleHud prefab 안에 BuildSynergyPanel.prefab 자식으로 배치 + 인스펙터에서 본 필드에 드래그.
        [SerializeField] private BuildSynergyPanel _synergyPanel;

        private BattleViewModel _vm;
        //# 타이머 경고 색 적용 상태 — -1 미적용 / 0 평상시 / 1 경고. 변할 때만 SetColor.
        private int _timerWarnState = -1;
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
            vm.OnActiveCountdownChanged += HandleCountdown;
            vm.OnPassiveTickAcquired    += HandleTickAcquired;

            //# Close 시 자동 해제
            closeDisposable.Add(() => vm.OnTimerChanged        -= HandleTimer);
            closeDisposable.Add(() => vm.OnHeroHpValuesChanged -= HandleHpValues);
            closeDisposable.Add(() => vm.OnBattleEnded         -= HandleEnded);
            closeDisposable.Add(() => vm.OnStatusIconAdded     -= HandleStatusIconAdded);
            closeDisposable.Add(() => vm.OnStatusIconRemoved   -= HandleStatusIconRemoved);
            closeDisposable.Add(() => vm.OnActiveCountdownChanged -= HandleCountdown);
            closeDisposable.Add(() => vm.OnPassiveTickAcquired    -= HandleTickAcquired);

            //# 빌드 패널 바인딩 (Close 시 자동 해제)
            if (_buildPanel != null)
            {
                _buildPanel.Bind(vm, ba.HudLayout);
                closeDisposable.Add(() => _buildPanel.Unbind());
            }

            //# 카드 리뉴얼 v0.6 — 시너지 패널 바인딩 (Close 시 자동 해제).
            if (_synergyPanel != null)
            {
                _synergyPanel.Bind(vm, ba.HudLayout);
                closeDisposable.Add(() => _synergyPanel.Unbind());
            }

            //# 스포너 상태 패널 바인딩 (Close 시 자동 해제)
            if (_spawnerStatusPanel != null)
            {
                _spawnerStatusPanel.Bind(vm, ba.Spawners);
                closeDisposable.Add(() => _spawnerStatusPanel.Unbind());
            }

            //# 보스 바 초기 동기화 — 제목·눈금 위치·이미 획득한 눈금
            if (_bossBar != null)
            {
                _bossBar.SetTitle(vm.HeroTitle);
                _bossBar.SetTicks(vm.PassiveThresholds);
                for (int i = 0; i < vm.PassiveTriggerTotal; i++)
                {
                    _bossBar.SetTickAcquired(i, vm.IsPassiveTickAcquired(i));
                }
            }
            HandleCountdown(BattleViewModel.ComputeActiveCountdown(vm.ActiveThresholds, vm.ElapsedSeconds));

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

            int warnState = BattleViewModel.IsTimerWarning(elapsed, total) ? 1 : 0;
            if (warnState == _timerWarnState)
                return;
            _timerWarnState = warnState;
            _timerText.SetColor(warnState == 1 ? UiDotPalette.TimerWarn : _timerNormalColor);
        }

        private void HandleHpValues(int current, int max)
        {
            if (_bossBar != null) _bossBar.SetHp(current, max);
        }

        private void HandleCountdown(ActiveCountdown c)
        {
            if (_activeCountdown != null) _activeCountdown.Show(c);
        }

        private void HandleTickAcquired(int index)
        {
            if (_bossBar != null) _bossBar.SetTickAcquired(index, true);
        }

        //# 상태 아이콘 — ECardId→Sprite 해석 후 영웅 HP바 아이콘 행에 추가.
        //# dict 매핑 누락 시 icon null → HpBarView 가 슬롯 미표시(graceful).
        private void HandleStatusIconAdded(object key, ECardId iconId)
        {
            if (_bossBar == null) return;
            Sprite icon = null;
            _cardIcons?.TryGetValue(iconId, out icon);
            _bossBar.AddStatusIcon(key, icon);
        }

        private void HandleStatusIconRemoved(object key)
        {
            if (_bossBar != null) _bossBar.RemoveStatusIcon(key);
        }

        private void HandleEnded(BattleResult result)
        {
            //# HUD 는 자기 표시만 — ResultPopup 은 BattleController 가 직접 띄움
        }
    }
}
