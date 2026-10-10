using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 암살자 월드 연동 - Health 무적, 이동/사거리 보정 채널 분리, HeroSkillContext 실구현(레지스트리 기반), AutoCombatAI 습격 구성/리셋,
    //# 로드아웃 매핑, 기사/궁수 경로 불변 (chapter-3-hero §3.3·§4·§7). 정적 레지스트리는 SetUp/TearDown 으로 보존·복원한다.
    public class AssassinWorldIntegrationTests : ChapterStaticStateTestBase
    {
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

        private static void InvokeNonPublic(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 존재 - 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        private static object GetPrivate(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 - 시그니처 변경 감지");
            return fi.GetValue(target);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 - 시그니처 변경 감지");
            fi.SetValue(target, value);
        }

        //# 실제 몬스터 구성(Health + MeleeAttacker + SimpleMover)을 레지스트리에 등록한다.
        private GameObject AddMonster(Vector3 position, int power, float cooldown, int hp, bool engaging = true)
        {
            GameObject go = Spawn("Monster", position);
            Health health = go.AddComponent<Health>();
            health.SetMax(hp, true);
            MeleeAttacker attacker = go.AddComponent<MeleeAttacker>();
            attacker.Configure(2f, cooldown, power);
            go.AddComponent<SimpleMover>();
            CharacterRegistry.RegisterMonster(go.transform, health, attacker);
            CharacterRegistry.SetMonsterEngaging(go.transform, engaging);
            return go;
        }

        //# ===== Health 무적 =====

        private Health NewHealth(int max = 100)
        {
            Health health = Spawn("Hero", Vector3.zero).AddComponent<Health>();
            health.SetMax(max, true);
            return health;
        }

        [Test]
        public void Health는_IInvulnerable을_구현하고_초기에는_무적이_아니다()
        {
            Health health = NewHealth();

            Assert.IsInstanceOf<IInvulnerable>(health);
            Assert.IsFalse(health.IsInvulnerable);
        }

        [Test]
        public void 무적_중_피해는_무시되고_변경_이벤트도_발행되지_않는다()
        {
            Health health = NewHealth();
            int changed = 0;
            health.OnChanged += (current, max) => ++changed;
            health.GrantInvulnerability(5f);

            health.TakeDamage(60);

            Assert.AreEqual(100, health.Current);
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void 무적이_만료되면_피해가_다시_들어간다()
        {
            Health health = NewHealth();
            health.GrantInvulnerability(5f);
            SetPrivate(health, "_invulnerableUntil", Time.time - 1f);

            health.TakeDamage(30);

            Assert.IsFalse(health.IsInvulnerable);
            Assert.AreEqual(70, health.Current);
        }

        [Test]
        public void 짧은_무적이_이미_있는_긴_무적을_줄이지_않는다()
        {
            Health health = NewHealth();
            health.GrantInvulnerability(100f);
            float longUntil = (float)GetPrivate(health, "_invulnerableUntil");

            health.GrantInvulnerability(0.1f);

            Assert.AreEqual(longUntil, (float)GetPrivate(health, "_invulnerableUntil"), 0.0001f);
        }

        [TestCase(0f)]
        [TestCase(-3f)]
        public void 영_이하_초의_무적_부여는_무효다(float seconds)
        {
            Health health = NewHealth();

            health.GrantInvulnerability(seconds);

            Assert.IsFalse(health.IsInvulnerable);
        }

        [Test]
        public void 받는_피해_배율과_무적은_독립이라_무적_해제_뒤에도_배율은_유지된다()
        {
            Health health = NewHealth();
            health.DamageTakenScale = 0.5f;
            health.GrantInvulnerability(5f);

            health.TakeDamage(40);
            Assert.AreEqual(100, health.Current);

            SetPrivate(health, "_invulnerableUntil", Time.time - 1f);
            health.TakeDamage(40);
            Assert.AreEqual(80, health.Current, "40 x 0.5");
        }

        [Test]
        public void 무적_중에도_회복은_가능하다()
        {
            Health health = NewHealth();
            health.TakeDamage(50);
            health.GrantInvulnerability(5f);

            health.Heal(20);

            Assert.AreEqual(70, health.Current);
        }

        [Test]
        public void 풀_재사용_OnEnable은_무적과_받는_피해_배율을_초기화한다()
        {
            Health health = NewHealth();
            health.GrantInvulnerability(100f);
            health.DamageTakenScale = 0.3f;

            InvokeNonPublic(health, "OnEnable");

            Assert.IsFalse(health.IsInvulnerable);
            Assert.AreEqual(1f, health.DamageTakenScale, 0.0001f);
        }

        //# ===== SimpleMover.SkillSpeedScale =====

        private SimpleMover NewMover()
        {
            GameObject go = Spawn("Mover", Vector3.zero);
            SimpleMover mover = go.AddComponent<SimpleMover>();
            mover.Speed = 3f;
            return mover;
        }

        private float StepDistance(SimpleMover mover)
        {
            mover.transform.position = Vector3.zero;
            mover.MoveTo(new Vector3(1000f, 0f, 0f));
            InvokeNonPublic(mover, "FixedUpdate");
            return mover.transform.position.x;
        }

        [Test]
        public void SkillSpeedScale은_기본_1이고_이동_속도에_곱해진다()
        {
            SimpleMover mover = NewMover();
            Assert.AreEqual(1f, mover.SkillSpeedScale, 0.0001f);
            float baseline = StepDistance(mover);

            mover.SkillSpeedScale = 0.6f;

            Assert.AreEqual(baseline * 0.6f, StepDistance(mover), baseline * 0.001f);
        }

        [Test]
        public void 버프_규칙_스킬_세_속도_채널은_서로_독립으로_곱해진다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover);

            mover.SpeedScale = 1.3f;
            mover.RuleSpeedScale = 0.5f;
            mover.SkillSpeedScale = 0.6f;

            Assert.AreEqual(baseline * 1.3f * 0.5f * 0.6f, StepDistance(mover), baseline * 0.001f);
        }

        [Test]
        public void 버프_틱이_SpeedScale을_1로_리셋해도_규칙과_스킬_감속은_유지된다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover);
            mover.RuleSpeedScale = 0.5f;
            mover.SkillSpeedScale = 0.6f;

            mover.SpeedScale = 1.3f;
            mover.SpeedScale = 1f;

            Assert.AreEqual(baseline * 0.5f * 0.6f, StepDistance(mover), baseline * 0.001f);
        }

        [Test]
        public void 규칙_감속_복구는_스킬_감속을_지우지_않는다()
        {
            SimpleMover mover = NewMover();
            float baseline = StepDistance(mover);
            mover.RuleSpeedScale = 0.5f;
            mover.SkillSpeedScale = 0.6f;

            mover.RuleSpeedScale = 1f;

            Assert.AreEqual(baseline * 0.6f, StepDistance(mover), baseline * 0.001f);
        }

        [Test]
        public void 풀_재사용_OnEnable은_세_속도_배율을_모두_1로_리셋한다()
        {
            SimpleMover mover = NewMover();
            mover.SpeedScale = 1.3f;
            mover.RuleSpeedScale = 0.5f;
            mover.SkillSpeedScale = 0.6f;

            InvokeNonPublic(mover, "OnEnable");

            Assert.AreEqual(1f, mover.SpeedScale, 0.0001f);
            Assert.AreEqual(1f, mover.RuleSpeedScale, 0.0001f);
            Assert.AreEqual(1f, mover.SkillSpeedScale, 0.0001f);
        }

        //# ===== MeleeAttacker 사거리 보정 / 실효 쿨다운 / strike 취소 =====

        private MeleeAttacker NewAttacker(float range = 2f, float cooldown = 1f, int power = 10)
        {
            MeleeAttacker attacker = Spawn("Attacker", Vector3.zero).AddComponent<MeleeAttacker>();
            attacker.Configure(range, cooldown, power);
            InvokeNonPublic(attacker, "OnEnable");
            return attacker;
        }

        [Test]
        public void 사거리_보정_기본값은_스케일_1_하한_0이고_Range는_원래_값이다()
        {
            MeleeAttacker attacker = NewAttacker(2f);

            Assert.AreEqual(1f, attacker.RangeScale, 0.0001f);
            Assert.AreEqual(0f, attacker.RangeFloor, 0.0001f);
            Assert.AreEqual(2f, attacker.Range, 0.0001f);
        }

        [Test]
        public void 사거리_보정은_하한을_지키고_원래_사거리가_하한보다_짧으면_늘리지_않는다()
        {
            MeleeAttacker attacker = NewAttacker(2f);
            attacker.SetRangeScale(0.5f, 0.8f);
            Assert.AreEqual(1.0f, attacker.Range, 0.0001f);

            attacker.Configure(1.0f, 1f, 10);
            Assert.AreEqual(0.8f, attacker.Range, 0.0001f, "1.0 x 0.5 = 0.5 -> 하한 0.8");

            attacker.Configure(0.5f, 1f, 10);
            Assert.AreEqual(0.5f, attacker.Range, 0.0001f, "원래 0.5 < 하한 0.8 이면 불변");
        }

        [Test]
        public void 사거리_보정은_공격_판정에도_적용되고_해제하면_복구된다()
        {
            MeleeAttacker attacker = NewAttacker(2f);
            StageRuleFakeHero target = new StageRuleFakeHero();
            Vector3 targetPos = new Vector3(1.5f, 0f, 0f);

            attacker.SetRangeScale(0.5f, 0.8f);
            Assert.IsFalse(attacker.TryAttack(target, Vector3.zero, targetPos, 10f), "보정 사거리 1.0 < 1.5");

            attacker.SetRangeScale(1f, 0f);
            Assert.IsTrue(attacker.TryAttack(target, Vector3.zero, targetPos, 20f));
        }

        [Test]
        public void 풀_재사용_OnEnable은_사거리_보정을_초기화한다()
        {
            MeleeAttacker attacker = NewAttacker(2f);
            attacker.SetRangeScale(0.5f, 0.8f);

            InvokeNonPublic(attacker, "OnEnable");

            Assert.AreEqual(1f, attacker.RangeScale, 0.0001f);
            Assert.AreEqual(0f, attacker.RangeFloor, 0.0001f);
        }

        [Test]
        public void 실효_쿨다운은_기본_쿨다운_곱_쿨다운_스케일이고_기본_Cooldown은_변하지_않는다()
        {
            MeleeAttacker attacker = NewAttacker(2f, 2f);

            attacker.CooldownScale = 0.5f;

            Assert.AreEqual(1f, attacker.EffectiveCooldown, 0.0001f);
            Assert.AreEqual(2f, attacker.Cooldown, 0.0001f);
            Assert.IsInstanceOf<IEffectiveCooldown>(attacker);
        }

        [Test]
        public void 보류_중인_strike를_취소하면_헛스윙이_되고_쿨다운은_이미_기록돼_있다()
        {
            MeleeAttacker attacker = NewAttacker(2f, 1f, 10);
            StageRuleFakeHero target = new StageRuleFakeHero();
            Assert.IsTrue(attacker.TryBeginAttack(target, Vector3.zero, Vector3.right, 10f));

            attacker.CancelPendingStrike();

            Assert.IsFalse(attacker.TryApplyStrike(10.2f));
            Assert.AreEqual(100, target.Current, "피해 없음");
            Assert.IsFalse(attacker.TryBeginAttack(target, Vector3.zero, Vector3.right, 10.5f), "쿨다운 1.0 이 개시 시점에 기록돼 있다");
        }

        [Test]
        public void 취소하지_않은_strike는_정상_적용된다_기준_동작()
        {
            MeleeAttacker attacker = NewAttacker(2f, 1f, 10);
            StageRuleFakeHero target = new StageRuleFakeHero();
            attacker.TryBeginAttack(target, Vector3.zero, Vector3.right, 10f);

            Assert.IsTrue(attacker.TryApplyStrike(10.2f));
            Assert.AreEqual(90, target.Current);
        }

        [Test]
        public void 사거리_보정_공식_두_진입점은_같은_결과를_낸다()
        {
            foreach (float range in new[] { 0.5f, 1f, 2f, 5f })
            {
                Assert.AreEqual(RangeScaleMath.Compute(range, 0.5f, 0.8f), MonsterRangeModifier.Compute(range, 0.5f, 0.8f), 0.0001f, $"range {range}");
            }
        }

        //# ===== 레지스트리 공격자 캐싱 / 후보 =====

        [Test]
        public void 몬스터_등록은_공격자를_캐싱하고_공격자가_없으면_null이다()
        {
            GameObject withAttacker = AddMonster(Vector3.zero, 10, 1f, 100);
            GameObject plain = Spawn("Plain", Vector3.right);
            CharacterRegistry.RegisterMonster(plain.transform, new StageRuleFakeHero());

            Assert.IsNotNull(CharacterRegistry.Monsters.Find(e => e.Transform == withAttacker.transform).Attacker);
            Assert.IsNull(CharacterRegistry.Monsters.Find(e => e.Transform == plain.transform).Attacker);
        }

        [Test]
        public void 몬스터_등록에_null_Transform을_주면_아무것도_등록되지_않는다()
        {
            int before = CharacterRegistry.Monsters.Count;

            CharacterRegistry.RegisterMonster(null, new StageRuleFakeHero(), null);

            Assert.AreEqual(before, CharacterRegistry.Monsters.Count);
        }

        [Test]
        public void 위협_후보는_교전_중인_몬스터만_반경_경계를_포함해_담고_호출마다_목록을_비운다()
        {
            AddMonster(new Vector3(9f, 0f, 0f), 10, 1f, 100);
            AddMonster(new Vector3(9.01f, 0f, 0f), 10, 1f, 100);
            AddMonster(new Vector3(2f, 0f, 0f), 10, 1f, 100, engaging: false);
            List<SkirmishCandidate> list = new List<SkirmishCandidate> { new SkirmishCandidate() };

            bool found = CharacterRegistry.TryGetThreatCandidates(Vector3.zero, 9f, list);

            Assert.IsTrue(found);
            Assert.AreEqual(1, list.Count, "이전 내용 소거 + 반경 9 포함 / 9.01 제외 / 행군 중 제외");
            Assert.AreEqual(0.1f, list[0].Score, 0.0001f);
        }

        [Test]
        public void 위협_후보가_없으면_false이고_목록은_비어_있다()
        {
            AddMonster(new Vector3(20f, 0f, 0f), 10, 1f, 100);
            List<SkirmishCandidate> list = new List<SkirmishCandidate>();

            Assert.IsFalse(CharacterRegistry.TryGetThreatCandidates(Vector3.zero, 9f, list));
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void 쿨다운_버프를_받은_몬스터의_후보_점수가_올라간다()
        {
            GameObject monster = AddMonster(new Vector3(2f, 0f, 0f), 10, 1f, 100);
            List<SkirmishCandidate> list = new List<SkirmishCandidate>();
            CharacterRegistry.TryGetThreatCandidates(Vector3.zero, 9f, list);
            float before = list[0].Score;

            monster.GetComponent<MeleeAttacker>().CooldownScale = 0.5f;
            CharacterRegistry.TryGetThreatCandidates(Vector3.zero, 9f, list);

            Assert.AreEqual(before * 2f, list[0].Score, 0.0001f);
        }

        [Test]
        public void 영웅_타깃_제공자는_ISkirmishThreatProvider를_구현하고_후보_조회가_레지스트리와_같다()
        {
            GameObject hero = Spawn("HeroProvider", Vector3.zero);
            HeroTargetProvider provider = hero.AddComponent<HeroTargetProvider>();
            AddMonster(new Vector3(3f, 0f, 0f), 10, 1f, 100);
            List<SkirmishCandidate> list = new List<SkirmishCandidate>();

            bool found = ((ISkirmishThreatProvider)provider).TryGetThreatCandidates(Vector3.zero, 9f, list);

            Assert.IsTrue(found);
            Assert.AreEqual(1, list.Count);
        }

        //# ===== HeroSkillContext 암살자 구현 =====

        private HeroSkillContext NewHeroContext(out GameObject hero, bool withAi = false)
        {
            hero = Spawn("HeroCtx", Vector3.zero);
            hero.AddComponent<LairCharacter>();
            hero.AddComponent<Health>().SetMax(100, true);
            hero.AddComponent<MeleeAttacker>();
            if (withAi)
            {
                hero.AddComponent<AutoCombatAI>();
            }
            return new HeroSkillContext(hero.transform);
        }

        [Test]
        public void 영웅_행동_가능_판정은_생존과_공격자_활성이다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            Assert.IsTrue(ctx.IsHeroActive);

            hero.GetComponent<MeleeAttacker>().Enabled = false;
            Assert.IsFalse(ctx.IsHeroActive, "TimeStop/침묵");

            hero.GetComponent<MeleeAttacker>().Enabled = true;
            hero.GetComponent<Health>().TakeDamage(1000);
            Assert.IsFalse(ctx.IsHeroActive, "사망");
        }

        [Test]
        public void 최상위_위협은_사거리_안에서_점수가_가장_높은_교전_몬스터다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            AddMonster(new Vector3(3f, 0f, 0f), 10, 1f, 100);
            GameObject danger = AddMonster(new Vector3(5f, 0f, 0f), 12, 1f, 100);
            AddMonster(new Vector3(12f, 0f, 0f), 99, 1f, 100);
            AddMonster(new Vector3(2f, 0f, 0f), 99, 1f, 100, engaging: false);

            bool found = ctx.TryFindTopThreat(9f, out Vector3 position, out IHealth health);

            Assert.IsTrue(found);
            Assert.AreEqual(danger.transform.position, position);
            Assert.AreSame(danger.GetComponent<Health>(), health);
        }

        [Test]
        public void 최상위_위협은_점수가_같으면_가까운_쪽이고_없으면_false다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            Assert.IsFalse(ctx.TryFindTopThreat(9f, out Vector3 none, out IHealth noneHealth));
            Assert.IsNull(noneHealth);

            AddMonster(new Vector3(6f, 0f, 0f), 10, 1f, 100);
            GameObject near = AddMonster(new Vector3(4f, 0f, 0f), 10, 1f, 100);

            ctx.TryFindTopThreat(9f, out Vector3 position, out IHealth health);

            Assert.AreEqual(near.transform.position, position);
        }

        [Test]
        public void 최상위_위협은_사거리_경계를_포함한다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            AddMonster(new Vector3(9f, 0f, 0f), 10, 1f, 100);

            Assert.IsTrue(ctx.TryFindTopThreat(9f, out Vector3 p1, out IHealth h1));
            Assert.IsFalse(ctx.TryFindTopThreat(8.99f, out Vector3 p2, out IHealth h2));
        }

        [Test]
        public void 몬스터_피해는_영웅_공격_배율을_곱하고_처치하면_true를_돌려준다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            hero.GetComponent<MeleeAttacker>().PowerScale = 2f;
            GameObject monster = AddMonster(new Vector3(3f, 0f, 0f), 10, 1f, 100);
            Health health = monster.GetComponent<Health>();

            bool killed = ctx.DamageMonster(health, 30);
            Assert.IsFalse(killed);
            Assert.AreEqual(40, health.Current, "30 x 2");

            Assert.IsTrue(ctx.DamageMonster(health, 30), "남은 40 이 60 에 처치");
            Assert.AreEqual(0, health.Current);
        }

        [Test]
        public void 몬스터_피해는_null이거나_이미_죽은_표적에는_false이고_레지스트리에_없는_표적은_피해_없이_false다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            GameObject dead = AddMonster(new Vector3(3f, 0f, 0f), 10, 1f, 100);
            dead.GetComponent<Health>().TakeDamage(1000);
            Health stranger = Spawn("Stranger", Vector3.zero).AddComponent<Health>();
            stranger.SetMax(50, true);

            Assert.IsFalse(ctx.DamageMonster(null, 10));
            Assert.IsFalse(ctx.DamageMonster(dead.GetComponent<Health>(), 10));
            Assert.IsFalse(ctx.DamageMonster(stranger, 10));
            Assert.AreEqual(50, stranger.Current);
        }

        [Test]
        public void 영웅_무적_부여와_받는_피해_배율_곱은_영웅_Health에_반영된다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            Health health = hero.GetComponent<Health>();

            ctx.GrantHeroInvulnerability(5f);
            Assert.IsTrue(health.IsInvulnerable);

            ctx.MultiplyHeroDamageTaken(0.7f);
            ctx.MultiplyHeroDamageTaken(1f / 0.7f);
            Assert.AreEqual(1f, health.DamageTakenScale, 0.0001f, "곱과 역수 원복");
        }

        [Test]
        public void 순간이동은_AI가_없으면_위치를_직접_옮기고_AI가_있으면_높이를_보존하며_도약을_끊는다()
        {
            HeroSkillContext plain = NewHeroContext(out GameObject plainHero);
            plain.TeleportHero(new Vector3(3f, 0f, 4f));
            Assert.AreEqual(new Vector3(3f, 0f, 4f), plainHero.transform.position);

            HeroSkillContext withAi = NewHeroContext(out GameObject aiHero, withAi: true);
            aiHero.transform.position = new Vector3(0f, 1f, 0f);
            AutoCombatAI ai = aiHero.GetComponent<AutoCombatAI>();
            ai.BeginLeap(new Vector3(5f, 0f, 5f), 0.5f);
            Assert.IsTrue(ai.IsLeaping);

            withAi.TeleportHero(new Vector3(3f, 9f, 4f));

            Assert.AreEqual(new Vector3(3f, 1f, 4f), aiHero.transform.position, "높이는 영웅 높이 유지");
            Assert.IsFalse(ai.IsLeaping);
        }

        [Test]
        public void 연막_지대는_안의_몬스터만_감속하고_사거리를_줄이며_밖으로_나가면_즉시_복구한다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            GameObject inside = AddMonster(new Vector3(1f, 0f, 0f), 10, 1f, 100);
            GameObject outside = AddMonster(new Vector3(6f, 0f, 0f), 10, 1f, 100);

            ctx.ApplyMonsterSlowZone(Vector3.zero, 3f, 0.6f, 0.5f, 0.8f);

            Assert.AreEqual(0.6f, inside.GetComponent<SimpleMover>().SkillSpeedScale, 0.0001f);
            Assert.AreEqual(1.0f, inside.GetComponent<MeleeAttacker>().Range, 0.0001f, "2.0 x 0.5, 하한 0.8");
            Assert.AreEqual(1f, outside.GetComponent<SimpleMover>().SkillSpeedScale, 0.0001f);
            Assert.AreEqual(2f, outside.GetComponent<MeleeAttacker>().Range, 0.0001f);

            inside.transform.position = new Vector3(6f, 0f, 0f);
            ctx.ApplyMonsterSlowZone(Vector3.zero, 3f, 0.6f, 0.5f, 0.8f);

            Assert.AreEqual(1f, inside.GetComponent<SimpleMover>().SkillSpeedScale, 0.0001f);
            Assert.AreEqual(2f, inside.GetComponent<MeleeAttacker>().Range, 0.0001f);
        }

        [Test]
        public void 연막_사거리_하한은_충돌_거리가_크면_충돌_거리_더하기_0_1이다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            GameObject monster = AddMonster(new Vector3(1f, 0f, 0f), 10, 1f, 100);
            CapsuleCollider capsule = monster.AddComponent<CapsuleCollider>();
            capsule.radius = 1f;

            ctx.ApplyMonsterSlowZone(Vector3.zero, 3f, 0.6f, 0.5f, 0.8f);

            Assert.AreEqual(1.1f, monster.GetComponent<MeleeAttacker>().Range, 0.001f, "max(0.8, 영웅 0 + 몬스터 1.0 + 0.1) = 1.1, 2.0 x 0.5 = 1.0 -> 1.1");
        }

        [Test]
        public void 연막_해제는_영향받던_몬스터를_전원_복구하고_규칙_감속은_건드리지_않는다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            GameObject a = AddMonster(new Vector3(1f, 0f, 0f), 10, 1f, 100);
            GameObject b = AddMonster(new Vector3(-1f, 0f, 1f), 10, 1f, 100);
            a.GetComponent<SimpleMover>().RuleSpeedScale = 0.5f;
            ctx.ApplyMonsterSlowZone(Vector3.zero, 3f, 0.6f, 0.5f, 0.8f);

            ctx.ClearMonsterSlowZone();

            foreach (GameObject monster in new[] { a, b })
            {
                Assert.AreEqual(1f, monster.GetComponent<SimpleMover>().SkillSpeedScale, 0.0001f);
                Assert.AreEqual(2f, monster.GetComponent<MeleeAttacker>().Range, 0.0001f);
            }
            Assert.AreEqual(0.5f, a.GetComponent<SimpleMover>().RuleSpeedScale, 0.0001f, "DenseFog/HunterTraps 채널 불변");
        }

        [Test]
        public void 연막_지대는_죽은_몬스터를_건드리지_않고_연속_호출에도_안전하다()
        {
            HeroSkillContext ctx = NewHeroContext(out GameObject hero);
            GameObject dead = AddMonster(new Vector3(1f, 0f, 0f), 10, 1f, 100);
            dead.GetComponent<Health>().TakeDamage(1000);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 5; ++i)
                {
                    ctx.ApplyMonsterSlowZone(Vector3.zero, 3f, 0.6f, 0.5f, 0.8f);
                }
                ctx.ClearMonsterSlowZone();
            });
            Assert.AreEqual(1f, dead.GetComponent<SimpleMover>().SkillSpeedScale, 0.0001f);
        }

        [Test]
        public void 슬립_요청은_AI가_없으면_무동작이고_습격_AI에는_강제_슬립으로_전달된다()
        {
            HeroSkillContext noAi = NewHeroContext(out GameObject plainHero);
            Assert.DoesNotThrow(() => noAi.RequestHeroSlip());

            HeroSkillContext withAi = NewHeroContext(out GameObject hero, withAi: true);
            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.ConfigureSkirmish(new SkirmishSettings(9f, 1.5f, 2.8f, 3, 2f, 3.5f, 0.2f, 0.2f, 5f, 0.4f, 7f));

            withAi.RequestHeroSlip();

            SkirmishBehavior behavior = (SkirmishBehavior)GetPrivate(ai, "_skirmish");
            Assert.IsTrue(behavior.Update(1f, new SkirmishInput { HasTarget = false }), "강제 슬립 요청이 소비됨");
        }

        //# ===== AutoCombatAI 습격 구성 =====

        [Test]
        public void 습격_미구성_AI는_소프트_반경_0이고_RequestSlip은_무동작이다()
        {
            AutoCombatAI ai = Spawn("Knight", Vector3.zero).AddComponent<AutoCombatAI>();

            Assert.AreEqual(0f, ai.SkirmishArenaSoftRadius);
            Assert.IsNull(GetPrivate(ai, "_skirmish"));
            Assert.DoesNotThrow(() => ai.RequestSlip());
        }

        [Test]
        public void ConfigureSkirmish는_소프트_반경을_노출하고_ClampToArena가_그_반경을_쓴다()
        {
            AutoCombatAI ai = Spawn("Assassin", Vector3.zero).AddComponent<AutoCombatAI>();

            ai.ConfigureSkirmish(new SkirmishSettings(9f, 1.5f, 2.8f, 3, 2f, 3.5f, 0.2f, 0.2f, 5f, 0.4f, 7f));

            Assert.AreEqual(7f, ai.SkirmishArenaSoftRadius, 0.0001f);
            Assert.AreEqual(7f, ai.ClampToArena(new Vector3(10f, 0f, 0f)).x, 0.001f);
            Assert.AreEqual(new Vector3(3f, 0f, 3f), ai.ClampToArena(new Vector3(3f, 0f, 3f)));
        }

        [Test]
        public void 카이팅만_구성한_AI의_ClampToArena는_카이팅_반경을_쓰고_둘_다_없으면_제한이_없다()
        {
            AutoCombatAI kiter = Spawn("Archer", Vector3.zero).AddComponent<AutoCombatAI>();
            kiter.ConfigureKite(new KiteSettings(3f, 4.5f, 4f, 6f, 0.5f, 0.3f, 0.4f));
            AutoCombatAI plain = Spawn("Knight", Vector3.zero).AddComponent<AutoCombatAI>();

            Assert.AreEqual(6f, kiter.ClampToArena(new Vector3(10f, 0f, 0f)).x, 0.001f);
            Assert.AreEqual(10f, plain.ClampToArena(new Vector3(10f, 0f, 0f)).x, 0.001f);
        }

        [Test]
        public void 풀_재사용_OnEnable은_습격_상태와_표적을_초기화한다()
        {
            AutoCombatAI ai = Spawn("PooledAssassin", Vector3.zero).AddComponent<AutoCombatAI>();
            ai.ConfigureSkirmish(new SkirmishSettings(9f, 1.5f, 2.8f, 3, 2f, 3.5f, 0.2f, 0.2f, 5f, 0.4f, 7f));
            SkirmishBehavior behavior = (SkirmishBehavior)GetPrivate(ai, "_skirmish");
            behavior.Update(0f, new SkirmishInput { HasTarget = true, TargetDist = 1f, Range = 1.6f, EngageBuffer = 0.5f });
            behavior.RequestForcedSlip();
            Assert.IsTrue(behavior.Update(0.25f, new SkirmishInput { HasTarget = true, TargetDist = 1f, Range = 1.6f }), "전제: 슬립 상태");
            SetPrivate(ai, "_skirmishTarget", ai.transform);
            ai.BeginLeap(Vector3.one, 0.2f);

            InvokeNonPublic(ai, "OnEnable");

            Assert.AreEqual(ESkirmishState.Chase, behavior.State);
            Assert.IsNull(GetPrivate(ai, "_skirmishTarget"));
            Assert.IsFalse(ai.IsLeaping);
        }

        //# ===== 로드아웃 매핑 / 에디터 동기 =====

        private static EData LoadoutKeyOf(EHero hero)
        {
            MethodInfo mi = typeof(BattleController).GetMethod("LoadoutKeyOf", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "BattleController.LoadoutKeyOf 존재 - 시그니처 변경 감지");
            return (EData)mi.Invoke(null, new object[] { hero });
        }

        [Test]
        public void 영웅별_스킬_로드아웃_키는_영웅_이름을_따른다()
        {
            Assert.AreEqual(EData.HeroSkillLoadout, LoadoutKeyOf(EHero.Knight));
            Assert.AreEqual(EData.HeroSkillLoadout_Archer, LoadoutKeyOf(EHero.Archer));
            Assert.AreEqual(EData.HeroSkillLoadout_Assassin, LoadoutKeyOf(EHero.Assassin));
        }

        [Test]
        public void 매핑에_없는_영웅은_기사_로드아웃으로_폴백한다()
        {
            Assert.AreEqual(EData.HeroSkillLoadout, LoadoutKeyOf((EHero)99));
        }

        [Test]
        public void 로드아웃_키_값명은_asset_파일명과_Addressables_주소가_일치한다()
        {
            string groups = File.ReadAllText("Assets/AddressableAssetsData/AssetGroups/Resource.asset");

            foreach (EHero hero in new[] { EHero.Archer, EHero.Assassin })
            {
                string key = LoadoutKeyOf(hero).ToString();
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>($"Assets/_Lair/Art/Skills/{key}.asset"), key);
                StringAssert.Contains($"m_Address: {key}", groups);
            }
        }

        [Test]
        public void JsonSync의_영웅별_로드아웃_경로에_암살자가_포함되고_모든_경로의_asset이_존재한다()
        {
            FieldInfo field = typeof(HeroSkillSyncer).GetField("LoadoutPaths", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "HeroSkillSyncer.LoadoutPaths 존재 - 시그니처 변경 감지");
            KeyValuePair<string, string>[] paths = (KeyValuePair<string, string>[])field.GetValue(null);
            Dictionary<string, string> byHero = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> pair in paths)
            {
                byHero[pair.Key] = pair.Value;
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(pair.Value), pair.Value);
            }

            Assert.AreEqual("Assets/_Lair/Art/Skills/HeroSkillLoadout_Assassin.asset", byHero["Assassin"]);
            Assert.AreEqual(3, byHero.Count);
            Assert.IsTrue(byHero.ContainsKey("Knight"));
            Assert.IsTrue(byHero.ContainsKey("Archer"));
        }

        //# ===== 기사 / 궁수 경로 불변 =====

        [TestCase("Knight")]
        [TestCase("Archer")]
        public void 기사_궁수_프리팹의_AutoCombatAI는_습격_미구성이다(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Lair/Art/Characters/{prefabName}.prefab");
            Assert.IsNotNull(prefab, prefabName);
            AutoCombatAI ai = prefab.GetComponent<AutoCombatAI>();
            Assert.IsNotNull(ai);

            Assert.IsNull(GetPrivate(ai, "_skirmish"), "Skirmish 미설정이면 기존 경로");
            Assert.AreEqual(0f, ai.SkirmishArenaSoftRadius);
            Assert.IsFalse(ai.IsLeaping);
        }

        [Test]
        public void 궁수_AI는_습격을_구성하지_않아도_카이팅_구성은_그대로_동작한다()
        {
            AutoCombatAI ai = Spawn("ArcherKite", Vector3.zero).AddComponent<AutoCombatAI>();

            ai.ConfigureKite(new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f));

            Assert.AreEqual(7f, ai.KiteArenaSoftRadius, 0.0001f);
            Assert.AreEqual(0f, ai.SkirmishArenaSoftRadius);
            Assert.IsNull(GetPrivate(ai, "_skirmish"));
            Assert.IsNotNull(GetPrivate(ai, "_kite"));
        }
    }
}
