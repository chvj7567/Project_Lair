using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Lair.Card;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class EffectConverterTests
    {
        //# BerserkEffect 의 $type 필드가 JSON 에 기록되는지 확인
        [Test]
        public void BerserkEffect_Export_포함TypeField()
        {
            ICardEffect effect = new BerserkEffect();
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);

            Assert.IsTrue(json.Contains("\"$type\""), $"$type 필드 없음: {json}");
            Assert.IsTrue(json.Contains("BerserkEffect"), $"타입명 없음: {json}");
        }

        //# 직렬화 → 역직렬화 후 구상 타입 보존
        [Test]
        public void BerserkEffect_RoundTrip_타입보존()
        {
            ICardEffect effect = new BerserkEffect();
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsInstanceOf<BerserkEffect>(result);
        }

        //# [SerializeField] private float _duration 기본값이 JSON 을 거쳐도 보존
        [Test]
        public void BerserkEffect_RoundTrip_duration기본값보존()
        {
            ICardEffect effect = new BerserkEffect(); //# _duration 기본값 15f
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            float duration = (float)typeof(BerserkEffect)
                .GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(result);
            Assert.AreEqual(15f, duration, 0.001f);
        }

        //# 기본값이 아닌 값으로 직렬화 후 역직렬화해도 필드 값 보존 (기본값 우연 통과 방지)
        [Test]
        public void BerserkEffect_RoundTrip_duration비기본값보존()
        {
            BerserkEffect effect = new BerserkEffect();
            typeof(BerserkEffect)
                .GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(effect, 99f);
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            float duration = (float)typeof(BerserkEffect)
                .GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(result);
            Assert.AreEqual(99f, duration, 0.001f);
        }

        //# WispHpBoostEffect — BerserkEffect 외 다른 타입도 [SerializeField] 필드 round-trip
        [Test]
        public void WispHpBoostEffect_RoundTrip_hpMul보존()
        {
            ICardEffect effect = new WispHpBoostEffect(); //# _hpMul 기본값 1.5f
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsInstanceOf<WispHpBoostEffect>(result);
            float hpMul = (float)typeof(WispHpBoostEffect)
                .GetField("_hpMul", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(result);
            Assert.AreEqual(1.5f, hpMul, 0.001f);
        }

        //# [SerializeField] 필드 없는 Effect 타입 — $type 만 있는 JSON 으로 round-trip
        [Test]
        public void SpawnWispsEffect_RoundTrip_타입보존()
        {
            ICardEffect effect = new SpawnWispsEffect(); //# [SerializeField] 필드 없음
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsInstanceOf<SpawnWispsEffect>(result);
        }

        //# Effect 없는 카드 (null) 도 Export/Import 가능
        [Test]
        public void NullEffect_RoundTrip_null반환()
        {
            ICardEffect effect = null;
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsNull(result);
        }

        //# 알 수 없는 $type 값이면 JsonException 발생 (기획서 §5.2)
        [Test]
        public void 알수없는타입_역직렬화_JsonException발생()
        {
            string json = "{\"$type\":\"NonExistentEffect\"}";
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            Assert.Throws<JsonException>(() =>
                JsonConvert.DeserializeObject<ICardEffect>(json, settings));
        }

        //# $type 필드 자체가 없는 JSON 은 null 반환 (예외 없이 안전 처리)
        [Test]
        public void Type필드없는JSON_역직렬화_null반환()
        {
            string json = "{\"duration\":15.0}"; //# $type 누락
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsNull(result);
        }

        //# Export 시 JSON 에 $type 값이 네임스페이스 없이 클래스명만 기록 (기획서 §5.2)
        [Test]
        public void Export_타입명_네임스페이스제외클래스명만포함()
        {
            ICardEffect effect = new FrenzyEffect();
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);

            Assert.IsTrue(json.Contains("\"FrenzyEffect\""), $"클래스명 불일치: {json}");
            Assert.IsFalse(json.Contains("Lair.Card.FrenzyEffect"), $"네임스페이스 노출됨: {json}");
        }
    }
}
