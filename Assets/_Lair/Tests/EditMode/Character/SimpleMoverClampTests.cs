using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# SimpleMover._clampZone 옵션 — 영웅 한정 zone-clamp. 몬스터는 미할당 (null) 이라 무동작.
    //# 클램프 경계는 BattleZone._clampHalfExtentX/Z(교전 trigger 와 분리, 기획서 §4.2) 로 산정 — collider size 와 무관.
    //# 본 스위트는 정사각 extent(X=Z)를 명시 주입해 경계를 테스트에 드러낸다(production 직사각 기본값 변경에 안 깨지게).
    public class SimpleMoverClampTests
    {
        private GameObject _zoneGo;
        private GameObject _moverGo;
        private GameObject _zoneGo2;

        [TearDown]
        public void TearDown()
        {
            if (_moverGo != null) Object.DestroyImmediate(_moverGo);
            if (_zoneGo != null) Object.DestroyImmediate(_zoneGo);
            if (_zoneGo2 != null) Object.DestroyImmediate(_zoneGo2);
        }

        //# 클램프 경계는 size 가 아니라 _clampHalfExtent — 그래서 size 는 임의(크게)로 두고 extent 만 명시 주입.
        private BattleZone CreateZoneAt(Vector3 center, float clampHalfExtent, out GameObject host)
        {
            host = new GameObject("ZoneUT");
            host.transform.position = center;
            BoxCollider col = host.AddComponent<BoxCollider>();
            //# size 는 교전 trigger 용(클램프와 무관) — 클램프보다 넉넉히 크게 두어 size 의존이 없음을 분명히.
            col.isTrigger = true; col.size = new Vector3(100, 1, 100);
            BattleZone zone = host.AddComponent<BattleZone>();
            MethodInfo mi = typeof(BattleZone).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(zone, null);
            SetClampHalfExtent(zone, clampHalfExtent);
            return zone;
        }

        //# 기본 헬퍼 — clampHalfExtent=5 명시. 5 는 production 직사각 기본값(X10/Z6) 과 다른 값이라
        //# 리플렉션 setter 가 잘못된 필드명으로 silent-fail 하면(기본값 잔존) 5 단언이 깨져 즉시 잡힌다.
        private BattleZone CreateZone(float clampHalfExtent = 5f)
        {
            return CreateZoneAt(Vector3.zero, clampHalfExtent, out _zoneGo);
        }

        //# 단일 값을 X/Z 양쪽에 동일 주입 — 정사각 클램프로 기존 케이스 의도 보존.
        //# (production 직사각 기본값(X10/Z6)과 다른 값을 명시 주입하므로 필드명 오타 silent-fail 도 잡힌다.)
        private static void SetClampHalfExtent(BattleZone zone, float value)
        {
            SetClampField(zone, "_clampHalfExtentX", value);
            SetClampField(zone, "_clampHalfExtentZ", value);
        }

        private static void SetClampField(BattleZone zone, string field, float value)
        {
            FieldInfo fi = typeof(BattleZone).GetField(field,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"BattleZone.{field} 필드가 존재해야 함");
            fi.SetValue(zone, value);
        }

        private SimpleMover CreateMover(Vector3 startPos, BattleZone clampZone)
        {
            _moverGo = new GameObject("MoverUT");
            _moverGo.transform.position = startPos;
            SimpleMover mover = _moverGo.AddComponent<SimpleMover>();
            //# Speed 를 크게(10만) 둬 한 FixedUpdate 의 maxDelta(Speed×fixedDeltaTime)가 어떤 target 도 항상 overshoot.
            //# MoveTowards 는 target 에서, ClampInside 는 extent 에서 캡 → fixedDeltaTime(물리 rate, TimeManager.asset 변동 가능) 에 무관하게 도달/클램프 단언이 성립.
            mover.Speed = 100000f;
            if (clampZone != null)
                mover.BindClampZone(clampZone);
            return mover;
        }

        //# FixedUpdate 를 리플렉션으로 1회 호출 — EditMode 에서 물리 시뮬 없이 검증.
        private static void InvokeFixedUpdate(SimpleMover mover)
        {
            MethodInfo mi = typeof(SimpleMover).GetMethod("FixedUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SimpleMover.FixedUpdate 메서드 존재");
            mi.Invoke(mover, null);
        }

        //# EditMode 의 AddComponent 는 OnEnable 미발화라 SetActive 토글로도 OnEnable 이 안 뜬다
        //# (HeroAuraRunnerStatusIconTests 와 동일 사유). 결정적 트리거를 위해 리플렉션으로 직접 호출.
        private static void InvokeOnEnable(SimpleMover mover)
        {
            MethodInfo mi = typeof(SimpleMover).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SimpleMover.OnEnable 메서드 존재");
            mi.Invoke(mover, null);
        }

        private static BattleZone GetClampZone(SimpleMover mover)
        {
            FieldInfo fi = typeof(SimpleMover).GetField("_clampZone",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi);
            return fi.GetValue(mover) as BattleZone;
        }

        //# ===== 기본 분기 (시드) =====

        [Test]
        public void clampZone_null_이면_zone_밖으로_자유_이동()
        {
            //# 몬스터 시뮬레이션 — _clampZone 미할당.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: null);
            //# zone 밖 X=20 으로 이동 명령.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# Speed 큼 → 한 FixedUpdate 로 overshoot, MoveTowards 가 target(20)에서 캡. 클램프 없으니 그대로 도달.
            Assert.AreEqual(20f, _moverGo.transform.position.x, 0.5f,
                "_clampZone null — 자유 이동, 클램프 경계 무시하고 target(20)까지 도달");
        }

        [Test]
        public void clampZone_할당시_zone_경계로_clamp()
        {
            //# clampHalfExtent=5 명시 — 경계가 5(size 와 무관).
            BattleZone zone = CreateZone(clampHalfExtent: 5f);
            //# 영웅 시뮬레이션 — _clampZone 할당.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);
            //# zone 밖 X=20 으로 이동 명령.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# 한 번에 X=20 직진하지만, ClampInside 가 _clampHalfExtent(5) 로 정확히 자른다.
            //# AreEqual — 너무 약하게 당겨도(예 6), 너무 세게 당겨도(예 4) 잡힌다 (LessOrEqual 은 과당김을 통과시켜 vacuous).
            Assert.AreEqual(5f, _moverGo.transform.position.x, 0.001f,
                "_clampZone 할당 — _clampHalfExtent(5) 경계로 정확히 클램프 (collider size 무관)");
        }

        //# ===== BindClampZone 재호출 / 해제 =====

        //# 엣지 — BindClampZone 을 다른 zone 으로 재호출하면 새 zone 의 _clampHalfExtent 로 클램프된다.
        //# 핵심: 두 zone 을 size(클램프가 안 읽음)가 아니라 _clampHalfExtent 로 구별해야 rebind 활성화가 vacuous 하지 않다.
        [Test]
        public void BindClampZone_재호출시_새_zone으로_갱신()
        {
            //# 좁은 zone — extent=5.
            BattleZone smallZone = CreateZoneAt(Vector3.zero, clampHalfExtent: 5f, out _zoneGo);
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: smallZone);

            //# 넓은 zone 으로 교체 — extent=20.
            BattleZone bigZone = CreateZoneAt(Vector3.zero, clampHalfExtent: 20f, out _zoneGo2);
            mover.BindClampZone(bigZone);

            //# 검증 (a) — _clampZone 필드 자체가 갱신됨.
            Assert.AreSame(bigZone, GetClampZone(mover),
                "BindClampZone(big) 호출 후 _clampZone 가 big 으로 교체");

            //# 검증 (b) — 새 extent(20) 가 실제 활성화됨을 입증. X=15 는 큰 zone(±20) 안이라 클램프 안 됨.
            //#   만약 옛 zone(extent 5)이 남아 있었다면 X=5 로 잘려 단언이 깨진다 → small/big 구별 성립.
            mover.MoveTo(new Vector3(15, 0, 0));
            InvokeFixedUpdate(mover);
            Assert.AreEqual(15f, _moverGo.transform.position.x, 0.5f,
                "큰 zone(extent 20) 안의 X=15 는 그대로 도달 — 새 extent 가 활성화됨(옛 5 면 잘렸을 것)");
        }

        //# 엣지 — BindClampZone(null) 로 해제하면 이후 zone 클램프 동작 안 함.
        [Test]
        public void BindClampZone_null로_해제하면_clamp_무동작()
        {
            BattleZone zone = CreateZone(clampHalfExtent: 5f);
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# 클램프 해제.
            mover.BindClampZone(null);
            Assert.IsNull(GetClampZone(mover), "BindClampZone(null) — _clampZone 해제");

            //# 이후 zone 밖 이동 명령 — 자유 이동.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# AreEqual(20) — 해제 실패로 여전히 클램프되면(extent 5) X=5 로 잘려 깨진다.
            //#   (옛 Greater(5) 단언은 해제가 silent-fail 해 5 로 잘려도, 클램프가 7 이면 7>5 로 통과해버려 vacuous 였음.)
            Assert.AreEqual(20f, _moverGo.transform.position.x, 0.5f,
                "_clampZone 해제 후 — 클램프 없이 target(20)까지 자유 이동");
        }

        //# ===== 정확 도달 / 모서리 =====

        //# 엣지 — clamp 경계 안의 좌표로 이동 명령 시 클램프 없이 정확히 그 위치까지 도달.
        //# ClampInside 가 입력 그대로 반환하는 분기 (=무동작) 회귀.
        [Test]
        public void zone_안의_좌표로_이동시_정확히_도달()
        {
            BattleZone zone = CreateZone(clampHalfExtent: 5f);
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# Speed=1000 + dt=0.02 → 한 프레임당 20 이동. target (3,0,4) 는 extent 5 안 → 직선 도달.
            Vector3 target = new Vector3(3f, 0f, 4f);
            mover.MoveTo(target);
            InvokeFixedUpdate(mover);

            Assert.AreEqual(3f, _moverGo.transform.position.x, 0.01f,
                "clamp(5) 안 X=3 정확 도달 (클램프 무동작)");
            Assert.AreEqual(4f, _moverGo.transform.position.z, 0.01f,
                "clamp(5) 안 Z=4 정확 도달");
        }

        //# 엣지 — 정확히 clamp 경계 좌표로 이동하면 그 경계에 정확히 멈춤.
        //# extent=5 명시 → MoveTo(5) 가 진짜 경계값이 됨 (옛 테스트는 5<7 이라 우연 통과 — 의도 거짓).
        [Test]
        public void zone_경계_좌표로_이동시_경계에_정확히_도달()
        {
            BattleZone zone = CreateZone(clampHalfExtent: 5f);   //# 경계 = ±5.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# 정확히 경계 — X=5 (= _clampHalfExtent).
            mover.MoveTo(new Vector3(5f, 0f, 0f));
            InvokeFixedUpdate(mover);

            Assert.AreEqual(5f, _moverGo.transform.position.x, 0.001f,
                "정확 경계 — X=5(_clampHalfExtent) 에 정확히 도달, 넘지도 모자라지도 않음");
            Assert.AreEqual(0f, _moverGo.transform.position.z, 0.001f);
        }

        //# 엣지 — 모서리(X/Z 둘 다 밖) 로 이동하면 모서리 좌표(둘 다 extent)로 클램프.
        [Test]
        public void zone_모서리_밖으로_이동시_모서리에_클램프()
        {
            BattleZone zone = CreateZone(clampHalfExtent: 5f);
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# (20, 0, 20) — 모서리(+X, +Z) 밖. extent=5 → Clamp 결과 (5, 0, 5).
            mover.MoveTo(new Vector3(20f, 0f, 20f));
            InvokeFixedUpdate(mover);

            Assert.AreEqual(5f, _moverGo.transform.position.x, 0.001f, "X 모서리 클램프 (extent 5)");
            Assert.AreEqual(5f, _moverGo.transform.position.z, 0.001f, "Z 모서리 클램프 (extent 5)");
        }

        //# ===== 풀 재사용 State Reset (Rule 03 §4) =====

        //# 정상 케이스 — CHMPool.Push/Pop 은 SetActive(false)/(true) 로 OnDisable/OnEnable 을 발생시킨다(§4).
        //# 직전 배틀에서 이동 중(_moving=true) 이었던 오브젝트가 재활성화되면 이동 상태가 리셋되어야
        //# 마을 쇼케이스처럼 Mover 를 즉시 비활성화하는 재사용처에서 walk 애니가 고착되지 않는다.
        [Test]
        public void 재활성화시_이전_이동상태가_리셋된다()
        {
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: null);
            mover.MoveTo(new Vector3(10f, 0f, 0f));
            Assert.IsTrue(mover.IsMoving, "MoveTo 직후 IsMoving true (사전조건)");

            //# 풀 재사용(CHMPool Pop) 시뮬레이션 — OnEnable 직접 호출(EditMode SetActive 미발화 회피).
            InvokeOnEnable(mover);

            Assert.IsFalse(mover.IsMoving,
                "재활성화(OnEnable) 시 이전 배틀의 이동 상태(_moving)가 리셋되어야 walk 애니 고착 없음");
        }

        //# 엣지 — 재활성화 후 새로 MoveTo 를 호출하면 리셋과 무관하게 정상적으로 다시 이동한다(리셋이 과도하지 않음).
        [Test]
        public void 재활성화_후_새_MoveTo는_정상_동작()
        {
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: null);
            mover.MoveTo(new Vector3(10f, 0f, 0f));

            InvokeOnEnable(mover);

            mover.MoveTo(new Vector3(20f, 0f, 0f));
            InvokeFixedUpdate(mover);

            Assert.IsTrue(mover.IsMoving, "재활성화 후 새 MoveTo 호출 시 다시 이동 상태가 되어야 함");
            Assert.AreEqual(20f, _moverGo.transform.position.x, 0.5f,
                "재활성화 후에도 MoveTo 로 정상 이동");
        }
    }
}
