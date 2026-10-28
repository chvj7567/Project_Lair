using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class DailyPopupArg : UIArg
    {
        public DailyViewModel ViewModel;
        public Action OnStart;                //# 도전 시작 — 판정·세션 저장·씬 전환은 호출부(VillageController) 소유
        public Func<int, Sprite> PortraitOf;  //# 선형 스테이지 → 영웅 초상. null 이면 초상 숨김
    }

    //# 데일리 팝업 (daily-challenge §9.2) — 조합 카드 + 오늘의 순위(CHPoolingScrollView) + 내 현황 + 시작 버튼. 표시·입력만 한다 (Rule 02 §6).
    public class DailyPopup : UIBase
    {
        private const float CountdownRefreshSec = 1f;

        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _dateText;

        //# 조합 카드 — 오프라인이면 _comboRoot 를 숨기고 안내 문구만.
        [SerializeField] private GameObject _comboRoot;
        [SerializeField] private CHText _comboOfflineText;
        [SerializeField] private Image _portraitBox;
        [SerializeField] private Color[] _chapterColors = new Color[0];
        [SerializeField] private Image _portrait;
        [SerializeField] private CHText _heroNameText;
        [SerializeField] private CHText _heroSubText;
        [SerializeField] private CHText _difficultyText;
        [SerializeField] private CHText _alwaysRuleText;
        [SerializeField] private CHText _phaseRuleText;
        [SerializeField] private Image[] _relicIcons = new Image[0];
        [SerializeField] private CHText[] _relicNameTexts = new CHText[0];
        [SerializeField] private CHText[] _relicStackTexts = new CHText[0];

        //# 순위 — 목록(풀링) + 하단 고정 내 행. 상태 문구는 조회 중/실패/오프라인.
        [SerializeField] private GameObject _rankListRoot;
        [SerializeField] private RankingPoolingScrollView _scrollView;
        [SerializeField] private RankingCell _myRankCell;
        [SerializeField] private CHText _rankStatusText;

        //# 내 현황.
        [SerializeField] private Image[] _attemptPips = new Image[0];
        [SerializeField] private CHText _attemptsText;
        [SerializeField] private CHText _myRecordText;
        [SerializeField] private CHText _streakText;
        [SerializeField] private Image[] _attendanceCells = new Image[0];
        [SerializeField] private GameObject _attendanceTodayFrame;
        [SerializeField] private CHText _firstWinText;
        [SerializeField] private Color _pipOnColor = new Color32(0xF7, 0xC6, 0x4A, 0xFF);
        [SerializeField] private Color _pipOffColor = new Color32(0x23, 0x28, 0x34, 0xFF);
        [SerializeField] private Color _attendOnColor = new Color32(0x5E, 0xF0, 0xB4, 0xFF);
        [SerializeField] private Color _attendOffColor = new Color32(0x10, 0x15, 0x1F, 0xFF);
        [SerializeField] private Color _claimedColor = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        [SerializeField] private Color _rewardColor = new Color32(0xF7, 0xC6, 0x4A, 0xFF);

        [SerializeField] private CHText _guideText;
        [SerializeField] private CHButton _startButton;
        [SerializeField] private Image _startButtonImage;
        [SerializeField] private Sprite _startOnSprite;
        [SerializeField] private Sprite _startOffSprite;
        [SerializeField] private CHText _startText;
        [SerializeField] private CHText _startSubText;

        private DailyViewModel _vm;
        private Action _onStart;
        private Func<int, Sprite> _portraitOf;
        private float _countdownTimer;

        public override void InitUI(UIArg arg)
        {
            DailyPopupArg popupArg = arg as DailyPopupArg;
            if (popupArg == null || popupArg.ViewModel == null)
                return;

            _vm = popupArg.ViewModel;
            _onStart = popupArg.OnStart;
            _portraitOf = popupArg.PortraitOf;
            _vm.OnChanged += HandleChanged;
            closeDisposable.Add(() =>
            {
                if (_vm != null)
                {
                    _vm.OnChanged -= HandleChanged;
                }
                _vm = null;
                _onStart = null;
                _portraitOf = null;
            });

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_startButton != null)
            {
                _startButton.OnClick(() => _onStart?.Invoke(), closeDisposable);
            }

            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        //# CHMUI 는 InitUI 후 SetActive(true) 순서 — 비활성 시점엔 layout 미산정이라 순위 목록 첫 구성은 OnEnable 이 맡는다.
        private void OnEnable()
        {
            if (_vm != null)
            {
                Rebuild();
            }
        }

        //# 초기화까지 남은 시간 — 단조 시계 기준 값을 1초마다 다시 읽는다(VM 계산).
        private void Update()
        {
            if (_vm == null || _vm.IsOnline == false || _dateText == null)
                return;
            _countdownTimer += Time.unscaledDeltaTime;
            if (_countdownTimer < CountdownRefreshSec)
                return;
            _countdownTimer = 0f;
            _dateText.SetText(_vm.DateLineText);
        }

        private void HandleChanged()
        {
            if (_vm == null || isActiveAndEnabled == false)
                return;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_vm == null)
                return;
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            SetText(_dateText, _vm.DateLineText);
            ShowCombo();
            ShowRank();
            ShowStatus();
            SetText(_guideText, _vm.GuideText);
            ShowStart();
        }

        private void ShowCombo()
        {
            bool online = _vm.IsOnline;
            if (_comboRoot != null)
            {
                _comboRoot.SetActive(online);
            }
            if (_comboOfflineText != null)
            {
                bool showOffline = online == false && _vm.IsLoading == false;
                _comboOfflineText.gameObject.SetActive(showOffline);
                _comboOfflineText.SetText(_vm.OfflineText);
            }
            if (online == false)
                return;

            SetText(_heroNameText, _vm.HeroNameText);
            SetText(_heroSubText, _vm.HeroSubText);
            SetText(_difficultyText, _vm.DifficultyText);
            if (_difficultyText != null)
            {
                _difficultyText.SetColor(UiDotPalette.DifficultyColor(_vm.DifficultyIndex));
            }
            SetText(_alwaysRuleText, _vm.AlwaysRuleText);
            SetText(_phaseRuleText, _vm.PhaseRuleText);
            if (_portraitBox != null && _vm.HeroChapterId >= 1 && _vm.HeroChapterId <= _chapterColors.Length)
            {
                _portraitBox.color = _chapterColors[_vm.HeroChapterId - 1];
            }
            if (_portrait != null)
            {
                Sprite sprite = _portraitOf != null ? _portraitOf(_vm.PortraitStage) : null;
                _portrait.sprite = sprite;
                _portrait.enabled = sprite != null;
            }
            ShowRelic(0, _vm.RelicA);
            ShowRelic(1, _vm.RelicB);
        }

        private void ShowRelic(int index, DailyRelicView relic)
        {
            if (relic == null)
                return;
            if (index < _relicNameTexts.Length)
            {
                SetText(_relicNameTexts[index], relic.NameText);
            }
            if (index < _relicStackTexts.Length)
            {
                SetText(_relicStackTexts[index], relic.StackText);
            }
            if (index < _relicIcons.Length && _relicIcons[index] != null)
            {
                LoadRelicIcon(_relicIcons[index], relic.RelicId);
            }
        }

        //# 아이콘 — ERelic 값명 = 스프라이트 파일명(Rule 03 §2). 값명이 enum 에 없으면 아이콘만 비운다.
        private void LoadRelicIcon(Image icon, string relicId)
        {
            icon.enabled = false;
            if (Enum.TryParse(relicId, out ERelic key) == false || Enum.IsDefined(typeof(ERelic), key) == false)
                return;
            CHMResource.Instance.Load<Sprite>(key, sprite =>
            {
                if (this == null || icon == null || sprite == null)
                    return;
                icon.sprite = sprite;
                icon.enabled = true;
            });
        }

        private void ShowRank()
        {
            bool loaded = _vm.RankState == EDailyRankState.Loaded;
            if (_rankListRoot != null)
            {
                _rankListRoot.SetActive(loaded);
            }
            if (_rankStatusText != null)
            {
                bool failed = _vm.RankState == EDailyRankState.Failed && _vm.IsLoading == false;
                _rankStatusText.gameObject.SetActive(failed);
                _rankStatusText.SetText(_vm.IsOnline ? _vm.RankFailedText : _vm.OfflineText);
            }
            if (loaded == false)
                return;
            if (_scrollView != null)
            {
                _scrollView.SetItemList(_vm.TopRows);
            }
            if (_myRankCell != null && _vm.MyRow != null)
            {
                _myRankCell.Bind(_vm.MyRow);
            }
        }

        private void ShowStatus()
        {
            bool[] pips = _vm.AttemptPips;
            for (int i = 0; i < _attemptPips.Length; ++i)
            {
                if (_attemptPips[i] == null)
                    continue;
                _attemptPips[i].gameObject.SetActive(i < pips.Length);
                _attemptPips[i].color = i < pips.Length && pips[i] ? _pipOnColor : _pipOffColor;
            }
            SetText(_attemptsText, _vm.AttemptsText);
            SetText(_myRecordText, _vm.MyRecordText);
            SetText(_streakText, _vm.StreakText);
            bool[] days = _vm.Attendance;
            for (int i = 0; i < _attendanceCells.Length; ++i)
            {
                if (_attendanceCells[i] != null)
                {
                    _attendanceCells[i].color = i < days.Length && days[i] ? _attendOnColor : _attendOffColor;
                }
            }
            if (_attendanceTodayFrame != null)
            {
                _attendanceTodayFrame.SetActive(_vm.IsOnline);
            }
            SetText(_firstWinText, _vm.FirstWinText);
            if (_firstWinText != null)
            {
                _firstWinText.SetColor(_vm.FirstWinClaimed ? _claimedColor : _rewardColor);
            }
        }

        private void ShowStart()
        {
            bool enabled = _vm.StartEnabled;
            if (_startButton != null)
            {
                _startButton.Interactable = enabled;
            }
            if (_startButtonImage != null)
            {
                Sprite sprite = enabled ? _startOnSprite : _startOffSprite;
                if (sprite != null)
                {
                    _startButtonImage.sprite = sprite;
                }
            }
            SetText(_startText, _vm.StartText);
            if (_startSubText != null)
            {
                string sub = _vm.StartSubText;
                _startSubText.gameObject.SetActive(string.IsNullOrEmpty(sub) == false);
                _startSubText.SetText(sub);
            }
        }

        private static void SetText(CHText label, string text)
        {
            if (label != null)
            {
                label.SetText(text);
            }
        }
    }
}
