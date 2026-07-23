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

        //# 풀 재사용 리셋 — 이전 행의 잠금/배지 상태가 새 행에 새지 않게 (Rule 03 §4).
        private void OnEnable()
        {
            if (_selectedBadge != null)
            {
                _selectedBadge.SetActive(false);
            }
            if (_lockHintText != null)
            {
                _lockHintText.gameObject.SetActive(false);
            }
        }

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
