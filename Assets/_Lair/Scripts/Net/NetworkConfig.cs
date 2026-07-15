using UnityEngine;

namespace Lair.Net
{
    //# 접속 설정 SO — Addressable(EData.NetworkConfig) 로 로드. (2026-07-14 Firebase 피벗)
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Lair/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [SerializeField] private string _baseUrl = "http://localhost:8080";
        [SerializeField] private string _firebaseApiKey = "";
        [SerializeField] private string _firebaseProjectId = "";
        [SerializeField] private int _timeoutSec = 10;

        //# (사문화 — LairApiClient 하위호환. Firebase 는 apiKey/projectId 사용)
        public string BaseUrl => _baseUrl.TrimEnd('/');
        public string FirebaseApiKey => _firebaseApiKey;
        public string FirebaseProjectId => _firebaseProjectId;
        public int TimeoutSec => _timeoutSec;
    }
}
