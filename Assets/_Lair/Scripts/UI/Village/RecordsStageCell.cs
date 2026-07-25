using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 기록 스테이지 셀 — 표시 전용. 문구·색은 전부 RecordsStageCellData 가 확정해서 들어온다 (Rule 02 §6).
    public class RecordsStageCell : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private CHText _stageText;
        [SerializeField] private CHText _threatText;
        [SerializeField] private CHText _bestText;
        [SerializeField] private CHText _winText;
        [SerializeField] private CHText _runRateText;
        [SerializeField] private CHText _lockHintText;
        [SerializeField] private GameObject _selectedBadge;

        //# 풀 재사용/재오픈 리셋은 Bind 이 전담한다 (다른 셀 관례 — CodexCell·ShopItemCell 등).
        //# OnEnable 리셋 금지: 재오픈 시 팝업 재활성화가 셀 OnEnable 을 Bind 뒤에 발화시켜(부모→자식 순서)
        //# 잠금 힌트·선택 배지를 도로 꺼버린다. Bind 이 잠금/배지 상태를 매번 전부 재설정하므로 별도 리셋 불필요.
        public void Bind(RecordsStageCellData data)
        {
            if (data == null)
                return;

            if (_portrait != null)
            {
                _portrait.sprite = data.Portrait;
                _portrait.color = data.PortraitTint;
            }
            if (_stageText != null)
            {
                _stageText.SetText(data.StageText);
            }
            if (_threatText != null)
            {
                _threatText.SetText(data.ThreatText);
            }
            if (_bestText != null)
            {
                _bestText.SetText(data.BestText);
                _bestText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_winText != null)
            {
                _winText.SetText(data.WinText);
                _winText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_runRateText != null)
            {
                _runRateText.SetText(data.RunRateText);
                _runRateText.gameObject.SetActive(data.IsLocked == false);
            }
            if (_lockHintText != null)
            {
                _lockHintText.SetText(data.LockHintText);
                _lockHintText.gameObject.SetActive(data.IsLocked);
            }
            if (_selectedBadge != null)
            {
                _selectedBadge.SetActive(data.IsSelected);
            }
        }
    }
}
