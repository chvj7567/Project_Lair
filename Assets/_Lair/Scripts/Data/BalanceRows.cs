using System;

namespace Lair.Data
{
    //# 전투 밸런스 JSON row 2종 (so-to-json-migration §3.2~§3.3). 순수 데이터 — 파일명 = EData 값명. sentinel 없음.

    //# BalanceConfig.json (단일 객체) — 핵심 엔티티: 전투 한 판에 공통으로 적용되는 전역 상수 묶음.
    [Serializable]
    public class BalanceConfigRow
    {
        public int heroHp;                  //# 영웅 HP 앵커
        public int heroPower;               //# 영웅 공격력 앵커
        public float runDuration;           //# 한 판 길이(초)
        public float[] passiveThresholds;   //# 패시브 카드 3택1 을 여는 영웅 HP 비율 (내림차순)
        public float[] activeThresholds;    //# 액티브 카드 3택1 을 여는 경과 초 (오름차순)
    }

    //# MonsterStat.json — 핵심 엔티티: 기준 몬스터 한 종의 기본 능력치와 스폰 주기. PK monster.
    [Serializable]
    public class MonsterStatRow
    {
        public string monster;      //# EMonster 기준 6종 값명
        public int hp;
        public int power;
        public float range;
        public float cooldown;
        public float moveSpeed;
        public float spawnPeriod;   //# 이 종을 출력하는 둥지의 기본 스폰 주기(초) — 필수 1:1 이라 직접 필드
    }
}
