using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# SimpleMover._clampZone 옵션 — 영웅 한정 zone-clamp. 몬스터는 미할당 (null) 이라 무동작.
    //# 시드: gameplay-programmer. 본 스위트는 BindClampZone 재호출/해제, 정확 도달, 모서리 정확 일치 회귀.
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

        private BattleZone CreateZoneAt(Vector3 center, Vector3 size, out GameObject host)
        {
            host = new GameObject("ZoneUT");
            host.transform.position = center;
            BoxCollider col = host.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = size;
            BattleZone zone = host.AddComponent<BattleZone>();
            MethodInfo mi = typeof(BattleZone).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(zone, null);
            return zone;
        }

        private BattleZone CreateZone()
        {
            return CreateZoneAt(Vector3.zero, new Vector3(10, 1, 10), out _zoneGo);
        }

        private SimpleMover CreateMover(Vector3 startPos, BattleZone clampZone)
        {
            _moverGo = new GameObject("MoverUT");
            _moverGo.transform.position = startPos;
            SimpleMover mover = _moverGo.AddComponent<SimpleMover>();
            //# Time.fixedDeltaTime 기본값 0.02 — Speed=1000 이면 한 프레임당 20 단위 이동 (target X=20 에 정확히 도달).
            //# Speed=100 으론 2 단위/프레임 이라 5 (zone 경계) 도달도 안 됨 → 테스트 의도(자유 이동시 5 초과) 가 깨짐.
            mover.Speed = 1000f;
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

            //# Speed=1000 + 큰 fixedDeltaTime — 한 번에 도달.
            Assert.Greater(_moverGo.transform.position.x, 5f,
                "_clampZone null — 자유 이동, zone 경계(5)를 넘어감");
        }

        [Test]
        public void clampZone_할당시_zone_경계로_clamp()
        {
            BattleZone zone = CreateZone();
            //# 영웅 시뮬레이션 — _clampZone 할당.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);
            //# zone 밖 X=20 으로 이동 명령.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            //# 한 번에 X=20 직진하지만, ClampInside 가 X=5 로 자름.
            Assert.LessOrEqual(_moverGo.transform.position.x, 5.001f,
                "_clampZone 할당 — zone bounds.max.x (5) 안쪽으로 클램프");
        }

        //# ===== BindClampZone 재호출 / 해제 =====

        //# 엣지 — BindClampZone 을 다른 zone 으로 재호출하면 새 zone 의 bounds 로 클램프된다.
        [Test]
        public void BindClampZone_재호출시_새_zone으로_갱신()
        {
            BattleZone smallZone = CreateZone();   //# center=0, size=(10,1,10) → ±5.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: smallZone);

            //# 더 큰 zone 으로 교체 — size=(40,1,40) → ±20.
            BattleZone bigZone = CreateZoneAt(Vector3.zero, new Vector3(40, 1, 40), out _zoneGo2);
            mover.BindClampZone(bigZone);

            //# 검증 (a) — _clampZone 필드 자체가 갱신됨.
            Assert.AreSame(bigZone, GetClampZone(mover),
                "BindClampZone(big) 호출 후 _clampZone 가 big 으로 교체");

            //# 검증 (b) — 큰 zone 이라 X=15 이동은 클램프 안 됨.
            mover.MoveTo(new Vector3(15, 0, 0));
            InvokeFixedUpdate(mover);
            Assert.AreEqual(15f, _moverGo.transform.position.x, 0.5f,
                "큰 zone(±20) 안의 X=15 는 그대로 도달 (Speed=1000 한 프레임 도달)");
        }

        //# 엣지 — BindClampZone(null) 로 해제하면 이후 zone 클램프 동작 안 함.
        [Test]
        public void BindClampZone_null로_해제하면_clamp_무동작()
        {
            BattleZone zone = CreateZone();
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# 클램프 해제.
            mover.BindClampZone(null);
            Assert.IsNull(GetClampZone(mover), "BindClampZone(null) — _clampZone 해제");

            //# 이후 zone 밖 이동 명령 — 자유 이동.
            mover.MoveTo(new Vector3(20, 0, 0));
            InvokeFixedUpdate(mover);

            Assert.Greater(_moverGo.transform.position.x, 5f,
                "_clampZone 해제 후 — zone 경계(5) 를 넘어 자유 이동");
        }

        //# ===== 정확 도달 / 모서리 =====

        //# 엣지 — zone 안의 좌표로 이동 명령 시 클램프 없이 정확히 그 위치까지 도달.
        //# ClampInside 가 입력 그대로 반환하는 분기 (=무동작) 회귀.
        [Test]
        public void zone_안의_좌표로_이동시_정확히_도달()
        {
            BattleZone zone = CreateZone();
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# Speed=1000 + dt=0.02 → 한 프레임당 20 이동. target X=3 직선 도달.
            Vector3 target = new Vector3(3f, 0f, 4f);
            mover.MoveTo(target);
            InvokeFixedUpdate(mover);

            Assert.AreEqual(3f, _moverGo.transform.position.x, 0.01f,
                "zone 안 X=3 정확 도달 (클램프 무동작)");
            Assert.AreEqual(4f, _moverGo.transform.position.z, 0.01f,
                "zone 안 Z=4 정확 도달");
        }

        //# 엣지 — 정확히 zone 경계 좌표로 이동하면 그 경계에 정확히 멈춤.
        //# 부동소수 오차로 경계가 살짝 어긋날 위험 회귀.
        [Test]
        public void zone_경계_좌표로_이동시_경계에_정확히_도달()
        {
            BattleZone zone = CreateZone();   //# ±5.
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# 정확히 경계 — X=5.
            mover.MoveTo(new Vector3(5f, 0f, 0f));
            InvokeFixedUpdate(mover);

            Assert.AreEqual(5f, _moverGo.transform.position.x, 0.001f,
                "정확 경계 — X=5 도달");
            Assert.AreEqual(0f, _moverGo.transform.position.z, 0.001f);
        }

        //# 엣지 — 모서리(X/Z 둘 다 밖) 로 이동하면 모서리 좌표로 클램프.
        [Test]
        public void zone_모서리_밖으로_이동시_모서리에_클램프()
        {
            BattleZone zone = CreateZone();
            SimpleMover mover = CreateMover(Vector3.zero, clampZone: zone);

            //# (20, 0, 20) — 모서리(+X, +Z) 밖. Clamp 결과 (5, 0, 5).
            mover.MoveTo(new Vector3(20f, 0f, 20f));
            InvokeFixedUpdate(mover);

            Assert.LessOrEqual(_moverGo.transform.position.x, 5.001f, "X 모서리 클램프");
            Assert.LessOrEqual(_moverGo.transform.position.z, 5.001f, "Z 모서리 클램프");
        }
    }
}
