using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — 4중 정합 회귀 + 신규 효과 5개 NRE 없음 + ECardCategory 잔존 0건.
    //# 검증 4중 정합:
    //#   1) ECardId enum (28개)
    //#   2) SO 파일 28장 (Multiply 포함 — 자리 보존)
    //#   3) CardPool Passive 16 + Active 12 = 28
    //#   4) cards.json 28장
    //# 추가 회귀:
    //#   - ICardEffect 구현체 25 → 28 (신규 5 효과 모두 ICardEffect 구현)
    //#   - 신규 5 효과 (WallOfWisps·MarkOfDeath·SpawnerHaste·SwarmRush·GuardianRage) 의 Apply 본문이 NRE 없음
    //#   - ECardCategory 잔존 0건 (production 코드 grep)
    public class CardRenewalConsistencyTests
    {
        //# ===== 1. 28장 카운트 4중 정합 =====

        [Test]
        public void ECardId_enum_28개_정확()
        {
            Assert.AreEqual(28, System.Enum.GetValues(typeof(ECardId)).Length,
                "ECardId enum 값 28개 (기존 25 + 신규 3)");
        }

        [Test]
        public void SO_파일_28장_Multiply_포함_보존()
        {
            //# Multiply.asset 도 자리 보존(SwarmRush 효과 재사용)이므로 SO 파일 카운트는 28.
            string[] guids = AssetDatabase.FindAssets("t:CardData",
                new[] { "Assets/_Lair/Art/Cards/Items" });
            Assert.AreEqual(28, guids.Length,
                "Items 폴더의 CardData SO 28장 (Multiply 자리 보존 포함)");
        }

        [Test]
        public void CardPool_총합_28장_정합()
        {
            CardPool passive = AssetDatabase.LoadAssetAtPath<CardPool>(
                "Assets/_Lair/Art/Cards/CardPool_Passive.asset");
            CardPool active = AssetDatabase.LoadAssetAtPath<CardPool>(
                "Assets/_Lair/Art/Cards/CardPool_Active.asset");
            Assert.IsNotNull(passive);
            Assert.IsNotNull(active);
            Assert.AreEqual(28, passive.Cards.Count + active.Cards.Count,
                "CardPool Passive 16 + Active 12 = 28");
        }

        [Test]
        public void cards_json_28장_정합()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", "Assets/_Lair/Data/Json/cards.json");
            Assert.IsTrue(File.Exists(fullPath), $"cards.json 부재: {fullPath}");
            string raw = File.ReadAllText(fullPath);
            JArray arr = JArray.Parse(raw);
            Assert.AreEqual(28, arr.Count, "cards.json 28장 정합");
        }

        //# ===== 2. 4축 색 정합 — 컨셉서·기획서·SO·json 4중 일치 (axis 키만 검증 — Hex 색은 LairCardPrefabBuilder 에서) =====

        //# 모든 SO 의 _axis 값이 0~3 사이 (Tank=0/Dps=1/Debuff=2/Swarm=3) — 잘못된 값 잔존 회귀.
        [Test]
        public void 모든_SO_axis_값_Tank_Dps_Debuff_Swarm_4개_중_하나()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData",
                new[] { "Assets/_Lair/Art/Cards/Items" });

            HashSet<EBuildAxis> validAxes = new HashSet<EBuildAxis>
            {
                EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm
            };

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.IsNotNull(card, $"SO 로드 실패: {path}");
                Assert.Contains(card.Axis, validAxes.ToList(),
                    $"{card.Id} 의 _axis 가 4축 enum 값 외: {card.Axis} ({path})");
            }
        }

        //# cards.json 의 axis 키 값도 "Tank|Dps|Debuff|Swarm" 4개 중 하나.
        [Test]
        public void cards_json_axis_값_Tank_Dps_Debuff_Swarm_4개_중_하나()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", "Assets/_Lair/Data/Json/cards.json");
            string raw = File.ReadAllText(fullPath);
            JArray arr = JArray.Parse(raw);
            HashSet<string> validAxes = new HashSet<string> { "Tank", "Dps", "Debuff", "Swarm" };

            foreach (JToken t in arr)
            {
                string axis = (string)t["axis"];
                Assert.IsNotNull(axis, $"카드 {t["id"]} 에 axis 키 누락");
                Assert.Contains(axis, validAxes.ToList(),
                    $"카드 {t["id"]} 의 axis 가 4축 외: {axis}");
            }
        }

        //# ===== 3. ICardEffect 구현체 25 → 28 회귀 =====

        //# Lair.Card.Effects/ 하위 ICardEffect 구현 클래스가 28개 (신규 5: WallOfWisps·MarkOfDeath·SpawnerHaste·SwarmRush·GuardianRage).
        [Test]
        public void ICardEffect_구현_클래스_28개_신규_5_포함()
        {
            Assembly asm = typeof(ICardEffect).Assembly;
            IEnumerable<System.Type> types = asm.GetTypes()
                .Where(t => typeof(ICardEffect).IsAssignableFrom(t) && t.IsClass && t.IsAbstract == false);
            int count = types.Count();
            Assert.AreEqual(28, count, $"ICardEffect 구현 클래스 28개 (실제: {count})");

            //# 신규 5개 확인.
            HashSet<string> names = new HashSet<string>(types.Select(t => t.Name));
            Assert.Contains("WallOfWispsEffect", names.ToList(), "WallOfWispsEffect 구현 존재");
            Assert.Contains("MarkOfDeathEffect", names.ToList(), "MarkOfDeathEffect 구현 존재");
            Assert.Contains("SpawnerHasteEffect", names.ToList(), "SpawnerHasteEffect 구현 존재");
            Assert.Contains("SwarmRushEffect", names.ToList(), "SwarmRushEffect 구현 존재");
            Assert.Contains("GuardianRageEffect", names.ToList(), "GuardianRageEffect 구현 존재");
        }

        //# ===== 4. 신규 5 효과의 Apply 본문이 NRE 없음 (sanity) =====

        [Test]
        public void 신규_5_효과_Apply_NRE_없음()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;
            try
            {
                Assert.DoesNotThrow(() => new WallOfWispsEffect().Apply(ctx), "WallOfWispsEffect.Apply NRE 없음");
                Assert.DoesNotThrow(() => new MarkOfDeathEffect().Apply(ctx), "MarkOfDeathEffect.Apply NRE 없음");
                Assert.DoesNotThrow(() => new SpawnerHasteEffect().Apply(ctx), "SpawnerHasteEffect.Apply NRE 없음");
                Assert.DoesNotThrow(() => new SwarmRushEffect().Apply(ctx), "SwarmRushEffect.Apply NRE 없음");
                Assert.DoesNotThrow(() => new GuardianRageEffect().Apply(ctx), "GuardianRageEffect.Apply NRE 없음");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# ===== 5. ECardCategory 잔존 0건 — production 코드 grep =====

        //# production 코드(Assets/_Lair/Scripts) 내에 ECardCategory 키워드가 잔존하지 않음을 보장.
        //# v0.6 마이그레이션에서 ECardCategory 완전 제거 — 잔존 시 type 동기화 깨짐.
        [Test]
        public void production_코드에_ECardCategory_잔존_0건()
        {
            string scriptsPath = Path.Combine(Application.dataPath, "_Lair", "Scripts");
            Assert.IsTrue(Directory.Exists(scriptsPath), $"Scripts 폴더 존재: {scriptsPath}");

            List<string> hits = new List<string>();
            foreach (string file in Directory.EnumerateFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                if (content.Contains("ECardCategory"))
                    hits.Add(file);
            }

            //# 발견된 파일이 있으면 친절히 출력.
            Assert.AreEqual(0, hits.Count,
                $"ECardCategory 잔존 발견: {string.Join(", ", hits)} — v0.6 에서 완전 제거되어야 함");
        }

        //# ===== 6. CardPool ref 정합 — null entry 없음 =====

        [Test]
        public void CardPool_Passive_Active_null_엔트리_없음()
        {
            CardPool passive = AssetDatabase.LoadAssetAtPath<CardPool>(
                "Assets/_Lair/Art/Cards/CardPool_Passive.asset");
            CardPool active = AssetDatabase.LoadAssetAtPath<CardPool>(
                "Assets/_Lair/Art/Cards/CardPool_Active.asset");

            foreach (CardData c in passive.Cards)
                Assert.IsNotNull(c, "Passive 풀 — null 엔트리 없음 (ref 깨진 슬롯 차단)");
            foreach (CardData c in active.Cards)
                Assert.IsNotNull(c, "Active 풀 — null 엔트리 없음");
        }

        //# 모든 28장 CardData 의 Effect 가 null 이 아니다 — SerializeReference 누락 회귀.
        [Test]
        public void 모든_CardData_Effect_null_아님()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData",
                new[] { "Assets/_Lair/Art/Cards/Items" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.IsNotNull(card.Effect,
                    $"{card.Id} ({path}) — Effect 가 null (SerializeReference 누락)");
            }
        }

        //# ===== Fake =====

        private class FakeCtx : IBattleContext
        {
            public Transform HeroTransform;

            public Transform GetHeroTransform() => HeroTransform;

            //# no-op stubs
            public float DeltaTime => 0f;
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void IncrementGlobalMonsterCap(int delta) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
        }
    }
}
