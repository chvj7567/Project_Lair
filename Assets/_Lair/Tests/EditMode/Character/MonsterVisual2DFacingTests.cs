using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# MonsterVisual2D — 카메라 빌보드 + 좌우 반전 + 오버레이 프레임 순서 소유(Rule 02 §10, monster-2d-conversion.md §5.8·§6.3.6·§9).
    //# 신규 루트 파사드 — gameplay-programmer 시드 테스트 없음. test-engineer 가 처음부터 커버(회귀 대상 없음, 신규 커버리지).
    public class MonsterVisual2DFacingTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private static void InvokeVoid(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 메서드 존재 확인 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        private static Sprite MakeSprite(string name)
        {
            Texture2D tex = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            sprite.name = name;
            return sprite;
        }

        private MonsterVisual2D NewVisual(out SpriteRenderer body)
        {
            _root = new GameObject("MonsterRoot");
            GameObject visualGo = new GameObject("Visual2D");
            visualGo.transform.SetParent(_root.transform, false);
            body = visualGo.AddComponent<SpriteRenderer>();
            MonsterVisual2D visual = visualGo.AddComponent<MonsterVisual2D>();
            visual.SetBodyForTest(body);
            InvokeVoid(visual, "Awake");
            InvokeVoid(visual, "OnEnable");
            return visual;
        }

        [Test]
        public void 정면_오른쪽으로_이동하면_flipX가_꺼진다()
        {
            MonsterVisual2D visual = NewVisual(out SpriteRenderer body);
            _root.transform.forward = new Vector3(1f, 0f, 0.1f).normalized;

            InvokeVoid(visual, "LateUpdate");

            Assert.IsTrue(visual.FacingRightForTest);
            Assert.IsFalse(body.flipX);
        }

        [Test]
        public void 왼쪽으로_이동하면_flipX가_켜진다()
        {
            MonsterVisual2D visual = NewVisual(out SpriteRenderer body);
            _root.transform.forward = new Vector3(-1f, 0f, 0.1f).normalized;

            InvokeVoid(visual, "LateUpdate");

            Assert.IsFalse(visual.FacingRightForTest);
            Assert.IsTrue(body.flipX);
        }

        //# 데드존(§5.8) — |forward.x| ≤ 0.1(세로 이동 중)이면 직전 방향을 유지, 좌우 깜빡임 방지.
        [Test]
        public void 데드존_안에서는_직전_방향을_유지한다()
        {
            MonsterVisual2D visual = NewVisual(out SpriteRenderer body);
            _root.transform.forward = new Vector3(1f, 0f, 0.1f).normalized;
            InvokeVoid(visual, "LateUpdate");
            Assert.IsTrue(visual.FacingRightForTest, "먼저 오른쪽으로 확정");

            _root.transform.forward = new Vector3(0.02f, 0f, 1f).normalized;   //# 거의 정면(세로) 이동 — |x| < 0.1
            InvokeVoid(visual, "LateUpdate");

            Assert.IsTrue(visual.FacingRightForTest, "데드존 안이면 직전 방향(오른쪽) 유지");
            Assert.IsFalse(body.flipX);
        }

        //# 풀 재사용 — OnEnable 재호출 시 기본 방향(오른쪽)으로 복귀한다.
        [Test]
        public void 풀재사용_OnEnable에서_기본_오른쪽_방향으로_복귀한다()
        {
            MonsterVisual2D visual = NewVisual(out SpriteRenderer body);
            _root.transform.forward = new Vector3(-1f, 0f, 0.1f).normalized;
            InvokeVoid(visual, "LateUpdate");
            Assert.IsFalse(visual.FacingRightForTest, "왼쪽으로 확정");

            InvokeVoid(visual, "OnEnable");   //# 풀 재사용 시뮬레이션

            Assert.IsTrue(visual.FacingRightForTest, "OnEnable 재호출 후 기본 오른쪽 복귀");
            Assert.IsFalse(body.flipX);
        }

        //# 엣지 — 오버레이가 없는(Lv0) 몬스터도 LateUpdate 가 예외 없이 동작한다(§6.3.8 null 허용).
        [Test]
        public void 오버레이가_없어도_LateUpdate가_예외없이_동작한다()
        {
            MonsterVisual2D visual = NewVisual(out SpriteRenderer body);

            Assert.DoesNotThrow(() => InvokeVoid(visual, "LateUpdate"));
        }

        //# 통합 — ③⑨ 오버레이 동기: LateUpdate 가 몸 flipX 를 먼저 적용한 뒤, 그 값을 같은 프레임 안에서
        //# 오버레이 Tick 에 그대로 전달한다(§6.3.6 "Animator 갱신 뒤 같은 프레임 안에서").
        [Test]
        public void LateUpdate에서_오버레이가_몸의_최신_flipX를_그대로_전달받는다()
        {
            _root = new GameObject("MonsterRoot");
            GameObject visualGo = new GameObject("Visual2D");
            visualGo.transform.SetParent(_root.transform, false);
            SpriteRenderer body = visualGo.AddComponent<SpriteRenderer>();

            GameObject overlayGo = new GameObject("Enhance");
            overlayGo.transform.SetParent(visualGo.transform, false);
            SpriteRenderer overlayRd = overlayGo.AddComponent<SpriteRenderer>();
            MonsterTierOverlay overlay = overlayGo.AddComponent<MonsterTierOverlay>();
            overlay.SetOverlayRendererForTest(overlayRd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_0") }, null, null);
            overlay.SetTier(1);

            MonsterVisual2D visual = visualGo.AddComponent<MonsterVisual2D>();
            visual.SetBodyForTest(body);
            visual.SetTierOverlayForTest(overlay);
            InvokeVoid(visual, "Awake");
            InvokeVoid(visual, "OnEnable");

            body.sprite = MakeSprite("Wisp_Sheet_0");
            _root.transform.forward = new Vector3(-1f, 0f, 0.1f).normalized;   //# 왼쪽으로 전환

            InvokeVoid(visual, "LateUpdate");

            Assert.IsTrue(body.flipX, "몸이 먼저 왼쪽으로 flip");
            Assert.IsTrue(overlayRd.flipX, "오버레이도 같은 프레임에 몸의 최신 flipX 를 전달받는다");
            Assert.AreEqual("Wisp_Sheet_T1_0", overlayRd.sprite.name, "인덱스 동기도 함께 유지");
        }
    }
}
