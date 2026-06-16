using UnityEngine;

namespace Lair.Net
{
    //# 서버 접속 설정 SO — Addressable(EData.NetworkConfig) 로 로드.
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Lair/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [SerializeField] private string _baseUrl = "http://localhost:8080";
        [SerializeField] private int _timeoutSec = 10;

        public string BaseUrl => _baseUrl.TrimEnd('/');
        public int TimeoutSec => _timeoutSec;
    }
}
