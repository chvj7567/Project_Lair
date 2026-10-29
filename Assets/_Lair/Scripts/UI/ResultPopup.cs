using System.Collections;
using System.Collections.Generic;
using System.Text;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ResultPopupArg : UIArg
    {
        public BattleResult Result;
        //# v0.2 메타 — 보상 표기 (기획서 §9). HasMeta=false (MetaConfig 미할당) 면 보상 블록 숨김.
        public bool HasMeta;
        public int SoulsGained;
        public int XpGained;
        public int LordLevel;          //# 이번 정산 레벨 업 시 신규 레벨, 아니면 0 — 영주 줄 생략 키 (§9.2)
        public int LordRewardSouls;    //# 이번 정산 자동 수령 소울 합 — 0 이면 "+N 소울" 부분만 생략
        public List<AchievementDef> NewlyAchieved = new List<AchievementDef>();
        //# UI 리디자인 — 결과 항목 줄 입력. ClearTime=이번 판 경과(초), IsNewBest=스테이지 신기록(승리 한정),
        //# HeroHpRatio=영웅 남은 HP 비율 0~1(패배 화면 전용 줄).
        public float ClearTime;
        public bool IsNewBest;
        public float HeroHpRatio;
        //# 난이도(difficulty-select §5.3) — Difficulty=이번 판 EffectiveDifficulty, DifficultyUnlocked=이번 승리로 새로 열린 난이도(0=없음).
        public int Difficulty;
        public int DifficultyUnlocked;
        //# 유물 드랍(relics §7.3) — HasRelicDrop=이번 판 획득 여부(sentinel 없음, 아래 필드는 true 일 때만 유효).
        public bool HasRelicDrop;
        public string RelicId;
        public int RelicStackAfter;
        public int RelicMaxStack;
        public bool RelicIsNew;
        //# 승리 + 미획득이면 "유물 없음 · 다음 승리 시 획득 확률 N%" 줄.
        public bool ShowNextChance;
        public int NextWinChancePercent;
        //# 탑(infinite-tower §6.4) — IsTower=이번 판이 탑 층 도전, TowerFloor/TowerIsBoss 는 IsTower 일 때만 유효. 탑은 난이도 줄 대신 "도달 층" 줄을 쓴다.
        public bool IsTower;
        public int TowerFloor;
        public bool TowerIsBoss;
        //# 데일리(daily-challenge §9.4) — IsDaily 하나로 분기, 아래 필드는 IsDaily 일 때만 유효. 순위는 결과창 표시 후 ShowDailyRank 로 갱신.
        public bool IsDaily;
        public int DailyDayIndex;
        public int DailyAttemptsLeft;
        public bool DailyFirstWinBonus;
        public int DailyBonusSouls;
        public int DailyStreak;
        public decimal DailyStreakMultiplier = 1m;
        //# 데일리 "다시 도전" 판정·씬 이동 — 날짜/남은 도전 판정은 호출부 소유. 반환 = 씬 이동을 시작했는가(false 면 버튼 재활성).
        public System.Func<bool> OnDailyRetry;
        //# 육성 재화(monster-growth-currencies §8.4) — 이번 판 특수 재화 획득(0 은 행 없음). 소울 줄에 칩으로 합친다.
        public List<CurrencyGain> CurrencyGains = new List<CurrencyGain>();
        //# 유물 띠 잔해 변형 — HasRelicShard 일 때만 아래 두 필드가 유효(뽑힌 등급의 후보가 모두 최대 중첩).
        public bool HasRelicShard;
        public int RelicShardAmount;
        public string RelicShardRarity;
    }

    //# 결과 팝업 항목 줄 표시 확정값 — View 는 이 값을 그대로 표시만 한다(Rule 02 §6).
    public struct ResultRowsData
    {
        public string SubText;         //# 큰 결과 글씨 아래 부제
        public bool ShowClearRow;      //# 승리 전용 "클리어 시간" 줄
        public string ClearTimeText;   //# "3:42.1"
        public bool ShowNewBadge;      //# 신기록 NEW 배지
        public bool ShowHeroHpRow;     //# 패배 전용 "영웅 남은 HP" 줄
        public string HeroHpText;      //# "18%"
        public bool ShowSoulsRow;      //# 메타 미할당이면 숨김
        public string SoulsText;       //# "+180" / 특수 재화가 있으면 "+180 소울  +25 각성석" (재화 색 칩)
        public string SoulsLabelText;  //# "소울" / 특수 재화가 있으면 "보상"
        public List<CurrencyPillData> RewardChips;   //# 특수 재화 획득 판의 칩 [소울, 특수] — 없으면 null (기존 "+180" 텍스트)
        public bool ShowXpRow;
        public string XpText;          //# "+60"
        public bool ShowDifficultyRow;     //# Normal/Hard 판에서만 "난이도" 줄
        public string DifficultyText;      //# "Normal" / "Hard"
        public int DifficultyIndex;        //# 값 색 결정용 난이도 인덱스
        public bool ShowUnlockBanner;      //# 승리 + 새 난이도 해금 배너
        public string UnlockBannerText;    //# "Normal 해금!"
        public bool ShowRelicBand;         //# 이번 판 유물 획득 띠
        public string RelicId;             //# 아이콘 로드 키(ERelic 값명)
        public string RelicNameText;
        public string RelicRarityText;     //# "일반" / "희귀" / "전설"
        public Color RelicRarityColor;     //# 등급 색(테두리·칩·이름)
        public bool RelicIsLegendary;      //# 전설이면 테두리 펄스·글로우
        public bool RelicIsShard;          //# 잔해 변형 띠 (monster-growth-currencies §8.4) — 아이콘 = 잔해, NEW 배지 없음
        public bool ShowRelicNewBadge;     //# 신규 획득 "NEW"
        public string RelicStackText;      //# 중복이면 "중첩 +1 (2 / 5)", 신규면 빈 문자열
        public bool ShowRelicMissRow;      //# 승리 + 미획득
        public string RelicMissText;       //# "유물 없음 · 다음 승리 시 획득 확률 N%"
        public string DifficultyLabelText; //# 난이도 줄 라벨 — 챕터 "난이도", 탑 "도달 층" (재사용 팝업이 이전 문구를 남기지 않게 매번 확정)
        public string RetryLabelText;      //# 왼쪽 버튼 라벨 — "다시 도전" / 탑 승리 "다음 층" / 데일리 "다시 도전 (남은 n)"
        public bool RetryInteractable;     //# 데일리 남은 도전 0 이면 false
    }

    //# 결과 표시 + 보상 요약 + 「다시 도전」(Battle 재시작) / 「마을로」(Village 복귀) 2버튼. ChvjPackage UI 래퍼 사용 (Rule 11).
    public class ResultPopup : UIBase
    {
        //# 결과 팝업 도전과제 표기 상한 — 초과분은 "외 N건 달성" (기획서 §9.1).
        private const int MaxAchievedLines = 3;
        private const int StrUnlockBanner = 1333;   //# {0} 해금!
        private const int StrDifficultyLabel = 1334; //# 난이도
        private const int StrRelicStack = 1351;     //# 중첩 +1 ({0} / {1})
        private const int StrRelicMiss = 1352;      //# 유물 없음 · 다음 승리 시 획득 확률 {0}%
        private const float RelicBandSlideSec = 0.3f;
        //# 유물 잔해 색 #c8a27a (Currency.json) — 띠 이름 줄·임시 아이콘 틴트.
        private static readonly Color ShardColor = new Color32(0xC8, 0xA2, 0x7A, 0xFF);
        private const float RelicIconPopSec = 0.25f;
        private const float RelicBandSlideDistance = 30f;

        //# 전투 종료 후 유일한 출구 — ESC 로 닫히면 씬 이동 수단이 사라진다.
        public override bool CanCloseByEsc => false;

        [SerializeField] private CHText _resultText;
        [SerializeField] private CHText _rewardText;
        //# UI 리디자인 항목 줄 — 위젯 연결은 프리팹 단계. 미할당이면 건너뛴다.
        [SerializeField] private CHText _subText;
        [SerializeField] private GameObject _rowClear;
        [SerializeField] private CHText _clearTimeText;
        [SerializeField] private GameObject _newBadge;
        [SerializeField] private GameObject _rowSouls;
        [SerializeField] private CHText _soulsText;
        [SerializeField] private CHText _soulsLabelText;   //# 소울 줄 라벨 — 특수 재화가 있으면 "보상"(monster-growth-currencies §8.4)
        [SerializeField] private GameObject _rowXp;
        [SerializeField] private CHText _xpText;
        [SerializeField] private GameObject _rowHeroHp;
        [SerializeField] private CHText _heroHpText;
        [SerializeField] private GameObject _rowDifficulty;
        [SerializeField] private CHText _difficultyText;
        [SerializeField] private CHText _difficultyLabelText;   //# 난이도 줄 라벨 — 탑에서 "도달 층"으로 바뀐다
        [SerializeField] private CHText _retryLabelText;         //# 왼쪽 버튼 라벨 — 탑 승리에서 "다음 층"으로 바뀐다
        [SerializeField] private GameObject _unlockBanner;
        [SerializeField] private CHText _unlockBannerText;
        [SerializeField] private GameObject _rays;   //# 승리 방사광 — 승리에서만 표시
        [SerializeField] private GameObject[] _winDecor;    //# 승리 장식 묶음 (DecoWinBack · DecoWinFront) — 승리에서만 표시
        [SerializeField] private GameObject[] _loseDecor;   //# 패배 장식 묶음 (DecoLoseBack · DecoLoseFront) — 패배에서만 표시
        //# 유물 획득 띠(relics §7.3) — 위젯 연결은 프리팹 단계. 미할당이면 건너뛴다.
        [SerializeField] private RectTransform _relicBand;
        [SerializeField] private Image _relicBandBorder;
        [SerializeField] private Image _relicIcon;
        [SerializeField] private RectTransform _relicIconFrame;   //# 아이콘 팝 연출 대상(아이콘 + 등급 테두리)
        [SerializeField] private CHText _relicNameText;
        [SerializeField] private Image _relicRarityChip;
        [SerializeField] private CHText _relicRarityText;
        [SerializeField] private GameObject _relicNewBadge;
        [SerializeField] private CHText _relicStackText;
        [SerializeField] private GameObject _relicGlow;
        [SerializeField] private GameObject _rowRelicMiss;
        [SerializeField] private CHText _relicMissText;
        //# 잔해 아이콘 — 전용 스프라이트가 없으면 소울 코인을 잔해 색으로 물들인 임시 표시(_shardIconTint).
        [SerializeField] private Sprite _shardIcon;
        [SerializeField] private bool _shardIconTint = true;
        //# 소울 줄 재화 칩 2개 (아이콘 + "+N" + 이름, §8.4). 미배선이면 _soulsText 리치 텍스트로 표시.
        [SerializeField] private CurrencyPillView[] _rewardChips = new CurrencyPillView[0];
        [SerializeField] private Sprite _soulIcon;
        [SerializeField] private Sprite _awakenStoneIcon;
        [SerializeField] private Sprite _moonSealIcon;
        [SerializeField] private Sprite _abyssCrystalIcon;
        [SerializeField] private CHButton _retryButton;
        [SerializeField] private CHButton _villageButton;

        private bool _sceneLoadRequested;
        private bool _isDaily;
        private System.Func<bool> _onDailyRetry;
        private Vector2 _relicBandRest;
        private bool _relicBandRestCaptured;
        private Coroutine _relicIntro;
        //# CHMUI 는 InitUI 후 SetActive(true) 순서라 InitUI 시점엔 비활성 — 연출은 OnEnable 에서 시작한다.
        private bool _relicIntroPending;
        private bool _relicIntroLegendary;
        private Color _relicIntroColor;

        public override void InitUI(UIArg arg)
        {
            //# 풀 재사용 리셋 (Rule 03 §4) — 가드가 남으면 재사용된 팝업의 버튼이 죽는다.
            _sceneLoadRequested = false;
            _isDaily = false;
            _onDailyRetry = null;

            if (arg is ResultPopupArg rp)
            {
                _isDaily = rp.IsDaily;
                _onDailyRetry = rp.IsDaily ? rp.OnDailyRetry : null;
                if (_resultText != null)
                {
                    _resultText.SetText(rp.Result switch
                    {
                        BattleResult.Win  => "승리",
                        BattleResult.Lose => "패배",
                        _                 => "-"
                    });
                }

                if (_resultText != null)
                {
                    //# 승리=금 / 패배=붉은 제목 (시안).
                    _resultText.SetColor(rp.Result == BattleResult.Win ? UiDotPalette.Gold : new Color32(0xFF, 0x6B, 0x6F, 0xFF));
                }
                ApplyRows(BuildRows(rp));
                if (_rays != null)
                {
                    _rays.SetActive(rp.Result == BattleResult.Win);
                }
                //# 매 InitUI 가 두 묶음을 모두 덮어써 풀 재사용 시 이전 결과 장식이 남지 않는다.
                SetDecorActive(_winDecor, rp.Result == BattleResult.Win);
                SetDecorActive(_loseDecor, rp.Result == BattleResult.Lose);

                if (_rewardText != null)
                {
                    _rewardText.gameObject.SetActive(rp.HasMeta);
                    if (rp.HasMeta)
                    {
                        _rewardText.SetText(BuildRewardText(rp));
                    }
                }
            }

            //# CHButton.OnClick(Action, CompositeDisposable) — closeDisposable.Clear() 시 자동 해제
            if (_retryButton != null)
            {
                _retryButton.OnClick(OnClickRetry, closeDisposable);
            }

            if (_villageButton != null)
            {
                _villageButton.OnClick(OnClickToVillage, closeDisposable);
            }
        }

        //# 항목 줄 데이터 조립 — 승리=클리어 시간(+NEW), 패배=영웅 남은 HP. 소울/XP 는 메타가 있을 때만.
        public static ResultRowsData BuildRows(ResultPopupArg arg)
        {
            bool win = arg.Result == BattleResult.Win;
            bool lose = arg.Result == BattleResult.Lose;
            ResultRowsData rows = WithRelic(new ResultRowsData
            {
                SubText = win ? "영웅을 처치했다" : lose ? "영웅이 던전을 돌파했다" : string.Empty,
                ShowClearRow = win,
                ClearTimeText = FormatClearTime(arg.ClearTime),
                ShowNewBadge = win && arg.IsNewBest,
                ShowHeroHpRow = lose,
                HeroHpText = $"{Mathf.RoundToInt(Mathf.Clamp01(arg.HeroHpRatio) * 100f)}%",
                ShowSoulsRow = arg.HasMeta,
                SoulsText = $"+{arg.SoulsGained}",
                ShowXpRow = arg.HasMeta,
                XpText = $"+{arg.XpGained}",
                ShowDifficultyRow = arg.Difficulty >= 1,
                DifficultyText = DifficultyTable.LabelOf(arg.Difficulty),
                DifficultyIndex = arg.Difficulty,
                ShowUnlockBanner = win && arg.DifficultyUnlocked >= 1,
                UnlockBannerText = string.Format(LookupString(StrUnlockBanner, "{0} 해금!"), DifficultyTable.LabelOf(arg.DifficultyUnlocked)),
                DifficultyLabelText = LookupString(StrDifficultyLabel, "난이도"),
                RetryLabelText = LookupString(TowerStrings.Retry, "다시 도전"),
                RetryInteractable = true,
            }, arg);
            rows = WithCurrencyGains(rows, arg);
            if (arg.IsDaily)
                return WithDaily(rows, arg);
            return arg.IsTower ? WithTower(rows, arg) : rows;
        }

        //# 재화 칩 (§8.4) — 특수 재화 획득이 있으면 라벨 "보상" + "+180 소울  +25 각성석"(값은 재화 색). 없으면 기존 "소울" / "+180".
        public static ResultRowsData WithCurrencyGains(ResultRowsData rows, ResultPopupArg arg)
        {
            rows.SoulsLabelText = LookupString(1465, "소울");
            if (arg.CurrencyGains == null || arg.CurrencyGains.Count == 0)
                return rows;
            GrowthDatabase growth = GrowthDatabase.Current;
            StringBuilder sb = new StringBuilder();
            rows.RewardChips = new List<CurrencyPillData>
            {
                new CurrencyPillData { Currency = ECurrency.Soul, AmountText = $"+{arg.SoulsGained}", NameText = rows.SoulsLabelText, ColorHex = GrowthText.CurrencyColorHex(ECurrency.Soul, growth) },
            };
            sb.Append(Chip($"+{arg.SoulsGained}", GrowthText.CurrencyColorHex(ECurrency.Soul, growth), rows.SoulsLabelText));
            foreach (CurrencyGain gain in arg.CurrencyGains)
            {
                if (gain.Amount < 1)
                    continue;
                string name = GrowthText.CurrencyName(gain.Currency, growth, id => LookupString(id, string.Empty));
                rows.RewardChips.Add(new CurrencyPillData { Currency = gain.Currency, AmountText = $"+{gain.Amount}", NameText = name, ColorHex = GrowthText.CurrencyColorHex(gain.Currency, growth) });
                sb.Append("  ").Append(Chip($"+{gain.Amount}", GrowthText.CurrencyColorHex(gain.Currency, growth), name));
            }
            rows.SoulsLabelText = LookupString(GrowthStrings.RewardLabel, "보상");
            rows.SoulsText = sb.ToString();
            return rows;
        }

        //# 칩 위젯 미배선 폴백 — "+N"(재화 색) + 작은 재화 이름 리치 텍스트.
        private static string Chip(string amount, string colorHex, string name)
            => $"<color={colorHex}>{amount}</color> <size=75%>{name}</size>";

        //# 데일리 변형 (daily-challenge §9.4) — 부제 "데일리 · 10월 27일", 난이도 줄을 "오늘의 순위"(집계 전 "순위 집계 중")로, 첫 승리 배너, 신기록·해금 배너·다음 확률 줄 없음.
        public static ResultRowsData WithDaily(ResultRowsData rows, ResultPopupArg arg)
        {
            DailyRules.MonthDayOf(arg.DailyDayIndex, out int month, out int dayOfMonth);
            string date = string.Format(LookupString(DailyStrings.Date, "{0}월 {1}일"), month, dayOfMonth);
            rows.SubText = string.Format(LookupString(DailyStrings.ModeTitle, "데일리 · {0}"), date);
            rows.ShowNewBadge = false;
            rows.ShowRelicMissRow = false;
            rows.ShowDifficultyRow = true;
            rows.DifficultyLabelText = LookupString(DailyStrings.RankSection, "오늘의 순위");
            rows.DifficultyText = LookupString(DailyStrings.RankPending, "순위 집계 중");
            rows.DifficultyIndex = 0;
            rows.ShowUnlockBanner = arg.Result == BattleResult.Win && arg.DailyFirstWinBonus;
            rows.UnlockBannerText = string.Format(LookupString(DailyStrings.FirstWinBanner, "데일리 첫 승리 +{0}"), arg.DailyBonusSouls)
                + "  " + string.Format(LookupString(DailyStrings.StreakMultiplier, "연속 {0}일 x{1}"), arg.DailyStreak, DailyRules.FormatMultiplier(arg.DailyStreakMultiplier));
            int left = System.Math.Max(0, arg.DailyAttemptsLeft);
            rows.RetryLabelText = string.Format(LookupString(DailyStrings.RetryLeft, "다시 도전 (남은 {0})"), left);
            rows.RetryInteractable = left > 0;
            return rows;
        }

        //# 데일리 순위 비동기 갱신 — 제출·조회가 끝나면 호출부가 "{n}위" / "순위를 불러오지 못했습니다" 로 덮는다.
        public void ShowDailyRank(string text)
        {
            if (_isDaily == false)
                return;
            SetText(_difficultyText, text);
        }

        //# 탑 변형 (infinite-tower §6.4) — 난이도 줄을 "도달 층"으로, 승리 배너를 "N층 돌파!"로, 승리 시 왼쪽 버튼을 "다음 층"으로. 유물 띠는 그대로.
        public static ResultRowsData WithTower(ResultRowsData rows, ResultPopupArg arg)
        {
            bool win = arg.Result == BattleResult.Win;
            string floorText = string.Format(LookupString(TowerStrings.FloorNumber, "{0}층"), arg.TowerFloor);
            string tag = string.Format(LookupString(TowerStrings.TitleFloor, "탑 {0}층"), arg.TowerFloor);
            rows.SubText = rows.SubText + " · " + tag;
            rows.ShowDifficultyRow = true;
            rows.DifficultyLabelText = LookupString(TowerStrings.ReachedFloor, "도달 층");
            rows.DifficultyText = floorText;
            rows.DifficultyIndex = 1;
            rows.ShowUnlockBanner = win;
            rows.UnlockBannerText = string.Format(LookupString(TowerStrings.FloorCleared, "{0}층 돌파!"), arg.TowerFloor);
            rows.ShowNewBadge = false;
            if (win)
            {
                rows.RetryLabelText = LookupString(TowerStrings.NextFloor, "다음 층");
            }
            return rows;
        }

        //# 유물 획득 띠 / 미획득 줄 확정값 — 유물 DB 미로드거나 id 를 못 찾으면 띠를 숨긴다(E-1).
        public static ResultRowsData WithRelic(ResultRowsData rows, ResultPopupArg arg)
        {
            RelicDatabase db = RelicDatabase.Current;
            RelicData data = db != null && arg.HasRelicDrop ? db.Get(arg.RelicId) : null;
            if (data != null)
            {
                RelicRarityRow rarityRow = db.RarityRow(data.Rarity);
                rows.ShowRelicBand = true;
                rows.RelicId = data.RelicId;
                rows.RelicNameText = LookupString(data.NameKey, data.RelicId);
                rows.RelicRarityText = rarityRow != null ? LookupString(rarityRow.nameKey, data.Rarity.ToString()) : data.Rarity.ToString();
                rows.RelicRarityColor = RarityColor(rarityRow);
                rows.RelicIsLegendary = data.Rarity == ERelicRarity.Legendary;
                rows.ShowRelicNewBadge = arg.RelicIsNew;
                rows.RelicStackText = arg.RelicIsNew ? string.Empty
                    : string.Format(LookupString(StrRelicStack, "중첩 +1 ({0} / {1})"), arg.RelicStackAfter, arg.RelicMaxStack);
            }
            if (db != null && arg.HasRelicShard && System.Enum.TryParse(arg.RelicShardRarity, out ERelicRarity shardRarity))
            {
                RelicRarityRow rarityRow = db.RarityRow(shardRarity);
                string rarityName = rarityRow != null ? LookupString(rarityRow.nameKey, shardRarity.ToString()) : shardRarity.ToString();
                rows.ShowRelicBand = true;
                rows.RelicIsShard = true;
                rows.RelicId = null;
                rows.RelicNameText = string.Format(LookupString(GrowthStrings.ShardGain, "유물 잔해 +{0}"), arg.RelicShardAmount);
                rows.RelicRarityText = rarityName;
                rows.RelicRarityColor = RarityColor(rarityRow);
                rows.RelicIsLegendary = false;
                rows.ShowRelicNewBadge = false;
                rows.RelicStackText = string.Format(LookupString(GrowthStrings.ShardConverted, "{0} 유물이 모두 최대 중첩이라 잔해로 바뀌었다"), rarityName);
            }
            bool win = arg.Result == BattleResult.Win;
            rows.ShowRelicMissRow = win && arg.HasRelicDrop == false && arg.HasRelicShard == false && arg.ShowNextChance;
            rows.RelicMissText = string.Format(LookupString(StrRelicMiss, "유물 없음 · 다음 승리 시 획득 확률 {0}%"), arg.NextWinChancePercent);
            return rows;
        }

        //# RelicRarity.colorHex → Color. 파싱 실패 시 회색.
        public static Color RarityColor(RelicRarityRow row)
        {
            if (row != null && ColorUtility.TryParseHtmlString(row.colorHex, out Color color))
                return color;
            return new Color32(0x9A, 0xA5, 0xBD, 0xFF);
        }

        //# 클리어 시간 표기 "m:ss.d" — 표기 규약은 ClearTimeFormat 단일 소유(음수 방어는 0 으로).
        public static string FormatClearTime(float seconds)
        {
            return ClearTimeFormat.WithTenths(Mathf.Max(0f, seconds));
        }

        //# Strings_Ko 조회 — provider 미준비(테스트)면 fallback.
        private static string LookupString(int id, string fallback)
        {
            string text = CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
            return string.IsNullOrEmpty(text) ? fallback : text;
        }

        private void ApplyRows(ResultRowsData rows)
        {
            SetText(_subText, rows.SubText);
            SetText(_difficultyLabelText, rows.DifficultyLabelText);
            SetText(_retryLabelText, rows.RetryLabelText);
            //# 풀 재사용 팝업 — 매 InitUI 마다 확정(데일리 소진 판의 비활성이 다음 판에 남지 않게).
            if (_retryButton != null)
            {
                _retryButton.Interactable = rows.RetryInteractable;
            }
            SetRow(_rowClear, _clearTimeText, rows.ShowClearRow, rows.ClearTimeText);
            SetRow(_rowSouls, _soulsText, rows.ShowSoulsRow, rows.SoulsText);
            SetText(_soulsLabelText, rows.SoulsLabelText);
            ApplyRewardChips(rows);
            SetRow(_rowXp, _xpText, rows.ShowXpRow, rows.XpText);
            SetRow(_rowHeroHp, _heroHpText, rows.ShowHeroHpRow, rows.HeroHpText);
            SetRow(_rowDifficulty, _difficultyText, rows.ShowDifficultyRow, rows.DifficultyText);
            if (rows.ShowDifficultyRow && _difficultyText != null)
            {
                _difficultyText.SetColor(UiDotPalette.DifficultyColor(rows.DifficultyIndex));
            }
            SetRow(_unlockBanner, _unlockBannerText, rows.ShowUnlockBanner, rows.UnlockBannerText);
            if (_newBadge != null)
            {
                _newBadge.SetActive(rows.ShowNewBadge);
            }
            SetRow(_rowRelicMiss, _relicMissText, rows.ShowRelicMissRow, rows.RelicMissText);
            ApplyRelicBand(rows);
        }

        //# 재화 칩 (§8.4) — 위젯이 배선돼 있고 특수 재화 획득이 있으면 칩, 아니면 기존 텍스트.
        private void ApplyRewardChips(ResultRowsData rows)
        {
            bool useChips = rows.ShowSoulsRow && rows.RewardChips != null && _rewardChips.Length > 0;
            if (_soulsText != null)
            {
                _soulsText.gameObject.SetActive(useChips == false);
            }
            for (int i = 0; i < _rewardChips.Length; ++i)
            {
                if (_rewardChips[i] == null)
                    continue;
                CurrencyPillData chip = useChips && i < rows.RewardChips.Count ? rows.RewardChips[i] : null;
                if (chip != null)
                {
                    Sprite own = CurrencyIcon(chip.Currency);
                    chip.Icon = own != null ? own : _soulIcon;
                    chip.TintIcon = own == null && chip.Currency != ECurrency.Soul;
                }
                _rewardChips[i].Bind(chip);
            }
        }

        private Sprite CurrencyIcon(ECurrency currency) => currency switch
        {
            ECurrency.Soul         => _soulIcon,
            ECurrency.AwakenStone  => _awakenStoneIcon,
            ECurrency.MoonSeal     => _moonSealIcon,
            ECurrency.AbyssCrystal => _abyssCrystalIcon,
            _                      => null,
        };

        //# 유물 띠 — 표시 후 슬라이드 인 + 아이콘 팝 연출(EndBattle 후 timeScale 은 1 이지만 연출은 unscaled 시간 기준).
        private void ApplyRelicBand(ResultRowsData rows)
        {
            if (_relicIntro != null)
            {
                StopCoroutine(_relicIntro);
                _relicIntro = null;
            }
            _relicIntroPending = false;
            if (_relicBand == null)
                return;
            if (_relicBandRestCaptured == false)
            {
                _relicBandRest = _relicBand.anchoredPosition;
                _relicBandRestCaptured = true;
            }
            _relicBand.anchoredPosition = _relicBandRest;
            _relicBand.gameObject.SetActive(rows.ShowRelicBand);
            if (rows.ShowRelicBand == false)
                return;

            SetText(_relicNameText, rows.RelicNameText);
            if (_relicNameText != null)
            {
                _relicNameText.SetColor(rows.RelicIsShard ? ShardColor : rows.RelicRarityColor);
            }
            SetText(_relicRarityText, rows.RelicRarityText);
            if (_relicRarityText != null)
            {
                _relicRarityText.SetColor(rows.RelicRarityColor);
            }
            if (_relicRarityChip != null)
            {
                _relicRarityChip.color = rows.RelicRarityColor;
            }
            if (_relicBandBorder != null)
            {
                _relicBandBorder.color = rows.RelicRarityColor;
            }
            if (_relicNewBadge != null)
            {
                _relicNewBadge.SetActive(rows.ShowRelicNewBadge);
            }
            if (_relicStackText != null)
            {
                _relicStackText.gameObject.SetActive(rows.ShowRelicNewBadge == false);
                _relicStackText.SetText(rows.RelicStackText);
            }
            if (_relicGlow != null)
            {
                _relicGlow.SetActive(rows.RelicIsLegendary);
            }
            if (_relicIcon != null)
            {
                _relicIcon.sprite = null;
                _relicIcon.enabled = false;
                _relicIcon.color = Color.white;
                if (rows.RelicIsShard)
                {
                    _relicIcon.sprite = _shardIcon;
                    _relicIcon.enabled = _shardIcon != null;
                    _relicIcon.color = _shardIconTint ? ShardColor : Color.white;
                }
                else
                {
                    LoadRelicIcon(rows.RelicId);
                }
            }
            _relicIntroLegendary = rows.RelicIsLegendary;
            _relicIntroColor = rows.RelicRarityColor;
            _relicIntroPending = true;
            if (isActiveAndEnabled)
            {
                StartRelicIntro();
            }
        }

        private void OnEnable()
        {
            if (_relicIntroPending)
            {
                StartRelicIntro();
            }
        }

        private void OnDisable()
        {
            _relicIntro = null;
        }

        //# 획득 SFX 1회 + 슬라이드 인/아이콘 팝/전설 펄스 개시.
        private void StartRelicIntro()
        {
            _relicIntroPending = false;
            CHMSound.Instance?.Play(EAudio.RelicAcquire);
            _relicIntro = StartCoroutine(PlayRelicIntro(_relicIntroLegendary, _relicIntroColor));
        }

        //# 아이콘 — ERelic 값명 = 스프라이트 파일명(Rule 03 §2). 값명이 enum 에 없으면 아이콘만 비운다.
        private void LoadRelicIcon(string relicId)
        {
            if (System.Enum.TryParse(relicId, out ERelic key) == false || System.Enum.IsDefined(typeof(ERelic), key) == false)
                return;
            CHMResource.Instance.Load<Sprite>(key, sprite =>
            {
                if (this == null || _relicIcon == null || sprite == null)
                    return;
                _relicIcon.sprite = sprite;
                _relicIcon.enabled = true;
            });
        }

        private IEnumerator PlayRelicIntro(bool legendary, Color rarityColor)
        {
            RectTransform pop = _relicIconFrame;
            if (pop != null)
            {
                pop.localScale = Vector3.one * 0.6f;
            }
            _relicBand.anchoredPosition = _relicBandRest + new Vector2(0f, -RelicBandSlideDistance);

            float t = 0f;
            while (t < RelicBandSlideSec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / RelicBandSlideSec);
                _relicBand.anchoredPosition = _relicBandRest + new Vector2(0f, -RelicBandSlideDistance * (1f - k));
                yield return null;
            }
            _relicBand.anchoredPosition = _relicBandRest;

            //# 아이콘 팝 0.6 -> 1.15 -> 1.0
            t = 0f;
            while (t < RelicIconPopSec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / RelicIconPopSec);
                float scale = k < 0.6f ? Mathf.Lerp(0.6f, 1.15f, k / 0.6f) : Mathf.Lerp(1.15f, 1f, (k - 0.6f) / 0.4f);
                if (pop != null)
                {
                    pop.localScale = Vector3.one * scale;
                }
                yield return null;
            }
            if (pop != null)
            {
                pop.localScale = Vector3.one;
            }

            //# 전설 — 띠 테두리 펄스 2회(색 -> 흰색 -> 색).
            if (legendary && _relicBandBorder != null)
            {
                for (int pulse = 0; pulse < 2; ++pulse)
                {
                    t = 0f;
                    while (t < 0.4f)
                    {
                        t += Time.unscaledDeltaTime;
                        float wave = Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI);
                        _relicBandBorder.color = Color.Lerp(rarityColor, Color.white, wave * 0.6f);
                        yield return null;
                    }
                }
                _relicBandBorder.color = rarityColor;
            }
            _relicIntro = null;
        }

        private static void SetDecorActive(GameObject[] decor, bool active)
        {
            if (decor == null)
                return;

            for (int i = 0; i < decor.Length; i++)
            {
                if (decor[i] != null)
                {
                    decor[i].SetActive(active);
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

        private static void SetRow(GameObject row, CHText label, bool show, string text)
        {
            if (row != null)
            {
                row.SetActive(show);
            }
            if (show)
            {
                SetText(label, text);
            }
        }

        //# 연타/중복 클릭 가드 — LoadScene 은 다음 프레임 적용이라 그 사이 재클릭이 중복 로드를 일으킨다. 첫 요청만 통과.
        public bool TryBeginSceneLoad()
        {
            if (_sceneLoadRequested)
                return false;

            _sceneLoadRequested = true;
            return true;
        }

        //# 보상 블록 — 줄 포맷은 기획서 §9.2 표가 단일 진실.
        public static string BuildRewardText(ResultPopupArg arg)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"보상  소울 +{arg.SoulsGained} · XP +{arg.XpGained}");

            //# 영주 줄 — 생략 키는 LordLevel == 0. 레벨 업 + 보상 0 (잠금 더미 구간) 이면 "+N 소울" 부분만 생략.
            if (arg.LordLevel != 0)
            {
                sb.Append($"\n영주 레벨 업!  Lv {arg.LordLevel}");
                if (arg.LordRewardSouls > 0)
                {
                    sb.Append($"  +{arg.LordRewardSouls} 소울");
                }
            }

            //# 도전과제 — 달성 건당 1줄, 최대 3줄 + 초과분 "외 N건 달성".
            if (arg.NewlyAchieved != null)
            {
                int shown = 0;
                foreach (AchievementDef def in arg.NewlyAchieved)
                {
                    if (def == null)
                        continue;
                    if (shown >= MaxAchievedLines)
                        break;
                    sb.Append($"\n도전과제 달성!  {def.DisplayName}  +{def.RewardSouls} 소울");
                    shown++;
                }
                int extra = CountNonNull(arg.NewlyAchieved) - shown;
                if (extra > 0)
                {
                    sb.Append($"\n외 {extra}건 달성");
                }
            }
            return sb.ToString();
        }

        private static int CountNonNull(List<AchievementDef> list)
        {
            int count = 0;
            foreach (AchievementDef def in list)
            {
                if (def != null)
                {
                    count++;
                }
            }
            return count;
        }

        private void OnClickRetry()
        {
            if (TryBeginSceneLoad() == false)
                return;

            //# 데일리 — 날짜·남은 도전 판정 후 같은 조합으로 재시작(호출부 소유). 씬 이동을 안 했으면 가드를 풀어 버튼을 살린다.
            if (_onDailyRetry != null)
            {
                if (_onDailyRetry() == false)
                {
                    _sceneLoadRequested = false;
                }
                return;
            }

            //# 마을 미경유 즉시 재도전 — MetaSession 은 static 홀더라 프로필이 메모리에 유지된다 (Battle Start 의 메타 보너스 동일 적용).
            SceneManager.LoadScene(EScene.Battle.ToString());
        }

        private void OnClickToVillage()
        {
            if (TryBeginSceneLoad() == false)
                return;

            //# Rule 08 — EScene.Village.ToString() == "Village" 씬 파일명과 일치 (기획서 §9.2).
            SceneManager.LoadScene(EScene.Village.ToString());
        }
    }
}
