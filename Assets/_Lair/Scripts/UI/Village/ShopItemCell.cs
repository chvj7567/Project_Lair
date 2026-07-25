using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 상점 품목 셀 — 이름/레벨/효과 설명/가격 + 구매 버튼 (기획서 §3.2 / §7).
    //# 단일 셀 타입이 RowKind 로 섹션 헤더 행 ↔ 항목 행을 분기 (기획서 §1·7, SynergyModalCell 관례).
    //# 종족 강화 셀은 아이콘 + 발광 프레임 추가 표시 (monster-species-enhancement §5.2). 글로벌 항목이면 숨김.
    public class ShopItemCell : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _levelText;
        [SerializeField] private CHText _descText;
        [SerializeField] private CHText _priceText;
        [SerializeField] private CHButton _buyButton;
        [SerializeField] private CHText _buyLabel;

        //# 종족 강화 전용 위젯 — 글로벌 항목이면 비활성 (monster-species-enhancement §5.2).
        [SerializeField] private Image _iconImage;      //# 종족 아이콘
        [SerializeField] private Image _glowFrame;      //# 현재 Lv 발광 프레임 (색=SpeciesGlowColor, 밝기=Level/MaxLevel)
        [SerializeField] private Image _glowHintRing;   //# 다음 Lv 힌트 링 (만렙 아닐 때만, 낮은 알파)

        //# 섹션 헤더 전용 위젯 그룹 (기획서 §1·2.1). 항목 행이면 전부 비활성.
        [SerializeField] private CHText _headerText;    //# 섹션 제목 ("스탯 강화"/"몬스터 강화")
        [SerializeField] private Image _headerBg;       //# 배너 밴드 배경 + 하단 구분선
        [SerializeField] private Image _headerAccent;   //# 금 액센트 바 3×22 (#FBBF24)

        //# 다음 Lv 힌트 링 알파 배수 (§5.2 — ≈0.35×).
        private const float HintRingAlpha = 0.35f;

        //# 구매 가능 노랑 (#FBBF24) / 불가 회색 (#9CA3AF).
        private static readonly Color BuyableColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color DisabledColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        private ShopItemCellData _data;
        private bool _wired;

        //# 표시 상태는 Bind 가 완전히 결정 — 풀 재사용 시 매 InitItem 마다 호출됨.
        //# 리셋을 OnEnable 에 두지 않는다 (RecordsStageCell·SynergyModalCell 교훈): 재오픈 SetActive cascade 에서
        //# 셀 OnEnable 이 Bind 보다 늦게 돌면 Bind 결과를 덮어쓴다. 헤더/항목 그룹을 매 Bind 가 명시 on/off 로 재설정.
        public void Bind(ShopItemCellData data)
        {
            if (data == null)
                return;
            _data = data;

            bool isHeader = data.RowKind == ShopPopup.ShopRowKind.SectionHeader;
            SetHeaderWidgetsActive(isHeader);
            SetItemWidgetsActive(isHeader == false);

            if (isHeader)
            {
                if (_headerText != null)
                {
                    _headerText.SetText(data.HeaderText);
                }
                return;
            }

            WireOnce();

            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
            }
            if (_levelText != null)
            {
                _levelText.SetText(data.LevelText);
            }
            if (_descText != null)
            {
                _descText.SetText(data.Description);
            }

            if (_priceText != null)
            {
                _priceText.gameObject.SetActive(data.IsMax == false);
                if (data.IsMax == false)
                {
                    _priceText.SetText($"{data.Price:N0} 소울");
                }
            }

            if (_buyButton != null)
            {
                _buyButton.Interactable = data.CanBuy;
            }
            if (_buyLabel != null)
            {
                //# 버튼 문구 치환 — 구매 / 만렙 / 소울 부족 (기획서 §7). 프리팹 _buyLabel 은 stringID 미사용 전제.
                string label = data.IsMax ? "만렙" : data.CanBuy ? "구매" : "소울 부족";
                _buyLabel.SetText(label);
                _buyLabel.SetColor(data.CanBuy ? BuyableColor : DisabledColor);
            }

            BindSpeciesGlow(data);
        }

        //# 종족 강화 셀의 아이콘 + 발광 프레임 표시. 글로벌 항목(Icon/Species null)이면 세 위젯 모두 숨김(풀 재사용 잔존 방지 Rule 03 §4).
        private void BindSpeciesGlow(ShopItemCellData data)
        {
            bool isSpecies = data.Icon != null && data.Species.HasValue && data.MaxLevel > 0;

            if (_iconImage != null)
            {
                _iconImage.sprite = isSpecies ? data.Icon : null;
                _iconImage.gameObject.SetActive(isSpecies);
            }

            if (isSpecies == false)
            {
                //# 글로벌 셀 — 발광 위젯 확실히 숨김 (풀 재사용 잔존 방지).
                if (_glowFrame != null)
                {
                    _glowFrame.gameObject.SetActive(false);
                }
                if (_glowHintRing != null)
                {
                    _glowHintRing.gameObject.SetActive(false);
                }
                return;
            }

            Color glow = SpeciesVisual.SpeciesGlowColor(data.Species.Value);

            //# 현재 Lv 프레임 — 색조=종족 발광색, 밝기(알파)=Level/MaxLevel 선형(Lv0=0 → 꺼짐, §5.2).
            if (_glowFrame != null)
            {
                float brightness = Mathf.Clamp01((float)data.Level / data.MaxLevel);
                _glowFrame.color = new Color(glow.r, glow.g, glow.b, brightness);
                _glowFrame.gameObject.SetActive(true);
            }

            //# 다음 Lv 힌트 링 — 만렙 아닐 때만. (Level+1)/MaxLevel 밝기 × 낮은 알파(§5.2).
            if (_glowHintRing != null)
            {
                bool showHint = data.IsMax == false;
                _glowHintRing.gameObject.SetActive(showHint);
                if (showHint)
                {
                    float hintBrightness = Mathf.Clamp01((float)(data.Level + 1) / data.MaxLevel);
                    _glowHintRing.color = new Color(glow.r, glow.g, glow.b, hintBrightness * HintRingAlpha);
                }
            }
        }

        //# 헤더 그룹 3위젯 일괄 토글 (기획서 §1·2.1).
        private void SetHeaderWidgetsActive(bool on)
        {
            if (_headerText != null)
            {
                _headerText.gameObject.SetActive(on);
            }
            if (_headerBg != null)
            {
                _headerBg.gameObject.SetActive(on);
            }
            if (_headerAccent != null)
            {
                _headerAccent.gameObject.SetActive(on);
            }
        }

        //# 항목 위젯 그룹 일괄 토글 — 헤더 행이면 off. 종족 아이콘/발광은 항목 행에서 BindSpeciesGlow 가 다시 세분 제어.
        private void SetItemWidgetsActive(bool on)
        {
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(on);
            }
            if (_levelText != null)
            {
                _levelText.gameObject.SetActive(on);
            }
            if (_descText != null)
            {
                _descText.gameObject.SetActive(on);
            }
            if (_priceText != null)
            {
                _priceText.gameObject.SetActive(on);
            }
            if (_buyButton != null)
            {
                _buyButton.gameObject.SetActive(on);
            }
            if (_buyLabel != null)
            {
                _buyLabel.gameObject.SetActive(on);
            }
            if (_iconImage != null)
            {
                _iconImage.gameObject.SetActive(on);
            }
            if (_glowFrame != null)
            {
                _glowFrame.gameObject.SetActive(on);
            }
            if (_glowHintRing != null)
            {
                _glowHintRing.gameObject.SetActive(on);
            }
        }

        //# CHButton.OnClick 은 listener 누적 — 풀 재사용 셀은 1회만 등록하고 현재 _data 로 위임.
        private void WireOnce()
        {
            if (_wired)
                return;
            _wired = true;
            if (_buyButton != null)
            {
                _buyButton.OnClick(HandleBuyClick);
            }
        }

        private void HandleBuyClick()
        {
            if (_data == null || _data.CanBuy == false)
                return;
            _data.OnBuy?.Invoke(_data.Id);
        }
    }
}
