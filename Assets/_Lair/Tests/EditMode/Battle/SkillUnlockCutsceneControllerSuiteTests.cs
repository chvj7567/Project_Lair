using System.Collections;
using System.Collections.Generic;
using Lair.Battle;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# [보강] SkillUnlockCutsceneController 본격 스위트 (test-engineer).
    //# 스모크 3건(단일 포맷·다중 순차·빈이름)은 SkillUnlockCutsceneControllerTests.cs.
    //# 여기선 그 너머 — 큐 견고성(진행중 Enqueue·재드레인·null/빈 혼재·5개 순서),
    //# 쉐이크 인자/횟수, Reset 계약, PauseService depth 공유 회귀를 박제한다.
    //#
    //# 설계 노트:
    //#   - Pause/Resume 은 비-virtual + depth accessor 없음 → "Pause 정확히 1회" 는 어떤 seam 으로도
    //#     관측 불가. 따라서 관측 가능한 계약(드레인 후 외부 정지 유지 / 매 PlayCo 시점 정지 상태 /
    //#     Resume depth0 음수 불가)만 단언한다. exact-call-count 락은 production depth accessor 가
    //#     필요하나 본 회귀 범위 밖 — 위 관측치가 실제 회귀(카드픽 정지 과해제)를 잡는다.
    //#   - PauseService.Pause 는 Time.timeScale 을 전역 0 으로 만든다 → SetUp/TearDown 으로 save+restore.
    public class SkillUnlockCutsceneControllerSuiteTests
    {
        //# PlayCo 즉시 완료 + 텍스트 기록. PlayCo 시점 정지상태 샘플링 / Hide 횟수 / one-shot 재진입 훅.
        private class FakeBanner : ISkillUnlockBanner
        {
            public readonly List<string> Played = new();
            public readonly List<bool> PausedAtPlay = new();
            public int HideCount;

            //# PlayCo 시점에 정지상태를 샘플링하기 위한 참조(선택).
            public PauseService PauseProbe;
            //# 진행중 Enqueue 재진입 검증용 — 첫 PlayCo 에서 1회만 Enqueue.
            public SkillUnlockCutsceneController ReentryController;
            public string ReentryName;
            private bool _reentryFired;

            public IEnumerator PlayCo(string text)
            {
                Played.Add(text);
                if (PauseProbe != null)
                    PausedAtPlay.Add(PauseProbe.IsPaused);
                if (ReentryController != null && _reentryFired == false)
                {
                    _reentryFired = true;
                    ReentryController.Enqueue(ReentryName);
                }
                yield break;
            }

            public void HideImmediate() => HideCount++;
        }

        //# 호출 횟수 + 마지막 인자 캡처.
        private class FakeShake : ICameraShake
        {
            public int ShakeCount;
            public float LastDuration;
            public float LastMagnitude;

            public void Shake(float duration, float magnitude)
            {
                ShakeCount++;
                LastDuration = duration;
                LastMagnitude = magnitude;
            }
        }

        private float _prevTimeScale;

        [SetUp]
        public void SetUp()
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            //# Pause 가 timeScale=0 으로 남긴 전역 상태가 다른 테스트로 새지 않게 복원.
            Time.timeScale = _prevTimeScale;
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

        //# ===== 1. 큐 견고성 =====

        [Test]
        public void 진행중_Enqueue_추가분_같은드레인에_순차포함()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            //# 첫 PlayCo 안에서 B 를 1회 Enqueue → 같은 RunQueueCo while 루프가 이어서 처리.
            banner.ReentryController = c;
            banner.ReentryName = "B";

            c.Enqueue("A");
            Pump(c.DrainForTest());

            Assert.AreEqual(2, banner.Played.Count, "진행 중 추가분도 같은 드레인에 포함");
            Assert.AreEqual("영웅의 'A' 스킬 해제", banner.Played[0]);
            Assert.AreEqual("영웅의 'B' 스킬 해제", banner.Played[1]);
            Assert.IsFalse(pause.IsPaused, "전부 처리 후 Resume");
        }

        [Test]
        public void 드레인완료후_재Enqueue_재구동()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("A");
            Pump(c.DrainForTest());
            //# 1차 드레인 후 _running 이 false 로 리셋되어야 2차가 다시 돈다.
            c.Enqueue("B");
            Pump(c.DrainForTest());

            Assert.AreEqual(2, banner.Played.Count, "_running 리셋 안 되면 2차가 안 돎");
            Assert.AreEqual("영웅의 'A' 스킬 해제", banner.Played[0]);
            Assert.AreEqual("영웅의 'B' 스킬 해제", banner.Played[1]);
            Assert.IsFalse(pause.IsPaused);
        }

        [Test]
        public void null과_빈문자_혼재큐_각각_fallback문구()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue(null);
            c.Enqueue("");
            c.Enqueue("광역 노바");
            Pump(c.DrainForTest());

            Assert.AreEqual(3, banner.Played.Count);
            Assert.AreEqual("영웅의 새 스킬 해제", banner.Played[0], "null → fallback");
            Assert.AreEqual("영웅의 새 스킬 해제", banner.Played[1], "빈 문자 → fallback");
            Assert.AreEqual("영웅의 '광역 노바' 스킬 해제", banner.Played[2]);
        }

        [Test]
        public void 다수_5개_큐_순서보존()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            string[] names = { "스킬1", "스킬2", "스킬3", "스킬4", "스킬5" };
            foreach (string n in names)
                c.Enqueue(n);
            Pump(c.DrainForTest());

            Assert.AreEqual(5, banner.Played.Count);
            for (int i = 0; i < names.Length; ++i)
                Assert.AreEqual($"영웅의 '{names[i]}' 스킬 해제", banner.Played[i], $"{i}번 순서 보존");
        }

        //# ===== 2. 정지/재개 — 관측 가능한 계약 =====

        [Test]
        public void 외부정지_공유시_드레인후_외부정지_유지()
        {
            //# 카드픽이 먼저 Pause(depth 1) 한 상태에서 컷인이 끼어든다.
            //# 컷인 드레인이 끝나도 카드픽 정지는 풀리면 안 됨(depth 음수/과해제 회귀).
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            pause.Pause();   //# 외부(카드픽) 정지
            Assert.IsTrue(pause.IsPaused);

            c.Enqueue("A");
            c.Enqueue("B");
            Pump(c.DrainForTest());

            Assert.IsTrue(pause.IsPaused, "컷인 드레인 후에도 외부 정지는 유지(과해제 금지)");
        }

        [Test]
        public void 드레인중_매_PlayCo시점_정지상태()
        {
            //# 컷인 진행 동안(각 배너 재생 시점) 항상 정지 상태여야 — 정지 후 큐 처리, 끝나고 1회 Resume.
            PauseService pause = new();
            FakeBanner banner = new();
            banner.PauseProbe = pause;
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("A");
            c.Enqueue("B");
            c.Enqueue("C");
            Pump(c.DrainForTest());

            Assert.AreEqual(3, banner.PausedAtPlay.Count);
            foreach (bool paused in banner.PausedAtPlay)
                Assert.IsTrue(paused, "각 배너 재생 시점은 정지 상태");
            Assert.IsFalse(pause.IsPaused, "드레인 후 Resume 으로 해제");
        }

        [Test]
        public void DrainForTest_빈Enqueue없이_직접호출_무해()
        {
            //# 큐가 비어 있어도 Pause→(루프 0회)→Resume — 순 정지 잔존 없음, 예외 없음.
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            Assert.DoesNotThrow(() => Pump(c.DrainForTest()));
            Assert.AreEqual(0, banner.Played.Count);
            Assert.IsFalse(pause.IsPaused, "빈 큐 드레인 후 순 정지 잔존 없음");
        }

        //# ===== 2b. PauseService 자체 계약 회귀 (depth 음수 불가) =====

        [Test]
        public void PauseService_depth0에서_Resume_음수안됨()
        {
            //# 컷인 Resume 이 카드픽과 depth 공유 중 과호출돼도 depth 가 음수로 내려가면 안 된다.
            PauseService pause = new();

            Assert.DoesNotThrow(() => pause.Resume());
            Assert.IsFalse(pause.IsPaused, "depth0 Resume 은 no-op");

            //# Pause 1 → Resume 2회 → 두 번째는 no-op, 이후 Pause 1회로 다시 정지 가능(음수 누적 없음).
            pause.Pause();
            pause.Resume();
            pause.Resume();
            Assert.IsFalse(pause.IsPaused);
            pause.Pause();
            Assert.IsTrue(pause.IsPaused, "음수 depth 누적 없어 재정지 정상");
        }

        //# ===== 3. 쉐이크 호출 수 / 인자 =====

        [Test]
        public void 스킬N개_쉐이크N회_스킬당1회()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("A");
            c.Enqueue("B");
            c.Enqueue("C");
            Pump(c.DrainForTest());

            Assert.AreEqual(3, shake.ShakeCount, "스킬당 쉐이크 1회");
            Assert.AreEqual(3, banner.Played.Count);
        }

        [Test]
        public void 쉐이크_인자_0_4초_0_3세기_전달()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            FakeShake shake = new();
            SkillUnlockCutsceneController c = new(pause, shake, banner);

            c.Enqueue("A");
            Pump(c.DrainForTest());

            Assert.AreEqual(0.4f, shake.LastDuration, 0.0001f, "ShakeDuration 0.4");
            Assert.AreEqual(0.3f, shake.LastMagnitude, 0.0001f, "ShakeMagnitude 0.3");
        }

        //# ===== 4. Reset 계약 =====

        [Test]
        public void Reset_큐비움_HideImmediate호출()
        {
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("A");
            c.Enqueue("B");
            c.Reset();
            //# Reset 후 드레인 — 큐가 비워졌으면 아무것도 재생 안 됨.
            Pump(c.DrainForTest());

            Assert.AreEqual(0, banner.Played.Count, "Reset 이 큐를 비움");
            Assert.AreEqual(1, banner.HideCount, "Reset 이 HideImmediate 1회 호출");
        }

        [Test]
        public void Reset_running리셋_후_재Enqueue_정상구동()
        {
            //# Reset 이 _running 도 false 로 → 이후 새 큐가 정상 드레인.
            PauseService pause = new();
            FakeBanner banner = new();
            SkillUnlockCutsceneController c = new(pause, new FakeShake(), banner);

            c.Enqueue("A");
            c.Reset();
            c.Enqueue("B");
            Pump(c.DrainForTest());

            Assert.AreEqual(1, banner.Played.Count);
            Assert.AreEqual("영웅의 'B' 스킬 해제", banner.Played[0]);
        }
    }
}
