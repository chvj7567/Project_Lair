using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SpawnerStatusTooltipArg : UIArg
    {
        //# 어느 셀의 툴팁인지 — 헤더 "Spawner #N" 표시 + ViewModel.Spawners[N] 조회 키.
        public int SpawnerIndex;
        public BattleViewModel ViewModel;
        //# 셀 RectTransform — 툴팁이 그 위에 floating 으로 배치.
        public RectTransform AnchorCell;
        //# 툴팁이 닫힐 때 패널에 알려 활성 테두리 원복. 인자는 닫힌 셀 인덱스 — Panel 이
        //# stale 콜백(다른 셀로 전환되어 이전 disposable 이 강제 해제된 경우) 을 self-ignore 한다.
        public Action<int> OnClosed;
        //# 강화 줄 base 스탯의 단일 진실 (Rule 03 — 인터페이스 주입 우선).
        public BalanceConfig Balance;
    }

    //# 셀 위 floating 툴팁 — 헤더 + 강화 줄 리스트 (CHPoolingScrollView<BuffLine, AppliedBuff>).
    //# CHMUI.ShowUI(EUI.SpawnerStatusTooltip, arg) 로 띄움.
    //# v0.8 — Rule 11 완전 적용. 본문 CHText 단일 텍스트 → BuffPoolingScrollView 로 교체.
    //# FormatBuffLine 메서드는 BuffLine.cs 로 이주됨 — 본 클래스는 줄 수만큼 BuffLine 풀 인스턴스에 위임.
    public class SpawnerStatusTooltip : UIBase
    {
        [SerializeField] private RectTransform _root;                  //# 툴팁 본체 (가로 201px, padding 8, 위치 조정 대상)
        [SerializeField] private CHText _headerText;                   //# "Spawner #0 — Wisp ×2"
        [SerializeField] private BuffPoolingScrollView _buffScrollView; //# 강화 줄 리스트 (CHPoolingScrollView 외부에 헤더 고정)
        [SerializeField] private CHText _emptyText;                    //# "적용된 강화 없음" — buffs.Count==0 일 때만 SetActive(true)

        private SpawnerStatusTooltipArg _arg;
        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as SpawnerStatusTooltipArg;
            if (_arg == null) return;
            _vm = _arg.ViewModel;

            //# 동일 셀이라도 매번 InitUI 시 재구성 — 카드 픽 누적으로 강화 줄이 갱신될 수 있음.
            RefreshContent();
            PositionAboveAnchor();

            //# VM 갱신 구독 — 툴팁 열려있는 동안 출력 종 / count / 강화 픽 변경 시 자동 갱신.
            if (_vm != null)
            {
                _vm.OnSpawnerSnapshotChanged += HandleSnapshotChanged;
                closeDisposable.Add(() =>
                {
                    if (_vm != null) _vm.OnSpawnerSnapshotChanged -= HandleSnapshotChanged;
                });
            }

            //# 패널이 활성 테두리 원복 가능하도록 Close 시 콜백.
            //# 닫히는 셀 인덱스를 캡처 — CHMUI.ShowUI 재호출 시 이전 disposable 이 동기적으로 Clear 되어
            //# 콜백이 발화되는데, 그 시점 _openCellIndex 가 이미 새 셀로 바뀌어 있을 수 있음.
            //# 인덱스를 전달해 Panel 이 stale 콜백을 self-ignore 한다 (advisor BLOCKER).
            if (_arg.OnClosed != null)
            {
                Action<int> onClosed = _arg.OnClosed;
                int closedIndex = _arg.SpawnerIndex;
                closeDisposable.Add(() => onClosed(closedIndex));
            }
        }

        private void HandleSnapshotChanged(int index)
        {
            if (_arg == null || index != _arg.SpawnerIndex) return;
            RefreshContent();
        }

        //# 툴팁 위치 — 셀 RectTransform 상단 + gap 8px (기획서 §2.5.2).
        //# Pivot (0.5, 0) 으로 하단 중앙이 셀 상단을 가리킴.
        //# 부모(_root.parent) 는 tooltip 의 full-stretch root → pivot (0.5, 0.5) 라 localInCanvas.y 가
        //# [-H/2, H/2] 범위. anchor 도 (0.5, 0.5) 로 두어 localInCanvas.y 그대로 anchoredPosition 으로 사용.
        private void PositionAboveAnchor()
        {
            if (_root == null || _arg == null || _arg.AnchorCell == null) return;

            //# 안전 — 캔버스 좌표계가 일치한다고 가정 (둘 다 BattleHud 의 동일 캔버스 하위).
            RectTransform canvasRt = _root.parent as RectTransform;
            if (canvasRt == null) return;

            //# 셀의 월드 위치를 캔버스 로컬로 변환해서 셀 상단 중앙을 구함.
            RectTransform anchor = _arg.AnchorCell;
            Vector3 cellTopWorld = anchor.TransformPoint(new Vector3(0f, anchor.rect.yMax, 0f));
            Vector3 localInCanvas = canvasRt.InverseTransformPoint(cellTopWorld);

            //# Pivot (0.5, 0) — 하단 중앙이 anchored 위치. Anchor 를 부모 중심(0.5, 0.5)으로 맞춰
            //# localInCanvas 좌표계와 일치시킨다 (advisor BLOCKER 1).
            _root.pivot = new Vector2(0.5f, 0f);
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);

            //# 화면 좌우 clamp — 툴팁 width 의 절반 만큼 안전 margin (기획서 §2.5.2 v0.8).
            //# v0.7 좌측 anchor 변경으로 셀 0 위 툴팁이 좌측 화면 벗어남 — 분기 자동 발동 (코드 변경 없음).
            float halfWidth = _root.rect.width * 0.5f;
            float canvasHalfWidth = canvasRt.rect.width * 0.5f;
            float safeMargin = 4f;
            float minX = -canvasHalfWidth + halfWidth + safeMargin;
            float maxX =  canvasHalfWidth - halfWidth - safeMargin;
            float clampedX = Mathf.Clamp(localInCanvas.x, minX, maxX);

            _root.anchoredPosition = new Vector2(clampedX, localInCanvas.y + 8f);
        }

        private void RefreshContent()
        {
            if (_arg == null || _vm == null) return;
            IReadOnlyList<BattleViewModel.SpawnerSnapshot> spawners = _vm.Spawners;
            if (spawners == null || _arg.SpawnerIndex < 0 || _arg.SpawnerIndex >= spawners.Count) return;
            BattleViewModel.SpawnerSnapshot snap = spawners[_arg.SpawnerIndex];
            if (snap == null) return;

            //# 헤더 — "Spawner #N — Wisp ×2" — 일관성 위해 1픽 케이스에도 ×count 표시 (기획서 §2.5.4).
            string speciesName = SpawnerStatusCell.SpeciesName(snap.CurrentType);
            if (_headerText != null)
                _headerText.SetText($"Spawner #{snap.Index} — {speciesName} ×{snap.OutputCount}");

            //# 강화 줄 — CHPoolingScrollView 가 자동 풀링·재바인딩.
            IReadOnlyList<BattleViewModel.AppliedBuff> buffs = snap.AppliedBuffs;
            int count = buffs != null ? buffs.Count : 0;

            if (_emptyText != null) _emptyText.gameObject.SetActive(count == 0);

            if (_buffScrollView != null)
            {
                //# CHPoolingScrollView 가 풀 인스턴스를 viewport 기준으로 자동 관리.
                //# Context (종 + balance) 를 SetItemList 전에 주입 — InitItem 안에서 BuffLine.Bind 가 그대로 사용.
                _buffScrollView.SetContext(snap.CurrentType, _arg.Balance);
                _buffScrollView.SetItemList(ToList(buffs));
            }
        }

        //# IReadOnlyList → List 복사 (CHPoolingScrollView.SetItemList 가 List<TData> 요구).
        private static List<BattleViewModel.AppliedBuff> ToList(IReadOnlyList<BattleViewModel.AppliedBuff> src)
        {
            List<BattleViewModel.AppliedBuff> list = new List<BattleViewModel.AppliedBuff>();
            if (src == null) return list;
            for (int i = 0; i < src.Count; ++i) list.Add(src[i]);
            return list;
        }
    }
}
