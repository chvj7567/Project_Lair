using System;
using UnityEngine;

namespace Lair.Net
{
    //# deviceId(GUID 1회 생성)와 JWT, accountId 를 PlayerPrefs 에 저장. Application.dataPath 쓰기 금지(과거 사고 회피).
    //# (2026-07-14 Firebase: idToken/uid/refreshToken 추가, AccountId 사문화)
    public static class AuthTokenStore
    {
        private const string DeviceIdKey = "Lair.Net.DeviceId";
        private const string TokenKey = "Lair.Net.Token";
        private const string AccountIdKey = "Lair.Net.AccountId";
        private const string RefreshTokenKey = "Lair.Net.RefreshToken";
        private const string UidKey = "Lair.Net.Uid";               //# Firebase localId

        public static string GetOrCreateDeviceId()
        {
            string id = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceIdKey, id);
                PlayerPrefs.Save();
            }
            return id;
        }

        public static string Token => PlayerPrefs.GetString(TokenKey, string.Empty);

        public static bool HasToken => string.IsNullOrEmpty(Token) == false;

        public static void SaveToken(string token)
        {
            PlayerPrefs.SetString(TokenKey, token ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static void ClearToken()
        {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }

        //# 인증 응답의 accountId — 랭킹 "내 행" 식별에 사용(Delta 6 / 기획서 §4·§8). 미설정이면 0.
        public static long AccountId => long.TryParse(PlayerPrefs.GetString(AccountIdKey, string.Empty), out long id) ? id : 0;

        public static bool HasAccountId => AccountId != 0;

        public static void SaveAccountId(long accountId)
        {
            PlayerPrefs.SetString(AccountIdKey, accountId.ToString());
            PlayerPrefs.Save();
        }

        public static string RefreshToken => PlayerPrefs.GetString(RefreshTokenKey, string.Empty);

        public static void SaveRefreshToken(string t)
        {
            PlayerPrefs.SetString(RefreshTokenKey, t ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static string Uid => PlayerPrefs.GetString(UidKey, string.Empty);

        public static bool HasUid => string.IsNullOrEmpty(Uid) == false;

        public static void SaveUid(string uid)
        {
            PlayerPrefs.SetString(UidKey, uid ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
