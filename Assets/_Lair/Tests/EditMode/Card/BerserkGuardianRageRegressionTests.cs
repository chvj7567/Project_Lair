using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — Berserk enum 자리·SO 파일명 보존 + GuardianRageEffect 정합 회귀.
    //# code-reviewer Phase 3 권장수정 1 — Multiply/SwarmRush 패턴(MultiplySwarmRushRegressionTests) 과 동일 패턴으로 박제.
    //# 다음 desync 회피용:
    //#   1) ECardId.Berserk enum 값이 24 자리에서 옮겨가면 기존 SO _id 직렬화와 desync.
    //#   2) Berserk.asset 의 효과가 BerserkPowerEffect 등 폐기 클래스로 회귀하면 GuardianRage 효과(Wisp/Wraith HP×2) 사라짐.
    //#   3) Berserk.asset 의 _axis 가 Tank(0) 외로 회귀하면 §3.5 4×7 균등 분배 깨짐.
    public class BerserkGuardianRageRegressionTests
    {
        private const string BerserkAssetPath = "Assets/_Lair/Art/Cards/Items/Berserk.asset";

        [Test]
        public void ECardId_Berserk_int값_24_자리_보존()
        {
            //# enum 값 자리 보존 — 기존 SO 의 _id 직렬화(int=24) 와 정합 유지.
            Assert.AreEqual(24, (int)ECardId.Berserk,
                "ECardId.Berserk enum 값 자리 보존 (24) — SO _id 직렬화 desync 방지");
        }

        [Test]
        public void Berserk_asset_GuardianRageEffect_정합()
        {
            CardData berserkCard = AssetDatabase.LoadAssetAtPath<CardData>(BerserkAssetPath);
            Assert.IsNotNull(berserkCard,
                $"Berserk.asset 존재 — enum 자리·SO 파일명 보존 정책 (v0.6, {BerserkAssetPath})");

            Assert.AreEqual(ECardId.Berserk, berserkCard.Id,
                "Berserk.asset 의 _id 가 ECardId.Berserk (값 24) — SO 직렬화 정합");

            Assert.AreEqual(EBuildAxis.Tank, berserkCard.Axis,
                "Berserk.asset 의 _axis = Tank (4축 분배 §3.5: Tank 액티브 3장 자리)");

            Assert.IsInstanceOf<GuardianRageEffect>(berserkCard.Effect,
                "Berserk.asset 의 효과 클래스 = GuardianRageEffect (v0.6 — Wisp/Wraith HP×2 + 받는데미지×0.5)");
        }

        //# DisplayName 도 "수호자의 분노" 로 리뉴얼됨 — UI 표기 desync 방지.
        [Test]
        public void Berserk_asset_DisplayName_수호자의_분노()
        {
            CardData berserkCard = AssetDatabase.LoadAssetAtPath<CardData>(BerserkAssetPath);
            Assert.IsNotNull(berserkCard, "Berserk.asset 존재");
            Assert.AreEqual("수호자의 분노", berserkCard.DisplayName,
                "Berserk.asset 의 displayName = '수호자의 분노' (v0.6 리뉴얼)");
        }
    }
}
