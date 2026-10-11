using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class WorldMapPopupArg : UIArg
    {
        public WorldMapViewModel ViewModel;
        //# 선형 스테이지 번호 → 영웅 초상. null 이면 초상을 숨긴다(HeroStageVariantConfig 소유).
        public Func<int, Sprite> PortraitOf;
    }

    //# 침입 지도 — 챕터 탭 5 + 스테이지 노드 5(프리팹 정적 배치) + 상세 패널 + 선택 버튼. 표시·입력만 (Rule 02 §6), 모든 판정은 WorldMapViewModel.
    public class WorldMapPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private WorldMapChapterTabView[] _tabs = new WorldMapChapterTabView[0];
        [SerializeField] private WorldMapNodeView[] _nodes = new WorldMapNodeView[0];

        //# 지도 영역 헤더
        [SerializeField] private CHText _chapterTitleText;
        [SerializeField] private CHText _chapterThemeText;

        //# 상세 패널
        [SerializeField] private Image _detailPortrait;
        [SerializeField] private GameObject _portraitLockOverlay;
        [SerializeField] private CHText _detailNumberText;
        [SerializeField] private GameObject _detailBossBadge;
        [SerializeField] private CHText _detailNameText;
        [SerializeField] private CHText _detailStarsText;
        [SerializeField] private CHText _detailHeroText;
        [SerializeField] private CHText _detailRecordText;
        [SerializeField] private CHText _detailRulesText;
        [SerializeField] private GameObject _bossPhaseRoot;     //# "보스 페이즈" 라벨 + 목록 묶음 — 보스 페이즈 없으면 숨김
        [SerializeField] private CHText _bossPhaseText;

        //# 챕터 색조·연결선
        [SerializeField] private Image _mapBackground;
        [SerializeField] private Image[] _pathSolid = new Image[0];     //# 구간 k = 노드 k+1 → k+2 (클리어 실선)
        [SerializeField] private Image[] _pathDashed = new Image[0];    //# 같은 구간의 미개척 점선
        [SerializeField] private Image _bannerTint;
        [SerializeField] private Image _mapSilhouette;                  //# 챕터별 하단 실루엣
        [SerializeField] private Sprite[] _silhouetteSprites = new Sprite[0];   //# 인덱스 = 챕터 id − 1
        [SerializeField] private Image _portraitGlow;                   //# 상세 초상 뒤 발광
        [SerializeField] private GameObject _selectGlow;                //# 선택 버튼 글로우 루트(버튼 뒤)
        [SerializeField] private GameObject _selectGloss;               //# 선택 버튼 광택 루트(버튼 위 — 그리기 순서상 글로우와 분리)

        //# 하단
        [SerializeField] private CHText _bottomText;
        [SerializeField] private CHButton _selectButton;

        private const float PortraitGlowAlpha = 0.60f;

        private WorldMapViewModel _vm;
        private Func<int, Sprite> _portraitOf;

        public override void InitUI(UIArg arg)
        {
            WorldMapPopupArg popupArg = arg as WorldMapPopupArg;
            if (popupArg == null || popupArg.ViewModel == null)
                return;

            _vm = popupArg.ViewModel;
            _portraitOf = popupArg.PortraitOf;

            WorldMapViewModel vmRef = _vm;
            Action refresh = Refresh;
            Action<string> notice = HandleNotice;
            Action confirmed = HandleConfirmed;
            vmRef.OnChanged += refresh;
            vmRef.OnNotice += notice;
            vmRef.OnConfirmed += confirmed;
            closeDisposable.Add(() =>
            {
                vmRef.OnChanged -= refresh;
                vmRef.OnNotice -= notice;
                vmRef.OnConfirmed -= confirmed;
                _vm = null;
            });

            BindInputs();
            Refresh();
        }

        //# 입력은 VM 호출만 — 판정 로직 없음.
        private void BindInputs()
        {
            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_selectButton != null)
            {
                _selectButton.OnClick(() => _vm?.Confirm(), closeDisposable);
            }
            for (int i = 0; i < _tabs.Length; ++i)
            {
                if (_tabs[i] == null)
                    continue;
                int chapterId = i + 1;
                _tabs[i].SetClickHandler(() => _vm?.SelectChapter(chapterId), closeDisposable);
            }
            for (int i = 0; i < _nodes.Length; ++i)
            {
                if (_nodes[i] == null)
                    continue;
                int stageIndex = i + 1;
                _nodes[i].SetClickHandler(() => _vm?.SelectNode(stageIndex), closeDisposable);
            }
        }

        private void HandleNotice(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            ToastView.Show(message, EToastKind.Warning);
        }

        private void HandleConfirmed()
        {
            Close(reuse: true);
        }

        //# VM 상태 → 위젯 전체 갱신.
        private void Refresh()
        {
            if (_vm == null)
                return;

            for (int i = 0; i < _tabs.Length && i < _vm.Tabs.Count; ++i)
            {
                if (_tabs[i] != null)
                {
                    _tabs[i].Show(_vm.Tabs[i]);
                }
            }
            for (int i = 0; i < _nodes.Length && i < _vm.Nodes.Count; ++i)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].SetChapter(_vm.SelectedChapter);
                    _nodes[i].Show(_vm.Nodes[i]);
                }
            }

            RefreshChapterTone();
            SetText(_chapterTitleText, _vm.ChapterTitleText);
            SetText(_chapterThemeText, _vm.ChapterThemeText);
            RefreshDetail();

            SetText(_bottomText, _vm.BottomText);
            if (_selectButton != null)
            {
                _selectButton.SetText(_vm.SelectButtonText);
                _selectButton.Interactable = _vm.SelectEnabled;
            }
            if (_selectGlow != null)
            {
                _selectGlow.SetActive(_vm.SelectEnabled);
            }
            if (_selectGloss != null)
            {
                _selectGloss.SetActive(_vm.SelectEnabled);
            }
        }

        //# 선택 챕터의 지도 배경·배너 색조와 구간 연결선(실선/점선) 갱신.
        private void RefreshChapterTone()
        {
            if (_mapBackground != null)
            {
                _mapBackground.color = WorldMapChapterPalette.MapBackground(_vm.SelectedChapter);
            }
            if (_bannerTint != null)
            {
                _bannerTint.color = WorldMapChapterPalette.BannerTint(_vm.SelectedChapter);
            }
            if (_mapSilhouette != null)
            {
                int silhouetteIndex = _vm.SelectedChapter - 1;
                bool hasSilhouette = silhouetteIndex >= 0 && silhouetteIndex < _silhouetteSprites.Length;
                _mapSilhouette.gameObject.SetActive(hasSilhouette);
                if (hasSilhouette)
                {
                    _mapSilhouette.sprite = _silhouetteSprites[silhouetteIndex];
                    _mapSilhouette.color = WorldMapChapterPalette.Silhouette(_vm.SelectedChapter);
                }
            }
            for (int k = 0; k < _vm.Nodes.Count; ++k)
            {
                bool cleared = _vm.Nodes[k].NextPathCleared;
                if (k < _pathSolid.Length && _pathSolid[k] != null)
                {
                    _pathSolid[k].gameObject.SetActive(cleared);
                }
                if (k < _pathDashed.Length && _pathDashed[k] != null)
                {
                    _pathDashed[k].gameObject.SetActive(cleared == false);
                }
            }
        }

        private void RefreshDetail()
        {
            SetText(_detailNumberText, _vm.DetailNumberText);
            SetText(_detailNameText, _vm.DetailNameText);
            SetText(_detailStarsText, _vm.DetailStarsText);
            SetText(_detailHeroText, _vm.DetailHeroText);
            SetText(_detailRecordText, _vm.DetailRecordText);
            SetText(_detailRulesText, _vm.DetailRulesText);
            SetText(_bossPhaseText, _vm.DetailBossPhaseText);

            if (_detailBossBadge != null)
            {
                _detailBossBadge.SetActive(_vm.DetailIsBoss);
            }
            if (_bossPhaseRoot != null)
            {
                _bossPhaseRoot.SetActive(_vm.HasBossPhases);
            }
            if (_portraitLockOverlay != null)
            {
                _portraitLockOverlay.SetActive(_vm.DetailLocked);
            }
            if (_detailPortrait != null)
            {
                Sprite portrait = _portraitOf != null ? _portraitOf(_vm.SelectedLinear) : null;
                _detailPortrait.sprite = portrait;
                _detailPortrait.enabled = portrait != null;
            }
            if (_portraitGlow != null)
            {
                Color glow = WorldMapChapterPalette.Accent(_vm.SelectedChapter);
                glow.a = PortraitGlowAlpha;
                _portraitGlow.color = glow;
                _portraitGlow.gameObject.SetActive(_detailPortrait != null && _detailPortrait.enabled);
            }
        }

        private static void SetText(CHText target, string value)
        {
            if (target != null)
            {
                target.SetText(value ?? string.Empty);
            }
        }
    }
}
