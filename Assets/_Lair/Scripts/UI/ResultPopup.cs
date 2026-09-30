using System.Collections.Generic;
using System.Text;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public string SoulsText;       //# "+180"
        public bool ShowXpRow;
        public string XpText;          //# "+60"
    }

    //# 결과 표시 + 보상 요약 + 「다시 도전」(Battle 재시작) / 「마을로」(Village 복귀) 2버튼. ChvjPackage UI 래퍼 사용 (Rule 11).
    public class ResultPopup : UIBase
    {
        //# 결과 팝업 도전과제 표기 상한 — 초과분은 "외 N건 달성" (기획서 §9.1).
        private const int MaxAchievedLines = 3;

        [SerializeField] private CHText _resultText;
        [SerializeField] private CHText _rewardText;
        //# UI 리디자인 항목 줄 — 위젯 연결은 프리팹 단계. 미할당이면 건너뛴다.
        [SerializeField] private CHText _subText;
        [SerializeField] private GameObject _rowClear;
        [SerializeField] private CHText _clearTimeText;
        [SerializeField] private GameObject _newBadge;
        [SerializeField] private GameObject _rowSouls;
        [SerializeField] private CHText _soulsText;
        [SerializeField] private GameObject _rowXp;
        [SerializeField] private CHText _xpText;
        [SerializeField] private GameObject _rowHeroHp;
        [SerializeField] private CHText _heroHpText;
        [SerializeField] private GameObject _rays;   //# 승리 방사광 — 승리에서만 표시
        [SerializeField] private CHButton _retryButton;
        [SerializeField] private CHButton _villageButton;

        private bool _sceneLoadRequested;

        public override void InitUI(UIArg arg)
        {
            //# 풀 재사용 리셋 (Rule 03 §4) — 가드가 남으면 재사용된 팝업의 버튼이 죽는다.
            _sceneLoadRequested = false;

            if (arg is ResultPopupArg rp)
            {
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
            return new ResultRowsData
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
            };
        }

        //# 클리어 시간 표기 "m:ss.d" — 표기 규약은 ClearTimeFormat 단일 소유(음수 방어는 0 으로).
        public static string FormatClearTime(float seconds)
        {
            return ClearTimeFormat.WithTenths(Mathf.Max(0f, seconds));
        }

        private void ApplyRows(ResultRowsData rows)
        {
            SetText(_subText, rows.SubText);
            SetRow(_rowClear, _clearTimeText, rows.ShowClearRow, rows.ClearTimeText);
            SetRow(_rowSouls, _soulsText, rows.ShowSoulsRow, rows.SoulsText);
            SetRow(_rowXp, _xpText, rows.ShowXpRow, rows.XpText);
            SetRow(_rowHeroHp, _heroHpText, rows.ShowHeroHpRow, rows.HeroHpText);
            if (_newBadge != null)
            {
                _newBadge.SetActive(rows.ShowNewBadge);
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
