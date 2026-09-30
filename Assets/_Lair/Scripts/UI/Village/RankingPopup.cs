using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 UIBase 와 같은 파일.
    public class RankingPopupArg : UIArg
    {
        public RankingClient Ranking;
        //# "내 행" 최우선 식별 키(2026-07-14 Firebase 피벗) — 양쪽 uid 존재 시 이걸로만 매칭.
        public string MyUid;
        public float MyBestClearTime;   //# uid 미식별 시 fallback(초). 없으면 -1(MetaProfile.BestClearTime).
        //# 미등재("랭킹 없음") 표시용 내 닉네임 — HUD 와 같은 해석 경로(VillageViewModel.DisplayName)로 주입.
        public string MyDisplayName;
        //# 스테이지 탭(제안 2) — 첫 표시 탭. 0=최단 클리어(전체), 1~5=스테이지. 기본은 전체(기존 동작).
        public int InitialStage;
        //# 스테이지 탭의 "내 행" 시간 fallback(초) 조회 — uid 미식별 시 사용. null 이면 -1(없음) 취급.
        public Func<int, float> MyStageBestClearTime;
    }

    //# 최단클리어 랭킹 조회 — Top N + 내 순위. 통신 실패 시 빈 목록 + 안내(기획서 §4).
    public class RankingPopup : UIBase
    {
        [SerializeField] private RankingPoolingScrollView _scrollView;
        [SerializeField] private CHText _emptyText;   //# 빈 목록/실패/오프라인 안내
        [SerializeField] private GameObject _myRankContainer;   //# 스크롤 밖 고정 "내 순위" 영역
        [SerializeField] private RankingCell _myRankCell;       //# 고정 영역 셀(풀링 대상 아님)
        //# 스테이지 탭 — StageTab1~5 (인덱스 0 = 스테이지 1) + OverallTab. 위젯 연결은 프리팹 단계, 미할당이면 탭 없이 전체만.
        [SerializeField] private Toggle[] _stageTabs = new Toggle[0];
        [SerializeField] private Toggle _overallTab;

        //# 탭 전환 중 이전 조회 응답이 늦게 도착해 목록을 덮어쓰지 않도록 조회마다 올리는 세대 번호.
        private int _loadVersion;

        public static int ClampStage(int stage)
        {
            return Mathf.Clamp(stage, 0, StageLeaderboard.MaxStage);
        }

        public override void InitUI(UIArg arg)
        {
            if (arg is RankingPopupArg rankArg)
            {
                //# 탭 위젯이 없으면(프리팹 배선 전) 스테이지 탭으로 진입해 갇히지 않도록 전체 랭킹으로 연다.
                bool hasTabs = _overallTab != null && _stageTabs != null && _stageTabs.Length > 0;
                int stage = hasTabs ? ClampStage(rankArg.InitialStage) : 0;
                WireTabs(rankArg, stage);
                Load(rankArg, stage);
            }
        }

        //# 탭 배선 — 켜진 탭의 스테이지로 다시 조회. 초기 탭은 알림 없이 켠다(중복 조회 방지).
        private void WireTabs(RankingPopupArg arg, int initialStage)
        {
            WireTab(_overallTab, arg, 0, initialStage);
            for (int i = 0; i < _stageTabs.Length; ++i)
            {
                WireTab(_stageTabs[i], arg, i + 1, initialStage);
            }
        }

        private void WireTab(Toggle tab, RankingPopupArg arg, int stage, int initialStage)
        {
            if (tab == null)
                return;
            tab.SetIsOnWithoutNotify(stage == initialStage);
            UnityEngine.Events.UnityAction<bool> handler = isOn =>
            {
                if (isOn == false)
                    return;
                Load(arg, stage);
            };
            tab.onValueChanged.AddListener(handler);
            closeDisposable.Add(() =>
            {
                if (tab != null)
                {
                    tab.onValueChanged.RemoveListener(handler);
                }
            });
        }

        //# stage 0 = 전체 최단 클리어 랭킹, 1~5 = 스테이지 랭킹.
        private async void Load(RankingPopupArg arg, int stage)
        {
            int version = ++_loadVersion;
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(false);
            if (_myRankContainer != null)
                _myRankContainer.SetActive(false);

            if (arg.Ranking == null)
            {
                ShowEmpty("오프라인 — 랭킹을 불러올 수 없습니다.");
                return;
            }

            List<RankingRowDto> top = stage == 0
                ? await arg.Ranking.GetTopAsync(100)
                : await arg.Ranking.GetStageTopAsync(stage, 100);
            //# 팝업이 닫혔거나 그 사이 다른 탭으로 바뀌었으면 이 응답은 버린다.
            if (this == null || version != _loadVersion)
                return;
            if (top == null || top.Count == 0)
            {
                ShowEmpty(stage == 0
                    ? "아직 기록이 없습니다. 첫 클리어의 주인공이 되어 보세요."
                    : "이 스테이지의 기록이 아직 없습니다.");
                return;
            }

            //# Top 100 행 매핑 — "내 행" 표시(uid 1차 → BestClearTime 시간 fallback).
            List<RankingRowEntry> entries = new List<RankingRowEntry>();
            //# 목록에서 하이라이트한 내 행 — 고정 영역 폴백에 재사용. null 여부가 "이미 찾음" 판정을 겸한다.
            RankingRowDto mineInTop = null;
            string myUid = arg.MyUid;
            float myBest = stage == 0
                ? arg.MyBestClearTime
                : (arg.MyStageBestClearTime != null ? arg.MyStageBestClearTime(stage) : -1f);
            int myClearMs = myBest > 0f ? Mathf.RoundToInt(myBest * 1000f) : -1;
            foreach (RankingRowDto row in top)
            {
                bool isMine = IsMyRow(row, myUid, myClearMs, mineInTop != null);
                if (isMine)
                    mineInTop = row;
                entries.Add(new RankingRowEntry { Row = row, IsMine = isMine });
            }

            _scrollView.SetItemList(entries);

            //# 내 순위는 Top 100 안팎 상관없이 스크롤 밖 고정 영역에 항상 표시(사용자 결정).
            List<RankingRowDto> mine = stage == 0
                ? await arg.Ranking.GetMyRankAsync()
                : await arg.Ranking.GetMyStageRankAsync(stage);
            //# 왕복 중 팝업이 닫혀 Destroy 됐거나 다른 탭으로 바뀌었을 수 있다 — Unity 의 destroyed 체크(this == null) 후 위젯 접근 금지.
            if (this == null || version != _loadVersion)
                return;

            RankingRowDto myRow = PickMyRow(mine, myUid, myClearMs);
            if (_myRankContainer == null || _myRankCell == null)
                return;

            //# 여기까지 왔으면 Top 조회 성공 = 연결 확인 — 내 행이 없어도 "랭킹 없음"으로 표시한다.
            //# 오프라인/실패는 위 ShowEmpty 에서 컨테이너를 숨긴 채 종료하므로 이 경로에 오지 않는다.
            _myRankContainer.SetActive(true);
            _myRankCell.Bind(BuildMyEntry(myRow, mineInTop, arg.MyDisplayName));
        }

        //# 고정 "내 순위" 표시 데이터 — 내 순위 조회 행 우선, 없으면 Top 안의 내 행(조회 실패 시 목록과 모순 방지).
        //# 둘 다 없으면 미등재 표기 + 내 닉네임. Top 밖 유저의 조회 실패는 계약상 구분 불가라 남는다.
        private static RankingRowEntry BuildMyEntry(RankingRowDto myRow, RankingRowDto mineInTop, string myDisplayName)
        {
            RankingRowDto row = myRow ?? mineInTop;
            if (row != null)
                return new RankingRowEntry { Row = row, IsMine = true };
            return new RankingRowEntry { Unranked = true, UnrankedName = myDisplayName, IsMine = true };
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
            if (_myRankContainer != null)
                _myRankContainer.SetActive(false);
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(true);
                _emptyText.SetText(message);
            }
        }
    }
}
