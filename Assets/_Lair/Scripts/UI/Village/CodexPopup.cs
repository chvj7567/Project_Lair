using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class CodexPopupArg : UIArg
    {
        public MetaProfile Profile;
        public MetaConfig Config;
        public List<CardData> AllCards = new List<CardData>();   //# 패시브+액티브 28장 — VillageController 가 로드
    }

    //# 카드 탭 셀 표시 데이터 — 해금(일러스트+이름) / 미해금(실루엣+???). 몬스터 탭은 CodexMonsterCellData(monster-codex).
    public class CodexCellData
    {
        public string DisplayName;
        public bool Unlocked;
        public bool IsLockedDummy;
        public Sprite Icon;          //# 카드 일러스트 또는 몬스터 아이콘 (null → 색칩 fallback)
        public Color TintColor;      //# 몬스터 종 색 (아이콘 미할당 시 색칩)
        public EMonster? Species;    //# 몬스터 셀만 지정 (카드/더미는 null) — 강화 발광색 SoT 조회 키
        public int EnhanceLevel;     //# Enhance_<종족> 상점 레벨 0~5 (카드/더미는 기본값 0)
    }

    //# 도감 — 몬스터 탭(CodexMonsterPanel: 6종족 x 등급 x 티어 매트릭스, monster-codex) / 카드 탭(6열 풀링 스크롤뷰).
    public class CodexPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private Toggle _monsterTab;       //# CHToggle 동행 (Rule 03 §3)
        [SerializeField] private Toggle _cardTab;
        [SerializeField] private CodexMonsterPanel _monsterPanel;
        [SerializeField] private CodexPoolingScrollView _cardScrollView;      //# 6열
        [SerializeField] private CHText _collectedText;   //# 탭 옆 "수집 n / 120" (카드 탭은 "수집 n / 28")

        private CodexPopupArg _arg;
        private bool _showCards;
        private CodexViewModel _monsterVm;   //# 팝업 수명 — 탭을 오가도 NEW 배지 유지

        public override void InitUI(UIArg arg)
        {
            _arg = arg as CodexPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() =>
                {
                    if (_monsterPanel != null)
                    {
                        _monsterPanel.Hide();
                    }
                    _monsterVm = null;
                    _arg = null;
                });
            }
            _showCards = false;
            _monsterVm = null;

            if (_dimButton != null)
            {
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            }
            if (_closeButton != null)
            {
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
            }

            WireTab(_monsterTab, showCards: false);
            WireTab(_cardTab, showCards: true);
            if (_monsterTab != null)
            {
                _monsterTab.SetIsOnWithoutNotify(true);
            }
            if (_cardTab != null)
            {
                _cardTab.SetIsOnWithoutNotify(false);
            }

            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        private void WireTab(Toggle tab, bool showCards)
        {
            if (tab == null)
                return;
            UnityEngine.Events.UnityAction<bool> handler = isOn =>
            {
                if (isOn == false)
                    return;
                _showCards = showCards;
                Rebuild();
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

        private void OnEnable()
        {
            if (_arg == null)
                return;
            Rebuild();
        }

        //# 슬롯·미리보기·시트 해제 — 비활성(재사용 닫힘)에서도 점유를 남기지 않는다.
        private void OnDisable()
        {
            if (_monsterPanel != null)
            {
                _monsterPanel.Hide();
            }
        }

        private static string LookupText(int id) => CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;

        private void Rebuild()
        {
            if (_arg == null)
                return;

            //# 활성 토글 — SetItemList 는 Awake(origin 비활성/ScrollRect 캐싱) 이후여야 하므로 활성화 먼저.
            if (_monsterPanel != null)
            {
                _monsterPanel.gameObject.SetActive(_showCards == false);
            }
            if (_cardScrollView != null)
            {
                _cardScrollView.gameObject.SetActive(_showCards);
            }

            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            if (_showCards)
            {
                if (_monsterPanel != null)
                {
                    _monsterPanel.Hide();
                }
                List<CodexCellData> cells = BuildCardCellData(_arg.Profile, _arg.Config, _arg.AllCards);
                if (_collectedText != null)
                {
                    _collectedText.SetText(BuildCollectedText(cells));
                }
                if (_cardScrollView != null)
                {
                    _cardScrollView.SetItemList(cells);
                }
                return;
            }

            if (_monsterVm == null)
            {
                _monsterVm = new CodexViewModel(_arg.Profile, FormationDatabase.Current, LookupText);
            }
            if (_collectedText != null)
            {
                _collectedText.SetText(_monsterVm.CollectedText);
            }
            if (_monsterPanel != null)
            {
                _monsterPanel.Show(_monsterVm, CodexDatabase.Current, CreateTierLoader(_arg.Profile));
            }
        }

        //# 도감 전용 티어 시트 로더 — 현재 티어 키는 마을 쇼케이스가 쥐고 있어 해제하지 않는다(CHMResource.Unload 는 참조 카운트가 없다).
        private static MonsterTierSheetLoader CreateTierLoader(MetaProfile profile)
        {
            HashSet<EMonsterTierSheet> keep = new HashSet<EMonsterTierSheet>();
            foreach (EMonster race in MonsterGrades.BaseMonsters)
            {
                int tier = CodexRules.TierOf(profile != null ? profile.GetShopLevel("Enhance_" + race) : 0);
                if (tier > 0 && MonsterTierSheetLoader.TryKeyOf(race, tier, out EMonsterTierSheet key))
                {
                    keep.Add(key);
                }
            }
            return new MonsterTierSheetLoader(
                key => CHMResource.Instance.LoadAsync<MonsterTierSheetSet>(key),
                key =>
                {
                    if (keep.Contains(key))
                        return;
                    CHMResource.Instance.Unload(key);
                },
                Debug.LogWarning);
        }

        //# "수집 n / 28" — 해금(Unlocked)만 분자(IsLockedDummy 는 분모 제외 유지).
        public static string BuildCollectedText(List<CodexCellData> cells)
        {
            int total = 0;
            int collected = 0;
            if (cells != null)
            {
                foreach (CodexCellData cell in cells)
                {
                    if (cell == null || cell.IsLockedDummy)
                        continue;
                    total++;
                    if (cell.Unlocked)
                    {
                        collected++;
                    }
                }
            }
            return $"수집 {collected} / {total}";
        }

        //# 카드 탭 — 28장 (해금 = PickedCards 포함). 잠금 더미는 제거(monster-codex D7).
        public static List<CodexCellData> BuildCardCellData(MetaProfile profile, MetaConfig cfg, List<CardData> allCards)
        {
            List<CodexCellData> list = new List<CodexCellData>();
            if (profile == null || cfg == null || allCards == null)
                return list;

            foreach (CardData card in allCards)
            {
                if (card == null)
                    continue;
                bool picked = profile.PickedCards.Contains(card.Id.ToString());
                list.Add(new CodexCellData
                {
                    DisplayName = picked ? card.DisplayName : "???",
                    Unlocked = picked,
                    Icon = card.CardImage != null ? card.CardImage : card.Icon,
                });
            }
            return list;
        }
    }
}
