using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도감 몬스터 탭 패널(monster-codex §2~§6, Rule 03 §3 Panel) — 종족 칩 / 풀링 매트릭스(6종족 공유 1개) / 상세를 묶는다. 로직은 CodexViewModel, 여기는 표시·배선만.
    //# 칸 수·행/열은 VM 의 등급 수 x 티어 수에서 나온다(스크롤뷰 열 수는 프리팹 _columnCount = 티어 수). 칸 이펙트 슬롯은 배분기가 소유(보이는 칸만, 상한 fxLiveCap).
    public class CodexMonsterPanel : MonoBehaviour
    {
        [SerializeField] private CodexMatrixPoolingScrollView _scrollView;
        [SerializeField] private ScrollRect _scrollRect;                 //# _scrollView 와 같은 오브젝트의 ScrollRect — viewport 가시 판정·스크롤 통지
        [SerializeField] private CodexRaceChip[] _chips;
        [SerializeField] private CHText[] _gradeHeaderTexts;             //# 등급 행 머리글(등급 1부터)
        [SerializeField] private CHText[] _tierHeaderTexts;              //# 티어 열 머리글(티어 0부터)
        [SerializeField] private CHText[] _tierLevelTexts;               //# 티어 열 Lv 범위
        [SerializeField] private CHText _progressText;                   //# 진척 패널 "수집 n / 120"
        [SerializeField] private Image _progressFill;                    //# fillAmount = 열린 칸 / 전체
        [SerializeField] private CodexDetailPanel _detail;
        [SerializeField] private CodexRaceArt[] _raceArt;

        private CodexViewModel _vm;
        private CodexConfigRow _config;
        private CodexDatabase _codex;
        private MonsterTierSheetLoader _loader;
        private CodexFxAllocator _allocator;
        private int _version;
        private bool _shown;
        private bool _fxDirty;
        private bool _fxHold;
        private readonly Vector3[] _corners = new Vector3[4];

        //# 탭 진입 — 팝업이 VM·로더를 만들어 넘긴다(로더 unload 는 쇼케이스가 쥔 키를 건드리지 않게 팝업이 주입).
        public void Show(CodexViewModel vm, CodexDatabase codex, MonsterTierSheetLoader loader)
        {
            if (vm == null)
                return;
            Hide();
            _vm = vm;
            _codex = codex;
            _config = codex != null ? codex.Config : null;
            _loader = loader;
            _allocator = new CodexFxAllocator(new GradeFxBudgetSlots(), _config != null ? _config.fxLiveCap : 0);
            _shown = true;
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.AddListener(OnScrolled);
            }
            if (_detail != null)
            {
                _detail.OpenPreview();
            }
            RefreshHeaders();
            RefreshChips();
            ShowRace(false);
        }

        //# 닫힘·탭 전환 — 슬롯 전부 반납, 상세 미리보기·티어 시트 해제. 멱등.
        public void Hide()
        {
            _version++;
            _shown = false;
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrolled);
            }
            _allocator?.ReleaseAll();
            _allocator = null;
            if (_detail != null)
            {
                _detail.ClosePreview();
            }
            _loader?.Release();
            _loader = null;
            _vm = null;
        }

        private void OnDisable()
        {
            _allocator?.ReleaseAll();
        }

        private void OnDestroy()
        {
            Hide();
        }

        private void OnScrolled(Vector2 position)
        {
            _fxDirty = true;
        }

        private void LateUpdate()
        {
            if (_shown == false || _fxDirty == false || _fxHold)
                return;
            _fxDirty = false;
            RebalanceFx();
        }

        private void OnChipSelected(EMonster race)
        {
            if (_shown == false || _vm == null || race == _vm.SelectedRace)
                return;
            _allocator?.ReleaseAll();
            if (_vm.SelectRace(race) == false)
                return;
            RefreshChips();
            ShowRace(true);
        }

        private void OnCellSelected(int index)
        {
            if (_shown == false || _vm == null || _vm.SelectCell(index) == false)
                return;
            ApplySelectionMarks();
            RefreshDetail();
        }

        private void OnTierSelected(int tier)
        {
            CodexCellModel selected = _vm != null ? _vm.SelectedCell : null;
            if (selected == null)
                return;
            OnCellSelected(CodexRules.CellIndex(selected.Grade, tier));
        }

        //# 종족 표시 — 그 종족 T(race) 이하 티어 시트를 로드(이전 종족 것은 해제)한 뒤 매트릭스·상세를 채운다.
        private async void ShowRace(bool releaseLoaded)
        {
            int version = ++_version;
            try
            {
                if (_loader != null)
                {
                    if (releaseLoaded)
                    {
                        _loader.Release();
                    }
                    await _loader.LoadAsync(TierNeeds(_vm.SelectedRace, _vm.OwnedTier(_vm.SelectedRace)));
                }
                if (version != _version || _shown == false)
                    return;
                RebuildMatrix();
                if (_progressText != null)
                {
                    _progressText.SetText(_vm.CollectedText);
                }
                if (_progressFill != null)
                {
                    _progressFill.fillAmount = _vm.TotalCount > 0 ? (float)_vm.TotalOpen / _vm.TotalCount : 0f;
                }
                RefreshDetail();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static List<(EMonster, int)> TierNeeds(EMonster race, int ownedTier)
        {
            List<(EMonster, int)> needs = new List<(EMonster, int)>();
            for (int tier = 1; tier <= ownedTier; ++tier)
            {
                needs.Add((race, tier));
            }
            return needs;
        }

        private void RefreshHeaders()
        {
            SetHeaders(_gradeHeaderTexts, _vm.GradeCount, i => _vm.GradeName(i + MonsterGrades.MinGrade));
            SetHeaders(_tierHeaderTexts, _vm.TierCount, i => _vm.TierHeader(i));
            SetHeaders(_tierLevelTexts, _vm.TierCount, i => _vm.TierLevelRange(i));
        }

        private static void SetHeaders(CHText[] texts, int count, Func<int, string> label)
        {
            if (texts == null)
                return;
            for (int i = 0; i < texts.Length; ++i)
            {
                if (texts[i] == null)
                    continue;
                bool used = i < count;
                texts[i].gameObject.SetActive(used);
                if (used)
                {
                    texts[i].SetText(label(i));
                }
            }
        }

        private void RefreshChips()
        {
            if (_chips == null)
                return;
            List<CodexRaceChipModel> models = _vm.BuildChips();
            for (int i = 0; i < _chips.Length; ++i)
            {
                if (_chips[i] == null)
                    continue;
                bool used = i < models.Count;
                _chips[i].gameObject.SetActive(used);
                if (used)
                {
                    _chips[i].Bind(models[i], models[i].Race == _vm.SelectedRace, OnChipSelected);
                }
            }
        }

        private void RebuildMatrix()
        {
            if (_scrollView == null)
                return;
            EMonster race = _vm.SelectedRace;
            CodexRaceArt art = ArtOf(race);
            Color raceColor = SpeciesVisual.SpeciesGlowColor(race);
            List<CodexMonsterCellData> data = new List<CodexMonsterCellData>();
            int playOrder = 0;
            foreach (CodexCellModel model in _vm.Cells)
            {
                float stagger = 0f;
                if (model.PlayUnlock && _config != null)
                {
                    stagger = playOrder * _config.newCellStaggerSec;
                    playOrder++;
                }
                data.Add(new CodexMonsterCellData
                {
                    Model = model,
                    Body = BodyOf(model, art),
                    Art = art,
                    RaceColor = raceColor,
                    GradeColor = _codex != null ? _codex.ColorOf(model.Grade) : Color.white,
                    Config = _config,
                    Unlock = _codex != null ? _codex.UnlockOf(model.Grade) : null,
                    Selected = model.Index == _vm.SelectedIndex,
                    StaggerDelaySec = stagger,
                    NewLabel = _vm.NewLabel,
                    OnSelect = OnCellSelected,
                });
            }
            _scrollView.SetItemList(data);
            _fxDirty = true;
        }

        private Sprite BodyOf(CodexCellModel model, CodexRaceArt art)
        {
            if (model.Tier > 0 && _loader != null)
            {
                MonsterTierSheetSet set = _loader.Find(model.Race, model.Tier);
                Sprite frame = set != null ? set.GetFrame(0) : null;
                if (frame != null)
                    return frame;
            }
            return art != null ? art.Body : null;
        }

        private CodexRaceArt ArtOf(EMonster race)
        {
            if (_raceArt == null)
                return null;
            foreach (CodexRaceArt art in _raceArt)
            {
                if (art != null && art.Race == race)
                    return art;
            }
            return null;
        }

        private void ApplySelectionMarks()
        {
            if (_scrollView == null)
                return;
            foreach (CodexMonsterCell cell in _scrollView.ItemList)
            {
                if (cell != null && cell.Data != null)
                {
                    cell.SetSelected(cell.Index == _vm.SelectedIndex);
                }
            }
        }

        //# 상세 갱신 — 미리보기를 먼저 확보(슬롯 우선)한 뒤 칸 이펙트 배분을 재개한다.
        private async void RefreshDetail()
        {
            if (_detail == null || _vm == null)
                return;
            CodexDetailModel model = _vm.BuildDetail();
            if (model == null)
                return;
            int version = _version;
            CodexRaceArt art = ArtOf(model.Cell.Race);
            _detail.Bind(model, _codex != null ? _codex.ColorOf(model.Cell.Grade) : Color.white,
                cell => BodyOf(cell, art), _vm.TierHeader, _vm.TierLevelRange, OnTierSelected);
            _fxHold = true;
            try
            {
                await _detail.ShowPreviewAsync(model, _loader);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _fxHold = false;
            }
            if (version == _version && _shown)
            {
                _fxDirty = true;
            }
        }

        //# 보이는 2등급 이상 해금 칸만 후보로 슬롯 재배분 — 스크롤 밖 칸은 점유하지 않는다.
        private void RebalanceFx()
        {
            if (_allocator == null || _scrollView == null || _scrollRect == null || _scrollRect.viewport == null)
                return;
            _scrollRect.viewport.GetWorldCorners(_corners);
            Rect viewport = new Rect(_corners[0].x, _corners[0].y, _corners[2].x - _corners[0].x, _corners[2].y - _corners[0].y);
            List<CodexMonsterCell> cells = _scrollView.ItemList;
            List<CodexFxAllocator.Candidate> candidates = new List<CodexFxAllocator.Candidate>();
            foreach (CodexMonsterCell cell in cells)
            {
                if (cell != null && cell.IsFxCandidate && cell.IsVisibleIn(viewport))
                {
                    candidates.Add(new CodexFxAllocator.Candidate(cell.Index, cell.Grade, cell.Tier));
                }
            }
            HashSet<int> live = _allocator.Rebalance(candidates);
            foreach (CodexMonsterCell cell in cells)
            {
                if (cell != null)
                {
                    cell.SetFxLive(live.Contains(cell.Index));
                }
            }
        }
    }
}
