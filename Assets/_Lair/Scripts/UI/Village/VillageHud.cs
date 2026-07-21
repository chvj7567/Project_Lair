using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class VillageHudArg : UIArg
    {
        public VillageViewModel Vm;
        public Action<EUI> OnOpenMenu;
        public Action OnSortie;
    }

    //# 마을 상단바(소울/이름/영주 Lv+게이지) + 좌우 메뉴 6종 + 하단 출격. 표시·입력만 (Rule 02 §6).
    public class VillageHud : UIBase
    {
        [SerializeField] private CHText _displayNameText; //# 상단 — 플레이어 닉네임(표시명)
        [SerializeField] private CHText _soulText;        //# 좌 — "1,234 소울"
        [SerializeField] private CHText _lordLevelText;   //# 우 — "영주 Lv 3"
        [SerializeField] private Image _lordXpFill;       //# 우 — XP 게이지 fill
        [SerializeField] private CHButton _shopButton;
        [SerializeField] private CHButton _codexButton;
        [SerializeField] private CHButton _recordsButton;
        [SerializeField] private CHButton _heroButton;
        [SerializeField] private CHButton _questButton;
        [SerializeField] private CHButton _lordButton;
        [SerializeField] private CHButton _rankingButton;       //# v0.3 — 랭킹
        [SerializeField] private CHButton _cloudButton;         //# v0.3 — 계정(클라우드 세이브)
        [SerializeField] private GameObject _cloudConflictDot;  //# v0.3 — 클라우드 충돌 배지(기획서 §3)
        [SerializeField] private CHButton _sortieButton;

        //# hero-stage-variant v2 캐러셀 (기획서 §4.2·§5) — 중앙 쇼케이스 영웅을 감싸는 스테이지 선택 위젯.
        [SerializeField] private CHButton _stagePrevButton;     //# ◀ 이전 스테이지
        [SerializeField] private CHButton _stageNextButton;     //# ▶ 다음 스테이지
        [SerializeField] private CHText _stageIndicatorText;    //# "STAGE {N}"
        [SerializeField] private CHText _stageThreatText;       //# 위협도 ★×N + ☆×(5-N)
        [SerializeField] private GameObject _stageLockOverlay;  //# 잠금 오버레이 그룹 루트(켜고/끔)
        [SerializeField] private Image _stageLockDim;           //# 검정 반투명(α0.55) — 프리팹 값, 참조만 보유
        [SerializeField] private CHText _stageLockLabel;        //# 중앙 "잠금"
        [SerializeField] private CHText _stageLockHintText;     //# "스테이지 {N-1} 클리어 필요"

        private VillageViewModel _vm;

        //# 마을 베이스 HUD — ESC(뒤로가기)로 닫히지 않는다. 팝업만 닫히게 한다.
        public override bool CanCloseByEsc => false;

        public override void InitUI(UIArg arg)
        {
            VillageHudArg hudArg = arg as VillageHudArg;
            if (hudArg == null)
                return;

            _vm = hudArg.Vm;
            if (_vm != null)
            {
                Action refresh = RefreshTop;
                _vm.OnChanged += refresh;
                VillageViewModel vmRef = _vm;
                closeDisposable.Add(() => vmRef.OnChanged -= refresh);
                closeDisposable.Add(() => _vm = null);
                RefreshTop();
            }

            BindMenuButton(_shopButton,        hudArg, EUI.ShopPopup);
            BindMenuButton(_codexButton,       hudArg, EUI.CodexPopup);
            BindMenuButton(_recordsButton,     hudArg, EUI.RecordsPopup);
            BindMenuButton(_heroButton,        hudArg, EUI.HeroSelectPopup);
            BindMenuButton(_questButton,       hudArg, EUI.QuestPopup);
            BindMenuButton(_lordButton,        hudArg, EUI.LordLevelPopup);
            BindMenuButton(_rankingButton,     hudArg, EUI.RankingPopup);
            BindMenuButton(_cloudButton,       hudArg, EUI.CloudPopup);

            RefreshCloudConflictDot();

            if (_sortieButton != null)
            {
                _sortieButton.OnClick(() => hudArg.OnSortie?.Invoke(), closeDisposable);
            }

            BindStageCarousel();
        }

        //# 캐러셀 — ◀/▶ 는 VM.MoveStage 만 호출(로직 없음, Rule 02 §6). VM.OnStageChanged 구독으로 표시 갱신.
        private void BindStageCarousel()
        {
            if (_stagePrevButton != null)
            {
                _stagePrevButton.OnClick(() => _vm?.MoveStage(-1), closeDisposable);
            }
            if (_stageNextButton != null)
            {
                _stageNextButton.OnClick(() => _vm?.MoveStage(1), closeDisposable);
            }
            if (_vm != null)
            {
                Action stageRefresh = RefreshStage;
                _vm.OnStageChanged += stageRefresh;
                VillageViewModel vmRef = _vm;
                closeDisposable.Add(() => vmRef.OnStageChanged -= stageRefresh);
            }
            RefreshStage();
        }

        //# 인디케이터/위협도/화살표·출격 활성/잠금 오버레이 갱신 — 표시만(상태 판정은 VM). 기획서 §4.2·§4.3·§4.5.
        private void RefreshStage()
        {
            if (_vm == null)
                return;
            int stage = _vm.SelectedStage;
            bool unlocked = _vm.IsCurrentStageUnlocked;

            if (_stageIndicatorText != null)
            {
                _stageIndicatorText.SetText($"STAGE {stage}");
            }
            if (_stageThreatText != null)
            {
                _stageThreatText.SetText(BuildThreat(stage));
            }
            if (_stagePrevButton != null)
            {
                _stagePrevButton.Interactable = _vm.CanMovePrev;
            }
            if (_stageNextButton != null)
            {
                _stageNextButton.Interactable = _vm.CanMoveNext;
            }
            if (_sortieButton != null)
            {
                _sortieButton.Interactable = unlocked;
            }
            if (_stageLockOverlay != null)
            {
                _stageLockOverlay.SetActive(unlocked == false);
            }
            if (_stageLockLabel != null)
            {
                _stageLockLabel.SetText("잠금");
            }
            if (_stageLockHintText != null)
            {
                _stageLockHintText.SetText($"스테이지 {stage - 1} 클리어 필요");
            }
        }

        //# 위협도 ★×N + ☆×(5-N) — 문자 글리프(이모지 아님, 신규 아트 불필요, 기획서 §4.2).
        private static string BuildThreat(int stage)
        {
            int filled = Mathf.Clamp(stage, 0, 5);
            return new string('★', filled) + new string('☆', 5 - filled);
        }

        //# 클라우드 충돌 배지(기획서 §3) — pending 이면 빨간 dot 표시. HUD 표시·재오픈마다 갱신.
        private void RefreshCloudConflictDot()
        {
            if (_cloudConflictDot != null)
                _cloudConflictDot.SetActive(Meta.MetaSession.CloudConflictPending);
        }

        private void BindMenuButton(CHButton button, VillageHudArg arg, EUI ui)
        {
            if (button == null)
                return;
            button.OnClick(() => arg.OnOpenMenu?.Invoke(ui), closeDisposable);
        }

        //# 소울 표기 규칙 — 잔액 "N 소울" (천 단위 콤마), 레벨 "영주 Lv N" (기획서 §7).
        private void RefreshTop()
        {
            if (_vm == null)
                return;
            if (_displayNameText != null)
            {
                _displayNameText.SetText(_vm.DisplayName);
            }
            if (_soulText != null)
            {
                _soulText.SetText($"{_vm.Souls:N0} 소울");
            }
            if (_lordLevelText != null)
            {
                _lordLevelText.SetText($"영주 Lv {_vm.LordLevel}");
            }
            if (_lordXpFill != null)
            {
                _lordXpFill.fillAmount = _vm.LordProgress;
            }
        }
    }
}
