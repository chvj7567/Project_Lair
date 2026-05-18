using System;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// <see cref="JsonUtility"/>는 최상위 JSON 배열(<c>[ {...}, {...} ]</c>)을 직접 파싱하지 못한다.
    /// 이 헬퍼는 입력을 임시 객체로 감싸 우회한다.
    /// </summary>
    public static class JsonArrayUtility
    {
        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }

        /// <summary>
        /// 최상위 JSON 배열 문자열을 <typeparamref name="T"/> 배열로 파싱한다.
        /// null/빈 문자열은 빈 배열 반환. 잘못된 JSON은 <see cref="JsonUtility"/>가 던지는 예외가 그대로 전파된다.
        /// </summary>
        public static T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<T>();
            }

            string wrapped = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper?.items ?? Array.Empty<T>();
        }
    }
}
