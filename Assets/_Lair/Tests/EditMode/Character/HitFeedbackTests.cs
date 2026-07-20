using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# ─────────────────────────────────────────────────────────────
    //# 본격 스위트 보강분(test-engineer) 은 partial 의 다른 영역(아래)에 둔다.
    //# 자체 테스트 4건(정상 + 엣지 1) 위에 엣지 망라·회귀·DoT 3경로·풀 리셋을 쌓는다.
    //# 더블(FakeTarget/FakeSpawner)·Invoke 헬퍼는 본 클래스 것을 그대로 재사용(부분 더블 금지).
    //# ─────────────────────────────────────────────────────────────
    //# 타격 피드백 자체 테스트 — 정상 케이스 + 엣지 1개 수준.
    //# 본격 엣지·회귀·통합은 test-engineer 단계.
    public class HitFeedbackTests
    {
        //# EditMode 는 AddComponent/SetActive 가 Awake/OnEnable 을 호출하지 않는다.
        //# 구현 코드의 lifecycle 배선(_health 캐시·구독·대표색 주입)을 테스트에서 명시 구동.
        private static void Invoke(MonoBehaviour mb, string method)
        {
            MethodInfo m = mb.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(m, $"{mb.GetType().Name}.{method} 미발견 — 시그니처 변경?");
            m.Invoke(mb, null);
        }
        //# IHealth + IDamageColorSink 더블 (real 인터페이스 시그니처 기준).
        private class FakeTarget : MonoBehaviour, IHealth, IDamageColorSink
        {
            public int Max => 100;
            public int Current { get; private set; } = 100;
            public float Ratio => Current / 100f;
            public bool IsAlive => Current > 0;
            public Color StampedColor = Color.clear;
            public int StampOrderHp = -1;   //# 스탬프 시점의 Current (TakeDamage 前인지 검증)

            public event Action<int, int> OnChanged;
            public event Action OnDied;

            public void TakeDamage(int amount)
            {
                Current -= amount;
                OnChanged?.Invoke(Current, Max);
                if (Current <= 0) OnDied?.Invoke();
            }

            public void Heal(int amount) { }
            public void SetMax(int max, bool resetCurrent = true) { }
            public void StampDamageColor(Color c) { StampedColor = c; StampOrderHp = Current; }
        }

        //# IHitFeedbackSpawner 스파이. 팝업/임팩트 스폰 위치도 기록(상단 Y 검증용).
        private class FakeSpawner : IHitFeedbackSpawner
        {
            public int ImpactCount;
            public int PopupCount;
            public int LastAmount;
            public Color LastColor;
            public Vector3 LastPopupPos;
            public Vector3 LastImpactPos;
            public HitVictimKind LastVictim;

            public void SpawnImpact(Vector3 pos, Color color, HitVictimKind victim)
            {
                ImpactCount++;
                LastColor = color;
                LastImpactPos = pos;
                LastVictim = victim;
            }

            public void SpawnPopup(Vector3 pos, int amount, Color color)
            {
                PopupCount++;
                LastAmount = amount;
                LastColor = color;
                LastPopupPos = pos;
            }
        }

        [Test]
        public void 근접공격_적중시_TakeDamage_직전_대표색_스탬프()
        {
            GameObject go = new GameObject("attacker");
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(range: 5f, cooldown: 0f, power: 10);
            atk.DamageColor = Color.red;

            GameObject tgtGo = new GameObject("target");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();
            tgtGo.AddComponent<LairCharacter>();   //# 색 스탬프가 대상 Character 로케이터 경유 → 부착 필요.

            bool hit = atk.TryAttack(tgt, Vector3.zero, Vector3.zero, now: 1f);

            Assert.IsTrue(hit);
            Assert.AreEqual(Color.red, tgt.StampedColor);
            //# 스탬프가 데미지 적용 전(Current=100)에 찍혀야 함 — OnHit 은 TakeDamage 後라 부적합.
            Assert.AreEqual(100, tgt.StampOrderHp);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        [Test]
        public void 데미지_입으면_델타만큼_팝업_요청_회복은_무시()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            //# SetMax 로 Current=100 강제 (Awake 미실행 대체). OnEnable 이 _lastHp=100 을 캐시하기 전에 선행.
            hp.SetMax(100);
            Invoke(fb, "Awake");    //# _health 캐시
            Invoke(fb, "OnEnable"); //# OnChanged 구독 + _lastHp=100 캐시
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            fb.StampDamageColor(Color.white);   //# 전투 데미지원 모사 — TakeDamage 직전 스탬프.
            hp.TakeDamage(30);
            Assert.AreEqual(1, spy.PopupCount);
            Assert.AreEqual(1, spy.ImpactCount);
            Assert.AreEqual(30, spy.LastAmount);

            hp.Heal(10);          //# 회복은 팝업 없음 (엣지)
            Assert.AreEqual(1, spy.PopupCount);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void 피격자_스탬프색이_팝업색으로_전달된다()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");   //# 여기서 _nextColor=white 리셋 → 스탬프는 반드시 이후에.
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            fb.StampDamageColor(HitFeedbackPalette.Poison);   //# DoT 출처가 스탬프
            hp.TakeDamage(5);

            Assert.AreEqual(HitFeedbackPalette.Poison, spy.LastColor);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_시_몬스터_몸체색을_DamageColor_로_주입()
        {
            GameObject go = new GameObject("attacker");
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            AttackJuice juice = go.AddComponent<AttackJuice>();

            //# _attacker 캐시(Awake) 전에 대표색 설정 — _attacker==null 이라 _repColor 만 세팅(DamageColor 직접 주입 회피).
            //# 그래야 아래 OnEnable 경로가 DamageColor 를 주입하는지 진짜 검증된다(직접 set false-pass 회피).
            juice.SetRepresentativeColorForTest(Color.green);
            Invoke(juice, "Awake");     //# _attacker 캐시 + CacheRepColor (이미 cached 라 early-return)
            Invoke(juice, "OnEnable");  //# OnEnable → _attacker.DamageColor = _repColor 주입

            Assert.AreEqual(Color.green, atk.DamageColor);
            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== test-engineer 보강 — 공용 헬퍼/더블 =====

        //# IMover 더블 — BleedAura/EternalBleedAura 는 IsMoving 만 본다(나머지 no-op).
        private class FakeMover : IMover
        {
            public float Speed { get; set; }
            public bool IsMoving { get; set; } = true;
            public void MoveTo(Vector3 target) { }
            public void Stop() { }
        }

        //# 리플렉션 — private/[NonSerialized] 필드 주입 (Poison 의 _visualPoolable/_heroTransform 등).
        private static void SetField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} 미발견 — 시그니처 변경?");
            f.SetValue(target, value);
        }

        //# ===== S2 — DoT 3경로 색 스탬프 (TakeDamage 직전) =====

        [Test]
        public void BleedAura_틱_시_출혈색_스탬프_데미지직전_그리고_델타정확()
        {
            GameObject tgtGo = new GameObject("hero");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();   //# Max=100
            tgtGo.AddComponent<LairCharacter>();      //# DoT 색 스탬프가 대상 Character 로케이터 경유 → 부착 필요.

            BleedAura aura = new BleedAura(ratio: 0.02f);  //# 2%/s → 100×0.02=2
            aura.OnAttached(tgt);

            aura.Tick(tgt, 1f);   //# 1초 누적 → 정확히 1틱

            Assert.AreEqual(HitFeedbackPalette.Bleed, tgt.StampedColor, "출혈 DoT 는 Bleed(#A01346) 스탬프");
            //# 스탬프가 TakeDamage 직전(Current 아직 100) — DoT 도 직격과 동일 순서 계약.
            Assert.AreEqual(100, tgt.StampOrderHp, "스탬프는 TakeDamage 직전이어야 함");
            //# 회귀 박제 — 스탬프 추가가 Bleed 데미지 수치에 영향 0 (RoundToInt(100×0.02)=2).
            Assert.AreEqual(98, tgt.Current, "Bleed 틱 데미지 = RoundToInt(Max×ratio) 불변");

            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        [Test]
        public void EternalBleedAura_틱_시_Bleed색_일관_그리고_델타정확()
        {
            GameObject tgtGo = new GameObject("hero");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();
            tgtGo.AddComponent<LairCharacter>();      //# DoT 색 스탬프가 대상 Character 로케이터 경유 → 부착 필요.

            FakeMover mover = new FakeMover { IsMoving = true };
            EternalBleedAura aura = new EternalBleedAura(mover, ratio: 0.01f);  //# 1%/s → 1
            aura.OnAttached(tgt);

            aura.Tick(tgt, 1f);

            //# EternalBleed = Bleed 변형 → 동일 자홍색(기획서 §1.6 일관성).
            Assert.AreEqual(HitFeedbackPalette.Bleed, tgt.StampedColor, "EternalBleed 도 Bleed 색 일관");
            Assert.AreEqual(100, tgt.StampOrderHp);
            Assert.AreEqual(99, tgt.Current, "EternalBleed 틱 = RoundToInt(100×0.01)=1");

            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        [Test]
        public void PoisonAura_틱_시_독색_스탬프_데미지직전_그리고_델타정확()
        {
            GameObject tgtGo = new GameObject("hero");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();
            tgtGo.AddComponent<LairCharacter>();      //# DoT 색 스탬프가 대상 Character 로케이터 경유 → 부착 필요.
            tgtGo.transform.position = Vector3.zero;

            //# 독장판 visual 의 CHPoolable 더미 — Push 안 부르고 Tick 의 영역판정용 위치만 필요.
            GameObject diskGo = new GameObject("poisonDisk");
            ChvjUnityInfra.CHPoolable disk = diskGo.AddComponent<ChvjUnityInfra.CHPoolable>();
            diskGo.transform.position = Vector3.zero;   //# 영웅과 동일 위치 → 반경 1.25 내

            PoisonAura aura = new PoisonAura(dps: 5f, radius: 1.25f);
            //# CHMResource/CHMPool 우회 — Tick 가 보는 두 private 필드를 직접 주입.
            SetField(aura, "_heroTransform", tgtGo.transform);
            SetField(aura, "_visualPoolable", disk);

            aura.Tick(tgt, 1f);   //# 영역 내 + 1초 → 1틱

            Assert.AreEqual(HitFeedbackPalette.Poison, tgt.StampedColor, "독 DoT 는 Poison(#0B5B4A) 스탬프");
            Assert.AreEqual(100, tgt.StampOrderHp, "스탬프는 TakeDamage 직전");
            Assert.AreEqual(95, tgt.Current, "Poison 틱 = (int)dps = 5 불변");

            UnityEngine.Object.DestroyImmediate(diskGo);
            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        [Test]
        public void PoisonAura_영역밖이면_틱없음_스탬프없음()
        {
            GameObject tgtGo = new GameObject("hero");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();
            tgtGo.transform.position = new Vector3(10f, 0f, 0f);   //# 반경 밖

            GameObject diskGo = new GameObject("poisonDisk");
            ChvjUnityInfra.CHPoolable disk = diskGo.AddComponent<ChvjUnityInfra.CHPoolable>();
            diskGo.transform.position = Vector3.zero;

            PoisonAura aura = new PoisonAura(dps: 5f, radius: 1.25f);
            SetField(aura, "_heroTransform", tgtGo.transform);
            SetField(aura, "_visualPoolable", disk);

            aura.Tick(tgt, 1f);

            Assert.AreEqual(Color.clear, tgt.StampedColor, "영역 밖 — 스탬프 없음");
            Assert.AreEqual(100, tgt.Current, "영역 밖 — 데미지 없음");

            UnityEngine.Object.DestroyImmediate(diskGo);
            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        //# ===== S3 — DamageFeedback 델타 정확성 / 회복 무시 / 폴백색 =====

        [Test]
        public void 다중타격_누적시_각_델타가_정확히_팝업으로()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            //# 스탬프는 1회 소비 — 매 전투 타격마다 직전 스탬프 필요.
            fb.StampDamageColor(Color.white);
            hp.TakeDamage(30);
            Assert.AreEqual(30, spy.LastAmount);
            fb.StampDamageColor(Color.white);
            hp.TakeDamage(25);
            Assert.AreEqual(25, spy.LastAmount, "두 번째 델타 = 25 (누적 55 아님)");
            fb.StampDamageColor(Color.white);
            hp.TakeDamage(1);
            Assert.AreEqual(1, spy.LastAmount, "경계 — 최소 델타 1");

            Assert.AreEqual(3, spy.PopupCount);
            Assert.AreEqual(3, spy.ImpactCount, "임팩트와 팝업 1:1 동시 스폰");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void 회복후_다시_피격시_델타는_회복전기준_아님()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            fb.StampDamageColor(Color.white);
            hp.TakeDamage(40);   //# 100→60
            Assert.AreEqual(1, spy.PopupCount);
            hp.Heal(20);          //# 60→80 — 무시, _lastHp=80 갱신
            Assert.AreEqual(1, spy.PopupCount, "회복은 팝업 없음");
            fb.StampDamageColor(Color.white);
            hp.TakeDamage(10);    //# 80→70 → 델타 10 (회복 반영된 80 기준)
            Assert.AreEqual(2, spy.PopupCount);
            Assert.AreEqual(10, spy.LastAmount, "델타는 회복 후 _lastHp(80) 기준");

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# (repurposed) 구버전은 미스탬프 피격이 흰색 팝업을 띄우는 "버그 동작" 을 박제했다.
        //# 수정 후 — 스탬프 없는 감소는 전투 데미지가 아니므로 팝업/임팩트 0회 (중앙 흰 숫자 버그 박제).
        [Test]
        public void 미스탬프_상태_피격시_팝업_임팩트_0회()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");   //# 스탬프 없이 곧장 피격 — 비전투 감소로 취급돼야.
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            hp.TakeDamage(7);   //# 스탬프 없는 감소 — spawn 안 함.

            Assert.AreEqual(0, spy.PopupCount, "미스탬프 감소는 전투 데미지 아님 — 팝업 0");
            Assert.AreEqual(0, spy.ImpactCount, "미스탬프 감소는 임팩트도 0");

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== S5 — 풀 재사용 리셋 / 구독 누수 =====

        [Test]
        public void DamageFeedback_OnDisable후_피격은_스폰안함_구독누수없음()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            Invoke(fb, "OnDisable");   //# OnChanged 구독 해제 — 누수 없으면 이후 피격에 무반응.
            hp.TakeDamage(20);

            Assert.AreEqual(0, spy.PopupCount, "OnDisable 후 피격은 스폰 안 함 (구독 누수 없음)");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DamageFeedback_풀재사용_OnEnable_재구독_그리고_lastHp_재캐시()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            Invoke(fb, "OnDisable");
            //# 풀 재사용 — Health 가 다시 Current 복원됐다 가정 후 OnEnable.
            hp.SetMax(100);
            Invoke(fb, "OnEnable");   //# 재구독 + _lastHp = 100 재캐시 + _hasFreshStamp = false.

            fb.StampDamageColor(Color.white);   //# 재사용 후 전투 타격 모사.
            hp.TakeDamage(15);
            Assert.AreEqual(1, spy.PopupCount, "재사용 후 재구독되어 정상 스폰");
            Assert.AreEqual(15, spy.LastAmount, "재캐시된 _lastHp 기준 정확한 델타");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void AttackJuice_OnDisable_OnEnable_스케일_baseScale로_리셋()
        {
            GameObject go = new GameObject("attacker");
            go.AddComponent<MeleeAttacker>();
            AttackJuice juice = go.AddComponent<AttackJuice>();
            go.transform.localScale = Vector3.one;

            Invoke(juice, "Awake");      //# _baseScale = (1,1,1) 캐시.
            go.transform.localScale = new Vector3(2f, 2f, 2f);   //# 펀치 잔류 가정.

            Invoke(juice, "OnDisable");
            Assert.AreEqual(Vector3.one, go.transform.localScale, "OnDisable 시 baseScale 복원");

            go.transform.localScale = new Vector3(3f, 3f, 3f);
            Invoke(juice, "OnEnable");
            Assert.AreEqual(Vector3.one, go.transform.localScale, "OnEnable 시 baseScale 복원(누수 방지)");

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== S1 — 직격 대표색: 영웅 흰색 오버라이드 / 몬스터 몸체색 =====

        [Test]
        public void 영웅_AttackJuice_흰색오버라이드시_DamageColor_흰색()
        {
            GameObject go = new GameObject("hero");
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            AttackJuice juice = go.AddComponent<AttackJuice>();

            //# 빌더가 켜는 영웅 한정 플래그(§4.3) 를 직접 set — _repColorCached 캐시 전에.
            SetField(juice, "_heroWhiteDamageColor", true);
            Invoke(juice, "Awake");      //# CacheRepColor → 흰색 분기.
            Invoke(juice, "OnEnable");   //# _attacker.DamageColor = white 주입.

            Assert.AreEqual(Color.white, atk.DamageColor, "영웅 직격 숫자색 = 흰색 오버라이드(§4.3)");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void 몬스터_AttackJuice_몸체색을_DamageColor로_읽음()
        {
            GameObject go = new GameObject("monster");
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();

            //# 자식 Renderer 에 몸체색 부여 — _BaseColor 없는 Sprites/Default 로 m.color 폴백 경로 사용.
            GameObject body = new GameObject("Body");
            body.transform.SetParent(go.transform);
            MeshRenderer rd = body.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.13f, 0.16f, 0.22f, 1f);   //# Phantom #1F2937 류.
            rd.sharedMaterial = mat;

            AttackJuice juice = go.AddComponent<AttackJuice>();
            Invoke(juice, "Awake");      //# CacheRepColor → 첫 비제외 Renderer 의 색.
            Invoke(juice, "OnEnable");

            Assert.AreEqual(mat.color, atk.DamageColor, "몬스터는 몸체색을 DamageColor 로 읽음");

            UnityEngine.Object.DestroyImmediate(mat);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void AttackJuice_Aura_HpBar_Renderer는_몸체색_후보에서_제외()
        {
            GameObject go = new GameObject("monster");
            go.AddComponent<MeleeAttacker>();

            //# 첫 자식이 Aura(제외 대상) — 이 색을 읽으면 안 됨.
            GameObject aura = new GameObject("Aura");
            aura.transform.SetParent(go.transform);
            MeshRenderer auraRd = aura.AddComponent<MeshRenderer>();
            Material auraMat = new Material(Shader.Find("Sprites/Default"));
            auraMat.color = Color.green;
            auraRd.sharedMaterial = auraMat;

            //# 두 번째 자식이 진짜 몸체.
            GameObject body = new GameObject("Body");
            body.transform.SetParent(go.transform);
            MeshRenderer bodyRd = body.AddComponent<MeshRenderer>();
            Material bodyMat = new Material(Shader.Find("Sprites/Default"));
            bodyMat.color = Color.red;
            bodyRd.sharedMaterial = bodyMat;

            AttackJuice juice = go.AddComponent<AttackJuice>();
            MeleeAttacker atk = go.GetComponent<MeleeAttacker>();
            Invoke(juice, "Awake");
            Invoke(juice, "OnEnable");

            //# Aura 가 GetComponentsInChildren 순서상 먼저 잡혀도 제외되어 Body(red) 색이 채택돼야.
            Assert.AreNotEqual((Color)Color.green, atk.DamageColor, "Aura 색은 제외되어야");
            Assert.AreEqual((Color)Color.red, atk.DamageColor, "제외 후 Body(red) 색 채택");

            UnityEngine.Object.DestroyImmediate(auraMat);
            UnityEngine.Object.DestroyImmediate(bodyMat);
            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== 회귀 — 비전투 HP 감소(SetMax)는 팝업/임팩트 0회 (중앙 흰 숫자 버그 박제) =====

        //# 풀 Pop 시 ApplyMonsterStats 가 SetMax(resetCurrent:true) 호출 → 이전 Current 보다 작은 새 max 면
        //# OnChanged 감소 델타가 발행되지만 스탬프가 없으므로 전투 데미지가 아니다. spawn 0 이어야.
        [Test]
        public void 스탬프없이_SetMax로_Current감소시_팝업_임팩트_0회_회귀()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);          //# 이전 max=100, Current=100.
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");  //# _lastHp = 100 캐시, _hasFreshStamp = false.
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            //# 풀 재사용 몬스터 스탯 적용 모사 — 스탬프 없는 비전투 감소(100→50).
            hp.SetMax(50, resetCurrent: true);

            Assert.AreEqual(0, spy.PopupCount, "SetMax 비전투 감소는 팝업 0 (중앙 흰 숫자 미발생)");
            Assert.AreEqual(0, spy.ImpactCount, "SetMax 비전투 감소는 임팩트 0");

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# 비전투 감소 직후 실제 전투 타격이 오면 — 스탬프되어 정상 팝업 + 색 전달.
        [Test]
        public void SetMax감소_후_스탬프된_전투타격은_정상_팝업_그리고_색전달()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            hp.SetMax(50, resetCurrent: true);   //# 비전투 감소 — spawn 0, _lastHp=50.
            Assert.AreEqual(0, spy.PopupCount);

            fb.StampDamageColor(HitFeedbackPalette.Poison);   //# 전투 데미지원 스탬프.
            hp.TakeDamage(10);                                //# 50→40.

            Assert.AreEqual(1, spy.PopupCount, "스탬프된 전투 타격은 정상 팝업");
            Assert.AreEqual(10, spy.LastAmount, "델타 = 10");
            Assert.AreEqual(HitFeedbackPalette.Poison, spy.LastColor, "스탬프 색 전달");

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== S6 — HitFeedbackSpawner 안전성 (prefab 미주입) =====

        [Test]
        public void HitFeedbackSpawner_prefab_미주입시_Pop안함_예외없음()
        {
            GameObject go = new GameObject("spawner");
            HitFeedbackSpawner sp = go.AddComponent<HitFeedbackSpawner>();
            //# Init 호출 안 함 → _impactPrefab / _popupPrefab == null.

            //# null 가드로 CHMPool 접근 자체가 없어야 함 — 예외 미발생이 계약.
            Assert.DoesNotThrow(() => sp.SpawnImpact(Vector3.zero, Color.red, HitVictimKind.Hero));
            Assert.DoesNotThrow(() => sp.SpawnImpact(Vector3.zero, Color.red, HitVictimKind.Monster));
            Assert.DoesNotThrow(() => sp.SpawnPopup(Vector3.zero, 10, Color.red));

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# 엣지 — 몬스터 임팩트 프리팹 미주입(null) 시 영웅 victim 도 예외 없이 폴백 경로. (자체 스모크)
        [Test]
        public void HitFeedbackSpawner_몬스터프리팹_미주입시_Monster피격_예외없이_폴백()
        {
            GameObject go = new GameObject("spawner");
            HitFeedbackSpawner sp = go.AddComponent<HitFeedbackSpawner>();
            //# 영웅 임팩트만 null, 몬스터 임팩트도 null → 둘 다 null 이라 Pop 없이 안전 리턴.
            sp.Init(null, null, null);

            Assert.DoesNotThrow(() => sp.SpawnImpact(Vector3.zero, Color.red, HitVictimKind.Monster));

            UnityEngine.Object.DestroyImmediate(go);
        }

        //# ===== S7 회귀 — 직격 스탬프가 데미지 수치에 영향 0 =====

        [Test]
        public void 직격_스탬프는_데미지수치에_영향없음_회귀()
        {
            GameObject go = new GameObject("attacker");
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(range: 5f, cooldown: 0f, power: 37);
            atk.DamageColor = Color.cyan;

            GameObject tgtGo = new GameObject("target");
            FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();   //# Current=100
            tgtGo.AddComponent<LairCharacter>();      //# 색 스탬프가 대상 Character 로케이터 경유 → 부착 필요.

            bool hit = atk.TryAttack(tgt, Vector3.zero, Vector3.zero, now: 1f);

            Assert.IsTrue(hit);
            //# 스탬프가 추가됐어도 power 만큼만 깎임 (스탬프는 데미지에 무영향).
            Assert.AreEqual(63, tgt.Current, "데미지 = power(37) 불변, 스탬프 영향 0");
            Assert.AreEqual(Color.cyan, tgt.StampedColor);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(tgtGo);
        }

        //# ===== 팝업 시작 높이 — 콜라이더 상단 Y / 폴백 =====

        [Test]
        public void 콜라이더있는엔티티_팝업은_콜라이더상단Y에서_시작()
        {
            GameObject go = new GameObject("victim");
            go.transform.position = Vector3.zero;
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.5f;
            col.height = 1.8f;   //# bounds.max.y = center.y + height/2 = 0.9 + 0.9 = 1.8
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");     //# _collider 캐시
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            fb.StampDamageColor(Color.white);
            hp.TakeDamage(10);

            Assert.AreEqual(1.8f, spy.LastPopupPos.y, 0.001f, "팝업 Y = 콜라이더 bounds.max.y");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void 콜라이더없는엔티티_팝업은_transform_position_폴백()
        {
            GameObject go = new GameObject("victim");
            go.transform.position = new Vector3(2f, 3f, 4f);   //# 콜라이더 없음.
            Health hp = go.AddComponent<Health>();
            DamageFeedback fb = go.AddComponent<DamageFeedback>();

            hp.SetMax(100);
            Invoke(fb, "Awake");
            Invoke(fb, "OnEnable");
            FakeSpawner spy = new FakeSpawner();
            fb.SetSpawnerForTest(spy);

            fb.StampDamageColor(Color.white);
            hp.TakeDamage(10);

            Assert.AreEqual(new Vector3(2f, 3f, 4f), spy.LastPopupPos, "콜라이더 없으면 transform.position 폴백");

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
