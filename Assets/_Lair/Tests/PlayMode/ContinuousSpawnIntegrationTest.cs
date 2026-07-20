using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 지속 스폰 — PlayMode 통합. MonoBehaviour/CharacterRegistry/씬 의존 영역 검증.
    //# 1) ApplyMonsterStats — raw×배율 + resetCurrent 분기 (실제 Health/MeleeAttacker 컴포넌트).
    public class ContinuousSpawnIntegrationTest : BattlePlayTestBase
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
            foreach (GameObject go in _spawned)
                if (go != null) Object.Destroy(go);
            _spawned.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
            if (_balance != null)
            {
                Object.Destroy(_balance);
            }
            //# 캡18 테스트가 timeScale 가속을 쓰므로 원복 — 후속 테스트 영향 차단.
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
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        private static T GetPrivate<T>(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            return (T)fi.GetValue(target);
        }

        //# 비활성 BattleController — Start(async void) 미실행. _balance 만 주입해 순수 메서드 검증.
        private BattleController CreateIsolatedController()
        {
            GameObject go = new GameObject("BattleControllerUT");
            go.SetActive(false);   //# Start 가 안 돌도록 비활성 생성.
            _spawned.Add(go);
            BattleController bc = go.AddComponent<BattleController>();
            SetPrivate(bc, "_balance", _balance);
            return bc;
        }

        //# 몬스터 GameObject — Health / MeleeAttacker / MonsterTag 부착.
        //# 비활성 생성 후 반환 — 호출자가 필요 시 SetActive(true).
        private GameObject CreateMonster(EMonster key)
        {
            GameObject go = new GameObject($"Monster_{key}");
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
            BattleController bc = CreateIsolatedController();
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);

            Health hp = mon.GetComponent<Health>();
            MeleeAttacker atk = mon.GetComponent<MeleeAttacker>();
            Assert.AreEqual(200, hp.Max, "raw HP 200 그대로");
            Assert.AreEqual(200, hp.Current, "resetCurrent:true — 풀피");
            Assert.AreEqual(10, atk.Power, "raw Power 10 그대로");
            Assert.AreEqual(1.0f, atk.Cooldown, 0.0001f, "raw Cooldown 그대로");
        }

        //# 정상 — 강화 픽 1회 후 신규 Pop: raw×배율 적용 (HP 200×1.5=300).
        [UnityTest]
        public IEnumerator ApplyMonsterStats_강화_1픽후_신규Pop_raw곱배율()
        {
            BattleController bc = CreateIsolatedController();
            //# dict 에 위스프 HP ×1.5 등록.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);

            Health hp = mon.GetComponent<Health>();
            Assert.AreEqual(300, hp.Max, "raw 200 × HpMul 1.5 = 300");
            Assert.AreEqual(300, hp.Current, "신규 Pop — 풀피 300");
        }

        //# 회귀 — 강화 픽 2회 곱연산: HP ×1.5×1.5 = ×2.25 → 200×2.25 = 450.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_강화_2픽_곱연산_누적()
        {
            BattleController bc = CreateIsolatedController();
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            GameObject mon = CreateMonster(EMonster.Wisp);
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
            BattleController bc = CreateIsolatedController();
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            //# 먼저 raw 적용 후 데미지 — Current 50/200 상태로 만든다.
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Health hp = mon.GetComponent<Health>();
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
            BattleController bc = CreateIsolatedController();
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Health hp = mon.GetComponent<Health>();
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
            BattleController bc = CreateIsolatedController();
            GameObject plague = CreateMonster(EMonster.Plague);
            plague.AddComponent<PlagueSlowOnHit>();
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, 0.75f);
            plague.SetActive(true);
            yield return null;

            bc.ApplyMonsterStats(plague, EMonster.Plague, resetCurrent: true);

            //# _slowFactor 는 private — 리플렉션으로 확인. BaseSlowFactor 0.8 × 0.75 = 0.6.
            PlagueSlowOnHit slow = plague.GetComponent<PlagueSlowOnHit>();
            float applied = GetPrivate<float>(slow, "_slowFactor");
            Assert.AreEqual(0.6f, applied, 0.0001f, "플레이그 둔화 = BaseSlowFactor 0.8 × 0.75");
        }

        //# 회귀 — 플레이그 둔화는 풀 재사용(반복 ApplyMonsterStats) 시 복리 누적되지 않는다.
        //# baseline 이 const 0.8 이라 매 Pop 항상 0.8 부터 — §7.5.2 복리 버그 차단 검증.
        [UnityTest]
        public IEnumerator ApplyMonsterStats_플레이그_반복적용_복리누적_없음()
        {
            BattleController bc = CreateIsolatedController();
            GameObject plague = CreateMonster(EMonster.Plague);
            plague.AddComponent<PlagueSlowOnHit>();
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, 0.75f);
            plague.SetActive(true);
            yield return null;

            PlagueSlowOnHit slow = plague.GetComponent<PlagueSlowOnHit>();

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
            BattleController bc = CreateIsolatedController();
            //# 필드에 위스프 몬스터 — CharacterRegistry 에 등록 (Health.OnEnable 의 자기등록은
            //# 없으므로 명시 등록 — 실제로는 캐릭터가 자기 등록하지만 본 테스트는 합성).
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Health hp = mon.GetComponent<Health>();
            CharacterRegistry.RegisterMonster(mon.transform, hp);

            //# 강화 카드 픽 — dict 갱신 + 필드 소급.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(300, hp.Max, "필드 위스프에 소급 — 최대치 200×1.5=300");
        }

        //# 회귀 — 강화 소급은 다른 종 몬스터를 건드리지 않는다 (종별 격리).
        [UnityTest]
        public IEnumerator RegisterMonsterTypeBuff_다른종_몬스터는_불변()
        {
            BattleController bc = CreateIsolatedController();
            GameObject wisp = CreateMonster(EMonster.Wisp);
            GameObject wraith = CreateMonster(EMonster.Wraith);
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
            BattleController bc = CreateIsolatedController();
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Health hp = mon.GetComponent<Health>();
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
            BattleController bc = CreateIsolatedController();

            Assert.DoesNotThrow(() =>
                bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f),
                "필드 몬스터 0개 — dict 갱신만, 예외 없음");

            //# 이후 신규 Pop 은 갱신된 dict 반영.
            GameObject mon = CreateMonster(EMonster.Wisp);
            mon.SetActive(true);
            yield return null;
            bc.ApplyMonsterStats(mon, EMonster.Wisp, resetCurrent: true);
            Assert.AreEqual(300, mon.GetComponent<Health>().Max,
                "필드 0개여도 dict 갱신됨 — 이후 신규 위스프 300");
        }

        //# ===== 3. 동시 몬스터 캡 제거 — Battle 씬 통합 (spec §2.A) =====

        //# 회귀 (고가치) — 캡 제거 후 한 스폰 사이클이 구 캡(18)을 초과해도 truncate 되지 않는다.
        //# 구 테스트 'Battle씬_지속스폰_..캡18_절대초과없음' 을 캡 제거 spec 에 맞춰 반대 단언으로 대체.
        //# ISpawnerHost.SpawnFromSpawner(type, pos, count) 를 count=25 로 직접 호출 → 25마리 전량 등록 확인.
        [UnityTest]
        public IEnumerator 캡_제거_후_18마리_초과_스폰_truncate_없음()
        {
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();

            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 가 씬에 존재해야 함");

#if UNITY_EDITOR
            //# 카드 팝업 hang 우회 — 트리거 발생 시 첫 장 자동 픽.
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# 비동기 Start 완료(영웅 스폰) 대기.
            float elapsed = 0f;
            while (elapsed < 4f && CharacterRegistry.Heroes.Count == 0)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "4초 후 영웅 미스폰 — BattleController 비동기 초기화 미완");

            //# 스폰 직전 살아있는 몬스터 스냅샷.
            int aliveBefore = AliveMonsterCount();

            //# 한 사이클 25마리 강제 스폰 (구 캡 18 초과). 영웅 근처 위치.
            Vector3 spawnPos = CharacterRegistry.Heroes[0].Transform != null
                ? CharacterRegistry.Heroes[0].Transform.position + Vector3.right * 2f
                : Vector3.zero;
            const int RequestCount = 25;
            bc.SpawnFromSpawner(EMonster.Wisp, spawnPos, RequestCount);

            //# async void — Addressables 로드 + 25회 Pop 완료까지 등록수 누적을 대기 (unscaledDeltaTime).
            int target = aliveBefore + RequestCount;
            float wait = 0f;
            while (wait < 6f && AliveMonsterCount() < target)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            int aliveAfter = AliveMonsterCount();
            //# 캡 제거 회귀 핵심 — 18 에 막히지 않고 요청한 25마리가 전량 추가됐는지.
            Assert.GreaterOrEqual(aliveAfter, target,
                $"캡 제거 후 truncate 없어야 함 — before {aliveBefore} + {RequestCount} 요청, after {aliveAfter}");
            Assert.Greater(aliveAfter, 18,
                $"살아있는 몬스터 {aliveAfter} — 구 캡 18 을 넘겨 누적돼야 함 (캡 제거 검증)");

            yield return null;
        }

        //# 회귀 — 전투 종료(_model.Result != None) 후 SpawnFromSpawner 호출은 한 마리도 스폰하지 않는다 (종료검사 보존).
        //# 캡 제거 시 enforcement 4곳을 지우면서 종료검사까지 지웠다면 이 단언이 깨진다 (가드 박제).
        [UnityTest]
        public IEnumerator 전투_종료_후_SpawnFromSpawner_스폰_안됨_종료검사_보존()
        {
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();

            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 존재");

#if UNITY_EDITOR
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# 비동기 Start 완료 대기 — _model 인스턴스화까지.
            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<object>(bc, "_model") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            object model = GetPrivate<object>(bc, "_model");
            Assert.IsNotNull(model, "_model 인스턴스화");

            //# 전투를 강제 종료 상태로 — BattleStateModel.Result(public 필드) = Win 설정 (리플렉션).
            FieldInfo resultField = model.GetType().GetField("Result",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(resultField, "BattleStateModel.Result 필드 존재");
            resultField.SetValue(model, BattleResult.Win);

            int aliveBefore = AliveMonsterCount();

            //# 종료 상태에서 한 사이클 요청 — 종료검사로 즉시 return, 스폰 0.
            bc.SpawnFromSpawner(EMonster.Wisp, Vector3.zero, 10);

            //# 혹시 비동기로 스폰될 여지를 두고 잠시 대기 후 불변 확인.
            float wait = 0f;
            while (wait < 1.5f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(aliveBefore, AliveMonsterCount(),
                "전투 종료 후에는 종료검사로 스폰 차단 — 살아있는 수 불변");

            yield return null;
        }

        //# 회귀 (고가치) — 스포너 한 사이클이 N마리(>1) 를 같은 exactPos 가 아니라 산개시킨다.
        //# 버그: SpawnFromSpawner 가 count 만큼 Pop 하면서 전부 동일 좌표 배치 → 겹쳐 1마리처럼 보임.
        //# (몬스터는 OnEnable 에서 isKinematic 이라 물리로 흩어지지 않음.) SpawnMonsterRuntime 의 산개 패턴 부재.
        [UnityTest]
        public IEnumerator SpawnFromSpawner_N마리_스폰시_좌표_산개_겹치지_않음()
        {
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();

            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 존재");

#if UNITY_EDITOR
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# 비동기 Start 완료(영웅 스폰) 대기.
            float elapsed = 0f;
            while (elapsed < 4f && CharacterRegistry.Heroes.Count == 0)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(CharacterRegistry.Heroes.Count, 0, "영웅 스폰 완료 (비동기 초기화)");

            int aliveBefore = AliveMonsterCount();

            //# 한 사이클 5마리 모두 동일 exactPos 로 요청. 산개가 없으면 5마리 좌표가 전부 같다.
            Vector3 exactPos = new Vector3(7f, 0f, 3f);
            const int RequestCount = 5;
            bc.SpawnFromSpawner(EMonster.Wisp, exactPos, RequestCount);

            int target = aliveBefore + RequestCount;
            float wait = 0f;
            while (wait < 6f && AliveMonsterCount() < target)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.GreaterOrEqual(AliveMonsterCount(), target,
                $"5마리 전량 스폰 — before {aliveBefore} + {RequestCount}");

            //# 본 스폰 종(Wisp) 의 살아있는 몬스터 좌표 수집 — distinct 좌표가 2개 이상이어야 산개.
            List<Vector3> positions = new List<Vector3>();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || e.Health.IsAlive == false || e.Transform == null) continue;
                MonsterTag tag = e.Transform.GetComponent<MonsterTag>();
                if (tag == null || tag.Key != EMonster.Wisp) continue;
                positions.Add(e.Transform.position);
            }
            Assert.GreaterOrEqual(positions.Count, RequestCount,
                $"수집된 Wisp 좌표 {positions.Count} ≥ 요청 {RequestCount}");

            //# 산개 단언 — 모든 좌표가 동일하면 distinct == 1 (버그). 산개되면 distinct > 1.
            HashSet<Vector3> distinct = new HashSet<Vector3>(positions);
            Assert.Greater(distinct.Count, 1,
                $"N마리 좌표가 산개돼야 함 — distinct 좌표 {distinct.Count} (1 이면 전부 겹침 = 버그)");

            yield return null;
        }

        //# 살아있는 몬스터 수 — CharacterRegistry 순회.
        private static int AliveMonsterCount()
        {
            int alive = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
                if (e?.Health != null && e.Health.IsAlive) alive++;
            return alive;
        }

        //# 통합 — Battle 씬에 §5.3 스타터 프리셋대로 6개 Spawner 가 BattleController 에 배선돼 있다.
        //# _spawners 는 직렬화 필드 — Awake 시점에 이미 배선됨. 비동기 Start 완료를 기다릴 필요 없음.
        [UnityTest]
        public IEnumerator Battle씬_BattleController에_Spawner_6개_배선()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 등장만 대기 (1~2 프레임). 카드 팝업 hang 우회 — unscaledDeltaTime.
            BattleController bc = null;
            float wait = 0f;
            while (wait < 2f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "씬에 BattleController 존재");

            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            Assert.IsNotNull(spawners, "_spawners 배열 배선 확인");
            Assert.AreEqual(6, spawners.Length, "스타터 프리셋 — Spawner 6개 (§5.3)");
            foreach (Spawner sp in spawners)
                Assert.IsNotNull(sp, "Spawner 슬롯 누락 없음");

            yield return null;
        }

        //# ===== 4. 카드 리뉴얼 v0.6 — Plague Spawner #4 / Debuff Tier2 발화 / Multiply 자리 보존 =====

        //# 통합 — 카드 리뉴얼 v0.6 Plague Spawner 정합 (Battle.unity §3.1):
        //#   Wisp 2개(#1·#4) 구성에서 #4(180°) 가 Plague 로 전환되었다 (10s 주기, 1.5s 초기 지연).
        [UnityTest]
        public IEnumerator Battle씬_v0점6_Spawner_종분포_6종_각1개_Plague_포함()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 2f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "씬에 BattleController 존재");

            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            Assert.AreEqual(6, spawners.Length, "Spawner 6개 (§3.1)");

            //# 종별 카운트 — 카드 리뉴얼 v0.6 (Wisp 2 → Wisp 1 + Plague 1) 으로 6종 모두 1개씩 균등.
            Dictionary<EMonster, int> dist = new Dictionary<EMonster, int>();
            foreach (Spawner sp in spawners)
            {
                EMonster outputType = GetPrivate<EMonster>(sp, "_outputType");
                int v;
                dist.TryGetValue(outputType, out v);
                dist[outputType] = v + 1;
            }

            Assert.IsTrue(dist.ContainsKey(EMonster.Plague) && dist[EMonster.Plague] == 1,
                "Plague Spawner 정확히 1개 — Debuff 빌드 축 작동의 전제 (card-renewal.md §5)");
            Assert.IsTrue(dist.ContainsKey(EMonster.Wisp) && dist[EMonster.Wisp] == 1,
                "Wisp Spawner 1개 (v0.6 에서 2→1 로 감소, #4 가 Plague 로 전환)");
            Assert.AreEqual(6, dist.Count, "6종 모두 1개씩 균등 (v0.6 §3.1)");
        }

        //# 통합 — 카드 리뉴얼 v0.6: BattleController._synergy 가 Debuff 5장 픽에 Tier2 를 발화.
        //#   라이브 BattleController 의 BindTier 12회 호출(§Phase 2) 이 실제로 작동하는지 검증.
        [UnityTest]
        public IEnumerator Battle씬_v0점6_Debuff_5장_픽_시_Tier2_임계_도달()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 2f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "씬에 BattleController 존재");

            //# 비동기 Start (Addressables 로드 / 영웅 스폰) 완료를 대기 — _synergy 는 Start 안에서 생성됨.
            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<BuildSynergyService>(bc, "_synergy") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            BuildSynergyService synergy = GetPrivate<BuildSynergyService>(bc, "_synergy");
            Assert.IsNotNull(synergy, "BattleController._synergy 가 Start 안에서 인스턴스화됨");

            //# RegisterCardPick 5회 호출 — BattleContext.RegisterCardPick → BuildSynergyService.RegisterPick(axis, ctx).
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx, "BattleController._ctx 가 Start 안에서 인스턴스화됨");

            for (int i = 0; i < 5; i++)
            {
                ctx.RegisterCardPick(EBuildAxis.Debuff);
            }

            //# 5장 누적 — Tier1(3장 임계) + Tier2(5장 임계) 모두 도달했어야 함.
            //# Tier2 발화 자체 검증은 EditMode 단위 테스트가 커버 (DebuffSynergyTier2.Apply 가 ApplyHeroAura 호출).
            Assert.AreEqual(5, ctx.GetBuildCount(EBuildAxis.Debuff),
                "Debuff 5장 픽 시 빌드 카운트 5 — Tier1·Tier2 임계 도달 (Tier2 발화는 EditMode 단위 검증)");
            //# 다른 축은 미픽 — 0 유지.
            Assert.AreEqual(0, ctx.GetBuildCount(EBuildAxis.Tank), "타 축 미픽 — 카운트 0 유지");
            Assert.AreEqual(0, ctx.GetBuildCount(EBuildAxis.Dps), "타 축 미픽 — 카운트 0 유지");
            Assert.AreEqual(0, ctx.GetBuildCount(EBuildAxis.Swarm), "타 축 미픽 — 카운트 0 유지");
        }

        //# 회귀 — 카드 리뉴얼 v0.6: Multiply enum 자리 보존 (값 20).
        //#   spec D10 (Multiply 삭제) 의 실제 정책 = enum 자리·SO 파일명 보존 + 효과 클래스만 교체.
        [Test]
        public void v0점6_Multiply_enum_자리_보존_값20()
        {
            Assert.AreEqual(20, (int)ECardId.Multiply,
                "ECardId.Multiply enum 값 자리 보존 (20) — SO _id 직렬화 정합");
        }
    }
}
