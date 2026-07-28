using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 UIBase 와 같은 파일.
    public class RankingPopupArg : UIArg
    {
        public RankingClient Ranking;
        //# "내 행" 최우선 식별 키(2026-07-14 Firebase 피벗) — 양쪽 uid 존재 시 이걸로만 매칭.
        public string MyUid;
        public float MyBestClearTime;   //# uid 미식별 시 fallback(초). 없으면 -1(MetaProfile.BestClearTime).
    }

    //# 최단클리어 랭킹 조회 — Top N + 내 순위. 통신 실패 시 빈 목록 + 안내(기획서 §4).
    public class RankingPopup : UIBase
    {
        [SerializeField] private RankingPoolingScrollView _scrollView;
        [SerializeField] private CHText _emptyText;   //# 빈 목록/실패/오프라인 안내

        public override void InitUI(UIArg arg)
        {
            if (arg is RankingPopupArg rankArg)
                Load(rankArg);
        }

        private async void Load(RankingPopupArg arg)
        {
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(false);

            if (arg.Ranking == null)
            {
                ShowEmpty("오프라인 — 랭킹을 불러올 수 없습니다.");
                return;
            }

            List<RankingRowDto> top = await arg.Ranking.GetTopAsync(100);
            if (top == null || top.Count == 0)
            {
                ShowEmpty("아직 기록이 없습니다. 첫 클리어의 주인공이 되어 보세요.");
                return;
            }

            //# Top 100 행 매핑 — "내 행" 표시(uid 1차 → BestClearTime 시간 fallback).
            List<RankingRowEntry> entries = new List<RankingRowEntry>();
            bool foundMineInTop = false;
            string myUid = arg.MyUid;
            int myClearMs = arg.MyBestClearTime > 0f ? Mathf.RoundToInt(arg.MyBestClearTime * 1000f) : -1;
            foreach (RankingRowDto row in top)
            {
                bool isMine = IsMyRow(row, myUid, myClearMs, foundMineInTop);
                if (isMine)
                    foundMineInTop = true;
                entries.Add(new RankingRowEntry { Row = row, IsMine = isMine });
            }

            //# Top 100 밖이면 내 순위 행을 맨 아래에 붙여 항상 "내가 몇 등인지" 보이게(기획서 §4).
            if (foundMineInTop == false)
            {
                List<RankingRowDto> mine = await arg.Ranking.GetMyRankAsync();
                RankingRowDto myRow = PickMyRow(mine, myUid, myClearMs);
                if (myRow != null)
                    entries.Add(new RankingRowEntry { Row = myRow, IsMine = true });
            }

            _scrollView.SetItemList(entries);
        }

        //# "내 행" 식별 — uid 1차(양쪽 존재 시 권위 키, 유일 매칭). 동률 시 첫 매칭만(중복 강조 방지).
        //# uid 미식별(내 uid 없음 또는 행에 uid 없음)이면 clearTimeMs 시간 폴백.
        private static bool IsMyRow(RankingRowDto row, string myUid, int myClearMs, bool alreadyFound)
        {
            if (alreadyFound || row == null)
                return false;
            if (string.IsNullOrEmpty(myUid) == false && string.IsNullOrEmpty(row.uid) == false)
                return row.uid == myUid;
            if (myClearMs < 0)
                return false;
            return row.clearTimeMs == myClearMs;
        }

        //# 내 순위 응답에서 내 행 1개 선택 — uid 일치 우선, 없으면 시간 일치, 그래도 없으면 첫 행.
        private static RankingRowDto PickMyRow(List<RankingRowDto> rows, string myUid, int myClearMs)
        {
            if (rows == null || rows.Count == 0)
                return null;
            if (string.IsNullOrEmpty(myUid) == false)
            {
                foreach (RankingRowDto row in rows)
                {
                    if (row != null && string.IsNullOrEmpty(row.uid) == false && row.uid == myUid)
                        return row;
                }
            }
            if (myClearMs >= 0)
            {
                foreach (RankingRowDto row in rows)
                {
                    if (row != null && row.clearTimeMs == myClearMs)
                        return row;
                }
            }
            return rows[0];
        }

        private void ShowEmpty(string message)
        {
            _scrollView.SetItemList(new List<RankingRowEntry>());
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(true);
                _emptyText.SetText(message);
            }
        }
    }
}
