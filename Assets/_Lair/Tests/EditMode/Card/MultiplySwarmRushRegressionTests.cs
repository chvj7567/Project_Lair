using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 (2026-05-31, Phase 3 Task 16) — Multiply enum 자리·SO 파일명 보존 회귀.
    //# spec D10 (Multiply 삭제) 의 실제 정책 = enum 자리 + SO 파일명 보존 + 효과 클래스만 FastBreedingEffect 로 교체.
    public class MultiplySwarmRushRegressionTests
    {
        private const string MultiplyAssetPath = "Assets/_Lair/Art/Cards/Items/Multiply.asset";

        [Test]
        public void ECardId_Multiply_int값_20_자리_보존()
        {
            //# enum 값 자리 보존 — 기존 SO 의 _id 직렬화(int=20) 와 정합 유지.
            Assert.AreEqual(20, (int)ECardId.Multiply,
                "ECardId.Multiply enum 값 자리 보존 (20) — SO _id 직렬화 desync 방지");
        }

        [Test]
        public void Multiply_asset_FastBreedingEffect_정합()
        {
            CardData multiplyCard = AssetDatabase.LoadAssetAtPath<CardData>(MultiplyAssetPath);
            Assert.IsNotNull(multiplyCard,
                $"Multiply.asset 존재 — enum 자리·SO 파일명 보존 정책 (v0.6, {MultiplyAssetPath})");

            Assert.AreEqual(ECardId.Multiply, multiplyCard.Id,
                "Multiply.asset 의 _id 가 ECardId.Multiply (값 20) — SO 직렬화 정합");

            Assert.AreEqual(EBuildAxis.Swarm, multiplyCard.Axis,
                "Multiply.asset 의 _axis = Swarm (4축 분배 §3.5: Swarm 액티브 3장 자리)");

            Assert.IsInstanceOf<FastBreedingEffect>(multiplyCard.Effect,
                "Multiply.asset 의 효과 클래스 = FastBreedingEffect (v0.6 — Phantom 6마리 즉시 소환)");
        }
    }
}
