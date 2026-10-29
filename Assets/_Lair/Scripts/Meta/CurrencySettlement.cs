using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;

namespace Lair.Meta
{
    //# 한 판의 재화 획득 한 줄 — 결과창 칩 입력 (0 은 행을 만들지 않는다).
    public struct CurrencyGain
    {
        public ECurrency Currency;
        public int Amount;
    }

    //# 챕터 승리 각성석 정산 결과 — 이번 판분(결과창 칩)과 소급분(토스트 1481)을 나눈다.
    public struct ChapterAwakenGain
    {
        public int ThisRun;
        public int Retroactive;
    }

    //# 재화 해금 규칙 (monster-growth-currencies §1) — 해금 값을 따로 저장하지 않고 그 재화를 주는 콘텐츠의 기존 해금 값을 읽는다.
    public static class CurrencyRules
    {
        //# 각성석 = 1-1 Easy 클리어(Normal 첫 해금과 같은 시점).
        public const int AwakenUnlockClearedStage = 1;

        //# 콘텐츠 해금 스테이지(ClearedStage 기준). 콘텐츠 데이터가 없으면 int.MaxValue(잠김). 소울·잔해는 0.
        public static int UnlockClearedStage(ECurrency currency, int dailyUnlock, int towerUnlock)
        {
            switch (currency)
            {
                case ECurrency.AwakenStone: return AwakenUnlockClearedStage;
                case ECurrency.MoonSeal: return dailyUnlock;
                case ECurrency.AbyssCrystal: return towerUnlock;
                default: return 0;
            }
        }

        //# 현재 로드된 데일리/탑 데이터 기준 해금 스테이지.
        public static int UnlockClearedStage(ECurrency currency)
        {
            return UnlockClearedStage(currency, DailyUnlockOrLocked(), TowerUnlockOrLocked());
        }

        //# 잔해만 콘텐츠 해금이 없어 "처음 얻은 순간"(TotalEarned >= 1) 이다.
        public static bool IsUnlocked(ECurrency currency, MetaProfile profile, int dailyUnlock, int towerUnlock)
        {
            if (profile == null)
                return currency == ECurrency.Soul;
            if (currency == ECurrency.Soul)
                return true;
            if (currency == ECurrency.RelicShard)
                return profile.GetCurrencyTotalEarned(ECurrency.RelicShard) >= 1;
            return profile.ClearedStage >= UnlockClearedStage(currency, dailyUnlock, towerUnlock);
        }

        public static bool IsUnlocked(ECurrency currency, MetaProfile profile)
        {
            return IsUnlocked(currency, profile, DailyUnlockOrLocked(), TowerUnlockOrLocked());
        }

        private static int DailyUnlockOrLocked() => DailyDatabase.Current != null ? DailyDatabase.Current.Config.unlockClearedStage : int.MaxValue;

        private static int TowerUnlockOrLocked() => TowerDatabase.Current != null ? TowerDatabase.Current.Config.unlockClearedStage : int.MaxValue;
    }

    //# 잔해 교환 (§2.4 교환 표) — 1회 = fromAmount 개 → toAmount 개. 되돌리기·역교환 없음. isUnlocked 는 목표 재화 해금 판정.
    public static class CurrencyExchange
    {
        public static EExchangeResult Check(MetaProfile profile, GrowthDatabase growth, ECurrency to, Func<ECurrency, bool> isUnlocked)
        {
            Currency_ExchangeRow row = growth != null ? growth.FindExchange(ECurrency.RelicShard, to) : null;
            if (profile == null || row == null)
                return EExchangeResult.Unavailable;
            if (isUnlocked != null && isUnlocked(to) == false)
                return EExchangeResult.Locked;
            if (profile.GetCurrency(ECurrency.RelicShard) < row.fromAmount)
                return EExchangeResult.Insufficient;
            return EExchangeResult.Ok;
        }

        public static EExchangeResult Exchange(MetaProfile profile, GrowthDatabase growth, ECurrency to, Func<ECurrency, bool> isUnlocked)
        {
            EExchangeResult check = Check(profile, growth, to, isUnlocked);
            if (check != EExchangeResult.Ok)
                return check;
            Currency_ExchangeRow row = growth.FindExchange(ECurrency.RelicShard, to);
            profile.TrySpendCurrency(ECurrency.RelicShard, row.fromAmount);
            profile.AddCurrency(to, row.toAmount);
            return EExchangeResult.Ok;
        }
    }

    //# 재화 지급 순수 함수 (monster-growth-currencies §2·§7.2) — 난수 없음. 정산은 지급 기록과 클리어 기록을 비교해 모자란 만큼만 준다(멱등).
    public static class CurrencySettlement
    {
        //# 각성석 첫 클리어 정산 — 각 DifficultyClears 행의 (AwakenPaid+1 .. MaxDifficulty) 첫 클리어분 지급 후 AwakenPaid = MaxDifficulty. 반환 = 지급량.
        public static int SettleAwakenFirstClears(MetaProfile profile, GrowthDatabase growth)
        {
            if (profile == null || growth == null || profile.DifficultyClears == null)
                return 0;
            int total = 0;
            foreach (DifficultyClearEntry entry in profile.DifficultyClears)
            {
                if (entry == null || entry.AwakenPaid >= entry.MaxDifficulty)
                    continue;
                for (int d = Math.Max(0, entry.AwakenPaid) + 1; d <= entry.MaxDifficulty; ++d)
                {
                    Difficulty_AwakenRow row = growth.AwakenRewardOf(d);
                    if (row != null)
                    {
                        total += row.firstClear;
                    }
                }
                entry.AwakenPaid = entry.MaxDifficulty;
            }
            profile.AddCurrency(ECurrency.AwakenStone, total);
            return total;
        }

        //# 챕터 승리 한 판의 각성석 — paidBefore = 이번 판 기록 "전" 그 스테이지 AwakenPaid. 이미 그 난이도 첫 클리어분을 받았으면 반복분, 아니면 정산(첫 클리어분).
        //# 이번 판분 = 그 스테이지 (paidBefore+1 .. difficulty) 첫 클리어분 + 반복분. 같은 정산이 함께 지급한 나머지(다른 스테이지·더 높은 기존 기록)는 소급분.
        //# Easy·패배는 0 (호출부가 승리만 넘긴다).
        public static ChapterAwakenGain SettleChapterWin(MetaProfile profile, GrowthDatabase growth, int difficulty, int paidBefore)
        {
            ChapterAwakenGain gain = new ChapterAwakenGain();
            if (profile == null || growth == null || difficulty < 1)
                return gain;
            int total = SettleAwakenFirstClears(profile, growth);
            for (int d = Math.Max(0, paidBefore) + 1; d <= difficulty; ++d)
            {
                Difficulty_AwakenRow row = growth.AwakenRewardOf(d);
                if (row != null)
                {
                    gain.ThisRun += row.firstClear;
                }
            }
            gain.ThisRun = Math.Min(gain.ThisRun, total);
            gain.Retroactive = total - gain.ThisRun;
            if (difficulty <= paidBefore)
            {
                Difficulty_AwakenRow row = growth.AwakenRewardOf(difficulty);
                int repeat = row != null ? row.repeatWin : 0;
                profile.AddCurrency(ECurrency.AwakenStone, repeat);
                gain.ThisRun += repeat;
            }
            return gain;
        }

        //# 탑 층 하나의 결정 = 층 유물 등급 단계의 층당 지급량 + (이정표 층이면 보너스).
        public static int CrystalsForFloor(int floor, GrowthDatabase growth, TowerDatabase tower, int relicMaxIndex)
        {
            if (growth == null || tower == null || floor < 1)
                return 0;
            TowerConfigRow cfg = tower.Config;
            int amount = growth.CrystalPerFloor(tower.RelicTierOf(floor, relicMaxIndex));
            if (cfg.crystalMilestoneEvery >= 1 && floor % cfg.crystalMilestoneEvery == 0)
            {
                amount += Math.Max(0, cfg.crystalMilestoneBonus);
            }
            return amount;
        }

        //# 탑 결정 정산 — CrystalPaid == false 인 층마다 지급 후 CrystalPaid = true. 시즌 정리(NormalizeTowerSeason)를 먼저 해야 한다(E-8). 반환 = 지급량.
        public static int SettleTowerCrystals(MetaProfile profile, GrowthDatabase growth, TowerDatabase tower, int relicMaxIndex)
        {
            if (profile == null || growth == null || tower == null || profile.TowerFloorClears == null)
                return 0;
            int total = 0;
            foreach (TowerFloorClearEntry entry in profile.TowerFloorClears)
            {
                if (entry == null || entry.CrystalPaid)
                    continue;
                total += CrystalsForFloor(entry.Floor, growth, tower, relicMaxIndex);
                entry.CrystalPaid = true;
            }
            profile.AddCurrency(ECurrency.AbyssCrystal, total);
            return total;
        }

        //# 데일리 판 하나의 달빛 인장 — 판 종료분 + (그날 첫 승리면) 첫 승리분, 각각 floor(기본 x 연속 배율 + 0.5) (§2.2). 지급은 호출부.
        public static int DailySeals(bool isFirstWin, int streak, DailyConfigRow config)
        {
            if (config == null)
                return 0;
            decimal m = DailyRules.StreakMultiplier(streak, config);
            int seals = RoundHalfUp(Math.Max(0, config.sealPerFinish) * m);
            if (isFirstWin)
            {
                seals += RoundHalfUp(Math.Max(0, config.sealFirstWin) * m);
            }
            return seals;
        }

        //# 마을 진입 소급 정산 (§7.2-2) — 업데이트 이전 Normal/Hard·탑 기록분. 반환 = 재화별 지급 줄(0 은 제외).
        public static List<CurrencyGain> SettleRetroactive(MetaProfile profile, GrowthDatabase growth, TowerDatabase tower, int relicMaxIndex)
        {
            List<CurrencyGain> gains = new List<CurrencyGain>();
            if (profile == null || growth == null)
                return gains;
            AddGain(gains, ECurrency.AwakenStone, SettleAwakenFirstClears(profile, growth));
            if (tower != null)
            {
                profile.NormalizeTowerSeason(tower.Config.seasonId);
                AddGain(gains, ECurrency.AbyssCrystal, SettleTowerCrystals(profile, growth, tower, relicMaxIndex));
            }
            return gains;
        }

        public static void AddGain(List<CurrencyGain> gains, ECurrency currency, int amount)
        {
            if (gains == null || amount < 1)
                return;
            gains.Add(new CurrencyGain { Currency = currency, Amount = amount });
        }

        private static int RoundHalfUp(decimal value) => (int)Math.Floor(value + 0.5m);
    }
}
