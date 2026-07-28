#if UNITY_INFRA_FIREBASE
using System.Threading.Tasks;
using Firebase;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Firebase 초기화 매니저. FirebaseApp 의존성 체크를 1회 보장한다.
    /// 도메인(세이브/랭킹 스키마)을 알지 못한다 — 그건 게임 코드 소관.
    /// Tools/ChvjUnityInfra/Settings > Firebase 탭에서 모듈 토글.
    /// </summary>
    public class CHMFirebase : CHSingletonStatic<CHMFirebase>
    {
        private Task<bool> _initTask;

        /// <summary>초기화가 끝나고 Firebase 사용 가능한 상태인지.</summary>
        public bool IsReady { get; private set; }

        /// <summary>의존성 체크 1회 보장. 중복 호출은 같은 Task 를 공유한다. 실패 시 false.</summary>
        public Task<bool> EnsureReadyAsync()
        {
            if (_initTask == null)
            {
                _initTask = InitAsync();
            }
            return _initTask;
        }

        private async Task<bool> InitAsync()
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogWarning($"[CHMFirebase] 의존성 사용 불가: {status}");
                IsReady = false;
                return false;
            }
            IsReady = true;
            return true;
        }
    }
}
#endif
