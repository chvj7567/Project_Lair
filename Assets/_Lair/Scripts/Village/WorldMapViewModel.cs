using System;
using System.Collections.Generic;
using System.Text;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    //# 월드맵 노드 상태 (chapter-system §6.3). 선택/보스는 별도 플래그.
    public enum EWorldMapNodeState
    {
        Locked,    //# 아직 해금 전 (또는 출격 불가 챕터)
        Open,      //# 해금, 미클리어
        Cleared,   //# linear <= ClearedStage
    }

    //# 챕터 탭 1칸 표시 데이터 — 개방 탭 + "???" 미출시 슬롯.
    public class WorldMapTabData
    {
        public int ChapterId;
        public string Label;           //# "1장 지하 묘지" / "3장 ???"
        public string LockTagText;     //# 잠금 탭 보조 표기("잠금"/"준비 중"), 열린 탭은 빈 문자열
        public bool IsSelected;
        public bool IsLocked;          //# 눌러도 이동 불가(진행 잠금 또는 출격 불가)
        public bool IsPlaceholder;     //# 미출시 "???" 슬롯
        public string ChapterNameText; //# 카드 챕터명 — 개방 "그림자 성채" / 미출시 "{N}장 ???"
        public string HeroText;        //# 카드 영웅 직업 — 영웅 row 없거나 미출시면 빈 문자열
        public int ClearedCount;       //# 챕터 안 클리어 수 0~5 (미출시 0)
    }

    //# 스테이지 노드 1칸 표시 데이터.
    public class WorldMapNodeData
    {
        public int StageIndex;
        public int Linear;
        public EWorldMapNodeState State;
        public bool IsBoss;
        public bool IsSelected;
        public string LabelText;       //# 해금 "{챕터}-{인덱스}" / 잠금 "잠금"
        public string NameText;        //# 해금 스테이지명 / 잠금 "???"
        public string ClearedTagText;  //# 클리어 노드만 "클리어", 그 외 빈 문자열
        public bool NextPathCleared;   //# 이 노드 → 다음 노드 구간이 클리어 실선인지 (보스는 항상 false)
    }

    //# 월드맵 ViewModel — 챕터 탭·노드 상태·상세 패널 문구·선택 확정을 가공한다 (Rule 02 §6, Unity 비의존, View 를 모른다).
    //# 해금 판정은 StageProgress, 출격 가능 판정은 ChapterGate 단일 소유. 선택 확정 저장은 호출자가 onConfirm 으로 처리한다.
    public class WorldMapViewModel
    {
        //# 탭 슬롯 수 — 출시 챕터 + "???" 미출시 슬롯 합 (목업 5칸).
        public const int ChapterSlotCount = 5;

        //# Strings_Ko id (chapter-system §6.5).
        public const int StrChapterTitle = 1311;        //# {0}장 {1}
        public const int StrChapterPlaceholder = 1312;  //# {0}장 ???
        public const int StrSelectButton = 1307;
        public const int StrCleared = 1308;
        public const int StrLocked = 1309;
        public const int StrNoRules = 1310;
        public const int StrNoRecord = 1306;
        public const int StrChapterLockedHint = 1315;   //# {0}장은 {1}장 보스를 클리어하면 열립니다
        public const int StrSelectNote = 1316;
        public const int StrThemeHero = 1320;           //# 테마: {0} / 영웅: {1}
        public const int StrHeroName = 1321;            //# 영웅: {0}
        public const int StrNotReady = 1322;            //# 준비 중
        public const int StrNotReadyHint = 1323;        //# 이 챕터는 아직 출격할 수 없습니다
        public const int StrRecord = 1324;              //# 최단 {0} / {1}승 {2}판
        public const int StrStageLockedNote = 1325;

        private readonly ChapterDatabase _chapters;
        private readonly MetaProfile _profile;
        private readonly Func<int, string> _text;
        private readonly Action<int> _onConfirm;

        private int _chapterId;
        private int _stageIndex;
        private readonly List<WorldMapTabData> _tabs = new List<WorldMapTabData>();
        private readonly List<WorldMapNodeData> _nodes = new List<WorldMapNodeData>();

        public event Action OnChanged;
        //# 안내 문구(잠금 탭/노드를 눌렀을 때) — View 가 토스트로 표시.
        public event Action<string> OnNotice;
        //# 선택 확정 직후 — View 가 팝업을 닫는다.
        public event Action OnConfirmed;

        public WorldMapViewModel(ChapterDatabase chapters, MetaProfile profile, Func<int, string> text, Action<int> onConfirm)
        {
            _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _text = text ?? (id => string.Empty);
            _onConfirm = onConfirm;

            //# 초기 선택 = 현재 SelectedStage. 잠금/출격 불가 값이면 최대 플레이 가능 스테이지로 보정(기획서 §6.3).
            int linear = _profile.SelectedStage;
            if (IsSelectable(linear) == false)
            {
                linear = Math.Max(1, Math.Min(ChapterGate.PlayableStageCount, StageProgress.MaxPlayableStage(_profile.ClearedStage)));
            }
            _chapterId = StageProgress.ChapterOf(linear);
            _stageIndex = StageProgress.IndexOf(linear);
            Rebuild();
        }

        public int SelectedChapter => _chapterId;
        public int SelectedStageIndex => _stageIndex;
        public int SelectedLinear => StageProgress.ToLinear(_chapterId, _stageIndex);
        public IReadOnlyList<WorldMapTabData> Tabs => _tabs;
        public IReadOnlyList<WorldMapNodeData> Nodes => _nodes;

        //# 지도 영역 헤더.
        public string ChapterTitleText { get; private set; }
        public string ChapterThemeText { get; private set; }

        //# 상세 패널.
        public string DetailNumberText { get; private set; }
        public bool DetailIsBoss { get; private set; }
        public string DetailNameText { get; private set; }
        public string DetailStarsText { get; private set; }
        public string DetailHeroText { get; private set; }
        public string DetailRecordText { get; private set; }
        public string DetailRulesText { get; private set; }
        public string DetailBossPhaseText { get; private set; }
        public bool HasBossPhases { get; private set; }
        public bool DetailLocked { get; private set; }

        //# 하단.
        public string BottomText { get; private set; }
        public string SelectButtonText { get; private set; }
        public bool SelectEnabled { get; private set; }

        //# 챕터 탭 클릭 — 잠금/출격 불가면 안내만. 이동하면 true.
        public bool SelectChapter(int chapterId)
        {
            ChapterRow chapter = _chapters.GetChapter(chapterId);
            if (chapter == null)
                return false;

            if (ChapterGate.IsChapterPlayable(chapterId) == false)
            {
                OnNotice?.Invoke(_text(StrNotReadyHint));
                return false;
            }
            if (StageProgress.IsChapterUnlocked(chapterId, _profile.ClearedStage) == false)
            {
                OnNotice?.Invoke(string.Format(_text(StrChapterLockedHint), chapterId, chapterId - 1));
                return false;
            }

            //# 기존 인덱스를 유지하되 잠겨 있으면 1 로(기획서 목업 동작).
            int index = _stageIndex;
            if (IsSelectable(StageProgress.ToLinear(chapterId, index)) == false)
            {
                index = 1;
            }
            _chapterId = chapterId;
            _stageIndex = index;
            Rebuild();
            OnChanged?.Invoke();
            return true;
        }

        //# 노드 클릭 — 잠금/출격 불가면 안내만. 선택되면 true.
        public bool SelectNode(int stageIndex)
        {
            if (stageIndex < 1 || stageIndex > StageProgress.StagesPerChapter)
                return false;

            int linear = StageProgress.ToLinear(_chapterId, stageIndex);
            if (IsSelectable(linear) == false)
            {
                OnNotice?.Invoke(LockedHint(linear));
                return false;
            }
            _stageIndex = stageIndex;
            Rebuild();
            OnChanged?.Invoke();
            return true;
        }

        //# "이 스테이지로 선택" — 선택 가능한 스테이지일 때만 onConfirm(선형 번호) 후 OnConfirmed. 로컬 저장·마을 반영은 호출자 책임.
        public bool Confirm()
        {
            int linear = SelectedLinear;
            if (IsSelectable(linear) == false)
                return false;
            _onConfirm?.Invoke(linear);
            OnConfirmed?.Invoke();
            return true;
        }

        //# 해금(선행 클리어) + 출격 가능 챕터. 두 조건이 모두 맞아야 선택 가능.
        private bool IsSelectable(int linear)
        {
            return StageProgress.IsUnlocked(linear, _profile.ClearedStage) && ChapterGate.IsStagePlayable(linear);
        }

        //# 잠금 안내 — 출격 불가(해금은 됨)면 "준비 중" 안내, 선행 미클리어면 선행 조건 안내.
        private string LockedHint(int linear)
        {
            if (StageProgress.IsUnlocked(linear, _profile.ClearedStage) && ChapterGate.IsStagePlayable(linear) == false)
                return _text(StrNotReadyHint);
            return StageLabels.UnlockHint(linear, _text);
        }

        private void Rebuild()
        {
            RebuildTabs();
            RebuildNodes();
            RebuildHeader();
            RebuildDetail();
        }

        private void RebuildTabs()
        {
            _tabs.Clear();
            for (int slot = 1; slot <= ChapterSlotCount; ++slot)
            {
                ChapterRow chapter = _chapters.GetChapter(slot);
                if (chapter == null)
                {
                    string placeholder = string.Format(_text(StrChapterPlaceholder), slot);
                    _tabs.Add(new WorldMapTabData
                    {
                        ChapterId = slot,
                        Label = placeholder,
                        ChapterNameText = placeholder,
                        HeroText = string.Empty,
                        ClearedCount = 0,
                        LockTagText = string.Empty,
                        IsPlaceholder = true,
                        IsLocked = true,
                    });
                    continue;
                }

                bool playable = ChapterGate.IsChapterPlayable(slot);
                bool unlocked = StageProgress.IsChapterUnlocked(slot, _profile.ClearedStage);
                HeroArchetypeRow tabHero = _chapters.GetHero(chapter.heroId);
                _tabs.Add(new WorldMapTabData
                {
                    ChapterId = slot,
                    Label = string.Format(_text(StrChapterTitle), slot, _text(chapter.nameKey)),
                    ChapterNameText = _text(chapter.nameKey),
                    HeroText = tabHero != null ? _text(tabHero.nameKey) : string.Empty,
                    ClearedCount = StageProgress.ClearedCountInChapter(slot, _profile.ClearedStage),
                    LockTagText = playable == false ? _text(StrNotReady) : (unlocked ? string.Empty : _text(StrLocked)),
                    IsSelected = slot == _chapterId,
                    IsLocked = playable == false || unlocked == false,
                    IsPlaceholder = false,
                });
            }
        }

        private void RebuildNodes()
        {
            _nodes.Clear();
            for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
            {
                int linear = StageProgress.ToLinear(_chapterId, index);
                bool selectable = IsSelectable(linear);
                StageRow stage = _chapters.GetStage(_chapterId, index);
                EWorldMapNodeState state = selectable == false
                    ? EWorldMapNodeState.Locked
                    : (linear <= _profile.ClearedStage ? EWorldMapNodeState.Cleared : EWorldMapNodeState.Open);
                _nodes.Add(new WorldMapNodeData
                {
                    StageIndex = index,
                    Linear = linear,
                    State = state,
                    IsBoss = StageProgress.IsBossStage(index),
                    IsSelected = index == _stageIndex,
                    LabelText = selectable ? StageLabels.Number(linear) : _text(StrLocked),
                    NameText = selectable && stage != null ? _text(stage.nameKey) : "???",
                    ClearedTagText = state == EWorldMapNodeState.Cleared ? _text(StrCleared) : string.Empty,
                    NextPathCleared = state == EWorldMapNodeState.Cleared && index < StageProgress.StagesPerChapter,
                });
            }
        }

        private void RebuildHeader()
        {
            ChapterRow chapter = _chapters.GetChapter(_chapterId);
            HeroArchetypeRow hero = chapter != null ? _chapters.GetHero(chapter.heroId) : null;
            string heroName = hero != null ? _text(hero.nameKey) : string.Empty;
            ChapterTitleText = chapter != null ? string.Format(_text(StrChapterTitle), _chapterId, _text(chapter.nameKey)) : string.Empty;
            ChapterThemeText = chapter != null ? string.Format(_text(StrThemeHero), _text(chapter.themeTextKey), heroName) : string.Empty;
        }

        private void RebuildDetail()
        {
            int linear = SelectedLinear;
            bool selectable = IsSelectable(linear);
            ChapterRow chapter = _chapters.GetChapter(_chapterId);
            HeroArchetypeRow hero = chapter != null ? _chapters.GetHero(chapter.heroId) : null;
            StageRow stage = _chapters.GetStage(_chapterId, _stageIndex);

            DetailLocked = selectable == false;
            DetailIsBoss = StageProgress.IsBossStage(_stageIndex);
            DetailNumberText = StageLabels.Number(linear);
            DetailNameText = selectable && stage != null ? _text(stage.nameKey) : "???";
            DetailStarsText = StageLabels.Stars(_stageIndex);
            DetailHeroText = hero != null ? string.Format(_text(StrHeroName), _text(hero.nameKey)) : string.Empty;
            DetailRecordText = RecordText(linear, selectable);

            BuildRuleTexts();

            if (selectable)
            {
                BottomText = _text(StrSelectNote);
                SelectButtonText = _text(StrSelectButton);
            }
            else
            {
                BottomText = _text(StrStageLockedNote);
                SelectButtonText = LockedHint(linear);
            }
            SelectEnabled = selectable;
        }

        //# 해금 + 기록 있음 "최단 m:ss.t / N승 M판", 해금 + 기록 없음 "기록 없음", 잠금 "-".
        private string RecordText(int linear, bool selectable)
        {
            if (selectable == false)
                return "-";
            StageRecordEntry record = _profile.GetStageRecord(linear);
            if (record.Runs < 1)
                return _text(StrNoRecord);
            return string.Format(_text(StrRecord), ClearTimeFormat.WithTenths(record.BestClearTime), record.Wins, record.Runs);
        }

        //# 상시 규칙 목록(없으면 "특수 규칙 없음") + 보스 페이즈 목록(트리거 row 가 있는 규칙).
        private void BuildRuleTexts()
        {
            StringBuilder rules = new StringBuilder();
            StringBuilder phases = new StringBuilder();
            foreach (StageRuleData data in _chapters.GetRules(_chapterId, _stageIndex))
            {
                StringBuilder target = data.HasTrigger ? phases : rules;
                if (target.Length > 0)
                {
                    target.Append("\n");
                }
                target.Append(StageRuleText.Entry(data, _text));
            }

            DetailRulesText = rules.Length > 0 ? rules.ToString() : _text(StrNoRules);
            DetailBossPhaseText = phases.ToString();
            HasBossPhases = phases.Length > 0;
        }
    }
}
