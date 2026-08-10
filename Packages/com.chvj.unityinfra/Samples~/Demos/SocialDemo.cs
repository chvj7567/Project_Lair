#if UNITY_INFRA_SOCIAL && UNITY_ANDROID
using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMGPGS 데모 (Android only).
    /// 사전 준비:
    /// 1. Google Play Games Plugin for Unity 임포트
    /// 2. Tools > ChvjUnityInfra > Settings > Social 탭 → Use GPGS ✓
    /// 3. Window > Google Play Games > Setup > Android setup 으로 SDK 셋업
    /// 4. Google Play Console에서 게임 등록 + 업적/리더보드/이벤트 ID 발급
    /// </summary>
    public class SocialDemo : MonoBehaviour
    {
        // 게임 측에서 발급받은 GPGS ID들 (예시값)
        private const string ACHIEVEMENT_FIRST_WIN = "CgkI__example_achievement_id";
        private const string LEADERBOARD_HIGH_SCORE = "CgkI__example_leaderboard_id";
        private const string SAVE_FILE_NAME = "player_save.dat";

        // UI 버튼에서 호출
        public void OnClickLogin()
        {
            CHMGPGS.Instance.Login((success, user) =>
            {
                if (success)
                {
                    Debug.Log($"[SocialDemo] 로그인 성공: {user.userName} ({user.id})");
                }
                else
                {
                    Debug.LogWarning("[SocialDemo] 로그인 실패");
                }
            });
        }

        public void OnClickLogout() => CHMGPGS.Instance.Logout();

        public void OnClickSaveCloud()
        {
            string saveJson = "{\"level\":10,\"gold\":1000}"; // 게임 상태 직렬화
            CHMGPGS.Instance.SaveCloud(SAVE_FILE_NAME, saveJson, success =>
            {
                Debug.Log($"[SocialDemo] 클라우드 저장: {success}");
            });
        }

        public void OnClickLoadCloud()
        {
            CHMGPGS.Instance.LoadCloud(SAVE_FILE_NAME, (success, data) =>
            {
                if (success)
                {
                    Debug.Log($"[SocialDemo] 클라우드 로드: {data}");
                    // JsonUtility.FromJson<SaveData>(data);
                }
            });
        }

        public void OnClickShowAchievements() => CHMGPGS.Instance.ShowAchievementUI();

        public void OnClickUnlockAchievement()
        {
            CHMGPGS.Instance.UnlockAchievement(ACHIEVEMENT_FIRST_WIN, success =>
            {
                Debug.Log($"[SocialDemo] 업적 달성: {success}");
            });
        }

        public void OnClickShowLeaderboard() => CHMGPGS.Instance.ShowAllLeaderboardUI();

        public void OnClickReportScore()
        {
            long score = 12345;
            CHMGPGS.Instance.ReportLeaderboard(LEADERBOARD_HIGH_SCORE, score, success =>
            {
                Debug.Log($"[SocialDemo] 점수 등록: {success}");
            });
        }
    }
}
#endif
