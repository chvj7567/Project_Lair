using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 지속 스폰 — Spawner 주기 구동 컴포넌트 본격 스위트 (엣지·회귀).
    //# Tick(dt) 수동 주입으로 EditMode 검증. 첫 발사 t=InitialDelay, 이후 t=InitialDelay+주기×n (§2.4).
    public class SpawnerTests
    {
        //# ISpawnerHost 테스트 더블 — SpawnFromSpawner 호출을 전부 기록.
        private class FakeSpawnerHost : ISpawnerHost
        {
            public readonly List<(EMonster type, Vector3 pos, int count)> Spawns = new();
            public int CallCount => Spawns.Count;

            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
                => Spawns.Add((type, exactPos, count));
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# Spawner 를 직렬화 필드 주입 후 생성. EditMode 에서는 SetActive 토글이 OnEnable 을
        //# 신뢰성 있게 트리거하지 못하므로(테스트 라이프사이클 한계), 리플렉션으로 직접 호출한다.
        //# PlayMode 에서는 production 의 OnEnable 이 자연 호출 — 본 헬퍼는 EditMode 전용.
        private Spawner CreateSpawner(EMonster outputType, float spawnPeriod, float initialDelay)
        {
            var go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            var sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", spawnPeriod);
            SetPrivate(sp, "_initialDelay", initialDelay);
            //# OnEnable 명시 호출 — _currentType = _outputType / _outputCount = 1 / _timer = 0 / _firstSpawnDone = false.
            InvokeOnEnable(sp);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드가 존재해야 함 (production 시그니처 변경 감지)");
            fi.SetValue(target, value);
        }

        //# Spawner.OnEnable 을 리플렉션으로 직접 호출 — EditMode 테스트 라이프사이클 보정.
        private static void InvokeOnEnable(Component c)
        {
            var mi = c.GetType().GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Spawner.OnEnable 메서드 존재 확인 (production 시그니처 변경 감지)");
            mi.Invoke(c, null);
        }

        //# ===== 첫 발사 시점 (위상 오프셋 §2.4) =====

        //# 정상 — InitialDelay 0: 첫 Tick(0) 시점에 즉시 첫 발사.
        [Test]
        public void 첫발사_InitialDelay_0이면_즉시_발사()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            sp.Tick(0f);

            Assert.AreEqual(1, host.CallCount, "InitialDelay 0 — 첫 Tick(0) 에 발사");
        }

        //# 경계 — InitialDelay 0.5: 0.49 누적까진 미발사, 0.5 도달 시 발사.
        [Test]
        public void 첫발사_InitialDelay_0점5_경계_정확()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Phantom, 6f, 0.5f);
            sp.Bind(host);

            sp.Tick(0.49f);
            Assert.AreEqual(0, host.CallCount, "0.49s — InitialDelay 0.5 미만이라 미발사");

            sp.Tick(0.01f);
            Assert.AreEqual(1, host.CallCount, "누적 0.5s — InitialDelay 도달 시 첫 발사");
        }

        //# 경계 — InitialDelay 2.5: 스타터 프리셋 최대 지연. 2.5 도달 전 미발사.
        [Test]
        public void 첫발사_InitialDelay_2점5_도달전_미발사()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Hex, 15f, 2.5f);
            sp.Bind(host);

            sp.Tick(2.4f);
            Assert.AreEqual(0, host.CallCount, "2.4s — 미발사");

            sp.Tick(0.1f);
            Assert.AreEqual(1, host.CallCount, "누적 2.5s — 첫 발사");
        }

        //# ===== 주기 발사 위상 유지 (t = InitialDelay + 주기×n) =====

        //# 정상 — InitialDelay 후 주기마다 1발씩. 위상이 InitialDelay 기준으로 유지된다.
        [Test]
        public void 주기발사_InitialDelay_후_주기마다_1발()
        {
            var host = new FakeSpawnerHost();
            //# InitialDelay 1, 주기 3.
            var sp = CreateSpawner(EMonster.Wisp, 3f, 1f);
            sp.Bind(host);

            //# t=1.0 첫 발사
            sp.Tick(1.0f);
            Assert.AreEqual(1, host.CallCount, "t=1.0 첫 발사");

            //# t=4.0 두 번째 (InitialDelay 1 + 주기 3)
            sp.Tick(3.0f);
            Assert.AreEqual(2, host.CallCount, "t=4.0 두 번째 발사");

            //# t=7.0 세 번째
            sp.Tick(3.0f);
            Assert.AreEqual(3, host.CallCount, "t=7.0 세 번째 발사");
        }

        //# 회귀 — 작은 dt 다수로 위상이 InitialDelay 기준으로 유지되는지 (드리프트 없음).
        //# InitialDelay 1 / 주기 3 / dt 0.5 × 20 = t 10 → t=1,4,7,10 에서 4발.
        //# dt 는 IEEE 754 정확 표현이 되는 0.5 사용 — 0.1 누적은 부동소수 드리프트로 마지막 발사를
        //# 경계에서 놓칠 수 있어 테스트가 깨졌었음. 0.5×20=10.0 은 부동소수 정확값.
        [Test]
        public void 주기발사_작은_dt_다수_누적시_위상_유지()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Phantom, 3f, 1f);
            sp.Bind(host);

            for (int i = 0; i < 20; ++i)
                sp.Tick(0.5f);

            //# t=1,4,7,10 — 정확히 4발.
            Assert.AreEqual(4, host.CallCount, "t=10 까지 InitialDelay 1 + 주기 3 위상으로 4발");
        }

        //# 경계 — InitialDelay 0 일 때도 첫 발사(t=0) 후 주기마다 정확히 1발.
        [Test]
        public void 주기발사_InitialDelay_0_첫발사후_주기마다_1발()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            sp.Tick(0f);            //# t=0 첫 발사
            Assert.AreEqual(1, host.CallCount);

            sp.Tick(8.99f);         //# t=8.99 — 주기 9 미만, 미발사
            Assert.AreEqual(1, host.CallCount, "주기 미경과 — 미발사");

            sp.Tick(0.01f);         //# t=9.0 — 주기 경과
            Assert.AreEqual(2, host.CallCount, "주기 9 경과 — 두 번째 발사");
        }

        //# ===== dt 폭주 — 폭주 스폰 방지 =====

        //# 엣지 — 한 프레임 dt 폭주(100s)에도 1주기 1발사. 누적 dt 가 커도 1발만.
        [Test]
        public void dt폭주_한프레임_100초도_1발만_발사()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            //# t=0 첫 발사 후, 단일 Tick(100) — 주기 9 가 11번 들어가지만 1발만.
            sp.Tick(0f);
            sp.Tick(100f);

            Assert.AreEqual(2, host.CallCount, "dt 폭주 — 한 Tick 호출당 최대 1발 (폭주 스폰 방지)");
        }

        //# 엣지 — dt 폭주 후 작은 Tick — 남은 누적분으로 매끄럽게 추가 발사 (드레인, 예외 없음).
        [Test]
        public void dt폭주_후_작은_Tick_누적분_드레인()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            sp.Tick(0f);     //# t=0 첫 발사 (총 1)
            sp.Tick(100f);   //# 1발 + _timer 에 초과분 (~91) 남음 (총 2)

            //# 남은 ~91 은 주기 9 의 10배 이상 — 작은 Tick 마다 1발씩 드레인.
            sp.Tick(0.001f);
            Assert.AreEqual(3, host.CallCount, "폭주 잔여분 — 다음 Tick 에서 즉시 1발 드레인");

            sp.Tick(0.001f);
            Assert.AreEqual(4, host.CallCount, "잔여분 계속 드레인 — 호출당 1발");
        }

        //# ===== OnEnable 상태 리셋 (씬 재진입) =====

        //# 회귀 — 씬 재진입(OnEnable 재호출) 시 _firstSpawnDone·타이머 리셋.
        //# 비활성화 전 첫 발사를 끝낸 Spawner 가 재활성화되면 다시 InitialDelay 부터 시작.
        [Test]
        public void OnEnable_재호출시_타이머와_첫발사플래그_리셋()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 1f);
            sp.Bind(host);

            sp.Tick(1f);    //# 첫 발사 완료
            Assert.AreEqual(1, host.CallCount);

            //# 씬 재진입 시뮬 — OnEnable 직접 호출 (_timer=0 / _firstSpawnDone=false).
            //# EditMode 에서 SetActive 토글은 신뢰성 없어 리플렉션으로 직접 호출.
            InvokeOnEnable(sp);
            sp.Bind(host);  //# 재바인드 (Bind 는 OnEnable 에서 안 하므로 명시)

            //# InitialDelay 1 미만은 다시 미발사여야 — 리셋 안 됐으면 즉시 발사됨.
            sp.Tick(0.5f);
            Assert.AreEqual(1, host.CallCount, "재진입 후 InitialDelay 미도달 — 미발사 (타이머 리셋 확인)");

            sp.Tick(0.5f);
            Assert.AreEqual(2, host.CallCount, "재진입 후 누적 1.0s — 첫 발사 다시 발생");
        }

        //# 회귀 — OnEnable 이 _outputCount 를 1 로 리셋. 추가소환 +N 후 재진입하면 출력 1 로 복귀.
        [Test]
        public void OnEnable_재호출시_동시출력수_1로_리셋()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            //# 추가소환 카드 3회 — 동시 출력 4.
            sp.IncrementOutput();
            sp.IncrementOutput();
            sp.IncrementOutput();

            //# 씬 재진입 — OnEnable 직접 호출이 _outputCount=1 로 리셋.
            InvokeOnEnable(sp);
            sp.Bind(host);

            sp.Tick(0f);
            Assert.AreEqual(1, host.CallCount);
            Assert.AreEqual(1, host.Spawns[0].count,
                "재진입 후 동시 출력 1 로 리셋 (이전 +N 잔존 금지)");
        }

        //# 회귀 — OnEnable 이 _currentType 를 직렬화 _outputType 으로 리셋.
        //# 융합 카드로 출력 종 변경 후 재진입하면 원래 종으로 복귀.
        [Test]
        public void OnEnable_재호출시_출력종_직렬화값으로_리셋()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            //# 융합 카드 — 출력 종을 레이스으로 변경.
            sp.ReplaceOutput(EMonster.Wraith);
            Assert.AreEqual(EMonster.Wraith, sp.CurrentType);

            //# 씬 재진입 — OnEnable 직접 호출이 _currentType = _outputType(Wisp) 으로 리셋.
            InvokeOnEnable(sp);
            sp.Bind(host);

            Assert.AreEqual(EMonster.Wisp, sp.CurrentType,
                "재진입 후 출력 종이 직렬화 _outputType(Wisp) 으로 리셋");

            sp.Tick(0f);
            Assert.AreEqual(EMonster.Wisp, host.Spawns[0].type, "리셋된 종으로 스폰");
        }

        //# ===== _host == null 방어 =====

        //# 엣지 — Bind 미호출(_host==null) 시 Tick 은 무동작, 예외 없음.
        [Test]
        public void Bind_미호출시_Tick_무동작()
        {
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            //# Bind 안 함 — _host == null.

            Assert.DoesNotThrow(() =>
            {
                sp.Tick(0f);
                sp.Tick(100f);
            }, "_host null 일 때 Tick 은 예외 없이 무동작");
        }

        //# 엣지 — Bind 전 Tick 은 _host null 검사로 즉시 return — _timer += dt 도 실행 안 됨.
        //# 따라서 Bind 후 첫 Tick(0) 이 InitialDelay 0 기준으로 정상 첫 발사를 한다.
        [Test]
        public void Bind_미호출_Tick_후_Bind하면_정상_발사()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);

            //# Bind 전 Tick — 무동작 (타이머 누적도 없음).
            sp.Tick(5f);

            //# 이제 Bind 후 첫 Tick — InitialDelay 0 이므로 즉시 발사.
            sp.Bind(host);
            sp.Tick(0f);
            Assert.AreEqual(1, host.CallCount, "Bind 후 첫 Tick 정상 발사");
        }

        //# ===== 동시 출력 수 / 출력 종 런타임 상태 =====

        //# 정상 — IncrementOutput 한 만큼 SpawnFromSpawner 의 count 인자에 반영.
        [Test]
        public void IncrementOutput_횟수만큼_동시출력_count_증가()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Phantom, 6f, 0f);
            sp.Bind(host);

            //# 추가소환 2회 — 동시 출력 3.
            sp.IncrementOutput();
            sp.IncrementOutput();

            sp.Tick(0f);
            Assert.AreEqual(3, host.Spawns[0].count, "기본 1 + Increment 2회 = 3마리 동시 출력");
        }

        //# 정상 — ReplaceOutput 후 CurrentType 변경 + 이후 스폰이 변경 종으로.
        [Test]
        public void ReplaceOutput_이후_스폰종_변경()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            sp.ReplaceOutput(EMonster.Wraith);
            sp.Tick(0f);

            Assert.AreEqual(EMonster.Wraith, sp.CurrentType, "CurrentType 이 레이스으로 변경");
            Assert.AreEqual(EMonster.Wraith, host.Spawns[0].type, "융합 후 레이스 스폰");
        }

        //# §3.5 케이스 3 — 추가소환(출력+1) 후 융합(출력종 변경) → 출력 수 보너스 유지.
        //# 동시 출력 수는 Spawner 슬롯에 귀속, 출력 종만 바뀐다.
        [Test]
        public void 추가소환_후_융합_동시출력수_유지_종만_변경()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            //# SpawnWisps 2픽 — 위스프 Spawner 동시 출력 3.
            sp.IncrementOutput();
            sp.IncrementOutput();
            //# 융합(위스프→레이스) — 출력 종만 레이스으로. 동시 출력 수는 유지.
            sp.ReplaceOutput(EMonster.Wraith);

            sp.Tick(0f);
            Assert.AreEqual(EMonster.Wraith, host.Spawns[0].type, "출력 종은 레이스으로 변경");
            Assert.AreEqual(3, host.Spawns[0].count, "동시 출력 수 3 은 Spawner 슬롯에 귀속 — 유지");
        }

        //# §3.5 케이스 — 융합 후 추가소환. 융합으로 종이 바뀌면 그 종에 추가소환이 작동한다
        //# (Spawner 단위 검증 — 본 테스트는 ReplaceOutput → IncrementOutput 순서 무결성).
        [Test]
        public void 융합_후_추가소환_변경된종으로_count_증가()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host);

            sp.ReplaceOutput(EMonster.Wraith);
            sp.IncrementOutput();

            sp.Tick(0f);
            Assert.AreEqual(EMonster.Wraith, host.Spawns[0].type);
            Assert.AreEqual(2, host.Spawns[0].count, "융합 후 추가소환 — 기본 1 + 1 = 2");
        }

        //# 정상 — 스폰 위치는 Spawner 의 transform.position.
        [Test]
        public void 스폰_위치는_Spawner_transform_position()
        {
            var host = new FakeSpawnerHost();
            var sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(9f, 0f, 0f);
            sp.Bind(host);

            sp.Tick(0f);
            Assert.AreEqual(new Vector3(9f, 0f, 0f), host.Spawns[0].pos);
        }
    }
}
