using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// MonoBehaviour 싱글톤 베이스. 첫 <see cref="Instance"/> 접근 시 씬에서 검색하고,
    /// 없으면 GameObject를 자동 생성해 <c>DontDestroyOnLoad</c> 처리한다.
    /// 컴포넌트 사이클(Update/OnEnable 등)이 필요한 매니저(예: <c>CHMSound</c>)에 사용.
    /// MonoBehaviour가 필요 없다면 <see cref="CHSingletonStatic{T}"/>를 사용한다.
    /// </summary>
    public class CHSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static object _lock = new object();
        private static bool applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindAnyObjectByType(typeof(T));

                        if (_instance == null)
                        {
                            GameObject obj = new GameObject();
                            _instance = obj.AddComponent(typeof(T)) as T;
                            obj.name = typeof(T).ToString();

                            DontDestroyOnLoad(obj);
                        }
                    }

                    return _instance;
                }
            }
        }

        // 앱 종료 시점에만 Instance를 봉인. OnDestroy에서 봉인하면
        // 명시적 Destroy나 Domain Reload off 환경에서 재접근이 막힌다.
        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }
    }
}
