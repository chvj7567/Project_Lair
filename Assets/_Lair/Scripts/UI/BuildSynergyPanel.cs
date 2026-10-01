using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BattleHud 좌측 빌드 시너지 패널 (롤토체스 스타일).
    //# Rule 11 v0.8 — BuildModalPopup 패턴: Panel 은 단순 컨테이너, ScrollView 는 별도 컴포넌트.
    public class BuildSynergyPanel : MonoBehaviour
    {
        [SerializeField] private BuildSynergyCardPoolingScrollView _scrollView;

        //# 패널 루트 클릭 → SynergyModalPopup 호출.
        [SerializeField] private CHButton _rootButton;

        //# 4축 아이콘 — 직접 Sprite 참조 (CardData._icon 과 동일 관례, Addressables 키 아님).
        //# 인스펙터 순서 = EBuildAxis 순서 (Tank·Dps·Debuff·Swarm). AxisIcon 으로 매핑.
        [SerializeField] private Sprite _tankIcon;
        [SerializeField] private Sprite _dpsIcon;
        [SerializeField] private Sprite _debuffIcon;
        [SerializeField] private Sprite _swarmIcon;

        //# 4축 키 색 (기획서 §2 / §11.4). MonsterTag 색과 동일.
        public static readonly Dictionary<EBuildAxis, Color> AxisColor = new()
        {
            { EBuildAxis.Tank,   new Color32(0x5A, 0xA9, 0xFF, 0xFF) },
            { EBuildAxis.Dps,    new Color32(0xFF, 0x6B, 0x5A, 0xFF) },
            { EBuildAxis.Debuff, new Color32(0xC0, 0x8B, 0xFF, 0xFF) },
            { EBuildAxis.Swarm,  new Color32(0x7B, 0xE3, 0x6A, 0xFF) },
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

        //# 펼친 셀·접힌 탭 점의 공통 순서(시안: TANK · SWARM · DPS · DEBUFF)
        private static readonly EBuildAxis[] AllAxes =
            { EBuildAxis.Tank, EBuildAxis.Swarm, EBuildAxis.Dps, EBuildAxis.Debuff };

        //# 접기/펼치기(scene-2d-conversion §4.10) — 본문·접힌 탭은 프리팹 정적 자식, 전환은 즉시.
        [SerializeField] private GameObject _body;
        [SerializeField] private GameObject _foldedTab;
        [SerializeField] private CHButton _collapseButton;
        [SerializeField] private CHButton _expandButton;
        //# 접힌 탭 축 점 4개 — AllAxes 순서
        [SerializeField] private Image[] _foldedDots;
        [SerializeField] private float _expandedHeight = 247f;
        [SerializeField] private float _collapsedHeight = 31f;

        private static readonly Color DotOff = new Color32(0x0B, 0x0E, 0x14, 0xFF);
        private HudLayoutViewModel _layout;
        private System.Action<bool> _layoutHandler;

        //# View 의도 API — 본문 ↔ 접힌 탭 즉시 전환. 앵커 위치(우상단)는 그대로, 높이만 바꿔 다른 패널이 움직이지 않는다.
        public void SetCollapsed(bool collapsed)
        {
            if (_body != null)
                _body.SetActive(collapsed == false);
            if (_foldedTab != null)
                _foldedTab.SetActive(collapsed);
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, collapsed ? _collapsedHeight : _expandedHeight);
            }
            if (_rootButton != null)
            {
                _rootButton.Interactable = collapsed == false;
            }
            if (collapsed == false)
            {
                HandleBuildChanged();
            }
        }

        //# 접힌 탭 축 점 — 활성 단계 ≥ 1 이면 축 색. 새 단계 도달(JustCrossed)이면 알파 펄스(R12).
        private void RefreshFoldedDots()
        {
            if (_foldedDots == null)
                return;
            for (int i = 0; i < _foldedDots.Length && i < _dataList.Count; i++)
            {
                if (_foldedDots[i] == null)
                    continue;
                BuildSynergyCellData d = _dataList[i];
                Color c = d.ActiveTier > 0 ? d.Color : DotOff;
                _foldedDots[i].color = c;
                if (d.JustCrossed && _foldedTab != null && _foldedTab.activeInHierarchy)
                {
                    StartCoroutine(PulseDot(_foldedDots[i], c));
                }
            }
        }

        private System.Collections.IEnumerator PulseDot(Image dot, Color baseColor)
        {
            const float duration = 0.3f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.5f, 1f, Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI));
                dot.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            dot.color = baseColor;
        }

        private BattleViewModel _vm;
        private readonly List<BuildSynergyCellData> _dataList = new();
        private readonly Dictionary<EBuildAxis, int> _prevCounts = new();
        //# 루트 버튼 listener 수명 관리.
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public void Bind(BattleViewModel vm, HudLayoutViewModel layout = null)
        {
            _vm = vm;
            if (_vm == null) return;
            _vm.OnBuildChanged += HandleBuildChanged;

            //# 접기/펼치기 — 버튼은 VM Toggle 만 호출, 표시는 이벤트로 SetCollapsed(Rule 02 §6)
            if (layout != null)
            {
                _layout = layout;
                _layoutHandler = SetCollapsed;
                layout.OnSynergyCollapsedChanged += _layoutHandler;
                if (_collapseButton != null)
                    _collapseButton.OnClick(layout.ToggleSynergy, _disposable);
                if (_expandButton != null)
                    _expandButton.OnClick(layout.ToggleSynergy, _disposable);
            }

            //# 루트 클릭 → SynergyModalPopup. CHMUI 가 단일 인스턴스 caching 으로 재사용.
            if (_rootButton != null)
            {
                _rootButton.OnClick(() =>
                {
                    if (_vm == null) return;
                    CHMUI.Instance.ShowUI(EUI.SynergyModalPopup,
                        new SynergyModalPopupArg { ViewModel = _vm });
                }, _disposable);
            }

            HandleBuildChanged();
            if (layout != null)
            {
                SetCollapsed(layout.IsSynergyCollapsed);
            }
        }

        public void Unbind()
        {
            if (_layout != null && _layoutHandler != null)
            {
                _layout.OnSynergyCollapsedChanged -= _layoutHandler;
            }
            _layout = null;
            _layoutHandler = null;
            //# _vm null 여부와 무관하게 루트 버튼 listener 는 항상 정리 (BuildPanel 동일 패턴).
            if (_vm != null)
            {
                _vm.OnBuildChanged -= HandleBuildChanged;
            }
            _vm = null;
            _disposable.Clear();
        }

        private void HandleBuildChanged()
        {
            if (_vm == null || _scrollView == null) return;
            _dataList.Clear();
            //# 접힌 동안은 스크롤뷰가 비활성이라 목록 갱신을 미루고(펼칠 때 재호출) 접힌 탭 점만 갱신
            bool collapsed = _body != null && _body.activeInHierarchy == false;
            foreach (EBuildAxis axis in AllAxes)
            {
                int count = _vm.GetBuildCount(axis);
                _dataList.Add(new BuildSynergyCellData
                {
                    Axis          = axis,
                    Color         = AxisColor[axis],
                    Label         = AxisLabel[axis],
                    Icon          = AxisIcon(axis),
                    Count         = count,
                    NextThreshold = NextThreshold(count),
                    ActiveTier    = ActiveTier(count),
                    JustCrossed   = CrossedThreshold(_prevCounts.GetValueOrDefault(axis, 0), count),
                });
                _prevCounts[axis] = count;
            }
            RefreshFoldedDots();
            if (collapsed)
                return;
            _scrollView.SetItemList(_dataList);
        }

        //# 축별 아이콘 — 인스펙터 직접 참조 4개를 EBuildAxis 로 매핑. 미할당이면 null (셀이 마커 전부 숨김).
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
        public Sprite Icon;         //# 축 아이콘. ActiveTier 만큼 우측 티어 마커로 표시. null 이면 마커 전부 숨김.
        public int Count;
        public int NextThreshold;   //# -1 이면 7+ 도달
        public int ActiveTier;      //# 0·1·2·3
        public bool JustCrossed;    //# 이번 갱신에서 임계를 새로 넘은 경우 펄스
    }
}
