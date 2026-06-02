using NUnit.Framework;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class CardEnumCodegenTests
    {
        private const string Sample =
            "namespace Lair.Data\n" +
            "{\n" +
            "    public enum ECardId\n" +
            "    {\n" +
            "        WispHpBoost,\n" +
            "        SpawnerHaste,\n" +
            "        //# <card-editor:insert>\n" +
            "    }\n" +
            "}\n";

        //# 신규 ID 가 마커 줄 바로 위에 삽입된다.
        [Test]
        public void InsertCardId_마커_위에_삽입()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "NewCard");
            int insertIdx = result.IndexOf("NewCard,");
            int markerIdx = result.IndexOf("//# <card-editor:insert>");
            Assert.Greater(insertIdx, 0);
            Assert.Less(insertIdx, markerIdx, "신규 ID 는 마커 줄 위에 와야 한다");
        }

        //# 기존 값은 그대로 보존된다 (인덱스 시프트 없음).
        [Test]
        public void InsertCardId_기존값_보존()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "NewCard");
            Assert.Less(result.IndexOf("WispHpBoost,"), result.IndexOf("SpawnerHaste,"));
            Assert.Less(result.IndexOf("SpawnerHaste,"), result.IndexOf("NewCard,"));
        }

        //# 중복 ID 는 예외.
        [Test]
        public void InsertCardId_중복_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, "WispHpBoost"));
        }

        //# 유효하지 않은 C# 식별자는 예외.
        [Test]
        public void InsertCardId_잘못된_식별자_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, "1Bad Name"));
        }

        //# 마커가 없으면 예외 (파일 구조 손상 방지).
        [Test]
        public void InsertCardId_마커없음_예외()
        {
            string noMarker = "public enum ECardId { WispHpBoost, }";
            Assert.Throws<System.InvalidOperationException>(
                () => CardEnumCodegen.InsertCardId(noMarker, "NewCard"));
        }

        //# ── 엣지 보강 (test-engineer) ──────────────────────────────

        //# 마커 줄의 들여쓰기를 신규 줄이 그대로 따른다. 마커 indent 와 멤버 indent 를
        //# 일부러 다르게 둔 샘플로, 신규 줄이 멤버(8칸)가 아닌 마커(2칸)를 따르는지 변별한다.
        [Test]
        public void InsertCardId_마커_들여쓰기_보존()
        {
            string sample =
                "    public enum ECardId\n" +
                "    {\n" +
                "        WispHpBoost,\n" +
                "  //# <card-editor:insert>\n" +
                "    }\n";
            string result = CardEnumCodegen.InsertCardId(sample, "NewCard");
            Assert.IsTrue(result.Contains("\n  NewCard,\n"),
                "신규 줄은 마커 줄의 들여쓰기(2칸)를 그대로 따라야 한다");
            Assert.IsFalse(result.Contains("        NewCard,"),
                "멤버 들여쓰기(8칸)가 아닌 마커 들여쓰기를 따라야 한다");
        }

        //# 연속 2회 삽입 — InsertCardId 결과에 다시 InsertCardId. 둘 다 마커 위, 삽입 순서 보존.
        [Test]
        public void InsertCardId_연속_2회_삽입_순서보존()
        {
            string once = CardEnumCodegen.InsertCardId(Sample, "FirstCard");
            string twice = CardEnumCodegen.InsertCardId(once, "SecondCard");

            int firstIdx = twice.IndexOf("FirstCard,");
            int secondIdx = twice.IndexOf("SecondCard,");
            int markerIdx = twice.IndexOf("//# <card-editor:insert>");

            Assert.Greater(firstIdx, 0);
            Assert.Greater(secondIdx, 0);
            Assert.Less(firstIdx, secondIdx, "먼저 삽입한 FirstCard 가 위에 와야 한다");
            Assert.Less(secondIdx, markerIdx, "둘 다 마커 줄 위에 와야 한다");
        }

        //# 선행 언더스코어 식별자(_Foo)는 유효 — 마커 위에 삽입된다.
        [Test]
        public void InsertCardId_선행_언더스코어_허용()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "_Foo");
            int insertIdx = result.IndexOf("_Foo,");
            int markerIdx = result.IndexOf("//# <card-editor:insert>");
            Assert.Greater(insertIdx, 0);
            Assert.Less(insertIdx, markerIdx, "_Foo 는 마커 줄 위에 삽입돼야 한다");
        }

        //# 숫자를 포함한 식별자(Foo2)는 유효 — 마커 위에 삽입된다 (첫 글자만 비숫자면 OK).
        [Test]
        public void InsertCardId_숫자_포함_허용()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "Foo2");
            int insertIdx = result.IndexOf("Foo2,");
            int markerIdx = result.IndexOf("//# <card-editor:insert>");
            Assert.Greater(insertIdx, 0);
            Assert.Less(insertIdx, markerIdx, "Foo2 는 마커 줄 위에 삽입돼야 한다");
        }

        //# 빈 문자열은 예외.
        [Test]
        public void InsertCardId_빈문자열_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, ""));
        }

        //# 공백 문자열은 예외 (식별자 정규식 불일치).
        [Test]
        public void InsertCardId_공백_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, "   "));
        }

        //# null 은 예외 (IsNullOrEmpty 가드).
        [Test]
        public void InsertCardId_null_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, null));
        }

        //# 부분 일치 오탐 방지 — 기존 WispHpBoost 만 있을 때 부분문자열 Wisp 는 중복이 아니다.
        //# ContainsMember 정규식이 단어 경계(라인 단위 ^\s*id\s*(,|=|$))로 동작해 부분 매칭을 막는지 검증.
        [Test]
        public void InsertCardId_부분문자열은_중복아님_허용()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "Wisp");
            int insertIdx = result.IndexOf("\n        Wisp,");
            int markerIdx = result.IndexOf("//# <card-editor:insert>");
            Assert.Greater(insertIdx, 0, "Wisp 는 WispHpBoost 의 부분문자열이지만 중복이 아니므로 삽입돼야 한다");
            Assert.Less(insertIdx, markerIdx, "Wisp 는 마커 줄 위에 삽입돼야 한다");
        }
    }
}
