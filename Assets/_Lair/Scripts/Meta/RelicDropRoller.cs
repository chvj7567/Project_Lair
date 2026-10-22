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

        public bool Acquired => Outcome == ERelicDropOutcome.Acquired;
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

        //# 한 번 굴리고 profile 을 갱신한다(Relics, RelicMissStreak). 후보가 없으면 아무것도 바꾸지 않는다.
        public static RelicDropResult Roll(RelicDatabase db, MetaProfile profile, string heroId, int chapterId, int stageIndex,
            int difficulty, bool win, Func<float> rng)
        {
            if (db == null || profile == null || rng == null)
                return new RelicDropResult { Outcome = ERelicDropOutcome.NoCandidates };

            List<Relic_DropRow> candidates = new List<Relic_DropRow>();
            foreach (Relic_DropRow drop in db.DropsOf(heroId))
            {
                RelicData data = db.Get(drop.relicId);
                if (data != null && profile.GetRelicStack(drop.relicId) < data.MaxStack)
                {
                    candidates.Add(drop);
                }
            }
            if (candidates.Count == 0)
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

            Relic_DropRow picked = PickRelic(db, candidates, difficulty, rng);
            RelicData pickedData = db.Get(picked.relicId);
            bool isNew = profile.GetRelicStack(picked.relicId) < 1;
            int stackAfter = profile.AddRelic(picked.relicId);
            profile.RelicMissStreak = 0;
            return new RelicDropResult
            {
                Outcome = ERelicDropOutcome.Acquired,
                RelicId = picked.relicId,
                StackAfter = stackAfter,
                MaxStack = pickedData.MaxStack,
                IsNew = isNew,
            };
        }

        //# 2단계 추첨 — 후보가 있는 등급들 중 (baseWeight x 난이도 배율) 비례로 등급, 그 등급 후보 중 weight 비례로 유물.
        private static Relic_DropRow PickRelic(RelicDatabase db, List<Relic_DropRow> candidates, int difficulty, Func<float> rng)
        {
            ERelicRarity[] order = (ERelicRarity[])Enum.GetValues(typeof(ERelicRarity));
            List<ERelicRarity> present = new List<ERelicRarity>();
            List<float> weights = new List<float>();
            float total = 0f;
            foreach (ERelicRarity rarity in order)
            {
                bool any = candidates.Exists(c => db.Get(c.relicId).Rarity == rarity);
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

            List<Relic_DropRow> pool = candidates.FindAll(c => db.Get(c.relicId).Rarity == chosen);
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
