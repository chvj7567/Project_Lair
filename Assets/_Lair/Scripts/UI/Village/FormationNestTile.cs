using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("_costText")]
        [SerializeField] private CHText _raceText;            //# 종족 이름
        [SerializeField] private CHText _gradeText;           //# "2등급 · 보유 2등급"
        [SerializeField] private GameObject _defaultBadge;    //# 기본 편성(보유 최고 등급)과 같은 몬스터 — 글자 없는 회색 원
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
            if (_raceText != null)
            {
                _raceText.SetText(data.RaceText);
                _raceText.SetColor(data.SpeciesColor);
            }
            if (_gradeText != null)
            {
                _gradeText.SetText(data.GradeText);
            }
            if (_iconBorder != null)
            {
                _iconBorder.color = MonsterGradeFrame.BorderColor(data.Grade, data.SpeciesColor);
            }
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
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
