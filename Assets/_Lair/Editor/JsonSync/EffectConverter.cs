using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lair.Card;

namespace Lair.EditorTools
{
    //# ICardEffect 폴리모픽 직렬화 — $type 필드로 구상 타입을 기록/복원.
    public class EffectConverter : JsonConverter<ICardEffect>
    {
        private readonly JsonSerializer _inner;

        public EffectConverter()
        {
            _inner = new JsonSerializer
            {
                ContractResolver = new UnitySerializeFieldContractResolver()
            };
        }

        public override ICardEffect ReadJson(JsonReader reader, Type objectType,
            ICardEffect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject jo = JObject.Load(reader);
            string typeName = jo["$type"]?.Value<string>();
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = FindEffectType(typeName);
            if (type == null)
                throw new JsonException($"[EffectConverter] 알 수 없는 Effect 타입: {typeName}");

            ICardEffect effect = (ICardEffect)Activator.CreateInstance(type);
            using (JsonReader jr = jo.CreateReader())
                _inner.Populate(jr, effect);
            return effect;
        }

        public override void WriteJson(JsonWriter writer, ICardEffect value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JObject jo = JObject.FromObject(value, _inner);
            jo.AddFirst(new JProperty("$type", value.GetType().Name));
            jo.WriteTo(writer);
        }

        private static Type FindEffectType(string typeName)
        {
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType($"Lair.Card.{typeName}");
                if (t != null)
                    return t;
            }
            return null;
        }
    }
}
