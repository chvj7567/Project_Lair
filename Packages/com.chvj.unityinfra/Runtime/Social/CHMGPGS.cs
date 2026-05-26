#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.Events;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Google Play Games Services 매니저. Android only.
    /// 로그인, 클라우드 저장, 업적, 리더보드, 이벤트 지원.
    ///
    /// GPGS Plugin v2.x 기준 (v1의 PlayGamesClientConfiguration은 v2에서 제거됨).
    /// Init은 Activate()만 호출하고, web client ID / 이메일 요청은 Android google-services
    /// 리소스(Window > Google Play Games > Setup)에서 주입한다.
    ///
    /// v2에는 명시적 SignOut() API가 없다. 로그아웃은 사용자가 Play Games 시스템 앱에서
    /// 직접 수행해야 한다. 따라서 본 클래스도 Logout() 메서드를 제공하지 않는다.
    /// </summary>
    public class CHMGPGS : CHSingletonStatic<CHMGPGS>
    {
        private bool _initialized = false;

        /// <summary>디버그 로그 출력 여부. Init 호출 전에 설정해야 반영된다. 기본 true.</summary>
        public bool DebugLogEnabled { get; set; } = true;

        /// <summary>현재 로그인되어 있는지 여부.</summary>
        public bool IsAuthenticated =>
            PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();

        private void Init()
        {
            if (_initialized) return;
            _initialized = true;
            PlayGamesPlatform.DebugLogEnabled = DebugLogEnabled;
            PlayGamesPlatform.Activate();
        }

        /// <summary>silent 로그인 시도. v2는 앱 시작 시 자동 시도하므로 그 결과를 회수하는 용도.</summary>
        public void Login(Action<bool, ILocalUser> onLoginSuccess = null)
        {
            Init();
            PlayGamesPlatform.Instance.Authenticate(status =>
                onLoginSuccess?.Invoke(status == SignInStatus.Success, Social.localUser));
        }

        /// <summary>강제 다이얼로그 인증. silent 실패 후 사용자 명시적 로그인 트리거.</summary>
        public void ManuallyLogin(Action<bool, ILocalUser> onLoginSuccess = null)
        {
            Init();
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
                onLoginSuccess?.Invoke(status == SignInStatus.Success, Social.localUser));
        }

        /// <summary>서버 사이드 OAuth auth code 발급. 백엔드에서 access/refresh token으로 교환.</summary>
        public void RequestServerSideAccess(bool forceRefreshToken, Action<bool, string> onAuthCode = null)
        {
            if (!IsAuthenticated) { onAuthCode?.Invoke(false, null); return; }
            PlayGamesPlatform.Instance.RequestServerSideAccess(forceRefreshToken, code =>
                onAuthCode?.Invoke(!string.IsNullOrEmpty(code), code));
        }

        public string GetUserId() => IsAuthenticated ? PlayGamesPlatform.Instance.GetUserId() : null;
        public string GetUserDisplayName() => IsAuthenticated ? PlayGamesPlatform.Instance.GetUserDisplayName() : null;
        public string GetUserImageUrl() => IsAuthenticated ? PlayGamesPlatform.Instance.GetUserImageUrl() : null;

        // ---- Saved Games ----

        public void SaveCloud(string fileName, string saveData, Action<bool> onCloudSaved = null)
        {
            if (!IsAuthenticated) { onCloudSaved?.Invoke(false); return; }
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(fileName,
                DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLastKnownGood, (status, game) =>
                {
                    if (status == SavedGameRequestStatus.Success)
                    {
                        var update = new SavedGameMetadataUpdate.Builder().Build();
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(saveData);
                        PlayGamesPlatform.Instance.SavedGame.CommitUpdate(game, update, bytes, (status2, game2) =>
                            onCloudSaved?.Invoke(status2 == SavedGameRequestStatus.Success));
                    }
                    else onCloudSaved?.Invoke(false);
                });
        }

        public void LoadCloud(string fileName, Action<bool, string> onCloudLoaded = null)
        {
            if (!IsAuthenticated) { onCloudLoaded?.Invoke(false, null); return; }
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(fileName,
                DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLastKnownGood, (status, game) =>
                {
                    if (status == SavedGameRequestStatus.Success)
                    {
                        PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(game, (status2, loadedData) =>
                        {
                            if (status2 == SavedGameRequestStatus.Success)
                                onCloudLoaded?.Invoke(true, System.Text.Encoding.UTF8.GetString(loadedData));
                            else onCloudLoaded?.Invoke(false, null);
                        });
                    }
                    else onCloudLoaded?.Invoke(false, null);
                });
        }

        public void DeleteCloud(string fileName, Action<bool> onCloudDeleted = null)
        {
            if (!IsAuthenticated) { onCloudDeleted?.Invoke(false); return; }
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(fileName,
                DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, (status, game) =>
                {
                    if (status == SavedGameRequestStatus.Success)
                    {
                        PlayGamesPlatform.Instance.SavedGame.Delete(game);
                        onCloudDeleted?.Invoke(true);
                    }
                    else onCloudDeleted?.Invoke(false);
                });
        }

        // ---- Achievements ----

        public void ShowAchievementUI() => Social.ShowAchievementsUI();

        public void UnlockAchievement(string gpgsId, Action<bool> onUnlocked = null) =>
            Social.ReportProgress(gpgsId, 100, success => onUnlocked?.Invoke(success));

        public void IncrementAchievement(string gpgsId, int steps, Action<bool> onUnlocked = null) =>
            PlayGamesPlatform.Instance.IncrementAchievement(gpgsId, steps, success => onUnlocked?.Invoke(success));

        // ---- Leaderboards ----

        public void ShowAllLeaderboardUI() => Social.ShowLeaderboardUI();

        public void ShowTargetLeaderboardUI(string gpgsId) =>
            ((PlayGamesPlatform)Social.Active).ShowLeaderboardUI(gpgsId);

        public void ReportLeaderboard(string gpgsId, long score, Action<bool> onReported = null) =>
            Social.ReportScore(score, gpgsId, success => onReported?.Invoke(success));

        public void LoadAllLeaderboardArray(string gpgsId, Action<IScore[]> onLoaded = null) =>
            Social.LoadScores(gpgsId, onLoaded);

        public void LoadCustomLeaderboardArray(string gpgsId, int rowCount, LeaderboardStart leaderboardStart,
            LeaderboardTimeSpan leaderboardTimeSpan, Action<bool, LeaderboardScoreData> onLoaded = null)
        {
            PlayGamesPlatform.Instance.LoadScores(gpgsId, leaderboardStart, rowCount,
                LeaderboardCollection.Public, leaderboardTimeSpan, data =>
                    onLoaded?.Invoke(data.Status == ResponseStatus.Success, data));
        }

        public void LoadUsers(IScore[] scores, Action<IUserProfile[]> onUserProfiles)
        {
            List<string> userIds = new List<string>();
            foreach (IScore score in scores) userIds.Add(score.userID);
            Social.LoadUsers(userIds.ToArray(), users => onUserProfiles?.Invoke(users));
        }

        // ---- Events ----

        public void IncrementEvent(string gpgsId, uint steps) =>
            PlayGamesPlatform.Instance.Events.IncrementEvent(gpgsId, steps);

        public void LoadEvent(string gpgsId, Action<bool, IEvent> onEventLoaded = null) =>
            PlayGamesPlatform.Instance.Events.FetchEvent(DataSource.ReadCacheOrNetwork, gpgsId, (status, iEvent) =>
                onEventLoaded?.Invoke(status == ResponseStatus.Success, iEvent));

        public void LoadAllEvent(Action<bool, List<IEvent>> onEventsLoaded = null) =>
            PlayGamesPlatform.Instance.Events.FetchAllEvents(DataSource.ReadCacheOrNetwork, (status, events) =>
                onEventsLoaded?.Invoke(status == ResponseStatus.Success, events));
    }
}
#endif
