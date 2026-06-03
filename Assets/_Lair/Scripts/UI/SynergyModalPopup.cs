using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SynergyModalPopupArg : UIArg
    {
        public BattleViewModel ViewModel;
    }

    //# 화면 중앙 모달 — 적용된(임계 도달) 시너지 효과 목록. BuildModalPopup 패턴.
    public partial class SynergyModalPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;                            //# 전체 화면 dim (#000 α0.6) — 클릭 시 닫힘
        [SerializeField] private CHButton _closeButton;                          //# 우상단 X
        [SerializeField] private SynergyModalCardPoolingScrollView _scrollView;  //# 단일 세로 시너지 리스트
        [SerializeField] private CHText _emptyText;                              //# 활성 티어 0개일 때만 활성

        //# 4축 아이콘 — 직접 Sprite 참조 (BuildSynergyPanel 관례 동형, Addressables 키 아님).
        //# 인스펙터에서 SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png 연결. AxisIcon 으로 매핑.
        [SerializeField] private Sprite _tankIcon;
        [SerializeField] private Sprite _dpsIcon;
        [SerializeField] private Sprite _debuffIcon;
        [SerializeField] private Sprite _swarmIcon;

        //# OnEnable / OnBuildChanged 양쪽이 참조하므로 필드로 보관.
        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            if (arg is SynergyModalPopupArg ma && ma.ViewModel != null)
            {
                _vm = ma.ViewModel;
                //# 팝업 열린 동안 카드 픽 시 자동 갱신.
                System.Action refresh = HandleBuildChanged;
                _vm.OnBuildChanged += refresh;
                BattleViewModel vmRef = _vm;
                closeDisposable.Add(() => vmRef.OnBuildChanged -= refresh);
                closeDisposable.Add(() => _vm = null);
            }

            //# 배경 dim 클릭 / X 버튼 클릭 → 닫힘.
            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);

            //# prefab 이 active 상태로 저장된 경우 보강 (BuildModalPopup 동일).
            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        //# 활성화 직후 layout 산정 + Build. 재오픈(cached UI 재사용) 시 누적 픽 반영.
        private void OnEnable()
        {
            if (_vm == null)
                return;
            BuildAndLayout();
        }

        //# 모달 루트 강제 산정 후 Build. InitUI(prefab active)·OnEnable 양쪽에서 호출.
        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Build();
        }

        private void HandleBuildChanged()
        {
            if (_vm == null)
                return;
            Build();
        }

        private void Build()
        {
            if (_vm == null)
                return;
            List<SynergyModalCellData> rows = BuildRows(_vm.GetBuildCount, AxisIcon);

            //# 활성 티어 0개면 빈 상태 라벨, 그 외엔 리스트.
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(rows.Count == 0);
            if (_scrollView != null)
                _scrollView.SetItemList(rows);
        }

        //# 축별 아이콘 — 인스펙터 직접 참조 4개를 EBuildAxis 로 매핑 (BuildSynergyPanel.AxisIcon 동형).
        //# 미할당이면 null (헤더 셀이 아이콘 숨김).
        private Sprite AxisIcon(EBuildAxis axis)
        {
            if (axis == EBuildAxis.Tank)
                return _tankIcon;
            if (axis == EBuildAxis.Dps)
                return _dpsIcon;
            if (axis == EBuildAxis.Debuff)
                return _debuffIcon;
            if (axis == EBuildAxis.Swarm)
                return _swarmIcon;
            return null;
        }
    }

    //# BuildRows 순수 로직 — VM/Prefab 없이 EditMode 검증 (Task 2).
    public partial class SynergyModalPopup
    {
        private static readonly int[] Thresholds = { 3, 5, 7 };

        private static readonly EBuildAxis[] AllAxes =
            { EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm };

        //# 기획서 §2 마스터 표. (축, 티어) → 효과 설명.
        private static readonly Dictionary<(EBuildAxis, int), string> TierDesc = new()
        {
            { (EBuildAxis.Tank,   1), "Wisp·Wraith HP ×1.3" },
            { (EBuildAxis.Tank,   2), "Wisp·Wraith Power ×1.2" },
            { (EBuildAxis.Tank,   3), "필드 캡 +6 (18→24)" },
            { (EBuildAxis.Dps,    1), "Reaper·Hex Power ×1.3" },
            { (EBuildAxis.Dps,    2), "Reaper·Hex 공속 +25%" },
            { (EBuildAxis.Dps,    3), "Reaper·Hex Range ×1.3" },
            { (EBuildAxis.Debuff, 1), "Plague 둔화 ×0.8" },
            { (EBuildAxis.Debuff, 2), "영웅 공격력 ×0.85" },
            { (EBuildAxis.Debuff, 3), "출혈 영구 — 이동 시 1s당 HP -1%" },
            { (EBuildAxis.Swarm,  1), "Phantom·Wisp 이동속도 ×1.3" },
            { (EBuildAxis.Swarm,  2), "모든 스포너 주기 ×0.85" },
            { (EBuildAxis.Swarm,  3), "모든 스포너 동시 출력 +1" },
        };

        //# 활성 티어 수 (count 가 넘은 임계 개수).
        private static int ActiveTier(int count)
        {
            int tier = 0;
            foreach (int t in Thresholds)
                if (count >= t) ++tier;
            return tier;
        }

        //# 축 카운트 조회 함수를 받아 행 리스트로 평탄화. 활성 티어 0 인 축은 스킵.
        //# 축 순서(AllAxes) × 티어 1→ActiveTier. 각 축 헤더 1행 + 효과 N행.
        //# iconOf 는 optional — 미지정(null)이면 헤더 Icon=null (기존 1-arg 호출부 무회귀, 기획서 §8).
        public static List<SynergyModalCellData> BuildRows(Func<EBuildAxis, int> countOf,
            Func<EBuildAxis, Sprite> iconOf = null)
        {
            List<SynergyModalCellData> rows = new List<SynergyModalCellData>();
            if (countOf == null)
                return rows;
            foreach (EBuildAxis axis in AllAxes)
            {
                int count = countOf(axis);
                int tiers = ActiveTier(count);
                if (tiers <= 0)
                    continue;

                Color color = BuildSynergyPanel.AxisColor[axis];
                rows.Add(new SynergyModalCellData
                {
                    RowKind   = SynergyModalCellData.Kind.Header,
                    AxisColor = color,
                    Label     = $"{BuildSynergyPanel.AxisLabel[axis]} ({count}장)",
                    Icon      = iconOf?.Invoke(axis),
                });
                for (int tier = 1; tier <= tiers; ++tier)
                {
                    string desc;
                    TierDesc.TryGetValue((axis, tier), out desc);
                    rows.Add(new SynergyModalCellData
                    {
                        RowKind   = SynergyModalCellData.Kind.Effect,
                        AxisColor = color,
                        Label     = $"Tier{tier}  {desc}",
                    });
                }
            }
            return rows;
        }
    }
}
