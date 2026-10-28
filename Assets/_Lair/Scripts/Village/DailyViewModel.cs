using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;

namespace Lair.Village
{
    public enum EDailyRankState
    {
        Loading,
        Loaded,
        Failed,   //# 조회 실패(오프라인·인덱스 미생성 등) — "순위를 불러오지 못했습니다"
    }

    //# 오늘의 유물 칸 하나의 확정 표시값.
    public class DailyRelicView
    {
        public string RelicId;     //# 아이콘 로드 키(ERelic 값명)
        public string NameText;
        public string StackText;   //# "x3"
        public ERelicRarity Rarity;
    }

    //# 데일리 팝업 ViewModel (daily-challenge §9.2) — 서버 시각·조합·순위·내 현황·버튼 상태를 계산. View 를 모르고 프로필은 읽기만 한다(차감·저장은 Battle).
    //# 서버 시각은 ILairApiClient(주입)로 받아 DailyClock 에 고정하고 이후 단조 시계로만 전진한다(§2.2). 호출부가 시작 시 Clock·Challenge 를 세션에 넘긴다.
    public class DailyViewModel
    {
        private const int AttendanceDays = DailyRules.KeepDays;

        private readonly DailyDatabase _db;
        private readonly ChapterDatabase _chapters;
        private readonly RelicDatabase _relics;
        private readonly MetaProfile _profile;
        private readonly ILairApiClient _api;
        private readonly Func<int, string> _text;
        private readonly Func<double> _realtime;
        private readonly string _myUid;
        private readonly string _myName;
        private int _loadVersion;

        public event Action OnChanged;

        //# api 가 null 이면 오프라인(서버 시각 없음 → 시작 불가). realtime 은 단조 시계(초).
        public DailyViewModel(DailyDatabase db, ChapterDatabase chapters, RelicDatabase relics, MetaProfile profile, ILairApiClient api,
            Func<int, string> text, Func<double> realtime, string myUid, string myName)
        {
            _db = db;
            _chapters = chapters;
            _relics = relics;
            _profile = profile;
            _api = api;
            _text = text ?? (id => string.Empty);
            _realtime = realtime;
            _myUid = myUid;
            _myName = myName;
        }

        public bool IsLoading { get; private set; } = true;
        public bool IsOnline => Clock != null;
        public DailyClock Clock { get; private set; }
        public DailyChallenge Challenge { get; private set; }
        public int Today => Challenge != null ? Challenge.DayIndex : 0;
        public EDailyRankState RankState { get; private set; } = EDailyRankState.Loading;
        public List<RankingRowEntry> TopRows { get; private set; } = new List<RankingRowEntry>();
        public RankingRowEntry MyRow { get; private set; }

        private DailyConfigRow Config => _db.Config;

        //# 팝업을 열 때 1회 — 서버 시각 → 오늘 조합 → 순위. 실패해도 예외 없이 오프라인 상태로 남는다(§8).
        public async Task LoadAsync()
        {
            int version = ++_loadVersion;
            IsLoading = true;
            ServerTimeResult time = _api != null ? await _api.GetServerTimeAsync() : ServerTimeResult.Failed;
            if (version != _loadVersion)
                return;
            IsLoading = false;
            if (time.Ok == false)
            {
                Clock = null;
                Challenge = null;
                RankState = EDailyRankState.Failed;
                OnChanged?.Invoke();
                return;
            }
            Clock = new DailyClock(time.UtcMs, _realtime);
            Challenge = _db.Generate(Clock.Today(Config.utcOffsetMinutes));
            OnChanged?.Invoke();
            await LoadRankAsync(version);
        }

        private async Task LoadRankAsync(int version)
        {
            RankState = EDailyRankState.Loading;
            int day = Today;
            List<RankingRowDto> top = await _api.GetDailyTopAsync(day, Config.leaderboardTop);
            List<RankingRowDto> mine = top != null ? await _api.GetMyDailyRankAsync(day) : null;
            if (version != _loadVersion)
                return;
            if (top == null || mine == null)
            {
                RankState = EDailyRankState.Failed;
                OnChanged?.Invoke();
                return;
            }
            TopRows = new List<RankingRowEntry>();
            foreach (RankingRowDto row in top)
            {
                TopRows.Add(new RankingRowEntry
                {
                    Row = row,
                    IsMine = string.IsNullOrEmpty(_myUid) == false && row.uid == _myUid,
                    ScoreText = ScoreText(row.clearTimeMs),
                    HideHero = true,
                });
            }
            MyRow = mine.Count > 0
                ? new RankingRowEntry { Row = mine[0], IsMine = true, ScoreText = ScoreText(mine[0].clearTimeMs), HideHero = true }
                : new RankingRowEntry { Unranked = true, IsMine = true, UnrankedName = _myName, ScoreText = Text(DailyStrings.NoRecord, "기록 없음"), HideHero = true };
            RankState = EDailyRankState.Loaded;
            OnChanged?.Invoke();
        }

        //# 점수 표기 — 승리 "2:51.3", 패배 "패배 · 영웅 HP 12% 남음" (§6.1).
        public string ScoreText(int score)
        {
            if (DailyRules.IsWinScore(score))
                return ClearTimeFormat.WithTenths(score / 1000f);
            return string.Format(Text(DailyStrings.LoseScore, "패배 · 영웅 HP {0}% 남음"), DailyRules.LosePercent(score));
        }

        public string TitleText => Text(DailyStrings.Title, "오늘의 도전");

        //# "10월 27일 · 초기화까지 07:12:33" — 오프라인이면 빈 문자열. 초 단위 갱신은 View 가 이 값을 다시 읽는다.
        public string DateLineText
        {
            get
            {
                if (Challenge == null)
                    return string.Empty;
                DailyRules.MonthDayOf(Challenge.DayIndex, out int month, out int day);
                string date = string.Format(Text(DailyStrings.Date, "{0}월 {1}일"), month, day);
                TimeSpan left = TimeSpan.FromMilliseconds(DailyRules.MsUntilNextDay(Clock.NowUtcMs, Config.utcOffsetMinutes));
                string countdown = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
                return date + "  ·  " + string.Format(Text(DailyStrings.ResetIn, "초기화까지 {0}"), countdown);
            }
        }

        public string OfflineText => Text(DailyStrings.OnlineRequired, "온라인 연결이 필요합니다");

        public string HeroNameText => Challenge != null ? HeroName(Challenge.HeroId) : string.Empty;

        //# "어둠의 숲 · 궁수 · 기준 2-2".
        public string HeroSubText
        {
            get
            {
                if (Challenge == null)
                    return string.Empty;
                ChapterRow chapter = _chapters != null ? _chapters.GetChapter(Challenge.HeroChapterId) : null;
                string chapterName = chapter != null ? _text(chapter.nameKey) : string.Empty;
                return $"{chapterName} · {HeroNameText} · {Challenge.HeroChapterId}-{Challenge.BaseStageIndex}";
            }
        }

        public int HeroChapterId => Challenge != null ? Challenge.HeroChapterId : 0;

        //# 영웅 초상 조회용 선형 스테이지(기준 스테이지).
        public int PortraitStage => Challenge != null ? StageProgress.ToLinear(Challenge.HeroChapterId, Challenge.BaseStageIndex) : 0;

        public string DifficultyText => Challenge != null ? DifficultyTable.LabelOf(Challenge.Difficulty, _text) : string.Empty;
        public int DifficultyIndex => Challenge != null ? Challenge.Difficulty : 0;

        public string AlwaysRuleText => Challenge != null ? StageRuleText.Entry(Challenge.AlwaysRule.Data, _text) : string.Empty;
        public string PhaseRuleText => Challenge != null ? StageRuleText.Entry(Challenge.PhaseRule.Data, _text) : string.Empty;

        public DailyRelicView RelicA => Challenge != null ? RelicView(Challenge.RelicAId, Challenge.RelicAStack) : null;
        public DailyRelicView RelicB => Challenge != null ? RelicView(Challenge.RelicBId, Challenge.RelicBStack) : null;

        public string RankSectionText => Text(DailyStrings.RankSection, "오늘의 순위");
        public string RankFailedText => Text(DailyStrings.RankFailed, "순위를 불러오지 못했습니다");

        //# 내 현황 — 오프라인이면 오늘을 모르므로 도전 칸·기록은 로컬 최신 날짜 기준이 아니라 "모름"(칸 전부 회색)으로 둔다. 출석·연속은 로컬 값.
        public int AttemptsTotal => Config.attemptsPerDay;
        public int AttemptsLeft => Challenge != null ? _profile.DailyAttemptsLeft(Today, Config.attemptsPerDay) : 0;

        public bool[] AttemptPips
        {
            get
            {
                bool[] pips = new bool[AttemptsTotal];
                for (int i = 0; i < pips.Length; ++i)
                {
                    pips[i] = i < AttemptsLeft;
                }
                return pips;
            }
        }

        public string AttemptsText => string.Format(Text(DailyStrings.AttemptsLeft, "남은 도전 {0} / {1}"), AttemptsLeft, AttemptsTotal);

        public string MyRecordText
        {
            get
            {
                if (Challenge != null && _profile.TryGetDailyBest(Today, out int score))
                    return ScoreText(score);
                return Text(DailyStrings.NoRecord, "기록 없음");
            }
        }

        //# 연속 — 오프라인이면 로컬 마지막 기록 날짜 기준이 없어 저장값을 그대로 보인다.
        public string StreakText => string.Format(Text(DailyStrings.Streak, "연속 {0}일"),
            Challenge != null ? _profile.DisplayDailyStreak(Today) : Math.Max(0, _profile.DailyStreak));

        //# 최근 7일 출석 칸(왼쪽 = 6일 전, 마지막 = 오늘). 오프라인이면 전부 false.
        public bool[] Attendance
        {
            get
            {
                bool[] days = new bool[AttendanceDays];
                if (Challenge == null)
                    return days;
                for (int i = 0; i < days.Length; ++i)
                {
                    days[i] = _profile.HasDailyResult(Today - (AttendanceDays - 1) + i);
                }
                return days;
            }
        }

        public bool FirstWinClaimed => Challenge != null && _profile.HasDailyWin(Today);

        //# "첫 승리 보상: 소울 +180, 유물 1개 확정" — 미리보기 배율은 이번 판으로 갱신될 연속 기준(§7.1). 받았으면 "받음".
        public string FirstWinText
        {
            get
            {
                if (FirstWinClaimed)
                    return Text(DailyStrings.Claimed, "받음");
                int streak = Challenge != null ? _profile.PreviewDailyStreak(Today) : 1;
                return string.Format(Text(DailyStrings.FirstWinReward, "첫 승리 보상: 소울 +{0}, 유물 1개 확정"), DailyRules.BonusSouls(streak, Config));
            }
        }

        public string GuideText => string.Format(Text(DailyStrings.Guide, "하루 {0}번 도전할 수 있습니다."), Config.attemptsPerDay);

        public bool StartEnabled => Challenge != null && AttemptsLeft > 0;

        public string StartText
        {
            get
            {
                if (Challenge == null)
                    return OfflineText;
                return AttemptsLeft > 0 ? Text(DailyStrings.Start, "도전 시작") : Text(DailyStrings.Exhausted, "오늘 도전을 모두 사용했습니다");
            }
        }

        public string StartSubText => Challenge != null && AttemptsLeft > 0 ? AttemptsText : string.Empty;

        //# 시작 판정 (§2.2-3) — 시작 순간 날짜를 다시 계산. 0시를 넘겼으면 새 조합으로 갱신하고 시작하지 않는다(DayChanged).
        public EDailyStartCheck TryStart()
        {
            if (Challenge == null)
                return EDailyStartCheck.Offline;
            EDailyStartCheck check = DailyRules.CheckStart(_profile, _db, Clock, Challenge.DayIndex);
            if (check == EDailyStartCheck.DayChanged)
            {
                Challenge = _db.Generate(Clock.Today(Config.utcOffsetMinutes));
                OnChanged?.Invoke();
                if (_api != null)
                {
                    _ = LoadRankAsync(_loadVersion);
                }
            }
            return check;
        }

        private DailyRelicView RelicView(string relicId, int stack)
        {
            RelicData data = _relics != null ? _relics.Get(relicId) : null;
            return new DailyRelicView
            {
                RelicId = relicId,
                NameText = data != null ? Text(data.NameKey, relicId) : relicId,
                StackText = "x" + stack,
                Rarity = data != null ? data.Rarity : ERelicRarity.Common,
            };
        }

        private string HeroName(string heroId)
        {
            HeroArchetypeRow hero = _chapters != null ? _chapters.GetHero(heroId) : null;
            return hero != null ? Text(hero.nameKey, heroId) : heroId;
        }

        private string Text(int id, string fallback)
        {
            string value = _text(id);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
