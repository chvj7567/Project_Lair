using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    public enum ERelicDropOutcome
    {
        NoCandidates,   //# 영웅 테이블 후보가 전부 최대 중첩 — 굴리지 않음(연패 카운터 불변)
        Missed,         //# 굴렸으나 미획득
        Acquired,       //# 획득
        Shard,          //# 뽑힌 등급의 후보가 모두 최대 중첩 — 유물 대신 잔해 (monster-growth-currencies §2.4 C). "획득"으로 본다
    }

    //# 드랍 굴림 결과 — 결과창 ResultPopupArg 로 옮길 확정값.
    public struct RelicDropResult
    {
        public ERelicDropOutcome Outcome;
        public string RelicId;            //# Acquired 일 때만 유효
        public int StackAfter;
        public int MaxStack;
        public bool IsNew;                //# 처음 얻은 유물인가
        public bool ShowNextChance;       //# 승리 + 미획득
        public int NextWinChancePercent;  //# 다음 승리의 예상 확률(정수 %)
        public int ShardAmount;           //# Shard 일 때만 유효
        public ERelicRarity ShardRarity;  //# Shard 일 때만 유효 — 뽑힌 등급

        public bool Acquired => Outcome == ERelicDropOutcome.Acquired;
        public bool IsShard => Outcome == ERelicDropOutcome.Shard;
    }

    //# 유물 드랍 굴림 순수 함수 (relics §2). 난수원 주입(Func<float> [0,1)) — 단위 테스트·시뮬레이터 재현용. Unity 비의존.
    public static class RelicDropRoller
    {
        //# 이번 굴림의 확률. 승리: stage x diff + pity (보유 0개면 1.0), 패배: stage x diff x lossMul (pity 미사용). maxChance 로 절단.
        public static float ComputeChance(RelicDatabase db, MetaProfile profile, int chapterId, int stageIndex, int difficulty, bool win)
        {
            RelicConfigRow cfg = db.Config;
            float baseChance = db.StageDropChance(chapterId, stageIndex) * db.DifficultyChanceMul(difficulty);
            if (win == false)
                return Clamp(baseChance * cfg.lossChanceMul, 0f, cfg.maxChance);

            if (OwnsAnyKnownRelic(db, profile) == false)
                return 1f;
            float pity = Math.Min(cfg.pityMaxBonus, Math.Max(0, profile.RelicMissStreak) * cfg.pityPerMiss);
            return Clamp(baseChance + pity, 0f, cfg.maxChance);
        }

        //# 한 번 굴리고 profile 을 갱신한다(Relics, RelicMissStreak, 잔해). 후보가 없으면 아무것도 바꾸지 않는다.
        //# growth 에 잔해 데이터가 있으면 등급 단위 변환(§2.4 C) — 등급 후보는 최대 중첩 여부를 보지 않고, 뽑힌 등급이 다 찼으면 잔해. 없으면 기존 규칙.
        public static RelicDropResult Roll(RelicDatabase db, MetaProfile profile, string heroId, int chapterId, int stageIndex,
            int difficulty, bool win, Func<float> rng, GrowthDatabase growth = null)
        {
            if (db == null || profile == null || rng == null)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };

            bool shards = growth != null && growth.HasShards;
            List<Relic_DropRow> candidates = Candidates(db, profile, heroId);
            List<Relic_DropRow> table = shards ? TableOf(db, heroId) : candidates;
            if (table.Count == 0)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };

            float chance = ComputeChance(db, profile, chapterId, stageIndex, difficulty, win);
            if (rng() >= chance)
            {
                if (win)
                {
                    profile.RelicMissStreak++;
                }
                return new RelicDropResult
                {
                    Outcome = ERelicDropOutcome.Missed,
                    ShowNextChance = win,
                    NextWinChancePercent = win ? (int)Math.Round(ComputeChance(db, profile, chapterId, stageIndex, difficulty, true) * 100f) : 0,
                };
            }

            ERelicRarity rarity = PickRarity(db, table, difficulty, rng);
            profile.RelicMissStreak = 0;
            List<Relic_DropRow> pool = candidates.FindAll(c => db.Get(c.relicId).Rarity == rarity);
            if (pool.Count == 0)
                return GrantShard(profile, growth, rarity);
            Relic_DropRow picked = PickInPool(pool, rng);
            RelicData pickedData = db.Get(picked.relicId);
            bool isNew = profile.GetRelicStack(picked.relicId) < 1;
            int stackAfter = profile.AddRelic(picked.relicId);
            return new RelicDropResult
            {
                Outcome = ERelicDropOutcome.Acquired,
                RelicId = picked.relicId,
                StackAfter = stackAfter,
                MaxStack = pickedData.MaxStack,
                IsNew = isNew,
            };
        }

        //# 데일리 첫 승리 확정 굴림 (daily-challenge §7.1) — 확률 단계를 건너뛰고(chance 1.0) 등급 → 유물 추첨만 한다.
        //# RelicMissStreak 은 읽지도 쓰지도 않는다(챕터 판의 천장 진행 보존). 후보가 0 이면 NoCandidates(E-12).
        //# 잔해 데이터가 있으면 확정 굴림이 항상 유물이나 잔해 중 하나를 준다(§2.4 적용 경로 표).
        public static RelicDropResult RollGuaranteed(RelicDatabase db, MetaProfile profile, string heroId, int difficulty, Func<float> rng, GrowthDatabase growth = null)
        {
            if (db == null || profile == null || rng == null)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };

            bool shards = growth != null && growth.HasShards;
            List<Relic_DropRow> candidates = Candidates(db, profile, heroId);
            List<Relic_DropRow> table = shards ? TableOf(db, heroId) : candidates;
            if (table.Count == 0)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };

            ERelicRarity rarity = PickRarity(db, table, difficulty, rng);
            List<Relic_DropRow> pool = candidates.FindAll(c => db.Get(c.relicId).Rarity == rarity);
            if (pool.Count == 0)
                return GrantShard(profile, growth, rarity);
            Relic_DropRow picked = PickInPool(pool, rng);
            RelicData pickedData = db.Get(picked.relicId);
            bool isNew = profile.GetRelicStack(picked.relicId) < 1;
            int stackAfter = profile.AddRelic(picked.relicId);
            return new RelicDropResult
            {
                Outcome = ERelicDropOutcome.Acquired,
                RelicId = picked.relicId,
                StackAfter = stackAfter,
                MaxStack = pickedData.MaxStack,
                IsNew = isNew,
            };
        }

        //# 영웅 드랍 테이블 중 최대 중첩이 아닌 유물.
        private static List<Relic_DropRow> Candidates(RelicDatabase db, MetaProfile profile, string heroId)
        {
            List<Relic_DropRow> candidates = new List<Relic_DropRow>();
            foreach (Relic_DropRow drop in db.DropsOf(heroId))
            {
                RelicData data = db.Get(drop.relicId);
                if (data != null && profile.GetRelicStack(drop.relicId) < data.MaxStack)
                {
                    candidates.Add(drop);
                }
            }
            return candidates;
        }

        //# 영웅 드랍 테이블 전체(데이터가 있는 유물) — 잔해 규칙의 등급 후보 판정용(최대 중첩 무관).
        private static List<Relic_DropRow> TableOf(RelicDatabase db, string heroId)
        {
            List<Relic_DropRow> rows = new List<Relic_DropRow>();
            foreach (Relic_DropRow drop in db.DropsOf(heroId))
            {
                if (db.Get(drop.relicId) != null)
                {
                    rows.Add(drop);
                }
            }
            return rows;
        }

        private static RelicDropResult GrantShard(MetaProfile profile, GrowthDatabase growth, ERelicRarity rarity)
        {
            int amount = growth.ShardAmount(rarity);
            profile.AddCurrency(ECurrency.RelicShard, amount);
            return new RelicDropResult { Outcome = ERelicDropOutcome.Shard, ShardAmount = amount, ShardRarity = rarity };
        }

        //# 1단계 — rows 에 있는 등급들 중 (baseWeight x 난이도 배율) 비례로 등급 하나. 난수 1회 소비.
        private static ERelicRarity PickRarity(RelicDatabase db, List<Relic_DropRow> rows, int difficulty, Func<float> rng)
        {
            ERelicRarity[] order = (ERelicRarity[])Enum.GetValues(typeof(ERelicRarity));
            List<ERelicRarity> present = new List<ERelicRarity>();
            List<float> weights = new List<float>();
            float total = 0f;
            foreach (ERelicRarity rarity in order)
            {
                bool any = rows.Exists(c => db.Get(c.relicId).Rarity == rarity);
                if (any == false)
                    continue;
                float w = db.RarityBaseWeight(rarity) * db.RarityWeightMul(difficulty, rarity);
                present.Add(rarity);
                weights.Add(w);
                total += w;
            }

            ERelicRarity chosen = present[present.Count - 1];
            float t = rng() * total;
            for (int i = 0; i < present.Count; ++i)
            {
                if (t < weights[i])
                {
                    chosen = present[i];
                    break;
                }
                t -= weights[i];
            }
            return chosen;
        }

        //# 2단계 — 그 등급 후보 중 weight 비례로 유물 하나. 난수 1회 소비.
        private static Relic_DropRow PickInPool(List<Relic_DropRow> pool, Func<float> rng)
        {
            float sum = 0f;
            foreach (Relic_DropRow row in pool)
            {
                sum += row.weight;
            }
            float r = rng() * sum;
            foreach (Relic_DropRow row in pool)
            {
                if (r < row.weight)
                    return row;
                r -= row.weight;
            }
            return pool[pool.Count - 1];
        }

        private static bool OwnsAnyKnownRelic(RelicDatabase db, MetaProfile profile)
        {
            if (profile.Relics == null)
                return false;
            foreach (RelicEntry entry in profile.Relics)
            {
                if (entry != null && entry.Stack >= 1 && db.Get(entry.RelicId) != null)
                    return true;
            }
            return false;
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
