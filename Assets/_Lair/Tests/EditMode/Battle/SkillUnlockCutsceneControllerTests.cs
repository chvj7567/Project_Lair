using System.Collections;
using System.Collections.Generic;
using Lair.Battle;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.Battle
{
    //# SkillUnlockCutsceneController 의 큐 순차 재생 + 정지/재개 + 쉐이크 + 빈 이름 fallback 검증.
    public class SkillUnlockCutsceneControllerTests
    {
        //# PlayCo 가 즉시 끝나는 mock 배너 — 호출된 텍스트 기록.
        private class FakeBanner : ISkillUnlockBanner
        {
            public readonly List<string> Played = new();
            public IEnumerator PlayCo(string text)
            {
                Played.Add(text);
                yield break;
            }
            public void HideImmediate() { }
        }

        private class FakeShake : ICameraShake
        {
            public int ShakeCount;
            public void Shake(float duration, float magnitude) => ShakeCount++;
        }

        //# IEnumerator 를 끝까지 수동 펌핑(중첩 yield return IEnumerator 포함).
        private static void Pump(IEnumerator co)
        {
            Stack<IEnumerator> stack = new();
            stack.Push(co);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is IEnumerator inner)
                        stack.Push(inner);
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        [Test]
        public void 단일_해금_시_포맷된_텍스트로_배너_재생_및_쉐이크_1회()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("회전 블레이드");
            Pump(c.DrainForTest());

            Assert.AreEqual(1, banner.Played.Count);
            Assert.AreEqual("영웅의 '회전 블레이드' 스킬 해제", banner.Played[0]);
            Assert.AreEqual(1, shake.ShakeCount);
        }

        [Test]
        public void 다중_해금_큐_순차_재생_후_정지_재개_1쌍()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("A");
            c.Enqueue("B");
            Pump(c.DrainForTest());

            Assert.AreEqual(2, banner.Played.Count);
            Assert.AreEqual("영웅의 'A' 스킬 해제", banner.Played[0]);
            Assert.AreEqual("영웅의 'B' 스킬 해제", banner.Played[1]);
            Assert.IsFalse(pause.IsPaused, "큐 드레인 후 Resume 으로 정지 해제되어야 함");
        }

        [Test]
        public void 빈_이름은_fallback_문구로_재생()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("");
            Pump(c.DrainForTest());

            Assert.AreEqual("영웅의 새 스킬 해제", banner.Played[0]);
        }
    }
}
