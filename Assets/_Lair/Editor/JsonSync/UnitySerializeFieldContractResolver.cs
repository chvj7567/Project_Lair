using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Lair.EditorTools
{
    //# [SerializeField] private 필드를 존중하는 ContractResolver.
    //# 필드명 앞의 _ 를 제거해 JSON 키를 깔끔하게 만든다 (예: _duration → "duration").
    public class UnitySerializeFieldContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            List<JsonProperty> props = new List<JsonProperty>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                bool isPublic = field.IsPublic;
                bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;
                if (isPublic == false && hasSerializeField == false)
                    continue;

                JsonProperty prop = base.CreateProperty(field, memberSerialization);
                prop.Readable = true;
                prop.Writable = true;
                prop.PropertyName = field.Name.TrimStart('_');
                props.Add(prop);
            }
            return props;
        }
    }
}
