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
    //# 목록 = 등급 수 x (티어 수 + 1) 항목(행마다 머리글 1 + 티어 칸), 열 수 = 티어 수 + 1 (프리팹 _columnCount). 머리글은 행과 함께 스크롤된다.
    //# 칸 이펙트 슬롯은 배분기가 소유(보이는 칸만, 상한 fxLiveCap).
    public class CodexMonsterPanel : MonoBehaviour
    {
        [SerializeField] private CodexMatrixPoolingScrollView _scrollView;
        [SerializeField] private ScrollRect _scrollRect;                 //# _scrollView 와 같은 오브젝트의 ScrollRect — viewport 가시 판정·스크롤 통지
        [SerializeField] private CodexRaceChip[] _chips;
        [SerializeField] private CHText[] _tierHeaderTexts;              //# 티어 열 머리글(티어 0부터) — 스크롤 밖 고정, 열 1~4 위에 정렬
        [SerializeField] private CHText[] _tierLevelTexts;               //# 티어 열 Lv 범위
        [SerializeField] private CHText _progressText;                   //# 진척 패널 "수집 n / 120"
        [SerializeField] private Image _progressFill;                    //# fillAmount = 열린 칸 / 전체
        [SerializeField] private CodexDetailPanel _detail;
        [SerializeField] private CodexRaceArt[] _raceArt;

        private CodexViewModel _vm;
        private CodexConfigRow _config;
        private CodexDatabase _codex;
        private MonsterTierSheetLoader _loader;            //# 현재 표시 중인 종족의 시트
        private MonsterTierSheetLoader _pendingLoader;     //# 종족 전환 중 로드 중인 새 시트
        private Func<MonsterTierSheetLoader> _loaderFactory;
        private CodexFxAllocator _allocator;
        private int _version;
        private bool _shown;
        private bool _fxDirty;
        private bool _fxHold;
        private bool _previewBusy;
        private CodexDetailModel _pendingPreview;     //# 연타 코얼레싱 - 마지막 요청만 처리
        private const int PreviewTimeoutMs = 8000;
        private readonly Vector3[] _corners = new Vector3[4];

        //# 탭 진입 — 팝업이 VM·로더 공장을 넘긴다(로더 unload 는 쇼케이스가 쥔 키를 건드리지 않게 팝업이 주입).
        public void Show(CodexViewModel vm, CodexDatabase codex, Func<MonsterTierSheetLoader> loaderFactory)
        {
            if (vm == null)
                return;
            Hide();
            _vm = vm;
            _codex = codex;
            _config = codex != null ? codex.Config : null;
            _loaderFactory = loaderFactory;
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
            ShowRace();
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
            //# 진행 중인 로드(Release 세대 값으로 늦게 오는 결과도 해제)와 표시 중인 시트를 전부 해제.
            _pendingPreview = null;
            _pendingLoader?.Release();
            _pendingLoader = null;
            _loader?.Release();
            _loader = null;
            _loaderFactory = null;
            _vm = null;
        }

        private void OnEnable()
        {
            _fxDirty = true;
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
            StopAllFx();
            if (_vm.SelectRace(race) == false)
                return;
            RefreshChips();
            ShowRace();
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

        //# 종족 표시 — 새 시트를 먼저 끝까지 로드하고, 목록 교체 직전에 옛 시트를 해제한다(대기 중 옛 칸은 이펙트 정지).
        private async void ShowRace()
        {
            int version = ++_version;
            try
            {
                MonsterTierSheetLoader next = _loaderFactory != null ? _loaderFactory() : null;
                _pendingLoader = next;
                if (next != null)
                {
                    await next.LoadAsync(TierNeeds(_vm.SelectedRace, _vm.OwnedTier(_vm.SelectedRace)));
                }
                if (version != _version || _shown == false)
                {
                    next?.Release();
                    return;
                }
                MonsterTierSheetLoader old = _loader;
                _loader = next;
                _pendingLoader = null;
                List<CodexMonsterCellData> items = BuildItems();
                old?.Release();
                if (_scrollView != null)
                {
                    _scrollView.SetItemList(items);
                }
                _fxDirty = true;
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
                Debug.LogError($"[CodexPanel] 종족 표시 실패 - {e}");
            }
        }

        //# 종족 전환 대기 구간 — 슬롯 반납 + 보이는 옛 칸 정지 프레임.
        private void StopAllFx()
        {
            _allocator?.ReleaseAll();
            if (_scrollView == null)
                return;
            foreach (CodexMonsterCell cell in _scrollView.ItemList)
            {
                if (cell != null && cell.IsRowHeader == false)
                {
                    cell.SetFxLive(false);
                }
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

        //# 행마다 [등급 머리글, 티어0..N-1 칸] — 항목 수 = 등급 수 x (티어 수 + 1).
        private List<CodexMonsterCellData> BuildItems()
        {
            List<CodexMonsterCellData> items = new List<CodexMonsterCellData>();
            EMonster race = _vm.SelectedRace;
            CodexRaceArt art = ArtOf(race);
            Color raceColor = SpeciesVisual.SpeciesGlowColor(race);
            int playOrder = 0;
            for (int grade = MonsterGrades.MinGrade; grade < MonsterGrades.MinGrade + _vm.GradeCount; ++grade)
            {
                Color gradeColor = _codex != null ? _codex.ColorOf(grade) : Color.white;
                items.Add(new CodexMonsterCellData
                {
                    IsRowHeader = true,
                    HeaderName = _vm.GradeName(grade),
                    HeaderGrade = grade,
                    GradeColor = gradeColor,
                });
                for (int tier = 0; tier < _vm.TierCount; ++tier)
                {
                    CodexCellModel model = _vm.Cells[CodexRules.CellIndex(grade, tier)];
                    float stagger = 0f;
                    if (model.PlayUnlock && _config != null)
                    {
                        stagger = playOrder * _config.newCellStaggerSec;
                        playOrder++;
                    }
                    items.Add(new CodexMonsterCellData
                    {
                        Model = model,
                        Body = BodyOf(model, art),
                        Art = art,
                        RaceColor = raceColor,
                        GradeColor = gradeColor,
                        Config = _config,
                        Unlock = _codex != null ? _codex.UnlockOf(grade) : null,
                        Selected = model.Index == _vm.SelectedIndex,
                        StaggerDelaySec = stagger,
                        NewLabel = _vm.NewLabel,
                        OnSelect = OnCellSelected,
                        ConsumeUnlock = _vm.ConsumeUnlock,
                    });
                }
            }
            return items;
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

        //# 선택 표시 — 데이터 전체(스크롤 밖 포함)의 Selected 를 갱신하고 화면에 있는 풀 셀만 다시 그린다.
        private void ApplySelectionMarks()
        {
            if (_scrollView == null)
                return;
            foreach (CodexMonsterCellData data in _scrollView.DataList)
            {
                data.Selected = data.IsRowHeader == false && data.Model != null && data.Model.Index == _vm.SelectedIndex;
            }
            foreach (CodexMonsterCell cell in _scrollView.ItemList)
            {
                if (cell != null)
                {
                    cell.RefreshSelection();
                }
            }
        }

        //# 상세 갱신 - 문구·칸 표시는 즉시, 미리보기(비동기)는 마지막 요청만 하나씩 처리한다(연타 시 풀 Pop/Push·로드가 겹치지 않게).
        private void RefreshDetail()
        {
            if (_detail == null || _vm == null)
                return;
            try
            {
                CodexDetailModel model = _vm.BuildDetail();
                if (model == null)
                    return;
                CodexRaceArt art = ArtOf(model.Cell.Race);
                _detail.Bind(model, _codex != null ? _codex.ColorOf(model.Cell.Grade) : Color.white,
                    cell => BodyOf(cell, art), _vm.TierHeader, _vm.TierLevelRange, OnTierSelected);
                _pendingPreview = model;
                if (_previewBusy == false)
                {
                    RunPreviewLoop();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodexPanel] 상세 갱신 실패 - {e}");
            }
        }

        //# 진행 중인 미리보기가 끝나면 그 사이 쌓인 마지막 요청 하나를 이어서 처리. 각 단계는 타임아웃이 있어 영영 대기하지 않는다.
        private async void RunPreviewLoop()
        {
            _previewBusy = true;
            _fxHold = true;
            try
            {
                while (_pendingPreview != null && _shown && _vm != null)
                {
                    CodexDetailModel model = _pendingPreview;
                    _pendingPreview = null;
                    Task preview = _detail.ShowPreviewAsync(model, _loader);
                    Task finished = await Task.WhenAny(preview, Task.Delay(PreviewTimeoutMs));
                    if (finished != preview)
                    {
                        Debug.LogError($"[CodexPanel] 미리보기 시간 초과({PreviewTimeoutMs}ms) - 다음 요청으로 넘어감");
                        continue;
                    }
                    await preview;
                    if (_shown == false || _vm == null)
                        break;
                    //# 더 새로운 요청이 없을 때만 칸 이펙트 재배분·상세 해금 연출을 재생한다.
                    if (_pendingPreview == null)
                    {
                        _fxDirty = true;
                        if (_codex != null && _vm.ConsumeDetailUnlock(model.Cell))
                        {
                            _detail.PlayUnlock(_config, _codex.UnlockOf(model.Cell.Grade), _codex.ColorOf(model.Cell.Grade));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodexPanel] 미리보기 처리 실패 - {e}");
            }
            finally
            {
                _previewBusy = false;
                _fxHold = false;
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
                    candidates.Add(new CodexFxAllocator.Candidate(cell.Index, cell.Race, cell.Grade, cell.Tier));
                }
            }
            HashSet<int> live = _allocator.Rebalance(candidates);
            foreach (CodexMonsterCell cell in cells)
            {
                if (cell != null && cell.IsRowHeader == false)
                {
                    cell.SetFxLive(live.Contains(cell.Index));
                }
            }
        }
    }
}
