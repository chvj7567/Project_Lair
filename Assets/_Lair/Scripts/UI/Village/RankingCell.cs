using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 셀에 넘기는 표시 단위 — 서버 DTO + "내 행" 여부(accountId/시간 매칭은 Popup 이 판정, 기획서 §4·§8).
    public class RankingRowEntry
    {
        public RankingRowDto Row;
        public bool IsMine;
    }

    //# 랭킹 한 행 — 순위/이름/시간/영웅. 풀 재사용 셀(Rule 03 §3 CHText).
    public class RankingCell : MonoBehaviour
    {
        [SerializeField] private Image _background;     //# 내 행 강조 배경
        [SerializeField] private CHText _rankText;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _timeText;
        [SerializeField] private CHText _heroText;

        //# 순위 색 강조(기획서 §4) — 1/2/3위 금/은/동, 그 외 기본.
        private static readonly Color Gold = new Color(1f, 0.823f, 0.290f);       //# #FFD24A
        private static readonly Color Silver = new Color(0.788f, 0.804f, 0.823f); //# #C9CDD2
        private static readonly Color Bronze = new Color(0.816f, 0.545f, 0.357f); //# #D08B5B
        private static readonly Color RankDefault = Color.white;
        //# 내 행 배경 강조(반투명 청록 #2FB6A8 alpha 0.25) / 일반 행 투명.
        private static readonly Color MineBg = new Color(0.184f, 0.714f, 0.659f, 0.25f);
        private static readonly Color NormalBg = new Color(0f, 0f, 0f, 0f);

        //# 풀 재사용 리셋 — 이전 행의 강조 잔존 방지(Rule 03 §4).
        private void OnEnable()
        {
            if (_background != null)
                _background.color = NormalBg;
            if (_rankText != null)
                _rankText.SetColor(RankDefault);
        }

        public void Bind(RankingRowEntry entry)
        {
            if (entry == null || entry.Row == null)
                return;
            RankingRowDto data = entry.Row;

            if (_rankText != null)
            {
                _rankText.SetText(data.rank.ToString());
                _rankText.SetColor(RankColor(data.rank));
            }
            if (_nameText != null)
            {
                string suffix = entry.IsMine ? " (나)" : string.Empty;
                _nameText.SetText(Truncate(data.displayName, 12) + suffix);
            }
            if (_timeText != null)
                _timeText.SetText(FormatMs(data.clearTimeMs));
            if (_heroText != null)
                _heroText.SetText(data.hero);
            if (_background != null)
                _background.color = entry.IsMine ? MineBg : NormalBg;
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
