using System;
using System.Collections;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BattleHud 좌측 빌드 시너지 패널 (롤토체스 스타일).
    //# 4축 (Tank/Dps/Debuff/Swarm) 행. 각 행 = 색 박스 + 축 이름 + 카운트 + Tier 마커.
    //# Bind 시 VM.OnBuildChanged 구독. 임계(3/5/7) 도달 시 0.3s 펄스 애니메이션.
    //# 자식 4 Cell 은 Awake 에서 동적 생성 — 사용자 환경 액션 0건.
    public class BuildSynergyPanel : MonoBehaviour
    {
        //# 4축 키 색 (기획서 §2 / §11.4). MonsterTag 색과 동일.
        private static readonly Dictionary<EBuildAxis, Color> AxisColor = new()
        {
            { EBuildAxis.Tank,   new Color32(0x22, 0xC5, 0x5E, 0xFF) }, //# #22C55E
            { EBuildAxis.Dps,    new Color32(0xEF, 0x44, 0x44, 0xFF) }, //# #EF4444
            { EBuildAxis.Debuff, new Color32(0xA8, 0x55, 0xF7, 0xFF) }, //# #A855F7
            { EBuildAxis.Swarm,  new Color32(0x1F, 0x29, 0x37, 0xFF) }, //# #1F2937
        };

        //# 축 이름 (UI 표시용 — Enum 명 그대로, MVP §8 비주얼).
        private static readonly Dictionary<EBuildAxis, string> AxisLabel = new()
        {
            { EBuildAxis.Tank,   "TANK"   },
            { EBuildAxis.Dps,    "DPS"    },
            { EBuildAxis.Debuff, "DEBUFF" },
            { EBuildAxis.Swarm,  "SWARM"  },
        };

        //# 임계 단계 — 기획서 §4.1 (3/5/7장).
        private static readonly int[] Thresholds = { 3, 5, 7 };

        private BattleViewModel _vm;
        private readonly Dictionary<EBuildAxis, BuildSynergyCell> _cells = new();

        //# 동적 자식 생성 — 자식 4 Cell. 호출자(BattleHud) 가 panel 의 RectTransform anchor 만 설정.
        private void Awake()
        {
            BuildCells();
        }

        private void BuildCells()
        {
            if (_cells.Count > 0) return;

            //# VerticalLayoutGroup — 자식 4 Cell 수직 배치, 간격 4px.
            VerticalLayoutGroup vlg = gameObject.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            //# 패널 자체 배경 (반투명 어두운 박스 — 가독성).
            Image bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);

            EBuildAxis[] axes = { EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm };
            foreach (EBuildAxis axis in axes)
            {
                BuildSynergyCell cell = BuildSynergyCell.Create(transform, axis, AxisColor[axis], AxisLabel[axis]);
                _cells[axis] = cell;
            }
        }

        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            if (_vm == null) return;
            _vm.OnBuildChanged += HandleBuildChanged;
            //# 초기 동기화.
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
            if (_vm == null) return;
            foreach (KeyValuePair<EBuildAxis, BuildSynergyCell> kv in _cells)
            {
                int count = _vm.GetBuildCount(kv.Key);
                int prev = kv.Value.Count;
                kv.Value.SetCount(count, NextThreshold(count), ActiveTier(count));
                //# 임계 도달 시 펄스 — prev 와 count 비교해 새 임계 넘은 경우만.
                if (CrossedThreshold(prev, count)) kv.Value.Pulse();
            }
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
}
