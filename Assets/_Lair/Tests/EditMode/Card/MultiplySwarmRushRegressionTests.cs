using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 (2026-05-31, Phase 3 Task 16) — Multiply enum 자리·SO 파일명 보존 회귀.
    //# spec D10 (Multiply 삭제) 의 실제 정책 = enum 자리 + SO 파일명 보존 + 효과 클래스만 SwarmRushEffect 로 교체.
    //# Berserk → GuardianRage 와 동일 패턴. 본 회귀는 다음 desync 회피용:
    //#   1) ECardId.Multiply enum 값이 20 자리에서 옮겨가면 기존 SO _id 직렬화와 desync.
    //#   2) Multiply.asset 의 효과가 MultiplyEffect 등 폐기 클래스로 회귀하면 SwarmRush 효과(Phantom 6마리) 사라짐.
    //#   3) Multiply.asset 의 _axis 가 Swarm 외로 회귀하면 §3.5 4×7 균등 분배 깨짐.
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
        public void Multiply_asset_SwarmRushEffect_정합()
        {
            CardData multiplyCard = AssetDatabase.LoadAssetAtPath<CardData>(MultiplyAssetPath);
            Assert.IsNotNull(multiplyCard,
                $"Multiply.asset 존재 — enum 자리·SO 파일명 보존 정책 (v0.6, {MultiplyAssetPath})");

            Assert.AreEqual(ECardId.Multiply, multiplyCard.Id,
                "Multiply.asset 의 _id 가 ECardId.Multiply (값 20) — SO 직렬화 정합");

            Assert.AreEqual(EBuildAxis.Swarm, multiplyCard.Axis,
                "Multiply.asset 의 _axis = Swarm (4축 분배 §3.5: Swarm 액티브 3장 자리)");

            Assert.IsInstanceOf<SwarmRushEffect>(multiplyCard.Effect,
                "Multiply.asset 의 효과 클래스 = SwarmRushEffect (v0.6 — Phantom 6마리 즉시 소환)");
        }
    }
}
