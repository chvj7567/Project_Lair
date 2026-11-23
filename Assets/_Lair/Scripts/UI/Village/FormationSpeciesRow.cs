using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 던전 편성 우측 몬스터 행 1개 (332x72) — 선택 둥지 종족의 보유 몬스터 하나. 표시 + 클릭 수신만 (Rule 02 §6). 위젯은 [SerializeField] private(§6.1).
    public class FormationSpeciesRow : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _iconBorder;      //# 등급 테두리 (MonsterGradeFrame)
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;       //# "견습 도깨비불"
        [FormerlySerializedAs("_costText")]
        [SerializeField] private CHText _gradeText;      //# "2등급 · HP·공격력 x1.10 · 보유 최고"
        [SerializeField] private CHText _awakenText;     //# "체급 Lv 2" (금색) — Lv 0 이면 숨김
        [SerializeField] private CHText _placementText;
        [SerializeField] private CHText _axisText;       //# 빌드 축 칩 글자
        [SerializeField] private Image _axisBorder;      //# 빌드 축 칩 테두리
        [SerializeField] private Image _selectionRing;          //# 굵은 금 링
        [SerializeField] private GameObject _selectedTag;       //# "출전 중" 표식
        [SerializeField] private Image _gradeBadge;             //# 등급 동그라미 (BadgeColor 틴트)
        [SerializeField] private CHText _gradeBadgeText;        //# 등급 숫자

        private static readonly Color AxisTextColor = new Color32(0x14, 0x10, 0x0a, 0xff);   //# 속이 찬 축 칩 위 글자색
        private Action _pooledClick;

        public GameObject FocusTarget => _button != null ? _button.gameObject : null;

        public void InitPooledClick()
        {
            _button?.OnClick(() => _pooledClick?.Invoke());
        }

        public void BindPooled(FormationSpeciesData data, Sprite icon, Action onClick)
        {
            _pooledClick = onClick;
            Show(data, icon);
        }

        //# data 가 null 이면 행을 숨긴다(보유하지 않은 등급은 표시하지 않음).
        public void Show(FormationSpeciesData data, Sprite icon)
        {
            gameObject.SetActive(data != null);
            if (data == null)
                return;
            if (_nameText != null)
            {
                _nameText.SetText(data.NameText);
                _nameText.SetColor(data.SpeciesColor);
            }
            _gradeText?.SetText(_placementText == null ? data.PlacementText : data.GradeText);
            _placementText?.SetText(data.PlacementText);
            if (_awakenText != null)
            {
                _awakenText.gameObject.SetActive(true);
                _awakenText.SetText(data.TierText);
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
            Color axisColor = AxisColor(data.Axis);
            if (_axisText != null)
            {
                _axisText.SetText(data.Axis.ToString());
                _axisText.SetColor(AxisTextColor);
            }
            if (_axisBorder != null)
            {
                _axisBorder.color = axisColor;
            }
            ShowEnhanced(data);
        }

        private void ShowEnhanced(FormationSpeciesData data)
        {
            if (_selectionRing != null)
            {
                _selectionRing.enabled = data.IsSelected;
            }
            if (_selectedTag != null)
            {
                _selectedTag.SetActive(data.IsSelected);
            }
            if (_gradeBadge != null)
            {
                _gradeBadge.color = MonsterGradeFrame.BadgeColor(data.Grade);
            }
            if (_gradeBadgeText != null)
            {
                //# 글자색은 프리팹 고정 — 원(_gradeBadge)과 같은 색이면 숫자가 안 보인다.
                _gradeBadgeText.SetText(data.Grade.ToString());
            }
        }

        //# 축 칩 색 = 카드 테두리 색(종 색) 재사용. Swarm 은 어두운 배경 대비를 위해 #9aa5bd (dungeon-formation §7.4).
        private static Color AxisColor(EBuildAxis axis)
        {
            switch (axis)
            {
                case EBuildAxis.Tank:
                    return SpawnerStatusCell.SpeciesColor(EMonster.Wisp);
                case EBuildAxis.Dps:
                    return SpawnerStatusCell.SpeciesColor(EMonster.Reaper);
                case EBuildAxis.Debuff:
                    return SpawnerStatusCell.SpeciesColor(EMonster.Plague);
                default:
                    return new Color32(0x9a, 0xa5, 0xbd, 0xff);
            }
        }
    }
}
