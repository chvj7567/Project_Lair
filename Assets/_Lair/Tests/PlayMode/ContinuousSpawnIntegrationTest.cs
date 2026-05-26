using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 지속 스폰 — PlayMode 통합. MonoBehaviour/CharacterRegistry/씬 의존 영역 검증.
    //# 1) ApplyMonsterStats — raw×배율 + resetCurrent 분기 (실제 Health/MeleeAttacker 컴포넌트).
    //# 2) RegisterMonsterTypeBuff — 글로벌 dict 곱연산 + 필드 동일 종 소급 (CharacterRegistry 경유).
    //# 3) 필드 몬스터 캡 15 — Battle 씬 로드 후 라이브 BattleController 로 절대값 검증.
    public class ContinuousSpawnIntegrationTest
    {
        private readonly List<GameObject> _spawned = new();
        private BalanceConfig _balance;

        [SetUp]
        public void SetUp()
        {
            //# CharacterRegistry 는 정적 — 테스트 간 잔존 방지 위해 비운다.
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
            //# 런타임 BalanceConfig SO — 위스프 raw HP 200 / Power 10 등 (§6.2 스타터 수치).
            _balance = ScriptableObject.CreateInstance<BalanceConfig>();
            SetPrivate(_balance, "_monsters", new[]
            {
                MakeRow(EMonster.Wisp,  hp: 200, power: 10, range: 1.5f, cooldown: 1.0f, move: 2.0f),
                MakeRow(EMonster.Wraith,  hp: 500, power: 20, range: 1.5f, cooldown: 2.0f, move: 1.0f),
                MakeRow(EMonster.Plague, hp: 40,  power: 5,  range: 1.2f, cooldown: 1.2f, move: 3.0f),
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.Destroy(go);
            _spawned.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
            if (_balance != null) Object.Destroy(_balance);
            //# 캡15 테스트가 timeScale 가속을 쓰므로 원복 — 후속 테스트 영향 차단.
            Time.timeScale = 1f;
        }

        //# ===== 유틸 =====

        private static BalanceConfig.MonsterStatRow MakeRow(
            EMonster key, int hp, int power, float range, float cooldown, float move)
        {
            return new BalanceConfig.MonsterStatRow
            {
                Key = key,
                Stat = new BalanceConfig.CharacterStat
                {
                    Hp = hp, Power = power, Range = range, Cooldown = cooldown, MoveSpeed = move
                }
            };
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        private static T GetPrivate<T>(object target, string field)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            return (T)fi.GetValue(target);
        }

        //# 비활성 BattleController — Start(async void) 미실행. _balance 만 주입해 순수 메서드 검증.
        private BattleController CreateIsolatedController()
        {
            var go = new GameObject("BattleControllerUT");
            go.SetActive(false);   //# Start 가 안 돌도록 비활성 생성.
            _spawned.Add(go);
            var bc = go.AddComponent<BattleController>();
            SetPrivate(bc, "_balance", _balance);
            return bc;
        }

        //# 몬스터 GameObject — Health / MeleeAttacker / MonsterTag 부착.
        //# 비활성 생성 후 반환 — 호출자가 필요 시 SetActive(true).
        private GameObject CreateMonster(EMonster key)
        {
            var go = new GameObject($"Monster_{key}");
            go.SetActive(false);
            _spawned.Add(go);
            go.AddComponent<Health>();
            go.AddComponent<MeleeAttacker>();
            go.AddComponent<MonsterTag>().Configure(key);
            return go;
        }

        //# ===== 1. ApplyMonsterStats — raw×배율 + resetCurrent =====

        //# 정상 — 모디파이어 없을 때 raw 스탯 그대로 적용 (배율 전부 1.0).
        [UnityTest]
        public IEnumerator ApplyMonsterStats_모디파이어_없으면_raw_스탯_그대로()
        {
            var bc = CreateIsolatedController();
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);

            var hp = mon.GetComponent<Health>();
            var atk = mon.GetComponent<MeleeAttacker>();
            Assert.AreEqual(200, hp.Max, "raw HP 200 그대로");
            Assert.AreEqual(200, hp.Current, "resetCurrent:true — 풀피");
            Assert.AreEqual(10, atk.Power, "raw Power 10 그대로");
            Assert.AreEqual(1.0f, atk.Cooldown, 0.0001f, "raw Cooldown 그대로");
        }

        //# 정상 — 강화 픽 1회 후 신규 Pop: raw×배율 적용 (HP 200×1.5=300).
        [UnityTest]
        public IEnumerator ApplyMonsterStats_강화_1픽후_신규Pop_raw곱배율()
        {
            var bc = CreateIsolatedController();
            //# dict 에 위스프 HP ×1.5 등록.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);

            var hp = mon.GetComponent<Health>();
            Assert.AreEqual(300, hp.Max, "raw 200 × HpMul 1.5 = 300");
            Assert.AreEqual(300, hp.Current, "신규 Pop — 풀피 300");
        }

        //# 회귀 — 강화 픽 2회 곱연산: HP ×1.5×1.5 = ×2.25 → 200×2.25 = 450.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_강화_2픽_곱연산_누적()
        {
            var bc = CreateIsolatedController();
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);

            Assert.AreEqual(450, mon.GetComponent<Health>().Max, "200 × 1.5 × 1.5 = 450 (곱연산)");
        }

        //# 회귀 (고가치) — 강화 카드 필드 소급은 현재 HP 보존, 최대치만 상향 (resetCurrent:false).
        //# 강화 픽이 풀피 회복을 주는 부조리 방지 (§7.5.3). RegisterMonsterTypeBuff 소급 경로 검증.
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_소급_현재HP_보존_최대치만_상향()
        {
            var bc = CreateIsolatedController();
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            //# 먼저 raw 적용 후 데미지 — Current 50/200 상태로 만든다.
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            var hp = mon.GetComponent<Health>();
            hp.TakeDamage(150);
            Assert.AreEqual(50, hp.Current, "선조건 — Current 50/200");

            //# 필드 소급 대상이 되려면 레지스트리 등록 필요 (RegisterMonsterTypeBuff 가 순회).
            CharacterRegistry.RegisterMonster(mon.transform, hp);

            //# 강화 카드 픽 — dict 갱신 + 필드 동일 종 소급(내부에서 resetCurrent:false 호출).
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(300, hp.Max, "최대치는 200×1.5=300 으로 상향");
            Assert.AreEqual(50, hp.Current,
                "현재 HP 50 보존 (소급은 resetCurrent:false — 강화 픽이 풀피 회복을 주지 않음)");
        }

        //# 정상 — 신규 Pop(resetCurrent:true) 은 현재 HP 를 새 최대치로 채운다.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_resetCurrent_true_현재HP_최대로()
        {
            var bc = CreateIsolatedController();
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            var hp = mon.GetComponent<Health>();
            hp.TakeDamage(100);
            Assert.AreEqual(100, hp.Current, "선조건 — Current 100/200");

            //# 풀 재사용 신규 Pop 시뮬 — resetCurrent:true 재적용.
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Assert.AreEqual(200, hp.Current, "resetCurrent:true — 현재 HP 가 최대치로 복원");
        }

        //# 엣지 — 플레이그 SlowFactor: ApplyMonsterStats 가 BaseSlowFactor 0.8 × 배율 적용.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_플레이그_SlowFactor_baseline_곱배율()
        {
            var bc = CreateIsolatedController();
            var plague = CreateMonster(EMonster.Plague);
            plague.AddComponent<PlagueSlowOnHit>();
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, 0.75f);
            plague.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(plague, EMonster.Plague, resetCurrent: true);

            //# _slowFactor 는 private — 리플렉션으로 확인. BaseSlowFactor 0.8 × 0.75 = 0.6.
            var slow = plague.GetComponent<PlagueSlowOnHit>();
            float applied = GetPrivate<float>(slow, "_slowFactor");
            Assert.AreEqual(0.6f, applied, 0.0001f, "플레이그 둔화 = BaseSlowFactor 0.8 × 0.75");
        }

        //# 회귀 — 플레이그 둔화는 풀 재사용(반복 ApplyMonsterStats) 시 복리 누적되지 않는다.
        //# baseline 이 const 0.8 이라 매 Pop 항상 0.8 부터 — §7.5.2 복리 버그 차단 검증.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_플레이그_반복적용_복리누적_없음()
        {
            var bc = CreateIsolatedController();
            var plague = CreateMonster(EMonster.Plague);
            plague.AddComponent<PlagueSlowOnHit>();
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, 0.75f);
            plague.SetActive(true);
            yield return null;

            var slow = plague.GetComponent<PlagueSlowOnHit>();

            //# 같은 dict 상태로 3번 재적용 — 풀 재사용 Pop 반복 시뮬.
            bc.ApplyMonsterStats(plague, EMonster.Plague, resetCurrent: true);
            bc.ApplyMonsterStats(plague, EMonster.Plague, resetCurrent: true);
            bc.ApplyMonsterStats(plague, EMonster.Plague, resetCurrent: true);

            float applied = GetPrivate<float>(slow, "_slowFactor");
            Assert.AreEqual(0.6f, applied, 0.0001f,
                "3회 재적용해도 0.6 — baseline const 라 복리(0.6→0.45→...) 누적 없음");
        }

        //# ===== 2. RegisterMonsterTypeBuff — 필드 동일 종 소급 =====

        //# 정상 — 강화 픽 시 필드의 동일 종 살아있는 몬스터에 소급 적용.
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_필드_동일종_소급적용()
        {
            var bc = CreateIsolatedController();
            //# 필드에 위스프 몬스터 — CharacterRegistry 에 등록 (Health.OnEnable 의 자기등록은
            //# 없으므로 명시 등록 — 실제로는 캐릭터가 자기 등록하지만 본 테스트는 합성).
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            var hp = mon.GetComponent<Health>();
            CharacterRegistry.RegisterMonster(mon.transform, hp);

            //# 강화 카드 픽 — dict 갱신 + 필드 소급.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(300, hp.Max, "필드 위스프에 소급 — 최대치 200×1.5=300");
        }

        //# 회귀 — 강화 소급은 다른 종 몬스터를 건드리지 않는다 (종별 격리).
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_다른종_몬스터는_불변()
        {
            var bc = CreateIsolatedController();
            var wisp = CreateMonster(EMonster.Wisp);
            var wraith = CreateMonster(EMonster.Wraith);
            wisp.SetActive(true);
            wraith.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(wisp, EMonster.Wisp, resetCurrent: true);
            bc.ApplyMonsterStats(wraith, EMonster.Wraith, resetCurrent: true);
            CharacterRegistry.RegisterMonster(wisp.transform, wisp.GetComponent<Health>());
            CharacterRegistry.RegisterMonster(wraith.transform, wraith.GetComponent<Health>());

            //# 위스프 강화 — 레이스은 영향 없어야.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(300, wisp.GetComponent<Health>().Max, "위스프 — 소급 적용 300");
            Assert.AreEqual(500, wraith.GetComponent<Health>().Max, "레이스 — 종 불일치, raw 500 불변");
        }

        //# 회귀 — 죽은 몬스터에는 소급하지 않는다 (IsAlive 필터).
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_죽은_몬스터는_소급제외()
        {
            var bc = CreateIsolatedController();
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            var hp = mon.GetComponent<Health>();
            CharacterRegistry.RegisterMonster(mon.transform, hp);

            //# 몬스터 사망.
            hp.TakeDamage(hp.Current);
            Assert.IsFalse(hp.IsAlive, "선조건 — 몬스터 사망");

            //# 강화 픽 — 죽은 몬스터엔 소급 안 됨.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(200, hp.Max, "죽은 몬스터 — 소급 제외, Max 200 불변");
        }

        //# 엣지 — RegisterMonsterTypeBuff 는 빈 필드(몬스터 0)에서도 예외 없이 dict 만 갱신.
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_필드_몬스터_0개_예외없음()
        {
            var bc = CreateIsolatedController();

            Assert.DoesNotThrow(() =>
                bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f),
                "필드 몬스터 0개 — dict 갱신만, 예외 없음");

            //# 이후 신규 Pop 은 갱신된 dict 반영.
            var mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Assert.AreEqual(300, mon.GetComponent<Health>().Max,
                "필드 0개여도 dict 갱신됨 — 이후 신규 위스프 300");
        }

        //# ===== 3. 필드 몬스터 캡 15 — Battle 씬 통합 =====

        //# 통합 — Battle 씬을 진행해도 살아있는 몬스터가 캡 15 를 절대 넘지 않는다.
        //# Spawner 자연 스폰(사이클 skip) + 증식 경로 모두 합쳐도 절대값 유지 (§4.2).
        //# 카드 팝업은 DebugAutoPicker 로 즉시 처리해 hang 방지(Time.timeScale=0 Pause 회피).
        [UnityTest]
        public IEnumerator Battle씬_지속스폰_살아있는_몬스터_캡15_절대초과없음()
        {
            //# 정적 레지스트리 정리 — 본 테스트 전 상태 격리.
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();

            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 가 씬에 잡힐 때까지 unscaledDeltaTime 으로 대기 (timeScale 무관).
            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null) break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 가 씬에 존재해야 함");

#if UNITY_EDITOR
            //# 게임 진행 중 HP%/30s 트리거로 팝업이 뜨면 PauseService 가 timeScale=0 → hang.
            //# DebugAutoPicker 로 첫 장 픽해 팝업 우회. BalanceSimulationTest 와 동일 훅 사용.
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# 비동기 Start 완료(영웅 스폰) 대기 — unscaledDeltaTime 으로 timeScale 영향 차단.
            float elapsed = 0f;
            while (elapsed < 4f && CharacterRegistry.Heroes.Count == 0)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "4초 후 영웅 미스폰 — BattleController 비동기 초기화 미완 (Addressables/씬 확인)");

            //# 게임 시간 70초 진행 — BalanceSimulationTest 패턴으로 가속(5x → 벽시계 ~14s).
            Time.timeScale = 5f;

            int maxObserved = 0;
            float wallElapsed = 0f;
            //# 벽시계 fail-safe: 70초 / 5x = 14초 게임시간 + 초기화 여유 → 25초 안에 완료해야 함.
            const float WallTimeLimit = 25f;
            //# 게임 시간 70초가 누적되거나, 영웅이 모두 죽으면 종료.
            float gameTimeAccum = 0f;
            while (wallElapsed < WallTimeLimit && gameTimeAccum < 70f)
            {
                int alive = 0;
                foreach (var e in CharacterRegistry.Monsters)
                    if (e?.Health != null && e.Health.IsAlive) alive++;
                if (alive > maxObserved) maxObserved = alive;
                //# 캡 절대값 — 매 프레임 검사. 한 번이라도 초과하면 실패.
                Assert.LessOrEqual(alive, 15,
                    $"살아있는 몬스터 {alive} — 캡 15 초과 금지 (게임 t={gameTimeAccum:F1}s)");

                //# 영웅 전멸이면 더 이상 스폰 안 됨 — 조기 종료.
                if (AllHeroesDead()) break;

                gameTimeAccum += Time.deltaTime;  //# 게임 시간 (timeScale 반영)
                wallElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            //# 가속 원복.
            Time.timeScale = 1f;

            //# 캡이 실제로 작동했는지 — 적어도 몇 마리는 스폰됐어야 (Spawner 가 도는지 sanity).
            Assert.Greater(maxObserved, 0,
                "한 마리도 안 나옴 — Spawner 가 구동되지 않음 (씬 구성 확인)");

            yield return null;
        }

        //# 영웅 전멸이면 스폰이 멈춤 — 캡15 테스트 조기 종료 조건.
        private static bool AllHeroesDead()
        {
            var heroes = CharacterRegistry.Heroes;
            if (heroes.Count == 0) return false;
            foreach (var e in heroes)
                if (e?.Health != null && e.Health.IsAlive) return false;
            return true;
        }

        //# 통합 — Battle 씬에 §5.3 스타터 프리셋대로 6개 Spawner 가 BattleController 에 배선돼 있다.
        //# _spawners 는 직렬화 필드 — Awake 시점에 이미 배선됨. 비동기 Start 완료를 기다릴 필요 없음.
        [UnityTest]
        public IEnumerator Battle씬_BattleController에_Spawner_6개_배선()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 등장만 대기 (1~2 프레임). 카드 팝업 hang 우회 — unscaledDeltaTime.
            BattleController bc = null;
            float wait = 0f;
            while (wait < 2f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null) break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "씬에 BattleController 존재");

            var spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            Assert.IsNotNull(spawners, "_spawners 배열 배선 확인");
            Assert.AreEqual(6, spawners.Length, "스타터 프리셋 — Spawner 6개 (§5.3)");
            foreach (var sp in spawners)
                Assert.IsNotNull(sp, "Spawner 슬롯 누락 없음");

            yield return null;
        }
    }
}
