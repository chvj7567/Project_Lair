using System;
using NUnit.Framework;
using Lair.Data;

namespace Lair.Tests
{
    //# ECardId 를 CommonEnum.cs 에서 ECardId.cs 전용 파일로 분리한 작업의 회귀 박제.
    //# CardData._id 가 int 직렬화이므로 enum 값의 정수값/순서가 깨지면 기존 28장 에셋이 어긋난다.
    //# 분리는 순수 이동(값·순서 무변경)이어야 하며, 본 테스트가 그 안전 보장을 고정한다.
    public class ECardIdRegressionTests
    {
        //# 핵심 경계값 — 첫 패시브, 마지막 액티브(구 Berserk 자리), 마지막 신규(SpawnerHaste).
        [Test]
        public void ECardId_핵심_정수값_보존()
        {
            Assert.AreEqual(0, (int)ECardId.WispHpBoost, "첫 패시브 WispHpBoost 는 0 이어야 한다");
            Assert.AreEqual(24, (int)ECardId.Berserk, "구 Berserk 자리는 24 여야 한다");
            Assert.AreEqual(27, (int)ECardId.SpawnerHaste, "마지막 신규 SpawnerHaste 는 27 이어야 한다");
        }

        //# 패시브/액티브 구간 경계 — 정수값이 한 칸이라도 밀리면 직렬화 정합이 깨진다.
        [Test]
        public void ECardId_구간_경계값_보존()
        {
            Assert.AreEqual(14, (int)ECardId.HeroAttackDown, "패시브 마지막 HeroAttackDown 은 14 여야 한다");
            Assert.AreEqual(15, (int)ECardId.Fear, "액티브 첫 Fear 는 15 여야 한다");
            Assert.AreEqual(25, (int)ECardId.WallOfWisps, "v0.6 신규 첫 WallOfWisps 는 25 여야 한다");
        }

        //# 총 개수 28 (패시브 15 + 액티브 10 + v0.6 신규 3). 새 값 추가 시 본 테스트를 의도적으로 갱신해야 함.
        [Test]
        public void ECardId_총_개수_28()
        {
            int count = Enum.GetValues(typeof(ECardId)).Length;
            Assert.AreEqual(28, count, "ECardId 총 개수는 28 이어야 한다 (값 추가 시 회귀 테스트를 의도적으로 갱신)");
        }

        //# 선언 순서가 정수값 순서와 일치 — 0,1,2,…,27 연속. 중간 값 명시 지정(= 대입)이나 재배열을 차단.
        [Test]
        public void ECardId_연속된_정수값_0부터()
        {
            Array values = Enum.GetValues(typeof(ECardId));
            for (int i = 0; i < values.Length; i++)
            {
                int actual = (int)values.GetValue(i);
                Assert.AreEqual(i, actual, $"인덱스 {i} 의 ECardId 정수값은 {i} 여야 한다 (연속 보존)");
            }
        }
    }
}
