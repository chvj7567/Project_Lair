using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class ResultPopupArg : UIArg
    {
        public BattleResult Result;
    }

    //# 결과 표시 + 재시작 버튼. ChvjPackage UI 래퍼 사용 (Rule 11).
    public class ResultPopup : UIBase
    {
        [SerializeField] private CHText _resultText;
        [SerializeField] private CHButton _restartButton;

        public override void InitUI(UIArg arg)
        {
            if (arg is ResultPopupArg rp && _resultText != null)
            {
                _resultText.SetText(rp.Result switch
                {
                    BattleResult.Win  => "승리",
                    BattleResult.Lose => "패배",
                    _                 => "-"
                });
            }

            //# CHButton.OnClick(Action, CompositeDisposable) — closeDisposable.Clear() 시 자동 해제
            if (_restartButton != null)
            {
                _restartButton.OnClick(OnClickRestart, closeDisposable);
            }
        }

        private void OnClickRestart()
        {
            //# Rule 08 — EScene.Battle.ToString() == "Battle" 씬 파일명과 일치
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
