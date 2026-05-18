using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 풀링 대상 마커. <c>CHMPool.Pop</c> 시 자동 부착되며,
    /// <c>poolKey</c>로 어느 풀 소속인지 기억해 GameObject 이름이 바뀌어도 안전히 반환된다.
    /// </summary>
    public class CHPoolable : MonoBehaviour
    {
        [SerializeField, ReadOnly] private string poolKey;
        [SerializeField, ReadOnly] private bool _isUse = true;

        /// <summary>이 인스턴스가 속한 풀의 키(원본 prefab 이름). CHMPool이 설정.</summary>
        public string PoolKey
        {
            get => poolKey;
            internal set => poolKey = value;
        }

        /// <summary>풀에서 꺼내져 사용 중인지 여부. CHMPool이 설정.</summary>
        public bool IsUse
        {
            get => _isUse;
            internal set => _isUse = value;
        }
    }
}
