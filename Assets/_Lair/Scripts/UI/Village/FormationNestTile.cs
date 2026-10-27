using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 던전 편성 둥지 칸 1개 (136x200) — 표시 + 클릭 수신만 (Rule 02 §6). 위젯은 [SerializeField] private, 외부엔 의도 API 만 노출(§6.1).
    public class FormationNestTile : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private CHText _titleText;           //# "둥지 3"
        [SerializeField] private Image _iconBorder;           //# 종 색 테두리 3px
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;            //# 종 이름(종 색)
        [SerializeField] private CHText _costText;            //# "코스트 3"
        [SerializeField] private Image[] _costPips = new Image[0];   //# 금색 코스트 칸 — 코스트 수만큼 켠다
        [SerializeField] private GameObject _defaultBadge;    //# 기본 편성과 같은 종 — 글자 없는 회색 원
        [SerializeField] private GameObject _sealBadge;       //# 봉인 예정 칸 — 사슬 표식(보라)
        [SerializeField] private GameObject _sealedGlow;      //# 봉인 예정 칸 테두리 발광
        [SerializeField] private GameObject _selectedOutline; //# 선택 금색 외곽선 3px

        //# 클릭 핸들러 1회 등록 — 팝업 InitUI 에서 호출. disposable.Clear() 시 해제된다.
        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        //# 매 갱신마다 전체 상태를 덮어쓴다(이전 상태 잔존 방지).
        public void Show(FormationNestData data, Sprite icon)
        {
            if (data == null)
                return;
            if (_titleText != null)
            {
                _titleText.SetText(data.TitleText);
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.NameText);
                _nameText.SetColor(data.SpeciesColor);
            }
            if (_costText != null)
            {
                _costText.SetText(data.CostText);
            }
            if (_iconBorder != null)
            {
                _iconBorder.color = data.SpeciesColor;
            }
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }
            for (int i = 0; i < _costPips.Length; ++i)
            {
                if (_costPips[i] != null)
                {
                    _costPips[i].gameObject.SetActive(i < data.Cost);
                }
            }
            SetActive(_defaultBadge, data.IsDefault);
            SetActive(_sealBadge, data.IsSealed);
            SetActive(_sealedGlow, data.IsSealed);
            SetActive(_selectedOutline, data.IsSelected);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
