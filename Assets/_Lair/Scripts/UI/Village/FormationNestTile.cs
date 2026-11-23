using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 던전 편성 둥지 칸 1개 (136x200) — 표시 + 클릭 수신만 (Rule 02 §6). 위젯은 [SerializeField] private, 외부엔 의도 API 만.
    //# 구성(dungeon-formation §0.10): 상단 "둥지 {n}" + 아이콘(등급 테두리) + 몬스터 표시 이름(14) + 종족 이름(12) + 등급 줄(11).
    public class FormationNestTile : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private CHText _titleText;           //# "둥지 3"
        [SerializeField] private Image _iconBorder;           //# 등급 테두리 3px (MonsterGradeFrame 색)
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;            //# 몬스터 표시 이름(종 색, 접두어 포함)
        [SerializeField] private CHText _tierText;
        [SerializeField] private Image _selectionRing;        //# 굵은 흰색 링 — 선택 금색·봉인 보라(둘 다면 금색)
        [SerializeField] private GameObject _selectedRibbon;  //# "출전 중" 띠
        [SerializeField] private GameObject _sealRibbon;      //# "봉인 예정" 띠
        [SerializeField] private Image _sealIconBig;          //# 큰 자물쇠

        private static readonly Color RingGold = new Color32(0xf5, 0xc5, 0x42, 0xff);
        private static readonly Color RingViolet = new Color32(0x9b, 0x4d, 0xff, 0xff);
        private static readonly Color SealedIconTint = new Color(0.35f, 0.35f, 0.35f, 1f);

        public GameObject FocusTarget => _button != null ? _button.gameObject : null;

        //# 클릭 핸들러 1회 등록 — 팝업 InitUI 에서 호출. disposable.Clear() 시 해제된다.
        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        public void Focus()
        {
            if (_button != null)
            {
                EventSystem.current?.SetSelectedGameObject(_button.gameObject);
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
            if (_tierText != null)
            {
                _tierText.gameObject.SetActive(data.IsEmpty == false);
                _tierText.SetText(data.TierText ?? string.Empty);
            }
            if (_iconBorder != null)
            {
                _iconBorder.enabled = data.IsEmpty == false;
                if (data.IsEmpty == false)
                {
                    _iconBorder.color = MonsterGradeFrame.BorderColor(data.Grade, data.SpeciesColor);
                }
            }
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
                _icon.color = data.IsSealed ? SealedIconTint : Color.white;
            }
            ShowEnhanced(data);
        }

        private void ShowEnhanced(FormationNestData data)
        {
            if (_selectionRing != null)
            {
                _selectionRing.enabled = data.IsSelected || data.IsSealed;
                _selectionRing.color = data.IsSelected ? RingGold : RingViolet;
            }
            SetActive(_selectedRibbon, data.IsSelected);
            SetActive(_sealRibbon, data.IsSealed);
            if (_sealIconBig != null)
            {
                _sealIconBig.gameObject.SetActive(data.IsSealed);
            }
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
