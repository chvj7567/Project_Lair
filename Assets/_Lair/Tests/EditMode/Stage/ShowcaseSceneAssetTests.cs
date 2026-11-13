using System.Collections.Generic;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.Showcase;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Tests.EditMode
{
    //# 쇼케이스 무대 슬롯 읽기 헬퍼 — MonsterShowcaseStage._slots 를 SerializedObject 로 읽는다(읽기 전용).
    public static class ShowcaseSceneUtil
    {
        public struct SlotInfo
        {
            public int Species;
            public Transform Holder;
            public SpriteRenderer Shadow;
            public GameObject Placeholder;
        }

        public static List<SlotInfo> Slots(Scene scene)
        {
            MonsterShowcaseStage stage = DotSceneTestUtil.Find<MonsterShowcaseStage>(scene);
            Assert.IsNotNull(stage, "MonsterShowcaseStage");
            SerializedProperty slots = new SerializedObject(stage).FindProperty("_slots");
            List<SlotInfo> result = new List<SlotInfo>();
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                result.Add(new SlotInfo
                {
                    Species = slot.FindPropertyRelative("Species").intValue,
                    Holder = slot.FindPropertyRelative("Holder").objectReferenceValue as Transform,
                    Shadow = slot.FindPropertyRelative("Shadow").objectReferenceValue as SpriteRenderer,
                    Placeholder = slot.FindPropertyRelative("Placeholder").objectReferenceValue as GameObject,
                });
            }
            return result;
        }
    }

    //# 마을 씬 쇼케이스 구조(village-monster-showcase §7, T-S10 일부) - 슬롯 6·그림자 6·구 배회자 0, 컨트롤러 배선.
    public class ShowcaseVillageSceneTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void Open() => _scene = DotSceneTestUtil.Open(DotSceneTestUtil.VillageScene);

        [OneTimeTearDown]
        public void Close() => DotSceneTestUtil.Close(_scene);

        [Test]
        public void 쇼케이스_무대는_Village_씬용이고_컨트롤러가_연결돼_있고_메타_수치는_씬에_직렬화되지_않는다()
        {
            MonsterShowcaseStage stage = DotSceneTestUtil.Find<MonsterShowcaseStage>(_scene);
            SerializedObject so = new SerializedObject(stage);
            Assert.AreEqual((int)EScene.Village, so.FindProperty("_scene").intValue);
            Assert.IsNull(so.FindProperty("_metaConfig"), "강화 Lv 계산용 MetaConfig 는 MetaConfig.Current 로 받는다(씬 직렬화 없음)");

            VillageController controller = DotSceneTestUtil.Find<VillageController>(_scene);
            Assert.IsNotNull(controller);
            Assert.AreSame(stage, new SerializedObject(controller).FindProperty("_monsterShowcase").objectReferenceValue, "VillageController 가 같은 무대를 참조");
        }

        [Test]
        public void 슬롯_홀더와_그림자는_서로_다른_오브젝트다()
        {
            List<ShowcaseSceneUtil.SlotInfo> slots = ShowcaseSceneUtil.Slots(_scene);
            HashSet<Transform> holders = new HashSet<Transform>();
            HashSet<SpriteRenderer> shadows = new HashSet<SpriteRenderer>();
            foreach (ShowcaseSceneUtil.SlotInfo slot in slots)
            {
                holders.Add(slot.Holder);
                shadows.Add(slot.Shadow);
                Assert.IsTrue(slot.Holder.name.Contains(((EMonster)slot.Species).ToString()), $"홀더 이름 {slot.Holder.name}");
            }
            Assert.AreEqual(6, holders.Count);
            Assert.AreEqual(6, shadows.Count, "그림자 6개 - 공유 금지(fit 스케일이 서로 간섭)");
        }

        [Test]
        public void 슬롯_종족은_기준_6종과_일치한다()
        {
            HashSet<int> expected = new HashSet<int>();
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                expected.Add((int)monster);
            }
            HashSet<int> actual = new HashSet<int>();
            foreach (ShowcaseSceneUtil.SlotInfo slot in ShowcaseSceneUtil.Slots(_scene))
            {
                actual.Add(slot.Species);
            }
            Assert.IsTrue(expected.SetEquals(actual), "등급 enum 이 슬롯 종족에 들어가면 ActorOf 조회 실패");
        }

        [Test]
        public void 마을_씬에_깨진_스크립트_참조가_없다()
        {
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), go.name);
            }
        }
    }

    //# 로딩 씬 쇼케이스 구조 - Actor_* 자리표시자 6 · 무대 1 · LoadingController 배선.
    public class ShowcaseLoadingSceneTests
    {
        private Scene _scene;

        [OneTimeSetUp]
        public void Open() => _scene = DotSceneTestUtil.Open(DotSceneTestUtil.LoadingScene);

        [OneTimeTearDown]
        public void Close() => DotSceneTestUtil.Close(_scene);

        [Test]
        public void 무대는_하나이고_Loading_용이며_LoadingController_가_참조한다()
        {
            List<MonsterShowcaseStage> stages = DotSceneTestUtil.FindAll<MonsterShowcaseStage>(_scene);
            Assert.AreEqual(1, stages.Count);
            Assert.AreEqual((int)EScene.Loading, new SerializedObject(stages[0]).FindProperty("_scene").intValue);

            LoadingController controller = DotSceneTestUtil.Find<LoadingController>(_scene);
            Assert.IsNotNull(controller);
            Assert.AreSame(stages[0], new SerializedObject(controller).FindProperty("_monsterShowcase").objectReferenceValue);
        }

        [Test]
        public void Actor_홀더는_6개이고_각_슬롯은_홀더_안의_정적_자리표시자를_가진다()
        {
            int actors = 0;
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                if (go.name.StartsWith("Actor_"))
                {
                    actors++;
                }
            }
            Assert.AreEqual(6, actors);

            HashSet<GameObject> placeholders = new HashSet<GameObject>();
            foreach (ShowcaseSceneUtil.SlotInfo slot in ShowcaseSceneUtil.Slots(_scene))
            {
                Assert.AreEqual("Actor_" + (EMonster)slot.Species, slot.Holder.name);
                Assert.IsNotNull(slot.Placeholder, $"{(EMonster)slot.Species} 자리표시자");
                Assert.IsTrue(slot.Placeholder.transform.IsChildOf(slot.Holder), "자리표시자는 자기 홀더 안(Pop 몬스터와 같은 위치에서 교체)");
                Assert.IsTrue(slot.Placeholder.activeSelf, "데이터 준비 전에는 정적 몬스터가 보여야 함(T-S8)");
                placeholders.Add(slot.Placeholder);
            }
            Assert.AreEqual(6, placeholders.Count);
        }

        [Test]
        public void 로딩_씬에_구_배회자가_없고_깨진_스크립트도_없다()
        {
            Assert.AreEqual(0, DotSceneTestUtil.FindAll<VillageWanderer>(_scene).Count);
            foreach (GameObject go in DotSceneTestUtil.AllObjects(_scene))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), go.name);
            }
        }
    }

    //# 몬스터 프리팹 30종(기준 6 + 등급 Variant 24)의 MonsterShowcase 배선(T-S1 전제) - 비면 쇼케이스에서 전투 요소가 켜진 채 보인다.
    public class ShowcasePrefabWiringTests
    {
        private const string CharDir = "Assets/_Lair/Art/Characters/";
        private static readonly string[] BattleBehaviourNames = { "AutoCombatAI", "SimpleMover", "SimpleRotator", "CharacterAnimationDriver", "DespawnOnDeath" };

        private static IEnumerable<string> AllPrefabNames()
        {
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                yield return monster.ToString();
                for (int grade = 2; grade <= 5; grade++)
                {
                    yield return MonsterGrades.Of(monster, grade).ToString();
                }
            }
        }

        private static HashSet<Object> RefSet(SerializedProperty array)
        {
            HashSet<Object> set = new HashSet<Object>();
            for (int i = 0; i < array.arraySize; i++)
            {
                set.Add(array.GetArrayElementAtIndex(i).objectReferenceValue);
            }
            return set;
        }

        [Test]
        public void 프리팹_30종은_LairCharacter_와_같은_루트에_MonsterShowcase_를_가진다()
        {
            int count = 0;
            foreach (string name in AllPrefabNames())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharDir}{name}.prefab");
                Assert.IsNotNull(prefab, name);
                Assert.IsNotNull(prefab.GetComponent<LairCharacter>(), $"{name} LairCharacter");
                Assert.IsNotNull(prefab.GetComponent<MonsterShowcase>(), $"{name} MonsterShowcase - LairCharacter.Get<IMonsterShowcase> 조회 전제");
                count++;
            }
            Assert.AreEqual(30, count);
        }

        [Test]
        public void MonsterShowcase_참조는_비어_있지_않고_자기_프리팹_안을_가리킨다()
        {
            foreach (string name in AllPrefabNames())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharDir}{name}.prefab");
                MonsterShowcase showcase = prefab.GetComponent<MonsterShowcase>();
                SerializedObject so = new SerializedObject(showcase);

                Assert.IsNotNull(so.FindProperty("_animator").objectReferenceValue, $"{name} _animator");
                Assert.IsNotNull(so.FindProperty("_visualRoot").objectReferenceValue, $"{name} _visualRoot");
                Assert.AreSame(prefab.GetComponent<MonsterEnhancementVisual>(), so.FindProperty("_enhancement").objectReferenceValue, $"{name} _enhancement - 자기 프리팹의 컴포넌트(Variant 가 기준을 가리키면 높이 0)");

                SerializedProperty behaviours = so.FindProperty("_battleBehaviours");
                Assert.Greater(behaviours.arraySize, 0, $"{name} _battleBehaviours");
                HashSet<Object> behaviourSet = RefSet(behaviours);
                Assert.IsFalse(behaviourSet.Contains(null), $"{name} 전투 컴포넌트 배열에 Missing");
                Assert.IsFalse(behaviourSet.Contains(showcase), $"{name} 자기 자신을 끄면 복구 불가");

                SerializedProperty hidden = so.FindProperty("_hiddenObjects");
                Assert.Greater(hidden.arraySize, 0, $"{name} _hiddenObjects");
                Assert.IsFalse(RefSet(hidden).Contains(null), $"{name} 숨김 배열에 Missing");
            }
        }

        [Test]
        public void 루트의_전투_컴포넌트_콜라이더_HP바_강화링은_모두_끄기_목록에_들어_있다()
        {
            foreach (string name in AllPrefabNames())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharDir}{name}.prefab");
                SerializedObject so = new SerializedObject(prefab.GetComponent<MonsterShowcase>());
                HashSet<Object> behaviours = RefSet(so.FindProperty("_battleBehaviours"));
                HashSet<Object> colliders = RefSet(so.FindProperty("_colliders"));
                HashSet<Object> hidden = RefSet(so.FindProperty("_hiddenObjects"));

                foreach (Behaviour behaviour in prefab.GetComponents<Behaviour>())
                {
                    if (System.Array.IndexOf(BattleBehaviourNames, behaviour.GetType().Name) >= 0)
                    {
                        Assert.IsTrue(behaviours.Contains(behaviour), $"{name} {behaviour.GetType().Name} 가 끄기 목록에 없음");
                    }
                }
                foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
                {
                    Assert.IsTrue(colliders.Contains(collider), $"{name} 콜라이더 {collider.name} 가 끄기 목록에 없음(마을에서 충돌)");
                }
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "HpBarWrapper" || child.name == "AuraSigil")
                    {
                        Assert.IsTrue(hidden.Contains(child.gameObject), $"{name} {child.name} 가 숨김 목록에 없음");
                    }
                }
            }
        }

        [Test]
        public void 정렬_대상_렌더러는_몸이_0번이고_등급_이펙트_렌더러가_전부_포함된다()
        {
            foreach (string name in AllPrefabNames())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharDir}{name}.prefab");
                SerializedProperty renderers = new SerializedObject(prefab.GetComponent<MonsterShowcase>()).FindProperty("_renderers");
                Assert.GreaterOrEqual(renderers.arraySize, 1, name);
                SpriteRenderer body = renderers.GetArrayElementAtIndex(0).objectReferenceValue as SpriteRenderer;
                Assert.IsNotNull(body, $"{name} 0번 렌더러");
                Assert.IsNotNull(body.GetComponent<MonsterVisual2D>(), $"{name} 0번 렌더러는 몸(Visual2D)");

                HashSet<Object> set = RefSet(renderers);
                Assert.IsFalse(set.Contains(null), $"{name} 렌더러 배열에 Missing");
                foreach (GradeFxSheet sheet in prefab.GetComponentsInChildren<GradeFxSheet>(true))
                {
                    Assert.IsTrue(set.Contains(sheet.GetComponent<SpriteRenderer>()), $"{name}/{sheet.name} 이펙트 렌더러가 정렬 목록에 없음 - 로딩에서 몸 뒤/앞 순서가 어긋남");
                }
            }
        }
    }
}
