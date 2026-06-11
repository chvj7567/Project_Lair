using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ShopPopupArg : UIArg
    {
        public ShopService Shop;
        public MetaProfile Profile;
        public MetaConfig Config;
        public Action OnPurchased;   //# 구매 성공 시 — VillageController 가 저장 + VM 갱신
    }

    //# 셀 표시 데이터 — BuildCellData 가 가공 (EditMode 테스트 대상).
    public class ShopItemCellData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string LevelText;          //# "Lv 2/5"
        public int Price;                 //# 만렙이면 0
        public bool IsMax;
        public bool CanBuy;               //# 만렙 아님 + 소울 충분
        public Action<string> OnBuy;      //# 셀 구매 버튼 → 팝업 핸들러
    }

    //# 소울 상점 — 7품목 레벨제 영구 업그레이드 목록 (기획서 §3).
    public class ShopPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;
        [SerializeField] private CHButton _closeButton;
        [SerializeField] private CHText _soulText;     //# 잔액 "N 소울"
        [SerializeField] private ShopItemPoolingScrollView _scrollView;

        private ShopPopupArg _arg;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as ShopPopupArg;
            if (_arg != null)
            {
                closeDisposable.Add(() => _arg = null);
            }

            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);

            //# prefab active 저장 케이스 보강 — BuildModalPopup 과 동일 (layout 산정 후 Build).
            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        private void OnEnable()
        {
            if (_arg == null)
                return;
            BuildAndLayout();
        }

        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Rebuild();
        }

        private void Rebuild()
        {
            if (_arg == null)
                return;
            if (_soulText != null)
                _soulText.SetText($"{_arg.Profile.Souls:N0} 소울");

            List<ShopItemCellData> data = BuildCellData(_arg.Profile, _arg.Config);
            foreach (ShopItemCellData cell in data)
            {
                cell.OnBuy = HandleBuy;
            }
            if (_scrollView != null)
                _scrollView.SetItemList(data);
        }

        private void HandleBuy(string itemId)
        {
            if (_arg == null || _arg.Shop == null)
                return;
            if (_arg.Shop.Buy(itemId) == false)
                return;
            _arg.OnPurchased?.Invoke();
            Rebuild();
        }

        //# 데이터 가공 — 표시 문자열/가격/구매 가능 (기획서 §3.2 / §7 id 18~20 치환은 셀이 담당).
        public static List<ShopItemCellData> BuildCellData(MetaProfile profile, MetaConfig cfg)
        {
            List<ShopItemCellData> list = new List<ShopItemCellData>();
            if (profile == null || cfg == null)
                return list;

            foreach (ShopItemDef def in cfg.ShopItems)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;

                int level = profile.GetShopLevel(def.Id);
                bool isMax = level >= def.MaxLevel;
                int price = isMax ? 0 : ShopService.PriceOf(def, level);
                list.Add(new ShopItemCellData
                {
                    Id = def.Id,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    LevelText = $"Lv {level}/{def.MaxLevel}",
                    Price = price,
                    IsMax = isMax,
                    CanBuy = isMax == false && profile.Souls >= price,
                });
            }
            return list;
        }
    }
}
