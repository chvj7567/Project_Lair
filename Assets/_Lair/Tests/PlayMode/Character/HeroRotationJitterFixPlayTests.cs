using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# 영웅 회전 떨림 수정 — 본격 통합/회귀 스위트(PlayMode). EditMode 로 못 잡는 시간/Update 의존 케이스 전담.
    //# 다룸:
    //#  - 수용기준 4 : 히스테리시스 OFF(몬스터) → 매 프레임 최근접(불변) — 실제 Update 경로.
    //#  - 수용기준 5(시간) : 영웅 AttackAligned 즉시 스냅 후 다음 프레임 보간 정상 재개(_snappedThisFrame 1프레임만 skip).
    //#  - 수용기준 7 : 히스테리시스 ON 상태에서 도주(FleeMode)/중앙끌림 브랜치가 ResolveStickyTarget 이 고정한 t 를 쓴다
    //#                (= 캐싱 타겟이 그 브랜치 진입 중에도 깜빡이지 않는다).
    //# 영웅 AutoCombatAI 는 DeferStrike·AttackGate·SpawnGate 가 없는 합성 구성 — 회전/타겟 캐싱만 검증(밸런스 무관).
    //# CharacterRegistry 는 static — SetUp/TearDown 에서 양 리스트 + timeScale 격리(AutoCombatAIRotationTests 패턴).
    public class HeroRotationJitterFixPlayTests
    {
        private const float YawTolerance = 2.0f;
        private const float HighSpeed = 1080f;

        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        private static float YawDelta(Quaternion rot, float expectedYaw)
            => Quaternion.Angle(rot, Quaternion.Euler(0f, expectedYaw, 0f));

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{name} 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            f.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{name} 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            return (T)f.GetValue(target);
        }

        //# 영웅 — HeroTargetProvider(Monsters 검색) + 회전 히스테리시스 ON 으로 합성. 위치는 테스트가 제어.
        private GameObject SpawnHero(Vector3 pos, bool hysteresis, bool snapInstant, float margin = 1.5f)
        {
            GameObject hero = new GameObject("hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);
            hero.AddComponent<SimpleMover>().Speed = 0.001f;   //# 사실상 정지 — 위치 안정.
            hero.AddComponent<MeleeAttacker>().Configure(1.5f, 1.0f, 1);
            SimpleRotator rot = hero.AddComponent<SimpleRotator>();
            rot.TurnSpeedDegPerSec = HighSpeed;
            SetField(rot, "_snapInstant", snapInstant);
            hero.AddComponent<HeroTargetProvider>();
            AutoCombatAI ai = hero.AddComponent<AutoCombatAI>();
            SetField(ai, "_targetHysteresisEnabled", hysteresis);
            SetField(ai, "_retargetMargin", margin);
            //# 스폰 게이트 보험 — 합성 영웅의 DeferStrike 가 true 면 게이트가 fallback(1.8s) 까지 닫혀
            //# Update 가 ResolveStickyTarget 전에 bail 한다. 짧은 대기 테스트가 그 전에 끝나므로 미리 open.
            //# DeferStrike=false(현재 기본)면 무해한 no-op.
            ai.OpenSpawnGate();
            _spawned.Add(hero);
            return hero;
        }

        //# 몬스터 — Monsters 레지스트리 등록(영웅 검색 대상). Engaging=true 로 후보 자격 부여.
        private GameObject SpawnMonster(Vector3 pos, bool engaging = true)
        {
            GameObject m = new GameObject("monster");
            m.transform.position = pos;
            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            CharacterRegistry.RegisterMonster(m.transform, hp);
            CharacterRegistry.SetMonsterEngaging(m.transform, engaging);
            _spawned.Add(m);
            return m;
        }

        //# 몬스터 — 자기 AutoCombatAI 까지 풀스택(수용기준 4 의 OFF 경로 회귀용).
        private GameObject SpawnMonsterAI(Vector3 pos, float range = 1.5f)
        {
            GameObject m = new GameObject("monster_ai");
            m.transform.position = pos;
            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            m.AddComponent<SimpleMover>().Speed = 0.001f;
            m.AddComponent<MeleeAttacker>().Configure(range, 1.0f, 1);
            m.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = HighSpeed;
            m.AddComponent<MonsterTargetProvider>();
            m.AddComponent<AutoCombatAI>();
            //# _targetHysteresisEnabled 기본 false — 몬스터 OFF 경로.
            _spawned.Add(m);
            return m;
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        //# ===== 수용기준 4 — 히스테리시스 OFF(몬스터)는 매 프레임 최근접을 따른다(불변) =====

        //# 몬스터(OFF)는 더 가까운 적이 나타나면 margin 무관 즉시 그쪽을 본다(영웅과 정반대 — 회귀 가드).
        [UnityTest]
        public IEnumerator 몬스터_히스테리시스OFF는_더가까운적으로_즉시_재조준한다()
        {
            //# 몬스터 AI 가 +Z 5 의 영웅 A 를 먼저 추적/조준(Moving — range 밖이라 Move, 회전은 +Z 0°).
            GameObject monster = SpawnMonsterAI(Vector3.zero);
            GameObject heroA = new GameObject("heroA");
            heroA.transform.position = new Vector3(0f, 0f, 5f);
            Health ha = heroA.AddComponent<Health>();
            ha.SetMax(100000);
            CharacterRegistry.RegisterHero(heroA.transform, ha);
            _spawned.Add(heroA);

            yield return WaitSeconds(0.6f);
            Assert.Less(YawDelta(monster.transform.rotation, 0f), YawTolerance,
                "사전: 몬스터가 +Z(0°) 영웅 A 를 조준");

            //# 더 가까운 영웅 B 를 +X 2 에 배치 — OFF 면 margin 무관 즉시 B(+X 90°)로 재조준.
            GameObject heroB = new GameObject("heroB");
            heroB.transform.position = new Vector3(2f, 0f, 0f);
            Health hb = heroB.AddComponent<Health>();
            hb.SetMax(100000);
            CharacterRegistry.RegisterHero(heroB.transform, hb);
            _spawned.Add(heroB);

            yield return WaitSeconds(0.6f);
            Assert.Less(YawDelta(monster.transform.rotation, 90f), YawTolerance,
                "몬스터(OFF) — 더 가까운 B(+X 90°)로 매 프레임 최근접 재조준(히스테리시스 없음, 동작 불변)");
        }

        //# 대조 — 같은 배치에서 영웅(ON)은 margin 미만이면 A 를 유지(OFF 와의 차이 박제).
        [UnityTest]
        public IEnumerator 영웅_히스테리시스ON은_margin미만_신규근접에_고정유지한다()
        {
            //# 영웅 origin, 몬스터 A 를 +Z 3 에 — 추적/조준(0°).
            GameObject hero = SpawnHero(Vector3.zero, hysteresis: true, snapInstant: true);
            SpawnMonster(new Vector3(0f, 0f, 3f));

            yield return WaitSeconds(0.6f);
            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            Transform stuck = GetField<Transform>(ai, "_currentTarget");
            Assert.IsNotNull(stuck, "사전: 영웅이 A 고정");

            //# B 를 +X 2.0 에 — distA=3, distB=2 → diff=1.0 < margin 1.5 → 유지.
            SpawnMonster(new Vector3(2f, 0f, 0f));
            yield return WaitSeconds(0.6f);

            Assert.AreSame(stuck, GetField<Transform>(ai, "_currentTarget"),
                "영웅(ON) — 신규 근접(diff 1.0 < margin 1.5)에 재조준하지 않고 A 고정 유지");
        }

        //# ===== 수용기준 5(시간) — 영웅 즉시 스냅이 후속 회전을 영구 잠그지 않는다 =====

        //# 교전 진입(AttackAligned 즉시 스냅) → 타겟을 옆으로 옮겨도 매 프레임 스냅으로 새 방향에 재정렬된다.
        //# (스냅-후-1프레임-skip 이 _snappedThisFrame 에 stuck-true 로 남으면 회전이 멎는데, 그 set/clear 자체는
        //#  EditMode(영웅_AttackAligned는_snappedThisFrame를_세운다 / 영웅_Smooth는_세우지않는다)가 박는다.
        //#  여기선 통합 경로에서 회전이 영구히 멈추지 않음을 검증.)
        [UnityTest]
        public IEnumerator 영웅_교전스냅후_타겟이동시_새방향에_재정렬된다()
        {
            //# 영웅 origin, 몬스터 +X 1(range 1.5 안 → 교전 AttackAligned 즉시 스냅 90°).
            GameObject hero = SpawnHero(Vector3.zero, hysteresis: true, snapInstant: true);
            GameObject mon = SpawnMonster(new Vector3(1f, 0f, 0f));

            yield return WaitSeconds(0.3f);
            Assert.Less(YawDelta(hero.transform.rotation, 90f), YawTolerance,
                "교전 — AttackAligned 즉시 스냅으로 +X(90°) 정렬");

            //# 타겟을 +Z 1 로 옮김(여전히 사거리 안 → 교전 유지). 회전이 멎지 않았다면 0° 로 재정렬돼야.
            mon.transform.position = new Vector3(0f, 0f, 1f);
            yield return WaitSeconds(0.5f);

            Assert.Less(YawDelta(hero.transform.rotation, 0f), YawTolerance,
                "타겟 이동 후 +Z(0°)로 재정렬 — 스냅이 후속 회전을 영구 잠그지 않음");
        }

        //# ===== 수용기준 7 — 히스테리시스 ON 상태에서 FleeMode 브랜치가 캐싱 타겟 t 를 쓴다 =====

        //# 수용기준 7 — FleeMode 가 활성이어도 ResolveStickyTarget 이 flee 브랜치보다 먼저 실행돼 _currentTarget 을 채운다.
        //# 즉 도주 브랜치가 매 프레임 '히스테리시스가 결정한 t' 를 쓰며, 그 t 가 프레임 간 불변(고정 유지)임을 검증.
        //# 주의(범위) — 두 후보는 정확히 등거리(dist 5)라 TryFindNearest 의 strict `d < best` 가 항상 먼저 등록된 A 를 고른다.
        //#   따라서 candidate 자체는 교차하지 않는다(깜빡임을 '강제'하진 않음). 본 테스트가 박는 것은
        //#   "flee 브랜치 진입 중에도 캐싱 타겟이 살아있고 불변" 이라는 통합 사실. candidate 교차 자체는 EditMode 1번이 담당.
        [UnityTest]
        public IEnumerator FleeMode중에도_히스테리시스_캐싱타겟이_고정유지된다()
        {
            GameObject hero = SpawnHero(Vector3.zero, hysteresis: true, snapInstant: true);
            //# 등거리 두 몬스터(둘 다 dist 5) — A(+X), B(+Z).
            SpawnMonster(new Vector3(5f, 0f, 0f));
            SpawnMonster(new Vector3(0f, 0f, 5f));

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.FleeMode = true;

            //# 첫 프레임 — 둘 중 하나로 고정.
            yield return null;
            yield return null;
            Transform stuck = GetField<Transform>(ai, "_currentTarget");
            Assert.IsNotNull(stuck,
                "FleeMode 중에도 히스테리시스가 타겟을 고정(ResolveStickyTarget 이 flee 브랜치 전에 실행)");

            //# 여러 프레임 도주 지속 — 캐싱 타겟이 flee 브랜치 동작 내내 불변(살아있고 안 바뀜)임을 확인.
            for (int i = 0; i < 20; ++i)
                yield return null;

            Assert.AreSame(stuck, GetField<Transform>(ai, "_currentTarget"),
                "FleeMode 지속 중 캐싱 타겟 불변 — 도주 브랜치가 고정 t 를 일관 사용");

            //# 도주 중이므로 공격/교전이 아니라 이동(도주) 상태여야 — 브랜치 진입 확인.
            Assert.IsTrue(hero.GetComponent<SimpleMover>().IsMoving,
                "FleeMode → 도주 이동(Move) 브랜치 진입 확인");
        }

        //# ===== 수용기준 6(통합) — 풀 재사용 후 영웅 캐싱 타겟 잔존 없음(SetActive 사이클) =====

        [UnityTest]
        public IEnumerator 풀재사용_SetActive사이클후_캐싱타겟이_잔존하지않는다()
        {
            GameObject hero = SpawnHero(Vector3.zero, hysteresis: true, snapInstant: true);
            SpawnMonster(new Vector3(2f, 0f, 0f));
            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();

            yield return WaitSeconds(0.3f);
            Assert.IsNotNull(GetField<Transform>(ai, "_currentTarget"), "사전: 타겟 고정 성립");

            //# 풀 Push/Pop 모사 — OnDisable 이 provider 를 레지스트리에서 빼고, OnEnable 이 _currentTarget 리셋.
            hero.SetActive(false);
            yield return null;
            hero.SetActive(true);
            //# OnEnable 직후(첫 Update 전) 잔존 검증 — 다음 프레임이 오기 전에 읽는다.
            Assert.IsNull(GetField<Transform>(ai, "_currentTarget"),
                "재활성 직후 _currentTarget 잔존 없음(OnEnable 리셋)");
            Assert.IsNull(GetField<IHealth>(ai, "_currentTargetHealth"),
                "재활성 직후 _currentTargetHealth 잔존 없음");

            yield return null;   //# 한 프레임 진행 — 깨끗한 상태에서 새로 고정되는지(크래시 없이 동작).
        }
    }
}
