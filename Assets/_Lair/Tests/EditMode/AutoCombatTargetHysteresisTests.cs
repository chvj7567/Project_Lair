using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.EditMode
{
    //# 타겟 히스테리시스 본격 스위트 — ResolveStickyTarget 분기 망라(수용기준 1·2·3·6 의 _currentTarget 측면).
    //# ResolveStickyTarget 은 private 이라 리플렉션으로 직접 호출(영웅 한정 경로). 시간/Update 의존 없는 순수 결정 로직.
    //# 주의 — 교체 조건은 strict `>` (curDist - candDist > _retargetMargin). spec 산문은 "이상"이지만 구현=plan=strict.
    //#   따라서 diff == margin 은 미교체(이 경계 테스트가 `>` 와 `>=` 를 가르는 유일한 판별점).
    //# ResolveStickyTarget 은 _targetHysteresisEnabled OFF 일 때 호출되지 않으므로(=수용기준 4 는 Update 경로),
    //#   여기선 OFF 경로를 다루지 않는다 — 몬스터 OFF 불변은 PlayMode 회귀(AutoCombatAIRotationTests 등)가 담당.
    public class AutoCombatTargetHysteresisTests
    {
        private static readonly MethodInfo ResolveSticky = typeof(AutoCombatAI).GetMethod(
            "ResolveStickyTarget", BindingFlags.NonPublic | BindingFlags.Instance);

        private AutoCombatAI NewAi(out GameObject root)
        {
            root = new GameObject("ai");
            root.transform.position = Vector3.zero;
            //# RequireComponent 의존 컴포넌트를 함께 부착(Awake 통과용).
            root.AddComponent<SimpleMover>();
            root.AddComponent<Health>();
            root.AddComponent<MeleeAttacker>();
            root.AddComponent<SimpleRotator>();
            AutoCombatAI ai = root.AddComponent<AutoCombatAI>();
            TestReflection.SetField(ai, "_targetHysteresisEnabled", true);
            TestReflection.SetField(ai, "_retargetMargin", 1.5f);
            return ai;
        }

        private Transform MakeTargetAt(Vector3 pos)
        {
            GameObject go = new GameObject("target");
            go.transform.position = pos;
            return go.transform;
        }

        private Transform Invoke(AutoCombatAI ai, Transform candidate, IHealth candidateH, out IHealth resolved)
        {
            object[] args = { candidate, candidateH, null };
            Transform result = (Transform)ResolveSticky.Invoke(ai, args);
            resolved = (IHealth)args[2];
            return result;
        }

        private static T ReadField<T>(AutoCombatAI ai, string name)
        {
            FieldInfo f = typeof(AutoCombatAI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{name} 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            return (T)f.GetValue(ai);
        }

        //# ===== 수용기준 1 — 등거리 2후보가 매 프레임 교차로 들어와도 유효 타겟이 안 뒤집힌다 =====

        //# 핵심 — 같은 후보를 반복 전달하면 candidate==_currentTarget 가드로 trivially stable(vacuous).
        //# 따라서 A↔B 를 번갈아 전달(둘 다 등거리)해야 실제 깜빡임 흡수를 검증한다.
        [Test]
        public void 등거리_두후보_교차입력에도_유효타겟이_안뒤집힌다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            //# A(+X 5.0) 와 B(+Z 5.0) — self=원점 기준 정확히 등거리(5.0).
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Transform b = MakeTargetAt(new Vector3(0f, 0f, 5f));

            //# 첫 호출에서 A 채택(초기 무효 → candidate).
            Transform first = Invoke(ai, a, new FakeHealth(), out _);
            Assert.AreSame(a, first, "첫 프레임 — A 채택");

            //# A↔B 를 번갈아 6프레임. 등거리(diff=0 < margin)라 A 가 계속 유지돼야.
            for (int i = 0; i < 6; ++i)
            {
                Transform cand = (i % 2 == 0) ? b : a;
                Transform result = Invoke(ai, cand, new FakeHealth(), out _);
                Assert.AreSame(a, result, $"프레임 {i} — 등거리 후보 교차에도 A 유지(깜빡임 흡수)");
            }

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# ===== 수용기준 2 — margin 경계 (strict `>`) =====

        //# diff > margin (교체) — curDist(5.0) - candDist(3.0) = 2.0 > 1.5 → 교체.
        [Test]
        public void 후보가_margin_초과로_가까우면_교체한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);
            Transform b = MakeTargetAt(new Vector3(3f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(b, result, "diff=2.0 > margin=1.5 → B 로 교체");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# diff == margin (미교체) — strict `>` 와 `>=` 를 가르는 유일한 판별점.
        //# 축정렬 정확한 float 거리로 구성 — curDist(5.0) - candDist(3.5) = 1.5f 정확. 1.5 > 1.5 = false → 유지.
        //# sqrt 라운딩이 끼는 비축정렬 좌표를 쓰면 경계가 흔들리므로 반드시 축정렬.
        [Test]
        public void 후보거리차가_margin과_정확히_같으면_미교체_strict()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            //# A dist 5.0 으로 고정.
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);
            //# B dist 3.5 → 5.0f - 3.5f == 1.5f 정확 → 1.5 > 1.5 false → 미교체.
            Transform b = MakeTargetAt(new Vector3(3.5f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(a, result,
                "diff == margin(1.5) → strict `>` 라 미교체. 구현=plan=strict (spec 산문 '이상' 과 다름)");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# diff < margin (미교체) — 거의 등거리. 깜빡임 흡수.
        [Test]
        public void 후보가_margin_미만으로만_가까우면_유지한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);
            //# B dist 4.0 → diff 1.0 < margin 1.5 → 유지.
            Transform b = MakeTargetAt(new Vector3(4f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(a, result, "diff=1.0 < margin=1.5 → A 유지");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# 후보가 현재보다 더 멀어도(diff 음수) 당연히 미교체 — 부호 가드.
        [Test]
        public void 후보가_현재보다_멀면_유지한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(3f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);
            //# B dist 6.0 → diff = 3 - 6 = -3 < margin → 유지.
            Transform b = MakeTargetAt(new Vector3(6f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(a, result, "후보가 더 멀면(diff 음수) 유지");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# ===== 수용기준 3 — 현재 타겟 무효 4조건 각각 즉시 candidate 채택 =====

        //# 무효 ① — _currentTarget == null (최초 호출, 미고정 상태) → candidate 즉시 채택.
        [Test]
        public void 현재타겟_null이면_후보를_즉시채택한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform c = MakeTargetAt(new Vector3(9f, 0f, 0f));
            FakeHealth ch = new FakeHealth();
            Transform result = Invoke(ai, c, ch, out IHealth resolved);

            Assert.AreSame(c, result, "최초(_currentTarget=null) → 후보 즉시 채택");
            Assert.AreSame(ch, resolved, "채택 시 후보 Health 도 함께 저장");

            Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }

        //# 무효 ② — 현재 타겟 IsAlive == false (사망) → candidate 즉시 채택.
        [Test]
        public void 현재타겟_사망이면_후보를_즉시채택한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            FakeHealth ah = new FakeHealth();
            Invoke(ai, a, ah, out _);

            //# A 사망 처리. 더 가까운 교체 조건이 아니어도(등거리여도) 사망이라 즉시 교체돼야.
            ah.ForceSetCurrent(0);
            Transform b = MakeTargetAt(new Vector3(5f, 0f, 0f));   //# 등거리(diff=0) — margin 으로는 교체 안 될 위치.
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(b, result, "현재 타겟 사망(IsAlive=false) → margin 무관 즉시 candidate 채택");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# 무효 ③ — 현재 타겟 GameObject 비활성(소멸/풀 Push 모사) → candidate 즉시 채택.
        [Test]
        public void 현재타겟_비활성이면_후보를_즉시채택한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);

            //# A 비활성 — activeInHierarchy=false. 등거리 후보여도 즉시 교체.
            a.gameObject.SetActive(false);
            Transform b = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);

            Assert.AreSame(b, result, "현재 타겟 비활성(activeInHierarchy=false) → 즉시 candidate 채택");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# 무효 ④ — _currentTargetHealth == null. 자연 경로론 도달 불가(채택 시 항상 target+health 동시 저장)라
        //# 리플렉션으로 강제 null 주입 후, 그 가드가 실제로 무효 처리하는지 검증.
        [Test]
        public void 현재타겟_Health가_null이면_후보를_즉시채택한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);

            //# Health 만 강제 null — target 은 살아있고 활성. Health-null 가드가 없으면 잘못 유지될 케이스.
            TestReflection.SetField(ai, "_currentTargetHealth", null);
            Transform b = MakeTargetAt(new Vector3(5f, 0f, 0f));
            FakeHealth bh = new FakeHealth();
            Transform result = Invoke(ai, b, bh, out IHealth resolved);

            Assert.AreSame(b, result, "_currentTargetHealth=null → 무효로 보고 즉시 candidate 채택");
            Assert.AreSame(bh, resolved, "채택 후 Health 도 후보 것으로 갱신");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# 채택/교체 시 resolvedHealth out 이 _currentTargetHealth 와 동기되는지 — 회전/공격이 같은 타겟 Health 를 쓰는 계약.
        [Test]
        public void 교체시_resolvedHealth가_새타겟_Health로_갱신된다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            FakeHealth ah = new FakeHealth();
            Invoke(ai, a, ah, out IHealth resolvedA);
            Assert.AreSame(ah, resolvedA, "고정 직후 resolved=A Health");

            //# B 가 margin 초과로 교체 → resolved 도 B Health.
            Transform b = MakeTargetAt(new Vector3(3f, 0f, 0f));
            FakeHealth bh = new FakeHealth();
            Invoke(ai, b, bh, out IHealth resolvedB);
            Assert.AreSame(bh, resolvedB, "교체 후 resolved=B Health");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }

        //# ===== 수용기준 6 — OnEnable(풀 재사용) 후 _currentTarget / _currentTargetHealth 잔존 없음 =====

        //# private OnEnable 을 리플렉션 호출(SetActive 사이클보다 견고 — provider 재등록 부작용 회피).
        [Test]
        public void OnEnable후_고정타겟_상태가_리셋된다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);
            Assert.IsNotNull(ReadField<Transform>(ai, "_currentTarget"), "사전: 타겟 고정 성립");

            //# 풀 재사용 모사 — OnEnable 직접 호출.
            MethodInfo onEnable = typeof(AutoCombatAI).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onEnable, "OnEnable 리플렉션 실패 — production 시그니처 변경 의심.");
            onEnable.Invoke(ai, null);

            Assert.IsNull(ReadField<Transform>(ai, "_currentTarget"),
                "OnEnable 후 _currentTarget 잔존 없음(풀 재사용 격리)");
            Assert.IsNull(ReadField<IHealth>(ai, "_currentTargetHealth"),
                "OnEnable 후 _currentTargetHealth 잔존 없음");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(root);
        }

        //# OnEnable 후 다음 ResolveStickyTarget 호출이 깨끗이 새 후보를 채택하는지(리셋의 관찰 가능한 증거).
        [Test]
        public void OnEnable후_다음후보를_새로_채택한다()
        {
            AutoCombatAI ai = NewAi(out GameObject root);
            Transform a = MakeTargetAt(new Vector3(5f, 0f, 0f));
            Invoke(ai, a, new FakeHealth(), out _);

            MethodInfo onEnable = typeof(AutoCombatAI).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable.Invoke(ai, null);

            //# A 보다 가깝지 않은(등거리 이상) B 라도 — _currentTarget=null 이라 즉시 채택돼야.
            Transform b = MakeTargetAt(new Vector3(8f, 0f, 0f));
            Transform result = Invoke(ai, b, new FakeHealth(), out _);
            Assert.AreSame(b, result, "리셋 후 첫 후보를 무조건 채택(이전 A 미잔존)");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
