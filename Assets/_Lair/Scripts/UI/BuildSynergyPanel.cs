using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BattleHud 좌측 빌드 시너지 패널 (롤토체스 스타일).
    //# Rule 11 v0.8 — BuildModalPopup 패턴: Panel 은 단순 컨테이너, ScrollView 는 별도 컴포넌트.
    //# prefab 안에 BuildSynergyCardPoolingScrollView 자식으로 박혀 있고 인스펙터에서 본 필드에 드래그.
    public class BuildSynergyPanel : MonoBehaviour
    {
        [SerializeField] private BuildSynergyCardPoolingScrollView _scrollView;

        //# 4축 키 색 (기획서 §2 / §11.4). MonsterTag 색과 동일.
        public static readonly Dictionary<EBuildAxis, Color> AxisColor = new()
        {
            { EBuildAxis.Tank,   new Color32(0x22, 0xC5, 0x5E, 0xFF) },
            { EBuildAxis.Dps,    new Color32(0xEF, 0x44, 0x44, 0xFF) },
            { EBuildAxis.Debuff, new Color32(0xA8, 0x55, 0xF7, 0xFF) },
            { EBuildAxis.Swarm,  new Color32(0x1F, 0x29, 0x37, 0xFF) },
        };

        //# 축 이름 (UI 표시용 — Enum 명 그대로, MVP §8 비주얼).
        public static readonly Dictionary<EBuildAxis, string> AxisLabel = new()
        {
            { EBuildAxis.Tank,   "TANK"   },
            { EBuildAxis.Dps,    "DPS"    },
            { EBuildAxis.Debuff, "DEBUFF" },
            { EBuildAxis.Swarm,  "SWARM"  },
        };

        //# 임계 단계 — 기획서 §4.1 (3/5/7장).
        private static readonly int[] Thresholds = { 3, 5, 7 };

        private static readonly EBuildAxis[] AllAxes =
            { EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm };

        private BattleViewModel _vm;
        private readonly List<BuildSynergyCellData> _dataList = new();
        private readonly Dictionary<EBuildAxis, int> _prevCounts = new();

        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            if (_vm == null) return;
            _vm.OnBuildChanged += HandleBuildChanged;
            HandleBuildChanged();
        }

        public void Unbind()
        {
            if (_vm == null) return;
            _vm.OnBuildChanged -= HandleBuildChanged;
            _vm = null;
        }

        private void HandleBuildChanged()
        {
            if (_vm == null || _scrollView == null) return;
            _dataList.Clear();
            foreach (EBuildAxis axis in AllAxes)
            {
                int count = _vm.GetBuildCount(axis);
                _dataList.Add(new BuildSynergyCellData
                {
                    Axis          = axis,
                    Color         = AxisColor[axis],
                    Label         = AxisLabel[axis],
                    Count         = count,
                    NextThreshold = NextThreshold(count),
                    ActiveTier    = ActiveTier(count),
                    JustCrossed   = CrossedThreshold(_prevCounts.GetValueOrDefault(axis, 0), count),
                });
                _prevCounts[axis] = count;
            }
            _scrollView.SetItemList(_dataList);
        }

        //# 새 임계를 넘었는지 — prev < T <= count 인 T 존재.
        private static bool CrossedThreshold(int prev, int count)
        {
            foreach (int t in Thresholds)
                if (prev < t && count >= t) return true;
            return false;
        }

        //# 다음 임계 — count 이상 첫 임계. 7+ 이면 -1 (다음 없음).
        private static int NextThreshold(int count)
        {
            foreach (int t in Thresholds)
                if (count < t) return t;
            return -1;
        }

        //# 현재 활성 Tier (1·2·3). 임계 미달이면 0.
        private static int ActiveTier(int count)
        {
            int tier = 0;
            foreach (int t in Thresholds)
                if (count >= t) ++tier;
            return tier;
        }
    }

    //# CHPoolingScrollView 의 TData — 1축 셀에 푸시되는 데이터.
    public class BuildSynergyCellData
    {
        public EBuildAxis Axis;
        public Color Color;
        public string Label;
        public int Count;
        public int NextThreshold;   //# -1 이면 7+ 도달
        public int ActiveTier;      //# 0·1·2·3
        public bool JustCrossed;    //# 이번 갱신에서 임계를 새로 넘은 경우 펄스
    }
}
