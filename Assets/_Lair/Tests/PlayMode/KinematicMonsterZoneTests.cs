using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    //# 방식 B 회귀 박제 — 몬스터 Rigidbody kinematic 전환 후에도
    //# (1) MonsterTag.OnEnable 이 isKinematic 을 강제하고
    //# (2) kinematic 콜라이더가 BattleZone trigger 를 발화시켜 IsEngaging 전환되는지 검증.
    //# 시스템 마비 위험(교전 불가)의 핵심 리스크를 한 케이스로 압축.
    public class KinematicMonsterZoneTests
    {
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            //# 선행 Battle 씬 로드 테스트가 남긴 BattleZone trigger 제거.
            //# 물리는 로드된 씬 전역 공유라 활성씬 교체만으론 부족 — 잔존 zone 을 직접 파괴해야
            //# 본 테스트 몬스터(10,0,0)가 남의 zone 에 걸려 오염되는 것을 막는다.
            BattleZone[] residual = Object.FindObjectsByType<BattleZone>(FindObjectsSortMode.None);
            foreach (BattleZone bz in residual)
            {
                if (bz == null) continue;
                Object.DestroyImmediate(bz.gameObject);
            }
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
        }

        //# 몬스터 프리팹 구성 모사 — Dynamic Rigidbody + 비-trigger CapsuleCollider + MonsterTag.
        //# MonsterTag.OnEnable 이 kinematic 강제 + Registry 등록 시점.
        private GameObject SpawnMonster(Vector3 pos)
        {
            GameObject go = new GameObject("monster");
            go.transform.position = pos;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.isTrigger = false;
            Health hp = go.AddComponent<Health>();
            hp.SetMax(100);
            //# Registry 등록 — MonsterTargetProvider 가 평소 담당. 테스트는 직접 등록.
            CharacterRegistry.RegisterMonster(go.transform, hp);
            //# MonsterTag 는 마지막에 추가 — OnEnable 이 kinematic 강제 + Engaging 리셋.
            go.AddComponent<MonsterTag>();
            _spawned.Add(go);
            return go;
        }

        //# kinematic 몬스터 + SimpleMover/SimpleRotator 가 한 GameObject 에 공존하는 march 모사.
        //# 회전 명령이 매 프레임 들어와도 MovePosition 이동이 무효화되지 않는지 검증용.
        private GameObject SpawnMover(Vector3 pos, Vector3 target, float speed)
        {
            GameObject go = new GameObject("mover");
            go.transform.position = pos;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            SimpleMover mover = go.AddComponent<SimpleMover>();
            mover.Speed = speed;
            mover.MoveTo(target);
            go.AddComponent<SimpleRotator>();
            _spawned.Add(go);
            return go;
        }

        //# 회귀 박제 — 매 프레임 회전 명령이 들어오는 kinematic 몬스터가 정상 행군한다.
        //# 원인: SimpleRotator 가 transform.rotation 을 직접 쓰면 같은 바디의 MovePosition 을 덮어써
        //# 이동이 step 단위로 무효화(행군 freeze). MoveRotation 경로 통일로 해소.
        [UnityTest]
        public IEnumerator 매프레임_회전_중에도_kinematic_몬스터_정상_행군()
        {
            //# 영웅(타겟) 을 매 프레임 살짝 옮겨 회전 yaw 가 매 프레임 갱신되게 함 — 실제 추적 상황 모사.
            Vector3 target = new Vector3(10f, 0f, 0f);
            GameObject m = SpawnMover(Vector3.zero, target, speed: 3f);
            SimpleRotator rotator = m.GetComponent<SimpleRotator>();
            yield return new WaitForFixedUpdate();

            Vector3 startPos = m.transform.position;
            float movingTargetZ = 0f;
            for (int i = 0; i < 60; ++i)
            {
                //# 타겟이 조금씩 측면 이동 → FaceDirection 이 매 프레임 새 yaw 명령.
                movingTargetZ += 0.05f;
                Vector3 movingTarget = new Vector3(10f, 0f, movingTargetZ);
                rotator.FaceDirection(movingTarget - m.transform.position);
                m.GetComponent<SimpleMover>().MoveTo(movingTarget);
                yield return new WaitForFixedUpdate();
            }

            float advanced = m.transform.position.x - startPos.x;
            //# 60 FixedUpdate × 3 speed × 0.02 dt ≈ 3.6m 직진 기대. 회전 clobber 면 ~0.3m 로 주저앉음.
            Assert.Greater(advanced, 2.0f,
                $"매 프레임 회전 명령 중에도 MovePosition 행군이 유지되어야 한다 (전진 {advanced:F2}m).");
        }

        //# 핵심 — MonsterTag.OnEnable 이 몬스터 Rigidbody 를 kinematic 으로 강제.
        [UnityTest]
        public IEnumerator 몬스터_스폰시_Rigidbody_kinematic_으로_전환()
        {
            GameObject m = SpawnMonster(Vector3.zero);
            yield return null;

            Rigidbody rb = m.GetComponent<Rigidbody>();
            Assert.IsTrue(rb.isKinematic, "MonsterTag.OnEnable 이 isKinematic=true 로 강제해야 한다.");
        }

        //# 시스템 마비 회귀 — kinematic 콜라이더가 BattleZone(Static Trigger)에 진입 시
        //# OnTriggerEnter 발화 → IsEngaging=true 전환. 깨지면 영웅 교전 불가.
        [UnityTest]
        public IEnumerator kinematic_몬스터_zone진입시_Engaging_전환()
        {
            //# BattleZone 본체 — BoxCollider isTrigger, Rigidbody 없음 (Static Trigger).
            GameObject zoneGo = new GameObject("zone");
            BoxCollider zoneCol = zoneGo.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(4f, 4f, 4f);
            zoneGo.AddComponent<BattleZone>();
            _spawned.Add(zoneGo);

            //# zone 밖에서 스폰 — 아직 미진입.
            GameObject m = SpawnMonster(new Vector3(10f, 0f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(CharacterRegistry.Monsters.Find(e => e.Transform == m.transform).IsEngaging,
                "zone 밖 — 아직 Engaging 아님.");

            //# kinematic 바디는 MovePosition 으로 이동 — zone 중심으로 진입.
            Rigidbody rb = m.GetComponent<Rigidbody>();
            for (int i = 0; i < 20; ++i)
            {
                rb.MovePosition(Vector3.MoveTowards(m.transform.position, Vector3.zero, 2f));
                yield return new WaitForFixedUpdate();
            }

            bool engaging = CharacterRegistry.Monsters.Find(e => e.Transform == m.transform).IsEngaging;
            Assert.IsTrue(engaging,
                "kinematic 몬스터가 zone trigger 에 진입하면 OnTriggerEnter 발화 → IsEngaging=true 여야 한다.");
        }
    }
}
