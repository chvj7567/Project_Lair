using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Application.logMessageReceived를 구독해 <see cref="logText"/>에 로그 내용을 표시한다.
    /// 디바이스 빌드에서 콘솔이 없을 때 게임 내 디버그 표시용. Warning은 표시하지 않음.
    /// </summary>
    public class CHDebugLog : MonoBehaviour
    {
        public CHText logText;

        [ReadOnly]
        [SerializeField] private int _logCount = 0;

        private void Awake()
        {
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Warning) return;

            _logCount++;
            string line = $"<{_logCount}>[{type}] : {logString}";

            if (logText != null)
            {
                logText.SetPlusString(line);
                logText.SetPlusString("\n");
            }
        }
    }
}
