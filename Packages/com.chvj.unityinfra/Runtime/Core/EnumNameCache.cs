using System;
using System.Collections.Generic;

namespace ChvjUnityInfra
{
    /// <summary>
    /// enum 값 → 이름 문자열 캐시. 타입(TEnum)당 1회 전체 값을 순회해 구축하고,
    /// 이후 조회는 구체 타입 Dictionary&lt;TEnum, string&gt; 히트라 박싱이 없다.
    /// CHMResource 의 Enum.ToString() 키 계약을 다른 매니저(CHMUI 등)에도 동일하게 적용하기 위한 공용 헬퍼.
    /// </summary>
    public static class EnumNameCache<TEnum> where TEnum : struct, Enum
    {
        private static readonly Dictionary<TEnum, string> _names = Build();

        private static Dictionary<TEnum, string> Build()
        {
            Dictionary<TEnum, string> map = new Dictionary<TEnum, string>();
            //# Enum.GetValues 순회 자체는 박싱을 동반하지만 static 초기화 시 타입당 1회만 발생.
            foreach (TEnum value in (TEnum[])Enum.GetValues(typeof(TEnum)))
            {
                if (map.ContainsKey(value) == false)
                {
                    map[value] = value.ToString();
                }
            }
            return map;
        }

        /// <summary>
        /// 정의된 enum 값이면 캐시 히트(박싱 없음). Flags 조합값 등 캐시에 없는 값만 ToString() fallback.
        /// </summary>
        public static string Get(TEnum value)
        {
            if (_names.TryGetValue(value, out string name))
            {
                return name;
            }

            return value.ToString();
        }
    }
}
