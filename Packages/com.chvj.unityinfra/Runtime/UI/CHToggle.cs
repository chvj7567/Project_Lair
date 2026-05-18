using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Toggle에 사운드 hook을 자동 연결하는 래퍼.
    /// Toggle은 첫 프레임에 Unity가 onValueChanged를 한 번 발생시키는 경우가 많아
    /// (초기 상태 동기화) 그 첫 콜백은 사운드 hook을 건너뛴다.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class CHToggle : MonoBehaviour
    {
        /// <summary>
        /// 값 변경 시 사운드 재생을 게임 측에 위임하는 정적 hook.
        /// 게임 부팅 시 한 번만 등록: CHToggle.ChangeSoundHook = () => AudioManager.Instance.Play(EAudio.Click);
        /// </summary>
        public static Action ChangeSoundHook;

        [NonSerialized]
        public Toggle toggle;

        // 첫 콜백은 초기 상태 동기화로 보고 사운드를 울리지 않는다.
        // 외부에서 의도적으로 첫 클릭 사운드도 원하면 false로 만들 수 있도록 protected.
        protected bool skipNextChangeSound = true;

        private void Start()
        {
            toggle = GetComponent<Toggle>();

            toggle.onValueChanged.AddListener(_ =>
            {
                if (skipNextChangeSound)
                {
                    skipNextChangeSound = false;
                    return;
                }

                ChangeSoundHook?.Invoke();
            });
        }
    }
}
