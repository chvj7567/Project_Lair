using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 결과 표시 + 재시작 버튼.
    public class ResultPopup : UIBase
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _restartButton;

        public override void InitUI(UIArg arg)
        {
            if (arg is ResultPopupArg rp && _resultText != null)
            {
                _resultText.text = rp.Result switch
                {
                    BattleResult.Win  => "승리",
                    BattleResult.Lose => "패배",
                    _                 => "-"
                };
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(OnClickRestart);
                //# Close 시 리스너 자동 해제
                closeDisposable.Add(() =>
                {
                    if (_restartButton != null)
                        _restartButton.onClick.RemoveListener(OnClickRestart);
                });
            }
        }

        private void OnClickRestart()
        {
            //# Rule 08 — EScene.Battle.ToString() == "Battle" 씬 파일명과 일치
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
