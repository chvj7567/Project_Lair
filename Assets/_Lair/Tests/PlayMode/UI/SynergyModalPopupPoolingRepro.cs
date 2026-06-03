using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lair.Tests.PlayMode.UI
{
    //# 시너지 모달 스크롤 affordance 재현 (델타 B). 실제 SynergyModalPopup.prefab 을 Canvas 하위에
    //# 띄우고 CHMUI.ActivateUI 순서(Init→InitUI→SetActive(true)→OnEnable) 를 모사한 뒤,
    //# 풀링 유지 결정에 맞춘 기대치를 단언한다.
    //#
    //# 결정(기획서 §4.1·§8): 풀링·행 높이·모달 폭 무변경 + 세로 스크롤바 추가. 따라서 뷰포트를 넘는
    //# 행은 "동시 활성 셀 == 전체 행 수" 가 아니라, content scrollable + 세로 스크롤바 존재 +
    //# 스크롤 끝에서 마지막 행 노출 로 검증한다. 뷰포트 이내(저스택)는 전부 활성 유지.
    public class SynergyModalPopupPoolingRepro
    {
        private const string PrefabPath = "Assets/_Lair/Art/UI/SynergyModalPopup.prefab";

        private GameObject _canvasGo;
        private SynergyModalPopup _popup;
        private BattleViewModel _vm;

        //# CHMUI 가 ShowUI 시 UI 를 붙이는 Canvas 를 모사 — Battle 씬 없이 독립 실행.
        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _vm = new BattleViewModel(new BattleStateModel());
        }

        [TearDown]
        public void TearDown()
        {
            if (_popup != null)
                Object.Destroy(_popup.gameObject);
            if (_canvasGo != null)
                Object.Destroy(_canvasGo);
        }

        //# _buildAxisCounts dict 에 직접 seed — CardData SO 없이 GetBuildCount 결과를 제어.
        //# BuildRows 는 GetBuildCount 만 의존하므로 행 데이터는 실제 로직으로 산출됨.
        private void SeedAxisCount(EBuildAxis axis, int count)
        {
            FieldInfo fi = typeof(BattleViewModel).GetField("_buildAxisCounts",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "BattleViewModel._buildAxisCounts 필드 존재 확인");
            Dictionary<EBuildAxis, int> dict = fi.GetValue(_vm) as Dictionary<EBuildAxis, int>;
            Assert.IsNotNull(dict, "_buildAxisCounts 가 Dictionary<EBuildAxis,int>");
            dict[axis] = count;
        }

        //# 실제 prefab 로드 + Canvas 하위 배치. CHMUI 의 SetParent/stretch 도 모사.
        private SynergyModalPopup InstantiatePopup()
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"prefab 로드 실패: {PrefabPath}");
            GameObject go = Object.Instantiate(prefab, _canvasGo.transform, false);
            SynergyModalPopup popup = go.GetComponent<SynergyModalPopup>();
            Assert.IsNotNull(popup, "SynergyModalPopup 컴포넌트 존재 확인");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return popup;
#else
            Assert.Ignore("EditMode AssetDatabase 경로 — PlayMode in-editor 전용 재현 테스트");
            return null;
#endif
        }

        //# CHMUI.ActivateUI 모사 — Init → InitUI(arg) → SetActive(true). prefab 이 active 저장이라
        //# InitUI 전에 SetActive(false) 로 offscreen-load 상태를 맞춰 OnEnable 발화 타이밍을 인게임과 일치시킴.
        private void ActivateLikeCHMUI(SynergyModalPopup popup)
        {
            popup.gameObject.SetActive(false);

            SynergyModalPopupArg arg = new SynergyModalPopupArg { ViewModel = _vm };

            //# UIBase.Init 은 internal — 같은 어셈블리가 아니므로 reflection 으로 호출 (UIType 세팅).
            MethodInfo init = typeof(UIBase).GetMethod("Init",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(init, "UIBase.Init(Enum) 존재 확인");
            init.Invoke(popup, new object[] { EUI.SynergyModalPopup });

            popup.InitUI(arg);
            popup.gameObject.SetActive(true);
        }

        //# 테스트가 참조하는 _scrollView / 그 ScrollRect 를 reflection 으로 꺼낸다.
        private SynergyModalCardPoolingScrollView GetScrollView(SynergyModalPopup popup)
        {
            FieldInfo svFi = typeof(SynergyModalPopup).GetField("_scrollView",
                BindingFlags.NonPublic | BindingFlags.Instance);
            SynergyModalCardPoolingScrollView sv =
                svFi.GetValue(popup) as SynergyModalCardPoolingScrollView;
            Assert.IsNotNull(sv, "_scrollView 인스펙터 참조 연결 확인");
            return sv;
        }

        //# Content(ScrollRect.content) 자식 중 activeSelf 셀 수.
        private int ActiveCellCount(SynergyModalCardPoolingScrollView sv)
        {
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();
            Transform content = scrollRect.content;
            int active = 0;
            for (int i = 0; i < content.childCount; ++i)
            {
                if (content.GetChild(i).gameObject.activeSelf)
                    ++active;
            }
            return active;
        }

        //# 인프라 CHPoolingScrollView 의 private _prevScrollPosition 을 reflection 으로 세팅.
        //# 첫 스크롤 스텝의 delta 가 (0,0) 초기값 때문에 0 으로 죽지 않도록 현재 top 위치로 초기화.
        private void ResetPrevScrollPosition(SynergyModalCardPoolingScrollView sv, Vector2 value)
        {
            System.Type t = sv.GetType();
            FieldInfo fi = null;
            while (t != null && fi == null)
            {
                fi = t.GetField("_prevScrollPosition",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }
            Assert.IsNotNull(fi, "CHPoolingScrollView._prevScrollPosition 필드 존재 확인");
            fi.SetValue(sv, value);
        }

        //# Content 자식 중 라벨 텍스트가 target 과 일치하는 활성 셀이 있는지.
        private bool HasActiveCellWithLabel(SynergyModalCardPoolingScrollView sv, string target)
        {
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();
            Transform content = scrollRect.content;
            for (int i = 0; i < content.childCount; ++i)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (child.activeSelf == false)
                    continue;
                TMPro.TMP_Text[] texts = child.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (TMPro.TMP_Text t in texts)
                {
                    if (t.text == target)
                        return true;
                }
            }
            return false;
        }

        //# 활성 셀 중 라벨 텍스트가 non-empty 인 셀이 하나라도 있는지 — 비주얼 채워짐 여부 판정.
        private bool AnyActiveCellHasNonEmptyLabel(SynergyModalCardPoolingScrollView sv)
        {
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();
            Transform content = scrollRect.content;
            for (int i = 0; i < content.childCount; ++i)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (child.activeSelf == false)
                    continue;
                TMPro.TMP_Text[] texts = child.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (TMPro.TMP_Text t in texts)
                {
                    if (string.IsNullOrEmpty(t.text) == false)
                        return true;
                }
            }
            return false;
        }

        //# 활성 셀 중 _icon(자식 "Icon") 이 activeSelf 인 헤더 셀이 하나라도 있는지.
        //# 셀의 _icon 필드를 reflection 으로 꺼내 GameObject.activeSelf 확인.
        private bool AnyActiveCellHasActiveIcon(SynergyModalCardPoolingScrollView sv)
        {
            FieldInfo iconFi = typeof(SynergyModalCell).GetField("_icon",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(iconFi, "SynergyModalCell._icon 필드 존재 확인");

            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();
            Transform content = scrollRect.content;
            for (int i = 0; i < content.childCount; ++i)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (child.activeSelf == false)
                    continue;
                SynergyModalCell cell = child.GetComponent<SynergyModalCell>();
                if (cell == null)
                    continue;
                Image icon = iconFi.GetValue(cell) as Image;
                if (icon != null && icon.gameObject.activeSelf)
                    return true;
            }
            return false;
        }

        //# 4축 모두 임계 초과(16행). 풀링 유지 결정: 동시 활성 == 16 단언 대신
        //# ① 활성 셀 수 <= 전체 행 수(=16, 풀링상 부분 활성 정상)
        //# ② content 높이 > viewport 높이(scrollable)
        //# ③ ScrollRect.verticalScrollbar != null (세로 스크롤바 연결)
        [UnityTest]
        public IEnumerator 모달_첫열림_4축최대_뷰포트초과시_스크롤바가_연결되고_content가_scrollable하다()
        {
            SeedAxisCount(EBuildAxis.Tank, 7);
            SeedAxisCount(EBuildAxis.Dps, 7);
            SeedAxisCount(EBuildAxis.Debuff, 7);
            SeedAxisCount(EBuildAxis.Swarm, 7);

            _popup = InstantiatePopup();
            ActivateLikeCHMUI(_popup);
            yield return null;

            List<SynergyModalCellData> expected =
                SynergyModalPopup.BuildRows(_vm.GetBuildCount);
            //# 기대 행 수는 BuildRows 결과로부터 산출 — 하드코딩 16 제거.
            int rowCount = expected.Count;
            Assert.Greater(rowCount, 0, "사전 조건 — 4축 최대는 행이 생성되어야 함");

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();

            //# ① 풀링상 활성 셀은 전체 행 수 이하 (부분 활성 정상, 16 전부 활성 단언 제거).
            int active = ActiveCellCount(sv);
            Assert.LessOrEqual(active, rowCount,
                $"활성 셀 수는 전체 행 수 이하여야 함 (활성 {active}, 전체 {rowCount})");

            //# ② 콘텐츠가 뷰포트를 초과 → scrollable.
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            Assert.Greater(contentHeight, viewportHeight,
                $"콘텐츠 높이가 뷰포트보다 커야 스크롤 가능 (content {contentHeight}, viewport {viewportHeight})");

            //# ③ 세로 스크롤바 연결됨 (델타 B affordance).
            Assert.IsNotNull(scrollRect.verticalScrollbar,
                "ScrollRect.verticalScrollbar 가 연결되어 있어야 함 (델타 B)");
        }

        //# 4축최대 — 스크롤 끝(정규화 0)까지 내렸을 때 마지막 행(Swarm Tier3) 셀이
        //# 풀 재활용으로 활성·표시되는지. UpdateContent 가 하위 행을 노출하는지 검증.
        [UnityTest]
        public IEnumerator 모달_4축최대_스크롤을_끝까지내리면_마지막행이_노출된다()
        {
            SeedAxisCount(EBuildAxis.Tank, 7);
            SeedAxisCount(EBuildAxis.Dps, 7);
            SeedAxisCount(EBuildAxis.Debuff, 7);
            SeedAxisCount(EBuildAxis.Swarm, 7);

            _popup = InstantiatePopup();
            ActivateLikeCHMUI(_popup);
            yield return null;

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();

            //# 인프라 OnScroll 은 delta.y != 0 일 때만 UpdateContent 발화(_prevScrollPosition 초기값 (0,0)).
            //# 실게임 드래그는 정규화 1→0 연속 이벤트라 매 스텝 delta≠0. 시뮬도 점진 스크롤로 충실화한다.
            //# 보조로 _prevScrollPosition 을 현재 top(y=1) 으로 reflection 초기화 — 첫 스텝 delta 유효 보장.
            ResetPrevScrollPosition(sv, scrollRect.normalizedPosition);

            //# 1.0 → 0.0 을 여러 스텝으로 내리며 각 스텝마다 onValueChanged → OnScroll → UpdateContent.
            float[] steps = { 1.0f, 0.75f, 0.5f, 0.25f, 0.0f };
            foreach (float pos in steps)
            {
                scrollRect.verticalNormalizedPosition = pos;
                scrollRect.onValueChanged.Invoke(scrollRect.normalizedPosition);
                yield return null;
            }
            yield return null;

            //# 마지막 행 라벨은 BuildRows 결과의 마지막 Effect 행에서 동적 산출 (기획 문구 변경 무회귀).
            //# 4축최대 기준 마지막은 Swarm Tier3 = "Tier3  모든 스포너 동시 출력 +1".
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(_vm.GetBuildCount);
            string lastLabel = rows[rows.Count - 1].Label;
            Assert.AreEqual("Tier3  모든 스포너 동시 출력 +1", lastLabel,
                "사전 조건 — 4축최대 마지막 행은 Swarm Tier3 효과");

            //# 풀 재활용으로 활성 셀에 노출되어야 함.
            Assert.IsTrue(HasActiveCellWithLabel(sv, lastLabel),
                $"스크롤 끝에서 마지막 행('{lastLabel}') 셀이 활성·표시되어야 함 (UpdateContent 풀 재활용)");
        }

        //# 저스택 — 1축 Tier2(5장) = 헤더1 + 효과2 = 3행. 뷰포트 이내이므로 전부 활성 유지 (정정 불요).
        [UnityTest]
        public IEnumerator 모달_첫열림_1축Tier2_활성셀수가_3행과_일치한다()
        {
            SeedAxisCount(EBuildAxis.Tank, 5);

            _popup = InstantiatePopup();
            ActivateLikeCHMUI(_popup);
            yield return null;

            List<SynergyModalCellData> expected =
                SynergyModalPopup.BuildRows(_vm.GetBuildCount);
            Assert.AreEqual(3, expected.Count, "사전 조건 — Tank 5장은 헤더1+효과2=3행");

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);
            int active = ActiveCellCount(sv);
            Assert.AreEqual(expected.Count, active,
                $"뷰포트 이내 저스택은 전부 활성이어야 함 (기대 {expected.Count}, 실제 {active})");
        }

        //# 재현(버그) — 닫기→재오픈 시 활성 셀의 라벨이 비워지는지. 메인 가설:
        //# 재오픈 SetActive(true) cascade 에서 팝업 OnEnable→Build(Bind) 가 먼저 셀을 채운 뒤
        //# 셀 OnEnable 이 늦게 돌아 _label/_icon 을 다시 비워 Bind 결과를 덮어쓴다.
        //# ① 첫 열림 직후 활성 셀 라벨 non-empty (정상) 확인 → ② 닫고 재오픈 후에도 non-empty 단언.
        //# 가설이 맞으면 ②가 FAIL(재현). 수정 후 PASS 기대.
        [UnityTest]
        public IEnumerator 모달_재오픈_활성셀라벨이_비워지지않고_유지된다()
        {
            SeedAxisCount(EBuildAxis.Tank, 5);

            _popup = InstantiatePopup();

            //# 첫 열림 — 정상 표시 확인.
            ActivateLikeCHMUI(_popup);
            yield return null;

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);
            Assert.IsTrue(AnyActiveCellHasNonEmptyLabel(sv),
                "첫 열림 — 활성 셀에 라벨이 채워져 있어야 함 (정상 경로)");

            //# 닫기 — CHMUI.CloseUI(reuse:true) 모사: SetActive(false) 만, 인스턴스 보존.
            _popup.gameObject.SetActive(false);
            yield return null;

            //# 재오픈 — 캐시 재사용 경로 모사: InitUI 다시 + SetActive(true) → cascade(OnEnable).
            SynergyModalPopupArg reArg = new SynergyModalPopupArg { ViewModel = _vm };
            _popup.InitUI(reArg);
            _popup.gameObject.SetActive(true);
            yield return null;

            //# 재현 단언 — 재오픈 후에도 활성 셀 라벨이 채워져 있어야 함.
            //# 셀 OnEnable 이 Bind 결과를 덮어쓰면 모든 활성 셀 라벨이 빈 문자열이 되어 FAIL.
            Assert.IsTrue(AnyActiveCellHasNonEmptyLabel(sv),
                "재오픈 후 — 활성 셀 라벨이 비워지면 안 됨 (셀 OnEnable 이 Bind 를 덮어쓰는 버그)");
        }

        //# 재현(버그) — 헤더 셀 아이콘이 재오픈 후 비활성화되는지. 아이콘 sprite 가 인스펙터에
        //# 연결된 환경에서만 의미. 미연결이면 첫 열림에서도 아이콘이 없으므로 Inconclusive 처리.
        [UnityTest]
        public IEnumerator 모달_재오픈_헤더셀아이콘이_꺼지지않고_유지된다()
        {
            SeedAxisCount(EBuildAxis.Tank, 5);

            _popup = InstantiatePopup();

            ActivateLikeCHMUI(_popup);
            yield return null;

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);

            //# 사전 조건 — 아이콘 sprite 미연결이면 첫 열림에도 헤더 아이콘이 꺼져 있어 재현 불가.
            if (AnyActiveCellHasActiveIcon(sv) == false)
            {
                Assert.Ignore("아이콘 sprite 미연결 환경 — 헤더 아이콘 재현 케이스 스킵 (라벨 재현 케이스로 충분)");
                yield break;
            }

            _popup.gameObject.SetActive(false);
            yield return null;

            SynergyModalPopupArg reArg = new SynergyModalPopupArg { ViewModel = _vm };
            _popup.InitUI(reArg);
            _popup.gameObject.SetActive(true);
            yield return null;

            Assert.IsTrue(AnyActiveCellHasActiveIcon(sv),
                "재오픈 후 — 헤더 셀 아이콘이 비활성화되면 안 됨 (셀 OnEnable ResetIcon 이 Bind 를 덮어쓰는 버그)");
        }

        //# 엣지 — 닫기→재오픈 사이클. 재오픈 후에도 4축최대에서 스크롤바 연결·content scrollable 유지.
        [UnityTest]
        public IEnumerator 모달_재오픈_4축최대_스크롤바연결과_scrollable이_유지된다()
        {
            SeedAxisCount(EBuildAxis.Tank, 7);
            SeedAxisCount(EBuildAxis.Dps, 7);
            SeedAxisCount(EBuildAxis.Debuff, 7);
            SeedAxisCount(EBuildAxis.Swarm, 7);

            _popup = InstantiatePopup();

            //# 첫 열림.
            ActivateLikeCHMUI(_popup);
            yield return null;

            //# 닫기 — CHMUI.CloseUI(reuse:true) 모사: SetActive(false) 만, 인스턴스 보존.
            _popup.gameObject.SetActive(false);
            yield return null;

            //# 재오픈 — 캐시 재사용 경로 모사: InitUI 다시 + SetActive(true) → OnEnable → Build.
            SynergyModalPopupArg arg = new SynergyModalPopupArg { ViewModel = _vm };
            _popup.InitUI(arg);
            _popup.gameObject.SetActive(true);
            yield return null;

            List<SynergyModalCellData> expected =
                SynergyModalPopup.BuildRows(_vm.GetBuildCount);
            Assert.Greater(expected.Count, 0, "사전 조건 — 4축 최대는 행이 생성됨");

            SynergyModalCardPoolingScrollView sv = GetScrollView(_popup);
            ScrollRect scrollRect = sv.GetComponent<ScrollRect>();

            int active = ActiveCellCount(sv);
            Assert.LessOrEqual(active, expected.Count,
                $"재오픈 후 활성 셀 수는 전체 행 수 이하여야 함 (활성 {active}, 전체 {expected.Count})");

            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            Assert.Greater(contentHeight, viewportHeight,
                "재오픈 후에도 콘텐츠가 뷰포트를 초과해 scrollable 유지");
            Assert.IsNotNull(scrollRect.verticalScrollbar,
                "재오픈 후에도 세로 스크롤바 연결 유지");
        }
    }
}
