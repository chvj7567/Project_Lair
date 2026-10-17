using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 궁수 월드 연동 — SkillGeometry.InLine, CharacterRegistry/HeroTargetProvider 최근접 차이, HeroSkillContext 실구현(레지스트리 기반),
    //# AutoCombatAI 카이팅 구성/도약/풀 재사용 리셋, 기사 경로 불변. 정적 레지스트리는 SetUp/TearDown 으로 보존·복원한다.
    public class ArcherWorldIntegrationTests : ChapterStaticStateTestBase
    {
        private const string KnightPrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private List<CharacterRegistry.Entry> _savedMonsters;

        [SetUp]
        public void 레지스트리를_보존하고_비운다()
        {
            ArcherFxLogGuard.Suppress();
            _savedMonsters = new List<CharacterRegistry.Entry>(CharacterRegistry.Monsters);
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void 레지스트리를_복원하고_생성물을_정리한다()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Monsters.AddRange(_savedMonsters);
            ArcherFxLogGuard.Restore();
        }

        private GameObject Spawn(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        private StageRuleFakeHero AddMonster(Vector3 position, bool engaging, int hp = 100)
        {
            GameObject go = Spawn("Monster", position);
            StageRuleFakeHero health = new StageRuleFakeHero { Current = hp };
            CharacterRegistry.RegisterMonster(go.transform, health);
            CharacterRegistry.SetMonsterEngaging(go.transform, engaging);
            return health;
        }

        private static object GetPrivate(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 — 시그니처 변경 감지");
            return fi.GetValue(target);
        }

        private static void InvokeNonPublic(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 존재 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        //# ===== SkillGeometry.InLine =====

        [TestCase(0f, 0f, true)]
        [TestCase(9f, 0f, true)]
        [TestCase(9.01f, 0f, false)]
        [TestCase(-0.01f, 0f, false)]
        [TestCase(5f, 0.6f, true)]
        [TestCase(5f, -0.6f, true)]
        [TestCase(5f, 0.61f, false)]
        [TestCase(9f, 0.6f, true)]
        public void 직선_판정은_시작_끝_반폭_경계를_포함한다(float along, float side, bool inside)
        {
            Assert.AreEqual(inside, SkillGeometry.InLine(new Vector3(along, 0f, side), Vector3.zero, Vector3.right, 9f, 0.6f));
        }

        [Test]
        public void 직선_판정은_높이를_무시한다()
        {
            Assert.IsTrue(SkillGeometry.InLine(new Vector3(4f, 50f, 0.2f), Vector3.zero, Vector3.right, 9f, 0.6f));
        }

        [Test]
        public void 직선_판정은_방향을_정규화하고_영벡터_방향은_항상_false다()
        {
            Assert.IsTrue(SkillGeometry.InLine(new Vector3(8f, 0f, 0f), Vector3.zero, new Vector3(3f, 0f, 0f), 9f, 0.6f), "비정규 방향도 길이 9 기준");
            Assert.IsFalse(SkillGeometry.InLine(Vector3.zero, Vector3.zero, Vector3.zero, 9f, 0.6f));
            Assert.IsFalse(SkillGeometry.InLine(Vector3.zero, Vector3.zero, new Vector3(0f, 5f, 0f), 9f, 0.6f), "수직 방향은 평면에서 영벡터");
        }

        [Test]
        public void 직선_판정은_원점이_아닌_시작점과_대각_방향에서도_성립한다()
        {
            Vector3 origin = new Vector3(2f, 0f, 2f);
            Vector3 dir = new Vector3(1f, 0f, 1f);

            Assert.IsTrue(SkillGeometry.InLine(new Vector3(5f, 0f, 5f), origin, dir, 9f, 0.6f));
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(5f, 0f, 3f), origin, dir, 9f, 0.6f), "축에서 옆으로 1.4 벗어남");
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(0f, 0f, 0f), origin, dir, 9f, 0.6f), "시작점 뒤쪽");
        }

        [Test]
        public void 길이_0인_직선은_시작점_근처_반폭_안만_포함한다()
        {
            Assert.IsTrue(SkillGeometry.InLine(new Vector3(0f, 0f, 0.3f), Vector3.zero, Vector3.right, 0f, 0.6f));
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(0.1f, 0f, 0f), Vector3.zero, Vector3.right, 0f, 0.6f));
        }

        //# ===== 최근접 위협 vs 영웅 타깃 제공자 =====

        [Test]
        public void 레지스트리_최근접_위협은_교전_토글과_무관하고_교전_전용_검색은_교전_몬스터만_본다()
        {
            AddMonster(new Vector3(5f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(2f, 0f, 0f), engaging: false);
            AddMonster(new Vector3(1f, 0f, 0f), engaging: false, hp: 0);
            AddMonster(new Vector3(1.5f, 0f, 0f), engaging: true, hp: 0);

            bool anyFound = CharacterRegistry.TryFindNearestMonsterAny(Vector3.zero, out Transform any, out IHealth anyHealth);
            bool engagedFound = CharacterRegistry.TryFindNearestMonster(Vector3.zero, out Transform engaged, out IHealth engagedHealth);

            Assert.IsTrue(anyFound);
            Assert.AreEqual(2f, any.position.x, 0.001f, "교전 토글 무관 — 살아있는 최근접");
            Assert.IsTrue(engagedFound);
            Assert.AreEqual(5f, engaged.position.x, 0.001f, "교전 중인 살아있는 몬스터만");
            Assert.IsTrue(anyHealth.IsAlive);
            Assert.IsTrue(engagedHealth.IsAlive);
        }

        [Test]
        public void 교전_중인_몬스터가_없으면_교전_전용_검색은_실패하고_최근접_위협은_성공한다()
        {
            AddMonster(new Vector3(3f, 0f, 0f), engaging: false);

            Assert.IsFalse(CharacterRegistry.TryFindNearestMonster(Vector3.zero, out Transform engaged, out IHealth engagedHealth));
            Assert.IsNull(engaged);
            Assert.IsNull(engagedHealth);
            Assert.IsTrue(CharacterRegistry.TryFindNearestMonsterAny(Vector3.zero, out Transform any, out IHealth anyHealth));
            Assert.IsNotNull(any);
        }

        [Test]
        public void 살아있는_몬스터가_없으면_최근접_위협도_실패한다()
        {
            AddMonster(new Vector3(1f, 0f, 0f), engaging: true, hp: 0);

            Assert.IsFalse(CharacterRegistry.TryFindNearestMonsterAny(Vector3.zero, out Transform t, out IHealth h));
            Assert.IsNull(t);
            Assert.IsNull(h);
        }

        [Test]
        public void 영웅_타깃_제공자의_TryFindNearest는_교전_몬스터만_TryFindNearestThreat는_교전_무관이다()
        {
            GameObject hero = Spawn("HeroProvider", Vector3.zero);
            HeroTargetProvider provider = hero.AddComponent<HeroTargetProvider>();
            AddMonster(new Vector3(6f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(2f, 0f, 0f), engaging: false);

            bool targetFound = provider.TryFindNearest(Vector3.zero, out Transform target, out IHealth targetHealth);
            bool threatFound = ((INearestThreatProvider)provider).TryFindNearestThreat(Vector3.zero, out Transform threat, out IHealth threatHealth);

            Assert.IsTrue(targetFound);
            Assert.AreEqual(6f, target.position.x, 0.001f);
            Assert.IsTrue(threatFound);
            Assert.AreEqual(2f, threat.position.x, 0.001f);
        }

        [Test]
        public void 영웅_타깃_제공자는_INearestThreatProvider를_구현하고_기사용_ITargetProvider도_유지한다()
        {
            GameObject hero = Spawn("HeroProvider2", Vector3.zero);
            HeroTargetProvider provider = hero.AddComponent<HeroTargetProvider>();

            Assert.IsInstanceOf<INearestThreatProvider>(provider);
            Assert.IsInstanceOf<ITargetProvider>(provider);
        }

        //# ===== HeroSkillContext 실구현 =====

        private HeroSkillContext ContextAt(Vector3 heroPosition)
        {
            return new HeroSkillContext(Spawn("Hero", heroPosition).transform);
        }

        [Test]
        public void 반경_안_몬스터_수는_살아있는_교전_몬스터만_세고_경계는_포함한다()
        {
            HeroSkillContext ctx = ContextAt(new Vector3(1f, 0f, 1f));
            AddMonster(new Vector3(3f, 0f, 1f), engaging: true);
            AddMonster(new Vector3(1f, 0f, 3f), engaging: true);
            AddMonster(new Vector3(3.01f, 0f, 1f), engaging: true);
            AddMonster(new Vector3(1.5f, 0f, 1f), engaging: false);
            AddMonster(new Vector3(1.2f, 0f, 1f), engaging: true, hp: 0);

            Assert.AreEqual(2, ctx.CountMonstersInRadius(2f));
        }

        [Test]
        public void 직선_타격은_범위_안_살아있는_교전_몬스터만_정확한_피해로_맞힌다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);
            StageRuleFakeHero inLine = AddMonster(new Vector3(3f, 0f, 0.3f), engaging: true);
            StageRuleFakeHero tooWide = AddMonster(new Vector3(5f, 0f, 0.7f), engaging: true);
            StageRuleFakeHero tooFar = AddMonster(new Vector3(9.5f, 0f, 0f), engaging: true);
            StageRuleFakeHero behind = AddMonster(new Vector3(-2f, 0f, 0f), engaging: true);
            StageRuleFakeHero notEngaging = AddMonster(new Vector3(4f, 0f, 0f), engaging: false);
            StageRuleFakeHero dead = AddMonster(new Vector3(2f, 0f, 0f), engaging: true, hp: 0);

            int hit = ctx.DamageMonstersInLine(Vector3.right, 9f, 0.6f, 70, 0f);

            Assert.AreEqual(1, hit);
            Assert.AreEqual(30, inLine.Current);
            Assert.AreEqual(100, tooWide.Current);
            Assert.AreEqual(100, tooFar.Current);
            Assert.AreEqual(100, behind.Current);
            Assert.AreEqual(100, notEngaging.Current);
            Assert.AreEqual(0, dead.Current);
        }

        [Test]
        public void 직선_타격은_줄_선_모든_몬스터를_관통해_맞히고_넉백은_영웅_반대_방향이다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);
            GameObject first = Spawn("M1", new Vector3(2f, 0f, 0f));
            GameObject second = Spawn("M2", new Vector3(6f, 0f, 0f));
            CharacterRegistry.RegisterMonster(first.transform, new StageRuleFakeHero());
            CharacterRegistry.RegisterMonster(second.transform, new StageRuleFakeHero());
            CharacterRegistry.SetMonsterEngaging(first.transform, true);
            CharacterRegistry.SetMonsterEngaging(second.transform, true);

            int hit = ctx.DamageMonstersInLine(Vector3.right, 9f, 0.6f, 10, 0.5f);

            Assert.AreEqual(2, hit, "관통");
            Assert.AreEqual(2.5f, first.transform.position.x, 0.001f);
            Assert.AreEqual(6.5f, second.transform.position.x, 0.001f);
        }

        [Test]
        public void 직선_타격은_넉백이_0이면_위치를_바꾸지_않는다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);
            GameObject target = Spawn("M", new Vector3(3f, 0f, 0f));
            CharacterRegistry.RegisterMonster(target.transform, new StageRuleFakeHero());
            CharacterRegistry.SetMonsterEngaging(target.transform, true);

            ctx.DamageMonstersInLine(Vector3.right, 9f, 0.6f, 10, 0f);

            Assert.AreEqual(3f, target.transform.position.x, 0.0001f);
        }

        [Test]
        public void 밀집_지점은_가장_많이_모인_군집의_무게중심이다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);
            AddMonster(new Vector3(3f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(3.5f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(3f, 0f, 0.5f), engaging: true);
            AddMonster(new Vector3(-6.5f, 0f, 0f), engaging: true);

            bool found = ctx.TryFindMonsterDensePoint(7f, 2.8f, out Vector3 point);

            Assert.IsTrue(found);
            Assert.AreEqual(3.1667f, point.x, 0.001f);
            Assert.AreEqual(0.1667f, point.z, 0.001f);
        }

        [Test]
        public void 군집_크기가_같으면_영웅에_더_가까운_군집을_고른다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);
            AddMonster(new Vector3(-5f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(-5.5f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(2f, 0f, 0f), engaging: true);
            AddMonster(new Vector3(2.5f, 0f, 0f), engaging: true);

            ctx.TryFindMonsterDensePoint(7f, 2.8f, out Vector3 point);

            Assert.AreEqual(2.25f, point.x, 0.001f);
        }

        [Test]
        public void 밀집_지점_탐색은_탐색_반경_밖_비교전_사망_몬스터를_제외하고_없으면_실패한다()
        {
            HeroSkillContext ctx = ContextAt(new Vector3(1f, 0f, 1f));
            AddMonster(new Vector3(20f, 0f, 1f), engaging: true);
            AddMonster(new Vector3(2f, 0f, 1f), engaging: false);
            AddMonster(new Vector3(3f, 0f, 1f), engaging: true, hp: 0);

            bool found = ctx.TryFindMonsterDensePoint(7f, 2.8f, out Vector3 point);

            Assert.IsFalse(found);
            Assert.AreEqual(new Vector3(1f, 0f, 1f), point, "실패 시 영웅 위치");
        }

        [Test]
        public void 몬스터_무게중심은_위협이_없으면_영웅_위치를_돌려준다()
        {
            HeroSkillContext ctx = ContextAt(new Vector3(2f, 0f, 3f));

            Assert.AreEqual(new Vector3(2f, 0f, 3f), ctx.MonsterCentroid(8f));
        }

        [Test]
        public void 도약_클램프는_AI_없이는_제한이_없다()
        {
            HeroSkillContext ctx = ContextAt(Vector3.zero);

            Assert.AreEqual(new Vector3(50f, 0f, 0f), ctx.ClampToArena(new Vector3(50f, 0f, 0f)));
        }

        [Test]
        public void 도약은_AI_없이는_영웅_위치를_목적지로_즉시_옮긴다()
        {
            GameObject hero = Spawn("HeroNoAi", Vector3.zero);
            HeroSkillContext ctx = new HeroSkillContext(hero.transform);

            ctx.BeginLeap(new Vector3(3f, 0f, 4f), 0.25f);

            Assert.AreEqual(new Vector3(3f, 0f, 4f), hero.transform.position);
        }

        [Test]
        public void 도약_클램프는_카이팅이_구성된_AI의_소프트_반경을_쓴다()
        {
            GameObject hero = Spawn("HeroAi", Vector3.zero);
            AutoCombatAI ai = hero.AddComponent<AutoCombatAI>();
            ai.ConfigureKite(new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f));
            HeroSkillContext ctx = new HeroSkillContext(hero.transform);

            Vector3 clamped = ctx.ClampToArena(new Vector3(10f, 0f, 0f));

            Assert.AreEqual(7f, clamped.x, 0.001f);
        }

        [Test]
        public void 도약은_AI가_있으면_이동_소유권을_AI에_넘기고_위치를_즉시_바꾸지_않는다()
        {
            GameObject hero = Spawn("HeroAi2", new Vector3(1f, 0f, 1f));
            AutoCombatAI ai = hero.AddComponent<AutoCombatAI>();
            HeroSkillContext ctx = new HeroSkillContext(hero.transform);

            ctx.BeginLeap(new Vector3(5f, 0f, 5f), 0.25f);

            Assert.IsTrue(ai.IsLeaping);
            Assert.AreEqual(new Vector3(1f, 0f, 1f), hero.transform.position);
        }

        //# ===== AutoCombatAI 카이팅 구성 / 풀 재사용 =====

        [Test]
        public void 카이팅을_구성하지_않은_AI는_아레나_반경_0_도약_아님이다()
        {
            GameObject go = Spawn("KnightLike", Vector3.zero);
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();

            Assert.AreEqual(0f, ai.KiteArenaSoftRadius);
            Assert.IsFalse(ai.IsLeaping);
            Assert.IsNull(GetPrivate(ai, "_kite"));
        }

        [Test]
        public void ConfigureKite는_소프트_반경을_노출하고_KiteBehavior를_접근_상태로_만든다()
        {
            GameObject go = Spawn("ArcherLike", Vector3.zero);
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();

            ai.ConfigureKite(new KiteSettings(3f, 4.5f, 4f, 6.5f, 0.5f, 0.3f, 0.4f));

            Assert.AreEqual(6.5f, ai.KiteArenaSoftRadius, 0.0001f);
            KiteBehavior kite = (KiteBehavior)GetPrivate(ai, "_kite");
            Assert.IsNotNull(kite);
            Assert.AreEqual(EKiteState.Approach, kite.State);
        }

        [Test]
        public void BeginLeap은_짧은_지속시간도_도약_상태로_만든다()
        {
            GameObject go = Spawn("Leaper", Vector3.zero);
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();

            ai.BeginLeap(new Vector3(4f, 0f, 0f), 0f);

            Assert.IsTrue(ai.IsLeaping, "duration 0 은 0.01 로 보정");
        }

        [Test]
        public void 풀_재사용_OnEnable은_카이팅_상태와_도약_상태를_초기화한다()
        {
            GameObject go = Spawn("PooledArcher", Vector3.zero);
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();
            ai.ConfigureKite(new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f));
            KiteBehavior kite = (KiteBehavior)GetPrivate(ai, "_kite");
            kite.Update(0f, new KiteInput { Position = Vector3.zero, HasThreat = true, NearestThreatDist = 1f, NearestThreatPos = Vector3.right, TargetDist = 5f, Range = 6f, EngageBuffer = 0.5f });
            ai.BeginLeap(new Vector3(4f, 0f, 0f), 0.25f);
            Assert.AreEqual(EKiteState.Retreat, kite.State, "전제");
            Assert.IsTrue(ai.IsLeaping, "전제");

            InvokeNonPublic(ai, "OnEnable");

            Assert.AreEqual(EKiteState.Approach, kite.State);
            Assert.IsFalse(ai.IsLeaping);
            Assert.AreEqual(Vector3.zero, kite.RetreatDirection);
        }

        [Test]
        public void 카이팅이_없는_AI의_OnEnable은_예외없이_도약_상태만_초기화한다()
        {
            GameObject go = Spawn("KnightPool", Vector3.zero);
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();
            ai.BeginLeap(Vector3.one, 0.25f);

            Assert.DoesNotThrow(() => InvokeNonPublic(ai, "OnEnable"));

            Assert.IsFalse(ai.IsLeaping);
        }

        //# ===== 기사 경로 불변 =====

        [Test]
        public void 기사_프리팹의_AutoCombatAI는_카이팅_미구성이고_도약_상태가_아니다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath);
            Assert.IsNotNull(prefab, KnightPrefabPath);
            AutoCombatAI ai = prefab.GetComponent<AutoCombatAI>();
            Assert.IsNotNull(ai);

            Assert.IsNull(GetPrivate(ai, "_kite"), "기사는 KiteBehavior 가 생성되지 않는다");
            Assert.AreEqual(0f, ai.KiteArenaSoftRadius);
            Assert.IsFalse(ai.IsLeaping);
        }

        [Test]
        public void 기사_프리팹에는_최근접_위협_제공자가_붙어_있지만_카이팅이_없으면_사용되지_않는다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath);
            HeroTargetProvider provider = prefab.GetComponent<HeroTargetProvider>();

            Assert.IsNotNull(provider, "영웅 타깃 제공자(INearestThreatProvider 구현)는 기사 프리팹에도 있다");
            Assert.IsNull(GetPrivate(prefab.GetComponent<AutoCombatAI>(), "_kite"));
        }
    }
}
