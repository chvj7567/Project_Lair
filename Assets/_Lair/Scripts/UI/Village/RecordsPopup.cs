using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class RecordsPopupArg : UIArg
    {
        public MetaProfile Profile;
        public HeroStageVariantConfig VariantConfig;
    }

    //# 스테이지 한 행의 표시 확정값 — 셀은 계산하지 않는다(spec §6.2).
    public class RecordsStageCellData
    {
        public int Stage;
        public bool IsLocked;
        public bool IsSelected;
        public Sprite Portrait;
        public Color PortraitTint;
        public string StageText;        //# "STAGE 3"
        public string ThreatText;       //# "★★★☆☆"
        public string WinText;          //# "12승" (잠금이면 빈 문자열)
        public string RunRateText;      //# "20판 · 60%" (잠금이면 빈 문자열)
        public string BestText;         //# "최단 3:18" (잠금이면 빈 문자열)
        public string LockHintText;     //# "스테이지 2 클리어 필요" (해금이면 빈 문자열)
    }

    //# 전적 기록 — 상단 총계 4항목 + 스테이지 1~5 스크롤 리스트 (spec §6).
    public class RecordsPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _bodyText;                             //# 상단 총계
        [SerializeField] private RecordsStagePoolingScrollView _scrollView;

        //# 영웅 초상 — 인스펙터 직접 참조 (HeroSelectPopup 관례, Addressables 키 아님).
        //# 스켈레톤 1모델 재스킨이라 5스테이지가 같은 초상을 틴트만 달리해 공유한다.
        [SerializeField] private Sprite _knightPortrait;

        //# 잠금 행 어둠 비율 — 캐러셀/영웅 목록의 잠금 톤과 동일.
        public const float LockedDimRatio = 0.55f;

        private RecordsPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as RecordsPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() => _arg = null);
            }

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }

            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        //# prefab 이 inactive 로 저장된 경우 InitUI 시점은 layout 미산정 → 첫 조립은 OnEnable 이 담당.
        private void OnEnable()
        {
            if (_arg == null)
                return;
            BuildAndLayout();
        }

        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Rebuild();
        }

        private void Rebuild()
        {
            if (_arg == null)
                return;

            if (_bodyText != null)
            {
                _bodyText.SetText(BuildBody(_arg.Profile));
            }
            if (_scrollView != null)
            {
                _scrollView.SetItemList(BuildCellData(_arg.Profile, _arg.VariantConfig, _knightPortrait));
            }
        }

        //# 상단 총계 — 산식(BestClearTime 소스)은 유지, 표기는 스테이지 행과 동일한 m:ss.
        //# 총계는 스테이지 합과 어긋날 수 있으나 의도된 동작.
        public static string BuildBody(MetaProfile profile)
        {
            if (profile == null)
                return string.Empty;

            int winRate = profile.TotalRuns > 0
                ? Mathf.RoundToInt(profile.TotalWins * 100f / profile.TotalRuns)
                : 0;
            string bestClear = FormatClearTime(profile.BestClearTime);
            return $"총 출격  {profile.TotalRuns}\n승리  {profile.TotalWins}\n승률  {winRate}%\n최단 클리어  {bestClear}";
        }

        //# 스테이지 1~5 행 — 해금은 전적, 잠금은 해금 조건. profile null 이면 진행도 0, config null 이면 틴트 흰색 폴백.
        public static List<RecordsStageCellData> BuildCellData(
            MetaProfile profile, HeroStageVariantConfig variantConfig, Sprite portrait)
        {
            List<RecordsStageCellData> list = new List<RecordsStageCellData>();
            int cleared = profile != null ? profile.ClearedStage : 0;
            int selected = profile != null ? profile.SelectedStage : 0;

            for (int stage = 1; stage <= StageProgress.MaxStage; ++stage)
            {
                //# 해금 판정은 캐러셀과 같은 단일 소유 헬퍼.
                bool unlocked = StageProgress.IsUnlocked(stage, cleared);
                Color tint = variantConfig != null ? variantConfig.GetStage(stage).TintColor : Color.white;
                StageRecordEntry record = profile != null
                    ? profile.GetStageRecord(stage)
                    : new StageRecordEntry { Stage = stage };
                int rate = record.Runs > 0 ? Mathf.RoundToInt(record.Wins * 100f / record.Runs) : 0;

                list.Add(new RecordsStageCellData
                {
                    Stage = stage,
                    IsLocked = unlocked == false,
                    IsSelected = unlocked && stage == selected,
                    Portrait = portrait,
                    PortraitTint = unlocked ? tint : Color.Lerp(tint, Color.black, LockedDimRatio),
                    StageText = $"STAGE {stage}",
                    ThreatText = BuildThreat(stage),
                    WinText = unlocked ? $"{record.Wins}승" : string.Empty,
                    RunRateText = unlocked ? $"{record.Runs}판 · {rate}%" : string.Empty,
                    BestText = unlocked ? $"최단 {FormatClearTime(record.BestClearTime)}" : string.Empty,
                    LockHintText = unlocked ? string.Empty : $"스테이지 {stage - 1} 클리어 필요",
                });
            }
            return list;
        }

        //# 위협도 — 채운 별 N + 빈 별 (5-N). VillageHud.BuildThreat 과 같은 규약(저쪽은 private static,
        //# 여기선 테스트 대상이라 public). 두 곳이 어긋나면 표기가 갈리므로 규약 변경 시 함께 고친다.
        public static string BuildThreat(int stage)
        {
            int filled = Mathf.Clamp(stage, 0, StageProgress.MaxStage);
            return new string('★', filled) + new string('☆', StageProgress.MaxStage - filled);
        }

        //# 클리어타임 표기 — 기록 없음(-1)은 "-", 그 외 m:ss (초는 내림).
        public static string FormatClearTime(float seconds)
        {
            if (seconds < 0f)
                return "-";
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
