using System;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# SO→JSON 조립(Build) 엣지 테스트 공용 — 검증 실패 예외에서 위반 줄만 뽑는다(머리말 1줄 + " - " 위반 줄들).
    public static class BuildEdgeTestKit
    {
        public static ChapterDataException Fails(TestDelegate build)
        {
            return Assert.Throws<ChapterDataException>(build);
        }

        //# 위반 줄 목록 — 모아 던지는 메시지의 "\n - " 구분자를 기준으로 머리말을 뺀 나머지.
        public static string[] Violations(ChapterDataException e)
        {
            string[] parts = e.Message.Split(new[] { "\n - " }, StringSplitOptions.None);
            string[] lines = new string[parts.Length - 1];
            Array.Copy(parts, 1, lines, 0, lines.Length);
            return lines;
        }

        public static void AssertFails(TestDelegate build, params string[] fragments)
        {
            ChapterDataException e = Fails(build);
            foreach (string fragment in fragments)
            {
                StringAssert.Contains(fragment, e.Message);
            }
        }
    }
}
