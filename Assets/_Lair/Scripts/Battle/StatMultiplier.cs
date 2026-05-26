using Lair.Data;

namespace Lair.Battle
{
    //# 지속 스폰 — 종(EMonster)별 누적 스탯 배율 (§3.0.1).
    //# 강화 카드 픽 시 곱연산으로 갱신, ApplyMonsterStats 가 raw×배율로 적용.
    //# 모든 필드 기본 1.0 — 픽 이력 리스트가 아니라 곱연산된 단일 배율값.
    public class StatMultiplier
    {
        public float HpMul = 1f;          //# 최대 HP 배율
        public float PowerMul = 1f;       //# 공격력 배율
        public float CooldownMul = 1f;    //# 공격 쿨다운 배율 (작을수록 빠름)
        public float RangeMul = 1f;       //# 사거리 배율
        public float MoveSpeedMul = 1f;   //# 이동속도 배율
        public float SlowFactorMul = 1f;  //# 플레이그 둔화 배율

        //# 글로벌 dict 에 종 키가 없을 때 쓰는 항등값 (전부 1.0).
        //# 매번 새 인스턴스 — 공유 가변 인스턴스 오염 방지 (호출처가 Multiply 호출 시 원본 보호).
        public static StatMultiplier Identity => new StatMultiplier();

        //# 강화 카드가 지정한 스탯 종류 필드에 배율을 곱연산 누적.
        public void Multiply(EMonsterStatKind stat, float multiplier)
        {
            switch (stat)
            {
                case EMonsterStatKind.Hp:         HpMul         *= multiplier; break;
                case EMonsterStatKind.Power:      PowerMul      *= multiplier; break;
                case EMonsterStatKind.Cooldown:   CooldownMul   *= multiplier; break;
                case EMonsterStatKind.Range:      RangeMul      *= multiplier; break;
                case EMonsterStatKind.MoveSpeed:  MoveSpeedMul  *= multiplier; break;
                case EMonsterStatKind.SlowFactor: SlowFactorMul *= multiplier; break;
            }
        }
    }
}
