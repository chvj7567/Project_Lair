using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 프리팹 [SerializeField] 와이어링 회귀 가드용 reflection 헬퍼(테스트 전용).
    //# private 필드는 선언 클래스에만 있으므로 base 체인을 끝까지 걸어 UIBase 의 _backgroundButton 등도 읽는다.
    internal static class PopupWiringAsserts
    {
        //# private 필드를 base 체인까지 탐색해 읽는다 — GetField 는 base 의 private 를 표면화하지 않음.
        public static object GetPrivateField(object target, string fieldName)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo f = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (f != null)
                    return f.GetValue(target);
                type = type.BaseType;
            }
            Assert.Fail($"필드 미발견(선언/상속 전체 탐색): {target.GetType().Name}.{fieldName}");
            return null;
        }

        //# 와이어링이 끊기면 직렬화상 fileID 0 → 로드 시 null. 그 모드를 잡는다.
        public static void AssertWired(object component, string fieldName, string context)
        {
            object value = GetPrivateField(component, fieldName);
            Object unityRef = value as Object;
            Assert.IsTrue(unityRef != null, $"{context}: {fieldName} 와이어링 끊김(null) — 프리팹 재직렬화 클로버 의심");
        }
    }
}
