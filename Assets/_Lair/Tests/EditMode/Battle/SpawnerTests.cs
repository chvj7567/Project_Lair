using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 지속 스폰 — Spawner 주기 구동 컴포넌트 본격 스위트 (엣지·회귀).
    //# Tick(dt) 수동 주입으로 EditMode 검증. 첫 발사는 첫 Tick(t≈0) 에 즉시, 이후 t=주기×n.
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
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# Spawner 를 직렬화 필드 주입 후 생성. EditMode 에서는 SetActive 토글이 OnEnable 을
        //# 신뢰성 있게 트리거하지 못하므로(테스트 라이프사이클 한계), 리플렉션으로 직접 호출한다.
        private Spawner CreateSpawner(EMonster outputType, float spawnPeriod, float initialDelay)
        {
            GameObject go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", spawnPeriod);
            SetPrivate(sp, "_initialDelay", initialDelay);
            //# OnEnable 명시 호출 — _currentType = _outputType / _outputCount = 1 / _timer = 0 / _firstSpawnDone = false.
            InvokeOnEnable(sp);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드가 존재해야 함 (production 시그니처 변경 감지)");
            fi.SetValue(target, value);
        }

        //# Spawner.OnEnable 을 리플렉션으로 직접 호출 — EditMode 테스트 라이프사이클 보정.
        private static void InvokeOnEnable(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Spawner.OnEnable 메서드 존재 확인 (production 시그니처 변경 감지)");
            mi.Invoke(c, null);
        }

        //# ===== 첫 발사 시점 (전투 시작 직후 즉시) =====

        //# 정상 — InitialDelay 0: 첫 Tick(0) 시점에 즉시 첫 발사.
        [Test]
        public void 첫발사_InitialDelay_0이면_즉시_발사()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

            sp.Tick(0f);

            Assert.AreEqual(1, host.CallCount, "InitialDelay 0 — 첫 Tick(0) 에 발사");
        }

        //# 회귀 (수정2) — InitialDelay 가 양수여도 첫 Tick 에 즉시 발사 (지연 무시).
        [Test]
        public void 첫발사_InitialDelay_양수여도_첫Tick에_즉시_발사()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Phantom, 6f, 2.5f);
            sp.Bind(host, null);

            //# InitialDelay 2.5 이지만 첫 Tick(0) 에 즉시 발사 — 게임 시작 직후 스폰.
            sp.Tick(0f);
            Assert.AreEqual(1, host.CallCount, "InitialDelay 2.5 무시 — 첫 Tick 에 즉시 발사");
        }

        //# ===== 주기 발사 위상 유지 (첫 발사 후 t = 주기×n) =====

        //# 정상 — 첫 발사(t≈0) 후 주기마다 1발씩. InitialDelay 와 무관하게 첫 발사 기준 위상.
        [Test]
        public void 주기발사_첫발사후_주기마다_1발()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            //# InitialDelay 1(무시), 주기 3.
            Spawner sp = CreateSpawner(EMonster.Wisp, 3f, 1f);
            sp.Bind(host, null);

            //# t=0 첫 발사 (즉시)
            sp.Tick(0f);
            Assert.AreEqual(1, host.CallCount, "첫 Tick 즉시 발사");

            //# +3 두 번째
            sp.Tick(3.0f);
            Assert.AreEqual(2, host.CallCount, "첫 발사 +주기 3 → 두 번째 발사");

            //# +3 세 번째
            sp.Tick(3.0f);
            Assert.AreEqual(3, host.CallCount, "세 번째 발사");
        }

        //# 회귀 — 작은 dt 다수로 위상이 첫 발사 기준으로 유지되는지 (드리프트 없음).
        //# 주기 3 / dt 0.5: 첫 Tick 즉시 1발(_timer 0 리셋), 이후 6 Tick(3.0초) 마다 1발.
        //# t=0,3,6,9 에서 발사 — 20 Tick(10초)이면 4발.
        [Test]
        public void 주기발사_작은_dt_다수_누적시_위상_유지()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Phantom, 3f, 1f);
            sp.Bind(host, null);

            for (int i = 0; i < 20; ++i)
                sp.Tick(0.5f);

            //# 첫 Tick(t=0) 즉시 + t=3,6,9 — 정확히 4발.
            Assert.AreEqual(4, host.CallCount, "첫 발사 후 주기 3 위상으로 총 4발");
        }

        //# 경계 — InitialDelay 0 일 때도 첫 발사(t=0) 후 주기마다 정확히 1발.
        [Test]
        public void 주기발사_InitialDelay_0_첫발사후_주기마다_1발()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

            //# t=0 첫 발사 후, 단일 Tick(100) — 주기 9 가 11번 들어가지만 1발만.
            sp.Tick(0f);
            sp.Tick(100f);

            Assert.AreEqual(2, host.CallCount, "dt 폭주 — 한 Tick 호출당 최대 1발 (폭주 스폰 방지)");
        }

        //# 엣지 — dt 폭주 후 작은 Tick — 남은 누적분으로 매끄럽게 추가 발사 (드레인, 예외 없음).
        [Test]
        public void dt폭주_후_작은_Tick_누적분_드레인()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

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
        //# 비활성화 전 발사를 진행한 Spawner 가 재활성화되면 다시 첫 발사(즉시) 국면으로 복귀.
        [Test]
        public void OnEnable_재호출시_타이머와_첫발사플래그_리셋()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

            sp.Tick(0f);    //# 첫 발사 완료 (즉시)
            sp.Tick(9f);    //# 두 번째 발사 — _firstSpawnDone=true 국면
            Assert.AreEqual(2, host.CallCount);

            //# 씬 재진입 시뮬 — OnEnable 직접 호출 (_timer=0 / _firstSpawnDone=false).
            //# EditMode 에서 SetActive 토글은 신뢰성 없어 리플렉션으로 직접 호출.
            InvokeOnEnable(sp);
            sp.Bind(host, null);  //# 재바인드 (Bind 는 OnEnable 에서 안 하므로 명시)

            //# 재진입 후 첫 Tick — _firstSpawnDone 리셋됐으므로 즉시 첫 발사여야 (리셋 확인).
            sp.Tick(0f);
            Assert.AreEqual(3, host.CallCount, "재진입 후 첫 Tick — 첫 발사 국면 복귀로 즉시 발사");

            //# 주기 미경과 — 미발사 (타이머도 0 으로 리셋됐는지 확인).
            sp.Tick(8.99f);
            Assert.AreEqual(3, host.CallCount, "재진입 후 주기 미경과 — 미발사 (타이머 리셋 확인)");
        }

        //# 회귀 — OnEnable 이 _outputCount 를 1 로 리셋. 추가소환 +N 후 재진입하면 출력 1 로 복귀.
        [Test]
        public void OnEnable_재호출시_동시출력수_1로_리셋()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

            //# 추가소환 카드 3회 — 동시 출력 4.
            sp.IncrementOutput();
            sp.IncrementOutput();
            sp.IncrementOutput();

            //# 씬 재진입 — OnEnable 직접 호출이 _outputCount=1 로 리셋.
            InvokeOnEnable(sp);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

            //# 융합 카드 — 출력 종을 레이스으로 변경.
            sp.ReplaceOutput(EMonster.Wraith);
            Assert.AreEqual(EMonster.Wraith, sp.CurrentType);

            //# 씬 재진입 — OnEnable 직접 호출이 _currentType = _outputType(Wisp) 으로 리셋.
            InvokeOnEnable(sp);
            sp.Bind(host, null);

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
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);

            //# Bind 전 Tick — 무동작 (타이머 누적도 없음).
            sp.Tick(5f);

            //# 이제 Bind 후 첫 Tick — InitialDelay 0 이므로 즉시 발사.
            sp.Bind(host, null);
            sp.Tick(0f);
            Assert.AreEqual(1, host.CallCount, "Bind 후 첫 Tick 정상 발사");
        }

        //# ===== 동시 출력 수 / 출력 종 런타임 상태 =====

        //# 정상 — IncrementOutput 한 만큼 SpawnFromSpawner 의 count 인자에 반영.
        [Test]
        public void IncrementOutput_횟수만큼_동시출력_count_증가()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Phantom, 6f, 0f);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.Bind(host, null);

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
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(9f, 0f, 0f);
            sp.Bind(host, null);

            sp.Tick(0f);
            Assert.AreEqual(new Vector3(9f, 0f, 0f), host.Spawns[0].pos);
        }

        //# ===== _spawnPoint 분리 스폰 지점 =====

        //# 정상 — _spawnPoint 가 null 이면 transform.position 을 fallback 으로 사용 (기존 동작 보전).
        [Test]
        public void spawnPoint_null이면_transform_position_fallback()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(5f, 0f, 0f);
            //# _spawnPoint 미할당 — 기본값 null.
            sp.Bind(host, null);

            sp.Tick(0f);

            Assert.AreEqual(new Vector3(5f, 0f, 0f), host.Spawns[0].pos,
                "_spawnPoint null fallback — transform.position 사용");
        }

        //# 엣지 — _spawnPoint 가 할당되면 transform.position 이 아닌 _spawnPoint.position 을 사용.
        [Test]
        public void spawnPoint_할당시_spawnPoint_position_사용()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(30f, 0f, 0f);  //# 맵 밖 위치

            //# _spawnPoint — 맵 안의 실제 스폰 지점.
            GameObject spawnPointGo = new GameObject("SpawnPoint");
            _spawned.Add(spawnPointGo);
            spawnPointGo.transform.position = new Vector3(8f, 0f, 0f);
            SetPrivate(sp, "_spawnPoint", spawnPointGo.transform);

            sp.Bind(host, null);
            sp.Tick(0f);

            Assert.AreEqual(new Vector3(8f, 0f, 0f), host.Spawns[0].pos,
                "_spawnPoint 할당 — transform.position(30) 아닌 spawnPoint.position(8) 사용");
        }

        //# ===== BattleZone 주입 — 스폰 위치 미사용 (수정1) =====
        //# zone 은 Bind 시그니처 보존을 위해 주입되나 스폰 위치 산정엔 더 이상 쓰이지 않는다.
        //# 스폰 위치는 항상 _spawnPoint → transform.position.

        //# 회귀 (수정1) — zone 을 주입해도 스폰 위치는 _spawnPoint(없으면 transform).
        //# 과거엔 zone 의 spawn point 가 우선이었으나, 그 기능이 제거되어 이제 zone 은 위치 산정에서 무시된다.
        [Test]
        public void Bind_with_zone_여도_스폰위치는_transform_position()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(30f, 0f, 0f);

            //# BattleZone 주입 (이제 위치 산정에 미사용 — zone 은 스폰 위치를 결정하지 못함).
            GameObject zoneGo = new GameObject("BattleZoneUT");
            BoxCollider col = zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(10, 1, 10);
            BattleZone zone = zoneGo.AddComponent<BattleZone>();
            _spawned.Add(zoneGo);
            MethodInfo awakeMi = typeof(BattleZone).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMi?.Invoke(zone, null);

            sp.Bind(host, zone);
            sp.Tick(0f);

            Assert.AreEqual(new Vector3(30f, 0f, 0f), host.Spawns[0].pos,
                "zone 주입돼도 무시 — _spawnPoint 미할당이라 transform.position(30) 사용");
        }

        //# 회귀 (수정1) — zone 이 있어도 _spawnPoint 가 할당되면 _spawnPoint 우선.
        [Test]
        public void Bind_with_zone_여도_spawnPoint_있으면_spawnPoint_우선()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(30f, 0f, 0f);

            GameObject zoneGo = new GameObject("BattleZoneUT");
            BoxCollider col = zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(10, 1, 10);
            BattleZone zone = zoneGo.AddComponent<BattleZone>();
            _spawned.Add(zoneGo);
            MethodInfo awakeMi = typeof(BattleZone).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMi?.Invoke(zone, null);

            //# Spawner._spawnPoint(8) — 우선 사용.
            GameObject spawnPointGo = new GameObject("SpawnPoint");
            spawnPointGo.transform.position = new Vector3(8f, 0f, 0f);
            _spawned.Add(spawnPointGo);
            SetPrivate(sp, "_spawnPoint", spawnPointGo.transform);

            sp.Bind(host, zone);
            sp.Tick(0f);

            Assert.AreEqual(new Vector3(8f, 0f, 0f), host.Spawns[0].pos,
                "_spawnPoint(8) 우선 — zone 주입/transform(30) 무시");
        }

        //# 엣지 — Bind 재호출시 host 갱신 (zone 은 위치 산정 무관).
        [Test]
        public void Bind_재호출시_host_갱신()
        {
            FakeSpawnerHost host1 = new FakeSpawnerHost();
            FakeSpawnerHost host2 = new FakeSpawnerHost();
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f, 0f);
            sp.transform.position = new Vector3(7f, 0f, 0f);

            sp.Bind(host1, null);
            sp.Tick(0f);
            Assert.AreEqual(1, host1.CallCount, "첫 Bind 후 host1 에 1회 발사");
            Assert.AreEqual(new Vector3(7f, 0f, 0f), host1.Spawns[0].pos, "transform.position 사용");

            //# Bind 재호출 — host2 로 갱신.
            sp.Bind(host2, null);
            sp.Tick(9f);   //# 주기 9 — 두 번째 발사.

            Assert.AreEqual(1, host1.CallCount, "host1 는 추가 발사 없음");
            Assert.AreEqual(1, host2.CallCount, "host2 로 갱신 — 1회 발사");
            Assert.AreEqual(new Vector3(7f, 0f, 0f), host2.Spawns[0].pos,
                "host2 도 동일 transform.position(7) 에 스폰");
        }
    }
}
