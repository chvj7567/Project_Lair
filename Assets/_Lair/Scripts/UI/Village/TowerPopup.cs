using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class TowerPopupArg : UIArg
    {
        public TowerViewModel ViewModel;
        public Action OnChallenge;   //# 도전 층으로 출격 — 모드 설정·씬 전환은 호출부(VillageController) 소유
    }

    //# 무한의 탑 팝업 (infinite-tower §6.2) — 층 사다리(CHPoolingScrollView 3-class) + 층 상세 + 도전 버튼. 표시·입력만 한다 (Rule 02 §6).
    //# 랭킹 패널은 슬라이스 B 자리 — 지금은 "불러오지 못했습니다" 고정 문구만 프리팹에 정적으로 둔다.
    public class TowerPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _subtitleText;
        [SerializeField] private TowerFloorPoolingScrollView _scrollView;
        [SerializeField] private CHButton _challengeButton;
        [SerializeField] private CHText _challengeText;

        //# 층 상세 — 초상 박스는 영웅 챕터(1~) 색으로 칠한다.
        [SerializeField] private Image _portraitBox;
        [SerializeField] private Color[] _chapterColors = new Color[0];
        [SerializeField] private CHText _floorText;
        [SerializeField] private GameObject _bossTag;
        [SerializeField] private CHText _heroText;
        [SerializeField] private CHText _statsText;
        [SerializeField] private CHText[] _ruleTexts = new CHText[0];          //# 규칙 박스 1·2 본문
        [SerializeField] private GameObject[] _ruleBoxes = new GameObject[0];  //# 규칙 박스 루트 (규칙 수만큼 표시)
        [SerializeField] private CHText _noRulesText;
        [SerializeField] private CHText _sourceText;
        [SerializeField] private CHText _rewardText;
        [SerializeField] private CHText _recordText;

        private TowerViewModel _vm;
        private Action _onChallenge;
        private List<TowerFloorCellData> _cells = new List<TowerFloorCellData>();

        public override void InitUI(UIArg arg)
        {
            TowerPopupArg popupArg = arg as TowerPopupArg;
            if (popupArg == null || popupArg.ViewModel == null)
                return;

            _vm = popupArg.ViewModel;
            _onChallenge = popupArg.OnChallenge;
            _vm.OnChanged += HandleChanged;
            closeDisposable.Add(() =>
            {
                if (_vm != null)
                {
                    _vm.OnChanged -= HandleChanged;
                }
                _vm = null;
                _onChallenge = null;
            });

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_challengeButton != null)
            {
                _challengeButton.OnClick(HandleChallenge, closeDisposable);
            }

            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        //# CHMUI 는 InitUI 후 SetActive(true) 순서 — 비활성 시점엔 layout 이 미산정이라 사다리 첫 구성은 OnEnable 이 맡는다.
        private void OnEnable()
        {
            if (_vm != null)
            {
                Rebuild();
            }
        }

        private void HandleChallenge()
        {
            _onChallenge?.Invoke();
        }

        //# 선택 변경 — 사다리는 표식만 갱신(스크롤 유지), 상세만 다시 그린다.
        private void HandleChanged()
        {
            if (_vm == null)
                return;
            _vm.ApplySelectionMarks(_cells);
            if (_scrollView != null)
            {
                _scrollView.RefreshVisible();
            }
            ShowDetail();
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

            if (_subtitleText != null)
            {
                _subtitleText.SetText(_vm.SubtitleText);
            }
            if (_challengeText != null)
            {
                _challengeText.SetText(_vm.ChallengeButtonText);
            }
            //# 도전 층(위에서 5번째 칸)은 기본 스크롤 위치(맨 위)에서 이미 보인다 — 별도 스크롤 이동 없음.
            _cells = _vm.BuildLadder();
            if (_scrollView != null)
            {
                _scrollView.SetItemList(_cells);
            }
            ShowDetail();
        }

        private void ShowDetail()
        {
            TowerFloorDetail detail = _vm.BuildDetail();
            SetText(_floorText, detail.FloorText);
            SetText(_heroText, detail.HeroText);
            SetText(_sourceText, detail.SourceText);
            SetText(_recordText, detail.RecordText);
            if (_bossTag != null)
            {
                _bossTag.SetActive(detail.IsBoss);
            }
            if (_portraitBox != null && detail.HeroChapterId >= 1 && detail.HeroChapterId <= _chapterColors.Length)
            {
                _portraitBox.color = _chapterColors[detail.HeroChapterId - 1];
            }
            if (_statsText != null)
            {
                _statsText.gameObject.SetActive(detail.ShowStats);
                if (detail.ShowStats)
                {
                    _statsText.SetText(detail.StatsText);
                }
            }
            for (int i = 0; i < _ruleBoxes.Length; ++i)
            {
                bool shown = i < detail.RuleTexts.Length;
                if (_ruleBoxes[i] != null)
                {
                    _ruleBoxes[i].SetActive(shown);
                }
                if (shown && i < _ruleTexts.Length)
                {
                    SetText(_ruleTexts[i], detail.RuleTexts[i]);
                }
            }
            if (_noRulesText != null)
            {
                _noRulesText.gameObject.SetActive(detail.ShowNoRules);
                if (detail.ShowNoRules)
                {
                    _noRulesText.SetText(detail.NoRulesText);
                }
            }
            if (_rewardText != null)
            {
                _rewardText.gameObject.SetActive(detail.ShowReward);
                if (detail.ShowReward)
                {
                    _rewardText.SetText(detail.RewardText);
                }
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
