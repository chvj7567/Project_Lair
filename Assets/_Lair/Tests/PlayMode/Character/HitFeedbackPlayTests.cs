using System.Collections;
using System.Threading.Tasks;
using ChvjUnityInfra;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;
using Lair.Card;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    //# 타격 피드백 PlayMode — coroutine/Camera/실제 프리팹/CHMPool 의존 케이스.
    //# EditMode 에선 Awake/OnEnable 미호출 + 코루틴 미진행 → 아래는 실제 lifecycle 위에서 검증.
    //# S4 외곽선 명도 분기는 production 의 ApplyOutline 이 private 라 seam 이 없어,
    //# 실제 DamagePopup 프리팹(폰트 할당됨)에 Play() 호출 후 fontMaterial 의 OutlineColor 를 readback 한다.
    public class HitFeedbackPlayTests : BattlePlayTestBase
    {
        private static readonly Color OutlineDark = new Color(0.102f, 0.102f, 0.102f, 1f);  //# #1A1A1A

        private static bool ApproxColor(Color a, Color b)
            => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        private IEnumerator LoadPrefab(EVisual key, System.Action<GameObject> onLoaded)
        {
            Task<GameObject> task = CHMResource.Instance.LoadAsync<GameObject>(key);
            yield return new WaitUntil(() => task.IsCompleted);
            onLoaded(task.Result);
        }

        //# 캐릭터 프리팹(EMonster/EHero) 로드 — CHMResource.LoadAsync 는 Enum 제네릭이라 EVisual 외 키도 동일 경로.
        private IEnumerator LoadCharacterPrefab(System.Enum key, System.Action<GameObject> onLoaded)
        {
            Task<GameObject> task = CHMResource.Instance.LoadAsync<GameObject>(key);
            yield return new WaitUntil(() => task.IsCompleted);
            onLoaded(task.Result);
        }

        //# ===== S4 — 외곽선 명도 분기 (실제 프리팹 readback) =====

        //# 밝은 fill(L≥128) → 근-검정 외곽선. 영웅 흰색(#FFFFFF, L=255) 대표 케이스.
        [UnityTest]
        public IEnumerator DamagePopup_밝은fill_흰색은_근검정_외곽선()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.DamagePopup, p => prefab = p);
            Assert.IsNotNull(prefab, "DamagePopup 프리팹 로드");

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            Assert.IsNotNull(p1);
            DamagePopup popup = p1.GetComponent<DamagePopup>();
            Assert.IsNotNull(popup, "DamagePopup 컴포넌트");

            popup.Play(Vector3.zero, 12, Color.white);   //# 흰 fill → 근-검정 외곽선.

            TMP_Text text = p1.GetComponentInChildren<TMP_Text>();
            Assert.IsNotNull(text, "TMP_Text 자식 존재");
            Assert.IsNotNull(text.fontMaterial, "fontMaterial 인스턴스 (실제 프리팹은 폰트 할당)");
            Color outline = text.fontMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
            Assert.IsTrue(ApproxColor(outline, OutlineDark),
                $"흰 fill(L=255) → 근-검정 외곽선 기대, 실제 {outline}");

            CHMPool.Instance.Push(p1);
        }

        //# 어두운 fill(L<128) → 흰 외곽선. 출혈 DoT(#A01346, L≈67) 대표 케이스.
        [UnityTest]
        public IEnumerator DamagePopup_어두운fill_출혈색은_흰_외곽선()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.DamagePopup, p => prefab = p);
            Assert.IsNotNull(prefab);

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            DamagePopup popup = p1.GetComponent<DamagePopup>();
            popup.Play(Vector3.zero, 8, HitFeedbackPalette.Bleed);   //# 출혈 자홍 → 흰 외곽선.

            TMP_Text text = p1.GetComponentInChildren<TMP_Text>();
            Color outline = text.fontMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
            Assert.IsTrue(ApproxColor(outline, Color.white),
                $"출혈 fill(L≈67<128) → 흰 외곽선 기대, 실제 {outline}");

            CHMPool.Instance.Push(p1);
        }

        //# 경계 근처 — Plague #A855F7 (L≈128.3 ≥ 128) → 근-검정. 임계 위쪽 검증.
        [UnityTest]
        public IEnumerator DamagePopup_경계상단_Plague색은_근검정_외곽선()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.DamagePopup, p => prefab = p);

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            DamagePopup popup = p1.GetComponent<DamagePopup>();
            Color plague = new Color(168f / 255f, 85f / 255f, 247f / 255f, 1f);   //# #A855F7
            popup.Play(Vector3.zero, 5, plague);

            TMP_Text text = p1.GetComponentInChildren<TMP_Text>();
            Color outline = text.fontMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
            Assert.IsTrue(ApproxColor(outline, OutlineDark),
                $"Plague fill(L≈128.3≥128) → 근-검정 외곽선 기대, 실제 {outline}");

            CHMPool.Instance.Push(p1);
        }

        //# fill 의 RGB 가 그대로 글자색으로 (외곽선과 별개로 fill 보존 검증).
        [UnityTest]
        public IEnumerator DamagePopup_fill색이_글자색으로_그대로()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.DamagePopup, p => prefab = p);

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            DamagePopup popup = p1.GetComponent<DamagePopup>();
            popup.Play(Vector3.zero, 99, HitFeedbackPalette.Poison);

            TMP_Text text = p1.GetComponentInChildren<TMP_Text>();
            Assert.IsTrue(ApproxColor(text.color, HitFeedbackPalette.Poison),
                $"fill RGB 가 글자색으로 보존, 실제 {text.color}");
            Assert.AreEqual("99", text.text, "표시 텍스트 = 데미지량");

            CHMPool.Instance.Push(p1);
        }

        //# ===== DamagePopup 부상 + 자동 Push (코루틴) =====

        [UnityTest]
        public IEnumerator DamagePopup_부상_그리고_지속후_자동_풀반환()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.DamagePopup, p => prefab = p);

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            DamagePopup popup = p1.GetComponent<DamagePopup>();
            Vector3 startPos = new Vector3(0f, 1f, 0f);
            popup.Play(startPos, 10, Color.white);

            yield return null;
            yield return null;
            //# 부상 — y 가 시작보다 위로.
            Assert.Greater(p1.transform.position.y, startPos.y, "부상 — y 상승 중");

            //# _duration 0.7s + 여유 → 자동 Push(SetActive(false)) 됨.
            yield return new WaitForSeconds(0.9f);
            Assert.IsFalse(p1.gameObject.activeSelf, "지속 후 자동 풀 반환(비활성)");
        }

        //# ===== S6 — Pop 성공 but DamagePopup 컴포넌트 누락 → Push 폴백 =====
        //# HitFeedbackSpawner.SpawnPopup 은 DamagePopup 없는 풀 객체면 즉시 Push 한다.
        //# 실제 프리팹엔 컴포넌트가 있으므로, 컴포넌트 누락 분기는 가짜 프리팹으로 검증.

        [UnityTest]
        public IEnumerator HitFeedbackSpawner_DamagePopup컴포넌트_누락시_즉시_Push폴백()
        {
            yield return EnsureCHMReady();

            //# DamagePopup 없는 풀 prefab (CHPoolable 만). 고유 이름 — 다른 풀과 키 충돌 회피.
            string poolName = "fakePopup_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            GameObject fakePrefab = new GameObject(poolName);
            fakePrefab.AddComponent<CHPoolable>();
            fakePrefab.SetActive(false);   //# 템플릿 비활성 — 풀 1개 워밍 후 idle.
            CHMPool.Instance.CreatePool(fakePrefab, 1);
            Assert.AreEqual(1, CHMPool.Instance.GetPooledCount(poolName), "워밍 후 idle 1개");

            GameObject spawnerGo = new GameObject("spawner");
            HitFeedbackSpawner sp = spawnerGo.AddComponent<HitFeedbackSpawner>();
            yield return null;   //# Awake → Instance set.
            sp.Init(null, null, fakePrefab);

            //# 컴포넌트 누락 분기 — Pop(idle 0) 후 DamagePopup 없으니 즉시 Push.
            Assert.DoesNotThrow(() => sp.SpawnPopup(Vector3.zero, 10, Color.red));
            yield return null;

            //# Pop→즉시 Push 면 idle 카운트가 다시 1 로 돌아와야(누수 없이 폴백 반환).
            Assert.AreEqual(1, CHMPool.Instance.GetPooledCount(poolName),
                "DamagePopup 누락 객체가 즉시 Push 되어 idle 복귀");

            Object.DestroyImmediate(spawnerGo);
            Object.DestroyImmediate(fakePrefab);
        }

        //# ===== 회귀 — 실제 몬스터 프리팹의 대표색이 종(species) 색과 일치 =====
        //# Fake 더블만 쓰던 25개 테스트가 놓친 경로: 실제 프리팹 visual(LittleGhost 창백색)이 아니라
        //# MonsterTag.Key → SpeciesColor 로 DamageColor 가 잡혀야 한다. 수정 전엔 흰색≈창백색이라 RED.

        [UnityTest]
        public IEnumerator 실제_Wisp프리팹_대표색은_종색_22C55E()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadCharacterPrefab(EMonster.Wisp, p => prefab = p);
            Assert.IsNotNull(prefab, "Wisp 프리팹 로드");

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            Assert.IsNotNull(p1);
            yield return null;   //# Awake(CacheRepColor) + OnEnable(DamageColor 주입) 확정.

            MeleeAttacker atk = p1.GetComponent<MeleeAttacker>();
            MonsterTag tag = p1.GetComponent<MonsterTag>();
            Assert.IsNotNull(atk, "MeleeAttacker 존재");
            Assert.IsNotNull(tag, "MonsterTag 존재");
            Assert.IsTrue(ApproxColor(atk.DamageColor, SpawnerStatusCell.SpeciesColor(tag.Key)),
                $"Wisp 대표색 = 종색 {SpawnerStatusCell.SpeciesColor(EMonster.Wisp)}, 실제 {atk.DamageColor}");

            CHMPool.Instance.Push(p1);
        }

        //# 영웅(Knight) 은 _heroWhiteDamageColor 오버라이드 → 흰색 유지. 오버라이드 회귀 방지.
        [UnityTest]
        public IEnumerator 실제_Knight프리팹_대표색은_흰색_오버라이드()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadCharacterPrefab(EHero.Knight, p => prefab = p);
            Assert.IsNotNull(prefab, "Knight 프리팹 로드");

            CHPoolable p1 = CHMPool.Instance.Pop(prefab, null);
            Assert.IsNotNull(p1);
            yield return null;

            MeleeAttacker atk = p1.GetComponent<MeleeAttacker>();
            Assert.IsNotNull(atk, "MeleeAttacker 존재");
            Assert.IsTrue(ApproxColor(atk.DamageColor, Color.white),
                $"Knight 대표색 = 흰색 오버라이드, 실제 {atk.DamageColor}");

            CHMPool.Instance.Push(p1);
        }

        //# ===== AttackJuice 펀치 (코루틴 peak) =====

        [UnityTest]
        public IEnumerator AttackJuice_OnHit시_스케일_펀치후_원복()
        {
            GameObject go = new GameObject("attacker");
            go.transform.localScale = Vector3.one;
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(range: 5f, cooldown: 0f, power: 1);
            go.AddComponent<AttackJuice>();
            yield return null;   //# Awake + OnEnable (실제 lifecycle).

            GameObject tgtGo = new GameObject("target");
            Health hp = tgtGo.AddComponent<Health>();
            yield return null;
            hp.SetMax(100);

            atk.TryAttack(hp, Vector3.zero, Vector3.zero, now: 1f);   //# OnHit → 펀치 시작.

            //# 펀치 도중 — baseScale 보다 커짐.
            yield return null;
            yield return null;
            Assert.Greater(go.transform.localScale.x, 1f, "펀치 도중 스케일 확대");

            //# 0.12s 지속 + 여유 → 원복.
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1f, go.transform.localScale.x, 0.001f, "펀치 후 baseScale 원복(잔류 0)");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tgtGo);
        }

        //# ===== S7 회귀 — HitFlash 피격 색 반전 동작 불변 =====

        [UnityTest]
        public IEnumerator HitFlash_피격시_색반전_그리고_원복_회귀()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();

            GameObject body = new GameObject("Body");
            body.transform.SetParent(go.transform);
            MeshRenderer rd = body.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color original = new Color(0.8f, 0.2f, 0.2f, 1f);
            mat.color = original;
            rd.sharedMaterial = mat;

            go.AddComponent<HitFlash>();
            yield return null;   //# Awake(CacheRenderers — .material 인스턴스화) + OnEnable.
            yield return null;   //# Start → _lastHp 캐시.
            hp.SetMax(100);

            //# 인스턴스 머티리얼 참조 (HitFlash 가 .material 로 인스턴스화한 것).
            Material inst = rd.material;
            hp.TakeDamage(20);   //# OnChanged 델타<0 → 반전 플래시 시작.
            yield return null;

            //# 반전 중 — 원색과 달라야 (R 성분이 반전되어 낮아짐).
            Color flashing = inst.color;
            Assert.IsTrue(flashing.r < original.r + 0.01f && Mathf.Abs(flashing.r - original.r) > 0.05f,
                $"피격 중 색 반전 (원색과 다름), 원={original} 현재={flashing}");

            //# _duration 0.1s + 여유 → 원복.
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(ApproxColor(inst.color, original),
                $"피격 flash 후 원색 복원, 실제 {inst.color}");

            Object.DestroyImmediate(go);
        }

        //# ===== S7 회귀 — HitFlash 회복(증가)은 플래시 안 함 =====

        [UnityTest]
        public IEnumerator HitFlash_회복시_색반전_안함_회귀()
        {
            GameObject go = new GameObject("victim");
            Health hp = go.AddComponent<Health>();

            GameObject body = new GameObject("Body");
            body.transform.SetParent(go.transform);
            MeshRenderer rd = body.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color original = new Color(0.3f, 0.6f, 0.9f, 1f);
            mat.color = original;
            rd.sharedMaterial = mat;

            go.AddComponent<HitFlash>();
            yield return null;
            yield return null;
            hp.SetMax(100);
            hp.TakeDamage(50);   //# 100→50, _lastHp=50.
            yield return new WaitForSeconds(0.2f);   //# 피격 flash 종료 대기.

            Material inst = rd.material;
            Color before = inst.color;
            hp.Heal(20);   //# 50→70 — 회복은 무시.
            yield return null;

            Assert.IsTrue(ApproxColor(inst.color, before),
                "회복(증가)은 색 반전 없음 — 회귀 불변");

            Object.DestroyImmediate(go);
        }
    }
}
