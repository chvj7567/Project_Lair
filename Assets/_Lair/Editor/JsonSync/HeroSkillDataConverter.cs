using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Lair.Character;
using UnityEngine;

namespace Lair.EditorTools
{
    //# HeroSkillData 폴리모픽 직렬화 — $type 으로 구상 SO 타입 기록/복원 + fileName 보존.
    //# 가변 상태 없는 데이터 SO 라 CreateInstance 후 Populate 안전.
    //# 베이스 HeroSkillData 의 private [SerializeField] _displayName 까지 포함하려면 상속 계층을 walk 하는 resolver 사용.
    public class HeroSkillDataConverter : JsonConverter<HeroSkillData>
    {
        private readonly JsonSerializer _inner;

        public HeroSkillDataConverter()
        {
            _inner = new JsonSerializer { ContractResolver = new InheritedSerializeFieldResolver() };
        }

        public override HeroSkillData ReadJson(JsonReader reader, Type objectType,
            HeroSkillData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            JObject jo = JObject.Load(reader);
            string typeName = jo["$type"]?.Value<string>();
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = FindSkillType(typeName);
            if (type == null)
                throw new JsonException($"[HeroSkillDataConverter] 알 수 없는 스킬 타입: {typeName}");

            HeroSkillData skill = (HeroSkillData)ScriptableObject.CreateInstance(type);
            using (JsonReader jr = jo.CreateReader())
                _inner.Populate(jr, skill);
            return skill;
        }

        public override void WriteJson(JsonWriter writer, HeroSkillData value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            JObject jo = JObject.FromObject(value, _inner);
            jo.AddFirst(new JProperty("fileName", value.name));
            jo.AddFirst(new JProperty("$type", value.GetType().Name));
            jo.WriteTo(writer);
        }

        private static Type FindSkillType(string typeName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType($"Lair.Character.{typeName}");
                if (t != null)
                    return t;
            }
            return null;
        }

        //# UnitySerializeFieldContractResolver 와 동일 규칙이되, 상속 계층 전체의 private [SerializeField] 를 포함한다.
        //# (DefaultContractResolver.GetFields 는 베이스의 private 필드를 누락 — _displayName 이 베이스 선언이라 필요.)
        private class InheritedSerializeFieldResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<JsonProperty> props = new List<JsonProperty>();
                HashSet<string> seen = new HashSet<string>();
                for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                {
                    FieldInfo[] fields = t.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        if (seen.Contains(field.Name))
                            continue;
                        bool isPublic = field.IsPublic;
                        bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;
                        if (isPublic == false && hasSerializeField == false)
                            continue;

                        JsonProperty prop = base.CreateProperty(field, memberSerialization);
                        prop.Readable = true;
                        prop.Writable = true;
                        prop.PropertyName = field.Name.TrimStart('_');
                        props.Add(prop);
                        seen.Add(field.Name);
                    }
                }
                return props;
            }
        }
    }
}
