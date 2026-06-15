using System.Collections;
using System.Collections.Generic;
using ChvjUnityInfra;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lair.Tests.PlayMode.UI
{
    //# CHPoolingScrollView.SetScrollPosition 검증 — duration:0 즉시 스냅 + InitItem() rebind 로
    //# 도착 지점 셀이 증분 OnScroll 없이 즉시 정확히 바인딩되는지 단언한다.
    //# EditMode 로는 viewport/layout rect 가 0 이라 잡히지 않으므로 실제 Canvas 를 띄우는 PlayMode 전용.
    public class CHPoolingScrollViewSetScrollPositionTests
    {
        //# 셀 — 마지막으로 바인딩된 데이터 인덱스를 노출해 rebind 관측을 가능하게 한다.
        private class TestCell : MonoBehaviour
        {
            public int BoundIndex = -1;
        }

        private class TestData
        {
            public int Index;
        }

        //# 테스트 전용 풀링 스크롤뷰 — InitItem 에서 바인딩 인덱스를 셀에 기록.
        private class TestPoolingScrollView : CHPoolingScrollView<TestCell, TestData>
        {
            public override void InitItem(TestCell item, TestData data, int index)
            {
                item.BoundIndex = data.Index;
            }

            public override void InitPoolingObject(TestCell item)
            {
            }
        }

        private GameObject _canvasGo;
        private TestPoolingScrollView _scrollView;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown]
        public void TearDown()
        {
            if (_scrollView != null)
                Object.Destroy(_scrollView.gameObject);
            if (_canvasGo != null)
                Object.Destroy(_canvasGo);
        }

        //# ScrollRect + viewport + content 계층을 먼저 완성해 ScrollRect.content/viewport 를 와이어링한 뒤,
        //# 마지막에 TestPoolingScrollView 컴포넌트를 붙여 Awake 가 유효한 참조를 캐싱하게 한다.
        //# _origin / _scrollDirection / _columnCount 는 reflection 으로 세팅.
        private void BuildScrollView(int viewportHeight, int cellHeight)
        {
            GameObject root = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            root.transform.SetParent(_canvasGo.transform, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(200f, viewportHeight);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportRt.sizeDelta = new Vector2(200f, viewportHeight);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            GameObject origin = new GameObject("Cell", typeof(RectTransform), typeof(TestCell));
            origin.transform.SetParent(content.transform, false);
            RectTransform originRt = origin.GetComponent<RectTransform>();
            originRt.sizeDelta = new Vector2(200f, cellHeight);

            ScrollRect scrollRect = root.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = content.GetComponent<RectTransform>();

            //# ScrollRect 필드 와이어링 후 마지막에 컴포넌트 추가 — Awake 가 유효 참조 캐싱.
            _scrollView = root.AddComponent<TestPoolingScrollView>();

            System.Reflection.FieldInfo originFi = typeof(CHPoolingScrollView<TestCell, TestData>)
                .GetField("_origin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            originFi.SetValue(_scrollView, origin);

            System.Reflection.FieldInfo dirFi = typeof(CHPoolingScrollView<TestCell, TestData>)
                .GetField("_scrollDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dirFi.SetValue(_scrollView, PoolingScrollViewDirection.Vertical);

            System.Reflection.FieldInfo colFi = typeof(CHPoolingScrollView<TestCell, TestData>)
                .GetField("_columnCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            colFi.SetValue(_scrollView, 1);
        }

        private List<TestData> MakeData(int count)
        {
            List<TestData> list = new List<TestData>();
            for (int i = 0; i < count; ++i)
            {
                list.Add(new TestData { Index = i });
            }
            return list;
        }

        //# Content 자식 중 활성 셀의 BoundIndex 집합.
        private HashSet<int> ActiveBoundIndices()
        {
            HashSet<int> result = new HashSet<int>();
            ScrollRect scrollRect = _scrollView.GetComponent<ScrollRect>();
            Transform content = scrollRect.content;
            for (int i = 0; i < content.childCount; ++i)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (child.activeSelf == false)
                    continue;
                TestCell cell = child.GetComponent<TestCell>();
                if (cell != null)
                    result.Add(cell.BoundIndex);
            }
            return result;
        }

        //# 정상 케이스 — 30개 데이터(스크롤 필요)에서 SetScrollPosition(k, duration:0) 직후,
        //# yield 없이 도착 인덱스 k 근방 셀이 활성·바인딩되어야 한다 (즉시 rebind).
        [UnityTest]
        public IEnumerator SetScrollPosition_duration0_도착인덱스근방_셀이_즉시_rebind된다()
        {
            BuildScrollView(viewportHeight: 200, cellHeight: 50);
            yield return null;

            _scrollView.SetItemList(MakeData(30));
            yield return null;

            //# 사전 조건 — 초기엔 상단(0 근방) 바인딩.
            HashSet<int> before = ActiveBoundIndices();
            Assert.IsTrue(before.Contains(0), "초기 상태에서 인덱스 0 셀이 활성 바인딩이어야 함");

            //# 중간 인덱스로 이동 — duration 0 이므로 yield 없이 동기 rebind 되어야 한다.
            const int target = 20;
            _scrollView.SetScrollPosition(target, duration: 0f);

            HashSet<int> after = ActiveBoundIndices();

            //# 도착 지점 근방(target 포함 ±1) 셀이 활성 바인딩되어야 함. 초기 상단(0)에 머물면 FAIL.
            bool hasTargetRegion = after.Contains(target)
                || after.Contains(target - 1)
                || after.Contains(target + 1);
            Assert.IsTrue(hasTargetRegion,
                $"duration:0 직후 도착 인덱스 {target} 근방 셀이 활성 바인딩이어야 함 — 활성: [{string.Join(",", after)}]");
            Assert.IsFalse(after.Contains(0),
                $"도착 후 상단 인덱스 0 셀이 활성으로 남으면 안 됨 (증분 OnScroll 의존 잔재) — 활성: [{string.Join(",", after)}]");
        }

        //# 엣지 — 빈 풀 가드. SetItemList 없이(풀 0개) SetScrollPosition 호출해도 예외 없이 즉시 return.
        [UnityTest]
        public IEnumerator SetScrollPosition_빈풀에서_예외없이_즉시리턴한다()
        {
            BuildScrollView(viewportHeight: 200, cellHeight: 50);
            yield return null;

            //# SetItemList 미호출 → liPoolItem.Count == 0. 예외 없이 통과해야 한다.
            Assert.DoesNotThrow(() => _scrollView.SetScrollPosition(5, duration: 0f),
                "빈 풀에서 SetScrollPosition 은 예외 없이 즉시 return 해야 함");
            yield return null;
        }
    }
}
