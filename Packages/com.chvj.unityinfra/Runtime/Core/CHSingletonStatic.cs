namespace ChvjUnityInfra
{
    /// <summary>
    /// 일반 클래스(POCO) 싱글톤 베이스. Unity 메시지(Update/OnEnable 등)가 필요 없는
    /// 데이터 매니저·로직 매니저용. Lazy 초기화이며 lock 없음(단일 스레드 사용 가정).
    /// 컴포넌트 사이클이 필요하면 <see cref="CHSingleton{T}"/>를 사용한다.
    /// </summary>
    public class CHSingletonStatic<T> where T : new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }
    }
}
