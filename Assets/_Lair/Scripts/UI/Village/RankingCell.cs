using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 셀에 넘기는 표시 단위 — 서버 DTO + "내 행" 여부(uid/시간 매칭은 Popup 이 판정, 기획서 §4·§8).
    public class RankingRowEntry
    {
        public RankingRowDto Row;
        public bool IsMine;
        //# 조회 성공 + 내 서버 행 없음 — 순위/시간 자리에 미등재 표기. 판정은 Popup 이 하고 셀은 표시만(Rule 02 §6).
        public bool Unranked;
        //# Unranked 일 때 표시할 내 닉네임 — 서버 행이 없어 DTO 가 없다.
        public string UnrankedName;
        //# 기록 칸 표기를 호출부가 확정한 값으로 — 데일리 점수("패배 · 영웅 HP 12% 남음")처럼 ms 포맷이 아닌 경우. 비면 기존 시간 포맷.
        public string ScoreText;
        //# 영웅 열 비우기 — 데일리는 모두 같은 영웅이라 표시하지 않는다(daily-challenge §9.2).
        public bool HideHero;
    }

    //# 랭킹 한 행 — 순위/이름/시간/영웅. 풀 재사용 셀(Rule 03 §3 CHText).
    public class RankingCell : MonoBehaviour
    {
        [SerializeField] private Image _background;     //# 내 행 강조 배경
        [SerializeField] private CHText _rankText;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _timeText;
        [SerializeField] private CHText _heroText;

        //# UI 리디자인 — 일반 행 Px_PanelDark / 내 행 Px_Panel + 소울 링. 미할당이면 기존 반투명 배경 방식.
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _mineSprite;
        [SerializeField] private Image _mineRing;

        //# 비주얼 폴리시 — 1~3위 메달 묶음(순위 1~3 에서만 활성) + 순위색 rgb 로 틴트할 Image(글로우·원판). 미할당이면 건너뜀.
        [SerializeField] private GameObject _rankMedalRoot;
        [SerializeField] private Image[] _rankMedalTinted;
        //# 1~3위 메달 전용 스프라이트 슬롯(인덱스 0=금,1=은,2=동). 미할당이면 위 틴트 방식 유지.
        [SerializeField] private Image _rankMedalImage;
        [SerializeField] private Sprite[] _rankMedalSprites;

        //# 순위 색 강조(기획서 §4) — 1/2/3위 금/은/동, 그 외 기본.
        private static readonly Color Gold = new Color(1f, 0.823f, 0.290f);       //# #FFD24A
        private static readonly Color Silver = new Color(0.788f, 0.804f, 0.823f); //# #C9CDD2
        private static readonly Color Bronze = new Color(0.816f, 0.545f, 0.357f); //# #D08B5B
        private static readonly Color RankDefault = Color.white;
        //# 내 행 배경 강조(반투명 청록 #2FB6A8 alpha 0.25) / 일반 행 투명.
        private static readonly Color MineBg = new Color(0.184f, 0.714f, 0.659f, 0.25f);
        private static readonly Color NormalBg = new Color(0f, 0f, 0f, 0f);
        //# 미등재 표기 — 시간/영웅은 "00:00" 같은 가짜 기록으로 보이면 안 되므로 빈 값 기호.
        //# 순위 칸은 83px/NoWrap/Ellipsis — 이 라벨(78px)보다 길어지면 경고 없이 "랭킹 없…" 로 잘린다.
        private const string UnrankedRankLabel = "랭킹 없음";
        private const string UnrankedValueLabel = "-";
        private const string MineSuffix = " (나)";

        //# 풀 재사용 리셋 — 이전 행의 강조 잔존 방지(Rule 03 §4).
        private void OnEnable()
        {
            ApplyBackground(false);
            if (_rankText != null)
            {
                _rankText.SetColor(RankDefault);
            }
            if (_rankMedalRoot != null)
            {
                _rankMedalRoot.SetActive(false);
            }
        }

        //# 1~3위만 메달을 켜고 순위색 rgb 로 틴트(각 Image 의 알파 보존). 그 외는 끈다.
        private void ApplyRankMedal(long rank)
        {
            bool podium = rank >= 1 && rank <= 3;
            if (_rankMedalRoot != null)
            {
                _rankMedalRoot.SetActive(podium);
            }
            if (podium == false)
                return;

            //# 순위별 스프라이트(0=금,1=은,2=동)가 있으면 교체하고 틴트 없이 원색 — 슬롯 비면 기존 동작.
            bool spriteApplied = false;
            if (_rankMedalImage != null && _rankMedalSprites != null && _rankMedalSprites.Length >= rank)
            {
                Sprite medal = _rankMedalSprites[rank - 1];
                if (medal != null)
                {
                    _rankMedalImage.sprite = medal;
                    _rankMedalImage.color = Color.white;
                    spriteApplied = true;
                }
            }
            if (_rankMedalTinted == null)
                return;

            Color rankColor = RankColor(rank);
            for (int i = 0; i < _rankMedalTinted.Length; i++)
            {
                Image tinted = _rankMedalTinted[i];
                if (tinted == null)
                    continue;
                if (spriteApplied && tinted == _rankMedalImage)
                    continue;
                tinted.color = new Color(rankColor.r, rankColor.g, rankColor.b, tinted.color.a);
            }
        }

        //# 배경 — 스프라이트 스킨이 있으면 색은 그대로 쓰고 내 행만 스프라이트/링을 바꾼다.
        private void ApplyBackground(bool mine)
        {
            if (_background != null)
            {
                if (_normalSprite != null)
                {
                    _background.sprite = mine && _mineSprite != null ? _mineSprite : _normalSprite;
                    _background.color = Color.white;
                }
                else
                {
                    _background.color = mine ? MineBg : NormalBg;
                }
            }
            if (_mineRing != null)
            {
                _mineRing.gameObject.SetActive(mine);
                _mineRing.color = UiDotPalette.Soul;
            }
        }

        public void Bind(RankingRowEntry entry)
        {
            if (entry == null)
                return;
            if (_rankMedalRoot != null)
            {
                _rankMedalRoot.SetActive(false);
            }
            if (entry.Unranked)
            {
                BindUnranked(entry);
                return;
            }
            if (entry.Row == null)
                return;
            RankingRowDto data = entry.Row;
            ApplyRankMedal(data.rank);

            if (_rankText != null)
            {
                _rankText.SetText(data.rank.ToString());
                _rankText.SetColor(RankColor(data.rank));
            }
            if (_nameText != null)
            {
                string suffix = entry.IsMine ? MineSuffix : string.Empty;
                _nameText.SetText(Truncate(data.displayName, 12) + suffix);
            }
            if (_timeText != null)
            {
                _timeText.SetText(string.IsNullOrEmpty(entry.ScoreText) ? FormatMs(data.clearTimeMs) : entry.ScoreText);
            }
            if (_heroText != null)
            {
                _heroText.SetText(entry.HideHero ? string.Empty : data.hero);
            }
            ApplyBackground(entry.IsMine);
        }

        //# 미등재 표시 — 정상 경로가 건드리는 위젯 전부를 덮는다(프리팹 authoring 잔상/가짜 기록 방지).
        private void BindUnranked(RankingRowEntry entry)
        {
            if (_rankText != null)
            {
                _rankText.SetText(UnrankedRankLabel);
                _rankText.SetColor(RankDefault);
            }
            if (_nameText != null)
            {
                string suffix = entry.IsMine ? MineSuffix : string.Empty;
                _nameText.SetText(Truncate(entry.UnrankedName, 12) + suffix);
            }
            if (_timeText != null)
            {
                _timeText.SetText(string.IsNullOrEmpty(entry.ScoreText) ? UnrankedValueLabel : entry.ScoreText);
            }
            if (_heroText != null)
            {
                _heroText.SetText(entry.HideHero ? string.Empty : UnrankedValueLabel);
            }
            ApplyBackground(entry.IsMine);
        }

        private static Color RankColor(long rank)
        {
            if (rank == 1)
                return Gold;
            if (rank == 2)
                return Silver;
            if (rank == 3)
                return Bronze;
            return RankDefault;
        }

        //# 12자 상한, 넘으면 말줄임(기획서 §4).
        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            if (text.Length <= max)
                return text;
            return text.Substring(0, max) + "…";
        }

        //# ms → m:ss.f 표기(기획서 §4 검산: 92500ms → 1:32.5).
        private static string FormatMs(int ms)
        {
            float sec = ms / 1000f;
            int m = (int)(sec / 60f);
            float s = sec - m * 60f;
            return $"{m}:{s:00.0}";
        }
    }
}
