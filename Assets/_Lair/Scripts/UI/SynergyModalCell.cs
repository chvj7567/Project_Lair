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
    }

    //# Rule 03 §3 — 풀 재사용 셀. Header/Effect 한 행 렌더.
    public class SynergyModalCell : MonoBehaviour
    {
        [SerializeField] private Image _axisStrip;   //# 좌측 축 색 띠
        [SerializeField] private Image _icon;         //# 축 아이콘 — 헤더 행만 활성 (기획서 §3.1)
        [SerializeField] private CHText _label;       //# 헤더/효과 텍스트

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
