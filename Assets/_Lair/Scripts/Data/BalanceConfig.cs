using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 전투 밸런스 런타임 모델 — BalanceConfig.json + MonsterStat.json 조립 결과 (so-to-json-migration §6.1).
    //# ChapterDatabaseLoader 가 Current 에 올리고 전투·마을이 조회한다.
    public class BalanceConfig
    {
        //# 한 캐릭터의 튜닝 가능한 스탯.
        [Serializable]
        public class CharacterStat
        {
            public int   Hp;
            public int   Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
        }

        //# 영웅 앵커 — HP/Power 만 가진다 (chapter-system §1.2 이관 규칙). range/cooldown/moveSpeed 는 HeroArchetype.json 소관.
        //# 실제 영웅 스탯 = ScaleHeroStat(앵커, HeroArchetype.scale x Stage.multiplier).
        [Serializable]
        public class HeroAnchor
        {
            public int Hp;
            public int Power;
        }

        //# 로드·검증 성공 시에만 채워진다. null 이면 밸런스 미로드 — 전투 진입 불가(§6.4).
        public static BalanceConfig Current { get; set; }

        //# 키 = 기준 몬스터(EMonster 기준 6종).
        private readonly Dictionary<EMonster, CharacterStat> _stats = new Dictionary<EMonster, CharacterStat>();
        private readonly Dictionary<EMonster, float> _spawnPeriods = new Dictionary<EMonster, float>();

        public HeroAnchor Hero { get; }
        public float RunDuration { get; }
        public float[] PassiveThresholds { get; }
        public float[] ActiveThresholds { get; }

        //# 검증 없는 조립 — 로더는 Build 를 쓴다. 값명이 EMonster 에 없는 행은 건너뛴다.
        //# 테스트가 일부 행만으로 미발견 폴백 경로를 재현할 때 직접 부른다.
        public BalanceConfig(BalanceConfigRow config, MonsterStatRow[] monsters)
        {
            Hero = new HeroAnchor { Hp = config.heroHp, Power = config.heroPower };
            RunDuration = config.runDuration;
            PassiveThresholds = config.passiveThresholds;
            ActiveThresholds = config.activeThresholds;
            if (monsters == null)
                return;

            foreach (MonsterStatRow row in monsters)
            {
                if (row == null || FormationDatabase.TryParseMonster(row.monster, out EMonster key) == false)
                    continue;
                _stats[key] = new CharacterStat
                {
                    Hp = row.hp,
                    Power = row.power,
                    Range = row.range,
                    Cooldown = row.cooldown,
                    MoveSpeed = row.moveSpeed,
                };
                _spawnPeriods[key] = row.spawnPeriod;
            }
        }

        //# EMonster 키로 스탯 행 조회 — 등급 몬스터는 기준 몬스터 행(monster-grades §6). 미발견 시 null + 경고.
        public CharacterStat GetMonster(EMonster key)
        {
            if (_stats.TryGetValue(MonsterGrades.BaseOf(key), out CharacterStat stat))
                return stat;
            Debug.LogWarning($"[BalanceConfig] 몬스터 스탯 미발견: {key}");
            return null;
        }

        //# EMonster 키로 base 스폰 주기 조회. 미발견 시 fallback 9f + 경고 (GetMonster 패턴 일치).
        public float GetSpawnPeriod(EMonster key)
        {
            if (_spawnPeriods.TryGetValue(MonsterGrades.BaseOf(key), out float period))
                return period;
            Debug.LogWarning($"[BalanceConfig] 스폰 주기 미발견: {key}");
            return 9f;
        }

        //# JSON row → 런타임 모델. 위반(§10 SJ3~SJ5)은 전부 모아 ChapterDataException 으로 던진다.
        public static BalanceConfig Build(BalanceConfigRow config, MonsterStatRow[] monsters)
        {
            if (config == null)
                throw new ChapterDataException("BalanceConfig.json 이 비어 있음");

            List<string> errors = new List<string>();
            ValidateConfig(config, errors);
            ValidateMonsters(monsters, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("밸런스 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return new BalanceConfig(config, monsters);
        }

        private static void ValidateConfig(BalanceConfigRow config, List<string> errors)
        {
            if (config.heroHp < 1 || config.heroPower < 1)
            {
                errors.Add("BalanceConfig heroHp·heroPower 는 1 이상이어야 함");
            }
            if (config.runDuration <= 0f || float.IsNaN(config.runDuration))
            {
                errors.Add("BalanceConfig runDuration 은 0 보다 커야 함");
            }
            ValidateThresholds("passiveThresholds", config.passiveThresholds, 1f, false, errors);
            ValidateThresholds("activeThresholds", config.activeThresholds, config.runDuration, true, errors);
        }

        //# 0 초과 upper 미만 + 엄격 단조(ascending 이면 오름차순, 아니면 내림차순). 빈 배열은 필수 필드 누락으로 본다.
        private static void ValidateThresholds(string name, float[] values, float upper, bool ascending, List<string> errors)
        {
            if (values == null || values.Length == 0)
            {
                errors.Add($"BalanceConfig {name} 가 비어 있음");
                return;
            }
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0f || values[i] >= upper || float.IsNaN(values[i]))
                {
                    errors.Add($"BalanceConfig {name}[{i}] = {values[i]} 는 0 초과 {upper} 미만이어야 함");
                }
                if (i > 0 && (ascending ? values[i] <= values[i - 1] : values[i] >= values[i - 1]))
                {
                    errors.Add($"BalanceConfig {name}[{i}] = {values[i]} 는 {(ascending ? "오름차순" : "내림차순")}이어야 함");
                }
            }
        }

        //# PK 중복 0 · monster 는 기준 6종 값명 · 기준 6종 전부 존재 · 값 범위.
        private static void ValidateMonsters(MonsterStatRow[] monsters, List<string> errors)
        {
            HashSet<EMonster> seen = new HashSet<EMonster>();
            if (monsters != null)
            {
                foreach (MonsterStatRow row in monsters)
                {
                    if (row == null || FormationDatabase.TryParseMonster(row.monster, out EMonster key) == false || MonsterGrades.IsBase(key) == false)
                    {
                        errors.Add($"MonsterStat.json monster '{row?.monster}' 는 EMonster 기준 6종 값명이어야 함");
                        continue;
                    }
                    if (seen.Add(key) == false)
                    {
                        errors.Add($"MonsterStat.json monster {key} 중복");
                        continue;
                    }
                    if (row.hp < 1 || row.power < 1)
                    {
                        errors.Add($"MonsterStat.json {key} hp·power 는 1 이상이어야 함");
                    }
                    if (row.range <= 0f || row.cooldown <= 0f || row.moveSpeed <= 0f || row.spawnPeriod <= 0f
                        || float.IsNaN(row.range) || float.IsNaN(row.cooldown) || float.IsNaN(row.moveSpeed) || float.IsNaN(row.spawnPeriod))
                    {
                        errors.Add($"MonsterStat.json {key} range·cooldown·moveSpeed·spawnPeriod 는 0 보다 커야 함");
                    }
                }
            }

            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                if (seen.Contains(monster) == false)
                {
                    errors.Add($"MonsterStat.json 에 {monster} 행이 없음");
                }
            }
        }
    }
}
