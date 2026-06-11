using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ResultPopup 씬 전환 연타 가드 — 첫 요청만 통과, InitUI(풀 재사용) 시 리셋 (Rule 03 §4).
    public class ResultPopupSceneGuardTests
    {
        private ResultPopup _popup;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new GameObject("ResultPopupGuardTest");
            _popup = go.AddComponent<ResultPopup>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_popup.gameObject);
        }

        [Test]
        public void 첫_씬전환_요청은_통과한다()
        {
            Assert.IsTrue(_popup.TryBeginSceneLoad());
        }

        [Test]
        public void 연타_두번째_요청은_차단되고_InitUI_재사용시_리셋된다()
        {
            _popup.TryBeginSceneLoad();
            Assert.IsFalse(_popup.TryBeginSceneLoad());

            //# CHMUI reuse 경로 — InitUI 재호출이 가드를 리셋해야 다음 결과 화면에서 버튼이 산다.
            _popup.InitUI(new ResultPopupArg());
            Assert.IsTrue(_popup.TryBeginSceneLoad());
        }
    }
}
