using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 던전 편성 종 팔레트 행 1개 (332x72) — 표시 + 클릭 수신만 (Rule 02 §6). 위젯은 [SerializeField] private(§6.1).
    public class FormationSpeciesRow : MonoBehaviour
    {
        private const float DimAlpha = 0.45f;   //# 무효가 되는 종은 흐림(밝기 0.45)

        [SerializeField] private CHButton _button;
        [SerializeField] private Image _iconBorder;
        [SerializeField] private Image _icon;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _costText;       //# "코스트 3"
        [SerializeField] private CHText _countText;      //# "편성 2 / 3"
        [SerializeField] private CHText _awakenText;     //# "각성 Lv 2" (금색) — Lv 0 이면 숨김
        [SerializeField] private CHText _axisText;       //# 빌드 축 칩 글자
        [SerializeField] private Image _axisBorder;      //# 빌드 축 칩 테두리
        [SerializeField] private CanvasGroup _dimGroup;

        //# 클릭 핸들러 1회 등록 — 팝업 InitUI 에서 호출. disposable.Clear() 시 해제된다.
        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        public void Show(FormationSpeciesData data, Sprite icon)
        {
            if (data == null)
                return;
            if (_nameText != null)
            {
                _nameText.SetText(data.NameText);
                _nameText.SetColor(data.SpeciesColor);
            }
            if (_costText != null)
            {
                _costText.SetText(data.CostText);
            }
            if (_countText != null)
            {
                _countText.SetText(data.CountText);
            }
            if (_awakenText != null)
            {
                _awakenText.gameObject.SetActive(data.ShowAwaken);
                _awakenText.SetText(data.AwakenText);
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
            Color axisColor = AxisColor(data.Axis);
            if (_axisText != null)
            {
                _axisText.SetText(data.Axis.ToString());
                _axisText.SetColor(axisColor);
            }
            if (_axisBorder != null)
            {
                _axisBorder.color = axisColor;
            }
            if (_dimGroup != null)
            {
                _dimGroup.alpha = data.Dimmed ? DimAlpha : 1f;
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
