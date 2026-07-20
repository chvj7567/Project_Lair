using NUnit.Framework;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceJsonLoader.Parse 순수 파싱 검증 — 파일IO 없이 문자열 → DTO.
    public class BalanceJsonLoaderParseTests
    {
        [Test]
        public void 유효_JSON은_DTO로_파싱된다()
        {
            string json = "{\"hero\":{\"hp\":1000,\"power\":50,\"range\":1.5,\"cooldown\":1.0,\"moveSpeed\":3.0},\"monsters\":[{\"key\":\"Reaper\",\"stat\":{\"hp\":30,\"power\":5,\"range\":1.0,\"cooldown\":1.2,\"moveSpeed\":2.0},\"spawnPeriod\":9.0}],\"runDuration\":300.0,\"passiveThresholds\":[0.9,0.5],\"activeThresholds\":[30.0]}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(1000, dto.Hero.Hp);
            Assert.AreEqual(1, dto.Monsters.Count);
            Assert.AreEqual("Reaper", dto.Monsters[0].Key);
            Assert.AreEqual(9.0f, dto.Monsters[0].SpawnPeriod);
            Assert.AreEqual(300.0f, dto.RunDuration);
        }

        [Test]
        public void 깨진_JSON은_null을_반환한다()
        {
            Assert.IsNull(BalanceJsonLoader.Parse("{ this is not json"));
        }

        [Test]
        public void 빈문자열은_null을_반환한다()
        {
            Assert.IsNull(BalanceJsonLoader.Parse(null));
            Assert.IsNull(BalanceJsonLoader.Parse(""));
            Assert.IsNull(BalanceJsonLoader.Parse("   "));
        }

        //# 필드 일부 누락 — hero 만 있고 monsters/thresholds/runDuration 없음.
        //# Newtonsoft: 초기자 있는 필드(Monsters=new List)는 유지(빈 리스트), 없는 필드는 기본값(0/null).
        [Test]
        public void 일부필드누락_hero만있는_JSON은_부분DTO로_파싱된다()
        {
            string json = "{\"hero\":{\"hp\":1000,\"power\":50,\"range\":1.5,\"cooldown\":1.0,\"moveSpeed\":3.0}}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(1000, dto.Hero.Hp);
            //# monsters 키 부재 → 초기자(빈 리스트) 유지, null 아님.
            Assert.IsNotNull(dto.Monsters);
            Assert.AreEqual(0, dto.Monsters.Count);
            //# 초기자 없는 필드는 기본값.
            Assert.AreEqual(0f, dto.RunDuration);
            Assert.IsNull(dto.PassiveThresholds);
            Assert.IsNull(dto.ActiveThresholds);
        }

        //# DTO 에 없는 여분 키는 예외 없이 무시(Newtonsoft MissingMemberHandling 기본 Ignore).
        [Test]
        public void 알수없는_여분키는_무시되고_나머지는_파싱된다()
        {
            string json = "{\"hero\":{\"hp\":1000,\"power\":50,\"range\":1.5,\"cooldown\":1.0,\"moveSpeed\":3.0}," +
                          "\"unknownField\":123,\"extraObj\":{\"nope\":true}}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(1000, dto.Hero.Hp);
        }

        //# monsters 빈 배열 → 빈 리스트(null 아님, Count 0).
        [Test]
        public void monsters_빈배열은_빈리스트로_파싱된다()
        {
            BalanceConfigDto dto = BalanceJsonLoader.Parse("{\"monsters\":[]}");
            Assert.IsNotNull(dto);
            Assert.IsNotNull(dto.Monsters);
            Assert.AreEqual(0, dto.Monsters.Count);
        }

        //# 숫자 타입 경계 — int 필드에 소수점 리터럴(30.0)·float 필드에 정수 리터럴(2) 이 안전 변환된다.
        [Test]
        public void 숫자타입경계_정수필드_소수리터럴과_실수필드_정수리터럴()
        {
            string json = "{\"hero\":{\"hp\":30.0,\"power\":50,\"range\":2,\"cooldown\":1.0,\"moveSpeed\":3.0}}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(30, dto.Hero.Hp);
            Assert.AreEqual(50, dto.Hero.Power);
            Assert.AreEqual(2f, dto.Hero.Range);
        }

        //# Parse 는 값 검증을 하지 않는다(음수도 그대로 파싱) — 검증/스킵은 OverlayFromDto 책임.
        [Test]
        public void 음수값도_파싱된다_검증은_오버레이_책임()
        {
            string json = "{\"hero\":{\"hp\":-5,\"power\":-1,\"range\":-1.0,\"cooldown\":-1.0,\"moveSpeed\":-1.0},\"runDuration\":-10}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(-5, dto.Hero.Hp);
            Assert.AreEqual(-10f, dto.RunDuration);
        }

        //# 어떤 비정상 입력에도 throw 하지 않고 null 반환(예외 삼킴) — 호출부 SO fallback 계약.
        [Test]
        public void 어떤_비정상입력도_예외없이_null을_반환한다()
        {
            string[] bads = { "[]", "123", "true", "null", "{ ", "\"just a string\"", "{\"hero\":" };
            foreach (string bad in bads)
            {
                BalanceConfigDto dto = null;
                Assert.DoesNotThrow(() => dto = BalanceJsonLoader.Parse(bad), $"입력 '{bad}' 은 throw 하면 안 된다");
                Assert.IsNull(dto, $"입력 '{bad}' 은 null 이어야 한다");
            }
        }
    }
}
