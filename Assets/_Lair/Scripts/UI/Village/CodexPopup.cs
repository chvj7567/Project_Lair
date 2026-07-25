using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using Lair.Meta;
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

    //# 셀 표시 데이터 — 해금(컬러+이름) / 미조우(실루엣+???) / 잠금 더미(??? — 추후 해금).
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

    //# 도감 — 몬스터/카드 탭 + 각 탭 말미 잠금 더미 (기획서 §6).
    //# 탭별 그리드 열 수가 달라(몬스터 4열 / 카드 6열) 스크롤뷰 2개를 토글 — CHPoolingScrollView 열 수는 직렬화 고정.
    public class CodexPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private Toggle _monsterTab;       //# CHToggle 동행 (Rule 03 §3)
        [SerializeField] private Toggle _cardTab;
        [SerializeField] private CodexPoolingScrollView _monsterScrollView;   //# 4열 (기획서 §6)
        [SerializeField] private CodexPoolingScrollView _cardScrollView;      //# 6열

        //# 종 → 도감 아이콘 — 인스펙터 직접 참조 (SpawnerStatusCell 관례, Addressables 키 아님 — 캐릭터 프리팹과 주소 충돌 방지).
        [SerializeField] private Sprite _wispIcon;
        [SerializeField] private Sprite _wraithIcon;
        [SerializeField] private Sprite _reaperIcon;
        [SerializeField] private Sprite _hexIcon;
        [SerializeField] private Sprite _plagueIcon;
        [SerializeField] private Sprite _phantomIcon;

        private CodexPopupArg _arg;
        private bool _showCards;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as CodexPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() => _arg = null);
            }
            _showCards = false;

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

        private void Rebuild()
        {
            if (_arg == null)
                return;

            //# 활성 토글 — SetItemList 는 Awake(origin 비활성/ScrollRect 캐싱) 이후여야 하므로 활성화 먼저.
            if (_monsterScrollView != null)
            {
                _monsterScrollView.gameObject.SetActive(_showCards == false);
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
                if (_cardScrollView != null)
                {
                    _cardScrollView.SetItemList(BuildCardCellData(_arg.Profile, _arg.Config, _arg.AllCards));
                }
            }
            else
            {
                if (_monsterScrollView != null)
                {
                    _monsterScrollView.SetItemList(BuildMonsterCellData(_arg.Profile, _arg.Config, SpeciesIcon));
                }
            }
        }

        //# 종 → 도감 아이콘 매핑 (인스펙터 직접 참조). 미할당이면 null → 색칩 fallback.
        private Sprite SpeciesIcon(EMonster type) => type switch
        {
            EMonster.Wisp    => _wispIcon,
            EMonster.Wraith  => _wraithIcon,
            EMonster.Reaper  => _reaperIcon,
            EMonster.Hex     => _hexIcon,
            EMonster.Plague  => _plagueIcon,
            EMonster.Phantom => _phantomIcon,
            _                => null,
        };

        //# 몬스터 탭 — 6종 + 잠금 더미 (해금 = SeenMonsters 포함, 기획서 §6).
        //# 미조우도 아이콘은 싣는다 — CodexCell 이 SilhouetteColor 틴트로 실루엣 처리.
        public static List<CodexCellData> BuildMonsterCellData(MetaProfile profile, MetaConfig cfg, Func<EMonster, Sprite> iconResolver)
        {
            List<CodexCellData> list = new List<CodexCellData>();
            if (profile == null || cfg == null)
                return list;

            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                bool seen = profile.SeenMonsters.Contains(type.ToString());
                list.Add(new CodexCellData
                {
                    DisplayName = seen ? SpeciesVisual.SpeciesName(type) : "???",
                    Unlocked = seen,
                    Icon = iconResolver != null ? iconResolver(type) : null,
                    TintColor = SpawnerStatusCell.SpeciesColor(type),
                    Species = type,
                    EnhanceLevel = profile.GetShopLevel("Enhance_" + type),
                });
            }
            AddLockedDummies(list, cfg.CodexLockedSlots);
            return list;
        }

        //# 카드 탭 — 28장 + 잠금 더미 (해금 = PickedCards 포함).
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
            AddLockedDummies(list, cfg.CodexLockedSlots);
            return list;
        }

        private static void AddLockedDummies(List<CodexCellData> list, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                list.Add(new CodexCellData
                {
                    DisplayName = "??? — 추후 해금",
                    IsLockedDummy = true,
                });
            }
        }
    }
}
