using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Loading 씬에 직접 배치되는 Canvas 컴포넌트 — CHMUI 로 로드하지 않음.
    //# SetProgress(ratio, desc) 로 로딩 진행률과 설명 텍스트를 갱신한다.
    public class LoadingHud : MonoBehaviour
    {
        [SerializeField] private Image _progressFill;
        [SerializeField] private CHText _percentText;
        [SerializeField] private CHText _descText;

        public void SetProgress(float ratio, string desc)
        {
            if (_progressFill != null)
            {
                _progressFill.fillAmount = ratio;
            }

            if (_percentText != null)
            {
                _percentText.SetText($"{Mathf.RoundToInt(ratio * 100)}%");
            }

            if (_descText != null)
            {
                _descText.SetText(desc);
            }
        }
    }
}
