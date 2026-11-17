using System;
using System.IO;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.UI
{
    //# sealed-nest-lock — 순수 서식(SL1·SL2)·문구 데이터·프리팹 배선 정적 단언·ConfirmPopup 재사용(SL10).
    //# 시간 경과·정지·ESC 흐름(SL3~SL8·SL11~SL13 런타임)은 씬이 필요해 PlayMode 몫 — 여기선 정적 근거만 고정.
    public class SealedNestLockTests
    {
        private const string StringsPath = "Assets/_Lair/Data/Json/Strings_Ko.json";
        private const string CellPrefabPath = "Assets/_Lair/Art/UI/SpawnerStatusCell.prefab";
        private const string PopupPrefabPath = "Assets/_Lair/Art/UI/ConfirmPopup.prefab";
        private const string Template = "{0} 둥지가 봉인되어 있다.\n영웅 HP가 {1}% 이하가 되면 봉인이 풀린다.\n현재 영웅 HP {2}% ({3}%p 더 깎으면 해제)";

        //# ---- SL1 본문 서식 ----

        [Test]
        public void 본문서식_비율0723_해제50이면_현재73_남은23이다()
        {
            string body = SealedNestInfo.FormatNestPopupBody(Template, "저주술사", 50, 0.723f);
            Assert.AreEqual("저주술사 둥지가 봉인되어 있다.\n영웅 HP가 50% 이하가 되면 봉인이 풀린다.\n현재 영웅 HP 73% (23%p 더 깎으면 해제)", body);
        }

        [Test]
        public void 본문서식_비율05001_해제50이면_현재51_남은1이다()
        {
            Assert.AreEqual("X 둥지 50 51 1", SealedNestInfo.FormatNestPopupBody("{0} 둥지 {1} {2} {3}", "X", 50, 0.5001f));
        }

        [Test]
        public void 본문서식_비율1_해제50이면_현재100_남은50이다()
        {
            Assert.AreEqual("100 50", SealedNestInfo.FormatNestPopupBody("{2} {3}", "X", 50, 1.0f));
        }

        [Test]
        public void 본문서식_종족이름과_해제값이_치환된다()
        {
            Assert.AreEqual("도깨비불|60", SealedNestInfo.FormatNestPopupBody("{0}|{1}", "도깨비불", 60, 0.9f));
        }

        [Test]
        public void 본문서식_부동소수_정확히_정수퍼센트면_올림이_튀지_않는다()
        {
            //# 0.56f*100 = 56.0000002 → 56 이어야 한다(57 아님).
            Assert.AreEqual("56", SealedNestInfo.FormatNestPopupBody("{2}", "X", 50, 0.56f));
            Assert.AreEqual("58", SealedNestInfo.FormatNestPopupBody("{2}", "X", 50, 0.58f));
        }

        [Test]
        public void 본문서식_k_나누기_100f_비율은_정확히_k퍼센트로_표시된다_전구간()
        {
            //# Health.Ratio = cur/max 형태의 실제 float 비율 — 정수 퍼센트가 한 칸 올라가 보이면 안 된다.
            for (int k = 1; k <= 100; ++k)
            {
                string body = SealedNestInfo.FormatNestPopupBody("{2}", "X", 0, k / 100f);
                Assert.AreEqual(k.ToString(), body, $"k={k}");
            }
        }

        //# ---- SL2 올림 불변식 ----

        [Test]
        public void 올림불변식_001단위_해제비율_봉인중이면_현재가_해제보다_항상_크다()
        {
            for (int unlock = 50; unlock <= 70; ++unlock)
            {
                float unlockRatio = unlock / 100f;
                //# 봉인 중 = 비율 > 해제 비율. 해제 비율 바로 위부터 1.0 까지 0.0005 간격 스윕.
                for (float ratio = unlockRatio + 0.0005f; ratio <= 1.0f; ratio += 0.0005f)
                {
                    string body = SealedNestInfo.FormatNestPopupBody("{2}|{3}", "X", unlock, ratio);
                    string[] parts = body.Split('|');
                    int current = int.Parse(parts[0]);
                    int remain = int.Parse(parts[1]);
                    Assert.Greater(current, unlock, $"unlock={unlock} ratio={ratio}");
                    Assert.GreaterOrEqual(remain, 1, $"unlock={unlock} ratio={ratio}");
                }
            }
        }

        [Test]
        public void 올림불변식_3자리_비율은_남은값이_1로_바닥처리된다()
        {
            //# 해제 0.555 → 반올림 56. 비율 0.5551 → 현재 56 → 56-56=0 → 1.
            Assert.AreEqual("56|1", SealedNestInfo.FormatNestPopupBody("{2}|{3}", "X", 56, 0.5551f));
        }

        [Test]
        public void 남은값_음수가_나오는_입력도_최소_1이다()
        {
            Assert.AreEqual("40|1", SealedNestInfo.FormatNestPopupBody("{2}|{3}", "X", 50, 0.40f));
        }

        //# ---- Strings_Ko ----

        private static StringEntry[] LoadStrings()
        {
            return JsonArrayUtility.FromJsonArray<StringEntry>(File.ReadAllText(StringsPath));
        }

        private static string TextOf(StringEntry[] entries, int id)
        {
            StringEntry found = null;
            int count = 0;
            foreach (StringEntry e in entries)
            {
                if (e.id == id)
                {
                    found = e;
                    ++count;
                }
            }
            Assert.AreEqual(1, count, $"문구 id {id} 는 정확히 1행");
            return found.text;
        }

        [Test]
        public void 문구_1688에서_1691은_각각_정확히_한_행이고_비어있지_않다()
        {
            StringEntry[] entries = LoadStrings();
            for (int id = 1688; id <= 1691; ++id)
            {
                Assert.IsNotEmpty(TextOf(entries, id), $"id {id}");
            }
        }

        [Test]
        public void 문구_id는_오름차순이고_중복이_없다()
        {
            StringEntry[] entries = LoadStrings();
            for (int i = 1; i < entries.Length; ++i)
            {
                Assert.Less(entries[i - 1].id, entries[i].id, $"위치 {i}");
            }
        }

        [Test]
        public void 문구_본문1689는_4개_서식자리를_가지고_실제로_치환된다()
        {
            string template = TextOf(LoadStrings(), 1689);
            foreach (string slot in new[] { "{0}", "{1}", "{2}", "{3}" })
            {
                StringAssert.Contains(slot, template);
            }
            string body = SealedNestInfo.FormatNestPopupBody(template, "저주술사", 50, 0.723f);
            StringAssert.Contains("저주술사", body);
            StringAssert.Contains("73%", body);
            StringAssert.Contains("23%p", body);
            StringAssert.DoesNotContain("{", body);
        }

        [Test]
        public void 문구_제목확인셀문구는_기획서_문안과_같다()
        {
            StringEntry[] entries = LoadStrings();
            Assert.AreEqual("봉인된 둥지", TextOf(entries, 1688));
            Assert.AreEqual("확인", TextOf(entries, 1690));
            Assert.AreEqual("탭하여 확인", TextOf(entries, 1691));
        }

        [Test]
        public void 문구_1319는_유지된다_회귀()
        {
            Assert.AreEqual("봉인 - 영웅 HP {0}% 이하 해제", TextOf(LoadStrings(), 1319));
        }

        //# ---- 셀 상수(리플렉션) ----

        [Test]
        public void 셀_봉인_상수는_기획서_값이다()
        {
            Assert.AreEqual(new Color(0.40f, 0.40f, 0.44f, 1f), SpawnerStatusCell.SealTintColor);
            Assert.AreEqual(new Color(0.969f, 0.776f, 0.290f, 1f), SpawnerStatusCell.SealTextColor);
            Assert.AreEqual(1691, GetConst<int>("SealTextStringId"));
            Assert.AreEqual(0.45f, GetConst<float>("UnlockAnimSeconds"), 1e-6f);
            Assert.AreEqual(1.35f, GetConst<float>("UnlockLockScale"), 1e-6f);
        }

        private static T GetConst<T>(string name)
        {
            FieldInfo fi = typeof(SpawnerStatusCell).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(fi, name);
            return (T)fi.GetRawConstantValue();
        }

        //# ---- 프리팹 배선 (SL12 정적·자물쇠 설정) ----

        private static SerializedProperty Prop(UnityEngine.Object target, string name, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(name);
            Assert.IsNotNull(p, $"{target.GetType().Name}.{name}");
            return p;
        }

        [Test]
        public void 셀프리팹_자물쇠는_배선되고_기본비활성_레이캐스트off_스프라이트_연결이다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            Assert.IsNotNull(prefab);
            SpawnerStatusCell cell = prefab.GetComponent<SpawnerStatusCell>();
            Assert.IsNotNull(cell);

            SerializedProperty lockProp = Prop(cell, "_sealLockIcon", out SerializedObject so);
            Image lockIcon = lockProp.objectReferenceValue as Image;
            Assert.IsNotNull(lockIcon, "_sealLockIcon 미배선");
            Assert.IsFalse(lockIcon.gameObject.activeSelf, "기본 비활성이어야 한다");
            Assert.IsFalse(lockIcon.raycastTarget, "레이캐스트 off 여야 셀 탭을 가로채지 않는다");
            Assert.IsNotNull(lockIcon.sprite, "자물쇠 스프라이트 연결");
            Assert.IsTrue(lockIcon.transform.IsChildOf(prefab.transform));
        }

        [Test]
        public void 셀프리팹_자물쇠는_아이콘과_발광_위에_그려진다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            SpawnerStatusCell cell = prefab.GetComponent<SpawnerStatusCell>();
            Component lockIcon = Prop(cell, "_sealLockIcon", out SerializedObject so1).objectReferenceValue as Component;
            Assert.IsNotNull(lockIcon);
            foreach (string field in new[] { "_icon", "_glowOverlay" })
            {
                Component other = Prop(cell, field, out SerializedObject so2).objectReferenceValue as Component;
                Assert.IsNotNull(other, field);
                //# 같은 부모일 때만 형제 순서를 비교(다른 부모면 부모 순서 비교는 과단정).
                if (other.transform.parent == lockIcon.transform.parent)
                {
                    Assert.Greater(lockIcon.transform.GetSiblingIndex(), other.transform.GetSiblingIndex(), field);
                }
            }
        }

        [Test]
        public void 셀프리팹_버튼_Transition은_None이다_SL12()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            SpawnerStatusCell cell = prefab.GetComponent<SpawnerStatusCell>();
            CHButton chButton = Prop(cell, "_button", out SerializedObject so).objectReferenceValue as CHButton;
            Assert.IsNotNull(chButton, "_button 미배선");
            Button button = chButton.GetComponent<Button>();
            Assert.IsNotNull(button, "CHButton 과 같은 오브젝트에 Button");
            Assert.AreEqual(Selectable.Transition.None, button.transition, "interactable 을 끄는 비봉인 셀이 어두워지면 안 된다");
        }

        [Test]
        public void 셀프리팹_버튼_그래픽은_셀_전체를_덮는_레이캐스트_대상이다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            SpawnerStatusCell cell = prefab.GetComponent<SpawnerStatusCell>();
            CHButton chButton = Prop(cell, "_button", out SerializedObject so).objectReferenceValue as CHButton;
            Graphic graphic = chButton.GetComponent<Graphic>();
            Assert.IsNotNull(graphic, "버튼 오브젝트에 Graphic");
            Assert.IsTrue(graphic.raycastTarget);
            //# 좌표 과단정 금지 — 셀 루트 크기와 같거나 더 큰지만 본다.
            RectTransform root = (RectTransform)prefab.transform;
            RectTransform rect = (RectTransform)graphic.transform;
            Assert.GreaterOrEqual(rect.rect.width + 0.5f, root.rect.width * 0.9f);
            Assert.GreaterOrEqual(rect.rect.height + 0.5f, root.rect.height * 0.9f);
        }

        //# ---- ConfirmPopup 프리팹·SL10 ----

        private GameObject _popupGo;

        [TearDown]
        public void TearDown()
        {
            if (_popupGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_popupGo);
                _popupGo = null;
            }
        }

        private ConfirmPopup InstantiatePopup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            Assert.IsNotNull(prefab);
            _popupGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return _popupGo.GetComponent<ConfirmPopup>();
        }

        private static T Field<T>(ConfirmPopup popup, string name) where T : class
        {
            FieldInfo fi = typeof(ConfirmPopup).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, name);
            return fi.GetValue(popup) as T;
        }

        private static ConfirmPopupArg Arg(string cancelLabel)
        {
            return new ConfirmPopupArg { Title = "t", Message = "m", ConfirmLabel = "확인", CancelLabel = cancelLabel };
        }

        [Test]
        public void 확인팝업프리팹_글로우슬롯이_배선되어_있다()
        {
            ConfirmPopup popup = InstantiatePopup();
            Assert.IsNotNull(popup);
            Assert.IsNotNull(Field<RectTransform>(popup, "_confirmGlow"));
            Assert.IsNotNull(Field<CHButton>(popup, "_confirmButton"));
            Assert.IsNotNull(Field<CHButton>(popup, "_cancelButton"));
        }

        [Test]
        public void 확인팝업_1버튼이면_취소숨김_확인과_글로우가_X0이다()
        {
            ConfirmPopup popup = InstantiatePopup();
            popup.InitUI(Arg(null));
            AssertOneButton(popup);
        }

        [Test]
        public void 확인팝업_빈문자열_취소라벨도_1버튼이다()
        {
            ConfirmPopup popup = InstantiatePopup();
            popup.InitUI(Arg(string.Empty));
            AssertOneButton(popup);
        }

        [Test]
        public void 확인팝업_2버튼이면_취소표시_확인과_글로우가_X마이너스130이다()
        {
            ConfirmPopup popup = InstantiatePopup();
            popup.InitUI(Arg("취소"));
            AssertTwoButtons(popup);
        }

        [Test]
        public void 확인팝업_1버튼_호출뒤_2버튼_호출하면_원래_모양으로_복귀한다_SL10()
        {
            ConfirmPopup popup = InstantiatePopup();
            popup.InitUI(Arg(null));
            AssertOneButton(popup);
            popup.InitUI(Arg("취소"));
            AssertTwoButtons(popup);
        }

        [Test]
        public void 확인팝업_2버튼_호출뒤_1버튼_호출하면_1버튼으로_전환된다()
        {
            ConfirmPopup popup = InstantiatePopup();
            popup.InitUI(Arg("취소"));
            popup.InitUI(Arg(null));
            AssertOneButton(popup);
        }

        private static void AssertOneButton(ConfirmPopup popup)
        {
            Assert.IsFalse(Field<CHButton>(popup, "_cancelButton").gameObject.activeSelf);
            Assert.AreEqual(0f, ((RectTransform)Field<CHButton>(popup, "_confirmButton").transform).anchoredPosition.x, 1e-3f);
            Assert.AreEqual(0f, Field<RectTransform>(popup, "_confirmGlow").anchoredPosition.x, 1e-3f);
        }

        private static void AssertTwoButtons(ConfirmPopup popup)
        {
            Assert.IsTrue(Field<CHButton>(popup, "_cancelButton").gameObject.activeSelf);
            Assert.AreEqual(-130f, ((RectTransform)Field<CHButton>(popup, "_confirmButton").transform).anchoredPosition.x, 1e-3f);
            Assert.AreEqual(-130f, Field<RectTransform>(popup, "_confirmGlow").anchoredPosition.x, 1e-3f);
        }

        [Test]
        public void 확인팝업_Y좌표는_호출로_변하지_않는다()
        {
            ConfirmPopup popup = InstantiatePopup();
            RectTransform confirm = (RectTransform)Field<CHButton>(popup, "_confirmButton").transform;
            float y = confirm.anchoredPosition.y;
            float glowY = Field<RectTransform>(popup, "_confirmGlow").anchoredPosition.y;
            popup.InitUI(Arg(null));
            popup.InitUI(Arg("취소"));
            Assert.AreEqual(y, confirm.anchoredPosition.y, 1e-3f);
            Assert.AreEqual(glowY, Field<RectTransform>(popup, "_confirmGlow").anchoredPosition.y, 1e-3f);
        }

        [Test]
        public void 확인팝업_확인_닫힘은_OnCancel을_호출하지_않고_그외_닫힘은_1회_호출한다()
        {
            ConfirmPopup popup = InstantiatePopup();
            int cancelCount = 0;
            ConfirmPopupArg arg = Arg(null);
            arg.OnCancel = () => ++cancelCount;
            popup.InitUI(arg);

            //# 확인 외 닫힘(배경·ESC) = OnDisable 수렴 → 정확히 1회, 재호출은 0.
            MethodInfo onDisable = typeof(ConfirmPopup).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onDisable);
            onDisable.Invoke(popup, null);
            onDisable.Invoke(popup, null);
            Assert.AreEqual(1, cancelCount);
        }
    }
}
