using Lair.Data;
using Lair.UI;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 테두리 색 결정 단일 출처 — 종(種) / 영웅 / 몬스터 전체 3계열로 분류.
    //# 종 색은 SpawnerStatusCell.SpeciesColor 재사용.
    public enum ECardScope { SingleSpecies, AllMonsters, Hero }

    public static class CardBorderColors
    {
        //# 영웅 대상 카드 — 6종 색과 명확히 구분되는 백색.
        public static readonly Color HeroColor = new Color(1f, 1f, 1f, 1f);
        //# 몬스터 전체/2종 묶음 카드 — 6종 + 영웅용과 모두 구분되는 시안 #06B6D4.
        public static readonly Color AllMonstersColor = new Color(0.024f, 0.714f, 0.831f, 1f);

        //# (scope, species) — SingleSpecies 일 때만 species 가 의미를 가짐.
        //# enum 값명과 실제 효과 클래스가 다른 카드(v0.6 리뉴얼 잔재) 는 *실제 효과* 기준 분류.
        public static (ECardScope scope, EMonster species) ScopeOf(ECardId id) => id switch
        {
            ECardId.WispHpBoost            => (ECardScope.SingleSpecies, EMonster.Wisp),
            ECardId.WraithDamageBoost      => (ECardScope.SingleSpecies, EMonster.Wraith),
            ECardId.ReaperAtkSpeed         => (ECardScope.SingleSpecies, EMonster.Reaper),
            ECardId.HexRangeBoost          => (ECardScope.SingleSpecies, EMonster.Hex),
            ECardId.PlagueSlowBoost        => (ECardScope.SingleSpecies, EMonster.Plague),
            ECardId.PhantomMoveSpeedBoost  => (ECardScope.SingleSpecies, EMonster.Phantom),
            ECardId.SpawnWisps             => (ECardScope.SingleSpecies, EMonster.Wisp),
            ECardId.SpawnWraith            => (ECardScope.SingleSpecies, EMonster.Wraith),
            ECardId.SpawnReapers           => (ECardScope.SingleSpecies, EMonster.Reaper),
            ECardId.SpawnPlagues           => (ECardScope.SingleSpecies, EMonster.Plague),
            ECardId.SpawnPhantoms          => (ECardScope.SingleSpecies, EMonster.Phantom),
            //# Multiply enum 자리 = SwarmRushEffect (Phantom 스폰 가속) — Phantom 단일.
            ECardId.Multiply               => (ECardScope.SingleSpecies, EMonster.Phantom),

            //# 영웅 대상.
            ECardId.HeroPoisonAura         => (ECardScope.Hero, default),
            ECardId.HeroAttackDown         => (ECardScope.Hero, default),
            ECardId.Fear                   => (ECardScope.Hero, default),
            ECardId.Bleed                  => (ECardScope.Hero, default),
            ECardId.Weaken                 => (ECardScope.Hero, default),
            ECardId.Slow                   => (ECardScope.Hero, default),
            ECardId.TimeStop               => (ECardScope.Hero, default),
            ECardId.MarkOfDeath            => (ECardScope.Hero, default),

            //# 몬스터 전체 일괄 + 2종 묶음.
            ECardId.Frenzy                 => (ECardScope.AllMonsters, default),
            ECardId.IronWill               => (ECardScope.AllMonsters, default),
            ECardId.SpawnerHaste           => (ECardScope.AllMonsters, default),
            ECardId.BloodThirst            => (ECardScope.AllMonsters, default),
            //# ReplaceWispsToWraith 자리 = WispWraithPowerBoostEffect (2종 묶음).
            ECardId.ReplaceWispsToWraith   => (ECardScope.AllMonsters, default),
            //# ReplaceReapersToHex 자리 = ReaperHexPowerBoostEffect (2종 묶음).
            ECardId.ReplaceReapersToHex    => (ECardScope.AllMonsters, default),
            //# Berserk 자리 = GuardianRageEffect (Wisp/Wraith 받피해 0.5×).
            ECardId.Berserk                => (ECardScope.AllMonsters, default),
            //# WallOfWisps 자리 = ToughHideEffect (Wisp/Wraith 받피해 0.75×).
            ECardId.WallOfWisps            => (ECardScope.AllMonsters, default),

            _                              => (ECardScope.AllMonsters, default),
        };

        //# 카드 ID → 테두리 색. 모든 카드 셀 색 결정 로직이 이 함수 한 곳을 거친다.
        public static Color BorderColorOf(ECardId id)
        {
            (ECardScope scope, EMonster species) = ScopeOf(id);
            return scope switch
            {
                ECardScope.Hero          => HeroColor,
                ECardScope.AllMonsters   => AllMonstersColor,
                ECardScope.SingleSpecies => SpawnerStatusCell.SpeciesColor(species),
                _                        => Color.gray,
            };
        }
    }
}
