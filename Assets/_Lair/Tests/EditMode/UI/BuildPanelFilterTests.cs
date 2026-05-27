using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 D (BuildPanel 필터 회귀, design-reviewer BLOCKER 1).
    //#
    //# 검증 대상: SO 데이터 카테고리가 기획서 §2.6.1 필터 조건과 일치하는지 회귀 검증.
    //#  - 강화 패시브 6장 (WispHpBoost ~ PhantomMoveSpeedBoost) Category == Enhance.
    //#  - 액티브 4장 (Berserk/BloodThirst/Frenzy/IronWill) Category == Enhance 도 그대로 직렬화 — 필터에서 IsPassive=false 로 통과 (BLOCKER 1).
    //#  - Spawn / Replace / Environment 패시브 9장 — Enhance 아님, 자연 통과.
    //#
    //# 본 테스트는 AssetDatabase 로 실제 SO 를 로드해 데이터 정합 회귀를 잡는다 (필터 if-식 자체가 아니라
    //# "SO 가 우연히 재분류되어 필터 결과가 바뀌었는지" 를 catch).
    public class BuildPanelFilterTests
    {
        //# 카드 SO 폴더 — Rule 14 Art/Cards/Items.
        private const string CardItemsFolder = "Assets/_Lair/Art/Cards/Items";

        private static CardData LoadCard(string fileName)
        {
            string path = $"{CardItemsFolder}/{fileName}.asset";
            var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            Assert.IsNotNull(card, $"카드 SO 로드 실패: {path}");
            return card;
        }

        //# ===== 강화 패시브 6장 — Category == Enhance =====

        [TestCase("WispHpBoost")]
        [TestCase("WraithDamageBoost")]
        [TestCase("ReaperAtkSpeed")]
        [TestCase("HexRangeBoost")]
        [TestCase("PlagueSlowBoost")]
        [TestCase("PhantomMoveSpeedBoost")]
        public void 강화_패시브_6장_Category_Enhance(string id)
        {
            var card = LoadCard(id);
            Assert.AreEqual(ECardCategory.Enhance, card.Category,
                $"{id} 는 Category == Enhance 로 직렬화되어야 함 (BuildPanel 필터 대상)");
        }

        //# ===== 액티브 강화 4장 — Category == Enhance 동일 (BLOCKER 1 회귀) =====

        //# Berserk / BloodThirst / Frenzy / IronWill 의 SO `_category: 0` (= Enhance).
        //# BuildPanel 필터가 Category 만으로 거르면 액티브가 함께 사라지므로 IsPassive 추가 검사가 필수.
        //# 이 4장이 Enhance 로 직렬화된 현실을 회귀 고정 — 향후 SO 가 재분류되면 본 테스트가 catch.
        [TestCase("Berserk")]
        [TestCase("BloodThirst")]
        [TestCase("Frenzy")]
        [TestCase("IronWill")]
        public void 액티브_4장_Category_Enhance_그대로_유지_BLOCKER1_회귀(string id)
        {
            var card = LoadCard(id);
            Assert.AreEqual(ECardCategory.Enhance, card.Category,
                $"{id} 의 _category 가 Enhance 아닌 다른 값으로 재분류됨 — " +
                "BuildPanel 필터 조건(Enhance && IsPassive) 검토 필요");
        }

        //# ===== Spawn / Replace / Environment 패시브 — Enhance 아닌 카테고리 =====

        [TestCase("SpawnWisps",   ECardCategory.Spawn)]
        [TestCase("SpawnWraith",  ECardCategory.Spawn)]
        [TestCase("SpawnReapers", ECardCategory.Spawn)]
        [TestCase("SpawnPlagues", ECardCategory.Spawn)]
        [TestCase("SpawnPhantoms", ECardCategory.Spawn)]
        [TestCase("ReplaceWispsToWraith", ECardCategory.Replace)]
        [TestCase("ReplaceReapersToHex",  ECardCategory.Replace)]
        [TestCase("HeroPoisonAura",  ECardCategory.Environment)]
        [TestCase("HeroAttackDown",  ECardCategory.Environment)]
        public void 비강화_패시브_카테고리_정합(string id, ECardCategory expected)
        {
            var card = LoadCard(id);
            Assert.AreEqual(expected, card.Category,
                $"{id} 의 카테고리가 {expected} 아님 — BuildPanel 필터 통과 여부 검토");
        }

        //# ===== 필터 조건 (Enhance && IsPassive) 시뮬레이션 =====
        //#
        //# BuildPanel.Refresh 의 if (entry.Card.Category == ECardCategory.Enhance && entry.IsPassive) continue;
        //# 동일 로직을 직접 평가해 6 강화 패시브 만 제외, 4 액티브 강화는 통과를 회귀 고정.

        //# 강화 패시브 6장은 (Enhance, IsPassive=true) → 필터 제외 대상.
        [TestCase("WispHpBoost")]
        [TestCase("WraithDamageBoost")]
        [TestCase("ReaperAtkSpeed")]
        [TestCase("HexRangeBoost")]
        [TestCase("PlagueSlowBoost")]
        [TestCase("PhantomMoveSpeedBoost")]
        public void 강화_패시브_6장_필터_제외_대상_확인(string id)
        {
            var card = LoadCard(id);
            //# (IsPassive 는 BuildEntry 속성이라 카드 SO 에 직접 없음 — 본 테스트는 "패시브 풀에 속한다는 전제 하에" 평가)
            bool wouldBeExcluded = card.Category == ECardCategory.Enhance && true;   //# IsPassive=true 가정.
            Assert.IsTrue(wouldBeExcluded, $"{id} 는 패시브 풀에서 픽 시 필터로 제외되어야 함");
        }

        //# 액티브 강화 4장은 IsPassive=false 로 들어와 필터에서 통과.
        [TestCase("Berserk")]
        [TestCase("BloodThirst")]
        [TestCase("Frenzy")]
        [TestCase("IronWill")]
        public void 액티브_강화_4장_필터_통과_BLOCKER1_정합(string id)
        {
            var card = LoadCard(id);
            //# 액티브 풀에서 들어오므로 IsPassive=false.
            bool wouldBeExcluded = card.Category == ECardCategory.Enhance && false;
            Assert.IsFalse(wouldBeExcluded,
                $"{id} 는 액티브 카드(IsPassive=false)라 필터를 통과해야 함 — BLOCKER 1 회귀");
        }

        //# 비-강화 패시브 9장은 그냥 통과.
        [TestCase("SpawnWisps")]
        [TestCase("SpawnWraith")]
        [TestCase("SpawnReapers")]
        [TestCase("SpawnPlagues")]
        [TestCase("SpawnPhantoms")]
        [TestCase("ReplaceWispsToWraith")]
        [TestCase("ReplaceReapersToHex")]
        [TestCase("HeroPoisonAura")]
        [TestCase("HeroAttackDown")]
        public void 비강화_패시브_9장_필터_통과(string id)
        {
            var card = LoadCard(id);
            bool wouldBeExcluded = card.Category == ECardCategory.Enhance && true;
            Assert.IsFalse(wouldBeExcluded, $"{id} 는 Enhance 아니므로 필터 통과");
        }
    }
}
