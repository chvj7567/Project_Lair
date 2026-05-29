using Newtonsoft.Json;

namespace Lair.EditorTools
{
    //# Newtonsoft.Json 설정 팩토리 — EffectConverter 포함, Indented 출력.
    public static class JsonSyncSettings
    {
        public static JsonSerializerSettings Build()
        {
            return new JsonSerializerSettings
            {
                Formatting        = Formatting.Indented,
                Converters        = { new EffectConverter() },
                NullValueHandling = NullValueHandling.Include
            };
        }
    }
}
