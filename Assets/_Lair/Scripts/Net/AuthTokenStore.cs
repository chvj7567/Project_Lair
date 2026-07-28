using System;
using UnityEngine;

namespace Lair.Net
{
    //# deviceId(GUID 1회 생성)와 Firebase uid 를 PlayerPrefs 에 저장. Application.dataPath 쓰기 금지(과거 사고 회피).
    //# deviceId 는 인증과 무관하다 — 자동 표시명 "영주 #xxxx" 의 시드로만 쓰인다(VillageViewModel).
    //# 자격증명(idToken·refreshToken)은 Firebase SDK 가 영속화한다 — 여기서 관리하지 않는다.
    public static class AuthTokenStore
    {
        private const string DeviceIdKey = "Lair.Net.DeviceId";
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

        //# Firebase 계정 식별자 — 랭킹 "내 행" 매칭 키.
        public static string Uid => PlayerPrefs.GetString(UidKey, string.Empty);

        public static bool HasUid => string.IsNullOrEmpty(Uid) == false;

        public static void SaveUid(string uid)
        {
            PlayerPrefs.SetString(UidKey, uid ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
