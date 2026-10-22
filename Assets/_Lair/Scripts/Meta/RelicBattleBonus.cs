using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    //# 보유 유물 → 전투 시작 스냅샷 집계 (relics §6.1). MetaBattleBonus 와 같은 모양 — 전투 중 보유는 변하지 않으므로 1회 집계.
    //# Unity 비의존. DB 에 없는 id 는 무시, 중첩은 maxStack 으로 클램프(읽는 순간 보정, 세이브 비수정 — relics §3.6).
    public class RelicBattleBonus
    {
        //# 곱 하한 — 능력치 배율·스포너 주기가 이보다 내려가지 않는다(설계 방어, relics §6.3).
        public const float MulFloor = 0.5f;
        //# 영웅 공격력 배율 하한 — 감소 유물이 늘어도 0 이 되지 않게(설계 방어).
        public const float AttackFactorFloor = 0.1f;

        public struct LightningSpec
        {
            public float IntervalSec;
            public float HpRatio;
        }

        public struct SummonSpec
        {
            public EMonster Monster;
            public int Count;
        }

        public struct FrenzySpec
        {
            public float HpFraction;
            public float DurationSec;
        }

        private readonly Dictionary<EMonsterStatKind, float> _statMuls = new Dictionary<EMonsterStatKind, float>();
        private float _spawnerPeriodMul = 1f;
        private float _attackDownAmount;

        public static RelicBattleBonus None => new RelicBattleBonus();

        public float SpawnerPeriodMul => Math.Max(MulFloor, _spawnerPeriodMul);
        public float SpawnPreludeAdvanceSec { get; private set; }
        public float HeroBurnRatioPerSec { get; private set; }
        public List<LightningSpec> Lightnings { get; } = new List<LightningSpec>();
        public List<SummonSpec> OpeningSummons { get; } = new List<SummonSpec>();
        public List<FrenzySpec> Frenzies { get; } = new List<FrenzySpec>();
        //# 2배속 유물 보유 시 목표 속도(speedScale), 미보유 0.
        public float BattleSpeedScale { get; private set; }
        public float SoulBonus { get; private set; }
        public float XpBonus { get; private set; }
        public float LoseSoulBonus { get; private set; }

        public bool HasBattleSpeed => BattleSpeedScale > 0f;

        //# 영웅 공격력 감소 유물이 있을 때만 true — 값은 HeroAttackFactor.
        public bool HasHeroAttackDown => _attackDownAmount > 0f;
        public float HeroAttackFactor => Math.Max(AttackFactorFloor, 1f - _attackDownAmount);

        public float GetStatMul(EMonsterStatKind kind)
            => _statMuls.TryGetValue(kind, out float mul) ? Math.Max(MulFloor, mul) : 1f;

        public static RelicBattleBonus From(MetaProfile profile, RelicDatabase db)
        {
            RelicBattleBonus bonus = new RelicBattleBonus();
            if (profile == null || db == null || profile.Relics == null)
                return bonus;

            foreach (RelicEntry entry in profile.Relics)
            {
                if (entry == null)
                    continue;
                RelicData data = db.Get(entry.RelicId);
                if (data == null)
                    continue;
                int stack = Math.Min(entry.Stack, data.MaxStack);
                if (stack < 1)
                    continue;

                foreach (RelicEffectData effect in data.Effects)
                {
                    bonus.Accumulate(db, effect, stack);
                }
            }
            return bonus;
        }

        private void Accumulate(RelicDatabase db, RelicEffectData effect, int stack)
        {
            switch (effect.Kind)
            {
                case ERelicEffectKind.MonsterStat:
                {
                    int statValue = (int)effect.Get("statKind");
                    if (Enum.IsDefined(typeof(EMonsterStatKind), statValue) == false)
                        return;
                    EMonsterStatKind kind = (EMonsterStatKind)statValue;
                    float mul = 1f + db.ScaledParam(effect, "amount", stack);
                    _statMuls[kind] = (_statMuls.TryGetValue(kind, out float current) ? current : 1f) * mul;
                    break;
                }
                case ERelicEffectKind.SpawnerPeriod:
                    _spawnerPeriodMul *= 1f + db.ScaledParam(effect, "amount", stack);
                    break;
                case ERelicEffectKind.SpawnPrelude:
                    SpawnPreludeAdvanceSec += db.ScaledParam(effect, "advanceSec", stack);
                    break;
                case ERelicEffectKind.HeroBurn:
                    HeroBurnRatioPerSec += db.ScaledParam(effect, "hpRatioPerSec", stack);
                    break;
                case ERelicEffectKind.HeroLightning:
                    Lightnings.Add(new LightningSpec
                    {
                        IntervalSec = db.ScaledParam(effect, "intervalSec", stack),
                        HpRatio = db.ScaledParam(effect, "hpRatio", stack),
                    });
                    break;
                case ERelicEffectKind.HeroAttackDown:
                    _attackDownAmount += db.ScaledParam(effect, "amount", stack);
                    break;
                case ERelicEffectKind.OpeningSummon:
                {
                    int monsterValue = (int)effect.Get("monster");
                    if (Enum.IsDefined(typeof(EMonster), monsterValue) == false)
                        return;
                    OpeningSummons.Add(new SummonSpec
                    {
                        Monster = (EMonster)monsterValue,
                        Count = (int)Math.Round(db.ScaledParam(effect, RelicDatabase.CountPerStackKey, stack)),
                    });
                    break;
                }
                case ERelicEffectKind.LowHpFrenzy:
                    Frenzies.Add(new FrenzySpec
                    {
                        HpFraction = db.ScaledParam(effect, "hpFraction", stack),
                        DurationSec = db.ScaledParam(effect, "durationSec", stack),
                    });
                    break;
                case ERelicEffectKind.BattleSpeed:
                    BattleSpeedScale = Math.Max(BattleSpeedScale, db.ScaledParam(effect, "speedScale", stack));
                    break;
                case ERelicEffectKind.SoulBonus:
                    SoulBonus += db.ScaledParam(effect, "amount", stack);
                    break;
                case ERelicEffectKind.XpBonus:
                    XpBonus += db.ScaledParam(effect, "amount", stack);
                    break;
                case ERelicEffectKind.LoseSoulBonus:
                    LoseSoulBonus += db.ScaledParam(effect, "amount", stack);
                    break;
            }
        }

        //# 보상 가산 (relics §6.4) — 승리: 소울 x(1+SoulBonus), XP x(1+XpBonus). 패배: 소울 x(1+SoulBonus+LoseSoulBonus) 합산 후 1회 floor, XP 는 XpBonus 만.
        public SoulReward ApplyReward(SoulReward reward, bool win)
        {
            double soulRate = 1.0 + SoulBonus + (win ? 0.0 : LoseSoulBonus);
            double xpRate = 1.0 + XpBonus;
            return new SoulReward
            {
                Souls = FloorScaled(reward.Souls, soulRate),
                Xp = FloorScaled(reward.Xp, xpRate),
            };
        }

        //# 부동소수 오차로 정수 경계가 한 칸 내려가지 않도록 아주 작은 보정 후 floor.
        private static int FloorScaled(int value, double rate)
        {
            //# 비율의 float 합산 오차(예: 0.52499998)를 6자리 반올림으로 제거한 뒤 내림 — 값 크기와 무관하게 안전.
            return (int)Math.Floor(value * Math.Round(rate, 6) + 1e-6);
        }
    }
}
