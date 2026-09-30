using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# CHPoolingScrollView 의 TData — 한 행(축 헤더 or 티어 효과).
    public class SynergyModalCellData
    {
        public enum Kind { Header, Effect }
        public Kind RowKind;
        public Color AxisColor;   //# 축 색 띠 (헤더·효과 공통)
        public string Label;       //# Header: "TANK (5장)" / Effect: "Tier1  Wisp·Wraith HP ×1.3"
        public Sprite Icon;        //# 축 아이콘 — 헤더 행만 사용. 효과 행은 항상 null (기획서 §3.2).
        //# 헤더 행 전용 — UI 리디자인 단계 배지/현재 설명/다음 단계 미리보기. 효과 행은 빈 값.
        public string TierBadgeText;   //# "2/3"
        public bool IsMaxTier;         //# 3/3 — 미리보기 대신 "최대 단계"
        public string DescText;        //# 현재 최고 단계 설명
        public string NextText;        //# "다음: …" (최대 단계면 빈 문자열)
    }

    //# Rule 03 §3 — 풀 재사용 셀. Header/Effect 한 행 렌더.
    public class SynergyModalCell : MonoBehaviour
    {
        [SerializeField] private Image _axisStrip;   //# 좌측 축 색 띠
        [SerializeField] private Image _icon;         //# 축 아이콘 — 헤더 행만 활성 (기획서 §3.1)
        [SerializeField] private CHText _label;       //# 헤더/효과 텍스트
        //# UI 리디자인 — 단계 배지 + 현재 설명 + 다음 단계 미리보기. 위젯 연결은 프리팹 단계, 미할당이면 건너뛴다.
        [SerializeField] private CHText _tierBadge;
        [SerializeField] private CHText _descText;
        [SerializeField] private CHText _nextText;

        //# 비주얼 리셋을 OnEnable 에 두지 않는다 — 재오픈 SetActive(true) cascade 에서
        //# 셀 OnEnable 이 팝업 Build(Bind) 보다 늦게 돌면 Bind 결과를 빈 값으로 덮어쓰기 때문.
        //# 활성 셀은 Bind 가, 빈 셀은 SetActive(false) 가 책임지므로 stale 잔류 없음.
        public void Bind(SynergyModalCellData data)
        {
            if (data == null)
                return;
            if (_axisStrip != null)
                _axisStrip.color = data.AxisColor;
            if (_label != null)
                _label.SetText(data.Label);
            ApplyTierInfo(data);

            //# 헤더 + 아이콘 할당 시에만 표시. 효과 행(Icon=null)·미할당이면 숨김 (기획서 §3.2).
            bool showIcon = data.RowKind == SynergyModalCellData.Kind.Header && data.Icon != null;
            if (showIcon == false)
            {
                ResetIcon();
                return;
            }
            if (_icon == null)
                return;
            _icon.sprite = data.Icon;
            _icon.gameObject.SetActive(true);
            //# 헤더는 굵게/들여쓰기 없음 — MVP 텍스트만. 색 띠 + 아이콘 + "(N장)" 표기로 구분.
        }

        //# 헤더 행일 때만 단계 배지/설명/다음 미리보기를 채운다. 최대 단계는 "최대 단계" 문구로 대체.
        private void ApplyTierInfo(SynergyModalCellData data)
        {
            bool header = data.RowKind == SynergyModalCellData.Kind.Header;
            if (_tierBadge != null)
            {
                _tierBadge.gameObject.SetActive(header);
                if (header)
                {
                    _tierBadge.SetText(data.TierBadgeText);
                }
            }
            if (_descText != null)
            {
                _descText.gameObject.SetActive(header);
                if (header)
                {
                    _descText.SetText(data.DescText);
                }
            }
            if (_nextText != null)
            {
                _nextText.gameObject.SetActive(header);
                if (header)
                {
                    _nextText.SetText(data.IsMaxTier ? "· 최대 단계" : data.NextText);
                }
            }
        }

        //# 아이콘 비활성 + sprite null — 효과 행·아이콘 미할당 헤더 처리.
        private void ResetIcon()
        {
            if (_icon == null)
                return;
            _icon.sprite = null;
            _icon.gameObject.SetActive(false);
        }
    }
}
