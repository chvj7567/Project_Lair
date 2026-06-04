using System.Reflection;

namespace Lair.Tests.EditMode
{
    //# private [SerializeField] 필드 주입 헬퍼 (테스트 전용).
    public static class TestReflection
    {
        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert(f != null, $"필드 미발견: {target.GetType().Name}.{fieldName}");
            f.SetValue(target, value);
        }

        private static void Assert(bool cond, string msg)
        {
            if (cond == false)
                throw new System.Exception(msg);
        }
    }
}
