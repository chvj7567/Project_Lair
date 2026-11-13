using System;
using System.Collections.Generic;
using System.Globalization;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    public enum ETowerFloorState
    {
        Preview,    //# 도전 층 위로 미리 보이는 층
        Challenge,  //# 도전 층 = 최고 층 + 1
        Cleared,    //# 클리어한 층
    }

    //# 사다리 셀 한 칸의 확정 표시값 — View(TowerFloorCell)는 그대로 표시만 한다 (Rule 02 §6).
    public class TowerFloorCellData
    {
        public int Floor;
        public ETowerFloorState State;
        public bool IsBoss;
        public bool IsSelected;
        public string NumberText;     //# "18"
        public string HeroName;
        public int RuleCount;         //# 0~2 — 하늘색 점 개수
        public string TagText;        //# "미리보기" / "도전 중" / "클리어 2:31.4"
        public string BossTagText;    //# 보스층 작은 태그 (아니면 빈 문자열)
        public Action<int> OnSelect;
    }

    //# 층 상세 패널 확정 표시값 (infinite-tower §6.2).
    public class TowerFloorDetail
    {
        public int Floor;
        public string FloorText;      //# "10층"
        public bool IsBoss;
        public string BossTagText;
        public string HeroText;       //# "영웅: 성기사"
        public int HeroChapterId;     //# 초상 박스 색 (영웅 챕터)
        public bool ShowStats;        //# 앵커 스탯을 모르면 숨김
        public string StatsText;      //# "영웅 HP 10,719 · 공격력 99"
        public string[] RuleTexts = Array.Empty<string>();   //# 규칙 항목 (최대 2)
        public bool ShowNoRules;
        public string NoRulesText;
        public string SourceText;     //# "규칙 출처: 2-5 사냥꾼 우두머리의 야영지"
        public bool ShowReward;       //# 유물 DB 없으면 숨김 (E-12)
        public string RewardText;     //# "승리 시 유물 확률 60% · 등급 보정 Easy"
        public string RecordText;     //# "클리어 2:31.4" / "최고 17층" / "기록 없음"
    }

    //# 무한의 탑 팝업 ViewModel — 사다리 셀·상세·도전 버튼 계산. View 를 모르고(테스트 가능) 프로필은 읽기만 한다.
    //# 시즌 정리(NormalizeTowerSeason)와 저장은 호출부(VillageController) 소유다.
    public class TowerViewModel
    {
        private const int MaxRuleLines = 2;

        private readonly TowerDatabase _tower;
        private readonly ChapterDatabase _chapters;
        private readonly MetaProfile _profile;
        private readonly Func<int, string> _text;
        private readonly RelicDatabase _relics;
        private readonly int _anchorHp;
        private readonly int _anchorPower;
        private readonly int _challengeFloor;
        private int _selectedFloor;

        public event Action OnChanged;

        //# anchorHp/anchorPower: BalanceConfig.json 영웅 앵커 (0 이하면 능력치 줄 숨김). relics 는 null 허용(보상 줄 숨김).
        public TowerViewModel(TowerDatabase tower, ChapterDatabase chapters, MetaProfile profile, Func<int, string> text,
            RelicDatabase relics, int anchorHp, int anchorPower)
        {
            _tower = tower;
            _chapters = chapters;
            _profile = profile;
            _text = text ?? (id => string.Empty);
            _relics = relics;
            _anchorHp = anchorHp;
            _anchorPower = anchorPower;
            _challengeFloor = profile.GetTowerHighestFloor() + 1;
            _selectedFloor = _challengeFloor;
        }

        public int HighestFloor => _challengeFloor - 1;

        //# 도전 버튼은 항상 도전 층 — 사다리 선택과 무관 (라벨에 층 번호를 넣어 혼동 방지).
        public int ChallengeFloor => _challengeFloor;

        public int SelectedFloor => _selectedFloor;

        public string TitleText => Text(TowerStrings.Title, "무한의 탑");

        //# "시즌 1 · 최고 17층" / 기록 없음이면 "시즌 1 · 기록 없음".
        public string SubtitleText => HighestFloor > 0
            ? Format(TowerStrings.SubtitleBest, "시즌 {0} · 최고 {1}층", _tower.Config.seasonId, HighestFloor)
            : Format(TowerStrings.SubtitleNone, "시즌 {0} · 기록 없음", _tower.Config.seasonId);

        public string ChallengeButtonText => Format(TowerStrings.ChallengeButton, "도전 · {0}층", _challengeFloor);

        public string RankingFailedText => Text(TowerStrings.RankingFailed, "랭킹을 불러오지 못했습니다");

        //# 사다리 선택 — 사다리에 보이는 층만 받는다. 변경 시에만 통지.
        public void SelectFloor(int floor)
        {
            if (IsInLadder(floor) == false || floor == _selectedFloor)
                return;
            _selectedFloor = floor;
            OnChanged?.Invoke();
        }

        //# 선택 표식만 다시 칠한다 — 사다리를 다시 만들면 스크롤이 맨 위로 돌아가므로 선택 변경은 기존 셀 데이터를 갱신한다.
        public void ApplySelectionMarks(IReadOnlyList<TowerFloorCellData> cells)
        {
            foreach (TowerFloorCellData cell in cells)
            {
                cell.IsSelected = cell.Floor == _selectedFloor;
            }
        }

        private bool IsInLadder(int floor)
        {
            if (floor >= _challengeFloor && floor <= _challengeFloor + _tower.Config.previewFloors)
                return true;
            return floor >= 1 && _profile.GetTowerFloorClearTime(floor) > 0f;
        }

        //# 위에서 아래로: 미리보기(도전 층 + N … 도전 층 + 1) → 도전 층 → 클리어한 층(높은 층부터).
        public List<TowerFloorCellData> BuildLadder()
        {
            List<TowerFloorCellData> cells = new List<TowerFloorCellData>();
            for (int floor = _challengeFloor + _tower.Config.previewFloors; floor > _challengeFloor; --floor)
            {
                cells.Add(BuildCell(floor, ETowerFloorState.Preview));
            }
            cells.Add(BuildCell(_challengeFloor, ETowerFloorState.Challenge));

            List<int> cleared = new List<int>();
            if (_profile.TowerFloorClears != null)
            {
                foreach (TowerFloorClearEntry entry in _profile.TowerFloorClears)
                {
                    if (entry != null && cleared.Contains(entry.Floor) == false)
                    {
                        cleared.Add(entry.Floor);
                    }
                }
            }
            cleared.Sort((a, b) => b.CompareTo(a));
            foreach (int floor in cleared)
            {
                cells.Add(BuildCell(floor, ETowerFloorState.Cleared));
            }
            return cells;
        }

        private TowerFloorCellData BuildCell(int floor, ETowerFloorState state)
        {
            TowerFloor info = Floor(floor);
            return new TowerFloorCellData
            {
                Floor = floor,
                State = state,
                IsBoss = info.IsBoss,
                IsSelected = floor == _selectedFloor,
                NumberText = floor.ToString(CultureInfo.InvariantCulture),
                HeroName = HeroName(info.HeroId),
                RuleCount = Math.Min(MaxRuleLines, _chapters.GetRules(info.TemplateChapterId, info.TemplateStageIndex).Count),
                TagText = TagOf(floor, state),
                BossTagText = info.IsBoss ? Text(TowerStrings.BossTag, "보스층") : string.Empty,
                OnSelect = SelectFloor,
            };
        }

        private string TagOf(int floor, ETowerFloorState state)
        {
            switch (state)
            {
                case ETowerFloorState.Challenge:
                    return Text(TowerStrings.ChallengeTag, "도전 중");
                case ETowerFloorState.Cleared:
                    return Format(TowerStrings.ClearTag, "클리어 {0}", ClearTimeFormat.WithTenths(_profile.GetTowerFloorClearTime(floor)));
                default:
                    return Text(TowerStrings.PreviewTag, "미리보기");
            }
        }

        //# 선택한 층의 상세. 능력치는 실제 계산값 (infinite-tower §3.2).
        public TowerFloorDetail BuildDetail()
        {
            TowerFloor info = Floor(_selectedFloor);
            HeroArchetypeRow hero = _chapters.GetHero(info.HeroId);
            HeroArchetype_TowerRow towerHero = _tower.GetHeroRow(info.HeroId);
            TowerFloorDetail detail = new TowerFloorDetail
            {
                Floor = _selectedFloor,
                FloorText = Format(TowerStrings.FloorNumber, "{0}층", _selectedFloor),
                IsBoss = info.IsBoss,
                BossTagText = info.IsBoss ? Text(TowerStrings.BossTag, "보스층") : string.Empty,
                HeroText = Format(TowerStrings.HeroName, "영웅: {0}", HeroName(info.HeroId)),
                HeroChapterId = info.HeroChapterId,
                RecordText = RecordOf(_selectedFloor),
            };

            if (hero != null && towerHero != null && _anchorHp > 0 && _anchorPower > 0)
            {
                HeroStats stats = HeroStatCalculator.ComputeTower(_anchorHp, _anchorPower, hero, towerHero, info);
                detail.ShowStats = true;
                detail.StatsText = Format(TowerStrings.StatsLine, "영웅 HP {0} · 공격력 {1}",
                    stats.Hp.ToString("N0", CultureInfo.InvariantCulture), stats.Power);
            }

            IReadOnlyList<StageRuleData> rules = _chapters.GetRules(info.TemplateChapterId, info.TemplateStageIndex);
            int ruleCount = Math.Min(MaxRuleLines, rules.Count);
            detail.RuleTexts = new string[ruleCount];
            for (int i = 0; i < ruleCount; ++i)
            {
                detail.RuleTexts[i] = StageRuleText.Entry(rules[i], _text);
            }
            detail.ShowNoRules = ruleCount == 0;
            detail.NoRulesText = Text(TowerStrings.NoRules, "특수 규칙 없음");

            StageRow source = _chapters.GetStage(info.TemplateChapterId, info.TemplateStageIndex);
            detail.SourceText = Format(TowerStrings.RuleSource, "규칙 출처: {0}-{1} {2}",
                info.TemplateChapterId, info.TemplateStageIndex, source != null ? _text(source.nameKey) : string.Empty);

            if (_relics != null)
            {
                float chance = RelicDropRoller.ComputeChance(_relics, _profile, info.HeroChapterId, info.Position, info.RelicTier, true);
                detail.ShowReward = true;
                detail.RewardText = Format(TowerStrings.RewardLine, "승리 시 유물 확률 {0}% · 등급 보정 {1}",
                    (int)Math.Round(chance * 100f), DifficultyTable.LabelOf(info.RelicTier, _text));
            }
            return detail;
        }

        //# 클리어한 층이면 클리어 시간, 아니면 최고 층, 진행이 없으면 "기록 없음".
        private string RecordOf(int floor)
        {
            float time = _profile.GetTowerFloorClearTime(floor);
            if (time > 0f)
                return Format(TowerStrings.ClearTag, "클리어 {0}", ClearTimeFormat.WithTenths(time));
            if (HighestFloor > 0)
                return Format(TowerStrings.BestShort, "최고 {0}", Format(TowerStrings.FloorNumber, "{0}층", HighestFloor));
            return Text(TowerStrings.NoRecord, "기록 없음");
        }

        private TowerFloor Floor(int floor)
        {
            return _tower.GetFloor(floor, DifficultyTable.CurrentMaxIndex);
        }

        private string HeroName(string heroId)
        {
            HeroArchetypeRow hero = _chapters.GetHero(heroId);
            return hero != null ? _text(hero.nameKey) : heroId;
        }

        private string Text(int id, string fallback)
        {
            string value = _text != null ? _text(id) : null;
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private string Format(int id, string fallback, params object[] args)
        {
            return string.Format(Text(id, fallback), args);
        }
    }
}
