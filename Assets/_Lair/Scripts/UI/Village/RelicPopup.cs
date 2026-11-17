using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class RelicPopupArg : UIArg
    {
        public MetaProfile Profile;
    }

    //# 유물 도감 팝업 (relics §7.2) — 종류·등급 필터 칩, 4열 그리드(CHPoolingScrollView 3-class), 상세 패널.
    //# 표시·입력만 한다. 필터/셀/상세 계산은 RelicViewModel (Rule 02 §6).
    public class RelicPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private RelicPoolingScrollView _scrollView;
        [SerializeField] private RelicDetailView _detailView;
        [SerializeField] private CHText _collectedText;     //# "수집 7 / 16"
        [SerializeField] private CHText _stackSumText;      //# "9 / 52"
        [SerializeField] private GameObject _emptyRoot;     //# 필터 결과 0개 안내
        [SerializeField] private CHText _emptyText;
        //# 필터 칩 — 인덱스 0 = 전체, 1~ = enum 값 + 1. 각 그룹은 ToggleGroup 단일 선택, CHToggle 동행 (Rule 03 §3).
        [SerializeField] private Toggle[] _categoryChips = new Toggle[0];
        [SerializeField] private Toggle[] _rarityChips = new Toggle[0];
        //# 칩별 Label — _categoryChips/_rarityChips 와 같은 인덱스. 선택(금색 바탕) 시 어두운 글자로 바꾼다.
        [SerializeField] private TMP_Text[] _categoryChipLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] _rarityChipLabels = new TMP_Text[0];
        //# 잔해 교환 줄 (monster-growth-currencies §8.3) — 미배선이면 기존 안내 문구 그대로.
        [SerializeField] private ShardExchangeView _shardExchange;

        private static readonly Color ChipSelectedLabelColor = new Color(0.06f, 0.08f, 0.11f, 1f);
        private readonly Dictionary<TMP_Text, Color> _labelBaseColors = new Dictionary<TMP_Text, Color>();
        private RelicViewModel _vm;
        private ShardExchangeViewModel _exchangeVm;
        private MetaProfile _profile;
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();

        public override void InitUI(UIArg arg)
        {
            RelicPopupArg popupArg = arg as RelicPopupArg;
            MetaProfile profile = popupArg != null ? popupArg.Profile : MetaSession.GetOrLoad();
            _vm = new RelicViewModel(RelicDatabase.Current, profile, LookupText, HeroNameOf, IconOf);
            _vm.OnChanged += HandleChanged;
            _profile = profile;
            _exchangeVm = new ShardExchangeViewModel(profile, GrowthDatabase.Current, c => CurrencyRules.IsUnlocked(c, profile), LookupText);
            _exchangeVm.OnChanged += BindExchange;
            BindExchange();
            closeDisposable.Add(() =>
            {
                if (_exchangeVm != null)
                {
                    _exchangeVm.OnChanged -= BindExchange;
                }
                _exchangeVm = null;
                _profile = null;
                if (_vm != null)
                {
                    _vm.OnChanged -= HandleChanged;
                }
                _vm = null;
            });

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }

            CacheLabelColors(_categoryChipLabels);
            CacheLabelColors(_rarityChipLabels);
            WireChips(_categoryChips, _categoryChipLabels, index => _vm?.SetCategoryFilter(index));
            WireChips(_rarityChips, _rarityChipLabels, index => _vm?.SetRarityFilter(index));
            ResetChips(_categoryChips, _categoryChipLabels);
            ResetChips(_rarityChips, _rarityChipLabels);

            if (isActiveAndEnabled)
            {
                _ = LoadIconsAndRebuild();
            }
        }

        //# 잔해 교환 줄 갱신 — 교환 성공 시 VM 이 OnChanged 로 다시 부른다.
        private void BindExchange()
        {
            if (_shardExchange == null || _exchangeVm == null)
                return;
            _shardExchange.Bind(_exchangeVm.Build(), HandleExchange);
        }

        private void HandleExchange(ECurrency to)
        {
            if (_exchangeVm == null)
                return;
            string toast = _exchangeVm.Exchange(to);
            if (toast != null)
            {
                ToastView.Show(toast, EToastKind.Warning);
                return;
            }
            MetaSession.Store?.Save(_profile);
        }

        private void OnEnable()
        {
            if (_vm == null)
                return;
            _ = LoadIconsAndRebuild();
        }

        private void HandleChanged()
        {
            Rebuild();
        }

        //# 칩 입력 → VM. Toggle.onValueChanged 는 켜질 때만 전달(ToggleGroup 이 나머지를 끈다).
        private void WireChips(Toggle[] chips, TMP_Text[] labels, System.Action<int> onSelected)
        {
            for (int i = 0; i < chips.Length; ++i)
            {
                Toggle chip = chips[i];
                if (chip == null)
                    continue;
                int index = i;
                UnityAction<bool> handler = isOn =>
                {
                    ApplyLabelColor(labels, index, isOn);
                    if (isOn)
                    {
                        onSelected(index);
                    }
                };
                chip.onValueChanged.AddListener(handler);
                closeDisposable.Add(() =>
                {
                    if (chip != null)
                    {
                        chip.onValueChanged.RemoveListener(handler);
                    }
                });
            }
        }

        //# 열 때마다 "전체" 칩으로 초기화 — 알림 없이 켠다.
        private void ResetChips(Toggle[] chips, TMP_Text[] labels)
        {
            for (int i = 0; i < chips.Length; ++i)
            {
                if (chips[i] != null)
                {
                    bool on = i == RelicViewModel.FilterAll;
                    chips[i].SetIsOnWithoutNotify(on);
                    ApplyLabelColor(labels, i, on);
                }
            }
        }

        //# 첫 바인드 시 프리팹 원래 색(기본·등급색)을 캐싱 — 재바인드 때 선택색으로 덮어쓰지 않도록 1회만.
        private void CacheLabelColors(TMP_Text[] labels)
        {
            for (int i = 0; i < labels.Length; ++i)
            {
                if (labels[i] != null && _labelBaseColors.ContainsKey(labels[i]) == false)
                {
                    _labelBaseColors[labels[i]] = labels[i].color;
                }
            }
        }

        private void ApplyLabelColor(TMP_Text[] labels, int index, bool selected)
        {
            if (index >= labels.Length || labels[index] == null)
                return;
            if (_labelBaseColors.TryGetValue(labels[index], out Color baseColor) == false)
                return;
            labels[index].color = selected ? ChipSelectedLabelColor : baseColor;
        }

        //# 아이콘 16종을 Enum 키로 로드(Rule 03 §2) 후 한 번에 그린다. 팝업이 먼저 닫혔으면 중단.
        private async Task LoadIconsAndRebuild()
        {
            RelicViewModel vm = _vm;
            if (vm == null)
                return;
            foreach (ERelic key in (ERelic[])System.Enum.GetValues(typeof(ERelic)))
            {
                string id = key.ToString();
                if (_icons.ContainsKey(id))
                    continue;
                Sprite sprite = await CHMResource.Instance.LoadAsync<Sprite>(key);
                if (this == null || _vm != vm)
                    return;
                if (sprite != null)
                {
                    _icons[id] = sprite;
                }
            }
            Rebuild();
        }

        private void Rebuild()
        {
            if (_vm == null)
                return;

            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            if (_scrollView != null)
            {
                _scrollView.SetItemList(_vm.BuildCells());
            }
            if (_collectedText != null)
            {
                _collectedText.SetText(_vm.CollectedText);
            }
            if (_stackSumText != null)
            {
                _stackSumText.SetText(_vm.StackSumLabelText);
            }
            bool empty = _vm.IsEmptyResult;
            if (_emptyRoot != null)
            {
                _emptyRoot.SetActive(empty);
            }
            if (_emptyText != null && empty)
            {
                _emptyText.SetText(_vm.NoResultText);
            }
            if (_detailView != null)
            {
                _detailView.Show(_vm.BuildDetail());
            }
        }

        private Sprite IconOf(string relicId)
        {
            return _icons.TryGetValue(relicId, out Sprite sprite) ? sprite : null;
        }

        private static string LookupText(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        //# 영웅 이름 — 챕터 영웅 직업 데이터의 nameKey, 없으면 heroId 그대로.
        private static string HeroNameOf(string heroId)
        {
            HeroArchetypeRow hero = ChapterDatabase.Current != null ? ChapterDatabase.Current.GetHero(heroId) : null;
            if (hero == null)
                return heroId;
            string name = LookupText(hero.nameKey);
            return string.IsNullOrEmpty(name) ? heroId : name;
        }
    }
}
